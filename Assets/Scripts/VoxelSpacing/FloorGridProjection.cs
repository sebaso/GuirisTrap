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
    public bool TryGetWorldTransform(Vector3Int voxel, out Vector3 pos, out Quaternion rot)
    {
        pos = default;
        rot = Quaternion.identity;

        if (voxel.y != 0 || voxel.x < 0 || voxel.z < 0 || voxel.x >= _voxelData.width || voxel.z >= _voxelData.depth)
            return false;

        pos = transform.TransformPoint(new Vector3(voxel.x + 0.5f, 0f, voxel.z + 0.5f));
        rot = transform.rotation;
        return true;
    }
    public bool TryGetVoxelUnderRay(Ray ray, out Vector3Int voxel)
    {
        voxel = default;
        Plane plane = new Plane(transform.up, transform.position);
        if (!plane.Raycast(ray, out float dist)) return false;

        Vector3 local = transform.InverseTransformPoint(ray.GetPoint(dist));
        int x = Mathf.FloorToInt(local.x);
        int z = Mathf.FloorToInt(local.z);

        if (x < 0 || z < 0 || x >= _voxelData.width || z >= _voxelData.depth) return false;

        voxel = new Vector3Int(x, 0, z);
        return true;
    }

    public void SetCellVisual(Vector3Int voxel, CellVisualState state)
    {
        if (_cells == null) return;
        if (voxel.y != 0 || voxel.x < 0 || voxel.z < 0 || voxel.x >= _voxelData.width || voxel.z >= _voxelData.depth) return;
        _cells[voxel.x, voxel.z].SetState(state);
    }
    public void SetVisible(bool visible)
    {
        if (_cells == null) return;

        for (int z = 0; z < _voxelData.depth; z++)
            for (int x = 0; x < _voxelData.width; x++)
                if (_cells[x, z] != null)
                    _cells[x, z].SetVisible(visible);
    }
    public bool TryGetVoxelAtWorldPos(Vector3 worldPos, out Vector3Int voxel)
    {
        voxel = default;
        Vector3 local = transform.InverseTransformPoint(worldPos);
        int x = Mathf.FloorToInt(local.x);
        int z = Mathf.FloorToInt(local.z);

        if (x < 0 || z < 0 || x >= _voxelData.width || z >= _voxelData.depth) return false;

        voxel = new Vector3Int(x, 0, z);
        return true;
    }
}