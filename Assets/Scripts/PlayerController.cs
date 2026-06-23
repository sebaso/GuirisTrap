using UnityEngine;

public class PlayerController : ControllableMonoBehaviour
{
    [Header("Movement")]
    public float speed    = 5f;
    public float maxSpeed = 10f;
    private Rigidbody rb;
    private Vector3 movementDirection;

    // Cuando es true, el jugador está dentro de un minijuego y no debe moverse.
    private bool _movementLocked = false;

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
        if (_movementLocked)
        {
            // Frena en seco mientras estamos en un minijuego (conserva gravedad en Y).
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
        else
        {
            rb.linearVelocity = new Vector3(
                -movementDirection.x * speed,
                rb.linearVelocity.y,
                -movementDirection.z * speed
            );
        }

        // facing = actual travel dir, so carried items drop in front, not on the player
        Vector3 horiz = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (horiz.sqrMagnitude > 0.01f) _lastFacing = horiz.normalized;

        if (interactPrompt != null)
    {
        Vector3 targetScale = isNearInteractable ? originalPromptScale : Vector3.zero;
        interactPrompt.transform.localScale = Vector3.Lerp(
            interactPrompt.transform.localScale,
            targetScale,
            Time.deltaTime * promptPopupSpeed
        );
    }

        if (_heldPlaceable != null) UpdateCarryPreview();
    }

    // ── ControllableMonoBehaviour ─────────────────────────────────────────

    public override void OnMove(Vector2 direction)
    {
        if (_movementLocked)
        {
            movementDirection = Vector3.zero;
            return;
        }
        movementDirection = new Vector3(direction.x, 0f, direction.y).normalized;
    }

    /// <summary>Llamar al entrar a un minijuego: detiene y bloquea el movimiento.</summary>
    public void LockMovement()
    {
        _movementLocked   = true;
        movementDirection = Vector3.zero;
        if (rb != null)
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    /// <summary>Llamar al salir de un minijuego: reactiva el movimiento.</summary>
    public void UnlockMovement()
    {
        _movementLocked   = false;
        movementDirection = Vector3.zero;
    }

    public override void OnInteractDown()
    {
        // 0. Si lleva una mesa/silla, intentar soltarla.
        if (_heldPlaceable != null) { TryDropFurniture(); return; }

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
        if (TryPickUpFood()) return;

        // 4. Intentar recoger una mesa o silla para reorganizar.
        TryPickUpFurniture();
    }

    // ── Pickup System ─────────────────────────────────────────────────────

    private bool TryPickUpFood()
    {
        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, interactionRange);
        foreach (Collider col in nearbyObjects)
        {
            Kitchen kitchen = col.GetComponent<Kitchen>();
            if (kitchen != null)
            {
                Food newFood = kitchen.GetFood();
                if (newFood != null) { PickUpFood(newFood); return true; }
            }

            Food food = col.GetComponent<Food>();
            if (food != null && !food.IsBeingHeld) { PickUpFood(food); return true; }
        }
        return false;
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

    // ── Furniture Carry ───────────────────────────────────────────────────
    // Day-time table/chair moving; same placement rules as the editor (CanPlaceItem + SaveGrid).

    private PlaceableObject _heldPlaceable;
    private int _dropX, _dropY;
    private bool _dropValid;
    private Vector3 _lastFacing = Vector3.forward;
    public float carryLerpSpeed = 12f;
    private GameObject _ghost; // translucent drop-spot preview
    private static readonly Color GhostOk  = new Color(0.3f, 1f, 0.3f, 0.45f);
    private static readonly Color GhostBad = new Color(1f, 0.3f, 0.3f, 0.45f);

    private void TryPickUpFurniture()
    {
        Collider[] nearby = Physics.OverlapSphere(transform.position, interactionRange);
        PlaceableObject best = null;
        float bestDist = float.MaxValue;

        foreach (Collider col in nearby)
        {
            PlaceableObject p = col.GetComponentInParent<PlaceableObject>();
            if (p == null || p.GetItemData() == null) continue;

            PlaceableCategory cat = p.GetItemData().category;
            if (cat != PlaceableCategory.Table && cat != PlaceableCategory.Chair) continue;

            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < bestDist) { bestDist = d; best = p; }
        }

        if (best == null) return;

        GameGridManager grid = best.GridManager;
        PlaceableCategory category = best.GetItemData().category;

        if (category == PlaceableCategory.Table)
        {
            Table table = best.GetComponent<Table>();
            if (table != null && table.IsOccupied)
            {
                Debug.LogWarning("[Carry] That table is in use — wait until the group leaves.");
                return;
            }
            if (grid != null && grid.HasAdjacentChairs(best.CurrentCellX, best.CurrentCellY))
            {
                Debug.LogWarning("[Carry] Take the chairs out before moving this table.");
                return;
            }
            table?.SetCarried(true);
        }
        else // Chair
        {
            Chair chair = best.GetComponent<Chair>();
            if (chair != null && chair.IsBeingSatOn)
            {
                Debug.LogWarning("[Carry] Someone is sitting on this chair — can't take it.");
                return;
            }
            chair?.SetCarried(true);
        }

        _heldPlaceable = best;

        Collider c = best.GetComponent<Collider>();
        if (c != null) c.enabled = false;

        // keep world pos so it glides to the head instead of snapping
        best.transform.SetParent(holdPoint, worldPositionStays: true);

        _ghost = CreateGhost(best.GetItemData());
    }

