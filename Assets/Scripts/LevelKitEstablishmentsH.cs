using UnityEngine;
using Plan = CityDistrict.DistrictPlan;
using Block = CityDistrict.Block;
using EK = CityDistrict.EntranceKind;

/// <summary>
/// THE SANATORIUM'S STREETS (Level 1): the BATHHOUSE — THE HYDRO. A POOL HALL twelve metres tall in white glazed tile
/// with oxblood courses and brass rails: two EMPTY POOLS down the west side (drop-ins: a metre off the rim onto a 26°
/// sloped floor that runs out three and a half down, a pad in the deep end that throws you on — into the second pool,
/// then onto the back strip), the lane between the pools the main line, THE PLUNGE down the east side (four metres
/// deep, a 26° SLIDE from the MEZZANINE running its whole length, the hop through a glass pane at its foot, the pad
/// out), the MEZZANINE over the front strip (changing cubicles: bench lips, partition kick walls; pads at both ends
/// for the slow way up, plinth lips and fluted kick walls for the WALL COMBO), the SKYLIGHT WELL in the roof over the
/// slide, the ALLEY WINDOW four and a half metres up the east wall (a sill through the wall: the tenement's fire escape
/// lands on it), the BOILER ROOM across the back-east with its door to the alley and its flue up to a chimney kick
/// stack. The block floor the dispatcher lays is taken up and relaid in pieces round the pools (the speakeasy digs its
/// cellar the same way). MAIN: doors → the lane → the back strip → the boiler door (or the avenue door west). FAST: the
/// drop-ins in a chain, the window → the mezzanine → the slide → the glass → the pad, the roof's well onto the slide.
/// FUEL: a sentry at the doors, dummies on the mezzanine, a chaser on the slide's lip, a turret over the plunge, the
/// stoker in the boiler room, a roster unit on the back strip. Local frame: x across the front, z front (−) to back (+).
/// </summary>
public static partial class Establishments
{
    const float HydroH = 12f, HydroMezz = 5.3f, HydroLaneHW = 3.25f, HydroPoolW = 10f, HydroSlideDeg = 26f;

