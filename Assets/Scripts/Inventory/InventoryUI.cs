using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private InventorySlotUI[] _slotsUI;
    [SerializeField] private GridController _gridController;

    // Prefer the live singleton over the serialized reference: a panel-baked
    // Inventory destroys itself as a duplicate once the real singleton exists,
    // and Unity defers that Destroy to end-of-frame, so on first open the
    // serialized field can still point at the soon-to-be-destroyed (empty)
    // instance. Reading Instance keeps the UI on the inventory that actually
    // holds the purchased items.
    private Inventory Inv => Inventory.Instance != null ? Inventory.Instance : _inventory;

    void OnEnable()
    {
        Inventory inv = Inv;
        if (inv != null)
        {
            inv.OnInventoryChanged += Refresh;
            Inventory.OnAnyInventoryChanged += Refresh;
            Refresh();
        }
    }

    void OnDisable()
    {
        Inventory inv = Inv;
        if (inv != null) inv.OnInventoryChanged -= Refresh;
        Inventory.OnAnyInventoryChanged -= Refresh;
    }

    void Update()
    {
        RefreshCompatibility();
    }

    public void Refresh()
    {
        Inventory inv = Inv;
        if (inv == null || _slotsUI == null) return;

        for (int y = 0; y < inv.Height; y++)
        {
            for (int x = 0; x < inv.Width; x++)
            {
                int index = y * inv.Width + x;
                if (index >= _slotsUI.Length || _slotsUI[index] == null) continue;

                InventorySlot slot = inv.GetSlot(x, y);
                _slotsUI[index].Init(x, y);
                _slotsUI[index].SetSlot(slot);
            }
        }
    }

    private void RefreshCompatibility()
    {
        if (_gridController == null || _slotsUI == null) return;

        PlaceableSurface activeSurface = _gridController.GetActiveSurface();

        foreach (InventorySlotUI slotUI in _slotsUI)
        {
            if (slotUI == null) continue;
            slotUI.RefreshCompatibility(activeSurface);
        }
    }
}
