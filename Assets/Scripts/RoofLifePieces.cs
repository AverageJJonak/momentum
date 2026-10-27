using UnityEngine;

/// <summary>
/// THE SOLID PIECES OF A ROOF (RoofLife): parapets, mansards, skylights, stair heads, water tanks, chimneys and vents.
/// Everything a body can meet is a Block (baked look + collider) or a kit slab; the caps, bands, doors, lamps and
/// slits on them are looks baked with them. Every piece stands two centimetres into the slab and every cap a
/// centimetre into its body, so no face shares a plane with another (the coplanar rule).
/// </summary>
public static partial class RoofLife
{
    /// Edge e of the roof being dressed (0 south, 1 north, 2 west, 3 east): its outward axis, the axis along it, half
    /// its length and how far out it stands, all in the roof's own x/z
    static void Edge(int e, out Vector2 outward, out Vector2 along, out float half, out float reach)
    {
        bool z = e < 2;
        float sgn = e % 2 == 0 ? -1f : 1f;
        outward = z ? new Vector2(0f, sgn) : new Vector2(sgn, 0f);
        along = z ? Vector2.right : Vector2.up;
        half = z ? F.hx : F.hz;
        reach = z ? F.hz : F.hx;
    }

    /// A PARAPET of varied height (0.6–1.1 m, the corner piers to 1.45) along edge e in six-metre runs, each laid only
    /// where nothing stands (a landing crosses the edge there, a kerb is there already); a stone cap and HARD LINE's
    /// lit edge along it. Never brass: a lip you corner-boost wears brass, and those stay where they are.
    static void Parapet(int e)
    {
        if (edgeTaken[e]) return;
        Edge(e, out var o, out var a, out float half, out float reach);
        float h = 0.6f + 0.5f * H(30 + e), thick = 0.4f;
        Material m = H(34) < 0.5f ? LevelKit.Limestone : LevelKit.Brick;
        bool zEdge = e < 2;
        float run = half * 2f - 1.8f;
        int segs = Mathf.Max(1, Mathf.RoundToInt(run / 6f));
        float segLen = run / segs;
        for (int i = 0; i < segs; i++)
        {
            float s = -run * 0.5f + segLen * (i + 0.5f);
            Vector2 lc = o * (reach - thick * 0.5f - 0.2f) + a * s;
            Vector3 size = zEdge ? new Vector3(segLen - 0.1f, h, thick) : new Vector3(thick, h, segLen - 0.1f);
            if (!Free(lc, size, 0.3f)) continue;
            Block(m, W(lc.x, h * 0.5f - 0.02f, lc.y), size);
            Look(LevelKit.Limestone, W(lc.x, h + 0.03f, lc.y), zEdge ? new Vector3(size.x + 0.1f, 0.12f, thick + 0.16f) : new Vector3(thick + 0.16f, 0.12f, size.z + 0.1f));
            Vector2 lip = lc + o * (thick * 0.5f + 0.04f);
            Look(LevelKit.Led, W(lip.x, h + 0.12f, lip.y), zEdge ? new Vector3(size.x, 0.05f, 0.05f) : new Vector3(0.05f, 0.05f, size.z));
        }
        if (e >= 2) return;   // the piers belong to the south and north edges: once per corner
        for (int s = -1; s <= 1; s += 2)
        {
            Vector2 lc = new Vector2(s * (F.hx - 0.55f), o.y * (F.hz - 0.55f));
            Vector3 size = new Vector3(0.7f, h + 0.35f, 0.7f);
            if (!Free(lc, size, 0.3f)) continue;
            Block(LevelKit.Limestone, W(lc.x, size.y * 0.5f - 0.02f, lc.y), size);
            Look(LevelKit.Limestone, W(lc.x, size.y + 0.04f, lc.y), new Vector3(0.9f, 0.14f, 0.9f));
            Look(LevelKit.Limestone, W(lc.x, size.y + 0.28f, lc.y), new Vector3(0.36f, 0.36f, 0.36f));
        }
    }

