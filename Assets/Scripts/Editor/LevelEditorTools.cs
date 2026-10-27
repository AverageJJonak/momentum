using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// THE LEVEL EDITOR, the hand tools the window offers: revert a piece to the code, select what you changed, the
/// level's materials as a paint palette, a new block, the HARD LINE redrawn after you move things.
/// </summary>
public static partial class LevelEditor
{
    /// The selected pieces go back to how the builders made them; selected additions are deleted
    public static void RevertSelection()
    {
        var h = Holder();
        if (h == null) return;
        var pieces = OriginalsByKey(h);
        Undo.SetCurrentGroupName("Revert To Generated");
        int group = Undo.GetCurrentGroup();
        foreach (var go in Selection.gameObjects)
        {
            if (go == null || !go.transform.IsChildOf(h.transform) || go == h.gameObject) continue;
            var t = go.transform;
            if (go.GetComponent<LevelSpawnMarker>() != null)
            {
                Undo.RecordObject(t, "Revert To Generated");
                t.localPosition = h.spawn + Vector3.up * t.localScale.y;
                continue;
            }
            if (go.GetComponent<LevelDerived>() != null) continue;
            var piece = go.GetComponent<LevelPiece>();
            bool original = piece != null && piece.origin == Origin(go) && go.GetComponent<LevelAdded>() == null;
            if (!original) { Undo.DestroyObjectImmediate(go); continue; }
            Transform parent = string.IsNullOrEmpty(piece.gParent) ? h.transform : pieces.TryGetValue(piece.gParent, out var p) ? p.transform : null;
            if (parent != null && t.parent != parent) Undo.SetTransformParent(t, parent, "Revert To Generated");
            Undo.RecordObject(t, "Revert To Generated");
            t.localPosition = piece.gPos; t.localRotation = piece.gRot; t.localScale = piece.gScale;
            Undo.RecordObject(piece, "Revert To Generated");
            piece.bPos = piece.gPos; piece.bRot = piece.gRot; piece.bScale = piece.gScale;
            piece.hadEdit = false;
            if (go.activeSelf != piece.gActive) { Undo.RecordObject(go, "Revert To Generated"); go.SetActive(piece.gActive); }
            var r = go.GetComponent<Renderer>();
            if (r != null && !string.IsNullOrEmpty(piece.gMat) && MaterialOf(t) != piece.gMat)
            {
                var m = FindMaterial(h, piece.gMat);
                if (m != null) { Undo.RecordObject(r, "Revert To Generated"); r.sharedMaterial = LevelKit.Retile(m, t.lossyScale); }
            }
        }
        Undo.CollapseUndoOperations(group);
        Touched(h);
    }

    /// Selects every piece you moved, painted or hid, every addition, and the spawn if it moved
    public static void SelectChanged()
    {
        var h = Holder();
        if (h == null) return;
        var changed = new List<GameObject>();
        var patch = Capture(h, null, changed);
        Selection.objects = changed.ToArray();
        int removed = 0;
        foreach (var e in patch.edits) if (e.removed && !e.moved) removed++;
        Debug.Log("Momentum Level Editor: " + changed.Count + " changed object(s) selected" + (removed > 0 ? "; " + removed + " deleted piece(s) cannot be selected (Ctrl+Z, or Reset To Code)" : "") + ".");
    }

    public struct Swatch { public string name; public Material material; public int uses; public Color colour; }

    /// The level's materials, most used first: the palette (one entry per surface, whatever size it was tiled to)
    public static List<Swatch> Palette()
    {
        var list = new List<Swatch>();
        var h = Holder();
        if (h == null) return list;
        var at = new Dictionary<string, int>();
        foreach (var r in h.GetComponentsInChildren<Renderer>(true))
        {
            if (r.GetComponentInParent<LevelDerived>() != null || r.GetComponent<LevelSpawnMarker>() != null) continue;
            var m = r.sharedMaterial;
            if (m == null) continue;
            var b = LevelKit.UntiledOf(m);
            if (at.TryGetValue(b.name, out int i)) { var s = list[i]; s.uses++; list[i] = s; continue; }
            at[b.name] = list.Count;
            Color c = b.HasProperty("_BaseColor") ? b.GetColor("_BaseColor") : b.HasProperty("_Color") ? b.color : Color.grey;
            if (b.HasProperty("_BaseMap") && b.GetTexture("_BaseMap") is Texture2D tex && c.r > 0.95f && c.g > 0.95f && c.b > 0.95f) c = Average(tex);
            list.Add(new Swatch { name = b.name, material = m, uses = 1, colour = c });
        }
        list.Sort((a, b) => b.uses.CompareTo(a.uses));
        return list;
    }

