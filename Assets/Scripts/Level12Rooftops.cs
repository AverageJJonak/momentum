using UnityEngine;

/// <summary>
/// The rooftops themselves (Level12Builder), as a WEB like the district after
/// them: from the gate roof three ways north — the stacks and their cut
/// bridges to the west, the cut line straight up the middle, the slot and its
/// leap pads to the east — all landing on the arcology's second terrace; up
/// the arcology by thermal or by pad to the viewing terrace, and three ways
/// on from it — the halo towers to the west, the tram line up the middle with
/// a station under it, the gardens to the east — all converging on the
/// spire's top. Then who holds them, the district's defences, the mines, the
/// drones, and the city round it all.
/// </summary>
public static partial class Level12Builder
{
    // The gate roof, the arcology and the spire: the three hubs
    static readonly Vector3 FootA = new Vector3(0f, 0f, 20f), FootArc = new Vector3(0f, 0f, 880f), FootS = new Vector3(0f, 0f, 1800f);
    const float RoofA = 84f, ArcH = 300f, SpireH = 250f;   // arcology tier tops 90, 150, 198, 237, 270, 300; spire 95, 155, 197.5, 227.5, 250
    // The stacks (west, first stretch)
    static readonly Vector3 FootS1 = new Vector3(-120f, 0f, 150f), FootS2 = new Vector3(-125f, 0f, 300f), FootS3 = new Vector3(-115f, 0f, 450f), FootS4 = new Vector3(-110f, 0f, 640f);
    const float RoofS1 = 100f, RoofS2 = 112f, RoofS3 = 126f, RoofS4 = 138f;
    // The cut line (middle, first stretch): a blade pair, two stacks
    static readonly Vector3 FootC1 = new Vector3(0f, 0f, 260f), FootC3 = new Vector3(0f, 0f, 520f), FootC4 = new Vector3(0f, 0f, 700f);
    const float BladeC1 = 130f, RoofC3 = 146f, RoofC4 = 150f;
    // The slot (east, first stretch): three blade pairs and a round tower
    static readonly Vector3 FootE1 = new Vector3(120f, 0f, 200f), FootE2 = new Vector3(125f, 0f, 380f), FootE3 = new Vector3(115f, 0f, 560f), FootE4 = new Vector3(120f, 0f, 720f);
    const float BladeE1 = 120f, BladeE2 = 136f, BladeE3 = 152f, RoundE4 = 150f;
    // The halo (west, second stretch), the line (middle), the gardens (east)
    static readonly Vector3 FootI = new Vector3(-110f, 0f, 1120f), FootJ = new Vector3(-120f, 0f, 1330f), FootK = new Vector3(-110f, 0f, 1560f);
    const float RoundI = 240f, RoundJ = 300f, RoofK = 270f;
    static readonly Vector3 DeckA = new Vector3(0f, 285f, 1000f), DeckB = new Vector3(0f, 262f, 1690f), FootStation = new Vector3(0f, 0f, 1350f);
    const float RoofStation = 268f;
    static readonly Vector3 FootG1 = new Vector3(120f, 0f, 1050f), FootG2 = new Vector3(130f, 0f, 1300f), FootG3 = new Vector3(115f, 0f, 1540f);
    const float RoofG1 = 288f, LobbyG2 = 275f, RoundG3 = 262f;

    // ------------------------------------------------------------ the gate roof

