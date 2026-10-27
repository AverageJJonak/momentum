using UnityEngine;

/// <summary>
/// THE SEWERS and THE STEAM MAINS as course spaces (course-grammar; act2-underground), the Underbelly zone's brick:
/// the WEIR RUN (a 25° brick channel under a foot of black water with weirs across it — lips on a slide — ending at an
/// edge the water falls over as a WATERFALL into the sump), the SLUICE COURT (the cistern: catwalk lanes over black
/// water with SLUICE GATES across them on a cycle, hook orbs down the middle, a high gallery broken by a waterfall)
/// and the STEAM GALLERY (the mains along the walls, STEAM VENTS in the floor that burst on a cycle and launch you —
/// the fast line is vent to vent up onto the pipe gallery — cross mains as lips to slide under or boost over).
/// </summary>
public static partial class LevelSections
{
    /// WEIR RUN. MAIN: the slide, over the weirs, off the edge into the sump, the pad on. FAST: jump on contact with
    /// each weir (a corner boost: the earlier the press, the more it pays), dash far off the edge, drop through the fall
    /// (it pushes you down: the quick way to the pad). FUEL: sweepers on the slide, a stoker in the sump, a page over the fall.
    public static Cursor WeirRun(Transform root, MovementConfig config, Cursor c, float drop = 20f, float len = 80f, float halfW = 8f)
    {
        OpenBegin(c);
        halfW = Mathf.Max(halfW, 7f);
        drop = Mathf.Clamp(drop, 14f, 32f);
        const float mouth = 6f, fall = 7f, sump = 18f, slope = 25f;
        float slideDrop = drop - fall, run = slideDrop / Mathf.Tan(slope * Mathf.Deg2Rad);
        len = Mathf.Max(len, mouth + run + sump + 2f);
        float z0 = c.z, z1 = c.z + len, y = c.floorY, low = y - drop, edgeY = y - slideDrop, height = drop + Course.HeadLips;
        float sz0 = z0 + mouth, sz1 = sz0 + run, sumpZ0 = sz1 + 2f;
        OpenThreshold(root, c, halfW, y, Course.Head);
        LevelKit.Room(root, new Vector3(-halfW, low, z0), new Vector3(halfW, low + height, z1), OpenWalled);
        LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, (z0 - T + sz0) * 0.5f), new Vector3(halfW * 2f + 2f * T, 1f, sz0 - z0 + T), "Mouth");
        LevelKit.Ramp(root, 0f, halfW * 2f, sz0, y, sz1, edgeY, LevelKit.Brick);
        LevelKit.FloorSlab(root, new Vector3(0f, edgeY - 0.5f, sz1 + 1f), new Vector3(halfW * 2f + 2f * T, 1f, 2f), "Edge");
        LevelKit.WallFace(root, Face.North, sumpZ0, -halfW - T * 0.5f, halfW + T * 0.5f, low, edgeY - 1f);
        LevelKit.FloorSlab(root, new Vector3(0f, low - 0.5f, (sumpZ0 + z1 + T) * 0.5f), new Vector3(halfW * 2f + 2f * T, 1f, z1 + T - sumpZ0), "Sump");
        // The water down the channel, the weirs across it, the egg's fillets banking its sides, the lit line
        Vector3 a = new Vector3(0f, y, sz0), b = new Vector3(0f, edgeY, sz1), d = (b - a).normalized;
        Quaternion along = Quaternion.LookRotation(d, Vector3.up);
        Vector3 normal = along * Vector3.up;
        Vector3 size = new Vector3(halfW * 2f - 0.4f, 0.04f, run - 0.4f);
        var sheet = LevelKit.Prim(root, (a + b) * 0.5f + normal * 0.04f, size, LevelKit.BlackWater, "ChannelWater", PrimitiveType.Cube, along);
        sheet.GetComponent<MeshRenderer>().sharedMaterial = LevelKit.Tiled(LevelKit.BlackWater, size);
        sheet.AddComponent<WaterFlow>().speed = new Vector2(0f, 0.5f);
        for (int i = 0; i < 3; i++) LevelKit.Weir(root, Vector3.Lerp(a, b, 0.28f + 0.26f * i), d, halfW * 2f - 0.6f, 0.45f);
        for (int s = -1; s <= 1; s += 2)
        {
            var fillet = LevelKit.Slab(root, Vector3.Lerp(a, b, 0.5f) + new Vector3(s * halfW, 0f, 0f), new Vector3(1.6f, 1.6f, run), LevelKit.Brick, "EggFillet");
            fillet.transform.rotation = along * Quaternion.Euler(0f, 0f, 45f);
            LevelKit.Flute(root, new Vector3(s * halfW, Mathf.Lerp(y, edgeY, 0.5f) + 2.2f, (sz0 + sz1) * 0.5f), Vector3.left * s, 4f);
        }
        LevelKit.LineRun(root, new Vector3(0f, y, sz0 + 1f), new Vector3(0f, edgeY, sz1 - 1f), c.zone);
        // The fall, the sump's black water, the pad on
        LevelKit.Waterfall(root, new Vector3(0f, edgeY, sumpZ0), halfW * 2f - 1f, fall, true);
        LevelKit.Water(root, new Vector3(0f, low, (sumpZ0 + z1) * 0.5f), new Vector2(halfW * 2f - 1f, z1 - sumpZ0 - 1f));
        LevelKit.JumpPad(root, new Vector3(0f, low, z1 - 3f), 0f, 9f, 9f);
        LevelKit.Orb(root, new Vector3(0f, edgeY + 4f, sumpZ0 + 3f));
        LevelKit.HoloSign(root, new Vector3(-halfW + 0.15f, y + 3.2f, z0 + 3f), Vector3.right, 4.4f, 1.2f, "WEIR RUN  ·  HIGH WATER", true);
        DressSewer(root, c, halfW, low, height, z0, z1);
        for (int i = 0; i < 2; i++) Course.Fuel(root, config, c, EnemyKind.Scourer, Vector3.Lerp(a, b, 0.4f + 0.25f * i) + new Vector3(i == 0 ? -2.5f : 2.5f, 0.25f, 0f), "the slide");
        Course.Fuel(root, config, c, EnemyKind.Kettler, new Vector3(2f, low, z1 - 8f), "the sump");
        Pool(config, new Vector3(0f, edgeY + 3f, sumpZ0 + 1f), "over the fall", true);
        Postcard(new Vector3(-halfW * 0.6f, y + 3.5f, z0 + 3f), new Vector3(0f, edgeY + 1f, sz1), 76f, signature: true);
        return OpenExit(c, z1, low, halfW, height);
    }

    /// SLUICE COURT. The cistern: the floor under a foot of black water; two catwalk lanes 2.5 m over it off 1.2 m lips,
    /// three crossings between them, ramps down before the end; a SLUICE GATE across each lane twice (open 3 s in 8,
    /// alternating), the sluice piers on their plinths (lips), the crane's hooks down the middle as orbs; and along the
    /// east wall a high gallery at 7 m (a pad up) broken in the middle by the inlet's WATERFALL: jump the gap, or drop
    /// through the fall onto the lane below. MAIN: wade the lane between the plinths. FAST: the lanes timed through the
    /// sluices; the hooks; the gallery. FUEL: housekeeping on the lanes, a sweeper and a platelayer in the water, an
    /// inspector over the sluices, a stoker on the gallery.
    public static Cursor SluiceCourt(Transform root, MovementConfig config, Cursor c, float len = 100f, float halfW = 24f, float height = 16f)
    {
        OpenBegin(c);
        len = Mathf.Max(len, 80f);
        halfW = Mathf.Max(halfW, 18f);
        height = Mathf.Max(height, 14f);
        float z0 = c.z, z1 = c.z + len, y = c.floorY, mid = z0 + len * 0.5f;
        OpenThreshold(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), OpenRoofed);
        LevelKit.Water(root, new Vector3(0f, y, mid), new Vector2(halfW * 2f - 1f, len - 2f));
        const float lx = 9f, lw = 3f, catH = 2.5f;
        float c0 = z0 + 12f, c1 = z1 - 6f - Run18(catH);
        for (int s = -1; s <= 1; s += 2)
        {
            LevelKit.FloorSlab(root, new Vector3(s * lx, y + catH - 0.5f, (c0 + c1) * 0.5f), new Vector3(lw, 1f, c1 - c0), "SluiceLane");
            LevelKit.Ramp(root, s * lx, lw, c1, y + catH, z1 - 6f, y);
            LevelKit.BrassStrip(root, new Vector3(s * (lx + lw * 0.5f - 0.1f), y + catH, c0 + 0.3f), new Vector3(s * (lx + lw * 0.5f - 0.1f), y + catH, c1 - 0.3f));
            LevelKit.Lip(root, new Vector3(s * lx, y + 0.6f, c0 - 3f), new Vector3(lw, 1.2f, 2.4f));
            LevelKit.LineRun(root, new Vector3(s * lx, y, z0 + 3f), new Vector3(s * lx, y, c0 - 4.4f), c.zone);
            LevelKit.SluiceGate(root, new Vector3(s * lx, y + catH, z0 + len * 0.42f), 0f, lw + 0.4f, 3f, 8f, 3f, s < 0 ? 0f : 0.5f);
            LevelKit.SluiceGate(root, new Vector3(s * lx, y + catH, z0 + len * 0.74f), 0f, lw + 0.4f, 3f, 8f, 3f, s < 0 ? 0.25f : 0.75f);
        }
        for (int i = 0; i < 3; i++)
            LevelKit.Lip(root, new Vector3(0f, y + catH + 0.9f, z0 + len * (0.3f + 0.2f * i)), new Vector3(lx * 2f + lw, 0.4f, 3f), LevelKit.RivetIron, "SluiceCross");
        Vector3[] piers = { new Vector3(0f, y, z0 + len * 0.4f), new Vector3(0f, y, z0 + len * 0.7f), new Vector3(-halfW + 4.5f, y, z0 + len * 0.55f) };
        for (int i = 0; i < piers.Length; i++)
        {
            LevelKit.Lip(root, piers[i] + Vector3.up * 0.7f, new Vector3(4.2f, 1.4f, 4.2f), LevelKit.Brick, "SluicePier");
            LevelKit.Pump(root, piers[i] + Vector3.up * 1.4f, i * 90f);
            LevelKit.SteamVent(root, piers[i] + new Vector3(0.9f, 6.7f, 0.9f), new Vector3(0.3f, 1f, 0.3f), 6f + i, i * 0.25f, 0.3f);
        }
        // The east gallery and the inlet's fall through its gap
        const float gh = 7f, gw = 3.6f;
        float gx = halfW - gw * 0.5f - 0.3f, g0 = z0 + len * 0.25f, g1 = z1 - 4f - Run18(gh), gap = z0 + len * 0.6f;
        LevelKit.FloorSlab(root, new Vector3(gx, y + gh - 0.5f, (g0 + gap - 2f) * 0.5f), new Vector3(gw, 1f, gap - 2f - g0), "InletGallery");
        LevelKit.FloorSlab(root, new Vector3(gx, y + gh - 0.5f, (gap + 2f + g1) * 0.5f), new Vector3(gw, 1f, g1 - gap - 2f), "InletGallery");
        LevelKit.Ramp(root, gx, gw, g1, y + gh, z1 - 4f, y);
        LevelKit.BrassStrip(root, new Vector3(gx - gw * 0.5f + 0.1f, y + gh, g0 + 0.3f), new Vector3(gx - gw * 0.5f + 0.1f, y + gh, gap - 2.3f));
        LevelKit.BrassStrip(root, new Vector3(gx - gw * 0.5f + 0.1f, y + gh, gap + 2.3f), new Vector3(gx - gw * 0.5f + 0.1f, y + gh, g1 - 0.3f));
        LevelKit.JumpPad(root, new Vector3(gx, y, g0 - 5f), 0f, Mathf.Sqrt(2f * 25f * (gh + 1.2f)), 6f);
        LevelKit.Prim(root, new Vector3(gx, y + height - 1f, gap), new Vector3(3.2f, 2f, 3.2f), LevelKit.Brick, "InletCulvert", PrimitiveType.Cylinder);
        LevelKit.Waterfall(root, new Vector3(gx, y + height - 1.6f, gap), 3f, height - 1.7f, true);
        LevelKit.HoloSign(root, new Vector3(halfW - 0.15f, y + gh + 3f, gap - 6f), Vector3.left, 4.2f, 1.2f, "INLET  ·  MIND THE GAP", true);
        // Overhead: the hoist beam down the middle, three hooks on their chains, the orbs at them; the pipes and ducts
        float beamY = y + height - 3f;
        LevelKit.Beam(root, new Vector3(0f, beamY, z0 + 8f), new Vector3(0f, beamY, z1 - 8f), 0.8f, 0.8f, LevelKit.Girder);
        for (int i = 0; i < 3; i++) HookOrb(root, new Vector3(0f, beamY - 0.4f, z0 + len * (0.28f + 0.22f * i)), height - 11.3f);
        LevelKit.Duct(root, new Vector3(-halfW, y + height - 1.5f, z0 + len * 0.3f), new Vector3(halfW, y + height - 1.5f, z0 + len * 0.3f), 1.2f, 0.8f);
        LevelKit.Drips(root, new Vector3(-halfW + 1f, y + height - 1.6f, z0 + 2f), new Vector3(halfW - 1f, y + height - 1f, z1 - 2f));
        LevelKit.HoloSign(root, new Vector3(0f, y + Mathf.Min(height - 1.5f, 7.4f), z1 - 1.5f), Vector3.back, 9f, 1.8f, "CISTERN  ·  SLUICES OPEN 3 IN 8");
        LevelKit.Stencil(root, new Vector3(-halfW + 0.1f, y + 2.6f, z0 + 6f), Vector3.right, "WADERS BEYOND THIS POINT", 0.09f, 8f);
        Postcard(new Vector3(halfW * 0.7f, y + 6.5f, z0 + 5f), new Vector3(-2f, y + 2f, z0 + len * 0.55f), 76f, signature: true);
        DressSewer(root, c, halfW, y, height, z0, z1);
        Course.Fuel(root, config, c, EnemyKind.DamageControl, new Vector3(-lx, y + catH, c0 + 8f), "the west lane");
        Course.Fuel(root, config, c, EnemyKind.DamageControl, new Vector3(lx, y + catH, z0 + len * 0.6f), "the east lane");
        Course.Fuel(root, config, c, EnemyKind.Scourer, new Vector3(4f, y, z0 + len * 0.3f), "the water");
        Course.Fuel(root, config, c, EnemyKind.Borer, new Vector3(-4f, y, z0 + len * 0.58f), "the water");
        Course.Fuel(root, config, c, EnemyKind.Overseer, new Vector3(0f, y + 9f, z0 + len * 0.5f), "over the sluices");
        Course.Fuel(root, config, c, EnemyKind.Kettler, new Vector3(gx, y + gh, g0 + 8f), "the inlet gallery");
        Pool(config, new Vector3(-halfW * 0.5f, y + 9f, z0 + len * 0.75f), "the pipework", true);
        return OpenExit(c, z1, y, halfW, height);
    }

    /// STEAM GALLERY. The mains along both walls (three a side, brass valves and cream dials), the pipe gallery over the
    /// west mains at 6 m (the WALL COMBO) and a lower one over the east at 3.2 (a perfect boost), six STEAM VENTS zigzag
    /// down the lane on a six-second cycle — a 2 s hiss, a 0.4 s burst that launches you 12 m/s up: timed, vent to vent
    /// is the way up to the galleries — and three cross mains over the lane: two a metre up (slide under, or corner
    /// boost over) and one on the floor (a hop). MAIN: the lane, under and over the mains. FAST: the vents, the galleries.
    /// FUEL: stokers at the valves, a signalman on the west gallery, a ganger on the east, roster units in the lane.
    public static Cursor SteamGallery(Transform root, MovementConfig config, Cursor c, float len = 100f, float halfW = 14f, float height = 12f)
    {
        OpenBegin(c);
        len = Mathf.Max(len, 80f);
        halfW = Mathf.Max(halfW, 12f);
        height = Mathf.Max(height, 11f);
        float z0 = c.z, z1 = c.z + len, y = c.floorY, mid = z0 + len * 0.5f;
        OpenThreshold(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), OpenRoofed);
        for (int s = -1; s <= 1; s += 2)
        {
            float x = s * (halfW - 0.75f);
            LevelKit.Pipe(root, new Vector3(x, y + 1.6f, z0 + 1f), new Vector3(x, y + 1.6f, z1 - 1f), 1.2f);
            LevelKit.Pipe(root, new Vector3(x, y + 3f, z0 + 1f), new Vector3(x, y + 3f, z1 - 1f), 0.9f);
            LevelKit.Pipe(root, new Vector3(x, y + 4.3f, z0 + 1f), new Vector3(x, y + 4.3f, z1 - 1f), 0.9f);
            for (float z = z0 + 10f; z < z1 - 6f; z += 16f)
            {
                LevelKit.Valve(root, new Vector3(x - s * 0.7f, y + 1.6f, z), Vector3.forward);
                LevelKit.Gauge(root, new Vector3(s * (halfW - 0.05f), y + 5.4f, z + 3f), Vector3.left * s);
            }
        }
        Catwalk(root, config, c, -1, 6f, 3.4f, halfW, y, z0 + len * 0.35f, z1 - 4f, "the west gallery");
        Catwalk(root, config, c, 1, 3.2f, 3.4f, halfW, y, z0 + len * 0.5f, z1 - 4f, "the east gallery");
        for (int i = 0; i < 6; i++)
            LevelKit.SteamVentPad(root, new Vector3(i % 2 == 0 ? -3.5f : 3.5f, y, z0 + len * (0.16f + 0.12f * i)), 6f, i * 0.17f);
        LevelKit.Lip(root, new Vector3(0f, y + 1.4f, z0 + len * 0.3f), new Vector3(halfW * 2f - 3f, 0.8f, 0.8f), LevelKit.RivetIron, "CrossMain");
        LevelKit.Lip(root, new Vector3(0f, y + 0.6f, z0 + len * 0.58f), new Vector3(halfW * 2f - 3f, 1.2f, 1.2f), LevelKit.RivetIron, "CrossMain");
        LevelKit.Lip(root, new Vector3(0f, y + 1.4f, z0 + len * 0.84f), new Vector3(halfW * 2f - 3f, 0.8f, 0.8f), LevelKit.RivetIron, "CrossMain");
        LevelKit.LineRun(root, new Vector3(-3.5f, y, z0 + 4f), new Vector3(-3.5f, y, z0 + len * 0.16f - 1.6f), c.zone);
        LevelKit.HoloSign(root, new Vector3(0f, y + Mathf.Min(height - 1.5f, 7.4f), z1 - 1.5f), Vector3.back, 9f, 1.8f, "STEAM MAINS  ·  140 % OF RATED", true);
        LevelKit.Stencil(root, new Vector3(halfW - 0.1f, y + 5.8f, z0 + 8f), Vector3.left, "VENTS BLOW ON THE SIX  ·  DO NOT STAND IN THEM", 0.08f, 9f);
        if (c.zone == Zone.Underbelly) DressSewer(root, c, halfW, y, height, z0, z1);
        else { Flavour(UndergroundFlavour.Tunnel, "THE HOUSE MAINS"); Dress(root, c, halfW, y, height, z0, z1); }
        Course.Fuel(root, config, c, EnemyKind.Kettler, new Vector3(-(halfW - 3f), y, z0 + len * 0.26f), "the west valves");
        Course.Fuel(root, config, c, EnemyKind.Kettler, new Vector3(halfW - 3f, y, z0 + len * 0.74f), "the east valves");
        Course.Fuel(root, config, c, EnemyKind.Rigger, new Vector3(-(halfW - 2f), y + 6f, z0 + len * 0.35f + 16f), "the west gallery");
        Course.Fuel(root, config, c, EnemyKind.Blaster, new Vector3(halfW - 2f, y + 3.2f, z0 + len * 0.5f + 12f), "the east gallery");
        Pool(config, new Vector3(0f, y, z0 + len * 0.45f), "the lane");
        Pool(config, new Vector3(0f, y + 6f, z0 + len * 0.7f), "over the lane", true);
        Postcard(new Vector3(-halfW * 0.5f, y + 4f, z0 + 5f), new Vector3(2f, y + 1.5f, mid), 76f);
        return OpenExit(c, z1, y, halfW, height);
    }
}
