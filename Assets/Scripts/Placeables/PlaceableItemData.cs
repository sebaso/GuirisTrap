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
    [Tooltip("Un elemento por tier, en orden: [0] es el tier inicial, el último es la mejora máxima.")]
    [SerializeField]
    private List<PlaceableTierData> _tiers = new();
    public int TierCount => _tiers.Count;
    public PlaceableTierData GetTier(int index) => _tiers[Mathf.Clamp(index, 0, _tiers.Count - 1)];

    public PlaceableCategory category;
    public PlaceableSurface surface;
    public int maxStack;

    public bool ocuppied;
    public Vector3Int size = Vector3Int.one;
    public Vector3 placementOffset;

    [Tooltip("Zonas donde se puede colocar este item. Vacío = todas las zonas.")]
    [SerializeField]
    private List<ZoneId> _allowedZones = new();

    [Tooltip("El jugador puede rotarlo manualmente de 90 en 90. Desactivado en objetos que se rotan solos (ej. sillas).")]
    public bool isRotatable = true;

    public bool IsCompatibleWith(PlaceableSurface targetSurface) => surface == targetSurface;

    public bool CanBeUsedInZone(ZoneId zone) => _allowedZones == null || _allowedZones.Count == 0 || _allowedZones.Contains(zone);
}