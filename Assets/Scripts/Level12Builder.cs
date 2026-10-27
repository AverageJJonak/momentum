using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LEVEL 12 — SKYLINE · THE ROOFTOPS. Chapter 3's second level begins where the
/// first one's bridge ends: on the roof of the next tower, ninety metres over
/// the city, with the house eight kilometres north and nothing between here and
/// there but the roofs of Halcyon's utopia, each within reach of the next and
/// rising the whole way. No cordon, no doors, no ground: the street is a
/// hundred metres down and it kills you (the beacon you passed last brings you
/// back).
/// Nothing here is a line: from the gate roof three ways go north and meet
/// on the arcology's terrace; from its top three ways go on and meet on the
/// spire. The blocks between them are the city's own, at the route's height,
/// with roofs you can drop onto and fight across and ways back up.
///   THE GATE ROOF  the hub you start on: three bridges ahead.
///   THE STACKS     west: four plain towers rising, the bridges between them
///                  cut where the response cut them, a fallen storey to climb.
///   THE CUT LINE   north: the fast way, a blade pair and a leap pad, then a
///                  bridge with thirty metres gone and the drones over the hole.
///   THE SLOT       east: three twin-blade towers, a leap pad from every low
///                  blade to its high one, a rotunda and its halo.
///   THE ARCOLOGY   the second hub: a terraced garden mountain, the thermals
///                  up its west flank, the pads up its east, the viewing
///                  terrace at the top and three ways on from it.
///   THE HALO       west: two round towers wearing their halo decks, a thermal
///                  up the second, a stack on the way down.
///   THE LINE       north: a tram on a viaduct in the sky, seven hundred
///                  metres of it, a station under it halfway to drop to.
///   THE GARDENS    east: a roof garden, a tower split by a sky lobby, a
///                  rotunda and its halo.
///   THE SPIRE      the finish: every way converges on the top tier of a
///                  spire, the response's last stand on it, the door facing
///                  the house.
/// The city round the route is the utopia's own (MenuCity.OuterTower), the
/// house and its plan at the end of it (HalcyonCity), the district you left
/// behind you in the south. Everything you stand on is a kit slab or a bare
/// collider under the plan's textured look (the towers are looks with solids
/// inside them); everything else is welded. The towers, bridges and district
/// things are RoofKit's, shared with the levels after this one.
/// </summary>
public static partial class Level12Builder
{
    // Where the street is, and where it kills: the level lives in the air
    const float GroundY = 0f, DeathY = 10f;
    // The house, and the corridor the route runs up (nothing of the outer city stands in it)
    static readonly Vector3 House = new Vector3(0f, 0f, 8000f);
    const float CorridorHalf = 300f, RouteZ0 = -120f, RouteZ1 = 2250f;

    /// For the back door (DevMenu): three places on the route worth arriving at without the climb
    public static readonly Vector3 DevGate = new Vector3(0f, 91.4f, 12f), DevArcology = new Vector3(0f, 151.4f, 842f), DevSpire = new Vector3(0f, 251.4f, 1796f);

    static Transform root;
    static MovementConfig cfg;
    static District district;
    static CityLife life;
    static SheetBaker sheets;
    static readonly List<Renderer> cityBeacons = new List<Renderer>();
    static readonly List<float> cityBeaconPhases = new List<float>();

    /// THE SCORE (2026-10-27): the level is GauntletLevels' line for it; what follows is the old build, kept for reference
    public static Vector3 Build(MovementConfig config) => GauntletLevels.Build(LevelKind.Level12, config);

