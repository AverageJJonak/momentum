using UnityEngine;

/// <summary>
/// THE KNUCKLES on the arm rig (2026-10-08; v3 2026-10-16: the combo's three beats and THE KICK in THE FINALS' grammar;
/// the one kit since the simplification): the viewmodel of every run. The hands
/// are the rig's own (ArmRig); this drives them, after ULTRAKILL's Feedbacker:
///  STANCE — a southpaw's loose guard, low in the frame and close in, never symmetric: the right leads, the left is
///           the rear hand by the chin; breathing and swaying with the rig, free to plant on a wall or reach for the ground.
///  JAB    — Fists.cs lands the hit THE FRAME YOU PRESS, so the fist has no time to wind up: a flick back
///           (viewmodelJabAnticipation), the SNAP to full extension with the elbow straightening and the shoulder
///           rolling in (viewmodelJabSnap: inside the knuckle flash and the hitstop), the follow-through past the
///           target on a whiff or a bite and shudder on a connect, and the recovery with an overshoot into the stance.
///  CROSS  — the rear (left) hand, the same grammar bigger (viewmodelCrossScale), and THE ROOT TURNS with it
///           (viewmodelCrossRootYaw): the rear shoulder comes through.
///  HOOK   — the right fist SWEEPS ACROSS THE WHOLE FRAME, right to left, the knuckles facing the way it travels, the
///           body whipping round with it (viewmodelHookRootYaw) and the view following; the follow-through sails past
///           the far edge on a whiff, bites on a connect.
///  KICK   — there are no legs on the rig, so the kick is the BODY's: the camera dips and pushes forward
///           (viewmodelKickDip / viewmodelKickPush), the root pitches into it (viewmodelKickRootPitch), both fists
///           pull back to the chin, and a BOOT — a dark shoe and shin on the rig's root — swings up through the lower
///           right of the frame and drops away. A springboard throws the root up after it.
///  GUARD  — Sweep into crossed forearms, Hold while the parry window is open, the Impact flick on a connect, Recover
///           (the movement parry is everyone's: the forearms come up on every press).
///  TACKLE — both fists brace forward and the right shoulder drops into the frame (the rig's own tackle spring).
/// The knuckle edge is state: cyan while a body is in THE WINDOW, white on a connect, gold on a perfect tackle or a
/// perfect springboard. Unscaled time: it moves through hitstop. The mechanics and timings are Fists.cs's, untouched.
/// </summary>
public class FistRig : MonoBehaviour
{
    /// The bootstrap adds the hands on the camera, beside the arm rig
    public static FistRig Add(Camera cam, PlayerMotor motor, MovementConfig config)
    {
        var rig = cam.gameObject.AddComponent<FistRig>();
        rig.motor = motor;
        rig.config = config;
        return rig;
    }

    // THE VECTOR's colour ramp (the opening cutscene drives the knuckle edges along it): asleep, the flow's gold, white-hot
    static readonly Color VeinDim = new Color(0.34f, 0.2f, 0.08f), VeinGold = new Color(2.6f, 1.5f, 0.45f), VeinWhite = new Color(3.6f, 3.2f, 2.4f);
    /// The vector's colour at a glow: 0 asleep, 1 the flow's gold, 2 white-hot (the subject's own ramp, PlayerAvatar)
    public static Color VectorAt(float glow) => glow <= 1f ? Color.Lerp(VeinDim, VeinGold, glow) : Color.Lerp(VeinGold, VeinWhite, Mathf.Clamp01(glow - 1f));

    const float SweepTime = 0.08f, ImpactHold = 0.26f, GhostInterval = 0.02f;
    static float EaseOutCubic(float x) { float y = 1f - x; return 1f - y * y * y; }
    static float EaseOutQuart(float x) { float y = 1f - x; return 1f - y * y * y * y; }
    static float EaseInOut(float x) => x * x * (3f - 2f * x);

    public PlayerMotor motor;
    public MovementConfig config;

