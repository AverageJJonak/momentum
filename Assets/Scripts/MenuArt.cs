using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE PRINT SHOP. Every pattern the menus are made of, generated once into a
/// small tiling texture and kept for the session: a halftone screen, diagonal
/// hatch, paper grain, a ray burst, a Deco sunburst fan, chevron rules and
/// friezes, fluting, a notary's seal, a ring, a dash, a light streak, a fade,
/// a glow — and, for THE INSTRUMENT PANEL, the instrument faces (a speed dial,
/// a hotel clock, a pocket watch, a pressure gauge), a needle, a disc, a brass
/// tube, a wax seal, felt and ticker-tape perforation. All of it is ink on
/// black paper — nothing is an asset. Draw with Tile() to scroll a pattern
/// across a rect under whatever GUI.matrix rotation and GUI.color tint is
/// current. Unity's texture row 0 is the drawn BOTTOM. Warm() touches
/// everything once so nothing is generated mid-frame later. The sized plates
/// (faces, discs, tubes…) are keyed by an int so a lookup never allocates.
/// </summary>
public static class MenuArt
{
    static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

    // The default plates, made once: every per-frame draw takes one of these, never a generator call (a generator
    // builds its cache key and a closure before it even looks in the cache)
    static Texture2D hatchTex, grainTex, burstTex, ringTex, dashTex, streakTex, halftoneTex, fadeTex, glowTex;
    static Texture2D fanTex, chevronTex, friezeTex, fluteTex, sealTex;
    static Texture2D fullRingTex, feltTex, perfTex, needleTex, waxTex;
    public static Texture2D HatchTex => hatchTex != null ? hatchTex : (hatchTex = Hatch());
    public static Texture2D GrainTex => grainTex != null ? grainTex : (grainTex = Grain());
    public static Texture2D BurstTex => burstTex != null ? burstTex : (burstTex = Burst());
    public static Texture2D RingTex => ringTex != null ? ringTex : (ringTex = Ring());
    public static Texture2D DashTex => dashTex != null ? dashTex : (dashTex = Dash());
    public static Texture2D StreakTex => streakTex != null ? streakTex : (streakTex = Streak());
    public static Texture2D HalftoneTex => halftoneTex != null ? halftoneTex : (halftoneTex = Halftone());
    public static Texture2D FadeTex => fadeTex != null ? fadeTex : (fadeTex = Fade());
    public static Texture2D GlowTex => glowTex != null ? glowTex : (glowTex = Glow());
    // The Deco plates
    public static Texture2D FanTex => fanTex != null ? fanTex : (fanTex = Fan());
    public static Texture2D ChevronTex => chevronTex != null ? chevronTex : (chevronTex = Chevron());
    public static Texture2D FriezeTex => friezeTex != null ? friezeTex : (friezeTex = Frieze());
    public static Texture2D FluteTex => fluteTex != null ? fluteTex : (fluteTex = Flute());
    public static Texture2D SealTex => sealTex != null ? sealTex : (sealTex = Seal());
    // The instrument panel's plates
    /// A closed ring (no gap): lamp rims, bezels, the wax seal's impression
    public static Texture2D FullRingTex => fullRingTex != null ? fullRingTex : (fullRingTex = Ring(64, 30f, 25f, -4f));
    /// Black felt: the directory board's ground
    public static Texture2D FeltTex => feltTex != null ? feltTex : (feltTex = Felt());
    /// Ticker-tape perforation: one sprocket hole per 12 px, tiled along a tape's edge
    public static Texture2D PerfTex => perfTex != null ? perfTex : (perfTex = Perforation());
    /// A needle: the tip at the drawn top, the hub a fifth of the way up from the bottom
    public static Texture2D NeedleTex => needleTex != null ? needleTex : (needleTex = Needle());
    /// A wax seal: a filled disc with a run-over rim
    public static Texture2D WaxTex => waxTex != null ? waxTex : (waxTex = Wax());

