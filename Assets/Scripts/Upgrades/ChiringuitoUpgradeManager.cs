using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;

public class ChiringuitoUpgradeManager : MonoBehaviour
{
    public static event System.Action<int> OnVenueUpgraded;

    [SerializeField] private List<ChiringuitoTierData> _tiers = new();

    [SerializeField] 
    private CameraController _cameraController;
    private int _currentTier = 0;

    void Awake()
    {
        _currentTier = SaveManager.Instance != null ? SaveManager.Instance.ChiringuitoTier : 0;
        ApplyTier(_currentTier, notify: false);
    }

    public bool TryUpgrade()
    {
        int next = _currentTier + 1;
        if (next >= _tiers.Count)
        {
            Debug.Log("[VenueUpgradeManager] El chiringuito ya está en el tier máximo.");
            return false;
        }

        ChiringuitoTierData oldTier = _tiers[_currentTier];
        ChiringuitoTierData newTier = _tiers[next];

        var oldZones = oldTier.tierRoot != null ? oldTier.tierRoot.GetComponentsInChildren<GridZone>(true) : new GridZone[0];
        var newZones = newTier.tierRoot != null ? newTier.tierRoot.GetComponentsInChildren<GridZone>(true) : new GridZone[0];

        var pairs = new List<(GridZone oldZone, GridZone newZone)>();
        foreach (var oldZone in oldZones)
        {
            GridZone newZone = System.Array.Find(newZones, z => z.ZoneId == oldZone.ZoneId);
            if (newZone == null) continue;
            GridManager.MigrateGridData(oldZone.VoxelData, newZone.VoxelData);
            pairs.Add((oldZone, newZone));
        }

        foreach (var pair in pairs)
            foreach (var placeable in pair.oldZone.Registry.All())
                if (placeable != null) Destroy(placeable.gameObject);

        _currentTier = next;
        ApplyTier(_currentTier, notify: true);

        if (_cameraController != null)
        {
            ZoneId currentZoneId = _cameraController.ActiveZone != null ? _cameraController.ActiveZone.ZoneId : ZoneId.Interior;
            GridZone matchingZone = System.Array.Find(newZones, z => z.ZoneId == currentZoneId);
            if (matchingZone != null) _cameraController.SetActiveZone(matchingZone);
        }

        PlaceableGenerator generator = FindAnyObjectByType<PlaceableGenerator>();
        foreach (var pair in pairs)
            generator?.GenerateForZone(pair.newZone);

        if (SaveManager.Instance != null) SaveManager.Instance.ChiringuitoTier = _currentTier;
        return true;
    }

    private void ApplyTier(int tierIndex, bool notify)
    {
        for (int i = 0; i < _tiers.Count; i++)
            if (_tiers[i].tierRoot != null) _tiers[i].tierRoot.SetActive(i == tierIndex);

        var allButtons = new HashSet<GameObject>();
        foreach (var t in _tiers) foreach (var b in t.cameraViewButtons) if (b != null) allButtons.Add(b);
        foreach (var b in allButtons)
            b.SetActive(_tiers[tierIndex].cameraViewButtons.Contains(b));

        int previousTier = tierIndex - 1;
        if (notify && previousTier >= 0)
        {
            var prevGrids = _tiers[previousTier].tierVoxelsGridData;
            var newGrids  = _tiers[tierIndex].tierVoxelsGridData;

            if (prevGrids != null && newGrids != null)
            {
                int count = Mathf.Min(prevGrids.Count, newGrids.Count);
                for (int i = 0; i < count; i++)
                {
                    if (prevGrids[i] != null && newGrids[i] != null)
                        GridManager.MigrateGridData(prevGrids[i], newGrids[i]);
                }
            }
        }

        if (notify) OnVenueUpgraded?.Invoke(tierIndex);
    }

    public IEnumerable<VoxelGridData> GetAllTierGridData()
    {
        foreach (var tier in _tiers)
            if (tier.tierVoxelsGridData != null)
                foreach (var grid in tier.tierVoxelsGridData)
                    if (grid != null) yield return grid;
    }

    public GridZone GetDefaultZone(ZoneId zoneId)
    {
        if (_currentTier < 0 || _currentTier >= _tiers.Count) return null;
        GameObject root = _tiers[_currentTier].tierRoot;
        if (root == null) return null;

        var zones = root.GetComponentsInChildren<GridZone>(true);
        return System.Array.Find(zones, z => z.ZoneId == zoneId);
    }

    [ContextMenu("Debug: Mejorar chiringuito")]
    private void DebugUpgrade() => TryUpgrade();
}