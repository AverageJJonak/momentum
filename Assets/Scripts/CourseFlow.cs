using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE FLOW (2026-10-09, course-grammar): what the room loop was, with the rooms gone. A level is one continuous
/// course; this watches the player's place on it. CHECKPOINTS are VOLUMES (district-grammar, 2026-10-12): boxes in
/// the world — a junction, a plaza, a lobby — and, on a chain that marks them by z alone (Course.Checkpoint), thin
/// slabs across the lane at that z. Entering one makes it the respawn, and a respawn puts you there MOVING — at run
/// speed along the checkpoint's own FORWARD (the authored route's direction there, +z on a chain), the view turned
/// the same way — so the first thing you do after dying is run, not stand (the respawn-moving rule; GameBootstrap
/// .Respawn teleports, this launches). On a chain a line crossed is never un-crossed; in a district the LAST junction
/// you entered is the respawn, whichever way you came. SEGMENTS are named by their checkpoint (a junction's name in a
/// district) and are what the baseline logs and the HUD names; the units standing in one are its fuel, counted for
/// the readout and never for a gate. Nothing here gates, counts you down, loops or closes. Not on the tutorial or the range.
/// </summary>
public class CourseFlow : MonoBehaviour
{
    public static CourseFlow Instance { get; private set; }

    public MovementConfig config;
    public PlayerMotor motor;
    public CombatDirector director;

    public class Segment
    {
        public int index;
        public string name;
        public float z, floorY;
        public Vector3 spawn;
        /// The way a respawn here sends you: the route's direction at the checkpoint (+z on a chain)
        public Vector3 forward = Vector3.forward;
        /// The checkpoint's box; `volume` when it is a real box (a junction), else a slab across a lane ordered by z
        public Bounds bounds;
        public bool volume;
        public readonly List<Enemy> units = new List<Enemy>();
        public int Alive { get { int n = 0; foreach (var e in units) if (e != null && e.Alive) n++; return n; } }
    }

    readonly List<Segment> segments = new List<Segment>();
    int current = -1, reached = -1, lastVolume = -1;
    float enteredAt;
    PlayerLook look;

    public int Count => segments.Count;
    /// The segment the player is in (the volume they are in, else the last one entered, else the last line crossed) and
    /// the one a respawn goes to
    public Segment Current => current >= 0 && current < segments.Count ? segments[current] : null;
    public Segment Reached => reached >= 0 && reached < segments.Count ? segments[reached] : null;
    public string CurrentName => Current != null ? Current.name : "";
    public float SegmentTime => current >= 0 ? Time.time - enteredAt : 0f;
    public int Respawns { get; private set; }
    /// A word that pops on the HUD when a line is crossed (DebugHUD draws it as it drew the room stamps)
    public string Stamp { get; private set; } = "";
    public float StampUntil { get; private set; }
    public Color StampColor { get; private set; } = Color.white;
    static readonly Color Paper = new Color(0.93f, 0.88f, 0.76f);

    void Awake() { Instance = this; }
    void OnDisable()
    {
        if (Instance == this) Instance = null;
        GameBootstrap.Respawned -= OnRespawned;
    }

    /// The course's checkpoints from the layout; units sorted into the segments they stand in
    public void Init(LevelSections.Layout layout)
    {
        segments.Clear();
        lastVolume = -1;
        if (layout != null && layout.checkpoints.Count > 0)
        {
            var first = layout.checkpoints[0];
            if (first.volume || first.z > 1f)
                segments.Add(new Segment { index = 0, name = "START", z = float.NegativeInfinity, floorY = layout.spawn.y, spawn = layout.spawn });
            foreach (var cp in layout.checkpoints)
            {
                var s = new Segment { index = segments.Count, name = cp.name, z = cp.z, floorY = cp.at.y - 0.6f, spawn = cp.at, volume = cp.volume };
                s.forward = cp.forward.sqrMagnitude > 1e-4f ? cp.forward.normalized : Vector3.forward;
                s.bounds = cp.volume || cp.bounds.size.sqrMagnitude > 1e-4f ? cp.bounds : Slab(layout, cp);
                segments.Add(s);
            }
        }
        else if (layout != null && layout.rooms.Count > 0)
        {
            // An old chain: every section's start is a line, its spawn just inside on its floor
            foreach (var r in layout.rooms)
                segments.Add(new Segment { index = segments.Count, name = "SECTION " + (segments.Count + 1).ToString("00"), z = r.z0, floorY = r.entryY, spawn = new Vector3(0f, r.entryY + 0.6f, r.z0 + 3.5f),
                    bounds = new Bounds(new Vector3(0f, r.entryY + 12f, r.z0), new Vector3(r.halfW * 2f + 4f, 30f, 1.2f)) });
            segments[0].z = float.NegativeInfinity;
            segments[0].spawn = layout.spawn;
        }
        else segments.Add(new Segment { index = 0, name = "COURSE", z = float.NegativeInfinity, spawn = layout != null ? layout.spawn : Vector3.zero });

        Physics.SyncTransforms();
        foreach (var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (e.kind == EnemyKind.Boss) continue;
            segments[IndexForUnit(e.transform.position)].units.Add(e);
        }
        GameBootstrap.Respawned -= OnRespawned;
        GameBootstrap.Respawned += OnRespawned;
        if (motor != null)
        {
            look = motor.look;
            current = reached = IndexAt(motor.CurrPosition);
            enteredAt = Time.time;
            var s = segments[current];
            GameBootstrap.CheckpointForward = s.forward;
        }
    }

