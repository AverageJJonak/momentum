using UnityEngine;

/// <summary>
/// The Works set, DARK DECO — the 1930s iron vocabulary THE UNDERGROUND (act2-underground) still draws on: iron rails
/// on timber sleepers (every track), iron sets with lamp strips (the tunnels), arc lamps on yokes, barriers striped
/// black and mustard, gas lamps and brick piers and arches (the sewers), strata bands and lit rings for shafts, black
/// water with brass valves and pumps (the cisterns), crate stacks as luggage, rubble for the foundations. The colliery
/// pieces (ore carts, crushers, conveyors, drill rigs, the headframe, the cage, hoppers, winches) stay here unused by
/// Act II, for whatever asks for them. Decor has no colliders; the things meant to be run over (carts, barriers, crate
/// stacks, conveyor decks) are solid and say so. Colour: amber is a lamp's own light, red is danger, nothing here is cyan.
/// </summary>
public static partial class LevelKit
{
    static Material rockMat, oreMat, floodMat, girderMat, railMat, tarpMat, earthMat, sleeperMat, bulbMat, blackWaterMat, holoMat, mustardMat, sootMat, leatherMat;

    static void EnsureWorks()
    {
        if (rockMat != null) return;
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        rockMat = Lit(lit, new Color(0.3f, 0.27f, 0.24f), 0.08f);
        earthMat = Lit(lit, new Color(0.24f, 0.2f, 0.16f), 0.05f);
        oreMat = Unlit(unlit, new Color(1.5f, 0.95f, 0.4f));       // the seam glows amber: a lamp's colour, never the Joker's green
        floodMat = Unlit(unlit, new Color(2.2f, 1.95f, 1.5f));     // an arc lamp's cream face
        bulbMat = Unlit(unlit, new Color(2.2f, 1.7f, 1.1f));       // warm white
        girderMat = Lit(lit, new Color(0.24f, 0.18f, 0.13f), 0.35f); girderMat.SetFloat("_Metallic", 0.6f); // boiler iron, bronze-brown, weathered
        railMat = Lit(lit, new Color(0.3f, 0.3f, 0.32f), 0.7f); railMat.SetFloat("_Metallic", 0.9f);
        sleeperMat = Lit(lit, new Color(0.2f, 0.16f, 0.12f), 0.1f);
        tarpMat = Lit(lit, new Color(0.22f, 0.19f, 0.15f), 0.2f);  // oiled canvas
        blackWaterMat = WaterSurface(new Color(0.02f, 0.02f, 0.025f, 0.86f), 0.9f, 3f, 0.5f);   // black water, like the kit's own: the only colour in it is the lamps' amber, never a teal
        holoMat = Transparent(new Color(0.95f, 0.75f, 0.45f, 0.2f));   // amber glass, where a panel used to be a hologram
        mustardMat = Lit(lit, new Color(0.7f, 0.55f, 0.2f), 0.4f);
        sootMat = Lit(lit, new Color(0.06f, 0.06f, 0.07f), 0.25f);
        leatherMat = Lit(lit, new Color(0.2f, 0.12f, 0.08f), 0.35f);
    }

    public static Material Rock { get { EnsureWorks(); return rockMat; } }
    public static Material Earth { get { EnsureWorks(); return earthMat; } }
    public static Material Ore { get { EnsureWorks(); return oreMat; } }
    public static Material Girder { get { EnsureWorks(); return girderMat; } }
    /// The hazard pair: mustard paint and soot black
    public static Material MustardPaint { get { EnsureWorks(); return mustardMat; } }
    public static Material Soot { get { EnsureWorks(); return sootMat; } }

    // ------------------------------------------------------------ the 1930s vocabulary

    /// Hazard stripes, black and mustard, along the longest flat axis of `size` (decor, welded into two meshes)
    public static void Stripes(Transform parent, Vector3 centre, Vector3 size, float yaw = 0f)
    {
        EnsureWorks();
        var b = new MeshBaker();
        Stripes(b, centre, size, Quaternion.Euler(0f, yaw, 0f));
        b.Flush(parent, "Stripes");
    }

    /// The same, into a baker that is someone else's
    static void Stripes(MeshBaker b, Vector3 centre, Vector3 size, Quaternion? rot = null)
    {
        EnsureWorks();
        Quaternion r = rot ?? Quaternion.identity;
        bool alongX = size.x >= size.z;
        float len = alongX ? size.x : size.z;
        int n = Mathf.Max(2, Mathf.RoundToInt(len / 0.5f));
        float seg = len / n;
        for (int i = 0; i < n; i++)
        {
            float t = -len * 0.5f + seg * (i + 0.5f);
            Vector3 local = alongX ? new Vector3(t, 0f, 0f) : new Vector3(0f, 0f, t);
            Vector3 s = alongX ? new Vector3(seg + 0.005f, size.y, size.z) : new Vector3(size.x, size.y, seg + 0.005f);
            b.Add(i % 2 == 0 ? mustardMat : sootMat, centre + r * local, s, r);
        }
    }

    /// A striped band round a drum or a stack: mustard between two rings of soot (decor). `rot` turns the cylinder's axis.
    public static void Band(Transform parent, Vector3 centre, Quaternion rot, float dia, float thick)
    {
        EnsureWorks();
        Decor(parent, PrimitiveType.Cylinder, centre, rot, new Vector3(dia, thick * 0.5f, dia), mustardMat, "Band");
        Decor(parent, PrimitiveType.Cylinder, centre + rot * Vector3.up * (thick * 0.5f + 0.03f), rot, new Vector3(dia + 0.01f, 0.03f, dia + 0.01f), sootMat, "BandRing");
        Decor(parent, PrimitiveType.Cylinder, centre - rot * Vector3.up * (thick * 0.5f + 0.03f), rot, new Vector3(dia + 0.01f, 0.03f, dia + 0.01f), sootMat, "BandRing");
    }

