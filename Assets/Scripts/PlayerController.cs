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
    public bool IsCarryingFood => heldFood != null;

    [Header("Minigame System")]
    public RecipeData currentRecipe;
    public GameObject redCubeIngredient;

    [Header("UI Interaction Feedback")]
    public GameObject interactPrompt; // Arrastra aquí el Quad/Sprite flotante que harás de hijo
    public float promptPopupSpeed = 12f; // Velocidad del escalado suave
    private bool isNearInteractable = false;
    private Vector3 originalPromptScale;
    [Header("Furniture Carry — Colocación")]
    [SerializeField] 
    private FloorGridProjection _floorProjection;
    [SerializeField] 
    private LayerMask _furnitureObstacleMask;
    public float dropDistance = 1.2f;
    public float dropCheckRadius = 0.45f;
    private Vector3 _dropTargetPos;
    private Quaternion _dropTargetRot;

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

        FoodStorage    bestStorage  = null;
        EspetoMinigame bestEspeto   = null;
        CookingStation bestStation  = null;
        ExtintorPickup bestExtintor = null;
        ExtintorSoporte bestSoporte = null;
        FregonaPickup  bestFregona  = null;
        FregonaSoporte bestFregSop  = null;
        CacaGaviota    bestCaca     = null;
        float bestFregonaDist  = float.MaxValue;
        float bestFregSopDist  = float.MaxValue;
        float bestCacaDist     = float.MaxValue;
        float bestStorageDist  = float.MaxValue;
        float bestEspetoDist   = float.MaxValue;
        float bestStationDist  = float.MaxValue;
        float bestExtintorDist = float.MaxValue;
        float bestSoporteDist  = float.MaxValue;
        // Track the nearest pickable food too, so a station standing within range
        // can't silently swallow the interact when food is actually closer.
        float bestFoodDist = float.MaxValue;

        foreach (Collider col in nearby)
        {
            float dist = (col.transform.position - transform.position).sqrMagnitude;

            FoodStorage fs = col.GetComponent<FoodStorage>();
            if (fs != null && dist < bestStorageDist) { bestStorage = fs; bestStorageDist = dist; }

            EspetoMinigame esp = col.GetComponent<EspetoMinigame>();
            if (esp != null && dist < bestEspetoDist) { bestEspeto = esp; bestEspetoDist = dist; }

            CookingStation cs = col.GetComponent<CookingStation>();
            if (cs != null && dist < bestStationDist) { bestStation = cs; bestStationDist = dist; }

            // Extintor físico: si NO llevas uno, se puede coger. Si SÍ lo
            // llevas, interactuar con un soporte lo devuelve a su sitio.
            if (!ExtintorPickup.IsPlayerCarrying)
            {
                ExtintorPickup ext = col.GetComponent<ExtintorPickup>() ?? col.GetComponentInParent<ExtintorPickup>();
                if (ext != null && !ext.IsCarried && dist < bestExtintorDist) { bestExtintor = ext; bestExtintorDist = dist; }
            }
            else
            {
                ExtintorSoporte sop = col.GetComponent<ExtintorSoporte>() ?? col.GetComponentInParent<ExtintorSoporte>();
                if (sop != null && dist < bestSoporteDist) { bestSoporte = sop; bestSoporteDist = dist; }
            }

            // Fregona: mismo patrón que el extintor. Si la llevas, pulsar E
            // cerca de una caca de gaviota la limpia.
            if (!FregonaPickup.IsPlayerCarrying)
            {
                FregonaPickup fre = col.GetComponent<FregonaPickup>() ?? col.GetComponentInParent<FregonaPickup>();
                if (fre != null && !fre.IsCarried && dist < bestFregonaDist) { bestFregona = fre; bestFregonaDist = dist; }
            }
            else
            {
                FregonaSoporte fsop = col.GetComponent<FregonaSoporte>() ?? col.GetComponentInParent<FregonaSoporte>();
                if (fsop != null && dist < bestFregSopDist) { bestFregSop = fsop; bestFregSopDist = dist; }

                CacaGaviota caca = col.GetComponent<CacaGaviota>() ?? col.GetComponentInParent<CacaGaviota>();
                if (caca != null && dist < bestCacaDist) { bestCaca = caca; bestCacaDist = dist; }
            }

            // Only loose, grabbable food counts (mirrors TryPickUpFood's filter).
            if (heldFood == null)
            {
                Food f = col.GetComponent<Food>() ?? col.GetComponentInParent<Food>();
                if (f != null && !f.IsBeingHeld && !f.IsServed && dist < bestFoodDist) bestFoodDist = dist;
            }
        }

        // Atender al más cercano de entre los tipos encontrados.
        // (Storage y Espeto tienen prioridad porque abren su propio menú; la
        //  estación de cocina es la acción de "cocinar" lo que llevas.)
        // Cada estación solo gana si además está más cerca que la comida suelta;
        // si la comida es lo más cercano, caemos a TryPickUpFood más abajo.
        if (bestExtintor != null && bestExtintorDist <= bestStorageDist && bestExtintorDist <= bestEspetoDist && bestExtintorDist <= bestStationDist && bestExtintorDist <= bestFoodDist
            && bestExtintorDist <= bestFregonaDist && bestExtintorDist <= bestCacaDist && bestExtintorDist <= bestFregSopDist)
        {
            bestExtintor.TryPickUp(this); return;
        }
        // Coger la fregona de su soporte.
        if (bestFregona != null && bestFregonaDist <= bestStorageDist && bestFregonaDist <= bestEspetoDist && bestFregonaDist <= bestStationDist && bestFregonaDist <= bestFoodDist)
        {
            bestFregona.TryPickUp(this); return;
        }
        // Limpiar una caca de gaviota con la fregona.
        if (bestCaca != null && bestCacaDist <= bestStorageDist && bestCacaDist <= bestEspetoDist && bestCacaDist <= bestStationDist && bestCacaDist <= bestFoodDist && bestCacaDist <= bestFregSopDist)
        {
            bestCaca.LimpiarConFregona(); return;
        }
        // Devolver la fregona a su soporte.
        if (bestFregSop != null && bestFregSopDist <= bestStorageDist && bestFregSopDist <= bestEspetoDist && bestFregSopDist <= bestStationDist && bestFregSopDist <= bestFoodDist)
        {
            FregonaPickup.Carried?.ReturnToHolder();
            AudioManager.Instance?.PlaySFX("fregona_pickup");
            HUDMessage.Instance?.ShowGood("Fregona devuelta a su soporte.");
            return;
        }
        // Devolver el extintor a su soporte pulsando E (si lo llevas encima).
        if (bestSoporte != null && bestSoporteDist <= bestStorageDist && bestSoporteDist <= bestEspetoDist && bestSoporteDist <= bestStationDist && bestSoporteDist <= bestFoodDist)
        {
            ExtintorPickup.Carried?.ReturnToHolder();
            AudioManager.Instance?.PlaySFX("extintor_pickup");
            HUDMessage.Instance?.ShowGood("Extintor devuelto a su soporte.");
            return;
        }
        if (bestStorage != null && bestStorageDist <= bestEspetoDist && bestStorageDist <= bestStationDist && bestStorageDist <= bestFoodDist)
        {
            bestStorage.TryOpen(); return;
        }
        if (bestEspeto != null && bestEspetoDist <= bestStationDist && bestEspetoDist <= bestFoodDist)
        {
            bestEspeto.TryOpen(this); return;
        }
        if (bestStation != null && bestStationDist <= bestFoodDist)
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

    /// <summary>Pierde el plato que lleva (impacto de caca de gaviota).</summary>
    public void LoseHeldFood()
    {
        if (heldFood == null) return;
        Destroy(heldFood.gameObject);
        heldFood = null;
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
    /// <param name="source">The recipe that produced this plate, so orders can
    /// be matched by reference. If null, resolved from the catalogue by
    /// <paramref name="foodPrefab"/> (covers the Espeto minigame, which cooks a
    /// fixed prefab with no recipe in scope).</param>
    public Food CreateAndHoldFood(GameObject foodPrefab, RecipeData source = null)
    {
        if (foodPrefab == null || holdPoint == null) return null;
        if (heldFood != null) DropFood();

        GameObject obj = Instantiate(foodPrefab, holdPoint.position, Quaternion.identity);
        Food food = obj.GetComponent<Food>();
        if (food == null) food = obj.AddComponent<Food>();
        // Stamp the plate with its source recipe so Table.PlaceFood can match it
        // against a group's order by reference (never by foodName string).
        food.recipe = source != null ? source : RecipeCatalogue.Instance?.FindByPrefab(foodPrefab);
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
                // PlaceFood now returns false when the plate is a dish the
                // group didn't order — in that case keep holding it so the
                // player can carry it to the right table.
                // Clientes especiales: veto de ingredientes (Poseidón + pescado)
                // y pedidos "sorpréndeme". Si se queda el plato, lo perdemos.
                if (SpecialClientManager.Instance != null &&
                    SpecialClientManager.Instance.TryInterceptServe(table, heldFood))
                {
                    heldFood = null;
                    return;
                }

                if (table.PlaceFood(heldFood))
                {
                    heldFood = null;
                    Debug.Log("Placed food on table");
                }
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

        PlaceableCategory category = best.GetItemData().category;

        if (category == PlaceableCategory.Table)
        {
            Table table = best.GetComponent<Table>();
            if (table != null && table.IsOccupied)
            {
                HUDMessage.Instance?.ShowWarning("Mesa ocupada — espera a que el grupo termine.");
                return;
            }

            if (table != null && table.GetSeatPoints().Count > 0)
            {
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
                HUDMessage.Instance?.ShowWarning("Alguien está usando esta silla.");
                return;
            }
            chair?.SetCarried(true);
        }

        _heldPlaceable = best;

        Collider c = best.GetComponent<Collider>();
        if (c != null) c.enabled = false;

        best.transform.SetParent(holdPoint, worldPositionStays: true);

        Vector3 initialGhostPos = transform.position + _lastFacing * dropDistance;
        _ghost = CreateGhost(best.GetItemData(), initialGhostPos, Quaternion.LookRotation(_lastFacing, Vector3.up));
    }

    private GameObject CreateGhost(PlaceableItemData item, Vector3 initialPos, Quaternion initialRot)
    {
        if (item == null || item.prefab == null) return null;

        GameObject g = Instantiate(item.prefab, initialPos, initialRot);
        foreach (var mb in g.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false;
        foreach (var col in g.GetComponentsInChildren<Collider>()) col.enabled = false;

        Shader holo = Shader.Find("Guiri/Hologram");
        if (holo != null)
        {
            foreach (var r in g.GetComponentsInChildren<Renderer>())
            {
                Material[] mats = r.materials;
                for (int i = 0; i < mats.Length; i++) mats[i] = new Material(holo);
                r.materials = mats;
            }
        }

        return g;
    }

    private void TintGhost(Color color)
    {
        if (_ghost == null) return;
        foreach (var r in _ghost.GetComponentsInChildren<Renderer>())
            foreach (var m in r.materials)
                if (m.HasProperty("_Color")) m.color = color;
    }

    private void TryDropFurniture()
    {
        if (!_dropValid)
        {
            HUDMessage.Instance?.ShowWarning("No puedes ponerlo ahí.");
            return;
        }

        PlaceableItemData item = _heldPlaceable.GetItemData();

        Quaternion finalRot = item.category == PlaceableCategory.Chair
            ? GetChairDropRotation(_dropTargetPos, _dropTargetRot)
            : _dropTargetRot;

        _heldPlaceable.transform.SetParent(null);
        _heldPlaceable.transform.position = _dropTargetPos;
        _heldPlaceable.transform.rotation = finalRot;

        Collider c = _heldPlaceable.GetComponent<Collider>();
        if (c != null) c.enabled = true;

        if (_ghost != null) { Destroy(_ghost); _ghost = null; }

        if (item.category == PlaceableCategory.Chair)
        {
            _heldPlaceable.GetComponent<Chair>()?.SetCarried(false);
        }
        else
        {
            _heldPlaceable.GetComponent<Table>()?.SetCarried(false);
            RealignNearbyChairs(_dropTargetPos);
        }

        RestaurantManager.Instance?.NotifyTablesRearranged();

        _heldPlaceable = null;
    }

    private void RealignNearbyChairs(Vector3 tablePos)
    {
        Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };

        foreach (var dir in directions)
        {
            Vector3 checkPos = tablePos + dir * 1.1f;
            Collider[] hits = Physics.OverlapSphere(checkPos, 0.5f, _furnitureObstacleMask);

            foreach (var hit in hits)
            {
                Chair chair = hit.GetComponentInParent<Chair>();
                if (chair != null && chair.IsPlaced)
                {
                    chair.transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);
                }
            }
        }
    }

    private void UpdateCarryPreview()
    {
        _heldPlaceable.transform.localPosition = Vector3.Lerp(
            _heldPlaceable.transform.localPosition, Vector3.zero, Time.fixedDeltaTime * carryLerpSpeed);

        Vector3 targetWorld = transform.position + _lastFacing * dropDistance;
        targetWorld.y = transform.position.y; // suelo plano; ajusta si tu sala tiene desniveles

        bool withinRoom = _floorProjection != null && _floorProjection.TryGetVoxelAtWorldPos(targetWorld, out _);

        bool overlapsFurniture = Physics.CheckSphere(targetWorld, dropCheckRadius, _furnitureObstacleMask);

        _dropValid = withinRoom && !overlapsFurniture;
        _dropTargetPos = targetWorld;
        _dropTargetRot = Quaternion.LookRotation(_lastFacing, Vector3.up);

        if (_ghost != null)
        {
            _ghost.transform.position = targetWorld;
            _ghost.transform.rotation = _dropTargetRot;
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

    private Quaternion GetChairDropRotation(Vector3 dropPos, Quaternion fallbackRot)
    {
        Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };

        foreach (var dir in directions)
        {
            Vector3 checkPos = dropPos + dir * 1.1f;
            Collider[] hits = Physics.OverlapSphere(checkPos, 0.5f, _furnitureObstacleMask);

            foreach (var hit in hits)
            {
                Table table = hit.GetComponentInParent<Table>();
                if (table != null && table.IsPlaced)
                {
                    return Quaternion.LookRotation(dir, Vector3.up);
                }
            }
        }

        return fallbackRot;
    }
}