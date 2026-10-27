using UnityEngine;
using CrownKind = PosterKit.CrownKind;
using FigureKind = PosterKit.FigureKind;

/// <summary>
/// ACT III's posters: the surface at dawn, the rooftops in the rain, the House in worm's-eye. One hue each
/// (PosterLook), the landmark on a third, searchlights, a tiny figure.
/// </summary>
public static class PosterActIII
{
    /// THE BREACH · ABOVE THE TERMINUS — sepia dawn: the steam column rising from the open shaft, the fallen clock
    /// tower lying across the street, the viaduct behind, the sun low in its arches, a bellhop in the ash
    public static void Breach(PosterKit p)
    {
        p.Use(PosterLook.Sepia);
        var P = p.pal;
        p.Sky(P.skyTop, PosterKit.Glow(P.skyBottom, 1.15f), 0.5f);
        p.CloudBar(-4.5f, 3.4f, 6f, 0.4f, P.shade, 8.6f);
        p.Line(-7.5f, 3.2f, -1.5f, 3.2f, 0.04f, P.light, 8.55f, true);
        p.CloudBar(3.5f, 2.6f, 5f, 0.3f, P.shade, 8.6f);
        p.Line(1.0f, 2.45f, 6.0f, 2.45f, 0.03f, P.light, 8.55f, true);
        float[] fx = { -6.4f, -1.8f, 6.2f }, fw = { 2.6f, 1.8f, 2.4f }, fh = { 3.8f, 5.2f, 4.4f };
        for (int i = 0; i < 3; i++)
        {
            var m = p.Mass(fx[i], -1.4f, fw[i], fh[i], 3, P.mid, 8f);
            p.Windows(m, 0.3f, P.window, 111 + i);
        }
        p.Lamp(4.2f, -0.5f, 0.55f, P.lamp, 7.8f, 6f, 1.25f);
        p.FogBand(-0.6f, 2.2f, P.haze, 0.45f, 7.2f);
        p.Viaduct(-8.5f, -1.5f, 17f, 6, 2.1f, 0.5f, P.shade, 6.5f);
        p.Rail(1.17f, -8.5f, 8.5f, P.shade, 6.5f, 0.5f);
        // the street: the open shaft at the right third and its steam, the clock tower lying across it
        p.Ground(-1.5f, P.mid, 5.5f);
        p.Poly(P.ink, 2.6f, false, new Vector2(3.4f, -3.3f), new Vector2(3.9f, -2.7f), new Vector2(7.2f, -2.7f), new Vector2(7.6f, -3.3f));
        p.Box(3.6f, -2.7f, 7.4f, -2.55f, P.light, 2.6f);
        p.Smoke(5.5f, -2.8f, 8.5f, 2.6f, P.light, 0.55f, 2.4f, 11);
        p.Smoke(5.2f, -2.6f, 6.0f, 1.4f, P.lamp, 0.3f, 2.35f, 8);
        var t = new Vector2(-1.2f, -2.1f);
        float ca = Mathf.Cos(-7f * Mathf.Deg2Rad), sa = Mathf.Sin(-7f * Mathf.Deg2Rad);
        Vector2 R(float ux, float uy) => t + new Vector2(ux * ca - uy * sa, ux * sa + uy * ca);
        p.Poly(P.shade, 3f, false, R(-4.8f, -0.7f), R(-4.8f, 0.7f), R(4.2f, 0.75f), R(4.5f, 0.2f), R(4.1f, -0.3f), R(4.6f, -0.7f));
        for (int k = -1; k <= 1; k++) p.Line(R(-4.8f, k * 0.42f).x, R(-4.8f, k * 0.42f).y, R(4.2f, k * 0.42f).x, R(4.2f, k * 0.42f).y, 0.03f, P.mid, 2.95f);
        p.Clock(R(-4.1f, 0f).x, R(-4.1f, 0f).y, 0.55f, P.ink, P.light, 2.9f, 250f, 40f);
        // the rubble, the torn track, the bellhop in the ash
        p.Ground(-3.3f, P.shade, 2f);
        p.Poly(P.ink, 1.9f, false, new Vector2(-8.5f, -3.3f), new Vector2(-6.8f, -2.5f), new Vector2(-5.2f, -3.3f));
        p.Poly(P.ink, 1.9f, false, new Vector2(-3.0f, -3.3f), new Vector2(-1.6f, -2.8f), new Vector2(0.4f, -3.3f));
        p.Line(-8.5f, -3.5f, 2.0f, -3.9f, 0.05f, P.ink, 1.8f);
        p.Figure(-6.6f, -2.5f, 0.95f, P.ink, 1f, FigureKind.Bellhop, 1);
    }

