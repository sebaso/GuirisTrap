using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [SerializeField] private PlaceableItemData[] _allItems;

    [SerializeField] private SaveData _data = new SaveData();
    private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public int CurrentDay => _data.day;

    /// <summary>Saldo guardado en disco (lo escribe SaveMoney/IncrementDayAndSave).</summary>
    public int SavedMoney => _data.money;

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
        // El dinero lo gestiona MoneyManager (DontDestroyOnLoad). No lo
        // sobrescribimos aquí al cargar las grids: el saldo vivo ya incluye
        // las ganancias del día y viaja entre escenas por sí solo.
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

    /// <summary>
    /// Persiste únicamente el dinero actual del MoneyManager en el archivo de
    /// guardado, sin avanzar el día ni volver a serializar las grids. Se usa al
    /// terminar el día para que el dinero ganado quede guardado en disco y llegue
    /// a la PreparationScene.
    /// </summary>
    public void SaveMoney()
    {
        _data.money = MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : _data.money;
        WriteFile();
    }

    private void WriteFile()
    {
        File.WriteAllText(SavePath, JsonUtility.ToJson(_data, true));
        Debug.Log($"[SaveManager] Saved day {_data.day}, money {_data.money} → {SavePath}");
    }

    [System.Serializable]
    public class SaveData
    {
        public int day;
        public int money;
        public GridSaveData[] grids;
    }

    [System.Serializable]
    public class GridSaveData
    {
        public string gridName;
        public CellSaveData[] cells;
    }

    [System.Serializable]
    public class CellSaveData
    {
        public int x, y;
        public string itemName;
        public Quaternion rotation;
    }
}
