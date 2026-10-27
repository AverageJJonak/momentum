using UnityEngine;

/// <summary>
/// WHAT IS IN THE DISTRICT: the House's units as fuel on the lines, the cordon's own defences, the mines on the slow
/// ways, the drones over all of it. (Level11Builder partial)
/// </summary>
public static partial class Level11Builder
{
    /// The response, in numbers, every one of them ON a line (course-grammar: fuel, never a gate). Patrolmen (pursuit
    /// units) and hounds in pairs on the street, the strip, the rims, the mains and the plaza; the old units hold ground
    /// (snipers on their perches, the gang in the rubble, blades on the stubs); the guns do neither: a sentry on every
    /// perch and either side of the stack's mouth. Housekeeping has the plaza.
    static void Units()
    {
        Vector3 right = Vector3.Cross(Vector3.up, Along);
        // THE FORECOURT AND THE STREET: first contact is a pair of hounds behind the trestle with a gun covering them from
        // either side of the street; patrolmen on the line, blades in the rubble, a gun behind the kicker, snipers and the
        // gang on the stubs, two in the air
        Pair(new Vector3(0f, 0f, 66f));
        Enemy.CreatePursuit(new Vector3(-5f, 0f, 160f), cfg);
        Enemy.CreatePursuit(new Vector3(4f, 0f, 220f), cfg);
        Enemy.CreatePursuit(new Vector3(-44f, 0f, 118f), cfg);
        Enemy.CreatePursuit(new Vector3(48f, 0f, 96f), cfg);
        Enemy.CreateSentry(new Vector3(-16f, 0f, 100f), cfg);
        Enemy.CreateSentry(new Vector3(18f, 0f, 128f), cfg);
        Enemy.CreateSentry(new Vector3(24f, 0f, 176f), cfg);
        Enemy.CreateSentry(new Vector3(-72f, 9f, 168f), cfg);
        Enemy.CreateSentry(new Vector3(118f, 0f, 204f), cfg);
        Enemy.CreateChaser(new Vector3(-26f, 0f, 62f), cfg);
        Enemy.CreateChaser(new Vector3(-62f, 0f, 140f), cfg);
        Enemy.CreateChaser(new Vector3(64f, 0f, 172f), cfg);
        Enemy.CreateChaser(new Vector3(-112f, 0f, 92f), cfg);
        Enemy.CreateChaser(new Vector3(132f, 0f, 142f), cfg);
        Enemy.CreateBlaster(new Vector3(-150f, 16f, 112f), cfg);
        Enemy.CreateTurret(new Vector3(92f, 26f, 106f), cfg);
        Enemy.CreateTurret(new Vector3(-150f, 25f, 124f), cfg);
        Enemy.CreateInterceptor(new Vector3(8f, 13f, 205f), cfg);
        Enemy.CreateInterceptor(new Vector3(-92f, 17f, 128f), cfg);
        Enemy.CreateSkitters(new Vector3(-40f, 0f, 84f), cfg);
        Enemy.CreateSkitters(new Vector3(44f, 0f, 120f), cfg);
        Enemy.CreateMarshal(new Vector3(12f, 0f, 150f), cfg);
        Enemy.CreateLurker(new Vector3(-58f, 0f, 108f), cfg);
        Enemy.CreateMoth(new Vector3(-20f, 6f, 130f), cfg);
        Enemy.CreateMoth(new Vector3(36f, 7f, 88f), cfg);
        Enemy.CreateOcular(new Vector3(70f, 12f, 150f), cfg);

        // THE VIADUCT: the foot of the span and the street north of it (a stoker: the street is where you were going);
        // on the strip a pair of hounds and a patrolman, two snipers, two in the air, a gun on the strip and the halt's own
        Enemy.CreatePursuit(new Vector3(-8f, 0f, DeckZ + 8f), cfg);
        Enemy.CreateChaser(new Vector3(-20f, 0f, DeckZ - 22f), cfg);
        Enemy.CreateSentry(new Vector3(-36f, 0f, DeckZ - 14f), cfg);
        Enemy.CreateKettler(new Vector3(-22f, 0f, 268f), cfg);
        Enemy.CreateTurret(new Vector3(-150f, DeckY + 7f, DeckZ), cfg);
        Enemy.CreateTurret(new Vector3(-215f, DeckY + 8f, DeckZ + 1f), cfg);
        Enemy.CreateInterceptor(new Vector3(-185f, DeckY + 11f, DeckZ + 9f), cfg);
        Enemy.CreateInterceptor(new Vector3(-128f, DeckY + 12f, DeckZ - 9f), cfg);
        Pair(new Vector3(-150f, DeckY, DeckZ));
        Enemy.CreateSentry(new Vector3(-196f, DeckY, DeckZ), cfg);
        Enemy.CreatePursuit(new Vector3(-212f, DeckY, DeckZ), cfg);
        Enemy.CreateSentry(new Vector3(-268f, DeckY + 5.4f, DeckZ), cfg);
        Enemy.CreateBlaster(new Vector3(-263f, DeckY, DeckZ + 5.4f), cfg);
        Enemy.CreateChaser(new Vector3(-263f, DeckY, DeckZ - 5.4f), cfg);
        Enemy.CreateTurret(new Vector3(170f, DeckY + 6f, DeckZ), cfg);
        Enemy.CreateSentry(new Vector3(150f, DeckY, DeckZ), cfg);
        Enemy.CreateChaser(new Vector3(196f, DeckY, DeckZ + 2f), cfg);
        Enemy.CreatePursuit(new Vector3(128f, DeckY, DeckZ - 2f), cfg);
        Enemy.CreateWarden(new Vector3(-100f, DeckY, DeckZ + 1f), cfg);
        Enemy.CreateMarshal(new Vector3(-170f, DeckY, DeckZ - 3f), cfg);
        Enemy.CreateOverseer(new Vector3(-190f, DeckY + 14f, DeckZ), cfg);
        Enemy.CreateSkitters(new Vector3(160f, DeckY, DeckZ + 1f), cfg);
        Enemy.CreateScourer(new Vector3(-60f, 0f, DeckZ - 30f), cfg);

        // THE CUT: hounds on the street where it meets the paving, two snipers over the east half (stepping stones, to
        // the right player), two in the air, on the island a patrolman — who will come off it after you — the gang and
        // a gun; every way on has somebody standing at the start of it; the eye and the inspector over the hole
        float rimS = Cut.z - CutHalfZ, rimN = Cut.z + CutHalfZ, rimW = Cut.x - CutHalfX, rimE = Cut.x + CutHalfX;
        Pair(new Vector3(0f, 0f, 270f));
        Enemy.CreateTurret(new Vector3(rimE - 12f, 13f, Cut.z), cfg);
        Enemy.CreateTurret(new Vector3(Cut.x + 32f, 14f, Cut.z - 4f), cfg);
        Enemy.CreateInterceptor(new Vector3(Cut.x - 48f, 30f, Cut.z + 30f), cfg);
        Enemy.CreateInterceptor(new Vector3(Cut.x + 4f, 24f, Cut.z - 52f), cfg);
        Enemy.CreatePursuit(new Vector3(Cut.x - 7f, IslandTop, Cut.z - 5f), cfg);
        Enemy.CreateBlaster(new Vector3(Cut.x + 6f, IslandTop, Cut.z + 1f), cfg);
        Enemy.CreateSentry(new Vector3(Cut.x - 10f, IslandTop, Cut.z + 4f), cfg);
        Enemy.CreateSentry(new Vector3(Cut.x + 13f, 0f, rimS - 9f), cfg);
        Enemy.CreateChaser(new Vector3(Cut.x - 22f, 0f, rimS - 14f), cfg);
        Enemy.CreatePursuit(new Vector3(Cut.x - 44f, 0f, rimS - 8f), cfg);
        Enemy.CreateSentry(new Vector3(rimW - 14f, 0f, Cut.z - 14f), cfg);
        Enemy.CreateChaser(new Vector3(rimW - 20f, 0f, Cut.z + 6f), cfg);
        Enemy.CreateSentry(new Vector3(Cut.x + 34f, 0f, rimN + 10f), cfg);
        Enemy.CreatePursuit(new Vector3(Cut.x + 40f, 0f, rimN + 18f), cfg);
        Enemy.CreateBlaster(new Vector3(Cut.x - 4f, 0f, rimN + 26f), cfg);
        Enemy.CreateWarden(new Vector3(Cut.x + 18f, 0f, rimN + 14f), cfg);
        Enemy.CreateScourer(new Vector3(Cut.x - 30f, 0f, rimS - 20f), cfg);
        Enemy.CreateLurker(new Vector3(rimW - 12f, 0f, Cut.z + 20f), cfg);
        Enemy.CreateLurker(new Vector3(rimE + 12f, 0f, Cut.z - 10f), cfg);
        Enemy.CreateSkitters(new Vector3(Cut.x + 10f, 0f, rimS - 24f), cfg);
        Enemy.CreateMarshal(new Vector3(Cut.x - 20f, 0f, rimN + 12f), cfg);
        Enemy.CreateOcular(new Vector3(Cut.x - 20f, 20f, Cut.z + 10f), cfg);
        Enemy.CreateOverseer(new Vector3(Cut.x + 20f, 26f, Cut.z + 20f), cfg);
        Enemy.CreateMoth(new Vector3(Cut.x - 40f, 8f, rimS - 10f), cfg);
        Enemy.CreateMoth(new Vector3(Cut.x + 44f, 9f, rimN + 12f), cfg);
        Enemy.CreateChaser(new Vector3(rimE + 20f, 0f, Cut.z), cfg);
        Enemy.CreatePursuit(new Vector3(92f, 0f, 360f), cfg);

        // THE MAINS and the open ground, west, east and north: hounds on the pipe's line, and whatever can keep up with you
        Pair(new Vector3(12f, 0f, 540f));
        Enemy.CreateChaser(new Vector3(-190f, 0f, 330f), cfg);
        Enemy.CreatePursuit(new Vector3(-176f, 0f, 400f), cfg);
        Enemy.CreateSentry(new Vector3(-200f, 0f, 436f), cfg);
        Enemy.CreateChaser(new Vector3(-220f, 0f, 480f), cfg);
        Enemy.CreateChaser(new Vector3(226f, 0f, 350f), cfg);
        Enemy.CreateSentry(new Vector3(214f, 0f, 430f), cfg);
        Enemy.CreatePursuit(new Vector3(236f, 0f, 500f), cfg);
        Enemy.CreateChaser(new Vector3(-12f, 0f, 524f), cfg);
        Enemy.CreatePursuit(new Vector3(-40f, 0f, 540f), cfg);
        Enemy.CreatePursuit(new Vector3(44f, 0f, 566f), cfg);
        Enemy.CreateKettler(new Vector3(-62f, 0f, 566f), cfg);
        Enemy.CreateChaser(new Vector3(-92f, 0f, 584f), cfg);
        Enemy.CreateChaser(new Vector3(84f, 0f, 604f), cfg);
        Enemy.CreateInterceptor(new Vector3(60f, 16f, 545f), cfg);
        Enemy.CreateInterceptor(new Vector3(-70f, 18f, 520f), cfg);

        // THE CLOCK TOWER: the foot of the pads, the crown, the facade (two snipers off its flanks, two guns and the
        // gang on it, one in the air over it), and a stoker at its foot — whatever comes down it fast comes down it through steam
        Enemy.CreateSentry(new Vector3(90f, 0f, 318f), cfg);
        Enemy.CreateChaser(new Vector3(112f, 0f, 312f), cfg);
        Enemy.CreatePursuit(TowerHigh + Along * 8f + right * 8f, cfg);
        Enemy.CreateBlaster(TowerHigh + Along * 15f - right * 9f, cfg);
        Enemy.CreateSentry(TowerHigh + Along * 4f - right * 9f, cfg);
        Enemy.CreateTurret(SurfaceKit.OnSpan(TowerLow, TowerHigh, 0.35f, -22f, 9f), cfg);
        Enemy.CreateTurret(SurfaceKit.OnSpan(TowerLow, TowerHigh, 0.7f, 22f, 9f), cfg);
        Enemy.CreateBlaster(SurfaceKit.OnSpan(TowerLow, TowerHigh, 0.47f, 7f, 0.2f), cfg);
        Enemy.CreateSentry(SurfaceKit.OnSpan(TowerLow, TowerHigh, 0.33f, 8f, 0.2f), cfg);
        Enemy.CreateSentry(SurfaceKit.OnSpan(TowerLow, TowerHigh, 0.58f, -8f, 0.2f), cfg);
        Enemy.CreateInterceptor(SurfaceKit.OnSpan(TowerLow, TowerHigh, 0.5f, 0f, 16f), cfg);
        Enemy.CreateKettler(TowerLow - Along * 34f + new Vector3(10f, 0f, 0f), cfg);
        Enemy.CreateChaser(TowerLow - Along * 22f, cfg);

        // THE HOUSE: HOUSEKEEPING has the plaza — here to make the area safe, which is you — with the last pair of
        // hounds, a patrolman, blades and a stoker round it, a gun either side of the stack's mouth and one in the air;
        // the ladder of snipers up the west face; and the roof
        Enemy.CreateDamageControl(new Vector3(0f, 0f, 664f), cfg);
        Pair(new Vector3(-16f, 0f, 648f));
        Enemy.CreatePursuit(new Vector3(18f, 0f, 652f), cfg);
        Enemy.CreateKettler(new Vector3(-30f, 0f, 682f), cfg);
        Enemy.CreateChaser(new Vector3(26f, 0f, 676f), cfg);
        Enemy.CreateChaser(new Vector3(-40f, 0f, 652f), cfg);
        Enemy.CreateSentry(new Vector3(-9f, 0f, 686f), cfg);
        Enemy.CreateSentry(new Vector3(9f, 0f, 686f), cfg);
        Enemy.CreateInterceptor(new Vector3(0f, 22f, 640f), cfg);
        for (int i = 0; i < 4; i++)
            Enemy.CreateTurret(new Vector3(-GoalHalf - (i % 2 == 0 ? 14f : 16f), 16f + i * 18f, Goal.z + (i % 2 == 0 ? -8f : -4f)), cfg);
        Enemy.CreatePursuit(new Vector3(6f, RoofY, Goal.z + 8f), cfg);
        Enemy.CreateSentry(new Vector3(9f, RoofY, Goal.z + 15f), cfg);
    }

