using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE DISTRICT: what an open level needs where a linear one has rooms.
///   BEACONS        the police's marker posts. Pass one and it is where you come
///                  back to (a fall, the street, being downed, the respawn key);
///                  the last one you passed is the one that is lit.
///   RELIEF VALVES  the breakers (Breaker): the valves on the burst steam main.
///                  Each one shut lights a lamp on the gauge up the relief stack;
///                  all of them and the stack goes to MAIN PRESSURE. A SIDE
///                  OBJECTIVE since course-grammar (2026-10-11): nothing on a
///                  route waits for a switch, so the stack vents from the start
///                  on the main's residual pressure and MAIN PRESSURE only makes
///                  it lift harder.
///   UPDRAFTS       columns of steam: the burst main's columns in the cut and
///                  over the lift head (always on) and the relief stack
///                  (liveBoost: a stronger, higher ride on main pressure). The
///                  same rule as the plant's fans: inside one you are carried
///                  up, and the top quarter only holds you, so you can steer
///                  off it. Level 12's thermals are the same thing.
///   THE MELT       a plane that puts you back at your beacon: the street under
///                  the rooftops (Levels 12 and 13). The Breach (Level 11) lays
///                  none: its holes have floors, and pads in the floors.
/// It checks nothing else. The finish is a door on a roof: how you got onto
/// the roof is your business, and the valves are only the way the railway
/// meant you to.
/// The HUD reads StateLine: state, never an instruction.
/// </summary>
public class District : MonoBehaviour, IWeldDynamic
{
    /// What the district lights, turns or takes away: the weld leaves it alone (the beacon lamps, the gauge lamps, the
    /// shaft's fan hubs and signs, the tie-ins' lamps, bars and beams)
    public void DynamicParts(List<Transform> into)
    {
        foreach (var b in beacons) if (b != null && b.lamp != null) into.Add(b.lamp.transform);
        foreach (var r in gauge) if (r != null) into.Add(r.transform);
        foreach (var h in fanHubs) if (h != null) into.Add(h);
        if (deadSign != null) into.Add(deadSign.transform);
        if (liveSign != null) into.Add(liveSign.transform);
        foreach (var b in breakers)
        {
            if (b == null) continue;
            if (b.lamp != null) into.Add(b.lamp.transform);
            if (b.bar != null) into.Add(b.bar.transform);
            if (b.beam != null) into.Add(b.beam.transform);
        }
    }
    public static District Instance { get; private set; }

    public class Beacon { public Vector3 at; public float radius = 7f; public Renderer lamp; public Light light; }
    /// `needsPower`: off until the valves are shut (nothing on a route uses it now). `liveBoost`: always on, and once
    /// the valves are shut it lifts to 20 m/s instead of 16 and holds you only over the top tenth
    public class Updraft { public Vector3 centre; public float half, y0, y1; public bool needsPower, liveBoost; }
    public class Melt { public float x0, x1, z0, z1, y; }

    public MovementConfig config;
    public readonly List<Beacon> beacons = new List<Beacon>();
    public readonly List<Breaker> breakers = new List<Breaker>();
    public readonly List<Updraft> updrafts = new List<Updraft>();
    public readonly List<Melt> melts = new List<Melt>();

    public Material beaconOff, beaconOn, gaugeOn;
    /// The lamp stack up the stack: one a valve, lit from the bottom
    public readonly List<Renderer> gauge = new List<Renderer>();
    /// The stack's two signs: the one it wears on residual pressure, the one it wears on main pressure
    public GameObject deadSign, liveSign;
    /// What spins on main pressure: the governor at the stack's foot (Level 11), the fan hubs (Level 12)
    public readonly List<Transform> fanHubs = new List<Transform>();
    public readonly List<ParticleSystem> shaftAir = new List<ParticleSystem>();
    /// The soot in the air: it is only ever falling where you are
    public Transform ash;

    int thrown;
    int current = -1;
    float shadowsWere = -1f;
    const float ShadowReach = 190f;

    public bool Powered => breakers.Count > 0 && thrown >= breakers.Count;
    public string StateLine { get; private set; } = "";

    void OnEnable() { Instance = this; }
    void OnDisable()
    {
        if (Instance == this) Instance = null;
        // The pipeline asset is shared by every scene: what Start borrowed goes back
        var urp = UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset;
        if (urp != null && shadowsWere > 0f) urp.shadowDistance = shadowsWere;
        shadowsWere = -1f;
    }

