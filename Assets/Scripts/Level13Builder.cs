using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LEVEL 13 — CROSSTOWN · THE BLOCKS. Through the spire's door and out onto
/// the next district, two hundred and fifty metres up, the house six
/// kilometres north and closer every roof. Nothing here is a line: the route
/// is a WEB. From every hub three ways go north, each its own kind of
/// ground, each with its own people on it, and they cross and meet and part
/// again; the blocks between them are the city's own, at the route's height,
/// with roofs you can drop onto and fight across and ways back up.
///   THE HALL       the hub you start on: the door behind you, three bridges
///                  ahead — west, north, east.
///   THE GALLERIES  west: two towers split through the middle by a sky lobby
///                  (columns, planters, the lifts that do not come) with the
///                  response inside, a fire escape up the first one's face to
///                  a nest and a drone line.
///   THE SKY LINE   north: the fast way, the high way, the exposed way — a
///                  stack, a blade pair and a leap pad, a long viaduct under
///                  the snipers.
///   THE YARDS      east: a round tower and its halo, a roof garden with
///                  cover in it.
///   THE EXCHANGE   the second hub, a plaza a hundred metres across where
///                  every way lands and the response holds it. Three ways on.
///   THE COURTS     west: two towers with a covered court between them, a
///                  sky lobby after.
///   THE GAP        north: a bridge the response cut, a drone line over it.
///   THE TERRACES   east: two gardens rising, a round tower and its halo.
///   THE JUNCTION   the third hub, a blade pair: every way lands on the low
///                  blade, the pad goes up to the high one.
///   THE LAST LINE  a tram viaduct north to the Terminal, and the Terminal's
///                  top: the last stand, and the door facing the house.
/// The towers, bridges, lobbies and neighbours are RoofKit's; the city round
/// it is the plan's (MenuCity); the house at the end of it is the menu's
/// (HalcyonCity). The street is two hundred metres down and it kills.
/// </summary>
public static partial class Level13Builder
{
    const float GroundY = 0f, DeathY = 10f;
    static readonly Vector3 House = new Vector3(0f, 0f, 6200f);
    const float CorridorHalf = 300f, RouteZ0 = -120f, RouteZ1 = 1900f;

    /// For the back door (DevMenu): the three hubs
    public static readonly Vector3 DevHall = new Vector3(0f, 251.4f, 10f), DevExchange = new Vector3(0f, 281.4f, 680f), DevTerminal = new Vector3(0f, 331.4f, 1752f);

    // The hubs' feet and tops
    static readonly Vector3 FootHall = new Vector3(0f, 0f, 10f), FootExchange = new Vector3(0f, 0f, 680f), FootJunction = new Vector3(0f, 0f, 1400f), FootTerminal = new Vector3(0f, 0f, 1760f);
    const float HallH = 250f, ExchangeH = 280f, JunctionH = 320f, TerminalH = 330f;
    // The junction's blades: every way lands on the low one
    const float LowBlade = JunctionH * 0.86f;   // 275.2
    static readonly Vector3 LowBladeS = new Vector3(0f, LowBlade, 1387.6f), HighBladeN = new Vector3(0f, JunctionH, 1412.4f);
    // The terminal's top tier (64 x 54 shrunk by 0.85 four times: 33.4 x 28.2)
    static readonly Vector3 TerminalS = new Vector3(0f, TerminalH, 1745.9f);
    static readonly float[] NeighbourLanes = { -250f, -210f, -70f, 70f, 210f, 250f };

    static Transform root;
    static MovementConfig cfg;
    static District district;
    static CityLife life;
    static SheetBaker sheets;
    static List<RoofKit.Block> neighbours;
    static readonly List<Renderer> cityBeacons = new List<Renderer>();
    static readonly List<float> cityBeaconPhases = new List<float>();

    /// THE SCORE (2026-10-27): the level is GauntletLevels' line for it; what follows is the old build, kept for reference
    public static Vector3 Build(MovementConfig config) => GauntletLevels.Build(LevelKind.Level13, config);