    /// Where the wound's bridge arrives: the roof of the first tower, the relay tower behind you, three ways north
    static void GateRoof()
    {
        // The relay tower you climbed, a look only, and the bridge off its roof to this one (the last stretch of it torn)
        SurfaceKit.Look(CityKit.Mass, new Vector3(0f, 41.5f, -82f), new Vector3(44f, 83f, 44f));
        SurfaceKit.Look(LevelKit.Wall, new Vector3(0f, 83.4f, -82f), new Vector3(44f, 1.2f, 44f));   // its top at the roof's height: the bridge's deck sits a hair over it
        LevelKit.SearchlightTower(root, new Vector3(-14f, 84.1f, -70f), 7f, 0.2f);   // the relay's roof is nobody's: a searchlight and a mast on it
        LevelKit.RadioMast(root, new Vector3(15f, 84.1f, -96f), 16f);
        Bridge(new Vector3(0f, RoofA, -60f), new Vector3(0f, RoofA, 3.8f));
        for (int s = -1; s <= 1; s += 2)
        {
            SurfaceKit.Block(LevelKit.Graphite, new Vector3(s * 2.3f, RoofA + 2.2f, -14f), new Vector3(1.2f, 4.4f, 1.2f));   // the gate you came through
            LevelKit.Flutes(root, new Vector3(s * 2.3f, RoofA + 2.2f, -14f), new Vector3(1.2f, 4.4f, 1.2f));                // its corners are edge boosts: cream flutes say so
        }

        StackTower(FootA, 44f, 40f, RoofA, 0f);   // top 36 x 32.8: x ±18, z 3.6 .. 36.4
        RoofKit.FastLine(new Vector3(0f, RoofA, 5f), new Vector3(0f, RoofA, 30f));   // the fast way is straight on: the cut line
        LevelKit.ClockFace(root, new Vector3(0f, 70f, 36.85f), Vector3.forward, 6f, 300f, 60f);   // on the north face, for the city
        RoofKit.Searchlight(FootA, 36.08f, 32.8f, RoofA, 0f, 1, 0.6f);                            // off the south-east corner, outside the roof
        Beacon(new Vector3(0f, RoofA, 24f));
        LevelKit.HoloSign(root, new Vector3(0f, RoofA + 3.4f, 34f), Vector3.back, 14f, 1.3f, "SKYLINE  ·  NO GROUND BELOW THIS LINE", true);
        LevelKit.HoloSign(root, new Vector3(-14f, RoofA + 2.2f, 12f), Vector3.right, 9f, 1f, "THE STACKS  ·  WEST");
        LevelKit.HoloSign(root, new Vector3(14f, RoofA + 2.2f, 12f), Vector3.left, 9f, 1f, "THE SLOT  ·  EAST");
        LevelKit.Floodlight(root, new Vector3(-16f, RoofA + 6f, 8f), new Vector3(0.6f, -0.6f, 0.5f), 34f, 2.4f);
        LevelKit.Milk(root, new Vector3(14f, RoofA, 8f));
    }

    /// The three ways off the gate roof, laid once the roofs they land on stand (a deck ends at a roof's edge, so the roof must be there)
    static void GateWays()
    {
        Bridge(new Vector3(-18f, RoofA, 20f), new Vector3(-120f, RoofS1, 135f));           // west: the stacks
        Bridge(new Vector3(0f, RoofA, 36.6f), new Vector3(0f, BladeC1 * 0.86f, 247.4f));  // north: the cut line
        Bridge(new Vector3(18f, RoofA, 20f), new Vector3(120f, BladeE1 * 0.86f, 187.4f)); // east: the slot
    }

    // ------------------------------------------------------------ west: the stacks

