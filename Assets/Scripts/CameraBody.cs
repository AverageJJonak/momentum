using UnityEngine;

/// <summary>
/// THE BODY CAMERA (2026-10-23; the user: "I do want the camera following where the player would look though, like
/// Mirror's Edge's dynamic camera"). The mouse owns the look; this lays a BODY over it — every layer a damped spring,
/// every amplitude a camera* field (MovementConfigCamera.cs), all of it scaled by the Settings' CAMERA MOTION knob
/// (Motion, 0..1) so it can be turned down to nothing. PlayerLook calls Tick each LateUpdate and composes Rotation,
/// Offset and Fov into its one `view`: pitch/yaw/roll are added to the look's Euler, the offset to the eye.
///  · STRIDE a figure-eight head bob only at speed on the ground, LANDINGS a dip and a nod by the impact, THE ROLL a
///    forward tumble on a hard landing (a fall ≥ cameraRollFall, any slam, a trick's stumble), WALL RUN a yaw toward
///    the run with the bob tilted on the wall, SLIDE a nod and a roll into the turn, DASH a push, a FOV punch and a
///    roll into a lateral dash, AIR a drift down as the fall grows and a float at the apex, LOOK-AHEAD a yaw toward
///    the velocity and a nod toward the landing point (blended out whenever the mouse moves, never in a trick),
///    SPEED LEAN a nod into speed, a roll into strafes and turns. The layers are in CameraBodyLayers.cs.
///  · The limits: pitch and roll clamped (cameraBodyPitchMax / RollMax) with only the roll and the tricks on top,
///    offsets clamped (cameraBodyOffsetMax); the impulse springs are critically damped, so nothing snaps.
/// The arms are children of the camera: they ride the body with it, and the ViewmodelCamera takes its usual fraction
/// of the FOV punch. Sign language is Unity's: +pitch looks down, +roll leans left. Nothing read in Awake/OnEnable.
/// </summary>
public partial class CameraBody : MonoBehaviour
{
    public static CameraBody Instance { get; private set; }
    public MovementConfig config;
    public PlayerMotor motor;
    public PlayerLook look;

    const string MotionKey = "set_cam_motion";
    const float MotionDefault = 0.7f;
    static float motion = -1f;
    /// The Settings' CAMERA MOTION knob, 0..1: every layer is scaled by it; 0 = the mouse look alone
    public static float Motion
    {
        get { if (motion < 0f) motion = Mathf.Clamp01(PlayerPrefs.GetFloat(MotionKey, MotionDefault)); return motion; }
    }
    public static void SetMotion(float v) { motion = Mathf.Clamp01(v); PlayerPrefs.SetFloat(MotionKey, motion); }

    /// This frame's body over the look: degrees (x pitch, + = down; y yaw; z roll, + = a lean left)
    public Vector3 Rotation { get; private set; }
    /// ... the eye's offset, world metres
    public Vector3 Offset { get; private set; }
    /// ... and the extra field of view, degrees (the dash's and the tricks' punch)
    public float Fov { get; private set; }
    /// THE ROLL: 0..1 through a hard landing's tumble, 0 outside one
    public float Roll01 => rollSeconds > 0f && Time.time - rollStart < rollSeconds ? (Time.time - rollStart) / rollSeconds : 0f;
    public bool Rolling => Roll01 > 0f;

    const float E = 2.71828f;   // an impulse v0 on a critically damped spring peaks at v0 / (ω e): v0 = peak × ω × e makes the peak the amount asked

    // The continuous layers' spring (its target is rebuilt every frame by the layers)
    Vector3 bodyRot, bodyRotVel, bodyPos, bodyPosVel;
    Vector3 targetRot, targetPos;           // written by the layers: degrees, and metres in the yaw frame (x right, y up, z forward)
    // The impulses: landings, the double jump's and the springboard's lifts (one vertical spring, one pitch spring)
    float dipY, dipYVel, dipPitch, dipPitchVel;
    // The dash: a push along it (world) and a roll into a lateral one
    Vector3 dashOff, dashOffVel;
    float dashRoll, dashRollVel;
    // The look-ahead's own slow springs
    float aheadYaw, aheadYawVel, aheadPitch, aheadPitchVel, aheadYawTarget, aheadPitchTarget;
    // The stride
    float stridePhase, strideW, strideWVel, strideRoll;
    Vector3 strideBob;
    float slideW, slideWVel;
    // THE ROLL
    float rollStart = -10f, rollSeconds;
    // The FOV punch (the dash's shape: in, then back)
    float fovAt = -10f, fovAmount, fovIn = 0.1f, fovOut = 0.25f;
    // The mouse's veto on the look-ahead
    float mouseQuietUntil = -10f;
    // Rates
    float prevYaw, yawRate, prevHeading, headingRate;
    bool yawInit, headingInit;

