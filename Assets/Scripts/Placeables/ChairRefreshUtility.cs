using System.Collections.Generic;
using UnityEngine;

public class ChairRefreshUtility : MonoBehaviour
{
    private readonly Dictionary<GridZone, System.Action> _handlers = new();

    void OnEnable()
    {
        GridZone.OnZoneRegistered += Subscribe;
        foreach (GridZone zone in GridZone.ActiveZones)
            Subscribe(zone);
    }

    void OnDisable()
    {
        GridZone.OnZoneRegistered -= Subscribe;
        foreach (var kvp in _handlers)
            kvp.Key.OnGridChanged -= kvp.Value;
        _handlers.Clear();
    }

    private void Subscribe(GridZone zone)
    {
        if (zone == null || _handlers.ContainsKey(zone)) return;

        System.Action handler = () => RefreshChairs(zone);
        zone.OnGridChanged += handler;
        _handlers[zone] = handler;
    }

    private void RefreshChairs(GridZone zone)
    {
        VoxelGridData voxelData = zone.VoxelData;
        PlaceableInstanceRegistry registry = zone.Registry;
        IGridWorldResolver resolver = zone.Resolver;

        var validity = GridManager.ValidateAllChairs(voxelData);

        foreach (var kvp in validity)
        {
            Vector3Int anchor = kvp.Key;
            bool isValid = kvp.Value;

            PlaceableObject obj = registry.Get(anchor);
            if (obj == null) continue;

            PlaceableItemData item = obj.GetItemData();

            if (resolver.TryGetWorldTransform(CameraView.Perspective, anchor, out Vector3 basePos, out Quaternion baseRot))
            {
                Quaternion chairRot = GridManager.GetChairRotationTowardsTable(voxelData, anchor, baseRot);
                obj.transform.position = basePos + chairRot * item.placementOffset;
                obj.transform.rotation = chairRot;

                GridManager.SetRotationAtAnchor(voxelData, anchor, chairRot);
            }

            obj.SetValid(isValid);
        }
    }

    public static void ApplyValidityColorsOnly(VoxelGridData voxelData, PlaceableInstanceRegistry registry)
    {
        var validity = GridManager.ValidateAllChairs(voxelData);
        foreach (var kvp in validity)
            registry?.Get(kvp.Key)?.SetValid(kvp.Value);
    }
}