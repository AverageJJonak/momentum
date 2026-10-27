using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy shot, v2 — HOMING, and loud about it. A glowing orange core with a
/// spinning ring, a bright trail, a point light — and NO hum, so the parry
/// crack owns the mix. It steers toward you until it gets close, then commits to a
/// straight line: that last stretch is the dodge/parry window. Three fates:
/// hits you (unless dash i-frames), passes close for a near-miss reset, or gets
/// PARRIED — reflected gold, supercharged, and homing onto its shooter.
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    /// Live projectiles, so the instant parry scan can reach them the frame
    /// the button is pressed
    public static readonly List<EnemyProjectile> Active = new List<EnemyProjectile>();

    MovementConfig config;
    Enemy source, homeTarget;
    Vector3 velocity;
    bool reflected, popped;
    bool inNearMissRange;
    bool nearMissAwarded;
    bool parryWarned;
    float bornAt;
    Transform core, ring;
    Renderer coreRenderer;
    Renderer[] bulbExtras;   // the filament, its post and the brass base: look only, hidden under a skin
    Light glow;
    static Material orbMat, warnMat, reflectedMat, ringMat, trailMat, bulbMat, filamentMat, baseMat2;
    static readonly Color OrbColor = new Color(1f, 0.5f, 0.15f);
    static readonly Color WarnColor = new Color(1f, 0.85f, 0.3f);

    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);

    /// A pull orb drags you toward its shooter instead of hurting you; it is parried the same way
    bool pull;
    float pullImpulse;
    float coreScale = 0.42f;
    float hitRadius = 0.7f;
    float turnRate = -1f, lifetime = 10f, damageOverride = -1f, knockback;
    Material baseMat;
    /// How wide it is to a hit (the arrow's sweep reads it), and whether it is yours now (parried: an arrow leaves it alone)
    public float Radius => hitRadius;
    public bool Reflected => reflected;

    /// Retune a fresh orb: speed, size, and whether it pulls rather than hits
    public void Configure(float speed, float scale, bool pulls, float pullStrength)
    {
        velocity = velocity.normalized * speed;
        coreScale = 0.42f * scale;
        hitRadius = 0.7f * scale;
        pull = pulls;
        pullImpulse = pullStrength;
        if (core != null) core.localScale = Vector3.one * coreScale;
        if (ring != null) ring.localScale = new Vector3(0.8f * scale, 0.02f, 0.8f * scale);
    }

    /// A heavier, dumber shot: its own turn rate, life, damage and knockback, and a skin (the Rigger's ball)
    public void Tune(float turnRateDeg, float life, float damage, float knock, Material skin)
    {
        turnRate = turnRateDeg;
        lifetime = life;
        damageOverride = damage;
        knockback = knock;
        if (skin != null)
        {
            baseMat = skin;
            if (coreRenderer != null) coreRenderer.sharedMaterial = skin;
            if (bulbExtras != null) foreach (var r in bulbExtras) if (r != null) r.enabled = false;   // an iron ball has no filament
        }
    }

    /// THE STATIONMASTER's shot: a punched brass TOKEN instead of a flashbulb — the core flattened into a disc with the
    /// punch hole through it, spinning as it flies, the bulb's extras hidden. Look only: the same size to a hit, the
    /// same gold in reach, the same parry and the same way home.
    bool token;
    Transform punch;
    static Material punchMat;
    public void TokenLook()
    {
        token = true;
        baseMat = BaseMat();
        if (coreRenderer != null) coreRenderer.sharedMaterial = parryWarned ? WarnMat() : baseMat;
        if (bulbExtras != null) foreach (var r in bulbExtras) if (r != null) r.enabled = false;
        if (core != null && punch == null)
            punch = Part(PrimitiveType.Cylinder, new Vector3(0.3f, 1.2f, 0.3f), punchMat ??= Lit(new Color(0.04f, 0.04f, 0.05f), 0.2f, 0.3f), "Punch", core, Vector3.zero, Quaternion.Euler(90f, 0f, 0f));
    }

    public static EnemyProjectile Spawn(MovementConfig config, Vector3 pos, Vector3 dir, Enemy source)
    {
        var go = new GameObject("EnemyProjectile") { layer = 2 }; // never blocks the motor's casts
        go.transform.position = pos;
        if (dir.sqrMagnitude > 1e-4f) go.transform.rotation = Quaternion.LookRotation(dir);   // the bulb's base trails (look only)
        var p = go.AddComponent<EnemyProjectile>();
        p.config = config;
        p.source = source;
        p.velocity = dir * config.projectileSpeed;
        p.bornAt = Time.time;
        p.Build();
        MuzzleFlash.Spawn(pos, 0.6f);
        return p;
    }

    void Build()
    {
        // THE FLASHBULB (the House overhaul): the core is a glass bulb with an amber-hot filament on its post inside and
        // a brass bayonet base behind, the base trailing (FixedUpdate turns the shot down its velocity). The extras are
        // children of the core, so the pulse and Configure's scale carry them; the bulb itself still goes gold in the
        // warn radius and cream-gold when it is yours, as every shot does, and a skinned shot (the Rigger's iron ball)
        // hides them. Sizes, homing, the trail, the light and the parry are exactly as they were.
        core = Part(PrimitiveType.Sphere, Vector3.one * 0.42f, BulbMat(), "Core");
        coreRenderer = core.GetComponent<Renderer>();
        baseMat = BulbMat();
        bulbExtras = new[]
        {
            Part(PrimitiveType.Sphere, Vector3.one * 0.42f, FilamentMat(), "Filament", core, new Vector3(0f, 0f, 0.05f)).GetComponent<Renderer>(),
            Part(PrimitiveType.Cube, new Vector3(0.06f, 0.06f, 0.7f), BaseMat(), "FilamentPost", core, new Vector3(0f, 0f, -0.25f)).GetComponent<Renderer>(),
            Part(PrimitiveType.Cylinder, new Vector3(0.5f, 0.18f, 0.5f), BaseMat(), "Base", core, new Vector3(0f, 0f, -0.55f), Quaternion.Euler(90f, 0f, 0f)).GetComponent<Renderer>(),
        };
        ring = Part(PrimitiveType.Cylinder, new Vector3(0.8f, 0.02f, 0.8f), RingMat(), "Ring");
        ring.localRotation = Quaternion.Euler(60f, 0f, 20f);

        var lightGo = new GameObject("Glow");
        lightGo.transform.SetParent(transform, false);
        glow = lightGo.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = OrbColor;
        glow.range = 7f;
        glow.intensity = 4f;
        glow.shadows = LightShadows.None;

        var trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.35f;
        trail.startWidth = 0.3f;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.05f;
        trail.alignment = LineAlignment.View;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.generateLightingData = false;
        trail.sharedMaterial = TrailMat();

    }

    Transform Part(PrimitiveType type, Vector3 scale, Material mat, string name, Transform parent = null, Vector3 localPos = default, Quaternion? localRot = null)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.layer = 2;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent != null ? parent : transform, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot ?? Quaternion.identity;   // (a default Quaternion is all zeros, never identity)
        go.transform.localScale = scale;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go.transform;
    }

    void Update()
    {
        // Spin + breathe so it never reads as a static ball, even mid-hitstop
        float now = Time.unscaledTime;
        if (ring != null) ring.Rotate(Vector3.up, 540f * Time.unscaledDeltaTime, Space.Self);
        float pulse = parryWarned ? 1f + 0.3f * Mathf.Sin(now * 22f) : 1f + 0.08f * Mathf.Sin(now * 9f);
        if (core != null)
        {
            core.localScale = (token ? new Vector3(1f, 1f, 0.22f) : Vector3.one) * (coreScale * pulse);   // a token is a disc
            if (token) core.localRotation = Quaternion.Euler(0f, 0f, now * 540f);                       // ...and it spins
        }
    }

    void FixedUpdate()
    {
        var director = CombatDirector.Instance;
        if (director == null || director.motor == null || Time.time - bornAt > lifetime)
        {
            Destroy(gameObject);
            return;
        }
        float dt = Time.fixedDeltaTime;
        if (Practice.Active && Practice.Frozen) return; // the range's console: shots hang in the air

        if (reflected)
        {
            // Home onto the shooter (or whoever was nearest) and hunt for a body to hit
            if (homeTarget != null && homeTarget.Alive)
                Steer(homeTarget.Center, config.reflectedProjectileTurnRate, dt);
            foreach (Collider c in Physics.OverlapSphere(transform.position, 0.6f))
            {
                var enemy = c.GetComponentInParent<Enemy>();
                if (enemy != null && enemy.Alive)
                {
                    enemy.Damage(config.reflectedProjectileDamage, KillMethod.ReflectedProjectile);
                    if (GameFeel.Instance != null) GameFeel.Instance.PlayProjectileImpact();
                    MuzzleFlash.Spawn(transform.position, 0.9f);
                    Destroy(gameObject);
                    return;
                }
            }
        }
        else
        {
            Vector3 chest = director.motor.CurrPosition + Vector3.up * 0.9f;
            float dist = Vector3.Distance(chest, transform.position);

            // HOMING: curve toward the chest until close, then commit to a straight line
            if (dist > config.projectileTrackingStopDistance)
                Steer(chest, turnRate >= 0f ? turnRate : config.projectileTurnRate, dt);

            // Telegraph: inside the warn radius the orb turns gold, pulses and chirps
            // once — the universal "you can parry this NOW"
            if (dist <= config.projectileParryWarnRadius)
            {
                if (!parryWarned)
                {
                    parryWarned = true;
                    coreRenderer.sharedMaterial = WarnMat();
                    glow.color = WarnColor;
                    glow.intensity = 6f;
                    if (GameFeel.Instance != null) GameFeel.Instance.PlayParryWarn();
                }
            }
            else if (parryWarned)
            {
                parryWarned = false;
                coreRenderer.sharedMaterial = baseMat != null ? baseMat : OrbMat();
                glow.color = OrbColor;
                glow.intensity = 4f;
            }

            // Parry beats everything — reflect back at the source, supercharged
            if (TryParryReflect(director.motor)) return;

            if (dist <= hitRadius)
            {
                if (director.motor.HasIFrames) { /* dashed through it */ }
                else
                {
                    if (pull)
                    {
                        // The announcement: you are drawn to the host
                        Vector3 toHost = source != null ? source.Center - chest : -velocity;
                        director.motor.AddImpulse(toHost.normalized * pullImpulse);
                        if (GameFeel.Instance != null) GameFeel.Instance.PlayHurt();
                    }
                    else
                    {
                        director.ReportPlayerHit(velocity.normalized, damageOverride >= 0f ? damageOverride : config.projectileDamage);
                        if (knockback > 0f) director.motor.AddImpulse(velocity.normalized * knockback + Vector3.up * 3f); // the Rigger's ball carries you
                    }
                    MuzzleFlash.Spawn(transform.position, 0.9f);
                    Destroy(gameObject);
                    return;
                }
            }
            else if (dist <= config.projectileNearMissRadius)
            {
                inNearMissRange = true;
            }
            else if (inNearMissRange && !nearMissAwarded && dist > config.projectileNearMissRadius + 0.5f)
            {
                // It came close and left without connecting — that's style
                nearMissAwarded = true;
                director.NearMiss();
            }
        }

        // World collision
        Vector3 step = velocity * dt;
        if (Physics.Raycast(transform.position, velocity.normalized, out RaycastHit hit,
                step.magnitude + 0.25f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (reflected)
            {
                var enemy = hit.collider.GetComponentInParent<Enemy>();
                if (enemy != null && enemy.Alive)
                    enemy.Damage(config.reflectedProjectileDamage, KillMethod.ReflectedProjectile);
            }
            if (GameFeel.Instance != null) GameFeel.Instance.PlayProjectileImpact();
            MuzzleFlash.Spawn(hit.point, 0.8f);
            Destroy(gameObject);
            return;
        }
        transform.position += step;
        if (velocity.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(velocity);   // the base trails the bulb: look only
    }

    /// Shot out of the air (an arrow through it): a flash, and gone
    public void Pop()
    {
        if (popped) return; // two arrows of one fan through the same orb: one pop
        popped = true;
        MuzzleFlash.Spawn(transform.position, 1.1f);
        if (GameFeel.Instance != null) GameFeel.Instance.PlayProjectileImpact();
        Destroy(gameObject);
    }

    /// Turn the velocity toward `target` at `turnRateDeg` per second, keeping speed
    void Steer(Vector3 target, float turnRateDeg, float dt)
    {
        Vector3 want = (target - transform.position).normalized * velocity.magnitude;
        velocity = Vector3.RotateTowards(velocity, want, turnRateDeg * Mathf.Deg2Rad * dt, 0f);
    }

    /// Reflects this orb if it's in range and the motor's parry window is open.
    /// Called per-tick from FixedUpdate AND synchronously at the press instant.
    public bool TryParryReflect(PlayerMotor motor)
    {
        if (reflected || motor == null) return false;
        Vector3 chest = motor.CurrPosition + Vector3.up * 0.9f;
        if (Vector3.Distance(chest, transform.position) > config.projectileParryRadius) return false;
        if (!motor.TryConsumeParry(transform.position)) return false;

        reflected = true;
        parryWarned = false;
        homeTarget = source != null && source.Alive ? source : NearestEnemy();
        Vector3 dir = homeTarget != null
            ? (homeTarget.Center - transform.position).normalized
            : -velocity.normalized;
        velocity = dir * config.projectileSpeed * config.reflectedProjectileSpeedMultiplier;
        coreRenderer.sharedMaterial = ReflectedMat();
        glow.color = WarnColor;
        glow.intensity = 7f;

        // Parrying an enemy's shot feeds your momentum too — once per guard, however many bolts it turns
        Vector3 travel = new Vector3(motor.Velocity.x, 0f, motor.Velocity.z);
        if (travel.sqrMagnitude > 0.5f && motor.ParryGuardConnects == 1)
            motor.AddImpulse(travel.normalized * config.parryEnemyMomentumBoost);
        return true;
    }

    Enemy NearestEnemy()
    {
        Enemy best = null;
        float bestDist = 60f;
        foreach (Collider c in Physics.OverlapSphere(transform.position, 60f))
        {
            var e = c.GetComponentInParent<Enemy>();
            if (e == null || !e.Alive) continue;
            float d = Vector3.Distance(e.Center, transform.position);
            if (d < bestDist) { bestDist = d; best = e; }
        }
        return best;
    }

    static Material OrbMat() => orbMat ??= Unlit(new Color(2.6f, 0.9f, 0.25f));        // HDR orange — blooms
    static Material FilamentMat() => filamentMat ??= Unlit(new Color(2.8f, 1.5f, 0.45f));   // the filament: amber-hot, a unit's own light (never the gold tell)
    static Material BaseMat() => baseMat2 ??= Lit(new Color(0.78f, 0.58f, 0.25f), 0.9f, 0.7f);  // the bayonet base: brass
    /// The glass: a warm, half-clear bulb round the filament (the same transparent template the kettle fence uses)
    static Material BulbMat()
    {
        if (bulbMat != null) return bulbMat;
        bulbMat = LevelKit.FromTemplate("UnlitTransparent", "Universal Render Pipeline/Unlit");
        bulbMat.SetColor("_BaseColor", new Color(1.3f, 0.9f, 0.5f, 0.42f));
        bulbMat.SetFloat("_Surface", 1f);
        bulbMat.SetFloat("_Blend", 0f);
        bulbMat.SetOverrideTag("RenderType", "Transparent");
        bulbMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        bulbMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        bulbMat.SetInt("_ZWrite", 0);
        bulbMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        bulbMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return bulbMat;
    }
    static Material Lit(Color c, float metallic, float smoothness)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Smoothness", smoothness);
        return m;
    }
    static Material WarnMat() => warnMat ??= Unlit(new Color(2.6f, 2.1f, 0.6f));       // gold: parry NOW
    static Material ReflectedMat() => reflectedMat ??= Unlit(new Color(2.8f, 2.5f, 1.4f));
    static Material RingMat() => ringMat ??= Unlit(new Color(1.6f, 0.6f, 0.2f));
    static Material TrailMat() => trailMat ??= Unlit(new Color(2f, 0.7f, 0.2f));

    static Material Unlit(Color c)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetColor("_BaseColor", c);
        return m;
    }
}

/// A bloom sphere that collapses in a tenth of a second — muzzle blasts and impacts.
public class MuzzleFlash : MonoBehaviour
{
    static Material flashMat;
    float born, size;

    public static void Spawn(Vector3 at, float size)
    {
        if (flashMat == null)
        {
            flashMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            flashMat.SetColor("_BaseColor", new Color(3f, 1.6f, 0.6f));
        }
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Flash";
        go.layer = 2;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.position = at;
        go.transform.localScale = Vector3.one * size;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = flashMat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.AddComponent<MuzzleFlash>().size = size;
    }

    void Awake() => born = Time.unscaledTime;

    void Update()
    {
        float t = (Time.unscaledTime - born) / 0.1f;
        if (t >= 1f) { Destroy(gameObject); return; }
        transform.localScale = Vector3.one * size * (1f - t * t);
    }
}
