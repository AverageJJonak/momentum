using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE END OF CHAPTER 2 (act2-underground). Two places built for it:
///   THE LIFT LANDING — the last room of the terminus, past the Stationmaster's gates: the brass lift car at the foot
///   of a tiled shaft that goes seventy metres straight up to the street, a depth dial on its wall, and beside it the
///   steam mains' RELIEF VALVE the railway's own rules forbid you to touch. Walk up to the post and the ending plays
///   (MineBlast). Nothing follows it: there is no finish door — the cutscene ends the level.
///   THE SURFACE — the terminus's forecourt behind the House, built eight kilometres off to the east where the level
///   cannot see it: the lift head under its canopy, the clock tower, the kiosk, the company's plaza, and the House
///   itself NINE KILOMETRES north with the dawn coming up behind it, the whole city laid out between — the ground
///   Chapter 3 has to cross. The steam main bursts through the street here; the last shots are here; Chapter 3 starts here.
/// </summary>
public static partial class LevelSections
{
    public static Cursor LiftLanding(Transform root, MovementConfig config, Cursor c, float len = 26f, float halfW = 14f, float height = 14f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY, top = y + height;
        const float wellHalf = 4f, wellRise = 70f;
        float wellZ = z1 - 6f;
        Vector3 wellCentre = new Vector3(0f, y, wellZ);
        Seal(root, c, halfW, y, height);
        // No ceiling: it is laid around the shaft's opening
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, top, z1), NoSouth & ~LevelKit.Faces.Ceiling);
        LandGlass(root, c);
        LevelKit.Life(root);
        LevelKit.FloorSlab(root, new Vector3(0f, top + 0.5f, (z0 + wellZ - wellHalf) * 0.5f), new Vector3(halfW * 2f + 1f, 1f, wellZ - wellHalf - z0), "CeilingS");
        LevelKit.FloorSlab(root, new Vector3(0f, top + 0.5f, (wellZ + wellHalf + z1) * 0.5f), new Vector3(halfW * 2f + 1f, 1f, z1 - wellZ - wellHalf), "CeilingN");
        LevelKit.FloorSlab(root, new Vector3(-(halfW + wellHalf) * 0.5f, top + 0.5f, wellZ), new Vector3(halfW - wellHalf, 1f, wellHalf * 2f), "CeilingW");
        LevelKit.FloorSlab(root, new Vector3((halfW + wellHalf) * 0.5f, top + 0.5f, wellZ), new Vector3(halfW - wellHalf, 1f, wellHalf * 2f), "CeilingE");

        // The shaft: glazed tile up all four sides, brass guide rails in the corners, a globe every four metres and a
        // lit ring every eight, the depth in mosaic, a cap
        float wellTop = top + wellRise, wellMid = (top + wellTop) * 0.5f;
        LevelKit.Slab(root, new Vector3(-wellHalf - T * 0.5f, wellMid, wellZ), new Vector3(T, wellTop - top, wellHalf * 2f + 2f * T), LevelKit.Wall, "ShaftW");
        LevelKit.Slab(root, new Vector3(wellHalf + T * 0.5f, wellMid, wellZ), new Vector3(T, wellTop - top, wellHalf * 2f + 2f * T), LevelKit.Wall, "ShaftE");
        LevelKit.Slab(root, new Vector3(0f, wellMid, wellZ - wellHalf - T * 0.5f), new Vector3(wellHalf * 2f, wellTop - top, T), LevelKit.Wall, "ShaftS");
        LevelKit.Slab(root, new Vector3(0f, wellMid, wellZ + wellHalf + T * 0.5f), new Vector3(wellHalf * 2f, wellTop - top, T), LevelKit.Wall, "ShaftN");
        LevelKit.Slab(root, new Vector3(0f, wellTop + 0.5f, wellZ), new Vector3(wellHalf * 2f + 2f, 1f, wellHalf * 2f + 2f), LevelKit.Charcoal, "WellCap");
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                LevelKit.Prim(root, new Vector3(sx * (wellHalf - 0.25f), (y + wellTop) * 0.5f, wellZ + sz * (wellHalf - 0.25f)), new Vector3(0.22f, wellTop - y, 0.22f), LevelKit.Brass, "GuideRail");
        for (float ry = top + 4f; ry < wellTop - 2f; ry += 8f)
        {
            LevelKit.ShaftRing(root, new Vector3(0f, ry, wellZ), wellHalf - 0.12f, wellHalf - 0.12f);
            int s = (int)((ry - top) / 8f) % 2 == 0 ? -1 : 1;
            LevelKit.WallGlobe(root, new Vector3(s * wellHalf, ry + 4f, wellZ), Vector3.left * s);
            if (s < 0) LevelKit.Mosaic(root, new Vector3(wellHalf, ry + 1.4f, wellZ), Vector3.left, Mathf.RoundToInt(wellTop - ry) + " M TO GO", 2.6f, 0.8f);
        }
        LevelKit.Text(root, new Vector3(0f, top + 2f, wellZ + wellHalf - 0.1f), Quaternion.LookRotation(Vector3.forward), "STREET LEVEL  ·  70 M", 0.12f, LevelKit.SignCream, "WellText", 6f, true, 160f, 280f);

        // The lift car on its sill at the foot of the shaft, the relief valve beside it, the dials, the words on the walls
        LevelKit.Prim(root, wellCentre + Vector3.up * 0.03f, new Vector3(3.6f, 0.06f, 3.6f), LevelKit.Brass, "LiftSill");
        LevelKit.Stripes(root, wellCentre + new Vector3(0f, 0.05f, -wellHalf - 0.3f), new Vector3(wellHalf * 2f + 1f, 0.08f, 0.24f));
        LevelKit.Stripes(root, wellCentre + new Vector3(-wellHalf - 0.3f, 0.05f, 0f), new Vector3(0.24f, 0.08f, wellHalf * 2f + 1f));
        LevelKit.Stripes(root, wellCentre + new Vector3(wellHalf + 0.3f, 0.05f, 0f), new Vector3(0.24f, 0.08f, wellHalf * 2f + 1f));
        LevelKit.Chevrons(root, new Vector3(0f, y, z0 + 4f), Vector3.forward, 3);
        var cage = EndingProps.Cage(root, wellCentre, 180f);
        var post = EndingProps.OverridePost(root, new Vector3(6f, y, z1 - 12f), Vector3.back);
        LevelKit.DepthDial(root, new Vector3(wellHalf - 0.1f, top + 3.2f, wellZ), Vector3.left, wellTop, y, cage, 1.6f);
        LevelKit.DepthDial(root, new Vector3(halfW - 0.1f, y + 3.4f, z1 - 8f), Vector3.left, wellTop, y, cage, 1.4f);
        LevelKit.Stencil(root, new Vector3(-halfW + 0.1f, y + 2.6f, z0 + 10f), Vector3.right, "LIFT 3  ·  STREET LEVEL", 0.1f, 8f);
        LevelKit.Stencil(root, new Vector3(halfW - 0.1f, y + 2.6f, z0 + 10f), Vector3.left, "STEAM MAINS  ·  RELIEF  ·  DO NOT TOUCH", 0.1f, 8f);
        LevelKit.Mosaic(root, new Vector3(-halfW, y + 2.75f, z1 - 5f), Vector3.right, "LIFT 3  ·  TO THE STREET", 6f, 1.1f);
        LevelKit.PipeGallery(root, new Vector3(halfW - 0.2f, y + 6f, z0 + 2f), new Vector3(halfW - 0.2f, y + 6f, z1 - 2f), Vector3.left, 3);
        LevelKit.Gauge(root, new Vector3(halfW - 0.1f, y + 3f, z1 - 5.5f), Vector3.left);
        LevelKit.Gauge(root, new Vector3(halfW - 0.1f, y + 3f, z1 - 4f), Vector3.left);
        LevelKit.GlobeCage(root, new Vector3(-halfW * 0.5f, y + 7f, z0 + 6f), top);
        LevelKit.GlobeCage(root, new Vector3(halfW * 0.5f, y + 7f, z1 - 4f), top);
        LevelKit.Floodlight(root, new Vector3(-halfW + 1f, top - 2f, z1 - 4f), new Vector3(0.6f, -0.7f, -0.3f), 26f, 2.4f);
        for (int s = -1; s <= 1; s += 2) LevelKit.Prim(root, new Vector3(s * (halfW - 1.4f), y + 1f, z0 + 3f), Vector3.one * 0.4f, LevelKit.SignalRed, "LandingLamp", PrimitiveType.Sphere);
        denseSkipRoom = true;   // the ending plays here: nothing in the way of its camera
        Flavour(UndergroundFlavour.Station, "TERMINUS");
        Dress(root, c, halfW, y, height, z0, z1);

        // The alarms: three in the bay, four over the furnaces next door — dark until the override
        var alarms = new List<EndingProps.Alarm>
        {
            EndingProps.AlarmLamp(root, new Vector3(-halfW + 0.05f, y + 5f, z0 + 8f), Vector3.right),
            EndingProps.AlarmLamp(root, new Vector3(halfW - 0.05f, y + 5f, z0 + 16f), Vector3.left),
            EndingProps.AlarmLamp(root, new Vector3(0f, y + 6f, z1 - 0.05f), Vector3.back),
            EndingProps.AlarmLamp(root, new Vector3(-16f, y + 12f, z0 - 0.4f), Vector3.back),
            EndingProps.AlarmLamp(root, new Vector3(16f, y + 12f, z0 - 0.4f), Vector3.back),
            EndingProps.AlarmLamp(root, new Vector3(-29.9f, y + 8f, z0 - 20f), Vector3.right),
            EndingProps.AlarmLamp(root, new Vector3(29.9f, y + 8f, z0 - 20f), Vector3.left),
        };
        // The steam mains' own light, for the bursting: dark until then (they rise along the platform's far wall, just south of here)
        var furnaces = new List<Light>();
        for (int i = 0; i < 4; i++)
        {
            var l = LevelKit.Light(root, new Vector3(-21f + i * 14f, y + 4.5f, z0 - 5f), 26f, 0f);
            l.color = new Color(1f, 0.95f, 0.85f);
            l.enabled = false;
            furnaces.Add(l);
        }

        // The surface, eight kilometres east, and the ending that joins the two
        var set = EndingSet.Build(root);
        var host = new GameObject("MineBlast");
        host.transform.SetParent(root);
        var cut = host.AddComponent<MineBlast>();
        cut.config = config;
        cut.post = post;
        cut.cage = cage;
        cut.alarms = alarms;
        cut.furnaceLights = furnaces;
        cut.set = set;
        cut.floorY = y;
        cut.bayZ0 = z0;
        cut.bayZ1 = z1;
        cut.wellCentre = wellCentre;
        cut.wellTop = wellTop;
        cut.furnaceZ = z0 - 2.6f;
        cut.arenaZ0 = z0 - 60f;

        // No finish door: the ending ends the level. The portal the bootstrap lays goes somewhere no one can reach
        Last.finishPortal = new Vector3(0f, y - 400f, z1);
        Last.length = z1;
        return new Cursor { z = z1, floorY = y, zone = c.zone, prevZone = c.zone, hasWall = true };
    }
}

