using UnityEngine;
using CrownKind = PosterKit.CrownKind;
using FigureKind = PosterKit.FigureKind;

/// <summary>
/// ACT II's posters: the underground. The "sky" is the far haze of a hall or a bore; the light is a lamp, a headlamp,
/// the third rail's arc. One hue each (PosterLook), the landmark on a third, a tiny figure.
/// </summary>
public static class PosterActII
{
    /// LAST TRAIN · HALCYON METRO — steel blue and cream tile: the lift's depth dial as a huge gauge, the escalators as
    /// diagonal bands, a train's headlamp beam across the platform, a doorman's lantern
    public static void Metro(PosterKit p)
    {
        p.Use(PosterLook.SteelCream);
        var P = p.pal;
        p.Sky(P.skyTop, P.skyBottom, 0.8f);
        // the tiled hall: the far wall, its cream band between two oxblood lines
        p.Box(-8.5f, -2.6f, 8.5f, 4.6f, P.mid, 7f);
        p.Box(-8.5f, -1.3f, 8.5f, 1.5f, P.light, 6.9f);
        p.Courses(-8.5f, -1.3f, 8.5f, 1.5f, 0.2f, PosterKit.Mix(P.light, P.mid, 0.35f), 6.85f);
        p.Box(-8.5f, 1.5f, 8.5f, 1.62f, P.accent, 6.85f);
        p.Box(-8.5f, -1.42f, 8.5f, -1.3f, P.accent, 6.85f);
        p.FogBand(-1.0f, 2.4f, P.haze, 0.4f, 6.5f);
        p.Gauge(-4.3f, 1.5f, 2.7f, P.light, P.ink, PosterLook.Brass, 0.82f, 5.5f);
        // the escalators: three bands descending to the right, their steps and balustrades
        for (int k = 0; k < 3; k++)
        {
            float ox = k * 1.15f, z = 4f - k * 0.05f;
            p.Poly(k % 2 == 0 ? P.light : P.mid, z, false, new Vector2(-2.4f + ox, 2.3f), new Vector2(-1.7f + ox, 2.3f), new Vector2(6.4f + ox, -2.6f), new Vector2(5.7f + ox, -2.6f));
            var steps = new PosterKit.Batch();
            for (float t = 0.05f; t < 1f; t += 0.07f)
                steps.Line(Mathf.Lerp(-2.4f + ox, 5.7f + ox, t), Mathf.Lerp(2.3f, -2.6f, t), Mathf.Lerp(-1.7f + ox, 6.4f + ox, t), Mathf.Lerp(2.3f, -2.6f, t), 0.02f);
            steps.Line(-2.05f + ox, 2.65f, 6.05f + ox, -2.25f, 0.05f);
            p.Put(steps.Build("Escalator"), z - 0.02f, p.Tone(P.shade, z));
        }
        // the platform edge, the train's lamps and its beam across the tile
        p.Ground(-2.6f, P.shade, 3.5f);
        p.Box(-8.5f, -2.66f, 8.5f, -2.56f, P.light, 3.4f);
        p.Disc(6.7f, -1.9f, 0.7f, P.ink, 3f);
        p.Box(6.0f, -2.6f, 8.5f, -1.9f, P.ink, 3f);
        p.Lamp(6.45f, -2.0f, 0.12f, P.lamp, 2.9f, 6f);
        p.Lamp(6.95f, -2.0f, 0.12f, P.lamp, 2.9f, 6f);
        p.Beam(6.4f, -1.95f, 174f, 9.5f, 0.5f, P.beam, 0.38f, 2.95f, 3.5f);
        // the doorman and his lantern
        p.Ground(-3.6f, P.ink, 1.5f);
        p.Figure(-5.6f, -3.6f, 1.0f, P.ink, 1f, FigureKind.Doorman, 1);
        p.Lamp(-5.28f, -3.08f, 0.06f, P.window, 0.95f, 7f);
        p.LightPool(-5.0f, -3.6f, -4.3f, 1.6f, 2.4f, P.window, 0.14f, 1.1f);
    }