    /// The slab a z-only mark stands for: across the lane of the section it sits in, thirty metres tall
    static Bounds Slab(LevelSections.Layout layout, LevelSections.CheckpointMark cp)
    {
        float halfW = 10f;
        foreach (var r in layout.rooms)
            if (cp.z >= r.z0 - 0.5f && cp.z <= r.z1 + 0.5f) { halfW = Mathf.Max(halfW, r.halfW); break; }
        return new Bounds(new Vector3(0f, cp.at.y + 12f, cp.z), new Vector3(halfW * 2f + 4f, 30f, 1.2f));
    }

    /// The segment for a point: the volume it is in (the last one entered while it is between volumes), else the
    /// last line crossed by z — whichever lies later on the course
    int IndexAt(Vector3 p)
    {
        int vol = -1;
        for (int i = 0; i < segments.Count; i++)
            if (segments[i].volume && segments[i].bounds.Contains(p)) { vol = i; break; }
        if (vol >= 0) lastVolume = vol;
        int lin = 0;
        for (int i = 0; i < segments.Count; i++) if (!segments[i].volume && p.z >= segments[i].z) lin = i;
        return lastVolume > lin ? lastVolume : lin;
    }

    /// Where a unit belongs at the start: the volume it stands in, the nearest volume within fifty metres of it, else by z
    int IndexForUnit(Vector3 p)
    {
        int best = -1;
        float bestD = float.MaxValue;
        for (int i = 0; i < segments.Count; i++)
        {
            var s = segments[i];
            if (!s.volume) continue;
            if (s.bounds.Contains(p)) return i;
            float d = Mathf.Sqrt(s.bounds.SqrDistance(p));
            if (d < 50f && d < bestD) { bestD = d; best = i; }
        }
        if (best >= 0) return best;
        int lin = 0;
        for (int i = 0; i < segments.Count; i++) if (!segments[i].volume && p.z >= segments[i].z) lin = i;
        return lin;
    }

    void Update()
    {
        if (motor == null || segments.Count == 0) return;
        int idx = IndexAt(motor.CurrPosition);
        if (idx == current) return;
        current = idx;
        enteredAt = Time.time;
        var s = segments[idx];
        // A line crossed for the first time is the respawn now and going back never moves it back; a volume entered is
        // the respawn whichever way you came (the last junction you passed)
        bool take = s.volume ? idx != reached : idx > reached;
        if (!take) return;
        reached = idx;
        GameBootstrap.Checkpoint = s.spawn;
        GameBootstrap.CheckpointForward = s.forward;
        if (idx > 0) SetStamp(s.name, Paper, 0.9f);
    }

    /// The respawn-moving rule: the bootstrap has just stood you at the checkpoint; you leave it at run speed along its
    /// forward, the view pulled the same way
    void OnRespawned()
    {
        Respawns++;
        if (motor == null || config == null) return;
        var s = Reached;
        Vector3 fwd = s != null ? s.forward : Vector3.forward;
        motor.Launch(fwd * config.baseRunSpeed);
        if (look != null) look.Steer(motor.CurrPosition + Vector3.up * 1.5f + fwd * 30f, 1f, 0.6f);
    }

    /// A unit that fights (not a dummy, not a boss), alive and awake, within `range` of `p`: what the music and the
    /// baseline call a fight now that no room says so
    public static bool UnitNear(Vector3 p, float range)
    {
        float r2 = range * range;
        foreach (var e in Enemy.Live)
        {
            if (e == null || !e.Alive || e.kind == EnemyKind.Dummy || e.kind == EnemyKind.Boss) continue;
            if ((e.transform.position - p).sqrMagnitude < r2 && !e.IsResting) return true;
        }
        return false;
    }

    void SetStamp(string text, Color color, float seconds)
    {
        Stamp = text;
        StampColor = color;
        StampUntil = Time.unscaledTime + seconds;
    }
}
