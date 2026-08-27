using System.Collections.Generic;
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

    [Tooltip("Zonas donde se puede colocar este item. Vacío = todas las zonas.")]
    [SerializeField]
    private List<ZoneId> _allowedZones = new();

    public bool IsCompatibleWith(PlaceableSurface targetSurface) => surface == targetSurface;

    public bool CanBeUsedInZone(ZoneId zone) => _allowedZones == null || _allowedZones.Count == 0 || _allowedZones.Contains(zone);
}