    public static Vector3 BuildBefore(MovementConfig config)
    {
        LevelKit.Init();
        LevelKit.SetZone(Zone.Surface);
        CityKit.Init(true, 1.6f);   // after dark: every lit floor lit
        cfg = config;
        root = new GameObject("Level13").transform;
        Random.InitState(3013);
        SurfaceKit.Begin(root);
        sheets = new SheetBaker();
        cityBeacons.Clear();
        cityBeaconPhases.Clear();
        MenuCity.Init(1.4f, false, House);   // the city as it was drawn to be seen: at night, lit by design
        MenuCity.ProudGrowth = 0f;   // walked past at arm's length: the windows sit on the walls
        MenuCity.ProudBase = 0.03f;

        var layout = new LevelSections.Layout();
        LevelSections.Last = layout;
        var host = new GameObject("District");
        host.transform.SetParent(root, false);
        district = host.AddComponent<District>();
        district.config = config;
        district.beaconOff = LevelKit.LampOff;
        district.beaconOn = LevelKit.LampOn;
        district.gaugeOn = LevelKit.LampOn;
        RoofKit.Begin(root, cfg, district, sheets, GroundY);
        life = null;
        if (!CityVista.Preview)
        {
            var living = new GameObject("SkyLife");
            living.transform.SetParent(root, false);
            life = living.AddComponent<CityLife>();
        }

        Ground();
        Hall();
        Exchange();
        Junction();
        Terminal();
        Galleries();
        SkyLine();
        Yards();
        Courts();
        Gap();
        Terraces();
        LastLine();
        HallWays();
        ExchangeWays();
        neighbours = RoofKit.Neighbours(RouteHeight, -110f, RouteZ1 + 40f, NeighbourLanes, 100f);
        OffRoute();
        Beyond();
        var routeSheets = sheets.Flush(root, "RouteSheets");
        SurfaceKit.End();
        if (life != null)
        {
            life.SetBeacons(cityBeacons, cityBeaconPhases);
            life.gameObject.AddComponent<WindowTwinkle>().Init(HalcyonCity.Windows(routeSheets), false);   // the route's own lit sheets breathe, as the plan's do
        }

        Physics.SyncTransforms();
        if (!CityVista.Preview)
        {
            Defences();
            Mines();
            Physics.SyncTransforms();
            Units();
            NeighbourUnits();
            Drones();
        }
        // ROOFTOPS ALIVE (2026-10-12): the streets below, then every roof dressed once everything it must keep off stands;
        // the Exchange's party in its south-east corner, between the viaduct's landing and the east port, by hand
        if (life != null) Streets();
        RoofLife.ReservePenthouse(FootExchange + Vector3.up * ExchangeH, 0, new Vector2(44f, -41.15f));
        RoofLife.Dress(root, cfg, district, life, neighbours, true);

        layout.spawn = DevHall;
        layout.menuPortal = new Vector3(0f, HallH + 2f, -10f);
        layout.finishPortal = new Vector3(0f, TerminalH + 1.5f, 1769f);
        layout.length = RouteZ1;
        // COURSE-GRAMMAR (2026-10-11): the lines across the route a respawn starts you from, moving, on the MIDDLE way
        // (CourseFlow reads them by z alone; the beacons on the side ways still take you back to where you passed them).
        // The junction's high blade gets none: its line would be crossed on the low blade under it
        Checkpoint(layout, "SKY LINE 5", 215f, new Vector3(0f, 262f, 224f));
        Checkpoint(layout, "BLADE PAIR 9", 408f, new Vector3(0f, 237.4f, 409f));              // the low blade, three metres short of its pad
        Checkpoint(layout, "THE EXCHANGE", 636f, new Vector3(0f, ExchangeH, 646f));           // past the viaduct's landing, the garden ahead
        Checkpoint(layout, "BRIDGE 9", 845f, new Vector3(0f, 300f, 854f));
        Checkpoint(layout, "THE NORTH STACK", 1104f, new Vector3(0f, 314f, 1113f));
        Checkpoint(layout, "JUNCTION 3", 1388f, new Vector3(0f, LowBlade, 1389f));            // the low blade, three metres short of its pad
        Checkpoint(layout, "SKY LINE 6", 1590f, new Vector3(0f, 325.9f, 1590f));              // the strip, between the trams (deck ~325.4), past the arc
        layout.postcardPos = new Vector3(-190f, 310f, 520f);
        layout.postcardLookAt = new Vector3(20f, 280f, 950f);
        layout.postcardFov = 74f;
        layout.hasPostcard = true;
        layout.postcardSignature = true;
        Random.InitState(System.Environment.TickCount);
        return layout.spawn;
    }

