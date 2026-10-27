using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The detail pass every section gets, and the corporate sections of Level 5 —
/// HALCYON CIVIC's headquarters: the entrance, the reception hall, the lifts,
/// the open plan, the executive wing, the auditorium, the server hall where the
/// rot lives, and the atrium where the Concierge waits behind his desk.
/// </summary>
public static partial class LevelSections
{
    public static Cursor Office(Cursor c) { c.zone = Zone.Office; return c; }

    // A section whose walls are already spoken for (doors, screens) sets these before Dress()
    static bool detailSkipWest, detailSkipEast;
    static bool denseSkipWest, denseSkipEast;
    /// A section that wants no density pass at all (a cutscene bay), or only walls and roof (a boss arena)
    static bool denseSkipRoom, denseLight;

    // ------------------------------------------------------------ the detail pass

    /// Trim, fixtures and clutter by zone, on the side walls, the floor and the
    /// ceiling. All of it is decor — nothing here has a collider except benches,
    /// scaffold planks and the like, which are meant to be stood on.
    static void Detail(Transform root, Cursor c, float halfW, float y, float height, float z0, float z1, bool openSky)
    {
        bool skipW = detailSkipWest, skipE = detailSkipEast;
        detailSkipWest = detailSkipEast = false;
        denseSkipWest = skipW; denseSkipEast = skipE;   // the dressing pass wants to know too (LevelKit.Densify)
        float len = z1 - z0;
        if (len < 8f || halfW < 2.5f) return;
        switch (c.zone)
        {
            case Zone.Office: DetailOffice(root, halfW, y, height, z0, z1, openSky, skipW, skipE); break;
            case Zone.Works: DetailWorks(root, halfW, y, height, z0, z1, openSky, skipW, skipE); break;
            case Zone.Underbelly: DetailUnder(root, halfW, y, height, z0, z1, openSky, skipW, skipE); break;
            default: DetailCity(root, halfW, y, height, z0, z1, openSky, skipW, skipE); break;
        }
    }

    static void DetailCity(Transform root, float halfW, float y, float height, float z0, float z1, bool openSky, bool skipW, bool skipE)
    {
        float len = z1 - z0, top = y + height, mid = z0 + len * 0.5f;
        Vector3 wA = new Vector3(-halfW, y, z0 + 0.6f), wB = new Vector3(-halfW, y, z1 - 0.6f);
        Vector3 eA = new Vector3(halfW, y, z0 + 0.6f), eB = new Vector3(halfW, y, z1 - 0.6f);
        Material shaft = LevelKit.Fluted, cap = LevelKit.Limestone;   // the civic halls: fluted plaster, limestone, oxblood, brass
        if (height >= 5f)
            for (float z = z0 + 6f; z < z1 - 4f; z += 12f)
            {
                if (!skipW) LevelKit.Pilaster(root, new Vector3(-halfW, y, z), Vector3.right, height, shaft, cap);
                if (!skipE) LevelKit.Pilaster(root, new Vector3(halfW, y, z), Vector3.left, height, shaft, cap);
            }
        if (!skipW) LevelKit.Wainscot(root, wA, wB, Vector3.right, LevelKit.Oxblood, LevelKit.Brass);
        if (!skipE) LevelKit.Wainscot(root, eA, eB, Vector3.left, LevelKit.Oxblood, LevelKit.Brass);
        if (height >= 6f)
        {
            if (!skipW) LevelKit.Cornice(root, wA, wB, Vector3.right, top, LevelKit.Limestone);
            if (!skipE) LevelKit.Cornice(root, eA, eB, Vector3.left, top, LevelKit.Limestone);
        }
        LevelKit.NeonStrip(root, new Vector3(-halfW + 0.45f, y + 0.03f, mid), new Vector3(0.7f, 0.03f, len - 1.2f), LevelKit.Marble);
        LevelKit.NeonStrip(root, new Vector3(halfW - 0.45f, y + 0.03f, mid), new Vector3(0.7f, 0.03f, len - 1.2f), LevelKit.Marble);
        LevelKit.NeonStrip(root, new Vector3(-halfW + 0.82f, y + 0.04f, mid), new Vector3(0.06f, 0.03f, len - 1.2f), LevelKit.Brass);
        LevelKit.NeonStrip(root, new Vector3(halfW - 0.82f, y + 0.04f, mid), new Vector3(0.06f, 0.03f, len - 1.2f), LevelKit.Brass);
        if (len >= 30f)
            for (float z = z0 + 14f; z < z1 - 8f; z += 30f)
            {
                if (!skipW) LevelKit.Bench(root, new Vector3(-halfW + 0.35f, y, z), 90f);
                if (!skipE && len >= 60f) LevelKit.Bench(root, new Vector3(halfW - 0.35f, y, z + 15f), -90f);
            }
        if (!openSky)
        {
            if (len >= 24f)
                for (float z = z0 + 12f; z < z1 - 6f; z += 12f)
                    LevelKit.Beam(root, new Vector3(-halfW, top - 0.2f, z), new Vector3(halfW, top - 0.2f, z), 0.5f, 0.36f, LevelKit.Limestone);
            if (height >= 10f && len >= 40f)
            {
                int n = 0;
                for (float z = z0 + 20f; z < z1 - 10f; z += 40f, n++)
                    LevelKit.Banner(root, new Vector3(0f, top - 1.2f, z), 1.6f, 4.5f, top, n % 2 == 0);
            }
        }
        // The future: glass fins between the pilasters, data pillars, a ring in the floor,
        // chevrons at the door, drones on patrol, skylights, the company's line by the way out
        if (height >= 5f)
            for (float z = z0 + 12f; z < z1 - 4f; z += 12f)
            {
                if (!skipW) LevelKit.GlassFin(root, new Vector3(-halfW, y, z), Vector3.right, height * 0.8f);
                if (!skipE) LevelKit.GlassFin(root, new Vector3(halfW, y, z), Vector3.left, height * 0.8f);
            }
        if (halfW >= 6f && len >= 30f)
            for (float z = z0 + 10f; z < z1 - 8f; z += 25f)
            {
                if (!skipW) LevelKit.DataPillar(root, new Vector3(-halfW + 1.2f, y, z), Mathf.Min(3.5f, height - 1.5f));
                if (!skipE) LevelKit.DataPillar(root, new Vector3(halfW - 1.2f, y, z + 12f), Mathf.Min(3.5f, height - 1.5f));
            }
        if (len >= 20f)
        {
            LevelKit.FloorRing(root, new Vector3(0f, y, z0 + len * 0.5f), Mathf.Min(3f, halfW * 0.5f));
            LevelKit.Chevrons(root, new Vector3(0f, y, z0 + 2f), Vector3.forward, 3);
        }
        int drones = len >= 60f ? 2 : 1;
        for (int i = 0; i < drones; i++)
            LevelKit.Drone(root, new Vector3(R(-halfW * 0.5f, halfW * 0.5f), y + Mathf.Clamp(height * 0.55f, 3.5f, 9f), z0 + len * (0.3f + 0.4f * i)), Mathf.Min(6f, halfW * 0.5f), false);
        if (!openSky && height >= 8f && halfW >= 5f)
            for (float z = z0 + 14f; z < z1 - 8f; z += 28f)
                LevelKit.Skylight(root, new Vector3(0f, top, z), new Vector2(Mathf.Min(6f, halfW), 5f));
        if (height >= 6f && halfW >= 10f && len >= 24f)
            LevelKit.HoloSign(root, new Vector3(-halfW + 3.2f, y + Mathf.Min(height - 1.5f, 7f), z1 - 1.2f), Vector3.back, 5f, 1f,
                LevelKit.Taglines[Mathf.Abs((int)(z0 * 7f)) % LevelKit.Taglines.Length]);
    }

