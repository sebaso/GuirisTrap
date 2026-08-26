using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using static InputSystem_Actions;

public class PlacementInputController : MonoBehaviour, IUIActions
{
    [SerializeField] 
    private Camera _mainCamera;
    [SerializeField] 
    private CameraController _cameraController;

    private InputSystem_Actions _inputs;

    private PlaceableObject _selected;
    private Vector3Int _dragTargetVoxel;
    private bool _dragValid;
    private bool _hasDragTarget;
    private Vector2 _pointerPos;
    private bool _pointerOverUI;

    void Awake()
    {
        _inputs = new InputSystem_Actions();
        _inputs.UI.AddCallbacks(this);
    }

    void OnEnable()  => _inputs.UI.Enable();
    void OnDisable() => _inputs.UI.Disable();
    void OnDestroy() { _inputs.UI.RemoveCallbacks(this); _inputs.Dispose(); }

    void Update()
    {
        _pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (_cameraController != null && _cameraController.IsTransitioning) return;
        if (_selected != null) UpdateDrag();
    }

    // ── UI Actions ───────────────────────────────────────────────────────

    public void OnPoint(InputAction.CallbackContext context)
        => _pointerPos = context.ReadValue<Vector2>();

    public void OnClick(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (_selected == null)
        {
            if (_pointerOverUI) return;
            TrySelect();
        }
        else
        {
            ConfirmOrCancel();
        }
    }

    public void OnRightClick(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (_selected == null)
        {
            if (_pointerOverUI) return;
            TryPickUpToInventory();
        }
    }

    public void OnNavigate(InputAction.CallbackContext context) { }
    public void OnSubmit(InputAction.CallbackContext context) { }
    public void OnCancel(InputAction.CallbackContext context) { }
    public void OnMiddleClick(InputAction.CallbackContext context) { }
    public void OnScrollWheel(InputAction.CallbackContext context) { }
    public void OnTrackedDevicePosition(InputAction.CallbackContext context) { }
    public void OnTrackedDeviceOrientation(InputAction.CallbackContext context) { }

    private static bool IsFloorView(CameraView view) => view == CameraView.Perspective || view == CameraView.TopDown;

    private void TrySelect()
    {
        Ray ray = _mainCamera.ScreenPointToRay(_pointerPos);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        PlaceableObject placeable = hit.transform.GetComponentInParent<PlaceableObject>();
        if (placeable == null) return;

        CameraView current = _cameraController.CurrentView;
        bool sameView = placeable.PlacedView == current;
        bool bothFloor = IsFloorView(placeable.PlacedView) && IsFloorView(current);

        if (!sameView && !bothFloor) return;

        _selected = placeable;
        _selected.Select(true);
        _hasDragTarget = false;

        _cameraController.SetInputLocked(true);
    }

    private void UpdateDrag()
    {
        GridZone zone = _cameraController.ActiveZone;
        if (zone == null) return;
        VoxelGridData voxelData = zone.VoxelData;

        CameraView view = _cameraController.CurrentView;
        Ray ray = _mainCamera.ScreenPointToRay(_pointerPos);

        zone.RefreshActive(view);
        _hasDragTarget = zone.TryGetVoxelUnderRay(view, ray, out _dragTargetVoxel);

        if (!_hasDragTarget) { _dragValid = false; return; }

        PlaceableItemData item = _selected.GetItemData();
        PlacementAxis axis = GridManager.AxisForView(view);
        _dragValid = GridManager.CanPlaceItem(voxelData, _dragTargetVoxel.x, _dragTargetVoxel.y, _dragTargetVoxel.z, item, axis, _selected.AnchorVoxel);

        zone.SetPreview(view, _dragTargetVoxel, _dragValid ? CellVisualState.Empty : CellVisualState.Blocked);

        if (zone.TryGetWorldTransform(view, _dragTargetVoxel, out Vector3 pos, out Quaternion rot))
        {
            _selected.transform.position = pos + rot * item.placementOffset;
            _selected.transform.rotation = rot;
        }
    }

    private void ConfirmOrCancel()
    {
        GridZone zone = _cameraController.ActiveZone;
        if (zone == null) return;
        VoxelGridData voxelData = zone.VoxelData;
        PlaceableInstanceRegistry registry = zone.Registry;

        CameraView view = _cameraController.CurrentView;
        PlaceableItemData item = _selected.GetItemData();
        PlacementAxis axis = GridManager.AxisForView(view);

        if (_hasDragTarget && _dragValid)
        {
            Vector3Int oldAnchor = _selected.AnchorVoxel;
            registry.Unregister(oldAnchor);
            registry.Register(_dragTargetVoxel, _selected);

            GridManager.MoveItem(voxelData, oldAnchor, _dragTargetVoxel, item, axis, _selected.transform.rotation);
            _selected.InstancePlaceableObjectCreated(_dragTargetVoxel, view);
        }
        else if (zone.TryGetWorldTransform(view, _selected.AnchorVoxel, out Vector3 pos, out Quaternion rot))
        {
            _selected.transform.position = pos + rot * item.placementOffset;
            _selected.transform.rotation = rot;
        }

        zone.RefreshActive(view);
        _selected.Select(false);
        _selected = null;
        _hasDragTarget = false;

        _cameraController.SetInputLocked(false);
    }

    private void TryPickUpToInventory()
    {
        GridZone zone = _cameraController.ActiveZone;
        if (zone == null) return;
        VoxelGridData voxelData = zone.VoxelData;
        PlaceableInstanceRegistry registry = zone.Registry;

        Ray ray = _mainCamera.ScreenPointToRay(_pointerPos);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        PlaceableObject placeable = hit.transform.GetComponentInParent<PlaceableObject>();
        if (placeable == null) return;

        CameraView view = _cameraController.CurrentView;
        if (placeable.PlacedView != view) return;

        PlaceableItemData item = placeable.GetItemData();
        Vector3Int anchor = placeable.AnchorVoxel;
        PlacementAxis axis = GridManager.AxisForView(view);

        if (!GridManager.RemoveItemAt(voxelData, anchor.x, anchor.y, anchor.z, axis)) return;

        Inventory inv = Inventory.Instance != null ? Inventory.Instance : Inventory.EnsureExists();
        bool added = inv.AddItem(item);

        if (!added)
        {
            GridManager.PlaceItem(voxelData, anchor.x, anchor.y, anchor.z, item, axis, placeable.transform.rotation);
            HUDMessage.Instance?.ShowWarning("Inventario lleno.");
            return;
        }

        registry.Unregister(anchor);
        Destroy(placeable.gameObject);
    }
}