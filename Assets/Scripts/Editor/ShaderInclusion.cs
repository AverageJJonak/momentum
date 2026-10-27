using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// THE SHADERS THE GAME ASKS FOR BY NAME, KEPT IN THE BUILD.
///
/// Nothing in MOMENTUM is an authored asset: every material is made at runtime
/// out of Shader.Find. Unity decides what to ship by looking at what the scenes
/// and Resources reference, and a shader nothing references is stripped — so in
/// a player build Shader.Find returns null, every `new Material(shader)` throws
/// ArgumentNullException, and the level is never built at all. It works in the
/// editor, where nothing is stripped, which is why it survives to the first
/// real build.
///
/// ONLY THE PIPELINE'S OWN SHADERS BELONG IN THAT LIST. Forcing Unity's
/// built-in shaders (Skybox/Procedural, Unlit/Color) into it drags the
/// editor-only asset file 'Library/unity default resources' into the player,
/// which fails the build with 'GUI/Text Shader is marked with
/// HideFlags.DontSave' and then 'Failed to write Resources/unity_builtin_extra'.
/// So the built-ins are listed under Remove below and actively stripped back
/// out, along with Momentum/WorldText, which needs no help: it already ships
/// because it lives in Resources/Shaders.
/// </summary>
public static class ShaderInclusion
{
    /// The pipeline shaders the game makes materials from. These are the ones that were being stripped.
    static readonly string[] Required =
    {
        "Universal Render Pipeline/Lit",              // the world: floors, walls, machines, bodies
        "Universal Render Pipeline/Unlit",            // the light: hardlight edges, neon, veins, orbs, sparks
        "Universal Render Pipeline/Particles/Unlit",  // smoke and steam
        "Momentum/Toon",                              // the cel look (Assets/Shaders): the world when WorldLook.Toon is on
        "Momentum/ToonTransparent",                   // its glass and water
        "Momentum/Sky",                               // the painted skies (WorldLook.Skybox)
        "Momentum/Sheet",                             // MaterialNet's stand-in for a see-through sheet whose own shader comes out magenta
        "Momentum/Particle",                          // the game's own particle shader (LevelKit.Particle): puffs, steam, embers, dust
    };

    /// The user's first check after a pull: did the toon shaders compile? Logs every message the compiler left, the pass
    /// count, and whether the pipeline's light cookies (the floods' stripes) are on — turning them on when they are not
    [MenuItem("Momentum/Look/Check Toon Shader", false, 72)]
    static void CheckToon()
    {
        foreach (string name in new[] { "Momentum/Toon", "Momentum/ToonTransparent", "Momentum/Sky" })
        {
            var s = Shader.Find(name);
            if (s == null) { Debug.LogError("Check Toon Shader: " + name + " NOT FOUND — is Assets/Shaders imported?"); continue; }
            var messages = ShaderUtil.GetShaderMessages(s);
            bool error = ShaderUtil.ShaderHasError(s);
            foreach (var m in messages)
            {
                string line = name + " (line " + m.line + ", " + m.platform + "): " + m.message;
                if (m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error) Debug.LogError(line);
                else Debug.LogWarning(line);
            }
            if (error) Debug.LogError("Check Toon Shader: " + name + " HAS ERRORS — the game draws with Lit (LevelKit.Template falls back) until it is fixed");
            else Debug.Log("Check Toon Shader: " + name + " OK — " + s.passCount + " passes, supported: " + s.isSupported
                + (messages.Length > 0 ? ", " + messages.Length + " message(s) above" : ""));
        }
        LightCookies();
    }

