using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Representa un grupo de clientes.
/// </summary>
public class ClientGroup
{
    public int GroupID { get; private set; }
    public int Size { get; private set; }
    public List<Client> Members { get; private set; }

    private static int _nextGroupID = 0;

    public ClientGroup(int size)
    {
        GroupID = _nextGroupID++;
        Size = size;
        Members = new List<Client>(size);

        // Sharing: 50/50 chance the group shares dishes and orders one fewer
        // plate than diners. The last plate then feeds two diners. Groups of 1
        // never share (would order 0 plates).
        IsSharing = size > 1 && Random.value < 0.5f;
        PlatesNeeded = IsSharing ? size - 1 : size;
    }

    public void AddMember(Client client)
    {
        if (Members.Count < Size)
        {
            Members.Add(client);
            client.SetGroup(this);
        }
        else
        {
            Debug.LogWarning($"[ClientGroup] Attempted to add more members than group size ({Size})");
        }
    }

    public bool IsFull => Members.Count >= Size;

    // one shared timer for the whole group (queue, then seated); the leader ticks it
    public float Patience { get; private set; }
    public float MaxPatience { get; private set; }
    public float PatienceRatio => MaxPatience > 0f ? Patience / MaxPatience : 0f;

    // ── Per-plate delivery: one plate feeds one diner. The bar refills on each
    // delivery so a big group needs several trips instead of a single plate.
    // When IsSharing, the group orders PlatesNeeded (= Size-1) dishes and the
    // last plate feeds two diners. ──
    public bool IsSharing { get; private set; }
    public int PlatesNeeded { get; private set; }
    public int PlatesServed { get; private set; }
    public bool AllFed => PlatesServed >= PlatesNeeded;

    // how many diners have finished eating; the group leaves together at the end
    public int FinishedEatingCount { get; private set; }
    public bool AllFinishedEating => FinishedEatingCount >= Members.Count;

    public void StartPatience(float seconds)
    {
        MaxPatience = seconds;
        Patience = seconds;
        // a fresh patience cycle also starts a fresh serving cycle
        PlatesServed = 0;
        FinishedEatingCount = 0;
    }

    /// <summary>Refills the patience bar to full without changing its max.
    /// Called by Table.PlaceFood every time a plate is delivered.</summary>
    public void RefillPatience()
    {
        Patience = MaxPatience;
    }

    /// <summary>Called by Table.PlaceFood: counts one served plate and refills
    /// the shared patience bar.</summary>
    public void OnPlateServed()
    {
        PlatesServed++;
        RefillPatience();
    }

    /// <summary>How many diners this next plate should feed. Normally 1; when
    /// sharing, the LAST plate of the order feeds 2 diners. The caller
    /// (Table.PlaceFood) then transitions that many waiting diners to Eating.
    /// Returns 0 if the order is already complete (AllFed).</summary>
    public int DinersForNextPlate()
    {
        if (AllFed) return 0;
        // The shared plate is the final one: index PlatesNeeded-1.
        bool isLastPlate = PlatesServed == PlatesNeeded - 1;
        return (IsSharing && isLastPlate) ? 2 : 1;
    }

    /// <summary>Called by Client.FinishEating. When the last diner finishes,
    /// the whole group leaves together (the leader frees the table).</summary>
    public void OnMemberFinishedEating(Client client)
    {
        FinishedEatingCount++;
        if (FinishedEatingCount >= Members.Count)
        {
            foreach (var m in Members)
                if (m != null) m.StartLeaving();
        }
    }

    // true only the frame it reaches zero
    public bool TickPatience(float deltaTime)
    {
        if (Patience <= 0f) return false;
        Patience -= deltaTime;
        if (Patience <= 0f) { Patience = 0f; return true; }
        return false;
    }

    // drives the table bar: anyone seated, pre-food
    public bool IsWaitingForFood
    {
        get
        {
            foreach (var m in Members)
                if (m != null && m.CurrentState == Client.State.WaitingForFood) return true;
            return false;
        }
    }

    public bool IsValid => Members.TrueForAll(c => c != null);
    public void CleanupNullMembers()
    {
        Members.RemoveAll(c => c == null);
    }

    public override string ToString()
    {
        return $"Group #{GroupID} (Size: {Size}, Members: {Members.Count})";
    }
}