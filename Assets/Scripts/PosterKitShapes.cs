using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE POSTER KIT's architecture (partial PosterKit): stepped masses with one lit face, fluting, crowns, windows
/// clustered by floor (never a grid), pylons, arches and walls with true openings, viaducts, sky bridges, fire
/// escapes, rails, stairs, brick courses, water towers. Every shape is one mesh; a mass remembers its tiers so its
/// dressing (Lit / Fluting / Crown / Windows) can follow its setbacks and its lean.
/// </summary>
public sealed partial class PosterKit
{
    public enum CrownKind { None, Spire, Sunburst, Ziggurat, Gothic, WaterTower }

    public sealed class MassShape
    {
        public struct Tier { public float x0, x1, y0, y1, inset; }
        public readonly List<Tier> tiers = new List<Tier>();
        public float baseY, shear, z;
        public Color colour;
        public Tier Top => tiers[tiers.Count - 1];
        public float TopY => Top.y1;
        /// The lean applied to a point on the mass
        public float X(float x, float y) => x + shear * (y - baseY);
    }

    /// A stepped tower silhouette: `steps` setbacks (3–5 for a hero), each 15–25 % narrower and not always centred,
    /// the top thin; `lean` degrees shears it (forced perspective), `taper` narrows the top tier's head (0..0.5)
    public MassShape Mass(float x, float baseY, float w, float h, int steps, Color c, float z, float lean = 0f, float taper = 0f)
    {
        var m = new MassShape { baseY = baseY, shear = Mathf.Tan(lean * Mathf.Deg2Rad), z = z, colour = c };
        steps = Mathf.Clamp(steps, 1, 6);
        float total = 0f;
        for (int i = 0; i < steps; i++) total += steps - i + 0.5f;
        float y = baseY, cx = x, width = w;
        var b = new Batch();
        for (int i = 0; i < steps; i++)
        {
            float th = h * (steps - i + 0.5f) / total, y1 = y + th;
            float inset = i == steps - 1 ? taper * width * 0.5f : 0f;
            float x0 = cx - width * 0.5f, x1 = cx + width * 0.5f;
            b.Poly(new Vector2(m.X(x0, y), y), new Vector2(m.X(x0 + inset, y1), y1), new Vector2(m.X(x1 - inset, y1), y1), new Vector2(m.X(x1, y), y));
            m.tiers.Add(new MassShape.Tier { x0 = x0, x1 = x1, y0 = y, y1 = y1, inset = inset });
            float next = width * Rand(0.75f, 0.85f);
            cx += Rand(-1f, 1f) * (width - next) * 0.3f;
            width = next;
            y = y1;
        }
        Put(b.Build("Mass"), z, Tone(c, z), name: "Mass");
        return m;
    }

    /// One lit face: a strip `fraction` of each tier's width on `side` (−1 left, +1 right); the rest stays black
    public void Lit(MassShape m, int side, float fraction, Color c)
    {
        var b = new Batch();
        foreach (var t in m.tiers)
        {
            float w = (t.x1 - t.x0) * fraction;
            float x0 = side < 0 ? t.x0 : t.x1 - w, x1 = x0 + w;
            b.Poly(new Vector2(m.X(x0, t.y0), t.y0), new Vector2(m.X(x0, t.y1), t.y1), new Vector2(m.X(x1, t.y1), t.y1), new Vector2(m.X(x1, t.y0), t.y0));
        }
        Put(b.Build("LitFace"), m.z, Tone(c, m.z), name: "LitFace");
    }

    /// Full-height ribs a shade lighter, every `spacing`
    public void Fluting(MassShape m, Color c, float spacing = 0.12f, float width = 0.035f)
    {
        var b = new Batch();
        foreach (var t in m.tiers)
            for (float x = t.x0 + spacing * 0.5f; x < t.x1 - width; x += spacing)
                b.Poly(new Vector2(m.X(x, t.y0), t.y0), new Vector2(m.X(x, t.y1 - 0.04f), t.y1 - 0.04f), new Vector2(m.X(x + width, t.y1 - 0.04f), t.y1 - 0.04f), new Vector2(m.X(x + width, t.y0), t.y0));
        Put(b.Build("Fluting"), m.z, Tone(c, m.z), name: "Fluting");
    }

