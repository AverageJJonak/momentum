using UnityEngine;

/// <summary>
/// The corporate set and the detail set, DARK DECO. HALCYON CIVIC's
/// headquarters in 1939: black marble, cream enamel, nickel, brass, walnut,
/// oxblood leather, amber lamps. Reception counters with round brass corners,
/// turnstiles and velvet ropes, mail carts, cabinets, lecterns, chandelier
/// rings, bronze lift doors with a sunburst transom and a dial, control boards,
/// bells, clock faces, torchiere sconces, enamel signs, brass handrails, walnut
/// doors with portholes, desks with their lamps and typewriters, fluted
/// screens, meeting rooms, switchgear, fluted columns — and the pieces every
/// level's rooms are dressed with: pilasters, wainscots, stepped cornices,
/// beams, amber windows, stone curtain walls, banners, benches, fins, dados,
/// coves, lit signboards, pipes, brass valves, ducts, grates, scaffolds and
/// chains. Everything here is decor unless it says otherwise: no colliders, so
/// wall runs never snag. Nothing sits in the plane of a wall or a floor —
/// coplanar faces flicker. Every lamp is warm; red is danger; the only cyan is a cut.
/// </summary>
public static partial class LevelKit
{
    static Material steelMat, glassMat, frostMat, ledMat, amberMat, graphiteMat, whiteGlossMat, fabricMat, screenMat,
        carpetGreyMat, satinMat, paperMat, ironMat, rustMat, plasticMat, cardboardMat, bannerA, bannerB;
    static readonly Color[] ScreenTones =
    {
        new Color(1.3f, 1.0f, 0.6f), new Color(1.1f, 0.75f, 0.45f), new Color(1.2f, 1.1f, 0.85f), new Color(0.95f, 0.7f, 0.4f),
    };
    static void EnsureOffice()
    {
        if (steelMat != null) return;
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        steelMat = Lit(lit, new Color(0.6f, 0.58f, 0.52f), 0.75f); steelMat.SetFloat("_Metallic", 0.9f);   // nickel
        glassMat = Transparent(new Color(1f, 0.78f, 0.5f, 0.28f));         // amber glass
        frostMat = Transparent(new Color(0.95f, 0.9f, 0.78f, 0.55f));      // warm frost
        ledMat = Unlit(unlit, new Color(1.9f, 1.7f, 1.35f));               // a lamp's cream: every lamp is warm
        amberMat = Unlit(unlit, new Color(1.8f, 1.25f, 0.25f));
        graphiteMat = Lit(lit, new Color(0.11f, 0.11f, 0.13f), 0.5f);      // charcoal
        whiteGlossMat = Lit(lit, new Color(0.8f, 0.74f, 0.62f), 0.8f);     // cream enamel
        fabricMat = Lit(lit, new Color(0.3f, 0.09f, 0.08f), 0.3f);         // oxblood leather
        screenMat = Lit(lit, new Color(0.04f, 0.04f, 0.05f), 0.9f);        // black lacquer
        carpetGreyMat = Lit(lit, new Color(0.28f, 0.08f, 0.08f), 0.08f);   // oxblood carpet
        satinMat = Lit(lit, new Color(0.03f, 0.03f, 0.04f), 0.9f);
        paperMat = Lit(lit, new Color(0.9f, 0.85f, 0.72f), 0.1f);
        ironMat = Lit(lit, new Color(0.2f, 0.2f, 0.22f), 0.45f); ironMat.SetFloat("_Metallic", 0.7f);
        rustMat = Lit(lit, new Color(0.42f, 0.2f, 0.1f), 0.15f);
        plasticMat = Lit(lit, new Color(0.15f, 0.1f, 0.08f), 0.5f);        // bakelite
        cardboardMat = Lit(lit, new Color(0.72f, 0.6f, 0.45f), 0.1f);
        bannerA = Lit(lit, new Color(0.42f, 0.1f, 0.09f), 0.15f);
        bannerB = Lit(lit, new Color(0.86f, 0.8f, 0.66f), 0.15f);
    }

    /// URP Lit set up for alpha blending (the recipe BreakableGlass uses)
    static Material Transparent(Color c)
    {
        var m = FromTemplate("LitTransparent", "Universal Render Pipeline/Lit");
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", 0.92f);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return m;
    }

    public static Material Steel { get { EnsureOffice(); return steelMat; } }
    public static Material GlassPane { get { EnsureOffice(); return glassMat; } }
    public static Material Frost { get { EnsureOffice(); return frostMat; } }
    public static Material Led { get { EnsureOffice(); return ledMat; } }
    public static Material Amber { get { EnsureOffice(); return amberMat; } }
    public static Material Graphite { get { EnsureOffice(); return graphiteMat; } }
    public static Material WhiteGloss { get { EnsureOffice(); return whiteGlossMat; } }
    public static Material Fabric { get { EnsureOffice(); return fabricMat; } }
    public static Material CarpetGrey { get { EnsureOffice(); return carpetGreyMat; } }
    public static Material Paper { get { EnsureOffice(); return paperMat; } }
    public static Material Iron { get { EnsureOffice(); return ironMat; } }
    public static Material Plastic { get { EnsureOffice(); return plasticMat; } }

    // ------------------------------------------------------------ small helpers

