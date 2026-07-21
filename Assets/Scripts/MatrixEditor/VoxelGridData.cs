using UnityEngine;

[CreateAssetMenu(fileName = "VoxelGridData", menuName = "Scriptable Objects/VoxelGridData")]
public class VoxelGridData : ScriptableObject
{
    public int width;
    public int height;
    public int depth;
    public GridCell[] _cells;

    private int Index(int x, int y, int z) => x + y * width + z * width * height;

    public CellType GetType(int x, int y, int z) => _cells[Index(x, y, z)].type;
    public void SetType(int x, int y, int z, CellType type) => _cells[Index(x, y, z)].type = type;

    public void SetItem(int x, int y, int z, PlaceableItemData item) => _cells[Index(x, y, z)].item = item;

    public bool GetIsEntrance(int x, int y, int z) => _cells[Index(x, y, z)].isEntrance;
    public void SetIsEntrance(int x, int y, int z, bool isEntrance) => _cells[Index(x, y, z)].isEntrance = isEntrance;

    public GridCell GetCell(int x, int y, int z) => _cells[Index(x, y, z)];

    public Quaternion GetRotation(int x, int y, int z) => _cells[Index(x, y, z)].rotation;
    public void SetRotation(int x, int y, int z, Quaternion rot) => _cells[Index(x, y, z)].rotation = rot;
}