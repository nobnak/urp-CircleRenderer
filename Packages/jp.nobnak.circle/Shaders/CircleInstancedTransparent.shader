Shader "jp.nobnak.circle/Circle/Instanced Transparent"
{
    Properties
    {
        [Header(Blend)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }
        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite Off
            Blend [_SrcBlend] [_DstBlend]

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
            #ifndef UNITY_GET_INSTANCE_ID
            #define UNITY_GET_INSTANCE_ID(input) 0u
            #endif

            #include "Packages/jp.nobnak.circle/Shaders/Includes/CircleShared.hlsl"

            struct CircleInstanceData
            {
                float radius;
                float tess;
                float debugVis;
                float tessMode;
                float4 color;
            };

            StructuredBuffer<CircleInstanceData> _CircleInstances;

            CircleInstanceData LoadInstance(uint iid)
            {
                return _CircleInstances[iid];
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
                o.positionCS = TransformObjectToHClip(posOS);
                o.patchBary = bary;
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
            ENDHLSL
        }
    }
    FallBack Off
}
