using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE STATIONMASTER (the Foreman re-dressed, 2026-10-12: EnemyForeman) — the
/// end of Act II, on the terminus platform. The Underground's own supervisor: a
/// frock-coated automaton on a railway trolley with a lantern in its chest, a
/// signal lever for one arm and a coal scuttle for the other, and WEAKSPOTS —
/// the coat panels on a ring round the body, turning, one quadrant open. Only
/// the open quadrant takes a hit: the cyan band on the coat shows through it
/// (cut here); every other side is plate, and the plate says SHIELDED. It walks
/// you down between attacks, and it fights with its hands (the mechanics,
/// phases, plates, tells and seals are the Foreman's, untouched):
///  HAMMER   — up (gold: parry the raise for a COUNTER) and down: the blow, and
///             a red ring out across the floor from where the ram lands.
///  SWEEP    — the hammer cocked back (red) and swung round the whole body at
///             knee height: jump it or slide it. Not parryable.
///  STOMP    — it rises on its skids and drops: a red ring out across the whole
///             floor. Be in the air, or up on the gantry.
///  CHARGE   — lines you up (gold body: parry near it to counter; a red lane on
///             the floor) and drives the lane out, hammer forward like a lance;
///             a wall or the lane's end STALLS it with the STACKS open (cyan).
///  POUR     — the ladle swings out over you: a red disc, then a pool of molten
///             metal that stays and burns.
///  RIVETS   — only past the leash, to bring you back: gold, parry them home.
/// Any COUNTER (or a rivet sent home) STAGGERS it: the ring swings its open side
/// to you and locks, the furnace doors open on the CORE, cyan, in reach. An
/// EXECUTE there takes a big bite and knocks one of the three plates off the
/// ring. The third plate opens OVERTIME: no ring — instead it GUARDS ITS FRONT
/// with the hammer, and the only spot is its back, where the stack lids stand
/// open; it is faster, the pour lays three pools, the winch hook joins the
/// rivets, and the FLURRY: three strikes in a row, gold, gold, red.
/// The body is an Enemy of kind Boss wearing the Foreman rig; this drives it.
/// All damage is filtered here so the bar is honest. Getting downed puts you
/// back at the door and the fight starts over.
/// </summary>
public class BossForeman : Boss
{
    public enum State { Dormant, Intro, Phase1, Transition, Phase2, Dead }
    public enum Attack { None, Hammer, Sweep, Stomp, Charge, Pour, Flurry }

    public static BossForeman Instance { get; private set; }
    public const string Name = "THE STATIONMASTER";
    public const string Subtitle1 = "HALCYON RAILWAYS · THE TERMINUS";   // the House's register: a post, not a model number

    public TextMesh shiftText;

    public State state = State.Dormant;
    public Attack attack = Attack.None;
    public override bool Engaged => state == State.Phase1 || state == State.Transition || state == State.Phase2;
    public override bool IsDead => state == State.Dead;
    public override bool ShowPlates => state == State.Phase1 || state == State.Transition;
    public int PlatesOn { get; private set; }
    public override int PlateCount => Mathf.Max(1, plates.Count);
    public override int PlatesLeft => PlatesOn;
    public bool CoreOpen => WindowOpen;
    public bool Stalled { get; private set; }
    public override float StaggerWindow => config != null ? config.foremanStaggerWindow : 2f;
    public override string PhaseName => state == State.Phase2 ? "LAST TRAIN" : state == State.Transition ? "…" : "FIRST SERVICE";

    // The cards (BossIntro, BossOutro, the bar): the Underground's register — a railway terminus, not a foundry
    public override string BossName => Name;
    public override string CompanyLine => Subtitle1;
    public override string IntroBeat => "PASSENGER · PROCESSED";
    public override string ExitStamp => "DEPARTED";
    public override string ExitLine => "LINE CLOSED";
    public override string LastWord => "DELAYED";
    public override string LastLine => "“THIS DELAY WILL BE LOGGED.”";
    public override string BeatPhase => "LAST TRAIN";
    public override string BeatWord => "PROCESSED";

    const float HammerRecover = 0.9f, SweepRecover = 0.7f, StompTell = 0.95f, StompDrop = 0.22f, StompRecover = 0.6f;
    const float FlurrySpacing = 0.72f, FlurryWindup = 0.6f;
    const float VentLife = 1.15f, VentReach = 6f, RingLife = 1.35f, VolleyGap = 0.3f;
    const float ArenaMargin = 9f, GapCentre = 270f, PlateHalf = 45f;

    float nextVolleyAt, nextHammerAt, nextSweepAt, nextStompAt, nextChargeAt, nextPourAt, nextFlurryAt, nextHookAt;
    float staggerUntil = float.NegativeInfinity, stallUntil = float.NegativeInfinity;
    float lastCounterAt = float.NegativeInfinity;
    float transitionUntil;
    float subtitleAt = float.PositiveInfinity;
    Vector3 standPos;
    Quaternion standRot;

    // The attack in progress
    float attackStart;
    bool attackHitDone, chargeGo, sweepGo, sweepHit, stompDropped;
    int flurryIndex;
    float flurryAt;
    bool flurryHitDone;
    float prevHammerYaw;
    Vector3 pourTarget, chargeDir = Vector3.forward, chargeFrom;
    float restUntil;

    // The volley, fired one rivet at a time
    int volleyLeft;
    float volleyNextAt;
    Vector3 volleyAim;

    // The core-hit counter (it used to be the shield's plates): three of them break it into overtime
    readonly List<Renderer> plates = new List<Renderer>();
    readonly List<bool> plateOn = new List<bool>();

    // The red things: the charge lane, the pour disc, the rings (the slam's, the stomp's), the vent
    Transform lane, pourDisc, ventRing;
    float ventStart = float.NegativeInfinity;
    bool ventHit;
    Vector3 ventCenter;
    class Ring { public Transform t; public Vector3 centre; public float start = float.NegativeInfinity, r0, r1, damage; public bool hit; }
    readonly Ring slamRing = new Ring(), stompRing = new Ring();
    class Pool { public Transform t; public Light light; public Vector3 centre; public float born, radius, nextHurt; }
    readonly List<Pool> pools = new List<Pool>();
    // The melt in the air: the pour is a THROW now, and the pool is where the glob lands
    class Glob { public Transform t; public Vector3 from, to; public float start, radius, nextDrip; }
    readonly List<Glob> globs = new List<Glob>();
    const float GlobFlight = 0.75f, GlobArc = 9f;

    Material redMat, dimMat, moltenMat, plateMat;
    readonly List<Hologram> holograms = new List<Hologram>();
    float idlePhase, flinch, doorOpen, beaconSpin, nextSparkAt, ladleTip, torsoYaw, torsoLift, bob;
    BossOutro outro;
    BossTell tell;                        // the reach, the fuse and the light: every attack announces itself through it
    int tellId;

    void Awake() { Instance = this; Current = this; }

    void OnEnable() { GameBootstrap.Respawned += OnPlayerRespawned; }

    void OnDisable()
    {
        GameBootstrap.Respawned -= OnPlayerRespawned;
        if (Instance == this) Instance = null;
        if (Current == this) Current = null;
    }

    void Start()
    {
        standPos = body.transform.position;
        standRot = body.transform.rotation;
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        redMat = new Material(unlit); redMat.SetColor("_BaseColor", new Color(2.6f, 0.2f, 0.12f));
        dimMat = new Material(unlit); dimMat.SetColor("_BaseColor", new Color(0.9f, 0.25f, 0.2f));
        moltenMat = new Material(unlit); moltenMat.SetColor("_BaseColor", new Color(2.6f, 0.5f, 0.12f));
        plateMat = new Material(lit); plateMat.SetColor("_BaseColor", new Color(0.35f, 0.24f, 0.12f)); plateMat.SetFloat("_Smoothness", 0.5f); plateMat.SetFloat("_Metallic", 0.6f); // dark bronze, as the hull plates are
        body.SetWeakspotOpen(false);
        OpenStacks(); // the stacks are weakspots for the whole fight: the thing you aim at
        if (body.BossButtons != null) foreach (var r in body.BossButtons) { plates.Add(r); plateOn.Add(true); }
        PlatesOn = plates.Count;
        foreach (var h in FindObjectsByType<Hologram>(FindObjectsSortMode.None))
            if (Vector3.Distance(h.transform.position, arenaCenter) < arenaRadius * 2.2f) holograms.Add(h);
        outro = gameObject.AddComponent<BossOutro>(); // its exit; it reads nothing until OnDied hands over
        outro.boss = this;

        lane = MakeBox("ChargeLane");
        pourDisc = MakeRing("PourDisc");
        ventRing = MakeRing("VentRing");
        slamRing.t = MakeRing("SlamRing");
        stompRing.t = MakeRing("StompRing");
        tell = new BossTell(transform);

        SetRise(0f); // under the casting floor until the intro raises it
        if (exitSeal != null) exitSeal.SetActive(true);
        if (entrySeal != null) entrySeal.SetActive(false);
    }

