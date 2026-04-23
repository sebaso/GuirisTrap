using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EspeciasMinigame : MonoBehaviour
{
    [Header("Interacción en Escena")]
    public float interactionDistance = 2.5f;

    [Header("Cuchara (UI)")]
    public RectTransform cucharaRect;   // Image dentro del panel
    public float         cucharaSpeed  = 400f; // píxeles/segundo
    public float         cucharaMinX   = -350f; 
    public float         cucharaMaxX   =  350f; 

    [Header("Bala (UI)")]
    public RectTransform balaPrefabRect; 
    public float         balaSpeed     = 600f; 

    [Header("Layouts por grupo DE DIFICULTAD")]
    public GameObject[][] layoutGroups; 

    [Tooltip("Grupo 0 = Fácil")]
    public GameObject[] layoutsFacil;
    [Tooltip("Grupo 1 = Normal")]
    public GameObject[] layoutsNormal;
    [Tooltip("Grupo 2 = Difícil")]
    public GameObject[] layoutsDificil;
    [Tooltip("Grupo 3 = Imposible")]
    public GameObject[] layoutsImposible;

    [Header("Velocidad base de Especias")]
    public float baseEspeciaSpeed = 80f;  
    public float speedPerDifficulty = 30f; 

    [Header("UI - Panel")]
    public GameObject minigamePanel;
    public TMP_Text   balasText;
    public TMP_Text   instruccionText;

    private bool              _isPlaying      = false;
    private PlayerController  _player;
    private EspeciasRecipeData _currentRecipe;

    private int               _balasRestantes;
    private int               _totalEspecias;
    private int               _especiasCongeladas;

    private EspecieroLayout   _layoutActivo;
    private RectTransform     _balaActiva; // solo una bala a la vez !!!
    private bool              _balaEnVuelo = false;


    void Awake()
    {
        if (minigamePanel) minigamePanel.SetActive(false);
        if (cucharaRect)   cucharaRect.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!_isPlaying) return;
        HandleCuchara();
        HandleBala();
        HandleShoot();
        HandleColisionesEspecias();
    }

    public void StartMinigame(RecipeData recipe, PlayerController currentPlayer)
    {
        _currentRecipe = recipe as EspeciasRecipeData;
        if (_currentRecipe == null)
        {
            Debug.LogError("[EspeciasMinigame] La receta no es EspeciasRecipeData.");
            return;
        }

        _player = currentPlayer;
        _player.enabled = false;

        GameObject[][] grupos = { layoutsFacil, layoutsNormal, layoutsDificil, layoutsImposible };
        int grupoIdx = Mathf.Clamp(_currentRecipe.difficulty - 1, 0, grupos.Length - 1);
        GameObject[] grupo = grupos[grupoIdx];

        if (grupo == null || grupo.Length == 0)
        {
            Debug.LogWarning($"[EspeciasMinigame] No hay layouts en el grupo {grupoIdx}.");
            _player.enabled = true;
            return;
        }

        int idx = Random.Range(0, grupo.Length);
        grupo[idx].SetActive(true);
        _layoutActivo = grupo[idx].GetComponent<EspecieroLayout>();

        if (_layoutActivo == null)
        {
            Debug.LogError("[EspeciasMinigame] El layout no tiene EspecieroLayout.cs");
            _player.enabled = true;
            return;
        }

        float vel = baseEspeciaSpeed + speedPerDifficulty * (_currentRecipe.difficulty - 1);
        foreach (EspeciaUI e in _layoutActivo.GetEspecias())
        {
            e.speed = vel;
            e.Resetear();
        }

        _totalEspecias      = _layoutActivo.GetEspecias().Length;
        _especiasCongeladas = 0;
        _balasRestantes     = _currentRecipe.balas;
        _balaEnVuelo        = false;

        // UI
        minigamePanel.SetActive(true);
        cucharaRect.gameObject.SetActive(true);
        cucharaRect.anchoredPosition = Vector2.zero;

        _isPlaying = true;
        RefreshUI();
    }
    void HandleCuchara()
    {
        float h = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  h = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h =  1f;

        Vector2 pos = cucharaRect.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x + h * cucharaSpeed * Time.deltaTime, cucharaMinX, cucharaMaxX);
        cucharaRect.anchoredPosition = pos;
    }
