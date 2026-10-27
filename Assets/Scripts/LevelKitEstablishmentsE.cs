using UnityEngine;
using Plan = CityDistrict.DistrictPlan;
using Block = CityDistrict.Block;
using EK = CityDistrict.EntranceKind;

/// <summary>
/// BELOW STAIRS, THROUGH THE CITY (Level 3; LevelKitEstablishmentsE, E2, E3): the service district's own establishments —
/// the hotel KITCHENS (this file), the steam LAUNDRY (E2) and the SERVICE YARD (E3) — and what they share: copper and
/// white tile (the below-stairs look inside the city's cream shells), a tile dado, a roof deck laid in pieces round two
/// openings, a decor pipe. Local frame as every establishment: x across the front, z from the front (−hd, the street it
/// faces) to the back (+hd), y up from the street.
/// </summary>
public static partial class Establishments
{
    static Material copperMat;
    /// COPPER: the ranges' tops and hoods, the boiler's pipes, the mangles' rollers (brass's surface in a redder metal)
    static Material Copper => copperMat ??= LevelKit.Surface(new Color(0.64f, 0.36f, 0.2f), 0.72f, LevelKit.Surf.Brass, 1f);
    /// WHITE TILE: the prep tables, the sinks, the pass, the dados (the Underground's cream tile)
    static Material WhiteTile => LevelKit.Tile;

    /// A white tile DADO along the foot of a local wall face from `u0` to `u1` along it: a decor sheet `height` tall,
    /// three centimetres proud of the wall (never on its plane)
    static void Dado(Transform root, BlockFrame f, Face local, float u0, float u1, float height = 1.6f)
    {
        bool alongX = local == Face.North || local == Face.South;
        float hw = f.hw - T * 0.5f, hd = f.hd - T * 0.5f;   // the walls stand centred on the frame's planes: their inner faces half a wall in
        float plane = local == Face.North ? hd : local == Face.South ? -hd : local == Face.East ? hw : -hw;
        float um = (u0 + u1) * 0.5f, len = u1 - u0;
        if (len < 0.5f) return;
        Vector3 lc = (alongX ? new Vector3(um, height * 0.5f, plane) : new Vector3(plane, height * 0.5f, um)) - BlockFrame.Normal(local) * 0.03f;
        Mark(root, f, lc, alongX ? new Vector3(len, height, 0.02f) : new Vector3(0.02f, height, len), WhiteTile, "TileDado");
    }

    /// A piece of a ROOF DECK over local x0..x1, z0..z1, a metre thick with its centre at `y` (the decks laid round openings)
    static void Deck(Transform root, BlockFrame f, float x0, float x1, float z0, float z1, float y)
    {
        if (x1 - x0 < 0.05f || z1 - z0 < 0.05f) return;
        LevelKit.Slab(root, f.P((x0 + x1) * 0.5f, y, (z0 + z1) * 0.5f), f.S(x1 - x0, 1f, z1 - z0), LevelKit.Charcoal, "RoofDeck");
    }

    /// A decor CYLINDER (no collider) `length` long and `dia` across, lying along local x, local z or standing (`along`)
    static void Pipe(Transform root, BlockFrame f, Vector3 lc, float length, float dia, Vector3 along, Material m, string name)
    {
        Quaternion lie = along == Vector3.right ? Quaternion.Euler(0f, 0f, 90f) : along == Vector3.forward ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity;
        LevelKit.Prim(root, f.P(lc), new Vector3(dia, length * 0.5f, dia), m, name, PrimitiveType.Cylinder, f.Q * lie);
    }

