using UnityEngine;

/// <summary>
/// THE TWO-PRESS TRICK and THE GRADE THAT DRIVES THE BODY (2026-10-26, user: "our movement system should remain the
/// same but include the new tricking system"). In the air the first press of the trick key ARMS the QTE (the ring
/// closes), the second press at the beat throws the flip; a hit pays a burst, style and a pip on the spot, a miss
/// is a stumble in the air. The style grade feeds the motor: run speed and stamina regen rise with the rank, and the
/// grade starves without tricks.
/// </summary>
public partial class MovementConfig
{
    [Header("The two-press trick")]
    [Tooltip("Seconds from the arming press to the beat; the ring closes over twice this")]
    public float trickQteLead = 0.4f;
    [Tooltip("The forward burst on a hit, m/s")]
    public float trickBurstForward = 3.5f;
    [Tooltip("The upward burst on a hit, m/s")]
    public float trickBurstUp = 2.5f;
    [Tooltip("The burst's multiplier on a perfect press")]
    public float trickPerfectBurstScale = 1.5f;
    [Tooltip("Style points on a hit (a style zone scales them; perfect adds half again)")]
    public float trickStylePoints = 120f;
    [Tooltip("Pips refunded on a hit")]
    public int trickPips = 1;
    [Tooltip("How much of the horizontal speed a missed window keeps (the stumble in the air)")]
    public float trickFailKeep = 0.6f;   // 0.35 until 2026-10-27 (the user: "recovery options so a mistimed jump does not instantly kill momentum")
    [Tooltip("Style points docked on a missed window")]
    public float trickFailStyleLoss = 40f;

    [Header("Each flip is its own speed")]
    [Tooltip("The front flip's forward burst, as a multiple of trickBurstForward (its up is half of trickBurstUp)")]
    public float trickFrontForwardScale = 1.4f;
    [Tooltip("The backflip's upward burst, as a multiple of trickBurstUp (its forward is 0.4 of trickBurstForward)")]
    public float trickBackUpScale = 1.8f;
    [Tooltip("The side flip's sideways burst, m/s, toward the side you flipped")]
    public float trickSideLateral = 4.5f;
    [Tooltip("The spin's style, as a multiple (its burst is the plain one)")]
    public float trickSpinStyleScale = 1.3f;
    [Tooltip("Each later flip in one air bursts this much harder")]
    public float trickComboBurstScale = 1.15f;

    [Header("The grade drives the body")]
    [Tooltip("Run speed gained per rank above D (S+ is rank 5)")]
    public float styleSpeedPerRank = 0.035f;
    [Tooltip("Stamina regen gained per rank above D")]
    public float styleRegenPerRank = 0.1f;
    [Tooltip("Seconds without a trick before the grade starts to starve (0 = never)")]
    public float trickStarveSeconds = 10f;
    [Tooltip("Style points per second lost while starving (scaled by rank pressure like the flat-ground decay)")]
    public float trickStarveDecayRate = 6f;

    [Header("Momentum pads")]
    [Tooltip("A momentum pad multiplies the incoming speed by 1 + this per rank")]
    public float padSpeedPerRank = 0.12f;
}
