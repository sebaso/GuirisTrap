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


    void Start()
    {
        if(_gridData == null) return;
        
        TableGenerator();

        if(SceneManager.GetActiveScene().name != "PreparationScene") return;
        _cells = new GridVisualCell[_gridData._widht, _gridData._height];

        for(int y = 0; y < _gridData._height; y++)
        {
            for(int x = 0; x < _gridData._widht; x++)
            {
                Vector3 position = new Vector3 (x + 0.5f, 0f, y + 0.5f);
                GridVisualCell cell = Instantiate(_gridViewCellPrefab, position, Quaternion.identity, transform);
                cell.Init(x, y);
                _cells[x, y] = cell;
            }
        }
    }

    public bool UpdateVisualCell(int newPlaceableObjectX, int newPlaceableObjectY)
    {
        if(newPlaceableObjectX < 0 || newPlaceableObjectY < 0 || newPlaceableObjectX >= _gridData._widht || newPlaceableObjectY >= _gridData._height) return false;

        CellType type = _gridData.Get(newPlaceableObjectX, newPlaceableObjectY);

        if(type == CellType.Empty)
        {
            _cells[newPlaceableObjectX, newPlaceableObjectY].SetState(CellVisualState.Empty);
        }
        else
        {
            _cells[newPlaceableObjectX, newPlaceableObjectY].SetState(CellVisualState.Blocked);
        }

        return true;

    }

    public void ClearLastCell(int lastCellX, int lastCellY)
    {
        if(lastCellX < 0 || lastCellY < 0 || lastCellX >= _gridData._widht || lastCellY >= _gridData._height) return;

        _cells[lastCellX, lastCellY].SetState(CellVisualState.Default);
    }
    public void SaveGrid(int newPlaceableObjectX, int newPlaceableObjectY, int startPlaceableObjectX, int starPlaceableObjectY)
    {
        if(_gridData == null ) return;

        if(newPlaceableObjectX < 0 || newPlaceableObjectY < 0 || newPlaceableObjectX >= _gridData._widht || newPlaceableObjectY >= _gridData._height) return;

        _gridData.Set(newPlaceableObjectX, newPlaceableObjectY, CellType.Occupied);

        if(startPlaceableObjectX != -1 && starPlaceableObjectY != -1)
            _gridData.Set(startPlaceableObjectX, starPlaceableObjectY, CellType.Empty);
    }
    public void TableGenerator()
    {
        for(int y = 0; y < _gridData._height; y++)
        {
            for(int x = 0; x < _gridData._widht; x++)
            {
                if(_gridData.Get(x,y) == CellType.Occupied)
                {
                    Vector3 pos = new Vector3(x + 0.5f, 0f, y + 0.5f);
                    GameObject tableInstance = Instantiate(_tablePrefab, pos, Quaternion.identity);
                    PlaceableObject placeable = tableInstance.GetComponent<PlaceableObject>();
                    placeable.SetGridManager(this);
                    placeable.InstancePlaceableObjectCreated(x,y);
                }
            }
        }
    }
}
