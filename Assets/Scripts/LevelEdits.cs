using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// HAND EDITS (2026-10-20). The levels are built by code when a scene loads, so a fix made by hand in the editor
/// (Momentum → Level Editor) is kept as a small file per level, Resources/LevelEdits/&lt;LevelKind&gt;.json, and laid
/// back over the built level by GameBootstrap right after the builders run and BEFORE the passes that follow them
/// (the coplanar fix, the HARD LINE beads, the welding), so those passes see the fixed level.
///
/// What an edit can be: a piece MOVED, turned or scaled (its local transform), REMOVED (hidden; a unit is destroyed),
/// moved UNDER another piece, PAINTED with another of the level's materials; an ADDITION is a copy of a piece (Ctrl+D
/// in the editor) or a plain block (a Unity primitive) in one of the level's materials; and the SPAWN.
///
/// How a piece is found: by its KEY, its name path from its root and where it stood when built, to the centimetre
/// ("Level1/Hall/Slab@1250,1400,8800"; "#n" when two pieces share both). A builder change that shifts a piece a little
/// still finds it: the nearest piece on the same path within two metres. One that renames or removes it leaves the
/// edit an ORPHAN, named in the console; nothing else breaks.
///
/// Play in the editor uses the working copy's UNSAVED edits (Temp/LevelEditsPending, written on entering Play and gone
/// when Unity closes), the way Play uses an unsaved scene; a build reads only the saved file.
/// </summary>
public static partial class LevelEdits
{
    public const string ResourceFolder = "LevelEdits";
    public const string PendingFolder = "Temp/LevelEditsPending";
    const float FallbackReach = 2f;

    [Serializable]
    public class Patch
    {
        public int version = 1;
        public string level;
        public bool spawnMoved;
        public Vector3 spawn;
        public List<Edit> edits = new List<Edit>();
        public List<Add> adds = new List<Add>();
        public bool Empty => !spawnMoved && (edits == null || edits.Count == 0) && (adds == null || adds.Count == 0);
    }

    [Serializable]
    public class Edit
    {
        public string key;
        /// For the reader: the piece's name
        public string name;
        public bool removed;
        /// pos / rot / scale are its new LOCAL transform
        public bool moved;
        public Vector3 pos;
        public Quaternion rot = Quaternion.identity;
        public Vector3 scale = Vector3.one;
        /// It now hangs under `parent` (a key; "" = a root of the level)
        public bool reparented;
        public string parent;
        /// Another of this level's materials, by its (untiled) name; "" = unchanged
        public string material;
    }

    [Serializable]
    public class Add
    {
        public string name;
        /// A copy of this piece (its key); "" = a primitive
        public string source;
        /// Cube, Sphere, Cylinder, Capsule, Plane or Quad when it is not a copy
        public string primitive;
        /// Its parent's key; "" = a root of the level
        public string parent;
        /// Local to the parent (to the level's root when there is none)
        public Vector3 pos;
        public Quaternion rot = Quaternion.identity;
        public Vector3 scale = Vector3.one;
        public string material;
        public bool active = true;
    }

    public class Result
    {
        public int edited, added;
        public bool spawnMoved;
        public Vector3 spawn;
        public readonly List<string> orphans = new List<string>();
        public readonly List<KeyValuePair<Transform, Edit>> edits = new List<KeyValuePair<Transform, Edit>>();
        public readonly List<KeyValuePair<GameObject, Add>> adds = new List<KeyValuePair<GameObject, Add>>();
    }

    // ------------------------------------------------------------ the file

    public static string FileName(LevelKind kind) => kind.ToString();
    static string ProjectFolder => Path.GetDirectoryName(Application.dataPath);
    public static string PendingPath(LevelKind kind) => Path.Combine(ProjectFolder, PendingFolder, FileName(kind) + ".json");
    public static string SavedPath(LevelKind kind) => Path.Combine(Application.dataPath, "Resources", ResourceFolder, FileName(kind) + ".json");

