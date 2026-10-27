using UnityEngine;

/// <summary>
/// THE ARM RIG (2026-10-08): one pair of jointed arms on the camera. Two shoulders behind the eye, an upper arm and
/// a forearm each (charcoal sleeve, cream cuff, brass cufflink), a wrist, and a HAND in grey suede with four
/// three-jointed fingers and a thumb. The knuckles (FistRig) drive the hands directly (Drive); a driver may also put
/// a hand on a held transform (HoldWeapon: the clip drivers' anchors). The rig turns those into hand targets, lays
/// its procedural layers over them (ArmRigLayers: sway, bob, breath, land, jump, dash, slide, wall plant, sprint
/// pump, tackle, hit shudder), bends each arm to its hand with a two-bone IK (ArmRigSolve), then re-poses a held
/// transform from the hand so it carries the layers with it. Fingers are curl states blended with a ripple
/// (ArmRigFingers). A hold is sticky: it lasts until released or hidden, so a disabled driver (a cutscene) leaves
/// the hand where it was. A hand nobody drives idles low in its corner, breathing. A hand someone else moves
/// directly (the opening cutscene writes HandR/HandL by name) is followed: the arm bends to wherever it was put.
/// Camera space throughout (the root sits at the camera's origin); everything on the Viewmodel layer; unscaled time.
/// The whole tree lives under "Gauntlets" (the opening cutscene finds it by that name, and "HandR"/"HandL" under it).
/// </summary>
public partial class ArmRig : MonoBehaviour
{
    public enum HandSide { Right = 0, Left = 1 }
    /// The finger curl states: FIST (the knuckles), GRIP (a handle), PINCH (a card), OPEN (a wall, a guard), RELAXED (idle)
    public enum Fingers { Relaxed, Open, Fist, Grip, Pinch }

    public struct ArmPose
    {
        public Vector3 pos;
        public Quaternion rot;
        public ArmPose(Vector3 p, Quaternion r) { pos = p; rot = r; }
        public ArmPose(Vector3 p, Vector3 euler) { pos = p; rot = Quaternion.Euler(euler); }
        /// The same pose for the other hand: across the view
        public ArmPose Mirrored()
        {
            Vector3 e = rot.eulerAngles;
            return new ArmPose(new Vector3(-pos.x, pos.y, pos.z), new Vector3(e.x, -e.y, -e.z));
        }
        public static ArmPose Lerp(ArmPose a, ArmPose b, float t) => new ArmPose(Vector3.LerpUnclamped(a.pos, b.pos, t), Quaternion.SlerpUnclamped(a.rot, b.rot, t));
    }

    enum Source { None, Hold, Direct, External, Idle }

    public static ArmRig Instance { get; private set; }
    public PlayerMotor motor;
    public MovementConfig config;
    /// "Gauntlets": the root of the whole viewmodel under the camera, at its origin
    public Transform Root { get; private set; }
    public bool Built => Root != null;

    partial class Hand
    {
        public HandSide side;
        public float sign;            // +1 the right hand, -1 the left
        public Transform hand;        // HandR / HandL: the wrist, a child of the root in camera space
        // A hold: the hand sits on a weapon. Main holds snapshot the weapon's authored pose and re-pose it after the
        // solve; followers read the weapon live (a second hand on the same weapon, or a hand on a moving part of it)
        public Transform holdWeapon;
        public ArmPose holdAuthored, handInWeapon;
        public Fingers holdFingers;
        public bool holdMain, holdStretch, holdSnap, holdFree;
        public float holdLayer = 1f;
        // A direct target, good for this frame only
        public int directFrame = -1;
        public ArmPose direct;
        public Fingers directFingers;
        public bool directSnap, directStretch, directFree;
        public float directLayer = 1f;
        // What was decided and written last frame
        public Source source = Source.None;
        public Transform sourceWeapon;
        public ArmPose written, outPose;
        public bool everWritten;
        public float externalUntil = -1f;
        public bool free;             // nothing in this hand this frame: it may plant on a wall or reach for the ground
        // The catch-up after a change of source: the difference between where the hand was and where it is now asked
        // to be, sprung to nothing so no change ever pops
        public Vector3 catchPos, catchPosVel, catchRot, catchRotVel;
    }

    readonly Hand[] hands = { new Hand(), new Hand() };
    Hand HandOf(HandSide s) => hands[(int)s];

    /// The rig on the camera, made once (the bootstrap asks for it)
    public static ArmRig Ensure(GameObject cameraGo, PlayerMotor motor, MovementConfig config)
    {
        var rig = cameraGo.GetComponent<ArmRig>();
        if (rig == null) rig = cameraGo.AddComponent<ArmRig>();
        rig.motor = motor;
        rig.config = config;
        if (!rig.Built) rig.Build();
        Instance = rig;
        return rig;
    }

