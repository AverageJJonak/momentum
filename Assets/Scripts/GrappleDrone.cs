using UnityEngine;

/// <summary>
/// THE GRAPPLE DRONE. What used to be a gold ball hanging in the air is one of
/// the company's drones holding station: a white shell clamped round a GOLD
/// EYE, a rotor out either side, a strobe under it. The eye is what you aim
/// at — it is still the gold thing in the room, so it still reads as "line
/// goes here" from across a hall — and it is the same size to a grapple as the
/// ball was (one sphere collider, on the root, that never moves: a line stuck
/// to it stays where it stuck). Everything you SEE hangs off a child that bobs,
/// turns to keep its eye on you when you are near, and dips and flares when a
/// line bites into it. They are in every chapter: the city is full of things
/// that watch you, and you have been swinging from them the whole time.
/// </summary>
public class GrappleDrone : MonoBehaviour
{
    static Material eyeMat, eyeHotMat, shellMat, darkMat, bladeMat, strobeMat, strobeOffMat;

    Transform body, rotorL, rotorR;
    Renderer eye, strobe;
    float phase, spin, dip, dipVel, flare;
    bool hot, strobeOn;

    /// One drone holding station at `pos`: the grapple target the levels ask LevelKit.Orb for
    public static GameObject Build(Transform parent, Vector3 pos)
    {
        EnsureMats();
        var go = new GameObject("GrappleDrone");
        go.transform.SetParent(parent);
        go.transform.position = pos;
        var ball = go.AddComponent<SphereCollider>();
        ball.radius = 0.75f;   // what the gold ball was: as forgiving to aim at, as solid to land on
        go.AddComponent<GrappleTarget>();
        var d = go.AddComponent<GrappleDrone>();
        d.phase = Mathf.Abs(pos.x * 0.73f + pos.y * 1.31f + pos.z * 0.41f) % 6.283f;   // no two bob together

        var b = new GameObject("Body").transform;
        b.SetParent(go.transform, false);
        d.body = b;
        d.eye = Piece(b, new Vector3(0f, 0f, 0f), Vector3.one * 0.62f, eyeMat, "Eye", PrimitiveType.Sphere);
        Piece(b, new Vector3(0f, 0f, 0.3f), new Vector3(0.24f, 0.012f, 0.24f), darkMat, "Pupil", PrimitiveType.Cylinder).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Piece(b, new Vector3(0f, 0.36f, -0.02f), new Vector3(0.74f, 0.1f, 0.62f), shellMat, "ShellTop");
        Piece(b, new Vector3(0f, -0.36f, -0.02f), new Vector3(0.6f, 0.08f, 0.5f), darkMat, "ShellLow");
        Piece(b, new Vector3(0f, 0f, -0.35f), new Vector3(0.22f, 0.72f, 0.12f), shellMat, "Spine");
        Piece(b, new Vector3(0.2f, 0.56f, -0.2f), new Vector3(0.02f, 0.3f, 0.02f), darkMat, "Antenna");
        d.strobe = Piece(b, new Vector3(0f, -0.44f, 0.1f), Vector3.one * 0.09f, strobeMat, "Strobe", PrimitiveType.Sphere);
        for (int s = -1; s <= 1; s += 2)
        {
            Piece(b, new Vector3(s * 0.6f, 0.36f, 0f), new Vector3(0.5f, 0.05f, 0.08f), darkMat, "Boom");
            Piece(b, new Vector3(s * 0.88f, 0.37f, 0f), new Vector3(0.48f, 0.03f, 0.48f), darkMat, "Duct", PrimitiveType.Cylinder);
            var rotor = new GameObject("Rotor").transform;
            rotor.SetParent(b, false);
            rotor.localPosition = new Vector3(s * 0.88f, 0.42f, 0f);
            Piece(rotor, Vector3.zero, new Vector3(0.44f, 0.01f, 0.05f), bladeMat, "Blade");
            Piece(rotor, Vector3.zero, new Vector3(0.05f, 0.01f, 0.44f), bladeMat, "Blade");
            if (s < 0) d.rotorL = rotor; else d.rotorR = rotor;
        }
        return go;
    }

