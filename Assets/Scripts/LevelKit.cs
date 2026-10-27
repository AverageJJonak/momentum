using System.Collections.Generic;
using UnityEngine;

public enum Face { North, South, East, West }

/// Two faces of the same city. UTOPIA is what the citizens see: cream plaster,
/// terrazzo with brass in it, fluted columns, lamp-cream light. UNDERBELLY is
/// what runs it: brick vaults, iron, charcoal floors, dim amber light that
/// flickers. OFFICE is the company's house (marble, walnut, oxblood, brass),
/// WORKS its plant (brick, riveted iron, gaslight), SURFACE the rooftops.
public enum Zone { Utopia, Underbelly, Office, Works, Surface, Brutal }   // Brutal (2026-10-27): raw concrete, the VHOLUME courses

/// A rectangular cut in one wall face, in WORLD units: u runs along the wall
/// (x for North/South, z for East/West), v is height.
public struct Hole
{
    public Face face;
    public float u0, u1, v0, v1;

    public Hole(Face face, float u0, float u1, float v0, float v1)
    {
        this.face = face;
        this.u0 = u0; this.u1 = u1;
        this.v0 = v0; this.v1 = v1;
    }
}

/// <summary>
/// Toolkit for ENCLOSED levels in a DARK DECO city (Gotham 1939) that looks
/// civic and is secretly rotten. Every material comes from the active ZONE, so a
/// builder flips SetZone(Underbelly) before the maintenance corridor and back
/// after, and the same Room/Ramp/Pillar calls come out in brick and iron. Rooms
/// are built face by face so adjacent rooms share ONE wall, and holes cut doors,
/// vents and glass openings out of it. Walls sit centred on the room boundary.
/// Props: lamp strips, ceiling light bars, the company's panels, planters,
/// surveillance domes, cables, flickering emergency lights. The world is dark by
/// default; the lights carry the picture.
/// </summary>
public static partial class LevelKit
{
    public const float T = 1f; // wall / floor thickness

    // ---- The company. The city has one owner — the House — and its name is on everything.
    public const string Company = "HALCYON";
    public const string CompanyFull = "HALCYON CONSOLIDATED";
    /// What the House says about itself (signboards, signs, menus): courteous, possessive, plural
    public static readonly string[] Taglines =
    {
        "EVERYTHING, BEAUTIFULLY", "THE HOUSE PROVIDES", "A ROOM FOR EVERYONE",
        "SERVICE WITHOUT END", "YOUR PLEASURE IS OUR BUSINESS", "ALWAYS AT YOUR DISPOSAL",
    };
    /// What leaks through when a signboard glitches red, and what's sprayed on the walls below stairs
    public static readonly string[] Corruptions =
    {
        "EVERYTHING IS OURS", "THE HOUSE ALWAYS WINS", "NO ONE CHECKS OUT",
        "SERVICE IS COMPULSORY", "YOUR BUSINESS IS OUR PLEASURE", "YOU ARE AT OUR DISPOSAL",
    };
    /// A text mesh in the world, readable from its local -Z side (so `rot` looks AWAY
    /// from the viewer: LookRotation(-facing)). Draws with the depth-tested world-text
    /// material, so it never shows through a wall; `maxWidth` shrinks the lettering
    /// to fit its surface; `brand` uses the company face instead of the title face.
    /// Parent it to something with unit scale — never to a stretched panel.
    public static TextMesh Text(Transform parent, Vector3 pos, Quaternion rot, string text, float size, Color color,
        string name = "Text", float maxWidth = 0f, bool brand = false, float fadeStart = 60f, float fadeEnd = 140f)
    {
        Font font = brand ? GameFont.Brand : GameFont.Title;
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.SetPositionAndRotation(pos, rot);
        var tm = go.AddComponent<TextMesh>();
        tm.font = font;
        tm.text = text;
        tm.fontSize = 64;
        tm.fontStyle = FontStyle.Normal;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
        Fit(tm, size, maxWidth);
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = GameFont.WorldMaterial(font, fadeStart, fadeEnd);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return tm;
    }

    /// Sets a text mesh's character size from `size`, shrunk so the line fits `maxWidth`
    /// (0 = no limit). Glyph height is size × fontSize × 0.1; width follows the face's advance.
    public static void Fit(TextMesh tm, float size, float maxWidth)
    {
        tm.characterSize = size;
        if (maxWidth <= 0f || string.IsNullOrEmpty(tm.text)) return;
        float est = tm.text.Length * size * tm.fontSize * 0.1f * GameFont.Advance(tm.font);
        if (est > maxWidth) tm.characterSize = size * maxWidth / est;
        // Then measure the real thing: the mesh is rebuilt on the spot, so the renderer's
        // local bounds are the true width — wide faces overran their estimate and hid in walls
        var r = tm.GetComponent<Renderer>();
        if (r == null) return;
        float real = r.localBounds.size.x * Mathf.Abs(tm.transform.lossyScale.x);
        if (real > 0.001f && real > maxWidth) tm.characterSize *= maxWidth / real;
    }

    static Material inkMat;
    static Material EmblemInk() => inkMat != null ? inkMat : inkMat = Unlit(Shader.Find("Universal Render Pipeline/Unlit"), new Color(0.07f, 0.07f, 0.1f));

    /// The company mark: a sun on the horizon — a ring with a bar through its lower
    /// third — facing -forward of `rot`. `hole` is the surface behind it, so the ring reads as a ring.
    // ---- Keyword templates -------------------------------------------------
    // Every material in this game is made at runtime, so no material ASSET uses the keywords the game needs and
    // Unity strips those shader variants out of the player: a transparent material then runs the OPAQUE variant and
    // renders solid, in a build only, silently. VariantKeepAlive writes one material asset per keyword set under
    // Resources/KeepAlive to hold them. The runtime COPIES those assets rather than building from the bare shader,
    // so the keyword set the game asks for is by construction the keyword set the build kept — URP's own material
    // validator decides what that set is (it adds _ALPHAPREMULTIPLY_ON to alpha blending, for one), and we inherit
    // its answer instead of guessing at it.
    static readonly Dictionary<string, Material> templates = new Dictionary<string, Material>();

    // ---- TOON (2026-10-16): the DARK DECO cel look. The Lit templates have toon TWINS (Resources/KeepAlive/Toon*.mat on
    // Momentum/Toon and Momentum/ToonTransparent: the same property names and keyword sets, so every map and number a
    // material sets carries over untouched). When WorldLook.Toon is on (the Settings toggle, PlayerPrefs "look_toon",
    // default on) AND the shader is present and compiled (Shader.isSupported), Template resolves a Lit name to its twin;
    // otherwise the Lit template as before, with ONE warning when the shader is the reason. A shader that failed to
    // compile in Unity must never turn the game pink. Unlit is never touched: the glows draw themselves.
    public const string ToonShaderName = "Momentum/Toon", ToonTransparentShaderName = "Momentum/ToonTransparent";
    static int toonOpaque = -1, toonTransparent = -1;   // -1 unchecked this build, 0 missing or unsupported, 1 ok

    static bool ToonOk(ref int state, string shaderName)
    {
        if (!WorldLook.Toon) return false;
        if (state < 0)
        {
            var s = Shader.Find(shaderName);
            state = s != null && s.isSupported ? 1 : 0;
            if (state == 0) Debug.LogWarning("MomentumToon: shader missing or unsupported, Lit fallback (" + shaderName + ")");
        }
        return state == 1;
    }

