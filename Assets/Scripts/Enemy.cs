using System.Collections.Generic;
using UnityEngine;

public enum KillMethod { SlamShockwave, GrappleDagger, ReflectedProjectile, MeleeParry, Sword, SwordExecute, Cleave, Arrow, Blast, Fist, Tackle, Blunderbuss, Maul }

public enum EnemyKind { Dummy, Turret, Chaser, Boss, Janitor, Borer, Rigger, Blaster, Pursuit, Hound, Interceptor, Kettler, DamageControl, Sentry, Skitter, Marshal, Warden, Lurker, Moth, Ocular, Overseer, Scourer }

/// Marks an enemy's weakspot collider (the glowing cyan head): a sword strike
/// here is an instant kill.
public class EnemyWeakspot : MonoBehaviour { }

/// <summary>
/// Enemy roster, v6 (2026-10-16, the simplification: ENEMIES ARE PLATFORMS AND
/// FUEL, NOT FIGHTS). THE HOUSE'S STAFF are CLOCKWORK MANNEQUINS: shop-window
/// dummies given clockwork by the company (EnemyMannequin: enamel shells, brass
/// ball joints, white gloves, a wind-up key in the back, a glass head with the
/// escapement ticking inside), built from primitives (root at the feet, facing
/// local +Z). One shared skeleton: pelvis, torso pivot, head pivot, two arm
/// pivots, two leg pivots — animated procedurally, stiff and in clicks; the
/// livery (hats, the painted coat, props, the brass name plate) hangs under
/// those pivots, look only (EnemySuit, EnemyLivery). Hitboxes stay CLEAR: one
/// body capsule (normal damage) + the glowing cyan HEAD (the execute: the
/// mainspring in the jar). Every EnemyKind keeps its rig and its name; what it
/// DOES is one of four roles (EnemyRoles: footman = the platform, shooter = the
/// reason to move, bouncer = the wall, turret = the sniper as built) or the
/// practice dummy, mapped at the factory (RoleOf) so no level file changes.
/// A unit on a course is jabbed through (one jab ends a footman), kicked off
/// (a launch, and the springboard refreshes your double jump), pogoed on, or run
/// past. Nothing chases. Nothing is random: every timing is seeded from where
/// the unit was built. Death = the mannequin flies apart into gears and limbs;
/// it rebuilds on respawn (the range and the tutorial only).
/// </summary>
public partial class Enemy : MonoBehaviour
{
    public MovementConfig config;
    public EnemyKind kind;

    public bool Alive { get; private set; } = true;
    /// What finished it: the ragdoll is cut by it, whenever the body finally falls
    public KillMethod DeathMethod { get; private set; }

    /// Hub only: enemies come back on a timer for practice. In levels they stay
    /// dead for the run — only a level reset (Restart / re-entering, i.e. a scene
    /// rebuild) brings them back. Set per scene by GameBootstrap.
    public static bool RespawnEnabled = true;
    /// The tutorial's two per-unit switches. A dummy that holds its fire is a target and nothing else (the blade and
    /// bow rooms: one subject at a time); a unit that stays dead does so even where the rest come back, so a room
    /// that has been cleared STAYS cleared and nothing follows you into the next lesson.
    public bool HoldFire, StaysDead;
    /// Chest height — the aim point for shots, yanks and parries
    public Vector3 Center => transform.position + Vector3.up * (CentreHeight * transform.lossyScale.y);
    /// The low units carry their chest lower
    float CentreHeight => kind == EnemyKind.Skitter ? 0.7f : kind == EnemyKind.Lurker ? 0.55f : ChestHeight;
    /// Body scale (the boss is built at five times a steward)
    public float Scale => transform.lossyScale.y;
    public float Health01 => config != null && MaxHp > 0f ? Mathf.Clamp01(hp / MaxHp) : 0f;
    public void RestoreHealth() => hp = MaxHp;
    // The boss controller drives these directly
    public Transform BossTorso => rig?.torso;
    public Transform BossArmL => rig?.armL;
    public Transform BossArmR => rig?.armR;
    public Transform BossMuzzle => rig?.muzzle;
    public Transform BossHead => rig?.headPivot;
    public Renderer[] BossPlates => rig?.plates;
    public void SetPlateSkin(Material m) => SetSkin(m);
    bool weakspotClosed;
    /// A closed weakspot cannot be locked on or executed: its collider is off and it goes dark
    public void SetWeakspotOpen(bool open)
    {
        weakspotClosed = !open;
        if (rig == null || rig.head == null) return;
        var col = rig.head.GetComponent<Collider>();
        if (col != null) col.enabled = open && Alive;
        rig.head.sharedMaterial = open ? WeakMat() : SealedMat();
    }
    public bool IsWindingUp => Alive && windupEndsAt > Time.time;

    const float ChestHeight = 1.25f;
    static readonly Vector3 ArmRRest = new Vector3(0.3f, 0.32f, 0f);

    /// The procedural skeleton: pivots the animator drives, renderers the skins swap
    class Rig
    {
        public Transform torso, headPivot, armL, armR, legL, legR, ring, muzzle;
        public Renderer head, status;
        public Renderer[] plates;
        public Vector3 torsoRest;
        // The Concierge's extras
        public Renderer throat, cuffL, cuffR, badge, eyeL, eyeR;
        public Transform coatL, coatR, handL, handR, foreL, foreR, smile;
        public Renderer[] buttons, coat;
        // The Janitor's extras
        public Transform hatch, buffer;
        // The Works' extras: the Borer's drill and charge lane, the Rigger's boom and chain
        public Transform drill, boom, chain;
        public Renderer lane;
        // What the telegraph skins go back to (the Concierge's navy, the Foreman's orange and amber), and the Foreman's beacon
        public Material coatRest, eyeRest;
        public Transform beacon;
        // The Foreman's shield: the ring its plates ride on, and the band on the boiler that shows through the open side
        public Transform shield;
        public Renderer shieldBand;
        // The mannequin's clockwork (EnemyMannequin): the wind-up key in its back, the balance wheel in its glass head
        public Transform key, balance;
    }

