using UnityEngine;

/// <summary>
/// THE SURFACE KIT (Chapter 3), DARK DECO: what the House put up round the railway's disaster, and the roofline of 1939.
///   THE CORDON         a wall of red light round the whole district, a hundred
///                      and thirty metres of it, on stone pylons: the way out is
///                      over the top, through the one gate in it, at roof height.
///                      Red means what it always means.
///   RESPONSE BEACONS   the marker posts the district brings you back to.
///   RELIEF VALVES      a valve chest on the steam main in a brick valve house,
///                      the brass wheel on its face, a cyan locking bar across the
///                      wheel (the one cyan: a cut), and a column of light over
///                      the site you can see from anywhere in the cordon.
///   THERMALS           the look of a column of rising air (Level 12's); the
///                      STEAM COLUMN is the same column over a burst main.
///   THE STACK          the relief stack's grate and governor, its steam, the
///                      lamps of its gauge. TRESTLES are the police's barriers:
///                      a plank at a metre, a lip to boost on or a beam to slide under.
///   THE SOOT           falling, wherever you are: the tunnel's, thrown up by the burst.
///   THE ROOFLINE       clock faces, water towers, chimneys, radio masts with red
///                      lamps, bronze statues on stepped plinths, searchlight towers
///                      sweeping the sky — the things a 1939 roof carries. All
///                      looks (no colliders) except a plinth's steps and the bronze on them.
/// The district does the work (District); these are what it looks like.
/// </summary>
public static partial class LevelKit
{
    static Material cordonMat, cordonBandMat, siteBeamMat, lampOffMat, lampOnMat, thermalHoopMat, thermalEmberMat, shaftAirMat, ashFallMat, beamMat;

    public static Material LampOff { get { EnsureSurface(); return lampOffMat; } }
    public static Material LampOn { get { EnsureSurface(); return lampOnMat; } }
    public static Material CordonBand { get { EnsureSurface(); return cordonBandMat; } }

    static void EnsureSurface()
    {
        if (cordonMat != null) return;
        EnsureHazards();
        EnsureWorks();
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        cordonMat = Sheet(new Color(1f, 0.1f, 0.06f, 0.13f), null, false);
        cordonBandMat = Unlit(unlit, new Color(2.3f, 0.2f, 0.12f));
        siteBeamMat = CityKit.HazeSheet(new Color(1.3f, 1.4f, 1.5f, 0.17f));
        lampOffMat = Lit(lit, new Color(0.05f, 0.05f, 0.06f), 0.4f);
        lampOnMat = Unlit(unlit, new Color(2.2f, 2.1f, 1.8f));   // a lamp lit: warm white
        thermalHoopMat = Unlit(unlit, new Color(1.1f, 0.82f, 0.26f));   // gold, and UNDER the bloom threshold: a line in the air, not a lamp
        thermalEmberMat = Particle(new Color(2.4f, 1.3f, 0.5f, 0.85f), true);
        shaftAirMat = Particle(new Color(0.8f, 0.85f, 0.9f, 0.22f), false);
        ashFallMat = Particle(new Color(0.72f, 0.7f, 0.68f, 0.5f), false);
        beamMat = Sheet(new Color(0.9f, 0.95f, 1f, 0.12f), null, true);   // a searchlight's beam
    }

    // ------------------------------------------------------------ the cordon

    static readonly string[] CordonLines =
    {
        "RAILWAY INCIDENT  ·  CORDON  ·  BY ORDER OF THE HOUSE", "THERE IS NO CAUSE FOR ALARM", "SERVICES WILL RESUME SHORTLY", "THE TRAINS ARE RUNNING ON TIME",
        "THE AIR IS PERFECTLY SAFE", "NOTHING HAS HAPPENED HERE",
    };

