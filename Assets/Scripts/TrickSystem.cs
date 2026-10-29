using UnityEngine;

/// <summary>
/// THE TRICKS (2026-10-23; the user: "allow for flips and tricks with the use of QTE"). In the air, when the jump is
/// big enough (airtime ≥ trickMinAirtime with ≥ trickMinRemaining predicted left, or a launch: a pad, a springboard,
/// a corner boost, a slam jump), a TRICK WINDOW opens around the apex: the HUD's brass ring closes onto the crosshair
/// over trickWindow, the beat is the apex. A press inside it is a trick, chosen by the movement input held: W = FRONT
/// FLIP, S = BACKFLIP, A/D = SIDE FLIP, nothing = SPIN. PERFECT = within ±trickPerfectWindow of the beat; LAZY =
/// inside the window; outside it nothing happens (a missed prompt is just a jump). The camera rides THROUGH the arc:
/// PlayerLook asks Apply(view) for the trick's rotation on top of the look (eased, returning exactly to zero), so the
/// mouse still aims underneath and you land where you looked. The arms tuck (ArmRig.Tuck), the dash's FOV punch
/// fires, the kick and the jab wait (CommitArm). THE PAY is the springboard's: a PERFECT trick pays its tier on the
/// landing's horizontal through AddImpulse under the lunge's cap max(current, run) × the perfect-hop tier, and a pip
/// comes back; a LAZY trick pays style only (the "trick" freshness key). Mid-arc at touchdown = a STUMBLE: the
/// landing roll, trickStumbleSeconds at half acceleration (PlayerMotor.Stumble), the pay forfeited. When the air is
/// long enough a second prompt follows the first arc (a combo), each later trick worth trickComboStyle× more style.
/// Everything attributed, no allocation per frame (the names are literals), nothing read in Awake.
/// </summary>
public class TrickSystem : MonoBehaviour
{
    public static TrickSystem Instance { get; private set; }
    public MovementConfig config;
    public PlayerMotor motor;
    public PlayerLook look;
    public StaminaSystem stamina;
    public StyleSystem style;
    public CameraBody body;

    public enum Kind { None, Front, Back, Side, Spin }
    static readonly string[] Names = { null, "FRONT FLIP", "BACKFLIP", "SIDE FLIP", "SPIN" };
    static readonly string[] PerfectNames = { null, "PERFECT FRONT FLIP", "PERFECT BACKFLIP", "PERFECT SIDE FLIP", "PERFECT SPIN" };

    /// A trick's arc is running (the camera is mid-turn)
    public bool Active => kind != Kind.None;
    public Kind Current => kind;
    public float ArcSeconds => arcSeconds;
    /// The prompt is up: the HUD's ring closes over Prompt01 (0 open → 1 shut; 0.5 is the beat)
    public bool PromptOpen => armed && !Active && Time.time <= ringClose;
    public float Prompt01 => Mathf.Clamp01((Time.time - armedAt) / Mathf.Max(0.01f, ringClose - armedAt));
    /// The QTE is armed: the first press is in and the ring is closing
    public bool Armed => armed;
    /// THE BAR (2026-10-27): the window and the perfect window as fractions of the bar either side of its middle (the
    /// beat is 0.5 of Prompt01), for a HUD that draws the QTE as a marker sliding along a bar into a zone
    public float WindowFraction => (windowClose - windowCentre) / Mathf.Max(0.01f, ringClose - armedAt);
    public float PerfectFraction => (config != null ? config.trickPerfectWindow : 0.06f) / Mathf.Max(0.01f, ringClose - armedAt);
    /// The last press (unscaled time) and its verdict, for the HUD's flash; the last perfect pay, for its gold
    public float PressedAt { get; private set; } = -10f;
    public bool PressedPerfect { get; private set; }
    public float LandedAt { get; private set; } = -10f;
    /// THE BAR'S STAMPS (2026-10-27): when the window was last missed (Fail stamps PressedAt too, so FailedAt >= PressedAt reads "a miss"),
    /// where along the bar (0..1) the press or the miss fell, whether the last landing paid
    public float FailedAt { get; private set; } = -10f;
    public float Pressed01 { get; private set; }
    public bool LandedPaid { get; private set; }
    /// Tricks already finished in this air
    public int ComboIndex { get; private set; }
    /// The predicted landing (TrickArcs), refreshed every trickMarchInterval while airborne
    public bool HasLanding { get; private set; }
    public Vector3 LandingPoint { get; private set; }
    public float RemainingAir => landAt - Time.time;

