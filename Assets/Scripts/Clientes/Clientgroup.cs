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