using UnityEngine;
using CrownKind = PosterKit.CrownKind;
using FigureKind = PosterKit.FigureKind;

/// <summary>
/// The posters' dispatcher: a card's composition (LevelCatalog.PosterInfo) to the function that draws it
/// </summary>
public static class Posters
{
    public static void Build(LevelCatalog.PosterKind kind, PosterKit p)
    {
        switch (kind)
        {
            case LevelCatalog.PosterKind.Sanatorium: PosterActI.Sanatorium(p); break;
            case LevelCatalog.PosterKind.TheatreDistrict: PosterActI.TheatreDistrict(p); break;
            case LevelCatalog.PosterKind.BelowStairs: PosterActI.BelowStairs(p); break;
            case LevelCatalog.PosterKind.GrandHotels: PosterActI.GrandHotels(p); break;
            case LevelCatalog.PosterKind.HalcyonHouse: PosterActI.HalcyonHouse(p); break;
            case LevelCatalog.PosterKind.Metro: PosterActII.Metro(p); break;
            case LevelCatalog.PosterKind.Tunnels: PosterActII.Tunnels(p); break;
            case LevelCatalog.PosterKind.Sewers: PosterActII.Sewers(p); break;
            case LevelCatalog.PosterKind.Foundations: PosterActII.Foundations(p); break;
            case LevelCatalog.PosterKind.Terminus: PosterActII.Terminus(p); break;
            case LevelCatalog.PosterKind.Breach: PosterActIII.Breach(p); break;
            case LevelCatalog.PosterKind.Rooftops: PosterActIII.Rooftops(p); break;
            default: PosterActIII.Finale(p); break;
        }
    }
}

/// <summary>
/// ACT I's posters: the city by day and by night. Each is one dominant hue from PosterLook, the landmark on a third,
/// three to five layers receding into the haze, one light source, a tiny figure. No text: the card carries the title.
/// </summary>
public static class PosterActI
{
    /// FIRST FLIGHT · THE SANATORIUM — sepia dawn: the gate pylons and the Hydro's marquee in the mid layer, a lit tram
    /// crossing, the forecourt fountain, a bellhop on the steps
    public static void Sanatorium(PosterKit p)
    {
        p.Use(PosterLook.Sepia);
        var P = p.pal;
        p.Sky(P.skyTop, P.skyBottom, 0.55f);
        p.CloudBar(-3.5f, 3.3f, 7f, 0.45f, P.shade, 8.6f);
        p.CloudBar(4.8f, 2.5f, 4.5f, 0.32f, P.shade, 8.6f);
        p.Lamp(2.4f, -0.3f, 0.6f, P.lamp, 8.7f, 5f, 1.2f);   // the dawn sun, low behind the far city
        float[] fx = { -6.6f, -3.2f, 0.2f, 3.4f, 6.6f }, fw = { 2.4f, 1.7f, 2.0f, 1.5f, 2.6f }, fh = { 4.2f, 5.4f, 3.6f, 6.4f, 3.2f };
        for (int i = 0; i < 5; i++)
        {
            var m = p.Mass(fx[i], -1.2f, fw[i], fh[i], 3, P.mid, 8f);
            p.Windows(m, 0.35f, P.window, 11 + i);
            if (i == 3) p.Crown(m, CrownKind.Spire, P.mid);
        }
        p.FogBand(-0.4f, 2.4f, P.haze, 0.5f, 7.5f);
        // the Hydro right of centre with its marquee; the Sanatorium's gate pylons framing the left third
        var hydro = p.Mass(2.8f, -1.8f, 5.4f, 4.6f, 3, P.shade, 6f);
        p.Lit(hydro, -1, 0.32f, P.mid);
        p.Fluting(hydro, PosterKit.Mix(P.shade, P.mid, 0.5f), 0.14f, 0.04f);
        p.Windows(hydro, 0.5f, P.window, 7, 0.28f, 0.24f);
        p.Crown(hydro, CrownKind.Ziggurat, P.shade);
        p.Marquee(0.5f, -1.0f, 3.6f, 0.34f, P.light, P.window, 5.6f);
        p.Pylon(-5.8f, -1.9f, 1.4f, 3.8f, P.shade, 5f, P.mid);
        p.Pylon(-2.4f, -1.9f, 1.4f, 3.8f, P.shade, 5f, P.mid);
        p.Lamp(-5.8f, 2.05f, 0.09f, P.lamp, 4.9f, 5f);
        p.Lamp(-2.4f, 2.05f, 0.09f, P.lamp, 4.9f, 5f);
        p.Rail(-1.5f, -5.2f, -3.0f, P.shade, 5f, 0.25f);
        // the forecourt and its fountain
        p.Ground(-1.9f, P.shade, 4.5f);
        p.Box(-1.9f, -2.5f, 1.7f, -2.15f, P.mid, 3.5f);
        p.Line(-0.1f, -2.15f, -0.1f, -0.7f, 0.14f, P.shade, 3.5f);
        p.Disc(-0.1f, -0.6f, 0.2f, P.light, 3.5f);
        p.Line(-0.1f, -0.75f, -0.95f, -2.1f, 0.03f, P.light, 3.4f, true);
        p.Line(-0.1f, -0.75f, 0.75f, -2.1f, 0.03f, P.light, 3.4f, true);
        p.Tram(3.0f, -3.15f, 3.8f, P.ink, P.window, 2.6f, true);
        // the steps, the lamp standard and the bellhop
        p.Ground(-3.15f, P.ink, 2f);
        p.Stair(-7.4f, -3.15f, 3, 0.55f, 0.13f, P.shade, 1.6f, 1);
        p.Line(-7.6f, -3.15f, -7.6f, -1.1f, 0.05f, P.ink, 1.3f);
        p.Lamp(-7.6f, -1.0f, 0.1f, P.lamp, 1.3f, 5f);
        p.Figure(-5.95f, -2.76f, 1.0f, P.ink, 1f, FigureKind.Bellhop, 1);
    }