    Kind kind;
    float arcStart, arcSeconds, sideSign = 1f, arcFresh = 1f;
    bool arcPerfect, pendingPay, stumbling;
    float stumbleAt, stumbleAngle0, stumbleSeconds;
    float windowOpen = -10f, windowClose = -10f, windowCentre, armedAt = -10f, ringClose = -10f;
    bool armed;
    float airStart = -10f, landAt, lastMarchAt = -10f;
    bool wasGrounded = true;
    /// When the last trick was thrown (scaled time): the style system's starvation clock reads it
    public float LastTrickAt { get; private set; } = -100f;

    void OnEnable() { Instance = this; MovementEvents.MoveExecuted += OnMove; }
    void OnDisable() { if (Instance == this) Instance = null; MovementEvents.MoveExecuted -= OnMove; }

    void OnMove(MoveType type, string detail)
    {
        if (config == null || motor == null) return;
        if (type == MoveType.Land || type == MoveType.SlamLand) Touchdown();
    }

    void Update()
    {
        if (config == null || motor == null || look == null) return;
        if (PauseMenu.IsPaused || OpeningCutscene.Active || BossIntro.Active || BossOutro.Active || LevelResults.Active || MineBlast.Active) return;
        float now = Time.time;
        bool grounded = motor.Grounded;
        Vector3 v = motor.Velocity;
        if (!grounded && wasGrounded) BeginAir(now);
        wasGrounded = grounded;

        if (!grounded && !motor.WallRunning && now - lastMarchAt >= config.trickMarchInterval) March(now);

        if (Active) TickArc(now, grounded);
        else if (!grounded) TickQte(now, v);
        else armed = false;
    }

    void BeginAir(float now)
    {
        airStart = now;
        armed = false;
        ComboIndex = 0;
        windowOpen = windowClose = -10f;
        HasLanding = false;
        landAt = now;
        lastMarchAt = -10f;
    }

    /// Where this air comes down, by the fall arc (TrickArcs): the airtime left, and the landing point for the camera
    void March(float now)
    {
        lastMarchAt = now;
        if (TrickArcs.Landing(motor.CurrPosition, motor.Velocity, config.gravity, 1.6f, 0.08f, out Vector3 p, out float t))
        {
            HasLanding = true;
            LandingPoint = p;
            landAt = now + t;
        }
        else
        {
            HasLanding = false;
            landAt = now + 1.6f;   // nothing under you for a long way: room for anything
        }
    }

    /// THE QTE (2026-10-26, the two-press trick): in the air the first press ARMS it — the ring closes over
    /// trickQteLead seconds to the beat and the same again past it — and the second press at the beat throws the
    /// flip: inside the window a trick (inside the perfect window a PERFECT one); early, or never, a STUMBLE in the
    /// air that cuts the horizontal speed. A style zone widens the window. On a wall the feet are planted: no arming.
    /// Too little air left for the lead and the press is just a press
    void TickQte(float now, Vector3 v)
    {
        if (motor.WallRunning) { armed = false; return; }
        bool press = Keybinds.Down(GameAction.Trick);
        if (!armed)
        {
            if (press && RemainingAir >= config.trickQteLead + 0.05f) Arm(now);
            return;
        }
        if (press)
        {
            float off = Mathf.Abs(now - windowCentre);
            if (off <= windowClose - windowCentre) Begin(now, off <= config.trickPerfectWindow);
            else Fail();
            return;
        }
        if (now > windowClose) Fail();   // the beat went by unpressed
    }

    void Arm(float now)
    {
        float lead = Mathf.Max(0.1f, config.trickQteLead);
        float half = Mathf.Max(0.02f, config.trickWindow) * 0.5f * StyleZone.WindowScaleAt(motor.CurrPosition);
        armed = true;
        armedAt = now;
        windowCentre = now + lead;
        windowOpen = windowCentre - half;
        windowClose = windowCentre + half;
        ringClose = now + lead * 2f;   // the HUD's ring: 0 open → 1 shut, the beat at 0.5
    }

    /// The window missed: the stumble in the air, the horizontal cut to trickFailKeep, style docked
    void Fail()
    {
        armed = false;
        windowOpen = windowClose = -10f;
        PressedAt = Time.unscaledTime;
        PressedPerfect = false;
        FailedAt = PressedAt;
        Pressed01 = Prompt01;
        Vector3 v = motor.Velocity;
        float keep = Mathf.Clamp01(config.trickFailKeep);
        motor.Launch(new Vector3(v.x * keep, v.y, v.z * keep));
        motor.Stumble(config.trickStumbleSeconds, config.trickStumbleAccelScale);
        if (style != null) style.TakeTrickFail(config.trickFailStyleLoss);
        if (body != null) body.StartRoll();
        MovementEvents.Emit(MoveType.Trick, "STUMBLE");
    }