    /// THIRD RAIL · THE TUNNELS — violet-black: a tunnel bore of receding rings, the third rail's amber arc, two
    /// headlamps bearing down with their beams, a signal of red over amber, the runner on the walkway
    public static void Tunnels(PosterKit p)
    {
        p.Use(PosterLook.VioletBlack);
        var P = p.pal;
        p.Sky(P.skyTop, P.skyBottom, 0.9f);
        float vx = 2.2f, vy = -0.5f;
        p.Disc(vx, vy, 1.1f, P.haze, 8.6f, true);
        for (int i = 5; i >= 0; i--)
        {
            float r = 1.0f + i * 1.25f;
            p.Ring(vx, vy + i * 0.12f, r, r * 1.12f, r * 0.14f, PosterKit.Mix(P.shade, P.mid, 0.3f), 8f - i * 0.9f);   // the nearest at 3.5, behind the track bed
        }
        // the track bed, the running rails and the live one
        p.Poly(P.ink, 3f, false, new Vector2(-8.5f, -4.6f), new Vector2(8.5f, -4.6f), new Vector2(vx + 0.5f, vy - 0.4f), new Vector2(vx - 0.5f, vy - 0.4f));
        p.Line(-7.5f, -4.4f, vx - 0.3f, vy - 0.45f, 0.06f, P.shade, 2.9f);
        p.Line(7.0f, -4.4f, vx + 0.3f, vy - 0.45f, 0.06f, P.shade, 2.9f);
        p.Line(-5.6f, -4.2f, vx - 0.05f, vy - 0.5f, 0.05f, PosterKit.Glow(P.window, 1.5f), 2.85f, true);
        p.Burst(-3.0f, -3.3f, 0.55f, 14, 0.03f, PosterKit.Glow(P.window, 1.8f), 2.8f, 0.1f);
        p.Tex(-3.8f, -4.1f, -2.2f, -2.5f, PosterLook.HaloTex, PosterKit.With(P.window, 0.5f), 2.82f, PosterKit.Blend.Add);
        // the walkway on the left, the cable tray on the right
        p.Poly(P.shade, 2.6f, false, new Vector2(-8.5f, -3.5f), new Vector2(-8.5f, -2.3f), new Vector2(vx - 0.9f, vy - 0.3f), new Vector2(vx - 0.9f, vy - 0.5f));
        p.Line(8.5f, 1.8f, vx + 0.9f, vy + 0.1f, 0.07f, P.shade, 3.2f);
        p.Line(8.5f, 1.5f, vx + 0.9f, vy + 0.0f, 0.04f, P.shade, 3.2f);
        // the train: two headlamps and their beams
        p.Disc(vx, vy + 0.3f, 0.9f, P.ink, 5f);
        p.Lamp(vx - 0.3f, vy + 0.1f, 0.14f, P.lamp, 4.9f, 7f, 1.6f);
        p.Lamp(vx + 0.3f, vy + 0.1f, 0.14f, P.lamp, 4.9f, 7f, 1.6f);
        p.Beam(vx - 0.3f, vy + 0.1f, 232f, 7f, 0.35f, P.lamp, 0.33f, 4.8f, 5f);
        p.Beam(vx + 0.3f, vy + 0.1f, 236f, 7f, 0.35f, P.lamp, 0.33f, 4.8f, 5f);
        p.FogBand(vy, 2.4f, P.haze, 0.35f, 6.5f);
        // the signal on the left wall: red over amber
        p.Box(-3.5f, 0.1f, -3.1f, 1.5f, P.ink, 3.4f);
        p.Line(-3.3f, -2.5f, -3.3f, 0.1f, 0.05f, P.ink, 3.4f);
        p.Lamp(-3.3f, 1.2f, 0.1f, PosterLook.SignalRed, 3.35f, 5f, 1.5f);
        p.Lamp(-3.3f, 0.5f, 0.1f, P.window, 3.35f, 5f, 1.3f);
        p.Figure(-5.4f, -1.85f, 0.85f, P.ink, 1.2f, FigureKind.Runner, 1);
    }

