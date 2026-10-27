using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LEVEL 11 — THE BREACH · ABOVE THE TERMINUS. Chapter 3 opens where Chapter 2's last frame left you: the terminus's
/// forecourt at dawn, the morning after Lift 3 came up through it with the steam main behind it, and the House nine
/// kilometres north across the city.
///
/// IT IS NOT A CORRIDOR. One district, 560 m by 870, under the House's cordon, with no doors and no rooms in it, and
/// one way out: the roof of Halcyon Railways' house at its north end, eighty-four metres up, where the rooftops to the
/// House begin. The way the railway meant you to get up there is the RELIEF STACK on the house's south face: the burst
/// main vents up it, and the steam lifts anyone who runs into it (course-grammar: nothing on a route is gated). Three
/// RELIEF VALVES on the same main, in any order, each at the end of its own line, shut it in and put the stack on MAIN
/// PRESSURE: the gauge, the governor, the steam, a faster ride. They are the side objective, never the way:
///   THE VIADUCT MAIN      west. Up the fallen span onto the elevated line, where the trains are still running on
///                         schedule: two tracks, one safe strip between them, the valve at the halt at the far end.
///                         Ride a roof there; ride one back and jump at full tilt, and its speed is yours.
///   THE PLATFORM MAIN     the cut in the middle: the metro's cut, collapsed into a long hole with the tunnel's floor
///                         in the bottom of it, the platform's vault still standing in the middle like an island, and
///                         four ways onto it — the torn track bed from the south (paving that gives way under you), the
///                         steam columns from the west (you FLY them), the track bed from the north under a signal
///                         gantry, and the snipers over the east half, stepping stones to anyone who can yank and cut.
///   THE CLOCK TOWER MAIN  east. The railway's clock tower lying on its side across the district, its facade a ramp
///                         two hundred and fifty metres long and its string courses a ladder of lips. Leap pads, or a
///                         stair of floors, take you up the parcels office it fell on to the crown; the way back down is
///                         the slide of your life, and it throws you at the house's door.
/// NOTHING CHECKS WHAT YOU DID. The finish is a door on a roof. The valves are only how the railway meant you to reach
/// it: a locking bar can be cut by an arrow from anywhere with a line to it, the snipers up the house's west face are
/// a ladder to anyone who can ride a yank, and there is an orb on the crane over the roof for whoever arrives with speed.
///
/// AND THE DISTRICT IS ARMED (DistrictDefense). Searchlights that call mortar fire down on whoever stands in them,
/// hover mines on the slow ways, a sentry gun on every perch, patrolmen and hounds on every line — and seventy of the
/// House's drones holding station over all of it, every one of them somewhere to put a line.
/// NOTHING IN IT KILLS: the holes have floors, and pads in the floors.
/// </summary>
public static partial class Level11Builder
{
    // The district
    const float X0 = -280f, X1 = 280f, Z0 = -70f, Z1 = 800f, CordonH = 130f;
    // Lift 3's head: the hole in the forecourt the lift came up through, and the floor of it now (the car's wreckage)
    static readonly Vector3 Head = new Vector3(0f, 0f, 10f);
    const float HeadHalf = 8f, HeadFloor = -14f;
    // The cut: the metro's cut, collapsed — a long hole with the tunnel's floor in the bottom and the platform's vault in the middle
    static readonly Vector3 Cut = new Vector3(-30f, 0f, 380f);
    const float CutHalfX = 70f, CutHalfZ = 85f, CutFloor = -22f, IslandTop = 4f, IslandHalf = 13f;
    // The elevated line
    const float DeckY = 30f, DeckZ = 230f, DeckW = 16f, BreakW = -100f, BreakE = 100f;
    // The railway's clock tower, lying down: the low end of its facade (its foot, by the house) and the high (its crown, on the parcels office)
    static readonly Vector3 TowerLow = new Vector3(186f, 1.5f, 606f), TowerHigh = new Vector3(122f, 68f, 380f);
    const float TowerW = 24f, TowerThick = 20f;
    /// The way the tower lies, on the ground
    static readonly Vector3 Along = Flat(TowerHigh - TowerLow);
    // Halcyon Railways' house, and the way out on top of it
    static readonly Vector3 Goal = new Vector3(0f, 0f, 720f);
    const float GoalHalf = 22f, RoofY = 84f;

