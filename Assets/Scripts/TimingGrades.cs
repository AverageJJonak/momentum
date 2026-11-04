using System;
using UnityEngine;

/// <summary>A timed moment's grade (TIMING GRADES, STEP 2, 2026-11-04): inside the perfect window, inside the good band past it, or the rest</summary>
public enum TimingGrade { Perfect, Good, Late }

/// <summary>
/// The sums the graded moments share (MovementConfigGrades has the numbers): the grade of a miss against two bounds,
/// what a grade pays of a move's gain, its label for the event detail, and one event every grade is raised through so
/// GameFeel can give each its sound. PlayerMotor grades the hop press, the wall catch and the slide entry.
/// </summary>
public static class TimingGrades
{
    /// Every graded moment: the move and its grade. GameFeel listens for the sounds
    public static event Action<MoveType, TimingGrade> Graded;
    /// Whether the config wants the grade sounds (read from the config at every report, so the toggle is live)
    public static bool Sounds { get; private set; } = true;

    /// The grade of a miss of `seconds`: perfect within `perfect`, good within `good`, late past it
    public static TimingGrade Of(float seconds, float perfect, float good)
        => seconds <= perfect ? TimingGrade.Perfect : seconds <= good ? TimingGrade.Good : TimingGrade.Late;

    /// What a grade pays of the perfect gain: all of it, the good fraction, the late fraction
    public static float Fraction(MovementConfig c, TimingGrade g)
        => g == TimingGrade.Perfect ? 1f : g == TimingGrade.Good ? c.gradeGoodFraction : c.gradeLateFraction;

    public static string Label(TimingGrade g) => g == TimingGrade.Perfect ? "PERFECT" : g == TimingGrade.Good ? "GOOD" : "LATE";

    /// A grade scored: remembers the sound toggle and tells whoever listens
    public static void Report(MovementConfig c, MoveType move, TimingGrade g)
    {
        Sounds = c.gradeSounds;
        Graded?.Invoke(move, g);
    }
}
