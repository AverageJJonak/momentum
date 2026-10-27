using System;
using UnityEngine;

/// <summary>
/// Plays an ARM clip on the first-person rig through ArmRig's public API only. Each hand the clip keys is held on a
/// driver-owned ANCHOR transform under the rig's root (HoldWeapon on the anchor, re-posed from the clip every frame
/// before the rig solves): a hold outranks the weapon file's own Drive/HoldWeapon of the same frame, and because
/// the anchor is a different "weapon" from whatever the hand was on, the rig's own catch-up spring carries the
/// hand into the clip and back out of it when it ends — never a pop, and the weapon file's procedural pose resumes
/// by itself (every weapon re-asks for its hand every frame). The anchors alternate between two pairs so a clip
/// straight after a clip springs too. Hands the clip does not key are untouched. Fingers: the clip's per-finger
/// curls are mapped to the rig's nearest hand state. A weapon passed to Play rides in its hand for the clip's
/// length (ArmClipCarry, after the rig has solved). Markers raise `Marker` (and Play's callback) on their frame.
/// Runs before ArmRig (execution order −10); time is scaled (clips freeze in hitstop with everything else).
/// Opt-in: nothing plays unless a weapon file asks, e.g. at the moment it would run its procedural beat:
///     if (ClipLibrary.Has("cane_slash_1")) ArmClipDriver.Play("cane_slash_1", 1f, false, sword);
/// </summary>
[DefaultExecutionOrder(-10)]
public class ArmClipDriver : MonoBehaviour
{
    public static ArmClipDriver Instance { get; private set; }
    /// (clip name, marker name) on the marker's frame
    public static event Action<string, string> Marker;

    public static bool Playing => Instance != null && Instance.clip != null;
    public static string Current => Instance != null && Instance.clip != null ? Instance.clip.name : null;
    /// 0 … 1 through the clip (a loop wraps)
    public static float Progress => Instance != null && Instance.clip != null && Instance.clip.Duration > 0f ? Mathf.Clamp01(Instance.clip.FrameAt(Instance.t) / Mathf.Max(1, Instance.clip.frames - 1)) : 0f;

    ClipData clip;
    float t, prevFrame, speed;
    bool loop;
    int armFrame;
    Action<string> onMarker;
    ClipBone boneR, boneL;
    ClipFingers fingR, fingL;
    readonly Transform[] anchorsR = new Transform[2], anchorsL = new Transform[2];
    int anchorSet;
    ArmRig rig;
    ArmClipCarry carry;

    /// Play a clip (speed multiplies the clip's own; loop null = the clip's flag). `weapon` is the transform the
    /// hand holds (the cane, the deck, the gun, the maul): it rides in `weaponHand` (null = the clip's weaponHand)
    /// for the clip's length. False when the clip or the rig is missing: the caller keeps its procedural motion
    public static bool Play(string name, float speed = 1f, bool? loop = null, Transform weapon = null, ArmRig.HandSide? weaponHand = null, Action<string> onMarker = null)
    {
        var data = ClipLibrary.Get(name);
        var rig = ArmRig.Instance;
        if (data == null || rig == null || !rig.Built) return false;
        var d = Instance;
        if (d == null)
        {
            d = rig.GetComponent<ArmClipDriver>();
            if (d == null) d = rig.gameObject.AddComponent<ArmClipDriver>();
            Instance = d;
        }
        d.Begin(rig, data, speed, loop ?? data.loop, weapon, weaponHand, onMarker);
        return true;
    }

    /// The clip ends now: the hands spring back to whatever drives them
    public static void Stop() { if (Instance != null) Instance.End(); }

