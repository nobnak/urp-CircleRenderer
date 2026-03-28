using UnityEngine;

/// <summary>
/// <c>Custom/CircleInstanced</c> 用。インスタンスデータは <see cref="CircleInstanceData"/>。
/// </summary>
[DefaultExecutionOrder(1000)]
public sealed class CircleInstancedRenderer : InstancedRendererBase
{
    static readonly int CircleInstancesId = Shader.PropertyToID("_CircleInstances");

    [System.NonSerialized] CircleInstanceData[] _accumData;
    [System.NonSerialized] Matrix4x4[] _matrixScratch;
    [System.NonSerialized] CircleInstanceData[] _dataScratch;

    protected override int InstanceDataStride => CircleInstanceData.Stride;
    protected override int InstanceBufferPropertyId => CircleInstancesId;
    protected override string InstancingWarningSourceName => nameof(CircleInstancedRenderer);

    protected override void UploadAccumInstanceData(int sourceOffset, int elementCount) =>
        _instanceBuffer.SetData(_accumData, sourceOffset, 0, elementCount);

    protected override void EnsureDefaultMeshIfNull()
    {
        if (_mesh == null)
            _mesh = CirclePatchMesh.Create();
    }

    void LateUpdate() => DrawAccumulatedFrame();

    public void AddInstance(Matrix4x4 matrix, CircleInstanceData data)
    {
        EnsureAccumPairCapacity(ref _accumData, _accumCount + 1);
        _accumMatrices[_accumCount] = matrix;
        _accumData[_accumCount] = data;
        _accumCount++;
    }

    public void AddInstances(Matrix4x4[] matrices, CircleInstanceData[] data, int count, int sourceStart = 0)
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

    public void Draw(Matrix4x4[] matrices, CircleInstanceData[] instances, int count, int layer = 0) =>
        DrawInstancedBatches(matrices, instances, count, layer, CircleInstancesId, CircleInstanceData.Stride);

    public void DrawOne(Matrix4x4 matrix, CircleInstanceData instance, int layer = 0)
    {
        if (_matrixScratch == null)
            _matrixScratch = new Matrix4x4[1];
        if (_dataScratch == null)
            _dataScratch = new CircleInstanceData[1];
        _matrixScratch[0] = matrix;
        _dataScratch[0] = instance;
        Draw(_matrixScratch, _dataScratch, 1, layer);
    }
}