    /// Hounds come in pairs: one to turn you, one to take the pip
    static void Pair(Vector3 at)
    {
        Enemy.CreateHound(at + Vector3.left * 2.2f, cfg);
        Enemy.CreateHound(at + Vector3.right * 2.2f, cfg);
    }

    /// What the police left running inside their wall (DistrictDefense): searchlights that call fire down on whoever
    /// they catch. None of it can be killed — only outrun, or turned on the units standing next to you: a shell does
    /// not care whose it is. Nothing here closes a route (course-grammar): the trestles are lips.
    static void Defences()
    {
        // THE STREET: a light on a post off the broken end of the line, walking the ground you cross to get anywhere
        Vector3 post = new Vector3(BreakW - 4f, DeckY + 1f, DeckZ - DeckW * 0.5f + 0.25f);
        SurfaceKit.Block(LevelKit.Girder, post + Vector3.up * 2.5f, new Vector3(0.5f, 5f, 0.5f));
        SurfaceKit.Block(LevelKit.Girder, post + new Vector3(0f, 4.8f, -1.4f), new Vector3(0.5f, 0.4f, 3.2f));
        SweepLight.Build(root, cfg, post + new Vector3(0f, 4.2f, -2.7f), new Vector3(-24f, 0f, 196f), 52f, 22f, 0.31f, 0f);
        LightMast(new Vector3(158.5f, 26f, 176f), 7f, new Vector3(-4f, 0f, 0f), new Vector3(118f, 0f, 150f), 40f, 44f, 0.27f, 1.3f);   // off the corner of the big stub
        LightMast(new Vector3(-206f, 0f, 430f), 32f, new Vector3(3.5f, 0f, 0f), new Vector3(-196f, 0f, 430f), 52f, 90f, 0.29f, 3.4f);   // the open ground west
        LightMast(new Vector3(64f, 0f, 520f), 34f, new Vector3(-3.5f, 0f, 0f), new Vector3(0f, 0f, 548f), 78f, 44f, 0.24f, 2.1f);     // and north: the mains
        SurfaceKit.Look(LevelKit.Girder, new Vector3(-15f, 57f, Goal.z - GoalHalf - 1.1f), new Vector3(0.5f, 0.5f, 2.2f));             // the plaza: off the house's own face
        SweepLight.Build(root, cfg, new Vector3(-15f, 56.3f, Goal.z - GoalHalf - 2.4f), new Vector3(0f, 0f, 646f), 44f, 26f, 0.36f, 0.7f);
        LevelKit.HoloSign(root, new Vector3(-16f, 2.6f, 63f), Vector3.back, 9f, 1.1f, "MINED GROUND  ·  KEEP MOVING", true);
        LevelKit.HoloSign(root, new Vector3(13f, 3.2f, 172f), Vector3.back, 12f, 1.1f, "SEARCHLIGHTS DIRECT FIRE  ·  STAY OUT OF THE LIGHT", true);
        LevelKit.HoloSign(root, new Vector3(0f, 4.6f, 639f), Vector3.back, 16f, 1.2f, "RAILWAY INCIDENT  ·  HOUSEKEEPING ON SITE  ·  THIS AREA IS BEING MADE SAFE");
    }