    void Awake() { if (Instance == null) Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    void Begin(ArmRig r, ClipData data, float spd, bool lp, Transform weapon, ArmRig.HandSide? weaponHand, Action<string> cb)
    {
        if (clip != null) End();
        rig = r;
        clip = data;
        speed = Mathf.Max(0.01f, spd) * Mathf.Max(0.01f, data.speed);
        loop = lp;
        t = 0f;
        prevFrame = -1f;
        onMarker = cb;
        boneR = data.Bone("HandR");
        boneL = data.Bone("HandL");
        fingR = data.FingersOf("R");
        fingL = data.FingersOf("L");
        anchorSet = 1 - anchorSet;
        EnsureAnchors();
        // A weapon rides in its hand: the carry measures where the hand sits on it on the frame the rig last
        // solved the weapon's own hold, so the override starts one frame later; without a weapon it starts now
        if (weapon != null)
        {
            if (carry == null) carry = GetComponent<ArmClipCarry>() ?? gameObject.AddComponent<ArmClipCarry>();
            ArmRig.HandSide side = weaponHand ?? (data.weaponHand == "L" ? ArmRig.HandSide.Left : ArmRig.HandSide.Right);
            carry.Begin(rig, weapon, side);
            armFrame = Time.frameCount + 1;
        }
        else armFrame = Time.frameCount;
    }

    void End()
    {
        if (rig != null)
        {
            foreach (var a in anchorsR) if (a != null) rig.Release(a);
            foreach (var a in anchorsL) if (a != null) rig.Release(a);
        }
        if (carry != null) carry.End();
        clip = null;
        onMarker = null;
        boneR = boneL = null;
        fingR = fingL = null;
    }

    void EnsureAnchors()
    {
        for (int i = 0; i < 2; i++)
        {
            if (anchorsR[i] == null) anchorsR[i] = MakeAnchor("ClipAnchorR" + i);
            if (anchorsL[i] == null) anchorsL[i] = MakeAnchor("ClipAnchorL" + i);
        }
    }

    Transform MakeAnchor(string name)
    {
        var a = new GameObject(name).transform;
        a.SetParent(rig.Root, false);
        a.localPosition = Vector3.zero;
        a.localRotation = Quaternion.identity;
        return a;
    }

    void LateUpdate()
    {
        if (clip == null) return;
        if (rig == null || !rig.Built) { End(); return; }
        float dt = Time.deltaTime * speed;
        t += dt;   // the clock runs from the press even on the measuring frame, so the hit frame stays on schedule
        if (Time.frameCount < armFrame) return;
        float frame = clip.FrameAt(t);
        FireMarkers(prevFrame, frame);
        prevFrame = frame;
        if (!loop && t >= clip.Duration) { End(); return; }
        DriveHand(ArmRig.HandSide.Right, boneR, fingR, anchorsR[anchorSet], frame);
        DriveHand(ArmRig.HandSide.Left, boneL, fingL, anchorsL[anchorSet], frame);
    }

    void DriveHand(ArmRig.HandSide side, ClipBone bone, ClipFingers fingers, Transform anchor, float frame)
    {
        if (bone == null || anchor == null) return;                    // no track: the hand keeps its own driver
        if (!clip.SamplePos(bone, frame, out Vector3 p)) return;       // a wrist without positions cannot be placed
        anchor.localPosition = p;
        anchor.localRotation = clip.SampleRot(bone, frame);
        ArmRig.Fingers f = fingers != null ? StateOf(fingers, frame) : ArmRig.Fingers.Relaxed;
        rig.HoldWeapon(anchor, side, Vector3.zero, Quaternion.identity, f, true, true, clip.layerScale, clip.snap, false);
    }

    /// The nearest of the rig's five hand states to the clip's curls (ArmRigFingers' tables: open ≈ 0.04,
    /// relaxed ≈ 0.27, grip ≈ 0.79, fist 1.0; a pinch keeps the index straighter than the rest)
    ArmRig.Fingers StateOf(ClipFingers fingers, float frame)
    {
        float i = clip.Curl(fingers, frame, 0), m = clip.Curl(fingers, frame, 1), r = clip.Curl(fingers, frame, 2), p = clip.Curl(fingers, frame, 3);
        float mean = (i + m + r + p) * 0.25f, others = (m + r + p) / 3f;
        if (mean < 0.14f) return ArmRig.Fingers.Open;
        if (mean < 0.5f) return ArmRig.Fingers.Relaxed;
        if (i < others - 0.15f) return ArmRig.Fingers.Pinch;
        if (mean < 0.9f) return ArmRig.Fingers.Grip;
        return ArmRig.Fingers.Fist;
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
            Marker?.Invoke(clip.name, e.name);
            onMarker?.Invoke(e.name);
        }
    }
}
