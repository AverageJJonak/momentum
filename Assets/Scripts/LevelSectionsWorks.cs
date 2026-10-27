using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE UNDERGROUND — Halcyon Railways' metro and what lies beneath it, ACT II's floors (act2-underground, 2026-10-12;
/// the colliery is gone). The dressing for the Works zone in four FLAVOURS a space names before it dresses — STATION
/// (cream tile, the oxblood band and brass rail, mosaics, globes in cages), TUNNEL (iron sets and rivets, cable runs,
/// drips), SEWER (brick piers and arches, the egg's fillets, pipes and gauges; the sewer spaces run in the Underbelly
/// zone and dress through DressSewer), CRYPT (the foundations: limestone pilasters, the old shop signs, work lamps on
/// cables) — the open-chain helpers every Underground space uses, L6's way in (the FORECOURT at street level and the
/// LIFT SHAFT you drop down), and the TERMINUS GATE kept as the vestibule before the Stationmaster's platform (the
/// one gated space). The spaces themselves: LevelSectionsUnderground*.cs (TicketHall, Platform, TunnelGallery,
/// VentShaft, WeirRun, SluiceCourt, SteamGallery, Crypt, SignalGallery, TurntableCourt, TerminusHall, the
/// Stationmaster's platform) and LevelSectionsJanitor (Sanitation). The Works kinds are THE TRACK GANG (Borer =
/// platelayer, Rigger = signalman, Blaster = ganger, Kettler = stoker, Overseer = inspector, Scourer = sweeper,
/// DamageControl = housekeeping, Janitor = Sanitation): FUEL on named lines. The rot never came this deep.
/// </summary>
public static partial class LevelSections
{
    public static Cursor Works(Cursor c) { c.zone = Zone.Works; return c; }

    public enum UndergroundFlavour { Station, Tunnel, Sewer, Crypt }
    static UndergroundFlavour flavour = UndergroundFlavour.Station;
    static string stationName = "HALCYON METRO";

    /// What the next Works dressing is: the flavour, and the name on the mosaics (consumed by the dressing)
    public static void Flavour(UndergroundFlavour f, string name = null)
    {
        flavour = f;
        if (name != null) stationName = name;
    }

    // ------------------------------------------------------------ the detail pass

    /// The Underground's detail pass, by flavour; the lit cream lines along the floor's edges, the ring and chevrons at
    /// the way in, and the company's drone are every flavour's
    static void DetailWorks(Transform root, float halfW, float y, float height, float z0, float z1, bool openSky, bool skipW, bool skipE)
    {
        float len = z1 - z0, mid = z0 + len * 0.5f;
        switch (flavour)
        {
            case UndergroundFlavour.Tunnel: DetailTunnel(root, halfW, y, height, z0, z1, openSky, skipW, skipE); break;
            case UndergroundFlavour.Sewer: DetailSewer(root, halfW, y, height, z0, z1, openSky, skipW, skipE); break;
            case UndergroundFlavour.Crypt: DetailCrypt(root, halfW, y, height, z0, z1, openSky, skipW, skipE); break;
            default: DetailStation(root, halfW, y, height, z0, z1, openSky, skipW, skipE); break;
        }
        LevelKit.NeonStrip(root, new Vector3(-halfW + 1f, y + 0.03f, mid), new Vector3(0.08f, 0.02f, len - 1.5f), LevelKit.LampCream);
        LevelKit.NeonStrip(root, new Vector3(halfW - 1f, y + 0.03f, mid), new Vector3(0.08f, 0.02f, len - 1.5f), LevelKit.LampCream);
        if (len >= 20f)
        {
            LevelKit.FloorRing(root, new Vector3(0f, y, z0 + 4f), Mathf.Min(2.4f, halfW * 0.4f));
            LevelKit.Chevrons(root, new Vector3(0f, y, z0 + 7f), Vector3.forward, 3);
        }
        if (len >= 40f && height >= 6f)
            LevelKit.Drone(root, new Vector3(R(-halfW * 0.4f, halfW * 0.4f), y + Mathf.Clamp(height * 0.6f, 3.5f, 9f), mid), Mathf.Min(6f, halfW * 0.5f), false);
    }

    /// STATION: the tile band and brass rail, fluted cream pilasters every eight metres under a stepped cornice, the
    /// station's name in mosaic on both walls, roundels, globes in cages down the middle and on the walls, WAY OUT
    /// signs, the clock, brass vault ribs, a poster, ticket machines, a ventilation grille, the company's words
    static void DetailStation(Transform root, float halfW, float y, float height, float z0, float z1, bool openSky, bool skipW, bool skipE)
    {
        float len = z1 - z0, top = y + height, mid = z0 + len * 0.5f;
        LevelKit.TileBand(root, halfW, y, z0 + 0.4f, z1 - 0.4f, skipW, skipE);
        for (int s = -1; s <= 1; s += 2)
        {
            if (s < 0 && skipW || s > 0 && skipE) continue;
            float x = s * halfW;
            Vector3 facing = Vector3.left * s;
            for (float z = z0 + 4f; z < z1 - 3f; z += 8f)
                LevelKit.Prim(root, new Vector3(x - s * 0.12f, y + height * 0.5f - 0.3f, z), new Vector3(0.22f, height - 0.6f, 0.9f), LevelKit.Fluted, "Pilaster");
            if (!openSky)
            {
                LevelKit.Prim(root, new Vector3(x - s * 0.16f, top - 0.25f, mid), new Vector3(0.3f, 0.5f, len - 0.2f), LevelKit.Cream, "Cornice");
                LevelKit.Prim(root, new Vector3(x - s * 0.1f, top - 0.65f, mid), new Vector3(0.18f, 0.3f, len - 0.2f), LevelKit.Cream, "CorniceStep");
            }
            for (float z = z0 + 9f; z < z1 - 6f; z += 10f) LevelKit.WallGlobe(root, new Vector3(x, y + 3.4f, z), facing);
            if (len >= 30f) LevelKit.Mosaic(root, new Vector3(x, y + 2.75f, z0 + len * (s < 0 ? 0.3f : 0.68f)), facing, stationName, Mathf.Min(7f, len * 0.16f), 1.1f);
            if (len >= 24f && height >= 6f) LevelKit.Roundel(root, new Vector3(x, y + 4.6f, z0 + len * (s < 0 ? 0.56f : 0.22f)), facing, "HALCYON RAILWAYS");
            for (float z = z0 + len * 0.4f; z < z1 - 8f; z += 30f)
                LevelKit.HoloSign(root, new Vector3(x - s * 0.15f, y + 3.9f, z + (s > 0 ? 7f : 0f)), facing, 3.4f, 1f, s < 0 ? "WAY OUT" : "TO THE TRAINS");
        }
        if (!openSky)
        {
            float py = y + Mathf.Min(height - 2.2f, 7f);
            for (float z = z0 + 10f; z < z1 - 6f; z += 12f) LevelKit.GlobeCage(root, new Vector3(0f, py, z), top);
            if (len >= 24f && height >= 8f)
                for (float z = z0 + 8f; z < z1 - 6f; z += 16f)
                    LevelKit.Beam(root, new Vector3(-halfW + 0.3f, top - 0.3f, z), new Vector3(halfW - 0.3f, top - 0.3f, z), 0.5f, 0.45f, LevelKit.Cream);
            if (len >= 30f && height >= 7f) LevelKit.ClockFace(root, new Vector3(0f, top - 2.4f, z0 + len * 0.62f), Vector3.back, Mathf.Min(2.2f, height * 0.2f), 290f, 50f);
        }
        if (height >= 7f && halfW >= 8f)
            for (float z = z0 + 16f; z < z1 - 10f; z += 42f)
                LevelKit.WallFan(root, new Vector3(halfW, y + height * 0.7f, z), Vector3.left, Mathf.Min(1.4f, height * 0.11f));
        if (halfW >= 8f && len >= 30f && !skipW)
            for (float z = z0 + 15f; z < z1 - 8f; z += 34f)
                LevelKit.Terminal(root, new Vector3(-halfW + 1.2f, y, z), 90f);
        LevelKit.Stencil(root, new Vector3(-halfW + 0.1f, y + 2.05f, z0 + len * R(0.35f, 0.5f)), Vector3.right, "NO SMOKING  ·  NO SPITTING", 0.07f, 5f);
        LevelKit.Stencil(root, new Vector3(halfW - 0.1f, y + 2.05f, z0 + len * R(0.55f, 0.8f)), Vector3.left, "MIND THE GAP", 0.08f, 4f);
    }

    /// TUNNEL: iron sets every six metres, a pipe gallery and a lit cable run, globes on the walls, work lamps on their
    /// yokes, drips off the crown, LIVE RAIL in red, the company's words in soot
    static void DetailTunnel(Transform root, float halfW, float y, float height, float z0, float z1, bool openSky, bool skipW, bool skipE)
    {
        float len = z1 - z0, top = y + height, mid = z0 + len * 0.5f;
        if (!openSky && len >= 12f)
            for (float z = z0 + 5f; z < z1 - 3f; z += 6f) LevelKit.PortalFrame(root, z, halfW, y, Mathf.Min(height, 12f));
        if (!skipW) LevelKit.PipeGallery(root, new Vector3(-halfW + 0.05f, y + 2.6f, z0 + 0.5f), new Vector3(-halfW + 0.05f, y + 2.6f, z1 - 0.5f), Vector3.right, height >= 8f ? 3 : 2);
        if (!skipE) LevelKit.CableRun(root, new Vector3(halfW - 0.25f, y + 3.3f, z0 + 0.5f), new Vector3(halfW - 0.25f, y + 3.3f, z1 - 0.5f), LevelKit.Led);
        for (float z = z0 + 7f; z < z1 - 5f; z += 12f)
        {
            if (!skipW) LevelKit.WallGlobe(root, new Vector3(-halfW, y + 3.6f, z), Vector3.right);
            if (!skipE && z + 6f < z1 - 5f) LevelKit.WallGlobe(root, new Vector3(halfW, y + 3.6f, z + 6f), Vector3.left);
        }
        float fy = y + Mathf.Min(height - 0.8f, 6.5f);
        for (float z = z0 + 14f; z < z1 - 8f; z += 30f)
            LevelKit.Floodlight(root, new Vector3(skipW ? halfW - 0.6f : -halfW + 0.6f, fy, z), new Vector3(skipW ? -0.7f : 0.7f, -0.5f, 0.2f), 18f, 2f);
        if (!openSky) LevelKit.Drips(root, new Vector3(-halfW + 1f, top - 1.4f, z0 + 2f), new Vector3(halfW - 1f, top - 0.8f, z1 - 2f));
        if (!skipW) for (float z = z0 + 12f; z < z1 - 6f; z += 28f) LevelKit.HoloSign(root, new Vector3(-halfW + 0.15f, y + 4.6f, z), Vector3.right, 4.2f, 1.2f, "LIVE RAIL", true);
        if (!skipE) for (float z = z0 + 22f; z < z1 - 6f; z += 32f) LevelKit.HoloSign(root, new Vector3(halfW - 0.15f, y + 4.6f, z), Vector3.left, 4.6f, 1.2f, "KEEP OFF THE TRACK", true);
        LevelKit.Stencil(root, new Vector3(-halfW + 0.1f, y + 2.1f, z0 + len * R(0.3f, 0.5f)), Vector3.right, "RUNNING TUNNEL  ·  " + stationName, 0.08f, 7f);
        LevelKit.Stencil(root, new Vector3(halfW - 0.1f, y + 2.3f, z0 + len * R(0.55f, 0.8f)), Vector3.left, "REFUGES EVERY 25 M", 0.08f, 6f);
    }

    /// SEWER: brick piers and arches, the egg's fillets along the crown, an iron rail at waist height, pipes with brass
    /// valves and cream dials, gas lamps, drips, a decorative vent, the district's words
    static void DetailSewer(Transform root, float halfW, float y, float height, float z0, float z1, bool openSky, bool skipW, bool skipE)
    {
        float len = z1 - z0, top = y + height, mid = z0 + len * 0.5f;
        float pierH = Mathf.Min(height - 1.5f, 7f);
        for (int s = -1; s <= 1; s += 2)
        {
            if (s < 0 && skipW || s > 0 && skipE) continue;
            float x = s * halfW;
            Vector3 facing = Vector3.left * s;
            float prev = -1f;
            for (float z = z0 + 5f; z < z1 - 3f; z += 10f)
            {
                LevelKit.BrickPier(root, new Vector3(x, y, z), facing, pierH, 1.2f);
                if (prev > 0f) LevelKit.BrickArch(root, new Vector3(x, y + pierH, prev), new Vector3(x, y + pierH, z), facing, 1.2f);
                prev = z;
            }
            if (!openSky)
            {
                var fillet = LevelKit.Prim(root, new Vector3(x - s * 0.7f, top - 0.7f, mid), new Vector3(1.5f, 1.5f, len - 0.2f), LevelKit.Brick, "EggFillet");
                fillet.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            }
            LevelKit.Prim(root, new Vector3(x - s * 0.1f, y + 1.15f, mid), new Vector3(0.05f, 0.05f, len - 0.4f), LevelKit.Iron, "SewerRail");
            for (float z = z0 + 6f; z < z1 - 4f; z += 14f) LevelKit.GasLamp(root, new Vector3(x, y + 3.6f, z + (s > 0 ? 7f : 0f)), facing, 1.1f);
            LevelKit.Pipe(root, new Vector3(x - s * 0.6f, y + Mathf.Min(height - 2.5f, 4.6f), z0 + 2f), new Vector3(x - s * 0.6f, y + Mathf.Min(height - 2.5f, 4.6f), z1 - 2f), 0.5f);
            for (float z = z0 + 11f; z < z1 - 6f; z += 24f)
            {
                LevelKit.Valve(root, new Vector3(x - s * 0.6f, y + Mathf.Min(height - 2.5f, 4.6f), z), Vector3.forward);
                LevelKit.Gauge(root, new Vector3(x - s * 0.05f, y + 3f, z + 2f), facing);
            }
        }
        if (!openSky) LevelKit.Drips(root, new Vector3(-halfW + 1f, top - 1.6f, z0 + 2f), new Vector3(halfW - 1f, top - 1f, z1 - 2f));
        if (!skipW) for (float z = z0 + 18f; z < z1 - 8f; z += 26f) LevelKit.SteamVent(root, new Vector3(-halfW + 0.45f, y + 1.9f, z), new Vector3(0.75f, 0.45f, 0.1f), 6.5f + Mathf.Repeat(z, 3f), Mathf.Repeat(z * 0.11f, 1f), 0.25f);
        LevelKit.Stencil(root, new Vector3(-halfW + 0.1f, y + 2.3f, z0 + len * R(0.3f, 0.5f)), Vector3.right, "SEWER DISTRICT 4  ·  HIGH WATER", 0.08f, 7f);
        LevelKit.Stencil(root, new Vector3(halfW - 0.1f, y + 2.3f, z0 + len * R(0.55f, 0.8f)), Vector3.left, "SANITATION  ·  KEEP MOVING", 0.08f, 6f);
    }

    /// CRYPT: the foundations — limestone pilasters under brick arches, the old shop signs, work lamps on cables,
    /// rubble, dust off the crown
    static void DetailCrypt(Transform root, float halfW, float y, float height, float z0, float z1, bool openSky, bool skipW, bool skipE)
    {
        float len = z1 - z0, top = y + height, mid = z0 + len * 0.5f;
        float pierH = Mathf.Min(height - 1f, 8f);
        string[] shops = { "HALCYON & SONS  ·  EST. 1899", "ROOMS  ·  BY THE WEEK", "THE OLD HIGH STREET", "TOBACCONIST", "CLOSED 1922  ·  BY ORDER" };
        for (int s = -1; s <= 1; s += 2)
        {
            if (s < 0 && skipW || s > 0 && skipE) continue;
            float x = s * halfW;
            Vector3 facing = Vector3.left * s;
            float prev = -1f;
            int k = s < 0 ? 0 : 2;
            for (float z = z0 + 4f; z < z1 - 3f; z += 9f)
            {
                LevelKit.Prim(root, new Vector3(x - s * 0.21f, y + pierH * 0.5f, z), new Vector3(0.4f, pierH, 1f), LevelKit.Limestone, "CryptPilaster");
                if (prev > 0f)
                {
                    LevelKit.BrickArch(root, new Vector3(x, y + pierH, prev), new Vector3(x, y + pierH, z), facing, 1f);
                    LevelKit.Stencil(root, new Vector3(x - s * 0.1f, y + 3.1f, (prev + z) * 0.5f), facing, shops[k++ % shops.Length], 0.08f, 6.5f);
                }
                prev = z;
            }
        }
        float fy = y + Mathf.Min(height - 1f, 6f);
        for (float z = z0 + 10f; z < z1 - 6f; z += 20f)
        {
            int side = Mathf.RoundToInt(z) % 2 == 0 ? -1 : 1;
            LevelKit.Cable(root, new Vector3(-halfW + 0.3f, fy + 0.6f, z), new Vector3(halfW - 0.3f, fy + 0.4f, z), 0.06f);
            LevelKit.Floodlight(root, new Vector3(side * (halfW - 2f), fy, z), new Vector3(-side * 0.5f, -0.8f, 0.1f), 16f, 1.6f);
        }
        LevelKit.Rubble(root, new Vector3(-halfW + 2.4f, y, z0 + len * R(0.2f, 0.4f)), 1.4f);
        LevelKit.Rubble(root, new Vector3(halfW - 2.6f, y, z0 + len * R(0.55f, 0.85f)), 1.6f);
        if (!openSky) LevelKit.Drips(root, new Vector3(-halfW + 1f, top - 1.4f, z0 + 2f), new Vector3(halfW - 1f, top - 0.8f, z1 - 2f));
        if (len >= 30f) LevelKit.Mosaic(root, new Vector3(-halfW, y + 2.2f, z0 + len * 0.5f), Vector3.right, "OLD TOWN  ·  CLOSED", 5f, 1f);
    }
}

public static partial class LevelSections
{
    /// The Underground's version of Dress: the plant's density only where the flavour is industrial (tunnels, sewers;
    /// light in the crypt; never in a station), the eye in the corner, a poster, warm light from above. The flavour and
    /// the name are consumed here
    static void DressWorks(Transform root, Cursor c, float halfW, float floorY, float height, float z0, float z1, bool openSky)
    {
        float len = z1 - z0;
        var f = flavour;
        bool industrial = f == UndergroundFlavour.Tunnel || f == UndergroundFlavour.Sewer || f == UndergroundFlavour.Crypt;
        if (!denseSkipRoom && industrial) LevelKit.Densify(root, halfW, floorY, height, z0, z1, openSky, denseSkipWest, denseSkipEast, !denseLight && f != UndergroundFlavour.Crypt);
        denseSkipRoom = denseLight = false;
        LevelKit.CameraDome(root, new Vector3(halfW - 0.4f, floorY + height - 1f, z0 + len * 0.2f), new Vector3(-1f, -0.6f, 0.4f));
        if (f == UndergroundFlavour.Station && height >= 7f && len >= 30f)
            LevelKit.Billboard(root, new Vector3(-halfW + 0.1f, floorY + Mathf.Min(height * 0.6f, 6.5f), z0 + len * R(0.1f, 0.2f)), Mathf.Min(6f, len * 0.12f), Mathf.Min(2.6f, height * 0.22f), 90f);
        WarmLamps(root, halfW, floorY, height, z0, z1);
        flavour = UndergroundFlavour.Station;
        stationName = "HALCYON METRO";
    }

    static void WarmLamps(Transform root, float halfW, float floorY, float height, float z0, float z1)
    {
        float len = z1 - z0;
        int lamps = Mathf.Max(1, Mathf.RoundToInt(len / 22f));
        for (int i = 0; i < lamps; i++)
        {
            var l = LevelKit.Light(root, new Vector3((i % 2 == 0 ? 1f : -1f) * halfW * 0.35f, floorY + height - 1f, z0 + len * (i + 0.5f) / lamps), Mathf.Min(26f, height * 2.4f), 1.5f);
            l.color = LevelKit.LampCreamLight;
        }
    }

    /// The sewer spaces (the Underbelly zone: brick, amber flicker) dress here, not through Dress: the SEWER detail, the
    /// zone's air, the plant's structure without its furniture, warm light — and no vines: the rot never came this deep
    static void DressSewer(Transform root, Cursor c, float halfW, float floorY, float height, float z0, float z1)
    {
        flavour = UndergroundFlavour.Sewer;
        DetailWorks(root, halfW, floorY, height, z0, z1, false, false, false);
        LevelKit.Air(root, c.zone, new Vector3(-halfW, floorY, z0), new Vector3(halfW, floorY + height, z1), false);
        if (!denseSkipRoom) LevelKit.Densify(root, halfW, floorY, height, z0, z1, false, false, false, false);
        denseSkipRoom = denseLight = denseSkipWest = denseSkipEast = false;
        WarmLamps(root, halfW, floorY, height, z0, z1);
        flavour = UndergroundFlavour.Station;
    }

    // ------------------------------------------------------------ the open chain (the Underground's course spaces)

    const LevelKit.Faces OpenRoofed = LevelKit.Faces.East | LevelKit.Faces.West | LevelKit.Faces.Floor | LevelKit.Faces.Ceiling;
    const LevelKit.Faces OpenWalled = LevelKit.Faces.East | LevelKit.Faces.West | LevelKit.Faces.Ceiling;    // the floor drawn by hand

    /// Every Underground space starts here: the zone, and its own seed (Course.Begin's rule: the level's and its place on
    /// the chain, so the same course builds the same way every run whatever is built before it)
    static void OpenBegin(Cursor c)
    {
        LevelKit.SetZone(c.zone);
        rng = new System.Random(unchecked(Seed * 7919 + Mathf.RoundToInt(c.z * 10f) + 17));
    }

    /// The entry plane: whatever keeps the outside out (never across the lane: Seal), the balcony for a high glass in a
    /// wall behind, and the arch that says you crossed into a new space
    static void OpenThreshold(Transform root, Cursor c, float halfW, float floorY, float height)
    {
        Seal(root, c, halfW, floorY, height);
        LandGlass(root, c);
        LevelKit.Arch(root, halfW, floorY, height, c.z + 1.3f);
    }

    /// The way on: no wall, no door. The next space lands on `floorY` inside a lane `halfW` wide under `height`
    static Cursor OpenExit(Cursor c, float z1, float floorY, float halfW, float height)
    {
        var next = Exit(c, z1, floorY, halfW, floorY, floorY + height);
        next.open = true;
        return next;
    }

    /// The run an 18° ramp needs for `rise` (the main line runs it without losing speed)
    static float Run18(float rise) => Mathf.Abs(rise) / Mathf.Tan(18f * Mathf.Deg2Rad);

    /// A unit from the level's roster on a named line (fuel, never a gate)
    static void Pool(MovementConfig config, Vector3 at, string line, bool air = false)
    {
        var e = air ? EnemyPool.Air(at, config) : EnemyPool.Ground(at, config);
        if (e != null) Last?.fuel.Add(new FuelMark { unit = e, line = line });
    }

    /// A CATWALK (a gallery) along the `side` wall, `h` up and `w` wide, from `cz0` to an 18° ramp down before `zEnd`;
    /// before it the plinth LIP that is the way up — a PERFECT corner boost at run speed lands 3.2 m; 6.2 m takes the
    /// WALL COMBO (a 14 m wall run marked by two flutes, a kick off the wall, the boost ×1.6 on the lip). Two sweepers
    /// stand along it.
    static void Catwalk(Transform root, MovementConfig config, Cursor c, int side, float h, float w, float halfW, float y, float cz0, float zEnd, string line)
    {
        float x = side * (halfW - w * 0.5f - 0.3f);
        float cz1 = zEnd - Run18(h);
        if (cz1 - cz0 < 10f) return;
        LevelKit.FloorSlab(root, new Vector3(x, y + h - 0.5f, (cz0 + cz1) * 0.5f), new Vector3(w, 1f, cz1 - cz0), "Catwalk");
        LevelKit.Ramp(root, x, w, cz1, y + h, zEnd, y);
        LevelKit.BrassStrip(root, new Vector3(x - side * (w * 0.5f - 0.1f), y + h, cz0 + 0.3f), new Vector3(x - side * (w * 0.5f - 0.1f), y + h, cz1 - 0.3f));
        float lipZ = cz0 - 3.5f;
        LevelKit.Lip(root, new Vector3(x, y + 0.7f, lipZ), new Vector3(w - 0.4f, 1.4f, 3f));
        if (h > 4f)
        {
            float wr1 = lipZ - 4f, wr0 = wr1 - 14f;
            LevelKit.Flute(root, new Vector3(side * halfW, y + 2.2f, wr0), Vector3.left * side, 4f);
            LevelKit.Flute(root, new Vector3(side * halfW, y + 2.2f, wr1), Vector3.left * side, 4f);
            LevelKit.LineRun(root, new Vector3(side * (halfW - 0.5f), y, wr0), new Vector3(side * (halfW - 0.5f), y, wr1), c.zone);
        }
        else LevelKit.LineRun(root, new Vector3(x, y, lipZ - 12f), new Vector3(x, y, lipZ - 2.2f), c.zone);
        for (int i = 0; i < 2; i++) Course.Fuel(root, config, c, EnemyKind.Scourer, new Vector3(x, y + h, cz0 + 5f + i * 7f), line);
    }

    /// A crane HOOK as an orb: the chain down from `beam`, the hook block, the orb hanging at the hook
    static void HookOrb(Transform root, Vector3 beam, float drop)
    {
        LevelKit.Chain(root, beam, Mathf.Max(1f, drop - 1.2f));
        LevelKit.Prim(root, beam + Vector3.down * (drop - 0.6f), new Vector3(0.7f, 1.2f, 0.4f), LevelKit.Iron, "HookBlock");
        LevelKit.Orb(root, beam + Vector3.down * (drop + 0.8f));
    }

    /// Black water over the last `len` metres behind the cursor (a flooded run-out: a foot deep, walk through it)
    public static void Flood(Transform root, Cursor c, float len)
    {
        float halfW = c.halfW > 1f ? c.halfW : 8f;
        LevelKit.Water(root, new Vector3(0f, c.floorY, c.z - len * 0.5f), new Vector2(halfW * 2f - 1f, len - 1f));
    }

    // ------------------------------------------------------------ the way in

    /// THE FORECOURT: street level behind the House at sundown — the terminus's forecourt, clean and lit and empty — and
    /// the LIFT HEAD cut through its far end under a brass-and-glass canopy, LIFTS OUT OF SERVICE, the shaft open under it.
    /// The MENU door is behind you. Nothing here fights: this is the walk to the edge, and the drop.
    public static Cursor Forecourt(Transform root, MovementConfig config, Cursor c, float len = 46f, float halfW = 22f, float height = 20f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY;
        const float mouthHalf = 8f;
        float mouthZ0 = z0 + 28f, mouthZ1 = z1 - 2f;
        float mouthZ = (mouthZ0 + mouthZ1) * 0.5f;
        Seal(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), LevelKit.Faces.North | LevelKit.Faces.East | LevelKit.Faces.West);   // no floor: laid round the hole; no ceiling: sky
        LevelKit.Clouds(root, new Vector3(0f, 0f, z0 + len * 0.5f), halfW * 1.7f, len * 0.7f, y + height + 6f, 6);

        // The floor around the lift head: the forecourt, the kerbs either side of the opening, the lip past it
        LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, (z0 - 0.5f + mouthZ0) * 0.5f), new Vector3(halfW * 2f + 1f, 1f, mouthZ0 - z0 + 0.5f), "Forecourt");
        float kerbW = halfW - mouthHalf + 0.5f, kerbX = (halfW + 0.5f + mouthHalf) * 0.5f;
        LevelKit.FloorSlab(root, new Vector3(-kerbX, y - 0.5f, mouthZ), new Vector3(kerbW, 1f, mouthZ1 - mouthZ0), "KerbW");
        LevelKit.FloorSlab(root, new Vector3(kerbX, y - 0.5f, mouthZ), new Vector3(kerbW, 1f, mouthZ1 - mouthZ0), "KerbE");
        LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, (mouthZ1 + z1 + 0.5f) * 0.5f), new Vector3(halfW * 2f + 1f, 1f, z1 - mouthZ1 + 0.5f), "Lip");

        // The lift head: a brass edge round the opening, red lamps at its corners, brass posts along its sides, the
        // canopy over it on four columns — HALCYON METRO on the fascia, the clock under the letters, the winding gear on
        // the roof — and three lift cables down into the dark
        LevelKit.BrassStrip(root, new Vector3(-mouthHalf - 0.5f, y, mouthZ0 - 0.5f), new Vector3(mouthHalf + 0.5f, y, mouthZ0 - 0.5f));
        LevelKit.BrassStrip(root, new Vector3(-mouthHalf - 0.5f, y, mouthZ1 + 0.5f), new Vector3(mouthHalf + 0.5f, y, mouthZ1 + 0.5f));
        LevelKit.BrassStrip(root, new Vector3(-mouthHalf - 0.5f, y, mouthZ0 - 0.5f), new Vector3(-mouthHalf - 0.5f, y, mouthZ1 + 0.5f));
        LevelKit.BrassStrip(root, new Vector3(mouthHalf + 0.5f, y, mouthZ0 - 0.5f), new Vector3(mouthHalf + 0.5f, y, mouthZ1 + 0.5f));
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                Vector3 corner = new Vector3(sx * (mouthHalf + 0.9f), y, sz < 0 ? mouthZ0 - 0.9f : mouthZ1 + 0.9f);
                LevelKit.Prim(root, corner + Vector3.up * 0.45f, new Vector3(0.08f, 0.9f, 0.08f), LevelKit.Brass, "LampPost", PrimitiveType.Cylinder);
                LevelKit.Prim(root, corner + Vector3.up * 1f, Vector3.one * 0.3f, LevelKit.SignalRed, "HeadLamp", PrimitiveType.Sphere);
            }
        LevelKit.Queue(root, new Vector3(-mouthHalf - 2.6f, y, mouthZ0 - 1f), Vector3.forward, 5, 3.8f);
        LevelKit.Queue(root, new Vector3(mouthHalf + 2.6f, y, mouthZ0 - 1f), Vector3.forward, 5, 3.8f);
        LevelKit.Column(root, new Vector3(-11.5f, y, mouthZ0 - 1f), 12f, 1.2f);
        LevelKit.Column(root, new Vector3(11.5f, y, mouthZ0 - 1f), 12f, 1.2f);
        LevelKit.Column(root, new Vector3(-11.5f, y, mouthZ1 + 1f), 12f, 1.2f);
        LevelKit.Column(root, new Vector3(11.5f, y, mouthZ1 + 1f), 12f, 1.2f);
        LevelKit.Prim(root, new Vector3(0f, y + 12.3f, mouthZ), new Vector3(26f, 0.6f, mouthZ1 - mouthZ0 + 4f), LevelKit.Cream, "Canopy");
        LevelKit.Prim(root, new Vector3(0f, y + 12.9f, mouthZ), new Vector3(22f, 0.5f, mouthZ1 - mouthZ0), LevelKit.Charcoal, "CanopyStep");
        LevelKit.Prim(root, new Vector3(0f, y + 13.35f, mouthZ), new Vector3(17f, 0.4f, mouthZ1 - mouthZ0 - 4f), LevelKit.Brass, "CanopyCrown");
        LevelKit.Prim(root, new Vector3(0f, y + 11.3f, mouthZ0 - 1.15f), new Vector3(26f, 1.4f, 0.3f), LevelKit.Oxblood, "Fascia");
        LevelKit.Text(root, new Vector3(0f, y + 11.3f, mouthZ0 - 1.32f), Quaternion.LookRotation(Vector3.forward), "HALCYON METRO", 0.42f, LevelKit.SignCream, "FasciaText", 22f, true, 120f, 220f);
        LevelKit.ClockFace(root, new Vector3(0f, y + 9.4f, mouthZ0 - 1.3f), Vector3.back, 2.4f, 290f, 50f);
        LevelKit.Prim(root, new Vector3(0f, y + 14.5f, mouthZ), new Vector3(6f, 2.4f, 4f), LevelKit.RivetIron, "WindingHouse");
        LevelKit.Prim(root, new Vector3(0f, y + 13.6f, mouthZ), new Vector3(2.2f, 0.3f, 2.2f), LevelKit.Brass, "Sheave", PrimitiveType.Cylinder, Quaternion.Euler(90f, 0f, 0f));
        for (int i = 0; i < 3; i++) LevelKit.Cable(root, new Vector3(-4f + i * 4f, y + 12f, mouthZ + (i - 1) * 2f), new Vector3(-4f + i * 4f, y - 34f, mouthZ + (i - 1) * 2f), 0.1f);
        LevelKit.HoloSign(root, new Vector3(0f, y + 3.4f, mouthZ0 - 1.6f), Vector3.back, 8f, 1.6f, "LIFTS OUT OF SERVICE  ·  PLATFORMS 1 & 2 BELOW", true);
        LevelKit.Floodlight(root, new Vector3(-halfW + 1f, y + 8f, mouthZ0 - 4f), new Vector3(0.7f, -0.6f, 0.4f), 26f, 2.4f);
        LevelKit.Floodlight(root, new Vector3(halfW - 1f, y + 8f, mouthZ0 - 4f), new Vector3(-0.7f, -0.6f, 0.4f), 26f, 2.4f);

        // The forecourt: bollards, a queue of brass posts, the ticket kiosk, a ticket machine, benches, luggage, lamp
        // standards with their globes, the station's name in mosaic, the company's poster, a drone on its rounds
        for (int i = 0; i < 4; i++) LevelKit.Bollard(root, new Vector3(-9f + i * 6f, y, z0 + 5f));
        LevelKit.Queue(root, new Vector3(-6f, y, z0 + 12f), Vector3.right, 4, 3f);
        LevelKit.Kiosk(root, new Vector3(halfW - 4f, y, z0 + 10f), -90f);
        LevelKit.Terminal(root, new Vector3(-halfW + 2f, y, z0 + 16f), 90f);
        LevelKit.StationBench(root, new Vector3(-halfW + 4f, y, z0 + 22f), 90f);
        LevelKit.StationBench(root, new Vector3(halfW - 4f, y, z0 + 20f), -90f);
        LevelKit.CrateStack(root, new Vector3(halfW - 5f, y, z0 + 25f), -20f);
        LevelKit.Cart(root, new Vector3(-halfW + 6f, y, z0 + 8f), 30f);
        for (int s = -1; s <= 1; s += 2)
            for (int k = 0; k < 2; k++)
            {
                Vector3 foot = new Vector3(s * (halfW - 3f), y, z0 + 8f + k * 14f);
                LevelKit.Column(root, foot, 6f, 0.3f);
                LevelKit.Prim(root, foot + Vector3.up * 6.3f, Vector3.one * 0.7f, LevelKit.LampCream, "StandardGlobe", PrimitiveType.Sphere);
                var l = LevelKit.Light(root, foot + Vector3.up * 6f, 16f, 1.8f);
                l.color = LevelKit.LampCreamLight;
            }
        LevelKit.Mosaic(root, new Vector3(-halfW, y + 2.75f, z0 + 20f), Vector3.right, "HALCYON METRO  ·  HOUSE STATION", 7f, 1.1f);
        LevelKit.Roundel(root, new Vector3(halfW, y + 4.6f, z0 + 16f), Vector3.left, "HALCYON RAILWAYS");
        LevelKit.Drone(root, new Vector3(0f, y + 7f, z0 + 14f), 8f, true);
        LevelKit.Billboard(root, new Vector3(0f, y + 10f, z1 - 0.15f), 14f, 5f, 0f);
        Dress(root, c, halfW, y, height, z0, mouthZ0 - 1f, true);   // the company's trim stops at the lift head: nothing floats over the hole

        Last.spawn = new Vector3(0f, y + 1f, z0 + 8f);
        Last.menuPortal = new Vector3(0f, y + 2f, z0 + 1.7f);
        Postcard(new Vector3(-halfW * 0.7f, y + 7f, z0 + 6f), new Vector3(0f, y + 1f, mouthZ), 74f);
        // The cursor stops at the lift head: the shaft is built under it, from here
        return new Cursor { z = mouthZ0, floorY = y, zone = c.zone, prevZone = c.zone, hasWall = false };
    }
}

