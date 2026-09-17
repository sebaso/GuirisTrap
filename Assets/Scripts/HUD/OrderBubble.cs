using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space "order bubble" floating above a table, showing what the seated
/// group has ordered: text (dish + cooking station) plus a row of dish icons
/// (<see cref="RecipeData.icon"/>). Mirrors <see cref="PatienceBar"/>: the
/// component lives on the Table root and drives a separate <see cref="bubbleRoot"/>
/// Transform, which it billboards toward the camera and toggles on/off. Do NOT
/// billboard the table itself.
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

    [Tooltip("Horizontal container inside the bubble where one icon Image per " +
             "distinct ordered dish is created. Leave null to use the auto-built " +
             "row of the default bubble; wired bubbles without it stay text-only.")]
    public Transform iconsRow;

    [Tooltip("Icon size in canvas pixels inside the auto-built icon row.")]
    public float iconSize = 54f;

    private Table _table;
    private Camera _cam;

    private readonly List<Image> _iconImages = new();
    private readonly List<RecipeData> _distinctDishes = new();

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
    /// background Image → TMP_Text + icon row, parented to this Table. Rendered
    /// text needs a World-space Canvas ancestor, which the existing client/table
    /// UI lacks (a latent bug); this guarantees one.
    ///
    /// Sizing note: in a World-space canvas the rect is in PIXELS, and the on-
    /// world size comes from a uniform localScale. We pick a generous pixel size
    /// and a small scale so the text (also sized in px) renders at a readable
    /// on-world height instead of overflowing.</summary>
    private void BuildDefaultBubble()
    {
        if (_table == null) return;

        const float widthPx = 260f;
        const float heightPx = 230f;
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

        // Text (upper area, above the icon strip).
        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(rootRt, false);
        var textRt = (RectTransform)textGo.transform;
        textRt.anchorMin = new Vector2(0f, 1f); textRt.anchorMax = new Vector2(1f, 1f);
        textRt.pivot = new Vector2(0.5f, 1f);
        textRt.offsetMin = new Vector2(14f, -160f);  // left + height of the text area
        textRt.offsetMax = new Vector2(-14f, -8f);   // right + top padding
        orderText = textGo.GetComponent<TextMeshProUGUI>();
        orderText.textWrappingMode = TextWrappingModes.Normal;
        orderText.alignment = TextAlignmentOptions.Center;
        orderText.fontSize = 28f;
        orderText.richText = true;
        orderText.color = Color.white;

        // Icon strip (bottom): one icon per distinct dish, laid out centered.
        var rowGo = new GameObject("Iconos", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowGo.transform.SetParent(rootRt, false);
        var rowRt = (RectTransform)rowGo.transform;
        rowRt.anchorMin = new Vector2(0f, 0f); rowRt.anchorMax = new Vector2(1f, 0f);
        rowRt.pivot = new Vector2(0.5f, 0f);
        rowRt.anchoredPosition = new Vector2(0f, 6f);
        rowRt.sizeDelta = new Vector2(0f, iconSize + 4f);
        var rowLayout = rowGo.GetComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.spacing = 10f;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = false;
        iconsRow = rowRt;

        bubbleRoot = rootRt;
    }

    /// <summary>One icon per distinct dish in the order, created/destroyed and
    /// (re)sprited to match. Dishes without art just hide their icon.</summary>
    private void SyncIconsRow(ClientGroup g)
    {
        if (iconsRow == null) return;

        _distinctDishes.Clear();
        if (g != null && g.Order != null)
            foreach (var r in g.Order)
                if (r != null && !_distinctDishes.Contains(r))
                    _distinctDishes.Add(r);

        while (_iconImages.Count < _distinctDishes.Count)
        {
            var iconGo = new GameObject("Icono" + _iconImages.Count, typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(iconsRow, false);
            var iconRt = (RectTransform)iconGo.transform;
            iconRt.sizeDelta = new Vector2(iconSize, iconSize);
            var img = iconGo.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            _iconImages.Add(img);
        }
        while (_iconImages.Count > _distinctDishes.Count)
        {
            int last = _iconImages.Count - 1;
            if (_iconImages[last] != null) Destroy(_iconImages[last].gameObject);
            _iconImages.RemoveAt(last);
        }

        for (int i = 0; i < _distinctDishes.Count; i++)
        {
            Sprite sp = _distinctDishes[i].icon;
            _iconImages[i].sprite = sp;
            _iconImages[i].enabled = sp != null;
        }
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

        SyncIconsRow(g);
    }

    private string FormatOrder(ClientGroup g)
    {
        // Group duplicate dishes: "Paella x2" reads better than two lines.
        var counts = new Dictionary<RecipeData, int>();
        foreach (var r in g.Order)
        {
            if (r == null) continue;
            counts[r] = counts.TryGetValue(r, out int c) ? c + 1 : 1;
        }

        // Cada plato con la estación donde se cocina al lado.
        var lines = new List<string>(counts.Count);
        foreach (var kv in counts)
        {
            string linea = RecipeStations.DishWithStation(kv.Key);
            lines.Add(kv.Value > 1 ? $"{linea} x{kv.Value}" : linea);
        }

        return $"Mesa {_table.tableNumber}\n{string.Join("\n", lines)}";
    }
}
