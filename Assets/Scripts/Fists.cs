using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SPEED IS DAMAGE: the multiplier the jab runs on — 1 at a standstill, climbing with every metre per second you
/// carry (horizontal, plus half of anything vertical) up to a ceiling. The whip never does — a ride is not a weapon.
/// </summary>
public static class SpeedDamage
{
    public static float Speed(PlayerMotor motor)
    {
        if (motor == null) return 0f;
        Vector3 v = motor.Velocity;
        return new Vector2(v.x, v.z).magnitude + Mathf.Abs(v.y) * 0.5f;
    }

    public static float Mult(MovementConfig config, PlayerMotor motor)
    {
        if (config == null || motor == null) return 1f;
        float cap = Mathf.Max(1f, config.speedDamageMaxMult);
        return Mathf.Min(cap, 1f + Speed(motor) * Mathf.Max(0f, config.speedDamagePerMps));
    }
}

/// <summary>
/// THE KNUCKLES (v3, 2026-10-16: THE COMBO and THE KICK; the fan, the lunge and the tackle-through of 2026-10-06/07
/// are untouched). THE ONE KIT (the simplification, 2026-10-16): the hands are the whole game's weapon — there is no
/// cloakroom, no class, nothing to switch to. The jab is what breaks a unit and what strikes every cyan target in the
/// world (IStrikeTarget: locking bars, breakers, relief valves, panes — cyan is the thing you break); the kick is the
/// platformer's verb (enemies are platforms). Both keys are read here, each kept inputBuffer seconds so no press is
/// dropped and none fires late.
///  · THE COMBO — the strike button cycles JAB → CROSS → HOOK: each press inside fistComboWindow of the last is the
///    next step, a pause goes back to the jab. JAB: narrow and fast, the fan's target at fistReach. CROSS: the same
///    at fistReach × fistCrossReachMult, the root turning with it (the viewmodel). HOOK: a WIDE SWEEP — its own fan
///    of lines across hookArc out to hookReach — that damages EVERY unit it meets (hookDamage × SpeedDamage, each
///    unit once; a weakspot on any line is that unit's EXECUTE) and knocks them SIDEWAYS along the sweep (Enemy.Toss
///    hookKnockSide + up hookKnockUp): the brawler's crowd tool, wider than the cane's line. Every step keeps the
///    jab's timings and commit rules (fistCommit, fistCooldown, fistWhiffRecovery, the lunge at a body in the window,
///    the tackle's tap at tackle speed). MoveType.Fist's detail stays MISS / HIT / EXECUTE / TACKLE / PERFECT TACKLE
///    (the Ward's lesson and the baseline read those exact words); the step is LastStep (JAB / CROSS / HOOK) and the
///    next press's is ComboStep.
///  · THE WINDOW (2026-10-07) — a fist is not precise, so the probe is a WIDE, SHORT FAN, not one line down the
///    crosshair — fistSweepCasts fat sphere casts across ±fistSweepAngle, in three rows (level, ±fistSweepPitch),
///    each line stopped by the first wall. A weakspot in reach on any line is an EXECUTE (the line nearest the centre
///    picks it); else the nearest body in reach takes the step's damage. Parry THEN punch: the arm is committed for a beat.
///  · LUNGE — a live unit in the window inside fistLungeRange makes the press a closer: for a beat your horizontal
///    velocity is held TOWARD IT at max(what you had, fistLungeSpeed), and the floor is capped at a hop's worth over
///    your run (the motor's ApplyLunge). Attributed; never more than a hop adds; no pip; never reaches tackleMinSpeed
///    on its own.
///  · TACKLE-THROUGH — at tackleMinSpeed or more with Strike down, a unit met from the side is not a wall: the
///    motor hands the contact here, the unit takes speed × tackleDamagePerSpeed (an execute at tackleExecuteSpeed
///    with the contact at chest height), and you keep going through it. LAZY (held) keeps your speed; PERFECT
///    (tapped inside tacklePerfectWindow before the contact) pays the motor's perfect tier and a pip.
///  · THE KICK (its own key, v3) — a front kick through the fan at kickReach: the unit it lands on is LAUNCHED
///    (Toss kickLaunchAway + up kickLaunchUp, kickDamage; a boss takes the chip; a weakspot executes). If you are
///    AIRBORNE or above run speed the contact is a SPRINGBOARD: you are thrown up at jumpImpulse and back along your
///    input (no input: straight back off the body), the horizontal at max(what you had, kickSpringSpeed) capped like
///    the lunge — max(current, run) × the tier, the tier only on a PERFECT kick (inside ±kickPerfectWindow of a jump's
///    apex: the bell) — through the motor's Launch (attributed, MoveType.Kick "SPRINGBOARD"); the double jump resets:
///    jump off heads. A whiff commits the arms kickWhiffCommit.
/// Skill-gated momentum: nothing here beats a perfect bunny hop. Hidden tech: every velocity change is attributed
/// (the motor's Lunge and Launch, and the tackle through the motor's hook).
/// </summary>
public class Fists : MonoBehaviour
{
    public static Fists Instance { get; private set; }
    public MovementConfig config;
    public PlayerMotor motor;
    public PlayerLook look;

