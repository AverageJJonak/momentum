using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BUILT THINGS, THE POSTER CUT (2026-10-16). Every surface in the game is a
/// scaled cube; these make the cubes read as PAINTED surfaces at no draw-call
/// cost: a handful of procedural textures generated once, each ONE FLAT COLOUR
/// FIELD with at most one large-scale feature, the way a 1930s poster or an
/// Animated Series background paints a wall — plaster: flat; cladding: a panel
/// joint; concrete: a board line every half metre; plate: one seam quartering
/// it; tile: a faint grid; brick and limestone: a faint course line every row;
/// marble: one soft vein; terrazzo: the brass divider strips of the speed grid
/// and nothing else; fluted plaster: five wide ribs; brass: flat; wood: three
/// wide boards; lacquer: flat; gravel: flat. What went with the cut: the grain,
/// the mottle, the seams with bevels, the rivets and fixings, the tie holes
/// and pits, the scratches, the dust on the lower half of a panel, the shade
/// per brick, the terrazzo chips, the damp patches, the brushing. Every kind
/// still carries its NORMAL MAP (near flat: the bump never over 0.2), its
/// OCCLUSION map (flat white now) and its METALLIC-SMOOTHNESS map (one value
/// per kind, a brass strip metal and the stone round it not, a joint matte),
/// so every material binds the maps it always bound, by the names it always
/// used. The kinds that carry their own colours (the vein, a brass line on a
/// black floor) bake the tint in and the material is white. Surface() makes a
/// palette material from them (on the LitFull template: normal, metallic and
/// occlusion maps, the variant kept alive for the build); Tiled() hands every
/// slab a shared material instance whose tiling matches the slab's metres,
/// quantised to half repeats so a level needs a few dozen instances instead of
/// one per wall. The HARD LINE's beads and lit lines are geometry (LevelKitPrecision), not texture.
/// </summary>
public static partial class LevelKit
{
    static readonly Dictionary<string, Texture2D> surfaces = new Dictionary<string, Texture2D>();
    static readonly Dictionary<string, float[]> heights = new Dictionary<string, float[]>();
    static readonly Dictionary<Material, float> repeatOf = new Dictionary<Material, float>();
    static readonly Dictionary<(Material, int, int), Material> tiledCache = new Dictionary<(Material, int, int), Material>();
    /// Each tiled instance's own material, so a piece scaled by hand can be tiled again (LevelEdits) and a material is
    /// known by one name whatever size it was tiled to
    static readonly Dictionary<Material, Material> tiledBase = new Dictionary<Material, Material>();

    /// The grid line's brass (the bible's), and the cream a marble vein is drawn in
    public static readonly Color BrassLine = new Color(0.78f, 0.58f, 0.25f);
    static readonly Color VeinCream = new Color(0.78f, 0.72f, 0.6f);

    /// The hardest any texture's normal map is allowed to push (the poster rule: a surface is a flat field)
    public const float BumpCap = 0.2f;