    /// SKYLINE · THE ROOFTOPS — violet night: a roofscape of water towers, parapets and a gothic spire, the police
    /// blimp with its searchlight, rain as thin diagonals, orange windows, searchlight fans from the far crowns
    public static void Rooftops(PosterKit p)
    {
        p.Use(PosterLook.Violet);
        var P = p.pal;
        p.Sky(P.skyTop, P.skyBottom, 0.65f);
        p.Stars(30, 1.5f, 0.8f);
        p.Moon(-5.6f, 3.1f, 0.65f, P.moon, 0.9f);
        float[] fx = { -7.2f, -4.6f, -2.0f, 0.8f, 3.2f, 5.6f, 7.6f }, fw = { 1.8f, 1.4f, 2.0f, 1.6f, 2.2f, 1.4f, 1.9f }, fh = { 4.0f, 6.4f, 3.4f, 7.6f, 4.6f, 5.4f, 3.6f };
        CrownKind[] ck = { CrownKind.None, CrownKind.Spire, CrownKind.WaterTower, CrownKind.Sunburst, CrownKind.None, CrownKind.Ziggurat, CrownKind.None };
        for (int i = 0; i < 7; i++)
        {
            var m = p.Mass(fx[i], -1.6f, fw[i], fh[i], 4, P.shade, 8f);
            p.Windows(m, 0.65f, P.window, 121 + i);
            p.Crown(m, ck[i], P.shade);
        }
        foreach (float a in new[] { 68f, 88f, 108f }) p.Beam(0.8f, 5.6f, a, 5f, 0.2f, P.beam, 0.3f, 7.6f, 3f);
        foreach (float a in new[] { 62f, 84f }) p.Beam(-4.6f, 4.2f, a, 5f, 0.18f, P.beam, 0.26f, 7.6f, 3f);
        p.FogBand(-0.2f, 2.6f, PosterKit.Mix(P.haze, P.light, 0.3f), 0.6f, 6.6f);
        // the mid roofs: low blocks with parapets and water towers; the gothic spire at the left third
        float[] mx = { -6.4f, -1.0f, 3.4f, 7.2f }, mw = { 3.0f, 3.4f, 2.8f, 2.6f }, mh = { 2.2f, 1.6f, 2.6f, 1.8f };
        for (int i = 0; i < 4; i++)
        {
            var m = p.Mass(mx[i], -2.4f, mw[i], mh[i], 1, P.ink, 5f);
            p.Windows(m, 0.7f, P.window, 131 + i, 0.3f, 0.27f);
            p.Rail(m.TopY, mx[i] - mw[i] * 0.5f, mx[i] + mw[i] * 0.5f, P.ink, 5f, 0.3f);
            if (i == 1 || i == 3) p.WaterTower(mx[i] + 0.8f, m.TopY, 1.1f, P.ink, 4.9f);
        }
        var spire = p.Mass(-3.6f, -2.6f, 2.6f, 5.4f, 2, P.ink, 3.6f);
        p.Crown(spire, CrownKind.Gothic, P.ink);
        foreach (float wx in new[] { -4.2f, -3.6f, -3.0f })
        {
            p.Box(wx - 0.14f, 0.2f, wx + 0.14f, 1.4f, PosterKit.Glow(P.window, 1.2f), 3.55f, true);
            p.Line(wx, 0.2f, wx, 1.4f, 0.03f, P.ink, 3.5f);
        }
        p.Windows(spire, 0.5f, P.window, 135, 0.32f, 0.28f);
        // the police blimp and its searchlight on the near roof
        p.Blimp(4.8f, 3.1f, 3.6f, P.shade, 4f);
        p.Beam(4.8f, 2.3f, -78f, 5.6f, 0.25f, P.beam, 0.42f, 3.9f, 5f);
        p.Tex(2.2f, -3.9f, 5.2f, -2.9f, PosterLook.HaloTex, PosterKit.With(P.beam, 0.3f), 1.85f, PosterKit.Blend.Add);
        // the near roof: parapet, stacks, a water tower, a lit stair head, the runner; the rain over all
        p.Ground(-3.4f, P.ink, 2f);
        p.Rail(-3.4f, -8.5f, 8.5f, P.shade, 1.9f, 0.5f);
        p.Box(-2.2f, -3.4f, -1.6f, -2.5f, P.ink, 1.8f);
        p.Box(-1.5f, -3.4f, -1.1f, -2.7f, P.ink, 1.8f);
        p.WaterTower(-6.8f, -3.4f, 1.7f, P.ink, 1.8f);
        p.Box(5.8f, -3.4f, 7.2f, -2.2f, P.ink, 1.8f);
        p.Box(6.2f, -3.1f, 6.8f, -2.3f, PosterKit.Glow(P.window, 1.1f), 1.75f, true);
        p.Figure(2.9f, -3.22f, 1.0f, P.ink, 1.2f, FigureKind.Runner, 1);
        p.Rain(170, 70f, P.light, 0.22f);
    }

