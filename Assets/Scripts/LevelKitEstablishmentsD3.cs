using UnityEngine;
using Plan = CityDistrict.DistrictPlan;
using Block = CityDistrict.Block;
using EK = CityDistrict.EntranceKind;

/// <summary>
/// THE THEATRE DISTRICT'S HOUSES (Level 2): the BILLIARD HALL.
/// </summary>
public static partial class Establishments
{
    /// The dispatcher lays every house's block floor before its builder runs; a house that digs a cellar takes it
    /// back (the slab named for it at the block's centre) and lays its own pieces round the holes. True if it was there
    /// The billiard hall DIGS (Establishments.Digs): the dispatcher lays it no block floor, so the cellar is always cut and
    /// the ground floor laid here in pieces. Always true; kept as the switch the floor code reads
    static bool TakeBlockFloor(Transform root, BlockFrame f) => true;

    /// BILLIARD HALL. THE CAROM ROOMS, nine metres tall over a CELLAR. The front desk (a lip) inside the doors, then
    /// the LONG ROOM: ten tables in two columns down it, each a 0.9 m lip three metres long with a 2.2 m gap to the
    /// next (a hop chain down either column at speed; the centre aisle the main line), the REFEREES' GALLERY at 3 m
    /// along the east wall (a flight up to it from the desk, a parapet lip, its window out over an awning), the
    /// CUE-RACK WALL west fluted as a kick wall with a boost pillar 3.5 m off it: the ladder to the SKYLIGHT in the
    /// roof. The TRAPDOOR in the east aisle drops six metres into THE CELLAR BAR (a pad under it throws you back
    /// up): the bar along its east wall, tables, a chandelier orb, and THE AREA STEPS up along the west wall and out
    /// of the back wall onto the street behind (the cellar's own door). MAIN: doors → the aisle → the back door.
    /// FAST: the table chain, the gallery, the cue wall to the roof, the trapdoor, the area steps.
    /// FUEL: a marker at the desk, players on the tables, a referee on the gallery, a barman and drinkers below.
    static void BilliardHall(Transform root, MovementConfig config, Plan plan, Block b, BlockFrame f)
    {
        const float h = 9f, gallery = 3f, galleryW = 3.2f, cellar = -6f;
        float hw = f.hw, hd = f.hd, bhw = f.blockHW, bhd = f.blockHD;
        float roomZ0 = -hd + 5f, flightZ1 = -hd + 8f, pillarX = -hw + 3.8f;
        float trapX0 = 7.8f, trapX1 = 10.8f, trapZ0 = 2.5f, trapZ1 = 5.5f;                 // the trapdoor, in the east aisle
        float stairX0 = -hw + 0.5f, stairX1 = -hw + 3.5f, stairZ0 = hd - 14f, stairZ1 = hd - 2f;   // the area steps' slot, along the west wall
        Vector3 min = new Vector3(-hw, 0f, -hd), max = new Vector3(hw, h, hd);
        Vector2 hatch = new Vector2(-hw + 1.75f, hd - 5f);
        bool dig = TakeBlockFloor(root, f);
        Shell(root, f, h, hatch,
            Door(f, Face.South, 0f, 0f, min, max), Door(f, Face.North, hw * 0.4f, 0f, min, max), Door(f, Face.West, -hd * 0.3f, 0f, min, max),
            dig ? Door(f, Face.North, stairX0 + 1.5f, 0f, min, max, 3.6f, 3.2f) : Door(f, Face.North, hw * 0.4f, 0f, min, max),
            Window(f, Face.East, 0f, 3.4f, gallery + 0.2f, gallery + 3.2f, min, max));
        var zone = plan.zone;
        if (dig)
        {
            // The ground floor in pieces round the stair's slot and the trapdoor, then the cellar under it all
            Floor(root, f, new Vector3((-bhw + stairX0) * 0.5f, -0.5f, 0f), new Vector3(stairX0 + bhw, 1f, bhd * 2f), "BlockFloor");
            Floor(root, f, new Vector3((stairX0 + stairX1) * 0.5f, -0.5f, (-bhd + stairZ0) * 0.5f), new Vector3(stairX1 - stairX0, 1f, stairZ0 + bhd), "BlockFloor");
            Floor(root, f, new Vector3((stairX0 + stairX1) * 0.5f, -0.5f, (stairZ1 + bhd) * 0.5f), new Vector3(stairX1 - stairX0, 1f, bhd - stairZ1), "BlockFloor");
            Floor(root, f, new Vector3((stairX1 + trapX0) * 0.5f, -0.5f, 0f), new Vector3(trapX0 - stairX1, 1f, bhd * 2f), "BlockFloor");
            Floor(root, f, new Vector3((trapX0 + trapX1) * 0.5f, -0.5f, (-bhd + trapZ0) * 0.5f), new Vector3(trapX1 - trapX0, 1f, trapZ0 + bhd), "BlockFloor");
            Floor(root, f, new Vector3((trapX0 + trapX1) * 0.5f, -0.5f, (trapZ1 + bhd) * 0.5f), new Vector3(trapX1 - trapX0, 1f, bhd - trapZ1), "BlockFloor");
            Floor(root, f, new Vector3((trapX1 + bhw) * 0.5f, -0.5f, 0f), new Vector3(bhw - trapX1, 1f, bhd * 2f), "BlockFloor");
            Mark(root, f, new Vector3((trapX0 + trapX1) * 0.5f, 0.02f, trapZ0 - 0.3f), new Vector3(3.4f, 0.04f, 0.3f), LevelKit.Brass, "TrapFrame");
            Mark(root, f, new Vector3((trapX0 + trapX1) * 0.5f, 0.02f, trapZ1 + 0.3f), new Vector3(3.4f, 0.04f, 0.3f), LevelKit.Brass, "TrapFrame");
            BilliardCellar(root, config, plan, b, f, cellar, trapX0, trapX1, trapZ0, trapZ1, stairX0, stairX1, stairZ0, stairZ1);
        }
        // The desk, the flight up to the gallery, the gallery itself with its parapet and the referee's chair
        Lip(root, f, new Vector3(-hw * 0.3f, 0.55f, -hd + 3f), new Vector3(6f, 1.1f, 1.2f), LevelKit.WoodPanel, "Desk");
        Fuel(root, config, f, EnemyKind.Sentry, new Vector3(2.5f, 0f, -hd + 3f), "the desk");
        Ramp(root, f, hw - 1.6f, 3f, -hd + 1f, 0f, flightZ1, gallery, LevelKit.Floor, true);
        Floor(root, f, new Vector3(hw - galleryW * 0.5f, gallery - 0.5f, (flightZ1 + hd) * 0.5f), new Vector3(galleryW, 1f, hd - flightZ1), "Gallery");
        LevelKit.Parapet(root, f.P(hw - galleryW - 0.35f, gallery + 0.45f, (flightZ1 + hd) * 0.5f + 0.6f), f.S(0.7f, 0.9f, hd - flightZ1 - 1.2f));
        Lip(root, f, new Vector3(hw - 1.4f, gallery + 0.6f, 6f), new Vector3(1.2f, 1.2f, 1.2f), LevelKit.WoodPanel, "RefereeChair");
        Fuel(root, config, f, EnemyKind.Chaser, new Vector3(hw - 1.6f, gallery, 0f), "the referees' gallery");
        Line(root, b, f, "the gallery", new Vector3(hw - 1.6f, 0f, -hd + 0.5f), new Vector3(hw - 1.6f, 0f, -hd + 1.8f), zone);
        // THE TABLES: two columns of five, 0.9 m lips three metres long, 2.2 m apart; cues and balls as looks
        for (int c = -1; c <= 1; c += 2)
            for (int r = 0; r < 5; r++)
            {
                float z = roomZ0 + 3.5f + r * 5.2f;
                Lip(root, f, new Vector3(c * 5.5f, 0.45f, z), new Vector3(1.7f, 0.9f, 3f), LevelKit.WoodPanel, "Table");
                Mark(root, f, new Vector3(c * 5.5f, 0.92f, z), new Vector3(1.5f, 0.02f, 2.8f), LevelKit.Charcoal, "Baize");
                if (r % 2 == 0) Mark(root, f, new Vector3(c * 5.5f + 0.3f, 0.96f, z - 0.6f + r * 0.3f), new Vector3(0.12f, 0.12f, 0.12f), LevelKit.Brass, "Ball", PrimitiveType.Sphere);
                if (r == 1 || r == 3) Fuel(root, config, f, EnemyKind.Dummy, new Vector3(c * 5.5f, 0.9f, z), "the tables");
                Lamp(root, f, new Vector3(c * 5.5f, 3.2f, z), h);
            }
        PoolFuel(config, f, new Vector3(R(-2f, 2f), 0f, roomZ0 + 6f), "the aisle");
        Line(root, b, f, "the aisle", new Vector3(0f, 0f, -hd + 1f), new Vector3(0f, 0f, roomZ0 + 1f), zone, false);
        for (int c = -1; c <= 1; c += 2) Line(root, b, f, c < 0 ? "the west tables" : "the east tables", new Vector3(c * 5.5f, 0f, roomZ0 - 0.5f), new Vector3(c * 5.5f, 0f, roomZ0 + 1.8f), zone);
        // THE CUE-RACK WALL: fluted (a kick wall), the racks as looks, the boost pillar off it, the ladder to the skylight
        for (int i = 0; i < 3; i++)
        {
            float rz = i == 0 ? -13f : i == 1 ? 1f : 10f;   // the racks clear of the side door
            Flute(root, f, new Vector3(-hw, h * 0.5f, -10f + i * 9.5f), Vector3.right, h - 2f, 0.6f);
            Mark(root, f, new Vector3(-hw + 0.15f, 1.5f, rz), new Vector3(0.1f, 0.08f, 4f), LevelKit.Brass, "CueRack");
            for (int k = 0; k < 6; k++) Mark(root, f, new Vector3(-hw + 0.16f, 1.1f, rz - 1.7f + k * 0.65f), new Vector3(0.03f, 1.5f, 0.03f), LevelKit.WoodPanel, "Cue", PrimitiveType.Cylinder);
        }
        Corner(root, f, new Vector3(pillarX, h * 0.5f - 0.03f, hd - 5f), new Vector3(0.6f, h - 0.06f, 3f), LevelKit.Charcoal);
        Line(root, b, f, "the cue wall", new Vector3(-hw + 1.5f, 0f, -hd + 6f), new Vector3(-hw + 1.5f, 0f, hd - 7.5f), zone);
        // The roof, the facades, the sign; the awning under the gallery window; the entrances
        Roof(root, f, h, hatch, 2, true);
        Facades(root, b, f, h, 1, "THE CAROM ROOMS", new Vector2(-9f, 6f));
        LevelKit.BladeSign(root, f.P(hw * 0.65f, h - 1f, -hd - 0.5f), f.D(Vector3.back), "CAROM", 1.0f);
        LevelKit.Awning(root, f.P(hw + 0.5f, 0f, 0f), f.D(Vector3.right), 6f);
        Entrance(plan, b, f, "the front doors", new Vector3(0f, 0f, -hd), Vector3.back, EK.Door);
        Entrance(plan, b, f, "the back door", new Vector3(hw * 0.4f, 0f, hd), Vector3.forward, EK.Door);
        Entrance(plan, b, f, "the side door", new Vector3(-hw, 0f, -hd * 0.3f), Vector3.left, EK.Door);
        if (dig) Entrance(plan, b, f, "the area steps", new Vector3(stairX0 + 1.5f, 0f, hd), Vector3.forward, EK.CellarHatch);
        Entrance(plan, b, f, "the gallery window", new Vector3(hw, gallery + 0.2f, 0f), Vector3.right, EK.Window, true);
        Entrance(plan, b, f, "the skylight", new Vector3(hatch.x, h + 1f, hatch.y), Vector3.back, EK.Skylight, true);
        if (string.IsNullOrEmpty(b.name)) b.name = "THE CAROM ROOMS";
    }

