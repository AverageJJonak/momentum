using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays an ENEMY clip over a unit's procedural animation. Enemy.Update runs its Animate* for the kind, then
/// `clips?.Apply()`: this samples the clip and writes each keyed pivot's local rotation (and position, when the
/// clip keys it) over what the procedural pass just wrote, blended in over blendIn and out over blendOut
/// (0.1 s by default) so a clip starts from the gait and hands back to it. Pivots the clip does not key stay
/// procedural; a bone name the rig lacks is skipped; no clip at all costs one null check. Bones are resolved once
/// per clip per unit (by name under the root, or by a dotted path for a name a rig repeats) and cached; nothing
/// allocates per frame. Time is scaled, and the clip only advances while the unit animates (not resting).
/// Opt-in, from the unit's own file at the moment of its beat, e.g.
///     if (ClipLibrary.Has("chaser_swing")) EnemyClipDriver.Play(this, "chaser_swing");
/// Bosses are not supported (their controllers own the rig outside Enemy.Update).
/// </summary>
public class EnemyClipDriver
{
    /// (unit, clip name, marker name) on the marker's frame
    public static event Action<Enemy, string, string> Marker;

    readonly Enemy enemy;
    readonly Transform root;
    readonly Dictionary<ClipData, Transform[]> bound = new Dictionary<ClipData, Transform[]>();
    ClipData clip;
    Transform[] bones;
    float t, prevFrame, speed, weight;
    bool loop, fading;
    Action<string> onMarker;

    public bool Playing => clip != null;
    public string Current => clip != null ? clip.name : null;

    public EnemyClipDriver(Enemy e)
    {
        enemy = e;
        root = e != null ? e.transform : null;
    }

    /// Play a clip on a unit (speed multiplies the clip's own; loop null = the clip's flag). False when the clip is
    /// missing: the unit keeps its procedural motion. The driver is made on first use (Enemy.clips)
    public static bool Play(Enemy e, string name, float speed = 1f, bool? loop = null, Action<string> onMarker = null)
    {
        if (e == null) return false;
        var data = ClipLibrary.Get(name);
        if (data == null) return false;
        if (e.clips == null) e.clips = new EnemyClipDriver(e);
        e.clips.Begin(data, speed, loop ?? data.loop, onMarker);
        return true;
    }

    public static void Stop(Enemy e) { if (e != null && e.clips != null) e.clips.Stop(); }

    void Begin(ClipData data, float spd, bool lp, Action<string> cb)
    {
        clip = data;
        bones = Bind(data);
        speed = Mathf.Max(0.01f, spd) * Mathf.Max(0.01f, data.speed);
        loop = lp;
        t = 0f;
        prevFrame = -1f;
        fading = false;
        onMarker = cb;
        // `weight` is kept: a clip straight after a clip stays blended in, no dip through the procedural pose
    }

    /// Fade out over the clip's blendOut, then hand back to the procedural motion
    public void Stop() { if (clip != null) fading = true; }

    /// Called by Enemy.Update right after the kind's Animate*: the clip over the procedural pose
    public void Apply()
    {
        if (clip == null || root == null) return;
        float dt = Time.deltaTime;
        t += dt * speed;
        float frame = clip.FrameAt(t);
        if (!loop && t >= clip.Duration) fading = true;
        if (fading)
        {
            weight -= dt / Mathf.Max(0.001f, clip.blendOut);
            if (weight <= 0f) { Clear(); return; }
        }
        else weight = Mathf.Min(1f, weight + dt / Mathf.Max(0.001f, clip.blendIn));
        if (!fading) FireMarkers(prevFrame, frame);
        prevFrame = frame;
        var cb = clip.bones;
        for (int i = 0; i < cb.Length; i++)
        {
            var tr = bones[i];
            if (tr == null) continue;
            var b = cb[i];
            Quaternion r = clip.SampleRot(b, frame);
            tr.localRotation = weight >= 1f ? r : Quaternion.Slerp(tr.localRotation, r, weight);
            if (clip.SamplePos(b, frame, out Vector3 p))
                tr.localPosition = weight >= 1f ? p : Vector3.Lerp(tr.localPosition, p, weight);
        }
    }

    void Clear()
    {
        clip = null;
        bones = null;
        weight = 0f;
        fading = false;
        onMarker = null;
    }

    Transform[] Bind(ClipData data)
    {
        if (bound.TryGetValue(data, out var cached))
        {
            bool stale = false;
            for (int i = 0; i < cached.Length && !stale; i++) stale = (object)cached[i] != null && cached[i] == null;   // a destroyed pivot (the unit was rebuilt)
            if (!stale) return cached;
        }
        var arr = new Transform[data.bones.Length];
        for (int i = 0; i < arr.Length; i++)
        {
            string n = data.bones[i].name;
            arr[i] = n.IndexOf('.') >= 0 ? ByPath(root, n) : ByName(root, n);
            if (arr[i] == null) Debug.LogWarning("EnemyClipDriver: " + data.name + " keys '" + n + "', which " + root.name + " has no pivot for");
        }
        bound[data] = arr;
        return arr;
    }

    static Transform ByPath(Transform root, string path)
    {
        Transform t = root;
        foreach (var part in path.Split('.'))
        {
            if (part == root.name && t == root) continue;
            t = ByName(t, part);
            if (t == null) return null;
        }
        return t;
    }

    static Transform ByName(Transform under, string name)
    {
        for (int i = 0; i < under.childCount; i++)
        {
            var c = under.GetChild(i);
            if (c.name == name) return c;
        }
        for (int i = 0; i < under.childCount; i++)
        {
            var f = ByName(under.GetChild(i), name);
            if (f != null) return f;
        }
        return null;
    }

    void FireMarkers(float prev, float now)
    {
        var ev = clip.events;
        if (ev == null || ev.Length == 0) return;
        bool wrapped = loop && now < prev;
        for (int k = 0; k < ev.Length; k++)
        {
            var e = ev[k];
            if (e == null) continue;
            bool hit = wrapped ? (e.frame > prev || e.frame <= now) : (e.frame > prev && e.frame <= now);
            if (!hit) continue;
            Marker?.Invoke(enemy, clip.name, e.name);
            onMarker?.Invoke(e.name);
        }
    }
}