    /// CURTAIN UP · THE THEATRE DISTRICT — violet night: the elevated's deck across the frame on riveted piers with
    /// its station canopy and a parked carriage, a marquee of bulbs under it, the Regent's blade sign, a moon
    public static void TheatreDistrict(PosterKit p)
    {
        p.Use(PosterLook.Violet);
        var P = p.pal;
        p.Sky(P.skyTop, P.skyBottom, 0.7f);
        p.Stars(40, 0.8f);
        p.Moon(-4.8f, 3.0f, 0.75f, P.moon, 0.9f);
        float[] fx = { -6.8f, -2.4f, 1.0f, 4.4f, 7.2f }, fw = { 2.2f, 1.8f, 2.4f, 1.6f, 2.0f }, fh = { 5.0f, 6.6f, 4.2f, 7.2f, 5.4f };
        for (int i = 0; i < 5; i++)
        {
            var m = p.Mass(fx[i], -1.0f, fw[i], fh[i], 4, P.shade, 8f);
            p.Windows(m, 0.6f, P.window, 21 + i);
            p.Crown(m, i == 3 ? CrownKind.Sunburst : i == 1 ? CrownKind.Spire : CrownKind.None, P.shade);
        }
        p.Beam(-1.6f, 0.8f, 72f, 7f, 0.3f, P.beam, 0.3f, 7.6f, 3f);
        p.FogBand(-0.2f, 2.2f, P.haze, 0.5f, 7.2f);
        // the elevated: the deck on its piers, the rivets, the canopy and the carriage
        for (float px = -6.2f; px < 8f; px += 4.1f) p.Box(px - 0.3f, -2.6f, px + 0.3f, 0.9f, P.ink, 5.1f);
        p.Box(-8.5f, 0.9f, 8.5f, 1.5f, P.ink, 5f);
        p.Bulbs(-8.3f, 1.2f, 8.3f, 1.2f, 46, 0.022f, P.shade, 5f);
        p.Rail(1.5f, -8.5f, 8.5f, P.ink, 5f, 0.5f);
        p.Tram(1.6f, 1.52f, 3.4f, P.ink, P.window, 4.9f, false, false);
        for (float px = 2.0f; px <= 6.0f; px += 2.0f) p.Line(px, 1.5f, px, 2.75f, 0.05f, P.ink, 4.9f);
        p.Box(1.5f, 2.7f, 6.6f, 2.9f, P.ink, 4.9f);
        // the Regent: its marquee under the deck, its blade sign climbing past it
        p.Marquee(-2.0f, -0.4f, 3.4f, 0.3f, P.light, P.window, 4f);
        p.Line(5.5f, 2.5f, 5.95f, 2.5f, 0.04f, P.ink, 3.5f);
        p.Line(5.5f, -1.0f, 5.95f, -1.0f, 0.04f, P.ink, 3.5f);
        p.Box(4.9f, -1.3f, 5.5f, 2.8f, P.shade, 3.5f);
        p.Box(5.0f, -1.1f, 5.4f, 2.6f, PosterKit.Mix(P.accent, P.ink, 0.45f), 3.5f, true);
        p.Bulbs(4.93f, -1.2f, 4.93f, 2.7f, 18, 0.025f, P.window, 3.5f);
        p.Bulbs(5.47f, -1.2f, 5.47f, 2.7f, 18, 0.025f, P.window, 3.5f);
        var l = p.Mass(-6.6f, -2.6f, 3.6f, 4.6f, 2, P.ink, 3f);
        p.Windows(l, 0.45f, P.window, 31, 0.3f, 0.26f);
        var r = p.Mass(7.4f, -2.6f, 3.0f, 5.2f, 2, P.ink, 3f);
        p.Windows(r, 0.45f, P.window, 32, 0.3f, 0.26f);
        // the street: the marquee's pool, a lamp standard, the runner
        p.Ground(-2.6f, P.ink, 2f);
        p.LightPool(-0.3f, -2.6f, -3.6f, 3.4f, 5.2f, P.window, 0.14f, 1.9f);
        p.Line(3.2f, -2.6f, 3.2f, -0.6f, 0.05f, P.ink, 1.4f);
        p.Lamp(3.2f, -0.5f, 0.09f, P.lamp, 1.4f, 5f);
        p.Figure(-3.6f, -2.6f, 0.95f, P.ink, 1f, FigureKind.Runner, 1);
    }

