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

    [Header("Pivots")]
    [SerializeField] 
    private Transform _pivotPerspective;
    [SerializeField] 
    private Transform _pivotTopDown;
    [SerializeField] 
    private Transform _pivotWallNorth;
    [SerializeField] 
    private Transform _pivotWallEast;
    [SerializeField] 
    private Transform _pivotWallWest;

    private Transform _currentTarget;
    private CameraView _currentView = CameraView.Perspective;

    private bool _inputLocked = false;
    public bool IsLocked => _inputLocked;

    public bool IsTransitioning { get; private set; }
    public void SetInputLocked(bool locked) => _inputLocked = locked;

    void Start()
    {
        _currentTarget = _pivotPerspective;
        
        _mainCamera.transform.position = _currentTarget.position;
        _mainCamera.transform.rotation = _currentTarget.rotation;
    }

    void Update()
    {
        if (_currentTarget == null) return;
        _mainCamera.transform.position = Vector3.SmoothDamp(_mainCamera.transform.position,_currentTarget.position,ref _velocityPos,1f / _transitionSpeed);

        _mainCamera.transform.rotation = Quaternion.SlerpUnclamped(_mainCamera.transform.rotation,_currentTarget.rotation,Time.deltaTime * _transitionSpeed);
        
        IsTransitioning = Vector3.Distance(_mainCamera.transform.position, _currentTarget.position) > 0.05f;
    }

    public void SetView(CameraView view)
    {
        if (_inputLocked) return;
        _currentView = view;
        switch (view)
        {
            case CameraView.Perspective: 
                _currentTarget = _pivotPerspective;
                break;
            case CameraView.TopDown:     
                _currentTarget = _pivotTopDown;
                break;
            case CameraView.WallNorth:   
                _currentTarget = _pivotWallNorth;
                break;
            case CameraView.WallEast:    
                _currentTarget = _pivotWallEast;
                break;
            case CameraView.WallWest:    
                _currentTarget = _pivotWallWest;
                break;
            default:                     
                _currentTarget = _pivotPerspective;
                break;
        }
        _mainCamera.orthographic = view == CameraView.TopDown;
        OnViewChanged?.Invoke(view);
    }

    public void CycleWalls(int direction)
    {
        CameraView[] walls = { CameraView.WallEast, CameraView.WallNorth, CameraView.WallWest };
        int currentIndex = System.Array.IndexOf(walls, _currentView);

        if (currentIndex == -1) currentIndex = 0;
        else currentIndex = (currentIndex + direction + walls.Length) % walls.Length;

        SetView(walls[currentIndex]);
    }

    public CameraView CurrentView => _currentView;
    public void SetPerspectiveView() => SetView(CameraView.Perspective);
    public void SetTopDownView()     => SetView(CameraView.TopDown);
    public void SetWallView()        => SetView(CameraView.WallNorth);
    public void NextWall()     => CycleWalls(+1);
    public void PreviousWall() => CycleWalls(-1);
}