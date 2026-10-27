using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE CLIP PIPELINE (2026-10-16): authored animation from Blender, played on top of the procedural rigs. A clip is
/// a JSON TextAsset in Resources/Clips (written by Tools/Blender/momentum_clip_export.py; see
/// Tools/Animation/ANIMATOR_GUIDE.md): per bone a flat array of local positions (3 per frame, or none) and
/// rotations (4 per frame, Unity x y z w), the arm rig's finger curls summarised per finger (5 per frame per hand:
/// index, middle, ring, pinky, thumb, 0 straight … 1 closed), a loop flag and named events on frames. The data is
/// kept exactly as loaded and sampled by index with interpolation: no per-frame allocation anywhere.
/// ClipLibrary loads every clip once, keyed by its name (the JSON's `name`, else the asset's), and `Has` is the
/// gate the weapon and enemy files use before asking a driver to play (ArmClipDriver, EnemyClipDriver).
/// </summary>
[Serializable]
public class ClipBone
{
    public string name;
    public float[] pos = new float[0];   // frames × 3, or empty: the bone's position is left to the game
    public float[] rot = new float[0];   // frames × 4: x y z w
}

[Serializable]
public class ClipFingers
{
    public string hand = "R";           // "R" or "L"
    public float[] curl = new float[0]; // frames × 5: index, middle, ring, pinky, thumb (0 straight … 1 closed)
}

[Serializable]
public class ClipEvent
{
    public int frame;
    public string name;
}

[Serializable]
public class ClipData
{
    public string name;
    public string rig = "arms";          // "arms", or the enemy kind's code name in lowercase ("chaser")
    public int fps = 60;
    public int frames;                   // samples: frame 0 … frames−1
    public bool loop;
    public float speed = 1f;             // the authored playback speed (Play's speed multiplies it)
    public float blendIn = 0.1f, blendOut = 0.1f;   // enemies: the fade over the procedural pose (the arms spring by themselves)
    public float layerScale = 1f;        // arms: the rig's procedural layers' scale while the clip plays (0.3 = a calibrated cut)
    public bool snap;                    // arms: fingers change state at the jab's snap rate rather than rippling
    public string weaponHand = "R";      // arms: the hand the held weapon rides on (the deck's is "L")
    public ClipBone[] bones = new ClipBone[0];
    public ClipFingers[] fingers = new ClipFingers[0];
    public ClipEvent[] events = new ClipEvent[0];

    /// Seconds: a loop runs from its last frame back into its first; a one-shot ends on its last frame
    public float Duration => frames <= 1 || fps <= 0 ? 0f : (loop ? frames : frames - 1) / (float)fps;

    /// The fractional frame at a time: wrapped for a loop, held on the last frame for a one-shot
    public float FrameAt(float t)
    {
        if (frames <= 1 || fps <= 0) return 0f;
        float f = t * fps;
        if (loop)
        {
            f %= frames;
            if (f < 0f) f += frames;
            return f;
        }
        return Mathf.Clamp(f, 0f, frames - 1);
    }

    void Indices(float frame, out int i0, out int i1, out float k)
    {
        i0 = Mathf.Clamp(Mathf.FloorToInt(frame), 0, Mathf.Max(0, frames - 1));
        k = Mathf.Clamp01(frame - i0);
        i1 = loop ? (i0 + 1) % Mathf.Max(1, frames) : Mathf.Min(i0 + 1, frames - 1);
    }

    /// The bone's position on a fractional frame; false when the bone carries no position keys
    public bool SamplePos(ClipBone b, float frame, out Vector3 p)
    {
        p = Vector3.zero;
        if (b == null || b.pos == null || b.pos.Length < frames * 3 || frames <= 0) return false;
        Indices(frame, out int i0, out int i1, out float k);
        var a = new Vector3(b.pos[i0 * 3], b.pos[i0 * 3 + 1], b.pos[i0 * 3 + 2]);
        var c = new Vector3(b.pos[i1 * 3], b.pos[i1 * 3 + 1], b.pos[i1 * 3 + 2]);
        p = Vector3.LerpUnclamped(a, c, k);
        return true;
    }

