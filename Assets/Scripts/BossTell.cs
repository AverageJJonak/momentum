using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE TELL. Every boss attack announces itself the same way, in three channels
/// at once, so it can be read from anywhere in the arena and never has to be
/// read off the animation alone:
///   THE REACH   — a disc on the floor the size of what is about to land, dim,
///                 where it lands (under the boss, out in front of it, or on the
///                 spot it is aiming at).
///   THE FUSE    — a bright ring round that disc that CLOSES on the centre over
///                 the wind-up. When the ring reaches the middle, it lands. That
///                 is the timer, and it is the same timer for every attack.
///   THE LIGHT   — a lamp on the part that is coming (the hammer head, the hand)
///                 and the part itself burning the colour, pulsing faster as the
///                 fuse closes, so the thing to watch is lit and the floor round
///                 the boss carries the colour.
/// GOLD means answer it: a parry in the window counters. RED means move: out of
/// the reach before the fuse closes. The colour is the whole rule, the same rule
/// every other unit in the game follows.
/// One tell at a time per boss; Begin hands back a ticket so an attack that has
/// already ended can never take down the tell of the one that followed it.
/// </summary>
public class BossTell
{
    public enum Kind { Gold, Red }

    const int Segments = 28;
    static readonly Renderer[] NoRenderers = new Renderer[0];
    static readonly Material[] NoMaterials = new Material[0];

    readonly Transform root, disc, fuse, lampT;
    readonly MeshRenderer discR;
    readonly MeshRenderer[] fuseR = new MeshRenderer[Segments];
    readonly Light lamp;
    readonly Material goldFill, goldRim, redFill, redRim;
    readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
    readonly Dictionary<Transform, Renderer[]> partsOf = new Dictionary<Transform, Renderer[]>();
    Renderer[] glowing = NoRenderers;
    Material[] rest = NoMaterials;
    Transform follow;
    Vector3 centre;
    float start, duration, radius, ahead;
    Kind kind;
    int serial;

    /// The ticket of the tell that is up, or 0
    public int Active { get; private set; }

    public BossTell(Transform parent)
    {
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        Material Mat(Color c) { var m = new Material(unlit); m.SetColor("_BaseColor", c); return m; }
        goldFill = Mat(new Color(0.8f, 0.6f, 0.18f));
        goldRim = Mat(new Color(2.6f, 2f, 0.55f));
        redFill = Mat(new Color(0.85f, 0.2f, 0.14f));
        redRim = Mat(new Color(2.8f, 0.3f, 0.15f));

        root = new GameObject("BossTell").transform;
        root.SetParent(parent, false);
        var d = LevelKit.Prim(root, Vector3.zero, Vector3.one, goldFill, "TellReach", PrimitiveType.Cylinder);
        disc = d.transform;
        discR = d.GetComponent<MeshRenderer>();
        fuse = new GameObject("TellFuse").transform;
        fuse.SetParent(root, false);
        for (int i = 0; i < Segments; i++)
        {
            var seg = LevelKit.Prim(fuse, Vector3.zero, Vector3.one, goldRim, "FuseSeg");
            fuseR[i] = seg.GetComponent<MeshRenderer>();
        }
        lampT = new GameObject("TellLamp").transform;
        lampT.SetParent(root, false);
        lamp = lampT.gameObject.AddComponent<Light>();
        lamp.type = LightType.Point;
        lamp.range = 16f;
        lamp.intensity = 0f;
        lamp.shadows = LightShadows.None;
        root.gameObject.SetActive(false);
    }

