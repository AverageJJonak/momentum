using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE CONCIERGE — the end of Chapter 1, behind his own front desk. A tall thing
/// in a tailcoat with a porcelain smile, and every hit you land shows on his bar.
///  Phase one, HOSPITALITY: slashes chip him; his gold volleys parried back into
///    him pop a gold BUTTON each (four). A red sweep at knee height crosses the
///    floor (jump it or slide under it); the desk bell rings a red ring across the
///    floor (be in the air, or on the desk). Come close and he greets you: both
///    arms spread wide (gold — parry the clap for a COUNTER), the cuffs at his
///    wrists open (cyan) while they are spread. Stay out on the floor and he
///    reaches for a handshake — a red disc where his hand will land; the hand
///    rests there afterwards with its cuff open.
///  Phase two, ACCOMMODATION: the coat swings open on the vines inside, the eyes
///    go red, the smile widens. The sweep rises to wherever you stand. The
///    announcement — a slow gold orb that drags you in — joins the volleys.
///    Parry anything (or counter a greeting) and he STAGGERS: he bows over the
///    desk and the THROAT behind his bow tie opens, cyan, in reach from the
///    counter. Executes there take big bites. Miss the window and the vines lash.
///  The tag: NOW SERVING says YOU when he rises and THANK YOU when he falls; the
///    holograms lock to HOLLOW; a voice we have not heard says a room has been
///    found for you (the House's register: its guests never check out).
/// The body is an Enemy of kind Boss (its own rig); this drives it. All damage
/// is filtered here so the bar is honest. Getting downed puts you back at the
/// door and the fight starts over.
/// </summary>
public class BossConcierge : Boss, IWeldDynamic
{
    /// The desk bell rocks when he rings it: the weld leaves it alone
    public void DynamicParts(List<Transform> into) { if (bell != null) into.Add(bell); }
    public enum State { Dormant, Intro, Phase1, Transition, Phase2, Dead }
    public enum Attack { None, Greeting, Handshake }

    public static BossConcierge Instance { get; private set; }
    public const string Name = "THE CONCIERGE";
    public const string Subtitle1 = "HALCYON HOUSE · FRONT OF HOUSE";   // the House's register: a post, not a model number

    public TextMesh servingText;
    public Transform bell;

    public State state = State.Dormant;
    public Attack attack = Attack.None;
    public override bool Engaged => state == State.Phase1 || state == State.Transition || state == State.Phase2;
    public override bool IsDead => state == State.Dead;
    public override bool ShowPlates => state == State.Phase1 || state == State.Transition;
    public int ButtonsLeft { get; private set; }
    public int ButtonCount => config != null ? Mathf.Max(1, config.bossButtonCount) : 4;
    public override int PlateCount => ButtonCount;
    public override int PlatesLeft => ButtonsLeft;
    public bool ThroatOpen => WindowOpen;
    public override float StaggerWindow => config != null ? config.bossStaggerWindow : 2f;
    // The House's register (2026-10-07): hospitality first, then ACCOMMODATION — a guest who never checks out. "HOLLOW"
    // stays: it is the rot's word, not the company's
    public override string PhaseName => state == State.Phase2 ? "ACCOMMODATION" : state == State.Transition ? "…" : "HOSPITALITY";

    public override string BossName => Name;
    public override string CompanyLine => Subtitle1;
    public override string IntroBeat => "HOLLOW · ACCOMMODATED";
    public override string ExitStamp => "CHECKED OUT";
    public override string ExitLine => "RELIEVED OF HIS POST";
    public override string LastWord => "HOLLOW";
    public override string LastLine => "“THE HOUSE REGRETS ANY INCONVENIENCE.”";
    public override string BeatPhase => "ACCOMMODATION";
    public override string BeatWord => "ACCOMMODATED";

    const float SweepTell = 0.55f, SweepSwing = 1.1f, SweepArc = 200f, SweepBand = 0.125f;
    const float BellLife = 0.95f, BellTell = 0.55f, LashLife = 0.5f, LashReach = 6f;
    const float GreetRecover = 0.8f;
    const float DeskRadius = 6.5f;

    float nextVolleyAt, nextSweepAt, nextBellAt, nextPullAt, nextGreetAt, nextHandshakeAt;
    float staggerUntil = float.NegativeInfinity;
    float lastCounterAt = float.NegativeInfinity;
    float transitionUntil;
    float subtitleAt = float.PositiveInfinity;
    Vector3 standPos;

    // The attack in progress
    float attackStart;
    bool attackHitDone;
    Vector3 slamTarget;
    float restUntil;

    // The red things: sweep blade, bell ring, lash ring, slam disc
    Transform sweepPivot;
    MeshRenderer sweepBlade;
    float sweepStart = float.NegativeInfinity, sweepFrom, sweepHeight;
    bool sweepHit, sweepTelegraphed;
    Transform bellRing, lashRing, slamDisc;
    float bellStart = float.NegativeInfinity, lashStart = float.NegativeInfinity;
    bool bellHit, lashHit;
    Vector3 lashCenter;
    float bellPulse;
    Vector3 bellScale;

