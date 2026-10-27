using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DARK DECO. The menus are Halcyon Civic's own letterhead, Gotham 1939: black
/// paper, cream type, brass rules and stepped frames, oxblood stripes, amber for
/// the company's own light. The Persona bones stay (the -16° band, the -2.5°
/// slash rows, hard offset shadows, selection that inverts to the lit sheet,
/// staggered throw-ins with a little overshoot); the dress is a 1930s
/// corporation's: a letterspaced masthead on a sunburst, a double rule under
/// every heading, chevron rules, a stepped cornice under the band, a
/// searchlight sweeping it, the company's sun as the cursor and the seal.
/// The title is a two-plate letterpress job whose plates kick on every move.
/// The rot keeps its leaks: every ten-odd seconds one idle row ticks red and
/// ink and says a word it should not. The company mark never glitches.
/// All IMGUI, all procedural, nothing allocated per frame.
/// </summary>
public static class MenuStyle
{
    // ---------------------------------------------------------------- the palette
    // Black paper, cream type, brass rules, oxblood accents, amber for the company's own light
    public static readonly Color Gold = new Color(0.96f, 0.76f, 0.3f);        // gilt: the mark, the grades, the top of the ladder
    public static readonly Color Brass = new Color(0.78f, 0.58f, 0.25f);      // rules, frames, slivers, hairlines
    public static readonly Color Foil = new Color(1f, 0.9f, 0.55f);           // lit brass: the sweep, the burst, a hairline catching light
    public static readonly Color Bullion = new Color(0.35f, 0.24f, 0.12f);    // dark bronze: trims, rules on cream — never text
    public static readonly Color Ink = new Color(0.05f, 0.05f, 0.06f);        // black marble: text on cream, chips, tracks
    public static readonly Color Panel = new Color(0.11f, 0.11f, 0.13f);      // charcoal: idle rows, cards, slabs
    public static readonly Color Paper = new Color(0.94f, 0.88f, 0.74f);      // cream: labels on charcoal, the civic sheet, the print flash
    public static readonly Color Sick = new Color(0.56f, 0.53f, 0.46f);       // limestone: dim text, hairlines, ticks — never on cream
    public static readonly Color Burnt = new Color(0.2f, 0.12f, 0.03f);       // dim text on a lit cream sheet
    public static readonly Color Rot = new Color(1f, 0.15f, 0.1f);            // the rot's red: its ticks and leaks, LOCKED — the story, untouched
    public static readonly Color Oxblood = new Color(0.32f, 0.08f, 0.08f);    // the first printing plate, the stripes
    public static readonly Color DeepTeal = new Color(0.55f, 0.38f, 0.14f);   // the second printing plate: bronze now (the field keeps the spec's name)
    public static readonly Color Amber = new Color(1f, 0.62f, 0.1f);          // a unit's own light (the HUD's); never menu type — the menus print, they do not glow
    public static readonly Color Veil = new Color(0.02f, 0.025f, 0.05f);      // night: the full-screen overlay tint
    public static readonly Color Locked = new Color(0.07f, 0.07f, 0.08f);     // a locked card's flat panel
    // The plant (Chapter 2): brick, iron, mustard
    public static readonly Color Mustard = new Color(0.72f, 0.55f, 0.18f);    // the Works' lit sheet
    public static readonly Color Brick = new Color(0.42f, 0.19f, 0.13f);      // the Works' stripe
    public static readonly Color Iron = new Color(0.16f, 0.16f, 0.18f);       // the Works' idle sheets
    public static readonly Color IronDark = new Color(0.09f, 0.09f, 0.1f);    // trims
    public static readonly Color Soot = new Color(0.62f, 0.58f, 0.52f);       // dim text on iron
    // The rooftops (Chapter 3): charcoal and brass
    public static readonly Color Charcoal = new Color(0.085f, 0.085f, 0.1f);  // the rooftops' idle sheets
    public static readonly Color Ashen = new Color(0.6f, 0.6f, 0.56f);        // dim text on charcoal

    // The instrument panel's materials: the wax of the seal, the felt of the directory board, the mercury
    public static readonly Color Wax = new Color(0.55f, 0.08f, 0.06f);        // sealing wax: the grade on the bill
    public static readonly Color WaxDark = new Color(0.3f, 0.04f, 0.03f);     // the wax where it is pressed
    public static readonly Color Felt = new Color(0.06f, 0.06f, 0.07f);       // the directory board's felt

    // The HUD's colour language, hoisted so it lives in one place: their MEANING never changes
    public static readonly Color HudCyan = new Color(0.3f, 1f, 1f);          // cut here
    public static readonly Color HudGold = new Color(1f, 0.85f, 0.2f);       // parry, reset, flow
    public static readonly Color HudRed = new Color(1f, 0.3f, 0.25f);        // unparryable, danger, whiff
    public static readonly Color HudAmber = new Color(1f, 0.62f, 0.1f);      // a unit's own light
    public static readonly Color HudHealth = new Color(0.85f, 0.22f, 0.18f);
    public static readonly Color HudLow = new Color(1f, 0.5f, 0.15f);

    // ---------------------------------------------------------------- the skins
    /// What the menus are made of on a given floor. HALCYON CIVIC (Chapter 1, the main menu) prints on cream with
    /// brass; HALCYON WORKS (Chapter 2) on iron with a mustard sheet, brick stripes and chevron friezes for its
    /// slivers; HALCYON RESPONSE (Chapter 3, the rooftops) on charcoal with a brass sheet. The bones are the same
    /// either way; only the material changes. Gold stays the mark and the grades on all three.
    public sealed class Skin
    {
        public string name, chip, pauseLine, serviceLine, ticker;
        public Color sheet, sheetDim, slate, accent, trim, stripe, mark, subDim, tickerText, ray;
        public bool chevron;   // slivers and stripes are chevron friezes in ink, not plain brass
        public float tickerW;  // the ticker's width at the scale it was last measured at (tickerFactor); the band re-measures on a change
        public float tickerFactor = -1f;
    }

    public static readonly Skin Civic = new Skin
    {
        name = "CIVIC", chip = "THE HOUSE · PROGRAMME · ACT I", pauseLine = "INTERMISSION · THE HOUSE PROVIDES", serviceLine = "THE PERFORMANCE RESUMES AT YOUR PLEASURE",
        sheet = Paper, sheetDim = Burnt, slate = Panel, accent = Brass, trim = Bullion, stripe = Oxblood, mark = Bullion, subDim = Sick, tickerText = Paper,
        ray = new Color(0.35f, 0.24f, 0.12f, 0.22f), chevron = false,
    };
    public static readonly Skin Works = new Skin
    {
        name = "WORKS", chip = "THE WORKS · PROGRAMME · ACT II", pauseLine = "INTERMISSION · THE WORKS", serviceLine = "THE SHIFT RESUMES AT YOUR PLEASURE",
        sheet = Mustard, sheetDim = new Color(0.2f, 0.13f, 0.03f), slate = Iron, accent = new Color(0.9f, 0.7f, 0.25f), trim = IronDark, stripe = Brick,
        mark = new Color(0.16f, 0.1f, 0.03f), subDim = Soot, tickerText = new Color(0.9f, 0.7f, 0.25f),
        ray = new Color(0.3f, 0.18f, 0.05f, 0.25f), chevron = true,
    };
    /// HALCYON RESPONSE (Chapter 3): the division that arrives after. A brass sheet on charcoal — and nothing it prints is true.
    public static readonly Skin Response = new Skin
    {
        name = "RESPONSE", chip = "THE HEIGHTS · PROGRAMME · ACT III", pauseLine = "INTERMISSION · THE HOUSE ASKS FOR CALM", serviceLine = "THE PERFORMANCE RESUMES · REMAIN SEATED",
        sheet = Brass, sheetDim = new Color(0.12f, 0.08f, 0.02f), slate = Charcoal, accent = new Color(0.95f, 0.78f, 0.42f), trim = IronDark, stripe = Oxblood,
        mark = new Color(0.14f, 0.1f, 0.03f), subDim = Ashen, tickerText = new Color(0.95f, 0.78f, 0.42f),
        ray = new Color(0.05f, 0.05f, 0.06f, 0.14f), chevron = false,
    };
    /// The skin everything draws with right now: the menu sets it per page, the pause and the report card from the level
    public static Skin S = Civic;
    /// The skin of the level being played, set by the bootstrap after the build
    public static Skin LevelSkin = Civic;
    public static void Use(Skin s) => S = s ?? Civic;
    /// The first chapter is the Civic tower, the second the Works, the third the Response's rooftops
    public static Skin SkinFor(int chapter) => chapter >= 2 ? Response : chapter == 1 ? Works : Civic;

    /// What the plant says about itself, on the Works band's ticker
    static readonly string[] WorksLines =
    {
        "HALCYON WORKS", "DIG SAFE", "HARD HAT AREA", "AUTHORISED UNITS ONLY", "MIND THE TRAFFIC", "HOT METAL · NO STANDING",
        "PRODUCTION IS SAFETY", "TONNAGE TODAY: ON TARGET", "YOUR SHIFT IS OUR SHIFT",
    };

    /// What the Response says to a district it has sealed
    static readonly string[] ResponseLines =
    {
        "HALCYON RESPONSE", "THERE IS NO CAUSE FOR ALARM", "REMAIN WHERE YOU ARE", "HELP IS COMING", "THE AIR IS SAFE",
        "NOTHING HAS HAPPENED", "CONTAINMENT IS A KINDNESS", "REPORT THE UNACCOUNTED FOR", "YOUR DISTRICT IS BEING CARED FOR",
    };

    /// The band's tickers: built once the styles exist, measured once
    static void BuildTickers()
    {
        Civic.ticker = Loop(LevelKit.Taglines);
        Works.ticker = Loop(WorksLines);
        Response.ticker = Loop(ResponseLines);
        Civic.tickerW = Measure(Doc, Civic.ticker).x;
        Works.tickerW = Measure(Doc, Works.ticker).x;
        Response.tickerW = Measure(Doc, Response.ticker).x;
    }

    /// The lines joined into one long run, four times over, so a scroll of one run's length wraps seamlessly
    static string Loop(string[] lines)
    {
        string one = string.Join("     ·     ", lines) + "     ·     ";
        return one + one + one + one;
    }

    /// Title is the condensed face at the size the host sets (the cutscenes copy it for their own cards); the
    /// masthead the menus actually print is Masthead — the company's wide face, letterspaced, at 0.74 of that size
    public static GUIStyle Title, Masthead, Item, ItemSelected, Sub, Small, Tagline, Brand, Doc, Index, Code, Watermark;
    /// The register's type: Slip is the guest's line in the display face, Register the small caps under it (set
    /// letterspaced through TrackedLabel, never wrapped — the slip measures its lines)
    public static GUIStyle Slip, Register;
    static readonly GUIContent scratch = new GUIContent();
    static readonly string[] IndexText = BuildIndexText();
    static string[] BuildIndexText() { var a = new string[100]; for (int i = 0; i < a.Length; i++) a[i] = i.ToString("00"); return a; }
    /// A two-digit index ('01'…'99') from the shared table: never built per frame
    public static string IndexLabel(int i) => IndexText[Mathf.Clamp(i, 0, 99)];

