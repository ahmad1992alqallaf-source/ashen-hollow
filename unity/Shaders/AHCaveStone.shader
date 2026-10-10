// Ashen Hollow: cave and dungeon walls. The walls were drawn in flat painted colours (one colour per wall piece); this
// keeps each piece's colour and lays a rock grain over it, projected from three sides so it needs no texture layout.
// Lit by the sun or cave light, the sky, and the torches and braziers (the extra lights).
Shader "AshenHollow/CaveStone"
{
    Properties
    {
        _BaseMap ("Stone grain", 2D) = "white" {}
        _Scale ("Grain size (per m)", Float) = 0.33
        _Tint ("Tint", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _Scale;
                half4 _Tint;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; half4 color : COLOR; half fog : TEXCOORD2; };
            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs pi = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = pi.positionCS; o.positionWS = pi.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = v.color;
                o.fog = ComputeFogFactor(pi.positionCS.z);
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                float3 n = SafeNormalize(i.normalWS);
                float3 w = abs(n); w = pow(w, 4); w /= max(1e-4, w.x + w.y + w.z);
                float3 p = i.positionWS * _Scale;
                half g = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, p.zy).r * w.x
                       + SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, p.xz).r * w.y
                       + SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, p.xy).r * w.z;
                // a second, coarser grain so the rock does not repeat in a visible grid
                float3 q = i.positionWS * (_Scale * 0.23);
                half g2 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, q.zy + 0.37).r * w.x
                        + SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, q.xz + 0.37).r * w.y
                        + SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, q.xy + 0.37).r * w.z;
                half3 albedo = i.color.rgb * _Tint.rgb * (g * 0.75 + g2 * 0.45) * 1.05;

                Light L = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half3 lit = L.color * saturate(dot(n, L.direction)) * L.shadowAttenuation * L.distanceAttenuation + SampleSH(n);
            #if defined(_ADDITIONAL_LIGHTS)
                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                uint count = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(count)
                    Light a = GetAdditionalLight(lightIndex, i.positionWS);
                    lit += a.color * a.distanceAttenuation * a.shadowAttenuation * saturate(dot(n, a.direction));
                LIGHT_LOOP_END
            #endif
                half3 col = albedo * lit;
                col = MixFog(col, i.fog);
                if (any(isnan(col)) || any(isinf(col))) col = half3(0, 0, 0);
                return half4(max(col, 0), 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            float3 _LightDirection;
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            float4 vert(A v) : SV_POSITION
            {
                float3 pw = TransformObjectToWorld(v.positionOS.xyz); float3 nw = TransformObjectToWorldNormal(v.normalOS);
                float4 c = TransformWorldToHClip(ApplyShadowBias(pw, nw, _LightDirection));
            #if UNITY_REVERSED_Z
                c.z = min(c.z, UNITY_NEAR_CLIP_VALUE);
            #else
                c.z = max(c.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                return c;
            }
            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 vert(float4 p : POSITION) : SV_POSITION { return TransformObjectToHClip(p.xyz); }
            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
