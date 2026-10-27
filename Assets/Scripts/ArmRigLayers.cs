using UnityEngine;

/// <summary>
/// The procedural layers, additive over whatever the knuckles author, every one a damped spring or a
/// critically damped smooth, every amplitude a viewmodel* field. SHARED (both hands, and a share on the shoulders):
/// SWAY (the hands lag the look), BOB (a stride-synced figure-eight on the ground, growing with speed, gone in the
/// air), BREATH (idle), LAND (a dip and bounce from the landing speed), JUMP (a lift), DASH (pulled back, thrown
/// forward), SLIDE (the hands drop), HIT SHUDDER (a short high-frequency rattle on a connect). PER HAND: the free
/// hand reaches for the ground in a slide; on a wall run the near free hand PLANTS flat on the wall (found by a ray
/// to the motor's side) while the other arm pumps; over the run speed the arms PUMP counter-phase; a TACKLE drops
/// the right shoulder into the frame and springs it back.
/// THE ROOT (2026-10-12, THE FINALS' grammar): the whole rig — shoulders, arms, hands and the weapons under it — sits
/// on one more spring, the implied torso. It drops and tilts in a sprint, takes a share of the landing's compression
/// and tilts into it, rolls into a turn, leans with a slide, goes with the dash, shifts its weight idling, and
/// LEANS into every attack the weapon files ask for (Lean = an impulse that springs back with overshoot; LeanTo = a
/// held offset for this frame, for a wind-up). The locomotion part is damped by the quietest hand's layer scale so
/// a calibrated cut keeps its crosshair.
/// </summary>
public partial class ArmRig
{
    Vector3 sharedPos, sharedRot;                       // this frame's shared offset: metres, and degrees about the camera's axes
    Vector3 rootPos, rootPosVel, rootRot, rootRotVel;   // THE ROOT's spring: metres, degrees about the camera's axes
    Vector3 rootHoldPos, rootHoldRot;                   // a held lean, good for this frame only
    int rootHoldFrame = -1;
    float rootScale = 1f;                               // the quietest hand's layer scale this frame
    float yawRate, pitchRate;                           // the look's rates, degrees per second (world time)
    Vector3 swayPos, swayPosVel, swayRot, swayRotVel;
    float prevYaw, prevPitch;
    bool lookInit;
    float stridePhase, bobW, bobWVel;
    float landY, landYVel;
    bool prevGrounded;
    float prevVy;
    Vector3 dashOff, dashVel;
    float slideW, slideWVel;
    float wallW, wallWVel;
    ArmPose wallPose;
    HandSide wallHand;
    float pumpW, pumpWVel;
    float shudderAmp, shudderT;
    Vector3 tackleOff, tackleVel;
    float tuckUntil = -10f, tuckSeconds, tuckW, tuckWVel;   // THE TUCK: the tricks and the landing roll pull the arms in

    void OnEnable() => MovementEvents.MoveExecuted += OnMove;
    void OnDisable() => MovementEvents.MoveExecuted -= OnMove;

    /// A connect's rattle through both arms (0..1)
    public void Shudder(float amount) => shudderAmp = Mathf.Clamp01(Mathf.Max(shudderAmp, amount));

    /// THE TUCK (2026-10-23, TrickSystem and CameraBody's roll): for `seconds` both hands pull in to the chest and the
    /// root pitches with them, in over the first beat and out over the last fifth, on the rig's own blend
    public void Tuck(float seconds)
    {
        tuckSeconds = Mathf.Max(0.1f, seconds);
        tuckUntil = Time.time + tuckSeconds;
    }

    /// The tackle: the right shoulder drops into the frame and springs back
    public void Tackle()
    {
        Vector3 to = config != null ? config.viewmodelTackleShoulder : new Vector3(-0.12f, -0.07f, 0.17f);
        float omega = Omega(config != null ? config.viewmodelTackleFreq : 5f);
        tackleVel += to * omega;
    }

    /// THE ROOT leans: an impulse on the torso's spring (metres, degrees about the camera's axes: x pitch, y yaw, z
    /// roll) that peaks near the amounts given and springs back with the root's overshoot. The knuckles lean the
    /// body into every punch and kick with it
    public void Lean(Vector3 pos, Vector3 rotDeg)
    {
        float k = config != null ? Mathf.Max(0f, config.viewmodelRootLeanScale) : 1f;
        float omega = Omega(config != null ? config.viewmodelRootFreq : 4f);
        rootPosVel += pos * (k * omega);
        rootRotVel += rotDeg * (k * omega);
    }

