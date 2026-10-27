using UnityEngine;

/// <summary>
/// THE BOSS of the chase: a Boss (the HUD's gauge, Boss.Current for whoever asks) whose body is the project's own rig
/// (Enemy.CreateBoss: the Concierge's or the Stationmaster's mannequin, at a trolley's scale) riding a vehicle
/// (LevelKit.ChaseVehicle) down the course's centre channel AHEAD of you. He runs at run × 1.04 when you are far
/// and HOLDS a gap of fourteen metres when you run at run speed: only a hop chain, a springboard or the whip closes
/// it. Within reach, a jab or a kick on his body is a HIT: the fists' probe finds him because his hitbox is an
/// Enemy's collider (Fists.Probe: GetComponentInParent<Enemy>, Alive, no weakspot — his head is sealed), and
/// Enemy.Damage asks Boss.Current.FilterDamage, which is where a hit is counted, the bar bitten a ninth, the head
/// flashed cyan, the brass rung, and the SURGE begun (25 m ahead over a second: never two in a row). A kick on him
/// springboards you through the fists' own path; nothing here launches you. Three hits a phase (the course steps,
/// the vehicle drops an obstacle that is also a platform); nine and the vehicle BREAKS: it derails and stops, he
/// slumps, the course lays the finish. Nothing here is read from config in Awake.
/// </summary>
public class ChaseBoss : Boss
{
    public static ChaseBoss Instance { get; private set; }

    static readonly string[] PhaseNames = { "PHASE I", "PHASE II", "PHASE III" };

    ChaseKind kind;
    ChaseCourse course;
    Transform vehicle, cab;
    Light[] lamps;
    Vector3 standLocal;
    Renderer[] headRs;
    Material[] headMats;
    float z, speed, baseY, buffersZ, maxSpeed;
    bool engaged, broken;
    int hits, dropSide = 1;
    float surgeStart = -10f, surgeFrom, surgeTo, lastHitAt = -10f, flashUntil, brokenAt, wreckFrom, wreckTo;
    PlayerMotor motor;

    public float Z => z;
    public Vector3 HeadPoint => body != null && body.BossHead != null ? body.BossHead.position : vehicle.position + Vector3.up * 3f;

    // ------------------------------------------------------------ the cards and the bar

    public override string BossName => kind == ChaseKind.Concierge ? "THE CONCIERGE" : "THE STATIONMASTER";
    public override string CompanyLine => kind == ChaseKind.Concierge ? "HALCYON HOUSE  ·  FRONT OF HOUSE" : "HALCYON RAILWAYS  ·  TERMINUS";
    public override string IntroBeat => kind == ChaseKind.Concierge ? "THE HOUSE DOES NOT WAIT, SIR" : "NOTHING IS LATE BUT YOU";
    public override string ExitStamp => kind == ChaseKind.Concierge ? "CHECKED OUT" : "CANCELLED";
    public override string ExitLine => kind == ChaseKind.Concierge ? "SERVICE TERMINATED" : "SERVICE WITHDRAWN";
    public override string LastWord => kind == ChaseKind.Concierge ? "VACANT" : "TERMINATED";
    public override string LastLine => kind == ChaseKind.Concierge ? "…CHECKED OUT." : "…END OF THE LINE.";
    public override string BeatPhase => "PHASE III";
    public override string BeatWord => "LAST CALL";
    public override bool Engaged => engaged;
    public override bool IsDead => broken;
    public override bool ShowPlates => false;
    public override int PlateCount => 0;
    public override int PlatesLeft => 0;
    public override string PhaseName => PhaseNames[Mathf.Clamp(hits / Chase.HitsPerPhase, 0, PhaseNames.Length - 1)];
    public override float StaggerWindow => 0f;

    void Awake() { Instance = this; Current = this; }
    void OnDisable()
    {
        if (Instance == this) Instance = null;
        if (Current == this) Current = null;
    }

