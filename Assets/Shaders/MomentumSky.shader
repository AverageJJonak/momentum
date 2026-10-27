// MOMENTUM SKY (2026-10-16). A painted sky: one long-lat picture (MenuSky.Paint: u = azimuth from +z toward +x,
// v = elevation from the nadir to the zenith) drawn as the skybox, so a level's sky is the same kind of painting the
// menu's dome is: a violet night with a moon and flat clouds, a sepia day under a mauve overcast, a black starfield.
// WorldLook.Skybox uses it when it compiled and falls back to Skybox/Procedural when it did not.
Shader "Momentum/Sky"
{
    Properties
    {
        _MainTex("Sky (lat-long)", 2D) = "black" {}
        _Exposure("Exposure", Range(0.0, 4.0)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            Name "Sky"

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex SkyVertex
            #pragma fragment SkyFragment

            // Core: the transforms, the texture macros, PI
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half _Exposure;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dirWS      : TEXCOORD0;
            };

            Varyings SkyVertex(Attributes input)
            {
                Varyings output;
                // The skybox mesh sits on the camera: its object-space position is the view direction
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.dirWS = input.positionOS.xyz;
                return output;
            }

            half4 SkyFragment(Varyings input) : SV_Target
            {
                float3 d = normalize(input.dirWS);
                // u: azimuth from +z toward +x, 0..1 round; v: elevation, nadir 0 to zenith 1
                float u = frac(atan2(d.x, d.z) / TWO_PI);
                float v = asin(clamp(d.y, -1.0, 1.0)) / PI + 0.5;
                half3 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(u, v)).rgb;
                return half4(c * _Exposure, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback "Skybox/Procedural"
}