    /// Four plain towers rising, the bridges between them broken where the response broke them, a fallen storey on the third
    static void Stacks()
    {
        StackTower(FootS1, 40f, 36f, RoofS1, 0f);   // top x -136.4 .. -103.6, z 135.2 .. 164.8
        StackTower(FootS2, 42f, 38f, RoofS2, 0f);   // x -142.2 .. -107.8, z 284.4 .. 315.6
        StackTower(FootS3, 46f, 40f, RoofS3, 0f);   // x -133.9 .. -96.1, z 433.6 .. 466.4
        StackTower(FootS4, 44f, 40f, RoofS4, 0f);   // x -128 .. -92, z 623.6 .. 656.4
        Parapet(FootS1 + Vector3.up * RoofS1, 32.6f, 29.3f, 0f, 8f);
        Parapet(FootS2 + Vector3.up * RoofS2, 34.2f, 31f, 0f, 8f);
        Parapet(FootS4 + Vector3.up * RoofS4, 35.9f, 32.6f, 0f, 8f);
        BrokenBridge(new Vector3(-120f, RoofS1, 165f), new Vector3(-125f, RoofS2, 284.2f), 11f);
        Bridge(new Vector3(-125f, RoofS2, 315.8f), new Vector3(-115f, RoofS3, 433.4f));
        BrokenBridge(new Vector3(-115f, RoofS3, 466.6f), new Vector3(-110f, RoofS4, 623.4f), 15f);
        Bridge(new Vector3(-110f, RoofS4, 656.6f), new Vector3(-70f, 150f, 838f));   // onto the arcology's second terrace
        Beacon(new Vector3(-132f, RoofS1, 150f));
        Beacon(new Vector3(-138f, RoofS2, 300f));
        Beacon(new Vector3(-124f, RoofS4, 640f));
        RoofKit.KickPair(-120f, RoofS1, 152f);   // the kick chain into the first cut bridge: stacks either side of its ramp (the mines by the edge are nine metres on)
        RoofKit.FastLine(new Vector3(-120f, RoofS1, 145f), new Vector3(-120f, RoofS1, 155f));
        LevelKit.ClockFace(root, new Vector3(-125f, 95f, 283.95f), Vector3.back, 5f, 300f, 60f);   // the second stack's south face
        RoofKit.Searchlight(FootS4, 36.08f, 32.8f, RoofS4, 0f, 0, 1.1f);
        // A tower that fell against the third: its top storey lying on the roof, tilted, its windows dark, the beams it
        // tore out with it and the rubble round its foot; a deck up onto it and off the far side at speed, on stringers
        // and posts like any deck
        FallenStorey(new Vector3(-108f, RoofS3 + 2.6f, 458f), RoofS3);
        Vector3 k0 = new Vector3(-124f, RoofS3 + 0.05f, 438f), k1 = new Vector3(-110.5f, RoofS3 + 5f, 451.5f);
        SurfaceKit.SpanSlab(root, k0, k1, 5f, 0.4f, LevelKit.Slope, "StackKicker");
        for (int s = -1; s <= 1; s += 2)
            SurfaceKit.SpanBlock(LevelKit.Girder, SurfaceKit.OnSpan(k0, k1, 0f, s * 2.2f, -0.55f), SurfaceKit.OnSpan(k0, k1, 1f, s * 2.2f, -0.55f), 0.3f, 0.3f);
        for (int i = 1; i <= 3; i++)
        {
            Vector3 under = SurfaceKit.OnSpan(k0, k1, i / 4f, 0f, -0.7f);
            SurfaceKit.Look(LevelKit.Girder, new Vector3(under.x, (under.y + RoofS3) * 0.5f, under.z), new Vector3(0.25f, under.y - RoofS3, 0.25f));
        }
        LevelKit.HoloSign(root, new Vector3(-125f, RoofS1 + 3f, 163f), Vector3.back, 10f, 1f, "BRIDGE 2 CUT  ·  DO NOT ATTEMPT", true);
        LevelKit.HoloSign(root, new Vector3(-110f, RoofS4 + 3f, 654f), Vector3.back, 12f, 1.1f, "ARCOLOGY 7 AHEAD  ·  RESIDENTS ACCOUNTED FOR");
    }

