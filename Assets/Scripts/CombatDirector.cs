using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Routes combat outcomes into the existing systems: kills into StyleSystem
/// (with variety) and ResetDetector (airborne kill, yank kill full refill),
/// slam shockwaves into enemies, player hits into style penalty + knockback.
/// </summary>
public class CombatDirector : MonoBehaviour
{
    public MovementConfig config;
    public PlayerMotor motor;
    public StyleSystem style;
    public ResetDetector resets;
    public GrappleAbility grapple;
    public PlayerHealth health;

    public static CombatDirector Instance { get; private set; }
    /// Kills this level — the report card's COMBAT share
    public int Kills { get; private set; }
    /// When something last happened in a fight (a kill, a hit taken, an execute, a near miss): the score keeps its guard up a while after
    public float LastActionTime { get; private set; } = float.NegativeInfinity;

    void Awake() => Instance = this;

    void Start()
    {
        // Zero-delay parry: the motor fires this the same frame the button
        // goes down. Priority: projectiles (the skill shot), then enemies;
        // the motor tries walls itself afterward if nothing consumed it.
        if (motor != null) motor.ParryPressed += InstantParryScan;
    }

    void OnEnable() => MovementEvents.MoveExecuted += OnMove;

    void OnDisable()
    {
        MovementEvents.MoveExecuted -= OnMove;
        if (motor != null) motor.ParryPressed -= InstantParryScan;
        if (Instance == this) Instance = null;
    }

    void InstantParryScan()
    {
        // Everything in reach, not the first thing: the guard is up for all of it at once
        foreach (EnemyProjectile p in EnemyProjectile.Active)
            if (p != null) p.TryParryReflect(motor);
        TryEnemyParry();
    }

    void FixedUpdate() => TryEnemyParry();

    // Enemy parry — a COUNTER. With the window open, an enemy WINDING UP within reach is parried: damaged,
    // staggered, its attack cancelled, the heal, momentum along your travel. An enemy that is not attacking is not
    // parryable: the window closes on nothing and the press is a whiff, cooldown and all — that is what makes it a
    // parry and not a button. The one other body a parry connects with is one you are FALLING ONTO at speed: a POGO
    // off its head — movement's parry, half the heal, and it sends you up, so it cannot repeat until you come down.
    // Bosses run their own counters (BossForeman.TryCounter, the Concierge's greeting) and are left to them.
    // Runs at the press instant AND per tick while the guard is up, so a press just before the windup still lands
    // (press early: E33 timing) and a windup that starts inside the guard is countered too. EVERY windup in reach is
    // countered, each once per guard: the guard is up for all of them at once. The momentum boost is paid once.
    readonly HashSet<Enemy> countered = new HashSet<Enemy>();
    float counteredPress = float.NegativeInfinity;

    void TryEnemyParry()
    {
        if (motor == null || !motor.ParryWindowOpen) return;
        if (motor.ParryPressedAt != counteredPress) { countered.Clear(); counteredPress = motor.ParryPressedAt; }
        Vector3 chest = motor.CurrPosition + Vector3.up * 0.9f;
        float fall = -motor.Velocity.y;
        bool falling = !motor.Grounded && fall >= config.parryMinImpactSpeed;
        Enemy pogoBest = null;
        float pogoDist = float.MaxValue;
        foreach (Collider c in Physics.OverlapSphere(chest, config.enemyParryRadius))
        {
            var enemy = c.GetComponentInParent<Enemy>();
            if (enemy == null || !enemy.Alive || enemy.kind == EnemyKind.Boss || countered.Contains(enemy)) continue;
            float d = Vector3.Distance(chest, enemy.Center);
            if (d > config.enemyParryRadius) continue;
            if (enemy.IsWindingUp)
            {
                if (!motor.TryConsumeParry(enemy.Center, "COUNTER!")) return;
                countered.Add(enemy);
                enemy.Damage(config.parryMeleeDamage, KillMethod.MeleeParry);
                enemy.Stagger(); // its attack, cancelled
                // A counter ADDS momentum along your travel — engaging pays, unlike surfaces (which blast you away)
                if (motor.ParryGuardConnects == 1)
                {
                    Vector3 travel = new Vector3(motor.Velocity.x, 0f, motor.Velocity.z);
                    Vector3 boostDir = travel.sqrMagnitude > 0.5f
                        ? travel.normalized
                        : Vector3.ProjectOnPlane(chest - enemy.Center, Vector3.up).normalized;
                    if (boostDir != Vector3.zero) motor.AddImpulse(boostDir * config.parryEnemyMomentumBoost);
                }
                continue;
            }
            if (falling && motor.CurrPosition.y > enemy.Center.y + 0.3f && d < pogoDist) { pogoBest = enemy; pogoDist = d; }
        }
        // The pogo: one body under your feet, the same words the floor's pogo uses, so the heal knows it for movement's
        if (pogoBest == null || countered.Count > 0) return;   // a windup outranks a body under your feet
        float up = Mathf.Max(config.parryPogoMinUpSpeed, fall * config.parryPogoBounce);
        if (!motor.TryConsumeParry(pogoBest.Center, $"POGO ↑{up:0.0} m/s")) return;
        countered.Add(pogoBest);
        pogoBest.Damage(config.parryMeleeDamage, KillMethod.MeleeParry);
        motor.ParryPogo();
    }

