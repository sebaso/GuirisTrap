using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Mensajes del HUD apilados: cada mensaje nuevo aparece con un "plop" arriba
/// del todo y empuja a los anteriores hacia abajo, que van perdiendo alpha
/// hasta desaparecer. Así un mensaje importante nunca espera en cola detrás
/// de otro menos importante.
/// </summary>
public class HUDMessage : MonoBehaviour
{
    public static HUDMessage Instance { get; private set; }

    [Header("UI")]
    [Tooltip("Texto de la versión antigua (un solo mensaje fijo). Solo se usa como plantilla de fuente y se desactiva al arrancar.")]
    [SerializeField] private TMP_Text _messageText;

    [Header("Animación")]
    [SerializeField] private float _fadeInDuration  = 0.25f;
    [SerializeField] private float _displayDuration = 2.5f;
    [SerializeField] private float _fadeOutDuration = 0.75f;
    [Tooltip("Duración del pop de aparición (se evalúa sobre la curva).")]
    [SerializeField] private float _popDuration = 0.3f;
    [Tooltip("Escala del mensaje durante el pop: 0 → rebasa → se asienta en 1.")]
    [SerializeField] private AnimationCurve _popCurve = new AnimationCurve(
        new Keyframe(0f,    0f),
        new Keyframe(0.4f,  1.18f),
        new Keyframe(0.75f, 0.94f),
        new Keyframe(1f,    1f));
    [Tooltip("Suavidad al deslizarse al ser empujado (mayor = más rápido).")]
    [SerializeField] private float _slideSmoothing = 14f;

    [Header("Caja de mensajes")]
    [SerializeField] private float _messageWidth = 640f;
    [SerializeField] private float _padding = 14f;
    [SerializeField] private float _spacing = 8f;
    [Tooltip("Tamaño de fuente si no hay texto plantilla asignado.")]
    [SerializeField] private float _fontSize = 26f;
    [SerializeField] private Color _backgroundColor = new Color(0f, 0f, 0f, 0.55f);
    [Tooltip("Alpha que pierde un mensaje por cada posición que baja en la pila.")]
    [SerializeField] private float _alphaStepPerDepth = 0.45f;
    [Tooltip("Máximo de mensajes vivos a la vez (el más viejo se descarta).")]
    [SerializeField] private int _maxVisibleMessages = 4;

    [Header("Colores por tipo (opcional)")]
    [SerializeField] private Color _defaultColor = Color.white;
    [SerializeField] private Color _goodColor    = Color.green;
    [SerializeField] private Color _badColor     = Color.red;
    [SerializeField] private Color _warningColor = Color.yellow;

    private sealed class HUDMessageItem
    {
        public string Text;
        public RectTransform Rect;
        public CanvasGroup Group;
        public float Height;       // altura de la caja, para apilar
        public float Age;          // segundos en pantalla
        public float Alpha;        // alpha actual
        public float Y;            // posición vertical actual
        public float TargetY;      // posición vertical objetivo
        public float PopElapsed;   // tiempo de pop transcurrido
    }

    private readonly List<HUDMessageItem> _items = new List<HUDMessageItem>(); // índice 0 = más nuevo (arriba)

    private bool _templateReady;
    private TMP_FontAsset _templateFont;
    private float _templateFontSize = 26f;
    private TextAlignmentOptions _templateAlignment = TextAlignmentOptions.Center;

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

        EnsureTemplate();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Métodos públicos ───────────────────────────────────────────

    /// <summary>Muestra un mensaje blanco.</summary>
    public void Show(string message)
    {
        Spawn(message, _defaultColor);
    }

    /// <summary>Muestra un mensaje con el color "bueno" (verde).</summary>
    public void ShowGood(string message)
    {
        Spawn(message, _goodColor);
    }

    /// <summary>Muestra un mensaje con el color "malo" (rojo).</summary>
    public void ShowBad(string message)
    {
        Spawn(message, _badColor);
    }

    /// <summary>Muestra un mensaje con el color "aviso" (amarillo).</summary>
    public void ShowWarning(string message)
    {
        Spawn(message, _warningColor);
    }

    /// <summary>Muestra un mensaje con un color personalizado.</summary>
    public void Show(string message, Color color)
    {
        Spawn(message, color);
    }

    // ── Interno ────────────────────────────────────────────────────

    private void EnsureTemplate()
    {
        if (_templateReady) return;
        _templateReady = true;

        if (_messageText != null)
        {
            // El TMP que había en escena pintaba un único mensaje fijo;
            // ahora solo sirve para heredar fuente, tamaño y alineación.
            _templateFont      = _messageText.font;
            _templateFontSize  = _messageText.fontSize;
            _templateAlignment = _messageText.alignment;
            _messageText.text    = string.Empty;
            _messageText.enabled = false;
        }
        else
        {
            _templateFontSize = _fontSize;
        }
    }

