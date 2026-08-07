[System.Serializable]
public class VoxelCell
{
    public CellType type = CellType.Empty;
    public PlaceableItemData item;
    public UnityEngine.Vector3Int anchor;
    public bool isEntrance = false;
    public UnityEngine.Quaternion rotation = UnityEngine.Quaternion.identity;
}