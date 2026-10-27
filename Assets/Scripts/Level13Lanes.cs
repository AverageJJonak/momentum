using UnityEngine;

/// <summary>
/// The ways between the hubs (Level13Builder): the galleries, the sky line and
/// the yards out of the Hall; the courts, the gap and the terraces out of the
/// Exchange; the last line out of the Junction. Then who holds them, the
/// neighbours' roofs included, the district's defences, the mines, the drones.
/// </summary>
public static partial class Level13Builder
{
    // ------------------------------------------------------------ out of the Hall

    /// WEST. Two towers split by a sky lobby: in through the balustrade, past the columns and the core, out the far
    /// side. A fire escape up the first one's west face to the ledge over the lobby, a nest and a drone line from it
    static void Galleries()
    {
        RoofKit.SplitTower(new Vector3(-130f, 0f, 200f), 60f, 50f, 240f, 9f, 80f, 0f);   // lobby 240: x -154.5 .. -105.5, z 179.5 .. 220.5; upper ledge 298.6, upper top 329
        RoofKit.SplitTower(new Vector3(-150f, 0f, 470f), 56f, 48f, 262f, 9f, 70f, 0f);   // lobby 262: x -173 .. -127, z 450.3 .. 489.7
        RoofKit.Bridge(new Vector3(-130f, 240f, 220.7f), new Vector3(-150f, 262f, 450.1f));
        RoofKit.Bridge(new Vector3(-150f, 262f, 489.9f), new Vector3(-61.3f, ExchangeH, 660f));
        RoofKit.Escape(new Vector3(-157.5f, 0f, 200f), 241f, 298f, Vector3.forward);
        RoofKit.Beacon(new Vector3(-118f, 240f, 184f));
        RoofKit.Beacon(new Vector3(-162f, 262f, 484f));
        RoofKit.Beacon(new Vector3(-130f, 298.6f, 190f));
        LevelKit.HoloSign(root, new Vector3(-130f, 243.4f, 178.6f), Vector3.back, 10f, 1f, "GALLERY 40  ·  SKY LOBBY  ·  RESIDENTS ONLY");
        LevelKit.HoloSign(root, new Vector3(-150f, 265.4f, 449.4f), Vector3.back, 10f, 1f, "GALLERY 52  ·  SKY LOBBY  ·  RESIDENTS ONLY");
        LevelKit.HoloSign(root, new Vector3(-157.5f, 246f, 206f), Vector3.left, 6f, 0.8f, "FIRE ESCAPE  ·  KEEP CLEAR", true);
    }

    /// NORTH. The fast way: a stack, a blade pair with the pad over its slot, and the long viaduct under the guns
    static void SkyLine()
    {
        RoofKit.StackTower(new Vector3(0f, 0f, 230f), 44f, 40f, 262f, 0f);   // top 36 x 32.8: z 213.6 .. 246.4
        RoofKit.Parapet(new Vector3(0f, 262f, 230f), 35.8f, 32.6f, 0f, 8f);
        RoofKit.BladeTower(new Vector3(0f, 0f, 420f), 40f, 276f, 0f);        // low blade z 412 (237.4), high blade z 428 (276)
        RoofKit.Bridge(new Vector3(0f, 262f, 246.6f), new Vector3(0f, 237.4f, 407.4f));
        LevelKit.JumpPad(root, new Vector3(0f, 237.4f, 412f), 0f, 45f, 4f);   // 40 m up, the high blade
        RoofKit.Viaduct(new Vector3(0f, 276f, 432.4f), new Vector3(0f, ExchangeH, 634.7f), 10f);
        RoofKit.Beacon(new Vector3(12f, 262f, 226f));
        RoofKit.Beacon(new Vector3(-14f, 237.4f, 412f));
        RoofKit.Beacon(new Vector3(14f, 276f, 428f));
        RoofKit.FastLine(new Vector3(0f, 262f, 223f), new Vector3(0f, 262f, 237f));   // the through line on the stack, landing to ramp
        LevelKit.ClockFace(root, new Vector3(0f, 245f, 213.15f), Vector3.back, 6f, 300f, 60f);   // the sky line stack's south face, over the way in
        LevelKit.HoloSign(root, new Vector3(0f, 265.4f, 214f), Vector3.back, 10f, 1f, "SKY LINE 5  ·  BLADE PAIR 9  ·  LEAP PAD LIVE");
        LevelKit.HoloSign(root, new Vector3(0f, 279.4f, 436f), Vector3.back, 12f, 1.1f, "SKY LINE 5  ·  THE EXCHANGE  ·  MIND THE GUNS", true);
    }

