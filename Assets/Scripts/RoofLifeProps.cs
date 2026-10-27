using UnityEngine;

/// <summary>
/// THE LIFE ON A ROOF (RoofLife): laundry lines whose sheets sway, pigeon lofts and roosts, period billboards with a
/// lamp row, the odd cat, the looks on the outer neighbours (gables, stepped crowns, a painted board) and the
/// window-washer's cradles on two big faces.
/// </summary>
public static partial class RoofLife
{
    /// A child primitive placed in its parent's own space (LevelKit.Prim places in the world)
    static GameObject Child(Transform parent, Vector3 lpos, Quaternion lrot, Vector3 size, Material m, string name)
    {
        var go = LevelKit.Prim(parent, parent.position, size, m, name);
        go.transform.localPosition = lpos;
        go.transform.localRotation = lrot;
        return go;
    }

    /// A LAUNDRY LINE: two iron posts (solid), a line between their heads, three to five sheets hung from it that sway
    /// on the wind (RoofLifeAnimator): cream, white, the odd oxblood
    static bool Laundry()
    {
        bool alongX = H(80) < 0.5f;
        float len = 4f + 3f * H(81);
        Vector3 size = alongX ? new Vector3(len + 0.4f, 2.3f, 1.6f) : new Vector3(1.6f, 2.3f, len + 0.4f);
        if (!Spot(size, 0.4f, 0.2f, 82, out Vector2 at)) return false;
        Vector2 a = alongX ? Vector2.right : Vector2.up;
        Vector2 p0 = at - a * (len * 0.5f), p1 = at + a * (len * 0.5f);
        for (int s = 0; s < 2; s++)
        {
            Vector2 p = s == 0 ? p0 : p1;
            Block(LevelKit.RivetIron, W(p.x, 1.1f - 0.02f, p.y), new Vector3(0.12f, 2.2f, 0.12f));
            Look(LevelKit.RivetIron, W(p.x, 2.2f, p.y), alongX ? new Vector3(0.6f, 0.06f, 0.06f) : new Vector3(0.06f, 0.06f, 0.6f));
            Look(LevelKit.Charcoal, W(p.x, 0.14f, p.y), new Vector3(0.5f, 0.3f, 0.5f));
        }
        Vector3 la = W(p0.x, 2.16f, p0.y), lb = W(p1.x, 2.16f, p1.y);
        Look(LevelKit.Dark, (la + lb) * 0.5f, new Vector3(0.025f, 0.025f, len - 0.1f), SurfaceKit.SpanRot(la, lb));
        if (anim == null) return true;
        int n = 3 + (int)(H(83) * 3f);
        Material[] cloth = { LevelKit.Cream, LevelKit.Paper, LevelKit.Oxblood, LevelKit.Cream, LevelKit.Paper };
        Vector3 axis = (lb - la).normalized;
        for (int i = 0; i < n; i++)
        {
            Vector3 hang = Vector3.Lerp(la, lb, (i + 0.5f) / n);
            var pivot = new GameObject("Sheet").transform;
            pivot.SetParent(root, false);
            pivot.SetPositionAndRotation(hang, Quaternion.LookRotation(axis, Vector3.up));
            float sw = Mathf.Min(0.8f + 0.5f * H(84 + i), len / n - 0.25f), sh = 1.0f + 0.5f * H(90 + i);
            Child(pivot, new Vector3(0f, -sh * 0.5f, 0f), Quaternion.identity, new Vector3(0.02f, sh, sw), cloth[(Mathf.Abs(F.seed) + i) % cloth.Length], "SheetCloth");
            anim.Sway(pivot, Vector3.forward, 9f + 7f * H(96 + i), 0.9f + 0.6f * H(97 + i), H(98 + i) * 6.2f, 3f);
            Movers++;
        }
        return true;
    }