    static void DetailUnder(Transform root, float halfW, float y, float height, float z0, float z1, bool openSky, bool skipW, bool skipE)
    {
        float len = z1 - z0, top = y + height, mid = z0 + len * 0.5f;
        for (int side = -1; side <= 1; side += 2)
        {
            if (side < 0 && skipW || side > 0 && skipE) continue;
            float x = side * (halfW - 0.15f);
            Vector3 facing = side < 0 ? Vector3.right : Vector3.left;
            LevelKit.Pipe(root, new Vector3(x, y + 0.7f, z0 + 0.3f), new Vector3(x, y + 0.7f, z1 - 0.3f), 0.2f);
            LevelKit.Pipe(root, new Vector3(x, y + 2.3f, z0 + 0.3f), new Vector3(x, y + 2.3f, z1 - 0.3f), 0.13f);
            LevelKit.Stripes(root, new Vector3(side * (halfW - 0.07f), y + 0.07f, mid), new Vector3(0.1f, 0.12f, len - 1f));
            int n = 0;
            for (float z = z0 + 10f; z < z1 - 5f; z += 25f, n++)
                if ((n + (side < 0 ? 0 : 1)) % 2 == 0) LevelKit.Valve(root, new Vector3(x + facing.x * 0.28f, y + 0.7f, z), Vector3.forward);
            for (float z = z0 + 8f; z < z1 - 4f; z += 15f)
                LevelKit.Pipe(root, new Vector3(side * (halfW - 0.12f), y + 2.3f, z), new Vector3(side * (halfW - 0.12f), top - 0.3f, z), 0.08f);
            for (int i = 0; i < 2; i++)
                LevelKit.Streak(root, new Vector3(side * halfW, top - R(0f, 1.5f), z0 + len * R(0.1f, 0.9f)), facing, height * R(0.3f, 0.6f));
        }
        for (float z = z0 + 9f; z < z1 - 6f; z += 18f)
            LevelKit.Grate(root, new Vector3(R(-halfW * 0.5f, halfW * 0.5f), y, z), new Vector2(2f, 1f));
        if (!openSky)
        {
            if (halfW >= 4f && !skipE)
                LevelKit.Duct(root, new Vector3(halfW - 1.2f, top - 0.6f, z0 + 1f), new Vector3(halfW - 1.2f, top - 0.6f, z1 - 1f), 1.0f, 0.7f);
            if (height >= 8f)
                for (float z = z0 + 10f; z < z1 - 6f; z += 20f)
                    LevelKit.Chain(root, new Vector3(R(-halfW * 0.6f, halfW * 0.6f), top - 0.05f, z), R(1.5f, 3.5f));
        }
        if (len >= 40f && halfW >= 6f && height >= 6f)
        {
            if (!skipW) LevelKit.Scaffold(root, new Vector3(-halfW + 1.6f, y, z0 + len * 0.3f), 2.6f);
            if (!skipE) LevelKit.Scaffold(root, new Vector3(halfW - 1.6f, y, z1 - len * 0.25f), 3.2f);
        }
        // The future's underside: data lines along the pipes, hazard holos, consoles, a dark drone, chevrons in
        for (int side = -1; side <= 1; side += 2)
        {
            if (side < 0 && skipW || side > 0 && skipE) continue;
            float x = side * (halfW - 0.4f);
            LevelKit.CableRun(root, new Vector3(x, y + 2.6f, z0 + 0.5f), new Vector3(x, y + 2.6f, z1 - 0.5f), LevelKit.Amber);
        }
        int holos = 0;
        for (float z = z0 + 12f; z < z1 - 6f; z += 30f, holos++)
        {
            bool west = holos % 2 == 0;
            if (west && skipW || !west && skipE) continue;
            LevelKit.HazardHolo(root, new Vector3(west ? -halfW : halfW, y + Mathf.Min(height * 0.5f, 3.2f), z), west ? Vector3.right : Vector3.left, 2.6f, 0.9f, "PRIVATE");
        }
        if (len >= 30f)
        {
            int k = 0;
            for (float z = z0 + 8f; z < z1 - 6f; z += 26f, k++)
            {
                bool west = k % 2 == 0;
                if (west && skipW || !west && skipE) continue;
                LevelKit.Terminal(root, new Vector3(west ? -halfW + 0.9f : halfW - 0.9f, y, z + 3f), west ? 90f : -90f);
            }
        }
        LevelKit.Drone(root, new Vector3(R(-halfW * 0.4f, halfW * 0.4f), y + Mathf.Clamp(height * 0.6f, 3f, 7f), z0 + len * 0.5f), Mathf.Min(4f, halfW * 0.5f), true);
        LevelKit.Chevrons(root, new Vector3(0f, y, z0 + 2f), Vector3.forward, 3, LevelKit.Amber);
    }