    /// A checkpoint line at `z`; `roofAt` is where the respawn stands, given at the roof's DRAWN height (the kit's Proud
    /// and the player's 1.2 m are added here, as the beacons add them)
    static void Checkpoint(LevelSections.Layout layout, string name, float z, Vector3 roofAt)
        => layout.checkpoints.Add(new LevelSections.CheckpointMark { name = name, z = z, at = roofAt + Vector3.up * (RoofKit.Proud + 1.2f) });

    /// The height of the route at `z`, the middle way of it: the hubs, and the lanes between
    static float RouteHeight(float z)
    {
        float[] kz = { -300f, 60f, 200f, 450f, 640f, 720f, 880f, 1120f, 1250f, 1390f, 1410f, 1760f, 2500f };
        float[] ky = { 250f, 250f, 245f, 262f, 280f, 280f, 292f, 314f, 300f, 275f, 320f, 330f, 330f };
        for (int i = 1; i < kz.Length; i++)
            if (z <= kz[i]) return Mathf.Lerp(ky[i - 1], ky[i], Mathf.InverseLerp(kz[i - 1], kz[i], z));
        return ky[ky.Length - 1];
    }

    // ------------------------------------------------------------ the ground, a long way down

    static void Ground()
    {
        const float Far = 15000f;
        LevelKit.Prim(root, new Vector3(0f, GroundY - 0.6f, 0f), new Vector3(Far * 2f, 1f, Far * 2f), EndingProps.Ash, "FarGround");
        district.melts.Add(new District.Melt { x0 = -700f, x1 = 700f, z0 = -400f, z1 = 2400f, y = DeathY });
    }

    /// Lit lips round a hub's edge: the edge reads, and nothing stands in the way of the bridges
    static void Lips(Vector3 top, float w, float d)
    {
        top += Vector3.up * RoofKit.Proud;   // on the slab, not under it
        SurfaceKit.Look(LevelKit.Led, top + new Vector3(-w * 0.5f + 0.2f, 0.04f, 0f), new Vector3(0.36f, 0.06f, d));
        SurfaceKit.Look(LevelKit.Led, top + new Vector3(w * 0.5f - 0.2f, 0.04f, 0f), new Vector3(0.36f, 0.06f, d));
        SurfaceKit.Look(LevelKit.Led, top + new Vector3(0f, 0.04f, -d * 0.5f + 0.2f), new Vector3(w, 0.06f, 0.36f));
        SurfaceKit.Look(LevelKit.Led, top + new Vector3(0f, 0.04f, d * 0.5f - 0.2f), new Vector3(w, 0.06f, 0.36f));
    }

    // ------------------------------------------------------------ the hubs

    /// Where you start: the top of the tower behind the spire's door, three bridges off it
    static void Hall()
    {
        RoofKit.StackTower(FootHall, 70f, 60f, HallH, 0f);   // top 57.4 x 49.2: x ±28.7, z -14.6 .. 34.6
        Vector3 top = FootHall + Vector3.up * HallH;
        Lips(top, 57.2f, 49f);
        RoofKit.Door(new Vector3(0f, HallH, -12f));   // the door you came through: six metres wide, its posts edge-boost corners
        RoofKit.FastLine(new Vector3(0f, HallH, -8f), new Vector3(0f, HallH, 25f));   // the fast way is straight on: the sky line
        RoofKit.KickPair(0f, HallH, 28f);   // the kick chain into the north bridge's ramp, the marshal between them
        RoofKit.Beacon(new Vector3(0f, HallH, 2f));
        LevelKit.HoloSign(root, new Vector3(0f, HallH + 3.6f, 30f), Vector3.back, 14f, 1.3f, "CROSSTOWN  ·  THREE WAYS NORTH", true);
        LevelKit.HoloSign(root, new Vector3(-22f, HallH + 2.4f, 16f), Vector3.right, 9f, 1f, "GALLERIES  ·  WEST");
        LevelKit.HoloSign(root, new Vector3(22f, HallH + 2.4f, 16f), Vector3.left, 9f, 1f, "THE YARDS  ·  EAST");
        LevelKit.Floodlight(root, new Vector3(-14f, HallH + 6f, -6f), new Vector3(0.5f, -0.6f, 0.6f), 34f, 2.4f);
        LevelKit.Floodlight(root, new Vector3(14f, HallH + 6f, -6f), new Vector3(-0.5f, -0.6f, 0.6f), 34f, 2.4f);
        LevelKit.Milk(root, new Vector3(-16f, HallH, 28f));
    }

