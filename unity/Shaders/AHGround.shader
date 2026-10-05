// Ashen Hollow: the ground for URP. The web game's painted ground map, with detail added in world space:
// large soft patches of lighter and darker earth, fine grain, and small bumps that catch the light.
// Lit by URP's own lighting (sun with shadows, campfires and other lights, ambient occlusion).
// In the cities the streets and squares inside the walls are laid with cobblestones (_PaveMap), keeping the map's own
// colours, so each city has its own stone; grass and water stay as they are.
Shader "AshenHollow/Ground"
{
    Properties
    {
        _BaseMap ("Ground map", 2D) = "white" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Detail ("Detail", Range(0, 1)) = 1
        _Bump ("Bumps", Range(0, 2)) = 1
        _PaveMap ("Cobblestones (grey)", 2D) = "grey" {}
        _PaveScale ("Cobble tiles per metre", Float) = 0.25
        _PaveAmt ("Paving", Range(0, 1)) = 0
        _PaveRect ("Paved area (x0, z0, x1, z1)", Vector) = (0, 0, 0, 0)
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
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_PaveMap); SAMPLER(sampler_PaveMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _Tint;
                half _Detail;
                half _Bump;
                float _PaveScale;
                half _PaveAmt;
                float4 _PaveRect;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float2 uv : TEXCOORD1; float fogFactor : TEXCOORD2; float3 normalWS : TEXCOORD3; };

            float Hash(float2 p) { float3 p3 = frac(float3(p.xyx) * 0.1031); p3 += dot(p3, p3.yzx + 33.33); return frac((p3.x + p3.y) * p3.z); }
            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }
            float Fbm(float2 p) { return Noise(p) * 0.5 + Noise(p * 2.1 + 3.1) * 0.3 + Noise(p * 4.3 + 7.7) * 0.2; }

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs pi = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = pi.positionCS;
                o.positionWS = pi.positionWS;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fogFactor = ComputeFogFactor(pi.positionCS.z);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 p = i.positionWS.xz;
                float3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _Tint.rgb;
                // large patches, a hint of warmer and cooler earth, and grain
                float macro = Fbm(p * 0.045);
                float hue = Noise(p * 0.11 + 5.3);
                float grain = Noise(p * 3.1) * 0.6 + Noise(p * 9.0) * 0.4;
                float3 det = albedo * lerp(0.84, 1.14, macro) * lerp(float3(1.03, 0.99, 0.94), float3(0.96, 1.0, 1.04), hue) * lerp(0.93, 1.06, grain);
                // cobblestones: inside the walls, not on grass or water
                float3 a0 = albedo;
                float inRect = step(_PaveRect.x, p.x) * step(p.x, _PaveRect.z) * step(_PaveRect.y, p.y) * step(p.y, _PaveRect.w);
                float green = saturate((a0.g - max(a0.r, a0.b)) * 14.0), blue = saturate((a0.b - a0.r - 0.06) * 8.0);
                float pm = _PaveAmt * inRect * (1.0 - green) * (1.0 - blue);
                float2 g = 0;
                if (pm > 0.001)
                {
                    float2 pu = p * _PaveScale; float pe = 0.0025;
                    float c = SAMPLE_TEXTURE2D(_PaveMap, sampler_PaveMap, pu).r;
                    float cx = SAMPLE_TEXTURE2D(_PaveMap, sampler_PaveMap, pu + float2(pe, 0)).r, cz = SAMPLE_TEXTURE2D(_PaveMap, sampler_PaveMap, pu + float2(0, pe)).r;
                    // stones a touch lighter and varied, the joints between them dark
                    float stone = Noise(floor(pu * 9.0) + 0.5);
                    float3 paved = a0 * saturate(0.25 + 1.5 * c) * 1.05 * lerp(0.86, 1.12, stone);
                    det = lerp(det, paved * lerp(0.9, 1.08, grain), pm);
                    g = float2(cx - c, cz - c) * 12.0 * pm;
                }
                albedo = lerp(albedo, det, max(_Detail, pm));
                // small bumps
                float e = 0.04, b0 = Noise(p * 2.4), bx = Noise((p + float2(e, 0)) * 2.4), bz = Noise((p + float2(0, e)) * 2.4);
                g += float2(bx - b0, bz - b0) / e * 0.08 * _Bump * (1.0 - pm * 0.7);
                float3 n = normalize(normalize(i.normalWS) + float3(-g.x, 0, -g.y));

                InputData id = (InputData)0;
                id.positionWS = i.positionWS;
                id.positionCS = i.positionCS;
                id.normalWS = n;
                id.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                id.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                id.fogCoord = i.fogFactor;
                id.bakedGI = SampleSH(n);
                id.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                id.shadowMask = half4(1, 1, 1, 1);

                SurfaceData sd = (SurfaceData)0;
                sd.albedo = albedo;
                sd.alpha = 1;
                sd.metallic = 0;
                sd.smoothness = 0.08 + grain * 0.06 + pm * 0.06;
                sd.occlusion = lerp(0.85, 1.0, macro);
                sd.normalTS = float3(0, 0, 1);
                sd.specular = 0;

                half4 c = UniversalFragmentPBR(id, sd);
                c.rgb = MixFog(c.rgb, i.fogFactor);
                return half4(c.rgb, 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _Tint;
                half _Detail;
                half _Bump;
                float _PaveScale;
                half _PaveAmt;
                float4 _PaveRect;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings vert(Attributes v) { Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); return o; }
            half frag(Varyings i) : SV_Target { return i.positionCS.z; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _Tint;
                half _Detail;
                half _Bump;
                float _PaveScale;
                half _PaveAmt;
                float4 _PaveRect;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };
            Varyings vert(Attributes v) { Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.normalWS = TransformObjectToWorldNormal(v.normalOS); return o; }
            half4 frag(Varyings i) : SV_Target { return half4(NormalizeNormalPerPixel(normalize(i.normalWS)), 0.0); }
            ENDHLSL
        }
    }
    FallBack Off
}
