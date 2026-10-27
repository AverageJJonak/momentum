using UnityEngine;
using Block = CityDistrict.Block;

/// <summary>
/// THE HYDRO's dressing (LevelKitEstablishmentsH.cs has its bones): the white glazed tile DADO with its oxblood course
/// and brass rail, the names in tile mosaic, the BOILER ROOM (the drum a kick wall, the flue, the pipes and gauges,
/// the steam, the coal heap), the ROOF (the deck in pieces round the open skylight well, its curb, the stair head,
/// the chimney kick stack over the flue, the water tower, two glazed lanterns, the towels on the line) and the FRONT
/// (the Deco facades, the marquee canopy carrying the name, the blade sign, the roundel). Decor is 3 mm proud of
/// the face it sits on; everything walk-into-able is solid.
/// </summary>
public static partial class Establishments
{
    /// White glazed tile 2.4 m up every wall (either side of the doors), its top an oxblood course, a brass rail at
    /// a metre; the names in mosaic over the pools and across the back; roundels high on the side walls
    static void HydroDado(Transform root, BlockFrame f, float backZ, string name)
    {
        float hw = f.hw, hd = f.hd, ix = hw - 0.5f, iz = hd - 0.5f;   // the walls' inner faces
        float wx = -HydroLaneHW - HydroPoolW * 0.5f, px = HydroLaneHW + HydroPoolW * 0.5f;
        HydroDadoRun(root, f, new Vector3(-ix, 0f, -iz), new Vector3(-4.3f, 0f, -iz), Vector3.forward);
        HydroDadoRun(root, f, new Vector3(4.3f, 0f, -iz), new Vector3(ix, 0f, -iz), Vector3.forward);
        HydroDadoRun(root, f, new Vector3(-ix, 0f, iz), new Vector3(8.5f, 0f, iz), Vector3.back);
        HydroDadoRun(root, f, new Vector3(-ix, 0f, -iz), new Vector3(-ix, 0f, backZ - 0.5f), Vector3.right);
        HydroDadoRun(root, f, new Vector3(ix, 0f, -iz), new Vector3(ix, 0f, iz), Vector3.left);
        LevelKit.Mosaic(root, f.P(wx, 3.3f, -iz), f.D(Vector3.forward), "TEPIDARIUM", 6f, 0.9f);
        LevelKit.Mosaic(root, f.P(px, 3.3f, -iz), f.D(Vector3.forward), "THE PLUNGE", 6f, 0.9f);
        LevelKit.Mosaic(root, f.P(-8f, 6.5f, iz), f.D(Vector3.back), name, 10f, 1.4f);
        LevelKit.Mosaic(root, f.P(ix, 6.5f, 2f), f.D(Vector3.left), "COLD · TEPID · PLUNGE", 8f, 1.1f);
        LevelKit.Mosaic(root, f.P(-ix, 6.5f, -2f), f.D(Vector3.right), "HALCYON BATHS", 8f, 1.1f);
        LevelKit.Mosaic(root, f.P(0.5f, 3.3f, iz), f.D(Vector3.back), "STOKE HOLE", 4f, 0.8f);
        for (int s = -1; s <= 1; s += 2)
        {
            LevelKit.Roundel(root, f.P(s * ix, 9.5f, -8f), f.D(Vector3.left * s), "H", 2f);
            LevelKit.Roundel(root, f.P(s * ix, 9.5f, 4f), f.D(Vector3.left * s), "H", 2f);
        }
    }

    /// A run of tile from local `a` to `b` on a wall's inner face, `lIn` the way into the room
    static void HydroDadoRun(Transform root, BlockFrame f, Vector3 a, Vector3 b, Vector3 lIn)
    {
        Vector3 mid = (a + b) * 0.5f;
        float len = (b - a).magnitude;
        bool alongX = Mathf.Abs(b.x - a.x) > Mathf.Abs(b.z - a.z);
        Vector3 S(float across, float tall, float thick) => alongX ? new Vector3(across, tall, thick) : new Vector3(thick, tall, across);
        Mark(root, f, mid + lIn * 0.028f + Vector3.up * 1.2f, S(len, 2.4f, 0.05f), LevelKit.Tile, "TileDado");
        Mark(root, f, mid + lIn * 0.058f + Vector3.up * 2.28f, S(len, 0.24f, 0.05f), LevelKit.OxbloodTile, "TileCourse");
        Mark(root, f, mid + lIn * 0.09f + Vector3.up * 1.02f, S(len, 0.07f, 0.07f), LevelKit.Brass, "BrassRail");
    }

