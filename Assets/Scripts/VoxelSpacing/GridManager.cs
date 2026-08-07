using UnityEngine;
using System.Collections.Generic;

public enum PlacementAxis { Floor, WallNorth, WallEastWest }

public class GridManager : MonoBehaviour
{
    [SerializeField] 
    private VoxelGridData _voxelData;
    private const int MIN_TABLE_DISTANCE = 4;

    public System.Action OnGridChanged;

    public bool IsInBounds(int x, int y, int z)
    {
        return _voxelData != null && _voxelData.IsInBounds(x, y, z);
    }

    public static PlacementAxis AxisForView(CameraView view)
    {
        return view switch
        {
            CameraView.WallEast or CameraView.WallWest => PlacementAxis.WallEastWest,
            CameraView.WallNorth => PlacementAxis.WallNorth,
            _ => PlacementAxis.Floor
        };
    }

    private List<Vector3Int> GetFootprintCells(int x, int y, int z, PlaceableItemData item, PlacementAxis axis)
    {
        int sx = Mathf.Max(1, item.size.x);
        int sy = Mathf.Max(1, item.size.y);
        int sz = Mathf.Max(1, item.size.z);

        if (axis == PlacementAxis.WallEastWest)
            (sx, sz) = (sz, sx);

        int startX = x - (sx - 1) / 2;
        int startY = y - (sy - 1) / 2;
        int startZ = z - (sz - 1) / 2;

        var cells = new List<Vector3Int>(sx * sy * sz);

        for (int i = 0; i < sx; i++)
        {
            for (int j = 0; j < sy; j++)
            {
                for (int k = 0; k < sz; k++)
                {
                    var cell = new Vector3Int(startX + i, startY + j, startZ + k);
                    if (!IsInBounds(cell.x, cell.y, cell.z)) return null;
                    cells.Add(cell);
                }
            }
        }
        return cells;
    }

    public bool CanPlaceItem(int x, int y, int z, PlaceableItemData item, PlacementAxis axis, Vector3Int? ignoreAnchor = null)
    {
        if (item == null) return false;

        var cells = GetFootprintCells(x, y, z, item, axis);
        if (cells == null) return false;

        List<Vector3Int> ignoreCells = null;
        if (ignoreAnchor.HasValue)
            ignoreCells = GetFootprintCells(ignoreAnchor.Value.x, ignoreAnchor.Value.y, ignoreAnchor.Value.z, item, axis);

        foreach (var c in cells)
        {
            if (ignoreCells != null && ignoreCells.Contains(c)) continue;
            if (_voxelData.GetType(c.x, c.y, c.z) != CellType.Empty) return false;
        }

        if (item.category == PlaceableCategory.Table)
        {
            if (!IsValidTablePlacement(new Vector3Int(x, y, z), ignoreAnchor))
                return false;
        }

        return true;
    }

    public bool MoveItem(Vector3Int fromAnchor, Vector3Int toAnchor, PlaceableItemData item, PlacementAxis axis, Quaternion rotation)
    {
        if (item == null) return false;
        if (!CanPlaceItem(toAnchor.x, toAnchor.y, toAnchor.z, item, axis, fromAnchor)) return false;

        var oldCells = GetFootprintCells(fromAnchor.x, fromAnchor.y, fromAnchor.z, item, axis);
        if (oldCells != null)
        {
            foreach (var c in oldCells)
            {
                _voxelData.SetType(c.x, c.y, c.z, CellType.Empty);
                _voxelData.SetItem(c.x, c.y, c.z, null);
                _voxelData.SetAnchor(c.x, c.y, c.z, default);
                _voxelData.SetRotation(c.x, c.y, c.z, Quaternion.identity);
            }
        }

        var newCells = GetFootprintCells(toAnchor.x, toAnchor.y, toAnchor.z, item, axis);
        foreach (var c in newCells)
        {
            _voxelData.SetType(c.x, c.y, c.z, CellType.Occupied);
            _voxelData.SetAnchor(c.x, c.y, c.z, toAnchor);
        }
        _voxelData.SetItem(toAnchor.x, toAnchor.y, toAnchor.z, item);
        _voxelData.SetRotation(toAnchor.x, toAnchor.y, toAnchor.z, rotation);

        OnGridChanged?.Invoke();
        return true;
    }

