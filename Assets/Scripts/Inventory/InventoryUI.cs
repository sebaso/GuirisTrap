using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] 
    private Inventory _inventory;

    [SerializeField] 
    private InventorySlotUI[] _slotsUI;


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
}
