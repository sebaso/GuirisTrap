using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private InventorySlotUI[] _slotsUI;
    [SerializeField] private GridController _gridController;

    void Start()
    {
        Refresh();
        _inventory.OnInventoryChanged += Refresh;
    }

    void OnDestroy()
    {
        _inventory.OnInventoryChanged -= Refresh;
    }

    void Update()
    {
        RefreshCompatibility();
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
                _slotsUI[index].Init(x, y);
                _slotsUI[index].SetSlot(slot);
            }
        }
    }

    private void RefreshCompatibility()
    {
        if (_gridController == null) return;

        PlaceableSurface activeSurface = _gridController.GetActiveSurface();

        foreach (InventorySlotUI slotUI in _slotsUI)
        {
            if (slotUI == null) continue;
            slotUI.RefreshCompatibility(activeSurface);
        }
    }
}