    /// HIGH WATER · THE SEWERS — steel blue over brick umber: egg-shaped brick arches in a row, a weir with the water
    /// falling as pale bands into black water, a sluice gate, a caged lamp, the runner on the weir's lip
    public static void Sewers(PosterKit p)
    {
        p.Use(PosterLook.SteelBrick);
        var P = p.pal;
        p.Sky(P.skyTop, P.skyBottom, 0.9f);
        p.Ring(0.2f, 0.6f, 1.4f, 1.9f, 0.3f, P.mid, 8f);
        p.FogBand(-0.6f, 2.6f, P.haze, 0.55f, 7.5f);
        // the brick wall with its three egg openings, their rings a lighter brick
        float[] cs = { -4.8f, 0f, 4.8f };
        p.WallWithArches(-8.5f, -2.5f, 8.5f, 4.6f, cs, 3.2f, 4.9f, P.mid, 6f, 2.1f);
        p.Courses(-8.5f, -2.5f, 8.5f, 4.6f, 0.22f, P.shade, 5.95f);
        foreach (float cx in cs) p.Arch(cx, -2.5f, 3.2f, 4.9f, 0.3f, P.light, 5.9f, 2.1f);
        // the sluice gate on the left: its iron frame, wheel and chain
        p.Box(-7.3f, -2.2f, -5.5f, 1.6f, P.ink, 4.5f);
        p.Box(-7.4f, 1.6f, -5.4f, 1.85f, P.accent, 4.5f);
        p.Line(-7.3f, -2.2f, -7.3f, 1.6f, 0.08f, P.accent, 4.45f);
        p.Line(-5.5f, -2.2f, -5.5f, 1.6f, 0.08f, P.accent, 4.45f);
        p.Ring(-6.4f, 2.4f, 0.4f, 0.4f, 0.07f, P.accent, 4.45f);
        p.Line(-6.4f, 1.85f, -6.4f, 2.4f, 0.05f, P.accent, 4.45f);
        // a lamp in its cage
        p.Line(2.4f, 1.9f, 2.4f, 1.3f, 0.03f, P.accent, 4.4f);
        p.Lamp(2.4f, 1.15f, 0.1f, P.lamp, 4.4f, 7f);
        p.Ring(2.4f, 1.15f, 0.17f, 0.2f, 0.02f, P.accent, 4.35f);
        // the weir: black water below, the fall as pale bands
        p.Water(-2.5f, 2.3f, P.ink, P.accent, 3.6f, 14);
        p.Box(-8.5f, -2.55f, 8.5f, -2.2f, P.shade, 3.5f);
        p.Box(-8.5f, -2.2f, 8.5f, -2.12f, P.light, 3.5f);
        foreach (float x in new[] { -7.6f, -5.3f, -3.6f, -1.0f, 1.4f, 3.9f, 6.3f })
            p.Box(x, -4.6f, x + p.Rand(0.25f, 0.45f), -2.55f, PosterKit.With(P.accent, 0.35f), 3.4f, true, PosterKit.Blend.Alpha);
        p.Box(-8.5f, -2.62f, 8.5f, -2.55f, PosterKit.With(P.accent, 0.6f), 3.38f, true, PosterKit.Blend.Alpha);
        p.Figure(-1.6f, -2.12f, 0.85f, P.ink, 2f, FigureKind.Runner, 1);
        p.FogBand(-3.4f, 1.6f, P.haze, 0.3f, 1.8f);
    }