    public bool PlaceItem(int x, int y, int z, PlaceableItemData item, PlacementAxis axis, Quaternion rotation)
    {
        if (!CanPlaceItem(x, y, z, item, axis)) return false;

        var cells = GetFootprintCells(x, y, z, item, axis);
        var anchor = new Vector3Int(x, y, z);

        foreach (var c in cells)
        {
            _voxelData.SetType(c.x, c.y, c.z, CellType.Occupied);
            _voxelData.SetAnchor(c.x, c.y, c.z, anchor);
        }

        _voxelData.SetItem(x, y, z, item);
        _voxelData.SetRotation(x, y, z, rotation);
        OnGridChanged?.Invoke();
        return true;
    }

    public bool TryGetItemAt(int x, int y, int z, out PlaceableItemData item, out Vector3Int anchor)
    {
        item = null;
        anchor = default;

        if (!IsInBounds(x, y, z) || _voxelData.GetType(x, y, z) != CellType.Occupied)
            return false;

        anchor = _voxelData.GetAnchor(x, y, z);
        item = _voxelData.GetItem(anchor.x, anchor.y, anchor.z);
        return item != null;
    }

    public bool RemoveItemAt(int x, int y, int z, PlacementAxis axis)
    {
        if (!TryGetItemAt(x, y, z, out var item, out var anchor)) return false;

        var cells = GetFootprintCells(anchor.x, anchor.y, anchor.z, item, axis);
        if (cells == null) return false;

        foreach (var c in cells)
        {
            _voxelData.SetType(c.x, c.y, c.z, CellType.Empty);
            _voxelData.SetItem(c.x, c.y, c.z, null);
            _voxelData.SetAnchor(c.x, c.y, c.z, default);
            _voxelData.SetRotation(c.x, c.y, c.z, Quaternion.identity);
        }
        OnGridChanged?.Invoke();
        return true;
    }

