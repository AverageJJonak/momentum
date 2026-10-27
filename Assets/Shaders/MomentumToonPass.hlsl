// MOMENTUM TOON: the forward pass. DARK DECO's cel light: a world painted on black paper, the light drawn on in flat
// bands. Used by Momentum/Toon as it is, and by Momentum/ToonTransparent with MOMENTUM_TOON_TRANSPARENT and
// _SURFACE_TYPE_TRANSPARENT defined ahead of it (alpha out, a probe reflection on the glass, no SSAO).
//
//   THE RAMP     what lands (N.L x shadow) is posterised: _Bands 2 = dark | lit at _Band2; _Bands 3 = a mid tone at
//                _Band1 between them. _BandSoft is the width of each edge: a hard pencil line, not an aliased one.
//                THE KEY'S DARK BAND IS NOT BLACK (readability, 2026-10-16): it is the lit band x _ShadowTone (0.45),
//                so a face turned from the key, a floor under a raking key (N.L = sin 26 deg, under _Band2) and a room
//                the key is shadowed out of all sit at a mid tone: lit : shadow = 2.2 : 1, the paintings' ochre over
//                umber. Only the key gets the tone; the lamps and the rim keep a hard 0 | 1 ramp, so a pool is a pool.
//   THE POOLS    a lamp's light (attenuation x intensity, URP's own number) is posterised into _PoolSteps rings with
//                _PoolTail of the smooth value kept under them, so a lamp throws a hard-edged pool and still fills a room.
//                The rings are ROUNDED (not floored) in the sqrt domain and lifted by _PoolLift toward it, so the three
//                rings of a 3-step pool land at 0.22 / 0.56 / 1.0 of the lamp (floored squares put them at 0.11 / 0.44 /
//                1.0, and a lamp at three metres fell on a floor as nothing): a visible band on the floor, the painting's.
//   THE CAP      one hard specular cap of _SpecSize; off by default (matte poster surfaces), thin on metals and on
//                anything that sets _SpecStrength (lacquer).
//   THE RIM      a band along the silhouette on the side the key does not reach: a figure reads against black.
//   THE PAPER    the probe's ambient kept at _AmbientCrush (0.8) over a floor (_AmbientFloor, 0.10 of the albedo): the
//                black paper is the sky, the far city and the fog's far end, never the floor the player runs on.
//   THE GLOW     _EmissionColor added last, over everything: weakspots, targets and lit windows glow through the ramp.
// Same URP 17 idioms as Lit: GetMainLight(inputData, shadowMask, aoFactor) with the shadow coord from the vertex or
// the pixel, the clustered (Forward+) directional loop + LIGHT_LOOP_BEGIN/END for the lamps, MixFog at the end.
#ifndef MOMENTUM_TOON_PASS_INCLUDED
#define MOMENTUM_TOON_PASS_INCLUDED

#include "MomentumToonInput.hlsl"
// Lighting: GetMainLight / GetAdditionalLight, the LIGHT_LOOP macros, the SH / lightmap macros, shadows, AO, the fog helpers
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv                       : TEXCOORD0;
    float3 positionWS               : TEXCOORD1;
    float3 normalWS                 : TEXCOORD2;
#if defined(_NORMALMAP)
    half4 tangentWS                 : TEXCOORD3;    // xyz: tangent, w: sign
#endif
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    half4 fogFactorAndVertexLight   : TEXCOORD4;    // x: fogFactor, yzw: vertex light
#else
    half  fogFactor                 : TEXCOORD4;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord              : TEXCOORD5;
#endif
    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 6);
#ifdef USE_APV_PROBE_OCCLUSION
    float4 probeOcclusion           : TEXCOORD7;
#endif
    float4 positionCS               : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

// The per-pixel inputs URP's light functions want, filled the way LitForwardPass fills them
void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;
    inputData.positionWS = input.positionWS;

    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
#if defined(_NORMALMAP)
    float sgn = input.tangentWS.w;      // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
    inputData.tangentToWorld = tangentToWorld;
    inputData.normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
#else
    inputData.normalWS = input.normalWS;
#endif

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = viewDirWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
#else
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
#endif

    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
}

///////////////////////////////////////////////////////////////////////////////
//                           The cel functions                               //
///////////////////////////////////////////////////////////////////////////////

// What lands, posterised. 0 in the dark band, _MidTone in the mid band (three-band ramps only), 1 in the lit band
half ToonRamp(half t)
{
    half lit = smoothstep(_Band2 - _BandSoft, _Band2 + _BandSoft, t);
    half mid = smoothstep(_Band1 - _BandSoft, _Band1 + _BandSoft, t) * _MidTone * step(2.5, _Bands);
    return max(lit, mid);
}

// A lamp's energy at the point (attenuation x intensity), posterised into rings. sqrt first so the rings sit where the
// eye expects them (a lamp's falloff is steep); _PoolTail of the smooth value survives under the rings
half ToonPool(half energy)
{
    half s = max(_PoolSteps, 1.0);
    half q = round(sqrt(saturate(energy)) * s) / s;   // the nearest ring, so a ring stands for the energy it covers
    half ring = lerp(q * q, q, _PoolLift);            // lifted toward the sqrt domain: the outer ring reads on a floor
    return max(ring, energy * _PoolTail);
}