    /// A storey that fell from a tower and lies on a roof: a dark body with two rows of dead windows on each long face,
    /// one edge light still working, the beams it tore out, rubble round its foot and a cordon someone laid
    static void FallenStorey(Vector3 c, float roofY)
    {
        Quaternion tilt = Quaternion.Euler(0f, 20f, 3f);
        SurfaceKit.Block(CityKit.Hollow, c, new Vector3(7f, 5f, 13f), tilt);
        for (int k = 0; k < 5; k++)
            for (int s = -1; s <= 1; s += 2)
                for (int r = 0; r < 2; r++)
                    SurfaceKit.Look(LevelKit.Dark, c + tilt * new Vector3(s * 3.53f, -1.2f + r * 2.2f, -5f + k * 2.5f), new Vector3(0.06f, 1.4f, 1.6f), tilt);
        SurfaceKit.Look(LevelKit.Led, c + tilt * new Vector3(0f, 2.55f, -6.2f), new Vector3(6.6f, 0.08f, 0.3f), tilt);
        SurfaceKit.Look(LevelKit.Girder, c + tilt * new Vector3(-2.6f, -2.2f, 7.4f), new Vector3(0.4f, 0.4f, 3.5f), tilt);
        SurfaceKit.Look(LevelKit.Girder, c + tilt * new Vector3(2.2f, -1.6f, 7.8f), new Vector3(0.4f, 0.4f, 4.2f), tilt * Quaternion.Euler(12f, 0f, 0f));
        for (int k = 0; k < 7; k++)
        {
            float a = k * 0.9f + 0.3f;
            float rx = c.x + Mathf.Cos(a) * (5.5f + k % 3), rz = c.z + Mathf.Sin(a) * (5.5f + k % 2 * 1.5f);
            SurfaceKit.Look(CityKit.Hollow, new Vector3(rx, roofY + 0.3f + k % 3 * 0.15f, rz), new Vector3(0.6f + k % 3 * 0.4f, 0.5f, 0.8f + k % 2 * 0.5f), Quaternion.Euler(0f, k * 47f, 0f));
        }
        SurfaceKit.Look(LevelKit.CordonBand, c + new Vector3(-8f, -2.0f, -10f), new Vector3(0.06f, 0.2f, 9f), Quaternion.Euler(0f, 20f, 0f));
    }

    // ------------------------------------------------------------ north: the cut line

    /// The fast way: a blade pair with the pad over its slot, then the bridge the response cut with the drones over the
    /// hole in it, two stacks, and the terrace
    static void CutLine()
    {
        BladeTower(FootC1, 40f, BladeC1, 0f);        // low blade z 252 (111.8), high z 268 (130)
        StackTower(FootC3, 44f, 40f, RoofC3, 0f);    // top x ±18, z 503.6 .. 536.4
        StackTower(FootC4, 44f, 40f, RoofC4, 0f);    // z 683.6 .. 716.4
        Parapet(FootC3 + Vector3.up * RoofC3, 35.9f, 32.6f, 0f, 8f);
        LevelKit.JumpPad(root, new Vector3(0f, BladeC1 * 0.86f, 252f), 0f, 34f, 4f);   // 23 m up, the high blade
        BrokenBridge(new Vector3(0f, BladeC1, 272.6f), new Vector3(0f, RoofC3, 503.4f), 30f);
        Bridge(new Vector3(0f, RoofC3, 536.6f), new Vector3(0f, RoofC4, 683.4f));
        Bridge(new Vector3(0f, RoofC4, 716.6f), new Vector3(0f, 150f, 836.1f));
        Beacon(new Vector3(-14f, BladeC1 * 0.86f, 252f));
        Beacon(new Vector3(12f, RoofC3, 512f));
        Beacon(new Vector3(-12f, RoofC4, 708f));
        RoofKit.FastLine(new Vector3(0f, RoofC3, 513f), new Vector3(0f, RoofC3, 527f));   // the through line on each stack, landing to ramp
        RoofKit.FastLine(new Vector3(0f, RoofC4, 693f), new Vector3(0f, RoofC4, 707f));
        LevelKit.ClockFace(root, new Vector3(0f, 130f, 536.85f), Vector3.forward, 6f, 300f, 60f);   // the third stack's north face
        LevelKit.HoloSign(root, new Vector3(0f, BladeC1 + 3f, 274f), Vector3.back, 12f, 1.1f, "LINE 3 CUT  ·  THE DRONES HOLD  ·  DO NOT ATTEMPT", true);
        LevelKit.HoloSign(root, new Vector3(0f, RoofC4 + 3f, 718f), Vector3.back, 12f, 1.1f, "ARCOLOGY 7  ·  TERRACE 2  ·  NORTH");
    }

