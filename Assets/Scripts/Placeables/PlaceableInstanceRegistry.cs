using System.Collections.Generic;
using UnityEngine;

public class PlaceableInstanceRegistry : MonoBehaviour
{
    private static PlaceableInstanceRegistry _instance;
    public static PlaceableInstanceRegistry Instance => _instance;

    private readonly Dictionary<Vector3Int, PlaceableObject> _instances = new();

    void Awake()
    {
        if (_instance == null) _instance = this;
        else Destroy(gameObject);
    }

    public void Register(Vector3Int anchor, PlaceableObject obj) => _instances[anchor] = obj;
    public void Unregister(Vector3Int anchor) => _instances.Remove(anchor);
    public PlaceableObject Get(Vector3Int anchor) => _instances.TryGetValue(anchor, out var o) ? o : null;
}