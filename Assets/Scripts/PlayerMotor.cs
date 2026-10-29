using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Custom kinematic capsule motor using collide-and-slide (spec §6).
/// Quake-style physics plus the full expressive kit: slide, dash, double jump,
/// slam + slam storage, wall kicks with Deadlock-style input shaping, corner
/// boosts, edge boosts, and the hidden-tech detector.
/// Refunds live in ResetDetector — the motor only emits MovementEvents.
/// Transform position is the BOTTOM of the capsule. Runs in FixedUpdate;
/// the camera interpolates between PrevPosition and CurrPosition.
/// </summary>
public class PlayerMotor : MonoBehaviour
{
    public MovementConfig config;
    public PlayerLook look;
    public StaminaSystem stamina;
    public GrappleAbility grapple;

    /// The one motor in play (the course zones, pads and triggers read it)
    public static PlayerMotor Current { get; private set; }
    /// THE GRADE DRIVES THE BODY (2026-10-26): the style system sets this from the rank every frame
    public float StyleSpeedScale = 1f;
    /// Gravity eased by a glide chute the player is sailing through (CourseZones)
    float GravityScale => GlideChute.ScaleAt(this);
    public Vector3 Velocity => velocity;
    public bool Grounded => grounded;
    public bool Sliding => sliding;
    public bool Slamming => slamming;
    public bool Dashing => Time.time < dashEndTime;
    public bool FrictionDisabled => grounded && !sliding && Time.time - landTime <= config.bhopFrictionGraceTime;
    /// What is left of the landing grace (1 at touchdown, 0 when friction comes back): the window a bhop keeps speed in
    public float BhopGraceFraction => FrictionDisabled && HorizontalSpeed > 2f ? Mathf.Clamp01(1f - (Time.time - landTime) / config.bhopFrictionGraceTime) : 0f;
    public float CurrentHeight => currentHeight;
    public bool OnFlatGround => grounded && !sliding && Vector3.Angle(groundNormal, Vector3.up) < 5f;
    /// Increments on every flat-ground landing, including one-tick touch-and-go
    /// landings a frame-rate poll would miss. ResetDetector keys the chain on it.
    public int FlatGroundTouchCount { get; private set; }
    public Vector3 PrevPosition { get; private set; }
    public Vector3 CurrPosition { get; private set; }
    // Eye rides at a constant fraction of the current capsule height (crouch lowers it)
    public float EyeHeight => currentHeight * (config.eyeHeight / config.capsuleHeight);

    const float Skin = 0.02f;
    const float GroundCheckDist = 0.08f;
    const float SnapDist = 0.35f;
    const int MaxSlideIterations = 5;

    Vector3 velocity;
    bool grounded;
    bool sliding;
    bool crouchHeld;
    bool slamming;
    Vector3 groundNormal = Vector3.up;
    float currentHeight;
    float landTime = float.NegativeInfinity;
    bool jumpPressPending;                       // a press seen in Update, stamped on the physics clock at the next tick
    float lastWallHitEmit = float.NegativeInfinity;
    float lastGroundedTime = float.NegativeInfinity;
    float jumpBufferedUntil = float.NegativeInfinity;
    float lastJumpPressTime = float.NegativeInfinity;
    float slideStartTime = float.NegativeInfinity;
    float dashEndTime = float.NegativeInfinity;
    float dashStartTime = float.NegativeInfinity;
    float slamLandTime = float.NegativeInfinity;
    float slamImpactSpeed, slamStartY, slamStoreSpeed;
    /// How far the last slam fell, in metres: the style it is worth, and (slamStorageFromDrop) what it stores
    public float LastSlamDrop { get; private set; }
    float lastWallContactTime = float.NegativeInfinity;
    // Wall run: sustained travel along a wall while airborne, a jump off it, and
    // the corner-boost combo window that jump opens
    bool wallRunning;
    Vector3 wallRunNormal = Vector3.right;
    float wallRunStartTime = float.NegativeInfinity;
    float wallRunEndTime = float.NegativeInfinity;
    float wallRunDetachTime = float.NegativeInfinity;
    Collider wallRunCollider;
    float lastWallJumpTime = float.NegativeInfinity;
    // Corner boost: the last LIP contact (a wall whose top lies within cornerBoostLipReach above the feet), kept
    // for a press to come inside the window. The wall takes your speed at the contact; the boost gives it back
    float lipContactTime = float.NegativeInfinity;
    float lipTopY, lipSpeed;                 // the lip's top, and the horizontal speed you brought to it
    Vector3 lipTravel, lipNormal;            // the way you were going, and the lip wall's horizontal normal
    float cornerPaidPressTime = float.NegativeInfinity;   // one corner boost per press
    const float LipProbeInset = 0.1f, LipProbeUp = 0.1f, LipProbeDown = 0.05f, LipMinAbove = 0.05f, LipClear = 0.02f;
    public bool WallRunning => wallRunning;
    /// -1 = the wall is on your left, +1 on your right (the camera leans off it)
    public float WallRunSide { get; private set; }
    int airJumpsUsed;
    int airWallKicksUsed;
    float parryPressTime = float.NegativeInfinity;
    float parryLockoutUntil = float.NegativeInfinity;
    bool moveParryUsed;      // the movement beat is one impact per press
    bool guardClosed = true; // the guard came down: timed out, cancelled by a cut, or whiffed (down until the first press)
    int guardConnects;       // what connected inside this guard
    float parryWhiffedAt = float.NegativeInfinity;
    bool dashPressed;
    bool slamPressed;
    bool jumpConsumedSinceGrounded;
    bool jumpedThisTick;
    // External systems (grapple) hand the motor a velocity for the coming tick
    Vector3 externalVelocity;
    bool hasExternalVelocity;
    // Spider-Man swing: a rope-length constraint the grapple sets each tick;
    // gravity + air control still run, the rope just keeps you on its arc
    Vector3 swingAnchor;
    float ropeLength;
    bool swinging;
    // Hidden-tech detector: known systems attribute their speed gains each tick;
    // any surplus is an emergent glitch the player found — reward it, don't patch it
    float hSpeedTickStart;
    float attributedGain;
    float glitchCooldownUntil = float.NegativeInfinity;
    CapsuleCollider capsule;
    // Player lives on Ignore Raycast so DefaultRaycastLayers queries skip our own collider. Hover mines live on a layer
    // of their own that the body does not collide with — a cut or an arrow still finds them (those cast against every
    // default layer), but you pass THROUGH one: a fist-sized thing that stops you dead at thirty metres a second is not
    // a hazard, it is a bug
    readonly int mask = Physics.DefaultRaycastLayers & ~(1 << HoverMine.Layer);

    void Awake()
    {
        Current = this;
        // Awake runs inside AddComponent, BEFORE the bootstrap assigns config —
        // nothing here may touch config (that's what Start is for)
        capsule = GetComponent<CapsuleCollider>();
        PrevPosition = CurrPosition = transform.position;
    }

    void Start()
    {
        currentHeight = config.capsuleHeight;
    }

    /// Move without stopping: the position changes, the velocity and the air carry over (a Pac-Man wrap)
    public void Warp(Vector3 position)
    {
        transform.position = position;
        PrevPosition = CurrPosition = position;
        swinging = false;
        grounded = false;
    }

    public void Teleport(Vector3 position)
    {
        transform.position = position;
        PrevPosition = CurrPosition = position;
        velocity = Vector3.zero;
        grounded = false;
        sliding = false;
        slamming = false;
        hasExternalVelocity = false;
        swinging = false;
        dashEndTime = float.NegativeInfinity;
        lungeUntil = float.NegativeInfinity;
        airJumpsUsed = 0;
        airWallKicksUsed = 0;
        // Clear timing state too, or a buffered jump / stale coyote window
        // fires a midair jump at the spawn point
        jumpBufferedUntil = float.NegativeInfinity;
        lastJumpPressTime = float.NegativeInfinity;
        lastGroundedTime = float.NegativeInfinity;
        landTime = float.NegativeInfinity;
        slideStartTime = float.NegativeInfinity;
        slamLandTime = float.NegativeInfinity;
        dashStartTime = float.NegativeInfinity;
        lipContactTime = float.NegativeInfinity;
        jumpConsumedSinceGrounded = true;
    }

    /// Overrides the motor's velocity for the next tick (grapple yank etc.).
    /// Collide-and-slide, wall contact, and landing still apply.
    public void SetExternalVelocity(Vector3 v)
    {
        externalVelocity = v;
        hasExternalVelocity = true;
    }