    /// A brick pier standing off a wall: a stone plinth, the brick shaft, a stepped stone cap; shallow enough for a
    /// wall-run to pass over (decor). `facing` points into the room.
    public static void BrickPier(Transform parent, Vector3 wallBase, Vector3 facing, float height, float width = 1.4f)
    {
        Vector3 n = facing.normalized;
        Quaternion rot = Quaternion.LookRotation(n, Vector3.up);
        Decor(parent, PrimitiveType.Cube, wallBase + n * 0.19f + Vector3.up * 0.2f, rot, new Vector3(width + 0.3f, 0.4f, 0.38f), Limestone, "PierPlinth");
        Decor(parent, PrimitiveType.Cube, wallBase + n * 0.15f + Vector3.up * (height * 0.5f), rot, new Vector3(width, height - 0.9f, 0.3f), Brick, "Pier");
        Decor(parent, PrimitiveType.Cube, wallBase + n * 0.17f + Vector3.up * (height - 0.42f), rot, new Vector3(width + 0.16f, 0.16f, 0.34f), Limestone, "PierCap");
        Decor(parent, PrimitiveType.Cube, wallBase + n * 0.2f + Vector3.up * (height - 0.26f), rot, new Vector3(width + 0.34f, 0.16f, 0.4f), Limestone, "PierCap");
    }

    /// A brick arch between two piers' tops, in voussoirs, with a stone keystone (decor). `a` and `b` are the springing
    /// points on the wall; `rise` is how far the crown stands over them.
    public static void BrickArch(Transform parent, Vector3 a, Vector3 b, Vector3 facing, float rise)
    {
        Vector3 n = facing.normalized;
        Vector3 d = b - a;
        float span = d.magnitude;
        if (span < 2f) return;
        Vector3 along = d / span;
        Vector3 mid = (a + b) * 0.5f;
        const int segs = 9;
        for (int i = 0; i < segs; i++)
        {
            float t0 = i / (float)segs, t1 = (i + 1) / (float)segs;
            Vector3 p0 = Arc(mid, along, span * 0.5f, rise, t0), p1 = Arc(mid, along, span * 0.5f, rise, t1);
            Vector3 c = (p0 + p1) * 0.5f + n * 0.15f;
            Vector3 dir = (p1 - p0).normalized;
            // The box's z looks into the room, its y is the arc's normal, so its x runs along the arc
            Decor(parent, PrimitiveType.Cube, c, Quaternion.LookRotation(n, Vector3.Cross(dir, n)), new Vector3(Vector3.Distance(p0, p1) + 0.06f, 0.5f, 0.3f), Brick, "Voussoir");
        }
        Decor(parent, PrimitiveType.Cube, mid + Vector3.up * rise + n * 0.19f, Quaternion.LookRotation(n, Vector3.up), new Vector3(0.44f, 0.62f, 0.38f), Limestone, "Keystone");
    }

    /// A point on a half-ellipse over `mid`: `t` 0..1 from one springing to the other
    static Vector3 Arc(Vector3 mid, Vector3 along, float half, float rise, float t)
    {
        float ang = Mathf.PI * (1f - t);
        return mid + along * (Mathf.Cos(ang) * half) + Vector3.up * (Mathf.Sin(ang) * rise);
    }

    /// A gas lamp on an iron bracket off a wall: the bracket and its brace, an iron lantern with amber glass, and a
    /// warm light that flickers (decor + light). `facing` points into the room.
    public static void GasLamp(Transform parent, Vector3 wallPoint, Vector3 facing, float scale = 1f)
    {
        EnsureWorks();
        Vector3 n = facing.normalized;
        Quaternion rot = Quaternion.LookRotation(n, Vector3.up);
        float s = scale;
        Decor(parent, PrimitiveType.Cube, wallPoint + n * (0.26f * s), rot, new Vector3(0.05f, 0.05f, 0.52f * s), Iron, "LampBracket");
        Decor(parent, PrimitiveType.Cube, wallPoint + n * (0.16f * s) - Vector3.up * (0.16f * s), rot * Quaternion.Euler(-45f, 0f, 0f), new Vector3(0.04f, 0.04f, 0.45f * s), Iron, "LampBrace");
        Decor(parent, PrimitiveType.Cube, wallPoint + n * (0.02f * s), rot, new Vector3(0.12f, 0.3f * s, 0.03f), Iron, "LampPlate");
        Vector3 lantern = wallPoint + n * (0.48f * s) - Vector3.up * (0.24f * s);
        Decor(parent, PrimitiveType.Cube, lantern, rot, new Vector3(0.22f * s, 0.34f * s, 0.22f * s), WindowAmber, "LampGlass");
        for (int k = 0; k < 4; k++)
        {
            Quaternion e = rot * Quaternion.Euler(0f, k * 90f + 45f, 0f);
            Decor(parent, PrimitiveType.Cube, lantern + e * Vector3.forward * (0.155f * s), rot, new Vector3(0.03f, 0.36f * s, 0.03f), Iron, "LampBar");
        }
        Decor(parent, PrimitiveType.Cube, lantern + Vector3.up * (0.2f * s), rot, new Vector3(0.3f * s, 0.05f, 0.3f * s), Iron, "LampCap");
        Decor(parent, PrimitiveType.Cube, lantern + Vector3.up * (0.26f * s), rot, new Vector3(0.14f * s, 0.08f, 0.14f * s), Iron, "LampVent");
        var l = Light(parent, lantern + n * 0.15f - Vector3.up * 0.1f, 8f * s, 1.4f);
        l.color = new Color(1f, 0.68f, 0.32f);
    }

    // ------------------------------------------------------------ the set

