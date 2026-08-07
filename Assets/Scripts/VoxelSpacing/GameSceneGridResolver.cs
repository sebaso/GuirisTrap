using UnityEngine;

public class GameSceneGridResolver : MonoBehaviour, IGridWorldResolver
{
    [Header("Proyecciones (mismas posiciones que en PreparationScene)")]
    [SerializeField] private FloorGridProjection _floor;
    [SerializeField] private WallGridProjection _wallNorth;
    [SerializeField] private WallGridProjection _wallEast;
    [SerializeField] private WallGridProjection _wallWest;

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
}