    /// BELOW STAIRS · THROUGH THE CITY — steel blue: an alley canyon, fire-escape zigzags up both walls, washing
    /// lines, one lamp, steam from a grate, the sky a thin slot at the top
    public static void BelowStairs(PosterKit p)
    {
        p.Use(PosterLook.Steel);
        var P = p.pal;
        p.Sky(P.skyTop, P.skyBottom, 0.8f);
        p.Stars(16, 2.0f);
        var far = p.Mass(0.3f, -2.4f, 2.6f, 5.6f, 3, P.mid, 8f);
        p.Windows(far, 0.4f, P.window, 41);
        p.FogBand(-1.0f, 2.6f, P.haze, 0.5f, 7.6f);
        // the lit doorway at the far end, and someone in its light
        p.Box(-0.55f, -2.4f, 0.35f, -1.0f, PosterKit.Glow(P.window, 1.2f), 7f, true);
        p.Tex(-1.6f, -3.0f, 1.4f, 0.0f, PosterLook.HaloTex, PosterKit.With(P.window, 0.35f), 6.9f, PosterKit.Blend.Add);
        p.Figure(-0.1f, -2.4f, 0.55f, P.ink, 6.5f, FigureKind.Runner, 1);
        p.Ground(-2.4f, P.shade, 5f);
        // the canyon: two walls leaning in, a slot of sky between
        var lw = p.Mass(-5.3f, -2.4f, 6.6f, 7.4f, 1, P.shade, 3f, 5f);
        p.Windows(lw, 0.3f, P.window, 42, 0.5f, 0.46f);
        var rw = p.Mass(5.3f, -2.4f, 6.6f, 7.4f, 1, P.ink, 2.5f, -5f);
        p.Windows(rw, 0.25f, P.window, 43, 0.5f, 0.46f);
        p.FireEscape(-5.6f, -1.5f, 4.3f, 5, 1.5f, P.ink, 2.9f, 1);
        p.FireEscape(3.6f, -1.0f, 4.4f, 5, 1.5f, P.shade, 2.4f, -1);
        p.WashingLine(-1.95f, 1.1f, 1.95f, 1.3f, 4, P.light, P.light, 2.8f);
        p.WashingLine(-1.85f, 2.4f, 1.85f, 2.2f, 3, P.light, P.light, 2.8f);
        p.WashingLine(-1.75f, 3.5f, 1.75f, 3.6f, 4, P.light, P.light, 2.8f);
        // one lamp on the left wall and its pool; steam from a grate on the right; the wet setts
        p.Line(-2.6f, 0.5f, -2.1f, 0.5f, 0.04f, P.ink, 2.85f);
        p.Lamp(-2.1f, 0.45f, 0.1f, P.lamp, 2.85f, 6f);
        p.Beam(-2.1f, 0.4f, -75f, 3.2f, 0.3f, P.beam, 0.22f, 2.84f, 4f);
        p.LightPool(-1.2f, -3.4f, -2.4f, 3.2f, 1.6f, P.lamp, 0.16f, 2.2f);
        p.Water(-2.4f, 2.2f, P.ink, P.light, 2.3f, 10);
        p.Box(-4.9f, -2.4f, -3.3f, -1.65f, P.ink, 2.2f);
        p.Line(-4.95f, -1.65f, -3.25f, -1.65f, 0.05f, P.shade, 2.2f);
        p.Box(2.6f, -2.5f, 3.6f, -2.38f, P.ink, 2.0f);
        p.Smoke(3.1f, -2.4f, 2.6f, 1.0f, P.light, 0.3f, 1.8f);
    }

