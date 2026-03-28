using UnityEngine;

/// <summary>
/// <c>Custom/RingInstanced</c> 用。インスタンスデータは <see cref="RingInstanceData"/>。
/// </summary>
[DefaultExecutionOrder(1000)]
public sealed class RingInstancedRenderer : InstancedRendererBase
{
    static readonly int RingInstancesId = Shader.PropertyToID("_RingInstances");

    [System.NonSerialized] RingInstanceData[] _accumData;
    [System.NonSerialized] Matrix4x4[] _matrixScratch;
    [System.NonSerialized] RingInstanceData[] _dataScratch;

    protected override int InstanceDataStride => RingInstanceData.Stride;
    protected override int InstanceBufferPropertyId => RingInstancesId;
    protected override string InstancingWarningSourceName => nameof(RingInstancedRenderer);

    protected override void UploadAccumInstanceData(int sourceOffset, int elementCount) =>
        _instanceBuffer.SetData(_accumData, sourceOffset, 0, elementCount);

    protected override void EnsureDefaultMeshIfNull()
    {
        if (_mesh == null)
            _mesh = RingPatchMesh.Create();
    }

    void LateUpdate() => DrawAccumulatedFrame();

    public void AddInstance(Matrix4x4 matrix, RingInstanceData data)
    {
        EnsureAccumPairCapacity(ref _accumData, _accumCount + 1);
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
        EnsureAccumPairCapacity(ref _accumData, _accumCount + count);
        for (int i = 0; i < count; i++)
        {
            int s = sourceStart + i;
            _accumMatrices[_accumCount + i] = matrices[s];
            _accumData[_accumCount + i] = data[s];
        }
        _accumCount += count;
    }

    public void Draw(Matrix4x4[] matrices, RingInstanceData[] instances, int count, int layer = 0) =>
        DrawInstancedBatches(matrices, instances, count, layer, RingInstancesId, RingInstanceData.Stride);

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
