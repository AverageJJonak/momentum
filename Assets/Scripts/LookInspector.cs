using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// THE LOOK INSPECTOR (2026-10-23, user: "still tons of magenta and I also spawned beneath the map"). F7 in play names
/// what the crosshair is on: the object by its path, its renderer, its material slots, each slot's shader and whether
/// Unity can draw it, its keywords and maps, and what stands under the player. On screen until Escape or the next F7,
/// and in the console. A pink surface is then reported by what it IS, not by a screenshot of pink. The same component
/// watches the spawn: what stands under it when the level is built, and whether the player dropped away from it in the
/// first second (a floor with no collider under the spawn, or a spawn inside a solid that pushed the player out).
/// </summary>
public class LookInspector : MonoBehaviour
{
    public static Vector3 Spawn;
    static string spawnUnder = "unknown";

    string report;
    GUIStyle box, text;
    Transform player;

    /// Called by the bootstrap once the level is built and physics is synced, before the player exists
    public static void SpawnReport(Vector3 spawn)
    {
        Spawn = spawn;
        spawnUnder = Under(spawn);
        Debug.Log("LookInspector: spawn at " + spawn.ToString("0.0") + "; under it: " + spawnUnder);
    }

    void Start() => StartCoroutine(SpawnWatch());

    IEnumerator SpawnWatch()
    {
        yield return new WaitForSeconds(1f);
        var motor = FindAnyObjectByType<PlayerMotor>();
        if (motor != null) player = motor.transform;   // the menu has no player: the census still runs
        float drop = player != null ? Spawn.y - player.position.y : 0f;
        if (drop > 3f)
            Debug.LogWarning("LookInspector: the player fell " + drop.ToString("0.0") + " m in the first second after spawning at " + Spawn.ToString("0.0")
                + " (now " + player.position.ToString("0.0") + "). Under the spawn when the level was built: " + spawnUnder
                + ". Under the player now: " + Under(player.position));
        yield return new WaitForSeconds(1.5f);
        yield return Census(false);   // the first look round: is anything on screen magenta, and what is under those pixels
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.f7Key.wasPressedThisFrame) { report = Inspect(); Debug.Log("LookInspector:\n" + report); }
        else if (kb.f6Key.wasPressedThisFrame) StartCoroutine(Census(true));
        else if (report != null && kb.escapeKey.wasPressedThisFrame) report = null;
    }

    /// THE CENSUS (F6, and once on its own after the spawn): the frame is read back, every pixel of the error shader's
    /// magenta is counted, and for a spread of them the renderers whose bounds the camera's ray crosses are tallied,
    /// nearest weighted most, with everything about how each is drawn. Nothing has to have a collider, and nothing
    /// has to be aimed at: whatever is pink names itself in the console, and on screen when asked for
    IEnumerator Census(bool show)
    {
        yield return new WaitForEndOfFrame();
        var cam = Camera.main;
        if (cam == null) yield break;
        Texture2D shot;
        try { shot = ScreenCapture.CaptureScreenshotAsTexture(); }
        catch (System.Exception e) { Debug.LogWarning("LookInspector: the frame could not be read back (" + e.Message + ")"); yield break; }
        if (shot == null) yield break;
        int w = shot.width, h = shot.height, step = Mathf.Max(4, w / 200);
        var px = shot.GetPixels32();
        Destroy(shot);
        int total = 0, pink = 0;
        var hits = new System.Collections.Generic.List<Vector2Int>();
        for (int y = 0; y < h; y += step)
            for (int x = 0; x < w; x += step)
            {
                var c = px[y * w + x];
                total++;
                if (c.r > 140 && c.b > 120 && c.g < 90 && c.r - c.g > 80 && c.b - c.g > 60) { pink++; if (hits.Count < 600) hits.Add(new Vector2Int(x, y)); }
            }
        float pct = total > 0 ? 100f * pink / total : 0f;
        if (pink == 0)
        {
            string none = "LookInspector: census — no magenta on screen. " + MaterialNet.Device();
            Debug.Log(none);
            if (show) report = none;
            yield break;
        }
        var tally = new System.Collections.Generic.Dictionary<string, int>();
        var renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
        var near = new System.Collections.Generic.List<(float d, Renderer r)>();
        int stride = Mathf.Max(1, hits.Count / 40);
        for (int i = 0; i < hits.Count; i += stride)
        {
            var ray = cam.ScreenPointToRay(new Vector3(hits[i].x, hits[i].y, 0f));
            near.Clear();
            foreach (var r in renderers) if (r.enabled && r.bounds.IntersectRay(ray, out float d) && d < 6000f) near.Add((d, r));
            near.Sort((a, b) => a.d.CompareTo(b.d));
            for (int k = 0; k < Mathf.Min(3, near.Count); k++)
            {
                string key = Signature(near[k].r);
                tally.TryGetValue(key, out int n);
                tally[key] = n + (3 - k);
            }
        }
        var top = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, int>>(tally);
        top.Sort((a, b) => b.Value.CompareTo(a.Value));
        var sb = new StringBuilder("LookInspector: census — " + pct.ToString("0.0") + "% of the frame is magenta. Under those pixels, most likely first:");
        for (int i = 0; i < Mathf.Min(8, top.Count); i++) sb.Append("\n  " + top[i].Value + "  " + top[i].Key);
        sb.Append("\n" + MaterialNet.Device());
        Debug.LogWarning(sb.ToString());
        if (show) report = sb.ToString();
    }

    /// Everything about how a renderer is drawn, on one line
    static string Signature(Renderer r)
    {
        var mats = r.sharedMaterials;
        var m = mats.Length > 0 ? mats[0] : null;
        var mf = r.GetComponent<MeshFilter>();
        string mesh = mf != null && mf.sharedMesh != null ? mf.sharedMesh.name + " (" + mf.sharedMesh.vertexCount + " v" + (mf.sharedMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Normal) ? "" : ", no normals") + (mf.sharedMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0) ? "" : ", no uv") + ")" : "-";
        return Path(r.transform) + " | " + (m == null ? "EMPTY SLOT" : "'" + m.name + "' on " + (m.shader != null ? m.shader.name : "no shader") + " [" + string.Join(" ", m.shaderKeywords) + "] q" + m.renderQueue)
            + " | mesh " + mesh + " | shadows " + r.shadowCastingMode + ", receive " + r.receiveShadows + ", probes " + r.lightProbeUsage + ", block " + r.HasPropertyBlock() + ", layer " + r.gameObject.layer + ", scale " + r.transform.lossyScale.ToString("0");
    }

    string Inspect()
    {
        var cam = Camera.main;
        if (cam == null) return "F7: no main camera";
        if (player == null) { var motor = FindAnyObjectByType<PlayerMotor>(); if (motor != null) player = motor.transform; }
        var sb = new StringBuilder();
        var ray = new Ray(cam.transform.position, cam.transform.forward);
        if (!Physics.Raycast(ray, out var hit, 3000f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            sb.AppendLine("F7: nothing with a collider under the crosshair within 3 km (a pink thing with no collider cannot be named this way)");
        else
        {
            sb.AppendLine("F7: " + Path(hit.transform) + "   at " + hit.point.ToString("0.0") + ", " + hit.distance.ToString("0.0") + " m away");
            var r = hit.collider.GetComponent<Renderer>();
            if (r == null) r = hit.collider.GetComponentInChildren<Renderer>();
            if (r == null) r = hit.collider.GetComponentInParent<Renderer>();
            if (r == null) sb.AppendLine("no renderer on it: a collider only, so what you see there is drawn by something else");
            else Describe(sb, r);
        }
        // The things with no collider (smog, haze, glass, glows): every renderer whose bounds the ray crosses, nearest first
        var crossed = new System.Collections.Generic.List<(float d, Renderer r)>();
        foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
            if (r.enabled && r.bounds.IntersectRay(ray, out float d) && d < 3000f) crossed.Add((d, r));
        crossed.Sort((a, b) => a.d.CompareTo(b.d));
        sb.AppendLine("drawn along the ray, collider or not, nearest first:");
        for (int i = 0; i < Mathf.Min(6, crossed.Count); i++)
        {
            var (d, r) = crossed[i];
            var mats = r.sharedMaterials;
            var m = mats.Length > 0 ? mats[0] : null;
            string mat = m == null ? "EMPTY material slot: draws pink" : "'" + m.name + "' on " + (m.shader != null ? m.shader.name : "no shader")
                + (MaterialNet.DrawsPink(m) ? "   DRAWS PINK ALONE" : "");
            sb.AppendLine("  " + d.ToString("0") + " m  " + Path(r.transform) + "  " + mat);
        }
        if (player != null)
        {
            sb.AppendLine("player at " + player.position.ToString("0.0") + "; under the player: " + Under(player.position));
            sb.AppendLine("spawned at " + Spawn.ToString("0.0") + "; under the spawn at build: " + spawnUnder);
        }
        return sb.ToString().TrimEnd();
    }

    static void Describe(StringBuilder sb, Renderer r)
    {
        sb.AppendLine(r.GetType().Name + (r.enabled ? "" : " (disabled: welded into a merged mesh that wears the same material)")
            + ", layer " + LayerMask.LayerToName(r.gameObject.layer) + ", " + (r.gameObject.activeInHierarchy ? "active" : "INACTIVE"));
        var mats = r.sharedMaterials;
        var mf = r.GetComponent<MeshFilter>();
        if (mf != null)
        {
            var mesh = mf.sharedMesh;
            sb.AppendLine(mesh == null ? "mesh: NONE" : "mesh '" + mesh.name + "', " + mesh.subMeshCount + " submesh(es), " + mats.Length + " material slot(s)");
        }
        if (mats.Length == 0) sb.AppendLine("no material slots: nothing is drawn");
        for (int i = 0; i < mats.Length; i++)
        {
            var m = mats[i];
            if (m == null) { sb.AppendLine("material " + i + ": EMPTY SLOT (destroyed, or never set): this is what draws pink"); continue; }
            var s = m.shader;
            sb.AppendLine("material " + i + ": '" + m.name + "'   shader " + (s == null ? "NONE: draws pink" : "'" + s.name + "'" + (s.isSupported ? "" : " NOT SUPPORTED: draws pink") + ", " + s.passCount + " pass(es)") + "   queue " + m.renderQueue);
            if (s != null && MaterialNet.EditorError != null)
            {
                string e = MaterialNet.EditorError(s);
                if (!string.IsNullOrEmpty(e)) sb.AppendLine("   shader error: " + e);
            }
            string kw = string.Join(" ", m.shaderKeywords);
            if (kw.Length > 0) sb.AppendLine("   keywords: " + kw);
            sb.AppendLine("   base map " + Tex(m, "_BaseMap") + "; normal " + Tex(m, "_BumpMap") + "; colour " + Col(m));
        }
    }

    /// What a ray straight down from `pos` meets: name, material, shader and depth, or nothing
    static string Under(Vector3 pos)
    {
        if (!Physics.Raycast(pos + Vector3.up * 0.5f, Vector3.down, out var hit, 1000f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return "NOTHING with a collider within 1 km";
        var r = hit.collider.GetComponent<Renderer>();
        if (r == null) r = hit.collider.GetComponentInParent<Renderer>();
        var m = r != null && r.sharedMaterials.Length > 0 ? r.sharedMaterials[0] : null;
        string mat = r == null ? "no renderer" : m == null ? "EMPTY material slot" : "'" + m.name + "' on " + (m.shader != null ? m.shader.name : "no shader");
        return "'" + Path(hit.transform) + "' " + (hit.distance - 0.5f).ToString("0.0") + " m below (" + mat + ")";
    }

    static string Path(Transform t)
    {
        var sb = new StringBuilder(t.name);
        for (var p = t.parent; p != null; p = p.parent) sb.Insert(0, p.name + "/");
        return sb.ToString();
    }

    static string Tex(Material m, string prop)
    {
        if (!m.HasProperty(prop)) return "-";
        var t = m.GetTexture(prop);
        return t == null ? "none" : "'" + t.name + "' " + t.width + "x" + t.height;
    }

    static string Col(Material m)
    {
        if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor").ToString("0.00");
        if (m.HasProperty("_Color")) return m.GetColor("_Color").ToString("0.00");
        return "-";
    }

    void OnGUI()
    {
        if (report == null) return;
        if (box == null)
        {
            box = new GUIStyle(GUI.skin.box);
            box.normal.background = Texture2D.blackTexture;
            text = new GUIStyle(GUI.skin.label) { wordWrap = true, richText = false };
            text.normal.textColor = new Color(0.95f, 0.9f, 0.78f);
        }
        float s = Screen.height / 1080f;
        text.fontSize = Mathf.RoundToInt(16f * s);
        float w = Mathf.Min(Screen.width - 80f * s, 1100f * s), pad = 14f * s;
        float h = text.CalcHeight(new GUIContent(report), w - pad * 2f) + pad * 2f;
        var r = new Rect(40f * s, 40f * s, w, h);
        GUI.Box(r, GUIContent.none, box);
        GUI.Label(new Rect(r.x + pad, r.y + pad, w - pad * 2f, h - pad * 2f), report, text);
    }
}
