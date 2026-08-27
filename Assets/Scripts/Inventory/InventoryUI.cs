using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] 
    private Inventory _inventory;

    [SerializeField] 
    private InventorySlotUI[] _slotsUI;
    [SerializeField] 
    private CameraController _cameraController;


    private Inventory Inv => Inventory.Instance != null ? Inventory.Instance : _inventory;
    private void HandleViewChanged(CameraView view) => Refresh();

    void OnEnable()
    {
        Inventory inv = Inv;
        if (inv != null)
        {
            inv.OnInventoryChanged += Refresh;
            Inventory.OnAnyInventoryChanged += Refresh;
        }
        if (_cameraController != null)
            _cameraController.OnViewChanged += HandleViewChanged;

        Refresh();
    }

    void OnDisable()
    {
        Inventory inv = Inv;
        if (inv != null) inv.OnInventoryChanged -= Refresh;
        Inventory.OnAnyInventoryChanged -= Refresh;
        if (_cameraController != null)
            _cameraController.OnViewChanged -= HandleViewChanged;
    }


    public void Refresh()
    {
        Inventory inv = Inv;
        if (inv == null || _slotsUI == null) return;

        PlaceableSurface activeSurface = _cameraController != null
            ? GridZone.SurfaceForView(_cameraController.CurrentView)
            : PlaceableSurface.Floor;
        GridZone activeZone = _cameraController != null ? _cameraController.ActiveZone : null;

        for (int y = 0; y < inv.Height; y++)
        {
            for (int x = 0; x < inv.Width; x++)
            {
                int index = y * inv.Width + x;
                if (index >= _slotsUI.Length || _slotsUI[index] == null) continue;

                InventorySlot slot = inv.GetSlot(x, y);
                bool isCompatible = slot?.item == null
                    || (slot.item.IsCompatibleWith(activeSurface)
                        && (activeZone == null || slot.item.CanBeUsedInZone(activeZone.ZoneId)));

                _slotsUI[index].Init(x, y);
                _slotsUI[index].SetSlot(slot, isCompatible);
            }
        }
    }
}