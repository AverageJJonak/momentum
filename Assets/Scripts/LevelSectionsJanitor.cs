using UnityEngine;

/// <summary>
/// SANITATION: the washdown court the Janitor keeps, opened into the course (course-grammar). A wet floor with the
/// drain in the middle of it, troughs and drums to boost off, hoses along the walls, wet-floor signs, the sanitation
/// sign over the way out — and a side gallery along the west wall (3.2 m: a PERFECT corner boost on its plinth lip)
/// that ramps back down to the lane before the end. Nothing closes. The Janitor is fuel like everyone else: it stands
/// on the main line past the drain with two housekeepers and a scrubber, and cutting through them is the point.
/// </summary>
public static partial class LevelSections
{
    /// MAIN: the wet floor past the drain, 14 m clear. FAST: the west gallery; the troughs (lips) to corner-boost.
    /// FUEL: the Janitor past the drain, housekeepers at the hoses and the drums, a scrubber on the wet floor.
    public static Cursor Sanitation(Transform root, MovementConfig config, Cursor c, float len = 70f, float halfW = 20f, float height = 10f)
    {
        OpenBegin(c);
        len = Mathf.Max(len, 60f);
        halfW = Mathf.Max(halfW, 14f);
        height = Mathf.Max(height, 9f);
        float z0 = c.z, z1 = c.z + len, y = c.floorY;
        OpenThreshold(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), OpenRoofed);
        Vector3 mid = new Vector3(0f, y, z0 + len * 0.5f);

        // The drain and its hazard square, the wet floor, hoses coiled along the walls
        LevelKit.Grate(root, mid, new Vector2(3f, 3f));
        LevelKit.Stripes(root, mid + new Vector3(0f, 0.05f, 2.2f), new Vector3(4.6f, 0.08f, 0.12f));
        LevelKit.Stripes(root, mid + new Vector3(0f, 0.05f, -2.2f), new Vector3(4.6f, 0.08f, 0.12f));
        LevelKit.Stripes(root, mid + new Vector3(2.2f, 0.05f, 0f), new Vector3(0.12f, 0.08f, 4.6f));
        LevelKit.Stripes(root, mid + new Vector3(-2.2f, 0.05f, 0f), new Vector3(0.12f, 0.08f, 4.6f));
        LevelKit.Water(root, mid + Vector3.back * (len * 0.1f), new Vector2(halfW * 1.2f, len * 0.5f));
        LevelKit.Cable(root, new Vector3(-halfW + 0.6f, y + 0.12f, z0 + 4f), new Vector3(-halfW + 3f, y + 0.12f, z0 + len * 0.6f), 0.1f);
        LevelKit.Cable(root, new Vector3(halfW - 0.6f, y + 0.12f, z1 - 4f), new Vector3(halfW - 4f, y + 0.12f, z0 + len * 0.35f), 0.1f);
        LevelKit.Cable(root, new Vector3(-halfW + 3f, y + 0.12f, z0 + len * 0.6f), new Vector3(-halfW + 0.3f, y + 2.4f, z0 + len * 0.7f), 0.1f);

        // The west gallery off its trough lip; drums along the east to kick off; the sinks as lips in the lane
        Catwalk(root, config, c, -1, 3.2f, 3.6f, halfW, y, z0 + len * 0.35f, z1 - 4f, "west gallery");
        Vector3[] drums = { new Vector3(halfW - 5f, y, z0 + 9f), new Vector3(halfW - 6f, y, z0 + 11.5f), new Vector3(halfW - 4.5f, y, z0 + len * 0.6f), new Vector3(halfW - 7f, y, z1 - 9f) };
        for (int i = 0; i < drums.Length; i++) LevelKit.Drum(root, drums[i], i % 3 == 0);
        LevelKit.Lip(root, new Vector3(6f, y + 0.55f, z0 + len * 0.28f), new Vector3(4f, 1.1f, 1.6f), LevelKit.Iron, "Trough");
        LevelKit.Lip(root, new Vector3(-5f, y + 0.55f, z0 + len * 0.72f), new Vector3(4f, 1.1f, 1.6f), LevelKit.Iron, "Trough");
        LevelKit.Cart(root, new Vector3(-7f, y, z0 + 6f), 30f);
        LevelKit.Terminal(root, new Vector3(halfW - 1.2f, y, z0 + len * 0.5f), -90f);
        LevelKit.WetFloorSign(root, new Vector3(-3f, y, z0 + len * 0.35f), 20f);
        LevelKit.WetFloorSign(root, new Vector3(3.5f, y, z0 + len * 0.65f), -35f);
        LevelKit.WetFloorSign(root, new Vector3(0.5f, y, z0 + 4f), 5f);
        for (int i = 0; i < 4; i++)
            LevelKit.NeonStrip(root, new Vector3(-halfW + 1.2f + (i % 2) * 0.7f, y + 0.22f, z0 + len * 0.25f + i * 0.9f), new Vector3(0.5f, 0.44f, 0.5f), LevelKit.Amber);
        LevelKit.Orb(root, new Vector3(0f, y + height - 1.6f, z0 + len * 0.62f));
        LevelKit.HoloSign(root, new Vector3(0f, y + Mathf.Min(height - 1.5f, 6.4f), z1 - 1.5f), Vector3.back, 9f, 1.8f, "SANITATION");
        LevelKit.Stencil(root, new Vector3(-halfW + 0.1f, y + 3f, z0 + len * 0.45f), Vector3.right, "KEEP IT CLEAN", 0.09f, 7f);
        LevelKit.LineRun(root, new Vector3(0f, y, z0 + 4f), new Vector3(0f, y, mid.z - 3f), c.zone);
        if (c.zone == Zone.Underbelly) DressSewer(root, c, halfW, y, height, z0, z1);   // the sewers' brick: Sanitation lives down here now
        else Dress(root, c, halfW, y, height, z0, z1);

        // The keeper of the floor, on the line past the drain, and the staff it has not finished with
        Course.Fuel(root, config, c, EnemyKind.Janitor, new Vector3(0f, y, z0 + len * 0.62f), "past the drain");
        Course.Fuel(root, config, c, EnemyKind.DamageControl, new Vector3(-halfW + 4f, y, z0 + 9f), "the hoses");
        Course.Fuel(root, config, c, EnemyKind.DamageControl, new Vector3(halfW - 4f, y, z1 - 8f), "the drums");
        Course.Fuel(root, config, c, EnemyKind.Scourer, new Vector3(4f, y, z0 + len * 0.45f), "the wet floor");
        return OpenExit(c, z1, y, halfW, height);
    }
}
