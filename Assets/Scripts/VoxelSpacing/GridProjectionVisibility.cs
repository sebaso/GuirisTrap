using UnityEngine;

public class GridProjectionVisibility : MonoBehaviour
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
        if (_gridManager != null) _gridManager.OnGridReady += InitProjections;

        if (_cameraController != null) _cameraController.OnViewChanged += HandleViewChanged;
    }

    void OnDisable()
    {
        if (_gridManager != null) _gridManager.OnGridReady -= InitProjections;
        if (_cameraController != null) _cameraController.OnViewChanged -= HandleViewChanged;
    }

    void Start()
    {
        if (_cameraController != null)
            HandleViewChanged(_cameraController.CurrentView);
    }
    private void InitProjections()
    {
        _floor?.Init();
        _wallNorth?.Init();
        _wallEast?.Init();
        _wallWest?.Init();

        if (_cameraController != null)
            HandleViewChanged(_cameraController.CurrentView);
    }
    public void HandleViewChanged(CameraView view)
    {
            Debug.Log($"[GridProjectionVisibility] HandleViewChanged → {view}");
        _floor?.SetVisible(view == CameraView.Perspective || view == CameraView.TopDown);
        _wallNorth?.SetVisible(view == CameraView.WallNorth);
        _wallEast?.SetVisible(view == CameraView.WallEast);
        _wallWest?.SetVisible(view == CameraView.WallWest);
    }
}