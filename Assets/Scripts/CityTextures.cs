using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE CITY'S PRINT SHOP. Everything about the Deco city that is a PATTERN and
/// not a shape, painted once in code and kept for the session: the stone a
/// facade is cut from, the light its windows wear, the lamps of a promenade, a
/// lane of traffic, a train of lit carriages, the storm's cloud deck, the sky
/// behind it, the wash a floodlight leaves up a wall, a beam.
///
/// GOTHAM AS THE ANIMATED SERIES DREW IT (2026-10-16): a facade is NEAR-BLACK
/// STONE WITH WINDOWS PUNCHED IN IT, flat, the ashlar barely there, a faint
/// lighter string course every five floors, HARD LINE's ink joint down and along
/// every bay; bays five and a half metres apart with blank piers between, floors
/// of 3.9 m, one tall narrow window to a bay (never more than two fifths of the
/// bay wide or half a floor tall), whole bays blind where the stair and the lift
/// run. The WALL ATLAS and the LIGHT ATLAS share one layout: a per-cell hash
/// (IsWindow) decides for every (bay, floor) of the 32 x 32 atlas whether it
/// holds a window or blank stone; the wall draws the window only there and the
/// light lights only there, and a facade samples both from the same random
/// corner, so they agree. THE WINDOW RULE (after the user's reference paintings,
/// 2026-10-16): many SMALL WARM WINDOWS on black masses, never a curtain-wall
/// grid. On a shaft they come in CLUSTERS by floor (a patch of two to four bays
/// on one to three floors: a firm working late) with scattered singles between
/// and dark bands of floors between the clusters, about an eighth of the face;
/// the brick tenements and warehouses are DENSE, nearly half their small panes
/// orange; the landmarks' great arched windows mostly lit. Every lit pane is
/// an old window: tall, a mullion cross in it, a deep reveal round it. An unlit
/// window is not drawn in the light at all, only the near-black pane the stone
/// carries. The first two floors of a shaft are a rusticated base with arched
/// openings (a band of its own), the top floor an attic of small square windows.
///
/// WHY TEXTURES. A window is a texel, a facade is one quad, and it is
/// mip-mapped, so distance turns it into glow instead of into shimmer. Every
/// transparent texel carries the COLOUR of its lit neighbours with no alpha:
/// mips average colour and alpha separately, and black in the gaps would make a
/// far tower a quarter as bright as a near one.
///
/// Colours here are authored as they are seen (sRGB). The sky and its reflection
/// are the exception: those are half-float and linear, because a night gradient
/// in eight bits is a staircase.
/// </summary>
public static class CityTextures
{
    static readonly Dictionary<string, Texture> cache = new Dictionary<string, Texture>();

    /// The atlases are AtlasCells bays by AtlasCells floors; a light cell is CellPx texels square, a wall cell WallPx
    public const int AtlasCells = 32;
    const int CellPx = 16, WallPx = 32;
    /// A band (a base of two floors, an attic of one) is BandCells bays wide and tiles sideways
    public const int BandCells = 16;

    /// The light a facade wears, and the cell layout it shares with its wall: punched office rows; vertical strips
    /// between piers; homes' small windows; tall arched windows (banks, stations, naves); the House's own bays;
    /// an attic of small squares; a shopfront base of arched openings two floors tall
    public const int Rows = 0, Strip = 1, Homes = 2, Arch = 3, House = 4, Attic = 5, Shop = 6;

    /// The stone a wall atlas is cut from. The first seven are the shaft kinds; Brownstone and Granite the new ones;
    /// BlindBrick, BlindStone, BlindChar and Soot carry no window at all (party walls, the dead ones)
    public const int KindLime = 0, KindChar = 1, KindCream = 2, KindHouse = 3, KindBrick = 4, KindPierLime = 5, KindPierChar = 6,
        KindBrownstone = 7, KindGranite = 8, KindBlindBrick = 9, KindBlindStone = 10, KindBlindChar = 11, KindSoot = 12, KindOxblood = 13;
    public const int Kinds = 14;

    /// Where the window sits in its sixteen-texel cell, per style (inclusive). The wall atlas uses the same numbers, so
    /// the pane in the wall and the light in the atlas are the same rectangle. Tall and narrow: never more than six
    /// texels of sixteen wide, never more than nine tall
    static void WindowRect(int style, out int x0, out int x1, out int y0, out int y1)
    {
        switch (style)
        {
            case Strip: x0 = 6; x1 = 9; y0 = 3; y1 = 11; break;     // a window between piers, a spandrel under it
            case Homes: x0 = 6; x1 = 9; y0 = 5; y1 = 10; break;     // a small window
            case Arch: x0 = 4; x1 = 11; y0 = 2; y1 = 12; break;     // a tall window, arched at the top: the landmarks'
            case House: x0 = 6; x1 = 9; y0 = 4; y1 = 11; break;     // the House's own bay
            case Attic: x0 = 6; x1 = 9; y0 = 5; y1 = 8; break;      // a small square under the cornice
            default: x0 = 6; x1 = 9; y0 = 4; y1 = 11; break;        // an office window, twice as tall as wide
        }
    }

    /// Is this texel inside the arch of an Arch window (the top corners cut away)?
    static bool InArch(int lx, int ly, int x0, int x1, int y1)
    {
        if (ly == y1) return lx >= x0 + 3 && lx <= x1 - 3;
        if (ly == y1 - 1) return lx >= x0 + 1 && lx <= x1 - 1;
        return true;
    }

    static uint Mix(uint x)
    {
        x ^= x >> 16; x *= 0x7feb352dU; x ^= x >> 15; x *= 0x846ca68bU; x ^= x >> 16;
        return x;
    }

