using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    private Rigidbody rb;
    public float maxSpeed = 10f;
    private Vector3 movementDirection;

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
        movementDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0.0f, Input.GetAxisRaw("Vertical")).normalized;
        HandleInteraction();
        
        if (Input.GetKeyDown(KeyCode.Escape)) 
            Application.Quit();
    }

    void FixedUpdate()
    {
        Movement();
    }

    public void Movement()
    {
        rb.linearVelocity = new Vector3(movementDirection.x * speed, rb.linearVelocity.y, movementDirection.z * speed);
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
                Debug.Log("COCINAMOS LA COMIDA");
                PickUpFood(food);
                return;
            }
        }
    }

    public void PickUpFood(Food food)
    {
        heldFood = food;
        food.PickUp(holdPoint);
        Debug.Log($"Picked up: {food.foodName}");
    }

    private void TryPlaceOrDropFood()
    {
        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, interactionRange);
        
        bool foundAnyTable = false;
        foreach (Collider col in nearbyObjects)
        {
            Table table = col.GetComponent<Table>();
            if (table == null)
            {
                table = col.GetComponentInParent<Table>();
            }
            
            if (table != null)
            {
                foundAnyTable = true;
                if (table.CanPlaceFood())
                {
                    table.PlaceFood(heldFood);
                    heldFood = null;
                    Debug.Log("Placed food on table");
                    return;
                }
            }
        }
        
        // Only drop if no table is nearby, even if it's full/not ready
        if (!foundAnyTable)
        {
            DropFood();
        }
        else
        {
            Debug.Log("Cannot place food on nearby table (not ready or full).");
        }
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
        if (redCubeIngredient != null) 
        {
            redCubeIngredient.SetActive(true); 
        }
    }

    public void ResetInput()
    {
        Input.ResetInputAxes();
    }
}
