using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// THE MATERIAL NET (2026-10-21, user: "missing textures is happening in a lot of places"). Magenta is Unity's error
/// shader: a material whose shader is missing, failed to compile, or cannot run here. After a level is built (the game
/// and the Level Editor alike) every renderer in the scene is checked, and any material Unity cannot draw is swapped
/// for a stand-in in the same colour and base map, so nothing is ever pink. One line in the console says what was
/// wrong and how often, with the shader's own error text in the editor: the diagnosis.
///
/// THE DRAW TEST (2026-10-23, user: "the smog is still magenta"). A shader can answer every question (present,
/// supported, no error logged) and still be drawn by the error shader on one machine: a variant, or the driver, is the
/// problem, and nothing says so. So each distinct material (shader + keywords + blend state) is DRAWN ALONE, once, into
/// an 8×8 target, and the pixel is read back: pure magenta means it cannot draw here, whatever it claims. A stand-in is
/// built on the next shader that can (tested the same way): for a see-through sheet a particle unlit or Momentum/Sheet,
/// for a solid the Lit; and if nothing on this machine draws it, the renderer is hidden, which beats a magenta wall.
/// </summary>
public static class MaterialNet
{
    /// The editor fills this with ShaderUtil.ShaderHasError (and the messages); a build has no such thing and relies on
    /// null / isSupported and the draw test
    public static Func<Shader, string> EditorError;

    /// Off: trust the questions alone (batch mode, the offline harness)
    public static bool DrawTest = true;

    static Shader lit;
    static readonly Dictionary<Material, Material> swaps = new Dictionary<Material, Material>();