    /// A screen of dots: `period` px between dot centres, dots of radius `radius` px, white on clear
    public static Texture2D Halftone(int period = 8, float radius = 2.6f)
        => Make($"halftone{period}_{radius}", period, period, (x, y) =>
        {
            float dx = x + 0.5f - period * 0.5f, dy = y + 0.5f - period * 0.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Clamp01(radius + 0.5f - d);
        });

    /// A Deco sunburst rising from the bottom centre: `rays` wedges across the half-plane (odd, so one ray stands
    /// straight up), lit in from the apex and fading out toward the rim. Stretch it behind a masthead.
    public static Texture2D Fan(int width = 512, int height = 256, int rays = 23)
        => Make($"fan{width}_{height}_{rays}", width, height, (x, y) =>
        {
            float dx = x + 0.5f - width * 0.5f, dy = y + 0.5f;         // row 0 is the drawn bottom: the apex row
            float a = Mathf.Atan2(dy, dx) / Mathf.PI;                   // 0 at the right, 1 at the left
            float r = Mathf.Sqrt(dx * dx + dy * dy) / height;
            float wedge = Mathf.Repeat(a * rays, 1f);
            float ray = Mathf.Clamp01((0.25f - Mathf.Abs(wedge - 0.5f)) * rays * 2f + 0.5f);   // half duty, centred on the half-integers, a soft edge
            float fade = Mathf.Clamp01(1f - r);
            return ray * fade * fade * Mathf.Clamp01(r * 12f);
        }, FilterMode.Bilinear, TextureWrapMode.Clamp, TextureWrapMode.Clamp);

    /// A zigzag rule: one chevron per `period` px, `height` px tall, a 2 px line — tile it along a rule
    public static Texture2D Chevron(int period = 16, int height = 8)
        => Make($"chevron{period}_{height}", period, height, (x, y) =>
        {
            float u = Mathf.Abs((x + 0.5f) / period - 0.5f) * 2f;      // 0 at the centre, 1 at the edges
            float line = 0.75f + (height - 1.5f) * (1f - u);            // the peak in the middle, the feet at the edges
            return Mathf.Clamp01(1.5f - Mathf.Abs(y + 0.5f - line));
        }, FilterMode.Bilinear, TextureWrapMode.Repeat, TextureWrapMode.Clamp);

    /// A chevron frieze: bands of ink zigzagging across, half on and half off with a 1 px edge — fill the ground
    /// first, then this in ink over it (the Works' slivers and stripes)
    public static Texture2D Frieze(int period = 16)
        => Make($"frieze{period}", period, period, (x, y) =>
        {
            float u = Mathf.Abs((x + 0.5f) / period - 0.5f) * 2f;
            float v = Mathf.Repeat(y + 0.5f + u * period * 0.5f, period);
            float half = period * 0.5f;
            return Mathf.Clamp01(half - v + 0.5f) * Mathf.Clamp01(v + 0.5f);
        });

    /// Fluting: a vertical rib every `period` px, `width` px wide — the plaster's ribs, faint behind a panel
    public static Texture2D Flute(int period = 8, float width = 2f)
        => Make($"flute{period}_{width}", period, 4, (x, y) =>
        {
            float u = (x + 0.5f) % period;
            float d = Mathf.Min(u, period - u);
            return Mathf.Clamp01(width * 0.5f + 0.5f - d);
        });

