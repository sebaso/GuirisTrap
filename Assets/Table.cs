using UnityEngine;

public class Table : MonoBehaviour
{
    [Header("Table Settings")]
    public Transform foodPlacementPoint;
    public int tableNumber = 1;

    private Food placedFood;
    public bool hasCustomer = false;

    void Start()
    {
        if (foodPlacementPoint == null)
        {
            GameObject placementObj = new GameObject("FoodPlacementPoint");
            placementObj.transform.SetParent(transform);
            placementObj.transform.localPosition = new Vector3(0, 1f, 0);
            foodPlacementPoint = placementObj.transform;
        }
    }

    public bool CanPlaceFood()
    {
        return placedFood == null;
    }

    public void PlaceFood(Food food)
    {
        if (placedFood != null)
        {
            Debug.Log($"Table {tableNumber} already has food!");
            return;
        }

        placedFood = food;
        food.PlaceOnTable(foodPlacementPoint);
        Debug.Log($"Food placed on Table {tableNumber}");

        if (hasCustomer)
        {
            ServeCustomer();
        }
    }

    public Food RemoveFood()
    {
        if (placedFood == null) return null;

        Food food = placedFood;
        food.Drop();
        placedFood = null;
        return food;
    }

    public bool HasFood()
    {
        return placedFood != null;
    }

    public Food GetPlacedFood()
    {
        return placedFood;
    }

    public void SetCustomerPresent(bool present)
    {
        hasCustomer = present;
        
        if (hasCustomer && placedFood != null)
        {
            ServeCustomer();
        }
    }

    private void ServeCustomer()
    {
        if (placedFood != null && hasCustomer)
        {
            Debug.Log($"Customer at Table {tableNumber} received their {placedFood.foodName}!");
            placedFood.Serve();
            placedFood = null;
            hasCustomer = false;
        }
    }

    public bool HasCustomer()
    {
        return hasCustomer;
    }
}
