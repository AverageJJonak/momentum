using UnityEngine;

/// <summary>
/// Deadlock-style authored tech hitboxes: invisible trigger volumes that mark
/// exactly where corner boosts and edge boosts live. The motor treats a marked
/// corner/edge as guaranteed tech (the geometric heuristics stay as fallback
/// for unmarked geometry). Markers live on layer 2 so they never block casts.
/// </summary>
public class CornerMarker : MonoBehaviour { }

public class EdgeMarker : MonoBehaviour { }

/// A LIP a corner boost lives on (course-grammar): the marker sits along the walkable top of a low block, so a
/// level says where the boost is instead of leaving it to the motor's lip raycast alone. Layer 2, trigger, no cast ever hits it.
public class LipMarker : MonoBehaviour { }

public static class TechMarkers
{
    /// Marks the four vertical edges of an axis-aligned box (pillar, wall end).
    public static void MarkBoxCorners(GameObject box)
    {
        Vector3 c = box.transform.position;
        Vector3 s = box.transform.localScale;
        for (int dx = -1; dx <= 1; dx += 2)
            for (int dz = -1; dz <= 1; dz += 2)
                Create<CornerMarker>(
                    c + new Vector3(dx * s.x * 0.5f, 0f, dz * s.z * 0.5f),
                    new Vector3(0.4f, s.y, 0.4f));
    }

    /// Marks a lip to slide off — an edge-boost strip along the drop.
    public static void MarkEdgeStrip(Vector3 center, Vector3 size)
        => Create<EdgeMarker>(center, size);

    /// Marks a corner-boost LIP: `top` is the centre of the block's walkable top, `size` the volume over it to mark
    public static void MarkLip(Vector3 top, Vector3 size)
        => Create<LipMarker>(top + Vector3.up * (size.y * 0.5f - 0.2f), size);

    static void Create<T>(Vector3 pos, Vector3 size) where T : Component
    {
        var go = new GameObject(typeof(T).Name);
        go.layer = 2; // Ignore Raycast: invisible to the motor's movement casts
        go.transform.position = pos;
        var box = go.AddComponent<BoxCollider>();
        box.size = size;
        box.isTrigger = true;
        go.AddComponent<T>();
    }
}
