using UnityEngine;

[System.Serializable]
public class GridZoneProjectionEntry
{
    public CameraView view;
    [SerializeField]
    private MonoBehaviour _projectionBehaviour;
    public IVoxelProjection Projection => _projectionBehaviour as IVoxelProjection;
}