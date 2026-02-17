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

    public int X {get; private set;}
    public int Y {get; private set;}

    public void Init(int x, int y)
    {
        X = x;
        Y = y;
        SetState(CellVisualState.Default);
    }

    public void SetState(CellVisualState state)
    {
        if(state == CellVisualState.Empty)
        {
            _renderer.material = _emptyMaterial;
        }
        else if( state == CellVisualState.Blocked)
        {
            _renderer.material = _blockedMaterial;
        }
        else
        {
            _renderer.material = _defaultMaterial;
        }
    }
}