/// <summary>
/// THE SURFACE, for the ending: the terminus's forecourt with the lift head in it under its canopy; THE WOUND the
/// bursting mains made running north from it — the Underground ran under the city, and when its mains went the ground
/// went with them: sinkholes where the stations were with the burst mains burning in them, fissures out of the lift
/// head still glowing, towers toppled and leaning and cut to stubs, fires, smoke, the viaduct down; THE ROOFTOPS
/// beyond — a chain of the working city's towers bridged one to the next, rising toward the house; and the House
/// NINE kilometres off over all of it — across the whole city, and so big it is still the biggest thing in the frame
/// from here — with the skyline behind. The walk Chapter 3 makes, laid out in one frame: the debris, then the roofs,
/// then the door. Built EIGHT kilometres east of the level, because the ending's surface shots draw six (MineBlast
/// raises the camera's far clip) and the level has to stay out of that reach. Built dark; the cutscene lights it with
/// the dawn when the lift breaks the street.
/// </summary>
public static class EndingSet
{
    public class Parts
    {
        public Transform root;
        public Vector3 origin, mouth, towerFoot, land;
        public Transform headframe, winch, stack, gantry;
        public GameObject pit;
        public List<Renderer> beacons = new List<Renderer>();
    }

    public const float TowerScale = MenuBackdrop.TowerScale;   // the one megastructure, the size the menu shows it: nine kilometres off it is still the biggest thing in the frame
    public static float TowerH => CityKit.TowerH * TowerScale;
    public const float MouthHalf = 8f;