    /// THE FOUNDATIONS · OLD TOWN — umber: a buried street of shopfronts under a vault, a chasm, columns as boost
    /// pillars, a slab tilting over the drop, a lantern, the runner mid-leap
    public static void Foundations(PosterKit p)
    {
        p.Use(PosterLook.Umber);
        var P = p.pal;
        p.Sky(P.skyTop, P.skyBottom, 0.8f);
        p.FogBand(-0.4f, 3.0f, P.haze, 0.5f, 8.2f);
        p.Ring(0f, -1.6f, 8.0f, 5.6f, 0.5f, P.shade, 7f, 0f, 180f);
        p.Ring(0f, -2.0f, 9.6f, 6.8f, 0.6f, P.ink, 5.5f, 0f, 180f);
        // the buried street: a ledge of shopfronts on the left, each with a lit window, an awning and a door
        p.Box(-8.5f, -1.35f, -1.4f, -1.1f, P.shade, 5f);
        float[] sx = { -8.0f, -6.0f, -4.0f };
        for (int i = 0; i < 3; i++)
        {
            float x = sx[i];
            p.Box(x, -1.1f, x + 1.7f, 1.0f + i * 0.2f, P.mid, 5.2f);
            p.Box(x + 0.25f, -0.85f, x + 1.1f, -0.15f, PosterKit.Glow(P.window, 1.1f), 5.1f, true);
            p.Line(x + 0.45f, -0.85f, x + 0.45f, -0.15f, 0.025f, P.ink, 5.05f);
            p.Line(x + 0.25f, -0.5f, x + 1.1f, -0.5f, 0.025f, P.ink, 5.05f);
            p.Poly(P.accent, 5.05f, false, new Vector2(x + 0.15f, -0.1f), new Vector2(x + 1.2f, -0.1f), new Vector2(x + 1.3f, 0.12f), new Vector2(x + 0.05f, 0.12f));
            p.Box(x + 1.25f, -1.1f, x + 1.55f, -0.3f, P.ink, 5.1f);
        }
        // the columns at the right third, with their capitals
        foreach (float x in new[] { 2.6f, 4.3f, 6.0f })
        {
            p.Box(x - 0.24f, -3.3f, x + 0.24f, 2.2f, P.shade, 4f);
            p.Box(x - 0.36f, 2.2f, x + 0.36f, 2.42f, P.shade, 4f);
            p.Line(x - 0.1f, -3.3f, x - 0.1f, 2.2f, 0.03f, P.mid, 3.98f);
            p.Line(x + 0.1f, -3.3f, x + 0.1f, 2.2f, 0.03f, P.mid, 3.98f);
        }
        // the floor, the chasm through it, the slab tilting over it
        p.Ground(-2.6f, P.shade, 3.4f);
        p.Poly(P.ink, 3.3f, false, new Vector2(-1.4f, -4.6f), new Vector2(-0.7f, -2.6f), new Vector2(2.3f, -2.6f), new Vector2(1.8f, -4.6f));
        var s = new Vector2(0.5f, -2.95f);
        float ca = Mathf.Cos(14f * Mathf.Deg2Rad), sa = Mathf.Sin(14f * Mathf.Deg2Rad);
        Vector2 R(float ux, float uy) => s + new Vector2(ux * ca - uy * sa, ux * sa + uy * ca);
        p.Poly(P.mid, 2.6f, false, R(-1.0f, -0.14f), R(-1.0f, 0.14f), R(1.0f, 0.14f), R(1.0f, -0.14f));
        p.Line(R(-0.2f, 0.14f).x, R(-0.2f, 0.14f).y, R(0.1f, -0.14f).x, R(0.1f, -0.14f).y, 0.025f, P.ink, 2.55f);
        // the lantern on its bracket; the runner leaping the chasm
        p.Line(-1.4f, 0.4f, -1.9f, 0.4f, 0.04f, P.ink, 4.8f);
        p.Lamp(-1.95f, 0.3f, 0.11f, P.lamp, 4.8f, 7f);
        p.LightPool(-1.9f, -1.1f, 0.2f, 2.4f, 0.6f, P.lamp, 0.12f, 4.9f);
        p.Ground(-3.7f, P.ink, 1.6f);
        p.Figure(0.3f, -2.0f, 0.9f, P.ink, 1.2f, FigureKind.Runner, 1);
    }