    static Color Average(Texture2D tex)
    {
        try { var px = tex.GetPixels32(tex.mipmapCount > 1 ? tex.mipmapCount - 1 : 0); if (px.Length == 0) return Color.grey; return px[0]; }
        catch { return Color.grey; }   // not readable: the swatch is grey, the paint still works
    }

    /// Paints the selected pieces (and blocks) with `m`, tiled to each one's size
    public static void Paint(Material m)
    {
        var h = Holder();
        if (h == null || m == null) return;
        Undo.SetCurrentGroupName("Paint");
        int group = Undo.GetCurrentGroup();
        int n = 0;
        foreach (var go in Selection.gameObjects)
        {
            if (go == null || !go.transform.IsChildOf(h.transform) || go.GetComponentInParent<LevelDerived>() != null) continue;
            var r = go.GetComponent<Renderer>();
            if (r == null || go.GetComponent<LevelSpawnMarker>() != null) continue;
            Undo.RecordObject(r, "Paint");
            r.sharedMaterial = LevelKit.Retile(m, go.transform.lossyScale);
            n++;
        }
        Undo.CollapseUndoOperations(group);
        if (n > 0) Touched(h);
    }

    /// A 4 × 1 × 4 m block at the middle of the Scene view, on the grid, in `m`: a new floor, a lip, a wall
    public static void AddBlock(Material m)
    {
        var h = Holder();
        if (h == null) return;
        var view = SceneView.lastActiveSceneView;
        Vector3 at = view != null ? view.pivot : Vector3.zero;
        at = new Vector3(Mathf.Round(at.x * 2f) * 0.5f, Mathf.Round(at.y * 2f) * 0.5f, Mathf.Round(at.z * 2f) * 0.5f);
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Block";
        go.transform.SetParent(h.transform, false);
        go.transform.position = at;
        go.transform.localScale = new Vector3(4f, 1f, 4f);
        if (m != null) go.GetComponent<Renderer>().sharedMaterial = LevelKit.Retile(m, go.transform.lossyScale);
        NoSave(go);
        Undo.RegisterCreatedObjectUndo(go, "Add Block");
        Selection.activeGameObject = go;
        Touched(h);
    }

    /// The HARD LINE (the beads and the lit floor lines) drawn again round the level as it now stands
    public static void RefreshLines()
    {
        var h = Holder();
        if (h == null) return;
        working = true;
        try
        {
            var old = new List<GameObject>();
            for (int i = 0; i < h.transform.childCount; i++)
            {
                var c = h.transform.GetChild(i);
                if (c.name == "HardLine" && c.GetComponent<LevelDerived>() != null) old.Add(c.gameObject);
            }
            foreach (var go in old) Object.DestroyImmediate(go);
            var scene = h.gameObject.scene;
            var before = new HashSet<GameObject>(scene.GetRootGameObjects());
            Physics.SyncTransforms();
            GameBootstrap.HardLine(h.kind);
            foreach (var go in scene.GetRootGameObjects())
            {
                if (before.Contains(go)) continue;
                go.transform.SetParent(h.transform, true);
                if (go.GetComponent<LevelDerived>() == null) go.AddComponent<LevelDerived>();
                NoSave(go);
            }
        }
        finally { working = false; Quiet(); }
    }

    static Dictionary<string, LevelPiece> OriginalsByKey(LevelHolder h)
    {
        var map = new Dictionary<string, LevelPiece>();
        foreach (var p in h.GetComponentsInChildren<LevelPiece>(true))
            if (p.origin == Origin(p.gameObject) && p.GetComponent<LevelAdded>() == null) map[p.key] = p;
        return map;
    }

    static Material FindMaterial(LevelHolder h, string name)
    {
        foreach (var r in h.GetComponentsInChildren<Renderer>(true))
            if (r.sharedMaterial != null && LevelKit.UntiledOf(r.sharedMaterial).name == name) return r.sharedMaterial;
        return null;
    }
}
