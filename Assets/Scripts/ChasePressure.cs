using UnityEngine;

/// <summary>
/// THE PRESSURE: a COLLAPSE FRONT behind you. From the moment the chase is on, a plane of red advances up the course
/// — the chunks it passes fall away (ChaseCourse.DropBehind), a rumble rides on it (a looping sub on a 3D source: the
/// nearer, the louder), red lights on the plane and the curtain itself (red = danger, the one colour it wears). It
/// starts at Chase.FrontStart and gains Chase.FrontRise every Chase.FrontRiseEvery seconds, and NEVER exceeds the
/// phase's cap (0.9 × run in phase 1, 1.0 × in phase 2, 1.1 × in phase 3: in the last phase only hop chains stay clear).
/// Caught by it = the one way to die here: PlayerHealth is drained (the bootstrap respawns you at the phase's first
/// chunk; the course resets the front forty metres behind it). Nothing here is a move: it attributes nothing. It dies
/// with the vehicle: the plane fades, the rumble stops, the course stops falling.
/// </summary>
public class ChasePressure : MonoBehaviour
{
    public static ChasePressure Instance { get; private set; }

    ChaseCourse course;
    MovementConfig config;
    float z, speed, startedAt, lastKillAt = -10f, dieAt = -1f, floorY;
    int phase = 1;
    bool live;
    Transform plane;
    Renderer curtain;
    Light[] lights;
    AudioSource rumble;
    PlayerMotor motor;
    PlayerHealth health;

    public float Z => z;
    public bool Live => live;
    public float Speed => speed;
    /// 0 far from it, 1 at it: for a HUD lamp, when one wants it
    public float Danger01 => live && motor != null ? Mathf.Clamp01(1f - (motor.CurrPosition.z - z) / 40f) : 0f;

    void Awake() { Instance = this; }
    void OnDisable() { if (Instance == this) Instance = null; }

    /// Built at build time, parked behind the start: the curtain across the lane, its lights, the rumble (silent)
    public static ChasePressure Create(Transform root, MovementConfig config, ChaseCourse course, ChaseKind kind, float floorY, float zStart)
    {
        var host = new GameObject("ChaseFront");
        host.transform.SetParent(root);
        host.transform.position = new Vector3(0f, floorY, zStart);
        var f = host.AddComponent<ChasePressure>();
        f.course = course; f.config = config; f.z = zStart; f.floorY = floorY;
        float height = Chase.Height(kind);
        f.plane = host.transform;
        var sheet = LevelKit.Prim(host.transform, new Vector3(0f, floorY + height * 0.5f - 2f, zStart), new Vector3(Chase.HalfW * 2f + 2f, height + Chase.WallBelow + 1f, 0.3f), LevelKit.SignalRed, "CollapseCurtain");
        f.curtain = sheet.GetComponent<Renderer>();
        f.lights = new Light[3];
        for (int i = 0; i < 3; i++)
        {
            f.lights[i] = LevelKit.Light(host.transform, new Vector3((i - 1) * 6f, floorY + 4f, zStart + 1.5f), 30f, 3f);
            f.lights[i].color = new Color(1f, 0.2f, 0.12f);
        }
        var clip = GameFeel.Clip("sub");
        if (clip != null)
        {
            f.rumble = host.AddComponent<AudioSource>();
            f.rumble.clip = clip;
            f.rumble.loop = true;
            f.rumble.spatialBlend = 1f;
            f.rumble.rolloffMode = AudioRolloffMode.Linear;
            f.rumble.minDistance = 10f;
            f.rumble.maxDistance = 70f;
            f.rumble.volume = 0.7f;
            f.rumble.pitch = 0.6f;
            f.rumble.playOnAwake = false;
        }
        host.SetActive(false);   // nothing to see until the chase is on
        return f;
    }

    public void Begin(float zStart)
    {
        z = zStart;
        speed = Chase.FrontStart;
        startedAt = Time.time;
        live = true;
        Instance = this;
        gameObject.SetActive(true);
        if (rumble != null && !rumble.isPlaying) rumble.Play();
        Place();
    }

    public void ResetTo(float zStart) { z = zStart; Place(); }
    public void SetPhase(int p) => phase = Mathf.Clamp(p, 1, Chase.Phases);

    /// The vehicle broke: the front stops where it is and fades
    public void Die()
    {
        if (!live) return;
        live = false;
        dieAt = Time.time;
        if (rumble != null) rumble.Stop();
    }

    void Place() => plane.position = new Vector3(0f, floorY, z);

    void Update()
    {
        if (!Application.isPlaying) return;
        if (!live)
        {
            if (dieAt > 0f)
            {
                float k = (Time.time - dieAt) / 1.2f;
                foreach (var l in lights) if (l != null) l.intensity = Mathf.Lerp(3f, 0f, k);
                if (k >= 1f) { dieAt = -1f; gameObject.SetActive(false); }
            }
            return;
        }
        if (motor == null)
        {
            var d = CombatDirector.Instance;
            if (d == null || d.motor == null) return;
            motor = d.motor;
            health = motor.GetComponent<PlayerHealth>();
            if (health == null) health = FindFirstObjectByType<PlayerHealth>();
        }
        float now = Time.time, dt = Time.deltaTime;
        float ramp = Chase.FrontStart + Chase.FrontRise * Mathf.Floor((now - startedAt) / Chase.FrontRiseEvery);
        speed = Mathf.Min(ramp, config.baseRunSpeed * Chase.FrontCap[phase - 1]);
        z += speed * dt;
        Place();
        float pulse = 2.4f + Mathf.Sin(now * 9f) * 0.6f;
        foreach (var l in lights) if (l != null) l.intensity = pulse;
        course.DropBehind(z);
        if (motor.CurrPosition.z < z && now - lastKillAt > Chase.FrontKillGrace) Catch(now);
    }

    /// Caught: the one death. The health is drained so the bootstrap's own downed path respawns you (the flow then
    /// launches you from the checkpoint); a player it cannot drain (deathless, the range's god) is respawned outright
    void Catch(float now)
    {
        lastKillAt = now;
        int downs = health != null ? health.Downs : -1;
        if (health != null) health.TakeDamage(health.Max + 1f);
        if (health == null || health.Downs == downs) GameBootstrap.RespawnPlayer();
    }
}