    /// Built at build time: the vehicle at its start, the body on its tail facing back down the course
    public static ChaseBoss Create(Transform root, MovementConfig config, ChaseCourse course, ChaseKind kind, float floorY, float zStart, float zBuffers)
    {
        var host = new GameObject(kind == ChaseKind.Concierge ? "TheConcierge" : "TheStationmaster");
        host.transform.SetParent(root);
        host.transform.position = Vector3.zero;   // the vehicle is drawn in world units round the origin, then moved
        var b = host.AddComponent<ChaseBoss>();
        b.config = config; b.course = course; b.kind = kind; b.floorY = floorY; b.buffersZ = zBuffers;
        b.baseY = floorY - Chase.ChanDrop(kind);
        b.vehicle = LevelKit.ChaseVehicle(host.transform, kind, out b.standLocal, out b.lamps, out b.cab);
        b.vehicle.position = new Vector3(0f, b.baseY, zStart);
        b.z = zStart;
        Physics.SyncTransforms();
        float scale = kind == ChaseKind.Concierge ? Chase.BossScaleHouse : Chase.BossScaleTerminus;
        Vector3 feet = b.vehicle.position + b.standLocal;
        b.body = Enemy.CreateBoss(feet, config, scale, kind == ChaseKind.Concierge ? Enemy.BossKind.Concierge : Enemy.BossKind.Foreman);
        b.body.transform.SetParent(b.vehicle, true);
        b.body.transform.position = feet;   // on the open tail, whatever Settle thought
        b.body.transform.rotation = Quaternion.LookRotation(Vector3.back);
        b.body.HoldFire = true;
        b.body.SetWeakspotOpen(false);      // no execute: every hit is a hit (the head, the throat/core, the cuffs/caps all sealed)
        b.body.SetWeakspot(b.body.BossThroat, false);
        b.body.SetWeakspot(b.body.BossCuffL, false);
        b.body.SetWeakspot(b.body.BossCuffR, false);
        foreach (var w in b.body.GetComponentsInChildren<EnemyWeakspot>()) { var col = w.GetComponent<Collider>(); if (col != null) col.enabled = false; }
        b.CacheHead();
        b.arenaCenter = new Vector3(0f, floorY, zStart);
        b.arenaRadius = Chase.HalfW;
        b.checkpoint = new Vector3(0f, floorY + 1f, course.Z0 + 2.5f);
        return b;
    }

    void CacheHead()
    {
        var head = body != null ? body.BossHead : null;
        headRs = head != null ? head.GetComponentsInChildren<Renderer>() : new Renderer[0];
        headMats = new Material[headRs.Length];
        for (int i = 0; i < headRs.Length; i++) headMats[i] = headRs[i].sharedMaterial;
    }

    // ------------------------------------------------------------ the chase's calls

    public override void Engage()
    {
        if (engaged) return;
        engaged = true;
        EngagedAt = Time.time;
        maxSpeed = config.baseRunSpeed * Chase.BossSpeedMult;
        speed = config.baseRunSpeed;
        Say(IntroBeat, 3.5f);
    }

    /// A death: back to his lead ahead of the phase's checkpoint, standing, the hits kept
    public void ResetTo(float zAt)
    {
        if (broken) return;
        z = Mathf.Min(zAt, buffersZ);
        speed = 0f;
        surgeStart = -10f;
        vehicle.position = new Vector3(0f, baseY, z);
    }

    /// The body asked: a jab or a kick on him (Fists → Enemy.Damage). A hit if he is engaged, not surging and not
    /// still ringing from the last; the bar bites a ninth, the ninth is the end of him
    public override bool FilterDamage(ref float amount, KillMethod method)
    {
        if (!engaged || broken) return false;
        float now = Time.time;
        if (now - surgeStart < Chase.SurgeTime || now - lastHitAt < Chase.HitCooldown) return false;
        hits++;
        HitsTaken = hits;
        lastHitAt = now;
        LastHitTime = Time.unscaledTime;
        LastHitFraction = 1f / Chase.HitsToBreak;
        bool last = hits >= Chase.HitsToBreak;
        LastHitLabel = last ? "BROKEN" : "HIT  " + hits + " OF " + Chase.HitsToBreak;
        amount = last ? float.MaxValue : body.MaxHp / Chase.HitsToBreak;
        surgeStart = now; surgeFrom = z; surgeTo = Mathf.Min(z + Chase.SurgeDist, buffersZ);
        FlashHead(now);
        var feel = GameFeel.Instance;
        if (feel != null) { feel.PlayBossHit(); feel.PlayBossBell(); feel.Sparks(HeadPoint, 14, true); }
        MovementEvents.Emit(MoveType.Fist, last ? "BOSS  BROKEN" : "BOSS  " + hits + "/" + Chase.HitsToBreak);
        Say(Line(hits), 2.5f);
        DropObstacle();
        course.OnBossHit(hits);
        if (last) Break();
        return true;
    }

    string Line(int n)
    {
        bool house = kind == ChaseKind.Concierge;
        if (n >= Chase.HitsToBreak) return LastLine;
        if (n % Chase.HitsPerPhase != 0) return house ? (n % 2 == 1 ? "DO MIND THE LUGGAGE" : "ALWAYS AT YOUR DISPOSAL") : (n % 2 == 1 ? "STAND CLEAR OF THE DOORS" : "THE TRAINS RUN ON TIME");
        return n == Chase.HitsPerPhase ? (house ? "THE HOUSE IS NOT AMUSED" : "SIGNALS AT DANGER") : (house ? "YOU WILL BE ACCOMMODATED" : "ALL CHANGE");
    }

    void Say(string s, float seconds) { Subtitle = s; SubtitleUntil = Time.time + seconds; }

    void FlashHead(float now)
    {
        for (int i = 0; i < headRs.Length; i++) if (headRs[i] != null) headRs[i].sharedMaterial = LevelKit.NeonCyan;
        flashUntil = now + 0.25f;
    }

