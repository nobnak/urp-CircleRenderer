using System.Runtime.InteropServices;
using UnityEngine;

public enum CircleTessMode
{
    Fixed = 0,
    LogDistance = 1,
}

public enum CircleDebugVis
{
    Off = 0,
    PatchBarycentric = 1,
}

[StructLayout(LayoutKind.Sequential)]
public struct CircleInstanceData
{
    public float radius;
    public float tess;
    public float debugVis;
    public float tessMode;
    public Color color;

    public static int Stride => Marshal.SizeOf<CircleInstanceData>();
}
