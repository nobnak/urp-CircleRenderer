#ifndef PASS_SHADOW
#error Define PASS_SHADOW (0 or 1) before including CircleInstancedTess.hlsl
#endif

#include "Packages/jp.nobnak.circle/Shaders/Includes/CircleShared.hlsl"
#include "Packages/jp.nobnak.circle/Shaders/Includes/CircleUrpDepthShadow.hlsl"
#if PASS_SHADOW
#include "Packages/jp.nobnak.circle/Shaders/Includes/CircleUrpShadowClip.hlsl"
#endif

struct CircleInstanceData
{
    float radius;
    float tess;
    float debugVis;
    float tessMode;
    float4 color;
};

uint _InstanceBufferBase;

StructuredBuffer<CircleInstanceData> _CircleInstances;

CircleInstanceData LoadInstance(uint iid)
{
    return _CircleInstances[_InstanceBufferBase + iid];
}

ControlPoint Vert(Attributes input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    uint iid = UNITY_GET_INSTANCE_ID(input);
    CircleInstanceData inst = LoadInstance(iid);
    ControlPoint o = BuildControlPoint(input.uv, inst.radius);
    UNITY_TRANSFER_INSTANCE_ID(input, o);
    return o;
}

TessellationFactors PatchConstant(InputPatch<ControlPoint, 3> patch)
{
    UNITY_SETUP_INSTANCE_ID(patch[0]);
    uint iid = UNITY_GET_INSTANCE_ID(patch[0]);
    CircleInstanceData inst = LoadInstance(iid);
    float arc = ComputeArcTess(inst.tess, inst.tessMode);
    return BuildPatchFactors(arc);
}

[domain("tri")]
[partitioning("integer")]
[outputtopology("triangle_ccw")]
[patchconstantfunc("PatchConstant")]
[outputcontrolpoints(3)]
[maxtessfactor(64.0)]
ControlPoint Hull(InputPatch<ControlPoint, 3> patch, uint id : SV_OutputControlPointID)
{
    return patch[id];
}

[domain("tri")]
Varyings Domain(TessellationFactors factors, OutputPatch<ControlPoint, 3> patch, float3 bary : SV_DomainLocation)
{
    UNITY_SETUP_INSTANCE_ID(patch[0]);
    uint iid = UNITY_GET_INSTANCE_ID(patch[0]);
    float radius = LoadInstance(iid).radius;
    float3 posOS = EvalDomainPosOS(radius, patch[0].sectorAngles, bary);
    Varyings o;
#if PASS_SHADOW
    o.positionCS = CircleRing_ShadowClipFromOS(posOS);
#else
    o.positionCS = TransformObjectToHClip(posOS);
#endif
    o.patchBary = bary;
    o.normalWS = CircleRing_FlatNormalWS();
    UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
    return o;
}

half4 Frag(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    uint iid = UNITY_GET_INSTANCE_ID(input);
    CircleInstanceData inst = LoadInstance(iid);
    return EvalFragColor(input.patchBary, inst.debugVis, inst.color);
}

half DepthOnlyFrag(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    return CircleRing_DepthOnlyZ(input.positionCS);
}

void DepthNormalsFrag(Varyings input, out half4 outNormalWS : SV_Target0)
{
    UNITY_SETUP_INSTANCE_ID(input);
    CircleRing_DepthNormalsOut(input.positionCS, input.normalWS, outNormalWS);
}

half4 ShadowCasterFrag(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    return CircleRing_ShadowCasterOut(input.positionCS);
}
