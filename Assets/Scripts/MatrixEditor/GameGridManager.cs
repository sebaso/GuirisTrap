using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
public class GameGridManager : MonoBehaviour
{
    [SerializeField]
    private GridData _gridData;
    [SerializeField]
    private GridVisualCell _gridViewCellPrefab;

    private GridVisualCell[,] _cells;
    private PlaceableObject[,] _placeables;
    public GridData GetGridData => _gridData;
    [SerializeField] 
    private PlaceableSurface _surface = PlaceableSurface.Floor;
    public PlaceableSurface Surface => _surface;
    const int MIN_DISTANCE = 4;

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
                Vector3 localPos = new Vector3(x + 0.5f, 0f, y + 0.5f);

                Vector3 worldPos = transform.TransformPoint(localPos);

                GridVisualCell cell = Instantiate( _gridViewCellPrefab, worldPos, transform.rotation, transform );

                cell.Init(x, y);
                _cells[x, y] = cell;
            }
        }
    }
    public void SetGridVisible(bool visible)
    {
        if (_cells == null) return;
        
        for (int y = 0; y < _gridData.height; y++)
            for (int x = 0; x < _gridData.width; x++)
                if (_cells[x, y] != null)
                    _cells[x, y].gameObject.SetActive(visible);
    }

    public bool UpdateVisualCell(int newPlaceableObjectX, int newPlaceableObjectY, int newPlaceableObjectStartAtX, int newPlaceableObjectStartAtY, PlaceableItemData item)
    {
        if(newPlaceableObjectX < 0 || newPlaceableObjectY < 0 || newPlaceableObjectX >= _gridData.width || newPlaceableObjectY >= _gridData.height) return false;
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
                if (_gridData.GetIsEntrance(x, y))
                {
                    _cells[x,y].SetState(CellVisualState.Blocked);
                }
            }
        }
    }
    public void ClearLastCell(int lastCellX, int lastCellY)
    {
        if(lastCellX < 0 || lastCellY < 0 || lastCellX >= _gridData.width || lastCellY >= _gridData.height) return;

        _cells[lastCellX, lastCellY].SetState(CellVisualState.Default);
    }
    public void SaveGrid(int newPlaceableObjectX, int newPlaceableObjectY, int startPlaceableObjectX, int startPlaceableObjectY, PlaceableItemData itemData, Quaternion rotation = default)
    {
        if (_gridData == null) return;
        if (newPlaceableObjectX < 0 || newPlaceableObjectY < 0 || newPlaceableObjectX >= _gridData.width || newPlaceableObjectY >= _gridData.height) return;

        _gridData.SetType(newPlaceableObjectX, newPlaceableObjectY, CellType.Occupied);
        _gridData.SetItem(newPlaceableObjectX, newPlaceableObjectY, itemData);
        _gridData.SetRotation(newPlaceableObjectX, newPlaceableObjectY, rotation == default ? Quaternion.identity : rotation);

        bool hasValidStart = startPlaceableObjectX != -1 && startPlaceableObjectY != -1;
        bool movedToNewCell = startPlaceableObjectX != newPlaceableObjectX || startPlaceableObjectY != newPlaceableObjectY;

        if (hasValidStart && movedToNewCell)
        {
            _gridData.SetType(startPlaceableObjectX, startPlaceableObjectY, CellType.Empty);
            _gridData.SetItem(startPlaceableObjectX, startPlaceableObjectY, null);
            _gridData.SetRotation(startPlaceableObjectX, startPlaceableObjectY, Quaternion.identity);
            _placeables[newPlaceableObjectX, newPlaceableObjectY] = _placeables[startPlaceableObjectX, startPlaceableObjectY];
            _placeables[startPlaceableObjectX, startPlaceableObjectY] = null;
        }
    }