    void OnEnable() { Instance = this; MovementEvents.MoveExecuted += OnMove; }
    void OnDisable() { if (Instance == this) Instance = null; MovementEvents.MoveExecuted -= OnMove; }

    /// A FOV punch with the dash's shape: up over cameraDashFovIn, back over cameraDashFovOut (the tricks use it too)
    public void Punch(float degrees)
    {
        if (config == null) return;
        fovAt = Time.time;
        fovAmount = Mathf.Max(FovPunch(Time.time), degrees);
        fovIn = Mathf.Max(0.02f, config.cameraDashFovIn);
        fovOut = Mathf.Max(0.02f, config.cameraDashFovOut);
    }

    /// THE ROLL: the forward tumble of a hard landing (the trick's stumble asks for it too); the arms tuck with it
    public void StartRoll()
    {
        if (config == null) return;
        rollStart = Time.time;
        rollSeconds = Mathf.Max(0.05f, config.cameraRollSeconds);
        if (ArmRig.Instance != null) ArmRig.Instance.Tuck(rollSeconds);
    }

    float FovPunch(float now)
    {
        float t = now - fovAt;
        if (t < 0f || fovAmount <= 0f) return 0f;
        if (t < fovIn) return fovAmount * Mathf.SmoothStep(0f, 1f, t / fovIn);
        return fovAmount * (1f - Mathf.SmoothStep(0f, 1f, (t - fovIn) / fovOut));
    }

    void OnMove(MoveType type, string detail)
    {
        if (config == null) return;
        float wi = Omega(config.cameraImpulseFreq) * E;
        switch (type)
        {
            case MoveType.Land:
            {
                float fall = 0f;
                float.TryParse(detail, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fall);
                LandDip(fall);
                if (fall >= config.cameraRollFall) StartRoll();
                break;
            }
            case MoveType.SlamLand:
                LandDip(config.cameraLandFallMax);
                StartRoll();
                break;
            case MoveType.Dash:
            {
                Vector3 d = motor != null && motor.LastDashDir.sqrMagnitude > 1e-4f ? motor.LastDashDir.normalized : Vector3.forward;
                dashOffVel += d * (config.cameraDashPush * wi);
                Punch(config.cameraDashFov);
                float side = look != null ? Vector3.Dot(d, Quaternion.Euler(0f, look.Yaw, 0f) * Vector3.right) : 0f;
                dashRollVel -= side * config.cameraDashRoll * wi;   // a lean INTO a dash to the right is a roll to the right (negative)
                break;
            }
            case MoveType.DoubleJump:
                dipYVel += config.cameraDoubleJumpLift * wi;
                dipPitchVel -= 1f * wi;
                break;
            case MoveType.Kick:
                // THE SPRINGBOARD: the boot's own dip is FistRig's pitch kick on PlayerLook; the lift off the body is this
                if (detail != null && detail.StartsWith("SPRINGBOARD")) { dipYVel += config.cameraSpringboardLift * wi; dipPitchVel -= 1.5f * wi; }
                break;
        }
    }

    /// The landing's dip and nod, sized by the fall: nothing for a step, the small one at cameraLandFallMin, the big one at cameraLandFallMax
    void LandDip(float fall)
    {
        float k = Mathf.InverseLerp(config.cameraLandFallMin, config.cameraLandFallMax, fall);
        float small = Mathf.Clamp01(fall / Mathf.Max(0.1f, config.cameraLandFallMin));
        float dip = Mathf.Lerp(config.cameraLandDipMin, config.cameraLandDipMax, k) * small;
        float nod = Mathf.Lerp(config.cameraLandPitchMin, config.cameraLandPitchMax, k) * small;
        float w = DipOmega() * E;
        dipYVel -= dip * w;
        dipPitchVel += nod * w;
    }

    float DipOmega() => 4.7f / Mathf.Max(0.05f, config.cameraLandRecover);   // a critically damped spring is settled (~1 %) at 4.7/ω

