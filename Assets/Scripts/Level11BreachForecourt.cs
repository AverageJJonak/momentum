using UnityEngine;

/// <summary>
/// THE FORECOURT and THE STREET: Level 11's first two hundred and fifty metres. The terminus's forecourt as the lift
/// left it, and the street north of it over the tunnel, subsided and venting. (Level11Builder's partials: the
/// forecourt, the street, the viaduct, the cut, the tower and the house, the city beyond, the units.)
/// </summary>
public static partial class Level11Builder
{
    // ------------------------------------------------------------ the forecourt

    /// Where Chapter 2 left you: the forecourt behind the House, the morning after. Lift 3's head in the middle with
    /// the shaft open under it and the steam still standing over it (an updraft: ride it); the canopy down on its north
    /// columns, a chute now, its fascia a lip at the foot; the lift car flung on its side; the kiosk, the queue posts
    /// and the luggage where they landed; the clock tower down across the street outside the gate, a lip across the
    /// line; the police's trestle and notice. Four ways out: round the canopy either side on foot, down it at speed,
    /// or up the steam and onto it
    static void Forecourt()
    {
        const float yx = 36f, yz0 = -30f, yz1 = 34f;
        Plate(-yx, Head.x - HeadHalf, yz0, yz1);
        Plate(Head.x + HeadHalf, yx, yz0, yz1);
        Plate(Head.x - HeadHalf, Head.x + HeadHalf, yz0, Head.z - HeadHalf);
        Plate(Head.x - HeadHalf, Head.x + HeadHalf, Head.z + HeadHalf, yz1);
        Wall(new Vector3(-yx, 0f, 2f), new Vector3(1f, 2.6f, 64f));
        Wall(new Vector3(yx, 0f, 2f), new Vector3(1f, 2.6f, 64f));
        Wall(new Vector3(-22.5f, 0f, yz0), new Vector3(27f, 2.6f, 1f));     // the south gate between them: the way back to the menu
        Wall(new Vector3(22.5f, 0f, yz0), new Vector3(27f, 2.6f, 1f));
        Wall(new Vector3(-25.25f, 0f, yz1), new Vector3(21.5f, 2.6f, 1f));   // and the north gate, 29 m of it: the canopy's foot lies in it
        Wall(new Vector3(25.25f, 0f, yz1), new Vector3(21.5f, 2.6f, 1f));
        LevelKit.HoloSign(root, new Vector3(17f, 3.2f, yz0 + 1.2f), Vector3.forward, 9f, 1.6f, "HALCYON METRO  ·  TERMINUS  ·  LIFT 3", true);

        LiftHead();
        Canopy();
        // The lift car, thrown clear: on its side east of the head, a box under its brass
        Quaternion fell = Quaternion.Euler(0f, 40f, 90f);
        Vector3 carAt = new Vector3(14f, 1.45f, 24f);
        var car = EndingProps.Cage(root, carAt, 0f);
        car.rotation = fell;
        SurfaceKit.Solid(carAt + fell * new Vector3(0f, 1.5f, 0f), new Vector3(3.1f, 3.1f, 3.1f), fell);
        // The winding house, flat on the paving east of the head, its cables still running down the shaft
        SurfaceKit.Block(LevelKit.RivetIron, Head + new Vector3(14f, 1.2f, 4f), new Vector3(6f, 2.4f, 4f), Quaternion.Euler(0f, 25f, 0f));
        for (int i = 0; i < 3; i++) LevelKit.Cable(root, Head + new Vector3(11f, 2.4f, 2f + i * 1.6f), Head + new Vector3(HeadHalf - 1f, HeadFloor + 1f, -2f + i * 2f), 0.1f);

        LevelKit.Kiosk(root, new Vector3(-22f, 0f, -8f), 160f);
        LevelKit.Queue(root, new Vector3(-6f, 0f, -4f), Vector3.right, 4, 3f);
        for (int i = 0; i < 4; i++) LevelKit.Bollard(root, new Vector3(-9f + i * 6f, 0f, -26f));
        LevelKit.CrateStack(root, new Vector3(-24f, 0f, -14f), 15f);
        LevelKit.CrateStack(root, new Vector3(26f, 0f, -16f), -20f);
        LevelKit.Cart(root, new Vector3(22f, 0f, -4f), 90f);
        LevelKit.StationBench(root, new Vector3(-24f, 0f, 20f), 0f);
        LevelKit.Smokestack(root, new Vector3(-30f, 0f, 26f), 36f, 3.2f);
        SurfaceKit.Solid(new Vector3(-30f, 18f, 26f), new Vector3(3f, 36f, 3f));
        LevelKit.Floodlight(root, new Vector3(-18f, 8f, -8f), new Vector3(0.7f, -0.6f, 0.5f), 30f, 2.4f);
        LevelKit.Floodlight(root, new Vector3(18f, 8f, -8f), new Vector3(-0.7f, -0.6f, 0.5f), 30f, 2.4f);
        Beacon(new Vector3(0f, 0f, -20f));

        // The paving the burst threw up, and the steam still coming out of the cracks it left
        Heap(new Vector3(-17f, 0f, 2f), 5f, 5);
        Heap(new Vector3(24f, 0f, 14f), 4f, 4);
        Heap(new Vector3(-10f, 0f, 30f), 4f, 4);
        for (int i = 0; i < 5; i++)
        {
            float a = (-70f + i * 35f) * Mathf.Deg2Rad, len = Random.Range(14f, 26f);
            Vector3 dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            Vector3 mid = Head + dir * (HeadHalf + 2f + len * 0.5f);
            SurfaceKit.Look(EndingProps.Black, mid + Vector3.up * 0.03f, new Vector3(Random.Range(0.5f, 0.9f), 0.06f, len), Quaternion.LookRotation(dir));
            LevelKit.SteamLeak(root, Head + dir * (HeadHalf + 2f + len) + Vector3.up * 0.1f, 2.5f);
        }

        // The forecourt's clock tower, down across the street outside the north gate: thirty-six metres of brick lying
        // from its foot by the gate to its crown in the street, a lip across the line (brass along its top), the crown
        // in pieces where it hit. The street's west half runs clear past its top: nothing is closed
        Vector3 foot = new Vector3(26f, 0f, 40f), fallDir = new Vector3(-27.6f, 0f, 23.1f).normalized;
        const float towerH = 36f, towerDia = 1.8f;
        var tower = EndingProps.Stack(root, foot + Vector3.up * (towerDia * 0.5f), towerH, towerDia);
        tower.rotation = Quaternion.FromToRotation(Vector3.up, fallDir);
        Vector3 top = foot + fallDir * towerH;
        SolidBetween(foot + Vector3.up * (towerDia * 0.5f), top + Vector3.up * (towerDia * 0.5f), towerDia, towerDia);
        SurfaceKit.Look(LevelKit.Brass, (foot + top) * 0.5f + Vector3.up * (towerDia + 0.015f), new Vector3(0.14f, 0.024f, towerH - 0.2f), Quaternion.LookRotation(fallDir));
        SurfaceKit.Block(LevelKit.Limestone, foot + Vector3.up * 0.6f, new Vector3(3.2f, 1.2f, 3.2f));   // its pedestal, still standing
        Heap(top + fallDir * 3f, 4f, 5);
        LevelKit.SteamLeak(root, foot + new Vector3(-2f, 0.1f, 2f), 2f);

        // The police's trestle across the street, and their notice over it: where out is, and what it is waiting for
        LevelKit.Trestle(root, new Vector3(-1f, 0f, 56f), 8f, 0f);
        Vector3 board = new Vector3(0f, 0f, 72f);
        SurfaceKit.Block(LevelKit.Charcoal, board + new Vector3(-7.6f, 3.4f, 0f), new Vector3(0.35f, 6.8f, 0.35f));
        SurfaceKit.Block(LevelKit.Charcoal, board + new Vector3(7.6f, 3.4f, 0f), new Vector3(0.35f, 6.8f, 0.35f));
        LevelKit.HoloSign(root, board + new Vector3(0f, 5.9f, 0f), Vector3.back, 14.6f, 1.5f, "RAILWAY INCIDENT  ·  CORDON  ·  BY ORDER OF THE HOUSE", true);
        LevelKit.HoloSign(root, board + new Vector3(0f, 4.4f, 0f), Vector3.back, 14.6f, 1.1f, "GUESTS TO THE RELIEF STACK, RAILWAY HOUSE  ·  NORTH");
        LevelKit.HoloSign(root, board + new Vector3(0f, 3.2f, 0f), Vector3.back, 14.6f, 1.1f, "STACK ON RESIDUAL PRESSURE  ·  THREE RELIEF VALVES OPEN");
        LevelKit.HoloSign(root, board + new Vector3(0f, 2f, 0f), Vector3.back, 14.6f, 1.1f, "THE VIADUCT  ·  THE PLATFORM  ·  THE CLOCK TOWER");
    }