    public void Crown(MassShape m, CrownKind kind, Color c)
    {
        var t = m.Top;
        float w = t.x1 - t.x0 - 2f * t.inset, cx = (t.x0 + t.x1) * 0.5f, y = t.y1;
        var b = new Batch();
        switch (kind)
        {
            case CrownKind.Spire:
                b.Tri(m.X(cx - w * 0.5f, y), y, m.X(cx, y + w * 1.4f), y + w * 1.4f, m.X(cx + w * 0.5f, y), y);
                b.Line(m.X(cx, y + w), y + w, m.X(cx, y + w * 2.2f), y + w * 2.2f, 0.03f);
                break;
            case CrownKind.Sunburst:
                b.Rect(m.X(cx - w * 0.5f, y), y, m.X(cx + w * 0.5f, y + 0.08f), y + 0.08f);
                for (int k = 0; k < 7; k++)
                {
                    float a = (25f + k * 21.67f) * Mathf.Deg2Rad, len = w * (k == 3 ? 0.7f : 0.55f);
                    b.Line(m.X(cx, y + 0.06f), y + 0.06f, m.X(cx + Mathf.Cos(a) * len, y + 0.06f + Mathf.Sin(a) * len), y + 0.06f + Mathf.Sin(a) * len, 0.035f);
                }
                break;
            case CrownKind.Ziggurat:
                for (int k = 0; k < 3; k++)
                {
                    float sw = w * (0.75f - k * 0.22f), y0 = y + k * 0.09f;
                    b.Rect(m.X(cx - sw * 0.5f, y0), y0, m.X(cx + sw * 0.5f, y0 + 0.09f), y0 + 0.09f);
                }
                b.Line(m.X(cx, y + 0.27f), y + 0.27f, m.X(cx, y + 0.27f + w * 0.5f), y + 0.27f + w * 0.5f, 0.03f);
                break;
            case CrownKind.Gothic:
                b.Tri(m.X(cx - w * 0.5f, y), y, m.X(cx, y + w * 1.6f), y + w * 1.6f, m.X(cx + w * 0.5f, y), y);
                b.Rect(m.X(cx - w * 0.5f - 0.1f, y), y, m.X(cx - w * 0.5f + 0.02f, y + 0.12f), y + 0.12f);
                b.Rect(m.X(cx + w * 0.5f - 0.02f, y), y, m.X(cx + w * 0.5f + 0.1f, y + 0.12f), y + 0.12f);
                b.Line(m.X(cx, y + w * 1.5f), y + w * 1.5f, m.X(cx, y + w * 1.9f), y + w * 1.9f, 0.025f);
                break;
            case CrownKind.WaterTower:
                Put(b.Build("Crown"), m.z, Tone(c, m.z));
                WaterTower(m.X(cx + w * 0.18f, y), y, Mathf.Clamp(w * 0.5f, 0.3f, 0.9f), c, m.z);
                return;
            default: return;
        }
        Put(b.Build("Crown"), m.z, Tone(c, m.z), name: "Crown");
    }

    /// Small warm windows with a mullion cross where they are big enough, clustered by floor (blocks of five bays by
    /// three floors hold one lit patch with probability `density`, plus a few singles), never a grid
    public void Windows(MassShape m, float density, Color c, int seed, float floorH = 0.2f, float bayW = 0.17f)
    {
        var r = new System.Random(seed);
        var b = new Batch();
        float ww = bayW * 0.5f, wh = floorH * 0.55f;
        bool cross = ww >= 0.12f;
        foreach (var t in m.tiers)
        {
            int floors = Mathf.FloorToInt((t.y1 - t.y0 - 0.12f) / floorH), bays = Mathf.FloorToInt((t.x1 - t.x0 - 0.12f) / bayW);
            if (floors < 1 || bays < 1) continue;
            var lit = new bool[bays, floors];
            for (int f = 0; f < floors; f += 3)
                for (int bb = 0; bb < bays; bb += 5)
                {
                    if (r.NextDouble() >= density) continue;
                    int pw = 2 + r.Next(3), ph = 1 + r.Next(2), px = bb + r.Next(5), py = f + r.Next(3);
                    for (int i = px; i < px + pw && i < bays; i++)
                        for (int j = py; j < py + ph && j < floors; j++) lit[i, j] = true;
                }
            for (int i = 0; i < bays; i++)
                for (int j = 0; j < floors; j++)
                {
                    if (!lit[i, j] && r.NextDouble() >= density * 0.06) continue;
                    float x = t.x0 + 0.06f + i * bayW + bayW * 0.5f, y = t.y0 + 0.06f + j * floorH + floorH * 0.5f;
                    Window(b, m, x, y, ww, wh, cross);
                }
        }
        if (b.Count > 0) Put(b.Build("Windows"), m.z, ToneLit(c, m.z), name: "Windows");
    }

