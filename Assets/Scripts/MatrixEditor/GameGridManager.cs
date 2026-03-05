using UnityEngine;
using UnityEngine.SceneManagement;

public class GameGridManager : MonoBehaviour
{
    [SerializeField]
    private GridData _gridData;
    [SerializeField]
    private GridVisualCell _gridViewCellPrefab;
    [SerializeField]
    private GameObject _tablePrefab;

    private GridVisualCell[,] _cells;

    public GridData GetGridData => _gridData;

    void Start()
    {
        if(_gridData == null) return;
        
        if(SceneManager.GetActiveScene().name != "PreparationScene") return;
        _cells = new GridVisualCell[_gridData.widht, _gridData.height];

        for(int y = 0; y < _gridData.height; y++)
        {
            for(int x = 0; x < _gridData.widht; x++)
            {
                Vector3 position = new Vector3 (x + 0.5f, 0f, y + 0.5f);
                GridVisualCell cell = Instantiate(_gridViewCellPrefab, position, Quaternion.identity, transform);
                cell.Init(x, y);
                _cells[x, y] = cell;
            }
        }
    }

    public bool UpdateVisualCell(int newPlaceableObjectX, int newPlaceableObjectY, int newPlaceableObjectStartAtX, int newPlaceableObjectStartAtY)
    {
        if(newPlaceableObjectX < 0 || newPlaceableObjectY < 0 || newPlaceableObjectX >= _gridData.widht || newPlaceableObjectY >= _gridData.height) return false;

        CellType type = _gridData.GetType(newPlaceableObjectX, newPlaceableObjectY);

        if(type == CellType.Empty || _gridData._cells[newPlaceableObjectY * _gridData.widht + newPlaceableObjectX] == _gridData._cells[newPlaceableObjectStartAtY * _gridData.widht + newPlaceableObjectStartAtX])
        {
            _cells[newPlaceableObjectX, newPlaceableObjectY].SetState(CellVisualState.Empty);
        }
        else
        {
            _cells[newPlaceableObjectX, newPlaceableObjectY].SetState(CellVisualState.Blocked);
        }

        return true;

    }
    public void ResetVisualGrid()
    {
        for(int y = 0; y < _gridData.height; y++)
        {
            for(int x = 0; x < _gridData.widht; x++)
            {
                _cells[x,y].SetState(CellVisualState.Default);
            }
        }
    }
    public void ClearLastCell(int lastCellX, int lastCellY)
    {
        if(lastCellX < 0 || lastCellY < 0 || lastCellX >= _gridData.widht || lastCellY >= _gridData.height) return;

        _cells[lastCellX, lastCellY].SetState(CellVisualState.Default);
    }
    public void SaveGrid(int newPlaceableObjectX, int newPlaceableObjectY, int startPlaceableObjectX, int startPlaceableObjectY)
    {
        if(_gridData == null ) return;

        if(newPlaceableObjectX < 0 || newPlaceableObjectY < 0 || newPlaceableObjectX >= _gridData.widht || newPlaceableObjectY >= _gridData.height) return;

        _gridData.SetType(newPlaceableObjectX, newPlaceableObjectY, CellType.Occupied);

        if(startPlaceableObjectX != -1 && startPlaceableObjectY != -1 && startPlaceableObjectX != newPlaceableObjectX || startPlaceableObjectY != newPlaceableObjectY)
            _gridData.SetType(startPlaceableObjectX, startPlaceableObjectY, CellType.Empty);
    }
    public void TableGenerator()
    {
        Transform tableFolder = new GameObject("Tables").transform;

        for(int y = 0; y < _gridData.height; y++)
        {
            for(int x = 0; x < _gridData.widht; x++)
            {
                if(_gridData.GetType(x,y) == CellType.Occupied)
                {
                    Vector3 pos = new Vector3(x + 0.5f, 0f, y + 0.5f);
                    GameObject tableInstance = Instantiate(_tablePrefab, pos, Quaternion.identity, tableFolder.transform);
                    PlaceableObject placeable = tableInstance.GetComponent<PlaceableObject>();
                    placeable.SetGridManager(this);
                    placeable.InstancePlaceableObjectCreated(x,y);
                }
            }
        }
    }
}
