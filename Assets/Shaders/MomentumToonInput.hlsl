// MOMENTUM TOON: the input every pass shares (Momentum/Toon and Momentum/ToonTransparent).
//
// ONE UnityPerMaterial CBUFFER, identical in every pass, so the SRP batcher takes the shader (a property in a
// different buffer in one pass breaks batching for the whole shader). The property NAMES are URP Lit's — _BaseMap,
// _BaseColor, _BumpMap, _BumpScale, _MetallicGlossMap, _Metallic, _Smoothness, _OcclusionMap, _OcclusionStrength,
// _EmissionColor, _Cutoff — and the KEYWORDS are Lit's (_NORMALMAP, _METALLICSPECGLOSSMAP, _OCCLUSIONMAP), so a
// material LevelKit built for Lit carries every map and number over to this shader untouched. The toon numbers are
// added after them.
#ifndef MOMENTUM_TOON_INPUT_INCLUDED
#define MOMENTUM_TOON_INPUT_INCLUDED

// Core: the space transforms, GPU instancing macros, the cluster-loop switch (_CLUSTER_LIGHT_LOOP -> USE_CLUSTER_LIGHT_LOOP)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
// CommonMaterial: LerpWhiteTo (the occlusion strength)
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
// The texture-streaming debug macros (empty outside the debug display; LitInput carries the same line in its CBUFFER)
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
// SurfaceInput: declares _BaseMap / _BumpMap / _EmissionMap with their samplers, and SampleAlbedoAlpha, Alpha, SampleNormal
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
half4 _BaseColor;
half4 _EmissionColor;
half4 _OutlineColor;
half4 _RimColor;
half _Cutoff;
half _Smoothness;
half _Metallic;
half _BumpScale;
half _NormalStrength;
half _OcclusionStrength;
half _Bands;
half _Band1;
half _Band2;
half _BandSoft;
half _MidTone;
half _SpecSize;
half _SpecStrength;
half _RimPower;
half _RimStrength;
half _AmbientCrush;
half _AmbientFloor;
half _ShadowTone;
half _PoolSteps;
half _PoolTail;
half _PoolLift;
half _OutlineWidth;
half _Outline;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END

TEXTURE2D(_MetallicGlossMap);   SAMPLER(sampler_MetallicGlossMap);
TEXTURE2D(_OcclusionMap);       SAMPLER(sampler_OcclusionMap);

// x = metallic, y = smoothness. With the LitFull keyword set the map carries both (red, alpha) and the alpha scales
// _Smoothness, as URP's Lit does (which is why LevelKit sets _Smoothness x 1.55 on textured surfaces); without it, the floats
half2 ToonMetalGloss(float2 uv)
{
#ifdef _METALLICSPECGLOSSMAP
    half4 mg = half4(SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv));
    return half2(mg.r, mg.a * _Smoothness);
#else
    return half2(_Metallic, _Smoothness);
#endif
}

// The baked cavity darkening (URP reads the green channel), scaled by _OcclusionStrength
half ToonOcclusion(float2 uv)
{
#ifdef _OCCLUSIONMAP
    half occ = half(SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g);
    return LerpWhiteTo(occ, _OcclusionStrength);
#else
    return half(1.0);
#endif
}

#endif
