using UnityEngine;
using Plan = CityDistrict.DistrictPlan;
using Block = CityDistrict.Block;
using EK = CityDistrict.EntranceKind;

/// <summary>
/// THE ESTABLISHMENTS of the Sanatorium's streets (Act I, L1): the SPEAKEASY and the DINER. Local frame: x across the
/// front, z from the front (−hd, the street it faces) to the back (+hd), y up from the street.
/// </summary>
public static partial class Establishments
{
    /// A SLOPE whose top runs from local `a` to local `b`, `width` across: the chutes and the slides that run any way
    /// `supports`: square posts from the block floor to the underside at the high end and the midpoint, for a flight that
    /// crosses open room (a slope against a wall or over its own cellar needs none)
    static GameObject Slope(Transform root, BlockFrame f, Vector3 a, Vector3 b, float width, Material mat = null, string name = "Ramp", bool supports = false)
    {
        Vector3 wa = f.P(a), wb = f.P(b), d = wb - wa;
        float len = d.magnitude;
        Quaternion rot = Quaternion.LookRotation(d / len, Vector3.up);
        var go = LevelKit.Slab(root, (wa + wb) * 0.5f - rot * Vector3.up * 0.175f, new Vector3(width, 0.35f, len), mat ?? LevelKit.Slope, name);
        go.transform.rotation = rot;
        if (supports)
        {
            Vector3 hi = a.y >= b.y ? a : b, lo = a.y >= b.y ? b : a, mid = (a + b) * 0.5f;
            foreach (var p in new[] { hi, mid })
            {
                float top = p.y - 0.4f;
                if (top < 1f) continue;
                LevelKit.Slab(root, f.P(p.x, top * 0.5f, p.z), f.S(0.45f, top, 0.45f), LevelKit.Charcoal, name + "Post");
            }
        }
        return go;
    }