    public bool TryFindFreeCellInLayer(CameraView view, PlaceableItemData item, out Vector3Int cell)
    {
        cell = default;
        if (_voxelData == null || item == null) return false;

        PlacementAxis axis = AxisForView(view);

        switch (view)
        {
            case CameraView.Perspective:
            case CameraView.TopDown:
                for (int z = 0; z < _voxelData.depth; z++)
                    for (int x = 0; x < _voxelData.width; x++)
                        if (CanPlaceItem(x, 0, z, item, axis)) { cell = new Vector3Int(x, 0, z); return true; }
                break;

            case CameraView.WallNorth:
                for (int y = 0; y < _voxelData.height; y++)
                    for (int x = 0; x < _voxelData.width; x++)
                        if (CanPlaceItem(x, y, _voxelData.depth - 1, item, axis)) { cell = new Vector3Int(x, y, _voxelData.depth - 1); return true; }
                break;

            case CameraView.WallEast:
                for (int y = 0; y < _voxelData.height; y++)
                    for (int z = 0; z < _voxelData.depth; z++)
                        if (CanPlaceItem(_voxelData.width - 1, y, z, item, axis)) { cell = new Vector3Int(_voxelData.width - 1, y, z); return true; }
                break;

            case CameraView.WallWest:
                for (int y = 0; y < _voxelData.height; y++)
                    for (int z = 0; z < _voxelData.depth; z++)
                        if (CanPlaceItem(0, y, z, item, axis)) { cell = new Vector3Int(0, y, z); return true; }
                break;
        }
        return false;
    }
    public bool IsValidTablePlacement(Vector3Int cell, Vector3Int? ignoreAnchor = null)
    {
        bool hasAdjacent = false;

        for (int z = 0; z < _voxelData.depth; z++)
        {
            for (int x = 0; x < _voxelData.width; x++)
            {
                Vector3Int other = new Vector3Int(x, 0, z);
                if (_voxelData.GetType(x, 0, z) != CellType.Occupied) continue;

                Vector3Int otherAnchor = _voxelData.GetAnchor(x, 0, z);
                if (otherAnchor != other) continue;
                if (ignoreAnchor.HasValue && otherAnchor == ignoreAnchor.Value) continue;

                PlaceableItemData item = _voxelData.GetItem(otherAnchor.x, otherAnchor.y, otherAnchor.z);
                if (item == null || item.category != PlaceableCategory.Table) continue;

                int dist = Mathf.Abs(cell.x - otherAnchor.x) + Mathf.Abs(cell.z - otherAnchor.z);

                if (dist == 1) hasAdjacent = true;
            }
        }

        if (hasAdjacent) return true;

        // Si no está pegada a ninguna,  debe respetar la distancia mínima con TODAS.
        for (int z = 0; z < _voxelData.depth; z++)
        {
            for (int x = 0; x < _voxelData.width; x++)
            {
                Vector3Int other = new Vector3Int(x, 0, z);
                if (_voxelData.GetType(x, 0, z) != CellType.Occupied) continue;

                Vector3Int otherAnchor = _voxelData.GetAnchor(x, 0, z);
                if (otherAnchor != other) continue;
                if (ignoreAnchor.HasValue && otherAnchor == ignoreAnchor.Value) continue;

                PlaceableItemData item = _voxelData.GetItem(otherAnchor.x, otherAnchor.y, otherAnchor.z);
                if (item == null || item.category != PlaceableCategory.Table) continue;

                int dist = Mathf.Abs(cell.x - otherAnchor.x) + Mathf.Abs(cell.z - otherAnchor.z);
                if (dist < MIN_TABLE_DISTANCE) return false;
            }
        }

        return true;
    }

    public bool TryGetAdjacentTableDirection(Vector3Int cell, out Vector3Int direction)
    {
        Vector3Int[] dirs = {
            new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
        };

        foreach (var d in dirs)
        {
            Vector3Int neighbor = cell + d;
            if (!IsInBounds(neighbor.x, neighbor.y, neighbor.z)) continue;
            if (_voxelData.GetType(neighbor.x, neighbor.y, neighbor.z) != CellType.Occupied) continue;

            Vector3Int anchor = _voxelData.GetAnchor(neighbor.x, neighbor.y, neighbor.z);
            PlaceableItemData item = _voxelData.GetItem(anchor.x, anchor.y, anchor.z);

            if (item != null && item.category == PlaceableCategory.Table)
            {
                direction = d;
                return true;
            }
        }

        direction = Vector3Int.zero;
        return false;
    }

    public Quaternion GetChairRotationTowardsTable(Vector3Int cell, Quaternion fallbackRotation)
    {
        if (!TryGetAdjacentTableDirection(cell, out Vector3Int dir))
            return fallbackRotation;

        float angle = 0f;
        if (dir == new Vector3Int(0, 0, 1)) angle = 0f;
        else if (dir == new Vector3Int(0, 0, -1)) angle = 180f;
        else if (dir == new Vector3Int(1, 0, 0)) angle = 90f;
        else if (dir == new Vector3Int(-1, 0, 0)) angle = -90f;

        return Quaternion.Euler(0f, angle, 0f);
    }
    [ContextMenu("TEST: Limpiar el grid")]
    public void ClearAll()
    {
        if (_voxelData == null) return;

        for (int z = 0; z < _voxelData.depth; z++)
            for (int y = 0; y < _voxelData.height; y++)
                for (int x = 0; x < _voxelData.width; x++)
                {
                    _voxelData.SetType(x, y, z, CellType.Empty);
                    _voxelData.SetItem(x, y, z, null);
                    _voxelData.SetAnchor(x, y, z, default);
                }

        OnGridChanged?.Invoke();
    }
    public bool HasAdjacentTable(Vector3Int cell)
    {
        return IsTableAt(cell + new Vector3Int(1, 0, 0))
            || IsTableAt(cell + new Vector3Int(-1, 0, 0))
            || IsTableAt(cell + new Vector3Int(0, 0, 1))
            || IsTableAt(cell + new Vector3Int(0, 0, -1));
    }

