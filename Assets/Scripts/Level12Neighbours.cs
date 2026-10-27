using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE NEIGHBOURS, and who holds what (Level12Builder). The city within a jump
/// of the route stands at the route's own height (RoofKit.Neighbours), the two
/// lanes nearest the route fully solid and gridded to run on; bridges are laid
/// from the route's edges to the nearest of their roofs, every other inner
/// roof has something on it, and a drone hangs over every third so there is a
/// way back up. Then the units on the route itself, the district's defences,
/// the mines and the drones.
/// </summary>
public static partial class Level12Builder
{
    static readonly float[] NeighbourLanes = { -250f, -210f, -60f, 60f, 210f, 250f };
    static List<RoofKit.Block> neighbours;

    /// The height of the route at `z`, the middle way of it: the gate, the cut line, the terrace, the top, the line, the spire
    static float RouteHeight(float z)
    {
        float[] kz = { -300f, 40f, 260f, 520f, 700f, 836f, 900f, 1000f, 1690f, 1800f, 2500f };
        float[] ky = { 84f, 84f, 112f, 146f, 150f, 150f, 300f, 285f, 262f, 250f, 250f };
        for (int i = 1; i < kz.Length; i++)
            if (z <= kz[i]) return Mathf.Lerp(ky[i - 1], ky[i], Mathf.InverseLerp(kz[i - 1], kz[i], z));
        return ky[ky.Length - 1];
    }

    static void Neighbours() => neighbours = RoofKit.Neighbours(RouteHeight, -110f, RouteZ1 + 40f, NeighbourLanes, 100f);

    /// Where a way off the route could go: from these edges, a bridge to the nearest neighbour's roof a bridge can reach
    static readonly Vector3[] Ports =
    {
        new Vector3(-18f, RoofA, 6f), new Vector3(18f, RoofA, 6f),
        new Vector3(-142f, RoofS2, 300f), new Vector3(-108f, RoofS2, 300f), new Vector3(-92f, RoofS4, 640f),
        new Vector3(-18f, RoofC3, 520f), new Vector3(18f, RoofC3, 520f), new Vector3(-18f, RoofC4, 700f), new Vector3(18f, RoofC4, 700f),
        new Vector3(-76f, 150f, 845f), new Vector3(76f, 150f, 845f), new Vector3(-76f, 150f, 915f), new Vector3(76f, 150f, 915f),
        new Vector3(-18f, RoofStation, 1350f), new Vector3(18f, RoofStation, 1350f),
        new Vector3(95.6f, RoofG1, 1050f), new Vector3(-92f, RoofK, 1560f),
    };

    static void OffRoute()
    {
        foreach (var from in Ports)
        {
            if (!RoofKit.NearestRoof(neighbours, from, 60f, 40f, 8f, out _, out Vector3 top, out float hx, out float hz)) continue;
            Vector3 d = top - from; d.y = 0f;
            if (d.sqrMagnitude < 1f) continue;
            d.Normalize();
            Vector3 to = top - new Vector3(d.x * hx * 0.85f, 0f, d.z * hz * 0.85f);
            RoofKit.Bridge(from, to + Vector3.up * 0.02f, 3f);
        }
    }

    /// The neighbours are the level too: every other full roof in the inner lanes has something on it, the ones over
    /// the route have an eye over them, and a drone hangs over every third so there is a way back up to the line
    static void NeighbourUnits()
    {
        int n = 0;
        foreach (var b in neighbours)
        {
            if (!b.full) continue;
            RoofKit.RoofOf(b, out Vector3 top, out float hx, out float hz);
            if (hx < 6f || hz < 3f) continue;
            n++;
            if (n % 2 == 0) EnemyPool.Ground(top + Vector3.up * 0.1f + new Vector3(Random.Range(-hx * 0.4f, hx * 0.4f), 0f, Random.Range(-hz * 0.4f, hz * 0.4f)), cfg);
            if (n % 5 == 0 && top.y > RouteHeight(top.z) - 6f) EnemyPool.Air(top + Vector3.up * 9f, cfg);
            if (n % 3 == 0) LevelKit.Orb(root, top + Vector3.up * 14f);
        }
    }

