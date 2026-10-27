using UnityEngine;
using Plan = CityDistrict.DistrictPlan;
using Block = CityDistrict.Block;
using EK = CityDistrict.EntranceKind;

/// <summary>
/// HALCYON HOUSE'S OWN BLOCK (Level 5): THE REGISTRY. Local frame as the Arcade's (LevelKitEstablishmentsG.cs).
/// </summary>
public static partial class Establishments
{
    /// THE REGISTRY (h 12): the House's registry of guests. The doors under the clock; THE HALL OF LEDGERS — eight rows
    /// of 2.4 m LEDGER STACKS (lanes between them, every stack a kick wall with authored corners, a cross lane through
    /// the middle), the central lane kept clear for the main line; THE COUNTER, a 1.1 m lip the width of the hall
    /// (hop it); FILING GALLERIES at 4.6 along both walls (a plinth lip before each: the corner boost) and at 9 (the
    /// east one from the lift, a cross gallery along the front joining it to the west); THE RECORDS LIFT, a shaft of
    /// two boost pillars with rest lips (a kick-wall ladder) to the roof hatch, a pad at its foot for the slow way;
    /// PNEUMATIC TUBES along the ceiling down to stations behind the counter (detail); the back door onto Directors'
    /// Row, TO THE VAULTS over it; the FILING WINDOW onto the west street at 4.6 over an awning (tech).
    /// MAIN: doors → the central lane → over the counter → the back door. FAST: the stacks' lanes, the galleries,
    /// the lift. FUEL: a doorman at the doors, footmen in the stacks, a page over the counter, a bellhop behind it,
    /// a roster unit in the lanes.
    static void Registry(Transform root, MovementConfig config, Plan plan, Block b, BlockFrame f)
    {
        const float h = 12f, g1 = 4.6f, g2 = 9f;
        float hw = f.hw, hd = f.hd, counterZ = hd - 8f, winZ = -6f, gz0 = -hd + 8.5f;
        Vector3 min = new Vector3(-hw, 0f, -hd), max = new Vector3(hw, h, hd);
        Vector2 hatch = new Vector2(hw - 3f, hd - 3f);
        var zone = plan.zone;
        Shell(root, f, h, hatch, Door(f, Face.South, 0f, 0f, min, max, 8f, 5f), Door(f, Face.North, 0f, 0f, min, max),
            Window(f, Face.West, winZ, 3.2f, g1, g1 + 3.2f, min, max));
        // THE LEDGER STACKS: eight rows in two groups with a cross lane between; footmen in the lanes
        for (int i = 0; i < 4; i++)
            for (int s = -1; s <= 1; s += 2)
            {
                float x = s * (4.5f + i * 3.5f);
                HouseKit.Stack(root, f.P(x, 1.2f, -7.5f), f.S(0.6f, 2.4f, 8f));
                HouseKit.Stack(root, f.P(x, 1.2f, 2.5f), f.S(0.6f, 2.4f, 8f));
            }
        for (int s = -1; s <= 1; s += 2)
        {
            Fuel(root, config, f, EnemyKind.Dummy, new Vector3(s * 6.25f, 0f, -7.5f), "the stacks");
            Fuel(root, config, f, EnemyKind.Dummy, new Vector3(s * 9.75f, 0f, 2.5f), "the stacks");
        }
        // THE COUNTER across the hall, the page over it, the bellhop behind it, the tubes down to their stations
        Lip(root, f, new Vector3(0f, 0.55f, counterZ), new Vector3(hw * 2f - 4f, 1.1f, 1.4f), LevelKit.WoodPanel, "Counter");
        Mark(root, f, new Vector3(0f, 1.12f, counterZ), new Vector3(hw * 2f - 3.9f, 0.04f, 1.5f), LevelKit.Marble, "CounterTop");
        Fuel(root, config, f, EnemyKind.Turret, new Vector3(0f, 4.5f, counterZ + 1f), "over the counter");
        Fuel(root, config, f, EnemyKind.Chaser, new Vector3(hw * 0.3f, 0f, counterZ + 3f), "behind the counter");
        foreach (float x in new[] { -12f, -2.2f, 2.2f, 12f })
        {
            HouseKit.Tube(root, f.P(x, h - 0.7f, -hd + 1.5f), f.P(x, h - 0.7f, counterZ + 0.9f));
            HouseKit.Tube(root, f.P(x, h - 0.7f, counterZ + 0.9f), f.P(x, 2.1f, counterZ + 0.9f));
            Mark(root, f, new Vector3(x, 1.65f, counterZ + 0.9f), new Vector3(0.5f, 0.9f, 0.4f), LevelKit.Brass, "TubeStation");
        }
        LevelKit.Text(root, f.P(0f, 3.4f, counterZ + 1.4f), Quaternion.LookRotation(f.D(Vector3.forward)), "REGISTRY OF GUESTS", 0.5f, new Color(2.2f, 1.7f, 0.4f), "RegistrySign", 12f, true);
        LevelKit.Text(root, f.P(0f, 6f, hd - 0.4f), Quaternion.LookRotation(f.D(Vector3.forward)), "TO THE VAULTS", 0.4f, new Color(2.2f, 1.7f, 0.4f), "VaultsSign", 6f, true);
        // THE FILING GALLERIES: 4.6 along both walls from their plinth lips, 9 along the east and across the front
        float gz1w = hd - 4f, gz1e = hd - 5f;
        for (int s = -1; s <= 1; s += 2)
        {
            float gz1 = s < 0 ? gz1w : gz1e;
            Lip(root, f, new Vector3(s * (hw - 1.75f), 0.65f, -hd + 5.5f), new Vector3(3.1f, 1.3f, 3f));
            Floor(root, f, new Vector3(s * (hw - 1.75f), g1 - 0.5f, (gz0 + gz1) * 0.5f), new Vector3(3.5f, 1f, gz1 - gz0), "Gallery");
            HouseKit.Rail(root, f.P(s * (hw - 3.4f), g1, gz0 + 0.2f), f.P(s * (hw - 3.4f), g1, gz1 - 0.2f), 1f);
            Fuel(root, config, f, EnemyKind.Dummy, new Vector3(s * (hw - 1.75f), g1, 0f), s < 0 ? "the west gallery" : "the east gallery");
            Lamp(root, f, new Vector3(s * 10f, h - 1.5f, -6f), h);
            Lamp(root, f, new Vector3(s * 10f, h - 1.5f, 3f), h);
        }
        Floor(root, f, new Vector3(hw - 1.75f, g2 - 0.5f, (-hd + 4f + gz1e) * 0.5f), new Vector3(3.5f, 1f, gz1e + hd - 4f), "Gallery");
        Floor(root, f, new Vector3(-hw + 1.75f, g2 - 0.5f, (-hd + 4f + gz1w) * 0.5f), new Vector3(3.5f, 1f, gz1w + hd - 4f), "Gallery");
        Floor(root, f, new Vector3(0f, g2 - 0.5f, -hd + 2.25f), new Vector3(hw * 2f - 7f, 1f, 3.5f), "Gallery");
        HouseKit.Rail(root, f.P(hw - 3.4f, g2, -hd + 4.2f), f.P(hw - 3.4f, g2, gz1e - 0.2f), 1f);
        HouseKit.Rail(root, f.P(-hw + 3.4f, g2, -hd + 4.2f), f.P(-hw + 3.4f, g2, gz1w - 0.2f), 1f);
        HouseKit.Rail(root, f.P(-hw + 3.6f, g2, -hd + 4f), f.P(hw - 3.6f, g2, -hd + 4f), 1f);
        Fuel(root, config, f, EnemyKind.Dummy, new Vector3(0f, g2, -hd + 2.25f), "the front gallery");
        // THE RECORDS LIFT: the shaft to the roof hatch, its pad; the window's awning outside; the chandelier
        LiftShaft(root, f, new Vector3(hw - 3f, 0f, hd - 3f), 0f, h + 1f);
        LevelKit.Chandelier(root, f.P(0f, 8.5f, -hd + 10f), f.origin.y + h);
        LevelKit.Awning(root, f.P(-hw - T * 0.5f, 0f, winZ), f.D(Vector3.left), 5f);
        Fuel(root, config, f, EnemyKind.Sentry, new Vector3(3f, 0f, -hd + 3f), "the doors");
        PoolFuel(config, f, new Vector3(R(-3f, 3f), 0f, -2.5f), "the lanes");
        Line(root, b, f, "the ledgers", new Vector3(0f, 0f, -hd + 1f), new Vector3(0f, 0f, counterZ - 1.5f), zone, false);
        Line(root, b, f, "the filing window", new Vector3(-hw - 8f, 0f, winZ), new Vector3(-hw - 1.2f, 0f, winZ), zone);
        Line(root, b, f, "the records lift", new Vector3(hw - 3f, 0f, counterZ + 1.5f), new Vector3(hw - 3f, 0f, hd - 4.8f), zone, false);
        // The roof, the facades, the clock over the doors
        Roof(root, f, h, hatch, 1, false);
        HouseFacades(b, f, h, 1, 4.4f, false, false, true);
        LevelKit.ClockFace(root, f.P(0f, h - 1.5f, -hd - 0.45f), f.D(Vector3.back), 3f);
        Entrance(plan, b, f, "the doors", new Vector3(0f, 0f, -hd), Vector3.back, EK.Door);
        Entrance(plan, b, f, "the back door", new Vector3(0f, 0f, hd), Vector3.forward, EK.Door);
        Entrance(plan, b, f, "the filing window", new Vector3(-hw, g1, winZ), Vector3.left, EK.Window, true);
        Entrance(plan, b, f, "the records hatch", new Vector3(hatch.x, h + 1f, hatch.y), Vector3.forward, EK.RoofHatch, true, false);
        if (string.IsNullOrEmpty(b.name)) b.name = "THE REGISTRY";
    }
}
