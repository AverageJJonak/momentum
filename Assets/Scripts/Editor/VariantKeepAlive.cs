using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// THE VARIANTS THE GAME MAKES AT RUNTIME, KEPT IN THE BUILD.
///
/// Sibling problem to ShaderInclusion, and a nastier one. That list keeps the
/// SHADER in the player; this keeps its VARIANTS. Unity strips a
/// `shader_feature` variant unless some MATERIAL ASSET in the build uses it —
/// and MOMENTUM has no material assets at all, because every material in the
/// game is `new Material(Shader.Find(...))` at runtime. So the shader ships,
/// the keyword does not, and the player silently runs the wrong variant.
///
/// What that looked like: `_SURFACE_TYPE_TRANSPARENT` was stripped, so URP's
/// OutputAlpha() took the opaque branch and returned 1.0 instead of the
/// material's alpha. Every transparent thing in the game — the civic windows,
/// the pod glass in the opening, the breakable panes, holo signs, smoke —
/// rendered as a SOLID panel in a build while looking right in the editor, with
/// nothing in the player log. `_NORMALMAP` went the same way, flattening every
/// textured surface.
///
/// The cure is to give the build the material assets it is looking for. These
/// are never instantiated or drawn; they exist so the stripper sees a material
/// using each keyword set and keeps it. They live under Resources so they ship
/// unconditionally. Add a new EnableKeyword anywhere in the game, add it here.
/// </summary>
public static class VariantKeepAlive
{
    const string Root = "Assets/Resources";
    const string Folder = Root + "/KeepAlive";

    /// One material asset per keyword set the game builds at runtime
    class Keep
    {
        public string file, shader;
        public string[] keywords;
        public bool transparent;
    }

    static readonly Keep[] Wanted =
    {
        // The transparent Lit: window glass, breakable panes, pod glass, holo, water, the city's glass
        new Keep { file = "LitTransparent", shader = "Universal Render Pipeline/Lit",
                   keywords = new[] { "_SURFACE_TYPE_TRANSPARENT" }, transparent = true },
        // The rippled transparent Lit: water (LevelKit.WaterSurface)
        new Keep { file = "LitTransparentNormal", shader = "Universal Render Pipeline/Lit",
                   keywords = new[] { "_SURFACE_TYPE_TRANSPARENT", "_NORMALMAP" }, transparent = true },
        // The textured Lit: every floor, wall and machine surface LevelKitTextures builds
        new Keep { file = "LitNormalMap", shader = "Universal Render Pipeline/Lit",
                   keywords = new[] { "_NORMALMAP" }, transparent = false },
        // The full textured Lit: normal + metallic/smoothness + occlusion maps, every LevelKit.Surface material since the overhaul
        new Keep { file = "LitFull", shader = "Universal Render Pipeline/Lit",
                   keywords = new[] { "_NORMALMAP", "_METALLICSPECGLOSSMAP", "_OCCLUSIONMAP" }, transparent = false },
        // The transparent Unlit: smoke, fire, the ending's haze
        new Keep { file = "UnlitTransparent", shader = "Universal Render Pipeline/Unlit",
                   keywords = new[] { "_SURFACE_TYPE_TRANSPARENT" }, transparent = true },
        // The transparent Particles/Unlit: steam, embers, dust — LevelKitAtmosphere.Particle
        new Keep { file = "ParticlesTransparent", shader = "Universal Render Pipeline/Particles/Unlit",
                   keywords = new[] { "_SURFACE_TYPE_TRANSPARENT" }, transparent = true },
        // The toon twins (2026-10-16): the same keyword sets on Momentum/Toon and Momentum/ToonTransparent (Assets/Shaders),
        // so the cel look's variants ship; LevelKit.Template resolves the Lit names to these when WorldLook.Toon is on
        new Keep { file = "Toon", shader = "Momentum/Toon", keywords = new string[0], transparent = false },
        new Keep { file = "ToonFull", shader = "Momentum/Toon",
                   keywords = new[] { "_NORMALMAP", "_METALLICSPECGLOSSMAP", "_OCCLUSIONMAP" }, transparent = false },
        new Keep { file = "ToonTransparent", shader = "Momentum/ToonTransparent", keywords = new string[0], transparent = true },
        new Keep { file = "ToonTransparentNormal", shader = "Momentum/ToonTransparent",
                   keywords = new[] { "_NORMALMAP" }, transparent = true },
    };

    static int retries;

    // Same rule as ShaderInclusion: a build recompiles and reloads the domain, so this fires INSIDE the build, and
    // writing assets there trips 'Assertion failed: m_LockCount == 0' and the player fails to write its resources.
    // Wait for the editor to be genuinely idle.
    [InitializeOnLoadMethod]
    static void OnLoad()
    {
        retries = 0;
        EditorApplication.delayCall += Later;
    }

    static void Later()
    {
        if (BuildPipeline.isBuildingPlayer || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            if (retries++ < 200) EditorApplication.delayCall += Later;
            return;
        }
        Ensure(false);
    }

    [MenuItem("Momentum/Fix Shader Variants", false, 71)]
    static void FromMenu() => Ensure(true);

    static void Ensure(bool loud)
    {
        if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(Root, "KeepAlive");

        var made = new List<string>();
        var missing = new List<string>();
        foreach (var k in Wanted)
        {
            var shader = Shader.Find(k.shader);
            if (shader == null) { missing.Add(k.shader); continue; }
            string path = Folder + "/" + k.file + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool fresh = mat == null;
            if (fresh) mat = new Material(shader);
            else if (mat.shader != shader) mat.shader = shader;

            if (!fresh && Correct(mat, k)) continue;   // already right: write nothing
            Apply(mat, k);
            if (fresh) AssetDatabase.CreateAsset(mat, path);
            else EditorUtility.SetDirty(mat);
            made.Add(k.file);
        }

        if (made.Count > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log("VariantKeepAlive: wrote keyword-holder materials — " + string.Join(", ", made)
                + ". These are never drawn; they keep their shader variants out of the stripper.");
        }
        else if (loud)
        {
            Debug.Log("VariantKeepAlive: all " + Wanted.Length + " keyword holders already present and correct.");
        }
        if (missing.Count > 0)
            Debug.LogWarning("VariantKeepAlive: the editor cannot find — " + string.Join(", ", missing));
    }

    static bool Correct(Material m, Keep k)
    {
        foreach (string kw in k.keywords) if (!m.IsKeywordEnabled(kw)) return false;
        return true;
    }

    /// The same recipe the runtime uses, so the keyword set the stripper sees is the keyword set the game asks for
    static void Apply(Material m, Keep k)
    {
        if (k.transparent)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.renderQueue = (int)RenderQueue.Transparent;
        }
        foreach (string kw in k.keywords) m.EnableKeyword(kw);
    }
}