    /// A MANSARD along edge e: a four-metre slope rising from the deck to the roof's edge at 32–38° (a kit slab in the
    /// chute's own surface: a SLIDE, down into the roof or up to the edge's wall), the wall it rises to standing on
    /// through a parapet, stepped stone cheeks at the ends, lit dormer slits on the slope. Six metres or longer: the
    /// whole edge if it is clear, else the clear half of it.
    static bool Mansard(int e)
    {
        Edge(e, out var o, out var a, out float half, out float reach);
        if (half < 4f || reach < 8f) return false;
        const float run = 4f;
        float angle = 32f + 6f * H(40 + e), rise = run * Mathf.Tan(angle * Mathf.Deg2Rad);
        bool zEdge = e < 2;
        float full = half * 2f - 2f;
        for (int k = 0; k < 3; k++)
        {
            float len = k == 0 ? full : full * 0.5f - 0.5f;
            float off = k == 0 ? 0f : (k == 1 ? -1f : 1f) * (full * 0.25f + 0.25f);
            if (len < 6f) continue;
            Vector2 lc = o * (reach - run * 0.5f - 0.35f) + a * off;
            Vector3 size = zEdge ? new Vector3(len, rise + 0.6f, run) : new Vector3(run, rise + 0.6f, len);
            if (!Free(lc, size, 0.6f)) continue;
            Wedge(e, lc, len, run, rise, zEdge);
            edgeTaken[e] = true;
            return true;
        }
        return false;
    }

    static void Wedge(int e, Vector2 lc, float len, float run, float rise, bool zEdge)
    {
        Edge(e, out var o, out var a, out _, out float reach);
        Vector2 inner = lc - o * (run * 0.5f), outer = lc + o * (run * 0.5f);
        Vector3 lowTop = W(inner.x, 0f, inner.y), highTop = W(outer.x, rise, outer.y);
        var slab = SurfaceKit.SpanSlab(root, lowTop, highTop, len, 0.3f, LevelKit.Slope, "Mansard");
        Solids++;
        // The wall at the edge the slope rises into (its end is inside the wall), up through the ridge: the mansard's
        // own parapet, capped
        Vector2 back = o * (reach - 0.2f);
        float wallH = rise + 0.6f;
        Vector3 wallSize = zEdge ? new Vector3(len, wallH, 0.4f) : new Vector3(0.4f, wallH, len);
        Block(LevelKit.Limestone, W(back.x, wallH * 0.5f - 0.02f, back.y), wallSize);
        Look(LevelKit.Limestone, W(back.x, wallH + 0.03f, back.y), zEdge ? new Vector3(len + 0.1f, 0.12f, 0.56f) : new Vector3(0.56f, 0.12f, len + 0.1f));
        // Stepped stone cheeks at both ends, under the slope's line
        for (int s = -1; s <= 1; s += 2)
            for (int k = 0; k < 3; k++)
            {
                float r0 = run * k / 3f, r1 = run * (k + 1) / 3f;
                float hk = rise * (1f - r1 / run) - 0.3f;
                if (hk < 0.3f) continue;
                Vector2 sc = outer - o * ((r0 + r1) * 0.5f) + a * (s * (len * 0.5f - 0.2f));
                Vector3 ss = zEdge ? new Vector3(0.4f, hk, r1 - r0) : new Vector3(r1 - r0, hk, 0.4f);
                Block(LevelKit.Limestone, W(sc.x, hk * 0.5f - 0.02f, sc.y), ss);
            }
        // Dormer slits on the slope: lit amber (a switching group now and then), a stone sill under each
        Quaternion slabRot = slab.transform.rotation;
        Vector3 up = slabRot * Vector3.up;
        int dormers = Mathf.Clamp(Mathf.FloorToInt(len / 5f), 1, 3);
        for (int i = 0; i < dormers; i++)
        {
            float t = (i + 0.5f) / dormers - 0.5f;
            Vector3 mid = Vector3.Lerp(lowTop, highTop, 0.55f) + F.rot * (zEdge ? new Vector3(t * len, 0f, 0f) : new Vector3(0f, 0f, t * len));
            Look(WindowMat(F.seed + e, i, 1, 1), mid + up * 0.02f, new Vector3(1.4f, 0.03f, 0.5f), slabRot);
            Look(LevelKit.Limestone, mid + up * 0.04f + slabRot * Vector3.back * 0.32f, new Vector3(1.6f, 0.07f, 0.1f), slabRot);
        }
    }