    // ------------------------------------------------------------ east: the slot

    /// Three twin-blade towers: the low blade, a leap pad to the high one, the slot between as a lane; a round tower
    /// and its halo; the terrace
    static void Slot()
    {
        BladeTower(FootE1, 40f, BladeE1, 0f);   // low z 192 (103.2), high z 208 (120)
        BladeTower(FootE2, 40f, BladeE2, 0f);   // low z 372 (117), high z 388 (136)
        BladeTower(FootE3, 40f, BladeE3, 0f);   // low z 552 (130.7), high z 568 (152)
        RoundTower(FootE4, 40f, RoundE4);       // top drum 150 (dia 28: z 706 .. 734), halo 166
        LevelKit.JumpPad(root, new Vector3(120f, BladeE1 * 0.86f, 192f), 0f, 30f, 4f);
        LevelKit.JumpPad(root, new Vector3(125f, BladeE2 * 0.86f, 372f), 0f, 32f, 4f);
        LevelKit.JumpPad(root, new Vector3(115f, BladeE3 * 0.86f, 552f), 0f, 34f, 4f);
        LevelKit.JumpPad(root, FootE4 + Vector3.up * RoundE4, 0f, 30f, 0f);   // up to the halo
        Bridge(new Vector3(120f, BladeE1, 212.6f), new Vector3(125f, BladeE2 * 0.86f, 367.4f));
        Bridge(new Vector3(125f, BladeE2, 392.6f), new Vector3(115f, BladeE3 * 0.86f, 547.4f));
        Bridge(new Vector3(115f, BladeE3, 572.6f), new Vector3(120f, RoundE4, 706.2f));
        Bridge(new Vector3(120f, RoundE4, 733.8f), new Vector3(60f, 150f, 836.1f));
        Beacon(new Vector3(106f, BladeE1 * 0.86f, 192f));
        Beacon(new Vector3(139f, BladeE2, 388f));
        Beacon(new Vector3(120f, RoundE4, 712f));
        LevelKit.HoloSign(root, new Vector3(120f, BladeE1 * 0.86f + 3f, 196.4f), Vector3.back, 8f, 1f, "TRANSIT  ·  BLADE PAIR 3  ·  LEAP PAD LIVE");
        LevelKit.HoloSign(root, new Vector3(120f, RoundE4 + 3.2f, 736f), Vector3.back, 12f, 1.1f, "ROTUNDA 9  ·  ARCOLOGY 7 AHEAD");
    }

    // ------------------------------------------------------------ the arcology: the second hub