    /// A held lean, good for this frame only: the spring follows it while it is asked for (a wind-up, a plant) and
    /// comes home when it stops
    public void LeanTo(Vector3 pos, Vector3 rotDeg)
    {
        float k = config != null ? Mathf.Max(0f, config.viewmodelRootLeanScale) : 1f;
        rootHoldPos = pos * k;
        rootHoldRot = rotDeg * k;
        rootHoldFrame = Time.frameCount;
    }

    void OnMove(MoveType type, string detail)
    {
        if (!Built) return;
        switch (type)
        {
            case MoveType.Jump: case MoveType.DoubleJump: case MoveType.Bhop: case MoveType.PerfectBhop:
            case MoveType.SlideJump: case MoveType.PerfectSlideJump: case MoveType.WallJump: case MoveType.WallKick:
            case MoveType.SlamStorageJump:
                landYVel += (config != null ? config.viewmodelJumpLift : 0.03f) * Omega(config != null ? config.viewmodelLandFreq : 5.5f);
                break;
            case MoveType.Dash:
            {
                // The dash's own direction (omni: up and down count), in the camera's frame: the hands are pulled against it
                Vector3 d = motor != null ? motor.LastDashDir : Vector3.zero;
                Vector3 dir = d.sqrMagnitude > 1e-4f ? transform.InverseTransformDirection(d.normalized) : Vector3.forward;
                dashVel -= dir.normalized * ((config != null ? config.viewmodelDashPull : 0.1f) * Omega(config != null ? config.viewmodelDashFreq : 4.5f));
                break;
            }
            case MoveType.Fist:
                if (detail == null || detail == "MISS") break;
                Shudder(detail == "EXECUTE" ? 1f : 0.8f);
                if (detail == "TACKLE" || detail == "PERFECT TACKLE") Tackle();
                break;
            case MoveType.Parry:
                Shudder(1f);
                break;
            case MoveType.SlamLand:
                landYVel -= (config != null ? config.viewmodelLandMax : 0.14f) * Omega(config != null ? config.viewmodelLandFreq : 5.5f);
                break;
        }
    }

