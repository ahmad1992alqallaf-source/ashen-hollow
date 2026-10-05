// Ashen Hollow: soft glowing sparks for the particle effects (spell signatures, hits, level-ups). Additive, coloured
// per particle by the particle system's vertex colour, round and soft from the quad's UVs.
Shader "AshenHollow/Spark"
{
    Properties
    {
        _Boost ("Brightness", Float) = 1.6
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+20" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Pass
        {
            Name "Spark"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Boost;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float r = length(i.uv * 2.0 - 1.0);
                float core = saturate(1.0 - r);
                float a = core * core * (0.6 + 0.4 * core);
                half3 c = i.color.rgb * _Boost + core * core * core * 0.6;   // a hot white heart
                return half4(c, saturate(a * i.color.a * 1.4));
            }
            ENDHLSL
        }
    }
    FallBack Off
}
