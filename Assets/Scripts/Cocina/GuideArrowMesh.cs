using UnityEngine;


public static class GuideArrowMesh
{
    private static Mesh _mesh;

    public static Mesh Get()
    {
        if (_mesh != null) return _mesh;

        _mesh = new Mesh { name = "GuideArrow (compartida)" };

        Vector3[] verts =
        {
            new( 0.00f, 0f,  0.50f), // punta
            new(-0.28f, 0f,  0.08f), // ala izquierda
            new( 0.28f, 0f,  0.08f), // ala derecha
            new(-0.11f, 0f,  0.08f), // hombro izq.
            new( 0.11f, 0f,  0.08f), // hombro der.
            new(-0.11f, 0f, -0.45f), // cola izq.
            new( 0.11f, 0f, -0.45f), // cola der.
        };

        Vector2[] uvs =
        {
            new(0.5f, 1.00f), new(0.0f, 0.56f), new(1.0f, 0.56f),
            new(0.3f, 0.56f), new(0.7f, 0.56f), new(0.3f, 0.00f), new(0.7f, 0.00f),
        };

        int[] tris = { 0, 2, 1,   3, 4, 6,   3, 6, 5 };

        Vector3[] normals = new Vector3[verts.Length];
        for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.up;

        _mesh.vertices  = verts;
        _mesh.uv        = uvs;
        _mesh.triangles = tris;
        _mesh.normals   = normals;

        return _mesh;
    }
}
