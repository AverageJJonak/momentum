using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WHAT MOVES ON THE ROOFS (RoofLife): the sheets on the laundry lines, the cats' tails, the band's musicians — every
/// pivot swings about one local axis on two sines, one Update over flat arrays. A pivot keeps the rotation it was
/// built with (the sway is composed on it). Nothing here allocates after Finish.
/// </summary>
public class RoofLifeAnimator : MonoBehaviour
{
    readonly List<Transform> pivots = new List<Transform>();
    readonly List<Quaternion> bases = new List<Quaternion>();
    readonly List<Vector3> axes = new List<Vector3>();
    readonly List<Vector4> prm = new List<Vector4>();   // amplitude, rate, phase, second amplitude
    Transform[] P; Quaternion[] B; Vector3[] A; Vector4[] K;

    /// `pivot` swings `amp` degrees about its local `axis` at `rate` rad/s (plus a faster `amp2` harmonic)
    public void Sway(Transform pivot, Vector3 axis, float amp, float rate, float phase, float amp2 = 0f)
    {
        pivots.Add(pivot); bases.Add(pivot.localRotation); axes.Add(axis); prm.Add(new Vector4(amp, rate, phase, amp2));
    }

    public void Finish() { P = pivots.ToArray(); B = bases.ToArray(); A = axes.ToArray(); K = prm.ToArray(); }

    void Update()
    {
        if (P == null) return;
        float t = Time.time;
        for (int i = 0; i < P.Length; i++)
        {
            var p = P[i];
            if (p == null) continue;
            var k = K[i];
            float ang = k.x * Mathf.Sin(t * k.y + k.z) + k.w * Mathf.Sin(t * k.y * 2.7f + k.z * 1.3f);
            p.localRotation = B[i] * Quaternion.AngleAxis(ang, A[i]);
        }
    }
}

/// <summary>
/// A WINDOW-WASHER'S CRADLE on a facade (RoofLife.Cradle): the platform climbs from `y0` to `y1` under its davit,
/// dwells, comes down, dwells, on a period of a minute or so; two cables stretch from the davit's arms to it.
/// </summary>
public class WashCradle : MonoBehaviour
{
    Vector3 top;
    float y0, y1, period;
    Transform[] cables;
    Vector3[] anchors;

    /// `topAt` is the davit's hang point; `dy0`/`dy1` the travel below it (negative); `cableAnchors` where the cables hang from
    public void Init(Vector3 topAt, float dy0, float dy1, Transform[] cableRods, Vector3[] cableAnchors, float seconds)
    {
        top = topAt; y0 = dy0; y1 = dy1; cables = cableRods; anchors = cableAnchors; period = Mathf.Max(10f, seconds);
    }

    void Update()
    {
        float u = Mathf.Repeat(Time.time / period, 1f);
        float k = u < 0.42f ? Mathf.SmoothStep(0f, 1f, u / 0.42f) : u < 0.5f ? 1f : u < 0.92f ? Mathf.SmoothStep(1f, 0f, (u - 0.5f) / 0.42f) : 0f;
        float y = Mathf.Lerp(y0, y1, k);
        Vector3 p = transform.position;
        p.y = top.y + y;
        transform.position = p;
        if (cables == null) return;
        float len = Mathf.Max(0.2f, -y - 1.0f);   // from the arm down to the rail tops
        for (int i = 0; i < cables.Length; i++)
        {
            if (cables[i] == null) continue;
            cables[i].position = anchors[i] - Vector3.up * (len * 0.5f);
            cables[i].localScale = new Vector3(0.03f, len, 0.03f);
        }
    }
}

/// <summary>
/// WINDOWS THAT SWITCH. RoofKit.Dress draws a third of the route's and the neighbours' amber windows in one of eight
/// shared materials (RoofLife.WindowMat); each group here is lit for most of a period of a few minutes and dark for the
/// rest, fading over a second and a half either way, so across a facade the lights go out and come back a block at a
/// time the way a building empties out. One SetColor per group per frame, and only while it is fading.
/// </summary>
public class WindowSwitch : MonoBehaviour
{
    /// The knobs: how long a change takes (the periods and the lit share are seeded in Init)
    public float fadeSeconds = 1.5f;
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly Color Dark = new Color(0.1f, 0.075f, 0.05f, 1f);   // a dark pane still catches the sky
    Material[] mats;
    Color lit;
    float[] period, duty, phase, cur;

    public void Init(Material[] materials, bool night)
    {
        mats = materials;
        lit = mats[0].GetColor(BaseColor);
        int n = mats.Length;
        period = new float[n]; duty = new float[n]; phase = new float[n]; cur = new float[n];
        for (int i = 0; i < n; i++)
        {
            period[i] = 70f + 150f * CityTextures.Hash01(i, 3, 501);
            duty[i] = (night ? 0.72f : 0.55f) + 0.2f * CityTextures.Hash01(i, 5, 502);
            phase[i] = CityTextures.Hash01(i, 7, 503);
            cur[i] = -1f;
        }
    }

    void Update()
    {
        if (mats == null) return;
        float t = Time.time;
        for (int i = 0; i < mats.Length; i++)
        {
            if (mats[i] == null) continue;
            float target = Mathf.Repeat(t / period[i] + phase[i], 1f) < duty[i] ? 1f : 0f;
            float c = cur[i] < 0f ? target : Mathf.MoveTowards(cur[i], target, Time.deltaTime / fadeSeconds);
            if (cur[i] >= 0f && Mathf.Abs(c - cur[i]) < 0.002f) continue;
            cur[i] = c;
            mats[i].SetColor(BaseColor, Color.Lerp(Dark, lit, c));
        }
    }

    void OnDestroy()
    {
        if (mats == null) return;
        foreach (var m in mats) if (m != null) m.SetColor(BaseColor, lit);
    }
}
