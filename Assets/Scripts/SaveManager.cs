using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [SerializeField] private PlaceableItemData[] _allItems;
    public PlaceableItemData[] AllItems => _allItems;
    [SerializeField] private SaveData _data = new SaveData();
    private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");
    private bool _pendingNewGame = false;

    public int CurrentDay => _data.day;
    public int SavedMoney => _data.money;
    public ItemCountData[] GetOwnedItems() => _data.ownedItems;
    public CellSaveData3D[] GetGridCells() => _data.gridCells;

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

    private bool _hasSyncedGridsThisSession = false;
    public bool ShouldSyncGridsOnLoad()
    {
        if (_hasSyncedGridsThisSession) return false;
        _hasSyncedGridsThisSession = true;
        return true;
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
        WriteFile();
    }

    public void ForceSave()
    {
        _data.money = MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : _data.money;
        SaveGridFromManager();
        WriteFile();
    }

    private void SaveGridFromManager()
    {
        GridManager gridManager = FindAnyObjectByType<GridManager>();
        if (gridManager != null)
            _data.gridCells = gridManager.ToSaveData();
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

    /// <summary>
    /// Aplica los datos de grid ya cargados en memoria (_data.gridCells) sobre
    /// el GridManager de la escena actual. Se llama desde SceneController tras
    /// cargar PreparationScene, una única vez por sesión (ShouldSyncGridsOnLoad).
    /// </summary>
    public void ApplyGridToScene(GridManager gridManager)
    {
        if (gridManager == null || _data.gridCells == null || _data.gridCells.Length == 0) return;
        gridManager.LoadFromSaveData(_data.gridCells, _allItems);
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

    /// <summary>
    /// Empieza una partida nueva: borra el save en disco, PlayerPrefs, y limpia
    /// el VoxelGridData de la escena actual (si hay una cargada).
    /// </summary>
    public void NewGame()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
        PlayerPrefs.DeleteAll();

        _data = new SaveData();
        OwnedItemsManager.Instance?.LoadFromSave(null);

        Inventory inv = Inventory.Instance != null ? Inventory.Instance : Inventory.EnsureExists();
        inv.Clear();

        MoneyManager.Instance?.ResetToStarting();

        _pendingNewGame = true;
        _hasSyncedGridsThisSession = false;

        Debug.Log("[SaveManager] Nueva partida solicitada; se limpiará el grid al entrar a PreparationScene.");
        Debug.Log($"[SaveManager] NewGame ejecutado. _pendingNewGame = {_pendingNewGame}");
    }
    public bool ConsumePendingNewGame()
    {
            Debug.Log($"[SaveManager] ConsumePendingNewGame llamado. _pendingNewGame = {_pendingNewGame}");
        if (!_pendingNewGame) return false;
        _pendingNewGame = false;
        _hasSyncedGridsThisSession = true;
        return true;
    }
    public bool HasSaveFile => File.Exists(SavePath);
    public string SaveFilePath => SavePath;

    public void SaveMoney()
    {
        _data.money = MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : _data.money;
        WriteFile();
    }

    public void WriteCurrentData() => WriteFile();

    private void WriteFile()
    {
        _data.ownedItems = OwnedItemsManager.Instance?.ToSaveData();
        File.WriteAllText(SavePath, JsonUtility.ToJson(_data, true));
        Debug.Log($"[SaveManager] Saved day {_data.day}, money {_data.money}, " +
                  $"stars {_data.stars:0.##}, celdas {(_data.gridCells != null ? _data.gridCells.Length : 0)} → {SavePath}");
    }

    [System.Serializable]
    public class SaveData
    {
        public int day;
        public int money;
        public float stars;
        public int lastGradedDay = -1;
        public System.Collections.Generic.List<int> weekGrades = new System.Collections.Generic.List<int>();

        public CellSaveData3D[] gridCells;
        public ItemCountData[] ownedItems;
    }

    [System.Serializable]
    public class CellSaveData3D
    {
        public int x, y, z;
        public CellType type;
        public string itemName;
        public int anchorX, anchorY, anchorZ;
        public bool isEntrance;
        public Quaternion rotation;
    }

    [System.Serializable]
    public class ItemCountData
    {
        public string itemName;
        public int count;
    }
}