    /// An iron set across the gallery at `z`: uprights with a lamp strip up each, a crossbeam with a striped band (decor)
    public static void PortalFrame(Transform parent, float z, float halfW, float y, float height)
    {
        EnsureWorks();
        float w = 0.5f;
        float xw = -halfW + w * 0.5f + 0.06f, xe = halfW - w * 0.5f - 0.06f;
        Decor(parent, PrimitiveType.Cube, new Vector3(xw, y + height * 0.5f, z), Quaternion.identity, new Vector3(w, height, w), girderMat, "SetLegW");
        Decor(parent, PrimitiveType.Cube, new Vector3(xe, y + height * 0.5f, z), Quaternion.identity, new Vector3(w, height, w), girderMat, "SetLegE");
        Decor(parent, PrimitiveType.Cube, new Vector3(0f, y + height - w * 0.5f - 0.06f, z), Quaternion.identity, new Vector3(halfW * 2f - 0.12f, w, w), girderMat, "SetBeam");
        Decor(parent, PrimitiveType.Cube, new Vector3(xw + w * 0.5f + 0.02f, y + height * 0.5f, z), Quaternion.identity, new Vector3(0.03f, height - 0.6f, 0.12f), bulbMat, "SetSpineW");
        Decor(parent, PrimitiveType.Cube, new Vector3(xe - w * 0.5f - 0.02f, y + height * 0.5f, z), Quaternion.identity, new Vector3(0.03f, height - 0.6f, 0.12f), bulbMat, "SetSpineE");
        Stripes(parent, new Vector3(0f, y + height - w - 0.09f, z), new Vector3(halfW * 2f - 1f, 0.05f, 0.3f));
        for (int s = -1; s <= 1; s += 2)   // a rivet row down each leg's face
            for (float r = y + 0.6f; r < y + height - 0.8f; r += 0.7f)
                Decor(parent, PrimitiveType.Cube, new Vector3(s > 0 ? xe - w * 0.5f - 0.015f : xw + w * 0.5f + 0.015f, r, z + 0.18f), Quaternion.identity, new Vector3(0.03f, 0.06f, 0.06f), railMat, "Rivet");
    }

    /// An arc lamp on a yoke: an iron drum housing aimed along `aim` with a cream face, and a real warm light with range (decor + light)
    public static void Floodlight(Transform parent, Vector3 pos, Vector3 aim, float range = 16f, float intensity = 2.4f)
    {
        EnsureWorks();
        Vector3 dir = aim.sqrMagnitude > 0.001f ? aim.normalized : Vector3.down;
        Quaternion rot = Quaternion.LookRotation(dir, Mathf.Abs(dir.y) > 0.95f ? Vector3.forward : Vector3.up);
        Quaternion drum = rot * Quaternion.Euler(90f, 0f, 0f);
        Decor(parent, PrimitiveType.Cylinder, pos, drum, new Vector3(0.62f, 0.22f, 0.62f), Iron, "FloodHousing");
        Decor(parent, PrimitiveType.Cylinder, pos + rot * new Vector3(0f, 0f, 0.23f), drum, new Vector3(0.54f, 0.01f, 0.54f), floodMat, "FloodFace");
        Decor(parent, PrimitiveType.Cylinder, pos + rot * new Vector3(0f, 0f, 0.25f), drum, new Vector3(0.66f, 0.03f, 0.66f), Brass, "FloodRim");
        Decor(parent, PrimitiveType.Cube, pos + rot * new Vector3(0f, -0.36f, -0.1f), rot, new Vector3(0.12f, 0.3f, 0.12f), Iron, "FloodYoke");
        var l = Light(parent, pos + dir * 0.5f, range, intensity * 1.5f);
        l.color = new Color(1f, 0.9f, 0.72f);
        // A real lamp: a cone along the aim that throws the shadow of everything that crosses it
        l.type = LightType.Spot;
        l.transform.rotation = rot;
        l.spotAngle = 75f;
        l.innerSpotAngle = 40f;
        l.shadows = LightShadows.Soft;
        l.shadowStrength = 0.85f;
        l.shadowBias = 0.03f;
        l.shadowNormalBias = 0.4f;
    }

    /// A light bar between two points: an iron trough on two hangers with a cream lamp strip in it, lit (decor + light)
    public static void LightBar(Transform parent, Vector3 from, Vector3 to)
    {
        EnsureWorks();
        Vector3 d = to - from;
        float len = d.magnitude;
        if (len < 0.5f) return;
        Quaternion rot = Quaternion.LookRotation(d / len);
        Vector3 mid = (from + to) * 0.5f + Vector3.down * 0.6f;
        Decor(parent, PrimitiveType.Cube, mid + Vector3.up * 0.05f, rot, new Vector3(0.18f, 0.06f, len), Iron, "BarBack");
        Decor(parent, PrimitiveType.Cube, mid, rot, new Vector3(0.14f, 0.03f, len - 0.1f), bulbMat, "BarFace");
        Cable(parent, from, from + d * 0.15f + Vector3.down * 0.6f, 0.04f);
        Cable(parent, to, to - d * 0.15f + Vector3.down * 0.6f, 0.04f);
        var l = Light(parent, mid + Vector3.down * 0.3f, Mathf.Min(14f, len * 1.4f), 1.6f);
        l.color = new Color(1f, 0.88f, 0.7f);
    }