    /// The cordon round x0..x1, z0..z1, `height` tall, with one gate in its north face: `gateHalf` either side of x = 0,
    /// from `gateY0` to `gateY1`. Solid all the way round; the sheet is only what it looks like.
    public static void Cordon(Transform parent, float x0, float x1, float z0, float z1, float height, float gateHalf, float gateY0, float gateY1)
    {
        EnsureSurface();
        float cx = (x0 + x1) * 0.5f, cz = (z0 + z1) * 0.5f, dx = x1 - x0, dz = z1 - z0;
        // West, east, south: one sheet each. North: four, round the gate
        CordonSheet(parent, new Vector3(x0, 0f, cz), new Vector3(0.6f, height, dz), Vector3.left);
        CordonSheet(parent, new Vector3(x1, 0f, cz), new Vector3(0.6f, height, dz), Vector3.right);
        CordonSheet(parent, new Vector3(cx, 0f, z0), new Vector3(dx, height, 0.6f), Vector3.back);
        CordonSheet(parent, new Vector3((x0 - gateHalf) * 0.5f, 0f, z1), new Vector3(-gateHalf - x0, height, 0.6f), Vector3.forward);
        CordonSheet(parent, new Vector3((x1 + gateHalf) * 0.5f, 0f, z1), new Vector3(x1 - gateHalf, height, 0.6f), Vector3.forward);
        CordonSheet(parent, new Vector3(0f, 0f, z1), new Vector3(gateHalf * 2f, gateY0, 0.6f), Vector3.forward);
        CordonSheet(parent, new Vector3(0f, gateY1, z1), new Vector3(gateHalf * 2f, height - gateY1, 0.6f), Vector3.forward);
        // The gate's frame: stepped stone, the one thing in the wall that is not red, a cream lamp strip under its head
        SurfaceKit.Look(Charcoal, new Vector3(-gateHalf - 0.5f, (gateY0 + gateY1) * 0.5f, z1), new Vector3(1f, gateY1 - gateY0 + 2f, 1.4f));
        SurfaceKit.Look(Charcoal, new Vector3(gateHalf + 0.5f, (gateY0 + gateY1) * 0.5f, z1), new Vector3(1f, gateY1 - gateY0 + 2f, 1.4f));
        SurfaceKit.Look(Limestone, new Vector3(-gateHalf - 0.5f, gateY1 + 0.3f, z1), new Vector3(1.4f, 0.6f, 1.8f));
        SurfaceKit.Look(Limestone, new Vector3(gateHalf + 0.5f, gateY1 + 0.3f, z1), new Vector3(1.4f, 0.6f, 1.8f));
        SurfaceKit.Look(Charcoal, new Vector3(0f, gateY1 + 0.5f, z1), new Vector3(gateHalf * 2f + 2f, 1f, 1.4f));
        SurfaceKit.Look(Brass, new Vector3(0f, gateY1 + 1.1f, z1), new Vector3(gateHalf * 2f + 2.6f, 0.2f, 1.8f));
        SurfaceKit.Look(lampOnMat, new Vector3(0f, gateY1 - 0.1f, z1 - 0.72f), new Vector3(gateHalf * 2f, 0.12f, 0.06f));

        // The bands of light up it, the pylons it hangs on, and what it says to the people inside it
        for (float y = 9f; y < height; y += 15f)
        {
            SurfaceKit.Look(cordonBandMat, new Vector3(x0 + 0.4f, y, cz), new Vector3(0.25f, 0.5f, dz));
            SurfaceKit.Look(cordonBandMat, new Vector3(x1 - 0.4f, y, cz), new Vector3(0.25f, 0.5f, dz));
            SurfaceKit.Look(cordonBandMat, new Vector3(cx, y, z0 + 0.4f), new Vector3(dx, 0.5f, 0.25f));
            if (y < gateY0 - 1f || y > gateY1 + 1f) SurfaceKit.Look(cordonBandMat, new Vector3(cx, y, z1 - 0.4f), new Vector3(dx, 0.5f, 0.25f));
        }
        int line = 0;
        for (float z = z0; z <= z1 + 1f; z += 72.5f)
        {
            Pylon(new Vector3(x0 - 1f, 0f, Mathf.Min(z, z1)), height);
            Pylon(new Vector3(x1 + 1f, 0f, Mathf.Min(z, z1)), height);
        }
        for (float x = x0 + 70f; x < x1 - 1f; x += 70f)
        {
            Pylon(new Vector3(x, 0f, z0 - 1f), height);
            if (Mathf.Abs(x) > gateHalf + 8f) Pylon(new Vector3(x, 0f, z1 + 1f), height);
        }
        var ink = new Color(2.4f, 0.5f, 0.4f);
        for (float z = z0 + 80f; z < z1 - 40f; z += 145f)
        {
            Text(parent, new Vector3(x0 + 0.9f, 20f, z), Quaternion.LookRotation(Vector3.left), CordonLines[line++ % CordonLines.Length], 0.5f, ink, "CordonText", 120f, true, 320f, 640f);
            Text(parent, new Vector3(x1 - 0.9f, 20f, z + 60f), Quaternion.LookRotation(Vector3.right), CordonLines[line++ % CordonLines.Length], 0.5f, ink, "CordonText", 120f, true, 320f, 640f);
        }
        for (float x = x0 + 100f; x < x1 - 60f; x += 180f)
        {
            Text(parent, new Vector3(x, 20f, z0 + 0.9f), Quaternion.LookRotation(Vector3.back), CordonLines[line++ % CordonLines.Length], 0.5f, ink, "CordonText", 120f, true, 320f, 640f);
            if (Mathf.Abs(x) > 60f) Text(parent, new Vector3(x, 20f, z1 - 0.9f), Quaternion.LookRotation(Vector3.forward), CordonLines[line++ % CordonLines.Length], 0.5f, ink, "CordonText", 120f, true, 320f, 640f);
        }
    }

    /// One face of it: the sheet you see, and three metres of solid behind it (`outward` is the way out of the district)
    static void CordonSheet(Transform parent, Vector3 foot, Vector3 size, Vector3 outward)
    {
        if (size.x < 0.05f || size.y < 0.05f || size.z < 0.05f) return;
        Vector3 centre = foot + Vector3.up * (size.y * 0.5f);
        Prim(parent, centre, size, cordonMat, "Cordon");
        Vector3 thick = new Vector3(Mathf.Abs(outward.x) > 0.5f ? 3f : size.x, size.y, Mathf.Abs(outward.z) > 0.5f ? 3f : size.z);
        SurfaceKit.Solid(centre + outward * 1.5f, thick);
    }

    /// A pylon: a stepped charcoal stone shaft with a red lamp on top
    static void Pylon(Vector3 foot, float height)
    {
        SurfaceKit.Look(Charcoal, foot + Vector3.up * ((height + 6f) * 0.5f), new Vector3(4f, height + 6f, 4f));
        SurfaceKit.Look(Charcoal, foot + Vector3.up * ((height + 6f) * 0.35f), new Vector3(5.2f, (height + 6f) * 0.7f, 5.2f));
        SurfaceKit.Look(Limestone, foot + Vector3.up * 2f, new Vector3(6.4f, 4f, 6.4f));
        SurfaceKit.Look(Limestone, foot + Vector3.up * (height + 6.2f), new Vector3(4.6f, 0.4f, 4.6f));
        SurfaceKit.Look(cordonBandMat, foot + Vector3.up * (height + 6.8f), new Vector3(2f, 0.8f, 2f));
    }

    // ------------------------------------------------------------ beacons, tie-ins

