using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [SerializeField] 
    private VoxelGridData _voxelData;
    public System.Action OnGridReady;

    void Start()
    {
        OnGridReady?.Invoke();
    }
    public bool IsInBounds(int x, int y, int z)
    {
        return _voxelData != null && _voxelData.IsInBounds(x, y, z);
    }
 
    private List<Vector3Int> GetFootprintCells(int x, int y, int z, PlaceableItemData item)
    {
        int sx = Mathf.Max(1, item.size.x);
        int sy = Mathf.Max(1, item.size.y);
        int sz = Mathf.Max(1, item.size.z);
 
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
 
    public bool CanPlaceItem(int x, int y, int z, PlaceableItemData item)
    {
        if (item == null) return false;
 
        var cells = GetFootprintCells(x, y, z, item);
        if (cells == null) return false;
 
        foreach (var c in cells)
            if (_voxelData.GetType(c.x, c.y, c.z) != CellType.Empty)
                return false;
 
        return true;
    }
 
    public bool PlaceItem(int x, int y, int z, PlaceableItemData item)
    {
        if (!CanPlaceItem(x, y, z, item)) return false;
 
        var cells = GetFootprintCells(x, y, z, item);
        var anchor = new Vector3Int(x, y, z);
 
        foreach (var c in cells)
        {
            _voxelData.SetType(c.x, c.y, c.z, CellType.Occupied);
            _voxelData.SetAnchor(c.x, c.y, c.z, anchor);
        }
 
        _voxelData.SetItem(x, y, z, item);
 
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
 
    public bool RemoveItemAt(int x, int y, int z)
    {
        if (!TryGetItemAt(x, y, z, out var item, out var anchor)) return false;
 
        var cells = GetFootprintCells(anchor.x, anchor.y, anchor.z, item);
        if (cells == null) return false;
 
        foreach (var c in cells)
        {
            _voxelData.SetType(c.x, c.y, c.z, CellType.Empty);
            _voxelData.SetItem(c.x, c.y, c.z, null);
            _voxelData.SetAnchor(c.x, c.y, c.z, default);
        }
 
        return true;
    }
    #region "TEST"
    
        [Header("Solo para probar con el menú contextual (clic derecho en el componente)")]
        [SerializeField] private PlaceableItemData _testItem;
        [SerializeField] private Vector3Int _testCell = new Vector3Int(2, 0, 2);
        [ContextMenu("TEST: Colocar en _testCell")]


        private void TestPlace()
        {
            bool placed = PlaceItem(_testCell.x, _testCell.y, _testCell.z, _testItem);
            Debug.Log($"[GameGridManager] PlaceItem en {_testCell} con '{_testItem?.name}' → {placed}");
        }
    
        [ContextMenu("TEST: Comprobar CanPlaceItem en _testCell")]
        private void TestCanPlace()
        {
            bool canPlace = CanPlaceItem(_testCell.x, _testCell.y, _testCell.z, _testItem);
            Debug.Log($"[GameGridManager] CanPlaceItem en {_testCell} con '{_testItem?.name}' → {canPlace}");
        }
    
        [ContextMenu("TEST: Quitar de _testCell")]
        private void TestRemove()
        {
            bool removed = RemoveItemAt(_testCell.x, _testCell.y, _testCell.z);
            Debug.Log($"[GameGridManager] RemoveItemAt en {_testCell} → {removed}");
        }
        [ContextMenu("TEST: Consultar TryGetItemAt en _testCell")]
        private void TestGetItemAt()
        {
            bool found = TryGetItemAt(_testCell.x, _testCell.y, _testCell.z, out var item, out var anchor);
            Debug.Log($"[GameGridManager] TryGetItemAt en {_testCell} → found={found}, item={(item != null ? item.name : "null")}, ancla={anchor}");
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
    
            Debug.Log($"[GameGridManager] Total celdas ocupadas: {occupiedCount}");
        }
    #endregion
}