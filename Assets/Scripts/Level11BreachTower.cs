using UnityEngine;

/// <summary>
/// THE CLOCK TOWER, THE HOUSE and THE MAINS: Level 11's east and north — the fallen campanile, the way out on top of
/// Halcyon Railways' house, and the burst main between the cut and the stack. (Level11Builder partial)
/// </summary>
public static partial class Level11Builder
{
    // ------------------------------------------------------------ the clock tower

    /// The railway's clock tower, on its side across the east of the district: it fell INTO the cut, its foot by the
    /// house and its crown on the parcels office, sixty-eight metres up over the open ground. Its facade is the floor:
    /// two hundred and forty metres of limestone at sixteen degrees, the window rows going by, four string courses
    /// across it (LIPS: a ladder going up, four launches coming down), the clock's faces at the crown — one looking at
    /// the sky — and the crown itself, with the valve in it. Three ways up: the pads up the parcels office, the stair of
    /// floors up its west face, or the string courses one corner boost at a time
    static void ClockTower()
    {
        Quaternion rot = SurfaceKit.SpanRot(TowerLow, TowerHigh);
        float len = Vector3.Distance(TowerLow, TowerHigh);
        SurfaceKit.SpanSlab(root, TowerLow, TowerHigh, TowerW, TowerThick, LevelKit.Limestone, "ClockTower");
        for (int i = 1; i < 36; i++)
            for (int s = -1; s <= 1; s += 2)
                SurfaceKit.Look(LevelKit.Dark, SurfaceKit.OnSpan(TowerLow, TowerHigh, i / 36f, s * 6.5f, 0.03f), new Vector3(7f, 0.05f, 2f), rot);
        SurfaceKit.Look(LevelKit.Fluted, SurfaceKit.OnSpan(TowerLow, TowerHigh, 0.5f, 0f, 0.02f), new Vector3(1.6f, 0.04f, len - 10f), rot);
        for (int s = -1; s <= 1; s += 2)
            SurfaceKit.Look(LevelKit.Brass, SurfaceKit.OnSpan(TowerLow, TowerHigh, 0.5f, s * (TowerW * 0.5f - 0.4f), 0.05f), new Vector3(0.3f, 0.024f, len - 4f), rot);
        float[] at = { 0.2f, 0.4f, 0.6f, 0.8f };
        for (int i = 0; i < at.Length; i++)
        {
            SurfaceKit.Block(LevelKit.Limestone, SurfaceKit.OnSpan(TowerLow, TowerHigh, at[i], 0f, 0.6f), new Vector3(TowerW - 2f, 1.2f, 2.4f), rot);
            for (int s = -1; s <= 1; s += 2)
                SurfaceKit.Look(LevelKit.Brass, SurfaceKit.OnSpan(TowerLow, TowerHigh, at[i], 0f, 1.215f) + rot * new Vector3(0f, 0f, s * 1.11f), new Vector3(TowerW - 2.1f, 0.024f, 0.14f), rot);
            TechMarkers.MarkLip(SurfaceKit.OnSpan(TowerLow, TowerHigh, at[i], 0f, 1.2f), new Vector3(TowerW - 1.4f, 0.9f, 3f));
        }
        for (int i = 0; i < 3; i++) LevelKit.SteamLeak(root, SurfaceKit.OnSpan(TowerLow, TowerHigh, 0.3f + i * 0.2f, (i % 2 == 0 ? 1f : -1f) * 9f, 0.2f), 3f);
        // The foot: a lip down to the street, so what comes down the facade at speed leaves it at speed
        SurfaceKit.SpanSlab(root, TowerLow - Along * 16f + Vector3.down * 1.45f, TowerLow, TowerW, 1.2f, LevelKit.Slope, "TowerFoot");
        Vector3 scree = TowerLow - Along * 6f + new Vector3(24f, 0f, 0f);
        Heap(new Vector3(scree.x, 0f, scree.z), 8f, 6);
        Beacon(TowerLow - Along * 26f + new Vector3(-6f, -1.5f, 0f));

        // The crown, on the parcels office: the clock's faces (one on the facade, looking at the sky; one each side),
        // the deck beyond with the valve on it, the lantern where it fell on the office's roof
        Vector3 face = SurfaceKit.OnSpan(TowerLow, TowerHigh, 0.955f, 0f, 0.03f);
        SurfaceKit.Look(LevelKit.Brass, face, new Vector3(13f, 0.06f, 13f), rot, PrimitiveType.Cylinder);
        SurfaceKit.Look(LevelKit.LampCream, face + rot * Vector3.up * 0.04f, new Vector3(12.4f, 0.04f, 12.4f), rot, PrimitiveType.Cylinder);
        for (int i = 0; i < 12; i++)
        {
            Quaternion m = rot * Quaternion.Euler(0f, i * 30f, 0f);
            SurfaceKit.Look(LevelKit.Charcoal, face + m * new Vector3(0f, 0.07f, 5.3f), new Vector3(i % 3 == 0 ? 0.5f : 0.3f, 0.02f, i % 3 == 0 ? 1.4f : 0.9f), m);
        }
        Quaternion hour = rot * Quaternion.Euler(0f, 300f, 0f), minute = rot * Quaternion.Euler(0f, 60f, 0f);
        SurfaceKit.Look(LevelKit.Charcoal, face + hour * new Vector3(0f, 0.08f, 1.8f), new Vector3(0.5f, 0.02f, 3.8f), hour);
        SurfaceKit.Look(LevelKit.Charcoal, face + minute * new Vector3(0f, 0.09f, 2.6f), new Vector3(0.35f, 0.02f, 5.4f), minute);
        for (int s = -1; s <= 1; s += 2)
            LevelKit.ClockFace(root, SurfaceKit.OnSpan(TowerLow, TowerHigh, 0.955f, s * (TowerW * 0.5f + 0.08f), -10f), rot * Vector3.right * s, 12f, 300f, 60f);
        Vector3 crown = TowerHigh + Along * 13f;
        Quaternion level = Quaternion.LookRotation(Along, Vector3.up);
        var deck = LevelKit.FloorSlab(root, crown + Vector3.down * 0.5f, new Vector3(TowerW, 1f, 26f), "Crown");
        deck.transform.rotation = level;
        SurfaceKit.Block(CityKit.Hollow, crown + Vector3.down * 11.5f, new Vector3(TowerW, 21f, 26f), level);
        for (int s = -1; s <= 1; s += 2)
            SurfaceKit.Block(LevelKit.Limestone, crown + level * new Vector3(s * (TowerW * 0.5f - 0.25f), 0.6f, 0f), new Vector3(0.5f, 1.2f, 26f), level);
        LevelKit.ReliefValve(root, district, TowerHigh + Along * 21f, -Along, 2, "THE CLOCK TOWER MAIN");
        Beacon(TowerHigh + Along * 5f + level * new Vector3(9f, 0f, 0f));

        // The parcels office under the crown — an edge-boost corner on every corner, the crown's lantern on its roof —
        // and the ways up it: the leap pads ledge to ledge up its south face (every one throws you straight up, so the
        // ledge you want is never more than a drift away), and the STAIR OF FLOORS up its west face: its floors sticking
        // out, a lip on each, a corner boost to the next six metres up, seven times, for whoever would rather climb
        Vector3 office = new Vector3(118.5f, 23.5f, 362f);
        LevelKit.BoostCorner(root, office, new Vector3(44f, 47f, 40f), LevelKit.Limestone);
        for (float y = 4f; y < 47f; y += 6f) SurfaceKit.Look(LevelKit.Limestone, new Vector3(office.x, y, office.z), new Vector3(44.4f, 0.4f, 40.4f));
        SurfaceKit.Block(LevelKit.Brass, new Vector3(114f, 48.5f, 352f), new Vector3(4f, 3f, 6f), level);
        SurfaceKit.Look(LevelKit.SignalRed, new Vector3(114f, 50.4f, 352f), Vector3.one * 0.6f, null, PrimitiveType.Sphere);
        LevelKit.JumpPad(root, new Vector3(103f, 0f, 329f), 0f, 34f, 0f);
        SurfaceKit.Block(LevelKit.RivetIron, new Vector3(115f, 19.5f, 337f), new Vector3(16f, 1f, 10f));
        LevelKit.Chevrons(root, new Vector3(108f, 20f, 337f), Vector3.right, 3);
        LevelKit.JumpPad(root, new Vector3(119f, 20f, 339f), 0f, 34f, 0f);
        SurfaceKit.Block(LevelKit.RivetIron, new Vector3(104f, 39.5f, 337f), new Vector3(16f, 1f, 10f));
        LevelKit.Chevrons(root, new Vector3(110f, 40f, 337f), Vector3.left, 3);
        LevelKit.JumpPad(root, new Vector3(104f, 40f, 340f), 0f, 48f, 0f);
        LevelKit.HoloSign(root, new Vector3(104f, 3.6f, 324f), Vector3.back, 9f, 1f, "PARCELS OFFICE  ·  EMERGENCY ACCESS");
        Beacon(new Vector3(96f, 0f, 322f));
        for (int i = 1; i <= 7; i++)
        {
            float y = i * 6f, zc = office.z + (i % 2 == 0 ? 8f : -8f);
            LevelKit.FloorSlab(root, new Vector3(94.2f, y - 0.3f, zc), new Vector3(4.6f, 0.6f, 10f), "OfficeFloor");
            LevelKit.Lip(root, new Vector3(92.5f, y + 0.6f, zc), new Vector3(1.2f, 1.2f, 10f), LevelKit.Limestone, "FloorLip");
        }
    }

