using UnityEngine;
using System.Collections.Generic;

public enum PlacementAxis { Floor, WallNorth, WallEastWest }

public class GridManager : MonoBehaviour
{
    [SerializeField] 
    private VoxelGridData _voxelData;
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

        // En pared Este/Oeste, la "anchura" del item (size.x, pensada para
        // moverse en X en Floor/North) en realidad se mueve en Z. Intercambiamos.
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
        return true;
    }

    public bool MoveItem(Vector3Int fromAnchor, Vector3Int toAnchor, PlaceableItemData item, PlacementAxis axis)
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
            }
        }

        var newCells = GetFootprintCells(toAnchor.x, toAnchor.y, toAnchor.z, item, axis);
        foreach (var c in newCells)
        {
            _voxelData.SetType(c.x, c.y, c.z, CellType.Occupied);
            _voxelData.SetAnchor(c.x, c.y, c.z, toAnchor);
        }
        _voxelData.SetItem(toAnchor.x, toAnchor.y, toAnchor.z, item);

        OnGridChanged?.Invoke();
        return true;
    }

    public bool PlaceItem(int x, int y, int z, PlaceableItemData item, PlacementAxis axis)
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

    #region "TEST"

        [Header("Solo para probar con el menú contextual (clic derecho en el componente)")]
        [SerializeField] private PlaceableItemData _testItem;
        [SerializeField] private Vector3Int _testCell = new Vector3Int(2, 0, 2);
        [SerializeField] private PlacementAxis _testAxis = PlacementAxis.Floor;

        [ContextMenu("TEST: Colocar en _testCell")]
        private void TestPlace()
        {
            bool placed = PlaceItem(_testCell.x, _testCell.y, _testCell.z, _testItem, _testAxis);
            Debug.Log($"[GridManager] PlaceItem en {_testCell} con '{_testItem?.name}' ({_testAxis}) → {placed}");
        }

        [ContextMenu("TEST: Comprobar CanPlaceItem en _testCell")]
        private void TestCanPlace()
        {
            bool canPlace = CanPlaceItem(_testCell.x, _testCell.y, _testCell.z, _testItem, _testAxis);
            Debug.Log($"[GridManager] CanPlaceItem en {_testCell} con '{_testItem?.name}' ({_testAxis}) → {canPlace}");
        }

        [ContextMenu("TEST: Quitar de _testCell")]
        private void TestRemove()
        {
            bool removed = RemoveItemAt(_testCell.x, _testCell.y, _testCell.z, _testAxis);
            Debug.Log($"[GridManager] RemoveItemAt en {_testCell} → {removed}");
        }
        [ContextMenu("TEST: Consultar TryGetItemAt en _testCell")]
        private void TestGetItemAt()
        {
            bool found = TryGetItemAt(_testCell.x, _testCell.y, _testCell.z, out var item, out var anchor);
            Debug.Log($"[GridManager] TryGetItemAt en {_testCell} → found={found}, item={(item != null ? item.name : "null")}, ancla={anchor}");
        }
        [ContextMenu("TEST: Volcar estado de toda la matriz")]
        private void TestDumpGrid()
        {
            if (_voxelData == null) { Debug.LogWarning("Sin VoxelGridData asignado."); return; }

            int occupiedCount = 0;
            for (int z = 0; z < _voxelData.depth; z++)
                for (int y = 0; y < _voxelData.height; y++)
                    for (int x = 0; x < _voxelData.width; x++)
                        if (_voxelData.GetType(x, y, z) == CellType.Occupied)
                        {
                            occupiedCount++;
                            var item = _voxelData.GetItem(x, y, z);
                            Debug.Log($"  Ocupada ({x},{y},{z}) → item: {(item != null ? item.name : "null (celda secundaria de un footprint)")}");
                        }

            Debug.Log($"[GridManager] Total celdas ocupadas: {occupiedCount}");
        }
    #endregion
}