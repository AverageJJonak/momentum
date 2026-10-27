using UnityEngine;

/// <summary>
/// THE LIVING SET. The civic levels keep gardens in their halls: walnut on
/// the slides, trees, hedges, pools, clouds over the open rooms. These are the
/// surfaces that make them read as what they are (planks with their gaps and
/// end joints, bark with its fissures, leaves in overlapping clusters, water
/// with a ripple that moves) and the two builders that were the fakest things
/// in the game: a tree that is a trunk with a lean, branches, and a crown of
/// twenty leaf clusters welded into one mesh, and a cloud that is a slate
/// cumulus with a sepia belly, painted once and turned to face you, not three
/// white balls. DARK DECO: the water is black, the clouds slate, the stars
/// few and warm.
/// </summary>
public static partial class LevelKit
{
    // ---------------------------------------------------------------- the height fields

    /// Decking: four planks per repeat running along x, a gap between, a rounded edge into it, an end joint in each run
    static float WoodHeight(int x, int y, int n)
    {
        int plank = n / 4;
        int row = FloorDiv(y, plank);
        float gap = Mathf.Min(y % plank, plank - y % plank);
        float grain = Wrapped(x, (y * 5) % n, n, n / 10, 141);   // stretched along the plank
        float h = 0.5f + (grain - 0.5f) * 0.12f + (MenuArt.Hash01(row & 3, 0, 142) - 0.5f) * 0.06f;
        int seg = n / 2;
        int ex = ((x + (row & 3) * seg / 3) % seg + seg) % seg;
        float end = Mathf.Min(ex, seg - ex);
        if (gap < n / 200f + 0.5f || end < n / 256f + 0.5f) return 0.15f;
        if (gap < n / 64f) h -= (n / 64f - gap) / (n / 64f) * 0.12f;
        return h;
    }

    /// Bark: ridges running up the trunk, fissures between them, a fine grain over all of it
    static float BarkHeight(int x, int y, int n)
    {
        float r = Wrapped((x * 3) % n, y, n, n / 6, 131);
        float fine = Wrapped((x * 2) % n, y, n, n / 24, 132);
        float h = 0.3f + r * 0.5f + (fine - 0.5f) * 0.16f;
        if (r < 0.35f) h -= (0.35f - r) * 1.2f;
        return h;
    }

    /// Leaves: a jittered grid of overlapping discs, each tilted its own way, the gaps between them deep
    static float LeafHeight(int x, int y, int n)
    {
        int cell = n / 8, cells = n / cell;
        float best = 0f;
        int cx0 = FloorDiv(x, cell), cy0 = FloorDiv(y, cell);
        for (int j = -1; j <= 1; j++)
            for (int i = -1; i <= 1; i++)
            {
                int cx = cx0 + i, cy = cy0 + j;
                int wx = ((cx % cells) + cells) % cells, wy = ((cy % cells) + cells) % cells;
                float ox = (cx + 0.2f + MenuArt.Hash01(wx, wy, 121) * 0.6f) * cell, oy = (cy + 0.2f + MenuArt.Hash01(wx, wy, 122) * 0.6f) * cell;
                float r = cell * (0.55f + 0.35f * MenuArt.Hash01(wx, wy, 123));
                float dx = x - ox, dy = y - oy;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
                if (d >= 1f) continue;
                float lean = MenuArt.Hash01(wx, wy, 124) - 0.5f;
                best = Mathf.Max(best, 0.35f + (1f - d) * 0.5f + dx / r * lean * 0.3f);
            }
        return best + (Wrapped(x, y, n, n / 64, 125) - 0.5f) * 0.04f;
    }

    /// Water at rest: five crossing swells and a ripple, all of them whole waves per repeat so the sheet tiles
    static float WaterHeight(int x, int y, int n)
    {
        float u = x * 6.2831853f / n, v = y * 6.2831853f / n;
        float h = 0.5f;
        h += 0.10f * Mathf.Sin(u * 2f + v + 0.3f);
        h += 0.07f * Mathf.Sin(-u + v * 3f + 1.7f);
        h += 0.05f * Mathf.Sin(u * 4f + v * 3f + 2.9f);
        h += 0.035f * Mathf.Sin(-u * 5f + v * 2f + 4.1f);
        h += 0.025f * Mathf.Sin(u * 7f - v * 6f + 0.9f);
        h += (Wrapped(x, y, n, n / 16, 151) - 0.5f) * 0.08f;
        return h;
    }

    // ---------------------------------------------------------------- water

