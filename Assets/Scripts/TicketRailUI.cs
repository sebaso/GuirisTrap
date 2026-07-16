using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Screen-space panel listing every active order (one ticket per seated group
/// still waiting for food). Sits under the scene's main Canvas as a sibling of
/// CashUI / HUDMessage. Polls <see cref="RestaurantManager.GetWaitingForFoodGroups"/>
/// each frame and keeps its ticket list in sync — simple and fine for a dozen
/// tables.
///
/// Each ticket shows "Mesa {n}" + the ordered dishes, tinted green→yellow→red
/// by the group's patience ratio. If <see cref="ticketEntryPrefab"/> is null a
/// minimal entry is built in code so the panel works before you author a prefab.
/// </summary>
public class TicketRailUI : MonoBehaviour
{
    [Tooltip("Container with a VerticalLayoutGroup (+ ContentSizeFitter). Tickets are added here.")]
    public Transform ticketContainer;

    [Tooltip("Optional ticket prefab: needs a TMP_Text child and an Image on the root. " +
             "If null, a minimal entry is created in code.")]
    public GameObject ticketEntryPrefab;

    private readonly Dictionary<int, TicketEntry> _entries = new();
    private readonly Color _ok = new(0.35f, 0.75f, 0.35f, 0.55f);
    private readonly Color _mid = new(0.85f, 0.75f, 0.25f, 0.55f);
    private readonly Color _low = new(0.85f, 0.3f, 0.3f, 0.55f);

    private struct TicketEntry
    {
        public GameObject root;
        public TMP_Text text;
        public Image background;
        public int tableNumber; // cached at creation; doesn't change
    }

    void Update()
    {
        if (ticketContainer == null) return;

        var active = RestaurantManager.Instance != null
            ? RestaurantManager.Instance.GetWaitingForFoodGroups()
            : new List<ClientGroup>();

        // Index active groups by id.
        var activeIds = new HashSet<int>();
        foreach (var g in active)
            if (g != null) activeIds.Add(g.GroupID);

        // Remove tickets for groups no longer waiting.
        var toRemove = new List<int>();
        foreach (var kv in _entries)
            if (!activeIds.Contains(kv.Key))
                toRemove.Add(kv.Key);
        foreach (var id in toRemove)
        {
            if (_entries[id].root != null) Destroy(_entries[id].root);
            _entries.Remove(id);
        }

        // Add / refresh tickets.
        foreach (var g in active)
        {
            if (g == null) continue;
            if (!_entries.ContainsKey(g.GroupID))
                _entries[g.GroupID] = CreateEntry(g);
            RefreshEntry(g, _entries[g.GroupID]);
        }
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
            root = new GameObject($"Ticket_G{g.GroupID}", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(ticketContainer, false);
            background = root.GetComponent<Image>();
            background.color = _ok;

            var child = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            child.transform.SetParent(root.transform, false);
            var rt = (RectTransform)child.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(6, 4); rt.offsetMax = new Vector2(-6, -4);
            text = child.GetComponent<TextMeshProUGUI>();
            text.fontSize = 20;
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
        return $"<b>Mesa {tableNumber}</b>  <size=80%>({g.PlatesServed}/{g.PlatesNeeded})\n{dishes}";
    }

    private int FirstTableNumberOf(ClientGroup g)
    {
        // The rail doesn't hold a table ref; scan placed tables once (at ticket
        // creation) to label the ticket. Not called per frame.
        foreach (var t in GameObject.FindObjectsByType<Table>(FindObjectsSortMode.None))
        {
            if (t != null && t.OccupyingGroup == g) return t.tableNumber;
        }
        return g.GroupID;
    }
}