    // ------------------------------------------------------------ who holds the roofs

    static void Units()
    {
        // The gate roof: a welcome, and one of everything watching the three ways
        Enemy.CreateSkitters(new Vector3(-8f, RoofA, 30f), cfg);
        Enemy.CreateSkitters(new Vector3(10f, RoofA, 14f), cfg);
        Enemy.CreateMarshal(new Vector3(0f, RoofA, 30f), cfg);
        Enemy.CreateSentry(new Vector3(-14f, RoofA, 32f), cfg);
        // The stacks
        Enemy.CreateSentry(new Vector3(-132f, RoofS1, 140f), cfg);
        Enemy.CreateSkitters(new Vector3(-112f, RoofS1, 156f), cfg);
        Enemy.CreateMarshal(new Vector3(-125f, RoofS2, 306f), cfg);
        Enemy.CreateSkitters(new Vector3(-136f, RoofS2, 292f), cfg);
        Enemy.CreateSentry(new Vector3(-100f, RoofS3, 440f), cfg);
        Enemy.CreateMoth(new Vector3(-115f, RoofS3 + 10f, 450f), cfg);
        Enemy.CreateLurker(new Vector3(-126f, RoofS3, 446f), cfg);
        Enemy.CreateWarden(new Vector3(-110f, RoofS4, 636f), cfg);
        Enemy.CreateSkitters(new Vector3(-120f, RoofS4, 648f), cfg);
        Enemy.CreateMarshal(new Vector3(-98f, RoofS4, 646f), cfg);
        // The cut line: the eyes on the fast way
        Enemy.CreateLurker(new Vector3(-14f, BladeC1 * 0.86f, 252f), cfg);
        Enemy.CreateSentry(new Vector3(14f, BladeC1, 268f), cfg);
        Enemy.CreateOcular(new Vector3(50f, 160f, 390f), cfg);
        Enemy.CreateOcular(new Vector3(-50f, 170f, 470f), cfg);
        Enemy.CreateMoth(new Vector3(0f, 158f, 380f), cfg);
        Enemy.CreateMarshal(new Vector3(-10f, RoofC3, 520f), cfg);
        Enemy.CreateSentry(new Vector3(12f, RoofC3, 528f), cfg);
        Enemy.CreateSkitters(new Vector3(8f, RoofC3, 510f), cfg);
        Enemy.CreateWarden(new Vector3(0f, RoofC4, 700f), cfg);
        Enemy.CreateSentry(new Vector3(-14f, RoofC4, 710f), cfg);
        Enemy.CreateSentry(new Vector3(14f, RoofC4, 690f), cfg);
        // The slot: lurkers on the low blades, moths in the slots, a scourer on the rotunda
        Enemy.CreateSkitters(new Vector3(108f, BladeE1 * 0.86f, 192f), cfg);
        Enemy.CreateSentry(new Vector3(134f, BladeE1, 208f), cfg);
        Enemy.CreateLurker(new Vector3(136f, BladeE2 * 0.86f, 372f), cfg);
        Enemy.CreateMoth(new Vector3(115f, BladeE2 + 8f, 380f), cfg);
        Enemy.CreateMoth(new Vector3(135f, BladeE2 + 10f, 380f), cfg);
        Enemy.CreateMarshal(new Vector3(112f, BladeE2, 388f), cfg);
        Enemy.CreateSkitters(new Vector3(115f, BladeE3 * 0.86f, 552f), cfg);
        Enemy.CreateWarden(new Vector3(104f, BladeE3, 568f), cfg);
        Enemy.CreateSentry(new Vector3(128f, BladeE3, 568f), cfg);
        Enemy.CreateScourer(new Vector3(124f, RoundE4, 720f), cfg);
        Enemy.CreateSkitters(new Vector3(112f, RoundE4, 726f), cfg);
        Enemy.CreateOcular(new Vector3(170f, 160f, 600f), cfg);
        // The arcology: the terraces held, the top a stand
        Enemy.CreateScourer(new Vector3(-20f, 150f, 841f), cfg);
        Enemy.CreateSkitters(new Vector3(24f, 150f, 841f), cfg);
        Enemy.CreateKettler(new Vector3(-60f, 150f, 916f), cfg);
        Enemy.CreateBlaster(new Vector3(60f, 150f, 916f), cfg);
        Enemy.CreateMarshal(new Vector3(-66f, 198f, 880f), cfg);
        Enemy.CreateLurker(new Vector3(66f, 198f, 870f), cfg);
        Enemy.CreateSentry(new Vector3(-58f, 237f, 880f), cfg);
        Enemy.CreateWarden(new Vector3(0f, ArcH, 872f), cfg);
        Enemy.CreateMarshal(new Vector3(-30f, ArcH, 880f), cfg);
        Enemy.CreateMarshal(new Vector3(30f, ArcH, 880f), cfg);
        Enemy.CreateSkitters(new Vector3(-20f, ArcH, 890f), cfg);
        Enemy.CreateSkitters(new Vector3(20f, ArcH, 870f), cfg);
        Enemy.CreateOverseer(new Vector3(0f, ArcH + 30f, 880f), cfg);
        Enemy.CreateMoth(new Vector3(-40f, ArcH + 20f, 860f), cfg);
        Enemy.CreateMoth(new Vector3(40f, ArcH + 20f, 900f), cfg);
        // The halo
        Enemy.CreateSkitters(new Vector3(-110f, RoundI, 1112f), cfg);
        Enemy.CreateSentry(new Vector3(-124f, RoundI * 0.72f, 1120f), cfg);
        Enemy.CreateScourer(new Vector3(-120f, RoundJ * 0.72f, 1326f), cfg);
        Enemy.CreateMarshal(new Vector3(-104f, RoundJ * 0.72f, 1340f), cfg);
        Enemy.CreateSentry(new Vector3(-120f, RoundJ, 1330f), cfg);
        Enemy.CreateMoth(new Vector3(-115f, 262f, 1220f), cfg);
        Enemy.CreateOcular(new Vector3(-170f, 270f, 1240f), cfg);
        Enemy.CreateWarden(new Vector3(-110f, RoofK, 1552f), cfg);
        Enemy.CreateSkitters(new Vector3(-98f, RoofK, 1566f), cfg);
        Enemy.CreateSentry(new Vector3(-122f, RoofK, 1568f), cfg);
        // The line: the guns on the deck, two striders, moths over it, the station held
        Vector3 dir = (DeckB - DeckA).normalized;
        Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
        Enemy.CreateSentry(Vector3.Lerp(DeckA, DeckB, 0.3f) + side * 5f, cfg);
        Enemy.CreateSentry(Vector3.Lerp(DeckA, DeckB, 0.6f) - side * 5f, cfg);
        Enemy.CreateMarshal(Vector3.Lerp(DeckA, DeckB, 0.45f) + side * 5f, cfg);
        Enemy.CreatePursuit(Vector3.Lerp(DeckA, DeckB, 0.2f), cfg);
        Enemy.CreatePursuit(Vector3.Lerp(DeckA, DeckB, 0.8f), cfg);
        for (int i = 0; i < 3; i++) Enemy.CreateMoth(Vector3.Lerp(DeckA, DeckB, 0.25f + i * 0.25f) + Vector3.up * 9f, cfg);
        Enemy.CreateMarshal(new Vector3(8f, RoofStation, 1356f), cfg);
        Enemy.CreateHound(new Vector3(-10f, RoofStation, 1344f), cfg);
        Enemy.CreateSkitters(new Vector3(10f, RoofStation, 1340f), cfg);
        // The gardens: hounds in the hedges, the lobby held
        Enemy.CreateHound(new Vector3(104f, RoofG1, 1036f), cfg);
        Enemy.CreateHound(new Vector3(136f, RoofG1, 1064f), cfg);
        Enemy.CreateMarshal(new Vector3(120f, RoofG1, 1066f), cfg);
        Enemy.CreateWarden(new Vector3(130f, LobbyG2, 1310f), cfg);
        Enemy.CreateSkitters(new Vector3(116f, LobbyG2, 1292f), cfg);
        Enemy.CreateKettler(new Vector3(144f, LobbyG2, 1300f), cfg);
        Enemy.CreateSentry(new Vector3(115f, RoundG3, 1546f), cfg);
        Enemy.CreateScourer(new Vector3(108f, RoundG3, 1534f), cfg);
        Enemy.CreateOcular(new Vector3(170f, 290f, 1420f), cfg);
        // The spire: the last stand, every way arriving at once
        Enemy.CreateWarden(new Vector3(-6f, SpireH, 1800f), cfg);
        Enemy.CreateMarshal(new Vector3(-13f, 227.5f, 1800f), cfg);
        Enemy.CreateMarshal(new Vector3(13f, 227.5f, 1800f), cfg);
        Enemy.CreateSkitters(new Vector3(6f, SpireH, 1804f), cfg);
        Enemy.CreateSkitters(new Vector3(-6f, SpireH, 1794f), cfg);
        Enemy.CreateSentry(new Vector3(-14f, 227.5f, 1789f), cfg);
        Enemy.CreateSentry(new Vector3(14f, 227.5f, 1811f), cfg);
        Enemy.CreateLurker(new Vector3(10f, 227.5f, 1789f), cfg);
        Enemy.CreateOcular(new Vector3(30f, 262f, 1830f), cfg);
        Enemy.CreateOverseer(new Vector3(0f, SpireH + 28f, 1780f), cfg);
        Enemy.CreateInterceptor(new Vector3(-30f, SpireH + 14f, 1820f), cfg);
    }