    /// The press: the trick by the input held, the arc from now, the arms tucked, the kick and the jab waiting
    void Begin(float now, bool perfect)
    {
        float fx = (Keybinds.Held(GameAction.Right) ? 1f : 0f) - (Keybinds.Held(GameAction.Left) ? 1f : 0f);
        float fz = (Keybinds.Held(GameAction.Forward) ? 1f : 0f) - (Keybinds.Held(GameAction.Back) ? 1f : 0f);
        if (fz > 0f) { kind = Kind.Front; arcSeconds = config.trickFrontSeconds; }
        else if (fz < 0f) { kind = Kind.Back; arcSeconds = config.trickBackSeconds; }
        else if (fx != 0f) { kind = Kind.Side; arcSeconds = config.trickSideSeconds; sideSign = fx < 0f ? 1f : -1f; }   // a flip to the left rolls left (+)
        else { kind = Kind.Spin; arcSeconds = config.trickSpinSeconds; }
        arcSeconds = Mathf.Max(0.1f, arcSeconds);
        arcStart = now;
        arcPerfect = perfect;
        stumbling = false;
        windowOpen = windowClose = -10f;
        PressedAt = Time.unscaledTime;
        PressedPerfect = perfect;
        Pressed01 = Prompt01;
        motor.CommitArm(arcSeconds);
        if (body != null) body.Punch(config.trickFov);
        if (ArmRig.Instance != null) ArmRig.Instance.Tuck(arcSeconds);
        if (ComboIndex > 0 && style != null) style.AddBonus(config.styleFreshMovePoints * Mathf.Pow(Mathf.Max(1f, config.trickComboStyle), ComboIndex));
        // THE BURST (the two-press trick's pay on the press): forward and up at once, a slab of style (a style zone
        // scales it, perfect adds half again), a pip back
        armed = false;
        LastTrickAt = now;
        float zone = StyleZone.StyleScaleAt(motor.CurrPosition);
        arcFresh = StyleSystem.GainScale(MoveType.Trick);   // FRESHNESS (STEP 1): read before the press's event spends it; the landing's pay uses the same
        float burst = (perfect ? config.trickPerfectBurstScale : 1f) * Mathf.Pow(Mathf.Max(1f, config.trickComboBurstScale), ComboIndex) * arcFresh;
        Vector3 v = motor.Velocity;
        float h = new Vector2(v.x, v.z).magnitude;
        Vector3 fwd = h > 0.5f ? new Vector3(v.x, 0f, v.z) / h : Quaternion.Euler(0f, look.Yaw, 0f) * Vector3.forward;
        // EACH FLIP IS ITS OWN SPEED (2026-10-26, user: "flips and stuff that grant speed bursts"): the front flip is
        // the fast one (forward), the backflip the high one (up), the side flip the lane change (sideways, the way
        // you flipped, with a little forward), the spin the balanced one that pays the most style
        float f = config.trickBurstForward, u = config.trickBurstUp, styleScale = 1f;
        Vector3 impulse;
        switch (kind)
        {
            case Kind.Front: impulse = fwd * (f * config.trickFrontForwardScale) + Vector3.up * (u * 0.5f); break;
            case Kind.Back: impulse = fwd * (f * 0.4f) + Vector3.up * (u * config.trickBackUpScale); break;
            case Kind.Side:
            {
                Vector3 side = Vector3.Cross(Vector3.up, fwd).normalized * -sideSign;   // a flip to the left cuts left
                impulse = side * config.trickSideLateral + fwd * (f * 0.6f) + Vector3.up * (u * 0.6f);
                break;
            }
            default: impulse = fwd * f + Vector3.up * u; styleScale = config.trickSpinStyleScale; break;
        }
        motor.AddImpulse(impulse * burst);
        if (style != null) style.AddBonus(config.trickStylePoints * zone * styleScale * (perfect ? 1.5f : 1f));
        if (stamina != null && config.trickPips > 0) stamina.Refund(config.trickPips);
        MovementEvents.Emit(MoveType.Trick, perfect ? PerfectNames[(int)kind] : Names[(int)kind]);
    }

    void TickArc(float now, bool grounded)
    {
        if (stumbling)
        {
            if (now - stumbleAt >= stumbleSeconds) kind = Kind.None;
            return;
        }
        if (now - arcStart < arcSeconds) return;
        // The arc is done
        kind = Kind.None;
        ComboIndex++;
        if (arcPerfect) pendingPay = true; else cleanLanding = true;   // a lazy press lands clean too: it says so at touchdown, it just pays nothing
        if (grounded) { if (pendingPay) Pay(); else Landed(); return; }   // it finished its last degrees in the landing's dip (the grace)
        // THE COMBO: room for another? The next prompt opens at once, its beat half a window away
        // THE COMBO: air left? the next press arms the next one (TickQte); nothing opens by itself
    }