    /// SKYLIGHTS in a row of two or three: a curb 1.4 m high round a well two by three, the roof for its floor, amber
    /// light on that floor from the room below, and a breakable pane over the top (LevelKit.Glass: crash through it
    /// at speed, or slam it, and you drop into the well)
    static void Skylights()
    {
        int want = 2 + (int)(H(50) * 2f);
        bool alongX = F.hx >= F.hz;
        Vector3 size = alongX ? new Vector3(3.4f, 1.4f, 2.4f) : new Vector3(2.4f, 1.4f, 3.4f);
        if (!Spot(size, 0.6f, -0.3f, 51, out Vector2 at)) return;
        Vector2 step = alongX ? new Vector2(4.6f, 0f) : new Vector2(0f, 4.6f);
        for (int i = 0; i < want; i++)
        {
            Vector2 lc = at + step * i;
            if (i > 0 && !Free(lc, size, 0.6f)) break;
            Skylight(lc, alongX);
        }
    }

    static void Skylight(Vector2 lc, bool alongX)
    {
        float L = alongX ? 3.4f : 2.4f, D = alongX ? 2.4f : 3.4f;
        const float h = 1.4f, t = 0.2f;
        Material m = H(52) < 0.5f ? LevelKit.Charcoal : LevelKit.Brick;
        Block(m, W(lc.x, h * 0.5f - 0.02f, lc.y - D * 0.5f + t * 0.5f), new Vector3(L, h, t));
        Block(m, W(lc.x, h * 0.5f - 0.02f, lc.y + D * 0.5f - t * 0.5f), new Vector3(L, h, t));
        Block(m, W(lc.x - L * 0.5f + t * 0.5f, h * 0.5f - 0.02f, lc.y), new Vector3(t, h, D - 2f * t));
        Block(m, W(lc.x + L * 0.5f - t * 0.5f, h * 0.5f - 0.02f, lc.y), new Vector3(t, h, D - 2f * t));
        Look(LevelKit.WindowAmber, W(lc.x, 0.03f, lc.y), new Vector3(L - 2f * t - 0.1f, 0.02f, D - 2f * t - 0.1f));   // the room below
        var glass = LevelKit.Glass(root, W(lc.x, h + 0.04f, lc.y), new Vector3(L - 2f * t + 0.1f, 0.06f, D - 2f * t + 0.1f), cfg);
        glass.transform.rotation = F.rot;
        Solids++; Looks += 5;
    }

