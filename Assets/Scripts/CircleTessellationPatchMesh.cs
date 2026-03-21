using UnityEngine;

/// <summary>
/// Custom/CircleTessellation 用。全周を 2 パッチ（各 π）で表す。uv.x=0 中心 / 1,2 円周、uv.y=パッチ番号 0..1。
/// 弧の細かさはシェーダーのテッセレーションに任せる。アセット: Assets/Create/Meshes/Circle Tessellation Patch
/// </summary>
public static class CircleTessellationPatchMesh
{
    /// <summary>中心＋円周 2 点の三角形は最大 π の扇形のため、全周に必要なパッチ数の最小値。</summary>
    public const int PatchCount = 2;

    public static Mesh Create()
    {
        int vCount = PatchCount * 3;
        var verts = new Vector3[vCount];
        var uvs = new Vector2[vCount];
        var indices = new int[PatchCount * 3];
        for (int p = 0; p < PatchCount; p++)
        {
            int b = p * 3;
            uvs[b] = new Vector2(0f, p);
            uvs[b + 1] = new Vector2(1f, p);
            uvs[b + 2] = new Vector2(2f, p);
            indices[b] = b;
            indices[b + 1] = b + 1;
            indices[b + 2] = b + 2;
        }
        var m = new Mesh { name = "CircleTessellationRadial" };
        m.vertices = verts;
        m.uv = uvs;
        m.triangles = indices;
        m.UploadMeshData(true);
        return m;
    }
}
