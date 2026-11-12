using UnityEngine;

/// <summary>
/// THE BEAT GYM (Phase 3, 2026-10-27; the user: "a new test gym scene with each beat laid out separately in a row,
/// at 2-3 difficulty levels each, with a reset pad before each one"). For every beat in the library, its three
/// difficulties in turn, each on its own: a RESET PAD (a checkpoint across the floor and a brass strip, so a fall
/// respawns you there), a SIGN naming the beat, its difficulty, what it asks and the speed it expects, a SLIDE SLOPE
/// sized by the metrics to deliver exactly that speed when the beat wants more than a run (the sign says to slide it),
/// the beat, five metres of floor after it, and a BEND (2026-11-04; the user: "replace the gym's sharp 90 degree
/// settle corners with flowing turns that carry speed"): ninety degrees as six facets of fifteen, pitched a little
/// downhill, a facade round the outside, turned whichever way keeps the row a staircase of legs heading two ways (a TURN
/// beat turns on its own; the bend after it brings the row back), so the row is never a straight and never doubles back. The bend carries what the beat handed on, as a real level would (VHOLUME's bends are slight ramps a
/// run holds its speed round; the beat log, 2026-11-04): the speed a beat is entered with is now the player's own
/// business, and every beat's short landing and portal are where too much of it is punished. The settle squares
/// that shed everything to run speed (2026-10-29) are gone. The row descends where beats descend and a little at
/// every bend (nothing climbs back up: no uphill exits); the kill floor and the city vista follow it. Scene:
/// Assets/Scenes/BeatGym.unity (LevelKind.BeatGym); the finish at the end of the row.
/// </summary>
public static class BeatGymBuilder
{
    const float BaseY = 40f, HalfW = 5f, PadHalfW = 2.5f, SlopeDeg = 12f, GroundBelow = 480f;   // the row descends about 355 m (the beats' own drops, the trick descent's roofs and 2.6 m a bend); the ground stays under the kill floor
    const float FacadeLook = 14f, GateLook = 15f;
    /// The validator's check: build the gym with the safe wall heights real levels use, not the 14 m look (LevelValidator.ValidateGymPlainWalls)
    public static bool PlainWalls;

    public static Vector3 Build(MovementConfig config)
    {
        LevelKit.Init();
        LevelKit.SetZone(Zone.Utopia);
        DecoKit.BeginBuild();
        MoveSurface.Forget();
        GymStations.Begin();
        LevelSections.Begin(7, BaseY);
        var root = new GameObject("BeatGym").transform;
        var v = LevelMetrics.Read(config);
        var f = new Frame(root, v, new Pose(new Vector3(0f, BaseY, 0f), 0f, 0f)) { groundY = BaseY - GroundBelow };
        if (!PlainWalls) { f.facadeHeight = FacadeLook; f.gateHeight = GateLook; }   // the gym's look (the user, 2026-10-29): taller than the safe heights real levels need
        f.Ground();   // THE CITY FLOOR far below: every deck is a rooftop on its building, every stone a pillar, every wall a facade (2026-10-29: "no floating platforms")

        // the start: a floor, the first checkpoint, the gym's sign
        f.Deck(0f, 12f, 0f, HalfW + 2f, "Start");
        f.Checkpoint("THE GYM", 2f, 0f, HalfW + 2f);
        f.Sign(0f, 4.5f, 8f, "THE BEAT GYM\nevery beat three times, a reset pad before each, a bend after\nTRICKS  W front, S back (a GAINER at speed), A or D side, nothing spin, W+A/D FULL TWIST, S+A/D CORK, hold JUMP for a DOUBLE\nLEDGES  jump as you reach one: VAULT · BRASS RAILS  they carry you: GRIND · THE WHIP (E)  aim at a lit arch, HOLD to swing, let go to fly\nTAB  every station, jump to any · R  back to the start of the station you are in · F9 stop · F10 snag · F11 flow", 0.9f);
        f.Lamp(0f, 6f, 7f); f.LampPost(HalfW + 2.4f, 0f, 7f);
        Vector3 spawn = f.P(0f, 0f, 3f);
        f.Advance(12f, 0f);
        Bend(f, BendSide(f));   // left first; after that whichever way keeps the row a staircase

        foreach (var beat in BeatLibrary.All)
            for (int d = 1; d <= 3; d++)
                One(f, beat, d);

        // the finish
        f.Deck(0f, 16f, 0f, HalfW + 2f, "FinishDeck"); f.Line(0f, 0.5f, 15.5f, 0f);
        f.Checkpoint("THE END", 2f, 0f, HalfW + 2f);
        f.Sign(0f, 4.5f, 8f, "THE END OF THE ROW", 1.2f); f.LampPost(HalfW + 2.4f, 0f, 8f);
        LevelSections.Last.finishPortal = f.P(0f, 2f, 12f);
        LevelSections.Last.spawn = spawn;
        LevelSections.Last.length = Vector3.Distance(spawn, f.origin);
        GameBootstrap.KillFloorY = f.Structural ? f.groundY + 15f : f.lowest - 30f;   // the bodies reach the ground, so `lowest` is the ground: the kill floor stays above it
        var foot = f.built; foot.Expand(new Vector3(120f, 60f, 120f));
        CityVista.Override = foot;
        CityVista.Suppressed = true;   // a test rig, not a place: no skyline round two kilometres of row (it would be thousands of blocks)
        if (DecoKit.Enabled) Debug.Log("BeatGym " + DecoKit.Report());
        root.gameObject.AddComponent<BeatGymTools>();   // TAB: the station menu; R: back to the start of the station under the feet
        return spawn;
    }

