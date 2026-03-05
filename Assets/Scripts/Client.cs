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
    public float eatDuration  = 8f;
    public int money;
    public int happiness;
    public int nationality;
    private float _patience;
    private Table _assignedTable;
    private Transform _seatPoint;
    private Transform _entrancePoint;
    private NavMeshAgent _agent;
    private Vector3 _queueSlotPosition;
    private bool Initialized = false;

    public float PatienceRatio => _patience / maxPatience;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _patience = maxPatience;


        
    }

    void Start()
    {
        RestaurantManager.Instance?.OnClientSpawned(this);
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
                if (HasReachedDestination())
                    ArriveAtEntrance();
                break;

            case State.Waiting:    
                if (HasReachedDestination())
                    Freeze();
                break;

            case State.WalkingToTable:
                if (HasReachedDestination())
                    SitDown();
                break;

            case State.WaitingForFood:
                TickPatience();
                break;

            case State.Leaving:
            case State.Angry:
                if (HasReachedDestination())
                    Destroy(gameObject);
                break;
        }
    }

    private void ArriveAtEntrance()
    {
        Freeze();
        RestaurantManager.Instance?.ClientArrived(this);
    }
    public void initialize()
    {
        int randomIndex = Random.Range(0, clientModels.Length);
        GameObject selectedModel = Instantiate(clientModels[randomIndex-1], transform.position, Quaternion.Euler(0, 0, 0));
        selectedModel.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        selectedModel.transform.SetParent(transform);
        Initialized = true;
        selectedModel.transform.position = selectedModel.transform.position + new Vector3(0, -0.5f, 0);
    }
    public void EnterWaitQueue(Vector3 slotPosition)
    {
        _queueSlotPosition = slotPosition;
        SetState(State.Waiting);
        WalkTo(slotPosition);
    }

    public void MoveToQueueSlot(Vector3 newSlotPosition)
    {
        _queueSlotPosition = newSlotPosition;
        WalkTo(newSlotPosition);
    }

    public void AssignTable(Table table, Transform seatPoint)
    {
        _assignedTable = table;
        _seatPoint = seatPoint;
        SetState(State.WalkingToTable);
        WalkTo(seatPoint.position);
    }

    private void SitDown()
    {
        Freeze();
        transform.position = _seatPoint.position;

        _patience = maxPatience;
        SetState(State.WaitingForFood);
        Debug.Log($"[Client] Seated. Patience: {maxPatience}s");
    }

    private void TickPatience()
    {
        _patience -= Time.deltaTime;
        if (_patience <= 0f)
        {
            _patience = 0f;
            LeaveAngry();
        }
    }

    public void ReceiveFood()
    {
        if (CurrentState != State.WaitingForFood) return;

        SetState(State.Eating);
        Debug.Log("[Client] Food received! Eating...");
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
        Debug.Log("[Client] Finished eating. Leaving happy.");
        StartLeaving();
    }

    private void LeaveAngry()
    {
        happiness -= 10;
        Debug.Log("[Client] Patience ran out! Leaving angry.");
        _assignedTable?.FreeTable();
        SetState(State.Angry);
        WalkToExit();
    }

    private void StartLeaving()
    {
        _assignedTable?.FreeTable();
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
        }
    }
    //para que no se mueva como un puto idiota.
    private void Freeze()
    {
        _agent.ResetPath();
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;
    }

    private bool HasReachedDestination()
    {
        if (_agent.pathPending) return false;
        if (_agent.remainingDistance > _agent.stoppingDistance) return false;
        if (_agent.hasPath && _agent.velocity.sqrMagnitude > 0.01f) return false;
        return true;
    }

    private void SetState(State newState)
    {
        CurrentState = newState;
    }

    void OnDestroy()
    {
        if (_assignedTable != null && _assignedTable.SeatedClient == this)
            _assignedTable.FreeTable();
    }
}
