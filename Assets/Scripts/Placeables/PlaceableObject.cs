using UnityEngine;
using System.Collections;

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

    private PlaceableItemData _itemData;
    private bool _isValid = true;
    private Vector3Int _anchorVoxel;
    private CameraView _placedView;

    public bool OnMoved => _isMoved;
    public int CurrentCellX => _actualCellX;
    public int CurrentCellY => _actualCellY;
    public int StartCellX => _cellOccupiedAtStartX;
    public int StartCellY => _cellOccupiedAtStartY;
    public int LastCellX => _lastCellX;
    public int LastCellY => _lastCellY;
    public bool IsValid => _isValid;
    public Vector3Int AnchorVoxel => _anchorVoxel;
    public CameraView PlacedView => _placedView;

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

    public void IsPlacedAtCell()
    {
        _cellOccupiedAtStartX = _actualCellX;
        _cellOccupiedAtStartY = _actualCellY;
        _isMoved = false;
    }

    public void InstancePlaceableObjectCreated(Vector3Int anchor, CameraView placedView)
    {
        _wasInitialized = true;
        _anchorVoxel = anchor;
        _placedView = placedView;
        _cellOccupiedAtStartX = anchor.x;
        _cellOccupiedAtStartY = anchor.z;
        _actualCellX = anchor.x;
        _actualCellY = anchor.z;
        _lastCellX = anchor.x;
        _lastCellY = anchor.z;
        _isMoved = false;
    }

    public void RestartCell()
    {
        _actualCellX = _cellOccupiedAtStartX;
        _actualCellY = _cellOccupiedAtStartY;
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

    public void LerpTo(Vector3 targetWorldPos, Quaternion targetRot, float duration = 0.25f)
    {
        StartCoroutine(LerpRoutine(targetWorldPos, targetRot, duration));
    }

    private IEnumerator LerpRoutine(Vector3 targetPos, Quaternion targetRot, float duration)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;
    }

}