public void PlaceableGenerator()
{
    Transform placeableFolder = GameObject.Find("")?.transform;
    if (placeableFolder == null)
        placeableFolder = new GameObject("PlaceableItems").transform;

    for (int y = 0; y < _gridData.height; y++)
    {
        for (int x = 0; x < _gridData.width; x++)
        {
            GridCell cell = _gridData.GetCell(x, y);
            if (_gridData.GetType(x, y) != CellType.Occupied || cell.item == null) continue;

            Vector3 localPos = new Vector3(x, 0f, y) + cell.item.placementOffset;
            Vector3 worldPos = transform.TransformPoint(localPos);

            Quaternion spawnRot = cell.rotation != Quaternion.identity ? cell.rotation : transform.rotation;
            GameObject instance = Instantiate(cell.item.prefab, worldPos, spawnRot, placeableFolder);

            PlaceableObject placeable = instance.GetComponent<PlaceableObject>();

            if (placeable != null)
            {
                placeable.SetGridManager(this);
                placeable.InstancePlaceableObjectCreated(x, y);
                placeable.Init(cell.item);
                _placeables[x, y] = placeable;
            }
        }
    }

    if (_surface == PlaceableSurface.Floor)
    {
        for (int y = 0; y < _gridData.height; y++)
        {
            for (int x = 0; x < _gridData.width; x++)
            {
                PlaceableObject placeable = _placeables[x, y];
                if (placeable == null) continue;
                if (placeable.GetItemData().category == PlaceableCategory.Chair)
                    RotateTowardsTable(placeable, x, y);
            }
        }

        if (SceneController.Instance.IsSceneLoaded("PreparationScene"))
            ValidateAllChairs();
    }
}
    public void ValidateAllChairs()
    {
        if (_surface != PlaceableSurface.Floor) return;

        for (int y = 0; y < _gridData.height; y++)
        {
            for (int x = 0; x < _gridData.width; x++)
            {
                GridCell cell = _gridData.GetCell(x, y);

                if (cell.item != null && cell.item.category == PlaceableCategory.Chair)
                {
                    PlaceableObject chair = _placeables[x, y];

                    if (chair == null) continue;

                    if(CountAdjacentTables(x, y) == 1 && IsCellReachableFromEntrance(x,y)) chair.SetValid(true);
                    else chair.SetValid(false);
                }
            }
        }
    }

    public bool CheckPlaceables()
    {
        if(CountChairs() > 0 && CountTables() > 0)
            return true;
        return false;
    }

    public int CountChairs()
    {
        int numChairs = 0;
        for (int y = 0; y < _gridData.height; y++)
        {
            for (int x = 0; x < _gridData.width; x++)
            {
                GridCell cell = _gridData.GetCell(x, y);

                if (cell.item != null && !cell.isWarehouse && cell.item.category == PlaceableCategory.Chair)
                {
                    numChairs++;
                }
            }
        }
        return numChairs;
    }
    public int CountTables()
    {
        int numTables = 0;
        for (int y = 0; y < _gridData.height; y++)
        {
            for (int x = 0; x < _gridData.width; x++)
            {
                GridCell cell = _gridData.GetCell(x, y);

                if (cell.item != null && !cell.isWarehouse && cell.item.category == PlaceableCategory.Table)
                {
                    numTables++;
                }
            }
        }
        return numTables;
    }
    public bool IsValidTablePlacement(int posX, int posY, int startX, int startY)
    {
        if(CountAdjacentTables(posX, posY, startX, startY) > 0)
            return true;
        for(int y = 0; y < _gridData.height; y++)
        {
            for(int x = 0; x < _gridData.width; x++)
            {
                if (x == startX && y == startY) continue;

                GridCell cell = _gridData.GetCell(x,y);

                if(cell.item == null || cell.item.category != PlaceableCategory.Table) continue;

                int dx = Mathf.Abs(x - posX);
                int dy = Mathf.Abs(y - posY);
                
                int dist = dx + dy;

                if(dist == 1) continue;
                if(dist < MIN_DISTANCE) return false;
            }
        }
        return true;
    }

    public void RotateTowardsTable(PlaceableObject obj, int x, int y)
    {
        if (GetAdjacentTableDirection(x, y) == Vector2Int.zero) return;
        obj.transform.rotation = GetChairRotation(x, y);
    }

    // rotation a chair takes facing its adjacent table; grid rotation if none
    public Quaternion GetChairRotation(int x, int y)
    {
        Vector2Int dir = GetAdjacentTableDirection(x, y);
        if (dir == Vector2Int.zero) return transform.rotation;

        float angle = 0f;
        if (dir == Vector2Int.up) angle = -90f;
        else if (dir == Vector2Int.down) angle = 90f;
        else if (dir == Vector2Int.right) angle = 0f;
        else if (dir == Vector2Int.left) angle = 180f;

        return Quaternion.Euler(0f, angle, 0f);
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
                    if (!CanPlaceItem(x, y, x, y, cell.item ))
                        return false;
                }
            }
        }
        if(!CheckPlaceables())
            return false;
        return true;
    }

    public bool CanPlaceItem(int x, int y, int startX, int startY, PlaceableItemData item, bool ignoreChairRules = false)
    {
        if (x < 0 || y < 0 || x >= _gridData.width || y >= _gridData.height)
            return false;

        if (_gridData.GetType(x, y) != CellType.Empty && !(x == startX && y == startY))
            return false;

        if (item.category == PlaceableCategory.Chair && !ignoreChairRules)
        {
            if (CountAdjacentTables(x, y, startX, startY) != 1)
                return false;
            if(!IsCellReachableFromEntrance(x,y))
                return false;
        }
        if(item.category == PlaceableCategory.Table)
        {
            if(!IsValidTablePlacement(x, y, startX, startY))
                return false;
        }

        return true;
    }
    public int CountAdjacentTables(int x, int y, int ignoreX = -1, int ignoreY = -1)
    {
        int count = 0;

        // Arriba
        if (IsTable(x, y + 1, ignoreX, ignoreY))
            count++;
        // Abajo
        if (IsTable(x, y - 1, ignoreX, ignoreY))
            count++;
        // Derecha
        if (IsTable(x + 1, y, ignoreX, ignoreY))
            count++;
        // Izquierda
        if (IsTable(x - 1, y, ignoreX, ignoreY))
            count++;
        return count;
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
    // table can't be lifted while chairs are tucked against it
    public bool HasAdjacentChairs(int x, int y)
    {
        return IsChair(x, y + 1) || IsChair(x, y - 1) || IsChair(x + 1, y) || IsChair(x - 1, y);
    }
    private bool IsChair(int x, int y)
    {
        if (x < 0 || y < 0 || x >= _gridData.width || y >= _gridData.height)
            return false;

        GridCell cell = _gridData.GetCell(x, y);
        return cell.item != null && cell.item.category == PlaceableCategory.Chair;
    }

    private bool IsTable(int x, int y, int ignoreX = -1, int ignoreY = -1)
    {
        if (x < 0 || y < 0 || x >= _gridData.width || y >= _gridData.height)
            return false;

        if (x == ignoreX && y == ignoreY)
            return false;

        GridCell cell = _gridData.GetCell(x,y);

        if(cell.item != null)
        {
            if(cell.item.category == PlaceableCategory.Table)
                return true;
        }
        return false;
    }
    public bool IsCellReachableFromEntrance(int targetX, int targetY)
    {
        bool[,] visited = new bool[_gridData.width, _gridData.height];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        for (int y = 0; y < _gridData.height; y++)
        {
            for (int x = 0; x < _gridData.width; x++)
            {
                if (_gridData.GetIsEntrance(x, y))
                {
                    queue.Enqueue(new Vector2Int(x, y));
                    visited[x, y] = true;
                }
            }
        }
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            if (current.x == targetX && current.y == targetY)
                return true;

            foreach (var dir in directions)
            {
                int nx = current.x + dir.x;
                int ny = current.y + dir.y;

                if (nx < 0 || ny < 0 || nx >= _gridData.width || ny >= _gridData.height) continue;
                if (visited[nx, ny]) continue;

                bool isTarget  = (nx == targetX && ny == targetY);
                bool isWalkable = _gridData.GetType(nx, ny) == CellType.Empty 
                            || _gridData.GetIsEntrance(nx, ny);

                if ( isTarget || isWalkable)
                {
                    visited[nx, ny] = true;
                    queue.Enqueue(new Vector2Int(nx, ny));
                }
            }
        }
        return false;
    }
    public PlaceableObject GetPlaceableAt(int x, int y) { return _placeables[x,y]; }
    public void SetPlaceableAt(int x, int y, PlaceableObject obj){ _placeables[x, y] = obj; }
}
