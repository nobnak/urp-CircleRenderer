Shader "Custom/CircleTessellation"
{
    Properties
    {
        _Radius ("Radius", Float) = 0.5
        _Tess ("Arc Tessellation (BC)", Range(1, 64)) = 16
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
                UNITY_DEFINE_INSTANCED_PROP(float, _Tess)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float, _DebugVis)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

            // 3 セクタ: 各パッチは A=中心, B/C=円周上の隣接点。退化シームを避けて円を構成する。
            static const float kTwoPi = 6.28318530718;
            static const float kSectorSpan = kTwoPi / 3.0;

            // 弧の分割数 = _Tess / log(1 + dist * tan(half vertical fov))。線形より遠距離で分割が落ちにくい。dist=カメラ〜円中心。下限 3。
            float ArcTessFromViewScale()
            {
                float3 centerWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                float dist = distance(GetCameraPositionWS(), centerWS);
                float tanHalfFov = abs(rcp(UNITY_MATRIX_P._m11));
                float denom = max(log(1.0 + dist * tanHalfFov), 1e-5);
                float tess = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Tess);
                float baseTess = clamp(tess, 1.0, 64.0);
                return clamp(3 * baseTess / denom, 3.0, 64.0);
            }

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

            // uv.x: 0=中心A, 1=B, 2=C / uv.y: セクタ index(0..2)
            ControlPoint Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                uint corner = (uint)input.uv.x;
                float sector = round(input.uv.y);
                float theta0 = sector * kSectorSpan;
                float theta1 = theta0 + kSectorSpan;
                float radius = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Radius);
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
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                return o;
            }

            struct TessellationFactors
            {
                float edge[3] : SV_TessFactor;
                float inside : SV_InsideTessFactor;
            };

            // edge[0]=u==0 の弧 BC を分割。径方向 A–B, A–C は 1。
            TessellationFactors PatchConstant(InputPatch<ControlPoint, 3> patch)
            {
                UNITY_SETUP_INSTANCE_ID(patch[0]);
                TessellationFactors f;
                float arc = ArcTessFromViewScale();
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
                UNITY_SETUP_INSTANCE_ID(patch[0]);
                float radius = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Radius);
                float ring = saturate(bary.y + bary.z);
                float2 sectorAngles = patch[0].sectorAngles;
                float thetaT = bary.z / max(ring, 1e-6);
                float theta = lerp(sectorAngles.x, sectorAngles.y, thetaT);
                float3 posOS = radius * ring * float3(cos(theta), sin(theta), 0.0);
                Varyings o;
                o.positionCS = TransformObjectToHClip(posOS);
                o.patchBary = bary;
                UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Color);
                float debugVis = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DebugVis);
                if (debugVis > 0.5h)
                    return half4(saturate(input.patchBary), 1.0h);
                return (half4)color;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
