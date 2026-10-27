using UnityEngine;

/// <summary>
/// THE POSTER KIT's light, figures and furniture (partial PosterKit): searchlight beams, lamps with halos, louvre
/// light bands, chandelier bursts, the tiny elongated figures (a bellhop, the runner, the terrace couple, a doorman
/// with his lantern), trams, blimps, smoke columns, chequer floors in forced perspective, clocks and gauges, the
/// company chevron, rain, washing lines, marquees and bulb rows, black water with its glints.
/// </summary>
public sealed partial class PosterKit
{
    public enum FigureKind { Bellhop, Runner, Couple, Doorman }

    // ------------------------------------------------------------ light

    /// A searchlight: an additive soft-edged wedge from (x0, y0) at `angle` degrees (90 = up), `width` at its foot and
    /// `spread` times that at its end, fading along its length
    public void Beam(float x0, float y0, float angle, float length, float width, Color c, float alpha, float z, float spread = 2.5f)
    {
        float a = angle * Mathf.Deg2Rad;
        var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        var n = new Vector2(-d.y, d.x);
        var p = new Vector2(x0, y0);
        float w0 = width * 0.5f, w1 = width * spread * 0.5f;
        var b = new Batch();
        b.PolyUv(new[] { p - n * w0, p + n * w0, p + d * length + n * w1, p + d * length - n * w1 },
                 new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) });
        Put(b.Build("Beam"), z, With(c, alpha), Blend.Add, PosterLook.BeamTex, name: "Beam");
    }

    /// A lamp: a bright disc and a soft additive halo `halo` times its radius
    public void Lamp(float x, float y, float r, Color c, float z, float halo = 4f, float glow = 1.4f)
    {
        float hr = r * halo;
        Tex(x - hr, y - hr, x + hr, y + hr, PosterLook.HaloTex, With(c, 0.5f), z, Blend.Add);
        Disc(x, y, r, Glow(c, glow), z, true);
    }

    /// A hard-edged pale band of light across a floor or a wall (the louvre look)
    public void LightBand(float y, float x0, float x1, float h, Color c, float alpha, float z)
        => Box(x0, y, x1, y + h, With(c, alpha), z, true, Blend.Alpha);

    /// A pool of light on a floor in perspective: a trapezoid `near` wide at y0 and `far` wide at y1
    public void LightPool(float x, float y0, float y1, float near, float far, Color c, float alpha, float z)
    {
        var b = new Batch();
        b.Poly(new Vector2(x - near * 0.5f, y0), new Vector2(x - far * 0.5f, y1), new Vector2(x + far * 0.5f, y1), new Vector2(x + near * 0.5f, y0));
        Put(b.Build("LightPool"), z, With(c, alpha), Blend.Alpha, name: "LightPool");
    }

    /// A chandelier (or a sunburst): `rays` thin triangles from a core, their lengths jittered
    public void Burst(float x, float y, float r, int rays, float width, Color c, float z, float core = 0.15f)
    {
        var b = new Batch();
        for (int k = 0; k < rays; k++)
        {
            float a = (k * 360f / rays + Rand(-4f, 4f)) * Mathf.Deg2Rad, len = r * Rand(0.55f, 1f);
            float nx = -Mathf.Sin(a) * width * 0.5f, ny = Mathf.Cos(a) * width * 0.5f;
            b.Tri(x + nx, y + ny, x + Mathf.Cos(a) * len, y + Mathf.Sin(a) * len, x - nx, y - ny);
        }
        b.Disc(x, y, r * core);
        Put(b.Build("Burst"), z, c, name: "Burst");
    }

    // ------------------------------------------------------------ figures

    /// A tiny elongated silhouette, `scale` units tall, feet at (x, y), facing `dir`; never more than an eighth of the frame
    public void Figure(float x, float y, float scale, Color c, float z, FigureKind kind, int dir = 1)
    {
        var b = new Batch();
        float s = scale;
        float X(float u) => x + dir * u * s;
        float Y(float v) => y + v * s;
        void L(float x0, float y0, float x1, float y1, float w) => b.Line(X(x0), Y(y0), X(x1), Y(y1), w * s);
        void D(float cx, float cy, float r) => b.Disc(X(cx), Y(cy), r * s, 16);
        void Q(float ax, float ay, float bx, float by, float cx, float cy, float dx, float dy) => b.Poly(new Vector2(X(ax), Y(ay)), new Vector2(X(bx), Y(by)), new Vector2(X(cx), Y(cy)), new Vector2(X(dx), Y(dy)));
        switch (kind)
        {
            case FigureKind.Bellhop:
                L(-0.05f, 0f, -0.04f, 0.44f, 0.06f); L(0.05f, 0f, 0.04f, 0.44f, 0.06f);
                Q(-0.08f, 0.42f, -0.13f, 0.78f, 0.13f, 0.78f, 0.08f, 0.42f);
                D(0f, 0.87f, 0.075f); Q(-0.075f, 0.93f, -0.075f, 1.0f, 0.075f, 1.0f, 0.075f, 0.93f);
                L(0.12f, 0.74f, 0.3f, 0.62f, 0.05f); Q(0.22f, 0.6f, 0.22f, 0.63f, 0.42f, 0.63f, 0.42f, 0.6f);
                L(-0.12f, 0.74f, -0.14f, 0.5f, 0.05f);
                break;
            case FigureKind.Runner:
                Q(-0.06f, 0.44f, 0.08f, 0.84f, 0.2f, 0.8f, 0.06f, 0.42f);
                L(0f, 0.45f, -0.2f, 0.26f, 0.07f); L(-0.2f, 0.26f, -0.33f, 0.03f, 0.065f);
                L(0f, 0.45f, 0.22f, 0.3f, 0.07f); L(0.22f, 0.3f, 0.3f, 0.05f, 0.065f);
                L(0.14f, 0.8f, 0.35f, 0.66f, 0.055f); L(0.1f, 0.78f, -0.12f, 0.6f, 0.055f);
                D(0.2f, 0.9f, 0.075f);
                break;
            case FigureKind.Couple:
                L(-0.3f, 0f, -0.29f, 0.46f, 0.06f); L(-0.2f, 0f, -0.21f, 0.46f, 0.06f);
                Q(-0.33f, 0.44f, -0.39f, 0.82f, -0.11f, 0.82f, -0.17f, 0.44f);
                D(-0.25f, 0.9f, 0.075f); L(-0.12f, 0.78f, 0.02f, 0.62f, 0.05f);
                Q(0.18f, 0f, 0.22f, 0.56f, 0.34f, 0.56f, 0.38f, 0f);
                Q(0.2f, 0.54f, 0.18f, 0.82f, 0.38f, 0.82f, 0.36f, 0.54f);
                D(0.28f, 0.9f, 0.07f); L(0.2f, 0.78f, 0.06f, 0.62f, 0.045f);
                break;
            case FigureKind.Doorman:
                Q(-0.13f, 0.04f, -0.15f, 0.8f, 0.15f, 0.8f, 0.13f, 0.04f);
                L(-0.06f, 0f, -0.06f, 0.06f, 0.07f); L(0.06f, 0f, 0.06f, 0.06f, 0.07f);
                D(0f, 0.88f, 0.075f); Q(-0.09f, 0.93f, -0.09f, 1.0f, 0.09f, 1.0f, 0.09f, 0.93f); Q(0.05f, 0.92f, 0.05f, 0.94f, 0.16f, 0.94f, 0.16f, 0.92f);
                L(0.14f, 0.74f, 0.32f, 0.52f, 0.05f); L(-0.14f, 0.74f, -0.15f, 0.5f, 0.05f);
                break;
        }
        Put(b.Build("Figure"), z, c, name: "Figure");
    }

    // ------------------------------------------------------------ vehicles and air

    /// A tram or a carriage: a lit box `w` long with window dashes, a clerestory and (a tram) its pole; optional headlamp
    public void Tram(float x, float y, float w, Color body, Color windows, float z, bool headlamp, bool pole = true)
    {
        float h = w * 0.3f;
        var b = new Batch();
        b.Rect(x, y + 0.04f, x + w, y + h);
        b.Rect(x + w * 0.06f, y + h, x + w * 0.94f, y + h + w * 0.05f);
        if (pole) b.Line(x + w * 0.5f, y + h, x + w * 0.42f, y + h + w * 0.18f, 0.02f);
        Put(b.Build("Tram"), z, Tone(body, z), name: "Tram");
        var wb = new Batch();
        for (float wx = x + w * 0.1f; wx < x + w * 0.9f; wx += w * 0.1f) wb.Rect(wx, y + h * 0.45f, wx + w * 0.06f, y + h * 0.8f);
        Put(wb.Build("TramWindows"), z, ToneLit(windows, z), name: "TramWindows");
        if (headlamp) Lamp(x + w - 0.04f, y + h * 0.3f, h * 0.1f, pal.lamp, z, 5f);
    }

    /// A police blimp: an ellipse with tail fins and a lit gondola (its searchlight is the composition's Beam)
    public void Blimp(float x, float y, float w, Color c, float z)
    {
        float ry = w / 5.5f;
        var b = new Batch();
        b.Ellipse(x, y, w * 0.5f, ry);
        b.Tri(x - w * 0.42f, y + ry * 0.4f, x - w * 0.55f, y + ry * 1.3f, x - w * 0.3f, y + ry * 0.75f);
        b.Tri(x - w * 0.42f, y - ry * 0.4f, x - w * 0.55f, y - ry * 1.3f, x - w * 0.3f, y - ry * 0.75f);
        b.Rect(x - w * 0.08f, y - ry - w * 0.05f, x + w * 0.08f, y - ry + 0.02f);
        Put(b.Build("Blimp"), z, Tone(c, z), name: "Blimp");
        var g = new Batch();
        for (int k = -1; k <= 1; k++) g.Box(x + k * w * 0.045f, y - ry - w * 0.025f, w * 0.022f, w * 0.022f);
        Put(g.Build("Gondola"), z, ToneLit(pal.window, z), name: "Gondola");
    }

    /// A column of smoke or steam: soft discs rising from (x, y), widening and fading over `h`
    public void Smoke(float x, float y, float h, float w, Color c, float alpha, float z, int puffs = 9)
    {
        for (int i = 0; i < puffs; i++)
        {
            float t = i / (float)(puffs - 1);
            float cx = x + Mathf.Sin(i * 1.7f) * w * 0.25f * t + Rand(-0.05f, 0.05f) * w, cy = y + h * t;
            float r = w * (0.35f + 0.65f * t), a = alpha * (1f - t * 0.75f);
            Tex(cx - r, cy - r * 0.9f, cx + r, cy + r * 0.9f, PosterLook.HaloTex, With(c, a), z - i * 0.001f, Blend.Alpha);
        }
    }

    // ------------------------------------------------------------ floors, dials, weather, furniture

    /// A chequer floor in forced perspective from yFront (the frame's foot) to the horizon at yBack, converging on vx
    public void Chequer(float yFront, float yBack, int cols, Color a, Color b, float z, float vx = 0f, int rows = 7)
    {
        var ba = new Batch();
        var bb = new Batch();
        float W = HalfW * 1.6f;
        float Y(int r) => Mathf.Lerp(yFront, yBack, 1f - Mathf.Pow(1f - r / (float)rows, 2.2f));
        float Xc(int c, float y) => vx + (c / (float)cols - 0.5f) * 2f * W * Mathf.Max(0.02f, (y - yBack) / (yFront - yBack));
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                float y0 = Y(r), y1 = Y(r + 1);
                var batch = (r + c) % 2 == 0 ? ba : bb;
                batch.Poly(new Vector2(Xc(c, y0), y0), new Vector2(Xc(c, y1), y1), new Vector2(Xc(c + 1, y1), y1), new Vector2(Xc(c + 1, y0), y0));
            }
        Put(ba.Build("ChequerA"), z, Tone(a, z), name: "ChequerA");
        Put(bb.Build("ChequerB"), z, Tone(b, z), name: "ChequerB");
    }

    /// A clock face: a disc, its rim, twelve ticks and two hands (degrees, 0 = up, clockwise)
    public void Clock(float x, float y, float r, Color face, Color ink, float z, float hourDeg, float minDeg)
    {
        Disc(x, y, r, face, z);
        var b = new Batch();
        b.Ring(x, y, r, r, r * 0.06f);
        for (int k = 0; k < 12; k++)
        {
            float a = (90f - k * 30f) * Mathf.Deg2Rad;
            b.Line(x + Mathf.Cos(a) * r * 0.86f, y + Mathf.Sin(a) * r * 0.86f, x + Mathf.Cos(a) * r * 0.94f, y + Mathf.Sin(a) * r * 0.94f, r * (k % 3 == 0 ? 0.06f : 0.03f));
        }
        float ha = (90f - hourDeg) * Mathf.Deg2Rad, ma = (90f - minDeg) * Mathf.Deg2Rad;
        b.Line(x, y, x + Mathf.Cos(ha) * r * 0.55f, y + Mathf.Sin(ha) * r * 0.55f, r * 0.07f);
        b.Line(x, y, x + Mathf.Cos(ma) * r * 0.8f, y + Mathf.Sin(ma) * r * 0.8f, r * 0.045f);
        b.Disc(x, y, r * 0.06f, 12);
        Put(b.Build("ClockInk"), z, Tone(ink, z), name: "ClockInk");
    }

    /// A gauge: a face, a brass rim, ticks over 240 degrees and one needle (0 = the left stop, 1 = the right)
    public void Gauge(float x, float y, float r, Color face, Color ink, Color rim, float needle, float z)
    {
        Disc(x, y, r, face, z);
        Ring(x, y, r, r, r * 0.08f, rim, z);
        var b = new Batch();
        for (int k = 0; k <= 12; k++)
        {
            float a = (210f - k * 20f) * Mathf.Deg2Rad;
            b.Line(x + Mathf.Cos(a) * r * 0.78f, y + Mathf.Sin(a) * r * 0.78f, x + Mathf.Cos(a) * r * 0.88f, y + Mathf.Sin(a) * r * 0.88f, r * (k % 3 == 0 ? 0.05f : 0.025f));
        }
        float na = (210f - Mathf.Clamp01(needle) * 240f) * Mathf.Deg2Rad;
        b.Line(x, y, x + Mathf.Cos(na) * r * 0.74f, y + Mathf.Sin(na) * r * 0.74f, r * 0.05f);
        b.Disc(x, y, r * 0.08f, 12);
        Put(b.Build("GaugeInk"), z, Tone(ink, z), name: "GaugeInk");
    }

    /// The company chevron: two bars meeting at the bottom
    public void Chevron(float x, float y, float w, float h, float t, Color c, float z)
    {
        var b = new Batch();
        b.Line(x - w * 0.5f, y + h * 0.5f, x, y - h * 0.5f, t);
        b.Line(x + w * 0.5f, y + h * 0.5f, x, y - h * 0.5f, t);
        Put(b.Build("Chevron"), z, Tone(c, z), name: "Chevron");
    }

    /// Rain: `count` thin streaks at `angle` degrees over the whole frame, in front of everything but the grain
    public void Rain(int count, float angle, Color c, float alpha, float z = 0.6f)
    {
        var b = new Batch();
        float a = angle * Mathf.Deg2Rad;
        for (int i = 0; i < count; i++)
        {
            float x = Rand(-HalfW - 0.5f, HalfW + 0.5f), y = Rand(-HalfH - 0.5f, HalfH + 0.5f), len = Rand(0.25f, 0.6f);
            b.Line(x, y, x + Mathf.Cos(a) * len, y + Mathf.Sin(a) * len, 0.012f);
        }
        Put(b.Build("Rain"), z, With(c, alpha), Blend.Alpha, name: "Rain");
    }

    /// A washing line sagging between two points with `sheets` hung along it
    public void WashingLine(float x0, float y0, float x1, float y1, int sheets, Color line, Color sheet, float z)
    {
        float mx = (x0 + x1) * 0.5f, my = (y0 + y1) * 0.5f - 0.08f;
        var b = new Batch();
        b.Line(x0, y0, mx, my, 0.015f);
        b.Line(mx, my, x1, y1, 0.015f);
        Put(b.Build("Line"), z, Tone(line, z), name: "WashingLine");
        var s = new Batch();
        for (int k = 0; k < sheets; k++)
        {
            float t = (k + 0.5f) / sheets, x = Mathf.Lerp(x0, x1, t);
            float y = t < 0.5f ? Mathf.Lerp(y0, my, t * 2f) : Mathf.Lerp(my, y1, (t - 0.5f) * 2f);
            float w = Rand(0.12f, 0.2f), h = Rand(0.16f, 0.28f);
            s.Rect(x - w * 0.5f, y - h, x + w * 0.5f, y - 0.01f);
        }
        Put(s.Build("Sheets"), z, Tone(sheet, z), name: "Sheets");
    }

    /// A marquee: a lit band with bulb rows along its top and bottom edges and a glow beneath
    public void Marquee(float x, float y, float w, float h, Color c, Color bulbs, float z)
    {
        Box(x, y, x + w, y + h, c, z, true);
        Bulbs(x + 0.06f, y + h + 0.04f, x + w - 0.06f, y + h + 0.04f, Mathf.Max(4, (int)(w / 0.14f)), 0.03f, bulbs, z);
        Bulbs(x + 0.06f, y - 0.04f, x + w - 0.06f, y - 0.04f, Mathf.Max(4, (int)(w / 0.14f)), 0.03f, bulbs, z);
        LightPool(x + w * 0.5f, y - 0.06f, y - 0.6f, w, w * 1.3f, bulbs, 0.18f, z);
    }

    /// `n` bulb dots along a segment
    public void Bulbs(float x0, float y0, float x1, float y1, int n, float r, Color c, float z)
    {
        var b = new Batch();
        for (int k = 0; k < n; k++)
        {
            float t = n == 1 ? 0.5f : k / (float)(n - 1);
            b.Disc(Mathf.Lerp(x0, x1, t), Mathf.Lerp(y0, y1, t), r, 10);
        }
        Put(b.Build("Bulbs"), z, Glow(c, 1.3f), name: "Bulbs");
    }

    /// Black water `depth` below y with a few reflected highlights
    public void Water(float y, float depth, Color c, Color glint, float z, int dashes = 12)
    {
        Box(-HalfW - 0.5f, y - depth, HalfW + 0.5f, y, c, z);
        var b = new Batch();
        for (int k = 0; k < dashes; k++)
        {
            float x = Rand(-HalfW, HalfW), dy = y - Rand(0.1f, depth * 0.9f), w = Rand(0.2f, 0.6f);
            b.Rect(x, dy - 0.012f, x + w, dy + 0.012f);
        }
        Put(b.Build("Glints"), z, With(glint, 0.45f), Blend.Alpha, name: "Glints");
    }
}
