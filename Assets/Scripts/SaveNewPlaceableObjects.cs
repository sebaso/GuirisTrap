using UnityEngine;

public class SaveNewPlaceableObjects : MonoBehaviour
{
    [SerializeField] 
    private GameGridManager _gridManager;
    public void PlacePlaceableObject()
    {
        if (_gridManager == null)
        {
            Debug.LogWarning("No hay GameGridManager asignado");
            return;
        }
        
        PlaceableObject[] allPlaceables = FindObjectsByType<PlaceableObject>( FindObjectsSortMode.None );

        foreach (PlaceableObject placeable in allPlaceables)
        {
            if (placeable.OnMoved)
            {
                _gridManager.SaveGrid( placeable.CurrentCellX, placeable.CurrentCellY, placeable.StartCellX, placeable.StartCellY );

                placeable.IsPlacedAtCell();
            }
        }
    }
}
