using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// The painted skies: which picture a sky is (MenuSky.Paint paints it; the menu's dome and WorldLook's skybox draw it)
public enum SkyLook { Procedural, Sepia, Violet, Starfield }

/// <summary>
/// THE SKY OVER THE MENU, and THE PAINTED SKIES of the levels. The menu's is painted in three things:
///   THE DOME    the sky itself (Paint): since 2026-10-16 the skyline painting's night, a dark mauve-grey overcast
///               over an umber horizon, the last ochre where the sun went down, long dark bars of cloud with their
///               undersides lit. It is a small sphere that rides on the camera and is drawn FIRST and writes no
///               depth, so it is behind everything however near it is, and near enough that the scene's fog never
///               reaches it.
///   THE DECKS   two layers of slate storm cloud a few hundred metres over the House's crown, their bellies stained
///               sepia by the city, each one texture sliding across a ring of quads, at different speeds in
///               different directions, so the gaps open and close. The scene's own fog takes them out toward the
///               horizon, which is why the dome's horizon is painted in the fog's colour.
///   THE FLASH   lightning is INSIDE the storm: a soft patch of light between the two decks, thrown somewhere new
///               for every strike, seen through the lower deck's gaps and thin places.
/// All of it on the stock URP Unlit shader: nothing here can come up magenta. The reflection the wet ground and the
/// black marble use is set here too, and handed back for the thumbnails (which are shot in daylight) to put aside.
/// Paint() is the one painter for the dome AND the levels' skies (WorldLook.Skybox on Momentum/Sky): a violet night
/// with a moon and flat clouds (the red-street painting), the sepia overcast, or a black field of fine stars (the
/// tower paintings). Nothing in here touches a scene object except Build: the offline preview compiles this file.
/// </summary>
public static class MenuSky
{
    /// Where the sun went down: light travels along yaw 200°, so the sun is opposite, behind the House, a little right
    public const float SunAzimuth = 20f;
    public const float DeckLow = 700f, DeckHigh = 980f, DeckRadius = 3300f;

    // THE LIGHT (SEPIA, after the skyline painting, 2026-10-16): umber and olive masses with ochre on the one lit face,
    // the overcast's mauve for a fill, the ambient crushed so charcoal stone goes to black paper. The windows (pale
    // yellow-white), the lamps and the crowns carry the picture.
    public static readonly Color SunColor = new Color(1f, 0.8f, 0.5f), FillColor = new Color(0.46f, 0.4f, 0.52f);
    public const float SunIntensity = 0.95f, FillIntensity = 0.3f;
    public static readonly Color AmbientSky = new Color(0.17f, 0.15f, 0.16f), AmbientEquator = new Color(0.14f, 0.11f, 0.09f), AmbientGround = new Color(0.05f, 0.04f, 0.035f);
    /// The haze at the horizon: umber-olive away from where the sun went down, ochre toward it (sRGB; the fog takes it as is)
    public static readonly Color HazeAway = new Color(0.26f, 0.23f, 0.2f), HazeToward = new Color(0.52f, 0.38f, 0.2f);

    /// The haze `degrees` of azimuth away from where the sun went down. One function for the dome's paint AND the
    /// scene's fog (FogFor), so a far tower dissolves into exactly the sky behind it
    public static Color Haze(float degrees)
    {
        float k = Mathf.Exp(-(degrees * degrees) / (62f * 62f));
        return Color.Lerp(HazeAway, HazeToward, k);
    }

    /// The menu's picture: the skyline painting's night, the last ochre just under the horizon, no stars under the storm
    public static SkyPaint MenuPaint() => new SkyPaint
    {
        look = SkyLook.Sepia, hazeAway = HazeAway, hazeToward = HazeToward,
        sunAz = SunAzimuth, sunElev = -3f, sunSize = 0f, sunColor = Color.white, seed = 1939,
    };

