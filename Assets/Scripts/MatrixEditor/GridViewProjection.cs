using UnityEngine;

public enum GridView
{
    Floor,
    WallNorth,
    WallEast,
    WallWest
}

public static class GridViewProjection
{
    public static Vector3Int ToVoxel(GridView view, int u, int v, VoxelGridData data)
    {
        return view switch
        {
            GridView.Floor     => new Vector3Int(u, 0, v),
            GridView.WallNorth => new Vector3Int(u, v, data.depth - 1),
            GridView.WallEast  => new Vector3Int(data.width - 1, v, u),
            GridView.WallWest  => new Vector3Int(0, v, u),
            _ => Vector3Int.zero
        };
    }

    public static Vector2Int ViewSize(GridView view, VoxelGridData data)
    {
        return view switch
        {
            GridView.Floor     => new Vector2Int(data.width, data.depth),
            GridView.WallNorth => new Vector2Int(data.width, data.height),
            GridView.WallEast  => new Vector2Int(data.depth, data.height),
            GridView.WallWest  => new Vector2Int(data.depth, data.height),
            _ => Vector2Int.zero
        };
    }
}