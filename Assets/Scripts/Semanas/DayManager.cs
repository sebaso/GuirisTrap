using UnityEngine;
using System;
using UnityEngine.InputSystem;


public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    [Header("Day Duration")]
    [SerializeField] private float _dayDurationSeconds = 120f;

    [Header("Arranque")]
    [SerializeField] private bool _autoStart = true;
    [SerializeField] private float _startDelay = 0.5f;

    [Header("Cierre")]
    [Tooltip("Tecla para cerrar el día sin esperar a que se vayan todos los clientes.")]
    [SerializeField] private Key _forceEndDayKey = Key.F10;

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



    void Start()
    {
        if (_autoStart)
            Invoke(nameof(StartDay), _startDelay);
    }

    void Update()
    {
        if (IsWindingDown)
        {
            if (Client.ActiveCount == 0 || ForceEndPressed())
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

    private void StartWindDown()
    {
        IsWindingDown = true;
        if (Client.ActiveCount > 0)
            HUDMessage.Instance?.ShowWarning(
                $"Fin del servicio — esperando a que los clientes terminen ({_forceEndDayKey} para cerrar ya)");
    }

    // WeekManager.OnDayCompleted debe correr ANTES de OnDayEnded (el StatsPanel
    // lee el resultado semanal al mostrarse); así lo cierra el día entero,
    // incluyendo lo que ocurra durante el wind-down.
    private void FinishDay()
    {
        IsWindingDown = false;
        WeekManager.Instance?.OnDayCompleted();

        OnDayEnded?.Invoke();
        HandleDayEnd();
    }

    /// <summary>Cierra el día inmediatamente (tecla de forzado o botón de UI).</summary>
    public void ForceEndDay()
    {
        if (IsWindingDown) FinishDay();
    }

    [ContextMenu("Forzar fin del día")]
    private void DebugForceEndDay() => ForceEndDay();

    private bool ForceEndPressed()
        => Keyboard.current != null && Keyboard.current[_forceEndDayKey].wasPressedThisFrame;

    /// <summary>Start (or restart) the day timer.</summary>
    public void StartDay()
    {
        _timeRemaining = _dayDurationSeconds;
        _isDayActive = true;
        IsWindingDown = false;
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