    /// Checks everything under `roots` (null = the whole scene); returns how many renderers were rescued
    public static int Run(IList<GameObject> roots = null)
    {
        ResetDebugDisplay();
        var broken = new Dictionary<string, int>();
        string why = null;
        int rescued = 0, hidden = 0;
        IList<Renderer> renderers = roots == null ? (IList<Renderer>)UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include) : Gather(roots);
        foreach (var r in renderers)
        {
            if (r == null) continue;
            var mats = r.sharedMaterials;
            bool changed = false, hide = false;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                string problem = m == null ? "the slot is empty (the material was destroyed, or never set)" : Problem(m);
                if (problem == null) continue;
                string key = (m == null ? "no material" : m.shader != null ? m.shader.name + " [" + string.Join(" ", m.shaderKeywords) + "]" : "no shader") + " — " + problem;
                broken.TryGetValue(key, out int n);
                broken[key] = n + 1;
                if (why == null && EditorError != null && m != null && m.shader != null) why = EditorError(m.shader);
                var stand = m == null ? Blank() : Replacement(m);
                if (stand == null) { hide = true; continue; }
                mats[i] = stand;
                changed = true;
            }
            if (changed) { r.sharedMaterials = mats; rescued++; }
            if (hide) { r.enabled = false; hidden++; }
        }
        Teardown();
        if (broken.Count > 0)
        {
            var sb = new System.Text.StringBuilder("MaterialNet: " + rescued + " renderer(s) could not draw and now wear a stand-in"
                + (hidden > 0 ? ", " + hidden + " hidden because nothing on this machine draws them" : "") + " —");
            foreach (var kv in broken) sb.Append(" [" + kv.Key + " × " + kv.Value + "]");
            sb.Append("\n" + Device());
            if (!string.IsNullOrEmpty(why)) sb.Append("\n" + why);
            Debug.LogWarning(sb.ToString());
        }
        return rescued;
    }

    static List<Renderer> Gather(IList<GameObject> roots)
    {
        var list = new List<Renderer>();
        foreach (var g in roots) if (g != null) list.AddRange(g.GetComponentsInChildren<Renderer>(true));
        return list;
    }

    public static string Device() => SystemInfo.graphicsDeviceType + " · " + SystemInfo.graphicsDeviceName + " · driver " + SystemInfo.graphicsDeviceVersion;

    /// THE RENDERING DEBUGGER (2026-10-25, user: "notice the smog and airship + lights are still magenta"): its debug
    /// views are global, they apply to the game's camera as much as the scene's, and they paint every surface on a
    /// STOCK shader (Lit, Unlit, Particles: the city, the sheets, the beams) while the game's own shaders ignore them;
    /// the mipmap-streaming view in particular washes everything with a texture made at runtime in magenta. So any
    /// view left on is named in the console and switched off before the net looks at a single material
    static void ResetDebugDisplay()
    {
        var d = UniversalRenderPipelineDebugDisplaySettings.Instance;
        if (d == null || !d.AreAnySettingsActive) return;
        var sb = new System.Text.StringBuilder();
        var r = d.renderingSettings; var m = d.materialSettings; var l = d.lightingSettings;
        if (r != null)
        {
            if (r.mipInfoMode != DebugMipInfoMode.None) sb.Append(" mipmap streaming view = " + r.mipInfoMode + ";");
            if (r.wireframeMode != DebugWireframeMode.None) sb.Append(" wireframe = " + r.wireframeMode + ";");
            if (r.overdrawMode != DebugOverdrawMode.None) sb.Append(" overdraw = " + r.overdrawMode + ";");
            if (r.validationMode != DebugValidationMode.None) sb.Append(" validation = " + r.validationMode + ";");
            if (r.fullScreenDebugMode != DebugFullScreenMode.None) sb.Append(" full-screen view = " + r.fullScreenDebugMode + ";");
        }
        if (m != null && m.materialDebugMode != DebugMaterialMode.None) sb.Append(" material view = " + m.materialDebugMode + ";");
        if (l != null && l.lightingDebugMode != DebugLightingMode.None) sb.Append(" lighting view = " + l.lightingDebugMode + ";");
        d.Reset();
        var manager = DebugManager.instance;
        if (manager != null) { manager.displayRuntimeUI = false; manager.displayPersistentRuntimeUI = false; }
        Debug.LogWarning("MaterialNet: the Rendering Debugger had a view switched on (" + (sb.Length > 0 ? sb.ToString().Trim().TrimEnd(';') : "a setting") + "). Its views paint every surface on a stock shader, in the game as in the scene view, and the mipmap view paints them magenta. Reset. (Window → Analysis → Rendering Debugger is where it lives.)");
    }

    /// Why this material cannot draw, or null when it can
    static string Problem(Material m)
    {
        var s = m.shader;
        if (s == null) return "the material has no shader";
        if (s.name == "Hidden/InternalErrorShader") return "the error shader";
        if (!s.isSupported) return "the shader is not supported here (it failed to compile, or has no usable pass)";
        if (EditorError != null)
        {
            string e = EditorError(s);
            if (!string.IsNullOrEmpty(e)) return "the shader has compile errors";
        }
        if (DrawTest && DrawsPink(m)) return "drawn alone it comes out as the error shader's magenta (this variant does not run on this machine)";
        return null;
    }

    // ---------------------------------------------------------------- the draw test

    static RenderTexture probe;
    static Texture2D readback;
    static GameObject probeRig;
    static Camera probeCam;
    static MeshRenderer probeQuad;
    static bool blind;   // the rig cannot see here (no render request, or the calibration failed): trust the questions
    static readonly Dictionary<string, bool> pinkByKey = new Dictionary<string, bool>();

    /// True when `m`, on a quad drawn by a camera through the pipeline itself (the path the scene and game views
    /// take, batcher and all), comes out magenta. One draw per distinct shader + keyword set + blend state; the
    /// answer is kept for the session
    public static bool DrawsPink(Material m)
    {
        if (m == null || m.shader == null || Application.isBatchMode) return false;   // batch mode has no screen to draw on
        string key = Key(m);
        if (pinkByKey.TryGetValue(key, out bool pink)) return pink;
        pink = false;
        try
        {
            if (Rig()) pink = IsPink(Shoot(m));
        }
        catch (Exception e)
        {
            Debug.LogWarning("MaterialNet: the draw test could not run (" + e.Message + "); trusting the shaders' word.");
            blind = true;
        }
        pinkByKey[key] = pink;
        return pink;
    }

    static bool IsPink(Color c) => c.r > 0.85f && c.g < 0.2f && c.b > 0.85f;

    /// The rig, built once per run: a camera with a hidden layer to itself, a quad filling its view, an 8×8 target.
    /// Calibrated before its first word: a plain unlit red must come back red and the error shader must come back
    /// magenta, or the rig is blind here and says so instead of vouching for anything
    static bool Rig()
    {
        if (probeRig != null) return !blind;
        int layer = FreeLayer();
        probeRig = new GameObject("MaterialNet probe") { hideFlags = HideFlags.HideAndDontSave };
        probeRig.transform.position = new Vector3(0f, -90000f, 0f);   // where no camera looks
        var camGo = new GameObject("Camera") { hideFlags = HideFlags.HideAndDontSave };
        camGo.transform.SetParent(probeRig.transform, false);
        probeCam = camGo.AddComponent<Camera>();
        probeCam.enabled = false;   // draws only when asked
        probeCam.orthographic = true;
        probeCam.orthographicSize = 1f;
        probeCam.nearClipPlane = 0.1f;
        probeCam.farClipPlane = 10f;
        probeCam.clearFlags = CameraClearFlags.SolidColor;
        probeCam.backgroundColor = Color.black;
        probeCam.cullingMask = 1 << layer;
        probeCam.allowHDR = false;
        probeCam.allowMSAA = false;
        var data = probeCam.GetUniversalAdditionalCameraData();
        if (data != null)
        {
            data.renderPostProcessing = false;
            data.renderShadows = false;
            data.antialiasing = AntialiasingMode.None;
            data.requiresColorOption = CameraOverrideOption.Off;
            data.requiresDepthOption = CameraOverrideOption.Off;
        }
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Quad";
        quad.hideFlags = HideFlags.HideAndDontSave;
        var col = quad.GetComponent<Collider>();
        if (col != null) UnityEngine.Object.DestroyImmediate(col);
        quad.layer = layer;
        quad.transform.SetParent(probeRig.transform, false);
        quad.transform.localPosition = new Vector3(0f, 0f, 2f);
        quad.transform.localScale = Vector3.one * 4f;
        probeQuad = quad.GetComponent<MeshRenderer>();
        probeQuad.shadowCastingMode = ShadowCastingMode.Off;   // drawn as the sheets and the airships are
        probeQuad.receiveShadows = false;
        probeQuad.lightProbeUsage = LightProbeUsage.Off;
        probeQuad.reflectionProbeUsage = ReflectionProbeUsage.Off;
        probe = new RenderTexture(8, 8, 16, RenderTextureFormat.ARGB32) { name = "MaterialNet probe", hideFlags = HideFlags.HideAndDontSave };
        probe.Create();
        readback = new Texture2D(8, 8, TextureFormat.RGBA32, false) { name = "MaterialNet readback", hideFlags = HideFlags.HideAndDontSave };
        var red = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "probe red" };
        red.SetColor("_BaseColor", Color.red);
        var error = new Material(Shader.Find("Hidden/InternalErrorShader")) { name = "probe error" };
        Color cr = Shoot(red), ce = Shoot(error);
        // The pipeline's DEBUG DISPLAY variant: the Rendering Debugger and the Scene view's debug draw modes draw the
        // stock shaders with it, the game never does. Drawn plain, outside the pipeline, with the keyword forced on
        Color cd = new Color(-1f, -1f, -1f, -1f);
        try { Shader.EnableKeyword("DEBUG_DISPLAY"); cd = BlitShoot(red); }
        catch (Exception) { }
        finally { Shader.DisableKeyword("DEBUG_DISPLAY"); }
        DebugVariantBroken = IsPink(cd);
        UnityEngine.Object.DestroyImmediate(red);
        UnityEngine.Object.DestroyImmediate(error);
        bool redOk = cr.r > 0.5f && cr.g < 0.3f && cr.b < 0.3f;
        blind = !(redOk && IsPink(ce));
        string debugWord = DebugVariantBroken
            ? "the stock Unlit's DEBUG DISPLAY variant comes out MAGENTA (" + cd + "): anything on a stock shader goes pink whenever the Rendering Debugger or a Scene view debug draw mode is on; the game itself never uses that variant"
            : "the stock Unlit's DEBUG DISPLAY variant draws (" + cd + ")";
        if (blind) Debug.LogWarning("MaterialNet: the draw test is blind here (a plain red came back " + cr + ", the error shader " + ce + "); trusting the shaders' word. " + debugWord + ". " + Device());
        else Debug.Log("MaterialNet: the draw test sees (red " + cr + ", error " + ce + "); " + debugWord + ". " + Device());
        return !blind;
    }

    /// True once the calibration has seen the pipeline's debug variant fail here
    public static bool DebugVariantBroken { get; private set; }

    /// `m` drawn plain into the target, outside the pipeline: the global keywords as they stand apply
    static Color BlitShoot(Material m)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = probe;
        GL.Clear(true, true, Color.black);
        Graphics.Blit(Texture2D.whiteTexture, probe, m, 0);
        RenderTexture.active = probe;
        readback.ReadPixels(new Rect(0f, 0f, 8f, 8f), 0, 0, false);
        readback.Apply(false);
        RenderTexture.active = prev;
        return readback.GetPixel(4, 4);
    }

    /// `m` on the quad, the camera drawn through the pipeline into the target, the middle pixel read back
    static Color Shoot(Material m)
    {
        probeQuad.sharedMaterial = m;
        var request = new RenderPipeline.StandardRequest { destination = probe };
        if (!RenderPipeline.SupportsRenderRequest(probeCam, request)) throw new InvalidOperationException("the pipeline takes no render request");
        RenderPipeline.SubmitRenderRequest(probeCam, request);
        var prev = RenderTexture.active;
        RenderTexture.active = probe;
        readback.ReadPixels(new Rect(0f, 0f, 8f, 8f), 0, 0, false);
        readback.Apply(false);
        RenderTexture.active = prev;
        return readback.GetPixel(4, 4);
    }

    /// The rig goes when the run is over: the answers stay
    static void Teardown()
    {
        if (probeRig != null) UnityEngine.Object.DestroyImmediate(probeRig);
        if (probe != null) { probe.Release(); UnityEngine.Object.DestroyImmediate(probe); }
        if (readback != null) UnityEngine.Object.DestroyImmediate(readback);
        probeRig = null; probeCam = null; probeQuad = null; probe = null; readback = null;
    }

    /// The highest layer with no name: nothing else draws on it
    static int FreeLayer()
    {
        for (int i = 31; i >= 8; i--) if (string.IsNullOrEmpty(LayerMask.LayerToName(i))) return i;
        return 31;
    }

    static string Key(Material m)
    {
        var kw = (string[])m.shaderKeywords.Clone();
        Array.Sort(kw, StringComparer.Ordinal);
        return m.shader.name + "|" + string.Join(" ", kw) + "|" + I(m, "_SrcBlend", -1) + "/" + I(m, "_DstBlend", -1) + "/" + I(m, "_ZWrite", -1) + "/" + I(m, "_Cull", -1) + "/" + (m.renderQueue >= (int)RenderQueue.Transparent ? "T" : "O");
    }

    static int I(Material m, string prop, int fallback) => m.HasProperty(prop) ? Mathf.RoundToInt(m.GetFloat(prop)) : fallback;

    // ---------------------------------------------------------------- the stand-ins

    /// A material that draws here, in the broken one's colour and base map (cached per broken material); null when no
    /// shader on this machine draws it
    static Material Replacement(Material broken)
    {
        if (swaps.TryGetValue(broken, out var done)) return done;
        bool transparent = broken.renderQueue >= (int)RenderQueue.Transparent || (broken.HasProperty("_Surface") && broken.GetFloat("_Surface") > 0.5f);
        string[] shaders = transparent
            ? new[] { "Universal Render Pipeline/Particles/Unlit", "Momentum/Sheet", "Momentum/ToonTransparent", "Universal Render Pipeline/Unlit" }
            : new[] { "Universal Render Pipeline/Lit", "Momentum/Toon", "Universal Render Pipeline/Unlit", "Momentum/Sheet" };
        Material m = null;
        foreach (string name in shaders)
        {
            var s = Shader.Find(name);
            if (s == null || !s.isSupported) continue;
            m = StandIn(s, broken, transparent);
            if (!DrawTest || !DrawsPink(m)) break;
            UnityEngine.Object.DestroyImmediate(m);
            m = null;
        }
        swaps[broken] = m;
        return m;
    }

    static Material StandIn(Shader shader, Material broken, bool transparent)
    {
        var m = new Material(shader) { name = broken.name + " (net)" };
        Color c = broken.HasProperty("_BaseColor") ? broken.GetColor("_BaseColor") : broken.HasProperty("_Color") ? broken.GetColor("_Color") : Color.white;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        else if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (broken.HasProperty("_BaseMap") && m.HasProperty("_BaseMap") && broken.GetTexture("_BaseMap") != null)
        {
            m.SetTexture("_BaseMap", broken.GetTexture("_BaseMap"));
            m.SetTextureScale("_BaseMap", broken.GetTextureScale("_BaseMap"));
            m.SetTextureOffset("_BaseMap", broken.GetTextureOffset("_BaseMap"));
        }
        if (!transparent && broken.HasProperty("_EmissionColor") && m.HasProperty("_EmissionColor"))
        {
            var e = broken.GetColor("_EmissionColor");
            if (e.maxColorComponent > 0.01f) { m.SetColor("_EmissionColor", e); m.EnableKeyword("_EMISSION"); }
        }
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", broken.HasProperty("_Smoothness") ? broken.GetFloat("_Smoothness") : 0.2f);
        if (transparent)
        {
            int src = I(broken, "_SrcBlend", (int)BlendMode.SrcAlpha), dst = I(broken, "_DstBlend", (int)BlendMode.OneMinusSrcAlpha), cull = I(broken, "_Cull", (int)CullMode.Back);
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", src == (int)BlendMode.One && dst == (int)BlendMode.One ? 2f : 0f);
            if (m.HasProperty("_SrcBlend")) m.SetInt("_SrcBlend", src);
            if (m.HasProperty("_DstBlend")) m.SetInt("_DstBlend", dst);
            if (m.HasProperty("_ZWrite")) m.SetInt("_ZWrite", 0);
            if (m.HasProperty("_Cull")) m.SetInt("_Cull", cull);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        if (LevelKit.IsToon(m)) { m.SetFloat("_Outline", 0f); m.SetShaderPassEnabled("SRPDefaultUnlit", false); }   // a box's toon: no hull outline
        m.renderQueue = broken.renderQueue;
        return m;
    }

    static Material blank;
    /// What an empty slot draws instead of pink: plain lit grey, one for all
    static Material Blank()
    {
        if (blank != null) return blank;
        if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit");
        blank = new Material(lit) { name = "(empty slot: net)" };
        blank.SetColor("_BaseColor", new Color(0.5f, 0.5f, 0.5f));
        return blank;
    }

    /// A new level: the swaps belong to the last one's materials
    public static void Reset() => swaps.Clear();
}