    public static Vector3 BuildBefore(MovementConfig config)
    {
        LevelKit.Init();
        LevelKit.SetZone(Zone.Surface);
        CityKit.Init(false, 1.4f);   // the day going: the lit floors coming up
        cfg = config;
        root = new GameObject("Level12").transform;
        Random.InitState(3012);
        SurfaceKit.Begin(root);
        sheets = new SheetBaker();
        cityBeacons.Clear();
        cityBeaconPhases.Clear();
        MenuCity.Init(1.2f, false, House);   // the utopia's own towers, for the route and for everything round it, their light coming on for the evening
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
        GateRoof();
        Arcology();
        Spire();
        Stacks();
        CutLine();
        Slot();
        Halo();
        Line();
        Gardens();
        GateWays();
        ArcologyWays();
        Neighbours();   // the city within a jump of the route (Level12Neighbours)
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
        // ROOFTOPS ALIVE (2026-10-12): the streets below, then every roof dressed once everything it must keep off stands
        if (life != null) Streets();
        RoofLife.Dress(root, cfg, district, life, neighbours, false);

        layout.spawn = DevGate;
        layout.menuPortal = new Vector3(0f, 92f, -22f);
        layout.finishPortal = new Vector3(0f, 252f, 1808f);
        layout.length = RouteZ1;
        // COURSE-GRAMMAR (2026-10-11): the lines across the route a respawn starts you from, moving, on the MIDDLE way
        // (CourseFlow reads them by z alone; the beacons on the side ways still take you back to where you passed them).
        // None sits past a climb you could cross the line under: the arcology's top and the spire get no line of their own
        Checkpoint(layout, "LINE 3", 248f, new Vector3(0f, BladeC1 * 0.86f, 249f));           // the low blade, three metres short of its pad
        Checkpoint(layout, "THE THIRD STACK", 505f, new Vector3(0f, RoofC3, 514f));
        Checkpoint(layout, "THE FOURTH STACK", 685f, new Vector3(0f, RoofC4, 694f));
        Checkpoint(layout, "TERRACE 2", 838f, new Vector3(72.5f, 150f, 850f));                // the east ledge, the pads ahead
        Checkpoint(layout, "THE SKYWAY", 930f, new Vector3(0f, 296.3f, 930f));                // the bridge off the viewing terrace (its deck ~295.6 here)
        Checkpoint(layout, "SKY LINE 4", 1010f, new Vector3(0f, 285.3f, 1010f));              // the strip, between the trams (deck ~284.7)
        Checkpoint(layout, "HALT 4", 1340f, new Vector3(0f, 274.3f, 1340f));                  // over the station (deck ~273.7)
        Checkpoint(layout, "THE SPIRE", 1660f, new Vector3(0f, 263.6f, 1660f));               // the deck's last stretch (deck ~263.05)
        layout.postcardPos = new Vector3(-150f, 215f, 560f);
        layout.postcardLookAt = new Vector3(0f, 190f, 1150f);
        layout.postcardFov = 74f;
        layout.hasPostcard = true;
        layout.postcardSignature = true;
        Random.InitState(System.Environment.TickCount);
        return layout.spawn;
    }

    // ------------------------------------------------------------ the ground, a long way down

    static void Ground()
    {
        // Ash to the end of the world; the house's own plan lays its stone over the far end of it
        const float Far = 15000f;
        LevelKit.Prim(root, new Vector3(0f, GroundY - 0.6f, 0f), new Vector3(Far * 2f, 1f, Far * 2f), EndingProps.Ash, "FarGround");
        // The fall: below the roofs there is nothing to land on that counts. The beacon you passed brings you back
        district.melts.Add(new District.Melt { x0 = -600f, x1 = 600f, z0 = -400f, z1 = 2600f, y = DeathY });
    }

    /// A checkpoint line at `z`; `roofAt` is where the respawn stands, given at the roof's DRAWN height (the kit's Proud
    /// and the player's 1.2 m are added here, as the beacons add them)
    static void Checkpoint(LevelSections.Layout layout, string name, float z, Vector3 roofAt)
        => layout.checkpoints.Add(new LevelSections.CheckpointMark { name = name, z = z, at = roofAt + Vector3.up * (RoofKit.Proud + 1.2f) });

    // ------------------------------------------------------------ the kit, by its short names

    static float StackTower(Vector3 foot, float w, float d, float h, float az, bool lit = true) => RoofKit.StackTower(foot, w, d, h, az, lit);
    static void BladeTower(Vector3 foot, float w, float h, float az) => RoofKit.BladeTower(foot, w, h, az);
    static void RoundTower(Vector3 foot, float d, float h) => RoofKit.RoundTower(foot, d, h);
    static float ArcologyTower(Vector3 foot, float w, float d, float h, float az) => RoofKit.ArcologyTower(foot, w, d, h, az);
    static void Furnish() => RoofKit.Furnish();
    static GameObject Roof(Vector3 top, float w, float d, float az) => RoofKit.Roof(top, w, d, az);
    static void Parapet(Vector3 top, float w, float d, float az, float gapNorth = 0f) => RoofKit.Parapet(top, w, d, az, gapNorth);
    static void Bridge(Vector3 a, Vector3 b, float w = 6f) => RoofKit.Bridge(a, b, w);
    static void BrokenBridge(Vector3 a, Vector3 b, float gap, float w = 6f) => RoofKit.BrokenBridge(a, b, gap, w);
    static void Viaduct(Vector3 a, Vector3 b, float w) => RoofKit.Viaduct(a, b, w);
    static void Mast(Vector3 foot, float height, Vector3 arm, Vector3 centre, float reachX, float reachZ, float rate, float phase) => RoofKit.Mast(foot, height, arm, centre, reachX, reachZ, rate, phase);
    static void Beacon(Vector3 at) => RoofKit.Beacon(at);
    static void Field(float fromY, params float[] xz) => RoofKit.Field(fromY, xz);
    static void Thermal(Vector3 centre, float half, float y0, float y1) => RoofKit.Thermal(centre, half, y0, y1);
}
