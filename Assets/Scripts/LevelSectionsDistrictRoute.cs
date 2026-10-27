using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE DISTRICT, its routes and its lint. The ROUTE is the authored main way through (junction to junction, the
/// shortest by streets unless the builder names waypoints): it gives every junction its FORWARD (the respawn's
/// direction, the HUD's way on) and nothing else — it is one of the ways, never the only one. The LINT walks the
/// graph of junctions, street segments and entrances (an establishment joins every entrance it has to every other:
/// its interior lines) and counts the distinct routes from the start to the finish: a plan ships with three at least.
/// </summary>
public static partial class CityDistrict
{
    struct Edge { public int to; public bool tech; }

    class Graph
    {
        public List<Edge>[] adj;
        public string[] names;
        public int J, E, Start, Finish;
        public void Link(int a, int b, bool tech = false, bool both = true)
        {
            adj[a].Add(new Edge { to = b, tech = tech });
            if (both) adj[b].Add(new Edge { to = a, tech = tech });
        }
    }

    /// The plan as a graph: junctions, then entrances, then START (the lane in, south) and FINISH (the lane out, north)
    static Graph BuildGraph(DistrictPlan plan, bool streetsOnly)
    {
        var g = new Graph { J = plan.junctions.Count, E = streetsOnly ? 0 : plan.entrances.Count };
        int n = g.J + g.E + 2;
        g.Start = g.J + g.E; g.Finish = g.Start + 1;
        g.adj = new List<Edge>[n];
        g.names = new string[n];
        for (int i = 0; i < n; i++) g.adj[i] = new List<Edge>();
        for (int i = 0; i < g.J; i++) g.names[i] = plan.junctions[i].name;
        g.names[g.Start] = "START"; g.names[g.Finish] = "FINISH";
        int Jx(int gx, int gz) => plan.junctions.IndexOf(plan.JunctionAt(gx, gz));
        // The streets and alleys between neighbouring junctions
        for (int gx = 0; gx <= plan.blocksX; gx++)
            for (int gz = 0; gz <= plan.blocksZ; gz++)
            {
                if (gz < plan.blocksZ) g.Link(Jx(gx, gz), Jx(gx, gz + 1));
                if (gx < plan.blocksX) g.Link(Jx(gx, gz), Jx(gx + 1, gz));
            }
        // The lane in and out: the two junctions nearest x = 0 on the south and the north edge
        foreach (bool south in new[] { true, false })
        {
            int gz = south ? 0 : plan.blocksZ, node = south ? g.Start : g.Finish;
            int best = 0; float bd = float.MaxValue;
            for (int gx = 0; gx <= plan.blocksX; gx++) { float d = Mathf.Abs(plan.GapMid(true, gx)); if (d < bd) { bd = d; best = gx; } }
            g.Link(node, Jx(best, gz));
            int other = plan.GapMid(true, best) < 0f ? best + 1 : best - 1;
            if (other >= 0 && other <= plan.blocksX) g.Link(node, Jx(other, gz));
        }
        if (streetsOnly) return g;
        // Every entrance: onto the street segment it faces (both junctions of it; up to it is tech when it says so),
        // and to every other entrance of its block (the interior lines)
        for (int i = 0; i < plan.entrances.Count; i++)
        {
            var e = plan.entrances[i];
            int node = g.J + i;
            g.names[node] = e.ToString();
            int bx = e.block.bx, bz = e.block.bz;
            int a, b;
            switch (e.side)
            {
                case Face.West: a = Jx(bx, bz); b = Jx(bx, bz + 1); break;
                case Face.East: a = Jx(bx + 1, bz); b = Jx(bx + 1, bz + 1); break;
                case Face.South: a = Jx(bx, bz); b = Jx(bx + 1, bz); break;
                default: a = Jx(bx, bz + 1); b = Jx(bx + 1, bz + 1); break;
            }
            if (e.fromStreet) { g.Link(a, node, e.tech, false); g.Link(b, node, e.tech, false); }
            g.Link(node, a, false, false); g.Link(node, b, false, false);
            for (int k = 0; k < i; k++)
                if (plan.entrances[k].block == e.block) g.Link(node, g.J + k);
        }
        return g;
    }

    // ------------------------------------------------------------ the route

    /// The main route from junction `from` to junction `to` (grid coordinates gx, gz): the shortest by streets, alleys
    /// costing three streets. Sets every junction's forward: along the route on it, toward the route off it
    public static List<Junction> Route(DistrictPlan plan, Vector2Int from, Vector2Int to) => Route(plan, new[] { from, to });

    /// The main route through the named junctions in order (the first on the south edge, the last on the north)
    public static List<Junction> Route(DistrictPlan plan, params Vector2Int[] waypoints)
    {
        if (plan.junctions.Count == 0) Layout(plan, plan.z0, plan.floorY);
        var g = BuildGraph(plan, true);
        plan.route.Clear();
        for (int w = 0; w + 1 < waypoints.Length; w++)
        {
            var leg = Shortest(plan, g, Index(plan, waypoints[w]), Index(plan, waypoints[w + 1]));
            foreach (int node in leg)
                if (plan.route.Count == 0 || plan.route[plan.route.Count - 1] != plan.junctions[node]) plan.route.Add(plan.junctions[node]);
        }
        for (int i = 0; i < plan.route.Count; i++)
        {
            var j = plan.route[i];
            j.routeIndex = i;
            Vector3 next = i + 1 < plan.route.Count ? plan.route[i + 1].at : new Vector3(0f, plan.floorY, plan.Z1 + 20f);
            j.forward = Flat(next - j.at);
        }
        // Off the route: toward the nearest route junction by street distance, breadth first from the route
        var dist = new int[plan.junctions.Count];
        var via = new int[plan.junctions.Count];
        for (int i = 0; i < dist.Length; i++) { dist[i] = int.MaxValue; via[i] = -1; }
        var q = new Queue<int>();
        foreach (var j in plan.route) { int i = plan.junctions.IndexOf(j); dist[i] = 0; q.Enqueue(i); }
        while (q.Count > 0)
        {
            int i = q.Dequeue();
            foreach (var e in g.adj[i])
                if (e.to < g.J && dist[e.to] == int.MaxValue) { dist[e.to] = dist[i] + 1; via[e.to] = i; q.Enqueue(e.to); }
        }
        for (int i = 0; i < plan.junctions.Count; i++)
            if (plan.junctions[i].routeIndex < 0 && via[i] >= 0) plan.junctions[i].forward = Flat(plan.junctions[via[i]].at - plan.junctions[i].at);
        return plan.route;
    }

    static int Index(DistrictPlan plan, Vector2Int at) => plan.junctions.IndexOf(plan.JunctionAt(Mathf.Clamp(at.x, 0, plan.blocksX), Mathf.Clamp(at.y, 0, plan.blocksZ)));
    static Vector3 Flat(Vector3 d) { d.y = 0f; return d.sqrMagnitude > 1e-4f ? d.normalized : Vector3.forward; }

    /// Dijkstra over the junctions: a street step costs one, an alley step three
    static List<int> Shortest(DistrictPlan plan, Graph g, int from, int to)
    {
        int n = g.J;
        var cost = new float[n]; var prev = new int[n]; var done = new bool[n];
        for (int i = 0; i < n; i++) { cost[i] = float.MaxValue; prev[i] = -1; }
        cost[from] = 0f;
        for (int k = 0; k < n; k++)
        {
            int u = -1;
            for (int i = 0; i < n; i++) if (!done[i] && cost[i] < float.MaxValue && (u < 0 || cost[i] < cost[u])) u = i;
            if (u < 0 || u == to) break;
            done[u] = true;
            foreach (var e in g.adj[u])
            {
                if (e.to >= n) continue;
                var a = plan.junctions[u]; var b = plan.junctions[e.to];
                bool alley = a.gx == b.gx ? plan.IsAlley(true, a.gx) : plan.IsAlley(false, a.gz);
                float c = cost[u] + (alley ? 3f : 1f);
                if (c < cost[e.to]) { cost[e.to] = c; prev[e.to] = u; }
            }
        }
        var path = new List<int>();
        for (int v = to; v >= 0; v = prev[v]) { path.Add(v); if (v == from) break; }
        path.Reverse();
        return path;
    }

    /// The route's direction at the junction nearest `pos`
    public static Vector3 ForwardAt(DistrictPlan plan, Vector3 pos)
    {
        Junction best = null; float bd = float.MaxValue;
        foreach (var j in plan.junctions) { float d = (j.at - pos).sqrMagnitude; if (d < bd) { bd = d; best = j; } }
        return best != null ? best.forward : Vector3.forward;
    }

    // ------------------------------------------------------------ the lint

    const int PathCap = 400, StepCap = 400000;

    /// Walks the plan's graph: every entrance and junction reachable from the start, three distinct routes at least
    /// to the finish (counted as simple paths over junctions and entrances, capped), every establishment with two
    /// entrances or more, the lanes' widths. Returns the issues (empty is clean) and logs one summary line
    public static List<string> Lint(DistrictPlan plan)
    {
        var report = new List<string>();
        if (plan.junctions.Count == 0) { report.Add("district: not laid out (Build first)"); return report; }
        var g = BuildGraph(plan, false);
        var seen = new bool[g.adj.Length];
        var stack = new Stack<int>();
        stack.Push(g.Start); seen[g.Start] = true;
        while (stack.Count > 0) { int u = stack.Pop(); foreach (var e in g.adj[u]) if (!seen[e.to]) { seen[e.to] = true; stack.Push(e.to); } }
        for (int i = 0; i < g.adj.Length; i++) if (!seen[i]) report.Add("district: " + g.names[i] + " is unreachable from the start");
        int all = CountPaths(g, false), main = CountPaths(g, true);
        if (all < 3) report.Add($"district: only {all} distinct route(s) from the start to the finish — a plan ships with three at least");
        if (main < 1) report.Add("district: no route to the finish without tech — the main line must run at eight metres a second");
        foreach (var b in plan.blocks)
        {
            if (b.IsOpenGround) continue;
            if (b.entrances.Count < 2) report.Add($"district: {b.Name} has {b.entrances.Count} entrance(s) — every establishment is a pass-through (two to four)");
            int heights = 0; var hs = new HashSet<int>();
            foreach (var e in b.entrances) if (hs.Add(Mathf.RoundToInt(e.height / 2f))) heights++;
            if (b.entrances.Count >= 2 && heights < 2) report.Add($"district: {b.Name}'s entrances are all at one height — a door, a window, a roof");
        }
        for (int gx = 0; gx <= plan.blocksX; gx++) if (plan.GapW(true, gx) < 3f) report.Add($"district: gap {gx} along x is {plan.GapW(true, gx):0.#} m — an alley is three metres at least");
        for (int gz = 0; gz <= plan.blocksZ; gz++) if (plan.GapW(false, gz) < 3f) report.Add($"district: gap {gz} along z is {plan.GapW(false, gz):0.#} m — an alley is three metres at least");
        if (plan.route.Count < 2) report.Add("district: no route authored (CityDistrict.Route) — the respawns would all face north");
        int alleys = 0; foreach (bool a in plan.alleyX) if (a) alleys++; foreach (bool a in plan.alleyZ) if (a) alleys++;
        Debug.Log($"CityDistrict.Lint: {plan.blocksX}×{plan.blocksZ} blocks, {plan.junctions.Count} junctions, {alleys} alleys, {plan.entrances.Count} entrances, routes {all}{(all >= PathCap ? "+" : "")} ({main}{(main >= PathCap ? "+" : "")} without tech)" + (report.Count == 0 ? "  ·  clean" : $"  ·  {report.Count} issue(s)"));
        return report;
    }

    /// Simple paths from START to FINISH, capped; `mainOnly` walks no tech edge (the eight-metres-a-second routes)
    static int CountPaths(Graph g, bool mainOnly)
    {
        var onPath = new bool[g.adj.Length];
        int paths = 0, steps = 0;
        void Walk(int u)
        {
            if (paths >= PathCap || ++steps > StepCap) return;
            if (u == g.Finish) { paths++; return; }
            onPath[u] = true;
            foreach (var e in g.adj[u])
            {
                if (onPath[e.to] || (mainOnly && e.tech)) continue;
                Walk(e.to);
                if (paths >= PathCap) break;
            }
            onPath[u] = false;
        }
        Walk(g.Start);
        return paths;
    }

    /// Every district built this level, for LevelLint
    public static void LintAll(List<string> report)
    {
        foreach (var plan in Built) report.AddRange(Lint(plan));
    }
}