    /// The three ways off the hall, laid once the roofs they land on stand (a deck ends at a roof's edge, so the roof must be there)
    static void HallWays()
    {
        RoofKit.Bridge(new Vector3(-28.5f, HallH, 20f), new Vector3(-130f, 240f, 179.3f));    // west: the first gallery's lobby
        RoofKit.Bridge(new Vector3(0f, HallH, 34.4f), new Vector3(0f, 262f, 213.4f));         // north: the sky line
        RoofKit.Bridge(new Vector3(28.5f, HallH, 20f), new Vector3(130f, 245f, 194.8f));      // east: the round tower
    }

    /// The second hub: a plaza a hundred metres across, a garden in the middle of it for cover, the response on it
    static void Exchange()
    {
        RoofKit.StackTower(FootExchange, 150f, 110f, ExchangeH, 0f);   // top 123 x 90.2: x ±61.5, z 634.9 .. 725.1
        Vector3 top = FootExchange + Vector3.up * ExchangeH;
        Lips(top, 122.8f, 90f);
        RoofKit.Garden(top, 60f, 40f, 0f);
        RoofKit.FastLine(new Vector3(-34f, ExchangeH, 648f), new Vector3(-34f, ExchangeH, 714f));   // the fast lines flank the garden, through the wardens
        RoofKit.FastLine(new Vector3(34f, ExchangeH, 648f), new Vector3(34f, ExchangeH, 714f));
        RoofKit.Beacon(new Vector3(-50f, ExchangeH, 650f));
        RoofKit.Beacon(new Vector3(50f, ExchangeH, 710f));
        LevelKit.HoloSign(root, new Vector3(0f, ExchangeH + 4f, 640f), Vector3.back, 16f, 1.4f, "THE EXCHANGE  ·  ALL LINES", true);
        LevelKit.HoloSign(root, new Vector3(-40f, ExchangeH + 2.4f, 722f), Vector3.back, 9f, 1f, "THE COURTS  ·  WEST");
        LevelKit.HoloSign(root, new Vector3(40f, ExchangeH + 2.4f, 722f), Vector3.back, 9f, 1f, "THE TERRACES  ·  EAST");
        LevelKit.HoloSign(root, new Vector3(0f, ExchangeH + 2.4f, 724f), Vector3.back, 9f, 1f, "BRIDGE 9 CUT  ·  DO NOT ATTEMPT", true);
        for (int i = 0; i < 4; i++)
            LevelKit.Floodlight(root, new Vector3(-45f + 30f * i, ExchangeH + 7f, 660f + (i % 2) * 40f), new Vector3(0f, -0.8f, i % 2 == 0 ? 0.6f : -0.6f), 40f, 2.4f);
    }

    /// The three ways on from the exchange, laid once the roofs they land on stand
    static void ExchangeWays()
    {
        RoofKit.Bridge(new Vector3(-61.3f, ExchangeH, 700f), new Vector3(-140f, 292f, 861.8f));   // west: the courts
        RoofKit.Bridge(new Vector3(0f, ExchangeH, 724.9f), new Vector3(0f, 300f, 843.4f));        // north: the gap
        RoofKit.Bridge(new Vector3(61.3f, ExchangeH, 700f), new Vector3(150f, 288f, 858.5f));     // east: the terraces
    }

