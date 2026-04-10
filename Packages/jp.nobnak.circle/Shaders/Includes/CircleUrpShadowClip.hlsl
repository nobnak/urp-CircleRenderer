#ifndef CIRCLE_URP_SHADOW_CLIP_INCLUDED
#define CIRCLE_URP_SHADOW_CLIP_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

// Set per draw by ShadowUtils.SetupShadowCasterConstantBuffer (see URP ShadowCasterPass.hlsl).
float3 _LightDirection;
float3 _LightPosition;

float4 CircleRing_ShadowClipFromOS(float3 posOS)
{
    float3 positionWS = TransformObjectToWorld(posOS);
    float3 normalWS = TransformObjectToWorldNormal(float3(0.0, 0.0, -1.0));
#if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
    return ApplyShadowClamping(positionCS);
}

#endif
