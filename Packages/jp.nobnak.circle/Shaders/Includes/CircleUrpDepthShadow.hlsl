#ifndef CIRCLE_URP_DEPTH_SHADOW_INCLUDED
#define CIRCLE_URP_DEPTH_SHADOW_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#if defined(LOD_FADE_CROSSFADE)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

float3 CircleRing_FlatNormalOS()
{
    return float3(0.0, 0.0, -1.0);
}

float3 CircleRing_FlatNormalWS()
{
    return TransformObjectToWorldNormal(CircleRing_FlatNormalOS());
}

half CircleRing_DepthOnlyZ(float4 positionCS)
{
#if defined(LOD_FADE_CROSSFADE)
    LODFadeCrossFade(positionCS);
#endif
    return positionCS.z;
}

void CircleRing_DepthNormalsOut(float4 positionCS, float3 normalWS, out half4 outNormalWS)
{
#if defined(LOD_FADE_CROSSFADE)
    LODFadeCrossFade(positionCS);
#endif
#if defined(_GBUFFER_NORMALS_OCT)
    float3 nWS = normalize(normalWS);
    float2 octNormalWS = PackNormalOctQuadEncode(nWS);
    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
    half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
    outNormalWS = half4(packedNormalWS, 0.0);
#else
    outNormalWS = half4(NormalizeNormalPerPixel(normalWS), 0.0);
#endif
}

half4 CircleRing_ShadowCasterOut(float4 positionCS)
{
#if defined(LOD_FADE_CROSSFADE)
    LODFadeCrossFade(positionCS);
#endif
    return 0.0;
}

#endif
