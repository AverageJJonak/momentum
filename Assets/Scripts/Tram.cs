using UnityEngine;

/// <summary>
/// THE TRAM. The district is sealed and empty and the trams are still running
/// their loop, on schedule, doors opening for no one. One runs out along its
/// track, brakes to a halt, waits, and runs back — a trapezoid of speed worked
/// out from the clock, so it is always where the clock says it is.
/// It is two things:
///   A HAZARD   stand in its way and it hits you, the way the plant's carts do.
///   A RIDE     its roof is solid, and standing on it you go with it: the
///              ground's friction works against the ROOF's speed and not the
///              world's (the same sum the plant's belts do — and only while
///              that friction is really acting, or a bunny hop on the roof
///              would be paid the tram's speed again at every landing).
///              Leave the roof and what it gave you is yours: nothing in the
///              air takes speed away. Jump off a tram at full tilt and you are
///              doing its twenty-odd metres a second, plus your own.
/// A kinematic body moved in the physics step, so the motor's casts find the
/// roof where it is.
/// </summary>
public class Tram : MonoBehaviour
{
    public const float Width = 3f, Height = 2.4f, Length = 17f, Skid = 0.35f;
    const float RoofY = Skid + Height;

    public MovementConfig config;
    public Vector3 a, b;
    public float topSpeed = 22f, accel = 9f, dwell = 2.5f, phase;

    Rigidbody body;
    Quaternion facing;
    Vector3 dir;
    float run, legTime;
    float hitReadyAt, hornAt;

    /// A tram on the track from `a` to `b` (rail level), `phase` of its cycle in
    public static Tram Create(Transform parent, MovementConfig config, Vector3 a, Vector3 b, float phase)
    {
        var go = new GameObject("Tram");
        go.transform.SetParent(parent, false);
        Quaternion rot = Quaternion.LookRotation((b - a).normalized, Vector3.up);
        go.transform.SetPositionAndRotation(a, rot);
        var t = go.AddComponent<Tram>();
        t.config = config;
        t.a = a;
        t.b = b;
        t.phase = phase;

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        var hull = go.AddComponent<BoxCollider>();
        hull.center = new Vector3(0f, Skid + Height * 0.5f, 0f);
        hull.size = new Vector3(Width, Height, Length);

        // The car: a white civic tram with the white gone grey, dark glass, a lit stripe, lamps at both ends (it never turns round)
        Transform tr = go.transform;
        var shell = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        shell.SetColor("_BaseColor", new Color(0.62f, 0.62f, 0.6f));
        shell.SetFloat("_Smoothness", 0.35f);
        Part(tr, new Vector3(0f, Skid + Height * 0.5f, 0f), new Vector3(Width, Height, Length), shell, "Hull");
        Part(tr, new Vector3(0f, Skid * 0.5f, 0f), new Vector3(Width * 0.7f, Skid, Length * 0.92f), LevelKit.Iron, "Bogie");
        Part(tr, new Vector3(0f, RoofY + 0.06f, 0f), new Vector3(Width * 0.5f, 0.12f, Length * 0.5f), LevelKit.Graphite, "RoofPlant");
        for (int s = -1; s <= 1; s += 2)
        {
            Part(tr, new Vector3(s * (Width * 0.5f + 0.01f), Skid + 1.6f, 0f), new Vector3(0.04f, 0.8f, Length * 0.86f), LevelKit.Dark, "Glass");
            Part(tr, new Vector3(s * (Width * 0.5f + 0.02f), Skid + 1.0f, 0f), new Vector3(0.04f, 0.12f, Length * 0.96f), LevelKit.Led, "Stripe");
            // The lips along the roof: enough to keep a rider from drifting off the side, solid, and they ride with it
            var lip = Part(tr, new Vector3(s * (Width * 0.5f - 0.1f), RoofY + 0.12f, 0f), new Vector3(0.2f, 0.24f, Length), LevelKit.Iron, "RoofLip");
            lip.AddComponent<BoxCollider>();
        }
        for (int e = -1; e <= 1; e += 2)
        {
            Part(tr, new Vector3(0f, Skid + 1.55f, e * (Length * 0.5f + 0.01f)), new Vector3(Width * 0.8f, 0.8f, 0.04f), LevelKit.Dark, "Screen");
            for (int s = -1; s <= 1; s += 2)
                Part(tr, new Vector3(s * 1.1f, Skid + 0.75f, e * (Length * 0.5f + 0.02f)), new Vector3(0.5f, 0.22f, 0.05f), LevelKit.Accent, "Lamp");
            LevelKit.Text(tr, tr.TransformPoint(new Vector3(0f, Skid + 2.18f, e * (Length * 0.5f + 0.04f))),
                rot * Quaternion.Euler(0f, e > 0 ? 180f : 0f, 0f), "9  ·  ON SCHEDULE", 0.035f, new Color(1.9f, 1.5f, 0.6f), "Blind", Width * 0.75f);
        }
        var l = LevelKit.Light(tr, tr.TransformPoint(new Vector3(0f, Skid + 1.5f, 0f)), 10f, 1.4f);
        l.color = new Color(0.82f, 0.9f, 1f);
        return t;
    }

