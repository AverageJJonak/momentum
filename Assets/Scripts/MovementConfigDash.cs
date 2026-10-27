using UnityEngine;

/// <summary>
/// THE OMNI DASH's numbers (2026-10-08, PlayerMotor.TryDash / ReadDashDir): the dash goes where you look. W rides the
/// view's forward WITH its pitch (up or down), A/D are the horizontal sideways of the view, S is the horizontal
/// backward, diagonals mix, no input = the look. A dead zone keeps a near-level look flat, the floor is never a
/// destination from the ground, and the vertical is capped under the timed corner boost's lift
/// (jumpImpulse × cornerBoostMaxLift = 18 m/s) so an untimed dash never out-climbs a timed lip jump.
/// The speed rule is unchanged: max(dashSpeed, the horizontal speed you had) along the new direction.
/// </summary>
public partial class MovementConfig
{
    [Header("Dash — omni-directional (2026-10-08)")]
    [Tooltip("The dash follows the view's pitch (W = the look direction, up or down; A/D and S stay horizontal). Off = the flat 8-way dash")]
    public bool dashOmni = true;
    [Tooltip("Degrees of pitch below which the dash stays flat: a slight downward look while running must not send you into the floor")]
    public float dashPitchDeadzone = 12f;
    [Tooltip("Cap on the dash's vertical speed in m/s, up or down. Keep it under jumpImpulse × cornerBoostMaxLift (18): an untimed dash never out-climbs a timed corner boost. Straight up from standing = this ÷ jumpImpulse jumps' worth of impulse")]
    public float dashVerticalMax = 12f;
}
