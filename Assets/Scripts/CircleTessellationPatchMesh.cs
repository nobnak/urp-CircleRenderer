using UnityEngine;

public static class CircleTessellationPatchMesh
{
    public const int SectorCount = 3;
    public const int VertexCount = SectorCount * 3;

    public static Mesh Create()
    {
        var verts = new Vector3[VertexCount];
        var uvs = new Vector2[VertexCount];
        var indices = new int[VertexCount];
        const float twoPi = Mathf.PI * 2f;

        for (int s = 0; s < SectorCount; s++)
        {
            int v = s * 3;
            float t0 = twoPi * s / SectorCount;
            float t1 = twoPi * (s + 1) / SectorCount;

            verts[v] = Vector3.zero;
            verts[v + 1] = new Vector3(Mathf.Cos(t0), Mathf.Sin(t0), 0f);
            verts[v + 2] = new Vector3(Mathf.Cos(t1), Mathf.Sin(t1), 0f);

            uvs[v] = new Vector2(0f, s);
            uvs[v + 1] = new Vector2(1f, s);
            uvs[v + 2] = new Vector2(2f, s);

            indices[v] = v;
            indices[v + 1] = v + 1;
            indices[v + 2] = v + 2;
        }

        var m = new Mesh { name = "CircleTessellationFanThreeSectors" };
        m.vertices = verts;
        m.uv = uvs;
        m.triangles = indices;
        m.UploadMeshData(true);
        return m;
    }
}
