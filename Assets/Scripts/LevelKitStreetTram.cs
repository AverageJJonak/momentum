using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE TRAM (district-grammar): a street's moving hazard on the Underground's train rule (TrainHazard): two cars of
/// oxblood over cream on baked rails, up and down an avenue with a dwell at each end, a bell before it leaves.
/// Contact with a car = 30 and a throw along its travel and up; a pass within a hand of its flank is a NEAR MISS; on
/// its roof you ride (it is solid: a lip 3.2 m up, brass along its edge). Its cars move under this component
/// (IWeldDynamic: the weld leaves them), everything else is still. No per-frame allocation.
/// </summary>
public static partial class LevelKit
{
    /// A tram line from `a` to `b` at street level: rails baked, `cars` trams spaced along the cycle, `speed` m/s,
    /// `dwell` seconds at each end
    public static TramHazard Tram(Transform parent, Vector3 a, Vector3 b, float speed, int cars, float dwell, int seed)
    {
        var baker = new MeshBaker();
        CityTraffic.Rails(baker, RivetIron, a, b);
        baker.Flush(parent, "TramRails");
        var host = new GameObject("TramLine");
        host.transform.SetParent(parent);
        host.transform.position = (a + b) * 0.5f;
        cars = Mathf.Clamp(cars, 1, 3);
        var fleet = new Transform[cars];
        for (int i = 0; i < cars; i++) fleet[i] = TramCar(host.transform, i);
        var hz = host.AddComponent<TramHazard>();
        hz.Init(a, b, fleet, speed, dwell, seed);
        return hz;
    }

    /// One car: a solid body (ride its roof), cream window band, a cream lamp at each end, a brass roof lip, a pole
    static Transform TramCar(Transform host, int index)
    {
        var car = new GameObject("TramCar" + index).transform;
        car.SetParent(host);
        Slab(car, Vector3.up * 1.75f, new Vector3(2.5f, 2.9f, 13f), Oxblood, "Tram");   // solid: ride its roof
        Prim(car, Vector3.up * 3.3f, new Vector3(2.3f, 0.2f, 12.6f), Charcoal, "TramRoof");
        for (int s = -1; s <= 1; s += 2)
        {
            Prim(car, new Vector3(s * 1.27f, 2.1f, 0f), new Vector3(0.04f, 0.9f, 11.6f), WindowAmber, "TramGlass");
            Prim(car, new Vector3(0f, 1.1f, s * 6.52f), new Vector3(0.3f, 0.3f, 0.08f), LampCream, "TramLamp");
            Prim(car, new Vector3(s * 1.2f, 3.42f, 0f), new Vector3(0.1f, 0.06f, 12.6f), Brass, "TramLip");
        }
        Prim(car, new Vector3(0f, 4.4f, 0f), new Vector3(0.08f, 2f, 0.08f), Bronze, "TramPole", PrimitiveType.Cylinder);
        Prim(car, new Vector3(0f, 0.35f, 4f), new Vector3(2f, 0.7f, 2f), RivetIron, "TramBogie");
        Prim(car, new Vector3(0f, 0.35f, -4f), new Vector3(2f, 0.7f, 2f), RivetIron, "TramBogie");
        return car;
    }
}

/// A tram up and down a line: `cars` spaced round a ping-pong cycle, each dwelling `dwell` seconds at the ends, a
/// bell before it moves. Contact = 30 and a throw; a pass is a near miss; its roof carries you
public class TramHazard : MonoBehaviour, IWeldDynamic
{
    public const float Damage = 30f, ThrowAlong = 14f, ThrowUp = 6f, CarLen = 13f;
    Vector3 south, dirN;
    Quaternion rotN, invN;
    Transform[] cars;
    float[] phase;
    float len, speed, dwell, cycle, hitReadyAt;
    bool[] moving;
    int[] side;
    AudioSource bell;

    public void DynamicParts(List<Transform> into) { foreach (var c in cars) if (c != null) into.Add(c); }

    public void Init(Vector3 a, Vector3 b, Transform[] fleet, float runSpeed, float dwellSeconds, int seed)
    {
        south = a;
        len = (b - a).magnitude;
        dirN = (b - a) / Mathf.Max(0.01f, len);
        rotN = Quaternion.LookRotation(dirN, Vector3.up);
        invN = Quaternion.Inverse(rotN);
        cars = fleet;
        speed = Mathf.Max(2f, runSpeed);
        dwell = Mathf.Max(1f, dwellSeconds);
        float run = Mathf.Max(1f, len - CarLen) / speed;
        cycle = 2f * (run + dwell);
        phase = new float[cars.Length];
        moving = new bool[cars.Length];
        side = new int[cars.Length];
        for (int i = 0; i < cars.Length; i++) phase[i] = (i + 0.37f * CityTextures.Hash01(seed, i, 77)) / cars.Length * cycle;
        bell = UndergroundRun.Speaker(gameObject, "bell", 60f, false, 0.5f, 1.1f);
        Tick(Time.time, true);
    }

    void Update() { if (Time.deltaTime > 0f) Tick(Time.time, false); }

    /// Where each car's south end is this frame: a dwell, a run north, a dwell, a run south
    void Tick(float t, bool first)
    {
        float run = Mathf.Max(1f, len - CarLen) / speed;
        bool have = UndergroundRun.Resolve(out var motor, out var director, out Vector3 feet) && director != null && !first;
        for (int i = 0; i < cars.Length; i++)
        {
            float u = Mathf.Repeat(t + phase[i], cycle);
            float s;
            bool run1 = u >= dwell && u < dwell + run, run2 = u >= 2f * dwell + run && u < cycle;
            if (u < dwell) s = 0f;
            else if (run1) s = (u - dwell) * speed;
            else if (u < 2f * dwell + run) s = len - CarLen;
            else s = len - CarLen - (u - 2f * dwell - run) * speed;
            bool wasMoving = moving[i];
            moving[i] = run1 || run2;
            if (moving[i] && !wasMoving && !first && bell != null) { bell.transform.position = cars[i].position; UndergroundRun.Shot(bell, "bell", 0.5f, 1.1f); }
            Vector3 p = south + dirN * (s + CarLen * 0.5f);
            cars[i].SetPositionAndRotation(p, rotN);
            if (!have || !moving[i]) continue;
            // The contact rule: inside the car's box at its height = a hit and a throw along its travel; past its
            // flank within a hand = a near miss
            Vector3 travel = run1 ? dirN : -dirN;
            Vector3 local = invN * (feet - p);
            bool band = local.y > -0.6f && local.y < 2.9f;
            if (!band) continue;
            bool inside = Mathf.Abs(local.x) <= 1.4f && Mathf.Abs(local.z) <= CarLen * 0.5f + 0.4f;
            if (inside && !motor.HasIFrames && t >= hitReadyAt)
            {
                hitReadyAt = t + 1.2f;
                director.ReportPlayerHit((travel + Vector3.up * 0.4f).normalized, Damage);
                motor.AddImpulse(travel * ThrowAlong + Vector3.up * ThrowUp);
                var feel = GameFeel.Instance;
                if (feel != null) { feel.Sparks(feet + Vector3.up * 1f, 20, false); feel.PlayClank(1f); }
                continue;
            }
            float along = Vector3.Dot(feet - p, travel);
            int sg = along > 0f ? 1 : -1;
            if (side[i] > 0 && sg < 0 && Mathf.Abs(local.x) > 1.4f && Mathf.Abs(local.x) < 2.5f) director.NearMiss();
            if (Mathf.Abs(along) < CarLen * 0.5f + 3f) side[i] = sg;
        }
    }
}
