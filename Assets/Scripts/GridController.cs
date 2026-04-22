using UnityEngine;
using UnityEngine.EventSystems;

public class GridController : MonoBehaviour
{
    [SerializeField]
    private Camera _mainCamera;
    [SerializeField] 
    private LayerMask _floorMask;
    private bool _hasObjectSelected = false;
    PlaceableObject placeableObject;
    [SerializeField]
    private GridData _gridData;
    private GameGridManager _gridManager;
    [SerializeField]
    private GameObject chairPrefab;
    void Start()
    {
        _gridManager = GetComponent<GameGridManager>();
    }
    public void Update()
    {
        SelectPlaceableObject();
        if(placeableObject != null)
        {
            MovePlaceableObject();
            PlacePlaceableObject();
        }
    }

    private void SelectPlaceableObject()
    {
        if (Input.GetMouseButtonDown(0) && !_hasObjectSelected)
        {
            Vector3 moussePos = Input.mousePosition;
            Ray ray = _mainCamera.ScreenPointToRay(moussePos);

            RaycastHit hitInfo;

            bool hit = Physics.Raycast(ray, out hitInfo);
            
            if (hit)
            {
                placeableObject = hitInfo.transform.GetComponent<PlaceableObject>();
                if (placeableObject && !placeableObject.IsSelected())
                {
                    _hasObjectSelected = true;
                    placeableObject.Select(true);
                }
            }
        }
    }
    private void MovePlaceableObject()
    {
       if (placeableObject && _hasObjectSelected)
        {
            if(placeableObject.IsSelected())
            {
                Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                if(Physics.Raycast(ray, out RaycastHit hit, 1000f, _floorMask))
                {
                    placeableObject.transform.position = hit.point;
                    placeableObject.GetComponent<Collider>().enabled = false;
                }
            }
        }
    }
    private void PlacePlaceableObject()
    {
        if (!placeableObject || !_hasObjectSelected) 
            return;
        if (!placeableObject.IsSelected() || !Input.GetMouseButtonUp(0))
        {
            _gridManager.ResetVisualGrid();
            return;
        }

        int x = placeableObject.CurrentCellX;
        int y = placeableObject.CurrentCellY;

        if (CanPlace(x, y))
        {
            PlaceObject(x, y);
            if(placeableObject.GetItemData().category == PlaceableCategory.Table || placeableObject.GetItemData().category == PlaceableCategory.Chair)
                _gridManager.ValidateAllChairs();
        }
        else
        {
            RevertObject();
        }

        _gridManager.ResetVisualGrid();
    }
    private bool CanPlace(int x, int y)
    {
        if (x < 0 || x >= _gridData.width || y < 0 || y >= _gridData.height)
            return false;

        if (_gridData.GetType(x, y) != CellType.Empty || (x == placeableObject.StartCellX && y == placeableObject.StartCellY))
            return false;

        PlaceableItemData item = placeableObject.GetItemData();

        if (item.category == PlaceableCategory.Chair)
        {
            if (!_gridManager.HasAdjacentTable(x, y, placeableObject.StartCellX, placeableObject.StartCellY))
                return false;
        }else if(item.category == PlaceableCategory.Table)
        {
            if(!_gridManager.IsValidTablePlacement(x,y, placeableObject.StartCellX, placeableObject.StartCellY))
                return false;
        }

        return true;
    }
    private void PlaceObject(int x, int y)
    {
        PlaceableItemData item = placeableObject.GetItemData();

        Vector3 pos = new Vector3(x, 0f, y);
        Vector3 finalPos = pos + item.placementOffset;

        placeableObject.transform.position = finalPos;
        placeableObject.GetComponent<Collider>().enabled = true;

        _hasObjectSelected = false;
        placeableObject.Select(false);
        if (item.category == PlaceableCategory.Chair)
            _gridManager.RotateTowardsTable(placeableObject, x, y);

        _gridManager.SaveGrid( x, y, placeableObject.StartCellX, placeableObject.StartCellY, item );

        placeableObject.IsPlacedAtCell();
    }
    private void RevertObject()
    {
        PlaceableItemData item = placeableObject.GetItemData();

        Vector3 pos = new Vector3(placeableObject.StartCellX, 0f, placeableObject.StartCellY);
        Vector3 finalPos = pos + item.placementOffset;

        placeableObject.transform.position = finalPos;
        placeableObject.RestartCell();
        placeableObject.GetComponent<Collider>().enabled = true;

        _hasObjectSelected = false;
        placeableObject.Select(false);
    }
}
