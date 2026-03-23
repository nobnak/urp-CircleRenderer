Shader "Custom/CircleTessellation"
{
    Properties
    {
        _Center ("Center (Object Space)", Vector) = (0, 0, 0, 0)
        _Radius ("Radius", Float) = 0.5
        [IntRange] _Tess ("Edge Tessellation", Range(1, 64)) = 16
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

            static const float kTwoPi = 6.28318530718;
            static const float kThird = 2.09439510239; // 2π/3

            struct Attributes
            {
                float2 uv : TEXCOORD0;
            };

            struct ControlPoint
            {
                float4 positionOS : INTERNALTESSPOS;
                float theta : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            ControlPoint Vert(Attributes input)
            {
                uint i = (uint)input.uv.x;
                float theta = kThird * (float)i;
                float3 c = _Center.xyz;
                float2 rim = float2(cos(theta), sin(theta)) * _Radius;
                ControlPoint o;
                o.positionOS = float4(c.xy + rim, c.z, 1.0);
                o.theta = theta;
                return o;
            }

            struct TessellationFactors
            {
                float edge[3] : SV_TessFactor;
                float inside : SV_InsideTessFactor;
            };

            // Tri: edge[0]=u==0 (V1–V2), edge[1]=v==0 (V0–V2), edge[2]=w==0 (V0–V1)
            TessellationFactors PatchConstant(InputPatch<ControlPoint, 3> patch)
            {
                TessellationFactors f;
                float t = clamp(_Tess, 1.0, 64.0);
                f.edge[0] = t;
                f.edge[1] = t;
                f.edge[2] = t;
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

            float2 RimPoint(OutputPatch<ControlPoint, 3> patch, uint i)
            {
                float th = patch[i].theta;
                return _Center.xy + float2(cos(th), sin(th)) * _Radius;
            }

            // 原点から dir 方向へ、線分 A–B との最初の交点までの距離（無ければ大きい値）
            float RaySegDist2D(float2 O, float2 dir, float2 A, float2 B)
            {
                float2 e = B - A;
                float2 r = O - A;
                float denom = dir.x * e.y - dir.y * e.x;
                if (abs(denom) < 1e-8)
                    return 1e9;
                float t = (r.x * e.y - r.y * e.x) / denom;
                float u = (r.x * dir.y - r.y * dir.x) / denom;
                if (t >= 0.0 && u >= 0.0 && u <= 1.0)
                    return t;
                return 1e9;
            }

            float ChordTriExitDist(float2 O, float2 dir, float2 P0, float2 P1, float2 P2)
            {
                float d = RaySegDist2D(O, dir, P0, P1);
                d = min(d, RaySegDist2D(O, dir, P1, P2));
                d = min(d, RaySegDist2D(O, dir, P2, P0));
                return d;
            }

            [domain("tri")]
            Varyings Domain(TessellationFactors factors, OutputPatch<ControlPoint, 3> patch, float3 bary : SV_DomainLocation)
            {
                float u = bary.x;
                float v = bary.y;
                float w = bary.z;
                float th0 = patch[0].theta;
                float th1 = patch[1].theta;
                float th2 = patch[2].theta;
                float2 P0 = RimPoint(patch, 0);
                float2 P1 = RimPoint(patch, 1);
                float2 P2 = RimPoint(patch, 2);
                float2 O = _Center.xy;
                float2 pos2;
                static const float kEps = 1e-5;
                if (u <= kEps)
                {
                    float t = w / max(v + w, kEps);
                    float th = th1 + t * kThird;
                    pos2 = O + float2(cos(th), sin(th)) * _Radius;
                }
                else if (v <= kEps)
                {
                    float t = u / max(u + w, kEps);
                    float th = th2 + t * kThird;
                    if (th >= kTwoPi - kEps)
                        th -= kTwoPi;
                    pos2 = O + float2(cos(th), sin(th)) * _Radius;
                }
                else if (w <= kEps)
                {
                    float t = v / max(u + v, kEps);
                    float th = th0 + t * kThird;
                    pos2 = O + float2(cos(th), sin(th)) * _Radius;
                }
                else
                {
                    float2 pc = u * P0 + v * P1 + w * P2;
                    float len = length(pc - O);
                    if (len < kEps)
                        pos2 = O;
                    else
                    {
                        float2 dir = (pc - O) / len;
                        float d = ChordTriExitDist(O, dir, P0, P1, P2);
                        if (d > 1e8)
                            d = len;
                        float r = len * (_Radius / max(d, kEps));
                        pos2 = O + dir * r;
                    }
                }
                float3 posOS = float3(pos2, _Center.z);
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
