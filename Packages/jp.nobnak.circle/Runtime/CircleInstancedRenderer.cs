using UnityEngine;

[DefaultExecutionOrder(1000)]
public sealed class CircleInstancedRenderer : InstancedRendererBase<CircleInstanceData>
{
    static readonly int CircleInstancesId = Shader.PropertyToID("_CircleInstances");

    protected override int InstanceBufferPropertyId => CircleInstancesId;
    protected override string FallbackInstancedShaderName => "jp.nobnak.circle/Circle/Instanced";

    protected override void EnsureDefaultMeshIfNull()
    {
        if (_mesh == null)
            _mesh = CirclePatchMesh.Create();
    }

    void LateUpdate() => DrawAccumulatedFrame();
}