    void Start()
    {
        // A dawn sun seven degrees up throws shadows a hundred metres long, and the project draws them for fifty. Out here
        // that is most of the picture, so the district borrows a longer reach for as long as it stands (never for a postcard:
        // Start does not run on one).
        var urp = UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset;
        if (urp != null && urp.shadowDistance < ShadowReach) { shadowsWere = urp.shadowDistance; urp.shadowDistance = ShadowReach; }
        if (liveSign != null) liveSign.SetActive(false);
        foreach (var ps in shaftAir) if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        Restate();
    }

    void Restate()
        => StateLine = breakers.Count == 0 ? ""
            : Powered ? "RELIEF VALVES  " + thrown + " / " + breakers.Count + "  ·  MAIN PRESSURE"
            : "RELIEF VALVES  " + thrown + " / " + breakers.Count + "  ·  RESIDUAL PRESSURE";

    /// A valve has been shut (Breaker.Struck)
    public void TieIn(Breaker b)
    {
        thrown++;
        if (thrown - 1 < gauge.Count && gauge[thrown - 1] != null && gaugeOn != null) gauge[thrown - 1].sharedMaterial = gaugeOn;
        Restate();
        var feel = GameFeel.Instance;
        if (feel != null) feel.PlayRoomClear();
        if (!Powered) return;
        // All of them: the stack goes to main pressure
        if (deadSign != null) deadSign.SetActive(false);
        if (liveSign != null) liveSign.SetActive(true);
        foreach (var ps in shaftAir) if (ps != null) ps.Play(true);
    }

    void Update()
    {
        var director = CombatDirector.Instance;
        var motor = director != null ? director.motor : null;
        if (motor == null) return;
        Vector3 p = motor.CurrPosition;

        // The beacon you are at is the one you come back to
        for (int i = 0; i < beacons.Count; i++)
        {
            if (i == current) continue;
            var b = beacons[i];
            float dx = p.x - b.at.x, dz = p.z - b.at.z;
            if (dx * dx + dz * dz > b.radius * b.radius || Mathf.Abs(p.y - b.at.y) > 8f) continue;
            Light(i);
            break;
        }

        // The melt: back to your beacon
        foreach (var m in melts)
        {
            if (p.y > m.y || p.x < m.x0 || p.x > m.x1 || p.z < m.z0 || p.z > m.z1) continue;
            var feel = GameFeel.Instance;
            if (feel != null) feel.Sparks(p + Vector3.up * 0.5f, 30, false);
            GameBootstrap.RespawnPlayer();
            break;
        }

        if (ash != null) ash.position = p + Vector3.up * 16f;
        if (Powered)
            foreach (var hub in fanHubs) if (hub != null) hub.Rotate(Vector3.up, 420f * Time.deltaTime, Space.World);
    }

    void Light(int index)
    {
        if (current >= 0 && current < beacons.Count)
        {
            var was = beacons[current];
            if (was.lamp != null && beaconOff != null) was.lamp.sharedMaterial = beaconOff;
            if (was.light != null) was.light.enabled = false;
        }
        current = index;
        var b = beacons[index];
        if (b.lamp != null && beaconOn != null) b.lamp.sharedMaterial = beaconOn;
        if (b.light != null) b.light.enabled = true;
        GameBootstrap.Checkpoint = b.at + Vector3.up * 1.2f;
        if (GameFeel.Instance != null) GameFeel.Instance.PlayPipRegen();
    }

    void FixedUpdate()
    {
        var director = CombatDirector.Instance;
        var motor = director != null ? director.motor : null;
        if (motor == null || config == null) return;
        Vector3 feet = motor.CurrPosition;
        float dt = Time.fixedDeltaTime, g = config.gravity, lift = config.factoryLift;
        bool powered = Powered;
        for (int i = 0; i < updrafts.Count; i++)
        {
            var u = updrafts[i];
            if (u.needsPower && !powered) continue;
            if (Mathf.Abs(feet.x - u.centre.x) > u.half || Mathf.Abs(feet.z - u.centre.z) > u.half || feet.y < u.y0 - 0.5f || feet.y > u.y1) continue;
            bool live = u.liveBoost && powered;   // the tie-ins' reward: the same column, run harder
            float k = (feet.y - u.y0) / Mathf.Max(1f, u.y1 - u.y0);
            float accel = k > (live ? 0.9f : 0.75f) ? g : g + lift;
            if (motor.Velocity.y < (live ? 20f : 16f)) motor.AddImpulse(Vector3.up * (accel * dt));
            break;
        }
    }
}