    void Update()
    {
        if (PauseMenu.IsPaused) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (Keybinds.Down(GameAction.Jump))
        {
            jumpPressPending = true;   // stamped in FixedUpdate: the landing is on that clock, so the perfect window compares like with like
            jumpBufferedUntil = Time.time + config.jumpBufferTime;
        }
        if (Keybinds.Down(GameAction.Dash)) dashPressed = true;
        if (Keybinds.Down(GameAction.Slam)) slamPressed = true;

        // Parry (RMB or F): THE GUARD. From the press it is up for parryGuardWindow, and everything parryable
        // that arrives while it is up is parried — every bolt, every sniper's shot, every windup in reach, all of
        // them (Deadlock's window). Walls and floors keep the tighter movement beat at the front of it (E33
        // timing). The guard blocks re-presses for its length; a WHIFF (it comes down on nothing) adds a real
        // cooldown with its own feedback, a guard that connected only a short one. ONE
        // HAND: while the arm is committed to a cut there is no guard to raise,
        // and a press then is thrown away as a whiff — the cost of mashing both.
        // THE MOVEMENT PARRY IS EVERYONE'S (the simplification, 2026-10-16): the key is read here, always; the cane
        // and its alt casts are gone
        bool parryInput = ParryAllowed && Keybinds.Down(GameAction.Parry);
        if (parryInput && Time.time >= parryLockoutUntil && Time.time < armCommittedUntil)
        {
            armCommittedUntil = 0f; // one penalty per cut
            Whiff("ARM COMMITTED");
        }
        else if (parryInput && Time.time >= parryLockoutUntil)
        {
            parryPressTime = Time.time;
            moveParryUsed = false;
            guardClosed = false;
            guardConnects = 0;
            parryLockoutUntil = Time.time + config.parryGuardWindow;

            // Zero-delay evaluation, this very frame: projectiles and enemies
            // first (CombatDirector listens), then walls if the beat is still there.
            // The guard stays up afterward for everything that arrives inside it.
            ParryPressed?.Invoke();
            if (ParryActive) TryProximityWallParry();
        }
        if (!guardClosed && Time.time - parryPressTime > config.parryGuardWindow) CloseGuard(); // the guard comes down
    }

    /// The guard comes down: on nothing, the whiff and its cooldown; after a connect or more, the short one
    void CloseGuard()
    {
        guardClosed = true;
        if (guardConnects == 0) { Whiff(); return; }
        parryLockoutUntil = Time.time + config.parrySuccessCooldown;
    }

    /// The whiff: the guard came up and met nothing (or was thrown away). Cooldown, and everyone hears it
    void Whiff(string cause = "EMPTY")
    {
        guardClosed = true;
        moveParryUsed = true;
        parryWhiffedAt = Time.time;
        parryLockoutUntil = Time.time + config.parryWhiffCooldown;
        MovementEvents.Emit(MoveType.ParryWhiff, cause);   // which rule was broken: EMPTY, ARM COMMITTED, CUT THREW IT
    }

    /// Fired the instant parry is pressed — CombatDirector checks projectiles
    /// and enemies synchronously so the parry never waits for the next tick.
    public event System.Action ParryPressed;

    /// The parry is the motor's own, for everyone: there is one kit and it carries no condition
    public static bool ParryAllowed => true;
    /// The movement beat: the front of the guard, one wall or floor per press (the tighter E33 timing)
    bool ParryActive => ParryAllowed && !moveParryUsed && !guardClosed && Time.time - parryPressTime <= config.parryWindow;
    /// The guard: everything parryable that arrives while it is up is parried, all of it
    bool GuardActive => ParryAllowed && !guardClosed && Time.time - parryPressTime <= config.parryGuardWindow;

    /// Where the last successful parry connected — GameFeel spawns the burst here
    public Vector3 LastParryPoint { get; private set; }
    /// The parry arm swings on every press; the HUD dot reads these
    public float ParryPressedAt => parryPressTime;
    public bool ParryWindowOpen => GuardActive;
    /// 1 → 0 while the guard is up (0 when it is down): the HUD's draining bar
    public float ParryGuardFraction => GuardActive ? Mathf.Clamp01(1f - (Time.time - parryPressTime) / config.parryGuardWindow) : 0f;
    /// How many things this guard has parried so far
    public int ParryGuardConnects => guardConnects;
    public float ParryLockoutRemaining => Mathf.Max(0f, parryLockoutUntil - Time.time);
    /// 1 → 0 while a WHIFFED parry's cooldown drains (0 when ready, or after a connect)
    public float ParryWhiffCooldownFraction => parryWhiffedAt >= parryPressTime && config.parryWhiffCooldown > 0f
        ? Mathf.Clamp01((parryLockoutUntil - Time.time) / config.parryWhiffCooldown) : 0f;

    /// A wall or floor connected: the movement beat is spent, the guard stays up for whatever else arrives
    void ParryConnected(Vector3 point)
    {
        moveParryUsed = true;
        guardConnects++;
        LastParryPoint = point;
    }

    /// Dash i-frames (spec §3.4 decision: kept) — projectiles pass through
    public bool HasIFrames => config.dashGivesIFrames
        && (Dashing || Time.time - dashStartTime <= config.dashIFrames);
    /// When the last dash started (the fists' perfect tackle is not paid on top of one)
    public float LastDashTime => dashStartTime;
    /// The last dash's direction (world space, unit, before the vertical cap): the viewmodel's dash layer reads it
    /// (transform.InverseTransformDirection for camera space) — a dash up pulls the hands the other way
    public Vector3 LastDashDir { get; private set; } = Vector3.forward;

    // THE LUNGE (Fists): for a beat the horizontal velocity is held along a line at a floor speed — a closer on a
    // body under the crosshair, the floor capped at a hop's worth over your run, and above it a plain redirect at
    // the speed you had
    Vector3 lungeDir;
    float lungeFloor, lungeUntil = float.NegativeInfinity;

