using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// THE PINK HUNTER (2026-10-27; the user: "tons of magenta again, keep working on it"). The material net checks
/// every material before the first frame by drawing it alone, and the census names what sits under magenta pixels;
/// both said "fine" while the smog, the airships and the flock drew magenta in play. So this one works from the
/// frame itself, in play, on a clock: every couple of seconds it reads the screen back, finds the error shader's
/// magenta, tallies which materials sit under it, and PUTS THE WORST ONE ON A STAND-IN SHADER — the stock Unlit for
/// anything see-through, the toon for anything solid (Unlit when the toon is the one failing) — keeping its colours,
/// maps, blend and queue. The next read is the verdict: if the magenta under that material went, the swap stays,
/// every material drawn the same way (same shader, same keywords) is swapped with it, and the way is REMEMBERED
/// (PlayerPrefs), so the next level and the next session swap those before their first frame. If the magenta
/// stayed, the material is put back and marked innocent (a solid one gets a second try on Unlit). The console gets
/// one line per confirmed culprit, naming the shader and the keyword set that does not run on this machine, with the
/// GPU and the driver. Nothing is swapped on a guess alone, and nothing is swapped twice.
/// </summary>
public class PinkHunter : MonoBehaviour
{
    const float Interval = 1.6f;        // seconds between reads
    const float MinPinkPct = 0.03f;     // less than this of the frame is noise
    const int MaxSamples = 160;         // pink pixels traced per read
    const string PrefsKey = "PinkHunter.Culprits";

    class Trial { public Material m; public Shader was; public int queue; public string key; public float share; public int attempt; public int rounds; }

    static HashSet<string> known;       // shader + keyword sets confirmed broken on this machine, across sessions
    readonly Dictionary<Material, int> attempts = new Dictionary<Material, int>();   // per material: stand-ins tried (−1 = confirmed culprit)
    readonly Dictionary<Material, float> weight = new Dictionary<Material, float>();
    readonly List<(float d, Renderer r)> near = new List<(float, Renderer)>();
    Trial trial;
    bool busy;
    float next;
    int swapped;

    static HashSet<string> Known
    {
        get
        {
            if (known != null) return known;
            known = new HashSet<string>();
            string saved = PlayerPrefs.GetString(PrefsKey, "");
            foreach (string k in saved.Split('\n')) if (!string.IsNullOrWhiteSpace(k)) known.Add(k);
            return known;
        }
    }

    /// The way a material is drawn: its shader, its keywords, its queue — what a verdict is about
    public static string KeyOf(Material m) => m.shader != null ? m.shader.name + " [" + string.Join(" ", m.shaderKeywords) + "] q" + m.renderQueue : "no shader";

    void Start()
    {
        next = Time.unscaledTime + 2.5f;   // the census and the net have the first seconds
        if (Known.Count == 0) return;
        // what the last session confirmed is swapped before anyone sees it
        int n = Sweep(null, null);
        if (n > 0) Debug.LogWarning("PinkHunter: " + n + " material(s) drawn a way this machine cannot (found in an earlier session) put on stand-in shaders before the first frame. " + MaterialNet.Device());
    }

    void Update()
    {
        if (busy || Time.unscaledTime < next) return;
        next = Time.unscaledTime + Interval;
        StartCoroutine(Hunt());
    }

    IEnumerator Hunt()
    {
        busy = true;
        yield return new WaitForEndOfFrame();
        try { Read(); }
        catch (System.Exception e) { Debug.LogWarning("PinkHunter: a read failed (" + e.Message + ")"); }
        busy = false;
    }

