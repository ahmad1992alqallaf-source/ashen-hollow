// Ashen Hollow: the great mountain ranges around every land. Vertex-coloured (forest, rock, snow, painted by AHRange),
// lit by the sun and the sky, and fading into a blue-grey haze with distance instead of the game's fog, so the far
// peaks stand against the sky as soft silhouettes rather than vanishing.
Shader "AshenHollow/Range"
{
    Properties
    {
        _Haze ("Haze colour", Color) = (0.7, 0.78, 0.86, 1)
        _HazeNear ("Haze starts (m)", Float) = 110
        _HazeFar ("Haze full (m)", Float) = 650
        _HazeMax ("Most haze", Range(0, 1)) = 0.5
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Haze;
                float _HazeNear;
                float _HazeFar;
                half _HazeMax;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; half4 color : COLOR; };
            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs pi = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = pi.positionCS; o.positionWS = pi.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = v.color;
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                float3 n = SafeNormalize(i.normalWS);   // a few ridge triangles are degenerate: normalize() would give NaN
                Light L = GetMainLight();
                half ndl = saturate(dot(n, L.direction));
                half3 amb = SampleSH(n);
                half3 col = i.color.rgb * (L.color * ndl * 0.95 + amb * 0.85);
                float d = distance(i.positionWS, GetCameraPositionWS());
                // aerial perspective: further is bluer and paler; the far range (vertex alpha) a little more so
                half h = saturate((d - _HazeNear) / max(1.0, _HazeFar - _HazeNear)) * _HazeMax + (1.0 - i.color.a) * 0.18;
                col = lerp(col, _Haze.rgb, saturate(h));
                // never let a NaN out: one NaN pixel blooms into a white flash over the whole screen
                if (any(isnan(col)) || any(isinf(col))) col = _Haze.rgb;
                return half4(max(col, 0), 1);
            }
            ENDHLSL
        }
    }
}
