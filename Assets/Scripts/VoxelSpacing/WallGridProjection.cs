using UnityEngine;

public enum WallSide
{
    North,
    East,
    West
}

public class WallGridProjection : MonoBehaviour, IVoxelProjection
{
    [SerializeField] 
    private VoxelGridData _voxelData;
    [SerializeField] 
    private GridVisualCell _cellPrefab;
    [SerializeField] 
    private WallSide _side;

    private GridVisualCell[,] _cells;

    private int USize => _side == WallSide.North ? _voxelData.width : _voxelData.depth;
    private int VSize => _voxelData.height;

    private void GetVoxelCoords(int u, int v, out int x, out int y, out int z)
    {
        y = v;
        switch (_side)
        {
            case WallSide.North:
                x = u;
                z = _voxelData.depth - 1;
                break;
            case WallSide.East:
                x = _voxelData.width - 1;
                z = u;
                break;
            default: // West
                x = 0;
                z = u;
                break;
        }
    }

    private Vector3 GetLocalPos(int u, int v)
    {
        switch (_side)
        {
            case WallSide.North:
                return new Vector3(u, v, _voxelData.depth);
            case WallSide.East:
                return new Vector3(_voxelData.width, v, u);
            default: // West
                return new Vector3(0f, v, u);
        }
    }

    private Vector3 InwardNormalLocal => _side switch
    {
        WallSide.North => Vector3.back,
        WallSide.East  => Vector3.left,
        _              => Vector3.right, // West
    };

    private Quaternion GetCellRotation()
    {
        Quaternion localRot = Quaternion.FromToRotation(Vector3.up, InwardNormalLocal);
        return transform.rotation * localRot * RotationCorrection();
    }    
    private Quaternion RotationCorrection()
    {
        if (_side == WallSide.North)
            return Quaternion.Euler(0f, 180f, 0f);
 
        if (_side == WallSide.East)
            return Quaternion.Euler(0f, -90f, 0f);
 
        if (_side == WallSide.West)
            return Quaternion.Euler(0f, 90f, 0f);
 
        return Quaternion.identity;
    }

    [ContextMenu("TEST: Init (instanciar cubos visuales)")]
    public void Init()
    {
        if (_voxelData == null || _cellPrefab == null)
        {
            Debug.LogWarning("[WallGridProjection] Falta VoxelGridData o el prefab de celda.");
            return;
        }

        int uSize = USize;
        int vSize = VSize;
        _cells = new GridVisualCell[uSize, vSize];

        for (int v = 0; v < vSize; v++)
        {
            for (int u = 0; u < uSize; u++)
            {
                Vector3 worldPos = transform.TransformPoint(GetLocalPos(u, v));

                GridVisualCell cell = Instantiate(_cellPrefab, worldPos, GetCellRotation(), transform);
                cell.Init(u, v);
                _cells[u, v] = cell;
            }
        }

        RefreshAll();
    }

    [ContextMenu("TEST: Refrescar estado visual")]
    public void RefreshAll()
    {
        if (_cells == null || _voxelData == null) return;

        int uSize = USize;
        int vSize = VSize;

        for (int v = 0; v < vSize; v++)
        {
            for (int u = 0; u < uSize; u++)
            {
                GetVoxelCoords(u, v, out int x, out int y, out int z);
                CellType type = _voxelData.GetType(x, y, z);
                CellVisualState state = (type == CellType.Empty) ? CellVisualState.Default : CellVisualState.Blocked;
                _cells[u, v].SetState(state);
            }
        }
    }
    public bool TryGetWorldTransform(Vector3Int voxel, out Vector3 pos, out Quaternion rot)
    {
        pos = default;
        rot = Quaternion.identity;

        int u, v;
        switch (_side)
        {
            case WallSide.North:
                if (voxel.z != _voxelData.depth - 1) return false;
                u = voxel.x; v = voxel.y;
                break;
            case WallSide.East:
                if (voxel.x != _voxelData.width - 1) return false;
                u = voxel.z; v = voxel.y;
                break;
            default: // West
                if (voxel.x != 0) return false;
                u = voxel.z; v = voxel.y;
                break;
        }

        pos = transform.TransformPoint(GetLocalPos(u, v));
        rot = GetCellRotation();
        return true;
    }
    public bool TryGetVoxelUnderRay(Ray ray, out Vector3Int voxel)
    {
        voxel = default;

        Vector3 localNormal = _side == WallSide.North ? Vector3.forward : Vector3.right;
        Vector3 worldNormal = transform.TransformDirection(localNormal);
        Vector3 pointOnPlane = transform.TransformPoint(GetLocalPos(0, 0));

        Plane plane = new Plane(worldNormal, pointOnPlane);
        if (!plane.Raycast(ray, out float dist)) return false;

        Vector3 local = transform.InverseTransformPoint(ray.GetPoint(dist));

        int u = _side == WallSide.North ? Mathf.FloorToInt(local.x) : Mathf.FloorToInt(local.z);
        int v = Mathf.FloorToInt(local.y);

        if (u < 0 || v < 0 || u >= USize || v >= VSize) return false;

        GetVoxelCoords(u, v, out int x, out int y, out int z);
        voxel = new Vector3Int(x, y, z);
        return true;
    }

    public void SetCellVisual(Vector3Int voxel, CellVisualState state)
    {
        if (_cells == null || !TryVoxelToUV(voxel, out int u, out int v)) return;
        _cells[u, v].SetState(state);
    }

    private bool TryVoxelToUV(Vector3Int voxel, out int u, out int v)
    {
        u = v = 0;
        switch (_side)
        {
            case WallSide.North:
                if (voxel.z != _voxelData.depth - 1) return false;
                u = voxel.x; v = voxel.y;
                break;
            case WallSide.East:
                if (voxel.x != _voxelData.width - 1) return false;
                u = voxel.z; v = voxel.y;
                break;
            default:
                if (voxel.x != 0) return false;
                u = voxel.z; v = voxel.y;
                break;
        }
        return u >= 0 && v >= 0 && u < USize && v < VSize;
    }
    public void SetVisible(bool visible)
    {
        if (_cells == null) return;

        int uSize = USize;
        int vSize = VSize;

        for (int v = 0; v < vSize; v++)
            for (int u = 0; u < uSize; u++)
                if (_cells[u, v] != null)
                    _cells[u, v].SetVisible(visible);
    }
}