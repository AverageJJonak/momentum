using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HAND EDITS, laid on a level just built (GameBootstrap, and the editor's working copy). One walk of the level finds
/// every piece the patch names (exact keys first, then the nearest piece on the same path for any key a builder has
/// since moved a little), then: reparenting, moves, paint, removals, additions, the spawn. Nothing here runs when a
/// level has no edits.
/// </summary>
public static partial class LevelEdits
{
    static readonly string[] Primitives = { "Cube", "Sphere", "Cylinder", "Capsule", "Plane", "Quad" };

    /// The saved (or, in the editor, unsaved) edits for `kind`, laid on `roots`; a line in the console when there were any
    public static Result ApplyFor(LevelKind kind, IList<GameObject> roots, MovementConfig config)
    {
        var res = Apply(Load(kind), roots, config);
        if (res.edited + res.added > 0 || res.spawnMoved)
            Debug.Log("LevelEdits: " + kind + " — " + res.edited + " hand edit(s), " + res.added + " addition(s)" + (res.spawnMoved ? ", the spawn moved" : "") + ".");
        foreach (var o in res.orphans)
            Debug.LogWarning("LevelEdits: " + kind + " — no piece found for " + o + " (a builder has changed since the edit was made; open the level in Momentum → Level Editor to see what is left).");
        return res;
    }

    public static Result Apply(Patch patch, IList<GameObject> roots, MovementConfig config)
    {
        var res = new Result();
        if (patch == null || patch.Empty) return res;

        // The keys the patch names, and what to look for while walking the level
        var wanted = new List<string>();
        foreach (var e in patch.edits)
        {
            wanted.Add(e.key);
            if (e.reparented && !string.IsNullOrEmpty(e.parent)) wanted.Add(e.parent);
        }
        foreach (var a in patch.adds)
        {
            if (!string.IsNullOrEmpty(a.source)) wanted.Add(a.source);
            if (!string.IsNullOrEmpty(a.parent)) wanted.Add(a.parent);
        }
        var cms = new HashSet<Vector3Int>();
        var paths = new HashSet<string>();
        var leaves = new HashSet<string>();
        foreach (var k in wanted)
        {
            if (!ParseKey(k, out var p, out var cm, out _)) continue;
            cms.Add(cm);
            paths.Add(p);
            leaves.Add(Leaf(p));
        }

        // One walk: the exact keys, and every piece on a wanted path for the near match
        var exact = new Dictionary<string, Transform>();
        var byPath = new Dictionary<string, List<Transform>>();
        var count = new Dictionary<string, int>();
        foreach (var r in roots)
            if (r != null) Index(r.transform, cms, paths, leaves, exact, byPath, count);

        // Resolve every key before anything moves: exact matches claimed first, so a near match never steals one
        var found = new Dictionary<string, Transform>();
        var claimed = new HashSet<Transform>();
        foreach (var k in wanted)
            if (!found.ContainsKey(k) && exact.TryGetValue(k, out var t) && t != null) { found[k] = t; claimed.Add(t); }
        foreach (var k in wanted)
        {
            if (found.ContainsKey(k) || !ParseKey(k, out var p, out var cm, out _) || !byPath.TryGetValue(p, out var list)) continue;
            Vector3 want = (Vector3)cm * 0.01f;
            Transform best = null;
            float bd = FallbackReach * FallbackReach;
            foreach (var c in list)
            {
                if (c == null || claimed.Contains(c)) continue;
                float d = (c.position - want).sqrMagnitude;
                if (d < bd) { bd = d; best = c; }
            }
            if (best != null) { found[k] = best; claimed.Add(best); }
        }
        Transform Get(string key) => !string.IsNullOrEmpty(key) && found.TryGetValue(key, out var t) ? t : null;

        // The level's materials by untiled name, for paint (made on the first use)
        Dictionary<string, Material> mats = null;
        Material MaterialNamed(string name)
        {
            if (mats == null)
            {
                mats = new Dictionary<string, Material>();
                foreach (var r in roots)
                {
                    if (r == null) continue;
                    foreach (var rend in r.GetComponentsInChildren<Renderer>(true))
                        foreach (var m in rend.sharedMaterials)
                        {
                            if (m == null) continue;
                            string n = LevelKit.UntiledOf(m).name;
                            if (!mats.ContainsKey(n)) mats[n] = m;
                        }
                }
            }
            return mats.TryGetValue(name, out var mat) ? mat : null;
        }
        void Paint(Transform t, string name)
        {
            var rend = t.GetComponent<Renderer>();
            if (rend == null || string.IsNullOrEmpty(name)) return;
            var mat = MaterialNamed(name);
            if (mat != null) rend.sharedMaterial = mat;
            else res.orphans.Add("the material " + name);
        }

        // 1. Moves under another piece first, so the local transforms below are in the new parent's space
        foreach (var e in patch.edits)
        {
            if (!e.reparented) continue;
            var t = Get(e.key);
            if (t == null) continue;
            Transform parent = Get(e.parent);
            if (!string.IsNullOrEmpty(e.parent) && parent == null) { res.orphans.Add(e.parent); continue; }
            t.SetParent(parent, false);
        }

        // 2. Moves, paint, removals
        foreach (var e in patch.edits)
        {
            var t = Get(e.key);
            if (t == null) { res.orphans.Add(e.key); continue; }
            if (!string.IsNullOrEmpty(e.material)) Paint(t, e.material);
            if (e.moved)
            {
                t.localPosition = e.pos;
                t.localRotation = e.rot;
                t.localScale = e.scale;
                Retile(t);
                var unit = t.GetComponent<Enemy>();
                if (unit != null) unit.Rehome();
            }
            if (e.removed) Remove(t);
            res.edits.Add(new KeyValuePair<Transform, Edit>(t, e));
            res.edited++;
        }

        // 3. Additions: copies of pieces, units, plain blocks
        foreach (var a in patch.adds)
        {
            Transform parent = Get(a.parent);
            if (!string.IsNullOrEmpty(a.parent) && parent == null) { res.orphans.Add(a.parent); continue; }
            GameObject go;
            if (!string.IsNullOrEmpty(a.source))
            {
                var src = Get(a.source);
                if (src == null) { res.orphans.Add(a.source); continue; }
                var unit = src.GetComponent<Enemy>();
                if (unit != null)
                {
                    // A unit is made by its factory (a copy of a built one would miss what the factory registers)
                    if (unit.kind == EnemyKind.Boss) continue;
                    Vector3 feet = parent != null ? parent.TransformPoint(a.pos) : a.pos;
                    var made = Enemy.Spawn(unit.kind, feet, config);
                    if (made == null) continue;
                    made.transform.rotation = parent != null ? parent.rotation * a.rot : a.rot;
                    if (!a.active) made.gameObject.SetActive(false);
                    res.adds.Add(new KeyValuePair<GameObject, Add>(made.gameObject, a));
                    res.added++;
                    continue;
                }
                go = Object.Instantiate(src.gameObject, parent, false);
            }
            else
            {
                int i = System.Array.IndexOf(Primitives, a.primitive);
                if (i < 0) continue;
                go = GameObject.CreatePrimitive((PrimitiveType)System.Enum.Parse(typeof(PrimitiveType), Primitives[i]));
                go.transform.SetParent(parent, false);
                go.GetComponent<Renderer>().sharedMaterial = LevelKit.Floor;
            }
            if (!string.IsNullOrEmpty(a.name)) go.name = a.name;
            var tr = go.transform;
            tr.localPosition = a.pos;
            tr.localRotation = a.rot;
            tr.localScale = a.scale;
            if (go.activeSelf != a.active) go.SetActive(a.active);
            if (!string.IsNullOrEmpty(a.material)) Paint(tr, a.material);
            Retile(tr);
            res.adds.Add(new KeyValuePair<GameObject, Add>(go, a));
            res.added++;
        }

        if (patch.spawnMoved) { res.spawnMoved = true; res.spawn = patch.spawn; }
        return res;
    }

    static void Index(Transform t, HashSet<Vector3Int> cms, HashSet<string> paths, HashSet<string> leaves,
        Dictionary<string, Transform> exact, Dictionary<string, List<Transform>> byPath, Dictionary<string, int> count)
    {
        var cm = Cm(t.position);
        bool cmHit = cms.Contains(cm);
        bool leafHit = leaves.Contains(t.name);
        if (cmHit || leafHit)
        {
            string path = PathOf(t);
            if (cmHit)
            {
                // Every piece standing on this centimetre is counted, so the ordinals agree with Walk's
                string b = KeyOf(path, cm, 0);
                count.TryGetValue(b, out int n);
                count[b] = n + 1;
                exact[n == 0 ? b : b + "#" + n] = t;
            }
            if (leafHit && paths.Contains(path))
            {
                if (!byPath.TryGetValue(path, out var list)) byPath[path] = list = new List<Transform>();
                list.Add(t);
            }
        }
        for (int i = 0; i < t.childCount; i++) Index(t.GetChild(i), cms, paths, leaves, exact, byPath, count);
    }

    /// A removed piece is hidden (anything that holds it keeps a valid reference); a unit in play is destroyed so the
    /// level's count of the living stays true
    static void Remove(Transform t)
    {
        if (Application.isPlaying && t.GetComponent<Enemy>() != null) Object.Destroy(t.gameObject);
        else t.gameObject.SetActive(false);
    }

    /// A block's texture tiled again to its new size (only the kit's cubes tile)
    static void Retile(Transform t)
    {
        var mf = t.GetComponent<MeshFilter>();
        var r = t.GetComponent<MeshRenderer>();
        if (mf == null || r == null || mf.sharedMesh == null || mf.sharedMesh.name != "Cube" || r.sharedMaterial == null) return;
        r.sharedMaterial = LevelKit.Retile(r.sharedMaterial, t.lossyScale);
    }
}
