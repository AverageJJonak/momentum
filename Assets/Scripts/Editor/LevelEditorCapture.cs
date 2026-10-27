using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// THE LEVEL EDITOR, what you changed: the working copy compared with the level as built, written as a LevelEdits
/// patch; saving, throwing away, reverting a piece, selecting what you changed, painting, adding a block, redrawing the
/// HARD LINE after you move things.
/// </summary>
public static partial class LevelEditor
{
    const string SavedFolder = "Assets/Resources/" + LevelEdits.ResourceFolder;
    const float PosEps = 0.0005f, RotEps = 0.01f, ScaleEps = 0.0005f;

    public struct Counts
    {
        public int edits, adds, unsupported;
        public bool spawn;
        public Counts(int edits, int adds, bool spawn, int unsupported) { this.edits = edits; this.adds = adds; this.spawn = spawn; this.unsupported = unsupported; }
    }

    /// What the last save (or generation) counted
    public static Counts LastCounts;

    /// Your edits, saved into the level's file (the game reads it on every load)
    public static void Save(bool commit)
    {
        var h = Holder();
        if (h == null) return;
        var unsupported = new List<string>();
        var patch = Capture(h, unsupported, null);
        LastCounts = new Counts(patch.edits.Count, patch.adds.Count, patch.spawnMoved, unsupported.Count);
        if (unsupported.Count > 0)
            Debug.LogWarning("Momentum Level Editor: " + unsupported.Count + " object(s) inside Level_Holder cannot be kept (only the level's pieces, their copies and plain primitives can): " + string.Join(", ", unsupported));
        if (!commit) { WritePending(h.kind, patch); Changed?.Invoke(); return; }

        working = true;
        try
        {
            string path = SavedFolder + "/" + LevelEdits.FileName(h.kind) + ".json";
            if (patch.Empty)
            {
                if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
            }
            else
            {
                Directory.CreateDirectory(SavedFolder);
                File.WriteAllText(path, JsonUtility.ToJson(patch, true));
                EditorApplication.delayCall += () => AssetDatabase.ImportAsset(path);   // not inside a scene save
            }
            DeletePending(h.kind);
            Dirty = false;
            Quiet();
            Debug.Log("Momentum Level Editor: " + h.kind + " edits saved (" + patch.edits.Count + " edit(s), " + patch.adds.Count + " addition(s)" + (patch.spawnMoved ? ", the spawn" : "") + ") → " + path);
        }
        finally { working = false; }
    }

    /// Throws away what you have not saved: the level comes back as the saved file has it
    public static void Discard(bool regenerate)
    {
        var h = Holder();
        LevelKind kind = h != null ? h.kind : LevelKind.Level1;
        if (h != null) DeletePending(kind);
        Dirty = false;
        if (regenerate && h != null)
        {
            DestroyHolder(h);
            var b = FindBootstrap();
            Generate(kind, b != null ? b.config : null, true);
        }
    }

    /// Throws away every edit of this level, saved or not: the level is the code's again
    public static void ResetToCode()
    {
        var h = Holder();
        if (h == null) return;
        var kind = h.kind;
        if (!EditorUtility.DisplayDialog("Reset " + kind + " to the code", "Every hand edit of " + kind + ", saved or not, is deleted. This cannot be undone.", "Delete my edits", "Cancel")) return;
        DeletePending(kind);
        string path = SavedFolder + "/" + LevelEdits.FileName(kind) + ".json";
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
        Dirty = false;
        DestroyHolder(h);
        var b = FindBootstrap();
        Generate(kind, b != null ? b.config : null, true);
    }

