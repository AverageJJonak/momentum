using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WHAT DRIVES, and where (CityTraffic): the six kinds as welded boxes, and the lane layouts a sky level asks for —
/// the fabric's avenue grid either side of the route's corridor, the corridor's own streets found by probing, the
/// tram rails baked into the level.
/// </summary>
public partial class CityTraffic
{
    sealed class Boxes
    {
        readonly List<Vector3> c = new List<Vector3>(), s = new List<Vector3>();
        public Boxes Add(float x, float y, float z, float sx, float sy, float sz) { c.Add(new Vector3(x, y, z)); s.Add(new Vector3(sx, sy, sz)); return this; }
        /// The same box mirrored in x (a lamp each side, a wheel each side)
        public Boxes Pair(float x, float y, float z, float sx, float sy, float sz) => Add(x, y, z, sx, sy, sz).Add(-x, y, z, sx, sy, sz);
        public Mesh Mesh(string name)
        {
            if (c.Count == 0) return null;
            var cube = LevelKit.PrimMesh(PrimitiveType.Cube);
            var ci = new CombineInstance[c.Count];
            for (int i = 0; i < c.Count; i++) ci[i] = new CombineInstance { mesh = cube, transform = Matrix4x4.TRS(c[i], Quaternion.identity, s[i]) };
            var m = new Mesh { name = name };
            m.CombineMeshes(ci, true, true);
            m.RecalculateBounds();
            return m;
        }
    }

    static Material Paint(Shader shader, Color c, float smooth)
    {
        var m = new Material(shader);
        m.SetColor("_BaseColor", c);
        if (smooth > 0f) m.SetFloat("_Smoothness", smooth);
        m.enableInstancing = true;
        return m;
    }

