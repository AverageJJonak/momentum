using UnityEngine;

/// What a unit DOES, since the simplification (2026-10-16): four behaviours and the practice dummy. Every EnemyKind
/// keeps its own rig, livery and name (the visual variety stays) but is MAPPED onto one of these at the factory
/// (Enemy.RoleOf), so no level file changes and no unit is a fight.
public enum EnemyRole
{
    /// THE PLATFORM (the bellhop and everyone who used to chase, fly or skitter): walks its line at a readable pace,
    /// STOPS and braces when you are within FootmanBraceRange — a stable springboard — and never chases. Its touch
    /// hurts a little and shoves, never kills. One jab ends it; a kick launches it and the landing ends it.
    Footman,
    /// THE REASON TO MOVE (the doorman, and the ranged crew): stands still, a slow readable aim (the lamp goes amber,
    /// then red for the last beat) and ONE straight shot you dodge by moving. Killing it is optional.
    Shooter,
    /// THE WALL (the bouncer): blocks a lane, never pursues, shoves if touched. Jabs chip it in three; the glass head
    /// is still the execute. It is never tossed: the best springboard in the game stays standing under the kick.
    Bouncer,
    /// The page's sniper laser: a fixed hazard with a tell, as built (EnemySniper)
    Turret,
    /// The practice dummy, as built: a target that plinks flashbulbs on the range and in the tutorial only
    Dummy,
    /// Driven by its own controller (BossConcierge / BossForeman); nothing here touches it
    Boss,
}

/// <summary>
/// THE ROSTER AS PLATFORMS AND FUEL. "Movement is the focus, not combat": a unit on a course is something to jab
/// through, kick off, pogo on or run past — never a fight. Everything in here is PREDICTABLE: no random timings;
/// every offset is seeded from where the unit was built (Phase), tells are the unit's own lamp (amber = braced or
/// aiming, red = the shot is coming), and the cyan glass head is the execute on every one of them.
/// </summary>
public partial class Enemy
{
    // ---------------------------------------------------------------- the numbers

    /// The footman stops and braces inside this: a springboard that holds still while you line it up
    public const float FootmanBraceRange = 12f;
    /// Its walk: a readable pace, a short leash either side of its post
    const float FootmanWalkSpeed = 1.6f, FootmanLeash = 4f;
    /// The touch (a grounded, slow player walking into a body): a little damage, a shove, a beat before the next
    const float TouchDamage = 6f, TouchShoveSpeed = 5f, TouchCooldown = 0.9f, BouncerShoveSpeed = 8f;
    /// The shooter: the aim (amber, then red for the last ShooterRedBeat), one shot every ShooterCycle, 20 damage,
    /// a lead that never anticipates a moving target by much
    const float ShooterAim = 1.2f, ShooterRedBeat = 0.3f, ShooterCycle = 2.5f, ShooterDamage = 20f, ShooterLead = 0.25f, ShooterBoltLife = 2.5f;
    /// The bouncer: a jab chips a third of it, whatever speed you brought
    const int BouncerChips = 3;

    /// Which behaviour a kind runs. The code names never change (the rigs, liveries and name plates key on them);
    /// the behaviour is one of four. Ranged kinds became shooters; everything that chased, hopped, flew or hid is a
    /// footman (a lurker is a footman that stands); the warden is the bouncer.
    public static EnemyRole RoleOf(EnemyKind k) => k switch
    {
        EnemyKind.Dummy => EnemyRole.Dummy,
        EnemyKind.Turret => EnemyRole.Turret,
        EnemyKind.Boss => EnemyRole.Boss,
        EnemyKind.Warden => EnemyRole.Bouncer,
        EnemyKind.Sentry => EnemyRole.Shooter,
        EnemyKind.Blaster => EnemyRole.Shooter,
        EnemyKind.Rigger => EnemyRole.Shooter,
        _ => EnemyRole.Footman,
    };