    /// A PIGEON LOFT: a walnut hutch on iron legs (one solid: the gap under it is nobody's), a pitched oxblood lid,
    /// three dark holes, a perch bar in front, and the birds on it (CityKit.Roost)
    static bool Loft()
    {
        if (!Spot(new Vector3(1.8f, 1.9f, 1.3f), 0.4f, 0.5f, 100, out Vector2 at)) return false;
        Solid(W(at.x, 0.95f - 0.02f, at.y), new Vector3(1.6f, 1.9f, 1.0f));
        for (int i = 0; i < 4; i++) Look(LevelKit.RivetIron, W(at.x + (i % 2 == 0 ? -0.7f : 0.7f), 0.45f, at.y + (i < 2 ? -0.4f : 0.4f)), new Vector3(0.08f, 0.9f, 0.08f));
        Look(LevelKit.WoodPanel, W(at.x, 1.4f, at.y), new Vector3(1.6f, 1.0f, 1.0f));
        Look(LevelKit.Oxblood, W(at.x, 1.96f, at.y + 0.05f), new Vector3(1.8f, 0.05f, 1.25f), F.rot * Quaternion.Euler(-12f, 0f, 0f));
        for (int k = 0; k < 3; k++) Look(LevelKit.Dark, W(at.x - 0.5f + k * 0.5f, 1.45f, at.y - 0.51f), new Vector3(0.2f, 0.2f, 0.02f));
        Look(LevelKit.RivetIron, W(at.x, 1.16f, at.y - 0.62f), new Vector3(1.6f, 0.04f, 0.04f));
        if (life != null) { CityKit.Roost(root, W(at.x, 1.2f, at.y - 0.62f), F.rot * Vector3.right, 0.72f, 5, RoostSeed()); Movers++; }
        return true;
    }

    /// Pigeons on the deck itself, in a line near an edge
    static void RoostOnDeck()
    {
        if (life == null) return;
        if (!Spot(new Vector3(3.2f, 0.4f, 1.0f), 0.3f, 0.9f, 105, out Vector2 at)) return;
        CityKit.Roost(root, W(at.x, 0f, at.y), F.rot * Vector3.right, 1.4f, 4 + (int)(H(106) * 3f), RoostSeed());
        Movers++;
    }

    static readonly string[] Adverts = { "DANCE TONIGHT", "COAL · ICE · OIL", "HATS  GLOVES  FURS", "RADIO  ·  THE WORLD TONIGHT", "GRAND HOTEL", "COLD BEER", "TAILOR  ·  SINCE 1902", "THE MIDNIGHT TRAIN", "SODA  ·  CIGARS  ·  NEWS" };

    /// A PERIOD BILLBOARD near edge e: a charcoal plate in an iron frame on two iron posts (all solid: you run into it
    /// and under it), a cream painted panel with the words in ink, a row of hooded cream lamps along the top shining
    /// down the face. `outward` faces it off the roof (a neighbour, read from the route); else into the roof
    static bool Billboard(int e, bool outward)
    {
        Edge(e, out var o, out var a, out float half, out float reach);
        float w = Mathf.Min(7.5f, half * 2f - 3f);
        if (w < 4.5f) return false;
        bool zEdge = e < 2;
        Vector3 size = zEdge ? new Vector3(w + 0.4f, 5.9f, 0.9f) : new Vector3(0.9f, 5.9f, w + 0.4f);
        Vector2 lc = o * (reach - 1.0f);
        float[] offs = { 0f, -(half - w * 0.5f - 0.8f), half - w * 0.5f - 0.8f };
        foreach (float off in offs)
        {
            Vector2 c = lc + a * off;
            if (!Free(c, size, 0.5f)) continue;
            Plate(c, outward ? o : -o, w, true);
            return true;
        }
        return false;
    }