    /// A notary's seal: an outer ring, an inner ring, and `ticks` teeth between them around the turn — the
    /// company's sun goes in the middle
    public static Texture2D Seal(int size = 128, int ticks = 36)
        => Make($"seal{size}_{ticks}", size, size, (x, y) =>
        {
            float dx = x + 0.5f - size * 0.5f, dy = y + 0.5f - size * 0.5f;
            float px = size * 0.5f;                                      // px per unit of radius
            float d = Mathf.Sqrt(dx * dx + dy * dy) / px;
            float outer = Mathf.Clamp01((0.98f - d) * px + 0.5f) * Mathf.Clamp01((d - 0.91f) * px + 0.5f);
            float inner = Mathf.Clamp01((0.79f - d) * px + 0.5f) * Mathf.Clamp01((d - 0.75f) * px + 0.5f);
            float band = Mathf.Clamp01((0.89f - d) * px + 0.5f) * Mathf.Clamp01((d - 0.81f) * px + 0.5f);
            float ang = Mathf.Atan2(dy, dx) / (2f * Mathf.PI) + 0.5f;
            float f = Mathf.Repeat(ang * ticks, 1f);
            float edgePx = (0.25f - Mathf.Abs(f - 0.5f)) * (2f * Mathf.PI * d * px / ticks);   // distance to the tooth's edge, in px
            float tooth = Mathf.Clamp01(edgePx + 0.5f);
            return Mathf.Max(outer, Mathf.Max(inner, band * tooth));
        }, FilterMode.Bilinear, TextureWrapMode.Clamp, TextureWrapMode.Clamp);

    /// Diagonal hatch at 45 degrees: `period` px between lines, each `width` px thick
    public static Texture2D Hatch(int period = 16, float width = 3f)
        => Make($"hatch{period}_{width}", period, period, (x, y) =>
        {
            float u = (x + y + 0.5f) % period;
            float d = Mathf.Min(u, period - u);
            return Mathf.Clamp01(width * 0.5f + 0.5f - d);
        });

    /// Paper grain: hashed noise, most of it faint, a few dark specks; point-filtered so the specks stay specks
    public static Texture2D Grain(int size = 128, int seed = 7)
        => Make($"grain{size}_{seed}", size, size, (x, y) =>
        {
            float n = Hash01(x, y, seed);
            return n > 0.97f ? 1f : n * 0.35f;
        }, FilterMode.Point);

    /// Felt: soft hashed nap, bilinear so it reads as cloth and not as specks — tile it in ink over a near-black ground
    public static Texture2D Felt(int size = 64, int seed = 21)
        => Make($"felt{size}_{seed}", size, size, (x, y) =>
        {
            float n = 0.5f * Hash01(x, y, seed) + 0.25f * Hash01(x >> 1, y >> 1, seed + 1) + 0.25f * Hash01(x >> 2, y >> 2, seed + 2);
            return 0.25f + 0.75f * n;
        });

