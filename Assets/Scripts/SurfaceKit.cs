using UnityEngine;

/// <summary>
/// HOW AN OPEN DISTRICT IS BUILT (Chapter 3). The linear levels are rooms: a
/// few hundred boxes each, every one its own object. A district is thousands,
/// all in view at once, so its bulk is WELDED (MeshBaker, in 160 m squares the
/// frustum can cull) — but a weld has no collider, and out here everything you
/// can reach has to be solid. So a Block is two things: a box in the weld, and
/// a bare BoxCollider of the same shape under one "Solids" parent. Look is the
/// weld alone, for what can never be touched. The handful of big surfaces you
/// actually RUN on (decks, ramps, the facade of a fallen tower) are ordinary
/// kit slabs instead, so they carry the 2 m speed grid and tile it properly:
/// SpanSlab lays one between any two points of its top surface.
/// The far city goes in a second weld, in 600 m squares, with no shadows.
/// </summary>
public static class SurfaceKit
{
    /// How far the camera draws out here: the house is nine kilometres off and the far city past eleven
    public const float FarClip = 12500f;

    static Transform root, solids;
    static MeshBaker near, far;

    public static Transform Root => root;
    /// The district's own weld: shadows on, small squares
    public static MeshBaker Near => near;
    /// The city beyond the cordon: no shadows, big squares
    public static MeshBaker Far => far;

    public static void Begin(Transform parent)
    {
        root = parent;
        solids = new GameObject("Solids").transform;
        solids.SetParent(parent, false);
        near = new MeshBaker { CellSize = 160f };
        far = new MeshBaker { CellSize = 600f };
    }

    /// Weld everything handed over so far
    public static void End()
    {
        near.Flush(root, "District", true);
        far.Flush(root, "FarCity");
    }

    /// A box you can stand on, run into and grapple: welded into the district, with its own collider
    public static void Block(Material m, Vector3 centre, Vector3 size, Quaternion? rot = null)
    {
        near.Add(m, centre, size, rot);
        Solid(centre, size, rot);
    }

    /// The collider alone (the look is somebody else's)
    public static void Solid(Vector3 centre, Vector3 size, Quaternion? rot = null)
    {
        var go = new GameObject("Solid");
        go.transform.SetParent(solids, false);
        go.transform.SetPositionAndRotation(centre, rot ?? Quaternion.identity);
        go.transform.localScale = size;
        go.AddComponent<BoxCollider>();
    }

    /// The look alone: for what nobody can ever reach
    public static void Look(Material m, Vector3 centre, Vector3 size, Quaternion? rot = null, PrimitiveType type = PrimitiveType.Cube)
        => near.Add(m, centre, size, rot, type);

    /// The rotation of a span from `a` to `b`: its long axis on the line, its top as near to up as that allows, rolled by `roll`
    public static Quaternion SpanRot(Vector3 a, Vector3 b, float roll = 0f)
        => Quaternion.LookRotation((b - a).normalized, Vector3.up) * Quaternion.Euler(0f, 0f, roll);

    /// A kit slab whose TOP surface's centreline runs from `topA` to `topB`: a deck, a ramp, a fallen span. Tiled, solid.
    public static GameObject SpanSlab(Transform parent, Vector3 topA, Vector3 topB, float width, float thick, Material mat, string name, float roll = 0f)
    {
        Quaternion rot = SpanRot(topA, topB, roll);
        Vector3 centre = (topA + topB) * 0.5f - rot * Vector3.up * (thick * 0.5f);
        var go = LevelKit.Slab(parent, centre, new Vector3(width, thick, Vector3.Distance(topA, topB)), mat, name);
        go.transform.rotation = rot;
        return go;
    }

    /// The same, welded: a girder, a toppled pier, a plate of somebody's floor
    public static void SpanBlock(Material m, Vector3 topA, Vector3 topB, float width, float thick, float roll = 0f)
    {
        Quaternion rot = SpanRot(topA, topB, roll);
        Vector3 centre = (topA + topB) * 0.5f - rot * Vector3.up * (thick * 0.5f);
        Block(m, centre, new Vector3(width, thick, Vector3.Distance(topA, topB)), rot);
    }

    /// A point on a span's top surface: `t` along it, `side` metres to its right, `up` off it
    public static Vector3 OnSpan(Vector3 topA, Vector3 topB, float t, float side = 0f, float up = 0f, float roll = 0f)
    {
        Quaternion rot = SpanRot(topA, topB, roll);
        return Vector3.Lerp(topA, topB, t) + rot * new Vector3(side, up, 0f);
    }
}