    /// The striped cookies on the floods need the pipeline's light cookies: on in both RP assets today; set if ever off
    static void LightCookies()
    {
        var rp = GraphicsSettings.currentRenderPipeline;
        if (rp == null) { Debug.LogWarning("Check Toon Shader: no render pipeline asset is set"); return; }
        var so = new SerializedObject(rp);
        var prop = so.FindProperty("m_SupportsLightCookies");
        if (prop == null) { Debug.LogWarning("Check Toon Shader: " + rp.name + " has no m_SupportsLightCookies"); return; }
        if (prop.boolValue) { Debug.Log("Check Toon Shader: light cookies are on in " + rp.name + " (the floods' stripes draw)"); return; }
        prop.boolValue = true;
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Debug.Log("Check Toon Shader: light cookies were OFF in " + rp.name + " — turned on (the floods' striped cookies need them)");
    }

    /// Never in the list. Each breaks the build, or is already covered, and each is safe to leave out:
    ///   Skybox/Procedural   built-in; WorldLook.Skybox already returns early when it is missing
    ///   Unlit/Color         built-in; asked for by no runtime code (the old editor preview used it)
    ///   Momentum/WorldText  ships anyway, as an asset under Resources/Shaders
    static readonly string[] Remove =
    {
        "Skybox/Procedural",
        "Unlit/Color",
        "Momentum/WorldText",
    };

    const string SettingsPath = "ProjectSettings/GraphicsSettings.asset";

    static int retries;

    // A build recompiles the scripts and reloads the domain, so this fires INSIDE the build. Touching the asset
    // database there trips 'Assertion failed: m_LockCount == 0'. So: never on the reload itself, and never while a
    // build, compile or import is in flight — wait for the editor to go quiet, then reconcile.
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

    [MenuItem("Momentum/Fix Always Included Shaders", false, 70)]
    static void FromMenu() => Ensure(true);

    static void Ensure(bool loud)
    {
        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(SettingsPath);
        if (asset == null)
        {
            if (loud) Debug.LogError("ShaderInclusion: could not open " + SettingsPath);
            return;
        }
        var so = new SerializedObject(asset);
        var list = so.FindProperty("m_AlwaysIncludedShaders");
        if (list == null || !list.isArray)
        {
            if (loud) Debug.LogError("ShaderInclusion: no m_AlwaysIncludedShaders on the graphics settings.");
            return;
        }

        // Strip the banned ones first, back to front so the indices hold
        var banned = new HashSet<UnityEngine.Object>();
        foreach (string name in Remove)
        {
            var s = Shader.Find(name);
            if (s != null) banned.Add(s);
        }
        var dropped = new List<string>();
        for (int i = list.arraySize - 1; i >= 0; i--)
        {
            var v = list.GetArrayElementAtIndex(i).objectReferenceValue;
            if (v == null || !banned.Contains(v)) continue;
            dropped.Add(v.name);
            list.DeleteArrayElementAtIndex(i);
        }

        // Then add anything required that is not already there
        var have = new HashSet<UnityEngine.Object>();
        for (int i = 0; i < list.arraySize; i++)
        {
            var v = list.GetArrayElementAtIndex(i).objectReferenceValue;
            if (v != null) have.Add(v);
        }
        var added = new List<string>();
        var missing = new List<string>();
        foreach (string name in Required)
        {
            var shader = Shader.Find(name);
            if (shader == null) { missing.Add(name); continue; }
            if (!have.Add(shader)) continue;
            int at = list.arraySize;
            list.InsertArrayElementAtIndex(at);
            list.GetArrayElementAtIndex(at).objectReferenceValue = shader;
            added.Add(name);
        }

        if (added.Count > 0 || dropped.Count > 0)
        {
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            if (added.Count > 0) Debug.Log("ShaderInclusion: added " + string.Join(", ", added));
            if (dropped.Count > 0) Debug.Log("ShaderInclusion: removed " + string.Join(", ", dropped));
        }
        else if (loud)
        {
            Debug.Log("ShaderInclusion: the list is already correct (" + Required.Length + " pipeline shaders).");
        }
        if (missing.Count > 0)
            Debug.LogWarning("ShaderInclusion: the editor itself cannot find — " + string.Join(", ", missing));
    }
}
