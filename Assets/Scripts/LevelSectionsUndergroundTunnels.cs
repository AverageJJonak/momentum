using UnityEngine;

/// <summary>
/// THE UNDERGROUND's tunnels as course spaces (course-grammar; act2-underground): the TUNNEL GALLERY (a twin bore: the
/// lane, an arcade of brick piers, and the running tunnel beside it with its live rail, its cable tray, its refuges and
/// the TRAIN through it), the VENT SHAFT (a vertical shaft with the great fan at its foot: the updraft lifts you; the
/// grille shelves are the lips of the kick-wall ladder beside it) and the SIGNAL GALLERY (gantries across the lane —
/// posts to kick, beams to boost onto — lever frames as lips, the signal box on its gallery, the track along the east).
/// </summary>
public static partial class LevelSections
{
    /// TUNNEL GALLERY. The lane (15 m) east of an arcade of brick piers; through the arcade the running tunnel: the
    /// track at x −8 with the THIRD RAIL between it and the piers (hop it to get onto the bed), the cable tray along the
    /// west wall (a lip line out of everything's reach), refuges every twenty metres, a tunnel box at each end the
    /// TRAIN runs between. In the lane two signal gantries (kick posts, lip beams) and a gallery along the east wall.
    /// MAIN: the lane. FAST: the bed (the pad throws you down it; leave it through the arcade before the far box), the
    /// tray, the gantry beams, the east gallery off the WALL COMBO.
    /// FUEL: a platelayer on the bed, a signalman on the first gantry, a ganger in the lane, roster units and pages.
    public static Cursor TunnelGallery(Transform root, MovementConfig config, Cursor c, string name, float len = 110f, float halfW = 11f, float height = 9f, float period = 14f, float phase = 0f, bool northbound = true)
    {
        OpenBegin(c);
        len = Mathf.Max(len, 90f);
        halfW = Mathf.Max(halfW, 10f);
        height = Mathf.Max(height, 8f);
        const float boxLen = 28f, pierX = -4.5f, trackX = -8f;
        float z0 = c.z, z1 = c.z + len, y = c.floorY, mid = z0 + len * 0.5f;
        float r0 = z0 + boxLen, r1 = z1 - boxLen;
        OpenThreshold(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), OpenRoofed);
        LevelKit.TrainLine(root, trackX, y, z0, z1, -halfW + 0.05f, pierX + 0.45f, boxLen, 6f, period, phase, 3, 20f, northbound);
        LevelKit.ThirdRail(root, new Vector3(pierX - 1.3f, y, r0 + 0.6f), new Vector3(pierX - 1.3f, y, r1 - 0.6f));
        // The arcade between the boxes: piers every six metres under brick arches
        float prev = -1f;
        for (float z = r0 + 1f; z <= r1 - 0.5f; z += 6f)
        {
            LevelKit.Slab(root, new Vector3(pierX, y + 3.5f, z), new Vector3(1f, 7f, 1.2f), LevelKit.Brick, "ArcadePier");
            if (prev > 0f) LevelKit.BrickArch(root, new Vector3(pierX, y + 5f, prev), new Vector3(pierX, y + 5f, z), Vector3.right, 1.3f);
            prev = z;
        }
        LevelKit.CableTray(root, new Vector3(-halfW, y + 1.4f, r0 + 1f), new Vector3(-halfW, y + 1.4f, r1 - 1f), -1);
        for (float z = r0 + 12f; z < r1 - 8f; z += 20f) LevelKit.Refuge(root, new Vector3(-halfW, y, z), Vector3.right);
        LevelKit.JumpPad(root, new Vector3(trackX, y, r0 + 4f), 0f, 11f, 15f);
        LevelKit.LineRun(root, new Vector3(trackX, y, r0 + 6f), new Vector3(trackX, y, r1 - 8f), c.zone);
        // The lane: the gantries, the east gallery
        float laneMid = (pierX + 0.5f + halfW) * 0.5f;
        LevelKit.SignalGantry(root, new Vector3(laneMid, y, z0 + len * 0.36f), 12f, 5f);
        LevelKit.SignalGantry(root, new Vector3(laneMid, y, z0 + len * 0.68f), 12f, 4.5f);
        Catwalk(root, config, c, 1, 6.2f, 3.2f, halfW, y, z0 + len * 0.42f, z1 - 4f, "the east gallery");
        LevelKit.HoloSign(root, new Vector3(pierX + 0.6f, y + 7.6f, mid), Vector3.right, 5f, 1.2f, "RUNNING TUNNEL  ·  KEEP OUT", true);
        Flavour(UndergroundFlavour.Tunnel, name);
        detailSkipWest = true;   // the west wall is the running tunnel's own
        Dress(root, c, halfW, y, height, z0, z1);
        Course.Fuel(root, config, c, EnemyKind.Borer, new Vector3(trackX, y, mid), "the track bed");
        Course.Fuel(root, config, c, EnemyKind.Rigger, new Vector3(laneMid, y + 5.5f, z0 + len * 0.36f), "the first gantry");
        Course.Fuel(root, config, c, EnemyKind.Blaster, new Vector3(laneMid + 2f, y, z0 + len * 0.55f), "the lane");
        Pool(config, new Vector3(laneMid - 3f, y, z0 + len * 0.25f), "the lane");
        Pool(config, new Vector3(laneMid, y + 5f, z0 + len * 0.8f), "over the lane", true);
        Postcard(new Vector3(laneMid + 4f, y + 3.5f, z0 + 6f), new Vector3(trackX, y + 1.2f, mid), 76f, signature: true);
        return OpenExit(c, z1, y, halfW, height);
    }

    /// VENT SHAFT. A vertical shaft `rise` tall: the great fan in its floor, the column of air over it carrying you to
    /// the top (the top quarter only holds you: steer off it onto the landing at the north end); grille shelves up both
    /// walls every six metres (LIPS: the kick-wall ladder for the climb without the fan), flutes on the walls, an orb at
    /// the top. MAIN: the fan. FAST: wall kick to wall kick up the shelves, a WALL COMBO boost on each.
    /// FUEL: a signalman on a shelf, a page mid-shaft, a stoker on the top landing.
    public static Cursor VentShaft(Transform root, MovementConfig config, Cursor c, float rise, float halfW = 7f)
    {
        OpenBegin(c);
        halfW = Mathf.Clamp(halfW, 6f, 8f);
        rise = Mathf.Clamp(rise, 14f, 30f);
        const float len = 26f, landing = 6f;
        float z0 = c.z, z1 = c.z + len, y = c.floorY, topY = y + rise, height = rise + 12f, mid = z0 + 11f;
        OpenThreshold(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), OpenRoofed);
        LevelKit.WallFace(root, Face.North, z1, -halfW - T * 0.5f, halfW + T * 0.5f, y, topY - 1f);   // under the top landing: the lane leaves at the top
        LevelKit.FloorSlab(root, new Vector3(0f, topY - 0.5f, (z1 - landing + z1 + T) * 0.5f), new Vector3(halfW * 2f + 2f * T, 1f, landing + T), "TopLanding");
        LevelKit.Parapet(root, new Vector3(0f, topY + 0.45f, z1 - landing + 0.4f), new Vector3(halfW * 2f - 1f, 0.9f, 0.6f));
        LevelKit.FanShaft(root, new Vector3(0f, y, mid), rise + 2.5f, 6f);
        int shelves = Mathf.FloorToInt((rise - 3f) / 6f);
        for (int i = 1; i <= shelves; i++)
        {
            int s = i % 2 == 0 ? 1 : -1;
            float sy = y + i * 6f;
            LevelKit.Lip(root, new Vector3(s * (halfW - 1.3f), sy - 0.4f, z0 + 5f + (i % 2) * 10f), new Vector3(2.6f, 0.8f, 3f), LevelKit.RivetIron, "GrilleShelf");
            for (int k = 0; k < 4; k++) LevelKit.Prim(root, new Vector3(s * (halfW - 0.08f), sy + 0.6f + k * 0.5f, z0 + 5f + (i % 2) * 10f), new Vector3(0.1f, 0.12f, 2.6f), LevelKit.Iron, "Louvre");
            if (i == 2) Course.Fuel(root, config, c, EnemyKind.Rigger, new Vector3(s * (halfW - 1.3f), sy, z0 + 5f + (i % 2) * 10f), "the second shelf");
        }
        for (int s = -1; s <= 1; s += 2) LevelKit.Flute(root, new Vector3(s * halfW, y + rise * 0.5f, mid), Vector3.left * s, 4f);
        LevelKit.Orb(root, new Vector3(0f, topY + 2.5f, mid));
        LevelKit.LineRun(root, new Vector3(0f, y, z0 + 2f), new Vector3(0f, y, mid - 3.6f), c.zone);
        LevelKit.HoloSign(root, new Vector3(-halfW + 0.15f, y + 4f, z0 + 4f), Vector3.right, 4f, 1.1f, "FAN RUNNING  ·  MIND YOUR HAT", true);
        Flavour(UndergroundFlavour.Tunnel, "VENT SHAFT 3");
        denseSkipRoom = true;
        Dress(root, c, halfW, y, height, z0, z1);
        Course.Fuel(root, config, c, EnemyKind.Kettler, new Vector3(0f, topY, z1 - 3f), "the top landing");
        Pool(config, new Vector3(0f, y + rise * 0.55f, z0 + 4f), "mid-shaft", true);
        return OpenExit(c, z1, topY, halfW, 12f);
    }

    /// SIGNAL GALLERY. The lane (15 m) west of an arcade; through it the track along the east with its live rail and
    /// a TRAIN on a cycle. Three signal gantries across the lane (the posts kick walls, the beams lips at 4.5–5.5 m),
    /// two lever frames in the lane (lips, 1.2 m: perfect corner boosts), the signal box on a gallery 4 m up the west
    /// wall with its own lever frames. MAIN: the lane under the gantries. FAST: lever frame → gantry beam → gallery.
    /// FUEL: signalmen on the gantries, a ganger at the first lever frame, a platelayer on the bed, a page over the lane.
    public static Cursor SignalGallery(Transform root, MovementConfig config, Cursor c, float len = 100f, float halfW = 12f, float height = 11f, float period = 15f, float phase = 0.3f)
    {
        OpenBegin(c);
        len = Mathf.Max(len, 80f);
        halfW = Mathf.Max(halfW, 11f);
        height = Mathf.Max(height, 9f);
        const float boxLen = 28f, pierX = 3.5f, trackX = 8f;
        float z0 = c.z, z1 = c.z + len, y = c.floorY, mid = z0 + len * 0.5f;
        float r0 = z0 + boxLen, r1 = z1 - boxLen, laneMid = (-halfW + pierX - 0.5f) * 0.5f;
        OpenThreshold(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), OpenRoofed);
        LevelKit.TrainLine(root, trackX, y, z0, z1, pierX - 0.45f, halfW - 0.05f, boxLen, 6f, period, phase, 3, 20f, false);
        LevelKit.ThirdRail(root, new Vector3(pierX + 1.4f, y, r0 + 0.6f), new Vector3(pierX + 1.4f, y, r1 - 0.6f));
        float prev = -1f;
        for (float z = r0 + 1f; z <= r1 - 0.5f; z += 6f)
        {
            LevelKit.Slab(root, new Vector3(pierX, y + 3.5f, z), new Vector3(1f, 7f, 1.2f), LevelKit.Brick, "ArcadePier");
            if (prev > 0f) LevelKit.BrickArch(root, new Vector3(pierX, y + 5f, prev), new Vector3(pierX, y + 5f, z), Vector3.left, 1.3f);
            prev = z;
        }
        LevelKit.SignalGantry(root, new Vector3(laneMid, y, z0 + len * 0.25f), 13f, 4.5f);
        LevelKit.SignalGantry(root, new Vector3(laneMid, y, z0 + len * 0.55f), 13f, 5.5f);
        LevelKit.SignalGantry(root, new Vector3(laneMid, y, z0 + len * 0.85f), 13f, 4.5f);
        LevelKit.LeverFrame(root, new Vector3(laneMid + 1f, y, z0 + len * 0.15f), 0f, 8);
        LevelKit.LeverFrame(root, new Vector3(laneMid - 2f, y, z0 + len * 0.7f), 0f, 6);
        LevelKit.LineRun(root, new Vector3(laneMid + 1f, y, z0 + 3f), new Vector3(laneMid + 1f, y, z0 + len * 0.15f - 1.2f), c.zone);
        Catwalk(root, config, c, -1, 4f, 3.6f, halfW, y, z0 + len * 0.3f, z1 - 4f, "the signal box");
        LevelKit.LeverFrame(root, new Vector3(-(halfW - 2.1f), y + 4f, z0 + len * 0.3f + 9f), 90f, 6);
        LevelKit.ControlDesk(root, new Vector3(-(halfW - 2.1f), y + 4f, z0 + len * 0.3f + 16f), 90f);
        LevelKit.HoloSign(root, new Vector3(pierX - 0.6f, y + 7.6f, mid), Vector3.left, 5f, 1.2f, "RUNNING LINE  ·  KEEP OUT", true);
        Flavour(UndergroundFlavour.Tunnel, "SIGNAL GALLERY 2");
        detailSkipEast = true;
        Dress(root, c, halfW, y, height, z0, z1);
        Course.Fuel(root, config, c, EnemyKind.Rigger, new Vector3(laneMid, y + 5f, z0 + len * 0.25f), "the first gantry");
        Course.Fuel(root, config, c, EnemyKind.Rigger, new Vector3(laneMid, y + 6f, z0 + len * 0.55f), "the second gantry");
        Course.Fuel(root, config, c, EnemyKind.Blaster, new Vector3(laneMid + 1f, y, z0 + len * 0.15f + 4f), "the first lever frame");
        Course.Fuel(root, config, c, EnemyKind.Borer, new Vector3(trackX, y, mid), "the track");
        Pool(config, new Vector3(laneMid, y + 4f, z0 + len * 0.7f), "over the lane", true);
        Postcard(new Vector3(laneMid + 3f, y + 4f, z0 + 5f), new Vector3(laneMid, y + 2f, z0 + len * 0.55f), 76f);
        return OpenExit(c, z1, y, halfW, height);
    }
}