    /// EAST. A round tower and its halo, then a roof garden: cover, and the people in it
    static void Yards()
    {
        RoofKit.RoundTower(new Vector3(130f, 0f, 210f), 44f, 245f);           // top drum 245 (dia 30.8: z 194.6 .. 225.4), halo 261
        LevelKit.JumpPad(root, new Vector3(130f, 245f, 210f), 0f, 30f, 0f);    // up to the halo
        RoofKit.StackTower(new Vector3(150f, 0f, 470f), 70f, 60f, 262f, 0f);   // top 57.4 x 49.2: x 121.3 .. 178.7, z 445.4 .. 494.6
        RoofKit.Garden(new Vector3(150f, 262f, 470f), 56f, 48f, 0f);
        RoofKit.Bridge(new Vector3(130f, 245f, 225.2f), new Vector3(150f, 262f, 445.6f));
        RoofKit.Bridge(new Vector3(150f, 262f, 494.4f), new Vector3(61.3f, ExchangeH, 660f));
        RoofKit.Beacon(new Vector3(130f, 245f, 200f));
        RoofKit.Beacon(new Vector3(130f, 261f, 210f));
        RoofKit.Beacon(new Vector3(172f, 262f, 452f));
        LevelKit.HoloSign(root, new Vector3(130f, 248.4f, 197f), Vector3.back, 10f, 1f, "ROTUNDA 15  ·  HALO DECK ABOVE");
        LevelKit.HoloSign(root, new Vector3(150f, 265.4f, 448f), Vector3.back, 10f, 1f, "THE YARDS  ·  ROOF GARDEN  ·  KEEP OFF THE GRASS");
    }

    // ------------------------------------------------------------ out of the Exchange

    /// WEST. Two towers with a covered court between them, then a sky lobby and the way down to the junction
    static void Courts()
    {
        RoofKit.StackTower(new Vector3(-140f, 0f, 880f), 50f, 44f, 292f, 0f);   // top 41 x 36: z 862 .. 898
        RoofKit.StackTower(new Vector3(-140f, 0f, 1060f), 50f, 44f, 300f, 0f);  // z 1042 .. 1078
        RoofKit.Court(new Vector3(-140f, 292f, 898.2f), new Vector3(-140f, 300f, 1041.8f), 16f, 7f);
        RoofKit.SplitTower(new Vector3(-130f, 0f, 1250f), 56f, 48f, 300f, 9f, 70f, 0f);   // lobby 300: z 1230.3 .. 1269.7
        RoofKit.Bridge(new Vector3(-140f, 300f, 1078.2f), new Vector3(-130f, 300f, 1230.1f));
        RoofKit.Bridge(new Vector3(-130f, 300f, 1269.9f), new Vector3(-20f, LowBlade, 1387.4f));
        RoofKit.Beacon(new Vector3(-156f, 292f, 870f));
        RoofKit.Beacon(new Vector3(-156f, 300f, 1070f));
        RoofKit.Beacon(new Vector3(-118f, 300f, 1236f));
        LevelKit.StatuePlinth(root, new Vector3(-128f, 292.06f, 868f), 0f, 0.8f);   // a bronze on the first court tower, facing the arcade
        LevelKit.HoloSign(root, new Vector3(-140f, 295.4f, 861.4f), Vector3.back, 10f, 1f, "THE COURTS  ·  ARCADE LEVEL");
        LevelKit.HoloSign(root, new Vector3(-130f, 303.4f, 1229.4f), Vector3.back, 10f, 1f, "GALLERY 61  ·  SKY LOBBY  ·  RESIDENTS ONLY");
    }