    /// A number in [0,1) from two integers and a seed, the same every time
    public static float Hash01(int x, int y, int seed)
    {
        uint h = Mix((uint)x * 73856093U ^ (uint)y * 19349663U ^ (uint)seed * 83492791U);
        return (h & 0xFFFFFF) / 16777216f;
    }

    static Color32 C32(Color c, float a) => new Color32((byte)(Mathf.Clamp01(c.r) * 255f), (byte)(Mathf.Clamp01(c.g) * 255f), (byte)(Mathf.Clamp01(c.b) * 255f), (byte)(Mathf.Clamp01(a) * 255f));

    static Texture2D Finish(string key, int w, int h, Color32[] px, bool repeatU = true, bool repeatV = true, int aniso = 8)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = "City_" + key, filterMode = FilterMode.Trilinear, anisoLevel = aniso };
        t.wrapModeU = repeatU ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        t.wrapModeV = repeatV ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        t.SetPixels32(px);
        t.Apply(true, true);
        t.hideFlags = HideFlags.HideAndDontSave;
        cache[key] = t;
        return t;
    }

    static bool Cached(string key, out Texture2D t)
    {
        t = cache.TryGetValue(key, out var found) ? found as Texture2D : null;
        return t != null;
    }

    // ---------------------------------------------------------------- the one layout the wall and its light share

    /// Does cell (bay cx, floor cy) of a `style` atlas hold a WINDOW, or blank stone? Blank comes in two kinds so it
    /// is not noise: whole BLIND BAY COLUMNS (the stair, the lift, a pier) and the odd single cell. Bands (Attic, Shop)
    /// are one floor tall: only cx counts
    public static bool IsWindow(int style, int cx, int cy)
    {
        int seed = 101 + style * 17;
        float column = Hash01(cx, 0, seed + 1), cell = Hash01(cx, cy, seed + 3);
        switch (style)
        {
            case Shop: return column < 0.86f;                               // nearly every ground bay is a shop or a door
            case Attic: return column < 0.82f;
            case Arch: return column > 0.14f;                               // a bank: every bay an arch but the odd pier
            case House: return column > 0.3f && cell > 0.08f;               // the bays between the House's piers
            case Strip: return column > 0.28f;                              // whole blind piers up a Deco shaft
            case Homes: return column > 0.2f && cell > 0.1f;
            default: return column > 0.26f && cell > 0.08f;                 // office rows
        }
    }

    /// A CLUSTER of lit windows: is cell (cx, cy) inside one? The atlas is cut into blocks of six bays by four
    /// floors; a block holds a cluster with probability `p`, a patch two to `maxW` bays wide and one to `maxH`
    /// floors tall set down somewhere inside it. So the lit windows come as patches by floor with dark floors
    /// between, the way a painted facade has them
    static bool InCluster(int cx, int cy, int seed, float p, int maxW, int maxH)
    {
        const int BW = 6, BH = 4;
        int bx = cx / BW, by = cy / BH;
        if (Hash01(bx, by, seed + 21) >= p) return false;
        int w = 2 + (int)(Hash01(bx, by, seed + 22) * (maxW - 1)), h = 1 + (int)(Hash01(bx, by, seed + 23) * maxH);
        int ox = (int)(Hash01(bx, by, seed + 24) * (BW - w + 1)), oy = (int)(Hash01(bx, by, seed + 25) * (BH - h + 1));
        int lx = cx - bx * BW, ly = cy - by * BH;
        return lx >= ox && lx < ox + w && ly >= oy && ly < oy + h;
    }

    /// How bright the window of cell (cx, cy) is at night, 0 for dark. THE WINDOW RULE: on a shaft, clusters by floor
    /// (InCluster) and a few scattered singles, about an eighth of the face lit; the homes dense and scattered (the
    /// tenements and warehouses glow orange); the landmarks' arches mostly lit; the shops at the foot half
    public static float LitLevel(int style, int cx, int cy)
    {
        if (!IsWindow(style, cx, cy)) return 0f;
        int seed = 101 + style * 17;
        float h0 = Hash01(cx, cy, seed + 2), h1 = Hash01(cx, cy, seed + 4);
        switch (style)
        {
            case Strip: return InCluster(cx, cy, seed, 0.5f, 5, 2) || h0 < 0.03f ? 0.6f + 0.4f * h1 : 0f;     // a Deco shaft: runs along a floor or two
            case Homes: return h0 < 0.42f ? 0.45f + 0.55f * h1 : 0f;                                           // the tenements: dense, scattered
            case Arch: return h0 < 0.55f ? 0.6f + 0.4f * h1 : 0f;                                              // the landmarks
            case House: return InCluster(cx, cy, seed, 0.5f, 4, 3) || h0 < 0.03f ? 0.5f + 0.5f * h1 : 0f;     // the House: floors working late
            case Attic: return h0 < 0.25f ? 0.3f + 0.5f * h1 : 0f;
            case Shop: return h0 < 0.5f ? 0.6f + 0.4f * h1 : 0f;                                               // the shopfronts and lobbies
            default: return InCluster(cx, cy, seed, 0.55f, 4, 3) || h0 < 0.03f ? 0.5f + 0.5f * h1 : 0f;       // office rows: patches, and the odd single
        }
    }

    /// For the report: of the atlas's cells, how many are windows, and of those how many are lit
    public static void Stats(int style, out float windows, out float lit)
    {
        int cells = style == Attic || style == Shop ? BandCells : AtlasCells, rows = style == Attic || style == Shop ? 1 : AtlasCells;
        int nw = 0, nl = 0;
        for (int cy = 0; cy < rows; cy++)
            for (int cx = 0; cx < cells; cx++)
            {
                if (!IsWindow(style, cx, cy)) continue;
                nw++;
                if (LitLevel(style, cx, cy) > 0f) nl++;
            }
        windows = nw / (float)(cells * rows);
        lit = nw > 0 ? nl / (float)nw : 0f;
    }

    // ---------------------------------------------------------------- the light a tower wears

    /// A light atlas: AtlasCells floors of AtlasCells bays, lit only where IsWindow says a window is. A facade maps a
    /// run of its own bays and floors onto a stretch of this, from a random corner, so no two facades are the same stretch
    public static Texture2D Windows(int style)
    {
        string key = "windows" + style;
        if (Cached(key, out var done)) return done;
        const int Size = AtlasCells * CellPx;
        var px = new Color32[Size * Size];
        WindowRect(style, out int x0, out int x1, out int y0, out int y1);
        int seed = 101 + style * 17;
        Color amber = new Color(1f, 0.72f, 0.38f), warm = new Color(1f, 0.85f, 0.6f), cream = new Color(1f, 0.93f, 0.76f), sodium = new Color(1f, 0.64f, 0.3f);
        Color bleed = style == Arch ? warm : style == Homes ? sodium : amber;
        var level = new float[AtlasCells, AtlasCells];
        var tint = new Color[AtlasCells, AtlasCells];
        for (int cy = 0; cy < AtlasCells; cy++)
            for (int cx = 0; cx < AtlasCells; cx++)
            {
                level[cx, cy] = LitLevel(style, cx, cy);
                float h2 = Hash01(cx, cy, seed + 5), h3 = Hash01(cx, cy, seed + 6);
                switch (style)
                {
                    case Strip: tint[cx, cy] = Color.Lerp(amber, warm, h2 * 0.6f); break;
                    case Homes: tint[cx, cy] = h2 < 0.1f ? cream : Color.Lerp(sodium, warm, h3); break;   // sodium, the odd bare cream bulb
                    case Arch: tint[cx, cy] = Color.Lerp(warm, cream, h2); break;
                    case House: tint[cx, cy] = Color.Lerp(amber, warm, h2 * 0.5f); break;
                    default: tint[cx, cy] = h2 < 0.04f ? cream : Color.Lerp(amber, warm, h3 * 0.7f); break;
                }
            }
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                int cx = x / CellPx, cy = y / CellPx, lx = x % CellPx, ly = y % CellPx;
                Color32 outPx = C32(bleed, 0f);
                bool window = IsWindow(style, cx, cy);
                bool inWindow = window && lx >= x0 && lx <= x1 && ly >= y0 && ly <= y1 && (style != Arch || InArch(lx, ly, x0, x1, y1));
                // A dark window is NOT drawn in the light: the stone carries its near-black pane (WallAtlas) and
                // that is all a dark window is in this city
                if (inWindow && level[cx, cy] > 0f)
                {
                    float shade = 1f, alpha = 1f;
                    float v = (ly - y0) / (float)Mathf.Max(1, y1 - y0);
                    if (style == Homes || style == Rows || style == House) shade = 0.78f + 0.22f * v;      // the ceiling light is at the top
                    // An old window: a MULLION CROSS in every lit pane (the arches a mullion and a transom)
                    if (style == Arch)
                    {
                        if (lx == 7 || lx == 8) shade *= 0.45f;                                              // the mullion
                        if (ly == 8) shade *= 0.5f;                                                          // the transom
                    }
                    else
                    {
                        if (lx == 8) shade *= 0.5f;                                                          // the mullion
                        if (ly == (y0 + y1) / 2) shade *= 0.5f;                                              // the transom
                    }
                    if (style == Homes && (lx == x0 || lx == x1 || ly == y0 || ly == y1)) alpha = 0.8f;
                    outPx = C32(tint[cx, cy] * (level[cx, cy] * shade), alpha);
                }
                px[y * Size + x] = outPx;
            }
        return Finish(key, Size, Size, px);
    }

    /// The light of a BAND: a row of BandCells cells, one floor tall (`attic`: small squares, few lit) or two floors
    /// tall (a shopfront base: arched openings, most of them lit cream-gold, the lobbies and the shops)
    public static Texture2D BandLight(bool attic)
    {
        string key = attic ? "bandlight_attic" : "bandlight_shop";
        if (Cached(key, out var done)) return done;
        int style = attic ? Attic : Shop;
        int cellH = attic ? CellPx : CellPx * 2;
        int W = BandCells * CellPx, H = cellH;
        var px = new Color32[W * H];
        Color amber = new Color(1f, 0.72f, 0.38f), warm = new Color(1f, 0.85f, 0.6f), cream = new Color(1f, 0.93f, 0.76f);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int cx = x / CellPx, lx = x % CellPx, ly = y;
                Color32 outPx = C32(attic ? amber : warm, 0f);
                bool window = IsWindow(style, cx, 0);
                bool inWindow;
                if (attic) inWindow = window && lx >= 6 && lx <= 9 && ly >= 5 && ly <= 8;
                else inWindow = window && lx >= 4 && lx <= 11 && ly >= 1 && ly <= 24 && InArch(lx, ly, 4, 11, 24);
                float lv = LitLevel(style, cx, 0);
                if (inWindow && lv > 0f)
                {
                    float shade = 1f;
                    if (!attic && (lx == 7 || lx == 8) && ly > 6) shade *= 0.5f;          // the shop's door post / the mullion
                    if (!attic && ly == 18) shade *= 0.55f;                                 // the transom over the door
                    Color tint = attic ? amber : Color.Lerp(warm, cream, Hash01(cx, 3, 909));
                    outPx = C32(tint * (lv * shade), 1f);
                }
                px[y * W + x] = outPx;
            }
        return Finish(key, W, H, px, true, false);
    }

    // ---------------------------------------------------------------- the stone

    /// What a wall kind is cut from: its style (the cell layout it shares with its light), its stone, and whether it is
    /// brick (running bond), a Deco shaft of fluted piers, or blind (no window at all)
    static void KindLook(int kind, out int style, out Color frame, out bool brick, out bool piers, out bool blind)
    {
        style = Rows; brick = false; piers = false; blind = false;
        // The painted palette: UMBER, OLIVE and OCHRE for the stone the light catches, near-black for the rest;
        // mid-dark albedos, never saturated (the grade crushes the darks and wants a ramp to band), a shade apart
        // so a limestone shaft still reads against a charcoal one. The oxblood is the red street's: matte, never a glow
        switch (kind)
        {
            case KindChar: frame = new Color(0.12f, 0.12f, 0.13f); break;
            case KindCream: frame = new Color(0.4f, 0.35f, 0.24f); break;                                  // ochre
            case KindHouse: style = House; frame = new Color(0.1f, 0.1f, 0.115f); break;
            case KindBrick: style = Homes; frame = new Color(0.24f, 0.12f, 0.08f); brick = true; break;
            case KindPierLime: style = Strip; frame = new Color(0.32f, 0.29f, 0.21f); piers = true; break;  // umber
            case KindPierChar: style = Strip; frame = new Color(0.14f, 0.14f, 0.15f); piers = true; break;
            case KindBrownstone: style = Homes; frame = new Color(0.2f, 0.12f, 0.08f); break;
            case KindGranite: frame = new Color(0.16f, 0.16f, 0.17f); break;
            case KindBlindBrick: frame = new Color(0.22f, 0.11f, 0.08f); brick = true; blind = true; break;
            case KindBlindStone: frame = new Color(0.27f, 0.25f, 0.18f); blind = true; break;               // olive
            case KindBlindChar: frame = new Color(0.115f, 0.115f, 0.13f); blind = true; break;
            case KindSoot: frame = new Color(0.09f, 0.08f, 0.08f); brick = true; blind = true; break;
            case KindOxblood: frame = new Color(0.3f, 0.075f, 0.07f); break;                               // the red street: flat crimson
            default: frame = new Color(0.33f, 0.3f, 0.21f); break;                                          // umber-olive limestone
        }
    }

    /// The WALL ATLAS of a kind: AtlasCells bays by AtlasCells floors of near-black stone (or brick), FLAT (the
    /// ashlar barely a shade apart, no recessed panels), the window punched into the cells IsWindow gives it (a
    /// reveal, a sill that catches the light, a lintel), a faint lighter string course every five floors, HARD LINE's
    /// ink joint down and along every bay. A blind kind has no window anywhere; the blind brick carries a faded
    /// painted wall sign
    public static Texture2D WallAtlas(int kind)
    {
        string key = "wallatlas" + kind;
        if (Cached(key, out var done)) return done;
        const int Size = AtlasCells * WallPx, K = WallPx / CellPx;
        var px = new Color32[Size * Size];
        KindLook(kind, out int style, out Color frame, out bool brick, out bool piers, out bool blind);
        WindowRect(style, out int x0, out int x1, out int y0, out int y1);
        Color glass = new Color(0.05f, 0.05f, 0.065f), mortar = new Color(0.14f, 0.1f, 0.09f), paint = new Color(0.5f, 0.44f, 0.34f);
        bool brownstone = kind == KindBrownstone;
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                int cx = x / WallPx, cy = y / WallPx, lx = (x % WallPx) / K, ly = (y % WallPx) / K;
                bool window = !blind && IsWindow(style, cx, cy);
                bool inGlass = window && lx >= x0 && lx <= x1 && ly >= y0 && ly <= y1 && (style != Arch || InArch(lx, ly, x0, x1, y1));
                // Ashlar: blocks half a bay long and a third of a floor high, every course shifted, barely a shade apart
                int course = y / 11, block = (x + (course % 2) * 8) / 16;
                Color c = frame * (0.97f + 0.05f * Hash01(block, course, 31 + kind));
                if (brownstone) c = frame * (0.95f + 0.08f * Hash01(block, course, 31 + kind));
                if (brick)
                {
                    // Running bond: courses four texels high, bricks eight long, every other course shifted half a brick
                    int bc = y / 4, bx = (x + (bc % 2) * 4) / 8;
                    bool joint = y % 4 == 0 || (x + (bc % 2) * 4) % 8 == 0;
                    c = joint ? mortar * (kind == KindSoot ? 0.6f : 1f) : frame * (0.9f + 0.16f * Hash01(bx, bc, 57));
                }
                if (piers)
                {
                    // HEAVY VERTICAL RIBBING: wide fluted piers the full height of the shaft, the flutes a stripe apart
                    bool pier = lx <= 3 || lx >= 12;
                    if (pier) c = frame * (x % 3 == 0 ? 0.62f : x % 3 == 1 ? 1.16f : 0.96f);   // the fluting: a shadow, a lit edge, the flat
                    else if (!window) c = frame * (x % 3 == 0 ? 0.66f : 1.08f);                // a blind bay: a wider fluted pier
                    else if (ly >= 12 || ly <= 2) c = frame * 0.72f;                           // the spandrel under and over the window
                    else c = frame * 0.88f;                                                      // the recessed field
                }
                if (!blind && cy % 5 == 4 && y % WallPx >= WallPx - 4) c = frame * 1.35f;      // the string course: the lighter band every five floors that lets the steps read
                if (inGlass)
                {
                    float v = (ly - y0) / (float)Mathf.Max(1, y1 - y0 + 1);
                    c = glass * (0.8f + 0.4f * v);                                              // the pane, near-black, the sky faintly in the top of it
                    if (lx == x0 || lx == x1 || ly == y0) c = frame * 0.4f;                     // the reveal, in shadow
                }
                else if (window && ly == y0 - 1 && lx >= x0 - 1 && lx <= x1 + 1) c = frame * 1.2f;    // the sill catches what light there is
                else if (window && ly == y1 + 1 && lx >= x0 && lx <= x1) c = frame * (brick ? 0.65f : 0.85f);   // the lintel
                else if (window && style == Arch && ly == y1 + 1 && lx >= x0 - 1 && lx <= x1 + 1) c = frame * 1.1f;
                if (kind == KindBlindBrick && cx >= 9 && cx <= 22 && cy >= 11 && cy <= 18)
                {
                    // A painted wall sign, faded: a cream panel, three lines of blocky dark lettering
                    c = Color.Lerp(c, paint, 0.55f);
                    bool line = cy == 12 || cy == 14 || cy == 16;
                    if (line && ly >= 4 && ly <= 11 && cx >= 10 && cx <= 21 && Hash01(x / 10, cy, 5) < 0.62f) c = Color.Lerp(c, mortar, 0.6f);
                }
                if (x % WallPx == 0 || y % WallPx == 0) c = c * 0.5f;                            // HARD LINE: the ink joint of every bay
                px[y * Size + x] = C32(c, 1f);
            }
        return Finish(key, Size, Size, px);
    }

    /// A BAND of wall, BandCells bays wide, tiled sideways: a BASE of two floors (rusticated granite for the stone
    /// kinds, dark brick for the brick ones, an arched opening two floors tall in every shop bay, the door post down
    /// its middle), or an ATTIC of one floor in the kind's own stone (small square windows, a string course under it,
    /// the cornice's shadow over it)
    public static Texture2D WallBand(int kind, bool attic)
    {
        string key = "band" + kind + (attic ? "a" : "b");
        if (Cached(key, out var done)) return done;
        const int K = WallPx / CellPx;
        KindLook(kind, out _, out Color frame, out bool brick, out _, out _);
        int style = attic ? Attic : Shop;
        int cellH = attic ? WallPx : WallPx * 2;
        int W = BandCells * WallPx, H = cellH;
        var px = new Color32[W * H];
        Color stone = attic ? frame : brick ? new Color(0.2f, 0.11f, 0.08f) : new Color(0.17f, 0.17f, 0.19f);
        Color glass = new Color(0.05f, 0.05f, 0.065f), mortar = new Color(0.14f, 0.1f, 0.09f);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int cx = x / WallPx, lx = (x % WallPx) / K, ly = y / K;
                bool window = IsWindow(style, cx, 0);
                Color c;
                if (attic)
                {
                    int course = y / 11, block = (x + (course % 2) * 8) / 16;
                    c = stone * (0.93f + 0.1f * Hash01(block, course, 41 + kind));
                    if (brick) { int bc = y / 4, bx = (x + (bc % 2) * 4) / 8; c = (y % 4 == 0 || (x + (bc % 2) * 4) % 8 == 0) ? mortar : stone * (0.82f + 0.3f * Hash01(bx, bc, 57)); }
                    bool inGlass = window && lx >= 6 && lx <= 9 && ly >= 5 && ly <= 8;
                    if (y < 3) c = stone * 1.22f;                                                // the string course under the attic
                    if (y >= H - 3) c = stone * 0.55f;                                           // the cornice's shadow
                    if (inGlass) c = lx == 6 || lx == 9 || ly == 5 ? stone * 0.45f : glass * 1.1f;
                    else if (window && ly == 4 && lx >= 5 && lx <= 10) c = stone * 1.18f;        // the sill
                }
                else
                {
                    // Rusticated: deep bevelled joints every eleven texels, the blocks a shade apart; brick for the brick kinds
                    int course = y / 11, block = (x + (course % 2) * 12) / 24;
                    bool joint = y % 11 == 0 || (x + (course % 2) * 12) % 24 == 0;
                    c = joint ? stone * 0.45f : stone * (0.9f + 0.16f * Hash01(block, course, 43 + kind));
                    if (brick) { int bc = y / 4, bx = (x + (bc % 2) * 4) / 8; c = (y % 4 == 0 || (x + (bc % 2) * 4) % 8 == 0) ? mortar : stone * (0.82f + 0.3f * Hash01(bx, bc, 57)); }
                    bool inArch = window && lx >= 4 && lx <= 11 && ly >= 1 && ly <= 24 && InArch(lx, ly, 4, 11, 24);
                    if (y >= H - 4) c = stone * 1.2f;                                            // the band's top: a cornice over the base
                    if (inArch)
                    {
                        c = glass * (0.8f + 0.5f * Mathf.Clamp01(ly / 24f));
                        if (lx == 4 || lx == 11 || ly == 1) c = stone * 0.4f;                    // the reveal
                        else if ((lx == 7 || lx == 8) && ly > 6) c = stone * 0.7f;                // the door post
                        else if (ly == 18) c = stone * 0.7f;                                     // the transom
                    }
                    else if (window && ly == 25 && lx >= 3 && lx <= 12) c = stone * 1.25f;       // the keystone course over the arch
                }
                if (x % WallPx == 0) c = c * 0.5f;                                               // HARD LINE: the joint of every bay
                px[y * W + x] = C32(c, 1f);
            }
        return Finish(key, W, H, px, true, false);
    }

    // ---------------------------------------------------------------- the ground, and the air

    /// A promenade, as light: a line of lamps down either edge with a stud in it every few metres, and the faintest
    /// glow between. Nothing else: the wet ground under it keeps its reflections. White; the material says what colour
    public static Texture2D Promenade()
    {
        const string key = "promenade";
        if (Cached(key, out var done)) return done;
        const int S = 64;
        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) / S, v = (y + 0.5f) / S;
                float edge = Mathf.Min(Mathf.Abs(u - 0.09f), Mathf.Abs(u - 0.91f));
                float line = Mathf.Clamp01(1f - edge / 0.035f);
                float stud = Mathf.Clamp01(1f - Mathf.Abs(Mathf.Repeat(v * 2f, 1f) - 0.5f) / 0.12f) * Mathf.Clamp01(1f - edge / 0.07f);
                float glow = 0.1f * Mathf.Clamp01(1f - Mathf.Abs(u - 0.5f) / 0.5f);
                px[y * S + x] = C32(Color.white, Mathf.Clamp01(Mathf.Max(line * 0.8f, stud) + glow));
            }
        return Finish(key, S, S, px, false, true);
    }

    /// A lane of traffic, 256 m of it: soft bright capsules at uneven gaps. Scrolled along the lane by whoever
    /// owns the material. White so the material's colour decides whether it is coming or going
    public static Texture2D Traffic()
    {
        const string key = "traffic";
        if (Cached(key, out var done)) return done;
        const int W = 16, H = 1024;
        var a = new float[W * H];
        int cars = 0;
        float at = 6f;
        while (at < H - 40f && cars < 40)
        {
            float len = 14f + Hash01(cars, 3, 77) * 8f;                       // 3.5 to 5.5 m, at four pixels a metre
            for (int y = (int)at; y < at + len && y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float du = (x + 0.5f - W * 0.5f) / (W * 0.36f), dv = (y + 0.5f - at - len * 0.5f) / (len * 0.5f);
                    float d = du * du + dv * dv;
                    float body = Mathf.Clamp01(1.15f - d);
                    float lamp = dv > 0.35f ? Mathf.Clamp01(1.4f - d * 0.9f) : 0f;   // the lit end leads
                    a[y * W + x] = Mathf.Max(a[y * W + x], Mathf.Clamp01(body * 0.45f + lamp));
                }
            at += len + 16f + Hash01(cars, 5, 77) * Hash01(cars, 6, 77) * 170f;   // bunched, then a gap: traffic
            cars++;
        }
        var px = new Color32[W * H];
        for (int i = 0; i < px.Length; i++) px[i] = C32(Color.white, a[i]);
        return Finish(key, W, H, px, false, true);
    }

    /// A TRAIN on the elevated: 512 m of track with one train of five carriages on it, each a row of lit windows
    /// (a metre of amber, half a metre of frame) and a lamp on the leading end; the body is not drawn, the viaduct
    /// is dark anyway. Scrolled along the track by whoever owns the material. White; the material tints it amber
    public static Texture2D Carriages()
    {
        const string key = "carriages";
        if (Cached(key, out var done)) return done;
        const int W = 8, H = 2048;   // four pixels a metre
        var px = new Color32[W * H];
        float at = 120f;
        for (int car = 0; car < 5; car++)
        {
            int y0 = (int)at, len = 56;   // fourteen metres
            for (int y = y0 + 4; y < y0 + len - 4; y++)
            {
                int k = (y - y0 - 4) % 6;
                if (k >= 4) continue;   // the frame between windows
                for (int x = 1; x < W - 1; x++) px[y * W + x] = C32(Color.white, 0.9f);
            }
            if (car == 4) for (int y = y0 + len - 4; y < y0 + len; y++) for (int x = 2; x < W - 2; x++) px[y * W + x] = C32(Color.white, 1f);   // the head lamp
            at += len + 6f;
        }
        return Finish(key, W, H, px, false, true);
    }

    // ---------------------------------------------------------------- light

    /// A BANK OF FOG lying between the ranks of towers (MenuCity.HazeBanks): a tiling sheet of soft-edged alpha, thick
    /// in its middle, gone at its edges, so a flat quad of it reads as a painted bank and never as a sheet. White;
    /// the material tints it
    public static Texture2D HazeBank()
    {
        const string key = "hazebank";
        if (Cached(key, out var done)) return done;
        const int S = 256;
        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) / S, v = (y + 0.5f) / S;
                float wu = Fbm(u, v, 2, 2, 611) - 0.5f, wv = Fbm(u, v, 2, 2, 643) - 0.5f;
                float d = Fbm(u + wu * 0.22f, v + wv * 0.22f, 3, 4, 677);
                float a = Mathf.Clamp01((d - 0.36f) / 0.3f);
                a = a * a * (3f - 2f * a);
                px[y * S + x] = C32(Color.white, a);
            }
        return Finish(key, S, S, px, true, true, 2);
    }

    /// A floodlight's wash up a wall: solid at the foot, gone by the top. White; the material tints it
    public static Texture2D Wash()
    {
        const string key = "wash";
        if (Cached(key, out var done)) return done;
        const int W = 4, H = 128;
        var px = new Color32[W * H];
        for (int y = 0; y < H; y++)
        {
            float v = y / (float)(H - 1);
            float a = Mathf.Pow(1f - v, 2.4f);
            for (int x = 0; x < W; x++) px[y * W + x] = C32(Color.white, a);
        }
        return Finish(key, W, H, px, false, false, 1);
    }

    /// A soft round glow: lightning inside a cloud, the bloom of a lamp
    public static Texture2D Glow()
    {
        const string key = "glow";
        if (Cached(key, out var done)) return done;
        const int S = 128;
        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = (x + 0.5f) / S * 2f - 1f, dy = (y + 0.5f) / S * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - r);
                px[y * S + x] = C32(Color.white, a * a * (3f - 2f * a));
            }
        return Finish(key, S, S, px, false, false, 1);
    }

    /// A beam of light seen from the side: bright down its middle, gone at its edges, fading along its length.
    /// u runs ACROSS the beam, v ALONG it (0 at the lamp)
    public static Texture2D Beam()
    {
        const string key = "beam";
        if (Cached(key, out var done)) return done;
        const int W = 32, H = 128;
        var px = new Color32[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = (x + 0.5f) / W * 2f - 1f, v = (y + 0.5f) / H;
                float across = Mathf.Clamp01(1f - u * u);
                float along = Mathf.Pow(1f - v, 1.6f) * Mathf.Clamp01(v * 30f);
                px[y * W + x] = C32(Color.white, across * across * along);
            }
        return Finish(key, W, H, px, false, false, 1);
    }

    // ---------------------------------------------------------------- noise

    static float Smooth(float t) => t * t * (3f - 2f * t);

    /// Value noise that tiles every `period` cells
    static float Noise(float x, float y, int period, int seed) => Noise(x, y, period, period, seed);

    /// ...or every `px` cells across and `py` up
    static float Noise(float x, float y, int px, int py, int seed)
    {
        int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
        float fx = Smooth(x - xi), fy = Smooth(y - yi);
        int x0 = ((xi % px) + px) % px, y0 = ((yi % py) + py) % py;
        int x1 = (x0 + 1) % px, y1 = (y0 + 1) % py;
        float a = Hash01(x0, y0, seed), b = Hash01(x1, y0, seed), c = Hash01(x0, y1, seed), d = Hash01(x1, y1, seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }

    /// Tiling fractal noise over [0,1)², `cells` lattice cells across at the coarsest octave
    static float Fbm(float u, float v, int cells, int octaves, int seed)
    {
        float sum = 0f, amp = 0.5f, norm = 0f;
        int period = cells;
        for (int o = 0; o < octaves; o++)
        {
            sum += Noise(u * period, v * period, period, seed + o * 13) * amp;
            norm += amp;
            amp *= 0.5f;
            period *= 2;
        }
        return sum / norm;
    }

    /// The storm's cloud deck, seen from underneath: slate, with sepia bellies where the city lights it, thin pale
    /// edges, gaps. Tiles.
    public static Texture2D Clouds()
    {
        const string key = "clouds";
        if (Cached(key, out var done)) return done;
        const int S = 384;
        var px = new Color32[S * S];
        Color belly = new Color(0.15f, 0.115f, 0.1f), rim = new Color(0.4f, 0.42f, 0.49f);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) / S, v = (y + 0.5f) / S;
                // Warp the lookup by a second noise so the shapes billow instead of lying in a grid
                float wu = Fbm(u, v, 3, 2, 401) - 0.5f, wv = Fbm(u, v, 3, 2, 433) - 0.5f;
                float d = Fbm(u + wu * 0.18f, v + wv * 0.18f, 4, 5, 467);
                float a = Mathf.Clamp01((d - 0.40f) / 0.22f);
                a = a * a * (3f - 2f * a);
                float thick = Mathf.Clamp01((d - 0.46f) / 0.26f);
                Color c = Color.Lerp(rim, belly, thick);
                px[y * S + x] = C32(c, a * 0.96f);
            }
        return Finish(key, S, S, px, true, true, 2);
    }

    /// The wet ground: how WET. Alpha is how much of the ground is road and how much is mirror: low in the
    /// puddles, high where the surface has drained. Colour barely varies. Tiles.
    public static Texture2D Wet()
    {
        const string key = "wet";
        if (Cached(key, out var done)) return done;
        const int S = 256;
        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) / S, v = (y + 0.5f) / S;
                float n = Fbm(u, v, 5, 4, 911);
                float puddle = Mathf.Clamp01((n - 0.42f) / 0.2f);
                puddle = puddle * puddle * (3f - 2f * puddle);
                float shade = 0.85f + 0.15f * Fbm(u, v, 16, 2, 947);
                px[y * S + x] = C32(new Color(shade, shade, shade), Mathf.Lerp(0.70f, 1f, puddle));
            }
        return Finish(key, S, S, px, true, true, 4);
    }

    // ---------------------------------------------------------------- the sky

    /// The haze at the horizon, as it is seen, `degrees` of azimuth away from where the sun went down: a dark
    /// indigo everywhere, burnt sienna where the last of it is. One function for the sky's paint AND the scene's
    /// fog, so a far tower dissolves into exactly the sky behind it
    public static Color Haze(float degrees)
    {
        Color away = new Color(0.12f, 0.125f, 0.165f), toward = new Color(0.5f, 0.27f, 0.14f);
        float k = Mathf.Exp(-(degrees * degrees) / (62f * 62f));
        return Color.Lerp(away, toward, k);
    }

    /// The sky's colour (as seen) in a direction: `azFromSun` degrees of azimuth off the sun, `elev` degrees up
    static Color SkyColor(float azFromSun, float elev, float az, bool stars = true)
    {
        Color haze = Haze(azFromSun);
        if (elev < 0f)
        {
            // Below the horizon there is only haze, darkening: the ground hides nearly all of it
            return Color.Lerp(haze, haze * 0.45f, Mathf.Clamp01(-elev / 25f));
        }
        // Deep indigo: lighter and warmer at the horizon, near-black at the zenith
        Color low = new Color(0.1f, 0.11f, 0.19f), mid = new Color(0.065f, 0.07f, 0.145f), zenith = new Color(0.035f, 0.04f, 0.095f);
        Color c = Color.Lerp(haze, low, Smooth(Mathf.Clamp01((elev - 15f) / 22f)));
        c = Color.Lerp(c, mid, Smooth(Mathf.Clamp01((elev - 32f) / 26f)));
        c = Color.Lerp(c, zenith, Smooth(Mathf.Clamp01((elev - 52f) / 38f)));
        // Where the sun went down: a band of burnt sienna left along the horizon, and a wider warmth above that
        float ax = Mathf.Exp(-(azFromSun * azFromSun) / (34f * 34f));
        float band = ax * Mathf.Exp(-(elev * elev) / (5.5f * 5.5f));
        float warmth = Mathf.Exp(-(azFromSun * azFromSun) / (70f * 70f)) * Mathf.Exp(-(elev * elev) / (19f * 19f));
        c += new Color(0.9f, 0.42f, 0.14f) * (band * 0.9f) + new Color(0.38f, 0.16f, 0.08f) * (warmth * 0.5f);
        if (elev >= 16f)
        {
            // A few stars, where the haze has thinned: one texel each, the brighter ones higher up
            float star = Hash01(Mathf.RoundToInt(az * 2.85f), Mathf.RoundToInt(elev * 2.85f), 1931);
            if (stars && star > 0.9988f)
            {
                float k = Mathf.Clamp01((elev - 16f) / 30f) * (1f - warmth) * (0.5f + 0.5f * Hash01(Mathf.RoundToInt(az * 2.85f), 3, 1933));
                c += new Color(0.5f, 0.5f, 0.56f) * k;
            }
            return c;
        }
        // Strata across it: long dark bars of cloud low on the horizon, which is what makes a sunset a sunset
        float strata = Fbm(az / 360f, elev / 14f, 6, 4, 1303);
        float bars = Smooth(Mathf.Clamp01((strata - 0.5f) / 0.16f)) * Mathf.Clamp01(1f - elev / 16f) * Mathf.Clamp01(elev / 1.5f);
        return Color.Lerp(c, haze * 0.62f, bars * 0.8f);
    }

    /// The sky as one long-lat picture (u = azimuth from +z toward +x, v = elevation −90..90), half-float, linear
    public static Texture2D SkyDome(float sunAzimuth)
    {
        string key = "sky" + Mathf.RoundToInt(sunAzimuth);
        if (Cached(key, out var done)) return done;
        const int W = 1024, H = 512;
        var px = new Color[W * H];
        for (int y = 0; y < H; y++)
        {
            float elev = (y + 0.5f) / H * 180f - 90f;
            for (int x = 0; x < W; x++)
            {
                float az = (x + 0.5f) / W * 360f;
                float d = Mathf.DeltaAngle(az, sunAzimuth);
                Color c = SkyColor(Mathf.Abs(d), elev, az).linear;
                c.a = 1f;
                px[y * W + x] = c;
            }
        }
        var t = new Texture2D(W, H, TextureFormat.RGBAHalf, false, true) { name = "City_" + key, filterMode = FilterMode.Bilinear };
        t.wrapModeU = TextureWrapMode.Repeat;
        t.wrapModeV = TextureWrapMode.Clamp;
        t.SetPixels(px);
        t.Apply(false, true);
        t.hideFlags = HideFlags.HideAndDontSave;
        cache[key] = t;
        return t;
    }

    /// What the wet ground and the black marble mirror: the same sky with no sun in it, so it is the same from every
    /// side and cannot be the wrong way round. Small; blurred by its own mips
    public static Cubemap SkyCube()
    {
        const string key = "skycube";
        if (cache.TryGetValue(key, out var found) && found is Cubemap kept && kept != null) return kept;
        const int N = 32;
        var cube = new Cubemap(N, TextureFormat.RGBAHalf, true) { name = "City_" + key };
        var px = new Color[N * N];
        var faces = new[] { CubemapFace.PositiveX, CubemapFace.NegativeX, CubemapFace.PositiveY, CubemapFace.NegativeY, CubemapFace.PositiveZ, CubemapFace.NegativeZ };
        foreach (var face in faces)
        {
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f;
                    // Only the HEIGHT of the direction matters, so which way a face's u and v run does not
                    float h;
                    switch (face)
                    {
                        case CubemapFace.PositiveY: h = 1f / Mathf.Sqrt(1f + u * u + v * v); break;
                        case CubemapFace.NegativeY: h = -1f / Mathf.Sqrt(1f + u * u + v * v); break;
                        default: h = v / Mathf.Sqrt(1f + u * u + v * v); break;
                    }
                    float elev = Mathf.Abs(Mathf.Asin(Mathf.Clamp(h, -1f, 1f)) * Mathf.Rad2Deg);   // mirrored top to bottom: see above
                    Color c = SkyColor(90f, elev, 0f, false).linear * 1.15f;
                    c.a = 1f;
                    px[y * N + x] = c;
                }
            cube.SetPixels(px, face);
        }
        cube.Apply(true, true);
        cube.hideFlags = HideFlags.HideAndDontSave;
        cache[key] = cube;
        return cube;
    }
}