    /// For the back door (DevMenu): the four places in the district worth arriving at without the walk
    public static readonly Vector3 DevHalt = new Vector3(-236f, DeckY + 1.4f, DeckZ), DevIsland = new Vector3(Cut.x + 8f, IslandTop + 1.4f, Cut.z - 6f),
        DevCrown = TowerHigh + Along * 5f + Vector3.up * 1.4f, DevPlaza = new Vector3(10f, 1.4f, 622f);

    static Transform root;
    static MovementConfig cfg;
    static District district;
    static CityLife life;
    static readonly List<Renderer> cityBeacons = new List<Renderer>();
    static readonly List<float> cityBeaconPhases = new List<float>();

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v.normalized; }

    /// THE SCORE (2026-10-27): the level is GauntletLevels' line for it; what follows is the old build, kept for reference
    public static Vector3 Build(MovementConfig config) => GauntletLevels.Build(LevelKind.Level11, config);

    public static Vector3 BuildBefore(MovementConfig config)
    {
        LevelKit.Init();
        LevelKit.SetZone(Zone.Surface);
        CityKit.Init(false, 1.4f);
        cfg = config;
        root = new GameObject("Level11").transform;
        Random.InitState(3011);
        SurfaceKit.Begin(root);
        cityBeacons.Clear();
        cityBeaconPhases.Clear();

        var layout = new LevelSections.Layout();
        LevelSections.Last = layout;
        var host = new GameObject("District");
        host.transform.SetParent(root, false);
        district = host.AddComponent<District>();
        district.config = config;
        district.beaconOff = LevelKit.LampOff;
        district.beaconOn = LevelKit.LampOn;
        district.gaugeOn = LevelKit.LampOn;
        life = null;
        if (!CityVista.Preview)
        {
            var living = new GameObject("SurfaceLife");
            living.transform.SetParent(root, false);
            life = living.AddComponent<CityLife>();
        }

        Ground();
        LevelKit.Cordon(root, X0, X1, Z0, Z1, CordonH, 7f, RoofY - 2f, RoofY + 10f);
        Forecourt();
        Street();
        Viaduct();
        TheCut();
        ClockTower();
        TerminusHouse();
        Mains();
        Defences();
        if (!CityVista.Preview) Beyond();
        SurfaceKit.End();
        district.ash = LevelKit.AshFall(root);
        if (life != null) life.SetBeacons(cityBeacons, cityBeaconPhases);

        Physics.SyncTransforms();
        if (!CityVista.Preview)
        {
            // A postcard is the district's shape: nothing that thinks, hovers or blows up is built for one
            Mines();
            Physics.SyncTransforms();   // the units settle round the mines, not into them
            Units();
            Drones();
        }

        layout.spawn = new Vector3(-3.5f, 1.2f, -14f);
        layout.menuPortal = new Vector3(0f, 2f, -29f);
        layout.finishPortal = new Vector3(0f, RoofY + 2f, 770f);
        layout.length = Z1;
        // COURSE-GRAMMAR: the checkpoints along the main line — the forecourt's gate, the street under the viaduct's break,
        // the cut's south-east corner, the east rim, the mains, the plaza — read by CourseFlow by z; a respawn stands here
        // and runs north. The beacons on the lines (the halt, the island, the crown, the roof) still take you back to where
        // you passed them. None on the house's roof: its line could be crossed on the ground round the house
        Cordon(layout, "THE FORECOURT", 42f, new Vector3(2f, 0f, 46f));
        Cordon(layout, "THE VIADUCT", 248f, new Vector3(0f, 0f, 252f));
        Cordon(layout, "THE CUT", 290f, new Vector3(70f, 0f, 294f));
        Cordon(layout, "THE EAST RIM", 336f, new Vector3(92f, 0f, 340f));
        Cordon(layout, "THE MAINS", 500f, new Vector3(8f, 0f, 504f));
        Cordon(layout, "THE SHAFT", 641f, new Vector3(0f, 0f, 645f));
        layout.postcardPos = new Vector3(-70f, 40f, 40f);
        layout.postcardLookAt = new Vector3(30f, 26f, 420f);
        layout.postcardFov = 76f;
        layout.hasPostcard = true;
        layout.postcardSignature = true;
        Random.InitState(System.Environment.TickCount);
        return layout.spawn;
    }

    /// A checkpoint line across the district at `z`; `groundAt` where the respawn stands, on the ground (the player's
    /// 1.2 m added here). A brass inlay across the lane marks it (decor)
    static void Cordon(LevelSections.Layout layout, string name, float z, Vector3 groundAt)
    {
        layout.checkpoints.Add(new LevelSections.CheckpointMark { name = name, z = z, at = groundAt + Vector3.up * 1.2f });
        LevelKit.BrassStrip(root, new Vector3(groundAt.x - 7f, groundAt.y, z), new Vector3(groundAt.x + 7f, groundAt.y, z), 0.2f);
    }

    // ------------------------------------------------------------ the ground, and the holes in it

    static void Ground()
    {
        float hz0 = Head.z - HeadHalf, hz1 = Head.z + HeadHalf, cz0 = Cut.z - CutHalfZ, cz1 = Cut.z + CutHalfZ;
        Sheet(X0, X1, Z0, hz0);
        Sheet(X0, Head.x - HeadHalf, hz0, hz1);
        Sheet(Head.x + HeadHalf, X1, hz0, hz1);
        Sheet(X0, X1, hz1, cz0);
        Sheet(X0, Cut.x - CutHalfX, cz0, cz1);
        Sheet(Cut.x + CutHalfX, X1, cz0, cz1);
        Sheet(X0, X1, cz1, Z1);
        // Past the cordon: the city's streets to the end of the world, in four sheets round the district so the holes stay holes
        const float Far = 15000f;
        FarSheet(-Far, X0 - 3f, -Far, Far);
        FarSheet(X1 + 3f, Far, -Far, Far);
        FarSheet(X0 - 3f, X1 + 3f, -Far, Z0 - 3f);
        FarSheet(X0 - 3f, X1 + 3f, Z1 + 3f, Far);
    }

    static void Sheet(float x0, float x1, float z0, float z1)
    {
        if (x1 - x0 < 0.05f || z1 - z0 < 0.05f) return;
        LevelKit.FloorSlab(root, new Vector3((x0 + x1) * 0.5f, -0.5f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, 1f, z1 - z0), "Ground");
    }

    static void FarSheet(float x0, float x1, float z0, float z1)
        => LevelKit.Prim(root, new Vector3((x0 + x1) * 0.5f, -0.6f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, 1f, z1 - z0), EndingProps.Concrete, "FarGround");

    /// A rectangular hole's insides: four walls of rock from the floor to the ground, the top eight metres of each faced
    /// in the station's cream tile with its oxblood band and brass rail (the cut WAS a station), the corners taken off
    /// (`cut`: a plate at ground level over a wedge of rock, so the hole reads as a collapse, not a box), the FLOOR —
    /// a floor, never a melt — and its lamps. What stands on the floor is the caller's.
    static void Pit(Vector3 c, float halfX, float halfZ, float floorY, float cut)
    {
        const float top = -1f, tileH = 8f;
        float depth = top - (floorY - 3f), wallY = (top + floorY - 3f) * 0.5f, spanX = halfX * 2f + 8f, spanZ = halfZ * 2f + 8f;
        SurfaceKit.Block(LevelKit.Rock, new Vector3(c.x - halfX - 2f, wallY, c.z), new Vector3(4f, depth, spanZ));
        SurfaceKit.Block(LevelKit.Rock, new Vector3(c.x + halfX + 2f, wallY, c.z), new Vector3(4f, depth, spanZ));
        SurfaceKit.Block(LevelKit.Rock, new Vector3(c.x, wallY, c.z - halfZ - 2f), new Vector3(spanX, depth, 4f));
        SurfaceKit.Block(LevelKit.Rock, new Vector3(c.x, wallY, c.z + halfZ + 2f), new Vector3(spanX, depth, 4f));
        // The tile, proud of the rock, the band proud of the tile, the rail proud of the band: never two faces on one plane
        float ty = top - tileH * 0.5f, lenZ = halfZ * 2f - cut * 2f, lenX = halfX * 2f - cut * 2f;
        Quaternion railZ = Quaternion.Euler(90f, 0f, 0f), railX = Quaternion.Euler(0f, 0f, 90f);
        for (int s = -1; s <= 1; s += 2)
        {
            float wx = c.x + s * (halfX - 0.07f), wz = c.z + s * (halfZ - 0.07f);
            SurfaceKit.Look(LevelKit.Tile, new Vector3(wx, ty, c.z), new Vector3(0.14f, tileH, lenZ));
            SurfaceKit.Look(LevelKit.OxbloodTile, new Vector3(wx - s * 0.09f, top - 2.2f, c.z), new Vector3(0.04f, 0.36f, lenZ));
            SurfaceKit.Look(LevelKit.Brass, new Vector3(wx - s * 0.16f, top - 1.98f, c.z), new Vector3(0.07f, lenZ * 0.5f, 0.07f), railZ, PrimitiveType.Cylinder);
            SurfaceKit.Look(LevelKit.Tile, new Vector3(c.x, ty, wz), new Vector3(lenX, tileH, 0.14f));
            SurfaceKit.Look(LevelKit.OxbloodTile, new Vector3(c.x, top - 2.2f, wz - s * 0.09f), new Vector3(lenX, 0.36f, 0.04f));
            SurfaceKit.Look(LevelKit.Brass, new Vector3(c.x, top - 1.98f, wz - s * 0.16f), new Vector3(0.07f, lenX * 0.5f, 0.07f), railX, PrimitiveType.Cylinder);
        }
        if (cut > 0f)
        {
            float hyp = cut * Mathf.Sqrt(2f) + 12f, deep = cut * 0.9f;
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    Vector3 corner = new Vector3(c.x + sx * halfX, 0f, c.z + sz * halfZ);
                    Vector3 mid = corner - new Vector3(sx, 0f, sz) * (cut * 0.5f) + new Vector3(sx, 0f, sz).normalized * (deep * 0.5f);
                    Quaternion rot = Quaternion.Euler(0f, sx == sz ? 45f : -45f, 0f);
                    var plate = LevelKit.FloorSlab(root, new Vector3(mid.x, -0.54f, mid.z), new Vector3(hyp, 1f, deep), "RimPlate");
                    plate.transform.rotation = rot;
                    SurfaceKit.Block(LevelKit.Rock, new Vector3(mid.x, wallY - 0.05f, mid.z), new Vector3(hyp, depth, deep), rot);
                }
        }
        LevelKit.FloorSlab(root, new Vector3(c.x, floorY - 0.5f, c.z), new Vector3(spanX, 1f, spanZ), "CutFloor");
        for (int i = 0; i < 4; i++)
        {
            Vector3 at = c + new Vector3((i % 2 == 0 ? -1f : 1f) * halfX * 0.5f, 0f, (i < 2 ? -1f : 1f) * halfZ * 0.5f);
            var l = LevelKit.Light(root, new Vector3(at.x, floorY + 9f, at.z), 60f, 2.4f);
            l.color = new Color(1f, 0.9f, 0.72f);
        }
    }

    // ------------------------------------------------------------ small helpers every piece uses

    /// Paving: a limestone plate a hair proud of the ground (a look)
    static void Plate(float x0, float x1, float z0, float z1)
        => SurfaceKit.Look(LevelKit.Limestone, new Vector3((x0 + x1) * 0.5f, 0.015f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, 0.03f, z1 - z0));

    /// A low limestone wall under a brass cap (solid)
    static void Wall(Vector3 foot, Vector3 size)
    {
        SurfaceKit.Block(LevelKit.Limestone, foot + Vector3.up * (size.y * 0.5f), size);
        SurfaceKit.Look(LevelKit.Brass, foot + Vector3.up * (size.y + 0.1f), new Vector3(size.x + 0.2f, 0.2f, size.z + 0.2f));
    }

    static void Beacon(Vector3 groundPos) => district.beacons.Add(LevelKit.ResponseBeacon(root, groundPos));

    /// A collider along a line: for the kit's props that are only a look, laid where you can walk into them
    static void SolidBetween(Vector3 a, Vector3 b, float w, float h)
        => SurfaceKit.Solid((a + b) * 0.5f, new Vector3(w, h, Vector3.Distance(a, b)), Quaternion.LookRotation((b - a).normalized, Vector3.up));

    /// What the rubble is made of: paving, the station's tile, the mains' iron, soot
    static Material Rubble(int i) => (i % 5) switch { 0 => LevelKit.Tile, 1 => LevelKit.RivetIron, 2 => LevelKit.Charcoal, 3 => LevelKit.OxbloodTile, _ => LevelKit.Limestone };

    /// A heap of somebody's floors and the street's paving: boxes at every angle, all of them solid
    static void Heap(Vector3 at, float radius, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float s = Random.Range(1.6f, 5f);
            Vector3 size = new Vector3(s * Random.Range(0.7f, 1.7f), s * Random.Range(0.35f, 0.8f), s * Random.Range(0.7f, 1.7f));
            Vector3 p = at + new Vector3(Random.Range(-radius, radius), size.y * 0.5f - 0.08f, Random.Range(-radius, radius));
            SurfaceKit.Block(Rubble(i + Mathf.RoundToInt(at.x)), p, size, Quaternion.Euler(Random.Range(-24f, 24f), Random.Range(0f, 360f), Random.Range(-24f, 24f)));
        }
    }

    /// What is left of a block: a stump with its top broken, solid; a limestone cornice round its foot
    static void Stub(Vector3 at, Vector3 size)
    {
        SurfaceKit.Block(CityKit.Hollow, at + Vector3.up * (size.y * 0.5f), size);
        SurfaceKit.Look(LevelKit.Limestone, at + Vector3.up * 3.9f, new Vector3(size.x + 0.4f, 0.4f, size.z + 0.4f));
        for (int i = 0; i < 4; i++)
        {
            float s = Random.Range(3f, 7f);
            Vector3 p = at + new Vector3(Random.Range(-0.4f, 0.4f) * size.x, size.y + s * 0.25f, Random.Range(-0.4f, 0.4f) * size.z);
            SurfaceKit.Block(CityKit.Hollow, p, new Vector3(s * Random.Range(0.8f, 1.6f), s * 0.5f, s * Random.Range(0.8f, 1.6f)), Quaternion.Euler(Random.Range(-14f, 14f), Random.Range(0f, 360f), Random.Range(-14f, 14f)));
        }
        Heap(at + new Vector3(size.x * 0.6f, 0f, -size.z * 0.5f), Mathf.Max(size.x, size.z) * 0.35f, 5);
    }

    /// A railway carriage, cream over oxblood with a brass waist, dark glass, a headlamp at each end: welded, solid.
    /// `rot` turns it any way it fell; its long axis is z, its floor the origin's y − 1.3
    static void Carriage(Vector3 centre, Quaternion rot)
    {
        Vector3 L(float x, float y, float z) => centre + rot * new Vector3(x, y, z);
        SurfaceKit.Block(LevelKit.Cream, centre, new Vector3(3f, 2.6f, 17f), rot);
        SurfaceKit.Look(LevelKit.Oxblood, L(0f, -0.75f, 0f), new Vector3(3.06f, 0.9f, 17.06f), rot);
        SurfaceKit.Look(LevelKit.Brass, L(0f, -0.28f, 0f), new Vector3(3.08f, 0.06f, 17.08f), rot);
        SurfaceKit.Look(LevelKit.Charcoal, L(0f, 1.36f, 0f), new Vector3(2.6f, 0.14f, 16.6f), rot);
        SurfaceKit.Look(LevelKit.RivetIron, L(0f, -1.5f, 0f), new Vector3(2.2f, 0.4f, 15.6f), rot);
        for (int s = -1; s <= 1; s += 2)
        {
            SurfaceKit.Look(LevelKit.Dark, L(s * 1.52f, 0.45f, 0f), new Vector3(0.04f, 0.8f, 14.6f), rot);
            SurfaceKit.Look(LevelKit.LampCream, L(s * 0.9f, 0.2f, 8.52f), new Vector3(0.3f, 0.3f, 0.04f), rot);
            SurfaceKit.Look(LevelKit.SignalRed, L(s * 0.9f, 0.2f, -8.52f), new Vector3(0.3f, 0.3f, 0.04f), rot);
        }
    }
}
