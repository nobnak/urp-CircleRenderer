using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// <see cref="CircleTessellationInstanceData"/> を StructuredBuffer に載せ、<see cref="Graphics.DrawMeshInstanced"/> で複数円を描画する。
/// <see cref="CircleTessellationInstance"/> の登録によりシーン上のパラメータ付きインスタンスを描画できる。
/// マテリアルは GPU Instancing を有効にし、シェーダーは Custom/CircleTessellationInstanced を使用する。
/// <see cref="ExecuteAlwaysAttribute"/> により再生していない Edit Mode の Scene ビューでも登録インスタンスを描画する。
/// 描画カメラは常に未指定（<c>null</c>＝既定の全カメラ向け）とする。
/// </summary>
[ExecuteAlways]
public sealed class CircleTessellationInstancedRenderer : MonoBehaviour
{
    const int kMaxInstancesPerDraw = 1023;

    static readonly int CircleInstancesId = Shader.PropertyToID("_CircleInstances");

    [SerializeField] Material _material;
    [SerializeField] Mesh _mesh;
    [SerializeField] ShadowCastingMode _castShadows = ShadowCastingMode.Off;
    [SerializeField] bool _receiveShadows;
    [SerializeField] int _layer;
    [SerializeField] bool _drawRegisteredInLateUpdate = true;

    readonly List<CircleTessellationInstance> _registered = new();

    GraphicsBuffer _instanceBuffer;
    MaterialPropertyBlock _mpb;
    Matrix4x4[] _matrixBatch;
    CircleTessellationInstanceData[] _dataBatch;
    Matrix4x4[] _matrixScratch;
    CircleTessellationInstanceData[] _dataScratch;
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

    public void RegisterInstance(CircleTessellationInstance inst)
    {
        if (inst == null || _registered.Contains(inst))
            return;
        _registered.Add(inst);
    }

    public void UnregisterInstance(CircleTessellationInstance inst)
    {
        if (inst == null)
            return;
        _registered.Remove(inst);
    }

    void LateUpdate()
    {
        if (!_drawRegisteredInLateUpdate)
            return;
        CompactRegistered();
        int count = _registered.Count;
        if (count <= 0 || _material == null || _mesh == null)
            return;
        if (!MaterialInstancingReady())
            return;
        EnsureBuffers();
        for (int offset = 0; offset < count; offset += kMaxInstancesPerDraw)
        {
            int n = Mathf.Min(kMaxInstancesPerDraw, count - offset);
            for (int i = 0; i < n; i++)
            {
                var c = _registered[offset + i];
                _matrixBatch[i] = c.transform.localToWorldMatrix;
                _dataBatch[i] = c.InstanceData;
            }
            _instanceBuffer.SetData(_dataBatch, 0, 0, n);
            _mpb.Clear();
            _mpb.SetBuffer(CircleInstancesId, _instanceBuffer);
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
    }

    void OnEnable()
    {
        _instancingWarnIssued = false;
        if (_mesh == null)
            _mesh = CircleTessellationPatchMesh.Create();
        EnsureBuffers();
    }

    void OnDisable()
    {
        DisposeBuffer();
    }

    void OnDestroy()
    {
        DisposeBuffer();
    }

    void EnsureBuffers()
    {
        int stride = CircleTessellationInstanceData.Stride;
        if (_instanceBuffer == null || _instanceBuffer.count < kMaxInstancesPerDraw || _instanceBuffer.stride != stride)
        {
            DisposeBuffer();
            _instanceBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, kMaxInstancesPerDraw, stride);
        }
        if (_matrixBatch == null || _matrixBatch.Length < kMaxInstancesPerDraw)
            _matrixBatch = new Matrix4x4[kMaxInstancesPerDraw];
        if (_dataBatch == null || _dataBatch.Length < kMaxInstancesPerDraw)
            _dataBatch = new CircleTessellationInstanceData[kMaxInstancesPerDraw];
        _mpb ??= new MaterialPropertyBlock();
    }

    void DisposeBuffer()
    {
        _instanceBuffer?.Dispose();
        _instanceBuffer = null;
    }

    void CompactRegistered()
    {
        for (int i = _registered.Count - 1; i >= 0; i--)
        {
            if (_registered[i] == null)
                _registered.RemoveAt(i);
        }
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
            Debug.LogWarning($"{nameof(CircleTessellationInstancedRenderer)}: material must have GPU Instancing enabled.", this);
        }
        return false;
    }

    /// <summary>
    /// ワールド行列とインスタンスデータ（同じ長さ）で描画する。
    /// </summary>
    public void Draw(Matrix4x4[] matrices, CircleTessellationInstanceData[] instances, int count, int layer = 0)
    {
        if (_material == null || _mesh == null || count <= 0)
            return;
        if (matrices == null || instances == null || matrices.Length < count || instances.Length < count)
            return;
        if (!MaterialInstancingReady())
            return;
        EnsureBuffers();
        for (int offset = 0; offset < count; offset += kMaxInstancesPerDraw)
        {
            int n = Mathf.Min(kMaxInstancesPerDraw, count - offset);
            _instanceBuffer.SetData(instances, offset, 0, n);
            for (int i = 0; i < n; i++)
                _matrixBatch[i] = matrices[offset + i];
            _mpb.Clear();
            _mpb.SetBuffer(CircleInstancesId, _instanceBuffer);
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

    /// <summary>
    /// 単一インスタンス（テスト用）。
    /// </summary>
    public void DrawOne(Matrix4x4 matrix, CircleTessellationInstanceData instance, int layer = 0)
    {
        if (_matrixScratch == null)
            _matrixScratch = new Matrix4x4[1];
        if (_dataScratch == null)
            _dataScratch = new CircleTessellationInstanceData[1];
        _matrixScratch[0] = matrix;
        _dataScratch[0] = instance;
        Draw(_matrixScratch, _dataScratch, 1, layer);
    }
}