    /// Lift 3's head: the shaft open under the forecourt, tiled, the brass guide rails still in its corners, the car's
    /// wreckage fourteen metres down for a floor (a pad on it brings you back up), a depth dial whose needle follows
    /// you, the brass edge round the opening with its red lamps, and the steam column standing over it: an updraft that
    /// carries you to sixteen metres — the top of the canopy's chute is a drift away
    static void LiftHead()
    {
        float wallY = (HeadFloor - 1f) * 0.5f, depth = 1f - HeadFloor, span = HeadHalf * 2f + 2f;
        SurfaceKit.Block(LevelKit.Tile, Head + new Vector3(-HeadHalf - 0.5f, wallY, 0f), new Vector3(1f, depth, span));
        SurfaceKit.Block(LevelKit.Tile, Head + new Vector3(HeadHalf + 0.5f, wallY, 0f), new Vector3(1f, depth, span));
        SurfaceKit.Block(LevelKit.Tile, Head + new Vector3(0f, wallY, -HeadHalf - 0.5f), new Vector3(HeadHalf * 2f, depth, 1f));
        SurfaceKit.Block(LevelKit.Tile, Head + new Vector3(0f, wallY, HeadHalf + 0.5f), new Vector3(HeadHalf * 2f, depth, 1f));
        LevelKit.FloorSlab(root, Head + Vector3.up * (HeadFloor - 0.5f), new Vector3(HeadHalf * 2f, 1f, HeadHalf * 2f), "LiftFloor");
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                LevelKit.Prim(root, Head + new Vector3(sx * (HeadHalf - 0.25f), HeadFloor * 0.5f, sz * (HeadHalf - 0.25f)), new Vector3(0.22f, -HeadFloor, 0.22f), LevelKit.Brass, "GuideRail");
                Vector3 corner = Head + new Vector3(sx * (HeadHalf + 0.9f), 0f, sz * (HeadHalf + 0.9f));
                SurfaceKit.Look(LevelKit.Brass, corner + Vector3.up * 0.45f, new Vector3(0.08f, 0.45f, 0.08f), null, PrimitiveType.Cylinder);
                SurfaceKit.Look(LevelKit.SignalRed, corner + Vector3.up * 1f, Vector3.one * 0.3f, null, PrimitiveType.Sphere);
            }
        SurfaceKit.Block(LevelKit.RivetIron, Head + new Vector3(3f, HeadFloor + 0.6f, -2f), new Vector3(2.2f, 1.2f, 1.6f), Quaternion.Euler(0f, 30f, 12f));   // the counterweight
        SurfaceKit.Block(LevelKit.Brass, Head + new Vector3(-3.5f, HeadFloor + 0.3f, 3f), new Vector3(2.9f, 0.6f, 0.4f), Quaternion.Euler(0f, -20f, 0f));    // the car's roof rim
        LevelKit.JumpPad(root, Head + Vector3.up * HeadFloor, 0f, 30f, 4f);
        LevelKit.Mosaic(root, Head + new Vector3(0f, -3.2f, HeadHalf - 0.05f), Vector3.back, "LIFT 3  ·  STREET LEVEL", 7f, 1.1f);
        LevelKit.WallGlobe(root, Head + new Vector3(-HeadHalf + 0.05f, -2f, 0f), Vector3.right);
        LevelKit.WallGlobe(root, Head + new Vector3(HeadHalf - 0.05f, -8f, 0f), Vector3.left);
        LevelKit.DepthDial(root, Head + new Vector3(HeadHalf - 0.05f, -2.4f, -4f), Vector3.left, 0f, HeadFloor, null, 1.4f);
        float e = HeadHalf + 0.5f;
        LevelKit.BrassStrip(root, Head + new Vector3(-e, 0f, -e), Head + new Vector3(e, 0f, -e));
        LevelKit.BrassStrip(root, Head + new Vector3(-e, 0f, e), Head + new Vector3(e, 0f, e));
        LevelKit.BrassStrip(root, Head + new Vector3(-e, 0f, -e), Head + new Vector3(-e, 0f, e));
        LevelKit.BrassStrip(root, Head + new Vector3(e, 0f, -e), Head + new Vector3(e, 0f, e));
        EndingProps.SmokeColumn.Spawn(root, Head + Vector3.up * 1f, 7f, 150f, 18, 0.08f, EndingProps.Steam);
        LevelKit.SteamColumn(root, Head, 7f, HeadFloor + 1f, 16f);
        district.updrafts.Add(new District.Updraft { centre = Head, half = 7f, y0 = HeadFloor + 2f, y1 = 16f });
    }

    /// The canopy: its south columns still standing with nothing on them, its north columns snapped at nine metres and
    /// the canopy lying on them, sloping down to the ground in the north gate — a chute at twenty-five degrees out of the
    /// forecourt, with HALCYON METRO on the fascia at its foot, which is a lip: slide it, boost off it, and the street is
    /// yours at speed. Going in on foot you go round it: it is twenty-six metres of a seventy-two metre forecourt
    static void Canopy()
    {
        Vector3 crest = new Vector3(0f, 9f, 19f), foot = new Vector3(0f, 0.4f, 37f);
        for (int s = -1; s <= 1; s += 2)
        {
            LevelKit.Column(root, new Vector3(s * 11.5f, 0f, 1f), 12f, 1.2f);
            LevelKit.Column(root, new Vector3(s * 11.5f, 0f, 20f), 8.7f, 1.2f);
        }
        SurfaceKit.SpanSlab(root, crest, foot, 26f, 0.6f, LevelKit.Cream, "Canopy");
        Quaternion rot = SurfaceKit.SpanRot(crest, foot);
        SurfaceKit.Look(LevelKit.Charcoal, SurfaceKit.OnSpan(crest, foot, 0.5f, 0f, -0.86f), new Vector3(22f, 0.5f, 15f), rot);   // its stepped underside, seen from the hole (a centimetre off the slab: never two faces on one plane)
        SurfaceKit.Look(LevelKit.Brass, SurfaceKit.OnSpan(crest, foot, 0.5f, 0f, -1.3f), new Vector3(17f, 0.4f, 11f), rot);
        for (int s = -1; s <= 1; s += 2)
            SurfaceKit.Look(LevelKit.Brass, SurfaceKit.OnSpan(crest, foot, 0.5f, s * 12.6f, 0.012f), new Vector3(0.14f, 0.024f, 19.6f), rot);   // the edges, in brass: a slide's rails
        // The fascia at the foot, a lip across the chute's exit: the sign, the clock, the brass along its top
        Vector3 fascia = new Vector3(0f, 1.1f, 37.3f);
        SurfaceKit.Block(LevelKit.Oxblood, fascia, new Vector3(26f, 1.4f, 0.3f));
        SurfaceKit.Look(LevelKit.Brass, fascia + Vector3.up * (0.7f + 0.015f), new Vector3(25.9f, 0.024f, 0.14f));
        LevelKit.Text(root, fascia + new Vector3(-3f, 0f, 0.17f), Quaternion.LookRotation(Vector3.back), "HALCYON METRO", 0.4f, LevelKit.SignCream, "FasciaText", 16f, true, 400f, 900f);
        LevelKit.ClockFace(root, fascia + new Vector3(10.5f, 0f, 0.17f), Vector3.forward, 1.2f, 290f, 50f);
        TechMarkers.MarkLip(fascia + Vector3.up * 0.7f, new Vector3(26.6f, 0.9f, 1.5f));
        LevelKit.HoloSign(root, new Vector3(-20f, 3.4f, 36f), Vector3.back, 8f, 1f, "CANOPY UNSAFE  ·  KEEP OFF", true);
    }

    // ------------------------------------------------------------ the street

    /// From the forecourt to the elevated line: the street over the tunnel, subsided. Cracks out of the lift head
    /// with the steam coming out of them, the blocks that stood on the tunnel's line down to stubs, the paving in
    /// heaps, a street you can still read up the middle with the mains' vents in it (timed launches: the burst is a
    /// pad), the lamp posts still lit, and a sheet of paving propped on a kerb: a kicker, for whoever comes down the
    /// street fast enough to want one. HARD LINE up the middle, vent to vent to the break
    static void Street()
    {
        float[] bearing = { -58f, -34f, -13f, 17f, 41f, 63f };
        for (int i = 0; i < bearing.Length; i++)
        {
            float a = bearing[i] * Mathf.Deg2Rad, len = Random.Range(60f, 130f), w = Random.Range(1.2f, 2.2f);
            Vector3 dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            Vector3 mid = Head + dir * (46f + len * 0.5f);
            SurfaceKit.Look(EndingProps.Black, mid + Vector3.up * 0.03f, new Vector3(w, 0.06f, len), Quaternion.LookRotation(dir));
            LevelKit.SteamLeak(root, Head + dir * (46f + len) + Vector3.up * 0.1f, 3.5f);
            LevelKit.SteamLeak(root, mid + Vector3.up * 0.1f, 2.5f);
            var l = LevelKit.Light(root, mid + Vector3.up * 2f, 14f, 1.2f);
            l.color = new Color(1f, 0.95f, 0.85f);
        }

        // What stood over the tunnel: stubs you can get on top of, and heaps you go round or over. The street stays clear.
        Stub(new Vector3(-150f, 0f, 118f), new Vector3(40f, 16f, 34f));
        Stub(new Vector3(-72f, 0f, 168f), new Vector3(26f, 9f, 30f));
        Stub(new Vector3(92f, 0f, 112f), new Vector3(36f, 18f, 30f));
        Stub(new Vector3(178f, 0f, 176f), new Vector3(44f, 26f, 38f));
        Stub(new Vector3(-214f, 0f, 46f), new Vector3(38f, 12f, 40f));
        Stub(new Vector3(214f, 0f, 30f), new Vector3(34f, 22f, 36f));
        for (int i = 0; i < 46; i++)
        {
            Vector3 at = new Vector3(Random.Range(X0 + 30f, X1 - 30f), 0f, Random.Range(44f, 205f));
            if (Mathf.Abs(at.x) < 11f) continue;                                   // the street
            if (at.x > -110f && at.x < -14f && at.z > 190f) continue;              // the foot of the fallen span
            if (Mathf.Abs(at.x - 106f) < 24f && at.z > 186f) continue;             // the pad up to the east line
            if (Mathf.Abs(at.x - 24f) < 16f && Mathf.Abs(at.z - 157f) < 22f) continue; // the kicker
            if (at.x > -8f && at.x < 34f && at.z < 80f) continue;                 // the clock tower and the notice
            Heap(at, Random.Range(5f, 11f), Random.Range(3, 7));
        }
        SurfaceKit.SpanSlab(root, new Vector3(24f, 0.05f, 150f), new Vector3(24f, 4.6f, 164f), 8f, 0.8f, LevelKit.Slope, "Kicker");
        SurfaceKit.Block(LevelKit.Limestone, new Vector3(24f, 1.8f, 163.2f), new Vector3(6f, 3.6f, 1.4f));
        LevelKit.SteamVentPad(root, new Vector3(0f, 0f, 112f), 6f, 0f);
        LevelKit.SteamVentPad(root, new Vector3(0f, 0f, 186f), 6f, 2.5f);
        LevelKit.LineRun(root, new Vector3(0f, 0f, 46f), new Vector3(0f, 0f, 110.6f), Zone.Surface);
        LevelKit.LineRun(root, new Vector3(0f, 0f, 113.4f), new Vector3(0f, 0f, 184.6f), Zone.Surface);
        LevelKit.LineRun(root, new Vector3(0f, 0f, 187.4f), new Vector3(0f, 0f, 244f), Zone.Surface);
        for (int i = 0; i < 3; i++)
            for (int s = -1; s <= 1; s += 2) LampPost(new Vector3(s * 9.5f, 0f, 90f + i * 60f));
        EndingProps.SmokeColumn.Spawn(root, new Vector3(70f, 2f, 150f), 6f, 110f, 14, 0.08f, EndingProps.Steam);
        Beacon(new Vector3(-5f, 0f, 84f));
    }

    /// A street lamp: a charcoal post with a brass collar and a cream globe, lit (looks and a light)
    static void LampPost(Vector3 at)
    {
        SurfaceKit.Look(LevelKit.Charcoal, at + Vector3.up * 2.3f, new Vector3(0.24f, 2.3f, 0.24f), null, PrimitiveType.Cylinder);
        SurfaceKit.Look(LevelKit.Limestone, at + Vector3.up * 0.2f, new Vector3(0.7f, 0.4f, 0.7f));
        SurfaceKit.Look(LevelKit.Brass, at + Vector3.up * 4.55f, new Vector3(0.4f, 0.05f, 0.4f), null, PrimitiveType.Cylinder);
        SurfaceKit.Look(LevelKit.LampCream, at + Vector3.up * 4.9f, Vector3.one * 0.55f, null, PrimitiveType.Sphere);
        var l = LevelKit.Light(root, at + Vector3.up * 4.6f, 13f, 1.5f);
        l.color = new Color(1f, 0.9f, 0.7f);
    }
}
