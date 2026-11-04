using UnityEngine;

/// <summary>
/// TIMING GRADES (STEP 2, 2026-11-04; the user's movement identity programme: "perfect / good / late on bhops, wall
/// attaches and slide entries; perfect = today's numbers, good and late scale the gain by two config fractions; a
/// distinct sound per grade"). Three timed moments get a grade: THE HOP PRESS (the jump press against the landing: the
/// ±50 ms perfect window already paid ×1.18; now a press a little outside it is GOOD and pays a fraction of that gain,
/// and the rest of the grace is LATE and pays the late fraction, 0 by default: the kept hop as before), THE WALL CATCH
/// (how soon a wall is reached after leaving the ground or the last wall: a perfect catch pays a little speed along
/// the wall as the run begins, a plain one is the run as before) and THE SLIDE ENTRY (the crouch press against the
/// landing: a perfect entry pays a little as the slide begins, a slide that was not timed to a landing is a slide as
/// before). The grade goes in the move's event detail (the HUD feed) and TimingGrades raises it for GameFeel's sounds.
/// The toggle off is today's numbers exactly. Nothing here is read in Awake.
/// </summary>
public partial class MovementConfig
{
    [Header("TIMING GRADES (STEP 2) · perfect, good and late on the hop press, the wall catch and the slide entry")]
    [Tooltip("Off: the hop, the wall run and the slide are exactly as before this step")]
    public bool gradesEnabled = true;
    [Tooltip("The GOOD band for the hop press and the slide entry: this many seconds outside the perfect window still grades good; past it is late")]
    public float gradeGoodBand = 0.03f;
    [Tooltip("A good move pays this fraction of its perfect gain")]
    public float gradeGoodFraction = 0.5f;
    [Tooltip("A late move pays this fraction of its perfect gain (0: what a lazy move paid before this step)")]
    public float gradeLateFraction = 0f;
    [Tooltip("THE WALL CATCH: a wall reached within this many seconds of leaving the ground, or the last wall, is a PERFECT catch")]
    public float wallCatchPerfectTime = 0.3f;
    [Tooltip("Within this many seconds it is a GOOD catch; later is late")]
    public float wallCatchGoodTime = 0.6f;
    [Tooltip("A perfect catch multiplies the speed along the wall by this as the run begins (the gain above 1 scales with freshness); a late catch is the run as before")]
    public float wallCatchPerfectMultiplier = 1.06f;
    [Tooltip("THE SLIDE ENTRY: a crouch pressed within this many seconds either side of a landing is a PERFECT entry")]
    public float slideEntryPerfectWindow = 0.08f;
    [Tooltip("A slide that begins more than this long after a landing was not timed to it: no grade, no sound, the slide as before")]
    public float slideEntryGradeSpan = 0.3f;
    [Tooltip("A perfect entry multiplies the slide's speed by this as it begins (the gain above 1 scales with freshness); a late entry is the slide as before")]
    public float slideEntryPerfectMultiplier = 1.05f;
    [Tooltip("A sound per grade (GameFeel): a chime for perfect, a click for good, a dull tick for late")]
    public bool gradeSounds = true;
}
