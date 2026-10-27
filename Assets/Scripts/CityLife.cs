using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE CITY IS AWAKE. Everything in the skyline that moves, run from ONE
/// Update over flat arrays — no component per car, no component per light.
/// Traffic runs its lanes and sweeps round its curves, trains follow the
/// same paths nose to tail, airships and aircraft cross above, dishes and
/// crane jibs turn on the roofs, cloud banks drift, floors of the near towers
/// go dark and come back, beacons blink. Nothing here is welded into the
/// baked meshes, because all of it moves — so it is kept deliberately few and
/// deliberately bright, which is how a city reads from a window a hundred
/// metres up.
/// </summary>
public class CityLife : MonoBehaviour, IWeldDynamic
{
    /// Everything this animator moves or switches: the weld leaves it alone (the cars, the flocks, the jibs, the
    /// searchlights, the strips and beacons it blinks)
    public void DynamicParts(List<Transform> into)
    {
        foreach (var m in traffic) if (m.t != null) into.Add(m.t);
        foreach (var p in paths) if (p.t != null) into.Add(p.t);
        foreach (var m in aircraft) if (m.t != null) into.Add(m.t);
        foreach (var s in spinners) if (s.t != null) into.Add(s.t);
        foreach (var r in flickers) if (r != null) into.Add(r.transform);
        foreach (var r in beacons) if (r != null) into.Add(r.transform);
        foreach (var r in strobes) if (r != null) into.Add(r.transform);
    }

    struct Mover
    {
        public Transform t;
        public Vector3 from, to;
        public float speed, phase;
        public Quaternion facing;
    }

    /// A mover on a polyline: `lengths[i]` is the distance along the path at `points[i]`
    struct PathMover
    {
        public Transform t;
        public Vector3[] points;
        public float[] lengths;
        public float total, speed, phase;
    }

    struct Spinner
    {
        public Transform t;
        public float degPerSec, phase;
        public Vector3 axis;
    }

    readonly List<Mover> traffic = new List<Mover>();
    readonly List<Mover> aircraft = new List<Mover>();
    readonly List<PathMover> paths = new List<PathMover>();
    readonly List<Spinner> spinners = new List<Spinner>();
    Renderer[] flickers = new Renderer[0];
    float[] flickerPeriod = new float[0], flickerPhase = new float[0], flickerDuty = new float[0];
    Renderer[] beacons = new Renderer[0];
    float[] beaconPhase = new float[0];
    Renderer[] strobes = new Renderer[0];
    bool unscaled;

    /// The menu runs on unscaled time (it pauses nothing); a level runs on the game's clock
    public void UseUnscaledTime() => unscaled = true;

    static Transform focus;
    static float focusAt = -10f;

    /// WHO THE CITY IS SEEN BY: the player when there is one (the capsule GameBootstrap names "Player"), else the main
    /// camera (the menu, a cutscene). Found lazily and retried once a second, because the player is spawned after the
    /// level is built; null until then. The traffic sleeps by distance from this, the roosts startle by it.
    public static Transform Focus
    {
        get
        {
            if (focus != null) return focus;
            if (Time.unscaledTime - focusAt < 1f) return null;
            focusAt = Time.unscaledTime;
            var p = GameObject.Find("Player");
            if (p != null) focus = p.transform;
            else if (Camera.main != null) focus = Camera.main.transform;
            return focus;
        }
    }

    /// One vehicle on a lane: it runs from → to, wraps, and faces the way it goes
    public Transform AddTraffic(Transform t, Vector3 from, Vector3 to, float speed, float phase)
    {
        Vector3 d = to - from;
        traffic.Add(new Mover
        {
            t = t, from = from, to = to, speed = speed, phase = phase,
            facing = d.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(d.normalized, Vector3.up) : Quaternion.identity,
        });
        return t;
    }

    /// One vehicle on a curve: it follows the points in order, wraps to the start, and faces along each leg
    public Transform AddPath(Transform t, Vector3[] points, float speed, float phase)
    {
        if (points == null || points.Length < 2) return t;
        var lengths = new float[points.Length];
        float total = 0f;
        for (int i = 1; i < points.Length; i++) { total += Vector3.Distance(points[i - 1], points[i]); lengths[i] = total; }
        if (total < 1e-3f) return t;
        paths.Add(new PathMover { t = t, points = points, lengths = lengths, total = total, speed = speed, phase = phase });
        return t;
    }