    Material redMat, dimMat, goldMat;
    readonly List<Renderer> buttons = new List<Renderer>();
    readonly List<Hologram> holograms = new List<Hologram>();
    Transform vines;
    float idlePhase, armRaise, flinch, coatOpen;
    Vector3 smileRest = Vector3.one;
    BossOutro outro;
    BossTell tell;                        // the reach, the fuse and the light: every attack announces itself through it
    int attackTell, sweepTellId, bellTellId, lashTellId;
    bool bellPending;

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
        ButtonsLeft = ButtonCount;
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        redMat = new Material(unlit); redMat.SetColor("_BaseColor", new Color(2.6f, 0.2f, 0.12f));
        dimMat = new Material(unlit); dimMat.SetColor("_BaseColor", new Color(0.9f, 0.25f, 0.2f));
        goldMat = new Material(lit); goldMat.SetColor("_BaseColor", new Color(0.78f, 0.58f, 0.25f)); goldMat.SetFloat("_Smoothness", 0.7f); goldMat.SetFloat("_Metallic", 0.9f); // brass, as the buttons are
        body.SetWeakspotOpen(false);
        CloseCuffs();
        if (body.BossButtons != null) buttons.AddRange(body.BossButtons);
        if (body.BossSmile != null) smileRest = body.BossSmile.localScale;
        foreach (var h in FindObjectsByType<Hologram>(FindObjectsSortMode.None))
            if (Vector3.Distance(h.transform.position, arenaCenter) < arenaRadius * 2.2f) holograms.Add(h);
        if (bell != null) bellScale = bell.localScale;
        outro = gameObject.AddComponent<BossOutro>(); // his exit; it reads nothing until OnDied hands over
        outro.boss = this;

        sweepPivot = new GameObject("SweepBlade").transform;
        var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade.name = "Blade";
        Object.DestroyImmediate(blade.GetComponent<Collider>());
        blade.transform.SetParent(sweepPivot, false);
        blade.transform.localPosition = new Vector3(0f, 0f, config.bossSweepLength * 0.5f);
        blade.transform.localScale = new Vector3(1.2f, SweepBand * 2f, config.bossSweepLength);
        sweepBlade = blade.GetComponent<MeshRenderer>();
        sweepBlade.sharedMaterial = redMat;
        sweepPivot.gameObject.SetActive(false);
        bellRing = MakeRing("BellRing");
        lashRing = MakeRing("LashRing");
        slamDisc = MakeRing("HandshakeDisc");
        tell = new BossTell(transform);

        SetRise(0f); // under the desk until the intro raises him
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

    // ------------------------------------------------------------ the fight's shape

    /// 0 = sunk under the floor, 1 = standing: the intro raises him
    public override void SetRise(float k)
    {
        if (body != null) body.transform.position = standPos + Vector3.down * ((1f - k) * body.Scale * 2.6f);
    }

    public override void SealEntry() { if (entrySeal != null) entrySeal.SetActive(true); }
    public override void BeginIntro() { state = State.Intro; }

    /// The cutscene ends: the fight is on, and the door behind you is where you come back to
    public override void Engage()
    {
        SetRise(1f);
        state = State.Phase1;
        EngagedAt = Time.time;
        GameBootstrap.Checkpoint = checkpoint;
        if (servingText != null) servingText.text = "NOW SERVING   YOU";
        ArmTimers(Time.time);
        if (BossMusic.Instance != null) BossMusic.Instance.Transition(BossMusic.Section.Drop);
    }

    void ArmTimers(float now)
    {
        nextVolleyAt = now + 1.4f;
        nextSweepAt = now + 5f;
        nextBellAt = now + 8f;
        nextPullAt = now + 1.5f;
        nextGreetAt = now + 2.5f;
        nextHandshakeAt = now + 4f;
    }

    /// Downed mid-fight: the buttons go back on, the coat closes, and he starts over
    void OnPlayerRespawned()
    {
        if (!Engaged || body == null) return;
        body.RestoreHealth();
        DisplayHealth01 = 1f;
        ButtonsLeft = ButtonCount;
        foreach (var r in buttons) if (r != null) r.enabled = true;
        if (vines != null) { Destroy(vines.gameObject); vines = null; }
        coatOpen = 0f;
        body.SetEyes(null);
        if (body.BossSmile != null) body.BossSmile.localScale = smileRest;
        CloseThroat();
        CancelAttack();
        HideAttacks();
        state = State.Phase1;
        ArmTimers(Time.time + 1.5f);
        if (BossMusic.Instance != null) BossMusic.Instance.Transition(BossMusic.Section.Drop);
    }

    void HideAttacks()
    {
        if (sweepPivot != null) sweepPivot.gameObject.SetActive(false);
        if (bellRing != null) bellRing.gameObject.SetActive(false);
        if (lashRing != null) lashRing.gameObject.SetActive(false);
        if (slamDisc != null) slamDisc.gameObject.SetActive(false);
    }