    /// A primitive with no collider, placed and rotated in the world
    static GameObject Decor(Transform parent, PrimitiveType type, Vector3 pos, Quaternion rot, Vector3 scale, Material mat, string name)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent);
        go.transform.SetPositionAndRotation(pos, rot);
        go.transform.localScale = scale;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    /// A primitive that keeps its collider (a thing to stand on, run along or weave between)
    static GameObject Solid(Transform parent, PrimitiveType type, Vector3 pos, Quaternion rot, Vector3 scale, Material mat, string name)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.SetPositionAndRotation(pos, rot);
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    /// A warm point light (the name is history: nothing in the company's house is cold any more)
    static Light CoolLight(Transform parent, Vector3 pos, float range, float intensity, string name = "WarmLight")
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = range;
        l.intensity = intensity;
        l.color = new Color(1f, 0.88f, 0.68f);
        l.shadows = LightShadows.None;
        return l;
    }

    /// A stepped frame round an opening: `steps` nested rectangles, each one proud of the last. `w`/`h` are the
    /// innermost opening; `forward` points into the wall; the frame stands on the room side.
    static void SteppedFrame(Transform parent, Vector3 center, Quaternion rot, Vector3 forward, float w, float h, int steps, float band, float deep, Material mat, string name)
    {
        Vector3 right = rot * Vector3.right;
        for (int s = 0; s < steps; s++)
        {
            float ww = w + band * 2f * (s + 1), hh = h + band * 2f * (s + 1);
            Vector3 c = center - forward * (deep * (steps - s) * 0.5f + 0.01f);
            Decor(parent, PrimitiveType.Cube, c + right * (-ww * 0.5f + band * 0.5f), rot, new Vector3(band, hh, deep * (steps - s)), mat, name);
            Decor(parent, PrimitiveType.Cube, c + right * (ww * 0.5f - band * 0.5f), rot, new Vector3(band, hh, deep * (steps - s)), mat, name);
            Decor(parent, PrimitiveType.Cube, c + Vector3.up * (hh * 0.5f - band * 0.5f), rot, new Vector3(ww - band * 2f, band, deep * (steps - s)), mat, name);
        }
    }

    /// A sunburst over an opening: brass rays fanning from a keystone, an arc round them, on the room side of a wall
    static void SunburstTransom(Transform parent, Vector3 top, Quaternion rot, Vector3 forward, float radius, Material mat)
    {
        Vector3 right = rot * Vector3.right;
        Decor(parent, PrimitiveType.Cube, top + Vector3.up * 0.1f - forward * 0.05f, rot, new Vector3(0.3f, 0.2f, 0.1f), mat, "Keystone");
        const int rays = 7;
        for (int i = 0; i < rays; i++)
        {
            float a = Mathf.Lerp(14f, 166f, i / (rays - 1f)) * Mathf.Deg2Rad;
            float len = radius - 0.3f;
            Vector3 c = top + right * (Mathf.Cos(a) * (0.2f + len * 0.5f)) + Vector3.up * (Mathf.Sin(a) * (0.2f + len * 0.5f)) - forward * 0.03f;
            Decor(parent, PrimitiveType.Cube, c, rot * Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg), new Vector3(len, 0.06f, 0.04f), mat, "Ray");
        }
        const int arc = 9;
        float seg = Mathf.PI * radius / arc * 1.06f;
        for (int k = 0; k < arc; k++)
        {
            float b = (k + 0.5f) / arc * Mathf.PI;
            Vector3 c = top + right * (Mathf.Cos(b) * radius) + Vector3.up * (Mathf.Sin(b) * radius) - forward * 0.03f;
            Decor(parent, PrimitiveType.Cube, c, rot * Quaternion.Euler(0f, 0f, b * Mathf.Rad2Deg + 90f), new Vector3(seg, 0.07f, 0.05f), mat, "Arc");
        }
    }

    // One material per tone, shared: a level makes hundreds of lit panels, and every
    // material a level makes is an asset the next scene load has to throw away
    static readonly Material[] screenMats = new Material[ScreenTones.Length];
    static readonly Material[] posterMats = new Material[ScreenTones.Length * 2];
    static Material holoInkMat;
    /// The cream the signs' letters are lit in: HoloSign's, and anyone who rewrites one (StateSign, the room gates)
    public static readonly Color SignCream = new Color(1.9f, 1.7f, 1.35f);

    static Material Screen(int tone)
    {
        int i = Mathf.Abs(tone) % ScreenTones.Length;
        if (screenMats[i] == null) screenMats[i] = Unlit(Shader.Find("Universal Render Pipeline/Unlit"), ScreenTones[i]);
        return screenMats[i];
    }

    static Material PosterTone(int tone, bool dim)
    {
        int i = Mathf.Abs(tone) % ScreenTones.Length;
        int slot = i * 2 + (dim ? 1 : 0);
        if (posterMats[slot] == null) posterMats[slot] = Lit(Shader.Find("Universal Render Pipeline/Lit"), ScreenTones[i] * (dim ? 0.42f : 0.6f), 0.4f);
        return posterMats[slot];
    }

    // ------------------------------------------------------------ the corporate set

    /// An oxblood carpet inset with brass guide lines down both edges. Decor: you walk on the floor under it.
    public static GameObject Runner(Transform parent, Vector3 groundCenter, Vector2 size)
    {
        EnsureOffice();
        var c = Decor(parent, PrimitiveType.Cube, groundCenter + Vector3.up * 0.02f, Quaternion.identity, new Vector3(size.x, 0.04f, size.y), carpetGreyMat, "Runner");
        Decor(parent, PrimitiveType.Cube, groundCenter + new Vector3(-size.x * 0.5f + 0.1f, 0.05f, 0f), Quaternion.identity, new Vector3(0.1f, 0.02f, size.y), Brass, "GuideLine");
        Decor(parent, PrimitiveType.Cube, groundCenter + new Vector3(size.x * 0.5f - 0.1f, 0.05f, 0f), Quaternion.identity, new Vector3(0.1f, 0.02f, size.y), Brass, "GuideLine");
        return c;
    }

    /// A round inset under a desk, ringed with brass
    public static void RoundInset(Transform parent, Vector3 groundCenter, float radius)
    {
        EnsureOffice();
        Decor(parent, PrimitiveType.Cylinder, groundCenter + Vector3.up * 0.015f, Quaternion.identity, new Vector3(radius * 2f + 0.3f, 0.015f, radius * 2f + 0.3f), Brass, "InsetRing");
        Decor(parent, PrimitiveType.Cylinder, groundCenter + Vector3.up * 0.035f, Quaternion.identity, new Vector3(radius * 2f, 0.02f, radius * 2f), carpetGreyMat, "RoundInset");
    }

    /// A brass post with a lit cream cap on a bronze foot. Keeps its collider: a thing to weave between.
    public static void Bollard(Transform parent, Vector3 groundPos)
    {
        EnsureOffice();
        Solid(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * 0.5f, Quaternion.identity, new Vector3(0.1f, 0.5f, 0.1f), Brass, "Bollard");
        Decor(parent, PrimitiveType.Sphere, groundPos + Vector3.up * 1.03f, Quaternion.identity, new Vector3(0.14f, 0.14f, 0.14f), ledMat, "BollardCap");
        Decor(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * 0.03f, Quaternion.identity, new Vector3(0.3f, 0.03f, 0.3f), Bronze, "BollardBase");
    }

    /// A velvet rope between two posts at 1 m: slide under it or hop it. Keeps its collider.
    public static void Belt(Transform parent, Vector3 groundA, Vector3 groundB)
    {
        EnsureOffice();
        Vector3 from = groundA + Vector3.up * 0.98f, to = groundB + Vector3.up * 0.98f;
        Vector3 d = to - from;
        Solid(parent, PrimitiveType.Cylinder, (from + to) * 0.5f, Quaternion.FromToRotation(Vector3.up, d.normalized), new Vector3(0.06f, d.magnitude * 0.5f, 0.06f), fabricMat, "Belt");
    }

    /// A row of bollards roped together from `start` along `dir`
    public static void Queue(Transform parent, Vector3 start, Vector3 dir, int posts, float spacing)
    {
        dir.y = 0f;
        dir.Normalize();
        Vector3 prev = start;
        for (int i = 0; i < posts; i++)
        {
            Vector3 p = start + dir * (i * spacing);
            Bollard(parent, p);
            if (i > 0) Belt(parent, prev, p);
            prev = p;
        }
    }

    /// A security gate: two black marble pedestals with a frosted paddle between them at chest height — slide under it
    /// or hop it. Pedestals and paddle are real.
    public static void Turnstile(Transform parent, Vector3 groundCenter, float yaw)
    {
        EnsureOffice();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 p = groundCenter + rot * new Vector3(s * 0.8f, 0f, 0f);
            var ped = Solid(parent, PrimitiveType.Cube, p + Vector3.up * 0.5f, rot, new Vector3(0.28f, 1.0f, 1.1f), Marble, "Pedestal");
            TechMarkers.MarkBoxCorners(ped);
            Decor(parent, PrimitiveType.Cube, p + Vector3.up * 1.005f, rot, new Vector3(0.2f, 0.012f, 0.9f), Brass, "PedestalTop");
            Decor(parent, PrimitiveType.Cube, p + rot * new Vector3(0f, 0.7f, 0.56f), rot, new Vector3(0.14f, 0.14f, 0.012f), amberMat, "GateLamp");
        }
        Solid(parent, PrimitiveType.Cube, groundCenter + Vector3.up * 1.2f, rot, new Vector3(1.32f, 0.3f, 0.03f), frostMat, "Paddle");
        Decor(parent, PrimitiveType.Cube, groundCenter + Vector3.up * 1.36f, rot, new Vector3(1.34f, 0.02f, 0.05f), Brass, "PaddleEdge");
    }

    /// A line of turnstiles side by side, centred on `center`, along the direction `yaw` faces sideways
    public static void TurnstileLine(Transform parent, Vector3 groundCenter, float yaw, int count, float spacing = 1.95f)
    {
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        for (int i = 0; i < count; i++)
            Turnstile(parent, groundCenter + rot * new Vector3((i - (count - 1) * 0.5f) * spacing, 0f, 0f), yaw);
    }

    /// A mail cart: an iron frame over a walnut deck with two boxes on it. The deck and
    /// the boxes are real boxes — corner boosts off the mail.
    public static void Cart(Transform parent, Vector3 groundPos, float yaw)
    {
        EnsureOffice();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        var deck = Pillar(parent, groundPos + Vector3.up * 0.28f, new Vector3(1.4f, 0.1f, 0.85f), WoodPanel);
        deck.name = "CartDeck";
        deck.transform.rotation = rot;
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                Vector3 corner = groundPos + rot * new Vector3(sx * 0.62f, 0f, sz * 0.36f);
                Decor(parent, PrimitiveType.Cylinder, corner + Vector3.up * 0.11f, rot * Quaternion.Euler(0f, 0f, 90f), new Vector3(0.22f, 0.03f, 0.22f), ironMat, "Wheel");
                Decor(parent, PrimitiveType.Cylinder, corner + Vector3.up * 1.0f, rot, new Vector3(0.04f, 0.7f, 0.04f), ironMat, "Upright");
            }
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0f, 1.7f, 0.36f), rot, new Vector3(1.3f, 0.04f, 0.04f), Brass, "Rail");
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0f, 1.7f, -0.36f), rot, new Vector3(1.3f, 0.04f, 0.04f), Brass, "Rail");
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0.62f, 1.7f, 0f), rot, new Vector3(0.04f, 0.04f, 0.76f), Brass, "Handle");
        var boxA = Pillar(parent, groundPos + rot * new Vector3(-0.25f, 0.6f, 0f), new Vector3(0.8f, 0.55f, 0.55f), cardboardMat);
        boxA.name = "Box";
        boxA.transform.rotation = rot;
        var boxB = Pillar(parent, groundPos + rot * new Vector3(0.42f, 0.53f, 0.08f), new Vector3(0.45f, 0.4f, 0.4f), cardboardMat);
        boxB.name = "Box";
        boxB.transform.rotation = rot * Quaternion.Euler(0f, 14f, 0f);
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(-0.25f, 0.88f, 0f), rot, new Vector3(0.82f, 0.012f, 0.1f), satinMat, "Tape");
    }

    /// A filing cabinet in walnut with brass trim and a lit panel. A boost block.
    public static void Copier(Transform parent, Vector3 groundPos, float yaw)
    {
        EnsureOffice();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        var body = Pillar(parent, groundPos + Vector3.up * 0.52f, new Vector3(1.2f, 1.04f, 0.72f), WoodPanel);
        body.name = "Copier";
        body.transform.rotation = rot;
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0f, 1.07f, 0f), rot, new Vector3(1.22f, 0.06f, 0.74f), Marble, "CopierLid");
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0f, 1.105f, 0f), rot, new Vector3(1.26f, 0.015f, 0.78f), Brass, "CopierEdge");
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0.35f, 1.12f, 0.2f), rot * Quaternion.Euler(-30f, 0f, 0f), new Vector3(0.3f, 0.2f, 0.02f), amberMat, "CopierPanel");
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(-0.2f, 0.75f, 0.42f), rot, new Vector3(0.5f, 0.03f, 0.2f), paperMat, "PaperTray");
        for (int i = 0; i < 3; i++)
            Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0f, 0.2f + i * 0.3f, 0.365f), rot, new Vector3(0.9f, 0.22f, 0.012f), satinMat, "Drawer");
        for (int i = 0; i < 3; i++)
            Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0f, 0.2f + i * 0.3f, 0.38f), rot, new Vector3(0.18f, 0.03f, 0.02f), Brass, "DrawerPull");
    }

    /// A visitor lectern: a bronze pedestal with a tilted lit sign plate in a brass frame
    public static void Kiosk(Transform parent, Vector3 groundPos, float yaw)
    {
        EnsureOffice();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Solid(parent, PrimitiveType.Cube, groundPos + Vector3.up * 0.62f, rot, new Vector3(0.5f, 1.24f, 0.4f), Bronze, "Kiosk");
        Decor(parent, PrimitiveType.Cube, groundPos + Vector3.up * 0.06f, rot, new Vector3(0.7f, 0.12f, 0.6f), Limestone, "KioskFoot");
        Quaternion tilt = rot * Quaternion.Euler(-28f, 0f, 0f);
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0f, 1.38f, 0.02f), tilt, new Vector3(0.52f, 0.36f, 0.03f), Brass, "KioskScreen");
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0f, 1.38f, 0.02f) + tilt * Vector3.forward * -0.02f, tilt, new Vector3(0.46f, 0.3f, 0.01f), Screen(1), "KioskContent");
    }

    /// A drinking fountain: a stone pedestal, a brass basin and tap, a frosted globe lamp over it. Every office has one.
    public static void WaterCooler(Transform parent, Vector3 groundPos)
    {
        EnsureOffice();
        Solid(parent, PrimitiveType.Cube, groundPos + Vector3.up * 0.5f, Quaternion.identity, new Vector3(0.36f, 1.0f, 0.36f), Limestone, "Cooler");
        Decor(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * 1.05f, Quaternion.identity, new Vector3(0.44f, 0.06f, 0.44f), Brass, "Bottle");
        Decor(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * 1.3f, Quaternion.identity, new Vector3(0.03f, 0.2f, 0.03f), Brass, "GlobeStem");
        Decor(parent, PrimitiveType.Sphere, groundPos + Vector3.up * 1.6f, Quaternion.identity, new Vector3(0.3f, 0.3f, 0.3f), ledMat, "BottleTop");
        Decor(parent, PrimitiveType.Cube, groundPos + new Vector3(0f, 1.14f, 0.1f), Quaternion.identity, new Vector3(0.05f, 0.1f, 0.14f), Brass, "Tap");
        CoolLight(parent, groundPos + Vector3.up * 1.8f, 4f, 0.8f, "FountainLight");
    }

    /// A stone planter with a bronze rim, a hedge and a small tree in it (the planter and the trunk keep their colliders)
    public static void PlanterBox(Transform parent, Vector3 groundCenter, Vector3 size, float treeHeight)
    {
        Slab(parent, groundCenter + Vector3.up * (size.y * 0.5f), size, Limestone, "Planter");
        Decor(parent, PrimitiveType.Cube, groundCenter + Vector3.up * (size.y + 0.02f), Quaternion.identity, new Vector3(size.x + 0.08f, 0.04f, size.z + 0.08f), Bronze, "PlanterRim");
        Slab(parent, groundCenter + Vector3.up * (size.y + 0.35f), new Vector3(size.x * 0.9f, 0.7f, size.z * 0.9f), hedgeMat, "Hedge");
        if (treeHeight > 0f) Tree(parent, groundCenter + Vector3.up * size.y, treeHeight, false);
    }

    /// A brass chandelier ring on chains — and a grapple orb for a heart when `grapple` is set
    public static void LightRing(Transform parent, Vector3 pos, float ceilingY, bool grapple)
    {
        EnsureOffice();
        Vector3 ring = pos + Vector3.up * 0.9f;
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 a = ring + new Vector3(s * 1.1f, 0f, 0f), b = new Vector3(ring.x + s * 1.1f, ceilingY, ring.z);
            Decor(parent, PrimitiveType.Cylinder, (a + b) * 0.5f, Quaternion.identity, new Vector3(0.03f, (b.y - a.y) * 0.5f, 0.03f), Brass, "RingChain");
        }
        Decor(parent, PrimitiveType.Cylinder, ring, Quaternion.identity, new Vector3(2.8f, 0.05f, 2.8f), ledMat, "LightRing");
        Decor(parent, PrimitiveType.Cylinder, ring + Vector3.up * 0.02f, Quaternion.identity, new Vector3(2.4f, 0.04f, 2.4f), Brass, "RingCore");
        Decor(parent, PrimitiveType.Cylinder, ring - Vector3.up * 0.02f, Quaternion.identity, new Vector3(2.4f, 0.04f, 2.4f), Brass, "RingCore");
        Decor(parent, PrimitiveType.Cylinder, ring + Vector3.up * 0.1f, Quaternion.identity, new Vector3(3f, 0.06f, 3f), Brass, "RingRim");
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            Decor(parent, PrimitiveType.Sphere, ring + new Vector3(Mathf.Cos(a) * 1.5f, -0.1f, Mathf.Sin(a) * 1.5f), Quaternion.identity, Vector3.one * 0.22f, ledMat, "RingGlobe");
        }
        if (grapple) Orb(parent, pos);
        CoolLight(parent, ring - Vector3.up * 0.5f, 16f, 1.5f, "RingLight");
    }

    /// Bronze lift doors in a stepped brass frame, a sunburst over them, a lit seam, an amber indicator, a floor letter,
    /// and the dial over it all: an arc of brass with a needle
    public static void LiftDoors(Transform parent, Vector3 center, Vector3 facing, string label)
    {
        EnsureOffice();
        Vector3 forward = -facing.normalized;
        PlaceOnWall(ref center, ref forward, 0.02f);
        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
        Vector3 right = rot * Vector3.right;
        Decor(parent, PrimitiveType.Cube, center - forward * 0.12f, rot, new Vector3(3.2f, 4.6f, 0.24f), Marble, "LiftFrame");
        SteppedFrame(parent, center - forward * 0.24f, rot, forward, 3f, 4.4f, 3, 0.1f, 0.05f, Brass, "LiftStep");
        for (int s = -1; s <= 1; s += 2)
        {
            Decor(parent, PrimitiveType.Cube, center - forward * 0.26f + right * (s * 0.72f) - Vector3.up * 0.2f, rot, new Vector3(1.38f, 4f, 0.06f), Bronze, "LiftDoor");
            for (int k = 0; k < 3; k++)   // three brass lines up each leaf: the fluting
                Decor(parent, PrimitiveType.Cube, center - forward * 0.3f + right * (s * (0.4f + k * 0.3f)) - Vector3.up * 0.2f, rot, new Vector3(0.04f, 3.6f, 0.02f), Brass, "LiftFlute");
        }
        Decor(parent, PrimitiveType.Cube, center - forward * 0.27f - Vector3.up * 0.2f, rot, new Vector3(0.04f, 4f, 0.08f), ledMat, "LiftSeam");
        Decor(parent, PrimitiveType.Cube, center - forward * 0.27f + Vector3.up * 2.05f, rot, new Vector3(1.2f, 0.12f, 0.06f), amberMat, "LiftIndicator");
        Text(parent, center - forward * 0.29f + Vector3.up * 1.72f, rot, label, 0.024f, new Color(0.06f, 0.06f, 0.08f), "FloorLetter", 1.2f, true, 30f, 70f);
        SunburstTransom(parent, center - forward * 0.3f + Vector3.up * 2.3f, rot, forward, 1.3f, Brass);
        // The dial: a half ring of brass marks, the floors on it, the needle pointing at none of them
        Vector3 dial = center - forward * 0.32f + Vector3.up * 3.9f;
        const int marks = 7;
        for (int i = 0; i < marks; i++)
        {
            float a = Mathf.Lerp(160f, 20f, i / (marks - 1f)) * Mathf.Deg2Rad;
            Decor(parent, PrimitiveType.Cube, dial + right * (Mathf.Cos(a) * 0.55f) + Vector3.up * (Mathf.Sin(a) * 0.55f), rot * Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg), new Vector3(0.12f, 0.04f, 0.04f), Brass, "DialMark");
            Text(parent, dial + right * (Mathf.Cos(a) * 0.72f) + Vector3.up * (Mathf.Sin(a) * 0.72f) - forward * 0.01f, rot, (i + 1).ToString(), 0.009f, new Color(0.78f, 0.58f, 0.25f), "DialNumber", 0.2f, true, 20f, 50f);
        }
        Decor(parent, PrimitiveType.Cube, dial + rot * Quaternion.Euler(0f, 0f, 110f) * Vector3.right * 0.24f, rot * Quaternion.Euler(0f, 0f, 110f), new Vector3(0.48f, 0.03f, 0.02f), Charcoal, "DialNeedle");
        Decor(parent, PrimitiveType.Cylinder, dial, rot * Quaternion.Euler(90f, 0f, 0f), new Vector3(0.12f, 0.03f, 0.12f), Brass, "DialHub");
    }

    /// A control board: a charcoal backboard, brass bezels, lit panels in cream and amber
    public static void MonitorWall(Transform parent, Vector3 center, Vector3 facing, float w, float h)
    {
        EnsureOffice();
        Vector3 forward = -facing.normalized;
        PlaceOnWall(ref center, ref forward, 0.02f);
        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
        Vector3 right = rot * Vector3.right;
        Decor(parent, PrimitiveType.Cube, center - forward * 0.05f, rot, new Vector3(w, h, 0.1f), graphiteMat, "MonitorWall");
        Decor(parent, PrimitiveType.Cube, center - forward * 0.11f + Vector3.up * (h * 0.5f), rot, new Vector3(w + 0.1f, 0.06f, 0.04f), Brass, "BoardTrim");
        Decor(parent, PrimitiveType.Cube, center - forward * 0.11f - Vector3.up * (h * 0.5f), rot, new Vector3(w + 0.1f, 0.06f, 0.04f), Brass, "BoardTrim");
        int cols = Mathf.Clamp(Mathf.RoundToInt(w / 1.0f), 1, 6), rows = Mathf.Clamp(Mathf.RoundToInt(h / 0.65f), 1, 4);
        float cw = (w - 0.2f) / cols, ch = (h - 0.2f) / rows;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                Vector3 p = center - forward * 0.13f + right * ((c + 0.5f) / cols * (w - 0.2f) - (w - 0.2f) * 0.5f) + Vector3.up * ((r + 0.5f) / rows * (h - 0.2f) - (h - 0.2f) * 0.5f);
                Decor(parent, PrimitiveType.Cube, p, rot, new Vector3(cw - 0.08f, ch - 0.08f, 0.05f), Brass, "Bezel");
                Decor(parent, PrimitiveType.Cube, p - forward * 0.04f, rot, new Vector3(cw - 0.18f, ch - 0.18f, 0.02f), Screen(r * 3 + c), "Content");
                Decor(parent, PrimitiveType.Cylinder, p - forward * 0.06f + right * (cw * 0.3f), rot * Quaternion.Euler(90f, 0f, 0f), new Vector3(ch * 0.4f, 0.01f, ch * 0.4f), Charcoal, "Dial");
            }
    }

    /// The desk bell, in brass. Returns the dome so it can be rung.
    public static Transform Bell(Transform parent, Vector3 pos)
    {
        EnsureOffice();
        Decor(parent, PrimitiveType.Cylinder, pos + Vector3.up * 0.015f, Quaternion.identity, new Vector3(0.36f, 0.015f, 0.36f), Bronze, "BellBase");
        var dome = Decor(parent, PrimitiveType.Sphere, pos + Vector3.up * 0.13f, Quaternion.identity, new Vector3(0.3f, 0.22f, 0.3f), Brass, "Bell");
        Decor(parent, PrimitiveType.Cube, pos + Vector3.up * 0.26f, Quaternion.identity, new Vector3(0.05f, 0.04f, 0.05f), satinMat, "BellButton");
        return dome.transform;
    }

    /// A wall clock: a lit cream face in a brass rim with charcoal hands, and a place name under it (the world's time, all of it Halcyon's)
    public static void Clock(Transform parent, Vector3 center, Vector3 facing, string label, float hourAngle, float minuteAngle)
    {
        EnsureOffice();
        Vector3 forward = -facing.normalized;
        PlaceOnWall(ref center, ref forward, 0.02f);
        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
        ClockFace(parent, center - forward * 0.08f, -forward, 0.9f, hourAngle, minuteAngle);
        Text(parent, center - forward * 0.06f - Vector3.up * 0.85f, rot, label, 0.012f, new Color(0.78f, 0.58f, 0.25f), "ClockLabel", 1.6f, true, 25f, 60f);
    }

    /// A torchiere sconce: a brass plate, a bronze stem, a cream bowl, a warm light thrown up the wall
    public static void WallLight(Transform parent, Vector3 center, Vector3 facing)
    {
        EnsureOffice();
        Vector3 forward = -facing.normalized;
        PlaceOnWall(ref center, ref forward, 0.02f);
        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
        Decor(parent, PrimitiveType.Cube, center - forward * 0.015f - Vector3.up * 0.5f, rot, new Vector3(0.16f, 0.34f, 0.03f), Brass, "LightPlate");
        Decor(parent, PrimitiveType.Cylinder, center - forward * 0.1f - Vector3.up * 0.1f, Quaternion.identity, new Vector3(0.06f, 0.45f, 0.06f), Bronze, "LightStem");
        Decor(parent, PrimitiveType.Cylinder, center - forward * 0.17f + Vector3.up * 0.4f, Quaternion.identity, new Vector3(0.28f, 0.1f, 0.28f), Bronze, "LightBowl");
        Decor(parent, PrimitiveType.Cylinder, center - forward * 0.17f + Vector3.up * 0.505f, Quaternion.identity, new Vector3(0.22f, 0.012f, 0.22f), ledMat, "LightBar");
        var l = CoolLight(parent, center - forward * 0.3f + Vector3.up * 0.7f, 7f, 1.2f, "WallLight");
        l.type = LightType.Spot;
        l.spotAngle = 120f;
        l.innerSpotAngle = 50f;
        l.transform.rotation = Quaternion.LookRotation(Vector3.up - forward * 0.25f, -forward);
    }

    /// A painted enamel sign in a brass frame: a two-tone plate, the mark, and one of the company's lines
    public static void Poster(Transform parent, Vector3 center, Vector3 facing, float w, float h, int tone)
    {
        EnsureOffice();
        Vector3 forward = -facing.normalized;
        PlaceOnWall(ref center, ref forward, 0.02f);
        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
        Decor(parent, PrimitiveType.Cube, center - forward * 0.05f, rot, new Vector3(w + 0.16f, h + 0.16f, 0.1f), Brass, "PosterFrame");
        Material a = PosterTone(tone, true), b = PosterTone(tone + 2, false);
        Decor(parent, PrimitiveType.Cube, center - forward * 0.11f, rot, new Vector3(w, h, 0.02f), a, "Poster");
        Decor(parent, PrimitiveType.Cube, center - forward * 0.13f + Vector3.up * (h * 0.18f), rot, new Vector3(w, h * 0.5f, 0.012f), b, "Poster");
        Decor(parent, PrimitiveType.Cube, center - forward * 0.14f - Vector3.up * (h * 0.07f), rot, new Vector3(w * 0.8f, 0.02f, 0.01f), Brass, "PosterRule");
        Emblem(parent, center - forward * 0.16f + Vector3.up * (h * 0.14f), rot, h * 0.34f, EmblemInk(), b);
        string line = Taglines[Mathf.Abs(tone) % Taglines.Length];
        Text(parent, center - forward * 0.15f - Vector3.up * (h * 0.36f), rot, line, h * 0.012f, new Color(0.06f, 0.06f, 0.08f), "PosterLine", w - 0.3f, false, 25f, 60f);
    }

    /// A balustrade: brass posts, a frosted panel, a brass handrail. Decor (you can run through it).
    public static void GlassRail(Transform parent, Vector3 from, Vector3 to, float height = 1.05f)
    {
        EnsureOffice();
        Vector3 d = to - from;
        float len = d.magnitude;
        if (len < 0.5f) return;
        Vector3 dir = d / len;
        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
        int posts = Mathf.Max(2, Mathf.RoundToInt(len / 2.5f) + 1);
        for (int i = 0; i < posts; i++)
        {
            Vector3 p = from + dir * (len * i / (posts - 1));
            Decor(parent, PrimitiveType.Cylinder, p + Vector3.up * (height * 0.5f), Quaternion.identity, new Vector3(0.06f, height * 0.5f, 0.06f), Brass, "RailPost");
            Decor(parent, PrimitiveType.Sphere, p + Vector3.up * (height + 0.06f), Quaternion.identity, Vector3.one * 0.1f, Brass, "RailKnob");
        }
        Decor(parent, PrimitiveType.Cube, (from + to) * 0.5f + Vector3.up * (height * 0.5f), rot, new Vector3(0.02f, height - 0.12f, len), frostMat, "GlassPanel");
        Decor(parent, PrimitiveType.Cube, (from + to) * 0.5f + Vector3.up * height, rot, new Vector3(0.1f, 0.06f, len + 0.1f), Brass, "Handrail");
    }

    /// A reception counter: a black marble front with brass round corners and the mark on it, a walnut top (a real
    /// box — run along it, boost off its corners), amber underglow, brass desk lamps. Local +Z faces the visitors.
    public static GameObject Desk(Transform parent, Vector3 groundCenter, float length, float yaw)
    {
        EnsureOffice();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        var front = Slab(parent, groundCenter + Vector3.up * 0.56f, new Vector3(length, 1.12f, 0.7f), Marble, "DeskFront");
        front.transform.rotation = rot;
        for (int s = -1; s <= 1; s += 2)   // the round corners: a drum at each end of the front
            Decor(parent, PrimitiveType.Cylinder, groundCenter + rot * new Vector3(s * length * 0.5f, 0.56f, 0f), rot, new Vector3(0.7f, 0.56f, 0.7f), Marble, "DeskCorner");
        var top = Pillar(parent, groundCenter + Vector3.up * 1.16f, new Vector3(length + 0.2f, 0.08f, 0.95f), WoodPanel);
        top.name = "DeskTop";
        top.transform.rotation = rot;
        Decor(parent, PrimitiveType.Cube, groundCenter + rot * new Vector3(0f, 0.06f, 0.4f), rot, new Vector3(length, 0.05f, 0.12f), amberMat, "Underglow");
        Decor(parent, PrimitiveType.Cube, groundCenter + rot * new Vector3(0f, 1.21f, 0.475f), rot, new Vector3(length + 0.2f, 0.02f, 0.02f), Brass, "DeskEdge");
        Decor(parent, PrimitiveType.Cube, groundCenter + rot * new Vector3(0f, 0.9f, 0.36f), rot, new Vector3(length, 0.04f, 0.02f), Brass, "DeskLine");
        Decor(parent, PrimitiveType.Cube, groundCenter + rot * new Vector3(0f, 0.3f, 0.36f), rot, new Vector3(length, 0.04f, 0.02f), Brass, "DeskLine");
        if (length >= 4f)
            Emblem(parent, groundCenter + rot * new Vector3(0f, 0.66f, 0.37f), Quaternion.Euler(0f, yaw + 180f, 0f), 0.55f, Brass, Marble);
        int lamps = Mathf.Max(1, Mathf.RoundToInt(length / 5f));
        for (int i = 0; i < lamps; i++)
        {
            float x = (i + 0.5f) / lamps * length - length * 0.5f + Random.Range(-0.5f, 0.5f);
            DeskLamp(parent, groundCenter + rot * new Vector3(x, 1.2f, -0.15f), new Color(1f, 0.86f, 0.62f));
        }
        return top;
    }

    /// A ring of counters around a centre: the concierge's station
    public static void DeskRing(Transform parent, Vector3 centre, float radius, int segments = 14)
    {
        float len = 2f * Mathf.PI * radius / segments + 0.15f;
        for (int k = 0; k < segments; k++)
        {
            float ang = k * 360f / segments;
            Vector3 radial = new Vector3(Mathf.Sin(ang * Mathf.Deg2Rad), 0f, Mathf.Cos(ang * Mathf.Deg2Rad));
            Desk(parent, centre + radial * radius, len, ang);
        }
    }

    /// A charcoal signboard in brass trims with amber lettering ("NOW SERVING 0042"). Returns the text.
    public static TextMesh Display(Transform parent, Vector3 center, Vector3 facing, float w, float h, string text)
    {
        EnsureOffice();
        Vector3 forward = -facing.normalized;
        PlaceOnWall(ref center, ref forward, 0.02f);
        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
        Decor(parent, PrimitiveType.Cube, center - forward * 0.07f, rot, new Vector3(w, h, 0.12f), screenMat, "Display");
        Decor(parent, PrimitiveType.Cube, center - forward * 0.14f - Vector3.up * (h * 0.5f - 0.03f), rot, new Vector3(w, 0.04f, 0.03f), Brass, "DisplayTrim");
        Decor(parent, PrimitiveType.Cube, center - forward * 0.14f + Vector3.up * (h * 0.5f - 0.03f), rot, new Vector3(w, 0.04f, 0.03f), Brass, "DisplayTrim");
        return Text(parent, center - forward * 0.15f, rot, text, h * 0.075f, new Color(2.2f, 1.2f, 0.3f), "DisplayText", w - 0.4f, false, 60f, 130f);
    }

    /// A long brass band with one of the company's lines on it in amber
    public static void Ticker(Transform parent, Vector3 center, Vector3 facing, float w, string line = null)
    {
        EnsureOffice();
        Vector3 forward = -facing.normalized;
        PlaceOnWall(ref center, ref forward, 0.02f);
        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
        Decor(parent, PrimitiveType.Cube, center - forward * 0.06f, rot, new Vector3(w, 0.5f, 0.1f), screenMat, "Ticker");
        Decor(parent, PrimitiveType.Cube, center - forward * 0.12f - Vector3.up * 0.26f, rot, new Vector3(w, 0.03f, 0.03f), Brass, "TickerRule");
        Decor(parent, PrimitiveType.Cube, center - forward * 0.12f + Vector3.up * 0.26f, rot, new Vector3(w, 0.03f, 0.03f), Brass, "TickerRule");
        Text(parent, center - forward * 0.13f, rot, line ?? (Company + "   ·   " + Taglines[Random.Range(0, Taglines.Length)]), 0.03f, new Color(2.2f, 1.2f, 0.3f), "TickerText", w - 0.5f, true, 50f, 110f);
    }

    /// An office door: a stepped brass frame, a walnut leaf with a porthole, a brass bar handle, a nameplate and a bell button
    public static void OfficeDoor(Transform parent, Vector3 groundCenter, Vector3 facing, string name, bool frosted)
    {
        EnsureOffice();
        Vector3 center = groundCenter + Vector3.up * 1.3f;
        Vector3 forward = -facing.normalized;
        PlaceOnWall(ref center, ref forward, 0.02f);
        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
        Vector3 right = rot * Vector3.right;
        Decor(parent, PrimitiveType.Cube, center - forward * 0.05f, rot, new Vector3(1.4f, 2.8f, 0.1f), Marble, "DoorFrame");
        SteppedFrame(parent, center - forward * 0.1f, rot, forward, 1.1f, 2.5f, 2, 0.07f, 0.04f, Brass, "DoorStep");
        Decor(parent, PrimitiveType.Cube, center - forward * 0.11f, rot, new Vector3(1.1f, 2.5f, 0.04f), WoodPanel, "Door");
        Decor(parent, PrimitiveType.Cylinder, center - forward * 0.135f + Vector3.up * 0.5f, rot * Quaternion.Euler(90f, 0f, 0f), new Vector3(0.5f, 0.015f, 0.5f), Brass, "PortholeRim");
        Decor(parent, PrimitiveType.Cylinder, center - forward * 0.14f + Vector3.up * 0.5f, rot * Quaternion.Euler(90f, 0f, 0f), new Vector3(0.4f, 0.01f, 0.4f), frosted ? frostMat : glassMat, "Porthole");
        Decor(parent, PrimitiveType.Cube, center - forward * 0.14f - Vector3.up * 1.15f, rot, new Vector3(1.1f, 0.2f, 0.02f), Brass, "KickPlate");
        Decor(parent, PrimitiveType.Cylinder, center - forward * 0.18f + right * 0.42f, rot, new Vector3(0.03f, 0.35f, 0.03f), Brass, "Handle");
        Decor(parent, PrimitiveType.Cube, center - forward * 0.06f + right * 1.1f + Vector3.up * 0.35f, rot, new Vector3(0.6f, 0.16f, 0.06f), Bronze, "Nameplate");
        Text(parent, center - forward * 0.1f + right * 1.1f + Vector3.up * 0.35f, rot, name, 0.008f, new Color(0.9f, 0.84f, 0.7f), "DoorName", 0.52f, true, 20f, 50f);
        Decor(parent, PrimitiveType.Cylinder, center - forward * 0.06f + right * 1.1f - Vector3.up * 0.1f, rot * Quaternion.Euler(90f, 0f, 0f), new Vector3(0.14f, 0.03f, 0.14f), Brass, "BadgeReader");
        Decor(parent, PrimitiveType.Cylinder, center - forward * 0.095f + right * 1.1f - Vector3.up * 0.1f, rot * Quaternion.Euler(90f, 0f, 0f), new Vector3(0.05f, 0.01f, 0.05f), amberMat, "ReaderLamp");
    }

    /// A workstation: a walnut desk (a real box, hop it or boost off it), a brass lamp, a typewriter, a leather chair
    public static void Workstation(Transform parent, Vector3 groundCenter, float yaw, int tone)
    {
        EnsureOffice();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        var desk = Pillar(parent, groundCenter + Vector3.up * 0.75f, new Vector3(1.6f, 0.06f, 0.8f), WoodPanel);
        desk.name = "Desk";
        desk.transform.rotation = rot;
        for (int s = -1; s <= 1; s += 2)
            Decor(parent, PrimitiveType.Cube, groundCenter + rot * new Vector3(s * 0.72f, 0.37f, 0f), rot, new Vector3(0.06f, 0.74f, 0.7f), Brass, "DeskLeg");
        Decor(parent, PrimitiveType.Cube, groundCenter + rot * new Vector3(0f, 0.785f, 0f), rot, new Vector3(1.64f, 0.012f, 0.84f), Brass, "DeskGlow");
        DeskLamp(parent, groundCenter + rot * new Vector3(-0.5f, 0.78f, -0.25f), new Color(1f, 0.86f, 0.62f));
        Decor(parent, PrimitiveType.Cube, groundCenter + rot * new Vector3(0.15f, 0.86f, -0.05f), rot, new Vector3(0.42f, 0.16f, 0.36f), screenMat, "Typewriter");
        Decor(parent, PrimitiveType.Cube, groundCenter + rot * new Vector3(0.15f, 0.96f, -0.18f), rot, new Vector3(0.46f, 0.05f, 0.08f), Brass, "TypewriterBar");
        Decor(parent, PrimitiveType.Cube, groundCenter + rot * new Vector3(0.15f, 0.8f, 0.14f), rot, new Vector3(0.36f, 0.03f, 0.12f), satinMat, "Keys");
        Decor(parent, PrimitiveType.Cube, groundCenter + rot * new Vector3(-0.05f, 0.79f, 0.28f), rot, new Vector3(0.3f, 0.01f, 0.22f), paperMat, "Papers");
        Decor(parent, PrimitiveType.Cylinder, groundCenter + rot * new Vector3(0f, 0.45f, 0.85f), rot, new Vector3(0.5f, 0.04f, 0.5f), fabricMat, "ChairSeat");
        Decor(parent, PrimitiveType.Cube, groundCenter + rot * new Vector3(0f, 0.75f, 1.08f), rot * Quaternion.Euler(-8f, 0f, 0f), new Vector3(0.46f, 0.5f, 0.06f), fabricMat, "ChairBack");
        Decor(parent, PrimitiveType.Cylinder, groundCenter + rot * new Vector3(0f, 0.22f, 0.85f), rot, new Vector3(0.05f, 0.22f, 0.05f), Brass, "ChairPost");
        Decor(parent, PrimitiveType.Cylinder, groundCenter + rot * new Vector3(0f, 0.02f, 0.85f), rot, new Vector3(0.5f, 0.015f, 0.5f), Brass, "ChairBase");
    }

    /// A fluted frosted screen on a brass rail — a real pane, hop it
    public static void Partition(Transform parent, Vector3 groundCenter, float yaw, float w, float h = 1.3f)
    {
        EnsureOffice();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        var pane = Slab(parent, groundCenter + Vector3.up * (h * 0.5f + 0.05f), new Vector3(w, h, 0.04f), frostMat, "Partition");
        pane.transform.rotation = rot;
        int flutes = Mathf.Max(2, Mathf.RoundToInt(w / 0.3f));
        for (int i = 0; i < flutes; i++)
            Decor(parent, PrimitiveType.Cube, groundCenter + rot * new Vector3(-w * 0.5f + w * (i + 0.5f) / flutes, h * 0.5f + 0.05f, 0.025f), rot, new Vector3(0.03f, h - 0.1f, 0.012f), Brass, "PartitionFlute");
        Decor(parent, PrimitiveType.Cube, groundCenter + Vector3.up * 0.04f, rot, new Vector3(w + 0.1f, 0.08f, 0.1f), Brass, "PartitionRail");
        Decor(parent, PrimitiveType.Cube, groundCenter + Vector3.up * (h + 0.06f), rot, new Vector3(w + 0.1f, 0.04f, 0.08f), Brass, "PartitionCap");
    }

    /// A meeting room: four frosted walls in brass posts (real: kick and run them), a glass roof you can stand on, a
    /// walnut table, leather chairs, a globe pendant. `size` is the outer footprint and the height.
    public static void MeetingPod(Transform parent, Vector3 groundCenter, Vector3 size, float yaw)
    {
        EnsureOffice();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        float hx = size.x * 0.5f, hz = size.z * 0.5f, h = size.y;
        var wA = Slab(parent, groundCenter + rot * new Vector3(0f, h * 0.5f, hz), new Vector3(size.x, h, 0.06f), frostMat, "PodWall"); wA.transform.rotation = rot;
        var wB = Slab(parent, groundCenter + rot * new Vector3(0f, h * 0.5f, -hz), new Vector3(size.x, h, 0.06f), frostMat, "PodWall"); wB.transform.rotation = rot;
        var wC = Slab(parent, groundCenter + rot * new Vector3(hx, h * 0.5f, 0f), new Vector3(0.06f, h, size.z), frostMat, "PodWall"); wC.transform.rotation = rot;
        var wD = Slab(parent, groundCenter + rot * new Vector3(-hx, h * 0.5f, 0f), new Vector3(0.06f, h, size.z), frostMat, "PodWall"); wD.transform.rotation = rot;
        var roof = Pillar(parent, groundCenter + Vector3.up * (h + 0.05f), new Vector3(size.x + 0.2f, 0.1f, size.z + 0.2f), glassMat);
        roof.name = "PodRoof";
        roof.transform.rotation = rot;
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Decor(parent, PrimitiveType.Cube, groundCenter + rot * new Vector3(sx * hx, h * 0.5f, sz * hz), rot, new Vector3(0.14f, h, 0.14f), Brass, "PodPost");
        Decor(parent, PrimitiveType.Cube, groundCenter + rot * new Vector3(0f, h + 0.13f, hz), rot, new Vector3(size.x + 0.2f, 0.06f, 0.14f), Brass, "PodRail");
        Decor(parent, PrimitiveType.Cube, groundCenter + rot * new Vector3(0f, h + 0.13f, -hz), rot, new Vector3(size.x + 0.2f, 0.06f, 0.14f), Brass, "PodRail");
        Decor(parent, PrimitiveType.Cube, groundCenter + rot * new Vector3(0f, 1.05f, hz + 0.04f), rot, new Vector3(size.x, 0.05f, 0.03f), Brass, "PodDado");
        Decor(parent, PrimitiveType.Cube, groundCenter + rot * new Vector3(0f, 1.05f, -hz - 0.04f), rot, new Vector3(size.x, 0.05f, 0.03f), Brass, "PodDado");
        var table = Pillar(parent, groundCenter + Vector3.up * 0.38f, new Vector3(Mathf.Max(1.2f, size.x - 2.4f), 0.76f, Mathf.Max(0.8f, size.z - 2.2f)), WoodPanel);
        table.name = "PodTable";
        table.transform.rotation = rot;
        for (int s = -1; s <= 1; s += 2)
            for (int i = -1; i <= 1; i++)
                Decor(parent, PrimitiveType.Cylinder, groundCenter + rot * new Vector3(i * 1.0f, 0.45f, s * (Mathf.Max(0.8f, size.z - 2.2f) * 0.5f + 0.5f)), rot, new Vector3(0.45f, 0.04f, 0.45f), fabricMat, "PodChair");
        Decor(parent, PrimitiveType.Cylinder, groundCenter + Vector3.up * (h - 0.4f), rot, new Vector3(0.03f, 0.4f, 0.03f), Brass, "PodChain");
        Decor(parent, PrimitiveType.Sphere, groundCenter + Vector3.up * (h - 0.95f), rot, Vector3.one * 0.5f, ledMat, "PodLamp");
        CoolLight(parent, groundCenter + Vector3.up * (h - 1.3f), 6f, 1.2f, "PodLight");
    }

    /// A switchgear cabinet: a charcoal tower with brass dials and amber lamps. A real box — a kick fin.
    public static void ServerRack(Transform parent, Vector3 groundPos, float yaw)
    {
        EnsureOffice();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        var rack = Pillar(parent, groundPos + Vector3.up * 1.15f, new Vector3(0.8f, 2.3f, 1.1f), graphiteMat);
        rack.name = "ServerRack";
        rack.transform.rotation = rot;
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0f, 1.15f, 0.56f), rot, new Vector3(0.7f, 2.1f, 0.02f), RivetIron, "RackDoor");
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0f, 2.32f, 0f), rot, new Vector3(0.86f, 0.04f, 1.16f), Brass, "RackCap");
        for (int i = 0; i < 4; i++)
        {
            float yy = 0.5f + i * 0.5f;
            Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(-0.22f, yy, 0.58f), rot, new Vector3(0.05f, 0.05f, 0.012f), i % 3 == 2 ? amberMat : ledMat, "RackLamp");
            Decor(parent, PrimitiveType.Cylinder, groundPos + rot * new Vector3(0.1f, yy, 0.58f), rot * Quaternion.Euler(90f, 0f, 0f), new Vector3(0.2f, 0.01f, 0.2f), Brass, "RackDial");
            Decor(parent, PrimitiveType.Cylinder, groundPos + rot * new Vector3(0.1f, yy, 0.59f), rot * Quaternion.Euler(90f, 0f, 0f), new Vector3(0.15f, 0.01f, 0.15f), paperMat, "RackDialFace");
        }
    }

    /// A fluted column with a stone base, a brass collar and a stepped cap (the column keeps its collider)
    public static void Column(Transform parent, Vector3 groundCenter, float height, float diameter = 1.2f)
    {
        EnsureOffice();
        Solid(parent, PrimitiveType.Cylinder, groundCenter + Vector3.up * (height * 0.5f), Quaternion.identity, new Vector3(diameter, height * 0.5f, diameter), Fluted, "Column");
        Decor(parent, PrimitiveType.Cylinder, groundCenter + Vector3.up * 0.15f, Quaternion.identity, new Vector3(diameter + 0.3f, 0.15f, diameter + 0.3f), Limestone, "ColumnBase");
        Decor(parent, PrimitiveType.Cube, groundCenter + Vector3.up * 0.06f, Quaternion.identity, new Vector3(diameter + 0.6f, 0.12f, diameter + 0.6f), Limestone, "ColumnPlinth");
        Decor(parent, PrimitiveType.Cylinder, groundCenter + Vector3.up * 3f, Quaternion.identity, new Vector3(diameter + 0.08f, 0.05f, diameter + 0.08f), Brass, "ColumnCollar");
        Decor(parent, PrimitiveType.Cylinder, groundCenter + Vector3.up * (height - 0.3f), Quaternion.identity, new Vector3(diameter + 0.2f, 0.12f, diameter + 0.2f), Limestone, "ColumnCap");
        Decor(parent, PrimitiveType.Cube, groundCenter + Vector3.up * (height - 0.1f), Quaternion.identity, new Vector3(diameter + 0.5f, 0.2f, diameter + 0.5f), Limestone, "ColumnCap");
    }

    // ------------------------------------------------------------ the detail set

    /// A wall column with a plinth and a stepped capital, two flutes up its face. `facing` points into the room.
    public static void Pilaster(Transform parent, Vector3 wallBase, Vector3 facing, float height, Material body, Material capital)
    {
        Quaternion rot = Quaternion.LookRotation(facing, Vector3.up);
        Decor(parent, PrimitiveType.Cube, wallBase + facing * 0.15f + Vector3.up * (height * 0.5f), rot, new Vector3(0.8f, height, 0.26f), body, "Pilaster");
        for (int s = -1; s <= 1; s += 2)
            Decor(parent, PrimitiveType.Cube, wallBase + facing * 0.29f + rot * Vector3.right * (s * 0.2f) + Vector3.up * (height * 0.5f), rot, new Vector3(0.1f, height - 0.9f, 0.03f), body, "Flute");
        Decor(parent, PrimitiveType.Cube, wallBase + facing * 0.18f + Vector3.up * (height - 0.38f), rot, new Vector3(0.95f, 0.16f, 0.32f), capital, "Capital");
        Decor(parent, PrimitiveType.Cube, wallBase + facing * 0.2f + Vector3.up * (height - 0.16f), rot, new Vector3(1.1f, 0.3f, 0.38f), capital, "Capital");
        Decor(parent, PrimitiveType.Cube, wallBase + facing * 0.19f + Vector3.up * 0.16f, rot, new Vector3(1.0f, 0.32f, 0.34f), capital, "Plinth");
    }

    /// A slim fluted fin on a wall with a lit cream seam down its face
    public static void Fin(Transform parent, Vector3 wallBase, Vector3 facing, float height)
    {
        EnsureOffice();
        Quaternion rot = Quaternion.LookRotation(facing, Vector3.up);
        Decor(parent, PrimitiveType.Cube, wallBase + facing * 0.13f + Vector3.up * (height * 0.5f), rot, new Vector3(0.4f, height, 0.22f), Fluted, "Fin");
        Decor(parent, PrimitiveType.Cube, wallBase + facing * 0.245f + Vector3.up * (height * 0.5f), rot, new Vector3(0.03f, height - 0.4f, 0.01f), ledMat, "FinSeam");
        Decor(parent, PrimitiveType.Cube, wallBase + facing * 0.15f + Vector3.up * (height - 0.08f), rot, new Vector3(0.5f, 0.16f, 0.28f), Brass, "FinCap");
    }

    /// A panelled band along the foot of a wall with a rail on top
    public static void Wainscot(Transform parent, Vector3 a, Vector3 b, Vector3 facing, Material band, Material rail, float height = 1.1f)
    {
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 1f) return;
        Quaternion rot = Quaternion.LookRotation(facing, Vector3.up);
        Vector3 mid = (a + b) * 0.5f;
        Decor(parent, PrimitiveType.Cube, mid + facing * 0.07f + Vector3.up * (height * 0.5f), rot, new Vector3(len, height, 0.1f), band, "Wainscot");
        Decor(parent, PrimitiveType.Cube, mid + facing * 0.11f + Vector3.up * height, rot, new Vector3(len, 0.08f, 0.18f), rail, "DadoRail");
    }

    /// A walnut band along the foot of a wall with a brass rail
    public static void Dado(Transform parent, Vector3 a, Vector3 b, Vector3 facing, float height = 0.9f)
    {
        EnsureOffice();
        Wainscot(parent, a, b, facing, WoodPanel, Brass, height);
    }

    /// A stepped moulding along the top of a wall: three steps, each deeper than the one under it
    public static void Cornice(Transform parent, Vector3 a, Vector3 b, Vector3 facing, float topY, Material mat)
    {
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 1f) return;
        Quaternion rot = Quaternion.LookRotation(facing, Vector3.up);
        Vector3 mid = (a + b) * 0.5f;
        mid.y = topY;
        Decor(parent, PrimitiveType.Cube, mid + facing * 0.06f - Vector3.up * 0.62f, rot, new Vector3(len, 0.2f, 0.12f), mat, "Cornice");
        Decor(parent, PrimitiveType.Cube, mid + facing * 0.11f - Vector3.up * 0.42f, rot, new Vector3(len, 0.2f, 0.22f), mat, "Cornice");
        Decor(parent, PrimitiveType.Cube, mid + facing * 0.17f - Vector3.up * 0.16f, rot, new Vector3(len, 0.32f, 0.34f), mat, "Cornice");
    }

    /// A cream cove along the top of a wall with a warm line of light under it
    public static void Cove(Transform parent, Vector3 a, Vector3 b, Vector3 facing, float topY)
    {
        EnsureOffice();
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 1f) return;
        Quaternion rot = Quaternion.LookRotation(facing, Vector3.up);
        Vector3 mid = (a + b) * 0.5f;
        mid.y = topY - 0.22f;
        Decor(parent, PrimitiveType.Cube, mid + facing * 0.14f, rot, new Vector3(len, 0.44f, 0.24f), Cream, "Cove");
        Decor(parent, PrimitiveType.Cube, mid + facing * 0.2f - Vector3.up * 0.25f, rot, new Vector3(len, 0.03f, 0.12f), ledMat, "CoveLight");
    }

    /// A beam between two points (ceiling grids, lintels)
    public static void Beam(Transform parent, Vector3 a, Vector3 b, float width, float depth, Material mat)
    {
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 0.5f) return;
        Decor(parent, PrimitiveType.Cube, (a + b) * 0.5f, Quaternion.LookRotation(d / len, Vector3.up), new Vector3(width, depth, len), mat, "Beam");
    }

    /// A ceiling light trough: a charcoal beam with a cream line of light along its underside
    public static void Trough(Transform parent, Vector3 a, Vector3 b, float width, float depth)
    {
        EnsureOffice();
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 0.5f) return;
        Quaternion rot = Quaternion.LookRotation(d / len, Vector3.up);
        Decor(parent, PrimitiveType.Cube, (a + b) * 0.5f, rot, new Vector3(width, depth, len), graphiteMat, "Trough");
        Decor(parent, PrimitiveType.Cube, (a + b) * 0.5f - Vector3.up * (depth * 0.5f + 0.015f), rot, new Vector3(width * 0.4f, 0.03f, len - 0.4f), ledMat, "TroughLight");
    }

    /// A window: a charcoal frame, an amber pane, brass mullion and transom, a stone sill
    public static void WindowPane(Transform parent, Vector3 center, Vector3 facing, float w, float h)
    {
        EnsureOffice();
        Vector3 forward = -facing.normalized;
        PlaceOnWall(ref center, ref forward, 0.02f);
        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
        Decor(parent, PrimitiveType.Cube, center - forward * 0.06f, rot, new Vector3(w + 0.24f, h + 0.24f, 0.12f), graphiteMat, "WindowFrame");
        Decor(parent, PrimitiveType.Cube, center - forward * 0.12f, rot, new Vector3(w, h, 0.04f), glassMat, "Pane");
        Decor(parent, PrimitiveType.Cube, center - forward * 0.15f, rot, new Vector3(0.08f, h, 0.06f), Brass, "Mullion");
        Decor(parent, PrimitiveType.Cube, center - forward * 0.15f + Vector3.up * (h * 0.18f), rot, new Vector3(w, 0.08f, 0.06f), Brass, "Transom");
        Decor(parent, PrimitiveType.Cube, center - forward * 0.2f - Vector3.up * (h * 0.5f + 0.06f), rot, new Vector3(w + 0.4f, 0.1f, 0.34f), Limestone, "Sill");
        Decor(parent, PrimitiveType.Cube, center - forward * 0.14f + Vector3.up * (h * 0.5f + 0.16f), rot, new Vector3(w + 0.5f, 0.12f, 0.26f), Limestone, "WindowCornice");
    }

    /// A stone wall with punched amber windows across a stretch of wall (where a glass curtain used to hang)
    public static void CurtainWall(Transform parent, Vector3 center, Vector3 facing, float w, float h)
    {
        EnsureOffice();
        Vector3 forward = -facing.normalized;
        PlaceOnWall(ref center, ref forward, 0.02f);
        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
        Vector3 right = rot * Vector3.right;
        int panes = Mathf.Max(1, Mathf.RoundToInt(w / 2.2f));
        int rows = Mathf.Max(1, Mathf.RoundToInt(h / 3f));
        Decor(parent, PrimitiveType.Cube, center - forward * 0.07f, rot, new Vector3(w, h, 0.1f), Limestone, "CurtainStone");
        for (int i = 0; i <= panes; i++)
            Decor(parent, PrimitiveType.Cube, center - forward * 0.15f + right * (-w * 0.5f + w * i / panes), rot, new Vector3(0.3f, h + 0.1f, 0.16f), Fluted, "Mullion");
        for (int i = 0; i < panes; i++)
            for (int r = 0; r < rows; r++)
            {
                float pw = w / panes - 0.7f, ph = h / rows - 0.9f;
                Vector3 p = center - forward * 0.13f + right * (-w * 0.5f + w * (i + 0.5f) / panes) + Vector3.up * (-h * 0.5f + h * (r + 0.5f) / rows);
                Decor(parent, PrimitiveType.Cube, p, rot, new Vector3(pw, ph, 0.03f), WindowAmber, "CurtainWindow");
                Decor(parent, PrimitiveType.Cube, p - forward * 0.01f, rot, new Vector3(0.05f, ph, 0.02f), graphiteMat, "WindowBar");
                Decor(parent, PrimitiveType.Cube, p - forward * 0.01f, rot, new Vector3(pw, 0.05f, 0.02f), graphiteMat, "WindowBar");
            }
        Decor(parent, PrimitiveType.Cube, center - forward * 0.16f + Vector3.up * (h * 0.5f), rot, new Vector3(w + 0.1f, 0.2f, 0.2f), Limestone, "Transom");
        Decor(parent, PrimitiveType.Cube, center - forward * 0.16f - Vector3.up * (h * 0.5f), rot, new Vector3(w + 0.1f, 0.2f, 0.2f), Limestone, "Transom");
    }

    /// A hanging cloth with the company mark, on a brass rod under the ceiling: oxblood with the mark in cream, or
    /// (`oxblood` false) cream with the mark in oxblood
    public static void Banner(Transform parent, Vector3 top, float w, float h, float ceilingY, bool oxblood)
    {
        EnsureOffice();
        Material cloth = oxblood ? bannerA : bannerB;
        Decor(parent, PrimitiveType.Cube, top - Vector3.up * (h * 0.5f), Quaternion.identity, new Vector3(w, h, 0.04f), cloth, "Banner");
        Decor(parent, PrimitiveType.Cylinder, top, Quaternion.Euler(0f, 0f, 90f), new Vector3(0.06f, w * 0.5f + 0.2f, 0.06f), Brass, "BannerRod");
        Decor(parent, PrimitiveType.Sphere, top + Vector3.right * (w * 0.5f + 0.24f), Quaternion.identity, Vector3.one * 0.12f, Brass, "BannerKnob");
        Decor(parent, PrimitiveType.Sphere, top - Vector3.right * (w * 0.5f + 0.24f), Quaternion.identity, Vector3.one * 0.12f, Brass, "BannerKnob");
        Cable(parent, top + new Vector3(-w * 0.4f, 0f, 0f), new Vector3(top.x - w * 0.4f, ceilingY, top.z), 0.03f);
        Cable(parent, top + new Vector3(w * 0.4f, 0f, 0f), new Vector3(top.x + w * 0.4f, ceilingY, top.z), 0.03f);
        Material ink = oxblood ? bannerB : bannerA;
        Emblem(parent, top - Vector3.up * (h * 0.42f) + Vector3.back * 0.05f, Quaternion.identity, w * 0.5f, ink, cloth);
        Decor(parent, PrimitiveType.Cube, top - Vector3.up * (h * 0.78f) + Vector3.back * 0.04f, Quaternion.identity, new Vector3(w * 0.6f, 0.06f, 0.01f), ink, "BannerRule");
    }

    /// A lit cream signboard hanging under the ceiling in a brass frame, with the mark and one of the lines
    public static void HoloBanner(Transform parent, Vector3 top, float w, float h, float ceilingY, int tone)
    {
        EnsureOffice();
        Cable(parent, top + new Vector3(-w * 0.4f, 0f, 0f), new Vector3(top.x - w * 0.4f, ceilingY, top.z), 0.02f);
        Cable(parent, top + new Vector3(w * 0.4f, 0f, 0f), new Vector3(top.x + w * 0.4f, ceilingY, top.z), 0.02f);
        Decor(parent, PrimitiveType.Cube, top, Quaternion.identity, new Vector3(w + 0.2f, 0.05f, 0.08f), Brass, "BannerRail");
        Decor(parent, PrimitiveType.Cube, top - Vector3.up * (h * 0.5f + 0.05f), Quaternion.identity, new Vector3(w + 0.1f, h + 0.1f, 0.05f), Brass, "SignFrame");
        Decor(parent, PrimitiveType.Cube, top - Vector3.up * (h * 0.5f + 0.05f), Quaternion.identity, new Vector3(w, h, 0.07f), LampCream, "SignBoard");
        Emblem(parent, top - Vector3.up * (h * 0.42f) + Vector3.back * 0.05f, Quaternion.identity, w * 0.45f, EmblemInk(), LampCream);
        Text(parent, top - Vector3.up * (h * 0.8f) + Vector3.back * 0.05f, Quaternion.identity, Taglines[Mathf.Abs(tone) % Taglines.Length], h * 0.014f, new Color(0.07f, 0.07f, 0.1f), "BannerLine", w - 0.3f, false, 40f, 90f);
    }

    /// A bench against a wall: the walnut seat is real (a step, a boost), the back is decor, bronze legs
    public static void Bench(Transform parent, Vector3 groundCenter, float yaw)
    {
        EnsureOffice();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        var seat = Slab(parent, groundCenter + Vector3.up * 0.45f, new Vector3(1.9f, 0.1f, 0.55f), WoodPanel, "BenchSeat");
        seat.transform.rotation = rot;
        Decor(parent, PrimitiveType.Cube, groundCenter + rot * new Vector3(0f, 0.78f, -0.25f), rot * Quaternion.Euler(-8f, 0f, 0f), new Vector3(1.9f, 0.5f, 0.07f), WoodPanel, "BenchBack");
        Decor(parent, PrimitiveType.Cube, groundCenter + rot * new Vector3(-0.8f, 0.2f, 0f), rot, new Vector3(0.08f, 0.4f, 0.5f), Bronze, "BenchLeg");
        Decor(parent, PrimitiveType.Cube, groundCenter + rot * new Vector3(0.8f, 0.2f, 0f), rot, new Vector3(0.08f, 0.4f, 0.5f), Bronze, "BenchLeg");
    }

    /// A lounge bench: a cream shell, an oxblood leather cushion (the seat is real)
    public static void Lounge(Transform parent, Vector3 groundCenter, float yaw)
    {
        EnsureOffice();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        var seat = Slab(parent, groundCenter + Vector3.up * 0.3f, new Vector3(2.0f, 0.36f, 0.7f), whiteGlossMat, "LoungeSeat");
        seat.transform.rotation = rot;
        Decor(parent, PrimitiveType.Cube, groundCenter + rot * new Vector3(0f, 0.53f, 0f), rot, new Vector3(1.9f, 0.1f, 0.62f), fabricMat, "Cushion");
        Decor(parent, PrimitiveType.Cube, groundCenter + rot * new Vector3(0f, 0.8f, -0.3f), rot * Quaternion.Euler(-10f, 0f, 0f), new Vector3(1.9f, 0.45f, 0.08f), fabricMat, "LoungeBack");
        Decor(parent, PrimitiveType.Cube, groundCenter + rot * new Vector3(0f, 0.1f, 0f), rot, new Vector3(1.8f, 0.04f, 0.6f), Brass, "LoungeFoot");
    }

    /// A pipe with flanges at both ends
    public static void Pipe(Transform parent, Vector3 from, Vector3 to, float thickness, Material mat = null)
    {
        EnsureOffice();
        mat = mat ?? ironMat;
        Vector3 d = to - from;
        float len = d.magnitude;
        if (len < 0.2f) return;
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, d / len);
        Decor(parent, PrimitiveType.Cylinder, (from + to) * 0.5f, rot, new Vector3(thickness, len * 0.5f, thickness), mat, "Pipe");
        Decor(parent, PrimitiveType.Cylinder, from + d / len * 0.08f, rot, new Vector3(thickness * 1.6f, 0.05f, thickness * 1.6f), mat, "Flange");
        Decor(parent, PrimitiveType.Cylinder, to - d / len * 0.08f, rot, new Vector3(thickness * 1.6f, 0.05f, thickness * 1.6f), mat, "Flange");
    }

    /// A brass valve wheel on a pipe; `axis` is the pipe's direction
    public static void Valve(Transform parent, Vector3 center, Vector3 axis)
    {
        EnsureOffice();
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, axis.normalized);
        Decor(parent, PrimitiveType.Cylinder, center, rot, new Vector3(0.55f, 0.03f, 0.55f), Brass, "ValveWheel");
        Decor(parent, PrimitiveType.Cylinder, center, rot, new Vector3(0.14f, 0.09f, 0.14f), Bronze, "ValveHub");
        Decor(parent, PrimitiveType.Cube, center, rot, new Vector3(0.5f, 0.05f, 0.04f), Brass, "Spoke");
        Decor(parent, PrimitiveType.Cube, center, rot * Quaternion.Euler(0f, 90f, 0f), new Vector3(0.5f, 0.05f, 0.04f), Brass, "Spoke");
    }

    /// A ventilation duct along the ceiling with grilles on its underside
    public static void Duct(Transform parent, Vector3 from, Vector3 to, float w, float h)
    {
        EnsureOffice();
        Vector3 d = to - from;
        float len = d.magnitude;
        if (len < 1f) return;
        Quaternion rot = Quaternion.LookRotation(d / len, Vector3.up);
        Decor(parent, PrimitiveType.Cube, (from + to) * 0.5f, rot, new Vector3(w, h, len), ironMat, "Duct");
        int vents = Mathf.Max(1, Mathf.RoundToInt(len / 9f));
        for (int i = 0; i < vents; i++)
        {
            Vector3 p = Vector3.Lerp(from, to, (i + 0.5f) / vents) - Vector3.up * (h * 0.5f + 0.03f);
            Decor(parent, PrimitiveType.Cube, p, rot, new Vector3(w * 0.6f, 0.04f, 1.2f), satinMat, "Vent");
            for (int k = -1; k <= 1; k++)
                Decor(parent, PrimitiveType.Cube, p - Vector3.up * 0.03f + rot * Vector3.forward * (k * 0.35f), rot, new Vector3(w * 0.5f, 0.02f, 0.08f), ironMat, "VentBar");
        }
    }

    /// A floor grate: a dark recess with iron bars across it
    public static void Grate(Transform parent, Vector3 groundCenter, Vector2 size)
    {
        EnsureOffice();
        Decor(parent, PrimitiveType.Cube, groundCenter + Vector3.up * 0.015f, Quaternion.identity, new Vector3(size.x, 0.03f, size.y), satinMat, "Grate");
        int bars = Mathf.Clamp(Mathf.RoundToInt(size.y / 0.3f), 2, 6);
        for (int i = 0; i < bars; i++)
            Decor(parent, PrimitiveType.Cube, groundCenter + new Vector3(0f, 0.04f, (i + 0.5f) / bars * size.y - size.y * 0.5f), Quaternion.identity, new Vector3(size.x, 0.03f, 0.08f), ironMat, "GrateBar");
    }

    /// A scaffold tower: iron poles and a wooden plank at `height` you can stand on
    public static void Scaffold(Transform parent, Vector3 groundCenter, float height, float w = 2.4f, float d = 1.6f)
    {
        EnsureOffice();
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Decor(parent, PrimitiveType.Cylinder, groundCenter + new Vector3(sx * w * 0.5f, height * 0.5f + 0.4f, sz * d * 0.5f), Quaternion.identity, new Vector3(0.08f, height * 0.5f + 0.4f, 0.08f), ironMat, "Pole");
        Decor(parent, PrimitiveType.Cube, groundCenter + new Vector3(0f, height * 0.5f, d * 0.5f), Quaternion.Euler(0f, 0f, 32f), new Vector3(w * 1.1f, 0.06f, 0.06f), ironMat, "Brace");
        Decor(parent, PrimitiveType.Cube, groundCenter + new Vector3(0f, height * 0.5f, -d * 0.5f), Quaternion.Euler(0f, 0f, -32f), new Vector3(w * 1.1f, 0.06f, 0.06f), ironMat, "Brace");
        var plank = Pillar(parent, groundCenter + Vector3.up * (height - 0.05f), new Vector3(w + 0.3f, 0.1f, d + 0.2f), woodMat);
        plank.name = "ScaffoldPlank";
        Decor(parent, PrimitiveType.Cube, groundCenter + new Vector3(0f, height + 0.45f, -d * 0.5f - 0.1f), Quaternion.identity, new Vector3(w + 0.3f, 0.05f, 0.05f), ironMat, "GuardRail");
    }

    /// A chain hanging from a ceiling with a hook on the end
    public static void Chain(Transform parent, Vector3 top, float length)
    {
        EnsureOffice();
        Cable(parent, top, top - Vector3.up * length, 0.05f);
        Decor(parent, PrimitiveType.Cube, top - Vector3.up * (length + 0.12f), Quaternion.Euler(0f, 0f, 20f), new Vector3(0.05f, 0.26f, 0.05f), ironMat, "Hook");
        Decor(parent, PrimitiveType.Cube, top - Vector3.up * (length + 0.24f), Quaternion.identity, new Vector3(0.16f, 0.05f, 0.05f), ironMat, "HookTip");
    }

    /// A rust streak running down a wall
    public static void Streak(Transform parent, Vector3 wallTop, Vector3 facing, float length)
    {
        EnsureOffice();
        Quaternion rot = Quaternion.LookRotation(facing, Vector3.up);
        Decor(parent, PrimitiveType.Cube, wallTop + facing * 0.025f - Vector3.up * (length * 0.5f), rot, new Vector3(0.35f, length, 0.03f), rustMat, "Streak");
    }

    // ------------------------------------------------------------ the company's eyes

    static Material redHoloMat;
    static Material RedHolo() => redHoloMat ??= Transparent(new Color(1f, 0.25f, 0.2f, 0.35f));

    /// A drone on patrol: a disc with a red eye and a soft light, looping round its post.
    /// Cream and brass for the city, charcoal for the rot.
    public static void Drone(Transform parent, Vector3 pos, float radius, bool rot)
    {
        EnsureOffice();
        var drone = new GameObject(rot ? "RotDrone" : "CivicDrone");
        drone.transform.SetParent(parent);
        drone.transform.position = pos;
        Material shell = rot ? graphiteMat : whiteGlossMat;
        Decor(drone.transform, PrimitiveType.Cylinder, pos, Quaternion.identity, new Vector3(0.7f, 0.06f, 0.7f), shell, "Body");
        Decor(drone.transform, PrimitiveType.Cylinder, pos + Vector3.up * 0.065f, Quaternion.identity, new Vector3(0.5f, 0.01f, 0.5f), Brass, "BodyRing");
        Decor(drone.transform, PrimitiveType.Sphere, pos - Vector3.up * 0.06f, Quaternion.identity, new Vector3(0.36f, 0.2f, 0.36f), satinMat, "Belly");
        Decor(drone.transform, PrimitiveType.Sphere, pos + new Vector3(0f, -0.07f, 0.2f), Quaternion.identity, Vector3.one * 0.08f, dotMat, "Eye");
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            Decor(drone.transform, PrimitiveType.Cylinder, pos + new Vector3(Mathf.Cos(a) * 0.42f, 0.045f, Mathf.Sin(a) * 0.42f), Quaternion.identity, new Vector3(0.22f, 0.008f, 0.22f), rot ? amberMat : ledMat, "Rotor");
        }
        var l = drone.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 7f;
        l.intensity = 1f;
        l.color = rot ? new Color(1f, 0.25f, 0.15f) : new Color(1f, 0.88f, 0.68f);
        l.shadows = LightShadows.None;
        var drift = drone.AddComponent<DroneDrift>();
        drift.origin = pos;
        drift.radius = radius;
        drift.speed = Random.Range(0.25f, 0.45f);
        drift.phase = Random.value * 6.28f;
    }

    /// A frosted glass column with a cream core that breathes, in a brass cap and a stone base
    public static void DataPillar(Transform parent, Vector3 groundCenter, float height)
    {
        EnsureOffice();
        Decor(parent, PrimitiveType.Cylinder, groundCenter + Vector3.up * 0.08f, Quaternion.identity, new Vector3(0.7f, 0.08f, 0.7f), Limestone, "PillarBase");
        Decor(parent, PrimitiveType.Cylinder, groundCenter + Vector3.up * (height * 0.5f + 0.16f), Quaternion.identity, new Vector3(0.5f, height * 0.5f, 0.5f), frostMat, "PillarGlass");
        var core = Decor(parent, PrimitiveType.Cylinder, groundCenter + Vector3.up * (height * 0.5f + 0.16f), Quaternion.identity, new Vector3(0.12f, height * 0.5f - 0.1f, 0.12f), ledMat, "PillarCore");
        core.AddComponent<PulseGlow>().phase = Random.value * 6.28f;
        Decor(parent, PrimitiveType.Cylinder, groundCenter + Vector3.up * (height + 0.2f), Quaternion.identity, new Vector3(0.6f, 0.05f, 0.6f), Brass, "PillarCap");
        Decor(parent, PrimitiveType.Cylinder, groundCenter + Vector3.up * 0.2f, Quaternion.identity, new Vector3(0.58f, 0.04f, 0.58f), Brass, "PillarCollar");
    }

    /// A lit ring in the floor with a charcoal core
    public static void FloorRing(Transform parent, Vector3 groundCenter, float radius, Material glow = null)
    {
        EnsureOffice();
        var ring = Decor(parent, PrimitiveType.Cylinder, groundCenter + Vector3.up * 0.012f, Quaternion.identity, new Vector3(radius * 2f, 0.012f, radius * 2f), glow ?? ledMat, "FloorRing");
        Decor(parent, PrimitiveType.Cylinder, groundCenter + Vector3.up * 0.035f, Quaternion.identity, new Vector3(radius * 2f - 0.3f, 0.015f, radius * 2f - 0.3f), graphiteMat, "FloorRingCore");
        ring.AddComponent<PulseGlow>().phase = Random.value * 6.28f;
    }

    /// Lit chevrons on the floor pointing along `dir`
    public static void Chevrons(Transform parent, Vector3 groundStart, Vector3 dir, int count, Material glow = null)
    {
        EnsureOffice();
        dir.y = 0f;
        dir.Normalize();
        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
        for (int i = 0; i < count; i++)
        {
            Vector3 p = groundStart + dir * (i * 1.6f) + Vector3.up * 0.03f;
            Decor(parent, PrimitiveType.Cube, p + rot * new Vector3(-0.35f, 0f, -0.35f), rot * Quaternion.Euler(0f, 45f, 0f), new Vector3(0.12f, 0.012f, 1.0f), glow ?? ledMat, "Chevron");
            Decor(parent, PrimitiveType.Cube, p + rot * new Vector3(0.35f, 0f, -0.35f), rot * Quaternion.Euler(0f, -45f, 0f), new Vector3(0.12f, 0.012f, 1.0f), glow ?? ledMat, "Chevron");
        }
    }

    /// A lit sign in a brass frame, DARK DECO: cream block letters carrying the light on a charcoal plate between brass
    /// rules; `red` makes it an oxblood warning plate with red rules. Steady — a sign box, not a hologram: the plate is
    /// a plain look (welded), only the words ever change (the room gates, a StateSign). `facing` is the way it faces.
    public static void HoloSign(Transform parent, Vector3 center, Vector3 facing, float w, float h, string line, bool red = false)
    {
        EnsureOffice();
        Quaternion rot = Quaternion.LookRotation(-facing.normalized, Vector3.up);
        holoInkMat ??= Unlit(Shader.Find("Universal Render Pipeline/Unlit"), new Color(0.5f, 0.12f, 0.1f));    // the oxblood plate
        Material plate = red ? holoInkMat : graphiteMat;   // charcoal, lit by the room: the letters are what glows
        Decor(parent, PrimitiveType.Cube, center, rot, new Vector3(w, h, 0.05f), plate, "HoloSign");
        Decor(parent, PrimitiveType.Cube, center - Vector3.up * (h * 0.5f + 0.03f), rot, new Vector3(w + 0.1f, 0.04f, 0.06f), red ? dotMat : Brass, "HoloEdge");
        Decor(parent, PrimitiveType.Cube, center + Vector3.up * (h * 0.5f + 0.03f), rot, new Vector3(w + 0.1f, 0.04f, 0.06f), red ? dotMat : Brass, "HoloEdge");
        Decor(parent, PrimitiveType.Cube, center + rot * Vector3.right * (w * 0.5f + 0.03f), rot, new Vector3(0.04f, h + 0.1f, 0.06f), red ? dotMat : Brass, "HoloEdge");
        Decor(parent, PrimitiveType.Cube, center - rot * Vector3.right * (w * 0.5f + 0.03f), rot, new Vector3(0.04f, h + 0.1f, 0.06f), red ? dotMat : Brass, "HoloEdge");
        Text(parent, center - rot * Vector3.forward * 0.04f, rot, line, h * 0.03f, red ? new Color(1.8f, 1.6f, 1.3f) : SignCream, "HoloText", w - 0.3f, true, 160f, 280f);
    }

    /// A lantern in the ceiling: a cream pane in a stepped brass frame with a warm light under it
    public static void Skylight(Transform parent, Vector3 center, Vector2 size)
    {
        EnsureOffice();
        var pane = Decor(parent, PrimitiveType.Cube, center - Vector3.up * 0.1f, Quaternion.identity, new Vector3(size.x, 0.06f, size.y), ledMat, "Skylight");
        pane.AddComponent<PulseGlow>().depth = 0.08f;
        Decor(parent, PrimitiveType.Cube, center - Vector3.up * 0.04f, Quaternion.identity, new Vector3(size.x + 0.3f, 0.08f, size.y + 0.3f), Brass, "SkylightFrame");
        Decor(parent, PrimitiveType.Cube, center - Vector3.up * 0.16f, Quaternion.identity, new Vector3(size.x + 0.6f, 0.06f, size.y + 0.6f), graphiteMat, "SkylightStep");
        Decor(parent, PrimitiveType.Cube, center - Vector3.up * 0.145f, Quaternion.identity, new Vector3(0.08f, 0.04f, size.y), Brass, "SkylightBar");
        Decor(parent, PrimitiveType.Cube, center - Vector3.up * 0.145f, Quaternion.identity, new Vector3(size.x, 0.04f, 0.08f), Brass, "SkylightBar");
        CoolLight(parent, center - Vector3.up * 0.8f, Mathf.Max(size.x, size.y) * 1.6f, 1.6f, "Skylight");
        Shaft(parent, center, size); // the light itself, hanging under the glass
    }

    /// A fluted frosted fin on a wall with a warm lit edge that breathes
    public static void GlassFin(Transform parent, Vector3 wallBase, Vector3 facing, float height)
    {
        EnsureOffice();
        Quaternion rot = Quaternion.LookRotation(facing, Vector3.up);
        Decor(parent, PrimitiveType.Cube, wallBase + facing * 0.17f + Vector3.up * (height * 0.5f), rot, new Vector3(0.08f, height, 0.3f), frostMat, "GlassFin");
        Decor(parent, PrimitiveType.Cube, wallBase + facing * 0.03f + Vector3.up * (height * 0.5f), rot, new Vector3(0.14f, height + 0.1f, 0.04f), Brass, "FinFoot");
        var edge = Decor(parent, PrimitiveType.Cube, wallBase + facing * 0.335f + Vector3.up * (height * 0.5f), rot, new Vector3(0.03f, height - 0.2f, 0.02f), ledMat, "FinEdge");
        edge.AddComponent<PulseGlow>().depth = 0.15f;
    }

    /// A bundle of three lit lines running between two points, in brass conduit
    public static void CableRun(Transform parent, Vector3 from, Vector3 to, Material glow)
    {
        EnsureOffice();
        Vector3 d = to - from;
        float len = d.magnitude;
        if (len < 0.5f) return;
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, d / len);
        Vector3 side = Vector3.Cross(d / len, Vector3.up);
        if (side.sqrMagnitude < 0.01f) side = Vector3.right;
        side.Normalize();
        for (int i = -1; i <= 1; i++)
            Decor(parent, PrimitiveType.Cylinder, (from + to) * 0.5f + side * (i * 0.07f), rot, new Vector3(0.03f, len * 0.5f, 0.03f), glow ?? ledMat, "DataLine");
        for (float s = 2f; s < len - 1f; s += 6f)
            Decor(parent, PrimitiveType.Cube, from + d / len * s, Quaternion.LookRotation(d / len, Vector3.up), new Vector3(0.28f, 0.1f, 0.14f), Brass, "ConduitClip");
    }

    /// An oxblood enamel warning plate on a wall
    public static void HazardHolo(Transform parent, Vector3 center, Vector3 facing, float w, float h, string line)
    {
        Vector3 forward = -facing.normalized;
        PlaceOnWall(ref center, ref forward, 0.1f);
        HoloSign(parent, center - forward * 0.05f, -forward, w, h, line, true);
    }

    /// A chemical drum: a real cylinder to boost off, iron-banded, rust or mustard
    public static void Drum(Transform parent, Vector3 groundPos, bool yellow)
    {
        EnsureOffice();
        var body = Solid(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * 0.6f, Quaternion.identity, new Vector3(0.9f, 0.6f, 0.9f),
            yellow ? MustardPaint : rustMat, "Drum");
        Decor(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * 0.3f, Quaternion.identity, new Vector3(0.93f, 0.025f, 0.93f), ironMat, "DrumBand");
        Decor(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * 0.9f, Quaternion.identity, new Vector3(0.93f, 0.025f, 0.93f), ironMat, "DrumBand");
        Decor(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * 1.205f, Quaternion.identity, new Vector3(0.86f, 0.01f, 0.86f), satinMat, "DrumLid");
    }

    /// A wet-floor sign: two mustard boards leaning on each other
    public static void WetFloorSign(Transform parent, Vector3 groundPos, float yaw)
    {
        EnsureOffice();
        Material board = MustardPaint;
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0f, 0.42f, 0.14f), rot * Quaternion.Euler(-18f, 0f, 0f), new Vector3(0.5f, 0.85f, 0.03f), board, "WetFloor");
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0f, 0.42f, -0.14f), rot * Quaternion.Euler(18f, 0f, 0f), new Vector3(0.5f, 0.85f, 0.03f), board, "WetFloor");
        Text(parent, groundPos + rot * new Vector3(0f, 0.5f, 0.3f), rot * Quaternion.Euler(-18f, 180f, 0f), "WET FLOOR", 0.012f, new Color(0.05f, 0.05f, 0.06f), "WetFloorText", 0.42f, true, 20f, 45f);
    }

    /// A console: a charcoal block (a real box) with a tilted dark dial board in brass and red readouts
    public static void Terminal(Transform parent, Vector3 groundPos, float yaw)
    {
        EnsureOffice();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        var body = Pillar(parent, groundPos + Vector3.up * 0.55f, new Vector3(0.9f, 1.1f, 0.6f), graphiteMat);
        body.name = "Terminal";
        body.transform.rotation = rot;
        Quaternion tilt = rot * Quaternion.Euler(-35f, 0f, 0f);
        Vector3 screenAt = groundPos + rot * new Vector3(0f, 1.2f, 0.05f);
        Decor(parent, PrimitiveType.Cube, screenAt, tilt, new Vector3(0.8f, 0.4f, 0.04f), Brass, "TerminalScreen");
        Decor(parent, PrimitiveType.Cube, screenAt + tilt * Vector3.forward * -0.03f, tilt, new Vector3(0.72f, 0.32f, 0.01f), screenMat, "TerminalGlass");
        for (int i = 0; i < 3; i++)
            Decor(parent, PrimitiveType.Cube, screenAt + tilt * Vector3.forward * -0.045f + tilt * new Vector3(-0.22f + i * 0.22f, 0.08f - i * 0.06f, 0f), tilt, new Vector3(0.16f, 0.02f, 0.005f), dotMat, "Readout");
        Decor(parent, PrimitiveType.Cylinder, screenAt + tilt * Vector3.forward * -0.045f + tilt * new Vector3(0.25f, -0.08f, 0f), tilt * Quaternion.Euler(90f, 0f, 0f), new Vector3(0.1f, 0.005f, 0.1f), paperMat, "TerminalDial");
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0f, 0.3f, 0.31f), rot, new Vector3(0.5f, 0.05f, 0.012f), amberMat, "TerminalLamp");
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0f, 1.105f, 0f), rot, new Vector3(0.94f, 0.015f, 0.64f), Brass, "TerminalEdge");
    }
}
