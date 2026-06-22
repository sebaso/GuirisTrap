using UnityEngine;

public class SaveNewPlaceableObjects : MonoBehaviour
{
    public void PlacePlaceableObject()
    {
        PlaceableObject[] allPlaceables = FindObjectsByType<PlaceableObject>(FindObjectsSortMode.None);

        foreach (PlaceableObject placeable in allPlaceables)
        {
            if (!placeable.OnMoved) continue;

            GameGridManager manager = placeable.GridManager;

            if (manager == null)
            {
                Debug.LogWarning("PlaceableObject " + placeable.name + " no tiene GridManager asignado");
                continue;
            }

            manager.SaveGrid(
                placeable.CurrentCellX, placeable.CurrentCellY,
                placeable.StartCellX, placeable.StartCellY,
                placeable.GetItemData(),
                placeable.transform.rotation
            );

            placeable.IsPlacedAtCell();
        }
    }
}