    // ------------------------------------------------------------ the district's own defences

    static void Defences()
    {
        // Sweep lights: on the third stack, on the arcology's top, on the spire's fourth tier, over the deck
        Mast(new Vector3(-100f, RoofS3, 440f), 9f, new Vector3(-4f, 0f, 0f), new Vector3(-115f, RoofS3, 450f), 14f, 12f, 0.3f, 0f);
        Mast(new Vector3(-44f, ArcH, 868f), 12f, new Vector3(4f, 0f, 0f), new Vector3(0f, ArcH, 880f), 44f, 12f, 0.27f, 1.2f);
        Mast(new Vector3(-12f, 227.5f, 1812f), 26f, new Vector3(4f, 0f, 0f), new Vector3(0f, SpireH, 1800f), 11f, 9f, 0.34f, 2.4f);
        Vector3 mid = (DeckA + DeckB) * 0.5f;
        Vector3 side = Vector3.Cross(Vector3.up, (DeckB - DeckA).normalized).normalized;
        Mast(mid + side * 5.5f, 8f, -side * 3f, mid, 8f, 60f, 0.25f, 0.6f);

        // Arc fences across the ways: the second stack's bridge, the skyway down, the deck
        Vector3 b0 = new Vector3(-125f, RoofS2, 315.8f), b1 = new Vector3(-115f, RoofS3, 433.4f);
        LevelKit.ArcGap(root, SurfaceKit.OnSpan(b0, b1, 0.5f, -3f), SurfaceKit.OnSpan(b0, b1, 0.5f, 3f), 3f, 0f, 0.4f);
        Vector3 s0 = new Vector3(-50f, ArcH, 890f), s1 = new Vector3(-110f, RoundI, 1104.4f);
        LevelKit.ArcGap(root, SurfaceKit.OnSpan(s0, s1, 0.5f, -3f), SurfaceKit.OnSpan(s0, s1, 0.5f, 3f), 3.2f, 0.3f, 0.45f);
        LevelKit.ArcGap(root, mid - side * 6f, mid + side * 6f, 3f, 0.5f, 0.4f);
        LevelKit.HoloSign(root, new Vector3(-115f, RoofS3 + 3f, 436f), Vector3.back, 12f, 1f, "SWEEP LIGHTS DIRECT FIRE  ·  STAY OUT OF THE LIGHT", true);
    }