    /// TERMINUS · THE STATIONMASTER — violet with amber: the terminus clock huge and dark in its arch, platforms
    /// converging on the throat, the locomotive's headlamp and smoke column, the last carriage's red tail lamp
    public static void Terminus(PosterKit p)
    {
        p.Use(PosterLook.VioletBlack);
        var P = p.pal;
        p.Sky(P.skyTop, PosterKit.Mix(P.skyBottom, P.window, 0.35f), 0.7f);
        p.WallWithArches(-8.5f, -2.6f, 8.5f, 4.6f, new[] { 2.4f }, 5.8f, 7.0f, P.shade, 6.6f);
        p.Clock(2.4f, 1.6f, 2.4f, P.ink, P.light, 6.4f, 335f, 110f);
        p.FogBand(-1.4f, 2.0f, P.haze, 0.4f, 6f);
        // the signal gantry across the throat and its lamps
        p.Box(-8.5f, 2.3f, 8.5f, 2.55f, P.ink, 4.5f);
        p.Line(-6.0f, -2.6f, -6.0f, 2.3f, 0.07f, P.ink, 4.5f);
        p.Line(6.5f, -2.6f, 6.5f, 2.3f, 0.07f, P.ink, 4.5f);
        foreach (var (x, red) in new[] { (-2.6f, true), (0.2f, false), (4.6f, true) })
        {
            p.Box(x - 0.12f, 1.75f, x + 0.12f, 2.3f, P.ink, 4.45f);
            p.Lamp(x, 1.9f, 0.08f, red ? PosterLook.SignalRed : P.window, 4.4f, 5f, 1.5f);
        }
        // the platforms converging in forced perspective
        float vx = 1.0f, vy = -1.2f;
        float[] xb = { -13f, -6.5f, -1.5f, 3.2f, 8.5f, 14f };
        for (int k = 0; k < 5; k++)
        {
            float off0 = (k - 2) * 0.12f, off1 = (k - 1) * 0.12f;
            p.Poly(k % 2 == 0 ? P.shade : P.ink, 3.5f, false, new Vector2(xb[k], -4.6f), new Vector2(vx + off0, vy), new Vector2(vx + off1, vy), new Vector2(xb[k + 1], -4.6f));
            if (k % 2 != 0) continue;
            p.Line(xb[k], -4.6f, vx + off0, vy, 0.03f, P.light, 3.45f);
            p.Line(xb[k + 1], -4.6f, vx + off1, vy, 0.03f, P.light, 3.45f);
        }
        // the locomotive at the left third: its headlamp and beam, its smoke past the clock
        p.Box(-7.2f, -3.0f, -3.4f, -0.3f, P.ink, 2.5f);
        p.Disc(-5.3f, -1.5f, 1.2f, P.shade, 2.45f);
        p.Ring(-5.3f, -1.5f, 1.2f, 1.2f, 0.08f, P.ink, 2.44f);
        p.Box(-5.55f, -0.3f, -5.05f, 0.5f, P.ink, 2.5f);
        p.Lamp(-5.3f, -0.9f, 0.2f, P.lamp, 2.4f, 7f, 1.6f);
        p.Beam(-5.3f, -0.9f, -18f, 9f, 0.5f, P.lamp, 0.33f, 2.35f, 3.5f);
        p.Smoke(-5.3f, 0.5f, 4.6f, 1.5f, P.light, 0.45f, 2.3f);
        p.Disc(-6.6f, -2.8f, 0.18f, P.shade, 2.4f);
        p.Disc(-4.0f, -2.8f, 0.18f, P.shade, 2.4f);
        // the last carriage out, its red tail lamp
        p.Box(5.0f, -2.6f, 7.4f, 0.1f, P.ink, 3f);
        p.Box(5.3f, -1.4f, 7.1f, -0.5f, PosterKit.Mix(P.window, P.ink, 0.6f), 2.95f, true);
        p.Lamp(6.2f, -0.2f, 0.12f, PosterLook.SignalRed, 2.9f, 6f, 1.6f);
        p.Figure(1.9f, -3.4f, 0.9f, P.ink, 1.5f, FigureKind.Runner, -1);
    }
}