    /// One beat at one difficulty: the pad, the sign, the slope that makes its speed, the beat, the floor after, the bend
    static void One(Frame f, Beat beat, int d)
    {
        float s = beat.EntrySpeed(f.v, d);
        string title = beat.Name + " " + BeatMath.Roman(d);
        int first = f.root.childCount; Vector3 padAt = f.origin; Quaternion padRot = f.Rot;   // the test tools (BeatGymTools): what this station is made of, measured from its own pad
        f.Deck(0f, 8f, 0f, PadHalfW, "ResetDeck"); f.Line(0f, 0.5f, 7.5f, 0f);   // five metres wide: the bend's wide floor narrows to the pad, and a hop off the bend's side finds no deck
        f.Checkpoint(title, 1.5f, 0f, PadHalfW);
        bool slope = s > f.v.baseRunSpeed + 0.5f;
        f.Sign(0f, 4f, 4.5f, title + "\n" + beat.Note(f.v, d) + (slope ? "\nSLIDE the slope ahead to arrive at " + s.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " m/s" : ""), 0.8f);
        f.Lamp(0f, 6f, 5f); f.LampPost(PadHalfW + 0.4f, 0f, 5f);   // the standard beside the pad, off its right edge
        f.Advance(8f, 0f);
        if (slope)
        {
            float len = BeatMath.SlideRun(f.v, f.v.baseRunSpeed, s, SlopeDeg) + 1.5f;
            float dz = len * BeatMath.Cos(SlopeDeg), drop = len * BeatMath.Sin(SlopeDeg);
            f.Slope(0f, 0f, dz, -drop, HalfW, "SpeedSlope");
            f.Advance(dz, -drop);
        }
        beat.Build(f, d);
        int beatEnd = f.root.childCount;
        Bend(f, BendSide(f));
        GymStations.Add(title, beat.Name, d, f.root, first, beatEnd, f.root.childCount, padAt, padRot);
    }

    /// Which way the next bend turns: the row is a staircase of legs heading 0° (the start's way) and 270° (left of it), so
    /// from 0° or 90° the bend turns left and from 180° or 270° it turns right. A TURN beat has already turned right on its
    /// own; the bend after it brings the row back onto the staircase instead of doubling back (2026-11-04: alternating
    /// by count, the bend after TURN II ran eleven metres under the bend before TURN I, its facades up through that floor)
    static float BendSide(Frame f) { float h = Mathf.Repeat(f.yaw, 360f); return h < 135f || h >= 315f ? -1f : 1f; }

    /// THE BEND: three metres of floor, then ninety degrees (right for `side` 1, left for -1) as eight facets of 11.25
    /// degrees, each 2.5 m long and pitched 4° down (VHOLUME's bends are slight ramps; never an uphill exit), a FACADE
    /// along the outside the whole way round (a wall run round the outside is the fast way: "walls are the gain"), the
    /// lit line down the middle, a lamp at the apex, and a two-metre straight at the same pitch so the last joint is
    /// flush with the floor that follows. The facets overlap a little at their joints so the outer edge has no hole
    /// (the TURN beat's curve, the same sums). A run holds its speed round it, a slide gains a little, a hop chain has
    /// to be steered fifteen degrees a facet, two or three facets a hop; nothing here sheds speed on purpose any more
    static void Bend(Frame f, float side)
    {
        const int segs = 8; const float each = 11.25f, segLen = 2.5f, pitch = 4f, exitLen = 2f, after = 3f;   // 2026-11-10, the user having played the 42 m bends: "too long". Now 25 m: a 20 m arc of eight short facets (smoother than six long ones, and the outer wall turns less at each joint), three metres before it and two after
        f.Deck(0f, after, 0f, HalfW, "After"); f.Line(0f, 0.5f, after - 0.5f, 0f);
        f.Advance(after, 0f);
        float over = HalfW * Mathf.Tan(each * 0.5f * Mathf.Deg2Rad) * 0.5f + 0.2f;   // the joints overlap a little so the outer edge has no hole
        float lift = over * Mathf.Tan(pitch * Mathf.Deg2Rad);   // the overlap keeps the pitch: the top at z = 0 is exactly the origin's height
        float segDrop = segLen * BeatMath.Sin(pitch), segDz = segLen * BeatMath.Cos(pitch);
        float outer = -side * (HalfW + 0.6f);   // the outside of a right turn is the left
        for (int i = 0; i < segs; i++)
        {
            f.Slope(-over, lift, segDz + over, -segDrop - lift, HalfW, "BendDeck");
            f.Wall(outer, -over, segDz + over, 0.3f, 4f, "KickWall", 0f, segDz);   // dressed over the facet alone, not the overlaps
            f.LineRun(0f, 0f, 0f, segDz, -segDrop);
            if (i == segs / 2 - 1) { f.Lamp(0f, 5f, segDz); f.LampPost(side * (HalfW + 0.4f), -segDrop, segDz); }   // the apex lamp, its standard on the inside edge
            f.Advance(segDz, -segDrop);
            f.Turn(side * each);
        }
        float exitDrop = exitLen * Mathf.Tan(pitch * Mathf.Deg2Rad);
        f.Slope(-over, lift, exitLen, -exitDrop, HalfW, "BendExit"); f.LineRun(0f, 0f, 0f, exitLen, -exitDrop);   // one more pitch of the same slope, straight: the joint stays flush
        f.Advance(exitLen, -exitDrop);
    }
}