    private bool IsTableAt(Vector3Int cell)
    {
        if (!IsInBounds(cell.x, cell.y, cell.z)) return false;
        if (_voxelData.GetType(cell.x, cell.y, cell.z) != CellType.Occupied) return false;

        Vector3Int anchor = _voxelData.GetAnchor(cell.x, cell.y, cell.z);
        PlaceableItemData item = _voxelData.GetItem(anchor.x, anchor.y, anchor.z);
        return item != null && item.category == PlaceableCategory.Table;
    }

    public Dictionary<Vector3Int, bool> ValidateAllChairs()
    {
        var result = new Dictionary<Vector3Int, bool>();
        if (_voxelData == null) return result;

        for (int z = 0; z < _voxelData.depth; z++)
        {
            for (int x = 0; x < _voxelData.width; x++)
            {
                if (_voxelData.GetType(x, 0, z) != CellType.Occupied) continue;

                Vector3Int anchor = _voxelData.GetAnchor(x, 0, z);
                if (anchor != new Vector3Int(x, 0, z)) continue;

                PlaceableItemData item = _voxelData.GetItem(anchor.x, anchor.y, anchor.z);
                if (item == null || item.category != PlaceableCategory.Chair) continue;

                bool hasTable = HasAdjacentTable(anchor);
                bool reachable = IsCellReachableFromEntrance(anchor);

                result[anchor] = hasTable && reachable;
            }
        }
        return result;
    }
    
