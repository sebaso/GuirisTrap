using UnityEngine;
using System;


public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    [Header("Day Duration")]
    [SerializeField] private float _dayDurationSeconds = 120f;

    [Header("Arranque")]
    [SerializeField] private bool _autoStart = true;
    [SerializeField] private float _startDelay = 0.5f;

    // Fin del día: cuando el timer llega a cero se entra en wind-down (ya no
    // entran clientes). El jugador cierra entonces la PUERTA DE ENTRADA
    // (PuertaFinDia, pulsando E junto a ella) para expulsar a los clientes que
    // queden y mostrar la pantalla de fin de día; si prefiere esperar, el día
    // se cierra solo cuando todos se van.

    private float _timeRemaining;
    private bool _isDayActive;

    /// <summary>Servicio terminado: ya no entran clientes, se espera a que se vayan los que quedan.</summary>
    public bool IsWindingDown { get; private set; }

    /// <summary>Time remaining in the current day (0 to _dayDurationSeconds).</summary>
    public float TimeRemaining => _timeRemaining;

    /// <summary>Total duration of the day in seconds.</summary>
    public float DayDuration => _dayDurationSeconds;

    /// <summary>Normalized progress (0 = day just started, 1 = day ended).</summary>
    public float DayProgress => Mathf.Clamp01(1f - (_timeRemaining / _dayDurationSeconds));

    /// <summary>Whether the day is currently running.</summary>
    public bool IsDayActive => _isDayActive;

    /// <summary>La puerta de entrada está cerrada (interacción con PuertaFinDia).</summary>
    public bool IsDoorClosed { get; private set; }

    /// <summary>Fired every frame with the normalized progress (0→1).</summary>
    public event Action<float> OnDayProgress;

    /// <summary>Fired when the day ends (timer reaches zero).</summary>
    public event Action OnDayEnded;

    /// <summary>Fired when a new day starts (timer (re)started).</summary>
    public event Action OnDayStarted;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }



    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    void Start()
    {
        // Un timeScale 0 arrastrado de la sesión anterior (p.ej. salir de play
        // con el panel de stats abierto, que pone timeScale a 0) congelaba el
        // Invoke de StartDay —que va en tiempo ESCALADO— y el spawner: el día
        // no arrancaba nunca. GameScene carga siempre con el tiempo en marcha.
        Time.timeScale = 1f;

        if (_autoStart)
            Invoke(nameof(StartDay), _startDelay);
    }

    void Update()
    {
        if (IsWindingDown)
        {
            if (Client.ActiveCount == 0)
                FinishDay();
            return;
        }

        if (!_isDayActive) return;

        _timeRemaining -= Time.deltaTime;

        if (_timeRemaining <= 0f)
        {
            _timeRemaining = 0f;
            _isDayActive = false;
            OnDayProgress?.Invoke(1f);
            StartWindDown();
            return;
        }

        OnDayProgress?.Invoke(DayProgress);
    }

    // internal: los tests lo usan para llegar al cierre sin esperar el timer
    internal void StartWindDown()
    {
        IsWindingDown = true;
        if (Client.ActiveCount > 0)
            HUDMessage.Instance?.ShowWarning(
                "Fin del servicio — ¡cierra la puerta! (acércate a la puerta y pulsa E)");
    }

    // WeekManager.OnDayCompleted debe correr ANTES de OnDayEnded (el StatsPanel
    // lee el resultado semanal al mostrarse); así lo cierra el día entero,
    // incluyendo lo que ocurra durante el wind-down.
    private void FinishDay()
    {
        IsWindingDown = false;
        // Día cerrado del todo: el timer no debe seguir corriendo ni relanzar el
        // wind-down detrás del panel de resultados (importante si el cierre se pide
        // con el día aún activo).
        _isDayActive = false;

        // Si el cierre del día explota, el día queda pegado para siempre
        // (IsWindingDown ya es false): el panel tiene que salir SIEMPRE.
        try
        {
            WeekManager.Instance?.OnDayCompleted();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DayManager] OnDayCompleted falló, se cierra el día igualmente: {e}");
        }

        OnDayEnded?.Invoke();
        HandleDayEnd();
    }

    /// <summary>Cierra la puerta de entrada, expulsa a los clientes que queden y
    /// muestra la pantalla de fin de día. La llama PuertaFinDia cuando termina
    /// su animación de cierre (la puerta se ve cerrarse ANTES de llegar aquí).</summary>
    public void CloseEntranceDoor()
    {
        if (IsDoorClosed) return;
        IsDoorClosed = true;

        HUDMessage.Instance?.ShowWarning("¡Cierras la puerta! Los clientes son expulsados");

        // Expulsar a los clientes: los de la cola salen sin penalizar.
        if (RestaurantManager.Instance != null)
            RestaurantManager.Instance.KickAllClients();
        else
            Client.KickAll();

        // Y se cierra el día (stats + panel de fin de día).
        FinishDay();
    }

    /// <summary>Cierra el día inmediatamente cerrando la puerta de entrada.</summary>
    public void ForceEndDay() => CloseEntranceDoor();

    [ContextMenu("DEBUG: Terminar el día ya")]
    private void DebugEndDayNow()
    {
        // Fin instantáneo desde cualquier punto del día: corta el timer y
        // lanza el cierre (stats + panel) sin esperar al wind-down.
        _timeRemaining = 0f;
        _isDayActive = false;
        FinishDay();
    }

    /// <summary>Start (or restart) the day timer.</summary>
    public void StartDay()
    {
        _timeRemaining = _dayDurationSeconds;
        _isDayActive = true;
        IsWindingDown = false;
        IsDoorClosed = false;
        OnDayStarted?.Invoke();
        OnDayProgress?.Invoke(0f);
        Debug.Log($"[DayManager] Day started! Duration: {_dayDurationSeconds}s");
    }

    private void HandleDayEnd()
    {
        Debug.Log("[DayManager] Día terminado. Mostrando pantalla de Stats...");
    }

    /// <summary>Override the day duration (can be called before StartDay).</summary>
    public void SetDayDuration(float seconds)
    {
        _dayDurationSeconds = Mathf.Max(1f, seconds);
    }
}