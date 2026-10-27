using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// THE WELD (2026-10-06): a level is thousands of primitives, each its own draw call and its own shadow caster four
/// times over (four cascades), and that is where the frames went. After a level is built, every STATIC primitive (a
/// plain cube, cylinder or sphere renderer with no behaviour on it or over it, opaque, on the default layer) is welded
/// into one mesh per material per 40 m cell (since 2026-10-09 a level has no rooms, course-grammar), with the same
/// material, the same shadow mode and a probe anchor at the cell's centre on the course's floor there, so the picture
/// is the same and the draw count falls by a hundred times. Colliders stay on the originals; only their renderers go dark.
/// What moves is left alone: anything carrying a behaviour or under one, anything named for a thing that moves, and
/// anything a builder offers up through IWeldDynamic (the plant's animator, the weapon cases, the ward's seal, the
/// ending's set). And a safety net: the weld remembers every source's pose and material and sweeps
/// them a slice a frame; a source that moved, swapped material, was switched back on, hidden or destroyed is RELEASED
/// (its own renderer returns) and its group is welded again without it, so nothing missed is frozen for more than a
/// moment. Nothing here changes what is drawn, only how many times the GPU is asked.
/// </summary>
public interface IWeldDynamic { void DynamicParts(List<Transform> into); }

/// A thing that moves: put this on it and the weld leaves it (and everything under it) alone
public class DynamicMarker : MonoBehaviour { }

public class LevelWeld : MonoBehaviour
{
    public static LevelWeld Instance { get; private set; }
    /// One line for the F3 readout, set once at the weld
    public string Live { get; private set; } = "";

    const float Cell = 40f;
    const int MaxSourceVerts = 2000;
    static readonly string[] movingWords = { "Beacon", "Gate", "Seal", "Case", "Pane", "Glass", "Kit", "Lift", "Cage", "Door", "Pod", "Lever", "Stack", "Override", "Wreck", "Headframe", "Winch", "Cart", "Pot", "Jaw", "Sweep", "Arc", "Pad", "Tram", "Airship", "Aircraft", "Patrol" };
    static readonly string[] neverWords = { "FarGround", "Ground", "Ash", "Sky", "Cloud", "Star", "Weld", "HardLine" };

    class Source { public Transform t; public MeshRenderer r; public Mesh mesh; public Matrix4x4 m; public Material mat; public int group = -1; public bool released; }
    class Group { public Material mat; public ShadowCastingMode shadows; public bool receive; public Transform anchor; public readonly List<int> src = new List<int>(); public GameObject go; public bool dirty; }

    readonly List<Source> sources = new List<Source>();
    readonly List<Group> groups = new List<Group>();
    readonly Dictionary<(Material, int, int, int, ShadowCastingMode, bool), Group> index = new Dictionary<(Material, int, int, int, ShadowCastingMode, bool), Group>();
    readonly List<Transform> anchors = new List<Transform>();
    int cursor, welded, meshCount, released;
    long triangles;

    /// Run once, after everything in the level exists (the end of the bootstrap). Groups by 40 m cell everywhere;
    /// `rooms` is kept for old callers and ignored (there are no rooms to group by)
    public static void Run(bool rooms = true)
    {
        if (Instance != null) Destroy(Instance.gameObject);
        var go = new GameObject("Weld");
        Instance = go.AddComponent<LevelWeld>();
        Instance.Collect();
        Instance.Build();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // ------------------------------------------------------------ what is static

    static bool Has(string name, string[] words) { foreach (var w in words) if (name.Contains(w)) return true; return false; }

    void Collect()
    {
        var dynamicRoots = new HashSet<Transform>();
        var parts = new List<Transform>();
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if (mb is IWeldDynamic w)
            {
                parts.Clear();
                try { w.DynamicParts(parts); } catch (System.Exception) { }
                foreach (var t in parts) if (t != null) dynamicRoots.Add(t);
            }
        var layout = LevelSections.Last;   // the section spans, for the anchors' floor heights (empty on a hand-built level: anchors sit at the parts)
        var comps = new List<Component>();
        foreach (var mr in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (!mr.enabled || !mr.gameObject.activeInHierarchy || mr.gameObject.layer != 0) continue;
            var mf = mr.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null || mf.sharedMesh.vertexCount > MaxSourceVerts) continue;
            var mats = mr.sharedMaterials;
            if (mats.Length != 1 || mats[0] == null || mats[0].renderQueue >= 2500) continue;   // the transparent keep their own sort order
            // Nothing but a transform, a mesh and a collider on it
            mr.GetComponents(comps);
            bool plain = true;
            foreach (var c in comps) if (!(c is Transform || c is MeshFilter || c is MeshRenderer || c is Collider)) { plain = false; break; }
            if (!plain) continue;
            // Nothing that moves it, over it
            bool still = true;
            for (var t = mr.transform; t != null; t = t.parent)
            {
                if (dynamicRoots.Contains(t) || Has(t.name, movingWords) || Has(t.name, neverWords)) { still = false; break; }
                if (t != mr.transform && t.GetComponent<MonoBehaviour>() != null) { still = false; break; }
            }
            if (!still) continue;
            // Its group: by 40 m cell; the probe anchor at the cell's centre, at head height over the course's floor
            // there when a section span says where that is (so the reflections stay the space's own), else at the part
            Vector3 p = mr.transform.position;
            int cx = Mathf.FloorToInt(p.x / Cell), cz = Mathf.FloorToInt(p.z / Cell);
            var key = (mats[0], 0, cx, cz, mr.shadowCastingMode, mr.receiveShadows);
            if (!index.TryGetValue(key, out var g))
            {
                g = new Group { mat = mats[0], shadows = mr.shadowCastingMode, receive = mr.receiveShadows };
                float zc = (cz + 0.5f) * Cell, xc = (cx + 0.5f) * Cell, yc = p.y;
                if (layout != null)
                    foreach (var rs in layout.rooms)
                        if (zc >= rs.z0 - 0.5f && zc < rs.z1 + 0.5f) { yc = Mathf.Lerp(rs.entryY, rs.exitY, Mathf.InverseLerp(rs.z0, rs.z1, zc)) + 4f; xc = Mathf.Clamp(xc, -rs.halfW, rs.halfW); break; }
                g.anchor = Anchor(new Vector3(xc, yc, zc));
                index[key] = g;
                groups.Add(g);
            }
            g.src.Add(sources.Count);
            sources.Add(new Source { t = mr.transform, r = mr, mesh = mf.sharedMesh, m = mr.transform.localToWorldMatrix, mat = mats[0], group = groups.IndexOf(g) });
        }
    }

