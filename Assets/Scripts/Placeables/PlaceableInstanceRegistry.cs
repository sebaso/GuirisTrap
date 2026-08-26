using System.Collections.Generic;
using UnityEngine;

public class PlaceableInstanceRegistry : MonoBehaviour
{
    private readonly Dictionary<Vector3Int, PlaceableObject> _instances = new();

    public void Register(Vector3Int anchor, PlaceableObject obj) => _instances[anchor] = obj;
    public void Unregister(Vector3Int anchor) => _instances.Remove(anchor);
    public PlaceableObject Get(Vector3Int anchor) => _instances.TryGetValue(anchor, out var o) ? o : null;
}