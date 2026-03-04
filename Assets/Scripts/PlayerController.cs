using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float speed;
    private Rigidbody rb;
    public float maxSpeed = 10f;

    [Header("Pickup System")]
    public Transform holdPoint;
    public float interactionRange = 2f;
    public KeyCode interactKey = KeyCode.E;
    
    private Food heldFood;

    [Header("Minigame System")]
    public RecipeData currentRecipe;
    public GameObject redCubeIngredient; 

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (holdPoint == null)
        {
            GameObject holdObj = new GameObject("HoldPoint");
            holdObj.transform.SetParent(transform);
            holdObj.transform.localPosition = new Vector3(0, 1.5f, 0.5f);
            holdPoint = holdObj.transform;
        }
    }

    void Update()
    {
        Movement();
        HandleInteraction();
    }

    public void Movement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0.0f, vertical).normalized;
        rb.linearVelocity = new Vector3(direction.x * speed, rb.linearVelocity.y, direction.z * speed);
    }

    private void HandleInteraction()
    {
        if (Input.GetKeyDown(interactKey))
        {
            if (heldFood != null)
            {
                TryPlaceOrDropFood();
            }
            else
            {
                TryPickUpFood();
            }
        }
    }

    private void TryPickUpFood()
    {
        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, interactionRange);
        
        foreach (Collider col in nearbyObjects)
        {
            Kitchen kitchen = col.GetComponent<Kitchen>();
            if (kitchen != null)
            {
                Food newFood = kitchen.GetFood();
                if (newFood != null)
                {
                    PickUpFood(newFood);
                    return;
                }
            }
            
            Food food = col.GetComponent<Food>();
            if (food != null && !food.IsBeingHeld)
            {
                PickUpFood(food);
                return;
            }
        }
    }

    private void PickUpFood(Food food)
    {
        heldFood = food;
        food.PickUp(holdPoint);
        Debug.Log($"Picked up: {food.foodName}");
    }

    private void TryPlaceOrDropFood()
    {
        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, interactionRange);
        print("Nearby objects: " + nearbyObjects.Length);
        
        foreach (Collider col in nearbyObjects)
        {
            Table table = col.GetComponent<Table>();
            if(table == null){
                table = col.GetComponentInParent<Table>();
            }
            
            if (table != null && table.CanPlaceFood())
            {
                table.PlaceFood(heldFood);
                heldFood = null;
                Debug.Log("Placed food on table");
                return;
            }
        }
        
        DropFood();
    }

    private void DropFood()
    {
        if (heldFood != null)
        {
            heldFood.Drop();
            Debug.Log($"Dropped: {heldFood.foodName}");
            heldFood = null;
        }
    }

    public bool IsHoldingFood()
    {
        return heldFood != null;
    }

    public Food GetHeldFood()
    {
        return heldFood;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
    
    public void SetCurrentIngredients(RecipeData data)
    {
    currentRecipe = data;
    if(redCubeIngredient != null) 
    {
        redCubeIngredient.SetActive(true); 
    }
}

    public void ResetInput()
    {
    Input.ResetInputAxes();
}
}
