using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// THE PLAY SESSION PROBE (2026-10-28, the gym's magenta ground and skyline). The validator builds a level in edit mode
/// and sees no magenta; the user's play session does. So this plays the game the way the user does: Play from the main
/// menu, a few seconds in the menu, the BEAT GYM entry's scene load, a few seconds in the gym, and then it reads the
/// frame the player's own camera draws (the stack, the post, the HDR: all of it), lists every renderer that exists in
/// the session (hidden and persistent ones included, with the scene each belongs to), and if the frame has magenta in
/// it, switches groups of renderers off one at a time until the magenta goes: the culprit, named.
/// Momentum → Probe Play Session (gym), or batch: -executeMethod PlaySessionProbe.Run (no -quit: it exits itself).
/// Output: Docs/Design/validation/PlaySession-menu.png, PlaySession-gym.png, PlaySession.txt.
/// </summary>
[InitializeOnLoad]
public static class PlaySessionProbe
{
    const string Flag = "Momentum.PlaySessionProbe";
    public const string OutDir = "Docs/Design/validation";

    static PlaySessionProbe() => EditorApplication.playModeStateChanged += OnPlayMode;

    [MenuItem("Momentum/Probe Play Session (gym)", false, 2)]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("PlaySessionProbe: stop play first."); return; }
        SessionState.SetBool(Flag, true);
        EditorApplication.EnterPlaymode();   // from the main menu, as the game boots (PlayFromMenu)
    }

    /// The user's editor as it was: the practice range open and the Level Editor's working copy of it standing in the
    /// scene (the editor builds one for the open scene on start), then Play. Batch: -executeMethod PlaySessionProbe.RunAsUser
    public static void RunAsUser()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/PracticeRange.unity", OpenSceneMode.Single);
        var config = AssetDatabase.LoadAssetAtPath<MovementConfig>("Assets/Config/MovementConfig.asset");
        LevelEditor.Generate(LevelKind.PracticeRange, config, true);
        Debug.Log("PlaySessionProbe: the range's working copy is up (" + (LevelEditor.Holder() != null ? "holder found" : "NO HOLDER") + "); entering play");
        Run();
    }

    static void OnPlayMode(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Flag, false)) return;
        SessionState.SetBool(Flag, false);
        var go = new GameObject("PlaySessionProbe");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<PlaySessionProbeRunner>();
    }
}

public class PlaySessionProbeRunner : MonoBehaviour
{
    const int W = 1600, H = 900;
    readonly StringBuilder log = new StringBuilder();

    IEnumerator Start()
    {
        Directory.CreateDirectory(PlaySessionProbe.OutDir);
        Line("PlaySessionProbe  " + SystemInfo.graphicsDeviceType + " · " + SystemInfo.graphicsDeviceName + " · " + SystemInfo.graphicsDeviceVersion
            + " · batch " + Application.isBatchMode + " · quality '" + QualitySettings.names[QualitySettings.GetQualityLevel()] + "' · pipeline " + (GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.name : "NONE"));
        yield return new WaitForSecondsRealtime(4f);
        Capture("menu");
        Dump("menu");

        // the gym, the way the menu's BEAT GYM entry loads it
        string scene = "BeatGym";
        if (Application.CanStreamedLevelBeLoaded(scene)) { Line("load: SceneManager.LoadScene(" + scene + ")"); SceneManager.LoadScene(scene); }
        else { Line("load: not in the build list, LoadSceneInPlayMode by path"); EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/BeatGym.unity", new LoadSceneParameters(LoadSceneMode.Single)); }
        yield return null; yield return null;
        yield return new WaitForSecondsRealtime(6f);
        float pink = Capture("gym");
        Dump("gym");
        if (pink > 0.05f) yield return Bisect();
        else Line("no magenta in the gym frame: nothing to bisect");

        string path = Path.Combine(PlaySessionProbe.OutDir, "PlaySession.txt");
        File.WriteAllText(path, log.ToString());
        Debug.Log("PlaySessionProbe: done → " + path);
        if (Application.isBatchMode) EditorApplication.Exit(0);
        else EditorApplication.isPlaying = false;
    }

    void Line(string s) { log.AppendLine(s); Debug.Log("PlaySessionProbe: " + s); }

    // ---------------------------------------------------------------- the frame

    /// The player's camera's own frame, into a texture; the share of error magenta; saved as a PNG
    float Capture(string name, bool save = true)
    {
        var cam = Camera.main;
        if (cam == null) { Line(name + ": no main camera"); return 0f; }
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32); rt.Create();
        var keep = cam.targetTexture;
        float pct = 0f;
        try
        {
            cam.targetTexture = rt;
            bool drawn = false;
            try { if (RenderPipeline.SupportsRenderRequest(cam, new RenderPipeline.StandardRequest())) { RenderPipeline.SubmitRenderRequest(cam, new RenderPipeline.StandardRequest { destination = rt }); drawn = true; } } catch (System.Exception e) { Line("render request failed: " + e.Message); }
            if (!drawn) cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
            RenderTexture.active = prev;
            var px = tex.GetPixels32();
            int pink = 0;
            foreach (var c in px) if (c.r > 140 && c.b > 120 && c.g < 90 && c.r - c.g > 80 && c.b - c.g > 60) pink++;
            pct = 100f * pink / px.Length;
            if (save) File.WriteAllBytes(Path.Combine(PlaySessionProbe.OutDir, "PlaySession-" + name + ".png"), tex.EncodeToPNG());
            Destroy(tex);
        }
        finally { cam.targetTexture = keep; rt.Release(); Destroy(rt); }
        if (save) Line(name + ": magenta " + pct.ToString("0.00") + "% of the player camera's frame  (camera '" + cam.name + "' at " + cam.transform.position.ToString("0.0") + ", HDR " + cam.allowHDR + ", post " + (cam.GetUniversalAdditionalCameraData() != null && cam.GetUniversalAdditionalCameraData().renderPostProcessing) + ", stack " + (cam.GetUniversalAdditionalCameraData() != null && cam.GetUniversalAdditionalCameraData().cameraStack != null ? cam.GetUniversalAdditionalCameraData().cameraStack.Count : 0) + ")");
        return pct;
    }