    /// Water: a transparent sheet with a rippled normal map that WaterFlow drifts. On the LitTransparentNormal
    /// template when it is there; plain glossy glass when it is not.
    public static Material WaterSurface(Color tint, float smoothness = 0.95f, float metresPerRepeat = 3f, float bumpScale = 0.6f)
    {
        bool full = Template("LitTransparentNormal") != null;
        var m = FromTemplate(full ? "LitTransparentNormal" : "LitTransparent", "Universal Render Pipeline/Lit");
        m.SetColor("_BaseColor", tint);
        m.SetFloat("_Smoothness", smoothness);
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        if (full)
        {
            m.SetTexture("_BumpMap", NormalMap("Water", 256, WaterHeight, 3f));
            m.SetFloat("_BumpScale", bumpScale);
            m.EnableKeyword("_NORMALMAP");
        }
        repeatOf[m] = metresPerRepeat;
        return m;
    }

    // ---------------------------------------------------------------- clouds

    /// A cumulus, painted: a dozen lobes heaped on a body, more of them on top, their edges warped by noise and
    /// their rims soft; slate (the bible's 0.18, 0.2, 0.26) lit a little from above, its underside sepia where the
    /// warm horizon reaches it and darker where it is thick. Levels tint it further (SetCloudTint) over white.
    static Texture2D CloudSprite(int seed)
    {
        string key = "Cloud" + seed;
        if (surfaces.TryGetValue(key, out var t) && t != null) return t;
        const int n = 256;
        t = new Texture2D(n, n, TextureFormat.RGBA32, true, false) { name = "Surface_" + key, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.HideAndDontSave };
        int lobes = 11 + seed % 4;
        var cx = new float[lobes]; var cy = new float[lobes]; var r = new float[lobes];
        for (int i = 0; i < lobes; i++)
        {
            float a = MenuArt.Hash01(i, 0, seed) * 6.2832f, k = MenuArt.Hash01(i, 1, seed);
            float ex = 0.26f * Mathf.Sqrt(k), ey = 0.11f * Mathf.Sqrt(k);
            cx[i] = n * (0.5f + Mathf.Cos(a) * ex);
            cy[i] = n * (0.5f + Mathf.Sin(a) * ey + (Mathf.Sin(a) > 0f ? 0.05f : -0.02f));
            r[i] = n * (0.09f + 0.1f * MenuArt.Hash01(i, 2, seed) + (Mathf.Sin(a) > 0f ? 0.03f : 0f));
        }
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float wx = x + (Noise(x, y, 22, seed * 5 + 1) - 0.5f) * 26f, wy = y + (Noise(x, y, 22, seed * 5 + 2) - 0.5f) * 22f;
                float field = 0f, depth = 0f;
                for (int i = 0; i < lobes; i++)
                {
                    float dx = (wx - cx[i]) / r[i], dy = (wy - cy[i]) / r[i];
                    float d = 1f - (dx * dx + dy * dy);
                    if (d > field) field = d;
                    if (d > 0f) depth += d;
                }
                float edge = (Noise(x, y, 6, seed * 5 + 3) - 0.5f) * 0.22f;
                float a = Mathf.Clamp01((field + edge - 0.04f) / 0.26f);
                a = a * a * (3f - 2f * a);
                float rim = Mathf.Min(Mathf.Min(x, n - 1 - x), Mathf.Min(y, n - 1 - y)) / (n * 0.06f);
                a *= Mathf.Clamp01(rim);
                float up = Mathf.Clamp01((y - n * 0.3f) / (n * 0.45f));
                float thick = Mathf.Clamp01(depth * 0.5f);
                Color c = up > 0.5f ? Color.Lerp(CloudBody, CloudTop, (up - 0.5f) * 2f) : Color.Lerp(CloudUnder, CloudBody, up * 2f);
                c = Color.Lerp(c, CloudDeep, thick * (1f - up) * 0.7f);
                px[y * n + x] = new Color(c.r, c.g, c.b, a);
            }
        t.SetPixels32(px);
        t.Apply(true, true);
        surfaces[key] = t;
        return t;
    }

    // The cloud's paint: slate lit at the crown, the body the bible's slate, a sepia belly, near-black where it is thick
    static readonly Color CloudTop = new Color(0.36f, 0.37f, 0.42f), CloudBody = new Color(0.18f, 0.2f, 0.26f), CloudUnder = new Color(0.3f, 0.22f, 0.14f), CloudDeep = new Color(0.15f, 0.11f, 0.07f);

    /// An unlit, transparent, textured material for a sprite (a cloud): the template's blend, the texture's alpha
    static Material SpriteMat(Texture2D tex)
    {
        var m = FromTemplate("UnlitTransparent", "Universal Render Pipeline/Unlit");
        m.SetColor("_BaseColor", Color.white);
        m.SetTexture("_BaseMap", tex);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.renderQueue = QSmog - 3;   // after the stars, before the smog: the smog lies under the clouds, never over them
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        return m;
    }

    /// A camera-facing sprite: a quad with no collider that turns to the camera every frame (Billboard)
    static GameObject Sprite(Transform parent, Vector3 pos, float size, Material mat, string name)
    {
        var go = Prim(parent, pos, new Vector3(size, size, 1f), mat, name, PrimitiveType.Quad);
        go.AddComponent<Billboard>();
        return go;
    }

    /// Clouds: painted cumulus in threes (a body and two puffs), turned to face you, drifting slowly across the
    /// sky. Sized to the sky they are in: a room's worth over a hall, half a kilometre over a district.
    public static void Clouds(Transform parent, Vector3 center, float spreadX, float spreadZ, float y, int count = 6)
    {
        // A room's open sky asks for clouds a few metres over its walls: those go up where weather is, and grow to it
        bool room = spreadX < 200f;
        if (room) { spreadX = Mathf.Max(spreadX * 12f, 700f); spreadZ = Mathf.Max(spreadZ * 12f, 700f); y += 260f; count = Mathf.Max(count, 9); }
        float size = room ? 1f : Mathf.Max(1f, spreadX / 2600f);
        for (int i = 0; i < count; i++)
        {
            var cloud = new GameObject("Cloud");
            cloud.transform.SetParent(parent);
            // Two decks: every other cloud rides low and big and drifts faster, the rest high, small and slow, so the
            // sky has parallax in it
            bool low = i % 2 == 0;
            float lift = low ? Random.Range(-40f, 10f) : Random.Range(50f, 110f);
            cloud.transform.position = center + new Vector3(Random.Range(-spreadX, spreadX), y + lift * size, Random.Range(-spreadZ, spreadZ));
            float s = (low ? Random.Range(340f, 560f) : Random.Range(220f, 400f)) * size;
            Vector3 p = cloud.transform.position;
            Sprite(cloud.transform, p, s, cloudMats[i % cloudMats.Length], "Body");
            int puffs = 4 + i % 3;
            for (int k = 0; k < puffs; k++)
            {
                float side = (k % 2 == 0 ? 1f : -1f) * Random.Range(0.28f, 0.55f) * s;
                Vector3 off = new Vector3(side, Random.Range(-0.08f, 0.1f) * s, Random.Range(-0.2f, 0.2f) * s);
                Sprite(cloud.transform, p + off, s * Random.Range(0.42f, 0.7f), cloudMats[(i + k + 1) % cloudMats.Length], "Puff");
            }
            var drift = cloud.AddComponent<CloudDrift>();
            drift.speed = (low ? Random.Range(1.2f, 2f) : Random.Range(0.45f, 0.8f)) * size;
            drift.lane = spreadX + s;
            drift.origin = cloud.transform.position;
        }
    }

    // ---------------------------------------------------------------- trees

    /// A tree: a trunk you can boost off (the post keeps its collider), two leaning segments above it, five
    /// branches into a crown of twenty leaf clusters, the upper ones in the light and the lower in shade, all
    /// but the post welded into one mesh per material. Dried blossom (sepia) or leaves (dark sepia-olive): nothing green.
    public static void Tree(Transform parent, Vector3 groundPos, float height = 5f, bool blossom = true)
    {
        float h = height, d0 = h * 0.07f;
        var baker = new MeshBaker();
        var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.name = "Trunk";
        post.transform.SetParent(parent);
        post.transform.position = groundPos + Vector3.up * (h * 0.19f);
        post.transform.localScale = new Vector3(d0, h * 0.19f, d0);
        post.GetComponent<MeshRenderer>().sharedMaterial = trunkMat;
        Vector3 a = groundPos + Vector3.up * (h * 0.36f);
        Vector3 b = a + (Vector3.up + new Vector3(Random.Range(-0.12f, 0.12f), 0f, Random.Range(-0.12f, 0.12f))).normalized * (h * 0.26f);
        Vector3 c = b + (Vector3.up + new Vector3(Random.Range(-0.2f, 0.2f), 0f, Random.Range(-0.2f, 0.2f))).normalized * (h * 0.24f);
        Segment(baker, trunkMat, a, b, d0 * 0.8f);
        Segment(baker, trunkMat, b, c, d0 * 0.55f);
        float yaw0 = Random.Range(0f, 360f);
        for (int i = 0; i < 5; i++)
        {
            float yaw = (yaw0 + i * 72f + Random.Range(-20f, 20f)) * Mathf.Deg2Rad, elev = Random.Range(30f, 55f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(elev) * Mathf.Cos(yaw), Mathf.Sin(elev), Mathf.Cos(elev) * Mathf.Sin(yaw));
            Vector3 from = Vector3.Lerp(a, c, Random.Range(0.35f, 0.9f));
            Segment(baker, trunkMat, from, from + dir * (h * Random.Range(0.22f, 0.32f)), d0 * 0.3f);
        }
        Vector3 crown = groundPos + Vector3.up * (h * 0.72f);
        Vector3 radii = new Vector3(h * 0.42f, h * 0.3f, h * 0.42f);
        Material light = blossom ? blossomMat : leafMat, dark = blossom ? blossomDarkMat : leafDarkMat;
        for (int i = 0; i < 14; i++)
        {
            Vector3 u = Random.onUnitSphere; u.y *= 0.7f;
            Vector3 p = crown + Vector3.Scale(u.normalized * Random.Range(0.3f, 0.85f), radii);
            baker.Add(p.y > crown.y + h * 0.02f ? light : dark, p, Vector3.one * (h * Random.Range(0.2f, 0.34f)), null, PrimitiveType.Sphere);
        }
        for (int i = 0; i < 6; i++)   // the ragged edge
        {
            Vector3 p = crown + Vector3.Scale(Random.onUnitSphere, radii) * 0.95f;
            baker.Add(Random.value < 0.5f ? light : dark, p, Vector3.one * (h * Random.Range(0.1f, 0.16f)), null, PrimitiveType.Sphere);
        }
        baker.Flush(parent, "Tree", true);
    }

    /// One cylinder from `a` to `b`, welded
    static void Segment(MeshBaker baker, Material mat, Vector3 a, Vector3 b, float dia)
    {
        Vector3 d = b - a;
        baker.Add(mat, (a + b) * 0.5f, new Vector3(dia, d.magnitude * 0.5f + dia * 0.3f, dia), Quaternion.FromToRotation(Vector3.up, d.normalized), PrimitiveType.Cylinder);
    }
}

