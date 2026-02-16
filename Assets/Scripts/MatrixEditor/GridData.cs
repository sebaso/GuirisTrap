using UnityEngine;

[CreateAssetMenu(fileName = "GridData", menuName = "Scriptable Objects/GridData")]
public class GridData : ScriptableObject
{
    public int _widht;
    public int _height;
    public GridCell[] _cells;

    public CellType GetType(int x, int y)
    {
        return _cells[y * _widht + x].type;
    }

    public void SetType(int x, int y, CellType type)
    {
        _cells[y * _widht + x].type = type;
    }

    public bool GetIsWarehouse(int x, int y)
    {
        return _cells[y * _widht + x].isWarehouse;
    }

    public void SetIsWarehouse(int x, int y, bool isWarehouse)
    {
        _cells[y * _widht + x].isWarehouse = isWarehouse;
    }

}
