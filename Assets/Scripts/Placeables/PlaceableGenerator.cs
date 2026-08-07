using UnityEngine;

public class PlaceableGenerator : MonoBehaviour
{
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private GridProjectionVisibility _gridProjectionVisibility;

    public void Generate()
    {
        if (_gridManager == null || _gridProjectionVisibility == null) return;

        Transform folder = GameObject.Find("PlaceableItems")?.transform;
        if (folder == null) folder = new GameObject("PlaceableItems").transform;

        foreach (var anchor in _gridManager.GetAllAnchors())
        {
            PlaceableItemData item = _gridManager.GetItemAtAnchor(anchor);
            if (item == null || item.prefab == null) continue;

            CameraView view = _gridManager.DetermineViewForAnchor(anchor, item);
            Quaternion rot = _gridManager.GetRotationAtAnchor(anchor);

            if (!_gridProjectionVisibility.TryGetWorldTransform(view, anchor, out Vector3 basePos, out _))
                continue;

            Vector3 worldPos = basePos + rot * item.placementOffset;

            GameObject obj = Instantiate(item.prefab, worldPos, rot, folder);
            PlaceableObject placeable = obj.GetComponent<PlaceableObject>();
            placeable.Init(item);
            placeable.InstancePlaceableObjectCreated(anchor, view);

            PlaceableInstanceRegistry.Instance?.Register(anchor, placeable);
        }
        ChairRefreshUtility.ApplyValidityColorsOnly(_gridManager);
    }
}