    /// CROSSTOWN · THE BLOCKS — black and steel: the House's tower in worm's-eye with its fluting and one lit face,
    /// searchlight fans from its crown, the mark on it, the flanking towers leaning in, stars, a figure on a ledge
    public static void Finale(PosterKit p)
    {
        p.Use(PosterLook.BlackSteel);
        var P = p.pal;
        p.Sky(P.skyTop, P.skyBottom, 0.7f);
        p.Stars(90, -1.0f, 1.0f);
        var far = p.Mass(-2.6f, -4.6f, 2.4f, 8.2f, 4, P.mid, 8f, 3f);
        p.Windows(far, 0.3f, P.window, 141);
        var far2 = p.Mass(4.2f, -4.6f, 2.0f, 7.0f, 4, P.mid, 8f, -3f);
        p.Windows(far2, 0.3f, P.window, 142);
        p.FogBand(-2.4f, 3.0f, P.haze, 0.4f, 7f);
        // the House: fluted, one lit face, its crown throwing searchlights, the mark on the crown
        foreach (float a in new[] { 48f, 66f, 84f, 102f, 120f, 138f }) p.Beam(0.4f, 3.6f, a, 6.5f, 0.22f, P.beam, 0.38f, 4.5f, 3.2f);
        var house = p.Mass(0.4f, -4.6f, 7.2f, 8.4f, 4, P.shade, 4f, 0f, 0.3f);
        p.Lit(house, -1, 0.3f, P.mid);
        p.Fluting(house, PosterKit.Mix(P.shade, P.light, 0.3f), 0.17f, 0.05f);
        p.Windows(house, 0.3f, P.window, 143, 0.27f, 0.21f);
        p.Crown(house, CrownKind.Sunburst, P.light);
        p.Disc(0.4f, house.TopY - 0.55f, 0.3f, PosterLook.Brass, 3.9f, true);
        p.Chevron(0.4f, house.TopY - 0.57f, 0.3f, 0.18f, 0.05f, P.ink, 3.85f);
        // the flanking towers leaning in, a bridge thrown to the right one
        var l = p.Mass(-6.4f, -4.6f, 3.8f, 7.2f, 3, P.ink, 5.5f, 6f);
        p.Windows(l, 0.25f, P.window, 144, 0.26f, 0.21f);
        p.Crown(l, CrownKind.Ziggurat, P.ink);
        var r = p.Mass(6.9f, -4.6f, 3.4f, 6.4f, 3, P.ink, 5.5f, -6f);
        p.Windows(r, 0.25f, P.window, 145, 0.26f, 0.21f);
        p.Crown(r, CrownKind.Gothic, P.ink);
        p.Bridge(3.3f, 4.8f, -0.8f, P.ink, 5.2f);
        // the ledge at the foot and the figure on it, looking up
        p.Box(-8.5f, -4.6f, -4.2f, -3.3f, P.ink, 1.2f);
        p.Rail(-3.3f, -8.5f, -4.2f, P.shade, 1.15f, 0.4f);
        p.Box(-4.5f, -3.3f, -4.2f, -3.0f, P.ink, 1.15f);
        p.Figure(-5.6f, -3.15f, 0.75f, P.ink, 1f, FigureKind.Runner, 1);
    }
}
