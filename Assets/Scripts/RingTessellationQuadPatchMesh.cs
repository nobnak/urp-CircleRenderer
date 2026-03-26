using UnityEngine;

/// <summary>
/// Custom/RingTessellationQuad 用。1 quad パッチ（4 制御点）。uv は (0,0),(1,0),(1,1),(0,1) = Domain の (u,v)。
/// メッシュは <see cref="MeshTopology.Quads"/> 必須（三角形 2 枚の Quad プリミティブではパッチ数が合わない）。
/// </summary>
public static class RingTessellationQuadPatchMesh
{
    public const int VertexCount = 4;

    public static Mesh Create()
    {
        var verts = new Vector3[VertexCount];
        var uvs = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
        };
        var indices = new[] { 0, 1, 2, 3 };
        var m = new Mesh { name = "RingTessellationQuadOnePatch" };
        m.vertices = verts;
        m.uv = uvs;
        m.SetIndices(indices, MeshTopology.Quads, 0);
        m.UploadMeshData(true);
        return m;
    }
}
