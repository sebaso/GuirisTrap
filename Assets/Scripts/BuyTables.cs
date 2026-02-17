using UnityEngine;

public class BuyTables : MonoBehaviour
{
    [SerializeField] private GameGridManager _gridManager;
    [SerializeField] private GameObject _tablePrefab;
    public void BuyTable()
    {
        Transform tableFolder = GameObject.Find("Tables")?.transform;

        if (_tablePrefab == null || _gridManager == null)
        {
            Debug.LogWarning("No hay prefab o grid asignado en BuildManager");
            return;
        }
        for(int y = 0; y < _gridManager.GetGridData.height; y++)
        {
            for(int x = 0; x < _gridManager.GetGridData.widht; x++)
            {
                if(_gridManager.GetGridData.GetIsWarehouse(x,y) == true && _gridManager.GetGridData.GetType(x,y) == CellType.Empty)
                {
                    Vector3 initialPosition = new Vector3(x+0.5f, 0f, y+0.5f);

                    GameObject tableInstance = Instantiate(_tablePrefab, initialPosition, Quaternion.identity, tableFolder);

                    PlaceableObject placeable = tableInstance.GetComponent<PlaceableObject>();
                    _gridManager.GetGridData.SetType(x,y, CellType.Occupied);
                    placeable.SetGridManager(_gridManager);
                    placeable.InstancePlaceableObjectCreated(x,y);
                    return;
                }
            }
        }
    }
}
