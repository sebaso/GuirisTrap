using UnityEngine;


public class FloorGridProjection : MonoBehaviour
{
    [SerializeField] 
    private VoxelGridData _voxelData;
    [SerializeField] 
    private GridVisualCell _cellPrefab;

    private GridVisualCell[,] _cells;

    [ContextMenu("TEST: Init (instanciar cubos visuales)")]
    public void Init()
    {
        if (_voxelData == null || _cellPrefab == null)
        {
            Debug.LogWarning("[FloorGridProjection] Falta VoxelGridData o el prefab de celda.");
            return;
        }

        _cells = new GridVisualCell[_voxelData.width, _voxelData.depth];

        for (int z = 0; z < _voxelData.depth; z++)
        {
            for (int x = 0; x < _voxelData.width; x++)
            {
                Vector3 localPos = new Vector3(x + 0.5f, 0f, z + 0.5f);
                Vector3 worldPos = transform.TransformPoint(localPos);

                GridVisualCell cell = Instantiate(_cellPrefab, worldPos, transform.rotation, transform);
                cell.Init(x, z);
                _cells[x, z] = cell;
            }
        }

        RefreshAll();
    }

    [ContextMenu("TEST: Refrescar estado visual")]
    public void RefreshAll()
    {
        if (_cells == null || _voxelData == null) return;

        for (int z = 0; z < _voxelData.depth; z++)
        {
            for (int x = 0; x < _voxelData.width; x++)
            {
                CellType type = _voxelData.GetType(x, 0, z);
                CellVisualState state = (type == CellType.Empty) ? CellVisualState.Default : CellVisualState.Blocked;
                _cells[x, z].SetState(state);
            }
        }
    }
    public void SetVisible(bool visible)
    {
        if (_cells == null) return;

        for (int z = 0; z < _voxelData.depth; z++)
            for (int x = 0; x < _voxelData.width; x++)
                if (_cells[x, z] != null)
                    _cells[x, z].SetVisible(visible);
    }
}