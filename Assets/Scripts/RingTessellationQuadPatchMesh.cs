using UnityEngine;

/// <summary>
/// Custom/RingTessellationQuad 用。3 quad パッチ（各 4 制御点）。uv は各パッチで (0,0),(1,0),(1,1),(0,1) = Domain の (u,v)。
/// uv2.x にセクタ index(0..2)。メッシュは <see cref="MeshTopology.Quads"/> 必須。
/// </summary>
public static class RingTessellationQuadPatchMesh
{
    public const int SectorCount = 3;
    public const int VertexCount = SectorCount * 4;

    public static Mesh Create()
    {
        var verts = new Vector3[VertexCount];
        var uvs = new Vector2[VertexCount];
        var uv2 = new Vector2[VertexCount];
        var indices = new int[VertexCount];

        for (int s = 0; s < SectorCount; s++)
        {
            int b = s * 4;
            uvs[b] = new Vector2(0f, 0f);
            uvs[b + 1] = new Vector2(1f, 0f);
            uvs[b + 2] = new Vector2(1f, 1f);
            uvs[b + 3] = new Vector2(0f, 1f);
            var sec = new Vector2(s, 0f);
            uv2[b] = sec;
            uv2[b + 1] = sec;
            uv2[b + 2] = sec;
            uv2[b + 3] = sec;
            indices[b] = b;
            indices[b + 1] = b + 1;
            indices[b + 2] = b + 2;
            indices[b + 3] = b + 3;
        }

        var m = new Mesh { name = "RingTessellationQuadThreePatches" };
        m.vertices = verts;
        m.uv = uvs;
        m.uv2 = uv2;
        m.SetIndices(indices, MeshTopology.Quads, 0);
        m.UploadMeshData(true);
        return m;
    }
}