    float hp;
    float respawnAt;
    float nextFireAt;
    float windupEndsAt;
    float staggeredUntil;
    float attackReadyAt;
    float strikeAt = -10f;
    float recoil, aimBlend, hitFlinch;
    float gait, gaitPhase, moveSpeed;
    float targetedUntil;
    bool targetedVisual;
    float corruptUntil;
    bool corruptVisual;
    Vector3 spawnPos;
    Rig rig;
    /// An authored clip over the procedural pose (Clips/EnemyClipDriver): made on the first EnemyClipDriver.Play, null otherwise
    [System.NonSerialized] public EnemyClipDriver clips;
    Renderer[] renderers;
    Collider[] colliders;
    Material normalMat;
    static Material chassisMat, jointMat, visorMat, statusMat, thrusterMat,
        windupMat, staggerMat, weakMat, ringMat, targetMat, corruptMat;

    public static Enemy CreateDummy(Vector3 groundPos, MovementConfig config)
        => Create(EnemyKind.Dummy, groundPos, config);

    /// Sentinels hover: `pos` is where the chest (the aim point) sits
    public static Enemy CreateTurret(Vector3 pos, MovementConfig config)
        => Create(EnemyKind.Turret, pos - Vector3.up * ChestHeight, config);

    public static Enemy CreateChaser(Vector3 groundPos, MovementConfig config)
        => Create(EnemyKind.Chaser, groundPos, config);

    /// Which boss body a Boss unit wears: the Concierge's (Chapter 1) or the Foreman's (Chapter 2)
    public enum BossKind { Concierge, Foreman }
    public BossKind bossKind;
    static BossKind nextBossKind;

    /// A boss: its own rig, built at `scale`; the boss controller (BossConcierge, BossForeman) drives it
    public static Enemy CreateBoss(Vector3 feet, MovementConfig config, float scale, BossKind which = BossKind.Concierge)
    {
        nextBossKind = which;
        var e = Create(EnemyKind.Boss, feet, config);
        e.bossKind = which;
        e.transform.localScale = Vector3.one * scale;
        return e;
    }

    /// A unit of `kind` stood at `feet`, made by the one factory (the level editor's copies of units come through here)
    public static Enemy Spawn(EnemyKind kind, Vector3 feet, MovementConfig config) => Create(kind, feet, config);

    /// The post it walks and comes back to is where it now stands (a hand edit moved it after it was made)
    public void Rehome() { spawnPos = transform.position; }

    /// NO UNITS (2026-10-26, user: "lets also remove the enemies"): the factory makes nothing but the bosses. Every
    /// placement in every level still asks, and gets null (Course.Fuel, the pool, the chase's footmen, the ranges
    /// and the tutorial all take a null); the bodies, the roles and the liveries stay in code for the day they return
    public static bool Units = false;

    static Enemy Create(EnemyKind kind, Vector3 feet, MovementConfig config)
    {
        if (!Units && kind != EnemyKind.Boss) return null;
        var root = new GameObject("Enemy" + kind);
        root.transform.position = feet;
        Material plate = kind == EnemyKind.Turret ? TurretPlate() : kind == EnemyKind.Chaser ? ChaserPlate() : kind == EnemyKind.Janitor ? JanitorPlate() : IsWorks(kind) ? WorksPlate() : IsResponse(kind) ? ResponsePlate() : IsVariety(kind) ? VarietyPlate(kind) : DummyPlate();
        Rig built = kind == EnemyKind.Boss ? (nextBossKind == BossKind.Foreman ? BuildForemanRig(root.transform) : BuildConciergeRig(root.transform)) : kind == EnemyKind.Janitor ? BuildJanitorRig(root.transform, plate) : IsWorks(kind) ? BuildWorksRig(root.transform, kind, plate) : IsResponse(kind) ? BuildResponseRig(root.transform, kind, plate) : IsVariety(kind) ? BuildVarietyRig(root.transform, kind, plate) : BuildRig(root.transform, kind, plate);
        Livery(root.transform, built, kind); // the House's livery over the rig: Look-only, under the pivots (EnemyLivery)

        // Component last: Awake caches the finished rig's renderers and colliders
        var enemy = root.AddComponent<Enemy>();
        enemy.config = config;
        enemy.kind = kind;
        enemy.normalMat = plate;
        enemy.rig = built;
        enemy.role = RoleOf(kind); // THE MAPPING: the kind keeps its body and its name, the role is what it does (EnemyRoles)
        enemy.Settle(); // on the ground, clear of solids, clear of everyone already here
        return enemy;
    }

    // ---------------------------------------------------------------- rig

    static Rig BuildRig(Transform root, EnemyKind kind, Material plate)
    {
        var rig = new Rig();
        var plates = new List<Renderer>();

        // Hitbox: one clear body capsule (shins to shoulders); the head is its own collider
        var hitbox = new GameObject("Hitbox");
        hitbox.transform.SetParent(root, false);
        var capsule = hitbox.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 0.82f, 0f);
        capsule.radius = 0.3f;
        capsule.height = 1.3f;

        // THE MANNEQUIN (EnemyMannequin): the hip shell, the legs on their brass ball joints (pivots at the hips)   [1 + 10]
        plates.Add(Part(root, PrimitiveType.Sphere, new Vector3(0f, 0.99f, 0f), new Vector3(0.34f, 0.2f, 0.24f), plate, "Pelvis"));
        rig.legL = Leg(root, -0.12f, plate, plates);
        rig.legR = Leg(root, 0.12f, plate, plates);

