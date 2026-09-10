using System.Collections;
using UnityEngine;
public class PuertaFinDia : MonoBehaviour
{
    [Header("Cierre de la puerta")]
    [Tooltip("Grados que gira la hoja sobre su bisagra hasta quedar cerrada. " +
             "Negativo = hacia el marco (con el modelo actual, -25 la deja a ras).")]
    [SerializeField] private float _closeSwingDegrees = -25f;
    [Tooltip("Segundos que tarda el giro de cierre.")]
    [SerializeField] private float _closeDuration = 0.7f;
    [Tooltip("Sonido al quedarse cerrada (clave del AudioManager). Vacío = sin sonido " +
             "(si la clave no existe, el AudioManager la ignora con un warning).")]
    [SerializeField] private string _closeSfx = "";

    private DayManager Day => DayManager.Instance;

    /// <summary>El servicio ha terminado y la puerta todavía está operable.</summary>
    public bool CanInteract => Day != null && Day.IsWindingDown && !Day.IsDoorClosed && !_closing;

    // El pivote del nodo "Puerta" del modelo está en la bisagra: girando la
    // rotación original alrededor de la vertical por él, la hoja oscila.
    private Vector3 _hingeWorld;
    private Quaternion _openRotation;

    private bool _closing;       // animación de cierre en curso
    private bool _closedForDay;  // cerrada del todo (hasta el día siguiente)
    private Coroutine _closingRoutine;

    private bool _playerInside;
    private bool _promptActive;
    private bool _highlighted;
    private bool _subscribedDayStarted;
    private PlayerController _player;
    private Renderer _renderer;
    private Material _materialInstance;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        _hingeWorld = transform.position;
        _openRotation = transform.rotation;
        _renderer = GetComponentInChildren<Renderer>();
        EnsureTrigger();
    }

    private void OnEnable()
    {
        TrySubscribeDayStarted();
    }

    private void OnDisable()
    {
        if (_subscribedDayStarted && DayManager.Instance != null)
            DayManager.Instance.OnDayStarted -= OnDayStartedReset;
        _subscribedDayStarted = false;
    }

    // El DayManager puede no existir aún en OnEnable (orden de ejecución);
    // reintentamos desde Update hasta que aparezca (patrón de DayTimerUI).
    private void TrySubscribeDayStarted()
    {
        if (_subscribedDayStarted || DayManager.Instance == null) return;
        DayManager.Instance.OnDayStarted += OnDayStartedReset;
        _subscribedDayStarted = true;
    }

    private void OnDestroy()
    {
        if (_materialInstance != null)
            Destroy(_materialInstance);
    }

    // El collider de interacción es un trigger (no bloquea a clientes ni al
    // jugador; solo delimita la zona de "estar junto a la puerta"). Si el
    // objeto ya trae uno en la escena se reutiliza.
    private void EnsureTrigger()
    {
        BoxCollider trigger = GetComponent<BoxCollider>();
        if (trigger == null)
        {
            trigger = gameObject.AddComponent<BoxCollider>();
            Renderer r = GetComponentInChildren<Renderer>();
            if (r != null)
            {
                Bounds b = r.bounds;
                trigger.center = transform.InverseTransformPoint(b.center);
                // Cubo uniforme a partir del tamaño mundial + margen: con nodos
                // muy rotados (el modelo importa a 270° en X) un box por ejes
                // se descoloca al pasar de mundo a local.
                Vector3 worldSize = b.size + new Vector3(0.8f, 0.2f, 0.8f);
                Vector3 local = transform.InverseTransformVector(worldSize);
                float maxLocal = Mathf.Max(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z));
                trigger.size = Vector3.one * maxLocal;
            }
        }
        trigger.isTrigger = true;
    }

    private void Update()
    {
        TrySubscribeDayStarted();

        bool active = CanInteract;
        if (active != _highlighted)
        {
            _highlighted = active;
            SetHighlight(active);
            RefreshPrompt();
        }
    }

    // Día nuevo: reabrir la puerta (vuelve a su pose original de escena) y
    // rearmar el aviso y el resalte. Va por el evento OnDayStarted, no por
    // sondeo, para no pisar el cierre en curso.
    private void OnDayStartedReset()
    {
        if (_closingRoutine != null)
        {
            StopCoroutine(_closingRoutine);
            _closingRoutine = null;
        }
        _closing = false;
        _closedForDay = false;
        _highlighted = false;
        _promptActive = false;
        transform.rotation = _openRotation;
        SetHighlight(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;
        _player = player;
        _playerInside = true;
        player.SetNearbyDoor(this);
        RefreshPrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || player != _player) return;
        _playerInside = false;
        player.SetNearbyDoor(null);
        RefreshPrompt();
    }

    private void RefreshPrompt()
    {
        bool want = _playerInside && CanInteract;
        if (want == _promptActive) return;
        _promptActive = want;
        if (_player != null)
            _player.SetNearInteractable(want);
    }

    /// <summary>Cerrar la puerta: la hoja se cierra visiblemente y, al terminar
    /// el giro, se expulsa a los clientes y se muestra la pantalla de fin de día.
    /// Lo llama PlayerController al pulsar E junto a la puerta durante el wind-down.</summary>
    public void TryClose()
    {
        if (!CanInteract || _closingRoutine != null) return;
        _closingRoutine = StartCoroutine(CloseRoutine());
    }

    private IEnumerator CloseRoutine()
    {
        _closing = true;
        RefreshPrompt();

        // 1) La hoja gira hasta el marco acelerando, como una puerta de verdad.
        float t = 0f;
        while (t < _closeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / _closeDuration);
            SetSwing(_closeSwingDegrees * (k * k)); // ease-in
            yield return null;
        }

        // 2) Pequeño rebote de asentamiento contra el marco.
        const float bounceDegrees = 3f;
        const float bounceTime = 0.12f;
        t = 0f;
        while (t < bounceTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / bounceTime);
            SetSwing(_closeSwingDegrees + bounceDegrees * Mathf.Sin(k * Mathf.PI));
            yield return null;
        }
        SetSwing(_closeSwingDegrees);

        // 3) Cerrada: ahora sí, expulsar clientes y pantalla de fin de día.
        _closing = false;
        _closedForDay = true;
        if (!string.IsNullOrEmpty(_closeSfx))
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(_closeSfx);

        // Si el wind-down acabó solo mientras cerrábamos (se fueron todos),
        // no duplicar el cierre del día.
        if (Day != null && Day.IsWindingDown && !Day.IsDoorClosed)
            Day.CloseEntranceDoor();

        _closingRoutine = null;
    }

    private void SetSwing(float angle)
    {
        transform.rotation = _openRotation;
        if (Mathf.Abs(angle) > 0.01f)
            transform.RotateAround(_hingeWorld, Vector3.up, angle);
    }

    private void SetHighlight(bool on)
    {
        if (_renderer == null) return;
        if (_materialInstance == null)
        {
            _materialInstance = _renderer.material;
            _materialInstance.EnableKeyword("_EMISSION");
        }
        // Tinte dorado suave para que se note que la puerta "es la acción" al cerrar el día.
        _materialInstance.SetColor(EmissionColor, on ? new Color(0.55f, 0.42f, 0.05f) : Color.black);
    }
}
