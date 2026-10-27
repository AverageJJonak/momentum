using UnityEngine;

/// <summary>
/// THE KIT, v3 (2026-10-16, the House bible §2e; the one kit since the simplification): every number of the knuckles'
/// COMBO and KICK. Every cap here sits under the ladder (combat-ladder.md): a lazy springboard redirects and never
/// gains, a timed one pays the perfect tier at most. Nothing here is read in Awake.
/// </summary>
public partial class MovementConfig
{
    [Header("KNUCKLES · THE COMBO (jab → cross → HOOK on each press inside the window, else back to the jab)")]
    [Tooltip("Seconds after a punch inside which the next press is the next step of the combo")]
    public float fistComboWindow = 0.9f;
    [Tooltip("The cross reaches fistReach × this (the root turns with it)")]
    public float fistCrossReachMult = 1.15f;
    [Tooltip("THE HOOK: a wide sweep that hits EVERY unit in its arc; its reach, m")]
    public float hookReach = 2.2f;
    [Tooltip("The hook's arc, degrees (the whole width; ±half)")]
    public float hookArc = 60f;
    [Tooltip("Lines across the hook's arc (odd: the centre line exists)")]
    public int hookCasts = 9;
    [Tooltip("The hook's damage to every unit it meets, × SpeedDamage")]
    public float hookDamage = 40f;
    [Tooltip("Units the hook meets are knocked SIDEWAYS along the sweep, m/s")]
    public float hookKnockSide = 6f;
    [Tooltip("... and up, m/s")]
    public float hookKnockUp = 2f;

    [Header("KNUCKLES · THE KICK (its own key): a front kick in the fan; a SPRINGBOARD off a body when you are airborne or above run speed")]
    [Tooltip("The kick's reach through the fan, m")]
    public float kickReach = 2.4f;
    [Tooltip("Damage of the kick (a boss takes it as a chip; a unit is launched besides)")]
    public float kickDamage = 20f;
    [Tooltip("The unit it lands on is LAUNCHED: m/s away from you")]
    public float kickLaunchAway = 14f;
    [Tooltip("... and m/s up")]
    public float kickLaunchUp = 5f;
    [Tooltip("THE SPRINGBOARD: the floor of the horizontal speed you leave the body at, m/s (capped like the lunge: max(current, run) × the tier)")]
    public float kickSpringSpeed = 11f;
    [Tooltip("±seconds around a jump's apex in which a springboard is PERFECT: the tackle's tier on the horizontal, the bell")]
    public float kickPerfectWindow = 0.05f;
    [Tooltip("A whiff commits the arms this long")]
    public float kickWhiffCommit = 0.3f;
    [Tooltip("Seconds before the next kick after one that landed")]
    public float kickCooldown = 0.35f;

    [Header("ARM RIG — THE KICK's own motion")]
    [Tooltip("THE KICK: the camera's dip, degrees, and the push forward, m; the root's pitch, degrees")]
    public float viewmodelKickDip = 3f;
    public float viewmodelKickPush = 0.05f;
    public float viewmodelKickRootPitch = 6f;
}
