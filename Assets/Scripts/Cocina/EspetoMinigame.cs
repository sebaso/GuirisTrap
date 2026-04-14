using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EspetoMinigame : MonoBehaviour
{
    public enum EspetoState { Empty, Cooking, Done, Burned }

    [System.Serializable]
    public class Espeto
    {
        public EspetoState state    = EspetoState.Empty;
        public float cookProgress   = 0f;  
        public float burnTimer      = 0f;   
        public float zonePosition   = 0.5f; 
        public float espetoPosition = 0.5f; 
        public float zoneShiftTimer = 0f;   
    }

    [Header("Configuración Espetos")]
    public float cookDuration      = 20f;  
    public float burnDuration      = 10f;  
    public float zoneShiftInterval = 12f;  
    public float zoneHalfSize      = 0.12f;
    public float moveSpeed         = 0.6f; 
    public int   espetoCount       = 3;    

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

    [Header("UI - Global")]
    public TMP_Text instructionText;


    private Espeto[]      _espetos;
    private int           _selectedIndex    = 0;
    private bool          _isRepositioning  = false;
    private bool          _isPanelOpen      = false;
    private PlayerController _player;


    private static readonly Color ColEmpty   = new Color(0.6f, 0.6f, 0.6f);
    private static readonly Color ColCooking = new Color(1f, 0.55f, 0.1f);
    private static readonly Color ColDone    = new Color(0.2f, 0.85f, 0.2f);
    private static readonly Color ColBurned  = new Color(0.15f, 0.1f, 0.1f);


    void Awake()
    {
        _espetos = new Espeto[espetoCount];
        for (int i = 0; i < espetoCount; i++)
        {
            _espetos[i] = new Espeto
            {
                zonePosition   = Random.Range(0.2f, 0.8f),
                espetoPosition = 0.5f,
                zoneShiftTimer = zoneShiftInterval
            };
        }

        if (minigamePanel) minigamePanel.SetActive(false);
    }


    void Start()
    {

    }

    void Update()
    {
        TickTimers();

        if (!_isPanelOpen)
        {
            DetectPlayerInteraction();
            return;
        }

        HandlePanelInput();
        RefreshUI();
    }

    private void DetectPlayerInteraction()
    {
        if (_player == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("Player");
            if (obj) _player = obj.GetComponent<PlayerController>();
        }
        if (_player == null) return;

        float dist = Vector3.Distance(transform.position, _player.transform.position);
        if (dist <= interactionDistance && Input.GetKeyDown(KeyCode.E))
            OpenPanel();
    }

    void OpenPanel()
    {
        _isPanelOpen      = true;
        _isRepositioning  = false;
        _selectedIndex    = 0;
        _player.enabled   = false;
        minigamePanel.SetActive(true);
        RefreshUI();
    }

    void ClosePanel()
    {
        for (int i = 0; i < espetoCount; i++)
            if (_espetos[i].state == EspetoState.Cooking)
                ShiftZone(i);

        _isPanelOpen      = false;
        _isRepositioning  = false;
        minigamePanel.SetActive(false);
        if (_player) _player.enabled = true;
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
                // En zona verde: progresa la cocción, regenera burn timer
                e.cookProgress  += Time.deltaTime;
                e.burnTimer      = Mathf.Min(e.burnTimer + Time.deltaTime * 0.5f, burnDuration);

                if (e.cookProgress >= cookDuration)
                {
                    e.state = EspetoState.Done;
                    Debug.Log($"[Espetera] Espeto {i} ¡LISTO!");
                    continue;
                }
            }
            else
            {
                // Fuera de zona: el timer de quemado baja
                e.burnTimer -= Time.deltaTime;

                if (e.burnTimer <= 0f)
                {
                    e.state = EspetoState.Burned;
                    Debug.Log($"[Espetera] Espeto {i} ¡QUEMADO!");
                    continue;
                }
            }

            // Timer de desplazamiento de zona verde
            e.zoneShiftTimer -= Time.deltaTime;
            if (e.zoneShiftTimer <= 0f)
            {
                ShiftZone(i);
                e.zoneShiftTimer = zoneShiftInterval;
            }
        }
    }

    void HandlePanelInput()
    {
        // Cerrar panel
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
            return;
        }

        Espeto sel = _espetos[_selectedIndex];

        if (!_isRepositioning)
        {
            if ((Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)))
                _selectedIndex = (_selectedIndex - 1 + espetoCount) % espetoCount;
            if ((Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)))
                _selectedIndex = (_selectedIndex + 1) % espetoCount;

            sel = _espetos[_selectedIndex]; // refrescar tras navegar
        }

        // Acción con E según estado
        if (Input.GetKeyDown(KeyCode.E))
        {
            switch (sel.state)
            {
                case EspetoState.Empty:
                    PlaceEspeto(_selectedIndex);
                    break;

                case EspetoState.Cooking:
                    _isRepositioning = !_isRepositioning; // toggle reposicionamiento
                    break;

                case EspetoState.Done:
                    PickupEspeto(_selectedIndex);
                    return; // cierra panel
                    
                case EspetoState.Burned:
                    DiscardEspeto(_selectedIndex);
                    break;
            }
        }

        // Mover espeto con W/S 
        if (_isRepositioning && sel.state == EspetoState.Cooking)
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                sel.espetoPosition = Mathf.Clamp01(sel.espetoPosition + moveSpeed * Time.deltaTime);
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                sel.espetoPosition = Mathf.Clamp01(sel.espetoPosition - moveSpeed * Time.deltaTime);
        }
    }

    void PlaceEspeto(int index)
    {
        Espeto e = _espetos[index];
        e.state          = EspetoState.Cooking;
        e.cookProgress   = 0f;
        e.burnTimer      = burnDuration;
        e.espetoPosition = 0.5f;
        e.zonePosition   = Random.Range(0.2f, 0.8f);
        e.zoneShiftTimer = zoneShiftInterval;
        Debug.Log($"[Espetera] Espeto {index} colocado.");
    }

    void PickupEspeto(int index)
    {
        _espetos[index].state = EspetoState.Empty;

        if (espetoFoodPrefab != null && _player != null)
        {
            GameObject foodObj = Instantiate(espetoFoodPrefab,
                _player.transform.position + Vector3.up,
                Quaternion.identity);

            Food food = foodObj.GetComponent<Food>();
            if (food != null)
                food.PickUp(_player.holdPoint);
        }

        Debug.Log($"[Espetera] Espeto {index} recogido.");
        ClosePanel();
    }

    void DiscardEspeto(int index)
    {
        _espetos[index].state = EspetoState.Empty;
        Debug.Log($"[Espetera] Espeto {index} tirado a la basura.");
    }

    void ShiftZone(int index)
    {
        _espetos[index].zonePosition = Random.Range(0.2f, 0.8f);
        Debug.Log($"[Espetera] Espeto {index}: zona verde movida a {_espetos[index].zonePosition:F2}");
    }

    bool IsInGreenZone(int index)
    {
        Espeto e = _espetos[index];
        return Mathf.Abs(e.espetoPosition - e.zonePosition) <= zoneHalfSize;
    }

    void RefreshUI()
    {
        for (int i = 0; i < espetoCount; i++)
        {
            Espeto e = _espetos[i];

            // Fondo de la varilla 
            if (espetoTrackImages != null && i < espetoTrackImages.Length && espetoTrackImages[i])
            {
                espetoTrackImages[i].color = e.state switch
                {
                    EspetoState.Empty   => ColEmpty,
                    EspetoState.Cooking => ColCooking,
                    EspetoState.Done    => ColDone,
                    EspetoState.Burned  => ColBurned,
                    _                   => ColEmpty
                };
            }

            // — Zona verde
            if (greenZoneRects != null && i < greenZoneRects.Length && greenZoneRects[i])
            {
                float min = Mathf.Clamp01(e.zonePosition - zoneHalfSize);
                float max = Mathf.Clamp01(e.zonePosition + zoneHalfSize);
                greenZoneRects[i].anchorMin  = new Vector2(0, min);
                greenZoneRects[i].anchorMax  = new Vector2(1, max);
                greenZoneRects[i].offsetMin  = Vector2.zero;
                greenZoneRects[i].offsetMax  = Vector2.zero;
                greenZoneRects[i].gameObject.SetActive(e.state == EspetoState.Cooking);
            }

            // — Handle del espeto
            if (espetoHandleRects != null && i < espetoHandleRects.Length && espetoHandleRects[i])
            {
                float pos = e.espetoPosition;
                espetoHandleRects[i].anchorMin  = new Vector2(0f, pos - 0.06f);
                espetoHandleRects[i].anchorMax  = new Vector2(1f, pos + 0.06f);
                espetoHandleRects[i].offsetMin  = Vector2.zero;
                espetoHandleRects[i].offsetMax  = Vector2.zero;
                espetoHandleRects[i].gameObject.SetActive(e.state != EspetoState.Empty);
            }

            // Texto de timer 
            if (timerTexts != null && i < timerTexts.Length && timerTexts[i])
            {
                timerTexts[i].text = e.state switch
                {
                    EspetoState.Empty   => "[E] Poner espeto",
                    EspetoState.Cooking => $" {e.burnTimer:F1}s | ✅ {e.cookProgress / cookDuration * 100f:F0}%",
                    EspetoState.Done    => "¡LISTO! [E] Recoger",
                    EspetoState.Burned  => "QUEMADO [E] Tirar",
                    _                   => ""
                };
            }

            // — Highlight del seleccionado —
            if (selectionFrames != null && i < selectionFrames.Length && selectionFrames[i])
                selectionFrames[i].SetActive(i == _selectedIndex);
        }
        if (instructionText)
        {
            Espeto sel = _espetos[_selectedIndex];
            instructionText.text = sel.state switch
            {
                EspetoState.Empty   => "A/D Navegar  |  E Poner espeto  |  ESC Cerrar",
                EspetoState.Cooking => _isRepositioning
                                        ? "W/S Mover espeto  |  E Soltar  |  ESC Cerrar"
                                        : "A/D Navegar  |  E Reposicionar  |  ESC Cerrar",
                EspetoState.Done    => "A/D Navegar  |  E Recoger espeto  |  ESC Cerrar",
                EspetoState.Burned  => "A/D Navegar  |  E Tirar  |  ESC Cerrar",
                _                   => ""
            };
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