    enum Beat { Stance, Sweep, Hold, Impact, Recover, Anticipate, Snap, Follow, Return, Hook, Kick }

    // THE HOOK (v3: the combo's third beat): from the stance the right fist crosses the WHOLE frame to the far side,
    // the knuckles facing the way it travels, the back of the hand rolled out; the follow-through past it. Authored
    // in the fists' own frame (+Z along the knuckles, +Y the back of the hand, the forearm continued)
    static readonly ArmRig.ArmPose HookR = new ArmRig.ArmPose(new Vector3(-0.16f, -0.09f, 0.62f), new Vector3(-10f, -32f, -30f));
    static readonly ArmRig.ArmPose HookFollowR = new ArmRig.ArmPose(new Vector3(-0.3f, -0.12f, 0.56f), new Vector3(-8f, -48f, -30f));
    const float HookTime = 0.07f;

    // THE STANCE (2026-10-12, THE FINALS' grammar): a SOUTHPAW's loose guard, low in the frame and close in, never
    // symmetric — the RIGHT hand LEADS (forward, nearer the centre, a little higher: it jabs), the LEFT is the REAR
    // hand (back by the chin at the lower-left corner, closer: it crosses, and the cross turns the whole body).
    // Both fists three-quarters vertical (the thumb riding up), elbows in and down (the rig's rest pole).
    // The hand frame is +Z along the knuckles, +Y the back of the hand: every fist here continues its forearm's
    // line from its own shoulder (±0.27, −0.30, −0.12) with a few degrees of flex, so the wrist stays straight
    // (the rig's elbow follows the wrist). The other poses are the right's, mirrored for the left
    // THE ARMS AT YOUR SIDES (2026-10-27, user: "get rid of the hands, make them run by your side like mirrors edge"): the
    // stance is no guard now — both hands hang low and wide at the bottom corners of the frame, nearly out of it, and the
    // rig's pump layer swings them up into the corners as you run (viewmodelPumpOverRun, from half run speed)
    static readonly ArmRig.ArmPose StanceR = new ArmRig.ArmPose(new Vector3(0.34f, -0.44f, 0.24f), new Vector3(-6f, -28f, -72f));   // the right, at the side
    static readonly ArmRig.ArmPose StanceL = new ArmRig.ArmPose(new Vector3(-0.34f, -0.44f, 0.24f), new Vector3(-6f, 28f, 72f));   // the left, at the side
    static readonly ArmRig.ArmPose GuardR = new ArmRig.ArmPose(new Vector3(-0.02f, -0.02f, 0.36f), new Vector3(-55f, 5f, -30f));
    static readonly ArmRig.ArmPose ImpactR = new ArmRig.ArmPose(new Vector3(-0.05f, 0.02f, 0.40f), new Vector3(-62f, 5f, -35f));
    // The flick back before a punch: toward the shoulder and down, the knuckles turning out; the extension turns
    // the fist over (the corkscrew: three-quarters vertical → nearly flat) and lands just under the crosshair; the
    // follow-through sails past it, flatter still
    static readonly ArmRig.ArmPose AnticR = new ArmRig.ArmPose(new Vector3(0.21f, -0.19f, 0.36f), new Vector3(-16f, -20f, -58f));
    static readonly ArmRig.ArmPose ExtR = new ArmRig.ArmPose(new Vector3(0.03f, -0.09f, 0.66f), new Vector3(-8f, -4f, -12f));
    static readonly ArmRig.ArmPose FollowR = new ArmRig.ArmPose(new Vector3(0.0f, -0.1f, 0.72f), new Vector3(-5f, -1f, -6f));
    static readonly ArmRig.ArmPose ShoveR = new ArmRig.ArmPose(new Vector3(0.18f, -0.14f, 0.60f), new Vector3(-15f, -12f, -25f));
    // THE KICK: both fists pulled up and back to the chin (the guard a boxer keeps while the leg goes), the lead a little higher
    static readonly ArmRig.ArmPose KickR = new ArmRig.ArmPose(new Vector3(0.17f, -0.06f, 0.34f), new Vector3(-34f, -14f, -52f));
    static readonly ArmRig.ArmPose KickL = new ArmRig.ArmPose(new Vector3(-0.18f, -0.1f, 0.3f), new Vector3(-36f, 14f, 58f));
    // THE BOOT (camera space, the ankle): off the frame below at rest, up through the lower right at the kick's peak
    static readonly ArmRig.ArmPose BootRest = new ArmRig.ArmPose(new Vector3(0.22f, -1.1f, 0.5f), new Vector3(10f, -6f, 0f));
    static readonly ArmRig.ArmPose BootKick = new ArmRig.ArmPose(new Vector3(0.16f, -0.36f, 0.72f), new Vector3(55f, -8f, -6f));
    const float KickUp = 0.07f, KickHold = 0.05f, KickDown = 0.24f;
    const float ConnectFlash = 0.1f, PerfectFlash = 0.25f, ExecuteHold = 0.28f, GuardRecover = 0.27f;