    static void Bathhouse(Transform root, MovementConfig config, Plan plan, Block b, BlockFrame f)
    {
        if (string.IsNullOrEmpty(b.name)) b.name = "THE HYDRO";                      // the name the mosaic and the marquee carry (a level may call it THE PUBLIC BATHS)
        float hw = f.hw, hd = f.hd, h = HydroH, mezz = HydroMezz;
        Vector3 min = new Vector3(-hw, 0f, -hd), max = new Vector3(hw, h, hd);
        float poolZ0 = -hd + 4f, backZ = hd - 6.5f;                                   // the front strip under the mezzanine; the back strip
        float wx = -HydroLaneHW - HydroPoolW * 0.5f, px = HydroLaneHW + HydroPoolW * 0.5f;   // the drop-ins' centre x, the plunge's
        float p1z1 = poolZ0 + 8.5f, p2z0 = p1z1 + 2.5f, p2z1 = backZ - 1f, mezzZ = poolZ0 + 1.5f;
        float sideX = hw - 1.9f, windowZ = -6f, avenueZ = backZ + 3f, boilerX = 12f;
        Vector2 well = new Vector2(px, windowZ);
        Room(root, f, min, max, LevelKit.Faces.North | LevelKit.Faces.South | LevelKit.Faces.East | LevelKit.Faces.West,
            Door(f, Face.South, 0f, 0f, min, max, 8f, 4.25f), Door(f, Face.West, avenueZ, 0f, min, max), Door(f, Face.North, boilerX, 0f, min, max),
            Window(f, Face.East, windowZ, 3.2f, 4.6f, 7.4f, min, max));
        // The floor, relaid in pieces round the three pools (the block floor the dispatcher laid is taken up first)
        bool dug = HydroClearBlockFloor(root, f);
        float ox = hw + T * 0.5f, oz = hd + T * 0.5f, pw = HydroPoolW * 0.5f;
        if (dug)
        {
            HydroFloor(root, f, -ox, ox, -oz, poolZ0, LevelKit.Floor);                    // the front strip
            HydroFloor(root, f, -ox, wx - pw, poolZ0, backZ, LevelKit.Floor);              // the west walkway
            HydroFloor(root, f, -HydroLaneHW, HydroLaneHW, poolZ0, backZ, LevelKit.Floor); // the lane between the pools
            HydroFloor(root, f, px + pw, ox, poolZ0, backZ, LevelKit.Floor);               // the east walkway
            HydroFloor(root, f, wx - pw, wx + pw, p1z1, p2z0, LevelKit.Floor);             // between the drop-ins
            HydroFloor(root, f, wx - pw, wx + pw, p2z1, backZ, LevelKit.Floor);
            HydroFloor(root, f, -ox, 2f, backZ, oz, LevelKit.Floor);                       // the back strip
            HydroFloor(root, f, 2f, ox, backZ, oz, LevelKit.Charcoal);                     // the boiler room's floor
        }
        var zone = plan.zone;
        // THE POOLS: the two drop-ins in a chain down the west side, the plunge with its slide down the east
        HydroDropIn(root, f, wx, poolZ0, p1z1, dug ? 3.5f : 0f);
        HydroDropIn(root, f, wx, p2z0, p2z1, dug ? 3.5f : 0f);
        HydroPlunge(root, config, f, px, poolZ0, backZ, mezzZ, dug ? 4f : 0f);
        Line(root, b, f, "the pools", new Vector3(0f, 0f, -hd + 1f), new Vector3(0f, 0f, poolZ0 - 0.4f), zone, false);
        Line(root, b, f, "the drop-ins", new Vector3(wx, 0f, -hd + 1f), new Vector3(wx, 0f, poolZ0 - 0.4f), zone);
        // THE MEZZANINE over the front strip: the cubicles, the rail with its mouth at the slide, the pads and plinths
        Floor(root, f, new Vector3(0f, mezz - 0.5f, (-oz + mezzZ) * 0.5f), new Vector3(ox * 2f, 1f, mezzZ + oz), "Mezzanine");
        HydroCubicles(root, f, mezz, -hd + 0.5f);
        float mouth0 = px - 3.1f, mouth1 = px + 3.1f;
        Mark(root, f, new Vector3((-hw + 0.5f + mouth0) * 0.5f, mezz + 0.5f, mezzZ - 0.15f), new Vector3(mouth0 + hw - 0.5f, 1f, 0.08f), LevelKit.Brass, "MezzRail");
        Mark(root, f, new Vector3((mouth1 + hw - 0.5f) * 0.5f, mezz + 0.5f, mezzZ - 0.15f), new Vector3(hw - 0.5f - mouth1, 1f, 0.08f), LevelKit.Brass, "MezzRail");
        float up = Mathf.Sqrt(2f * 25f * (mezz + 1.6f));
        for (int s = -1; s <= 1; s += 2)
        {
            LevelKit.JumpPad(root, f.P(s * sideX, 0f, poolZ0 + 3.5f), f.Yaw + 180f, up, 3.5f);                 // the slow way up: thrown back onto the mezzanine
            Lip(root, f, new Vector3(s * sideX, 0.65f, poolZ0 + 7.5f), new Vector3(2.6f, 1.3f, 2.6f), LevelKit.Tile, "Plinth");   // the fast way: corner boost it off a wall kick
            Flute(root, f, new Vector3(s * (hw - 0.5f), 2.5f, poolZ0 + 6.5f), Vector3.left * s, 4f);
            Line(root, b, f, s < 0 ? "the west cubicles" : "the east cubicles", new Vector3(s * sideX, 0f, poolZ0 + 13.5f), new Vector3(s * sideX, 0f, poolZ0 + 9.2f), zone);
        }
        Line(root, b, f, "the slide", new Vector3(px, mezz, -hd + 1f), new Vector3(px, mezz, mezzZ - 0.4f), zone);
        // THE ALLEY WINDOW: a limestone sill through the wall (a ledge in the alley, a ledge inside), a kick wall under it
        Lip(root, f, new Vector3(hw, 4.45f, windowZ), new Vector3(3.4f, 0.4f, 5f), LevelKit.Limestone, "WindowSill");
        Flute(root, f, new Vector3(hw - 0.5f, 2.3f, windowZ), Vector3.left, 3.6f);
        Line(root, b, f, "the alley window", new Vector3(hw - 0.6f, 4.65f, windowZ + 2.2f), new Vector3(hw - 0.6f, 4.65f, windowZ - 2.2f), zone);
        // The dressing: the tile, the names, the boiler room, the roof, the front
        HydroDado(root, f, backZ, b.name);
        HydroBoiler(root, config, f, backZ);
        HydroRoof(root, b, f, h, well, 2f, zone);
        HydroFront(root, b, f, h, avenueZ, boilerX);
        for (int i = 0; i < 3; i++) Lamp(root, f, new Vector3(0f, h - 2.6f, poolZ0 + 3f + i * 7f), h);
        for (int i = 0; i < 2; i++) { Lamp(root, f, new Vector3(wx, h - 2.6f, poolZ0 + 5f + i * 9f), h); Lamp(root, f, new Vector3(px, h - 2.6f, poolZ0 + 9f + i * 8f), h); }
        Lamp(root, f, new Vector3(-8f, h - 3f, (backZ + hd) * 0.5f), h);
        Lamp(root, f, new Vector3(0f, mezz - 1.4f, -hd + 2f), mezz - 0.5f);
        // FUEL
        Fuel(root, config, f, EnemyKind.Sentry, new Vector3(3f, 0f, -hd + 2f), "the doors");
        Fuel(root, config, f, EnemyKind.Dummy, new Vector3(-9f, mezz, mezzZ - 1.4f), "the cubicles");
        Fuel(root, config, f, EnemyKind.Dummy, new Vector3(1f, mezz, mezzZ - 1.4f), "the cubicles");
        Fuel(root, config, f, EnemyKind.Chaser, new Vector3(px, mezz, mezzZ - 1.2f), "the slide's lip");
        Fuel(root, config, f, EnemyKind.Turret, new Vector3(px, 4.5f, poolZ0 + 11f), "over the plunge");
        Fuel(root, config, f, EnemyKind.Warden, new Vector3(10.5f, 0f, (backZ + hd) * 0.5f), "the boiler room");
        PoolFuel(config, f, new Vector3(R(-3f, 1f), 0f, backZ + 2.5f), "the back strip");
        // The record
        Entrance(plan, b, f, "the front doors", new Vector3(0f, 0f, -hd), Vector3.back, EK.Door);
        Entrance(plan, b, f, "the avenue door", new Vector3(-hw, 0f, avenueZ), Vector3.left, EK.Door);
        Entrance(plan, b, f, "the boiler door", new Vector3(boilerX, 0f, hd), Vector3.forward, EK.Door);
        Entrance(plan, b, f, "the alley window", new Vector3(hw, 4.65f, windowZ), Vector3.right, EK.Window, true);
        Entrance(plan, b, f, "the skylight", new Vector3(well.x, h + 1f, well.y), Vector3.right, EK.Skylight, true, false);
    }

