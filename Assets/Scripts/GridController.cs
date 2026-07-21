using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class GridController : MonoBehaviour
{
    [SerializeField]
    private Camera _mainCamera;
    [SerializeField] 
    private LayerMask _floorMask;
    [Header("Grid Managers")]
    [SerializeField] 
    private GameGridManager _floorGridManager;
    [SerializeField] 
    private GameGridManager _wallNorthGridManager;
    [SerializeField] 
    private GameGridManager _wallEastGridManager;
    [SerializeField] 
    private GameGridManager _wallWestGridManager;
    [Header("Camera Controller")]
    [SerializeField] 
    private CameraController _cameraController;
    [Header("Overview 3D (solo visual)")]
    [SerializeField] 
    private PerspectiveVoxelView _perspectiveVoxelView;
    private GameGridManager _activeGridManager;
    private bool _hasObjectSelected = false;
    private PlaceableObject _placeableObject;
void Start()
{
    _cameraController.OnViewChanged += OnCameraViewChanged;
    
    if (_wallNorthGridManager != null) _wallNorthGridManager.SetGridVisible(false);
    if (_wallEastGridManager != null)  _wallEastGridManager.SetGridVisible(false);
    if (_wallWestGridManager != null)  _wallWestGridManager.SetGridVisible(false);
    if (_perspectiveVoxelView != null) _perspectiveVoxelView.SetVisible(false);

    OnCameraViewChanged(_cameraController.CurrentView);
}
    void Update()
    {
        SelectPlaceableObject();

        if (_placeableObject != null)
        {
            MovePlaceableObject();
            PlacePlaceableObject();
        }
    }
    void OnDestroy()
    {
        _cameraController.OnViewChanged -= OnCameraViewChanged;
    }

    private void OnCameraViewChanged(CameraView view)
    {
        GameGridManager targetManager = GetManagerForView(view);
        SetActiveGrid(targetManager);

        if (_perspectiveVoxelView != null)
            _perspectiveVoxelView.SetVisible(view == CameraView.Perspective);
    }
    private GameGridManager GetManagerForView(CameraView view)
    {
        switch (view)
        {
            case CameraView.TopDown:   return _floorGridManager;
            case CameraView.WallNorth: return _wallNorthGridManager;
            case CameraView.WallEast:  return _wallEastGridManager;
            case CameraView.WallWest:  return _wallWestGridManager;
            // Perspective es solo overview visual, no hay grid editable activo aquí.
            default:                   return null;
        }
    }
    private void SetActiveGrid(GameGridManager newManager)
    {
        if (_activeGridManager != null)
            _activeGridManager.SetGridVisible(false);

        // Revertir ANTES de reasignar: RevertObject usa _activeGridManager, y
        // debe ser el manager donde estaba el objeto, no el nuevo (que además
        // puede ser null si vamos a Perspective).
        if (_placeableObject != null)
        {
            RevertObject();
        }

        _activeGridManager = newManager;

        if (_activeGridManager != null)
            _activeGridManager.SetGridVisible(true);
    }
    public PlaceableSurface GetActiveSurface()
    {
        if (_activeGridManager == null) return PlaceableSurface.Floor;
        return _activeGridManager.Surface;
    }
    private void SelectPlaceableObject()
    {
        if (_cameraController.IsTransitioning) return;
        if (_activeGridManager == null) return; // Perspective: solo overview, no se puede seleccionar/colocar
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (Input.GetMouseButtonDown(0) && !_hasObjectSelected)
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hitInfo)) return;

            PlaceableObject hit = hitInfo.transform.GetComponent<PlaceableObject>();
            if (hit != null && !hit.IsSelected())
            {
                if (hit.GetItemData().IsCompatibleWith(_activeGridManager.Surface))
                {
                    if (hit.GetItemData().category == PlaceableCategory.Table &&
                        _activeGridManager.HasAdjacentChairs(hit.CurrentCellX, hit.CurrentCellY))
                    {
                        Debug.LogWarning("[GridController] Remove the chairs before moving this table.");
                        return;
                    }

                    _placeableObject = hit;
                    _hasObjectSelected = true;
                    _placeableObject.Select(true);
                }
            }
        }
        else if (Input.GetMouseButtonDown(1) && !_hasObjectSelected)
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hitInfo)) return;

            PlaceableObject hit = hitInfo.transform.GetComponent<PlaceableObject>();
            if (hit != null && hit.GetItemData() != null)
            {
                Inventory.Instance.AddItem(hit.GetItemData());
                RemovePlaceableObject(hit);
                _activeGridManager.ValidateAllChairs();
            }
        }
    }

    private void RemovePlaceableObject(PlaceableObject placeable)
    {
        if (placeable == null) return;

        int u = placeable.CurrentCellX;
        int v = placeable.CurrentCellY;

        if (!_activeGridManager.IsWithinBounds(u, v))
        {
            Destroy(placeable.gameObject);
            return;
        }

        _activeGridManager.ClearCell(u, v);

        if (_activeGridManager.GetPlaceableAt(u, v) != null)
            _activeGridManager.SetPlaceableAt(u, v, null);

        Destroy(placeable.gameObject);
    }

    private void MovePlaceableObject()
    {
        if (_placeableObject == null || !_hasObjectSelected || !_placeableObject.IsSelected()) return;

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _floorMask))
        {
            _placeableObject.transform.position = hit.point;
            _placeableObject.GetComponent<Collider>().enabled = false;
        }
    }

    private void PlacePlaceableObject()
    {
        if (!_placeableObject || !_hasObjectSelected) 
            return;
        if (!_placeableObject.IsSelected() || !Input.GetMouseButtonUp(0))
        {
            _activeGridManager.ResetVisualGrid();
            return;
        }

        int u = _placeableObject.CurrentCellX;
        int v = _placeableObject.CurrentCellY;

        PlaceableCategory category = _placeableObject.GetItemData().category;
        if (CanPlace(u, v))
        {
            PlaceObject(u, v);
            if (category == PlaceableCategory.Table || category == PlaceableCategory.Chair)
                _activeGridManager.ValidateAllChairs();
        }
        else
        {
            RevertObject();
        }

        _activeGridManager.ResetVisualGrid();
    }
    private bool CanPlace(int u, int v)
    {
        if (!_activeGridManager.IsWithinBounds(u, v))
            return false;

        bool isStartCell = (u == _placeableObject.StartCellX && v == _placeableObject.StartCellY);

        if (!_activeGridManager.IsCellEmpty(u, v) && !isStartCell)
            return false;

        return _activeGridManager.CanPlaceItem(u, v, _placeableObject.StartCellX, _placeableObject.StartCellY, _placeableObject.GetItemData());
    }
    private void PlaceObject(int u, int v)
    {
        PlaceableItemData item = _placeableObject.GetItemData();

        Vector3 worldPos = _activeGridManager.GetWorldPosition(u, v, item.placementOffset);
        Quaternion targetRot = item.category == PlaceableCategory.Chair
            ? _activeGridManager.GetChairRotation(u, v)
            : _activeGridManager.transform.rotation;

        _placeableObject.GetComponent<Collider>().enabled = true;

        _hasObjectSelected = false;
        _placeableObject.Select(false);

        _activeGridManager.SaveGrid(u, v,
            _placeableObject.StartCellX, _placeableObject.StartCellY, item, targetRot);

        _placeableObject.IsPlacedAtCell();
        _placeableObject.LerpTo(worldPos, targetRot);
        _placeableObject = null;
    }

    private void RevertObject()
    {
        if (_placeableObject == null) return;

        PlaceableItemData item = _placeableObject.GetItemData();

        Vector3 worldPos = _activeGridManager.GetWorldPosition(
            _placeableObject.StartCellX, _placeableObject.StartCellY, item.placementOffset);

        _placeableObject.transform.position = worldPos;
        _placeableObject.RestartCell();
        _placeableObject.GetComponent<Collider>().enabled = true;

        _hasObjectSelected = false;
        _placeableObject.Select(false);
        _placeableObject = null;
    }
    public GameGridManager ActiveGridManager { get { return _activeGridManager; } }
}