    /// A burst of rays from the centre: `rays` spokes, each `duty` of its wedge lit, fading toward the rim
    public static Texture2D Burst(int size = 256, int rays = 24, float duty = 0.5f)
        => Make($"burst{size}_{rays}_{duty}", size, size, (x, y) =>
        {
            float dx = x + 0.5f - size * 0.5f, dy = y + 0.5f - size * 0.5f;
            float a = Mathf.Atan2(dy, dx) / (2f * Mathf.PI) + 0.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy) / (size * 0.5f);
            float wedge = Mathf.Repeat(a * rays, 1f);
            return wedge < duty ? Mathf.Clamp01(1.15f - r) : 0f;
        }, FilterMode.Bilinear, TextureWrapMode.Clamp, TextureWrapMode.Clamp);

    /// A ring with one gap at twelve o'clock, so a spin can be read (a negative gap closes it)
    public static Texture2D Ring(int size = 64, float outer = 27f, float inner = 24f, float gapDeg = 90f)
        => Make($"ring{size}_{outer}_{inner}_{gapDeg}", size, size, (x, y) =>
        {
            float dx = x + 0.5f - size * 0.5f, dy = y + 0.5f - size * 0.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float band = Mathf.Clamp01(outer + 0.5f - d) * Mathf.Clamp01(d - inner + 0.5f);
            float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;            // row 0 is the bottom, so +y is up: 90 = twelve o'clock
            float away = Mathf.Abs(Mathf.DeltaAngle(ang, 90f));
            float mask = Mathf.Clamp01((away - gapDeg * 0.5f) / 2f);   // a two-degree ramp at the gap's edges
            return band * mask;
        }, FilterMode.Bilinear, TextureWrapMode.Clamp, TextureWrapMode.Clamp);

    /// A dash: half on, half off, along x
    public static Texture2D Dash(int width = 16, int height = 4)
        => Make($"dash{width}_{height}", width, height, (x, y) => x < width / 2 ? 1f : 0f, FilterMode.Point);

    /// Ticker-tape perforation: one round sprocket hole of radius 2 per `period` px, on the middle row
    public static Texture2D Perforation(int period = 12, int height = 8)
        => Make($"perf{period}_{height}", period, height, (x, y) =>
        {
            float dx = x + 0.5f - period * 0.5f, dy = y + 0.5f - height * 0.5f;
            return Mathf.Clamp01(2.2f + 0.5f - Mathf.Sqrt(dx * dx + dy * dy));
        }, FilterMode.Bilinear, TextureWrapMode.Repeat, TextureWrapMode.Clamp);

    /// A light streak: five soft bands across, fading toward the top and bottom — stretch it to any rect
    public static Texture2D Streak(int width = 512, int height = 64)
        => Make($"streak{width}_{height}", width, height, (x, y) =>
        {
            float sum = 0f;
            sum += 1.0f * Mathf.Exp(-Sq((x - 64f) / 10f));
            sum += 0.5f * Mathf.Exp(-Sq((x - 140f) / 38f));
            sum += 0.9f * Mathf.Exp(-Sq((x - 260f) / 6f));
            sum += 0.35f * Mathf.Exp(-Sq((x - 330f) / 120f));
            sum += 0.7f * Mathf.Exp(-Sq((x - 450f) / 18f));
            float v = Mathf.Pow(1f - Mathf.Abs(y / (height * 0.5f) - 1f), 1.5f);
            return Mathf.Clamp01(sum) * v;
        }, FilterMode.Bilinear, TextureWrapMode.Repeat, TextureWrapMode.Clamp);

    /// A left-to-right fade, white to clear (tint it, rotate it: a gradient in any direction)
    public static Texture2D Fade(int size = 64)
        => Make($"fade{size}", size, 2, (x, y) => 1f - (x + 0.5f) / size, FilterMode.Bilinear, TextureWrapMode.Clamp, TextureWrapMode.Clamp);

    /// A soft disc: bright centre, clear rim — a glow behind a stamp or a knob
    public static Texture2D Glow(int size = 64)
        => Make($"glow{size}", size, size, (x, y) =>
        {
            float dx = (x + 0.5f) / size - 0.5f, dy = (y + 0.5f) / size - 0.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
            return Mathf.Clamp01(1f - r) * Mathf.Clamp01(1f - r);
        }, FilterMode.Bilinear, TextureWrapMode.Clamp, TextureWrapMode.Clamp);

    /// A needle: `width` px across at the hub, 2 px at the tip (the drawn top), with a short tail below the hub.
    /// The hub sits a fifth of the way up: rotate about (centre x, 0.8 × height) to swing it.
    public static Texture2D Needle(int width = 16, int height = 128)
        => Make($"needle{width}_{height}", width, height, (x, y) =>
        {
            float v = (y + 0.5f) / height;                               // 0 at the drawn bottom (the tail), 1 at the tip
            float hub = 0.2f;
            float half = v >= hub ? Mathf.Lerp(width * 0.5f, 1f, (v - hub) / (1f - hub)) : width * 0.3f * (v / hub);
            float dx = Mathf.Abs(x + 0.5f - width * 0.5f);
            return Mathf.Clamp01(half + 0.5f - dx);
        }, FilterMode.Bilinear, TextureWrapMode.Clamp, TextureWrapMode.Clamp);

    /// A wax seal: a filled disc whose rim has run over unevenly, with the impression's ring pressed in
    public static Texture2D Wax(int size = 128)
        => Make($"wax{size}", size, size, (x, y) =>
        {
            float dx = x + 0.5f - size * 0.5f, dy = y + 0.5f - size * 0.5f;
            float px = size * 0.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy) / px;
            float a = Mathf.Atan2(dy, dx);
            float rim = 0.90f + 0.045f * Mathf.Sin(5f * a + 1f) + 0.03f * Mathf.Sin(11f * a + 2.3f) + 0.015f * Mathf.Sin(17f * a);
            float body = Mathf.Clamp01((rim - d) * px + 0.5f);
            float ring = Mathf.Clamp01((0.74f - d) * px + 0.5f) * Mathf.Clamp01((d - 0.70f) * px + 0.5f);   // the pressed ring, a little lighter
            return body * (1f - 0.35f * ring);
        }, FilterMode.Bilinear, TextureWrapMode.Clamp, TextureWrapMode.Clamp);

    // ---------------------------------------------------------------- the instrument faces (colour plates, keyed by size)

    /// Which face: the speed dial (270°, 0–30 with the perfect-hop band in brass), the hotel clock (12 hours),
    /// the pocket watch (12 marks, no numerals' worth of room) or the pressure gauge (270°, tenths)
    public enum Face { Dial, Clock, Watch, Gauge }

    static readonly Dictionary<int, Texture2D> faces = new Dictionary<int, Texture2D>();
    static readonly Dictionary<int, Texture2D> discs = new Dictionary<int, Texture2D>();
    static readonly Dictionary<int, Texture2D> tubes = new Dictionary<int, Texture2D>();

    static readonly Color Cream = new Color(0.94f, 0.88f, 0.74f), BrassC = new Color(0.78f, 0.58f, 0.25f), InkC = new Color(0.05f, 0.05f, 0.06f);
    static readonly Color BronzeC = new Color(0.35f, 0.24f, 0.12f), FoilC = new Color(1f, 0.9f, 0.55f);

    /// The face of an instrument, `size` px across: a brass bezel with a bronze lip, a cream face, ink ticks
    public static Texture2D FaceTex(Face kind, int size)
    {
        int key = size * 8 + (int)kind;
        if (faces.TryGetValue(key, out var t) && t != null) return t;
        t = MakeFace(kind, size);
        faces[key] = t;
        return t;
    }

    /// A filled anti-aliased disc, `size` px across (hubs, lamps, bulbs, the medallion)
    public static Texture2D DiscTex(int size)
    {
        if (discs.TryGetValue(size, out var t) && t != null) return t;
        t = Make("disc" + size, size, size, (x, y) =>
        {
            float dx = x + 0.5f - size * 0.5f, dy = y + 0.5f - size * 0.5f;
            return Mathf.Clamp01(size * 0.5f - 0.5f + 0.5f - Mathf.Sqrt(dx * dx + dy * dy));
        }, FilterMode.Bilinear, TextureWrapMode.Clamp, TextureWrapMode.Clamp);
        discs[size] = t;
        return t;
    }

    /// A brass tube seen side-on: a rounded-rectangle ring `thick` px thick with `radius` px corners, `w`×`h`
    public static Texture2D TubeTex(int w, int h, float radius = 6f, float thick = 3f)
    {
        int key = w * 4096 + h;
        if (tubes.TryGetValue(key, out var t) && t != null) return t;
        t = Make("tube" + w + "_" + h, w, h, (x, y) =>
        {
            float px = x + 0.5f, py = y + 0.5f;
            float dOuter = RoundRectDist(px, py, 0f, 0f, w, h, radius);
            float dInner = RoundRectDist(px, py, thick, thick, w - thick, h - thick, Mathf.Max(0.5f, radius - thick));
            return Mathf.Clamp01(0.5f - dOuter) * Mathf.Clamp01(dInner + 0.5f);
        }, FilterMode.Bilinear, TextureWrapMode.Clamp, TextureWrapMode.Clamp);
        tubes[key] = t;
        return t;
    }

    /// Signed distance from a point to a rounded rectangle's edge (negative inside)
    static float RoundRectDist(float px, float py, float x0, float y0, float x1, float y1, float r)
    {
        float cx = Mathf.Clamp(px, x0 + r, x1 - r), cy = Mathf.Clamp(py, y0 + r, y1 - r);
        float dx = px - cx, dy = py - cy;
        return Mathf.Sqrt(dx * dx + dy * dy) - r;
    }

    static Texture2D MakeFace(Face kind, int size)
    {
        float sweep = kind == Face.Dial || kind == Face.Gauge ? 270f : 360f;
        int majors = kind == Face.Dial ? 6 : kind == Face.Gauge ? 10 : 12;    // divisions of the sweep
        int minors = kind == Face.Dial ? 5 : kind == Face.Gauge ? 2 : kind == Face.Clock ? 5 : 0;   // per major
        float band0 = kind == Face.Dial ? 0.6f : -1f, band1 = kind == Face.Dial ? 1f : -1f;   // the perfect-hop band, 18–30 of 30
        float R = size * 0.5f - 1f;
        return MakeColor("face" + (int)kind + "_" + size, size, size, (x, y) =>
        {
            float dx = x + 0.5f - size * 0.5f, dy = y + 0.5f - size * 0.5f;   // +dy is up: row 0 is the drawn bottom
            float d = Mathf.Sqrt(dx * dx + dy * dy) / R;
            float px = R;
            // The bezel: brass from 0.88 to 1 with a bronze lip on its inside
            float bezel = Mathf.Clamp01((1f - d) * px + 0.5f) * Mathf.Clamp01((d - 0.88f) * px + 0.5f);
            float lip = Mathf.Clamp01((0.905f - d) * px + 0.5f) * Mathf.Clamp01((d - 0.88f) * px + 0.5f);
            float face = Mathf.Clamp01((0.885f - d) * px + 0.5f);
            Color c = Cream; c.a = face;
            // The sweep: 0 at the start, 1 at the end, clockwise from the left foot of the arc
            float a = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;              // 0 at twelve, +90 at three
            float start = -sweep * 0.5f;
            float u = (a - start) / sweep;                               // the value along the sweep
            bool inSweep = sweep >= 360f || (u >= -0.004f && u <= 1.004f);
            if (inSweep)
            {
                // The band: brass between two radii across its part of the sweep
                if (band0 >= 0f && u >= band0 && u <= band1)
                {
                    float band = Mathf.Clamp01((0.84f - d) * px + 0.5f) * Mathf.Clamp01((d - 0.72f) * px + 0.5f);
                    c = Color.Lerp(c, BrassC, band * 0.9f);
                }
                // The ticks: arc distance to the nearest mark, in px, against the mark's half width
                float arcPx = d * px * Mathf.PI / 180f * sweep;         // px per unit of u at this radius
                float maj = Mathf.Abs(Mathf.Repeat(u * majors + 0.5f, 1f) - 0.5f) / majors * arcPx;
                float majTick = Mathf.Clamp01(1.5f - maj) * Mathf.Clamp01((0.86f - d) * px + 0.5f) * Mathf.Clamp01((d - 0.70f) * px + 0.5f);
                float minTick = 0f;
                if (minors > 0)
                {
                    float min = Mathf.Abs(Mathf.Repeat(u * majors * minors + 0.5f, 1f) - 0.5f) / (majors * minors) * arcPx;
                    minTick = Mathf.Clamp01(1f - min) * Mathf.Clamp01((0.86f - d) * px + 0.5f) * Mathf.Clamp01((d - 0.79f) * px + 0.5f);
                }
                float tick = Mathf.Max(majTick, minTick);
                if (sweep < 360f && (u < 0f || u > 1f)) tick = 0f;
                c = Color.Lerp(c, InkC, tick * 0.9f);
            }
            // A hairline ring inside the ticks, bronze, faint
            float inner = Mathf.Clamp01((0.68f - d) * px + 0.5f) * Mathf.Clamp01((d - 0.665f) * px + 0.5f);
            c = Color.Lerp(c, BronzeC, inner * 0.35f);
            c = Color.Lerp(c, BrassC, bezel);
            c = Color.Lerp(c, BronzeC, lip * 0.8f);
            c.a = Mathf.Max(face, bezel);
            // The bezel catches light at its upper left
            float sheen = Mathf.Clamp01(0.5f + 0.5f * (-dx + dy) / Mathf.Max(1f, R));
            c = Color.Lerp(c, FoilC, bezel * sheen * 0.35f);
            return c;
        }, FilterMode.Bilinear, TextureWrapMode.Clamp, TextureWrapMode.Clamp);
    }

    /// Everything above, generated now rather than in the first frame that wants it
    public static void Warm()
    {
        _ = HatchTex; _ = GrainTex; _ = BurstTex; _ = RingTex; _ = DashTex; _ = StreakTex; _ = HalftoneTex; _ = FadeTex; _ = GlowTex;
        _ = FanTex; _ = ChevronTex; _ = FriezeTex; _ = FluteTex; _ = SealTex;
        _ = FullRingTex; _ = FeltTex; _ = PerfTex; _ = NeedleTex; _ = WaxTex;
        _ = DiscTex(64);
    }

    /// Draw `tex` tiled across `r` at `scale` canvas px per texel, scrolled by `offset` px; `flipY` turns it over
    public static void Tile(Rect r, Texture2D tex, float scale, Vector2 offset, bool flipY = false)
    {
        if (tex == null) return;
        float tw = tex.width * scale, th = tex.height * scale;
        float u = r.width / tw, v = r.height / th;
        Rect coords = flipY
            ? new Rect(offset.x / tw, 1f - offset.y / th, u, -v)
            : new Rect(offset.x / tw, offset.y / th, u, v);
        GUI.DrawTextureWithTexCoords(r, tex, coords);
    }

    static float Sq(float v) => v * v;

    static Texture2D Make(string key, int w, int h, System.Func<int, int, float> alpha,
        FilterMode filter = FilterMode.Bilinear, TextureWrapMode wrapU = TextureWrapMode.Repeat, TextureWrapMode wrapV = TextureWrapMode.Repeat)
    {
        if (cache.TryGetValue(key, out var t) && t != null) return t;
        t = NewTexture(key, w, h, filter, wrapU, wrapV);
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha(x, y)) * 255f);
                px[y * w + x] = new Color32(255, 255, 255, a);
            }
        t.SetPixels32(px);
        t.Apply(false, true);
        cache[key] = t;
        return t;
    }

    /// A colour plate: the generator gives every texel its own colour and alpha (the instrument faces)
    static Texture2D MakeColor(string key, int w, int h, System.Func<int, int, Color> color,
        FilterMode filter = FilterMode.Bilinear, TextureWrapMode wrapU = TextureWrapMode.Clamp, TextureWrapMode wrapV = TextureWrapMode.Clamp)
    {
        if (cache.TryGetValue(key, out var t) && t != null) return t;
        t = NewTexture(key, w, h, filter, wrapU, wrapV);
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = color(x, y);
                px[y * w + x] = new Color32((byte)Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f), (byte)Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f),
                    (byte)Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f), (byte)Mathf.RoundToInt(Mathf.Clamp01(c.a) * 255f));
            }
        t.SetPixels32(px);
        t.Apply(false, true);
        cache[key] = t;
        return t;
    }

    static Texture2D NewTexture(string key, int w, int h, FilterMode filter, TextureWrapMode wrapU, TextureWrapMode wrapV)
        => new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            name = "MenuArt_" + key,
            wrapModeU = wrapU,
            wrapModeV = wrapV,
            filterMode = filter,
            hideFlags = HideFlags.HideAndDontSave,
        };

    /// A stable hash in 0..1 of three integers: the same inputs always give the same answer, frame after frame
    public static float Hash01(int x, int y, int seed)
    {
        uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1274126177);
        h = (h ^ (h >> 13)) * 1274126177u;
        return ((h ^ (h >> 16)) & 0xFFFFFF) / 16777215f;
    }
}
