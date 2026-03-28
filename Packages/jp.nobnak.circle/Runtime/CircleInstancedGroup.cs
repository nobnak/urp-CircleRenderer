using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public sealed class CircleInstancedGroup : MonoBehaviour
{
    [SerializeField] CircleInstancedRenderer _renderer;

    readonly List<CircleInstance> _instances = new List<CircleInstance>();
    [System.NonSerialized] Matrix4x4[] _matrices;
    [System.NonSerialized] CircleInstanceData[] _data;

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
        InstancedGroupScratch.EnsurePair(ref _matrices, ref _data, n);
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
}