    public bool IsCellReachableFromEntrance(Vector3Int target)
    {
        bool[,] visited = new bool[_voxelData.width, _voxelData.depth];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        for (int z = 0; z < _voxelData.depth; z++)
        {
            for (int x = 0; x < _voxelData.width; x++)
            {
                if (_voxelData.GetIsEntrance(x, 0, z))
                {
                    queue.Enqueue(new Vector2Int(x, z));
                    visited[x, z] = true;
                }
            }
        }

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        Vector2Int targetXZ = new Vector2Int(target.x, target.z);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (current == targetXZ) return true;

            foreach (var dir in directions)
            {
                int nx = current.x + dir.x;
                int nz = current.y + dir.y;

                if (nx < 0 || nz < 0 || nx >= _voxelData.width || nz >= _voxelData.depth) continue;
                if (visited[nx, nz]) continue;

                bool isTarget = (nx == targetXZ.x && nz == targetXZ.y);
                bool isWalkable = _voxelData.GetType(nx, 0, nz) != CellType.Occupied;

                if (isTarget || isWalkable)
                {
                    visited[nx, nz] = true;
                    queue.Enqueue(new Vector2Int(nx, nz));
                }
            }
        }
        return false;
    }
    public bool CanStartDay()
    {
        if (CountByCategory(PlaceableCategory.Table) == 0) return false;
        if (CountByCategory(PlaceableCategory.Chair) == 0) return false;

        var validity = ValidateAllChairs();
        foreach (var kvp in validity)
        {
            if (!kvp.Value) return false;
        }

        return true;
    }

    private int CountByCategory(PlaceableCategory category)
    {
        int count = 0;
        for (int z = 0; z < _voxelData.depth; z++)
        {
            for (int x = 0; x < _voxelData.width; x++)
            {
                if (_voxelData.GetType(x, 0, z) != CellType.Occupied) continue;

                Vector3Int anchor = _voxelData.GetAnchor(x, 0, z);
                if (anchor != new Vector3Int(x, 0, z)) continue;

                PlaceableItemData item = _voxelData.GetItem(anchor.x, anchor.y, anchor.z);
                if (item != null && item.category == category)
                    count++;
            }
        }
        return count;
    }
    public CameraView DetermineViewForAnchor(Vector3Int anchor, PlaceableItemData item)
    {
        if (item.surface == PlaceableSurface.Floor) return CameraView.Perspective;
        if (anchor.z == _voxelData.depth - 1) return CameraView.WallNorth;
        if (anchor.x == _voxelData.width - 1) return CameraView.WallEast;
        return CameraView.WallWest;
    }
    public SaveManager.CellSaveData3D[] ToSaveData()
    {
        if (_voxelData == null) return new SaveManager.CellSaveData3D[0];

        var list = new List<SaveManager.CellSaveData3D>();
        for (int z = 0; z < _voxelData.depth; z++)
        {
            for (int y = 0; y < _voxelData.height; y++)
            {
                for (int x = 0; x < _voxelData.width; x++)
                {
                    VoxelCell cell = _voxelData.GetCell(x, y, z);
                    list.Add(new SaveManager.CellSaveData3D
                    {
                        x = x, y = y, z = z,
                        type = cell.type,
                        itemName = cell.item != null ? cell.item.name : "",
                        anchorX = cell.anchor.x, anchorY = cell.anchor.y, anchorZ = cell.anchor.z,
                        isEntrance = cell.isEntrance,
                        rotation = cell.rotation
                    });
                }
            }
        }
        return list.ToArray();
    }

    public void LoadFromSaveData(SaveManager.CellSaveData3D[] cells, PlaceableItemData[] allItems)
    {
        if (_voxelData == null || cells == null) return;

        foreach (var c in cells)
        {
            if (!IsInBounds(c.x, c.y, c.z)) continue; // el grid de diseño pudo cambiar de tamaño

            _voxelData.SetType(c.x, c.y, c.z, c.type);
            _voxelData.SetAnchor(c.x, c.y, c.z, new Vector3Int(c.anchorX, c.anchorY, c.anchorZ));
            _voxelData.SetIsEntrance(c.x, c.y, c.z, c.isEntrance);
            _voxelData.SetRotation(c.x, c.y, c.z, c.rotation);

            PlaceableItemData item = string.IsNullOrEmpty(c.itemName)
                ? null
                : System.Array.Find(allItems, i => i.name == c.itemName);
            _voxelData.SetItem(c.x, c.y, c.z, item);
        }

        OnGridChanged?.Invoke();
    }
    public System.Collections.Generic.List<Vector3Int> GetAllAnchors()
    {
        var result = new System.Collections.Generic.List<Vector3Int>();
        for (int z = 0; z < _voxelData.depth; z++)
            for (int y = 0; y < _voxelData.height; y++)
                for (int x = 0; x < _voxelData.width; x++)
                {
                    if (_voxelData.GetType(x, y, z) != CellType.Occupied) continue;
                    Vector3Int anchor = _voxelData.GetAnchor(x, y, z);
                    if (anchor == new Vector3Int(x, y, z))
                        result.Add(anchor);
                }
        return result;
    }

    public PlaceableItemData GetItemAtAnchor(Vector3Int anchor) => _voxelData.GetItem(anchor.x, anchor.y, anchor.z);
    public Quaternion GetRotationAtAnchor(Vector3Int anchor) => _voxelData.GetRotation(anchor.x, anchor.y, anchor.z);
    public void SetRotationAtAnchor(Vector3Int anchor, Quaternion rotation)
    {
        _voxelData.SetRotation(anchor.x, anchor.y, anchor.z, rotation);
    }
}