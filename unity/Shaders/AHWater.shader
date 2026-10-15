// Ashen Hollow: living water for URP (v2). Layered swells and chop, sun glints, a fresnel reflection of the sky
// and clouds, the ground seen through the water (refraction, when the camera keeps an opaque texture), light
// shallows and dark deeps, foam along the shore of round pools, and ripples from the game (the hero wading,
// splashes): up to 8 ripples in _AH_Ripples (x, z, start time, strength).
// v3: real wave detail from an ocean-spectrum slope map (_AH_WaveTex, two layers drifting different ways, faded out with
// distance so it never sparkles), caustics dancing over the shallows and foam broken up by a foam map (_AH_WaterDetail:
// r caustics, g foam). Both maps are made by tools/water/make_water_tex.py and set by the game (AHWaterTex).
Shader "AshenHollow/Water"
{
    Properties
    {
        _Color ("Water colour", Color) = (0.235, 0.494, 0.627, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.9
        _Foam ("Shore foam", Range(0, 2)) = 1
        _Speed ("Speed", Float) = 1
        _Mode ("Shore (0 none, 1 round pool, 2 painted sea by mask)", Float) = 1
        _SeaMask ("Sea mask (r shape, g depth from shore)", 2D) = "black" {}
        _SeaOut ("Open sea past the mask's edge", Float) = 0
        _SeaSides ("Open sea past each side (left, right, near, far)", Vector) = (0,0,0,0)
        _Refract ("Refraction", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Opacity;
                half _Foam;
                float _Speed;
                float _Mode;
                float _Refract;
                float _SeaOut;
                float4 _SeaSides;
            CBUFFER_END
            TEXTURE2D(_SeaMask); SAMPLER(sampler_SeaMask);
            TEXTURE2D(_AH_WaveTex); SAMPLER(sampler_AH_WaveTex);
            TEXTURE2D(_AH_WaterDetail); SAMPLER(sampler_AH_WaterDetail);
            float _AH_WaterTexOn;   // 1 once the game has set the maps
            float4 _AH_Sky;         // set by the game: the sky colour the water reflects
            float4 _AH_SkyTop;      // the colour high up
            float _AH_Light;        // how bright the day is (0..1)
            float4 _AH_Ripples[8];  // x, z, start time (game time), strength
            float _AH_Now;          // game time for the ripples

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float2 uv : TEXCOORD1; float fogFactor : TEXCOORD2; float4 screen : TEXCOORD3; };

            float Hash(float2 p) { float3 p3 = frac(float3(p.xyx) * 0.1031); p3 += dot(p3, p3.yzx + 33.33); return frac((p3.x + p3.y) * p3.z); }
            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }
            // a sum of directional waves: returns the slope (d height / dx, dz)
            float2 Wave(float2 p, float t, float2 k, float amp, float w) { return amp * cos(dot(p, k) + t * w) * k; }
            float2 Waves(float2 p, float t)
            {
                return Wave(p, t, float2(0.38, 0.22), 0.5, 0.9) + Wave(p, t, float2(-0.17, 0.52), 0.28, 1.15) + Wave(p, t, float2(0.61, -0.31), 0.15, 1.6)
                     + Wave(p, t, float2(1.9, 0.7), 0.085, 2.3) + Wave(p, t, float2(-0.8, 1.7), 0.08, 2.0) + Wave(p, t, float2(1.1, -1.5), 0.075, 2.7)
                     + Wave(p, t, float2(-2.6, -1.1), 0.04, 3.4);
            }
            float2 NoiseG(float2 q)
            {
                float e = 0.05, n0 = Noise(q);
                return float2(Noise(q + float2(e, 0)) - n0, Noise(q + float2(0, e)) - n0) / e;
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs pi = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = pi.positionCS;
                o.positionWS = pi.positionWS;
                o.uv = v.uv;
                o.fogFactor = ComputeFogFactor(pi.positionCS.z);
                o.screen = ComputeScreenPos(pi.positionCS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y * _Speed;
                float2 p = i.positionWS.xz;
                float d = 1.0, a = 1.0, r = 0.0, beyond = 0.0;
                if (_Mode > 1.5)
                {
                    // a sea painted on the ground: its shape and its depth from the shore from the game's mask
                    float4 mk = SAMPLE_TEXTURE2D(_SeaMask, sampler_SeaMask, i.uv);
                    // past the painted ground: open sea all round an island map (the edge pixels are not stretched out)
                    // and past a side that is (mostly) sea the sea runs on to the horizon; past a land side there is none
                    // (stretching the edge pixels out drew stripes and saw-tooth shores)
                    float oL = step(i.uv.x, 0.0), oR = step(1.0, i.uv.x), oB = step(i.uv.y, 0.0), oT = step(1.0, i.uv.y);
                    float outside = saturate(oL + oR + oB + oT);
                    float open = saturate(max(max(oL * _SeaSides.x, oR * _SeaSides.y), max(oB * _SeaSides.z, oT * _SeaSides.w)) + _SeaOut);
                    // past the edge the sea deepens slowly from the depth at the edge (it jumped straight to the deep colour,
                    // a hard-edged dark square round the land)
                    float past = max(max(-i.uv.x, i.uv.x - 1.0), max(-i.uv.y, i.uv.y - 1.0));
                    float edgeG = SAMPLE_TEXTURE2D(_SeaMask, sampler_SeaMask, saturate(i.uv)).g;
                    mk = lerp(mk, float4(open, lerp(edgeG, 1.0, saturate(past * 3.0)), 0, 1), outside);
                    beyond = outside;
                    a = smoothstep(0.3, 0.62, mk.r);
                    d = mk.g;
                    clip(a - 0.01);
                }
                else if (_Mode > 0.5)
                {
                    r = length(i.uv * 2.0 - 1.0);
                    d = saturate((1.0 - r) * 4.0);
                    a = smoothstep(0.0, 0.035, 1.0 - r);
                }
                float2 g = Waves(p, t) * 0.75;
                float camD = distance(GetCameraPositionWS(), i.positionWS);
                float near = saturate(1.0 - camD / 160.0);
                if (_AH_WaterTexOn > 0.5)
                {
                    // the swells, then real wind waves on them: two layers of the slope map drifting apart
                    float2 w1 = SAMPLE_TEXTURE2D(_AH_WaveTex, sampler_AH_WaveTex, p / 21.0 + float2(t * 0.021, t * 0.009)).rg * 2.0 - 1.0;
                    float2 w2 = SAMPLE_TEXTURE2D(_AH_WaveTex, sampler_AH_WaveTex, float2(p.y, -p.x) / 8.5 + float2(-t * 0.034, t * 0.027)).rg * 2.0 - 1.0;
                    float2 w3 = SAMPLE_TEXTURE2D(_AH_WaveTex, sampler_AH_WaveTex, p / 3.3 + float2(t * 0.06, -t * 0.05)).rg * 2.0 - 1.0;
                    g = g * 0.45 + (w1 * 0.42 + w2 * 0.3 * lerp(0.35, 1.0, near) + w3 * 0.16 * near) * 1.15;
                }
                else g += NoiseG(p * 3.0 + float2(t * 0.45, t * 0.3)) * 0.03 + NoiseG(p * 7.0 - float2(t * 0.7, t * 0.2)) * 0.012;

                // ripples: rings that spread out at 1.6 m/s and fade in 2.2 s
                float ripFoam = 0.0;
                [unroll] for (int k = 0; k < 8; k++)
                {
                    float4 rp = _AH_Ripples[k];
                    float age = _AH_Now - rp.z;
                    float live = step(0.001, rp.w) * step(0.0, age) * step(age, 2.2);
                    float2 dv = p - rp.xy; float dist = length(dv) + 1e-4;
                    float front = age * 1.6;
                    float ring = exp(-pow((dist - front) * 4.0, 2.0)) * saturate(1.0 - age / 2.2) * rp.w * live;
                    g += dv / dist * ring * cos((dist - front) * 18.0) * 0.6;
                    ripFoam += ring * 0.35;
                }
                float3 n = normalize(float3(-g.x, 1.0, -g.y));

                float3 V = normalize(GetCameraPositionWS() - i.positionWS);
                Light L = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float sh = lerp(0.55, 1.0, L.shadowAttenuation);
                float fres = 0.03 + 0.97 * pow(1.0 - saturate(dot(n, V)), 5.0);

                float3 baseC = _Color.rgb;
                float3 shallow = baseC * float3(1.15, 1.35, 1.25) + float3(0.01, 0.035, 0.03);
                float3 deep = baseC * 0.42;
                if (_AH_WaterTexOn > 0.5) { shallow = baseC * float3(0.95, 1.4, 1.3) + float3(0.0, 0.05, 0.04); deep = baseC * float3(0.22, 0.36, 0.52); }   // a clear green-blue shore, a deep navy sea
                float3 col = lerp(shallow, deep, d);
                // the ground under the water, bent by the waves (needs the camera's opaque texture)
                if (_Refract > 0.5)
                {
                    float2 suv = i.screen.xy / i.screen.w + n.xz * 0.025;
                    float3 under = SampleSceneColor(suv);
                    col = lerp(under * baseC * 1.7, col, saturate(0.55 + d * 0.45 + beyond));   // (past the painted ground there is only sky under it: it made the sea there pale)
                }
                // light dancing on the bottom of the shallows: two layers of the caustic net, the brighter where they cross
                if (_AH_WaterTexOn > 0.5)
                {
                    float2 cuv = p / 5.5 + n.xz * 0.12;
                    float c1 = SAMPLE_TEXTURE2D(_AH_WaterDetail, sampler_AH_WaterDetail, cuv + float2(t * 0.03, t * 0.017)).r;
                    float c2 = SAMPLE_TEXTURE2D(_AH_WaterDetail, sampler_AH_WaterDetail, cuv * 0.83 + float2(-t * 0.022, t * 0.031)).r;
                    float caus = min(c1, c2) * 1.6;
                    col += caus * pow(saturate(1.0 - d), 1.5) * 0.32 * _AH_Light * sh * near;
                    // the deeps darker and bluer, the shallows a clear green-blue
                    col = lerp(col, col * float3(0.82, 0.92, 1.05), saturate(d * 1.2));
                }
                float dif = saturate(dot(n, L.direction));
                col = col * (0.3 + 0.55 * _AH_Light) + col * L.color * dif * 0.35 * sh;

                // reflection: the sky gradient by the reflected ray's height, with soft cloud shapes
                float3 R = reflect(-V, n);
                float up = saturate(R.y);
                float3 skyR = lerp(_AH_Sky.rgb, _AH_SkyTop.rgb, smoothstep(0.0, 0.7, up));
                float cl = Noise(R.xz / (R.y + 0.15) * 1.6 + t * 0.02);
                skyR = lerp(skyR, float3(1, 1, 1) * (0.35 + 0.65 * _AH_Light), smoothstep(0.55, 0.85, cl) * 0.35);
                col = lerp(col, skyR * (_AH_WaterTexOn > 0.5 ? float3(0.62, 0.78, 0.95) : float3(1, 1, 1)), fres * (_AH_WaterTexOn > 0.5 ? 0.34 : 0.6));   // (seen across the waves, at a low angle, the sky took the sea over: grey-white)   // (less washed-out white sky in the water)
                // the sun's glitter
                float3 Hs = normalize(L.direction + V);
                float nh = saturate(dot(n, Hs));
                col += L.color * (pow(nh, 400.0) * 2.2 + pow(nh, 60.0) * 0.12) * sh * _AH_Light;

                // shore foam on round pools, and foam from ripples
                float fn = Noise(p * 2.2 + float2(t * 0.25, -t * 0.2));
                float band = (_Mode > 0.5) ? 1.0 - smoothstep(0.0, 0.45, d) : 0.0;
                float wv = 0.5 + 0.5 * sin(d * 18.0 - t * 1.6 + fn * 3.0);
                float fmap = _AH_WaterTexOn > 0.5 ? SAMPLE_TEXTURE2D(_AH_WaterDetail, sampler_AH_WaterDetail, p / 6.0 + float2(t * 0.02, -t * 0.015)).g : 0.6;
                float foam = smoothstep(0.55, 0.85, band * (0.6 + 0.6 * wv) * (0.55 + 0.8 * fmap) + fn * 0.35 * band) * _Foam + saturate(ripFoam);
                // white caps here and there out on the open water, where the waves crest
                if (_AH_WaterTexOn > 0.5 && _Mode > 1.5) foam += smoothstep(0.78, 0.95, SAMPLE_TEXTURE2D(_AH_WaveTex, sampler_AH_WaveTex, p / 21.0 + float2(t * 0.021, t * 0.009)).b * (0.7 + 0.5 * fmap)) * 0.35 * d * near;
                col = lerp(col, float3(0.88, 0.94, 1.0) * (0.3 + 0.7 * _AH_Light), saturate(foam));

                col = MixFog(col, i.fogFactor);
                float alpha = a * _Opacity * lerp(0.72, 1.0, d);
                if (_Refract > 0.5) alpha = a;   // the refraction already shows what is under
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
