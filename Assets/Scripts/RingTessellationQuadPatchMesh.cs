using UnityEngine;

public static class RingTessellationQuadPatchMesh
{
    public const int SectorCount = 3;
    public const int VertexCount = SectorCount * 4;
    const float kTwoPi = Mathf.PI * 2f;
    const float kInputROut = 2f;

    public static Mesh Create()
    {
        var verts = new Vector3[VertexCount];
        var uvs = new Vector2[VertexCount];
        var uv2 = new Vector2[VertexCount];
        var indices = new int[VertexCount];

        for (int s = 0; s < SectorCount; s++)
        {
            int b = s * 4;
            float t0 = kTwoPi * s / SectorCount;
            float t1 = kTwoPi * (s + 1) / SectorCount;
            float c0 = Mathf.Cos(t0), si0 = Mathf.Sin(t0);
            float c1 = Mathf.Cos(t1), si1 = Mathf.Sin(t1);
            verts[b] = new Vector3(c0, -si0, 0f);
            verts[b + 1] = new Vector3(c1, -si1, 0f);
            verts[b + 2] = new Vector3(kInputROut * c1, -kInputROut * si1, 0f);
            verts[b + 3] = new Vector3(kInputROut * c0, -kInputROut * si0, 0f);
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