    static Renderer Piece(Transform parent, Vector3 local, Vector3 size, Material mat, string name, PrimitiveType type = PrimitiveType.Cube)
    {
        var go = LevelKit.Prim(parent, parent.TransformPoint(local), size, mat, name, type, parent.rotation);
        return go.GetComponent<Renderer>();
    }

    /// A line has bitten into it: the eye flares, the drone takes the weight
    public void Hooked()
    {
        flare = 1f;
        dipVel = -2.4f;
    }

    void Update()
    {
        if (body == null) return;
        float dt = Time.deltaTime, now = Time.time;

        // A district has seventy of these in the air: the ones you cannot make out do not bob, turn or spin
        var watcher = CombatDirector.Instance;
        bool far = watcher != null && watcher.motor != null && (watcher.motor.CurrPosition - transform.position).sqrMagnitude > 130f * 130f;
        if (far && flare <= 0f && Mathf.Abs(dip) < 0.002f && Mathf.Abs(dipVel) < 0.002f) return;

        // Bob, and a dip under load that springs back: a soft spring (7.7 rad/s), stepped with the step capped, so a long
        // frame cannot blow it up
        float k = 60f, c = 9f;
        dipVel += (-k * dip - c * dipVel) * Mathf.Min(dt, 0.033f);
        dip += dipVel * Mathf.Min(dt, 0.033f);
        body.localPosition = new Vector3(0f, Mathf.Sin(now * 1.3f + phase) * 0.1f + dip, 0f);

        // The eye keeps on you when you are near; otherwise it sweeps
        var director = CombatDirector.Instance;
        Quaternion want;
        if (director != null && director.motor != null && (director.motor.CurrPosition - transform.position).sqrMagnitude < 45f * 45f)
        {
            Vector3 to = director.motor.CurrPosition + Vector3.up * 1.4f - body.position;
            want = to.sqrMagnitude > 0.01f ? Quaternion.LookRotation(to.normalized, Vector3.up) : body.rotation;
        }
        else want = Quaternion.Euler(8f, Mathf.Sin(now * 0.35f + phase) * 70f + phase * 57.3f, 0f);
        body.rotation = Quaternion.Slerp(body.rotation, want, 1f - Mathf.Exp(-3.5f * dt));

        spin += (1500f + 1400f * flare) * dt;
        if (rotorL != null) rotorL.localRotation = Quaternion.Euler(0f, spin, 0f);
        if (rotorR != null) rotorR.localRotation = Quaternion.Euler(0f, -spin, 0f);

        flare = Mathf.MoveTowards(flare, 0f, dt * 2.2f);
        bool nowHot = flare > 0.3f;
        if (nowHot != hot) { hot = nowHot; eye.sharedMaterial = hot ? eyeHotMat : eyeMat; }
        bool blink = Mathf.Repeat(now * 0.8f + phase, 1f) < 0.12f;
        if (blink != strobeOn) { strobeOn = blink; strobe.sharedMaterial = blink ? strobeMat : strobeOffMat; }
    }

    static void EnsureMats()
    {
        if (eyeMat != null) return;
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        eyeMat = new Material(unlit); eyeMat.SetColor("_BaseColor", new Color(2.2f, 1.7f, 0.45f));       // gold: the line goes here
        eyeHotMat = new Material(unlit); eyeHotMat.SetColor("_BaseColor", new Color(3.2f, 2.8f, 1.6f));
        shellMat = new Material(lit); shellMat.SetColor("_BaseColor", new Color(0.9f, 0.91f, 0.93f)); shellMat.SetFloat("_Smoothness", 0.75f);
        darkMat = new Material(lit); darkMat.SetColor("_BaseColor", new Color(0.1f, 0.105f, 0.12f)); darkMat.SetFloat("_Smoothness", 0.5f);
        bladeMat = new Material(lit); bladeMat.SetColor("_BaseColor", new Color(0.55f, 0.57f, 0.6f)); bladeMat.SetFloat("_Smoothness", 0.4f);
        strobeMat = new Material(unlit); strobeMat.SetColor("_BaseColor", new Color(2.2f, 1.3f, 0.3f));  // amber: a unit's own light
        strobeOffMat = new Material(lit); strobeOffMat.SetColor("_BaseColor", new Color(0.18f, 0.12f, 0.05f));
    }
}