    public void AddAircraft(Transform t, Vector3 from, Vector3 to, float speed, float phase, Renderer strobe)
    {
        Vector3 d = to - from;
        aircraft.Add(new Mover
        {
            t = t, from = from, to = to, speed = speed, phase = phase,
            facing = d.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(d.normalized, Vector3.up) : Quaternion.identity,
        });
        if (strobe == null) return;
        var list = new List<Renderer>(strobes) { strobe };
        strobes = list.ToArray();
    }

    /// Something that turns in place: a dish, a crane's jib
    public void AddSpinner(Transform t, float degPerSec, Vector3 axis, float phase = 0f)
        => spinners.Add(new Spinner { t = t, degPerSec = degPerSec, axis = axis, phase = phase });

    /// A block of a tower's windows that goes dark now and then
    public void SetFlickers(List<Renderer> rends, List<float> periods, List<float> phases, List<float> duties)
    {
        flickers = rends.ToArray();
        flickerPeriod = periods.ToArray();
        flickerPhase = phases.ToArray();
        flickerDuty = duties.ToArray();
    }

    public void SetBeacons(List<Renderer> rends, List<float> phases)
    {
        beacons = rends.ToArray();
        beaconPhase = phases.ToArray();
    }

    void Update()
    {
        float t = unscaled ? Time.unscaledTime : Time.time;

        for (int i = 0; i < traffic.Count; i++)
        {
            var m = traffic[i];
            if (m.t == null) continue;
            float k = Mathf.Repeat(m.phase + t * m.speed, 1f);
            m.t.SetPositionAndRotation(Vector3.LerpUnclamped(m.from, m.to, k), m.facing);
        }

        for (int i = 0; i < paths.Count; i++)
        {
            var p = paths[i];
            if (p.t == null) continue;
            float d = Mathf.Repeat(p.phase + t * p.speed, 1f) * p.total;
            int seg = 1;
            while (seg < p.lengths.Length - 1 && p.lengths[seg] < d) seg++;
            float a = p.lengths[seg - 1], b = p.lengths[seg];
            float k = b > a ? (d - a) / (b - a) : 0f;
            Vector3 dir = p.points[seg] - p.points[seg - 1];
            p.t.SetPositionAndRotation(Vector3.Lerp(p.points[seg - 1], p.points[seg], k),
                dir.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(dir.normalized, Vector3.up) : p.t.rotation);
        }

        for (int i = 0; i < aircraft.Count; i++)
        {
            var m = aircraft[i];
            if (m.t == null) continue;
            float k = Mathf.Repeat(m.phase + t * m.speed, 1f);
            m.t.SetPositionAndRotation(Vector3.LerpUnclamped(m.from, m.to, k), m.facing);
        }
        for (int i = 0; i < strobes.Length; i++)
            if (strobes[i] != null) strobes[i].enabled = Mathf.Repeat(t * 1.3f + i * 0.37f, 1f) < 0.14f;

        for (int i = 0; i < spinners.Count; i++)
        {
            var s = spinners[i];
            if (s.t == null) continue;
            s.t.localRotation = Quaternion.AngleAxis(s.phase + t * s.degPerSec, s.axis);
        }

        // A floor goes dark for a while, then the lights come back: the building is emptying out
        for (int i = 0; i < flickers.Length; i++)
        {
            if (flickers[i] == null) continue;
            bool lit = Mathf.Repeat(flickerPhase[i] + t / flickerPeriod[i], 1f) > flickerDuty[i];
            if (flickers[i].enabled != lit) flickers[i].enabled = lit;
        }

        for (int i = 0; i < beacons.Length; i++)
        {
            if (beacons[i] == null) continue;
            bool on = Mathf.Repeat(t * 0.55f + beaconPhase[i], 1f) < 0.13f;
            if (beacons[i].enabled != on) beacons[i].enabled = on;
        }
    }
}
