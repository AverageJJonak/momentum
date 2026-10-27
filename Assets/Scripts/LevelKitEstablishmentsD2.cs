using UnityEngine;
using Plan = CityDistrict.DistrictPlan;
using Block = CityDistrict.Block;
using EK = CityDistrict.EntranceKind;

/// <summary>
/// THE THEATRE DISTRICT'S HOUSES (Level 2): the DANCE HALL.
/// </summary>
public static partial class Establishments
{
    /// DANCE HALL. THE PALAIS, twelve metres tall. A VESTIBULE (the paybox lip) under a marquee, then the HALL: the
    /// SPRUNG FLOOR (walnut, brass-bordered, five centimetres proud), the BAR along the west wall (a 1.1 m lip), the
    /// BAND STAND across the back (a 1.2 m lip under a brass sunburst), the BALCONY RING at 4.6 round all four sides
    /// with parapet lips along its inner edges (the lazy way up a flight from the vestibule along the east wall; the
    /// fast ways a corner boost on the bar, the band stand, or a pad on the floor; a grapple orb over the floor to
    /// swing from), the MIRROR BALL hung over the floor — a glass cube round a cyan core: a cut target, breakable.
    /// Behind the band stand the ANNEX: the band room (the stage door into the alley, a pad under the WELL) with the
    /// CLOAKROOM over it at 4.6 (its WINDOW over the alley, a brick sill lip under it outside; from the ring through
    /// the upper opening), the well a kick-wall ladder to the ROOF TERRACE (a pergola lip, benches, the parapets).
    /// MAIN: doors → vestibule → the floor → over the band stand → the band room → the stage door. FAST: the ring and
    /// its lips, the marquee window out, the window over the alley in, the well to the roof.
    /// FUEL: a doorman at the doors, footmen on the ring, a bellhop on the band stand, a page under the mirror ball,
    /// a roster unit on the floor.
    static void DanceHall(Transform root, MovementConfig config, Plan plan, Block b, BlockFrame f)
    {
        const float h = 12f, ring = 4.6f, ringW = 3.2f;
        float hw = f.hw, hd = f.hd, hallZ0 = -hd + 6f, hallZ1 = hd - 6f, flightZ0 = -hd + 1.5f, flightZ1 = -hd + 10.5f, wellX = hw - 3.2f;
        Vector3 min = new Vector3(-hw, 0f, -hd), max = new Vector3(hw, h, hd);
        Vector2 hatch = new Vector2(hw - 1.6f, hd - 3f);
        Shell(root, f, h, hatch,
            Door(f, Face.South, 0f, 0f, min, max, 8f, 4.5f), Window(f, Face.South, 0f, 4f, ring + 0.2f, ring + 3f, min, max),
            Door(f, Face.West, -hd + 3f, 0f, min, max), Door(f, Face.North, -hw * 0.4f, 0f, min, max),
            Window(f, Face.North, 0f, 3.4f, ring + 0.2f, ring + 3.2f, min, max));
        var zone = plan.zone;
        // THE VESTIBULE: the paybox, the flight up to the ring along the east wall (27°, nosed), the lamps
        Lip(root, f, new Vector3(-hw + 7f, 0.65f, -hd + 3f), new Vector3(3f, 1.3f, 2f), LevelKit.WoodPanel, "Paybox");   // clear of the side door
        Ramp(root, f, hw - 1.6f, 3f, flightZ0, 0f, flightZ1, ring, LevelKit.Floor, true);
        Mark(root, f, new Vector3(hw - 3.2f, 2.8f, (flightZ0 + flightZ1) * 0.5f), new Vector3(0.08f, 1f, flightZ1 - flightZ0), LevelKit.Brass, "FlightRail");
        Fuel(root, config, f, EnemyKind.Sentry, new Vector3(2.5f, 0f, -hd + 3f), "the doors");
        Lamp(root, f, new Vector3(-hw * 0.3f, ring - 1.6f, -hd + 3f), ring - 1f);
        Line(root, b, f, "the floor", new Vector3(0f, 0f, -hd + 1f), new Vector3(0f, 0f, hallZ0 - 0.4f), zone, false);
        Line(root, b, f, "the flight", new Vector3(hw - 1.6f, 0f, -hd + 0.6f), new Vector3(hw - 1.6f, 0f, flightZ0 - 0.3f), zone);
        // THE HALL: the sprung floor with its brass border, the bar, the band stand and its sunburst, the orb, the mirror ball
        float fw = hw * 2f - ringW * 2f - 0.4f, fd = hallZ1 - hallZ0 - ringW * 2f - 0.4f, fz = (hallZ0 + hallZ1) * 0.5f;
        LevelKit.Slab(root, f.P(0f, 0.025f, fz), f.S(fw, 0.05f, fd), LevelKit.WoodPanel, "SprungFloor");
        for (int s = -1; s <= 1; s += 2)
        {
            LevelKit.BrassStrip(root, f.P(s * (fw * 0.5f - 0.2f), 0.05f, fz - fd * 0.5f + 0.2f), f.P(s * (fw * 0.5f - 0.2f), 0.05f, fz + fd * 0.5f - 0.2f), 0.14f);
            LevelKit.BrassStrip(root, f.P(-fw * 0.5f + 0.2f, 0.05f, fz + s * (fd * 0.5f - 0.2f)), f.P(fw * 0.5f - 0.2f, 0.05f, fz + s * (fd * 0.5f - 0.2f)), 0.14f);
        }
        Lip(root, f, new Vector3(-hw + 2.4f, 0.55f, fz), new Vector3(1.4f, 1.1f, 14f), LevelKit.WoodPanel, "Bar");
        for (int i = 0; i < 6; i++) Mark(root, f, new Vector3(-hw + 0.25f, 1.6f + (i % 2) * 0.6f, fz - 5f + i * 2f), new Vector3(0.3f, 0.5f, 1.6f), LevelKit.Pane, "Bottles");
        Lip(root, f, new Vector3(0f, 0.6f, hallZ1 - 1.5f), new Vector3(12f, 1.2f, 3f), LevelKit.WoodPanel, "BandStand");
        for (int i = -3; i <= 3; i++)
            LevelKit.Prim(root, f.P(0f, 3.4f, hallZ1 - 0.3f) + (f.Q * Quaternion.Euler(0f, 0f, i * 20f)) * Vector3.up * 1.6f, new Vector3(0.1f, 3.2f, 0.05f), LevelKit.Brass, "SunburstRay", PrimitiveType.Cube, f.Q * Quaternion.Euler(0f, 0f, i * 20f));
        LevelKit.Orb(root, f.P(0f, 7.6f, fz + 3f));
        LevelKit.Glass(root, f.P(0f, 8.5f, fz - 1f), new Vector3(1.4f, 1.4f, 1.4f), config);
        Mark(root, f, new Vector3(0f, 8.5f, fz - 1f), new Vector3(0.8f, 0.8f, 0.8f), LevelKit.NeonCyan, "MirrorBallCore", PrimitiveType.Sphere);
        Mark(root, f, new Vector3(0f, 10.6f, fz - 1f), new Vector3(0.05f, 2.8f, 0.05f), LevelKit.Bronze, "BallChain", PrimitiveType.Cylinder);
        var l = LevelKit.Light(root, f.P(0f, 7.6f, fz - 1f), 18f, 1.6f);
        l.color = LevelKit.LampCreamLight;
        Pad(root, f, new Vector3(hw - 5f, 0f, hallZ1 - 5f), Mathf.Sqrt(2f * 25f * (ring + 1.2f)), 1f);
        Fuel(root, config, f, EnemyKind.Chaser, new Vector3(0f, 1.2f, hallZ1 - 1.5f), "the band stand");
        Fuel(root, config, f, EnemyKind.Turret, new Vector3(0f, 5f, fz - 1f), "under the mirror ball");
        PoolFuel(config, f, new Vector3(R(-3f, 3f), 0.05f, fz), "the floor");
        Line(root, b, f, "over the band stand", new Vector3(0f, 0.05f, hallZ1 - 6.5f), new Vector3(0f, 0.05f, hallZ1 - 3.3f), zone);
        Line(root, b, f, "over the bar", new Vector3(-hw + 6f, 0.05f, fz - 6f), new Vector3(-hw + 3.6f, 0.05f, fz - 2f), zone);
        DanceHallRing(root, config, plan, b, f, ring, ringW, hallZ0, hallZ1, flightZ1);
        DanceHallAnnex(root, config, plan, b, f, h, ring, hallZ1, wellX);
        // The roof terrace: parapets and a stair head (Roof), the pergola lip, benches, a hedge
        Roof(root, f, h, hatch, 1, false);
        Lip(root, f, new Vector3(-hw * 0.4f, h + 1.6f, -hd * 0.3f), new Vector3(7f, 1.2f, 3f), LevelKit.WoodPanel, "Pergola");
        LevelKit.Hedge(root, f.P(hw * 0.3f, h + 1f, -hd * 0.3f), f.S(6f, 0.9f, 1f));
        LevelKit.Bench(root, f.P(0f, h + 1f, hd * 0.2f), f.Yaw);
        LevelKit.Bench(root, f.P(-hw * 0.5f, h + 1f, hd * 0.2f), f.Yaw);
        Facades(root, b, f, h, 2, null, null);
        LevelKit.Marquee(root, f.P(0f, 0f, -hd - 0.5f), f.D(Vector3.back), 8f, "THE PALAIS", 5f, 4.5f);
        LevelKit.BladeSign(root, f.P(-hw * 0.5f, h - 2f, -hd - 0.5f), f.D(Vector3.back), "PALAIS", 1.3f);
        Entrance(plan, b, f, "the front doors", new Vector3(0f, 0f, -hd), Vector3.back, EK.Door);
        Entrance(plan, b, f, "the marquee window", new Vector3(0f, ring + 0.2f, -hd), Vector3.back, EK.Marquee, true);
        Entrance(plan, b, f, "the side door", new Vector3(-hw, 0f, -hd + 3f), Vector3.left, EK.Door);
        Entrance(plan, b, f, "the stage door", new Vector3(-hw * 0.4f, 0f, hd), Vector3.forward, EK.Door);
        Entrance(plan, b, f, "the cloakroom window", new Vector3(0f, ring + 0.2f, hd), Vector3.forward, EK.Window, true);
        Entrance(plan, b, f, "the roof terrace", new Vector3(hatch.x, h + 1f, hatch.y), Vector3.forward, EK.RoofHatch, true, false);
        if (string.IsNullOrEmpty(b.name)) b.name = "THE PALAIS";
    }