    /// The behaviour this unit runs (set at the factory, Enemy.Create)
    public EnemyRole role;

    /// The civic rig (BuildRig): the footman, the page, the bellhop — the only rigs whose right arm rests at ArmRRest
    static bool IsCivic(EnemyKind k) => k == EnemyKind.Dummy || k == EnemyKind.Turret || k == EnemyKind.Chaser;

    /// The damage that is yours to feed on: the hands, the slam, a counter, a shot sent back
    static bool IsYours(KillMethod m) => m == KillMethod.Fist || m == KillMethod.Tackle || m == KillMethod.SwordExecute
        || m == KillMethod.SlamShockwave || m == KillMethod.MeleeParry || m == KillMethod.ReflectedProjectile;

    /// A footman that stands where it was put (a valet, or anyone posted on a lip, a rail or a ledge with no room to walk)
    bool fmPosted, fmProbed, fmBraced;
    Vector3 fmHeading = Vector3.forward;
    float fmDir = 1f, touchReadyAt;
    // The shooter
    float shAimUntil, shNextAt, shPitch;
    bool shRed;

    static Material braceMat;
    /// The lamp, braced or aiming: the unit's own amber, brighter
    static Material BraceMat() => braceMat ??= Unlit(new Color(3.2f, 2f, 0.5f));

    /// The HUD's heavy bar reads these off Enemy.Janitors; nothing joins that list now, so they are never shown
    public bool HeavyOpen => false;
    public string HeavyName => StaffName(kind);
    public string HeavyOpenNote => "";

    // ---------------------------------------------------------------- shared

    float circlePhase = -1f;
    /// A per-unit offset derived from where it was built: every stagger of timing in the roster comes from this, never
    /// from Random, so the same level plays the same way twice
    float Phase
    {
        get
        {
            if (circlePhase < 0f) circlePhase = Mathf.Abs(spawnPos.x * 0.37f + spawnPos.z * 0.61f) % 6.283f;
            return circlePhase;
        }
    }

    /// Born, and back from the dead (the range): every timer as it was built
    void RoleReset()
    {
        float now = Time.time;
        fmBraced = false;
        fmProbed = false;
        fmDir = 1f;
        touchReadyAt = now + 0.5f;
        shAimUntil = 0f;
        shRed = false;
        shNextAt = now + 1f + Phase * 0.3f;   // a row of doormen fires in its own order, the same order every time
        SetLamp(LampRest());
    }

    /// The lamp a unit was built with: a Works lamp and a Response lightbar are amber, the civic lamp is cream
    Material LampRest() => IsResponse(kind) || IsVariety(kind) ? ResponseLamp() : IsWorks(kind) ? WorksLamp() : StatusMat();

    void SetLamp(Material m)
    {
        if (rig != null && rig.status != null && rig.status.sharedMaterial != m) rig.status.sharedMaterial = m;
    }

