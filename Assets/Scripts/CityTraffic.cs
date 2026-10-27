using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// THE STREETS BELOW (rooftops alive, 2026-10-12). Period traffic on the city's avenues, seen from the roofs a hundred
/// metres up as motion and lamps: boxy SEDANS, TAXIS with a lit roof sign, a BUS with amber windows, TRUCKS, TRAMS on
/// their rails with lit windows going up and down an avenue (a dwell at each end, no bell), and WALKERS on the
/// pavements, dozens a block, both ways at a metre and a half a second. Nothing here is a GameObject: every kind is
/// ONE mesh (its boxes welded once) drawn with Graphics.RenderMeshInstanced from a matrix array this component fills
/// each frame, so hundreds of vehicles and walkers are twenty draw calls and no transforms. Lanes are polylines
/// (closed loops round the blocks, open avenues that wrap, ping-pong tram lines); every mover's place is a function
/// of the clock (phase + speed × time), so a lane asleep for a minute wakes where it should be. A lane whose box is
/// further than `sleepDistance` from the player (CityLife.Focus) is skipped whole. No per-frame allocation.
/// Headlamps cream, tail lamps red, a pale pool on the road ahead of every car: what a street is from a roof at night.
/// </summary>
public partial class CityTraffic : MonoBehaviour
{
    public enum Kind { Sedan, Taxi, Bus, Truck, Tram, Walker }
    const int Kinds = 6, Parts = 5;   // 0 body, 1 cream lamps, 2 red lamps, 3 amber windows, 4 the pool of light on the road ahead
    /// The knobs: how far a lane is driven and drawn from the player
    public float sleepDistance = 600f;
    /// Whose distance the lanes sleep by: the player (CityLife.Focus) unless told otherwise
    public Transform focus;
    bool unscaled;

    class Lane
    {
        public Vector3[] pts, fwd, right;
        public float[] cum;
        public float total, dwellLen;
        public bool pingPong;
        public Bounds box;
        public int first, count;
    }
    struct Mover { public int lane, seg; public Kind kind; public float s0, speed, side, dir; }

    readonly List<Lane> lanes = new List<Lane>();
    readonly List<Mover> building = new List<Mover>();
    Mover[] movers;
    readonly Mesh[,] meshes = new Mesh[Kinds, Parts];
    readonly Material[,] mats = new Material[Kinds, Parts];
    readonly RenderParams[,] rp = new RenderParams[Kinds, Parts];
    Matrix4x4[][] inst;
    int[] count;
    public int Vehicles { get; private set; }
    public int Walkers { get; private set; }
    public int LaneCount => lanes.Count;

    public static CityTraffic Create(Transform parent, bool unscaledTime)
    {
        var go = new GameObject("CityTraffic");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<CityTraffic>();
        t.unscaled = unscaledTime;
        t.BuildKinds();
        return t;
    }

    // ------------------------------------------------------------ lanes

    Lane AddLane(Vector3[] pts, bool pingPong, float dwellLen)
    {
        int n = pts.Length;
        var lane = new Lane { pts = pts, fwd = new Vector3[n], right = new Vector3[n], cum = new float[n], pingPong = pingPong, dwellLen = dwellLen };
        var box = new Bounds(pts[0], Vector3.zero);
        for (int i = 0; i < n; i++)
        {
            box.Encapsulate(pts[i]);
            if (i == 0) continue;
            Vector3 d = pts[i] - pts[i - 1];
            float len = d.magnitude;
            lane.cum[i] = lane.cum[i - 1] + len;
            lane.fwd[i - 1] = len > 1e-4f ? d / len : Vector3.forward;
            lane.right[i - 1] = Vector3.Cross(Vector3.up, lane.fwd[i - 1]);
        }
        lane.fwd[n - 1] = lane.fwd[n - 2];
        lane.right[n - 1] = lane.right[n - 2];
        lane.total = lane.cum[n - 1];
        box.Expand(30f);
        lane.box = box;
        lane.first = building.Count;
        lanes.Add(lane);
        return lane;
    }

    /// Vehicles down a lane (`twoWay`: both directions, each on its right-hand side) and walkers on both its pavements
    void Fill(Lane lane, float speed, float carsPer100m, float walkersPer100m, int seed, bool twoWay, float roadHalf, float pavement)
    {
        int li = lanes.Count - 1;
        int cars = Mathf.RoundToInt(lane.total / 100f * carsPer100m);
        float carSide = Mathf.Min(4.5f, roadHalf - 1.2f);
        for (int i = 0; i < cars; i++)
        {
            float h = CityTextures.Hash01(seed, i, 301), hk = CityTextures.Hash01(seed, i, 302);
            Kind k = hk < 0.55f ? Kind.Sedan : hk < 0.77f ? Kind.Taxi : hk < 0.9f ? Kind.Truck : Kind.Bus;
            float dir = twoWay && i % 2 == 1 ? -1f : 1f;
            building.Add(new Mover { lane = li, kind = k, s0 = (i + 0.3f * h) / cars * lane.total, speed = speed * (0.97f + 0.06f * CityTextures.Hash01(seed, i, 303)), side = dir * carSide, dir = dir });
        }
        Vehicles += cars;
        int walkers = Mathf.RoundToInt(lane.total / 100f * walkersPer100m);
        for (int i = 0; i < walkers; i++)
        {
            float h1 = CityTextures.Hash01(seed, i, 311), h2 = CityTextures.Hash01(seed, i, 312), h3 = CityTextures.Hash01(seed, i, 313);
            float side = (h3 < 0.5f ? -1f : 1f) * (pavement + (h1 - 0.5f) * 2.4f);
            building.Add(new Mover { lane = li, kind = Kind.Walker, s0 = (i + h1) / walkers * lane.total, speed = 1.2f + 0.4f * h2, side = side, dir = h2 < 0.5f ? -1f : 1f });
        }
        Walkers += walkers;
        lane.count = building.Count - lane.first;
    }

