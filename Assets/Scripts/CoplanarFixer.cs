using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NO TWO FACES ON ONE PLANE. Everything in these levels is a scaled cube, and
/// a few thousand of them placed by number land face to face: a landing on a
/// floor, a poster on a wall, two rooms that both built the wall between them,
/// a trim on a slab. Two faces on the same plane facing the same way flicker as
/// the depth buffer picks one and then the other. This runs once after a level
/// is built: every axis-aligned cube's six faces go into bins by their plane,
/// any two faces in one bin that overlap in area get pulled apart by a little
/// over a centimetre — the decor moves rather than the solid, and the smaller
/// face rather than the larger — and the flicker is gone. Welded meshes,
/// units, moving things and anything turned off the axes are left alone.
/// </summary>
public static class CoplanarFixer
{
    const float Eps = 0.012f;
    const float Bin = 0.004f;

    struct Box { public Transform t; public Vector3 c, e; public bool solid; }

    /// Fix the whole scene; returns how many boxes were moved
    public static int Run()
    {
        var boxes = new List<Box>();
        foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            var mf = mr.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null || mf.sharedMesh.name != "Cube") continue;
            var t = mr.transform;
            if (!AxisAligned(t.rotation)) continue;
            if (mr.GetComponentInParent<Enemy>() != null || mr.GetComponentInParent<Rigidbody>() != null) continue;
            Vector3 s = t.lossyScale;
            Vector3 e = Abs(t.rotation * new Vector3(s.x, 0f, 0f)) * 0.5f + Abs(t.rotation * new Vector3(0f, s.y, 0f)) * 0.5f + Abs(t.rotation * new Vector3(0f, 0f, s.z)) * 0.5f;
            if (e.x <= 1e-4f || e.y <= 1e-4f || e.z <= 1e-4f) continue;
            boxes.Add(new Box { t = t, c = t.position, e = e, solid = mr.GetComponent<Collider>() != null });
        }
        int moved = 0;
        var bins = new Dictionary<int, List<int>>();
        for (int axis = 0; axis < 3; axis++)
            for (int sign = -1; sign <= 1; sign += 2)
            {
                bins.Clear();
                for (int i = 0; i < boxes.Count; i++)
                {
                    int key = Mathf.FloorToInt((boxes[i].c[axis] + sign * boxes[i].e[axis]) / Bin);
                    if (!bins.TryGetValue(key, out var list)) bins[key] = list = new List<int>();
                    list.Add(i);
                }
                int a1 = (axis + 1) % 3, a2 = (axis + 2) % 3;
                foreach (var pair in bins)
                {
                    var g = pair.Value;
                    bins.TryGetValue(pair.Key + 1, out var next);
                    for (int i = 0; i < g.Count; i++)
                    {
                        for (int j = i + 1; j < g.Count; j++) moved += Separate(boxes, g[i], g[j], axis, sign, a1, a2);
                        if (next != null) for (int j = 0; j < next.Count; j++) moved += Separate(boxes, g[i], next[j], axis, sign, a1, a2);
                    }
                }
            }
        return moved;
    }

    /// Two faces in one bin: if they overlap in area (touching edges do not count), move one of the boxes off the plane
    static int Separate(List<Box> boxes, int ia, int ib, int axis, int sign, int a1, int a2)
    {
        var A = boxes[ia]; var B = boxes[ib];
        if (A.t == null || B.t == null) return 0;
        float pa = A.c[axis] + sign * A.e[axis], pb = B.c[axis] + sign * B.e[axis];
        if (Mathf.Abs(pa - pb) > Bin) return 0;   // the neighbouring bin held it, but it is not on this plane
        float o1 = Mathf.Min(A.c[a1] + A.e[a1], B.c[a1] + B.e[a1]) - Mathf.Max(A.c[a1] - A.e[a1], B.c[a1] - B.e[a1]);
        float o2 = Mathf.Min(A.c[a2] + A.e[a2], B.c[a2] + B.e[a2]) - Mathf.Max(A.c[a2] - A.e[a2], B.c[a2] - B.e[a2]);
        if (o1 < 0.01f || o2 < 0.01f) return 0;
        bool moveA = A.solid == B.solid ? A.e[a1] * A.e[a2] <= B.e[a1] * B.e[a2] : !A.solid;
        int m = moveA ? ia : ib;
        var M = boxes[m];
        Vector3 d = Vector3.zero;
        d[axis] = sign * Eps;
        M.t.position += d;
        M.c += d;
        boxes[m] = M;
        return 1;
    }

    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    /// Turned only by right angles: every local axis lies on a world axis
    static bool AxisAligned(Quaternion q)
    {
        Vector3 r = q * Vector3.right, u = q * Vector3.up, f = q * Vector3.forward;
        return OnAxis(r) && OnAxis(u) && OnAxis(f);
    }

    static bool OnAxis(Vector3 v)
    {
        float ax = Mathf.Abs(v.x), ay = Mathf.Abs(v.y), az = Mathf.Abs(v.z);
        return Mathf.Max(ax, ay, az) > 0.9999f;
    }
}