public static partial class LevelKit
{
    /// The clouds' colour for this level's sky, over the painted slate: white leaves them as painted, a warm tint lights
    /// them from the sunset's side; after dark they want a tint near (0.6, 0.55, 0.5), not the sky's own dark, or they vanish
    public static void SetCloudTint(Color c)
    {
        if (cloudMats == null) return;
        foreach (var m in cloudMats) if (m != null) m.SetColor("_BaseColor", c);
    }

    /// A night's stars: a few thousand points on a sky-sized dome seen from inside, added to the sky through the
    /// transparent queue so the towers still stand in front of them. StarDome keeps it round the camera
    public static void Stars(Transform parent, float radius = 9000f)
    {
        var m = FromTemplate("UnlitTransparent", "Universal Render Pipeline/Unlit");
        m.SetColor("_BaseColor", Color.white);
        m.SetTexture("_BaseMap", StarField());
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        m.SetInt("_ZWrite", 0);
        m.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Front);
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent - 10;
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        var go = Prim(parent, Vector3.zero, Vector3.one * radius, m, "Stars", PrimitiveType.Sphere);
        go.AddComponent<StarDome>();
    }

    /// The star field: black and clear, two thousand points of one to three texels, dim and warm (a few stars drawn on
    /// the indigo, not a planetarium), a few of them brighter
    static Texture2D StarField()
    {
        const string key = "Stars";
        if (surfaces.TryGetValue(key, out var t) && t != null) return t;
        const int n = 1024;
        t = new Texture2D(n, n, TextureFormat.RGBA32, true, false) { name = "Surface_" + key, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
        var px = new Color32[n * n];
        for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 0);
        var rng = new System.Random(77);
        for (int s = 0; s < 2000; s++)
        {
            int x = rng.Next(n), y = rng.Next(n);
            float bright = 0.2f + (float)rng.NextDouble() * 0.5f;
            bool big = rng.Next(14) == 0;
            float warm = (float)rng.NextDouble();
            var c = new Color(bright, bright * (0.86f + warm * 0.1f), bright * (0.66f + (1f - warm) * 0.2f), bright);
            px[y * n + x] = c;
            if (!big) continue;
            var half = new Color(c.r, c.g, c.b, c.a * 0.45f);
            px[y * n + (x + 1) % n] = half; px[y * n + (x + n - 1) % n] = half;
            px[((y + 1) % n) * n + x] = half; px[((y + n - 1) % n) * n + x] = half;
        }
        t.SetPixels32(px);
        t.Apply(true, true);
        surfaces[key] = t;
        return t;
    }
}