    /// THE GRAND HOTELS · THE ASTOR & THE MAJESTIC — sepia gold on black: two fluted towers with the glass skybridge
    /// between them, searchlight fans, the porte-cochère's lamps, the couple at the terrace rail
    public static void GrandHotels(PosterKit p)
    {
        p.Use(PosterLook.SepiaGold);
        var P = p.pal;
        p.Sky(P.skyTop, P.skyBottom, 0.5f);
        p.Stars(70, -0.5f, 0.9f);
        float[] fx = { -7.0f, 0.2f, 6.8f }, fw = { 2.0f, 1.6f, 2.2f }, fh = { 4.6f, 6.2f, 4.0f };
        for (int i = 0; i < 3; i++)
        {
            var m = p.Mass(fx[i], -2.6f, fw[i], fh[i], 3, P.mid, 8f);
            p.Windows(m, 0.3f, P.window, 51 + i);
            p.Crown(m, i == 1 ? CrownKind.Spire : CrownKind.None, P.mid);
        }
        foreach (float a in new[] { 62f, 80f, 98f, 116f }) p.Beam(-3.4f, 3.9f, a, 6.5f, 0.2f, P.beam, 0.32f, 7.5f, 3.5f);
        foreach (float a in new[] { 70f, 92f, 114f }) p.Beam(3.6f, 3.1f, a, 6f, 0.2f, P.beam, 0.28f, 7.5f, 3.5f);
        p.FogBand(-2.2f, 1.8f, P.haze, 0.45f, 5.5f);
        // the Astor, pale and fluted; the Majestic, gold-lit; the glass skybridge between
        var astor = p.Mass(-3.4f, -3.4f, 3.0f, 7.4f, 4, P.mid, 4.5f);
        p.Lit(astor, 1, 0.34f, P.light);
        p.Fluting(astor, PosterKit.Mix(P.mid, P.light, 0.35f), 0.11f, 0.03f);
        p.Windows(astor, 0.3f, P.window, 54, 0.22f, 0.19f);
        p.Crown(astor, CrownKind.Spire, P.light);
        var maj = p.Mass(3.6f, -3.4f, 3.2f, 6.6f, 3, P.shade, 4.6f);
        p.Lit(maj, -1, 0.3f, PosterKit.Mix(P.light, P.window, 0.5f));
        p.Windows(maj, 0.5f, P.window, 55, 0.22f, 0.19f);
        p.Crown(maj, CrownKind.Sunburst, P.accent);
        p.Box(-2.0f, 0.3f, 2.1f, 0.78f, PosterKit.Glow(P.window, 1.1f), 4.4f, true);
        for (float mx = -1.6f; mx < 2.1f; mx += 0.45f) p.Line(mx, 0.3f, mx, 0.78f, 0.03f, P.ink, 4.3f);
        p.Box(-2.0f, 0.78f, 2.1f, 0.88f, P.ink, 4.3f);
        p.Box(-2.0f, 0.2f, 2.1f, 0.3f, P.ink, 4.3f);
        p.LightPool(0.05f, 0.2f, -1.2f, 4.1f, 5.0f, P.window, 0.12f, 4.35f);
        // the porte-cochère at the Astor's foot: a canopy and its three lamps
        p.Box(-5.3f, -2.3f, -1.5f, -2.12f, P.shade, 3f);
        p.Line(-5.1f, -3.4f, -5.1f, -2.3f, 0.06f, P.shade, 3f);
        p.Line(-1.7f, -3.4f, -1.7f, -2.3f, 0.06f, P.shade, 3f);
        for (float lx = -4.6f; lx < -1.8f; lx += 1.2f) p.Lamp(lx, -2.42f, 0.07f, P.lamp, 2.9f, 6f);
        // the terrace: the rail, the planter and the couple
        p.Ground(-3.4f, P.ink, 2f);
        p.Rail(-3.05f, -8.5f, 8.5f, P.ink, 1.5f, 0.7f);
        p.Box(-4.9f, -3.4f, -4.1f, -2.75f, P.ink, 1.2f);
        p.Figure(-5.6f, -3.4f, 1.05f, P.ink, 1f, FigureKind.Couple, 1);
    }

