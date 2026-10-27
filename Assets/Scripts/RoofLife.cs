using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ROOFTOPS ALIVE (2026-10-12; user: "the rooftops should feel more alive as if it was actually part of a city instead
/// of a flat arena"). Once a sky level's route, its neighbours, its mines and its units all stand, this walks every
/// slab RoofKit laid (RoofKit.Roofs) and dresses it as a 1939 roof within the plan's solids: parapets of varied
/// height where the route needs no lip, MANSARD slopes along free edges (solid kit slabs: slides, 32–38°, six metres
/// or more), SKYLIGHTS (a breakable pane over a well 1.4 m deep whose floor is the roof: a break drops you into the
/// well, never to the street), STAIR HEADS (solid, a door and a lamp: kick walls), water tanks on legs you run under,
/// pigeon lofts and roosts that startle (CityKit.Roost), laundry lines with sheets that sway, period billboards with a
/// lamp row, PENTHOUSES with lit windows, a terrace, guests and a three-piece band, chimneys and vents with steam on
/// some, the odd cat; gables and stepped crowns as looks on the outer neighbours; a window-washer's cradle on a big
/// face either side; and the SWITCHING window groups RoofKit.Dress draws a third of its windows in (WindowMat).
/// THE RULES: every candidate is PROBED against everything already built (landings, pads, kerbs, lips, kick walls,
/// the plan's furniture, mines, units), kept three metres off every fast line (RoofKit.Lines), off the lanes between
/// a roof's landings, off the thermals, the respawn spots and the gardens; a roof with anything over it (a lobby
/// ceiling, a deck) is left alone. Everything walk-into-able is a SurfaceKit.Block (baked look + collider), every look
/// alone is baked and flushed here as "RoofLife" (nothing on these levels is welded), every mover carries a
/// MonoBehaviour (RoofLifeAnimator, WashCradle, WindowSwitch, Roost). No Lights: lamps are unlit boxes under the bloom.
/// </summary>
public static partial class RoofLife
{
    static Transform root;
    static MovementConfig cfg;
    static District district;
    static CityLife life;
    static bool night;
    static RoofLifeAnimator anim;
    static readonly List<Vector3> respawns = new List<Vector3>();
    static readonly List<Vector3> ports = new List<Vector3>();
    static readonly Collider[] hits = new Collider[256];   // a hub roof's halo scan can touch a lot: never truncate it
    static readonly List<Vector3> cand = new List<Vector3>();
    static readonly bool[] edgeTaken = new bool[4];
    static int roosts;
    /// Tallies for the console
    public static int Solids, Looks, Movers, RoofsDressed;

    struct Reserved { public Vector3 top; public int edge; public Vector2 at; }
    static readonly List<Reserved> reserved = new List<Reserved>();

    /// A penthouse asked for by hand (the Exchange): the roof's drawn top, the edge its back is to (0 south, 1 north,
    /// 2 west, 3 east) and the body's centre in the roof's own x/z. Taken before the roof's seeded pieces.
    public static void ReservePenthouse(Vector3 roofTop, int edge, Vector2 localCentre) => reserved.Add(new Reserved { top = roofTop, edge = edge, at = localCentre });

    public static void Dress(Transform levelRoot, MovementConfig config, District d, CityLife cityLife, List<RoofKit.Block> blocks, bool isNight)
    {
        root = levelRoot; cfg = config; district = d; life = cityLife; night = isNight;
        Solids = Looks = Movers = RoofsDressed = roosts = 0;
        Physics.SyncTransforms();
        respawns.Clear();
        if (LevelSections.Last != null) foreach (var c in LevelSections.Last.checkpoints) respawns.Add(c.at);
        anim = null;
        if (life != null)
        {
            var go = new GameObject("RoofLife");
            go.transform.SetParent(root, false);
            anim = go.AddComponent<RoofLifeAnimator>();
            go.AddComponent<WindowSwitch>().Init(SwitchMats(), night);
        }
        var roofs = new List<RoofKit.RoofRec>(RoofKit.Roofs);
        for (int i = 0; i < roofs.Count; i++) DressRoof(roofs[i], i);
        if (blocks != null)
        {
            foreach (var b in blocks) if (!b.full) OuterLooks(b);
            Cradles(blocks);
        }
        reserved.Clear();
        if (anim != null) anim.Finish();
        SurfaceKit.Near.Flush(root, "RoofLife", true);
        Physics.SyncTransforms();
        Debug.Log($"RoofLife: {RoofsDressed} roofs dressed, {Solids} solids, {Looks} baked looks, {Movers} movers");
    }

    // ------------------------------------------------------------ the windows that switch

    const int Groups = 8;
    static Material[] switchMats;

    static Material[] SwitchMats()
    {
        if (switchMats != null && switchMats[0] != null) return switchMats;
        switchMats = new Material[Groups];
        for (int i = 0; i < Groups; i++) switchMats[i] = new Material(LevelKit.WindowAmber) { name = "WindowSwitch" + i };
        return switchMats;
    }

    /// The material a dressed window is drawn in (RoofKit.Dress, the penthouses): amber, or for a third of them one
    /// of eight shared SWITCHING groups (WindowSwitch takes a group dark for a while now and then), by hash, so a
    /// tower's pattern never changes between builds
    public static Material WindowMat(int seed, int face, int row, int col)
    {
        float h = CityTextures.Hash01(seed * 7 + face, row * 131 + col, 4421);
        if (h > 0.34f) return LevelKit.WindowAmber;
        return SwitchMats()[(int)(h * 1000f) % Groups];
    }

    // ------------------------------------------------------------ one roof

    /// The roof being dressed: its slab's top at `c`, its yaw, its half extents, its seed
    struct Frame { public Vector3 c; public Quaternion rot; public float hx, hz; public int seed; public bool neighbour; }
    static Frame F;
    static Vector3 stairTop;
    static bool haveStair;

    static Vector3 W(float lx, float ly, float lz) => F.c + F.rot * new Vector3(lx, ly, lz);
    static float H(int k) => CityTextures.Hash01(F.seed, k, 977);
    static int RoostSeed() => F.seed * 13 + roosts++;

    static void DressRoof(RoofKit.RoofRec r, int index)
    {
        if (r.w < 6f || r.d < 6f) return;
        F = new Frame
        {
            c = r.top + Vector3.up * RoofKit.Proud, rot = Quaternion.Euler(0f, r.az, 0f), hx = r.w * 0.5f, hz = r.d * 0.5f,
            seed = Mathf.RoundToInt(r.top.x * 7.3f + r.top.z * 1.7f + r.top.y * 0.9f) + index * 3, neighbour = r.neighbour,
        };
        if (Covered()) return;
        haveStair = false;
        for (int e = 0; e < 4; e++) edgeTaken[e] = false;
        FindPorts();
        RoofsDressed++;
        if (r.d < 10f || r.w < 10f) { BladeEnds(); return; }
        foreach (var p in reserved)
            if ((p.top - r.top).sqrMagnitude < 0.25f) Penthouse(p.edge, p.at);
        if (F.neighbour) NeighbourRoof(); else RouteRoof();
    }

    /// Something within twelve metres over this roof (a sky lobby's ceiling, a deck, a canopy): leave it alone
    static bool Covered()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3 p = i == 0 ? F.c : W((i % 2 == 0 ? -1f : 1f) * F.hx * 0.55f, 0f, (i < 3 ? -1f : 1f) * F.hz * 0.55f);
            if (Physics.Raycast(p + Vector3.up * 0.3f, Vector3.up, 12f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return true;
        }
        return false;
    }

    /// Where the ways arrive on this roof: a landing, a pad, a fire-escape platform. The lanes between them stay clear
    static void FindPorts()
    {
        ports.Clear();
        int n = Physics.OverlapBoxNonAlloc(F.c + Vector3.up * 1.5f, new Vector3(F.hx + 1f, 1.5f, F.hz + 1f), hits, F.rot, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            string nm = hits[i].gameObject.name;
            if (nm == "Landing" || nm == "JumpPad" || nm == "Escape") ports.Add(hits[i].bounds.center);
        }
    }

    /// Is a piece of `size` standing at roof-local `lc` clear? Nothing already there (with `margin` of air round it),
    /// three metres from any authored tech, landing or pane, two from a beacon, and off every line: the fast lines,
    /// the lanes between this roof's ports, the respawn spots, the thermals, the gardens
    static bool Free(Vector2 lc, Vector3 size, float margin = 0.4f)
    {
        if (Mathf.Abs(lc.x) + size.x * 0.5f > F.hx - 0.15f || Mathf.Abs(lc.y) + size.z * 0.5f > F.hz - 0.15f) return false;
        Vector3 centre = W(lc.x, 0.02f + size.y * 0.5f, lc.y);
        Vector3 half = new Vector3(size.x * 0.5f + margin, size.y * 0.5f, size.z * 0.5f + margin);
        if (Physics.OverlapBoxNonAlloc(centre, half, hits, F.rot, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide) > 0) return false;
        int n = Physics.OverlapBoxNonAlloc(centre, half + new Vector3(3f, 0.5f, 3f), hits, F.rot, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            string nm = hits[i].gameObject.name;
            if (nm == "KickWall" || nm == "LipLedge" || nm == "ParapetLedge" || nm == "KickStack" || nm == "BoostPillar" || nm == "Pillar"
                || nm == "Landing" || nm == "LandingRamp" || nm == "JumpPad" || nm == "Escape" || nm == "BeaconPost" || nm == "Glass") return false;
        }
        Vector3 hw = new Vector3(size.x * 0.5f, 0f, size.z * 0.5f);
        foreach (var l in RoofKit.Lines) if (NearSegment(centre, hw, l.a, l.b, 3.2f)) return false;
        for (int i = 0; i < ports.Count; i++)
            for (int j = i + 1; j < ports.Count; j++) if (NearSegment(centre, hw, ports[i], ports[j], 3.2f)) return false;
        foreach (var p in respawns) if (NearSegment(centre, hw, p, p, 2.6f)) return false;
        if (district != null)
            foreach (var u in district.updrafts)
                if (centre.y > u.y0 - 3f && centre.y < u.y1 + 1f && NearSegment(centre, hw, u.centre, u.centre, u.half + 2.5f, true)) return false;
        foreach (var k in RoofKit.KeepOut) if (InKeep(centre, hw, k)) return false;
        return true;
    }

    /// Does a rect (centre, half extents, in the roof's frame) come within `dist` of the segment a–b, in plan? Heights
    /// more than four metres apart are other roofs' lines (`anyHeight` for a thermal's column)
    static bool NearSegment(Vector3 centre, Vector3 hw, Vector3 a, Vector3 b, float dist, bool anyHeight = false)
    {
        if (!anyHeight && Mathf.Abs(a.y - centre.y) > 4.5f && Mathf.Abs(b.y - centre.y) > 4.5f) return false;
        float d2 = dist * dist;
        int nx = Mathf.Max(1, Mathf.CeilToInt(hw.x * 2f)), nz = Mathf.Max(1, Mathf.CeilToInt(hw.z * 2f));
        for (int i = 0; i <= nx; i++)
            for (int j = 0; j <= nz; j++)
            {
                bool rim = i == 0 || i == nx || j == 0 || j == nz;
                if (!rim && !(i == nx / 2 && j == nz / 2)) continue;
                Vector3 p = centre + F.rot * new Vector3(-hw.x + hw.x * 2f * i / nx, 0f, -hw.z + hw.z * 2f * j / nz);
                if (SegDist2(p, a, b) < d2) return true;
            }
        return false;
    }

    static float SegDist2(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector2 P = new Vector2(p.x, p.z), A = new Vector2(a.x, a.z), AB = new Vector2(b.x - a.x, b.z - a.z);
        float len2 = AB.sqrMagnitude;
        float t = len2 > 1e-4f ? Mathf.Clamp01(Vector2.Dot(P - A, AB) / len2) : 0f;
        return (P - (A + AB * t)).sqrMagnitude;
    }

    static bool InKeep(Vector3 centre, Vector3 hw, RoofKit.KeepRec k)
    {
        if (Mathf.Abs(k.centre.y - centre.y) > 4f) return false;
        Quaternion inv = Quaternion.Inverse(Quaternion.Euler(0f, k.az, 0f));
        for (int i = 0; i < 5; i++)
        {
            Vector3 p = i == 0 ? centre : centre + F.rot * new Vector3((i % 2 == 0 ? -1f : 1f) * hw.x, 0f, (i < 3 ? -1f : 1f) * hw.z);
            Vector3 l = inv * (p - k.centre);
            if (Mathf.Abs(l.x) < k.hx && Mathf.Abs(l.z) < k.hz) return true;
        }
        return false;
    }

    /// The first free spot for a piece: a seeded scan over a two-metre grid on the roof, `edgeBias` > 0 trying the
    /// spots nearest the edges first (a stair head), < 0 the middle first (a skylight). Twenty probes at most.
    static bool Spot(Vector3 size, float margin, float edgeBias, int salt, out Vector2 at)
    {
        at = default;
        float sx = F.hx - size.x * 0.5f - 0.4f, sz = F.hz - size.z * 0.5f - 0.4f;
        if (sx < 0f || sz < 0f) return false;
        cand.Clear();
        int nx = Mathf.Max(1, Mathf.FloorToInt(sx)), nz = Mathf.Max(1, Mathf.FloorToInt(sz));
        for (int i = 0; i <= nx; i++)
            for (int j = 0; j <= nz; j++)
            {
                float lx = -sx + 2f * sx * i / nx, lz = -sz + 2f * sz * j / nz;
                float edge = Mathf.Max(Mathf.Abs(lx) / Mathf.Max(0.1f, sx), Mathf.Abs(lz) / Mathf.Max(0.1f, sz));
                cand.Add(new Vector3(lx, lz, -edgeBias * edge + CityTextures.Hash01(F.seed + salt, i * 97 + j, 61) * 0.6f));
            }
        cand.Sort((p, q) => p.z.CompareTo(q.z));
        int tries = Mathf.Min(cand.Count, 20);
        for (int k = 0; k < tries; k++)
            if (Free(new Vector2(cand[k].x, cand[k].y), size, margin)) { at = new Vector2(cand[k].x, cand[k].y); return true; }
        return false;
    }

    // ------------------------------------------------------------ what each kind of roof gets

    /// A roof on the route (a stack's top, a terrace, a tier, a hub): the full dressing, as much as the probes allow
    static void RouteRoof()
    {
        bool big = F.hx >= 12f && F.hz >= 12f;
        int m = 0;
        for (int e = 0; e < 4 && m < 2; e++) if (H(10 + e) < 0.7f && Mansard(e)) m++;
        for (int e = 0; e < 4; e++) Parapet(e);
        if (big) { Skylights(); StairHead(); }
        else if (H(20) < 0.6f) StairHead();
        if (H(21) < 0.55f) Tank();
        if (H(22) < 0.7f) Laundry();
        if (H(23) < 0.6f) Loft(); else RoostOnDeck();
        Chimneys(2 + (int)(H(24) * 2f));
        if (H(25) < 0.35f) Billboard(1, false);
        if (H(26) < 0.25f) Cat();
    }

    /// A neighbour's roof (full, gridded, a landing off the route): lighter, a party on the biggest of them
    static void NeighbourRoof()
    {
        bool big = F.hx >= 9f && F.hz >= 8f;
        int toward = F.c.x < 0f ? 3 : 2;   // the edge that looks at the route
        if (!(big && H(200) < 0.45f && Penthouse(toward == 3 ? 2 : 3)) && H(201) < 0.4f) Mansard(toward == 3 ? 2 : 3);
        for (int e = 0; e < 4; e++) Parapet(e);
        if (H(202) < 0.5f) StairHead();
        if (big && H(203) < 0.5f) Skylights();
        if (H(204) < 0.5f) Laundry();
        if (H(205) < 0.3f) Loft(); else if (H(206) < 0.25f) RoostOnDeck();
        Chimneys(1 + (int)(H(207) * 2f));
        if (H(208) < 0.3f) Billboard(toward, true);
        if (H(209) < 0.12f) Cat();
    }

    /// A twin blade's top (forty by nine): the middle is the pad's and the bridges'; the ends get a chimney, a kerb across, a roost
    static void BladeEnds()
    {
        bool alongX = F.hx > F.hz;
        float half = alongX ? F.hx : F.hz;
        for (int s = -1; s <= 1; s += 2)
        {
            Vector2 end = alongX ? new Vector2(s * (half - 2.4f), 0f) : new Vector2(0f, s * (half - 2.4f));
            if (Free(end, new Vector3(0.9f, 2.8f, 0.9f), 0.5f)) Chimney(end, 2.6f + H(190 + s) * 1.2f);
            Vector2 kerb = alongX ? new Vector2(s * (half - 0.35f), 0f) : new Vector2(0f, s * (half - 0.35f));
            Vector3 ks = alongX ? new Vector3(0.4f, 0.7f, F.hz * 2f - 0.8f) : new Vector3(F.hx * 2f - 0.8f, 0.7f, 0.4f);
            if (!Free(kerb, ks, 0.2f)) continue;
            Block(LevelKit.Limestone, W(kerb.x, 0.33f, kerb.y), ks);
            Look(LevelKit.Limestone, W(kerb.x, 0.73f, kerb.y), ks + new Vector3(0.14f, -0.58f, 0.14f));   // the cap, a centimetre into the kerb
        }
        if (life == null || H(195) > 0.5f) return;
        Vector2 r = alongX ? new Vector2(F.hx - 5.5f, F.hz - 0.8f) : new Vector2(F.hx - 0.8f, F.hz - 5.5f);
        if (Free(r, new Vector3(2.6f, 0.4f, 0.8f), 0.2f)) { CityKit.Roost(root, W(r.x, 0f, r.y), F.rot * (alongX ? Vector3.right : Vector3.forward), 1.1f, 4, RoostSeed()); Movers++; }
    }

    // ------------------------------------------------------------ the counted primitives

    static void Block(Material m, Vector3 c, Vector3 s, Quaternion? r = null) { SurfaceKit.Block(m, c, s, r ?? F.rot); Solids++; Looks++; }
    static void Look(Material m, Vector3 c, Vector3 s, Quaternion? r = null, PrimitiveType t = PrimitiveType.Cube) { SurfaceKit.Look(m, c, s, r ?? F.rot, t); Looks++; }
    static void Solid(Vector3 c, Vector3 s, Quaternion? r = null) { SurfaceKit.Solid(c, s, r ?? F.rot); Solids++; }

    static Material ink, skin;
    static Material InkMat => ink ??= Flat(new Color(0.06f, 0.06f, 0.07f), 0.5f);
    static Material SkinMat => skin ??= Flat(new Color(0.6f, 0.46f, 0.36f), 0.3f);

    static Material Flat(Color c, float smooth)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smooth);
        return m;
    }
}
