using UnityEngine;
using Plan = CityDistrict.DistrictPlan;
using Block = CityDistrict.Block;
using EK = CityDistrict.EntranceKind;

/// <summary>
/// THE THEATRE DISTRICT'S HOUSES (Level 2): the CINEMA here, the DANCE HALL (LevelKitEstablishmentsD2.cs) and the
/// BILLIARD HALL (LevelKitEstablishmentsD3.cs), each drawn apart from the generic hall. Local frame as the others:
/// x across the front, z from the front (−hd, the street it faces) to the back (+hd), y up from the street.
/// </summary>
public static partial class Establishments
{
    /// CINEMA. A picture palace sixteen metres tall under a marquee and a blade sign. The FOYER (a box office and a
    /// sweets counter as lips, globe lamps) with the GRAND STAIR up four metres to the cross-aisle at the back of
    /// the stalls (brass banisters: slides down); the AUDITORIUM a RAKE falling four metres over sixteen to the
    /// organ pit at 14° (a slide line down the centre aisle; the seat rows are 0.5 m lips to slide-hop), the ORGAN
    /// PIT (its rail and the console, lips) under the SCREEN wall (passages either side of it to the back of house,
    /// where a kick-wall ladder climbs to the roof hatch); the CIRCLE at 7.5 over the back half (a side flight up to
    /// it from the cross-aisle, a parapet lip along its front, two rows of seats, the chandelier orb over the stalls
    /// to swing from) and at its back the PROJECTION BOX a metre higher, its PORTHOLE out onto the marquee (a lip
    /// ladder from the street: lamp standard, marquee, a wall combo up the facade); the circle exit east onto a
    /// balcony over an awning; a side exit west at the rake's height. MAIN: doors → foyer → stair → the aisle → past
    /// the screen → the exit doors. FAST: the banisters, the rake, the porthole, the circle's parapet drop, the
    /// chandelier. FUEL: an usher at the doors, three footmen in the stalls, a page over the circle, a bellhop on the
    /// organ, a roster unit in the foyer.
    static void Cinema(Transform root, MovementConfig config, Plan plan, Block b, BlockFrame f)
    {
        const float h = 16f, circle = 7.5f, rakeTop = 4f;
        float hw = f.hw, hd = f.hd;
        float stairZ0 = -hd + 0.5f, stairZ1 = stairZ0 + 7.5f, aisleZ1 = stairZ1 + 2f, screenZ = hd - 5f, rakeZ1 = screenZ - 4f, circleZ1 = -hd + 16.5f, flightZ1 = -hd + 2.5f;
        float RakeY(float z) => rakeTop * (rakeZ1 - z) / (rakeZ1 - aisleZ1);
        float sideZ = aisleZ1 + 11.5f, sideY = RakeY(sideZ);
        Vector3 min = new Vector3(-hw, 0f, -hd), max = new Vector3(hw, h, hd);
        Vector2 hatch = new Vector2(hw - 3f, hd - 3f);
        Shell(root, f, h, hatch,
            Door(f, Face.South, 0f, 0f, min, max, 8f, 4.5f), Door(f, Face.North, hw * 0.35f, 0f, min, max),
            Door(f, Face.West, sideZ, sideY, min, max), Door(f, Face.East, -hd + 5f, circle, min, max, 5f, 3.5f),
            Window(f, Face.South, 0f, 2.6f, circle + 1.4f, circle + 4f, min, max));
        var zone = plan.zone;
        // THE FOYER: the grand stair (28°, eight metres wide, banisters), the box office, the sweets counter, the lamps
        Ramp(root, f, 0f, 8f, stairZ0, 0f, stairZ1, rakeTop, LevelKit.Floor, true);
        Banister(root, f, -4.7f, stairZ0, 0f, stairZ1, rakeTop);
        Banister(root, f, 4.7f, stairZ0, 0f, stairZ1, rakeTop);
        Floor(root, f, new Vector3(0f, rakeTop - 0.5f, (stairZ1 + aisleZ1) * 0.5f), new Vector3(hw * 2f, 1f, aisleZ1 - stairZ1), "CrossAisle");
        Lip(root, f, new Vector3(-hw + 2.5f, 0.65f, -hd + 3f), new Vector3(4f, 1.3f, 2f), LevelKit.WoodPanel, "BoxOffice");
        Lip(root, f, new Vector3(hw - 3f, 0.55f, -hd + 4f), new Vector3(4f, 1.1f, 1.4f), LevelKit.WoodPanel, "SweetsCounter");
        for (int s = -1; s <= 1; s += 2) Lamp(root, f, new Vector3(s * hw * 0.5f, circle - 2.2f, -hd + 4.5f), circle - 1f);
        Fuel(root, config, f, EnemyKind.Sentry, new Vector3(2.5f, 0f, -hd + 3f), "the doors");
        PoolFuel(config, f, new Vector3(R(-4f, 4f), 0f, -hd + 6f), "the foyer");
        Line(root, b, f, "the foyer", new Vector3(0f, 0f, -hd + 1f), new Vector3(0f, 0f, stairZ0 - 0.3f), zone, false);
        // THE STALLS: the rake (a slide), seven rows of seats as lips either side of the centre aisle, the side aisles clear
        var rake = Ramp(root, f, 0f, hw * 2f, aisleZ1, rakeTop, rakeZ1, 0f, LevelKit.Slope);
        rake.name = "Rake";
        float rowW = hw - 1.5f - 2.4f;
        for (int r = 0; r < 7; r++)
        {
            float z = aisleZ1 + 1.5f + r * 2.2f, yr = RakeY(z + 0.35f);
            for (int s = -1; s <= 1; s += 2) Lip(root, f, new Vector3(s * (1.5f + rowW * 0.5f), yr + 0.28f, z), new Vector3(rowW, 0.5f, 0.7f), LevelKit.Oxblood, "SeatRow");
            if (r % 2 == 1 && r < 6) Fuel(root, config, f, EnemyKind.Dummy, new Vector3((r == 1 ? -1f : 1f) * 5f, RakeY(z + 1.1f), z + 1.1f), "the stalls");
        }
        Line(root, b, f, "the rake", new Vector3(0f, rakeTop, aisleZ1 + 0.3f), new Vector3(0f, 0f, rakeZ1 - 0.3f), zone);
        // THE ORGAN PIT and THE SCREEN: the pit rail and the console (lips), the pipes, the screen wall with its
        // passages either side, the lit screen, the proscenium bands
        Lip(root, f, new Vector3(0f, 0.45f, rakeZ1 + 0.45f), new Vector3(12f, 0.9f, 0.5f), LevelKit.Limestone, "PitRail");
        Lip(root, f, new Vector3(0f, 0.6f, rakeZ1 + 2.3f), new Vector3(2.6f, 1.2f, 1.4f), LevelKit.WoodPanel, "Organ");
        for (int i = 0; i < 7; i++)
        {
            float ph = 2.4f + 0.5f * (3 - Mathf.Abs(i - 3));
            Mark(root, f, new Vector3(-3f + i, ph * 0.5f, screenZ - 0.9f), new Vector3(0.3f, ph, 0.3f), LevelKit.Bronze, "OrganPipe", PrimitiveType.Cylinder);
        }
        Fuel(root, config, f, EnemyKind.Chaser, new Vector3(0f, 1.2f, rakeZ1 + 2.3f), "the organ");
        Box(root, f, new Vector3(0f, h * 0.5f, screenZ), new Vector3(22f, h, 0.6f), LevelKit.Wall, "ScreenWall");
        Mark(root, f, new Vector3(0f, 8f, screenZ - 0.34f), new Vector3(16f, 8f, 0.06f), LevelKit.LampCream, "Screen");
        for (int s = -1; s <= 1; s += 2) Mark(root, f, new Vector3(s * 8.4f, 8f, screenZ - 0.34f), new Vector3(0.3f, 9f, 0.06f), LevelKit.Brass, "Proscenium");
        Mark(root, f, new Vector3(0f, 12.65f, screenZ - 0.34f), new Vector3(17f, 0.3f, 0.06f), LevelKit.Brass, "Proscenium");
        var screenLight = LevelKit.Light(root, f.P(0f, 6f, screenZ - 4f), 22f, 1.2f);
        screenLight.color = LevelKit.LampCreamLight;
        LevelKit.Chandelier(root, f.P(0f, 10.5f, (aisleZ1 + rakeZ1) * 0.5f + 2f), f.origin.y + h);
        for (int i = 0; i < 2; i++) Lamp(root, f, new Vector3((i == 0 ? -1f : 1f) * hw * 0.6f, h - 2.5f, (aisleZ1 + rakeZ1) * 0.5f), h);
        // THE BACK OF HOUSE: the kick-wall ladder to the roof hatch in the east corner (a boost pillar and a kick wall)
        Corner(root, f, new Vector3(hw - 3f - 1.6f, h * 0.5f - 0.03f, hd - 3f), new Vector3(0.6f, h - 0.06f, 3f), LevelKit.Charcoal);
        Kick(root, f, new Vector3(hw - 3f + 1.6f, 10f, hd - 3f), new Vector3(0.6f, 5f, 3f));
        Flute(root, f, new Vector3(hw, h - 2f, hd - 3f), Vector3.left, 4f);
        Line(root, b, f, "the hatch ladder", new Vector3(hw - 3f, 0f, screenZ + 1f), new Vector3(hw - 3f, 0f, hd - 1.2f), zone);
        CinemaCircle(root, config, plan, b, f, h, circle, rakeTop, aisleZ1, circleZ1, flightZ1);
        // The roof, the facades, the marquee, the blade sign, the balcony over its awning, the entrances
        Roof(root, f, h, hatch, 2, true);
        Facades(root, b, f, h, 2, null, null);
        LevelKit.Marquee(root, f.P(0f, 0f, -hd - 0.5f), f.D(Vector3.back), 10f, "THE RIALTO", 5f, 5.4f, 8f);   // ten wide: the elevated's middle pier clears it
        LevelKit.BladeSign(root, f.P(hw * 0.5f, h - 2f, -hd - 0.5f), f.D(Vector3.back), "RIALTO", 1.3f);
        LevelKit.Awning(root, f.P(hw + 0.5f, 0f, -hd + 5f), f.D(Vector3.right), 6f);
        Lip(root, f, new Vector3(hw + 1.1f, circle - 0.2f, -hd + 5f), new Vector3(1.2f, 0.4f, 5f), LevelKit.Limestone, "Balcony");
        Entrance(plan, b, f, "the front doors", new Vector3(0f, 0f, -hd), Vector3.back, EK.Door);
        Entrance(plan, b, f, "the exit doors", new Vector3(hw * 0.35f, 0f, hd), Vector3.forward, EK.Door);
        Entrance(plan, b, f, "the side exit", new Vector3(-hw, sideY, sideZ), Vector3.left, EK.Door);
        Entrance(plan, b, f, "the circle exit", new Vector3(hw, circle, -hd + 5f), Vector3.right, EK.Balcony, true);
        Entrance(plan, b, f, "the porthole", new Vector3(0f, circle + 1.4f, -hd), Vector3.back, EK.Window, true);
        Entrance(plan, b, f, "the roof hatch", new Vector3(hatch.x, h + 1f, hatch.y), Vector3.forward, EK.RoofHatch, true, false);
        if (string.IsNullOrEmpty(b.name)) b.name = "THE RIALTO";
    }

