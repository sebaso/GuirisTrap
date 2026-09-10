using UnityEngine;
public enum CameraView
{
    Perspective,
    TopDown,
    WallNorth,
    WallEast,
    WallWest
}

public class CameraController : MonoBehaviour
{
    [SerializeField] 
    private Camera _mainCamera;
    [SerializeField] 
    private float _transitionSpeed = 3f;
    private Vector3 _velocityPos;
    public System.Action<CameraView> OnViewChanged;

    public System.Action<GridZone> OnActiveZoneChanged;

    private GridZone _activeZone;
    public GridZone ActiveZone => _activeZone;

    private Transform _currentTarget;
    private CameraView _currentView = CameraView.Perspective;

    private bool _inputLocked = false;
    public bool IsLocked => _inputLocked;

    public bool IsTransitioning { get; private set; }
    public void SetInputLocked(bool locked) => _inputLocked = locked;

    void Update()
    {
        if (_currentTarget == null) return;
        _mainCamera.transform.position = Vector3.SmoothDamp(_mainCamera.transform.position,_currentTarget.position,ref _velocityPos,1f / _transitionSpeed);

        _mainCamera.transform.rotation = Quaternion.SlerpUnclamped(_mainCamera.transform.rotation,_currentTarget.rotation,Time.deltaTime * _transitionSpeed);
        
        IsTransitioning = Vector3.Distance(_mainCamera.transform.position, _currentTarget.position) > 0.05f;
    }

    public void SetActiveZone(GridZone zone)
    {
        if (zone == null || zone == _activeZone) return;
        _activeZone = zone;
        OnActiveZoneChanged?.Invoke(zone);
        SetView(CameraView.Perspective);
    }

    public void SetView(CameraView view)
    {
        if (_inputLocked || _activeZone == null) return;

        Transform pivot = _activeZone.GetCameraPivot(view);
        if (pivot == null) return;

        _currentView = view;
        _currentTarget = pivot;
        _mainCamera.orthographic = view == CameraView.TopDown;
        OnViewChanged?.Invoke(view);
    }

    public void CycleWalls(int direction)
    {
        if (_activeZone == null) return;

        CameraView[] walls = { CameraView.WallEast, CameraView.WallNorth, CameraView.WallWest };
        var owned = System.Array.FindAll(walls, w => _activeZone.OwnsView(w));
        if (owned.Length == 0) return;

        int currentIndex = System.Array.IndexOf(owned, _currentView);
        if (currentIndex == -1) currentIndex = 0;
        else currentIndex = (currentIndex + direction + owned.Length) % owned.Length;

        SetView(owned[currentIndex]);
    }

    public CameraView CurrentView => _currentView;
    public void SetPerspectiveView() => SetView(CameraView.Perspective);
    public void SetTopDownView()     => SetView(CameraView.TopDown);
    public void SetWallView()        => SetView(CameraView.WallNorth);
    public void NextWall()     => CycleWalls(+1);
    public void PreviousWall() => CycleWalls(-1);
}