    void OnMove(MoveType type, string detail)
    {
        if (type != MoveType.SlamLand || motor == null) return;
        // Slam shockwave (spec §3.6): damage + implicit knock via shatter
        foreach (Collider c in Physics.OverlapSphere(motor.CurrPosition, config.slamShockwaveRadius))
        {
            var enemy = c.GetComponentInParent<Enemy>();
            if (enemy != null && enemy.Alive)
                enemy.Damage(config.slamShockwaveDamage, KillMethod.SlamShockwave);
        }
    }

    /// Every kill, with the speed you had: the baseline listens (CombatBaseline)
    public static event System.Action<KillMethod, float, bool> KillReported;

    public void ReportKill(KillMethod method)
    {
        Kills++;
        LastActionTime = Time.time;
        bool airborne = motor != null && !motor.Grounded;
        KillReported?.Invoke(method, motor != null ? new Vector2(motor.Velocity.x, motor.Velocity.z).magnitude : 0f, airborne);
        bool duringYank = grapple != null && grapple.KillCountsAsYank;
        if (resets != null) resets.OnKill(airborne, duringYank);
        if (GameFeel.Instance != null) GameFeel.Instance.PlayKill();
    }

    float mercyUntil = float.NegativeInfinity;
    /// The moment of the last hit that LANDED (one that fell in the mercy window did not)
    public float LastHitAt { get; private set; } = float.NegativeInfinity;

    /// A unit's hit reaches you. False if it did not land: inside the mercy window after the last one, so a pack
    /// cannot put three in you in one frame and a burst counts once. Every hit is scaled by enemyDamageScale first.
    public bool ReportPlayerHit(Vector3 hitDirection, float damage)
    {
        if (damage <= 0f) return false;
        if (Time.time < mercyUntil) return false;
        mercyUntil = Time.time + Mathf.Max(0f, config.hitMercy);
        LastHitAt = Time.time;
        // Taking a hit costs style flat (spec 5.3); it also costs HEALTH
        LastActionTime = Time.time;
        if (style != null) style.TakeHit();
        if (motor != null) motor.AddImpulse(hitDirection * config.hitKnockback);
        if (health != null) health.TakeDamage(damage * Mathf.Max(0f, config.enemyDamageScale));
        if (GameFeel.Instance != null) GameFeel.Instance.PlayHurt();
        return true;
    }

    /// What you took off a unit comes back as health, by how you were moving when you did it (PlayerHealth.Feed)
    public void ReportDamageDealt(Enemy enemy, float dealt, KillMethod method)
    {
        if (health == null || dealt <= 0f) return;
        LastActionTime = Time.time;
        health.Feed(dealt, method, Momentum01);
    }

    /// 0 planted on the ground, 1 airborne or at feedSpeedFull: the scale of the feed (the speed as SpeedDamage reads
    /// it: the horizontal plus half the vertical)
    public float Momentum01
    {
        get
        {
            if (motor == null || !motor.Grounded) return 1f;
            Vector3 v = motor.Velocity;
            float speed = new Vector2(v.x, v.z).magnitude + Mathf.Abs(v.y) * 0.5f;
            return Mathf.Clamp01(speed / Mathf.Max(1f, config.feedSpeedFull));
        }
    }

    /// Weakspot execute: precision is a reset (+1 pip); it scores no style (movement only, 2026-10-29)
    public void ReportExecute()
    {
        LastActionTime = Time.time;
        if (resets != null) resets.CombatReset("EXECUTE", 1);
    }

    /// A PERFECT tackle (Strike timed inside the window before the body): timing is a reset, +1 pip
    public void ReportPerfectTackle()
    {
        LastActionTime = Time.time;
        if (resets != null) resets.CombatReset("TACKLE", 1);
    }

    /// A room cleared, or left at speed: style, and a pip when it says so
    public void ReportRoomClear(string label, float styleBonus, int pips)
    {
        if (resets != null && pips > 0) resets.CombatReset(label, pips);
        if (style != null) style.AddBonus(styleBonus);
    }

    public void NearMiss()
    {
        LastActionTime = Time.time;
        if (resets != null) resets.CombatReset("NEAR MISS", 1);
    }
}