    Beat beat = Beat.Stance;
    float beatStart;
    float lastSeenPress = float.NegativeInfinity;
    float ghostTimer;
    string outcome = "MISS";
    int hand;            // 0 = the right (the jab, the hook), 1 = the left (the cross)
    bool shove, big;     // the tackle: both hands; the cross and the lunge: bigger
    bool init;
    ArmRig.ArmPose fromR, fromL, lastR, lastL;
    Material knuckleDim, knuckleCyan, knuckleWhite, knuckleGold, current;
    static Material fistGhostMat;
    Transform boot;      // the kick's silhouette, under the rig's root

    bool Active => Fists.Instance != null && Fists.Instance.Active && !OpeningCutscene.Active && !BossIntro.Active && !BossOutro.Active; // the cutscenes drive or hide the hands themselves

    float Anticipation => config != null ? Mathf.Max(0f, config.viewmodelJabAnticipation) : 0.03f;
    float SnapTime => config != null ? Mathf.Max(0.01f, config.viewmodelJabSnap) : 0.06f;
    float FollowTime => config != null ? Mathf.Max(0.01f, config.viewmodelJabFollow) : 0.06f;
    float ReturnTime => config != null ? Mathf.Max(0.05f, config.viewmodelJabRecover) : 0.22f;
    float Overshoot => config != null ? Mathf.Clamp(config.viewmodelJabOvershoot, 0f, 0.6f) : 0.18f;
    float Big => config != null ? Mathf.Max(1f, config.viewmodelCrossScale) : 1.15f;

    /// The arm rig is built here, in Start, never in Awake: the bootstrap has set config and motor by then
    void Start() => ArmRig.Ensure(gameObject, motor, config);

    void OnEnable() => MovementEvents.MoveExecuted += OnMove;
    void OnDisable() { MovementEvents.MoveExecuted -= OnMove; if (boot != null) boot.gameObject.SetActive(false); }

    void Materials()
    {
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        knuckleDim = new Material(unlit); knuckleDim.SetColor("_BaseColor", new Color(0.08f, 0.2f, 0.22f));   // the edge, unlit: nothing in reach
        knuckleCyan = new Material(unlit); knuckleCyan.SetColor("_BaseColor", new Color(0.35f, 2.6f, 3f));    // cut here
        knuckleWhite = new Material(unlit); knuckleWhite.SetColor("_BaseColor", new Color(3f, 3f, 3f));      // the connect
        knuckleGold = new Material(unlit); knuckleGold.SetColor("_BaseColor", new Color(3f, 2.3f, 0.9f));    // the perfect tackle, the perfect springboard
        current = null;
    }