    /// What is under the fan: nothing, a body, or a weakspot (cyan: the execute)
    public enum AimKind { None, Body, Weakspot }

    /// The hands are the weapon: wired and awake (a cutscene disables the component to take them)
    public bool Active => enabled && config != null && motor != null;

    /// What a punch would meet right now anywhere in THE WINDOW (the fan, in reach): the HUD brackets read it
    public AimKind Aim { get; private set; }
    /// The unit the press is for: the window's target in reach, else the closer's body out to lunge range
    public Enemy AimEnemy { get; private set; }
    /// Where the press connects (the chosen hit's point), and how far that is
    public Vector3 AimPoint { get; private set; }
    public float AimDistance { get; private set; } = float.PositiveInfinity;
    /// The way the closer goes: toward AimEnemy's hit, the view forward when there is none (the motor flattens it)
    public Vector3 LungeDir { get; private set; } = Vector3.forward;
    /// A body in the window inside lunge range: the next press closes on it
    public bool BodyInLunge => AimEnemy != null && config != null && AimDistance <= config.fistLungeRange;
    /// A body (or a weakspot) in the window and in reach: the knuckle light's cyan
    public bool BodyInWindow => Aim != AimKind.None;
    /// A cyan target (a breaker, a lock, a valve, a pane) in the window and in reach: the next press breaks it
    public bool TargetInWindow => probeTarget != null;
    /// 0 = just punched, 1 = ready
    public float Readiness => config == null ? 1f : Mathf.Clamp01(1f - (readyAt - Time.time) / Mathf.Max(0.01f, lastRecovery > 0f ? lastRecovery : config.fistCooldown));

    /// 0 = the right hand (the jab, the hook), 1 = the left (the cross): the gauntlets read it
    public static int JabIndex { get; private set; }
    /// THE COMBO: the step the NEXT press throws — 0 the jab, 1 the cross, 2 the hook (the viewmodel and the HUD read it)
    public static int ComboStep { get; private set; }
    /// The step the LAST punch was: JAB / CROSS / HOOK (stamped before MoveType.Fist goes out)
    public static string LastStep { get; private set; } = "JAB";
    public static string NextStep => StepName(ComboStep);
    public static string StepName(int step) => step == 2 ? "HOOK" : step == 1 ? "CROSS" : "JAB";
    /// How many units the last hook met, and the way it swept (world, flat: the knock's direction)
    public static int LastHookHits { get; private set; }
    public static Vector3 LastSweepDir = Vector3.left;
    /// Where the last punch or tackle connected (GameFeel bursts here)
    public static Vector3 LastHitPoint;
    /// The way the last punch went, and the way the last tackle travelled (ragdolls read them)
    public static Vector3 LastHitDir = Vector3.forward;
    /// THE EXECUTE's cut plane (the weakspot taken by a jab, a hook or a kick): where, the way it went in, and the sweep
    /// across it — the ragdoll's head comes off along these (Enemy reads them for KillMethod.SwordExecute)
    public static Vector3 LastStrikePoint, LastStrikeForward = Vector3.forward, LastStrikeSweep = Vector3.right;
    public static Vector3 LastTackleDir = Vector3.forward;
    public static float LastTackleSpeed;
    /// The SpeedDamage multiplier the last punch went out at
    public static float LastMult = 1f;
    /// Unscaled stamps the gauntlets flash on: white-hot on a connect, gold on a perfect tackle or a perfect springboard
    public static float LastConnectAt = -10f, LastPerfectTackleAt = -10f;