    public const float BandAngle = -16f;
    public const float RowTilt = -2.5f;
    const float MastheadScale = 0.74f;
    /// Letterspacing on the masthead, in ems
    public const float Tracking = 0.16f;

    /// The last selection change, and the last thing that should kick the title's plates (Bump, page opens, drop-frames)
    public static float SelectedAt { get; private set; } = -10f;
    public static float KickAt { get; set; } = -10f;
    /// Every selection change comes through here: the jitter, the cursor spin, the plate kick
    public static void Bump() { SelectedAt = KickAt = Time.unscaledTime; }

    static Rect confirmRect;
    static float confirmAt = -10f;
    /// A pick landed here: a brass afterimage burns on the spot for a tenth of a second, over whatever comes next
    public static void Confirm(Rect r) { confirmRect = r; confirmAt = Time.unscaledTime; }
    public static void DrawAfterimage()
    {
        float age = Time.unscaledTime - confirmAt;
        if (age < 0f || age > 0.1f) return;
        DrawRotRect(confirmRect, RowTilt, new Color(S.accent.r, S.accent.g, S.accent.b, 0.8f * (1f - age / 0.1f)));
    }

    /// The throw-in: ease-out-back, a 5 % overshoot — the sheet lands a hair past its mark and settles
    public static float Ease(float s)
    {
        float k = Mathf.Clamp01(s) - 1f;
        return 1f + 2.2f * k * k * k + 1.2f * k * k;
    }

    public static Vector2 Measure(GUIStyle style, string text)
    {
        scratch.text = text;
        return style.CalcSize(scratch);
    }

    static Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

