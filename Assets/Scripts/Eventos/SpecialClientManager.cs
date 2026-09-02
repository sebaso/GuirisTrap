using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Cerebro de los clientes especiales: spawn por probabilidad, pedidos, veto de
// ingredientes, rotura y reparación de mobiliario, y diálogos.

public class SpecialClientManager : MonoBehaviour
{
    public static SpecialClientManager Instance { get; private set; }

    [Header("Refs")]
    [Tooltip("Se usa solo para leer su clientPrefab / spawnPoint / entrancePoint. " +
             "Vacío = lo busca en la escena.")]
    [SerializeField] private ClientSpawner _spawner;

    [Tooltip("Tabla receta → tags. Sin ella nadie veta nada.")]
    [SerializeField] private FoodTagCatalogue _tagCatalogue;

    [Header("Pool de clientes especiales")]
    [SerializeField] private SpecialClientData[] _pool;

    [Header("Probabilidad")]
    [SerializeField] private float _rollInterval = 15f;
    [Range(0f, 1f)]
    [SerializeField] private float _chancePerRoll = 0.08f;
    [Tooltip("Segundos de margen al empezar el día antes del primer especial.")]
    [SerializeField] private float _minDelayBeforeFirst = 60f;
    [SerializeField] private int _maxSpecialsPerDay = 1;

    [Header("Coste de reparación del mobiliario roto")]
    [SerializeField] private int _repairCostPerTable = 40;
    [SerializeField] private int _repairCostPerChair = 15;

    [Header("Castigo SpawnMess (regalitos)")]
    [SerializeField] private int _messCount = 4;
    [SerializeField] private float _messRadius = 2.5f;

    [Header("Debug")]
    [Tooltip("Tecla para forzar el spawn del primer especial del pool.")]
    [SerializeField] private Key _debugSpawnKey = Key.F9;

    // Qué grupo es qué cliente especial (en vez de un campo en ClientGroup).
    private readonly Dictionary<ClientGroup, SpecialClientData> _specialGroups = new();
    // Caché del veredicto de decoración por grupo (evaluar una vez por visita).
    private readonly Dictionary<ClientGroup, bool> _decorVerdict = new();
    // Grupos cuyo fallo ya se está resolviendo (evita dobles resoluciones).
    private readonly HashSet<ClientGroup> _resolving = new();
    // Grupos a los que ya se les ha aplicado su pedido especial.
    private readonly HashSet<ClientGroup> _orderedGroups = new();
    // Grupos cuyo desenlace (propina / pulla de despedida) ya se ha narrado.
    private readonly HashSet<ClientGroup> _outcomeClaimed = new();
    // Mobiliario roto pendiente de reparar mañana.
    private readonly List<BrokenPiece> _broken = new();

    private int _spawnedToday;
    private float _rollTimer;
    private bool _subscribedDay;