    // THE KICK (the knuckles' alt cast, v3)
    /// 0 = the kick has just gone (or the arms are committed by its whiff), 1 = a kick can go
    public float KickReadiness => config == null ? 1f : Mathf.Clamp01(1f - (kickReadyAt - Time.time) / Mathf.Max(0.01f, lastKickRecovery > 0f ? lastKickRecovery : config.kickCooldown));
    /// Unscaled stamps for the viewmodel and GameFeel: the kick went; it was a springboard; it was a perfect one
    public static float LastKickAt = -10f, LastPerfectKickAt = -10f;
    public static bool LastKickSprang;
    float kickReadyAt, lastKickRecovery;
    bool rose;   // this airtime has had a real rise: its apex is a jump's, not a ledge walked off

    float readyAt, lastRecovery;
    float lastPunchAt = float.NegativeInfinity;
    float strikePressedAt = float.NegativeInfinity;
    float strikeWantAt = -10f, kickWantAt = -10f;   // the last press of each key: kept inputBuffer seconds while the hands are busy, never longer
    bool strikeHeld;
    float Buffer => Mathf.Max(0f, config.inputBuffer);
    float lastLockAt = -10f;
    IStrikeTarget probeTarget; // a breaker or a lock in the window, in reach
    // Units tackled this beat: not solid to the capsule, and not hit again, until the stamp runs out
    readonly Dictionary<Enemy, float> ghosted = new Dictionary<Enemy, float>();
    readonly List<Enemy> expired = new List<Enemy>();
    // The hook's units this sweep: each once
    readonly List<Enemy> hookTouched = new List<Enemy>(16);
    // THE WINDOW's scratch: the fan's local directions (rebuilt only when the config's fan changes), each line's
    // |yaw| and its angular offset from the centre, and one hit buffer every cast reuses — nothing per frame
    Vector3[] fanDirs = System.Array.Empty<Vector3>();
    float[] fanYaw = System.Array.Empty<float>(), fanOff = System.Array.Empty<float>();
    int fanCount, fanBuiltCasts = -1;
    float fanBuiltAngle = float.NaN, fanBuiltPitch = float.NaN;
    readonly RaycastHit[] fanHits = new RaycastHit[32];

    /// How many units are mid-pass (the motor's casts look through them only while this is non-zero)
    public int GhostCount => ghosted.Count;
    public bool Ghosted(Enemy unit) => ghosted.TryGetValue(unit, out float until) && Time.time < until;

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    Quaternion ViewRotation => look != null ? Quaternion.Euler(look.Pitch, look.Yaw, 0f) : transform.rotation;
    Vector3 Eye => motor.CurrPosition + Vector3.up * motor.EyeHeight;

    /// The reach a step probes at: the cross reaches further, the hook its own
    float StepReach(int step) => step == 1 ? config.fistReach * Mathf.Max(1f, config.fistCrossReachMult) : step == 2 ? Mathf.Max(config.fistReach, config.hookReach) : config.fistReach;

    void Update()
    {
        Prune();
        if (PauseMenu.IsPaused || !Active || OpeningCutscene.Active || BossIntro.Active || BossOutro.Active || LevelResults.Active || MineBlast.Active) // the cutscenes and the card own the keys
        {
            Aim = AimKind.None;
            AimEnemy = null;
            probeTarget = null;
            strikeWantAt = kickWantAt = -10f;
            return;
        }
        TrackApex();
        if (ComboStep != 0 && Time.time - lastPunchAt > Mathf.Max(0f, config.fistComboWindow)) ComboStep = 0; // the pause: back to the jab
        Probe(StepReach(ComboStep));
        strikeHeld = Keybinds.Held(GameAction.Strike);
        // THE KICK's key, read here: a press is kept a beat while the arms are busy and goes the instant they are free
        if (Keybinds.Down(GameAction.Kick)) kickWantAt = Time.time;
        if (Time.time - kickWantAt <= Buffer && Kick()) { kickWantAt = -10f; return; }   // one pair of arms: the jab waits for the kick's frame
        // THE JAB's key: the same beat, so a tap inside the recovery is thrown the moment it ends, never dropped
        if (Keybinds.Down(GameAction.Strike)) strikeWantAt = Time.time;
        if (Time.time - strikeWantAt > Buffer || Time.time < readyAt) return;
        strikeWantAt = -10f;
        strikePressedAt = Time.time; // the tackle's timing reads this: only a punch that was thrown opens the window (a mash inside recovery does not)
        Punch();
    }