    /// The third hub: a blade pair. Every way lands on the low blade; the pad goes up to the high one, and the last line
    static void Junction()
    {
        RoofKit.BladeTower(FootJunction, 60f, JunctionH, 0f);   // low blade z 1392 (top 275.2), high blade z 1408 (top 320)
        LevelKit.JumpPad(root, new Vector3(0f, LowBlade, 1392f), 0f, 48f, 4f);   // 46 m up, the high blade
        RoofKit.Beacon(new Vector3(-24f, LowBlade, 1392f));
        RoofKit.Beacon(new Vector3(20f, JunctionH, 1408f));
        LevelKit.HoloSign(root, new Vector3(0f, LowBlade + 3f, 1396.4f), Vector3.back, 10f, 1f, "JUNCTION 3  ·  LEAP PAD LIVE");
        LevelKit.HoloSign(root, new Vector3(0f, JunctionH + 3.2f, 1412f), Vector3.back, 12f, 1.1f, "SKY LINE 6  ·  THE TERMINAL  ·  NORTH");
    }

    /// The finish: the top of the Terminal, the last stand, the door facing the house
    static void Terminal()
    {
        RoofKit.SpireTower(FootTerminal, 64f, 54f, TerminalH, 0.85f, true);   // tier tops 125.4, 204.6, 260.7, 300.3, 330; top 33.4 x 28.2
        RoofKit.Door(new Vector3(0f, TerminalH, 1772f));   // the last door: six metres wide, its posts edge-boost corners
        RoofKit.Beacon(new Vector3(8f, TerminalH, 1750f));
        LevelKit.HoloSign(root, new Vector3(0f, TerminalH + 5.2f, 1773f), Vector3.back, 12f, 1.3f, "HALCYON HOUSE  ·  THE LAST LINE  ·  YOU ARE EXPECTED");
        LevelKit.Floodlight(root, new Vector3(-9f, TerminalH + 6f, 1770f), new Vector3(0.5f, -0.7f, -0.5f), 30f, 2.6f);
        LevelKit.Floodlight(root, new Vector3(9f, TerminalH + 6f, 1770f), new Vector3(-0.5f, -0.7f, -0.5f), 30f, 2.6f);
    }

    // ------------------------------------------------------------ off the route

    /// Where a way off the route could go: from these edges, a bridge to the nearest neighbour's roof that a bridge
    /// can reach. The neighbours are the city's own blocks, so where they happen to stand decides which edges get one
    static readonly Vector3[] Ports =
    {
        new Vector3(-28.5f, HallH, -4f), new Vector3(28.5f, HallH, -4f),
        new Vector3(-18f, 262f, 230f), new Vector3(18f, 262f, 230f),
        new Vector3(-61.3f, ExchangeH, 645f), new Vector3(61.3f, ExchangeH, 645f), new Vector3(-61.3f, ExchangeH, 718f), new Vector3(61.3f, ExchangeH, 718f),
        new Vector3(-18f, 300f, 860f), new Vector3(18f, 300f, 860f), new Vector3(-18.8f, 314f, 1120f), new Vector3(18.8f, 314f, 1120f),
        new Vector3(-160.5f, 292f, 880f), new Vector3(174.6f, 288f, 880f), new Vector3(-160.5f, 300f, 1060f), new Vector3(183f, 302f, 1050f),
        new Vector3(-30f, LowBlade, 1392f), new Vector3(30f, LowBlade, 1392f),
    };

    static void OffRoute()
    {
        foreach (var from in Ports)
        {
            if (!RoofKit.NearestRoof(neighbours, from, 60f, 40f, 8f, out _, out Vector3 top, out float hx, out float hz)) continue;
            Vector3 d = top - from; d.y = 0f;
            if (d.sqrMagnitude < 1f) continue;
            d.Normalize();
            // The bridge ends a little inside the roof's edge along the line it came in on
            Vector3 to = top - new Vector3(d.x * hx * 0.85f, 0f, d.z * hz * 0.85f);
            RoofKit.Bridge(from, to + Vector3.up * 0.02f, 3f);
        }
    }

