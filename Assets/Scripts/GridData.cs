using UnityEngine;

[CreateAssetMenu(fileName = "GridData", menuName = "Scriptable Objects/GridData")]
public class GridData : ScriptableObject
{
    public int _widht;
    public int _height;
    public CellType[] cells;

    public CellType Get(int x, int y)
    {
        return cells[y * _widht + x];
    }

    public void Set(int x, int y, CellType type)
    {
        cells[y * _widht + x] = type;
    }
}