    /// A rail: two iron rails on timber sleepers over a bed of earth (decor, welded)
    public static void Rail(Transform parent, Vector3 from, Vector3 to)
    {
        EnsureWorks();
        Vector3 d = to - from;
        float len = d.magnitude;
        if (len < 1f) return;
        Quaternion rot = Quaternion.LookRotation(d / len);
        Vector3 mid = (from + to) * 0.5f;
        Vector3 side = rot * Vector3.right;
        var b = new MeshBaker();
        b.Add(earthMat, mid + Vector3.up * 0.04f, new Vector3(2.2f, 0.08f, len), rot);
        int sleepers = Mathf.Max(2, Mathf.RoundToInt(len / 0.9f));
        for (int i = 0; i < sleepers; i++)
            b.Add(sleeperMat, from + d * ((i + 0.5f) / sleepers) + Vector3.up * 0.13f, new Vector3(2f, 0.1f, 0.24f), rot);
        b.Add(railMat, mid + side * 0.75f + Vector3.up * 0.25f, new Vector3(0.08f, 0.14f, len), rot);
        b.Add(railMat, mid - side * 0.75f + Vector3.up * 0.25f, new Vector3(0.08f, 0.14f, len), rot);
        b.Add(railMat, mid + side * 0.75f + Vector3.up * 0.19f, new Vector3(0.16f, 0.02f, len), rot);   // the foot of each rail
        b.Add(railMat, mid - side * 0.75f + Vector3.up * 0.19f, new Vector3(0.16f, 0.02f, len), rot);
        b.Flush(parent, "Rail");
    }

