#ifndef PASS_SHADOW
#error Define PASS_SHADOW (0 or 1) before including CircleOpaqueTess.hlsl
#endif

#include "Packages/jp.nobnak.circle/Shaders/Includes/CircleShared.hlsl"
#include "Packages/jp.nobnak.circle/Shaders/Includes/CircleUrpDepthShadow.hlsl"
#if PASS_SHADOW
#include "Packages/jp.nobnak.circle/Shaders/Includes/CircleUrpShadowClip.hlsl"
#endif

ControlPoint Vert(Attributes input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    float radius = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Radius);
    ControlPoint o = BuildControlPoint(input.uv, radius);
    UNITY_TRANSFER_INSTANCE_ID(input, o);
    return o;
}

TessellationFactors PatchConstant(InputPatch<ControlPoint, 3> patch)
{
    UNITY_SETUP_INSTANCE_ID(patch[0]);
    float tess = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Tess);
    float mode = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _TessMode);
    float arc = ComputeArcTess(tess, mode);
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
    float radius = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Radius);
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
    float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Color);
    float debugVis = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DebugVis);
    return EvalFragColor(input.patchBary, debugVis, color);
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
