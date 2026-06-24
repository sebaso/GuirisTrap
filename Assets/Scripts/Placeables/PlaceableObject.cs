using UnityEngine;

public class PlaceableObject : MonoBehaviour
{
    private int _lastCellX = -1;
    private int _lastCellY = -1;

    private int _cellOccupiedAtStartX = -1;
    private int _cellOccupiedAtStartY = -1;

    private int _actualCellX = -1;
    private int _actualCellY = -1;

    private bool _isSelected = false;
    private bool _isMoved = false;
    private bool _wasInitialized = false;

    public bool OnMoved => _isMoved;
    public int CurrentCellX => _actualCellX;
    public int CurrentCellY => _actualCellY;
    public int StartCellX => _cellOccupiedAtStartX;
    public int StartCellY => _cellOccupiedAtStartY;
    public int LastCellX => _lastCellX;
    public int LastCellY => _lastCellY;

    private GameGridManager _gridManager;
    public GameGridManager GridManager => _gridManager;

    private bool _isStoraged = false;
    public bool Storaged => _isStoraged;

    private PlaceableItemData _itemData;
    private bool _isValid = true;
    public bool IsValid => _isValid;

    void Awake()
    {
        _gridManager = FindFirstObjectByType<GameGridManager>();
    }

    void Start()
    {
        if (!_wasInitialized)
        {
            _actualCellX = Mathf.FloorToInt(transform.position.x);
            _actualCellY = Mathf.FloorToInt(transform.position.z);
            _cellOccupiedAtStartX = _actualCellX;
            _cellOccupiedAtStartY = _actualCellY;
        }
    }


public void Init(PlaceableItemData itemData)
    {
        _itemData = itemData;
    }

    void Update()
    {
        MovePlaceableObject();
        SetStorage();
    }

    public void SetGridManager(GameGridManager gridManager)
    {
        _gridManager = gridManager;
    }

    public void IsPlacedAtCell()
    {
        _cellOccupiedAtStartX = _actualCellX;
        _cellOccupiedAtStartY = _actualCellY;
        _isMoved = false;
    }

    public void InstancePlaceableObjectCreated(int x, int y)
    {
        _wasInitialized = true;
        _cellOccupiedAtStartX = x;
        _cellOccupiedAtStartY = y;
        _actualCellX = x;
        _actualCellY = y;
        _lastCellX = x;
        _lastCellY = y;
        _isMoved = false;
    }    

    private void MovePlaceableObject()
    {
        if (!_isSelected) return;

        Vector3 localPos = _gridManager.transform.InverseTransformPoint(transform.position);

        _actualCellX = Mathf.FloorToInt(localPos.x);
        _actualCellY = Mathf.FloorToInt(localPos.z);

        _gridManager.ClearLastCell(_lastCellX, _lastCellY);
        _lastCellX = -1;
        _lastCellY = -1;

        if (_gridManager.UpdateVisualCell(_actualCellX, _actualCellY, _cellOccupiedAtStartX, _cellOccupiedAtStartY, _itemData))
        {
            _lastCellX = _actualCellX;
            _lastCellY = _actualCellY;
        }
        
        _isMoved = true;
    }

    public void RestartCell()
    {
        _actualCellX = _cellOccupiedAtStartX;
        _actualCellY = _cellOccupiedAtStartY;
    }

    public void SetStorage()
    {
        if (_gridManager == null) return;

        _isStoraged = _gridManager.GetGridData.GetIsWarehouse(_cellOccupiedAtStartX, _cellOccupiedAtStartY);
    }

    public bool IsSelected() { return _isSelected; }
    public void Select(bool isSelected) { _isSelected = isSelected; }
    public PlaceableItemData GetItemData() { return _itemData; }

    public void SetValid(bool valid)
    {
        _isValid = valid;
        Renderer[] renders = GetComponentsInChildren<Renderer>();
        if (renders == null) return;

        foreach (var r in renders)
        {
            r.material.color = valid ? Color.green : Color.red;
        }
    }
}