    /// CHECKING IN · HALCYON HOUSE — cobalt: the twin pylons framing the mark (a brass disc with a chevron), the
    /// arcade's lantern glowing, chandelier bursts, a chequer floor in perspective, a doorman with his lantern
    public static void HalcyonHouse(PosterKit p)
    {
        p.Use(PosterLook.Cobalt);
        var P = p.pal;
        p.Sky(P.skyTop, P.skyBottom, 0.6f);
        p.Stars(50, 0.6f);
        var spire = p.Mass(0.5f, -1.4f, 1.3f, 4.6f, 4, P.mid, 8f);
        p.Windows(spire, 0.4f, P.window, 61);
        p.Crown(spire, CrownKind.Spire, P.mid);
        var nb = p.Mass(-1.6f, -1.4f, 1.6f, 2.6f, 2, P.mid, 8.2f);
        p.Windows(nb, 0.4f, P.window, 62);
        var nb2 = p.Mass(2.2f, -1.4f, 1.4f, 2.2f, 2, P.mid, 8.2f);
        p.Windows(nb2, 0.4f, P.window, 63);
        p.FogBand(-0.8f, 2.0f, P.haze, 0.4f, 7.5f);
        // the arcade's great arch: the wall with its opening, the ring round it, the flutes either side
        p.WallWithArches(-8.5f, -2.6f, 8.5f, 4.6f, new[] { 0f }, 5.4f, 6.4f, P.shade, 6.6f);
        p.Arch(0f, -2.6f, 5.4f, 6.4f, 0.4f, P.mid, 6.5f);
        for (float fx = -8.2f; fx < -3.0f; fx += 0.5f) p.Line(fx, -2.6f, fx, 4.6f, 0.05f, P.mid, 6.55f);
        for (float fx = 3.2f; fx < 8.5f; fx += 0.5f) p.Line(fx, -2.6f, fx, 4.6f, 0.05f, P.mid, 6.55f);
        // the twin pylons, the mark between them, the lantern, the chandeliers
        p.Pylon(-3.8f, -2.6f, 1.7f, 4.4f, P.mid, 4.5f, P.light);
        p.Pylon(3.8f, -2.6f, 1.7f, 4.4f, P.mid, 4.5f, P.light);
        p.Disc(0f, 2.5f, 0.72f, PosterLook.Brass, 4f, true);
        p.Chevron(0f, 2.45f, 0.7f, 0.42f, 0.12f, P.ink, 3.95f);
        p.Tex(-2.2f, -1.6f, 2.2f, 2.0f, PosterLook.HaloTex, PosterKit.With(P.window, 0.4f), 4.3f, PosterKit.Blend.Add);
        p.Box(-0.95f, -0.5f, 0.95f, 1.35f, PosterKit.Glow(P.window, 1.25f), 4.2f, true);
        for (float mx = -0.6f; mx < 0.95f; mx += 0.32f) p.Line(mx, -0.5f, mx, 1.35f, 0.03f, P.ink, 4.15f);
        p.Line(-0.95f, 0.45f, 0.95f, 0.45f, 0.03f, P.ink, 4.15f);
        p.Line(5.8f, 4.6f, 5.8f, 3.3f, 0.03f, P.light, 3.6f);
        p.Burst(5.8f, 2.9f, 1.15f, 26, 0.05f, PosterKit.Glow(P.window, 1.4f), 3.5f);
        p.Line(-6.4f, 4.6f, -6.4f, 3.4f, 0.03f, P.light, 3.6f);
        p.Burst(-6.4f, 2.8f, 0.75f, 20, 0.04f, PosterKit.Glow(P.window, 1.4f), 3.5f);
        // the chequer floor to the horizon, the lantern's light across it, the doorman
        p.Chequer(-4.6f, -2.6f, 10, P.mid, P.ink, 2.6f, 0f);
        p.LightPool(0f, -4.6f, -2.6f, 7f, 2.2f, P.window, 0.14f, 2.4f);
        p.Figure(5.0f, -3.5f, 1.0f, P.ink, 1f, FigureKind.Doorman, -1);
        p.Lamp(4.68f, -2.98f, 0.06f, P.window, 0.95f, 6f);
    }
}
