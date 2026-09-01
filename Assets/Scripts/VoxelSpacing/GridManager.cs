using UnityEngine;
using System.Collections.Generic;

public enum PlacementAxis { Floor, WallNorth, WallEastWest }

public static class GridManager
{
    private const int MIN_TABLE_DISTANCE = 4;
    public static event System.Action<VoxelGridData> OnGridChanged;

    public static bool IsInBounds(VoxelGridData voxelData, int x, int y, int z)
    {
        return voxelData != null && voxelData.IsInBounds(x, y, z);
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

    private static List<Vector3Int> GetFootprintCells(VoxelGridData voxelData, int x, int y, int z, PlaceableItemData item, PlacementAxis axis)
        => GetFootprintCellsSwap(voxelData, x, y, z, item, axis == PlacementAxis.WallEastWest);

    private static List<Vector3Int> GetFootprintCellsSwap(VoxelGridData voxelData, int x, int y, int z, PlaceableItemData item, bool swapXZ)
    {
        int sx = Mathf.Max(1, item.size.x);
        int sy = Mathf.Max(1, item.size.y);
        int sz = Mathf.Max(1, item.size.z);

        if (swapXZ)
            (sx, sz) = (sz, sx);

        int startX = x - (sx - 1) / 2;
        int startY = y - (sy - 1) / 2;
        int startZ = z - (sz - 1) / 2;

        var cells = new List<Vector3Int>(sx * sy * sz);

        for (int i = 0; i < sx; i++)
            for (int j = 0; j < sy; j++)
                for (int k = 0; k < sz; k++)
                {
                    var cell = new Vector3Int(startX + i, startY + j, startZ + k);
                    if (!IsInBounds(voxelData, cell.x, cell.y, cell.z)) return null;
                    cells.Add(cell);
                }
        return cells;
    }

    public static bool CanPlaceItem(VoxelGridData voxelData, int x, int y, int z, PlaceableItemData item, PlacementAxis axis, Vector3Int? ignoreAnchor = null)
    {
        if (item == null) return false;

        var cells = GetFootprintCells(voxelData, x, y, z, item, axis);
        if (cells == null) return false;

        List<Vector3Int> ignoreCells = null;
        if (ignoreAnchor.HasValue)
            ignoreCells = GetFootprintCells(voxelData, ignoreAnchor.Value.x, ignoreAnchor.Value.y, ignoreAnchor.Value.z, item, axis);

        foreach (var c in cells)
        {
            if (ignoreCells != null && ignoreCells.Contains(c)) continue;
            if (voxelData.GetType(c.x, c.y, c.z) != CellType.Empty) return false;
        }

        if (item.category == PlaceableCategory.Table)
        {
            if (!IsValidTablePlacement(voxelData, new Vector3Int(x, y, z), ignoreAnchor))
                return false;
        }

        return true;
    }

    public static bool MoveItem(VoxelGridData voxelData, Vector3Int fromAnchor, Vector3Int toAnchor, PlaceableItemData item, PlacementAxis axis, Quaternion rotation)
    {
        if (item == null) return false;
        if (!CanPlaceItem(voxelData, toAnchor.x, toAnchor.y, toAnchor.z, item, axis, fromAnchor)) return false;

        var oldCells = GetFootprintCells(voxelData, fromAnchor.x, fromAnchor.y, fromAnchor.z, item, axis);
        if (oldCells != null)
        {
            foreach (var c in oldCells)
            {
                voxelData.SetType(c.x, c.y, c.z, CellType.Empty);
                voxelData.SetItem(c.x, c.y, c.z, null);
                voxelData.SetAnchor(c.x, c.y, c.z, default);
                voxelData.SetRotation(c.x, c.y, c.z, Quaternion.identity);
            }
        }

        var newCells = GetFootprintCells(voxelData, toAnchor.x, toAnchor.y, toAnchor.z, item, axis);
        foreach (var c in newCells)
        {
            voxelData.SetType(c.x, c.y, c.z, CellType.Occupied);
            voxelData.SetAnchor(c.x, c.y, c.z, toAnchor);
        }
        voxelData.SetItem(toAnchor.x, toAnchor.y, toAnchor.z, item);
        voxelData.SetRotation(toAnchor.x, toAnchor.y, toAnchor.z, rotation);

        OnGridChanged?.Invoke(voxelData);
        return true;
    }

    private static int NormalizeStep(int step) => ((step % 4) + 4) % 4;

    /// <summary>El giro extra de 90° es "impar" (90 o 270) si cambia si la huella está swapeada o no.</summary>
    private static bool SwapForStep(PlacementAxis axis, int rotationStep)
        => (axis == PlacementAxis.WallEastWest) ^ (NormalizeStep(rotationStep) % 2 == 1);

    /// <summary>
    /// Comprueba si un item puede pasar de su paso de rotación actual (0-3, cada uno 90°) a otro,
    /// sin mover el ancla. Rechaza si la nueva huella no cabe en el grid o colisiona con otra cosa
    /// (ignorando las celdas que el propio item ya ocupa en su rotación actual).
    /// </summary>
    public static bool CanRotateItem(VoxelGridData voxelData, Vector3Int anchor, PlaceableItemData item, PlacementAxis axis, int currentStep, int newStep)
    {
        if (item == null || !item.isRotatable) return false;

        var ignoreCells = GetFootprintCellsSwap(voxelData, anchor.x, anchor.y, anchor.z, item, SwapForStep(axis, currentStep));
        var newCells = GetFootprintCellsSwap(voxelData, anchor.x, anchor.y, anchor.z, item, SwapForStep(axis, newStep));
        if (newCells == null) return false;

        foreach (var c in newCells)
        {
            if (ignoreCells != null && ignoreCells.Contains(c)) continue;
            if (voxelData.GetType(c.x, c.y, c.z) != CellType.Empty) return false;
        }

        return true;
    }
    public static bool RotateItem(VoxelGridData voxelData, Vector3Int anchor, PlaceableItemData item, PlacementAxis axis, int currentStep, int newStep, Quaternion baseRotation, out Quaternion newRotation)
    {
        newRotation = baseRotation;
        if (!CanRotateItem(voxelData, anchor, item, axis, currentStep, newStep)) return false;

        var oldCells = GetFootprintCellsSwap(voxelData, anchor.x, anchor.y, anchor.z, item, SwapForStep(axis, currentStep));
        if (oldCells != null)
        {
            foreach (var c in oldCells)
            {
                voxelData.SetType(c.x, c.y, c.z, CellType.Empty);
                voxelData.SetItem(c.x, c.y, c.z, null);
                voxelData.SetAnchor(c.x, c.y, c.z, default);
            }
        }

        var newCells = GetFootprintCellsSwap(voxelData, anchor.x, anchor.y, anchor.z, item, SwapForStep(axis, newStep));
        foreach (var c in newCells)
        {
            voxelData.SetType(c.x, c.y, c.z, CellType.Occupied);
            voxelData.SetAnchor(c.x, c.y, c.z, anchor);
        }
        voxelData.SetItem(anchor.x, anchor.y, anchor.z, item);

        newRotation = baseRotation * Quaternion.Euler(0f, 90f * NormalizeStep(newStep), 0f);
        voxelData.SetRotation(anchor.x, anchor.y, anchor.z, newRotation);

        OnGridChanged?.Invoke(voxelData);
        return true;
    }

    public static bool PlaceItem(VoxelGridData voxelData, int x, int y, int z, PlaceableItemData item, PlacementAxis axis, Quaternion rotation)
    {
        if (!CanPlaceItem(voxelData, x, y, z, item, axis)) return false;

        var cells = GetFootprintCells(voxelData, x, y, z, item, axis);
        var anchor = new Vector3Int(x, y, z);

        foreach (var c in cells)
        {
            voxelData.SetType(c.x, c.y, c.z, CellType.Occupied);
            voxelData.SetAnchor(c.x, c.y, c.z, anchor);
        }

        voxelData.SetItem(x, y, z, item);
        voxelData.SetRotation(x, y, z, rotation);
        OnGridChanged?.Invoke(voxelData);
        return true;
    }

    public static bool TryGetItemAt(VoxelGridData voxelData, int x, int y, int z, out PlaceableItemData item, out Vector3Int anchor)
    {
        item = null;
        anchor = default;

        if (!IsInBounds(voxelData, x, y, z) || voxelData.GetType(x, y, z) != CellType.Occupied)
            return false;

        anchor = voxelData.GetAnchor(x, y, z);
        item = voxelData.GetItem(anchor.x, anchor.y, anchor.z);
        return item != null;
    }

    public static bool RemoveItemAt(VoxelGridData voxelData, int x, int y, int z, PlacementAxis axis)
    {
        if (!TryGetItemAt(voxelData, x, y, z, out var item, out var anchor)) return false;

        var cells = GetFootprintCells(voxelData, anchor.x, anchor.y, anchor.z, item, axis);
        if (cells == null) return false;

        foreach (var c in cells)
        {
            voxelData.SetType(c.x, c.y, c.z, CellType.Empty);
            voxelData.SetItem(c.x, c.y, c.z, null);
            voxelData.SetAnchor(c.x, c.y, c.z, default);
            voxelData.SetRotation(c.x, c.y, c.z, Quaternion.identity);
        }
        OnGridChanged?.Invoke(voxelData);
        return true;
    }

    public static bool TryFindFreeCellInLayer(VoxelGridData voxelData, CameraView view, PlaceableItemData item, out Vector3Int cell)
    {
        cell = default;
        if (voxelData == null || item == null) return false;

        PlacementAxis axis = AxisForView(view);

        switch (view)
        {
            case CameraView.Perspective:
            case CameraView.TopDown:
                for (int z = 0; z < voxelData.depth; z++)
                    for (int x = 0; x < voxelData.width; x++)
                        if (CanPlaceItem(voxelData, x, 0, z, item, axis)) { cell = new Vector3Int(x, 0, z); return true; }
                break;

            case CameraView.WallNorth:
                for (int y = 0; y < voxelData.height; y++)
                    for (int x = 0; x < voxelData.width; x++)
                        if (CanPlaceItem(voxelData, x, y, voxelData.depth - 1, item, axis)) { cell = new Vector3Int(x, y, voxelData.depth - 1); return true; }
                break;

            case CameraView.WallEast:
                for (int y = 0; y < voxelData.height; y++)
                    for (int z = 0; z < voxelData.depth; z++)
                        if (CanPlaceItem(voxelData, voxelData.width - 1, y, z, item, axis)) { cell = new Vector3Int(voxelData.width - 1, y, z); return true; }
                break;

            case CameraView.WallWest:
                for (int y = 0; y < voxelData.height; y++)
                    for (int z = 0; z < voxelData.depth; z++)
                        if (CanPlaceItem(voxelData, 0, y, z, item, axis)) { cell = new Vector3Int(0, y, z); return true; }
                break;
        }
        return false;
    }

    public static bool IsValidTablePlacement(VoxelGridData voxelData, Vector3Int cell, Vector3Int? ignoreAnchor = null)
    {
        bool hasAdjacent = false;

        for (int z = 0; z < voxelData.depth; z++)
            for (int x = 0; x < voxelData.width; x++)
            {
                Vector3Int other = new Vector3Int(x, 0, z);
                if (voxelData.GetType(x, 0, z) != CellType.Occupied) continue;

                Vector3Int otherAnchor = voxelData.GetAnchor(x, 0, z);
                if (otherAnchor != other) continue;
                if (ignoreAnchor.HasValue && otherAnchor == ignoreAnchor.Value) continue;

                PlaceableItemData item = voxelData.GetItem(otherAnchor.x, otherAnchor.y, otherAnchor.z);
                if (item == null || item.category != PlaceableCategory.Table) continue;

                int dist = Mathf.Abs(cell.x - otherAnchor.x) + Mathf.Abs(cell.z - otherAnchor.z);
                if (dist == 1) hasAdjacent = true;
            }

        if (hasAdjacent) return true;

        for (int z = 0; z < voxelData.depth; z++)
            for (int x = 0; x < voxelData.width; x++)
            {
                Vector3Int other = new Vector3Int(x, 0, z);
                if (voxelData.GetType(x, 0, z) != CellType.Occupied) continue;

                Vector3Int otherAnchor = voxelData.GetAnchor(x, 0, z);
                if (otherAnchor != other) continue;
                if (ignoreAnchor.HasValue && otherAnchor == ignoreAnchor.Value) continue;

                PlaceableItemData item = voxelData.GetItem(otherAnchor.x, otherAnchor.y, otherAnchor.z);
                if (item == null || item.category != PlaceableCategory.Table) continue;

                int dist = Mathf.Abs(cell.x - otherAnchor.x) + Mathf.Abs(cell.z - otherAnchor.z);
                if (dist < MIN_TABLE_DISTANCE) return false;
            }

        return true;
    }

    public static bool TryGetAdjacentTableDirection(VoxelGridData voxelData, Vector3Int cell, out Vector3Int direction)
    {
        Vector3Int[] dirs = {
            new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
        };

        foreach (var d in dirs)
        {
            Vector3Int neighbor = cell + d;
            if (!IsInBounds(voxelData, neighbor.x, neighbor.y, neighbor.z)) continue;
            if (voxelData.GetType(neighbor.x, neighbor.y, neighbor.z) != CellType.Occupied) continue;

            Vector3Int anchor = voxelData.GetAnchor(neighbor.x, neighbor.y, neighbor.z);
            PlaceableItemData item = voxelData.GetItem(anchor.x, anchor.y, anchor.z);

            if (item != null && item.category == PlaceableCategory.Table)
            {
                direction = d;
                return true;
            }
        }

        direction = Vector3Int.zero;
        return false;
    }

    public static Quaternion GetChairRotationTowardsTable(VoxelGridData voxelData, Vector3Int cell, Quaternion baseRotation)
    {
        if (!TryGetAdjacentTableDirection(voxelData, cell, out Vector3Int dir))
            return baseRotation;

        float angle = 0f;
        if (dir == new Vector3Int(0, 0, 1)) angle = 0f;
        else if (dir == new Vector3Int(0, 0, -1)) angle = 180f;
        else if (dir == new Vector3Int(1, 0, 0)) angle = 90f;
        else if (dir == new Vector3Int(-1, 0, 0)) angle = -90f;

        return baseRotation * Quaternion.Euler(0f, angle, 0f);
    }

    public static void ClearAll(VoxelGridData voxelData)
    {
        if (voxelData == null) return;

        for (int z = 0; z < voxelData.depth; z++)
            for (int y = 0; y < voxelData.height; y++)
                for (int x = 0; x < voxelData.width; x++)
                {
                    voxelData.SetType(x, y, z, CellType.Empty);
                    voxelData.SetItem(x, y, z, null);
                    voxelData.SetAnchor(x, y, z, default);
                }

        OnGridChanged?.Invoke(voxelData);
    }

    public static bool HasAdjacentTable(VoxelGridData voxelData, Vector3Int cell)
    {
        return IsTableAt(voxelData, cell + new Vector3Int(1, 0, 0))
            || IsTableAt(voxelData, cell + new Vector3Int(-1, 0, 0))
            || IsTableAt(voxelData, cell + new Vector3Int(0, 0, 1))
            || IsTableAt(voxelData, cell + new Vector3Int(0, 0, -1));
    }

    private static bool IsTableAt(VoxelGridData voxelData, Vector3Int cell)
    {
        if (!IsInBounds(voxelData, cell.x, cell.y, cell.z)) return false;
        if (voxelData.GetType(cell.x, cell.y, cell.z) != CellType.Occupied) return false;

        Vector3Int anchor = voxelData.GetAnchor(cell.x, cell.y, cell.z);
        PlaceableItemData item = voxelData.GetItem(anchor.x, anchor.y, anchor.z);
        return item != null && item.category == PlaceableCategory.Table;
    }

    public static Dictionary<Vector3Int, bool> ValidateAllChairs(VoxelGridData voxelData)
    {
        var result = new Dictionary<Vector3Int, bool>();
        if (voxelData == null) return result;

        for (int z = 0; z < voxelData.depth; z++)
            for (int x = 0; x < voxelData.width; x++)
            {
                if (voxelData.GetType(x, 0, z) != CellType.Occupied) continue;

                Vector3Int anchor = voxelData.GetAnchor(x, 0, z);
                if (anchor != new Vector3Int(x, 0, z)) continue;

                PlaceableItemData item = voxelData.GetItem(anchor.x, anchor.y, anchor.z);
                if (item == null || item.category != PlaceableCategory.Chair) continue;

                bool hasTable = HasAdjacentTable(voxelData, anchor);
                bool reachable = IsCellReachableFromEntrance(voxelData, anchor);

                result[anchor] = hasTable && reachable;
            }
        return result;
    }

    public static bool IsCellReachableFromEntrance(VoxelGridData voxelData, Vector3Int target)
    {
        bool[,] visited = new bool[voxelData.width, voxelData.depth];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        for (int z = 0; z < voxelData.depth; z++)
            for (int x = 0; x < voxelData.width; x++)
                if (voxelData.GetIsEntrance(x, 0, z))
                {
                    queue.Enqueue(new Vector2Int(x, z));
                    visited[x, z] = true;
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

                if (nx < 0 || nz < 0 || nx >= voxelData.width || nz >= voxelData.depth) continue;
                if (visited[nx, nz]) continue;

                bool isTarget = (nx == targetXZ.x && nz == targetXZ.y);
                bool isWalkable = voxelData.GetType(nx, 0, nz) != CellType.Occupied;

                if (isTarget || isWalkable)
                {
                    visited[nx, nz] = true;
                    queue.Enqueue(new Vector2Int(nx, nz));
                }
            }
        }
        return false;
    }

