using UnityEngine;

/// <summary>
/// THE JAZZ, DARK DECO (2026-10-07): a route tower's body below its roof stops being a plain box. By its seed (or
/// `force`) one of four dressings: FLUTED (full-height fluted shafts up every face with stepped stone capitals and a
/// lit line up every other flute), GARGOYLED (stepped stone gargoyles at the corners and mid-face under the tier
/// line, tall amber lancet windows between them, a brass sunburst on the face that looks at the route), CORBELLED (a
/// stepped band under the roof's edge on three-step brackets and a stepped turret on every corner at roof height,
/// both SOLID, since a player can reach them), CORNICED (a deep stepped cornice plate under the tier line with the
/// light along its edge, solid for the same reason). Every solid of the old styles is kept exactly where it was:
/// the band, the turrets and their finials (style 2), the eave plate (style 3). Everything else is a look. Partial
/// of RoofKit; not compiled by the menu harness.
/// </summary>
public static partial class RoofKit
{
    static Material stone, neonA, neonW, neonV;

    static void EnsureJazz()
    {
        if (stone != null) return;
        EnsureDress();
        stone = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        stone.SetColor("_BaseColor", new Color(0.14f, 0.14f, 0.16f));   // charcoal stone
        stone.SetFloat("_Smoothness", 0.35f);
        neonA = Sign(new Color(1.9f, 1.3f, 0.55f));    // window amber
        neonW = Sign(new Color(1.6f, 1.5f, 1.2f));     // lamp cream
        neonV = Sign(new Color(1.1f, 0.85f, 0.45f));   // dim brass, lit
    }

    /// The light a tower's lines are drawn in: gold, amber, cream, brass — all of them warm
    static Material JazzNeon(int seed)
    {
        int k = ((seed % 4) + 4) % 4;
        return k == 0 ? gold : k == 1 ? neonA : k == 2 ? neonW : neonV;
    }

    /// A stepped finial: a cone of stone steps, lit up four ribs
    static void JazzSpire(Vector3 foot, float r, float h, Material rib)
    {
        const int steps = 5;
        float sh = h / steps;
        for (int i = 0; i < steps; i++)
        {
            float rr = r * (1f - (float)i / steps) + 0.15f;
            SurfaceKit.Look(ivory, foot + Vector3.up * (sh * (i + 0.5f)), new Vector3(rr * 2f, sh + 0.02f, rr * 2f));
        }
        for (int k = 0; k < 4; k++)
        {
            float a = (k * 90f + 45f) * Mathf.Deg2Rad;
            Vector3 p = foot + new Vector3(Mathf.Sin(a) * (r + 0.1f), 0.1f, Mathf.Cos(a) * (r + 0.1f)), q = foot + Vector3.up * (h + 0.2f);
            SurfaceKit.Look(rib, (p + q) * 0.5f, new Vector3(0.2f, 0.2f, Vector3.Distance(p, q)), SurfaceKit.SpanRot(p, q));
        }
    }

    /// A stepped gargoyle: a stone abstraction of something crouched at the edge, three steps out from the face and a
    /// bronze head on the last, looking down. `out` points away from the face; looks only.
    static void Gargoyle(Vector3 perch, Vector3 outward, Quaternion rot, float scale)
    {
        float s = scale;
        SurfaceKit.Look(stone, perch + outward * (0.3f * s), new Vector3(1.2f * s, 0.9f * s, 1.1f * s), rot);
        SurfaceKit.Look(stone, perch + outward * (0.8f * s) + Vector3.up * (0.45f * s), new Vector3(0.9f * s, 0.7f * s, 1f * s), rot);
        SurfaceKit.Look(stone, perch + outward * (1.25f * s) + Vector3.up * (0.75f * s), new Vector3(0.6f * s, 0.5f * s, 0.8f * s), rot);
        SurfaceKit.Look(LevelKit.Bronze, perch + outward * (1.6f * s) + Vector3.up * (0.95f * s), new Vector3(0.5f * s, 0.45f * s, 0.6f * s), rot);
        SurfaceKit.Look(LevelKit.Bronze, perch + outward * (0.6f * s) + Vector3.up * (1.05f * s), new Vector3(1.4f * s, 0.12f * s, 0.5f * s), rot);   // the folded wings
    }

    /// The seed a tower's jazz is picked by: from where it stands, so a level's towers differ and stay put
    static int JazzSeed(Vector3 foot) => Mathf.RoundToInt(foot.x * 0.37f + foot.z * 0.11f);

