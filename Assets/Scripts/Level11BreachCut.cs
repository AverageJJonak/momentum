using UnityEngine;

/// <summary>
/// THE CUT: the metro's cut under the street, collapsed — Level 11's middle. (Level11Builder partial)
/// </summary>
public static partial class Level11Builder
{
    /// Where the metro ran under the street: a hundred and forty metres by a hundred and seventy of the street gone,
    /// the tunnel's floor twenty-two metres down with the torn track across it, the last train in on its side down
    /// there and burning, the station's tile on the walls, and the platform's vault still standing in the middle — an
    /// island, with the valve on it. Four ways on: the paving over the south rim (it gives way: a line you cannot stop
    /// on) onto the torn track bed; the burst main's steam columns from the west (you fly them); the track bed from the
    /// north under a signal gantry, past the carriage hanging over the rim; and the snipers over the east half. The
    /// floor is a floor: pads on it bring you back up, and the main climbs out of its north-east corner as a ramp
    static void TheCut()
    {
        Pit(Cut, CutHalfX, CutHalfZ, CutFloor, 40f);
        float rimS = Cut.z - CutHalfZ, rimN = Cut.z + CutHalfZ, rimW = Cut.x - CutHalfX, rimE = Cut.x + CutHalfX;
        EndingProps.SmokeColumn.Spawn(root, Cut + new Vector3(20f, CutFloor + 4f, -24f), 18f, 230f, 18, 0.07f, EndingProps.Steam);

        // THE FLOOR: the track bed down the middle, the third rail dead under its board, the tile where the vault came
        // down, the last train in on its side and burning, the pads up (near the walls: fly up past the rim and drift)
        LevelKit.Rail(root, new Vector3(Cut.x, CutFloor, rimS + 4f), new Vector3(Cut.x, CutFloor, rimN - 4f));
        SurfaceKit.Look(LevelKit.Oxblood, new Vector3(Cut.x + 1.7f, CutFloor + 0.32f, Cut.z), new Vector3(0.3f, 0.2f, CutHalfZ * 2f - 8f));
        LevelKit.HoloSign(root, new Vector3(Cut.x + 4f, CutFloor + 2.2f, Cut.z - 60f), Vector3.back, 7f, 0.9f, "THIRD RAIL  ·  DEAD  ·  SECTION ISOLATED", true);
        for (int i = 0; i < 26; i++)
        {
            Vector3 at = Cut + new Vector3(Random.Range(-CutHalfX, CutHalfX) * 0.85f, 0f, Random.Range(-CutHalfZ, CutHalfZ) * 0.85f);
            if (Mathf.Abs(at.x - Cut.x) < 6f) continue;                                                                   // the track bed stays clear
            if (Mathf.Abs(at.x - Cut.x) < IslandHalf + 12f && Mathf.Abs(at.z - Cut.z) < IslandHalf + 4f) continue;          // the island's foot and haunches
            if (at.x > -4f && at.x < 4f && at.z > Cut.z) continue;                                                        // the main's way out
            float s = Random.Range(2f, 7f);
            SurfaceKit.Block(Rubble(i), new Vector3(at.x, CutFloor + s * 0.2f, at.z), new Vector3(s * Random.Range(0.8f, 1.8f), s * 0.4f, s * Random.Range(0.8f, 1.8f)), Quaternion.Euler(Random.Range(-20f, 20f), Random.Range(0f, 360f), Random.Range(-20f, 20f)));
        }
        Carriage(new Vector3(Cut.x + 24f, CutFloor + 1.5f, Cut.z + 40f), Quaternion.Euler(0f, 20f, 75f));
        EndingProps.Flames.Spawn(root, new Vector3(Cut.x + 24f, CutFloor + 0.2f, Cut.z + 31f), 3.5f);
        EndingProps.SmokeColumn.Spawn(root, new Vector3(Cut.x + 24f, CutFloor + 3f, Cut.z + 31f), 5f, 110f, 14, 0.08f);
        LevelKit.JumpPad(root, new Vector3(Cut.x, CutFloor, rimS + 8f), 0f, 44f, 0f);
        LevelKit.JumpPad(root, new Vector3(rimE - 10f, CutFloor, Cut.z), 0f, 44f, 0f);
        LevelKit.JumpPad(root, new Vector3(Cut.x, CutFloor, rimN - 8f), 0f, 44f, 0f);

        // THE ISLAND: the platform's vault, still standing on its pier — tile over rock — and the valve on it; its
        // haunches either side are slopes off it (the west one lands you in a steam column), the lever frame a lip
        Vector3 isle = new Vector3(Cut.x, IslandTop, Cut.z);
        float isleDepth = IslandTop - CutFloor + 2f;
        SurfaceKit.Block(LevelKit.Rock, new Vector3(isle.x, IslandTop - 1f - isleDepth * 0.5f, isle.z), new Vector3(IslandHalf * 2f, isleDepth, IslandHalf * 2f));
        SurfaceKit.Look(LevelKit.Tile, new Vector3(isle.x, IslandTop - 5f, isle.z), new Vector3(IslandHalf * 2f + 0.14f, 8f, IslandHalf * 2f + 0.14f));
        LevelKit.Slab(root, new Vector3(isle.x, IslandTop - 0.5f, isle.z), new Vector3(IslandHalf * 2f, 1f, IslandHalf * 2f), LevelKit.Tile, "Island");
        for (int s = -1; s <= 1; s += 2)
        {
            SurfaceKit.SpanSlab(root, new Vector3(isle.x + s * (IslandHalf - 0.5f), IslandTop, isle.z), new Vector3(isle.x + s * (IslandHalf + 9f), IslandTop - 6f, isle.z), 14f, 1f, LevelKit.Tile, "VaultHaunch");
            Pillar(new Vector3(isle.x + s * (IslandHalf + 8f), IslandTop - 6.5f, isle.z), 4f);
        }
        LevelKit.ReliefValve(root, district, new Vector3(isle.x, IslandTop, isle.z + IslandHalf - 3.4f), Vector3.back, 1, "THE PLATFORM MAIN");
        LevelKit.LeverFrame(root, new Vector3(isle.x - 7f, IslandTop, isle.z - 4f), 90f, 5);
        LevelKit.Roundel(root, new Vector3(isle.x - IslandHalf - 0.08f, IslandTop - 3f, isle.z + 6f), Vector3.left, "TERMINUS NORTH", 2.2f);
        Beacon(new Vector3(isle.x + 8f, IslandTop, isle.z - 6f));

        // SOUTH: the street's paving over the rim, holding on nothing (it gives way 0.4 s after you land, back in 6), the
        // track bed's first plate still on its pier, two more that give way, the bare rails up to the island
        LevelKit.CollapsingSlab(root, new Vector3(-6f, 0f, rimS + 3.4f), new Vector2(8f, 6f), CutFloor + 0.5f);
        LevelKit.CollapsingSlab(root, new Vector3(-14f, -0.4f, rimS + 10.4f), new Vector2(8f, 6f), CutFloor + 0.5f);
        LevelKit.CollapsingSlab(root, new Vector3(-22f, -0.8f, rimS + 17.4f), new Vector2(8f, 6f), CutFloor + 0.5f);
        SurfaceKit.SpanSlab(root, new Vector3(Cut.x, 0f, rimS - 2f), new Vector3(Cut.x, -1.5f, rimS + 12f), 10f, 1f, LevelKit.Floor, "TrackBed");
        Pillar(new Vector3(Cut.x, -2.2f, rimS + 9f), 5f);
        LevelKit.CollapsingSlab(root, new Vector3(Cut.x, -2.2f, rimS + 27f), new Vector2(8f, 8f), CutFloor + 0.5f);
        LevelKit.CollapsingSlab(root, new Vector3(Cut.x, -2.4f, rimS + 36f), new Vector2(8f, 8f), CutFloor + 0.5f);
        Vector3 girderA = new Vector3(Cut.x + 3f, -1.2f, rimS + 45f), girderB = new Vector3(Cut.x, 3.2f, isle.z - IslandHalf - 3f);
        SurfaceKit.SpanSlab(root, girderA, girderB, 2.4f, 0.8f, LevelKit.RivetIron, "Rails");
        LevelKit.Rail(root, girderA, girderB);
        Pillar(new Vector3(Cut.x + 1.5f, 0.6f, rimS + 58f), 3f);
        LevelKit.HoloSign(root, new Vector3(-14f, 2.8f, rimS - 6f), Vector3.back, 9f, 1f, "PAVING UNSAFE  ·  DO NOT STOP", true);
        LevelKit.LineRun(root, new Vector3(0f, 0f, 246f), new Vector3(-6f, 0f, rimS - 1f), Zone.Surface);
        Beacon(new Vector3(Cut.x, 0f, rimS - 10f));

        // WEST, the burst main on the floor: three columns of steam out of its torn joints in a row from the rim to
        // the island. You do not jump this: you fly it
        SurfaceKit.Look(LevelKit.RivetIron, new Vector3(rimW + 30f, CutFloor + 1.2f, Cut.z), new Vector3(2.4f, 30f, 2.4f), Quaternion.Euler(0f, 0f, 90f), PrimitiveType.Cylinder);
        SurfaceKit.Solid(new Vector3(rimW + 30f, CutFloor + 1.2f, Cut.z), new Vector3(60f, 2.4f, 1.7f));
        for (int i = 0; i < 3; i++)
        {
            Vector3 c = new Vector3(rimW + 14f + i * 18f, 0f, Cut.z);
            LevelKit.SteamColumn(root, c, 9f, CutFloor + 2.4f, 24f);
            district.updrafts.Add(new District.Updraft { centre = c, half = 9f, y0 = CutFloor + 3f, y1 = 24f });
        }
        LevelKit.HoloSign(root, new Vector3(rimW - 6f, 3.2f, Cut.z), Vector3.left, 9f, 1.1f, "STEAM MAIN VENTING  ·  KEEP CLEAR", true);
        Beacon(new Vector3(rimW - 12f, 0f, Cut.z + 8f));

        // EAST, the way round: the main line past the hole runs between its rim and the parcels office
        LevelKit.LineRun(root, new Vector3(92f, 0f, rimS + 2f), new Vector3(92f, 0f, rimN - 40f), Zone.Surface);

        // NORTH: the track bed back onto the island in two spans over a pier, the carriage that went over the rim
        // hanging nose-down beside it (its roof a slide into the cut, onto a slope of rubble), the signal gantry over
        // the approach (its posts kick walls, its beam a lip), and the drones slung under the first span
        Vector3 nA = new Vector3(Cut.x, 0.5f, rimN + 2f), nM = new Vector3(Cut.x, 2.6f, rimN - 35f), nB = new Vector3(Cut.x, IslandTop + 0.3f, isle.z + IslandHalf + 1f);
        SurfaceKit.SpanSlab(root, nA, nM, 2.4f, 0.8f, LevelKit.RivetIron, "Rails");
        SurfaceKit.SpanSlab(root, nM, nB, 2.4f, 0.8f, LevelKit.RivetIron, "Rails");
        LevelKit.Rail(root, nA, nM);
        LevelKit.Rail(root, nM, nB);
        Pillar(nM + Vector3.down * 0.8f, 3f);
        for (int i = 1; i <= 2; i++)
        {
            Vector3 under = Vector3.Lerp(nA, nM, i / 3f) + Vector3.down * 5f;
            LevelKit.Cable(root, under + Vector3.up * 4.2f, under + Vector3.up * 0.7f, 0.06f);
            LevelKit.Orb(root, under);
        }
        LevelKit.SignalGantry(root, new Vector3(Cut.x, 0f, rimN + 7f), 10f, 5f, 0f);
        Vector3 nose = new Vector3(0f, -Mathf.Sin(40f * Mathf.Deg2Rad), -Mathf.Cos(40f * Mathf.Deg2Rad));
        Vector3 tail = new Vector3(Cut.x - 26f, 1.2f, rimN + 6f);
        Carriage(tail + nose * 8.5f, Quaternion.LookRotation(nose, Vector3.up));
        SurfaceKit.SpanSlab(root, tail + nose * 17f + new Vector3(0f, -1.5f, -2f), new Vector3(Cut.x - 26f, CutFloor, rimN - 36f), 6f, 1f, LevelKit.Slope, "RubbleSlide");
        Beacon(new Vector3(Cut.x + 22f, 0f, rimN + 14f));

        // The rim: what stood on the edge, tipped in — never on a way on
        for (int i = 0; i < 22; i++)
        {
            float a = i * (360f / 22f) * Mathf.Deg2Rad, r = 1.12f + Random.Range(0f, 0.16f);
            Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            Vector3 at = Cut + new Vector3(dir.x * CutHalfX * r, 0f, dir.z * CutHalfZ * r);
            if (at.x > -40f && at.x < 10f && at.z < Cut.z) continue;                 // the track bed's mouth and the paving over the rim
            if (at.x < rimW && Mathf.Abs(at.z - Cut.z) < 20f) continue;             // the steam's lip
            if (Mathf.Abs(at.x - Cut.x) < 16f && at.z > Cut.z) continue;            // the gantry
            if (at.x > -62f && at.x < -50f && at.z > Cut.z) continue;               // the hanging carriage
            if (Mathf.Abs(at.x) < 9f && at.z > Cut.z) continue;                     // the main's way out
            if (at.x > 44f) continue;                                               // the east rim: the main line
            float s = Random.Range(6f, 13f);
            SurfaceKit.Block(CityKit.Hollow, at + Vector3.up * (s * 0.18f), new Vector3(s * 1.4f, s * 0.35f, s),
                Quaternion.LookRotation(-dir) * Quaternion.Euler(Random.Range(10f, 26f), Random.Range(-20f, 20f), Random.Range(-8f, 8f)));
        }
    }

    /// A pier of the tunnel's rock out of the floor up to `top`: what a piece of the track bed sits on
    static void Pillar(Vector3 top, float width)
    {
        float h = top.y - CutFloor + 2f;
        SurfaceKit.Block(LevelKit.Rock, new Vector3(top.x, top.y - h * 0.5f, top.z), new Vector3(width, h, width), Quaternion.Euler(0f, Random.Range(0f, 90f), 0f));
    }
}
