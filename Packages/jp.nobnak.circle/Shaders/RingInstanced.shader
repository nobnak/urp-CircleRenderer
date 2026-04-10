Shader "jp.nobnak.circle/Ring/Instanced"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        HLSLINCLUDE
        #pragma target 5.0
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
        ENDHLSL

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #define PASS_SHADOW 0
            #include "Packages/jp.nobnak.circle/Shaders/Includes/RingInstancedTess.hlsl"
            #pragma vertex Vert
            #pragma hull Hull
            #pragma domain Domain
            #pragma fragment Frag
            #pragma multi_compile_instancing
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #define PASS_SHADOW 1
            #include "Packages/jp.nobnak.circle/Shaders/Includes/RingInstancedTess.hlsl"
            #pragma vertex Vert
            #pragma hull Hull
            #pragma domain Domain
            #pragma fragment ShadowCasterFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #define PASS_SHADOW 0
            #include "Packages/jp.nobnak.circle/Shaders/Includes/RingInstancedTess.hlsl"
            #pragma vertex Vert
            #pragma hull Hull
            #pragma domain Domain
            #pragma fragment DepthOnlyFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #define PASS_SHADOW 0
            #include "Packages/jp.nobnak.circle/Shaders/Includes/RingInstancedTess.hlsl"
            #pragma vertex Vert
            #pragma hull Hull
            #pragma domain Domain
            #pragma fragment DepthNormalsFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            ENDHLSL
        }
    }
    FallBack Off
}
