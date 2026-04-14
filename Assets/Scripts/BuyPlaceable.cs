using UnityEngine;

public class BuyPlaceable : MonoBehaviour
{
    [SerializeField] private GameGridManager _gridManager;
    public void Buy(PlaceableItemData itemData)
    {
        Transform folder = GameObject.Find("PlaceableItems")?.transform;
        if (itemData == null || itemData.prefab == null || _gridManager == null)
        {
            Debug.LogWarning("No hay prefab o grid asignado en BuildManager");
            return;
        }
        for(int y = 0; y < _gridManager.GetGridData.height; y++)
        {
            for(int x = 0; x < _gridManager.GetGridData.width; x++)
            {
                if(_gridManager.GetGridData.GetIsWarehouse(x,y) == true && _gridManager.GetGridData.GetType(x,y) == CellType.Empty)
                {
                    Vector3 initialPosition = new Vector3(x+0.5f, 0f, y+0.5f);

                    GameObject obj = Instantiate(itemData.prefab, initialPosition, Quaternion.identity, folder);

                    PlaceableObject placeable = obj.GetComponent<PlaceableObject>();
                    _gridManager.GetGridData.SetType(x,y, CellType.Occupied);
                    placeable.SetGridManager(_gridManager);
                    placeable.InstancePlaceableObjectCreated(x,y);
                    return;
                }
            }
        }
    }
}