    /// A jump's apex is where the vertical speed crosses zero in the air: inside ±window of it means |vy| ≤ g × window.
    /// Only after a real rise this airtime (a ledge walked off is not an apex); wall runs hover and do not count
    void TrackApex()
    {
        if (motor.Grounded || motor.WallRunning) { rose = false; return; }
        if (motor.Velocity.y > ApexBand) rose = true;
    }

    float ApexBand => Mathf.Max(0f, config.gravity) * Mathf.Max(0f, config.kickPerfectWindow);
    bool AtApex => !motor.Grounded && !motor.WallRunning && rose && Mathf.Abs(motor.Velocity.y) <= ApexBand;

    /// THE KICK, on its press: through the fan at kickReach. True if it went (the arms were free)
    public bool Kick()
    {
        if (!Active || Time.time < kickReadyAt || motor.ArmCommitted) return false;
        Probe(config.kickReach);
        Quaternion view = ViewRotation;
        Vector3 forward = view * Vector3.forward;
        Vector3 hitDir = AimEnemy != null || probeTarget != null ? AimPoint - Eye : forward;
        hitDir = hitDir.sqrMagnitude > 1e-4f ? hitDir.normalized : forward;
        LastHitDir = hitDir;
        LastMult = 1f;
        LastKickAt = Time.unscaledTime;
        JabIndex = 0;
        string outcome = "MISS";
        bool contact = false, body = false;
        if (AimEnemy != null && Aim == AimKind.Weakspot)
        {
            outcome = "EXECUTE";
            contact = body = true;
            LastStrikePoint = AimPoint;
            LastStrikeForward = hitDir;
            LastStrikeSweep = view * Vector3.right;
            AimEnemy.Damage(float.MaxValue, KillMethod.SwordExecute);
            if (CombatDirector.Instance != null) CombatDirector.Instance.ReportExecute();
        }
        else if (AimEnemy != null && Aim == AimKind.Body)
        {
            var unit = AimEnemy;
            unit.Damage(config.kickDamage, KillMethod.Fist);
            outcome = "HIT";
            contact = body = true;
            if (unit.Alive && unit.kind != EnemyKind.Boss)
            {
                // LAUNCHED: away from you along the floor, and up
                Vector3 away = unit.Center - motor.CurrPosition;
                away.y = 0f;
                away = away.sqrMagnitude > 1e-4f ? away.normalized : new Vector3(forward.x, 0f, forward.z).normalized;
                unit.Toss(away * config.kickLaunchAway + Vector3.up * config.kickLaunchUp);
                if (unit.Tossed) outcome = "LAUNCH";
            }
        }
        else if (probeTarget != null && probeTarget.Struck(AimPoint, false)) { outcome = "HIT"; contact = true; }
        if (contact)
        {
            LastHitPoint = AimPoint;
            LastConnectAt = Time.unscaledTime;
        }
        // THE SPRINGBOARD: a body under the boot while you are airborne or above run speed throws YOU
        bool sprang = false, perfect = false;
        if (body && (!motor.Grounded || motor.HorizontalSpeed > config.baseRunSpeed))
        {
            perfect = AtApex;
            sprang = Springboard(view, perfect);
        }
        LastKickSprang = sprang;
        if (sprang && perfect) LastPerfectKickAt = Time.unscaledTime;
        bool whiff = !contact;
        if (whiff) motor.CommitArm(config.kickWhiffCommit);
        lastKickRecovery = whiff ? Mathf.Max(config.kickWhiffCommit, config.fistWhiffRecovery) : config.kickCooldown;
        kickReadyAt = Time.time + lastKickRecovery;
        // One pair of arms: the punch waits for the kick's beat too
        readyAt = Mathf.Max(readyAt, Time.time + (whiff ? config.kickWhiffCommit : config.fistCooldown));
        MovementEvents.Emit(MoveType.Kick, sprang ? (perfect ? "SPRINGBOARD  PERFECT" : "SPRINGBOARD") : outcome);
        return true;
    }

