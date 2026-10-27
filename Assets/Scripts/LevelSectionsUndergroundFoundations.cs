using UnityEngine;

/// <summary>
/// THE FOUNDATIONS as a course space (course-grammar; act2-underground): the CRYPT — the old city buried under the
/// new. The LOW ROAD is the buried street between the old shopfronts' columns (real corners) with tombs along its
/// walls (lips); the HIGH ROAD is the old street's deck 6.2 m over it, cut by three COLLAPSING SLABS (stand on one and
/// 0.4 s later it drops you onto the pad under it, which throws you on; back after six seconds) and ending in a parapet
/// over THE CHASM, where both roads stop: the low road's gap is 6 m (a run jump), the high road's leap 11 m and a drop
/// onto the landing; the chasm's floor is a catch with a pad back up. Nothing here kills; everything here moves you on.
/// </summary>
public static partial class LevelSections
{
    /// MAIN: the low road, the run jump over the chasm. FAST: the high road off its lip (the WALL COMBO) or the pad,
    /// over the slabs without stopping, the leap off its parapet; a slab dropped on purpose onto the pad below.
    /// FUEL: platelayers on the low road, gangers on the high, a page over the chasm, roster units on both.
    public static Cursor Crypt(Transform root, MovementConfig config, Cursor c, float len = 100f, float halfW = 18f, float height = 16f)
    {
        OpenBegin(c);
        len = Mathf.Max(len, 80f);
        halfW = Mathf.Max(halfW, 14f);
        height = Mathf.Max(height, 15f);
        float z0 = c.z, z1 = c.z + len, y = c.floorY;
        const float deckH = 6.2f, landing = 7f, gap = 6f, deep = 7f;
        float gapZ0 = z1 - landing - gap, gapZ1 = z1 - landing;
        OpenThreshold(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y - deep, z0), new Vector3(halfW, y + height, z1), OpenRoofed);   // the floor is the chasm's: the roads are laid over it
        // The low road to the chasm, the landing past it, the faces under both, the pad in the chasm
        LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, (z0 - T + gapZ0) * 0.5f), new Vector3(halfW * 2f + 2f * T, 1f, gapZ0 - z0 + T), "LowRoad");
        LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, (gapZ1 + z1 + T) * 0.5f), new Vector3(halfW * 2f + 2f * T, 1f, z1 + T - gapZ1), "Landing");
        LevelKit.WallFace(root, Face.North, gapZ0, -halfW - T * 0.5f, halfW + T * 0.5f, y - deep, y - 1f);
        LevelKit.WallFace(root, Face.South, gapZ1, -halfW - T * 0.5f, halfW + T * 0.5f, y - deep, y - 1f);
        LevelKit.JumpPad(root, new Vector3(0f, y - deep, gapZ0 + gap * 0.5f), 0f, 20f, 8f);
        LevelKit.Flute(root, new Vector3(-halfW * 0.3f, y - deep * 0.5f, gapZ0), Vector3.forward, 4f);
        LevelKit.Flute(root, new Vector3(halfW * 0.3f, y - deep * 0.5f, gapZ1), Vector3.back, 4f);
        LevelKit.BrassStrip(root, new Vector3(-halfW + 0.3f, y, gapZ0 - 0.12f), new Vector3(halfW - 0.3f, y, gapZ0 - 0.12f));
        TechMarkers.MarkLip(new Vector3(0f, y, gapZ1 + 0.5f), new Vector3(halfW * 2f, 0.9f, 1.2f));
        // The buried street: the old colonnade's columns down the middle (corners), the tombs along the walls (lips)
        for (int i = 0; i < 5; i++)
        {
            int s = i % 2 == 0 ? -1 : 1;
            LevelKit.BoostCorner(root, new Vector3(s * 5.5f, y + (deckH - 1f) * 0.5f, z0 + 10f + i * (gapZ0 - 20f - z0) / 4f), new Vector3(2f, deckH - 1f, 2.4f), LevelKit.Limestone);
        }
        for (int i = 0; i < 4; i++)
        {
            int s = i % 2 == 0 ? -1 : 1;
            LevelKit.Lip(root, new Vector3(s * (halfW - 2.2f), y + 0.5f, z0 + 14f + i * 18f), new Vector3(2.4f, 1f, 3.6f), LevelKit.Limestone, "Tomb");
        }
        // The high road: the deck in bands round three collapsing slabs, a pad under each on the low road, its end a
        // parapet over the chasm (a lip to boost off for the leap)
        float d0 = z0 + len * 0.2f, d1 = gapZ0 - 5f, dy = y + deckH - 0.5f, bandA = d0;
        float[] sk = { 0.3f, 0.52f, 0.74f };
        for (int i = 0; i < 3; i++)
        {
            float sz = Mathf.Lerp(d0, d1, sk[i]);
            if (sz - 2f - bandA > 0.2f) LevelKit.FloorSlab(root, new Vector3(0f, dy, (bandA + sz - 2f) * 0.5f), new Vector3(halfW * 2f, 1f, sz - 2f - bandA), "HighRoad");
            for (int s = -1; s <= 1; s += 2) LevelKit.FloorSlab(root, new Vector3(s * (halfW + 2f) * 0.5f, dy, sz), new Vector3(halfW - 2f, 1f, 4f), "HighRoad");
            LevelKit.CollapsingSlab(root, new Vector3(0f, y + deckH, sz), new Vector2(4f, 4f), y + 0.55f);
            LevelKit.JumpPad(root, new Vector3(0f, y, sz + 1.5f), 0f, 11f, 10f);
            bandA = sz + 2f;
        }
        if (d1 - bandA > 0.2f) LevelKit.FloorSlab(root, new Vector3(0f, dy, (bandA + d1) * 0.5f), new Vector3(halfW * 2f, 1f, d1 - bandA), "HighRoad");
        LevelKit.Parapet(root, new Vector3(0f, y + deckH + 0.45f, d1 - 0.4f), new Vector3(halfW * 2f - 0.4f, 0.9f, 0.6f));
        LevelKit.BrassStrip(root, new Vector3(-halfW + 0.3f, y + deckH, d0 + 0.2f), new Vector3(-halfW + 0.3f, y + deckH, d1 - 0.8f));
        // The way up: the plinth lip by the west wall, the WALL COMBO run before it, the pad beside it
        float lipX = -(halfW - 4f), lipZ = d0 - 3.5f;
        LevelKit.Lip(root, new Vector3(lipX, y + 0.7f, lipZ), new Vector3(6f, 1.4f, 3f));
        LevelKit.Flute(root, new Vector3(-halfW, y + 2.2f, lipZ - 18f), Vector3.right, 4f);
        LevelKit.Flute(root, new Vector3(-halfW, y + 2.2f, lipZ - 4f), Vector3.right, 4f);
        LevelKit.LineRun(root, new Vector3(-halfW + 0.5f, y, lipZ - 18f), new Vector3(-halfW + 0.5f, y, lipZ - 4f), c.zone);
        LevelKit.JumpPad(root, new Vector3(-(halfW - 10f), y, d0 - 5f), 0f, Mathf.Sqrt(2f * 25f * (deckH + 1.2f)), 6f);
        LevelKit.Orb(root, new Vector3(0f, y + height - 1.8f, (d0 + d1) * 0.5f));
        LevelKit.HoloSign(root, new Vector3(halfW - 0.15f, y + 3.4f, z0 + 6f), Vector3.left, 4.6f, 1.2f, "FLOORS UNSAFE  ·  KEEP MOVING", true);
        LevelKit.HoloSign(root, new Vector3(-halfW + 0.15f, y + deckH + 2.6f, d1 - 8f), Vector3.right, 4.2f, 1.2f, "THE CHASM", true);
        Flavour(UndergroundFlavour.Crypt, "OLD TOWN");
        denseLight = true;
        Dress(root, c, halfW, y, height, z0, z1);
        Course.Fuel(root, config, c, EnemyKind.Borer, new Vector3(0f, y, z0 + len * 0.3f), "the low road");
        Course.Fuel(root, config, c, EnemyKind.Borer, new Vector3(2f, y, z0 + len * 0.62f), "the low road");
        Course.Fuel(root, config, c, EnemyKind.Blaster, new Vector3(-6f, y + deckH, d0 + 6f), "the high road");
        Course.Fuel(root, config, c, EnemyKind.Blaster, new Vector3(6f, y + deckH, Mathf.Lerp(d0, d1, 0.62f)), "the high road");
        Course.Fuel(root, config, c, EnemyKind.Turret, new Vector3(0f, y + 4f, gapZ0 + gap * 0.5f), "over the chasm");
        Pool(config, new Vector3(R(-3f, 3f), y, z0 + len * 0.48f), "the low road");
        Pool(config, new Vector3(R(-6f, 6f), y + deckH, Mathf.Lerp(d0, d1, 0.42f)), "the high road");
        Postcard(new Vector3(-halfW * 0.6f, y + 4f, z0 + 5f), new Vector3(2f, y + 3f, z0 + len * 0.5f), 76f, signature: true);
        return OpenExit(c, z1, y, halfW, height);
    }
}