    /// Colliders back on after a cast — except a sealed weakspot's (a boss's), which stays off
    void RestoreColliders()
    {
        foreach (var c in colliders) if (c != null) c.enabled = true;
        if (weakspotClosed && rig != null && rig.head != null)
        {
            var col = rig.head.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }

    /// A clear line from this unit's body to a point (its own colliders step aside; the player is never in a cast's way)
    bool ClearLine(Vector3 from, Vector3 to, float radius)
    {
        Vector3 d = to - from;
        float len = d.magnitude;
        if (len < 0.05f) return true;
        foreach (var c in colliders) if (c != null) c.enabled = false;
        bool hit = Physics.SphereCast(from, radius, d / len, out _, Mathf.Max(0.01f, len - radius), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        RestoreColliders();
        return !hit;
    }

    static bool Near(float dist) => dist < 48f;   // the sounds a unit makes are not positional: only the ones near you make them

    /// One step for a walking unit, in the body's own scale: swept so walls stop it, slid so corners do not trap it,
    /// ground-clamped so it never steps off an edge (no floor ahead = no step: a unit on a lip holds the lip).
    /// Returns false when it could not go that way.
    bool WalkStep(Vector3 next)
    {
        float s = Scale;
        foreach (var c in colliders) if (c != null) c.enabled = false;
        Vector3 step = next - transform.position;
        float r = Girth;
        Vector3 p1 = transform.position + Vector3.up * (r + 0.1f), p2 = transform.position + Vector3.up * Mathf.Max(r + 0.2f, 1.35f * s - r);
        bool blocked = false;
        if (step.sqrMagnitude > 1e-6f && Physics.CapsuleCast(p1, p2, r, step.normalized, out RaycastHit block, step.magnitude + 0.05f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            Vector3 n = block.normal;
            n.y = 0f;
            Vector3 slide = n.sqrMagnitude > 1e-4f ? Vector3.ProjectOnPlane(step, n.normalized) : Vector3.zero;
            if (slide.sqrMagnitude > 1e-4f
                && !Physics.CapsuleCast(p1, p2, r, slide.normalized, out _, slide.magnitude + 0.05f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                next = transform.position + slide;
            else { next = transform.position; blocked = true; }
        }
        bool grounded = Physics.Raycast(next + Vector3.up * (2f * s), Vector3.down, out RaycastHit ground, 5f * s,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        RestoreColliders();
        if (!grounded) return false;
        next.y = ground.point.y;
        moveSpeed = Vector3.Distance(next, transform.position) / Time.fixedDeltaTime;
        transform.position = next;
        return !blocked;
    }

    /// THE TOUCH: a grounded player at a walk who runs into the body takes a little and is shoved off it. Never a
    /// kill: the damage is capped at what leaves you a point. Never while you are airborne or above run speed — that
    /// contact is the kick's springboard or the motor's tackle, and those own it.
    void TouchShove(CombatDirector director, float shove)
    {
        var motor = director.motor;
        if (Time.time < touchReadyAt || !motor.Grounded || motor.HasIFrames) return;
        if (motor.HorizontalSpeed > config.baseRunSpeed) return;
        Vector3 to = motor.CurrPosition - transform.position;
        float dy = to.y;
        to.y = 0f;
        float reach = Girth + 0.5f;
        if (to.sqrMagnitude > reach * reach) return;
        if (dy > 1.6f * Scale || dy < -1.2f) return;   // under its feet or over its head: not a touch
        touchReadyAt = Time.time + TouchCooldown;
        Vector3 away = to.sqrMagnitude > 1e-4f ? to.normalized : transform.forward;
        float damage = TouchDamage;
        if (director.health != null)
            damage = Mathf.Min(damage, Mathf.Max(0f, director.health.Current - 1f) / Mathf.Max(0.01f, config.enemyDamageScale));
        if (damage > 0f) director.ReportPlayerHit(away, damage);
        motor.AddImpulse(away * shove + Vector3.up * 1.5f);
        hitFlinch = Mathf.Max(hitFlinch, 0.5f);   // it braces into the shove
        if (Near(to.magnitude) && GameFeel.Instance != null) GameFeel.Instance.PlayWhiff();
    }

    // ---------------------------------------------------------------- the footman: THE PLATFORM

    /// Where it was put, what it can do there: a heading along or across the course from its own seed, and whether
    /// there is any floor to walk on either way. A unit on a lip, a rail or a ledge holds its spot.
    void FootmanProbe()
    {
        fmProbed = true;
        int quarter = Mathf.FloorToInt(Phase / 6.283f * 4f) & 3;
        fmHeading = quarter == 0 ? Vector3.forward : quarter == 1 ? Vector3.right : quarter == 2 ? Vector3.back : Vector3.left;
        fmPosted = kind == EnemyKind.Lurker || !(CanWalk(fmHeading) || CanWalk(-fmHeading));
        if (!fmPosted && !CanWalk(fmHeading)) fmDir = -1f;
        transform.rotation = Quaternion.LookRotation(fmHeading * fmDir);
    }

    /// Two metres of floor that way, with nothing in the body's way
    bool CanWalk(Vector3 dir)
    {
        float s = Scale, r = Girth;
        foreach (var c in colliders) if (c != null) c.enabled = false;
        Vector3 p1 = transform.position + Vector3.up * (r + 0.1f), p2 = transform.position + Vector3.up * Mathf.Max(r + 0.2f, 1.35f * s - r);
        bool clear = !Physics.CapsuleCast(p1, p2, r, dir, out _, 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        bool floor = clear && Physics.Raycast(transform.position + dir * 2f + Vector3.up * (1f * s), Vector3.down, 2.5f * s, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        RestoreColliders();
        return clear && floor;
    }

    void FootmanTick(CombatDirector director, Vector3 chest, float dist)
    {
        float dt = Time.fixedDeltaTime;
        if (!fmProbed) FootmanProbe();
        if (Time.time < staggeredUntil) { SetSkin(StaggerMat()); return; }   // dazed off a hook: it reels where it stands
        SetSkin(normalMat);
        TouchShove(director, TouchShoveSpeed);

        // THE BRACE: inside the range it stops where it is, squares up and holds — the tell is its lamp going bright
        bool braced = dist <= FootmanBraceRange;
        if (braced != fmBraced)
        {
            fmBraced = braced;
            SetLamp(braced ? BraceMat() : LampRest());
        }
        if (braced) { FaceTowards(chest, 180f, dt); return; }
        if (fmPosted) return;

        // THE WALK: its line, at a readable pace, a leash either side of its post; the leash's end, a wall or a ledge
        // turns it round, and it turns before it steps
        Vector3 heading = fmHeading * fmDir;
        FaceTowards(transform.position + heading * 4f, 240f, dt);
        if (Vector3.Dot(transform.forward, heading) < 0.94f) return;
        Vector3 fromPost = transform.position - spawnPos;
        fromPost.y = 0f;
        bool past = Vector3.Dot(fromPost, heading) >= FootmanLeash;
        if (past || !WalkStep(transform.position + heading * (FootmanWalkSpeed * dt))) fmDir = -fmDir;
    }

    void AnimateFootman(float dt, float now)
    {
        Gait(dt, now, 8f, FootmanWalkSpeed);
        float swing = Mathf.Sin(gaitPhase);
        rig.torso.localScale = Vector3.one;
        if (now < staggeredUntil)
        {
            // Dazed: rocked back, arms slack, head lolling
            rig.torso.localRotation = Quaternion.Euler(-22f + Mathf.Sin(now * 9f) * 3f, 0f, Mathf.Sin(now * 7f) * 4f);
            rig.armL.localRotation = Quaternion.Euler(15f, 0f, 35f);
            rig.armR.localRotation = Quaternion.Euler(15f, 0f, -35f);
            rig.headPivot.localRotation = Quaternion.Euler(-15f, Mathf.Sin(now * 5f) * 20f, 0f);
            return;
        }
        if (fmBraced)
        {
            // THE BRACE: it sets — knees out, a hand's width lower, the arms out from the body, the head on you in clicks
            rig.torso.localPosition = rig.torsoRest + Vector3.down * 0.04f;
            rig.torso.localRotation = Quaternion.Euler(6f + Mathf.Sin(now * 2f) * 1f, 0f, 0f);
            rig.legL.localRotation = Quaternion.Euler(-6f, 0f, 8f);
            rig.legR.localRotation = Quaternion.Euler(6f, 0f, -8f);
            rig.armL.localRotation = Quaternion.Euler(10f, 0f, 28f);
            rig.armR.localRotation = Quaternion.Euler(10f, 0f, -28f);
            rig.headPivot.localRotation = Quaternion.Euler(Click(Mathf.Sin(now * 1.1f) * 4f, 2f), 0f, 0f);
            return;
        }
        // The walk: a gentle arm swing (a wing or a foreleg on the same pivot swings the same), the head scanning in clicks
        rig.armL.localRotation = Quaternion.Euler(-swing * 14f * gait + 4f, 0f, 10f);
        rig.armR.localRotation = Quaternion.Euler(swing * 14f * gait + 4f, 0f, -10f);
        rig.headPivot.localRotation = Quaternion.Euler(0f, Click(Mathf.Sin(now * 0.7f + Phase) * 14f, 7f), 0f);
    }

    // ---------------------------------------------------------------- the shooter: THE REASON TO MOVE

    void ShooterTick(CombatDirector director, Vector3 chest, float dist)
    {
        float now = Time.time, dt = Time.fixedDeltaTime;
        var motor = director.motor;
        if (now < staggeredUntil) { SetSkin(StaggerMat()); SetLamp(LampRest()); shAimUntil = 0f; shRed = false; return; }
        bool sees = dist <= config.sentryRange && ClearLine(Center, chest, 0.15f);
        if (!sees)
        {
            shAimUntil = 0f;
            shRed = false;
            SetSkin(normalMat);
            SetLamp(LampRest());
            transform.Rotate(0f, Mathf.Sin(now * 0.5f + Phase) * 40f * dt, 0f);   // it sweeps its post
            return;
        }
        FaceTowards(chest, config.sentryTurn, dt);

        if (shAimUntil > 0f)
        {
            // THE AIM: the lamp comes up amber and holds on you; the last beat is red, and then the shot
            bool red = shAimUntil - now <= ShooterRedBeat;
            if (red != shRed)
            {
                shRed = red;
                SetLamp(red ? ResponseRed() : BraceMat());
                SetSkin(red ? ResponseRed() : normalMat);
                if (red && Near(dist) && GameFeel.Instance != null) GameFeel.Instance.PlayTellRed();
            }
            if (now < shAimUntil) return;
            shAimUntil = 0f;
            shRed = false;
            Vector3 from = rig != null && rig.muzzle != null ? rig.muzzle.position : Center;
            float flight = Vector3.Distance(from, chest) / Mathf.Max(1f, config.sentryBoltSpeed);
            Vector3 dir = (chest + motor.Velocity * (flight * ShooterLead) - from).normalized;   // a short lead: a moving target is missed
            var bolt = EnemyProjectile.Spawn(config, from + dir * 0.2f, dir, this);
            bolt.Configure(config.sentryBoltSpeed, 0.5f, false, 0f);
            bolt.Tune(0f, ShooterBoltLife, ShooterDamage, 0f, null);   // straight
            recoil = 1f;
            SetSkin(normalMat);
            SetLamp(LampRest());
            shNextAt = now + Mathf.Max(0.2f, ShooterCycle - ShooterAim);
            if (Near(dist) && GameFeel.Instance != null) GameFeel.Instance.PlayEnemyShot();
            return;
        }
        SetSkin(normalMat);
        if (now >= shNextAt)
        {
            shAimUntil = now + ShooterAim;
            SetLamp(BraceMat());
            if (Near(dist) && GameFeel.Instance != null) GameFeel.Instance.PlayLockOn();
        }
    }

    void AnimateShooter(float dt, float now, Vector3 chest, float dist)
    {
        // It stands its post: the body pitches to you while it aims (the post does the turning); the shot kicks it back
        float want = 0f;
        if (shAimUntil > 0f && dist <= config.sentryRange)
        {
            Vector3 to = chest - rig.torso.position;
            float flat = new Vector2(to.x, to.z).magnitude;
            want = Mathf.Clamp(-Mathf.Atan2(to.y, Mathf.Max(0.1f, flat)) * Mathf.Rad2Deg, -40f, 25f) * (kind == EnemyKind.Sentry ? 1f : 0.3f);
        }
        shPitch = Mathf.Lerp(shPitch, want, 1f - Mathf.Exp(-8f * dt));
        rig.torso.localRotation = now < staggeredUntil
            ? Quaternion.Euler(30f, Mathf.Sin(now * 9f) * 10f, 0f)
            : Quaternion.Euler(shPitch - 10f * recoil + Mathf.Sin(now * 2f) * 1f, 0f, 0f);
        rig.torso.localPosition = rig.torsoRest + Vector3.back * (0.05f * recoil);
        rig.headPivot.localRotation = Quaternion.identity;
        if (kind == EnemyKind.Sentry)
        {
            rig.armL.localPosition = new Vector3(-0.14f, -0.02f, 0.3f - 0.08f * recoil);
            rig.armR.localPosition = new Vector3(0.14f, -0.02f, 0.3f - 0.08f * recoil);
        }
        else
        {
            rig.armL.localRotation = Quaternion.Euler(Mathf.Sin(now * 1.7f) * 3f + 4f, 0f, 8f);
            rig.armR.localRotation = Quaternion.Euler(shAimUntil > 0f ? -40f : 4f, 0f, -8f);   // the throwing arm comes up with the aim
            rig.legL.localRotation = Quaternion.identity;
            rig.legR.localRotation = Quaternion.identity;
        }
    }

    // ---------------------------------------------------------------- the bouncer: THE WALL

    /// A jab or a tackle chips a third of it, whatever speed you brought; the glass head is still the execute
    bool BouncerFilter(ref float amount, KillMethod method)
    {
        if (method == KillMethod.Fist || method == KillMethod.Tackle)
        {
            amount = MaxHp / BouncerChips + 0.5f;
            if (GameFeel.Instance != null) GameFeel.Instance.PlayJanitorClang();
        }
        return true;
    }

    void BouncerTick(CombatDirector director, Vector3 chest, float dist)
    {
        float dt = Time.fixedDeltaTime;
        if (Time.time < staggeredUntil) { SetSkin(StaggerMat()); return; }
        SetSkin(normalMat);
        TouchShove(director, BouncerShoveSpeed);
        // It holds its lane: it never takes a step. Inside the brace range it squares up to you and its lamp goes bright
        bool braced = dist <= FootmanBraceRange;
        if (braced != fmBraced)
        {
            fmBraced = braced;
            SetLamp(braced ? BraceMat() : LampRest());
        }
        if (braced) FaceTowards(chest, 120f, dt);
    }

    void AnimateBouncer(float dt, float now)
    {
        gait = 0f;
        moveSpeed = 0f;
        rig.torso.localScale = Vector3.one;
        if (now < staggeredUntil)
        {
            rig.torso.localRotation = Quaternion.Euler(-18f + Mathf.Sin(now * 8f) * 3f, 0f, Mathf.Sin(now * 6f) * 5f);
            rig.armL.localRotation = Quaternion.Euler(40f, 0f, 20f);
            rig.armR.localRotation = Quaternion.Euler(15f, 0f, -35f);
            return;
        }
        // The stance: wide, the guard square (its rest pose holds the plate out), the lance down; braced it leans in
        float lean = fmBraced ? 6f : 0f;
        rig.torso.localPosition = rig.torsoRest + Vector3.down * (fmBraced ? 0.03f : 0f);
        rig.torso.localRotation = Quaternion.Euler(lean + Mathf.Sin(now * 1.6f) * 1.2f, 0f, 0f);
        rig.legL.localRotation = Quaternion.Euler(0f, 0f, 10f);
        rig.legR.localRotation = Quaternion.Euler(0f, 0f, -10f);
        rig.armL.localRotation = Quaternion.Euler(fmBraced ? -8f : 0f, 0f, 0f);
        rig.armR.localRotation = Quaternion.Euler(Mathf.Sin(now * 1.2f) * 2f + 6f, 0f, -6f);
        rig.headPivot.localRotation = Quaternion.Euler(0f, Click(Mathf.Sin(now * 0.5f + Phase) * 10f, 5f), 0f);
    }
}