    Transform Anchor(Vector3 at)
    {
        foreach (var a in anchors) if ((a.position - at).sqrMagnitude < 0.01f) return a;
        var go = new GameObject("WeldAnchor");
        go.transform.SetParent(transform, false);
        go.transform.position = at;
        anchors.Add(go.transform);
        return go.transform;
    }

    // ------------------------------------------------------------ the weld

    void Build()
    {
        welded = 0; meshCount = 0; triangles = 0;
        for (int gi = 0; gi < groups.Count; gi++) Weld(gi, true);
        Live = "WELD  " + welded + " parts → " + meshCount + " meshes";
        Debug.Log("WELD  " + welded + " static parts welded into " + meshCount + " meshes (" + (triangles / 1000) + " k tris); " + (sources.Count - welded) + " left alone");
    }

    /// Weld one group from its live sources. First time, a group of one is left as it is (nothing to gain)
    void Weld(int gi, bool first)
    {
        var g = groups[gi];
        var live = new List<int>();
        foreach (int i in g.src)
        {
            var s = sources[i];
            if (s.released || s.t == null || s.r == null) continue;
            live.Add(i);
        }
        if (g.go != null) { Destroy(g.go.GetComponent<MeshFilter>().sharedMesh); Destroy(g.go); g.go = null; }
        if (live.Count < (first ? 2 : 1))
        {
            foreach (int i in live) { sources[i].released = true; if (sources[i].r != null) sources[i].r.enabled = true; }
            return;
        }
        var ci = new CombineInstance[live.Count];
        for (int k = 0; k < live.Count; k++)
        {
            var s = sources[live[k]];
            ci[k].mesh = s.mesh;
            ci[k].subMeshIndex = 0;
            ci[k].transform = s.t.localToWorldMatrix;
        }
        var mesh = new Mesh { name = "Weld " + g.mat.name, indexFormat = IndexFormat.UInt32 };
        mesh.CombineMeshes(ci, true, true, false);
        mesh.RecalculateBounds();
        var go = new GameObject("Weld " + g.mat.name);
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = g.mat;
        r.shadowCastingMode = g.shadows;
        r.receiveShadows = g.receive;
        r.lightProbeUsage = LightProbeUsage.Off;
        if (g.anchor != null) r.probeAnchor = g.anchor;
        g.go = go;
        foreach (int i in live)
        {
            var s = sources[i];
            s.r.enabled = false;
            s.m = s.t.localToWorldMatrix;
            s.mat = s.r.sharedMaterial;
            if (first) welded++;
        }
        if (first) { meshCount++; triangles += mesh.triangles.Length / 3; }
    }

    // ------------------------------------------------------------ the net

    static bool Moved(Matrix4x4 a, Matrix4x4 b)
    {
        for (int c = 0; c < 4; c++) if ((a.GetColumn(c) - b.GetColumn(c)).sqrMagnitude > 1e-8f) return true;
        return false;
    }

    void Update()
    {
        if (sources.Count == 0) return;
        int per = Mathf.Max(1, sources.Count / 45);   // the whole list every three quarters of a second at sixty frames
        for (int k = 0; k < per; k++)
        {
            cursor = (cursor + 1) % sources.Count;
            var s = sources[cursor];
            if (s.released || s.group < 0) continue;
            bool gone = s.t == null || s.r == null;
            if (gone || !s.t.gameObject.activeInHierarchy || s.r.enabled || s.r.sharedMaterial != s.mat || Moved(s.t.localToWorldMatrix, s.m))
            {
                s.released = true;
                released++;
                groups[s.group].dirty = true;
                if (!gone) s.r.enabled = true;   // its own renderer is back, wherever it went
            }
        }
    }

    void LateUpdate()
    {
        for (int gi = 0; gi < groups.Count; gi++)
            if (groups[gi].dirty) { groups[gi].dirty = false; Weld(gi, false); }
    }
}
