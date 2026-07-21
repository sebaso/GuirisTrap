using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [SerializeField] private PlaceableItemData[] _allItems;
    public PlaceableItemData[] AllItems => _allItems;
    [SerializeField] private SaveData _data = new SaveData();
    private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public int CurrentDay => _data.day;

    public int SavedMoney => _data.money;
    public ItemCountData[] GetOwnedItems() => _data.ownedItems;

    public float Stars
    {
        get => _data.stars;
        set => _data.stars = Mathf.Clamp(value, 0f, 5f);
    }

    public System.Collections.Generic.List<int> WeekGrades
    {
        get
        {
            if (_data.weekGrades == null)
                _data.weekGrades = new System.Collections.Generic.List<int>();
            return _data.weekGrades;
        }
    }

    public int LastGradedDay
    {
        get => _data.lastGradedDay;
        set => _data.lastGradedDay = value;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void IncrementDayAndSave()
    {
        _data.day++;
        _data.money = MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : _data.money;
        SaveGridsFromManagers();
        WriteFile();
    }

    // Todos los GameGridManager de la escena comparten el mismo VoxelGridData,
    // así que basta con leerlo de cualquiera de ellos (el primero) para cargar
    // el estado completo del restaurante.
    public void LoadGrids(GameGridManager[] managers)
    {
        if (_data.cells == null || managers.Length == 0) return;

        VoxelGridData voxelData = managers[0].GetGridData;

        foreach (var cellSave in _data.cells)
        {
            PlaceableItemData item = System.Array.Find(_allItems, i => i.name == cellSave.itemName);
            if (item == null) continue;

            voxelData.SetType(cellSave.x, cellSave.y, cellSave.z, CellType.Occupied);
            voxelData.SetItem(cellSave.x, cellSave.y, cellSave.z, item);
            voxelData.SetRotation(cellSave.x, cellSave.y, cellSave.z, cellSave.rotation);
        }
    }

    private void Load()
    {
        if (!File.Exists(SavePath)) return;
        try
        {
            _data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            OwnedItemsManager.Instance?.LoadFromSave(_data.ownedItems);
        }
        catch
        {
            Debug.LogWarning("[SaveManager] Failed to read save file, starting fresh.");
            _data = new SaveData();
        }
    }
    [ContextMenu("Delete Save")]
    public void DeleteSave()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
        _data = new SaveData();
        PlayerPrefs.DeleteAll();
        OwnedItemsManager.Instance?.LoadFromSave(null);
        Debug.Log("[SaveManager] Save eliminado: " + SavePath);
    }

    private void SaveGridsFromManagers()
    {
        GameGridManager[] managers = FindObjectsByType<GameGridManager>(FindObjectsSortMode.None);
        if (managers.Length == 0) return;

        // Todos apuntan al mismo asset, con leer uno basta.
        VoxelGridData voxelData = managers[0].GetGridData;
        var cells = new System.Collections.Generic.List<CellSaveData>();

        for (int z = 0; z < voxelData.depth; z++)
        {
            for (int y = 0; y < voxelData.height; y++)
            {
                for (int x = 0; x < voxelData.width; x++)
                {
                    GridCell cell = voxelData.GetCell(x, y, z);
                    if (cell.type != CellType.Occupied || cell.item == null) continue;
                    cells.Add(new CellSaveData { x = x, y = y, z = z, itemName = cell.item.name, rotation = cell.rotation });
                }
            }
        }

        _data.cells = cells.ToArray();
    }

    public void ForceSave()
    {
        _data.money = MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : _data.money;
        SaveGridsFromManagers();
        WriteFile();
    }

    public void SaveMoney()
    {
        _data.money = MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : _data.money;
        WriteFile();
    }

    public void WriteCurrentData() => WriteFile();

    public void ResetSave()
    {
        _data = new SaveData();
        if (File.Exists(SavePath)) File.Delete(SavePath);

        // Las mesas/sillas colocadas viven en el VoxelGridData (ScriptableObject),
        // no en save.json. Como todos los managers comparten el mismo asset, con
        // limpiar uno basta; se llama en todos por si ClearPlacedItems hace algo
        // adicional específico de la vista.
        foreach (var manager in FindObjectsByType<GameGridManager>(FindObjectsSortMode.None))
            manager.ClearPlacedItems();

        Debug.Log($"[SaveManager] Save reset → {SavePath}");
    }

    public string SaveFilePath => SavePath;

    public bool HasSaveFile => File.Exists(SavePath);

    private void WriteFile()
    {
        _data.ownedItems = OwnedItemsManager.Instance?.ToSaveData(); 
        File.WriteAllText(SavePath, JsonUtility.ToJson(_data, true));
        Debug.Log($"[SaveManager] Saved day {_data.day}, money {_data.money}, " +
                  $"stars {_data.stars:0.##}, weekGrades {(_data.weekGrades != null ? _data.weekGrades.Count : 0)} → {SavePath}");
    }

    [System.Serializable]
    public class SaveData
    {
        public int day;
        public int money;

        public float stars;
        public int lastGradedDay = -1;
        public System.Collections.Generic.List<int> weekGrades
            = new System.Collections.Generic.List<int>();

        public CellSaveData[] cells;
        public ItemCountData[] ownedItems;
    }

    [System.Serializable]
    public class CellSaveData
    {
        public int x, y, z;
        public string itemName;
        public Quaternion rotation;
    }

    [System.Serializable]
    public class ItemCountData
    {
        public string itemName;
        public int count;
    }
}