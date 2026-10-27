using UnityEngine;

/// <summary>
/// Stamina pips (spec §3.1): slow passive regen, regen pause after spending,
/// and a temporary overcharge pip from refunds that overfill (spec §3.3).
/// Spend/refund API consumed by movement abilities; DebugHUD draws the pips.
/// </summary>
public class StaminaSystem : MonoBehaviour
{
    public MovementConfig config;

    public int Pips { get; private set; }
    public int MaxPips => config.staminaMaxPips;
    public float RegenProgress { get; private set; } // 0..1 toward the next pip
    /// THE GRADE DRIVES THE BODY (2026-10-26): regen speed from the rank, set by the style system every frame
    public float RegenScale = 1f;
    public bool Overcharge { get; private set; }
    /// FLOW STATE (spec §5.4): pips pinned full, spending always succeeds
    public bool InfinitePips { get; set; }

    float regenPausedUntil = float.NegativeInfinity;
    float overchargeExpiry;

    void Start()
    {
        Pips = config.staminaMaxPips;
    }

    void Update()
    {
        if (InfinitePips)
        {
            Pips = config.staminaMaxPips;
            RegenProgress = 0f;
            return;
        }

        if (Overcharge && Time.time >= overchargeExpiry)
        {
            Overcharge = false; // decayed unused
            if (GameFeel.Instance != null) GameFeel.Instance.PlayFizzle();
        }

        if (Pips >= config.staminaMaxPips)
        {
            RegenProgress = 0f;
            return;
        }
        if (Time.time < regenPausedUntil) return;

        RegenProgress += Time.deltaTime * RegenScale / config.staminaRegenSecondsPerPip;   // the grade drives the regen (StyleSystem sets RegenScale)
        if (RegenProgress >= 1f)
        {
            RegenProgress = 0f;
            Pips++;
            if (GameFeel.Instance != null) GameFeel.Instance.PlayPipRegen();
        }
    }

    public bool TrySpend(int amount)
    {
        if (InfinitePips) return true;
        int available = Pips + (Overcharge ? 1 : 0);
        if (available < amount) return false;

        for (int i = 0; i < amount; i++)
        {
            if (Overcharge) Overcharge = false; // overcharge drains before real pips
            else Pips--;
        }
        regenPausedUntil = Time.time + config.staminaRegenPauseAfterSpend;
        return true;
    }

    public void Refund(int amount)
    {
        if (InfinitePips) return;   // in flow the pips are pinned full: a refund is nothing, and must not set overcharge
        for (int i = 0; i < amount; i++)
        {
            if (Pips < config.staminaMaxPips)
            {
                Pips++;
            }
            else
            {
                Overcharge = true;
                overchargeExpiry = Time.time + config.overchargeDecaySeconds;
            }
        }
    }
}
