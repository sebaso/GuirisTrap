using UnityEngine;
using UnityEngine.SceneManagement;

public class GameGridManager : MonoBehaviour
{
    [SerializeField]
    private GridData _gridData;
    [SerializeField]
    private GridVisualCell _gridViewCellPrefab;

    private GridVisualCell[,] _cells;

    public GridData GetGridData => _gridData;

    void Start()
    {
        if(_gridData == null) return;
        
        if(SceneManager.GetActiveScene().name != "PreparationScene") return;
        _cells = new GridVisualCell[_gridData.width, _gridData.height];

        for(int y = 0; y < _gridData.height; y++)
        {
            for(int x = 0; x < _gridData.width; x++)
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
        if(newPlaceableObjectX < 0 || newPlaceableObjectY < 0 || newPlaceableObjectX >= _gridData.width || newPlaceableObjectY >= _gridData.height) return false;

        CellType type = _gridData.GetType(newPlaceableObjectX, newPlaceableObjectY);

        if(type == CellType.Empty || _gridData._cells[newPlaceableObjectY * _gridData.width + newPlaceableObjectX] == _gridData._cells[newPlaceableObjectStartAtY * _gridData.width + newPlaceableObjectStartAtX])
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
            for(int x = 0; x < _gridData.width; x++)
            {
                _cells[x,y].SetState(CellVisualState.Default);
            }
        }
    }
    public void ClearLastCell(int lastCellX, int lastCellY)
    {
        if(lastCellX < 0 || lastCellY < 0 || lastCellX >= _gridData.width || lastCellY >= _gridData.height) return;

        _cells[lastCellX, lastCellY].SetState(CellVisualState.Default);
    }
    public void SaveGrid(int newPlaceableObjectX, int newPlaceableObjectY, int startPlaceableObjectX, int startPlaceableObjectY, PlaceableItemData itemData)
    {
        if(_gridData == null ) return;

        if(newPlaceableObjectX < 0 || newPlaceableObjectY < 0 || newPlaceableObjectX >= _gridData.width || newPlaceableObjectY >= _gridData.height) return;

        _gridData.SetType(newPlaceableObjectX, newPlaceableObjectY, CellType.Occupied);
        _gridData.SetItem(newPlaceableObjectX, newPlaceableObjectY, itemData);
        if(startPlaceableObjectX != -1 && startPlaceableObjectY != -1 && startPlaceableObjectX != newPlaceableObjectX || startPlaceableObjectY != newPlaceableObjectY)
            _gridData.SetType(startPlaceableObjectX, startPlaceableObjectY, CellType.Empty);
            _gridData.SetItem(startPlaceableObjectX, startPlaceableObjectY, null);
    }
    public void PlaceableGenerator()
    {
        Transform placeableFolder = new GameObject("PlaceableItems").transform;
        Debug.Log("Vamoh a crear objetos");
        for(int y = 0; y < _gridData.height; y++)
        {
            for(int x = 0; x < _gridData.width; x++)
            {
                GridCell cell = _gridData._cells[y * _gridData.width + x];
                if(cell.item == null)
                    Debug.Log("El item está vacio");
                if(_gridData.GetType(x,y) == CellType.Occupied && cell.item != null)
                {
                                    Debug.Log("Vamoh a crear un: "+ cell.item);
                    Vector3 pos = new Vector3(x, 0f, y);
                    Vector3 finalPos = pos + cell.item.placementOffset;
                    GameObject tableInstance = Instantiate(cell.item.prefab, finalPos, Quaternion.identity, placeableFolder);
                    PlaceableObject placeable = tableInstance.GetComponent<PlaceableObject>();
                    if (placeable != null)
                    {
                        placeable.SetGridManager(this);
                        placeable.InstancePlaceableObjectCreated(x,y);
                        placeable.Init(cell.item);
                    }
                }
            }
        }
    }
}