    /// Mines on the stopping places: the landings, the pads, the terraces, the halos, the deck. Never on a line taken at speed
    static void Mines()
    {
        Field(RoofA + 30f, -9f, 30f, 9f, 30f);   // nine metres either side of the cut line's hairline, as the stacks' are (they sat four off it)
        Field(RoofS1 + 30f, -128f, 142f, -112f, 158f);
        Field(RoofS3 + 30f, -120f, 442f, -100f, 462f);
        Field(RoofS4 + 30f, -118f, 630f, -102f, 650f);
        Field(BladeC1 + 30f, -12f, 252f, 12f, 268f);
        Field(RoofC3 + 30f, -10f, 512f, 10f, 528f);
        Field(BladeE1 + 30f, 110f, 192f, 130f, 208f);
        Field(BladeE3 + 30f, 105f, 552f, 125f, 568f);
        Field(180f, -30f, 841f, 30f, 841f, -52f, 843f, 52f, 839f);   // all four on the south ledge (the minefield): the pair that sat on the side ledges' hairlines, at the thermal's foot and on the pads' approach, moved here
        Field(ArcH + 30f, -34f, 884f, 34f, 876f, 0f, 866f);          // the third sat on the north bridge's landing, the way off the top: now the south edge
        Field(RoundI + 30f, -102f, 1120f, -118f, 1120f);
        Field(RoundJ * 0.72f + 30f, -110f, 1340f, -134f, 1320f);
        Field(RoofStation + 30f, -10f, 1340f, 10f, 1360f);
        Field(RoofG1 + 30f, 104f, 1040f, 136f, 1062f);
        Field(SpireH + 30f, -8f, 1798f, 8f, 1802f, -14f, 1789f, 14f, 1811f);
    }