    /// THE BALCONY RING at `ring`: the west side the hall's length, the east side from the flight's top, the front
    /// over the vestibule, the back over the band stand; parapet lips along every inner edge; footmen on it
    static void DanceHallRing(Transform root, MovementConfig config, Plan plan, Block b, BlockFrame f, float ring, float ringW, float hallZ0, float hallZ1, float flightZ1)
    {
        float hw = f.hw, hd = f.hd, y = ring - 0.5f;
        Floor(root, f, new Vector3(-hw + ringW * 0.5f, y, (hallZ0 + hallZ1) * 0.5f), new Vector3(ringW, 1f, hallZ1 - hallZ0), "Ring");
        Floor(root, f, new Vector3(hw - ringW * 0.5f, y, (flightZ1 + hallZ1) * 0.5f), new Vector3(ringW, 1f, hallZ1 - flightZ1), "Ring");
        Floor(root, f, new Vector3(-ringW * 0.5f, y, (-hd + hallZ0 + ringW) * 0.5f), new Vector3(hw * 2f - ringW, 1f, hallZ0 + ringW + hd), "Ring");
        Floor(root, f, new Vector3(0f, y, hallZ1 - ringW * 0.5f), new Vector3(hw * 2f - ringW * 2f, 1f, ringW), "Ring");
        float inner = hw - ringW;
        LevelKit.Parapet(root, f.P(-inner - 0.35f, ring + 0.45f, (hallZ0 + ringW + hallZ1 - ringW) * 0.5f), f.S(0.7f, 0.9f, hallZ1 - hallZ0 - ringW * 2f));
        LevelKit.Parapet(root, f.P(inner - 0.35f, ring + 0.45f, (flightZ1 + hallZ1 - ringW) * 0.5f), f.S(0.7f, 0.9f, hallZ1 - ringW - flightZ1));
        LevelKit.Parapet(root, f.P(0f, ring + 0.45f, hallZ0 + ringW - 0.35f), f.S(inner * 2f - 1.4f, 0.9f, 0.7f));
        LevelKit.Parapet(root, f.P(0f, ring + 0.45f, hallZ1 - ringW + 0.35f), f.S(inner * 2f - 1.4f, 0.9f, 0.7f));
        Fuel(root, config, f, EnemyKind.Dummy, new Vector3(-hw + 1.6f, ring, hallZ0 + 8f), "the ring");
        Fuel(root, config, f, EnemyKind.Dummy, new Vector3(hw - 1.6f, ring, hallZ1 - 6f), "the ring");
        Line(root, b, f, "the ring", new Vector3(-hw + 1.6f, ring, hallZ0 + 1f), new Vector3(-hw + 1.6f, ring, hallZ1 - 1f), plan.zone);
    }

