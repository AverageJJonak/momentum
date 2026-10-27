using UnityEngine;
using Cursor = LevelSections.Cursor;

/// <summary>
/// THE COURSE, the roofed lanes: the GRAND HALL and the GALLERY. See LevelSectionsCourse.cs for the grammar.
/// </summary>
public static partial class Course
{
    /// GRAND HALL. Pillar rows down the lane (every pillar an edge-boost corner), two galleries at different heights
    /// along the walls, a chandelier orb over the middle, a ramp down off the end of each gallery.
    /// MAIN: the floor between the pillar rows at 8 m/s; nothing in the way but the units.
    /// FAST, west: a brass plinth lip by the wall — a PERFECT corner boost at run speed lands the low gallery
    /// (3.2 m); a lazy one drops you back on the floor. East: a 14 m wall run and a kick off the east wall, then
    /// the plinth lip under the high gallery (6.2 m): the WALL COMBO corner boost (×1.6) lands it. Centre: the
    /// chandelier — a grapple swing over the pillars onto either gallery.
    /// FUEL: a bellhop on the west lip, three footmen in a row along the high gallery, a page under the chandelier,
    /// a roster unit on the main line. Galleries want a lane at least 24 m wide and 70 m long (else one, or none).
    public static Cursor Hall(Transform root, MovementConfig config, Cursor c, float len, float halfW, float height, int galleries = 2, bool chandelier = true)
    {
        Begin(c);
        len = Mathf.Max(len, 60f);
        halfW = Mathf.Max(halfW, 7f);
        galleries = Mathf.Clamp(galleries, 0, 2);
        if (halfW < 12f) galleries = 0;
        if (galleries == 2 && len < 70f) galleries = 1;
        height = Mathf.Max(height, galleries > 0 ? HeadLips : Head);
        float z0 = c.z, z1 = z0 + len, y = c.floorY;
        Threshold(root, c, halfW, y, height);
        Shell(root, c, halfW, y, height, z0, z1, Roofed);
        var zone = c.zone;

        // The pillar rows: a hair under the ceiling (never on its plane), every one a corner
        float px = Mathf.Max(5.5f, halfW * 0.42f), pillarH = height - 0.05f;
        for (float z = z0 + 10f; z < z1 - 6f; z += 10f)
            for (int s = -1; s <= 1; s += 2)
                LevelKit.BoostCorner(root, new Vector3(s * px, y + pillarH * 0.5f, z), new Vector3(1.4f, pillarH, 1.4f), LevelKit.Wall);

        float gx = halfW - 2.5f, gw = 5f;
        // WEST: the low gallery, off a perfect boost on its plinth lip
        if (galleries >= 1)
        {
            float gy = y + 3.2f, run = Run(3.2f);
            float lipZ = z0 + len * 0.18f;
            LevelKit.Lip(root, new Vector3(-gx, y + 0.65f, lipZ), new Vector3(gw - 0.4f, 1.3f, 4f));
            float gz0 = lipZ + 3.5f, gz1 = z1 - 3f - run;
            LevelKit.FloorSlab(root, new Vector3(-gx, gy - 0.5f, (gz0 + gz1) * 0.5f), new Vector3(gw, 1f, gz1 - gz0), "Gallery");
            LevelKit.Ramp(root, -gx, gw, gz1, gy, z1 - 3f, y);
            LevelKit.LineRun(root, new Vector3(-gx, y, lipZ - 12f), new Vector3(-gx, y, lipZ - 2.2f), zone);
            Fuel(root, config, c, EnemyKind.Chaser, new Vector3(-gx, y + 1.3f, lipZ), "west lip");
        }
        // EAST: the high gallery — the wall run, the kick, the plinth lip, the WALL COMBO boost
        if (galleries >= 2)
        {
            float gy = y + 6.2f, run = Run(6.2f);
            float wr0 = z0 + len * 0.12f, wr1 = wr0 + 14f;
            LevelKit.Flute(root, new Vector3(halfW, y + 2.2f, wr0), Vector3.left, 4f);
            LevelKit.Flute(root, new Vector3(halfW, y + 2.2f, wr1), Vector3.left, 4f);
            LevelKit.LineRun(root, new Vector3(halfW - 0.5f, y, wr0), new Vector3(halfW - 0.5f, y, wr1), zone);
            float lipZ = wr1 + 4f;
            LevelKit.Lip(root, new Vector3(gx, y + 0.7f, lipZ), new Vector3(gw - 0.4f, 1.4f, 4f));
            float gz0 = lipZ + 3f, gz1 = z1 - 3f - run;
            LevelKit.FloorSlab(root, new Vector3(gx, gy - 0.5f, (gz0 + gz1) * 0.5f), new Vector3(gw, 1f, gz1 - gz0), "Gallery");
            LevelKit.Ramp(root, gx, gw, gz1, gy, z1 - 3f, y);
            for (int i = 0; i < 3; i++)
                Fuel(root, config, c, EnemyKind.Dummy, new Vector3(gx, gy, gz0 + 6f + i * 5f), "east gallery");
        }
        // The chandelier over the middle, globe pendants down the rest of the lane
        float cy = y + height * 0.72f, midZ = z0 + len * 0.5f;
        if (chandelier)
        {
            LevelKit.Chandelier(root, new Vector3(0f, cy, midZ), y + height);
            Fuel(root, config, c, EnemyKind.Turret, new Vector3(0f, cy - 4f, midZ + 8f), "chandelier");
        }
        for (float z = z0 + 15f; z < z1 - 8f; z += 30f)
            if (!chandelier || Mathf.Abs(z - midZ) > 8f) LevelKit.GlobeLamp(root, new Vector3(0f, y + height - 3f, z), y + height);
        PoolFuel(config, new Vector3(R(-3f, 3f), y, z0 + len * 0.62f), "main");
        Dressing(root, c, halfW, y, height, z0, z1, false);
        LevelSections.Postcard(new Vector3(-2.5f, y + 5f, z0 + 6f), new Vector3(3f, y + 4f, z0 + len * 0.6f), 74f);   // between the pillar rows, not through the first pillar
        return Open(c, z1, y, halfW, height);
    }

