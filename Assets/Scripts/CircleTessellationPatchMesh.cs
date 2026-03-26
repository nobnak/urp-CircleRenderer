using UnityEngine;

/// <summary>
/// Custom/CircleTessellation 用。1 三角形パッチを単位円に内接する正三角形で生成する。
/// uv.x=0,1,2 がコーナー ID。uv.y は未使用。
/// </summary>
public static class CircleTessellationPatchMesh
{
    public const int VertexCount = 3;

    public static Mesh Create()
    {
        var verts = new[]
        {
            new Vector3(0f, 1f, 0f),
            new Vector3(-0.8660254f, -0.5f, 0f),
            new Vector3(0.8660254f, -0.5f, 0f)
        };
        var uvs = new Vector2[VertexCount];
        for (int i = 0; i < VertexCount; i++)
            uvs[i] = new Vector2(i, 0f);
        var indices = new[] { 0, 1, 2 };
        var m = new Mesh { name = "CircleTessellationFanOnePatch" };
        m.vertices = verts;
        m.uv = uvs;
        m.triangles = indices;
        m.UploadMeshData(true);
        return m;
    }
}
