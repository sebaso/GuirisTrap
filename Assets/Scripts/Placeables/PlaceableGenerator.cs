using UnityEngine;
using UnityEngine.SceneManagement;

public class PlaceableGenerator : MonoBehaviour
{
    [SerializeField] 
    private GridZone _zone;

    public void Generate()
    {
        if (_zone == null || _zone.GridManager == null || _zone.Resolver == null || _zone.Registry == null) return;

        GridManager gridManager = _zone.GridManager;
        IGridWorldResolver resolver = _zone.Resolver;
        PlaceableInstanceRegistry registry = _zone.Registry;

        Transform folder = GameObject.Find("PlaceableItems")?.transform;
        if (folder == null) folder = new GameObject("PlaceableItems").transform;

        foreach (var anchor in gridManager.GetAllAnchors())
        {
            PlaceableItemData item = gridManager.GetItemAtAnchor(anchor);
            if (item == null || item.prefab == null) continue;

            CameraView view = gridManager.DetermineViewForAnchor(anchor, item);
            Quaternion rot = gridManager.GetRotationAtAnchor(anchor);

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
            ChairRefreshUtility.ApplyValidityColorsOnly(gridManager, registry);
    }
}