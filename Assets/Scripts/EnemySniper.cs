using UnityEngine;

/// <summary>
/// THE SENTINEL IS A SNIPER. The hovering unit with the shoulder cannon no
/// longer lobs homing orbs: it puts a RED LASER on you. The dot chases your
/// chest at a fixed speed — move slower than that and it catches you and sits
/// on you; move faster and it never does. While the dot is on you a LOCK
/// builds and you HEAR it — a hum on the muzzle that rises and swells as it
/// closes, the sniper lasering in; when it fills, the beam and the body go
/// GOLD for one short beat and the hum screams, and then the shot: hitscan, instant, where the dot is. Too
/// slow, and it takes most of your health. Out of the dot by then, and it goes
/// into the wall behind where you were — a near miss, if it was close.
/// The parry: ANY parry inside the gold beat answers it, the same rule as a
/// melee windup, so gold always means one thing. Land it and the shot goes
/// back down its own beam: the sniper dies of it, and the parry heals and resets like any
/// other. Red = the danger you outrun; gold = the one instant you can answer.
/// Line of sight is required: break it and the lock fades. Nothing here
/// throws a projectile, so EnemyProjectile's rules do not apply to it.
/// </summary>
public partial class Enemy
{
    enum SniperPhase { Idle, Track, Tell, Cooldown }

    LineRenderer laser;
    Transform laserDot;
    Renderer laserDotR;
    Light laserLamp;
    Vector3 laserAim;
    float lock01;
    float sniperTellAt = -1f, sniperReadyAt;
    AudioSource whine;
    float whineVol, whinePitch = 0.55f;
    SniperPhase sniperPhase;
    static Material laserRedMat, laserGoldMat, laserHotMat;