    /// The terraced garden mountain every first way lands on: up the thermals on the west flank or the pads on the
    /// east, the pillar of light at the top, the first look back, and three ways on from the viewing terrace
    static void Arcology()
    {
        ArcologyTower(FootArc, 170f, 112f, ArcH, 0f);   // terrace 2 (150): x ±76.5, z 836.3 .. 923.7; top (300): x ±50, z 863.8 .. 896.2
        Beacon(new Vector3(10f, 150f, 841f));
        Beacon(new Vector3(-70f, 150f, 900f));
        Beacon(new Vector3(70f, 150f, 900f));
        // The thermals: two, up the west flank, one from the second terrace to the fourth, one from there to the top
        Thermal(new Vector3(-72.5f, 0f, 880f), 5f, 150f, 242f);
        Thermal(new Vector3(-59f, 0f, 880f), 4.2f, 237f, 306f);
        // The pads up the east flank, terrace to terrace (face the wall: the push carries you in over the next ledge)
        LevelKit.JumpPad(root, new Vector3(72.5f, 150f, 880f), 270f, 52f, 2f);
        LevelKit.JumpPad(root, new Vector3(65.4f, 198f, 880f), 270f, 47f, 1.6f);
        LevelKit.JumpPad(root, new Vector3(58.9f, 237f, 880f), 270f, 43f, 1.4f);
        LevelKit.JumpPad(root, new Vector3(53f, 270f, 880f), 270f, 41f, 1.2f);
        Beacon(new Vector3(-60f, 237f, 866f));
        Beacon(new Vector3(0f, ArcH, 880f));
        // The fast ways up are the flanks: a hairline along each side ledge to its thermal / its pad (the south ledge is the response's minefield: unmarked)
        RoofKit.FastLine(new Vector3(-72.5f, 150f, 848f), new Vector3(-72.5f, 150f, 866f));
        RoofKit.FastLine(new Vector3(72.5f, 150f, 848f), new Vector3(72.5f, 150f, 866f));
        // The company's bronzes on the second terrace, looking back at the way in; searchlights off the top's south corners
        LevelKit.StatuePlinth(root, new Vector3(-40f, 150.06f, 915f), 180f, 1f);
        LevelKit.StatuePlinth(root, new Vector3(40f, 150.06f, 915f), 180f, 1f);
        RoofKit.Searchlight(FootArc, 100.4f, 32.3f, ArcH, 0f, 0, 0.3f);
        RoofKit.Searchlight(FootArc, 100.4f, 32.3f, ArcH, 0f, 1, 0.8f);
        LevelKit.HoloSign(root, new Vector3(-10f, 153.4f, 846f), Vector3.back, 12f, 1.2f, "ARCOLOGY 7  ·  ALL RESIDENTS ACCOUNTED FOR");
        LevelKit.HoloSign(root, new Vector3(0f, ArcH + 3f, 894f), Vector3.back, 9f, 1f, "VIEWING TERRACE  ·  THE HOUSE  ·  NORTH");
        LevelKit.HoloSign(root, new Vector3(-40f, ArcH + 2.4f, 892f), Vector3.back, 9f, 1f, "THE HALO  ·  WEST");
        LevelKit.HoloSign(root, new Vector3(40f, ArcH + 2.4f, 892f), Vector3.back, 9f, 1f, "THE GARDENS  ·  EAST");
    }

    /// The three ways on from the arcology's top, laid once the roofs they land on stand
    static void ArcologyWays()
    {
        Bridge(new Vector3(-50f, ArcH, 890f), new Vector3(-110f, RoundI, 1104.4f));   // west: the company's skyway down to the first round tower
        Bridge(new Vector3(0f, ArcH, 896.4f), DeckA);                                   // north: onto the line
        Bridge(new Vector3(50f, ArcH, 890f), new Vector3(120f, RoofG1, 1028.5f));     // east: the gardens
    }

    // ------------------------------------------------------------ west: the halo

    /// Two round towers wearing their halo decks, a thermal up the second, a stack on the way down to the spire
    static void Halo()
    {
        RoundTower(FootI, 40f, RoundI);   // top drum 240 (dia 28: z 1106 .. 1134), halo 256
        RoundTower(FootJ, 40f, RoundJ);   // lower drum 216 (dia 40: z 1310 .. 1350), top 300 (dia 28), halo 316
        StackTower(FootK, 44f, 40f, RoofK, 0f);   // top x -128 .. -92, z 1543.6 .. 1576.4
        Parapet(FootK + Vector3.up * RoofK, 35.9f, 32.6f, 0f, 8f);
        LevelKit.JumpPad(root, FootI + Vector3.up * RoundI, 0f, 30f, 0f);   // up to the halo that touches nothing
        Bridge(new Vector3(-110f, RoundI, 1134.2f), new Vector3(-120f, RoundJ * 0.72f, 1309.8f));
        Thermal(new Vector3(-143f, 0f, 1330f), 4f, RoundJ * 0.72f, RoundJ + 6f);
        Bridge(new Vector3(-120f, RoundJ, 1344f), new Vector3(-110f, RoofK, 1543.4f));
        Bridge(new Vector3(-110f, RoofK, 1576.6f), new Vector3(-11f, SpireH, 1796f));   // onto the spire's top
        Beacon(new Vector3(-110f, RoundI, 1112f));
        Beacon(new Vector3(-134f, RoundJ * 0.72f, 1318f));
        Beacon(new Vector3(-120f, RoundJ, 1336f));
        Beacon(new Vector3(-124f, RoofK, 1560f));
        LevelKit.HoloSign(root, new Vector3(-120f, RoundJ * 0.72f + 3f, 1312f), Vector3.back, 10f, 1f, "ROTUNDA 12  ·  THERMAL WEST  ·  SKY LINE 4 EAST");
    }

