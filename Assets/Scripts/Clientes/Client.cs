using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class Client : MonoBehaviour
{
    public GameObject[] clientModels;
    public enum State
    {
        WalkingToEntrance,
        Waiting,
        WalkingToTable,
        WaitingForFood,
        Eating,
        DoneEating,
        Leaving,
        Angry
    }

    public State CurrentState { get; private set; } = State.WalkingToEntrance;
    public float maxPatience = 90f;
    public float maxQueuePatience = 45f;
    public float eatDuration = 8f;
    public int money;
    public int happiness;
    public int nationality;

    [Header("Postura sentado")]
    [Tooltip("Un offset por modelo de clientModels (mismo orden): offset local del " +
             "ModelPivot al sentarse, para asentar cada modelo en la silla. " +
             "Se restaura a cero al levantarse.")]
    public Vector3[] seatOffsets;
    [Tooltip("Segundos que dura Levantarse antes de la reacción o de andar.")]
    [SerializeField] private float _standUpSeconds = 1.0f;
    [Tooltip("Segundos que dura la reacción (Feliz/Enfadado) de pie antes de caminar hacia la salida.")]
    [SerializeField] private float _reactionSeconds = 2.9f;
    [Tooltip("Metros que se aleja andando de la silla antes de soltar la reacción.")]
    [SerializeField] private float _stepOutDistance = 1.75f;
    [Tooltip("Velocidad del pasito de salida (m/s): más lento que el andar normal para que se lea como un paso deliberado.")]
    [SerializeField] private float _stepOutSpeed = 1.6f;


    private ClientGroup _group;
    public ClientGroup Group => _group;
    public bool IsInGroup => _group != null;
    public bool IsGroupLeader => IsInGroup && _group.Members.Count > 0 && _group.Members[0] == this;

    private Table _assignedTable;
    private Transform _seatPoint;
    private Chair _seatChair;
    private Transform _entrancePoint;
    // NavMeshAgent avoidance: lower value = right of way. Seated clients never
    // yield; queued clients leave avoidance entirely (None) so walkers pass
    // through them instead of deadlocking against the group at the door.
    private const int StationaryAvoidancePriority = 25;
    private const int WalkingAvoidancePriority = 75;
    private ObstacleAvoidanceType _walkAvoidanceType;

    // vivos ahora mismo; DayManager.IsWindingDown espera a que llegue a 0.
    // OnEnable/OnDisable (no OnDestroy) para que los descargues de escena también descuenten.
    public static int ActiveCount { get; private set; }

    // Todos los clientes vivos, para poder expulsarlos al cerrar la puerta de entrada.
    public static readonly List<Client> All = new();

    private NavMeshAgent _agent;
    private Vector3 _queueSlotPosition;
    private bool Initialized = false;
    private bool _hasStartedWalking = false;
    // comida terminada y reacción ya reproducidas: solo falta que el grupo
    // entero pueda marcharse (FinishAndLeave espera a esto para andar)
    private bool _canLeave;
    // hay un coroutine de salida en curso (FinishAndLeave/StandUpAndLeave):
    // evita arrancar un segundo (p. ej. expulsión durante el enfado)
    private bool _leaving;
    private Coroutine _departureRoutine;

    private const float StepOutTimeout = 3f;

    private Animator _animator;
    private Transform _modelPivot;
    private Vector3 _activeSeatOffset = Vector3.zero;

    public float PatienceRatio => _group != null ? _group.PatienceRatio : 0f;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.avoidancePriority = WalkingAvoidancePriority;
        _walkAvoidanceType = _agent.obstacleAvoidanceType;
    }

    void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
        ActiveCount++;
    }

    void OnDisable()
    {
        All.Remove(this);
        ActiveCount--;
    }

    private float _timeStateEntered;
    private const float STATE_TIMEOUT = 30f; // Max seconds in any walking state before forcing arrival

    void Start()
    {
        _timeStateEntered = Time.time;
        if (_entrancePoint != null)
        {
            // hold just outside the doorway instead of on it: the door is on
            // everyone's path (arrivals, seatings, exits) and must stay clear
            Vector3 target = _entrancePoint.position;
            Vector3 outward = OutwardDirection();
            if (outward.sqrMagnitude > 0.001f)
                target += outward * Random.Range(1f, 2f);
            target += new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
            WalkTo(target);
        }
    }

    // direction away from the restaurant (toward the spawn side of the queue)
    private Vector3 OutwardDirection()
    {
        var manager = RestaurantManager.Instance;
        return manager != null ? manager.queueDirection.normalized : Vector3.zero;
    }


    public void SetGroup(ClientGroup group)
    {
        _group = group;
    }

    public void SetEntrancePoint(Transform entrance)
    {
        _entrancePoint = entrance;
    }

    void Update()
    {
        if (!Initialized)
        {
            initialize();
        }
        switch (CurrentState)
        {
            case State.WalkingToEntrance:
                if (HasReachedDestination() || HasTimedOut())
                    ArriveAtEntrance();
                break;

            case State.Waiting:
                if (HasReachedDestination())
                    Freeze();
                if (!IsInGroup || IsGroupLeader)
                    TickQueuePatience();
                break;

            case State.WalkingToTable:
                // seat may have been carried off mid-walk
                if (_seatChair != null && !_seatChair.IsPlaced)
                {
                    OnSeatLost();
                    break;
                }
                if (HasReachedDestination() || HasTimedOut())
                    SitDown();
                break;

            case State.DoneEating:
                // Finished eating; just wait for the rest of the group to leave together.
                break;

            case State.Leaving:
            case State.Angry:
                // HasTimedOut evita clientes atascados que bloquean el wind-down
                // (p.ej. agente fuera del navmesh: WalkTo no-op y nunca llegan).
                if (HasReachedDestination() || HasTimedOut())
                    Destroy(gameObject);
                break;
        }

        // Seated patience is group-driven: only the leader ticks it, and it keeps
        // draining while any diner is still waiting for food (plate refills top it up).
        if (IsInGroup && IsGroupLeader && _group.IsWaitingForFood)
        {
            if (_group.TickPatience(Time.deltaTime))
                GroupLeaveAngry();
        }

        if (_animator != null)
            _animator.SetFloat(ClientAnimationParams.Speed, _agent.velocity.magnitude);
    }

    // public so RestaurantManager can force-arrive other group members during group processing
    public void ArriveAtEntrance()
    {
        Freeze();
        RestaurantManager.Instance?.ClientArrived(this);
    }

    public void initialize()
    {
        int randomIndex = Random.Range(0, clientModels.Length);
        GameObject selectedModel = Instantiate(clientModels[randomIndex], transform.position, Quaternion.Euler(0, 0, 0));
        selectedModel.transform.localRotation = Quaternion.Euler(0, 0, 0);

        _activeSeatOffset = seatOffsets != null && randomIndex < seatOffsets.Length
            ? seatOffsets[randomIndex]
            : Vector3.zero;

        // The generated controller animates this transform by path ("ModelPivot"),
        // keeping clip curves independent of the model offsets applied below.
        _modelPivot = new GameObject("ModelPivot").transform;
        _modelPivot.SetParent(transform, false);
        selectedModel.transform.SetParent(_modelPivot, true);

        Initialized = true;
        selectedModel.transform.position = selectedModel.transform.position + new Vector3(0, -0.5f, 0);

        // prefer a rigged model's own Animator; fall back to the placeholder on this root
        _animator = selectedModel.GetComponentInChildren<Animator>();
        if (_animator == null) _animator = GetComponent<Animator>();
        _animator?.SetInteger(ClientAnimationParams.State, (int)CurrentState);
    }

    public void EnterWaitQueue(Vector3 slotPosition)
    {
        _queueSlotPosition = slotPosition;
        if (!IsInGroup || IsGroupLeader) _group?.StartPatience(maxQueuePatience);
        SetState(State.Waiting);
        _timeStateEntered = Time.time;
        WalkTo(slotPosition);
    }

    public void MoveToQueueSlot(Vector3 newSlotPosition)
    {
        _queueSlotPosition = newSlotPosition;
        _timeStateEntered = Time.time;
        WalkTo(newSlotPosition);
    }

    public void AssignTable(Table table, Transform seatPoint)
    {
        if (_seatChair != null && _seatChair.Occupant == this) _seatChair.Occupant = null;

        _assignedTable = table;
        _seatPoint = seatPoint;
        _seatChair = seatPoint != null ? seatPoint.GetComponentInParent<Chair>() : null;
        if (_seatChair != null) _seatChair.Occupant = this;

        SetState(State.WalkingToTable);
        _timeStateEntered = Time.time;
        WalkTo(seatPoint.position);
    }

    private void SitDown()
    {
        Freeze();
        transform.position = _seatPoint.position;
        if (_modelPivot != null) _modelPivot.localPosition = _activeSeatOffset;

        if (_assignedTable != null)
        {
            Vector3 lookPos = _assignedTable.transform.position;
            lookPos.y = transform.position.y;
            transform.LookAt(lookPos);
        }

        if (!IsInGroup || IsGroupLeader) _group?.StartPatience(maxPatience);
        SetState(State.WaitingForFood);
        Debug.Log($"[Client] Seated at {_seatPoint.name}. Patience: {maxPatience}s. Group: {(IsInGroup ? Group.ToString() : "Solo")}");
    }

    private void OnSeatLost()
    {
        Transform newSeat = _assignedTable != null ? _assignedTable.GetFreeSeatPoint() : null;
        if (newSeat != null)
        {
            Debug.Log("[Client] Seat taken mid-trip — moving to another free seat.");
            AssignTable(_assignedTable, newSeat);
        }
        else
        {
            Debug.Log("[Client] Seat taken mid-trip and none left — leaving.");
            HUDMessage.Instance?.ShowWarning("¡Recogieron la silla! El cliente se va.");
            LeaveAngrySelf();
        }
    }

    private void TickQueuePatience()
    {
        if (_group == null) return;
        if (_group.TickPatience(Time.deltaTime))
            RestaurantManager.Instance?.AbandonGroup(Group);
    }

    private void GroupLeaveAngry()
    {
        _assignedTable?.FreeTable(Group);
        if (_group != null)
            foreach (var m in _group.Members)
                m?.LeaveAngrySelf();
        else
            LeaveAngrySelf();
    }

    public void LeaveAngrySelf()
    {
        happiness -= 10;
        ReleaseSeat();
        DayReport.Instance?.RegisterAngryClient();
        HUDMessage.Instance?.ShowBad("¡Cliente se fue enfadado sin pagar!");

        if (CurrentState == State.DoneEating)
        {
            _canLeave = true; // ya reaccionó: FinishAndLeave lo saca a andar
            return;
        }
        if (_leaving) return; // salida ya en curso

        if (IsSeated())
            _departureRoutine = StartCoroutine(StandUpAndLeave(true));
        else
        {
            SetState(State.Angry);
            WalkToExit();
        }
    }

    private void ReleaseSeat()
    {
        if (_seatChair != null && _seatChair.Occupant == this) _seatChair.Occupant = null;
    }

    public void LeaveQueue()
    {
        happiness -= 10;
        DayReport.Instance?.RegisterAngryClient();
        HUDMessage.Instance?.ShowBad("¡Cliente se fue de la cola!");
        SetState(State.Angry);
        WalkToExit();
    }

    public void ReceiveFood()
    {
        if (CurrentState != State.WaitingForFood) return;

        SetState(State.Eating);
        AudioManager.Instance?.PlaySFX("client_eating");
        // En edit mode no hay player loop que avance la corrutina: terminar al instante.
        if (Application.isPlaying)
            StartCoroutine(EatCoroutine());
        else
            FinishEating();
    }

    private IEnumerator EatCoroutine()
    {
        yield return new WaitForSeconds(eatDuration);
        FinishEating();
    }

    private void FinishEating()
    {
        // The group may have left angry while this diner was still in the eating
        // delay; that path already handled payment and leaving.
        if (CurrentState == State.Angry || CurrentState == State.Leaving) return;

        happiness += 10;

        int payment = money > 0 ? money : 20;
        MoneyManager.Instance?.Earn(payment);
        Debug.Log($"[Client] Finished eating. Paid {payment}€ (money field: {money}). (Group: {(IsInGroup ? Group.ToString() : "Solo")})");

        // Los especiales pagan igual pero cuentan como descontentos;
        // para un cliente normal esto siempre devuelve true.
        bool leavesHappy = SpecialClientManager.ClientLeavesHappy(this);

        if (leavesHappy) DayReport.Instance?.RegisterSatisfiedClient();
        else             DayReport.Instance?.RegisterAngryClient();

        DayReport.Instance?.RegisterEarnings(payment);
        AudioManager.Instance?.PlaySFX(leavesHappy ? "client_happy" : "client_angry");

        SetState(State.DoneEating);
        // int 6 ya: Levantarse acaba en Locomotion para el pasito hacia la
        // salida; la reacción (int 5) la lanza la corrutina al llegar.
        PlayAnimatorState(State.Leaving);
        // En edit mode no hay player loop que avance la corrutina.
        if (Application.isPlaying)
            _departureRoutine = StartCoroutine(FinishAndLeave());
        _group?.OnMemberFinishedEating(this);

        // Solo diners leave immediately; group members wait for the last diner.
        if (!IsInGroup)
            StartLeaving();
    }

    public void StartLeaving()
    {
        // Solo el líder o un cliente individual libera la mesa
        if (!IsInGroup || IsGroupLeader)
        {
            _assignedTable?.FreeTable(Group);
        }

        if (_leaving || CurrentState == State.DoneEating)
        {
            _canLeave = true; // FinishAndLeave completa la salida tras la reacción
            return;
        }

        if (IsSeated())
        {
            _departureRoutine = StartCoroutine(StandUpAndLeave(false));
        }
        else
        {
            SetState(State.Leaving);
            WalkToExit();
        }
    }

    /// <summary>Expulsado por el cierre de la puerta de entrada: sale de inmediato sin
    /// pagar y sin contabilizarse como satisfecho ni como enfadado.</summary>
    public void KickOut()
    {
        if (CurrentState == State.Leaving || CurrentState == State.Angry) return;

        // Cierre de puerta: corta el pasito y la reacción y que se marche
        // andando ya; el cierre del día no debe esperar la coreografía entera.
        if (_leaving)
        {
            if (_departureRoutine != null) StopCoroutine(_departureRoutine);
            _departureRoutine = null;
            SetState(State.Leaving);
            WalkToExit();
            return;
        }

        StartLeaving();
    }

    /// <summary>Expulsa a todos los clientes vivos (cierre de la puerta de entrada).</summary>
    public static void KickAll()
    {
        List<Client> clients = new(All);
        foreach (var client in clients)
            if (client != null)
                client.KickOut();
    }

    private bool IsSeated()
        => CurrentState == State.WaitingForFood || CurrentState == State.Eating || CurrentState == State.DoneEating;

    // Comida terminada: Levantarse en la silla, un pasito hacia la salida para
    // despejarla, y la reacción de pie ahí; el grupo espera a _canLeave para
    // el tramo final. CurrentState queda en DoneEating mientras: Leaving
    // destruiría al cliente al llegar al paso.
    private IEnumerator FinishAndLeave()
    {
        _leaving = true;
        Freeze();
        if (_modelPivot != null) _modelPivot.localPosition = Vector3.zero;

        yield return new WaitForSeconds(_standUpSeconds);   // clip Levantarse

        yield return StepOutAndSettle();

        PlayAnimatorState(State.DoneEating);                // Locomotion → Feliz
        yield return new WaitForSeconds(_reactionSeconds);  // clip Feliz, de pie
        yield return new WaitUntil(() => _canLeave);

        SetState(State.Leaving);
        WalkToExit();
    }

    // Enfado (Levantarse → Enfadado) o expulsión sin reacción; mismo paso
    // intermedio para no reaccionar encima de la silla. CurrentState no
    // cambia hasta el final por lo mismo que en FinishAndLeave.
    private IEnumerator StandUpAndLeave(bool angry)
    {
        _leaving = true;
        Freeze();
        if (_modelPivot != null) _modelPivot.localPosition = Vector3.zero;
        PlayAnimatorState(State.Leaving);                   // int 6: clip → Locomotion

        yield return new WaitForSeconds(_standUpSeconds);

        yield return StepOutAndSettle();

        if (angry)
        {
            PlayAnimatorState(State.Angry);                 // Locomotion → Enfadado
            yield return new WaitForSeconds(_reactionSeconds);
        }

        SetState(State.Leaving);
        WalkToExit();
    }

    // Paso corto hacia la salida y frenado al llegar (con tope de tiempo por
    // si el paso queda bloqueado). Va a media velocidad para que el paso se
    // lea como deliberado y no un micro-salto.
    private IEnumerator StepOutAndSettle()
    {
        if (_agent != null) _agent.avoidancePriority = WalkingAvoidancePriority;
        float normalSpeed = _agent != null ? _agent.speed : 0f;
        if (_agent != null) _agent.speed = _stepOutSpeed;
        WalkTo(StepOutTarget());
        float deadline = Time.time + StepOutTimeout;
        yield return new WaitUntil(() =>
            _agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh
            // sin path aún, remainingDistance es 0: exigir hasPath o el paso
            // se "asienta" al instante y Freeze cancela la caminata entera
            || (_agent.hasPath && !_agent.pathPending && _agent.remainingDistance <= 0.35f)
            || Time.time >= deadline);
        if (_agent != null) _agent.speed = normalSpeed;
        Freeze();
    }

    // CurrentState y el int del animator divergen a propósito durante la
    // salida: el estado lógico protege la maquinaria de Update mientras los
    // clips cuentan la historia.
    private void PlayAnimatorState(State s)
    {
        if (_animator != null) _animator.SetInteger(ClientAnimationParams.State, (int)s);
    }

    private Vector3 StepOutTarget()
    {
        Vector3 exit = _entrancePoint != null ? _entrancePoint.position : transform.position + Vector3.back * 10f;
        Vector3 dir = exit - transform.position;
        dir.y = 0f;
        dir = dir.sqrMagnitude > 0.001f ? dir.normalized : transform.forward;
        Vector3 target = transform.position + dir * _stepOutDistance;
        // radio de muestra estrecho: si el punto cae fuera de malla mejor
        // reaccionar en el sitio que acabar desplazado a mitad de paso
        return NavMesh.SamplePosition(target, out NavMeshHit hit, 0.6f, NavMesh.AllAreas)
            ? hit.position
            : transform.position;
    }

    private void WalkToExit()
    {
        Vector3 exitPos = _entrancePoint != null ? _entrancePoint.position : transform.position + Vector3.back * 10f;

        // walk past the doorway before despawning so leavers don't plow through the queue
        Vector3 outward = OutwardDirection();
        if (_entrancePoint != null && outward.sqrMagnitude > 0.001f)
            exitPos += outward * 1.5f;

        WalkTo(exitPos);
    }

    private void WalkTo(Vector3 destination)
    {
        if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.SetDestination(destination);
            _hasStartedWalking = true;
        }
    }

    private void Freeze()
    {
        if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            _agent.ResetPath();
            _agent.isStopped = true;
        }
        if (_agent != null) _agent.velocity = Vector3.zero;
        _hasStartedWalking = false;
    }

    private bool HasReachedDestination()
    {
        if (!_hasStartedWalking) return false;
        if (_agent.pathPending) return false;

        // If the agent can't find a path at all, force arrival
        if (_agent.hasPath && _agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            Debug.LogWarning($"[Client] Path invalid, forcing arrival.");
            return true;
        }

        // If the agent has no path and isn't pathPending, it means it can't reach the destination
        if (!_agent.hasPath && !_agent.pathPending && _agent.remainingDistance <= 0.1f)
        {
            Debug.LogWarning($"[Client] No valid path to destination, forcing arrival.");
            return true;
        }

        if (_agent.remainingDistance <= _agent.stoppingDistance + 0.1f) return true;
        if (_agent.remainingDistance <= 2.0f && _agent.velocity.sqrMagnitude < 0.05f) return true;
        if (_agent.remainingDistance <= 0.5f) return true;

        return false;
    }

    private bool HasTimedOut()
    {
        return Time.time - _timeStateEntered >= STATE_TIMEOUT;
    }

    private void SetState(State newState)
    {
        CurrentState = newState;
        _timeStateEntered = Time.time;
        // _agent puede ser null si Awake no corrió (herramientas de editor)
        if (_agent != null)
        {
            _agent.avoidancePriority = IsStationaryState(newState)
                ? StationaryAvoidancePriority
                : WalkingAvoidancePriority;
            // Waiting = en cola y Leaving/Angry = caminata final: atravesables.
            // Sin esto, un cliente que se va puede quedarse clavado detrás de
            // un comensal sentado (prioridad 25, no ceden) en un hueco estrecho.
            _agent.obstacleAvoidanceType =
                newState == State.Waiting || newState == State.Leaving || newState == State.Angry
                    ? ObstacleAvoidanceType.NoObstacleAvoidance
                    : _walkAvoidanceType;
        }
        if (_animator != null)
            _animator.SetInteger(ClientAnimationParams.State, (int)newState);
    }

    private static bool IsStationaryState(State s) =>
        s == State.Waiting || s == State.WaitingForFood || s == State.Eating || s == State.DoneEating;

    void OnDestroy()
    {
        ReleaseSeat();
        if (_assignedTable != null && (!IsInGroup || IsGroupLeader))
        {
            _assignedTable.FreeTable(Group);
        }
    }
}

/// <summary>
/// Names must match the ClientAnimator controller parameters
/// (Assets/Animations/Clients) or the animator link breaks.
/// </summary>
public static class ClientAnimationParams
{
    public const string Speed = "Speed";
    public const string State = "State";
}