    private struct BrokenPiece
    {
        public Transform target;
        public Chair chair;          // null si es la mesa
        public Table table;          // null si es una silla
        public Quaternion rotation;
        public Vector3 position;
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_subscribedDay && DayManager.Instance != null)
            DayManager.Instance.OnDayStarted -= OnDayStarted;
    }

    void Update()
    {
        TrySubscribeDay();
        HandleDebugKey();
        PruneDeadGroups();

        if (DayManager.Instance == null || !DayManager.Instance.IsDayActive) return;
        float elapsed = DayManager.Instance.DayDuration - DayManager.Instance.TimeRemaining;
        if (elapsed < _minDelayBeforeFirst) return;
        if (!DifficultyManager.SpecialsAllowed) return;
        if (_spawnedToday >= _maxSpecialsPerDay + DifficultyManager.ExtraSpecials) return;
        if (_pool == null || _pool.Length == 0) return;

        _rollTimer += Time.deltaTime;
        if (_rollTimer < _rollInterval) return;
        _rollTimer = 0f;

        if (Random.value > _chancePerRoll * DifficultyManager.SpecialChanceScale) return;

        Spawn(_pool[Random.Range(0, _pool.Length)]);
    }

    //  Spawn (sin tocar ClientSpawner: usa sus campos públicos)

    public Client Spawn(SpecialClientData data)
    {
        if (data == null) return null;

        if (_spawner == null) _spawner = FindFirstObjectByType<ClientSpawner>();
        if (_spawner == null || _spawner.clientPrefab == null)
        {
            Debug.LogWarning("[SpecialClientManager] No hay ClientSpawner (o le falta clientPrefab).");
            return null;
        }

        int size = Mathf.Clamp(data.groupSize, 1, 4);
        ClientGroup group = new ClientGroup(size);
        Vector3 basePos = _spawner.spawnPoint != null ? _spawner.spawnPoint.position : transform.position;
        Client leader = null;

        for (int i = 0; i < size; i++)
        {
            Vector3 offset = i == 0 ? Vector3.zero : new Vector3(
                Random.Range(-_spawner.groupMemberSpawnOffset, _spawner.groupMemberSpawnOffset),
                0f,
                Random.Range(-_spawner.groupMemberSpawnOffset, _spawner.groupMemberSpawnOffset));

            GameObject obj = Instantiate(_spawner.clientPrefab, basePos + offset, Quaternion.identity);
            Client client = obj.GetComponent<Client>();
            if (client == null) { Destroy(obj); continue; }

            obj.name = $"Client_{data.clientName}_{i + 1}";

            // Ajustes que ya son campos públicos del cliente: cero cambios en Client.cs.
            client.money = data.paymentOverride > 0 ? data.paymentOverride : Random.Range(10, 22);
            if (data.patienceMultiplier > 0f)
            {
                client.maxPatience *= data.patienceMultiplier;
                client.maxQueuePatience *= data.patienceMultiplier;
            }

            group.AddMember(client);
            obj.AddComponent<SpecialClientTag>().Setup(data, group);

            // Al fijar la entrada, el cliente arranca su ciclo y se registra
            // solo en RestaurantManager cuando llega. No hace falta el spawner.
            if (_spawner.entrancePoint != null)
                client.SetEntrancePoint(_spawner.entrancePoint);

            if (leader == null) leader = client;
        }

        if (leader == null) return null;

        _specialGroups[group] = data;
        _spawnedToday++;

        if (!string.IsNullOrEmpty(data.arrivalAnnouncement))
            HUDMessage.Instance?.ShowWarning(ResolveText(data, data.arrivalAnnouncement));

        Debug.Log($"[SpecialClientManager] ¡{data.clientName} x{group.Members.Count} ha llegado!");
        return leader;
    }

    //  Pedido especial (reescribe ClientGroup.Order, sin tocar ClientGroup)

    public bool ApplySpecialOrder(ClientGroup group, SpecialClientData data)
    {
        if (group == null || data == null || group.Order == null) return false;
        if (!_orderedGroups.Add(group)) return false; // ya aplicado

        RecipeData dish = data.orderMode switch
        {
            OrderMode.FixedDish => data.fixedDish,
            OrderMode.Wildcard  => data.surpriseDishPlaceholder,
            _                   => null,
        };
        if (dish == null) return true; // Normal, o falta el asset: pedido aleatorio normal

        for (int i = 0; i < group.Order.Count; i++)
            group.Order[i] = dish;

        Debug.Log($"[SpecialClientManager] Pedido de {data.clientName}: {dish.dishName} x{group.Order.Count}");
        return true;
    }

    //  Servir: veto de ingredientes y comodín (lo llama PlayerController)

    public bool TryInterceptServe(Table table, Food food)
    {
        if (table == null || food == null) return false;

        ClientGroup g = table.OccupyingGroup;
        if (g == null || !_specialGroups.TryGetValue(g, out SpecialClientData data)) return false;

        // ¿Contenido vetado? (Poseidón + pescado)
        FoodTag tags = GetTags(food.recipe);
        if (data.RejectsTags(tags))
        {
            ResolveForbiddenServe(table, g, data, food);
            return true;
        }

        // "Sorpréndeme": lo que traigas pasa a ser lo que quería.
        if (data.orderMode == OrderMode.Wildcard && g.Order != null && food.recipe != null)
        {
            for (int i = 0; i < g.Order.Count; i++)
                g.Order[i] = food.recipe;
        }

        return false;
    }

    public FoodTag GetTags(RecipeData recipe)
        => _tagCatalogue != null ? _tagCatalogue.GetTags(recipe) : FoodTag.None;

    private void ResolveForbiddenServe(Table table, ClientGroup group, SpecialClientData data, Food food)
    {
        if (_resolving.Contains(group)) return;
        _resolving.Add(group);

        if (food != null) Destroy(food.gameObject);

        AudioManager.Instance?.PlaySFX("client_angry");
        Debug.Log($"[SpecialClientManager] ¡{data.clientName} ha recibido un plato prohibido!");

        StartCoroutine(FailSequence(data, table, group));
    }

    private IEnumerator FailSequence(SpecialClientData data, Table table, ClientGroup group)
    {
        yield return PlayLinesRoutine(data, data.failLines);

        switch (data.onFail)
        {
            case FailConsequence.BreakFurniture:
                MakeGroupLeaveAngry(group, table);
                BreakFurniture(data, table);
                break;

            case FailConsequence.SpawnMess:
                MakeGroupLeaveAngry(group, table);
                SpawnMessAround(data, table);
                break;

            default:
                MakeGroupLeaveAngry(group, table);
                break;
        }

        _resolving.Remove(group);
    }

    private void MakeGroupLeaveAngry(ClientGroup group, Table table)
    {
        if (group == null) return;

        table?.FreeTable(group);

        foreach (Client m in group.Members.ToArray())
        {
            if (m == null) continue;
            if (m.CurrentState == Client.State.Angry || m.CurrentState == Client.State.Leaving) continue;
            m.LeaveAngrySelf();
        }
    }

    //  Romper y reparar mobiliario (sin tocar Table.cs ni Chair.cs)

    private void BreakFurniture(SpecialClientData data, Table table)
    {
        if (table == null) return;

        // Las sillas exactas que la mesa tiene registradas. Se piden a la propia
        // mesa (GetSeatPoints devuelve el SeatTransform de cada silla) en vez de
        // buscarlas a ojo: así no hay que adivinar distancias ni capas, y
        // funciona aunque el equipo cambie chairDetectionDistance.
        var chairs = new HashSet<Chair>();
        foreach (Transform seat in table.GetSeatPoints())
        {
            if (seat == null) continue;
            Chair c = seat.GetComponentInParent<Chair>();
            if (c != null) chairs.Add(c);
        }

        // Respaldo por si la mesa usa asientos autogenerados (sin sillas
        // registradas): se buscan con los propios parámetros de la mesa.
        if (chairs.Count == 0)
        {
            Vector3[] dirs = { table.transform.forward, -table.transform.forward,
                               table.transform.right,   -table.transform.right };
            foreach (Vector3 dir in dirs)
            {
                Vector3 checkPos = table.transform.position + dir * table.chairDetectionDistance;
                foreach (Collider hit in Physics.OverlapSphere(checkPos, table.chairDetectionRadius))
                {
                    Chair c = hit.GetComponentInParent<Chair>();
                    if (c != null && c.IsPlaced) chairs.Add(c);
                }
            }
        }

        int chairsBroken = 0;
        foreach (Chair chair in chairs)
        {
            if (chair == null || !chair.IsPlaced) continue;

            _broken.Add(new BrokenPiece
            {
                target = chair.transform,
                chair = chair,
                table = null,
                rotation = chair.transform.localRotation,
                position = chair.transform.localPosition,
            });

            chair.SetCarried(true); // fuera del pool + suelta al ocupante
            chair.transform.localRotation *= Quaternion.Euler(75f, 0f, 15f);
            chair.transform.localPosition += new Vector3(0f, -0.05f, 0f);
            chairsBroken++;
        }

        // La mesa también sale del pool. Hace falta explícitamente: si la mesa
        // tiene autoGenerateSeats, quedarse sin sillas NO la deja a capacidad 0
        // y el RestaurantManager la seguiría usando.
        _broken.Add(new BrokenPiece
        {
            target = table.transform,
            chair = null,
            table = table,
            rotation = table.transform.localRotation,
            position = table.transform.localPosition,
        });

        table.SetCarried(true);
        table.transform.localRotation *= Quaternion.Euler(0f, 0f, 70f);
        table.transform.localPosition += new Vector3(0f, -0.08f, 0f);

        int cost = _repairCostPerTable + chairsBroken * _repairCostPerChair;
        if (MoneyManager.Instance != null && MoneyManager.Instance.TrySpend(cost))
            DayReport.Instance?.RegisterSpending(cost);
        else
            Debug.LogWarning($"[SpecialClientManager] Sin dinero para la reparación ({cost}€). TODO deuda.");

        HUDMessage.Instance?.ShowBad(
            $"¡{data.clientName} ha destrozado la mesa {table.tableNumber}! (-{cost}€ de reparación)");
        Debug.Log($"[SpecialClientManager] Mesa {table.tableNumber} destrozada (+{chairsBroken} sillas).");
    }

    private void RepairAllFurniture()
    {
        foreach (BrokenPiece p in _broken)
        {
            if (p.target == null) continue;   // la escena se recargó: ya no existe
            p.target.localRotation = p.rotation;
            p.target.localPosition = p.position;

            p.chair?.SetCarried(false);       // vuelve al pool de asientos
            p.table?.SetCarried(false);       // vuelve al pool de mesas
        }
        if (_broken.Count > 0) Debug.Log($"[SpecialClientManager] Mobiliario reparado ({_broken.Count} piezas).");
        _broken.Clear();
    }

    private void SpawnMessAround(SpecialClientData data, Table table)
    {
        if (GaviotaEventManager.Instance == null)
        {
            Debug.LogWarning("[SpecialClientManager] SpawnMess sin GaviotaEventManager — no caen regalitos.");
            return;
        }

        Vector3 center = table != null ? table.transform.position : transform.position;
        for (int i = 0; i < _messCount; i++)
        {
            Vector2 r = Random.insideUnitCircle * _messRadius;
            GaviotaEventManager.Instance.SoltarCacaEn(center + new Vector3(r.x, 0f, r.y));
        }

        HUDMessage.Instance?.ShowBad($"¡{data.clientName} lo ha dejado todo perdido de regalitos!");
    }

    //  Final de la visita (lo llama SpecialClientTag)

    public void OnSpecialSatisfied(Client client, SpecialClientData data)
    {
        if (data == null) return;

        int payment = client != null ? client.money : 0;
        HUDMessage.Instance?.ShowGood($"¡{data.clientName} está satisfecho! +{payment}€");

        // Propina de hincha (Guirincianos con tele + banderines).
        if (data.HasDecorCondition && data.tipIfDecorMet > 0)
        {
            MoneyManager.Instance?.Earn(data.tipIfDecorMet);
            DayReport.Instance?.RegisterEarnings(data.tipIfDecorMet);
            HUDMessage.Instance?.ShowGood($"¡Propina extra de hincha! +{data.tipIfDecorMet}€");
        }

        PlayLines(data, data.successLines);
        // Gancho futuro: aquí arrancaría la BATALLA DE POSEIDÓN / la Estatua.
    }

    public void OnSpecialUnhappy(Client client, SpecialClientData data)
    {
        if (data == null) return;

        int paid = client != null ? client.money : 0;
        HUDMessage.Instance?.ShowBad(paid > 0
            ? $"A {data.clientName} no les ha convencido: solo dejan {paid}€ por cabeza."
            : $"A {data.clientName} no les ha convencido tu comida...");

        PlayLines(data, data.unhappyLines);
    }

    //  Condición de decoración (Guirincianos: tele + banderines)

    public bool TryClaimOutcome(ClientGroup group)
    {
        if (group == null) return false;
        return _outcomeClaimed.Add(group);
    }

    public bool GroupIsHappy(ClientGroup group)
    {
        if (group == null) return true;
        if (!_decorVerdict.TryGetValue(group, out bool ok)) return true;
        return ok;
    }

    public static bool ClientLeavesHappy(Client client)
    {
        if (Instance == null || client == null) return true;
        return Instance.GroupIsHappy(client.Group);
    }

    public bool EvaluateDecorCondition(ClientGroup group, SpecialClientData data)
    {
        if (data == null || !data.HasDecorCondition) return true;
        if (group != null && _decorVerdict.TryGetValue(group, out bool cached)) return cached;

        bool ok = CheckDecoration(data);
        if (group != null) _decorVerdict[group] = ok;

        Debug.Log($"[SpecialClientManager] Decoración para {data.clientName}: {(ok ? "CUMPLIDA" : "FALTA")}.");
        return ok;
    }

    private bool CheckDecoration(SpecialClientData data)
    {
        var placed = FindObjectsByType<PlaceableObject>(FindObjectsSortMode.None);
        var candidates = new List<List<Transform>>();

        foreach (PlaceableItemData req in data.requiredDecoration)
        {
            if (req == null) continue;

            var list = new List<Transform>();
            foreach (PlaceableObject p in placed)
                if (p != null && p.GetItemData() == req)
                    list.Add(p.transform);

            if (list.Count == 0) return false;
            candidates.Add(list);
        }

        if (candidates.Count == 0) return true;
        if (data.decorationMaxDistance <= 0f) return true;

        float maxSqr = data.decorationMaxDistance * data.decorationMaxDistance;
        return TrySelectAdjacent(candidates, new List<Transform>(), 0, maxSqr);
    }

    private bool TrySelectAdjacent(List<List<Transform>> cands, List<Transform> chosen, int idx, float maxSqr)
    {
        if (idx == cands.Count) return true;

        foreach (Transform t in cands[idx])
        {
            if (chosen.Contains(t)) continue;

            bool nearAll = true;
            foreach (Transform c in chosen)
                if ((c.position - t.position).sqrMagnitude > maxSqr) { nearAll = false; break; }
            if (!nearAll) continue;

            chosen.Add(t);
            if (TrySelectAdjacent(cands, chosen, idx + 1, maxSqr)) return true;
            chosen.RemoveAt(chosen.Count - 1);
        }
        return false;
    }

    //  Diálogo (reutiliza el pipeline de DialogueManager)

    public void PlayLines(SpecialClientData data, string[] lines)
    {
        if (data == null || lines == null || lines.Length == 0) return;
        StartCoroutine(PlayLinesRoutine(data, lines));
    }

    private string ResolveText(SpecialClientData data, string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        if (data == null || !data.linesAreTranslationKeys) return raw;
        if (TranslateManager.Instance == null) return raw;

        string translated = TranslateManager.Instance.GetTextWithKey(raw);
        return string.IsNullOrEmpty(translated) ? raw : translated;
    }

    private IEnumerator PlayLinesRoutine(SpecialClientData data, string[] lines)
    {
        if (data == null || lines == null || lines.Length == 0) yield break;
        if (DialogueManager.Instance == null) yield break;

        bool anyShown = false;
        foreach (string line in lines)
        {
            if (string.IsNullOrEmpty(line)) continue;
            string text = ResolveText(data, line);
            DialogueReaction.OnDialogueReactionStart?.Invoke(text, data.dialogueColor, data.portrait);
            anyShown = true;
            yield return DialogueManager.Instance.WaitForAdvance();
        }

        if (anyShown)
            DialogueReaction.OnDialogueReactionFinish?.Invoke();
    }

    //  Día y limpieza

    private void TrySubscribeDay()
    {
        if (!_subscribedDay && DayManager.Instance != null)
        {
            DayManager.Instance.OnDayStarted += OnDayStarted;
            _subscribedDay = true;
        }
    }

    private void OnDayStarted()
    {
        _spawnedToday = 0;
        _rollTimer = 0f;
        _specialGroups.Clear();
        _decorVerdict.Clear();
        _resolving.Clear();
        _orderedGroups.Clear();
        _outcomeClaimed.Clear();
        RepairAllFurniture();
    }

    private void PruneDeadGroups()
    {
        if (_specialGroups.Count == 0) return;

        List<ClientGroup> dead = null;
        foreach (var kv in _specialGroups)
        {
            ClientGroup g = kv.Key;
            bool alive = false;
            foreach (Client c in g.Members)
                if (c != null) { alive = true; break; }

            if (!alive) (dead ??= new List<ClientGroup>()).Add(g);
        }

        if (dead == null) return;
        foreach (ClientGroup g in dead)
        {
            _specialGroups.Remove(g);
            _decorVerdict.Remove(g);
            _resolving.Remove(g);
            _orderedGroups.Remove(g);
            _outcomeClaimed.Remove(g);
        }
    }

    private void HandleDebugKey()
    {
        if (_pool == null || _pool.Length == 0) return;
        if (Keyboard.current == null) return;
        if (!Keyboard.current[_debugSpawnKey].wasPressedThisFrame) return;

        Debug.Log($"[SpecialClientManager] DEBUG: forzando spawn de {_pool[0].clientName}.");
        Spawn(_pool[0]);
    }

    [ContextMenu("Forzar cliente especial ahora")]
    private void DebugForceSpawn()
    {
        if (_pool != null && _pool.Length > 0) Spawn(_pool[0]);
    }
}