    // ---------------------------------------------------------------- the census of what exists

    /// Every renderer in the session, hidden and persistent ones included: where it lives, what it draws with
    void Dump(string name)
    {
        var all = Resources.FindObjectsOfTypeAll<Renderer>();
        var seen = new HashSet<Renderer>(FindObjectsByType<Renderer>(FindObjectsInactive.Include));   // what the game's own sweeps (the lint, the net, the census) can see
        int unseen = 0;
        var byScene = new Dictionary<string, int>();
        var byShader = new Dictionary<string, int>();
        var lines = new List<string>();
        int drawn = 0;
        foreach (var r in all)
        {
            if (r == null || EditorUtility.IsPersistent(r)) continue;   // prefabs and assets are not in the picture
            bool live = r.enabled && r.gameObject.activeInHierarchy;
            if (!live) continue;
            drawn++;
            string sc = r.gameObject.scene.IsValid() ? r.gameObject.scene.name + (r.gameObject.scene.isLoaded ? "" : " (NOT LOADED)") : "NO SCENE";
            if (!seen.Contains(r)) { unseen++; sc += " UNSEEN-BY-FIND"; }
            byScene.TryGetValue(sc, out int n); byScene[sc] = n + 1;
            var mats = r.sharedMaterials;
            var ms = new StringBuilder();
            foreach (var m in mats)
            {
                string key = m == null ? "EMPTY SLOT" : (m.shader != null ? m.shader.name : "NO SHADER");
                byShader.TryGetValue(key, out int k); byShader[key] = k + 1;
                ms.Append(m == null ? "EMPTY" : "'" + m.name + "'@" + key + (m.shader != null && !m.shader.isSupported ? " UNSUPPORTED" : "") + (m.shader != null && ShaderUtil.ShaderHasError(m.shader) ? " HAS-ERROR" : "")).Append("; ");
            }
            var b = r.bounds;
            lines.Add(sc + " | " + PathOf(r.transform) + " | flags " + r.gameObject.hideFlags + " | " + r.GetType().Name + " | centre " + b.center.ToString("0") + " size " + b.size.ToString("0") + " | " + ms);
        }
        Line(name + ": " + drawn + " live renderers, " + unseen + " of them invisible to FindObjectsByType");
        foreach (var kv in byScene) Line("   scene '" + kv.Key + "': " + kv.Value);
        var shaders = new List<KeyValuePair<string, int>>(byShader); shaders.Sort((a, c) => c.Value.CompareTo(a.Value));
        foreach (var kv in shaders)
        {
            var s = Shader.Find(kv.Key);
            Line("   shader " + kv.Key + ": " + kv.Value + (s == null ? " (not found by name)" : (s.isSupported ? "" : " UNSUPPORTED") + (ShaderUtil.ShaderHasError(s) ? " HAS-ERROR: " + Errors(s) : "")));
        }
        Line("   skybox: " + (RenderSettings.skybox == null ? "none" : "'" + RenderSettings.skybox.name + "'@" + (RenderSettings.skybox.shader != null ? RenderSettings.skybox.shader.name + (RenderSettings.skybox.shader.isSupported ? "" : " UNSUPPORTED") : "NO SHADER")));
        // the big ones: anything over 400 m across is a ground or a skyline, and that is what the user sees pink
        lines.Sort();
        log.AppendLine("---- " + name + ": renderers over 300 m across");
        foreach (var l in lines) if (Big(l)) log.AppendLine("   " + l);
        log.AppendLine("---- " + name + ": every live renderer");
        foreach (var l in lines) log.AppendLine("   " + l);
    }

    static bool Big(string l)
    {
        int i = l.IndexOf("size (");
        if (i < 0) return false;
        string s = l.Substring(i + 6, l.IndexOf(')', i) - i - 6);
        foreach (var p in s.Split(',')) if (float.TryParse(p.Trim(), out float v) && v >= 300f) return true;
        return false;
    }