    /// KITCHEN. A grand hotel's kitchens, ten metres to the roof deck. THE SERVICE CORRIDOR inside the tradesmen's door,
    /// then THE PASS (a partition with a nine-metre opening, the counter a 1.1 m tile lip in it, the heat rail over),
    /// then THE KITCHEN: ranges along the west wall under copper hoods (0.9 m lips), two rows of white-tile prep tables
    /// down the middle with a seven-metre lane between, THE COLD STORE against the east wall (a riveted kick box, its
    /// roof a lip: a corner boost from it reaches THE PANTRY, a mezzanine at six over the back). At the back THE SCULLERY
    /// (east: the sinks under a window at 2.6 onto the alley, the Astor's balcony across it) and THE BOILER ROOM (west:
    /// the boiler, THE COAL CHUTE sliding in at 25° from a hatch at 2.4 in the alley wall, the one hazard in Act I — a
    /// STEAM VENT in the floor under an opening in the pantry: a hiss, then a 12 m/s launch; jump on the burst and it is
    /// the pantry, stand in it through the hiss and it hurts), and THE DUMB WAITER in the corner: two boost pillars from
    /// the floor through the pantry to the roof hatch, a rest lip at 4.6 and 9.2, a pad at the foot.
    /// MAIN: the tradesmen's door → the pass → between the tables → the yard door. FAST: the cold store's roof → the
    /// pantry → the dumb waiter → the roof; the vent on the burst; in by the scullery window or the coal hatch.
    /// FUEL: a doorman at the door, chasers at the pass, footmen at the ranges and the sinks, a page over the tables,
    /// a footman in the pantry, a roster unit on the main line.
    static void Kitchen(Transform root, MovementConfig config, Plan plan, Block b, BlockFrame f)
    {
        const float h = 10f, pantryY = 6f, chuteDeg = 25f;
        float hw = f.hw, hd = f.hd, passZ = -hd + 8f, backZ = hd - 11f;
        float scullZ = hd - 5f, hatchZ = hd - 8.75f, chuteRun = 2.4f / Mathf.Tan(chuteDeg * Mathf.Deg2Rad);
        float ventX = hw - 10f, ventZ = hd - 6f, rangeX = hw - 2.2f, coldX = -hw + 3.7f;
        Vector3 min = new Vector3(-hw, 0f, -hd), max = new Vector3(hw, h, hd);
        Vector2 hatch = new Vector2(hw - 3.5f, hd - 2.5f);
        var zone = plan.zone;
        Shell(root, f, h, hatch,
            Door(f, Face.South, 0f, 0f, min, max), Door(f, Face.North, 0f, 0f, min, max),
            Window(f, Face.West, scullZ, 4f, 2.6f, 5.2f, min, max),          // the scullery window: onto the alley, the balcony across it
            Window(f, Face.East, hatchZ, 3.6f, 1.7f, 5f, min, max));          // the coal hatch: the chute's mouth in the alley wall (cut low: the chute runs through it)
        // THE SERVICE CORRIDOR and THE PASS: trolleys by the door, the partition, the counter lip in its opening, the heat rail
        for (int s = -1; s <= 1; s += 2) Lip(root, f, new Vector3(s * (hw - 2.2f), 0.45f, -hd + 4.5f), new Vector3(1.2f, 0.9f, 2.6f), LevelKit.RivetIron, "Trolley");
        Box(root, f, new Vector3(-(hw + 4.5f) * 0.5f, h * 0.5f, passZ), new Vector3(hw - 4.5f, h, 0.4f), LevelKit.Wall, "Partition");
        Box(root, f, new Vector3((hw + 4.5f) * 0.5f, h * 0.5f, passZ), new Vector3(hw - 4.5f, h, 0.4f), LevelKit.Wall, "Partition");
        Lip(root, f, new Vector3(0f, 0.55f, passZ), new Vector3(6.5f, 1.1f, 0.7f), WhiteTile, "Pass");
        Mark(root, f, new Vector3(0f, 4.4f, passZ), new Vector3(9.2f, 0.25f, 0.5f), Copper, "HeatRail");
        Fuel(root, config, f, EnemyKind.Sentry, new Vector3(2.5f, 0f, -hd + 3f), "the tradesmen's door");
        for (int s = -1; s <= 1; s += 2) Fuel(root, config, f, EnemyKind.Chaser, new Vector3(s * 2.6f, 0f, passZ + 2.6f), "the pass");
        // THE KITCHEN: the ranges under their hoods and flues, the prep tables, the cold store and its roof lip
        int k = 0;
        for (float z = passZ + 3.2f; z < backZ - 2.4f; z += 4.4f, k++)
        {
            Lip(root, f, new Vector3(rangeX, 0.45f, z), new Vector3(3.2f, 0.9f, 3f), Copper, "Range");
            Mark(root, f, new Vector3(rangeX, 3.4f, z), new Vector3(3.2f, 0.8f, 3.2f), Copper, "RangeHood");
            Pipe(root, f, new Vector3(rangeX, (3.8f + h) * 0.5f, z), h - 3.8f, 0.6f, Vector3.up, LevelKit.RivetIron, "Flue");
            if (k % 2 == 0) Fuel(root, config, f, EnemyKind.Dummy, new Vector3(rangeX - 2.6f, 0f, z), "the ranges");
        }
        for (int s = -1; s <= 1; s += 2)
            for (float z = passZ + 4f; z < backZ - 3f; z += 6f)
                Lip(root, f, new Vector3(s * 4.6f, 0.45f, z), new Vector3(2.2f, 0.9f, 4.2f), WhiteTile, "PrepTable");
        Kick(root, f, new Vector3(coldX, 2f, backZ - 3.6f), new Vector3(6f, 4f, 5f), LevelKit.RivetIron);
        Lip(root, f, new Vector3(coldX, 4.3f, backZ - 3.6f), new Vector3(6.2f, 0.6f, 5.2f), LevelKit.Limestone, "ColdStoreRoof");
        Mark(root, f, new Vector3(coldX, 1.6f, backZ - 6.14f), new Vector3(1.6f, 2.6f, 0.06f), LevelKit.Charcoal, "ColdStoreDoor");
        Fuel(root, config, f, EnemyKind.Turret, new Vector3(0f, 5.5f, 0f), "over the tables");
        PoolFuel(config, f, new Vector3(R(-3f, 3f), 0f, passZ + 6f), "main");
        // THE PANTRY: a mezzanine at six over the back, its front edge the lip you reach from the cold store's roof, laid
        // round the vent's opening and the dumb waiter's corner; a brass rail along it, shelves, a footman
        Lip(root, f, new Vector3((-hw + ventX - 1.5f) * 0.5f, pantryY - 0.5f, (backZ + hd) * 0.5f), new Vector3(ventX - 1.5f + hw, 1f, hd - backZ), LevelKit.Floor, "Pantry");
        Floor(root, f, new Vector3(ventX, pantryY - 0.5f, (backZ + ventZ - 1.5f) * 0.5f), new Vector3(3f, 1f, ventZ - 1.5f - backZ), "Pantry");
        Floor(root, f, new Vector3(ventX, pantryY - 0.5f, (ventZ + 1.5f + hd) * 0.5f), new Vector3(3f, 1f, hd - ventZ - 1.5f), "Pantry");
        Floor(root, f, new Vector3((ventX + 1.5f + hw - 5.9f) * 0.5f, pantryY - 0.5f, (backZ + hd) * 0.5f), new Vector3(hw - 7.4f - ventX, 1f, hd - backZ), "Pantry");
        Mark(root, f, new Vector3(-2.95f, pantryY + 0.5f, backZ + 0.4f), new Vector3(hw * 2f - 5.9f, 1f, 0.08f), LevelKit.Brass, "PantryRail");
        Mark(root, f, new Vector3(-hw * 0.45f, pantryY + 1.2f, hd - 0.72f), new Vector3(hw * 0.8f, 2.2f, 0.36f), LevelKit.WoodPanel, "Shelves");
        Fuel(root, config, f, EnemyKind.Dummy, new Vector3(-hw * 0.4f, pantryY, hd - 5f), "the pantry");
        // THE SCULLERY (east): the sinks under the window (a 1.2 m lip: boost out of it from them); crates in the alley under it
        Lip(root, f, new Vector3(-hw + 1.8f, 0.6f, hd - 6f), new Vector3(2.4f, 1.2f, 9f), WhiteTile, "Sinks");
        Fuel(root, config, f, EnemyKind.Dummy, new Vector3(-hw + 4.5f, 0f, hd - 6f), "the scullery");
        LevelKit.Crates(root, f.P(-hw - 1.2f, 0f, scullZ), f.Yaw + 90f);
        // THE BOILER ROOM (west): the boiler a riveted kick block with copper pipes up the wall, the STEAM VENT under the
        // pantry's opening, red-lit; the coal chute from the hatch down, a pad at its foot for the way up and out
        Kick(root, f, new Vector3(hw - 9.5f, 2.45f, hd - 2.2f), new Vector3(3f, 4.9f, 3f), LevelKit.RivetIron);
        for (int i = 0; i < 3; i++) Pipe(root, f, new Vector3(hw - 0.8f, 2.5f + i * 0.9f, hd - 3.5f), 5f, 0.16f, Vector3.forward, Copper, "SteamPipe");
        LevelKit.SteamVentPad(root, f.P(ventX, 0f, ventZ), 6f, 0.25f);
        var red = LevelKit.Light(root, f.P(hw - 6.5f, 3.4f, hd - 5f), 10f, 1.3f);
        red.color = new Color(1f, 0.22f, 0.12f);
        Slope(root, f, new Vector3(hw + 0.5f, 2.4f, hatchZ), new Vector3(hw + 0.5f - chuteRun, 0f, hatchZ), 3.4f, LevelKit.Slope, "CoalChute");   // its sill at the alley's face
        LevelKit.JumpPad(root, f.P(hw - 7.3f, 0f, hatchZ), f.Yaw + 90f, 12.5f, 7f);
        Fuel(root, config, f, EnemyKind.Dummy, new Vector3(hw - 7.3f, 0f, hd - 1.8f), "the boiler");
        // THE DUMB WAITER: the kick shaft from the floor through the pantry to the roof hatch (LiftShaft)
        LiftShaft(root, f, new Vector3(hatch.x, 0f, hatch.y), 0f, h + 1f);
        // The lines, the lamps, the tile
        Line(root, b, f, "the pass", new Vector3(0f, 0f, -hd + 1f), new Vector3(0f, 0f, passZ - 1.2f), zone, false);
        Line(root, b, f, "the cold store", new Vector3(coldX, 0f, passZ + 1f), new Vector3(coldX, 0f, backZ - 6.8f), zone);
        Line(root, b, f, "the dumb waiter", new Vector3(hatch.x, 0f, backZ + 0.5f), new Vector3(hatch.x, 0f, hd - 4.6f), zone);
        Line(root, b, f, "the coal hatch", new Vector3(hw - 11.6f, 0f, hatchZ), new Vector3(hw - 8.8f, 0f, hatchZ), zone);
        for (float z = passZ + 4f; z < backZ - 2f; z += 8f) Lamp(root, f, new Vector3(0f, h - 1.6f, z), h);
        Lamp(root, f, new Vector3(0f, h - 1.6f, -hd + 4f), h);
        Lamp(root, f, new Vector3(-hw * 0.5f, pantryY - 1.9f, hd - 5.5f), pantryY - 1f);
        Dado(root, f, Face.East, passZ + 0.4f, backZ - 0.4f);
        Dado(root, f, Face.West, passZ + 0.4f, hd - 0.4f);
        Dado(root, f, Face.North, -hw + 0.4f, -3.6f);
        Dado(root, f, Face.North, 3.6f, hw - 0.4f);
        // The roof (cowls, the ranges' flue head), the facades, the entrances
        Roof(root, f, h, hatch, 2, false);
        for (int i = 0; i < 3; i++) Pipe(root, f, new Vector3(-hw * 0.5f + i * 4f, h + 1.6f, -hd * 0.4f), 1.2f, 0.9f, Vector3.up, LevelKit.RivetIron, "Cowl");
        LevelKit.KickStack(root, f.P(rangeX - 1f, h + 1f, passZ + 6f), 4.5f, 2f);
        Facades(root, b, f, h, 1, "THE ASTOR · KITCHENS", new Vector2(-hw * 0.5f, hw * 0.6f));   // beside the tradesmen's door
        Entrance(plan, b, f, "the tradesmen's door", new Vector3(0f, 0f, -hd), Vector3.back, EK.Door);
        Entrance(plan, b, f, "the yard door", new Vector3(0f, 0f, hd), Vector3.forward, EK.Door);
        Entrance(plan, b, f, "the scullery window", new Vector3(-hw, 2.6f, scullZ), Vector3.left, EK.Window, true);
        Entrance(plan, b, f, "the coal hatch", new Vector3(hw, 2.4f, hatchZ), Vector3.right, EK.Chute, true);
        Entrance(plan, b, f, "the roof hatch", new Vector3(hatch.x, h + 1f, hatch.y), Vector3.forward, EK.RoofHatch, true, false);
        if (string.IsNullOrEmpty(b.name)) b.name = "THE KITCHENS";
    }
}