    /// A closed loop round the corners (x, z) at street level `y`: vehicles one way round it on the right-hand side,
    /// walkers on both pavements both ways
    public void Loop(Vector2[] corners, float y, float speed, float carsPer100m, float walkersPer100m, int seed, float roadHalf = 9f, float pavement = 11f)
    {
        var pts = new Vector3[corners.Length + 1];
        for (int i = 0; i < corners.Length; i++) pts[i] = new Vector3(corners[i].x, y, corners[i].y);
        pts[corners.Length] = pts[0];
        Fill(AddLane(pts, false, 0f), speed, carsPer100m, walkersPer100m, seed, false, roadHalf, pavement);
    }

    /// An open avenue from `a` to `b`: two-way vehicles that wrap at the ends, walkers on both pavements
    public void Avenue(Vector3 a, Vector3 b, float speed, float carsPer100m, float walkersPer100m, int seed, float roadHalf = 9f, float pavement = 11f)
        => Fill(AddLane(new[] { a, b }, false, 0f), speed, carsPer100m, walkersPer100m, seed, true, roadHalf, pavement);

    /// A tram line from `a` to `b`: `trams` cars on two tracks going up and back with `dwell` seconds at each end
    public void TramLine(Vector3 a, Vector3 b, float speed, int trams, float dwell, int seed)
    {
        var lane = AddLane(new[] { a, b }, true, dwell * speed);
        float cycle = 2f * (lane.total + 2f * lane.dwellLen);
        for (int i = 0; i < trams; i++)
            building.Add(new Mover { lane = lanes.Count - 1, kind = Kind.Tram, s0 = (i + 0.35f * CityTextures.Hash01(seed, i, 321)) / trams * cycle, speed = speed, side = (i % 2 == 0 ? -1f : 1f) * 1.4f, dir = 1f });
        lane.count = trams;
        Vehicles += trams;
    }

    /// Done laying: the arrays the frames run on
    public void Finish()
    {
        movers = building.ToArray();
        count = new int[Kinds];
        var per = new int[Kinds];
        foreach (var m in movers) per[(int)m.kind]++;
        inst = new Matrix4x4[Kinds][];
        var all = lanes.Count > 0 ? lanes[0].box : new Bounds(transform.position, Vector3.one);
        foreach (var l in lanes) all.Encapsulate(l.box);
        for (int k = 0; k < Kinds; k++)
        {
            inst[k] = new Matrix4x4[Mathf.Max(1, per[k])];
            for (int p = 0; p < Parts; p++)
                if (mats[k, p] != null)
                    rp[k, p] = new RenderParams(mats[k, p]) { worldBounds = all, shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false, layer = 0 };
        }
    }

    // ------------------------------------------------------------ the frame

    void Update()
    {
        if (movers == null) return;
        var f = focus != null ? focus : CityLife.Focus;
        if (f == null) return;
        Vector3 eye = f.position;
        float t = unscaled ? Time.unscaledTime : Time.time;
        float sleep2 = sleepDistance * sleepDistance;
        for (int k = 0; k < Kinds; k++) count[k] = 0;
        for (int li = 0; li < lanes.Count; li++)
        {
            var lane = lanes[li];
            if (lane.box.SqrDistance(eye) > sleep2) continue;
            int last = lane.cum.Length - 2;
            for (int i = lane.first; i < lane.first + lane.count; i++)
            {
                ref Mover m = ref movers[i];
                float s, facing;
                if (lane.pingPong)
                {
                    float cycle = 2f * (lane.total + 2f * lane.dwellLen);
                    float u = Mathf.Repeat(m.s0 + m.speed * t, cycle);
                    bool forward = u < cycle * 0.5f;
                    float p = forward ? u : cycle - u;
                    s = Mathf.Clamp(p - lane.dwellLen, 0f, lane.total);
                    facing = forward ? 1f : -1f;
                }
                else { s = Mathf.Repeat(m.s0 + m.dir * m.speed * t, lane.total); facing = m.dir; }
                int seg = m.seg;
                while (seg < last && s > lane.cum[seg + 1]) seg++;
                while (seg > 0 && s < lane.cum[seg]) seg--;
                m.seg = seg;
                Vector3 pos = lane.pts[seg] + lane.fwd[seg] * (s - lane.cum[seg]) + lane.right[seg] * m.side;
                int k = (int)m.kind;
                var arr = inst[k];
                if (count[k] >= arr.Length) continue;
                Vector3 r = lane.right[seg] * facing, fw = lane.fwd[seg] * facing;
                arr[count[k]++] = new Matrix4x4(new Vector4(r.x, r.y, r.z, 0f), new Vector4(0f, 1f, 0f, 0f), new Vector4(fw.x, fw.y, fw.z, 0f), new Vector4(pos.x, pos.y, pos.z, 1f));
            }
        }
        for (int k = 0; k < Kinds; k++)
        {
            int n = count[k];
            if (n == 0) continue;
            for (int part = 0; part < Parts; part++)
            {
                if (meshes[k, part] == null) continue;
                for (int start = 0; start < n; start += 1000)
                    Graphics.RenderMeshInstanced(rp[k, part], meshes[k, part], 0, inst[k], Mathf.Min(1000, n - start), start);
            }
        }
    }
}
