using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Test
{
    public static readonly List<GameObject> Spawned = new List<GameObject>();
    static int failures;

    static void Check(bool ok, string what) { Console.WriteLine((ok ? "PASS  " : "FAIL  ") + what); if (!ok) failures++; }
    static bool Near(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < 1e-6f;

    static readonly Material Brass = new Material { name = "Brass BBBBBB" }, Oxblood = new Material { name = "Oxblood CCCCCC" }, Plaster = new Material { name = "Plaster AAAAAA" };

    class Level { public List<GameObject> roots = new List<GameObject>(); public Transform A, B, B2, C, D, E, Hall, Gallery, U; }

    static GameObject Node(string name, Transform parent, Vector3 local)
    {
        var g = new GameObject(name);
        g.transform.localPosition = local;
        if (parent != null) g.transform.SetParent(parent, false);
        return g;
    }

    static void Cube(GameObject g, Material m) { g.AddComponent<MeshFilter>().sharedMesh = new Mesh { name = "Cube" }; g.AddComponent<MeshRenderer>().sharedMaterial = m; }

    /// The same level every time, as a builder would make it; `shiftA` moves slab A as a changed builder might
    static Level Build(float shiftA)
    {
        var L = new Level();
        var level = Node("Level1", null, Vector3.zero);
        L.Hall = Node("Hall", level.transform, new Vector3(0, 0, 10)).transform;
        var a = Node("Slab", L.Hall, new Vector3(shiftA, 0, 0)); Cube(a, Brass); L.A = a.transform;
        var b = Node("Slab", L.Hall, new Vector3(4, 0, 0)); Cube(b, Plaster); L.B = b.transform;
        var b2 = Node("Slab", L.Hall, new Vector3(4, 0, 0)); Cube(b2, Plaster); L.B2 = b2.transform;   // a twin on the same centimetre
        var c = Node("Lip", L.Hall, new Vector3(2, 1, 0)); Cube(c, Oxblood); L.C = c.transform;
        var d = Node("Floor", L.Hall, new Vector3(0, -0.5f, 0)); Cube(d, Plaster); L.D = d.transform;
        L.Gallery = Node("Gallery", level.transform, new Vector3(0, 0, 40)).transform;
        var e = Node("Slab", L.Gallery, Vector3.zero); Cube(e, Plaster); L.E = e.transform;
        var unit = Node("EnemyChaser", null, new Vector3(3, 0, 20));
        unit.AddComponent<Enemy>().kind = EnemyKind.Chaser;
        L.U = unit.transform;
        L.roots.Add(level);
        L.roots.Add(unit);
        return L;
    }

    static Dictionary<Transform, string> Keys(Level L)
    {
        var map = new Dictionary<Transform, string>();
        LevelEdits.Walk(L.roots, (t, key, parentKey) => map[t] = key);
        return map;
    }

    public static int Main()
    {
        // Keys
        var L0 = Build(0f);
        var k = Keys(L0);
        Check(k[L0.A] == "Level1/Hall/Slab@0,0,1000", "a piece's key is its path and where it stands, in centimetres: " + k[L0.A]);
        Check(!k[L0.B].Contains("#") && k[L0.B2].EndsWith("#1"), "twins on one centimetre are told apart by an ordinal: " + k[L0.B2]);
        Check(k.Values.Distinct().Count() == k.Count, "every key in the level is unique (" + k.Count + " pieces)");
        string odd = LevelEdits.KeyOf("Level1/Lip @ 2#3/Slab", new Vector3Int(-150, 20, 3), 2);
        Check(LevelEdits.ParseKey(odd, out var p, out var cm, out var ord) && p == "Level1/Lip @ 2#3/Slab" && cm.Equals(new Vector3Int(-150, 20, 3)) && ord == 2, "a key with '@' and '#' in its names parses back: " + odd);
        Check(LevelEdits.Leaf(k[L0.C]) == "Lip", "Leaf names the piece");

        // The patch a hand would make in the editor
        var patch = new LevelEdits.Patch { level = "Level1", spawnMoved = true, spawn = new Vector3(1, 2, 3) };
        patch.edits.Add(new LevelEdits.Edit { key = k[L0.A], moved = true, pos = new Vector3(1, 2, 3), rot = Quaternion.AngleY(90), scale = new Vector3(2, 1, 2), material = "Oxblood CCCCCC" });
        patch.edits.Add(new LevelEdits.Edit { key = k[L0.B2], removed = true });
        patch.edits.Add(new LevelEdits.Edit { key = k[L0.C], reparented = true, parent = k[L0.E], moved = true, pos = new Vector3(0, 1, 0), rot = Quaternion.identity, scale = Vector3.one });
        patch.edits.Add(new LevelEdits.Edit { key = k[L0.U], moved = true, pos = new Vector3(6, 0, 20), rot = Quaternion.identity, scale = Vector3.one });
        patch.adds.Add(new LevelEdits.Add { name = "Slab (1)", source = k[L0.B], parent = k[L0.Hall], pos = new Vector3(8, 0, 0), rot = Quaternion.identity, scale = Vector3.one, active = true });
        patch.adds.Add(new LevelEdits.Add { name = "Block", primitive = "Cube", parent = "", pos = new Vector3(10, 0, 10), rot = Quaternion.identity, scale = new Vector3(4, 1, 4), material = "Brass BBBBBB", active = true });
        patch.adds.Add(new LevelEdits.Add { name = "EnemyChaser", source = k[L0.U], parent = "", pos = new Vector3(5, 0, 5), rot = Quaternion.identity, scale = Vector3.one, active = true });

        // The game builds the same level again and lays the patch on
        var L1 = Build(0f);
        var r = LevelEdits.Apply(patch, L1.roots, new MovementConfig());
        Check(r.orphans.Count == 0, "every edit found its piece (orphans: " + string.Join(", ", r.orphans) + ")");
        Check(Near(L1.A.localPosition, new Vector3(1, 2, 3)) && Near(L1.A.localScale, new Vector3(2, 1, 2)), "the moved slab stands where the hand put it");
        Check(L1.A.GetComponent<Renderer>().sharedMaterial == Oxblood, "the painted slab carries the new material");
        Check(!L1.B2.gameObject.activeSelf && L1.B.gameObject.activeSelf, "the deleted twin is hidden and only it");
        Check(L1.C.parent == L1.E && Near(L1.C.position, new Vector3(0, 1, 40)), "the lip moved under the gallery's slab, at the right place: " + L1.C.position);
        Check(L1.U.GetComponent<Enemy>().rehomed && Near(L1.U.localPosition, new Vector3(6, 0, 20)), "the moved unit is moved and walks its new post");
        var copy = L1.Hall.children.FirstOrDefault(t => t.name == "Slab (1)");
        Check(copy != null && Near(copy.position, new Vector3(8, 0, 10)) && copy.GetComponent<Renderer>().sharedMaterial == Plaster, "the Ctrl+D copy is made under the hall, in the source's material");
        var block = r.adds.Select(kv => kv.Key).FirstOrDefault(g => g.name == "Block");
        Check(block != null && block.transform.parent == null && Near(block.transform.position, new Vector3(10, 0, 10)) && block.GetComponent<Renderer>().sharedMaterial == Brass, "the new block stands at the root in the brass it was painted");
        Check(Spawned.Count == 1 && Spawned[0].GetComponent<Enemy>().kind == EnemyKind.Chaser && Near(Spawned[0].transform.position, new Vector3(5, 0, 5)), "the copied unit is made by the factory where the hand put it");
        Check(r.spawnMoved && Near(r.spawn, new Vector3(1, 2, 3)), "the spawn moved");
        Check(r.edited == 4 && r.added == 3, "counts: " + r.edited + " edits, " + r.added + " additions");

        // A builder change that moves slab A half a metre: the edit still finds it
        Spawned.Clear();
        var L2 = Build(0.5f);
        var r2 = LevelEdits.Apply(patch, L2.roots, new MovementConfig());
        Check(r2.orphans.Count == 0 && Near(L2.A.localPosition, new Vector3(1, 2, 3)), "a piece a builder moved 0.5 m is still found (nearest on its path)");
        Check(Near(L2.B.localPosition, new Vector3(4, 0, 0)) && Near(L2.E.localPosition, Vector3.zero), "the near match did not steal another piece");

        // One that moves it five metres: that edit is an orphan, the rest still land
        Spawned.Clear();
        var L3 = Build(5f);
        var r3 = LevelEdits.Apply(patch, L3.roots, new MovementConfig());
        Check(r3.orphans.Count == 1 && r3.orphans[0] == k[L0.A], "a piece moved 5 m is reported, not guessed: " + string.Join(", ", r3.orphans));
        Check(Near(L3.A.localPosition, new Vector3(5, 0, 0)) && !L3.B2.gameObject.activeSelf && L3.C.parent == L3.E, "the other edits still land");

        // An empty patch does nothing
        var L4 = Build(0f);
        var r4 = LevelEdits.Apply(new LevelEdits.Patch(), L4.roots, new MovementConfig());
        Check(r4.edited == 0 && r4.added == 0 && Near(L4.A.localPosition, Vector3.zero), "an empty patch leaves the level alone");

        Console.WriteLine(failures == 0 ? "ALL PASSED" : failures + " FAILED");
        return failures == 0 ? 0 : 1;
    }
}
