using UnityEngine;

public class BuyTables : MonoBehaviour
{
    [SerializeField] private GameGridManager _gridManager;
    [SerializeField] private GameObject _tablePrefab;

    public void BuyTable()
    {
        if (_tablePrefab == null || _gridManager == null)
        {
            Debug.LogWarning("No hay prefab o grid asignado en BuildManager");
            return;
        }

        Vector3 initialPosition = new Vector3(-1f, 0f, -1f);

        GameObject tableInstance = Instantiate(_tablePrefab, initialPosition, Quaternion.identity);

        PlaceableObject placeable = tableInstance.GetComponent<PlaceableObject>();
        if (placeable == null)
        {
            Debug.LogWarning("El prefab no tiene PlaceableObject");
            return;
        }

        placeable.SetGridManager(_gridManager);

        tableInstance.transform.position = initialPosition;
    }
}
