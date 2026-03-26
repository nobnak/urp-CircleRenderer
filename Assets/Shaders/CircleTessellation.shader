Shader "Custom/CircleTessellation"
{
    Properties
    {
        _Radius ("Radius", Float) = 0.5
        [IntRange] _Tess ("Arc Tessellation (BC)", Range(1, 64)) = 16
        _Color ("Color", Color) = (1, 1, 1, 1)
        [Enum(Off,0,Barycentric,1)] _DebugVis ("Debug: Patch Barycentric (R=A,G=B,B=C)", Float) = 0
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
                float _Radius;
                float _Tess;
                float4 _Color;
                float _DebugVis;
            CBUFFER_END

            // 1 パッチのみ: 中心はオブジェクト空間原点固定。A=原点, B=θ=0, C=θ=2π（B/C 同座標で退化可）。弧は 0→2π。
            static const float kTwoPi = 6.28318530718;

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
                float3 patchBary : TEXCOORD0;
            };

            // uv.x: 0=中心A, 1=B(θ=0), 2=C(θ=2π, 位置は B と同じで可)
            ControlPoint Vert(Attributes input)
            {
                uint corner = (uint)input.uv.x;
                float theta0 = 0.0;
                float theta1 = kTwoPi;
                float3 p;
                if (corner == 0u)
                    p = float3(0.0, 0.0, 0.0);
                else
                    p = float3(_Radius, 0.0, 0.0);
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

            // edge[0]=u==0 → V1–V2（弧 BC）のみ分割。径方向 A–B, A–C は 1。
            TessellationFactors PatchConstant(InputPatch<ControlPoint, 3> patch)
            {
                TessellationFactors f;
                float arc = clamp(_Tess, 1.0, 64.0);
                f.edge[0] = arc;
                f.edge[1] = 1.0;
                f.edge[2] = 1.0;
                f.inside = 1.0;
                return f;
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
                float ring = saturate(bary.y + bary.z);
                float r = _Radius * ring;
                float thetaT = bary.z / max(ring, 1e-6);
                float theta = thetaT * kTwoPi;
                float3 posOS = lerp(r * float3(cos(theta), sin(theta), 0.0), 0.0, bary.x);
                Varyings o;
                o.positionCS = TransformObjectToHClip(posOS);
                o.patchBary = bary;
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                if (_DebugVis > 0.5h)
                    return half4(saturate(input.patchBary), 1.0h);
                return (half4)_Color;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
