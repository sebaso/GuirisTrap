using UnityEngine;

public class SaveNewPlaceableObjects : MonoBehaviour
{
    public void PlacePlaceableObject()
    {
        PlaceableObject[] allPlaceables = FindObjectsByType<PlaceableObject>(FindObjectsSortMode.None);

        foreach (PlaceableObject placeable in allPlaceables)
        {
            if (!placeable.OnMoved) continue;
            placeable.IsPlacedAtCell();
        }
    }
}