    /// The level's edits: in the editor the unsaved ones if there are any, else the saved file (read from disk, so a
    /// save made a moment ago is seen before Unity has imported it); in a build the saved file. Null when there are none
    public static Patch Load(LevelKind kind)
    {
        if (Application.isEditor)
        {
            string pending = PendingPath(kind);
            if (File.Exists(pending)) return Parse(File.ReadAllText(pending), kind);
            string saved = SavedPath(kind);
            return File.Exists(saved) ? Parse(File.ReadAllText(saved), kind) : null;
        }
        var text = Resources.Load<TextAsset>(ResourceFolder + "/" + FileName(kind));
        return text != null ? Parse(text.text, kind) : null;
    }

    public static Patch Parse(string json, LevelKind kind)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var p = JsonUtility.FromJson<Patch>(json);
            if (p == null) return null;
            if (p.edits == null) p.edits = new List<Edit>();
            if (p.adds == null) p.adds = new List<Add>();
            return p;
        }
        catch (Exception e)
        {
            Debug.LogWarning("LevelEdits: the " + kind + " edits could not be read (" + e.Message + "); the level is built without them.");
            return null;
        }
    }

    // ------------------------------------------------------------ keys

    public static Vector3Int Cm(Vector3 p) => new Vector3Int(Mathf.RoundToInt(p.x * 100f), Mathf.RoundToInt(p.y * 100f), Mathf.RoundToInt(p.z * 100f));
    public static string KeyOf(string path, Vector3Int cm, int ordinal) => path + "@" + cm.x + "," + cm.y + "," + cm.z + (ordinal > 0 ? "#" + ordinal : "");

    public static bool ParseKey(string key, out string path, out Vector3Int cm, out int ordinal)
    {
        path = null; cm = default; ordinal = 0;
        if (string.IsNullOrEmpty(key)) return false;
        int at = key.LastIndexOf('@');
        if (at <= 0) return false;
        path = key.Substring(0, at);
        string rest = key.Substring(at + 1);
        int hash = rest.IndexOf('#');
        if (hash >= 0) { int.TryParse(rest.Substring(hash + 1), out ordinal); rest = rest.Substring(0, hash); }
        var parts = rest.Split(',');
        if (parts.Length != 3 || !int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y) || !int.TryParse(parts[2], out int z)) return false;
        cm = new Vector3Int(x, y, z);
        return true;
    }

    /// The last name of a key's path (what the piece is called)
    public static string Leaf(string pathOrKey)
    {
        if (string.IsNullOrEmpty(pathOrKey)) return "";
        int at = pathOrKey.LastIndexOf('@');
        string path = at > 0 ? pathOrKey.Substring(0, at) : pathOrKey;
        int slash = path.LastIndexOf('/');
        return slash >= 0 ? path.Substring(slash + 1) : path;
    }

    public static string PathOf(Transform t)
    {
        var sb = new StringBuilder(64);
        Append(sb, t);
        return sb.ToString();
    }

    static void Append(StringBuilder sb, Transform t)
    {
        if (t.parent != null) { Append(sb, t.parent); sb.Append('/'); }
        sb.Append(t.name);
    }

    /// Every transform under `roots` with its key and its parent's key, in the one order the editor and the game both
    /// use: roots in scene order, then depth first in sibling order. Call it on a level as built, before anything moves
    public static void Walk(IList<GameObject> roots, Action<Transform, string, string> visit)
    {
        var count = new Dictionary<string, int>();
        foreach (var r in roots)
            if (r != null) WalkNode(r.transform, r.name, "", count, visit);
    }

    static void WalkNode(Transform t, string path, string parentKey, Dictionary<string, int> count, Action<Transform, string, string> visit)
    {
        string b = KeyOf(path, Cm(t.position), 0);
        count.TryGetValue(b, out int n);
        count[b] = n + 1;
        string key = n == 0 ? b : b + "#" + n;
        visit(t, key, parentKey);
        for (int i = 0; i < t.childCount; i++)
        {
            var c = t.GetChild(i);
            WalkNode(c, path + "/" + c.name, key, count, visit);
        }
    }

    /// The scene's roots that `before` did not hold: what a build just made
    public static List<GameObject> NewRoots(Scene scene, HashSet<GameObject> before)
    {
        var list = new List<GameObject>();
        foreach (var g in scene.GetRootGameObjects())
            if (!before.Contains(g)) list.Add(g);
        return list;
    }
}
