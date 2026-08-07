using UnityEngine;

public class GridProjectionVisibility : MonoBehaviour, IGridWorldResolver
{
    [SerializeField] 
    private GridManager _gridManager;
    [SerializeField] 
    private CameraController _cameraController;

    [Header("Proyecciones")]
    [SerializeField] 
    private FloorGridProjection _floor;
    [SerializeField] 
    private WallGridProjection _wallNorth;
    [SerializeField] 
    private WallGridProjection _wallEast;
    [SerializeField] 
    private WallGridProjection _wallWest;

    void OnEnable()
    {
        if (_cameraController != null) _cameraController.OnViewChanged += HandleViewChanged;
        if (_gridManager != null) _gridManager.OnGridChanged += RefreshAllProjections;
    }

    void OnDisable()
    {
        if (_cameraController != null) _cameraController.OnViewChanged -= HandleViewChanged;
        if (_gridManager != null) _gridManager.OnGridChanged -= RefreshAllProjections;
    }

    void Start()
    {
        _floor?.Init();
        _wallNorth?.Init();
        _wallEast?.Init();
        _wallWest?.Init();

        if (_cameraController != null) HandleViewChanged(_cameraController.CurrentView);
    }
    public static PlaceableSurface SurfaceForView(CameraView view)
    {
        return (view == CameraView.WallNorth || view == CameraView.WallEast || view == CameraView.WallWest)
            ? PlaceableSurface.Wall
            : PlaceableSurface.Floor; // Perspective y TopDown → suelo
    }
    private void RefreshAllProjections()
    {
        _floor?.RefreshAll();
        _wallNorth?.RefreshAll();
        _wallEast?.RefreshAll();
        _wallWest?.RefreshAll();
    }
    public void HandleViewChanged(CameraView view)
    {
        _floor?.SetVisible(view == CameraView.Perspective || view == CameraView.TopDown);
        _wallNorth?.SetVisible(view == CameraView.WallNorth);
        _wallEast?.SetVisible(view == CameraView.WallEast);
        _wallWest?.SetVisible(view == CameraView.WallWest);
    }
    public bool TryGetWorldTransform(CameraView view, Vector3Int voxel, out Vector3 pos, out Quaternion rot)
    {
        pos = default;
        rot = Quaternion.identity;

        switch (view)
        {
            case CameraView.Perspective:
            case CameraView.TopDown:
                return _floor != null && _floor.TryGetWorldTransform(voxel, out pos, out rot);
            case CameraView.WallNorth:
                return _wallNorth != null && _wallNorth.TryGetWorldTransform(voxel, out pos, out rot);
            case CameraView.WallEast:
                return _wallEast != null && _wallEast.TryGetWorldTransform(voxel, out pos, out rot);
            case CameraView.WallWest:
                return _wallWest != null && _wallWest.TryGetWorldTransform(voxel, out pos, out rot);
            default:
                return false;
        }
    }
    public bool TryGetVoxelUnderRay(CameraView view, Ray ray, out Vector3Int voxel)
    {
        voxel = default;
        return view switch
        {
            CameraView.Perspective or CameraView.TopDown => _floor != null && _floor.TryGetVoxelUnderRay(ray, out voxel),
            CameraView.WallNorth => _wallNorth != null && _wallNorth.TryGetVoxelUnderRay(ray, out voxel),
            CameraView.WallEast  => _wallEast != null && _wallEast.TryGetVoxelUnderRay(ray, out voxel),
            CameraView.WallWest  => _wallWest != null && _wallWest.TryGetVoxelUnderRay(ray, out voxel),
            _ => false
        };
    }

    public void RefreshActive(CameraView view)
    {
        switch (view)
        {
            case CameraView.Perspective: case CameraView.TopDown: _floor?.RefreshAll(); break;
            case CameraView.WallNorth: _wallNorth?.RefreshAll(); break;
            case CameraView.WallEast:  _wallEast?.RefreshAll(); break;
            case CameraView.WallWest:  _wallWest?.RefreshAll(); break;
        }
    }

    public void SetPreview(CameraView view, Vector3Int voxel, CellVisualState state)
    {
        switch (view)
        {
            case CameraView.Perspective: case CameraView.TopDown: _floor?.SetCellVisual(voxel, state); break;
            case CameraView.WallNorth: _wallNorth?.SetCellVisual(voxel, state); break;
            case CameraView.WallEast:  _wallEast?.SetCellVisual(voxel, state); break;
            case CameraView.WallWest:  _wallWest?.SetCellVisual(voxel, state); break;
        }
    }
}