    /// The toon twin of a Lit template's name; null when it has none, or toon is off, or the shader is not usable
    static string ToonTwin(string name)
    {
        switch (name)
        {
            case "LitFull":
            case "LitNormalMap": return ToonOk(ref toonOpaque, ToonShaderName) ? "ToonFull" : null;
            case "LitTransparent": return ToonOk(ref toonTransparent, ToonTransparentShaderName) ? "ToonTransparent" : null;
            case "LitTransparentNormal": return ToonOk(ref toonTransparent, ToonTransparentShaderName) ? "ToonTransparentNormal" : null;
            default: return null;
        }
    }

    /// The template asset for a keyword set, loaded once: the toon twin when the look is on and the twin exists, else the
    /// Lit one. Null if VariantKeepAlive has not written it yet.
    public static Material Template(string name)
    {
        string twin = ToonTwin(name);
        if (twin != null)
        {
            var t = LoadTemplate(twin);
            if (t != null) return t;
        }
        return LoadTemplate(name);
    }

    static Material LoadTemplate(string name)
    {
        if (templates.TryGetValue(name, out var cached) && cached != null) return cached;
        var loaded = Resources.Load<Material>("KeepAlive/" + name);
        templates[name] = loaded;
        return loaded;
    }

    /// True when a material draws with one of the toon shaders
    public static bool IsToon(Material m)
        => m != null && m.shader != null && (m.shader.name == ToonShaderName || m.shader.name == ToonTransparentShaderName);

    /// A fresh material with the template's shader, properties and KEYWORDS; falls back to the bare shader if the
    /// template is missing (the editor still looks right; only a build would strip the variant). A toon material comes
    /// back as a BOX wants it: no hull outline (the HARD LINE beads are a box's line); see Rounded for the rest.
    public static Material FromTemplate(string name, string fallbackShader)
    {
        var t = Template(name);
        var m = t != null ? new Material(t) : new Material(Shader.Find(fallbackShader));
        if (IsToon(m))
        {
            m.SetFloat("_Outline", 0f);
            m.SetShaderPassEnabled("SRPDefaultUnlit", false);
        }
        return m;
    }

    static readonly Dictionary<Material, Material> rounded = new Dictionary<Material, Material>();

    /// `mat` as a ROUNDED mesh wants it (a sphere, a capsule, a cylinder): the same material with the inverted-hull
    /// outline on, one per source, cached for the build. Anything not toon comes back as it is.
    public static Material Rounded(Material mat)
    {
        if (!IsToon(mat)) return mat;
        if (rounded.TryGetValue(mat, out var r) && r != null) return r;
        r = new Material(mat) { name = mat.name + " rounded" };
        r.SetFloat("_Outline", 1f);
        r.SetShaderPassEnabled("SRPDefaultUnlit", true);
        rounded[mat] = r;
        return r;
    }

    // ---- THE UNITS' NAMING CONTRACT (EnemyMannequin, 2026-10-16). The units make their shell materials themselves (static,
    // shared per kind, on bare Lit) and name them for this: "Toon/Flat/…" takes the ramp with no outline (a box's edges are
    // the beads), "Toon/Rounded/…" the ramp WITH the hull outline (brass balls, drums, the lacquer dome), "Toon/Glow/…"
    // stays Unlit (an HDR colour that glows on its own: the escapement, and every lamp, brace, weakspot and target are
    // Unlit too, so the colour language never passes through the ramp). The glass ("Toon/Rounded/Glass") already comes
    // through the transparent template. Re-shaded IN PLACE by name: once after the build (WorldLook.Apply) and every few
    // seconds after it (WorldLookBlend) for the kinds that spawn later; a swapped material stays swapped, so a sweep is a
    // string check per renderer of each unit, nothing more.
    static readonly List<Renderer> routeScratch = new List<Renderer>(64);

    public static void ToonRouteUnits()
    {
        if (!ToonOk(ref toonOpaque, ToonShaderName)) return;
        var toon = Shader.Find(ToonShaderName);
        if (toon == null) return;
        foreach (var unit in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            unit.GetComponentsInChildren(true, routeScratch);
            for (int i = 0; i < routeScratch.Count; i++)
            {
                var m = routeScratch[i].sharedMaterial;
                if (m == null || m.shader == null) continue;
                string n = m.name;
                if (!n.StartsWith("Toon/", System.StringComparison.Ordinal)) continue;
                if (m.shader == toon || m.shader.name == ToonTransparentShaderName) continue;   // done, or the glass through its template
                if (n.StartsWith("Toon/Glow/", System.StringComparison.Ordinal) || m.shader.name.Contains("Unlit")) continue;
                m.shader = toon;   // the properties stay by name: _BaseColor, _Metallic, _Smoothness, _EmissionColor
                bool round = n.StartsWith("Toon/Rounded/", System.StringComparison.Ordinal);
                m.SetFloat("_Outline", round ? 1f : 0f);
                m.SetShaderPassEnabled("SRPDefaultUnlit", round);
            }
        }
        routeScratch.Clear();
    }