    public class Parts
    {
        public Transform dome;
        public Material domeMat;
        public readonly List<Material> decks = new List<Material>();
        public readonly List<Vector2> wind = new List<Vector2>();      // tiles per second, per deck material
        public readonly List<Color> deckTint = new List<Color>();
        public Transform flash;
        public Material flashMat;
        public Color flashTint;
    }

    public static Parts Build(Transform root)
    {
        var parts = new Parts();

        // THE DOME. Opaque, drawn before everything (Background), no depth written, and not in the depth pre-pass:
        // a sphere a hundred metres across that the whole city is "behind" would otherwise eat the city.
        var dome = new GameObject("SkyDome");
        dome.transform.SetParent(root, false);
        dome.AddComponent<MeshFilter>().sharedMesh = DomeMesh(64, 32);
        var dr = dome.AddComponent<MeshRenderer>();
        var dm = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "MenuSkyDome" };
        dm.SetTexture("_BaseMap", Paint(MenuPaint(), "MenuSkyDome"));
        dm.SetColor("_BaseColor", Color.white);
        dm.SetInt("_ZWrite", 0);
        dm.SetInt("_Cull", (int)CullMode.Off);
        dm.renderQueue = (int)RenderQueue.Background;
        dm.SetShaderPassEnabled("DepthOnly", false);
        dm.SetShaderPassEnabled("DepthNormalsOnly", false);
        dr.sharedMaterial = dm;
        dr.shadowCastingMode = ShadowCastingMode.Off;
        dr.receiveShadows = false;
        dr.lightProbeUsage = LightProbeUsage.Off;
        dome.transform.localScale = Vector3.one * 120f;
        parts.dome = dome.transform;
        parts.domeMat = dm;

        // THE DECKS. Each is rings of the same sheet, fainter toward the rim, so its edge is never an edge.
        var sheets = new SheetBaker();
        AddDecks(parts, sheets);
        sheets.Flush(root, "CloudDeck");

        // THE FLASH, between them
        var flash = new GameObject("CloudFlash");
        flash.transform.SetParent(root, false);
        flash.transform.position = new Vector3(0f, (DeckLow + DeckHigh) * 0.5f, 0f);
        flash.transform.localScale = new Vector3(1500f, 1f, 1500f);
        flash.AddComponent<MeshFilter>().sharedMesh = FlatQuad();
        var fr = flash.AddComponent<MeshRenderer>();
        parts.flashTint = new Color(0.9f, 0.9f, 1f, 1f);
        parts.flashMat = MenuCity.Sheet(CityTextures.Glow(), new Color(0.9f, 0.9f, 1f, 0f), MenuCity.QFlash);
        fr.sharedMaterial = parts.flashMat;
        fr.shadowCastingMode = ShadowCastingMode.Off;
        fr.receiveShadows = false;
        fr.enabled = false;
        parts.flash = flash.transform;