    static Material SniperUnlit(Color c)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetColor("_BaseColor", c);
        return m;
    }

    void EnsureLaser()
    {
        if (laser != null) return;
        laserRedMat ??= SniperUnlit(new Color(2.6f, 0.22f, 0.15f));   // the danger red
        laserGoldMat ??= SniperUnlit(new Color(2.6f, 2.1f, 0.6f));    // parry NOW
        laserHotMat ??= SniperUnlit(new Color(3f, 2.6f, 2.2f));       // the shot
        var go = new GameObject("SniperLaser") { layer = 2 };
        go.transform.SetParent(transform, false);
        laser = go.AddComponent<LineRenderer>();
        laser.useWorldSpace = true;
        laser.positionCount = 2;
        laser.startWidth = laser.endWidth = 0.035f;
        laser.material = laserRedMat;
        laser.alignment = LineAlignment.View;
        laser.numCapVertices = 2;
        laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        laser.receiveShadows = false;
        laser.generateLightingData = false;
        laser.enabled = false;
        var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dot.name = "SniperDot";
        dot.layer = 2;
        Object.DestroyImmediate(dot.GetComponent<Collider>());
        dot.transform.SetParent(transform, false);
        dot.transform.localScale = Vector3.one * 0.16f;
        laserDotR = dot.GetComponent<MeshRenderer>();
        laserDotR.sharedMaterial = laserRedMat;
        laserDotR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        laserDot = dot.transform;
        dot.SetActive(false);
        var lampGo = new GameObject("SniperLamp");
        lampGo.transform.SetParent(rig != null && rig.muzzle != null ? rig.muzzle : transform, false);
        laserLamp = lampGo.AddComponent<Light>();
        laserLamp.type = LightType.Point;
        laserLamp.color = new Color(1f, 0.2f, 0.12f);
        laserLamp.range = 4f;
        laserLamp.intensity = 0f;
        laserLamp.shadows = LightShadows.None;
        // The whine used to be the force-field hum, swelling with the lock: the last electronic noise in the game, and
        // gone with the rest. The laser on you and the lock click (PlayLockOn) are the cue; SniperAudio idles on a null clip
        whine = lampGo.AddComponent<AudioSource>();
        whine.clip = null;
        whine.loop = true;
        whine.playOnAwake = false;
        whine.spatialBlend = 1f;
        whine.dopplerLevel = 0f;
        whine.rolloffMode = AudioRolloffMode.Linear;
        whine.minDistance = 8f;
        whine.maxDistance = config.sniperRange + 12f;
        whine.volume = 0f;
        whine.pitch = whinePitch;
    }

    /// The laser replaced the HOMING ORB, and only that. The Sentinel used to plink tracking shots at you and had no
    /// character of its own; the Blaster's lobbed charges and the Rigger's ball on a chain are their own weapons and
    /// their own reads, so they keep them. One rule per threat, not one threat.
    public bool Snipes => role == EnemyRole.Turret;

    Vector3 SniperMuzzle => rig != null && rig.muzzle != null ? rig.muzzle.position : Center;

    void SniperTick(CombatDirector director, Vector3 chest, float dist)
    {
        EnsureLaser();
        float now = Time.time, dt = Time.fixedDeltaTime;
        var motor = director.motor;
        Vector3 from = SniperMuzzle;

        if (now < staggeredUntil)
        {
            SetSkin(StaggerMat());
            LaserOff();
            lock01 = 0f;
            sniperPhase = SniperPhase.Idle;
            return;
        }

        if (sniperPhase == SniperPhase.Cooldown)
        {
            LaserOff();
            SetSkin(normalMat);
            if (now >= sniperReadyAt) sniperPhase = SniperPhase.Idle;
            return;
        }

        if (sniperPhase == SniperPhase.Tell)
        {
            // The gold beat: the dot keeps chasing, the body is gold, and then it fires where the dot is.
            // ANY parry inside the beat answers it — the same rule as a melee windup, so gold always means the
            // same thing. You do not have to cover the firing instant; you have to answer the tell.
            TrackDot(chest, dt);
            DrawLaser(from, laserGoldMat, 0.07f);
            if (motor != null && motor.ParryWindowOpen && motor.TryConsumeParry(from, "RETURN"))
            {
                Returned(chest, from);
                return;
            }
            if (now >= sniperTellAt + config.sniperTell) Fire(director, motor, chest, from);
            return;
        }

        // Idle / Track: it needs range and a line to you
        bool sees = dist <= config.sniperRange && HasLineOfSight(from, chest, dist);
        if (!sees)
        {
            lock01 = Mathf.MoveTowards(lock01, 0f, dt / Mathf.Max(0.05f, config.sniperLockDecay));
            LaserOff();
            SetSkin(normalMat);
            sniperPhase = SniperPhase.Idle;
            return;
        }
        if (sniperPhase == SniperPhase.Idle)
        {
            // The dot comes down onto you from above, so the first thing you see is it arriving
            sniperPhase = SniperPhase.Track;
            laserAim = chest + Vector3.up * 2.5f;
            lock01 = 0f;
        }
        SetSkin(normalMat);
        TrackDot(chest, dt);
        bool onYou = (laserAim - chest).magnitude <= config.sniperLockRadius;
        lock01 = onYou
            ? Mathf.MoveTowards(lock01, 1f, dt / Mathf.Max(0.05f, config.sniperLockTime))
            : Mathf.MoveTowards(lock01, 0f, dt / Mathf.Max(0.05f, config.sniperLockDecay));
        DrawLaser(from, laserRedMat, 0.035f + 0.05f * lock01);
        if (lock01 >= 1f)
        {
            sniperPhase = SniperPhase.Tell;
            sniperTellAt = now;
            SetSkin(WindupMat()); // the whine screams for this beat: that is the cue
        }
    }

    /// The dot chases your chest at the tracking speed: slower than it and it catches you, faster and it never does
    void TrackDot(Vector3 chest, float dt) => laserAim = Vector3.MoveTowards(laserAim, chest, config.sniperTrackSpeed * dt);

    /// The shot: instant, where the dot is. A parry whose window covers this instant sends it back down the beam
    void Fire(CombatDirector director, PlayerMotor motor, Vector3 chest, Vector3 from)
    {
        var feel = GameFeel.Instance;
        Vector3 dir = laserAim - from;
        float len = dir.magnitude;
        dir = len > 0.01f ? dir / len : transform.forward;
        Vector3 end = laserAim;
        if (Physics.Raycast(from, dir, out RaycastHit hit, len, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && hit.collider.GetComponentInParent<PlayerMotor>() == null && hit.collider.GetComponentInParent<Enemy>() != this)
            end = hit.point; // the wall it goes into
        float miss = (laserAim - chest).magnitude;
        bool onYou = miss <= config.sniperHitRadius;
        recoil = 1f;
        MuzzleFlash.Spawn(from, 0.9f);
        if (feel != null) feel.PlaySniperShot();

        sniperPhase = SniperPhase.Cooldown;
        sniperReadyAt = Time.time + config.sniperCooldown;
        lock01 = 0f;
        LaserOff();
        SetSkin(normalMat);

        // A parry that lands on the shot itself still answers it, even from outside the dot: you read it either way
        if (motor != null && motor.ParryWindowOpen && motor.TryConsumeParry(from, "RETURN"))
        {
            Returned(chest, from);
            return;
        }
        LaserFlash.Spawn(from, end, laserHotMat, 0.14f, 0.12f);
        if (onYou && motor != null && !motor.HasIFrames)
        {
            director.ReportPlayerHit(dir, config.sniperDamage);
            motor.AddImpulse(dir * config.sniperKnockback + Vector3.up * 2f);
            MuzzleFlash.Spawn(chest, 1.2f);
            if (feel != null) feel.Sparks(chest, 24, false);
            return;
        }
        // Into the wall behind where you were: a near miss, if it was close
        MuzzleFlash.Spawn(end, 0.7f);
        if (feel != null) feel.Sparks(end, 10, false);
        if (miss <= config.projectileNearMissRadius + config.sniperHitRadius) director.NearMiss();
    }

    /// Answered: the shot goes back down its own beam, gold, from you to it — and the sniper dies of it. The laser
    /// goes out and it drops into cooldown, so a returned shot costs it the same beat a fired one does
    void Returned(Vector3 chest, Vector3 from)
    {
        var feel = GameFeel.Instance;
        sniperPhase = SniperPhase.Cooldown;
        sniperReadyAt = Time.time + config.sniperCooldown;
        lock01 = 0f;
        LaserOff();
        LaserFlash.Spawn(chest, from, laserGoldMat, 0.18f, 0.18f);
        MuzzleFlash.Spawn(Center, 2.4f);
        if (feel != null) { feel.Sparks(Center, 50, true); feel.Sparks(from, 20, false); }
        Damage(float.MaxValue, KillMethod.ReflectedProjectile);
    }

    bool HasLineOfSight(Vector3 from, Vector3 chest, float dist)
    {
        Vector3 d = chest - from;
        float len = d.magnitude;
        if (len < 0.01f) return true;
        if (!Physics.Raycast(from, d / len, out RaycastHit hit, len, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return true;
        if (hit.collider.GetComponentInParent<PlayerMotor>() != null) return true;
        if (hit.collider.GetComponentInParent<Enemy>() == this) return true; // its own barrel
        return hit.distance >= len - 0.5f;
    }

    /// The beam from the muzzle to the dot, stopped by the world; the dot at its end; the lamp on the muzzle
    void DrawLaser(Vector3 from, Material mat, float width)
    {
        Vector3 d = laserAim - from;
        float len = d.magnitude;
        Vector3 end = laserAim;
        if (len > 0.01f && Physics.Raycast(from, d / len, out RaycastHit hit, len, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && hit.collider.GetComponentInParent<PlayerMotor>() == null && hit.collider.GetComponentInParent<Enemy>() != this)
            end = hit.point;
        laser.enabled = true;
        laser.SetPosition(0, from);
        laser.SetPosition(1, end);
        if (laser.material != mat) laser.material = mat;
        laser.startWidth = laser.endWidth = width;
        laserDot.gameObject.SetActive(true);
        laserDot.position = end;
        laserDot.localScale = Vector3.one * (0.16f + 0.16f * lock01 + (mat == laserGoldMat ? 0.1f : 0f));
        if (laserDotR.sharedMaterial != mat) laserDotR.sharedMaterial = mat;
        if (laserLamp != null) laserLamp.intensity = 0.8f + 2f * lock01;
    }

    /// The whine, every frame (Enemy.Update): swelling and rising with the lock while the laser is on you, a scream
    /// through the gold beat, silent the instant the laser goes off — and silent through the pause
    void SniperAudio(float dt)
    {
        if (whine == null || whine.clip == null) return;
        bool on = Alive && laser != null && laser.enabled && !PauseMenu.IsPaused;
        float wantVol = !on ? 0f : sniperPhase == SniperPhase.Tell ? 0.9f : Mathf.Lerp(0.14f, 0.6f, lock01);
        float wantPitch = !on ? whinePitch : sniperPhase == SniperPhase.Tell ? 2.2f : Mathf.Lerp(0.55f, 1.75f, lock01 * lock01);
        whineVol = Mathf.MoveTowards(whineVol, wantVol, dt * (wantVol > whineVol ? 6f : 12f));
        whinePitch = Mathf.MoveTowards(whinePitch, wantPitch, dt * 3f);
        whine.volume = whineVol * GameSettings.SfxVolume;
        whine.pitch = whinePitch + (on ? Mathf.Sin(Time.unscaledTime * 38f) * 0.02f * lock01 : 0f);
        if (whineVol > 0.002f && !whine.isPlaying) whine.Play();
        else if (whineVol <= 0.002f && whine.isPlaying) whine.Stop();
    }

    void LaserOff()
    {
        if (laser == null) return;
        // The sound cuts with the beam: the crack of the shot, or the dark of a lost line, is the next thing heard
        if (whine != null && whine.isPlaying) { whineVol = 0f; whine.volume = 0f; whine.Stop(); }
        laser.enabled = false;
        laserDot.gameObject.SetActive(false);
        if (laserLamp != null) laserLamp.intensity = 0f;
    }

    /// Dead: the laser goes with it
    void SniperOff()
    {
        LaserOff();
        sniperPhase = SniperPhase.Idle;
        lock01 = 0f;
    }

    /// Back on the range: cold
    void SniperReset()
    {
        SniperOff();
        sniperReadyAt = 0f;
        whinePitch = 0.55f;
    }
}

/// <summary>
/// One beam for an instant: the sniper's shot, or the shot sent back. A line
/// that thins to nothing over its life and is gone.
/// </summary>
public class LaserFlash : MonoBehaviour
{
    LineRenderer line;
    float born, life, width;

    public static void Spawn(Vector3 a, Vector3 b, Material mat, float width, float seconds)
    {
        var go = new GameObject("LaserFlash") { layer = 2 };
        var f = go.AddComponent<LaserFlash>();
        f.line = go.AddComponent<LineRenderer>();
        f.line.useWorldSpace = true;
        f.line.positionCount = 2;
        f.line.SetPosition(0, a);
        f.line.SetPosition(1, b);
        f.line.material = mat;
        f.line.alignment = LineAlignment.View;
        f.line.numCapVertices = 2;
        f.line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        f.line.receiveShadows = false;
        f.line.generateLightingData = false;
        f.line.startWidth = f.line.endWidth = width;
        f.width = width;
        f.life = Mathf.Max(0.02f, seconds);
        f.born = Time.time;
    }

    void Update()
    {
        float k = (Time.time - born) / life;
        if (k >= 1f) { Destroy(gameObject); return; }
        line.startWidth = line.endWidth = width * (1f - k * k);
    }
}