    static string Errors(Shader s)
    {
        var sb = new StringBuilder();
        foreach (var m in ShaderUtil.GetShaderMessages(s)) if (m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error) { sb.Append(m.message).Append(" @" + m.file + ":" + m.line).Append(" || "); }
        return sb.ToString();
    }

    static string PathOf(Transform t)
    {
        string s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }

    // ---------------------------------------------------------------- the bisection

    /// Groups of renderers off one at a time (by shader, then by root object), the frame read after each: whichever
    /// group's absence takes the magenta away is drawing it. The skybox, the particles, the camera stack and the post
    /// stack are tried too, since none of them is a renderer the census can point at
    IEnumerator Bisect()
    {
        var cam = Camera.main;
        var all = new List<Renderer>();
        foreach (var r in Resources.FindObjectsOfTypeAll<Renderer>()) if (r != null && !EditorUtility.IsPersistent(r) && r.enabled && r.gameObject.activeInHierarchy) all.Add(r);
        float baseline = Capture("bisect-base", false);
        Line("bisect: baseline " + baseline.ToString("0.00") + "%");

        // the skybox
        var sky = RenderSettings.skybox; RenderSettings.skybox = null;
        yield return null;
        Line("   without the skybox: " + Capture("b", false).ToString("0.00") + "%");
        RenderSettings.skybox = sky;

        // the post stack
        var data = cam != null ? cam.GetUniversalAdditionalCameraData() : null;
        if (data != null)
        {
            bool post = data.renderPostProcessing; data.renderPostProcessing = false;
            yield return null;
            Line("   without post-processing: " + Capture("b", false).ToString("0.00") + "%");
            data.renderPostProcessing = post;
            if (data.cameraStack != null && data.cameraStack.Count > 0)
            {
                var stack = new List<Camera>(data.cameraStack); data.cameraStack.Clear();
                yield return null;
                Line("   without the camera stack (" + stack.Count + " overlay): " + Capture("b", false).ToString("0.00") + "%");
                data.cameraStack.AddRange(stack);
            }
        }

        // by shader
        var byShader = new Dictionary<string, List<Renderer>>();
        foreach (var r in all)
        {
            var m = r.sharedMaterial;
            string key = m == null ? "EMPTY SLOT" : (m.shader != null ? m.shader.name : "NO SHADER");
            if (!byShader.TryGetValue(key, out var list)) byShader[key] = list = new List<Renderer>();
            list.Add(r);
        }
        foreach (var kv in byShader)
        {
            foreach (var r in kv.Value) if (r != null) r.enabled = false;
            yield return null;
            float p = Capture("b", false);
            foreach (var r in kv.Value) if (r != null) r.enabled = true;
            Line("   without " + kv.Value.Count + " on " + kv.Key + ": " + p.ToString("0.00") + "%" + (p < baseline * 0.5f ? "   <<<<< THE MAGENTA IS HERE" : ""));
        }

        // by root object
        var byRoot = new Dictionary<Transform, List<Renderer>>();
        foreach (var r in all)
        {
            var root = r.transform.root;
            if (!byRoot.TryGetValue(root, out var list)) byRoot[root] = list = new List<Renderer>();
            list.Add(r);
        }
        foreach (var kv in byRoot)
        {
            if (kv.Value.Count == 0) continue;
            foreach (var r in kv.Value) if (r != null) r.enabled = false;
            yield return null;
            float p = Capture("b", false);
            foreach (var r in kv.Value) if (r != null) r.enabled = true;
            if (p < baseline * 0.9f) Line("   without root '" + kv.Key.name + "' (" + kv.Value.Count + ", scene " + kv.Key.gameObject.scene.name + "): " + p.ToString("0.00") + "%" + (p < baseline * 0.5f ? "   <<<<< THE MAGENTA IS HERE" : ""));
        }
        // the culprits by material, within the root(s) that mattered: one renderer at a time is too slow for 800;
        // by material is enough to name it
        var byMat = new Dictionary<Material, List<Renderer>>();
        foreach (var r in all) { var m = r.sharedMaterial; if (m == null) continue; if (!byMat.TryGetValue(m, out var list)) byMat[m] = list = new List<Renderer>(); list.Add(r); }
        foreach (var kv in byMat)
        {
            foreach (var r in kv.Value) if (r != null) r.enabled = false;
            yield return null;
            float p = Capture("b", false);
            foreach (var r in kv.Value) if (r != null) r.enabled = true;
            if (p < baseline * 0.9f) Line("   without material '" + kv.Key.name + "' on " + (kv.Key.shader != null ? kv.Key.shader.name : "NO SHADER") + " [" + string.Join(" ", kv.Key.shaderKeywords) + "] q" + kv.Key.renderQueue + " (" + kv.Value.Count + " renderers, first " + PathOf(kv.Value[0].transform) + "): " + p.ToString("0.00") + "%");
        }
    }
}