    /// The company's drones, holding station: over every gap, over the cut line's hole, round the arcology, along the
    /// deck, and a line straight from the arcology's top to the spire's for whoever can ride five yanks in a row
    static void Drones()
    {
        float[] at =
        {
            -70f, 100f, 80f,   0f, 108f, 140f,   70f, 100f, 80f,
            -122f, 118f, 225f,   -118f, 132f, 545f,   0f, 150f, 360f,   0f, 154f, 388f,   0f, 152f, 416f,   0f, 158f, 610f,
            120f, 126f, 290f,   122f, 146f, 470f,   118f, 160f, 640f,
            -90f, 250f, 880f,   90f, 250f, 880f,   -80f, 275f, 1000f,   80f, 300f, 960f,
            -115f, 262f, 1220f,   -120f, 316f, 1440f,   0f, 290f, 1180f,   0f, 285f, 1500f,   125f, 300f, 1180f,   120f, 285f, 1430f,
            -60f, 262f, 1690f,   60f, 262f, 1690f,
            0f, 320f, 1000f,   0f, 330f, 1200f,   0f, 320f, 1400f,   0f, 300f, 1600f,   0f, 280f, 1740f,
        };
        for (int i = 0; i + 2 < at.Length; i += 3) LevelKit.Orb(root, new Vector3(at[i], at[i + 1], at[i + 2]));
    }

    // ------------------------------------------------------------ the city round the route

