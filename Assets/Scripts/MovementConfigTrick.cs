using UnityEngine;

/// <summary>
/// THE TRICKS' numbers (2026-10-23, TrickSystem.cs): the QTE flips. A prompt opens around a big jump's apex; the press
/// inside it is a trick, timed to the apex it is PERFECT; the flip is a camera arc that returns exactly to the look.
/// THE PAY is the kick's springboard's rule (combat-ladder.md): a PERFECT trick pays its tier on the landing's
/// horizontal, under the lunge's cap max(current, run) × the perfect-hop tier, so no flip ever out-runs a timed hop
/// chain; a LAZY trick pays style only. A flip still mid-arc at touchdown is a STUMBLE: the roll, and a beat of
/// footing lost. Nothing here is read in Awake.
/// </summary>
public partial class MovementConfig
{
    [Header("TRICKS · THE PROMPT (around the apex of a big enough jump)")]
    [Tooltip("The jump must have lasted this long, s, and have this much predicted air left, s, for the prompt to open")]
    public float trickMinAirtime = 0.3f;
    public float trickMinRemaining = 0.55f;
    [Tooltip("A launch (a pad, a springboard, a corner boost, a slam jump) opens the prompt with only this much air left, s")]
    public float trickLaunchMinAir = 0.35f;
    [Tooltip("A rise this fast, m/s, with no jump of yours behind it is a launch (a pad): over a jump's 9")]
    public float trickLaunchVy = 11f;
    [Tooltip("The window's length, s (the ring closes over it; the beat is its middle), and the PERFECT band either side of the beat, s")]
    public float trickWindow = 0.25f;
    public float trickPerfectWindow = 0.06f;
    [Tooltip("Seconds between the arc marches that find the landing point (the prediction)")]
    public float trickMarchInterval = 0.05f;

    [Header("TRICKS · THE ARCS (seconds for the full turn; the camera rides through it, eased in and out)")]
    public float trickFrontSeconds = 0.6f;
    public float trickBackSeconds = 0.65f;
    public float trickSideSeconds = 0.55f;
    public float trickSpinSeconds = 0.6f;
    [Tooltip("The FOV punch on the press, degrees (the dash's shape)")]
    public float trickFov = 6f;

    [Header("TRICKS · THE PAY (the springboard's rule: tier on the landing, under the hop's cap)")]
    [Tooltip("A PERFECT trick's multiplier on the landing's horizontal, and its floor at high speed (the tackle's tier; the cap above it is the perfect hop's)")]
    public float trickPerfectMultiplier = 1.12f;
    public float trickPerfectMinMultiplier = 1.05f;
    [Tooltip("The horizontal the tier is applied to is at least this, m/s: a perfect flip from a standstill pad lands you running")]
    public float trickLandFloorSpeed = 8f;
    [Tooltip("Pips given back on a perfect landing")]
    public int trickPerfectPips = 1;
    [Tooltip("A second trick in the same air (a combo) is worth this much more style, compounding")]
    public float trickComboStyle = 1.5f;

    [Header("TRICKS · THE STUMBLE (touching down mid-arc)")]
    [Tooltip("An arc with no more than this left, s, at touchdown lands clean (the last degrees finish in the dip)")]
    public float trickLandGrace = 0.08f;
    [Tooltip("The stumble: seconds of footing lost, and the ground acceleration's scale through them")]
    public float trickStumbleSeconds = 0.35f;
    public float trickStumbleAccelScale = 0.5f;

    [Header("ARM RIG — THE TUCK (the arms pull in through a flip and the landing roll)")]
    [Tooltip("Where both hands go in the tuck (camera space, m, added to the pose) and their pitch, degrees")]
    public Vector3 viewmodelTuckPos = new Vector3(0f, -0.05f, -0.1f);
    public float viewmodelTuckPitch = 30f;
    [Tooltip("The root's share: its pitch, degrees, and its drop, m")]
    public float viewmodelTuckRootPitch = 10f;
    public float viewmodelTuckRootDrop = 0.03f;
}
