using System.Collections;
using UnityEngine;
using TMPro;

public class CashUI : MonoBehaviour
{
    [Header("Balance Display")]
    [SerializeField] private TMP_Text moneyText;

    [Header("Earn / Spend Popup")]
    [SerializeField] private TMP_Text popupText;
    [SerializeField] private float popupRisePx   = 40f;
    [SerializeField] private float popupDuration  = 1.1f;

    [Header("Colors")]
    [SerializeField] private Color earnColor  = Color.green;
    [SerializeField] private Color spendColor = Color.red;
    [SerializeField] private Color neutralColor = Color.white;

    private RectTransform _popupRect;
    private Vector2       _popupStartAnchoredPos;
    private Coroutine     _popupCoroutine;

    void Awake()
    {
        if (popupText != null)
        {
            _popupRect = popupText.GetComponent<RectTransform>();
            _popupStartAnchoredPos = _popupRect.anchoredPosition;
            SetPopupAlpha(0f);
        }
    }

    void OnEnable()
    {
        if (CashManager.Instance != null)
        {
            RefreshDisplay(CashManager.Instance.Money);
            CashManager.Instance.OnMoneyChanged += HandleMoneyChanged;
        }
    }

    void OnDisable()
    {
        if (CashManager.Instance != null)
            CashManager.Instance.OnMoneyChanged -= HandleMoneyChanged;
    }

    private void HandleMoneyChanged(int newBalance, int delta)
    {
        RefreshDisplay(newBalance);
        ShowPopup(delta);
    }

    private void RefreshDisplay(int balance)
    {
        if (moneyText == null) return;
        moneyText.text = $"{balance}€";
    }

    private void ShowPopup(int delta)
    {
        if (popupText == null) return;

        // Format text
        if (delta > 0)
        {
            popupText.text  = $"+{delta}€";
            popupText.color = earnColor;
        }
        else
        {
            popupText.text  = $"{delta}€";
            popupText.color = spendColor;
        }

        if (_popupCoroutine != null) StopCoroutine(_popupCoroutine);
        _popupCoroutine = StartCoroutine(AnimatePopup());
    }

    private IEnumerator AnimatePopup()
    {
        _popupRect.anchoredPosition = _popupStartAnchoredPos;
        SetPopupAlpha(1f);

        float elapsed = 0f;
        Vector2 startPos = _popupStartAnchoredPos;
        Vector2 endPos   = startPos + Vector2.up * popupRisePx;

        while (elapsed < popupDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popupDuration;

            _popupRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            float alpha = t < 0.5f ? 1f : 1f - ((t - 0.5f) / 0.5f);
            SetPopupAlpha(alpha);

            yield return null;
        }

        SetPopupAlpha(0f);
        _popupRect.anchoredPosition = _popupStartAnchoredPos;
    }

    private void SetPopupAlpha(float a)
    {
        if (popupText == null) return;
        Color c = popupText.color;
        c.a = a;
        popupText.color = c;
    }
}