    void Update()
    {
        float now = Time.time, dt = Time.deltaTime;
        if (state == State.Dead) { TickDead(now); return; }
        if (body == null || config == null) return;
        if (!body.Alive) { OnDied(now); return; }

        Vector3 player = PlayerChest();
        if (state != State.Dormant) FacePlayer(player, dt);
        Animate(player, dt);
        tell?.Tick(now, floorY);
        if (Time.unscaledTime - LastHitTime > 0.35f)
            DisplayHealth01 = Mathf.MoveTowards(DisplayHealth01, Health01, dt * 0.35f);
        if (!Engaged) return;

        if (ThroatOpen && now >= staggerUntil)
        {
            CloseThroat();
            Lash(player); // the window closed on nothing: the vines answer
        }
        StaggerLeft = ThroatOpen ? Mathf.Max(0f, staggerUntil - now) : 0f;

        Vector3 flat = player - body.transform.position; flat.y = 0f;
        float dist = flat.magnitude;
        if (state == State.Transition)
        {
            if (now >= transitionUntil) EnterPhase2();
        }
        else if (!ThroatOpen)
        {
            float speed = state == State.Phase2 ? Mathf.Max(0.5f, config.bossPhase2Speed) : 1f;
            if (attack != Attack.None) TickAttack(player, dist, now, speed);
            else if (now >= nextGreetAt && dist <= config.bossGreetRange) StartGreeting();
            else if (now >= nextHandshakeAt && dist > 4f && dist <= config.bossHandshakeRange) StartHandshake(player);
            else
            {
                if (now >= nextVolleyAt) { Volley(player); nextVolleyAt = now + config.bossVolleyInterval / speed; }
                if (now >= nextSweepAt && dist > DeskRadius + 1f && now - sweepStart > SweepTell + SweepSwing + 0.5f) { StartSweep(player); nextSweepAt = now + config.bossSweepInterval / speed; }
                if (now >= nextBellAt) { StartBell(); nextBellAt = now + config.bossBellInterval / speed; }
                if (state == State.Phase2 && now >= nextPullAt) { Pull(player); nextPullAt = now + config.bossPullInterval; }
            }
        }
        TickSweep(player, now);
        TickBell(player, now);
        TickLash(player, now);
    }

    PlayerMotor Motor => CombatDirector.Instance != null ? CombatDirector.Instance.motor : null;

    Vector3 PlayerChest()
    {
        var m = Motor;
        return m != null ? m.CurrPosition + Vector3.up * 0.9f : arenaCenter;
    }