    static void Window(Batch b, MassShape m, float x, float y, float w, float h, bool cross)
    {
        if (!cross) { Pane(b, m, x - w * 0.5f, y - h * 0.5f, x + w * 0.5f, y + h * 0.5f); return; }
        float g = 0.012f;
        Pane(b, m, x - w * 0.5f, y - h * 0.5f, x - g, y - g);
        Pane(b, m, x + g, y - h * 0.5f, x + w * 0.5f, y - g);
        Pane(b, m, x - w * 0.5f, y + g, x - g, y + h * 0.5f);
        Pane(b, m, x + g, y + g, x + w * 0.5f, y + h * 0.5f);
    }

    static void Pane(Batch b, MassShape m, float x0, float y0, float x1, float y1)
        => b.Poly(new Vector2(m.X(x0, y0), y0), new Vector2(m.X(x0, y1), y1), new Vector2(m.X(x1, y1), y1), new Vector2(m.X(x1, y0), y0));

    /// A finned wedge buttress: a trapezoid with three ribs a shade lighter and a cap
    public void Pylon(float x, float y, float w, float h, Color c, float z, Color fin)
    {
        var b = new Batch();
        b.Poly(new Vector2(x - w * 0.5f, y), new Vector2(x - w * 0.3f, y + h), new Vector2(x + w * 0.3f, y + h), new Vector2(x + w * 0.5f, y));
        b.Rect(x - w * 0.36f, y + h, x + w * 0.36f, y + h + 0.1f);
        Put(b.Build("Pylon"), z, Tone(c, z), name: "Pylon");
        var f = new Batch();
        for (int k = -1; k <= 1; k++)
        {
            float fx = x + k * w * 0.2f, fw = 0.05f;
            f.Poly(new Vector2(fx - fw, y), new Vector2(fx - fw * 0.8f, y + h), new Vector2(fx + fw * 0.8f, y + h), new Vector2(fx + fw, y));
        }
        Put(f.Build("PylonFins"), z, Tone(fin, z), name: "PylonFins");
    }

    /// A round-headed arch as a ring on two legs, `w` wide and `h` tall in all; `ry` stretches the head (an egg)
    public void Arch(float x, float y, float w, float h, float thickness, Color c, float z, float ry = 0f)
    {
        float rx = w * 0.5f;
        if (ry <= 0f) ry = rx;
        float sy = y + h - ry;
        var b = new Batch();
        b.Rect(x - rx, y, x - rx + thickness, sy);
        b.Rect(x + rx - thickness, y, x + rx, sy);
        b.Ring(x, sy, rx, ry, thickness, 0f, 180f);
        Put(b.Build("Arch"), z, Tone(c, z), name: "Arch");
    }

    /// A wall from (x0,y0) to (x1,y1) with round-headed openings `w` wide and `h` tall at `centres` — true holes
    public void WallWithArches(float x0, float y0, float x1, float y1, float[] centres, float w, float h, Color c, float z, float ry = 0f)
    {
        float rx = w * 0.5f;
        if (ry <= 0f) ry = rx;
        var b = new Batch();
        float left = x0;
        foreach (var cx in centres)
        {
            b.Rect(left, y0, cx - rx, y1);
            b.Spandrels(cx, y0 + h - ry, rx, ry, y1);
            left = cx + rx;
        }
        b.Rect(left, y0, x1, y1);
        Put(b.Build("ArchWall"), z, Tone(c, z), name: "ArchWall");
    }

    /// A railway viaduct: `bays` arches under a deck with a parapet, from x across `span`
    public void Viaduct(float x, float y, float span, int bays, float archH, float deckH, Color c, float z)
    {
        float bayW = span / bays, pierW = bayW * 0.24f;
        var centres = new float[bays];
        for (int i = 0; i < bays; i++) centres[i] = x + bayW * (i + 0.5f);
        WallWithArches(x, y, x + span, y + archH, centres, bayW - pierW, archH - 0.12f, c, z);
        Box(x, y + archH, x + span, y + archH + deckH, c, z);
        Box(x, y + archH + deckH, x + span, y + archH + deckH + 0.07f, Mix(c, pal.light, 0.35f), z);
    }

