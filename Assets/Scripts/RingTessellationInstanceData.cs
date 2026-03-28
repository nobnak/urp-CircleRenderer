using System.Runtime.InteropServices;
using UnityEngine;

[StructLayout(LayoutKind.Sequential)]
public struct RingTessellationInstanceData
{
    public float radius;
    public float ringWidth;
    public float tess;
    public float pad;
    public Color color;

    public static int Stride => Marshal.SizeOf<RingTessellationInstanceData>();
}