    /// A girder mast, solid, with a sweep light on an arm at its head. `arm` runs along X or along Z.
    static void LightMast(Vector3 foot, float height, Vector3 arm, Vector3 centre, float reachX, float reachZ, float rate, float phase)
    {
        SurfaceKit.Block(LevelKit.Girder, foot + Vector3.up * (height * 0.5f), new Vector3(1.2f, height, 1.2f));
        SurfaceKit.Block(LevelKit.Limestone, foot + Vector3.up * 0.4f, new Vector3(3f, 0.8f, 3f));
        LevelKit.Flutes(root, foot + Vector3.up * 3.4f, new Vector3(1.2f, 6f, 1.2f));   // a real corner on open ground: an edge boost, and the cream flutes that say so
        Vector3 head = foot + Vector3.up * (height - 0.3f);
        SurfaceKit.Block(LevelKit.Girder, head + arm * 0.5f, new Vector3(Mathf.Abs(arm.x) + 0.5f, 0.5f, Mathf.Abs(arm.z) + 0.5f));
        SweepLight.Build(root, cfg, head + arm + Vector3.down * 0.9f, centre, reachX, reachZ, rate, phase);
    }

    /// Hover mines on the slow ways and the stopping places: behind the notice, at the foot of every way onto something,
    /// across the open ground. Never on a line you would take at speed. (Laid after the colliders are live: each hangs
    /// over whatever is under it — the street, a ramp, the deck, a heap.)
    static void Mines()
    {
        Field(30f, -14f, 78f, -18f, 85f, -13f, 91.5f, 15f, 80f, 19f, 87f);                      // behind the notice: near enough each other to go up in a ripple
        Field(30f, -18f, 226f, -14f, 234f);                                                     // the foot of the fallen span
        Field(45f, -50f, 226f, -75f, 234f);                                                     // and the span itself
        Field(30f, -14f, 262f, 13f, 270f, -12f, 284f);                                          // either side of the street north of the line
        Field(45f, -205f, DeckZ);                                                               // the strip
        Field(20f, -42f, 290f, -20f, 284f, -27f, 300f);                                         // the track bed's mouth, and the first plate
        Field(20f, -118f, 372f, -118f, 388f);                                                   // the steam's lip
        Field(20f, -42f, 476f, -18f, 482f);                                                     // the north track bed's approach
        Field(20f, 84f, 308f, 120f, 318f);                                                      // the foot of the pads
        Field(20f, -20f, 500f, 30f, 515f, -70f, 530f, 70f, 545f, -10f, 560f, 40f, 590f, -50f, 610f, 10f, 630f);   // the open ground north
        Field(20f, -180f, 300f, -210f, 360f, -170f, 410f, -230f, 470f, -190f, 540f, -240f, 600f);   // west
        Field(20f, 230f, 330f, 250f, 420f, 220f, 520f);                                         // east
        Field(20f, -24f, 650f, 30f, 646f, -12f, 688.5f, 12f, 688.5f);                           // the plaza: two of them near enough the door guns to be worth an arrow
    }

