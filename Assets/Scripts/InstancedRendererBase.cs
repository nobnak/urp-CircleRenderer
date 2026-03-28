using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// StructuredBuffer + <see cref="Graphics.DrawMeshInstanced"/> の共通実装。
/// 派生クラスに <c>[DefaultExecutionOrder(1000)]</c> と <c>void LateUpdate() =&gt; DrawAccumulatedFrame();</c> を置く。
/// </summary>
[ExecuteAlways]
public abstract class InstancedRendererBase : MonoBehaviour
{
    protected const int kMaxInstancesPerDraw = 1023;

    [SerializeField] protected Material _material;
    [SerializeField] protected Mesh _mesh;
    [SerializeField] protected ShadowCastingMode _castShadows = ShadowCastingMode.Off;
    [SerializeField] protected bool _receiveShadows;
    [SerializeField] protected int _layer;

    [System.NonSerialized] protected Matrix4x4[] _accumMatrices;
    [System.NonSerialized] protected int _accumCount;

    protected ComputeBuffer _instanceBuffer;
    protected int _instanceBufferStride;
    protected int _matrixBatchCapacity;
    protected Matrix4x4[] _matrixBatch;
    protected MaterialPropertyBlock _mpb;
    protected bool _instancingWarnIssued;

    public Material Material
    {
        get => _material;
        set => _material = value;
    }

    public Mesh Mesh
    {
        get => _mesh;
        set => _mesh = value;
    }

    public int AccumulatedCount => _accumCount;

    protected abstract int InstanceDataStride { get; }
    protected abstract int InstanceBufferPropertyId { get; }
    protected abstract string InstancingWarningSourceName { get; }
    protected abstract void UploadAccumInstanceData(int sourceOffset, int elementCount);
    protected abstract void EnsureDefaultMeshIfNull();

    public void ClearFrameInstances() => _accumCount = 0;

    protected void EnsureAccumPairCapacity<T>(ref T[] data, int requiredCount) where T : struct
    {
        int curM = _accumMatrices != null ? _accumMatrices.Length : 0;
        int curD = data != null ? data.Length : 0;
        if (curM >= requiredCount && curD >= requiredCount)
            return;
        int newCap = Align16(Mathf.Max(requiredCount, 16));
        if (_accumMatrices == null)
            _accumMatrices = new Matrix4x4[newCap];
        else
            System.Array.Resize(ref _accumMatrices, newCap);
        if (data == null)
            data = new T[newCap];
        else
            System.Array.Resize(ref data, newCap);
    }

    protected static int Align16(int n) => (n + 15) & ~15;

    protected static int AlignedDrawBatchCapacity(int minElements)
    {
        minElements = Mathf.Clamp(minElements, 1, kMaxInstancesPerDraw);
        int a = Align16(minElements);
        return Mathf.Min(a, kMaxInstancesPerDraw);
    }

    protected void DrawAccumulatedFrame()
    {
        int count = _accumCount;
        if (count <= 0 || _material == null || _mesh == null)
        {
            _accumCount = 0;
            return;
        }
        if (!MaterialInstancingReady())
        {
            _accumCount = 0;
            return;
        }
        for (int offset = 0; offset < count; offset += kMaxInstancesPerDraw)
        {
            int n = Mathf.Min(kMaxInstancesPerDraw, count - offset);
            EnsureGpuBuffer(n);
            EnsureMatrixBatch(n);
            for (int i = 0; i < n; i++)
                _matrixBatch[i] = _accumMatrices[offset + i];
            UploadAccumInstanceData(offset, n);
            _mpb.Clear();
            _mpb.SetBuffer(InstanceBufferPropertyId, _instanceBuffer);
            Graphics.DrawMeshInstanced(
                _mesh,
                0,
                _material,
                _matrixBatch,
                n,
                _mpb,
                _castShadows,
                _receiveShadows,
                _layer,
                null,
                LightProbeUsage.Off);
        }
        _accumCount = 0;
    }

    protected virtual void OnEnable()
    {
        _instancingWarnIssued = false;
        _accumCount = 0;
        _mpb ??= new MaterialPropertyBlock();
        EnsureDefaultMeshIfNull();
    }

    protected virtual void OnDisable()
    {
        _accumCount = 0;
        DisposeGpuBuffer();
    }

    protected virtual void OnDestroy()
    {
        _accumCount = 0;
        DisposeGpuBuffer();
    }

    protected void EnsureGpuBuffer(int elementCount)
    {
        int stride = InstanceDataStride;
        int cap = AlignedDrawBatchCapacity(elementCount);
        if (_instanceBuffer != null && _instanceBuffer.count == cap && _instanceBufferStride == stride)
            return;
        _instanceBuffer?.Release();
        _instanceBuffer = new ComputeBuffer(cap, stride, ComputeBufferType.Structured);
        _instanceBufferStride = stride;
    }

    protected void EnsureMatrixBatch(int forCount)
    {
        int cap = AlignedDrawBatchCapacity(forCount);
        if (_matrixBatch != null && _matrixBatchCapacity >= cap)
            return;
        _matrixBatch = new Matrix4x4[cap];
        _matrixBatchCapacity = cap;
    }

    protected void DisposeGpuBuffer()
    {
        _instanceBuffer?.Release();
        _instanceBuffer = null;
        _instanceBufferStride = 0;
    }

    protected bool MaterialInstancingReady()
    {
        if (_material != null && _material.enableInstancing)
        {
            _instancingWarnIssued = false;
            return true;
        }
        if (!_instancingWarnIssued)
        {
            _instancingWarnIssued = true;
            Debug.LogWarning($"{InstancingWarningSourceName}: material must have GPU Instancing enabled.", this);
        }
        return false;
    }

    protected void DrawInstancedBatches<T>(Matrix4x4[] matrices, T[] instances, int count, int layer, int bufferPropertyId, int stride) where T : struct
    {
        if (_material == null || _mesh == null || count <= 0)
            return;
        if (matrices == null || instances == null || matrices.Length < count || instances.Length < count)
            return;
        if (!MaterialInstancingReady())
            return;
        for (int offset = 0; offset < count; offset += kMaxInstancesPerDraw)
        {
            int n = Mathf.Min(kMaxInstancesPerDraw, count - offset);
            int cap = AlignedDrawBatchCapacity(n);
            if (_instanceBuffer == null || _instanceBuffer.count != cap || _instanceBufferStride != stride)
            {
                _instanceBuffer?.Release();
                _instanceBuffer = new ComputeBuffer(cap, stride, ComputeBufferType.Structured);
                _instanceBufferStride = stride;
            }
            EnsureMatrixBatch(n);
            _instanceBuffer.SetData(instances, offset, 0, n);
            for (int i = 0; i < n; i++)
                _matrixBatch[i] = matrices[offset + i];
            _mpb.Clear();
            _mpb.SetBuffer(bufferPropertyId, _instanceBuffer);
            Graphics.DrawMeshInstanced(
                _mesh,
                0,
                _material,
                _matrixBatch,
                n,
                _mpb,
                _castShadows,
                _receiveShadows,
                layer,
                null,
                LightProbeUsage.Off);
        }
    }
}