    /// THE CELLAR BAR under the hall: its own walls and floor, five metres of headroom; the bar along the east wall,
    /// four tables, the chandelier orb, lamps; the pad under the trapdoor (up and out again); the area steps up the
    /// west wall with a banister, out of the back wall at street level
    static void BilliardCellar(Transform root, MovementConfig config, Plan plan, Block b, BlockFrame f, float cellar, float tx0, float tx1, float tz0, float tz1, float sx0, float sx1, float sz0, float sz1)
    {
        float hw = f.hw, hd = f.hd;
        Room(root, f, new Vector3(-hw, cellar, -hd), new Vector3(hw, -1f, hd), LevelKit.Faces.North | LevelKit.Faces.South | LevelKit.Faces.East | LevelKit.Faces.West | LevelKit.Faces.Floor);
        Lip(root, f, new Vector3(hw - 2.4f, cellar + 0.55f, -2f), new Vector3(1.4f, 1.1f, 14f), LevelKit.WoodPanel, "Bar");
        Fuel(root, config, f, EnemyKind.Sentry, new Vector3(hw - 1.2f, cellar, -2f), "behind the bar");
        for (int i = 0; i < 4; i++)
        {
            Vector3 t = new Vector3(-hw * 0.3f + (i % 2) * hw * 0.45f, cellar + 0.45f, -hd * 0.5f + (i / 2) * hd * 0.6f);
            Lip(root, f, t, new Vector3(1.8f, 0.9f, 1.8f), LevelKit.WoodPanel, "Table");
            if (i < 2) Fuel(root, config, f, EnemyKind.Dummy, t + Vector3.up * 0.45f + Vector3.forward * 1.6f, "the cellar tables");
        }
        LevelKit.Chandelier(root, f.P(0f, cellar + 3.6f, 0f), f.origin.y - 1f, 2f);
        Lamp(root, f, new Vector3(-hw * 0.5f, cellar + 4.2f, hd * 0.5f), -1f);
        Pad(root, f, new Vector3((tx0 + tx1) * 0.5f, cellar, (tz0 + tz1) * 0.5f), Mathf.Sqrt(2f * 25f * (1.2f - cellar)), 0.3f);
        Fuel(root, config, f, EnemyKind.Turret, new Vector3((sx0 + sx1) * 0.5f + 3f, cellar + 3.5f, sz0 - 2f), "over the area steps");
        // The area steps: a 27° flight up the west wall from the cellar floor to the street door in the back wall
        Ramp(root, f, (sx0 + sx1) * 0.5f, sx1 - sx0 - 0.2f, sz0, cellar, sz1, 0f, LevelKit.Floor, true);
        Banister(root, f, sx1 + 0.7f, sz0, cellar, sz1, 0f);
        Line(root, b, f, "the area steps", new Vector3((sx0 + sx1) * 0.5f, cellar, sz0 - 6f), new Vector3((sx0 + sx1) * 0.5f, cellar, sz0 - 0.4f), plan.zone, false);
        Line(root, b, f, "the trapdoor", new Vector3((tx0 + tx1) * 0.5f, 0f, tz0 - 4f), new Vector3((tx0 + tx1) * 0.5f, 0f, tz0 - 0.4f), plan.zone);
    }
}
