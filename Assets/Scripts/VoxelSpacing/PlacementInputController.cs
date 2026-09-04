using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using static InputSystem_Actions;

public class PlacementInputController : MonoBehaviour, IUIActions
{
    private const float DRAG_THRESHOLD_PIXELS = 12f;

    [SerializeField] 
    private Camera _mainCamera;
    [SerializeField] 
    private CameraController _cameraController;
    [SerializeField]
    private RotateBillboardUI _rotateBillboard;

    private InputSystem_Actions _inputs;

    private PlaceableObject _selected;
    private Vector3Int _dragTargetVoxel;
    private bool _dragValid;
    private bool _hasDragTarget;
    private Vector2 _pointerPos;
    private bool _pointerOverUI;

    private bool _isPressed;
    private bool _wasPressedLastFrame;
    private bool _isDraggingMove;
    private Vector2 _pressStartPos;
    public static PlacementInputController Instance { get; private set; }

    private bool _isDragFromInventory;
    private int _inventorySourceX, _inventorySourceY;
    private bool _inventoryPressPending;
    private PlaceableItemData _pendingItem;
    private int _pendingSourceX, _pendingSourceY;
    private Vector2 _inventoryPressStartPos;
    [SerializeField] 
    private GameObject _inventoryPanel;
    void Awake()
    {
        Instance = this;
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

        bool isPressedNow = _inputs.UI.Click.IsPressed();

        if (isPressedNow && !_wasPressedLastFrame)
            HandlePressStarted();
        else if (!isPressedNow && _wasPressedLastFrame)
            HandlePressReleased();

        _wasPressedLastFrame = isPressedNow;

        if (_inventoryPressPending)
        {
            if (Vector2.Distance(_pointerPos, _inventoryPressStartPos) >= DRAG_THRESHOLD_PIXELS)
            {
                _inventoryPressPending = false;
                DragFromInventory(_pendingItem, _pendingSourceX, _pendingSourceY);
            }
        }

        if (_selected != null && _isPressed && !_isDraggingMove)
        {
            if (Vector2.Distance(_pointerPos, _pressStartPos) >= DRAG_THRESHOLD_PIXELS)
            {
                _isDraggingMove = true;
                _rotateBillboard?.Hide();
            }
        }

        if (_selected != null && _isDraggingMove) UpdateDrag();
    }

    // ── UI Actions ──

    public void OnPoint(InputAction.CallbackContext context)
        => _pointerPos = context.ReadValue<Vector2>();

    public void OnClick(InputAction.CallbackContext context) { }

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

    // ── Selección / tap / arrastre ──

    private PlaceableObject RaycastForPlaceable()
    {
        Ray ray = _mainCamera.ScreenPointToRay(_pointerPos);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return null;
        return hit.transform.GetComponentInParent<PlaceableObject>();
    }

    private bool IsSelectable(PlaceableObject placeable)
    {
        CameraView current = _cameraController.CurrentView;
        bool sameView = placeable.PlacedView == current;
        bool bothFloor = IsFloorView(placeable.PlacedView) && IsFloorView(current);
        return sameView || bothFloor;
    }

    private void SelectObject(PlaceableObject obj)
    {
        _selected = obj;
        _selected.Select(true);
        _hasDragTarget = false;
        _cameraController.SetInputLocked(true);
    }

    private void Deselect(bool unlockCamera)
    {
        if (_selected == null) return;
        _selected.Select(false);
        _selected = null;
        _hasDragTarget = false;
        _isDraggingMove = false;
        _rotateBillboard?.Hide();
        if (unlockCamera) _cameraController.SetInputLocked(false);
    }

    private void HandlePressStarted()
    {
        if (_pointerOverUI) return;

        PlaceableObject hit = RaycastForPlaceable();

        if (_selected != null && hit != _selected)
            Deselect(unlockCamera: hit == null);

        if (_selected == null)
        {
            if (hit == null || !IsSelectable(hit)) return;
            SelectObject(hit);
        }

        _pressStartPos = _pointerPos;
        _isDraggingMove = false;
        _isPressed = true;
    }

