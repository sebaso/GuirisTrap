using UnityEngine;

public enum PlaceableSurface
{
    Floor,
    Wall,
    Both
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

    [Header("Placement")]
    public Vector3 placementOffset;

    public bool IsCompatibleWith(PlaceableSurface activeSurface)
    {
        if (surface == PlaceableSurface.Both) return true;
        return surface == activeSurface;
    }
}
