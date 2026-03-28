using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public sealed class RingInstancedGroup : MonoBehaviour
{
    [SerializeField] RingInstancedRenderer _renderer;

    readonly List<RingInstance> _instances = new List<RingInstance>();
    [System.NonSerialized] Matrix4x4[] _matrices;
    [System.NonSerialized] RingInstanceData[] _data;

    public void RegisterInstance(RingInstance inst)
    {
        if (inst == null || _instances.Contains(inst))
            return;
        _instances.Add(inst);
    }

    public void UnregisterInstance(RingInstance inst)
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
}