    /// THE BOOT: a dark lacquer shoe with a brass toe-cap on a charcoal shin, the ankle its root (+Y up the shin,
    /// +Z the toe); it lives under the rig's root so the torso's lean carries it, and shows only for the kick
    void BuildBoot(ArmRig rig)
    {
        boot = new GameObject("Boot").transform;
        boot.SetParent(rig.Root, false);
        Material shin = RigDesign.Lit(new Color(0.1f, 0.1f, 0.12f), 0.05f, 0.35f);     // charcoal worsted
        Material shoe = RigDesign.Lit(new Color(0.05f, 0.04f, 0.04f), 0.2f, 0.85f);    // black lacquer
        RigDesign.Disc(boot, new Vector3(0f, -0.2f, -0.02f), 0.11f, 0.4f, shin, Viewmodel.Layer).transform.localRotation = Quaternion.Euler(0f, 0f, 0f); // the shin, a cylinder up its own Y
        var foot = new GameObject("Shoe").transform;
        foot.SetParent(boot, false);
        foot.localRotation = Quaternion.Euler(-60f, 0f, 0f);   // flexed: the toes up, the sole to the target
        RigDesign.Piece(foot, new Vector3(0f, -0.035f, 0.1f), new Vector3(0.1f, 0.07f, 0.27f), shoe, Viewmodel.Layer);
        RigDesign.Piece(foot, new Vector3(0f, -0.045f, 0.235f), new Vector3(0.096f, 0.05f, 0.05f), RigDesign.Brass, Viewmodel.Layer);   // the brass toe-cap
        RigDesign.Piece(foot, new Vector3(0f, -0.072f, 0.09f), new Vector3(0.104f, 0.012f, 0.29f), shin, Viewmodel.Layer);               // the sole
        RigDesign.Piece(foot, new Vector3(0f, 0.01f, -0.02f), new Vector3(0.108f, 0.06f, 0.09f), shin, Viewmodel.Layer);                 // the ankle, the trouser's break
        Viewmodel.Apply(boot);
        boot.gameObject.SetActive(false);
    }

    void OnMove(MoveType type, string detail)
    {
        if (!init || !Active) return;
        if (type == MoveType.Parry) Set(Beat.Impact);
        else if (type == MoveType.ParryWhiff) { if (beat == Beat.Hold) Set(Beat.Recover); }
        else if (type == MoveType.Kick)
        {
            // THE KICK: the body's — the view dips and shoves forward, the root pitches into it, the fists come
            // up to the chin and the boot swings through the lower frame. A springboard throws the root up after it
            string d = detail ?? "MISS";
            bool sprang = d.StartsWith("SPRINGBOARD");
            outcome = sprang ? "HIT" : d == "LAUNCH" ? "HIT" : d;
            shove = false; big = false; hand = 0;
            Set(Beat.Kick);
            var rig = ArmRig.Instance;
            float pitch = config != null ? config.viewmodelKickRootPitch : 6f;
            if (rig != null) rig.Lean(new Vector3(0f, -0.015f, 0.04f), new Vector3(pitch, -2f, 1.5f));
            var look = motor != null ? motor.look : null;
            if (look != null)
            {
                look.AddPitchKick(-(config != null ? config.viewmodelKickDip : 3f));
                look.AddPushKick(config != null ? config.viewmodelKickPush : 0.05f);
            }
            if (sprang && rig != null) rig.Lean(new Vector3(0f, 0.04f, -0.02f), new Vector3(-5f, 0f, 0f));
        }
        else if (type == MoveType.Fist)
        {
            outcome = detail ?? "MISS";
            shove = outcome == "TACKLE" || outcome == "PERFECT TACKLE";
            var rig = ArmRig.Instance;
            if (shove)
            {
                // no wind-up: the body is already through; the torso drops into the shoulder
                Set(Beat.Snap); ghostTimer = 0f;
                if (rig != null) rig.Lean(new Vector3(0f, -0.02f, 0.04f), new Vector3(3f, -2f, 4f));
                FollowPunch(0f, 0f, 1f);
                return;
            }
            string step = Fists.LastStep;
            if (step == "HOOK")
            {
                // THE HOOK: from the stance, across the frame; the body whips round with it (the torso turns left,
                // leans left) and the view follows it across
                hand = 0; big = true;
                Set(Beat.Hook);
                ghostTimer = 0f;
                float hookYaw = config != null ? config.viewmodelHookRootYaw : 9f;
                if (rig != null) rig.Lean(new Vector3(-0.03f, 0f, 0.03f), new Vector3(1f, -hookYaw, hookYaw * 0.35f));   // (a positive roll tips the top of the rig left, with the hook)
                FollowPunch(-0.6f, 0f, 1f);
                return;
            }
            hand = step == "CROSS" ? 1 : 0;
            big = hand == 1 || (Fists.Instance != null && Fists.Instance.BodyInLunge);
            Set(Anticipation > 0.001f ? Beat.Anticipate : Beat.Snap);
            ghostTimer = 0f;
            // The cross turns the whole root (the rear shoulder comes through: the body turns right, toward the
            // lead side); the jab counters by a third the other way. The view follows the fist toward the centre
            float crossYaw = config != null ? config.viewmodelCrossRootYaw : 6f;
            if (hand == 1)
            {
                if (rig != null) rig.Lean(new Vector3(-0.01f, -0.005f, 0.03f), new Vector3(1f, crossYaw, -crossYaw * 0.3f));
                FollowPunch(0.35f, 0f, 0.8f);
            }
            else
            {
                if (rig != null) rig.Lean(new Vector3(0.01f, 0f, 0.02f), new Vector3(0.5f, -crossYaw * 0.33f, crossYaw * 0.15f));
                FollowPunch(-0.25f, 0f, 0.5f);
            }
        }
    }