    /// THE ROLL's pitch at `u` (0..1): down to cameraRollDown, back through zero to a small lean back, home; `drop` is the crouch through it
    float RollCurve(float u, out float drop)
    {
        drop = config.cameraRollDrop * Bump(u);
        return config.cameraRollDown * Bump(u / 0.7f) - config.cameraRollBack * Bump((u - 0.55f) / 0.45f);
    }

    static float Bump(float x) => x > 0f && x < 1f ? Mathf.Sin(Mathf.PI * x) : 0f;

    /// Every frame from PlayerLook.LateUpdate, before it composes the view. `mouseDelta` is the raw mouse travel this frame
    float speedFov;

    /// THE SPEED LENS (2026-10-27, the user's note: "dynamic FOV shifts to emphasize speed without causing motion sickness"):
    /// the view widens with the run, nothing below cameraSpeedFovFrom, all of cameraSpeedFov at cameraSpeedFovAt, eased so
    /// it never flickers; scaled by Motion like every layer
    float SpeedFov(MovementConfig c, float dt)
    {
        Vector3 v = motor.Velocity;
        float h = new Vector2(v.x, v.z).magnitude;
        float target = c.cameraSpeedFov * Mathf.Clamp01((h - c.cameraSpeedFovFrom) / Mathf.Max(0.1f, c.cameraSpeedFovAt - c.cameraSpeedFovFrom));
        speedFov = Mathf.Lerp(speedFov, target, 1f - Mathf.Exp(-Mathf.Max(0.1f, c.cameraSpeedFovSmoothing) * dt));
        return speedFov;
    }

    public void Tick(float dt, float mouseDelta)
    {
        var c = config;
        if (c == null || motor == null || look == null) { Rotation = Vector3.zero; Offset = Vector3.zero; Fov = 0f; return; }
        dt = Mathf.Clamp(dt, 0f, 0.05f);
        float now = Time.time;
        if (mouseDelta > 0.01f) mouseQuietUntil = now + c.cameraLookAheadQuiet;

        targetRot = Vector3.zero;
        targetPos = Vector3.zero;
        Layers(dt, now);

        float wBody = Omega(c.cameraBodyFreq), zBody = Mathf.Clamp(c.cameraBodyDamping, 0.1f, 2f);
        Spring(ref bodyRot, ref bodyRotVel, targetRot, wBody, zBody, dt);
        Spring(ref bodyPos, ref bodyPosVel, targetPos, wBody, zBody, dt);
        float wDip = DipOmega();
        Spring(ref dipY, ref dipYVel, 0f, wDip, 1f, dt);
        Spring(ref dipPitch, ref dipPitchVel, 0f, wDip, 1f, dt);
        float wImp = Omega(c.cameraImpulseFreq);
        Spring(ref dashOff, ref dashOffVel, Vector3.zero, wImp, 1f, dt);
        Spring(ref dashRoll, ref dashRollVel, 0f, wImp, 1f, dt);
        float wAhead = Omega(c.cameraLookAheadFreq);
        Spring(ref aheadYaw, ref aheadYawVel, aheadYawTarget, wAhead, 1f, dt);
        Spring(ref aheadPitch, ref aheadPitchVel, aheadPitchTarget, wAhead, 1f, dt);

        // Compose, clamp, scale. The roll sits on top of the pitch limit: it is the one big move, and it is scripted
        float pitch = Mathf.Clamp(bodyRot.x + dipPitch + aheadPitch, -c.cameraBodyPitchMax, c.cameraBodyPitchMax);
        float yaw = bodyRot.y + aheadYaw;
        float roll = Mathf.Clamp(bodyRot.z + dashRoll + strideRoll, -c.cameraBodyRollMax, c.cameraBodyRollMax);
        float rollDrop = 0f;
        float rollPitch = Rolling ? RollCurve(Roll01, out rollDrop) : 0f;
        Vector3 local = Vector3.ClampMagnitude(bodyPos + strideBob + new Vector3(0f, dipY - rollDrop, 0f), c.cameraBodyOffsetMax);
        Vector3 world = Quaternion.Euler(0f, look.Yaw, 0f) * local + Vector3.ClampMagnitude(dashOff, c.cameraBodyOffsetMax);
        float k = Motion;
        Rotation = new Vector3(pitch + rollPitch, yaw, roll) * k;
        Offset = world * k;
        Fov = (FovPunch(now) + SpeedFov(c, dt)) * k;
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

    static float Omega(float hz) => 2f * Mathf.PI * Mathf.Max(0.1f, hz);
}
