using System.Collections.Generic;
using UnityEngine;

public class OwnedItemsManager : MonoBehaviour
{
public static OwnedItemsManager Instance { get; private set; }

    private Dictionary<string, int> _ownedItems = new();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        if (SaveManager.Instance != null)
            LoadFromSave(SaveManager.Instance.GetOwnedItems());
        else
            Debug.LogWarning("[OwnedItemsManager] SaveManager no encontrado");
    }
    public void LoadFromSave(SaveManager.ItemCountData[] data)
    {
        _ownedItems.Clear();
        if (data == null) {
            Debug.Log("[OwnedItemsManager] LoadFromSave: data es null");
            return;
            }
            Debug.Log("[OwnedItemsManager] LoadFromSave: cargando " + data.Length + " items");
        foreach (var entry in data)
        {
            Debug.Log($"[OwnedItemsManager] Cargado: {entry.itemName} x{entry.count}");
            _ownedItems[entry.itemName] = entry.count;
        }
    }

    public SaveManager.ItemCountData[] ToSaveData()
    {
        Debug.Log("[OwnedItemsManager] ToSaveData: guardando " + _ownedItems.Count + " items");
        var result = new SaveManager.ItemCountData[_ownedItems.Count];
        int i = 0;
        foreach (var kvp in _ownedItems)
            result[i++] = new SaveManager.ItemCountData { itemName = kvp.Key, count = kvp.Value };
        return result;
    }

    public void AddItem(string itemName)
    {
        if (_ownedItems.ContainsKey(itemName)) 
            _ownedItems[itemName]++;
        else 
            _ownedItems[itemName] = 1;
        Debug.Log($"[OwnedItemsManager] AddItem: {itemName} ahora x{_ownedItems[itemName]}");
    }

    public int GetCount(string itemName)
    {
        return _ownedItems.TryGetValue(itemName, out int count) ? count : 0;
    }
}