    /// An ore cart: a solid riveted iron hopper on four iron wheels, an amber lamp at the front — a heap of ore in it when full
    public static void OreCart(Transform parent, Vector3 groundPos, float yaw, bool full)
    {
        EnsureWorks();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Solid(parent, PrimitiveType.Cube, groundPos + Vector3.up * 1.05f, rot, new Vector3(1.7f, 1.1f, 2.6f), RivetIron, "OreCart");
        Decor(parent, PrimitiveType.Cube, groundPos + Vector3.up * 1.05f, rot, new Vector3(1.76f, 0.2f, 2.66f), Iron, "CartBand");
        Decor(parent, PrimitiveType.Cube, groundPos + Vector3.up * 1.56f, rot, new Vector3(1.78f, 0.08f, 2.68f), Iron, "CartLip");
        Decor(parent, PrimitiveType.Cube, groundPos + Vector3.up * 0.42f, rot, new Vector3(1.2f, 0.16f, 2.3f), Iron, "CartFrame");
        Quaternion axle = rot * Quaternion.Euler(0f, 0f, 90f);
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Decor(parent, PrimitiveType.Cylinder, groundPos + rot * new Vector3(sx * 0.82f, 0.35f, sz * 0.9f), axle, new Vector3(0.7f, 0.08f, 0.7f), railMat, "CartWheel");
        Decor(parent, PrimitiveType.Cylinder, groundPos + rot * new Vector3(0f, 0.35f, 0.9f), axle, new Vector3(0.08f, 0.85f, 0.08f), railMat, "CartAxle");
        Decor(parent, PrimitiveType.Cylinder, groundPos + rot * new Vector3(0f, 0.35f, -0.9f), axle, new Vector3(0.08f, 0.85f, 0.08f), railMat, "CartAxle");
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0.6f, 1.25f, 1.32f), rot, new Vector3(0.22f, 0.22f, 0.06f), Amber, "CartLamp");
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(-0.5f, 1.2f, 1.32f), rot, new Vector3(0.5f, 0.3f, 0.02f), Cream, "CartPlate");
        var l = Light(parent, groundPos + rot * new Vector3(0.6f, 1.25f, 1.6f), 4f, 1f);
        l.color = new Color(1f, 0.72f, 0.35f);
        if (!full) return;
        Decor(parent, PrimitiveType.Sphere, groundPos + Vector3.up * 1.55f, rot, new Vector3(1.5f, 0.6f, 2.3f), rockMat, "OreHeap");
        for (int i = 0; i < 3; i++)
            Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(-0.4f + i * 0.4f, 1.74f, -0.5f + i * 0.5f), rot * Quaternion.Euler(20f * i, 35f * i, 15f), Vector3.one * 0.28f, oreMat, "OreLump");
    }

    /// A vein of ore in a wall: three tilted seams glowing amber out of the rock (decor)
    public static void OreVein(Transform parent, Vector3 wallPoint, Vector3 facing, float length)
    {
        EnsureWorks();
        Vector3 n = facing.normalized;
        Vector3 along = Vector3.Cross(Vector3.up, n).normalized;
        for (int i = 0; i < 3; i++)
        {
            float t = (i - 1) * 0.3f;
            Vector3 c = wallPoint + n * 0.06f + along * (t * length) + Vector3.up * (t * length * 0.35f);
            Quaternion rot = Quaternion.LookRotation(n) * Quaternion.Euler(0f, 0f, 22f + i * 14f);
            Decor(parent, PrimitiveType.Cube, c, rot, new Vector3(length * (0.45f - 0.08f * i), 0.12f, 0.06f), oreMat, "OreSeam");
        }
    }

    /// A spill of rubble on the floor (decor)
    public static void Rubble(Transform parent, Vector3 center, float radius, int count = 7)
    {
        EnsureWorks();
        var rng = new System.Random(Mathf.RoundToInt(center.x * 13f + center.z * 7f));
        for (int i = 0; i < count; i++)
        {
            float a = (float)rng.NextDouble() * Mathf.PI * 2f, r = radius * (float)rng.NextDouble();
            float s = 0.25f + 0.45f * (float)rng.NextDouble();
            Vector3 p = center + new Vector3(Mathf.Cos(a) * r, s * 0.35f, Mathf.Sin(a) * r);
            Quaternion rot = Quaternion.Euler((float)rng.NextDouble() * 40f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 30f);
            Decor(parent, PrimitiveType.Cube, p, rot, new Vector3(s, s * 0.7f, s * 0.85f), i % 3 == 0 ? earthMat : rockMat, "Rubble");
        }
    }

    /// A barrier: a solid low iron slab striped black and mustard, an amber lamp on each post
    public static void Barrier(Transform parent, Vector3 groundCenter, float width, float yaw)
    {
        EnsureWorks();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Solid(parent, PrimitiveType.Cube, groundCenter + Vector3.up * 0.55f, rot, new Vector3(width, 1.1f, 0.35f), Iron, "Barrier");
        Stripes(parent, groundCenter + Vector3.up * 0.7f, new Vector3(width - 0.2f, 0.3f, 0.41f), yaw);
        for (int sx = -1; sx <= 1; sx += 2)
        {
            Vector3 post = groundCenter + rot * new Vector3(sx * (width * 0.5f - 0.15f), 0.55f, 0f);
            Decor(parent, PrimitiveType.Cube, post, rot, new Vector3(0.2f, 1.2f, 0.45f), Iron, "BarrierPost");
            Decor(parent, PrimitiveType.Cube, post + Vector3.up * 0.66f, rot, new Vector3(0.22f, 0.06f, 0.47f), Amber, "BarrierLamp");
        }
    }

    /// The headframe: two leaning iron legs, braces and ties, and the sheave wheel over the shaft mouth (decor)
    public static void Headframe(Transform parent, Vector3 groundCenter, float height)
    {
        EnsureWorks();
        float spread = height * 0.28f;
        for (int sx = -1; sx <= 1; sx += 2)
        {
            Vector3 foot = groundCenter + new Vector3(sx * spread, 0f, 0f);
            Vector3 top = groundCenter + new Vector3(sx * 0.7f, height, 0f);
            Vector3 d = top - foot;
            Decor(parent, PrimitiveType.Cube, (foot + top) * 0.5f, Quaternion.FromToRotation(Vector3.up, d.normalized), new Vector3(0.6f, d.magnitude, 0.6f), girderMat, "HeadframeLeg");
            Vector3 bfoot = groundCenter + new Vector3(sx * spread * 0.5f, 0f, 2.2f);
            Vector3 btop = groundCenter + new Vector3(sx * 0.5f, height * 0.7f, 0.3f);
            Vector3 bd = btop - bfoot;
            Decor(parent, PrimitiveType.Cube, (bfoot + btop) * 0.5f, Quaternion.FromToRotation(Vector3.up, bd.normalized), new Vector3(0.3f, bd.magnitude, 0.3f), Iron, "HeadframeBrace");
        }
        for (int i = 1; i <= 3; i++)
        {
            float t = i / 4f;
            Decor(parent, PrimitiveType.Cube, groundCenter + Vector3.up * (height * t), Quaternion.identity, new Vector3(spread * 2f * (1f - t) + 1.4f, 0.3f, 0.3f), Iron, "HeadframeTie");
        }
        Vector3 hub = groundCenter + Vector3.up * (height + 0.9f);
        Decor(parent, PrimitiveType.Cylinder, hub, Quaternion.Euler(0f, 0f, 90f), new Vector3(2.6f, 0.2f, 2.6f), railMat, "Sheave");
        Decor(parent, PrimitiveType.Cylinder, hub, Quaternion.Euler(0f, 0f, 90f), new Vector3(0.5f, 0.6f, 0.5f), Iron, "SheaveHub");
        Decor(parent, PrimitiveType.Cube, hub + Vector3.down * 0.9f, Quaternion.identity, new Vector3(1.4f, 2.2f, 0.4f), girderMat, "SheaveMount");
        Decor(parent, PrimitiveType.Sphere, hub + Vector3.up * 1.7f, Quaternion.identity, Vector3.one * 0.3f, dotMat, "HeadframeLamp");   // red: the top of the frame, for the aircraft
        Cable(parent, hub + Vector3.up * 1.3f, groundCenter + Vector3.up * 0.5f, 0.08f);
    }

    /// A winch: a drum on two iron frames with a striped band, on a base, an amber lamp (decor)
    public static void Winch(Transform parent, Vector3 groundPos, float yaw)
    {
        EnsureWorks();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Decor(parent, PrimitiveType.Cube, groundPos + Vector3.up * 0.6f, rot, new Vector3(1.6f, 1.2f, 0.3f), Iron, "WinchFrameA");
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0f, 0.6f, 1.2f), rot, new Vector3(1.6f, 1.2f, 0.3f), Iron, "WinchFrameB");
        Decor(parent, PrimitiveType.Cylinder, groundPos + rot * new Vector3(0f, 1.05f, 0.6f), rot * Quaternion.Euler(90f, 0f, 0f), new Vector3(0.9f, 0.5f, 0.9f), girderMat, "WinchDrum");
        Band(parent, groundPos + rot * new Vector3(0f, 1.05f, 0.6f), rot * Quaternion.Euler(90f, 0f, 0f), 0.94f, 0.2f);
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0f, 0.15f, 0.6f), rot, new Vector3(1.8f, 0.3f, 1.6f), Iron, "WinchBase");
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0.7f, 1.4f, 0.6f), rot, new Vector3(0.24f, 0.08f, 0.24f), Amber, "WinchLamp");
        Decor(parent, PrimitiveType.Cylinder, groundPos + rot * new Vector3(-0.9f, 1.05f, 0.6f), rot * Quaternion.Euler(0f, 0f, 90f), new Vector3(0.5f, 0.03f, 0.5f), Brass, "WinchWheel");
    }

    /// A conveyor deck between two ground points: a leather belt, solid to run along, iron rollers and legs under it,
    /// iron side rails with a brass edge
    public static void Conveyor(Transform parent, Vector3 from, Vector3 to, float width = 3f)
    {
        EnsureWorks();
        Vector3 d = to - from;
        float len = d.magnitude;
        if (len < 2f) return;
        Quaternion rot = Quaternion.LookRotation(d / len);
        Vector3 mid = (from + to) * 0.5f;
        Vector3 side = rot * Vector3.right;
        Solid(parent, PrimitiveType.Cube, mid + Vector3.up * 0.9f, rot, new Vector3(width, 0.3f, len), leatherMat, "ConveyorBelt");
        Decor(parent, PrimitiveType.Cube, mid + Vector3.up * 0.55f, rot, new Vector3(width - 0.6f, 0.4f, len - 0.4f), Iron, "ConveyorBed");
        Decor(parent, PrimitiveType.Cube, mid + side * (width * 0.5f + 0.07f) + Vector3.up * 1.15f, rot, new Vector3(0.1f, 0.5f, len), girderMat, "ConveyorRailW");
        Decor(parent, PrimitiveType.Cube, mid - side * (width * 0.5f + 0.07f) + Vector3.up * 1.15f, rot, new Vector3(0.1f, 0.5f, len), girderMat, "ConveyorRailE");
        Decor(parent, PrimitiveType.Cube, mid + side * (width * 0.5f + 0.07f) + Vector3.up * 1.42f, rot, new Vector3(0.14f, 0.05f, len - 0.3f), Brass, "ConveyorEdgeW");
        Decor(parent, PrimitiveType.Cube, mid - side * (width * 0.5f + 0.07f) + Vector3.up * 1.42f, rot, new Vector3(0.14f, 0.05f, len - 0.3f), Brass, "ConveyorEdgeE");
        int legs = Mathf.Max(2, Mathf.RoundToInt(len / 4f));
        for (int i = 0; i < legs; i++)
        {
            Vector3 p = from + d * ((i + 0.5f) / legs);
            Decor(parent, PrimitiveType.Cube, p + side * (width * 0.4f) + Vector3.up * 0.35f, rot, new Vector3(0.16f, 0.7f, 0.16f), Iron, "ConveyorLeg");
            Decor(parent, PrimitiveType.Cube, p - side * (width * 0.4f) + Vector3.up * 0.35f, rot, new Vector3(0.16f, 0.7f, 0.16f), Iron, "ConveyorLeg");
            Decor(parent, PrimitiveType.Cylinder, p + Vector3.up * 0.72f, rot * Quaternion.Euler(0f, 0f, 90f), new Vector3(0.3f, width * 0.5f - 0.2f, 0.3f), railMat, "Roller");
        }
    }

    /// The crusher: a huge toothed drum on an axle across the hall, a striped hood over it, an iron grate under it (decor, overhead).
    /// The drum and its teeth hang off the returned pivot: spin it about X and the drum turns
    public static Transform Crusher(Transform parent, Vector3 center, float length, float diameter)
    {
        EnsureWorks();
        Quaternion axle = Quaternion.Euler(0f, 0f, 90f); // the drum lies along X
        var pivot = new GameObject("CrusherPivot").transform;
        pivot.SetParent(parent);
        pivot.position = center;
        Decor(pivot, PrimitiveType.Cylinder, center, axle, new Vector3(diameter, length * 0.5f, diameter), RivetIron, "CrusherDrum");
        Decor(parent, PrimitiveType.Cylinder, center, axle, new Vector3(0.6f, length * 0.5f + 1.2f, 0.6f), railMat, "CrusherAxle");
        const int teeth = 10;
        for (int i = 0; i < teeth; i++)
        {
            Quaternion rot = Quaternion.Euler(i * 360f / teeth, 0f, 0f);
            Decor(pivot, PrimitiveType.Cube, center + rot * new Vector3(0f, diameter * 0.5f, 0f), rot, new Vector3(length * 0.9f, 0.5f, 0.35f), girderMat, "CrusherTooth");
        }
        Decor(parent, PrimitiveType.Cube, center + Vector3.up * (diameter * 0.5f + 0.9f), Quaternion.identity, new Vector3(length + 2f, 0.4f, diameter * 0.7f), Iron, "CrusherHood");
        Stripes(parent, center + Vector3.up * (diameter * 0.5f + 0.9f) + Vector3.forward * (diameter * 0.35f + 0.01f), new Vector3(length + 2f, 0.3f, 0.02f));
        Stripes(parent, center + Vector3.up * (diameter * 0.5f + 0.9f) - Vector3.forward * (diameter * 0.35f + 0.01f), new Vector3(length + 2f, 0.3f, 0.02f));
        Decor(parent, PrimitiveType.Cube, center + Vector3.down * (diameter * 0.5f + 0.6f), Quaternion.identity, new Vector3(length + 1f, 0.04f, diameter * 0.9f), Dark, "CrusherGrate");
        for (float g = -length * 0.5f + 0.5f; g < length * 0.5f; g += 0.6f)
            Decor(parent, PrimitiveType.Cube, center + Vector3.down * (diameter * 0.5f + 0.56f) + Vector3.right * g, Quaternion.identity, new Vector3(0.06f, 0.05f, diameter * 0.9f), Iron, "GrateBar");
        Decor(parent, PrimitiveType.Cube, center + Vector3.down * (diameter * 0.5f + 0.6f), Quaternion.identity, new Vector3(length + 1.1f, 0.03f, 0.06f), WindowAmber, "CrusherGrateEdge");
        return pivot;
    }

    /// A stack of crates under a canvas tarp: solid, to boost off
    public static void CrateStack(Transform parent, Vector3 groundPos, float yaw)
    {
        EnsureWorks();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Solid(parent, PrimitiveType.Cube, groundPos + Vector3.up * 1.1f, rot, new Vector3(2.6f, 2.2f, 2.2f), tarpMat, "CrateStack");
        Decor(parent, PrimitiveType.Cube, groundPos + Vector3.up * 0.5f, rot, new Vector3(2.7f, 0.9f, 2.3f), WoodPanel, "CrateRow");
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0f, 2.26f, 0f), rot, new Vector3(2.9f, 0.05f, 2.5f), tarpMat, "TarpTop");
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0f, 1.6f, 1.18f), rot * Quaternion.Euler(12f, 0f, 0f), new Vector3(2.9f, 1.3f, 0.05f), tarpMat, "TarpSkirt");
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0.9f, 0.5f, 1.17f), rot, new Vector3(0.5f, 0.3f, 0.02f), Cream, "CrateTag");
    }

    /// A rock rib: a rough pilaster of cut rock standing off a wall (decor). Real rock: the drifts' walls.
    public static void RockRib(Transform parent, Vector3 wallBase, Vector3 facing, float height)
    {
        EnsureWorks();
        Vector3 n = facing.normalized;
        var rng = new System.Random(Mathf.RoundToInt(wallBase.z * 3f + wallBase.x));
        int chunks = Mathf.Max(2, Mathf.RoundToInt(height / 2.2f));
        for (int i = 0; i < chunks; i++)
        {
            float y0 = height * i / chunks, y1 = height * (i + 1) / chunks;
            float depth = 0.5f + 0.5f * (float)rng.NextDouble();
            float w = 1.2f + 0.8f * (float)rng.NextDouble();
            Vector3 c = wallBase + n * (depth * 0.5f + 0.04f) + Vector3.up * ((y0 + y1) * 0.5f);
            Quaternion rot = Quaternion.LookRotation(n) * Quaternion.Euler(0f, 0f, (float)rng.NextDouble() * 8f - 4f);
            Decor(parent, PrimitiveType.Cube, c, rot, new Vector3(w, y1 - y0 + 0.1f, depth), i % 2 == 0 ? rockMat : earthMat, "RockRib");
        }
    }

    /// A drill rig: an iron tripod with a mast and a striped head, parked (decor)
    public static void DrillRig(Transform parent, Vector3 groundPos, float yaw)
    {
        EnsureWorks();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Vector3 top = groundPos + Vector3.up * 3.2f;
        for (int i = 0; i < 3; i++)
        {
            Quaternion leg = rot * Quaternion.Euler(0f, i * 120f, 0f);
            Vector3 foot = groundPos + leg * new Vector3(0f, 0f, 1.3f);
            Vector3 d = top - foot;
            Decor(parent, PrimitiveType.Cube, (foot + top) * 0.5f, Quaternion.FromToRotation(Vector3.up, d.normalized), new Vector3(0.14f, d.magnitude, 0.14f), Iron, "RigLeg");
        }
        Decor(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * 2f, Quaternion.identity, new Vector3(0.22f, 2f, 0.22f), railMat, "RigMast");
        Decor(parent, PrimitiveType.Cube, groundPos + Vector3.up * 3.35f, rot, new Vector3(0.9f, 0.5f, 0.6f), girderMat, "RigHead");
        Stripes(parent, groundPos + Vector3.up * 3.35f + rot * Vector3.forward * 0.31f, new Vector3(0.9f, 0.12f, 0.02f), yaw);
    }

    /// Strata: bands of rock, earth and the odd amber seam along a wall from `a` to `b`, between two heights (decor, stands 5 cm proud)
    public static void Strata(Transform parent, Vector3 a, Vector3 b, float y0, float y1, Vector3 facing)
    {
        EnsureWorks();
        Vector3 n = facing.normalized;
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 1f) return;
        Quaternion rot = Quaternion.LookRotation(n, Vector3.up);
        Vector3 mid = (a + b) * 0.5f + n * 0.2f;
        var rng = new System.Random(Mathf.RoundToInt(a.x * 5f + a.z * 3f + y0));
        for (float y = y0 + 1.5f; y < y1 - 1f; )
        {
            float h = 0.35f + 1.4f * (float)rng.NextDouble();
            int pick = rng.Next(7);
            Material m = pick == 0 ? oreMat : pick < 4 ? earthMat : rockMat;
            Decor(parent, PrimitiveType.Cube, new Vector3(mid.x, y + h * 0.5f, mid.z), rot, new Vector3(len - 0.4f, h, 0.3f), m, "Stratum");
            y += h + 1.2f + 2.5f * (float)rng.NextDouble();
        }
    }

    /// A ring of gas light round the inside of a shaft at height `y`: four amber strips in iron channels, a hand in from the walls (decor)
    public static void ShaftRing(Transform parent, Vector3 center, float halfX, float halfZ)
    {
        EnsureWorks();
        Decor(parent, PrimitiveType.Cube, center + new Vector3(-halfX, 0f, 0f), Quaternion.identity, new Vector3(0.1f, 0.2f, halfZ * 2f), Iron, "RingChannelW");
        Decor(parent, PrimitiveType.Cube, center + new Vector3(halfX, 0f, 0f), Quaternion.identity, new Vector3(0.1f, 0.2f, halfZ * 2f), Iron, "RingChannelE");
        Decor(parent, PrimitiveType.Cube, center + new Vector3(-halfX + 0.04f, 0f, 0f), Quaternion.identity, new Vector3(0.06f, 0.12f, halfZ * 2f - 0.2f), WindowAmber, "RingW");
        Decor(parent, PrimitiveType.Cube, center + new Vector3(halfX - 0.04f, 0f, 0f), Quaternion.identity, new Vector3(0.06f, 0.12f, halfZ * 2f - 0.2f), WindowAmber, "RingE");
        Decor(parent, PrimitiveType.Cube, center + new Vector3(0f, 0f, -halfZ + 0.04f), Quaternion.identity, new Vector3(halfX * 2f - 0.2f, 0.12f, 0.06f), WindowAmber, "RingS");
        Decor(parent, PrimitiveType.Cube, center + new Vector3(0f, 0f, halfZ - 0.04f), Quaternion.identity, new Vector3(halfX * 2f - 0.2f, 0.12f, 0.06f), WindowAmber, "RingN");
        var l = Light(parent, center, Mathf.Max(halfX, halfZ) * 2.2f, 1.4f);
        l.color = new Color(1f, 0.7f, 0.35f);
    }

    /// The cage: an open iron lift car, its roof lit cream, an enamel plate on the back, a striped gate, parked (decor)
    public static void Cage(Transform parent, Vector3 groundPos, float yaw)
    {
        EnsureWorks();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(sx * 1.3f, 1.5f, sz * 1.3f), rot, new Vector3(0.14f, 3f, 0.14f), girderMat, "CagePost");
        Decor(parent, PrimitiveType.Cube, groundPos + Vector3.up * 0.1f, rot, new Vector3(2.9f, 0.2f, 2.9f), Iron, "CageFloor");
        Decor(parent, PrimitiveType.Cube, groundPos + Vector3.up * 3.05f, rot, new Vector3(2.9f, 0.14f, 2.9f), Iron, "CageRoof");
        Decor(parent, PrimitiveType.Cube, groundPos + Vector3.up * 2.97f, rot, new Vector3(2.6f, 0.02f, 2.6f), bulbMat, "CageLamp");
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0f, 1.6f, -1.32f), rot, new Vector3(2.4f, 1.2f, 0.03f), Cream, "CagePlate");
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0f, 1.0f, -1.3f), rot, new Vector3(2.7f, 0.9f, 0.06f), Iron, "CageBack");
        Stripes(parent, groundPos + rot * new Vector3(0f, 0.6f, 1.3f), new Vector3(2.7f, 0.8f, 0.05f), yaw);
        Cable(parent, groundPos + Vector3.up * 3.15f, groundPos + Vector3.up * 9f, 0.08f);
        var l = Light(parent, groundPos + Vector3.up * 2.6f, 5f, 1.4f);
        l.color = new Color(1f, 0.88f, 0.7f);
    }

    /// A hopper: an inverted funnel of four iron plates under an amber-lit rim, hung overhead (decor)
    public static void Hopper(Transform parent, Vector3 center, float size)
    {
        EnsureWorks();
        float half = size * 0.5f;
        for (int i = 0; i < 4; i++)
        {
            Quaternion face = Quaternion.Euler(0f, i * 90f, 0f);
            Decor(parent, PrimitiveType.Cube, center + face * new Vector3(0f, 0f, half * 0.55f), face * Quaternion.Euler(-35f, 0f, 0f), new Vector3(size * 1.05f, 0.08f, size * 1.1f), RivetIron, "HopperPlate");
        }
        Decor(parent, PrimitiveType.Cube, center + Vector3.up * (half * 0.9f), Quaternion.identity, new Vector3(size * 1.1f, 0.2f, size * 1.1f), girderMat, "HopperRim");
        Decor(parent, PrimitiveType.Cube, center + Vector3.up * (half * 0.9f + 0.12f), Quaternion.identity, new Vector3(size * 1.12f, 0.04f, size * 1.12f), WindowAmber, "HopperLit");
        Decor(parent, PrimitiveType.Cube, center + Vector3.down * (half * 0.8f), Quaternion.identity, new Vector3(0.8f, 0.6f, 0.8f), Iron, "HopperThroat");
        Cable(parent, center + Vector3.up * (half * 0.9f), center + Vector3.up * (half * 0.9f + 6f), 0.06f);
    }

    /// A pump: a riveted body the size of a room, brass bands, a domed head with an amber ring, a brass dial plate, on a base (decor)
    public static void Pump(Transform parent, Vector3 groundPos, float yaw)
    {
        EnsureWorks();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Decor(parent, PrimitiveType.Cube, groundPos + Vector3.up * 0.3f, rot, new Vector3(4f, 0.6f, 4f), Iron, "PumpBase");
        Decor(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * 2.6f, Quaternion.identity, new Vector3(3f, 2f, 3f), RivetIron, "PumpBody");
        Decor(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * 2.6f, Quaternion.identity, new Vector3(3.06f, 0.08f, 3.06f), Brass, "PumpBand");
        Decor(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * 1.2f, Quaternion.identity, new Vector3(3.06f, 0.06f, 3.06f), Brass, "PumpBand");
        Decor(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * 4f, Quaternion.identity, new Vector3(3.06f, 0.06f, 3.06f), Brass, "PumpBand");
        Decor(parent, PrimitiveType.Sphere, groundPos + Vector3.up * 4.6f, Quaternion.identity, new Vector3(3f, 1.6f, 3f), Iron, "PumpDome");
        Decor(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * 4.62f, Quaternion.identity, new Vector3(3.1f, 0.03f, 3.1f), WindowAmber, "PumpRing");
        Vector3 plate = groundPos + rot * new Vector3(1.55f, 1.8f, 0f);
        Decor(parent, PrimitiveType.Cube, plate, rot, new Vector3(0.04f, 0.9f, 0.9f), Brass, "PumpPlate");
        Gauge(parent, plate + rot * new Vector3(0.03f, 0.2f, -0.2f), rot * Vector3.right);
        Gauge(parent, plate + rot * new Vector3(0.03f, -0.2f, 0.2f), rot * Vector3.right);
        Pipe(parent, groundPos + rot * new Vector3(0f, 1.2f, 1.5f), groundPos + rot * new Vector3(0f, 1.2f, 4.5f), 0.4f);
        Pipe(parent, groundPos + rot * new Vector3(0f, 5.4f, 0f), groundPos + rot * new Vector3(0f, 8.5f, 0f), 0.3f);
        Valve(parent, groundPos + rot * new Vector3(0f, 1.2f, 3f), rot * Vector3.forward);
    }

    /// Black water over a floor: a translucent slab a foot deep, no collider (walk through it)
    public static void Water(Transform parent, Vector3 groundCenter, Vector2 size)
    {
        EnsureWorks();
        var w = Decor(parent, PrimitiveType.Cube, groundCenter + Vector3.up * 0.16f, Quaternion.identity, new Vector3(size.x, 0.32f, size.y), blackWaterMat, "Water");
        w.GetComponent<MeshRenderer>().sharedMaterial = Tiled(blackWaterMat, new Vector3(size.x, 0.32f, size.y));   // ripples at their own size, not one per pool
        w.AddComponent<WaterFlow>();
    }
}