    void FacePlayer(Vector3 player, float dt)
    {
        if (ThroatOpen) return; // bowed: he doesn't track
        Vector3 flat = player - body.transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.01f) return;
        body.transform.rotation = Quaternion.RotateTowards(body.transform.rotation, Quaternion.LookRotation(flat), 110f * dt);
    }

    // ------------------------------------------------------------ the body

    void Animate(Vector3 player, float dt)
    {
        var torso = body.BossTorso;
        if (torso == null) return;
        idlePhase += dt;
        float now = Time.time;
        float breathe = Mathf.Sin(idlePhase * 1.4f) * 0.6f;
        bool greetWindup = attack == Attack.Greeting && !attackHitDone;
        bool greetClap = attack == Attack.Greeting && attackHitDone && now - attackStart < config.bossGreetWindup + 0.35f;
        float pitch = ThroatOpen ? 78f : greetWindup ? -16f : greetClap ? 22f : 0f;
        pitch += breathe + 12f * flinch;
        flinch = Mathf.MoveTowards(flinch, 0f, dt * 3f);
        torso.localRotation = Quaternion.Slerp(torso.localRotation, Quaternion.Euler(pitch, 0f, 0f), 1f - Mathf.Exp(-(ThroatOpen ? 10f : 6f) * dt));

        // The head looks down at you; bowed, it hangs
        var head = body.BossHead;
        if (head != null)
        {
            Vector3 toPlayer = player - head.position;
            float flatD = new Vector2(toPlayer.x, toPlayer.z).magnitude;
            float look = Mathf.Clamp(Mathf.Atan2(-toPlayer.y, Mathf.Max(0.5f, flatD)) * Mathf.Rad2Deg, -10f, 40f);
            head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(ThroatOpen ? 25f : look, 0f, 0f), 1f - Mathf.Exp(-5f * dt));
        }

        // The coat opens on the vines in phase two
        coatOpen = Mathf.MoveTowards(coatOpen, state == State.Transition || state == State.Phase2 ? 1f : 0f, dt * 1.2f);
        if (body.BossCoatL != null) body.BossCoatL.localRotation = Quaternion.Euler(0f, 105f * coatOpen, 0f);
        if (body.BossCoatR != null) body.BossCoatR.localRotation = Quaternion.Euler(0f, -105f * coatOpen, 0f);

        // The arms: clasped in front at rest, spread for the greeting, one stretched for the handshake
        armRaise = Mathf.MoveTowards(armRaise, 0f, dt * 1.5f);
        Vector3 dirL = new Vector3(0.3f, -0.95f, 0.3f), dirR = new Vector3(-0.3f, -0.95f, 0.3f);
        float rateL = 4f, rateR = 4f, extendR = 0f;
        if (ThroatOpen)
        {
            dirL = new Vector3(-0.05f, -1f, 0.1f); dirR = new Vector3(0.05f, -1f, 0.1f); rateL = rateR = 5f;
        }
        else if (attack == Attack.Greeting)
        {
            if (greetWindup) { dirL = new Vector3(-1f, -0.12f, 0.22f); dirR = new Vector3(1f, -0.12f, 0.22f); rateL = rateR = 5f; } // flung wide, shoulder height
            else if (greetClap) { dirL = new Vector3(0.2f, -0.5f, 0.95f); dirR = new Vector3(-0.2f, -0.5f, 0.95f); rateL = rateR = 24f; }
            else rateL = rateR = 5f;
        }
        else if (attack == Attack.Handshake)
        {
            float t = now - attackStart;
            if (t < config.bossHandshakeTelegraph) { dirR = new Vector3(0.2f, 1f, 0.45f); rateR = 7f; } // the hand raised right over his head
            else if (now < restUntil + 0.5f)
            {
                Vector3 shoulder = body.BossArmR != null ? body.BossArmR.position : body.Center;
                Vector3 toTarget = slamTarget - shoulder;
                dirR = torso.InverseTransformDirection(toTarget.normalized);
                extendR = Mathf.Clamp(toTarget.magnitude / (1.04f * body.Scale) - 1f, 0f, 3.2f);
                rateR = 30f;
            }
            else rateR = 6f;
        }
        else if (armRaise > 0f)
        {
            dirR = torso.InverseTransformDirection((player - (body.BossArmR != null ? body.BossArmR.position : body.Center)).normalized);
            rateR = 10f;
        }
        PoseArm(body.BossArmL, dirL, rateL, 0f, dt);
        PoseArm(body.BossArmR, dirR, rateR, extendR, dt);

        // The bell rocks when it's rung
        if (bell != null && bellPulse > 0f)
        {
            bellPulse = Mathf.MoveTowards(bellPulse, 0f, dt * 2.5f);
            float s = 1f + 0.35f * Mathf.Sin(bellPulse * 30f) * bellPulse;
            bell.localScale = new Vector3(bellScale.x * s, bellScale.y * (2f - s), bellScale.z * s);
        }
    }

    static void PoseArm(Transform arm, Vector3 localDir, float rate, float extend, float dt)
    {
        if (arm == null) return;
        Quaternion target = Quaternion.FromToRotation(Vector3.down, localDir.normalized);
        arm.localRotation = Quaternion.Slerp(arm.localRotation, target, 1f - Mathf.Exp(-rate * dt));
        float scaleY = Mathf.Lerp(arm.localScale.y, 1f + extend, 1f - Mathf.Exp(-(extend > 0f ? 18f : 6f) * dt));
        arm.localScale = new Vector3(1f, scaleY, 1f);
    }

    // ------------------------------------------------------------ attacks

    Vector3 Muzzle => body.BossMuzzle != null ? body.BossMuzzle.position : body.Center;

    void Volley(Vector3 player)
    {
        armRaise = 1f;
        Vector3 from = Muzzle;
        var motor = Motor;
        Vector3 lead = motor != null ? motor.Velocity * 0.35f : Vector3.zero;
        Vector3 aim = (player + lead - from).normalized;
        int shots = Mathf.Max(1, config.bossVolleyShots);
        for (int i = 0; i < shots; i++)
        {
            float spread = shots == 1 ? 0f : Mathf.Lerp(-12f, 12f, i / (float)(shots - 1));
            Vector3 dir = Quaternion.AngleAxis(spread, Vector3.up) * aim;
            EnemyProjectile.Spawn(config, from + dir * 0.8f, dir, body);
        }
        if (GameFeel.Instance != null) GameFeel.Instance.PlayEnemyShot();
    }

    /// The announcement: slow, big, gold — and it drags you in unless you parry it
    void Pull(Vector3 player)
    {
        armRaise = 1f;
        Vector3 from = Muzzle;
        Vector3 dir = (player - from).normalized;
        var orb = EnemyProjectile.Spawn(config, from + dir * 0.8f, dir, body);
        orb.Configure(config.bossPullSpeed, 2.4f, true, config.bossPullImpulse);
        if (GameFeel.Instance != null) GameFeel.Instance.PlayParryWarn();
    }

    /// Both arms open wide — gold: parry the clap — and the cuffs show while they are spread
    void StartGreeting()
    {
        attack = Attack.Greeting;
        attackStart = Time.time;
        attackHitDone = false;
        body.SetCoatSkin(Enemy.TelegraphGold());
        body.SetWindup(Time.time + config.bossGreetWindup);
        attackTell = Tell(BossTell.Kind.Gold, config.bossGreetWindup / PhaseSpeed, Feet, config.bossGreetRange, body.BossHandR, body.transform, 0f, body.BossHandL);
        OpenCuffs();
        if (GameFeel.Instance != null) GameFeel.Instance.PlayBossGreetWindup();
    }

    /// The handshake: a red disc where the hand will land; then the hand rests there, cuff open
    void StartHandshake(Vector3 player)
    {
        attack = Attack.Handshake;
        attackStart = Time.time;
        attackHitDone = false;
        slamTarget = new Vector3(player.x, floorY, player.z);
        var motor = Motor;
        if (motor != null)
        {
            slamTarget += new Vector3(motor.Velocity.x, 0f, motor.Velocity.z) * 0.25f;
            float feet = player.y - 0.9f;
            if (feet > floorY + 0.8f) slamTarget.y = feet; // on the desk or a step: it comes down there
        }
        slamDisc.gameObject.SetActive(true);
        slamDisc.position = slamTarget + Vector3.up * 0.12f;
        slamDisc.localScale = new Vector3(0.5f, 0.06f, 0.5f);
        slamDisc.GetComponent<MeshRenderer>().sharedMaterial = dimMat;
        body.SetCoatSkin(redMat);
        attackTell = Tell(BossTell.Kind.Red, config.bossHandshakeTelegraph, slamTarget, config.bossHandshakeRadius, body.BossHandR, null);
        if (GameFeel.Instance != null) GameFeel.Instance.PlayBossReach();
    }

    void TickAttack(Vector3 player, float dist, float now, float speed)
    {
        float t = now - attackStart;
        var motor = Motor;
        if (attack == Attack.Greeting)
        {
            float windup = config.bossGreetWindup / speed;
            if (t < windup)
            {
                // Parry the clap while it's coming: a COUNTER — he staggers and bows
                if (motor != null && dist <= config.bossGreetRange + 2f && motor.ParryWindowOpen && motor.TryConsumeParry(NearestHand(player), "COUNTER!"))
                {
                    OnCountered();
                    if (GameFeel.Instance != null) GameFeel.Instance.PlayBossCounter();
                }
                return;
            }
            if (!attackHitDone)
            {
                attackHitDone = true;
                body.SetCoatSkin(null);
                CloseCuffs();
                tell?.End(attackTell);
                flinch = 0.6f;
                if (GameFeel.Instance != null) GameFeel.Instance.PlayBossClap();
                float feet = player.y - 0.9f;
                Vector3 flat = player - body.transform.position; flat.y = 0f;
                float facing = Vector3.Dot(flat.normalized, body.transform.forward);
                if (dist <= config.bossGreetRange && facing > Mathf.Cos(80f * Mathf.Deg2Rad) && feet < floorY + 3f
                    && motor != null && !motor.HasIFrames && CombatDirector.Instance != null)
                    CombatDirector.Instance.ReportPlayerHit(flat.normalized, config.bossGreetDamage);
            }
            if (t >= windup + GreetRecover) EndAttack();
            return;
        }

        // The handshake
        float tele = config.bossHandshakeTelegraph;
        if (t < tele)
        {
            float k = t / tele;
            float r = Mathf.Lerp(0.5f, config.bossHandshakeRadius, k);
            slamDisc.localScale = new Vector3(r * 2f, 0.06f, r * 2f);
            if (k > 0.6f) slamDisc.GetComponent<MeshRenderer>().sharedMaterial = redMat;
            return;
        }
        if (!attackHitDone)
        {
            attackHitDone = true;
            slamDisc.gameObject.SetActive(false);
            body.SetCoatSkin(null);
            tell?.End(attackTell);
            flinch = 0.8f;
            if (GameFeel.Instance != null) GameFeel.Instance.PlayBossSlam();
            Vector3 flat = player - slamTarget; flat.y = 0f;
            float feet = player.y - 0.9f;
            if (flat.magnitude <= config.bossHandshakeRadius && feet < slamTarget.y + 2.2f && motor != null && !motor.HasIFrames && CombatDirector.Instance != null)
            {
                Vector3 away = player - body.transform.position; away.y = 0f;
                CombatDirector.Instance.ReportPlayerHit(away.normalized, config.bossHandshakeDamage);
                motor.AddImpulse(Vector3.up * 12f + away.normalized * 5f); // escorted off the premises
            }
            restUntil = now + config.bossHandshakeRest;
            body.SetWeakspot(body.BossCuffR, true);
            OpenSpot = "CUFF";
        }
        if (now >= restUntil)
        {
            if (OpenSpot == "CUFF") { body.SetWeakspot(body.BossCuffR, false); OpenSpot = ""; }
            if (now >= restUntil + 0.5f) EndAttack();
        }
    }

    float PhaseSpeed => state == State.Phase2 ? Mathf.Max(0.5f, config.bossPhase2Speed) : 1f;

    /// Where he stands, on the floor
    Vector3 Feet { get { Vector3 p = body.transform.position; p.y = floorY; return p; } }

    /// The announcement: gold to answer, red to move; where it lands, how far it reaches, what to watch
    int Tell(BossTell.Kind k, float seconds, Vector3 centre, float reach, Transform focus, Transform follow, float ahead = 0f, Transform focus2 = null)
        => tell != null ? tell.Begin(k, seconds, centre, reach, focus, follow, ahead, focus2) : 0;

    Vector3 NearestHand(Vector3 player)
    {
        Vector3 l = body.BossHandL != null ? body.BossHandL.position : body.Center;
        Vector3 r = body.BossHandR != null ? body.BossHandR.position : body.Center;
        return Vector3.Distance(player, l) < Vector3.Distance(player, r) ? l : r;
    }

    void EndAttack()
    {
        float now = Time.time;
        if (attack == Attack.Greeting) nextGreetAt = now + config.bossGreetCooldown;
        if (attack == Attack.Handshake) nextHandshakeAt = now + config.bossHandshakeCooldown;
        attack = Attack.None;
        attackHitDone = false;
        body.SetWindup(0f);
        tell?.End(attackTell);
    }

    /// A cuff cut or a counter ends the attack now: the arm recoils and the cuffs seal
    void CancelAttack()
    {
        if (attack == Attack.None) return;
        if (attack == Attack.Greeting) nextGreetAt = Time.time + config.bossGreetCooldown;
        if (attack == Attack.Handshake) nextHandshakeAt = Time.time + config.bossHandshakeCooldown;
        attack = Attack.None;
        attackHitDone = false;
        restUntil = 0f;
        tell?.End(attackTell);
        body.SetWindup(0f);
        body.SetCoatSkin(null);
        CloseCuffs();
        if (slamDisc != null) slamDisc.gameObject.SetActive(false);
    }

    void OpenCuffs()
    {
        body.SetWeakspot(body.BossCuffL, true);
        body.SetWeakspot(body.BossCuffR, true);
        if (!ThroatOpen) OpenSpot = "CUFFS";
    }

    void CloseCuffs()
    {
        body.SetWeakspot(body.BossCuffL, false);
        body.SetWeakspot(body.BossCuffR, false);
        if (OpenSpot == "CUFFS" || OpenSpot == "CUFF") OpenSpot = ThroatOpen ? "THROAT" : "";
    }

    void StartSweep(Vector3 player)
    {
        sweepStart = Time.time;
        sweepHit = false;
        sweepTelegraphed = false;
        float feet = player.y - 0.9f;
        // Knee height: over it or under it. Phase two brings it up to wherever you stand.
        sweepHeight = floorY + 1.175f;
        if (state == State.Phase2) sweepHeight = Mathf.Max(sweepHeight, feet + 1.175f);
        Vector3 flat = player - body.transform.position; flat.y = 0f;
        float toPlayer = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
        sweepFrom = toPlayer - SweepArc * 0.5f; // it starts behind you and comes round through you
        sweepPivot.gameObject.SetActive(true);
        sweepPivot.position = new Vector3(body.transform.position.x, sweepHeight, body.transform.position.z);
        sweepPivot.rotation = Quaternion.Euler(0f, sweepFrom, 0f);
        sweepBlade.sharedMaterial = dimMat; // the tell
        sweepTellId = Tell(BossTell.Kind.Red, SweepTell, Feet, config.bossSweepLength, null, body.transform);
    }

    void TickSweep(Vector3 player, float now)
    {
        if (!sweepPivot.gameObject.activeSelf) return;
        float t = now - sweepStart;
        sweepPivot.position = new Vector3(body.transform.position.x, sweepHeight, body.transform.position.z);
        if (t < SweepTell) return;
        if (!sweepTelegraphed)
        {
            sweepTelegraphed = true;
            sweepBlade.sharedMaterial = redMat;
            tell?.End(sweepTellId);
            if (GameFeel.Instance != null) GameFeel.Instance.PlayBossSweep();
        }
        float k = (t - SweepTell) / SweepSwing;
        if (k >= 1f) { sweepPivot.gameObject.SetActive(false); return; }
        float prevAngle = sweepFrom + SweepArc * Mathf.Clamp01(k - Time.deltaTime / SweepSwing);
        float angle = sweepFrom + SweepArc * k;
        sweepPivot.rotation = Quaternion.Euler(0f, angle, 0f);
        if (sweepHit) return;

        var motor = Motor;
        if (motor == null || motor.HasIFrames || CombatDirector.Instance == null) return;
        Vector3 flat = player - body.transform.position; flat.y = 0f;
        float dist = flat.magnitude;
        if (dist > config.bossSweepLength || dist < DeskRadius + 0.5f) return; // the desk is between you and it
        float feet = player.y - 0.9f;
        float top = feet + (motor.Sliding ? config.slideCapsuleHeight : 1.8f);
        if (feet > sweepHeight + SweepBand || top < sweepHeight - SweepBand) return;
        float pAng = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
        float da = Mathf.DeltaAngle(prevAngle, pAng), db = Mathf.DeltaAngle(prevAngle, angle);
        if (da < -2f || da > db + 2f) return;
        sweepHit = true;
        Vector3 push = Quaternion.Euler(0f, angle + 90f, 0f) * Vector3.forward;
        CombatDirector.Instance.ReportPlayerHit(push.normalized, config.bossSweepDamage);
    }

    /// He rings for service: the bell, and a red ring out across the floor from the desk
    void StartBell()
    {
        bellStart = Time.time + BellTell;   // it rings when the tell closes
        bellPending = true;
        bellHit = false;
        bellTellId = Tell(BossTell.Kind.Red, BellTell, Feet, config.bossBellRadius, bell, body.transform);
    }

    void TickBell(Vector3 player, float now)
    {
        if (bellPending)
        {
            if (now < bellStart) return;
            bellPending = false;
            bellPulse = 1f;
            bellRing.gameObject.SetActive(true);
            tell?.End(bellTellId);
            if (GameFeel.Instance != null) GameFeel.Instance.PlayBossBell();
        }
        if (!bellRing.gameObject.activeSelf) return;
        float k = (now - bellStart) / BellLife;
        if (k >= 1f) { bellRing.gameObject.SetActive(false); return; }
        float r = Mathf.Lerp(DeskRadius, config.bossBellRadius, k);
        bellRing.position = new Vector3(body.transform.position.x, floorY + 0.15f, body.transform.position.z);
        bellRing.localScale = new Vector3(r * 2f, 0.06f, r * 2f);
        if (bellHit) return;
        var motor = Motor;
        if (motor == null || !motor.Grounded || motor.HasIFrames || CombatDirector.Instance == null) return; // in the air: it passes under you
        if (player.y - 0.9f > floorY + 1.0f) return; // on the desk or a step: it passes below you
        Vector3 flat = player - body.transform.position; flat.y = 0f;
        float dist = flat.magnitude;
        if (Mathf.Abs(dist - r) > 1.6f) return;
        bellHit = true;
        CombatDirector.Instance.ReportPlayerHit(flat.normalized, config.bossBellDamage);
    }

    void Lash(Vector3 player)
    {
        lashStart = Time.time;
        lashHit = false;
        lashCenter = new Vector3(player.x, player.y - 0.9f + 0.15f, player.z);
        lashRing.gameObject.SetActive(true);
        lashRing.position = lashCenter;
        lashRing.localScale = new Vector3(0.5f, 0.06f, 0.5f);
        lashTellId = Tell(BossTell.Kind.Red, LashLife * 0.35f, lashCenter, LashReach, null, null);
        if (GameFeel.Instance != null) GameFeel.Instance.PlayChaserStrike();
    }

    void TickLash(Vector3 player, float now)
    {
        if (!lashRing.gameObject.activeSelf) return;
        float k = (now - lashStart) / LashLife;
        if (k >= 1f) { lashRing.gameObject.SetActive(false); return; }
        float r = Mathf.Lerp(0.5f, LashReach, k);
        lashRing.localScale = new Vector3(r * 2f, 0.06f, r * 2f);
        if (lashHit || k < 0.35f) return; // it winds up before it bites: move
        tell?.End(lashTellId);
        var motor = Motor;
        if (motor == null || motor.HasIFrames || CombatDirector.Instance == null) return;
        Vector3 flat = player - lashCenter; flat.y = 0f;
        if (flat.magnitude > r || player.y - 0.9f > lashCenter.y + 1.3f) return;
        lashHit = true;
        CombatDirector.Instance.ReportPlayerHit(flat.sqrMagnitude > 0.01f ? flat.normalized : Vector3.forward, config.bossLashDamage);
    }

    // ------------------------------------------------------------ being hurt

    /// The body asks before taking damage; every answer shows on the bar.
    /// Slashes chip. Reflected shots bite and pop a button (phase one) or stagger
    /// (phase two). A counter staggers. Executes bite by the spot: the throat
    /// deep, a cuff less — and a cut cuff ends the attack it was part of.
    public override bool FilterDamage(ref float amount, KillMethod method)
    {
        if (!Engaged) return false;
        string label;
        switch (method)
        {
            case KillMethod.SlamShockwave:
                amount = config.bossSlashDamage;
                label = "SLAM";
                break;
            case KillMethod.Fist:   // a punch chips like a slash
                amount = config.bossSlashDamage;
                label = "JAB";
                break;
            case KillMethod.Tackle: // a body at speed lands like a slam
                amount = config.bossSlashDamage;
                label = "TACKLE";
                break;
            case KillMethod.Cleave:
                amount = config.bossSlashDamage * 3f;
                label = "CLEAVE";
                break;
            case KillMethod.ReflectedProjectile:
                amount = config.bossReflectDamage;
                label = "PARRY";
                if (state == State.Phase1) PopButton();
                else if (state == State.Phase2 && !ThroatOpen) Stagger();
                break;
            case KillMethod.MeleeParry:
                amount = config.bossCounterDamage;
                label = "COUNTER";
                if (!ThroatOpen) Stagger();
                break;
            case KillMethod.SwordExecute:
            {
                Renderer spot = NearestOpenSpot(Fists.LastStrikePoint);
                if (spot != null && spot == body.BossThroat)
                {
                    amount = config.bossThroatDamage;
                    label = "EXECUTE";
                    CloseThroat();
                }
                else if (spot != null)
                {
                    amount = config.bossCuffDamage;
                    label = "CUFF CUT";
                    CancelAttack();
                }
                else
                {
                    amount = config.bossSlashDamage;
                    label = "SLASH";
                }
                flinch = 1f;
                if (GameFeel.Instance != null) GameFeel.Instance.PlayBossHit();
                break;
            }
            default:
                if (GameFeel.Instance != null) GameFeel.Instance.PlayShellClank(0.6f); // he doesn't even notice: porcelain, not a pail
                return false;
        }
        RegisterHit(amount, label);
        return true;
    }

    void RegisterHit(float amount, string label)
    {
        HitsTaken++;
        LastHitTime = Time.unscaledTime;
        LastHitFraction = config.bossHealth > 0f ? amount / config.bossHealth : 0f;
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
        Consider(body.BossThroat, ThroatOpen);
        Consider(body.BossCuffL, body.WeakspotIsOpen(body.BossCuffL));
        Consider(body.BossCuffR, body.WeakspotIsOpen(body.BossCuffR));
        return best;
    }

    /// A parry landed on his greeting (from here, or the director's melee parry near his body)
    public override void OnCountered()
    {
        if (!Engaged || Time.time - lastCounterAt < 0.5f) return; // one counter per parry, whichever path reports it
        lastCounterAt = Time.time;
        Counters++;
        if (attack == Attack.Greeting) CancelAttack();
        float dmg = config.bossCounterDamage;
        if (!ThroatOpen) Stagger();
        body.Damage(dmg, KillMethod.MeleeParry); // filtered above: the counter's bite, on the bar
    }

    void PopButton()
    {
        if (ButtonsLeft <= 0) return;
        ButtonsLeft--;
        int idx = ButtonCount - 1 - ButtonsLeft;
        if (idx >= 0 && idx < buttons.Count && buttons[idx] != null)
        {
            var r = buttons[idx];
            r.enabled = false;
            var shard = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shard.name = "Button";
            shard.layer = 2;
            shard.transform.position = r.transform.position;
            shard.transform.localScale = r.transform.lossyScale;
            shard.GetComponent<MeshRenderer>().sharedMaterial = goldMat;
            var rb = shard.AddComponent<Rigidbody>();
            rb.mass = 2f;
            rb.linearVelocity = body.transform.forward * 7f + Vector3.up * 5f + Random.insideUnitSphere * 2f;
            rb.angularVelocity = Random.insideUnitSphere * 8f;
            shard.AddComponent<ShardFade>();
        }
        if (GameFeel.Instance != null) GameFeel.Instance.PlayBossButton();
        if (ButtonsLeft == 0) BeginTransition();
    }

    /// The last button goes: the coat swings open on the vines, the eyes go red, the smile widens
    void BeginTransition()
    {
        state = State.Transition;
        transitionUntil = Time.time + 2.6f;
        CancelAttack();
        HideAttacks();
        body.SetEyes(Enemy.EyeRed());
        if (body.BossSmile != null) body.BossSmile.localScale = new Vector3(smileRest.x * 1.35f, smileRest.y * 1.15f, smileRest.z);
        var torso = body.BossTorso;
        if (torso != null)
        {
            // Vines from the waist up into the chest — under an unscaled root, so the body's scale never stretches them
            vines = new GameObject("ConciergeVines").transform;
            float s = body.Scale;
            Vector3 waist = body.transform.position + Vector3.up * (1.05f * s);
            for (int i = 0; i < 3; i++)
            {
                float a = (i * 120f - 60f) * Mathf.Deg2Rad;
                Vector3 from = waist + body.transform.rotation * new Vector3(Mathf.Sin(a) * 0.16f * s, -0.15f * s, Mathf.Cos(a) * 0.08f * s);
                Vector3 to = torso.position + torso.rotation * new Vector3(Mathf.Sin(a) * 0.1f * s, (0.25f + 0.08f * i) * s, 0.13f * s);
                LevelKit.Tendril(vines, from, to, 0.06f * s, 7);
            }
        }
        if (GameFeel.Instance != null) GameFeel.Instance.PlayBossCrack();
        if (BossMusic.Instance != null) BossMusic.Instance.Transition(BossMusic.Section.Breakdown);
    }

    void EnterPhase2()
    {
        state = State.Phase2;
        float now = Time.time;
        nextPullAt = now + 1.5f;
        nextVolleyAt = now + 2.5f;
        nextSweepAt = now + 4f;
        nextBellAt = now + 6f;
        nextGreetAt = now + 3f;
        nextHandshakeAt = now + 5f;
        if (BossMusic.Instance != null) BossMusic.Instance.Transition(BossMusic.Section.Climax);
    }

    /// He bows: the throat opens behind the bow tie, in reach from the counter
    void Stagger()
    {
        WindowOpen = true;
        staggerUntil = Time.time + config.bossStaggerWindow;
        body.SetWeakspotOpen(true);
        OpenSpot = "THROAT";
        if (attack != Attack.None) CancelAttack();
        body.SetCoatSkin(null);
        if (GameFeel.Instance != null) GameFeel.Instance.PlayBossStagger();
    }

    void CloseThroat()
    {
        WindowOpen = false;
        StaggerLeft = 0f;
        if (body != null) body.SetWeakspotOpen(false);
        if (OpenSpot == "THROAT") OpenSpot = "";
    }

    // ------------------------------------------------------------ the end

    void OnDied(float now)
    {
        state = State.Dead;
        HideAttacks();
        CloseThroat();
        foreach (var p in new List<EnemyProjectile>(EnemyProjectile.Active)) if (p != null) Destroy(p.gameObject); // nothing in flight outlives him
        foreach (var h in holograms) if (h != null) h.Lock("HOLLOW", "ACCOMMODATED");
        if (servingText != null) servingText.text = "THANK YOU FOR STAYING";
        subtitleAt = float.PositiveInfinity; // the exit plays first; Dismissed() hands the last word back
        if (GameFeel.Instance != null) GameFeel.Instance.PlayBossKill();
        if (CombatDirector.Instance != null && CombatDirector.Instance.style != null)
            CombatDirector.Instance.style.AddBonus(config.bossStyleBonus);
        if (outro != null) outro.Begin();
        else { Fall(); Dismissed(); }
    }

    /// The exit's burst: the vines go, the music cuts dead, and the body comes apart from its bowed pose (the outro throws the pieces)
    public override GameObject Fall()
    {
        if (vines != null) { Destroy(vines.gameObject); vines = null; }
        if (BossMusic.Instance != null) BossMusic.Instance.Transition(BossMusic.Section.Tail);
        return body != null ? body.Collapse() : null;
    }

    /// The exit is over: the way out opens and the house has the last word
    public override void Dismissed()
    {
        if (exitSeal != null) exitSeal.SetActive(false);
        subtitleAt = Time.time + 0.8f;
    }

    void TickDead(float now)
    {
        if (now < subtitleAt) return;
        subtitleAt = float.PositiveInfinity;
        Subtitle = "CONGRATULATIONS. A ROOM HAS BEEN FOUND FOR YOU.";   // the House's last word: a guest who never checks out
        SubtitleUntil = now + 6f;
    }
}
