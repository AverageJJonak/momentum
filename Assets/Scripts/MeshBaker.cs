using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// ONE DRAW INSTEAD OF A THOUSAND. Everything in this game is a scaled box, so
/// anything that never moves can be baked: hand the baker a material, a
/// position and a size instead of making a GameObject, and Flush welds every
/// box sharing a material into ONE mesh with ONE renderer. The city's lit
/// window bands went from about 1,700 objects a level to a couple of dozen
/// this way. Only for things that never move and never need a collider —
/// anything that animates keeps its own transform.
/// </summary>
public class MeshBaker
{
    readonly Dictionary<(Material mat, int cx, int cz), List<CombineInstance>> groups = new Dictionary<(Material mat, int cx, int cz), List<CombineInstance>>();

    public int Pieces { get; private set; }

    /// CELLS. When this is set, boxes are welded per material AND per square of this size, so a city becomes a grid
    /// of meshes Unity frustum-culls one square at a time. One mesh per material can never be culled — its bounds
    /// cover the whole city — and every triangle of it is drawn every frame whichever way the camera looks. Zero
    /// keeps the old behaviour: one mesh per material.
    public float CellSize;

    /// One box (or any primitive), in world space, to be welded into its material's mesh
    public void Add(Material mat, Vector3 pos, Vector3 size, Quaternion? rot = null, PrimitiveType type = PrimitiveType.Cube)
    {
        if (mat == null) return;
        int cx = 0, cz = 0;
        if (CellSize > 0f) { cx = Mathf.FloorToInt(pos.x / CellSize); cz = Mathf.FloorToInt(pos.z / CellSize); }
        var key = (mat, cx, cz);
        if (!groups.TryGetValue(key, out var list)) groups[key] = list = new List<CombineInstance>();
        list.Add(new CombineInstance { mesh = LevelKit.PrimMesh(type), transform = Matrix4x4.TRS(pos, rot ?? Quaternion.identity, size) });
        Pieces++;
    }

    /// Weld and empty: one child of `parent` per material, each a single mesh. Returns what it made.
    public List<Renderer> Flush(Transform parent, string name, bool shadows = false)
    {
        var made = new List<Renderer>();
        foreach (var pair in groups)
        {
            if (pair.Value.Count == 0) continue;
            var mesh = new Mesh { name = name + "Mesh", indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(pair.Value.ToArray(), true, true);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = pair.Key.mat;
            r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            r.receiveShadows = shadows;
            made.Add(r);
        }
        groups.Clear();
        Pieces = 0;
        return made;
    }
}

/// <summary>
/// The primitive meshes, fetched once. GameObject.CreatePrimitive makes a
/// collider every time and the level builders then destroy it again —
/// thousands of throwaway physics shapes per level. Prim() makes the same
/// object without ever creating one.
/// </summary>
public static partial class LevelKit
{
    static readonly Dictionary<PrimitiveType, Mesh> primMeshes = new Dictionary<PrimitiveType, Mesh>();

    /// The built-in mesh for a primitive type, borrowed once from a throwaway and kept
    public static Mesh PrimMesh(PrimitiveType type)
    {
        if (primMeshes.TryGetValue(type, out var mesh) && mesh != null) return mesh;
        var go = GameObject.CreatePrimitive(type);
        mesh = go.GetComponent<MeshFilter>().sharedMesh; // a built-in asset: it outlives the object
        Object.DestroyImmediate(go);
        primMeshes[type] = mesh;
        return mesh;
    }

    /// A primitive with no collider, ever: mesh, renderer, done. Shadows off unless asked.
    public static GameObject Prim(Transform parent, Vector3 pos, Vector3 size, Material mat, string name,
        PrimitiveType type = PrimitiveType.Cube, Quaternion? rot = null, bool shadows = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(pos, rot ?? Quaternion.identity);
        go.transform.localScale = size;
        go.AddComponent<MeshFilter>().sharedMesh = PrimMesh(type);
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        r.receiveShadows = shadows;
        return go;
    }
}
