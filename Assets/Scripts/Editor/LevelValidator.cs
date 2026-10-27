using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// THE LEVEL VALIDATOR (Phase 2, 2026-10-27; the user: "a validator that simulates jump arcs using the metrics and
/// flags impossible gaps, trivially easy gaps, small steps or ledges that snag the player, low ceilings, and anywhere
/// that forces a hard slowdown"). It measures and reports; it changes nothing.
///
/// Momentum/Level Validator/Validate All Levels builds every level outside Play Mode exactly as the game does (the
/// same builders, the saved edits laid on, the coplanar fix), with everything else in the open scenes hidden while it
/// works, reads every solid collider, and turns the boxes into PLATFORMS (walkable tops), WALLS (faces over two
/// metres) and CEILINGS. Between platforms it asks the metrics (LevelMetricsMath, the motor's own rules) what it
/// takes to get from one to the next: a walk, a drop, a step (the motor has no step-up, so a step is a jump or a
/// snag), a jump at the lowest entry speed that clears the gap with half a metre to spare, a double jump, or a wall
/// run with the jump off it. Then it walks THE MAIN LINE from the spawn through the checkpoints to the finish: the
/// path whose hardest move is the easiest (ties by total effort), and reports what it meets, ranked by how much each
/// thing hurts flow and speed. Every level gets Docs/Design/validation/<level>.md, an overhead render and a drawn
/// plan with the findings numbered on them; SUMMARY.md ranks the levels. Approximate by design: boxes are read as
/// their top faces, gaps as the straight line between rectangles, the strafe is not counted.
/// </summary>
public static class LevelValidator
{
    const string Menu = "Momentum/Level Validator/";
    const string OutDir = "Docs/Design/validation";
    const string ConfigPath = "Assets/Config/MovementConfig.asset";
    static readonly LevelKind[] AllLevels =
    {
        LevelKind.Tutorial, LevelKind.Level1, LevelKind.Level2, LevelKind.Level3, LevelKind.Level4, LevelKind.Level5, LevelKind.Level6,
        LevelKind.Level7, LevelKind.Level8, LevelKind.Level9, LevelKind.Level10, LevelKind.Level11, LevelKind.Level12, LevelKind.Level13,
    };
    static readonly float[] Speeds = { 8f, 10f, 12f, 14f, 16f, 18f, 20f, 24f };
    const float Margin = 0.5f;          // the landing's margin past the gap: the body's radius and a hair
    const float Touch = 0.35f;          // rectangles this close are one floor
    const float StepMin = 0.08f;        // a rise this small is the capsule's business; above it the motor stops
    const float StepMax = 1.45f;        // the apex a standing jump reaches, less the body
    const float ReachLimit = 42f;            // no pair further apart than this is asked about

    // ---------------------------------------------------------------- the menu

