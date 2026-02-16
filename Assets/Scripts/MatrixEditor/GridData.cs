using UnityEngine;

[CreateAssetMenu(fileName = "GridData", menuName = "Scriptable Objects/GridData")]
public class GridData : ScriptableObject
{
    public int widht;
    public int height;
    public int gridWarehouseStartY;
    public int gridWarehouseEndY;
    public GridCell[] _cells;

    public CellType GetType(int x, int y)
    {
        return _cells[y * widht + x].type;
    }

    public void SetType(int x, int y, CellType type)
    {
        _cells[y * widht + x].type = type;
    }

    public bool GetIsWarehouse(int x, int y)
    {
        return _cells[y * widht + x].isWarehouse;
    }

    public void SetIsWarehouse(int x, int y, bool isWarehouse)
    {
        _cells[y * widht + x].isWarehouse = isWarehouse;
    }

}
