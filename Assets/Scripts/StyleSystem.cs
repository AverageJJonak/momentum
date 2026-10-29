using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Style ranks D→S+ (spec §5): points from fresh moves and resets, scaled by the
/// movement-freshness multiplier (distinct moves in a rolling window; repeats add
/// nothing). Decays only while parked on flat ground. S+ triggers FLOW STATE:
/// pips pinned full, with a grace period once style dips below S.
/// Movement only (2026-10-29): there are no enemies, so kills and executes score nothing here.
/// </summary>
public class StyleSystem : MonoBehaviour
{
    public MovementConfig config;
    public PlayerMotor motor;
    public StaminaSystem stamina;

    public float Points { get; private set; }
    public string Rank { get; private set; } = "D";
    public float Multiplier { get; private set; } = 1f;
    public int DistinctMoves { get; private set; }
    public bool FlowState { get; private set; }
    public float RankProgress { get; private set; } // 0..1 toward the next rank
    /// How much style gains are worth at the current rank (rank pressure)
    public float RankGainScale { get; private set; } = 1f;
    /// 0 = D … 5 = S+
    public int RankIndex => rankIndex;
    /// Rank as a 0..1 fraction (D = 0, S+ = 1) — the blade's swing speed rides on it
    public float RankFraction => rankIndex / 5f;
    /// Time-weighted rank held across the level (0 = D … 5 = S+), the best rank reached, and seconds in flow — the report card's STYLE
    public float AverageRank => rankSeconds > 0f ? rankWeighted / rankSeconds : 0f;
    public int PeakRankIndex { get; private set; }
    public float FlowSeconds { get; private set; }
    /// The flow fuse: 1 = just fed, 0 = burnt out. Burnt out, style pours away until something fresh happens.
    public float FlowTimer01 { get; private set; } = 1f;
    public bool FlowDraining { get; private set; }
    float lastGainTime = float.NegativeInfinity;
    float rankWeighted, rankSeconds;

    int rankIndex;
    int lastRankIndex;
    bool lastFlow;

    readonly List<(string key, float time)> window = new List<(string, float)>();
    float flatGroundSince = float.PositiveInfinity;
    float flowDropTime = float.PositiveInfinity;

    /// Freshness move list (spec §5.2). The edge boost (a kick on a vertical corner) and the
    /// wall kick are separate types, the corner boost (a timed jump on a ledge's lip) is its
    /// own; slide and slide-jump are separate. Hidden tech counts too.
    public static string FreshnessKey(MoveType t) => t switch
    {
        MoveType.Bhop => "bhop-jump",
        MoveType.PerfectBhop => "bhop-jump",
        MoveType.Slide => "slide",
        MoveType.SlideJump => "slide-jump",
        MoveType.PerfectSlideJump => "slide-jump",
        MoveType.Dash => "dash",
        MoveType.DoubleJump => "double-jump",
        MoveType.WallKick => "wall-kick",
        MoveType.WallRun => "wall-run",
        MoveType.WallJump => "wall-jump",
        MoveType.CornerBoost => "corner-boost",
        MoveType.EdgeBoost => "edge-boost",
        MoveType.Slam => "slam",
        MoveType.SlamStorageJump => "slam-storage-jump",
        MoveType.GrappleYank => "grapple-yank",
        MoveType.GlitchTech => "hidden-tech",
        MoveType.Parry => "parry",
        MoveType.GlassBreak => "glass-break",
        MoveType.BlastJump => "blast-jump",
        MoveType.RocketJump => "rocket-jump",
        MoveType.Anchor => "anchor",
        MoveType.Reel => "reel",
        MoveType.Trick => "trick",   // the flips (TrickSystem): the press is the move; a lazy one pays style and nothing else
        _ => null,
    };

    float enabledAt;   // the starvation clock's start when no trick has been thrown yet

    // ---------------------------------------------------------------- FRESHNESS (STEP 1, 2026-10-29): one value per move key, 1 = fresh

    readonly Dictionary<string, float> fresh = new Dictionary<string, float>();
    readonly Dictionary<string, float> freshUsedAt = new Dictionary<string, float>();
    readonly List<string> freshOrder = new List<string>();   // the keys seen this level, in the order they first appeared (the HUD's strip)
    /// The keys seen so far and their freshness (0 stale … 1 fresh), when each was last used
    public int FreshCount => freshOrder.Count;
    public string FreshKey(int i) => freshOrder[i];
    public float FreshnessOf(string key) => fresh.TryGetValue(key, out float f) ? f : 1f;
    public float FreshUsedAt(string key) => freshUsedAt.TryGetValue(key, out float t) ? t : float.NegativeInfinity;
    public bool FreshnessOn => config != null && config.freshnessEnabled;

    /// What a move's GAIN is worth right now (1 = the full gain, down to the floor): the motor and the trick system ask
    /// before they pay, and the event the move then emits is what spends the freshness, so the scale read is the one
    /// before the spend. 1 when the system is off, absent, or the move is no freshness move
    public static float GainScale(MoveType type)
    {
        var s = Current;
        if (s == null || s.config == null || !s.config.freshnessEnabled) return 1f;
        string key = FreshnessKey(type);
        if (key == null) return 1f;
        return Mathf.Max(Mathf.Clamp01(s.config.freshnessGainFloor), s.FreshnessOf(key));
    }

