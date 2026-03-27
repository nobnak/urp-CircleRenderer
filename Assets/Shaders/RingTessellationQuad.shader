Shader "Custom/RingTessellationQuad"
{
    Properties
    {
        _Radius ("Radius (centerline)", Float) = 0.5
        _RingWidth ("Ring Width", Float) = 0.1
        _Tess ("Arc Tessellation", Range(1, 64)) = 16
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
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            #ifndef UNITY_VERTEX_OUTPUT_INSTANCE_ID
            #if defined(UNITY_INSTANCING_ENABLED)
            #define UNITY_VERTEX_OUTPUT_INSTANCE_ID uint instanceID : TEXCOORD1;
            #else
            #define UNITY_VERTEX_OUTPUT_INSTANCE_ID
            #endif
            #endif

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(float, _Radius)
                UNITY_DEFINE_INSTANCED_PROP(float, _RingWidth)
                UNITY_DEFINE_INSTANCED_PROP(float, _Tess)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

            // 3 quad パッチ: 各パッチで u∈[0,1] が円周の 1/3 弧、v が内周→外周。u=0/1 が別角度になり退化しない。
            static const float kTwoPi = 6.28318530718;
            static const float kSectorSpan = kTwoPi / 3.0;

            float ArcTessFromViewScale()
            {
                float3 centerWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                float dist = distance(GetCameraPositionWS(), centerWS);
                float tanHalfFov = abs(rcp(UNITY_MATRIX_P._m11));
                float denom = max(log(1.0 + dist * tanHalfFov), 1e-5);
                float tess = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Tess);
                float baseTess = clamp(tess, 1.0, 64.0);
                return clamp(3.0 * baseTess / denom, 3.0, 64.0);
            }

            void RingRadii(out float rIn, out float rOut)
            {
                float r = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Radius);
                float w = max(UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _RingWidth), 1e-6);
                rIn = max(r - 0.5 * w, 1e-6);
                rOut = max(r + 0.5 * w, rIn + 1e-6);
            }

            float3 RingPositionOS(float theta, float v)
            {
                float rIn, rOut;
                RingRadii(rIn, rOut);
                float rr = lerp(rIn, rOut, v);
                return float3(rr * cos(theta), rr * sin(theta), 0.0);
            }

            struct Attributes
            {
                float2 uv : TEXCOORD0;
                float2 sectorPack : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ControlPoint
            {
                float4 positionOS : INTERNALTESSPOS;
                float2 arcAngles : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_INSTANCE_ID
            };

            ControlPoint Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float u = saturate(input.uv.x);
                float v = saturate(input.uv.y);
                float sector = round(input.sectorPack.x);
                float theta0 = sector * kSectorSpan;
                float theta1 = theta0 + kSectorSpan;
                float theta = lerp(theta0, theta1, u);
                ControlPoint o;
                o.positionOS = float4(RingPositionOS(theta, v), 1.0);
                o.arcAngles = float2(theta0, theta1);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                return o;
            }

            struct TessellationFactors
            {
                float edge[4] : SV_TessFactor;
                float inside[2] : SV_InsideTessFactor;
            };

            // edge[0]: V0–V1 (v=0, 内周), edge[1]: V1–V2, edge[2]: V2–V3 (v=1, 外周), edge[3]: V3–V0
            TessellationFactors PatchConstant(InputPatch<ControlPoint, 4> patch)
            {
                UNITY_SETUP_INSTANCE_ID(patch[0]);
                TessellationFactors f;
                float arc = ArcTessFromViewScale();
                f.edge[0] = arc;
                f.edge[1] = 1.0;
                f.edge[2] = arc;
                f.edge[3] = 1.0;
                f.inside[0] = arc;
                f.inside[1] = 2.0;
                return f;
            }

            [domain("quad")]
            [partitioning("integer")]
            [outputtopology("triangle_ccw")]
            [patchconstantfunc("PatchConstant")]
            [outputcontrolpoints(4)]
            [maxtessfactor(64.0)]
            ControlPoint Hull(InputPatch<ControlPoint, 4> patch, uint id : SV_OutputControlPointID)
            {
                ControlPoint o = patch[id];
                o.arcAngles = patch[0].arcAngles;
                return o;
            }

            [domain("quad")]
            Varyings Domain(TessellationFactors factors, OutputPatch<ControlPoint, 4> patch, float2 uv : SV_DomainLocation)
            {
                UNITY_SETUP_INSTANCE_ID(patch[0]);
                float u = uv.x;
                float v = uv.y;
                float2 angles = patch[0].arcAngles;
                float theta = lerp(angles.x, angles.y, u);
                float3 posOS = RingPositionOS(theta, v);
                Varyings o;
                o.positionCS = TransformObjectToHClip(posOS);
                UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Color);
                return (half4)color;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