void HandleShoot()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (_balaEnVuelo) return; // una bala a la vez
        if (_balasRestantes <= 0) return;

        _balasRestantes--;
        _balaEnVuelo = true;

        GameObject balaObj = Instantiate(balaPrefabRect.gameObject, minigamePanel.transform);
        balaObj.SetActive(true);
        _balaActiva = balaObj.GetComponent<RectTransform>();
        _balaActiva.localScale = Vector3.one; 
        _balaActiva.localPosition = cucharaRect.localPosition + (Vector3.up * 40f);

        RefreshUI();
    }

    void HandleBala()
    {
        if (!_balaEnVuelo || _balaActiva == null) return;

        // Mover bala hacia arriba
        _balaActiva.localPosition += Vector3.up * balaSpeed * Time.deltaTime;

        Rect balaRect = GetCanvasRect(_balaActiva);

        // Comprobar colisión con especias
        foreach (EspeciaUI especia in _layoutActivo.GetEspecias())
        {
            if (especia.IsCongelada) continue;

            if (balaRect.Overlaps(GetCanvasRect(especia.Rect)))
            {
                especia.Congelar();
                _especiasCongeladas++;
                DestroyBala();
                CheckRebotar(); 
                CheckVictoria();
                return;
            }
        }

        // Colisión con especia congelada
        foreach (EspeciaUI especia in _layoutActivo.GetEspecias())
        {
            if (!especia.IsCongelada) continue;
            if (balaRect.Overlaps(GetCanvasRect(especia.Rect)))
            {
                DestroyBala();
                return;
            }
        }

        // Colisión con cubo negro
        foreach (RectTransform cubo in _layoutActivo.GetCuboNegros())
        {
            if (balaRect.Overlaps(GetCanvasRect(cubo)))
            {
                DestroyBala();
                return;
            }
        }

        // Techo: si sale del panel
        RectTransform panelRect = minigamePanel.GetComponent<RectTransform>();
        if (_balaActiva.localPosition.y > panelRect.rect.height * 0.5f)
            DestroyBala();
    }
    void HandleColisionesEspecias()
    {
        if (_layoutActivo == null) return;

        foreach (EspeciaUI especia in _layoutActivo.GetEspecias())
        {
            if (especia.IsCongelada) continue;

            Rect rectEspecia = GetCanvasRect(especia.Rect);

            foreach (RectTransform cubo in _layoutActivo.GetCuboNegros())
            {
                if (rectEspecia.Overlaps(GetCanvasRect(cubo)))
                {
                    especia.Rebotar();
                }
            }
        }
    }

    void DestroyBala()
    {
        if (_balaActiva != null) Destroy(_balaActiva.gameObject);
        _balaActiva  = null;
        _balaEnVuelo = false;

        // Sin balas y no ha ganado = fallo
        if (_balasRestantes <= 0)
            Invoke(nameof(CheckFallo), 0.3f);
    }

    // SI EN LA MISMA LINEA HAY OTRA ESPECIA, REBOTA CON LAS CONGELADAS
    void CheckRebotar()
    {
        foreach (EspeciaUI a in _layoutActivo.GetEspecias())
        {
            if (a.IsCongelada) continue;
            foreach (EspeciaUI b in _layoutActivo.GetEspecias())
            {
                if (!b.IsCongelada) continue;
                if (GetCanvasRect(a.Rect).Overlaps(GetCanvasRect(b.Rect)))
                    a.Rebotar();
            }
            foreach (RectTransform cubo in _layoutActivo.GetCuboNegros())
            {
                if (GetCanvasRect(a.Rect).Overlaps(GetCanvasRect(cubo)))
                    a.Rebotar();
            }
        }
    }


    void CheckVictoria()
    {
        if (_especiasCongeladas >= _totalEspecias)
            EndGame(true);
    }

    void CheckFallo()
    {
        if (_isPlaying && _especiasCongeladas < _totalEspecias)
            EndGame(false);
    }

    void EndGame(bool success)
    {
        if (!_isPlaying) return;
        _isPlaying = false;

        if (_layoutActivo != null)
            _layoutActivo.gameObject.SetActive(false); // vuelve a desactivarse

        if (_balaActiva != null)
            Destroy(_balaActiva.gameObject);

        cucharaRect.gameObject.SetActive(false);
        minigamePanel.SetActive(false);
        _player.enabled = true;

        if (success)
        {
            Debug.Log($"[EspeciasMinigame] ¡Éxito! {_currentRecipe.dishName}");
            if (_currentRecipe.foodPrefab != null)
                Instantiate(_currentRecipe.foodPrefab, _player.transform.position, Quaternion.identity);
            else
                Debug.LogWarning($"[EspeciasMinigame] {_currentRecipe.dishName} no tiene foodPrefab.");
        }
        else
        {
            Debug.Log("[EspeciasMinigame] Sin balas. ¡Fallaste!");
        }
    }
    Rect GetCanvasRect(RectTransform rt)
    {
        Vector2 size = rt.rect.size;
        Vector2 center = rt.localPosition; 
        return new Rect(center - size * 0.5f, size);
    }

    void RefreshUI()
    {
        if (balasText)      
        {
            balasText.text      = $"Balas: <bounce>{_balasRestantes}</bounce>";
        }
        if (instruccionText) 
        {
            instruccionText.text = "A/D Mover  |  E Disparar";
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}