    /// SPEAKEASY. A laundry on the street (the cover: a counter, a door-in-a-door 6.5 m wide into the back room);
    /// from the back room a stair down six metres into THE CELLAR — the bar along the west wall (a 1.1 m lip), the
    /// band stand at the back (a lip), tables (0.9 m lips), a chandelier — and out of the cellar THE COAL CHUTE, a 25°
    /// slide up through the west wall into the street beside it (a pad at its foot for the slow way up). Over the
    /// laundry an office with a window onto the east street, a sill lip under it, reached by a stair from the back room.
    /// MAIN: door → door-in-a-door → the stair → the cellar → the chute → out west. FAST: the banister down, a slide-jump
    /// over the bar, the band stand's corner boost, up the chute at speed; the awning outside to the office window.
    /// FUEL: a bouncer at the inner door, a barman behind the bar, a page over the band stand, drinkers at the tables.
    static void Speakeasy(Transform root, MovementConfig config, Plan plan, Block b, BlockFrame f)
    {
        const float h = 8f, cellar = -6f, chuteDeg = 25f;
        float hw = f.hw, hd = f.hd;
        Vector3 min = new Vector3(-hw, 0f, -hd), max = new Vector3(hw, h, hd);
        Vector2 hatch = new Vector2(-hw + 3f, hd - 3f);
        float frontZ = -hd + 10f, stairZ0 = -hd + 11f, stairZ1 = -hd + 23f, stairX0 = hw - 7.5f, chuteRun = -cellar / Mathf.Tan(chuteDeg * Mathf.Deg2Rad), chuteOpen = 8.5f;
        Shell(root, f, h, hatch,
            Door(f, Face.South, -hw * 0.3f, 0f, min, max), Door(f, Face.North, 0f, 0f, min, max),
            Window(f, Face.East, -hd + 5f, 3.2f, 4.8f, 7.6f, min, max),
            f.H(Face.West, -1.8f, 1.8f, 0f, 2.6f, min, max));
        // The ground floor in pieces round the stair hole (east) and the chute's opening (west), top at the street
        Floor(root, f, new Vector3(0f, -0.5f, (-hd + stairZ0) * 0.5f), new Vector3(hw * 2f, 1f, stairZ0 + hd), "BlockFloor");
        Floor(root, f, new Vector3(0f, -0.5f, (stairZ1 + hd) * 0.5f), new Vector3(hw * 2f, 1f, hd - stairZ1), "BlockFloor");
        Floor(root, f, new Vector3((-hw + chuteOpen + stairX0) * 0.5f, -0.5f, (stairZ0 + stairZ1) * 0.5f), new Vector3(stairX0 + hw - chuteOpen, 1f, stairZ1 - stairZ0), "BlockFloor");
        Floor(root, f, new Vector3((hw - 1.5f + hw) * 0.5f, -0.5f, (stairZ0 + stairZ1) * 0.5f), new Vector3(1.5f, 1f, stairZ1 - stairZ0), "BlockFloor");
        Floor(root, f, new Vector3(-hw + chuteOpen * 0.5f, -0.5f, (stairZ0 - 2.2f) * 0.5f), new Vector3(chuteOpen, 1f, -2.2f - stairZ0), "BlockFloor");
        Floor(root, f, new Vector3(-hw + chuteOpen * 0.5f, -0.5f, (2.2f + stairZ1) * 0.5f), new Vector3(chuteOpen, 1f, stairZ1 - 2.2f), "BlockFloor");
        // The cellar: its own walls and floor under the whole block, five metres of headroom under the ground floor
        Room(root, f, new Vector3(-hw, cellar, -hd), new Vector3(hw, -1f, hd), LevelKit.Faces.North | LevelKit.Faces.South | LevelKit.Faces.East | LevelKit.Faces.West | LevelKit.Faces.Floor);
        // The laundry: counters as lips, the door-in-a-door (a partition with a 6.5 m opening and a brass frame)
        Lip(root, f, new Vector3(-hw * 0.45f, 0.55f, -hd + 5f), new Vector3(hw * 0.7f, 1.1f, 1.2f), LevelKit.WoodPanel, "Counter");
        Box(root, f, new Vector3(-hw * 0.5f - 3.25f, h * 0.5f, frontZ), new Vector3(hw - 3.25f, h, 0.5f), LevelKit.Wall, "Partition");
        Box(root, f, new Vector3(hw * 0.5f + 3.25f, h * 0.5f, frontZ), new Vector3(hw - 3.25f, h, 0.5f), LevelKit.Wall, "Partition");
        Box(root, f, new Vector3(0f, (h + 4.25f) * 0.5f, frontZ), new Vector3(6.5f, h - 4.25f, 0.5f), LevelKit.Wall, "Partition");
        Mark(root, f, new Vector3(0f, 4.4f, frontZ), new Vector3(7f, 0.3f, 0.7f), LevelKit.Brass, "Frame");
        Fuel(root, config, f, EnemyKind.Warden, new Vector3(0f, 0f, frontZ + 2f), "the inner door");
        // The stair down, along the east wall: a 26° flight with a banister either side, the stair's own lip at the top
        Ramp(root, f, stairX0 + 3f, 6f, stairZ0, 0f, stairZ1, cellar, LevelKit.Floor, true);
        Banister(root, f, stairX0 + 0.5f, stairZ0, 0f, stairZ1, cellar);
        Banister(root, f, hw - 1.3f, stairZ0, 0f, stairZ1, cellar);
        Line(root, b, f, "the stair", new Vector3(stairX0 + 3f, 0f, frontZ + 0.5f), new Vector3(stairX0 + 3f, 0f, stairZ0 - 0.3f), plan.zone, false);
        // The cellar: the bar, the band stand, the tables, the chandelier, the chute up and out west (a pad at its foot)
        Lip(root, f, new Vector3(-hw + 5.2f, cellar + 0.55f, hd * 0.1f), new Vector3(1.4f, 1.1f, hd * 1.1f), LevelKit.WoodPanel, "Bar");
        Fuel(root, config, f, EnemyKind.Sentry, new Vector3(-hw + 3f, cellar, hd * 0.1f), "behind the bar");
        Lip(root, f, new Vector3(hw * 0.2f, cellar + 0.5f, hd - 3.5f), new Vector3(10f, 1f, 5f), LevelKit.WoodPanel, "BandStand");
        Fuel(root, config, f, EnemyKind.Turret, new Vector3(hw * 0.2f, cellar + 4f, hd - 7f), "over the band stand");
        for (int i = 0; i < 4; i++)
        {
            Vector3 t = new Vector3(-hw * 0.2f + (i % 2) * hw * 0.5f, cellar + 0.45f, -hd * 0.55f + (i / 2) * hd * 0.7f);
            Lip(root, f, t, new Vector3(1.8f, 0.9f, 1.8f), LevelKit.WoodPanel, "Table");
            if (i < 2) Fuel(root, config, f, EnemyKind.Dummy, t + Vector3.up * 0.45f + Vector3.forward * 1.6f, "the tables");
        }
        LevelKit.Chandelier(root, f.P(0f, -2.2f, 0f), f.origin.y - 1f, 2f);
        Slope(root, f, new Vector3(-hw + 0.4f, 0f, 0f), new Vector3(-hw + 0.4f + chuteRun, cellar, 0f), 3.4f, LevelKit.Slope, "CoalChute");
        Pad(root, f, new Vector3(-hw + 2f + chuteRun + 1.5f, cellar, 0f), 14f, 9f);
        Line(root, b, f, "the coal chute", new Vector3(-hw + chuteRun + 6f, cellar, 0f), new Vector3(-hw + chuteRun + 1.5f, cellar, 0f), plan.zone);
        Line(root, b, f, "over the bar", new Vector3(hw - 9f, cellar, stairZ1 - 1f), new Vector3(-hw + 8f, cellar, hd * 0.1f), plan.zone);
        // The office over the laundry: its floor, the stair up from the back room along the west wall, the sill lip
        Floor(root, f, new Vector3(0f, 3.9f, (-hd + frontZ) * 0.5f), new Vector3(hw * 2f, 1f, frontZ + hd), "OfficeFloor");
        Ramp(root, f, -hw + 2.5f, 4f, frontZ, 4.4f, frontZ + 13.5f, 0f, LevelKit.Floor, true);   // up from the back room to the office's edge
        Lip(root, f, new Vector3(hw - 1.2f, 4.4f + 0.6f, -hd + 5f), new Vector3(2f, 1.2f, 4f), LevelKit.WoodPanel, "Sill");
        Fuel(root, config, f, EnemyKind.Chaser, new Vector3(hw * 0.3f, 4.4f, -hd + 5f), "the office");
        Lamp(root, f, new Vector3(0f, 7f, 2f), h);
        Roof(root, f, h, hatch, 1, false);
        Facades(root, b, f, h, 1, "HALCYON LAUNDRY", new Vector2(hw * 0.35f, hw * 0.6f));   // beside the laundry door
        LevelKit.Awning(root, f.P(hw + 0.5f, 0f, -hd + 5f), f.D(Vector3.right), 6f);   // under the office window: the way up to it
        Entrance(plan, b, f, "the laundry door", new Vector3(-hw * 0.3f, 0f, -hd), Vector3.back, EK.Door);
        Entrance(plan, b, f, "the back door", new Vector3(0f, 0f, hd), Vector3.forward, EK.Door);
        Entrance(plan, b, f, "the coal chute", new Vector3(-hw, 0f, 0f), Vector3.left, EK.CellarHatch);
        Entrance(plan, b, f, "the office window", new Vector3(hw, 4.8f, -hd + 5f), Vector3.right, EK.Window, true);
        Entrance(plan, b, f, "roof hatch", new Vector3(hatch.x, h + 1f, hatch.y), Vector3.forward, EK.RoofHatch, true, false);
        if (string.IsNullOrEmpty(b.name)) b.name = "THE SPEAKEASY";
    }

