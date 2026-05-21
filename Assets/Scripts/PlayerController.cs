using UnityEngine;

public class PlayerController : ControllableMonoBehaviour
{
    [Header("Movement")]
    public float speed    = 5f;
    public float maxSpeed = 10f;
    private Rigidbody rb;
    private Vector3 movementDirection;

    [Header("Pickup System")]
    public Transform holdPoint;
    public float interactionRange = 2f;
    private Food heldFood;

    [Header("Minigame System")]
    public RecipeData currentRecipe;
    public GameObject redCubeIngredient;

    [Header("UI Interaction Feedback")]
    public GameObject interactPrompt; // Arrastra aquí el Quad/Sprite flotante que harás de hijo
    public float promptPopupSpeed = 12f; // Velocidad del escalado suave
    private bool isNearInteractable = false;
    private Vector3 originalPromptScale;

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

        if (interactPrompt != null)
        {
        originalPromptScale = interactPrompt.transform.localScale;
        interactPrompt.transform.localScale = Vector3.zero; 
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector3(
            movementDirection.x * speed,
            rb.linearVelocity.y,
            movementDirection.z * speed
        );
        
        if (interactPrompt != null)
    {
        Vector3 targetScale = isNearInteractable ? originalPromptScale : Vector3.zero;
        interactPrompt.transform.localScale = Vector3.Lerp(
            interactPrompt.transform.localScale, 
            targetScale, 
            Time.deltaTime * promptPopupSpeed
        );
    }
    }

    // ── ControllableMonoBehaviour ─────────────────────────────────────────

    public override void OnMove(Vector2 direction)
    {
        movementDirection = new Vector3(direction.x, 0f, direction.y).normalized;
    }

    public override void OnInteractDown()
    {
        // 1. Intentar abrir FoodStorage cercano
        Collider[] nearby = Physics.OverlapSphere(transform.position, interactionRange);
        foreach (Collider col in nearby)
        {
            FoodStorage fs = col.GetComponent<FoodStorage>();
            if (fs != null) { fs.TryOpen(); return; }

            EspetoMinigame esp = col.GetComponent<EspetoMinigame>();
            if (esp != null) { esp.TryOpen(this); return; }

            CookingStation cs = col.GetComponent<CookingStation>();
            if (cs != null) { cs.TryInteract(); return; }
        }

        // 2. Si lleva comida, intentar colocarla
        if (heldFood != null) { TryPlaceOrDropFood(); return; }

        // 3. Intentar recoger comida del suelo
        TryPickUpFood();
    }

    // ── Pickup System ─────────────────────────────────────────────────────

    private void TryPickUpFood()
    {
        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, interactionRange);
        foreach (Collider col in nearbyObjects)
        {
            Kitchen kitchen = col.GetComponent<Kitchen>();
            if (kitchen != null)
            {
                Food newFood = kitchen.GetFood();
                if (newFood != null) { PickUpFood(newFood); return; }
            }

            Food food = col.GetComponent<Food>();
            if (food != null && !food.IsBeingHeld) { PickUpFood(food); return; }
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
            Table table = col.GetComponent<Table>() ?? col.GetComponentInParent<Table>();
            if (table == null) continue;

            foundAnyTable = true;
            if (table.CanPlaceFood())
            {
                table.PlaceFood(heldFood);
                heldFood = null;
                Debug.Log("Placed food on table");
                return;
            }
        }

        if (!foundAnyTable) DropFood();
    }

    private void DropFood()
    {
        if (heldFood == null) return;
        heldFood.Drop();
        heldFood = null;
    }

    public bool IsHoldingFood() => heldFood != null;
    public Food GetHeldFood()   => heldFood;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }

    public void SetCurrentIngredients(RecipeData data)
    {
        currentRecipe = data;
        if (redCubeIngredient != null) redCubeIngredient.SetActive(true);
    }
    public void SetNearInteractable(bool near)
    {
    isNearInteractable = near;
    }

    public void ResetInput() { }
}