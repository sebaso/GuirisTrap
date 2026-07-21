using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// World-space "order bubble" floating above a table, showing what the seated
/// group has ordered (text-only for now; <see cref="RecipeData.icon"/> is ready
/// for art later). Mirrors <see cref="PatienceBar"/>: the component lives on the
/// Table root and drives a separate <see cref="bubbleRoot"/> Transform, which it
/// billboards toward the camera and toggles on/off. Do NOT billboard the table
/// itself.
/// </summary>
public class OrderBubble : MonoBehaviour
{
    [Tooltip("Child Transform of the table that holds the bubble visuals. Toggled " +
             "on/off and billboarded toward the camera. Must NOT be the Table root. " +
             "Leave null to auto-build a default bubble in code.")]
    public Transform bubbleRoot;

    [Tooltip("Text that shows the table number and the ordered dishes. " +
             "Leave null to auto-build (uses the default bubble).")]
    public TMP_Text orderText;

    private Table _table;
    private Camera _cam;

    void Awake()
    {
        _table = GetComponentInParent<Table>();
        _cam = Camera.main;

        // If nothing is wired, build a minimal world-space bubble in code so the
        // feature works by just adding this component (no prefab child needed).
        if (bubbleRoot == null)
            BuildDefaultBubble();

        if (bubbleRoot != null) bubbleRoot.gameObject.SetActive(false);
    }

    /// <summary>Builds a self-contained world-space bubble: a Canvas (World) →
    /// background Image → TMP_Text, parented to this Table. Rendered text needs
    /// a World-space Canvas ancestor, which the existing client/table UI lacks
    /// (a latent bug); this guarantees one.
    ///
    /// Sizing note: in a World-space canvas the rect is in PIXELS, and the on-
    /// world size comes from a uniform localScale. We pick a generous pixel size
    /// and a small scale so the text (also sized in px) renders at a readable
    /// on-world height instead of overflowing.</summary>
    private void BuildDefaultBubble()
    {
        if (_table == null) return;

        const float widthPx = 260f;
        const float heightPx = 130f;
        // On-world size (meters) ≈ pixelSize * localScale. 260px * 0.005 ≈ 1.3m wide.
        const float worldScale = 0.005f;

        // Root: RectTransform + Canvas (World Space) + CanvasScaler.
        var rootGo = new GameObject("OrderBubbleRoot", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        rootGo.transform.SetParent(_table.transform, false);
        rootGo.transform.localPosition = new Vector3(0f, 1.2f, 0f); // just above the table top
        var rootRt = (RectTransform)rootGo.transform;
        rootRt.sizeDelta = new Vector2(widthPx, heightPx);
        rootRt.localScale = new Vector3(worldScale, worldScale, worldScale);

        var canvas = rootGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = _cam;
        var scaler = rootGo.GetComponent<CanvasScaler>();
        // For World-space, leave the scaler at Constant Pixel Size (default) — the
        // rect is already in pixels and we control world size via localScale above.
        scaler.dynamicPixelsPerUnit = 10f; // crisp text when the small canvas is viewed up close

        // Background image (fills the canvas).
        var bgGo = new GameObject("BG", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(rootRt, false);
        var bgRt = (RectTransform)bgGo.transform;
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
        var bg = bgGo.GetComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);

        // Text (fills the canvas with a small margin).
        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(rootRt, false);
        var textRt = (RectTransform)textGo.transform;
        textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(14f, 10f);   // left, bottom padding in px
        textRt.offsetMax = new Vector2(-14f, -10f); // right, top padding in px
        orderText = textGo.GetComponent<TextMeshProUGUI>();
        orderText.enableWordWrapping = true;
        orderText.alignment = TextAlignmentOptions.Center;
        orderText.fontSize = 28f;
        orderText.richText = true;
        orderText.color = Color.white;

        bubbleRoot = rootRt;
    }

    void Update()
    {
        if (bubbleRoot == null || _table == null) return;

        ClientGroup g = _table.OccupyingGroup;
        bool show = g != null && g.IsWaitingForFood && !g.AllFed && g.Order != null && g.Order.Count > 0;

        bubbleRoot.gameObject.SetActive(show);
        if (!show) return;

        // Billboard toward the camera (same technique as PatienceBar).
        if (_cam != null)
            bubbleRoot.rotation = _cam.transform.rotation;

        if (orderText != null)
            orderText.text = FormatOrder(g);
    }

    private string FormatOrder(ClientGroup g)
    {
        // Group duplicate dishes: "Paella x2" reads better than two lines.
        var counts = new Dictionary<string, int>();
        foreach (var r in g.Order)
        {
            if (r == null) continue;
            string name = r.dishName;
            counts[name] = counts.TryGetValue(name, out int c) ? c + 1 : 1;
        }

        var lines = new List<string>(counts.Count);
        foreach (var kv in counts)
            lines.Add(kv.Value > 1 ? $"{kv.Key} x{kv.Value}" : kv.Key);

        return $"Mesa {_table.tableNumber}\n{string.Join("\n", lines)}";
    }
}
