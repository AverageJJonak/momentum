using UnityEngine;

/// <summary>
/// THE BEAT GYM (Phase 3, 2026-10-27; the user: "a new test gym scene with each beat laid out separately in a row,
/// at 2-3 difficulty levels each, with a reset pad before each one"). One straight row along +z (the Turn beats bend
/// it at the end): for every beat in the library, its three difficulties in turn, each on its own: a RESET PAD (a
/// checkpoint across the floor and a brass strip, so a fall respawns you there), a SIGN naming the beat, its
/// difficulty, what it asks and the speed it expects, a SLIDE SLOPE sized by the metrics to deliver exactly that
/// speed when the beat wants more than a run (the sign says to slide it), four metres of flat, the beat, and six
/// metres of floor after it. The row descends where beats descend (nothing climbs back up: no uphill exits); the
/// kill floor and the city vista follow it. Scene: Assets/Scenes/BeatGym.unity (LevelKind.BeatGym); the finish at
/// the end of the row.
/// </summary>
public static class BeatGymBuilder
{
    const float BaseY = 40f, HalfW = 5f, SlopeDeg = 12f;

    public static Vector3 Build(MovementConfig config)
    {
        LevelKit.Init();
        LevelKit.SetZone(Zone.Utopia);
        LevelSections.Begin(7, BaseY);
        var root = new GameObject("BeatGym").transform;
        var v = LevelMetrics.Read(config);
        var f = new Frame(root, v, new Pose(new Vector3(0f, BaseY, 0f), 0f, 0f));

        // the start: a floor, the first checkpoint, the gym's sign
        f.Deck(0f, 14f, 0f, HalfW + 2f, "Start");
        f.Checkpoint("THE GYM", 2f, 0f, HalfW + 2f);
        f.Sign(0f, 4.5f, 8f, "THE BEAT GYM\nevery beat three times, a reset pad before each\nF9 stop · F10 snag · F11 flow", 0.9f);
        f.Lamp(0f, 6f, 7f);
        Vector3 spawn = f.P(0f, 0f, 3f);
        f.Advance(14f, 0f);

        foreach (var beat in BeatLibrary.All)
            for (int d = 1; d <= 3; d++)
                One(f, beat, d);

        // the finish
        f.Deck(0f, 16f, 0f, HalfW + 2f, "FinishDeck"); f.Line(0f, 0.5f, 15.5f, 0f);
        f.Checkpoint("THE END", 2f, 0f, HalfW + 2f);
        f.Sign(0f, 4.5f, 8f, "THE END OF THE ROW", 1.2f);
        LevelSections.Last.finishPortal = f.P(0f, 2f, 12f);
        LevelSections.Last.spawn = spawn;
        LevelSections.Last.length = Vector3.Distance(spawn, f.origin);
        GameBootstrap.KillFloorY = f.lowest - 30f;
        var foot = f.built; foot.Expand(new Vector3(120f, 60f, 120f));
        CityVista.Override = foot;
        CityVista.Suppressed = true;   // a test rig, not a place: no skyline round two kilometres of row (it would be thousands of blocks)
        return spawn;
    }

    /// One beat at one difficulty: the pad, the sign, the slope that makes its speed, the beat, the floor after
    static void One(Frame f, Beat beat, int d)
    {
        float s = beat.EntrySpeed(f.v, d);
        string title = beat.Name + " " + BeatMath.Roman(d);
        f.Deck(0f, 10f, 0f, HalfW, "ResetDeck"); f.Line(0f, 0.5f, 9.5f, 0f);
        f.Checkpoint(title, 1.5f, 0f, HalfW);
        bool slope = s > f.v.baseRunSpeed + 0.5f;
        f.Sign(0f, 4f, 4.5f, title + "\n" + beat.Note(f.v, d) + (slope ? "\nSLIDE the slope ahead to arrive at " + s.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " m/s" : ""), 0.8f);
        f.Lamp(0f, 6f, 5f);
        f.Advance(10f, 0f);
        if (slope)
        {
            float len = BeatMath.SlideRun(f.v, f.v.baseRunSpeed, s, SlopeDeg) + 1.5f;
            float dz = len * BeatMath.Cos(SlopeDeg), drop = len * BeatMath.Sin(SlopeDeg);
            f.Slope(0f, 0f, dz, -drop, HalfW, "SpeedSlope");
            f.Advance(dz, -drop);
        }
        f.Deck(0f, 4f, 0f, HalfW, "RunUp"); f.Line(0f, 0.5f, 3.5f, 0f);
        f.Advance(4f, 0f);
        beat.Build(f, d);
        f.Deck(0f, 6f, 0f, HalfW, "After"); f.Line(0f, 0.5f, 5.5f, 0f);
        f.Advance(6f, 0f);
    }
}
