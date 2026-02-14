using Unity.VisualScripting;
using UnityEngine;

public class GameGridManager : MonoBehaviour
{
    [SerializeField]
    private GridData _gridData;
    [SerializeField]
    private GameObject _gridViewCellPrefab;
    private GameObject[,] _gridViewCells;
    [SerializeField]
    private GameObject _table;
    [SerializeField]
    private Material __gridViewCellsMaterialDefault;
    [SerializeField]
    private Material __gridViewCellsMaterialEmpty;
    [SerializeField]
    private Material __gridViewCellsMaterialBlocked;
    void Start()
    {
        if(_gridData == null) return;
        _gridViewCells = new GameObject[_gridData._widht, _gridData._height];
        for(int y = 0; y < _gridData._height; y++)
        {
            for(int x = 0; x < _gridData._widht; x++)
            {
                GameObject cell = Instantiate(_gridViewCellPrefab);
                cell.transform.position = new Vector3 (x+ 0.5f,0f,y + 0.5f);
                _gridViewCells[x,y] = cell;
            }
        }
    }
    void Update()
    {
        if(_table == null || _gridData == null) return;

        for(int y = 0; y < _gridData._height; y++)
        {
            for(int x = 0; x < _gridData._widht; x++)
            {
                if(_table.transform.position.x >= x && _table.transform.position.x <  x + 1 && _table.transform.position.z >= y && _table.transform.position.z <  y + 1)
                {
                    if(_gridData.Get(x,y) == CellType.Empty)
                    {
                        _gridViewCells[x,y].GetComponentInChildren<MeshRenderer>().material = __gridViewCellsMaterialEmpty;
                    }
                    else
                    {
                        _gridViewCells[x,y].GetComponentInChildren<MeshRenderer>().material = __gridViewCellsMaterialBlocked;
                    }
                }
                else
                {
                        _gridViewCells[x,y].GetComponentInChildren<MeshRenderer>().material = __gridViewCellsMaterialDefault;
                }
            }
        }
    }
}
