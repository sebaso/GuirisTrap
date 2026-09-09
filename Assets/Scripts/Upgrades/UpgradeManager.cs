using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    public static event System.Action<PlaceableItemData, int> OnItemUpgraded;

    private Dictionary<string, int> _tierByItem = new();

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
            LoadFromSave(SaveManager.Instance.GetUpgradeState());
        else
            Debug.LogWarning("[UpgradeManager] SaveManager no encontrado");
    }

    public void LoadFromSave(SaveManager.UpgradeStateData[] data)
    {
        _tierByItem.Clear();
        if (data == null) return;
        foreach (var entry in data)
            _tierByItem[entry.itemName] = entry.tierIndex;
    }

    public SaveManager.UpgradeStateData[] ToSaveData()
    {
        var result = new SaveManager.UpgradeStateData[_tierByItem.Count];
        int i = 0;
        foreach (var kvp in _tierByItem)
            result[i++] = new SaveManager.UpgradeStateData { itemName = kvp.Key, tierIndex = kvp.Value };
        return result;
    }

    public int GetTierIndex(PlaceableItemData item)
    {
        if (item == null) return 0;
        return _tierByItem.TryGetValue(item.name, out int tier) ? tier : 0;
    }

    public bool TryUpgrade(PlaceableItemData item)
    {
        if (item == null) return false;

        int current = GetTierIndex(item);
        int next = current + 1;

        if (next >= item.TierCount)
        {
            Debug.Log($"[UpgradeManager] '{item.name}' ya está en el tier máximo ({current}).");
            return false;
        }

        _tierByItem[item.name] = next;
        Debug.Log($"[UpgradeManager] '{item.name}' mejorado: tier {current} → {next}.");
        OnItemUpgraded?.Invoke(item, next);
        return true;
    }

    [Header("Debug")]
    [SerializeField] private PlaceableItemData _debugUpgradeTarget;

    [ContextMenu("Debug: Mejorar _debugUpgradeTarget")]
    private void DebugUpgrade()
    {
        if (_debugUpgradeTarget == null)
        {
            Debug.LogWarning("[UpgradeManager] Asigna _debugUpgradeTarget en el Inspector antes de probar.");
            return;
        }
        TryUpgrade(_debugUpgradeTarget);
    }
}