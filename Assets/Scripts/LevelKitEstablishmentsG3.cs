using UnityEngine;
using Plan = CityDistrict.DistrictPlan;
using Block = CityDistrict.Block;
using EK = CityDistrict.EntranceKind;

/// <summary>
/// HALCYON HOUSE'S OWN BLOCK (Level 5): THE CLUB. Local frame as the Arcade's (LevelKitEstablishmentsG.cs).
/// </summary>
public static partial class Establishments
{
    /// THE HALCYON CLUB (h 16). The members' club stands five metres off its −x boundary: THE MEWS, a four-metre lane
    /// between a six-metre garden wall and the club's flank, open at both ends and through a gap in the garden wall,
    /// with the DISCREET SIDE DOOR into the smoking room and a WINDOW over it at 5 (wall-kick the two walls: tech).
    /// Inside: the LOBBY under a marquee and the READING ROOM beside it (leather chairs as lips, a fire, the House's
    /// officer), the GRAND STAIR up the east wall to a landing at 4.6, THE LIBRARY two galleries high — rows of 2.4 m
    /// BOOK STACKS (authored corners, cream flutes: kick walls in lanes), the west galleries at 4.6 and 9.2 with curb
    /// lips and brass rails (a plinth lip under the first), the second flight along the back to a cross gallery at
    /// 9.2, the BOOST PILLAR PAIR in the stack well (a kick-wall ladder straight to the roof hatch) — the SMOKING ROOM
    /// and the BILLIARD ROOM (tables as lips) across the back, THE MEMBERS' TERRACE on the roof (pergola lips, hedges,
    /// a bar). MAIN: doors → the lobby → the stacks' lanes → the billiard room → the back door. FAST: the stair's
    /// banister, the galleries, the pillar pair, the mews. FUEL: doormen at the doors and the mews door, the officer
    /// in the reading room, footmen in the stacks and on the galleries, a page over the well, a bellhop on the cross
    /// gallery, a roster unit at the billiard tables.
    static void Club(Transform root, MovementConfig config, Plan plan, Block b, BlockFrame f)
    {
        const float h = 16f, g1 = 4.6f, g2 = 9.2f;
        float hw = f.hw, hd = f.hd, wx = -hw + 5f, mz = hd - 9.5f, backZ = hd - 12f, frontZ = -hd + 9f, cx = (wx + 0.5f + hw) * 0.5f;
        float gap0 = hd - 12f, gap1 = hd - 7f;
        Vector3 min = new Vector3(wx, 0f, -hd), max = new Vector3(hw, h, hd);
        Vector2 hatch = new Vector2(cx, 0.25f);
        var zone = plan.zone;
        Room(root, f, min, max, LevelKit.Faces.North | LevelKit.Faces.South | LevelKit.Faces.East | LevelKit.Faces.West,
            Door(f, Face.South, 3f, 0f, min, max, 8f, 5f), Door(f, Face.North, 10f, 0f, min, max),
            f.H(Face.West, mz - 1.5f, mz + 1.5f, 0f, 3.2f, min, max), f.H(Face.West, mz - 1.3f, mz + 1.3f, 5f, 8f, min, max));
        // THE MEWS: the garden wall in two pieces round its gap, coping, the flutes on both walls (the kick), bins, washing
        Box(root, f, new Vector3(-hw, 3f, (-hd + gap0) * 0.5f), new Vector3(1f, 6f, gap0 + hd), LevelKit.Limestone, "GardenWall");
        Box(root, f, new Vector3(-hw, 3f, (gap1 + hd) * 0.5f), new Vector3(1f, 6f, hd - gap1), LevelKit.Limestone, "GardenWall");
        Mark(root, f, new Vector3(-hw, 6.12f, (-hd + gap0) * 0.5f), new Vector3(1.3f, 0.24f, gap0 + hd), LevelKit.Limestone, "Coping");
        Mark(root, f, new Vector3(-hw, 6.12f, (gap1 + hd) * 0.5f), new Vector3(1.3f, 0.24f, hd - gap1), LevelKit.Limestone, "Coping");
        Flute(root, f, new Vector3(-hw + 0.5f, 3f, mz), Vector3.right, 4f, 0.5f);
        Flute(root, f, new Vector3(wx - 0.5f, 3.5f, mz - 3.5f), Vector3.left, 4f);
        LevelKit.Dumpster(root, f.P(-hw + 1.8f, 0f, mz - 6f), f.Yaw + 90f);
        LevelKit.Crates(root, f.P(-hw + 1.6f, 0f, mz + 4f), f.Yaw + 90f);
        LevelKit.WashingLine(root, f.P(-hw + 0.5f, 4.5f, mz - 2f), f.P(wx - 0.5f, 4.5f, mz - 2f), 3);
        LevelKit.WallLight(root, f.P(wx - 0.55f, 3.6f, mz + 2.6f), f.D(Vector3.left));
        Lip(root, f, new Vector3(wx + 1.4f, 4.5f, mz), new Vector3(1.8f, 1f, 3.6f), LevelKit.WoodPanel, "Sill");   // under the window, inside
        Fuel(root, config, f, EnemyKind.Sentry, new Vector3(wx + 3.5f, 0f, mz), "the mews door");
        Line(root, b, f, "the mews", new Vector3(-hw + 2f, 0f, -hd - 6f), new Vector3(-hw + 2f, 0f, -hd + 2f), zone);
        // THE LOBBY and THE READING ROOM: the partition to the library with its arch, the desk, the chairs, the fire
        Box(root, f, new Vector3(-10.875f, h * 0.5f, frontZ), new Vector3(4.25f, h, 0.5f), LevelKit.Wall, "Partition");
        Box(root, f, new Vector3(-0.125f, h * 0.5f, frontZ), new Vector3(4.25f, h, 0.5f), LevelKit.Wall, "Partition");
        Box(root, f, new Vector3(-5.5f, (h + 4.25f) * 0.5f, frontZ), new Vector3(6.5f, h - 4.25f, 0.5f), LevelKit.Wall, "Partition");
        Mark(root, f, new Vector3(-5.5f, 4.4f, frontZ), new Vector3(7f, 0.3f, 0.7f), LevelKit.Brass, "Frame");
        Lip(root, f, new Vector3(9f, 0.55f, -hd + 4f), new Vector3(4f, 1.1f, 1.2f), LevelKit.WoodPanel, "Desk");
        for (int i = 0; i < 6; i++) Lip(root, f, new Vector3(-10.5f + (i % 3) * 3.2f, 0.45f, -hd + 3.5f + (i / 3) * 3.2f), new Vector3(1.1f, 0.9f, 1.1f), LevelKit.Oxblood, "Armchair");
        Box(root, f, new Vector3(-6f, 1.3f, -hd + 0.85f), new Vector3(3.2f, 2.6f, 0.7f), LevelKit.Charcoal, "Fireplace");
        Mark(root, f, new Vector3(-6f, 2.7f, -hd + 0.95f), new Vector3(3.6f, 0.2f, 0.9f), LevelKit.Brass, "Mantel");
        Mark(root, f, new Vector3(-6f, 0.6f, -hd + 1.22f), new Vector3(1.6f, 1f, 0.06f), LevelKit.WindowAmber, "Fire");
        LevelKit.Light(root, f.P(-6f, 1f, -hd + 1.8f), 9f, 1.2f).color = LevelKit.WindowAmberLight;
        Fuel(root, config, f, EnemyKind.Marshal, new Vector3(-6f, 0f, -hd + 5.2f), "the reading room");
        Fuel(root, config, f, EnemyKind.Sentry, new Vector3(3f, 0f, -hd + 3f), "the doors");
        // THE GRAND STAIR up the east wall to the landing at 4.6; the second flight along the back to the cross gallery at 9.2
        Ramp(root, f, hw - 3f, 6f, -hd + 2f, 0f, -hd + 16f, g1, LevelKit.Floor, true);
        Banister(root, f, hw - 6.7f, -hd + 2f, 0f, -hd + 16f, g1);
        Floor(root, f, new Vector3(hw - 1.6f, g1 - 0.5f, 0.3f), new Vector3(3.2f, 1f, 9.6f), "Landing");
        HouseKit.Rail(root, f.P(hw - 3.2f, g1, -4.3f), f.P(hw - 3.2f, g1, 1.7f), 1f);
        Slope(root, f, new Vector3(hw - 3.2f, g1, 3.5f), new Vector3(hw - 13.2f, g2, 3.5f), 3.2f, LevelKit.Floor, "Flight", true);   // posts under the flight: it crosses the library's open floor
        for (int e = -1; e <= 1; e += 2) HouseKit.Rail(root, f.P(hw - 3.2f, g1 + 0.05f, 3.5f + e * 1.6f), f.P(hw - 13.2f, g2 + 0.05f, 3.5f + e * 1.6f), 1f);
        Fuel(root, config, f, EnemyKind.Dummy, new Vector3(hw - 3f, 2.3f, -hd + 9f), "the grand stair");
        Line(root, b, f, "the grand stair", new Vector3(hw - 3f, 0f, -hd + 0.5f), new Vector3(hw - 3f, 0f, -hd + 1.8f), zone, false);
        // THE LIBRARY: the west galleries at 4.6 and 9.2 (curb lips, rails, the plinth lip under the first), the cross
        // gallery at 9.2 along the back in two pieces round the flight's top, the stacks in two groups, the pillar pair
        Floor(root, f, new Vector3(-11.4f, g1 - 0.5f, 0.6f), new Vector3(3.2f, 1f, 15.2f), "Gallery");
        Floor(root, f, new Vector3(-11.4f, g2 - 0.5f, -4.55f), new Vector3(3.2f, 1f, 12.9f), "Gallery");
        Floor(root, f, new Vector3(-3.85f, g2 - 0.5f, 5.2f), new Vector3(18.3f, 1f, 6.6f), "Gallery");
        Floor(root, f, new Vector3((5.3f + hw) * 0.5f, g2 - 0.5f, 6.9f), new Vector3(hw - 5.3f, 1f, 3.2f), "Gallery");
        Lip(root, f, new Vector3(-9.6f, g1 + 0.2f, 0.6f), new Vector3(0.4f, 0.4f, 14.8f), LevelKit.Brass, "GalleryCurb");
        Lip(root, f, new Vector3(-9.6f, g2 + 0.2f, -4.55f), new Vector3(0.4f, 0.4f, 12.5f), LevelKit.Brass, "GalleryCurb");
        Lip(root, f, new Vector3(-3.85f, g2 + 0.2f, 2.1f), new Vector3(17.9f, 0.4f, 0.4f), LevelKit.Brass, "GalleryCurb");
        HouseKit.Rail(root, f.P(-9.65f, g1 + 0.4f, -6.8f), f.P(-9.65f, g1 + 0.4f, 8f), 0.9f);
        HouseKit.Rail(root, f.P(-9.65f, g2 + 0.4f, -10.8f), f.P(-9.65f, g2 + 0.4f, 1.7f), 0.9f);
        HouseKit.Rail(root, f.P(-12.8f, g2 + 0.4f, 2.1f), f.P(5.1f, g2 + 0.4f, 2.1f), 0.9f);
        Lip(root, f, new Vector3(-11.4f, 0.65f, -9.5f), new Vector3(3f, 1.3f, 3f));
        for (int i = 0; i < 6; i++)
        {
            float x = -7f + i * 3.5f;
            HouseKit.Stack(root, f.P(x, 1.2f, -6.25f), f.S(0.6f, 2.4f, 8.5f));
            HouseKit.Stack(root, f.P(x, 1.2f, 5.25f), f.S(0.6f, 2.4f, 5.5f));
        }
        for (int s = -1; s <= 1; s += 2) Corner(root, f, new Vector3(cx + s * 1.4f, h * 0.5f - 0.03f, 0.25f), new Vector3(0.6f, h - 0.06f, 2.4f), LevelKit.Charcoal);
        LevelKit.Chandelier(root, f.P(cx, 11.5f, -5f), f.origin.y + h);
        Fuel(root, config, f, EnemyKind.Turret, new Vector3(-3f, 7f, 0.25f), "the well");
        Fuel(root, config, f, EnemyKind.Dummy, new Vector3(1.75f, 0f, -6f), "the stacks");
        Fuel(root, config, f, EnemyKind.Dummy, new Vector3(-11.4f, g1, 2f), "the west gallery");
        Fuel(root, config, f, EnemyKind.Chaser, new Vector3(-2f, g2, 5f), "the cross gallery");
        Line(root, b, f, "the stacks", new Vector3(1.75f, 0f, -10.5f), new Vector3(1.75f, 0f, -2.5f), zone);
        Line(root, b, f, "the pillar pair", new Vector3(cx, 0f, -2.2f), new Vector3(cx, 0f, -0.4f), zone);
        // THE SMOKING ROOM and THE BILLIARD ROOM across the back: the partition with two arches, the divider, the bar,
        // the chairs, the tables, the back door
        Box(root, f, new Vector3(-10.875f, h * 0.5f, backZ), new Vector3(4.25f, h, 0.5f), LevelKit.Wall, "Partition");
        Box(root, f, new Vector3(2.25f, h * 0.5f, backZ), new Vector3(9f, h, 0.5f), LevelKit.Wall, "Partition");
        Box(root, f, new Vector3((13.25f + hw) * 0.5f, h * 0.5f, backZ), new Vector3(hw - 13.25f, h, 0.5f), LevelKit.Wall, "Partition");
        Box(root, f, new Vector3(-5.5f, (h + 4.25f) * 0.5f, backZ), new Vector3(6.5f, h - 4.25f, 0.5f), LevelKit.Wall, "Partition");
        Box(root, f, new Vector3(10f, (h + 4.25f) * 0.5f, backZ), new Vector3(6.5f, h - 4.25f, 0.5f), LevelKit.Wall, "Partition");
        Box(root, f, new Vector3(cx, h * 0.5f, (backZ + 0.25f + hd) * 0.5f), new Vector3(0.5f, h, hd - backZ - 0.25f), LevelKit.Wall, "Divider");
        for (int i = 0; i < 4; i++) Lip(root, f, new Vector3(-9f + (i % 2) * 4f, 0.45f, 12f + (i / 2) * 3.4f), new Vector3(1.1f, 0.9f, 1.1f), LevelKit.Oxblood, "Armchair");
        Lip(root, f, new Vector3(-5.5f, 0.55f, hd - 1.3f), new Vector3(10f, 1.1f, 1.2f), LevelKit.WoodPanel, "Bar");
        HouseKit.Rail(root, f.P(-10.4f, 0f, hd - 1.95f), f.P(-0.6f, 0f, hd - 1.95f), 0.25f);
        Lip(root, f, new Vector3(10f, 0.45f, 11.5f), new Vector3(3f, 0.9f, 1.6f), LevelKit.WoodPanel, "BilliardTable");
        Lip(root, f, new Vector3(10f, 0.45f, 16f), new Vector3(3f, 0.9f, 1.6f), LevelKit.WoodPanel, "BilliardTable");
        Lip(root, f, new Vector3(15f, 0.45f, 13.5f), new Vector3(1.6f, 0.9f, 3f), LevelKit.WoodPanel, "BilliardTable");
        Fuel(root, config, f, EnemyKind.Dummy, new Vector3(14f, 0f, 10.5f), "the billiard room");
        PoolFuel(config, f, new Vector3(R(5f, 9f), 0f, 18f), "the billiard room");
        Lamp(root, f, new Vector3(-5.5f, 5.5f, 14f), h);
        Lamp(root, f, new Vector3(10f, 5.5f, 14f), h);
        Lamp(root, f, new Vector3(-6f, 5.5f, -hd + 5f), h);
        Lamp(root, f, new Vector3(9f, 5.5f, -hd + 5f), h);
        // THE MEMBERS' TERRACE: the deck over the club (not the mews) round the hatch, parapets, the stair head, pergolas
        Box(root, f, new Vector3((wx - 0.5f + cx - 1.5f) * 0.5f, h + 0.5f, 0f), new Vector3(cx - 1f - wx, 1f, (hd + T) * 2f), LevelKit.Charcoal, "RoofDeck");
        Box(root, f, new Vector3((cx + 1.5f + hw + T) * 0.5f, h + 0.5f, 0f), new Vector3(hw + T - cx - 1.5f, 1f, (hd + T) * 2f), LevelKit.Charcoal, "RoofDeck");
        Box(root, f, new Vector3(cx, h + 0.5f, (-hd - T + hatch.y - 1.5f) * 0.5f), new Vector3(3f, 1f, hatch.y - 1.5f + hd + T), LevelKit.Charcoal, "RoofDeck");
        Box(root, f, new Vector3(cx, h + 0.5f, (hatch.y + 1.5f + hd + T) * 0.5f), new Vector3(3f, 1f, hd + T - hatch.y - 1.5f), LevelKit.Charcoal, "RoofDeck");
        LevelKit.Parapet(root, f.P(wx - 0.5f + 0.35f, h + 1.45f, 0f), f.S(0.7f, 0.9f, (hd + T) * 2f - 1.6f));
        LevelKit.Parapet(root, f.P(hw + T - 0.35f, h + 1.45f, 0f), f.S(0.7f, 0.9f, (hd + T) * 2f - 1.6f));
        for (int e = -1; e <= 1; e += 2) LevelKit.Parapet(root, f.P((wx + 0.3f + hw + T - 0.8f) * 0.5f, h + 1.45f, e * (hd + T - 0.35f)), f.S(hw + T - 0.8f - wx - 0.3f, 0.9f, 0.7f));
        Kick(root, f, new Vector3(cx - 3f, h + 2.5f, hatch.y), new Vector3(2.6f, 3f, 3.4f), LevelKit.Brick);
        Lip(root, f, new Vector3(-8f, h + 1.6f, hd - 5f), new Vector3(6f, 1.2f, 3f), LevelKit.WoodPanel, "Pergola");
        Lip(root, f, new Vector3(10f, h + 1.6f, -hd + 6f), new Vector3(6f, 1.2f, 3f), LevelKit.WoodPanel, "Pergola");
        LevelKit.Hedge(root, f.P(-8f, h + 1f, -hd + 6f), f.S(6f, 0.9f, 1f));
        Lip(root, f, new Vector3(hw - 3f, h + 1.55f, 2f), new Vector3(1.4f, 1.1f, 8f), LevelKit.WoodPanel, "TerraceBar");
        LevelKit.KickStack(root, f.P(hw - 4f, h + 1f, hd - 4f), 3.6f, 1.6f);
        Fuel(root, config, f, EnemyKind.Dummy, new Vector3(-5f, h + 1f, 0.25f), "the terrace");
        // THE FACADES by hand: the front and the back span the club, not the mews; the flank over the garden wall
        float fw = hw + T * 0.5f - (wx - 0.5f), fx = (wx - 0.5f + hw + T * 0.5f) * 0.5f;
        foreach (int e in new[] { -1, 1 })
        {
            Vector3 at = f.P(fx, 0f, e * f.blockHD), outward = f.D(Vector3.forward * e);
            LevelKit.PunchedWindows(at, outward, fw - 2f, 7f, 2, 4.5f, b.bx * 7 + b.bz * 3 + e);
            LevelKit.StringCourse(at, outward, fw, 11.5f);
            LevelKit.Cornice(at, outward, fw, h + 1f);
        }
        Vector3 ea = f.P(f.blockHW, 0f, 0f), eo = f.D(Vector3.right);
        LevelKit.PunchedWindows(ea, eo, f.blockHD * 2f - 2f, 7f, 2, 4.5f, b.bx * 7 + b.bz * 3 + 5);
        LevelKit.StringCourse(ea, eo, f.blockHD * 2f, 11.5f);
        LevelKit.StringCourse(ea, eo, f.blockHD * 2f, 6.8f, 0.5f);
        LevelKit.Cornice(ea, eo, f.blockHD * 2f, h + 1f);
        Vector3 ma = f.P(wx - 0.5f, 0f, 0f), mo = f.D(Vector3.left);
        LevelKit.PunchedWindows(ma, mo, hd * 2f - 2f, 7f, 2, 4.5f, b.bx * 7 + b.bz * 3 + 7);
        LevelKit.StringCourse(ma, mo, hd * 2f, 11.5f);
        LevelKit.Cornice(ma, mo, hd * 2f, h + 1f);
        LevelKit.StringCourse(f.P(-f.blockHW, 0f, 0f), mo, f.blockHD * 2f, 5.4f, 0.4f);
        Mark(root, f, new Vector3(-hw - 0.55f, 2.2f, gap0 - 1.6f), new Vector3(0.06f, 0.8f, 2.4f), LevelKit.Brass, "Plaque");
        LevelKit.Text(root, f.P(-hw - 0.6f, 2.2f, gap0 - 1.6f), Quaternion.LookRotation(f.D(Vector3.right)), "MEMBERS ONLY", 0.3f, new Color(0.08f, 0.07f, 0.05f), "PlaqueText", 2.2f, true);
        LevelKit.Marquee(root, f.P(3f, 0f, -hd - 0.5f), f.D(Vector3.back), 10f, string.IsNullOrEmpty(b.name) ? "THE HALCYON CLUB" : b.name, 5.2f, 4.5f);
        LevelKit.BladeSign(root, f.P(hw * 0.6f, h - 1f, -hd - 0.5f), f.D(Vector3.back), "CLUB", 1.2f);
        Entrance(plan, b, f, "the doors", new Vector3(3f, 0f, -hd), Vector3.back, EK.Door);
        Entrance(plan, b, f, "the back door", new Vector3(10f, 0f, hd), Vector3.forward, EK.Door);
        Entrance(plan, b, f, "the mews door", new Vector3(wx, 0f, mz), Vector3.left, EK.Door);
        Entrance(plan, b, f, "the mews window", new Vector3(wx, 5f, mz), Vector3.left, EK.Window, true);
        Entrance(plan, b, f, "the terrace", new Vector3(cx, h + 1f, hatch.y), Vector3.forward, EK.RoofHatch, true, false);
        if (string.IsNullOrEmpty(b.name)) b.name = "THE HALCYON CLUB";
    }
}
