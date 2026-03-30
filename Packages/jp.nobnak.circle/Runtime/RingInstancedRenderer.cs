using UnityEngine;

[DefaultExecutionOrder(1000)]
public sealed class RingInstancedRenderer : InstancedRendererBase<RingInstanceData>
{
    static readonly int RingInstancesId = Shader.PropertyToID("_RingInstances");

    protected override int InstanceBufferPropertyId => RingInstancesId;
    protected override string FallbackInstancedShaderName => "jp.nobnak.circle/Ring/Instanced";

    protected override void EnsureDefaultMeshIfNull()
    {
        if (_mesh == null)
            _mesh = RingPatchMesh.Create();
    }

    void LateUpdate() => DrawAccumulatedFrame();
}