    void UpdateLayers(float dt)
    {
        var c = config;
        float now = Time.unscaledTime;
        float sdt = Mathf.Min(Time.deltaTime, 0.05f);   // the motion-driven clocks run on the world's time: they stop in a freeze
        var look = motor != null ? motor.look : null;
        bool grounded = motor != null && motor.Grounded;
        bool sliding = motor != null && motor.Sliding;
        float hSpeed = motor != null ? motor.HorizontalSpeed : 0f;
        float run = c != null ? Mathf.Max(1f, c.baseRunSpeed) : 8f;
        float blend = c != null ? Mathf.Max(1f, c.viewmodelBlendRate) : 14f;

        // SWAY: the look's delta is an impulse on a spring that rests at zero
        float swayPosK = c != null ? c.viewmodelSwayPos : 0.0022f, swayRotK = c != null ? c.viewmodelSwayRot : 0.16f;
        float swayOmega = Omega(c != null ? c.viewmodelSwayFreq : 7f), swayZeta = c != null ? Mathf.Clamp(c.viewmodelSwayDamping, 0.05f, 2f) : 0.7f;
        if (look != null)
        {
            if (!lookInit) { prevYaw = look.Yaw; prevPitch = look.Pitch; lookInit = true; }
            float dYaw = Mathf.DeltaAngle(prevYaw, look.Yaw), dPitch = look.Pitch - prevPitch;
            prevYaw = look.Yaw; prevPitch = look.Pitch;
            dYaw = Mathf.Clamp(dYaw, -40f, 40f); dPitch = Mathf.Clamp(dPitch, -40f, 40f);
            swayPosVel += new Vector3(-dYaw, dPitch, 0f) * (swayPosK * swayOmega);
            swayRotVel += new Vector3(-dPitch, -dYaw, -dYaw * 0.5f) * (swayRotK * swayOmega);
            // The look's rates for the root (a frozen frame reads as stillness, not a divide)
            if (sdt > 1e-4f) { yawRate = dYaw / sdt; pitchRate = dPitch / sdt; } else { yawRate = pitchRate = 0f; }
        }
        Spring(ref swayPos, ref swayPosVel, Vector3.zero, swayOmega, swayZeta, dt);
        Spring(ref swayRot, ref swayRotVel, Vector3.zero, swayOmega, swayZeta, dt);
        float swayMax = c != null ? c.viewmodelSwayMax : 0.07f;
        swayPos = Vector3.ClampMagnitude(swayPos, swayMax);
        swayRot = Vector3.ClampMagnitude(swayRot, 12f);

        // BOB: the stride clock runs on ground covered; the figure-eight fades in on the ground and out in the air
        float stride = c != null ? Mathf.Max(0.3f, c.viewmodelBobStride) : 2.7f;
        bool walking = grounded && !sliding && hSpeed > 0.5f;
        if (walking) stridePhase += hSpeed / stride * (2f * Mathf.PI) * sdt;
        if (stridePhase > 1000f) stridePhase -= 1000f;
        Spring(ref bobW, ref bobWVel, walking ? 1f : 0f, c != null ? Mathf.Max(1f, c.viewmodelBobBlend) : 9f, 1f, dt);
        float bobAmp = (c != null ? c.viewmodelBobAmp : 0.011f) * Mathf.Min(hSpeed / run, c != null ? c.viewmodelBobMax : 1.7f) * bobW;
        float bobRot = (c != null ? c.viewmodelBobRot : 1.4f) * Mathf.Min(hSpeed / run, 1.7f) * bobW;
        Vector3 bobPos = new Vector3(Mathf.Sin(stridePhase), Mathf.Sin(2f * stridePhase) * 0.5f, 0f) * bobAmp;
        Vector3 bobR = new Vector3(Mathf.Sin(2f * stridePhase) * 0.5f, 0f, Mathf.Sin(stridePhase)) * bobRot;

        // BREATH
        float bAmp = c != null ? c.viewmodelBreathAmp : 0.0045f, bRate = c != null ? c.viewmodelBreathRate : 0.27f;
        float bPh = now * bRate * 2f * Mathf.PI;
        Vector3 breath = new Vector3(0f, Mathf.Sin(bPh), Mathf.Cos(bPh * 0.5f) * 0.3f) * bAmp;
        Vector3 breathR = new Vector3(Mathf.Sin(bPh) * 0.7f, 0f, Mathf.Sin(bPh * 0.5f) * 0.4f);

        // LAND and JUMP share one vertical spring: a landing dips it by the fall speed, a jump lifts it
        float landOmega = Omega(c != null ? c.viewmodelLandFreq : 5.5f), landZeta = c != null ? Mathf.Clamp(c.viewmodelLandDamping, 0.05f, 2f) : 0.42f;
        if (motor != null)
        {
            if (grounded && !prevGrounded)
            {
                float fall = Mathf.Max(0f, -prevVy);
                landYVel -= Mathf.Min(fall * (c != null ? c.viewmodelLandImpulse : 0.011f), c != null ? c.viewmodelLandMax : 0.14f) * landOmega;
            }
            prevGrounded = grounded;
            prevVy = motor.Velocity.y;
        }
        Spring(ref landY, ref landYVel, 0f, landOmega, landZeta, dt);

        // DASH: pulled back along the dash, thrown forward past rest
        Spring(ref dashOff, ref dashVel, Vector3.zero, Omega(c != null ? c.viewmodelDashFreq : 4.5f), c != null ? Mathf.Clamp(c.viewmodelDashDamping, 0.05f, 2f) : 0.38f, dt);

        // SLIDE, PUMP, WALL weights
        Spring(ref slideW, ref slideWVel, sliding ? 1f : 0f, blend, 1f, dt);
        float pumpStart = run * (c != null ? c.viewmodelPumpOverRun : 1.05f), pumpFull = c != null ? Mathf.Max(pumpStart + 0.1f, c.viewmodelPumpFullSpeed) : 18f;
        float pumpTarget = grounded && !sliding ? Mathf.InverseLerp(pumpStart, pumpFull, hSpeed) : 0f;
        Spring(ref pumpW, ref pumpWVel, pumpTarget, blend, 1f, dt);
        bool wallRun = motor != null && motor.WallRunning;
        if (wallRun) ProbeWall();
        bool plant = wallRun && hands[(int)wallHand].free;
        Spring(ref wallW, ref wallWVel, plant ? 1f : 0f, blend * 0.8f, 1f, dt);

        // HIT SHUDDER: a rattle that dies away
        shudderT += dt;
        if (shudderAmp > 0.001f) shudderAmp *= Mathf.Exp(-dt / (c != null ? Mathf.Max(0.02f, c.viewmodelShudderDecay) : 0.13f));
        else shudderAmp = 0f;
        float sf = (c != null ? c.viewmodelShudderFreq : 26f) * 2f * Mathf.PI;
        Vector3 shudder = shudderAmp > 0f
            ? new Vector3(Mathf.Sin(shudderT * sf), Mathf.Sin(shudderT * sf * 0.77f + 1.3f), 0.5f * Mathf.Sin(shudderT * sf * 1.21f)) * (shudderAmp * (c != null ? c.viewmodelShudderAmp : 0.012f))
            : Vector3.zero;

        // TACKLE: the shoulder's own spring
        Spring(ref tackleOff, ref tackleVel, Vector3.zero, Omega(c != null ? c.viewmodelTackleFreq : 5f), c != null ? Mathf.Clamp(c.viewmodelTackleDamping, 0.05f, 2f) : 0.5f, dt);

        // THE TUCK: both hands in to the chest and turned up through a flip or the landing roll; the root takes a share (UpdateRoot)
        float tuckElapsed = Time.time - (tuckUntil - tuckSeconds);
        bool tucked = tuckSeconds > 0f && tuckElapsed >= 0f && tuckElapsed < tuckSeconds * 0.8f;
        Spring(ref tuckW, ref tuckWVel, tucked ? 1f : 0f, blend, 1f, dt);
        Vector3 tuckPos = (c != null ? c.viewmodelTuckPos : new Vector3(0f, -0.05f, -0.1f)) * tuckW;
        Vector3 tuckRot = new Vector3(-(c != null ? c.viewmodelTuckPitch : 30f), 0f, 0f) * tuckW;

        sharedPos = swayPos + bobPos + breath + new Vector3(0f, landY - (c != null ? c.viewmodelSlideDrop : 0.06f) * slideW, 0f) + dashOff + shudder + tuckPos;
        sharedRot = swayRot + bobR + breathR + new Vector3(-landY * 60f, 0f, 0f) + tuckRot;

        UpdateRoot(dt, now);
    }