    /// A plain toon material for anything outside the kit (a unit's shell, a hand): colour, smoothness, a glow that
    /// shows through the ramp, outlined when the mesh is rounded. Lit with the same properties when toon is off or
    /// the shader is missing, so a caller never has to know.
    public static Material ToonLit(Color c, float smoothness, bool roundedMesh, Color emission = default)
    {
        var m = FromTemplate("Toon", "Universal Render Pipeline/Lit");
        m.name = "Toon " + Hex(c);
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smoothness);
        if (emission != default)
        {
            m.SetColor("_EmissionColor", emission);
            m.EnableKeyword("_EMISSION");   // Lit needs the keyword; the toon shader adds _EmissionColor without one
        }
        return roundedMesh ? Rounded(m) : m;
    }

    public static void Emblem(Transform parent, Vector3 center, Quaternion rot, float size, Material ring, Material hole)
    {
        Quaternion disc = rot * Quaternion.Euler(90f, 0f, 0f);
        Vector3 forward = rot * Vector3.forward, up = rot * Vector3.up;
        var outer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        outer.name = "EmblemRing";
        Object.DestroyImmediate(outer.GetComponent<Collider>());
        outer.transform.SetParent(parent);
        outer.transform.SetPositionAndRotation(center, disc);
        outer.transform.localScale = new Vector3(size, 0.01f, size);
        outer.GetComponent<MeshRenderer>().sharedMaterial = ring;
        var inner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        inner.name = "EmblemHole";
        Object.DestroyImmediate(inner.GetComponent<Collider>());
        inner.transform.SetParent(parent);
        inner.transform.SetPositionAndRotation(center - forward * 0.03f, disc);
        inner.transform.localScale = new Vector3(size * 0.62f, 0.01f, size * 0.62f);
        inner.GetComponent<MeshRenderer>().sharedMaterial = hole;
        var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = "EmblemBar";
        Object.DestroyImmediate(bar.GetComponent<Collider>());
        bar.transform.SetParent(parent);
        bar.transform.SetPositionAndRotation(center - up * (size * 0.16f) - forward * 0.06f, rot);
        bar.transform.localScale = new Vector3(size * 1.15f, size * 0.09f, 0.02f);
        bar.GetComponent<MeshRenderer>().sharedMaterial = ring;
    }

    /// Snaps a wall-mounted prop onto the nearest wall face along its facing axis:
    /// `center` moves to `clearance` in front of the wall and `forward` is turned
    /// to point INTO the wall (so a prop's front, its -forward, faces the room).
    /// Walls are 1 m thick, so authored positions a hand's width inside the plane
    /// would otherwise be buried.
    public static bool PlaceOnWall(ref Vector3 center, ref Vector3 forward, float clearance)
    {
        Physics.SyncTransforms();
        bool hitA = Physics.Raycast(center, forward, out RaycastHit a, 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        bool hitB = Physics.Raycast(center, -forward, out RaycastHit b, 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (!hitA && !hitB)
        {
            // Inside the slab: step back out along the facing axis and try again
            bool outA = Physics.Raycast(center - forward * 2f, forward, out a, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            bool outB = Physics.Raycast(center + forward * 2f, -forward, out b, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (!outA && !outB) return false;
            hitA = outA; hitB = outB;
        }
        RaycastHit hit = hitA && (!hitB || a.distance <= b.distance) ? a : b;
        center = hit.point + hit.normal * clearance;
        forward = -hit.normal;
        return true;
    }

    [System.Flags]
    public enum Faces
    {
        None = 0, North = 1, South = 2, East = 4, West = 8, Floor = 16, Ceiling = 32,
        Walls = 15, All = 63,
    }

    class Palette
    {
        public Material wall, ceil, floor, accent, dark, slope;
        public Color light;
        public float lightScale;
        public bool flicker;
    }

    static Palette utopia, underbelly, office, works, surface, brutal;
    static Texture2D grid;
    static Material orbMat, skyMat, neonMat, neonCyanMat, hedgeMat, domeMat, dotMat, cableMat, stripMat, hazardMat;
    static Material woodMat, lawnMat, waterMat, trunkMat, blossomMat, blossomDarkMat, leafMat, leafDarkMat, latticeMat, vineMat;
    static Material[] cloudMats;

    public static Zone Current { get; private set; }
    /// The zone's palette; built on first use when nothing has called Init yet (the Level Editor building a scene cold)
    static Palette P { get { var p = Pick(); if (p == null) { Init(); p = Pick(); } return p; } }
    static Palette Pick() => Current == Zone.Underbelly ? underbelly : Current == Zone.Office ? office : Current == Zone.Works ? works : Current == Zone.Surface ? surface : Current == Zone.Brutal ? brutal : utopia;

    public static Material Wall => P.wall;
    public static Material Ceil => P.ceil;
    public static Material Floor => P.floor;
    public static Material Accent => P.accent;
    public static Material Dark => P.dark;
    public static Material Slope => P.slope;
    public static Material OrbMat => orbMat;
    public static Material Sky => skyMat;
    public static Material Neon => neonMat;
    public static Material NeonCyan => neonCyanMat;
    public static Material Wood => woodMat;
    public static Material LawnMat => lawnMat;

    /// Call once per level build; starts in the Utopia zone
    public static void Init()
    {
        ResetDeco();   // the Deco materials belong to this level (LevelKitDeco)
        ResetSurfaces();
        rounded.Clear();                       // the outlined copies belong to the previous level's materials
        toonOpaque = toonTransparent = -1;     // the toggle may have moved since: ask the shader again, once
        factoryLife = null; // the plant's animator is per level
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");

        // DARK DECO (2026-10-07): every zone is Gotham 1939 (LevelKitDeco holds the named materials and the palette's
        // colours). The civic halls: terrazzo underfoot with the speed grid as brass divider strips, cream plaster walls,
        // charcoal ceilings, brass door frames, fluted charcoal columns, walnut on every slide, lamp-cream light
        // MATTE (2026-10-16, the poster look): every smoothness low except brass (the hard cap), lacquer and glass; the
        // toon shader shines nothing under 0.35 and gives metals a thin cap of their own
        // READABLE (2026-10-16, later the same day, after play: "the floors and walls are too dark to see anything"): the
        // FLOOR, WALL, DARK (the pillars you run into) and SLOPE (the slides) of every zone sit at MID-TONE albedos — the
        // paintings' ochre and olive on the lit face, umber in the shade — so a floor in the key's shadow band lands at
        // 0.18 linear luminance or more and a wall at 0.12 under that zone's ambient and key (the maths in LevelKitLook.cs).
        // The ceilings and the accents keep the bible's darks: overhead is the black paper, the floor never is
        utopia = new Palette
        {
            wall = Surface(WallOchre, 0.2f, Surf.Plaster, 2f),
            ceil = Surface(CharcoalStone, 0.2f, Surf.Plaster, 2f, 0.4f),
            floor = Surface(FloorStone, 0.45f, Surf.Terrazzo, 2f, 0.45f, Grid()),
            accent = Surface(BrassGold, 0.7f, Surf.Brass, 1f, 0.5f),
            dark = Surface(PillarCharcoal, 0.25f, Surf.Fluted, 2f, 1f),
            slope = Surface(WalnutLit, 0.3f, Surf.WoodPanel, 2f, 0.9f),
            light = LampCreamLight,
            lightScale = 0.9f,
            flicker = false,
        };
        // The sublevels: brick vaults, umber concrete underfoot with a dim brass grid, iron frames and plate, amber
        // lamps that flicker
        underbelly = new Palette
        {
            wall = Surface(BrickWarm, 0.15f, Surf.Brick, 2f),
            ceil = Surface(new Color(0.12f, 0.12f, 0.13f), 0.1f, Surf.Concrete, 2f, 0.8f),
            floor = Surface(FloorConcrete, 0.2f, Surf.Concrete, 2f, 0.6f, Grid(), new Color(0.5f, 0.37f, 0.16f)),
            accent = Surface(IronGrey, 0.3f, Surf.Plate, 1f, 0.8f),
            dark = Surface(new Color(0.17f, 0.17f, 0.19f), 0.25f, Surf.Plate, 1f, 0.8f),
            slope = Surface(new Color(0.24f, 0.23f, 0.22f), 0.2f, Surf.Concrete, 2f, 0.9f),
            light = new Color(1f, 0.62f, 0.3f),
            lightScale = 0.85f,
            flicker = true,
        };
        // The company's house, after the casino painting (2026-10-16): COBALT walls with the fluting in them, a deeper
        // cobalt overhead, a black-and-cobalt CHEQUER underfoot with the speed grid's brass line through it, brass frames,
        // oxblood lacquer on the dark pieces (the one red: matte, never lit), a cobalt-black slope, a chandelier's
        // yellow-white light
        office = new Palette
        {
            wall = Surface(CobaltWall, 0.3f, Surf.Fluted, 2f, 0.35f),
            ceil = Surface(CobaltDeep, 0.2f, Surf.Plaster, 2f, 0.25f),
            floor = Surface(Color.white, 0.5f, Surf.Marble, 2f, 0.35f, set: "Tiles012"),   // the chequer is a photograph of black-and-white marble now (2026-10-27)
            accent = Surface(BrassGold, 0.7f, Surf.Brass, 1f, 0.5f),
            dark = Surface(OxbloodLit, 0.45f, Surf.Oxblood, 2f, 0.4f),
            slope = Surface(CobaltWall, 0.2f, Surf.WoodPanel, 1f, 0.6f),   // a slide in the deep cobalt was a slide into black
            light = ChandelierLight,
            lightScale = 1f,
            flicker = false,
        };
        office.dark.SetFloat("_SpecStrength", LacquerBand);    // the lacquer's thin band
        office.floor.SetFloat("_SpecStrength", LacquerBand);   // the chequer's: the casino floor shines
        // THE UNDERGROUND (Act II, 2026-10-12, in the Works slot): Halcyon Railways' metro — glazed cream tile on the
        // walls (15 × 7.5 cm bricks, dark grout, glossy), charcoal plaster overhead, dark terrazzo underfoot with the
        // grid in brass, brass trim, oxblood glazed tile on the dark pieces (the band at 1.2 m, LevelKit.TileBand), riveted
        // iron plate on every slide (the escalators, the chutes), a steady incandescent cream: the globes never flicker —
        // the only flicker down here is the third rail's arc. The sewers run in the Underbelly zone (brick, amber flicker)
        // (2026-10-16, after the blue towers: the tile STEEL-BLUE, the key a cold cream — the Underground is the cold palette)
        works = new Palette
        {
            wall = Surface(SteelTile, 0.6f, Surf.Tile, 0.6f, 0.7f),
            ceil = Surface(CharcoalStone, 0.2f, Surf.Plaster, 2f, 0.4f),
            floor = Surface(FloorSteel, 0.45f, Surf.Terrazzo, 2f, 0.45f, Grid()),
            accent = Surface(BrassGold, 0.7f, Surf.Brass, 1f, 0.5f),
            dark = Surface(OxbloodLit, 0.5f, Surf.Tile, 0.6f, 0.7f),
            slope = Surface(IronLit, 0.35f, Surf.Plate, 1f, 1.1f),
            light = ColdCream,
            lightScale = 0.95f,
            flicker = false,
        };
        works.dark.SetFloat("_SpecStrength", LacquerBand);   // the glazed oxblood tile's thin band
        // The rooftops (Chapter 3): limestone parapets, charcoal slab overhead, tar and gravel underfoot with the grid in
        // brass, brass lips on every edge, a warm steady lamp
        surface = new Palette
        {
            wall = Surface(ParapetStone, 0.2f, Surf.Ashlar, 3f, 0.9f),
            ceil = Surface(new Color(0.12f, 0.12f, 0.13f), 0.1f, Surf.Concrete, 3f, 0.9f),
            floor = Surface(RoofGravel, 0.15f, Surf.Gravel, 2f, 0.8f, Grid(), BrassLine),
            accent = Surface(BrassGold, 0.7f, Surf.Brass, 1f, 0.5f),
            dark = Surface(PillarCharcoal, 0.2f, Surf.Concrete, 2f, 0.9f),
            slope = Surface(new Color(0.26f, 0.25f, 0.24f), 0.2f, Surf.Concrete, 2f, 1f),
            light = new Color(1f, 0.8f, 0.55f),
            lightScale = 0.9f,
            flicker = false,
        };

        // BRUTAL (2026-10-27, user: "generate the levels exactly like VHOLUME... they are a brutalist movement game"):
        // raw concrete everywhere, board-formed on the walls, smooth underfoot, dark cast overhead, bunker tiles for
        // the dark masses, pale cast for the lips and slides, rusted iron for the rails; a cool daylight, nothing flickers.
        // The sets are named outright (Surface's `set`) so the kinds' own photos never substitute
        brutal = new Palette
        {
            wall = Surface(new Color(0.56f, 0.56f, 0.55f), 0.12f, Surf.Groove, 4f, 0.9f, set: "Concrete028"),
            ceil = Surface(new Color(0.3f, 0.3f, 0.31f), 0.1f, Surf.Concrete, 4f, 0.8f, set: "Concrete036"),
            floor = Surface(new Color(0.5f, 0.5f, 0.49f), 0.18f, Surf.Concrete, 3f, 0.7f, set: "Concrete030"),
            accent = Surface(new Color(0.42f, 0.3f, 0.22f), 0.35f, Surf.Plate, 1f, 0.8f, set: "Metal041B"),
            dark = Surface(new Color(0.3f, 0.3f, 0.31f), 0.1f, Surf.Strata, 4f, 0.9f, set: "Concrete031"),
            slope = Surface(new Color(0.64f, 0.64f, 0.62f), 0.15f, Surf.Concrete, 3f, 0.7f, set: "Concrete046"),
            light = new Color(0.92f, 0.94f, 1f),
            lightScale = 0.8f,
            flicker = false,
        };

        orbMat = Lit(lit, new Color(1f, 0.8f, 0.3f), 0.3f);
        skyMat = Unlit(unlit, new Color(0.12f, 0.08f, 0.08f));   // a dusk behind a pane
        neonMat = Unlit(unlit, new Color(2.2f, 1.95f, 1.5f));    // the general glow is a lamp's cream now
        neonCyanMat = Unlit(unlit, new Color(0.3f, 2f, 2.4f));   // the blade's and the cut's: nothing else is cyan
        hedgeMat = Surface(new Color(0.14f, 0.13f, 0.09f), 0.2f, Surf.Leaf, 1f, 1.2f);   // clipped box after dark: near black, sepia-olive, never green
        domeMat = Lit(lit, new Color(0.42f, 0.3f, 0.13f), 0.75f); domeMat.SetFloat("_Metallic", 0.85f);   // the company watches, in brass
        dotMat = Unlit(unlit, new Color(2.5f, 0.15f, 0.1f));
        cableMat = Lit(lit, new Color(0.08f, 0.08f, 0.09f), 0.2f);
        stripMat = Unlit(unlit, new Color(1.9f, 1.7f, 1.3f));    // a lamp's cream
        hazardMat = Unlit(unlit, new Color(1.25f, 0.95f, 0.32f));   // mustard, painted on, not an LED
        // The garden set: walnut, a lawn and leaves gone sepia, black water, bark, dried blossom, brass lattice, vines
        woodMat = Surface(Walnut, 0.5f, Surf.WoodPanel, 2f, 0.9f);
        lawnMat = Lit(lit, new Color(0.2f, 0.18f, 0.1f), 0.1f);
        waterMat = WaterSurface(new Color(0.02f, 0.02f, 0.025f, 0.86f), 0.98f, 3f, 0.6f);   // black water: the lamps' amber lies on it
        trunkMat = Surface(new Color(0.22f, 0.16f, 0.11f), 0.2f, Surf.Bark, 1f, 1.4f);
        trunkMat.SetTextureScale("_BaseMap", new Vector2(2f, 1.5f));   // a cylinder's UV goes once round: two ridges' worth
        blossomMat = Leaves(new Color(0.8f, 0.66f, 0.5f));
        blossomDarkMat = Leaves(new Color(0.5f, 0.36f, 0.26f));
        leafMat = Leaves(new Color(0.25f, 0.22f, 0.12f));       // dark sepia-olive: foliage in this world is never the Joker's green
        leafDarkMat = Leaves(new Color(0.13f, 0.11f, 0.06f));
        latticeMat = Lit(lit, BrassGold, 0.7f); latticeMat.SetFloat("_Metallic", 0.9f);
        vineMat = Lit(lit, new Color(0.3f, 0.26f, 0.17f), 0.15f);
        cloudMats = new[] { SpriteMat(CloudSprite(1)), SpriteMat(CloudSprite(2)), SpriteMat(CloudSprite(3)) };
        Current = Zone.Utopia;
    }

    public static void SetZone(Zone zone) => Current = zone;

    // ------------------------------------------------------------ rooms

    /// An axis-aligned room from `min` to `max` (interior extents). Builds only
    /// the requested faces — leave out a face that another room already built.
    public static void Room(Transform parent, Vector3 min, Vector3 max, Faces faces, params Hole[] holes)
    {
        float cx = (min.x + max.x) * 0.5f, cz = (min.z + max.z) * 0.5f;
        float dx = max.x - min.x, dz = max.z - min.z;
        if ((faces & Faces.Floor) != 0)
            FloorSlab(parent, new Vector3(cx, min.y - T * 0.5f, cz), new Vector3(dx + 2f * T, T, dz + 2f * T));
        if ((faces & Faces.Ceiling) != 0)
        {
            Slab(parent, new Vector3(cx, max.y + T * 0.5f, cz), new Vector3(dx + 2f * T, T, dz + 2f * T), Ceil, "Ceiling");
            Probe(parent, min, max);   // a roofed room reflects itself
        }
        if ((faces & Faces.North) != 0) WallFace(parent, Face.North, max.z, min.x - T * 0.5f, max.x + T * 0.5f, min.y, max.y, holes);
        if ((faces & Faces.South) != 0) WallFace(parent, Face.South, min.z, min.x - T * 0.5f, max.x + T * 0.5f, min.y, max.y, holes);
        if ((faces & Faces.East) != 0) WallFace(parent, Face.East, max.x, min.z - T * 0.5f, max.z + T * 0.5f, min.y, max.y, holes);
        if ((faces & Faces.West) != 0) WallFace(parent, Face.West, min.x, min.z - T * 0.5f, max.z + T * 0.5f, min.y, max.y, holes);
        if (Detail) Articulate(parent, min, max, faces, holes);   // its edges and its bays drawn, its walls furnished (LevelKitDetail)
    }

    /// One wall on the plane `plane` (z for North/South, x for East/West),
    /// spanning u0..u1 along it and y0..y1 up, with this face's holes cut out.
    /// Holes may overlap or stack (a door under a glass window): the wall is a
    /// true 2-D subtraction — vertical strips between every hole edge, each
    /// keeping only the spans no hole covers, merged back into as few slabs as
    /// the layout allows. (The old one-pass cut filled "below the window" with
    /// wall — straight through the doorway beneath it.)
    public static void WallFace(Transform parent, Face face, float plane, float u0, float u1, float y0, float y1, params Hole[] holes)
    {
        const float Eps = 1e-4f;
        var mine = new List<Hole>();
        foreach (var h in holes)
            if (h.face == face && h.u1 > u0 + Eps && h.u0 < u1 - Eps && h.v1 > y0 + Eps && h.v0 < y1 - Eps) mine.Add(h);
        if (mine.Count == 0) { Segment(parent, face, plane, u0, u1, y0, y1); return; }

        // Strip edges: the wall's ends and every hole edge inside it
        var edges = new List<float> { u0, u1 };
        foreach (var h in mine)
        {
            if (h.u0 > u0 + Eps && h.u0 < u1 - Eps) edges.Add(h.u0);
            if (h.u1 > u0 + Eps && h.u1 < u1 - Eps) edges.Add(h.u1);
        }
        edges.Sort();

        // Walk the strips; a run of strips with identical open spans becomes one set of slabs
        var spans = new List<(float a, float b)>();
        var runSpans = new List<(float a, float b)>();
        float runStart = u0;
        bool started = false;
        for (int i = 0; i + 1 < edges.Count; i++)
        {
            float a = edges[i], b = edges[i + 1];
            if (b - a < Eps) continue;
            OpenSpans(mine, a, b, y0, y1, spans);
            if (!started)
            {
                started = true;
                runStart = a;
                runSpans.AddRange(spans);
                continue;
            }
            if (!SameSpans(spans, runSpans))
            {
                Emit(parent, face, plane, runStart, a, runSpans);
                runSpans.Clear();
                runSpans.AddRange(spans);
                runStart = a;
            }
        }
        if (started) Emit(parent, face, plane, runStart, u1, runSpans);
    }

    /// The vertical spans of the strip a..b that no hole covers
    static void OpenSpans(List<Hole> holes, float a, float b, float y0, float y1, List<(float a, float b)> result)
    {
        const float Eps = 1e-4f;
        result.Clear();
        var cover = new List<(float v0, float v1)>();
        foreach (var h in holes)
            if (h.u0 <= a + Eps && h.u1 >= b - Eps) cover.Add((Mathf.Max(h.v0, y0), Mathf.Min(h.v1, y1)));
        cover.Sort((p, q) => p.v0.CompareTo(q.v0));
        float cursor = y0;
        foreach (var c in cover)
        {
            if (c.v0 > cursor + Eps) result.Add((cursor, c.v0));
            cursor = Mathf.Max(cursor, c.v1);
        }
        if (cursor < y1 - Eps) result.Add((cursor, y1));
    }

    static bool SameSpans(List<(float a, float b)> x, List<(float a, float b)> y)
    {
        if (x.Count != y.Count) return false;
        for (int i = 0; i < x.Count; i++)
            if (Mathf.Abs(x[i].a - y[i].a) > 1e-4f || Mathf.Abs(x[i].b - y[i].b) > 1e-4f) return false;
        return true;
    }

    static void Emit(Transform parent, Face face, float plane, float a, float b, List<(float a, float b)> spans)
    {
        if (b - a < 1e-4f) return;
        foreach (var s in spans) Segment(parent, face, plane, a, b, s.a, s.b);
    }

    static void Segment(
Transform parent, Face face, float plane, float a0, float a1, float y0, float y1)
    {
        float mid = (a0 + a1) * 0.5f, len = a1 - a0, ym = (y0 + y1) * 0.5f, h = y1 - y0;
        bool alongX = face == Face.North || face == Face.South;
        Vector3 center = alongX ? new Vector3(mid, ym, plane) : new Vector3(plane, ym, mid);
        Vector3 size = alongX ? new Vector3(len, h, T) : new Vector3(T, h, len);
        Slab(parent, center, size, Wall, "Wall");
    }

    /// Posts + lintel just inside a doorway's edges (brass in the city, iron
    /// below, bronze in the plant). The hole should be ~0.5 wider and 0.25 taller than the passage.
    public static void DoorFrame(Transform parent, Hole h, float plane)
    {
        bool alongX = h.face == Face.North || h.face == Face.South;
        const float w = 0.25f;
        Vector3 P(float u, float v) => alongX ? new Vector3(u, v, plane) : new Vector3(plane, v, u);
        Vector3 S(float du, float dv) => alongX ? new Vector3(du, dv, T + 0.1f) : new Vector3(T + 0.1f, dv, du);
        float vm = (h.v0 + h.v1) * 0.5f, vh = h.v1 - h.v0;
        Slab(parent, P(h.u0 + w * 0.5f, vm), S(w, vh), Accent, "Frame");
        Slab(parent, P(h.u1 - w * 0.5f, vm), S(w, vh), Accent, "Frame");
        Slab(parent, P((h.u0 + h.u1) * 0.5f, h.v1 - w * 0.5f), S(h.u1 - h.u0, w), Accent, "Frame");
    }

    // ------------------------------------------------------------ pieces

    public static GameObject Slab(Transform parent, Vector3 center, Vector3 size, Material mat, string name = "Slab")
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = center;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = Tiled(mat, size);
        return go;
    }

    /// A solid slab turned by `rot`, its collider turned with it (Prim is decor and has none: 2026-10-27, the "transparent floors")
    public static GameObject SlabRot(Transform parent, Vector3 center, Vector3 size, Material mat, string name, Quaternion rot)
    {
        var go = Slab(parent, center, size, mat, name);
        go.transform.rotation = rot;
        return go;
    }

    /// Grid floor; the grid tiles at 2 m so speed stays readable
    public static GameObject FloorSlab(Transform parent, Vector3 center, Vector3 size, string name = "Floor")
        => Slab(parent, center, size, Floor, name); // Slab tiles it to the slab's metres (the grid stays 2 m)

    /// A slab whose TOP surface runs from (z0, y0) to (z1, y1) at lateral x. A deck, not a plank: a stringer under
    /// each edge and a post to whatever is under it every three metres, so a slope stands on something
    public static GameObject Ramp(Transform parent, float x, float width, float z0, float y0, float z1, float y1, Material mat = null)
    {
        const float RampT = 0.35f;
        if (z1 < z0) { float t = z0; z0 = z1; z1 = t; t = y0; y0 = y1; y1 = t; }   // the ends either way round: the pitch wants +z as its far end
        float dz = z1 - z0, dy = y1 - y0;
        float len = Mathf.Sqrt(dz * dz + dy * dy);
        float pitch = Mathf.Atan2(-dy, dz) * Mathf.Rad2Deg; // lower +z end → positive pitch
        Quaternion rot = Quaternion.Euler(pitch, 0f, 0f);
        Vector3 topMid = new Vector3(x, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f);
        var go = Slab(parent, topMid - rot * Vector3.up * (RampT * 0.5f), new Vector3(width, RampT, len), mat ?? Slope, "Ramp");
        go.transform.rotation = rot;
        RampSupports(parent, x, width, rot, topMid, len, RampT);
        return go;
    }

    /// What holds a ramp up: a stringer under each edge, posts to the floor found under it (none where it is on the floor)
    static void RampSupports(Transform parent, float x, float width, Quaternion rot, Vector3 topMid, float len, float thick)
    {
        EnsureOffice();
        Physics.SyncTransforms();
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 edge = Vector3.right * (s * (width * 0.5f - 0.22f));
            Decor(parent, PrimitiveType.Cube, topMid + edge - rot * Vector3.up * (thick + 0.14f), rot, new Vector3(0.3f, 0.28f, len - 0.4f), Graphite, "RampStringer");
            int posts = Mathf.Max(1, Mathf.FloorToInt(len / 3f));
            for (int i = 1; i <= posts; i++)
            {
                Vector3 under = topMid + edge + rot * Vector3.forward * (len * (i / (posts + 1f) - 0.5f)) - rot * Vector3.up * (thick + 0.28f);
                if (!Physics.Raycast(under + Vector3.down * 0.05f, Vector3.down, out RaycastHit hit, 40f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                float h = under.y - hit.point.y;
                if (h < 0.5f) continue;
                Decor(parent, PrimitiveType.Cube, new Vector3(under.x, hit.point.y + h * 0.5f, under.z), Quaternion.identity, new Vector3(0.2f, h, 0.2f), Graphite, "RampPost");
            }
        }
    }

    /// Corner-boost pillar (every vertical edge is an authored corner)
    public static GameObject Pillar(Transform parent, Vector3 center, Vector3 size, Material mat = null)
    {
        var p = Slab(parent, center, size, mat ?? Dark, "Pillar");
        TechMarkers.MarkBoxCorners(p);
        return p;
    }

    public static BreakableGlass Glass(Transform parent, Vector3 center, Vector3 size, MovementConfig config)
        => BreakableGlass.Create(parent, center, size, config);

    /// A grapple point in the air. It used to be a gold ball; it is one of the company's drones now, holding station with
    /// its gold eye on you (GrappleDrone) — the same target to a line, in every level that ever asked for one.
    public static void Orb(Transform parent, Vector3 pos) => GrappleDrone.Build(parent, pos);

    /// A point light in the zone's colour: lamp cream and steady up top, amber
    /// and flickering below
    public static Light Light(Transform parent, Vector3 pos, float range = 30f, float intensity = 2.1f)
    {
        var go = new GameObject("Light");
        go.transform.SetParent(parent);
        go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = range;
        l.intensity = intensity * P.lightScale;
        l.color = P.light;
        l.shadows = LightShadows.None;
        if (P.flicker) go.AddComponent<FlickerLight>();
        return l;
    }

    // ------------------------------------------------------------ props

    /// A glowing bar with no collider — lamp cream by default, cyan (the cut's) or any HDR colour
    public static GameObject NeonStrip(Transform parent, Vector3 center, Vector3 size, Material mat = null)
    {
        var go = Slab(parent, center, size, mat ?? neonMat, "Neon");
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    /// A ceiling light bar with a real light under it
    public static void LightStrip(Transform parent, Vector3 center, float length, bool alongX, float intensity = 2.2f)
    {
        NeonStrip(parent, center, alongX ? new Vector3(length, 0.06f, 0.3f) : new Vector3(0.3f, 0.06f, length), stripMat);
        Light(parent, center + Vector3.down * 0.4f, Mathf.Max(14f, length * 0.9f), intensity);
    }

    /// A company sign-panel: a lamp-cream to amber glow that drifts (Hologram, LevelProps) carrying the wordmark and
    /// a tagline, snapped onto the wall it was authored against. It glitches red — and when it does, the words
    /// change. Yaw 0 faces along z, 90 along x.
    public static void Billboard(Transform parent, Vector3 center, float width, float height, float yaw)
    {
        Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        PlaceOnWall(ref center, ref forward, 0.12f);
        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Hologram";
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent);
        go.transform.SetPositionAndRotation(center, rot);
        go.transform.localScale = new Vector3(width, height, 0.05f);
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        // A dark frame behind it
        var frame = Slab(parent, center + forward * 0.1f, new Vector3(width + 0.3f, height + 0.3f, 0.06f), domeMat, "HologramFrame");
        frame.transform.rotation = rot;
        Object.DestroyImmediate(frame.GetComponent<Collider>());

        // The mark and the words on the front face (readable from -forward), parented
        // to the level root — never to the panel, whose scale would stretch the letters
        Color ink = new Color(0.07f, 0.07f, 0.1f);
        Vector3 right = rot * Vector3.right;
        Vector3 face = center - forward * 0.06f;
        string tagline = Taglines[Random.Range(0, Taglines.Length)];
        float markW = width * 0.56f, tagW = width * 0.86f;
        Emblem(parent, face + right * (-width * 0.33f) + Vector3.up * (height * 0.1f), rot, height * 0.42f, EmblemInk(), mat);
        var mark = Text(parent, face + right * (width * 0.07f) + Vector3.up * (height * 0.12f), rot, Company, height * 0.04f, ink, "Wordmark", markW, true);
        var tag = Text(parent, face - Vector3.up * (height * 0.26f), rot, tagline, height * 0.013f, ink, "Tagline", tagW);
        go.AddComponent<Hologram>().Init(mat, mark, tag, markW, tagW);
    }

    /// A physical company sign: a dark plate with the wordmark in gold, over a
    /// door or across a lobby wall. `facing` is the direction it faces (into the room).
    public static void Sign(Transform parent, Vector3 center, Vector3 facing, float width, float height, string text = null)
    {
        Vector3 forward = -facing.normalized;
        PlaceOnWall(ref center, ref forward, 0.1f);
        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
        var plate = Slab(parent, center, new Vector3(width, height, 0.12f), domeMat, "Sign");
        plate.transform.rotation = rot;
        Object.DestroyImmediate(plate.GetComponent<Collider>());
        Vector3 right = rot * Vector3.right;
        Vector3 face = center - forward * 0.08f;
        float emblem = height * 0.7f;
        Emblem(parent, face + right * (-width * 0.5f + emblem * 0.6f + 0.1f), rot, emblem, hazardMat, domeMat);
        Text(parent, face + right * (emblem * 0.45f), rot, text ?? CompanyFull, height * 0.075f, new Color(2.2f, 1.7f, 0.4f), "SignText",
            width - emblem * 1.5f - 0.3f, true, 90f, 180f);
        NeonStrip(parent, center - forward * 0.05f - Vector3.up * (height * 0.5f + 0.04f), new Vector3(width, 0.06f, 0.06f));
    }

    /// The truth, sprayed on a wall in the underbelly (rust red, a little askew).
    /// `facing` is the direction the letters face (into the corridor).
    public static void Stencil(Transform parent, Vector3 center, Vector3 facing, string text = null, float size = 0.08f, float maxWidth = 6f)
    {
        Vector3 forward = -facing.normalized;
        PlaceOnWall(ref center, ref forward, 0.03f);
        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(0f, 0f, Random.Range(-3f, 3f));
        Text(parent, center, rot, text ?? Corruptions[Random.Range(0, Corruptions.Length)], size,
            new Color(0.75f, 0.12f, 0.08f), "Stencil", maxWidth, false, 35f, 80f);
    }


    /// A white planter box with a hedge on top — solid, so it's cover and a step
    public static void Planter(Transform parent, Vector3 groundCenter, Vector3 size)
    {
        Slab(parent, groundCenter + Vector3.up * (size.y * 0.5f), size, utopia.wall, "Planter");
        Slab(parent, groundCenter + Vector3.up * (size.y + 0.35f), new Vector3(size.x * 0.9f, 0.7f, size.z * 0.9f), hedgeMat, "Hedge");
    }

    // ------------------------------------------------------------ the Olympus set

    static GameObject Ball(Transform parent, Vector3 pos, float diameter, Material mat, string name)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * diameter;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    /// A clipped hedge: solid, so it is cover and a step
    public static void Hedge(Transform parent, Vector3 groundCenter, Vector3 size)
        => Slab(parent, groundCenter + Vector3.up * (size.y * 0.5f), size, hedgeMat, "Hedge");

    /// A lawn: a hand's width of green on the floor
    public static void Lawn(Transform parent, Vector3 groundCenter, Vector2 size)
        => Slab(parent, groundCenter + Vector3.up * 0.03f, new Vector3(size.x, 0.06f, size.y), lawnMat, "Lawn");

    /// An ornamental pool: glossy water, a white rim, a fountain post in the middle
    public static void Pool(Transform parent, Vector3 groundCenter, Vector2 size)
    {
        Slab(parent, groundCenter + Vector3.up * 0.06f, new Vector3(size.x, 0.12f, size.y), waterMat, "Water").AddComponent<WaterFlow>();
        Material rim = utopia.wall;
        Slab(parent, groundCenter + new Vector3(0f, 0.22f, size.y * 0.5f + 0.25f), new Vector3(size.x + 1f, 0.45f, 0.5f), rim, "PoolRim");
        Slab(parent, groundCenter + new Vector3(0f, 0.22f, -size.y * 0.5f - 0.25f), new Vector3(size.x + 1f, 0.45f, 0.5f), rim, "PoolRim");
        Slab(parent, groundCenter + new Vector3(size.x * 0.5f + 0.25f, 0.22f, 0f), new Vector3(0.5f, 0.45f, size.y), rim, "PoolRim");
        Slab(parent, groundCenter + new Vector3(-size.x * 0.5f - 0.25f, 0.22f, 0f), new Vector3(0.5f, 0.45f, size.y), rim, "PoolRim");
        Cyl(parent, groundCenter + Vector3.up * 0.5f, new Vector3(0.5f, 0.5f, 0.5f), rim, "Fountain");
        Ball(parent, groundCenter + Vector3.up * 1.3f, 0.9f, waterMat, "FountainHead");
    }

    /// An ornamental lattice over a wall: a grid of thin white bars, decor only
    public static void Lattice(Transform parent, Vector3 center, Vector3 facing, float width, float height, float cell = 2.5f)
    {
        Vector3 forward = -facing.normalized;
        PlaceOnWall(ref center, ref forward, 0.14f);
        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
        Vector3 right = rot * Vector3.right;
        int nx = Mathf.Max(1, Mathf.RoundToInt(width / cell)), ny = Mathf.Max(1, Mathf.RoundToInt(height / cell));
        for (int i = 0; i <= nx; i++)
        {
            var bar = Slab(parent, center + right * (-width * 0.5f + width * i / nx), new Vector3(0.14f, height, 0.14f), latticeMat, "Lattice");
            bar.transform.rotation = rot;
            Object.DestroyImmediate(bar.GetComponent<Collider>());
        }
        for (int j = 0; j <= ny; j++)
        {
            var bar = Slab(parent, center + Vector3.up * (-height * 0.5f + height * j / ny), new Vector3(width, 0.14f, 0.14f), latticeMat, "Lattice");
            bar.transform.rotation = rot;
            Object.DestroyImmediate(bar.GetComponent<Collider>());
        }
    }

    /// The rot made visible: a vine crawling from `from` to `to` in wobbling,
    /// tapering segments — over a wall, along a ceiling, out of a doorway
    public static void Tendril(Transform parent, Vector3 from, Vector3 to, float thickness = 0.35f, int segments = 10)
    {
        Vector3 axis = (to - from).normalized;
        Vector3 side = Vector3.Cross(axis, Vector3.up);
        if (side.sqrMagnitude < 0.01f) side = Vector3.right;
        side.Normalize();
        Vector3 lift = Vector3.Cross(axis, side).normalized;
        float phase = Random.value * 6.28f;
        Vector3 prev = from;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 p = i == segments
                ? to
                : Vector3.Lerp(from, to, t) + side * (Mathf.Sin(t * 9.4f + phase) * 0.55f) + lift * (Mathf.Sin(t * 6.3f + phase * 0.7f) * 0.3f);
            float th = thickness * (1f - 0.65f * t);
            Vector3 d = p - prev;
            var seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            seg.name = "Vine";
            Object.DestroyImmediate(seg.GetComponent<Collider>());
            seg.transform.SetParent(parent);
            seg.transform.position = (prev + p) * 0.5f;
            seg.transform.rotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
            seg.transform.localScale = new Vector3(th, d.magnitude * 0.5f + th * 0.2f, th);
            seg.GetComponent<MeshRenderer>().sharedMaterial = vineMat;
            Ball(parent, p, th * 1.05f, vineMat, "VineKnot");
            prev = p;
        }
    }

    /// A surveillance dome with a red eye — the city is always watching
    public static void CameraDome(Transform parent, Vector3 pos, Vector3 facing)
    {
        var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dome.name = "CameraDome";
        Object.DestroyImmediate(dome.GetComponent<Collider>());
        dome.transform.SetParent(parent);
        dome.transform.position = pos;
        dome.transform.localScale = Vector3.one * 0.36f;
        dome.GetComponent<MeshRenderer>().sharedMaterial = domeMat;
        var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "CameraEye";
        Object.DestroyImmediate(eye.GetComponent<Collider>());
        eye.transform.SetParent(parent);
        eye.transform.position = pos + facing.normalized * 0.16f;
        eye.transform.localScale = Vector3.one * 0.07f;
        eye.GetComponent<MeshRenderer>().sharedMaterial = dotMat;
    }

    /// A slack cable between two points (visual only)
    public static void Cable(Transform parent, Vector3 from, Vector3 to, float thickness = 0.08f)
    {
        Vector3 d = to - from;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "Cable";
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent);
        go.transform.position = (from + to) * 0.5f;
        go.transform.rotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
        go.transform.localScale = new Vector3(thickness, d.magnitude * 0.5f, thickness);
        go.GetComponent<MeshRenderer>().sharedMaterial = cableMat;
    }

    /// A red emergency lamp that flickers — the underbelly's only light
    public static void EmergencyLight(Transform parent, Vector3 pos, float range = 10f)
    {
        NeonStrip(parent, pos, new Vector3(0.28f, 0.12f, 0.12f), dotMat);
        var go = new GameObject("EmergencyLight");
        go.transform.SetParent(parent);
        go.transform.position = pos + Vector3.down * 0.2f;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = range;
        l.intensity = 2.2f;
        l.color = new Color(1f, 0.22f, 0.12f);
        l.shadows = LightShadows.None;
        go.AddComponent<FlickerLight>();
    }

    /// A yellow hazard bar (visual only)
    public static void HazardBar(Transform parent, Vector3 center, Vector3 size)
        => NeonStrip(parent, center, size, hazardMat);

    /// The milk. Every facility has one at the end.
    public static void Milk(Transform parent, Vector3 groundPos)
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        Material white = Lit(lit, new Color(0.97f, 0.97f, 0.97f), 0.3f);   // matte: a bottle in a poster
        Material cap = Lit(lit, new Color(0.88f, 0.8f, 0.62f), 0.2f);     // a 1939 paper cap: cream card, not a plastic blue
        Cyl(parent, groundPos + Vector3.up * 0.5f, new Vector3(0.55f, 0.5f, 0.55f), white, "Milk");
        Cyl(parent, groundPos + Vector3.up * 1.1f, new Vector3(0.28f, 0.12f, 0.28f), white, "MilkNeck");
        Cyl(parent, groundPos + Vector3.up * 1.27f, new Vector3(0.32f, 0.05f, 0.32f), cap, "MilkCap");
        Light(parent, groundPos + Vector3.up * 2f, 8f, 2f);
    }

    static void Cyl(Transform parent, Vector3 center, Vector3 size, Material mat, string name)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = center;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = Rounded(mat);   // a cylinder's line is the hull outline, not a bead
    }

    // ------------------------------------------------------------ materials

    // ---- THE REFERENCE PALETTES' colours (2026-10-16), beside LevelKitDeco's: the House's cobalt and its chandelier
    // light (the casino painting), the Underground's steel-blue tile and cold cream key (the blue towers), and the far
    // fabric's matte CRIMSON (the red street: a flat red mass for the city sibling's facades; red that GLOWS stays the
    // danger signal, so nothing in this colour ever blinks or carries emission)
    public static readonly Color CobaltWall = new Color(0.13f, 0.21f, 0.62f), CobaltDeep = new Color(0.05f, 0.08f, 0.3f);   // the wall a shade up (luminance 0.22): it is run into
    public static readonly Color ChandelierLight = new Color(1f, 0.95f, 0.72f), ColdCream = new Color(0.9f, 0.94f, 1f);
    public static readonly Color SteelTile = new Color(0.46f, 0.54f, 0.6f);   // a step under the bible's (0.6, 0.7, 0.76): the lit tiled wall stays under 0.45, the floor a step lighter than the shaded one
    public static readonly Color CrimsonMatte = new Color(0.55f, 0.08f, 0.06f);

    // ---- THE READABLE PALETTE (2026-10-16, the maths in LevelKitLook.cs): what the player runs on and into, at mid-tone
    // albedos. Luminance = 0.2126 R + 0.7152 G + 0.0722 B, the texture's mean a further x 0.9–0.96 (LevelKitTextures' lift).
    // The bible's charcoal (0.11) and cream (0.72) stay for the ceilings, the trims and the dressing (LevelKitDeco)
    public static readonly Color FloorStone = new Color(0.33f, 0.30f, 0.23f);      // the civic terrazzo's matrix: dark olive-umber, luminance 0.30 (was charcoal, 0.11)
    public static readonly Color WallOchre = new Color(0.30f, 0.255f, 0.17f);      // the civic plaster: ochre, 0.26 (was cream, 0.72: the one lit face blew out at 1.1 and the shade was black)
    public static readonly Color FloorConcrete = new Color(0.30f, 0.27f, 0.23f);   // the sublevels' concrete, 0.27 (was charcoal)
    public static readonly Color BrickWarm = new Color(0.46f, 0.24f, 0.17f);       // the sublevels' brick, 0.28 (the bible's BrickRed is 0.21: a wall in the shade sat at 0.09)
    public static readonly Color FloorSteel = new Color(0.30f, 0.33f, 0.37f);      // the Underground's terrazzo: steel grey, 0.33
    public static readonly Color RoofGravel = new Color(0.38f, 0.36f, 0.33f);      // the roofs' tar and gravel: pale, 0.36 (was 0.13): the night key is 0.8 and the roofs are run in the dark
    public static readonly Color ParapetStone = new Color(0.38f, 0.36f, 0.31f);    // the roofs' limestone, 0.36: a step under the roof it stands on
    public static readonly Color PillarCharcoal = new Color(0.19f, 0.19f, 0.21f);  // the dark pieces (pillars: kick walls), 0.19 (was 0.09–0.14)
    public static readonly Color WalnutLit = new Color(0.34f, 0.20f, 0.11f);       // the civic slides' walnut, 0.22 (the bible's Walnut is 0.17)
    public static readonly Color IronLit = new Color(0.26f, 0.26f, 0.29f);         // the Underground's slides' plate, 0.26 (IronGrey is 0.16)
    public static readonly Color OxbloodLit = new Color(0.46f, 0.12f, 0.11f);      // the matte red of the dark pieces, 0.19 (OxbloodRed is 0.13): crimson is allowed by the references; it never glows
    /// The thin specular band lacquer, glazed tile and the chequer keep (the toon shader's _SpecStrength; 0 = matte, the default)
    const float LacquerBand = 0.15f;

    static Texture2D chequer;

    /// The House's floor: charcoal and cobalt squares a metre across, the speed grid's brass line every 2 m and the fainter
    /// metre line on the squares' joint, drawn as a Surface's albedo (a white tint over it; the marble field under it).
    /// The squares average 0.26 luminance (the charcoal 0.11, the cobalt 0.40): the black-and-cobalt of the casino
    /// painting read as a floor of nothing under the House's ambient (LevelKitLook.cs)
    static Texture2D Chequer()
    {
        if (chequer != null) return chequer;
        const int n = 64;
        chequer = new Texture2D(n, n, TextureFormat.RGBA32, true);
        var black = new Color(0.11f, 0.11f, 0.14f);
        var cobalt = new Color(0.26f, 0.40f, 0.80f);
        var line = BrassLine;
        var metre = Color.Lerp(black, line, 0.5f);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                bool blue = (x < 32) ^ (y < 32);
                chequer.SetPixel(x, y, x < 2 || y < 2 ? line : (x == 32 || y == 32) ? metre : blue ? cobalt : black);
            }
        chequer.Apply();
        chequer.wrapMode = TextureWrapMode.Repeat;
        chequer.filterMode = FilterMode.Bilinear;
        return chequer;
    }

    /// The speed grid: brass lines on charcoal, 2 m to the repeat with a fainter metre line. Surface() draws the grid
    /// INTO a floor's own albedo when it is handed this; the texture itself is what anything else that asks for a grid gets
    static Texture2D Grid()
    {
        if (grid != null) return grid;
        const int n = 64;
        grid = new Texture2D(n, n, TextureFormat.RGBA32, true);
        var bg = CharcoalStone;
        var line = BrassLine;
        var metre = Color.Lerp(bg, line, 0.5f);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                grid.SetPixel(x, y, x < 2 || y < 2 ? line : (x == 32 || y == 32) ? metre : bg);   // the metre line, fainter, inside the 2 m grid
        grid.Apply();
        grid.wrapMode = TextureWrapMode.Repeat;
        grid.filterMode = FilterMode.Bilinear;
        return grid;
    }

    /// A plain lit material: on the Toon template when the look is on and the shader compiled, on `shader` (Lit) when
    /// not. Every caller passes the Lit shader; the toon material answers to the same property names.
    static Material Lit(Shader shader, Color c, float smoothness)
    {
        var m = ToonOk(ref toonOpaque, ToonShaderName) ? FromTemplate("Toon", "Universal Render Pipeline/Lit") : new Material(shader);
        m.name = "Paint " + Hex(c);   // a stable name: the level editor saves paint by it
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smoothness);
        return m;
    }

    static Material Unlit(Shader shader, Color c)
    {
        var m = new Material(shader);
        m.name = "Glow " + Hex(c);
        m.SetColor("_BaseColor", c);
        return m;
    }
}

public static partial class LevelKit
{
    /// A crown material: leaf clusters at a hand's width, on a sphere that goes once round in UV
    static Material Leaves(Color tint)
    {
        var m = Surface(tint, 0.2f, Surf.Leaf, 1f, 1.2f);
        m.SetTextureScale("_BaseMap", new Vector2(4f, 2f));
        return m;
    }
}