    public static void EnsureStyles()
    {
        GameFont.Apply(); // the body face goes on the skin, so every style below inherits it
        if (Title != null) return;
        MenuArt.Warm();
        Title = new GUIStyle(GUI.skin.label) { font = GameFont.Title, fontSize = 72, fontStyle = GameFont.TitleStyle, alignment = TextAnchor.MiddleLeft };
        Masthead = new GUIStyle(GUI.skin.label) { font = GameFont.Brand, fontSize = 53, alignment = TextAnchor.MiddleLeft };
        Item = new GUIStyle(GUI.skin.label) { font = GameFont.Title, fontSize = 24, fontStyle = GameFont.TitleStyle, alignment = TextAnchor.MiddleLeft };
        ItemSelected = new GUIStyle(Item) { fontSize = 28 };
        Sub = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleRight };
        Small = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleLeft };
        Tagline = new GUIStyle(GUI.skin.label) { font = GameFont.Title, fontSize = 18, fontStyle = GameFont.TitleStyle, alignment = TextAnchor.MiddleCenter };
        Brand = new GUIStyle(GUI.skin.label) { font = GameFont.Brand, fontSize = 15, alignment = TextAnchor.MiddleLeft };
        Doc = new GUIStyle(GUI.skin.label) { font = GameFont.Brand, fontSize = 12, alignment = TextAnchor.MiddleLeft };
        Index = new GUIStyle(GUI.skin.label) { font = GameFont.Brand, fontSize = 11, alignment = TextAnchor.MiddleLeft };
        Code = new GUIStyle(GUI.skin.label) { font = GameFont.Brand, fontSize = 16, alignment = TextAnchor.MiddleLeft };
        Watermark = new GUIStyle(GUI.skin.label) { font = GameFont.Title, fontSize = 300, fontStyle = GameFont.TitleStyle, alignment = TextAnchor.MiddleCenter };
        Slip = new GUIStyle(Item) { fontSize = 30, wordWrap = false };
        Register = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleLeft, wordWrap = false };
        BuildTickers();
    }

    // ---------------------------------------------------------------- the masthead
    /// The masthead is set one glyph at a time with tracking (IMGUI has no letterspacing): the layout is measured
    /// once per text and size and kept, the glyph strings with it, so a frame allocates nothing
    sealed class Spaced
    {
        public string[] glyphs;
        public float[] x, w;
        public float width, height;
    }
    static readonly Dictionary<(string text, int size), Spaced> spaced = new Dictionary<(string, int), Spaced>();
    static readonly string[] glyphText = new string[128];

    static Spaced MastheadLayout(string text)
    {
        Masthead.fontSize = Mathf.Max(8, Mathf.RoundToInt(Title.fontSize * MastheadScale));
        var key = (text, Masthead.fontSize);
        if (spaced.TryGetValue(key, out var lay)) return lay;
        int n = text.Length;
        lay = new Spaced { glyphs = new string[n], x = new float[n], w = new float[n] };
        float track = Masthead.fontSize * Tracking, pad = Masthead.padding.horizontal, x = 0f, h = 0f;
        for (int i = 0; i < n; i++)
        {
            char c = text[i];
            string g = c < 128 ? (glyphText[c] ?? (glyphText[c] = c.ToString())) : c.ToString();
            Vector2 size = Measure(Masthead, g);
            lay.glyphs[i] = g;
            lay.x[i] = x;
            lay.w[i] = size.x;
            x += size.x - pad + track;
            h = Mathf.Max(h, size.y);
        }
        lay.width = n > 0 ? x - track + pad : 0f;
        lay.height = h;
        spaced[key] = lay;
        return lay;
    }

    /// The masthead's size for `text` at the current Title size (the hosts centre the tucked header with it)
    public static float TitleWidth(string text) => MastheadLayout(text).width;
    public static Vector2 TitleSize(string text) { var l = MastheadLayout(text); return new Vector2(l.width, l.height); }

    static void DrawSpaced(Rect r, Spaced lay, Color c)
    {
        GUI.color = c;
        for (int i = 0; i < lay.glyphs.Length; i++)
            UIScale.Label(new Rect(r.x + lay.x[i], r.y, lay.w[i], r.height), lay.glyphs[i], Masthead);
    }

    static void DrawRotSpaced(Rect r, Vector2 pivot, float angle, Spaced lay, Color c)
    {
        Matrix4x4 m = GUI.matrix;
        UIScale.RotateAround(angle, pivot);
        DrawSpaced(r, lay, c);
        GUI.matrix = m;
        GUI.color = Color.white;
    }

    // ---------------------------------------------------------------- primitives
    public static void DrawRotRect(Rect r, float angle, Color c)
    {
        Matrix4x4 m = GUI.matrix;
        UIScale.RotateAround(angle, r.center);
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.matrix = m;
        GUI.color = Color.white;
    }

    public static void DrawRotLabel(Rect r, float angle, string text, GUIStyle style, Color c)
    {
        Matrix4x4 m = GUI.matrix;
        UIScale.RotateAround(angle, r.center);
        GUI.color = c;
        UIScale.Label(r, text, style);
        GUI.matrix = m;
        GUI.color = Color.white;
    }

    static void Fill(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
    }

    /// Fluting over a surface: the plaster's ribs, faint, in ink
    public static void Flute(Rect r, float alpha, float scale = 1f)
    {
        GUI.color = new Color(Ink.r, Ink.g, Ink.b, alpha);
        MenuArt.Tile(r, MenuArt.FluteTex, scale, Vector2.zero);
        GUI.color = Color.white;
    }

    /// Diagonal hatch crawling at `speedPx` per second (0 = still)
    public static void Hatch(Rect r, Color tint, float speedPx)
    {
        GUI.color = tint;
        MenuArt.Tile(r, MenuArt.HatchTex, 1f, new Vector2(Time.unscaledTime * speedPx, 0f));
        GUI.color = Color.white;
    }

    /// A thin double rule under a heading: a 2 px rule, a 3 px gap, a 1 px rule — 6 px tall; `flip` puts the
    /// heavy rule below (the foot of a card)
    public static void DoubleRule(float x, float y, float w, Color c, bool flip = false)
    {
        if (flip)
        {
            Fill(new Rect(x, y, w, 1f), c);
            Fill(new Rect(x, y + 4f, w, 2f), c);
        }
        else
        {
            Fill(new Rect(x, y, w, 2f), c);
            Fill(new Rect(x, y + 5f, w, 1f), c);
        }
        GUI.color = Color.white;
    }

    /// A zigzag rule tiled along `r`: the chevron stands `r.height` tall and twice that wide
    public static void ChevronRule(Rect r, Color c)
    {
        GUI.color = c;
        MenuArt.Tile(r, MenuArt.ChevronTex, r.height / 8f, Vector2.zero);
        GUI.color = Color.white;
    }

    /// A chevron frieze over a rect: the ground, then ink chevrons across it (the Works' slivers and stripes — never gold)
    public static void Frieze(Rect r, Color ground, float alpha, float scale = 0.5f)
    {
        Fill(r, new Color(ground.r, ground.g, ground.b, alpha));
        GUI.color = new Color(Ink.r, Ink.g, Ink.b, 0.85f * alpha);
        MenuArt.Tile(r, MenuArt.FriezeTex, scale, Vector2.zero);
        GUI.color = Color.white;
    }

    /// The strip under a card's picture: a chevron rule in the accent, or the frieze on the Works
    public static void AccentStrip(Rect r, float alpha)
    {
        if (S.chevron) Frieze(r, S.accent, alpha, 0.4f);
        else ChevronRule(r, new Color(S.accent.r, S.accent.g, S.accent.b, alpha));
    }

    /// The plate behind a cutscene's title card: charcoal with faint fluting in a stepped brass frame — the Deco
    /// answer to the hologram strip the cards used to sit on
    public static void TitlePlate(Rect r, float alpha)
    {
        Fill(r, new Color(Panel.r, Panel.g, Panel.b, 0.6f * alpha));
        Flute(r, 0.07f * alpha);
        StepFrame(r, new Color(Brass.r, Brass.g, Brass.b, 0.55f * alpha), 1f, 6f, 2);
    }

    /// A frame with stepped corners: `thick` px rules, each corner cut in `steps` steps of `step` px — the ziggurat
    public static void StepFrame(Rect r, Color c, float thick = 1f, float step = 6f, int steps = 2)
    {
        GUI.color = c;
        float cut = step * steps;
        Texture2D white = Texture2D.whiteTexture;
        GUI.DrawTexture(new Rect(r.x + cut, r.y, r.width - 2f * cut, thick), white);
        GUI.DrawTexture(new Rect(r.x + cut, r.yMax - thick, r.width - 2f * cut, thick), white);
        GUI.DrawTexture(new Rect(r.x, r.y + cut, thick, r.height - 2f * cut), white);
        GUI.DrawTexture(new Rect(r.xMax - thick, r.y + cut, thick, r.height - 2f * cut), white);
        StepCorner(r.x, r.y, 1, 1, step, steps, thick);
        StepCorner(r.xMax, r.y, -1, 1, step, steps, thick);
        StepCorner(r.x, r.yMax, 1, -1, step, steps, thick);
        StepCorner(r.xMax, r.yMax, -1, -1, step, steps, thick);
        GUI.color = Color.white;
    }

    /// One corner's staircase, from the end of one edge to the start of the next; `sx`/`sy` point into the rect
    static void StepCorner(float cx, float cy, int sx, int sy, float step, int steps, float thick)
    {
        float cut = step * steps;
        for (int i = 0; i < steps; i++)
        {
            float vx = cx + sx * (cut - i * step), vy0 = cy + sy * i * step, vy1 = cy + sy * (i + 1) * step;
            Seg(vx, vy0, vx, vy1, sx, sy, thick);
            Seg(cx + sx * (cut - (i + 1) * step), vy1, vx, vy1, sx, sy, thick);
        }
    }

    /// A horizontal or vertical stroke between two points, `thick` px, lying on the inside of the corner
    static void Seg(float x0, float y0, float x1, float y1, int sx, int sy, float thick)
    {
        float xmin = Mathf.Min(x0, x1), ymin = Mathf.Min(y0, y1), w = Mathf.Abs(x1 - x0), h = Mathf.Abs(y1 - y0);
        float rx = sx > 0 ? xmin : xmin - thick, ry = sy > 0 ? ymin : ymin - thick;
        Rect rr = w < 0.5f ? new Rect(rx, ry, thick, h + thick) : new Rect(rx, ry, w + thick, thick);
        GUI.DrawTexture(rr, Texture2D.whiteTexture);
    }

    /// Four plates in the corners of a frame: solid squares `size` across, `inset` px in from each corner
    public static void CornerPlates(Rect r, Color c, float size, float inset)
    {
        GUI.color = c;
        Texture2D white = Texture2D.whiteTexture;
        GUI.DrawTexture(new Rect(r.x + inset, r.y + inset, size, size), white);
        GUI.DrawTexture(new Rect(r.xMax - inset - size, r.y + inset, size, size), white);
        GUI.DrawTexture(new Rect(r.x + inset, r.yMax - inset - size, size, size), white);
        GUI.DrawTexture(new Rect(r.xMax - inset - size, r.yMax - inset - size, size, size), white);
        GUI.color = Color.white;
    }

    /// The company's seal: the notary ring with the sun inside, `size` across, centred on `pivot`, on its shadow
    public static void Seal(Vector2 pivot, float size, Color c, float punch = 1f)
    {
        Matrix4x4 m = GUI.matrix;
        UIScale.ScaleAround(new Vector2(punch, punch), pivot);
        Rect r = new Rect(pivot.x - size * 0.5f, pivot.y - size * 0.5f, size, size);
        float s = size * 0.5f;
        Rect sun = new Rect(pivot.x - s * 0.5f, pivot.y - s * 0.5f, s, s);
        GUI.color = new Color(0f, 0f, 0f, 0.6f * c.a);
        GUI.DrawTexture(new Rect(r.x + 3f, r.y + 3f, size, size), MenuArt.SealTex);
        GUI.DrawTexture(new Rect(sun.x + 3f, sun.y + 3f, s, s), GameFont.Logo);
        GUI.color = c;
        GUI.DrawTexture(r, MenuArt.SealTex);
        GUI.DrawTexture(sun, GameFont.Logo);
        GUI.matrix = m;
        GUI.color = Color.white;
    }

    /// A dashed frame around a rect
    public static void DashFrame(Rect r, Color c, float thick = 2f)
    {
        GUI.color = c;
        var dash = MenuArt.DashTex;
        MenuArt.Tile(new Rect(r.x, r.y, r.width, thick), dash, thick / 4f, Vector2.zero);
        MenuArt.Tile(new Rect(r.x, r.yMax - thick, r.width, thick), dash, thick / 4f, Vector2.zero);
        Matrix4x4 m = GUI.matrix;
        Rect left = new Rect(r.x, r.y, thick, r.height);
        UIScale.RotateAround(90f, left.center);
        MenuArt.Tile(new Rect(left.center.x - r.height * 0.5f, left.center.y - thick * 0.5f, r.height, thick), dash, thick / 4f, Vector2.zero);
        GUI.matrix = m;
        Rect right = new Rect(r.xMax - thick, r.y, thick, r.height);
        UIScale.RotateAround(90f, right.center);
        MenuArt.Tile(new Rect(right.center.x - r.height * 0.5f, right.center.y - thick * 0.5f, r.height, thick), dash, thick / 4f, Vector2.zero);
        GUI.matrix = m;
        GUI.color = Color.white;
    }

    /// The company's sun, bursting behind a stamp: from 1.6× to 2.4× of `size` and out as `k` runs 0..1
    public static void StampBurst(Vector2 pivot, float size, float k, Color c)
    {
        if (k < 0f || k >= 1f) return;
        float s = size * Mathf.Lerp(1.6f, 2.4f, k);
        GUI.color = new Color(c.r, c.g, c.b, c.a * (1f - k));
        GUI.DrawTexture(new Rect(pivot.x - s * 0.5f, pivot.y - s * 0.5f, s, s), MenuArt.BurstTex);
        GUI.color = Color.white;
    }

    /// The sunburst turning slowly behind something: `size` across, `degPerSec` of spin
    public static void SlowBurst(Vector2 pivot, float size, float degPerSec, Color c)
    {
        Matrix4x4 m = GUI.matrix;
        UIScale.RotateAround(Time.unscaledTime * degPerSec, pivot);
        GUI.color = c;
        GUI.DrawTexture(new Rect(pivot.x - size * 0.5f, pivot.y - size * 0.5f, size, size), MenuArt.BurstTex);
        GUI.matrix = m;
        GUI.color = Color.white;
    }

    // ---------------------------------------------------------------- the rot scheduler
    /// One idle row, every ten-odd seconds, does what the city's holograms do. Advanced ONLY in a host's Update;
    /// read in OnGUI. Three flashes per event, then a hold; events never closer than a quarter of the mean.
    public struct RotClock
    {
        public float nextAt, startAt, until;
        public int target, seq;
        public bool armed;
        public bool Ticking(float now) => target >= 0 && now >= startAt && now < until;
    }

    public static readonly int[] RotPattern = { 0, 1, 0, 0, 1, 0 };

    public static void AdvanceRot(ref RotClock c, float now, float mean, int rows, int skipA, int skipB = -1, float fixedDuration = -1f)
    {
        if (!c.armed)
        {
            c.armed = true;
            c.target = -1;
            c.nextAt = now + Interval(mean, 0);
            return;
        }
        if (now < c.until) return;      // an event is running
        c.target = -1;
        if (now < c.nextAt || rows <= 0) return;
        c.seq++;
        int target = (int)(MenuArt.Hash01(c.seq, 7, 1) * rows);
        for (int tries = 0; (target == skipA || target == skipB) && tries < rows; tries++) target = (target + 1) % rows;
        if (target == skipA || target == skipB) { c.nextAt = now + Interval(mean, c.seq); return; } // nothing to glitch: dropped
        float duration = fixedDuration > 0f ? fixedDuration : 0.08f + 0.27f * MenuArt.Hash01(c.seq, 11, 1);
        c.target = target;
        c.startAt = now;
        c.until = now + duration;
        c.nextAt = c.until + Interval(mean, c.seq);
    }

    static float Interval(float mean, int seq)
        => Mathf.Clamp(-mean * Mathf.Log(Mathf.Max(0.001f, MenuArt.Hash01(seq, 3, 1))), 0.25f * mean, 3f * mean);

    /// Which tick of the pattern a row is on right now, or -1 when it is not glitching
    public static int RotTick(in RotClock c, int row, float now)
        => c.target == row && now >= c.startAt && now < c.until ? (int)((now - c.startAt) / 0.03f) : -1;

    public static int RotState(int tick) => RotPattern[Mathf.Min(Mathf.Max(tick, 0), RotPattern.Length - 1)];

    /// The word an event says: one per event, so it can be read
    public static string RotWord(in RotClock c) => LevelKit.Corruptions[Mathf.Abs(c.seq) % LevelKit.Corruptions.Length];

    // ---------------------------------------------------------------- the band
    /// The big diagonal band behind a menu, in one matrix: shadow, the lit sheet with faint fluting, the company's
    /// sun behind its chip, the searchlight sweeping across, then the cornice — a brass rule, a chevron rule, the
    /// bronze trim carrying the ticker, and the stepped edge under it — with the oxblood stripe above and the chip.
    /// `age` (seconds since the page opened) drives the sweep; negative = none.
    public static void StripeBand(float screenW, float bandY, float angle = BandAngle, float scale = 1f, float age = -1f)
    {
        bool tucked = scale < 1f;
        float cx = screenW * 0.5f, w2 = screenW * 2f, x0 = cx - w2 * 0.5f, H = 180f * scale, top = bandY - 90f * scale;
        float t = Time.unscaledTime;
        Color accent = S.accent;
        Matrix4x4 saved = GUI.matrix;
        UIScale.RotateAround(angle, new Vector2(cx, bandY));
        Fill(new Rect(x0 + 10f, top + 12f, w2, H), new Color(0f, 0f, 0f, 0.55f));
        Fill(new Rect(x0, top, w2, H), S.sheet);
        Flute(new Rect(x0, top, w2, 152f * scale), 0.05f, scale);
        float chipX = cx + Mathf.Min(300f, screenW * 0.19f), chipY = top + 118f;
        if (!tucked)
        {
            // The company's sun, big and faint, behind its chip: the mark, printed into the sheet
            float s = H * 1.15f;
            GUI.color = new Color(S.mark.r, S.mark.g, S.mark.b, 0.15f + 0.03f * Mathf.Sin(t * 0.7f));
            GUI.DrawTexture(new Rect(chipX + 150f - s * 0.5f, top + H * 0.5f - s * 0.5f, s, s), GameFont.Logo);
        }
        if (age >= 0f)
        {
            // The searchlight: a sweep of light across the band every few seconds
            float k = Mathf.Repeat(age, 4.5f) / 0.6f;
            if (k < 1f)
            {
                float sx = Mathf.Lerp(cx - 0.9f * screenW, cx + 0.9f * screenW, k) - 130f;
                GUI.color = new Color(1f, 1f, 1f, 0.6f * Mathf.Sin(Mathf.PI * k));
                GUI.DrawTexture(new Rect(sx, top, 260f, H), MenuArt.StreakTex, ScaleMode.StretchToFill);
            }
        }
        // The cornice
        Fill(new Rect(x0, top + 152f * scale, w2, 2f), accent);
        ChevronRule(new Rect(x0, top + 155f * scale, w2, 7f * scale), Alpha(accent, 0.9f));
        Fill(new Rect(x0, top + 164f * scale, w2, 16f * scale), S.trim);
        Fill(new Rect(x0, top + 180f * scale, w2, 2f), accent);
        Fill(new Rect(x0, top + 182f * scale, w2, 6f * scale), Alpha(S.trim, 0.85f));
        Fill(new Rect(x0, top + 188f * scale, w2, 1f), Alpha(accent, 0.7f));
        // The stripe above, a brass hairline either side of it
        Fill(new Rect(x0, top - 29f * scale, w2, 1f), Alpha(accent, 0.7f));
        if (S.chevron) Frieze(new Rect(x0, top - 26f * scale, w2, 10f * scale), S.stripe, 0.95f, scale * 0.6f);
        else Fill(new Rect(x0, top - 26f * scale, w2, 10f * scale), Alpha(S.stripe, 0.95f));
        Fill(new Rect(x0, top - 16f * scale, w2, 2f), Alpha(accent, 0.9f));
        if (!tucked)
        {
            // The trim is the company's ticker: what the house says about itself, running along the band. Its width
            // is taken at the rasterised size whenever the scale changes (at 720p the type sets 4 % wider than the
            // base measure, and a box cut to the base width would wrap the run's last words under the cornice).
            if (S.tickerFactor != UIScale.Factor)
            {
                S.tickerW = UIScale.Measure(Doc, S.ticker).x;
                S.tickerFactor = UIScale.Factor;
            }
            float copy = S.tickerW * 0.25f;
            float off = copy > 1f ? Mathf.Repeat(t * 36f, copy) : 0f;
            GUI.color = Alpha(S.tickerText, 0.85f);
            UIScale.Label(new Rect(x0 - off, top + 164f * scale, S.tickerW + 10f, 16f * scale), S.ticker, Doc);
            // The chip: a double rule, the programme line under it. The box runs on along the band (left-aligned,
            // the band is two screens wide) so no skin's line can ever wrap it.
            DoubleRule(chipX, chipY + 2f, 120f, Alpha(Ink, 0.8f));
            GUI.color = Alpha(Ink, 0.9f);
            UIScale.Label(new Rect(chipX, chipY + 16f, 600f, 16f), S.chip, Doc);
        }
        GUI.matrix = saved;
        GUI.color = Color.white;
    }

    /// The band's drop-frame: while its clock ticks, a strip of the band tears out red and shoves an ink shadow under
    /// it — drawn AFTER the title so the glyphs smear beneath. Advance the clock in Update with rows = 1 and a fixed
    /// 0.1 s event; the start also kicks the title's plates. The rot's own leak: untouched by the dress.
    public static void BandDropFrame(in RotClock clock, float screenW, float bandY, float angle = BandAngle, float scale = 1f)
    {
        float now = Time.unscaledTime;
        if (!clock.Ticking(now)) return;
        if (now - clock.startAt < 0.02f) KickAt = clock.startAt;
        float cx = screenW * 0.5f, w2 = screenW * 2f, x0 = cx - w2 * 0.5f, top = bandY - 90f * scale;
        float y = top + 20f + MenuArt.Hash01(clock.seq, 5, 1) * 120f * scale;
        Matrix4x4 saved = GUI.matrix;
        UIScale.RotateAround(angle, new Vector2(cx, bandY));
        Fill(new Rect(x0, y, w2, 34f * scale), new Color(Rot.r, Rot.g, Rot.b, 0.7f));
        Fill(new Rect(x0, y + 34f * scale, w2, 8f * scale), new Color(Ink.r, Ink.g, Ink.b, 0.7f));
        GUI.matrix = saved;
        GUI.color = Color.white;
    }

    // ---------------------------------------------------------------- measured type
    // A label that wraps is a fault. IMGUI's default label wraps whatever is wider than its box, and the width a
    // label takes depends on the rasterised size (hinting), so every box that could be tight is tested against
    // UIScale.Measure at the current scale. Widths are kept per (style, text, rasterised size): a miss costs one
    // CalcSize when a page opens or the window changes, a hit costs a lookup — never a CalcSize per frame.
    static readonly Dictionary<(GUIStyle style, string text, int size), float> fitWidths = new Dictionary<(GUIStyle, string, int), float>();
    /// The box the document line under a title gets: one line along the title's frame
    public const float DocLineWidth = 700f;

    static int RasterSize(GUIStyle style) => Mathf.RoundToInt(style.fontSize * UIScale.Factor);

    /// The width `text` takes in `style` at the current UIScale, in canvas px (measured once per text and size)
    public static float FitWidth(GUIStyle style, string text)
    {
        var key = (style, text, RasterSize(style));
        if (fitWidths.TryGetValue(key, out float w)) return w;
        w = UIScale.Measure(style, text).x;
        fitWidths[key] = w;
        return w;
    }

    /// One line, whatever the box: drawn at the style's size when it fits `r`, stepped down (never under 10 px)
    /// when it does not — the anchored edge stays put and nothing wraps. Returns the width drawn.
    public static float FitLabel(Rect r, string text, GUIStyle style)
    {
        if (string.IsNullOrEmpty(text)) return 0f;
        float w = FitWidth(style, text);
        float room = r.width - 2f;
        if (w <= room || room <= 0f) { UIScale.Label(r, text, style); return w; }
        int baseSize = style.fontSize;
        style.fontSize = Mathf.Max(10, Mathf.FloorToInt(baseSize * room / w * 0.98f));
        float drawn = FitWidth(style, text);
        UIScale.Label(r, text, style);
        style.fontSize = baseSize;
        return drawn;
    }

    // Letterspaced small caps (the register): IMGUI has no tracking, so a line is set one glyph at a time from a
    // layout measured at the rasterised size, once per text and size, and kept — a frame allocates nothing
    sealed class Tracked
    {
        public string[] glyphs;
        public float[] x, w;
        public float width;
    }
    static readonly Dictionary<(GUIStyle style, string text, int size), Tracked> tracked = new Dictionary<(GUIStyle, string, int), Tracked>();

    static Tracked TrackedLayout(GUIStyle style, string text, float em)
    {
        var key = (style, text, RasterSize(style));
        if (tracked.TryGetValue(key, out var lay)) return lay;
        int n = text.Length;
        lay = new Tracked { glyphs = new string[n], x = new float[n], w = new float[n] };
        // Each glyph's measure carries the style's padding (in canvas units, padding / Factor): taken out of the advance
        float track = style.fontSize * em, pad = style.padding.horizontal / Mathf.Max(0.5f, UIScale.Factor), x = 0f;
        for (int i = 0; i < n; i++)
        {
            char c = text[i];
            string g = c < 128 ? (glyphText[c] ?? (glyphText[c] = c.ToString())) : c.ToString();
            float gw = UIScale.Measure(style, g).x;
            lay.glyphs[i] = g;
            lay.x[i] = x;
            lay.w[i] = gw;
            x += gw - pad + track;
        }
        lay.width = n > 0 ? x - track + pad : 0f;
        tracked[key] = lay;
        return lay;
    }

    /// The width a letterspaced line takes at the current scale, in canvas px (`em` = tracking in ems)
    public static float TrackedWidth(GUIStyle style, string text, float em) => TrackedLayout(style, text, em).width;

    /// A letterspaced line from `r`'s left edge, seated as the style aligns vertically
    public static void TrackedLabel(Rect r, string text, GUIStyle style, float em, Color c)
    {
        if (string.IsNullOrEmpty(text)) return;
        Tracked lay = TrackedLayout(style, text, em);
        GUI.color = c;
        for (int i = 0; i < lay.glyphs.Length; i++)
            UIScale.Label(new Rect(r.x + lay.x[i], r.y, lay.w[i] + 2f, r.height), lay.glyphs[i], style);
        GUI.color = Color.white;
    }

    // ---------------------------------------------------------------- the register slip
    /// Tracking on the register's small caps, in ems
    public const float RegisterTracking = 0.1f;
    const float SlipPad = 18f, SlipSealSize = 44f, SlipSealInset = 38f;
    /// The width a register line may take on a slip `cardW` wide: inside the pad, clear of the seal
    public static float SlipRoom(float cardW) => cardW - SlipPad - SlipSealInset - SlipSealSize * 0.5f - 10f;
    /// Whether `line` sets on ONE line of a slip `cardW` wide at the current scale — the host decides the split
    /// from this when its page opens or the window changes, never per frame
    public static bool SlipFits(string line, float cardW) => TrackedWidth(Register, line, RegisterTracking) <= SlipRoom(cardW);
    /// The slip's height with one or two register lines
    public static float SlipHeight(bool twoLines) => twoLines ? 108f : 92f;

    /// THE REGISTER SLIP: a cream card with ink type between two brass double rules and the House's wax seal at its
    /// right end — the guest's line in the display face, the record under it in letterspaced small caps, on one
    /// line (`line2`) or two aligned lines (`line2` over `line3`) as the host measured. Tilted with the rows about
    /// r.center; `alpha` is the throw-in.
    public static void RegisterSlip(Rect r, float alpha, string guestLine, string line2, string line3 = null)
    {
        float a = alpha;
        DrawRotRect(new Rect(r.x + 6f, r.y + 6f, r.width, r.height), RowTilt, new Color(0f, 0f, 0f, 0.5f * a));
        DrawRotRect(r, RowTilt, Alpha(Paper, a));
        Matrix4x4 m = GUI.matrix;
        UIScale.RotateAround(RowTilt, r.center);
        Color rule = Alpha(Brass, 0.9f * a);
        DoubleRule(r.x + 12f, r.y + 10f, r.width - 24f, rule);
        DoubleRule(r.x + 12f, r.yMax - 16f, r.width - 24f, rule, true);
        float room = SlipRoom(r.width);
        GUI.color = Alpha(Ink, a);
        UIScale.Label(new Rect(r.x + SlipPad, r.y + 18f, room, 34f), guestLine, Slip);
        Color small = Alpha(Burnt, a);
        TrackedLabel(new Rect(r.x + SlipPad, r.y + 54f, room, 16f), line2, Register, RegisterTracking, small);
        if (!string.IsNullOrEmpty(line3))
            TrackedLabel(new Rect(r.x + SlipPad, r.y + 70f, room, 16f), line3, Register, RegisterTracking, small);
        // The seal: red wax, the sun pressed in — the one red on the page
        WaxSeal(new Vector2(r.xMax - SlipSealInset, r.y + r.height * 0.5f), SlipSealSize, null, Index, a, 1f, -8f);
        GUI.matrix = m;
        GUI.color = Color.white;
    }

    // ---------------------------------------------------------------- the register card
    const float CardTop = 18f, CardHead = 34f, CardLineGap = 22f, CardNoLine = 8f, CardRule = 12f, CardFoot = 24f;
    /// A line of a register card: the name in the display face, its note in the register's small caps after it
    public const float RegisterCardLine = 36f;
    /// The card's height for `count` lines under the headline (and the record line, when there is one)
    public static float RegisterCardHeight(int count, bool hasLine)
        => CardTop + CardHead + (hasLine ? CardLineGap : CardNoLine) + CardRule + count * RegisterCardLine + CardFoot;
    static float CardLinesY(Rect r, bool hasLine) => r.y + CardTop + CardHead + (hasLine ? CardLineGap : CardNoLine) + CardRule;

    /// Which of the card's lines the mouse is over (-1 none), tested unrotated as the rows are
    public static int RegisterCardHit(Rect r, bool hasLine, int count, Vector2 mouse)
    {
        float y0 = CardLinesY(r, hasLine);
        if (count <= 0 || !new Rect(r.x + 12f, y0, r.width - 24f, count * RegisterCardLine).Contains(mouse)) return -1;
        return Mathf.Clamp((int)((mouse.y - y0) / RegisterCardLine), 0, count - 1);
    }

    /// THE REGISTER CARD: the slip's cousin, for a question the desk asks — cream, ink, the brass double rules, the
    /// wax seal — with the headline in the display face, the record line under it in letterspaced small caps, a
    /// hairline, then the lines to choose from: each a name in the display face with its note after it. The picked
    /// line is ink on a faint wash behind a brass sliver between two foil hairlines; the rest burnt. Tilted with the
    /// rows; `alpha` is the throw-in. Every string is the host's, built when the card opened; the picking is the
    /// host's too (its keys, and RegisterCardHit for the mouse) — nothing here reads the event.
    public static void RegisterCard(Rect r, float alpha, string headline, string line, string[] names, string[] notes, int count, int selected)
    {
        float a = alpha;
        DrawRotRect(new Rect(r.x + 8f, r.y + 8f, r.width, r.height), RowTilt, new Color(0f, 0f, 0f, 0.55f * a));
        DrawRotRect(r, RowTilt, Alpha(Paper, a));
        Matrix4x4 m = GUI.matrix;
        UIScale.RotateAround(RowTilt, r.center);
        Color rule = Alpha(Brass, 0.9f * a);
        DoubleRule(r.x + 12f, r.y + 10f, r.width - 24f, rule);
        DoubleRule(r.x + 12f, r.yMax - 16f, r.width - 24f, rule, true);
        float room = SlipRoom(r.width);
        float x = r.x + SlipPad;
        float y = r.y + CardTop;
        GUI.color = Alpha(Ink, a);
        FitLabel(new Rect(x, y, room, CardHead), headline, Slip);
        y += CardHead;
        bool hasLine = !string.IsNullOrEmpty(line);
        if (hasLine)
        {
            TrackedLabel(new Rect(x, y + 2f, room, 16f), line, Register, RegisterTracking, Alpha(Burnt, a));
            y += CardLineGap;
        }
        else y += CardNoLine;
        Fill(new Rect(r.x + 12f, y + 5f, r.width - 24f, 1f), Alpha(Brass, 0.55f * a));
        y += CardRule;
        float lineX = r.x + 12f, lineW = r.width - 24f;
        for (int i = 0; i < count; i++)
        {
            Rect lr = new Rect(lineX, y + i * RegisterCardLine, lineW, RegisterCardLine);
            bool sel = i == selected;
            if (sel)
            {
                Fill(new Rect(lr.x, lr.y + 3f, lr.width, lr.height - 6f), Alpha(Ink, 0.07f * a));
                Fill(new Rect(lr.x, lr.y + 3f, 5f, lr.height - 6f), Alpha(Brass, a));
                Fill(new Rect(lr.x, lr.y + 3f, lr.width, 1f), Alpha(Foil, 0.9f * a));
                Fill(new Rect(lr.x, lr.yMax - 4f, lr.width, 1f), Alpha(Foil, 0.9f * a));
            }
            GUI.color = sel ? Alpha(Ink, a) : Alpha(Burnt, 0.85f * a);
            float nameW = FitLabel(new Rect(lr.x + 18f, lr.y, lr.width * 0.5f, lr.height), names[i], Item);
            string note = notes != null && i < notes.Length ? notes[i] : null;
            if (!string.IsNullOrEmpty(note))
            {
                // The note after the name, only when it sets inside the line: never clipped, never wrapped
                float nx = lr.x + 18f + nameW + 16f;
                float noteRoom = lr.xMax - 8f - nx;
                if (TrackedWidth(Register, note, RegisterTracking) <= noteRoom)
                    TrackedLabel(new Rect(nx, lr.y, noteRoom, lr.height), note, Register, RegisterTracking, Alpha(Burnt, (sel ? 1f : 0.6f) * a));
            }
        }
        WaxSeal(new Vector2(r.xMax - SlipSealInset, r.y + CardTop + CardHead * 0.5f), SlipSealSize, null, Index, a, 1f, -8f);
        GUI.matrix = m;
        GUI.color = Color.white;
    }

    // ---------------------------------------------------------------- the title
    /// The masthead: letterspaced caps in the company's face, seated on the band's centreline over a sunburst,
    /// printed from two plates (oxblood and bronze) that breathe and kick under the ink, a double rule under it
    /// and the document line under that. Returns the LOWEST screen-y the tilted glyphs reach, so content can
    /// stack below. `bandScale` is the band's (the tucked header has no sunburst or rule).
    public static float TitleBlock(float x, float screenW, float bandY, string text, float angle = BandAngle, string docLine = null, float bandScale = 1f)
    {
        Spaced lay = MastheadLayout(text);
        Vector2 size = new Vector2(lay.width, lay.height);
        float centerX = x + size.x * 0.5f;
        float bandLineY = bandY + (centerX - screenW * 0.5f) * Mathf.Tan(angle * Mathf.Deg2Rad);
        Rect r = new Rect(x, bandLineY - size.y * 0.5f, size.x, size.y);
        float t = Time.unscaledTime;
        bool tucked = Title.fontSize <= 60;
        if (!tucked) Sunburst(screenW, bandY, angle, bandScale, centerX, size);
        float rest = tucked ? 1f : 2f + Mathf.Sin(t * 2.9f);
        float kick = (tucked ? 4f : 8f) * Mathf.Exp(-Mathf.Max(0f, t - KickAt) / 0.06f);
        float o = rest + kick;
        Vector2 pivot = r.center;
        DrawRotSpaced(new Rect(r.x + 5f, r.y + 5f, r.width, r.height), pivot, angle, lay, new Color(0f, 0f, 0f, 0.7f));
        DrawRotSpaced(new Rect(r.x - o, r.y, r.width, r.height), pivot, angle, lay, Alpha(Oxblood, 0.85f));
        DrawRotSpaced(new Rect(r.x + o, r.y + 1f, r.width, r.height), pivot, angle, lay, Alpha(DeepTeal, 0.85f));
        DrawRotSpaced(r, pivot, angle, lay, Ink);
        float swing = Mathf.Sin(Mathf.Abs(angle) * Mathf.Deg2Rad) * size.x * 0.5f;
        float result = r.yMax + swing;
        // The double rule under the heading and the document line under that, in the title's frame
        Matrix4x4 m = GUI.matrix;
        UIScale.RotateAround(angle, pivot);
        float ruleY = r.center.y + Masthead.fontSize * 0.42f;
        if (!tucked) DoubleRule(r.x + 2f, ruleY, r.width - 4f, Alpha(S.sheetDim, 0.9f));
        if (!string.IsNullOrEmpty(docLine))
        {
            GUI.color = Alpha(Ink, 0.9f);
            float docY = Mathf.Min(ruleY + 9f, r.center.y + 41f); // never onto the cornice, however tall the title
            FitLabel(new Rect(r.x + 6f, docY, DocLineWidth, 18f), docLine, Doc); // one line, measured: a long bill never wraps
            result += 12f;
        }
        GUI.matrix = m;
        GUI.color = Color.white;
        return result;
    }

    /// The sunburst behind the masthead: rays rising from the title's seat, drawn in the band's own frame and
    /// clipped to its clean sheet (above the cornice)
    static void Sunburst(float screenW, float bandY, float angle, float scale, float centerX, Vector2 size)
    {
        if (S.ray.a <= 0f) return;
        float cx = screenW * 0.5f, w2 = screenW * 2f, x0 = cx - w2 * 0.5f, top = bandY - 90f * scale;
        float u = cx + (centerX - cx) / Mathf.Cos(angle * Mathf.Deg2Rad) - x0; // the title's centre along the band, in the group's space
        float fw = Mathf.Max(size.x * 1.8f, 800f), fh = fw * 0.5f;
        Matrix4x4 saved = GUI.matrix;
        UIScale.RotateAround(angle, new Vector2(cx, bandY));
        GUI.BeginGroup(new Rect(x0, top, w2, 152f * scale));
        GUI.color = S.ray;
        GUI.DrawTexture(new Rect(u - fw * 0.5f, bandY - top + size.y * 0.22f - fh, fw, fh), MenuArt.FanTex, ScaleMode.StretchToFill);
        GUI.EndGroup();
        GUI.matrix = saved;
        GUI.color = Color.white;
    }

    // ---------------------------------------------------------------- the rows
    /// One tilted menu entry. Returns true when clicked. `slide` is the 0..1 staggered intro; selection inverts to
    /// the lit sheet and pops. `index` prints a two-digit index; `rotTick` >= 0 makes the row glitch (the word is
    /// `rotLabel`); `disabled` strikes it from the record; `subDim` prints the sub dim instead of in the accent.
    public static bool SlashItem(Rect r, string label, string sub, bool selected, float slide, Rect? passThrough = null,
        int index = -1, int rotTick = -1, string rotLabel = null, bool disabled = false, bool subDim = false)
    {
        float t = Time.unscaledTime;
        float ease = Ease(slide);
        float a = Mathf.Clamp01(ease);
        float restX = r.x;
        r.x -= (1f - ease) * 340f;
        if (selected) r = new Rect(r.x - 14f, r.y - 3f, r.width + 34f, r.height + 6f);
        float since = t - SelectedAt;
        if (selected && since < 0.1f)
        {
            float j = Mathf.Exp(-since / 0.04f);
            r.x += Mathf.Sin(t * 95f) * 4f * j;
            r.y += Mathf.Sin(t * 95f) * 2f * j;
        }
        const float tilt = RowTilt;
        float labelX = index >= 0 ? 46f : 22f;

        if (disabled)
        {
            DrawRotRect(r, tilt, Alpha(S.sheet, a * 0.55f));
            Matrix4x4 m = GUI.matrix;
            UIScale.RotateAround(tilt, r.center);
            Hatch(r, Alpha(S.sheetDim, 0.25f * a), 0f);
            GUI.color = Alpha(S.sheetDim, 0.8f * a);
            UIScale.Label(new Rect(r.x + labelX, r.y, r.width - labelX - 8f, r.height), label, Item);
            Fill(new Rect(r.x + labelX, r.y + r.height * 0.5f - 2f, r.width * 0.55f - labelX, 4f), Alpha(Rot, 0.8f * a));
            GUI.matrix = m;
            GUI.color = Color.white;
            return false;
        }

        // The rot's own leak: the row ticks red and ink and says its word — untouched by the dress
        bool rot = rotTick >= 0 && !selected;
        if (rot)
        {
            int state = RotState(rotTick);
            Rect rr = new Rect(r.x + Mathf.Sin(t * 131f) * 4f, r.y + Mathf.Sin(t * 97f) * 2f, r.width, r.height);
            Color panel = state == 0 ? Rot : Ink; panel.a = a;
            Color text = state == 0 ? Ink : Rot; text.a = a;
            DrawRotRect(rr, tilt, panel);
            DrawRotRect(new Rect(rr.x, rr.y, 7f, rr.height), tilt, Alpha(Rot, a));
            DrawRotLabel(new Rect(rr.x + labelX, rr.y, rr.width - labelX - 8f, rr.height), tilt, rotLabel ?? label, Item, text);
            return Click(r, passThrough);
        }

        // THE PROGRAMME LINE: a line of the programme on the property's lit sheet (cream on the House), ink type, an
        // oxblood sliver, a bronze rule along its foot with a short chevron; picked, the line inverts to ink with brass
        // letters in a stepped brass frame. The sheet lands lit brass for a tick, then prints.
        bool landing = ease >= 0.9f && ease < 1f;
        float shadow = selected ? 9f : 6f;
        DrawRotRect(new Rect(r.x + shadow, r.y + shadow, r.width, r.height), tilt, new Color(0f, 0f, 0f, 0.55f * a));
        Color panelColor = landing ? Foil : selected ? Ink : S.sheet;
        panelColor.a = selected || landing ? a : 0.94f * a;
        DrawRotRect(r, tilt, panelColor);

        Matrix4x4 saved = GUI.matrix;
        UIScale.RotateAround(tilt, r.center);
        if (!selected) Flute(r, 0.05f * a);
        // The sliver is seated at the rest position: the sheet slides in under it to meet it
        if (selected) Fill(new Rect(r.x, r.y, 7f, r.height), Alpha(S.accent, a));
        else
        {
            Matrix4x4 m2 = GUI.matrix;
            GUI.matrix = saved;
            UIScale.RotateAround(tilt, new Vector2(restX + r.width * 0.5f, r.center.y));
            if (S.chevron) Frieze(new Rect(restX, r.y, 7f, r.height), S.stripe, a, 0.45f);
            else Fill(new Rect(restX, r.y, 7f, r.height), Alpha(S.stripe, a));
            GUI.matrix = m2;
        }
        if (selected)
        {
            // A foil hairline along the top, the stepped brass frame inside the line, and a streak of light running it
            Fill(new Rect(r.x, r.y, r.width, 2f), Alpha(Foil, (0.6f + 0.25f * Mathf.Sin(t * 1.7f)) * a));
            StepFrame(new Rect(r.x + 11f, r.y + 4f, r.width - 15f, r.height - 8f), Alpha(S.accent, 0.45f * a), 1f, 4f, 2);
            GUI.BeginGroup(r);
            float sx = r.width - Mathf.Repeat(t * 260f, r.width + 220f);
            GUI.color = Alpha(Foil, 0.22f * a);
            GUI.DrawTexture(new Rect(sx, 0f, 220f, r.height), MenuArt.StreakTex, ScaleMode.StretchToFill);
            GUI.EndGroup();
        }
        else
        {
            Fill(new Rect(r.x + 7f, r.yMax - 2f, r.width - 7f, 1f), Alpha(S.trim, 0.7f * a));
            ChevronRule(new Rect(r.x + 14f, r.yMax - 7f, 40f, 4f), Alpha(S.trim, 0.6f * a));
        }
        if (index >= 0)
        {
            GUI.color = selected ? Alpha(S.accent, 0.85f * a) : Alpha(S.sheetDim, 0.85f * a);
            UIScale.Label(new Rect(r.x + 14f, r.y + 2f, 30f, r.height), IndexText[Mathf.Clamp(index, 0, 99)], Index);
        }
        Rect lr = new Rect(r.x + labelX, r.y, r.width - labelX - 8f, r.height);
        if (selected)
        {
            // Two plates under the brass: oxblood left, bronze right
            GUI.color = Alpha(Oxblood, 0.7f * a);
            UIScale.Label(new Rect(lr.x - 2f, lr.y, lr.width, lr.height), label, ItemSelected);
            GUI.color = Alpha(DeepTeal, 0.7f * a);
            UIScale.Label(new Rect(lr.x + 2f, lr.y + 1f, lr.width, lr.height), label, ItemSelected);
            GUI.color = Alpha(Gold, a);
            UIScale.Label(lr, label, ItemSelected);
        }
        else
        {
            GUI.color = Alpha(Ink, a);
            UIScale.Label(lr, label, Item);
        }
        if (!string.IsNullOrEmpty(sub))
        {
            // The record, right-aligned in what the letters leave: measured, stepped down before it could touch them
            GUI.color = selected ? Alpha(S.accent, a) : subDim ? Alpha(S.sheetDim, 0.55f * a) : Alpha(S.sheetDim, a);
            float subX = r.x + labelX + FitWidth(selected ? ItemSelected : Item, label) + 16f;
            FitLabel(new Rect(subX, r.y, r.xMax - 18f - subX, r.height), sub, Sub);
        }
        GUI.matrix = saved;
        GUI.color = Color.white;

        if (selected) SunCursor(r, a, since, Gold);
        return Click(r, passThrough);
    }

    /// The cursor beside a picked line: the company's sun, spun in from -110° to the band's angle
    static void SunCursor(Rect r, float a, float since, Color c)
    {
        Rect mark = new Rect(r.x - 30f, r.y + r.height * 0.5f - 9f, 18f, 18f);
        float spin = -16f - 94f * Mathf.Pow(1f - Mathf.Clamp01(since / 0.12f), 2f);
        Matrix4x4 m = GUI.matrix;
        UIScale.RotateAround(spin, mark.center);
        GUI.color = Alpha(c, a);
        GUI.DrawTexture(mark, GameFont.Logo);
        GUI.matrix = m;
        GUI.color = Color.white;
    }

    // ---------------------------------------------------------------- the directory
    /// THE DIRECTORY: one line of the lobby's directory board — brass letters on black felt, the index number in
    /// brass at the left, a brass hairline above and below, a short chevron rule at the foot. Picked, the letters
    /// light (gold on a foil hairline) in a stepped brass frame and the sun cursor spins in. The same bones as the
    /// programme line: the throw-in, the expansion, the jitter, the rot's leak, the click.
    public static bool DirectoryItem(Rect r, string label, string sub, bool selected, float slide, Rect? passThrough = null,
        int index = -1, int rotTick = -1, string rotLabel = null, bool disabled = false, bool subDim = false)
    {
        float t = Time.unscaledTime;
        float ease = Ease(slide);
        float a = Mathf.Clamp01(ease);
        float restX = r.x;
        r.x -= (1f - ease) * 340f;
        if (selected) r = new Rect(r.x - 14f, r.y - 3f, r.width + 34f, r.height + 6f);
        float since = t - SelectedAt;
        if (selected && since < 0.1f)
        {
            float j = Mathf.Exp(-since / 0.04f);
            r.x += Mathf.Sin(t * 95f) * 4f * j;
            r.y += Mathf.Sin(t * 95f) * 2f * j;
        }
        const float tilt = RowTilt;
        float labelX = index >= 0 ? 50f : 22f;

        if (disabled)
        {
            DrawRotRect(r, tilt, Alpha(Felt, a * 0.7f));
            Matrix4x4 m = GUI.matrix;
            UIScale.RotateAround(tilt, r.center);
            GUI.color = Alpha(Sick, a);
            UIScale.Label(new Rect(r.x + labelX, r.y, r.width - labelX - 8f, r.height), label, Item);
            Fill(new Rect(r.x + labelX, r.y + r.height * 0.5f - 2f, r.width * 0.55f - labelX, 4f), Alpha(Rot, 0.8f * a));
            GUI.matrix = m;
            GUI.color = Color.white;
            return false;
        }

        bool rot = rotTick >= 0 && !selected;
        if (rot)
        {
            int state = RotState(rotTick);
            Rect rr = new Rect(r.x + Mathf.Sin(t * 131f) * 4f, r.y + Mathf.Sin(t * 97f) * 2f, r.width, r.height);
            Color panel = state == 0 ? Rot : Ink; panel.a = a;
            Color text = state == 0 ? Ink : Rot; text.a = a;
            DrawRotRect(rr, tilt, panel);
            DrawRotRect(new Rect(rr.x, rr.y, 7f, rr.height), tilt, Alpha(Rot, a));
            DrawRotLabel(new Rect(rr.x + labelX, rr.y, rr.width - labelX - 8f, rr.height), tilt, rotLabel ?? label, Item, text);
            return Click(r, passThrough);
        }

        bool landing = ease >= 0.9f && ease < 1f;
        float shadow = selected ? 9f : 6f;
        DrawRotRect(new Rect(r.x + shadow, r.y + shadow, r.width, r.height), tilt, new Color(0f, 0f, 0f, 0.55f * a));
        Color panelColor = landing ? Foil : Felt;
        panelColor.a = a;
        DrawRotRect(r, tilt, panelColor);

        Matrix4x4 saved = GUI.matrix;
        UIScale.RotateAround(tilt, r.center);
        if (!landing)
        {
            // The felt's nap, and the brass hairlines that rule the board
            GUI.color = new Color(1f, 1f, 1f, 0.07f * a);
            MenuArt.Tile(r, MenuArt.FeltTex, 1f, Vector2.zero);
            Fill(new Rect(r.x, r.y, r.width, 1f), Alpha(S.accent, (selected ? 0.9f : 0.4f) * a));
            Fill(new Rect(r.x, r.yMax - 1f, r.width, 1f), Alpha(S.accent, (selected ? 0.9f : 0.4f) * a));
        }
        // The brass sliver at the rest position: the felt slides in under it
        {
            Matrix4x4 m2 = GUI.matrix;
            GUI.matrix = saved;
            UIScale.RotateAround(tilt, new Vector2(restX + r.width * 0.5f, r.center.y));
            Fill(new Rect(selected ? r.x : restX, r.y, 5f, r.height), Alpha(S.accent, a));
            GUI.matrix = m2;
        }
        if (selected)
        {
            Fill(new Rect(r.x, r.y, r.width, 2f), Alpha(Foil, (0.6f + 0.25f * Mathf.Sin(t * 1.7f)) * a));
            StepFrame(new Rect(r.x + 9f, r.y + 4f, r.width - 13f, r.height - 8f), Alpha(S.accent, 0.55f * a), 1f, 4f, 2);
            GUI.BeginGroup(r);
            float sx = r.width - Mathf.Repeat(t * 260f, r.width + 220f);
            GUI.color = Alpha(Foil, 0.2f * a);
            GUI.DrawTexture(new Rect(sx, 0f, 220f, r.height), MenuArt.StreakTex, ScaleMode.StretchToFill);
            GUI.EndGroup();
        }
        else ChevronRule(new Rect(r.x + 14f, r.yMax - 7f, 40f, 4f), Alpha(S.accent, 0.5f * a));
        if (index >= 0)
        {
            GUI.color = Alpha(selected ? Foil : S.accent, 0.9f * a);
            UIScale.Label(new Rect(r.x + 16f, r.y + 2f, 30f, r.height), IndexText[Mathf.Clamp(index, 0, 99)], Index);
        }
        Rect lr = new Rect(r.x + labelX, r.y, r.width - labelX - 8f, r.height);
        if (selected)
        {
            // The letters lit: two plates under the gold, oxblood left, bronze right
            GUI.color = Alpha(Oxblood, 0.7f * a);
            UIScale.Label(new Rect(lr.x - 2f, lr.y, lr.width, lr.height), label, ItemSelected);
            GUI.color = Alpha(DeepTeal, 0.7f * a);
            UIScale.Label(new Rect(lr.x + 2f, lr.y + 1f, lr.width, lr.height), label, ItemSelected);
            GUI.color = Alpha(Gold, a);
            UIScale.Label(lr, label, ItemSelected);
        }
        else
        {
            GUI.color = Alpha(S.accent, a);
            UIScale.Label(lr, label, Item);
        }
        if (!string.IsNullOrEmpty(sub))
        {
            // The record, right-aligned in what the letters leave: measured, stepped down before it could touch them
            GUI.color = selected ? Alpha(Foil, 0.9f * a) : subDim ? Alpha(Sick, 0.7f * a) : Alpha(S.accent, 0.75f * a);
            float subX = r.x + labelX + FitWidth(selected ? ItemSelected : Item, label) + 16f;
            FitLabel(new Rect(subX, r.y, r.xMax - 18f - subX, r.height), sub, Sub);
        }
        GUI.matrix = saved;
        GUI.color = Color.white;

        if (selected) SunCursor(r, a, since, Gold);
        return Click(r, passThrough);
    }

    static bool Click(Rect r, Rect? passThrough)
    {
        var e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition)
            && !(passThrough.HasValue && passThrough.Value.Contains(e.mousePosition)))
        {
            e.Use();
            return true;
        }
        return false;
    }

    public static bool Hovered(Rect r, float slide)
    {
        if (slide < 0.9f) return false;
        return r.Contains(Event.current.mousePosition);
    }

    // ---------------------------------------------------------------- the slider
    /// Persona slider: a slanted track with a bright fill (hatch crawling over it while it is live), tick marks,
    /// and a diamond knob with a brass edge. Owns its drag via hotControl. Returns the new value.
    /// The rect SlashSlider accepts a MouseDown on (8 px sideways, 10 px vertical forgiveness): hosts pass it as SlashItem's passThrough
    public static Rect SliderHit(Rect r) => new Rect(r.x - 8f, r.y - 10f, r.width + 16f, r.height + 20f);

    public static float SlashSlider(Rect r, float value, float min, float max, bool selected, Vector2 pivot)
    {
        int id = GUIUtility.GetControlID(FocusType.Passive);
        var e = Event.current;
        float t = Mathf.InverseLerp(min, max, value);
        Rect hit = SliderHit(r);
        switch (e.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                if (e.button == 0 && hit.Contains(e.mousePosition))
                {
                    GUIUtility.hotControl = id;
                    t = Mathf.Clamp01((e.mousePosition.x - r.x) / r.width);
                    e.Use();
                }
                break;
            case EventType.MouseDrag:
                if (GUIUtility.hotControl == id)
                {
                    t = Mathf.Clamp01((e.mousePosition.x - r.x) / r.width);
                    e.Use();
                }
                break;
            case EventType.MouseUp:
                if (GUIUtility.hotControl == id)
                {
                    GUIUtility.hotControl = 0;
                    e.Use();
                }
                break;
        }
        bool hot = GUIUtility.hotControl == id;
        float now = Time.unscaledTime;

        Matrix4x4 saved = GUI.matrix;
        UIScale.RotateAround(RowTilt, pivot);
        Fill(new Rect(r.x + 4f, r.y + 4f, r.width, r.height), new Color(0f, 0f, 0f, 0.55f));
        // The track: iron on the picked ink line, ink on the lit sheet; the fill gold on ink, brass on the sheet
        Fill(r, selected ? Iron : Ink);
        Color tick = selected ? S.accent : S.sheetDim;
        Fill(new Rect(r.x, r.yMax, r.width, 1f), Alpha(tick, 0.5f));
        Rect fill = new Rect(r.x, r.y, Mathf.Max(2f, r.width * t), r.height);
        Fill(fill, selected ? Gold : S.accent);
        if (selected) Hatch(fill, Alpha(Ink, 0.25f), hot ? 72f : 24f);
        for (int i = 0; i <= 4; i++) Fill(new Rect(r.x + r.width * i / 4f - 1f, r.yMax + 2f, 2f, 4f), Alpha(tick, 0.6f));

        Vector2 knob = new Vector2(r.x + r.width * t, r.y + r.height * 0.5f);
        if (hot) knob.x += Mathf.Repeat(now * 60f, 1f) < 0.5f ? 1f : -1f;
        // THE KNOB (2026-10-27): the pack's slider handle, cream on the picked row, the accent otherwise; the drawn diamond without it
        if (!UIAssets.Draw(new Rect(knob.x - 9f, knob.y - 13f, 18f, 24f), UIAssets.Pack("slide_hangle"), selected ? Paper : Alpha(S.accent, 0.95f)))
        {
            Matrix4x4 inner = GUI.matrix;
            UIScale.RotateAround(45f, knob);
            Fill(new Rect(knob.x - 6f, knob.y - 6f, 16f, 16f), new Color(0f, 0f, 0f, 0.6f));
            Fill(new Rect(knob.x - 8f, knob.y - 8f, 16f, 16f), selected ? Paper : Ink);
            Fill(new Rect(knob.x - 8f, knob.y - 8f, 1f, 16f), Alpha(selected ? Gold : S.accent, 0.9f));
            Fill(new Rect(knob.x - 8f, knob.y - 8f, 16f, 1f), Alpha(selected ? Gold : S.accent, 0.9f));
            GUI.matrix = inner;
        }

        GUI.matrix = saved;
        GUI.color = Color.white;
        return Mathf.Lerp(min, max, t);
    }

    // ---------------------------------------------------------------- the stamp
    /// ONE function draws every grade, on the cards, the report card and in the HUD's corner: the ladder runs
    /// ungraded (a dashed frame), D (limestone, hatched), C, B (a gold score line), A (a gold rule), S (a gold
    /// square with the company's sun turning behind it), S+ (lit brass, faster). `flow` overrides the square to the
    /// HUD's gold; `onGold` is a selected card, where an S inverts to ink so gold never sits on a light ground;
    /// `landing` is the one tick where the sheet lands before it prints.
    public static void RankSlab(Rect r, string rank, bool flow, float tilt, float punch, float alpha, GUIStyle letterStyle, bool onGold = false, bool landing = false, float flowMix = 1f)
    {
        Matrix4x4 saved = GUI.matrix;
        UIScale.RotateAround(tilt, r.center);
        UIScale.ScaleAround(new Vector2(punch, punch), r.center);
        float rule = r.width < 100f ? 2f : 3f, inset = r.width < 100f ? 4f : 8f;
        Color A(Color c, float k = 1f) => new Color(c.r, c.g, c.b, c.a * alpha * k);
        string letter = rank ?? "–";
        Color square = Panel, ruleColor = Color.clear, text = Paper;
        bool dashed = rank == null, hatch = false, scoreLine = false, foilTop = false;
        float burstSpin = 0f;
        Color burst = Color.clear;
        switch (rank)
        {
            case null: text = onGold ? S.sheetDim : S.subDim; break;
            case "D": hatch = true; text = Sick; break;
            case "C": text = Paper; break;
            case "B": text = Paper; scoreLine = true; break;
            case "A": ruleColor = Gold; text = Gold; break;
            case "S":
                if (onGold) { square = Ink; ruleColor = Gold; text = Foil; }
                else { square = Gold; ruleColor = Ink; text = Ink; foilTop = true; burst = new Color(Gold.r, Gold.g, Gold.b, 0.25f); burstSpin = 4f; }
                break;
            case "S+":
                square = Foil; ruleColor = Ink; text = Ink; burst = new Color(HudGold.r, HudGold.g, HudGold.b, 0.35f); burstSpin = 20f;
                break;
            default: text = Paper; break;
        }
        if (flow) { square = Color.Lerp(square, HudGold, flowMix); ruleColor = Ink; text = Color.Lerp(text, Ink, flowMix); }
        if (landing) { square = Ink; text = Gold; ruleColor = Color.clear; }

        if (burst.a > 0f) SlowBurst(r.center, r.width * 1.6f, burstSpin, A(burst));
        if (rank == "S" && onGold)
        {
            GUI.color = A(Ink, 0.3f);
            float s = r.width * 1.35f;
            GUI.DrawTexture(new Rect(r.center.x - s * 0.5f, r.center.y - s * 0.5f, s, s), GameFont.Logo);
        }
        if (dashed) DashFrame(r, A(onGold ? S.sheetDim : S.subDim, onGold ? 0.8f : 0.6f));
        else
        {
            Fill(r, A(square));
            if (hatch) Hatch(r, A(Sick, 0.35f), 0f);
            if (ruleColor.a > 0f)
            {
                Fill(new Rect(r.x + inset, r.y + inset, r.width - inset * 2f, rule), A(ruleColor));
                Fill(new Rect(r.x + inset, r.yMax - inset - rule, r.width - inset * 2f, rule), A(ruleColor));
                Fill(new Rect(r.x + inset, r.y + inset, rule, r.height - inset * 2f), A(ruleColor));
                Fill(new Rect(r.xMax - inset - rule, r.y + inset, rule, r.height - inset * 2f), A(ruleColor));
            }
            if (scoreLine) Fill(new Rect(r.x + inset, r.yMax - inset - 2f - 4f, r.width - inset * 2f, 4f), A(Gold));
            if (foilTop) Fill(new Rect(r.x, r.y, r.width, 1f), A(Foil));
        }
        GUI.color = A(text);
        UIScale.Label(r, letter, letterStyle);
        GUI.matrix = saved;
        GUI.color = Color.white;
    }

    // ---------------------------------------------------------------- the programme's medallion and the bill's wax
    /// A brass medallion for a grade on a programme postcard: a disc with a bronze rim, the letter in ink. The ladder:
    /// ungraded a dark bronze blank with a dash; D limestone; C and B brass (B with a foil inner ring); A gold; S gold
    /// on a foil rim with the company's sun turning behind it; S+ lit brass, faster.
    public static void Medallion(Rect r, string rank, float tilt, float punch, float alpha, GUIStyle letterStyle)
    {
        Matrix4x4 saved = GUI.matrix;
        UIScale.RotateAround(tilt, r.center);
        UIScale.ScaleAround(new Vector2(punch, punch), r.center);
        Texture2D disc = MenuArt.DiscTex(64), ring = MenuArt.FullRingTex;
        Color body = Brass, rim = Bullion, text = Ink;
        bool inner = false;
        float burst = 0f, spin = 4f;
        switch (rank)
        {
            case null: body = Alpha(Bullion, 0.6f); rim = Alpha(Sick, 0.5f); text = Sick; break;
            case "D": body = Sick; break;
            case "C": body = Alpha(Brass, 0.85f); break;
            case "B": inner = true; break;
            case "A": body = Gold; break;
            case "S": body = Gold; rim = Foil; burst = 0.3f; break;
            case "S+": body = Foil; rim = Gold; burst = 0.35f; spin = 20f; break;
        }
        if (burst > 0f) SlowBurst(r.center, r.width * 1.6f, spin, Alpha(Gold, burst * alpha));
        GUI.color = new Color(0f, 0f, 0f, 0.5f * alpha);
        GUI.DrawTexture(new Rect(r.x + 3f, r.y + 3f, r.width, r.height), disc);
        GUI.color = Alpha(body, body.a * alpha);
        GUI.DrawTexture(r, disc);
        GUI.color = Alpha(rim, rim.a * alpha);
        GUI.DrawTexture(r, ring);
        if (inner)
        {
            float k = r.width * 0.12f;
            GUI.color = Alpha(Foil, 0.8f * alpha);
            GUI.DrawTexture(new Rect(r.x + k, r.y + k, r.width - 2f * k, r.height - 2f * k), ring);
        }
        GUI.color = Alpha(text, alpha);
        UIScale.Label(r, rank ?? "–", letterStyle);
        GUI.matrix = saved;
        GUI.color = Color.white;
    }

    /// THE BILL's wax seal: red wax on its shadow, the company's sun pressed faint into it, the grade in cream over
    /// its own pressed shadow; `punch` is the slam, `tilt` the stamp's angle
    public static void WaxSeal(Vector2 pivot, float size, string letter, GUIStyle letterStyle, float alpha, float punch = 1f, float tilt = -8f)
    {
        Matrix4x4 saved = GUI.matrix;
        UIScale.RotateAround(tilt, pivot);
        UIScale.ScaleAround(new Vector2(punch, punch), pivot);
        Rect r = new Rect(pivot.x - size * 0.5f, pivot.y - size * 0.5f, size, size);
        GUI.color = new Color(0f, 0f, 0f, 0.55f * alpha);
        GUI.DrawTexture(new Rect(r.x + 4f, r.y + 5f, size, size), MenuArt.WaxTex);
        GUI.color = Alpha(Wax, alpha);
        GUI.DrawTexture(r, MenuArt.WaxTex);
        float s = size * 0.78f;
        GUI.color = Alpha(WaxDark, 0.55f * alpha);
        GUI.DrawTexture(new Rect(pivot.x - s * 0.5f, pivot.y - s * 0.5f, s, s), MenuArt.FullRingTex);
        float sun = size * 0.62f;
        GUI.color = Alpha(WaxDark, 0.35f * alpha);
        GUI.DrawTexture(new Rect(pivot.x - sun * 0.5f, pivot.y - sun * 0.5f, sun, sun), GameFont.Logo);
        if (!string.IsNullOrEmpty(letter)) // the register's seal carries only the sun
        {
            GUI.color = Alpha(WaxDark, alpha);
            UIScale.Label(new Rect(r.x + 1f, r.y + 2f, size, size), letter, letterStyle);
            GUI.color = Alpha(Paper, 0.94f * alpha);
            UIScale.Label(r, letter, letterStyle);
        }
        GUI.matrix = saved;
        GUI.color = Color.white;
    }

    // ---------------------------------------------------------------- the instrument panel
    /// The resolution each face is printed at: the speed dial is big, the pocket watch tiny
    static int FaceRes(MenuArt.Face k) => k == MenuArt.Face.Dial ? 256 : k == MenuArt.Face.Clock ? 192 : k == MenuArt.Face.Gauge ? 128 : 64;

    /// An instrument's face, `size` across on `pivot`, on its own ink shadow
    public static void Instrument(MenuArt.Face kind, Vector2 pivot, float size, float alpha)
    {
        Rect r = new Rect(pivot.x - size * 0.5f, pivot.y - size * 0.5f, size, size);
        GUI.color = new Color(0f, 0f, 0f, 0.5f * alpha);
        GUI.DrawTexture(new Rect(r.x + 3f, r.y + 4f, size, size), MenuArt.DiscTex(64));
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.DrawTexture(r, MenuArt.FaceTex(kind, FaceRes(kind)));
        GUI.color = Color.white;
    }

    /// The angle of a value on a 270° instrument: -135° (the left foot) at 0, +135° at 1, clockwise from twelve
    public static float Sweep(float v01) => -135f + 270f * Mathf.Clamp01(v01);

    /// A needle on `pivot`, `length` px to its tip, swung `angleDeg` clockwise from twelve
    public static void Needle(Vector2 pivot, float length, float angleDeg, Color c, float width = 5f)
    {
        Matrix4x4 m = GUI.matrix;
        UIScale.RotateAround(angleDeg, pivot);
        GUI.color = c;
        GUI.DrawTexture(new Rect(pivot.x - width * 0.5f, pivot.y - length, width, length / 0.8f), MenuArt.NeedleTex);
        GUI.matrix = m;
        GUI.color = Color.white;
    }

    /// The hub over a needle: a brass disc with an ink centre
    public static void Hub(Vector2 pivot, float size, float alpha)
    {
        Texture2D disc = MenuArt.DiscTex(64);
        GUI.color = Alpha(Brass, alpha);
        GUI.DrawTexture(new Rect(pivot.x - size * 0.5f, pivot.y - size * 0.5f, size, size), disc);
        float s = size * 0.4f;
        GUI.color = Alpha(Ink, alpha);
        GUI.DrawTexture(new Rect(pivot.x - s * 0.5f, pivot.y - s * 0.5f, s, s), disc);
        GUI.color = Color.white;
    }

    /// A lift lamp: a brass ring with the lamp inside it, the lamp's disc in `fill` at `scale` of the ring
    public static void Lamp(Vector2 pivot, float size, Color fill, float scale, Color ring)
    {
        Rect r = new Rect(pivot.x - size * 0.5f, pivot.y - size * 0.5f, size, size);
        GUI.color = ring;
        GUI.DrawTexture(r, MenuArt.FullRingTex);
        if (fill.a > 0f && scale > 0f)
        {
            float s = size * 0.72f * scale;
            GUI.color = fill;
            GUI.DrawTexture(new Rect(pivot.x - s * 0.5f, pivot.y - s * 0.5f, s, s), MenuArt.DiscTex(64));
        }
        GUI.color = Color.white;
    }

    /// A brass tube seen side-on around `r` (draw the column inside it first); the texture is keyed by the tube's size
    public static void Tube(Rect r, float alpha)
    {
        int w = Mathf.Max(16, Mathf.RoundToInt(r.width / 8f) * 8), h = Mathf.Max(8, Mathf.RoundToInt(r.height));
        GUI.color = Alpha(Brass, 0.92f * alpha);
        GUI.DrawTexture(r, MenuArt.TubeTex(w, h));
        GUI.color = Alpha(Foil, 0.35f * alpha);
        GUI.DrawTexture(new Rect(r.x + 6f, r.y + 1f, r.width - 12f, 1f), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    /// A ticker-tape chip: tape in `tape` with sprocket holes along both edges, for ink type
    public static void TapeChip(Rect r, Color tape, float alpha)
    {
        Fill(r, Alpha(tape, alpha));
        GUI.color = Alpha(Ink, 0.4f * alpha);
        MenuArt.Tile(new Rect(r.x, r.y, r.width, 5f), MenuArt.PerfTex, 0.6f, Vector2.zero);
        MenuArt.Tile(new Rect(r.x, r.yMax - 5f, r.width, 5f), MenuArt.PerfTex, 0.6f, Vector2.zero);
        GUI.color = Color.white;
    }

    /// THE MARQUEE: the rank as a letter card on a theatre marquee — the card in a stepped brass frame with a ring of
    /// twelve bulbs (one lit, chasing), the letter in ink. The ladder: ungraded a charcoal card, D limestone, C and B
    /// cream, A cream with every bulb lit brass, S gold with the bulbs lit foil and the sun behind, S+ lit brass. In
    /// flow the card goes the HUD's gold (`flowMix` blinks it while draining) and every bulb is lit.
    public static void MarqueeCard(Rect r, string rank, bool flow, float flowMix, float punch, float alpha, GUIStyle letterStyle, float t)
    {
        Matrix4x4 saved = GUI.matrix;
        UIScale.ScaleAround(new Vector2(punch, punch), r.center);
        Color card = Paper, letter = Ink;
        bool allLit = false;
        float burst = 0f, spin = 4f;
        switch (rank)
        {
            case null: card = Panel; letter = Sick; break;
            case "D": card = Sick; break;
            case "A": allLit = true; break;
            case "S": card = Gold; allLit = true; burst = 0.25f; break;
            case "S+": card = Foil; allLit = true; burst = 0.35f; spin = 20f; break;
        }
        if (flow) { card = Color.Lerp(card, HudGold, flowMix); letter = Ink; allLit = true; }
        if (burst > 0f) SlowBurst(r.center, r.width * 1.7f, spin, Alpha(Gold, burst * alpha));
        Fill(new Rect(r.x + 4f, r.y + 4f, r.width, r.height), new Color(0f, 0f, 0f, 0.5f * alpha));
        Fill(r, Alpha(card, alpha));
        StepFrame(new Rect(r.x + 3f, r.y + 3f, r.width - 6f, r.height - 6f), Alpha(Brass, 0.85f * alpha), 1f, 4f, 2);
        // The bulbs: three a side on the frame's outer edge, chasing one in three
        Texture2D disc = MenuArt.DiscTex(64);
        int phase = Mathf.FloorToInt(t * 3f);
        const float b = 5f;
        for (int i = 0; i < 12; i++)
        {
            int side = i / 3, k = i % 3;
            float along = r.width * (0.25f + 0.25f * k);
            float bx, by;
            if (side == 0) { bx = r.x + along; by = r.y; }
            else if (side == 1) { bx = r.xMax; by = r.y + along; }
            else if (side == 2) { bx = r.xMax - along; by = r.yMax; }
            else { bx = r.x; by = r.yMax - along; }
            bool lit = allLit || (i + phase) % 3 == 0;
            GUI.color = lit ? Alpha(Foil, 0.9f * alpha) : Alpha(Brass, 0.4f * alpha);
            GUI.DrawTexture(new Rect(bx - b * 0.5f, by - b * 0.5f, b, b), disc);
        }
        GUI.color = Alpha(letter, alpha);
        UIScale.Label(r, rank ?? "–", letterStyle);
        GUI.matrix = saved;
        GUI.color = Color.white;
    }

    // --- Navigation sounds: recorded foley from Resources/Impacts, nothing electronic — a cloth brush to move,
    // a metal latch to confirm. Silence, never a synth blip, if the recordings are missing

    public static AudioClip MakeMoveBlip()
        => Recorded("rpg_cloth") ?? Silent("menuMove");

    public static AudioClip MakeConfirm()
        => Recorded("rpg_metalLatch") ?? Recorded("rpg_metalClick") ?? Silent("menuConfirm");

    static AudioClip Recorded(string prefix)
    {
        var picks = new List<AudioClip>();
        foreach (var c in Resources.LoadAll<AudioClip>("Impacts"))
            if (c.name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)) picks.Add(c);
        return picks.Count == 0 ? null : picks[Random.Range(0, picks.Count)];
    }

    /// A clip of nothing, so a missing recording is a missing sound and never a null
    static AudioClip Silent(string name)
    {
        var clip = AudioClip.Create(name, 2205, 1, 44100, false);
        clip.SetData(new float[2205], 0);
        clip.hideFlags = HideFlags.HideAndDontSave;
        return clip;
    }
}
