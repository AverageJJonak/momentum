using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// THE OVERHEAD (Phase 2, 2026-10-27; the user: "an editor button for an overhead screenshot of a level"). Two images
/// of a level from straight above, the same framing, so a marker lands on the same pixel in both: the RENDER (a
/// temporary orthographic camera over the level, a temporary sun, the pipeline's own draw) and the PLAN (drawn from
/// the solid geometry the validator read: tops shaded by height, walls as brass lines, pads gold, the main line in
/// cyan, the findings as numbered red circles, the spawn green, the checkpoints white, the finish magenta).
/// Momentum/Level Validator/Overhead Of This Scene's Level shoots the Level Editor's working copy as it stands;
/// the validator writes both images for every level it reads. North on the image is +z.
/// </summary>
public static class LevelOverhead
{
    const string Menu = "Momentum/Level Validator/";
    const string OutDir = "Docs/Design/validation";
    const int Pixels = 2048;

    [MenuItem(Menu + "Overhead Of This Scene's Level", false, 20)]
    public static void OverheadOfHolder()
    {
        var h = LevelEditor.Holder();
        if (h == null) { Debug.LogError("LevelOverhead: no working copy. Momentum/Level Editor/Generate This Scene's Level first."); return; }
        var roots = new List<GameObject> { h.gameObject };
        var g = LevelValidator.Read(roots);
        if (g.plats.Count == 0) { Debug.LogError("LevelOverhead: no solid geometry under " + h.name); return; }
        string name = h.kind + "-working-copy";
        var tex = Render(g.bounds, roots);
        Save(tex, name + "-overhead.png");
        var plan = Plan(g.bounds, g);
        Paint(plan, g.bounds, null, null, h.spawn, null, Vector3.zero);
        Save(plan, name + "-plan.png");
        Debug.Log("LevelOverhead: wrote " + OutDir + "/" + name + "-overhead.png and -plan.png");
    }

    /// The validator's call: both images with the main line and the numbered findings on them
    public static void WriteImages(string name, Bounds bounds, LevelValidator.Geometry g, object segments, List<LevelValidator.Finding> findings, List<GameObject> roots)
    {
        var paths = new List<(string, bool, Vector3, Vector3, Vector3, List<(Vector3, Vector3, string)>)>(LevelValidator.Paths(segments));
        var marks = new List<(Vector3 at, int number)>();
        foreach (var f in findings) marks.Add((f.at, f.number));
        Vector3 spawn = paths.Count > 0 ? paths[0].Item3 : bounds.center;
        var checks = new List<Vector3>();
        Vector3 finish = Vector3.zero;
        for (int i = 0; i < paths.Count; i++) { if (i + 1 < paths.Count) checks.Add(paths[i].Item4); else finish = paths[i].Item4; }
        try
        {
            var tex = Render(bounds, roots);
            Paint(tex, bounds, paths, marks, spawn, checks, finish);
            Save(tex, name + "-overhead.png");
        }
        catch (System.Exception e) { Debug.LogWarning("LevelOverhead: the render of " + name + " failed (" + e.Message + "); the plan is still written"); }
        var plan = Plan(bounds, g);
        Paint(plan, bounds, paths, marks, spawn, checks, finish);
        Save(plan, name + "-plan.png");
    }

    // ---------------------------------------------------------------- the framing

    static float Size(Bounds b) => Mathf.Max(b.size.x, b.size.z) * 1.08f + 10f;
    /// The plan's world frame, for anything drawn onto it later (a recorded run): centre x, centre z, the width in metres of the whole image
    public static (float cx, float cz, float size) Frame(Bounds b) => (b.center.x, b.center.z, Size(b));
    static Vector2 ToPixel(Bounds b, Vector3 p)
    {
        float s = Size(b);
        float x = (p.x - (b.center.x - s * 0.5f)) / s * Pixels;
        float y = ((b.center.z + s * 0.5f) - p.z) / s * Pixels;
        return new Vector2(x, y);
    }

    // ---------------------------------------------------------------- the render