    /// A sky bridge between two masses: a deck on a shallow arch with hangers
    public void Bridge(float x1, float x2, float y, Color c, float z)
    {
        float cx = (x1 + x2) * 0.5f, rx = (x2 - x1) * 0.5f;
        var b = new Batch();
        b.Rect(x1, y, x2, y + 0.1f);
        b.Ring(cx, y - 0.5f, rx, 0.46f, 0.06f, 0f, 180f);
        for (int k = 1; k < 4; k++) b.Line(Mathf.Lerp(x1, x2, k * 0.25f), y - 0.5f, Mathf.Lerp(x1, x2, k * 0.25f), y, 0.025f);
        b.Rect(x1, y + 0.1f, x2, y + 0.13f);
        for (float px = x1 + 0.1f; px < x2; px += 0.3f) b.Rect(px, y + 0.1f, px + 0.025f, y + 0.3f);
        Put(b.Build("Bridge"), z, Tone(c, z), name: "Bridge");
    }

    /// A zigzag of flights between two heights up a wall, with landings, rails and the two posts; `dir` = the first flight's way
    public void FireEscape(float x, float y0, float y1, int flights, float w, Color c, float z, int dir = 1)
    {
        float fh = (y1 - y0) / flights;
        var b = new Batch();
        for (int k = 0; k < flights; k++)
        {
            float ya = y0 + k * fh, yb = ya + fh;
            int d = (k % 2 == 0) ? dir : -dir;
            float xa = d > 0 ? x : x + w, xb = d > 0 ? x + w : x;
            b.Line(xa, ya, xb, yb, 0.05f);
            b.Line(xa, ya + 0.16f, xb, yb + 0.16f, 0.02f);
            b.Rect(x, ya - 0.025f, x + w, ya + 0.025f);
        }
        b.Rect(x, y1 - 0.025f, x + w, y1 + 0.025f);
        b.Rect(x - 0.02f, y0, x + 0.02f, y1 + 0.2f);
        b.Rect(x + w - 0.02f, y0, x + w + 0.02f, y1 + 0.2f);
        Put(b.Build("FireEscape"), z, Tone(c, z), name: "FireEscape");
    }

    /// A parapet line with posts
    public void Rail(float y, float x0, float x1, Color c, float z, float postEvery = 0.35f)
    {
        var b = new Batch();
        b.Rect(x0, y + 0.12f, x1, y + 0.15f);
        for (float x = x0 + postEvery * 0.5f; x < x1; x += postEvery) b.Rect(x - 0.012f, y, x + 0.012f, y + 0.14f);
        Put(b.Build("Rail"), z, Tone(c, z), name: "Rail");
    }

    public void Stair(float x, float y, int steps, float stepW, float stepH, Color c, float z, int dir = 1)
    {
        var b = new Batch();
        for (int k = 0; k < steps; k++)
        {
            float sx = x + dir * k * stepW;
            b.Rect(Mathf.Min(sx, sx + dir * stepW), y + k * stepH, Mathf.Max(sx, sx + dir * stepW), y + (k + 1) * stepH);
        }
        Put(b.Build("Stair"), z, Tone(c, z), name: "Stair");
    }

    /// Brick courses: thin lines across a wall every `spacing`
    public void Courses(float x0, float y0, float x1, float y1, float spacing, Color c, float z)
    {
        var b = new Batch();
        for (float y = y0 + spacing; y < y1; y += spacing) b.Rect(x0, y - 0.008f, x1, y + 0.008f);
        Put(b.Build("Courses"), z, Tone(c, z), name: "Courses");
    }

    /// A rooftop water tower on stilts with a conical cap, `h` tall in all
    public void WaterTower(float x, float y, float h, Color c, float z)
    {
        float w = h * 0.55f;
        var b = new Batch();
        b.Rect(x - w * 0.5f, y + h * 0.4f, x + w * 0.5f, y + h * 0.8f);
        b.Tri(x - w * 0.55f, y + h * 0.8f, x, y + h, x + w * 0.55f, y + h * 0.8f);
        b.Line(x - w * 0.4f, y, x - w * 0.4f, y + h * 0.42f, 0.03f);
        b.Line(x, y, x, y + h * 0.42f, 0.03f);
        b.Line(x + w * 0.4f, y, x + w * 0.4f, y + h * 0.42f, 0.03f);
        b.Line(x - w * 0.4f, y + h * 0.1f, x + w * 0.4f, y + h * 0.3f, 0.02f);
        b.Line(x, y + h, x, y + h * 1.12f, 0.02f);
        Put(b.Build("WaterTower"), z, Tone(c, z), name: "WaterTower");
    }
}