    /// A use of `key`: its freshness drops, every other move's recovers a little (a trick's drop is its own, and a trick refreshes the others the most)
    void Spend(string key)
    {
        if (!FreshnessOn) return;
        bool trick = key == "trick";
        float drop = Mathf.Clamp01(trick ? config.freshnessTrickDrop : config.freshnessDropPerUse);
        float give = Mathf.Clamp01(trick ? config.freshnessTrickRefreshOthers : config.freshnessRecoverPerOtherMove);
        if (!fresh.ContainsKey(key)) { fresh[key] = 1f; freshOrder.Add(key); }
        for (int i = 0; i < freshOrder.Count; i++)
        {
            string k = freshOrder[i];
            if (k != key) fresh[k] = Mathf.Min(1f, fresh[k] + give);
        }
        fresh[key] = Mathf.Max(0f, fresh[key] - drop);
        freshUsedAt[key] = Time.time;
    }

    void RecoverFreshness(float dt)
    {
        if (!FreshnessOn || config.freshnessRecoverPerSecond <= 0f) return;
        for (int i = 0; i < freshOrder.Count; i++)
        {
            string k = freshOrder[i];
            fresh[k] = Mathf.Min(1f, fresh[k] + config.freshnessRecoverPerSecond * dt);
        }
    }

    void OnEnable()
    {
        enabledAt = Time.time;
        MovementEvents.MoveExecuted += OnMove;
        ResetDetector.ResetFired += OnReset;
    }

    void OnDisable()
    {
        MovementEvents.MoveExecuted -= OnMove;
        ResetDetector.ResetFired -= OnReset;
    }

    bool slamPressFresh;

    void OnMove(MoveType type, string detail)
    {
        // The slam scores when it LANDS, and only if it fell: the press is not the trick, the drop is
        if (type == MoveType.SlamLand)
        {
            if (slamPressFresh && motor != null && motor.LastSlamDrop >= config.slamStyleMinDrop) AddPoints(config.styleFreshMovePoints);
            slamPressFresh = false;
            return;
        }
        if (type == MoveType.Trick && detail != null && (detail == "STUMBLE" || detail.StartsWith("LANDED"))) return;   // the landing's verdicts are not moves
        string key = FreshnessKey(type);
        if (key == null) return;
        Prune();
        bool isFresh = !window.Exists(e => e.key == key);
        window.Add((key, Time.time));
        RecomputeMultiplier();
        Spend(key);   // FRESHNESS (STEP 1): the use is spent after the move paid (the motor read GainScale before this event)
        if (type == MoveType.Slam) { slamPressFresh = isFresh; return; } // held until the landing says how far it fell
        if (isFresh) AddPoints(config.styleFreshMovePoints); // repeats add nothing (spec §1)
    }

    void OnReset(string label, bool scores) { if (scores) AddPoints(config.styleResetPoints); }

    public void TakeHit() => Points = Mathf.Max(0f, Points - config.styleHitPenalty);

    /// Flat style award scaled by the current multipliers (executes, etc.)
    public void AddBonus(float basePoints) => AddPoints(basePoints);

    // Rank pressure: the same play is worth less the higher you climb
    void AddPoints(float basePoints)
    {
        if (basePoints > 0f) lastGainTime = Time.time; // feeds the flow fuse
        Points += basePoints * Multiplier * RankGainScale;
    }

    /// The one style system in play (the momentum pads read the rank from it)
    public static StyleSystem Current { get; private set; }

    /// A missed trick window docks style (TrickSystem.Fail)
    public void TakeTrickFail(float points) => Points = Mathf.Max(0f, Points - Mathf.Max(0f, points));