    private void Spawn(string text, Color color)
    {
        if (string.IsNullOrEmpty(text)) return;

        // Anti-spam: ignora duplicados que aún estén vivos en pantalla.
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].Age < _displayDuration && _items[i].Text == text)
                return;
        }

        EnsureTemplate();

        HUDMessageItem item = CreateItem(text, color);
        _items.Insert(0, item);

        // Límite duro: descarta por abajo (los más viejos ya casi ni se ven).
        while (_items.Count > Mathf.Max(1, _maxVisibleMessages))
        {
            HUDMessageItem oldest = _items[_items.Count - 1];
            _items.RemoveAt(_items.Count - 1);
            Destroy(oldest.Rect.gameObject);
        }
    }

    private HUDMessageItem CreateItem(string text, Color color)
    {
        GameObject boxGo = new GameObject("Message", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        boxGo.transform.SetParent(transform, false);

        // Cuelga del borde superior del contenedor y crece hacia abajo.
        RectTransform boxRect = (RectTransform)boxGo.transform;
        boxRect.anchorMin = new Vector2(0.5f, 1f);
        boxRect.anchorMax = new Vector2(0.5f, 1f);
        boxRect.pivot     = new Vector2(0.5f, 1f);
        boxRect.anchoredPosition = Vector2.zero;
        boxRect.localScale       = Vector3.zero;

        Image background = boxGo.GetComponent<Image>();
        background.color = _backgroundColor;
        background.raycastTarget = false;

        CanvasGroup group = boxGo.GetComponent<CanvasGroup>();
        group.interactable   = false;
        group.blocksRaycasts = false;
        group.alpha = 0f;

        float textWidth = _messageWidth - 2f * _padding;

        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(boxGo.transform, false);
        RectTransform textRect = (RectTransform)textGo.transform;
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot     = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(textWidth, 0f);

        TextMeshProUGUI label = textGo.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.color = color;
        if (_templateFont != null) label.font = _templateFont;
        label.fontSize = _templateFontSize;
        label.alignment = _templateAlignment;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode     = TextOverflowModes.Overflow;
        label.raycastTarget = false;

        // Medir la altura real con el ancho ya fijado, para que la caja
        // abrace al texto y TMP no parta líneas de más.
        label.ForceMeshUpdate(false, true);
        float textHeight = Mathf.Max(label.preferredHeight, _templateFontSize);
        textRect.sizeDelta = new Vector2(textWidth, textHeight);

        float height = textHeight + 2f * _padding;
        boxRect.sizeDelta = new Vector2(_messageWidth, height);

        return new HUDMessageItem
        {
            Text = text,
            Rect = boxRect,
            Group = group,
            Height = height,
            Alpha = 0f,
            Y = 0f,
            TargetY = 0f,
            PopElapsed = 0f
        };
    }

    private void Update()
    {
        if (_items.Count == 0) return;
        float dt = Time.deltaTime;

        // Objetivo vertical: cada mensaje se coloca debajo de todos los que tiene encima.
        float stackDepth = 0f;
        for (int i = 0; i < _items.Count; i++)
        {
            _items[i].TargetY = -stackDepth;
            stackDepth += _items[i].Height + _spacing;
        }

        for (int i = _items.Count - 1; i >= 0; i--)
        {
            HUDMessageItem item = _items[i];
            item.Age += dt;

            // Caduca solo, y además se va desvaneciendo según lo empujan hacia abajo.
            bool expired = item.Age >= _displayDuration;
            float targetAlpha = expired ? 0f : Mathf.Clamp01(1f - i * _alphaStepPerDepth);

            // Aparecer es rápido; desvanecer usa el fade-out.
            float rate = targetAlpha > item.Alpha ? _fadeInDuration : _fadeOutDuration;
            item.Alpha = Mathf.MoveTowards(item.Alpha, targetAlpha, dt / Mathf.Max(0.01f, rate));

            // Deslizamiento exponencial, independiente del framerate.
            float blend = 1f - Mathf.Exp(-_slideSmoothing * dt);
            item.Y = Mathf.Lerp(item.Y, item.TargetY, blend);

            if (item.PopElapsed < _popDuration)
            {
                item.PopElapsed += dt;
                float s = _popCurve.Evaluate(Mathf.Clamp01(item.PopElapsed / Mathf.Max(0.01f, _popDuration)));
                item.Rect.localScale = new Vector3(s, s, 1f);
            }

            item.Rect.anchoredPosition = new Vector2(0f, item.Y);
            item.Group.alpha = item.Alpha;

            // Fuera del todo: liberar el hueco para que el resto suba.
            if (item.Alpha <= 0.001f && targetAlpha <= 0f)
            {
                _items.RemoveAt(i);
                Destroy(item.Rect.gameObject);
            }
        }
    }
}
