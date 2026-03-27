#ifndef CIRCLE_TESSELLATION_SHARED_INCLUDED
#define CIRCLE_TESSELLATION_SHARED_INCLUDED

// 3 sectors: each patch uses A=center, B/C=adjacent points on the circumference.
static const float kTwoPi = 6.28318530718;
static const float kSectorSpan = kTwoPi / 3.0;

struct CircleParams
{
    float radius;
    float tess;
    float tessMode; // 0 = Fixed, 1 = Log Distance
    float debugVis;
    float4 color;
};

struct Attributes
{
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct ControlPoint
{
    float4 positionOS : INTERNALTESSPOS;
    float2 sectorAngles : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 patchBary : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_INSTANCE_ID
};

struct TessellationFactors
{
    float edge[3] : SV_TessFactor;
    float inside : SV_InsideTessFactor;
};

float ComputeArcTessellation(float baseTess, float mode)
{
    baseTess = clamp(baseTess, 1.0, 64.0);
    if (mode < 0.5)
        return baseTess;
    float3 centerWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
    float dist = distance(GetCameraPositionWS(), centerWS);
    float denom = max(log(1.0 + dist), 1e-5);
    return clamp(3.0 * baseTess / denom, 3.0, 64.0);
}

ControlPoint BuildControlPoint(float2 uv, float radius)
{
    uint corner = (uint)uv.x;
    float sector = round(uv.y);
    float theta0 = sector * kSectorSpan;
    float theta1 = theta0 + kSectorSpan;
    float3 p;
    if (corner == 0u)
        p = float3(0.0, 0.0, 0.0);
    else if (corner == 1u)
        p = radius * float3(cos(theta0), sin(theta0), 0.0);
    else
        p = radius * float3(cos(theta1), sin(theta1), 0.0);
    ControlPoint o;
    o.positionOS = float4(p, 1.0);
    o.sectorAngles = float2(theta0, theta1);
    return o;
}

TessellationFactors BuildPatchFactors(float arc)
{
    TessellationFactors f;
    f.edge[0] = arc; // Arc BC
    f.edge[1] = 1.0;
    f.edge[2] = 1.0;
    f.inside = 1.0;
    return f;
}

float3 EvalDomainPosOS(float radius, float2 sectorAngles, float3 bary)
{
    float ring = saturate(bary.y + bary.z);
    float thetaT = bary.z / max(ring, 1e-6);
    float theta = lerp(sectorAngles.x, sectorAngles.y, thetaT);
    return radius * ring * float3(cos(theta), sin(theta), 0.0);
}

half4 EvalFragColor(float3 patchBary, float debugVis, float4 color)
{
    if (debugVis > 0.5)
        return half4(saturate(patchBary), 1.0);
    return (half4)color;
}

#endif
