using UnityEngine;

/// <summary>
/// Custom/CircleTessellation 用。1 三角形パッチ: 中心 A、円周 B(θ=0) と C(θ=2π, 同座標で退化可)、弧 BC をテッセレートしてファン状に一周。
/// uv.x=0 中心 / 1,2 円周。uv.y は未使用。
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
        var m = new Mesh { name = "CircleTessellationFanOnePatch" };
        m.vertices = verts;
        m.uv = uvs;
        m.triangles = indices;
        m.UploadMeshData(true);
        return m;
    }
}
