using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public sealed class RingTessellationInstancedGroup : MonoBehaviour
{
    [SerializeField] RingTessellationInstancedRenderer _renderer;

    readonly List<RingTessellationInstance> _instances = new List<RingTessellationInstance>();
    [System.NonSerialized] Matrix4x4[] _matrices;
    [System.NonSerialized] RingTessellationInstanceData[] _data;

    public void RegisterInstance(RingTessellationInstance inst)
    {
        if (inst == null || _instances.Contains(inst))
            return;
        _instances.Add(inst);
    }

    public void UnregisterInstance(RingTessellationInstance inst)
    {
        if (inst == null)
            return;
        _instances.Remove(inst);
    }

    void Update()
    {
        if (_renderer == null)
            return;
        CompactInstances();
        int n = _instances.Count;
        if (n == 0)
            return;
        EnsureScratchCapacity(n);
        int write = 0;
        for (int i = 0; i < n; i++)
        {
            var r = _instances[i];
            if (r == null)
                continue;
            _matrices[write] = r.transform.localToWorldMatrix;
            _data[write] = r.InstanceData;
            write++;
        }
        if (write == 0)
            return;
        _renderer.AddInstances(_matrices, _data, write, 0);
    }

    void CompactInstances()
    {
        for (int i = _instances.Count - 1; i >= 0; i--)
        {
            if (_instances[i] == null)
                _instances.RemoveAt(i);
        }
    }

    void EnsureScratchCapacity(int need)
    {
        int newCap = Align16(Mathf.Max(need, 16));
        int curM = _matrices != null ? _matrices.Length : 0;
        int curD = _data != null ? _data.Length : 0;
        if (curM >= newCap && curD >= newCap)
            return;
        if (_matrices == null)
            _matrices = new Matrix4x4[newCap];
        else
            System.Array.Resize(ref _matrices, newCap);
        if (_data == null)
            _data = new RingTessellationInstanceData[newCap];
        else
            System.Array.Resize(ref _data, newCap);
    }

    static int Align16(int n) => (n + 15) & ~15;
}