    /// A STAIR HEAD: a solid box two and a half by two and a half by three, a door with a brass kick plate and a
    /// sunburst transom on the face that looks at the roof's middle, a hooded cream lamp over it, a stepped cap, a vent
    /// pipe. A kick wall, three metres tall, on every roof that has room for one.
    static bool StairHead()
    {
        Vector3 size = new Vector3(2.5f, 3f, 2.5f);
        if (!Spot(size, 0.5f, 0.6f, 60, out Vector2 at)) return false;
        Material m = H(61) < 0.4f ? LevelKit.Brick : H(61) < 0.7f ? LevelKit.Limestone : LevelKit.Charcoal;
        Block(m, W(at.x, 1.5f - 0.02f, at.y), size);
        Vector2 toMid = at.sqrMagnitude > 0.01f ? -at.normalized : Vector2.up;
        Vector2 face = Mathf.Abs(toMid.x) > Mathf.Abs(toMid.y) ? new Vector2(Mathf.Sign(toMid.x), 0f) : new Vector2(0f, Mathf.Sign(toMid.y));
        bool fx = face.x != 0f;
        Vector2 df = at + face * 1.275f;
        Look(LevelKit.Dark, W(df.x, 1.08f, df.y), fx ? new Vector3(0.05f, 2.15f, 1.0f) : new Vector3(1.0f, 2.15f, 0.05f));
        Look(LevelKit.Brass, W(df.x + face.x * 0.01f, 0.14f, df.y + face.y * 0.01f), fx ? new Vector3(0.06f, 0.24f, 0.9f) : new Vector3(0.9f, 0.24f, 0.06f));
        Look(LevelKit.Brass, W(df.x + face.x * 0.015f, 2.32f, df.y + face.y * 0.015f), fx ? new Vector3(0.05f, 0.36f, 1.2f) : new Vector3(1.2f, 0.36f, 0.05f));
        Vector2 lp = at + face * 1.45f;
        Look(LevelKit.RivetIron, W(lp.x, 2.8f, lp.y), fx ? new Vector3(0.44f, 0.06f, 0.5f) : new Vector3(0.5f, 0.06f, 0.44f));
        Look(LevelKit.LampCream, W(lp.x, 2.68f, lp.y), new Vector3(0.22f, 0.16f, 0.22f));
        Look(LevelKit.Limestone, W(at.x, 3.1f, at.y), new Vector3(2.8f, 0.26f, 2.8f));
        Look(LevelKit.Limestone, W(at.x, 3.31f, at.y), new Vector3(2.2f, 0.2f, 2.2f));
        Look(LevelKit.RivetIron, W(at.x + 0.7f, 3.75f, at.y - 0.6f), new Vector3(0.2f, 0.35f, 0.2f), null, PrimitiveType.Cylinder);
        stairTop = W(at.x, 3.41f, at.y);
        haveStair = true;
        return true;
    }

    /// A WATER TANK on splayed iron legs, seven to nine metres: a walnut tank with iron hoops, a stepped cap and a
    /// brass finial, a ladder up its side. The legs are solid, the tank a drum you run under (three and a half metres
    /// of air beneath it), pigeons on the cap now and then.
    static bool Tank()
    {
        float h = 7f + 2f * H(70), dia = Mathf.Max(2.6f, h * 0.4f);
        if (!Spot(new Vector3(dia + 0.8f, h, dia + 0.8f), 0.6f, 0.5f, 71, out Vector2 at)) return false;
        float legs = h * 0.48f, tankH = h * 0.4f;
        Vector3 foot = W(at.x, -0.02f, at.y), top = W(at.x, legs, at.y);
        Quaternion yaw = F.rot * Quaternion.Euler(0f, 45f, 0f);
        for (int i = 0; i < 4; i++)
        {
            Quaternion q = yaw * Quaternion.Euler(0f, i * 90f, 0f);
            Vector3 a = foot + q * Vector3.forward * (dia * 0.58f), b = top + q * Vector3.forward * (dia * 0.4f);
            Vector3 d = b - a;
            Quaternion lean = Quaternion.FromToRotation(Vector3.up, d.normalized);
            Look(LevelKit.RivetIron, (a + b) * 0.5f, new Vector3(0.18f, d.magnitude, 0.18f), lean);
            Solid((a + b) * 0.5f, new Vector3(0.3f, d.magnitude * 0.98f, 0.3f), lean);
            Quaternion bq = yaw * Quaternion.Euler(0f, i * 90f + 45f, 0f);
            float r = Mathf.Lerp(dia * 0.58f, dia * 0.4f, 0.4f);
            Look(LevelKit.RivetIron, foot + Vector3.up * (legs * 0.4f) + bq * Vector3.forward * (r * 0.707f), new Vector3(r * 1.414f, 0.08f, 0.08f), bq);
        }
        Look(LevelKit.RivetIron, top - Vector3.up * 0.12f, new Vector3(dia * 0.86f, 0.24f, dia * 0.86f), yaw);
        Vector3 tc = top + Vector3.up * (tankH * 0.5f);
        Look(LevelKit.WoodPanel, tc, new Vector3(dia, tankH * 0.5f, dia), F.rot, PrimitiveType.Cylinder);
        RoofKit.Drum(tc, dia, tankH); Solids++;
        for (int k = 0; k < 3; k++) Look(LevelKit.RivetIron, top + Vector3.up * (tankH * (0.15f + 0.3f * k)), new Vector3(dia + 0.08f, 0.025f, dia + 0.08f), F.rot, PrimitiveType.Cylinder);
        Look(LevelKit.Charcoal, top + Vector3.up * (tankH + 0.07f), new Vector3(dia + 0.3f, 0.08f, dia + 0.3f), F.rot, PrimitiveType.Cylinder);
        Look(LevelKit.Charcoal, top + Vector3.up * (tankH + 0.24f), new Vector3(dia * 0.7f, 0.1f, dia * 0.7f), F.rot, PrimitiveType.Cylinder);
        Look(LevelKit.Charcoal, top + Vector3.up * (tankH + 0.46f), new Vector3(dia * 0.4f, 0.12f, dia * 0.4f), F.rot, PrimitiveType.Cylinder);
        Look(LevelKit.Brass, top + Vector3.up * (tankH + 0.9f), new Vector3(0.12f, 0.3f, 0.12f), F.rot, PrimitiveType.Cylinder);
        Look(LevelKit.RivetIron, top + Vector3.up * (tankH * 0.5f) + F.rot * Vector3.forward * (dia * 0.5f + 0.1f), new Vector3(0.08f, tankH, 0.08f));
        if (life != null && H(72) < 0.5f) { CityKit.Roost(root, top + Vector3.up * (tankH + 0.15f), F.rot * Vector3.right, dia * 0.45f, 4, RoostSeed()); Movers++; }
        return true;
    }