    static void Plate(Vector2 c, Vector2 facing, float w, bool solid)
    {
        const float legH = 2.5f, plateH = 3.2f;
        Vector3 fwd = F.rot * new Vector3(facing.x, 0f, facing.y);
        Quaternion faceRot = Quaternion.LookRotation(fwd, Vector3.up);
        Vector3 right = Vector3.Cross(Vector3.up, fwd);
        Vector3 pc = W(c.x, legH + plateH * 0.5f, c.y);
        if (solid) Block(LevelKit.Charcoal, pc, new Vector3(w, plateH, 0.2f), faceRot); else Look(LevelKit.Charcoal, pc, new Vector3(w, plateH, 0.2f), faceRot);
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 post = W(c.x, legH * 0.5f - 0.02f, c.y) + right * (s * (w * 0.5f - 0.5f));
            if (solid) Block(LevelKit.RivetIron, post, new Vector3(0.22f, legH, 0.22f), faceRot); else Look(LevelKit.RivetIron, post, new Vector3(0.22f, legH, 0.22f), faceRot);
            Look(LevelKit.RivetIron, pc + right * (s * (w * 0.5f + 0.06f)), new Vector3(0.12f, plateH + 0.24f, 0.3f), faceRot);
        }
        Look(LevelKit.RivetIron, pc + Vector3.up * (plateH * 0.5f + 0.06f), new Vector3(w + 0.24f, 0.12f, 0.3f), faceRot);
        Look(LevelKit.RivetIron, pc - Vector3.up * (plateH * 0.5f + 0.06f), new Vector3(w + 0.24f, 0.12f, 0.3f), faceRot);
        Look(LevelKit.RivetIron, W(c.x, legH * 0.5f, c.y), new Vector3(w - 1.2f, 0.12f, 0.12f), faceRot);
        Look(LevelKit.Cream, pc + fwd * 0.115f, new Vector3(w - 0.4f, plateH - 0.4f, 0.03f), faceRot);
        if (solid)
        {
            string line = H(150) < 0.35f ? LevelKit.Company : Adverts[Mathf.Abs(F.seed) % Adverts.Length];
            LevelKit.Text(root, pc + fwd * 0.14f, Quaternion.LookRotation(-fwd, Vector3.up), line, 0.6f, new Color(0.08f, 0.07f, 0.09f), "Advert", w - 1.2f, true, 260f, 520f);
            Looks++;
        }
        else
        {
            Look(LevelKit.Dark, pc + fwd * 0.14f + Vector3.up * 0.3f, new Vector3(w - 1.6f, 0.5f, 0.02f), faceRot);   // the words, from a block away
            Look(LevelKit.Oxblood, pc + fwd * 0.14f - Vector3.up * 0.5f, new Vector3(w * 0.5f, 0.3f, 0.02f), faceRot);
        }
        int lamps = Mathf.Clamp(Mathf.RoundToInt(w / 1.6f), 3, 6);
        Vector3 bar = pc + Vector3.up * (plateH * 0.5f + 0.6f) + fwd * 0.5f;
        Look(LevelKit.RivetIron, bar, new Vector3(w - 0.6f, 0.06f, 0.06f), faceRot);
        for (int i = 0; i < lamps; i++)
        {
            Vector3 l = bar + right * (((i + 0.5f) / lamps - 0.5f) * (w - 0.8f));
            Look(LevelKit.RivetIron, l + Vector3.up * 0.1f, new Vector3(0.42f, 0.08f, 0.5f), faceRot);
            Look(LevelKit.LampCream, l - Vector3.up * 0.06f, new Vector3(0.26f, 0.14f, 0.26f), faceRot);
            Look(LevelKit.RivetIron, l + Vector3.up * 0.1f - fwd * 0.3f, new Vector3(0.05f, 0.05f, 0.5f), faceRot);
        }
        if (solid && life != null && H(151) < 0.5f) { CityKit.Roost(root, pc + Vector3.up * (plateH * 0.5f + 0.14f), right, w * 0.4f, 4, RoostSeed()); Movers++; }
    }

    /// THE ODD CAT, sitting on a stair head's cap, its tail flicking (RoofLifeAnimator), two amber eyes
    static void Cat()
    {
        if (!haveStair || anim == null) return;
        Vector3 at = stairTop + F.rot * new Vector3(0.6f, 0f, -0.5f);
        Quaternion r = F.rot * Quaternion.Euler(0f, 40f + H(210) * 280f, 0f);
        Look(InkMat, at + Vector3.up * 0.14f, new Vector3(0.18f, 0.2f, 0.46f), r);
        Look(InkMat, at + Vector3.up * 0.33f + r * Vector3.forward * 0.18f, new Vector3(0.15f, 0.14f, 0.14f), r);
        for (int s = -1; s <= 1; s += 2) Look(InkMat, at + Vector3.up * 0.42f + r * new Vector3(s * 0.05f, 0f, 0.18f), new Vector3(0.04f, 0.06f, 0.03f), r);
        for (int s = -1; s <= 1; s += 2) Look(LevelKit.WindowAmber, at + Vector3.up * 0.34f + r * new Vector3(s * 0.04f, 0f, 0.255f), new Vector3(0.025f, 0.02f, 0.01f), r);
        var pivot = new GameObject("CatTail").transform;
        pivot.SetParent(root, false);
        pivot.SetPositionAndRotation(at + Vector3.up * 0.1f - r * Vector3.forward * 0.23f, r);
        Child(pivot, new Vector3(0f, 0f, -0.15f), Quaternion.identity, new Vector3(0.035f, 0.035f, 0.3f), InkMat, "Tail");
        anim.Sway(pivot, Vector3.up, 25f, 1.1f, H(211) * 6f, 8f);
        Movers++;
    }

    // ------------------------------------------------------------ the outer neighbours: looks only

    /// An outer neighbour (nobody reaches it): a gable or a stepped crown over its roof, a chimney or two, now and
    /// then a painted board looking at the route. Looks only, no text.
    static void OuterLooks(RoofKit.Block b)
    {
        if (b.blade) return;
        RoofKit.RoofOf(b, out Vector3 top, out float hx, out float hz);
        F = new Frame { c = top + Vector3.up * 0.6f, rot = Quaternion.identity, hx = hx, hz = hz, seed = b.row * 53 + b.lane * 7 + Mathf.RoundToInt(b.foot.x), neighbour = true };
        float pick = H(220);
        if (pick < 0.35f) Gable();
        else if (pick < 0.6f) SteppedCrown();
        int chimneys = 1 + (int)(H(221) * 2f);
        for (int i = 0; i < chimneys; i++)
        {
            Vector2 at = new Vector2((H(222 + i) - 0.5f) * (hx * 1.4f), (H(225 + i) - 0.5f) * (hz * 1.4f));
            float h = 2.2f + H(228 + i) * 1.4f;
            Look(LevelKit.Brick, W(at.x, h * 0.5f, at.y), new Vector3(0.9f, h, 0.9f));
            Look(LevelKit.Limestone, W(at.x, h + 0.08f, at.y), new Vector3(1.1f, 0.2f, 1.1f));
        }
        if (H(229) < 0.25f && hx > 4f)
        {
            int toward = b.foot.x < 0f ? 3 : 2;
            Edge(toward, out var o, out _, out _, out float reach);
            Plate(o * (reach - 1f), o, Mathf.Min(7f, hz * 2f - 2f), false);
        }
    }

    /// A pitched roof: two slate slopes to a ridge along the roof's long axis, crow-stepped stone gables at the ends
    static void Gable()
    {
        bool alongX = F.hx >= F.hz;
        float half = alongX ? F.hz : F.hx, len = (alongX ? F.hx : F.hz) * 2f - 0.6f;
        float rise = Mathf.Min(half * 0.7f, 4f), pitch = Mathf.Atan2(rise, half) * Mathf.Rad2Deg, slope = Mathf.Sqrt(rise * rise + half * half);
        Quaternion yaw = alongX ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
        for (int s = -1; s <= 1; s += 2)
        {
            Quaternion r = yaw * Quaternion.Euler(s * pitch, 0f, 0f);
            Vector3 c = F.c + yaw * new Vector3(0f, rise * 0.5f, s * half * 0.5f);
            Look(LevelKit.Charcoal, c, new Vector3(len, 0.25f, slope), r);
        }
        Look(LevelKit.RivetIron, F.c + Vector3.up * (rise + 0.1f), new Vector3(len, 0.18f, 0.18f), yaw);
        for (int e = -1; e <= 1; e += 2)
            for (int k = 0; k < 3; k++)
            {
                float wk = half * 2f * (1f - k / 3f), hk = rise * (k + 1) / 3f;
                Look(LevelKit.Limestone, F.c + yaw * new Vector3(e * (len * 0.5f - 0.2f), hk * 0.5f, 0f), new Vector3(0.4f, hk, wk), yaw);
            }
    }

    /// A stepped Deco crown: three charcoal steps shrinking, a stone lip on each, a brass finial
    static void SteppedCrown()
    {
        float w = F.hx * 2f - 0.8f, d = F.hz * 2f - 0.8f, y = 0f;
        for (int k = 0; k < 3; k++)
        {
            float sk = 1f - k * 0.3f, hk = 1.2f - k * 0.15f;
            Look(LevelKit.Charcoal, F.c + Vector3.up * (y + hk * 0.5f), new Vector3(w * sk, hk, d * sk));
            Look(LevelKit.Limestone, F.c + Vector3.up * (y + hk + 0.05f), new Vector3(w * sk + 0.3f, 0.12f, d * sk + 0.3f));
            y += hk;
        }
        Look(LevelKit.Brass, F.c + Vector3.up * (y + 0.9f), new Vector3(0.2f, 1.6f, 0.2f));
    }

    // ------------------------------------------------------------ the cradles

    /// Two window-washers' cradles: on the tallest full neighbour either side of the route's middle stretch, hung from
    /// a davit on the upper storey's roof down the face that looks at the route (WashCradle)
    static void Cradles(System.Collections.Generic.List<RoofKit.Block> blocks)
    {
        if (life == null) return;
        float zMid = 0f; int n = 0;
        foreach (var b in blocks) { zMid += b.foot.z; n++; }
        if (n == 0) return;
        zMid /= n;
        for (int side = -1; side <= 1; side += 2)
        {
            RoofKit.Block best = default; float bestH = 0f; bool any = false;
            foreach (var b in blocks)
            {
                if (!b.full || b.blade || (b.foot.x < 0f ? -1 : 1) != side || Mathf.Abs(b.foot.z - zMid) > 500f) continue;
                if (b.h > bestH) { bestH = b.h; best = b; any = true; }
            }
            if (any) Cradle(best, -side);
        }
    }

    static void Cradle(RoofKit.Block b, float faceSign)
    {
        float h1 = b.h * 0.62f, w2 = b.w * 0.82f, d2 = b.d * 0.82f;
        float yTop = b.h + RoofKit.Proud, y0 = h1 + 3f, y1 = b.h - 3.5f;
        if (y1 - y0 < 8f) return;
        F = new Frame { c = b.foot + Vector3.up * yTop, rot = Quaternion.identity, hx = w2 * 0.5f, hz = d2 * 0.5f, seed = b.row * 31 + b.lane, neighbour = true };
        Vector3 outN = new Vector3(faceSign, 0f, 0f);
        Vector3 edge = b.foot + Vector3.up * yTop + outN * (w2 * 0.5f);
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 post = edge + Vector3.forward * (s * 1.3f) - outN * 0.8f + Vector3.up * 0.6f;
            Look(LevelKit.RivetIron, post, new Vector3(0.16f, 1.2f, 0.16f));
            Solid(post, new Vector3(0.2f, 1.2f, 0.2f));
            Look(LevelKit.RivetIron, edge + Vector3.forward * (s * 1.3f) + outN * 0.3f + Vector3.up * 1.2f, new Vector3(2.2f, 0.16f, 0.16f));
        }
        Look(LevelKit.Charcoal, edge - outN * 0.8f + Vector3.up * 0.4f, new Vector3(0.9f, 0.8f, 1.2f));
        Vector3 hang = edge + outN * 1.3f;
        var cradle = new GameObject("WashCradle").transform;
        cradle.SetParent(root, false);
        cradle.position = hang + Vector3.up * (y0 - yTop);
        Child(cradle, Vector3.zero, Quaternion.identity, new Vector3(1.0f, 0.12f, 3.2f), LevelKit.RivetIron, "CradleFloor");
        for (int s = -1; s <= 1; s += 2)
        {
            Child(cradle, new Vector3(s * 0.48f, 0.95f, 0f), Quaternion.identity, new Vector3(0.05f, 0.05f, 3.2f), LevelKit.RivetIron, "CradleRail");
            for (int t = -1; t <= 1; t += 2) Child(cradle, new Vector3(s * 0.48f, 0.5f, t * 1.55f), Quaternion.identity, new Vector3(0.05f, 1.0f, 0.05f), LevelKit.RivetIron, "CradlePost");
        }
        Quaternion toWall = Quaternion.LookRotation(-outN, Vector3.up);
        FigurePrims(cradle, new Vector3(0.1f, 0.06f, -0.8f), toWall, false);
        FigurePrims(cradle, new Vector3(0.1f, 0.06f, 0.8f), toWall, true);
        Child(cradle, new Vector3(-0.3f, 1.05f, 0f), Quaternion.identity, Vector3.one * 0.26f, LevelKit.LampCream, "WorkLamp");
        Child(cradle, new Vector3(0.3f, 0.21f, 0f), Quaternion.identity, new Vector3(0.3f, 0.3f, 0.3f), LevelKit.Charcoal, "Bucket");
        var cables = new Transform[2];
        var anchors = new Vector3[2];
        for (int s = 0; s < 2; s++)
        {
            anchors[s] = hang + Vector3.forward * (s == 0 ? 1.3f : -1.3f);
            cables[s] = LevelKit.Prim(root, anchors[s], new Vector3(0.03f, 1f, 0.03f), LevelKit.Dark, "CradleCable").transform;
        }
        cradle.gameObject.AddComponent<WashCradle>().Init(hang, y0 - yTop, y1 - yTop, cables, anchors, 45f + 25f * H(1));
        Movers += 3;
    }
}
