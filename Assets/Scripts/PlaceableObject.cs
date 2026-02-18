using System;
using Unity.Collections;
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
    public bool OnMoved => _isMoved;
    public int CurrentCellX => _actualCellX;
    public int CurrentCellY => _actualCellY;
    public int StartCellX => _cellOccupiedAtStartX;
    public int StartCellY => _cellOccupiedAtStartY;

    private GameGridManager _gridManager;

    void Awake()
    {
        _gridManager = FindFirstObjectByType<GameGridManager>();
    }
    void Start()
    {
        if(_cellOccupiedAtStartX == -1 && _cellOccupiedAtStartY == -1)
        {
            _actualCellX = Mathf.FloorToInt(transform.position.x);
            _actualCellY = Mathf.FloorToInt(transform.position.z);
            _cellOccupiedAtStartX = _actualCellX;
            _cellOccupiedAtStartY = _actualCellY;
        }
    }

    void Update()
    {
        movePlaceableObject();
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
        _cellOccupiedAtStartX = x;
        _cellOccupiedAtStartY = y;
        _actualCellX = x;
        _actualCellY = y;
        _lastCellX = x;
        _lastCellY = y;
        _isMoved = false;
    }

    private void movePlaceableObject()
    {
        _actualCellX = Mathf.FloorToInt(transform.position.x);
        _actualCellY = Mathf.FloorToInt(transform.position.z);

        if(_actualCellX == _lastCellX && _actualCellY == _lastCellY) return;

        _gridManager.ClearLastCell(_lastCellX,_lastCellY);

        _lastCellX = -1;
        _lastCellY = -1;

       if( _gridManager.UpdateVisualCell(_actualCellX, _actualCellY))
        {
            _lastCellX = _actualCellX;
            _lastCellY = _actualCellY;
        }
        _isMoved = true;
    }

    public bool IsSelected () { return _isSelected; }
    public void Select(bool isSelected) { _isSelected = isSelected; }
}
