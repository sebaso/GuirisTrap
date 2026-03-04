using UnityEngine;

public class Table : MonoBehaviour
{
    [Header("Table Settings")]
    public int tableNumber = 1;
    public Transform foodPlacementPoint;
    public Transform seatPoint;
    private Food _placedFood;
    private Client _seatedClient;
    public bool IsOccupied => _seatedClient != null;
    public Client SeatedClient => _seatedClient;
    public Transform SeatPoint => seatPoint;
    public bool hasCustomer => IsOccupied;

    void Awake()
    {
        if (foodPlacementPoint == null)
        {
            GameObject placementObj = new GameObject("FoodPlacementPoint");
            placementObj.transform.SetParent(transform);
            placementObj.transform.localPosition = new Vector3(0, 1f, 0);
            foodPlacementPoint = placementObj.transform;
        }

        if (seatPoint == null)
        {
            GameObject seatObj = new GameObject("SeatPoint");
            seatObj.transform.SetParent(transform);
            seatObj.transform.localPosition = new Vector3(0, 0f, -1f); // in front of table
            seatPoint = seatObj.transform;
        }
    }

    void Start()
    {
        RestaurantManager.Instance?.RegisterTable(this);
    }

    void OnDestroy()
    {
        RestaurantManager.Instance?.UnregisterTable(this);
    }

    public void ReserveForClient(Client client)
    {
        _seatedClient = client;
        Debug.Log($"[Table {tableNumber}] Reserved for client.");
    }


    public void FreeTable()
    {
        _seatedClient = null;

        if (_placedFood != null)
        {
            Destroy(_placedFood.gameObject);
            _placedFood = null;
        }

        Debug.Log($"[Table {tableNumber}] Freed.");
        RestaurantManager.Instance?.TableFreed(this);
    }



    public bool CanPlaceFood() => _placedFood == null;

    public void PlaceFood(Food food)
    {
        if (_placedFood != null)
        {
            Debug.Log($"[Table {tableNumber}] Already has food!");
            return;
        }

        _placedFood = food;
        food.PlaceOnTable(foodPlacementPoint);
        Debug.Log($"[Table {tableNumber}] Food placed.");

        if (_seatedClient != null)
            TryServeClient();
    }

    public Food RemoveFood()
    {
        if (_placedFood == null) return null;
        Food food = _placedFood;
        food.Drop();
        _placedFood = null;
        return food;
    }

    public bool HasFood() => _placedFood != null;
    public Food GetPlacedFood() => _placedFood;


    private void TryServeClient()
    {
        if (_placedFood == null || _seatedClient == null) return;

        Debug.Log($"[Table {tableNumber}] Serving client!");
        _seatedClient.ReceiveFood();
        _placedFood.Serve();   // plays serve animation / destroys object
        _placedFood = null;
    }

    public void SetCustomerPresent(bool present)
    {
        if (!present) _seatedClient = null;
    }

    public bool HasCustomer() => IsOccupied;
}
