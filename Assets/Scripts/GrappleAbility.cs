using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// THE WHIP (2026-10-07: the grapple dagger re-dressed as a leather bullwhip — a tapered dark line from the hand to
/// a knotted cracker; the crack is leather and belt; the yank and the swing are exactly what they were). Everyone's
/// movement tool: it needs nothing else in the hand.
/// Spider-Man split (spec §3.7):
///  • SURFACE / drone → SWING. Instant attach, then a rope-length pendulum:
///    gravity builds speed through the arc, hold E to reel in for height,
///    release E (or jump/dash off) to launch with all your momentum.
///  • ENEMY          → YANK. Instant accelerating pull toward the enemy; ride
///    it in and CUT on arrival — the arrival itself hurts nothing, but a kill
///    during the pull or just after it is the full-refill yank kill.
/// Costs 2 pips (refunded on a total whiff). Enemies are magnetized on the aim.
/// </summary>
public class GrappleAbility : MonoBehaviour
{
    public MovementConfig config;
    public PlayerMotor motor;
    public PlayerLook look;
    public StaminaSystem stamina;

    public bool IsPulling => state == State.Yank;
    public bool IsSwinging => state == State.Swing;
    public bool IsActive => state == State.Yank || state == State.Swing;
    /// 0 = the whip was just let go (its cooldown), 1 = a press takes hold (the HUD's lamp)
    public float Readiness => state == State.Cooldown && config != null && config.grappleCooldown > 0f
        ? Mathf.Clamp01(1f - (cooldownUntil - Time.time) / config.grappleCooldown) : state == State.Ready ? 1f : 0f;

    /// Spec §3.3: a kill during the pull or just after counts as a yank kill
    public bool KillCountsAsYank
        => IsPulling || Time.time - lastPullTickTime <= config.grappleYankKillWindow;

    enum State { Ready, Yank, Swing, Cooldown }

    State state = State.Ready;
    Vector3 stickPoint;
    Enemy stuckEnemy;
    float pullSpeed;
    float ropeLength;
    float attachTime;
    float cooldownUntil;
    float lastPullDistance;
    float lastPullTickTime = float.NegativeInfinity;
    float stallTime;
    bool pressed;   // E went down this frame (attach edge)
    bool held;      // E held (keeps a swing going)
    GameObject daggerVisual;   // the cracker: the knotted end of the whip (the field keeps its old name)
    LineRenderer line;         // the whip itself: a tapered dark leather line, sagging as it flies out, taut once it has hold
    const float StallReleaseSeconds = 0.35f;
    const int WhipPoints = 12;
    readonly Vector3[] whipPts = new Vector3[WhipPoints];

    void Start()
    {
        // THE CRACKER: a knot of dark leather and the short fall of the tassel behind it
        daggerVisual = new GameObject("GrappleDagger");
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        var leather = new Material(lit);
        leather.SetColor("_BaseColor", new Color(0.2f, 0.11f, 0.06f));
        leather.SetFloat("_Metallic", 0f);
        leather.SetFloat("_Smoothness", 0.45f);
        var knot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(knot.GetComponent<Collider>());
        knot.transform.SetParent(daggerVisual.transform, false);
        knot.transform.localScale = Vector3.one * 0.09f;
        knot.GetComponent<MeshRenderer>().sharedMaterial = leather;
        var tassel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(tassel.GetComponent<Collider>());
        tassel.transform.SetParent(daggerVisual.transform, false);
        tassel.transform.localPosition = new Vector3(0f, -0.07f, 0f);
        tassel.transform.localScale = new Vector3(0.016f, 0.12f, 0.016f);
        tassel.GetComponent<MeshRenderer>().sharedMaterial = leather;
        daggerVisual.SetActive(false);

        line = gameObject.AddComponent<LineRenderer>();
        line.startWidth = 0.05f;   // thick at the hand
        line.endWidth = 0.012f;    // thin at the cracker
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetColor("_BaseColor", new Color(0.19f, 0.1f, 0.055f)); // dark plaited leather
        line.material = mat;
        line.positionCount = WhipPoints;
        line.numCapVertices = 2;
        line.enabled = false;
    }