    // ------------------------------------------------------------ the house, and the stack on it

    /// The way out: Halcyon Railways' house, the first standing roof of the rooftops to the House — limestone, fluted,
    /// the clock on its flanks, the name on its front — with the RELIEF STACK on its south face: brick, iron-banded,
    /// the grate and the governor at its foot, the burst main venting up it and lifting whoever runs into it to the
    /// roof (always: the main's residual pressure; MAIN PRESSURE with the valves shut, harder and higher), the gauge
    /// on it the whole district can see, the bridge off its roof through the cordon's gate — and the things round it
    /// that are not the way the railway meant: the snipers up its west face, the crane over its roof
    static void TerminusHouse()
    {
        float south = Goal.z - GoalHalf;
        SurfaceKit.Block(LevelKit.Limestone, Goal + Vector3.up * ((RoofY - 1f) * 0.5f), new Vector3(GoalHalf * 2f, RoofY - 1f, GoalHalf * 2f));
        LevelKit.FloorSlab(root, Goal + Vector3.up * (RoofY - 0.5f), new Vector3(GoalHalf * 2f, 1f, GoalHalf * 2f), "Roof");
        for (int side = 0; side < 4; side++)
        {
            Quaternion f = Quaternion.Euler(0f, side * 90f, 0f);
            Vector3 n = f * Vector3.back, right = f * Vector3.right, wall = Goal + n * GoalHalf;
            for (int b = -3; b <= 3; b++)
                SurfaceKit.Look(LevelKit.Fluted, wall + right * (b * 6.4f) + n * 0.2f + Vector3.up * (RoofY * 0.5f - 0.5f), new Vector3(1.4f, RoofY - 3f, 0.4f), f);
            for (float y = 6f; y < RoofY - 4f; y += 4f)
                for (int b = -3; b < 3; b++)
                {
                    if (side == 0 && Mathf.Abs(b + 0.5f) < 1.6f) continue;   // the stack is on the south face
                    SurfaceKit.Look(LevelKit.Dark, wall + right * ((b + 0.5f) * 6.4f) + n * 0.06f + Vector3.up * y, new Vector3(3.6f, 1.7f, 0.1f), f);
                }
            for (float y = 20f; y < RoofY - 2f; y += 20f)
                SurfaceKit.Look(LevelKit.Limestone, wall + n * 0.25f + Vector3.up * y, new Vector3(GoalHalf * 2f + 0.9f, 0.5f, 0.5f), f);
            SurfaceKit.Look(LevelKit.Brass, wall + n * 0.3f + Vector3.up * (RoofY - 1.4f), new Vector3(GoalHalf * 2f + 1f, 0.6f, 0.6f), f);
        }
        LevelKit.Text(root, new Vector3(-13f, 72f, south - 0.45f), Quaternion.LookRotation(Vector3.forward), "HALCYON", 0.7f, LevelKit.SignCream, "HouseName", 14f, true, 400f, 900f);
        LevelKit.Text(root, new Vector3(13f, 72f, south - 0.45f), Quaternion.LookRotation(Vector3.forward), "RAILWAYS", 0.7f, LevelKit.SignCream, "HouseName", 14f, true, 400f, 900f);
        LevelKit.ClockFace(root, new Vector3(GoalHalf + 0.06f, 64f, Goal.z), Vector3.right, 8f, 300f, 60f);
        LevelKit.ClockFace(root, new Vector3(-GoalHalf - 0.06f, 64f, Goal.z), Vector3.left, 8f, 300f, 60f);
        for (int s = -1; s <= 1; s += 2)
        {
            LevelKit.Parapet(root, new Vector3(s * (GoalHalf - 0.25f), RoofY + 0.6f, Goal.z - 3f), new Vector3(0.5f, 1.2f, GoalHalf * 2f - 6f));
            LevelKit.Parapet(root, new Vector3(s * 13.1f, RoofY + 0.6f, Goal.z + GoalHalf - 0.25f), new Vector3(17.8f, 1.2f, 0.5f));   // the north wall, open 4.2 m either side of the bridge
        }

        // THE RELIEF STACK: three brick walls on the south face and a cap, open at the foot and open to the roof. It
        // vents from the start (course-grammar: the way up is never gated); on MAIN PRESSURE, with the three valves
        // shut, it lifts harder (District.Updraft.liveBoost), so the stack stands twenty metres taller than the ride needs
        const float stackHalf = 3.6f, stackDeep = 8f, stackTop = 104f;
        float sz = south - stackDeep * 0.5f;
        SurfaceKit.Block(LevelKit.Brick, new Vector3(-stackHalf - 0.5f, stackTop * 0.5f, sz), new Vector3(1f, stackTop, stackDeep));
        SurfaceKit.Block(LevelKit.Brick, new Vector3(stackHalf + 0.5f, stackTop * 0.5f, sz), new Vector3(1f, stackTop, stackDeep));
        SurfaceKit.Block(LevelKit.Brick, new Vector3(0f, (6f + stackTop) * 0.5f, south - stackDeep - 0.5f), new Vector3(stackHalf * 2f + 2f, stackTop - 6f, 1f));
        SurfaceKit.Block(LevelKit.Brick, new Vector3(0f, stackTop + 0.5f, sz - 0.5f), new Vector3(stackHalf * 2f + 2f, 1f, stackDeep + 1f));
        SurfaceKit.Look(LevelKit.Limestone, new Vector3(0f, stackTop + 1.3f, sz - 0.5f), new Vector3(stackHalf * 2f + 3f, 0.6f, stackDeep + 2f));
        SurfaceKit.Look(LevelKit.Limestone, new Vector3(0f, stackTop + 1.9f, sz - 0.5f), new Vector3(stackHalf * 2f + 1.6f, 0.6f, stackDeep + 0.6f));
        for (float y = 12f; y < stackTop; y += 12f)
        {
            SurfaceKit.Look(LevelKit.RivetIron, new Vector3(0f, y, sz - 0.5f), new Vector3(stackHalf * 2f + 3.4f, 0.5f, stackDeep + 1.4f));
            SurfaceKit.Look(LevelKit.Brass, new Vector3(0f, y, south - 0.1f), new Vector3(stackHalf * 2f, 0.12f, 0.1f));
        }
        SurfaceKit.Look(LevelKit.CordonBand, new Vector3(0f, 6.1f, south - stackDeep - 1.02f), new Vector3(stackHalf * 2f, 0.2f, 0.06f));
        Vector3 grate = new Vector3(0f, 0f, sz);
        district.fanHubs.Add(LevelKit.ReliefGrate(root, grate, stackHalf * 2f - 0.6f));
        var steam = LevelKit.StackSteam(root, grate, stackHalf * 2f - 0.6f, 90f);
        if (steam != null) district.shaftAir.Add(steam);
        district.updrafts.Add(new District.Updraft { centre = grate, half = stackHalf - 0.2f, y0 = 0.4f, y1 = 90f, needsPower = false, liveBoost = true });
        LevelKit.SteamLeak(root, grate + new Vector3(-1.6f, 0.5f, 0f), 8f);
        LevelKit.SteamLeak(root, grate + new Vector3(1.6f, 0.5f, 0f), 8f);
        float face = south - stackDeep - 1.06f;
        for (int i = 0; i < 3; i++)
        {
            district.gauge.Add(LevelKit.GaugeLamp(root, new Vector3(0f, 34f + i * 13f, face), Vector3.back, 5f));
            LevelKit.Text(root, new Vector3(0f, 30.2f + i * 13f, face - 0.05f), Quaternion.LookRotation(Vector3.forward), "VALVE " + (i + 1), 0.14f, LevelKit.SignCream, "GaugeText", 5f, true, 160f, 400f);
        }
        LevelKit.Gauge(root, new Vector3(-2.2f, 9.5f, face), Vector3.back);
        LevelKit.Gauge(root, new Vector3(2.2f, 9.5f, face), Vector3.back);
        district.deadSign = SignHolder("RELIEF STACK  ·  RESIDUAL PRESSURE", new Vector3(0f, 8f, face), false);
        district.liveSign = SignHolder("RELIEF STACK  ·  MAIN PRESSURE", new Vector3(0f, 8f, face), false);
        LevelKit.Mosaic(root, new Vector3(-stackHalf - 1.05f, 3.2f, sz), Vector3.left, "LIFT 3 MAINS  ·  RELIEF", 6f, 1f);

        // The plaza: the police's line, trestles across it (lips: boost them, slide them), their floods, HARD LINE to
        // the stack's foot through housekeeping, the kicker at the plaza's east edge for whatever comes off the tower
        LevelKit.Trestle(root, new Vector3(-16f, 0f, 640f), 8f, 0f);
        LevelKit.Trestle(root, new Vector3(16f, 0f, 640f), 8f, 0f);
        LevelKit.Trestle(root, new Vector3(-30f, 0f, 664f), 8f, 60f);
        LevelKit.Trestle(root, new Vector3(30f, 0f, 664f), 8f, -60f);
        LevelKit.Floodlight(root, new Vector3(-14f, 9f, south - 1f), new Vector3(-0.3f, -0.7f, -0.6f), 40f, 2.8f);
        LevelKit.Floodlight(root, new Vector3(14f, 9f, south - 1f), new Vector3(0.3f, -0.7f, -0.6f), 40f, 2.8f);
        Beacon(new Vector3(10f, 0f, 622f));
        LevelKit.LineRun(root, new Vector3(0f, 0f, 612f), new Vector3(0f, 0f, 686f), Zone.Surface);
        SurfaceKit.SpanSlab(root, new Vector3(128f, 0.05f, 700f), new Vector3(100f, 10f, 703f), 12f, 0.9f, LevelKit.Slope, "PlazaKicker");
        SurfaceKit.Block(LevelKit.Limestone, new Vector3(101.5f, 4.4f, 703f), new Vector3(3f, 8.8f, 9f));

        // The crane over the roof, and the orb on its hook: too high for the street, not for someone arriving some other way
        SurfaceKit.Block(LevelKit.Girder, new Vector3(62f, 53f, 700f), new Vector3(3f, 106f, 3f));
        SurfaceKit.SpanBlock(LevelKit.Girder, new Vector3(70f, 107.6f, 700f), new Vector3(14f, 107.6f, 700f), 1.6f, 1.6f);
        SurfaceKit.Look(CityKit.Mass, new Vector3(72f, 104f, 700f), new Vector3(5f, 4f, 4f));
        LevelKit.Cable(root, new Vector3(30f, 106f, 700f), new Vector3(30f, 97f, 700f), 0.08f);
        LevelKit.Orb(root, new Vector3(30f, 96f, 700f));
        LevelKit.Milk(root, new Vector3(16f, 107.6f, 700f));   // every facility has one at the end

        // THE WAY OUT: the bridge off the roof, through the gate, to the next roof — and the door in it. Eight metres
        // wide, six between the posts (course-grammar: no lane under six), the posts edge-boost corners
        SurfaceKit.SpanSlab(root, new Vector3(0f, RoofY, Goal.z + GoalHalf - 0.5f), new Vector3(0f, RoofY, Z1 + 2f), 8f, 1.2f, LevelKit.Floor, "Bridge");
        for (int s = -1; s <= 1; s += 2)
        {
            SurfaceKit.Block(LevelKit.Limestone, new Vector3(s * 3.75f, RoofY + 0.5f, (Goal.z + GoalHalf + Z1) * 0.5f), new Vector3(0.3f, 1f, Z1 - Goal.z - GoalHalf));
            LevelKit.BoostCorner(root, new Vector3(s * 3.6f, RoofY + 2.18f, 770f), new Vector3(1.2f, 4.4f, 1.2f), LevelKit.Charcoal);
        }
        Beacon(new Vector3(-10f, RoofY, Goal.z - 8f));
    }

