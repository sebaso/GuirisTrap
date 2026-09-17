using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla un popup reutilizable (fondo + texto + toggle de cierre).
/// Requiere un CanvasGroup en el mismo GameObject para el fade de salida.
/// </summary>
public class PopupController : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _text;
    [SerializeField] private Toggle _toggle;
    [SerializeField] private float _fadeDuration = 0.3f;

    private Action _onClosed;
    private bool _closing;

    private void Awake()
    {
        if (_toggle != null)
            _toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    private void OnDestroy()
    {
        if (_toggle != null)
            _toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
    }

    /// <summary>
    /// Activa el popup mostrando el texto indicado. onClosed se invoca cuando
    /// el jugador marca el toggle y termina el fade de salida.
    /// </summary>
    public void Show(string text, Action onClosed)
    {
        _onClosed = onClosed;
        _closing = false;

        if (_text != null)
            _text.text = text;

        if (_toggle != null)
            _toggle.SetIsOnWithoutNotify(false);

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

        gameObject.SetActive(true);
    }

    private void OnToggleValueChanged(bool isOn)
    {
        if (!isOn || _closing) return;
        _closing = true;
        CoroutineRunner.Instance.StartCoroutine(FadeOutAndClose());
    }

    private IEnumerator FadeOutAndClose()
    {
        float elapsed = 0f;
        float startAlpha = _canvasGroup != null ? _canvasGroup.alpha : 1f;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            if (_canvasGroup != null)
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / _fadeDuration);
            yield return null;
        }

        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;

        gameObject.SetActive(false);

        var callback = _onClosed;
        _onClosed = null;
        callback?.Invoke();
    }
}