    /// THE ROOT: the torso's continuous target from the locomotion (sprint, landing, slide, dash, the look, the idle
    /// weight shift) plus any held lean, sprung with overshoot; the impulses (Lean) ride the same spring. Written to
    /// the rig's root so the shoulders, both arms and every weapon under it go together
    void UpdateRoot(float dt, float now)
    {
        var c = config;
        Vector3 tp = Vector3.zero, tr = Vector3.zero;
        // SPRINT: over the run speed the body drops, tips forward and comes in
        tp.y -= (c != null ? c.viewmodelRootSprintDrop : 0.035f) * pumpW;
        tp.z += (c != null ? c.viewmodelRootSprintPush : 0.02f) * pumpW;
        tr.x += (c != null ? c.viewmodelRootSprintPitch : 2.5f) * pumpW;
        // LAND: a share of the hands' dip, and the compression tips the body forward over it
        tp.y += landY * (c != null ? c.viewmodelRootLandShare : 0.5f);
        tr.x -= landY * (c != null ? c.viewmodelRootLandPitch : 25f);
        // SLIDE: down, rolled, tipped
        tp.y -= (c != null ? c.viewmodelRootSlideDrop : 0.04f) * slideW;
        tr.z -= (c != null ? c.viewmodelRootSlideRoll : 3.5f) * slideW;
        tr.x += (c != null ? c.viewmodelRootSlidePitch : 2f) * slideW;
        // DASH: a share of the pull-and-throw, turned into it
        float dashShare = c != null ? c.viewmodelRootDashShare : 0.5f, dashRot = c != null ? c.viewmodelRootDashRot : 30f;
        tp += dashOff * dashShare;
        tr += new Vector3(dashOff.y, dashOff.x, -dashOff.x) * dashRot;
        // THE TUCK: the torso curls with the arms through a flip or the roll
        tr.x += (c != null ? c.viewmodelTuckRootPitch : 10f) * tuckW;
        tp.y -= (c != null ? c.viewmodelTuckRootDrop : 0.03f) * tuckW;
        // THE LOOK: the body rolls into a turn and lags the nod a touch
        tr.z -= yawRate * (c != null ? c.viewmodelRootLookRoll : 0.012f);
        tr.x -= pitchRate * (c != null ? c.viewmodelRootLookPitch : 0.006f);
        // IDLE: the slow shift of weight from foot to foot
        float shiftPh = now * (c != null ? c.viewmodelRootShiftRate : 0.11f) * 2f * Mathf.PI;
        tp.x += Mathf.Sin(shiftPh) * (c != null ? c.viewmodelRootShiftAmp : 0.006f);
        tp.y -= Mathf.Abs(Mathf.Cos(shiftPh)) * (c != null ? c.viewmodelRootShiftAmp : 0.006f) * 0.4f;
        tr.z += Mathf.Sin(shiftPh + 0.6f) * (c != null ? c.viewmodelRootShiftRoll : 0.5f);
        // The quietest hand's scale damps the locomotion; a held lean is wanted in full
        tp *= rootScale; tr *= rootScale;
        if (rootHoldFrame == Time.frameCount) { tp += rootHoldPos; tr += rootHoldRot; }

        float omega = Omega(c != null ? c.viewmodelRootFreq : 4f);
        float zeta = c != null ? Mathf.Clamp(c.viewmodelRootDamping, 0.05f, 2f) : 0.55f;
        Spring(ref rootPos, ref rootPosVel, tp, omega, zeta, dt);
        Spring(ref rootRot, ref rootRotVel, tr, omega, zeta, dt);
        rootPos = Vector3.ClampMagnitude(rootPos, 0.14f);
        rootRot = Vector3.ClampMagnitude(rootRot, 14f);
        Root.localPosition = rootPos;
        Root.localRotation = Quaternion.Euler(rootRot);
    }