    /// DINER. One room under a sign: the counter down the middle (a 1.1 m lip the length of the room), booths along
    /// the window wall (0.9 m lips), the kitchen out the back behind a partition with a 6.5 m pass, a kick-wall
    /// ladder in the kitchen's corner to the roof hatch, the big front WINDOW a pane of glass you crash through at
    /// speed. Doors: the front, the kitchen (back), the side (west).
    /// MAIN: in the front, over the counter or round it, out the kitchen. FAST: the window at speed; booth → counter
    /// corner boosts in a row. FUEL: a cook at the pass, a waitress behind the counter, two footmen in the booths.
    static void Diner(Transform root, MovementConfig config, Plan plan, Block b, BlockFrame f)
    {
        const float h = 5.5f;
        float hw = f.hw, hd = f.hd, kitchenZ = hd - 7f;
        Vector3 min = new Vector3(-hw, 0f, -hd), max = new Vector3(hw, h, hd);
        Vector2 hatch = new Vector2(hw - 3f, hd - 3f);
        Shell(root, f, h, hatch,
            Door(f, Face.South, hw * 0.4f, 0f, min, max), Door(f, Face.North, -hw * 0.3f, 0f, min, max), Door(f, Face.West, -hd * 0.3f, 0f, min, max),
            Window(f, Face.South, -hw * 0.25f, 9f, 0.9f, 4.2f, min, max));
        LevelKit.Glass(root, f.P(-hw * 0.25f, 2.55f, -hd), f.S(8.8f, 3.2f, 0.3f), config);
        Lip(root, f, new Vector3(0f, 0.55f, (-hd + kitchenZ) * 0.5f + 1f), new Vector3(1.4f, 1.1f, kitchenZ + hd - 8f), LevelKit.WoodPanel, "Counter");
        for (int i = 0; i < 3; i++)
            Lip(root, f, new Vector3(-hw + 1.6f, 0.45f, -hd + 3.5f + i * 4.5f), new Vector3(2.6f, 0.9f, 1.2f), LevelKit.Oxblood, "Booth");
        Box(root, f, new Vector3(-hw * 0.5f - 3.25f, h * 0.5f, kitchenZ), new Vector3(hw - 3.25f, h, 0.4f), LevelKit.Wall, "Partition");
        Box(root, f, new Vector3(hw * 0.5f + 3.25f, h * 0.5f, kitchenZ), new Vector3(hw - 3.25f, h, 0.4f), LevelKit.Wall, "Partition");
        Lip(root, f, new Vector3(0f, 0.55f, kitchenZ), new Vector3(6.5f, 1.1f, 0.6f), LevelKit.Brass, "Pass");   // the pass: a lip in the opening, slide under or hop it
        Kick(root, f, new Vector3(hw - 1.6f - 1.4f, 2.5f, hd - 3f), new Vector3(0.5f, 5f, 3f));
        Flute(root, f, new Vector3(hw, 2.5f, hd - 3f), Vector3.left, 4f);
        Line(root, b, f, "the window", new Vector3(-hw * 0.25f, 0f, -hd - 8f), new Vector3(-hw * 0.25f, 0f, -hd - 1f), plan.zone);
        Line(root, b, f, "the booths", new Vector3(-hw + 1.6f, 0f, -hd + 1f), new Vector3(-hw + 1.6f, 0f, -hd + 2.6f), plan.zone);
        Fuel(root, config, f, EnemyKind.Sentry, new Vector3(2.4f, 0f, kitchenZ + 2f), "the pass");
        Fuel(root, config, f, EnemyKind.Chaser, new Vector3(2f, 0f, -hd + 6f), "behind the counter");
        for (int i = 0; i < 2; i++) Fuel(root, config, f, EnemyKind.Dummy, new Vector3(-hw + 4.2f, 0f, -hd + 3.5f + i * 9f), "the booths");
        for (float z = -hd + 4f; z < kitchenZ - 2f; z += 6f) Lamp(root, f, new Vector3(0f, h - 1.2f, z), h);
        Roof(root, f, h, hatch, 1, false);
        Facades(root, b, f, h, 0, "DINER", new Vector2(-hw * 0.25f, 10.6f), false);   // the fascia and the awning over the real window
        LevelKit.BladeSign(root, f.P(hw * 0.4f + 4.5f, h + 1f, -hd - 0.5f), f.D(Vector3.back), "DINER", 0.7f);   // beside the door, from the cornice down to 2.4 m
        Entrance(plan, b, f, "the front door", new Vector3(hw * 0.4f, 0f, -hd), Vector3.back, EK.Door);
        Entrance(plan, b, f, "the kitchen door", new Vector3(-hw * 0.3f, 0f, hd), Vector3.forward, EK.Door);
        Entrance(plan, b, f, "the side door", new Vector3(-hw, 0f, -hd * 0.3f), Vector3.left, EK.Door);
        Entrance(plan, b, f, "the window", new Vector3(-hw * 0.25f, 0.9f, -hd), Vector3.back, EK.Window, true);
        Entrance(plan, b, f, "roof hatch", new Vector3(hatch.x, h + 1f, hatch.y), Vector3.forward, EK.RoofHatch, true, false);
        if (string.IsNullOrEmpty(b.name)) b.name = "THE DINER";
    }
}