    static void DetailOffice(Transform root, float halfW, float y, float height, float z0, float z1, bool openSky, bool skipW, bool skipE)
    {
        float len = z1 - z0, top = y + height, mid = z0 + len * 0.5f;
        Vector3 wA = new Vector3(-halfW, y, z0 + 0.6f), wB = new Vector3(-halfW, y, z1 - 0.6f);
        Vector3 eA = new Vector3(halfW, y, z0 + 0.6f), eB = new Vector3(halfW, y, z1 - 0.6f);
        float finH = height * 0.7f;
        for (float z = z0 + 8f; z < z1 - 4f; z += 10f)
        {
            if (!skipW) LevelKit.Fin(root, new Vector3(-halfW, y, z), Vector3.right, finH);
            if (!skipE) LevelKit.Fin(root, new Vector3(halfW, y, z), Vector3.left, finH);
        }
        if (!skipW) LevelKit.Dado(root, wA, wB, Vector3.right);
        if (!skipE) LevelKit.Dado(root, eA, eB, Vector3.left);
        if (height >= 6f)
        {
            if (!skipW) LevelKit.Cove(root, wA, wB, Vector3.right, top);
            if (!skipE) LevelKit.Cove(root, eA, eB, Vector3.left, top);
        }
        LevelKit.NeonStrip(root, new Vector3(-halfW + 0.3f, y + 0.03f, mid), new Vector3(0.12f, 0.02f, len - 1.2f), LevelKit.Led);
        LevelKit.NeonStrip(root, new Vector3(halfW - 0.3f, y + 0.03f, mid), new Vector3(0.12f, 0.02f, len - 1.2f), LevelKit.Led);
        if (!skipW) for (float z = z0 + 13f; z < z1 - 3f; z += 16f) LevelKit.WallLight(root, new Vector3(-halfW, y + 2.6f, z), Vector3.right);
        if (!skipE) for (float z = z0 + 5f; z < z1 - 3f; z += 16f) LevelKit.WallLight(root, new Vector3(halfW, y + 2.6f, z), Vector3.left);
        int tone = 0;
        if (!skipW) for (float z = z0 + 11f; z < z1 - 5f; z += 16f, tone++) LevelKit.Poster(root, new Vector3(-halfW, y + 3.2f, z), Vector3.right, 1.6f, 2.2f, tone);
        if (!skipE) for (float z = z0 + 27f; z < z1 - 5f; z += 16f, tone++) LevelKit.Poster(root, new Vector3(halfW, y + 3.2f, z), Vector3.left, 1.6f, 2.2f, tone);
        if (halfW >= 3f) LevelKit.Runner(root, new Vector3(0f, y, mid), new Vector2(Mathf.Min(3.2f, halfW * 0.8f), len - 1.5f));
        if (!openSky && height >= 6f && len >= 12f)
        {
            for (float z = z0 + 8f; z < z1 - 3f; z += 8f)
                LevelKit.Trough(root, new Vector3(-halfW, top - 0.2f, z), new Vector3(halfW, top - 0.2f, z), 0.6f, 0.3f);
            LevelKit.Trough(root, new Vector3(-halfW * 0.5f, top - 0.2f, z0), new Vector3(-halfW * 0.5f, top - 0.2f, z1), 0.5f, 0.3f);
            LevelKit.Trough(root, new Vector3(halfW * 0.5f, top - 0.2f, z0), new Vector3(halfW * 0.5f, top - 0.2f, z1), 0.5f, 0.3f);
        }
        if (!openSky && height >= 7f && len >= 30f)
            for (float z = z0 + 16f; z < z1 - 12f; z += 33f)
                LevelKit.LightRing(root, new Vector3(0f, top - 3.2f, z), top, false);
        if (halfW >= 5f)
        {
            if (!skipW) LevelKit.PlanterBox(root, new Vector3(-halfW + 1.6f, y, z0 + 2f), new Vector3(2.2f, 0.6f, 2.2f), 3f);
            if (!skipE) LevelKit.PlanterBox(root, new Vector3(halfW - 1.6f, y, z0 + 2f), new Vector3(2.2f, 0.6f, 2.2f), 3f);
        }
        // Drones on patrol, data pillars between the fins, a ring in the floor, chevrons in, skylights
        int drones = len >= 60f ? 2 : 1;
        for (int i = 0; i < drones; i++)
            LevelKit.Drone(root, new Vector3(R(-halfW * 0.5f, halfW * 0.5f), y + Mathf.Clamp(height * 0.55f, 3.5f, 9f), z0 + len * (0.3f + 0.4f * i)), Mathf.Min(6f, halfW * 0.5f), false);
        if (halfW >= 6f && len >= 30f)
            for (float z = z0 + 13f; z < z1 - 8f; z += 20f)
            {
                if (!skipW) LevelKit.DataPillar(root, new Vector3(-halfW + 1.2f, y, z), Mathf.Min(3.5f, height - 1.5f));
                if (!skipE) LevelKit.DataPillar(root, new Vector3(halfW - 1.2f, y, z + 10f), Mathf.Min(3.5f, height - 1.5f));
            }
        if (len >= 20f)
        {
            LevelKit.FloorRing(root, new Vector3(0f, y, z0 + len * 0.5f), Mathf.Min(3f, halfW * 0.5f));
            LevelKit.Chevrons(root, new Vector3(0f, y, z0 + 2f), Vector3.forward, 3);
        }
        if (!openSky && height >= 8f && halfW >= 5f)
            for (float z = z0 + 14f; z < z1 - 8f; z += 28f)
                LevelKit.Skylight(root, new Vector3(0f, top, z), new Vector2(Mathf.Min(6f, halfW), 5f));
    }

    /// The headquarters' version of Dress: the eye in the corner, the company on
    /// the high walls, and the rot following you in
    static void DressOffice(Transform root, Cursor c, float halfW, float floorY, float height, float z0, float z1, bool openSky)
    {
        float len = z1 - z0;
        LevelKit.CameraDome(root, new Vector3(halfW - 0.4f, floorY + height - 1f, z0 + len * 0.2f), new Vector3(-1f, -0.6f, 0.4f));
        if (height >= 10f)
        {
            float bh = Mathf.Min(4f, height * 0.25f);
            LevelKit.Billboard(root, new Vector3(-halfW + 0.1f, floorY + height * 0.8f, z0 + len * R(0.25f, 0.45f)), Mathf.Min(12f, len * 0.16f), bh, 90f);
            LevelKit.Billboard(root, new Vector3(halfW - 0.1f, floorY + height * 0.8f, z0 + len * R(0.55f, 0.8f)), Mathf.Min(10f, len * 0.14f), bh * 0.85f, 90f);
        }
        if (c.prevZone == Zone.Underbelly && c.hasWall)
        {
            LevelKit.Tendril(root, new Vector3(3.6f, floorY, z0 + 0.6f), new Vector3(halfW * 0.5f, floorY + height * 0.55f, z0 + len * 0.12f), 0.45f, 9);
            LevelKit.Tendril(root, new Vector3(-3.4f, floorY + 0.2f, z0 + 0.6f), new Vector3(-halfW * 0.7f, floorY + height * 0.3f, z0 + len * 0.08f), 0.35f, 7);
        }
    }

    // ------------------------------------------------------------ the headquarters

