using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public sealed class CircleInstancedGroup : MonoBehaviour
{
    [SerializeField] CircleInstancedRenderer _renderer;

    readonly List<CircleInstance> _instances = new List<CircleInstance>();
    Matrix4x4[] _matrices;
    CircleInstanceData[] _data;
    int _scratchCapacity;

    public void RegisterInstance(CircleInstance inst)
    {
        if (inst == null || _instances.Contains(inst))
            return;
        _instances.Add(inst);
    }

    public void UnregisterInstance(CircleInstance inst)
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
            var c = _instances[i];
            if (c == null)
                continue;
            _matrices[write] = c.transform.localToWorldMatrix;
            _data[write] = c.InstanceData;
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
        if (_matrices != null && _data != null && _matrices.Length >= newCap && _data.Length >= newCap)
        {
            _scratchCapacity = newCap;
            return;
        }
        System.Array.Resize(ref _matrices, newCap);
        System.Array.Resize(ref _data, newCap);
        _scratchCapacity = newCap;
    }

    static int Align16(int n) => (n + 15) & ~15;
}