    /// A response marker post: dark until the district lights it (District.Beacon)
    public static District.Beacon ResponseBeacon(Transform parent, Vector3 groundPos)
    {
        EnsureSurface();
        Solid(parent, PrimitiveType.Cube, groundPos + Vector3.up * 1.3f, Quaternion.identity, new Vector3(0.3f, 2.6f, 0.3f), Charcoal, "BeaconPost");
        Stripes(parent, groundPos + Vector3.up * 0.5f, new Vector3(0.36f, 0.3f, 0.36f));
        Decor(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * 0.02f, Quaternion.identity, new Vector3(3.2f, 0.02f, 3.2f), Charcoal, "BeaconPad");
        Decor(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * 0.035f, Quaternion.identity, new Vector3(2.4f, 0.012f, 2.4f), Brass, "BeaconRing");
        Decor(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * 0.035f, Quaternion.identity, new Vector3(2.1f, 0.013f, 2.1f), Charcoal, "BeaconRingCore");
        Decor(parent, PrimitiveType.Cube, groundPos + Vector3.up * 2.62f, Quaternion.identity, new Vector3(0.44f, 0.08f, 0.44f), Brass, "BeaconCollar");
        var lamp = Decor(parent, PrimitiveType.Sphere, groundPos + Vector3.up * 2.95f, Quaternion.identity, Vector3.one * 0.55f, lampOffMat, "BeaconLamp");
        var l = Light(parent, groundPos + Vector3.up * 3f, 12f, 2f);
        l.color = new Color(1f, 0.9f, 0.72f);
        l.enabled = false;
        return new District.Beacon { at = groundPos, lamp = lamp.GetComponent<Renderer>(), light = l };
    }

    /// A relief valve on the steam main: the brick valve house (open the way `facing` looks), the main coming up through
    /// its roof into the valve chest in the back of it, the brass wheel on the chest's face with the cyan locking bar
    /// across it (cut the bar and the valve slams shut on its own spring: the district counts it), the lamp, the words
    /// over the door, and the column of light over the site. The chest carries the Breaker.
    public static Breaker ReliefValve(Transform parent, District district, Vector3 groundPos, Vector3 facing, int index, string site)
    {
        EnsureSurface();
        facing.y = 0f;
        facing.Normalize();
        Quaternion rot = Quaternion.LookRotation(facing, Vector3.up);
        Vector3 P(float x, float y, float z) => groundPos + rot * new Vector3(x, y, z);
        const float halfW = 2.4f, depth = 3.2f, tall = 3.4f;
        Solid(parent, PrimitiveType.Cube, P(0f, tall * 0.5f, -depth * 0.5f - 0.25f), rot, new Vector3(halfW * 2f + 1f, tall, 0.5f), Brick, "ValveHouseBack");
        Solid(parent, PrimitiveType.Cube, P(-halfW - 0.25f, tall * 0.5f, 0f), rot, new Vector3(0.5f, tall, depth), Brick, "ValveHouseSide");
        Solid(parent, PrimitiveType.Cube, P(halfW + 0.25f, tall * 0.5f, 0f), rot, new Vector3(0.5f, tall, depth), Brick, "ValveHouseSide");
        Solid(parent, PrimitiveType.Cube, P(0f, tall + 0.25f, -0.25f), rot, new Vector3(halfW * 2f + 1f, 0.5f, depth + 0.5f), Brick, "ValveHouseRoof");
        Decor(parent, PrimitiveType.Cube, P(0f, tall + 0.62f, -0.25f), rot, new Vector3(halfW * 2f + 1.4f, 0.24f, depth + 0.9f), Limestone, "ValveHouseCornice");
        Decor(parent, PrimitiveType.Cube, P(0f, tall + 0.86f, -0.25f), rot, new Vector3(halfW * 2f + 1.8f, 0.24f, depth + 1.3f), Limestone, "ValveHouseCornice");
        Decor(parent, PrimitiveType.Cube, P(0f, 0.6f, -depth * 0.5f - 0.27f), rot, new Vector3(halfW * 2f + 1f, 1.2f, 0.06f), OxbloodTile, "ValveHouseDado");
        Stripes(parent, P(0f, tall + 0.52f, depth * 0.5f - 0.05f), new Vector3(halfW * 2f + 1f, 0.06f, 0.3f), rot.eulerAngles.y);

        // The chest, the main into it through the roof, the wheel on its face, the bar across the wheel
        Vector3 chestAt = P(0f, 1.3f, -depth * 0.5f + 0.5f);
        var chest = Solid(parent, PrimitiveType.Cube, chestAt, rot, new Vector3(1.9f, 2.6f, 0.9f), RivetIron, "ReliefValve");
        Decor(parent, PrimitiveType.Cube, P(0f, 2.64f, -depth * 0.5f + 0.5f), rot, new Vector3(2.1f, 0.08f, 1.1f), Brass, "ChestCap");
        Pipe(parent, P(0f, 2.68f, -depth * 0.5f + 0.5f), P(0f, tall + 3.4f, -depth * 0.5f + 0.5f), 0.6f, RivetIron);
        Pipe(parent, P(-1.2f, 0.9f, -depth * 0.5f + 0.5f), P(-halfW - 0.6f, 0.9f, -depth * 0.5f + 0.5f), 0.36f, RivetIron);
        Stripes(parent, P(0f, 0.2f, -depth * 0.5f + 0.98f), new Vector3(1.94f, 0.4f, 0.06f), rot.eulerAngles.y);
        Quaternion wheel = rot * Quaternion.Euler(90f, 0f, 0f);
        Vector3 wheelAt = P(0f, 1.45f, -depth * 0.5f + 1f);
        Decor(parent, PrimitiveType.Cylinder, wheelAt, wheel, new Vector3(1.5f, 0.05f, 1.5f), Brass, "ValveWheel");
        Decor(parent, PrimitiveType.Cylinder, wheelAt + facing * 0.02f, wheel, new Vector3(1.3f, 0.05f, 1.3f), RivetIron, "ValveWheelWell");
        for (int i = 0; i < 3; i++) Decor(parent, PrimitiveType.Cube, wheelAt + facing * 0.03f, rot * Quaternion.Euler(0f, 0f, i * 60f), new Vector3(1.36f, 0.1f, 0.06f), Brass, "ValveSpoke");
        Decor(parent, PrimitiveType.Cylinder, wheelAt + facing * 0.08f, wheel, new Vector3(0.3f, 0.14f, 0.3f), Bronze, "ValveHub");
        var bar = Decor(parent, PrimitiveType.Cube, P(0f, 1.45f, -depth * 0.5f + 1.2f), rot, new Vector3(2.2f, 0.28f, 0.14f), NeonCyan, "LockBar");
        var cut = Light(bar.transform, P(0f, 1.45f, -depth * 0.5f + 1.7f), 7f, 1.6f);
        cut.color = new Color(0.3f, 1f, 1f);
        var lamp = Decor(parent, PrimitiveType.Sphere, P(0f, 2.95f, -depth * 0.5f + 0.6f), rot, Vector3.one * 0.42f, lampOffMat, "ValveLamp");
        var glow = Light(parent, P(0f, 2.6f, 0.2f), 10f, 2.2f);
        glow.color = new Color(1f, 0.9f, 0.72f);
        glow.enabled = false;
        Text(parent, P(0f, tall + 1.3f, depth * 0.5f), Quaternion.LookRotation(-facing), "RELIEF VALVE " + (index + 1) + "  ·  " + site, 0.11f, new Color(2.2f, 1.9f, 1.4f), "ValveText", 9f, true, 90f, 180f);
        Stencil(parent, P(0f, 0.55f, -depth * 0.5f + 0.96f), facing, "SHUT  =  MAIN PRESSURE", 0.05f, 1.8f);

        var beam = Prim(parent, P(0f, tall + 131f, -0.4f), new Vector3(1.6f, 260f, 1.6f), siteBeamMat, "SiteBeam");

        var b = chest.AddComponent<Breaker>();
        b.district = district;
        b.index = index;
        b.site = site;
        b.lamp = lamp.GetComponent<Renderer>();
        b.lampOn = lampOnMat;
        b.bar = bar;
        b.beam = beam;
        b.glow = glow;
        if (district != null) district.breakers.Add(b);
        return b;
    }