    /// Hold the horizontal velocity along `dir` at max(current, floor) for `seconds`; vertical untouched; attributed each tick
    public void Lunge(Vector3 dir, float floor, float seconds)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-4f) return;
        lungeDir = dir.normalized;
        lungeFloor = floor;
        lungeUntil = Time.time + seconds;
        ApplyLunge();
    }

    void ApplyLunge()
    {
        if (Time.time >= lungeUntil) return;
        float hBefore = HorizontalSpeed;
        // The floor never pays more than a perfect hop would over a run: the closer closes, the hop stays the ladder's top
        float cap = Mathf.Max(hBefore, config.baseRunSpeed) * config.perfectBhopSpeedMultiplier;
        float s = Mathf.Max(hBefore, Mathf.Min(lungeFloor, cap));
        velocity = new Vector3(lungeDir.x * s, velocity.y, lungeDir.z * s);
        Attribute(hBefore);
    }

    /// External systems (projectiles, snipers, enemy counters) attempt a parry here: succeeds whenever the guard
    /// is up, fires the full payoff, and leaves the guard up — the next bolt in the same beat is parried too.
    public bool TryConsumeParry(Vector3 point, string detail = "REFLECT")
    {
        if (!GuardActive) return false;
        guardConnects++;
        LastParryPoint = point;
        MovementEvents.Emit(MoveType.Parry, detail);
        return true;
    }

    /// Pogo off an enemy (CombatDirector calls after a successful enemy parry):
    /// bounce upward off their head, horizontal untouched.
    public void ParryPogo()
    {
        float up = Mathf.Max(config.parryPogoMinUpSpeed, Mathf.Abs(velocity.y) * config.parryPogoBounce);
        velocity = new Vector3(velocity.x, up, velocity.z);
        slamming = false;
    }

    float armCommittedUntil;

    /// The strike commits the arm: for this long a parry press has no guard to raise (Fists calls it)
    public void CommitArm(float seconds) => armCommittedUntil = Mathf.Max(armCommittedUntil, Time.time + seconds);
    public bool ArmCommitted => Time.time < armCommittedUntil;

    /// A cut thrown while the guard is up and has met nothing throws the guard away with it: a whiff, cooldown
    /// and all. A guard that has connected just comes down early, so parry-THEN-cut is untouched
    public void CancelParry()
    {
        if (!GuardActive) return;
        if (guardConnects > 0) { CloseGuard(); return; }
        Whiff("CUT THREW IT");
    }

    public bool Swinging => swinging;

    /// Grapple sets/updates the swing rope each tick (anchor + length). Gravity
    /// and air-strafe keep running; ConstrainSwing keeps you on the arc.
    public void SetSwing(Vector3 anchor, float length)
    {
        swingAnchor = anchor;
        ropeLength = length;
        swinging = true;
    }

    public void ClearSwing() => swinging = false;

    // Rope constraint (Spider-Man pendulum): if the body drifts past the rope
    // length, pull it back onto the arc and strip the outward-radial velocity so
    // the motion becomes tangential — gravity then keeps building swing speed.
    void ConstrainSwing()
    {
        if (!swinging) return;
        Vector3 body = transform.position + Vector3.up * (currentHeight * 0.5f);
        Vector3 toAnchor = swingAnchor - body;
        float dist = toAnchor.magnitude;
        if (dist <= ropeLength || dist < 0.01f) return;
        Vector3 n = toAnchor / dist; // toward the anchor
        transform.position += n * (dist - ropeLength);
        float hBefore = HorizontalSpeed;
        float outward = -Vector3.Dot(velocity, n); // speed away from the anchor
        if (outward > 0f) velocity += n * outward;  // cancel it → tangential swing
        Attribute(hBefore);
    }

    /// Additive velocity change from outside (knockback etc.) — attributed.
    public void AddImpulse(Vector3 impulse)
    {
        float hBefore = HorizontalSpeed;
        velocity += impulse;
        Attribute(hBefore);
    }


    /// A springboard off a body (the knuckles' kick) resets the double jump the way a wall touch does
    public void RefreshAirJumps() => airJumpsUsed = 0;

    // THE STUMBLE (TrickSystem, 2026-10-23): a flip still mid-arc at touchdown costs a beat of footing — the ground
    // acceleration is scaled for a moment. Speed is never taken: the cost is that you cannot steer or build for a beat
    float stumbleUntil = float.NegativeInfinity, stumbleAccelScale = 1f;
    public bool Stumbling => Time.time < stumbleUntil;

    /// Scale the ground acceleration by `accelScale` for `seconds` (the trick's stumble)
    public void Stumble(float seconds, float accelScale)
    {
        stumbleUntil = Mathf.Max(stumbleUntil, Time.time + Mathf.Max(0f, seconds));
        stumbleAccelScale = Mathf.Clamp01(accelScale);
    }

    /// Thrown by the level (a jump pad): the velocity is set outright, the ground let go of — attributed, so it is
    /// never mistaken for hidden tech; whatever you do with the air afterwards is yours
    public void Launch(Vector3 newVelocity)
    {
        float hBefore = HorizontalSpeed;
        velocity = newVelocity;
        Attribute(hBefore);
        grounded = false;
        sliding = false;
        slamming = false;
        jumpConsumedSinceGrounded = true;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        if (jumpPressPending) { lastJumpPressTime = Time.time; jumpPressPending = false; }
        PrevPosition = transform.position;
        jumpedThisTick = false;

        var kb = Keyboard.current;
        crouchHeld = Keybinds.Held(GameAction.Crouch);

        hSpeedTickStart = HorizontalSpeed;
        attributedGain = 0f;

        bool externalThisTick = hasExternalVelocity;
        if (externalThisTick)
        {
            float hBefore = HorizontalSpeed;
            velocity = externalVelocity;
            hasExternalVelocity = false;
            Attribute(hBefore);
            slamming = false; // a yank overrides a slam — never latch it through a pull
        }

        GroundCheck();
        if (grounded) EndWallRun();
        UpdateSlideState();
        TryDash();
        TryJump();
        TrySlam();
        TryProximityWallParry();

        Vector3 wishDir = ReadWishDir();
        if (Dashing || externalThisTick)
        {
            // Overridden control: no input accel, no gravity, no friction
        }
        else if (slamming)
        {
            // Horizontal control locked; keep falling
            velocity += Vector3.down * (config.gravity * GravityScale * dt);
        }
        else if (grounded && sliding)
        {
            // Slides carry momentum: friction fades out the more directly you
            // point down-slope, so descents are free speed instead of fighting
            // friction toward a low terminal velocity. Flat slides still bleed
            // slowly — bhopping stays the flat-ground tech.
            Vector3 slopeAccel = Vector3.ProjectOnPlane(Vector3.down * config.gravity, groundNormal);
            float frictionScale = 1f;
            if (slopeAccel.sqrMagnitude > 0.01f && velocity.sqrMagnitude > 0.01f)
            {
                float downhill = Vector3.Dot(velocity.normalized, slopeAccel.normalized);
                if (downhill > 0f) frictionScale = 1f - Mathf.Clamp01(downhill);
            }
            velocity *= Mathf.Exp(-config.slideFriction * frictionScale * dt);
            float hBefore = HorizontalSpeed;
            velocity += slopeAccel * (config.slideSlopeAccelMultiplier * dt);
            Attribute(hBefore);
            SteerSlide(wishDir, dt); // WASD curves the slide, momentum preserved
        }
        else if (grounded)
        {
            bool dashGrace = Time.time - dashStartTime <= config.dashMomentumWindow;
            if (!FrictionDisabled && !dashGrace) ApplyFriction(dt);
            Accelerate(wishDir, config.baseRunSpeed * StyleSpeedScale, config.groundAcceleration * (Time.time < stumbleUntil ? stumbleAccelScale : 1f), dt);
            velocity = AlignToPlane(velocity, groundNormal);
        }
        else
        {
            UpdateWallRun(wishDir);
            if (wallRunning)
            {
                WallRunMove(wishDir, dt);
            }
            else
            {
                AirControl(wishDir, dt); // strafe gains + forward steers: W-held bhops work
                float g = config.gravity * GravityScale + (crouchHeld ? config.fastfallExtraGravity : 0f);
                // Wall cling (spec §4.1): brief gravity reduction on wall contact
                if (config.wallClingEnabled && Time.time - lastWallContactTime <= config.wallClingDuration)
                    g *= config.wallClingGravityFactor;
                velocity += Vector3.down * (g * dt);
            }
        }

        ApplyLunge(); // the fists' closer holds its line through friction and air control for its beat
        UpdateCapsuleHeight(dt);
        MoveAndSlide(dt);

        if (grounded && !jumpedThisTick) SnapToGround();
        ConstrainSwing();
        CurrPosition = transform.position;

        DetectHiddenTech();
        dashPressed = false;
        slamPressed = false;
    }

    // Speed gained beyond everything the systems above accounted for means the
    // player found a gap in the movement sim. That's a feature: reward it.
    void DetectHiddenTech()
    {
        float unexplained = HorizontalSpeed - hSpeedTickStart - attributedGain;
        if (unexplained <= config.glitchSpeedGainThreshold) return;
        if (Time.time < glitchCooldownUntil) return;

        glitchCooldownUntil = Time.time + config.glitchRewardCooldown;
        MovementEvents.Emit(MoveType.GlitchTech, $"+{unexplained:0.0} m/s");
    }

    void Attribute(float hSpeedBefore)
    {
        attributedGain += Mathf.Max(0f, HorizontalSpeed - hSpeedBefore);
    }

    // Slide steering: WASD ROTATES the slide (momentum preserved: no gain,
    // no loss); input against the slide brakes it gently instead of fighting.
    void SteerSlide(Vector3 wishDir, float dt)
    {
        if (wishDir == Vector3.zero) return;
        Vector3 h = new Vector3(velocity.x, 0f, velocity.z);
        float hMag = h.magnitude;
        if (hMag < 0.5f) return;
        float along = Vector3.Dot(wishDir, h / hMag);
        if (along < -0.5f)
        {
            velocity *= Mathf.Exp(-config.slideBrakeFriction * dt);
            return;
        }
        Vector3 steered = Vector3.RotateTowards(h, wishDir * hMag, config.slideSteerRate * Mathf.Deg2Rad * dt, 0f);
        velocity = new Vector3(steered.x, velocity.y, steered.z);
    }

    // Air control that survives holding W: the part of your input PERPENDICULAR
    // to travel strafe-accelerates (the classic gain), and the forward part
    // STEERS your velocity CPM-style (redirect, no gain). So W+A / W+D bhops
    // build speed instead of being ignored as "already moving that way".
    void AirControl(Vector3 wishDir, float dt)
    {
        if (wishDir == Vector3.zero) return;
        Vector3 h = new Vector3(velocity.x, 0f, velocity.z);
        float hMag = h.magnitude;
        if (hMag < 1f)
        {
            Accelerate(wishDir, Mathf.Min(config.baseRunSpeed * StyleSpeedScale, config.airMaxWishSpeed), config.airAcceleration, dt);
            return;
        }
        Vector3 travel = h / hMag;
        float forward = Vector3.Dot(wishDir, travel);
        Vector3 perp = wishDir - travel * forward;
        if (perp.sqrMagnitude > 0.001f)
            Accelerate(perp.normalized, config.airMaxWishSpeed, config.airAcceleration, dt);
        if (forward > 0.3f)
        {
            Vector3 steered = Vector3.RotateTowards(h, wishDir * hMag,
                config.airControlTurnRate * Mathf.Deg2Rad * dt * forward, 0f);
            velocity = new Vector3(steered.x, velocity.y, steered.z);
        }
    }

    Vector3 ReadWishDir()
    {
        if (look == null) return Vector3.zero;
        float x = (Keybinds.Held(GameAction.Right) ? 1f : 0f) - (Keybinds.Held(GameAction.Left) ? 1f : 0f);
        float z = (Keybinds.Held(GameAction.Forward) ? 1f : 0f) - (Keybinds.Held(GameAction.Back) ? 1f : 0f);
        Vector3 v = new Vector3(x, 0f, z);
        if (v.sqrMagnitude < 1e-4f) return Vector3.zero;
        return Quaternion.Euler(0f, look.Yaw, 0f) * v.normalized;
    }

    public float HorizontalSpeed => new Vector2(velocity.x, velocity.z).magnitude;

    void UpdateSlideState()
    {
        if (sliding)
        {
            if (!grounded || !crouchHeld || HorizontalSpeed < config.slideEndSpeed)
                sliding = false;
        }
        else if (grounded && crouchHeld && !slamming && HorizontalSpeed > config.slideActivationSpeed)
        {
            sliding = true;
            slideStartTime = Time.time;
            MovementEvents.Emit(MoveType.Slide);
        }
    }

    void ApplyFriction(float dt)
    {
        velocity *= Mathf.Exp(-config.groundFriction * dt);
        if (velocity.sqrMagnitude < 0.01f) velocity = Vector3.zero;
    }

    void Accelerate(Vector3 wishDir, float wishSpeed, float accel, float dt)
    {
        if (wishDir == Vector3.zero) return;
        float current = Vector3.Dot(velocity, wishDir);
        float add = wishSpeed - current;
        if (add <= 0f) return;
        float hBefore = HorizontalSpeed;
        velocity += wishDir * Mathf.Min(accel * dt, add);
        Attribute(hBefore);
    }

    // Expressive inputs release an active grapple pull first (spec §3.7:
    // releasable any time) — otherwise the yank overwrites the ability next tick
    // and the pip is silently wasted
    void ReleaseGrappleIfPulling()
    {
        // Jump/dash/slam off an active pull OR swing — launch with your momentum
        if (grapple != null && grapple.IsActive) grapple.Release();
    }

    // Dash (spec §3.4): instant velocity set along the dash direction, brief control override, then normal physics.
    // OMNI (2026-10-08, dashOmni): the direction is the LOOK rotated by the input — W rides the view's forward WITH
    // its pitch (up or down), A/D are the horizontal sideways of the view, S the horizontal backward, diagonals mix,
    // no input = the look (ReadDashDir). The speed rule is the flat dash's: max(dashSpeed, the horizontal speed you
    // had) along the new direction, so a redirect keeps what you had. THE LADDER: the vertical component is then
    // capped at dashVerticalMax (14), under the timed corner boost's jumpImpulse × cornerBoostMaxLift (18): an
    // untimed dash never out-climbs a timed lip jump. The horizontal is |dir.xz| × speed ≤ max(dashSpeed, hBefore),
    // as before, and attributed on the same path. Pip, duration, i-frames, the FOV kick and the sound are untouched.
    void TryDash()
    {
        if (!dashPressed || Dashing) return;
        ReleaseGrappleIfPulling();
        if (stamina == null || !stamina.TrySpend(config.dashCost)) return;

        Vector3 dir = config.dashOmni ? ReadDashDir() : ReadWishDir();
        if (dir == Vector3.zero && look != null)
            dir = Quaternion.Euler(0f, look.Yaw, 0f) * Vector3.forward;
        if (dir == Vector3.zero) return;

        float hBefore = HorizontalSpeed;
        Vector3 v = dir * Mathf.Max(config.dashSpeed, hBefore);   // never a brake: above the dash speed it is a paid redirect at the speed you had (the flat dash stays flat)
        v.y = Mathf.Clamp(v.y, -config.dashVerticalMax, config.dashVerticalMax);   // the ladder's cap, up and down; the horizontal is what the aim gave it
        velocity = v;
        Attribute(hBefore);
        LastDashDir = dir;
        dashStartTime = Time.time;
        dashEndTime = Time.time + config.dashDuration;
        sliding = false;
        slamming = false;
        if (grounded && v.y > 0.01f)
        {
            // A dash UP leaves the floor like a launch: the ground is let go of before SnapToGround (a third of a metre
            // of reach, more than the first tick climbs) can pull it back and flatten it, and the coyote jump is
            // consumed so a press right after is a double jump, not a 9 m/s SET over a 14 m/s rise
            grounded = false;
            jumpConsumedSinceGrounded = true;
        }
        // Above the dash speed it is a redirect, and the feel says so; a steep dash says UP or DOWN (DASH UP / DASH REDIRECT UP in the feed)
        bool redirect = hBefore > config.dashSpeed;
        string detail = dir.y > 0.5f ? (redirect ? "REDIRECT UP" : "UP")
                      : dir.y < -0.5f ? (redirect ? "REDIRECT DOWN" : "DOWN")
                      : redirect ? "REDIRECT" : null;
        MovementEvents.Emit(MoveType.Dash, detail);
    }

    // The omni dash's direction: the look rotated by the input. W (z > 0) rides the view's forward WITH its pitch,
    // A/D are the horizontal right/left of the view, S is the horizontal backward; no input = the look. The pitch is
    // dropped inside the dead zone (|pitch| < dashPitchDeadzone: a slight downward look while running is not a dive)
    // and, on the ground, whenever it points down (you cannot dash into the floor); in the air a downward aim is a
    // real downward dash, a fast fall you chose. PlayerLook.Pitch is positive looking DOWN (the camera's Euler x)
    Vector3 ReadDashDir()
    {
        if (look == null) return Vector3.zero;
        float x = (Keybinds.Held(GameAction.Right) ? 1f : 0f) - (Keybinds.Held(GameAction.Left) ? 1f : 0f);
        float z = (Keybinds.Held(GameAction.Forward) ? 1f : 0f) - (Keybinds.Held(GameAction.Back) ? 1f : 0f);
        float pitch = look.Pitch;
        bool usePitch = Mathf.Abs(pitch) >= config.dashPitchDeadzone && !(grounded && pitch > 0f);
        Quaternion yaw = Quaternion.Euler(0f, look.Yaw, 0f);
        Vector3 flatForward = yaw * Vector3.forward;
        Vector3 forward = usePitch ? Quaternion.Euler(pitch, look.Yaw, 0f) * Vector3.forward : flatForward;
        Vector3 dir = x == 0f && z == 0f
            ? forward                                                              // no input: where you look
            : (yaw * Vector3.right) * x + (z > 0f ? forward : flatForward) * z;    // W rides the pitch; S and the strafes stay level
        return dir.sqrMagnitude < 1e-6f ? Vector3.zero : dir.normalized;
    }

    // Slam (spec §3.6): instant downward velocity, horizontal control locked until landing
    void TrySlam()
    {
        if (!slamPressed || grounded || slamming) return;
        ReleaseGrappleIfPulling();
        if (stamina == null || !stamina.TrySpend(config.slamCost)) return;

        velocity = Vector3.down * config.slamDownSpeed;
        slamStartY = transform.position.y;
        slamming = true;
        sliding = false;
        MovementEvents.Emit(MoveType.Slam);
    }


    void TryJump()
    {
        if (Time.time > jumpBufferedUntil) return;
        if (TryCornerBoostFromPress()) return;   // a press at a lip: the 90° edge turns your speed into height, ahead of the hop, the wall jump and the kick
        bool coyote = !grounded && !jumpConsumedSinceGrounded
                      && Time.time - lastGroundedTime <= config.coyoteTime;
        if (!grounded && !coyote)
        {
            ReleaseGrappleIfPulling();
            if (!TryWallJump() && !TryWallKick() && !AboutToLand()) TryDoubleJump();
            return;
        }

        float hJumpBefore = HorizontalSpeed;
        Vector3 v = velocity;
        // Emits are deferred until after the airborne state is applied, so event
        // handlers (chain tracking) see launch moves as airborne, not parked
        MoveType pendingEmit = MoveType.GlitchTech;
        bool hasPendingEmit = false;
        string pendingDetail = null;
        bool slamStorage = grounded && Time.time - slamLandTime <= config.slamStorageWindow;
        if (slamStorage)
        {
            // Slam storage (spec §3.6): convert impact speed into a horizontal
            // boost in wishdir; ResetDetector refunds the slam's pip
            v.y = config.jumpImpulse;
            // Tiered like the hop: a press inside the perfect window converts in full, a buffered or late one half.
            // The pip comes back either way (ResetDetector), so a slam is never a dead stop in a fight
            bool perfectSlam = Mathf.Abs(lastJumpPressTime - slamLandTime) <= config.perfectBhopWindow;
            float conv = (perfectSlam ? config.slamStorageConversion : config.slamStorageConversion * 0.5f) * StyleSystem.GainScale(MoveType.SlamStorageJump);   // FRESHNESS (STEP 1)
            Vector3 dir = ReadWishDir();
            if (dir == Vector3.zero) dir = new Vector3(v.x, 0f, v.z).normalized;
            if (dir != Vector3.zero)
                v += dir * (slamStoreSpeed * conv);
            slamLandTime = float.NegativeInfinity;
            pendingEmit = MoveType.SlamStorageJump;
            pendingDetail = perfectSlam ? $"+{slamStoreSpeed * conv:0.0} m/s  PERFECT" : $"+{slamStoreSpeed * conv:0.0} m/s";
            hasPendingEmit = true;
        }
        else if (sliding)
        {
            // Perfect slide-jump = a press timed AFTER the slide started, inside the
            // window. A jump buffered from the air lands here on slide tick 0 but was
            // pressed before the slide existed — that's the lazy tier, and it fades
            // at high speed. Timing the press is what earns the unfaded multiplier.
            bool perfect = lastJumpPressTime > slideStartTime   // strictly after on the physics clock: a press pending before the slide's first tick is stamped equal, and that is the lazy tier
                           && lastJumpPressTime - slideStartTime <= config.perfectSlideJumpWindow;
            float mult = perfect
                ? PerfectTierMultiplier(config.slideJumpHorizontalMultiplier, config.perfectSlideJumpMinMultiplier)
                : LazySlideJumpMultiplier(HorizontalSpeed);
            mult = 1f + (mult - 1f) * StyleSystem.GainScale(perfect ? MoveType.PerfectSlideJump : MoveType.SlideJump);   // FRESHNESS (STEP 1)
            v = new Vector3(v.x * mult, config.slideJumpVerticalImpulse, v.z * mult);
            sliding = false;
            pendingEmit = perfect ? MoveType.PerfectSlideJump : MoveType.SlideJump;
            pendingDetail = $"×{mult:0.00}";
            hasPendingEmit = true;
        }
        else
        {
            v.y = config.jumpImpulse;
            if (grounded && Time.time - landTime <= config.bhopFrictionGraceTime && HorizontalSpeed > 2f)
            {
                // Perfect bhop = press within ±window of the landing instant.
                // A lazy early-buffered press still bhops (keeps all speed) but only
                // a genuinely timed press GAINS speed — hardest input, biggest reward.
                float missMs = (lastJumpPressTime - landTime) * 1000f;   // signed: early is negative, late is positive
                bool perfect = Mathf.Abs(lastJumpPressTime - landTime) <= config.perfectBhopWindow;
                if (perfect)
                {
                    float mult = PerfectTierMultiplier(
                        config.perfectBhopSpeedMultiplier, config.perfectBhopMinMultiplier);
                    mult = 1f + (mult - 1f) * StyleSystem.GainScale(MoveType.PerfectBhop);   // FRESHNESS (STEP 1): the gain above the kept speed, by how fresh the hop is
                    v.x *= mult;
                    v.z *= mult;
                    pendingEmit = MoveType.PerfectBhop;
                    pendingDetail = $"×{mult:0.00}  {missMs:+0;-0} ms";
                }
                else
                {
                    pendingEmit = MoveType.Bhop;
                    pendingDetail = $"{missMs:+0;-0} ms";
                }
                hasPendingEmit = true;
            }
            else
            {
                pendingEmit = MoveType.Jump;
                pendingDetail = null;
                hasPendingEmit = true;
            }
        }

        velocity = v;
        Attribute(hJumpBefore);
        grounded = false;
        jumpConsumedSinceGrounded = true;
        jumpedThisTick = true;
        jumpBufferedUntil = float.NegativeInfinity;
        if (hasPendingEmit) MovementEvents.Emit(pendingEmit, pendingDetail);
    }

    // A press just before landing means "bhop", not "double jump": if falling
    // with ground inside the distance we'll cover within the buffer window,
    // leave the press buffered for the landing instead of eating a pip on an
    // accidental air jump. Deliberate double jumps (high up / rising) pass.
    bool AboutToLand()
    {
        if (velocity.y > 0.5f) return false;
        float lookahead = Mathf.Max(0f, -velocity.y) * config.jumpBufferTime
                          + config.doubleJumpMinGroundClearance;
        return CastCapsule(transform.position, Vector3.down, lookahead, out RaycastHit hit)
               && Vector3.Angle(hit.normal, Vector3.up) <= config.maxSlopeAngle;
    }

    // Double jump (spec §3.5): fully redirects horizontal to wishdir at current
    // speed — no free accel, but free redirect. Resets on ground and wall touch.
    void TryDoubleJump()
    {
        if (airJumpsUsed >= config.maxAirJumps) return;
        if (stamina == null || !stamina.TrySpend(config.doubleJumpCost)) return;

        Vector3 dir = ReadWishDir();
        float hSpeed = HorizontalSpeed;
        Vector3 v = dir != Vector3.zero ? dir * hSpeed : new Vector3(velocity.x, 0f, velocity.z);
        v.y = config.doubleJumpImpulse;
        velocity = v;
        slamming = false;
        airJumpsUsed++;
        jumpedThisTick = true;
        jumpBufferedUntil = float.NegativeInfinity;
        MovementEvents.Emit(MoveType.DoubleJump);
    }

    // Proactive wall parry: pressing parry while RUSHING TOWARD a nearby wall
    // fires immediately — no frame-perfect collision required. (The passive
    // path in MoveAndSlide still catches genuine mid-window impacts.)
    // Requires movement toward the wall so running parallel can't farm pips.
    void TryProximityWallParry()
    {
        if (!ParryActive) return;
        float speed = velocity.magnitude;
        if (speed < config.parryMinImpactSpeed) return;
        if (!FindWallAndCorner(out RaycastHit wall, out _, out _)) return;

        Vector3 n = wall.normal;
        n.y = 0f;
        if (n.sqrMagnitude < 1e-4f) return;
        n.Normalize();
        if (Vector3.Dot(velocity / speed, -n) < 0.25f) return; // not heading at it

        ApplyWallParryBlast(n, wall.point);
    }

    // Wall parry = a little explosion at the surface: your velocity REFLECTS
    // off the wall (scaled) plus a blast away from it. A deliberate escape /
    // redirect — parrying walls needlessly throws away your line.
    void ApplyWallParryBlast(Vector3 n, Vector3 point)
    {
        float hBefore = HorizontalSpeed;
        Vector3 r = Vector3.Reflect(velocity, n); r.y = 0f;
        float keep = Mathf.Max(r.magnitude * config.parryBlastSpeedKeep, config.parryBlastPower);   // a floor, not an add: surfaces repel, they never reward
        velocity = (r.sqrMagnitude > 1e-4f ? r.normalized : n) * keep + Vector3.up * config.parryBlastUpPop;
        ParryConnected(point);
        Attribute(hBefore);
        slamming = false;
        sliding = false;
        MovementEvents.Emit(MoveType.Parry, "BLAST");
    }

    // ---- Wall run (Titanfall-flavoured). Airborne, fast, holding forward, with a
    // wall beside you: you stick to it — gravity nearly off, speed kept, a lean
    // into the surface — for a moment, then it sags back to full weight. Jump off
    // it (or off its end, within a beat) for a WALL JUMP: all your speed plus a
    // push, free of stamina. That jump opens the corner-boost combo window.
    void UpdateWallRun(Vector3 wishDir)
    {
        if (!config.wallRunEnabled || Dashing || slamming || swinging) { EndWallRun(); return; }
        Vector3 vh = new Vector3(velocity.x, 0f, velocity.z);
        float hSpeed = vh.magnitude;
        if (hSpeed < config.wallRunMinSpeed) { EndWallRun(); return; }
        Vector3 travel = vh / hSpeed;
        if (wishDir == Vector3.zero || Vector3.Dot(wishDir, travel) < 0.2f) { EndWallRun(); return; } // keep moving along it
        if (wallRunning && Time.time - wallRunStartTime > config.wallRunMaxDuration) { EndWallRun(); return; }

        // A wall beside you, left or right of the way you're going
        Vector3 right = Vector3.Cross(Vector3.up, travel);
        RaycastHit best = default;
        float bestDist = float.MaxValue, side = 0f;
        for (int s = -1; s <= 1; s += 2)
        {
            if (CastCapsule(transform.position, right * s, config.wallRunRange, out RaycastHit hit)
                && Vector3.Angle(hit.normal, Vector3.up) > config.maxSlopeAngle
                && Mathf.Abs(hit.normal.y) < 0.3f
                && hit.distance < bestDist)
            {
                best = hit;
                bestDist = hit.distance;
                side = s;
            }
        }
        if (bestDist == float.MaxValue) { EndWallRun(); return; }
        Vector3 n = best.normal;
        n.y = 0f;
        n.Normalize();
        if (Mathf.Abs(Vector3.Dot(travel, n)) > 0.7f) { EndWallRun(); return; } // along it, not into it
        if (!wallRunning && best.collider == wallRunCollider && Time.time - wallRunDetachTime < config.wallRunReattachLockout) return;

        if (!wallRunning)
        {
            // Attach: the wall catches your fall, a small hop, and the run begins
            wallRunning = true;
            wallRunStartTime = Time.time;
            wallRunCollider = best.collider;
            if (velocity.y < -config.wallRunCatchSpeed) velocity.y = -config.wallRunCatchSpeed;
            if (velocity.y > 0f) velocity.y = 0f;   // a run never keeps a jump's rise: jumped at, a 4 m wall lifted the body 6.5 m (LevelMetricsMath.WallLift); the user's pick, 2026-10-29
            velocity.y += config.wallRunEntryPop;
            airJumpsUsed = 0;
            airWallKicksUsed = 0; // a run is a fresh touch
            MovementEvents.Emit(MoveType.WallRun, side < 0f ? "left" : "right");
        }
        wallRunNormal = n;
        WallRunSide = side;
        lastWallContactTime = Time.time;
    }

    void WallRunMove(Vector3 wishDir, float dt)
    {
        Vector3 n = wallRunNormal;
        float into = Vector3.Dot(velocity, n);
        if (into < 0f) velocity -= n * into;               // never into the wall...
        velocity -= n * (config.wallRunStickAccel * dt);   // ...but lean on it
        // Gravity nearly off, sagging back to full weight as the run ages
        float life = Mathf.Clamp01((Time.time - wallRunStartTime) / config.wallRunMaxDuration);
        float g = config.gravity * Mathf.Lerp(config.wallRunGravityFactor, 1f, life * life);
        velocity += Vector3.down * (g * dt);
        // Holding forward keeps the pace
        Vector3 vh = new Vector3(velocity.x, 0f, velocity.z);
        Vector3 along = Vector3.ProjectOnPlane(wishDir, n);
        if (along.sqrMagnitude > 0.01f)
            Accelerate(along.normalized, Mathf.Max(vh.magnitude, config.wallRunMinSpeed), config.wallRunAccel, dt);
    }

    void EndWallRun()
    {
        if (!wallRunning) return;
        wallRunning = false;
        wallRunEndTime = Time.time;
        wallRunDetachTime = Time.time;
        WallRunSide = 0f;
    }

    // Wall jump: leave a run (or one that ended a beat ago — running off the end
    // of the wall) with everything you carried, a bit more, and a push off. Free:
    // the run was the skill. Opens the corner-boost combo window.
    bool TryWallJump()
    {
        bool recent = !wallRunning && Time.time - wallRunEndTime <= config.wallRunJumpCoyote;
        if (!wallRunning && !recent) return false;
        Vector3 n = wallRunNormal;
        float hBefore = HorizontalSpeed;
        Vector3 vh = new Vector3(velocity.x, 0f, velocity.z);
        Vector3 tangent = vh - n * Vector3.Dot(vh, n);
        Vector3 v = tangent * (1f + (config.wallRunJumpTangentBoost - 1f) * StyleSystem.GainScale(MoveType.WallJump)) + n * config.wallRunJumpNormalImpulse;   // FRESHNESS (STEP 1): the boost over the pace kept scales; the push away does not
        Vector3 inputBoost = ReadWishDir() * config.wallRunJumpInputImpulse;
        float intoWall = Vector3.Dot(inputBoost, n);
        if (intoWall < 0f) inputBoost -= n * intoWall;
        v += inputBoost;
        v.y = Mathf.Max(velocity.y, 0f) * 0.5f + config.wallRunJumpUpImpulse;
        velocity = v;
        Attribute(hBefore);
        EndWallRun();
        lastWallJumpTime = Time.time;
        airJumpsUsed = 0;
        grounded = false;
        sliding = false;
        slamming = false;
        jumpedThisTick = true;
        jumpBufferedUntil = float.NegativeInfinity;
        MovementEvents.Emit(MoveType.WallJump, $"×{config.wallRunJumpTangentBoost:0.00}");
        return true;
    }

    // Wall kick (spec §4.1) / EDGE BOOST, Deadlock-flavored: held movement input
    // shapes the kick (hold away from the wall to fly further), and the first kick
    // per airtime is stamina-free. A kick on the VERTICAL CORNER of a wall — where
    // two faces meet — is the edge boost: it leaves along the sum of both normals
    // (out of the corner, not off one face), bigger impulse, full tangential speed,
    // and ResetDetector refunds the pip.
    bool TryWallKick()
    {
        if (!FindWallAndCorner(out RaycastHit wall, out bool corner, out Vector3 n2)) return false;

        Vector3 n = wall.normal;
        n.y = 0f;
        if (n.sqrMagnitude < 1e-4f) return false;
        n.Normalize();

        bool free = airWallKicksUsed < config.freeWallKicksPerAirtime;
        if (!free && (stamina == null || !stamina.TrySpend(config.wallKickCost))) return false;

        float hBefore = HorizontalSpeed;
        float mult = corner ? 1f + (config.edgeBoostImpulseMultiplier - 1f) * StyleSystem.GainScale(MoveType.EdgeBoost) : 1f;   // FRESHNESS (STEP 1): the corner's extra over a plain kick scales
        float keep = corner ? 1f : config.wallKickTangentKeep;
        bool twoFaces = corner && n2 != Vector3.zero;   // a marked corner with one face found still kicks off that face
        Vector3 kickDir = n;
        if (twoFaces)
        {
            Vector3 sum = n + n2;
            if (sum.sqrMagnitude > 0.25f) kickDir = sum.normalized;
        }

        Vector3 vh = new Vector3(velocity.x, 0f, velocity.z);
        Vector3 tangent = vh - n * Vector3.Dot(vh, n);
        if (twoFaces)
        {
            float into2 = Vector3.Dot(tangent, n2);   // nothing into the second face either
            if (into2 < 0f) tangent -= n2 * into2;
        }
        Vector3 v = tangent * keep + kickDir * (config.wallKickNormalImpulse * mult);

        // Deadlock-style input shaping: add impulse along held input, minus any
        // component pointing back into the wall (either face of a corner)
        Vector3 inputBoost = ReadWishDir() * config.wallKickInputImpulse;
        float intoWall = Vector3.Dot(inputBoost, n);
        if (intoWall < 0f) inputBoost -= n * intoWall;
        if (twoFaces)
        {
            float into2 = Vector3.Dot(inputBoost, n2);
            if (into2 < 0f) inputBoost -= n2 * into2;
        }
        v += inputBoost;

        v.y = config.wallKickUpImpulse * mult;
        velocity = v;
        Attribute(hBefore);

        airWallKicksUsed++;
        airJumpsUsed = 0; // wall touch resets the double jump (spec §3.2)
        lastWallContactTime = Time.time;
        grounded = false;
        sliding = false;
        slamming = false;
        jumpedThisTick = true;
        jumpBufferedUntil = float.NegativeInfinity;

        if (corner) MovementEvents.Emit(MoveType.EdgeBoost, $"×{mult:0.0}");
        else MovementEvents.Emit(MoveType.WallKick, free ? "free" : null);
        return true;
    }

    static readonly RaycastHit[] wallScan = new RaycastHit[8];

    // Scan 8 horizontal directions for the nearest wall; a second wall face whose
    // normal is more than edgeBoostCornerAngle from it (and not the facing wall of
    // a corridor), with its contact within edgeBoostCornerReach of the kick's, is a
    // vertical corner: `secondNormal` is that face's horizontal normal (zero when
    // only an authored CornerMarker says it is a corner).
    bool FindWallAndCorner(out RaycastHit best, out bool corner, out Vector3 secondNormal)
    {
        best = default;
        corner = false;
        secondNormal = Vector3.zero;
        float bestDist = float.MaxValue;
        int found = 0;

        for (int i = 0; i < 8; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
            if (CastCapsule(transform.position, dir, config.wallKickRange, out RaycastHit hit)
                && Vector3.Angle(hit.normal, Vector3.up) > config.maxSlopeAngle)
            {
                wallScan[found++] = hit;
                if (hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    best = hit;
                }
            }
        }
        if (bestDist == float.MaxValue) return false;

        for (int i = 0; i < found; i++)
        {
            RaycastHit h = wallScan[i];
            float apart = Vector3.Angle(h.normal, best.normal);
            if (apart > config.edgeBoostCornerAngle && apart <= 135f
                && Vector3.Distance(h.point, best.point) <= config.edgeBoostCornerReach)
            {
                corner = true;
                secondNormal = h.normal;
                secondNormal.y = 0f;
                secondNormal = secondNormal.sqrMagnitude > 1e-4f ? secondNormal.normalized : Vector3.zero;
                break;
            }
        }
        // Authored corner hitboxes are authoritative — a marked corner always counts
        if (!corner && NearMarker<CornerMarker>(best.point, config.cornerMarkerRadius))
            corner = true;
        return true;
    }

    static readonly Collider[] markerHits = new Collider[8];

    bool NearMarker<T>(Vector3 point, float radius) where T : Component
    {
        int n = Physics.OverlapSphereNonAlloc(point, radius, markerHits, 1 << 2, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
            if (markerHits[i] != null && markerHits[i].GetComponent<T>() != null) return true;
        return false;
    }

    float LazySlideJumpMultiplier(float hSpeed)
    {
        float t = Mathf.InverseLerp(config.slideJumpFadeStartSpeed, config.slideJumpFadeEndSpeed, hSpeed);
        return Mathf.Lerp(config.slideJumpHorizontalMultiplier, 1f, t);
    }

    // Perfect tiers fade gently at high speed so compounding stays exponential
    // but tame. The floors keep the ladder ordered at every speed:
    // perfect bhop > perfect slide-jump > everything else.
    public float PerfectTierMultiplier(float full, float floor)
    {
        float t = Mathf.InverseLerp(config.perfectBhopFadeStartSpeed, config.perfectBhopFadeEndSpeed, HorizontalSpeed);
        return Mathf.Lerp(full, floor, t);
    }

    void GroundCheck()
    {
        bool wasGrounded = grounded;
        bool wasSliding = sliding;
        // Skip the probe only when moving AWAY from the ground plane (jump, kick
        // pop). Raw +y won't do: uphill slope-aligned velocity legitimately has
        // vy > 0 and must stay grounded, or every ramp ascent goes airborne.
        Vector3 planeNormal = wasGrounded ? groundNormal : Vector3.up;
        bool movingAwayFromGround = Vector3.Dot(velocity, planeNormal) > 0.1f;
        grounded = false;
        if (movingAwayFromGround) return;

        if (CastCapsule(transform.position, Vector3.down, GroundCheckDist + Skin, out RaycastHit hit)
            && Vector3.Angle(hit.normal, Vector3.up) <= config.maxSlopeAngle)
        {
            grounded = true;
            groundNormal = hit.normal;
            lastGroundedTime = Time.time;
            jumpConsumedSinceGrounded = false;
            airJumpsUsed = 0;
            airWallKicksUsed = 0;
            if (!wasGrounded)
            {
                landTime = Time.time;
                bool slamWasActive = slamming;
                if (Vector3.Angle(hit.normal, Vector3.up) < 5f)
                    FlatGroundTouchCount++; // chain-of-N keys off this (spec §3.3)
                if (slamming)
                {
                    // Slam landing: storage window opens; CombatDirector's shockwave and GameFeel's thud hang off the
                    // event. A slam STORES WHAT IT FELL: the storable speed is what the drop alone would have given
                    // you under gravity, capped at the slam's own — a hop stores a hop, a real drop stores everything
                    slamImpactSpeed = Mathf.Abs(velocity.y);
                    LastSlamDrop = Mathf.Max(0f, slamStartY - transform.position.y);
                    slamStoreSpeed = config.slamStorageFromDrop
                        ? Mathf.Min(slamImpactSpeed, Mathf.Sqrt(2f * config.gravity * LastSlamDrop))
                        : slamImpactSpeed;
                    slamLandTime = Time.time;
                    slamming = false;
                    MovementEvents.Emit(MoveType.SlamLand, $"{slamImpactSpeed:0.0} m/s");
                }
                // Landing keeps horizontal momentum, drops the fall component.
                // Attributed: slope landings convert some fall speed to horizontal
                // routinely — that's systemic, not discovered tech
                float fallSpeed = -velocity.y;
                float hBefore = HorizontalSpeed;
                velocity = Vector3.ProjectOnPlane(velocity, hit.normal);
                if (fallSpeed >= config.parryMinImpactSpeed && ParryActive)
                {
                    // FLOOR PARRY: the explosion throws you back the way you
                    // came — the fall reflects upward, horizontal untouched.
                    // Same rule as walls: surfaces repel, they don't reward.
                    float up = Mathf.Max(config.parryPogoMinUpSpeed,
                        fallSpeed * config.parryPogoBounce);
                    velocity = new Vector3(velocity.x, up, velocity.z);
                    grounded = false;
                    slamLandTime = float.NegativeInfinity; // the bounce consumed the landing
                    ParryConnected(transform.position);
                    MovementEvents.Emit(MoveType.Parry, $"POGO ↑{up:0.0} m/s");
                }
                Attribute(hBefore);
                if (!slamWasActive)
                    MovementEvents.Emit(MoveType.Land, fallSpeed.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
            }
        }
        else if (wasGrounded && !jumpedThisTick)
        {
            sliding = false;   // the ground left the slide: it ends, and what you carried is yours (a lip pays nothing by itself)
        }
    }

    // ---- CORNER BOOST (Deadlock's): jump ON a 90° edge — the lip where a wall meets its top — and your speed
    // becomes HEIGHT. The lip is found in the motor's own contacts: a wall contact whose top surface lies within
    // cornerBoostLipReach above the feet (one short ray, only on such a contact). A jump press within
    // ±cornerBoostWindow of that contact converts in full; out to cornerBoostLazyWindow it converts at the lazy rate;
    // later it is a plain jump. vertical = max(jumpImpulse, h × conversion) + pop, ×1.6 off a wall jump (the WALL
    // COMBO), capped at jumpImpulse × cornerBoostMaxLift; horizontal is KEPT × cornerBoostKeep — never gained, so a
    // perfect hop chain still owns horizontal speed. The edge pushes you over it (the feet rise to the lip when
    // there is room), everything through Launch, attributed. It resets the double jump the way any wall touch does,
    // not the free wall kick, and never starts the wall cling.

    // The lip under a wall contact: from just above the contact, a hair inside the wall, a short ray down finds the
    // wall's top. A walkable top within reach above the feet is a lip; a tall wall's inside, a floor at the feet
    // or a unit are not.
    bool LipAbove(RaycastHit wall, float feetY, out float lipY)
    {
        lipY = 0f;
        Vector3 n = wall.normal;
        n.y = 0f;
        if (n.sqrMagnitude < 1e-4f) return false;
        n.Normalize();
        float reach = config.cornerBoostLipReach;
        Vector3 origin = new Vector3(wall.point.x, feetY + reach + LipProbeUp, wall.point.z) - n * LipProbeInset;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit top, reach + LipProbeUp + LipProbeDown, mask, QueryTriggerInteraction.Ignore)) return false;
        if (Vector3.Angle(top.normal, Vector3.up) > config.maxSlopeAngle) return false;
        float above = top.point.y - feetY;
        if (above < LipMinAbove || above > reach) return false;
        if (top.collider.GetComponentInParent<Enemy>() != null) return false;   // a head is not a ledge
        lipY = top.point.y;
        return true;
    }

    // A wall contact in the slide loop: remember the lip for a press to come, and pay a press already stamped
    // inside the window now. Returns true when the boost fired (the loop re-casts from over the edge)
    bool TryCornerBoostAtContact(ref Vector3 pos, RaycastHit hit, float intoSurface, float hBefore)
    {
        if (wallRunning || intoSurface < 1f || hBefore < 2f) return false;
        if (!LipAbove(hit, pos.y, out float lipY)) return false;
        Vector3 vh = new Vector3(velocity.x, 0f, velocity.z);   // what you brought to the edge, before the wall takes it
        Vector3 n = hit.normal;
        n.y = 0f;
        lipContactTime = Time.time;
        lipTopY = lipY;
        lipSpeed = hBefore;
        lipTravel = vh / hBefore;
        lipNormal = n.sqrMagnitude > 1e-4f ? n.normalized : -lipTravel;
        float miss = lastJumpPressTime - Time.time;   // ≤ 0: the press came first
        if (lastJumpPressTime == cornerPaidPressTime || -miss > config.cornerBoostLazyWindow) return false;
        PayCornerBoost(ref pos, hBefore, lipTravel, lipY, miss);
        return true;
    }

    // A jump press: a lip met inside the window pays now (the wall took your speed at the contact; the boost gives
    // it back). Airborne with a lip dead ahead inside wallKickRange, the contact is imminent: it pays at once,
    // early by the time to the edge, so the kick never hijacks a press aimed at a ledge
    bool TryCornerBoostFromPress()
    {
        if (lastJumpPressTime == cornerPaidPressTime) return false;
        Vector3 pos = transform.position;
        if (Time.time - lipContactTime <= config.cornerBoostLazyWindow && lastJumpPressTime >= lipContactTime)
        {
            float above = lipTopY - pos.y;
            if (above >= -LipProbeDown && above <= config.cornerBoostLipReach + LipProbeUp
                && CastCapsule(pos, -lipNormal, config.wallKickRange, out RaycastHit wall)
                && Vector3.Angle(wall.normal, Vector3.up) > config.maxSlopeAngle)
            {
                PayCornerBoost(ref pos, lipSpeed, lipTravel, lipTopY, lastJumpPressTime - lipContactTime);
                transform.position = pos;
                return true;
            }
            lipContactTime = float.NegativeInfinity;   // gone stale: you left the edge
        }
        if (grounded) return false;   // on the ground a hop that clears the step is a hop: only a real contact converts
        Vector3 vh = new Vector3(velocity.x, 0f, velocity.z);
        float h = vh.magnitude;
        if (h < 2f) return false;
        Vector3 travel = vh / h;
        if (!CastCapsule(pos, travel, config.wallKickRange, out RaycastHit ahead)
            || Vector3.Angle(ahead.normal, Vector3.up) <= config.maxSlopeAngle || ahead.normal.y < -0.5f) return false;
        if (!LipAbove(ahead, pos.y, out float lipY)) return false;
        Vector3 n = ahead.normal;
        n.y = 0f;
        float into = n.sqrMagnitude > 1e-4f ? -Vector3.Dot(vh, n.normalized) : h;
        if (into < 1f) return false;
        float eta = ahead.distance / into;   // seconds to the edge at this speed
        if (eta > config.cornerBoostLazyWindow) return false;   // too early: this press is a kick or a double
        PayCornerBoost(ref pos, h, travel, lipY, -eta);
        transform.position = pos;
        return true;
    }

    void PayCornerBoost(ref Vector3 pos, float hSpeed, Vector3 travel, float lipY, float missSeconds)
    {
        ReleaseGrappleIfPulling();
        bool perfect = Mathf.Abs(missSeconds) <= config.cornerBoostWindow;
        float conv = perfect ? config.cornerBoostConversion : config.cornerBoostLazyConversion;
        float up = Mathf.Max(config.jumpImpulse, hSpeed * conv) + (perfect ? config.cornerBoostPop : config.cornerBoostPop * 0.5f);   // the lazy tier always pays less, at every speed (the skill gate)
        bool combo = Time.time - lastWallJumpTime <= config.wallJumpCornerComboWindow;
        float comboMult = combo ? config.wallJumpCornerComboMultiplier : 1f;
        up = Mathf.Min(up * comboMult, config.jumpImpulse * config.cornerBoostMaxLift);   // THE CAP: high, never higher
        if (combo) lastWallJumpTime = float.NegativeInfinity;   // one combo per wall jump
        // The edge pushes you over it: the feet rise to the lip when there is room above (Deadlock's hull push)
        float rise = lipY + LipClear - pos.y;
        if (rise > 0f && rise <= config.cornerBoostLipReach + LipProbeUp + LipClear && ClearAt(pos + Vector3.up * rise))
            pos.y = lipY + LipClear;
        Launch(travel * (hSpeed * config.cornerBoostKeep) + Vector3.up * up);   // attributed; keep ≤ 1, so nothing horizontal is ever gained
        airJumpsUsed = 0;   // a wall touch resets the double jump (spec §3.2); the free kick and the cling are not touched
        jumpedThisTick = true;
        jumpBufferedUntil = float.NegativeInfinity;
        cornerPaidPressTime = lastJumpPressTime;
        lipContactTime = float.NegativeInfinity;
        float missMs = missSeconds * 1000f;
        string verdict = perfect ? $"+{up:0.0} v  {missMs:+0;-0} ms  PERFECT" : $"+{up:0.0} v  {missMs:+0;-0} ms";
        MovementEvents.Emit(MoveType.CornerBoost, combo ? $"WALL COMBO ×{comboMult:0.0}: {verdict}" : verdict);
    }

    bool ClearAt(Vector3 pos)
    {
        GetCapsulePoints(pos, out Vector3 p1, out Vector3 p2);
        return !Physics.CheckCapsule(p1, p2, config.capsuleRadius - Skin, mask, QueryTriggerInteraction.Ignore);
    }

    void SnapToGround()
    {
        if (CastCapsule(transform.position, Vector3.down, SnapDist, out RaycastHit hit)
            && Vector3.Angle(hit.normal, Vector3.up) <= config.maxSlopeAngle)
        {
            transform.position += Vector3.down * Mathf.Max(hit.distance - Skin, 0f);
            groundNormal = hit.normal;
            // Deliberately UNattributed: this magnitude-preserving realign can
            // convert fall speed to horizontal at slope-to-flat transitions at
            // very high speed — that's an intentional hidden-tech gap
            velocity = AlignToPlane(velocity, hit.normal);
        }
        else
        {
            grounded = false;
            sliding = false;
        }
    }

    void UpdateCapsuleHeight(float dt)
    {
        bool wantShort = sliding || (!grounded && crouchHeld);
        float target = wantShort ? config.slideCapsuleHeight : config.capsuleHeight;

        // Standing back up needs headroom
        if (target > currentHeight && !HasHeadroom(target)) return;

        currentHeight = Mathf.MoveTowards(currentHeight, target, config.crouchTransitionSpeed * dt);
        capsule.height = currentHeight;
        capsule.center = Vector3.up * (currentHeight * 0.5f);
    }

    bool HasHeadroom(float height)
    {
        Vector3 pos = transform.position;
        Vector3 p1 = pos + Vector3.up * config.capsuleRadius;
        Vector3 p2 = pos + Vector3.up * (height - config.capsuleRadius);
        return !Physics.CheckCapsule(p1, p2, config.capsuleRadius - Skin, mask, QueryTriggerInteraction.Ignore);
    }

    void MoveAndSlide(float dt)
    {
        Vector3 pos = transform.position;
        float remaining = dt;
        for (int i = 0; i < MaxSlideIterations && remaining > 1e-5f; i++)
        {
            Vector3 step = velocity * remaining;
            float dist = step.magnitude;
            if (dist < 1e-6f) break;
            Vector3 dir = step / dist;

            if (CastCapsule(pos, dir, dist + Skin, out RaycastHit hit))
            {
                // Breakable glass: hit it square with enough speed and you go THROUGH
                var glass = hit.collider.GetComponent<BreakableGlass>();
                if (glass != null && glass.TryBreak(velocity, hit.normal, hit.point))
                {
                    velocity *= config.glassBreakSpeedKeep;
                    continue; // the pane is gone — re-cast the same step
                }
                bool isWall = Vector3.Angle(hit.normal, Vector3.up) > config.maxSlopeAngle;
                // TACKLE-THROUGH (Fists): a unit met from the side at speed with Strike down is not a wall; the
                // capsule keeps going and the unit is ghosted to it for a beat. Units are ground too: from above
                // you still land on a head, so only a wall-angled contact can be a tackle
                if (isWall && Fists.Instance != null && Fists.Instance.Active)
                {
                    var unit = hit.collider.GetComponentInParent<Enemy>();
                    if (unit != null)
                    {
                        float hTackle = HorizontalSpeed;
                        if (Fists.Instance.TryTackle(unit, ref velocity, hit))
                        {
                            Attribute(hTackle);
                            continue; // the body is behind you now — re-cast the same step through it
                        }
                    }
                }
                float travel = Mathf.Max(hit.distance - Skin, 0f);
                pos += dir * travel;
                remaining -= remaining * (travel / dist);
                float intoSurface = -Vector3.Dot(velocity, hit.normal);
                float hBefore = HorizontalSpeed;
                if (isWall && intoSurface >= config.parryMinImpactSpeed && ParryActive)
                {
                    // WALL PARRY on a genuine impact: same blast physics
                    Vector3 wallNormal = hit.normal;
                    wallNormal.y = 0f;
                    if (wallNormal.sqrMagnitude > 1e-4f)
                        ApplyWallParryBlast(wallNormal.normalized, hit.point);
                    else
                        velocity = Vector3.ProjectOnPlane(velocity, hit.normal);
                    Attribute(hBefore);
                }
                else if (isWall && hit.normal.y > -0.5f && TryCornerBoostAtContact(ref pos, hit, intoSurface, hBefore))
                {
                    continue;   // CORNER BOOST: the edge turned your speed into height — re-cast the rest of the step from over the lip
                }
                else
                {
                    if (isWall && intoSurface >= config.parryMinImpactSpeed && Time.time - lastWallHitEmit > 0.25f && glass == null && hit.normal.y > -0.5f && hit.collider.GetComponentInParent<Enemy>() == null)   // a wall: not a pane that spoke for itself, not a ceiling, not a body
                    {
                        lastWallHitEmit = Time.time;   // the wall took this much: say so (GameFeel and the HUD listen)
                        MovementEvents.Emit(MoveType.WallHit, intoSurface.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
                    }
                    velocity = Vector3.ProjectOnPlane(velocity, hit.normal);
                    Attribute(hBefore); // routine surface redirect, not discovered tech
                }
                if (isWall)
                {
                    // Wall touch: resets the double jump, starts the cling window
                    lastWallContactTime = Time.time;
                    airJumpsUsed = 0;
                }
            }
            else
            {
                pos += step;
                remaining = 0f;
            }
        }
        Depenetrate(ref pos);
        transform.position = pos;
    }

    void Depenetrate(ref Vector3 pos)
    {
        GetCapsulePoints(pos, out Vector3 p1, out Vector3 p2);
        Collider[] overlaps = Physics.OverlapCapsule(p1, p2, config.capsuleRadius, mask, QueryTriggerInteraction.Ignore);
        foreach (Collider col in overlaps)
        {
            if (col == capsule) continue;
            if (Ghosted(col)) continue; // mid-tackle: the unit is not solid to you until you are through it
            if (Physics.ComputePenetration(capsule, pos, transform.rotation,
                    col, col.transform.position, col.transform.rotation,
                    out Vector3 dir, out float depth))
            {
                pos += dir * (depth + 0.001f);
            }
        }
    }

    /// A unit you tackled a beat ago is not solid to the capsule (the pass goes all the way through it)
    static bool Ghosted(Collider col)
    {
        var fists = Fists.Instance;
        if (fists == null || fists.GhostCount == 0) return false;
        var unit = col.GetComponentInParent<Enemy>();
        return unit != null && fists.Ghosted(unit);
    }

    bool CastCapsule(Vector3 pos, Vector3 dir, float dist, out RaycastHit hit)
    {
        GetCapsulePoints(pos, out Vector3 p1, out Vector3 p2);
        float r = config.capsuleRadius - Skin * 0.5f;
        if (Fists.Instance == null || Fists.Instance.GhostCount == 0)
            return Physics.CapsuleCast(p1, p2, r, dir, out hit, dist, mask, QueryTriggerInteraction.Ignore);
        // Something is being tackled through: the nearest hit that is not it (allocates only during a pass)
        hit = default;
        float best = float.PositiveInfinity;
        foreach (var h in Physics.CapsuleCastAll(p1, p2, r, dir, dist, mask, QueryTriggerInteraction.Ignore))
        {
            if (h.distance >= best || Ghosted(h.collider)) continue;
            best = h.distance;
            hit = h;
        }
        return best < float.PositiveInfinity;
    }

    void GetCapsulePoints(Vector3 bottomPos, out Vector3 p1, out Vector3 p2)
    {
        p1 = bottomPos + Vector3.up * config.capsuleRadius;
        p2 = bottomPos + Vector3.up * (currentHeight - config.capsuleRadius);
    }

    // Redirect onto a plane without losing magnitude — momentum is never taken by systems (spec §1)
    static Vector3 AlignToPlane(Vector3 v, Vector3 n)
    {
        Vector3 projected = Vector3.ProjectOnPlane(v, n);
        return projected.sqrMagnitude > 1e-8f ? projected.normalized * v.magnitude : projected;
    }
}
