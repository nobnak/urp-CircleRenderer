Shader "Custom/Circle"
{
    Properties
    {
        _Radius ("Radius", Float) = 0.5
        _Tess ("Arc Tessellation (BC)", Range(1, 64)) = 16
        [Enum(Fixed,0,Log Distance,1)] _TessMode ("Tessellation Mode", Float) = 1
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
                UNITY_DEFINE_INSTANCED_PROP(float, _TessMode)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float, _DebugVis)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
            #include "Assets/Shaders/Includes/CircleShared.hlsl"

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
                return EvalFragColor(input.patchBary, debugVis, color);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
