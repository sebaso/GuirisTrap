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
    [SerializeField] private GameGridManager _floorGridManager;
    [SerializeField] private GameGridManager _wallSouthGridManager;
    [SerializeField] private GameGridManager _wallEastGridManager;
    [SerializeField] private GameGridManager _wallWestGridManager;
    [Header("Camera Controller")]
    [SerializeField] private CameraController _cameraController;
    private GameGridManager _activeGridManager;
    private bool _hasObjectSelected = false;
    private PlaceableObject _placeableObject;
void Start()
{
    _cameraController.OnViewChanged += OnCameraViewChanged;
    
    if (_wallSouthGridManager != null) _wallSouthGridManager.SetGridVisible(false);
    if (_wallEastGridManager != null)  _wallEastGridManager.SetGridVisible(false);
    if (_wallWestGridManager != null)  _wallWestGridManager.SetGridVisible(false);

    SetActiveGrid(_floorGridManager);
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
    }
    private GameGridManager GetManagerForView(CameraView view)
    {
        switch (view)
        {
            case CameraView.WallSouth: return _wallSouthGridManager;
            case CameraView.WallEast:  return _wallEastGridManager;
            case CameraView.WallWest:  return _wallWestGridManager;
            default:                   return _floorGridManager; // Perspective y TopDown usan el suelo
        }
    }
    private void SetActiveGrid(GameGridManager newManager)
    {
        if (_activeGridManager != null)
            _activeGridManager.SetGridVisible(false);

        _activeGridManager = newManager;

        if (_activeGridManager != null)
            _activeGridManager.SetGridVisible(true);

        if (_placeableObject != null)
        {
            Debug.Log("Lo REVERTEO");
            RevertObject();
        }
    }
    public PlaceableSurface GetActiveSurface()
    {
        if (_activeGridManager == null) return PlaceableSurface.Floor;
        return _activeGridManager.Surface;
    }
    private void SelectPlaceableObject()
    {
        if (_cameraController.IsTransitioning) return;
        if (Input.GetMouseButtonDown(0) && !_hasObjectSelected)
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hitInfo)) return;

            PlaceableObject hit = hitInfo.transform.GetComponent<PlaceableObject>();
            if (hit != null && !hit.IsSelected())
            {
                if (hit.GetItemData().IsCompatibleWith(_activeGridManager.Surface))
                {
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

        int x = placeable.CurrentCellX;
        int y = placeable.CurrentCellY;
        GridData gridData = _activeGridManager.GetGridData;

        if (x < 0 || y < 0 || x >= gridData.width || y >= gridData.height)
        {
            Destroy(placeable.gameObject);
            return;
        }

        gridData.SetType(x, y, CellType.Empty);
        gridData.SetItem(x, y, null);

        if (_activeGridManager.GetPlaceableAt(x, y) != null)
            _activeGridManager.SetPlaceableAt(x, y, null);

        Destroy(placeable.gameObject);
    }

    private void MovePlaceableObject()
    {
        if (_placeableObject == null || !_hasObjectSelected || !_placeableObject.IsSelected()) return;

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        Debug.Log(Physics.Raycast(ray, out RaycastHit hito, 1000f, _floorMask));
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

        int x = _placeableObject.CurrentCellX;
        int y = _placeableObject.CurrentCellY;

        PlaceableCategory category = _placeableObject.GetItemData().category;
        if (CanPlace(x, y))
        {
            PlaceObject(x, y);
            if (category == PlaceableCategory.Table || category == PlaceableCategory.Chair)
                _activeGridManager.ValidateAllChairs();
        }
        else
        {
            RevertObject();
        }

        _activeGridManager.ResetVisualGrid();
    }
    private bool CanPlace(int x, int y)
    {
        GridData gridData = _activeGridManager.GetGridData;
        Debug.Log("X: " + x + " Y: " + y + " GRIDDATA.WITH: " + gridData.width + " GRIDDATA.HEIGHT: " + gridData.height);
        if (x < 0 || y < 0 || x >= gridData.width || y >= gridData.height)
        {
            return false;
        }
        Debug.Log("DE MOMENTO CANPLACE");
        bool isStartCell = false;

        if (x == _placeableObject.StartCellX && y == _placeableObject.StartCellY) {   isStartCell = true; }

        CellType cellType = gridData.GetType(x, y);

        if (cellType != CellType.Empty && !isStartCell) {   return false; }

        bool canPlace = _activeGridManager.CanPlaceItem(x, y, _placeableObject.StartCellX, _placeableObject.StartCellY, _placeableObject.GetItemData());
        Debug.Log("¿CAN PLACE FINALMENTE?" + canPlace);
        if (!canPlace) { return false; }

        return true;
    }
    private void PlaceObject(int x, int y)
    {
        PlaceableItemData item = _placeableObject.GetItemData();

        Vector3 localPos = new Vector3(x, 0f, y) + item.placementOffset;
        Vector3 worldPos = _activeGridManager.transform.TransformPoint(localPos);

        _placeableObject.transform.position = worldPos;
        _placeableObject.transform.rotation = _activeGridManager.transform.rotation;
        _placeableObject.GetComponent<Collider>().enabled = true;

        _hasObjectSelected = false;
        _placeableObject.Select(false);

        if (item.category == PlaceableCategory.Chair)
            _activeGridManager.RotateTowardsTable(_placeableObject, x, y);

        _activeGridManager.SaveGrid(x, y,
            _placeableObject.StartCellX, _placeableObject.StartCellY, item);

        _placeableObject.IsPlacedAtCell();
        _placeableObject = null;
    }

    private void RevertObject()
    {
        if (_placeableObject == null) return;

        PlaceableItemData item = _placeableObject.GetItemData();

        Vector3 localPos = new Vector3(_placeableObject.StartCellX, 0f, _placeableObject.StartCellY) 
                        + item.placementOffset;
        Vector3 worldPos = _activeGridManager.transform.TransformPoint(localPos);

        _placeableObject.transform.position = worldPos;
        _placeableObject.RestartCell();
        _placeableObject.GetComponent<Collider>().enabled = true;

        _hasObjectSelected = false;
        _placeableObject.Select(false);
        _placeableObject = null;
    }
    public GameGridManager ActiveGridManager { get { return _activeGridManager; } }
}