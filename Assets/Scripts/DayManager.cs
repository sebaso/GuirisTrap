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

    private float _timeRemaining;
    private bool _isDayActive;

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
        if (!_isDayActive) return;

        _timeRemaining -= Time.deltaTime;

        if (_timeRemaining <= 0f)
        {
            _timeRemaining = 0f;
            _isDayActive = false;
            OnDayProgress?.Invoke(1f);
            OnDayEnded?.Invoke();
            HandleDayEnd();
            return;
        }

        OnDayProgress?.Invoke(DayProgress);
    }

    /// <summary>Start (or restart) the day timer.</summary>
    public void StartDay()
    {
        _timeRemaining = _dayDurationSeconds;
        _isDayActive = true;
        OnDayStarted?.Invoke();
        OnDayProgress?.Invoke(0f);
        Debug.Log($"[DayManager] Day started! Duration: {_dayDurationSeconds}s");
    }

    private void HandleDayEnd()
    {
        // El día ha terminado. Ya NO guardamos ni cambiamos de escena aquí:
        // eso lo hace el botón "Siguiente día" de la pantalla de Stats, después
        // de que el jugador haya visto el resumen (StatsPanel escucha OnDayEnded).
        Debug.Log("[DayManager] Día terminado. Mostrando pantalla de Stats...");
    }

    /// <summary>Override the day duration (can be called before StartDay).</summary>
    public void SetDayDuration(float seconds)
    {
        _dayDurationSeconds = Mathf.Max(1f, seconds);
    }
}