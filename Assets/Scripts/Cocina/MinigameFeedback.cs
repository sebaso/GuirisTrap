using System.Collections;
using UnityEngine;
using TMPro;


public class MinigameFeedback : MonoBehaviour
{
    public static MinigameFeedback Instance { get; private set; }

    [Header("Overlay de resultado (opcional)")]
    [SerializeField] private GameObject _resultRoot;
    [SerializeField] private TMP_Text _resultText;

    [Header("Animación del overlay")]
    [SerializeField] private float _showSeconds = 0.9f;
    [SerializeField] private float _popScale = 1.35f;
    [SerializeField] private float _popDuration = 0.15f;

    [Header("Colores")]
    [SerializeField] private Color _successColor = new Color(0.20f, 0.85f, 0.25f);
    [SerializeField] private Color _failColor    = new Color(0.90f, 0.20f, 0.20f);

    private Coroutine _current;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(this); return; }

        if (_resultRoot != null) _resultRoot.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }



    /// <summary>
    /// Muestra el resultado de un minijuego. Estático a propósito: funciona
    /// aunque no haya ninguna instancia en la escena (SFX + HUDMessage).
    /// </summary>
    /// <param name="success">Si el minijuego se superó.</param>
    /// <param name="message">Texto a mostrar (p. ej. "¡Paella lista!" o "¡Se acabó el tiempo!").</param>
    /// <param name="sfxKey">Clave del SFX en el AudioManager (p. ej. "nevera_success").</param>
    public static void Show(bool success, string message, string sfxKey = null)
    {
        if (!string.IsNullOrEmpty(sfxKey))
            AudioManager.Instance?.PlaySFX(sfxKey);

        // Overlay configurado → cartel grande centralizado.
        if (Instance != null && Instance._resultRoot != null && Instance._resultText != null)
        {
            Instance.ShowOverlay(success, message);
            return;
        }

        // Fallback: HUDMessage (ya existe en el Canvas del juego).
        if (HUDMessage.Instance != null)
        {
            if (success) HUDMessage.Instance.ShowGood(message);
            else         HUDMessage.Instance.ShowBad(message);
        }
        else
        {
            Debug.Log($"[MinigameFeedback] ({(success ? "ÉXITO" : "FALLO")}) {message}");
        }
    }

    // ------------------------------------------------------------------
    //  Overlay
    // ------------------------------------------------------------------

    private void ShowOverlay(bool success, string message)
    {
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(OverlayRoutine(success, message));
    }

    private IEnumerator OverlayRoutine(bool success, string message)
    {
        _resultText.text  = message;
        _resultText.color = success ? _successColor : _failColor;

        _resultRoot.SetActive(true);
        Transform t = _resultRoot.transform;

        // Pop: de _popScale a 1.
        float elapsed = 0f;
        while (elapsed < _popDuration)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.Clamp01(elapsed / _popDuration);
            t.localScale = Vector3.one * Mathf.Lerp(_popScale, 1f, k);
            yield return null;
        }
        t.localScale = Vector3.one;

        yield return new WaitForSeconds(Mathf.Max(0f, _showSeconds - _popDuration));

        _resultRoot.SetActive(false);
        _current = null;
    }
}