    /// Announce. `floorCentre` is where it lands and `reach` how far; `focus` is the part to watch (its renderers
    /// burn the colour and carry the lamp), `focus2` a second one (both hands); `follow` keeps the centre under a
    /// moving body, `ahead` metres in front of it. Returns the ticket to End with.
    public int Begin(Kind k, float seconds, Vector3 floorCentre, float reach, Transform focus, Transform follow, float ahead = 0f, Transform focus2 = null)
    {
        End();
        kind = k;
        start = Time.time;
        duration = Mathf.Max(0.05f, seconds);
        radius = Mathf.Max(0.8f, reach);
        centre = floorCentre;
        this.follow = follow;
        this.ahead = ahead;
        bool gold = k == Kind.Gold;
        discR.sharedMaterial = gold ? goldFill : redFill;
        for (int i = 0; i < Segments; i++) fuseR[i].sharedMaterial = gold ? goldRim : redRim;
        lamp.color = gold ? new Color(1f, 0.82f, 0.35f) : new Color(1f, 0.22f, 0.12f);
        lampT.SetParent(focus != null ? focus : root, false);
        lampT.localPosition = Vector3.zero;

        // The part itself burns the colour, and remembers what it wore
        glowing = Parts(focus, focus2);
        rest = glowing.Length > 0 ? new Material[glowing.Length] : NoMaterials;
        Material burn = gold ? goldRim : redRim;
        for (int i = 0; i < glowing.Length; i++) { rest[i] = glowing[i].sharedMaterial; glowing[i].sharedMaterial = burn; }

        root.gameObject.SetActive(true);
        Active = ++serial;
        var feel = GameFeel.Instance;
        if (feel != null) { if (gold) feel.PlayTellGold(); else feel.PlayTellRed(); }
        Tick(Time.time, floorCentre.y);
        return Active;
    }

    /// Every frame: the fuse closes, the light and the part pulse faster as it does
    public void Tick(float now, float floorY)
    {
        if (Active == 0) return;
        float k = Mathf.Clamp01((now - start) / duration);
        if (follow != null)
        {
            Vector3 p = follow.position + follow.forward * ahead;
            centre = new Vector3(p.x, floorY, p.z);
        }
        root.position = centre + Vector3.up * 0.08f;
        root.rotation = Quaternion.identity;
        disc.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);
        float r = Mathf.Lerp(radius, 0.5f, k);
        float segLen = 2f * Mathf.PI * r / Segments * 1.12f;
        for (int i = 0; i < Segments; i++)
        {
            float a = i * 360f / Segments;
            var seg = fuseR[i].transform;
            seg.localRotation = Quaternion.Euler(0f, a, 0f);
            seg.localPosition = seg.localRotation * new Vector3(0f, 0.08f, r);
            seg.localScale = new Vector3(0.3f, 0.16f, segLen);
        }
        float pulse = 0.5f + 0.5f * Mathf.Sin(now * (8f + 12f * k));
        lamp.intensity = Mathf.Lerp(3f, 9f, k) * (0.7f + 0.3f * pulse);
        Color glow = (kind == Kind.Gold ? new Color(2.4f, 1.85f, 0.5f) : new Color(2.6f, 0.28f, 0.14f)) * (0.85f + 0.5f * pulse);
        block.SetColor("_BaseColor", glow);
        for (int i = 0; i < glowing.Length; i++) if (glowing[i] != null) glowing[i].SetPropertyBlock(block);
    }

    /// Take it down. With a ticket, only if that tell is still the one up; without one, whatever is up.
    public void End(int ticket = 0)
    {
        if (Active == 0 || (ticket != 0 && ticket != Active)) return;
        Active = 0;
        for (int i = 0; i < glowing.Length; i++)
        {
            if (glowing[i] == null) continue;
            glowing[i].SetPropertyBlock(null);
            glowing[i].sharedMaterial = rest[i];
        }
        glowing = NoRenderers;
        rest = NoMaterials;
        lamp.intensity = 0f;
        lampT.SetParent(root, false);
        root.gameObject.SetActive(false);
    }

    /// The renderers under a part, found once per part
    Renderer[] Parts(Transform focus, Transform focus2)
    {
        var a = One(focus);
        var b = One(focus2);
        if (b.Length == 0) return a;
        if (a.Length == 0) return b;
        var both = new Renderer[a.Length + b.Length];
        a.CopyTo(both, 0);
        b.CopyTo(both, a.Length);
        return both;
    }

    Renderer[] One(Transform t)
    {
        if (t == null) return NoRenderers;
        if (partsOf.TryGetValue(t, out var cached)) return cached;
        var found = new List<Renderer>();
        foreach (var r in t.GetComponentsInChildren<MeshRenderer>())
            if (r != null && r.gameObject.layer != 2 && r.GetComponent<TextMesh>() == null) found.Add(r);
        var arr = found.ToArray();
        partsOf[t] = arr;
        return arr;
    }
}