    // ------------------------------------------------------------ north: the line

    /// A tram line in the sky from the arcology's top to the spire: two trams out of step, the strip between them,
    /// the guns on the deck, and a station under it halfway you can drop to and pad back up from. Ride a roof, or run it
    static void Line()
    {
        Viaduct(DeckA, DeckB, 12f);
        Vector3 da = RoofKit.LastDeckA, db = RoofKit.LastDeckB;   // where the deck actually runs: bearing on the roofs it joins
        Vector3 dir = (db - da).normalized;
        Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 a = da + side * (s * 3f) + dir * 8f + Vector3.up * 0.02f, b = db + side * (s * 3f) - dir * 8f + Vector3.up * 0.02f;   // on the deck, not in it
            LevelKit.Rail(root, a, b);
            if (CityVista.Preview) continue;
            Tram.Create(root, cfg, a + dir * 2f, b - dir * 2f, s < 0 ? 0f : 0.5f);
        }
        SurfaceKit.Look(LevelKit.Led, (da + db) * 0.5f + Vector3.up * 0.03f, new Vector3(0.3f, 0.05f, Vector3.Distance(da, db) - 16f), SurfaceKit.SpanRot(da, db));   // the strip
        Beacon(da + dir * 12f - side * 4.8f);
        LevelKit.HoloSign(root, da + dir * 6f + Vector3.up * 3.4f, -dir, 11f, 1.1f, "SKY LINE 4  ·  SERVICE IS NORMAL  ·  MIND THE TRAMS");
        // The station: a stack whose roof runs five metres under the deck halfway along; drop to it, pad back up
        StackTower(FootStation, 44f, 40f, RoofStation, 0f);   // top x ±18, z 1333.6 .. 1366.4; the deck passes at 273.3
        Parapet(FootStation + Vector3.up * RoofStation, 35.9f, 32.6f, 0f);
        LevelKit.JumpPad(root, new Vector3(0f, RoofStation, 1350f), 0f, 16f, 2f);   // 5 m up, back onto the deck
        // The skill line back up (course-grammar): a kick wall at each side of the roof and a brass lip inside it — kick
        // the wall, turn in, corner-boost the lip within two seconds and the WALL COMBO carries you over the deck's rail
        // (6.4 m up: the timed line only; the pad is the main line)
        for (int s = -1; s <= 1; s += 2)
        {
            RoofKit.KickWall(new Vector3(s * 15.5f, RoofStation, 1350f), new Vector3(0.5f, 4f, 10f));
            RoofKit.Lip(new Vector3(s * 9.5f, RoofStation, 1350f), new Vector3(1.2f, 1.4f, 4f));
        }
        Beacon(new Vector3(-12f, RoofStation, 1340f));
        LevelKit.HoloSign(root, new Vector3(0f, RoofStation + 3f, 1336f), Vector3.back, 10f, 1f, "HALT 4  ·  SKY LINE 4 OVERHEAD");
        // The last bridge: off the deck onto the spire's top tier
        Bridge(DeckB + Vector3.forward * 0.1f, new Vector3(0f, SpireH, 1789.6f));
    }

    // ------------------------------------------------------------ east: the gardens

    /// A roof garden, a tower split by a sky lobby, a round tower and its halo, and the long way down to the spire
    static void Gardens()
    {
        StackTower(FootG1, 60f, 52f, RoofG1, 0f);   // top 49.2 x 42.6: x 95.4 .. 144.6, z 1028.7 .. 1071.3
        RoofKit.Garden(FootG1 + Vector3.up * RoofG1, 48f, 41f, 0f);
        RoofKit.SplitTower(FootG2, 56f, 48f, LobbyG2, 9f, 60f, 0f);   // lobby 275: x 107 .. 153, z 1280.3 .. 1319.7
        RoundTower(FootG3, 40f, RoundG3);   // top drum 262 (dia 28: z 1526 .. 1554), halo 278
        LevelKit.JumpPad(root, FootG3 + Vector3.up * RoundG3, 0f, 30f, 0f);
        Bridge(new Vector3(120f, RoofG1, 1071.5f), new Vector3(130f, LobbyG2, 1280.1f));
        Bridge(new Vector3(130f, LobbyG2, 1319.9f), new Vector3(115f, RoundG3, 1526.2f));
        Bridge(new Vector3(115f, RoundG3, 1553.8f), new Vector3(11f, SpireH, 1796f));   // onto the spire's top
        Beacon(new Vector3(140f, RoofG1, 1036f));
        Beacon(new Vector3(118f, LobbyG2, 1286f));
        Beacon(new Vector3(115f, RoundG3, 1532f));
        LevelKit.HoloSign(root, new Vector3(120f, RoofG1 + 3.4f, 1031f), Vector3.back, 10f, 1f, "THE GARDENS  ·  ROOF GARDEN 3  ·  KEEP OFF THE GRASS");
        LevelKit.HoloSign(root, new Vector3(130f, LobbyG2 + 3.4f, 1279.4f), Vector3.back, 10f, 1f, "GALLERY 31  ·  SKY LOBBY  ·  RESIDENTS ONLY");
    }

    // ------------------------------------------------------------ the spire: the last hub

    /// The finish: the top tier of a spire, every way converging on it, the needle over it, the door facing the house
    static void Spire()
    {
        MenuCity.OuterTower(SurfaceKit.Far, sheets, FootS, 60f, 50f, SpireH, 0f, 1, true);
        Furnish();
        float[] part = { 0.38f, 0.24f, 0.17f, 0.12f, 0.09f };
        float y = 0f, cw = 60f, cd = 50f;
        for (int i = 0; i < part.Length; i++)
        {
            float th = SpireH * part[i];
            SurfaceKit.Solid(FootS + Vector3.up * (y + th * 0.5f), new Vector3(cw, th, cd));
            y += th;
            Roof(FootS + Vector3.up * y, cw - 0.2f, cd - 0.2f, 0f);
            cw *= 0.8f;
            cd *= 0.8f;
        }
        SurfaceKit.Solid(FootS + Vector3.up * (SpireH + 20f), new Vector3(1.1f, 40f, 1.1f));   // the needle
        LevelKit.RadioMast(root, FootS + Vector3.up * (SpireH + 40f), 12f);                        // and the mast on it, red lamps for the aircraft
        LevelKit.ClockFace(root, new Vector3(0f, 128f, 1779.55f), Vector3.back, 8f, 300f, 60f);   // the second tier's south face: the time, from anywhere on the route
        RoofKit.Searchlight(FootS, 24.6f, 20.5f, SpireH, 0f, 2, 0.4f);                              // off the top's north corners, behind the door
        RoofKit.Searchlight(FootS, 24.6f, 20.5f, SpireH, 0f, 3, 0.9f);
        RoofKit.Door(new Vector3(0f, SpireH, 1808f));   // the door is as wide as the lane (six metres), its posts edge-boost corners
        Beacon(new Vector3(8f, SpireH, 1794f));
        LevelKit.HoloSign(root, new Vector3(0f, SpireH + 5.2f, 1809f), Vector3.back, 12f, 1.3f, "HALCYON  ·  YOU ARE EXPECTED");
        LevelKit.Floodlight(root, new Vector3(-9f, SpireH + 6f, 1806f), new Vector3(0.5f, -0.7f, -0.5f), 30f, 2.6f);
        LevelKit.Floodlight(root, new Vector3(9f, SpireH + 6f, 1806f), new Vector3(-0.5f, -0.7f, -0.5f), 30f, 2.6f);
    }
}