    static void WritePending(LevelKind kind, LevelEdits.Patch patch)
    {
        string path = LevelEdits.PendingPath(kind);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(patch, true));
    }

    static void DeletePending(LevelKind kind)
    {
        string path = LevelEdits.PendingPath(kind);
        if (File.Exists(path)) File.Delete(path);
    }

    // ------------------------------------------------------------ the comparison

    /// The working copy as a patch: every piece that differs from the level as built, every copy and block you added,
    /// every piece you deleted, the spawn. `changed` collects the objects behind it (for "select what I changed")
    public static LevelEdits.Patch Capture(LevelHolder h, List<string> unsupported, List<GameObject> changed)
    {
        var patch = new LevelEdits.Patch { level = h.kind.ToString() };
        var present = new HashSet<string>();
        for (int i = 0; i < h.transform.childCount; i++) Visit(h.transform.GetChild(i), h, patch, present, unsupported, changed);
        // A deleted piece: only the top of a deleted branch is written (hiding a parent hides its children)
        for (int i = 0; i < h.keys.Count; i++)
        {
            if (present.Contains(h.keys[i])) continue;
            int p = i < h.parentOf.Count ? h.parentOf[i] : -1;
            if (p >= 0 && !present.Contains(h.keys[p])) continue;
            patch.edits.Add(new LevelEdits.Edit { key = h.keys[i], name = LevelEdits.Leaf(h.keys[i]), removed = true });
        }
        return patch;
    }

    static void Visit(Transform t, LevelHolder h, LevelEdits.Patch patch, HashSet<string> present, List<string> unsupported, List<GameObject> changed)
    {
        if (t.GetComponent<LevelSpawnMarker>() != null)
        {
            Vector3 feet = h.transform.InverseTransformPoint(t.position) - Vector3.up * t.localScale.y;
            if ((feet - h.spawn).sqrMagnitude > 0.0001f) { patch.spawnMoved = true; patch.spawn = feet; changed?.Add(t.gameObject); }
            return;
        }
        if (t.GetComponent<LevelDerived>() != null) return;
        var added = t.GetComponent<LevelAdded>();
        var piece = t.GetComponent<LevelPiece>();
        if (added != null) { patch.adds.Add(AddOf(t, h, added.source, added.primitive)); changed?.Add(t.gameObject); OriginalsUnder(t, h, patch, present, unsupported, changed); return; }
        if (piece != null && piece.origin != Origin(t.gameObject)) { patch.adds.Add(AddOf(t, h, piece.key, "")); changed?.Add(t.gameObject); OriginalsUnder(t, h, patch, present, unsupported, changed); return; }   // a Ctrl+D copy
        if (piece != null)
        {
            present.Add(piece.key);
            var e = EditOf(t, piece, h);
            if (e != null) { patch.edits.Add(e); changed?.Add(t.gameObject); }
            for (int i = 0; i < t.childCount; i++) Visit(t.GetChild(i), h, patch, present, unsupported, changed);
            return;
        }
        if (PrimitiveOf(t, out string prim)) { patch.adds.Add(AddOf(t, h, "", prim)); changed?.Add(t.gameObject); OriginalsUnder(t, h, patch, present, unsupported, changed); return; }
        unsupported?.Add(t.name);
        OriginalsUnder(t, h, patch, present, unsupported, changed);
    }

    /// A built piece you hung under a copy or a block is still that piece (written relative to the level's root):
    /// an addition carries its own children, so only the originals below it are looked for
    static void OriginalsUnder(Transform t, LevelHolder h, LevelEdits.Patch patch, HashSet<string> present, List<string> unsupported, List<GameObject> changed)
    {
        for (int i = 0; i < t.childCount; i++)
        {
            var c = t.GetChild(i);
            var p = c.GetComponent<LevelPiece>();
            if (p != null && p.origin == Origin(c.gameObject) && c.GetComponent<LevelAdded>() == null) Visit(c, h, patch, present, unsupported, changed);
            else OriginalsUnder(c, h, patch, present, unsupported, changed);
        }
    }

    static LevelEdits.Edit EditOf(Transform t, LevelPiece piece, LevelHolder h)
    {
        string parentKey = ParentKeyOf(t, h);
        var e = new LevelEdits.Edit { key = piece.key, name = t.name };
        if (parentKey == null || parentKey != piece.gParent) { e.reparented = true; e.parent = parentKey ?? ""; }
        bool touched = !Same(t, piece.bPos, piece.bRot, piece.bScale);
        bool keptFromFile = piece.hadEdit && !Same(t, piece.gPos, piece.gRot, piece.gScale);
        if (e.reparented || touched || keptFromFile)
        {
            e.moved = true;
            Local(t, h, parentKey, out e.pos, out e.rot, out e.scale);
        }
        e.removed = piece.gActive && !t.gameObject.activeSelf;
        string mat = MaterialOf(t);
        if (!string.IsNullOrEmpty(mat) && mat != piece.gMat) e.material = mat;
        return e.moved || e.removed || e.reparented || !string.IsNullOrEmpty(e.material) ? e : null;
    }

    static LevelEdits.Add AddOf(Transform t, LevelHolder h, string source, string primitive)
    {
        string parentKey = ParentKeyOf(t, h);   // null: under something the game cannot find again, so relative to the level's root
        var a = new LevelEdits.Add { name = t.name, source = source ?? "", primitive = primitive ?? "", parent = parentKey ?? "", active = t.gameObject.activeSelf, material = MaterialOf(t) };
        Local(t, h, parentKey, out a.pos, out a.rot, out a.scale);
        return a;
    }

    /// Its parent's key; "" when it hangs straight off Level_Holder (a root of the level); null when its parent is
    /// something the game cannot find again (a copy, a block): then it is written relative to the level's root
    static string ParentKeyOf(Transform t, LevelHolder h)
    {
        var parent = t.parent;
        if (parent == null || parent == h.transform) return "";
        var pp = parent.GetComponent<LevelPiece>();
        return pp != null && pp.origin == Origin(parent.gameObject) && parent.GetComponent<LevelAdded>() == null ? pp.key : null;
    }

    static void Local(Transform t, LevelHolder h, string parentKey, out Vector3 pos, out Quaternion rot, out Vector3 scale)
    {
        if (parentKey == null)
        {
            pos = h.transform.InverseTransformPoint(t.position);
            rot = Quaternion.Inverse(h.transform.rotation) * t.rotation;
            scale = t.lossyScale;
        }
        else { pos = t.localPosition; rot = t.localRotation; scale = t.localScale; }
    }

    static bool Same(Transform t, Vector3 pos, Quaternion rot, Vector3 scale) =>
        (t.localPosition - pos).sqrMagnitude <= PosEps * PosEps && Quaternion.Angle(t.localRotation, rot) <= RotEps && (t.localScale - scale).sqrMagnitude <= ScaleEps * ScaleEps;

    static string MaterialOf(Transform t)
    {
        var r = t.GetComponent<Renderer>();
        return r != null && r.sharedMaterial != null ? LevelKit.UntiledOf(r.sharedMaterial).name : "";
    }

    static readonly string[] Primitives = { "Cube", "Sphere", "Cylinder", "Capsule", "Plane", "Quad" };

    /// A plain Unity primitive (GameObject → 3D Object, or Add Block)
    static bool PrimitiveOf(Transform t, out string prim)
    {
        prim = null;
        var mf = t.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return false;
        if (AssetDatabase.GetAssetPath(mf.sharedMesh) != "Library/unity default resources") return false;
        if (System.Array.IndexOf(Primitives, mf.sharedMesh.name) < 0) return false;
        prim = mf.sharedMesh.name;
        return true;
    }
}