    public static bool CanStartDay(VoxelGridData voxelData)
    {
        if (CountByCategory(voxelData, PlaceableCategory.Table) == 0) return false;
        if (CountByCategory(voxelData, PlaceableCategory.Chair) == 0) return false;

        var validity = ValidateAllChairs(voxelData);
        foreach (var kvp in validity)
            if (!kvp.Value) return false;

        return true;
    }

    public static int CountByCategory(VoxelGridData voxelData, PlaceableCategory category)
    {
        int count = 0;
        for (int z = 0; z < voxelData.depth; z++)
            for (int x = 0; x < voxelData.width; x++)
            {
                if (voxelData.GetType(x, 0, z) != CellType.Occupied) continue;

                Vector3Int anchor = voxelData.GetAnchor(x, 0, z);
                if (anchor != new Vector3Int(x, 0, z)) continue;

                PlaceableItemData item = voxelData.GetItem(anchor.x, anchor.y, anchor.z);
                if (item != null && item.category == category)
                    count++;
            }
        return count;
    }

    public static CameraView DetermineViewForAnchor(VoxelGridData voxelData, Vector3Int anchor, PlaceableItemData item)
    {
        if (item.surface == PlaceableSurface.Floor) return CameraView.Perspective;
        if (anchor.z == voxelData.depth - 1) return CameraView.WallNorth;
        if (anchor.x == voxelData.width - 1) return CameraView.WallEast;
        return CameraView.WallWest;
    }

