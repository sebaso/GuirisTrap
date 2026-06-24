using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class DayTimerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _circleFill;
    [SerializeField] private TMP_Text _timerText;

    [Header("Appearance")]
    [SerializeField] private Color _fullColor = Color.green;
    [SerializeField] private Color _midColor = Color.yellow;
    [SerializeField] private Color _lowColor = Color.red;
    [SerializeField] private float _lowThreshold = 0.3f; 

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

    private bool _subscribed = false;

    void OnEnable()
    {
        TrySubscribe();
    }

    void OnDisable()
    {
        if (_subscribed && DayManager.Instance != null)
        {
            DayManager.Instance.OnDayProgress -= UpdateTimerDisplay;
            DayManager.Instance.OnDayEnded -= OnDayEnded;
        }
        _subscribed = false;
    }

    private void TrySubscribe()
    {
        if (_subscribed || DayManager.Instance == null) return;

        DayManager.Instance.OnDayProgress += UpdateTimerDisplay;
        DayManager.Instance.OnDayEnded += OnDayEnded;
        _subscribed = true;

        // Inicializar display con el estado actual
        UpdateTimerDisplay(DayManager.Instance.DayProgress);
    }

    void Update()
    {
        if (!_subscribed)
        {
            TrySubscribe();
        }

        if (DayManager.Instance != null && DayManager.Instance.IsDayActive)
        {
            UpdateTimerDisplay(DayManager.Instance.DayProgress);
        }

        // Pulse effect when time is low
        if (_enablePulse && _circleRect != null && DayManager.Instance != null && DayManager.Instance.IsDayActive)
        {
            float progress = DayManager.Instance.DayProgress;
            if (progress >= 1f - _lowThreshold) // last 30% of day
            {
                AudioManager.Instance?.PlaySFX("timer_low");
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
            _circleFill.fillAmount = Mathf.Clamp01(1f - progress);

            if (progress >= 1f - _lowThreshold)
            {
                float t = (progress - (1f - _lowThreshold)) / _lowThreshold;
                _circleFill.color = Color.Lerp(_midColor, _lowColor, t);
            }
            else
            {
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