    /// One sheet of ground between two x and two z bounds, solid: the debris has to land on it
    static void Sheet(Transform root, float x0, float x1, float z0, float z1, float y, float thick, Material mat, string name)
    {
        if (x1 - x0 < 0.01f || z1 - z0 < 0.01f) return;
        LevelKit.Slab(root, new Vector3((x0 + x1) * 0.5f, y, (z0 + z1) * 0.5f), new Vector3(x1 - x0, thick, z1 - z0), mat, name);
    }

    public static Parts Build(Transform parent)
    {
        var p = new Parts();
        var root = new GameObject("Surface").transform;
        root.SetParent(parent);
        p.root = root;
        Vector3 O = new Vector3(8000f, 0f, 0f);   // far enough east that the level is past the surface camera's far clip
        p.origin = O;
        Vector3 M = O + new Vector3(0f, 0f, 10f);
        p.mouth = M;
        p.land = O + new Vector3(-3.5f, 0f, -14f);
        p.towerFoot = O + new Vector3(0f, 0f, 9000f);   // across the city
        Random.InitState(2031);

        // The ground, laid in four sheets round the mouth so the hole is a hole. It runs out past the surface camera's far
        // clip in every direction, so the world never visibly ends: the haze takes the edge instead. The yard's concrete
        // on it the same way; the pit's floor thirty metres down, so what falls in lands
        const float GroundReach = 13000f;
        float gx0 = O.x - GroundReach, gx1 = O.x + GroundReach, gz0 = O.z - GroundReach, gz1 = O.z + GroundReach + 1000f, hx0 = M.x - MouthHalf, hx1 = M.x + MouthHalf, hz0 = M.z - MouthHalf, hz1 = M.z + MouthHalf;
        Sheet(root, gx0, hx0, gz0, gz1, -0.6f, 1f, EndingProps.Ash, "Ground");
        Sheet(root, hx1, gx1, gz0, gz1, -0.6f, 1f, EndingProps.Ash, "Ground");
        Sheet(root, hx0, hx1, gz0, hz0, -0.6f, 1f, EndingProps.Ash, "Ground");
        Sheet(root, hx0, hx1, hz1, gz1, -0.6f, 1f, EndingProps.Ash, "Ground");
        float yx0 = O.x - 38f, yx1 = O.x + 38f, yz0 = O.z - 30f, yz1 = O.z + 34f;
        Sheet(root, yx0, hx0, yz0, yz1, -0.3f, 0.6f, EndingProps.Concrete, "Yard");
        Sheet(root, hx1, yx1, yz0, yz1, -0.3f, 0.6f, EndingProps.Concrete, "Yard");
        Sheet(root, hx0, hx1, yz0, hz0, -0.3f, 0.6f, EndingProps.Concrete, "Yard");
        Sheet(root, hx0, hx1, hz1, yz1, -0.3f, 0.6f, EndingProps.Concrete, "Yard");
        p.pit = LevelKit.Slab(root, M + new Vector3(0f, -30.5f, 0f), new Vector3(MouthHalf * 2f + 1f, 1f, MouthHalf * 2f + 1f), EndingProps.Black, "PitFloor");
        LevelKit.Slab(root, M + new Vector3(-MouthHalf - 0.5f, -15f, 0f), new Vector3(1f, 30f, MouthHalf * 2f + 2f), LevelKit.Tile, "ShaftHeadW");
        LevelKit.Slab(root, M + new Vector3(MouthHalf + 0.5f, -15f, 0f), new Vector3(1f, 30f, MouthHalf * 2f + 2f), LevelKit.Tile, "ShaftHeadE");
        LevelKit.Slab(root, M + new Vector3(0f, -15f, -MouthHalf - 0.5f), new Vector3(MouthHalf * 2f, 30f, 1f), LevelKit.Tile, "ShaftHeadS");
        LevelKit.Slab(root, M + new Vector3(0f, -15f, MouthHalf + 0.5f), new Vector3(MouthHalf * 2f, 30f, 1f), LevelKit.Tile, "ShaftHeadN");

        // The lift head: a brass edge round the opening, red lamps at its corners, brass posts along its sides, the
        // canopy over it on four columns with HALCYON METRO on the fascia and the clock, the winding house on its roof;
        // the ticket kiosk; floods
        LevelKit.BrassStrip(root, M + new Vector3(-MouthHalf - 0.5f, 0f, -MouthHalf - 0.5f), M + new Vector3(MouthHalf + 0.5f, 0f, -MouthHalf - 0.5f));
        LevelKit.BrassStrip(root, M + new Vector3(-MouthHalf - 0.5f, 0f, MouthHalf + 0.5f), M + new Vector3(MouthHalf + 0.5f, 0f, MouthHalf + 0.5f));
        LevelKit.BrassStrip(root, M + new Vector3(-MouthHalf - 0.5f, 0f, -MouthHalf - 0.5f), M + new Vector3(-MouthHalf - 0.5f, 0f, MouthHalf + 0.5f));
        LevelKit.BrassStrip(root, M + new Vector3(MouthHalf + 0.5f, 0f, -MouthHalf - 0.5f), M + new Vector3(MouthHalf + 0.5f, 0f, MouthHalf + 0.5f));
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                Vector3 corner = M + new Vector3(sx * (MouthHalf + 0.9f), 0f, sz * (MouthHalf + 0.9f));
                LevelKit.Prim(root, corner + Vector3.up * 0.45f, new Vector3(0.08f, 0.9f, 0.08f), LevelKit.Brass, "LampPost", PrimitiveType.Cylinder);
                LevelKit.Prim(root, corner + Vector3.up * 1f, Vector3.one * 0.3f, LevelKit.SignalRed, "HeadLamp", PrimitiveType.Sphere);
            }
        p.headframe = new GameObject("Canopy").transform;
        p.headframe.SetParent(root);
        LevelKit.Column(p.headframe, M + new Vector3(-11.5f, 0f, -9f), 12f, 1.2f);
        LevelKit.Column(p.headframe, M + new Vector3(11.5f, 0f, -9f), 12f, 1.2f);
        LevelKit.Column(p.headframe, M + new Vector3(-11.5f, 0f, 9f), 12f, 1.2f);
        LevelKit.Column(p.headframe, M + new Vector3(11.5f, 0f, 9f), 12f, 1.2f);
        LevelKit.Prim(p.headframe, M + new Vector3(0f, 12.3f, 0f), new Vector3(26f, 0.6f, 20f), LevelKit.Cream, "Canopy");
        LevelKit.Prim(p.headframe, M + new Vector3(0f, 12.9f, 0f), new Vector3(22f, 0.5f, 16f), LevelKit.Charcoal, "CanopyStep");
        LevelKit.Prim(p.headframe, M + new Vector3(0f, 13.35f, 0f), new Vector3(17f, 0.4f, 12f), LevelKit.Brass, "CanopyCrown");
        LevelKit.Prim(p.headframe, M + new Vector3(0f, 11.3f, -10.15f), new Vector3(26f, 1.4f, 0.3f), LevelKit.Oxblood, "Fascia");
        LevelKit.Text(p.headframe, M + new Vector3(0f, 11.3f, -10.32f), Quaternion.LookRotation(Vector3.forward), "HALCYON METRO", 0.42f, LevelKit.SignCream, "FasciaText", 22f, true, 400f, 900f);
        LevelKit.ClockFace(p.headframe, M + new Vector3(0f, 9.4f, -10.3f), Vector3.back, 2.4f, 290f, 50f);
        LevelKit.Prim(p.headframe, M + new Vector3(0f, 14.5f, 0f), new Vector3(6f, 2.4f, 4f), LevelKit.RivetIron, "WindingHouse");
        for (int i = 0; i < 3; i++) LevelKit.Cable(p.headframe, M + new Vector3(-4f, 12f, (i - 1) * 2f), M + new Vector3(-4f, -28f, (i - 1) * 2f), 0.1f);
        p.winch = new GameObject("Kiosk").transform;
        p.winch.SetParent(root);
        LevelKit.Kiosk(p.winch, O + new Vector3(-16f, 0f, 2f), 90f);
        LevelKit.Queue(root, O + new Vector3(-6f, 0f, -2f), Vector3.right, 4, 3f);
        LevelKit.Queue(root, M + new Vector3(-MouthHalf - 2.6f, 0f, -7f), Vector3.forward, 4, 3.8f);
        LevelKit.Queue(root, M + new Vector3(MouthHalf + 2.6f, 0f, -7f), Vector3.forward, 4, 3.8f);
        LevelKit.HoloSign(root, O + new Vector3(17f, 3.2f, -9f), Vector3.back, 8f, 1.6f, "HALCYON METRO  ·  TERMINUS  ·  LIFT 3", true);
        LevelKit.Floodlight(root, O + new Vector3(-18f, 8f, -8f), new Vector3(0.7f, -0.6f, 0.5f), 30f, 2.4f);
        LevelKit.Floodlight(root, O + new Vector3(18f, 8f, -8f), new Vector3(-0.7f, -0.6f, 0.5f), 30f, 2.4f);
        for (int i = 0; i < 4; i++) LevelKit.Bollard(root, O + new Vector3(-9f + i * 6f, 0f, -26f));
        LevelKit.CrateStack(root, O + new Vector3(-24f, 0f, -12f), 15f);
        LevelKit.CrateStack(root, O + new Vector3(26f, 0f, -16f), -20f);
        LevelKit.Cart(root, O + new Vector3(22f, 0f, -4f), 90f);
        LevelKit.StationBench(root, O + new Vector3(-20f, 0f, 20f), 0f);

