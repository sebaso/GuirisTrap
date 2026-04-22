using UnityEngine;

[CreateAssetMenu(fileName = "PlaceableItemData", menuName = "Scriptable Objects/PlaceableItemData")]
public class PlaceableItemData : ScriptableObject
{
    public GameObject prefab;
    public Sprite icon;
    public PlaceableCategory category;

    public int cost;
    public int maxStack;

    public bool ocuppied;

    
    [Header("Placement")]
    public Vector3 placementOffset;
}
