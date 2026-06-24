using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EspetoMinigame : MonoBehaviour, IMinigameControllable
{
    public enum EspetoState { Empty, Cooking, Done, Burned }

    [System.Serializable]
    public class Espeto
    {
        public EspetoState state         = EspetoState.Empty;
        public float cookProgress        = 0f;
        public float burnTimer           = 0f;
        public float zonePosition        = 0.5f;
        public float espetoPosition      = 0.5f;
        public float zoneShiftTimer      = 0f;
    }

    [Header("Configuración Espetos")]
    public float DuracionCocina      = 45f;
    public float DuracionQuema       = 30f;
    public float intervaloCambioZona = 25f;
    public float margenZonaVerde     = 0.15f;
    public float velocidadMovimiento = 0.6f;
    public int   espetoCount         = 3;

    [Header("Food Output")]
    public GameObject espetoFoodPrefab;

    [Header("Interacción en Escena")]
    public float interactionDistance = 2.5f;

    [Header("UI - Panel")]
    public GameObject minigamePanel;

    [Header("UI - Por Espeto")]
    public RectTransform[] greenZoneRects;
    public RectTransform[] espetoHandleRects;
    public Image[]         espetoTrackImages;
    public TMP_Text[]      timerTexts;
    public GameObject[]    selectionFrames;

    [Header("UI - Flechas de Selección")]
    [Tooltip("Las 3 flechas fijas de arriba.")]
    public GameObject[]    selectionArrows;
    [Tooltip("Las 3 mini flechas hijas.")]
    public GameObject[]    controlArrows;

    [Header("UI - Colores de Flecha ")]
    public Color colorFlechaVacia      = Color.white;
    public Color colorFlechaConEspeto  = new Color(1f, 0.6f, 0f); 

    [Header("UI - Global")]
    public TMP_Text instructionText;

    private Espeto[]         _espetos;
    private int              _selectedIndex   = 0;
    private bool             _isRepositioning = false;
    private bool             _isPanelOpen     = false;
    private PlayerController _player;
    private float            _navCooldown     = 0f;
    private Vector2          _currentNav      = Vector2.zero;

    private static readonly Color ColEmpty   = new Color(0.6f, 0.6f, 0.6f);
    private static readonly Color ColCooking = new Color(1f, 0.55f, 0.1f);
    private static readonly Color ColDone    = new Color(0.2f, 0.85f, 0.2f);
    private static readonly Color ColBurned  = new Color(0.15f, 0.1f, 0.1f);

    void Awake()
    {
        _espetos = new Espeto[espetoCount];
        for (int i = 0; i < espetoCount; i++)
            _espetos[i] = new Espeto
            {
                zonePosition   = Random.Range(0.2f, 0.8f),
                espetoPosition = 0.5f,
                zoneShiftTimer = intervaloCambioZona
            };

        if (minigamePanel) minigamePanel.SetActive(false);
    }

    void Update()
    {
        TickTimers();
        if (_navCooldown > 0f) _navCooldown -= Time.deltaTime;

        if (_isPanelOpen && _isRepositioning)
        {
            Espeto sel = _espetos[_selectedIndex];
            if (sel.state == EspetoState.Cooking)
            {
                sel.espetoPosition = Mathf.Clamp01(
                    sel.espetoPosition + _currentNav.y * velocidadMovimiento * Time.deltaTime);
            }
        }

        if (_isPanelOpen) RefreshUI();
    }

    public void TryOpen(PlayerController player)
    {
        float dist = Vector3.Distance(transform.position, player.transform.position);
        if (dist <= interactionDistance) OpenPanel(player);
    }

    void OpenPanel(PlayerController player)
    {
        _player          = player;
        _isPanelOpen     = true;
        _isRepositioning = false;
        _selectedIndex   = 0;
        _currentNav      = Vector2.zero;
        _navCooldown     = 0f;
        InputManager.Instance.EnterMinigame(this);
        minigamePanel.SetActive(true);
        RefreshUI();
    }

    void ClosePanel()
    {
        _isPanelOpen     = false;
        _isRepositioning = false;
        _currentNav      = Vector2.zero;
        minigamePanel.SetActive(false);
        InputManager.Instance.ExitMinigame();
    }

    void TickTimers()
    {
        for (int i = 0; i < espetoCount; i++)
        {
            Espeto e = _espetos[i];
            if (e.state != EspetoState.Cooking) continue;

            bool inZone = IsInGreenZone(i);
            if (inZone)
            {
                e.cookProgress += Time.deltaTime;
                e.burnTimer     = Mathf.Min(e.burnTimer + Time.deltaTime * 0.5f, DuracionQuema);
                if (e.cookProgress >= DuracionCocina)
                {
                    e.state = EspetoState.Done;
                    AudioManager.Instance?.PlaySFX("espeto_done");
                    if (i == _selectedIndex) _isRepositioning = false;
                    continue;
                }
            }
            else
            {
                e.burnTimer -= Time.deltaTime;
                if (e.burnTimer <= 0f)
                {
                    e.state = EspetoState.Burned;
                    AudioManager.Instance?.PlaySFX("espeto_burned");
                    if (i == _selectedIndex) _isRepositioning = false;
                    continue;
                }
            }

            e.zoneShiftTimer -= Time.deltaTime;
            if (e.zoneShiftTimer <= 0f) { ShiftZone(i); e.zoneShiftTimer = intervaloCambioZona; }
        }
    }

    void PlaceEspeto(int index)
    {
        Espeto e         = _espetos[index];
        e.state          = EspetoState.Cooking;
        e.cookProgress   = 0f;
        e.burnTimer      = DuracionQuema;
        e.espetoPosition = 0.5f;
        e.zonePosition   = Random.Range(0.2f, 0.8f);
        e.zoneShiftTimer = intervaloCambioZona;
    }

    void PickupEspeto(int index)
    {
        _espetos[index].state = EspetoState.Empty;
        if (espetoFoodPrefab != null && _player != null)
            _player.CreateAndHoldFood(espetoFoodPrefab);
        ClosePanel();
    }

    void DiscardEspeto(int index) => _espetos[index].state = EspetoState.Empty;
    void ShiftZone(int index)     => _espetos[index].zonePosition = Random.Range(0.2f, 0.8f);
    bool IsInGreenZone(int index) =>
        Mathf.Abs(_espetos[index].espetoPosition - _espetos[index].zonePosition) <= margenZonaVerde;

    void RefreshUI()
    {
        for (int i = 0; i < espetoCount; i++)
        {
            Espeto e = _espetos[i];

            if (espetoTrackImages != null && i < espetoTrackImages.Length && espetoTrackImages[i])
                espetoTrackImages[i].color = e.state switch
                {
                    EspetoState.Empty   => ColEmpty,
                    EspetoState.Cooking => ColCooking,
                    EspetoState.Done    => ColDone,
                    EspetoState.Burned  => ColBurned,
                    _                   => ColEmpty
                };

            if (greenZoneRects != null && i < greenZoneRects.Length && greenZoneRects[i])
            {
                greenZoneRects[i].anchorMin = new Vector2(0, Mathf.Clamp01(e.zonePosition - margenZonaVerde));
                greenZoneRects[i].anchorMax = new Vector2(1, Mathf.Clamp01(e.zonePosition + margenZonaVerde));
                greenZoneRects[i].offsetMin = Vector2.zero;
                greenZoneRects[i].offsetMax = Vector2.zero;
                greenZoneRects[i].gameObject.SetActive(e.state == EspetoState.Cooking);
            }

            if (espetoHandleRects != null && i < espetoHandleRects.Length && espetoHandleRects[i])
            {
                float pos = e.espetoPosition;
                espetoHandleRects[i].anchorMin = new Vector2(0f, pos - 0.06f);
                espetoHandleRects[i].anchorMax = new Vector2(1f, pos + 0.06f);
                espetoHandleRects[i].offsetMin = Vector2.zero;
                espetoHandleRects[i].offsetMax = Vector2.zero;
                espetoHandleRects[i].gameObject.SetActive(e.state != EspetoState.Empty);
            }

            if (timerTexts != null && i < timerTexts.Length && timerTexts[i])
                timerTexts[i].text = e.state switch
                {
                    EspetoState.Empty   => "[E] Poner espeto",
                    EspetoState.Cooking => $"{e.burnTimer:F1}s  {e.cookProgress / DuracionCocina * 100f:F0}%",
                    EspetoState.Done    => "¡LISTO! [E] Recoger",
                    EspetoState.Burned  => "QUEMADO [E] Tirar",
                    _                   => ""
                };

            if (selectionFrames != null && i < selectionFrames.Length && selectionFrames[i])
                selectionFrames[i].SetActive(i == _selectedIndex);

            bool isCurrent = (i == _selectedIndex);

            // Flechas de arriba fijas
            if (selectionArrows != null && i < selectionArrows.Length && selectionArrows[i])
            {
                selectionArrows[i].SetActive(isCurrent && !_isRepositioning);
                
                Image arrowImg = selectionArrows[i].GetComponent<Image>();
                if (arrowImg != null)
                {
                    arrowImg.color = (e.state == EspetoState.Empty) ? colorFlechaVacia : colorFlechaConEspeto;
                }
            }

            // Mini flechas del cubo blanco
            if (controlArrows != null && i < controlArrows.Length && controlArrows[i])
            {
                controlArrows[i].SetActive(isCurrent && _isRepositioning);
            }
        }

        if (instructionText)
        {
            Espeto sel = _espetos[_selectedIndex];
            instructionText.text = sel.state switch
            {
                EspetoState.Empty   => "← → Navegar  |  E Poner espeto  |  Q Cerrar",
                EspetoState.Cooking => _isRepositioning
                    ? "↑ ↓ Mover espeto  |  E Soltar  |  Q Cerrar"
                    : "← → Navegar  |  E Reposicionar  |  Q Cerrar",
                EspetoState.Done    => "← → Navegar  |  E Recoger espeto  |  Q Cerrar",
                EspetoState.Burned  => "← → Navegar  |  E Tirar  |  Q Cerrar",
                _                   => ""
            };
        }
    }

    //  IMinigameControllable

    public void OnInteract()
    {
        Espeto sel = _espetos[_selectedIndex];
        switch (sel.state)
        {
            case EspetoState.Empty:   PlaceEspeto(_selectedIndex);           break;
            case EspetoState.Cooking: _isRepositioning = !_isRepositioning;  break;
            case EspetoState.Done:    PickupEspeto(_selectedIndex);          break;
            case EspetoState.Burned:  DiscardEspeto(_selectedIndex);         break;
        }

        if (_espetos[_selectedIndex].state != EspetoState.Cooking)
            _isRepositioning = false;
    }

    public void OnNavigate(Vector2 direction)
    {
        _currentNav = direction;

        if (_isRepositioning && _espetos[_selectedIndex].state != EspetoState.Cooking)
            _isRepositioning = false;

        if (_isRepositioning) return;

        if (_navCooldown > 0f) return;
        if (direction.x > 0.5f)
        {
            _selectedIndex = (_selectedIndex + 1) % espetoCount;
            _navCooldown   = 0.25f;
        }
        else if (direction.x < -0.5f)
        {
            _selectedIndex = (_selectedIndex - 1 + espetoCount) % espetoCount;
            _navCooldown   = 0.25f;
        }
    }

    public void OnCancel() => ClosePanel();
    public void OnSubmit() => OnInteract();

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}