    /// The hydro DIGS (Establishments.Digs): the dispatcher lays it no block floor, so there is nothing to take up and the
    /// floor below is laid here in pieces round the pools. Always true; kept as the switch the floor code reads
    static bool HydroClearBlockFloor(Transform root, BlockFrame f) => true;

    /// A piece of the hall's floor, a metre thick, its top at the street (local x0..x1, z0..z1)
    static void HydroFloor(Transform root, BlockFrame f, float x0, float x1, float z0, float z1, Material mat)
        => LevelKit.Slab(root, f.P((x0 + x1) * 0.5f, -0.5f, (z0 + z1) * 0.5f), f.S(x1 - x0, 1f, z1 - z0), mat, "BlockFloor");

    /// AN EMPTY POOL you drop into: the rim's oxblood course on the floor, the tiled basin, the sloped floor (a metre
    /// under the near rim it starts, 26° down to the deep end), the pad in the deep end that throws you on north
    static void HydroDropIn(Transform root, BlockFrame f, float cx, float z0, float z1, float depth)
    {
        float w = HydroPoolW, cz = (z0 + z1) * 0.5f;
        HydroRim(root, f, cx, cz, w, z1 - z0);
        if (depth < 0.5f) { Pad(root, f, new Vector3(cx, 0f, cz), 12f, 5f); return; }
        HydroBasin(root, f, cx, z0, z1, depth);
        float sz0 = z0 + 0.5f, sz1 = sz0 + (depth - 1f) / Mathf.Tan(HydroSlideDeg * Mathf.Deg2Rad);
        Ramp(root, f, cx, w - 0.6f, sz0, -1f, sz1, -depth, LevelKit.Slope);
        Pad(root, f, new Vector3(cx, -depth, z1 - 1.8f), Mathf.Sqrt(2f * 25f * (depth + 2.2f)), 5f);
    }

