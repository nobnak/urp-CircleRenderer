using UnityEngine;

/// <summary>
/// <see cref="CircleInstancedGroup"/> / <see cref="RingInstancedGroup"/> 用の行列・インスタンスデータスクラッチ配列確保。
/// </summary>
public static class InstancedGroupScratch
{
    public static void EnsurePair<T>(ref Matrix4x4[] matrices, ref T[] data, int need) where T : struct
    {
        int newCap = Align16(Mathf.Max(need, 16));
        int curM = matrices != null ? matrices.Length : 0;
        int curD = data != null ? data.Length : 0;
        if (curM >= newCap && curD >= newCap)
            return;
        if (matrices == null)
            matrices = new Matrix4x4[newCap];
        else
            System.Array.Resize(ref matrices, newCap);
        if (data == null)
            data = new T[newCap];
        else
            System.Array.Resize(ref data, newCap);
    }

    static int Align16(int n) => (n + 15) & ~15;
}