    Transform MakeRing(string name)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial = redMat;
        go.SetActive(false);
        return go.transform;
    }

    Transform MakeBox(string name)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial = dimMat;
        go.SetActive(false);
        return go.transform;
    }

    // ------------------------------------------------------------ the fight's shape

    /// 0 = sunk under the floor on its lift, 1 = up on the casting floor: the intro raises it
    public override void SetRise(float k)
    {
        if (body != null) body.transform.position = standPos + Vector3.down * ((1f - k) * body.Scale * 2.6f);
    }

    public override void SealEntry() { if (entrySeal != null) entrySeal.SetActive(true); }
    public override void BeginIntro() { state = State.Intro; }

    /// The cutscene ends: the shift is on, and the door behind you is where you come back to
    public override void Engage()
    {
        SetRise(1f);
        state = State.Phase1;
        EngagedAt = Time.time;
        GameBootstrap.Checkpoint = checkpoint;
        if (shiftText != null) shiftText.text = "NOW DEPARTING   YOU";
        ArmTimers(Time.time);
        if (BossMusic.Instance != null) BossMusic.Instance.Transition(BossMusic.Section.Drop);
    }

    void ArmTimers(float now)
    {
        nextHammerAt = now + 1.8f;
        nextSweepAt = now + 5f;
        nextStompAt = now + 8f;
        nextChargeAt = now + 11f;
        nextPourAt = now + 6f;
        nextFlurryAt = now + 4f;
        nextVolleyAt = now + 2f;
        nextHookAt = now + 1.5f;
    }

    /// Downed mid-fight: the plates go back on, the doors close, it rolls back to the pit and starts over
    void OnPlayerRespawned()
    {
        if (!Engaged || body == null) return;
        body.RestoreHealth();
        DisplayHealth01 = 1f;
        for (int i = 0; i < plates.Count; i++) { plateOn[i] = true; if (plates[i] != null) ShowPlate(plates[i], true); }
        PlatesOn = plates.Count;
        doorOpen = 0f;
        body.SetEyes(null);
        OpenStacks(); // and the stacks are open again
        CloseCore();
        Stalled = false;
        CloseStacks();
        CancelAttack();
        HideAttacks();
        ClearPools();
        body.transform.SetPositionAndRotation(standPos, standRot);
        torsoYaw = 0f;
        state = State.Phase1;
        ArmTimers(Time.time + 1.5f);
        if (BossMusic.Instance != null) BossMusic.Instance.Transition(BossMusic.Section.Drop);
    }

    static void ShowPlate(Renderer plate, bool on)
    {
        foreach (var r in plate.GetComponentsInChildren<Renderer>(true)) r.enabled = on;
    }

    void HideAttacks()
    {
        if (lane != null) lane.gameObject.SetActive(false);
        if (pourDisc != null) pourDisc.gameObject.SetActive(false);
        if (ventRing != null) ventRing.gameObject.SetActive(false);
        if (slamRing.t != null) slamRing.t.gameObject.SetActive(false);
        if (stompRing.t != null) stompRing.t.gameObject.SetActive(false);
        tell?.End();
    }

    void ClearPools()
    {
        foreach (var p in pools) if (p.t != null) Destroy(p.t.gameObject);
        pools.Clear();
        foreach (var g in globs) if (g.t != null) Destroy(g.t.gameObject);
        globs.Clear();
    }

    void Update()
    {
        float now = Time.time, dt = Time.deltaTime;
        if (state == State.Dead) { TickDead(now); return; }
        if (body == null || config == null) return;
        if (!body.Alive) { OnDied(now); return; }

        Vector3 player = PlayerChest();
        bool turning = attack == Attack.Sweep && sweepGo;
        if (state != State.Dormant && attack != Attack.Charge && !Stalled && !turning) FacePlayer(player, dt);
        TickShield(player, dt);
        TickVolley(now);
        Animate(player, dt);
        tell?.Tick(now, floorY);
        TickGlobs(now);
        if (Time.unscaledTime - LastHitTime > 0.35f)
            DisplayHealth01 = Mathf.MoveTowards(DisplayHealth01, Health01, dt * 0.35f);
        if (!Engaged) return;

        if (CoreOpen && now >= staggerUntil)
        {
            CloseCore();
            Vent(player); // the window closed on nothing: the furnace answers
        }
        StaggerLeft = CoreOpen ? Mathf.Max(0f, staggerUntil - now) : 0f;
        if (Stalled && now >= stallUntil) EndStall();

        Vector3 flat = player - body.transform.position; flat.y = 0f;
        float dist = flat.magnitude;
        if (state == State.Transition)
        {
            if (now >= transitionUntil) EnterPhase2();
        }
        else if (!CoreOpen && !Stalled)
        {
            bool overtime = state == State.Phase2;
            float speed = overtime ? Mathf.Max(0.5f, config.foremanPhase2Speed) : 1f;
            if (attack != Attack.None) TickAttack(player, dist, now, speed);
            else
            {
                if (overtime && now >= nextFlurryAt && dist <= config.foremanHammerRange) StartFlurry();
                else if (now >= nextHammerAt && dist <= config.foremanHammerRange) StartHammer();
                else if (now >= nextSweepAt && dist > 3f && dist <= config.foremanSweepReach + 2f) StartSweep();
                else if (now >= nextStompAt && dist <= config.foremanStompRadius * 0.8f) StartStomp();
                else if (now >= nextChargeAt && dist > 9f) StartCharge(player);
                else if (now >= nextPourAt && dist > 5f && dist <= config.foremanPourRange) StartPour(player);
                else
                {
                    Advance(player, dist, dt, speed);
                    if (now >= nextVolleyAt && dist > config.foremanLeash) { Volley(player); nextVolleyAt = now + config.foremanVolleyInterval / speed; }
                    if (overtime && now >= nextHookAt && dist > 10f) { Hook(player); nextHookAt = now + config.foremanHookInterval; }
                }
            }
        }
        TickRing(slamRing, player, now);
        TickRing(stompRing, player, now);
        TickVent(player, now);
        TickPools(player, now);
    }

    PlayerMotor Motor => CombatDirector.Instance != null ? CombatDirector.Instance.motor : null;

    Vector3 PlayerChest()
    {
        var m = Motor;
        return m != null ? m.CurrPosition + Vector3.up * 0.9f : arenaCenter;
    }

    void FacePlayer(Vector3 player, float dt)
    {
        if (CoreOpen) return; // staggered: it doesn't track
        Vector3 flat = player - body.transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.01f) return;
        body.transform.rotation = Quaternion.RotateTowards(body.transform.rotation, Quaternion.LookRotation(flat), 110f * dt);
    }

    /// Between attacks it comes for you: a glide on the skids, inside the floor's clamp
    void Advance(Vector3 player, float dist, float dt, float speed)
    {
        if (dist <= config.foremanHammerRange * 0.8f) return;
        Vector3 flat = player - body.transform.position; flat.y = 0f;
        Vector3 next = body.transform.position + flat.normalized * (config.foremanWalkSpeed * speed * dt);
        float limit = arenaRadius - ArenaMargin;
        next.x = Mathf.Clamp(next.x, arenaCenter.x - limit, arenaCenter.x + limit);
        next.z = Mathf.Clamp(next.z, arenaCenter.z - limit, arenaCenter.z + limit);
        body.transform.position = next;
        bob = Mathf.MoveTowards(bob, 1f, dt * 3f);
    }

    // ------------------------------------------------------------ the shield

    /// The shield is gone: it is hidden on the first tick and never turns. What is left is the reading — the stacks
    /// are always open, the core opens when it is staggered, and everywhere else it barely feels a hit.
    void TickShield(Vector3 player, float dt)
    {
        var shield = body.BossShield;
        if (shield != null && shield.gameObject.activeSelf) shield.gameObject.SetActive(false);
        if (body.BossShieldBand != null && body.BossShieldBand.enabled) body.BossShieldBand.enabled = false;
    }

    float BodyYaw => body.transform.eulerAngles.y;
    float YawTo(Vector3 from)
    {
        Vector3 flat = from - body.transform.position; flat.y = 0f;
        return Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
    }

    /// Whether a hit from `from` lands: phase one, only the ring's open side (and whatever a lost plate left open);
    /// overtime, only its back; staggered, anywhere
    /// Swing the ring so its open side faces you, and hold it there while the doors are open
    /// A hit on a plate: sparks where it landed, the clank, and the bar says so
    // ------------------------------------------------------------ the body

    void Animate(Vector3 player, float dt)
    {
        var torso = body.BossTorso;
        if (torso == null) return;
        idlePhase += dt;
        float now = Time.time;
        float t = now - attackStart;
        float breathe = Mathf.Sin(idlePhase * 1.1f) * 0.5f;
        float pitch = CoreOpen ? 22f : 0f, yaw = 0f, lift = 0f;
        float shudder = Stalled || attack == Attack.Charge && chargeGo ? Mathf.Sin(now * 43f) * 1.4f : 0f;
        flinch = Mathf.MoveTowards(flinch, 0f, dt * 3f);
        bob = Mathf.MoveTowards(bob, 0f, dt * 2f);

        // The arms at rest: splayed out past the ring, the hammer hanging forward, the boom out and down
        Vector3 dirR = new Vector3(0.5f, -0.8f, 0.35f), dirL = new Vector3(-0.55f, -0.75f, 0.4f);
        float rateL = 4f, rateR = 4f, extendR = 0f, extendL = 0f, tip = 0f, torsoRate = 6f;
        if (CoreOpen)
        {
            dirR = new Vector3(0.6f, -1f, 0.05f); dirL = new Vector3(-0.65f, -1f, 0.05f); rateL = rateR = 5f;
        }
        else switch (attack)
        {
            case Attack.Hammer:
            {
                float windup = config.foremanHammerWindup / TellSpeed;
                if (t < windup)
                {
                    // Reared right back, the hammer over its head, held at the top and straining: the biggest thing it does
                    dirR = new Vector3(0.25f, 1f, -0.5f); rateR = 7f; pitch = -20f; yaw = -25f; lift = 0.14f;
                    dirL = new Vector3(-0.95f, -0.3f, 0.2f); rateL = 6f;
                    if (t > windup * 0.45f) shudder += Mathf.Sin(now * 38f) * 0.9f;
                }
                else if (t < windup + 0.3f) { dirR = new Vector3(0.05f, -0.72f, 0.7f); rateR = 30f; pitch = 34f; yaw = 10f; lift = -0.12f; torsoRate = 18f; }
                else { rateR = 5f; pitch = 6f; }
                break;
            }
            case Attack.Sweep:
            {
                float tell = config.foremanSweepTell / TellSpeed, swing = config.foremanSweepSwing / Speed;
                if (t < tell) { dirR = new Vector3(1f, -0.2f, -0.6f); rateR = 8f; yaw = -80f; pitch = 8f; lift = 0.06f; dirL = new Vector3(-0.85f, -0.45f, 0.3f); rateL = 6f; } // wound right round behind it
                else if (t < tell + swing)
                {
                    float k = (t - tell) / swing;
                    yaw = Mathf.Lerp(-40f, 230f, k);
                    dirR = new Vector3(0.42f, -0.9f, 0.1f); rateR = 30f; pitch = 35f; torsoRate = 40f; // a low scythe, the whole body leaning into it
                    dirL = new Vector3(-0.6f, -0.3f, -0.6f); rateL = 12f;
                }
                else { yaw = 0f; rateR = 6f; torsoRate = 5f; }
                break;
            }
            case Attack.Stomp:
            {
                float tell = StompTell / TellSpeed;
                if (t < tell) { lift = 0.6f * Mathf.SmoothStep(0f, 1f, t / tell); dirR = new Vector3(0.9f, 0.35f, 0.2f); dirL = new Vector3(-0.9f, 0.35f, 0.2f); rateL = rateR = 8f; pitch = -14f; torsoRate = 10f; } // rising, arms thrown up
                else if (t < tell + StompDrop) { lift = -0.14f; pitch = 14f; torsoRate = 30f; dirR = new Vector3(0.7f, -0.7f, 0.2f); dirL = new Vector3(-0.7f, -0.7f, 0.2f); rateL = rateR = 20f; }
                else { pitch = 4f; }
                break;
            }
            case Attack.Charge:
                if (chargeGo) { dirR = new Vector3(0.1f, -0.15f, 0.98f); rateR = 14f; pitch = 12f; dirL = new Vector3(-0.7f, -0.6f, -0.3f); rateL = 8f; }
                else { dirR = new Vector3(0.35f, -0.3f, 0.92f); rateR = 8f; pitch = -10f; lift = -0.12f; } // crouched, the hammer levelled like a lance
                break;
            case Attack.Pour:
            {
                // The ladle goes back over the shoulder, then whips over and tips: the melt leaves it in an arc
                float tele = config.foremanPourTelegraph;
                if (t < tele) { dirL = new Vector3(-0.55f, 0.85f, -0.45f); rateL = 7f; yaw = 18f; pitch = -8f; }
                else if (t < tele + 0.35f) { dirL = new Vector3(-0.35f, 0.15f, 0.95f); rateL = 26f; yaw = -14f; pitch = 16f; torsoRate = 18f; tip = 1f; }
                else { dirL = new Vector3(-0.55f, -0.75f, 0.4f); rateL = 6f; }
                break;
            }
            case Attack.Flurry:
            {
                float ft = now - flurryAt;
                bool windup = ft < FlurryWindup / TellSpeed;
                if (flurryIndex == 0) { if (windup) { dirR = new Vector3(0.45f, 1f, -0.35f); rateR = 12f; yaw = -20f; pitch = -8f; } else { dirR = new Vector3(0.1f, -0.75f, 0.65f); rateR = 30f; pitch = 24f; yaw = 8f; torsoRate = 18f; } }
                else if (flurryIndex == 1) { if (windup) { dirL = new Vector3(-0.45f, 1f, -0.35f); rateL = 12f; yaw = 20f; pitch = -8f; } else { dirL = new Vector3(-0.1f, -0.75f, 0.65f); rateL = 30f; pitch = 24f; yaw = -8f; torsoRate = 18f; } }
                else { if (windup) { dirR = new Vector3(0.3f, 1f, -0.25f); dirL = new Vector3(-0.3f, 1f, -0.25f); rateL = rateR = 12f; pitch = -22f; lift = 0.3f; torsoRate = 10f; } else { dirR = new Vector3(0.2f, -0.7f, 0.7f); dirL = new Vector3(-0.2f, -0.7f, 0.7f); rateL = rateR = 30f; pitch = 34f; lift = -0.15f; torsoRate = 20f; } }
                break;
            }
            default:
                if (Stalled) { dirR = new Vector3(0.6f, -0.95f, 0.15f); dirL = new Vector3(-0.65f, -0.9f, 0.1f); }
                break;
        }
        pitch += breathe + 10f * flinch;
        torsoYaw = Mathf.LerpAngle(torsoYaw, yaw, 1f - Mathf.Exp(-torsoRate * dt));
        torsoLift = Mathf.Lerp(torsoLift, lift, 1f - Mathf.Exp(-torsoRate * dt));
        torso.localRotation = Quaternion.Slerp(torso.localRotation, Quaternion.Euler(pitch + shudder, torsoYaw, shudder * 0.6f), 1f - Mathf.Exp(-(CoreOpen ? 10f : 8f) * dt));
        torso.localPosition = new Vector3(0f, 0.85f + torsoLift + Mathf.Sin(idlePhase * 6f) * 0.03f * bob, 0f);

        // The cab looks down at you; stalled, it hangs and shakes
        var head = body.BossHead;
        if (head != null)
        {
            Vector3 toPlayer = player - head.position;
            float flatD = new Vector2(toPlayer.x, toPlayer.z).magnitude;
            float look = Mathf.Clamp(Mathf.Atan2(-toPlayer.y, Mathf.Max(0.5f, flatD)) * Mathf.Rad2Deg, -10f, 35f);
            float hy = Stalled ? Mathf.Sin(now * 31f) * 4f : -torsoYaw * 0.5f;
            head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(CoreOpen ? 18f : look, hy, 0f), 1f - Mathf.Exp(-5f * dt));
        }

        // The furnace doors: open on the core while it is staggered, and for good from the transition on
        doorOpen = Mathf.MoveTowards(doorOpen, CoreOpen || state == State.Transition || state == State.Phase2 ? 1f : 0f, dt * 1.6f);
        if (body.BossCoatL != null) body.BossCoatL.localRotation = Quaternion.Euler(0f, -115f * doorOpen, 0f);
        if (body.BossCoatR != null) body.BossCoatR.localRotation = Quaternion.Euler(0f, 115f * doorOpen, 0f);

        // The beacon turns, faster once the shift goes to overtime
        var beacon = body.BossBeacon;
        if (beacon != null)
        {
            beaconSpin += dt * (state == State.Phase2 || state == State.Transition ? 460f : 170f);
            beacon.localRotation = Quaternion.Euler(0f, beaconSpin, 0f);
        }

        PoseArm(body.BossArmL, dirL, rateL, extendL, dt);
        PoseArm(body.BossArmR, dirR, rateR, extendR, dt);
        ladleTip = Mathf.MoveTowards(ladleTip, tip, dt * 5f);
        if (body.BossHandL != null) body.BossHandL.localRotation = Quaternion.Euler(0f, 0f, -75f * ladleTip);

        // Sparks out of the stacks while it is stalled, and out of the core while the doors are open on it
        if (now >= nextSparkAt && GameFeel.Instance != null)
        {
            if (Stalled)
            {
                nextSparkAt = now + 0.18f;
                if (body.BossCuffL != null) GameFeel.Instance.Sparks(body.BossCuffL.transform.position, 5, false);
                if (body.BossCuffR != null) GameFeel.Instance.Sparks(body.BossCuffR.transform.position, 5, false);
            }
            else if (CoreOpen && body.BossThroat != null)
            {
                nextSparkAt = now + 0.25f;
                GameFeel.Instance.Sparks(body.BossThroat.transform.position, 4, true);
            }
        }
    }

    float Speed => state == State.Phase2 ? Mathf.Max(0.5f, config.foremanPhase2Speed) : 1f;

    /// Overtime makes it swing faster, but a tell you cannot read is not a tell: the telegraphs only take a third
    /// of the speed-up, so the fight gets harder to survive rather than harder to see
    float TellSpeed => Mathf.Lerp(1f, Speed, 0.35f);

    static void PoseArm(Transform arm, Vector3 localDir, float rate, float extend, float dt)
    {
        if (arm == null) return;
        Quaternion target = Quaternion.FromToRotation(Vector3.down, localDir.normalized);
        arm.localRotation = Quaternion.Slerp(arm.localRotation, target, 1f - Mathf.Exp(-rate * dt));
        float scaleY = Mathf.Lerp(arm.localScale.y, 1f + extend, 1f - Mathf.Exp(-(extend > 0f ? 16f : 6f) * dt));
        arm.localScale = new Vector3(1f, scaleY, 1f);
    }

    // ------------------------------------------------------------ attacks

    Vector3 Muzzle => body.BossMuzzle != null ? body.BossMuzzle.position : body.Center;
    Vector3 HammerPos => body.BossHandR != null ? body.BossHandR.position : body.Center;
    Vector3 LadlePos => body.BossHandL != null ? body.BossHandL.position : body.Center;

    /// The rivet gun: gold rivets to parry back, only when you are out past the leash. They come ONE AT A TIME —
    /// three arriving together is one wall you either eat or do not, where three in a row is three reads.
    void Volley(Vector3 player)
    {
        var motor = Motor;
        Vector3 lead = motor != null ? motor.Velocity * 0.35f : Vector3.zero;
        volleyAim = (player + lead - Muzzle).normalized;
        volleyLeft = Mathf.Max(1, config.foremanVolleyShots);
        volleyNextAt = Time.time;
        flinch = Mathf.Max(flinch, 0.2f);
    }

    /// One rivet per beat until the volley is spent
    void TickVolley(float now)
    {
        if (volleyLeft <= 0 || now < volleyNextAt) return;
        int shots = Mathf.Max(1, config.foremanVolleyShots);
        int i = shots - volleyLeft;
        float spread = shots == 1 ? 0f : Mathf.Lerp(-10f, 10f, i / (float)Mathf.Max(1, shots - 1));
        Vector3 dir = Quaternion.AngleAxis(spread, Vector3.up) * volleyAim;
        EnemyProjectile.Spawn(config, Muzzle + dir * 0.8f, dir, body).TokenLook();   // a punched brass token (look only: the same shot)
        volleyLeft--;
        volleyNextAt = now + VolleyGap;
        if (GameFeel.Instance != null) GameFeel.Instance.PlayEnemyShot();
    }

    /// The winch hook: slow, big, gold — and it drags you in unless you parry it
    void Hook(Vector3 player)
    {
        Vector3 from = body.BossForeL != null ? body.BossForeL.position : Muzzle;
        Vector3 dir = (player - from).normalized;
        var orb = EnemyProjectile.Spawn(config, from + dir * 0.8f, dir, body);
        orb.Configure(config.foremanHookSpeed, 2.4f, true, config.foremanHookImpulse);
        if (GameFeel.Instance != null) GameFeel.Instance.PlayParryWarn();
    }

    void StartHammer()
    {
        attack = Attack.Hammer;
        attackStart = Time.time;
        attackHitDone = false;
        body.SetCoatSkin(Enemy.TelegraphGold());
        body.SetPlateSkin(Enemy.TelegraphGold());
        body.SetWindup(Time.time + config.foremanHammerWindup / TellSpeed);
        Tell(BossTell.Kind.Gold, config.foremanHammerWindup / TellSpeed, Feet, config.foremanHammerRange * 0.8f, body.BossHandR, body.transform, config.foremanHammerRange * 0.35f);
        if (GameFeel.Instance != null) GameFeel.Instance.PlayBossGreetWindup();
    }

    /// The hammer cocked back: red — not this one. Then round the whole body at knee height
    void StartSweep()
    {
        attack = Attack.Sweep;
        attackStart = Time.time;
        sweepGo = false;
        sweepHit = false;
        body.SetCoatSkin(redMat);
        Tell(BossTell.Kind.Red, config.foremanSweepTell / TellSpeed, Feet, config.foremanSweepReach, body.BossHandR, body.transform);
        body.SetPlateSkin(redMat);
        if (GameFeel.Instance != null) GameFeel.Instance.PlayBossReach();
    }

    void StartStomp()
    {
        attack = Attack.Stomp;
        attackStart = Time.time;
        stompDropped = false;
        body.SetCoatSkin(redMat);
        Tell(BossTell.Kind.Red, StompTell / TellSpeed, Feet, config.foremanStompRadius, body.BossTorso, body.transform);
        body.SetPlateSkin(redMat);
        if (GameFeel.Instance != null) GameFeel.Instance.PlayBorerTell();
    }

    /// Three strikes in a row: hammer, boom, both — the first two gold, the last red
    void StartFlurry()
    {
        attack = Attack.Flurry;
        attackStart = Time.time;
        flurryIndex = 0;
        flurryAt = Time.time;
        flurryHitDone = false;
        body.SetCoatSkin(Enemy.TelegraphGold());
        body.SetPlateSkin(Enemy.TelegraphGold());
        body.SetWindup(Time.time + FlurryWindup / TellSpeed);
        Tell(BossTell.Kind.Gold, FlurryWindup / TellSpeed, Feet, config.foremanHammerRange * 0.8f, body.BossHandR, body.transform, config.foremanHammerRange * 0.35f);
        if (GameFeel.Instance != null) GameFeel.Instance.PlayBossGreetWindup();
    }

    /// The ladle swings out: a red disc where the pour lands; then the pool, and it stays
    void StartPour(Vector3 player)
    {
        attack = Attack.Pour;
        attackStart = Time.time;
        attackHitDone = false;
        pourTarget = new Vector3(player.x, floorY, player.z);
        var motor = Motor;
        if (motor != null)
        {
            pourTarget += new Vector3(motor.Velocity.x, 0f, motor.Velocity.z) * 0.3f;
            float feet = player.y - 0.9f;
            if (feet > floorY + 0.8f) pourTarget.y = feet; // up on the gantry: it comes down there
        }
        pourDisc.gameObject.SetActive(true);
        pourDisc.position = pourTarget + Vector3.up * 0.12f;
        pourDisc.localScale = new Vector3(0.5f, 0.06f, 0.5f);
        pourDisc.GetComponent<MeshRenderer>().sharedMaterial = dimMat;
        body.SetCoatSkin(redMat);
        Tell(BossTell.Kind.Red, config.foremanPourTelegraph, pourTarget, config.foremanPourRadius, body.BossHandL, null);
        body.SetPlateSkin(redMat);
        if (GameFeel.Instance != null) GameFeel.Instance.PlayBossReach();
    }

    /// It lines you up: gold body (counter it), the red lane on the floor (do not be on it in a moment), then the drive
    void StartCharge(Vector3 player)
    {
        attack = Attack.Charge;
        attackStart = Time.time;
        attackHitDone = false;
        chargeGo = false;
        Vector3 flat = player - body.transform.position; flat.y = 0f;
        chargeDir = flat.sqrMagnitude > 0.01f ? flat.normalized : body.transform.forward;
        chargeFrom = body.transform.position;
        body.SetPlateSkin(Enemy.TelegraphGold());
        Tell(BossTell.Kind.Gold, config.foremanChargeTell, Feet, 5f, body.BossHandR, body.transform);
        body.SetWindup(Time.time + config.foremanChargeTell);
        LayLane(true, false);
        if (GameFeel.Instance != null) GameFeel.Instance.PlayBorerTell();
    }

    /// Where it stands, on the floor
    Vector3 Feet { get { Vector3 p = body.transform.position; p.y = floorY; return p; } }

    /// The announcement: gold to answer, red to move; where it lands, how far it reaches, what to watch
    void Tell(BossTell.Kind k, float seconds, Vector3 centre, float reach, Transform focus, Transform follow, float ahead = 0f, Transform focus2 = null)
    {
        if (tell == null) return;
        tellId = tell.Begin(k, seconds, centre, reach, focus, follow, ahead, focus2);
    }

    void EndTell() => tell?.End(tellId);

    /// It steps INTO the blow: the body slides forward through the last beat of a wind-up, so the hammer arrives
    /// with the weight behind it instead of the arm stretching out to reach you
    void Lunge(float dt, float speed = 11f)
    {
        Vector3 next = body.transform.position + body.transform.forward * (speed * dt);
        float limit = arenaRadius - ArenaMargin;
        next.x = Mathf.Clamp(next.x, arenaCenter.x - limit, arenaCenter.x + limit);
        next.z = Mathf.Clamp(next.z, arenaCenter.z - limit, arenaCenter.z + limit);
        body.transform.position = next;
    }

    /// The pour is a THROW: the ladle whips over and the melt leaves it in an arc — a glob you can watch all the way
    /// down, dripping sparks — and the pool is wherever it lands
    void ThrowGlob(Vector3 to, float radius)
    {
        var g = new Glob { from = LadlePos, to = to, start = Time.time, radius = radius };
        var go = LevelKit.Prim(null, g.from, Vector3.one * 1.1f, moltenMat, "Glob", PrimitiveType.Sphere);
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 9f;
        light.intensity = 3f;
        light.color = new Color(1f, 0.5f, 0.15f);
        light.shadows = LightShadows.None;
        g.t = go.transform;
        globs.Add(g);
        if (GameFeel.Instance != null) { GameFeel.Instance.PlayBossReach(); GameFeel.Instance.Sparks(g.from, 16, false); }
    }

    void TickGlobs(float now)
    {
        for (int i = globs.Count - 1; i >= 0; i--)
        {
            var g = globs[i];
            if (g.t == null) { globs.RemoveAt(i); continue; }
            float k = (now - g.start) / GlobFlight;
            if (k >= 1f)
            {
                SpawnPool(g.to, g.radius);
                if (pourDisc != null) pourDisc.gameObject.SetActive(false);
                if (GameFeel.Instance != null) { GameFeel.Instance.PlayChargeBlast(); GameFeel.Instance.Sparks(g.to + Vector3.up * 0.4f, 40, false); }
                Destroy(g.t.gameObject);
                globs.RemoveAt(i);
                continue;
            }
            g.t.position = Vector3.Lerp(g.from, g.to, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * GlobArc);
            g.t.localScale = Vector3.one * (1.1f + 0.25f * Mathf.Sin(now * 30f));
            if (now >= g.nextDrip && GameFeel.Instance != null) { g.nextDrip = now + 0.06f; GameFeel.Instance.Sparks(g.t.position, 3, false); }
        }
    }

    void LayLane(bool on, bool hot)
    {
        lane.gameObject.SetActive(on);
        if (!on) return;
        float len = config.foremanChargeRange + 2f;
        lane.position = body.transform.position + chargeDir * (len * 0.5f) + Vector3.up * 0.06f;
        lane.rotation = Quaternion.LookRotation(chargeDir);
        lane.localScale = new Vector3(2.4f * body.Scale, 0.04f, len);
        lane.GetComponent<MeshRenderer>().sharedMaterial = hot ? redMat : dimMat;
    }

    /// A parry on the ram while it is coming (near the hammer head): a COUNTER
    bool TryCounter(Vector3 player, float dist, Vector3 at)
    {
        var motor = Motor;
        if (motor == null || dist > config.foremanHammerRange + 3f || !motor.ParryWindowOpen) return false;
        if (!motor.TryConsumeParry(at, "COUNTER!")) return false;
        OnCountered();
        if (GameFeel.Instance != null) GameFeel.Instance.PlayBossCounter();
        return true;
    }

    /// The blow in front of it: whoever is close, in front, and low takes it
    void Strike(Vector3 player, float dist, float damage, float range)
    {
        var motor = Motor;
        float feet = player.y - 0.9f;
        Vector3 flat = player - body.transform.position; flat.y = 0f;
        float facing = Vector3.Dot(flat.normalized, body.transform.forward);
        if (dist <= range && facing > Mathf.Cos(70f * Mathf.Deg2Rad) && feet < floorY + 3f
            && motor != null && !motor.HasIFrames && CombatDirector.Instance != null)
        {
            CombatDirector.Instance.ReportPlayerHit(flat.normalized, damage);
            motor.AddImpulse(flat.normalized * 6f + Vector3.up * 5f);
        }
    }

    void TickAttack(Vector3 player, float dist, float now, float speed)
    {
        float t = now - attackStart;
        float tellSpeed = Mathf.Lerp(1f, speed, 0.35f); // see TellSpeed: the wind-ups stay readable in overtime
        var motor = Motor;
        switch (attack)
        {
            case Attack.Hammer:
            {
                float windup = config.foremanHammerWindup / tellSpeed;
                if (t < windup) { if (t > windup - 0.22f) Lunge(Time.deltaTime); TryCounter(player, dist, HammerPos); return; }
                if (!attackHitDone)
                {
                    attackHitDone = true;
                    body.SetCoatSkin(null);
                    body.SetPlateSkin(null);
                    EndTell();
                    flinch = 0.7f;
                    if (GameFeel.Instance != null) GameFeel.Instance.PlayBossSlam();
                    Strike(player, dist, config.foremanHammerDamage, config.foremanHammerRange);
                    Vector3 impact = body.transform.position + body.transform.forward * (1.6f * body.Scale);
                    impact.y = floorY;
                    StartRing(slamRing, impact, 1.2f * body.Scale, config.foremanSlamRadius, config.foremanSlamDamage);
                    if (GameFeel.Instance != null) GameFeel.Instance.Sparks(impact + Vector3.up * 0.3f, 24, false);
                }
                if (t >= windup + HammerRecover) EndAttack();
                return;
            }
            case Attack.Sweep:
            {
                float tell = config.foremanSweepTell / tellSpeed, swing = config.foremanSweepSwing / speed;
                if (t < tell) return;
                if (!sweepGo)
                {
                    sweepGo = true;
                    EndTell();
                    prevHammerYaw = YawTo(HammerPos);
                    if (GameFeel.Instance != null) GameFeel.Instance.PlayBossSweep();
                }
                if (t < tell + swing)
                {
                    // The hammer head comes round: if its arc crossed you this frame, at your height, it takes you with it
                    float yawNow = YawTo(HammerPos);
                    if (!sweepHit && motor != null && !motor.HasIFrames && CombatDirector.Instance != null)
                    {
                        float feet = player.y - 0.9f;
                        float top = feet + (motor.Sliding ? config.slideCapsuleHeight : 1.8f);
                        float bladeY = HammerPos.y; // the band is the hammer head itself, not a number: slide under it or jump it
                        if (dist > 2.5f && dist <= config.foremanSweepReach && feet <= bladeY + 0.3f && top >= bladeY - 0.3f)
                        {
                            float pAng = YawTo(player);
                            float da = Mathf.DeltaAngle(prevHammerYaw, pAng), db = Mathf.DeltaAngle(prevHammerYaw, yawNow);
                            if (da >= -2f && da <= db + 2f)
                            {
                                sweepHit = true;
                                Vector3 push = Quaternion.Euler(0f, yawNow + 90f, 0f) * Vector3.forward;
                                CombatDirector.Instance.ReportPlayerHit(push, config.foremanSweepDamage);
                                motor.AddImpulse(push * 8f + Vector3.up * 5f);
                            }
                        }
                    }
                    prevHammerYaw = yawNow;
                    return;
                }
                if (t >= tell + swing + SweepRecover) EndAttack();
                else { body.SetCoatSkin(null); body.SetPlateSkin(null); }
                return;
            }
            case Attack.Stomp:
            {
                float tell = StompTell / tellSpeed;
                if (t < tell) return;
                if (!stompDropped)
                {
                    stompDropped = true;
                    body.SetCoatSkin(null);
                    EndTell();
                    body.SetPlateSkin(null);
                    Vector3 c = body.transform.position; c.y = floorY;
                    StartRing(stompRing, c, 1.6f * body.Scale, config.foremanStompRadius, config.foremanStompDamage);
                    if (GameFeel.Instance != null) { GameFeel.Instance.PlayBossStomp(); GameFeel.Instance.Sparks(c + Vector3.up * 0.4f, 40, false); }
                }
                if (t >= tell + StompDrop + StompRecover) EndAttack();
                return;
            }
            case Attack.Flurry:
            {
                float ft = now - flurryAt;
                float windup = FlurryWindup / tellSpeed;
                bool red = flurryIndex == 2;
                if (ft < windup)
                {
                    if (ft > windup - 0.2f) Lunge(Time.deltaTime, 9f);
                    if (!red) TryCounter(player, dist, flurryIndex == 0 ? HammerPos : LadlePos);
                    return;
                }
                if (!flurryHitDone)
                {
                    flurryHitDone = true;
                    body.SetCoatSkin(null);
                    body.SetPlateSkin(null);
                    EndTell();
                    flinch = 0.6f;
                    if (GameFeel.Instance != null) GameFeel.Instance.PlayBossSlam();
                    Strike(player, dist, red ? config.foremanFlurryDamage * 1.5f : config.foremanFlurryDamage, config.foremanHammerRange);
                    Vector3 impact = body.transform.position + body.transform.forward * (1.5f * body.Scale);
                    impact.y = floorY;
                    if (red) StartRing(slamRing, impact, 1.2f * body.Scale, config.foremanSlamRadius, config.foremanSlamDamage);
                    if (GameFeel.Instance != null) GameFeel.Instance.Sparks(impact + Vector3.up * 0.3f, red ? 30 : 14, false);
                }
                if (ft >= FlurrySpacing / speed)
                {
                    if (flurryIndex >= 2) { if (ft >= FlurrySpacing / speed + 0.6f) EndAttack(); return; }
                    flurryIndex++;
                    flurryAt = now;
                    flurryHitDone = false;
                    bool nextRed = flurryIndex == 2;
                    body.SetCoatSkin(nextRed ? redMat : Enemy.TelegraphGold());
                    body.SetPlateSkin(nextRed ? redMat : Enemy.TelegraphGold());
                    if (!nextRed) body.SetWindup(now + windup);
                    Tell(nextRed ? BossTell.Kind.Red : BossTell.Kind.Gold, windup, Feet, config.foremanHammerRange * 0.8f, nextRed || flurryIndex == 0 ? body.BossHandR : body.BossHandL, body.transform, config.foremanHammerRange * 0.35f, nextRed ? body.BossHandL : null);
                    if (GameFeel.Instance != null) { if (nextRed) GameFeel.Instance.PlayBorerTell(); else GameFeel.Instance.PlayBossGreetWindup(); }
                }
                return;
            }
            case Attack.Pour:
            {
                float tele = config.foremanPourTelegraph;
                if (t < tele)
                {
                    float k = t / tele;
                    float r = Mathf.Lerp(0.5f, config.foremanPourRadius, k);
                    pourDisc.localScale = new Vector3(r * 2f, 0.06f, r * 2f);
                    if (k > 0.6f) pourDisc.GetComponent<MeshRenderer>().sharedMaterial = redMat;
                    return;
                }
                if (!attackHitDone)
                {
                    attackHitDone = true;
                    body.SetCoatSkin(null);
                    EndTell();
                    body.SetPlateSkin(null);
                    ThrowGlob(pourTarget, config.foremanPourRadius);   // the disc stays lit under it until the glob lands
                    if (state == State.Phase2)
                    {
                        Vector3 across = Vector3.Cross(Vector3.up, (pourTarget - body.transform.position).normalized);
                        ThrowGlob(pourTarget + across * (config.foremanPourRadius * 1.9f), config.foremanPourRadius * 0.85f);
                        ThrowGlob(pourTarget - across * (config.foremanPourRadius * 1.9f), config.foremanPourRadius * 0.85f);
                    }
                    restUntil = now + config.foremanPourRest;
                }
                if (now >= restUntil + 0.3f) EndAttack();
                return;
            }
            case Attack.Charge:
            {
                if (t < config.foremanChargeTell)
                {
                    body.transform.rotation = Quaternion.RotateTowards(body.transform.rotation, Quaternion.LookRotation(chargeDir), 540f * Time.deltaTime);
                    LayLane(true, false);
                    TryCounter(player, dist, HammerPos); // gold body, gold rule: answer the ram and it is countered
                    return;
                }
                if (!chargeGo)
                {
                    chargeGo = true;
                    EndTell();
                    body.SetPlateSkin(null);
                    body.SetWindup(0f);
                    LayLane(true, true);
                    if (GameFeel.Instance != null) GameFeel.Instance.PlayBorerCharge();
                }
                Vector3 pos = body.transform.position;
                Vector3 next = pos + chargeDir * (config.foremanChargeSpeed * Time.deltaTime);
                float limit = arenaRadius - ArenaMargin;
                bool wall = Mathf.Abs(next.x - arenaCenter.x) > limit || Mathf.Abs(next.z - arenaCenter.z) > limit;
                if (wall || Vector3.Distance(next, chargeFrom) >= config.foremanChargeRange)
                {
                    StartStall(wall);
                    return;
                }
                body.transform.position = next;
                if (!attackHitDone && motor != null && !motor.HasIFrames && CombatDirector.Instance != null)
                {
                    Vector3 flat = player - next; flat.y = 0f;
                    float feet = player.y - 0.9f;
                    if (flat.magnitude <= config.foremanChargeHitRange && feet < floorY + 3.5f)
                    {
                        attackHitDone = true;
                        CombatDirector.Instance.ReportPlayerHit((chargeDir + Vector3.up * 0.4f).normalized, config.foremanChargeDamage);
                        motor.AddImpulse(chargeDir * 10f + Vector3.up * 8f);
                    }
                }
                return;
            }
        }
    }

    /// The drive ends — in a wall or at the lane's end — and it stalls: the lids blow off the stacks
    void StartStall(bool wall)
    {
        attack = Attack.None;
        attackHitDone = false;
        LayLane(false, false);
        body.SetWindup(0f);
        body.SetPlateSkin(null);
        Stalled = true;
        stallUntil = Time.time + config.foremanStallTime;
        nextChargeAt = stallUntil + config.foremanChargeCooldown;
        OpenStacks();
        flinch = 1f;
        if (GameFeel.Instance != null)
        {
            if (wall) GameFeel.Instance.PlayBorerJam(); else GameFeel.Instance.PlayBossStagger();
        }
    }

    void EndStall()
    {
        if (!Stalled) return;
        Stalled = false;
        if (state != State.Phase2) CloseStacks(); // in overtime the stacks stay open: they are the spot
    }

    void EndAttack()
    {
        float now = Time.time;
        switch (attack)
        {
            case Attack.Hammer: nextHammerAt = now + config.foremanHammerCooldown / Speed; break;
            case Attack.Sweep: nextSweepAt = now + config.foremanSweepInterval / Speed; break;
            case Attack.Stomp: nextStompAt = now + config.foremanStompInterval / Speed; break;
            case Attack.Pour: nextPourAt = now + config.foremanPourCooldown; break;
            case Attack.Charge: nextChargeAt = now + config.foremanChargeCooldown; break;
            case Attack.Flurry: nextFlurryAt = now + config.foremanFlurryInterval; nextHammerAt = Mathf.Max(nextHammerAt, now + 1.5f); break;
        }
        attack = Attack.None;
        attackHitDone = false;
        chargeGo = false;
        EndTell();
        sweepGo = false;
        body.SetWindup(0f);
        body.SetCoatSkin(null);
    }

    /// A counter or a cut ends the attack now: the arm recoils, the lane and the disc go
    void CancelAttack()
    {
        if (attack == Attack.None) return;
        EndAttack();
        restUntil = 0f;
        body.SetPlateSkin(null);
        if (lane != null) lane.gameObject.SetActive(false);
        if (pourDisc != null) pourDisc.gameObject.SetActive(false);
    }

    void OpenStacks()
    {
        body.SetWeakspot(body.BossCuffL, true);
        body.SetWeakspot(body.BossCuffR, true);
        if (!CoreOpen) OpenSpot = "STACKS";
    }

    void CloseStacks()
    {
        body.SetWeakspot(body.BossCuffL, false);
        body.SetWeakspot(body.BossCuffR, false);
        if (OpenSpot == "STACKS") OpenSpot = CoreOpen ? "CORE" : "";
    }

    void StartRing(Ring ring, Vector3 centre, float r0, float r1, float damage)
    {
        ring.start = Time.time;
        ring.hit = false;
        ring.centre = centre;
        ring.r0 = r0;
        ring.r1 = r1;
        ring.damage = damage;
        ring.t.gameObject.SetActive(true);
        ring.t.position = centre + Vector3.up * 0.15f;
        ring.t.localScale = new Vector3(r0 * 2f, 0.06f, r0 * 2f);
    }

    void TickRing(Ring ring, Vector3 player, float now)
    {
        if (ring.t == null || !ring.t.gameObject.activeSelf) return;
        float k = (now - ring.start) / RingLife;
        if (k >= 1f) { ring.t.gameObject.SetActive(false); return; }
        float r = Mathf.Lerp(ring.r0, ring.r1, k);
        ring.t.localScale = new Vector3(r * 2f, 0.06f, r * 2f);
        if (ring.hit) return;
        var motor = Motor;
        if (motor == null || !motor.Grounded || motor.HasIFrames || CombatDirector.Instance == null) return; // in the air: it passes under you
        if (player.y - 0.9f > ring.centre.y + 1.0f) return; // up on the gantry: it passes below you
        Vector3 flat = player - ring.centre; flat.y = 0f;
        float dist = flat.magnitude;
        if (Mathf.Abs(dist - r) > 1.6f) return;
        ring.hit = true;
        CombatDirector.Instance.ReportPlayerHit(flat.sqrMagnitude > 0.01f ? flat.normalized : Vector3.forward, ring.damage);
    }

    /// The furnace vents on you: a ring of fire at your feet — it winds up before it bites: move
    void Vent(Vector3 player)
    {
        ventStart = Time.time;
        ventHit = false;
        ventCenter = new Vector3(player.x, player.y - 0.9f + 0.15f, player.z);
        ventRing.gameObject.SetActive(true);
        ventRing.position = ventCenter;
        ventRing.localScale = new Vector3(0.5f, 0.06f, 0.5f);
        if (GameFeel.Instance != null) GameFeel.Instance.PlayChargeBlast();
    }

    void TickVent(Vector3 player, float now)
    {
        if (!ventRing.gameObject.activeSelf) return;
        float k = (now - ventStart) / VentLife;
        if (k >= 1f) { ventRing.gameObject.SetActive(false); return; }
        float r = Mathf.Lerp(0.5f, VentReach, k);
        ventRing.localScale = new Vector3(r * 2f, 0.06f, r * 2f);
        if (ventHit || k < 0.5f) return; // it grows at your feet for half a second before it bites: that is the cue
        var motor = Motor;
        if (motor == null || motor.HasIFrames || CombatDirector.Instance == null) return;
        Vector3 flat = player - ventCenter; flat.y = 0f;
        if (flat.magnitude > r || player.y - 0.9f > ventCenter.y + 1.3f) return;
        ventHit = true;
        CombatDirector.Instance.ReportPlayerHit(flat.sqrMagnitude > 0.01f ? flat.normalized : Vector3.forward, config.foremanVentDamage);
    }

    // ------------------------------------------------------------ the pools

    /// A pool of molten metal on the floor: it glows, it burns anyone standing in it, and it cools away
    void SpawnPool(Vector3 at, float radius)
    {
        while (pools.Count >= 6) { if (pools[0].t != null) Destroy(pools[0].t.gameObject); pools.RemoveAt(0); }
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "MoltenPool";
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial = moltenMat;
        go.transform.position = at + Vector3.up * 0.05f;
        go.transform.localScale = new Vector3(radius * 2f, 0.08f, radius * 2f);
        var lightGo = new GameObject("PoolLight");
        lightGo.transform.SetParent(go.transform, false);
        lightGo.transform.position = at + Vector3.up * 1.2f;
        var l = lightGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = radius * 4f;
        l.intensity = 3.2f;
        l.color = new Color(1f, 0.45f, 0.15f);
        l.shadows = LightShadows.None;
        pools.Add(new Pool { t = go.transform, light = l, centre = at, born = Time.time, radius = radius, nextHurt = 0f });
        if (GameFeel.Instance != null) GameFeel.Instance.Sparks(at + Vector3.up * 0.4f, 18, false);
    }

    void TickPools(Vector3 player, float now)
    {
        var motor = Motor;
        float feet = player.y - 0.9f;
        for (int i = pools.Count - 1; i >= 0; i--)
        {
            var p = pools[i];
            float age = now - p.born;
            if (age >= config.foremanPoolLife || p.t == null)
            {
                if (p.t != null) Destroy(p.t.gameObject);
                pools.RemoveAt(i);
                continue;
            }
            float cool = Mathf.Clamp01((config.foremanPoolLife - age) / 1.5f);
            float pulse = 1f + 0.04f * Mathf.Sin(now * 9f + i);
            float r = p.radius * cool * pulse;
            p.t.localScale = new Vector3(r * 2f, 0.08f, r * 2f);
            if (p.light != null) p.light.intensity = 3.2f * cool;
            if (motor == null || motor.HasIFrames || CombatDirector.Instance == null || now < p.nextHurt) continue;
            Vector3 flat = player - p.centre; flat.y = 0f;
            if (flat.magnitude > r || feet > p.centre.y + 1.2f || feet < p.centre.y - 1f) continue;
            p.nextHurt = now + 0.5f;
            Vector3 away = flat.sqrMagnitude > 0.01f ? flat.normalized : body.transform.forward;
            CombatDirector.Instance.ReportPlayerHit(away, config.foremanPourDamage);
            motor.AddImpulse(Vector3.up * 9f + away * 4f); // out of it
        }
    }

    // ------------------------------------------------------------ being hurt

    /// The body asks before taking damage; every answer shows on the bar. A hit anywhere but the ring's open side
    /// (or its back, in overtime) is SHIELDED and does nothing. Slashes chip. Its own rivets sent home bite and
    /// stagger it, as does a counter. Executes bite by the spot: the core deep — and a plate off the ring — a stack
    /// less, and a cut stack ends the stall it came with.
    public override bool FilterDamage(ref float amount, KillMethod method)
    {
        if (!Engaged) return false;
        Vector3 player = PlayerChest();
        string label;
        switch (method)
        {
            case KillMethod.SlamShockwave:
                amount = config.foremanSlashDamage;
                label = "SLAM";
                break;
            case KillMethod.Fist:   // a punch chips like a slash
                amount = config.foremanSlashDamage;
                label = "JAB";
                break;
            case KillMethod.Tackle: // a body at speed lands like a slam
                amount = config.foremanSlashDamage;
                label = "TACKLE";
                break;
            case KillMethod.Cleave:
                amount = config.foremanSlashDamage * 3f;
                label = "CLEAVE";
                break;
            case KillMethod.ReflectedProjectile:
                amount = config.foremanReflectDamage;
                label = "PARRY";
                if (!CoreOpen) Stagger();
                break;
            case KillMethod.MeleeParry:
                amount = config.foremanCounterDamage;
                label = "COUNTER";
                if (!CoreOpen) Stagger();
                break;
            case KillMethod.SwordExecute:
            {
                Renderer spot = NearestOpenSpot(Fists.LastStrikePoint);
                if (spot != null && spot == body.BossThroat)
                {
                    amount = config.foremanCoreDamage;
                    label = "EXECUTE";
                    CloseCore();
                    if (state == State.Phase1) PopPlate();
                }
                else if (spot != null)
                {
                    amount = config.foremanStackDamage;
                    label = "STACK CUT";
                    if (Stalled) EndStall();
                }
                else
                {
                    amount = config.foremanSlashDamage;
                    label = "SLASH";
                }
                flinch = 1f;
                if (GameFeel.Instance != null) GameFeel.Instance.PlayBossHit();
                break;
            }
            default:
                if (GameFeel.Instance != null) GameFeel.Instance.PlayClank(0.7f); // plate: it doesn't even notice
                return false;
        }
        RegisterHit(amount, label);
        return true;
    }

    void RegisterHit(float amount, string label)
    {
        HitsTaken++;
        LastHitTime = Time.unscaledTime;
        LastHitFraction = config.foremanHealth > 0f ? amount / config.foremanHealth : 0f;
        LastHitLabel = label;
        flinch = Mathf.Max(flinch, 0.5f);
    }

    Renderer NearestOpenSpot(Vector3 point)
    {
        Renderer best = null;
        float bestD = float.PositiveInfinity;
        void Consider(Renderer r, bool open)
        {
            if (r == null || !open) return;
            float d = Vector3.Distance(point, r.transform.position);
            if (d < bestD) { bestD = d; best = r; }
        }
        Consider(body.BossThroat, CoreOpen);
        Consider(body.BossCuffL, body.WeakspotIsOpen(body.BossCuffL));
        Consider(body.BossCuffR, body.WeakspotIsOpen(body.BossCuffR));
        return best;
    }

    /// A parry landed on its hammer, its flurry or its line-up (from here, or the director's melee parry near its body)
    public override void OnCountered()
    {
        if (!Engaged || Time.time - lastCounterAt < 0.5f) return; // one counter per parry, whichever path reports it
        lastCounterAt = Time.time;
        Counters++;
        if (attack == Attack.Hammer || attack == Attack.Charge || attack == Attack.Flurry) CancelAttack();
        float dmg = config.foremanCounterDamage;
        if (!CoreOpen) Stagger();
        body.Damage(dmg, KillMethod.MeleeParry); // filtered above: the counter's bite, on the bar
    }

    /// A plate comes off the ring: the bolts go and it spins away; the ring is that much more open
    void PopPlate()
    {
        for (int i = 0; i < plates.Count; i++)
        {
            if (!plateOn[i]) continue;
            plateOn[i] = false;
            PlatesOn--;
            var r = plates[i];
            if (r != null)
            {
                ShowPlate(r, false);
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "CoatPanel";
                shard.layer = 2;
                shard.transform.SetPositionAndRotation(r.transform.position, r.transform.rotation);
                shard.transform.localScale = r.transform.lossyScale;
                shard.GetComponent<MeshRenderer>().sharedMaterial = plateMat;
                var rb = shard.AddComponent<Rigidbody>();
                rb.mass = 8f;
                rb.linearVelocity = r.transform.forward * 9f + Vector3.up * 6f + Random.insideUnitSphere * 2f;
                rb.angularVelocity = Random.insideUnitSphere * 7f;
                shard.AddComponent<ShardFade>();
                if (GameFeel.Instance != null) GameFeel.Instance.Sparks(r.transform.position, 20, false);
            }
            break;
        }
        if (GameFeel.Instance != null) GameFeel.Instance.PlayBossPlatePop(); // iron off the ring, not the Concierge's porcelain
        if (PlatesOn <= 0) BeginTransition();
    }

    /// The last plate goes: the ring is gone, the lamps go red, the beacon races — it guards with the hammer now
    void BeginTransition()
    {
        state = State.Transition;
        transitionUntil = Time.time + 2.6f;
        CancelAttack();
        HideAttacks();
        EndStall();
        body.SetEyes(Enemy.EyeRed());
        if (body.BossShieldBand != null) body.BossShieldBand.enabled = false;
        if (GameFeel.Instance != null) GameFeel.Instance.PlayBossCrack();
        if (BossMusic.Instance != null) BossMusic.Instance.Transition(BossMusic.Section.Breakdown);
    }

    void EnterPhase2()
    {
        state = State.Phase2;
        float now = Time.time;
        nextHookAt = now + 1.5f;
        nextVolleyAt = now + 2.5f;
        nextHammerAt = now + 2.5f;
        nextFlurryAt = now + 4f;
        nextSweepAt = now + 6f;
        nextStompAt = now + 8f;
        nextPourAt = now + 5f;
        nextChargeAt = now + 9f;
        OpenStacks(); // its back is the spot now
        if (shiftText != null) shiftText.text = "OVERTIME   YOU";
        if (BossMusic.Instance != null) BossMusic.Instance.Transition(BossMusic.Section.Climax);
    }

    /// It staggers: the ring swings its open side to you and holds, the doors open on the core, cyan, in reach
    void Stagger()
    {
        WindowOpen = true;
        staggerUntil = Time.time + config.foremanStaggerWindow;
        body.SetWeakspotOpen(true);
        OpenSpot = "CORE";
        if (attack != Attack.None) CancelAttack();
        body.SetCoatSkin(null);
        if (GameFeel.Instance != null) GameFeel.Instance.PlayBossStagger();
    }

    void CloseCore()
    {
        WindowOpen = false;
        StaggerLeft = 0f;
        if (body != null) body.SetWeakspotOpen(false);
        if (OpenSpot == "CORE") OpenSpot = Stalled || state == State.Phase2 ? "STACKS" : "";
    }

    // ------------------------------------------------------------ the end

    void OnDied(float now)
    {
        state = State.Dead;
        HideAttacks();
        CloseCore();
        Stalled = false;
        foreach (var p in new List<EnemyProjectile>(EnemyProjectile.Active)) if (p != null) Destroy(p.gameObject); // nothing in flight outlives it
        foreach (var h in holograms) if (h != null) h.Lock("SCRAP", "PROCESSED");
        if (shiftText != null) shiftText.text = "SHIFT COMPLETE";
        subtitleAt = float.PositiveInfinity; // the exit plays first; Dismissed() hands the last word back
        if (GameFeel.Instance != null) GameFeel.Instance.PlayBossKill();
        if (CombatDirector.Instance != null && CombatDirector.Instance.style != null)
            CombatDirector.Instance.style.AddBonus(config.foremanStyleBonus);
        if (outro != null) outro.Begin();
        else { Fall(); Dismissed(); }
    }

    /// The exit's burst: the pools go cold, the music cuts dead, and the body comes apart (the outro throws the pieces)
    public override GameObject Fall()
    {
        ClearPools();
        if (BossMusic.Instance != null) BossMusic.Instance.Transition(BossMusic.Section.Tail);
        return body != null ? body.Collapse() : null;
    }

    /// The exit is over: the blast doors open and the plant has the last word
    public override void Dismissed()
    {
        if (exitSeal != null) exitSeal.SetActive(false);
        subtitleAt = Time.time + 0.8f;
    }

    void TickDead(float now)
    {
        if (now < subtitleAt) return;
        subtitleAt = float.PositiveInfinity;
        Subtitle = "SHIFT COMPLETE. YOU'VE BEEN PROCESSED.";
        SubtitleUntil = now + 6f;
    }
}