    /// Off the body: up at jumpImpulse, and along your input in the look's yaw frame (none: straight back). The
    /// horizontal is max(what you had, kickSpringSpeed) under the lunge's cap — max(current, run), × the tier only on a
    /// PERFECT kick — so a lazy springboard is a redirect and never out-runs a timed hop. Through Launch: attributed,
    /// the ground let go of; the double jump resets
    bool Springboard(Quaternion view, bool perfect)
    {
        Vector3 v = motor.Velocity;
        float h = new Vector2(v.x, v.z).magnitude;
        Vector3 yawF = view * Vector3.forward;
        yawF.y = 0f;
        yawF = yawF.sqrMagnitude > 1e-4f ? yawF.normalized : Vector3.forward;
        Vector3 yawR = Vector3.Cross(Vector3.up, yawF);
        float fx = (Keybinds.Held(GameAction.Right) ? 1f : 0f) - (Keybinds.Held(GameAction.Left) ? 1f : 0f);
        float fz = (Keybinds.Held(GameAction.Forward) ? 1f : 0f) - (Keybinds.Held(GameAction.Back) ? 1f : 0f);
        Vector3 dir = yawF * fz + yawR * fx;
        if (dir.sqrMagnitude < 1e-4f) dir = -yawF;
        dir.Normalize();
        float cap = Mathf.Max(h, config.baseRunSpeed) * (perfect ? motor.PerfectTierMultiplier(config.perfectBhopSpeedMultiplier, config.perfectBhopMinMultiplier) : 1f);
        float tier = perfect ? motor.PerfectTierMultiplier(config.tacklePerfectMultiplier, config.tacklePerfectMinMultiplier) : 1f;
        float s = Mathf.Min(Mathf.Max(h, config.kickSpringSpeed) * tier, cap);
        s = Mathf.Max(s, Mathf.Min(h, cap));   // never slower than you were, under the cap
        motor.Launch(dir * s + Vector3.up * Mathf.Max(0f, config.jumpImpulse));
        motor.RefreshAirJumps();
        rose = false;   // the apex is spent: the next perfect needs a new rise
        return true;
    }

    void Prune()
    {
        if (ghosted.Count == 0) return;
        foreach (var kv in ghosted) if (kv.Key == null || Time.time >= kv.Value) expired.Add(kv.Key);
        foreach (var e in expired) ghosted.Remove(e);
        expired.Clear();
    }

    /// The fan's lines in the view's own frame, rebuilt only when the config's fan changes: three rows (level,
    /// raised, lowered) of casts across ±fistSweepAngle, the centre line among them
    void BuildFan()
    {
        int casts = Mathf.Max(1, config.fistSweepCasts) | 1; // odd, so the centre line exists
        float angle = Mathf.Max(0f, config.fistSweepAngle), pitch = Mathf.Max(0f, config.fistSweepPitch);
        if (casts == fanBuiltCasts && angle == fanBuiltAngle && pitch == fanBuiltPitch) return;
        fanBuiltCasts = casts; fanBuiltAngle = angle; fanBuiltPitch = pitch;
        int rows = pitch > 0f ? 3 : 1;
        fanCount = casts * rows;
        if (fanDirs.Length < fanCount)
        {
            fanDirs = new Vector3[fanCount];
            fanYaw = new float[fanCount];
            fanOff = new float[fanCount];
        }
        int n = 0;
        for (int r = 0; r < rows; r++)
        {
            float p = r == 0 ? 0f : r == 1 ? -pitch : pitch; // level, raised (Euler x negative looks up), lowered
            for (int i = 0; i < casts; i++)
            {
                float yaw = casts == 1 ? 0f : Mathf.Lerp(-angle, angle, i / (float)(casts - 1));
                fanDirs[n] = Quaternion.Euler(p, yaw, 0f) * Vector3.forward;
                fanYaw[n] = Mathf.Abs(yaw);
                fanOff[n] = Mathf.Sqrt(yaw * yaw + p * p);
                n++;
            }
        }
    }

    /// Insertion sort of the hit buffer's first `n` by distance: a handful of hits, no allocation
    void SortHits(int n)
    {
        for (int i = 1; i < n; i++)
        {
            RaycastHit h = fanHits[i];
            int j = i - 1;
            while (j >= 0 && fanHits[j].distance > h.distance) { fanHits[j + 1] = fanHits[j]; j--; }
            fanHits[j + 1] = h;
        }
    }

