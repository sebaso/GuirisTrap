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
        WalkingToEntrance,  // Heading from spawn to restaurant entrance
        Waiting,            // Queued at entrance — no table available yet
        WalkingToTable,     // Told where to sit, navigating to seat
        WaitingForFood,     // Seated, patience ticking down
        Eating,             // Food arrived — eating timer running
        Leaving,            // Walking to exit, will be destroyed on arrival
        Angry               // Patience ran out — leaving unhappy
    }

    public State CurrentState { get; private set; } = State.WalkingToEntrance;
    public float maxPatience = 60f;
    public float maxQueuePatience = 45f; // patience while waiting in the entrance queue for a free table
    public float eatDuration = 8f;
    public int money;
    public int happiness;
    public int nationality;


    private ClientGroup _group;
    public ClientGroup Group => _group;
    public bool IsInGroup => _group != null;
    public bool IsGroupLeader => IsInGroup && _group.Members.Count > 0 && _group.Members[0] == this;

    private Table _assignedTable;
    private Transform _seatPoint;
    private Chair _seatChair;
    private Transform _entrancePoint;
    private NavMeshAgent _agent;
    private Vector3 _queueSlotPosition;
    private bool Initialized = false;
    private bool _hasStartedWalking = false;

    // reads the group's shared timer
    public float PatienceRatio => _group != null ? _group.PatienceRatio : 0f;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    private float _timeStateEntered;
    private const float STATE_TIMEOUT = 30f; // Max seconds in any walking state before forcing arrival

    void Start()
    {
        _timeStateEntered = Time.time;
        if (_entrancePoint != null)
        {
            Vector3 offset = new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
            WalkTo(_entrancePoint.position + offset);
        }
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
                // leader runs the group's queue timer
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

            case State.WaitingForFood:
                // leader runs the group's meal timer
                if (!IsInGroup || IsGroupLeader)
                    TickSeatedPatience();
                break;

            case State.Leaving:
            case State.Angry:
                if (HasReachedDestination())
                    Destroy(gameObject);
                break;
        }
    }

    /// <summary>
    /// Called when the client has reached the entrance. Makes this client enter the waiting state
    /// and notifies RestaurantManager to handle seating/queue logic.
    /// Public so RestaurantManager can force-arrive other group members during group processing.
    /// </summary>
    public void ArriveAtEntrance()
    {
        Freeze();
        RestaurantManager.Instance?.ClientArrived(this);
    }

    public void initialize()
    {
        int randomIndex = Random.Range(0, clientModels.Length);
        GameObject selectedModel = Instantiate(clientModels[randomIndex], transform.position, Quaternion.Euler(0, 0, 0));
        selectedModel.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        selectedModel.transform.SetParent(transform);
        Initialized = true;
        selectedModel.transform.position = selectedModel.transform.position + new Vector3(0, -0.5f, 0);
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
        // drop old claim, claim the new chair so others/the player see it's taken
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

        if (_assignedTable != null)
        {
            Vector3 lookPos = _assignedTable.transform.position;
            lookPos.y = transform.position.y;
            transform.LookAt(lookPos);
        }

        // leader (re)starts the shared meal timer
        if (!IsInGroup || IsGroupLeader) _group?.StartPatience(maxPatience);
        SetState(State.WaitingForFood);
        Debug.Log($"[Client] Seated at {_seatPoint.name}. Patience: {maxPatience}s. Group: {(IsInGroup ? Group.ToString() : "Solo")}");
    }

    // seat gone mid-walk: take another at the same table, else leave
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
            LeaveAngrySelf();
        }
    }

    // ponytail: only the leader ticks — fine, a leader leaves only when the whole group does
    private void TickSeatedPatience()
    {
        if (_group == null) return;
        if (_group.TickPatience(Time.deltaTime))
            GroupLeaveAngry();
    }

    private void TickQueuePatience()
    {
        if (_group == null) return;
        if (_group.TickPatience(Time.deltaTime))
            RestaurantManager.Instance?.AbandonGroup(Group);
    }

    // patience out: free the table once, everyone leaves angry
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
        SetState(State.Angry);
        WalkToExit();
    }

    // release seat claim on exit so the chair frees up
    private void ReleaseSeat()
    {
        if (_seatChair != null && _seatChair.Occupant == this) _seatChair.Occupant = null;
    }

    // gave up queueing; no table to free
    public void LeaveQueue()
    {
        happiness -= 10;
        SetState(State.Angry);
        WalkToExit();
    }

    public void ReceiveFood()
    {
        if (CurrentState != State.WaitingForFood) return;

        SetState(State.Eating);
        Debug.Log($"[Client] Food received! Eating... (Group: {(IsInGroup ? Group.ToString() : "Solo")})");
        StartCoroutine(EatCoroutine());
    }

    private IEnumerator EatCoroutine()
    {
        yield return new WaitForSeconds(eatDuration);
        LeaveHappy();
    }

    private void LeaveHappy()
    {
        happiness += 10;

        // ponytail: flat payout for now; tip/bill by happiness later
        const int HAPPY_PAYMENT = 20;
        CashManager.Instance?.Earn(HAPPY_PAYMENT);
        Debug.Log($"[Client] Finished eating. Leaving happy. Paid {HAPPY_PAYMENT}€. (Group: {(IsInGroup ? Group.ToString() : "Solo")})");

        StartLeaving();
    }

    private void StartLeaving()
    {
        ReleaseSeat();

        // Solo el líder o un cliente individual libera la mesa
        if (!IsInGroup || IsGroupLeader)
        {
            _assignedTable?.FreeTable(Group);
        }

        SetState(State.Leaving);
        WalkToExit();
    }

    private void WalkToExit()
    {
        Vector3 exitPos = _entrancePoint != null ? _entrancePoint.position : transform.position + Vector3.back * 10f;
        WalkTo(exitPos);
    }

    private void WalkTo(Vector3 destination)
    {
        if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.SetDestination(destination);
            _hasStartedWalking = true;
        }
    }

    private void Freeze()
    {
        if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            _agent.ResetPath();
            _agent.isStopped = true;
        }
        _agent.velocity = Vector3.zero;
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
    }

    void OnDestroy()
    {
        ReleaseSeat();
        if (_assignedTable != null && (!IsInGroup || IsGroupLeader))
        {
            _assignedTable.FreeTable(Group);
        }
    }
}