    void Awake() { if (Instance == null) Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    public Transform HandTransform(HandSide s) => HandOf(s).hand;

    /// Drive a hand to a pose in the camera's space for this frame (the knuckles, a free hand's own motion).
    /// `free` lets the layers plant it on a wall or send it to the ground; `stretch` lets the arm lengthen past
    /// its reach rather than pull the hand in; `layerScale` scales the shared layers on it (0 during a cut)
    public void Drive(HandSide side, Vector3 pos, Quaternion rot, Fingers fingers, bool snap = false, bool stretch = false, float layerScale = 1f, bool free = false)
    {
        var h = HandOf(side);
        h.directFrame = Time.frameCount;
        h.direct = new ArmPose(pos, rot);
        h.directFingers = fingers;
        h.directSnap = snap;
        h.directStretch = stretch;
        h.directLayer = layerScale;
        h.directFree = free;
    }

    /// Put a hand on a weapon: `handInWeapon` is where the wrist sits in the weapon's own frame. The main hand's
    /// weapon has its authored pose (as its transform stands right now, in camera space) snapshotted every call and
    /// is re-posed from the hand after the solve; a follower (main = false) is read live after the main hand has
    /// moved the weapon, so a second hand, or a hand on the gun's breaking barrels, stays on. Sticky until Release
    /// or the weapon is hidden
    public void HoldWeapon(Transform weapon, HandSide side, Vector3 handPosInWeapon, Quaternion handRotInWeapon, Fingers fingers,
        bool main = true, bool stretch = false, float layerScale = 1f, bool snap = false, bool free = false)
    {
        if (weapon == null) return;
        var h = HandOf(side);
        h.holdWeapon = weapon;
        h.handInWeapon = new ArmPose(handPosInWeapon, handRotInWeapon);
        h.holdFingers = fingers;
        h.holdMain = main;
        h.holdStretch = stretch;
        h.holdLayer = layerScale;
        h.holdSnap = snap;
        h.holdFree = free;
        if (main) h.holdAuthored = RootPose(weapon);
    }

    public void Release(Transform weapon)
    {
        foreach (var h in hands) if (h.holdWeapon == weapon) h.holdWeapon = null;
    }

    public void Release(HandSide side) => HandOf(side).holdWeapon = null;

    /// True while the hand is on this weapon
    public bool Holding(HandSide side, Transform weapon) => HandOf(side).holdWeapon == weapon;

    bool HoldLive(Hand h) => h.holdWeapon != null && h.holdWeapon.gameObject.activeInHierarchy;
    bool Follower(Hand h) => HoldLive(h) && !h.holdMain;

    void LateUpdate()
    {
        if (!Built) return;
        bool hide = BossIntro.Active || BossOutro.Active; // the cinematics own the camera: no arms across their picture
        if (Root.gameObject.activeSelf == hide) Root.gameObject.SetActive(!hide);
        if (hide || PauseMenu.IsPaused) return;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        if (dt <= 0f) return;
        // The root's locomotion is damped by the quietest hand's layer scale: a cut that asks for stillness (the
        // cane's calibrated tip) gets it from the torso too
        rootScale = Mathf.Min(LayerOf(hands[0]), LayerOf(hands[1]));
        UpdateLayers(dt);
        // The hands that decide where their weapon goes first; the followers after, off the weapon's new pose
        foreach (var h in hands) if (!Follower(h)) SolveHand(h, dt, false);
        foreach (var h in hands) if (Follower(h)) SolveHand(h, dt, true);
    }

    void SolveHand(Hand h, float dt, bool follower)
    {
        ArmPose target = BaseTarget(h, out Fingers fingers, out bool snap, out bool stretch, out float layer, out Source source, out Transform weapon);
        if (source != h.source || weapon != h.sourceWeapon)
        {
            // A new driver: carry the hand from where it is, and let the difference spring away
            if (h.everWritten)
            {
                h.catchPos = h.outPose.pos - target.pos;
                h.catchRot = ToRotVec(Quaternion.Inverse(target.rot) * h.outPose.rot);
                h.catchPosVel = h.catchRotVel = Vector3.zero;
            }
            h.source = source;
            h.sourceWeapon = weapon;
        }
        // The catch-up: a damped spring, never a pop
        float omega = config != null ? Mathf.Max(1f, config.viewmodelBlendRate) : 14f;
        Spring(ref h.catchPos, ref h.catchPosVel, Vector3.zero, omega, 1f, dt);
        Spring(ref h.catchRot, ref h.catchRotVel, Vector3.zero, omega, 1f, dt);
        target.pos += h.catchPos;
        target.rot = target.rot * FromRotVec(h.catchRot);
        h.free = source == Source.Idle || (source == Source.Direct && h.directFree) || (source == Source.Hold && h.holdFree);
        if (!follower)
        {
            ApplyShared(ref target, layer);
            PerHand(h, ref target, layer, ref fingers);
        }
        // A bare fist is kept on the forearm's line; a hand on a weapon is exactly where the weapon puts it
        float wristClamp = fingers == Fingers.Fist && source != Source.Hold ? (config != null ? Mathf.Max(0f, config.viewmodelFistWristFlex) : 20f) : 0f;
        SolveArm(h, ref target, stretch, follower, wristClamp, dt);
        if (source == Source.Hold && !follower) RePose(h, target);
        h.hand.localPosition = target.pos;
        h.hand.localRotation = target.rot;
        h.written = target;
        h.outPose = target;
        h.everWritten = true;
        UpdateFingers(h, fingers, snap, dt);
    }

    /// The layer scale this hand's driver asked for this frame (1 when nobody did)
    float LayerOf(Hand h) => HoldLive(h) ? h.holdLayer : h.directFrame == Time.frameCount ? h.directLayer : 1f;

    /// What drives this hand right now: its hold, this frame's direct target, someone else's write, or the idle
    ArmPose BaseTarget(Hand h, out Fingers fingers, out bool snap, out bool stretch, out float layer, out Source source, out Transform weapon)
    {
        weapon = null;
        if (HoldLive(h))
        {
            ArmPose w = h.holdMain ? h.holdAuthored : RootPose(h.holdWeapon);
            fingers = h.holdFingers; snap = h.holdSnap; stretch = h.holdStretch; layer = h.holdLayer;
            source = Source.Hold; weapon = h.holdWeapon;
            return Compose(w, h.handInWeapon);
        }
        if (h.directFrame == Time.frameCount)
        {
            fingers = h.directFingers; snap = h.directSnap; stretch = h.directStretch; layer = h.directLayer;
            source = Source.Direct;
            return h.direct;
        }
        // Moved by someone else since the last write (the opening cutscene): follow it, and keep following for a
        // beat so a pose held still is not mistaken for silence
        Vector3 p = h.hand.localPosition;
        Quaternion r = h.hand.localRotation;
        bool moved = h.everWritten && ((p - h.written.pos).sqrMagnitude > 1e-10f || Quaternion.Angle(r, h.written.rot) > 0.02f);
        if (moved) h.externalUntil = Time.unscaledTime + 0.3f;
        if (moved || Time.unscaledTime < h.externalUntil)
        {
            fingers = Fingers.Fist; snap = false; stretch = true; layer = 0.5f;
            source = Source.External;
            return new ArmPose(p, r);
        }
        fingers = Fingers.Relaxed; snap = false; stretch = false; layer = 1f;
        source = Source.Idle;
        return IdlePose(h.side);
    }

    // The relaxed hand: low in its corner and close in (THE FINALS' hold), continuing the forearm's line (forward
    // and a little up, 12° inward), the back of the hand rolled half-way out so the thumb rides up; out of the
    // crosshair (the left mirrors it)
    static readonly ArmPose IdleR = new ArmPose(new Vector3(0.21f, -0.2f, 0.38f), new Vector3(-20f, -12f, -40f));
    static ArmPose IdlePose(HandSide s) => s == HandSide.Right ? IdleR : IdleR.Mirrored();

    /// A transform's pose in the root's (the camera's) space
    public ArmPose RootPose(Transform t) => new ArmPose(Root.InverseTransformPoint(t.position), Quaternion.Inverse(Root.rotation) * t.rotation);

    static ArmPose Compose(ArmPose a, ArmPose b) => new ArmPose(a.pos + a.rot * b.pos, a.rot * b.rot);
    static ArmPose Inverse(ArmPose a) { Quaternion ir = Quaternion.Inverse(a.rot); return new ArmPose(-(ir * a.pos), ir); }

    static Vector3 ToRotVec(Quaternion q)
    {
        q.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;
        if (float.IsNaN(axis.x) || axis.sqrMagnitude < 1e-6f) return Vector3.zero;
        return axis.normalized * angle;
    }

    static Quaternion FromRotVec(Vector3 v)
    {
        float a = v.magnitude;
        return a < 1e-4f ? Quaternion.identity : Quaternion.AngleAxis(a, v / a);
    }

    /// A damped spring toward `target` (semi-implicit Euler, sub-stepped so a long frame never blows it up): ω rad/s, ζ 1 = no overshoot
    static void Spring(ref float x, ref float v, float target, float omega, float zeta, float dt)
    {
        int steps = Mathf.Clamp(Mathf.CeilToInt(dt * omega * 2f), 1, 8);
        float h = dt / steps;
        for (int i = 0; i < steps; i++)
        {
            v += (-omega * omega * (x - target) - 2f * zeta * omega * v) * h;
            x += v * h;
        }
    }

    static void Spring(ref Vector3 x, ref Vector3 v, Vector3 target, float omega, float zeta, float dt)
    {
        int steps = Mathf.Clamp(Mathf.CeilToInt(dt * omega * 2f), 1, 8);
        float h = dt / steps;
        for (int i = 0; i < steps; i++)
        {
            v += (-omega * omega * (x - target) - 2f * zeta * omega * v) * h;
            x += v * h;
        }
    }

    /// Hz and a damping ratio → ω (the config speaks in Hz)
    static float Omega(float hz) => 2f * Mathf.PI * Mathf.Max(0.1f, hz);
}
