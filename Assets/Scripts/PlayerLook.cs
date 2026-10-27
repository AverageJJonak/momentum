using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Mouse look + camera driver. The motor simulates at a fixed tick; the camera
/// interpolates between the motor's last two fixed positions every render frame (spec §6).
/// Cursor handling is editor-proof: the editor drops the lock on Esc or when
/// clicking outside the Game view, so any gameplay input instantly re-locks.
/// </summary>
public class PlayerLook : MonoBehaviour
{
    public MovementConfig config;
    public PlayerMotor motor;
    public Camera cam;
    /// THE BODY CAMERA (2026-10-23): ticked here and composed into `view` — its pitch/yaw/roll over the look, its offset on the eye, its FOV punch
    public CameraBody body;

    public float Yaw { get; private set; }
    public float Pitch { get; private set; }
    /// The raw mouse travel this frame (pixels, before sensitivity): the body's look-ahead yields to it
    public float LastMouseDelta { get; private set; }

    bool wantLock = true;
    float fovKick;
    float speedFov;
    float rollKick;
    float wallRoll; // the lean off a wall run, eased in and out
    float yawKick;
    float pitchKick; // the blunderbuss's muzzle climb: a punch up that springs back, never written into Pitch
    float pushKick;  // a shove of the whole view along its forward (metres), decaying: the camera follows a swing a step
    float shake;

    /// Quick additive FOV punch that springs back — corner boosts, dashes, parries
    public void AddFovKick(float amount) => fovKick = Mathf.Min(fovKick + amount, 25f);

    /// Signed roll punch (degrees) that springs back — the view leans into each sword cut
    public void AddRollKick(float degrees) => rollKick = Mathf.Clamp(rollKick + degrees, -8f, 8f);

    /// Signed yaw punch (degrees) that springs back — the body turns with a full swing (aim is untouched)
    public void AddYawKick(float degrees) => yawKick = Mathf.Clamp(yawKick + degrees, -10f, 10f);

    /// Signed pitch punch (degrees, positive = UP) that springs back — the blunderbuss's muzzle climb. Like the roll
    /// and yaw kicks it is laid over the view and decays; Pitch itself is never touched, so the aim is where you left it
    public void AddPitchKick(float degrees) => pitchKick = Mathf.Clamp(pitchKick + degrees, -10f, 10f);

    /// Signed push (metres, positive = FORWARD along the view) that decays — the camera takes a step into a swing and
    /// is shoved back by a shot. Small by its clamp: the aim's origin moves a few centimetres, never the aim itself
    public void AddPushKick(float metres) => pushKick = Mathf.Clamp(pushKick + metres, -0.08f, 0.08f);

    /// Camera shake (0..1.5): impacts rattle the view, decaying in real time so a
    /// freeze frame still trembles
    public void AddShake(float amount) => shake = Mathf.Min(shake + amount, 1.5f);

    // The cutscenes' hand on your head: a pull toward a point in the world, on top of the mouse, gone when its time is up
    Vector3 steerPoint;
    float steerStrength;
    float steerUntil = float.NegativeInfinity;

    /// Pull the view toward `worldPoint` for `seconds` of real time. Additive: the mouse still moves the view every
    /// frame and the pull only closes what is left, so you can always look away. `strength` 0..1 is how hard — at 1
    /// most of the angle goes inside half a second, at 0.3 the view drifts there over a couple of seconds. Call it
    /// again to retarget or extend; the pull stops on its own when the time is up, or at ClearSteer
    public void Steer(Vector3 worldPoint, float strength, float seconds)
    {
        steerPoint = worldPoint;
        steerStrength = Mathf.Clamp01(strength);
        steerUntil = Time.unscaledTime + Mathf.Max(0f, seconds);
    }

    public void ClearSteer() => steerUntil = float.NegativeInfinity;

    void ApplySteer()
    {
        Vector3 dir = steerPoint - cam.transform.position;
        if (dir.sqrMagnitude < 1e-4f) return;
        dir.Normalize();
        float wantYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        float wantPitch = Mathf.Clamp(-Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg, -89f, 89f);
        float k = 1f - Mathf.Exp(-6f * steerStrength * Time.unscaledDeltaTime);
        Yaw = Mathf.Repeat(Yaw + Mathf.DeltaAngle(Yaw, wantYaw) * k, 360f);
        Pitch = Mathf.Clamp(Pitch + (wantPitch - Pitch) * k, -89f, 89f);
    }

    void Start()
    {
        SetCursorLock(true);
        if (cam != null) cam.fieldOfView = GameSettings.Fov(config);
    }