        // The forecourt's walls, low limestone under brass; the city's stacks over them; the clock tower on a pivot
        for (int s = -1; s <= 1; s += 2)
        {
            LevelKit.Slab(root, O + new Vector3(s * 36f, 1.3f, 2f), new Vector3(1f, 2.6f, 64f), LevelKit.Limestone, "ForecourtWall");
            LevelKit.Prim(root, O + new Vector3(s * 36f, 2.7f, 2f), new Vector3(1.2f, 0.2f, 64f), LevelKit.Brass, "ForecourtWallCap");
        }
        LevelKit.Slab(root, O + new Vector3(-22f, 1.3f, -30f), new Vector3(28f, 2.6f, 1f), LevelKit.Limestone, "ForecourtWallS");
        LevelKit.Slab(root, O + new Vector3(22f, 1.3f, -30f), new Vector3(28f, 2.6f, 1f), LevelKit.Limestone, "ForecourtWallS");
        LevelKit.Smokestack(root, O + new Vector3(-30f, 0f, 26f), 36f, 3.2f);
        LevelKit.Smokestack(root, O + new Vector3(31f, 0f, 34f), 30f, 2.8f);
        p.stack = EndingProps.Stack(root, O + new Vector3(24f, 0f, 14f), 28f, 3f);

        // The plaza between the yard and the house is the city's own (HalcyonCity); the bollards on the way to it stay
        for (int i = 0; i < 5; i++)
        {
            float x = -60f + i * 30f;
            LevelKit.Prim(root, O + new Vector3(x, 0.4f, 80f), new Vector3(2.4f, 0.8f, 2.4f), LevelKit.Graphite, "PlazaBollard");
            LevelKit.Prim(root, O + new Vector3(x, 0.9f, 80f), new Vector3(1.2f, 0.1f, 1.2f), LevelKit.Amber, "PlazaBollardLight");
        }