    void RestoreHead()
    {
        for (int i = 0; i < headRs.Length; i++) if (headRs[i] != null) headRs[i].sharedMaterial = headMats[i];
    }

    /// Off the vehicle, onto the course just ahead of you (never in a gap): the Concierge throws luggage (a lip
    /// stack); the Stationmaster drops a signal gantry (a kick-wall arch) or vents steam (a pad), turn about
    void DropObstacle()
    {
        float at = course.FloorAfter(z + 3f, 3f);
        if (at < 0f) return;
        dropSide = -dropSide;
        Transform parent = course.ParentFor(at);
        if (kind == ChaseKind.Concierge) LevelKit.ChaseLuggage(parent, floorY, dropSide * 4.6f, at + 1f);
        else if (hits % 2 == 1) LevelKit.ChaseGantry(parent, floorY, at + 1f);
        else LevelKit.JumpPad(parent, new Vector3(dropSide * 5f, floorY, at + 1f), 0f, 14f, 6f);
        if (GameFeel.Instance != null) GameFeel.Instance.PlayUnitLand(new Vector3(dropSide * 4.6f, floorY, at));
    }

    /// The ninth hit: the vehicle derails and slides to a stop, the lamps go red, he slumps; the course lays the finish
    void Break()
    {
        broken = true;
        brokenAt = Time.time;
        surgeStart = -10f;
        wreckFrom = z;
        wreckTo = Mathf.Min(z + Chase.WreckSlide, buffersZ);
        foreach (var l in lamps) if (l != null) l.color = new Color(1f, 0.25f, 0.15f);
        if (GameFeel.Instance != null) { GameFeel.Instance.PlayBossCollapse(); GameFeel.Instance.Sparks(vehicle.position + Vector3.up * 1.5f, 30, false); }
        course.OnBossBroken(wreckTo);
    }

    // ------------------------------------------------------------ the frame

    void Update()
    {
        if (!Application.isPlaying) return;
        float dt = Time.deltaTime, now = Time.time;
        DisplayHealth01 = Mathf.MoveTowards(DisplayHealth01, Health01, dt * 0.5f);
        if (flashUntil > 0f && now > flashUntil) { flashUntil = 0f; RestoreHead(); }
        if (motor == null)
        {
            var d = CombatDirector.Instance;
            if (d != null) motor = d.motor;
            if (motor == null) return;
        }
        if (broken) { TickWreck(now); return; }
        FaceYou();
        if (!engaged) return;
        if (now - surgeStart < Chase.SurgeTime)
        {
            float k = (now - surgeStart) / Chase.SurgeTime;
            z = Mathf.Lerp(surgeFrom, surgeTo, 1f - (1f - k) * (1f - k));
            speed = maxSpeed;
        }
        else
        {
            // The hold: he matches your forward speed and corrects toward the gap, never faster than his own best; far
            // behind him he eases off so you can find him again
            float gap = z - motor.CurrPosition.z;
            float yours = Mathf.Max(0f, motor.Velocity.z);
            float want = yours + (Chase.HoldGap - gap) * Chase.HoldGain;
            if (gap > Chase.FarGap) want = Mathf.Min(want, yours * 0.85f);
            want = Mathf.Clamp(want, 0f, maxSpeed);
            speed = Mathf.MoveTowards(speed, want, Chase.BossAccel * dt);
            z += speed * dt;
        }
        if (z >= buffersZ) { z = buffersZ; speed = 0f; }
        vehicle.position = new Vector3(0f, baseY, z);
    }

    void FaceYou()
    {
        if (body == null || motor == null) return;
        Vector3 to = motor.CurrPosition - body.transform.position;
        to.y = 0f;
        if (to.sqrMagnitude > 0.01f) body.transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
    }

    void TickWreck(float now)
    {
        float k = Mathf.Clamp01((now - brokenAt) / Chase.WreckSeconds);
        float e = 1f - (1f - k) * (1f - k);
        z = Mathf.Lerp(wreckFrom, wreckTo, e);
        vehicle.position = new Vector3(dropSide * 0.6f * e, baseY - 0.45f * e, z);
        vehicle.rotation = Quaternion.Euler(-6f * e, 0f, dropSide * 24f * e);
        if (cab != null) cab.localRotation = Quaternion.Euler(0f, 28f * e, 0f);
        if (body != null && body.BossTorso != null) body.BossTorso.localRotation = Quaternion.Euler(55f * e, 0f, 20f * e);
    }

    // ------------------------------------------------------------ the choreography nobody runs here

    public override void SetRise(float k) { }
    public override void SealEntry() { }
    public override void BeginIntro() { }
    public override void OnCountered() { }
    public override GameObject Fall() => vehicle != null ? vehicle.gameObject : gameObject;
    public override void Dismissed() { }
}