    private void HandlePressReleased()
    {
        bool wasPressed = _isPressed;
        _isPressed = false;
        if (!wasPressed || _selected == null) return;

        if (_isDraggingMove)
            ConfirmOrCancel();
        else
            _rotateBillboard?.Show(_selected.transform);
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
        if (zone == null) { CancelDrag(); return; }
        VoxelGridData voxelData = zone.VoxelData;
        PlaceableInstanceRegistry registry = zone.Registry;

        CameraView view = _cameraController.CurrentView;
        PlaceableItemData item = _selected.GetItemData();
        PlacementAxis axis = GridManager.AxisForView(view);

        if (_hasDragTarget && _dragValid)
        {
            if (_isDragFromInventory)
            {
                _selected.InstancePlaceableObjectCreated(_dragTargetVoxel, view);
                registry.Register(_dragTargetVoxel, _selected);

                GridManager.PlaceItem(voxelData, _dragTargetVoxel.x, _dragTargetVoxel.y, _dragTargetVoxel.z, item, axis, _selected.transform.rotation);

                Inventory inv = Inventory.Instance != null ? Inventory.Instance : Inventory.EnsureExists();
                inv.RemoveItem(_inventorySourceX, _inventorySourceY);
            }
            else
            {
                Vector3Int oldAnchor = _selected.AnchorVoxel;
                registry.Unregister(oldAnchor);
                registry.Register(_dragTargetVoxel, _selected);

                GridManager.MoveItem(voxelData, oldAnchor, _dragTargetVoxel, item, axis, _selected.transform.rotation);
                _selected.InstancePlaceableObjectCreated(_dragTargetVoxel, view);
            }
        }
        else
        {
            CancelDrag();
            return;
        }

        zone.RefreshActive(view);
        Deselect(unlockCamera: true);
        _inventoryPanel?.SetActive(true);
        _isDragFromInventory = false;
    }

    private void CancelDrag()
    {
        if (_isDragFromInventory)
        {
            Destroy(_selected.gameObject);
            Deselect(unlockCamera: true);
            _inventoryPanel?.SetActive(true); 
            _isDragFromInventory = false;
            return;
        }

        GridZone zone = _cameraController.ActiveZone;
        CameraView view = _cameraController.CurrentView;
        PlaceableItemData item = _selected.GetItemData();

        if (zone != null && zone.TryGetWorldTransform(view, _selected.AnchorVoxel, out Vector3 pos, out Quaternion rot))
        {
            _selected.transform.position = pos + rot * item.placementOffset;
            _selected.transform.rotation = rot;
        }
        zone?.RefreshActive(view);
        Deselect(unlockCamera: true);
    }

    // ── Rotación ──

    public void RotateSelectedCW() => TryRotate(+1);
    public void RotateSelectedCCW() => TryRotate(-1);

    private void TryRotate(int direction)
    {
        if (_selected == null) return;

        PlaceableItemData item = _selected.GetItemData();
        if (item == null || !item.isRotatable) return;

        GridZone zone = _cameraController.ActiveZone;
        if (zone == null) return;

        VoxelGridData voxelData = zone.VoxelData;
        CameraView view = _selected.PlacedView;
        PlacementAxis axis = GridManager.AxisForView(view);
        Vector3Int anchor = _selected.AnchorVoxel;

        int currentStep = _selected.RotationStep;
        int newStep = currentStep + direction;

        if (!zone.TryGetWorldTransform(view, anchor, out Vector3 basePos, out Quaternion baseRot))
            return;

        if (!GridManager.RotateItem(voxelData, anchor, item, axis, currentStep, newStep, baseRot, out Quaternion newRotation))
            return;

        _selected.SetRotationStep(newStep);
        _selected.transform.position = basePos + newRotation * item.placementOffset;
        _selected.transform.rotation = newRotation;
    }

    // ── Recoger al inventario ──

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
    public void DragFromInventory(PlaceableItemData item, int sourceX, int sourceY)
    {
        if (_selected != null) return;

        GridZone zone = _cameraController.ActiveZone;
        if (zone == null || item.prefab == null) return;

        CameraView view = _cameraController.CurrentView;
        PlaceableSurface activeSurface = GridZone.SurfaceForView(view);
        if (!item.IsCompatibleWith(activeSurface)) return;
        if (!item.CanBeUsedInZone(zone.ZoneId)) return;

        Transform folder = GameObject.Find("PlaceableItems")?.transform;
        if (folder == null) folder = new GameObject("PlaceableItems").transform;

        Vector3 spawnPos = _mainCamera.ScreenPointToRay(_pointerPos).GetPoint(5f); 
        GameObject obj = Instantiate(item.prefab, spawnPos, Quaternion.identity, folder);
        PlaceableObject placeable = obj.GetComponent<PlaceableObject>();
        placeable.Init(item);

        _isDragFromInventory = true;
        _inventorySourceX = sourceX;
        _inventorySourceY = sourceY;
        _inventoryPanel?.SetActive(false);

        SelectObject(placeable);
        _isDraggingMove = true;
        _isPressed = true;
        _pressStartPos = _pointerPos;
    }
    public void BeginInventoryPress(PlaceableItemData item, int sourceX, int sourceY)
    {
        if (_selected != null) return;
        _inventoryPressPending = true;
        _pendingItem = item;
        _pendingSourceX = sourceX;
        _pendingSourceY = sourceY;
        _inventoryPressStartPos = _pointerPos;
    }

    public void EndInventoryPress()
    {
        if (!_inventoryPressPending) return;
        _inventoryPressPending = false;

        GameManager.Instance.Place(_pendingSourceX, _pendingSourceY);
    }
}