    void Update()
    {
        bool locked = PauseMenu.IsPaused;
        bool mid = TrickSystem.Instance != null && TrickSystem.Instance.Active;   // mid-flip the hands are tucked: the whip waits for the landing
        if (!locked && !mid && Keybinds.Down(GameAction.Grapple)) pressed = true;
        held = !locked && !mid && Keybinds.Held(GameAction.Grapple);

        bool show = IsActive;
        daggerVisual.SetActive(show);
        line.enabled = show;
        if (show)
        {
            // The whip flies out over 0.1 s from the right hand — presentation only, physics is already live — with a
            // sag in it that straightens as it takes the weight; the cracker rides the end
            float zip = Mathf.Clamp01((Time.unscaledTime - attachTime) / 0.1f);
            Quaternion view = look != null ? Quaternion.Euler(look.Pitch, look.Yaw, 0f) : transform.rotation;
            // The whip starts in the right hand's grip socket (ArmRig), at the world point the world's camera draws where
            // the hands' camera draws the socket: the line meets the hand on screen whatever the two FOVs are
            Vector3 hand = ArmRig.Instance != null && ArmRig.Instance.Built && look != null && look.cam != null
                ? ArmRig.Instance.GripLineAnchor(ArmRig.HandSide.Right, look.cam)
                : EyePosition() + view * new Vector3(0.3f, -0.24f, 0.25f);
            Vector3 target = Vector3.Lerp(hand, CurrentStickPoint(), zip);
            float sag = (1f - zip) * 0.9f + 0.08f;
            Vector3 across = Vector3.Cross((target - hand).normalized, Vector3.up) * 0.25f;
            for (int i = 0; i < WhipPoints; i++)
            {
                float t = i / (float)(WhipPoints - 1);
                float bow = Mathf.Sin(t * Mathf.PI);
                whipPts[i] = Vector3.Lerp(hand, target, t) + Vector3.down * (sag * bow) + across * (sag * bow * Mathf.Sin(t * 6.3f + Time.unscaledTime * 9f) * 0.3f);
            }
            line.SetPositions(whipPts);
            daggerVisual.transform.position = target;
            Vector3 along = target - hand;
            if (along.sqrMagnitude > 1e-4f) daggerVisual.transform.rotation = Quaternion.FromToRotation(Vector3.up, along.normalized); // the tassel trails back along the whip
        }
    }

    void FixedUpdate()
    {
        switch (state)
        {
            case State.Ready: if (pressed) TryAttach(); break;
            case State.Yank: Yank(); break;
            case State.Swing: Swing(); break;
            case State.Cooldown: if (Time.time >= cooldownUntil) state = State.Ready; break;
        }
        pressed = false;
    }

    Vector3 CurrentStickPoint()
        => stuckEnemy != null && stuckEnemy.Alive ? stuckEnemy.Center : stickPoint;