    [MenuItem(Menu + "Validate All Levels", false, 0)]
    public static void ValidateAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.Log("LevelValidator: works outside Play Mode."); return; }
        var config = AssetDatabase.LoadAssetAtPath<MovementConfig>(ConfigPath);
        if (config == null) { Debug.LogError("LevelValidator: " + ConfigPath + " not found."); return; }
        var results = new List<Result>();
        try
        {
            for (int i = 0; i < AllLevels.Length; i++)
            {
                EditorUtility.DisplayProgressBar("Level Validator", "Building and reading " + AllLevels[i] + " (" + (i + 1) + "/" + AllLevels.Length + ")", (float)i / AllLevels.Length);
                try { results.Add(Validate(AllLevels[i], config)); }
                catch (Exception e) { Debug.LogError("LevelValidator: " + AllLevels[i] + " failed: " + e); }
            }
            WriteSummary(results);
        }
        finally { EditorUtility.ClearProgressBar(); }
        Debug.Log("LevelValidator: " + results.Count + " level(s) validated → " + OutDir + "/SUMMARY.md");
    }

    [MenuItem(Menu + "Validate This Scene's Level", false, 1)]
    public static void ValidateThis()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.Log("LevelValidator: works outside Play Mode."); return; }
        var b = LevelEditor.FindBootstrap();
        if (b == null) { Debug.LogError("LevelValidator: open a level scene (Level1 … Level13, Tutorial)."); return; }
        var config = b.config != null ? b.config : AssetDatabase.LoadAssetAtPath<MovementConfig>(ConfigPath);
        try
        {
            EditorUtility.DisplayProgressBar("Level Validator", "Building and reading " + b.level, 0.3f);
            var r = Validate(b.level, config);
            WriteSummary(new List<Result> { r });
            Debug.Log("LevelValidator: " + b.level + " → " + OutDir + "/" + b.level + ".md\n" + r.summaryLine);
        }
        finally { EditorUtility.ClearProgressBar(); }
    }

    // ---------------------------------------------------------------- the build, isolated

    class Built
    {
        public LevelKind kind;
        public List<GameObject> roots = new List<GameObject>();
        public List<GameObject> hidden = new List<GameObject>();
        public LevelSections.Layout layout;
        public Vector3 spawn;
    }

    static Built Build(LevelKind kind, MovementConfig config)
    {
        var b = new Built { kind = kind };
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded) continue;
            foreach (var go in s.GetRootGameObjects()) if (go.activeSelf) { go.SetActive(false); b.hidden.Add(go); }
        }
        var scene = SceneManager.GetActiveScene();
        var before = new HashSet<GameObject>(scene.GetRootGameObjects());
        LevelSections.Last = null;
        b.spawn = GameBootstrap.BuildLevel(kind, config);
        b.roots = LevelEdits.NewRoots(scene, before);
        LevelEdits.Apply(LevelEdits.Load(kind), b.roots, config);   // the saved hand edits, as the game lays them on
        CoplanarFixer.Run();
        Physics.SyncTransforms();
        b.roots = LevelEdits.NewRoots(scene, before);
        b.layout = LevelSections.Last;
        return b;
    }

    static void Tear(Built b)
    {
        foreach (var r in b.roots) if (r != null) UnityEngine.Object.DestroyImmediate(r);
        foreach (var go in b.hidden) if (go != null) go.SetActive(true);
    }

    // ---------------------------------------------------------------- the geometry

    public class Plat
    {
        public int id; public string name; public Collider col;
        public float minX, maxX, minZ, maxZ;   // the top face's footprint
        public Vector3 c, n;                   // a point on the top plane and its normal
        public float slope;                    // degrees
        public bool lip, pad, kick;
        public float YAt(float x, float z) => c.y - (n.x * (x - c.x) + n.z * (z - c.z)) / Mathf.Max(0.2f, n.y);
        public float Width => maxX - minX;
        public float Depth => maxZ - minZ;
        public Vector3 Centre => new Vector3((minX + maxX) * 0.5f, YAt((minX + maxX) * 0.5f, (minZ + maxZ) * 0.5f), (minZ + maxZ) * 0.5f);
        /// The direction that climbs the slope, flat
        public Vector3 Uphill { get { var u = new Vector3(-n.x, 0f, -n.z); return u.sqrMagnitude > 1e-4f ? u.normalized : Vector3.zero; } }
    }
    public class Wall { public string name; public Vector3 c, n, u; public float halfLen, bottom, top; public bool kick; }
    public class Ceil { public Collider col; public float minX, maxX, minZ, maxZ; public Vector3 c, n; public float YAt(float x, float z) => c.y - (n.x * (x - c.x) + n.z * (z - c.z)) / Mathf.Min(-0.2f, n.y); }

    public class Geometry
    {
        public List<Plat> plats = new List<Plat>();
        public List<Wall> walls = new List<Wall>();
        public List<Ceil> ceils = new List<Ceil>();
        public Bounds bounds;
    }

    public static Geometry Read(IEnumerable<GameObject> roots)
    {
        var g = new Geometry();
        bool any = false;
        foreach (var root in roots)
            foreach (var col in root.GetComponentsInChildren<Collider>(false))
            {
                if (col == null || !col.enabled || col.isTrigger) continue;
                var box = col as BoxCollider;
                if (box == null) continue;
                var t = box.transform;
                Vector3 ls = t.lossyScale;
                Vector3 half = new Vector3(Mathf.Abs(box.size.x * ls.x), Mathf.Abs(box.size.y * ls.y), Mathf.Abs(box.size.z * ls.z)) * 0.5f;
                if (Mathf.Max(half.x, Mathf.Max(half.y, half.z)) < 0.15f) continue;
                Vector3 wc = t.TransformPoint(box.center);
                Vector3[] axes = { t.right, t.up, t.forward };
                float[] ext = { half.x, half.y, half.z };
                string name = col.gameObject.name;
                bool lip = name.StartsWith("Lip") || name.EndsWith("Lip") || name.Contains("Plinth") || name.Contains("Kerb");
                bool pad = name == "JumpPad", kick = name == "KickWall";
                for (int a = 0; a < 3; a++)
                    for (int sgn = -1; sgn <= 1; sgn += 2)
                    {
                        Vector3 nf = axes[a] * sgn;
                        Vector3 fc = wc + nf * ext[a];
                        int b1 = (a + 1) % 3, b2 = (a + 2) % 3;
                        Vector3 e1 = axes[b1] * ext[b1], e2 = axes[b2] * ext[b2];
                        if (nf.y >= 0.707f)
                        {
                            var p = new Plat { id = g.plats.Count, name = name, col = col, c = fc, n = nf, slope = Vector3.Angle(nf, Vector3.up), lip = lip, pad = pad, kick = kick };
                            Footprint(fc, e1, e2, out p.minX, out p.maxX, out p.minZ, out p.maxZ);
                            if (p.Width < 0.3f || p.Depth < 0.3f) continue;
                            g.plats.Add(p);
                            var bb = new Bounds(fc, Vector3.zero); bb.Encapsulate(fc + e1 + e2); bb.Encapsulate(fc - e1 - e2); bb.Encapsulate(fc + e1 - e2); bb.Encapsulate(fc - e1 + e2);
                            if (!any) { g.bounds = bb; any = true; } else g.bounds.Encapsulate(bb);
                        }
                        else if (nf.y <= -0.7f)
                        {
                            var cl = new Ceil { col = col, c = fc, n = nf };
                            Footprint(fc, e1, e2, out cl.minX, out cl.maxX, out cl.minZ, out cl.maxZ);
                            g.ceils.Add(cl);
                        }
                        else if (Mathf.Abs(nf.y) < 0.35f)
                        {
                            // a vertical face: its height is the extent along the more vertical in-plane axis
                            bool e1Up = Mathf.Abs(axes[b1].y) > Mathf.Abs(axes[b2].y);
                            float height = 2f * (e1Up ? ext[b1] : ext[b2]), halfLen = e1Up ? ext[b2] : ext[b1];
                            Vector3 u = e1Up ? axes[b2] : axes[b1];
                            if (height < 2f || halfLen < 0.5f) continue;
                            Vector3 nh = new Vector3(nf.x, 0f, nf.z).normalized;
                            g.walls.Add(new Wall { name = name, c = fc, n = nh, u = new Vector3(u.x, 0f, u.z).normalized, halfLen = halfLen, bottom = fc.y - height * 0.5f, top = fc.y + height * 0.5f, kick = kick });
                        }
                    }
            }
        // a top with a solid underside within a metre of it is not a floor (a plinth under a deck, a slab's own belly)
        g.plats.RemoveAll(p => Covered(g, p));
        for (int i = 0; i < g.plats.Count; i++) g.plats[i].id = i;
        return g;
    }

    static bool Covered(Geometry g, Plat p)
    {
        var at = p.Centre;
        foreach (var c in g.ceils)
        {
            if (c.col == p.col) continue;
            if (at.x < c.minX || at.x > c.maxX || at.z < c.minZ || at.z > c.maxZ) continue;
            float h = c.YAt(at.x, at.z) - at.y;
            if (h > -0.02f && h < 1.0f) return true;   // a slab sitting on it (h = 0) covers it as surely as one a hair above
        }
        return false;
    }

    static void Footprint(Vector3 c, Vector3 e1, Vector3 e2, out float minX, out float maxX, out float minZ, out float maxZ)
    {
        minX = maxX = c.x; minZ = maxZ = c.z;
        foreach (var k in new[] { c + e1 + e2, c + e1 - e2, c - e1 + e2, c - e1 - e2 })
        {
            minX = Mathf.Min(minX, k.x); maxX = Mathf.Max(maxX, k.x);
            minZ = Mathf.Min(minZ, k.z); maxZ = Mathf.Max(maxZ, k.z);
        }
    }

    // ---------------------------------------------------------------- the reach

    class Reach
    {
        public LevelMetricsMath.Values v;
        const float DyMin = -16f, DyMax = 1.6f, DyStep = 0.2f;
        float[,] plain, dbl;
        public Reach(LevelMetricsMath.Values values)
        {
            v = values;
            int n = (int)((DyMax - DyMin) / DyStep) + 1;
            plain = new float[Speeds.Length, n]; dbl = new float[Speeds.Length, n];
            for (int s = 0; s < Speeds.Length; s++)
                for (int i = 0; i < n; i++)
                {
                    float dy = DyMin + i * DyStep;
                    var a = LevelMetricsMath.Jump(v, Speeds[s], v.jumpImpulse, dy, false);
                    plain[s, i] = a.apex + 0.02f >= dy ? a.distance : 0f;
                    var d = LevelMetricsMath.DoubleJump(v, Speeds[s], dy, true, false);
                    dbl[s, i] = d.apex + 0.02f >= dy ? d.distance : 0f;
                }
        }
        float Look(float[,] table, int s, float dy)
        {
            if (dy > DyMax) return 0f;
            float f = (Mathf.Clamp(dy, DyMin, DyMax) - DyMin) / DyStep;
            int i = Mathf.Clamp(Mathf.CeilToInt(f), 0, table.GetLength(1) - 1);   // the next bucket up: the stricter one
            return table[s, i];
        }
        public float Plain(int s, float dy) => Look(plain, s, dy);
        public float Double(int s, float dy) => Look(dbl, s, dy);
    }

    // ---------------------------------------------------------------- the graph

    public enum Move { Walk, Drop, Step, Jump, DoubleJump, WallRun }
    public class Edge { public int from, to; public Move move; public float gap, dy, speed, cost; public bool trivial; public Vector3 a, b; public Wall wall; }

    static List<Edge>[] Graph(Geometry g, Reach r)
    {
        int n = g.plats.Count;
        var edges = new List<Edge>[n];
        for (int i = 0; i < n; i++) edges[i] = new List<Edge>();
        for (int i = 0; i < n; i++)
        {
            var P = g.plats[i];
            for (int j = 0; j < n; j++)
            {
                if (i == j) continue;
                var Q = g.plats[j];
                float gap = RectGap(P, Q, out Vector3 a, out Vector3 b);
                if (gap > ReachLimit) continue;
                float ya = P.YAt(a.x, a.z), yb = Q.YAt(b.x, b.z);
                float dy = yb - ya;
                if (dy > 2f || dy < -40f) continue;
                a.y = ya; b.y = yb;
                var e = new Edge { from = i, to = j, gap = gap, dy = dy, a = a, b = b };
                if (gap <= Touch && Mathf.Abs(dy) <= StepMin) { e.move = Move.Walk; e.cost = 0f; }
                else if (gap <= Touch && dy < -StepMin) { e.move = Move.Drop; e.cost = 0.5f; }
                else if (gap <= Touch && dy <= StepMax) { e.move = Move.Step; e.cost = 6f; }
                else
                {
                    float need = gap + Margin;
                    int s = -1;
                    for (int k = 0; k < Speeds.Length; k++) if (r.Plain(k, dy) >= need) { s = k; break; }
                    if (s >= 0) { e.move = Move.Jump; e.speed = Speeds[s]; e.cost = Speeds[s]; e.trivial = r.Plain(0, dy) >= 1.5f * need; }
                    else
                    {
                        for (int k = 0; k < Speeds.Length; k++) if (r.Double(k, dy) >= need) { s = k; break; }
                        if (s >= 0) { e.move = Move.DoubleJump; e.speed = Speeds[s]; e.cost = Speeds[s] + 4f; }
                        else
                        {
                            var w = WallAlong(g, a, b, ya);
                            if (w == null) continue;
                            int ws = -1;
                            for (int k = 0; k < Speeds.Length; k++)
                            {
                                var run = LevelMetricsMath.Run(r.v, Speeds[k], 0f);
                                var off = LevelMetricsMath.WallJump(r.v, Speeds[k], run.exitVy);
                                float arc = LevelMetricsMath.Jump(r.v, off.h, off.vy, dy, false).distance;
                                if (run.levelDistance + arc >= need) { ws = k; break; }
                            }
                            if (ws < 0) continue;
                            e.move = Move.WallRun; e.speed = Speeds[ws]; e.cost = Speeds[ws] + 1f; e.wall = w;
                        }
                    }
                }
                edges[i].Add(e);
            }
        }
        return edges;
    }

    /// The straight-line gap between two footprints and the nearest points on each
    static float RectGap(Plat P, Plat Q, out Vector3 a, out Vector3 b)
    {
        float ax = Mathf.Clamp(Mathf.Clamp((Q.minX + Q.maxX) * 0.5f, P.minX, P.maxX), Q.minX, Q.maxX);
        float az = Mathf.Clamp(Mathf.Clamp((Q.minZ + Q.maxZ) * 0.5f, P.minZ, P.maxZ), Q.minZ, Q.maxZ);
        // the point of Q nearest P's rectangle, then P's point nearest that
        float bx = Mathf.Clamp(Mathf.Clamp((P.minX + P.maxX) * 0.5f, Q.minX, Q.maxX), P.minX, P.maxX);
        float bz = Mathf.Clamp(Mathf.Clamp((P.minZ + P.maxZ) * 0.5f, Q.minZ, Q.maxZ), P.minZ, P.maxZ);
        float dx = Mathf.Max(0f, Mathf.Max(P.minX - Q.maxX, Q.minX - P.maxX));
        float dz = Mathf.Max(0f, Mathf.Max(P.minZ - Q.maxZ, Q.minZ - P.maxZ));
        float gap = Mathf.Sqrt(dx * dx + dz * dz);
        // the points: P's edge nearest Q and Q's edge nearest P, along the line between the centres
        float pcx = (P.minX + P.maxX) * 0.5f, pcz = (P.minZ + P.maxZ) * 0.5f, qcx = (Q.minX + Q.maxX) * 0.5f, qcz = (Q.minZ + Q.maxZ) * 0.5f;
        a = new Vector3(Mathf.Clamp(qcx, P.minX, P.maxX), 0f, Mathf.Clamp(qcz, P.minZ, P.maxZ));
        b = new Vector3(Mathf.Clamp(pcx, Q.minX, Q.maxX), 0f, Mathf.Clamp(pcz, Q.minZ, Q.maxZ));
        if (gap <= 0f)
        {
            // overlapping or touching: the shared middle
            float ox = (Mathf.Max(P.minX, Q.minX) + Mathf.Min(P.maxX, Q.maxX)) * 0.5f, oz = (Mathf.Max(P.minZ, Q.minZ) + Mathf.Min(P.maxZ, Q.maxZ)) * 0.5f;
            a = new Vector3(Mathf.Clamp(ox, P.minX, P.maxX), 0f, Mathf.Clamp(oz, P.minZ, P.maxZ));
            b = new Vector3(Mathf.Clamp(ox, Q.minX, Q.maxX), 0f, Mathf.Clamp(oz, Q.minZ, Q.maxZ));
        }
        _ = ax; _ = az; _ = bx; _ = bz;
        return gap;
    }

    /// A wall beside the line from a to b, parallel to it, standing from the floor to over head height
    static Wall WallAlong(Geometry g, Vector3 a, Vector3 b, float floorY)
    {
        Vector3 d = b - a; d.y = 0f;
        float len = d.magnitude;
        if (len < 1f) return null;
        d /= len;
        Vector3 mid = (a + b) * 0.5f;
        foreach (var w in g.walls)
        {
            if (Mathf.Abs(Vector3.Dot(w.u, d)) < 0.9f) continue;
            float lateral = Mathf.Abs(Vector3.Dot(mid - w.c, w.n));
            if (lateral > 2.2f) continue;
            float along = Vector3.Dot(mid - w.c, w.u);
            if (Mathf.Abs(along) > w.halfLen + len * 0.5f) continue;
            if (w.bottom > floorY + 0.8f || w.top < floorY + 1.6f) continue;
            return w;
        }
        return null;
    }

    // ---------------------------------------------------------------- the main line

    public class Segment { public string name; public Vector3 from, to; public int start = -1, end = -1; public List<Edge> path = new List<Edge>(); public List<Vector3> entries = new List<Vector3>(); public bool broken; public float gate; public Edge gateEdge; public List<int> reached; public string deadEnd; public Vector3 deadAt; }

    static int PlatUnder(Geometry g, Vector3 p)
    {
        int best = -1; float bestScore = float.MaxValue;
        for (int i = 0; i < g.plats.Count; i++)
        {
            var P = g.plats[i];
            float dx = Mathf.Max(0f, Mathf.Max(P.minX - p.x, p.x - P.maxX)), dz = Mathf.Max(0f, Mathf.Max(P.minZ - p.z, p.z - P.maxZ));
            float horiz = Mathf.Sqrt(dx * dx + dz * dz);
            float y = P.YAt(Mathf.Clamp(p.x, P.minX, P.maxX), Mathf.Clamp(p.z, P.minZ, P.maxZ));
            float below = p.y - y;
            if (horiz > 6f || below < -0.6f || below > 4f) continue;
            float score = horiz * 3f + below;
            if (score < bestScore) { bestScore = score; best = i; }
        }
        return best;
    }

    /// Dijkstra on (hardest move, then total effort): the main line is the line whose worst moment is mildest
    static Segment Walk(Geometry g, List<Edge>[] edges, string name, Vector3 from, Vector3 to)
    {
        var seg = new Segment { name = name, from = from, to = to };
        seg.start = PlatUnder(g, from); seg.end = PlatUnder(g, to);
        if (seg.start < 0 || seg.end < 0) { seg.broken = true; seg.deadEnd = seg.start < 0 ? "no floor under the start of this segment" : "no floor under its end"; seg.deadAt = seg.start < 0 ? from : to; return seg; }
        int n = g.plats.Count;
        var best = new (float max, float sum)[n];
        var prev = new Edge[n];
        var done = new bool[n];
        for (int i = 0; i < n; i++) best[i] = (float.MaxValue, float.MaxValue);
        best[seg.start] = (0f, 0f);
        var open = new List<int> { seg.start };
        while (open.Count > 0)
        {
            int bi = 0;
            for (int k = 1; k < open.Count; k++) if (Less(best[open[k]], best[open[bi]])) bi = k;
            int u = open[bi]; open.RemoveAt(bi);
            if (done[u]) continue;
            done[u] = true;
            if (u == seg.end) break;
            foreach (var e in edges[u])
            {
                if (done[e.to]) continue;
                var cand = (Mathf.Max(best[u].max, e.cost), best[u].sum + e.cost + 0.01f);
                if (Less(cand, best[e.to])) { best[e.to] = cand; prev[e.to] = e; open.Add(e.to); }
            }
        }
        if (!done[seg.end])
        {
            seg.broken = true;
            // the dead end: the platform reached that comes nearest the target
            int near = -1; float nd = float.MaxValue;
            for (int i = 0; i < n; i++) if (done[i]) { float d = Flat(g.plats[i].Centre, to); if (d < nd) { nd = d; near = i; } }
            seg.reached = new List<int>(); for (int i = 0; i < n; i++) if (done[i]) seg.reached.Add(i);
            if (near >= 0)
            {
                var P = g.plats[near];
                seg.deadAt = P.Centre;
                // the nearest platform beyond it toward the target
                int beyond = -1; float bd = float.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    if (done[i]) continue;
                    float d = Flat(g.plats[i].Centre, to);
                    if (d < nd && RectGap(P, g.plats[i], out _, out _) < bd) { bd = RectGap(P, g.plats[i], out _, out _); beyond = i; }
                }
                if (beyond >= 0)
                {
                    var Q = g.plats[beyond];
                    float gap = RectGap(P, Q, out Vector3 a, out Vector3 b);
                    float dy = Q.YAt(b.x, b.z) - P.YAt(a.x, a.z);
                    seg.deadEnd = "the line ends on '" + P.name + "' (" + Fmt(P.Centre) + "); the next floor toward the target is '" + Q.name + "', a gap of " + gap.ToString("0.0", Ci) + " m " + (dy >= 0f ? "up " : "down ") + Mathf.Abs(dy).ToString("0.0", Ci) + " m, beyond every move in the metrics";
                }
                else seg.deadEnd = "the line ends on '" + P.name + "' (" + Fmt(P.Centre) + ") with no floor within reach toward the target";
            }
            else { seg.deadEnd = "nothing reachable from the start"; seg.deadAt = from; }
            return seg;
        }
        for (int at = seg.end; at != seg.start && prev[at] != null; at = prev[at].from) seg.path.Add(prev[at]);
        seg.path.Reverse();
        foreach (var e in seg.path) if (seg.gateEdge == null || e.cost > seg.gate) { seg.gate = e.cost; seg.gateEdge = e; }
        return seg;
    }

    static bool Less((float max, float sum) a, (float max, float sum) b) => a.max < b.max || (Mathf.Approximately(a.max, b.max) && a.sum < b.sum);
    static float Flat(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;

    // ---------------------------------------------------------------- the findings

    public class Finding { public int severity; public string title, experience, where, why; public Vector3 at; public string segment; public int number; }

    public class Result
    {
        public LevelKind kind; public string title;
        public List<Finding> findings = new List<Finding>();
        public List<Segment> segments = new List<Segment>();
        public int score; public float routeLength, wallRunMetres, flatMetres; public int jumps, drops, walls, plats, pads, checkpoints; public float longestGap, clearSeconds;
        public string summaryLine;
        public Bounds bounds;
    }

    static Result Validate(LevelKind kind, MovementConfig config)
    {
        var built = Build(kind, config);
        try
        {
            var res = new Result { kind = kind, title = kind.ToString() };
            var g = Read(built.roots);
            res.bounds = g.bounds; res.plats = g.plats.Count; res.walls = g.walls.Count;
            foreach (var p in g.plats) if (p.pad) res.pads++;
            var reach = new Reach(LevelMetrics.Read(config));
            var edges = Graph(g, reach);

            // the waypoints: the spawn, the checkpoints in order, the finish
            var way = new List<(string name, Vector3 at)> { ("THE SPAWN", built.spawn) };
            if (built.layout != null)
            {
                foreach (var cp in built.layout.checkpoints) way.Add((cp.name, cp.at));
                if (built.layout.finishPortal.sqrMagnitude > 1e-3f) way.Add(("THE FINISH", built.layout.finishPortal));
                res.checkpoints = built.layout.checkpoints.Count;
            }
            if (way.Count == 1)
                res.findings.Add(new Finding { severity = 0, title = "NO WAYPOINTS TO WALK", segment = "the whole level", at = built.spawn,
                    experience = "This level names no checkpoints and no finish, so the validator has no line to follow; only the geometry was read.",
                    where = "The whole level.", why = "The main line runs from the spawn through the course's checkpoints to the finish; without them there is nothing to walk." });
            for (int i = 0; i + 1 < way.Count; i++)
            {
                if (Flat(way[i].at, way[i + 1].at) < 2f && Mathf.Abs(way[i].at.y - way[i + 1].at.y) < 2f) continue;
                res.segments.Add(Walk(g, edges, way[i].name + " → " + way[i + 1].name, way[i].at, way[i + 1].at));
            }

            // what the line meets
            foreach (var seg in res.segments)
            {
                if (seg.broken)
                {
                    res.findings.Add(new Finding { severity = 100, title = "THE LINE BREAKS", segment = seg.name, at = seg.deadAt,
                        experience = "The player runs out of floor: from here nothing ahead can be reached with any move the game has, so the level cannot be finished this way.",
                        where = "Segment " + seg.name + ", " + seg.deadEnd + ".",
                        why = "The gap or rise between the last floor reached and the next is beyond the longest arc in the metrics (a double jump at 24 m/s), and no wall runs beside it."
                            + (kind == LevelKind.Level5 || kind == LevelKind.Level10 ? " On this level the chase after the gates is generated in play, so the validator cannot see it; this break is most likely that." : "") });
                    continue;
                }
                Edge prevE = null;
                Vector3 entry = seg.from;   // where the line entered the floor it stands on
                entry.y = g.plats[seg.start].YAt(entry.x, entry.z);
                foreach (var e in seg.path)
                {
                    var P = g.plats[e.from]; var Q = g.plats[e.to];
                    seg.entries.Add(entry);
                    float traverse = Flat(entry, e.a);          // the run across P from where the line came in to where it leaves
                    float segLen = traverse + Flat(e.a, e.b);
                    res.routeLength += segLen;
                    Vector3 dir = e.b - entry; dir.y = 0f; dir = dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.forward;
                    switch (e.move)
                    {
                        case Move.Jump: res.jumps++; res.longestGap = Mathf.Max(res.longestGap, e.gap); res.flatMetres += traverse; break;
                        case Move.DoubleJump: res.jumps++; res.longestGap = Mathf.Max(res.longestGap, e.gap); res.flatMetres += traverse; break;
                        case Move.WallRun: res.jumps++; res.longestGap = Mathf.Max(res.longestGap, e.gap); res.flatMetres += traverse; break;
                        case Move.Drop: res.drops++; res.flatMetres += traverse; break;
                        case Move.Walk: res.flatMetres += segLen; break;
                    }
                    // a kerb across the run on this floor: a solid top a little higher, touching, inside the corridor
                    foreach (var R in Kerbs(g, P, entry, e.a))
                    {
                        if (R == Q) continue;   // the path's own step is reported below
                        res.findings.Add(new Finding { severity = 50, title = "A STEP THAT SNAGS", segment = seg.name, at = R.Centre,
                            experience = "A kerb " + (R.YAt(R.Centre.x, R.Centre.z) - P.YAt(R.Centre.x, R.Centre.z)).ToString("0.00", Ci) + " m tall stands across the run; at speed the player hits it and stops dead, since the motor has no step-up.",
                            where = "Segment " + seg.name + ": '" + R.name + "' on '" + P.name + "' at " + Fmt(R.Centre) + ".",
                            why = R.lip ? "A lip or plinth piece on the floor of the line. A bevel or a ramp would carry the run." : "Two solid tops meet with a small rise between them." });
                    }
                    // the gate: the hardest move of the line
                    if (e == seg.gateEdge && e.cost >= 14f)
                        res.findings.Add(new Finding { severity = e.move == Move.DoubleJump ? 70 : 55, title = e.move == Move.DoubleJump ? "THE ONLY WAY ON COSTS A PIP" : "THE ONLY WAY ON NEEDS " + e.speed.ToString("0", Ci) + " m/s", segment = seg.name, at = e.a,
                            experience = "Everyone has to carry " + e.speed.ToString("0", Ci) + " m/s into this jump" + (e.move == Move.DoubleJump ? " and spend a double jump" : "") + ", or fall. A first-time player at run speed (8 m/s) cannot clear it, so the level stops here for them.",
                            where = "Segment " + seg.name + ": from '" + P.name + "' to '" + Q.name + "', a gap of " + e.gap.ToString("0.0", Ci) + " m " + UpDown(e.dy) + ", at " + Fmt(e.a) + ".",
                            why = "A plain jump at run speed covers " + reach.Plain(0, e.dy).ToString("0.0", Ci) + " m to a landing at this height; the gap plus the landing margin is " + (e.gap + Margin).ToString("0.0", Ci) + " m." });
                    else if (e.move == Move.Step)
                        res.findings.Add(new Finding { severity = e.dy < 1f ? 50 : 40, title = e.dy < 1f ? "A STEP THAT SNAGS" : "A LEDGE TO CLIMB", segment = seg.name, at = e.a,
                            experience = e.dy < 1f ? "At speed the player hits a kerb " + e.dy.ToString("0.00", Ci) + " m tall and stops dead; the motor has no step-up, so anything taller than a few centimetres is a wall unless it is jumped." : "The line climbs a " + e.dy.ToString("0.00", Ci) + " m ledge: a jump from a standstill, every time.",
                            where = "Segment " + seg.name + ": the edge of '" + P.name + "' onto '" + Q.name + "' at " + Fmt(e.a) + ".",
                            why = "Two solid tops meet with a rise of " + e.dy.ToString("0.00", Ci) + " m between them (" + (Q.lip ? "a lip piece" : "a plinth or deck edge") + "). A bevel or a ramp would carry the run; a step cannot." });
                    else if (e.move == Move.WallRun)
                        res.findings.Add(new Finding { severity = 5, title = "A WALL RUN CROSSES IT", segment = seg.name, at = e.a,
                            experience = "The gap is longer than a jump; the wall beside it carries a run and the jump off the wall lands it. Good, if the wall reads as the way.",
                            where = "Segment " + seg.name + ": '" + P.name + "' to '" + Q.name + "', " + e.gap.ToString("0.0", Ci) + " m along '" + e.wall.name + "' at " + Fmt(e.a) + ".",
                            why = "A run at " + e.speed.ToString("0", Ci) + " m/s stays level for " + LevelMetricsMath.Run(reach.v, e.speed, 0f).levelDistance.ToString("0.0", Ci) + " m, then the wall jump's arc covers the rest." });
                    if ((e.move == Move.Jump || e.move == Move.DoubleJump) && e.trivial && e.gap >= 1.5f)
                        res.findings.Add(new Finding { severity = 10, title = "A GAP A WALK CLEARS", segment = seg.name, at = e.a,
                            experience = "A jump with nothing in it: at run speed it clears with half the arc to spare. No tension, no reason to carry speed.",
                            where = "Segment " + seg.name + ": " + e.gap.ToString("0.0", Ci) + " m " + UpDown(e.dy) + " from '" + P.name + "' at " + Fmt(e.a) + ".",
                            why = "Run speed reaches " + reach.Plain(0, e.dy).ToString("0.0", Ci) + " m here; the design band for a gap is 70–90% of that: " + (0.7f * reach.Plain(0, e.dy)).ToString("0.0", Ci) + "–" + (0.9f * reach.Plain(0, e.dy)).ToString("0.0", Ci) + " m." });
                    if ((e.move == Move.Drop || e.move == Move.Jump) && e.dy <= -LevelMetricsMath.RollHeight(reach.v) && Q.slope < 6f)
                        res.findings.Add(new Finding { severity = 20, title = "A FLAT LANDING OFF A DROP", segment = seg.name, at = e.b,
                            experience = "A fall of " + (-e.dy).ToString("0.0", Ci) + " m onto flat stone: the camera rolls, the fall's speed is thrown away, and unless the hop is timed inside 0.1 s the run speed goes with it.",
                            where = "Segment " + seg.name + ": landing on '" + Q.name + "' at " + Fmt(e.b) + ".",
                            why = "A flat landing keeps only the horizontal; a slope landing would turn the fall into speed along it. Friction keeps 45% of any extra speed after 0.1 s on the ground." });
                    // the line's own floor: narrow, uphill, under a ceiling
                    if (Across(Q, dir) < 1f && segLen > 1f)
                        res.findings.Add(new Finding { severity = 25, title = "A BEAM NARROWER THAN THE BODY", segment = seg.name, at = Q.Centre,
                            experience = "The line runs along a strip " + Across(Q, dir).ToString("0.00", Ci) + " m wide: the body is 0.8 m, so any drift is a fall and the player slows to aim.",
                            where = "Segment " + seg.name + ": '" + Q.name + "' at " + Fmt(Q.Centre) + ".", why = "A precision test on the main line costs speed for everyone; it belongs on a skip." });
                    if (Q.slope >= 8f)
                    {
                        float along = Vector3.Dot(Q.Uphill, dir);
                        float run = Mathf.Max(Q.Width, Q.Depth);
                        if (along > 0.5f)
                        {
                            float stop12 = LevelMetricsMath.SlideUpRun(reach.v, 12f, Q.slope);
                            res.findings.Add(new Finding { severity = run > stop12 ? 40 : 20, title = "UPHILL ON THE LINE", segment = seg.name, at = Q.Centre,
                                experience = "A " + Q.slope.ToString("0", Ci) + "° climb " + run.ToString("0", Ci) + " m long: a slide into it stops after " + stop12.ToString("0.0", Ci) + " m from 12 m/s, and a run up it is held to run speed by friction. Whatever speed came in is gone at the top.",
                                where = "Segment " + seg.name + ": '" + Q.name + "' at " + Fmt(Q.Centre) + ".",
                                why = "Running never gains or keeps speed above 8 m/s on the ground, and a slide fights gravity's share along the slope (" + LevelMetricsMath.SlideSlopeGain(reach.v, Q.slope).ToString("0.0", Ci) + " m/s² against it)." });
                        }
                    }
                    float clearP = ClearanceAlong(g, P, entry, e.a, out Vector3 lowAt), clearQ = Clearance(g, Q, e.b);
                    float clear = Mathf.Min(clearP, clearQ);
                    Vector3 clearAt = clearP <= clearQ ? lowAt : e.b;
                    string clearOver = clearP <= clearQ ? P.name : Q.name;
                    if (clear < 1.0f)
                        res.findings.Add(new Finding { severity = 80, title = "A CEILING TOO LOW EVEN TO SLIDE", segment = seg.name, at = clearAt,
                            experience = "Headroom of " + clear.ToString("0.00", Ci) + " m: a run stops here and a slide (0.9 m) scrapes or stops. The line is blocked unless there is another way round.",
                            where = "Segment " + seg.name + ": over '" + clearOver + "' at " + Fmt(clearAt) + ".", why = "A solid underside sits within a metre of the floor." });
                    else if (clear < 1.95f)
                        res.findings.Add(new Finding { severity = 15, title = "A FORCED SLIDE", segment = seg.name, at = clearAt,
                            experience = "Headroom of " + clear.ToString("0.00", Ci) + " m: the only way through is a slide, and a hop into it is a stop. Fine as a beat; a problem if it comes without warning at speed.",
                            where = "Segment " + seg.name + ": over '" + clearOver + "' at " + Fmt(clearAt) + ".", why = "A beam or underside between 1.0 and 1.9 m over the floor." });
                    // the turn: a dead stop against a wall
                    if (prevE != null)
                    {
                        Vector3 pd = prevE.b - prevE.a; pd.y = 0f;
                        if (pd.sqrMagnitude > 1e-3f && Vector3.Angle(pd.normalized, dir) >= 60f)
                        {
                            bool wallAhead = WallAhead(g, e.a, pd.normalized, P.YAt(e.a.x, e.a.z));
                            res.findings.Add(new Finding { severity = wallAhead ? 60 : 25, title = wallAhead ? "A DEAD STOP AT A WALL" : "A HARD TURN", segment = seg.name, at = e.a,
                                experience = wallAhead ? "The line arrives at a wall, turns " + Vector3.Angle(pd.normalized, dir).ToString("0", Ci) + "° and starts again from nothing: every bit of speed carried in is lost here." : "The line turns " + Vector3.Angle(pd.normalized, dir).ToString("0", Ci) + "° on the flat; a runner has to brake into it.",
                                where = "Segment " + seg.name + ": on '" + P.name + "' at " + Fmt(e.a) + ".",
                                why = wallAhead ? "A flat landing in front of a wall across the line of travel, with the way out at right angles. A wall run round the turn, a banked curve or a drop-through would carry the speed instead." : "No curve or bank carries the turn." });
                        }
                    }
                    prevE = e;
                    entry = e.b;
                }
            }
            // wall running along the line
            res.wallRunMetres = WallRunMetres(g, res.segments);
            if (res.routeLength > 50f && res.wallRunMetres < res.routeLength * 0.1f)
                res.findings.Add(new Finding { severity = 35, title = "BARELY ANY WALL RUNNING", segment = "the whole level", at = res.bounds.center,
                    experience = "Along " + res.routeLength.ToString("0", Ci) + " m of line there are " + res.wallRunMetres.ToString("0", Ci) + " m of wall a run can use. The best move in the kit has almost nowhere to happen.",
                    where = "The whole level.", why = "Walls over 2 m tall, parallel to the line within 2.5 m of it, standing from the floor to above head height, are what a wall run needs; a run lasts 1.6 s and stays level for 0.7 s, so faces 4–7 m apart would chain." });
            // the order and the numbers
            res.findings.Sort((x, y) => y.severity != x.severity ? y.severity.CompareTo(x.severity) : string.CompareOrdinal(x.segment, y.segment));
            for (int i = 0; i < res.findings.Count; i++) { res.findings[i].number = i + 1; res.score += res.findings[i].severity; }
            res.clearSeconds = res.routeLength / reach.v.baseRunSpeed + res.jumps * 0.7f + res.drops * 0.8f;
            res.summaryLine = kind + ": score " + res.score + ", " + res.findings.Count + " findings, line " + res.routeLength.ToString("0", Ci) + " m, ~" + res.clearSeconds.ToString("0", Ci) + " s at run speed, " + res.jumps + " gaps (longest " + res.longestGap.ToString("0.0", Ci) + " m), " + res.drops + " drops, wall-runnable " + res.wallRunMetres.ToString("0", Ci) + " m";
            WriteReport(res, g);
            LevelOverhead.WriteImages(kind.ToString(), res.bounds, g, res.segments, res.findings, built.roots);
            return res;
        }
        finally { Tear(built); }
    }

    static float Across(Plat Q, Vector3 dir) => Mathf.Abs(dir.x) >= Mathf.Abs(dir.z) ? Q.Depth : Q.Width;

    static float Clearance(Geometry g, Plat Q, Vector3 at)
    {
        float floor = Q.YAt(at.x, at.z), best = float.MaxValue;
        foreach (var c in g.ceils)
        {
            if (c.col == Q.col) continue;
            if (c.maxX - c.minX < 3f && c.maxZ - c.minZ < 3f) continue;   // a desk, a crate, a column: a thing to run round, not a ceiling
            if (at.x < c.minX - 0.3f || at.x > c.maxX + 0.3f || at.z < c.minZ - 0.3f || at.z > c.maxZ + 0.3f) continue;
            float h = c.YAt(at.x, at.z) - floor;
            if (h > 0.2f && h < best) best = h;
        }
        return best;
    }

    /// The least headroom along a run across P from `from` to `to`, sampled every metre, and where it was
    static float ClearanceAlong(Geometry g, Plat P, Vector3 from, Vector3 to, out Vector3 lowAt)
    {
        float best = float.MaxValue; lowAt = to;
        int n = Mathf.Max(1, Mathf.CeilToInt(Flat(from, to)));
        for (int i = 0; i <= n; i++)
        {
            Vector3 p = Vector3.Lerp(from, to, (float)i / n);
            float c = Clearance(g, P, p);
            if (c < best) { best = c; lowAt = p; lowAt.y = P.YAt(p.x, p.z); }
        }
        return best;
    }

    /// Kerbs across a run on P: solid tops touching P, a little higher (a kerb, not a ledge), inside the run's corridor
    static IEnumerable<Plat> Kerbs(Geometry g, Plat P, Vector3 from, Vector3 to)
    {
        float len = Flat(from, to);
        if (len < 1f) yield break;
        Vector3 d = to - from; d.y = 0f; d /= Mathf.Max(0.01f, d.magnitude);
        Vector3 side = new Vector3(-d.z, 0f, d.x);
        foreach (var R in g.plats)
        {
            if (R == P || R.pad) continue;
            if (RectGap(P, R, out _, out _) > Touch) continue;
            float rise = R.YAt(R.Centre.x, R.Centre.z) - P.YAt(R.Centre.x, R.Centre.z);
            if (rise < StepMin || rise > 1.0f) continue;
            Vector3 rc = R.Centre; rc.y = 0f;
            Vector3 f0 = from; f0.y = 0f;
            float along = Vector3.Dot(rc - f0, d);
            if (along < -1f || along > len + 1f) continue;
            if (Mathf.Abs(Vector3.Dot(rc - f0, side)) > 1.5f + Mathf.Max(R.Width, R.Depth) * 0.5f) continue;
            yield return R;
        }
    }

    static bool WallAhead(Geometry g, Vector3 at, Vector3 dir, float floorY)
    {
        foreach (var w in g.walls)
        {
            if (Mathf.Abs(Vector3.Dot(w.n, dir)) < 0.7f) continue;          // across the line of travel
            float ahead = Vector3.Dot(w.c - at, dir);
            if (ahead < 0f || ahead > 5f) continue;
            if (Mathf.Abs(Vector3.Dot(at - w.c, w.u)) > w.halfLen + 1f) continue;
            if (w.bottom > floorY + 0.5f || w.top < floorY + 1.5f) continue;
            return true;
        }
        return false;
    }

    static float WallRunMetres(Geometry g, List<Segment> segments)
    {
        float total = 0f;
        foreach (var seg in segments)
            for (int i = 0; i < seg.path.Count; i++)
            {
                var e = seg.path[i];
                Vector3 from = i < seg.entries.Count ? seg.entries[i] : e.a, to = e.b;
                Vector3 d = to - from; d.y = 0f;
                float len = d.magnitude;
                if (len < 2f) continue;
                d /= len;
                float floor = from.y;
                var P = g.plats[e.from];
                float reach = 2.5f + Mathf.Max(P.Width, P.Depth) * 0.5f;   // a wall along the floor's own side is within an angle
                foreach (var w in g.walls)
                {
                    if (Mathf.Abs(Vector3.Dot(w.u, d)) < 0.9f) continue;
                    float lateral = Mathf.Abs(Vector3.Dot((from + to) * 0.5f - w.c, w.n));
                    if (lateral > reach) continue;
                    float a0 = Vector3.Dot(from - w.c, w.u), a1 = Vector3.Dot(to - w.c, w.u);
                    float overlap = Mathf.Min(Mathf.Max(a0, a1), w.halfLen) - Mathf.Max(Mathf.Min(a0, a1), -w.halfLen);
                    if (overlap < 3f || w.bottom > floor + 0.8f || w.top < floor + 1.6f) continue;
                    total += overlap;
                    break;
                }
            }
        return total;
    }

    // ---------------------------------------------------------------- the reports

    static readonly CultureInfo Ci = CultureInfo.InvariantCulture;
    static string Fmt(Vector3 p) => "(" + p.x.ToString("0", Ci) + ", " + p.y.ToString("0", Ci) + ", " + p.z.ToString("0", Ci) + ")";
    static string UpDown(float dy) => Mathf.Abs(dy) < 0.3f ? "level" : (dy > 0f ? "up " : "down ") + Mathf.Abs(dy).ToString("0.0", Ci) + " m";
    static string Dir => Path.Combine(Directory.GetParent(Application.dataPath).FullName, OutDir);

    static void WriteReport(Result r, Geometry g)
    {
        Directory.CreateDirectory(Dir);
        var sb = new StringBuilder();
        sb.AppendLine("# " + r.kind + " — validation");
        sb.AppendLine();
        sb.AppendLine("Generated by Momentum/Level Validator. Measures only. Positions are world metres (x, y, z); the numbers on the images are the finding numbers below.");
        sb.AppendLine();
        sb.AppendLine("**" + r.summaryLine + "**  ");
        sb.AppendLine("Platforms read: " + r.plats + ", walls: " + r.walls + ", pads: " + r.pads + ", checkpoints: " + r.checkpoints + ", flat walking on the line: " + r.flatMetres.ToString("0", Ci) + " m of " + r.routeLength.ToString("0", Ci) + " m.");
        sb.AppendLine();
        sb.AppendLine("## Findings, worst first");
        sb.AppendLine();
        if (r.findings.Count == 0) sb.AppendLine("Nothing flagged on the main line.");
        foreach (var f in r.findings)
        {
            sb.AppendLine("### " + f.number + ". [" + f.severity + "] " + f.title);
            sb.AppendLine("- **What the player experiences:** " + f.experience);
            sb.AppendLine("- **Where:** " + f.where);
            sb.AppendLine("- **Why:** " + f.why);
            sb.AppendLine();
        }
        sb.AppendLine("## The main line, move by move");
        sb.AppendLine();
        foreach (var seg in r.segments)
        {
            sb.AppendLine("**" + seg.name + "**" + (seg.broken ? " — BROKEN: " + seg.deadEnd : " — hardest move " + seg.gate.ToString("0.0", Ci)));
            if (seg.broken) { sb.AppendLine(); continue; }
            var line = new StringBuilder();
            for (int i = 0; i < seg.path.Count; i++)
            {
                var e = seg.path[i];
                float run = i < seg.entries.Count ? Flat(seg.entries[i], e.a) : 0f;
                if (e.move != Move.Walk && run >= 1f) line.Append(line.Length > 0 ? " → " : "").Append("run " + run.ToString("0", Ci) + " m");
                string m = e.move switch
                {
                    Move.Walk => "run " + (run + Flat(e.a, e.b)).ToString("0", Ci) + " m",
                    Move.Drop => "drop " + (-e.dy).ToString("0.0", Ci) + " m",
                    Move.Step => "STEP up " + e.dy.ToString("0.00", Ci) + " m",
                    Move.Jump => "jump " + e.gap.ToString("0.0", Ci) + " m " + UpDown(e.dy) + " (" + e.speed.ToString("0", Ci) + " m/s)",
                    Move.DoubleJump => "DOUBLE JUMP " + e.gap.ToString("0.0", Ci) + " m " + UpDown(e.dy) + " (" + e.speed.ToString("0", Ci) + " m/s + a pip)",
                    Move.WallRun => "wall run + jump " + e.gap.ToString("0.0", Ci) + " m (" + e.speed.ToString("0", Ci) + " m/s)",
                    _ => e.move.ToString(),
                };
                line.Append(line.Length > 0 ? " → " : "").Append(m);
            }
            sb.AppendLine(line.ToString());
            sb.AppendLine();
        }
        File.WriteAllText(Path.Combine(Dir, r.kind + ".md"), sb.ToString());
    }

    static void WriteSummary(List<Result> results)
    {
        Directory.CreateDirectory(Dir);
        results.Sort((a, b) => b.score.CompareTo(a.score));
        var sb = new StringBuilder();
        sb.AppendLine("# Validation summary — levels ranked, worst first");
        sb.AppendLine();
        sb.AppendLine("Generated by Momentum/Level Validator on " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", Ci) + ". The score is the sum of the findings' severities on each level's main line.");
        sb.AppendLine();
        sb.AppendLine("| level | score | findings | line | first clear at run speed | gaps (longest) | drops | wall-runnable | worst three |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
        foreach (var r in results)
        {
            var worst = new List<string>();
            for (int i = 0; i < Mathf.Min(3, r.findings.Count); i++) worst.Add(r.findings[i].number + ". " + r.findings[i].title);
            sb.AppendLine("| [" + r.kind + "](" + r.kind + ".md) | " + r.score + " | " + r.findings.Count + " | " + r.routeLength.ToString("0", Ci) + " m | ~" + r.clearSeconds.ToString("0", Ci) + " s | " + r.jumps + " (" + r.longestGap.ToString("0.0", Ci) + " m) | " + r.drops + " | " + r.wallRunMetres.ToString("0", Ci) + " m | " + string.Join("; ", worst) + " |");
        }
        sb.AppendLine();
        sb.AppendLine("Images: `<level>-overhead.png` (rendered from above) and `<level>-plan.png` (drawn from the solid geometry: lighter is higher, brass lines are walls, gold squares are pads, the cyan line is the main line, red circles are the findings by number, green is the spawn, white diamonds the checkpoints, magenta the finish).");
        File.WriteAllText(Path.Combine(Dir, "SUMMARY.md"), sb.ToString());
    }

    // the images need the segments' paths and the geometry: exposed for LevelOverhead
    public static IEnumerable<(string name, bool broken, Vector3 from, Vector3 to, Vector3 deadAt, List<(Vector3 a, Vector3 b, string move)> moves)> Paths(object segmentsObj)
    {
        foreach (var seg in (List<Segment>)segmentsObj)
        {
            var moves = new List<(Vector3, Vector3, string)>();
            for (int i = 0; i < seg.path.Count; i++)
            {
                var e = seg.path[i];
                if (i < seg.entries.Count) moves.Add((seg.entries[i], e.a, "Walk"));
                moves.Add((e.a, e.b, e.move.ToString()));
            }
            yield return (seg.name, seg.broken, seg.from, seg.to, seg.deadAt, moves);
        }
    }
}
