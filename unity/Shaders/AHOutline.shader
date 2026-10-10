// Ashen Hollow: an anime ink line round armour and weapons (made with Tripo from our concept art), so they sit with
// the VRoid heroes' cel look. The inverted-hull way: the mesh drawn again, pushed out along its normals and showing
// only its back faces, in a dark ink colour. The width is kept about the same on screen near and far.
Shader "AshenHollow/Outline"
{
    Properties
    {
        _OutlineColor ("Ink colour", Color) = (0.07, 0.05, 0.045, 1)
        _Width ("Width (pixels at 1080p)", Float) = 1.6
        _MaxWorld ("Widest in metres", Float) = 0.012
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+10" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _Width;
                float _MaxWorld;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float fog : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                float3 n = normalize(TransformObjectToWorldNormal(v.normalOS));
                // metres per pixel at this depth, so the line is about _Width pixels wide on a 1080-line screen
                float dist = length(_WorldSpaceCameraPos.xyz - ws);
                float perPixel = dist * 2.0 * tan(radians(30.0) * 0.5) / 1080.0;
                float w = min(_Width * perPixel, _MaxWorld);
                ws += n * w;
                o.positionCS = TransformWorldToHClip(ws);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 c = MixFog(_OutlineColor.rgb, i.fog);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
