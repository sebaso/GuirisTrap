using UnityEngine;
using System;

/// <summary>
/// Manages the day timer for the GameScene.
/// When the day ends, it triggers a transition back to the PreparationScene.
/// </summary>
public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    [Header("Day Duration")]
    [SerializeField] private float _dayDurationSeconds = 120f; // 2 minutes default

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
        StartDay();
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
        OnDayProgress?.Invoke(0f);
        Debug.Log($"[DayManager] Day started! Duration: {_dayDurationSeconds}s");
    }

    private void HandleDayEnd()
    {
        Debug.Log("[DayManager] Day ended! Returning to PreparationScene...");

        if (SaveManager.Instance != null)
            SaveManager.Instance.IncrementDayAndSave();

        if (SceneController.Instance != null)
        {
            SceneController.Instance.ChangeScene("PreparationScene");
        }
        else
        {
            Debug.LogError("[DayManager] SceneController instance not found!");
        }
    }

    /// <summary>Override the day duration (can be called before StartDay).</summary>
    public void SetDayDuration(float seconds)
    {
        _dayDurationSeconds = Mathf.Max(1f, seconds);
    }
}