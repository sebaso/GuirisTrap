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

    public void StartPatience(float seconds)
    {
        MaxPatience = seconds;
        Patience = seconds;
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