    static GameObject SignHolder(string line, Vector3 at, bool red)
    {
        var go = new GameObject("StackSign");
        go.transform.SetParent(root, false);
        go.transform.position = at;
        LevelKit.HoloSign(go.transform, at, Vector3.back, 8.6f, 1.2f, line, red);
        return go;
    }

    // ------------------------------------------------------------ the mains

    /// THE MAINS: the burst main itself, out of the cut's north-east corner as a ramp and north across the open ground
    /// on its cradles to the valve house — a brass lip line along its top with HARD LINE on it, the mains' vents either
    /// side of it bursting in turn (timed launches), and a brick valve house at its end with cream flutes on its corners
    static void Mains()
    {
        float rimN = Cut.z + CutHalfZ, z0 = rimN + 2f, z1 = 601f;
        Vector3 pitEnd = new Vector3(0f, CutFloor + 1.2f, rimN - 46f), rimEnd = new Vector3(0f, 1.4f, z0);
        Vector3 climb = (rimEnd - pitEnd).normalized;
        float climbLen = Vector3.Distance(pitEnd, rimEnd);
        SurfaceKit.Look(LevelKit.RivetIron, (pitEnd + rimEnd) * 0.5f, new Vector3(2.4f, climbLen * 0.5f, 2.4f), Quaternion.FromToRotation(Vector3.up, climb), PrimitiveType.Cylinder);
        SurfaceKit.Solid((pitEnd + rimEnd) * 0.5f, new Vector3(1.7f, 2.4f, climbLen), Quaternion.LookRotation(climb, Vector3.up));
        SurfaceKit.Look(LevelKit.RivetIron, new Vector3(0f, 1.4f, (z0 + z1) * 0.5f), new Vector3(2.4f, (z1 - z0) * 0.5f, 2.4f), Quaternion.Euler(90f, 0f, 0f), PrimitiveType.Cylinder);
        SurfaceKit.Solid(new Vector3(0f, 1.4f, (z0 + z1) * 0.5f), new Vector3(1.7f, 2.4f, z1 - z0));
        for (float z = z0 + 10f; z < z1 - 4f; z += 20f)
        {
            SurfaceKit.Block(LevelKit.Limestone, new Vector3(0f, 0.5f, z), new Vector3(3.4f, 1f, 1.6f));
            SurfaceKit.Look(LevelKit.Brass, new Vector3(0f, 1.4f, z + 10f), new Vector3(2.7f, 0.15f, 2.7f), Quaternion.Euler(90f, 0f, 0f), PrimitiveType.Cylinder);
        }
        LevelKit.BrassStrip(root, new Vector3(0f, 2.6f, z0 + 1f), new Vector3(0f, 2.6f, z1 - 1f));
        LevelKit.LineRun(root, new Vector3(0f, 2.6f, z0 + 2f), new Vector3(0f, 2.6f, z1 - 2f), Zone.Surface);
        TechMarkers.MarkLip(new Vector3(0f, 2.6f, (z0 + z1) * 0.5f), new Vector3(3f, 0.9f, z1 - z0));
        for (int i = 0; i < 4; i++)
            LevelKit.SteamVentPad(root, new Vector3((i % 2 == 0 ? -1f : 1f) * 7f, 0f, 490f + i * 30f), 6f, i * 1.2f);
        Vector3 house = new Vector3(0f, 0f, 604f);
        LevelKit.BoostCorner(root, house + Vector3.up * 2.5f, new Vector3(8f, 5f, 6f), LevelKit.Brick);
        SurfaceKit.Look(LevelKit.Limestone, house + Vector3.up * 5.25f, new Vector3(8.8f, 0.5f, 6.8f));
        SurfaceKit.Look(LevelKit.Limestone, house + Vector3.up * 5.7f, new Vector3(6.4f, 0.4f, 4.4f));
        LevelKit.HoloSign(root, house + new Vector3(0f, 3.6f, -3.04f), Vector3.back, 7f, 0.9f, "VALVE HOUSE  ·  STEAM MAINS  ·  NO ADMITTANCE", true);
        LevelKit.Pipe(root, house + new Vector3(2.4f, 5.9f, 0f), house + new Vector3(2.4f, 8.6f, 0f), 0.6f, LevelKit.RivetIron);
        LevelKit.SteamLeak(root, house + new Vector3(2.4f, 8.7f, 0f), 5f);
    }
}