    /// Chimneys and vents, `n` of them, wherever there is room; steam off half the vents
    static void Chimneys(int n)
    {
        for (int i = 0; i < n; i++)
        {
            bool vent = H(110 + i) < 0.4f;
            Vector3 size = vent ? new Vector3(1.0f, 1.9f, 1.0f) : new Vector3(0.9f, 2.2f + 1.6f * H(120 + i), 0.9f);
            if (!Spot(size, 0.5f, 0.7f, 130 + i, out Vector2 at)) continue;
            if (vent) Vent(at); else Chimney(at, size.y);
        }
    }

    /// A brick chimney: iron-banded, soot round its mouth, a stone cap
    static void Chimney(Vector2 at, float h)
    {
        Block(LevelKit.Brick, W(at.x, h * 0.5f - 0.02f, at.y), new Vector3(0.9f, h, 0.9f));
        Look(LevelKit.RivetIron, W(at.x, h * 0.45f, at.y), new Vector3(0.96f, 0.08f, 0.96f));
        Look(LevelKit.Soot, W(at.x, h - 0.22f, at.y), new Vector3(0.94f, 0.3f, 0.94f));
        Look(LevelKit.Limestone, W(at.x, h + 0.07f, at.y), new Vector3(1.12f, 0.2f, 1.12f));
        Look(LevelKit.Dark, W(at.x, h + 0.19f, at.y), new Vector3(0.5f, 0.06f, 0.5f));
    }

    /// A mushroom vent: an iron stack with a cowl, steam off half of them (LevelKit.SteamPuff)
    static void Vent(Vector2 at)
    {
        Look(LevelKit.RivetIron, W(at.x, 0.78f, at.y), new Vector3(0.5f, 0.8f, 0.5f), null, PrimitiveType.Cylinder);
        Solid(W(at.x, 0.8f - 0.02f, at.y), new Vector3(0.5f, 1.6f, 0.5f));
        Look(LevelKit.Dark, W(at.x, 1.58f, at.y), new Vector3(0.6f, 0.05f, 0.6f), null, PrimitiveType.Cylinder);
        Look(LevelKit.RivetIron, W(at.x, 1.74f, at.y), new Vector3(0.95f, 0.16f, 0.95f), null, PrimitiveType.Cylinder);
        if (life != null && H(141) < 0.5f) { LevelKit.SteamPuff(root, W(at.x, 1.92f, at.y), 0.45f); Movers++; }
    }
}
