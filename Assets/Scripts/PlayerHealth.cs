using UnityEngine;

/// <summary>
/// Player health. Enemies are a real threat, and there is no passive regen
/// (except in the tutorial, which also cannot kill you). Two things put health
/// back, and both are earned:
///  THE PARRY. Every parry that connects heals, and each parry IN A ROW heals
///     more than the last, up to a cap. A hit breaks the chain; so does letting
///     it go cold. Wall and floor parries heal at a discount but keep the chain
///     warm between fights. A parry heals THROUGH hard damage.
///  THE FEED (after ULTRAKILL's blood). What the blade, the bow, the slam and a
///     counter take off a unit comes back to you as health, scaled by how you
///     were moving when you did it: planted on the ground it is a trickle,
///     airborne or at speed it is the whole of it. The way to live is the way
///     to score: move, and cut.
/// HARD DAMAGE keeps the feed honest: a share of every hit you take is hard,
/// and the feed cannot fill past the hard line (grey on the bar) until it has
/// decayed, a beat after the last hit. Take three hits in a row and the fight
/// has to be won at range for a moment; take none and it never shows.
/// Reaching zero "downs" you: the bootstrap respawns you at the level start
/// (timer keeps running, like a fall).
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public MovementConfig config;

    /// Set by the bootstrap: what happens when health hits zero (respawn).
    public System.Action OnDowned;

    /// THE TUTORIAL'S TWO MERCIES (the bootstrap sets them there and nowhere else): health never reaches zero, and it
    /// comes back on its own once nothing has hit you for a moment. Out in the levels neither is true.
    public bool Deathless;
    public float RegenPerSecond, RegenDelay = 2f;

    public float Current { get; private set; }
    public float Max => config != null ? config.playerMaxHealth : 100f;
    public float Fraction => Max > 0f ? Current / Max : 0f;
    /// True briefly after a hit: the HUD flashes the bar
    public float LastHitTime { get; private set; } = float.NegativeInfinity;

    /// Hard damage: the top of the bar the feed cannot fill until it has decayed
    public float Hard { get; private set; }
    public float HardFraction => Max > 0f ? Hard / Max : 0f;
    /// Where the feed stops
    public float FeedCeiling => Mathf.Max(0f, Max - Hard);
    float hardDecayAt = float.NegativeInfinity;

    /// The parry chain: how many in a row, when the last one landed, what it healed, and what the next one would
    public int Chain { get; private set; }
    public float LastParryAt { get; private set; } = float.NegativeInfinity;
    public float LastHealAt { get; private set; } = float.NegativeInfinity;
    public float LastHeal { get; private set; }
    /// Whether the last heal was a parry's (gold on the HUD) or the feed's (the bar's own colour)
    public bool LastHealParry { get; private set; }
    float lastHealPress = float.NegativeInfinity;   // the guard the last heal came from
    public float NextHeal => HealFor(Chain + 1, 1f);
    /// 1 fresh, 0 as the chain goes cold (1 for as long as it lives, when nothing but a hit breaks it)
    public float ChainWarmth => Chain == 0 ? 0f
        : config == null || config.parryChainWindow <= 0f ? 1f
        : Mathf.Clamp01(1f - (Time.time - LastParryAt) / config.parryChainWindow);

    /// Report-card bookkeeping: everything you took this level, how often it dropped you, what came back, and how
    /// much of that the feed gave
    public float TotalDamage { get; private set; }
    public int Downs { get; private set; }
    public float TotalHealed { get; private set; }
    public float TotalFed { get; private set; }

    void OnEnable() { MovementEvents.MoveExecuted += OnMove; }
    void OnDisable() { MovementEvents.MoveExecuted -= OnMove; }

    void Start()
    {
        Current = Max; // config is assigned by the bootstrap before Start
    }

    void Update()
    {
        // The chain goes cold: too long since the last parry
        if (config != null && Chain > 0 && config.parryChainWindow > 0f && Time.time - LastParryAt > config.parryChainWindow) Chain = 0;
        // Passive regen, where it is switched on: a beat after the last hit, steadily back to full
        if (RegenPerSecond > 0f && Current > 0f && Current < Max && Time.time - LastHitTime > RegenDelay)
        {
            Current = Mathf.Min(Max, Current + RegenPerSecond * Time.deltaTime);
            ClampHard();
        }
        // Hard damage decays, a beat after the last hit
        if (Hard > 0f && config != null && Time.time >= hardDecayAt)
            Hard = Mathf.MoveTowards(Hard, 0f, Mathf.Max(0f, config.hardDecayRate) * Time.deltaTime);
    }

    /// A wall blast or a floor pogo: movement's parries, not combat's
    static bool IsMovementParry(string detail) => detail == "BLAST" || (detail != null && detail.StartsWith("POGO ↑"));

    float HealFor(int chain, float scale)
    {
        if (config == null) return 0f;
        float amount = config.parryHealBase + config.parryHealStep * Mathf.Max(0, chain - 1);
        if (config.parryHealMax > 0f) amount = Mathf.Min(amount, config.parryHealMax);
        return Mathf.Max(0f, amount) * scale;
    }

    /// Hard damage and health never overlap on the bar: whatever heals through the hard line burns it away
    void ClampHard() => Hard = Mathf.Clamp(Hard, 0f, Mathf.Max(0f, Max - Current));

    /// Every parry that connects comes through here: one more link, and the health it is worth
    void OnMove(MoveType type, string detail)
    {
        if (type != MoveType.Parry || config == null || Current <= 0f) return;
        // One guard is one link however many things it parries: the second bolt of a volley heals a share of the
        // first and does not lengthen the chain
        var motor = CombatDirector.Instance != null ? CombatDirector.Instance.motor : null;
        bool burst = motor != null && motor.ParryPressedAt == lastHealPress;
        if (motor != null) lastHealPress = motor.ParryPressedAt;
        if (!burst) Chain++;
        LastParryAt = Time.time;
        float scale = IsMovementParry(detail) ? Mathf.Clamp01(config.parryHealMoveScale) : 1f;
        if (burst) scale *= Mathf.Clamp01(config.parryHealBurstScale);
        float heal = HealFor(Chain, scale);
        float before = Current;
        Current = Mathf.Min(Max, Current + heal);   // through the hard line: a parry is the one thing that is
        ClampHard();
        LastHeal = Current - before;
        TotalHealed += LastHeal;
        if (LastHeal > 0f)
        {
            LastHealAt = Time.time;
            LastHealParry = true;
            if (GameFeel.Instance != null) GameFeel.Instance.PlayParryHeal(Chain);
        }
    }

    /// THE FEED. `dealt` is what a hit actually took off a unit; `momentum01` is how you were moving when you did it
    /// (0 planted, 1 airborne or at speed). It never fills past the hard line.
    public void Feed(float dealt, KillMethod method, float momentum01)
    {
        if (config == null || Current <= 0f || dealt <= 0f) return;
        float amount = method switch
        {
            KillMethod.SwordExecute => config.feedExecute,
            KillMethod.Sword => dealt * config.feedSwordShare,
            KillMethod.Cleave => dealt * config.feedSwordShare,
            KillMethod.Arrow => dealt * config.feedArrowShare,
            KillMethod.SlamShockwave => dealt * config.feedSlamShare,
            KillMethod.Fist => dealt * config.feedSwordShare,    // the hands feed like the blade
            KillMethod.Tackle => dealt * config.feedSlamShare,   // and a body at speed like the slam
            KillMethod.MeleeParry => dealt * config.feedParryShare,
            KillMethod.ReflectedProjectile => dealt * config.feedParryShare,
            _ => 0f,
        };
        if (amount <= 0f) return;
        if (config.feedMax > 0f) amount = Mathf.Min(amount, config.feedMax);
        amount *= Mathf.Lerp(Mathf.Clamp01(config.feedStillFactor), 1f, Mathf.Clamp01(momentum01));
        float ceiling = Mathf.Max(Current, FeedCeiling);   // never takes health away: it only stops at the hard line
        float before = Current;
        Current = Mathf.Min(ceiling, Current + amount);
        float got = Current - before;
        if (got <= 0.01f) return;
        LastHeal = got;
        LastHealAt = Time.time;
        LastHealParry = false;
        TotalHealed += got;
        TotalFed += got;
        if (GameFeel.Instance != null) GameFeel.Instance.PlayFeed(config.feedMax > 0f ? got / config.feedMax : 0.5f);
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || Current <= 0f) return;
        if (Practice.Active && Practice.God) return; // the range's console
        Current = Mathf.Max(Deathless ? 1f : 0f, Current - amount);   // deathless: it can hurt, it cannot drop you
        TotalDamage += amount;
        LastHitTime = Time.time;
        Chain = 0; // a hit breaks the chain
        if (config != null)
        {
            // A share of it is hard: the feed cannot fill it back until it has decayed
            Hard = Mathf.Min(Hard + amount * Mathf.Max(0f, config.hardShare), Max * Mathf.Clamp01(config.hardMaxShare));
            ClampHard();
            hardDecayAt = Time.time + Mathf.Max(0f, config.hardDecayDelay);
        }
        if (Current <= 0f)
        {
            Downs++;
            if (GameFeel.Instance != null) GameFeel.Instance.PlayDowned();
            OnDowned?.Invoke();
        }
    }

    /// Full refill (respawn), the chain cold, nothing hard
    public void Restore()
    {
        Current = Max;
        Hard = 0f;
        Chain = 0;
        LastParryAt = float.NegativeInfinity;
    }
}
