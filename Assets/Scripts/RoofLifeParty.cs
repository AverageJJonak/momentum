using UnityEngine;

/// <summary>
/// THE PENTHOUSE PARTY (RoofLife): a penthouse against a roof's edge with lit windows, a door under a sunburst and a
/// globe lamp either side, a terrace in front of it inside a stone balustrade, guests standing in twos, a three-piece
/// band on a dais nodding as it plays, a string of lamps over everyone. The body, the balustrade's posts and rail, the
/// dais, the instruments and the guests are solid; the figures are stepped abstractions like the city's bronzes.
/// </summary>
public static partial class RoofLife
{
    /// A penthouse with its back to edge e, the terrace toward the roof's middle; `at` places the body by hand
    static bool Penthouse(int e, Vector2? at = null)
    {
        Edge(e, out var o, out _, out float half, out float reach);
        bool zEdge = e < 2;
        const float bw = 12f, bd = 6.5f, bh = 4.2f, td = 5.5f;
        if (half * 2f < bw + 2f || reach * 2f < bd + td + 4f) return false;
        Vector2 bc = at ?? o * (reach - bd * 0.5f - 0.6f);
        Vector2 tc = bc - o * (bd * 0.5f + td * 0.5f);
        Vector3 bodySize = zEdge ? new Vector3(bw, bh, bd) : new Vector3(bd, bh, bw);
        Vector3 terrSize = zEdge ? new Vector3(bw, 1.9f, td) : new Vector3(td, 1.9f, bw);
        if (!Free(bc, bodySize, 0.5f) || !Free(tc, terrSize, 0.3f)) return false;
        Vector3 fwd = F.rot * new Vector3(-o.x, 0f, -o.y);
        Vector3 right = Vector3.Cross(Vector3.up, fwd);
        Quaternion faceRot = Quaternion.LookRotation(fwd, Vector3.up);
        Vector3 body = W(bc.x, bh * 0.5f - 0.02f, bc.y);
        Material plaster = H(160) < 0.5f ? LevelKit.Cream : LevelKit.Limestone;
        Block(plaster, body, new Vector3(bw, bh, bd), faceRot);
        // The stepped cornice, the attic step over it, a gilt lip along the front
        Look(LevelKit.Limestone, body + Vector3.up * (bh * 0.5f + 0.11f), new Vector3(bw + 0.4f, 0.24f, bd + 0.4f), faceRot);
        Look(LevelKit.Limestone, body + Vector3.up * (bh * 0.5f + 0.32f), new Vector3(bw + 0.7f, 0.2f, bd + 0.7f), faceRot);
        Look(LevelKit.Charcoal, body + Vector3.up * (bh * 0.5f + 0.66f), new Vector3(bw - 0.4f, 0.5f, bd - 0.4f), faceRot);
        Look(LevelKit.Brass, body + Vector3.up * (bh * 0.5f + 0.45f) + fwd * (bd * 0.5f + 0.36f), new Vector3(bw + 0.7f, 0.05f, 0.08f), faceRot);
        // The terrace face: a door under a brass sunburst, two tall lit windows either side with cream glazing bars
        Vector3 face = body + fwd * (bd * 0.5f);
        float sill = -(bh * 0.5f);
        Look(LevelKit.Dark, face + fwd * 0.02f + Vector3.up * (sill + 1.27f), new Vector3(1.6f, 2.5f, 0.04f), faceRot);
        Look(LevelKit.Brass, face + fwd * 0.03f + Vector3.up * (sill + 2.77f), new Vector3(1.9f, 0.5f, 0.05f), faceRot);
        for (int k = 0; k < 7; k++) Look(LevelKit.Dark, face + fwd * 0.06f + Vector3.up * (sill + 2.77f) + right * (-0.75f + k * 0.25f), new Vector3(0.03f, 0.42f, 0.02f), faceRot);
        for (int s = -1; s <= 1; s += 2)
            for (int k = 0; k < 2; k++)
            {
                Vector3 wc = face + fwd * 0.02f + right * (s * (2.2f + k * 2.6f)) + Vector3.up * (sill + 1.9f);
                Look(WindowMat(F.seed + s + k * 3, 0, k, s + 1), wc, new Vector3(1.6f, 2.2f, 0.04f), faceRot);
                Look(LevelKit.Cream, wc + fwd * 0.025f, new Vector3(0.06f, 2.2f, 0.03f), faceRot);
                Look(LevelKit.Cream, wc + fwd * 0.025f, new Vector3(1.6f, 0.06f, 0.03f), faceRot);
                Look(LevelKit.Limestone, wc + Vector3.up * 1.25f + fwd * 0.08f, new Vector3(1.9f, 0.16f, 0.16f), faceRot);
            }
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 lb = face + fwd * 0.3f + right * (s * 1.3f) + Vector3.up * (sill + 3.0f);
            Look(LevelKit.RivetIron, lb - fwd * 0.15f, new Vector3(0.06f, 0.06f, 0.3f), faceRot);
            Look(LevelKit.LampCream, lb, Vector3.one * 0.3f, faceRot, PrimitiveType.Sphere);
        }
        for (int s = -1; s <= 1; s += 2)
            Look(WindowMat(F.seed + 7 * s, 1, 0, 0), body + right * (s * (bw * 0.5f + 0.02f)) + Vector3.up * (sill + 1.9f), new Vector3(0.04f, 2.0f, 1.6f), faceRot);
        // The terrace: the balustrade, the guests, the band, the lamps
        Vector3 terr = W(tc.x, 0f, tc.y);
        Rail(terr + fwd * (td * 0.5f - 0.2f) - right * ((bw * 0.5f + 1.5f) * 0.5f), right, bw * 0.5f - 1.5f);
        Rail(terr + fwd * (td * 0.5f - 0.2f) + right * ((bw * 0.5f + 1.5f) * 0.5f), right, bw * 0.5f - 1.5f);
        Rail(terr - right * (bw * 0.5f - 0.2f), fwd, td - 0.4f);
        Rail(terr + right * (bw * 0.5f - 0.2f), fwd, td - 0.4f);
        int pairs = 2 + (int)(H(170) * 3f);
        for (int p = 0; p < pairs; p++)
        {
            Vector3 pc = terr + right * Mathf.Lerp(-bw * 0.5f + 1.8f, bw * 0.5f - 4.6f, (p + 0.5f) / pairs) + fwd * ((H(171 + p) - 0.5f) * 2.4f);
            Pair(pc, H(176 + p) * 360f);
        }
        Band(terr + right * (bw * 0.5f - 2.4f), -right);
        Lamps(terr, fwd, right, bw, td);
        return true;
    }

    /// A length of stone balustrade along `along`: posts every two metres and the rail over them solid, the balusters looks
    static void Rail(Vector3 centre, Vector3 along, float len)
    {
        Quaternion r = Quaternion.LookRotation(along, Vector3.up);
        int posts = Mathf.Max(1, Mathf.RoundToInt(len / 2f));
        for (int i = 0; i <= posts; i++)
        {
            Vector3 p = centre + along * (-len * 0.5f + len * i / posts);
            Block(LevelKit.Limestone, p + Vector3.up * 0.48f, new Vector3(0.28f, 1.0f, 0.28f), r);
            Look(LevelKit.Limestone, p + Vector3.up * 1.03f, new Vector3(0.36f, 0.1f, 0.36f), r);
        }
        Block(LevelKit.Limestone, centre + Vector3.up * 0.95f, new Vector3(0.18f, 0.12f, len), r);
        int bal = Mathf.RoundToInt(len / 0.45f);
        for (int i = 0; i < bal; i++) Look(LevelKit.Limestone, centre + along * (-len * 0.5f + len * (i + 0.5f) / bal) + Vector3.up * 0.48f, new Vector3(0.1f, 0.84f, 0.1f), r);
        Look(LevelKit.Limestone, centre + Vector3.up * 0.04f, new Vector3(0.24f, 0.1f, len), r);
    }

    /// Two guests face to face, a gown and a top hat as the hash says; one solid for the pair
    static void Pair(Vector3 at, float yaw)
    {
        Vector3 d = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Figure(at - d * 0.4f, Quaternion.LookRotation(d, Vector3.up), H(180) < 0.6f, H(181) < 0.5f);
        Figure(at + d * 0.4f, Quaternion.LookRotation(-d, Vector3.up), H(182) < 0.3f, H(183) < 0.6f);
        Solid(at + Vector3.up * 0.88f, new Vector3(0.6f, 1.8f, 1.5f), Quaternion.LookRotation(d, Vector3.up));
    }

    /// A figure in evening dress, 1939, as a stepped abstraction (the bronzes' manner): legs and coat or a long gown,
    /// a shirt front, a head, a top hat or a cloche. Baked.
    static void Figure(Vector3 foot, Quaternion r, bool hat, bool gown)
    {
        if (gown) Look(LevelKit.Oxblood, foot + Vector3.up * 0.74f, new Vector3(0.5f, 1.5f, 0.36f), r);
        else
        {
            Look(InkMat, foot + Vector3.up * 0.41f, new Vector3(0.36f, 0.84f, 0.26f), r);
            Look(InkMat, foot + Vector3.up * 1.18f, new Vector3(0.5f, 0.72f, 0.3f), r);
            Look(LevelKit.Cream, foot + Vector3.up * 1.24f + r * Vector3.forward * 0.16f, new Vector3(0.16f, 0.5f, 0.02f), r);
        }
        Look(SkinMat, foot + Vector3.up * 1.66f, new Vector3(0.2f, 0.24f, 0.2f), r);
        if (hat) { Look(InkMat, foot + Vector3.up * 1.8f, new Vector3(0.34f, 0.03f, 0.34f), r); Look(InkMat, foot + Vector3.up * 1.9f, new Vector3(0.22f, 0.18f, 0.22f), r); }
        else Look(InkMat, foot + Vector3.up * 1.75f, new Vector3(0.22f, 0.1f, 0.22f), r);
    }

    /// The same figure as primitives under `parent` (the band, the cradle's washers): it can move with its parent
    static void FigurePrims(Transform parent, Vector3 foot, Quaternion r, bool hat)
    {
        Child(parent, foot + r * new Vector3(0f, 0.41f, 0f), r, new Vector3(0.36f, 0.84f, 0.26f), InkMat, "FigureLegs");
        Child(parent, foot + r * new Vector3(0f, 1.18f, 0f), r, new Vector3(0.5f, 0.72f, 0.3f), InkMat, "FigureCoat");
        Child(parent, foot + r * new Vector3(0f, 1.24f, 0.16f), r, new Vector3(0.16f, 0.5f, 0.02f), LevelKit.Cream, "FigureShirt");
        Child(parent, foot + r * new Vector3(0f, 1.66f, 0f), r, new Vector3(0.2f, 0.24f, 0.2f), SkinMat, "FigureHead");
        if (hat) { Child(parent, foot + r * new Vector3(0f, 1.8f, 0f), r, new Vector3(0.34f, 0.03f, 0.34f), InkMat, "FigureBrim"); Child(parent, foot + r * new Vector3(0f, 1.9f, 0f), r, new Vector3(0.22f, 0.18f, 0.22f), InkMat, "FigureHat"); }
        else Child(parent, foot + r * new Vector3(0f, 1.75f, 0f), r, new Vector3(0.22f, 0.1f, 0.22f), InkMat, "FigureHair");
    }

    /// THE BAND: three musicians on a walnut dais nodding as they play (RoofLifeAnimator), a double bass, an upright
    /// piano, a drum; an empty "PenthouseBand" transform marks where the music comes from, for whoever records a clip.
    /// No sound here.
    static void Band(Vector3 at, Vector3 facing)
    {
        Quaternion r = Quaternion.LookRotation(facing, Vector3.up);
        Vector3 right = Vector3.Cross(Vector3.up, facing);
        Block(LevelKit.WoodPanel, at + Vector3.up * 0.13f, new Vector3(3.8f, 0.3f, 2.6f), r);
        Look(LevelKit.Brass, at + Vector3.up * 0.3f + facing * 1.28f, new Vector3(3.8f, 0.03f, 0.08f), r);
        var anchor = new GameObject("PenthouseBand").transform;
        anchor.SetParent(root, false);
        anchor.position = at + Vector3.up * 1.2f;
        Vector3 top = at + Vector3.up * 0.28f;
        Musician(top - right * 1.3f - facing * 0.3f, r, 0);
        Musician(top + right * 1.3f - facing * 0.5f, r, 1);
        Musician(top - facing * 0.8f, r, 2);
        Vector3 bass = top - right * 1.3f + facing * 0.35f;
        Look(LevelKit.WoodPanel, bass + Vector3.up * 0.88f, new Vector3(0.5f, 1.7f, 0.26f), r * Quaternion.Euler(0f, 0f, 12f));
        Look(LevelKit.Cream, bass + Vector3.up * 1.05f + facing * 0.14f, new Vector3(0.03f, 1.2f, 0.01f), r * Quaternion.Euler(0f, 0f, 12f));
        Solid(bass + Vector3.up * 0.88f, new Vector3(0.5f, 1.7f, 0.3f), r);
        Vector3 piano = top + right * 1.3f + facing * 0.45f;
        Block(LevelKit.Marble, piano + Vector3.up * 0.55f, new Vector3(1.4f, 1.1f, 0.6f), r);
        Look(LevelKit.Cream, piano + Vector3.up * 0.84f - facing * 0.34f, new Vector3(1.2f, 0.04f, 0.1f), r);
        Look(LevelKit.Marble, piano + Vector3.up * 1.15f, new Vector3(1.44f, 0.08f, 0.66f), r);
        Vector3 drum = top - facing * 0.1f;
        Look(LevelKit.Oxblood, drum + Vector3.up * 0.32f, new Vector3(0.6f, 0.2f, 0.6f), r * Quaternion.Euler(90f, 0f, 0f), PrimitiveType.Cylinder);
        Look(LevelKit.RivetIron, drum + Vector3.up * 0.72f + right * 0.45f, new Vector3(0.03f, 0.4f, 0.03f), r, PrimitiveType.Cylinder);
        Look(LevelKit.Brass, drum + Vector3.up * 1.12f + right * 0.45f, new Vector3(0.4f, 0.01f, 0.4f), r, PrimitiveType.Cylinder);
        Solid(drum + Vector3.up * 0.32f, new Vector3(0.6f, 0.6f, 0.4f), r);
    }

    static void Musician(Vector3 foot, Quaternion r, int which)
    {
        if (anim == null) { Figure(foot, r, false, false); return; }
        var pivot = new GameObject("Musician").transform;
        pivot.SetParent(root, false);
        pivot.SetPositionAndRotation(foot, r);
        FigurePrims(pivot, Vector3.zero, Quaternion.identity, which == 1);
        anim.Sway(pivot, Vector3.right, 4f, 2.1f + which * 0.35f, which * 2.1f, 1.5f);
        Movers++;
    }

    /// A string of cream lamps over the terrace between two iron posts at its front corners, sagging in the middle
    static void Lamps(Vector3 terr, Vector3 fwd, Vector3 right, float bw, float td)
    {
        Vector3 a = terr + fwd * (td * 0.5f - 0.5f) - right * (bw * 0.5f - 0.6f), b = terr + fwd * (td * 0.5f - 0.5f) + right * (bw * 0.5f - 0.6f);
        foreach (var p in new[] { a, b })
        {
            Block(LevelKit.RivetIron, p + Vector3.up * 1.38f, new Vector3(0.1f, 2.8f, 0.1f));
            Look(LevelKit.Brass, p + Vector3.up * 2.84f, new Vector3(0.16f, 0.1f, 0.16f));
        }
        Vector3 ta = a + Vector3.up * 2.75f, tb = b + Vector3.up * 2.75f, mid = (ta + tb) * 0.5f - Vector3.up * 0.5f;
        Look(LevelKit.Dark, (ta + mid) * 0.5f, new Vector3(0.02f, 0.02f, Vector3.Distance(ta, mid)), SurfaceKit.SpanRot(ta, mid));
        Look(LevelKit.Dark, (mid + tb) * 0.5f, new Vector3(0.02f, 0.02f, Vector3.Distance(mid, tb)), SurfaceKit.SpanRot(mid, tb));
        int bulbs = 7;
        for (int i = 0; i < bulbs; i++)
        {
            float t = (i + 0.5f) / bulbs;
            Vector3 p = Vector3.Lerp(ta, tb, t) - Vector3.up * (0.5f * Mathf.Sin(t * Mathf.PI) + 0.12f);
            Look(LevelKit.LampCream, p, Vector3.one * 0.14f, null, PrimitiveType.Sphere);
        }
    }
}
