using UnityEngine;
using UnityEngine.SceneManagement;

public class PerspectiveVoxelView : MonoBehaviour
{
    [SerializeField] 
    private VoxelGridData _voxelData;
    [Tooltip("Transform que define el origen del mundo para la matriz (normalmente el mismo que usa el GameGridManager del suelo).")]
    [SerializeField] 
    private Transform _roomOrigin;
    [SerializeField] 
    private GridVisualCell _cellPrefab;

    private GridVisualCell[,,] _cubes;

    public void Init()
    {
        if (_voxelData == null || _roomOrigin == null) return;
        if (SceneManager.GetActiveScene().name != "PreparationScene") return;

        _cubes = new GridVisualCell[_voxelData.width, _voxelData.height, _voxelData.depth];

        for (int z = 0; z < _voxelData.depth; z++)
        {
            for (int y = 0; y < _voxelData.height; y++)
            {
                for (int x = 0; x < _voxelData.width; x++)
                {
                    Vector3 localPos = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
                    Vector3 worldPos = _roomOrigin.TransformPoint(localPos);

                    GridVisualCell cube = Instantiate(_cellPrefab, worldPos, _roomOrigin.rotation, transform);
                    _cubes[x, y, z] = cube;
                }
            }
        }
    }

    void Update()
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (_cubes == null) return;

        for (int z = 0; z < _voxelData.depth; z++)
        {
            for (int y = 0; y < _voxelData.height; y++)
            {
                for (int x = 0; x < _voxelData.width; x++)
                {
                    CellType type = _voxelData.GetType(x, y, z);
                    // Ocupado o bloqueado: se resalta. Vacío: invisible (estado Default).
                    CellVisualState state = (type == CellType.Empty) ? CellVisualState.Default : CellVisualState.Blocked;
                    _cubes[x, y, z].SetState(state);
                }
            }
        }
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}