    /// The bone's rotation on a fractional frame (identity when the bone carries no rotation keys)
    public Quaternion SampleRot(ClipBone b, float frame)
    {
        if (b == null || b.rot == null || b.rot.Length < frames * 4 || frames <= 0) return Quaternion.identity;
        Indices(frame, out int i0, out int i1, out float k);
        var a = new Quaternion(b.rot[i0 * 4], b.rot[i0 * 4 + 1], b.rot[i0 * 4 + 2], b.rot[i0 * 4 + 3]);
        var c = new Quaternion(b.rot[i1 * 4], b.rot[i1 * 4 + 1], b.rot[i1 * 4 + 2], b.rot[i1 * 4 + 3]);
        return k <= 0f ? a : Quaternion.Slerp(a, c, k);
    }

    /// One finger's curl (0 index … 3 pinky, 4 the thumb) on a fractional frame, 0 straight … 1 closed
    public float Curl(ClipFingers f, float frame, int finger)
    {
        if (f == null || f.curl == null || f.curl.Length < frames * 5 || frames <= 0) return 0f;
        Indices(frame, out int i0, out int i1, out float k);
        return Mathf.LerpUnclamped(f.curl[i0 * 5 + finger], f.curl[i1 * 5 + finger], k);
    }

    public ClipBone Bone(string boneName)
    {
        if (bones == null) return null;
        for (int i = 0; i < bones.Length; i++) if (bones[i] != null && bones[i].name == boneName) return bones[i];
        return null;
    }

    public ClipFingers FingersOf(string hand)
    {
        if (fingers == null) return null;
        for (int i = 0; i < fingers.Length; i++) if (fingers[i] != null && fingers[i].hand == hand) return fingers[i];
        return null;
    }

    /// Drops the tracks whose arrays do not match the frame count (a half-written clip never indexes out of range)
    public bool Validate(out string why)
    {
        why = null;
        if (frames <= 0) { why = "no frames"; return false; }
        if (fps <= 0) fps = 60;
        var keep = new List<ClipBone>();
        foreach (var b in bones ?? new ClipBone[0])
        {
            if (b == null || string.IsNullOrEmpty(b.name)) continue;
            bool rotOk = b.rot != null && b.rot.Length == frames * 4;
            bool posOk = b.pos == null || b.pos.Length == 0 || b.pos.Length == frames * 3;
            if (!rotOk || !posOk) { why = (why == null ? "" : why + "; ") + "bone " + b.name + " has the wrong key count"; continue; }
            keep.Add(b);
        }
        bones = keep.ToArray();
        var fk = new List<ClipFingers>();
        foreach (var f in fingers ?? new ClipFingers[0])
            if (f != null && f.curl != null && f.curl.Length == frames * 5) fk.Add(f);
        fingers = fk.ToArray();
        if (events == null) events = new ClipEvent[0];
        return bones.Length > 0 || fingers.Length > 0;
    }
}

/// Every clip in Resources/Clips, loaded once
public static class ClipLibrary
{
    static Dictionary<string, ClipData> clips;

    public static bool Has(string name) => !string.IsNullOrEmpty(name) && Load().ContainsKey(name);

    /// The clip, or null (never throws: the drivers fall back to the procedural motion)
    public static ClipData Get(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return Load().TryGetValue(name, out var c) ? c : null;
    }

    public static int Count => Load().Count;
    public static IEnumerable<string> Names => Load().Keys;

    /// Forget everything and read Resources/Clips again (after dropping a new clip in while playing in the editor)
    public static void Reload() { clips = null; Load(); }

    static Dictionary<string, ClipData> Load()
    {
        if (clips != null) return clips;
        clips = new Dictionary<string, ClipData>();
        var assets = Resources.LoadAll<TextAsset>("Clips");
        foreach (var a in assets)
        {
            if (a == null || string.IsNullOrEmpty(a.text) || !a.text.TrimStart().StartsWith("{")) continue;
            ClipData c;
            try { c = JsonUtility.FromJson<ClipData>(a.text); }
            catch (Exception e) { Debug.LogWarning("Clips: " + a.name + " is not a clip (" + e.Message + ")"); continue; }
            if (c == null) continue;
            if (string.IsNullOrEmpty(c.name)) c.name = a.name;
            if (!c.Validate(out string why)) { Debug.LogWarning("Clips: " + c.name + " skipped: " + (why ?? "no tracks")); continue; }
            if (why != null) Debug.LogWarning("Clips: " + c.name + ": " + why);
            clips[c.name] = c;
        }
        return clips;
    }
}
