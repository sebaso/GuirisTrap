using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component that displays a circular countdown timer for the current day.
/// Attach to a Canvas GameObject with:
///   - An Image (filled, radial 360) for the circle timer
///   - A TMP_Text child called "TimerText" to show remaining seconds
/// </summary>
public class DayTimerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _circleFill;
    [SerializeField] private TMP_Text _timerText;

    [Header("Appearance")]
    [SerializeField] private Color _fullColor = Color.green;
    [SerializeField] private Color _midColor = Color.yellow;
    [SerializeField] private Color _lowColor = Color.red;
    [SerializeField] private float _lowThreshold = 0.3f; // 30% time remaining triggers red

    [Header("Pulse Effect")]
    [SerializeField] private bool _enablePulse = true;
    [SerializeField] private float _pulseSpeed = 4f;
    [SerializeField] private float _pulseMinScale = 0.9f;
    [SerializeField] private float _pulseMaxScale = 1.05f;

    private RectTransform _circleRect;
    private Vector3 _originalScale;

    void Awake()
    {
        if (_circleFill != null)
        {
            _circleFill.type = Image.Type.Filled;
            _circleFill.fillMethod = Image.FillMethod.Radial360;
            _circleFill.fillOrigin = (int)Image.Origin360.Top;
            _circleFill.fillClockwise = true;
            _circleRect = _circleFill.GetComponent<RectTransform>();
            _originalScale = _circleRect != null ? _circleRect.localScale : Vector3.one;
        }
    }

    void OnEnable()
    {
        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnDayProgress += UpdateTimerDisplay;
            DayManager.Instance.OnDayEnded += OnDayEnded;
            
            // Initialize display
            UpdateTimerDisplay(DayManager.Instance.DayProgress);
        }
    }

    void OnDisable()
    {
        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnDayProgress -= UpdateTimerDisplay;
            DayManager.Instance.OnDayEnded -= OnDayEnded;
        }
    }

    void Update()
    {
        // Pulse effect when time is low
        if (_enablePulse && _circleRect != null && DayManager.Instance != null && DayManager.Instance.IsDayActive)
        {
            float progress = DayManager.Instance.DayProgress;
            if (progress >= 1f - _lowThreshold) // last 30% of day
            {
                float pulse = Mathf.Lerp(_pulseMinScale, _pulseMaxScale, (Mathf.Sin(Time.time * _pulseSpeed) + 1f) * 0.5f);
                _circleRect.localScale = _originalScale * pulse;
            }
            else
            {
                _circleRect.localScale = _originalScale;
            }
        }
    }

    private void UpdateTimerDisplay(float progress)
    {
        if (_circleFill != null)
        {
            // Fill amount: 1 = full (day start), 0 = empty (day end)
            _circleFill.fillAmount = Mathf.Clamp01(1f - progress);

            // Color interpolation based on time remaining
            if (progress >= 1f - _lowThreshold)
            {
                // In the low time zone - interpolate between mid and red
                float t = (progress - (1f - _lowThreshold)) / _lowThreshold;
                _circleFill.color = Color.Lerp(_midColor, _lowColor, t);
            }
            else
            {
                // Plenty of time left - interpolate between green and yellow
                float t = progress / (1f - _lowThreshold);
                _circleFill.color = Color.Lerp(_fullColor, _midColor, t);
            }
        }

        if (_timerText != null && DayManager.Instance != null)
        {
            float remaining = DayManager.Instance.TimeRemaining;
            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);
            _timerText.text = $"{minutes}:{seconds:D2}";
        }
    }

    private void OnDayEnded()
    {
        if (_circleFill != null)
        {
            _circleFill.fillAmount = 0f;
            _circleFill.color = _lowColor;
            _circleRect.localScale = _originalScale;
        }

        if (_timerText != null)
        {
            _timerText.text = "0:00";
        }
    }
}