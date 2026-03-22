using UnityEngine;

/// <summary>
/// Custom/CircleTessellation 用。円周上の正三角形 1 パッチ（θ=0, 2π/3, 4π/3）。uv.x=頂点 0..2。
/// アセット: Assets/Create/Meshes/Circle Tessellation Patch
/// </summary>
public static class CircleTessellationPatchMesh
{
    public const int VertexCount = 3;

    public static Mesh Create()
    {
        var verts = new Vector3[VertexCount];
        var uvs = new Vector2[VertexCount];
        for (int i = 0; i < VertexCount; i++)
            uvs[i] = new Vector2(i, 0f);
        var indices = new[] { 0, 1, 2 };
        var m = new Mesh { name = "CircleTessellationEquilateral" };
        m.vertices = verts;
        m.uv = uvs;
        m.triangles = indices;
        m.UploadMeshData(true);
        return m;
    }
}
