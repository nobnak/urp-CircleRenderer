using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public abstract class InstancedRendererBase<TData> : MonoBehaviour where TData : struct
{
    protected const int kMaxInstancesPerDraw = 1023;

    [SerializeField] protected Mesh _mesh;
    [SerializeField] protected ShadowCastingMode _castShadows = ShadowCastingMode.Off;
    [SerializeField] protected bool _receiveShadows;
    [SerializeField] protected int _layer;
    [SerializeField] protected Shader _shader;

    [System.NonSerialized] protected Matrix4x4[] _accumMatrices;
    [System.NonSerialized] protected int _accumCount;
    [System.NonSerialized] TData[] _accumData;
    [System.NonSerialized] Matrix4x4[] _matrixScratch;
    [System.NonSerialized] TData[] _dataScratch;

    protected ComputeBuffer _instanceBuffer;
    protected int _instanceBufferStride;
    protected int _matrixBatchCapacity;
    protected Matrix4x4[] _matrixBatch;
    protected MaterialPropertyBlock _mpb;
    protected bool _instancingWarnIssued;

    protected Material _material;
    Material _ownedMaterial;

    public Material Material => _material;

    public Mesh Mesh
    {
        get => _mesh;
        set => _mesh = value;
    }

    public int AccumulatedCount => _accumCount;

    protected int InstanceDataStride => Marshal.SizeOf<TData>();

    protected abstract int InstanceBufferPropertyId { get; }
    protected abstract string FallbackInstancedShaderName { get; }
    protected abstract void EnsureDefaultMeshIfNull();

    public void ClearFrameInstances() => _accumCount = 0;

    protected void RebuildDrawMaterial(Shader shader, string fallbackShaderName)
    {
        Shader s = shader != null ? shader : Shader.Find(fallbackShaderName);
        if (s == null)
        {
            DisposeOwnedDrawMaterial();
            return;
        }
        if (_material != null && _ownedMaterial == _material && _material.shader == s)
            return;
        DisposeOwnedDrawMaterial();
        _material = new Material(s) { hideFlags = HideFlags.HideAndDontSave };
        _material.enableInstancing = true;
        _ownedMaterial = _material;
    }

    protected void RebuildDrawMaterialFromFields() =>
        RebuildDrawMaterial(_shader, FallbackInstancedShaderName);

    protected void DisposeOwnedDrawMaterial()
    {
        if (_ownedMaterial == null)
            return;
        var m = _ownedMaterial;
        _ownedMaterial = null;
        if (_material == m)
            _material = null;
        if (Application.isPlaying)
            Destroy(m);
        else
            DestroyImmediate(m);
    }

    protected void EnsureAccumPairCapacity(int requiredCount)
    {
        int curM = _accumMatrices != null ? _accumMatrices.Length : 0;
        int curD = _accumData != null ? _accumData.Length : 0;
        if (curM >= requiredCount && curD >= requiredCount)
            return;
        int newCap = Align16(Mathf.Max(requiredCount, 16));
        if (_accumMatrices == null)
            _accumMatrices = new Matrix4x4[newCap];
        else
            System.Array.Resize(ref _accumMatrices, newCap);
        if (_accumData == null)
            _accumData = new TData[newCap];
        else
            System.Array.Resize(ref _accumData, newCap);
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
            _instanceBuffer.SetData(_accumData, offset, 0, n);
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
        RebuildDrawMaterialFromFields();
        _instancingWarnIssued = false;
        _accumCount = 0;
        _mpb ??= new MaterialPropertyBlock();
        EnsureDefaultMeshIfNull();
    }

#if UNITY_EDITOR
    protected void OnValidate() => RebuildDrawMaterialFromFields();
#endif

    protected virtual void OnDisable()
    {
        _accumCount = 0;
        DisposeGpuBuffer();
    }

    protected virtual void OnDestroy()
    {
        _accumCount = 0;
        DisposeGpuBuffer();
        DisposeOwnedDrawMaterial();
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
            Debug.LogWarning($"{GetType().Name}: material must have GPU Instancing enabled.", this);
        }
        return false;
    }

    protected void DrawInstancedBatches(Matrix4x4[] matrices, TData[] instances, int count, int layer)
    {
        if (_material == null || _mesh == null || count <= 0)
            return;
        if (matrices == null || instances == null || matrices.Length < count || instances.Length < count)
            return;
        if (!MaterialInstancingReady())
            return;
        int stride = InstanceDataStride;
        int bufferPropertyId = InstanceBufferPropertyId;
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

    #region public interface

    public void AddInstance(Matrix4x4 matrix, TData data)
    {
        EnsureAccumPairCapacity(_accumCount + 1);
        _accumMatrices[_accumCount] = matrix;
        _accumData[_accumCount] = data;
        _accumCount++;
    }

    public void AddInstances(Matrix4x4[] matrices, TData[] data, int count, int sourceStart = 0)
    {
        if (count <= 0)
            return;
        if (matrices == null || data == null)
            return;
        if (sourceStart < 0 || matrices.Length < sourceStart + count || data.Length < sourceStart + count)
            return;
        EnsureAccumPairCapacity(_accumCount + count);
        for (int i = 0; i < count; i++)
        {
            int s = sourceStart + i;
            _accumMatrices[_accumCount + i] = matrices[s];
            _accumData[_accumCount + i] = data[s];
        }
        _accumCount += count;
    }

    public void Draw(Matrix4x4[] matrices, TData[] instances, int count, int layer = 0) =>
        DrawInstancedBatches(matrices, instances, count, layer);

    public void DrawOne(Matrix4x4 matrix, TData data, int layer = 0)
    {
        if (_matrixScratch == null)
            _matrixScratch = new Matrix4x4[1];
        if (_dataScratch == null)
            _dataScratch = new TData[1];
        _matrixScratch[0] = matrix;
        _dataScratch[0] = data;
        Draw(_matrixScratch, _dataScratch, 1, layer);
    }

    #endregion
}