    void Update()
    {
        if (PauseMenu.IsPaused) return; // pause owns the cursor (Esc opens it)

        var kb = Keyboard.current;
        var mouse = Mouse.current;

        // Re-capture on any gameplay input — click, move, or jump — so a dropped
        // lock (clicking another window/panel) never strands the camera
        bool gameplayInput =
            (mouse != null && mouse.leftButton.wasPressedThisFrame) ||
            Keybinds.Down(GameAction.Forward) || Keybinds.Down(GameAction.Back) ||
            Keybinds.Down(GameAction.Left) || Keybinds.Down(GameAction.Right) ||
            Keybinds.Down(GameAction.Jump);
        if (gameplayInput && Cursor.lockState != CursorLockMode.Locked)
            SetCursorLock(true);

        LastMouseDelta = 0f;
        if (Cursor.lockState == CursorLockMode.Locked && mouse != null)
        {
            Vector2 raw = mouse.delta.ReadValue();
            LastMouseDelta = raw.magnitude;
            Vector2 delta = raw * GameSettings.Sensitivity(config);
            Yaw = Mathf.Repeat(Yaw + delta.x, 360f);
            Pitch = Mathf.Clamp(Pitch - delta.y, -89f, 89f);
        }
        if (Time.unscaledTime < steerUntil && cam != null) ApplySteer(); // after the mouse: the pull closes what the mouse left
    }

    void OnApplicationFocus(bool focused)
    {
        if (focused && wantLock && !PauseMenu.IsPaused) SetCursorLock(true);
    }

    void LateUpdate()
    {
        if (motor == null || cam == null) return;
        float alpha = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
        Vector3 feet = Vector3.Lerp(motor.PrevPosition, motor.CurrPosition, alpha);
        float ut = Time.unscaledTime;
        float sx = (Mathf.PerlinNoise(ut * 23f, 0.13f) - 0.5f) * 4.8f * shake;
        float sy = (Mathf.PerlinNoise(ut * 21f, 0.61f) - 0.5f) * 4.8f * shake;
        float sz = (Mathf.PerlinNoise(ut * 27f, 0.89f) - 0.5f) * 3.2f * shake;
        // THE BODY: ticked here so it always sees this frame's look; laid over the look's Euler, its offset on the eye
        Vector3 bodyRot = Vector3.zero, bodyOff = Vector3.zero;
        float bodyFov = 0f;
        if (body != null)
        {
            body.Tick(Time.deltaTime, LastMouseDelta);
            bodyRot = body.Rotation;
            bodyOff = body.Offset;
            bodyFov = body.Fov;
        }
        Quaternion view = Quaternion.Euler(Pitch + sx - pitchKick + bodyRot.x, Yaw + sy + yawKick + bodyRot.y, rollKick + sz + wallRoll + bodyRot.z);
        // THE TRICK: a flip's rotation on top of everything, about the eye; the look underneath is untouched, so a flip lands where you aimed
        var trick = TrickSystem.Instance;
        if (trick != null && trick.Active) view = trick.Apply(view);
        cam.transform.SetPositionAndRotation(
            feet + Vector3.up * motor.EyeHeight + view * Vector3.forward * pushKick + bodyOff,
            view);
        shake = Mathf.Lerp(shake, 0f, 1f - Mathf.Exp(-9f * Time.unscaledDeltaTime));

        fovKick = Mathf.MoveTowards(fovKick, 0f, 60f * Time.deltaTime);
        rollKick = Mathf.Lerp(rollKick, 0f, 1f - Mathf.Exp(-16f * Time.deltaTime));
        pitchKick = Mathf.Lerp(pitchKick, 0f, 1f - Mathf.Exp(-12f * Time.deltaTime));
        pushKick = Mathf.Lerp(pushKick, 0f, 1f - Mathf.Exp(-14f * Time.deltaTime));
        float wallTarget = motor != null && motor.WallRunning && config != null ? motor.WallRunSide * config.wallRunCameraRoll : 0f;
        wallRoll = Mathf.Lerp(wallRoll, wallTarget, 1f - Mathf.Exp(-10f * Time.deltaTime));
        yawKick = Mathf.Lerp(yawKick, 0f, 1f - Mathf.Exp(-10f * Time.deltaTime));

        // Speed stretches the FOV, smoothly — the world widens as you get fast
        float hSpeed = new Vector2(motor.Velocity.x, motor.Velocity.z).magnitude;
        float targetSpeedFov = config.speedFovMaxBoost
            * Mathf.InverseLerp(config.speedFovStartSpeed, config.speedFovFullSpeed, hSpeed);
        speedFov = Mathf.Lerp(speedFov, targetSpeedFov, 1f - Mathf.Exp(-4f * Time.deltaTime));

        cam.fieldOfView = GameSettings.Fov(config) + fovKick + speedFov + bodyFov;   // the body's punch (a dash, a trick) rides the same lens; the viewmodel takes its fraction of it
    }

    void SetCursorLock(bool locked)
    {
        wantLock = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