    /// The wall beside you, by a ray to the motor's side: the plant pose in camera space (a fallback beside the frame if the ray finds nothing)
    void ProbeWall()
    {
        float side = motor.WallRunSide >= 0f ? 1f : -1f;
        wallHand = side < 0f ? HandSide.Left : HandSide.Right;
        Vector3 along = motor.Velocity; along.y = 0f;
        if (along.sqrMagnitude < 0.01f) { along = transform.forward; along.y = 0f; }
        along.Normalize();
        Vector3 toWall = Vector3.Cross(Vector3.up, along) * side;
        float reach = config != null ? Mathf.Max(0.3f, config.viewmodelWallReach) : 1.2f;
        if (Physics.Raycast(transform.position, toWall, out RaycastHit hit, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            // In the ROOT's space (the root leans off the camera): the hand lands on the wall where the wall is
            Vector3 fingers = Vector3.ProjectOnPlane(Vector3.Slerp(along, Vector3.up, 0.45f), hit.normal);
            if (fingers.sqrMagnitude < 1e-4f) fingers = Vector3.ProjectOnPlane(Vector3.up, hit.normal);
            Quaternion rot = Quaternion.Inverse(Root.rotation) * Quaternion.LookRotation(fingers.normalized, hit.normal);
            wallPose = new ArmPose(Root.InverseTransformPoint(hit.point + hit.normal * 0.014f), rot);
        }
        else
        {
            Vector3 up = new Vector3(0f, 0.5f, 0.85f).normalized;
            wallPose = new ArmPose(new Vector3(side * 0.42f, -0.1f, 0.32f), Quaternion.LookRotation(up, new Vector3(-side, 0f, 0f)));
        }
    }

    /// The shoulders take a share of the shared offset; the right one the tackle
    Vector3 ShoulderOffset(Hand h)
    {
        float follow = config != null ? Mathf.Clamp01(config.viewmodelShoulderFollow) : 0.35f;
        Vector3 o = sharedPos * follow;
        if (h.side == HandSide.Right) o += tackleOff;
        return o;
    }

    void ApplyShared(ref ArmPose t, float scale)
    {
        t.pos += sharedPos * scale;
        t.rot = Quaternion.Euler(sharedRot * scale) * t.rot;
    }

    /// The per-hand layers: the pump, the slide reach, the wall plant
    void PerHand(Hand h, ref ArmPose t, float scale, ref Fingers fingers)
    {
        var c = config;
        float sign = h.side == HandSide.Right ? 1f : -1f;
        bool planted = wallW > 0.001f && h.side == wallHand && h.free;
        float pumpAmp = (c != null ? c.viewmodelPumpAmp : 0.028f) * pumpW * scale;
        if (wallW > 0.001f && !planted) pumpAmp += (c != null ? c.viewmodelWallPump : 0.05f) * wallW * scale;
        if (pumpAmp > 0f)
        {
            t.pos.z += sign * Mathf.Sin(stridePhase) * pumpAmp;
            t.pos.y += Mathf.Abs(Mathf.Cos(stridePhase)) * pumpAmp * 0.35f;
        }
        if (h.free && slideW > 0.001f)
        {
            Vector3 p = c != null ? c.viewmodelSlideReach : new Vector3(0.3f, -0.46f, 0.34f);
            p.x *= sign;
            var reach = new ArmPose(p, Quaternion.Euler(52f, -sign * 12f, -sign * 8f));
            t = ArmPose.Lerp(t, reach, slideW);
            if (slideW > 0.5f) fingers = Fingers.Open;
        }
        if (planted)
        {
            t = ArmPose.Lerp(t, wallPose, wallW);
            if (wallW > 0.5f) fingers = Fingers.Open;
        }
    }
}