    /// The camera's follow (PlayerLook's kicks): the view turns and nods a few degrees IN the punch's direction
    /// (screen units: x right, y up) and takes a step into it
    void FollowPunch(float x, float y, float push)
    {
        var look = motor != null ? motor.look : null;
        if (look == null) return;
        look.AddYawKick(x * (config != null ? config.viewmodelFollowYaw : 6f));
        look.AddPitchKick(y * (config != null ? config.viewmodelFollowPitch : 3f));
        look.AddPushKick(push * (config != null ? config.viewmodelFollowPush : 0.03f));
    }

    void Set(Beat next)
    {
        fromR = lastR; fromL = lastL; beat = next; beatStart = Time.unscaledTime;
        if (next == Beat.Hook) hooking = true;
        else if (next != Beat.Follow && next != Beat.Return) hooking = false;
    }

    /// The extension and the follow-through, the cross's and the lunge's reaching further; the hook's are its own
    /// (across the frame, not down the line)
    bool hooking;
    ArmRig.ArmPose Ext(bool left) { if (hooking && !left) return HookR; var p = ExtR; if (big) p.pos.z *= Big; return left ? p.Mirrored() : p; }
    ArmRig.ArmPose Follow(bool left) { if (hooking && !left) return HookFollowR; var p = FollowR; if (big) p.pos.z *= Big; return left ? p.Mirrored() : p; }

