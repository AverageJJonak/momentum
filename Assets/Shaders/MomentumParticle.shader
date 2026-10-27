// THE PARTICLE (2026-10-27): the game's own particle shader, so the puffs, the steam, the embers and the dust never
// ride on the stock Particles/Unlit (which draws the error shader's magenta on some machines — the pink hunter named
// it). The smallest thing a particle needs: one map, one colour, the particle's own colour and fade (COLOR), the
// blend and cull the material asks for, the scene's fog (fogged to black when the blend is additive, so a lit mote
// never adds grey). The same property names as the stock shader, so LevelKit's Particle() sets it the same way.
Shader "Momentum/Particle"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Colour", Color) = (1, 1, 1, 1)
        _SrcBlend ("Src", Float) = 5
        _DstBlend ("Dst", Float) = 10
        _ZWrite ("ZWrite", Float) = 0
        _Cull ("Cull", Float) = 0
        [HideInInspector] _Surface ("Surface", Float) = 1
        [HideInInspector] _Blend ("Blend", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Pass
        {
            Name "Particle"
            Tags { "LightMode" = "UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex ParticleVertex
            #pragma fragment ParticleFragment
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _SrcBlend;
                float _DstBlend;
                float _ZWrite;
                float _Cull;
                float _Surface;
                float _Blend;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float fogFactor : TEXCOORD1;
            };

            Varyings ParticleVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 ParticleFragment(Varyings input) : SV_Target
            {
                half4 colour = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor * input.color;
                half3 fogColour = _DstBlend > 0.5 && _DstBlend < 1.5 ? half3(0.0, 0.0, 0.0) : unity_FogColor.rgb;   // additive (One One) fogs to black
                colour.rgb = MixFogColor(colour.rgb, fogColour, input.fogFactor);
                return colour;
            }
            ENDHLSL
        }
    }
}