    static Texture2D Render(Bounds b, List<GameObject> roots)
    {
        float s = Size(b);
        var camGo = new GameObject("Overhead camera") { hideFlags = HideFlags.HideAndDontSave };
        var sunGo = new GameObject("Overhead sun") { hideFlags = HideFlags.HideAndDontSave };
        RenderTexture rt = null;
        try
        {
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = s * 0.5f;
            cam.aspect = 1f;
            cam.nearClipPlane = 1f;
            cam.farClipPlane = b.size.y + 400f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.07f, 0.09f, 1f);
            cam.transform.position = new Vector3(b.center.x, b.max.y + 200f, b.center.z);
            cam.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            cam.allowHDR = false; cam.allowMSAA = false;
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.4f; sun.color = new Color(1f, 0.95f, 0.85f);
            sun.shadows = LightShadows.None;
            sunGo.transform.rotation = Quaternion.Euler(62f, 35f, 0f);
            rt = new RenderTexture(Pixels, Pixels, 24, RenderTextureFormat.ARGB32);
            rt.Create();
            cam.targetTexture = rt;
            bool drawn = false;
            try
            {
                if (RenderPipeline.SupportsRenderRequest(cam, new RenderPipeline.StandardRequest()))
                {
                    RenderPipeline.SubmitRenderRequest(cam, new RenderPipeline.StandardRequest { destination = rt });
                    drawn = true;
                }
            }
            catch (System.Exception) { drawn = false; }
            if (!drawn) cam.Render();
            var tex = new Texture2D(Pixels, Pixels, TextureFormat.RGBA32, false);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, Pixels, Pixels), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            // black all over: the request did not draw; try the plain render once
            if (!drawn || Dark(tex))
            {
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, Pixels, Pixels), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
            }
            cam.targetTexture = null;
            return tex;
        }
        finally
        {
            if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(sunGo);
        }
    }

    static bool Dark(Texture2D tex)
    {
        float sum = 0f; int n = 0;
        for (int y = 0; y < tex.height; y += 64) for (int x = 0; x < tex.width; x += 64) { var c = tex.GetPixel(x, y); sum += c.r + c.g + c.b; n++; }
        return n > 0 && sum / n < 0.25f;
    }

    // ---------------------------------------------------------------- the plan

    static Texture2D Plan(Bounds b, LevelValidator.Geometry g)
    {
        var tex = new Texture2D(Pixels, Pixels, TextureFormat.RGBA32, false);
        var px = new Color32[Pixels * Pixels];
        var ground = new Color32(22, 22, 28, 255);
        for (int i = 0; i < px.Length; i++) px[i] = ground;
        float yMin = b.min.y, yMax = Mathf.Max(b.max.y, yMin + 1f);
        // the tops, lowest first so the higher ones draw over
        var plats = new List<LevelValidator.Plat>(g.plats);
        plats.Sort((p, q) => p.c.y.CompareTo(q.c.y));
        foreach (var p in plats)
        {
            float t = Mathf.InverseLerp(yMin, yMax, p.c.y);
            byte v = (byte)Mathf.RoundToInt(Mathf.Lerp(60f, 215f, t));
            Color32 c = p.pad ? new Color32(245, 195, 60, 255) : p.lip ? new Color32(200, 90, 60, 255) : new Color32(v, v, (byte)Mathf.Min(255, v + 10), 255);
            Vector2 a = ToPixel(b, new Vector3(p.minX, 0f, p.maxZ)), d = ToPixel(b, new Vector3(p.maxX, 0f, p.minZ));
            FillRect(px, a, d, c);
            if (p.slope >= 8f) Hatch(px, a, d, new Color32((byte)(v / 2), (byte)(v / 2), (byte)(v / 2), 255));
        }
        foreach (var w in g.walls)
        {
            Vector3 a = w.c - w.u * w.halfLen, c = w.c + w.u * w.halfLen;
            Line(px, ToPixel(b, a), ToPixel(b, c), w.kick ? new Color32(255, 215, 120, 255) : new Color32(150, 110, 60, 255), w.kick ? 3 : 2);
        }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    // ---------------------------------------------------------------- the marks

    static void Paint(Texture2D tex, Bounds b, List<(string, bool, Vector3, Vector3, Vector3, List<(Vector3, Vector3, string)>)> paths, List<(Vector3 at, int number)> marks, Vector3 spawn, List<Vector3> checks, Vector3 finish)
    {
        var px = tex.GetPixels32();
        if (paths != null)
            foreach (var p in paths)
            {
                foreach (var m in p.Item6) Line(px, ToPixel(b, m.Item1), ToPixel(b, m.Item2), m.Item3 == "Walk" ? new Color32(60, 220, 230, 255) : m.Item3 == "Drop" ? new Color32(120, 170, 255, 255) : new Color32(255, 240, 120, 255), 3);
                if (p.Item2) Cross(px, ToPixel(b, p.Item5), new Color32(255, 60, 60, 255), 18);
            }
        Diamond(px, ToPixel(b, spawn), new Color32(80, 255, 110, 255), 12);
        if (checks != null) foreach (var c in checks) Diamond(px, ToPixel(b, c), new Color32(255, 255, 255, 255), 10);
        if (finish.sqrMagnitude > 1e-3f) Diamond(px, ToPixel(b, finish), new Color32(255, 80, 255, 255), 12);
        if (marks != null)
            foreach (var m in marks)
            {
                var at = ToPixel(b, m.at);
                Circle(px, at, 15, new Color32(255, 55, 55, 255), 3);
                Digits(px, at + new Vector2(14f, -30f), m.number, new Color32(255, 255, 255, 255), 4);
            }
        tex.SetPixels32(px);
        tex.Apply();
    }

    static void Save(Texture2D tex, string file)
    {
        string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, OutDir);
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir, file), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    // ---------------------------------------------------------------- drawing into Color32[] (y down from the top)

    static void Put(Color32[] px, int x, int y, Color32 c)
    {
        if (x < 0 || y < 0 || x >= Pixels || y >= Pixels) return;
        px[(Pixels - 1 - y) * Pixels + x] = c;   // Texture2D rows run bottom-up
    }

    static void FillRect(Color32[] px, Vector2 a, Vector2 d, Color32 c)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, d.x))), x1 = Mathf.Min(Pixels - 1, Mathf.CeilToInt(Mathf.Max(a.x, d.x)));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, d.y))), y1 = Mathf.Min(Pixels - 1, Mathf.CeilToInt(Mathf.Max(a.y, d.y)));
        for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) Put(px, x, y, c);
    }

    static void Hatch(Color32[] px, Vector2 a, Vector2 d, Color32 c)
    {
        int x0 = Mathf.FloorToInt(Mathf.Min(a.x, d.x)), x1 = Mathf.CeilToInt(Mathf.Max(a.x, d.x)), y0 = Mathf.FloorToInt(Mathf.Min(a.y, d.y)), y1 = Mathf.CeilToInt(Mathf.Max(a.y, d.y));
        for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) if (((x + y) & 7) == 0) Put(px, x, y, c);
    }

    static void Line(Color32[] px, Vector2 a, Vector2 b, Color32 c, int thick)
    {
        float len = Vector2.Distance(a, b);
        int steps = Mathf.Max(1, Mathf.CeilToInt(len));
        for (int i = 0; i <= steps; i++)
        {
            Vector2 p = Vector2.Lerp(a, b, (float)i / steps);
            for (int dy = -thick / 2; dy <= thick / 2; dy++) for (int dx = -thick / 2; dx <= thick / 2; dx++) Put(px, Mathf.RoundToInt(p.x) + dx, Mathf.RoundToInt(p.y) + dy, c);
        }
    }

    static void Circle(Color32[] px, Vector2 at, int r, Color32 c, int thick)
    {
        for (int y = -r - thick; y <= r + thick; y++)
            for (int x = -r - thick; x <= r + thick; x++)
            {
                float d = Mathf.Sqrt(x * x + y * y);
                if (d >= r - thick * 0.5f && d <= r + thick * 0.5f) Put(px, Mathf.RoundToInt(at.x) + x, Mathf.RoundToInt(at.y) + y, c);
            }
    }

    static void Diamond(Color32[] px, Vector2 at, Color32 c, int r)
    {
        for (int y = -r; y <= r; y++) for (int x = -r; x <= r; x++) if (Mathf.Abs(x) + Mathf.Abs(y) <= r) Put(px, Mathf.RoundToInt(at.x) + x, Mathf.RoundToInt(at.y) + y, c);
    }

    static void Cross(Color32[] px, Vector2 at, Color32 c, int r)
    {
        Line(px, at + new Vector2(-r, -r), at + new Vector2(r, r), c, 4);
        Line(px, at + new Vector2(-r, r), at + new Vector2(r, -r), c, 4);
    }

    // a 3×5 digit font, each row three bits, top row first
    static readonly byte[][] Font =
    {
        new byte[] { 7, 5, 5, 5, 7 }, new byte[] { 2, 6, 2, 2, 7 }, new byte[] { 7, 1, 7, 4, 7 }, new byte[] { 7, 1, 7, 1, 7 }, new byte[] { 5, 5, 7, 1, 1 },
        new byte[] { 7, 4, 7, 1, 7 }, new byte[] { 7, 4, 7, 5, 7 }, new byte[] { 7, 1, 1, 1, 1 }, new byte[] { 7, 5, 7, 5, 7 }, new byte[] { 7, 5, 7, 1, 7 },
    };

    static void Digits(Color32[] px, Vector2 at, int number, Color32 c, int scale)
    {
        string s = number.ToString();
        int x0 = Mathf.RoundToInt(at.x), y0 = Mathf.RoundToInt(at.y);
        // a dark plate behind the figure, so it reads on anything
        FillRect(px, new Vector2(x0 - scale, y0 - scale), new Vector2(x0 + s.Length * 4 * scale, y0 + 6 * scale), new Color32(0, 0, 0, 255));
        for (int i = 0; i < s.Length; i++)
        {
            int d = s[i] - '0';
            if (d < 0 || d > 9) continue;
            for (int row = 0; row < 5; row++)
                for (int col = 0; col < 3; col++)
                    if ((Font[d][row] & (4 >> col)) != 0)
                        for (int yy = 0; yy < scale; yy++) for (int xx = 0; xx < scale; xx++) Put(px, x0 + (i * 4 + col) * scale + xx, y0 + row * scale + yy, c);
        }
    }
}