        // The house: the megastructure and its neighbours, the skyline behind, cloud over all of it
        LevelKit.Init();
        CityKit.Init(false, 1.4f);
        var baker = new MeshBaker { CellSize = 400f };   // welded per 400 m square: what the frame cannot see, the GPU never draws
        var sheets = new SheetBaker();
        MenuCity.Init(1f, false, p.towerFoot);   // the utopia's own towers for the ranks (HalcyonCity raises the house and calls Init again for itself)
        var bPhase = new List<float>();
        var living = new GameObject("SurfaceLife").transform;   // what moves out here: beacons, drones, cranes, airships
        living.SetParent(root);
        var life = living.gameObject.AddComponent<CityLife>();
        float ts = TowerScale;   // the house itself is the plan's (HalcyonCity, below); the rot up its foot is ours
        LevelKit.Tendril(root, p.towerFoot + new Vector3(-26f, 0f, -24f) * ts, p.towerFoot + new Vector3(-13f, 96f, -17.6f) * ts, 1.5f * ts, 16);
        LevelKit.Tendril(root, p.towerFoot + new Vector3(22f, 0f, -28f) * ts, p.towerFoot + new Vector3(15f, 60f, -17.6f) * ts, 1f * ts, 12);
        LevelKit.Tendril(root, p.towerFoot + new Vector3(-6f, 0f, -34f) * ts, p.towerFoot + new Vector3(-3f, 30f, -21.6f) * ts, 0.8f * ts, 9);
        // THE CITY: everything Chapter 3 has to cross, and the ruin you are standing in — DENSE and TALL, because you
        // are at ground level and the house has to be the one thing that dwarfs all of it. Ranks every hundred metres
        // from just behind the yard to the house's plaza; in each rank the towers stand on a jittered grid either side
        // of a clear corridor, so nothing overlaps and nothing hides the house. Near: ruins, dark, broken at the top.
        // Middle: lit, alive, the city that still works. Far: plain masses in the haze. Only what the cutscene's camera
        // can ever see is built at all — every shot looks north from the yard, so nothing stands behind it and the
        // sides are cut to the frame's own wedge — and what is built is welded per square, so the frame culls the rest.
        const float Pitch = 90f;
        float endZ = p.towerFoot.z - 2700f;                                    // the last rank before the plan's own skyline takes over
        for (float z = -120f; z < endZ; z += Random.Range(100f, 130f) * (z > 2400f ? 1.4f : 1f))
        {
            float far = Mathf.InverseLerp(120f, endZ, Mathf.Max(z, 120f));     // 0 at the yard, 1 at the house
            float corridor = Mathf.Lerp(120f, 220f, far);                      // the sightline to the house, always clear
            float spread = Mathf.Min(6000f, 1.3f * (z + 150f) + 80f);          // the frame's wedge: nothing outside it is ever seen
            bool ruin = z < 900f;                                              // the wound
            bool alive = z >= 900f && z < 2400f;                               // the lit city: near enough that a window is more than a pixel
            for (int side = -1; side <= 1; side += 2)
            {
                for (float col = ruin && z >= 140f ? 30f : corridor + 50f; col < spread; col += Pitch * (col > 2600f ? 2.2f : alive || ruin ? 1f : 1.12f))
                {
                    float x = side * (col + Random.Range(-9f, 9f));
                    float h = ruin ? Random.Range(140f, 380f) : alive ? Random.Range(120f, 360f)
                            : z > endZ - 1500f ? Random.Range(200f, 420f) : Mathf.Lerp(120f, 260f, far) + Random.Range(-40f, 140f); // rising into a downtown at the house
                    float w = Random.Range(30f, 62f), d = Random.Range(30f, 54f);
                    bool lit = alive && Mathf.Abs(x) < 900f;                    // lit floors only where they are more than a pixel
                    Vector3 at = O + new Vector3(x, 0f, z + Random.Range(-12f, 12f));
                    if (ruin)
                    {
                        // THE WOUND: nothing this near the mouth stands straight — toppled, leaning, cut to a stub. Inside the
                        // corridor only the low ruin, so the house's body stays in view over it
                        Vector3 flat = at - M; flat.y = 0f;
                        Ruin(baker, at, new Vector3(w, h, d), flat.normalized, flat.magnitude, Mathf.Abs(x) < corridor);
                        if (Mathf.Abs(x) < 700f && Random.value < 0.3f)
                            LevelKit.Rubble(root, at + new Vector3(Random.Range(-0.7f, 0.7f) * w, 0f, -d * 0.5f - Random.Range(8f, 20f)), Random.Range(8f, 16f), 6);
                        continue;
                    }
                    MenuCity.OuterBlock(baker, sheets, at, w, d, h, Bearing(at, p.towerFoot), Random.value < 0.07f ? 4 : Random.Range(0, 4), lit, lit ? 45f : 0f);   // the utopia's own towers, every one facing the house
                }
            }
            // The ground between is ruin: rubble every few ranks, close enough to read — never in the yard itself
            if (z < 900f && Random.value < 0.5f)
            {
                float rx = (Random.value < 0.5f ? -1f : 1f) * Random.Range(70f, 260f);
                LevelKit.Rubble(root, O + new Vector3(rx, 0f, z), Random.Range(6f, 14f), 6);
            }
        }
        Wound(root, baker, life, O, M);
        Route(root, baker, sheets, life, O, p, bPhase, p.towerFoot.z - 2700f);
        // THE HOUSE, and the city it was drawn in the middle of: the menu's plan, whole, nine kilometres off: the Court,
        // the terraces, the belt, the rings, the arcologies with their pillars of light, the far masses round all of it
        HalcyonCity.Raise(root, p.towerFoot, 1f, false, life, p.beacons, bPhase);
        // And the city past the house, all the way round the north: a far band three and a half to five and a half
        // kilometres out, either side of the tower and behind it, so the horizon is city from one edge of the frame to
        // the other. All of it stands further out than the house, so none of it can stand in front of it.
        CityKit.Skyline(baker, O, 0f, 260, 9800f, 11800f, 14f, 166f, 2.2f);   // the horizon behind the house: city to the end of the world
        baker.Flush(root, "SurfaceCity");
        sheets.Flush(root, "SurfaceSheets");
        LevelKit.Clouds(root, O + new Vector3(0f, 0f, 4200f), 2600f, 8400f, 380f, 26);
        life.SetBeacons(p.beacons, bPhase);
        Random.InitState(System.Environment.TickCount);
        return p;
    }

    // ---------------------------------------------------------------- the wound

    /// A ruin near the blast: a tower, leaning or toppled or cut to a stub by how close it stood to the mouth. Not the
    /// kit's Building — those stand straight; these do not. `low` keeps it down (the corridor: the house stays in view)
    static void Ruin(MeshBaker b, Vector3 at, Vector3 size, Vector3 away, float r, bool low)
    {
        float pick = Random.value;
        if (r < 340f || low)
        {
            // In the blast's own ring nothing stands: toppled, or a stub
            if (pick < 0.55f) Toppled(b, at, size, away, Random.Range(76f, 88f));
            else Stub(b, at, size, Random.Range(0.06f, 0.2f));
            return;
        }
        if (r < 700f)
        {
            if (pick < 0.28f) Toppled(b, at, size, away, Random.Range(70f, 86f));
            else if (pick < 0.42f) Stub(b, at, size, Random.Range(0.15f, 0.4f));
            else Leaning(b, at, size, away, Random.Range(3f, 13f));
            return;
        }
        if (pick < 0.12f) Toppled(b, at, size, away, Random.Range(60f, 84f));
        else Leaning(b, at, size, away, Random.Range(0f, 5f));
    }

    /// Tipped over: the top goes the way of `away`
    static Quaternion Lean(Vector3 away, float deg) => Quaternion.AngleAxis(deg, Vector3.Cross(Vector3.up, away).normalized);

    static void Leaning(MeshBaker b, Vector3 at, Vector3 size, Vector3 away, float deg)
    {
        var rot = Lean(away, deg);
        b.Add(CityKit.Hollow, at + rot * new Vector3(0f, size.y * 0.5f, 0f), size, rot);
        if (Random.value < 0.45f)   // broken at the top: a chunk of the upper floors leaning off the roof
            b.Add(CityKit.Hollow, at + rot * new Vector3(Random.Range(-0.15f, 0.15f) * size.x, size.y * 1.05f, Random.Range(-0.15f, 0.15f) * size.z),
                new Vector3(size.x * 0.55f, size.y * 0.16f, size.z * 0.55f), rot * Quaternion.Euler(Random.Range(-16f, 16f), Random.Range(0f, 360f), Random.Range(-12f, 12f)));
    }

    /// Fallen: most of its length lying out from its own foot the way it fell, the top end in pieces, a stub left standing
    static void Toppled(MeshBaker b, Vector3 at, Vector3 size, Vector3 away, float deg)
    {
        var rot = Lean(away, deg);
        float len = size.y * Random.Range(0.6f, 0.9f);
        b.Add(CityKit.Hollow, at + rot * new Vector3(0f, len * 0.5f, 0f), new Vector3(size.x, len, size.z), rot);
        Vector3 end = at + rot * new Vector3(0f, len, 0f);
        Debris(b, new Vector3(end.x, at.y, end.z), Mathf.Max(size.x, size.z) * 1.2f, 10, CityKit.Hollow);
        Stub(b, at, size, Random.Range(0.03f, 0.08f));
    }

    static void Stub(MeshBaker b, Vector3 at, Vector3 size, float k)
    {
        float h = size.y * k;
        b.Add(CityKit.Hollow, at + Vector3.up * (h * 0.5f), new Vector3(size.x, h, size.z));
        Debris(b, at + Vector3.up * h, Mathf.Max(size.x, size.z) * 0.8f, 6, CityKit.Hollow);
    }

    /// Baked rubble: boxes at every angle, in a heap
    static void Debris(MeshBaker b, Vector3 at, float radius, int count, Material m)
    {
        for (int i = 0; i < count; i++)
        {
            float s = Random.Range(2f, 7f);
            Vector3 p = at + new Vector3(Random.Range(-radius, radius), s * 0.4f, Random.Range(-radius, radius));
            b.Add(m, p, new Vector3(s * Random.Range(0.6f, 1.6f), s * Random.Range(0.4f, 1f), s * Random.Range(0.6f, 1.6f)),
                Quaternion.Euler(Random.Range(-30f, 30f), Random.Range(0f, 360f), Random.Range(-30f, 30f)));
        }
    }

    /// THE WOUND. The mine ran under the city, and when it went the ground went with it: a chain of sinkholes up the
    /// corridor toward the house where the shafts were, the melt showing in the bottom of them; fissures out of the
    /// mouth, still glowing; fires in the flattened ring; smoke standing over all of it; the viaduct that crossed here
    /// down in the middle. What Chapter 3 is made of.
    static void Wound(Transform root, MeshBaker b, CityLife life, Vector3 O, Vector3 M)
    {
        // Fissures out of the mouth, glowing: the fire is still down there
        for (int i = 0; i < 12; i++)
        {
            float ang = Random.Range(-80f, 80f) * Mathf.Deg2Rad;
            float len = Random.Range(160f, 620f), wdt = Random.Range(3f, 7f);
            Vector3 dir = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));
            Vector3 mid = M + dir * (30f + len * 0.5f);
            Quaternion rot = Quaternion.LookRotation(dir);
            b.Add(EndingProps.Black, mid + Vector3.up * 0.04f, new Vector3(wdt * 2.2f, 0.08f, len), rot);
            b.Add(LevelKit.Molten, mid + Vector3.up * 0.08f, new Vector3(wdt, 0.08f, len * Random.Range(0.6f, 0.95f)), rot);
        }
        // The sinkholes: where the shafts were, in a line up the corridor, the ground gone and the melt showing
        float zz = 260f;
        int k = 0;
        while (zz < 3600f)
        {
            float rad = Random.Range(45f, 95f) * (zz < 900f ? 1f : 0.75f);
            Sinkhole(root, b, life, O + new Vector3(Random.Range(-70f, 70f), 0f, zz), rad, k++);
            zz += Random.Range(260f, 380f);
        }
        // Fires in the flattened ring, and smoke standing over it
        for (int i = 0; i < 14; i++)
        {
            float a = Random.Range(-70f, 70f) * Mathf.Deg2Rad, r = Random.Range(90f, 620f);
            Vector3 at = M + new Vector3(Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r);
            EndingProps.Flames.Spawn(root, at, Random.Range(2.5f, 6f));
            if (i % 2 == 0) EndingProps.SmokeColumn.Spawn(root, at + Vector3.up * 2f, Random.Range(6f, 14f), Random.Range(90f, 220f), 14, Random.Range(0.06f, 0.1f));
        }
        // The viaduct that crossed here, down in the middle: both ends still standing, the span between them fallen
        float vy = 30f, vz = O.z + 230f;
        CityKit.Viaduct(b, O + new Vector3(-760f, vy, vz - 20f), O + new Vector3(-95f, vy, vz), 16f, 0f);
        CityKit.Viaduct(b, O + new Vector3(95f, vy, vz), O + new Vector3(760f, vy + 6f, vz + 30f), 16f, 0f);
        b.Add(CityKit.Mass, O + new Vector3(-40f, 12f, vz + 4f), new Vector3(16f, 1.2f, 110f), Quaternion.Euler(0f, 6f, 24f));   // the fallen span, one end down
        b.Add(CityKit.Mass, O + new Vector3(52f, 2f, vz - 12f), new Vector3(16f, 1.2f, 70f), Quaternion.Euler(4f, 100f, 2f));    // and the rest of it, flat
        Debris(b, O + new Vector3(0f, 0f, vz), 60f, 24, CityKit.Mass);
    }

    /// One sinkhole: the ground gone in a ring, the melt in the bottom, the rim broken up and what stood on it tipped
    /// in, a fire, smoke, and the company's drones over its own hole
    static void Sinkhole(Transform root, MeshBaker b, CityLife life, Vector3 c, float r, int index)
    {
        b.Add(EndingProps.Black, c + Vector3.up * 0.06f, new Vector3(r * 2f, 0.1f, r * 2f), Quaternion.identity, PrimitiveType.Cylinder);
        b.Add(LevelKit.Molten, c + Vector3.up * 0.1f, new Vector3(r * Random.Range(0.5f, 0.8f), 0.08f, r * Random.Range(0.5f, 0.8f)),
            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), PrimitiveType.Cylinder);
        for (int i = 0; i < 22; i++)
        {
            float a = i * 360f / 22f * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            Debris(b, c + dir * (r + Random.Range(4f, 18f)), 9f, 4, CityKit.Hollow);
            if (i % 5 == 0)
            {
                float w = Random.Range(24f, 40f), h = Random.Range(40f, 120f);
                Vector3 foot = c + dir * (r + w * 0.6f + Random.Range(6f, 20f));
                var rot = Lean(-dir, Random.Range(14f, 32f));   // into the hole
                b.Add(CityKit.Hollow, foot + rot * new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, w), rot);
            }
        }
        EndingProps.Flames.Spawn(root, c + new Vector3(Random.Range(-r, r) * 0.5f, 0.2f, Random.Range(-r, r) * 0.5f), Random.Range(3f, 5f));
        EndingProps.SmokeColumn.Spawn(root, c + Vector3.up * 2f, r * 0.25f, Random.Range(140f, 260f), 16, 0.07f);
        if (life != null && index % 2 == 0) CityKit.Patrol(root, life, c, r + 30f, 40f + Random.Range(0f, 30f), 3);
    }

    /// THE ROOFTOPS. The way across: a chain of the working city's towers up the corridor from the wound to the house,
    /// each within a bridge of the next and rising as they go — gantries, cranes, gardens, searchlights — the ground
    /// the next chapter is played on, laid out in one frame. Airships and aircraft over the lot: the city goes on.
    /// The bearing from one point to another, degrees from north toward east: which way a tower faces
    static float Bearing(Vector3 from, Vector3 to) => Mathf.Atan2(to.x - from.x, to.z - from.z) * Mathf.Rad2Deg;

    static void Route(Transform root, MeshBaker b, SheetBaker sheets, CityLife life, Vector3 O, Parts p, List<float> bPhase, float endZ)
    {
        Vector3 prevTop = Vector3.zero;
        bool has = false;
        int n = 0;
        for (float z = 720f; z < endZ; z += Random.Range(130f, 180f), n++)
        {
            float far = Mathf.InverseLerp(720f, endZ, z);
            float x = (n % 2 == 0 ? -1f : 1f) * Random.Range(20f, 75f);
            float h = Mathf.Lerp(70f, 330f, far) + Random.Range(-15f, 25f);   // rising the whole way, so the chain shows over the city
            float w = Random.Range(34f, 58f), d = Random.Range(34f, 52f);
            Vector3 at = O + new Vector3(x, 0f, z);
            float roof = MenuCity.OuterTower(b, sheets, at, w, d, h, Bearing(at, p.towerFoot), n % 3 == 0 ? 3 : n % 3 == 1 ? 0 : 2, true);
            Vector3 top = at + Vector3.up * roof;
            if (has)
            {
                float y = Mathf.Min(prevTop.y, top.y) - 2f;
                CityKit.Bridge(b, new Vector3(prevTop.x, y, prevTop.z), new Vector3(top.x, y, top.z), 6f);
            }
            if (n % 2 == 1) CityKit.Terrace(b, at, top.y + 0.2f, new Vector2(w * 0.8f, d * 0.8f));
            if (life != null && n % 5 == 2) CityKit.Crane(root, b, life, new Vector3(at.x + w * 0.3f, top.y, at.z - d * 0.25f), 26f);
            if (life != null && n % 6 == 1) CityKit.Searchlight(root, life, new Vector3(at.x, top.y + 3f, at.z), 300f, 12f, Random.Range(4f, 8f), Random.Range(0f, 360f));
            prevTop = top;
            has = true;
        }
        if (life == null) return;
        CityKit.Airship(root, life, O + new Vector3(-900f, 260f, 1400f), O + new Vector3(900f, 280f, 2200f), 0.005f, 0.2f);
        CityKit.Airship(root, life, O + new Vector3(700f, 300f, 2600f), O + new Vector3(-700f, 300f, 1000f), 0.004f, 0.7f);
        for (int i = 0; i < 3; i++)
            CityKit.Aircraft(root, life, O + new Vector3(-1500f + i * 300f, 420f + i * 60f, 900f + i * 500f), O + new Vector3(1500f, 440f + i * 60f, 2400f - i * 300f), 0.012f + i * 0.003f, i * 0.33f);
    }
}