    /// THE BOILER ROOM across the back-east: the drum (a riveted kick wall with a domed top and an amber firebox), the
    /// flue up to the roof, brass steam mains along the walls with a valve and gauges, a steam vent, the coal heap (a
    /// lip), a pendant over it all
    static void HydroBoiler(Transform root, MovementConfig config, BlockFrame f, float backZ)
    {
        float hw = f.hw, hd = f.hd, ix = hw - 0.5f, iz = hd - 0.5f, bz = (backZ + hd) * 0.5f;
        Vector3 boiler = new Vector3(5.5f, 2f, bz + 0.5f);
        Kick(root, f, boiler, new Vector3(5f, 4f, 3.4f), LevelKit.RivetIron);
        Mark(root, f, new Vector3(5.5f, 4.3f, bz + 0.5f), new Vector3(3.2f, 0.5f, 3.2f), LevelKit.RivetIron, "BoilerDome", PrimitiveType.Cylinder);
        Mark(root, f, new Vector3(5.5f, 1.1f, bz + 0.5f - 1.7f - 0.03f), new Vector3(1.2f, 1f, 0.06f), LevelKit.WindowAmber, "Firebox");
        Mark(root, f, new Vector3(5.5f, 1.1f, bz + 0.5f - 1.7f - 0.05f), new Vector3(1.4f, 0.1f, 0.04f), LevelKit.RivetIron, "FireboxBar");
        Box(root, f, new Vector3(5.5f, 8f, bz + 0.5f), new Vector3(0.9f, 8f, 0.9f), LevelKit.Charcoal, "Flue");
        LevelKit.Gauge(root, f.P(3f - 0.02f, 2.6f, bz + 0.5f), f.D(Vector3.left));
        LevelKit.Gauge(root, f.P(5.5f, 2.6f, bz + 0.5f - 1.7f - 0.02f), f.D(Vector3.back));
        LevelKit.Pipe(root, f.P(8f, 3.4f, bz + 0.5f), f.P(ix - 0.3f, 3.4f, bz + 0.5f), 0.22f, LevelKit.Brass);
        LevelKit.Pipe(root, f.P(ix - 0.3f, 3.4f, bz + 0.5f), f.P(ix - 0.3f, 0.2f, bz + 0.5f), 0.22f, LevelKit.Brass);
        LevelKit.Pipe(root, f.P(3f, 4.7f, iz - 0.3f), f.P(ix - 0.3f, 4.7f, iz - 0.3f), 0.18f, LevelKit.Iron);
        LevelKit.Valve(root, f.P(11f, 3.4f, bz + 0.5f), f.D(Vector3.right));
        LevelKit.SteamVent(root, f.P(8.05f, 1.5f, bz - 0.4f), f.D(Vector3.right), 5f, 0.5f, 0.3f);
        Lip(root, f, new Vector3(13.5f, 0.45f, backZ + 1.6f), new Vector3(2.6f, 0.9f, 2.2f), LevelKit.Charcoal, "CoalHeap");
        Lamp(root, f, new Vector3(11f, 7f, bz), HydroH);
    }

