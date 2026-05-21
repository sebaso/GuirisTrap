using UnityEngine;

[CreateAssetMenu(fileName = "GridData", menuName = "Scriptable Objects/GridData")]
public class GridData : ScriptableObject
{
    public int width;
    public int height;
    public int gridWarehouseStartY;
    public int gridWarehouseEndY;
    public GridCell[] _cells;

    public CellType GetType(int x, int y)
    {
        return _cells[y * width + x].type;
    }

    public void SetType(int x, int y, CellType type)
    {
        _cells[y * width + x].type = type;
    }
    public void SetItem(int x, int y, PlaceableItemData item)
    {
        _cells[y * width + x].item = item;
    }
    public bool GetIsWarehouse(int x, int y)
    {
        return _cells[y * width + x].isWarehouse;
    }

    public void SetIsWarehouse(int x, int y, bool isWarehouse)
    {
        _cells[y * width + x].isWarehouse = isWarehouse;
    }
    public bool GetIsEntrance(int x, int y)
    {
        return _cells[y * width + x].isEntrance;
    }

    public void SetIsEntrance(int x, int y, bool isEntrance)
    {
        _cells[y * width + x].isEntrance = isEntrance;
    }
    public GridCell GetCell(int x, int y)
    {
        return _cells[y * width + x];
    }
}