// One light through the ramp: the flat diffuse band (its dark side at `shadowTone` of the lit side: _ShadowTone for the
// key, 0 for a lamp or the rim), and the one specular cap where the band is lit
half3 ToonLight(Light light, half3 N, half3 V, half3 albedo, half metallic, half smoothness, half shadowTone)
{
    half lum = max(light.color.r, max(light.color.g, light.color.b));
    half3 tint = light.color / max(lum, 1e-4);
    // A directional light's distance attenuation is exactly 1; anything under it is a lamp, whose energy gets the rings
    half punctual = step(light.distanceAttenuation, 0.9999);
    half energy = light.distanceAttenuation * lum;
    half pool = lerp(energy, ToonPool(energy), punctual);

    half ndl = saturate(dot(N, light.direction));
    half ramp = ToonRamp(ndl * light.shadowAttenuation);
    half band = lerp(shadowTone, 1.0, ramp);   // the dark band is a mid tone, not black
    half3 c = albedo * tint * (band * pool);

    // The cap: a Blinn half-vector threshold, the size shrinking as the surface gets smoother, a hard edge
    half3 H = SafeNormalize(float3(light.direction) + float3(V));
    half ndh = saturate(dot(N, H));
    half size = _SpecSize * lerp(1.5, 0.5, smoothness);
    half thr = cos(size * HALF_PI);
    half cap = smoothstep(thr - _BandSoft, thr + _BandSoft, ndh);
    // Matte unless asked (_SpecStrength) or a smooth metal (brass): and nothing under a smoothness of 0.35 ever shines
    half strength = max(_SpecStrength, metallic * 0.3 * smoothstep(0.6, 0.9, smoothness)) * smoothstep(0.35, 0.75, smoothness);
    half3 specTint = lerp(half3(1.0, 1.0, 1.0), albedo, metallic);
    c += specTint * tint * (cap * ramp * pool * strength);
    return c;
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

Varyings ToonVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.positionWS = vertexInput.positionWS;
    output.normalWS = normalInput.normalWS;
#if defined(_NORMALMAP)
    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = half4(normalInput.tangentWS.xyz, sign);
#endif

    half fogFactor = 0;
#if !defined(_FOG_FRAGMENT)
    fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
#endif
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
#else
    output.fogFactor = fogFactor;
#endif

    OUTPUT_SH4(vertexInput.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.vertexSH, output.probeOcclusion);

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

    output.positionCS = vertexInput.positionCS;
    return output;
}

void ToonFragment(
    Varyings input
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    // The surface
    half4 albedoAlpha = SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    half alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);
    half3 albedo = albedoAlpha.rgb * _BaseColor.rgb;
    half2 mg = ToonMetalGloss(input.uv);
    half occlusion = ToonOcclusion(input.uv);
    // The normal map at _NormalStrength of its own scale: the seams and bolts stay readable, and flat
    half3 normalTS = SampleNormal(input.uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale * _NormalStrength);

    InputData inputData;
    InitializeInputData(input, normalTS, inputData);
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData.normalizedScreenSpaceUV, occlusion);
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, aoFactor);
    uint meshRenderingLayers = GetMeshRenderingLayer();

    half3 N = half3(inputData.normalWS);
    half3 V = inputData.viewDirectionWS;

    // THE PAPER: the probe's ambient at _AmbientCrush, over a floor of _AmbientFloor x the albedo
    half3 color = albedo * (inputData.bakedGI * _AmbientCrush * aoFactor.indirectAmbientOcclusion + _AmbientFloor);

    // THE KEY: the one light whose dark band is a tone (_ShadowTone) rather than black
#ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
#endif
    {
        color += ToonLight(mainLight, N, V, albedo, mg.x, mg.y, _ShadowTone);
    }

    // THE LAMPS (and any further directional light, the rim): the same ramp
#if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_CLUSTER_LIGHT_LOOP
    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK

        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
#ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
        {
            color += ToonLight(light, N, V, albedo, mg.x, mg.y, 0.0);
        }
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
#ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
        {
            color += ToonLight(light, N, V, albedo, mg.x, mg.y, 0.0);
        }
    LIGHT_LOOP_END
#endif

#if defined(_ADDITIONAL_LIGHTS_VERTEX)
    color += albedo * inputData.vertexLighting;
#endif

    // THE RIM: along the silhouette, on the side away from the key
    half rim = 1.0 - saturate(dot(N, V));
    half rimBand = smoothstep(0.5 - _BandSoft * 2.0, 0.5 + _BandSoft * 2.0, pow(max(rim, 1e-4), _RimPower));
    half rimSide = smoothstep(-0.2, 0.5, dot(N, -mainLight.direction));
    color += _RimColor.rgb * (_RimStrength * rimBand * rimSide);

    // THE GLOW
    color += _EmissionColor.rgb;

#if defined(MOMENTUM_TOON_TRANSPARENT)
    // Glass: what the pane mirrors, from the probes (the rooms' own, box-projected), most at a glancing angle
    half fresnel = pow(1.0 - saturate(dot(N, V)), 4.0);
    half3 R = reflect(-V, N);
    half3 refl = GlossyEnvironmentReflection(R, inputData.positionWS, 1.0 - mg.y, occlusion, inputData.normalizedScreenSpaceUV);
    color += refl * (mg.y * (0.12 + 0.55 * fresnel));
    alpha = saturate(alpha + fresnel * 0.35 * mg.y);
#else
    alpha = 1.0;
#endif

    color = MixFog(color, inputData.fogCoord);
    outColor = half4(color, alpha);

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#endif
