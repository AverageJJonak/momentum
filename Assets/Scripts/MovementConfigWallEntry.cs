using UnityEngine;

/// <summary>
/// THE WALL RUN'S ENTRY (2026-11-10; the user, after playing the entry clamp of 2026-10-29: "wall running needs to let
/// you run a little vertically, otherwise you just touch the ground very fast after starting the wall run"). The clamp
/// dropped ALL of a jump's rise at the wall, so a run beside a floor rose 0.3 m and was back on the ground in 0.7 s. This
/// is the capped version the user said they would ask for if the clamp felt flat: a run entered going up keeps this much
/// of the rise and no more. At 2 m/s a jumped-at run climbs about 1.3 m and stays on the wall 1.2 s beside a floor; the
/// wall jump, the pip and a kick after it reach 2.4 / 3.8 / 4.6 m (they were 1.3 / 2.8 / 3.5), so the safe wall height
/// the metrics give goes from 6 m to 7 m and nothing a level builds to that height can be topped. 0 is the clamp of
/// 2026-10-29 exactly; 9 (a jump's whole impulse) is the old uncapped run that lifted a body 8.5 m.
/// </summary>
public partial class MovementConfig
{
    [Header("WALL RUN ENTRY · how much of a jump's rise a run keeps")]
    [Tooltip("Metres a second of upward speed a wall run keeps when it is entered going up (the entry pop is added on top). 0: none, the flat run of 2026-10-29. 2: a climb of about 1.3 m. Higher lifts higher: the safe wall height in the level metrics follows this number")]
    public float wallRunEntryRiseKeep = 2f;
}
