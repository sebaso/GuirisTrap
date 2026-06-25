using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EspeciasMinigame : MonoBehaviour, IMinigameControllable
{
    [Header("Interacción en Escena")]
    public float interactionDistance = 2.5f;

    [Header("Cuchara (UI)")]
    public RectTransform cucharaRect;
    public float cucharaSpeed = 400f;
    public float cucharaMinX  = -350f;
    public float cucharaMaxX  =  350f;

    [Header("Bala (UI)")]
    public RectTransform balaPrefabRect;
    public float balaSpeed = 600f;

    [Header("Layouts - dificultad")]
    public GameObject[] layoutsFacil;
    public GameObject[] layoutsNormal;
    public GameObject[] layoutsDificil;
    public GameObject[] layoutsImposible;

    [Header("Velocidad base de Especias")]
    public float baseEspeciaSpeed   = 80f;
    public float speedPerDifficulty = 30f;

    [Header("UI - Panel")]
    public GameObject minigamePanel;
    public TMP_Text   balasText;
    public TMP_Text   instruccionText;

    private bool               _isPlaying = false;
    private PlayerController   _player;
    private EspeciasRecipeData _currentRecipe;
    private int   _balasRestantes;
    private int   _totalEspecias;
    private int   _especiasCongeladas;
    private EspecieroLayout _layoutActivo;
    private RectTransform   _balaActiva;
    private bool            _balaEnVuelo = false;
    private Vector2         _navInput    = Vector2.zero;

    void Awake()
    {
        if (minigamePanel) minigamePanel.SetActive(false);
        if (cucharaRect)   cucharaRect.gameObject.SetActive(false);
        DeactivateAllLayouts();
    }


    private void DeactivateAllLayouts()
    {
        GameObject[][] grupos = { layoutsFacil, layoutsNormal, layoutsDificil, layoutsImposible };
        foreach (GameObject[] grupo in grupos)
        {
            if (grupo == null) continue;
            foreach (GameObject layout in grupo)
                if (layout != null) layout.SetActive(false);
        }
    }

    void Update()
    {
        if (!_isPlaying) return;
        MoveCuchara();
        HandleBala();
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

        // Apaga cualquier layout que hubiera quedado activo de un intento anterior.
        DeactivateAllLayouts();

        GameObject[][] grupos = { layoutsFacil, layoutsNormal, layoutsDificil, layoutsImposible };
        int grupoIdx = Mathf.Clamp(_currentRecipe.difficulty - 1, 0, grupos.Length - 1);
        GameObject[] grupo = grupos[grupoIdx];

        if (grupo == null || grupo.Length == 0)
        {
            Debug.LogWarning($"[EspeciasMinigame] No hay layouts en el grupo {grupoIdx}.");
            return;
        }

        int idx = Random.Range(0, grupo.Length);
        grupo[idx].SetActive(true);
        _layoutActivo = grupo[idx].GetComponent<EspecieroLayout>();

        if (_layoutActivo == null)
        {
            Debug.LogError("[EspeciasMinigame] Sin EspecieroLayout.");
            grupo[idx].SetActive(false); // no dejar el layout colgando
            return;
        }

        float vel = baseEspeciaSpeed + speedPerDifficulty * (_currentRecipe.difficulty - 1);
        foreach (EspeciaUI e in _layoutActivo.GetEspecias()) { e.speed = vel; e.Resetear(); }

        _totalEspecias      = _layoutActivo.GetEspecias().Length;
        _especiasCongeladas = 0;
        _balasRestantes     = _currentRecipe.balas;
        _balaEnVuelo        = false;
        _navInput           = Vector2.zero;

        InputManager.Instance.EnterMinigame(this);
        minigamePanel.SetActive(true);
        cucharaRect.gameObject.SetActive(true);
        cucharaRect.anchoredPosition = Vector2.zero;

        _isPlaying = true;
        RefreshUI();
    }

    void MoveCuchara()
    {
        Vector2 pos = cucharaRect.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x + _navInput.x * cucharaSpeed * Time.deltaTime, cucharaMinX, cucharaMaxX);
        cucharaRect.anchoredPosition = pos;
    }

    void Shoot()
    {
        if (_balaEnVuelo || _balasRestantes <= 0) return;

        _balasRestantes--;
        _balaEnVuelo = true;

        GameObject balaObj = Instantiate(balaPrefabRect.gameObject, minigamePanel.transform);
        balaObj.SetActive(true);
        _balaActiva = balaObj.GetComponent<RectTransform>();
        _balaActiva.localScale    = Vector3.one;
        _balaActiva.localPosition = cucharaRect.localPosition + Vector3.up * 40f;

        RefreshUI();
    }

    void HandleBala()
    {
        if (!_balaEnVuelo || _balaActiva == null) return;

        _balaActiva.localPosition += Vector3.up * balaSpeed * Time.deltaTime;
        Rect balaRect = GetCanvasRect(_balaActiva);

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

        foreach (EspeciaUI especia in _layoutActivo.GetEspecias())
        {
            if (!especia.IsCongelada) continue;
            if (balaRect.Overlaps(GetCanvasRect(especia.Rect))) { DestroyBala(); return; }
        }

        foreach (RectTransform cubo in _layoutActivo.GetCuboNegros())
            if (balaRect.Overlaps(GetCanvasRect(cubo))) { DestroyBala(); return; }

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
            Rect re = GetCanvasRect(especia.Rect);
            foreach (RectTransform cubo in _layoutActivo.GetCuboNegros())
                if (re.Overlaps(GetCanvasRect(cubo))) especia.Rebotar();
        }
    }

    void DestroyBala()
    {
        if (_balaActiva != null) Destroy(_balaActiva.gameObject);
        _balaActiva  = null;
        _balaEnVuelo = false;
        if (_balasRestantes <= 0) Invoke(nameof(CheckFallo), 0.3f);
    }

    void CheckRebotar()
    {
        foreach (EspeciaUI a in _layoutActivo.GetEspecias())
        {
            if (a.IsCongelada) continue;
            foreach (EspeciaUI b in _layoutActivo.GetEspecias())
                if (b.IsCongelada && GetCanvasRect(a.Rect).Overlaps(GetCanvasRect(b.Rect))) a.Rebotar();
            foreach (RectTransform cubo in _layoutActivo.GetCuboNegros())
                if (GetCanvasRect(a.Rect).Overlaps(GetCanvasRect(cubo))) a.Rebotar();
        }
    }

    void CheckVictoria() { if (_especiasCongeladas >= _totalEspecias) EndGame(true); }
    void CheckFallo()    { if (_isPlaying && _especiasCongeladas < _totalEspecias) EndGame(false); }

    void EndGame(bool success)
    {
        if (!_isPlaying) return;
        _isPlaying = false;
        if (_layoutActivo != null) _layoutActivo.gameObject.SetActive(false);
        if (_balaActiva   != null) Destroy(_balaActiva.gameObject);
        cucharaRect.gameObject.SetActive(false);
        minigamePanel.SetActive(false);
        InputManager.Instance.ExitMinigame();

        if (success)
        {
            Debug.Log($"¡Éxito! {_currentRecipe.dishName}");
            AudioManager.Instance?.PlaySFX("especias_success");
            if (_currentRecipe.foodPrefab != null)
                _player.CreateAndHoldFood(_currentRecipe.foodPrefab);
        }
        else
        {
            Debug.Log("[EspeciasMinigame] Sin balas. ¡Fallaste!");
            AudioManager.Instance?.PlaySFX("especias_failure");
        }
    }

    Rect GetCanvasRect(RectTransform rt)
    {
        Vector2 size   = rt.rect.size;
        Vector2 center = rt.localPosition;
        return new Rect(center - size * 0.5f, size);
    }

    void RefreshUI()
    {
        if (balasText)       balasText.text      = $"Balas: {_balasRestantes}";
        if (instruccionText) instruccionText.text = "← → Mover  |  E Disparar";
    }

    //  IMinigameControllable 

    public void OnInteract()              => Shoot();
    public void OnSubmit()                => Shoot();
    public void OnCancel()                { }
    public void OnNavigate(Vector2 dir)   => _navInput = dir;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}