        // Torso pivot: the chest shell, the wind-up key in its back, the status lamp at the collar, the name plate   [1 + 2 + 1 + 2]
        rig.torso = Pivot(root, "Torso", new Vector3(0f, 1.15f, 0f));
        rig.torsoRest = rig.torso.localPosition;
        plates.Add(Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.18f, 0f), new Vector3(0.42f, 0.4f, 0.26f), plate, "Chest"));
        Key(rig, rig.torso, new Vector3(0f, 0.2f, -0.13f), 0.16f);
        rig.status = Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.31f, 0.132f), new Vector3(0.1f, 0.035f, 0.012f), StatusMat(), "StatusLight");
        NamePlate(rig.torso, new Vector3(0f, 0.005f, 0.136f), 0.2f, kind); // staff, and it says so: "HALCYON · BELLHOP"

        // Arms on their shoulder balls (pivots at the shoulders); the right one carries the flashbulb gun or the hook   [5 + 7 | 6]
        rig.armL = Arm(rig.torso, -0.3f, plate, plates, kind, false, out _);
        rig.armR = Arm(rig.torso, 0.3f, plate, plates, kind, true, out rig.muzzle);

        // THE GLASS HEAD = WEAKSPOT: the same sphere, collider and halo as ever, now a bell jar with the escapement ticking
        // inside and the mainspring's cyan glow (EnemyMannequin.GlassHead). Out in the district (Chapter 3) the same units
        // wear the response's issue instead   [5]
        rig.headPivot = Pivot(rig.torso, "HeadPivot", new Vector3(0f, 0.53f, 0f));
        if (LevelKit.Current == Zone.Surface) IssueHead(rig, kind, ChassisMat());
        else GlassHead(rig, rig.headPivot, 0.42f, -0.3f, 0.92f); // the collar sits on the chest top

        if (kind == EnemyKind.Turret)
            Part(root, PrimitiveType.Cylinder, new Vector3(0f, 0.12f, 0f), new Vector3(0.9f, 0.03f, 0.9f), ThrusterMat(), "ThrusterHalo");   // [1]

        SuitUp(root, rig, kind);   // the painted coat (EnemySuit)   [6 footman | 2 page | 3 bellhop]
        rig.plates = plates.ToArray();   // footman 40, page 38 (+2 rotor), bellhop 36 (+3 pillbox and hook) with the livery
        return rig;
    }

    /// A mannequin's leg: the brass hip ball, the thigh shell, the knee ball, the shin shell, a lacquer shoe. Five renderers.
    static Transform Leg(Transform root, float x, Material plate, List<Renderer> plates)
    {
        Transform hip = Pivot(root, x < 0f ? "LegL" : "LegR", new Vector3(x, 0.92f, 0f));
        Ball(hip, new Vector3(0f, -0.01f, 0f), 0.17f, "Hip");
        plates.Add(Shell(hip, new Vector3(0f, -0.22f, 0f), new Vector3(0.15f, 0.34f, 0.15f), plate, "Thigh"));
        Ball(hip, new Vector3(0f, -0.44f, 0f), 0.16f, "Knee");
        plates.Add(Shell(hip, new Vector3(0f, -0.66f, 0f), new Vector3(0.13f, 0.34f, 0.13f), plate, "Shin"));
        Part(hip, PrimitiveType.Cube, new Vector3(0f, -0.885f, 0.05f), new Vector3(0.13f, 0.07f, 0.26f), Satin(), "Shoe");
        return hip;
    }

    /// A mannequin's arm: the shoulder ball, the upper shell, the elbow ball, the forearm shell, the gloved hand (five),
    /// and on the weapon arm the flashbulb gun (two) or the bellhop's hook bar (one)
    static Transform Arm(Transform torso, float x, Material plate, List<Renderer> plates,
        EnemyKind kind, bool weapon, out Transform muzzle)
    {
        Transform shoulder = Pivot(torso, x < 0f ? "ArmL" : "ArmR", new Vector3(x, 0.32f, 0f));
        Ball(shoulder, Vector3.zero, 0.19f, "Shoulder");
        plates.Add(Shell(shoulder, new Vector3(0f, -0.18f, 0f), new Vector3(0.12f, 0.3f, 0.12f), plate, "UpperArm"));
        Ball(shoulder, new Vector3(0f, -0.36f, 0f), 0.14f, "Elbow");
        plates.Add(Shell(shoulder, new Vector3(0f, -0.53f, 0f), new Vector3(0.11f, 0.28f, 0.11f), plate, "Forearm"));
        Hand(shoulder, new Vector3(0f, -0.72f, 0f), 0.1f); // white gloves: staff
        muzzle = null;
        if (!weapon) return shoulder;

        if (kind == EnemyKind.Chaser)
        {
            // The luggage hook — the thing it swings at you: a brass bar off the hand (the tip and curl are in EnemyLivery)
            Part(shoulder, PrimitiveType.Cube, new Vector3(0f, -0.72f, 0.2f), new Vector3(0.03f, 0.04f, 0.42f), JointMat(), "Hook");
            return shoulder;
        }

        // Ranged: the barrel runs along the arm (local -Y), so aiming the arm aims the gun
        bool cannon = kind == EnemyKind.Turret;
        Part(shoulder, PrimitiveType.Cylinder,
            cannon ? new Vector3(0f, -0.66f, 0f) : new Vector3(0.05f, -0.62f, 0.06f),
            cannon ? new Vector3(0.14f, 0.22f, 0.14f) : new Vector3(0.05f, 0.16f, 0.05f), Satin(), "Barrel");
        Part(shoulder, PrimitiveType.Cylinder,
            cannon ? new Vector3(0f, -0.87f, 0f) : new Vector3(0.05f, -0.78f, 0.06f),
            cannon ? new Vector3(0.17f, 0.015f, 0.17f) : new Vector3(0.08f, 0.01f, 0.08f), ThrusterMat(), "MuzzleRing");
        muzzle = Pivot(shoulder, "Muzzle", cannon ? new Vector3(0f, -0.92f, 0f) : new Vector3(0.05f, -0.82f, 0.06f));
        return shoulder;
    }

    static Transform Pivot(Transform parent, string name, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        return go.transform;
    }

    static Renderer Part(Transform parent, PrimitiveType type, Vector3 localPos, Vector3 scale, Material mat, string name,
        bool keepCollider = false)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        // Stripped NOW (not end-of-frame) so Awake's collider cache never holds a dead one
        if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        return r;
    }

    // ---------------------------------------------------------------- life

    void OnDestroy() { Live.Remove(this); }

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider>();
        Live.Add(this);
        spawnPos = transform.position;
        nextFireAt = Time.time + 1f + Mathf.Abs(spawnPos.x * 0.37f + spawnPos.z * 0.61f) % 2f; // the dummies' opening volleys, staggered by where each stands (never random)
        // NOTE: no config access here — Awake runs inside AddComponent, before
        // Create() assigns config. Health is set in Start.
    }

    void Start()
    {
        hp = MaxHp;
        RoleReset();   // every weakspot is open from birth: the glass head is the execute on every unit (bosses seal their own)
    }

    /// Only in an open district (GameBootstrap sets it per level): the room levels never hold enough units to need it,
    /// and their cutscenes put the camera a long way from where the body is standing
    public static bool FarCulling;
    /// Asleep by hand (nothing sets it since the room loop went; kept for whoever needs a switch): no AI, no breathing
    public bool Dormant;
    Renderer dormantEye;   // one of its renderers: if the culling says it is on screen, it wakes, so a frozen pose is never seen
    float lastSeenAt = float.NegativeInfinity;
    /// RESTING BY DISTANCE (2026-10-09, course-grammar; no rooms to sleep by): past 120 m of the player, or out of
    /// every camera's sight for two seconds while more than 40 m off. Inside 40 m a unit is awake whether or not you
    /// are looking at it (it is behind you, chasing); on screen it wakes at once, so a frozen pose is never seen.
    /// Units ahead on the course wait on their lines as they were placed; units you passed stop when you are gone.
    /// Never a boss, never on the tutorial or the range (their lessons stand far off and must fire).
    public const float RestDistance = 120f, WakeDistance = 40f, UnseenRest = 2f;
    public bool IsResting => Resting;
    bool Resting
    {
        get
        {
            if (Dormant) return dormantEye == null || !dormantEye.isVisible;
            if (kind == EnemyKind.Boss || RespawnEnabled) return false;
            var director = CombatDirector.Instance;
            if (director == null || director.motor == null) return false;
            float d = Vector3.Distance(director.motor.CurrPosition, transform.position);
            if (d > RestDistance) return true;
            if (dormantEye == null || dormantEye.gameObject == null) dormantEye = GetComponentInChildren<Renderer>();
            bool visible = dormantEye != null && dormantEye.isVisible;
            if (visible) lastSeenAt = Time.time;
            if (d <= WakeDistance) return false;
            return !visible && Time.time - lastSeenAt > UnseenRest;
        }
    }

    /// Past this a unit is a dot in the ash: it neither thinks nor animates until you come back. Nothing in the game
    /// engages from further than seventy metres and an arrow dies at ninety, so nothing is lost — and a district can
    /// hold ninety units for the price of the dozen near you.
    const float FarSleep = 150f;
    /// ...and past this it is not drawn: a two-metre unit three hundred metres off is seven pixels, and a hundred of
    /// them are six thousand renderers the district does not need in the frame
    const float FarHide = 300f;

    bool farHidden;
    readonly List<Renderer> farHiddenSet = new List<Renderer>();

    /// Off, and back exactly as it was: only what was showing is hidden, so a popped plate or a shut hatch stays that way
    void SetFarHidden(bool hide)
    {
        farHidden = hide;
        if (hide)
        {
            farHiddenSet.Clear();
            foreach (var r in renderers)
                if (r != null && r.enabled) { r.enabled = false; farHiddenSet.Add(r); }
            return;
        }
        foreach (var r in farHiddenSet) if (r != null) r.enabled = true;
        farHiddenSet.Clear();
    }

    /// The unit's full health, by ROLE: a footman is one jab (fistDamage; the kick's kickDamage is less, so a kick
    /// launches it alive and the landing ends it), a bouncer three chips (BouncerFilter), a shooter the doorman's,
    /// the turret and the dummy as built, a boss its own. A tackle execute hits for all of it.
    public float MaxHp => role switch
    {
        EnemyRole.Boss => bossKind == BossKind.Foreman ? config.foremanHealth : config.bossHealth,
        EnemyRole.Turret => config.enemyTurretHealth,
        EnemyRole.Dummy => config.enemyDummyHealth,
        EnemyRole.Bouncer => config.wardenHealth,
        EnemyRole.Shooter => config.sentryHealth,
        _ => Mathf.Max(1f, config.fistDamage),
    };

    void FixedUpdate()
    {
        if (!Alive)
        {
            if (Time.time >= respawnAt) Respawn();
            return;
        }
        moveSpeed = 0f;
        if (Practice.Active && Practice.Frozen) return; // the range's console

        var director = CombatDirector.Instance;
        if (director == null || director.motor == null) return;
        Vector3 chest = director.motor.CurrPosition + Vector3.up * 0.9f;
        float dist = Vector3.Distance(chest, Center);
        if (FarCulling && dist > FarSleep) return;   // a dot in the ash: see FarSleep
        if (Resting) return;                          // far off, or unseen a while: it waits (resting by distance)
        if (Tossed) { TickToss(Time.fixedDeltaTime); return; }   // in the air off a kick: it flies, it does nothing else
        TickUnstick();

        // A body is a solid thing and running into one is a bump, however fast you were going: nothing in this game
        // dies of being run into. Speed is still damage — through the hands (SpeedDamage), never the shoulder.

        if (role == EnemyRole.Footman || role == EnemyRole.Bouncer) Separate(Time.fixedDeltaTime);

        switch (role)
        {
            case EnemyRole.Footman: FootmanTick(director, chest, dist); break;
            case EnemyRole.Shooter: ShooterTick(director, chest, dist); break;
            case EnemyRole.Bouncer: BouncerTick(director, chest, dist); break;
            case EnemyRole.Turret: SniperTick(director, chest, dist); break;
            case EnemyRole.Boss: break; // its controller drives it
            default: RangedTick(chest, dist); break;
        }
    }

    void RangedTick(Vector3 chest, float dist)
    {
        // Orbs are the bosses' now. The one other unit that fires one is the training dummy, and only where it
        // respawns (the tutorial and the range): in the levels no regular unit shoots
        bool canFire = !HoldFire && config.dummiesFireProjectiles && RespawnEnabled;
        float range = config.dummyProjectileRange;
        if (canFire && Time.time >= nextFireAt && dist <= range)
        {
            nextFireAt = Time.time + config.dummyFireInterval;
            // Lead the opening direction; the shot homes from there (still parryable)
            Vector3 playerVel = CombatDirector.Instance != null && CombatDirector.Instance.motor != null
                ? CombatDirector.Instance.motor.Velocity : Vector3.zero;
            float flight = dist / Mathf.Max(1f, config.projectileSpeed);
            Vector3 predicted = chest + playerVel * (flight * config.projectileLeadFactor);
            Vector3 from = rig != null && rig.muzzle != null ? rig.muzzle.position : Center;
            Vector3 dir = (predicted - from).normalized;
            EnemyProjectile.Spawn(config, from + dir * 0.15f, dir, this);
            recoil = 1f;
            if (GameFeel.Instance != null) GameFeel.Instance.PlayEnemyShot();
        }
    }

    // ---------------------------------------------------------------- animation

    void Update()
    {
        if (!Alive || rig == null) return;
        if (Practice.Active && Practice.Frozen) return; // the range's console
        UpdateLockOn();

        float dt = Time.deltaTime;
        float now = Time.time;
        var director = CombatDirector.Instance;
        bool hasTarget = director != null && director.motor != null;
        Vector3 chest = hasTarget ? director.motor.CurrPosition + Vector3.up * 0.9f : Center + transform.forward * 5f;
        float dist = Vector3.Distance(chest, Center);
        recoil = Mathf.MoveTowards(recoil, 0f, dt * 6f);
        if (Snipes) SniperAudio(Time.unscaledDeltaTime); // the lock whine, for every unit that carries a laser
        TickSeen();
        // Further still and it is not drawn either (never a MARKED unit: the mark is its own shape, seen through walls)
        bool hide = FarCulling && !seen && dist > (farHidden ? FarHide - 20f : FarHide);
        if (hide != farHidden) SetFarHidden(hide);
        if (FarCulling && dist > FarSleep) return;   // nobody can see it breathe from there
        if (Resting) return;                            // nor resting (seen, it breathes)

        // The dummy's and the page's status light glitches red now and then (look only: the roles' lamps are their tells, EnemyRoles)
        if (rig.status != null && (role == EnemyRole.Dummy || role == EnemyRole.Turret))
        {
            if (Time.time > corruptUntil && Random.value < dt * 0.35f) corruptUntil = Time.time + Random.Range(0.06f, 0.22f);
            bool corrupt = Time.time < corruptUntil;
            if (corrupt != corruptVisual)
            {
                corruptVisual = corrupt;
                rig.status.sharedMaterial = corrupt ? CorruptMat() : LampRest();
            }
        }

        switch (role)
        {
            case EnemyRole.Footman: AnimateFootman(dt, now); break;
            case EnemyRole.Shooter: AnimateShooter(dt, now, chest, dist); break;
            case EnemyRole.Bouncer: AnimateBouncer(dt, now); break;
            case EnemyRole.Turret: AnimateSentinel(dt, now, chest, dist, hasTarget); break;
            case EnemyRole.Boss: break;
            default: AnimateDummy(dt, now, chest, dist, hasTarget); break;
        }
        clips?.Apply(); // an authored clip (Blender, Resources/Clips) blended over the pivots the procedural pass just wrote; nothing when none plays
        MannequinTick(now); // the key turns, the escapement ticks (EnemyMannequin: look only)

        if (hitFlinch > 0f && kind != EnemyKind.Boss)
        {
            // Every hit rocks the torso back
            rig.torso.localRotation *= Quaternion.Euler(-16f * hitFlinch, 0f, 0f);
            hitFlinch = Mathf.MoveTowards(hitFlinch, 0f, dt * 5f);
        }
    }

    /// Legs and torso from how fast the body actually moved this tick (0 = idle breathing); `top` is the speed a
    /// full stride reads at. A rig whose "legs" are rail bogies, tails or fins does not swing them.
    void Gait(float dt, float now, float lean, float top)
    {
        float run = Mathf.Clamp01(moveSpeed / Mathf.Max(0.1f, top));
        gait = Mathf.MoveTowards(gait, run, dt * 6f);
        gaitPhase += moveSpeed * dt * 4.5f; // ~1.4 m stride
        float swing = Mathf.Sin(gaitPhase);
        bool strides = kind != EnemyKind.Borer && kind != EnemyKind.Moth && kind != EnemyKind.Ocular && kind != EnemyKind.Overseer;
        // A mannequin walks stiff: shorter steps than a man's, and the legs move in clicks (the escapement's beat)
        float legs = strides ? Click(swing * 34f * gait, 6f) : 0f;
        rig.legL.localRotation = Quaternion.Euler(legs, 0f, 0f);
        rig.legR.localRotation = Quaternion.Euler(-legs, 0f, 0f);
        rig.torso.localRotation = Quaternion.Euler(lean * gait + Mathf.Sin(now * 2f) * 1.5f, 0f, swing * 3f * gait);
        rig.torso.localPosition = rig.torsoRest
            + Vector3.up * (Mathf.Abs(Mathf.Cos(gaitPhase)) * 0.05f * gait + Mathf.Sin(now * 2f) * 0.01f);
    }

    void AnimateSentinel(float dt, float now, Vector3 chest, float dist, bool hasTarget)
    {
        // Hover on the thruster halo, legs hanging, cannon arm tracking you
        transform.position = spawnPos + Vector3.up * (Mathf.Sin(now * 1.6f) * 0.12f);
        rig.legL.localRotation = Quaternion.Euler(28f + Mathf.Sin(now * 1.3f) * 4f, 0f, 6f);
        rig.legR.localRotation = Quaternion.Euler(24f + Mathf.Sin(now * 1.3f + 1f) * 4f, 0f, -6f);
        rig.torso.localRotation = Quaternion.Euler(Mathf.Sin(now * 1.1f) * 3f, 0f, Mathf.Sin(now * 0.9f) * 2f);
        rig.torso.localPosition = rig.torsoRest;
        rig.armL.localRotation = Quaternion.Euler(Mathf.Sin(now * 1.4f) * 6f + 8f, 0f, 14f);
        rig.headPivot.localRotation = Quaternion.Euler(Click(Mathf.Sin(now * 1.2f) * 6f, 3f), 0f, 0f); // a nod, in clicks
        if (rig.handL != null) rig.handL.localRotation = Quaternion.Euler(0f, now * 1080f, 0f); // the page's rotor-hat (EnemyLivery): look only

        bool tracking = hasTarget && dist <= config.sniperRange * 1.2f;
        if (tracking) FaceTowards(chest, 200f, dt);
        aimBlend = Mathf.MoveTowards(aimBlend, tracking ? 1f : 0f, dt * 4f);
        AimArm(chest, aimBlend);
    }

    void AnimateDummy(float dt, float now, Vector3 chest, float dist, bool hasTarget)
    {
        Gait(dt, now, 0f, config.chaserSpeed);
        rig.armL.localRotation = Quaternion.Euler(Mathf.Sin(now * 1.7f) * 4f + 4f, 0f, 8f);
        rig.headPivot.localRotation = Quaternion.Euler(0f, Click(Mathf.Sin(now * 0.7f) * 14f, 7f), 0f); // scanning, in clicks

        bool near = hasTarget && dist <= config.dummyProjectileRange * 1.5f;
        if (near) FaceTowards(chest, 90f, dt);
        bool tracking = hasTarget && config.dummiesFireProjectiles && dist <= config.dummyProjectileRange;
        aimBlend = Mathf.MoveTowards(aimBlend, tracking ? 1f : 0f, dt * 3f);
        AimArm(chest, aimBlend);
    }

    /// Point the right arm (local -Y, the barrel) at `target`; blend 0 = hanging at rest
    void AimArm(Vector3 target, float blend)
    {
        Quaternion rest = rig.torso.rotation;
        Vector3 dir = target - rig.armR.position;
        Quaternion aim = dir.sqrMagnitude > 0.01f ? Quaternion.FromToRotation(Vector3.down, dir.normalized) : rest;
        rig.armR.rotation = Quaternion.Slerp(rest, aim, blend);
        rig.armR.localPosition = ArmRRest + Vector3.back * (0.08f * recoil); // kick on fire
    }

    void FaceTowards(Vector3 point, float turnRate, float dt)
    {
        Vector3 flat = point - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat), turnRate * dt);
    }

    void ResetPose()
    {
        rig.torso.localRotation = Quaternion.identity;
        rig.torso.localPosition = rig.torsoRest;
        rig.torso.localScale = Vector3.one;
        rig.headPivot.localRotation = Quaternion.identity;
        rig.armL.localRotation = Quaternion.identity;
        rig.armR.localRotation = Quaternion.identity;
        if (IsCivic(kind)) rig.armR.localPosition = ArmRRest; // every other rig hangs its arms elsewhere
        rig.legL.localRotation = Quaternion.identity;
        rig.legR.localRotation = Quaternion.identity;
        gait = 0f;
        moveSpeed = 0f;
        recoil = 0f;
        aimBlend = 0f;
        hitFlinch = 0f;
        strikeAt = -10f;
    }

    /// Called every frame the fist's window rests on this weakspot (Fists.Probe):
    /// the head goes white-hot and the halo flares — "in reach" is unmistakable
    public void MarkTargeted() => targetedUntil = Time.unscaledTime + 0.08f;

    Vector3 ringRest;
    bool ringRestKnown;

    void UpdateLockOn()
    {
        if (rig.head == null) return;
        bool targeted = Time.unscaledTime < targetedUntil;
        // What holds the weakpoint flares open round it: the stewards' halo, a Response unit's clamps, cradle or frame.
        // Multiplied off whatever it was built at, so any shape of cage does it
        if (rig.ring != null && !ringRestKnown) { ringRest = rig.ring.localScale; ringRestKnown = true; }
        if (targeted && rig.ring != null)
        {
            float pulse = 1.44f + 0.13f * Mathf.Sin(Time.unscaledTime * 24f);
            rig.ring.localScale = new Vector3(ringRest.x * pulse, ringRest.y * 1.33f, ringRest.z * pulse);
        }
        if (targeted == targetedVisual) return;
        targetedVisual = targeted;
        rig.head.sharedMaterial = weakspotClosed ? SealedMat() : targeted ? TargetMat() : WeakMat();
        if (kind == EnemyKind.Boss) LockOnExtras(targeted);
        if (!targeted && rig.ring != null) rig.ring.localScale = ringRest;
    }

    void SetSkin(Material mat)
    {
        if (rig == null || rig.plates.Length == 0 || rig.plates[0].sharedMaterial == mat) return;
        foreach (var r in rig.plates) r.sharedMaterial = mat;
    }

    // ---------------------------------------------------------------- damage

    /// Dazed: a toss's flight, a hook's knock. (A boss answers with its own counter.)
    public void Stagger()
    {
        if (role == EnemyRole.Boss) { if (Boss.Current != null) Boss.Current.OnCountered(); return; }
        windupEndsAt = 0f;
        staggeredUntil = Time.time + config.chaserStaggerTime;
        attackReadyAt = staggeredUntil + 0.3f;
        if (GameFeel.Instance != null) GameFeel.Instance.PlayStagger(true);   // a mannequin: the shell rattles, not iron
    }

    public void Damage(float amount, KillMethod method)
    {
        if (!Alive) return;
        if (role == EnemyRole.Boss && Boss.Current != null && !Boss.Current.FilterDamage(ref amount, method)) return;
        if (role == EnemyRole.Bouncer && !BouncerFilter(ref amount, method)) return;
        float dealt = Mathf.Min(Mathf.Max(0f, hp), amount);   // what it actually cost the unit: the feed is paid on this
        hp -= amount;
        hitFlinch = 1f;
        if (GameFeel.Instance != null) GameFeel.Instance.PlayThunk();
        if (CombatDirector.Instance != null && IsYours(method)) CombatDirector.Instance.ReportDamageDealt(this, dealt, method);
        if (hp <= 0f) Die(method);
    }

    void Die(KillMethod method) => Perish(method, true);

    /// The end of a unit, credited to you or not (a moth that reached you is nobody's kill)
    void Perish(KillMethod method, bool credited)
    {
        Alive = false;
        DeathMethod = method;
        respawnAt = RespawnEnabled && !StaysDead ? Time.time + config.enemyRespawnSeconds : float.PositiveInfinity;
        if (Snipes) SniperOff();
        DropSeen();
        if (role != EnemyRole.Boss) Collapse(); // the boss stays on his feet: his exit plays first (BossOutro), then he falls
        if (credited && CombatDirector.Instance != null)
            CombatDirector.Instance.ReportKill(method);
    }

    void Respawn()
    {
        Alive = true;
        hp = MaxHp;
        transform.position = spawnPos;
        windupEndsAt = 0f;
        staggeredUntil = 0f;
        TossReset();
        ResetPose();
        SetSkin(normalMat);
        foreach (var r in renderers) r.enabled = true;
        RoleReset();
        if (Snipes) SniperReset();
        foreach (var c in colliders) c.enabled = true;
        if (GameFeel.Instance != null) GameFeel.Instance.PlayEnemySpawn();
    }

    /// The range's console: a dead unit back on its feet now
    public void Revive()
    {
        if (!Alive) Respawn();
    }

    /// The range's console: a unit off the range — hidden, out of play, and not coming back until Revive
    public void Vanish()
    {
        Alive = false;
        respawnAt = float.PositiveInfinity;
        DropSeen();
        foreach (var r in renderers) r.enabled = false;
        foreach (var c in colliders) c.enabled = false;
    }

    // ---------------------------------------------------------------- ragdoll

    /// The robot drops as a jointed RAGDOLL built from copies of its parts:
    /// eleven bones (pelvis, torso, head, arms in two, legs in two) with
    /// character joints, on the Ignore Raycast layer so corpses never catch a
    /// cast. A sword kill CUTS it: every joint straddling the cut plane is
    /// severed and the halves fly apart; an execute takes the head clean off.
    GameObject SpawnRagdoll(KillMethod method)
    {
        if (rig == null) return null;
        var root = new GameObject("Ragdoll " + kind);
        root.transform.position = transform.position;
        var bones = new Dictionary<string, Rigidbody>();
        var parentOf = new Dictionary<string, string>();
        var pivotOf = new Dictionary<string, Vector3>();

        Rigidbody Bone(string name, string parent, Transform src, Vector3 centre, Vector3 size, float mass, Vector3 joint)
        {
            var go = new GameObject(name) { layer = 2 };
            go.transform.SetParent(root.transform);
            go.transform.SetPositionAndRotation(src.TransformPoint(centre), src.rotation);
            go.AddComponent<BoxCollider>().size = size * transform.lossyScale.y;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.linearDamping = 0.15f;
            rb.angularDamping = 0.6f;
            go.AddComponent<RagdollBone>();
            bones[name] = rb;
            parentOf[name] = parent;
            pivotOf[name] = src.TransformPoint(joint);
            return rb;
        }

        Bone("pelvis", null, transform, new Vector3(0f, 1.02f, 0f), new Vector3(0.36f, 0.3f, 0.24f), 2.5f, Vector3.zero);
        Bone("torso", "pelvis", rig.torso, new Vector3(0f, 0.2f, 0f), new Vector3(0.46f, 0.44f, 0.28f), 3f, Vector3.zero);
        Bone("head", "torso", rig.headPivot, Vector3.zero, new Vector3(0.4f, 0.4f, 0.4f), 0.8f, new Vector3(0f, -0.2f, 0f));
        Bone("armL", "torso", rig.armL, new Vector3(0f, -0.18f, 0f), new Vector3(0.13f, 0.32f, 0.13f), 0.7f, Vector3.zero);
        Bone("foreL", "armL", rig.armL, new Vector3(0f, -0.53f, 0f), new Vector3(0.11f, 0.36f, 0.11f), 0.5f, new Vector3(0f, -0.36f, 0f));
        Bone("armR", "torso", rig.armR, new Vector3(0f, -0.18f, 0f), new Vector3(0.13f, 0.32f, 0.13f), 0.7f, Vector3.zero);
        Bone("foreR", "armR", rig.armR, new Vector3(0f, -0.53f, 0f), new Vector3(0.11f, 0.36f, 0.11f), 0.5f, new Vector3(0f, -0.36f, 0f));
        Bone("thighL", "pelvis", rig.legL, new Vector3(0f, -0.22f, 0f), new Vector3(0.16f, 0.42f, 0.16f), 1.2f, Vector3.zero);
        Bone("shinL", "thighL", rig.legL, new Vector3(0f, -0.66f, 0f), new Vector3(0.14f, 0.5f, 0.18f), 0.9f, new Vector3(0f, -0.44f, 0f));
        Bone("thighR", "pelvis", rig.legR, new Vector3(0f, -0.22f, 0f), new Vector3(0.16f, 0.42f, 0.16f), 1.2f, Vector3.zero);
        Bone("shinR", "thighR", rig.legR, new Vector3(0f, -0.66f, 0f), new Vector3(0.14f, 0.5f, 0.18f), 0.9f, new Vector3(0f, -0.44f, 0f));

        foreach (var kv in parentOf)
        {
            if (kv.Value == null) continue;
            Rigidbody child = bones[kv.Key], parent = bones[kv.Value];
            var j = child.gameObject.AddComponent<CharacterJoint>();
            j.connectedBody = parent;
            j.anchor = child.transform.InverseTransformPoint(pivotOf[kv.Key]);
            j.axis = child.transform.InverseTransformDirection(parent.transform.right);
            j.swingAxis = child.transform.InverseTransformDirection(parent.transform.forward);
            j.lowTwistLimit = new SoftJointLimit { limit = -25f };
            j.highTwistLimit = new SoftJointLimit { limit = 25f };
            j.swing1Limit = new SoftJointLimit { limit = 50f };
            j.swing2Limit = new SoftJointLimit { limit = 35f };
            j.enableProjection = true;
            j.enableCollision = false;
        }

        // The visible parts ride their bones (copies: the live rig stays intact for respawn)
        foreach (var r in renderers)
        {
            if (r == null || !r.enabled) continue; // popped buttons and shed pieces are already gone
            string n = r.name;
            if (n == "WeakspotRing" || n == "ThrusterHalo" || n == "Lane" || n == "Link" || n == "Tether") continue;
            string bone = BoneFor(r.transform);
            if (r.GetComponent<TextMesh>() != null) continue; // labels stay on the corpse root, not as bones
            var mf = r.GetComponent<MeshFilter>();
            string mesh = mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "Cube";
            PrimitiveType type = mesh == "Sphere" ? PrimitiveType.Sphere : mesh == "Cylinder" ? PrimitiveType.Cylinder : mesh == "Capsule" ? PrimitiveType.Capsule : PrimitiveType.Cube;
            var piece = GameObject.CreatePrimitive(type);
            piece.name = n;
            piece.layer = 2;
            Object.DestroyImmediate(piece.GetComponent<Collider>());
            piece.transform.SetPositionAndRotation(r.transform.position, r.transform.rotation);
            piece.transform.localScale = r.transform.lossyScale;
            piece.GetComponent<MeshRenderer>().sharedMaterial = r.sharedMaterial;
            piece.transform.SetParent(bones[bone].transform, true);
        }

        // The killing blow throws the whole body
        Vector3 impulse = KillImpulse(method);
        foreach (var rb in bones.Values)
        {
            rb.linearVelocity = impulse * Random.Range(0.85f, 1.15f);
            rb.angularVelocity = Random.insideUnitSphere * 3f;
        }

        // THE EXECUTE (the glass head taken by a jab, a hook or a kick: Fists stamps the cut plane): a clean
        // decapitation — the head goes with the cut, the gears spill at the neck, the body just drops
        if (method == KillMethod.SwordExecute)
        {
            Vector3 n = Vector3.Cross(Fists.LastStrikeSweep, Fists.LastStrikeForward);
            if (n.sqrMagnitude < 1e-4f) n = Vector3.up;
            n.Normalize();
            var headJoint = bones["head"].GetComponent<CharacterJoint>();
            if (headJoint != null) Sever(headJoint, n, 1f);
            bones["head"].linearVelocity = Fists.LastStrikeSweep * 9f + Vector3.up * 6f;
            bones["head"].angularVelocity = Random.insideUnitSphere * 20f;
        }

        if (GameFeel.Instance != null) GameFeel.Instance.PlayUnitBreak(kind, method); // the material of it: enamel cracks, the glass head, the brass (the kill's punctuation is PlayKill)
        root.AddComponent<RagdollFade>().Init(config.ragdollSettle, config.ragdollLife);
        return root;
    }

    /// Which ragdoll bone a live part belongs to, from the pivot it hangs off
    string BoneFor(Transform part)
    {
        // Walk up to the nearest pivot: parts nested under coat or smile pivots ride the torso and head
        Transform child = part, p = part.parent;
        while (p != null && p != transform && p != rig.headPivot && p != rig.torso && p != rig.armL && p != rig.armR && p != rig.legL && p != rig.legR)
        {
            child = p;
            p = p.parent;
        }
        if (p == rig.headPivot) return "head";
        if (p == rig.torso) return "torso";
        if (p == rig.armL) return child.localPosition.y > -0.36f ? "armL" : "foreL";
        if (p == rig.armR) return child.localPosition.y > -0.36f ? "armR" : "foreR";
        if (p == rig.legL) return child.localPosition.y > -0.44f ? "thighL" : "shinL";
        if (p == rig.legR) return child.localPosition.y > -0.44f ? "thighR" : "shinR";
        return "pelvis";
    }

    void Sever(CharacterJoint j, Vector3 n, float side, float force = 3.5f)
    {
        var rb = j.GetComponent<Rigidbody>();
        var other = j.connectedBody;
        Vector3 at = j.transform.TransformPoint(j.anchor);
        Object.DestroyImmediate(j);
        Vector3 push = n * (side >= 0f ? 1f : -1f) * force;
        rb.linearVelocity += push + Vector3.up * 1.5f;
        if (other != null) other.linearVelocity -= push * 0.6f;
        MuzzleFlash.Spawn(at, 0.45f); // the cut flashes at the joint
        SpillGears(at, n, 6);         // and the clockwork comes out of it: gears and springs, never blood (EnemyMannequin)
    }

    Vector3 KillImpulse(KillMethod method)
    {
        var director = CombatDirector.Instance;
        bool hasPlayer = director != null && director.motor != null;
        Vector3 away = hasPlayer ? Center - director.motor.CurrPosition : transform.forward;
        away.y = 0f;
        away = away.sqrMagnitude > 0.01f ? away.normalized : transform.forward;
        switch (method)
        {
            case KillMethod.SlamShockwave: return away * 9f + Vector3.up * 7f;
            case KillMethod.SwordExecute: return Vector3.up * 1.5f;
            case KillMethod.Blast: return away * 10f + Vector3.up * 8f;
            case KillMethod.Fist: return Fists.LastHitDir * 7f + Vector3.up * 2.5f;             // blunt: the body goes the way the punch went
            case KillMethod.Tackle: return Fists.LastTackleDir * (6f + 0.3f * Fists.LastTackleSpeed) + Vector3.up * 4f; // bowled over, along your travel
            default: return away * 7f + Vector3.up * 3f;
        }
    }

    // ---------------------------------------------------------------- materials

    // Civic units, DARK DECO: enamel mannequin shells (the plate per kind IS the shell: the windup flashes it gold),
    // brass joints and trim, a cream incandescent status lamp — Halcyon's company property, 1939
    static Material ChassisMat() => chassisMat ??= Lit(new Color(0.11f, 0.11f, 0.13f), 0.2f, 0.6f);    // charcoal enamel: the machines' housings
    static Material JointMat() => jointMat ??= Lit(new Color(0.78f, 0.58f, 0.25f), 0.9f, 0.7f);        // brass joints
    static Material DummyPlate() => Enamel();        // the footman: cream enamel, the shop dummy's own skin
    static Material TurretPlate() => OxbloodShell(); // the page: the bell staff's oxblood
    static Material ChaserPlate() => OxbloodShell(); // the bellhop: the same oxblood, on the run
    static Material StaggerMat() => staggerMat ??= Lit(new Color(0.42f, 0.4f, 0.37f), 0.5f, 0.3f);     // dazed: a dead sepia grey
    static Material VisorMat() => visorMat ??= Lit(new Color(0.05f, 0.06f, 0.08f), 0.95f, 0.95f); // a mirrored visor
    static Material StatusMat() => statusMat ??= Unlit(new Color(1.4f, 1.26f, 0.98f));   // the status lamp: cream incandescent
    static Material CorruptMat() => corruptMat ??= Unlit(new Color(2.5f, 0.2f, 0.15f));  // the glitch: red
    static Material ThrusterMat() => thrusterMat ??= Unlit(new Color(2.0f, 1.4f, 0.7f));     // amber thruster halo (a unit's own light)
    static Material WindupMat() => windupMat ??= Unlit(new Color(1f, 0.85f, 0.35f));
    static Material WeakMat() => weakMat ??= Unlit(new Color(0.3f, 2f, 2.4f));      // HDR cyan — reads as a glow
    static Material sealedMat;
    static Material SealedMat() => sealedMat ??= Lit(new Color(0.2f, 0.19f, 0.2f), 0.6f, 0.45f); // a closed weakspot: dark iron, no glow
    static Material RingMat() => ringMat ??= Unlit(new Color(0.2f, 1.2f, 1.4f));
    static Material TargetMat() => targetMat ??= Unlit(new Color(1.6f, 3.2f, 3.4f)); // white-hot cyan: locked on

    static Material Lit(Color c, float metallic, float smoothness)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Smoothness", smoothness);
        return m;
    }

    static Material Unlit(Color c)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetColor("_BaseColor", c);
        return m;
    }
}
