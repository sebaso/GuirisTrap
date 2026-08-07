using UnityEngine;

public interface IGridWorldResolver
{
    bool TryGetWorldTransform(CameraView view, Vector3Int voxel, out Vector3 pos, out Quaternion rot);
}