    // ---------------------------------------------------------------- noise (the vein and the nature kit still ask for it)
    static float Noise(int x, int y, int cell, int seed)
    {
        // Value noise: hashed lattice, smooth between
        int x0 = FloorDiv(x, cell), y0 = FloorDiv(y, cell);
        float fx = (x - x0 * cell) / (float)cell, fy = (y - y0 * cell) / (float)cell;
        fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
        float a = MenuArt.Hash01(x0, y0, seed), b = MenuArt.Hash01(x0 + 1, y0, seed), c = MenuArt.Hash01(x0, y0 + 1, seed), d = MenuArt.Hash01(x0 + 1, y0 + 1, seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }
    static int FloorDiv(int a, int b) => a >= 0 ? a / b : -((-a + b - 1) / b);

    /// Periodic noise: the lattice wraps at `n`, so the texture tiles without a seam
    static float Wrapped(int x, int y, int n, int cell, int seed)
    {
        int cells = Mathf.Max(1, n / cell);
        int x0 = FloorDiv(x, cell), y0 = FloorDiv(y, cell);
        float fx = (x - x0 * cell) / (float)cell, fy = (y - y0 * cell) / (float)cell;
        fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
        int xa = ((x0 % cells) + cells) % cells, xb = (xa + 1) % cells, ya = ((y0 % cells) + cells) % cells, yb = (ya + 1) % cells;
        float a = MenuArt.Hash01(xa, ya, seed), b = MenuArt.Hash01(xb, ya, seed), c = MenuArt.Hash01(xa, yb, seed), d = MenuArt.Hash01(xb, yb, seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }

    // ---------------------------------------------------------------- the height fields: a flat field, one feature

    /// The face of every field; a joint sits this far under it, a strip this far over it
    const float Face0 = 0.5f, JointDrop = 0.1f, StripRise = 0.03f;

    /// How far a texel is from the nearest line of a grid `cell` texels apart (lines on the multiples)
    static float ToLine(int v, int cell) { int m = ((v % cell) + cell) % cell; return Mathf.Min(m, cell - m); }

    /// A soft-edged joint: the drop at distance `d` from a joint `halfW` wide with a `soft` ramp out of it
    static float Joint(float d, float halfW, float soft) => d < halfW ? JointDrop : d < halfW + soft ? JointDrop * (1f - (d - halfW) / soft) : 0f;

    /// Plaster: flat
    static float PlasterHeight(int x, int y, int n) => Face0;

    /// Cladding: 2x2 panels per repeat, the joint between them and nothing on the panel
    static float PanelHeight(int x, int y, int n)
    {
        int half = n / 2;
        float seam = Mathf.Min(ToLine(x, half), ToLine(y, half));
        return Face0 - Joint(seam, n / 256f, n / 128f);
    }

    /// Board-formed concrete: a board line every quarter repeat (half a metre), nothing else
    static float ConcreteHeight(int x, int y, int n) => Face0 - Joint(ToLine(y, n / 4), n / 256f, n / 200f);

    /// Cut rock: layered strata with hard ledges, no grain
    static float StrataHeight(int x, int y, int n)
    {
        int yy = ((y % n) + n) % n;
        int edge = 0, i = 0;
        int scale = Mathf.Max(1, n / 256);
        while (edge <= yy && i < 128) { int thick = (14 + (int)(MenuArt.Hash01(i, 0, 41) * 22f)) * scale; if (edge + thick > yy) break; edge += thick; i++; }
        float band = MenuArt.Hash01(i, 1, 41);
        float ledge = Mathf.Clamp01((yy - edge) / (3f * scale)); // a hard step at each band's foot
        return 0.35f + band * 0.3f * ledge;
    }

    /// Steel plate: one seam quartering the repeat, the plates themselves flat
    static float PlateHeight(int x, int y, int n)
    {
        int half = n / 2;
        float seam = Mathf.Min(ToLine(x, half), ToLine(y, half));
        return Face0 - Joint(seam, n / 256f + 0.5f, n / 128f);
    }

    /// Tile grooves: a grid of joints every quarter repeat
    static float GrooveHeight(int x, int y, int n)
    {
        int q = n / 4;
        float seam = Mathf.Min(ToLine(x, q), ToLine(y, q));
        return Face0 - Joint(seam, n / 256f, n / 200f);
    }

    /// Packed gravel and earth: flat (the roofs' grid is drawn in the albedo)
    static float GravelHeight(int x, int y, int n) => Face0;

    // ---------------------------------------------------------------- the Deco fields

    /// The speed grid in texels: 2 = the 2 m line at the edge of every repeat (a hand wide), 1 = the fainter metre line, 0 = neither
    static int GridLine(int x, int y, int n)
    {
        int w = Mathf.Max(2, n / 32);
        if (x < w || y < w) return 2;
        int half = n / 2;
        return (x % half) < w / 2 || (y % half) < w / 2 ? 1 : 0;
    }

    /// Black marble, polished: flat (the vein is paint, not relief)
    static float MarbleHeight(int x, int y, int n) => Face0;

    /// The vein: ONE soft bright line where a warped noise crosses its middle, wide and faint
    static float Vein(int x, int y, int n)
    {
        int wx = x + Mathf.RoundToInt((Wrapped(x, y, n, n / 5, 212) - 0.5f) * n * 0.14f);
        int wy = y + Mathf.RoundToInt((Wrapped(x, y, n, n / 7, 213) - 0.5f) * n * 0.14f);
        float a = Wrapped(wx, wy, n, n / 4, 214);
        float v = 1f - Mathf.Clamp01(Mathf.Abs(a - 0.5f) / 0.035f);
        return v * v * (3f - 2f * v) * 0.7f;
    }

    /// Terrazzo: polished flat, the brass divider strips of the speed grid a hair proud
    static float TerrazzoHeight(int x, int y, int n)
    {
        int line = GridLine(x, y, n);
        return line == 2 ? Face0 + StripRise : line == 1 ? Face0 + StripRise * 0.6f : Face0;
    }

    /// Fluted plaster: five wide half-round ribs across the repeat, a narrow flat between them
    static float FlutedHeight(int x, int y, int n)
    {
        const int ribs = 5;
        float t = ((x * ribs) % n) / (float)n;
        float u = t * 2f - 1f;
        float rib = Mathf.Abs(u) < 0.88f ? Mathf.Sqrt(1f - (u / 0.88f) * (u / 0.88f)) : 0f;
        return 0.3f + rib * 0.4f;
    }

    /// Laid in courses: `courses` rows per repeat, a course line between every row; with `heads`, the head joints too
    /// (every other row shifted half a unit: a running bond), each unit twice as long as it is high
    static float CourseHeight(int x, int y, int n, int courses, bool heads, float jointPx)
    {
        int ch = n / courses;
        float d = ToLine(y, ch);
        if (heads)
        {
            int bw = ch * 2;
            int row = FloorDiv(y, ch);
            int shift = (row & 1) * (bw / 2);
            d = Mathf.Min(d, ToLine(x + shift, bw));
        }
        return Face0 - Joint(d, jointPx * 0.5f, jointPx);
    }

    /// Brick: sixteen courses to the repeat (12.5 cm bricks at 2 m), a faint course line every row
    static float BrickHeight(int x, int y, int n) => CourseHeight(x, y, n, 16, false, n / 128f);

    /// Glazed tile (THE UNDERGROUND): eight courses of 2:1 tiles to the repeat — at 0.6 m a repeat, 15 × 7.5 cm — a faint grid
    static float TileHeight(int x, int y, int n) => CourseHeight(x, y, n, 8, true, n / 128f);

    /// Cut limestone: four courses to the repeat (half-metre blocks), a faint course line every row
    static float AshlarHeight(int x, int y, int n) => CourseHeight(x, y, n, 4, false, n / 150f);

    /// Brushed brass: flat (brass is trim, not cladding)
    static float BrassHeight(int x, int y, int n) => Face0;

    /// Wood: three wide boards to the repeat, a joint between them, no grain (the panelling and the decking alike)
    static float BoardHeight(int x, int y, int n)
    {
        float t = (((y % n) + n) % n) / (float)n * 3f;
        float f = t - Mathf.Floor(t);
        float d = Mathf.Min(f, 1f - f) * (n / 3f);
        return Face0 - Joint(d, n / 256f + 0.5f, n / 160f);
    }

    // ---------------------------------------------------------------- textures from height

    /// The height field, once, as numbers
    static float[] Heights(string key, int n, System.Func<int, int, int, float> height)
    {
        if (heights.TryGetValue(key, out var h) && h != null && h.Length == n * n) return h;
        h = new float[n * n];
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) h[y * n + x] = height(x, y, n);
        heights[key] = h;
        return h;
    }

    static Texture2D NewTex(string name, int n, bool linear)
        => new Texture2D(n, n, TextureFormat.RGBA32, true, linear) { name = "Surface_" + name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 8, hideFlags = HideFlags.HideAndDontSave };

    /// The kinds whose albedo carries its own colours (the tint is baked in, the material is white)
    static bool Coloured(Surf s) => s == Surf.Marble || s == Surf.Terrazzo;

    /// A texel's own colour on a coloured kind: the vein's cream on the marble's black; the terrazzo is its matrix
    static Color Paint(Surf s, int x, int y, int n, Color tint)
        => s == Surf.Marble ? Color.Lerp(tint, VeinCream, Vein(x, y, n)) : tint;

    static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

    /// The albedo from a height field: `lift` and `contrast` map the one feature's height to brightness and nothing
    /// else touches the field. Greyscale under the material's tint for most kinds; the coloured kinds, and any kind
    /// with a coloured grid line, bake the tint in here (a brass line cannot be reached by multiplying a charcoal tint)
    static Texture2D Albedo(string key, int n, System.Func<int, int, int, float> height, Surf surf, float lift, float contrast,
        bool gridLines, Color gridColour, Color tint)
    {
        bool coloured = Coloured(surf) || (gridLines && gridColour != default);
        string tk = key + (gridLines ? "_grid" : "") + (coloured ? "_" + Hex(tint) + (gridLines ? Hex(gridColour) : "") : "");
        if (surfaces.TryGetValue(tk, out var t) && t != null) return t;
        var h = Heights(key, n, height);
        t = NewTex(tk, n, false);
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int i = y * n + x;
                float v = lift + (h[i] - Face0) * contrast;
                Color c = coloured ? Paint(surf, x, y, n, tint) * v : new Color(v, v, v);
                int line = gridLines ? GridLine(x, y, n) : 0;
                if (line == 2) c = coloured ? gridColour * v : c * 0.8f;
                else if (line == 1) c = coloured ? Color.Lerp(c, gridColour * v, 0.5f) : c * 0.93f;   // the metre line, fainter
                px[i] = new Color32((byte)Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f), (byte)Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f), (byte)Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f), 255);
            }
        t.SetPixels32(px);
        t.Apply(true, true);
        surfaces[tk] = t;
        return t;
    }

    /// A normal map from a height field (central differences), packed the way URP unpacks it: x in A (and R), y in G
    static Texture2D NormalMap(string key, int n, System.Func<int, int, int, float> height, float strength)
    {
        string nk = key + "_n";
        if (surfaces.TryGetValue(nk, out var t) && t != null) return t;
        var h = Heights(key, n, height);
        t = NewTex(nk, n, true);
        var px = new Color32[n * n];
        float s = strength * n / 256f;   // the same slope whatever the resolution
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = h[y * n + (x + 1) % n] - h[y * n + (x + n - 1) % n];
                float dy = h[((y + 1) % n) * n + x] - h[((y + n - 1) % n) * n + x];
                Vector3 nm = new Vector3(-dx * s, -dy * s, 1f).normalized;
                byte bx = (byte)Mathf.RoundToInt((nm.x * 0.5f + 0.5f) * 255f), by = (byte)Mathf.RoundToInt((nm.y * 0.5f + 0.5f) * 255f), bz = (byte)Mathf.RoundToInt((nm.z * 0.5f + 0.5f) * 255f);
                px[y * n + x] = new Color32(255, by, bz, bx);
            }
        t.SetPixels32(px);
        t.Apply(true, true);
        surfaces[nk] = t;
        return t;
    }

    /// Occlusion: flat white (a poster surface has no cavities to darken); one map shared by every kind, bound by
    /// the name every material always bound
    static Texture2D Occlusion()
    {
        const string ok = "flat_o";
        if (surfaces.TryGetValue(ok, out var t) && t != null) return t;
        t = NewTex(ok, 4, true);
        var px = new Color32[16];
        for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
        t.SetPixels32(px);
        t.Apply(true, true);
        surfaces[ok] = t;
        return t;
    }

    /// One metal and one smoothness per kind: what the surface is made of, nothing texel by texel
    static (float metal, float smooth) Finish(Surf s) => s switch
    {
        Surf.Plate => (0.9f, 0.55f),
        Surf.Panel => (0.3f, 0.6f),
        Surf.Groove => (0f, 0.7f),
        Surf.Concrete => (0f, 0.3f),
        Surf.Strata => (0.05f, 0.3f),
        Surf.Gravel => (0f, 0.26f),
        Surf.Wood => (0f, 0.45f),
        Surf.Bark => (0f, 0.18f),
        Surf.Leaf => (0f, 0.4f),
        Surf.Marble => (0f, 0.9f),
        Surf.Terrazzo => (0f, 0.82f),
        Surf.Fluted => (0f, 0.3f),
        Surf.Brick => (0f, 0.22f),
        Surf.Ashlar => (0f, 0.3f),
        Surf.Brass => (0.9f, 0.7f),
        Surf.WoodPanel => (0f, 0.55f),
        Surf.Oxblood => (0f, 0.55f),
        Surf.Tile => (0f, 0.85f),
        _ => (0f, 0.5f),
    };

    /// Metallic in the red, smoothness in the alpha: one value per kind, a joint matte, and `gridMetal`: the floor's
    /// grid lines are brass divider strips (half metal, so they keep their colour in a dark room — the grid is a speed
    /// read — and still glint), whatever the floor is made of; a second map, keyed apart, so a wall of the same stone
    /// carries no strips
    static Texture2D MetalGloss(string key, int n, System.Func<int, int, int, float> height, Surf s, bool gridMetal)
    {
        string mk = key + (gridMetal ? "_gridm" : "") + "_m";
        if (surfaces.TryGetValue(mk, out var t) && t != null) return t;
        var h = Heights(key, n, height);
        var (metal, smooth) = Finish(s);
        t = NewTex(mk, n, true);
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int i = y * n + x;
                float m = metal, sm = smooth;
                if (h[i] < Face0 - JointDrop * 0.5f) sm *= 0.45f;   // a joint, a groove, mortar: matte
                if (gridMetal && GridLine(x, y, n) > 0) { m = Mathf.Max(m, 0.4f); sm = Mathf.Max(sm, 0.75f); }   // the brass strip
                px[i] = new Color32((byte)Mathf.RoundToInt(Mathf.Clamp01(m) * 255f), 0, 0, (byte)Mathf.RoundToInt(Mathf.Clamp01(sm) * 255f));
            }
        t.SetPixels32(px);
        t.Apply(true, true);
        surfaces[mk] = t;
        return t;
    }

    public enum Surf { Plaster, Panel, Concrete, Strata, Plate, Groove, Gravel, Wood, Bark, Leaf, Marble, Terrazzo, Fluted, Brick, Ashlar, Brass, WoodPanel, Oxblood, Tile }

    static System.Func<int, int, int, float> Field(Surf s)
    {
        switch (s)
        {
            case Surf.Plaster: return PlasterHeight;
            case Surf.Panel: return PanelHeight;
            case Surf.Concrete: return ConcreteHeight;
            case Surf.Strata: return StrataHeight;
            case Surf.Plate: return PlateHeight;
            case Surf.Groove: return GrooveHeight;
            case Surf.Wood: return BoardHeight;
            case Surf.Bark: return BarkHeight;
            case Surf.Leaf: return LeafHeight;
            case Surf.Marble: return MarbleHeight;
            case Surf.Terrazzo: return TerrazzoHeight;
            case Surf.Fluted: return FlutedHeight;
            case Surf.Brick: return BrickHeight;
            case Surf.Ashlar: return AshlarHeight;
            case Surf.Brass: return BrassHeight;
            case Surf.WoodPanel: return BoardHeight;
            case Surf.Oxblood: return PlasterHeight;   // lacquered plaster: flat under its own gloss and tint
            case Surf.Tile: return TileHeight;
            default: return GravelHeight;
        }
    }

    /// How each surface is drawn: the albedo's lift and the contrast its one feature gets (a joint 0.1 under the face
    /// at contrast 0.8 is 8 % darker), the normal's strength (never over BumpCap), the texture size
    static (float lift, float contrast, float bump, int size) Look(Surf s) => s switch
    {
        Surf.Plaster => (0.96f, 0f, 0f, 512),
        Surf.Panel => (0.95f, 0.8f, 0.15f, 512),
        Surf.Concrete => (0.94f, 0.8f, 0.15f, 512),
        Surf.Strata => (0.9f, 0.6f, 0.2f, 512),
        Surf.Plate => (0.94f, 0.8f, 0.15f, 256),
        Surf.Groove => (0.96f, 0.8f, 0.15f, 256),
        Surf.Wood => (0.94f, 0.8f, 0.12f, 512),
        Surf.Bark => (0.88f, 0.5f, 0.2f, 256),
        Surf.Leaf => (0.9f, 0.5f, 0.2f, 256),
        Surf.Marble => (0.96f, 0f, 0f, 512),
        Surf.Terrazzo => (0.95f, 0.3f, 0.1f, 512),
        Surf.Fluted => (0.95f, 0.45f, 0.2f, 512),
        Surf.Brick => (0.95f, 0.8f, 0.12f, 512),
        Surf.Ashlar => (0.95f, 0.8f, 0.12f, 512),
        Surf.Brass => (0.96f, 0f, 0f, 256),
        Surf.WoodPanel => (0.94f, 0.8f, 0.12f, 512),
        Surf.Oxblood => (0.96f, 0f, 0f, 512),
        Surf.Tile => (0.97f, 0.8f, 0.12f, 512),
        _ => (0.94f, 0f, 0f, 512),
    };

    /// A palette material: the tint over a procedural surface, its normal map, its occlusion, its metal and gloss,
    /// tiling every `metresPerRepeat`. `albedoOverride` keeps an existing base map, except the floor grid, which is drawn
    /// INTO the surface's own albedo (a flat white grid over it threw the texture away); `gridColour` is the grid's line
    /// colour (brass by default on the coloured kinds; a darkening of the surface on the others, as before). `bumpScale`
    /// is honoured up to BumpCap (the poster rule). On the LitFull template when it is there (normal + metallic +
    /// occlusion variants kept alive for the build), on the old normal-map template when it is not.
    public static Material Surface(Color tint, float smoothness, Surf surf, float metresPerRepeat, float bumpScale = -1f, Texture2D albedoOverride = null, Color gridColour = default, string set = null)
    {
        var look = Look(surf);
        var field = Field(surf);
        string key = surf.ToString();
        bool full = Template("LitFull") != null;
        var m = FromTemplate(full ? "LitFull" : "LitNormalMap", "Universal Render Pipeline/Lit");
        m.name = surf + (set != null ? "." + set : "") + " " + Hex(tint);   // a stable name: the level editor saves paint by it
        bool gridded = (albedoOverride != null && albedoOverride == grid) || surf == Surf.Terrazzo;   // terrazzo always carries its divider strips
        bool overridden = albedoOverride != null && !gridded;
        if (gridded && gridColour == default && Coloured(surf)) gridColour = BrassLine;
        bool coloured = !overridden && (Coloured(surf) || (gridded && gridColour != default));
        bool gridMetal = gridded && gridColour != default;   // a coloured line is a brass strip: metal in the map, not just paint in the albedo
        // A photo set for this kind (LevelKitSurfaceSets), or the one named outright: it carries the maps; the tint stays
        // the surface's average colour. A photo beats the drawn grid (2026-10-27: the grids read as missing textures);
        // a hand-drawn override (the House's chequer) still beats a photo
        SurfaceSet photo = null;
        bool hasPhoto = !overridden && (set != null ? TryLoad(set, out photo) : TryGetSet(surf, tint, out photo));
        if (hasPhoto)
        {
            ApplySet(m, photo, tint, smoothness, bumpScale < 0f ? Mathf.Max(look.bump, 0.1f) : bumpScale, full);
            repeatOf[m] = metresPerRepeat;
            return m;
        }
        m.SetColor("_BaseColor", coloured ? Color.white : tint);
        m.SetTexture("_BaseMap", overridden ? albedoOverride : Albedo(key, look.size, field, surf, look.lift, look.contrast, gridded, gridColour, tint));
        m.SetTexture("_BumpMap", NormalMap(key, look.size, field, 3f));
        m.SetFloat("_BumpScale", Mathf.Min(BumpCap, bumpScale < 0f ? look.bump : bumpScale));
        m.EnableKeyword("_NORMALMAP");
        if (full)
        {
            // The map's alpha is the texel's share of the palette's smoothness: a joint half of it, the face all of it
            m.SetTexture("_MetallicGlossMap", MetalGloss(key, look.size, field, surf, gridMetal));
            m.SetFloat("_Smoothness", Mathf.Clamp01(smoothness * 1.55f));
            m.SetFloat("_Metallic", 0f);
            m.SetTexture("_OcclusionMap", Occlusion());
            m.SetFloat("_OcclusionStrength", 1f);
            m.EnableKeyword("_METALLICSPECGLOSSMAP");
            m.EnableKeyword("_OCCLUSIONMAP");
        }
        else
        {
            m.SetFloat("_Smoothness", smoothness);
            if (surf == Surf.Brass) m.SetFloat("_Metallic", 0.9f);   // no metal map to carry it
        }
        repeatOf[m] = metresPerRepeat;
        return m;
    }

    /// The material for a slab of `size`: an instance of `mat` tiled to the slab's metres, shared by every slab of
    /// the same size (to the half repeat). Materials that carry no surface come back untouched.
    public static Material Tiled(Material mat, Vector3 size)
    {
        if (mat == null || !repeatOf.TryGetValue(mat, out float mpr) || mpr <= 0f) return mat;
        // The thin axis is the slab's thickness; the other two are the face the texture lies on, in the cube's UV order
        float u, v;
        if (size.y <= size.x && size.y <= size.z) { u = size.x; v = size.z; }
        else if (size.z <= size.x) { u = size.x; v = size.y; }
        else { u = size.z; v = size.y; }
        int ru = Mathf.Max(1, Mathf.RoundToInt(u / mpr * 2f)), rv = Mathf.Max(1, Mathf.RoundToInt(v / mpr * 2f));
        var key = (mat, ru, rv);
        if (tiledCache.TryGetValue(key, out var inst) && inst != null) return inst;
        inst = new Material(mat) { name = mat.name + " " + ru + "x" + rv };
        tiledBase[inst] = mat;
        inst.SetTextureScale("_BaseMap", new Vector2(ru * 0.5f, rv * 0.5f));
        tiledCache[key] = inst;
        return inst;
    }

    /// The material a tiled instance was made from (itself when it is not one): one name for a surface at every size
    public static Material UntiledOf(Material m) => m != null && tiledBase.TryGetValue(m, out var b) && b != null ? b : m;

    /// A piece's material tiled again to a new size (a hand edit scaled it)
    public static Material Retile(Material current, Vector3 size) => current == null ? null : Tiled(UntiledOf(current), size);

    /// A level build starts: the tiled instances belong to the previous level's materials, which Init has replaced
    static void ResetSurfaces()
    {
        tiledCache.Clear();
        tiledBase.Clear();
        repeatOf.Clear();
        ClearProbes();
    }
}
