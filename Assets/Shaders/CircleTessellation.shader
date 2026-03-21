Shader "Custom/CircleTessellation"
{
    Properties
    {
        _Center ("Center (Object Space)", Vector) = (0, 0, 0, 0)
        _Radius ("Radius", Float) = 0.5
        [IntRange] _Tess ("Arc Tessellation", Range(1, 64)) = 16
        _Color ("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma target 5.0
            #pragma vertex Vert
            #pragma hull Hull
            #pragma domain Domain
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Center;
                float _Radius;
                float _Tess;
                float4 _Color;
            CBUFFER_END

            // 1 パッチは最大 π の扇形。全周には最低 2 パッチ（メッシュ CircleTessellationPatch と一致）。
            static const float kPatchCount = 2.0;

            struct Attributes
            {
                float2 uv : TEXCOORD0;
            };

            struct ControlPoint
            {
                float4 positionOS : INTERNALTESSPOS;
                float2 sectorAngles : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            ControlPoint Vert(Attributes input)
            {
                static const float kTwoPi = 6.28318530718;
                uint corner = (uint)input.uv.x;
                float patchIndex = input.uv.y;
                float theta0 = kTwoPi * (patchIndex / kPatchCount);
                float theta1 = kTwoPi * ((patchIndex + 1.0) / kPatchCount);
                float3 c = _Center.xyz;
                float3 p;
                if (corner == 0u)
                    p = c;
                else if (corner == 1u)
                    p = c + float3(cos(theta0), sin(theta0), 0.0) * _Radius;
                else
                    p = c + float3(cos(theta1), sin(theta1), 0.0) * _Radius;
                ControlPoint o;
                o.positionOS = float4(p, 1.0);
                o.sectorAngles = float2(theta0, theta1);
                return o;
            }

            struct TessellationFactors
            {
                float edge[3] : SV_TessFactor;
                float inside : SV_InsideTessFactor;
            };

            // Tri patch: edge[0]=u==0 (V1–V2), edge[1]=v==0 (V0–V2), edge[2]=w==0 (V0–V1) — not CP order.
            TessellationFactors PatchConstant(InputPatch<ControlPoint, 3> patch)
            {
                TessellationFactors f;
                float arc = clamp(_Tess, 1.0, 64.0);
                f.edge[0] = arc;   // arc θ0–θ1
                f.edge[1] = 1.0; // radial center–θ1
                f.edge[2] = 1.0; // radial center–θ0
                f.inside = 1.0;
                return f;
            }

            [domain("tri")]
            [partitioning("fractional_odd")]
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
                float theta0 = patch[0].sectorAngles.x;
                float theta1 = patch[0].sectorAngles.y;
                float b = bary.y;
                float cz = bary.z;
                float w = b + cz;
                float3 posOS;
                if (w <= 1e-8)
                    posOS = _Center.xyz;
                else
                {
                    float t = cz / w;
                    float theta = lerp(theta0, theta1, t);
                    float r = _Radius * w;
                    posOS = _Center.xyz + float3(r * cos(theta), r * sin(theta), 0.0);
                }
                Varyings o;
                o.positionCS = TransformObjectToHClip(posOS);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return (half4)_Color;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