    /// Every kind's parts, once: the body in its paint, cream lamps, red lamps, amber windows, the pool of light
    void BuildKinds()
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        Material cream = Paint(unlit, new Color(2.4f, 2.1f, 1.5f), 0f), red = Paint(unlit, new Color(2.2f, 0.18f, 0.1f), 0f), amber = Paint(unlit, new Color(1.9f, 1.3f, 0.7f), 0f);
        Material pool = CityKit.HazeSheet(new Color(1.3f, 1.15f, 0.85f, 0.13f));
        pool.enableInstancing = true;
        Material lacquer = Paint(lit, new Color(0.05f, 0.05f, 0.06f), 0.75f), mustard = Paint(lit, new Color(0.7f, 0.55f, 0.2f), 0.6f);
        Material oxblood = Paint(lit, new Color(0.32f, 0.08f, 0.08f), 0.5f), charcoal = Paint(lit, new Color(0.14f, 0.13f, 0.12f), 0.3f), ink = Paint(lit, new Color(0.09f, 0.09f, 0.1f), 0.3f);
        void Set(Kind k, Material paint, Boxes body, Boxes lamps, Boxes tails, Boxes glass, Boxes spill)
        {
            int i = (int)k;
            meshes[i, 0] = body?.Mesh(k + "Body"); mats[i, 0] = paint;
            meshes[i, 1] = lamps?.Mesh(k + "Lamps"); mats[i, 1] = cream;
            meshes[i, 2] = tails?.Mesh(k + "Tails"); mats[i, 2] = red;
            meshes[i, 3] = glass?.Mesh(k + "Glass"); mats[i, 3] = amber;
            meshes[i, 4] = spill?.Mesh(k + "Pool"); mats[i, 4] = pool;
        }
        Boxes Car() => new Boxes().Add(0f, 0.62f, 0.3f, 1.8f, 0.62f, 4.6f).Add(0f, 1.22f, -0.5f, 1.62f, 0.6f, 2.1f).Add(0f, 0.9f, 1.9f, 1.7f, 0.2f, 1.2f)
            .Pair(0.85f, 0.3f, 1.5f, 0.22f, 0.6f, 0.6f).Pair(0.85f, 0.3f, -1.5f, 0.22f, 0.6f, 0.6f).Pair(0.98f, 0.36f, 0f, 0.18f, 0.08f, 2.8f);
        Set(Kind.Sedan, lacquer, Car(), new Boxes().Pair(0.62f, 0.82f, 2.33f, 0.26f, 0.24f, 0.1f), new Boxes().Pair(0.62f, 0.72f, -2.32f, 0.18f, 0.1f, 0.06f), null,
            new Boxes().Add(0f, 0.015f, 5.6f, 2.8f, 0.01f, 6.5f));
        Set(Kind.Taxi, mustard, Car(), new Boxes().Pair(0.62f, 0.82f, 2.33f, 0.26f, 0.24f, 0.1f).Add(0f, 1.62f, -0.5f, 0.6f, 0.2f, 0.3f), new Boxes().Pair(0.62f, 0.72f, -2.32f, 0.18f, 0.1f, 0.06f), null,
            new Boxes().Add(0f, 0.015f, 5.6f, 2.8f, 0.01f, 6.5f));
        Set(Kind.Bus, oxblood, new Boxes().Add(0f, 1.45f, 0f, 2.4f, 2.5f, 9.6f).Pair(1.1f, 0.4f, 3f, 0.3f, 0.8f, 0.8f).Pair(1.1f, 0.4f, 0f, 0.3f, 0.8f, 0.8f).Pair(1.1f, 0.4f, -3.2f, 0.3f, 0.8f, 0.8f),
            new Boxes().Pair(0.85f, 0.9f, 4.82f, 0.3f, 0.26f, 0.1f), new Boxes().Pair(0.9f, 0.9f, -4.82f, 0.2f, 0.12f, 0.06f),
            new Boxes().Pair(1.21f, 1.95f, -0.3f, 0.04f, 0.7f, 7.6f).Add(0f, 2.0f, 4.81f, 1.9f, 0.8f, 0.04f), new Boxes().Add(0f, 0.015f, 8.5f, 3.2f, 0.01f, 7.5f));
        Set(Kind.Truck, charcoal, new Boxes().Add(0f, 1.5f, 2.5f, 2.2f, 2.0f, 2.2f).Add(0f, 0.95f, 4.0f, 2.0f, 0.9f, 1.0f).Add(0f, 1.35f, -1.2f, 2.3f, 1.7f, 5.2f)
            .Pair(1.05f, 0.4f, 3.6f, 0.3f, 0.8f, 0.8f).Pair(1.05f, 0.4f, 0.2f, 0.3f, 0.8f, 0.8f).Pair(1.05f, 0.4f, -2.6f, 0.3f, 0.8f, 0.8f),
            new Boxes().Pair(0.75f, 1.0f, 4.52f, 0.26f, 0.24f, 0.1f), new Boxes().Pair(0.9f, 0.8f, -3.82f, 0.2f, 0.12f, 0.06f), null, new Boxes().Add(0f, 0.015f, 8.0f, 3.0f, 0.01f, 7.0f));
        Set(Kind.Tram, oxblood, new Boxes().Add(0f, 1.75f, 0f, 2.5f, 2.9f, 13f).Add(0f, 3.3f, 0f, 2.3f, 0.2f, 12.6f).Add(0f, 0.35f, 4f, 2.0f, 0.7f, 2.0f).Add(0f, 0.35f, -4f, 2.0f, 0.7f, 2.0f)
            .Add(0f, 4.2f, 0f, 0.08f, 1.6f, 0.08f).Add(0f, 5.0f, 0f, 0.12f, 0.06f, 1.2f),
            new Boxes().Add(0f, 1.2f, 6.52f, 0.3f, 0.3f, 0.06f).Add(0f, 1.2f, -6.52f, 0.3f, 0.3f, 0.06f),
            new Boxes().Pair(0.9f, 1.0f, 6.52f, 0.14f, 0.12f, 0.05f).Pair(0.9f, 1.0f, -6.52f, 0.14f, 0.12f, 0.05f),
            new Boxes().Pair(1.26f, 2.3f, 0f, 0.04f, 0.8f, 11.2f).Add(0f, 2.3f, 6.51f, 1.8f, 0.8f, 0.04f).Add(0f, 2.3f, -6.51f, 1.8f, 0.8f, 0.04f), null);
        Set(Kind.Walker, ink, new Boxes().Add(0f, 0.4f, 0f, 0.3f, 0.8f, 0.22f).Add(0f, 1.15f, 0f, 0.46f, 0.7f, 0.3f).Add(0f, 1.68f, 0f, 0.2f, 0.22f, 0.2f).Add(0f, 1.81f, 0f, 0.34f, 0.04f, 0.34f).Add(0f, 1.9f, 0f, 0.22f, 0.16f, 0.22f),
            null, null, null, null);
    }

    // ------------------------------------------------------------ the layouts

    /// THE FABRIC'S STREETS round a sky level: the plan's lots stand on a `cell` grid (a lot never reaches past
    /// ±(cell/2 − 15) of its cell's centre), so every line `cell/2 + i·cell` is a clear avenue; the corridor
    /// (|x| < `corridorHalf`, z in z0..z1) has none. Either side: a tall loop up the two nearest avenues, two short
    /// loops the other way round the nearest (both directions on it), a tram line up it, walkers round with the cars
    public void Fabric(float groundY, float cell, float corridorHalf, float z0, float z1, int seed, MeshBaker rails, Material railMat)
    {
        float half = cell * 0.5f;
        float ax = Mathf.Ceil(corridorHalf / cell) * cell + half;
        float zA = Mathf.Floor((z0 - 400f) / cell) * cell + half, zB = Mathf.Ceil((z1 + 400f) / cell) * cell + half;
        float zM = Mathf.Round((zA + zB) * 0.5f / cell) * cell + half;
        for (int side = -1; side <= 1; side += 2)
        {
            float x1 = side * ax, x2 = side * (ax + cell), x3 = side * (ax + 2f * cell);
            Loop(new[] { new Vector2(x1, zA), new Vector2(x1, zB), new Vector2(x3, zB), new Vector2(x3, zA) }, groundY, 11f, 1.4f, 3.5f, seed + side);
            Loop(new[] { new Vector2(x2, zA), new Vector2(x2, zM), new Vector2(x1, zM), new Vector2(x1, zA) }, groundY, 9.5f, 1.2f, 3f, seed + 7 * side);
            Loop(new[] { new Vector2(x2, zM), new Vector2(x2, zB), new Vector2(x1, zB), new Vector2(x1, zM) }, groundY, 12.5f, 1.2f, 3f, seed + 11 * side);
            Vector3 ta = new Vector3(x1, groundY, zA - cell), tb = new Vector3(x1, groundY, zB + cell);
            TramLine(ta, tb, 10f, 3, 9f, seed + 3 * side);
            if (rails != null) Rails(rails, railMat, ta, tb);
        }
    }

    /// THE CORRIDOR'S OWN STREETS: each candidate x is walked from z0 to z1 in ten-metre steps against what stands (the
    /// route's towers and the neighbours are solid), and every clear run of `minRun` or more becomes a two-way street
    /// `halfWidth` wide: the cars three metres off its middle, the walkers on pavements a metre and a half inside its edges
    public void Corridor(float groundY, float[] xs, float z0, float z1, float halfWidth, float minRun, int seed)
    {
        foreach (float x in xs)
        {
            float runStart = z0;
            bool open = false;
            for (float z = z0; z <= z1 + 10f; z += 10f)
            {
                bool clear = z <= z1 && !Physics.CheckBox(new Vector3(x, groundY + 2.6f, z), new Vector3(halfWidth, 2.4f, 5.2f), Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                if (clear && !open) { open = true; runStart = z; }
                else if (!clear && open)
                {
                    open = false;
                    float runEnd = z - 10f;
                    if (runEnd - runStart >= minRun)
                        Avenue(new Vector3(x, groundY, runStart + 4f), new Vector3(x, groundY, runEnd - 4f), 9f, 1.1f, 4f, seed++, halfWidth - 3.5f, halfWidth - 1.5f);
                }
            }
        }
    }

    /// Two tracks of two iron rails, baked (one box each, the length of the line)
    public static void Rails(MeshBaker b, Material m, Vector3 a, Vector3 c)
    {
        Vector3 d = c - a;
        float len = d.magnitude;
        if (len < 1f || b == null || m == null) return;
        Quaternion rot = Quaternion.LookRotation(d / len, Vector3.up);
        Vector3 right = Vector3.Cross(Vector3.up, d / len), mid = (a + c) * 0.5f + Vector3.up * 0.04f;
        foreach (float track in new[] { -1.4f, 1.4f })
            foreach (float rail in new[] { -0.72f, 0.72f })
                b.Add(m, mid + right * (track + rail), new Vector3(0.12f, 0.08f, len), rot);
    }
}