    /// The entrance: a lit runner, planters, three security gates to slide under, the security desk
    public static Cursor Entrance(Transform root, MovementConfig config, Cursor c, float len = 30f, float halfW = 18f, float height = 9f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY;
        var door = Door(y);
        Seal(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), NoSouth, door);
        LevelKit.DoorFrame(root, door, z1);
        LevelKit.Sign(root, new Vector3(0f, y + Mathf.Min(height - 1.5f, 6.6f), z1), Vector3.back, 8f, 1.6f, "RECEPTION");
        LevelKit.Runner(root, new Vector3(0f, y, z0 + len * 0.5f), new Vector2(3.6f, len - 2f));
        LevelKit.PlanterBox(root, new Vector3(-halfW * 0.55f, y, z0 + 4f), new Vector3(2.4f, 0.6f, 2.4f), 4f);
        LevelKit.PlanterBox(root, new Vector3(halfW * 0.55f, y, z0 + 4f), new Vector3(2.4f, 0.6f, 2.4f), 3.5f);
        LevelKit.PlanterBox(root, new Vector3(-halfW * 0.55f, y, z1 - 4f), new Vector3(2.4f, 0.6f, 2.4f), 3.5f);
        // Security: three gates across the runner (slide under the paddles or hop them), belts either side, a second line to weave
        LevelKit.TurnstileLine(root, new Vector3(0f, y, z0 + 12f), 0f, 3);
        LevelKit.Queue(root, new Vector3(-3.4f, y, z0 + 12f), Vector3.left, 3, 2.8f);
        LevelKit.Queue(root, new Vector3(3.4f, y, z0 + 12f), Vector3.right, 3, 2.8f);
        LevelKit.Queue(root, new Vector3(4.5f, y, z0 + 18f), Vector3.left, 3, 2.8f);
        // The security desk on the east, its screens and the world's clocks behind it, a ticker on the west
        float deskZ = z0 + len * 0.6f;
        LevelKit.Desk(root, new Vector3(halfW - 1.8f, y, deskZ), 6f, -90f);
        LevelKit.Bell(root, new Vector3(halfW - 1.8f, y + 1.21f, deskZ + 1.5f));
        LevelKit.Kiosk(root, new Vector3(halfW - 4.5f, y, deskZ - 4.5f), -120f);
        LevelKit.MonitorWall(root, new Vector3(halfW - 0.1f, y + 4.4f, deskZ), Vector3.left, 3.4f, 1.8f);
        string[] places = { "THE HOUSE", "THE PENTHOUSE", "BELOW STAIRS" };
        for (int i = 0; i < 3; i++)
            LevelKit.Clock(root, new Vector3(halfW - 0.1f, y + 6.6f, deskZ + (i - 1) * 2.2f), Vector3.left, places[i], R(0f, 360f), R(0f, 360f));
        LevelKit.Ticker(root, new Vector3(-halfW + 0.1f, y + 7f, z0 + len * 0.5f), Vector3.right, 12f);
        LevelKit.Cart(root, new Vector3(-halfW + 3.5f, y, z0 + 7f), 20f);
        LevelKit.Copier(root, new Vector3(halfW - 5f, y, z1 - 6f), -35f);
        LevelKit.WaterCooler(root, new Vector3(-halfW + 1.2f, y, z1 - 5f));
        LevelKit.LightRing(root, new Vector3(0f, y + height - 3.4f, z0 + len * 0.5f), y + height, false);
        detailSkipWest = true; // the ticker wall
        Dress(root, c, halfW, y, height, z0, z1);
        Enemy.CreateDummy(new Vector3(halfW - 3.6f, y, deskZ), config);   // security, behind the desk
        Enemy.CreateDummy(new Vector3(-5f, y, z1 - 8f), config);
        Last.spawn = new Vector3(0f, y + 1f, z0 + 6f);
        Last.menuPortal = new Vector3(0f, y + 2f, z0 + 1.7f);
        return Exit(c, z1, y, halfW, y, y + height);
    }

    /// The reception hall: eighty metres of desk to run along, security lines to
    /// weave, mail and copiers to boost off, a glass mezzanine with its sentries,
    /// and the screens, the display and the clocks on the wall behind the desk
    public static Cursor Reception(Transform root, MovementConfig config, Cursor c, float len = 100f, float halfW = 22f, float height = 12f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY;
        var door = Door(y);
        Seal(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), NoSouth, door);
        LevelKit.DoorFrame(root, door, z1);
        LandGlass(root, c);

        float deskX = -halfW + 1.9f;
        LevelKit.Desk(root, new Vector3(deskX, y, z0 + len * 0.5f), len - 16f, 90f);
        LevelKit.Bell(root, new Vector3(deskX, y + 1.21f, z0 + len * 0.3f));
        LevelKit.Bell(root, new Vector3(deskX, y + 1.21f, z0 + len * 0.72f));
        for (int i = 0; i < 3; i++)
            LevelKit.MonitorWall(root, new Vector3(-halfW + 0.1f, y + 4.6f, z0 + len * (0.22f + 0.24f * i)), Vector3.right, 5f, 2.2f);
        LevelKit.Display(root, new Vector3(-halfW + 0.1f, y + 7.6f, z0 + len * 0.5f), Vector3.right, 6f, 1.6f, "NOW ATTENDING   0042");
        string[] places = { "THE HOUSE", "THE SANATORIUM", "THE PENTHOUSE", "BELOW STAIRS", "THE RAILWAYS" };
        for (int i = 0; i < 5; i++)
            LevelKit.Clock(root, new Vector3(-halfW + 0.1f, y + 9.6f, z0 + len * 0.5f + (i - 2) * 3f), Vector3.right, places[i], R(0f, 360f), R(0f, 360f));
        detailSkipWest = true; // the wall behind the desk is dressed by hand

        // Security lines across the hall — belts, gates, belts — each open at the far end
        LevelKit.Queue(root, new Vector3(-7f, y, z0 + len * 0.3f), Vector3.right, 5, 2.8f);
        LevelKit.TurnstileLine(root, new Vector3(2.5f, y, z0 + len * 0.5f), 0f, 5);
        LevelKit.Queue(root, new Vector3(-7f, y, z0 + len * 0.7f), Vector3.right, 5, 2.8f);
        var avoid = new List<Vector3>();
        float[] tz = { 0.18f, 0.4f, 0.6f, 0.82f };
        for (int i = 0; i < 4; i++)
        {
            var p = new Vector3(i % 2 == 0 ? -4f : 2f, y, z0 + len * tz[i]);
            if (i % 2 == 0) LevelKit.Cart(root, p, R(-40f, 40f));
            else LevelKit.Copier(root, p, R(-30f, 30f));
            avoid.Add(p);
        }
        for (int i = 0; i < 3; i++)
        {
            var p = new Vector3(7f, y, z0 + len * (0.12f + 0.25f * i));
            LevelKit.Column(root, p, height, 1.4f);
            avoid.Add(p);
        }
        // The glass mezzanine along the east wall, its stair, its balustrade
        float mezY = y + 5f;
        LevelKit.FloorSlab(root, new Vector3(halfW - 2.5f, mezY - 0.5f, (z0 + 16f + z1 - 6f) * 0.5f), new Vector3(5f, 1f, z1 - 6f - (z0 + 16f)), "Mezzanine");
        LevelKit.Ramp(root, halfW - 2.5f, 4.5f, z0 + 4f, y, z0 + 16f, mezY);
        LevelKit.GlassRail(root, new Vector3(halfW - 5f, mezY, z0 + 16f), new Vector3(halfW - 5f, mezY, z1 - 6f));
        LevelKit.LightRing(root, new Vector3(0f, y + 8f, z0 + len * 0.35f), y + height, true);
        LevelKit.LightRing(root, new Vector3(0f, y + 8.5f, z0 + len * 0.72f), y + height, true);
        LevelKit.PlanterBox(root, new Vector3(halfW - 8f, y, z0 + 3f), new Vector3(2.4f, 0.6f, 2.4f), 3.5f);
        LevelKit.PlanterBox(root, new Vector3(-3f, y, z1 - 3f), new Vector3(2.4f, 0.6f, 2.4f), 4f);
        LevelKit.WaterCooler(root, new Vector3(halfW - 6f, y, z1 - 4f));
        Dress(root, c, halfW, y, height, z0, z1);
        EnemyPool.Air(new Vector3(halfW - 2.5f, mezY + 1.5f, z0 + len * 0.4f), config);
        EnemyPool.Air(new Vector3(halfW - 2.5f, mezY + 1.5f, z0 + len * 0.75f), config);
        Enemy.CreateDummy(new Vector3(-halfW + 3.4f, y, z0 + len * 0.45f), config);   // the receptionist
        Enemy.CreateDummy(new Vector3(halfW - 2.5f, mezY, z0 + len * 0.58f), config);
        Squad(config, -6f, 3f, z0 + 12f, z1 - 12f, y, y + 6f, 2, 0, 0, avoid);       // the porters
        return Exit(c, z1, y, halfW, y, y + height);
    }

    /// The lifts: wall-kick up the shaft between the glass doors — L, M, P — to the landing
    public static Cursor LiftBank(Transform root, MovementConfig config, Cursor c, float rise = 12f, float gap = 10f, float len = 16f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY, top = c.floorY + rise;
        float halfW = gap * 0.5f;
        float height = rise + 5f;
        var door = Door(top);
        Seal(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), NoSouth, door);
        LevelKit.DoorFrame(root, door, z1);
        LevelKit.FloorSlab(root, new Vector3(0f, top - 0.5f, z1 - 1.5f), new Vector3(gap, 1f, 3f), "LiftLanding");
        LevelKit.Orb(root, new Vector3(0f, top + 2.5f, z0 + len * 0.4f));
        LevelKit.Runner(root, new Vector3(0f, y, z0 + len * 0.5f), new Vector2(gap - 1f, len - 2f));
        string[] floors = { "L", "M", "P" };
        for (int i = 0; i < 3; i++)
        {
            float dy = y + 2.3f + i * (rise * 0.5f);
            LevelKit.LiftDoors(root, new Vector3(-halfW + 0.1f, dy, z0 + len * (0.3f + 0.2f * (i % 2))), Vector3.right, floors[i]);
            LevelKit.LiftDoors(root, new Vector3(halfW - 0.1f, dy, z0 + len * (0.5f - 0.2f * (i % 2))), Vector3.left, floors[i]);
        }
        LevelKit.NeonStrip(root, new Vector3(-halfW + 0.08f, y + 1.0f, z0 + len * 0.5f), new Vector3(0.08f, 0.06f, len - 1f), LevelKit.Led);
        LevelKit.NeonStrip(root, new Vector3(halfW - 0.08f, y + 1.0f, z0 + len * 0.5f), new Vector3(0.08f, 0.06f, len - 1f), LevelKit.Led);
        LevelKit.Stripes(root, new Vector3(0f, y + 0.05f, z0 + 2f), new Vector3(gap, 0.08f, 0.1f));
        detailSkipWest = detailSkipEast = true;
        Dress(root, c, halfW, y, height, z0, z1);
        return Exit(c, z1, top, halfW, y, y + height);
    }

    /// The open plan: rows of desks to hop and boost off, glass partitions across
    /// the aisle, glass meeting rooms to kick and stand on, a focus deck up a
    /// short stair, light rings to grapple
    public static Cursor OpenPlan(Transform root, MovementConfig config, Cursor c, float len = 100f, float halfW = 22f, float height = 8f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY;
        var door = Door(y);
        Seal(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), NoSouth, door);
        LevelKit.DoorFrame(root, door, z1);
        LandGlass(root, c);
        var avoid = new List<Vector3>();
        Vector3[] pods = { new Vector3(-10f, y, z0 + len * 0.22f), new Vector3(10f, y, z0 + len * 0.48f), new Vector3(-10f, y, z0 + len * 0.76f) };
        foreach (var p in pods)
        {
            LevelKit.MeetingPod(root, p, new Vector3(6f, 3f, 4.4f), 0f);
            avoid.Add(p);
        }
        float[] rows = { -8.5f, -3.5f, 3.5f, 8.5f };
        int tone = 0;
        for (float z = z0 + 8f; z < z1 - 8f; z += 12f)
            foreach (float x in rows)
            {
                bool nearPod = false;
                foreach (var p in pods) if (Mathf.Abs(p.z - z) < 4.5f && Mathf.Abs(p.x - x) < 5.5f) nearPod = true;
                if (nearPod) continue;
                LevelKit.Workstation(root, new Vector3(x, y, z), x < 0f ? 90f : -90f, tone++);
                avoid.Add(new Vector3(x, y, z));
            }
        for (int i = 0; i < 3; i++)
        {
            float z = z0 + len * (0.3f + 0.2f * i);
            LevelKit.Partition(root, new Vector3(0f, y, z), 0f, 6f);
            avoid.Add(new Vector3(0f, y, z));
        }
        // The focus deck along the east wall, up a short stair, behind glass
        float deckY = y + 4f;
        LevelKit.FloorSlab(root, new Vector3(halfW - 2.5f, deckY - 0.5f, (z0 + 14f + z1 - 6f) * 0.5f), new Vector3(5f, 1f, z1 - 6f - (z0 + 14f)), "FocusDeck");
        LevelKit.Ramp(root, halfW - 2.5f, 4.5f, z0 + 3f, y, z0 + 14f, deckY);
        LevelKit.GlassRail(root, new Vector3(halfW - 5f, deckY, z0 + 14f), new Vector3(halfW - 5f, deckY, z1 - 6f));
        for (int i = 0; i < 4; i++) LevelKit.Lounge(root, new Vector3(halfW - 2.5f, deckY, z0 + 22f + i * 18f), -90f);
        for (int i = 0; i < 3; i++)
            LevelKit.LightRing(root, new Vector3(0f, y + height - 2.6f, z0 + len * (0.2f + 0.3f * i)), y + height, true);
        LevelKit.Ticker(root, new Vector3(-halfW + 0.1f, y + 6.2f, z0 + len * 0.5f), Vector3.right, 16f);
        LevelKit.Copier(root, new Vector3(-6f, y, z0 + 4f), 15f);
        LevelKit.Copier(root, new Vector3(6f, y, z1 - 4f), -160f);
        LevelKit.WaterCooler(root, new Vector3(-halfW + 1.2f, y, z0 + len * 0.6f));
        LevelKit.WaterCooler(root, new Vector3(halfW - 6.5f, y, z1 - 3f));
        Dress(root, c, halfW, y, height, z0, z1);
        Squad(config, -9f, 9f, z0 + 10f, z1 - 10f, y, y + 6f, 2, 2, 2, avoid);
        Enemy.CreateDummy(new Vector3(halfW - 2.5f, deckY, z0 + len * 0.55f), config);
        return Exit(c, z1, y, halfW, y, y + height);
    }

    /// The executive wing: glass doors with nameplates, frosted walls, copiers to
    /// boost off, security shutters half down to slide under
    public static Cursor ExecutiveWing(Transform root, MovementConfig config, Cursor c, float len = 80f, float halfW = 9f, float height = 5f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY;
        var door = Door(y);
        Seal(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), NoSouth, door);
        LevelKit.DoorFrame(root, door, z1);
        LandGlass(root, c);
        string[] names = { "HOUSEKEEPING", "ACCOUNTS", "THE REGISTER", "THE BOARD", "LEGAL", "RECORDS", "QUIET ROOM", "RESIDENCE", "HOSPITALITY", "BOOKINGS" };
        int k = 0;
        for (float z = z0 + 5f; z < z1 - 5f; z += 8f, k++)
        {
            bool west = k % 2 == 0;
            Vector3 facing = west ? Vector3.right : Vector3.left;
            LevelKit.OfficeDoor(root, new Vector3(west ? -halfW + 0.1f : halfW - 0.1f, y, z), facing, names[k % names.Length], k % 3 != 1);
            LevelKit.WallLight(root, new Vector3(west ? -halfW : halfW, y + 2.7f, z + 4f), facing);
            LevelKit.CurtainWall(root, new Vector3(west ? halfW - 0.1f : -halfW + 0.1f, y + 1.7f, z + 2f), -facing, 4f, 2.6f);
        }
        var avoid = new List<Vector3>();
        var copA = new Vector3(-2.6f, y, z0 + len * 0.3f);
        var copB = new Vector3(2.6f, y, z0 + len * 0.66f);
        LevelKit.Copier(root, copA, 90f);
        LevelKit.Cart(root, copB, -90f);
        avoid.Add(copA);
        avoid.Add(copB);
        for (int i = 0; i < 2; i++)
        {
            float z = z0 + len * (0.45f + 0.35f * i);
            const float clear = 1.4f;
            LevelKit.Slab(root, new Vector3(0f, y + clear + (height - clear) * 0.5f, z), new Vector3(halfW * 2f, height - clear, 1.2f), LevelKit.Graphite, "Shutter");
            LevelKit.NeonStrip(root, new Vector3(0f, y + clear + 0.06f, z), new Vector3(halfW * 2f, 0.04f, 1.22f), LevelKit.Amber);
            LevelKit.Stripes(root, new Vector3(0f, y + clear + 0.05f, z - 0.7f), new Vector3(halfW * 2f, 0.1f, 0.1f));
        }
        LevelKit.WaterCooler(root, new Vector3(halfW - 0.9f, y, z0 + len * 0.5f));
        detailSkipWest = detailSkipEast = true;
        Dress(root, c, halfW, y, height, z0, z1);
        Squad(config, -halfW + 1.5f, halfW - 1.5f, z0 + 8f, z1 - 8f, y, y + height - 1.5f, 2, 0, 1, avoid);
        return Exit(c, z1, y, halfW, y, y + height);
    }

    /// The auditorium: tiers of seats stepping up to glass galleries, the all-hands
    /// screen over the stage, light rings to grapple, a glass over the door out
    public static Cursor Auditorium(Transform root, MovementConfig config, Cursor c, float len = 60f, float halfW = 24f, float height = 16f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY;
        var door = Door(y);
        var glass = HighGlass(y + 8f);
        Seal(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), NoSouth, door, glass);
        LevelKit.DoorFrame(root, door, z1);
        LevelKit.Glass(root, new Vector3(0f, y + 10f, z1), new Vector3(6f, 4f, 0.3f), config);
        LandGlass(root, c);
        LevelKit.Runner(root, new Vector3(0f, y, z0 + len * 0.45f), new Vector2(4f, len * 0.7f));
        float rowZ0 = z0 + 6f, rowZ1 = z1 - 14f;
        var avoid = new List<Vector3>();
        for (int side = -1; side <= 1; side += 2)
            for (int t = 0; t < 3; t++)
            {
                float xIn = 3f + 4f * t, xOut = 7f + 4f * t, h = 1.2f * (t + 1);
                LevelKit.FloorSlab(root, new Vector3(side * (xIn + xOut) * 0.5f, y + h * 0.5f, (rowZ0 + rowZ1) * 0.5f), new Vector3(4f, h, rowZ1 - rowZ0), "Tier");
                for (int s = 0; s < 3; s++)
                    LevelKit.Lounge(root, new Vector3(side * (xIn + 2.2f), y + h, rowZ0 + (rowZ1 - rowZ0) * (0.2f + 0.3f * s)), side < 0 ? 90f : -90f);
            }
        // The stage in the north-west with the all-hands screen over it
        LevelKit.FloorSlab(root, new Vector3(-halfW + 7f, y + 0.6f, z1 - 5f), new Vector3(14f, 1.2f, 10f), "Stage");
        LevelKit.NeonStrip(root, new Vector3(-halfW + 7f, y + 1.25f, z1 - 10.05f), new Vector3(14f, 0.04f, 0.06f), LevelKit.Led);
        LevelKit.Display(root, new Vector3(-halfW + 7f, y + 7.5f, z1 - 0.1f), Vector3.back, 12f, 3f, "ALL STAFF  ·  ANNUAL MEETING");
        LevelKit.Kiosk(root, new Vector3(-halfW + 3f, y + 1.2f, z1 - 7f), 150f);
        // Glass galleries along both walls, a hop up from the top tier, and a stair on the east
        float galY = y + 5f;
        LevelKit.FloorSlab(root, new Vector3(-halfW + 2f, galY - 0.5f, z0 + len * 0.5f), new Vector3(4f, 1f, len - 8f), "GalleryW");
        LevelKit.FloorSlab(root, new Vector3(halfW - 2f, galY - 0.5f, z0 + len * 0.5f + 4f), new Vector3(4f, 1f, len - 16f), "GalleryE");
        LevelKit.GlassRail(root, new Vector3(-halfW + 4f, galY, z0 + 4f), new Vector3(-halfW + 4f, galY, z1 - 4f));
        LevelKit.GlassRail(root, new Vector3(halfW - 4f, galY, z0 + 12f), new Vector3(halfW - 4f, galY, z1 - 4f));
        LevelKit.Ramp(root, halfW - 2f, 3.6f, z0 + 1f, y, z0 + 5.5f, y + 1.2f);
        LevelKit.Ramp(root, halfW - 2f, 3.6f, z0 + 5.5f, y + 1.2f, z0 + 12f, galY);
        for (int i = 0; i < 3; i++)
            LevelKit.LightRing(root, new Vector3(0f, y + height - 5f, z0 + len * (0.25f + 0.25f * i)), y + height, true);
        LevelKit.HoloBanner(root, new Vector3(0f, y + height - 1.6f, z0 + len * 0.3f), 3f, 4f, y + height, 1);
        LevelKit.HoloBanner(root, new Vector3(0f, y + height - 1.6f, z0 + len * 0.7f), 3f, 4f, y + height, 4);
        Dress(root, c, halfW, y, height, z0, z1);
        Squad(config, -2.5f, 2.5f, z0 + 8f, z1 - 14f, y, y + 9f, 2, 2, 1, avoid);
        Enemy.CreateDummy(new Vector3(-11f, y + 2.4f, z0 + len * 0.4f), config);
        Enemy.CreateDummy(new Vector3(11f, y + 2.4f, z0 + len * 0.5f), config);
        Enemy.CreateDummy(new Vector3(-halfW + 7f, y + 1.2f, z1 - 5f), config); // on stage
        Enemy.CreateJanitor(new Vector3(0f, y, z1 - 17f), config);               // the cleaner, at the all-hands
        return Exit(c, z1, y, halfW, y, y + height, y + 8f);
    }

    /// The server hall: two rows of racks to kick between, cable trays overhead,
    /// the rot climbing through the cabinets — the underbelly of the company
    public static Cursor ServerHall(Transform root, MovementConfig config, Cursor c, float len = 60f, float halfW = 11f, float height = 6f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY;
        var door = Door(y);
        Seal(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), NoSouth, door);
        LevelKit.DoorFrame(root, door, z1);
        LandGlass(root, c);
        LevelKit.Stripes(root, new Vector3(-1.9f, y + 0.05f, z0 + len * 0.5f), new Vector3(0.1f, 0.08f, len - 4f));
        LevelKit.Stripes(root, new Vector3(1.9f, y + 0.05f, z0 + len * 0.5f), new Vector3(0.1f, 0.08f, len - 4f));
        for (int side = -1; side <= 1; side += 2)
            for (float z = z0 + 5f; z < z1 - 4f; z += 3f)
            {
                int slot = Mathf.RoundToInt((z - z0 - 5f) / 3f);
                if (slot % 4 == 3) continue; // a gap every fourth: the way behind the rows
                LevelKit.ServerRack(root, new Vector3(side * 3.2f, y, z), side < 0 ? 90f : -90f);
            }
        LevelKit.Duct(root, new Vector3(-3.2f, y + height - 0.5f, z0 + 1f), new Vector3(-3.2f, y + height - 0.5f, z1 - 1f), 0.9f, 0.5f);
        LevelKit.Duct(root, new Vector3(3.2f, y + height - 0.5f, z0 + 1f), new Vector3(3.2f, y + height - 0.5f, z1 - 1f), 0.9f, 0.5f);
        for (int i = 0; i < 3; i++)
            LevelKit.Cable(root, new Vector3(-3.2f, y + height - 0.85f, z0 + len * (0.25f + 0.25f * i)), new Vector3(3.2f, y + height - 1.1f, z0 + len * (0.25f + 0.25f * i)), 0.06f);
        LevelKit.Tendril(root, new Vector3(-3.2f, y, z0 + len * 0.35f), new Vector3(-2.7f, y + 2.3f, z0 + len * 0.55f), 0.3f, 8);
        LevelKit.Tendril(root, new Vector3(3.2f, y + 2.3f, z0 + len * 0.6f), new Vector3(2.6f, y, z0 + len * 0.85f), 0.25f, 7);
        Dress(root, c, halfW, y, height, z0, z1);
        EnemyPool.Ground(new Vector3(0f, y, z0 + len * 0.5f), config);
        EnemyPool.Air(new Vector3(0f, y + 4.2f, z0 + len * 0.75f), config);
        Enemy.CreateDummy(new Vector3(-4.9f, y, z0 + len * 0.3f), config);
        Enemy.CreateDummy(new Vector3(0f, y, z0 + len * 0.2f), config);
        return Exit(c, z1, y, halfW, y, y + height);
    }

    /// The atrium: the Concierge's desk in the middle of a white floor, a lit runner
    /// and belts from the door, stairs to a glass mezzanine round three walls, light
    /// rings to grapple, the lifts and the welcome on the far wall. The host sleeps
    /// under the desk; the doors seal when he rises.
    public static Cursor Atrium(Transform root, MovementConfig config, Cursor c, float size = 66f, float height = 26f)
    {
        LevelKit.SetZone(c.zone);
        float halfW = size * 0.5f;
        float z0 = c.z, z1 = c.z + size, y = c.floorY, top = y + height;
        Vector3 centre = new Vector3(0f, y, z0 + size * 0.5f);
        var door = Door(y);
        Seal(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, top, z1), NoSouth, door);
        LevelKit.DoorFrame(root, door, z1);
        LandGlass(root, c);

        // The floor: a lit runner from the door to the desk, belted both sides, one belt to duck, kiosks either side
        LevelKit.Runner(root, new Vector3(0f, y, z0 + 10f), new Vector2(4.2f, 18f));
        LevelKit.RoundInset(root, centre, 10f);
        LevelKit.Queue(root, new Vector3(-2.8f, y, z0 + 4f), Vector3.forward, 4, 3.4f);
        LevelKit.Queue(root, new Vector3(2.8f, y, z0 + 4f), Vector3.forward, 4, 3.4f);
        LevelKit.Belt(root, new Vector3(-2.8f, y, z0 + 14.2f), new Vector3(2.8f, y, z0 + 14.2f));
        LevelKit.Kiosk(root, new Vector3(-5f, y, z0 + 3f), 150f);
        LevelKit.Kiosk(root, new Vector3(5f, y, z0 + 3f), -150f);

        // The desk: a ring the host stands inside, the bell on its front
        LevelKit.DeskRing(root, centre, 6.5f, 14);
        Transform bell = LevelKit.Bell(root, centre + new Vector3(0f, 1.21f, -6.5f));

        // Stairs up both walls to a glass mezzanine round three sides
        float mezY = y + 6f;
        LevelKit.Ramp(root, -halfW + 2.2f, 4.2f, z0 + 3f, y, z0 + 22.5f, mezY);
        LevelKit.Ramp(root, halfW - 2.2f, 4.2f, z0 + 3f, y, z0 + 22.5f, mezY);
        LevelKit.GlassRail(root, new Vector3(-halfW + 4.4f, y, z0 + 3f), new Vector3(-halfW + 4.4f, mezY, z0 + 21.5f));
        LevelKit.GlassRail(root, new Vector3(halfW - 4.4f, y, z0 + 3f), new Vector3(halfW - 4.4f, mezY, z0 + 21.5f));
        float mezLen = z1 - 1f - (z0 + 21.5f);
        LevelKit.FloorSlab(root, new Vector3(-halfW + 3.5f, mezY - 0.5f, z0 + 21.5f + mezLen * 0.5f), new Vector3(7f, 1f, mezLen), "MezzanineW");
        LevelKit.FloorSlab(root, new Vector3(halfW - 3.5f, mezY - 0.5f, z0 + 21.5f + mezLen * 0.5f), new Vector3(7f, 1f, mezLen), "MezzanineE");
        LevelKit.FloorSlab(root, new Vector3(0f, mezY - 0.5f, z1 - 4f), new Vector3(size - 14f, 1f, 6f), "MezzanineN");
        LevelKit.GlassRail(root, new Vector3(-halfW + 7f, mezY, z0 + 21.5f), new Vector3(-halfW + 7f, mezY, z1 - 7f));
        LevelKit.GlassRail(root, new Vector3(halfW - 7f, mezY, z0 + 21.5f), new Vector3(halfW - 7f, mezY, z1 - 7f));
        LevelKit.GlassRail(root, new Vector3(-halfW + 7f, mezY, z1 - 7f), new Vector3(halfW - 7f, mezY, z1 - 7f));

        // Overhead: light rings to grapple, the company's line floating over the host, a skylight
        for (int k = 0; k < 4; k++)
        {
            float a = (k * 90f + 45f) * Mathf.Deg2Rad;
            LevelKit.LightRing(root, centre + new Vector3(Mathf.Cos(a) * 11f, 15f, Mathf.Sin(a) * 11f), top, true);
        }
        LevelKit.HoloBanner(root, centre + Vector3.up * 21f, 8f, 4.5f, top, 3);
        LevelKit.LightStrip(root, new Vector3(0f, top - 0.1f, centre.z), 14f, true, 2.5f);
        LevelKit.LightStrip(root, new Vector3(0f, top - 0.1f, centre.z), 14f, false, 2.5f);
        LevelKit.Light(root, new Vector3(-12f, y + 17f, centre.z), 40f, 1.6f);
        LevelKit.Light(root, new Vector3(12f, y + 17f, centre.z), 40f, 1.6f);

        // The far wall: the lifts, the display, the welcome, the screens, the world's clocks
        LevelKit.LiftDoors(root, new Vector3(-8.5f, y + 2.3f, z1 - 0.1f), Vector3.back, "P");
        LevelKit.LiftDoors(root, new Vector3(8.5f, y + 2.3f, z1 - 0.1f), Vector3.back, "P");
        TextMesh serving = LevelKit.Display(root, new Vector3(0f, y + 8.4f, z1 - 0.1f), Vector3.back, 7f, 1.7f, "NOW ATTENDING   0041");
        LevelKit.Sign(root, new Vector3(0f, y + 12.2f, z1), Vector3.back, 14f, 2.8f, "ALL ROOMS OCCUPIED");
        LevelKit.MonitorWall(root, new Vector3(-16f, y + 9f, z1 - 0.1f), Vector3.back, 6f, 2.6f);
        string[] places = { "THE HOUSE", "THE SANATORIUM", "THE PENTHOUSE", "BELOW STAIRS", "YOUR ROOM" };
        for (int i = 0; i < 5; i++)
            LevelKit.Clock(root, new Vector3(11f + i * 2.6f, y + 9.4f, z1 - 0.1f), Vector3.back, places[i], R(0f, 360f), R(0f, 360f));

        // Mail, copiers and planters round the floor
        LevelKit.Cart(root, new Vector3(-13f, y, z0 + 13f), 30f);
        LevelKit.Copier(root, new Vector3(13f, y, z0 + 13f), -30f);
        LevelKit.Copier(root, new Vector3(-14f, y, z1 - 13f), -20f);
        LevelKit.Cart(root, new Vector3(14f, y, z1 - 13f), 20f);
        LevelKit.PlanterBox(root, new Vector3(-halfW + 6.5f, y, z0 + 2.5f), new Vector3(2.6f, 0.6f, 2.6f), 4.5f);
        LevelKit.PlanterBox(root, new Vector3(halfW - 6.5f, y, z0 + 2.5f), new Vector3(2.6f, 0.6f, 2.6f), 4.5f);
        LevelKit.PlanterBox(root, new Vector3(-6.5f, y, z1 - 2.5f), new Vector3(2.6f, 0.6f, 2.6f), 4f);
        LevelKit.PlanterBox(root, new Vector3(6.5f, y, z1 - 2.5f), new Vector3(2.6f, 0.6f, 2.6f), 4f);
        LevelKit.WaterCooler(root, new Vector3(-halfW + 8.5f, y, z1 - 3f));
        Dress(root, c, halfW, y, height, z0, z1);

        // The host, asleep under his desk; the doors; the cutscene; the theme
        var boss = Enemy.CreateBoss(centre, config, 4.5f);
        var host = new GameObject("TheConcierge");
        host.transform.SetParent(root);
        var ctrl = host.AddComponent<BossConcierge>();
        ctrl.config = config;
        ctrl.body = boss;
        ctrl.arenaCenter = centre;
        ctrl.arenaRadius = halfW;
        ctrl.floorY = y;
        ctrl.checkpoint = new Vector3(0f, y + 1f, z0 + 4f);
        // Named for the lint: the Concierge's is the one gated arena in the act (course-grammar), so its seals are allowed
        ctrl.entrySeal = LevelKit.Slab(root, new Vector3(0f, y + 2.2f, z0), new Vector3(7f, 4.4f, 0.8f), LevelKit.Graphite, "BossEntrySeal");
        ctrl.exitSeal = LevelKit.Slab(root, new Vector3(0f, y + 2.2f, z1), new Vector3(7f, 4.4f, 0.8f), LevelKit.Graphite, "BossExitSeal");
        ctrl.servingText = serving;
        ctrl.bell = bell;
        var intro = host.AddComponent<BossIntro>();
        intro.boss = ctrl;
        intro.triggerCenter = new Vector3(0f, y + 2f, z0 + 17f);
        intro.triggerSize = new Vector3(size, 6f, 4f);
        host.AddComponent<BossMusic>();

        Postcard(new Vector3(-halfW * 0.72f, y + 9f, z0 + 5f), centre + Vector3.up * 5f, 70f);
        return Exit(c, z1, y, halfW, y, top);
    }
}
