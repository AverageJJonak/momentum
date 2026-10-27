// MOMENTUM TOON (2026-10-16). The cel shader of the DARK DECO direction: the world painted on black paper, the light
// applied in flat bands, matte by default, one rim along the silhouette, the glows over everything. URP 17 Forward+.
// Passes: ForwardLit (the ramp), Outline (an inverted hull, ON only for rounded meshes: LevelKit.Rounded), ShadowCaster,
// DepthOnly, DepthNormals (SSAO reads it). The same property names and keyword sets as URP Lit, so LevelKit's materials
// carry over; LevelKit.Template routes the Lit templates here when WorldLook.Toon is on and this compiled
// (Shader.isSupported), and never otherwise. Fallback to Lit as a second net: a broken pass never turns the game pink.
// Check it in the editor: Momentum / Look / Check Toon Shader.
Shader "Momentum/Toon"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Colour", Color) = (1, 1, 1, 1)
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0
        _NormalStrength("Normal Strength (toon)", Range(0.0, 1.0)) = 0.35
        _MetallicGlossMap("Metallic (R) Smoothness (A)", 2D) = "white" {}
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
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
        _SpecSize("Specular cap size", Range(0.0, 1.0)) = 0.12
        _SpecStrength("Specular strength (0 = matte)", Range(0.0, 1.0)) = 0.0

        [Header(The rim)]
        _RimColor("Rim Colour", Color) = (0.6, 0.7, 1.0, 1)
        _RimPower("Rim Power", Range(0.5, 8.0)) = 3.0
        _RimStrength("Rim Strength", Range(0.0, 2.0)) = 0.6

        [Header(The black paper)]
        _AmbientCrush("Ambient kept", Range(0.0, 1.0)) = 0.8
        _AmbientFloor("Ambient floor", Range(0.0, 0.2)) = 0.10
        _PoolSteps("Lamp pool rings", Range(1.0, 6.0)) = 3.0
        _PoolTail("Lamp pool tail", Range(0.0, 1.0)) = 0.35
        _PoolLift("Lamp pool lift", Range(0.0, 1.0)) = 0.5

        [Header(The outline for rounded meshes only)]
        _Outline("Outline on", Float) = 0.0
        _OutlineWidth("Outline width (m)", Range(0.0, 0.1)) = 0.02
        _OutlineColor("Outline Colour", Color) = (0, 0, 0, 1)

        [HideInInspector] _Cull("Cull", Float) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 300

        // ------------------------------------------------------------------
        // The ramp. One pass for all the light, GI, emission and fog.
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex ToonVertex
            #pragma fragment ToonFragment

            // Material keywords: the LitFull set (VariantKeepAlive holds Resources/KeepAlive/ToonFull.mat with all three)
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _OCCLUSIONMAP

            // Universal Pipeline keywords, as the installed URP 17.5 Lit declares them
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN   // the sun's shadow map
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS                               // the lamps, per pixel
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX                                       // where the ambient SH is evaluated
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS                                         // the floods' shadows
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION                                           // the renderer's SSAO
            #pragma multi_compile_fragment _ _LIGHT_COOKIES                                                    // the striped cookies on the floods
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP                                                        // Forward+ (the 6.1 name of _FORWARD_PLUS)
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"        // multi_compile_fog, or the dynamic branch

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #include "MomentumToonPass.hlsl"
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // The outline: the mesh again, inflated along its normals, back faces only, black. Drawn in the forward pass
        // under URP's default unlit tag. OFF unless LevelKit.Rounded turns it on (_Outline 1 and the pass enabled):
        // a box's outline is the HARD LINE bead.
        Pass
        {
            Name "Outline"
            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment

            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

            #include "MomentumToonInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half   fogFactor  : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings OutlineVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Along the normal, pulled halfway toward the radial direction, so the hull of a hard-edged primitive
                // (a cylinder's rim, a capsule's seam) closes at the edge instead of splitting there. Transformed as a
                // normal (inverse transpose), so a stretched cylinder (a cable) still inflates outward.
                float3 radial = normalize(input.positionOS.xyz + float3(0.0, 1e-4, 0.0));
                float3 dirOS = normalize(normalize(input.normalOS) + radial);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 dirWS = TransformObjectToWorldNormal(dirOS);
                positionWS += dirWS * (_OutlineWidth * _Outline);

                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                half fogFactor = 0;
                #if !defined(_FOG_FRAGMENT)
                fogFactor = ComputeFogFactor(output.positionCS.z);
                #endif
                output.fogFactor = fogFactor;
                return output;
            }

            half4 OutlineFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half fog = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
                half3 c = MixFog(_OutlineColor.rgb, fog);
                return half4(c, 1.0);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // The shadow map. URP's own pass over this shader's input.
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
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW   // a flood's shadow (point/spot) against the sun's

            #include "MomentumToonInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // The depth prepass. URP's own pass.
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

        // ------------------------------------------------------------------
        // The normals texture (SSAO's source). URP's own Lit pass, with the normal map when the material has one.
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

    // The second net: if a pass of this fails to compile, Unity draws the material with Lit (the same property names)
    Fallback "Universal Render Pipeline/Lit"
}
