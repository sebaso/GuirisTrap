using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameGridManager : MonoBehaviour
{
    [SerializeField]
    private GridData _gridData;
    [SerializeField]
    private GridVisualCell _gridViewCellPrefab;

    private GridVisualCell[,] _cells;
    private PlaceableObject[,] _placeables;
    public GridData GetGridData => _gridData;

    public void Init()
    {
        if (_gridData == null) return;

        _placeables = new PlaceableObject[_gridData.width, _gridData.height];

        if (SceneManager.GetActiveScene().name != "PreparationScene") return;

        _cells = new GridVisualCell[_gridData.width, _gridData.height];

        for (int y = 0; y < _gridData.height; y++)
        {
            for (int x = 0; x < _gridData.width; x++)
            {
                Vector3 position = new Vector3(x + 0.5f, 0f, y + 0.5f);
                GridVisualCell cell = Instantiate(_gridViewCellPrefab, position, Quaternion.identity, transform);
                cell.Init(x, y);
                _cells[x, y] = cell;
            }
        }
    }

    public bool UpdateVisualCell(int newPlaceableObjectX, int newPlaceableObjectY, int newPlaceableObjectStartAtX, int newPlaceableObjectStartAtY, PlaceableItemData item)
    {
        bool valid = CanPlaceItem( newPlaceableObjectX, newPlaceableObjectY, newPlaceableObjectStartAtX, newPlaceableObjectStartAtY, item );
        
        if(valid)
            _cells[newPlaceableObjectX, newPlaceableObjectY].SetState(CellVisualState.Empty);
        else    
            _cells[newPlaceableObjectX, newPlaceableObjectY].SetState(CellVisualState.Blocked);

        return valid;
    }
    public void ResetVisualGrid()
    {
        for(int y = 0; y < _gridData.height; y++)
        {
            for(int x = 0; x < _gridData.width; x++)
            {
                _cells[x,y].SetState(CellVisualState.Default);
            }
        }
    }
    public void ClearLastCell(int lastCellX, int lastCellY)
    {
        if(lastCellX < 0 || lastCellY < 0 || lastCellX >= _gridData.width || lastCellY >= _gridData.height) return;

        _cells[lastCellX, lastCellY].SetState(CellVisualState.Default);
    }
    public void SaveGrid(int newPlaceableObjectX, int newPlaceableObjectY, int startPlaceableObjectX, int startPlaceableObjectY, PlaceableItemData itemData)
    {
        if(_gridData == null ) return;

        if(newPlaceableObjectX < 0 || newPlaceableObjectY < 0 || newPlaceableObjectX >= _gridData.width || newPlaceableObjectY >= _gridData.height) return;

        _gridData.SetType(newPlaceableObjectX, newPlaceableObjectY, CellType.Occupied);
        _gridData.SetItem(newPlaceableObjectX, newPlaceableObjectY, itemData);
        if(startPlaceableObjectX != -1 && startPlaceableObjectY != -1 && startPlaceableObjectX != newPlaceableObjectX || startPlaceableObjectY != newPlaceableObjectY)
            _gridData.SetType(startPlaceableObjectX, startPlaceableObjectY, CellType.Empty);
            _gridData.SetItem(startPlaceableObjectX, startPlaceableObjectY, null);
        _placeables[newPlaceableObjectX, newPlaceableObjectY] = _placeables[startPlaceableObjectX, startPlaceableObjectY];
        _placeables[startPlaceableObjectX, startPlaceableObjectY] = null;
    }
    public void PlaceableGenerator()
    {
        Transform placeableFolder = new GameObject("PlaceableItems").transform;
        for(int y = 0; y < _gridData.height; y++)
        {
            for(int x = 0; x < _gridData.width; x++)
            {
                GridCell cell = _gridData.GetCell(x,y);
                if(_gridData.GetType(x,y) == CellType.Occupied && cell.item != null)
                {
                    Vector3 pos = new Vector3(x, 0f, y);
                    Vector3 finalPos = pos + cell.item.placementOffset;
                    GameObject tableInstance = Instantiate(cell.item.prefab, finalPos, Quaternion.identity, placeableFolder);
                    PlaceableObject placeable = tableInstance.GetComponent<PlaceableObject>();
                    if (placeable != null)
                    {
                        placeable.SetGridManager(this);
                        placeable.InstancePlaceableObjectCreated(x,y);
                        placeable.Init(cell.item);
                        _placeables[x, y] = placeable;
                        if (cell.item.category == PlaceableCategory.Chair)
                        {
                            RotateTowardsTable(placeable, x, y);
                            if(SceneController.Instance.IsSceneLoaded("PreparationScene"))
                                ValidateAllChairs();
                        }
                    }
                }
            }
        }
    }
    public void ValidateAllChairs()
    {
        for (int y = 0; y < _gridData.height; y++)
        {
            for (int x = 0; x < _gridData.width; x++)
            {
                GridCell cell = _gridData.GetCell(x, y);

                if (cell.item != null && cell.item.category == PlaceableCategory.Chair)
                {
                    PlaceableObject chair = _placeables[x, y];

                    if (chair == null) 
                        continue;

                    bool valid = HasAdjacentTable(x, y);
                    chair.SetValid(valid);
                }
            }
        }
    }
    public void RotateTowardsTable(PlaceableObject obj, int x, int y)
    {
        Vector2Int dir = GetAdjacentTableDirection(x, y);

        if (dir == Vector2Int.zero) return;

        float angle = 0f;

        if (dir == Vector2Int.up) 
            angle = -90f;
        else if (dir == Vector2Int.down) 
            angle = 90f;
        else if (dir == Vector2Int.right) 
            angle = 0f;
        else if (dir == Vector2Int.left) 
            angle = 180f;

        obj.transform.rotation = Quaternion.Euler(0f, angle, 0f);
    }

    public bool CanStartDay()
    {
        for (int y = 0; y < _gridData.height; y++)
        {
            for (int x = 0; x < _gridData.width; x++)
            {
                var cell = _gridData.GetCell(x, y);

                if (cell.item != null && cell.item.category == PlaceableCategory.Chair)
                {
                    if (!HasAdjacentTable(x, y))
                        return false;
                }
            }
        }
        return true;
    }

    public bool CanPlaceItem(int x, int y, int startX, int startY, PlaceableItemData item)
    {
        if (x < 0 || y < 0 || x >= _gridData.width || y >= _gridData.height)
            return false;

        if (_gridData.GetType(x, y) != CellType.Empty && !(x == startX && y == startY))
            return false;

        if (item.category == PlaceableCategory.Chair)
        {
            if (!HasAdjacentTable(x, y))
                return false;
        }

        return true;
    }
    public bool HasAdjacentTable(int x, int y)
    {
        // Arriba
        if (IsTable(x, y + 1)) 
            return true;

        // Abajo
        if (IsTable(x, y - 1)) 
            return true;

        // Derecha
        if (IsTable(x + 1, y)) 
            return true;

        // Izquierda
        if (IsTable(x - 1, y)) 
            return true;

        return false;
    }
    public Vector2Int GetAdjacentTableDirection(int x, int y)
    {
        if (IsTable(x, y + 1)) 
            return Vector2Int.up;
        if (IsTable(x, y - 1)) 
            return Vector2Int.down;
        if (IsTable(x + 1, y)) 
            return Vector2Int.right;
        if (IsTable(x - 1, y)) 
            return Vector2Int.left;

        return Vector2Int.zero;
    }
    private bool IsTable(int x, int y)
    {
        if (x < 0 || y < 0 || x >= _gridData.width || y >= _gridData.height)
            return false;

        GridCell cell = _gridData.GetCell(x,y);

        if(cell.item != null)
        {
            if(cell.item.category == PlaceableCategory.Table)
                return true;
        }
        return false;
    }
    public PlaceableObject GetPlaceableAt(int x, int y) { return _placeables[x,y]; }
    public void SetPlaceableAt(int x, int y, PlaceableObject obj){ _placeables[x, y] = obj; }
}