    /// NORTH. Two stacks and the bridge the response cut between them, twenty-two metres of it gone: a drone line over
    /// the gap, or the long way round by the neighbours
    static void Gap()
    {
        RoofKit.StackTower(new Vector3(0f, 0f, 860f), 44f, 40f, 300f, 0f);    // top 36 x 32.8: z 843.6 .. 876.4
        RoofKit.StackTower(new Vector3(0f, 0f, 1120f), 46f, 42f, 314f, 0f);   // top 37.7 x 34.4: z 1102.8 .. 1137.2
        RoofKit.BrokenBridge(new Vector3(0f, 300f, 876.6f), new Vector3(0f, 314f, 1102.6f), 22f);
        RoofKit.Bridge(new Vector3(0f, 314f, 1137.4f), new Vector3(0f, LowBlade, 1387.4f));
        RoofKit.Beacon(new Vector3(-12f, 300f, 850f));
        RoofKit.Beacon(new Vector3(12f, 314f, 1130f));
        RoofKit.FastLine(new Vector3(0f, 300f, 853f), new Vector3(0f, 300f, 866f));     // the through lines, landing to ramp
        RoofKit.FastLine(new Vector3(0f, 314f, 1113f), new Vector3(0f, 314f, 1128f));
        RoofKit.KickPair(0f, 300f, 873f);   // the kick chain into the cut bridge (its torn end wears the lip, the catch hangs under the gap)
        LevelKit.HoloSign(root, new Vector3(0f, 303.4f, 878f), Vector3.back, 10f, 1f, "BRIDGE 9 CUT  ·  DO NOT ATTEMPT", true);
    }

    /// EAST. Two roof gardens rising, a round tower and its halo, the long way down to the junction
    static void Terraces()
    {
        RoofKit.StackTower(new Vector3(150f, 0f, 880f), 60f, 52f, 288f, 0f);    // top 49.2 x 42.6: z 858.7 .. 901.3
        RoofKit.Garden(new Vector3(150f, 288f, 880f), 48f, 41f, 0f);
        RoofKit.StackTower(new Vector3(160f, 0f, 1050f), 56f, 50f, 302f, 0f);   // top 45.9 x 41: z 1029.5 .. 1070.5
        RoofKit.Garden(new Vector3(160f, 302f, 1050f), 44f, 39f, 0f);
        RoofKit.RoundTower(new Vector3(150f, 0f, 1230f), 44f, 316f);            // top drum 316 (z 1214.6 .. 1245.4), halo 332
        LevelKit.JumpPad(root, new Vector3(150f, 316f, 1230f), 0f, 30f, 0f);
        RoofKit.Bridge(new Vector3(150f, 288f, 901.5f), new Vector3(160f, 302f, 1029.3f));
        RoofKit.Bridge(new Vector3(160f, 302f, 1070.7f), new Vector3(150f, 316f, 1214.4f));
        RoofKit.Bridge(new Vector3(150f, 316f, 1245.6f), new Vector3(20f, LowBlade, 1387.4f));
        RoofKit.Beacon(new Vector3(170f, 288f, 866f));
        RoofKit.Beacon(new Vector3(180f, 302f, 1036f));
        RoofKit.Beacon(new Vector3(150f, 316f, 1220f));
        LevelKit.HoloSign(root, new Vector3(150f, 291.4f, 861f), Vector3.back, 10f, 1f, "THE TERRACES  ·  GARDEN 1");
        LevelKit.HoloSign(root, new Vector3(150f, 319.4f, 1217f), Vector3.back, 10f, 1f, "ROTUNDA 19  ·  JUNCTION 3 AHEAD");
    }

    // ------------------------------------------------------------ out of the Junction

