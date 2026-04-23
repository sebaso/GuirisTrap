using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField]
    private Inventory _inventory;
    [SerializeField] 
    private InventorySlotUI[] _slotsUI;
    void Start()
    {
        Refresh();
        _inventory.OnInventoryChanged += Refresh;
    }
    void OnDestroy()
    {
        _inventory.OnInventoryChanged -= Refresh;
    }
    public void Refresh()
    {
        for (int y = 0; y < _inventory.Height; y++)
        {
            for (int x = 0; x < _inventory.Width; x++)
            {
                int index = y * _inventory.Width + x;
                            if (_slotsUI[index] == null)
            {
                Debug.LogError($"SlotUI [{x},{y}] es null en el array");
                continue;
            }
                InventorySlot slot = _inventory.GetSlot(x, y);
                _slotsUI[index].Init(x,y);
                _slotsUI[index].SetSlot(slot);
            }
        }
    }
}
