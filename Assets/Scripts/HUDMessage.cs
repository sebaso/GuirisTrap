using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Simple HUD message system. Shows one message at a time with a fade-in/fade-out
/// animation. Extra messages are queued and shown in order.
///
/// USO:
///   HUDMessage.Instance.Show("¡Clientes se han ido enfadados!");
///   HUDMessage.Instance.Show("Has servido el plato equivocado");
///
/// MONTAJE:
///   1. Crea un TMP_Text hijo de tu Canvas de juego.
///   2. Ponle el script HUDMessage al mismo GameObject.
///   3. Arrastra el TMP_Text al campo _messageText.
/// </summary>
public class HUDMessage : MonoBehaviour
{
    public static HUDMessage Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text _messageText;

    [Header("Animación")]
    [SerializeField] private float _fadeInDuration  = 0.25f;
    [SerializeField] private float _displayDuration = 2.5f;
    [SerializeField] private float _fadeOutDuration = 0.75f;

    [Header("Colores por tipo (opcional)")]
    [SerializeField] private Color _defaultColor = Color.white;
    [SerializeField] private Color _goodColor    = Color.green;
    [SerializeField] private Color _badColor     = Color.red;
    [SerializeField] private Color _warningColor = Color.yellow;

    private string _currentDisplayedText;
    private Queue<HUDMessageData> _queue = new Queue<HUDMessageData>();
    private Coroutine _currentCoroutine;

    private struct HUDMessageData
    {
        public string text;
        public Color  color;
    }

    private void Awake()
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

        if (_messageText != null)
            _messageText.text = "";
    }

    // ── Métodos públicos ───────────────────────────────────────────

    /// <summary>Muestra un mensaje blanco.</summary>
    public void Show(string message)
    {
        Enqueue(message, _defaultColor);
    }

    /// <summary>Muestra un mensaje con el color "bueno" (verde).</summary>
    public void ShowGood(string message)
    {
        Enqueue(message, _goodColor);
    }

    /// <summary>Muestra un mensaje con el color "malo" (rojo).</summary>
    public void ShowBad(string message)
    {
        Enqueue(message, _badColor);
    }

    /// <summary>Muestra un mensaje con el color "aviso" (amarillo).</summary>
    public void ShowWarning(string message)
    {
        Enqueue(message, _warningColor);
    }

    /// <summary>Muestra un mensaje con un color personalizado.</summary>
    public void Show(string message, Color color)
    {
        Enqueue(message, color);
    }

    // ── Interno ────────────────────────────────────────────────────

    private void Enqueue(string text, Color color)
    {
        // Si el mensaje actual en pantalla es el mismo, ignorar duplicado
        if (_currentCoroutine != null && _currentDisplayedText == text)
            return;

        // Si el último mensaje encolado ya es el mismo, ignorar duplicado
        if (_queue.Count > 0)
        {
            HUDMessageData last = _queue.ToArray()[_queue.Count - 1];
            if (last.text == text)
                return;
        }

        _queue.Enqueue(new HUDMessageData { text = text, color = color });
        if (_currentCoroutine == null)
            _currentCoroutine = StartCoroutine(PlayQueue());
    }

    private IEnumerator PlayQueue()
    {
        while (_queue.Count > 0)
        {
            HUDMessageData data = _queue.Dequeue();

            // Mostrar
            _currentDisplayedText = data.text;
            _messageText.text     = data.text;
            _messageText.color    = new Color(data.color.r, data.color.g, data.color.b, 0f);

            // Fade in
            yield return StartCoroutine(FadeAlpha(0f, 1f, _fadeInDuration));

            // Esperar
            yield return new WaitForSeconds(_displayDuration);

            // Fade out
            yield return StartCoroutine(FadeAlpha(1f, 0f, _fadeOutDuration));
        }

        _currentDisplayedText = null;
        _messageText.text     = "";
        _currentCoroutine     = null;
    }

    private IEnumerator FadeAlpha(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            Color c = _messageText.color;
            c.a = Mathf.Lerp(from, to, t);
            _messageText.color = c;
            yield return null;
        }
        Color final = _messageText.color;
        final.a = to;
        _messageText.color = final;
    }
}