    /// Mines at (x, z) pairs, each hung over the first thing under it from `fromY` down — and nowhere there is nothing
    static void Field(float fromY, params float[] xz)
    {
        for (int i = 0; i + 1 < xz.Length; i += 2)
        {
            var top = new Vector3(xz[i], fromY, xz[i + 1]);
            if (!Physics.Raycast(top, Vector3.down, out RaycastHit hit, fromY + 8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            HoverMine.Lay(root, cfg, hit.point);
        }
    }

    /// The House's drones, holding station over all of it: an answer, wherever you are, to "and now what" — a street
    /// line up the middle, a skyway beside the elevated line, one over every stub, four over the cut's corners, both
    /// flanks of the clock tower and two under its belly, a loose net over the open ground. Nothing within reach of
    /// the house's roof: that still has to be earned.
    static void Drones()
    {
        float cx = Cut.x, cz = Cut.z, dx = CutHalfX * 0.5f, dz = CutHalfZ * 0.42f;
        float[] at =
        {
            2f, 12f, 42f,   -22f, 13f, 16f,
            -6f, 14f, 70f,   6f, 16f, 102f,   -6f, 15f, 134f,   6f, 17f, 166f,   -6f, 16f, 198f,   0f, 21f, 224f,
            -40f, 18f, 100f,   -100f, 20f, 150f,   -160f, 28f, 110f,   -190f, 20f, 90f,   -214f, 22f, 46f,   -72f, 19f, 168f,
            50f, 18f, 80f,   130f, 22f, 140f,   178f, 37f, 176f,   214f, 33f, 30f,
            -112f, 43f, 216f,   -145f, 43f, 216f,   -178f, 43f, 216f,   -211f, 43f, 216f,   -244f, 43f, 216f,   -268f, 47f, 230f,
            115f, 44f, 230f,   160f, 44f, 236f,   210f, 44f, 224f,   250f, 46f, 230f,
            -238f, 19f, 230f,   -192f, 19f, 230f,   -146f, 19f, 230f,
            cx + dx, 20f, cz + dz,   cx - dx, 20f, cz + dz,   cx + dx, 20f, cz - dz,   cx - dx, 20f, cz - dz,
            88f, 30f, 316f,   134f, 52f, 324f,   150f, 12f, 470f,   136f, 16f, 410f,
            -160f, 16f, 500f,   -100f, 18f, 540f,   -40f, 16f, 500f,   20f, 18f, 530f,   80f, 16f, 500f,
            140f, 20f, 560f,   -130f, 20f, 620f,   -60f, 18f, 600f,   60f, 18f, 610f,   0f, 20f, 570f,
            -240f, 16f, 300f,   -180f, 18f, 340f,   -230f, 20f, 400f,   -170f, 18f, 450f,   -250f, 18f, 520f,   -200f, 20f, 580f,   -230f, 18f, 660f,   -150f, 20f, 700f,
            220f, 18f, 300f,   240f, 20f, 380f,   230f, 18f, 470f,   250f, 20f, 560f,   160f, 16f, 260f,
            -52f, 24f, 690f,   52f, 24f, 690f,   -46f, 28f, 752f,   46f, 28f, 752f,
        };
        for (int i = 0; i + 2 < at.Length; i += 3) LevelKit.Orb(root, new Vector3(at[i], at[i + 1], at[i + 2]));
        float[] west = { 0.12f, 0.27f, 0.42f, 0.57f, 0.8f, 0.92f }, east = { 0.2f, 0.5f, 0.85f };
        foreach (float t in west) LevelKit.Orb(root, SurfaceKit.OnSpan(TowerLow, TowerHigh, t, 26f, 12f));
        foreach (float t in east) LevelKit.Orb(root, SurfaceKit.OnSpan(TowerLow, TowerHigh, t, -26f, 12f));
    }
}
