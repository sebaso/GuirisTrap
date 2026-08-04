using UnityEngine;

public class PlacementInputController : MonoBehaviour
{
    [SerializeField] 
    private Camera _mainCamera;
    [SerializeField] 
    private GridManager _gridManager;
    [SerializeField] 
    private GridProjectionVisibility _gridProjectionVisibility;
    [SerializeField] 
    private CameraController _cameraController;

    private PlaceableObject _selected;
    private Vector3Int _dragTargetVoxel;
    private bool _dragValid;
    private bool _hasDragTarget;

    void Update()
    {
        if (_cameraController != null && _cameraController.IsTransitioning) return;

        if (_selected == null)
        {
            if (Input.GetMouseButtonDown(0)) TrySelect();
            return;
        }

        UpdateDrag();

        if (Input.GetMouseButtonUp(0))
            ConfirmOrCancel();
    }

    private void TrySelect()
    {
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        PlaceableObject placeable = hit.transform.GetComponentInParent<PlaceableObject>();
        if (placeable == null) return;

        _selected = placeable;
        _selected.Select(true);
        _hasDragTarget = false;
    }

    private void UpdateDrag()
    {
        CameraView view = _cameraController.CurrentView;
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

        _gridProjectionVisibility.RefreshActive(view);
        _hasDragTarget = _gridProjectionVisibility.TryGetVoxelUnderRay(view, ray, out _dragTargetVoxel);

        if (!_hasDragTarget) { _dragValid = false; return; }

        PlaceableItemData item = _selected.GetItemData();
        PlacementAxis axis = GridManager.AxisForView(view);
        _dragValid = _gridManager.CanPlaceItem(_dragTargetVoxel.x, _dragTargetVoxel.y, _dragTargetVoxel.z, item, axis, _selected.AnchorVoxel);

        _gridProjectionVisibility.SetPreview(view, _dragTargetVoxel, _dragValid ? CellVisualState.Empty : CellVisualState.Blocked);

        if (_gridProjectionVisibility.TryGetWorldTransform(view, _dragTargetVoxel, out Vector3 pos, out Quaternion rot))
        {
            _selected.transform.position = pos + rot * item.placementOffset;
            _selected.transform.rotation = rot;
        }
    }

    private void ConfirmOrCancel()
    {
        CameraView view = _cameraController.CurrentView;
        PlaceableItemData item = _selected.GetItemData();
        PlacementAxis axis = GridManager.AxisForView(view);

        if (_hasDragTarget && _dragValid)
        {
            _gridManager.MoveItem(_selected.AnchorVoxel, _dragTargetVoxel, item, axis);
            _selected.InstancePlaceableObjectCreated(_dragTargetVoxel);
        }
        else if (_gridProjectionVisibility.TryGetWorldTransform(view, _selected.AnchorVoxel, out Vector3 pos, out Quaternion rot))
        {
            _selected.transform.position = pos + rot * item.placementOffset;
            _selected.transform.rotation = rot;
        }

        _gridProjectionVisibility.RefreshActive(view);
        _selected.Select(false);
        _selected = null;
        _hasDragTarget = false;
    }
}