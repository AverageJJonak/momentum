using UnityEngine;

/// <summary>
/// THE FALL ARC (2026-10-23): where a body in the air comes down, by marching its ballistic arc in short straight
/// segments and linecasting each one against the world. The tricks' prompt needs the airtime left (is there room for
/// a flip?), the body camera's look-ahead needs the landing point (the camera nods toward it). Air control is not
/// predicted: the arc is the one gravity alone would give, which is the right guess for a committed jump. No
/// allocation: one Linecast per segment, nothing kept.
/// </summary>
public static class TrickArcs
{
    /// March from `from` (the feet) with velocity `v` under `g` for up to `maxSeconds` in steps of `step`. True with the
    /// first surface the arc meets and the seconds until it; false (and `seconds` = maxSeconds) when nothing is met
    public static bool Landing(Vector3 from, Vector3 v, float g, float maxSeconds, float step, out Vector3 point, out float seconds)
    {
        step = Mathf.Max(0.02f, step);
        int n = Mathf.Clamp(Mathf.CeilToInt(maxSeconds / step), 1, 64);
        Vector3 a = from;
        float t = 0f;
        for (int i = 0; i < n; i++)
        {
            float t2 = t + step;
            Vector3 b = from + new Vector3(v.x * t2, v.y * t2 - 0.5f * g * t2 * t2, v.z * t2);
            if (Physics.Linecast(a, b, out RaycastHit hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                float seg = Mathf.Max(1e-3f, (b - a).magnitude);
                float f = Mathf.Clamp01((hit.point - a).magnitude / seg);
                point = hit.point;
                seconds = t + step * f;
                return true;
            }
            a = b;
            t = t2;
        }
        point = a;
        seconds = maxSeconds;
        return false;
    }
}
