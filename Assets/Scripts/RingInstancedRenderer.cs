using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[DefaultExecutionOrder(1000)]
public sealed class RingInstancedRenderer : MonoBehaviour
{
    const int kMaxInstancesPerDraw = 1023;

    static readonly int RingInstancesId = Shader.PropertyToID("_RingInstances");

    [SerializeField] Material _material;
    [SerializeField] Mesh _mesh;
    [SerializeField] ShadowCastingMode _castShadows = ShadowCastingMode.Off;
    [SerializeField] bool _receiveShadows;
    [SerializeField] int _layer;

    [System.NonSerialized] Matrix4x4[] _accumMatrices;
    [System.NonSerialized] RingInstanceData[] _accumData;
    [System.NonSerialized] int _accumCount;

    ComputeBuffer _instanceBuffer;
    int _instanceBufferStride;
    int _matrixBatchCapacity;
    Matrix4x4[] _matrixBatch;
    MaterialPropertyBlock _mpb;
    Matrix4x4[] _matrixScratch;
    RingInstanceData[] _dataScratch;
    bool _instancingWarnIssued;

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

    public void ClearFrameInstances() => _accumCount = 0;

    public void AddInstance(Matrix4x4 matrix, RingInstanceData data)
    {
        EnsureAccumCapacity(_accumCount + 1);
        _accumMatrices[_accumCount] = matrix;
        _accumData[_accumCount] = data;
        _accumCount++;
    }

    public void AddInstances(Matrix4x4[] matrices, RingInstanceData[] data, int count, int sourceStart = 0)
    {
        if (count <= 0)
            return;
        if (matrices == null || data == null)
            return;
        if (sourceStart < 0 || matrices.Length < sourceStart + count || data.Length < sourceStart + count)
            return;
        EnsureAccumCapacity(_accumCount + count);
        for (int i = 0; i < count; i++)
        {
            int s = sourceStart + i;
            _accumMatrices[_accumCount + i] = matrices[s];
            _accumData[_accumCount + i] = data[s];
        }
        _accumCount += count;
    }

    void LateUpdate()
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
            _instanceBuffer.SetData(_accumData, offset, 0, n);
            for (int i = 0; i < n; i++)
                _matrixBatch[i] = _accumMatrices[offset + i];
            _mpb.Clear();
            _mpb.SetBuffer(RingInstancesId, _instanceBuffer);
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

    void OnEnable()
    {
        _instancingWarnIssued = false;
        _accumCount = 0;
        if (_mesh == null)
            _mesh = RingPatchMesh.Create();
        _mpb ??= new MaterialPropertyBlock();
    }

    void OnDisable()
    {
        _accumCount = 0;
        DisposeBuffer();
    }

    void OnDestroy()
    {
        _accumCount = 0;
        DisposeBuffer();
    }

    static int Align16(int n) => (n + 15) & ~15;

    static int AlignedDrawBatchCapacity(int minElements)
    {
        minElements = Mathf.Clamp(minElements, 1, kMaxInstancesPerDraw);
        int a = Align16(minElements);
        return Mathf.Min(a, kMaxInstancesPerDraw);
    }

    void EnsureAccumCapacity(int requiredCount)
    {
        int newCap = Align16(Mathf.Max(requiredCount, 16));
        int curM = _accumMatrices != null ? _accumMatrices.Length : 0;
        int curD = _accumData != null ? _accumData.Length : 0;
        if (curM >= newCap && curD >= newCap)
            return;
        if (_accumMatrices == null)
            _accumMatrices = new Matrix4x4[newCap];
        else
            System.Array.Resize(ref _accumMatrices, newCap);
        if (_accumData == null)
            _accumData = new RingInstanceData[newCap];
        else
            System.Array.Resize(ref _accumData, newCap);
    }

    void EnsureGpuBuffer(int elementCount)
    {
        int stride = RingInstanceData.Stride;
        int cap = AlignedDrawBatchCapacity(elementCount);
        if (_instanceBuffer != null && _instanceBuffer.count == cap && _instanceBufferStride == stride)
            return;
        _instanceBuffer?.Release();
        _instanceBuffer = new ComputeBuffer(cap, stride, ComputeBufferType.Structured);
        _instanceBufferStride = stride;
    }

    void EnsureMatrixBatch(int forCount)
    {
        int cap = AlignedDrawBatchCapacity(forCount);
        if (_matrixBatch != null && _matrixBatchCapacity >= cap)
            return;
        _matrixBatch = new Matrix4x4[cap];
        _matrixBatchCapacity = cap;
    }

    void DisposeBuffer()
    {
        _instanceBuffer?.Release();
        _instanceBuffer = null;
        _instanceBufferStride = 0;
    }

    bool MaterialInstancingReady()
    {
        if (_material != null && _material.enableInstancing)
        {
            _instancingWarnIssued = false;
            return true;
        }
        if (!_instancingWarnIssued)
        {
            _instancingWarnIssued = true;
            Debug.LogWarning($"{nameof(RingInstancedRenderer)}: material must have GPU Instancing enabled.", this);
        }
        return false;
    }

    public void Draw(Matrix4x4[] matrices, RingInstanceData[] instances, int count, int layer = 0)
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
            EnsureGpuBuffer(n);
            EnsureMatrixBatch(n);
            _instanceBuffer.SetData(instances, offset, 0, n);
            for (int i = 0; i < n; i++)
                _matrixBatch[i] = matrices[offset + i];
            _mpb.Clear();
            _mpb.SetBuffer(RingInstancesId, _instanceBuffer);
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

    public void DrawOne(Matrix4x4 matrix, RingInstanceData instance, int layer = 0)
    {
        if (_matrixScratch == null)
            _matrixScratch = new Matrix4x4[1];
        if (_dataScratch == null)
            _dataScratch = new RingInstanceData[1];
        _matrixScratch[0] = matrix;
        _dataScratch[0] = instance;
        Draw(_matrixScratch, _dataScratch, 1, layer);
    }
}
