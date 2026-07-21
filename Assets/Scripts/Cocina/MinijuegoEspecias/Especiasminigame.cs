using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class EspeciasMinigame : MonoBehaviour, IMinigameControllable
{
    [Header("Interacción en Escena")]
    public float interactionDistance = 2.5f;

    [Header("Cuchara (UI)")]
    public RectTransform cucharaRect;
    public float cucharaSpeed = 400f;
    public float cucharaMinX  = -350f;
    public float cucharaMaxX  =  350f;
    public float cucharaSmoothing = 14f;

    [Header("Bala (UI)")]
    public RectTransform balaPrefabRect;
    public float balaSpeed = 700f;
    public int maxBalasEnVuelo = 1;
    public float fireCooldown = 0.12f;
    public float muzzleOffset = 40f;
    public float maxSweepStep = 20f;

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

    private readonly List<RectTransform> _balas = new List<RectTransform>();
    private float   _fireCooldownLeft;
    private float   _cucharaVel;
    private bool[]  _especiaEnContacto; // detección de flanco para los rebotes
    private Vector2 _navInput = Vector2.zero;

    // Buffer reutilizable para GetWorldCorners (evita generar basura por frame).
    private static readonly Vector3[] _cornerBuf = new Vector3[4];

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
        if (_fireCooldownLeft > 0f) _fireCooldownLeft -= Time.deltaTime;

        MoveCuchara();
        HandleBalas();
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

        // Limpieza por si quedó algo colgando de un intento anterior.
        DeactivateAllLayouts();
        DestroyAllBalas();
        CancelInvoke(nameof(CheckFallo));

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
        _especiaEnContacto  = new bool[_totalEspecias];
        _navInput           = Vector2.zero;
        _cucharaVel         = 0f;
        _fireCooldownLeft   = 0f;

        InputManager.Instance.EnterMinigame(this);
        minigamePanel.SetActive(true);
        cucharaRect.gameObject.SetActive(true);
        cucharaRect.anchoredPosition = Vector2.zero;

        _isPlaying = true;
        RefreshUI();
    }

    // ------------------------------------------------------------------
    //  Cuchara
    // ------------------------------------------------------------------

    void MoveCuchara()
    {
        // Pequeña inercia, independiente del framerate. Con smoothing = 0,
        // control directo como antes.
        float targetVel = _navInput.x * cucharaSpeed;
        _cucharaVel = cucharaSmoothing > 0f
            ? Mathf.Lerp(_cucharaVel, targetVel, 1f - Mathf.Exp(-cucharaSmoothing * Time.deltaTime))
            : targetVel;

        Vector2 pos = cucharaRect.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x + _cucharaVel * Time.deltaTime, cucharaMinX, cucharaMaxX);
        cucharaRect.anchoredPosition = pos;
    }

    // ------------------------------------------------------------------
    //  Balas
    // ------------------------------------------------------------------

    void Shoot()
    {
        if (!_isPlaying) return;
        if (_fireCooldownLeft > 0f) return;
        if (_balasRestantes <= 0) return;
        if (_balas.Count >= Mathf.Max(1, maxBalasEnVuelo)) return;

        _balasRestantes--;
        _fireCooldownLeft = fireCooldown;

        GameObject balaObj = Instantiate(balaPrefabRect.gameObject, minigamePanel.transform);
        balaObj.SetActive(true);

        RectTransform bala = balaObj.GetComponent<RectTransform>();
        bala.localScale = Vector3.one;

        // Colocar la bala en la posición de MUNDO de la cuchara (funciona con
        // cualquier jerarquía) y aplicar el offset del cañón en espacio del panel.
        bala.position = cucharaRect.position;
        Vector3 lp = bala.localPosition;
        lp.z = 0f;
        bala.localPosition = lp + Vector3.up * muzzleOffset;

        _balas.Add(bala);

        AudioManager.Instance?.PlaySFX("especias_shoot");
        RefreshUI();
    }

    void HandleBalas()
    {
        if (_balas.Count == 0 || _layoutActivo == null) return;

        float move = balaSpeed * Time.deltaTime;
        RectTransform panelRect = minigamePanel.GetComponent<RectTransform>();

        for (int i = _balas.Count - 1; i >= 0; i--)
        {
            RectTransform bala = _balas[i];
            if (bala == null) { _balas.RemoveAt(i); continue; }

            // BARRIDO: avanzar en pasos pequeños comprobando colisión en cada
            // uno, para que la bala no atraviese especias entre frame y frame.
            bool dead  = false;
            int  steps = Mathf.Max(1, Mathf.CeilToInt(move / Mathf.Max(1f, maxSweepStep)));
            float stepDist = move / steps;

            for (int s = 0; s < steps && !dead; s++)
            {
                bala.localPosition += Vector3.up * stepDist;
                dead = BalaCollisionStep(bala);
            }

            // Techo del panel.
            if (!dead && bala.localPosition.y > panelRect.rect.height * 0.5f)
                dead = true;

            if (dead) KillBala(i);
        }

        if (_especiasCongeladas >= _totalEspecias)
            EndGame(true);
    }

    /// <summary>Comprueba colisiones de la bala en su posición actual. True si la bala muere.</summary>
    bool BalaCollisionStep(RectTransform bala)
    {
        Rect balaRect = GetWorldRect(bala);
        EspeciaUI[] especias = _layoutActivo.GetEspecias();

        // 1) Especias vivas: congelar.
        foreach (EspeciaUI especia in especias)
        {
            if (especia.IsCongelada) continue;
            if (balaRect.Overlaps(GetWorldRect(especia.Rect)))
            {
                especia.Congelar();
                _especiasCongeladas++;
                AudioManager.Instance?.PlaySFX("especias_freeze");
                return true;
            }
        }

        // 2) Especias congeladas: bloquean el tiro.
        foreach (EspeciaUI especia in especias)
        {
            if (!especia.IsCongelada) continue;
            if (balaRect.Overlaps(GetWorldRect(especia.Rect))) return true;
        }

        // 3) Cubos negros: bloquean el tiro.
        foreach (RectTransform cubo in _layoutActivo.GetCuboNegros())
            if (balaRect.Overlaps(GetWorldRect(cubo))) return true;

        return false;
    }

    void KillBala(int index)
    {
        if (_balas[index] != null) Destroy(_balas[index].gameObject);
        _balas.RemoveAt(index);

        // Sin balas en la recámara ni en vuelo y quedan especias → fallo
        // (con un respiro de 0.3s por si el último tiro fue el bueno).
        if (_balasRestantes <= 0 && _balas.Count == 0)
        {
            CancelInvoke(nameof(CheckFallo));
            Invoke(nameof(CheckFallo), 0.3f);
        }
    }

    void DestroyAllBalas()
    {
        foreach (RectTransform b in _balas)
            if (b != null) Destroy(b.gameObject);
        _balas.Clear();
    }



    void HandleColisionesEspecias()
    {
        if (_layoutActivo == null) return;
        EspeciaUI[] especias = _layoutActivo.GetEspecias();

        for (int i = 0; i < especias.Length; i++)
        {
            EspeciaUI e = especias[i];
            if (e.IsCongelada) { _especiaEnContacto[i] = false; continue; }

            Rect re = GetWorldRect(e.Rect);
            bool contacto = false;

            foreach (RectTransform cubo in _layoutActivo.GetCuboNegros())
                if (re.Overlaps(GetWorldRect(cubo))) { contacto = true; break; }

            if (!contacto)
            {
                foreach (EspeciaUI otra in especias)
                    if (otra != e && otra.IsCongelada &&
                        re.Overlaps(GetWorldRect(otra.Rect))) { contacto = true; break; }
            }


            if (contacto && !_especiaEnContacto[i]) e.Rebotar();
            _especiaEnContacto[i] = contacto;
        }
    }



    void CheckFallo()
    {
        if (_isPlaying && _especiasCongeladas < _totalEspecias) EndGame(false);
    }

    void EndGame(bool success)
    {
        if (!_isPlaying) return;
        _isPlaying = false;

        CancelInvoke(nameof(CheckFallo));
        DestroyAllBalas();
        if (_layoutActivo != null) _layoutActivo.gameObject.SetActive(false);
        cucharaRect.gameObject.SetActive(false);
        minigamePanel.SetActive(false);
        InputManager.Instance.ExitMinigame();

        if (success)
        {
            MinigameFeedback.Show(true, $"¡{_currentRecipe.dishName} listo!", "especias_success");

            if (_currentRecipe.foodPrefab != null)
                _player.CreateAndHoldFood(_currentRecipe.foodPrefab, _currentRecipe);
            else
                Debug.LogWarning($"[EspeciasMinigame] {_currentRecipe.dishName} no tiene foodPrefab.");
        }
        else
        {
            MinigameFeedback.Show(false, "¡Te quedaste sin balas!", "especias_failure");
        }
    }


    Rect GetWorldRect(RectTransform rt)
    {
        rt.GetWorldCorners(_cornerBuf);
        Vector3 bl = _cornerBuf[0]; // esquina inferior-izquierda
        Vector3 tr = _cornerBuf[2]; // esquina superior-derecha
        return new Rect(bl.x, bl.y, tr.x - bl.x, tr.y - bl.y);
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