    void Read()
    {
        var cam = Camera.main;
        if (cam == null || Application.isBatchMode) return;
        Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
        if (shot == null) return;
        int w = shot.width, h = shot.height, step = Mathf.Max(3, w / 220);
        var px = shot.GetPixels32();
        Destroy(shot);
        int total = 0, pink = 0;
        var hits = new List<Vector2Int>();
        for (int y = 0; y < h; y += step)
            for (int x = 0; x < w; x += step)
            {
                var c = px[y * w + x];
                total++;
                if (c.r > 140 && c.b > 120 && c.g < 90 && c.r - c.g > 80 && c.b - c.g > 60) { pink++; hits.Add(new Vector2Int(x, y)); }
            }
        float pct = total > 0 ? 100f * pink / total : 0f;

        // who sits under the magenta: the nearest two renderers along the camera's ray at a spread of the pink pixels,
        // the nearest weighted double; only what the camera draws (the viewmodel's layer and the UI never count)
        weight.Clear();
        float sum = 0f;
        if (hits.Count > 0)
        {
            var renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
            int stride = Mathf.Max(1, hits.Count / MaxSamples);
            for (int i = 0; i < hits.Count; i += stride)
            {
                var ray = cam.ScreenPointToRay(new Vector3(hits[i].x, hits[i].y, 0f));
                near.Clear();
                foreach (var r in renderers)
                {
                    if (!r.enabled || !r.isVisible || (cam.cullingMask & (1 << r.gameObject.layer)) == 0) continue;
                    if (r.bounds.IntersectRay(ray, out float d) && d < 8000f) near.Add((d, r));
                }
                near.Sort((a, b) => a.d.CompareTo(b.d));
                for (int k = 0; k < Mathf.Min(2, near.Count); k++)
                {
                    var mats = near[k].r.sharedMaterials;
                    var m = mats.Length > 0 ? mats[0] : null;
                    if (m == null) continue;
                    float wgt = k == 0 ? 2f : 1f;
                    weight.TryGetValue(m, out float have);
                    weight[m] = have + wgt;
                    sum += wgt;
                }
            }
        }

        // THE VERDICT on the last swap: its share of the magenta now against its share before
        if (trial != null)
        {
            trial.rounds++;
            weight.TryGetValue(trial.m, out float now);
            float share = sum > 0f ? now / sum : 0f;
            bool inconclusive = pink < 8 && trial.rounds < 3;   // the player looked away: wait a round or two
            if (inconclusive) return;
            if (pink < 8 || share < trial.share * 0.25f) Confirm(trial);
            else
            {
                trial.m.shader = trial.was;
                trial.m.renderQueue = trial.queue;
                attempts[trial.m] = trial.attempt + 1;
                Debug.Log("PinkHunter: '" + trial.m.name + "' on " + trial.was.name + " was not it (the magenta stayed); put back");
            }
            trial = null;
            return;
        }

        if (pct < MinPinkPct || sum <= 0f) return;

        // the worst material not yet judged goes on a stand-in; anything already known broken goes straight over
        Material worst = null; float worstW = 0f;
        foreach (var kv in weight)
        {
            var m = kv.Key;
            if (m.shader == null) continue;
            if (Known.Contains(KeyOf(m))) { Swap(m, StandIn(m, 0)); continue; }
            attempts.TryGetValue(m, out int tried);
            if (tried < 0 || tried >= 2) continue;
            if (kv.Value > worstW) { worstW = kv.Value; worst = m; }
        }
        if (worst == null) return;
        attempts.TryGetValue(worst, out int attempt);
        var standIn = StandIn(worst, attempt);
        if (standIn == null || standIn == worst.shader) { attempts[worst] = attempt + 1; return; }
        trial = new Trial { m = worst, was = worst.shader, queue = worst.renderQueue, key = KeyOf(worst), share = worstW / sum, attempt = attempt };
        Swap(worst, standIn);
    }

    void Confirm(Trial t)
    {
        attempts[t.m] = -1;
        if (Known.Add(t.key)) PlayerPrefs.SetString(PrefsKey, string.Join("\n", Known));
        int more = Sweep(t.key, t.m);
        Debug.LogWarning("PinkHunter: '" + t.m.name + "' on " + t.key + " draws the error shader here — put on " + t.m.shader.name
            + (more > 0 ? ", and " + more + " more material(s) drawn the same way with it" : "") + ". " + MaterialNet.Device());
    }

    /// Every material in the scene drawn a known-broken way goes on its stand-in (all of them when `key` is null)
    int Sweep(string key, Material except)
    {
        int n = 0;
        var seen = new HashSet<Material>();
        foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            foreach (var m in r.sharedMaterials)
            {
                if (m == null || m == except || m.shader == null || !seen.Add(m)) continue;
                string k = KeyOf(m);
                if (key != null ? k != key : !Known.Contains(k)) continue;
                var standIn = StandIn(m, 0);
                if (standIn == null || standIn == m.shader) continue;
                Swap(m, standIn);
                attempts[m] = -1;
                n++;
            }
        return n;
    }

    void Swap(Material m, Shader to)
    {
        if (to == null || m.shader == to) return;
        int q = m.renderQueue;
        m.shader = to;          // colours, maps, blend and keywords stay on the material; the queue is put back below
        m.renderQueue = q;
        swapped++;
    }

    /// The shader that stands in: Unlit for anything see-through (its blend and maps carry over by name), the toon for
    /// anything solid unless the toon itself is what fails here, Unlit then; the second attempt is always Unlit
    static Shader StandIn(Material m, int attempt)
    {
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        bool transparent = m.renderQueue >= 2450 || m.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT") || (m.HasProperty("_Surface") && m.GetFloat("_Surface") > 0.5f);
        if (transparent || attempt > 0) return unlit;
        var toon = Shader.Find("Momentum/Toon");
        if (toon == null || !toon.isSupported || m.shader == toon) return unlit;
        foreach (string k in Known) if (k.StartsWith(toon.name + " ")) return unlit;
        return toon;
    }

    /// F6's report can show what the hunter did
    public string Summary() => swapped == 0 ? "PinkHunter: nothing swapped" : "PinkHunter: " + swapped + " material(s) on stand-in shaders; known broken ways: " + Known.Count;
}
