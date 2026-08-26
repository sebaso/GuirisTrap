using UnityEngine;
using UnityEngine.SceneManagement;

public class PlaceableGenerator : MonoBehaviour
{
    public void Generate()
    {
        foreach (GridZone zone in GridZone.ActiveZones)
            GenerateForZone(zone);
    }

    private void GenerateForZone(GridZone zone)
    {
        if (zone == null || zone.VoxelData == null || zone.Resolver == null || zone.Registry == null) return;

        VoxelGridData voxelData = zone.VoxelData;
        IGridWorldResolver resolver = zone.Resolver;
        PlaceableInstanceRegistry registry = zone.Registry;

        Transform folder = GameObject.Find("PlaceableItems")?.transform;
        if (folder == null) folder = new GameObject("PlaceableItems").transform;

        foreach (var anchor in GridManager.GetAllAnchors(voxelData))
        {
            PlaceableItemData item = GridManager.GetItemAtAnchor(voxelData, anchor);
            if (item == null || item.prefab == null) continue;

            CameraView view = GridManager.DetermineViewForAnchor(voxelData, anchor, item);
            Quaternion rot = GridManager.GetRotationAtAnchor(voxelData, anchor);

            if (!resolver.TryGetWorldTransform(view, anchor, out Vector3 basePos, out _))
                continue;

            Vector3 worldPos = basePos + rot * item.placementOffset;

            GameObject obj = Instantiate(item.prefab, worldPos, rot, folder);
            PlaceableObject placeable = obj.GetComponent<PlaceableObject>();
            placeable.Init(item);
            placeable.InstancePlaceableObjectCreated(anchor, view);

            registry.Register(anchor, placeable);
        }

        if (SceneManager.GetActiveScene().name == "PreparationScene")
            ChairRefreshUtility.ApplyValidityColorsOnly(voxelData, registry);
    }
}