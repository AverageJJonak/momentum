// MOMENTUM TOON, TRANSPARENT (2026-10-16). The glass and water counterpart of Momentum/Toon: the same ramp, alpha
// blended, no depth write, a probe reflection on the pane, no outline. The blend and depth-write come from the same
// properties LevelKit sets on Lit's transparent template (_SrcBlend, _DstBlend, _ZWrite), so BreakableGlass, the water
// and the office glass carry over. LevelKit.Template routes LitTransparent / LitTransparentNormal here when
// WorldLook.Toon is on and this compiled. Fallback to Lit as a second net.
Shader "Momentum/ToonTransparent"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Colour", Color) = (1, 1, 1, 0.5)
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0
        _NormalStrength("Normal Strength (toon)", Range(0.0, 1.0)) = 0.6
        _MetallicGlossMap("Metallic (R) Smoothness (A)", 2D) = "white" {}
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.9
        _OcclusionMap("Occlusion (G)", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0
        _EmissionColor("Emission", Color) = (0, 0, 0, 1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [Header(The ramp)]
        _Bands("Bands (2 or 3)", Float) = 2
        _Band1("Mid band from (N.L)", Range(0.0, 1.0)) = 0.15
        _Band2("Lit band from (N.L)", Range(0.0, 1.0)) = 0.55
        _BandSoft("Band edge", Range(0.001, 0.2)) = 0.03
        _MidTone("Mid tone level", Range(0.0, 1.0)) = 0.45
        _ShadowTone("Shadow band (x the lit band)", Range(0.0, 1.0)) = 0.45

        [Header(The cap)]
        _SpecSize("Specular cap size", Range(0.0, 1.0)) = 0.1
        _SpecStrength("Specular strength", Range(0.0, 1.0)) = 0.25

        [Header(The rim)]
        _RimColor("Rim Colour", Color) = (0.6, 0.7, 1.0, 1)
        _RimPower("Rim Power", Range(0.5, 8.0)) = 3.0
        _RimStrength("Rim Strength", Range(0.0, 2.0)) = 0.35

        [Header(The black paper)]
        _AmbientCrush("Ambient kept", Range(0.0, 1.0)) = 0.8
        _AmbientFloor("Ambient floor", Range(0.0, 0.2)) = 0.10
        _PoolSteps("Lamp pool rings", Range(1.0, 6.0)) = 3.0
        _PoolTail("Lamp pool tail", Range(0.0, 1.0)) = 0.35
        _PoolLift("Lamp pool lift", Range(0.0, 1.0)) = 0.5

        [HideInInspector] _Outline("Outline on", Float) = 0.0
        [HideInInspector] _OutlineWidth("Outline width (m)", Range(0.0, 0.1)) = 0.0
        [HideInInspector] _OutlineColor("Outline Colour", Color) = (0, 0, 0, 1)

        // The blend state Lit's transparent materials are given (LevelKit sets these ints): alpha blend, no depth write
        [HideInInspector] _Surface("Surface", Float) = 1.0
        [HideInInspector] _Blend("Blend", Float) = 0.0
        [HideInInspector] _SrcBlend("Src", Float) = 5.0
        [HideInInspector] _DstBlend("Dst", Float) = 10.0
        [HideInInspector] _ZWrite("ZWrite", Float) = 0.0
        [HideInInspector] _Cull("Cull", Float) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex ToonVertex
            #pragma fragment ToonFragment

            // This pass is transparent: no SSAO on it, no screen-space shadow coord, alpha out (see MomentumToonPass.hlsl)
            #define MOMENTUM_TOON_TRANSPARENT 1
            #define _SURFACE_TYPE_TRANSPARENT 1

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _OCCLUSIONMAP

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING          // the rooms' probes, blended per pixel (the glass mirrors them)
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #include "MomentumToonPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "MomentumToonInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #pragma multi_compile_instancing

            #include "MomentumToonInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #pragma shader_feature_local _NORMALMAP

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #pragma multi_compile_instancing

            #include "MomentumToonInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Lit"
}
