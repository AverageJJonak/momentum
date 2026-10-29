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
        LevelKind.Level7, LevelKind.Level8, LevelKind.Level9, LevelKind.Level10, LevelKind.Level11, LevelKind.Level12, LevelKind.Level13, LevelKind.BeatGym,
    };
    static readonly float[] Speeds = { 8f, 10f, 12f, 14f, 16f, 18f, 20f, 24f };
    const float Margin = 0.5f;          // the landing's margin past the gap: the body's radius and a hair
    const float Touch = 0.35f;          // rectangles this close are one floor
    const float StepMin = 0.08f;        // a rise this small is the capsule's business; above it the motor stops
    const float StepMax = 1.45f;        // the apex a standing jump reaches, less the body
    const float ReachLimit = 80f;            // no pair further apart than this is asked about (a chain of wall runs carries a long way)

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

    [MenuItem(Menu + "Validate The Beat Gym", false, 2)]
    public static void ValidateGym()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.Log("LevelValidator: works outside Play Mode."); return; }
        var config = AssetDatabase.LoadAssetAtPath<MovementConfig>(ConfigPath);
        if (config == null) { Debug.LogError("LevelValidator: " + ConfigPath + " not found."); return; }
        try
        {
            EditorUtility.DisplayProgressBar("Level Validator", "Building and reading the beat gym", 0.3f);
            var r = Validate(LevelKind.BeatGym, config);
            Debug.Log("LevelValidator: the gym → " + OutDir + "/BeatGym.md " + r.summaryLine);
        }
        finally { EditorUtility.ClearProgressBar(); }
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
        // the look the game lays on next (the sky, the fog volumes, the lamps' glints, the smog sheets and the paper): what
        // the player sees from the spawn is not whole without it
        try { WorldLook.Apply(kind, LevelSections.Last); GameBootstrap.HardLine(kind); }
        catch (Exception e) { Debug.LogWarning("LevelValidator: the look pass failed on " + kind + " (" + e.Message + ")"); }
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
        public bool lip, pad, kick, lift;   // lift: a top a wall run's lift can put the player on (a wall's own top, a block, a lintel) that is no floor of any line
        public float YAt(float x, float z) => c.y - (n.x * (x - c.x) + n.z * (z - c.z)) / Mathf.Max(0.2f, n.y);
        public float Width => maxX - minX;
        public float Depth => maxZ - minZ;
        public Vector3 Centre => new Vector3((minX + maxX) * 0.5f, YAt((minX + maxX) * 0.5f, (minZ + maxZ) * 0.5f), (minZ + maxZ) * 0.5f);
        /// The direction that climbs the slope, flat
        public Vector3 Uphill { get { var u = new Vector3(-n.x, 0f, -n.z); return u.sqrMagnitude > 1e-4f ? u.normalized : Vector3.zero; } }
    }
    public class Wall { public string name; public Collider col; public Vector3 c, n, u; public float halfLen, bottom, top; public bool kick; }
    public class Ceil { public Collider col; public float minX, maxX, minZ, maxZ, top, thick; public Vector3 c, n; public float YAt(float x, float z) => c.y - (n.x * (x - c.x) + n.z * (z - c.z)) / Mathf.Min(-0.2f, n.y); }   // thick: the mass's vertical depth over this underside (a pitched box's top is YAt + thick, not `top`)

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
                            bool lift = name == "Lintel" || name == "Post" || name == "SettleBlock" || name == "KickWall" || name == "Divider" || name == "Cornice" || name == "Facade" || name == "Machinery" || name.EndsWith("Body");   // the top of a portal, a block, a wall, a cornice: not a floor of any line, but a wall run's lift can reach it (a body's top is inside its building: nothing)
                            var p = new Plat { id = g.plats.Count, name = name, col = col, c = fc, n = nf, slope = Vector3.Angle(nf, Vector3.up), lip = lip, pad = pad, kick = kick, lift = lift };
                            Footprint(fc, e1, e2, out p.minX, out p.maxX, out p.minZ, out p.maxZ);
                            if (p.Width < 0.3f || p.Depth < 0.3f) continue;
                            g.plats.Add(p);
                            var bb = new Bounds(fc, Vector3.zero); bb.Encapsulate(fc + e1 + e2); bb.Encapsulate(fc - e1 - e2); bb.Encapsulate(fc + e1 - e2); bb.Encapsulate(fc - e1 + e2);
                            if (!any) { g.bounds = bb; any = true; } else g.bounds.Encapsulate(bb);
                        }
                        else if (nf.y <= -0.7f)
                        {
                            var cl = new Ceil { col = col, c = fc, n = nf, top = wc.y + Mathf.Abs(axes[0].y) * ext[0] + Mathf.Abs(axes[1].y) * ext[1] + Mathf.Abs(axes[2].y) * ext[2], thick = 2f * ext[a] / Mathf.Max(0.2f, -nf.y) };   // the top of the mass this is the underside of: an arc over it clears it
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
                            g.walls.Add(new Wall { name = name, col = col, c = fc, n = nh, u = new Vector3(u.x, 0f, u.z).normalized, halfLen = halfLen, bottom = fc.y - height * 0.5f, top = fc.y + height * 0.5f, kick = kick });
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
            if (Mathf.Min(c.maxX - c.minX, c.maxZ - c.minZ) < 0.6f) continue;
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
                if (i == j || P.lift || g.plats[j].lift) continue;
                var Q = g.plats[j];
                float gap = RectGap(P, Q, out Vector3 a, out Vector3 b);
                if (gap > ReachLimit) continue;
                if (gap <= 0f && (P.slope >= 3f || Q.slope >= 3f)) LowEdgeJoin(P, Q, ref a, ref b);   // a ramp lying on a floor joins it at its foot, not at the overlap's middle
                else if (gap <= 0f && Clearance(g, Q, b) < 1.0f) EdgeJoin(P, Q, ref a, ref b, ref gap);   // a floor running on under a deck joins at the deck's edge, not under it
                float ya = P.YAt(a.x, a.z), yb = Q.YAt(b.x, b.z);
                float dy = yb - ya;
                if (dy > 2f || dy < -40f) continue;
                a.y = ya; b.y = yb;
                if (Clearance(g, P, a) < 1.0f || Clearance(g, Q, b) < 1.0f) continue;   // no headroom to leave or to land: not a way
                var e = new Edge { from = i, to = j, gap = gap, dy = dy, a = a, b = b };
                if (gap <= Touch && Mathf.Abs(dy) <= StepMin) { e.move = Move.Walk; e.cost = 0f; }
                else if (gap <= Touch && dy < -StepMin) { e.move = Move.Drop; e.cost = 0.5f; }
                else if (gap <= Touch && dy <= StepMax) { e.move = Move.Step; e.cost = 6f; }
                else
                {
                    // the cheapest of the three ways over: a plain jump, a wall run and the jump off it, a double jump
                    float need = gap + Margin;
                    int js = -1, ws = -1, ds = -1; Wall wall = null;
                    for (int k = 0; k < Speeds.Length; k++) if (r.Plain(k, dy) >= need) { js = k; break; }
                    for (int k = 0; k < Speeds.Length; k++) { float carry = WallCarry(g, P, a, b, ya, Speeds[k], dy, r.v, out Wall last); if (carry >= need) { ws = k; wall = last; break; } }
                    for (int k = 0; k < Speeds.Length; k++) if (r.Double(k, dy) >= need) { ds = k; break; }
                    float jc = js >= 0 ? Speeds[js] : float.MaxValue, wc = ws >= 0 ? Speeds[ws] + 1f : float.MaxValue, dc = ds >= 0 ? Speeds[ds] + 4f : float.MaxValue;
                    if (jc == float.MaxValue && wc == float.MaxValue && dc == float.MaxValue) continue;
                    if (jc <= wc && jc <= dc) { e.move = Move.Jump; e.speed = Speeds[js]; e.cost = jc; e.trivial = dy > -2f && r.Plain(0, dy) >= 1.5f * need; }   // a drop onto a landing is a fall, not a trivial gap
                    else if (wc <= dc) { e.move = Move.WallRun; e.speed = Speeds[ws]; e.cost = wc; e.wall = wall; }
                    else { e.move = Move.DoubleJump; e.speed = Speeds[ds]; e.cost = dc; }
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

    /// A ramp lying on a floor (their footprints overlap): the join is at the ramp's downhill edge. False when that edge is not over the floor
    static bool LowEdgeJoin(Plat P, Plat Q, ref Vector3 a, ref Vector3 b)
    {
        Plat S = P.slope >= Q.slope ? P : Q, F = S == P ? Q : P;
        Vector3 u = S.Uphill;
        if (u == Vector3.zero) return false;
        float extent = Mathf.Abs(u.x) * S.Width + Mathf.Abs(u.z) * S.Depth;
        Vector3 low = S.Centre - u * (extent * 0.5f - 0.25f);
        if (low.x < F.minX - 0.3f || low.x > F.maxX + 0.3f || low.z < F.minZ - 0.3f || low.z > F.maxZ + 0.3f) return false;
        a = new Vector3(Mathf.Clamp(low.x, P.minX, P.maxX), 0f, Mathf.Clamp(low.z, P.minZ, P.maxZ));
        b = new Vector3(Mathf.Clamp(low.x, Q.minX, Q.maxX), 0f, Mathf.Clamp(low.z, Q.minZ, Q.maxZ));
        return true;
    }

    /// A floor that runs on under a deck: the join is at the deck's edge on the way to the other floor, a hair past it
    static void EdgeJoin(Plat P, Plat Q, ref Vector3 a, ref Vector3 b, ref float gap)
    {
        Vector3 pc = P.Centre, qc = Q.Centre, d = qc - pc; d.y = 0f;
        if (d.sqrMagnitude < 1e-3f) return;
        d.Normalize();
        float tx = d.x > 1e-4f ? (P.maxX - pc.x) / d.x : d.x < -1e-4f ? (P.minX - pc.x) / d.x : float.MaxValue;
        float tz = d.z > 1e-4f ? (P.maxZ - pc.z) / d.z : d.z < -1e-4f ? (P.minZ - pc.z) / d.z : float.MaxValue;
        Vector3 edge = pc + d * Mathf.Min(tx, tz);
        a = new Vector3(edge.x, 0f, edge.z);
        Vector3 beyond = edge + d * 0.6f;   // past the deck's own tolerance, so the landing is judged in the open
        b = new Vector3(Mathf.Clamp(beyond.x, Q.minX, Q.maxX), 0f, Mathf.Clamp(beyond.z, Q.minZ, Q.maxZ));
        gap = 0f;   // the floors touch: a step off the edge, not a jump
    }

    /// How far a wall run beside the line from a to b carries at `speed`, chains included: every wall parallel to the
    /// line within reach of the floor it leaves, in order along it, each run level for what it has of the wall, a jump
    /// to the next (walls up to seven metres apart chain), the jump off the last one to a landing `dy` away
    static float WallCarry(Geometry g, Plat P, Vector3 a, Vector3 b, float floorY, float speed, float dy, LevelMetricsMath.Values v, out Wall last) => WallCarry(g, P, a, b, floorY, speed, dy, v, out last, out _, out _, out _);

    /// The same, telling where the last run leaves its wall (`runEnd` metres along the line) and the wall jump's speed and rise
    static float WallCarry(Geometry g, Plat P, Vector3 a, Vector3 b, float floorY, float speed, float dy, LevelMetricsMath.Values v, out Wall last, out float runEnd, out float offH, out float offVy)
    {
        last = null; runEnd = 0f; offH = speed; offVy = v.jumpImpulse;
        Vector3 d = b - a; d.y = 0f;
        float len = d.magnitude;
        if (len < 1f) return 0f;
        d /= len;
        Vector3 side = new Vector3(-d.z, 0f, d.x);
        float reach = 1.5f + 0.5f * (Mathf.Abs(d.x) >= Mathf.Abs(d.z) ? P.Depth : P.Width);
        var walls = new List<(float s0, float s1, float lat, Wall w)>();
        foreach (var w in g.walls)
        {
            if (w.name == "SettleBlock" || w.name == "Lintel" || w.name == "Post") continue;   // a corner block or a portal is a thing in the way, not a wall to run
            if (Mathf.Abs(Vector3.Dot(w.u, d)) < 0.9f) continue;
            float lat = Vector3.Dot(w.c - a, side);
            if (Mathf.Abs(lat) > reach) continue;
            if (w.bottom > floorY + 1.5f || w.top < floorY + 1.6f) continue;
            float c = Vector3.Dot(w.c - a, d);
            float s0 = c - w.halfLen, s1 = c + w.halfLen;
            if (s1 < -1f || s0 > len + 1f) continue;
            walls.Add((s0, s1, lat, w));
        }
        if (walls.Count == 0) return 0f;
        // one face per wall: a box has two long faces and both lie beside the line; counted twice, a wall ran twice over
        // (the second face was read as a wall a run could go straight on to) and chains stood at speeds that cannot make them
        for (int i = walls.Count - 1; i >= 0; i--)
            for (int j = 0; j < i; j++)
                if (walls[j].w.col == walls[i].w.col && walls[j].w.col != null)
                {
                    if (Mathf.Abs(walls[i].lat) < Mathf.Abs(walls[j].lat)) walls[j] = walls[i];   // keep the nearer face
                    walls.RemoveAt(i);
                    break;
                }
        walls.Sort((x, y) => x.s0.CompareTo(y.s0));
        var run = LevelMetricsMath.Run(v, speed, 0f);
        var off = LevelMetricsMath.WallJump(v, speed, 0f);
        float hop = LevelMetricsMath.Jump(v, off.h, off.vy, 0f, false).distance;   // from one wall to the next, level
        float first = LevelMetricsMath.Jump(v, speed, v.jumpImpulse, 0f, false).distance;   // from the floor to the first
        float pos = 0f, lastLat = 0f;
        bool any = false;
        foreach (var (s0, s1, lat, w) in walls)
        {
            if (s0 > pos + (any ? hop : first)) break;
            if (any && Mathf.Abs(lat - lastLat) > 7.5f) break;
            float at = Mathf.Max(pos, s0);
            pos = at + Mathf.Min(s1 - at, run.levelDistance);
            lastLat = lat; last = w; any = true;
        }
        if (!any) return 0f;
        runEnd = pos; offH = off.h; offVy = off.vy;
        return pos + LevelMetricsMath.Jump(v, off.h, off.vy, dy, false).distance;
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

    public class Segment { public string name; public Vector3 from, to; public int start = -1, end = -1; public List<Edge> path = new List<Edge>(); public List<Vector3> entries = new List<Vector3>(); public bool broken; public float gate; public Edge gateEdge; public List<int> reached; public string deadEnd; public Vector3 deadAt; public float sized; }

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
            if (P.lift || horiz > 6f || below < -0.6f || below > 4f) continue;
            float score = horiz * 3f + below;
            if (score < bestScore) { bestScore = score; best = i; }
        }
        return best;
    }

    /// Dijkstra on (hardest move, then total effort): the main line is the line whose worst moment is mildest
    static Segment Walk(Geometry g, List<Edge>[] edges, string name, Vector3 from, Vector3 to, System.Func<Plat, bool> skip = null)
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
                if (skip != null && skip(g.plats[e.to])) continue;   // a floor this walk is told to leave alone (the hidden routes)
                var cand = (Mathf.Max(best[u].max, e.cost), best[u].sum + e.cost + 0.01f + (e.move == Move.Walk ? 0f : e.gap * 0.02f));   // ties go to the shorter flight: a player jumps from the floor nearest the gap, not from the one before it
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

    // ---------------------------------------------------------------- the hidden routes (2026-10-28)

    /// A floor of a hidden line (named Hidden…) or of the lit line's own stretch beside it (Main…)
    static bool IsHidden(Plat p) => p.name.StartsWith("Hidden");
    static bool IsMainOnly(Plat p) => p.name.StartsWith("Main");

    public class HiddenReport
    {
        public string segment, mainMoves, hiddenMoves;
        public float speed, mainRun, mainKept, hidden, mainArrive, hiddenArrive;
        public Vector3 at, forward;
        public float SavesRun => mainRun - hidden;
        public float SavesKept => mainKept - hidden;
    }

    /// Where a hidden line parts from the lit one and where they meet again, each stretch is timed: the lit line as a RUN
    /// (run speed throughout, the slope ridden not slid, every drop a fall) and as KEPT (the hidden line's expected
    /// speed carried by hops), the hidden line as EXECUTED (its speed kept, its slopes slid, its drops turned to speed
    /// on a slope landing). Both are timed over the whole segment, reset pad to reset pad: the number the recorder's
    /// segment column gives for a run, so the user can check it
    static HiddenReport CompareRoutes(Geometry g, LevelMetricsMath.Values v, string segment, Segment main, Segment hid, float speed)
    {
        var ms = Sequence(main); var hs = Sequence(hid);
        if (ms.Count < 2 || hs.Count < 2) return null;
        int pre = 0;
        while (pre < ms.Count && pre < hs.Count && ms[pre] == hs[pre]) pre++;
        if (pre == ms.Count || pre == hs.Count) return null;   // one line is the other's prefix: no fork
        int suf = 0;
        while (suf < ms.Count - pre && suf < hs.Count - pre && ms[ms.Count - 1 - suf] == hs[hs.Count - 1 - suf]) suf++;
        if (suf == 0) return null;   // they never meet again
        bool any = false;
        for (int i = pre; i < hs.Count - suf; i++) if (IsHidden(g.plats[hs[i]])) any = true;
        if (!any) return null;   // the other way is not a hidden line
        // the whole segment, pad to pad: what the recorder measures, so the user can check it against a run
        var mainEdges = main.path;
        var hidEdges = hid.path;
        Vector3 entry = main.from; entry.y = g.plats[ms[0]].YAt(entry.x, entry.z);
        Vector3 exit = main.to; exit.y = g.plats[ms[ms.Count - 1]].YAt(exit.x, exit.z);
        var r = new HiddenReport { segment = segment, speed = speed, at = main.from, forward = main.to - main.from };
        r.mainRun = PathTime(g, v, mainEdges, entry, exit, v.baseRunSpeed, false, out _, out r.mainMoves);
        r.mainKept = PathTime(g, v, mainEdges, entry, exit, speed, true, out r.mainArrive, out _);
        r.hidden = PathTime(g, v, hidEdges, entry, exit, speed, true, out r.hiddenArrive, out r.hiddenMoves);
        return r;
    }

    static List<int> Sequence(Segment s)
    {
        var seq = new List<int>();
        if (s.start < 0) return seq;
        seq.Add(s.start);
        foreach (var e in s.path) seq.Add(e.to);
        return seq;
    }

    /// Seconds along `edges` from `entry` (on the first edge's floor) to `exit` (on the last edge's landing), at `speed`.
    /// `executed`: the speed is kept on the flat (hops inside the grace), a downhill slope is slid and gains, a landing
    /// on a slope keeps the fall's share along it. Not executed: run speed everywhere, slopes ridden, drops fallen
    static float PathTime(Geometry g, LevelMetricsMath.Values v, List<Edge> edges, Vector3 entry, Vector3 exit, float speed, bool executed, out float arrive, out string moves)
    {
        float t = 0f, sp = executed ? speed : v.baseRunSpeed, flown = 0f;   // flown: ground already covered in the air when the feet touch the next floor
        var sb = new StringBuilder();
        foreach (var e in edges)
        {
            var P = g.plats[e.from]; var Q = g.plats[e.to];
            t += Traverse(v, P, entry, e.a, ref sp, executed, ref flown);
            float need = e.move == Move.Walk || e.move == Move.Drop || e.move == Move.Step ? 0f : e.speed;
            float at = Mathf.Max(sp, need);   // a move that wants more than is carried is made at what it wants (the line asks it)
            switch (e.move)
            {
                case Move.Walk: t += Flat(e.a, e.b) / Mathf.Max(1f, sp); break;
                case Move.Step: t += 0.8f; sp = v.baseRunSpeed; break;   // a stop and a jump up
                case Move.Drop:
                {
                    float h = Mathf.Max(0f, -e.dy), ft = LevelMetricsMath.FallTime(v, h, false);
                    t += ft;
                    flown = sp * ft;   // the fall carries the run on
                    sp = Land(v, Q, sp, h, executed);
                    break;
                }
                case Move.Jump:
                case Move.DoubleJump:
                {
                    var arc = e.move == Move.Jump ? LevelMetricsMath.Jump(v, at, v.jumpImpulse, e.dy, false) : LevelMetricsMath.DoubleJump(v, at, e.dy, true, false);
                    t += arc.time;
                    flown = Mathf.Max(0f, arc.distance - (e.gap + Margin));   // the arc lands past the edge; that much of the next floor is already behind
                    sp = Land(v, Q, at, Mathf.Max(0f, -e.dy), executed);
                    break;
                }
                case Move.WallRun:
                {
                    t += (e.gap + Margin) / Mathf.Max(1f, at);   // the run keeps its pace, the jump off it too
                    sp = Land(v, Q, at, Mathf.Max(0f, -e.dy), executed);
                    break;
                }
            }
            sb.Append(sb.Length > 0 ? " → " : "").Append(MoveWord(e));
            entry = e.b;
        }
        if (edges.Count > 0) t += Traverse(v, g.plats[edges[edges.Count - 1].to], entry, exit, ref sp, executed, ref flown);
        arrive = sp;
        moves = sb.ToString();
        return t;
    }

    /// The run across a floor from `from` to `to`: on the flat the speed is kept (executed) or is run speed; down a
    /// slope an executed line slides and gains
    static float Traverse(LevelMetricsMath.Values v, Plat P, Vector3 from, Vector3 to, ref float sp, bool executed, ref float flown)
    {
        float len = Flat(from, to);
        float credit = Mathf.Min(flown, len); len -= credit; flown = 0f;   // what the last arc or fall already covered
        if (len < 0.05f) return 0f;
        if (!executed) { sp = v.baseRunSpeed; return len / sp; }
        Vector3 d = to - from; d.y = 0f; d.Normalize();
        if (P.slope >= 8f && Vector3.Dot(P.Uphill, d) < -0.5f)
        {
            float gain = LevelMetricsMath.SlideSlopeGain(v, P.slope);
            float sp1 = Mathf.Sqrt(sp * sp + 2f * gain * len);
            float t = len / Mathf.Max(1f, (sp + sp1) * 0.5f);
            sp = sp1;
            return t;
        }
        return len / Mathf.Max(1f, sp);
    }

    /// The speed after landing on Q from `h` metres up at `sp`: a slope keeps the fall's share along it (executed),
    /// the flat keeps the horizontal (executed: the hop inside the grace) or settles to run speed
    static float Land(LevelMetricsMath.Values v, Plat Q, float sp, float h, bool executed)
    {
        if (!executed) return v.baseRunSpeed;
        if (Q.slope >= 8f && h > 0.5f)
        {
            float along = LevelMetricsMath.FallSpeed(v, h) * Mathf.Sin(Q.slope * Mathf.Deg2Rad);
            return Mathf.Sqrt(sp * sp + along * along);
        }
        return sp;
    }

    static string MoveWord(Edge e) => e.move switch
    {
        Move.Walk => "run",
        Move.Drop => "drop " + (-e.dy).ToString("0.0", Ci) + " m",
        Move.Step => "STEP",
        Move.Jump => "jump " + e.gap.ToString("0.0", Ci) + " m",
        Move.DoubleJump => "double jump " + e.gap.ToString("0.0", Ci) + " m",
        Move.WallRun => "wall run " + e.gap.ToString("0.0", Ci) + " m",
        _ => e.move.ToString(),
    };

    static string HiddenLine(HiddenReport h) =>
        "HIDDEN ROUTE " + h.segment + ", pad to pad: the lit line " + h.mainRun.ToString("0.0", Ci) + " s as a run (" + h.mainKept.ToString("0.0", Ci) + " s keeping " + h.speed.ToString("0", Ci) + " m/s) · the high line " + h.hidden.ToString("0.0", Ci) + " s at " + h.speed.ToString("0", Ci) + " m/s executed → SAVES " + h.SavesRun.ToString("0.0", Ci) + " s (" + h.SavesKept.ToString("0.0", Ci) + " s against a kept lit line); arrives at the merge at " + h.hiddenArrive.ToString("0", Ci) + " m/s against " + h.mainArrive.ToString("0", Ci) + " kept / " + "8 run. High line: " + h.hiddenMoves + ". Lit line: " + h.mainMoves + ".";

    // ---------------------------------------------------------------- the snowball (2026-10-29)

    const float SnowballCap = 40f;   // the model's ceiling: the fastest the user has gone in the gym (39.1 m/s, Docs/Runs/BeatGym-20261028-144145.csv)
    const float HopRoom = 3.3f;      // the headroom a hop wants: its apex (1.6 m) and the body (1.8 m); under less a hop bonks and the chain is not counted
    const float StraightMin = 20f;   // a flat this long with headroom is a straight that snowballs
    const float Body = 1.8f;         // the capsule, for the arc's head against a lintel

    public class SpeedCheck
    {
        public string segment; public float sized, fromPad, carriedIn, carried, peak, exit;
        public List<string> gaps = new List<string>();        // one line per gap: the launch speeds, the fate, the tolerance
        public List<string> straights = new List<string>();
    }

    /// The rider: its speed, the flat gathered since the last break (to be cashed as a hop chain), what it met each gap with
    class Rider
    {
        public float sp, peak, straight, straightFrom; public Vector3 straightAt, lastDir;
        public List<(Edge e, float wanted, float at)> launches = new List<(Edge, float, float)>();   // what it arrived at each gap with, and what it had to shed to for the move to land
        public List<(float len, float from, float to, Vector3 at)> straights = new List<(float, float, float, Vector3)>();
        public Dictionary<int, float> leaving = new Dictionary<int, float>();   // the speed it left every floor of the line at
    }

    /// Rides a segment's main line from `start` m/s: perfect hops over every flat with headroom, slides down every slope,
    /// a press at every landing, the wall jump's gain; a step, a climb or a ground turn of sixty degrees sheds to run speed
    static Rider Ride(Geometry g, LevelMetricsMath.Values v, Segment seg, float start)
    {
        var r = new Rider { sp = start, peak = start };
        Edge prevE = null;
        Vector3 entry = seg.from; entry.y = g.plats[seg.start].YAt(entry.x, entry.z);
        foreach (var e in seg.path)
        {
            var P = g.plats[e.from]; var Q = g.plats[e.to];
            Vector3 dir = e.b - entry; dir.y = 0f; dir = dir.sqrMagnitude > 1e-4f ? dir.normalized : r.lastDir;
            // the speed across this floor had the rider gone straight on (the skips leave from here), before any turn the line makes on it
            r.leaving[e.from] = Mathf.Max(r.leaving.TryGetValue(e.from, out float was) ? was : 0f, PeekPlus(v, r, P.slope < 8f ? Flat(entry, e.a) : 0f));
            if (r.lastDir != Vector3.zero && dir != Vector3.zero)
            {
                float turn = Vector3.Angle(r.lastDir, dir);
                if (turn >= 60f) { Cash(v, r); r.sp = v.baseRunSpeed; }   // the honest player turns on the ground and the speed goes; a hop carried in is off the edge
                else if (turn >= 20f) Cash(v, r);
            }
            if (P.name.StartsWith("Settle") || P.name.StartsWith("ControlKink")) { Cash(v, r); r.sp = v.baseRunSpeed; r.straight = 0f; }   // a corner built not to be cut (the gym's settle squares have a block on the inside; a kink pad is a ground turn): shed, and nothing gathered across it (the line's diagonal over the square is not a line a body can take)
            else Run(g, v, r, P, entry, e.a, dir, prevE != null ? g.plats[prevE.from].col : null);
            switch (e.move)
            {
                case Move.Walk: Gather(r, e.a, Flat(e.a, e.b)); break;
                case Move.Step: Cash(v, r); r.sp = v.baseRunSpeed; break;
                case Move.Drop:
                    Cash(v, r); r.launches.Add((e, r.sp, r.sp));
                    r.sp = Land(v, Q, r.sp, Mathf.Max(0f, -e.dy), true); Press(g, v, r, Q, e.b);
                    break;
                case Move.Jump:
                case Move.DoubleJump:
                case Move.WallRun:
                {
                    // the rider arrives with what it built and sheds to the most the move still lands at: a faster arc is a lintel or a fall
                    Cash(v, r);
                    float wanted = Mathf.Max(r.sp, e.speed);
                    Move how = HowOf(v, e, Mathf.Max(seg.sized, e.speed));
                    float at = Passable(g, v, e, how, wanted, e.speed);
                    r.launches.Add((e, wanted, at));
                    float h = how == Move.WallRun ? Mathf.Min(SnowballCap, LevelMetricsMath.WallJump(v, at, 0f).h) : at;   // the jump off a wall: the tangent boost and the push away
                    r.sp = Land(v, Q, h, Mathf.Max(0f, -e.dy), true); Press(g, v, r, Q, e.b);
                    break;
                }
            }
            r.peak = Mathf.Max(r.peak, r.sp);
            if (dir != Vector3.zero) r.lastDir = dir;
            prevE = e; entry = e.b;
        }
        if (seg.path.Count > 0)
        {
            var last = g.plats[seg.path[seg.path.Count - 1].to];
            Vector3 dir = seg.to - entry; dir.y = 0f; dir = dir.sqrMagnitude > 1e-4f ? dir.normalized : r.lastDir;
            if (r.lastDir != Vector3.zero && dir != Vector3.zero)
            {
                float turn = Vector3.Angle(r.lastDir, dir);
                if (turn >= 60f) { Cash(v, r); r.sp = v.baseRunSpeed; }
                else if (turn >= 20f) Cash(v, r);
            }
            if (last.name.StartsWith("Settle") || last.name.StartsWith("ControlKink")) { Cash(v, r); r.sp = v.baseRunSpeed; r.straight = 0f; }
            else Run(g, v, r, last, entry, seg.to, dir, g.plats[seg.path[seg.path.Count - 1].from].col);
        }
        Cash(v, r);
        return r;
    }

    /// The run across P from `from` to `to`: a downhill slope is slid, a climb sheds, a roofed flat is a slide or a bonk, a clear flat is gathered for the hop chain
    static void Run(Geometry g, LevelMetricsMath.Values v, Rider r, Plat P, Vector3 from, Vector3 to, Vector3 dir, Collider ignore)
    {
        float len = Flat(from, to);
        if (len < 0.05f) return;
        if (P.slope >= 8f)
        {
            Cash(v, r);
            float along = Vector3.Dot(P.Uphill, dir);
            if (along < -0.5f) r.sp = Mathf.Min(SnowballCap, Mathf.Sqrt(r.sp * r.sp + 2f * LevelMetricsMath.SlideSlopeGain(v, P.slope) * len));
            else if (along > 0.5f) r.sp = v.baseRunSpeed;
            r.peak = Mathf.Max(r.peak, r.sp);
            return;
        }
        float room = ClearanceAlong(g, P, from, to, out _, ignore);
        if (room < HopRoom)
        {
            Cash(v, r);
            if (room < 1.95f) r.sp = Mathf.Max(v.baseRunSpeed, r.sp - v.slideFriction * len);   // a slide on the flat bleeds slideFriction a metre; under run speed you stand and run
            return;
        }
        Gather(r, from, len);
    }

    static void Gather(Rider r, Vector3 at, float len)
    {
        if (len < 0.05f) return;
        if (r.straight <= 0f) { r.straightFrom = r.sp; r.straightAt = at; }
        r.straight += len;
    }

    /// The hop chain over the flat gathered so far: a clean arc, a perfect press, again, as far as the floor allows
    static void Cash(LevelMetricsMath.Values v, Rider r)
    {
        if (r.straight < 0.5f) { r.straight = 0f; return; }
        float pos = 0f, s0 = r.sp;
        while (true)
        {
            float reach = LevelMetricsMath.Jump(v, r.sp, v.jumpImpulse, 0f, false).distance;
            if (pos + reach > r.straight) break;
            pos += reach;
            r.sp = Mathf.Min(SnowballCap, r.sp * LevelMetricsMath.BhopMultiplier(v, r.sp));
        }
        r.peak = Mathf.Max(r.peak, r.sp);
        r.straights.Add((r.straight, s0, r.sp, r.straightAt));
        r.straight = 0f;
    }

    /// What the chain over the flat gathered so far would reach, without cashing it
    static float Peek(LevelMetricsMath.Values v, Rider r) => PeekPlus(v, r, 0f);

    /// The same with `more` metres of flat added
    static float PeekPlus(LevelMetricsMath.Values v, Rider r, float more)
    {
        float pos = 0f, sp = r.sp, straight = r.straight + more;
        if (straight < 0.5f) return sp;
        while (true)
        {
            float reach = LevelMetricsMath.Jump(v, sp, v.jumpImpulse, 0f, false).distance;
            if (pos + reach > straight) break;
            pos += reach;
            sp = Mathf.Min(SnowballCap, sp * LevelMetricsMath.BhopMultiplier(v, sp));
        }
        return sp;
    }

    /// The press at a landing: a flat floor with headroom pays the perfect hop's multiplier (a slope landing was paid by Land)
    static void Press(Geometry g, LevelMetricsMath.Values v, Rider r, Plat Q, Vector3 at)
    {
        if (Q.slope >= 8f || Clearance(g, Q, at) < HopRoom) return;
        r.sp = Mathf.Min(SnowballCap, r.sp * LevelMetricsMath.BhopMultiplier(v, r.sp));
    }

    /// The move the gap is really made with at the speed it is sized for: the graph reads a plain jump that needs 16 m/s
    /// as a double jump at 10 (cheaper on its ladder), but a beat that makes 16 m/s on its slope or its ramp jumps it plain
    static Move HowOf(LevelMetricsMath.Values v, Edge e, float sized) =>
        e.move == Move.DoubleJump && Range(v, Move.Jump, sized, 0f, sized, v.jumpImpulse, e.dy) >= e.gap + Margin ? Move.Jump : e.move;

    /// The most speed, down from `wanted` in steps of one, the move still lands clean at; `floor` if none does
    static float Passable(Geometry g, LevelMetricsMath.Values v, Edge e, Move how, float wanted, float floor)
    {
        for (float Y = wanted; Y >= floor - 0.01f; Y -= 1f) if (Fate(g, v, e, Y, how, out _) == 0) return Y;
        return floor;
    }

    /// What becomes of the gap `e` taken at `Y` m/s with the move `how`: 0 lands clean on a floor, 1 hits a lintel or a
    /// roof on the way, 2 finds no floor under the landing, 3 falls short of the gap. `what` says it in words
    static int Fate(Geometry g, LevelMetricsMath.Values v, Edge e, float Y, Move how, out string what)
    {
        var P = g.plats[e.from]; var Q = g.plats[e.to];
        Vector3 dir = Q.Centre - e.a; dir.y = 0f;   // aimed at the middle of the floor the line lands on, as a player aims, not at its nearest corner
        if (dir.sqrMagnitude < 1e-4f) { what = "no direction"; return 3; }
        dir.Normalize();
        float ya = e.a.y;
        // the flight: where it starts along the line, its speed and its rise (a wall run runs level first, then jumps off)
        float x0 = 0f, h = Y, up = v.jumpImpulse;
        if (how == Move.WallRun)
        {
            WallCarry(g, P, e.a, e.b, ya, Y, e.dy, v, out Wall w, out x0, out h, out up);
            if (w == null) { what = "no wall to run at this speed"; return 3; }
        }
        // the floor the flight lands on: the flight marched out a quarter metre at a time against every floor its line
        // crosses — the first floor whose top the feet come down through is the landing; a floor's footprint entered
        // from the side with the feet level with its top is its face, hit. (A double jump's late press is timed to the
        // height of the floor the line lands on, `dyT`.)
        float dyT = e.dy;
        float Rmax = how == Move.DoubleJump ? LevelMetricsMath.DoubleJump(v, Y, -40f, true, false).distance : (how == Move.WallRun ? x0 : 0f) + LevelMetricsMath.Jump(v, h, up, -40f, false).distance;   // as far down as the graph looks: a 14 m drop lands on a slope that goes on down
        Vector3 far = e.a + dir * Rmax;
        float bx0 = Mathf.Min(e.a.x, far.x) - 0.5f, bx1 = Mathf.Max(e.a.x, far.x) + 0.5f, bz0 = Mathf.Min(e.a.z, far.z) - 0.5f, bz1 = Mathf.Max(e.a.z, far.z) + 0.5f;
        var near = new List<Plat>();
        foreach (var cand in g.plats)
            if (cand != P && !cand.lift && cand.col != P.col && cand.maxX >= bx0 && cand.minX <= bx1 && cand.maxZ >= bz0 && cand.minZ <= bz1) near.Add(cand);
        var prevIn = new bool[near.Count]; var prevAbove = new bool[near.Count];
        Plat T = null; float RT = 0f;
        const float Step = 0.25f;
        for (float u = Step; u <= Rmax + 0.01f && T == null; u += Step)
        {
            Vector3 p = e.a + dir * u;
            float feet = ya + FeetAt(v, how, Y, x0, h, up, dyT, u);
            for (int i = 0; i < near.Count; i++)
            {
                var cand = near[i];
                if (!Inside(cand, p, 0.2f)) { prevIn[i] = false; prevAbove[i] = false; continue; }
                float top = cand.YAt(Mathf.Clamp(p.x, cand.minX, cand.maxX), Mathf.Clamp(p.z, cand.minZ, cand.maxZ));
                bool above = feet > top + 0.02f;
                if (!prevIn[i])
                {
                    if (!above && feet > top - Body && u > x0 + 0.3f) { what = "hits the side of '" + cand.name + "' " + u.ToString("0", Ci) + " m out"; return 1; }
                }
                else if (!above && prevAbove[i]) { T = cand; RT = u; break; }
                prevIn[i] = true; prevAbove[i] = above;
            }
        }
        if (T == null) { what = "finds no floor under its landing"; return 2; }
        if (RT < e.gap + Margin - 0.3f) { what = "falls short of the gap"; return 3; }
        // a wall across the way, met before the landing: the body's height against the face where the path crosses it
        foreach (var w in g.walls)
        {
            float facing = Vector3.Dot(w.n, dir);
            if (Mathf.Abs(facing) < 0.5f) continue;
            float u = Vector3.Dot(w.c - e.a, w.n) / facing;
            if (u < 0.3f || u > RT - 0.2f) continue;
            Vector3 p = e.a + dir * u;
            if (Mathf.Abs(Vector3.Dot(p - w.c, w.u)) > w.halfLen + 0.4f) continue;
            float feet = ya + FeetAt(v, how, Y, x0, h, up, dyT, u);
            if (feet < w.top - 0.05f && feet + Body > w.bottom + 0.05f) { what = "hits '" + w.name + "' " + u.ToString("0", Ci) + " m out"; return 1; }
        }
        // a lintel or a roof on the way: the head against an underside with the feet below its top
        float landTop = T.YAt(Mathf.Clamp((e.a + dir * RT).x, T.minX, T.maxX), Mathf.Clamp((e.a + dir * RT).z, T.minZ, T.maxZ));
        foreach (var c in g.ceils)
        {
            if (c.col == P.col || c.col == T.col) continue;
            if (Mathf.Min(c.maxX - c.minX, c.maxZ - c.minZ) < 0.6f) continue;
            if (Mathf.Abs(c.YAt(Mathf.Clamp((e.a + dir * RT).x, c.minX, c.maxX), Mathf.Clamp((e.a + dir * RT).z, c.minZ, c.maxZ)) + c.thick - landTop) < 0.15f) continue;   // a floor beside the landing at its own level: its underside is not in the way
            if (!Clip(e.a, dir, RT, c.minX - 0.4f, c.maxX + 0.4f, c.minZ - 0.4f, c.maxZ + 0.4f, out float u0, out float u1)) continue;
            u0 = Mathf.Max(u0, x0);
            if (u1 <= u0) continue;
            // sampled along the crossing, against the underside right there: a pitched belly (a landing slope) is low at one end and high at the other
            for (float u = u0; u <= u1 + 0.01f; u += 0.5f)
            {
                Vector3 p = e.a + dir * u;
                float feet = ya + FeetAt(v, how, Y, x0, h, up, dyT, u);
                float under = c.YAt(Mathf.Clamp(p.x, c.minX, c.maxX), Mathf.Clamp(p.z, c.minZ, c.maxZ));
                if (feet + Body > under && feet < under + c.thick - 0.1f) { what = "hits the underside of '" + (c.col != null ? c.col.gameObject.name : "a roof") + "' " + u.ToString("0", Ci) + " m out"; return 1; }
            }
        }
        what = "lands on '" + T.name + "' " + (RT - e.gap).ToString("0.0", Ci) + " m past the gap" + (T != g.plats[e.to] ? " (not the floor the line lands on)" : "");
        return 0;
    }

    /// How far the move carries to a landing `dy` away (0 when it cannot reach that height)
    static float Range(LevelMetricsMath.Values v, Move how, float Y, float x0, float h, float up, float dy)
    {
        LevelMetricsMath.Arc a = how == Move.DoubleJump ? LevelMetricsMath.DoubleJump(v, Y, dy, true, false) : LevelMetricsMath.Jump(v, h, up, dy, false);
        if (a.apex + 0.02f < dy) return 0f;
        return (how == Move.WallRun ? x0 : 0f) + a.distance;
    }

    /// The feet's height over the launch at `u` metres along the line: level along a wall run, then the arc (a double jump's second arc from its late press at the landing height)
    static float FeetAt(LevelMetricsMath.Values v, Move how, float Y, float x0, float h, float up, float dyT, float u)
    {
        if (u <= x0) return 0f;
        float t = (u - x0) / Mathf.Max(1f, h);
        float y = up * t - 0.5f * v.gravity * t * t;
        if (how != Move.DoubleJump) return y;
        float disc = up * up - 2f * v.gravity * dyT;
        float t1 = disc > 0f ? (up + Mathf.Sqrt(disc)) / v.gravity : up / v.gravity;   // the first arc back down to the landing height: the late press
        if (t <= t1) return y;
        float t2 = t - t1;
        return dyT + v.doubleJumpImpulse * t2 - 0.5f * v.gravity * t2 * t2;
    }

    static bool Inside(Plat T, Vector3 p, float slack) => p.x >= T.minX - slack && p.x <= T.maxX + slack && p.z >= T.minZ - slack && p.z <= T.maxZ + slack;

    /// The stretch of the line from `a` along `dir` (up to `len`) that lies inside the rectangle, as distances along it
    static bool Clip(Vector3 a, Vector3 dir, float len, float minX, float maxX, float minZ, float maxZ, out float u0, out float u1)
    {
        u0 = 0f; u1 = len;
        if (!Slab(a.x, dir.x, minX, maxX, ref u0, ref u1)) return false;
        if (!Slab(a.z, dir.z, minZ, maxZ, ref u0, ref u1)) return false;
        return u1 > u0;
    }

    static bool Slab(float p, float d, float lo, float hi, ref float u0, ref float u1)
    {
        if (Mathf.Abs(d) < 1e-5f) return p >= lo && p <= hi;
        float t0 = (lo - p) / d, t1 = (hi - p) / d;
        if (t0 > t1) { float t = t0; t0 = t1; t1 = t; }
        u0 = Mathf.Max(u0, t0); u1 = Mathf.Min(u1, t1);
        return u1 > u0;
    }

    const float TrivialOver = 1.4f;   // a gap that lands clean at this much over its sized speed (two perfect hops' worth) tests nothing

    /// The snowball's verdict on a segment: the rides, every gap's fate at their speeds and its tolerance, the skips between
    /// floors of the line, the free straights; the findings that follow
    static SpeedCheck Snowball(Geometry g, LevelMetricsMath.Values v, Segment seg, float carriedIn, Result res, List<Edge>[] edges)
    {
        var sc = new SpeedCheck { segment = seg.name, sized = seg.sized, carriedIn = carriedIn };
        var pad = Ride(g, v, seg, v.baseRunSpeed);
        var car = Ride(g, v, seg, carriedIn);
        sc.exit = car.sp; sc.peak = car.peak;
        sc.fromPad = pad.launches.Count > 0 ? pad.launches[0].wanted : pad.sp;
        sc.carried = car.launches.Count > 0 ? car.launches[0].wanted : car.sp;
        // the gaps of the line, and the skips: a jump between two floors of the line that leaves floors of it out
        var gaps = new List<(Edge e, float Yp, float Yc, bool skip)>();
        for (int i = 0; i < car.launches.Count && i < pad.launches.Count; i++) gaps.Add((car.launches[i].e, pad.launches[i].wanted, car.launches[i].wanted, false));
        var order = new Dictionary<int, int>(); var seqList = Sequence(seg);
        for (int i = 0; i < seqList.Count; i++) order[seqList[i]] = i;
        foreach (int u in seqList)
        {
            Edge best = null;
            foreach (var x in edges[u])
            {
                if (!(x.move == Move.Jump || x.move == Move.DoubleJump || x.move == Move.WallRun) || !order.TryGetValue(x.to, out int at) || at <= order[u] + 1 || seg.path.Contains(x)) continue;
                if (best != null && order[x.to] <= order[best.to]) continue;
                // only a skip some speed up to the cap really lands: the graph does not see walls and blocks in an arc's way
                bool possible = false;
                for (float Y = Mathf.Ceil(Mathf.Max(seg.sized, x.speed)); Y <= SnowballCap + 0.01f && !possible; Y += 2f) possible = Fate(g, v, x, Y, HowOf(v, x, Mathf.Max(seg.sized, x.speed)), out _) == 0;
                if (possible) best = x;
            }
            if (best != null) gaps.Add((best, pad.leaving.TryGetValue(u, out float lp) ? lp : v.baseRunSpeed, car.leaving.TryGetValue(u, out float lc) ? lc : v.baseRunSpeed, true));
        }
        foreach (var (e, Yp, Yc, skip) in gaps)
        {
            if (e.move == Move.Drop || e.gap < 1.5f) continue;
            var P = g.plats[e.from]; var Q = g.plats[e.to];
            float sized = Mathf.Max(seg.sized, e.speed);
            if (skip && (Q.name.StartsWith("Settle") || Mathf.Max(Yp, Yc) < 0.8f * sized)) continue;   // a hop onto the settle square is the corner's own business; a skip the rider is nowhere near fast enough for is not news
            Move how = HowOf(v, e, sized);
            // the move's tolerance: up from the sized speed until its landing is a lintel or nothing
            float tol = SnowballCap; string then = null;
            for (float Y = Mathf.Ceil(sized); Y <= SnowballCap + 0.01f; Y += 1f)
            {
                int fate = Fate(g, v, e, Y, how, out string what);
                if (fate == 1 || fate == 2) { tol = Y - 1f; then = what; break; }
            }
            // trivial: a plain jump at the speed the snowball arrives with lands clean, well over the sized speed
            int padFate = Fate(g, v, e, Yp, Move.Jump, out string padWhat), carFate = Fate(g, v, e, Yc, Move.Jump, out string carWhat);
            bool control = Q.name.StartsWith("Control") || Q.name.StartsWith("Settle");   // a landing that is itself a control piece (a square to turn on, a kink pad) is meant to take the arc; what follows it is the test
            bool padTrivial = padFate == 0 && Yp >= TrivialOver * sized && !control;
            bool carTrivial = carFate == 0 && Yc >= TrivialOver * sized && !control;
            string gapWord = (skip ? "THE SKIP " : "") + "'" + P.name + "' → '" + Q.name + "', " + MoveWord(e) + " " + UpDown(e.dy) + ", sized " + sized.ToString("0", Ci) + " m/s";
            sc.gaps.Add(gapWord + ": arrives at " + Yp.ToString("0.0", Ci) + " m/s from the pad (a plain jump " + padWhat + (padTrivial ? ": TRIVIAL" : "") + "), " + Yc.ToString("0.0", Ci) + " carried (" + carWhat + (carTrivial ? ": TRIVIAL" : "") + ") · the " + (how == Move.WallRun ? "wall run" : how == Move.DoubleJump ? "double jump" : "jump") + " lands up to " + tol.ToString("0", Ci) + " m/s" + (then != null ? "; at " + (tol + 1f).ToString("0", Ci) + " it " + then : "; nothing punishes an overshoot"));
            if (padTrivial || carTrivial)
            {
                res.trivial++;
                float Y = padTrivial ? Yp : Yc;
                res.findings.Add(new Finding { severity = padTrivial ? 30 : 20, title = padTrivial ? "TRIVIAL AT SPEED" : "TRIVIAL WITH SPEED CARRIED IN", segment = seg.name, at = e.a,
                    experience = "A hop chain " + (padTrivial ? "from the reset pad" : "carried in from the beat before") + " arrives here at " + Y.ToString("0", Ci) + " m/s and a plain jump " + (padTrivial ? padWhat : carWhat) + ", with nothing to think about: the beat is sized for " + sized.ToString("0", Ci) + " m/s and tests nothing at this speed" + (e.move == Move.WallRun ? " (the wall run it was built around is not needed)." : e.move == Move.DoubleJump && how == Move.DoubleJump ? " (the pip it was built around is not needed)." : "."),
                    where = "Segment " + seg.name + ": " + gapWord + ", at " + Fmt(e.a) + ".",
                    why = "The gap plus the margin is " + (e.gap + Margin).ToString("0.0", Ci) + " m; a plain jump at " + Y.ToString("0", Ci) + " m/s covers " + Range(v, Move.Jump, Y, 0f, Y, v.jumpImpulse, e.dy).ToString("0.0", Ci) + " m and lands on floor. A shorter landing under a portal, a bend, or less flat before the gap would make the speed a problem." });
            }
            else if (tol >= SnowballCap - 0.5f && Q.slope < 8f && !control && !skip)
            {
                res.unpunished++;
                res.findings.Add(new Finding { severity = 15, title = "NO PUNISHMENT FOR SPEED", segment = seg.name, at = e.b,
                    experience = "Taken at any speed up to " + SnowballCap.ToString("0", Ci) + " m/s the move lands on floor and runs on: nothing here asks the player to control their speed, only to have it.",
                    where = "Segment " + seg.name + ": " + gapWord + ", landing at " + Fmt(e.b) + ".",
                    why = "The floor past the landing catches every overshoot and no lintel stands in the arc. A portal at the landing's end or a landing that bends would set a top speed." });
            }
        }
        foreach (var st in car.straights)
        {
            if (st.len < StraightMin || st.to < st.from + 0.5f) continue;
            res.straights++;
            sc.straights.Add("a straight of " + st.len.ToString("0", Ci) + " m from " + Fmt(st.at) + ": a hop chain goes " + st.from.ToString("0.0", Ci) + " → " + st.to.ToString("0.0", Ci) + " m/s on it");
            res.findings.Add(new Finding { severity = 20, title = "A STRAIGHT THAT SNOWBALLS", segment = seg.name, at = st.at,
                experience = st.len.ToString("0", Ci) + " m of flat with headroom and nothing in it: a perfect-hop chain goes from " + st.from.ToString("0.0", Ci) + " to " + st.to.ToString("0.0", Ci) + " m/s here for nothing.",
                where = "Segment " + seg.name + ": from " + Fmt(st.at) + ".",
                why = "Speed should be earned in specific places. A bend, a portal every eight metres (a colonnade caps a hop chain near 11 m/s), a drop or a gap would break the chain." });
        }
        return sc;
    }

    // ---------------------------------------------------------------- the lift (2026-10-29)

    public class LiftReport { public string wall, segment, from; public Vector3 at; public float wallHeight, length, floorY, cornice, rise, afterJump, afterDouble, reachY; public bool toTop; public List<string> reaches = new List<string>(); }

    /// Every wall a run can be entered on from a floor at its foot, jumped at (the worst case): how high the run, the
    /// wall jump and the pip lift the player (LevelMetricsMath.WallLift, with the wall's top and any ceiling over the
    /// run), and which surfaces above the floor that puts within reach along the wall. One report per wall (both
    /// faces of a box are in the list; the first with a floor at its foot speaks for it)
    static void WallLifts(Geometry g, LevelMetricsMath.Values v, Result res)
    {
        var segOf = new Dictionary<int, string>(); var orderOf = new Dictionary<int, int>(); int order = 0;
        foreach (var seg in res.segments)
        {
            if (seg.start >= 0) { segOf[seg.start] = seg.name; if (!orderOf.ContainsKey(seg.start)) orderOf[seg.start] = order++; }
            foreach (var e in seg.path) { segOf[e.to] = seg.name; if (!orderOf.ContainsKey(e.to)) orderOf[e.to] = order++; }
        }
        var done = new HashSet<Collider>();
        foreach (var w in g.walls)
        {
            if (w.name == "Lintel" || w.name == "Post" || w.name == "Cornice") continue;
            if (w.top - w.bottom < 2f || w.halfLen < 1.5f) continue;
            if (w.col != null && done.Contains(w.col)) continue;
            // the floor at its foot: a floor of the line within a body of the wall's bottom, in front of the face
            Plat P = null; float floorY = 0f; Vector3 foot = Vector3.zero;
            for (int k = -1; k <= 1 && P == null; k++)
            {
                Vector3 probe = w.c + w.u * (k * w.halfLen * 0.6f) + w.n * 1.0f;
                foreach (var p in g.plats)
                {
                    if (p.lift || !Inside(p, probe, 0.3f)) continue;
                    float y = p.YAt(Mathf.Clamp(probe.x, p.minX, p.maxX), Mathf.Clamp(probe.z, p.minZ, p.maxZ));
                    if (y < w.bottom - 1.6f || y > w.bottom + 1.6f || y > w.top - 1.6f) continue;
                    P = p; floorY = y; foot = probe; foot.y = y; break;
                }
            }
            if (P == null) continue;
            if (w.col != null) done.Add(w.col);
            float attachY = floorY + 0.3f;   // the feet a hair up: the jump has just left the floor when the wall takes
            // a ceiling over the strip the body runs in: the lowest underside within a run's rise
            float cornice = float.PositiveInfinity;
            Vector3 q = foot + w.n * (-0.3f);
            foreach (var c in g.ceils)
            {
                if (c.col == P.col) continue;
                if (q.x < c.minX - 0.2f || q.x > c.maxX + 0.2f || q.z < c.minZ - 0.2f || q.z > c.maxZ + 0.2f) continue;
                float h = c.YAt(q.x, q.z) - attachY;
                if (h > 0.5f && h < 12f && h < cornice) cornice = h;
            }
            var lift = LevelMetricsMath.WallLift(v, v.jumpImpulse, w.top - attachY, cornice);
            float reachY = attachY + lift.afterDouble;
            var r = new LiftReport { wall = w.name, segment = segOf.TryGetValue(P.id, out string s) ? s : "off the line", from = P.name, at = w.c, wallHeight = w.top - w.bottom, length = w.halfLen * 2f, floorY = floorY, cornice = cornice, rise = lift.rise, afterJump = lift.afterJump, afterDouble = lift.afterDouble, reachY = reachY, toTop = lift.toTop };
            var names = new Dictionary<string, int>();
            foreach (var p in g.plats)
            {
                if (p == P || p.col == P.col || p.name == "Cornice" || p.name.EndsWith("Body") || p.name.StartsWith("Hidden")) continue;   // a hidden line's pieces are reached from its own wall by design
                if (orderOf.TryGetValue(p.id, out int po) && orderOf.TryGetValue(P.id, out int fo) && po <= fo) continue;   // a floor of the line behind the player is no skip
                float top = p.YAt(p.Centre.x, p.Centre.z);
                if (top < floorY + 1.5f || top > reachY + 0.1f) continue;
                if (!InWallsReach(w, p)) continue;
                string label = p.col == w.col ? "the wall's own top (" + Mathf.Min(p.Width, p.Depth).ToString("0.0", Ci) + " m wide)" : "'" + p.name + "'";
                names[label] = names.TryGetValue(label, out int n) ? n + 1 : 1;
            }
            foreach (var kv in names) r.reaches.Add(kv.Value > 1 ? kv.Key + " ×" + kv.Value : kv.Key);
            res.lifts.Add(r);
            if (r.reaches.Count == 0 || !orderOf.ContainsKey(P.id)) continue;   // a wall beside a floor the line never stands on is noted, not a finding
            res.liftSkips++;
            res.findings.Add(new Finding { severity = 45, title = "A WALL THAT LIFTS", segment = r.segment, at = w.c + w.n * 1f,
                experience = "Jump at '" + w.name + "' and run it: the run rises " + lift.rise.ToString("0.0", Ci) + " m" + (lift.toTop ? " to its top" : "") + ", the wall jump off it tops out " + lift.afterJump.ToString("0.0", Ci) + " m over the floor and a double jump after it " + lift.afterDouble.ToString("0.0", Ci) + " m. That puts " + string.Join(", ", r.reaches) + " underfoot, over the beat.",
                where = "Segment " + r.segment + ": '" + w.name + "' " + r.length.ToString("0", Ci) + " m long, " + r.wallHeight.ToString("0.0", Ci) + " m tall, beside '" + P.name + "' at " + Fmt(w.c) + (float.IsInfinity(cornice) ? ", nothing over the run" : ", a ceiling " + cornice.ToString("0.0", Ci) + " m over it") + ".",
                why = "The run keeps the jump's rise and adds the entry pop under almost no gravity; the wall jump adds half of what is left; the wall touch refunds the pip. A cornice 3 m over the floor along the wall holds the lift to about 5 m; a wall that rises higher than the lift has no top to land on; or the motor could clamp the rise on entry." });
        }
    }

    /// Whether a floor lies where a run along the wall and the jumps off it can come down: along the wall's line, up
    /// to 22 m past either end, and no more than 12 m out from its face (the push away and the air control)
    static bool InWallsReach(Wall w, Plat p)
    {
        foreach (var k in new[] { new Vector3(p.minX, 0f, p.minZ), new Vector3(p.maxX, 0f, p.minZ), new Vector3(p.minX, 0f, p.maxZ), new Vector3(p.maxX, 0f, p.maxZ), new Vector3((p.minX + p.maxX) * 0.5f, 0f, (p.minZ + p.maxZ) * 0.5f) })
        {
            Vector3 d = k - new Vector3(w.c.x, 0f, w.c.z);
            float along = Mathf.Abs(Vector3.Dot(d, w.u)), lateral = Vector3.Dot(d, w.n);
            if (along <= w.halfLen + 22f && lateral >= -1f && lateral <= 12f) return true;
        }
        return false;
    }

    // ---------------------------------------------------------------- the findings

    public class Finding { public int severity; public string title, experience, where, why; public Vector3 at; public string segment; public int number; }

    public class Result
    {
        public LevelKind kind; public string title;
        public List<Finding> findings = new List<Finding>();
        public List<Segment> segments = new List<Segment>();
        public List<HiddenReport> hidden = new List<HiddenReport>();   // every hidden line found and timed
        public List<SpeedCheck> speed = new List<SpeedCheck>();        // the snowball's verdict on every segment
        public List<LiftReport> lifts = new List<LiftReport>();        // every wall a run can be entered on from a floor, and what its lift reaches
        public int liftSkips;
        public int trivial, unpunished, straights;
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
            bool hasHidden = g.plats.Exists(IsHidden);
            for (int i = 0; i + 1 < way.Count; i++)
            {
                if (Flat(way[i].at, way[i + 1].at) < 2f && Mathf.Abs(way[i].at.y - way[i + 1].at.y) < 2f) continue;
                string segName = way[i].name + " → " + way[i + 1].name;
                var seg = Walk(g, edges, segName, way[i].at, way[i + 1].at);
                seg.sized = BeatLibrary.SizedSpeed(way[i].name, reach.v);   // the speed the beat behind this pad sizes its gap for (run speed where the pad is not a beat's)
                res.segments.Add(seg);
                // THE HIDDEN ROUTES (2026-10-28): the same stretch walked without the hidden floors (the lit line) and without
                // the lit line's own (the high line); where they part, each is timed and the saving reported
                if (!hasHidden || seg.broken) continue;
                var mainOnly = Walk(g, edges, segName, way[i].at, way[i + 1].at, IsHidden);
                var hiddenOnly = Walk(g, edges, segName, way[i].at, way[i + 1].at, IsMainOnly);
                if (mainOnly.broken || hiddenOnly.broken) continue;
                var hr = CompareRoutes(g, reach.v, segName, mainOnly, hiddenOnly, BeatLibrary.ExpectedSpeed(way[i].name, reach.v));
                if (hr == null) continue;
                res.hidden.Add(hr);
                Debug.Log("LevelValidator: " + HiddenLine(hr));
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
                    // the line's own floor: narrow, uphill, under a ceiling (a control beat's beams are narrow on purpose)
                    if (Across(Q, dir) < 1f && segLen > 1f && !Q.name.StartsWith("Control"))
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
                    float clearP = ClearanceAlong(g, P, entry, e.a, out Vector3 lowAt, prevE != null ? g.plats[prevE.from].col : null), clearQ = Clearance(g, Q, e.b, P.col);   // the belly of the floor just stepped off is not a ceiling over this one
                    float clear = Mathf.Min(clearP, clearQ);
                    Vector3 clearAt = clearP <= clearQ ? lowAt : e.b;
                    string clearOver = clearP <= clearQ ? P.name : Q.name;
                    if (clear < 1.0f)
                        res.findings.Add(new Finding { severity = 80, title = "A CEILING TOO LOW EVEN TO SLIDE", segment = seg.name, at = clearAt,
                            experience = "Headroom of " + clear.ToString("0.00", Ci) + " m: a run stops here and a slide (0.9 m) scrapes or stops. The line is blocked unless there is another way round.",
                            where = "Segment " + seg.name + ": over '" + clearOver + "' at " + Fmt(clearAt) + ".", why = "The underside of " + Over(g, clearP <= clearQ ? P : Q, clearAt) + " sits within a metre of the floor." });
                    else if (clear < 1.95f)
                        res.findings.Add(new Finding { severity = 15, title = "A FORCED SLIDE", segment = seg.name, at = clearAt,
                            experience = "Headroom of " + clear.ToString("0.00", Ci) + " m: the only way through is a slide, and a hop into it is a stop. Fine as a beat; a problem if it comes without warning at speed.",
                            where = "Segment " + seg.name + ": over '" + clearOver + "' at " + Fmt(clearAt) + ".", why = "The underside of " + Over(g, clearP <= clearQ ? P : Q, clearAt) + " is between 1.0 and 1.9 m over the floor." });
                    // the turn: a dead stop against a wall
                    if (prevE != null)
                    {
                        Vector3 pd = prevE.b - prevE.a; pd.y = 0f;
                        bool meant = P.name.StartsWith("Settle") || P.name.StartsWith("Control");   // the gym's settle corners and the control beat's kinks turn on purpose: the turn IS the test
                        if (pd.sqrMagnitude > 1e-3f && Vector3.Angle(pd.normalized, dir) >= 60f && !meant)
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
            // THE SNOWBALL (2026-10-29): the same line ridden by a player who perfect-hops every flat, slides every slope and
            // presses at every landing, from the pad and with what the segment before handed on; every gap tried at that speed
            float carried = reach.v.baseRunSpeed;
            foreach (var seg in res.segments)
            {
                if (seg.broken) continue;
                var sc = Snowball(g, reach.v, seg, carried, res, edges);
                carried = sc.exit;
                res.speed.Add(sc);
            }
            // THE LIFT (2026-10-29): every wall at a floor's edge, jumped at and run: how high that puts the player, and onto what
            WallLifts(g, reach.v, res);
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
            if (res.hidden.Count > 0) { var sv = new List<string>(); foreach (var h in res.hidden) sv.Add(h.SavesRun.ToString("0.0", Ci) + " s"); res.summaryLine += ", " + res.hidden.Count + " hidden route(s) saving " + string.Join(" / ", sv); }
            res.summaryLine += ", at speed: " + res.trivial + " trivial gap(s), " + res.unpunished + " unpunished, " + res.straights + " free straight(s), " + res.liftSkips + " wall(s) that lift onto something";
            WriteReport(res, g);
            LevelOverhead.WriteImages(kind.ToString(), res.bounds, g, res.segments, res.findings, built.roots);
            foreach (var h in res.hidden)   // the view from each hidden line's reset pad, as the player sees it
            {
                string tag = h.segment.Contains(" → ") ? h.segment.Substring(0, h.segment.IndexOf(" → ")) : h.segment;
                try { LevelOverhead.ShootFromSpawn(kind + "-" + tag.Replace(' ', '-'), h.at, h.forward); } catch (Exception e) { Debug.LogWarning("LevelValidator: the hidden route shot failed (" + e.Message + ")"); }
            }
            return res;
        }
        finally { Tear(built); }
    }

    static float Across(Plat Q, Vector3 dir) => Mathf.Abs(dir.x) >= Mathf.Abs(dir.z) ? Q.Depth : Q.Width;

    static float Clearance(Geometry g, Plat Q, Vector3 at) => Clearance(g, Q, at, out _);
    static float Clearance(Geometry g, Plat Q, Vector3 at, Collider ignore) => Clearance(g, Q, at, out _, ignore);

    static float Clearance(Geometry g, Plat Q, Vector3 at, out Ceil hit, Collider ignore = null)
    {
        float floor = Q.YAt(at.x, at.z), best = float.MaxValue;
        hit = null;
        foreach (var c in g.ceils)
        {
            if (c.col == Q.col || (ignore != null && c.col == ignore)) continue;   // the floor's own belly; the belly of the floor just stepped off (a step down reads its seam as a ceiling otherwise)
            if (c.maxX - c.minX < 3f && c.maxZ - c.minZ < 3f) continue;   // a desk, a crate, a column: a thing to run round, not a ceiling
            if (Mathf.Min(c.maxX - c.minX, c.maxZ - c.minZ) < 0.6f) continue;   // the end face of a plate stood on edge: not a ceiling
            if (at.x < c.minX - 0.3f || at.x > c.maxX + 0.3f || at.z < c.minZ - 0.3f || at.z > c.maxZ + 0.3f) continue;
            float h = c.YAt(at.x, at.z) - floor;
            if (h > 0.2f && h < best) { best = h; hit = c; }
        }
        return best;
    }

    /// The name of what hangs over a point of Q, for the report
    static string Over(Geometry g, Plat Q, Vector3 at)
    {
        Clearance(g, Q, at, out Ceil hit);
        return hit != null && hit.col != null ? "'" + hit.col.gameObject.name + "' (its underside at y " + hit.YAt(at.x, at.z).ToString("0.0", Ci) + ", " + (hit.maxX - hit.minX).ToString("0", Ci) + " by " + (hit.maxZ - hit.minZ).ToString("0", Ci) + " m)" : "something";
    }

    /// The least headroom along a run across P from `from` to `to`, sampled every metre, and where it was
    static float ClearanceAlong(Geometry g, Plat P, Vector3 from, Vector3 to, out Vector3 lowAt, Collider ignore = null)
    {
        float best = float.MaxValue; lowAt = to;
        int n = Mathf.Max(1, Mathf.CeilToInt(Flat(from, to)));
        for (int i = 0; i <= n; i++)
        {
            Vector3 p = Vector3.Lerp(from, to, (float)i / n);
            float c = Clearance(g, P, p, ignore);
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
        var frame = LevelOverhead.Frame(r.bounds);
        sb.AppendLine("Plan frame: centre (" + frame.cx.ToString("0.00", Ci) + ", " + frame.cz.ToString("0.00", Ci) + "), " + frame.size.ToString("0.00", Ci) + " m across, 2048 px, north up: pixel x = (x - cx + size/2) / size * 2048, pixel y = (cz + size/2 - z) / size * 2048.");
        sb.AppendLine();
        if (r.hidden.Count > 0)
        {
            sb.AppendLine("## Hidden routes");
            sb.AppendLine();
            sb.AppendLine("Timed over the whole segment, reset pad to reset pad (the recorder's segment time). The lit line is timed twice: as a RUN (run speed throughout, the slope ridden, every drop a fall) and KEPT (the beat's speed carried by hops). The high line is timed EXECUTED (its speed kept, slopes slid, a slope landing keeps the fall's share). Turns and misses are not modelled.");
            sb.AppendLine();
            foreach (var h in r.hidden) sb.AppendLine("- " + HiddenLine(h));
            sb.AppendLine();
        }
        if (r.speed.Count > 0)
        {
            sb.AppendLine("## Speed control: the snowball");
            sb.AppendLine();
            sb.AppendLine("Each segment ridden by a player who perfect-hops every flat with headroom (×" + LevelMetricsMath.BhopMultiplier(new LevelMetricsMath.Values(), 8f).ToString("0.00", Ci) + " a landing, fading past 18 m/s), slides every downhill, presses at every landing and keeps the wall jump's gain; capped at " + SnowballCap.ToString("0", Ci) + " m/s, the fastest the recordings show. Twice: FROM THE PAD (run speed at the segment's start) and CARRIED (with what the segment before handed on). At every gap the rider sheds to the most speed the move still lands at, so what it hands on is what a player who never falls can build. A ground turn of 60° or more, a settle corner, a kink pad, a step and a climb shed to run speed; a roof under 1.95 m is a slide (−" + new LevelMetricsMath.Values().slideFriction.ToString("0.0", Ci) + " m/s a metre); a roof under " + HopRoom.ToString("0.0", Ci) + " m stops the hops but keeps the speed (a hop that bonks under a low roof is not modelled). Every gap of the line, and every skip between two floors of it, is tried at the speed the rider arrives with: a plain jump that lands clean at " + TrivialOver.ToString("0.0", Ci) + "× the sized speed or more is TRIVIAL. The gap's own move is tried at every speed up from the sized one until a lintel is hit or no floor is under the landing: 'lands up to'. A straight is " + StraightMin.ToString("0", Ci) + " m or more of flat with headroom.");
            sb.AppendLine();
            foreach (var s in r.speed)
            {
                sb.AppendLine("**" + s.segment + "** (sized " + s.sized.ToString("0", Ci) + " m/s): arrives at the first gap with " + s.fromPad.ToString("0.0", Ci) + " m/s from the pad, " + s.carried.ToString("0.0", Ci) + " carried (" + s.carriedIn.ToString("0.0", Ci) + " in); peak " + s.peak.ToString("0.0", Ci) + ", hands on " + s.exit.ToString("0.0", Ci));
                foreach (var l in s.gaps) sb.AppendLine("- " + l);
                foreach (var l in s.straights) sb.AppendLine("- " + l);
                sb.AppendLine();
            }
        }
        if (r.lifts.Count > 0)
        {
            var vv = new LevelMetricsMath.Values();
            var lj = LevelMetricsMath.WallLift(vv, vv.jumpImpulse, 1000f);
            sb.AppendLine("## Walls that lift");
            sb.AppendLine();
            sb.AppendLine("Every wall a run can be entered on from a floor at its foot, taken the worst way: jumped at (" + vv.jumpImpulse.ToString("0", Ci) + " m/s up at the touch). The run keeps that rise and adds the entry pop under almost no gravity, the wall jump adds half of what is left to its own " + vv.wallRunJumpUpImpulse.ToString("0", Ci) + ", the touch refunds the double jump: with no top and no ceiling the chain tops out " + lj.afterDouble.ToString("0.0", Ci) + " m over the floor (" + lj.afterJump.ToString("0.0", Ci) + " without the pip). A wall's top ends the run early; a ceiling over the run stops the rise. REACHES lists every surface at least a body above the floor and no higher than the lift within 32 m of the wall. The full table by entry and wall height is section 3b of Docs/Design/level-metrics.md.");
            sb.AppendLine();
            foreach (var l in r.lifts)
                sb.AppendLine("- **" + l.segment + "**: '" + l.wall + "' " + l.length.ToString("0", Ci) + " m long, " + l.wallHeight.ToString("0.0", Ci) + " m tall beside '" + l.from + "' at " + Fmt(l.at) + (float.IsInfinity(l.cornice) ? "" : ", a ceiling " + l.cornice.ToString("0.0", Ci) + " m over the run") + ": the run rises " + l.rise.ToString("0.0", Ci) + " m" + (l.toTop ? " to the top" : "") + ", the wall jump tops out " + l.afterJump.ToString("0.0", Ci) + " m over the floor, the pip " + l.afterDouble.ToString("0.0", Ci) + " m. " + (l.reaches.Count > 0 ? "REACHES " + string.Join(", ", l.reaches) + "." : "Reaches nothing above the floor."));
            sb.AppendLine();
        }
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
