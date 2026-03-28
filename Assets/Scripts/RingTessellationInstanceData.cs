using System.Runtime.InteropServices;
using UnityEngine;

public enum RingTessellationTessMode
{
    Fixed = 0,
    LogDistance = 1,
}

public enum RingTessellationDebugVis
{
    Off = 0,
    PatchBarycentric = 1,
}

[StructLayout(LayoutKind.Sequential)]
public struct RingTessellationInstanceData
{
    public float radius;
    public float ringWidth;
    public float tess;
    public float tessMode;
    public float debugVis;
    public float pad;
    public Color color;

    public static int Stride => Marshal.SizeOf<RingTessellationInstanceData>();
}