    /// THE ANNEX behind the band stand: the partition with the band room's opening west and the cloakroom's opening
    /// over the ring's back, the band room (the pad under the well), the cloakroom floor at the ring's height with
    /// the well slot along the east wall (a boost pillar at its edge, the wall fluted: the ladder to the hatch),
    /// the counter, the coat rails, the sill lip under the window outside
    static void DanceHallAnnex(Transform root, MovementConfig config, Plan plan, Block b, BlockFrame f, float h, float ring, float pz, float wellX)
    {
        float hw = f.hw, hd = f.hd, gx = -hw + 4f, ux0 = 3f, ux1 = 9.5f;
        Box(root, f, new Vector3(-hw + 0.375f, h * 0.5f, pz), new Vector3(0.75f, h, 0.5f), LevelKit.Wall, "Partition");
        Box(root, f, new Vector3(gx, (h + 4.25f) * 0.5f, pz), new Vector3(6.5f, h - 4.25f, 0.5f), LevelKit.Wall, "Partition");
        Box(root, f, new Vector3((gx + 3.25f + ux0) * 0.5f, h * 0.5f, pz), new Vector3(ux0 - gx - 3.25f, h, 0.5f), LevelKit.Wall, "Partition");
        Box(root, f, new Vector3((ux0 + ux1) * 0.5f, ring * 0.5f, pz), new Vector3(ux1 - ux0, ring, 0.5f), LevelKit.Wall, "Partition");
        Box(root, f, new Vector3((ux0 + ux1) * 0.5f, (ring + 3.4f + h) * 0.5f, pz), new Vector3(ux1 - ux0, h - ring - 3.4f, 0.5f), LevelKit.Wall, "Partition");
        Box(root, f, new Vector3((ux1 + hw) * 0.5f, h * 0.5f, pz), new Vector3(hw - ux1, h, 0.5f), LevelKit.Wall, "Partition");
        Mark(root, f, new Vector3((ux0 + ux1) * 0.5f, ring + 3.4f, pz), new Vector3(ux1 - ux0 + 0.4f, 0.3f, 0.7f), LevelKit.Brass, "Frame");
        // The cloakroom floor (the well slot left along the east wall), the counter, the coat rails, the lamp
        Floor(root, f, new Vector3((-hw + wellX) * 0.5f, ring - 0.5f, (pz + hd) * 0.5f), new Vector3(wellX + hw, 1f, hd - pz), "Cloakroom");
        Lip(root, f, new Vector3(-hw * 0.3f, ring + 0.55f, pz + 1.3f), new Vector3(hw * 0.8f, 1.1f, 0.8f), LevelKit.WoodPanel, "CloakCounter");
        for (int i = 0; i < 3; i++) Mark(root, f, new Vector3(-hw + 3f + i * 4f, ring + 1.9f, hd - 1.2f), new Vector3(3.2f, 0.06f, 0.06f), LevelKit.Brass, "CoatRail");
        Lamp(root, f, new Vector3(-hw * 0.3f, h - 1.4f, (pz + hd) * 0.5f), h);
        Fuel(root, config, f, EnemyKind.Dummy, new Vector3(-hw * 0.3f, ring, hd - 2.5f), "the cloakroom");
        // The well: the pillar at the floor's edge, the east wall fluted, the pad in the band room under it
        Corner(root, f, new Vector3(wellX - 0.3f, h * 0.5f - 0.03f, hd - 3f), new Vector3(0.6f, h - 0.06f, 3f), LevelKit.Charcoal);
        Flute(root, f, new Vector3(hw, h - 4f, hd - 3f), Vector3.left, 6f);
        Pad(root, f, new Vector3(hw - 1.6f, 0f, hd - 3f), Mathf.Sqrt(2f * 25f * (ring + 1.2f)), 0.5f);
        Line(root, b, f, "the well", new Vector3(hw - 1.6f, 0f, pz + 0.8f), new Vector3(hw - 1.6f, 0f, hd - 4.2f), plan.zone);
        Fuel(root, config, f, EnemyKind.Warden, new Vector3(-hw * 0.4f, 0f, hd - 3f), "the stage door");
        // The sill under the cloakroom window, in the alley
        Lip(root, f, new Vector3(0f, ring - 0.2f, hd + 1.1f), new Vector3(4f, 0.4f, 1.2f), LevelKit.Brick, "Sill");
    }
}
