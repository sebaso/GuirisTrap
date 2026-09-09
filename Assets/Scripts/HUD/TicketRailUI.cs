using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space panel listing every active order (one ticket per seated group
/// still waiting for food).
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class TicketRailUI : MonoBehaviour
{
    [Tooltip("Container with a VerticalLayoutGroup (+ ContentSizeFitter). Tickets are added here.")]
    public Transform ticketContainer;

    [Tooltip("Optional ticket prefab: needs a TMP_Text child and an Image on the root. " +
             "If null, a minimal entry is created in code.")]
    public GameObject ticketEntryPrefab;

    [Tooltip("Seconds to fade fully in/out when the rail gains/loses its last ticket.")]
    public float fadeDuration = 0.35f;

    private readonly Dictionary<int, TicketEntry> _entries = new();
    private readonly Color _ok = new(0.35f, 0.75f, 0.35f, 0.55f);
    private readonly Color _mid = new(0.85f, 0.75f, 0.25f, 0.55f);
    private readonly Color _low = new(0.85f, 0.3f, 0.3f, 0.55f);

    private CanvasGroup _canvasGroup;
    private float _alpha;

    private struct TicketEntry
    {
        public GameObject root;
        public TMP_Text text;
        public Image background;
        public int tableNumber; // cached at creation; doesn't change
    }

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _alpha = 0f;
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    void Update()
    {
        if (ticketContainer == null) return;

        var active = RestaurantManager.Instance != null
            ? RestaurantManager.Instance.GetWaitingForFoodGroups()
            : new List<ClientGroup>();

        var activeIds = new HashSet<int>();
        foreach (var g in active)
            if (g != null) activeIds.Add(g.GroupID);

        var toRemove = new List<int>();
        foreach (var kv in _entries)
            if (!activeIds.Contains(kv.Key))
                toRemove.Add(kv.Key);
        foreach (var id in toRemove)
        {
            if (_entries[id].root != null) Destroy(_entries[id].root);
            _entries.Remove(id);
        }
        foreach (var g in active)
        {
            if (g == null) continue;
            if (!_entries.ContainsKey(g.GroupID))
                _entries[g.GroupID] = CreateEntry(g);
            RefreshEntry(g, _entries[g.GroupID]);
        }

        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        float target = _entries.Count > 0 ? 1f : 0f;
        float speed = fadeDuration > 0f ? 1f / fadeDuration : float.MaxValue;
        _alpha = Mathf.MoveTowards(_alpha, target, speed * Time.deltaTime);

        _canvasGroup.alpha = _alpha;
        bool visible = _alpha > 0.01f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }

    private TicketEntry CreateEntry(ClientGroup g)
    {
        GameObject root;
        TMP_Text text;
        Image background;

        if (ticketEntryPrefab != null)
        {
            root = Instantiate(ticketEntryPrefab, ticketContainer);
            text = root.GetComponentInChildren<TMP_Text>();
            background = root.GetComponentInChildren<Image>();
        }
        else
        {
            // Minimal in-code fallback so the panel works without a prefab.
            root = new GameObject($"Ticket_G{g.GroupID}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            root.transform.SetParent(ticketContainer, false);
            background = root.GetComponent<Image>();
            background.color = _ok;
            var le = root.GetComponent<LayoutElement>();
            le.minHeight = 60f;
            le.preferredHeight = 60f;

            var child = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            child.transform.SetParent(root.transform, false);
            var rt = (RectTransform)child.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(10, 6); rt.offsetMax = new Vector2(-10, -6);
            text = child.GetComponent<TextMeshProUGUI>();
            text.enableAutoSizing = false;
            text.fontSize = 28f;
            text.richText = true;
            text.alignment = TextAlignmentOptions.Left;
            text.color = Color.white;
        }

        return new TicketEntry { root = root, text = text, background = background, tableNumber = FirstTableNumberOf(g) };
    }

    private void RefreshEntry(ClientGroup g, TicketEntry entry)
    {
        if (entry.text != null) entry.text.text = FormatTicket(g, entry.tableNumber);
        if (entry.background != null)
        {
            float r = g.PatienceRatio;
            entry.background.color = r > 0.6f ? _ok : (r > 0.3f ? _mid : _low);
        }
    }

    private string FormatTicket(ClientGroup g, int tableNumber)
    {
        // Collapse duplicate dishes: "Paella x2".
        var counts = new Dictionary<string, int>();
        if (g.Order != null)
        {
            foreach (var rec in g.Order)
            {
                if (rec == null) continue;
                counts[rec.dishName] = counts.TryGetValue(rec.dishName, out int c) ? c + 1 : 1;
            }
        }

        var parts = new List<string>(counts.Count);
        foreach (var kv in counts)
            parts.Add(kv.Value > 1 ? $"{kv.Key} x{kv.Value}" : kv.Key);

        string dishes = parts.Count > 0 ? string.Join(", ", parts) : "?";
        return $"<b>Mesa {tableNumber}</b>  ({g.PlatesServed}/{g.PlatesNeeded})\n{dishes}";
    }

    private int FirstTableNumberOf(ClientGroup g)
    {
        foreach (var t in GameObject.FindObjectsByType<Table>())
        {
            if (t != null && t.OccupyingGroup == g) return t.tableNumber;
        }
        return g.GroupID;
    }
}
