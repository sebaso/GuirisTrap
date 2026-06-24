using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RestaurantManager : MonoBehaviour
{
    public static RestaurantManager Instance { get; private set; }

    [Header("Tables")]
    public List<Table> _tables = new();
    private List<Table> _placedTables = new();
    [Header("Entrance Queue")]
    [Tooltip("Set automatically by ClientSpawner, or assign manually for scenes without a spawner.")]
    public Transform entrancePoint;
    public Vector3 queueDirection = Vector3.back;
    public float queueSpacing = 1.2f;

    [Header("Group Queue Settings")]
    [Tooltip("Spacing between different groups in the queue")]
    public float groupSpacing = 2.5f;
    [Tooltip("Spacing between members within a group")]
    public float memberSpacing = 0.8f;

    private List<ClientGroup> _waitingGroups = new();

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

    void Start()
    {
        SyncPlacedTables();
    }

    private void SyncPlacedTables()
    {
        int newlyAdded = 0;
        foreach (Table table in _tables)
        {
            if (table != null && table.IsPlaced && !_placedTables.Contains(table))
            {
                _placedTables.Add(table);
                newlyAdded++;
            }
        }
        if (newlyAdded > 0)
        {
            Debug.Log($"[RestaurantManager] Synced placed tables. Added {newlyAdded} existing tables. Total placed: {_placedTables.Count}");
            TryFlushWaitingGroups();
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
        _placedTables.Remove(table);
    }
    public void TablePlaced(Table table)
    {
        if (table == null) return;
        if (!_placedTables.Contains(table))
        {
            _placedTables.Add(table);
            Debug.Log($"[RestaurantManager] Table {table.tableNumber} placed. Total placed tables: {_placedTables.Count}");
            TryFlushWaitingGroups();
        }
    }

    public void TableStored(Table table)
    {
        if (table == null) return;
        if (_placedTables.Remove(table))
        {
            Debug.Log($"[RestaurantManager] Table {table.tableNumber} stored. Total placed tables: {_placedTables.Count}");
        }
    }


    public int PlacedTableCount => _placedTables.Count;


    public void ClientArrived(Client client)
    {
        if (client == null) return;

        ClientGroup group = client.Group;

        if (group == null)
        {
            group = new ClientGroup(1);
            group.AddMember(client);
        }

        // If this group is already in the waiting queue, just update positions
        if (_waitingGroups.Contains(group))
        {
            Debug.Log($"[RestaurantManager] {client.name} arrived but group {group.GroupID} is already waiting. Member {group.Members.IndexOf(client) + 1}/{group.Members.Count}.");
            return;
        }

        // Check if ALL group members have arrived (WalkingToEntrance or Waiting)
        bool allArrived = true;
        foreach (var member in group.Members)
        {
            if (member == null) continue;
            if (member.CurrentState != Client.State.Waiting && member.CurrentState != Client.State.WalkingToEntrance)
            {
                allArrived = false;
                break;
            }
        }

        if (!allArrived)
        {
            Debug.Log($"[RestaurantManager] Group {group.GroupID} not fully arrived yet. Waiting for all members.");
            return;
        }

        TableBlock freeBlock = GetFreeBlockForGroup(group.Size);
        if (freeBlock != null)
        {
            SeatGroup(group, freeBlock);
        }
        else
        {
            EnqueueGroup(group);
        }
    }

    public void OnClientSpawned(Client client)
    {
        //basura legacy
    }

    private void EnqueueGroup(ClientGroup group)
    {
        int groupIndex = _waitingGroups.Count;
        _waitingGroups.Add(group);

        for (int i = 0; i < group.Members.Count; i++)
        {
            Client member = group.Members[i];
            if (member != null)
            {
                Vector3 slotPosition = GetGroupMemberSlotPosition(groupIndex, i, group.Size);
                member.EnterWaitQueue(slotPosition);
            }
        }

        Debug.Log($"[RestaurantManager] No table for {group}. Queued at position {groupIndex}. Total waiting groups: {_waitingGroups.Count}");
        HUDMessage.Instance?.ShowWarning($"¡No hay mesa para el grupo de {group.Size}! En cola...");
    }

    // table moved: re-evaluate blocks so waiting groups can use new combos
    public void NotifyTablesRearranged() => TryFlushWaitingGroups();

    // queue patience ran out; Remove() false = already gone, so idempotent
    public void AbandonGroup(ClientGroup group)
    {
        if (group == null || !_waitingGroups.Remove(group)) return;

        foreach (var member in group.Members)
            member?.LeaveQueue();

        Debug.Log($"[RestaurantManager] {group} left without being seated (queue patience ran out).");
        HUDMessage.Instance?.ShowBad("¡Clientes se fueron por esperar demasiado!");
        RepositionWaitingGroups();
    }

    public void FreeGroupTables(ClientGroup group)
    {
        if (group == null) return;

        bool anyFreed = false;
        foreach (var t in _placedTables)
        {
            if (t != null && t.OccupyingGroup == group)
            {
                t.ClearReservation();
                anyFreed = true;
            }
        }

        if (anyFreed)
        {
            TryFlushWaitingGroups();
        }
    }

    private void RepositionWaitingGroups()
    {
        for (int groupIdx = 0; groupIdx < _waitingGroups.Count; groupIdx++)
        {
            ClientGroup group = _waitingGroups[groupIdx];
            for (int memberIdx = 0; memberIdx < group.Members.Count; memberIdx++)
            {
                Client member = group.Members[memberIdx];
                if (member != null)
                {
                    Vector3 newSlot = GetGroupMemberSlotPosition(groupIdx, memberIdx, group.Size);
                    member.MoveToQueueSlot(newSlot);
                }
            }
        }
    }

    private void SeatGroup(ClientGroup group, TableBlock block)
    {
        foreach (var t in block.Tables)
        {
            t.Reserve(group);
        }

        List<Transform> seatPoints = new List<Transform>();
        List<Table> seatTables = new List<Table>();

        foreach (var t in block.Tables)
        {
            var points = t.GetSeatPoints();
            seatPoints.AddRange(points);
            for (int j = 0; j < points.Count; j++) seatTables.Add(t);
        }

        int usableSeats = Mathf.Min(seatPoints.Count, group.Size);

        if (seatPoints.Count < group.Size)
        {
            Debug.LogWarning($"[RestaurantManager] Block has only {seatPoints.Count} seats but group needs {group.Size}! Using {usableSeats} seats.");
            HUDMessage.Instance?.ShowWarning($"¡Faltan sillas! El grupo necesita {group.Size} asientos.");
        }

        // Remove the group from waiting queue if it was there
        _waitingGroups.Remove(group);

        for (int i = 0; i < usableSeats; i++)
        {
            Client member = group.Members[i];
            if (member != null && seatPoints[i] != null)
            {
                member.AssignTable(seatTables[i], seatPoints[i]);
            }
        }

        // For excess members beyond available seats, keep them in the queue
        if (usableSeats < group.Members.Count)
        {
            ClientGroup overflowGroup = new ClientGroup(group.Members.Count - usableSeats);
            for (int i = usableSeats; i < group.Members.Count; i++)
            {
                overflowGroup.AddMember(group.Members[i]);
            }
            EnqueueGroup(overflowGroup);
            Debug.LogWarning($"[RestaurantManager] Created overflow group of {overflowGroup.Size} for excess members");
        }

        Debug.Log($"[RestaurantManager] {group} seated at TableBlock (capacity {block.Capacity}) spanning {block.Tables.Count} tables. Seated {usableSeats}/{group.Size} members.");
    }

    private List<TableBlock> GetTableBlocks()
    {
        List<TableBlock> blocks = new List<TableBlock>();
        HashSet<Table> visited = new HashSet<Table>();

        foreach (var table in _placedTables)
        {
            if (table == null || !table.IsPlaced || visited.Contains(table)) continue;

            TableBlock block = new TableBlock();
            Queue<Table> queue = new Queue<Table>();
            queue.Enqueue(table);
            visited.Add(table);

            while (queue.Count > 0)
            {
                Table current = queue.Dequeue();
                block.AddTable(current);
                foreach (var other in _placedTables)
                {
                    if (other == null || !other.IsPlaced || visited.Contains(other)) continue;


                    float dist = Vector3.Distance(current.transform.position, other.transform.position);
                    if (dist <= 1.2f)
                    {
                        visited.Add(other);
                        queue.Enqueue(other);
                    }
                }
            }
            blocks.Add(block);
        }
        return blocks;
    }

    public TableBlock GetFreeBlockForGroup(int groupSize)
    {
        if (_placedTables.Count == 0)
        {
            Debug.LogWarning("[RestaurantManager] No tables have been placed in the restaurant yet!");
            HUDMessage.Instance?.ShowWarning("¡No hay mesas en el restaurante! Añade algunas en preparación.");
            return null;
        }
        List<TableBlock> blocks = GetTableBlocks();
        foreach (TableBlock b in blocks)
        {
            if (b.IsOccupied) continue;
            if (b.Capacity == groupSize)
                return b;
        }
        TableBlock bestFit = null;
        int smallestCapacity = int.MaxValue;

        foreach (TableBlock b in blocks)
        {
            if (b.IsOccupied) continue;

            if (b.Capacity >= groupSize && b.Capacity < smallestCapacity)
            {
                smallestCapacity = b.Capacity;
                bestFit = b;
            }
        }

        if (bestFit == null)
        {
            Debug.Log($"[RestaurantManager] No suitable table block found for group size {groupSize}. Total placed tables: {_placedTables.Count}");
        }

        return bestFit;
    }


    private void TryFlushWaitingGroups()
    {
        for (int i = _waitingGroups.Count - 1; i >= 0; i--)
        {
            ClientGroup group = _waitingGroups[i];
            group.CleanupNullMembers();

            if (!group.IsValid || group.Members.Count == 0)
            {
                _waitingGroups.RemoveAt(i);
                continue;
            }

            TableBlock freeBlock = GetFreeBlockForGroup(group.Size);
            if (freeBlock != null)
            {
                _waitingGroups.RemoveAt(i);
                SeatGroup(group, freeBlock);
            }
        }

        RepositionWaitingGroups();
    }

    public int WaitingCount => _waitingGroups.Sum(g => g.Size);

    public int WaitingGroupCount => _waitingGroups.Count;

    private Vector3 GetGroupMemberSlotPosition(int groupIndex, int memberIndex, int groupSize)
    {
        Vector3 origin = entrancePoint != null ? entrancePoint.position : Vector3.zero;
        Vector3 queueDir = queueDirection.normalized;

        Vector3 perpDir = Vector3.Cross(queueDir, Vector3.up).normalized;

        float groupOffset = groupIndex * groupSpacing;
        Vector3 groupBasePos = origin + queueDir * groupOffset;

        float totalWidth = (groupSize - 1) * memberSpacing;
        float memberOffset = (memberIndex * memberSpacing) - (totalWidth / 2f);

        Vector3 candidate = groupBasePos + perpDir * memberOffset;
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            return hit.position;

        return candidate;
    }

    public Vector3 GetSlotPosition(int index)
    {
        Vector3 origin = entrancePoint != null ? entrancePoint.position : Vector3.zero;
        Vector3 candidate = origin + queueDirection.normalized * (queueSpacing * index);

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            return hit.position;

        return candidate;
    }
}

public static class ListExtensions
{
    public static int Sum(this List<ClientGroup> groups, System.Func<ClientGroup, int> selector)
    {
        int sum = 0;
        foreach (var group in groups)
            sum += selector(group);
        return sum;
    }
}