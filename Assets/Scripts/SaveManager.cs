using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [SerializeField] private PlaceableItemData[] _allItems;

    private SaveData _data = new SaveData();
    private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public int CurrentDay => _data.day;

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

    public void LoadGrids(GameGridManager[] managers)
    {
        if (_data.grids == null) return;

        foreach (var gridSave in _data.grids)
        {
            GameGridManager manager = System.Array.Find(managers, m => m.GetGridData.name == gridSave.gridName);
            if (manager == null) continue;

            GridData gridData = manager.GetGridData;

            foreach (var cell in gridSave.cells)
            {
                PlaceableItemData item = System.Array.Find(_allItems, i => i.name == cell.itemName);
                if (item == null) continue;

                gridData.SetType(cell.x, cell.y, CellType.Occupied);
                gridData.SetItem(cell.x, cell.y, item);
                gridData.SetRotation(cell.x, cell.y, cell.rotation);
            }
        }

        if (MoneyManager.Instance != null)
            MoneyManager.Instance.SetMoney(_data.money);
    }

    private void Load()
    {
        if (!File.Exists(SavePath)) return;
        try
        {
            _data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        }
        catch
        {
            Debug.LogWarning("[SaveManager] Failed to read save file, starting fresh.");
            _data = new SaveData();
        }
    }

    private void SaveGridsFromManagers()
    {
        GameGridManager[] managers = FindObjectsByType<GameGridManager>(FindObjectsSortMode.None);
        _data.grids = new GridSaveData[managers.Length];

        for (int i = 0; i < managers.Length; i++)
        {
            GridData gridData = managers[i].GetGridData;
            var cells = new System.Collections.Generic.List<CellSaveData>();

            for (int y = 0; y < gridData.height; y++)
            {
                for (int x = 0; x < gridData.width; x++)
                {
                    GridCell cell = gridData.GetCell(x, y);
                    if (cell.type != CellType.Occupied || cell.item == null) continue;
                    cells.Add(new CellSaveData { x = x, y = y, itemName = cell.item.name, rotation = cell.rotation });
                }
            }

            _data.grids[i] = new GridSaveData { gridName = gridData.name, cells = cells.ToArray() };
        }
    }

    public void ForceSave()
    {
        _data.money = MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : _data.money;
        SaveGridsFromManagers();
        WriteFile();
    }

    private void WriteFile()
    {
        File.WriteAllText(SavePath, JsonUtility.ToJson(_data, true));
        Debug.Log($"[SaveManager] Saved day {_data.day}, money {_data.money} → {SavePath}");
    }

    [System.Serializable]
    class SaveData
    {
        public int day;
        public int money;
        public GridSaveData[] grids;
    }

    [System.Serializable]
    class GridSaveData
    {
        public string gridName;
        public CellSaveData[] cells;
    }

    [System.Serializable]
    class CellSaveData
    {
        public int x, y;
        public string itemName;
        public Quaternion rotation;
    }
}