    void Update()
    {
        var rig = ArmRig.Instance;
        if (rig == null || !rig.Built) return;
        if (knuckleDim == null) Materials();
        if (boot == null) BuildBoot(rig);
        rig.ShowKnuckles(false);   // bare hands: the knuckle dusters went with the jab (2026-10-27)
        if (!Active) { beat = Beat.Stance; if (boot.gameObject.activeSelf) boot.gameObject.SetActive(false); return; }
        if (!init) { init = true; lastR = StanceR; lastL = StanceL; }
        if (motor != null && motor.ParryPressedAt > lastSeenPress)
        {
            lastSeenPress = motor.ParryPressedAt;
            if (beat != Beat.Impact && beat != Beat.Kick) Set(Beat.Sweep);
        }

        float now = Time.unscaledTime, t = now - beatStart;
        // The stance is alive: the lead hand floats on its own slow bounce, the rear hand on another (the root
        // shifts the weight under both)
        ArmRig.ArmPose stR = StanceR, stL = StanceL;
        stR.pos += new Vector3(Mathf.Sin(now * 1.9f) * 0.006f, Mathf.Sin(now * 2.7f) * 0.005f, Mathf.Sin(now * 1.3f) * 0.004f);
        stL.pos += new Vector3(Mathf.Sin(now * 2.2f + 1f) * 0.005f, Mathf.Sin(now * 1.6f + 2f) * 0.006f, 0f);
        ArmRig.ArmPose r = stR, l = stL;
        bool snapR = false, snapL = false, stretchR = false, stretchL = false, ghosts = false, freeR = true, freeL = true;
        float layer = 1f;
        bool right = hand == 0;
        bool bootOut = false;
        switch (beat)
        {
            case Beat.Sweep:
            {
                float k = EaseOutCubic(Mathf.Clamp01(t / SweepTime));
                r = ArmRig.ArmPose.Lerp(fromR, GuardR, k); l = ArmRig.ArmPose.Lerp(fromL, GuardR.Mirrored(), k);
                freeR = freeL = false; snapR = snapL = true;
                if (t >= SweepTime) Set(motor != null && motor.ParryWindowOpen ? Beat.Hold : Beat.Recover);
                break;
            }
            case Beat.Hold:
                r = GuardR; l = GuardR.Mirrored();
                freeR = freeL = false;
                if (motor == null || !motor.ParryWindowOpen) Set(Beat.Recover);
                break;
            case Beat.Impact:
                r = ImpactR; l = ImpactR.Mirrored();   // the forearms take it; the rig's shudder rattles them
                freeR = freeL = false;
                if (t >= ImpactHold) Set(Beat.Recover);
                break;
            case Beat.Recover:
            {
                float k = EaseInOut(Mathf.Clamp01(t / GuardRecover));
                r = ArmRig.ArmPose.Lerp(fromR, stR, k); l = ArmRig.ArmPose.Lerp(fromL, stL, k);
                if (t >= GuardRecover) Set(Beat.Stance);
                break;
            }
            case Beat.Anticipate:
            {
                // The flick back before the snap; the other hand settles into the stance
                float k = EaseInOut(Mathf.Clamp01(t / Anticipation));
                ArmRig.ArmPose a = right ? AnticR : AnticR.Mirrored();
                if (right) { r = ArmRig.ArmPose.Lerp(fromR, a, k); l = ArmRig.ArmPose.Lerp(fromL, stL, k); freeR = false; }
                else { l = ArmRig.ArmPose.Lerp(fromL, a, k); r = ArmRig.ArmPose.Lerp(fromR, stR, k); freeL = false; }
                if (t >= Anticipation) Set(Beat.Snap);
                break;
            }
            case Beat.Snap:
            {
                // Full speed from the first frame: the elbow straightens, the shoulder rolls in (the rig's lead)
                float k = EaseOutQuart(Mathf.Clamp01(t / SnapTime));
                layer = 0.5f; ghosts = true;
                if (shove) { r = ArmRig.ArmPose.Lerp(fromR, ShoveR, k); l = ArmRig.ArmPose.Lerp(fromL, ShoveR.Mirrored(), k); snapR = snapL = stretchR = stretchL = true; freeR = freeL = false; }
                else if (right) { r = ArmRig.ArmPose.Lerp(fromR, Ext(false), k); l = fromL; snapR = stretchR = true; freeR = false; }
                else { l = ArmRig.ArmPose.Lerp(fromL, Ext(true), k); r = fromR; snapL = stretchL = true; freeL = false; }
                if (t >= SnapTime) Set(Beat.Follow);
                break;
            }
            case Beat.Follow:
            {
                // A whiff follows through past the target; a connect bites and holds (the rig shudders it)
                bool connected = outcome != "MISS";
                float hold = outcome == "EXECUTE" ? ExecuteHold : connected ? FollowTime + 0.05f : FollowTime;
                float k = Mathf.Clamp01(t / hold);
                ArmRig.ArmPose endR = shove ? ShoveR : Ext(false), endL = shove ? ShoveR.Mirrored() : Ext(true);
                if (connected) { float bite = 0.02f * (1f - k); endR.pos.z -= bite; endL.pos.z -= bite; }
                else { float f = Mathf.Sin(Mathf.PI * k); endR = ArmRig.ArmPose.Lerp(endR, Follow(false), f); endL = ArmRig.ArmPose.Lerp(endL, Follow(true), f); }
                layer = 0.5f; ghosts = !connected && k < 0.6f;
                if (shove) { r = endR; l = endL; stretchR = stretchL = true; freeR = freeL = false; }
                else if (right) { r = endR; l = fromL; stretchR = true; freeR = false; }
                else { l = endL; r = fromR; stretchL = true; freeL = false; }
                if (t >= hold) Set(Beat.Return);
                break;
            }
            case Beat.Return:
            {
                // Home with an overshoot: the fist sails a touch past the stance and settles into it
                float k = EaseOutBack(Mathf.Clamp01(t / ReturnTime), Overshoot * 10f);
                if (shove) { r = ArmRig.ArmPose.Lerp(fromR, stR, k); l = ArmRig.ArmPose.Lerp(fromL, stL, k); }
                else if (right) { r = ArmRig.ArmPose.Lerp(fromR, stR, k); l = ArmRig.ArmPose.Lerp(fromL, stL, EaseInOut(Mathf.Clamp01(t / ReturnTime))); freeR = false; }
                else { l = ArmRig.ArmPose.Lerp(fromL, stL, k); r = ArmRig.ArmPose.Lerp(fromR, stR, EaseInOut(Mathf.Clamp01(t / ReturnTime))); freeL = false; }
                if (t >= ReturnTime) Set(Beat.Stance);
                break;
            }
            case Beat.Hook:
            {
                // Across the frame, full speed, the shoulder leading: the extension is the far side of the view;
                // the left comes up to the chin as the guard
                float k = EaseOutQuart(Mathf.Clamp01(t / HookTime));
                r = ArmRig.ArmPose.Lerp(fromR, HookR, k);
                l = ArmRig.ArmPose.Lerp(fromL, GuardR.Mirrored(), k);
                snapR = stretchR = true; freeR = freeL = false;
                layer = 0.4f; ghosts = true;
                if (t >= HookTime) Set(Beat.Follow);   // the follow-through is the jab's grammar from here: bite on a connect, sail past on a whiff
                break;
            }
            case Beat.Kick:
            {
                // The fists to the chin at once; the boot up through the lower right (KickUp), a beat at the top
                // (KickHold: the sole on the body), then down and away (KickDown); the hands home with it
                float total = KickUp + KickHold + KickDown;
                float k = Mathf.Clamp01(t / 0.08f);
                float home = Mathf.Clamp01((t - KickUp - KickHold) / KickDown);
                r = ArmRig.ArmPose.Lerp(ArmRig.ArmPose.Lerp(fromR, KickR, EaseOutCubic(k)), stR, EaseInOut(home));
                l = ArmRig.ArmPose.Lerp(ArmRig.ArmPose.Lerp(fromL, KickL, EaseOutCubic(k)), stL, EaseInOut(home));
                snapR = snapL = true; freeR = freeL = false;
                layer = 0.6f;
                bootOut = true;
                float bk = t < KickUp ? EaseOutQuart(t / KickUp)
                    : t < KickUp + KickHold ? 1f
                    : 1f - EaseInOut(Mathf.Clamp01((t - KickUp - KickHold) / KickDown));
                ArmRig.ArmPose bp = ArmRig.ArmPose.Lerp(BootRest, BootKick, bk);
                if (outcome != "MISS" && t >= KickUp && t < KickUp + KickHold) bp.pos.z -= 0.03f;   // the bite: the sole stops on the body
                boot.localPosition = bp.pos;
                boot.localRotation = bp.rot;
                if (t >= total) Set(Beat.Stance);
                break;
            }
            default:
                r = stR; l = stL;
                break;
        }

        if (boot.gameObject.activeSelf != bootOut) boot.gameObject.SetActive(bootOut);
        lastR = r; lastL = l;
        rig.Drive(ArmRig.HandSide.Right, r.pos, r.rot, ArmRig.Fingers.Fist, snapR, stretchR, layer, freeR);
        rig.Drive(ArmRig.HandSide.Left, l.pos, l.rot, ArmRig.Fingers.Fist, snapL, stretchL, layer, freeL);
        if (ghosts) SpawnGhosts(rig);
        Knuckles(rig, now);
    }