    void TryAttach()
    {
        if (stamina == null || !stamina.TrySpend(config.grappleCost)) return;

        Vector3 eye = EyePosition();
        Vector3 dir = AimDirection();
        stuckEnemy = null;

        // Enemy magnetism: sphere-sweep the aim for the nearest live enemy
        RaycastHit[] sweep = Physics.SphereCastAll(eye, config.grappleAimAssistRadius, dir,
            config.grappleMaxRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float bestDist = float.MaxValue;
        foreach (RaycastHit h in sweep)
        {
            var enemy = h.collider.GetComponentInParent<Enemy>();
            if (enemy != null && enemy.Alive && h.distance < bestDist)
            {
                bestDist = h.distance;
                stuckEnemy = enemy;
            }
        }

        if (stuckEnemy != null)
        {
            // ENEMY → YANK
            stickPoint = stuckEnemy.Center;
            // The dagger tags, it never hurts: the yank is a ride to the target, the blade is the weapon
            state = State.Yank;
            attachTime = Time.unscaledTime;
            pullSpeed = Mathf.Max(config.grappleYankStartSpeed, motor.Velocity.magnitude);
            lastPullDistance = float.PositiveInfinity;
            stallTime = 0f;
            if (GameFeel.Instance != null) GameFeel.Instance.PlayWhipCrack();
            MovementEvents.Emit(MoveType.GrappleYank);
            return;
        }

        if (Physics.Raycast(eye, dir, out RaycastHit hit, config.grappleMaxRange,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && (config.grappleAnySurface || hit.collider.GetComponent<GrappleTarget>() != null))
        {
            // SURFACE → SWING
            stickPoint = hit.point;
            state = State.Swing;
            attachTime = Time.unscaledTime;
            float dist = Vector3.Distance(eye, stickPoint);
            ropeLength = Mathf.Clamp(dist, config.grappleSwingMinLength, config.grappleSwingMaxLength);
            motor.SetSwing(stickPoint, ropeLength);
            // Tug you into the arc so a grounded grapple lifts off
            motor.AddImpulse((stickPoint - eye).normalized * config.grappleSwingInitialTug);
            var drone = hit.collider.GetComponentInParent<GrappleDrone>();
            if (drone != null) drone.Hooked();   // it takes the weight, and its eye flares
            if (GameFeel.Instance != null) GameFeel.Instance.PlayWhipCrack();
            MovementEvents.Emit(MoveType.GrappleYank);
            return;
        }

        // Total whiff: pips back, short anti-scan cooldown
        stamina.Refund(config.grappleCost);
        state = State.Cooldown;
        cooldownUntil = Time.time + 0.3f;
    }

    void Yank()
    {
        if (stuckEnemy != null && !stuckEnemy.Alive) { Retrieve(); return; }

        Vector3 toPoint = CurrentStickPoint() - EyePosition();
        float dist = toPoint.magnitude;
        if (pressed || dist <= config.grappleArriveDistance) { Retrieve(); return; }

        // Momentary snags must not drop the rope (time-based stall release)
        if (dist >= lastPullDistance - 0.01f)
        {
            stallTime += Time.fixedDeltaTime;
            if (stallTime >= StallReleaseSeconds) { Retrieve(); return; }
        }
        else stallTime = 0f;
        lastPullDistance = dist;
        lastPullTickTime = Time.time;

        pullSpeed = Mathf.Min(pullSpeed + config.grappleYankAcceleration * Time.fixedDeltaTime,
                              config.grappleYankSpeed);
        motor.SetExternalVelocity(toPoint / dist * pullSpeed);
    }

    void Swing()
    {
        // Let go on release (or press again) — momentum is preserved, so you
        // launch off the arc
        if (!held || pressed) { Retrieve(); return; }

        // Reel in while held: the rope slowly shortens toward the minimum, so
        // holding through the arc gains height and tightens the swing. Length
        // stays otherwise fixed — a free pendulum, not a direct pull.
        ropeLength = Mathf.MoveTowards(ropeLength, config.grappleSwingMinLength,
            config.grappleSwingReelSpeed * Time.fixedDeltaTime);
        motor.SetSwing(CurrentStickPoint(), ropeLength);
    }

    /// Jump/dash/slam off an active pull or swing — momentum preserved.
    public void Release()
    {
        if (IsActive) Retrieve();
    }

    /// Hard reset for respawns.
    public void Cancel()
    {
        motor.ClearSwing();
        state = State.Ready;
        stuckEnemy = null;
        pressed = false;
    }

    void Retrieve()
    {
        motor.ClearSwing();
        if (GameFeel.Instance != null) GameFeel.Instance.PlaySnap();
        EndToCooldown();
    }

    void EndToCooldown()
    {
        state = State.Cooldown;
        stuckEnemy = null;
        cooldownUntil = Time.time + config.grappleCooldown;
    }

    Vector3 EyePosition()
        => motor != null ? motor.CurrPosition + Vector3.up * motor.EyeHeight : transform.position;

    Vector3 AimDirection()
        => look != null ? Quaternion.Euler(look.Pitch, look.Yaw, 0f) * Vector3.forward : transform.forward;
}

/// Marker for grappleable surfaces once grappleAnySurface is turned off.
public class GrappleTarget : MonoBehaviour { }