public static partial class LevelSections
{
    /// THE LIFT SHAFT: the square shaft straight down under the lift head — glazed tile, a globe flashing past every four
    /// metres, the depth in mosaic every ten, brass guide rails down the corners, a counterweight hanging halfway, the
    /// brass lift car parked at the bottom by the landing (its gate open on the hall), a DEPTH DIAL at the top and one
    /// at the bottom whose needle follows you down — and its bottom OPEN to the north into the ticket hall: no door. Slam
    /// it: the drop is the way in and the slam at the bottom is the first move of the Underground. Nothing lives in here.
    public static Cursor LiftShaft(Transform root, MovementConfig config, Cursor c, float drop = 40f, float len = 16f, float halfW = 11f)
    {
        OpenBegin(c);
        float z0 = c.z, z1 = c.z + len, top = c.floorY - LevelKit.T, bottom = c.floorY - drop;
        float mid = (z0 + z1) * 0.5f;
        Seal(root, c, halfW, bottom, top - bottom);   // the south face of the shaft, from the bottom to just under the forecourt
        LevelKit.Room(root, new Vector3(-halfW, bottom, z0), new Vector3(halfW, top, z1), LevelKit.Faces.East | LevelKit.Faces.West | LevelKit.Faces.Floor);   // open at the top (the head) and the north (the hall)
        LevelKit.TileBand(root, halfW, bottom, z0 + 0.4f, z1 - 0.4f);
        for (float d = 4f; d < drop - 2f; d += 4f)
        {
            int s = Mathf.RoundToInt(d / 4f) % 2 == 0 ? -1 : 1;
            LevelKit.WallGlobe(root, new Vector3(s * halfW, c.floorY - d, mid + s * 3f), Vector3.left * s);
        }
        for (float d = 10f; d < drop - 3f; d += 10f)
            LevelKit.Mosaic(root, new Vector3(-halfW, c.floorY - d + 1.6f, mid - 4f), Vector3.right, "-" + Mathf.RoundToInt(d) + " M", 2.6f, 0.8f);
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                LevelKit.Prim(root, new Vector3(sx * (halfW - 0.3f), (top + bottom) * 0.5f, mid + sz * (len * 0.5f - 0.4f)), new Vector3(0.2f, top - bottom - 0.2f, 0.2f), LevelKit.Brass, "GuideRail");
        LevelKit.Prim(root, new Vector3(halfW - 0.9f, c.floorY - drop * 0.45f, mid), new Vector3(0.8f, 3f, 0.8f), LevelKit.RivetIron, "Counterweight");
        LevelKit.Cable(root, new Vector3(halfW - 0.9f, c.floorY - drop * 0.45f + 1.5f, mid), new Vector3(halfW - 0.9f, top + 0.5f, mid), 0.06f);
        // The dials: on the shaft's far face just under the head, where you see it before you drop; and by the landing
        LevelKit.DepthDial(root, new Vector3(0f, c.floorY - 2.6f, z1 - 0.1f), Vector3.back, c.floorY, bottom, null, 2f);
        LevelKit.DepthDial(root, new Vector3(halfW - 0.1f, bottom + 3.2f, z0 + 3f), Vector3.left, c.floorY, bottom, null, 1.4f);
        // The bottom: the landing ring, the lift car parked in the corner, the lit line out, the signs
        LevelKit.FloorRing(root, new Vector3(0f, bottom, mid), 4.5f);
        EndingProps.Cage(root, new Vector3(-halfW + 3f, bottom, z0 + 3f), 0f);
        LevelKit.Floodlight(root, new Vector3(halfW - 0.7f, bottom + 5f, z0 + 2f), new Vector3(-0.5f, 0.7f, 0.5f), 30f, 2.6f);
        LevelKit.Chevrons(root, new Vector3(0f, bottom, mid + 2f), Vector3.forward, 3);
        LevelKit.LineRun(root, new Vector3(0f, bottom, mid), new Vector3(0f, bottom, z1 - 0.3f), c.zone);
        LevelKit.Mosaic(root, new Vector3(halfW, bottom + 2.75f, mid), Vector3.left, "LIFTS  ·  PLATFORMS 1 & 2", 6f, 1.1f);
        LevelKit.HoloSign(root, new Vector3(-halfW + 0.15f, bottom + 4.4f, z1 - 4f), Vector3.right, 4.2f, 1.2f, "TICKET HALL");
        LevelKit.Stencil(root, new Vector3(-halfW + 0.1f, bottom + 2.1f, z0 + 3f), Vector3.right, "LIFT 3  ·  BOTTOM LANDING", 0.08f, 6f);
        return OpenExit(c, z1, bottom, halfW, top - bottom);   // the full height: the next space walls the shaft's face over its own ceiling
    }

