using UnityEngine;

public enum CellVisualState
{
    Default,
    Empty,
    Blocked
}

public class GridVisualCell : MonoBehaviour
{
    [SerializeField]
    private MeshRenderer _renderer;
    [SerializeField]
    private Material _defaultMaterial;
    [SerializeField]
    private Material _emptyMaterial;
    [SerializeField]
    private Material _blockedMaterial;

    public int X { get; private set; }
    public int Z { get; private set; }

    public void Init(int x, int z)
    {
        X = x;
        Z = z;
        SetState(CellVisualState.Default);
    }

    public void SetState(CellVisualState state)
    {
        if (state == CellVisualState.Empty)
        {
            _renderer.material = _emptyMaterial;
        }
        else if (state == CellVisualState.Blocked)
        {
            _renderer.material = _blockedMaterial;
        }
        else
        {
            _renderer.material = _defaultMaterial;
        }
    }
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}