    // ------------------------------------------------------------ the city round the route

    /// The utopia in every direction, the house and its plan ahead, closer than the roofs left it
    static void Beyond()
    {
        var far = SurfaceKit.Far;
        for (float gx = -2100f; gx <= 2100f; gx += 150f)
            for (float gz = -1500f; gz <= 4200f; gz += 150f)
            {
                float x = gx + Random.Range(-30f, 30f), z = gz + Random.Range(-30f, 30f);
                if (Mathf.Abs(x) < CorridorHalf && z > RouteZ0 && z < RouteZ1) continue;   // the route
                Vector3 foot = new Vector3(x, GroundY, z);
                float w = Random.Range(30f, 60f), d = Random.Range(30f, 55f);
                float k = Mathf.InverseLerp(-1500f, 4200f, z);
                float h = Mathf.Lerp(220f, 360f, k) + Random.Range(-40f, 40f);
                float bearing = Mathf.Atan2(House.x - x, House.z - z) * Mathf.Rad2Deg;
                // Near the route a whole cell, mostly the low fabric; far off the skyline's shafts more often; the odd dead one
                bool near = Mathf.Abs(x) < 900f && z > -600f && z < 2800f;
                MenuCity.OuterBlock(far, sheets, foot, w, d, h, bearing, Random.value < 0.05f ? 4 : MenuCity.FabricVariant(Random.value, Random.value, near), true, near ? 75f : 0f);
            }
        HalcyonCity.Raise(root, House, 1.4f, false, life, cityBeacons, cityBeaconPhases);
        CityKit.Skyline(far, new Vector3(0f, 0f, 900f), 0f, 250, 3400f, 6000f, 125f, 415f, 1.5f);
        LevelKit.SetCloudTint(new Color(0.55f, 0.52f, 0.5f));   // night clouds over the painted slate: warm slate, sepia under (the menu's); the sky's own dark would lose them
        LevelKit.Clouds(root, new Vector3(0f, 0f, 2600f), 2600f, 7000f, 520f, 26);
        LevelKit.Stars(root);

        if (life == null) return;
        CityKit.Airship(root, life, new Vector3(-900f, 380f, 1000f), new Vector3(900f, 400f, 2400f), 0.005f, 0.2f);
        CityKit.Airship(root, life, new Vector3(700f, 420f, 2600f), new Vector3(-700f, 420f, 600f), 0.004f, 0.7f);
        for (int i = 0; i < 3; i++)
            CityKit.Aircraft(root, life, new Vector3(-1500f + i * 300f, 560f + i * 60f, 400f + i * 500f), new Vector3(1500f, 580f + i * 60f, 2600f - i * 300f), 0.012f + i * 0.003f, i * 0.33f);
        CityKit.Patrol(root, life, FootExchange, 110f, ExchangeH + 60f, 3);
        CityKit.Patrol(root, life, FootTerminal, 70f, TerminalH + 50f, 3);
        Vector3 dir = (TerminalS - HighBladeN).normalized;
        CityKit.Searchlight(root, life, HighBladeN + dir * 90f + Vector3.up * 4f, 420f, 24f, 5f, 40f);
        CityKit.Searchlight(root, life, TerminalS - dir * 90f + Vector3.up * 4f, 420f, 24f, 6f, 200f);
    }

    /// THE STREETS BELOW (CityTraffic): the fabric's avenues either side of the corridor with trams up the nearest,
    /// and whatever streets the corridor itself leaves between the towers and the neighbours (probed)
    static void Streets()
    {
        var traffic = CityTraffic.Create(root, false);
        traffic.Fabric(GroundY, 150f, CorridorHalf, RouteZ0, RouteZ1, 13, SurfaceKit.Near, LevelKit.RivetIron);
        traffic.Corridor(GroundY, new[] { -185f, -105f, -45f, 45f, 105f, 185f }, RouteZ0, RouteZ1, 8f, 150f, 130);
        traffic.Finish();
        Debug.Log($"CityTraffic: {traffic.LaneCount} lanes, {traffic.Vehicles} vehicles, {traffic.Walkers} walkers");
    }
}