    /// The ground, from the motor's own landing (the Land / SlamLand event, inside its tick, before a hop on the same tick)
    void Touchdown()
    {
        float now = Time.time;
        windowOpen = windowClose = -10f;   // a prompt still up at touchdown is gone
        if (Active && !stumbling)
        {
            float remaining = arcStart + arcSeconds - now;
            if (remaining > config.trickLandGrace) { Stumble(now); return; }
            if (arcPerfect) pendingPay = true;   // landed clean: paid when the arc ends, a few degrees from now
            else cleanLanding = true;
            return;
        }
        if (pendingPay) Pay();
        else if (cleanLanding) Landed();
    }

    bool cleanLanding;

    /// A trick landed clean without the beat: the event the ward and the tutorial read ("LANDED"), no pay
    void Landed()
    {
        cleanLanding = false;
        LandedPaid = false;
        LandedAt = Time.unscaledTime;
        MovementEvents.Emit(MoveType.Trick, "LANDED");
    }

    /// Touched down mid-arc: the roll, a beat of footing lost, the pay forfeited; the arc's rest completes through the roll
    void Stumble(float now)
    {
        stumbling = true;
        stumbleAt = now;
        stumbleAngle0 = Angle(now);
        stumbleSeconds = Mathf.Max(0.1f, config.cameraRollSeconds);
        pendingPay = false;
        motor.Stumble(config.trickStumbleSeconds, config.trickStumbleAccelScale);
        if (body != null) body.StartRoll();
        MovementEvents.Emit(MoveType.Trick, "STUMBLE");
    }

    /// THE PAY (the springboard's rule): the landing's horizontal × the trick's tier, floored at trickLandFloorSpeed,
    /// under max(current, run) × the perfect-hop tier; through AddImpulse (attributed); a pip back
    void Pay()
    {
        pendingPay = false;
        cleanLanding = false;
        Vector3 v = motor.Velocity;
        float h = new Vector2(v.x, v.z).magnitude;
        Vector3 dir = h > 0.5f ? new Vector3(v.x, 0f, v.z) / h : Quaternion.Euler(0f, look.Yaw, 0f) * Vector3.forward;
        float tier = motor.PerfectTierMultiplier(config.trickPerfectMultiplier, config.trickPerfectMinMultiplier);
        float cap = Mathf.Max(h, config.baseRunSpeed) * motor.PerfectTierMultiplier(config.perfectBhopSpeedMultiplier, config.perfectBhopMinMultiplier);
        float s = Mathf.Min(Mathf.Max(h, config.trickLandFloorSpeed) * tier, cap);
        float gain = Mathf.Max(0f, s - h) * arcFresh;   // FRESHNESS (STEP 1): the landing pays at the freshness the press was thrown at
        if (gain > 0f) motor.AddImpulse(dir * gain);
        if (stamina != null && config.trickPerfectPips > 0) stamina.Refund(config.trickPerfectPips);
        LandedPaid = true;
        LandedAt = Time.unscaledTime;
        MovementEvents.Emit(MoveType.Trick, "LANDED  +" + gain.ToString("0.0") + " m/s");   // one string per perfect landing: an event, not a frame
    }

    /// The arc's angle now: 360° eased in and out over the arc; through a stumble the rest completes over the roll
    float Angle(float now)
    {
        if (stumbling)
        {
            float q = Mathf.Clamp01((now - stumbleAt) / Mathf.Max(0.01f, stumbleSeconds));
            return stumbleAngle0 + (360f - stumbleAngle0) * (1f - (1f - q) * (1f - q));
        }
        float p = Mathf.Clamp01((now - arcStart) / Mathf.Max(0.01f, arcSeconds));
        return 360f * Mathf.SmoothStep(0f, 1f, p);
    }

    /// The trick's rotation on top of the look (PlayerLook): a flip turns about the view's own right (front: forward
    /// and down first) or forward (the side flip), a spin about the world's up. At 360° every one is the identity
    public Quaternion Apply(Quaternion view)
    {
        if (!Active) return view;
        float a = Angle(Time.time);
        switch (kind)
        {
            case Kind.Front: return view * Quaternion.AngleAxis(a, Vector3.right);
            case Kind.Back: return view * Quaternion.AngleAxis(-a, Vector3.right);
            case Kind.Side: return view * Quaternion.AngleAxis(a * sideSign, Vector3.forward);
            case Kind.Spin: return Quaternion.AngleAxis(a, Vector3.up) * view;
        }
        return view;
    }
}
