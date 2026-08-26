using UnityEngine;

public interface IVoxelProjection
{
    void Init();
    void RefreshAll();
    void SetVisible(bool visible);
    bool TryGetWorldTransform(Vector3Int voxel, out Vector3 pos, out Quaternion rot);
    bool TryGetVoxelUnderRay(Ray ray, out Vector3Int voxel);
    void SetCellVisual(Vector3Int voxel, CellVisualState state);
}