    void Update()
    {
        Current = this;
        Prune();
        RecomputeMultiplier();
        RecoverFreshness(Time.deltaTime);

        // THE GRADE DRIVES THE BODY (2026-10-26): run speed and stamina regen climb with the rank
        if (motor != null) motor.StyleSpeedScale = 1f + Mathf.Max(0f, config.styleSpeedPerRank) * rankIndex;
        if (stamina != null) stamina.RegenScale = 1f + Mathf.Max(0f, config.styleRegenPerRank) * rankIndex;
        // ...and starves without tricks: so many seconds past the last one, the points bleed (rank pressure applies)
        if (config.trickStarveSeconds > 0f && Points > 0f)
        {
            float last = TrickSystem.Instance != null ? Mathf.Max(TrickSystem.Instance.LastTrickAt, enabledAt) : enabledAt;
            if (Time.time - last > config.trickStarveSeconds)
                Points = Mathf.Max(0f, Points - config.trickStarveDecayRate * (1f + config.styleDecayGrowthPerRank * rankIndex) * Time.deltaTime);
        }

        // Decay while parked on flat ground; airborne/sliding/walls are safe
        // (spec §5.3) — but the decay gets meaner per rank (rank pressure)
        float decayScale = 1f + config.styleDecayGrowthPerRank * rankIndex;
        if (motor != null && motor.OnFlatGround)
        {
            if (float.IsPositiveInfinity(flatGroundSince)) flatGroundSince = Time.time;
            if (Time.time - flatGroundSince > config.styleFlatDecayDelay)
                Points = Mathf.Max(0f, Points - config.styleDecayRate * decayScale * Time.deltaTime);
        }
        else
        {
            flatGroundSince = float.PositiveInfinity;
        }

        // High ranks bleed everywhere, even airborne: holding S+ means never
        // running out of things to do
        if (rankIndex >= config.stylePassiveDrainStartRank)
        {
            float drain = config.stylePassiveDrainPerRank
                          * (rankIndex - config.stylePassiveDrainStartRank + 1);
            Points = Mathf.Max(0f, Points - drain * Time.deltaTime);
        }

        // FLOW is a fuse: nothing fresh for flowHoldWindow seconds and style pours out
        // until you do something — S+ is held by playing, never by coasting
        if (FlowState)
        {
            float idle = Practice.Active && Practice.AlwaysFlow ? 0f : Time.time - lastGainTime; // the range's console holds the fuse
            FlowTimer01 = 1f - Mathf.Clamp01(idle / Mathf.Max(0.1f, config.flowHoldWindow));
            bool draining = idle > config.flowHoldWindow;
            if (draining && !FlowDraining && GameFeel.Instance != null) GameFeel.Instance.PlayFlowWarn();
            FlowDraining = draining;
            if (draining) Points = Mathf.Max(0f, Points - config.flowDrainRate * Time.deltaTime);
        }
        else
        {
            FlowTimer01 = 1f;
            FlowDraining = false;
        }

        UpdateRankAndFlow();

        // Report-card bookkeeping: what rank you HELD, not just touched
        rankWeighted += rankIndex * Time.deltaTime;
        rankSeconds += Time.deltaTime;
        if (rankIndex > PeakRankIndex) PeakRankIndex = rankIndex;
        if (FlowState) FlowSeconds += Time.deltaTime;
    }

    void Prune()
    {
        float cutoff = Time.time - config.freshnessWindowSeconds;
        window.RemoveAll(e => e.time < cutoff);
    }

    void RecomputeMultiplier()
    {
        var distinct = new HashSet<string>();
        foreach (var e in window) distinct.Add(e.key);
        DistinctMoves = distinct.Count;
        Multiplier = DistinctMoves <= 1
            ? 1f
            : Mathf.Min(config.freshnessMultiplierMax,
                        1f + (DistinctMoves - 1) * config.freshnessMultiplierPerDistinct);
    }

    void UpdateRankAndFlow()
    {
        float[] thresholds =
        {
            0f, config.styleThresholdC, config.styleThresholdB,
            config.styleThresholdA, config.styleThresholdS, config.styleThresholdSPlus,
        };
        string[] names = { "D", "C", "B", "A", "S", "S+" };

        int rank = 0;
        for (int i = thresholds.Length - 1; i >= 0; i--)
        {
            if (Points >= thresholds[i]) { rank = i; break; }
        }
        if (rank > lastRankIndex && GameFeel.Instance != null) GameFeel.Instance.PlayRankUp(rank);
        else if (rank < lastRankIndex && GameFeel.Instance != null) GameFeel.Instance.PlayRankDown();
        lastRankIndex = rank;
        rankIndex = rank;
        Rank = names[rank];
        RankProgress = rank >= thresholds.Length - 1
            ? 1f
            : Mathf.InverseLerp(thresholds[rank], thresholds[rank + 1], Points);
        RankGainScale = Mathf.Max(config.styleGainFalloffMin,
            1f - config.styleGainFalloffPerRank * rank);

        // FLOW STATE (spec §5.4): triggers at S+ and has to be HELD there — dip under the
        // hold line (S+ itself at full strictness, S at none) past the grace and it is gone
        if (Points >= config.styleThresholdSPlus)
        {
            FlowState = true;
            flowDropTime = float.PositiveInfinity;
        }
        else if (FlowState)
        {
            if (Points >= Mathf.Lerp(config.styleThresholdS, config.styleThresholdSPlus, Mathf.Clamp01(config.flowHoldStrictness)))
            {
                flowDropTime = float.PositiveInfinity;
            }
            else
            {
                if (float.IsPositiveInfinity(flowDropTime)) flowDropTime = Time.time;
                if (Time.time - flowDropTime >= config.flowStateGraceTime) FlowState = false;
            }
        }

        if (Practice.Active && Practice.AlwaysFlow) { FlowState = true; flowDropTime = float.PositiveInfinity; } // the range's console
        if (FlowState != lastFlow && GameFeel.Instance != null)
        {
            if (FlowState) GameFeel.Instance.PlayFlowIn(); else GameFeel.Instance.PlayFlowOut();
        }
        lastFlow = FlowState;
        if (stamina != null) stamina.InfinitePips = FlowState || (Practice.Active && Practice.InfiniteStamina);
    }
}