        // What the gloss in the city mirrors: this sky, not the scene's default noon
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = CityTextures.SkyCube();
        RenderSettings.reflectionIntensity = 1f;
        return parts;
    }

    /// Both decks, into a baker. (Public, and free of scene objects, so the city's offline preview builds the very
    /// same decks.) `parts` may be null.
    public static void AddDecks(Parts parts, SheetBaker sheets)
    {
        var cloud = CityTextures.Clouds();
        AddDeck(parts, sheets, cloud, DeckHigh, 3100f, new Color(0.7f, 0.71f, 0.74f), new Vector2(0.0042f, 0.0017f), MenuCity.QCloudHigh);
        AddDeck(parts, sheets, cloud, DeckLow, 2300f, new Color(0.96f, 0.92f, 0.88f), new Vector2(-0.0061f, 0.0034f), MenuCity.QCloudLow);

        // THE CITY ON THE CLOUD. A storm over a city that size is lit from underneath: a wide soft stain of the
        // lamps' amber on the belly of the lower deck, strongest over the middle of town. One sheet just under the
        // deck, drawn after it, cut into squares so the fog on it is worked out often enough.
        var stain = MenuCity.Sheet(CityTextures.Glow(), new Color(1f, 0.68f, 0.4f, 0.2f), MenuCity.QCloudLow + 4);
        stain.name = "CityOnCloud";
        const float Size = 3600f;
        const int Cuts = 8;
        for (int i = 0; i < Cuts; i++)
            for (int j = 0; j < Cuts; j++)
            {
                float u0 = i / (float)Cuts, u1 = (i + 1) / (float)Cuts, v0 = j / (float)Cuts, v1 = (j + 1) / (float)Cuts;
                float y = DeckLow - 10f;
                Vector3 P(float u, float v) => new Vector3((u - 0.5f) * Size, y, (v - 0.5f) * Size);
                sheets.Quad(stain, P(u0, v0), P(u0, v1), P(u1, v1), P(u1, v0), new Vector2(u0, v0), new Vector2(u1, v1));
            }
    }

    static void AddDeck(Parts parts, SheetBaker sheets, Texture tex, float y, float tile, Color tint, Vector2 wind, int queue)
    {
        // Rings of the same sheet, fainter and fainter toward the rim: by the time it ends there is nothing left to end
        float[] edge = { 0f, 0.4f, 0.52f, 0.64f, 0.76f, 0.88f, 1f };
        float[] alpha = { 1f, 0.84f, 0.66f, 0.48f, 0.3f, 0.14f };
        for (int i = 0; i < alpha.Length; i++)
        {
            Color c = tint;
            c.a = alpha[i];
            var m = MenuCity.Sheet(tex, c, queue);
            m.name = "CloudDeck" + queue + "_" + i;
            sheets.Ring(m, new Vector3(0f, y, 0f), DeckRadius * edge[i], DeckRadius * edge[i + 1], 28, i == 0 ? 5 : 1, tile);
            if (parts == null) continue;
            parts.decks.Add(m);
            parts.wind.Add(wind);
            parts.deckTint.Add(c);
        }
    }

    /// A long-lat sphere of unit radius: u is azimuth from +z toward +x, v is elevation from the nadir to the zenith,
    /// the same way Paint lays its picture out. Its own mesh, so neither depends on how Unity's sphere is mapped
    static Mesh DomeMesh(int around, int up)
    {
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        for (int j = 0; j <= up; j++)
        {
            float v = j / (float)up, elev = (v - 0.5f) * Mathf.PI;
            for (int i = 0; i <= around; i++)
            {
                float u = i / (float)around, az = u * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Sin(az) * Mathf.Cos(elev), Mathf.Sin(elev), Mathf.Cos(az) * Mathf.Cos(elev)));
                uvs.Add(new Vector2(u, v));
            }
        }
        int row = around + 1;
        for (int j = 0; j < up; j++)
            for (int i = 0; i < around; i++)
            {
                int a = j * row + i, b = a + 1, c = a + row, d = c + 1;
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(d);
            }
        var mesh = new Mesh { name = "SkyDomeMesh" };
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    /// A unit square in the xz plane, facing both ways as far as the sheet material is concerned
    static Mesh FlatQuad()
    {
        var mesh = new Mesh { name = "FlashQuad" };
        mesh.SetVertices(new List<Vector3> { new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f) });
        mesh.SetUVs(0, new List<Vector2> { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) });
        mesh.SetTriangles(new List<int> { 0, 1, 2, 0, 2, 3 }, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    /// A material colour that comes out `gain` times brighter in linear light (colours are converted on the way in)
    public static Color Gain(Color c, float gain)
    {
        float k = Mathf.LinearToGammaSpace(Mathf.Max(0f, gain));
        return new Color(c.r * k, c.g * k, c.b * k, c.a);
    }

    /// The fog for the way the camera is facing: the same haze the dome is painted with at that azimuth
    public static Color FogFor(Vector3 forward)
    {
        forward.y = 0f;
        if (forward.sqrMagnitude < 1e-4f) return Haze(90f);
        float az = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        return Haze(Mathf.Abs(Mathf.DeltaAngle(az, SunAzimuth)));
    }

    // ---------------------------------------------------------------- the painted skies

    /// What a sky is painted from. Colours in sRGB, as a painter mixes them (converted to linear per texel); the haze
    /// is the horizon's colour away from the sun's azimuth and toward it, and must be the fog's colour too
    public struct SkyPaint
    {
        public SkyLook look;
        public Color hazeAway, hazeToward;
        public float sunAz, sunElev, sunSize;   // where the sun sits (azimuth from +z toward +x, elevation), its size (the procedural scale: 0.03 is small; 0 = no disc)
        public Color sunColor;
        public float moonAz, moonElev, moonSize;   // a moon when moonSize > 0 (degrees of radius)
        public int seed;
    }

    const float Rad = 0.017453292f;

    /// One long-lat picture (u = azimuth from +z toward +x, v = elevation −90..90), half-float, linear, after the user's
    /// references (2026-10-16):
    ///   SEPIA      a dark mauve-grey overcast over an umber horizon, ochre where the sun went, long dark bars of cloud
    ///              with their undersides lit: the city by day and dusk, the menu
    ///   VIOLET     a violet gradient to near-black overhead, an orange glow at the horizon where the light is, a full
    ///              pale moon with a halo, flat cloud silhouettes lit violet-pink from below, a few faint stars
    ///   STARFIELD  black with a fine field of stars, a faint warm glow at the horizon: the high city
    /// Below the horizon there is only haze, darkening. Everything is a smooth function or a hash: no scene objects.
    public static Texture2D Paint(SkyPaint p, string name)
    {
        const int W = 1024, H = 512;
        Color zenith, upper, horizon, glow, cloud, cloudLit;
        Color starColor = new Color(0.8f, 0.82f, 0.9f), moonColor = new Color(0.96f, 0.93f, 0.8f), moonHalo = new Color(0.5f, 0.42f, 0.6f);
        float glowWidth = 48f, glowHeight = 16f, starChance = 0f, starGain = 0f;
        int cloudCount = 0;
        switch (p.look)
        {
            case SkyLook.Violet:
                zenith = new Color(0.05f, 0.02f, 0.1f); upper = new Color(0.2f, 0.08f, 0.42f); horizon = new Color(0.36f, 0.16f, 0.5f);
                glow = new Color(0.5f, 0.22f, 0.1f); cloud = new Color(0.07f, 0.03f, 0.11f); cloudLit = new Color(0.5f, 0.24f, 0.5f);
                starChance = 0.0008f; starGain = 0.5f; cloudCount = 7;
                break;
            case SkyLook.Starfield:
                zenith = new Color(0f, 0f, 0.01f); upper = new Color(0.015f, 0.015f, 0.04f); horizon = new Color(0.07f, 0.06f, 0.11f);
                glow = new Color(0.18f, 0.12f, 0.1f); cloud = Color.black; cloudLit = Color.black;
                starChance = 0.004f; starGain = 0.7f; moonHalo = new Color(0.3f, 0.3f, 0.4f);
                break;
            default:
                zenith = new Color(0.09f, 0.075f, 0.11f); upper = new Color(0.19f, 0.155f, 0.21f); horizon = new Color(0.3f, 0.25f, 0.22f);
                glow = new Color(0.55f, 0.36f, 0.14f); cloud = new Color(0.11f, 0.09f, 0.1f); cloudLit = new Color(0.44f, 0.3f, 0.16f);
                glowWidth = 40f; glowHeight = 12f; cloudCount = 9;
                break;
        }

        var px = new Color[W * H];
        var sinAz = new float[W];
        var cosAz = new float[W];
        for (int x = 0; x < W; x++)
        {
            float a = (x + 0.5f) / W * 360f * Rad;
            sinAz[x] = Mathf.Sin(a);
            cosAz[x] = Mathf.Cos(a);
        }
        Vector3 sunDir = Dir(p.sunAz, p.sunElev), moonDir = Dir(p.moonAz, p.moonElev);
        float sunDeg = p.sunSize * 40f;
        float cosSunIn = Mathf.Cos(sunDeg * Rad), cosSunOut = Mathf.Cos((sunDeg + 0.6f) * Rad);
        float cosMoonIn = Mathf.Cos(p.moonSize * Rad), cosMoonOut = Mathf.Cos((p.moonSize + 0.35f) * Rad), cosHalo = Mathf.Cos((p.moonSize + 10f) * Rad);
        var lobes = Clouds(cloudCount, p.seed);

        for (int y = 0; y < H; y++)
        {
            float elev = (y + 0.5f) / H * 180f - 90f;
            float se = Mathf.Sin(elev * Rad), ce = Mathf.Cos(elev * Rad);
            for (int x = 0; x < W; x++)
            {
                float az = (x + 0.5f) / W * 360f;
                float dSun = Mathf.Abs(Mathf.DeltaAngle(az, p.sunAz));
                Color haze = Color.Lerp(p.hazeAway, p.hazeToward, Mathf.Exp(-(dSun * dSun) / (62f * 62f)));
                Color c;
                if (elev < 0f)
                {
                    // Below the horizon there is only haze, darkening: the ground hides nearly all of it
                    c = Color.Lerp(haze, haze * 0.35f, Mathf.Clamp01(-elev / 30f));
                }
                else
                {
                    c = Color.Lerp(horizon, upper, Smooth(Mathf.Clamp01(elev / 28f)));
                    c = Color.Lerp(c, zenith, Smooth(Mathf.Clamp01((elev - 28f) / 62f)));
                    // The glow where the light is: a gaussian in azimuth and in elevation
                    float g = Mathf.Exp(-(dSun * dSun) / (glowWidth * glowWidth)) * Mathf.Exp(-(elev * elev) / (glowHeight * glowHeight));
                    c += glow * g;
                    // The haze band along the horizon
                    c = Color.Lerp(haze, c, Smooth(Mathf.Clamp01(elev / 6f)));
                    float dx = sinAz[x] * ce, dy = se, dz = cosAz[x] * ce;
                    if (starChance > 0f && elev > 8f)
                    {
                        // One texel each, fading in up from the haze, dimmer in the glow
                        float h = Hash01(x, y, p.seed);
                        if (h > 1f - starChance)
                        {
                            float k = Mathf.Clamp01((elev - 8f) / 25f) * (1f - g) * (0.35f + 0.65f * Hash01(x, 7, p.seed + 1));
                            c += starColor * (k * starGain);
                        }
                    }
                    if (p.moonSize > 0f)
                    {
                        float dm = dx * moonDir.x + dy * moonDir.y + dz * moonDir.z;
                        float halo = Smooth(Mathf.Clamp01((dm - cosHalo) / (cosMoonOut - cosHalo)));
                        c += moonHalo * (halo * halo * 0.4f);
                        float disc = Smooth(Mathf.Clamp01((dm - cosMoonOut) / (cosMoonIn - cosMoonOut)));
                        c = Color.Lerp(c, moonColor, disc);
                    }
                    if (sunDeg > 0f)
                    {
                        // The sun: small and pale
                        float ds = dx * sunDir.x + dy * sunDir.y + dz * sunDir.z;
                        float disc = Smooth(Mathf.Clamp01((ds - cosSunOut) / (cosSunIn - cosSunOut)));
                        c = Color.Lerp(c, p.sunColor, disc * 0.85f);
                    }
                    if (lobes.Length > 0 && elev < 40f)
                    {
                        // Flat cloud silhouettes over the moon and the glow, their undersides lit
                        CloudAt(lobes, dx, dy, dz, elev, out float inside, out float under);
                        c = Color.Lerp(c, cloud, inside);
                        c = Color.Lerp(c, cloudLit, under);
                    }
                }
                c = c.linear;
                c.a = 1f;
                px[y * W + x] = c;
            }
        }
        var t = new Texture2D(W, H, TextureFormat.RGBAHalf, false, true) { name = name, filterMode = FilterMode.Bilinear };
        t.wrapModeU = TextureWrapMode.Repeat;
        t.wrapModeV = TextureWrapMode.Clamp;
        t.SetPixels(px);
        t.Apply(false, true);
        t.hideFlags = HideFlags.HideAndDontSave;
        return t;
    }

    static float Smooth(float t) => t * t * (3f - 2f * t);

    static float Hash01(int x, int y, int seed)
    {
        uint h = (uint)x * 374761393u + (uint)y * 668265263u + (uint)seed * 2246822519u;
        h = (h ^ (h >> 13)) * 1274126177u;
        h ^= h >> 16;
        return (h & 0xFFFFFFu) / 16777216f;
    }

    /// A unit direction from an azimuth (from +z toward +x) and an elevation, in degrees
    static Vector3 Dir(float az, float elev)
    {
        float a = az * Rad, e = elev * Rad;
        return new Vector3(Mathf.Sin(a) * Mathf.Cos(e), Mathf.Sin(e), Mathf.Cos(a) * Mathf.Cos(e));
    }

    /// One disc of a cloud: its direction, the cosine of its radius, the cosine of the radius 0.9° in (the lit band), its elevation
    struct Lobe { public float x, y, z, cosR, cosRin, elev; }

    /// Flat clouds low over the horizon: each a long bar of 3–5 overlapping discs
    static Lobe[] Clouds(int count, int seed)
    {
        var lobes = new List<Lobe>();
        for (int i = 0; i < count; i++)
        {
            float az = Hash01(i, 11, seed) * 360f, elev = 7f + Hash01(i, 13, seed) * 16f;
            int n = 3 + (int)(Hash01(i, 17, seed) * 3f);
            float r0 = 2f + Hash01(i, 19, seed) * 3f;
            float stretch = r0 * 3.2f / Mathf.Max(0.3f, Mathf.Cos(elev * Rad));   // degrees of azimuth the bar spans
            for (int k = 0; k < n; k++)
            {
                float t = n > 1 ? k / (n - 1f) - 0.5f : 0f;
                float r = r0 * (0.55f + 0.6f * Hash01(i * 31 + k, 23, seed));
                float lobeAz = az + t * stretch, lobeEl = elev + (Hash01(i * 31 + k, 29, seed) - 0.5f) * r0 * 0.5f;
                Vector3 d = Dir(lobeAz, lobeEl);
                lobes.Add(new Lobe { x = d.x, y = d.y, z = d.z, cosR = Mathf.Cos(r * Rad), cosRin = Mathf.Cos((r - 0.9f) * Rad), elev = lobeEl });
            }
        }
        return lobes.ToArray();
    }

    /// How far inside any cloud a direction is (0..1 over about a texel), and whether it is on a cloud's lit underside
    static void CloudAt(Lobe[] lobes, float dx, float dy, float dz, float elev, out float inside, out float under)
    {
        float best = -1f, bestIn = -1f, bestElev = 0f;
        for (int i = 0; i < lobes.Length; i++)
        {
            float dot = dx * lobes[i].x + dy * lobes[i].y + dz * lobes[i].z;
            float depth = dot - lobes[i].cosR;
            if (depth > best) { best = depth; bestIn = dot - lobes[i].cosRin; bestElev = lobes[i].elev; }
        }
        inside = Mathf.Clamp01(best / 0.0003f);   // ~0.3° of edge
        // The underside: inside, under the disc's centre, within the 0.9° band of the edge
        under = inside * (elev < bestElev - 0.2f ? 1f : 0f) * (1f - Mathf.Clamp01(bestIn / 0.0003f));
    }
}
