using UnityEngine;

public class ChairRefreshUtility : MonoBehaviour
{
    [SerializeField] 
    private GridManager _gridManager;
    [SerializeField] 
    private GridProjectionVisibility _gridProjectionVisibility;

    void OnEnable()
    {
        if (_gridManager != null)
            _gridManager.OnGridChanged += RefreshChairs;
    }

    void OnDisable()
    {
        if (_gridManager != null)
            _gridManager.OnGridChanged -= RefreshChairs;
    }

    private void RefreshChairs()
    {
        var validity = _gridManager.ValidateAllChairs();

        foreach (var kvp in validity)
        {
            Vector3Int anchor = kvp.Key;
            bool isValid = kvp.Value;

            PlaceableObject obj = PlaceableInstanceRegistry.Instance?.Get(anchor);
            if (obj == null) continue;

            PlaceableItemData item = obj.GetItemData();

            if (_gridProjectionVisibility.TryGetWorldTransform(CameraView.Perspective, anchor, out Vector3 basePos, out Quaternion baseRot))
            {
                Quaternion chairRot = _gridManager.GetChairRotationTowardsTable(anchor, baseRot);
                obj.transform.position = basePos + chairRot * item.placementOffset;
                obj.transform.rotation = chairRot;

                _gridManager.SetRotationAtAnchor(anchor, chairRot);
            }

            obj.SetValid(isValid);
        }
    }

    public static void ApplyValidityColorsOnly(GridManager gridManager)
    {
        var validity = gridManager.ValidateAllChairs();
        foreach (var kvp in validity)
            PlaceableInstanceRegistry.Instance?.Get(kvp.Key)?.SetValid(kvp.Value);
    }
}