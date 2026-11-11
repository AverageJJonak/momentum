using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ARCHITECTURE MOVES (STEP 3, 2026-11-10; the movement identity programme: "ledge vault, rail grind, pole swing"). The
/// mark the level kit puts on a piece the movement can use: a LEDGE to vault, a RAIL to grind, a POLE to swing round.
/// The motor does these only on marked pieces, so nothing that already stands in a level changes, and the kit decides
/// which architecture is for use (art-direction.md: cornices are vaulted and ground, statues and lamp posts are swung
/// round). One lookup per collider, remembered until the next level is built.
/// </summary>
public class MoveSurface : MonoBehaviour
{
    public enum Kind { Vault, Grind, Swing }
    public Kind kind;

    static readonly Dictionary<Collider, MoveSurface> cache = new Dictionary<Collider, MoveSurface>();

    /// Forgets every lookup: a level builder calls this before it builds (the pieces of the level before are gone)
    public static void Forget() { cache.Clear(); Poles.Clear(); }

    /// Every pole marked since the last Forget: the motor looks through these for one in reach (a level has a handful), so a
    /// pole needs no solid collider and is never a thing to run into
    public static readonly List<MoveSurface> Poles = new List<MoveSurface>();
    /// A pole reaches this far above and below its middle
    public float halfHeight = 2.5f;

    /// The mark on this collider, or null
    public static MoveSurface Of(Collider c)
    {
        if (c == null) return null;
        if (!cache.TryGetValue(c, out var m)) { m = c.GetComponent<MoveSurface>(); cache[c] = m; }
        return m;
    }

    public static bool Is(Collider c, Kind k) { var m = Of(c); return m != null && m.kind == k; }

    /// Marks a piece (the kit calls this as it places it)
    public static MoveSurface Mark(GameObject go, Kind k)
    {
        if (go == null) return null;
        var m = go.GetComponent<MoveSurface>();
        if (m == null) m = go.AddComponent<MoveSurface>();
        m.kind = k;
        if (k == Kind.Swing && !Poles.Contains(m)) Poles.Add(m);
        foreach (var c in go.GetComponents<Collider>()) cache[c] = m;
        return m;
    }

    /// A rail's line in the world: the two ends of the box's longest side, along the middle of its top (the kit's boxes are unit cubes, scaled)
    public void Line(out Vector3 a, out Vector3 b)
    {
        var t = transform;
        Vector3 s = t.lossyScale;
        bool alongX = s.x >= s.z;
        Vector3 axis = alongX ? t.right : t.forward;
        float half = (alongX ? s.x : s.z) * 0.5f;
        Vector3 top = t.position + t.up * (s.y * 0.5f);
        a = top - axis * half;
        b = top + axis * half;
    }

    /// A pole's axis in the world: the point on it level with `y`
    public Vector3 AxisAt(float y) { Vector3 p = transform.position; p.y = y; return p; }
}
