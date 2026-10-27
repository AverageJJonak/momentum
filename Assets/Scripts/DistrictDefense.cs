using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE DISTRICT FIGHTS BACK. The cordon is not only a wall: the House's police
/// left their containment running inside it, and none of it is a unit you can
/// argue with — it is the MAP, and it is armed.
///   SWEEP LIGHTS   a searchlight on a mast, its pool walking the open ground.
///                  Stand in the pool too long and it has you: the light goes
///                  RED, follows you, and calls fire down on it.
///   MORTAR SHELLS  what it calls. A red ring on the ground that fills as the
///                  shell comes down — the same ring a demolition charge lays,
///                  so it already means what it means: be somewhere else. The
///                  blast is not particular: it takes the company's own units
///                  too, and a shell you walk away from under a pack is a gift.
///   HOVER MINES    dark shells hanging at chest height with an amber lamp.
///                  Come close and one arms — red, fast, a beat — and goes.
///                  At speed you are gone before it does; they are for the
///                  player who stops. They are not solid to YOU (a fist-sized
///                  thing that stops a thirty-metre-a-second run dead is a bug,
///                  not a hazard) but a cut or an arrow finds one and sets it
///                  off on purpose, one going off trips the next, and the blast
///                  throws whatever it catches — you included, if you wanted
///                  the speed more than the health.
/// (The sentry guns are units — Enemy, EnemyDamageControl — because they can be
/// killed and counted. These cannot: the lights are out of reach, the shells are
/// already in the air, and a mine only ever dies by doing its job.)
/// </summary>
public static class Blast
{
    static readonly HashSet<Enemy> hitSet = new HashSet<Enemy>();

    /// Everything inside `radius` of `at`: units, mines, and you
    public static void At(MovementConfig config, Vector3 at, float radius, float playerDamage, float unitDamage, float knock)
    {
        hitSet.Clear();
        foreach (Collider c in Physics.OverlapSphere(at, radius))
        {
            var e = c.GetComponentInParent<Enemy>();
            if (e != null && e.Alive && hitSet.Add(e)) e.Damage(unitDamage, KillMethod.Blast);
        }
        HoverMine.TripNear(at, radius * 1.8f, 0.14f);   // further than it hurts: a field goes up in a ripple, not all at once
        var director = CombatDirector.Instance;
        if (director != null && director.motor != null)
        {
            Vector3 chest = director.motor.CurrPosition + Vector3.up * 0.9f;
            float d = Vector3.Distance(chest, at);
            if (d <= radius && !director.motor.HasIFrames)
            {
                Vector3 dir = (chest - at).normalized;
                director.ReportPlayerHit(dir, playerDamage);
                director.motor.AddImpulse(dir * knock + Vector3.up * 4f);
            }
            // Heard — and felt — by how near it was: a shell two streets off is a thump, not an earthquake
            if (GameFeel.Instance != null) GameFeel.Instance.PlayBlastAt(1f - Mathf.Clamp01((d - radius) / 70f));
        }
        ShockRing.Spawn(at + Vector3.up * 0.05f, 0.4f, radius, 0.35f, false);
        MuzzleFlash.Spawn(at + Vector3.up * 0.4f, 3f);
    }

    static readonly RaycastHit[] groundHits = new RaycastHit[16];

