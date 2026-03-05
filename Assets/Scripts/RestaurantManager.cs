using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RestaurantManager : MonoBehaviour
{
    public static RestaurantManager Instance { get; private set; }

    [Header("Tables")]
    public List<Table> _tables = new();

    [Header("Entrance Queue")]
    [Tooltip("Set automatically by ClientSpawner, or assign manually for scenes without a spawner.")]
    public Transform entrancePoint;
    public Vector3 queueDirection = Vector3.back;
    public float   queueSpacing   = 1.2f;

    public List<Client> _waitingClients = new();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterTable(Table table)
    {
        if (!_tables.Contains(table))
            _tables.Add(table);
    }

    public void UnregisterTable(Table table)
    {
        _tables.Remove(table);
    }

    public void ClientArrived(Client client)
    {
        OnClientSpawned(client);
    }

    public void OnClientSpawned(Client client)
    {
        Table freeTable = GetFreeTable();
        if (freeTable != null)
        {
            SeatClient(client, freeTable);
        }
        else
        {
            int slotIndex = _waitingClients.Count;
            _waitingClients.Add(client);
            client.EnterWaitQueue(GetSlotPosition(slotIndex));
            Debug.Log($"[RestaurantManager] No free table. Client queued (slot {slotIndex}, {_waitingClients.Count} waiting).");
        }
    }


    public void TableFreed(Table table)
    {
        if (_waitingClients.Count == 0) return;

        Client next = _waitingClients[0];
        _waitingClients.RemoveAt(0);

        if (next != null)
            SeatClient(next, table);

        // Shuffle remaining clients one slot forward
        for (int i = 0; i < _waitingClients.Count; i++)
            _waitingClients[i]?.MoveToQueueSlot(GetSlotPosition(i));
    }

    private void SeatClient(Client client, Table table)
    {
        table.ReserveForClient(client);
        client.AssignTable(table, table.SeatPoint);
        Debug.Log($"[RestaurantManager] Client seated at Table {table.tableNumber}.");
    }

    public Table GetFreeTable()
    {
        foreach (Table t in _tables)
        {
            if (!t.IsOccupied && !t.GetComponent<PlaceableObject>().Storaged)
                return t;
        }
        return null;
    }

    public int WaitingCount => _waitingClients.Count;

    public Vector3 GetSlotPosition(int index)
    {
        Vector3 origin    = entrancePoint != null ? entrancePoint.position : Vector3.zero;
        Vector3 candidate = origin + queueDirection.normalized * (queueSpacing * index);

        // Snap to the nearest walkable NavMesh point (avoids off-mesh slot positions)
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            return hit.position;

        return candidate; // Fall back to raw position if nothing found nearby
    }
}