    // logic/physics-free clone, just to show where the item will land
    private GameObject CreateGhost(PlaceableItemData item)
    {
        if (item == null || item.prefab == null) return null;

        GameObject g = Instantiate(item.prefab);
        foreach (var mb in g.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false;
        foreach (var col in g.GetComponentsInChildren<Collider>()) col.enabled = false;
        return g;
    }

    private void TintGhost(Color color)
    {
        if (_ghost == null) return;
        foreach (var r in _ghost.GetComponentsInChildren<Renderer>())
            r.material.color = color;
    }

    private void TryDropFurniture()
    {
        if (!_dropValid)
        {
            Debug.LogWarning("[Carry] Can't put it there.");
            return;
        }

        GameGridManager grid = _heldPlaceable.GridManager;
        PlaceableItemData item = _heldPlaceable.GetItemData();
        int x = _dropX, y = _dropY;

        _heldPlaceable.transform.SetParent(null);
        Vector3 localPos = new Vector3(x, 0f, y) + item.placementOffset;
        _heldPlaceable.transform.position = grid.transform.TransformPoint(localPos);
        _heldPlaceable.transform.rotation = grid.transform.rotation;

        Collider c = _heldPlaceable.GetComponent<Collider>();
        if (c != null) c.enabled = true;

        if (_ghost != null) { Destroy(_ghost); _ghost = null; }

        if (item.category == PlaceableCategory.Chair)
            grid.RotateTowardsTable(_heldPlaceable, x, y);

        grid.SaveGrid(x, y, _heldPlaceable.StartCellX, _heldPlaceable.StartCellY, item, _heldPlaceable.transform.rotation);
        _heldPlaceable.InstancePlaceableObjectCreated(x, y);

        if (item.category == PlaceableCategory.Chair)
            _heldPlaceable.GetComponent<Chair>()?.SetCarried(false);
        else
            _heldPlaceable.GetComponent<Table>()?.SetCarried(false);

        RestaurantManager.Instance?.NotifyTablesRearranged();
        _heldPlaceable = null;
    }

    private void UpdateCarryPreview()
    {
        _heldPlaceable.transform.localPosition = Vector3.Lerp(
            _heldPlaceable.transform.localPosition, Vector3.zero, Time.fixedDeltaTime * carryLerpSpeed);

        GameGridManager grid = _heldPlaceable.GridManager;
        if (grid == null) { _dropValid = false; return; }

        // one cell ahead of the player
        Vector3 targetWorld = transform.position + _lastFacing;
        Vector3 local = grid.transform.InverseTransformPoint(targetWorld);
        _dropX = Mathf.FloorToInt(local.x);
        _dropY = Mathf.FloorToInt(local.z);

        PlaceableItemData item = _heldPlaceable.GetItemData();
        // chairs ignore adjacency rules so they can be staged anywhere to clear a table
        _dropValid = grid.CanPlaceItem(_dropX, _dropY, _heldPlaceable.StartCellX, _heldPlaceable.StartCellY, item, ignoreChairRules: true);

        if (_ghost != null)
        {
            Vector3 ghostLocal = new Vector3(_dropX, 0f, _dropY) + item.placementOffset;
            _ghost.transform.position = grid.transform.TransformPoint(ghostLocal);
            _ghost.transform.rotation = item.category == PlaceableCategory.Chair
                ? grid.GetChairRotation(_dropX, _dropY)
                : grid.transform.rotation;
            TintGhost(_dropValid ? GhostOk : GhostBad);
        }
    }

    private void OnDestroy()
    {
        if (_ghost != null) Destroy(_ghost);
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