using UnityEngine;

public class ChairRefreshUtility : MonoBehaviour
{
    [SerializeField] 
    private GridZone _zone;

    void OnEnable()
    {
        if (_zone != null && _zone.GridManager != null)
            _zone.GridManager.OnGridChanged += RefreshChairs;
    }

    void OnDisable()
    {
        if (_zone != null && _zone.GridManager != null)
            _zone.GridManager.OnGridChanged -= RefreshChairs;
    }

    private void RefreshChairs()
    {
        GridManager gridManager = _zone.GridManager;
        PlaceableInstanceRegistry registry = _zone.Registry;
        IGridWorldResolver resolver = _zone.Resolver;

        var validity = gridManager.ValidateAllChairs();

        foreach (var kvp in validity)
        {
            Vector3Int anchor = kvp.Key;
            bool isValid = kvp.Value;

            PlaceableObject obj = registry.Get(anchor);
            if (obj == null) continue;

            PlaceableItemData item = obj.GetItemData();

            if (resolver.TryGetWorldTransform(CameraView.Perspective, anchor, out Vector3 basePos, out Quaternion baseRot))
            {
                Quaternion chairRot = gridManager.GetChairRotationTowardsTable(anchor, baseRot);
                obj.transform.position = basePos + chairRot * item.placementOffset;
                obj.transform.rotation = chairRot;

                gridManager.SetRotationAtAnchor(anchor, chairRot);
            }

            obj.SetValid(isValid);
        }
    }

    public static void ApplyValidityColorsOnly(GridManager gridManager, PlaceableInstanceRegistry registry)
    {
        var validity = gridManager.ValidateAllChairs();
        foreach (var kvp in validity)
            registry?.Get(kvp.Key)?.SetValid(kvp.Value);
    }
}