    /// THE PLUNGE: the deepest pool, the slide from the mezzanine's edge down its whole length (brass banisters as its
    /// lips), stopping a metre and a half over its floor — the hop — through a glass pane, onto the pad that throws
    /// you out over the north rim onto the back strip. A slow walker goes round the pane
    static void HydroPlunge(Transform root, MovementConfig config, BlockFrame f, float cx, float z0, float z1, float mezzZ, float depth)
    {
        float w = HydroPoolW, cz = (z0 + z1) * 0.5f;
        HydroRim(root, f, cx, cz, w, z1 - z0);
        if (depth < 0.5f) { Pad(root, f, new Vector3(cx, 0f, cz), 12f, 5f); return; }
        HydroBasin(root, f, cx, z0, z1, depth);
        float run = 15.5f, sz1 = mezzZ + run, footY = HydroMezz - run * Mathf.Tan(HydroSlideDeg * Mathf.Deg2Rad);
        Ramp(root, f, cx, 3.4f, mezzZ, HydroMezz, sz1, footY, LevelKit.Slope);
        Banister(root, f, cx - 2.4f, mezzZ, HydroMezz, sz1, footY);
        Banister(root, f, cx + 2.4f, mezzZ, HydroMezz, sz1, footY);
        LevelKit.Glass(root, f.P(cx, -depth + 1.6f, sz1 + 1f), f.S(4f, 3f, 0.3f), config);
        Pad(root, f, new Vector3(cx, -depth, sz1 + 1.7f), Mathf.Sqrt(2f * 25f * (depth + 2.4f)), 6f);
        Mark(root, f, new Vector3(cx, -depth + 0.006f, cz), new Vector3(1.2f, 0.01f, 1.2f), LevelKit.RivetIron, "Drain");
    }