    // ------------------------------------------------------------ air

    /// What a thermal looks like: the vent at the foot of it, the embers going up it, and thin gold hoops round it so the
    /// column can be read from the rim (gold: a line of flow — you fly through them). The lift itself is the district's
    /// (District.Updraft). The hoops are welded bars, unlit and under the bloom threshold.
    public static void Thermal(Transform parent, Vector3 centre, float half, float y0, float y1)
    {
        EnsureSurface();
        Decor(parent, PrimitiveType.Cylinder, new Vector3(centre.x, y0 + 0.3f, centre.z), Quaternion.identity, new Vector3(half * 1.5f, 0.05f, half * 1.5f), moltenMat, "ThermalVent");
        Decor(parent, PrimitiveType.Cylinder, new Vector3(centre.x, y0 + 0.28f, centre.z), Quaternion.identity, new Vector3(half * 1.7f, 0.04f, half * 1.7f), RivetIron, "ThermalRim");
        for (int i = 1; i <= 4; i++)
            Hoop(new Vector3(centre.x, Mathf.Lerp(y0, y1, i / 4.6f), centre.z), half * 0.92f, thermalHoopMat);
        var l = Light(parent, new Vector3(centre.x, y0 + 4f, centre.z), half * 4f, 2.6f);
        l.color = new Color(1f, 0.5f, 0.15f);
        if (!Atmosphere) return;
        float rise = 9f;
        Cloud(parent, "Thermal", new Vector3(centre.x, y0 + 1f, centre.z), new Vector3(half * 1.6f, 0.5f, half * 1.6f), thermalEmberMat, 16f, (y1 - y0) / rise, 0.1f, 0.28f,
            new Color(2.4f, 1.3f, 0.5f, 0.85f), Vector3.up * rise, 0f, 0.5f);
    }

    /// A hoop flat in the air: short bars round a circle, welded into the district
    static void Hoop(Vector3 centre, float radius, Material mat, int segments = 28, float thick = 0.09f)
    {
        float len = 2f * Mathf.PI * radius / segments * 1.03f;
        for (int i = 0; i < segments; i++)
        {
            float a = (i + 0.5f) / segments * Mathf.PI * 2f;
            Vector3 p = centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
            SurfaceKit.Look(mat, p, new Vector3(len, thick, thick * 1.5f), Quaternion.Euler(0f, 90f - a * Mathf.Rad2Deg, 0f));   // each bar along the tangent
        }
    }

    /// The relief stack's grate, in the floor at its foot: a riveted frame, brass bars, and the mains' GOVERNOR standing in
    /// the middle of it (a brass spindle with two flyballs on their arms). Returns the governor; the district spins it
    /// when the main is on full pressure.
    public static Transform ReliefGrate(Transform parent, Vector3 groundCentre, float size)
    {
        EnsureSurface();
        Solid(parent, PrimitiveType.Cube, groundCentre + Vector3.up * 0.18f, Quaternion.identity, new Vector3(size, 0.36f, size), RivetIron, "GrateFrame");
        Stripes(parent, groundCentre + Vector3.up * 0.18f, new Vector3(size + 0.06f, 0.12f, size + 0.06f));
        int bars = Mathf.Max(3, Mathf.RoundToInt(size / 0.5f));
        for (int i = 0; i < bars; i++)
            Decor(parent, PrimitiveType.Cube, groundCentre + Vector3.up * 0.4f + Vector3.forward * ((i + 0.5f) / bars * size - size * 0.5f), Quaternion.identity, new Vector3(size, 0.06f, 0.08f), Brass, "GrateBar");
        var hub = new GameObject("Governor").transform;
        hub.SetParent(parent);
        hub.position = groundCentre + Vector3.up * 0.44f;
        Decor(hub, PrimitiveType.Cylinder, hub.position + Vector3.up * 0.9f, Quaternion.identity, new Vector3(0.12f, 0.9f, 0.12f), Brass, "GovernorSpindle");
        Decor(hub, PrimitiveType.Cylinder, hub.position + Vector3.up * 0.06f, Quaternion.identity, new Vector3(0.6f, 0.06f, 0.6f), Bronze, "GovernorFoot");
        for (int s = -1; s <= 1; s += 2)
        {
            Quaternion lean = Quaternion.Euler(0f, 0f, s * 38f);
            Decor(hub, PrimitiveType.Cube, hub.position + Vector3.up * 1.7f + lean * new Vector3(0f, -0.5f, 0f), lean, new Vector3(0.05f, 1f, 0.05f), Brass, "GovernorArm");
            Decor(hub, PrimitiveType.Sphere, hub.position + Vector3.up * 1.7f + lean * new Vector3(0f, -1f, 0f), Quaternion.identity, Vector3.one * 0.3f, Brass, "Flyball");
        }
        return hub;
    }