    /// THE WINDOW, every frame: a wide, short fan of fat casts from the eye, each line ending at its first wall.
    /// What the fist would meet — a weakspot in reach on any line (the line nearest the centre picks it), else the
    /// nearest body in reach, else a breaker or a lock in reach — and, with nothing in reach, whether a body in the
    /// closer's part of the fan is close enough to lunge at
    void Probe(float reach)
    {
        BuildFan();
        Vector3 origin = Eye;
        Quaternion view = ViewRotation;
        Vector3 forward = view * Vector3.forward;
        float range = Mathf.Max(reach, config.fistLungeRange), radius = config.fistRadius;
        float lungeYaw = Mathf.Max(0f, config.fistLungeSweepAngle);
        AimKind was = Aim;
        Aim = AimKind.None;
        AimEnemy = null;
        probeTarget = null;
        AimDistance = float.PositiveInfinity;
        AimPoint = origin + forward * Mathf.Min(1.5f, reach);
        LungeDir = forward;

        // The candidates across the fan: a weakspot in reach (nearest the centre), the nearest body in reach, a
        // breaker or a lock in reach (nearest the centre), and the closer's body out of reach (nearest the centre)
        Enemy weak = null, body = null, far = null;
        IStrikeTarget target = null;
        Vector3 weakPoint = default, bodyPoint = default, farPoint = default, targetPoint = default;
        float inf = float.PositiveInfinity;
        float weakOff = inf, weakDist = inf, bodyDist = inf, bodyOff = inf, farOff = inf, farDist = inf, targetOff = inf, targetDist = inf;

        for (int l = 0; l < fanCount; l++)
        {
            Vector3 dir = view * fanDirs[l];
            float off = fanOff[l];
            int n = Physics.SphereCastNonAlloc(origin, radius, dir, fanHits, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            SortHits(n);
            for (int k = 0; k < n; k++)
            {
                RaycastHit h = fanHits[k];
                Vector3 point = h.distance > 0f ? h.point : h.collider.bounds.ClosestPoint(origin); // a start-overlap reports no point
                var e = h.collider.GetComponentInParent<Enemy>();
                if (e == null)
                {
                    // Not a body: a breaker or a lock takes the punch, anything else is a wall, and either way this line ends here
                    if (h.distance <= reach && off < targetOff)
                    {
                        var t = h.collider.GetComponentInParent<IStrikeTarget>();
                        if (t != null) { target = t; targetOff = off; targetPoint = point; targetDist = h.distance; }
                    }
                    break;
                }
                if (!e.Alive) continue; // a corpse is not a wall
                // The first live body on the line is what the fist meets there
                if (h.distance <= reach)
                {
                    if (h.collider.GetComponent<EnemyWeakspot>() != null)
                    {
                        if (off < weakOff) { weak = e; weakOff = off; weakPoint = point; weakDist = h.distance; }
                    }
                    else if (h.distance < bodyDist || (h.distance == bodyDist && off < bodyOff))
                    {
                        body = e; bodyDist = h.distance; bodyOff = off; bodyPoint = point;
                    }
                }
                else if (fanYaw[l] <= lungeYaw && (off < farOff || (off == farOff && h.distance < farDist)))
                {
                    far = e; farOff = off; farDist = h.distance; farPoint = point;
                }
                break;
            }
        }

        // The window decides: a weakspot over a body over a lock, all in reach; with nothing in reach, the closer's body
        if (weak != null) { Aim = AimKind.Weakspot; AimEnemy = weak; AimPoint = weakPoint; AimDistance = weakDist; }
        else if (body != null) { Aim = AimKind.Body; AimEnemy = body; AimPoint = bodyPoint; AimDistance = bodyDist; }
        else if (target != null) { probeTarget = target; AimPoint = targetPoint; AimDistance = targetDist; }
        else if (far != null) { AimEnemy = far; AimPoint = farPoint; AimDistance = farDist; }
        if (AimEnemy != null) LungeDir = AimPoint - origin;

        if (Aim == AimKind.Weakspot)
        {
            AimEnemy.MarkTargeted();
            if (was != AimKind.Weakspot && Time.unscaledTime - lastLockAt > 0.15f && GameFeel.Instance != null)
            {
                lastLockAt = Time.unscaledTime;
                GameFeel.Instance.PlayLockOn();
            }
        }
    }

    /// One punch of the combo: the closer if a body is near, the hit if one is in reach, the whiff otherwise; the
    /// hook sweeps the crowd on top of its target
    void Punch()
    {
        int step = ComboStep;
        LastStep = StepName(step);
        JabIndex = step == 1 ? 1 : 0;          // the cross is the left hand; the jab and the hook are the right's
        ComboStep = (step + 1) % 3;
        lastPunchAt = Time.time;
        motor.CancelParry();                 // a punch through an open guard throws the guard away
        motor.CommitArm(config.fistCommit);  // and the arm is spoken for: parry THEN punch
        Quaternion view = ViewRotation;
        Vector3 forward = view * Vector3.forward;
        // The punch goes from the eye to the window's target when there is one: a body off the crosshair is hit on its own line, not along the view
        Vector3 hitDir = AimEnemy != null || probeTarget != null ? AimPoint - Eye : forward;
        hitDir = hitDir.sqrMagnitude > 1e-4f ? hitDir.normalized : forward;
        LastHitDir = hitDir;
        LastMult = SpeedDamage.Mult(config, motor);
        bool hook = step == 2;
        float damage = (hook ? config.hookDamage : config.fistDamage) * LastMult;
        Vector3 sweep = view * Vector3.left;   // the hook travels right → left across the frame: bodies go left
        sweep.y = 0f;
        sweep = sweep.sqrMagnitude > 1e-4f ? sweep.normalized : Vector3.left;
        LastSweepDir = sweep;
        LastHookHits = 0;

        // The closer: a body in the window, and the press carries you toward it (the motor flattens the line; floor and cap are its own)
        bool lunged = BodyInLunge;
        if (lunged) motor.Lunge(LungeDir, config.fistLungeSpeed, config.fistLungeDuration);

        string outcome = "MISS";
        bool tackling = false;
        Enemy main = null;
        if (AimEnemy != null && Aim == AimKind.Weakspot)
        {
            // THE EXECUTE: the cut plane for the ragdoll, the reward
            outcome = "EXECUTE";
            main = AimEnemy;
            LastStrikePoint = AimPoint;
            LastStrikeForward = hitDir;
            LastStrikeSweep = view * Vector3.right;
            AimEnemy.Damage(float.MaxValue, KillMethod.SwordExecute);
            if (CombatDirector.Instance != null) CombatDirector.Instance.ReportExecute();
        }
        else if (AimEnemy != null && Aim == AimKind.Body)
        {
            main = AimEnemy;
            // At tackle speed the press is the tackle's tap: the contact a tick later pays, not the fist (one press, one hit)
            if (motor.HorizontalSpeed >= config.tackleMinSpeed) tackling = true;
            else
            {
                outcome = "HIT";
                AimEnemy.Damage(damage, KillMethod.Fist);
                if (hook && AimEnemy.Alive && AimEnemy.kind != EnemyKind.Boss) AimEnemy.Toss(sweep * config.hookKnockSide + Vector3.up * config.hookKnockUp);
            }
        }
        else if (probeTarget != null && probeTarget.Struck(AimPoint, false))
        {
            outcome = "HIT"; // a cyan target takes the punch: a breaker, a lock, a valve, a pane
        }
        if (main != null && !tackling) LastHookHits = 1;
        // THE HOOK's sweep: every other unit in its arc, each once
        if (hook)
        {
            int swept = Sweep(view, Eye, main, damage, sweep, out bool executed);
            LastHookHits += swept;
            if (swept > 0 && outcome == "MISS") outcome = executed ? "EXECUTE" : "HIT";
            else if (executed && outcome == "HIT") outcome = "EXECUTE";
        }
        if (outcome != "MISS")
        {
            if (main == null || tackling) LastHitPoint = AimEnemy != null ? AimPoint : LastHitPoint; else LastHitPoint = AimPoint;
            LastConnectAt = Time.unscaledTime;
        }

        // A punch that meets nothing is gathered back slowly, unless it was the closer: that one is still going
        bool whiff = outcome == "MISS" && !lunged && !tackling;
        lastRecovery = whiff ? Mathf.Max(config.fistCooldown, config.fistWhiffRecovery) : config.fistCooldown;
        readyAt = Time.time + lastRecovery;
        if (!tackling) MovementEvents.Emit(MoveType.Fist, outcome); // the tackle emits its own
    }

    /// THE HOOK's own fan: hookCasts lines across hookArc out to hookReach (fat casts, each line stopped by its first
    /// wall, through every body on it); every live unit met once — a weakspot on any line is that unit's EXECUTE, a
    /// body takes `damage` and is knocked sideways along the sweep. Returns how many it met beyond `skip`
    int Sweep(Quaternion view, Vector3 origin, Enemy skip, float damage, Vector3 sweep, out bool executed)
    {
        executed = false;
        hookTouched.Clear();
        if (skip != null) hookTouched.Add(skip);
        float half = Mathf.Max(0f, config.hookArc) * 0.5f;
        int casts = Mathf.Max(1, config.hookCasts) | 1;
        float reach = Mathf.Max(0.5f, config.hookReach), radius = config.fistRadius;
        int hits = 0;
        for (int i = 0; i < casts; i++)
        {
            float yaw = casts == 1 ? 0f : Mathf.Lerp(-half, half, i / (float)(casts - 1));
            Vector3 dir = view * (Quaternion.Euler(0f, yaw, 0f) * Vector3.forward);
            int n = Physics.SphereCastNonAlloc(origin, radius, dir, fanHits, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            SortHits(n);
            for (int k = 0; k < n; k++)
            {
                RaycastHit h = fanHits[k];
                var e = h.collider.GetComponentInParent<Enemy>();
                if (e == null) break;   // a wall ends the line (breakers and locks are the main probe's)
                if (!e.Alive || hookTouched.Contains(e)) continue;
                hookTouched.Add(e);
                Vector3 point = h.distance > 0f ? h.point : e.Center;
                if (h.collider.GetComponent<EnemyWeakspot>() != null)
                {
                    LastStrikePoint = point;
                    LastStrikeForward = dir;
                    LastStrikeSweep = sweep;
                    e.Damage(float.MaxValue, KillMethod.SwordExecute);
                    if (!e.Alive) { executed = true; if (CombatDirector.Instance != null) CombatDirector.Instance.ReportExecute(); }
                }
                else
                {
                    e.Damage(damage, KillMethod.Fist);
                    if (e.Alive && e.kind != EnemyKind.Boss) e.Toss(sweep * config.hookKnockSide + Vector3.up * config.hookKnockUp);
                }
                hits++;
                LastHitPoint = point;
            }
        }
        return hits;
    }

    /// The motor's hook, from MoveAndSlide, on a wall-angled contact with a unit: true if you went through it.
    /// `velocity` is the motor's own; the motor attributes whatever this does to it.
    public bool TryTackle(Enemy unit, ref Vector3 velocity, RaycastHit hit)
    {
        if (!Active || unit == null || !unit.Alive) return false;
        if (unit.kind == EnemyKind.Boss) return false;                     // five times your size: a wall, not a body
        if (ghosted.TryGetValue(unit, out float until) && Time.time < until) return false; // one hit per pass
        float h = new Vector2(velocity.x, velocity.z).magnitude;
        if (h < config.tackleMinSpeed) return false;
        float since = Time.time - strikePressedAt;
        bool inWindow = since <= config.tacklePerfectWindow;
        bool held = strikeHeld && !inWindow;                               // a sustained hold, from before the window
        if (!inWindow && !held) return false;                              // Strike is not down: the unit is a wall
        bool perfect = inWindow && !held && Time.time - motor.LastDashTime > 0.5f;

        ghosted[unit] = Time.time + config.tackleUnitCooldown;
        Vector3 travel = new Vector3(velocity.x, 0f, velocity.z) / h;
        LastTackleDir = travel;
        LastTackleSpeed = h;
        LastHitPoint = hit.point;
        LastHitDir = travel;
        LastMult = SpeedDamage.Mult(config, motor);

        // Chest height or above, at execute speed, is an execute: the unit's whole health. Cover, a guard or armour
        // can still keep it alive, and an execute is only ever a kill
        float contactY = Mathf.Max(hit.point.y, motor.CurrPosition.y + motor.CurrentHeight * 0.6f);
        bool execute = h >= config.tackleExecuteSpeed && contactY >= unit.Center.y - 0.3f;
        float damage = h * config.tackleDamagePerSpeed;
        if (execute) damage = Mathf.Max(damage, unit.MaxHp);
        unit.Damage(damage, KillMethod.Tackle);
        execute = execute && !unit.Alive;
        if (execute && CombatDirector.Instance != null) CombatDirector.Instance.ReportExecute();

        if (perfect)
        {
            // The timing pays, under the perfect hop at every speed, and once per tap: the next body in this pass is lazy
            float mult = motor.PerfectTierMultiplier(config.tacklePerfectMultiplier, config.tacklePerfectMinMultiplier);
            velocity = new Vector3(velocity.x * mult, velocity.y, velocity.z * mult);
            strikePressedAt = float.NegativeInfinity;
            LastPerfectTackleAt = Time.unscaledTime;
            if (CombatDirector.Instance != null) CombatDirector.Instance.ReportPerfectTackle();
        }
        LastConnectAt = Time.unscaledTime;
        MovementEvents.Emit(MoveType.Fist, execute ? "EXECUTE" : perfect ? "PERFECT TACKLE" : "TACKLE");
        return true;
    }
}