    /// A tiled basin: its floor a metre thick, its four walls inside the footprint (the long ones kick walls), the
    /// waterline an oxblood course a half metre under the rim, a brass ladder in the near-west corner
    static void HydroBasin(Transform root, BlockFrame f, float cx, float z0, float z1, float depth)
    {
        float w = HydroPoolW, cz = (z0 + z1) * 0.5f, len = z1 - z0, yc = -depth * 0.5f;
        Box(root, f, new Vector3(cx, -depth - 0.5f, cz), new Vector3(w + 0.6f, 1f, len + 0.6f), LevelKit.Tile, "PoolFloor");
        for (int s = -1; s <= 1; s += 2)
        {
            Kick(root, f, new Vector3(cx + s * (w * 0.5f - 0.25f), yc, cz), new Vector3(0.5f, depth, len), LevelKit.Tile);
            Box(root, f, new Vector3(cx, yc, cz + s * (len * 0.5f - 0.25f)), new Vector3(w - 1f, depth, 0.5f), LevelKit.Tile, "PoolWall");
            Mark(root, f, new Vector3(cx + s * (w * 0.5f - 0.5f - 0.028f), -0.65f, cz), new Vector3(0.05f, 0.3f, len - 1.1f), LevelKit.OxbloodTile, "Waterline");
            Mark(root, f, new Vector3(cx, -0.65f, cz + s * (len * 0.5f - 0.5f - 0.028f)), new Vector3(w - 1.1f, 0.3f, 0.05f), LevelKit.OxbloodTile, "Waterline");
        }
        float lx = cx - w * 0.5f + 0.9f, lz = z0 + 0.75f;
        for (int s = -1; s <= 1; s += 2) Mark(root, f, new Vector3(lx + s * 0.25f, -depth * 0.5f + 0.35f, lz), new Vector3(0.05f, depth + 0.7f, 0.05f), LevelKit.Brass, "PoolLadder");
        for (float y = -depth + 0.4f; y < -0.1f; y += 0.5f) Mark(root, f, new Vector3(lx, y, lz), new Vector3(0.5f, 0.04f, 0.04f), LevelKit.Brass, "PoolLadder");
    }

    /// The rim: an oxblood tile course on the hall floor round a pool's footprint, 3 mm proud. Decor
    static void HydroRim(Transform root, BlockFrame f, float cx, float cz, float w, float len)
    {
        const float rw = 0.45f;
        float y = LevelKit.MarkProud;
        Mark(root, f, new Vector3(cx, y, cz - len * 0.5f - rw * 0.5f), new Vector3(w + 2f * rw, 0.006f, rw), LevelKit.OxbloodTile, "PoolRim");
        Mark(root, f, new Vector3(cx, y, cz + len * 0.5f + rw * 0.5f), new Vector3(w + 2f * rw, 0.006f, rw), LevelKit.OxbloodTile, "PoolRim");
        Mark(root, f, new Vector3(cx - w * 0.5f - rw * 0.5f, y, cz), new Vector3(rw, 0.006f, len), LevelKit.OxbloodTile, "PoolRim");
        Mark(root, f, new Vector3(cx + w * 0.5f + rw * 0.5f, y, cz), new Vector3(rw, 0.006f, len), LevelKit.OxbloodTile, "PoolRim");
    }

    /// THE CHANGING CUBICLES along the mezzanine's back wall: a partition every four metres (a kick wall, 3 m), a
    /// bench in every bay (a 1 m lip), a curtain half drawn across the odd ones (decor)
    static void HydroCubicles(Transform root, BlockFrame f, float mezz, float wallZ)
    {
        float x0 = -f.hw + 2.5f, x1 = f.hw - 2.5f;
        int bays = Mathf.Max(3, Mathf.FloorToInt((x1 - x0) / 4f));
        float pitch = (x1 - x0) / bays;
        for (int i = 0; i <= bays; i++) Kick(root, f, new Vector3(x0 + i * pitch, mezz + 1.5f, wallZ + 1.1f), new Vector3(0.15f, 3f, 2.2f), LevelKit.Tile);
        for (int i = 0; i < bays; i++)
        {
            float bx = x0 + (i + 0.5f) * pitch;
            Lip(root, f, new Vector3(bx, mezz + 0.5f, wallZ + 0.25f), new Vector3(pitch - 0.6f, 1f, 0.5f), LevelKit.WoodPanel, "Bench");
            Mark(root, f, new Vector3(bx, mezz + 2.55f, wallZ + 2.2f), new Vector3(pitch - 0.3f, 0.06f, 0.06f), LevelKit.Brass, "CurtainRail");
            if (R(0f, 1f) < 0.55f) Mark(root, f, new Vector3(bx - pitch * 0.25f, mezz + 1.3f, wallZ + 2.22f), new Vector3(pitch * 0.4f, 2.4f, 0.04f), LevelKit.Oxblood, "Curtain");
        }
    }
}