    /// THE CIRCLE: its floor at 7.5 over the foyer and the back of the stalls, in pieces round the side flight's
    /// slot (the flight up from the cross-aisle's west end), a parapet lip along its front, two rows of seats, and
    /// THE PROJECTION BOX at its back: a floor lip a metre up, side walls, a beam over its open side, the projectors,
    /// the PORTHOLE in the front wall ringed in brass outside (an octagon, Deco), a warm light
    static void CinemaCircle(Transform root, MovementConfig config, Plan plan, Block b, BlockFrame f, float h, float circle, float rakeTop, float aisleZ1, float circleZ1, float flightZ1)
    {
        float hw = f.hw, hd = f.hd, slotX = -hw + 3f;
        Floor(root, f, new Vector3((slotX + hw) * 0.5f, circle - 0.5f, (-hd + circleZ1) * 0.5f), new Vector3(hw - slotX, 1f, circleZ1 + hd), "Circle");
        Floor(root, f, new Vector3((-hw + slotX) * 0.5f, circle - 0.5f, (-hd + flightZ1) * 0.5f), new Vector3(slotX + hw, 1f, flightZ1 + hd), "Circle");
        Floor(root, f, new Vector3((-hw + slotX) * 0.5f, circle - 0.5f, (aisleZ1 + circleZ1) * 0.5f), new Vector3(slotX + hw, 1f, circleZ1 - aisleZ1), "Circle");
        Ramp(root, f, -hw + 1.5f, 3f, flightZ1, circle, aisleZ1, rakeTop, LevelKit.Floor, true);   // (ends in ascending z: the slab hangs under the line)
        Mark(root, f, new Vector3(slotX + 0.08f, circle + 0.5f, (aisleZ1 + flightZ1) * 0.5f), new Vector3(0.08f, 1f, aisleZ1 - flightZ1), LevelKit.Brass, "FlightRail");
        LevelKit.Parapet(root, f.P(0f, circle + 0.45f, circleZ1 - 0.35f), f.S(hw * 2f - 0.2f, 0.9f, 0.7f));
        float rowW = hw - 1.5f - 2.4f;
        for (int r = 0; r < 2; r++)
            for (int s = -1; s <= 1; s += 2)
                Lip(root, f, new Vector3(s * (1.5f + rowW * 0.5f), circle + 0.25f, circleZ1 - 2.2f - r * 2.2f), new Vector3(rowW, 0.5f, 0.7f), LevelKit.Oxblood, "SeatRow");
        Fuel(root, config, f, EnemyKind.Turret, new Vector3(0f, circle + 2.2f, circleZ1 - 6f), "the circle");
        Line(root, b, f, "the circle", new Vector3(0f, circle, -hd + 1f), new Vector3(0f, circle, circleZ1 - 1.2f), plan.zone);
        // The projection box
        float bz = -hd + 2f;
        Lip(root, f, new Vector3(0f, circle + 0.5f, bz), new Vector3(9f, 1f, 4f), LevelKit.Charcoal, "ProjectionFloor");
        for (int s = -1; s <= 1; s += 2) Box(root, f, new Vector3(s * 4.7f, circle + 2.75f, bz), new Vector3(0.4f, 3.5f, 4f), LevelKit.Wall, "ProjectionWall");
        Box(root, f, new Vector3(0f, circle + 4f, bz + 2.2f), new Vector3(9.8f, 1f, 0.4f), LevelKit.Wall, "ProjectionBeam");
        Box(root, f, new Vector3(0f, circle + 4.75f, bz + 0.2f), new Vector3(9.8f, 0.5f, 4.4f), LevelKit.Charcoal, "ProjectionCeiling");
        for (int s = -1; s <= 1; s += 2)
        {
            Mark(root, f, new Vector3(s * 1.6f, circle + 1.4f, bz + 1.4f), new Vector3(0.2f, 0.8f, 0.2f), LevelKit.RivetIron, "ProjectorStand");
            Mark(root, f, new Vector3(s * 1.6f, circle + 2.15f, bz + 1.6f), new Vector3(0.5f, 0.7f, 1.1f), LevelKit.RivetIron, "Projector");
        }
        var l = LevelKit.Light(root, f.P(0f, circle + 3.2f, bz), 10f, 1.4f);
        l.color = new Color(1f, 0.72f, 0.38f);
        // The porthole's brass octagon on the facade
        Vector3 c = f.P(0f, circle + 2.7f, -hd - 0.1f);
        for (int k = 0; k < 8; k++)
        {
            float a = k * 45f * Mathf.Deg2Rad;
            Vector3 at = c + f.D(Vector3.right) * (1.65f * Mathf.Cos(a)) + Vector3.up * (1.65f * Mathf.Sin(a));
            LevelKit.Prim(root, at, new Vector3(1.4f, 0.18f, 0.1f), LevelKit.Brass, "PortholeRing", PrimitiveType.Cube, f.Q * Quaternion.Euler(0f, 0f, k * 45f + 90f));
        }
    }
}
