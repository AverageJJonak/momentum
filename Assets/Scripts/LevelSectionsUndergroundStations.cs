using UnityEngine;

/// <summary>
/// THE UNDERGROUND's stations as course spaces (course-grammar; act2-underground): the TICKET HALL every Act II level
/// starts in (the MENU door behind you, turnstile banks as lips, ticket windows, a glazed waiting room on the line, and
/// the ESCALATORS out — three flights down to the platforms: a stopped one in the middle to run, a slide either side)
/// and the PLATFORM (a station: the platform, the sunk track bed beside it with the THIRD RAIL live along its edge, the
/// TRAIN through on a cycle, the footbridge and the west gallery over it all).
/// </summary>
public static partial class LevelSections
{
    /// TICKET HALL. MAIN: the staff gate in the middle of the barrier line, round the waiting room, down the stopped
    /// escalator. FAST: hop or corner-boost the turnstile banks and the brass rails (a metre, anywhere along the line),
    /// straight THROUGH the waiting room's glass, and the two outer escalators are slides (banisters either side).
    /// FUEL: an inspector at the staff gate, housekeeping at the ticket windows, a sweeper on the landing, a page over
    /// the hall. The hall drops `drop` to the platforms' level.
    public static Cursor TicketHall(Transform root, MovementConfig config, Cursor c, string name, float len = 60f, float halfW = 20f, float height = 12f, float drop = 6f)
    {
        OpenBegin(c);
        len = Mathf.Max(len, 50f);
        halfW = Mathf.Max(halfW, 16f);
        height = Mathf.Max(height, 10f);
        drop = Mathf.Clamp(drop, 4f, 8f);
        float z0 = c.z, z1 = c.z + len, y = c.floorY, low = y - drop;
        const float stairW = 8f, slideW = 4f, landing = 6f;
        const float flightsHalf = stairW * 0.5f + slideW;   // the well between the two solid masses: three flights, 8 m either side
        float fe = z1 - landing, slideRun = drop / Mathf.Tan(30f * Mathf.Deg2Rad);
        float hallEnd = fe - slideRun;
        Seal(root, c, halfW, low, height + drop);   // the back wall: the menu's, from the landing's level up
        LevelKit.Room(root, new Vector3(-halfW, low, z0), new Vector3(halfW, y + height, z1), OpenRoofed);   // the floor at the platforms' level: the landing
        LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, (z0 - T + hallEnd) * 0.5f), new Vector3(halfW * 2f + 2f * T, 1f, hallEnd - z0 + T), "HallFloor");
        LevelKit.WallFace(root, Face.North, hallEnd, -flightsHalf, flightsHalf, low, y - 1f);   // under the hall's end, over the landing
        // The escalator well: solid masses either side of the flights (their faces a hair off every plane they meet),
        // three flights at 30° between them — the stopped one in the middle to run, a slide either side
        for (int s = -1; s <= 1; s += 2)
        {
            float mw = halfW + T * 0.5f - flightsHalf;
            LevelKit.Slab(root, new Vector3(s * (flightsHalf + mw * 0.5f), (low + y + height - 0.05f) * 0.5f, (hallEnd + z1 - 0.05f) * 0.5f), new Vector3(mw, y + height - 0.05f - low, z1 - 0.05f - hallEnd), LevelKit.Wall, "WellMass");
            LevelKit.Escalator(root, s * (stairW * 0.5f + slideW * 0.5f), slideW, hallEnd, y, fe, low, false);
        }
        LevelKit.Escalator(root, 0f, stairW, hallEnd, y, fe, low, false);   // the stopped one: run it
        LevelKit.TileBand(root, flightsHalf, low, hallEnd + 0.4f, z1 - 0.4f);
        LevelKit.HoloSign(root, new Vector3(0f, y + 5f, hallEnd - 1.5f), Vector3.back, 7f, 1.5f, "TO THE TRAINS");
        LevelKit.GlobeCage(root, new Vector3(0f, y + 6f, fe - 2f), y + height);
        // The barrier line: a brass rail from each wall to a turnstile bank, the staff gate open in the middle
        float gz = z0 + len * 0.38f;
        for (int s = -1; s <= 1; s += 2)
        {
            LevelKit.TurnstileBank(root, new Vector3(s * 8f, y, gz), 0f, 3);
            LevelKit.Slab(root, new Vector3(s * (halfW + 9.9f) * 0.5f, y + 0.55f, gz), new Vector3(halfW - 9.9f, 1.1f, 0.08f), LevelKit.Brass, "BarrierRail");
            TechMarkers.MarkLip(new Vector3(s * (halfW + 9.9f) * 0.5f, y + 1.1f, gz), new Vector3(halfW - 9.9f, 0.9f, 1.4f));
        }
        LevelKit.Stripes(root, new Vector3(0f, y + 0.02f, gz), new Vector3(12f, 0.02f, 0.4f));
        LevelKit.HoloSign(root, new Vector3(0f, y + 4.2f, gz + 0.3f), Vector3.back, 5f, 1.1f, "STAFF GATE  ·  SHOW YOUR PASS");
        // The ticket windows, the waiting room on the line (its glass shatters at speed), benches, the kiosk
        LevelKit.TicketWindow(root, new Vector3(-halfW, y, z0 + 10f), Vector3.right);
        LevelKit.TicketWindow(root, new Vector3(-halfW, y, z0 + 14f), Vector3.right);
        float wz = z0 + len * 0.6f;
        LevelKit.Lip(root, new Vector3(-7f, y + 0.3f, wz), new Vector3(4.2f, 0.6f, 10f), LevelKit.Limestone, "WaitingRoomPlinth");
        LevelKit.Glass(root, new Vector3(-7f, y + 2f, wz - 5f), new Vector3(3.8f, 2.6f, 0.3f), config);
        LevelKit.Glass(root, new Vector3(-7f, y + 2f, wz + 5f), new Vector3(3.8f, 2.6f, 0.3f), config);
        for (int s = -1; s <= 1; s += 2)
        {
            LevelKit.Prim(root, new Vector3(-7f + s * 2f, y + 2f, wz), new Vector3(0.12f, 2.6f, 10.2f), LevelKit.Brass, "WaitingRoomFrame");
            LevelKit.Prim(root, new Vector3(-7f, y + 2f, wz + s * 5f), new Vector3(4.2f, 0.12f, 0.12f), LevelKit.Brass, "WaitingRoomFrame");
        }
        LevelKit.Prim(root, new Vector3(-7f, y + 3.4f, wz), new Vector3(4.4f, 0.2f, 10.4f), LevelKit.Charcoal, "WaitingRoomRoof");
        LevelKit.StationBench(root, new Vector3(-7f, y + 0.6f, wz), 90f);
        LevelKit.LineRun(root, new Vector3(-7f, y, gz + 2f), new Vector3(-7f, y, wz - 5.6f), c.zone);
        LevelKit.StationBench(root, new Vector3(halfW - 3f, y, z0 + len * 0.52f), -90f);
        LevelKit.StationBench(root, new Vector3(halfW - 3f, y, z0 + len * 0.6f), -90f);
        LevelKit.Kiosk(root, new Vector3(halfW - 4f, y, z0 + 9f), -90f);
        LevelKit.LineRun(root, new Vector3(0f, y, z0 + 8f), new Vector3(0f, y, gz - 1.2f), c.zone);
        Flavour(UndergroundFlavour.Station, name);
        Dress(root, c, halfW, y, height, z0, hallEnd);
        Course.Fuel(root, config, c, EnemyKind.Overseer, new Vector3(0f, y, gz + 3f), "the staff gate");
        Course.Fuel(root, config, c, EnemyKind.DamageControl, new Vector3(-halfW + 3.5f, y, z0 + 12f), "the ticket windows");
        Course.Fuel(root, config, c, EnemyKind.DamageControl, new Vector3(-halfW + 3.5f, y, z0 + 18f), "the ticket windows");
        Course.Fuel(root, config, c, EnemyKind.Scourer, new Vector3(0f, low, z1 - 3f), "the landing");
        Pool(config, new Vector3(4f, y + 6f, z0 + len * 0.5f), "over the hall", true);
        Last.spawn = new Vector3(0f, y + 1f, z0 + 6f);
        Last.menuPortal = new Vector3(0f, y + 2f, z0 + 1.7f);
        Postcard(new Vector3(-halfW * 0.7f, y + 5f, z0 + 5f), new Vector3(0f, y + 2f, gz), 74f);
        return OpenExit(c, z1, low, flightsHalf, height + drop);
    }

    /// PLATFORM. The platform on the east at `y` (the lane, 17 m clear); the track bed sunk 1.1 m beside it — the
    /// platform's edge a LIP all the way — with the track at x −3, the THIRD RAIL live along the platform side of it
    /// (red board, red lamps: hop it), and a TRAIN through every `period` seconds from a tunnel box at one end to the
    /// other and back. A gallery 6 m up the west wall (the WALL COMBO on its lip), the footbridge from it over the
    /// track to a mezzanine on the platform with a ramp down, refuges on the west wall, a pad on the bed.
    /// MAIN: the platform. FAST: the track bed at speed (the pad; the lip back up), the gallery and the footbridge.
    /// FUEL: a platelayer on the bed, a signalman on the footbridge, an inspector and two sweepers on the platform.
    public static Cursor Platform(Transform root, MovementConfig config, Cursor c, string name, float len = 120f, float halfW = 18f, float height = 12f, float period = 16f, float phase = 0f, bool northbound = true)
    {
        OpenBegin(c);
        len = Mathf.Max(len, 110f);
        halfW = Mathf.Max(halfW, 16f);
        height = Mathf.Max(height, 10f);
        const float pit = 1.1f, boxLen = 30f, edge = 1f, trackX = -3f;
        float z0 = c.z, z1 = c.z + len, y = c.floorY, bed = y - pit, mid = z0 + len * 0.5f;
        float r0 = z0 + boxLen, r1 = z1 - boxLen;   // the visible run between the tunnel mouths
        OpenThreshold(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, bed, z0), new Vector3(halfW, y + height, z1), OpenWalled);
        LevelKit.FloorSlab(root, new Vector3((edge + halfW + T * 0.5f) * 0.5f, y - 0.5f, mid), new Vector3(halfW + T * 0.5f - edge, 1f, len + 2f * T), "Platform");
        LevelKit.FloorSlab(root, new Vector3((edge - halfW - T * 0.5f) * 0.5f, bed - 0.5f, mid), new Vector3(halfW + T * 0.5f + edge, 1f, len), "TrackBed");
        LevelKit.BrassStrip(root, new Vector3(edge - 0.1f, y, r0 + 0.5f), new Vector3(edge - 0.1f, y, r1 - 0.5f));
        TechMarkers.MarkLip(new Vector3(edge, y, mid), new Vector3(1.4f, 0.9f, r1 - r0));
        LevelKit.Stripes(root, new Vector3(edge + 0.45f, y + 0.02f, mid), new Vector3(0.5f, 0.02f, r1 - r0));
        // The line, the live rail, the pad down the bed
        var train = LevelKit.TrainLine(root, trackX, bed, z0, z1, -halfW + 0.05f, edge - 0.05f, boxLen, 6f, period, phase, 3, 20f, northbound);
        LevelKit.ThirdRail(root, new Vector3(trackX + 2.5f, bed, r0 + 0.6f), new Vector3(trackX + 2.5f, bed, r1 - 0.6f));
        LevelKit.JumpPad(root, new Vector3(-10f, bed, r0 + 4f), 0f, 11f, 14f);
        LevelKit.LineRun(root, new Vector3(-10f, bed, r0 + 6f), new Vector3(-10f, bed, r1 - 3f), c.zone);
        // The gallery over the bed, the footbridge from it, the mezzanine and its ramp on the platform
        Catwalk(root, config, c, -1, 6.1f, 3.4f, halfW, bed, r0 + 22f, r1, "the west gallery");   // its wall run on the wall past the tunnel box
        float gx = -(halfW - 2f), by = y + 5f;
        LevelKit.FloorSlab(root, new Vector3((gx + 1.7f + edge + 3.4f) * 0.5f, by - 0.5f, mid), new Vector3(edge + 3.4f - gx - 1.7f, 1f, 3f), "Footbridge");
        for (int s = -1; s <= 1; s += 2) LevelKit.Parapet(root, new Vector3((gx + 1.7f + edge) * 0.5f, by + 0.45f, mid + s * 1.2f), new Vector3(edge - gx - 1.7f, 0.9f, 0.6f));
        LevelKit.Ramp(root, edge + 2f, 3.2f, mid + 1.5f, by, mid + 1.5f + Run18(5f), y, LevelKit.Floor);
        LevelKit.BrassStrip(root, new Vector3(edge + 0.5f, by, mid - 1.4f), new Vector3(edge + 3.4f, by, mid - 1.4f));
        for (int s = -1; s <= 1; s += 2) LevelKit.Prim(root, new Vector3(trackX - 3f - (s + 1) * 3f, by - 2.6f, mid + s * 1f), new Vector3(0.3f, 4.2f, 0.3f), LevelKit.Bronze, "BridgePost");
        LevelKit.HoloSign(root, new Vector3(-halfW + 0.15f, bed + 3f, r0 + 8f), Vector3.right, 4.2f, 1.2f, "LIVE RAIL", true);
        // The platform's furniture: benches facing the track, the name, the edge's words
        for (int i = 0; i < 3; i++) LevelKit.StationBench(root, new Vector3(halfW - 3f, y, r0 + 10f + i * (r1 - r0 - 20f) * 0.5f), -90f);
        LevelKit.Stencil(root, new Vector3(halfW - 0.1f, y + 2.05f, z0 + len * 0.4f), Vector3.left, "STAND BACK FROM THE EDGE", 0.08f, 6f);
        Flavour(UndergroundFlavour.Station, name);
        detailSkipWest = true;   // the west wall is the tunnel's: no trim over the bed
        Dress(root, c, halfW, y, height, z0, z1);
        Course.Fuel(root, config, c, EnemyKind.Borer, new Vector3(-9f, bed, mid + 8f), "the track bed");
        Course.Fuel(root, config, c, EnemyKind.Rigger, new Vector3(-6f, by, mid), "the footbridge");
        Course.Fuel(root, config, c, EnemyKind.Overseer, new Vector3(8f, y, z0 + len * 0.45f), "the platform");
        Course.Fuel(root, config, c, EnemyKind.Scourer, new Vector3(6f, y, z0 + len * 0.3f), "the platform");
        Course.Fuel(root, config, c, EnemyKind.Scourer, new Vector3(12f, y, z0 + len * 0.7f), "the platform");
        Pool(config, new Vector3(trackX, y + 4f, z0 + len * 0.62f), "over the track", true);
        Postcard(new Vector3(halfW * 0.6f, y + 5f, r0 - 2f), new Vector3(-3f, y + 1f, mid + 10f), 76f, signature: true);
        return OpenExit(c, z1, y, halfW, height);
    }
}
