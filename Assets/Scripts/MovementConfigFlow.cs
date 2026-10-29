using UnityEngine;

/// <summary>
/// FRESHNESS (STEP 1, 2026-10-29; the user: "each move type has a freshness value that drops every time it's repeated
/// and recovers while doing other moves. Speed gains from a move scale with its freshness, so spamming bhops gives
/// less and less, while chaining different moves keeps gains high"). StyleSystem keeps one value per move key (the
/// freshness keys it already had for the points), 1 = fresh. PlayerMotor and TrickSystem ask
/// StyleSystem.GainScale(move) before a gain and scale only the GAIN of a move, the part above the speed kept, never
/// the kept speed: a stale hop still keeps what you have, it just earns less. The trick is the core of the movement
/// (the user), so it goes stale slowest and refreshes every other move the most: throw tricks to keep the rest fresh.
/// The toggle off is today's numbers exactly. Nothing here is read in Awake.
/// </summary>
public partial class MovementConfig
{
    [Header("FRESHNESS (STEP 1) · a move's gain scales with how fresh it is")]
    [Tooltip("Off: every gain is paid in full, as before this step")]
    public bool freshnessEnabled = true;
    [Tooltip("What one use takes off a move's freshness (0..1)")]
    public float freshnessDropPerUse = 0.35f;
    [Tooltip("What every OTHER move's use gives back to this one (0..1)")]
    public float freshnessRecoverPerOtherMove = 0.15f;
    [Tooltip("Slow recovery of every move per second with nothing happening; 0 = only other moves refresh")]
    public float freshnessRecoverPerSecond = 0.06f;
    [Tooltip("The least a stale move still pays of its gain (0..1)")]
    public float freshnessGainFloor = 0.25f;
    [Tooltip("The trick's own drop per use (the core move stays fresh longer), and what a trick gives back to every other move")]
    public float freshnessTrickDrop = 0.2f;
    public float freshnessTrickRefreshOthers = 0.4f;
    [Tooltip("The HUD's freshness strip under the style card, and the freshness on the feed's lines")]
    public bool freshnessHud = true;
}
