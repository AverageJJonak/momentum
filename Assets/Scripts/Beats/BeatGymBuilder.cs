using UnityEngine;

/// <summary>
/// THE BEAT GYM (Phase 3, 2026-10-27; the user: "a new test gym scene with each beat laid out separately in a row,
/// at 2-3 difficulty levels each, with a reset pad before each one"). For every beat in the library, its three
/// difficulties in turn, each on its own: a RESET PAD (a checkpoint across the floor and a brass strip, so a fall
/// respawns you there), a SIGN naming the beat, its difficulty, what it asks and the speed it expects, a SLIDE SLOPE
/// sized by the metrics to deliver exactly that speed when the beat wants more than a run (the sign says to slide it),
/// the beat, five metres of floor after it, and a SETTLE CORNER (2026-10-29; the user: "no long flat straights to
/// build speed for free. Speed should be earned in specific places"): a square the line turns ninety degrees on, left
/// and right by turns, so the row is a staircase of legs and never a straight. A run turns the corner; whatever came
/// out of the beat faster is shed on the ground, and a hop carried into it flies off the square's far edge. So every
/// beat starts from run speed, and its slope is the one place its speed is made. The row descends where beats descend
/// (nothing climbs back up: no uphill exits); the kill floor and the city vista follow it. Scene:
/// Assets/Scenes/BeatGym.unity (LevelKind.BeatGym); the finish at the end of the row.
/// </summary>
public static class BeatGymBuilder
{
    const float BaseY = 40f, HalfW = 5f, PadHalfW = 2.5f, SlopeDeg = 12f, GroundBelow = 220f;   // the row descends about 170 m; the ground stays under the kill floor

    public static Vector3 Build(MovementConfig config)
    {
        LevelKit.Init();
        LevelKit.SetZone(Zone.Utopia);
        LevelSections.Begin(7, BaseY);
        var root = new GameObject("BeatGym").transform;
        var v = LevelMetrics.Read(config);
        var f = new Frame(root, v, new Pose(new Vector3(0f, BaseY, 0f), 0f, 0f)) { groundY = BaseY - GroundBelow };
        f.Ground();   // THE CITY FLOOR far below: every deck is a rooftop on its building, every stone a pillar, every wall a facade (2026-10-29: "no floating platforms")

        // the start: a floor, the first checkpoint, the gym's sign
        f.Deck(0f, 12f, 0f, HalfW + 2f, "Start");
        f.Checkpoint("THE GYM", 2f, 0f, HalfW + 2f);
        f.Sign(0f, 4.5f, 8f, "THE BEAT GYM\nevery beat three times, a reset pad before each, a corner after\nF9 stop · F10 snag · F11 flow", 0.9f);
        f.Lamp(0f, 6f, 7f);
        Vector3 spawn = f.P(0f, 0f, 3f);
        f.Advance(12f, 0f);
        Corner(f, -1f);   // the first beat starts from run speed too (left, so the beats' own corners alternate right, left, right…)

        int index = 0;
        foreach (var beat in BeatLibrary.All)
            for (int d = 1; d <= 3; d++)
                One(f, beat, d, index++);

        // the finish
        f.Deck(0f, 16f, 0f, HalfW + 2f, "FinishDeck"); f.Line(0f, 0.5f, 15.5f, 0f);
        f.Checkpoint("THE END", 2f, 0f, HalfW + 2f);
        f.Sign(0f, 4.5f, 8f, "THE END OF THE ROW", 1.2f);
        LevelSections.Last.finishPortal = f.P(0f, 2f, 12f);
        LevelSections.Last.spawn = spawn;
        LevelSections.Last.length = Vector3.Distance(spawn, f.origin);
        GameBootstrap.KillFloorY = f.Structural ? f.groundY + 15f : f.lowest - 30f;   // the bodies reach the ground, so `lowest` is the ground: the kill floor stays above it
        var foot = f.built; foot.Expand(new Vector3(120f, 60f, 120f));
        CityVista.Override = foot;
        CityVista.Suppressed = true;   // a test rig, not a place: no skyline round two kilometres of row (it would be thousands of blocks)
        return spawn;
    }

    /// One beat at one difficulty: the pad, the sign, the slope that makes its speed, the beat, the floor after, the corner
    static void One(Frame f, Beat beat, int d, int index)
    {
        float s = beat.EntrySpeed(f.v, d);
        string title = beat.Name + " " + BeatMath.Roman(d);
        f.Deck(0f, 8f, 0f, PadHalfW, "ResetDeck"); f.Line(0f, 0.5f, 7.5f, 0f);   // five metres wide: a hop over the corner's block finds no deck beside it
        f.Checkpoint(title, 1.5f, 0f, PadHalfW);
        bool slope = s > f.v.baseRunSpeed + 0.5f;
        f.Sign(0f, 4f, 4.5f, title + "\n" + beat.Note(f.v, d) + (slope ? "\nSLIDE the slope ahead to arrive at " + s.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " m/s" : ""), 0.8f);
        f.Lamp(0f, 6f, 5f);
        f.Advance(8f, 0f);
        if (slope)
        {
            float len = BeatMath.SlideRun(f.v, f.v.baseRunSpeed, s, SlopeDeg) + 1.5f;
            float dz = len * BeatMath.Cos(SlopeDeg), drop = len * BeatMath.Sin(SlopeDeg);
            f.Slope(0f, 0f, dz, -drop, HalfW, "SpeedSlope");
            f.Advance(dz, -drop);
        }
        beat.Build(f, d);
        Corner(f, index % 2 == 0 ? 1f : -1f);
    }

    /// THE SETTLE CORNER: five metres of floor and a twelve-metre square the line turns on (right for `side` 1, left for
    /// -1), with a BLOCK on the inside of the turn, eight metres deep and reaching past the square's side, and the next
    /// leg a five-metre deck leaving the square's far strip. The block and the narrow deck are sized so that no hop from
    /// the floor before the square lands on the leg (the validator's snowball checks every such skip): the way round is
    /// a landing on the far strip and a ground turn, and a hop carried into the square meets the block or the void
    static void Corner(Frame f, float side)
    {
        f.Deck(0f, 5f, 0f, HalfW, "After"); f.Line(0f, 0.5f, 4.5f, 0f);
        f.Deck(5f, 17f, 0f, HalfW, "SettleCorner"); f.Line(0f, 5.5f, 15f, 0f);
        f.Mass(side > 0f ? 1f : -9f, side > 0f ? 9f : -1f, -0.5f, Frame.FacadeHeight, 5f, 13f, "SettleBlock");   // rooftop machinery, as tall as a facade: a run up its face finds no top (the lift reaches eleven metres)
        f.Lamp(0f, 5f, 15f);
        f.Advance(15f, 0f);
        f.Turn(side * 90f);
        f.Line(0f, 0f, 4.5f, 0f);
        f.Advance(5f, 0f);
    }
}
