using UnityEngine;

public class PlayerController : ControllableMonoBehaviour
{
    [Header("Movement")]
    public float speed    = 5f;
    public float maxSpeed = 10f;
    private Rigidbody rb;
    private Vector3 movementDirection;

    // Cuando es true, el jugador está en un minijuego y no debe moverse.
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
            // Frena en seco durante el minijuego (conserva gravedad en Y).
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

    /// <summary>Llamado por InputManager al entrar a un minijuego: detiene y bloquea el movimiento.</summary>
    public void LockMovement()
    {
        _movementLocked   = true;
        movementDirection = Vector3.zero;
        if (rb != null)
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    /// <summary>Llamado por InputManager al salir de un minijuego: reactiva el movimiento.</summary>
    public void UnlockMovement()
    {
        _movementLocked   = false;
        movementDirection = Vector3.zero;
    }

    public override void OnInteractDown()
    {
        // 0. Si lleva una mesa/silla, intentar soltarla.
        if (_heldPlaceable != null) { TryDropFurniture(); return; }

        // 1. Buscar el interactable MÁS CERCANO (no el primero que devuelva la
        //    física, que es arbitrario y hace que hables con la estación de al lado).
        Collider[] nearby = Physics.OverlapSphere(transform.position, interactionRange);

        FoodStorage    bestStorage = null;
        EspetoMinigame bestEspeto  = null;
        CookingStation bestStation = null;
        float bestStorageDist = float.MaxValue;
        float bestEspetoDist  = float.MaxValue;
        float bestStationDist = float.MaxValue;

        foreach (Collider col in nearby)
        {
            float dist = (col.transform.position - transform.position).sqrMagnitude;

            FoodStorage fs = col.GetComponent<FoodStorage>();
            if (fs != null && dist < bestStorageDist) { bestStorage = fs; bestStorageDist = dist; }

            EspetoMinigame esp = col.GetComponent<EspetoMinigame>();
            if (esp != null && dist < bestEspetoDist) { bestEspeto = esp; bestEspetoDist = dist; }

            CookingStation cs = col.GetComponent<CookingStation>();
            if (cs != null && dist < bestStationDist) { bestStation = cs; bestStationDist = dist; }
        }

        // Atender al más cercano de entre los tipos encontrados.
        // (Storage y Espeto tienen prioridad porque abren su propio menú; la
        //  estación de cocina es la acción de "cocinar" lo que llevas.)
        if (bestStorage != null && bestStorageDist <= bestEspetoDist && bestStorageDist <= bestStationDist)
        {
            bestStorage.TryOpen(); return;
        }
        if (bestEspeto != null && bestEspetoDist <= bestStationDist)
        {
            bestEspeto.TryOpen(this); return;
        }
        if (bestStation != null)
        {
            bestStation.TryInteract(); return;
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

        // ponytail: nearest-first scan; Food collider may live on a child of
        // the prefab root, so GetComponentInParent is required.
        Food bestFood = null;
        float bestFoodDist = float.MaxValue;
        Kitchen bestKitchen = null;
        float bestKitchenDist = float.MaxValue;

        foreach (Collider col in nearbyObjects)
        {
            float dist = (col.transform.position - transform.position).sqrMagnitude;

            Kitchen k = col.GetComponent<Kitchen>();
            if (k != null && dist < bestKitchenDist) { bestKitchen = k; bestKitchenDist = dist; }

            Food f = col.GetComponent<Food>() ?? col.GetComponentInParent<Food>();
            if (f != null && !f.IsBeingHeld && !f.IsServed && dist < bestFoodDist) { bestFood = f; bestFoodDist = dist; }
        }

        if (bestFood != null) { PickUpFood(bestFood); return true; }
        if (bestKitchen != null)
        {
            Food newFood = bestKitchen.GetFood();
            if (newFood != null) { PickUpFood(newFood); return true; }
        }
        return false;
    }

    public void PickUpFood(Food food)
    {
        if (food == null) return;
        if (heldFood != null && heldFood != food) DropFood();
        heldFood = food;
        food.PickUp(holdPoint);
        Debug.Log($"Picked up: {food.foodName}");
    }

    /// <summary>Spawns a food prefab from a minigame/reward and auto-holds it.
    /// Drops whatever was previously held. Used by cooking minigames so the
    /// cooked dish lands directly in the player's hands.</summary>
    public Food CreateAndHoldFood(GameObject foodPrefab)
    {
        if (foodPrefab == null || holdPoint == null) return null;
        if (heldFood != null) DropFood();

        GameObject obj = Instantiate(foodPrefab, holdPoint.position, Quaternion.identity);
        Food food = obj.GetComponent<Food>();
        if (food == null) food = obj.AddComponent<Food>();
        PickUpFood(food);
        return food;
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
                HUDMessage.Instance?.ShowWarning("Mesa ocupada — espera a que el grupo termine.");
                return;
            }
            if (grid != null && grid.HasAdjacentChairs(best.CurrentCellX, best.CurrentCellY))
            {
                Debug.LogWarning("[Carry] Take the chairs out before moving this table.");
                HUDMessage.Instance?.ShowWarning("Quita las sillas antes de mover la mesa.");
                return;
            }
            table?.SetCarried(true);
        }
        else // Chair
        {
            Chair chair = best.GetComponent<Chair>();
            if (chair != null && chair.IsBeingSatOn)
            {
                Debug.LogWarning("[Carry] Someone is sitting or walking to this chair — can't take it.");
                HUDMessage.Instance?.ShowWarning("Alguien está usando esta silla.");
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
            HUDMessage.Instance?.ShowWarning("No puedes ponerlo ahí.");
            return;
        }

        GameGridManager grid = _heldPlaceable.GridManager;
        PlaceableItemData item = _heldPlaceable.GetItemData();
        int x = _dropX, y = _dropY;

        _heldPlaceable.transform.SetParent(null);
        Vector3 localPos = new Vector3(x, 0f, y) + item.placementOffset;
        Vector3 targetWorld = grid.transform.TransformPoint(localPos);
        Quaternion targetRot = item.category == PlaceableCategory.Chair
            ? grid.GetChairRotation(x, y)
            : grid.transform.rotation;

        Collider c = _heldPlaceable.GetComponent<Collider>();
        if (c != null) c.enabled = true;

        if (_ghost != null) { Destroy(_ghost); _ghost = null; }

        grid.SaveGrid(x, y, _heldPlaceable.StartCellX, _heldPlaceable.StartCellY, item, targetRot);
        _heldPlaceable.InstancePlaceableObjectCreated(x, y);

        if (item.category == PlaceableCategory.Chair)
            _heldPlaceable.GetComponent<Chair>()?.SetCarried(false);
        else
            _heldPlaceable.GetComponent<Table>()?.SetCarried(false);

        _heldPlaceable.LerpTo(targetWorld, targetRot);
        _heldPlaceable = null;

        RestaurantManager.Instance?.NotifyTablesRearranged();
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