    /// THE ROOF: the deck in four pieces round the open skylight well (over the slide: a drop onto it), the parapets,
    /// the well's curb with brass glazing bars on it, a stair head (a kick wall), the chimney kick stack over the flue
    /// and a second stack, the water tower, two glazed lanterns over the drop-ins (solid, a step), the towels
    static void HydroRoof(Transform root, Block b, BlockFrame f, float h, Vector2 well, float wellHalf, Zone zone)
    {
        float rw = f.hw + T, rd = f.hd + T, y = h + 0.5f, top = h + 1f;
        float x0 = well.x - wellHalf, x1 = well.x + wellHalf, z0 = well.y - wellHalf, z1 = well.y + wellHalf;
        float wx = -HydroLaneHW - HydroPoolW * 0.5f;
        LevelKit.Slab(root, f.P((-rw + x0) * 0.5f, y, 0f), f.S(x0 + rw, 1f, rd * 2f), LevelKit.Charcoal, "RoofDeck");
        LevelKit.Slab(root, f.P((x1 + rw) * 0.5f, y, 0f), f.S(rw - x1, 1f, rd * 2f), LevelKit.Charcoal, "RoofDeck");
        LevelKit.Slab(root, f.P((x0 + x1) * 0.5f, y, (-rd + z0) * 0.5f), f.S(x1 - x0, 1f, z0 + rd), LevelKit.Charcoal, "RoofDeck");
        LevelKit.Slab(root, f.P((x0 + x1) * 0.5f, y, (z1 + rd) * 0.5f), f.S(x1 - x0, 1f, rd - z1), LevelKit.Charcoal, "RoofDeck");
        LevelKit.Parapet(root, f.P(0f, top + 0.45f, -rd + 0.35f), f.S(rw * 2f - 1.6f, 0.9f, 0.7f));
        LevelKit.Parapet(root, f.P(0f, top + 0.45f, rd - 0.35f), f.S(rw * 2f - 1.6f, 0.9f, 0.7f));
        LevelKit.Parapet(root, f.P(-rw + 0.35f, top + 0.45f, 0f), f.S(0.7f, 0.9f, rd * 2f));
        LevelKit.Parapet(root, f.P(rw - 0.35f, top + 0.45f, 0f), f.S(0.7f, 0.9f, rd * 2f));
        // The well's curb (a half-metre lip round the opening), the brass bars on it, the stair head beside it
        for (int s = -1; s <= 1; s += 2)
        {
            LevelKit.Parapet(root, f.P(well.x + s * (wellHalf + 0.35f), top + 0.25f, well.y), f.S(0.7f, 0.5f, wellHalf * 2f + 1.4f));
            LevelKit.Parapet(root, f.P(well.x, top + 0.25f, well.y + s * (wellHalf + 0.35f)), f.S(wellHalf * 2f, 0.5f, 0.7f));
            Mark(root, f, new Vector3(well.x + s * (wellHalf + 0.35f), top + 0.5f + LevelKit.MarkProud + 0.02f, well.y), new Vector3(0.12f, 0.04f, wellHalf * 2f + 1.2f), LevelKit.Brass, "SkylightBar");
            Mark(root, f, new Vector3(well.x, top + 0.5f + LevelKit.MarkProud + 0.02f, well.y + s * (wellHalf + 0.35f)), new Vector3(wellHalf * 2f + 1.2f, 0.04f, 0.12f), LevelKit.Brass, "SkylightBar");
        }
        Kick(root, f, new Vector3(x0 - 2.2f, top + 1.5f, well.y), new Vector3(2.6f, 3f, 3.4f), LevelKit.Brick);
        Line(root, b, f, "the skylight", new Vector3(rw - 1.2f, top, well.y), new Vector3(x1 + 0.9f, top, well.y), zone);
        // The chimney over the flue (a kick stack), a second stack, the tank, the lanterns, the towels
        float bz = (f.hd - 6.5f + f.hd) * 0.5f;
        LevelKit.KickStack(root, f.P(5.5f, top, bz + 0.5f), 5f, 1.6f);
        LevelKit.KickStack(root, f.P(-12f, top, 11f), 4f, 1.6f);
        Vector3 foot = f.P(-9f, top, -8f);
        LevelKit.WaterTower(root, foot, 5.5f);
        LevelKit.Slab(root, foot + Vector3.up * 1.5f, new Vector3(1.4f, 3f, 1.4f), LevelKit.RivetIron, "TowerCore");
        foreach (float lz in new[] { -7f, 3.5f })
        {
            LevelKit.Slab(root, f.P(wx, top + 0.35f, lz), f.S(3f, 0.7f, 6f), LevelKit.FrostGlass, "RoofLantern");
            Mark(root, f, new Vector3(wx, top + 0.7f + 0.03f, lz), new Vector3(0.1f, 0.06f, 6.2f), LevelKit.Brass, "LanternRidge");
        }
        LevelKit.WashingLine(root, f.P(-14f, top + 2.8f, 8f), f.P(-4f, top + 2.8f, 8f), 4);
    }

    /// THE FRONT: the Deco facades (punched windows, string courses, the cornice), the marquee canopy over the doors
    /// carrying the name (a platform), the blade sign, the roundel over the doors; names in mosaic over the avenue
    /// door and the boiler door
    static void HydroFront(Transform root, Block b, BlockFrame f, float h, float avenueZ, float boilerX)
    {
        float hw = f.hw, hd = f.hd;
        Facades(root, b, f, h, 1, null, null);
        LevelKit.Marquee(root, f.P(0f, 0f, -hd - 0.5f), f.D(Vector3.back), 10f, b.name, 4.9f, 3.2f);
        LevelKit.BladeSign(root, f.P(hw * 0.55f, h - 1.2f, -hd - 0.5f), f.D(Vector3.back), "HYDRO", 1.1f);
        LevelKit.Roundel(root, f.P(0f, 10.8f, -hd - 0.5f), f.D(Vector3.back), "H", 2.2f);
        LevelKit.Mosaic(root, f.P(-hw - 0.5f, 5.2f, avenueZ), f.D(Vector3.left), "BATHS", 5f, 0.9f);
        LevelKit.Mosaic(root, f.P(boilerX, 5.2f, hd + 0.5f), f.D(Vector3.forward), "BOILER HOUSE", 5f, 0.9f);
    }
}