    public static void Jazz(Vector3 foot, float w, float d, float y0, float h, float az, int seed, bool top, int force = -1)
    {
        if (h < 8f || w < 6f || d < 6f) return;
        EnsureJazz();
        int style = force >= 0 ? force : ((seed % 4) + 4) % 4;
        Quaternion rot = Quaternion.Euler(0f, az, 0f);
        Material neon = JazzNeon(seed / 4 + 1);
        const float off = 0.14f;   // outside the dress's slabs
        for (int f = 0; f < 4; f++)
        {
            Vector3 nrm = rot * (f == 0 ? Vector3.back : f == 1 ? Vector3.forward : f == 2 ? Vector3.left : Vector3.right);
            Vector3 along = Vector3.Cross(Vector3.up, nrm);
            bool alongX = f < 2;
            float width = alongX ? w : d, half = alongX ? d * 0.5f : w * 0.5f;
            Quaternion faceRot = Quaternion.LookRotation(nrm, Vector3.up);
            Vector3 face = foot + nrm * (half + off);
            switch (style)
            {
                case 0:   // FLUTED: shafts the full height of the tier, a stepped capital on each, a lit line up every other one
                {
                    int shafts = Mathf.Max(2, Mathf.RoundToInt(width / 4f));
                    float sh = h - 1.6f;
                    for (int i = 0; i < shafts; i++)
                    {
                        Vector3 b = face + along * (width * ((i + 0.5f) / shafts - 0.5f));
                        SurfaceKit.Look(LevelKit.Fluted, b + nrm * 0.2f + Vector3.up * (y0 + 0.6f + sh * 0.5f), new Vector3(0.9f, sh, 0.4f), faceRot);
                        SurfaceKit.Look(LevelKit.Limestone, b + nrm * 0.24f + Vector3.up * (y0 + 0.3f), new Vector3(1.1f, 0.6f, 0.48f), faceRot);                   // the plinth
                        SurfaceKit.Look(LevelKit.Limestone, b + nrm * 0.24f + Vector3.up * (y0 + h - 0.85f), new Vector3(1.1f, 0.3f, 0.48f), faceRot);              // the capital, two steps
                        SurfaceKit.Look(LevelKit.Limestone, b + nrm * 0.3f + Vector3.up * (y0 + h - 0.55f), new Vector3(1.3f, 0.3f, 0.6f), faceRot);
                        if (i % 2 == 1) SurfaceKit.Look(neon, b + nrm * 0.41f + Vector3.up * (y0 + 0.6f + sh * 0.5f), new Vector3(0.12f, sh - 0.6f, 0.04f), faceRot);   // lit fluting
                    }
                    break;
                }
                case 1:   // GARGOYLED: lancets of amber light in pairs, gargoyles perched between them under the tier line, a sunburst on the route face
                {
                    int cols = Mathf.Max(2, Mathf.RoundToInt(width / 5.5f));
                    float lh = h * 0.55f, ly = y0 + h * 0.12f;
                    for (int i = 0; i < cols; i++)
                    {
                        Vector3 b = face + along * (width * ((i + 0.5f) / cols - 0.5f));
                        SurfaceKit.Look(neonA, b - along * 0.9f + Vector3.up * (ly + lh * 0.5f), new Vector3(0.5f, lh, 0.1f), faceRot);
                        SurfaceKit.Look(neonA, b + along * 0.9f + Vector3.up * (ly + lh * 0.5f), new Vector3(0.5f, lh, 0.1f), faceRot);
                        SurfaceKit.Look(stone, b + Vector3.up * (ly + lh * 0.5f), new Vector3(0.3f, lh + 0.4f, 0.3f), faceRot);                         // the mullion between the pair
                        SurfaceKit.Look(LevelKit.Limestone, b + nrm * 0.1f + Vector3.up * (ly + lh + 0.3f), new Vector3(2.6f, 0.3f, 0.4f), faceRot);   // a stepped head over the pair
                        SurfaceKit.Look(LevelKit.Limestone, b + nrm * 0.16f + Vector3.up * (ly + lh + 0.6f), new Vector3(2.2f, 0.3f, 0.5f), faceRot);
                        if (i < cols - 1) Gargoyle(face + along * (width * ((i + 1f) / cols - 0.5f)) + nrm * 0.2f + Vector3.up * (y0 + h - 2.2f), nrm, faceRot, 1f);
                    }
                    if (top && f == 0)   // a brass sunburst on the face that looks at the route
                    {
                        Vector3 c = face + nrm * 0.12f + Vector3.up * (y0 + h * 0.82f);
                        float r = Mathf.Min(width * 0.28f, 6f);
                        SurfaceKit.Look(gold, c, new Vector3(1.2f, 1.2f, 0.2f), faceRot);
                        for (int k = 0; k < 9; k++)
                        {
                            float a = Mathf.Lerp(10f, 170f, k / 8f) * Mathf.Deg2Rad;
                            Vector3 rc = c + along * (Mathf.Cos(a) * r * 0.55f) + Vector3.up * (Mathf.Sin(a) * r * 0.55f);
                            SurfaceKit.Look(gold, rc, new Vector3(r - 1f, 0.22f, 0.12f), faceRot * Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg));
                        }
                    }
                    break;
                }
                case 2:   // CORBELLED: the band under the roof's edge, solid: you can drop onto it; stepped brackets under it
                {
                    // The solid is the old box, kept exactly where it was (its top at y0 + h - 2.1); the looks are built
                    // DOWN from that top, so the surface you land on is the surface you see: the band's body to a foot
                    // under the top, two stone steps out and up to it, the upper one flush with the solid's top, and
                    // the lit lip astride that step's front edge. Nothing sits over the solid for a foot to sink into.
                    Vector3 band = face + nrm * 0.75f + Vector3.up * (y0 + h - 3.2f);
                    SurfaceKit.Solid(band, new Vector3(width + 2.6f, 2.2f, 1.5f), faceRot);
                    SurfaceKit.Look(LevelKit.Limestone, band - Vector3.up * 0.15f, new Vector3(width + 2.6f, 1.9f, 1.5f), faceRot);
                    SurfaceKit.Look(LevelKit.Limestone, band + nrm * 0.025f + Vector3.up * 0.6f, new Vector3(width + 2.9f, 0.5f, 1.55f), faceRot);   // the band's own two steps, each a hand further out
                    SurfaceKit.Look(LevelKit.Limestone, band + nrm * 0.05f + Vector3.up * 0.95f, new Vector3(width + 3.2f, 0.3f, 1.6f), faceRot);    // the top one's top at the solid's top
                    SurfaceKit.Look(neon, band + nrm * 0.85f + Vector3.up * 1.1f, new Vector3(width + 2.6f, 0.12f, 0.12f), faceRot);   // lit along its lip: astride the top step's front edge
                    int corbels = Mathf.Max(2, Mathf.RoundToInt(width / 4f));
                    for (int i = 0; i < corbels; i++)
                    {
                        Vector3 b = face + along * (width * ((i + 0.5f) / corbels - 0.5f));
                        SurfaceKit.Look(stone, b + nrm * 0.25f + Vector3.up * (y0 + h - 6.2f), new Vector3(1f, 1f, 0.5f), faceRot);     // three steps out and up
                        SurfaceKit.Look(stone, b + nrm * 0.55f + Vector3.up * (y0 + h - 5.3f), new Vector3(1f, 1f, 1.1f), faceRot);
                        SurfaceKit.Look(stone, b + nrm * 0.85f + Vector3.up * (y0 + h - 4.4f), new Vector3(1f, 1f, 1.7f), faceRot);
                    }
                    break;
                }
                case 3:   // CORNICED: a stepped cornice plate under the tier line, lit along its edge, solid
                {
                    float reach = Mathf.Min(width * 0.12f, 5f);
                    Vector3 eave = face + nrm * (reach * 0.5f) + Vector3.up * (y0 + h - 1.6f);
                    SurfaceKit.Look(LevelKit.Limestone, eave, new Vector3(width + reach * 2f, 0.7f, reach + 0.3f), faceRot);
                    SurfaceKit.Solid(eave, new Vector3(width + reach * 2f, 0.7f, reach + 0.3f), faceRot);
                    SurfaceKit.Look(LevelKit.Limestone, face + nrm * (reach * 0.33f) + Vector3.up * (y0 + h - 2.2f), new Vector3(width + reach * 1.3f, 0.5f, reach * 0.66f + 0.3f), faceRot);   // the steps under it
                    SurfaceKit.Look(LevelKit.Limestone, face + nrm * (reach * 0.17f) + Vector3.up * (y0 + h - 2.7f), new Vector3(width + reach * 0.6f, 0.5f, reach * 0.33f + 0.3f), faceRot);
                    SurfaceKit.Look(neon, eave + nrm * (reach * 0.5f) - Vector3.up * 0.45f, new Vector3(width + reach * 2f, 0.16f, 0.16f), faceRot);   // lit under its edge
                    SurfaceKit.Look(stone, face + Vector3.up * (y0 + h - 4f), new Vector3(width + 0.4f, 0.5f, 0.6f), faceRot);                     // the beam it rests on
                    int dentils = Mathf.Max(4, Mathf.RoundToInt(width / 1.5f));
                    for (int i = 0; i < dentils; i++)   // a row of dentils under the lowest step
                        SurfaceKit.Look(stone, face + along * (width * ((i + 0.5f) / dentils - 0.5f)) + nrm * 0.2f + Vector3.up * (y0 + h - 3.2f), new Vector3(0.5f, 0.5f, 0.4f), faceRot);
                    break;
                }
            }
        }
        if (style == 2)   // the turrets on the corners, at roof height, solid to stand on, a finial on each
            for (int c = 0; c < 4; c++)
            {
                Vector3 diag = (rot * new Vector3(c % 2 == 0 ? -1f : 1f, 0f, c < 2 ? -1f : 1f)).normalized;
                Vector3 corner = foot + rot * new Vector3((c % 2 == 0 ? -1f : 1f) * (w * 0.5f), 0f, (c < 2 ? -1f : 1f) * (d * 0.5f));
                Vector3 centre = corner + diag * 2.6f;
                float bh = Mathf.Min(h * 0.45f, 24f);
                // The solid is the old box, kept where it was: its top, y0 + h + 0.3, is the floor you stand on. So the
                // stone body stops half a metre under that and the limestone cap fills the half metre, flush with the
                // solid's top; the lit lip is four bars astride the cap's top edge (never a sheet over the floor); and
                // the finial reaches down to the cap, its foot solid too (a post you would otherwise walk into)
                float capY = y0 + h + 0.3f;
                SurfaceKit.Look(stone, centre + Vector3.up * (capY - 0.5f - (bh - 0.5f) * 0.5f), new Vector3(5f, bh - 0.5f, 5f), rot);
                SurfaceKit.Look(LevelKit.Fluted, centre + Vector3.up * (y0 + h - bh * 0.5f + 0.3f), new Vector3(4.4f, bh - 1f, 5.3f), rot);   // fluted on two faces
                SurfaceKit.Look(LevelKit.Fluted, centre + Vector3.up * (y0 + h - bh * 0.5f + 0.3f), new Vector3(5.3f, bh - 1f, 4.4f), rot);
                SurfaceKit.Solid(centre + Vector3.up * (y0 + h - bh * 0.5f + 0.3f), new Vector3(4.2f, bh, 4.2f), rot);
                SurfaceKit.Look(LevelKit.Limestone, centre + Vector3.up * (capY - 0.25f), new Vector3(5.8f, 0.5f, 5.8f), rot);
                for (int e = 0; e < 4; e++)
                {
                    Vector3 edge = rot * (e == 0 ? Vector3.forward : e == 1 ? Vector3.back : e == 2 ? Vector3.left : Vector3.right) * 2.9f;
                    SurfaceKit.Look(neon, centre + edge + Vector3.up * (capY + 0.06f), e < 2 ? new Vector3(5.9f, 0.12f, 0.12f) : new Vector3(0.12f, 0.12f, 5.68f), rot);
                }
                SurfaceKit.Look(LevelKit.Limestone, centre + diag * 1.6f + Vector3.up * (y0 + h + 3.35f), new Vector3(0.5f, 6.1f, 0.5f), rot);   // the finial, at the far edge, down to the cap
                SurfaceKit.Solid(centre + diag * 1.6f + Vector3.up * (y0 + h + 3.6f), new Vector3(0.5f, 5.6f, 0.5f), rot);
                SurfaceKit.Solid(centre + diag * 1.6f + Vector3.up * (y0 + h + 0.55f), new Vector3(0.5f, 0.5f, 0.5f), rot);   // its foot: the half metre the old solid never reached
                SurfaceKit.Look(LevelKit.Brass, centre + diag * 1.6f + Vector3.up * (y0 + h + 6.7f), Vector3.one * 0.7f, rot);
            }
    }
}