    /// The WORLD under a point — the highest box within `reach` below it. A drone, a unit's hitbox and a mine are all
    /// under there too and none of them is ground: every solid piece of a level is a box, and nothing that flies is
    public static bool GroundUnder(Vector3 from, float reach, out Vector3 point)
    {
        int n = Physics.RaycastNonAlloc(from, Vector3.down, groundHits, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float best = float.NegativeInfinity;
        for (int i = 0; i < n; i++)
        {
            Collider c = groundHits[i].collider;
            if (!(c is BoxCollider || c is MeshCollider) || c.GetComponentInParent<Enemy>() != null) continue;
            if (groundHits[i].point.y > best) best = groundHits[i].point.y;
        }
        point = new Vector3(from.x, best, from.z);
        return best > float.NegativeInfinity;
    }

    /// An unlit see-through material off the keyword template (a build keeps the variant): blended, or added
    public static Material Sheet(Color c, bool additive)
    {
        var m = LevelKit.FromTemplate("UnlitTransparent", "Universal Render Pipeline/Unlit");
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", additive ? 2f : 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.SrcAlpha));
        m.SetInt("_DstBlend", (int)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
        m.SetInt("_ZWrite", 0);
        m.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return m;
    }
}

public class SweepLight : MonoBehaviour
{
    static Material beamMat, beamRedMat, poolMat, poolRedMat, lensMat, lensRedMat, housingMat;

    MovementConfig config;
    Vector3 lamp, centre;
    float reachX, reachZ, rate, phase, radius;
    Transform beam, pool;
    Renderer beamR, poolR, lensR;
    Vector3 spot;
    float caughtFor, angryUntil, nextShellAt;
    bool angry;

    /// A light at `lamp` whose pool walks the ground round `centre`, out to `reachX` / `reachZ` either way
    public static SweepLight Build(Transform parent, MovementConfig config, Vector3 lamp, Vector3 centre, float reachX, float reachZ, float rate, float phase, float radius = 8f)
    {
        EnsureMats();
        var go = new GameObject("SweepLight");
        go.transform.SetParent(parent, false);
        go.transform.position = lamp;
        var s = go.AddComponent<SweepLight>();
        s.config = config;
        s.lamp = lamp;
        s.centre = centre;
        s.reachX = reachX;
        s.reachZ = reachZ;
        s.rate = rate;
        s.phase = phase;
        s.radius = radius;
        s.spot = centre;
        LevelKit.Prim(go.transform, lamp, new Vector3(1.6f, 1.2f, 1.6f), housingMat, "LightHousing");
        s.lensR = LevelKit.Prim(go.transform, lamp + Vector3.down * 0.65f, new Vector3(1.1f, 0.12f, 1.1f), lensMat, "LightLens", PrimitiveType.Cylinder).GetComponent<Renderer>();
        s.beam = LevelKit.Prim(go.transform, lamp, Vector3.one, beamMat, "LightBeam").transform;
        s.beam.GetComponent<MeshFilter>().sharedMesh = Cone();
        s.beamR = s.beam.GetComponent<Renderer>();
        s.pool = LevelKit.Prim(go.transform, centre, new Vector3(radius * 2f, 0.02f, radius * 2f), poolMat, "LightPool", PrimitiveType.Cylinder).transform;
        s.poolR = s.pool.GetComponent<Renderer>();
        return s;
    }

    void Update()
    {
        if (config == null) return;
        float now = Time.time, dt = Time.deltaTime;
        var director = CombatDirector.Instance;
        var motor = director != null ? director.motor : null;

        // Where the pool is: walking its beat, or — once it has you — coming after you
        Vector3 want = centre + new Vector3(Mathf.Sin(now * rate + phase) * reachX, 0f, Mathf.Sin(now * rate * 0.73f + phase * 1.7f) * reachZ);
        bool wasAngry = angry;
        angry = now < angryUntil;
        if (angry && motor != null)
        {
            // It comes after you at fifteen metres a second — a walk never shakes it, a run does — and never far off its beat
            Vector3 p0 = motor.CurrPosition;
            want = new Vector3(Mathf.Clamp(p0.x, centre.x - reachX - 20f, centre.x + reachX + 20f), 0f, Mathf.Clamp(p0.z, centre.z - reachZ - 20f, centre.z + reachZ + 20f));
        }
        spot = Vector3.MoveTowards(spot, new Vector3(want.x, spot.y, want.z), (angry ? 15f : 60f) * dt);
        if (Blast.GroundUnder(new Vector3(spot.x, lamp.y, spot.z), lamp.y + 60f, out Vector3 ground)) spot.y = ground.y;

        // The beam is a cone from the lens to the pool: its mesh runs 0..1 down +Z and opens to radius 1 at the far end
        Vector3 to = spot - lamp;
        float len = Mathf.Max(0.5f, to.magnitude);
        Vector3 aim = to / len;
        beam.SetPositionAndRotation(lamp, Quaternion.LookRotation(aim, Mathf.Abs(aim.y) > 0.98f ? Vector3.forward : Vector3.up));
        beam.localScale = new Vector3(radius, radius, len);
        pool.position = spot + Vector3.up * 0.07f;
        if (angry != wasAngry)
        {
            beamR.sharedMaterial = angry ? beamRedMat : beamMat;
            poolR.sharedMaterial = angry ? poolRedMat : poolMat;
            lensR.sharedMaterial = angry ? lensRedMat : lensMat;
        }
        if (motor == null) return;

        // In the pool, in its sight: it takes a moment to be sure of you
        Vector3 p = motor.CurrPosition;
        float dx = p.x - spot.x, dz = p.z - spot.z;
        bool lit = dx * dx + dz * dz <= radius * radius && Mathf.Abs(p.y - spot.y) < 7f
                   && !Physics.Linecast(lamp + Vector3.down * 1.2f, p + Vector3.up * 1.2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (lit)
        {
            caughtFor += dt;
            if (angry) angryUntil = Mathf.Max(angryUntil, now + 1.2f);   // stay in it and it stays on you
            else if (caughtFor >= config.searchlightCatch)
            {
                caughtFor = 0f;
                angryUntil = now + config.searchlightAngry;
                nextShellAt = now + 0.25f;
                if (GameFeel.Instance != null) GameFeel.Instance.PlayTellRed();
            }
        }
        else caughtFor = Mathf.Max(0f, caughtFor - dt * 2f);

        if (angry && now >= nextShellAt)
        {
            // It shells what it can SEE. In the light that is you, led by most of a fuse — so a line held at speed is
            // walked up the street behind you, and anything slower has to turn. Out of the light it is the light itself:
            // outrun the pool and the barrage falls where you were
            nextShellAt = now + 0.8f;
            Vector3 v = motor.Velocity; v.y = 0f;
            Vector3 at = lit ? p + v * (config.mortarFuse * 0.85f) : spot;
            if (Blast.GroundUnder(at + Vector3.up * 6f, 40f, out Vector3 land)) MortarShell.Call(config, land);
        }
    }

    static Mesh cone;

    /// The beam's shape: a cone with no caps, its point (as wide as the lens) at the origin and its mouth, radius 1, at z = 1
    static Mesh Cone()
    {
        if (cone != null) return cone;
        const int Seg = 24;
        const float Tip = 0.07f;
        var verts = new Vector3[Seg * 2];
        var tris = new int[Seg * 6];
        for (int i = 0; i < Seg; i++)
        {
            float a = i * Mathf.PI * 2f / Seg;
            var rim = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            verts[i * 2] = rim * Tip;
            verts[i * 2 + 1] = rim + Vector3.forward;
            int j = (i + 1) % Seg;
            tris[i * 6] = i * 2; tris[i * 6 + 1] = i * 2 + 1; tris[i * 6 + 2] = j * 2;
            tris[i * 6 + 3] = j * 2; tris[i * 6 + 4] = i * 2 + 1; tris[i * 6 + 5] = j * 2 + 1;
        }
        cone = new Mesh { name = "SweepBeam", vertices = verts, triangles = tris };
        cone.RecalculateBounds();
        return cone;
    }

    static void EnsureMats()
    {
        if (beamMat != null) return;
        beamMat = Blast.Sheet(new Color(0.9f, 0.95f, 1f, 0.09f), false);   // the searchlight's cold white, the bible's one cold light
        beamRedMat = Blast.Sheet(new Color(1f, 0.16f, 0.1f, 0.16f), false);
        poolMat = Blast.Sheet(new Color(0.95f, 0.95f, 0.85f, 0.18f), false);
        poolRedMat = Blast.Sheet(new Color(1f, 0.14f, 0.1f, 0.3f), false);
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        lensMat = new Material(unlit); lensMat.SetColor("_BaseColor", new Color(2.2f, 2.2f, 2f));
        lensRedMat = new Material(unlit); lensRedMat.SetColor("_BaseColor", new Color(2.4f, 0.25f, 0.15f));
        housingMat = new Material(Shader.Find("Universal Render Pipeline/Lit")); housingMat.SetColor("_BaseColor", new Color(0.09f, 0.095f, 0.11f));
    }
}

public class MortarShell : MonoBehaviour
{
    static Material fillMat, areaMat, shellMat;

    MovementConfig config;
    float calledAt, landAt;
    Transform fill, shell;
    Vector3 ground;

    public static void Call(MovementConfig config, Vector3 groundPoint)
    {
        EnsureMats();
        var go = new GameObject("MortarShell") { layer = 2 };
        go.transform.position = groundPoint;
        var m = go.AddComponent<MortarShell>();
        m.config = config;
        m.ground = groundPoint;
        m.calledAt = Time.time;
        m.landAt = Time.time + Mathf.Max(0.2f, config.mortarFuse);
        float r = config.mortarRadius;
        LevelKit.Prim(go.transform, groundPoint + Vector3.up * 0.05f, new Vector3(r * 2f, 0.01f, r * 2f), areaMat, "MortarArea", PrimitiveType.Cylinder);
        m.fill = LevelKit.Prim(go.transform, groundPoint + Vector3.up * 0.07f, new Vector3(0.5f, 0.012f, 0.5f), fillMat, "MortarFill", PrimitiveType.Cylinder).transform;
        m.shell = LevelKit.Prim(go.transform, groundPoint + Vector3.up * 80f, new Vector3(0.35f, 0.35f, 2.6f), shellMat, "Shell", PrimitiveType.Cube, Quaternion.LookRotation(Vector3.down, Vector3.forward)).transform;
    }

    void Update()
    {
        float k = Mathf.Clamp01((Time.time - calledAt) / Mathf.Max(0.01f, landAt - calledAt));
        float r = Mathf.Lerp(0.5f, config.mortarRadius * 2f, k);
        fill.localScale = new Vector3(r, 0.012f, r);
        shell.position = ground + Vector3.up * Mathf.Lerp(80f, 0f, k * k);   // it is already falling when the ring appears
        if (k < 1f) return;
        Blast.At(config, ground + Vector3.up * 0.3f, config.mortarRadius, config.mortarDamage, config.blastDamage, config.blastKnockback);
        Destroy(gameObject);
    }

    static void EnsureMats()
    {
        if (fillMat != null) return;
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        fillMat = new Material(unlit); fillMat.SetColor("_BaseColor", new Color(1.8f, 0.2f, 0.12f));   // the ring a charge lays: it means the same thing
        areaMat = Blast.Sheet(new Color(1f, 0.12f, 0.08f, 0.22f), false);
        shellMat = new Material(unlit); shellMat.SetColor("_BaseColor", new Color(2.6f, 1.4f, 0.5f));
    }
}

public class HoverMine : MonoBehaviour, IStrikeTarget
{
    /// The layer mines live on (the built-in "Water", which nothing else in the game uses): every cast that means a
    /// cut, an arrow, a bolt or a line of sight still finds them, and the body — PlayerMotor's mask — does not
    public const int Layer = 4;

    static Material shellMat, lampMat, lampOffMat, redMat;

    MovementConfig config;
    Renderer lamp, shell;
    float fuseAt = -1f;
    bool done, lampOn;

    /// A mine hanging at chest height over `groundPos`
    public static HoverMine Lay(Transform parent, MovementConfig config, Vector3 groundPos)
    {
        EnsureMats();
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);   // it keeps its collider: a cut or an arrow has to be able to find it
        go.name = "HoverMine";
        go.layer = Layer;
        go.transform.SetParent(parent);
        go.transform.position = groundPos + Vector3.up * 1.15f;
        go.transform.localScale = Vector3.one * 0.6f;
        var m = go.AddComponent<HoverMine>();
        m.config = config;
        m.shell = go.GetComponent<MeshRenderer>();
        m.shell.sharedMaterial = shellMat;
        m.shell.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        // Sizes here are in the shell's own space (it is a 0.6 m ball): a lamp band round its waist, a spike out of each pole
        m.lamp = LevelKit.Prim(go.transform, go.transform.position, new Vector3(1.1f, 0.07f, 1.1f), lampMat, "MineBand", PrimitiveType.Cylinder).GetComponent<Renderer>();
        LevelKit.Prim(go.transform, go.transform.position + Vector3.down * 0.37f, new Vector3(0.16f, 0.42f, 0.16f), shellMat, "MineSpike");
        LevelKit.Prim(go.transform, go.transform.position + Vector3.up * 0.34f, new Vector3(0.1f, 0.3f, 0.1f), shellMat, "MineMast");
        return m;
    }

    static readonly List<HoverMine> laid = new List<HoverMine>();

    void OnEnable() { laid.Add(this); }
    void OnDisable() { laid.Remove(this); }

    /// Every mine within `radius` of a blast goes too, a beat later — and the ones near THOSE a beat after that
    public static void TripNear(Vector3 at, float radius, float seconds)
    {
        for (int i = 0; i < laid.Count; i++)
            if (laid[i] != null && (laid[i].transform.position - at).sqrMagnitude <= radius * radius) laid[i].Trip(seconds);
    }

    public bool Struck(Vector3 point, bool arrow)
    {
        if (done) return false;
        Trip(0.05f);
        return true;
    }

    /// Set it going: it blows `seconds` from now (sooner, if it already was)
    public void Trip(float seconds)
    {
        if (done) return;
        float at = Time.time + seconds;
        if (fuseAt < 0f || at < fuseAt) fuseAt = at;
    }

    void Update()
    {
        if (done || config == null) return;
        float now = Time.time;
        if (fuseAt < 0f)
        {
            // Dormant: a slow amber blink, and an eye out for you
            bool on = Mathf.Repeat(now * 0.7f + transform.position.x * 0.13f, 1f) < 0.2f;
            if (on != lampOn) { lampOn = on; lamp.sharedMaterial = on ? lampMat : lampOffMat; }
            var director = CombatDirector.Instance;
            if (director == null || director.motor == null) return;
            if ((director.motor.CurrPosition + Vector3.up * 0.9f - transform.position).sqrMagnitude > config.mineTrigger * config.mineTrigger) return;
            fuseAt = now + config.mineFuse;
            if (GameFeel.Instance != null) GameFeel.Instance.PlayChargeArm();   // what a demolition charge says when it arms: it means the same
            return;
        }
        // Armed: red, and fast
        bool red = Mathf.Repeat(now * 14f, 1f) < 0.5f;
        lamp.sharedMaterial = red ? redMat : lampOffMat;
        shell.sharedMaterial = red ? redMat : shellMat;
        if (now < fuseAt) return;
        done = true;
        Vector3 at = transform.position;
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;   // or its own blast finds it
        Blast.At(config, at, config.mineRadius, config.mineDamage, config.blastDamage, config.blastKnockback);
        Destroy(gameObject);
    }

    static void EnsureMats()
    {
        if (shellMat != null) return;
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        shellMat = new Material(lit); shellMat.SetColor("_BaseColor", new Color(0.1f, 0.105f, 0.12f)); shellMat.SetFloat("_Smoothness", 0.6f); shellMat.SetFloat("_Metallic", 0.5f);
        lampMat = new Material(unlit); lampMat.SetColor("_BaseColor", new Color(2.2f, 1.3f, 0.3f));      // amber: its own light
        lampOffMat = new Material(lit); lampOffMat.SetColor("_BaseColor", new Color(0.2f, 0.13f, 0.05f));
        redMat = new Material(unlit); redMat.SetColor("_BaseColor", new Color(2.4f, 0.25f, 0.15f));       // red: move
    }
}