    /// A tram viaduct north to the Terminal: two trams out of step, the strip between them, the guns on the deck. The
    /// one way to the Terminal, so (course-grammar) the deck is sixteen wide and the tracks 4.6 m out: the strip between
    /// the trams is 6.2 m, a lane; the guns stand outside the tracks against the kerbs
    const float LastLineW = 16f, LastLineTrack = 4.6f, LastLineKerbside = 7f;

    static void LastLine()
    {
        RoofKit.Viaduct(HighBladeN, TerminalS, LastLineW);
        Vector3 da = RoofKit.LastDeckA, db = RoofKit.LastDeckB;   // where the deck actually runs: bearing on the roofs it joins
        Vector3 dir = (db - da).normalized;
        Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 a = da + side * (s * LastLineTrack) + dir * 8f + Vector3.up * 0.02f, b = db + side * (s * LastLineTrack) - dir * 8f + Vector3.up * 0.02f;   // on the deck, not in it
            LevelKit.Rail(root, a, b);
            if (CityVista.Preview) continue;
            Tram.Create(root, cfg, a + dir * 2f, b - dir * 2f, s < 0 ? 0f : 0.5f);
        }
        SurfaceKit.Look(LevelKit.Led, (da + db) * 0.5f + Vector3.up * 0.03f, new Vector3(0.3f, 0.05f, Vector3.Distance(da, db) - 16f), SurfaceKit.SpanRot(da, db));
        RoofKit.Beacon(da + dir * 14f - side * LastLineKerbside);
        LevelKit.HoloSign(root, da + dir * 6f + Vector3.up * 3.4f, -dir, 11f, 1.1f, "SKY LINE 6  ·  SERVICE IS NORMAL  ·  MIND THE TRAMS");
        Roofline();   // the last thing built before the district is welded: every hub stands by now
    }

    // ------------------------------------------------------------ the roofline

    /// What the hubs carry on their faces and off their corners: clock faces, searchlights outside the roofs, a bronze,
    /// a mast on the terminal's needle. All looks but the plinth's steps and the bronze standing on them.
    static void Roofline()
    {
        LevelKit.ClockFace(root, new Vector3(0f, 236f, 35.05f), Vector3.forward, 6f, 300f, 60f);          // the hall, north face
        RoofKit.Searchlight(FootHall, 57.4f, 49.2f, HallH, 0f, 0, 0.1f);
        LevelKit.ClockFace(root, new Vector3(0f, 262f, 634.45f), Vector3.back, 8f, 300f, 60f);           // the exchange, south face, over the viaduct's end
        LevelKit.StatuePlinth(root, new Vector3(-30f, ExchangeH + 0.06f, 712f), 180f, 1f);
        RoofKit.Searchlight(FootExchange, 123f, 90.2f, ExchangeH, 0f, 2, 0.5f);
        RoofKit.Searchlight(FootExchange, 123f, 90.2f, ExchangeH, 0f, 3, 1.3f);
        LevelKit.ClockFace(root, new Vector3(0f, 285f, 1742.95f), Vector3.back, 7f, 300f, 60f);          // the terminal, fourth tier, south face
        RoofKit.Searchlight(FootTerminal, 33.4f, 28.2f, TerminalH, 0f, 2, 0.7f);
        RoofKit.Searchlight(FootTerminal, 33.4f, 28.2f, TerminalH, 0f, 3, 1.6f);
        LevelKit.RadioMast(root, FootTerminal + Vector3.up * (TerminalH + 40f), 12f);                    // on the needle
    }

    // ------------------------------------------------------------ who holds the blocks

    static void Units()
    {
        // The hall: a welcome, and one of everything watching the three ways
        Enemy.CreateSkitters(new Vector3(-10f, HallH, 24f), cfg);
        Enemy.CreateSkitters(new Vector3(12f, HallH, 6f), cfg);
        Enemy.CreateMarshal(new Vector3(0f, HallH, 28f), cfg);
        Enemy.CreateSentry(new Vector3(-22f, HallH, 30f), cfg);
        // The galleries: the response inside the lobbies, among the columns
        Enemy.CreateMarshal(new Vector3(-142f, 240f, 190f), cfg);
        Enemy.CreateSkitters(new Vector3(-118f, 240f, 206f), cfg);
        Enemy.CreateKettler(new Vector3(-146f, 240f, 212f), cfg);
        Enemy.CreateWarden(new Vector3(-130f, 240f, 214f), cfg);
        Enemy.CreateSentry(new Vector3(-130f, 298.6f, 205f), cfg);   // the nest over the lobby
        Enemy.CreateBlaster(new Vector3(-160f, 262f, 460f), cfg);
        Enemy.CreateMarshal(new Vector3(-138f, 262f, 480f), cfg);
        Enemy.CreateSkitters(new Vector3(-150f, 262f, 456f), cfg);
        Enemy.CreateLurker(new Vector3(-165f, 262f, 484f), cfg);
        Enemy.CreateMoth(new Vector3(-100f, 258f, 340f), cfg);
        // The sky line: the guns, and the eyes off to the side
        Enemy.CreateSentry(new Vector3(-12f, 262f, 236f), cfg);
        Enemy.CreateSkitters(new Vector3(10f, 262f, 226f), cfg);
        Enemy.CreateLurker(new Vector3(12f, 237.4f, 412f), cfg);
        Enemy.CreateSentry(new Vector3(-14f, 276f, 428f), cfg);
        Enemy.CreateMarshal(new Vector3(0f, 276f, 500f), cfg);
        Enemy.CreateSentry(new Vector3(3f, 276f, 560f), cfg);
        Enemy.CreateSentry(new Vector3(-3f, 278f, 600f), cfg);
        Enemy.CreateOcular(new Vector3(60f, 290f, 520f), cfg);
        Enemy.CreateOcular(new Vector3(-60f, 292f, 580f), cfg);
        Enemy.CreateMoth(new Vector3(0f, 288f, 470f), cfg);
        Enemy.CreateMoth(new Vector3(8f, 290f, 610f), cfg);
        // The yards: the halo and the garden, hounds in the hedges
        Enemy.CreateSkitters(new Vector3(130f, 245f, 214f), cfg);
        Enemy.CreateScourer(new Vector3(140f, 245f, 204f), cfg);
        Enemy.CreateSentry(new Vector3(130f, 261f, 222f), cfg);
        Enemy.CreateHound(new Vector3(134f, 262f, 456f), cfg);
        Enemy.CreateHound(new Vector3(166f, 262f, 486f), cfg);
        Enemy.CreateMarshal(new Vector3(150f, 262f, 488f), cfg);
        Enemy.CreateWarden(new Vector3(160f, 262f, 462f), cfg);
        Enemy.CreateMoth(new Vector3(140f, 275f, 470f), cfg);
        // The exchange: the stand, from every side
        Enemy.CreateWarden(new Vector3(-30f, ExchangeH, 690f), cfg);
        Enemy.CreateWarden(new Vector3(30f, ExchangeH, 670f), cfg);
        Enemy.CreateMarshal(new Vector3(-40f, ExchangeH, 650f), cfg);
        Enemy.CreateMarshal(new Vector3(40f, ExchangeH, 712f), cfg);
        Enemy.CreateScourer(new Vector3(0f, ExchangeH, 700f), cfg);
        Enemy.CreateSkitters(new Vector3(-20f, ExchangeH, 720f), cfg);
        Enemy.CreateSkitters(new Vector3(20f, ExchangeH, 645f), cfg);
        Enemy.CreateKettler(new Vector3(50f, ExchangeH, 690f), cfg);
        Enemy.CreateBlaster(new Vector3(-50f, ExchangeH, 700f), cfg);
        Enemy.CreateSentry(new Vector3(-56f, ExchangeH, 640f), cfg);
        Enemy.CreateSentry(new Vector3(56f, ExchangeH, 720f), cfg);
        Enemy.CreateOverseer(new Vector3(0f, ExchangeH + 30f, 680f), cfg);
        Enemy.CreateMoth(new Vector3(-40f, ExchangeH + 20f, 660f), cfg);
        Enemy.CreateMoth(new Vector3(40f, ExchangeH + 20f, 700f), cfg);
        // The courts: under the canopy
        Enemy.CreateChaser(new Vector3(-140f, 292f, 872f), cfg);
        Enemy.CreateHound(new Vector3(-144f, 294f, 930f), cfg);
        Enemy.CreateHound(new Vector3(-136f, 296f, 990f), cfg);
        Enemy.CreateMarshal(new Vector3(-140f, 298f, 1020f), cfg);
        Enemy.CreateKettler(new Vector3(-134f, 300f, 1060f), cfg);
        Enemy.CreateWarden(new Vector3(-130f, 300f, 1244f), cfg);
        Enemy.CreateSkitters(new Vector3(-118f, 300f, 1256f), cfg);
        Enemy.CreateBlaster(new Vector3(-146f, 300f, 1262f), cfg);
        // The gap: what holds the two ends
        Enemy.CreateSentry(new Vector3(-12f, 300f, 870f), cfg);
        Enemy.CreateSkitters(new Vector3(10f, 300f, 856f), cfg);
        Enemy.CreateMarshal(new Vector3(0f, 314f, 1110f), cfg);
        Enemy.CreateSentry(new Vector3(14f, 314f, 1128f), cfg);
        Enemy.CreateLurker(new Vector3(-14f, 314f, 1126f), cfg);
        Enemy.CreateOcular(new Vector3(50f, 330f, 990f), cfg);
        Enemy.CreateMoth(new Vector3(0f, 322f, 960f), cfg);
        Enemy.CreateMoth(new Vector3(6f, 326f, 1030f), cfg);
        // The terraces: the gardens, the halo
        Enemy.CreateHound(new Vector3(136f, 288f, 870f), cfg);
        Enemy.CreateScourer(new Vector3(164f, 288f, 892f), cfg);
        Enemy.CreateMarshal(new Vector3(150f, 288f, 896f), cfg);
        Enemy.CreateWarden(new Vector3(160f, 302f, 1044f), cfg);
        Enemy.CreateSkitters(new Vector3(176f, 302f, 1060f), cfg);
        Enemy.CreateLurker(new Vector3(146f, 302f, 1036f), cfg);
        Enemy.CreateSentry(new Vector3(150f, 316f, 1238f), cfg);
        Enemy.CreateScourer(new Vector3(140f, 316f, 1224f), cfg);
        Enemy.CreateMoth(new Vector3(150f, 330f, 1150f), cfg);
        // The junction
        Enemy.CreateMarshal(new Vector3(-12f, LowBlade, 1392f), cfg);
        Enemy.CreateSkitters(new Vector3(12f, LowBlade, 1392f), cfg);
        Enemy.CreateSentry(new Vector3(-20f, JunctionH, 1408f), cfg);
        Enemy.CreateWarden(new Vector3(10f, JunctionH, 1408f), cfg);
        Enemy.CreateOcular(new Vector3(70f, 330f, 1400f), cfg);
        // The last line: the guns on the deck, two striders, moths over it
        Vector3 dir = (TerminalS - HighBladeN).normalized;
        Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
        Enemy.CreateSentry(Vector3.Lerp(HighBladeN, TerminalS, 0.3f) + side * LastLineKerbside, cfg);     // kerbside, outside the tracks
        Enemy.CreateSentry(Vector3.Lerp(HighBladeN, TerminalS, 0.6f) - side * LastLineKerbside, cfg);
        Enemy.CreateMarshal(Vector3.Lerp(HighBladeN, TerminalS, 0.45f) + side * LastLineKerbside, cfg);
        Enemy.CreatePursuit(Vector3.Lerp(HighBladeN, TerminalS, 0.2f), cfg);
        Enemy.CreatePursuit(Vector3.Lerp(HighBladeN, TerminalS, 0.75f), cfg);
        for (int i = 0; i < 3; i++) Enemy.CreateMoth(Vector3.Lerp(HighBladeN, TerminalS, 0.25f + i * 0.25f) + Vector3.up * 9f, cfg);
        // The terminal: the last stand
        Enemy.CreateWarden(new Vector3(-6f, TerminalH, 1758f), cfg);
        Enemy.CreateWarden(new Vector3(6f, TerminalH, 1764f), cfg);
        Enemy.CreateMarshal(new Vector3(-13f, 300.3f, 1760f), cfg);
        Enemy.CreateMarshal(new Vector3(13f, 300.3f, 1760f), cfg);
        Enemy.CreateSkitters(new Vector3(6f, TerminalH, 1752f), cfg);
        Enemy.CreateKettler(new Vector3(-8f, TerminalH, 1766f), cfg);
        Enemy.CreateSentry(new Vector3(-16f, 300.3f, 1748f), cfg);
        Enemy.CreateSentry(new Vector3(16f, 300.3f, 1772f), cfg);
        Enemy.CreateOcular(new Vector3(30f, 342f, 1790f), cfg);
        Enemy.CreateOverseer(new Vector3(0f, TerminalH + 28f, 1740f), cfg);
        Enemy.CreateInterceptor(new Vector3(-30f, TerminalH + 14f, 1780f), cfg);
        Enemy.CreateInterceptor(new Vector3(30f, TerminalH + 14f, 1720f), cfg);
    }

    /// The neighbours are the level too: every other full roof in the inner lanes has something on it, the ones over
    /// the route have an eye over them, and a drone hangs over every third so there is a way back up to the line
    static void NeighbourUnits()
    {
        int n = 0;
        foreach (var b in neighbours)
        {
            if (!b.full) continue;
            RoofKit.RoofOf(b, out Vector3 top, out float hx, out float hz);
            if (hx < 6f || hz < 3f) continue;
            n++;
            if (n % 2 == 0) EnemyPool.Ground(top + Vector3.up * 0.1f + new Vector3(Random.Range(-hx * 0.4f, hx * 0.4f), 0f, Random.Range(-hz * 0.4f, hz * 0.4f)), cfg);
            if (n % 5 == 0 && top.y > RouteHeight(top.z) - 6f) EnemyPool.Air(top + Vector3.up * 9f, cfg);
            if (n % 3 == 0) LevelKit.Orb(root, top + Vector3.up * 14f);
        }
    }

    // ------------------------------------------------------------ the district's own defences

    static void Defences()
    {
        Mast(new Vector3(-16f, 276f, 470f), 9f, new Vector3(4f, 0f, 0f), new Vector3(0f, 276f, 520f), 6f, 60f, 0.28f, 0f);
        Mast(new Vector3(56f, ExchangeH, 660f), 12f, new Vector3(-4f, 0f, 0f), new Vector3(0f, ExchangeH, 680f), 50f, 34f, 0.27f, 1.2f);
        Mast(new Vector3(-12f, 300.3f, 1772f), 26f, new Vector3(4f, 0f, 0f), new Vector3(0f, TerminalH, 1760f), 14f, 12f, 0.34f, 2.4f);
        Vector3 mid = (HighBladeN + TerminalS) * 0.5f;
        Vector3 side = Vector3.Cross(Vector3.up, (TerminalS - HighBladeN).normalized).normalized;
        Mast(mid + side * (LastLineKerbside + 0.6f), 8f, -side * 3f, mid, 8f, 60f, 0.25f, 0.6f);   // bracketed off the kerb: its base plate ends where the tram's side runs
        // Arc fences across the ways: the sky line's viaduct, the exchange's north bridge, the last line
        Vector3 v0 = new Vector3(0f, 276f, 432.4f), v1 = new Vector3(0f, ExchangeH, 634.7f);
        LevelKit.ArcGap(root, SurfaceKit.OnSpan(v0, v1, 0.7f, -5f), SurfaceKit.OnSpan(v0, v1, 0.7f, 5f), 3f, 0f, 0.4f);
        Vector3 n0 = new Vector3(0f, ExchangeH, 724.9f), n1 = new Vector3(0f, 300f, 843.4f);
        LevelKit.ArcGap(root, SurfaceKit.OnSpan(n0, n1, 0.5f, -3f), SurfaceKit.OnSpan(n0, n1, 0.5f, 3f), 3.2f, 0.3f, 0.45f);
        LevelKit.ArcGap(root, mid - side * LastLineKerbside, mid + side * LastLineKerbside, 3f, 0.5f, 0.4f);
        LevelKit.HoloSign(root, new Vector3(-20f, ExchangeH + 3f, 656f), Vector3.back, 12f, 1f, "SWEEP LIGHTS DIRECT FIRE  ·  STAY OUT OF THE LIGHT", true);
    }

    static void Mast(Vector3 foot, float height, Vector3 arm, Vector3 centre, float reachX, float reachZ, float rate, float phase)
        => RoofKit.Mast(foot, height, arm, centre, reachX, reachZ, rate, phase);

    /// Mines on the stopping places: the landings, the lobbies' doors, the gardens, the blades. Never on a line taken at speed
    static void Mines()
    {
        RoofKit.Field(HallH + 30f, -6f, 8f, 14f, 24f);   // off the kick stacks' line (course-grammar: never on a line taken at speed)
        RoofKit.Field(270f, -138f, 186f, -122f, 214f, -158f, 458f, -142f, 482f);
        RoofKit.Field(290f, 10f, 238f, -10f, 222f, 10f, 412f, -10f, 428f);
        RoofKit.Field(275f, 124f, 206f, 136f, 218f, 130f, 456f, 170f, 484f);
        RoofKit.Field(ExchangeH + 30f, -44f, 660f, 44f, 700f, -10f, 716f, 10f, 646f);
        RoofKit.Field(330f, -150f, 890f, -130f, 1050f, -140f, 1240f, -120f, 1260f);
        RoofKit.Field(330f, 12f, 850f, -13f, 866f, 8f, 1112f, -8f, 1130f);   // the gap stack's pair sits wide of its kick stacks
        RoofKit.Field(330f, 160f, 872f, 140f, 890f, 170f, 1040f, 150f, 1062f);
        RoofKit.Field(JunctionH + 30f, -16f, 1392f, 16f, 1392f, -12f, 1408f, 12f, 1408f);
        RoofKit.Field(TerminalH + 30f, -8f, 1758f, 8f, 1762f, -12f, 1750f, 12f, 1770f);
    }

    /// The company's drones, holding station: over the gaps, along the lanes, a line over the cut bridge, and a straight
    /// run from the galleries' nest to the exchange for whoever climbed for it
    static void Drones()
    {
        float[] at =
        {
            -80f, 262f, 100f,   0f, 272f, 120f,   80f, 262f, 100f,
            -140f, 262f, 330f,   0f, 262f, 320f,   140f, 268f, 330f,
            0f, 292f, 300f,   0f, 296f, 380f,   0f, 300f, 460f,   0f, 300f, 540f,   0f, 296f, 610f,
            -130f, 318f, 250f,   -110f, 316f, 360f,   -90f, 312f, 470f,   -70f, 306f, 580f,
            -100f, 300f, 790f,   0f, 300f, 790f,   100f, 300f, 790f,
            0f, 318f, 960f,   0f, 322f, 1000f,   0f, 320f, 1040f,
            -140f, 316f, 1150f,   150f, 330f, 1140f,
            -70f, 300f, 1330f,   70f, 300f, 1330f,   0f, 340f, 1500f,   0f, 350f, 1620f,   0f, 350f, 1720f,
        };
        for (int i = 0; i + 2 < at.Length; i += 3) LevelKit.Orb(root, new Vector3(at[i], at[i + 1], at[i + 2]));
    }
}
