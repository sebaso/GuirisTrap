using System.Collections.Generic;
using UnityEngine;

public class GridZone : MonoBehaviour, IGridWorldResolver
{
    [Header("Identidad de la zona")]
    [SerializeField]
    private ZoneId _zoneId;
    public ZoneId ZoneId => _zoneId;

    [Header("Datos y registro")]
    [SerializeField]
    private VoxelGridData _voxelData;
    [SerializeField]
    private PlaceableInstanceRegistry _registry;
    public VoxelGridData VoxelData => _voxelData;
    public PlaceableInstanceRegistry Registry => _registry;

    [Header("Resolver alternativo")]
    [SerializeField]
    private MonoBehaviour _resolverOverride;
    public IGridWorldResolver Resolver => (_resolverOverride as IGridWorldResolver) ?? this;

    [Header("Proyecciones")]
    [SerializeField]
    private List<GridZoneProjectionEntry> _projections = new();
    [SerializeField]
    private CameraController _cameraController;

    [Header("Pivotes de cámara")]
    [SerializeField]
    private List<GridZoneCamera> _cameraPivots = new();

    public Transform GetCameraPivot(CameraView view)
    {
        foreach (var entry in _cameraPivots)
            if (entry.view == view)
                return entry.pivot;
        return null;
    }

    public event System.Action OnGridChanged;

    public static readonly List<GridZone> ActiveZones = new();
    public static event System.Action<GridZone> OnZoneRegistered;

    void OnEnable()
    {
        ActiveZones.Add(this);
        OnZoneRegistered?.Invoke(this);
        GridManager.OnGridChanged += HandleGridDataChanged;
        if (_cameraController != null) _cameraController.OnViewChanged += HandleViewChanged;
    }

    void OnDisable()
    {
        ActiveZones.Remove(this);
        GridManager.OnGridChanged -= HandleGridDataChanged;
        if (_cameraController != null) _cameraController.OnViewChanged -= HandleViewChanged;
    }

    void Start()
    {
        foreach (var projection in DistinctProjections())
            projection.Init();

        if (_cameraController != null) HandleViewChanged(_cameraController.CurrentView);
    }

    private void HandleGridDataChanged(VoxelGridData changedData)
    {
        if (changedData != _voxelData) return;
        foreach (var projection in DistinctProjections())
            projection.RefreshAll();
        OnGridChanged?.Invoke();
    }

    private IEnumerable<IVoxelProjection> DistinctProjections()
    {
        var seen = new HashSet<IVoxelProjection>();
        foreach (var entry in _projections)
        {
            var p = entry.Projection;
            if (p != null && seen.Add(p))
                yield return p;
        }
    }

    private IVoxelProjection GetProjection(CameraView view)
    {
        foreach (var entry in _projections)
            if (entry.view == view)
                return entry.Projection;
        return null;
    }

    public bool OwnsView(CameraView view) => GetProjection(view) != null;

    public static PlaceableSurface SurfaceForView(CameraView view)
    {
        return (view == CameraView.WallNorth || view == CameraView.WallEast || view == CameraView.WallWest)
            ? PlaceableSurface.Wall
            : PlaceableSurface.Floor; // Perspective y TopDown → suelo
    }

    public void HandleViewChanged(CameraView view)
    {
        foreach (var projection in DistinctProjections())
        {
            bool visible = _projections.Exists(e => e.Projection == projection && e.view == view);
            projection.SetVisible(visible);
        }
    }

    public bool TryGetWorldTransform(CameraView view, Vector3Int voxel, out Vector3 pos, out Quaternion rot)
    {
        pos = default;
        rot = Quaternion.identity;
        var projection = GetProjection(view);
        return projection != null && projection.TryGetWorldTransform(voxel, out pos, out rot);
    }

    public bool TryGetVoxelUnderRay(CameraView view, Ray ray, out Vector3Int voxel)
    {
        voxel = default;
        var projection = GetProjection(view);
        return projection != null && projection.TryGetVoxelUnderRay(ray, out voxel);
    }

    public void RefreshActive(CameraView view) => GetProjection(view)?.RefreshAll();

    public void SetPreview(CameraView view, Vector3Int voxel, CellVisualState state)
        => GetProjection(view)?.SetCellVisual(voxel, state);
}