    static GameObject Part(Transform parent, Vector3 local, Vector3 size, Material mat, string name)
    {
        var go = LevelKit.Prim(parent, parent.TransformPoint(local), size, mat, name, PrimitiveType.Cube, parent.rotation, true);
        return go;
    }

    void Start()
    {
        body = GetComponent<Rigidbody>();
        Vector3 d = b - a;
        run = d.magnitude;
        dir = run > 0.01f ? d / run : Vector3.forward;
        facing = Quaternion.LookRotation(dir, Vector3.up);
        Leg(1e9f, out _, out _, out legTime);
    }

    /// One leg of the run, `t` seconds in: how far along, how fast — and how long the whole leg takes
    void Leg(float t, out float s, out float v, out float total)
    {
        float top = Mathf.Max(0.1f, topSpeed), acc = Mathf.Max(0.1f, accel);
        float ta = top / acc, da = 0.5f * acc * ta * ta;
        if (2f * da > run) { ta = Mathf.Sqrt(run / acc); da = run * 0.5f; top = acc * ta; }   // too short a track to reach its speed
        float tc = (run - 2f * da) / top;
        total = 2f * ta + tc;
        if (t <= 0f) { s = 0f; v = 0f; }
        else if (t < ta) { s = 0.5f * acc * t * t; v = acc * t; }
        else if (t < ta + tc) { s = da + top * (t - ta); v = top; }
        else if (t < total) { float r = total - t; s = run - 0.5f * acc * r * r; v = acc * r; }
        else { s = run; v = 0f; }
    }

    void FixedUpdate()
    {
        if (body == null || run < 0.01f) return;
        float period = 2f * (legTime + dwell);
        float t = Mathf.Repeat(Time.time + phase * period, period);
        float s, v;
        bool back = t >= legTime + dwell;
        if (!back) Leg(t, out s, out v, out _);
        else
        {
            Leg(t - legTime - dwell, out float sb, out v, out _);
            s = run - sb;
            v = -v;
        }
        Vector3 pos = a + dir * s;
        body.MovePosition(pos);
        Vector3 vel = dir * v;

        var director = CombatDirector.Instance;
        var motor = director != null ? director.motor : null;
        if (motor == null || config == null) return;
        Vector3 feet = motor.CurrPosition;
        Vector3 local = Quaternion.Inverse(facing) * (feet - pos);
        bool over = Mathf.Abs(local.x) <= Width * 0.5f + 0.1f && Mathf.Abs(local.z) <= Length * 0.5f + 0.1f;

        // The ride: on the roof the ground's friction works against the roof. Only while that friction is acting.
        if (over && motor.Grounded && local.y > RoofY - 0.4f && local.y < RoofY + 0.7f)
        {
            if (!motor.FrictionDisabled && !motor.Dashing)
            {
                float f = motor.Sliding ? config.slideFriction : config.groundFriction;
                motor.AddImpulse(vel * (1f - Mathf.Exp(-f * Time.fixedDeltaTime)));
            }
            return;
        }

        // The hazard: in its way, at its height, while it is moving
        float speed = Mathf.Abs(v);
        bool inBody = local.y > -0.6f && local.y < RoofY - 0.5f
                      && Mathf.Abs(local.x) <= Width * 0.5f + 0.15f && Mathf.Abs(local.z) <= Length * 0.5f + 0.6f;
        if (inBody && speed > 3f && !motor.HasIFrames && Time.time >= hitReadyAt)
        {
            hitReadyAt = Time.time + 0.8f;
            Vector3 way = vel / speed;
            director.ReportPlayerHit((way + Vector3.up * 0.5f).normalized, config.factoryCartDamage);
            motor.AddImpulse(way * (speed * 0.6f) + Vector3.up * 7f);
        }
        // The horn, as it comes at you down the track
        float ahead = local.z * Mathf.Sign(v);
        if (speed > 3f && ahead > 0f && ahead < 30f && Mathf.Abs(local.x) < 3f && Mathf.Abs(local.y) < 4f && Time.time >= hornAt && GameFeel.Instance != null)
        {
            hornAt = Time.time + 3f;
            GameFeel.Instance.PlayCartHorn();
        }
    }
}
