// THE SHEET (2026-10-23): the smallest see-through unlit shader that can exist in this pipeline — one colour, one map,
// the scene's fog, the blend and cull the material asks for. MaterialNet's stand-in for a see-through material whose
// own shader comes out magenta on a machine (the draw test); the smog, the haze and the glass sheets land on it.
Shader "Momentum/Sheet"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Colour", Color) = (1, 1, 1, 1)
        _SrcBlend ("Src", Float) = 5
        _DstBlend ("Dst", Float) = 10
        _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Sheet"
            Tags { "LightMode" = "UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex SheetVertex
            #pragma fragment SheetFragment
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _SrcBlend;
                float _DstBlend;
                float _Cull;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };

            Varyings SheetVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 SheetFragment(Varyings input) : SV_Target
            {
                half4 colour = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                colour.rgb = MixFog(colour.rgb, input.fogFactor);
                return colour;
            }
            ENDHLSL
        }
    }
}
