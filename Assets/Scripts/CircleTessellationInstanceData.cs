using System.Runtime.InteropServices;
using UnityEngine;

public enum CircleTessellationTessMode
{
    Fixed = 0,
    LogDistance = 1,
}

public enum CircleTessellationDebugVis
{
    Off = 0,
    PatchBarycentric = 1,
}

[StructLayout(LayoutKind.Sequential)]
public struct CircleTessellationInstanceData
{
    public float radius;
    public float tess;
    public float debugVis;
    public float tessMode;
    public Color color;

    public static int Stride => Marshal.SizeOf<CircleTessellationInstanceData>();
}