    /// The steam up the stack when the main is on full pressure (null while a postcard is being shot): the district
    /// starts and stops it
    public static ParticleSystem StackSteam(Transform parent, Vector3 groundCentre, float size, float height)
    {
        EnsureSurface();
        if (!Atmosphere) return null;
        return Cloud(parent, "StackSteam", groundCentre + Vector3.up * 0.8f, new Vector3(size * 0.8f, 0.3f, size * 0.8f), shaftAirMat, 46f, height / 14f, 0.3f, 0.7f,
            new Color(0.8f, 0.85f, 0.9f, 0.22f), Vector3.up * 14f, 0f, 0.4f);
    }

    /// What a STEAM COLUMN looks like: a burst main's mouth (a torn iron collar) at the foot of it, white steam going up it,
    /// and thin gold hoops round it so the column can be read from the rim (gold: a line of flow — you fly through them).
    /// The lift itself is the district's (District.Updraft). A cream light at the foot.
    public static void SteamColumn(Transform parent, Vector3 centre, float half, float y0, float y1)
    {
        EnsureSurface();
        Decor(parent, PrimitiveType.Cylinder, new Vector3(centre.x, y0 + 0.3f, centre.z), Quaternion.identity, new Vector3(half * 1.1f, 0.05f, half * 1.1f), Soot, "MainMouth");
        Decor(parent, PrimitiveType.Cylinder, new Vector3(centre.x, y0 + 0.28f, centre.z), Quaternion.identity, new Vector3(half * 1.3f, 0.04f, half * 1.3f), RivetIron, "MainCollar");
        for (int i = 0; i < 6; i++)
        {
            Quaternion r = Quaternion.Euler(Random.Range(20f, 50f), i * 60f + Random.Range(-15f, 15f), 0f);
            Decor(parent, PrimitiveType.Cube, new Vector3(centre.x, y0 + 0.6f, centre.z) + Quaternion.Euler(0f, i * 60f, 0f) * Vector3.forward * (half * 0.55f), r, new Vector3(1.2f, 0.2f, 1.6f), RivetIron, "TornPlate");
        }
        for (int i = 1; i <= 4; i++)
            Hoop(new Vector3(centre.x, Mathf.Lerp(y0, y1, i / 4.6f), centre.z), half * 0.92f, thermalHoopMat);
        var l = Light(parent, new Vector3(centre.x, y0 + 4f, centre.z), half * 4f, 2.2f);
        l.color = new Color(1f, 0.95f, 0.85f);
        if (!Atmosphere) return;
        float rise = 10f;
        Cloud(parent, "SteamColumn", new Vector3(centre.x, y0 + 1f, centre.z), new Vector3(half * 1.4f, 0.5f, half * 1.4f), shaftAirMat, 30f, (y1 - y0) / rise, 0.6f, 1.6f,
            new Color(0.85f, 0.87f, 0.9f, 0.3f), Vector3.up * rise, 0f, 0.5f);
    }

    /// A steam leak out of a crack in the paving or a torn joint: a small white plume `height` tall (null for a postcard)
    public static ParticleSystem SteamLeak(Transform parent, Vector3 at, float height = 3f)
        => Steam(parent, at, Vector3.up * height, 22f, 1.7f, 0.35f, 1.2f);

