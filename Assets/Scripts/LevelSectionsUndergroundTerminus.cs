using UnityEngine;

/// <summary>
/// THE TERMINUS as course spaces (course-grammar; act2-underground): the TURNTABLE COURT (the shunting yard under a
/// vault: the turntable in the middle with a loco turning on it, shunters running out of the west sheds to it across
/// the court, the through line along the east with its live rail, the gantry over the roads and the signal tower on
/// its stilts), the TERMINUS HALL (the grand concourse: a Course hall in the terminus's dress with the departures board
/// and the great clock) and the STATIONMASTER'S PLATFORM — the one gated space of Act II, kept as the Foreman's arena
/// was (its size, its seals, its controller and checkpoint untouched; another builder re-dresses the boss himself):
/// the turntable's rings in the middle he rises out of, the steam mains along the far wall, the buffer stops and the
/// live rail across it with a footbridge over, the stairs up to the gantry round three sides, the brass gates behind you.
/// </summary>
public static partial class LevelSections
{
    /// TURNTABLE COURT. MAIN: round the turntable, over the shunting roads at a run (never stand on one). FAST: the
    /// gantry over the west roads (a corner boost on its lip), the pads on the strips, the signal tower off its pad.
    /// FUEL: a signalman on the tower, gangers on the strips, sweepers on the gantry, roster units and pages.
    public static Cursor TurntableCourt(Transform root, MovementConfig config, Cursor c, float len = 96f, float halfW = 32f, float height = 16f)
    {
        OpenBegin(c);
        len = Mathf.Max(len, 84f);
        halfW = Mathf.Max(halfW, 26f);
        height = Mathf.Max(height, 14f);
        float z0 = c.z, z1 = c.z + len, y = c.floorY, mid = z0 + len * 0.5f;
        OpenThreshold(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), OpenRoofed);
        LevelKit.Life(root);
        LevelKit.Turntable(root, new Vector3(0f, y, mid), 10f);
        // The shunting roads from the west sheds to the turntable, a loco out and back on each
        float[] roadZ = { z0 + 26f, mid, z1 - 26f };
        for (int i = 0; i < 3; i++)
        {
            Vector3 inner = new Vector3(-11f, y, roadZ[i]), outer = new Vector3(-halfW + 1f, y, roadZ[i]);
            LevelKit.Rail(root, outer, inner);
            LevelKit.ShuntingLoco(root, new[] { new Vector3(-halfW - 9f, y, roadZ[i]), inner }, 14f, i * 0.3f);
            LevelKit.BlindPortal(root, new Vector3(-halfW, y, roadZ[i]), Vector3.right, 4.6f, 4.8f);
            LevelKit.Stripes(root, new Vector3(-(halfW + 11f) * 0.5f, y + 0.04f, roadZ[i] - 2.2f), new Vector3(halfW - 11f, 0.06f, 0.25f));
            LevelKit.Stripes(root, new Vector3(-(halfW + 11f) * 0.5f, y + 0.04f, roadZ[i] + 2.2f), new Vector3(halfW - 11f, 0.06f, 0.25f));
        }
        // The through line along the east: one-car trains at speed between boxes at the court's ends, the live rail
        LevelKit.TrainLine(root, halfW - 8f, y, z0, z1, halfW - 13f, halfW - 0.05f, 12f, 6f, 13f, 0.2f, 1, 20f, true);
        LevelKit.ThirdRail(root, new Vector3(halfW - 10.4f, y, z0 + 12.6f), new Vector3(halfW - 10.4f, y, z1 - 12.6f));
        // The gantry over the west roads: its lip, its deck on columns, its ramp down; the signal tower on its stilts
        // off a pad; pads on the strips; the travelling hoist; a gantry across the way in
        const float gx = -18f, gw = 3f, gh = 4.4f;
        float g0 = z0 + 14f, g1 = z1 - 6f - Run18(gh);
        LevelKit.Lip(root, new Vector3(gx, y + 0.7f, g0 - 3.5f), new Vector3(gw, 1.4f, 3f));
        LevelKit.LineRun(root, new Vector3(gx, y, z0 + 2f), new Vector3(gx, y, g0 - 5.2f), c.zone);
        LevelKit.FloorSlab(root, new Vector3(gx, y + gh - 0.4f, (g0 + g1) * 0.5f), new Vector3(gw, 0.8f, g1 - g0), "Gantry");
        LevelKit.Ramp(root, gx, gw, g1, y + gh, z1 - 6f, y);
        for (int i = 0; i < 4; i++) LevelKit.Column(root, new Vector3(gx, y, z0 + 19f + 16f * i), gh - 0.8f, 0.5f);
        LevelKit.BrassStrip(root, new Vector3(gx + gw * 0.5f - 0.1f, y + gh, g0 + 0.3f), new Vector3(gx + gw * 0.5f - 0.1f, y + gh, g1 - 0.3f));
        Vector3 tower = new Vector3(halfW - 19f, y, z0 + 40f);
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                LevelKit.Column(root, tower + new Vector3(sx * 2.4f, 0f, sz * 2.4f), 7f, 0.7f);
        LevelKit.FloorSlab(root, tower + Vector3.up * 6.5f, new Vector3(7f, 1f, 7f), "SignalTower");
        LevelKit.ControlDesk(root, tower + new Vector3(0f, 7f, 1.6f), 180f);
        LevelKit.LeverFrame(root, tower + new Vector3(0f, 7f, -1.8f), 0f, 6);
        LevelKit.JumpPad(root, tower + new Vector3(0f, 0f, -6f), 0f, Mathf.Sqrt(2f * 25f * 8.2f), 4f);
        LevelKit.JumpPad(root, new Vector3(-4f, y, z0 + 16f), 0f, 11f, 12f);
        LevelKit.JumpPad(root, new Vector3(6f, y, z1 - 16f), 0f, 11f, 12f);
        LevelKit.GantryCrane(root, halfW, y, z0 + 10f, z1 - 10f, height - 3f, 32f, 0f, 10f);
        LevelKit.SignalGantry(root, new Vector3(0f, y, z0 + 10f), 20f, 5.5f);
        LevelKit.HoloSign(root, new Vector3(0f, y + 7.4f, z1 - 1.5f), Vector3.back, 9f, 1.8f, "TURNTABLE COURT");
        LevelKit.HoloSign(root, new Vector3(-halfW + 0.15f, y + 6f, z0 + 38f), Vector3.right, 12f, 2.2f, "SHUNTING  ·  LOOK BOTH WAYS", true);
        Flavour(UndergroundFlavour.Tunnel, "TURNTABLE COURT");
        Dress(root, c, halfW, y, height, z0, z1);
        Course.Fuel(root, config, c, EnemyKind.Rigger, tower + new Vector3(0f, 7f, 0f), "the signal tower");
        Course.Fuel(root, config, c, EnemyKind.Blaster, new Vector3(-8f, y, z0 + 32f), "the first strip");
        Course.Fuel(root, config, c, EnemyKind.Blaster, new Vector3(8f, y, z1 - 32f), "the last strip");
        for (int i = 0; i < 2; i++) Course.Fuel(root, config, c, EnemyKind.Scourer, new Vector3(gx, y + gh, z0 + 30f + i * 20f), "the gantry");
        Pool(config, new Vector3(-6f, y, z0 + 21f), "the yard");
        Pool(config, new Vector3(4f, y, z1 - 20f), "past the turntable");
        Pool(config, new Vector3(-10f, y + 9f, z0 + 28f), "over the yard", true);
        Pool(config, new Vector3(10f, y + 9f, z0 + 56f), "over the yard", true);
        Postcard(new Vector3(-halfW * 0.7f, y + 9f, z0 + 6f), new Vector3(2f, y + 2f, mid), 78f, signature: true);
        return OpenExit(c, z1, y, halfW, height);
    }

    /// TERMINUS HALL. The grand concourse: a Course hall (pillar rows, two galleries, the chandelier orb) in the
    /// terminus's dress, the departures board and the great clock hung at its far end, the company's roundels
    public static Cursor TerminusHall(Transform root, MovementConfig config, Cursor c, float len = 100f, float halfW = 18f, float height = 16f)
    {
        Flavour(UndergroundFlavour.Station, "TERMINUS");
        float z0 = c.z, y = c.floorY;
        var next = Course.Hall(root, config, c, len, halfW, height);
        Vector3 board = new Vector3(0f, y + 9f, z0 + len * 0.8f);
        LevelKit.Display(root, board, Vector3.back, 10f, 2.2f, "DEPARTURES   ·   LAST TRAIN   ·   PLATFORM 9");
        LevelKit.ClockFace(root, board - Vector3.up * 2.6f, Vector3.back, 2.6f, 290f, 50f);
        for (int s = -1; s <= 1; s += 2)
        {
            LevelKit.Cable(root, board + new Vector3(s * 4.6f, 1.1f, 0f), new Vector3(s * 4.6f, y + height, board.z), 0.06f);
            LevelKit.Roundel(root, new Vector3(s * halfW, y + 7.5f, z0 + len * 0.5f), Vector3.left * s, "HALCYON RAILWAYS", 2.2f);
        }
        return next;
    }

    /// THE STATIONMASTER'S PLATFORM. A square hall with the turntable's rings in the middle (the Stationmaster rises
    /// out of the pit), the steam mains along the far wall behind the buffer stops and the live rail (a footbridge over
    /// it to the way out), stairs up both side walls to the gantry round three sides (out of the whistle's reach), the
    /// light rings to grapple, the hoist crossing overhead, the departures board over the brass gates that seal behind
    /// you. THE ONE GATED SPACE of Act II (its seals are named Boss*: the lint allows them)
    public static Cursor StationmasterPlatform(Transform root, MovementConfig config, Cursor c, float size = 62f, float height = 28f)
    {
        LevelKit.SetZone(c.zone);
        float halfW = size * 0.5f;
        float z0 = c.z, z1 = c.z + size, y = c.floorY, top = y + height;
        Vector3 centre = new Vector3(0f, y, z0 + size * 0.5f);
        var door = Door(y);
        Seal(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, top, z1), NoSouth, door);
        LevelKit.DoorFrame(root, door, z1);
        LandGlass(root, c);
        LevelKit.Life(root);
        // The floor: the turntable's rings, chevrons in from the gate, grates and vents in the corners
        LevelKit.FloorRing(root, centre, 9f, LevelKit.SignalRed);
        LevelKit.FloorRing(root, centre, 5.5f);
        LevelKit.Chevrons(root, new Vector3(0f, y, z0 + 5f), Vector3.forward, 3);
        for (int sx = -1; sx <= 1; sx += 2)
        {
            LevelKit.Grate(root, new Vector3(sx * (halfW - 9f), y, z0 + 9f), new Vector2(4f, 4f));
            LevelKit.SteamVent(root, new Vector3(sx * (halfW - 4f), y + 0.3f, z0 + 6f), Vector3.up, 9f, sx * 0.3f, 0.3f);
            LevelKit.SteamVent(root, new Vector3(sx * (halfW - 4f), y + 0.3f, z1 - 12f), Vector3.up, 11f, sx * 0.1f + 0.5f, 0.3f);
            LevelKit.Prim(root, new Vector3(sx * (halfW - 1.4f), y + 1f, z0 + 2.5f), Vector3.one * 0.4f, LevelKit.SignalRed, "GateLamp", PrimitiveType.Sphere);
        }
        // The far wall: the four steam mains rising behind the buffer stops, the live rail across, the footbridge over it
        for (int i = 0; i < 4; i++)
        {
            Vector3 foot = new Vector3(-21f + i * 14f, y, z1 - 2.6f);
            LevelKit.Pipe(root, foot + Vector3.up * 0.8f, foot + Vector3.up * 9.6f, 1.6f);
            LevelKit.Pipe(root, foot + Vector3.up * 9.6f, foot + new Vector3(0f, 9.6f, 2.4f), 1.6f);
            LevelKit.Valve(root, foot + new Vector3(0f, 3.2f, -1f), Vector3.forward);
            LevelKit.Gauge(root, foot + new Vector3(1.2f, 4.6f, -0.85f), Vector3.back);
            LevelKit.Prim(root, foot + new Vector3(0f, 0.4f, 0f), new Vector3(2.6f, 0.8f, 2.6f), LevelKit.RivetIron, "MainFoot");
        }
        LevelKit.Rail(root, new Vector3(-halfW + 6f, y, z1 - 8.5f), new Vector3(halfW - 6f, y, z1 - 8.5f));
        LevelKit.ThirdRail(root, new Vector3(-halfW + 7f, y, z1 - 7.3f), new Vector3(halfW - 7f, y, z1 - 7.3f));
        for (int s = -1; s <= 1; s += 2) LevelKit.Lip(root, new Vector3(s * (halfW - 5f), y + 0.7f, z1 - 8.5f), new Vector3(2f, 1.4f, 2.6f), LevelKit.RivetIron, "BufferStop");
        LevelKit.FloorSlab(root, new Vector3(0f, y + 0.55f, z1 - 8f), new Vector3(5f, 0.5f, 3.6f), "RailBridge");
        // Stairs up both side walls to the gantry, ten metres up, round three sides
        float mezY = y + 10f;
        LevelKit.Ramp(root, -halfW + 2.2f, 4.2f, z0 + 3f, y, z0 + 26f, mezY);
        LevelKit.Ramp(root, halfW - 2.2f, 4.2f, z0 + 3f, y, z0 + 26f, mezY);
        float mezZ0 = z0 + 25f, mezLen = z1 - 1f - mezZ0;
        LevelKit.FloorSlab(root, new Vector3(-halfW + 3f, mezY - 0.5f, mezZ0 + mezLen * 0.5f), new Vector3(6f, 1f, mezLen), "GantryW");
        LevelKit.FloorSlab(root, new Vector3(halfW - 3f, mezY - 0.5f, mezZ0 + mezLen * 0.5f), new Vector3(6f, 1f, mezLen), "GantryE");
        LevelKit.FloorSlab(root, new Vector3(0f, mezY - 0.5f, z1 - 3f), new Vector3(size - 12f, 1f, 6f), "GantryN");
        LevelKit.BrassStrip(root, new Vector3(-halfW + 6f, mezY, mezZ0 + 0.3f), new Vector3(-halfW + 6f, mezY, z1 - 6.3f));
        LevelKit.BrassStrip(root, new Vector3(halfW - 6f, mezY, mezZ0 + 0.3f), new Vector3(halfW - 6f, mezY, z1 - 6.3f));
        LevelKit.BrassStrip(root, new Vector3(-halfW + 6.3f, mezY, z1 - 6f), new Vector3(halfW - 6.3f, mezY, z1 - 6f));
        LevelKit.TileBand(root, halfW, mezY, mezZ0 + 1f, z1 - 2f);
        LevelKit.GlobeCage(root, new Vector3(-halfW + 3f, mezY + 4f, z1 - 10f), top);
        LevelKit.GlobeCage(root, new Vector3(halfW - 3f, mezY + 4f, z1 - 10f), top);
        // Overhead: light rings to grapple, the hoist crossing the floor, brass ribs, fans, floods, the company's sun
        for (int k = 0; k < 4; k++)
        {
            float a = (k * 90f + 45f) * Mathf.Deg2Rad;
            LevelKit.LightRing(root, centre + new Vector3(Mathf.Cos(a) * 14f, 17f, Mathf.Sin(a) * 14f), top, true);
        }
        LevelKit.GantryCrane(root, halfW, y, z0 + 8f, z1 - 12f, height - 5f, 30f, 0f, -8f);
        for (float z = z0 + 8f; z < z1 - 6f; z += 14f)
            LevelKit.Beam(root, new Vector3(-halfW + 0.5f, top - 1f, z), new Vector3(halfW - 0.5f, top - 1f, z), 0.7f, 0.6f, LevelKit.Cream);
        LevelKit.WallFan(root, new Vector3(-halfW, top - 6f, z0 + 20f), Vector3.right, 2.4f);
        LevelKit.WallFan(root, new Vector3(halfW, top - 6f, z0 + 40f), Vector3.left, 2.4f);
        LevelKit.Floodlight(root, new Vector3(-halfW + 1f, top - 3f, z0 + 12f), new Vector3(0.6f, -0.7f, 0.3f), 40f, 3f);
        LevelKit.Floodlight(root, new Vector3(halfW - 1f, top - 3f, z0 + 12f), new Vector3(-0.6f, -0.7f, 0.3f), 40f, 3f);
        LevelKit.Floodlight(root, new Vector3(-halfW + 1f, top - 3f, z1 - 12f), new Vector3(0.6f, -0.7f, -0.3f), 40f, 3f);
        LevelKit.Floodlight(root, new Vector3(halfW - 1f, top - 3f, z1 - 12f), new Vector3(-0.6f, -0.7f, -0.3f), 40f, 3f);
        LevelKit.Light(root, new Vector3(-12f, y + 19f, centre.z), 44f, 1.6f);
        LevelKit.Light(root, new Vector3(12f, y + 19f, centre.z), 44f, 1.6f);
        LevelKit.Emblem(root, new Vector3(0f, y + 21f, z1 - 0.25f), Quaternion.LookRotation(Vector3.back), 5f, LevelKit.Brass, LevelKit.Graphite);
        // The far wall: the departures board over the way out, the company's name in lights, the brass gates drawn back
        TextMesh board = LevelKit.Display(root, new Vector3(0f, y + 8.4f, z1 - 0.1f), Vector3.back, 7f, 1.7f, "LAST TRAIN   0041");
        LevelKit.HoloSign(root, new Vector3(0f, y + 14.5f, z1 - 0.15f), Vector3.back, 14f, 2.6f, "HALCYON RAILWAYS  ·  TERMINUS");
        LevelKit.Sign(root, new Vector3(0f, y + 6.4f, z1), Vector3.back, 10f, 1.8f, "PLATFORM 9");
        for (int s = -1; s <= 1; s += 2)
        {
            LevelKit.Slab(root, new Vector3(s * 6.4f, y + 3f, z1 - 0.9f), new Vector3(6f, 6f, 0.5f), LevelKit.Brass, "GateLeaf");
            for (int i = 0; i < 5; i++) LevelKit.Prim(root, new Vector3(s * (3.9f + i * 1.25f), y + 3f, z1 - 1.2f), new Vector3(0.12f, 5.6f, 0.08f), LevelKit.Bronze, "GateBar");
        }
        LevelKit.Mosaic(root, new Vector3(-halfW, y + 2.75f, z0 + 12f), Vector3.right, "TERMINUS  ·  PLATFORM 9", 7f, 1.1f);
        LevelKit.Stencil(root, new Vector3(halfW - 0.1f, y + 2.6f, z0 + 12f), Vector3.left, "THE STATIONMASTER'S PLATFORM  ·  STAFF ONLY", 0.09f, 10f);
        Flavour(UndergroundFlavour.Station, "TERMINUS");
        denseLight = true;   // the arena: the trim on its walls and over it, nothing on its floor
        Dress(root, c, halfW, y, height, z0, z1);

        // The Stationmaster, on his lift under the pit; the doors; the cutscene; the theme (the boss's own mechanics)
        var boss = Enemy.CreateBoss(centre, config, 3.6f, Enemy.BossKind.Foreman);
        var host = new GameObject("TheStationmaster");
        host.transform.SetParent(root);
        var ctrl = host.AddComponent<BossForeman>();
        ctrl.config = config;
        ctrl.body = boss;
        ctrl.arenaCenter = centre;
        ctrl.arenaRadius = halfW;
        ctrl.floorY = y;
        ctrl.checkpoint = new Vector3(0f, y + 1f, z0 + 4f);
        ctrl.entrySeal = LevelKit.Slab(root, new Vector3(0f, y + 2.2f, z0), new Vector3(7f, 4.4f, 0.8f), LevelKit.Brass, "BossEntrySeal");
        ctrl.exitSeal = LevelKit.Slab(root, new Vector3(0f, y + 2.2f, z1), new Vector3(7f, 4.4f, 0.8f), LevelKit.Brass, "BossExitSeal");
        ctrl.shiftText = board;
        var intro = host.AddComponent<BossIntro>();
        intro.boss = ctrl;
        intro.triggerCenter = new Vector3(0f, y + 2f, z0 + 16f);
        intro.triggerSize = new Vector3(size, 6f, 4f);
        host.AddComponent<BossMusic>();

        Postcard(new Vector3(-halfW * 0.7f, y + 12f, z0 + 5f), centre + Vector3.up * 6f, 72f, signature: true);
        return Exit(c, z1, y, halfW, y, top);
    }
}
