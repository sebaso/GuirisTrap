using UnityEngine;
public enum PlaceableSurface
{
    Floor,
    Wall
}

[CreateAssetMenu(fileName = "PlaceableItemData", menuName = "Scriptable Objects/PlaceableItemData")]
public class PlaceableItemData : ScriptableObject
{
    public GameObject prefab;
    public Sprite icon;
    public PlaceableCategory category;
    public PlaceableSurface surface;
    public int cost;
    public int maxStack;

    public bool ocuppied;
    public Vector3Int size = Vector3Int.one;
    public Vector3 placementOffset;
    public bool IsCompatibleWith(PlaceableSurface targetSurface) => surface == targetSurface;

}
