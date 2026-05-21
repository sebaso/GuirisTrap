using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD element that reflects the CashManager balance.
/// Attach to a Canvas GameObject that has:
///   - a TMP_Text child called "MoneyText"  (shows current balance)
///   - a TMP_Text child called "PopupText"  (floating +/- popup)
/// 
/// The popup prefab/child should start with alpha 0 and be positioned
/// above the balance label; this script animates it on every change.
/// </summary>
public class CashUI : MonoBehaviour
{
    [Header("Balance Display")]
    [SerializeField] private TMP_Text moneyText;

    [Header("Earn / Spend Popup")]
    [SerializeField] private TMP_Text popupText;
    [SerializeField] private float popupRisePx   = 40f;
    [SerializeField] private float popupDuration  = 1.1f;

    [Header("Colors")]
    [SerializeField] private Color earnColor  = new Color(0.18f, 0.85f, 0.45f);
    [SerializeField] private Color spendColor = new Color(0.95f, 0.25f, 0.25f);
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

    // Called by CashManager: (newBalance, delta)
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

        // Restart animation
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

            // Rise linearly, fade out in the second half
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