    static float EaseOutBack(float x, float s)
    {
        float c1 = 1.70158f * Mathf.Max(0f, s) / 1.8f, c3 = c1 + 1f, y = x - 1f;
        return 1f + c3 * y * y * y + c1 * y * y;
    }

    /// The knuckle edge is state: gold for a beat after a perfect tackle or a perfect springboard, white-hot for
    /// a beat after a connect, cyan while a body is in the window (the fan, in reach — cut here: the same read as
    /// the HUD brackets), dark otherwise
    void Knuckles(ArmRig rig, float now)
    {
        Material want = now - Fists.LastPerfectTackleAt < PerfectFlash || now - Fists.LastPerfectKickAt < PerfectFlash ? knuckleGold
            : now - Fists.LastConnectAt < ConnectFlash ? knuckleWhite
            : Fists.Instance.BodyInWindow ? knuckleCyan
            : knuckleDim;
        if (want == current) return;
        current = want;
        var r = rig.KnuckleEdge(ArmRig.HandSide.Right); if (r != null) r.sharedMaterial = want;
        var l = rig.KnuckleEdge(ArmRig.HandSide.Left); if (l != null) l.sharedMaterial = want;
    }

    /// Afterimages of the fist through a snap, the sword's ghost idea in the hands' colour
    void SpawnGhosts(ArmRig rig)
    {
        ghostTimer -= Time.unscaledDeltaTime;
        if (ghostTimer > 0f) return;
        ghostTimer = GhostInterval;
        if (fistGhostMat == null)
        {
            fistGhostMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            fistGhostMat.SetColor("_BaseColor", new Color(0.4f, 1.5f, 1.7f));
        }
        bool both = shove || beat == Beat.Sweep;
        if (both || hand == 0) Ghost(rig.Palm(ArmRig.HandSide.Right));
        if (both || hand == 1) Ghost(rig.Palm(ArmRig.HandSide.Left));
    }

    static void Ghost(Transform palm)
    {
        if (palm == null) return;
        var ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ghost.name = "FistGhost";
        ghost.layer = Viewmodel.Layer;
        Destroy(ghost.GetComponent<Collider>());
        ghost.transform.position = palm.position;
        ghost.transform.rotation = palm.rotation;
        ghost.transform.localScale = palm.lossyScale * 1.15f;
        ghost.GetComponent<MeshRenderer>().sharedMaterial = fistGhostMat;
        ghost.AddComponent<GhostFade>();
    }
}

/// Fist ghosts collapse flat and vanish in a blink.
public class GhostFade : MonoBehaviour
{
    float born;
    Vector3 baseScale;
    const float Life = 0.2f;

    void Start() { born = Time.unscaledTime; baseScale = transform.localScale; }

    void Update()
    {
        float t = (Time.unscaledTime - born) / Life;
        if (t >= 1f) { Destroy(gameObject); return; }
        transform.localScale = new Vector3(baseScale.x * (1f - t), baseScale.y * (1f - t), baseScale.z);
    }
}