    /// GALLERY. Long wall-run walls down both sides with kick fins proud of them (every fin an edge-boost corner,
    /// 16 m of clean wall between), a catwalk high line along the west wall, a choke of two kick walls with a
    /// doorman in it.
    /// MAIN: the floor at 8 m/s between the fins; the choke's lane is 7 m.
    /// FAST: wall-run the west wall, kick the fin, land on the catwalk's plinth lip and WALL COMBO boost up to the
    /// catwalk (4.6 m) — the pad beside the lip is the slow way up; a glass pane across the catwalk pays at speed.
    /// At the choke, edge-boost off a kick wall's corner round the doorman.
    /// FUEL: three footmen along the catwalk, the doorman in the choke, a roster unit on the floor past it.
    public static Cursor Gallery(Transform root, MovementConfig config, Cursor c, float len, float halfW, float height)
    {
        Begin(c);
        len = Mathf.Max(len, 60f);
        halfW = Mathf.Max(halfW, 8f);
        height = Mathf.Max(height, Head);
        float z0 = c.z, z1 = z0 + len, y = c.floorY;
        Threshold(root, c, halfW, y, height);
        Shell(root, c, halfW, y, height, z0, z1, Roofed);
        var zone = c.zone;

        // The catwalk: along the west wall, 4.6 m up; its plinth lip and its pad before it, its ramp off the end
        float cw = 3.2f, cx = -(halfW - 1.7f), cy = y + 4.6f;
        float cz0 = z0 + len * 0.3f, cz1 = z1 - 4f - Run(4.6f);
        // The fins: 5 m tall, 3 m deep, proud of the walls, every 16 m, sides alternating; the west ones under the
        // catwalk stop short of it
        int fins = 0;
        for (float z = z0 + 12f; z < z1 - 10f; z += 16f, fins++)
        {
            int s = fins % 2 == 0 ? -1 : 1;
            float finH = s < 0 && z > cz0 - 4f ? 3.5f : 5f;
            LevelKit.BoostCorner(root, new Vector3(s * (halfW - 1.5f), y + finH * 0.5f, z), new Vector3(3f, finH, 1f), LevelKit.Wall);
            LevelKit.LineRun(root, new Vector3(s * (halfW - 0.5f), y, z + 1.5f), new Vector3(s * (halfW - 0.5f), y, Mathf.Min(z1 - 3f, z + 14.5f)), zone);
        }
        LevelKit.Lip(root, new Vector3(cx, y + 0.6f, cz0 - 3.5f), new Vector3(cw, 1.2f, 3f));
        LevelKit.JumpPad(root, new Vector3(cx, y, cz0 - 9f), 0f, 16f, 5f);   // in line before the lip, off the main lane: over the lip onto the catwalk
        LevelKit.FloorSlab(root, new Vector3(cx, cy - 0.5f, (cz0 + cz1) * 0.5f), new Vector3(cw, 1f, cz1 - cz0), "Catwalk");
        LevelKit.Glass(root, new Vector3(cx, cy + 1.6f, z0 + len * 0.6f), new Vector3(cw, 3.2f, 0.3f), config);
        LevelKit.Ramp(root, cx, cw, cz1, cy, z1 - 4f, y);
        for (int i = 0; i < 3; i++) Fuel(root, config, c, EnemyKind.Dummy, new Vector3(cx, cy, cz0 + 5f + i * 6f), "catwalk");

        // The choke: two kick walls, 7 m apart, the doorman between them
        float kz = z0 + len * 0.55f;
        for (int s = -1; s <= 1; s += 2) LevelKit.KickWall(root, new Vector3(s * 4f, y + 2f, kz), new Vector3(1f, 4f, 5f));
        Fuel(root, config, c, EnemyKind.Sentry, new Vector3(0f, y, kz + 1f), "choke");
        PoolFuel(config, new Vector3(R(-4f, 4f), y, kz + 16f), "main");
        for (float z = z0 + 14f; z < z1 - 8f; z += 24f) LevelKit.GlobeLamp(root, new Vector3(0f, y + height - 2.6f, z), y + height);
        Dressing(root, c, halfW, y, height, z0, z1, false);
        return Open(c, z1, y, halfW, height);
    }
}
