using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>シェーダー <c>ComputeArcTessellation</c> の mode と同値（0 / 1）。</summary>
public enum CircleTessellationTessMode
{
    Fixed = 0,
    LogDistance = 1,
}

/// <summary>シェーダー <c>EvalFragColor</c> の debugVis と同値（&gt;0.5 でデバッグ表示）。</summary>
public enum CircleTessellationDebugVis
{
    Off = 0,
    PatchBarycentric = 1,
}

/// <summary>
/// Custom/CircleTessellationInstanced の StructuredBuffer とレイアウト一致。
/// tessMode: <see cref="CircleTessellationTessMode"/> を float として格納。
/// </summary>
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
