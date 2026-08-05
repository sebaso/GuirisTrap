using UnityEngine;

[CreateAssetMenu(fileName = "VoxelGridData", menuName = "Scriptable Objects/VoxelGridData")]
public class VoxelGridData : ScriptableObject
{
    public int width;
    public int height;
    public int depth;

    [SerializeField]
    private VoxelCell[] _cells;

    private int Index(int x, int y, int z) => x + y * width + z * width * height;

    public bool IsInBounds(int x, int y, int z)
    {
        return x >= 0 && y >= 0 && z >= 0 && x < width && y < height && z < depth;
    }

    public void Init(int newWidth, int newHeight, int newDepth)
    {
        width = newWidth;
        height = newHeight;
        depth = newDepth;

        int total = width * height * depth;
        _cells = new VoxelCell[total];
        for (int i = 0; i < total; i++)
            _cells[i] = new VoxelCell();
    }

    public VoxelCell GetCell(int x, int y, int z) => _cells[Index(x, y, z)];

    public CellType GetType(int x, int y, int z) => _cells[Index(x, y, z)].type;
    public void SetType(int x, int y, int z, CellType type) => _cells[Index(x, y, z)].type = type;

    public PlaceableItemData GetItem(int x, int y, int z) => _cells[Index(x, y, z)].item;
    public void SetItem(int x, int y, int z, PlaceableItemData item) => _cells[Index(x, y, z)].item = item;

    public Vector3Int GetAnchor(int x, int y, int z) => _cells[Index(x, y, z)].anchor;
    public void SetAnchor(int x, int y, int z, Vector3Int anchor) => _cells[Index(x, y, z)].anchor = anchor;
    public bool GetIsEntrance(int x, int y, int z) => _cells[Index(x, y, z)].isEntrance;
    public void SetIsEntrance(int x, int y, int z, bool isEntrance) => _cells[Index(x, y, z)].isEntrance = isEntrance;
}