    public static SaveManager.CellSaveData3D[] ToSaveData(VoxelGridData voxelData)
    {
        if (voxelData == null) return new SaveManager.CellSaveData3D[0];

        var list = new List<SaveManager.CellSaveData3D>();
        for (int z = 0; z < voxelData.depth; z++)
            for (int y = 0; y < voxelData.height; y++)
                for (int x = 0; x < voxelData.width; x++)
                {
                    VoxelCell cell = voxelData.GetCell(x, y, z);
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
        return list.ToArray();
    }

    public static void LoadFromSaveData(VoxelGridData voxelData, SaveManager.CellSaveData3D[] cells, PlaceableItemData[] allItems)
    {
        if (voxelData == null || cells == null) return;

        foreach (var c in cells)
        {
            if (!IsInBounds(voxelData, c.x, c.y, c.z)) continue;

            voxelData.SetType(c.x, c.y, c.z, c.type);
            voxelData.SetAnchor(c.x, c.y, c.z, new Vector3Int(c.anchorX, c.anchorY, c.anchorZ));
            voxelData.SetIsEntrance(c.x, c.y, c.z, c.isEntrance);
            voxelData.SetRotation(c.x, c.y, c.z, c.rotation);

            PlaceableItemData item = string.IsNullOrEmpty(c.itemName)
                ? null
                : System.Array.Find(allItems, i => i.name == c.itemName);
            voxelData.SetItem(c.x, c.y, c.z, item);
        }

        OnGridChanged?.Invoke(voxelData);
    }

    public static List<Vector3Int> GetAllAnchors(VoxelGridData voxelData)
    {
        var result = new List<Vector3Int>();
        for (int z = 0; z < voxelData.depth; z++)
            for (int y = 0; y < voxelData.height; y++)
                for (int x = 0; x < voxelData.width; x++)
                {
                    if (voxelData.GetType(x, y, z) != CellType.Occupied) continue;
                    Vector3Int anchor = voxelData.GetAnchor(x, y, z);
                    if (anchor == new Vector3Int(x, y, z))
                        result.Add(anchor);
                }
        return result;
    }

    public static PlaceableItemData GetItemAtAnchor(VoxelGridData voxelData, Vector3Int anchor) => voxelData.GetItem(anchor.x, anchor.y, anchor.z);
    public static Quaternion GetRotationAtAnchor(VoxelGridData voxelData, Vector3Int anchor) => voxelData.GetRotation(anchor.x, anchor.y, anchor.z);
    public static void SetRotationAtAnchor(VoxelGridData voxelData, Vector3Int anchor, Quaternion rotation)
    {
        voxelData.SetRotation(anchor.x, anchor.y, anchor.z, rotation);
    }
}