    /// A police TRESTLE: an oxblood plank at a metre on two iron A-frames, the House's words on it, a red lamp at one end.
    /// The plank is solid and nothing under it is: a LIP to corner-boost over (brass along its top), a beam to slide
    /// under. `yaw` turns it; `width` is along it.
    public static void Trestle(Transform parent, Vector3 groundCentre, float width, float yaw)
    {
        EnsureSurface();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Vector3 along = rot * Vector3.right, facing = rot * Vector3.back;
        Solid(parent, PrimitiveType.Cube, groundCentre + Vector3.up * 0.9f, rot, new Vector3(width, 0.3f, 0.12f), Oxblood, "Trestle");
        Prim(parent, groundCentre + Vector3.up * (1.05f + MarkProud + 0.012f), new Vector3(width - 0.08f, 0.024f, 0.14f), Brass, "TrestleBrass", PrimitiveType.Cube, rot);
        Text(parent, groundCentre + Vector3.up * 0.9f + facing * 0.065f, Quaternion.LookRotation(-facing), "HALCYON POLICE  ·  CORDON", 0.04f, new Color(1.9f, 1.75f, 1.45f), "TrestleText", width - 0.6f, true, 40f, 90f);
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 foot = groundCentre + along * (s * (width * 0.5f - 0.3f));
            for (int k = -1; k <= 1; k += 2)
            {
                Quaternion lean = rot * Quaternion.Euler(k * 22f, 0f, 0f);
                Decor(parent, PrimitiveType.Cube, foot + Vector3.up * 0.42f + lean * new Vector3(0f, 0f, 0f), lean, new Vector3(0.08f, 0.9f, 0.08f), Iron, "TrestleLeg");
            }
            Decor(parent, PrimitiveType.Cube, foot + Vector3.up * 0.3f, rot, new Vector3(0.08f, 0.06f, 0.5f), Iron, "TrestleTie");
        }
        Decor(parent, PrimitiveType.Sphere, groundCentre + along * (width * 0.5f - 0.3f) + Vector3.up * 1.2f, Quaternion.identity, Vector3.one * 0.22f, SignalRed, "TrestleLamp");
        TechMarkers.MarkLip(groundCentre + Vector3.up * 1.05f, new Vector3(Mathf.Abs(along.x) * width + Mathf.Abs(along.z) * 0.6f + 0.6f, 0.9f, Mathf.Abs(along.z) * width + Mathf.Abs(along.x) * 0.6f + 0.6f));
    }

    /// One lamp of the shaft's gauge: a dark disc in a brass rim on a wall, facing `facing`, that the district lights
    public static Renderer GaugeLamp(Transform parent, Vector3 centre, Vector3 facing, float diameter)
    {
        EnsureSurface();
        Quaternion rot = Quaternion.LookRotation(facing) * Quaternion.Euler(90f, 0f, 0f);
        Decor(parent, PrimitiveType.Cylinder, centre, rot, new Vector3(diameter + 0.5f, 0.08f, diameter + 0.5f), Brass, "GaugeRim");
        return Decor(parent, PrimitiveType.Cylinder, centre + facing.normalized * 0.06f, rot, new Vector3(diameter, 0.08f, diameter), lampOffMat, "GaugeLamp").GetComponent<Renderer>();
    }

    /// Soot, falling: the tunnel's, thrown up by the burst; one slow field the district keeps over your head (null while
    /// a postcard is being shot)
    public static Transform AshFall(Transform parent)
    {
        EnsureSurface();
        if (!Atmosphere) return null;
        var ps = Cloud(parent, "AshFall", Vector3.up * 16f, new Vector3(70f, 1f, 70f), ashFallMat, 42f, 9f, 0.05f, 0.13f,
            new Color(0.72f, 0.7f, 0.68f, 0.5f), new Vector3(0.7f, -1.7f, 0.25f), 0f, 0.7f);
        return ps.transform;
    }

    // ------------------------------------------------------------ the roofline of 1939

    /// A clock face on a wall: a brass rim, a lit cream disc, twelve charcoal marks, charcoal hands, a warm light in front.
    /// `facing` is the way it looks. Looks only.
    public static void ClockFace(Transform parent, Vector3 centre, Vector3 facing, float diameter, float hourAngle = 300f, float minuteAngle = 60f)
    {
        EnsureSurface();
        Vector3 n = facing.normalized;
        Quaternion face = Quaternion.LookRotation(n, Vector3.up);
        Quaternion axial = face * Quaternion.Euler(90f, 0f, 0f);
        float r = diameter * 0.5f;
        Decor(parent, PrimitiveType.Cylinder, centre, axial, new Vector3(diameter + 0.5f, 0.1f, diameter + 0.5f), Brass, "ClockRim");
        Decor(parent, PrimitiveType.Cylinder, centre + n * 0.06f, axial, new Vector3(diameter, 0.04f, diameter), LampCream, "ClockDisc");
        for (int i = 0; i < 12; i++)
        {
            Quaternion m = face * Quaternion.Euler(0f, 0f, i * 30f);
            bool quarter = i % 3 == 0;
            Decor(parent, PrimitiveType.Cube, centre + n * 0.09f + m * Vector3.up * (r * 0.86f), m, new Vector3(quarter ? diameter * 0.045f : diameter * 0.025f, diameter * (quarter ? 0.12f : 0.08f), 0.02f), Charcoal, "ClockMark");
        }
        Quaternion h = face * Quaternion.Euler(0f, 0f, -hourAngle), mi = face * Quaternion.Euler(0f, 0f, -minuteAngle);
        Decor(parent, PrimitiveType.Cube, centre + n * 0.1f + h * Vector3.up * (r * 0.28f), h, new Vector3(diameter * 0.05f, r * 0.6f, 0.02f), Charcoal, "HourHand");
        Decor(parent, PrimitiveType.Cube, centre + n * 0.12f + mi * Vector3.up * (r * 0.4f), mi, new Vector3(diameter * 0.035f, r * 0.86f, 0.02f), Charcoal, "MinuteHand");
        Decor(parent, PrimitiveType.Cylinder, centre + n * 0.13f, axial, new Vector3(diameter * 0.08f, 0.02f, diameter * 0.08f), Brass, "ClockPin");
        var l = Light(parent, centre + n * (r * 0.5f + 0.5f), diameter * 2.2f, 1.2f);
        l.color = new Color(1f, 0.9f, 0.72f);
    }

    /// A water tower on a roof: four splayed iron legs with cross braces, a walnut tank with iron hoops, a stepped
    /// cap and a brass finial. Looks only: stands outside the roof's walkable plane.
    public static void WaterTower(Transform parent, Vector3 foot, float height, float yaw = 0f)
    {
        EnsureSurface();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        float legs = height * 0.5f, dia = Mathf.Max(2.4f, height * 0.42f);
        Vector3 top = foot + Vector3.up * legs;
        for (int i = 0; i < 4; i++)
        {
            Quaternion q = rot * Quaternion.Euler(0f, 45f + i * 90f, 0f);
            Vector3 a = foot + q * Vector3.forward * (dia * 0.55f), b = top + q * Vector3.forward * (dia * 0.38f);
            Vector3 d = b - a;
            Decor(parent, PrimitiveType.Cube, (a + b) * 0.5f, Quaternion.LookRotation(d.normalized, Vector3.up) * Quaternion.Euler(90f, 0f, 0f), new Vector3(0.16f, d.magnitude, 0.16f), Iron, "TowerLeg");
            Decor(parent, PrimitiveType.Cube, (a + b) * 0.5f + Vector3.up * (legs * 0.1f), q * Quaternion.Euler(0f, 45f, 0f), new Vector3(dia * 0.72f, 0.08f, 0.08f), Iron, "TowerBrace");
        }
        Decor(parent, PrimitiveType.Cube, top - Vector3.up * 0.15f, rot, new Vector3(dia * 0.8f, 0.3f, dia * 0.8f), Iron, "TowerCradle");
        Decor(parent, PrimitiveType.Cylinder, top + Vector3.up * (height * 0.2f), rot, new Vector3(dia, height * 0.2f, dia), WoodPanel, "WaterTank");
        for (int k = 0; k < 3; k++)
            Decor(parent, PrimitiveType.Cylinder, top + Vector3.up * (height * (0.06f + 0.14f * k)), rot, new Vector3(dia + 0.08f, 0.04f, dia + 0.08f), Iron, "TankHoop");
        Decor(parent, PrimitiveType.Cylinder, top + Vector3.up * (height * 0.42f), rot, new Vector3(dia + 0.3f, 0.12f, dia + 0.3f), Charcoal, "TankCap");
        Decor(parent, PrimitiveType.Cylinder, top + Vector3.up * (height * 0.455f), rot, new Vector3(dia * 0.7f, 0.1f, dia * 0.7f), Charcoal, "TankCap");
        Decor(parent, PrimitiveType.Cylinder, top + Vector3.up * (height * 0.485f), rot, new Vector3(dia * 0.35f, 0.08f, dia * 0.35f), Charcoal, "TankCap");
        Decor(parent, PrimitiveType.Cylinder, top + Vector3.up * (height * 0.52f), rot, new Vector3(0.1f, height * 0.04f, 0.1f), Brass, "TankFinial");
        Decor(parent, PrimitiveType.Cube, top + Vector3.up * (height * 0.2f) - rot * Vector3.forward * (dia * 0.5f + 0.1f), rot, new Vector3(0.08f, height * 0.4f, 0.08f), Iron, "TankLadder");
    }

    /// A chimney on a roof: brick, iron-banded, soot round its mouth, a stone cap. No smoke. Looks only.
    public static void Chimney(Transform parent, Vector3 foot, float height, float diameter) => Smokestack(parent, foot, height, diameter, false);

    /// A radio mast: a lattice of three iron legs drawn together, cross bars up it, a red lamp at the top and one
    /// halfway, a brass finial. Red: a lamp for the aircraft. Looks only.
    public static void RadioMast(Transform parent, Vector3 foot, float height)
    {
        EnsureSurface();
        float spread = Mathf.Max(0.8f, height * 0.08f);
        Vector3 top = foot + Vector3.up * height;
        for (int i = 0; i < 3; i++)
        {
            Quaternion q = Quaternion.Euler(0f, i * 120f, 0f);
            Vector3 a = foot + q * Vector3.forward * spread, b = top + q * Vector3.forward * 0.12f;
            Vector3 d = b - a;
            Decor(parent, PrimitiveType.Cube, (a + b) * 0.5f, Quaternion.FromToRotation(Vector3.up, d.normalized), new Vector3(0.1f, d.magnitude, 0.1f), Iron, "MastLeg");
        }
        int bars = Mathf.Max(2, Mathf.RoundToInt(height / 3f));
        for (int k = 1; k < bars; k++)
        {
            float t = k / (float)bars, w = Mathf.Lerp(spread, 0.12f, t) * 1.7f;
            for (int i = 0; i < 3; i++)
                Decor(parent, PrimitiveType.Cube, foot + Vector3.up * (height * t) + Quaternion.Euler(0f, i * 120f + 60f, 0f) * Vector3.forward * (w * 0.29f), Quaternion.Euler(0f, i * 120f + 60f, 0f), new Vector3(w, 0.05f, 0.05f), Iron, "MastBar");
        }
        Decor(parent, PrimitiveType.Cylinder, top + Vector3.up * 0.6f, Quaternion.identity, new Vector3(0.08f, 0.6f, 0.08f), Brass, "MastFinial");
        Decor(parent, PrimitiveType.Sphere, top + Vector3.up * 0.2f, Quaternion.identity, Vector3.one * 0.4f, dotMat, "MastLamp");
        Decor(parent, PrimitiveType.Sphere, foot + Vector3.up * (height * 0.55f) + Vector3.forward * (spread * 0.5f), Quaternion.identity, Vector3.one * 0.3f, dotMat, "MastLamp");
        var l = Light(parent, top + Vector3.up * 0.2f, 14f, 1.2f);
        l.color = new Color(1f, 0.2f, 0.12f);
    }

    /// A bronze statue on a stepped plinth: the steps are solid (stand on them), and so are the figure's legs and coat
    /// (a plinth is a place to stand, and a bronze is not a thing to walk through); the figure is a stepped abstraction
    /// of someone standing with a torch raised, a warm lamp in its hand. `yaw` is the way it faces.
    public static void StatuePlinth(Transform parent, Vector3 foot, float yaw, float scale = 1f)
    {
        EnsureSurface();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        float s = scale;
        var step0 = Slab(parent, foot + Vector3.up * (0.2f * s), new Vector3(3.6f * s, 0.4f * s, 3.6f * s), Limestone, "PlinthStep");
        step0.transform.rotation = rot;
        var step1 = Slab(parent, foot + Vector3.up * (0.6f * s), new Vector3(2.8f * s, 0.4f * s, 2.8f * s), Limestone, "PlinthStep");
        step1.transform.rotation = rot;
        var block = Slab(parent, foot + Vector3.up * (1.5f * s), new Vector3(1.8f * s, 1.4f * s, 1.8f * s), Marble, "Plinth");
        block.transform.rotation = rot;
        Decor(parent, PrimitiveType.Cube, foot + Vector3.up * (2.26f * s), rot, new Vector3(2f * s, 0.12f * s, 2f * s), Brass, "PlinthCap");
        Text(parent, foot + rot * new Vector3(0f, 1.3f * s, -0.92f * s), rot * Quaternion.Euler(0f, 180f, 0f), Company, 0.03f * s, new Color(0.78f, 0.58f, 0.25f), "PlinthName", 1.5f * s, true, 25f, 60f);
        // The figure: legs, a coat, shoulders, a head, one arm up with the torch
        Vector3 f = foot + Vector3.up * (2.32f * s);
        Solid(parent, PrimitiveType.Cube, f + Vector3.up * (0.55f * s), rot, new Vector3(0.9f * s, 1.1f * s, 0.6f * s), Bronze, "StatueLegs");
        Solid(parent, PrimitiveType.Cube, f + Vector3.up * (1.5f * s), rot, new Vector3(1.1f * s, 0.9f * s, 0.7f * s), Bronze, "StatueCoat");
        Decor(parent, PrimitiveType.Cube, f + Vector3.up * (2.12f * s), rot, new Vector3(1.3f * s, 0.36f * s, 0.6f * s), Bronze, "StatueShoulders");
        Decor(parent, PrimitiveType.Cube, f + Vector3.up * (2.62f * s), rot, new Vector3(0.44f * s, 0.6f * s, 0.44f * s), Bronze, "StatueHead");
        Decor(parent, PrimitiveType.Cube, f + rot * new Vector3(0.75f * s, 2.75f * s, 0.1f * s), rot * Quaternion.Euler(0f, 0f, 12f), new Vector3(0.26f * s, 1.3f * s, 0.26f * s), Bronze, "StatueArm");
        Decor(parent, PrimitiveType.Cube, f + rot * new Vector3(0.86f * s, 3.5f * s, 0.1f * s), rot, new Vector3(0.36f * s, 0.3f * s, 0.36f * s), Brass, "StatueTorch");
        Decor(parent, PrimitiveType.Sphere, f + rot * new Vector3(0.86f * s, 3.78f * s, 0.1f * s), rot, Vector3.one * (0.34f * s), LampCream, "TorchFlame");
        var l = Light(parent, f + rot * new Vector3(0.86f * s, 3.9f * s, 0.1f * s), 9f * s, 1.4f);
        l.color = new Color(1f, 0.82f, 0.55f);
    }

    /// A searchlight tower: a lattice of four iron legs to a platform with a rail, the lamp on a yoke sweeping the sky
    /// with a real beam and a haze sheet along it. The head turns (SearchlightSweep). Looks only.
    public static void SearchlightTower(Transform parent, Vector3 foot, float height, float phase = 0f)
    {
        EnsureSurface();
        float spread = Mathf.Max(1.2f, height * 0.18f);
        Vector3 top = foot + Vector3.up * height;
        for (int i = 0; i < 4; i++)
        {
            Quaternion q = Quaternion.Euler(0f, 45f + i * 90f, 0f);
            Vector3 a = foot + q * Vector3.forward * spread, b = top + q * Vector3.forward * (spread * 0.45f);
            Vector3 d = b - a;
            Decor(parent, PrimitiveType.Cube, (a + b) * 0.5f, Quaternion.FromToRotation(Vector3.up, d.normalized), new Vector3(0.14f, d.magnitude, 0.14f), Iron, "TowerLeg");
        }
        int bars = Mathf.Max(2, Mathf.RoundToInt(height / 2.5f));
        for (int k = 1; k < bars; k++)
        {
            float t = k / (float)bars, w = Mathf.Lerp(spread, spread * 0.45f, t) * 1.42f;
            for (int i = 0; i < 4; i++)
            {
                Quaternion q = Quaternion.Euler(0f, i * 90f, 0f);
                Decor(parent, PrimitiveType.Cube, foot + Vector3.up * (height * t) + q * Vector3.forward * (w * 0.5f), q, new Vector3(w, 0.06f, 0.06f), Iron, "TowerBar");
            }
        }
        Decor(parent, PrimitiveType.Cube, top + Vector3.up * 0.1f, Quaternion.identity, new Vector3(spread * 1.5f, 0.2f, spread * 1.5f), Iron, "TowerDeck");
        for (int i = 0; i < 4; i++)
        {
            Quaternion q = Quaternion.Euler(0f, i * 90f, 0f);
            Decor(parent, PrimitiveType.Cube, top + Vector3.up * 1.1f + q * Vector3.forward * (spread * 0.75f), q, new Vector3(spread * 1.5f, 0.05f, 0.05f), Brass, "TowerRail");
            Decor(parent, PrimitiveType.Cube, top + Vector3.up * 0.6f + q * Vector3.forward * (spread * 0.75f), q, new Vector3(spread * 1.5f, 0.04f, 0.04f), Iron, "TowerRail");
        }
        // The head: a yoke on a post, the drum, its lens, the beam; it sweeps on its own
        var head = new GameObject("SearchlightHead");
        head.transform.SetParent(parent);
        head.transform.position = top + Vector3.up * 1.6f;
        Decor(parent, PrimitiveType.Cylinder, top + Vector3.up * 0.7f, Quaternion.identity, new Vector3(0.3f, 0.5f, 0.3f), Iron, "SearchlightPost");
        Decor(head.transform, PrimitiveType.Cube, head.transform.position - Vector3.up * 0.25f, Quaternion.identity, new Vector3(1.6f, 0.12f, 0.12f), Iron, "Yoke");
        Quaternion tilt = Quaternion.Euler(-32f, 0f, 0f);
        Quaternion drum = tilt * Quaternion.Euler(90f, 0f, 0f);
        Decor(head.transform, PrimitiveType.Cylinder, head.transform.position, drum, new Vector3(1.3f, 0.45f, 1.3f), RivetIron, "SearchlightDrum");
        Decor(head.transform, PrimitiveType.Cylinder, head.transform.position + tilt * Vector3.forward * 0.47f, drum, new Vector3(1.2f, 0.02f, 1.2f), lampOnMat, "SearchlightLens");
        Decor(head.transform, PrimitiveType.Cylinder, head.transform.position + tilt * Vector3.forward * 0.49f, drum, new Vector3(1.36f, 0.04f, 1.36f), Brass, "SearchlightRim");
        Prim(head.transform, head.transform.position + tilt * Vector3.forward * 60f, new Vector3(1.2f, 1.2f, 120f), beamMat, "SearchlightBeam", PrimitiveType.Cube, tilt);
        var l = Light(head.transform, head.transform.position + tilt * Vector3.forward * 0.6f, 90f, 2.4f);
        l.color = new Color(0.9f, 0.95f, 1f);
        l.type = LightType.Spot;
        l.spotAngle = 9f;
        l.innerSpotAngle = 4f;
        l.transform.rotation = tilt;
        var sweep = head.AddComponent<SearchlightSweep>();
        sweep.phase = phase;
    }
}