    // ------------------------------------------------------------ the terminus gate

    /// THE TERMINUS GATE: the vestibule before the Stationmaster's platform (the one gated space in Act II), the two
    /// brass gate leaves drawn back either side of the way through under PLATFORM 9 — and the track gang between you
    /// and them: a platelayer, a signalman, a ganger, roster units and a page. The doorway is the arena's entry seal's
    /// size: this is the one threshold that closes
    public static Cursor TerminusGate(Transform root, MovementConfig config, Cursor c, float len = 36f, float halfW = 22f, float height = 12f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY;
        var door = Door(y);
        Seal(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), NoSouth, door);
        LevelKit.DoorFrame(root, door, z1);
        LandGlass(root, c);
        for (int s = -1; s <= 1; s += 2)
        {
            LevelKit.Slab(root, new Vector3(s * 6.4f, y + 3f, z1 - 0.9f), new Vector3(6f, 6f, 0.5f), LevelKit.Brass, "GateLeaf");
            for (int i = 0; i < 5; i++) LevelKit.Prim(root, new Vector3(s * (3.9f + i * 1.25f), y + 3f, z1 - 1.2f), new Vector3(0.12f, 5.6f, 0.08f), LevelKit.Bronze, "GateBar");
            for (int k = 0; k < 3; k++) LevelKit.Prim(root, new Vector3(s * 6.4f, y + 1f + k * 2f, z1 - 1.2f), new Vector3(5.6f, 0.1f, 0.08f), LevelKit.Bronze, "GateRail");
            LevelKit.Prim(root, new Vector3(s * 6.4f, y + 3f, z1 - 1.27f), new Vector3(1.2f, 1.2f, 0.04f), LevelKit.OxbloodTile, "GateCartouche");
            LevelKit.Prim(root, new Vector3(s * 6.4f, y + 6.5f, z1 - 0.9f), Vector3.one * 0.4f, LevelKit.SignalRed, "GateLamp", PrimitiveType.Sphere);
        }
        LevelKit.Prim(root, new Vector3(0f, y + 6.6f, z1 - 0.56f), new Vector3(20f, 0.5f, 0.1f), LevelKit.Brass, "GateHeadRail");
        LevelKit.Chevrons(root, new Vector3(0f, y, z0 + len * 0.55f), Vector3.forward, 4);
        LevelKit.Sign(root, new Vector3(0f, y + Mathf.Min(height - 1.5f, 8.2f), z1), Vector3.back, 10f, 1.8f, "PLATFORM 9");
        LevelKit.Mosaic(root, new Vector3(0f, y + 5.3f, z1 - 0.1f), Vector3.back, "TERMINUS  ·  ALL CHANGE", 8f, 1.2f);
        LevelKit.SteamVent(root, new Vector3(-4.4f, y + 0.3f, z1 - 1.8f), Vector3.up, 7f, 0f, 0.3f);
        LevelKit.SteamVent(root, new Vector3(4.4f, y + 0.3f, z1 - 1.8f), Vector3.up, 7f, 0.5f, 0.3f);
        var avoid = new List<Vector3>();
        LevelKit.CrateStack(root, new Vector3(-halfW + 4f, y, z0 + 6f), 10f);
        LevelKit.CrateStack(root, new Vector3(halfW - 4f, y, z0 + 6f), -10f);
        LevelKit.Queue(root, new Vector3(-9f, y, z0 + len * 0.42f), Vector3.forward, 3, 2.6f);
        LevelKit.Queue(root, new Vector3(9f, y, z0 + len * 0.42f), Vector3.forward, 3, 2.6f);
        avoid.Add(new Vector3(-9f, y, z0 + len * 0.5f));
        avoid.Add(new Vector3(9f, y, z0 + len * 0.5f));
        LevelKit.GlobeCage(root, new Vector3(0f, y + 7f, z0 + len * 0.3f), y + height);
        LevelKit.GlobeCage(root, new Vector3(0f, y + 7f, z0 + len * 0.7f), y + height);
        LevelKit.Floodlight(root, new Vector3(-halfW + 1f, y + 6f, z1 - 3f), new Vector3(0.7f, -0.5f, -0.5f), 20f, 2.4f);
        LevelKit.Floodlight(root, new Vector3(halfW - 1f, y + 6f, z1 - 3f), new Vector3(-0.7f, -0.5f, -0.5f), 20f, 2.4f);
        Flavour(UndergroundFlavour.Station, "TERMINUS");
        Dress(root, c, halfW, y, height, z0, z1);
        Enemy.CreateBorer(new Vector3(0f, y, z0 + len * 0.7f), config);
        Enemy.CreateRigger(new Vector3(-halfW + 5f, y, z0 + len * 0.75f), config);
        Enemy.CreateBlaster(new Vector3(halfW - 5f, y, z0 + len * 0.72f), config);
        Squad(config, -halfW + 5f, halfW - 5f, z0 + 8f, z0 + len * 0.55f, y, y + height * 0.6f, 2, 1, 0, avoid);
        return Exit(c, z1, y, halfW, y, y + height);
    }
}