    /// The utopia in every direction, the district you came out of behind you, the house and its plan ahead
    static void Beyond()
    {
        var far = SurfaceKit.Far;
        for (float gx = -2100f; gx <= 2100f; gx += 150f)
            for (float gz = -1200f; gz <= 5600f; gz += 150f)
            {
                float x = gx + Random.Range(-30f, 30f), z = gz + Random.Range(-30f, 30f);
                if (Mathf.Abs(x) < CorridorHalf && z > RouteZ0 && z < RouteZ1) continue;   // the route
                Vector3 foot = new Vector3(x, GroundY, z);
                float w = Random.Range(30f, 60f), d = Random.Range(30f, 55f);
                float k = Mathf.InverseLerp(-1200f, 5600f, z);
                float h = Mathf.Lerp(90f, 330f, k) + Random.Range(-30f, 40f);
                bool lit = z > -400f;
                if (z < -150f && Mathf.Abs(x) < 500f)
                {
                    // The wound, behind you: what the blast left standing there, dark and cut short
                    SurfaceKit.Look(CityKit.Hollow, foot + Vector3.up * (h * 0.2f), new Vector3(w, h * 0.4f, d), Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(0f, 360f), Random.Range(-5f, 5f)));
                    continue;
                }
                float bearing = Mathf.Atan2(House.x - x, House.z - z) * Mathf.Rad2Deg;
                // Near the route a whole cell: the building, low blocks round it, mostly the fabric; far off, the
                // building alone, more often a shaft (the skyline). The odd dead one anywhere
                bool near = Mathf.Abs(x) < 900f && z > -600f && z < 3200f;
                MenuCity.OuterBlock(far, sheets, foot, w, d, h, bearing, Random.value < 0.05f ? 4 : MenuCity.FabricVariant(Random.value, Random.value, near), lit, near ? 75f : 0f);
            }
        // The house, in the middle of the city it was drawn for: the menu's plan, whole
        HalcyonCity.Raise(root, House, 1.2f, false, life, cityBeacons, cityBeaconPhases);
        CityKit.Skyline(far, new Vector3(0f, 0f, 1000f), 0f, 250, 3600f, 6200f, 125f, 415f, 1.5f);   // everywhere but north: nothing stands in front of the house
        LevelKit.SetCloudTint(new Color(1f, 0.74f, 0.6f));   // lit from the west, peach
        LevelKit.Clouds(root, new Vector3(0f, 0f, 3200f), 2600f, 8000f, 440f, 26);

        if (life == null) return;
        CityKit.Airship(root, life, new Vector3(-900f, 300f, 1200f), new Vector3(900f, 320f, 2600f), 0.005f, 0.2f);
        CityKit.Airship(root, life, new Vector3(700f, 340f, 3000f), new Vector3(-700f, 340f, 800f), 0.004f, 0.7f);
        for (int i = 0; i < 3; i++)
            CityKit.Aircraft(root, life, new Vector3(-1500f + i * 300f, 480f + i * 60f, 600f + i * 500f), new Vector3(1500f, 500f + i * 60f, 2800f - i * 300f), 0.012f + i * 0.003f, i * 0.33f);
        // The response, over the top of the arcology and the spire, and its lights on the deck's piers
        CityKit.Patrol(root, life, FootArc, 120f, ArcH + 60f, 3);
        CityKit.Patrol(root, life, FootS, 70f, SpireH + 50f, 3);
        Vector3 dir = (DeckB - DeckA).normalized;
        CityKit.Searchlight(root, life, DeckA + dir * 100f + Vector3.up * 4f, 420f, 24f, 5f, 40f);
        CityKit.Searchlight(root, life, DeckB - dir * 100f + Vector3.up * 4f, 420f, 24f, 6f, 200f);
    }

    /// THE STREETS BELOW (CityTraffic): the fabric's avenues either side of the corridor with trams up the nearest,
    /// and whatever streets the corridor itself leaves between the towers and the neighbours (probed)
    static void Streets()
    {
        var traffic = CityTraffic.Create(root, false);
        traffic.Fabric(GroundY, 150f, CorridorHalf, RouteZ0, RouteZ1, 12, SurfaceKit.Near, LevelKit.RivetIron);
        traffic.Corridor(GroundY, new[] { -170f, -100f, -40f, 40f, 100f, 170f }, RouteZ0, RouteZ1, 8f, 150f, 120);
        traffic.Finish();
        Debug.Log($"CityTraffic: {traffic.LaneCount} lanes, {traffic.Vehicles} vehicles, {traffic.Walkers} walkers");
    }
}
