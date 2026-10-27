using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE COURSE of the chase: a straight +z run of CHUNKS generated from the level's seed ahead of you as you run
/// (Chase.Ahead kept built, the far one complete long before you can read it), destroyed behind you (Chase.Behind) or
/// dropped by the front, whichever comes first. A chunk is planned (ChaseCoursePlan: a list of features from a
/// System.Random seeded by the level, the chunk's index and its phase, so a chunk rebuilt after a death is the same
/// chunk) and then BUILT a feature at a time inside a 2 ms budget per frame. Every chunk root carries this
/// component's ChaseChunk marker, so the weld leaves it alone (a MonoBehaviour on the root: LevelWeld's rule), and
/// every marker, unit and pad a chunk needs is parented to it, so destroying the root destroys all of it.
/// Phases: three hits on the boss step the phase; the first chunk you ENTER after the step is the phase's checkpoint
/// (kept alive whatever the front does; the respawn point; the front and the boss reset around it on a death).
/// The finish: when the boss breaks, the chunk after the wreck's is the FINISH PLATFORM, whose doors warp you to the
/// static platform at the buffers where the level's own tail is built — unless the wreck is already there.
/// </summary>
public class ChaseChunk : MonoBehaviour { public int index; }

public partial class ChaseCourse : MonoBehaviour
{
    public static ChaseCourse Instance { get; private set; }

    public class Chunk
    {
        public int index, phase;
        public GameObject root;
        public float z0, z1;
        public bool dropping, finish;
        public float dropAt;
        public readonly List<float> gapZ0 = new List<float>(4), gapZ1 = new List<float>(4);
    }

    Transform levelRoot;
    MovementConfig config;
    ChaseKind kind;
    Zone zone;
    float floorY, z0, height;
    int seed;
    readonly List<Chunk> chunks = new List<Chunk>(12);
    readonly Stack<Chunk> pool = new Stack<Chunk>(12);
    int nextIndex;
    Chunk building;
    int phase = 1, hits;
    int protectedIndex = -1, pendingCheckpoint = -1;
    bool engaged, broken;
    int lastAllowed = Chase.MaxChunks - 1;
    float warpZ = -1f, nominalZ;
    float engagedAt;
    bool pulseCued;
    ChaseBoss boss;
    ChasePressure front;
    PlayerMotor motor;
    PlayerLook look;
    readonly System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

    public int Phase => phase;
    public int Hits => hits;
    public float FloorY => floorY;
    public float Z0 => z0;
    public ChaseKind Kind => kind;
    public bool Engaged => engaged;
    public bool Broken => broken;
    public Transform LevelRoot => levelRoot;

    void Awake() { Instance = this; }
    void OnDisable()
    {
        if (Instance == this) Instance = null;
        GameBootstrap.Respawned -= OnRespawned;
    }

    public void Init(Transform root, MovementConfig cfg, ChaseKind k, Zone z, float y, float zStart, int levelSeed)
    {
        levelRoot = root; config = cfg; kind = k; zone = z; floorY = y; z0 = zStart; seed = levelSeed;
        height = Chase.Height(k);
    }

    public void Attach(ChaseBoss b, ChasePressure f) { boss = b; front = f; }

    /// The first chunks, whole, at build time (no budget: nothing is running yet)
    public void Prebuild()
    {
        while (nextIndex < Chase.Ahead) Step();
    }

    void Start()
    {
        if (!Application.isPlaying) return;
        GameBootstrap.Respawned -= OnRespawned;
        GameBootstrap.Respawned += OnRespawned;
        if (MusicDirector.Enabled) MusicDirector.Ensure().PrepareBoss();
    }

    // ------------------------------------------------------------ the frame

    int ChunkIndexAt(float z) => Mathf.FloorToInt((z - z0) / Chase.ChunkLen);

    Chunk Find(int index)
    {
        for (int i = 0; i < chunks.Count; i++) if (chunks[i].index == index) return chunks[i];
        return null;
    }

    void Update()
    {
        if (motor == null)
        {
            var d = CombatDirector.Instance;
            if (d == null || d.motor == null) return;
            motor = d.motor;
            look = motor.look;
        }
        float pz = motor.CurrPosition.z;
        int pc = ChunkIndexAt(pz);
        if (!engaged && pz > z0 + 2f) Engage();
        if (engaged && !pulseCued && Time.time - engagedAt > Chase.IntroSeconds) { pulseCued = true; Cue(BossMusic.Section.Pulse); }
        if (pendingCheckpoint >= 0 && pc >= pendingCheckpoint) TakeCheckpoint(pendingCheckpoint);
        if (warpZ > 0f && pz > warpZ) Warp();

        // Generation inside the budget: the chunk under construction gets a feature at a time until the clock says stop
        sw.Restart();
        int target = Mathf.Min(pc + Chase.Ahead, lastAllowed);
        while (nextIndex <= target && sw.Elapsed.TotalMilliseconds < Chase.BudgetMs) Step();

        // Behind you: gone, whatever the front did (never the checkpoint's chunk, never the finish)
        for (int i = chunks.Count - 1; i >= 0; i--)
        {
            var c = chunks[i];
            if (c.finish || c.index == protectedIndex) continue;
            if (c.index < pc - Chase.Behind) Release(i);
        }
        // The fall: a dropped chunk accelerates down and tilts, then is destroyed
        float now = Time.time;
        for (int i = chunks.Count - 1; i >= 0; i--)
        {
            var c = chunks[i];
            if (!c.dropping) continue;
            float t = now - c.dropAt;
            if (t > Chase.DropSeconds || c.root == null) { Release(i); continue; }
            // Falls and tilts about its own centre (the root sits at the origin: a turn about that would fling it)
            Vector3 pivot = new Vector3(0f, floorY, (c.z0 + c.z1) * 0.5f);
            Quaternion r = Quaternion.Euler(t * 3f, 0f, (c.index & 1) == 0 ? t * 4f : -t * 4f);
            c.root.transform.rotation = r;
            c.root.transform.position = pivot - r * pivot + Vector3.down * (0.5f * 14f * t * t);
        }
    }

    /// One unit of work: start the next chunk's plan, build its next feature, or close it
    void Step()
    {
        if (building == null)
        {
            building = pool.Count > 0 ? pool.Pop() : new Chunk();
            int idx = nextIndex;
            building.index = idx;
            building.phase = idx < 2 ? 1 : phase;
            building.z0 = z0 + idx * Chase.ChunkLen;
            building.z1 = building.z0 + Chase.ChunkLen;
            building.dropping = false; building.finish = false;
            building.gapZ0.Clear(); building.gapZ1.Clear();
            building.root = new GameObject("ChaseChunk" + idx);
            building.root.transform.SetParent(levelRoot);
            building.root.AddComponent<ChaseChunk>().index = idx;
            Plan(building);
            featureIndex = 0;
            return;
        }
        if (featureIndex < features.Count) { BuildFeature(building, features[featureIndex]); featureIndex++; }
        if (featureIndex >= features.Count)
        {
            chunks.Add(building);
            building = null;
            nextIndex++;
        }
    }

    void Release(int i)
    {
        var c = chunks[i];
        if (c.root != null) Destroy(c.root);
        c.root = null;
        chunks.RemoveAt(i);
        pool.Push(c);
    }

    // ------------------------------------------------------------ the chase's beats

    void Engage()
    {
        engaged = true;
        engagedAt = Time.time;
        protectedIndex = 0;
        boss?.Engage();
        front?.Begin(z0 - Chase.FrontBehindStart);
        Cue(BossMusic.Section.Approach);
        if (look != null && boss != null) look.Steer(boss.HeadPoint, 0.6f, Chase.IntroSeconds);
    }

    static void Cue(BossMusic.Section s) { if (MusicDirector.Instance != null) MusicDirector.Instance.BossCue(s); }

    /// From the boss: a hit landed (hits so far). Three hits step the phase: the next chunks step up, the front's cap
    /// rises, and the first chunk you enter from here is the phase's checkpoint
    public void OnBossHit(int count)
    {
        hits = count;
        int p = Mathf.Min(Chase.Phases, 1 + count / Chase.HitsPerPhase);
        if (p == phase || count >= Chase.HitsToBreak) return;
        phase = p;
        front?.SetPhase(phase);
        pendingCheckpoint = motor != null ? ChunkIndexAt(motor.CurrPosition.z) + 1 : nextIndex;
        Cue(phase == 2 ? BossMusic.Section.Drop : BossMusic.Section.Climax);
    }

    void TakeCheckpoint(int index)
    {
        var c = Find(index);
        pendingCheckpoint = -1;
        if (c == null) return;
        protectedIndex = index;
        GameBootstrap.Checkpoint = new Vector3(0f, floorY + 0.6f, c.z0 + 2.5f);
        GameBootstrap.CheckpointForward = Vector3.forward;
    }

    /// From the boss: the ninth hit. The front dies; the chunk after the wreck's is the finish platform (or the
    /// static one at the buffers when the wreck is that close to them)
    public void OnBossBroken(float wreckZ)
    {
        if (broken) return;
        broken = true;
        front?.Die();
        Cue(BossMusic.Section.Tail);
        int finishIndex = ChunkIndexAt(wreckZ) + 2;
        if (finishIndex >= Chase.MaxChunks) { lastAllowed = Chase.MaxChunks - 1; return; }
        lastAllowed = finishIndex - 1;
        for (int i = chunks.Count - 1; i >= 0; i--) if (chunks[i].index >= finishIndex) Release(i);
        if (building != null && building.index >= finishIndex) { if (building.root != null) Destroy(building.root); pool.Push(building); building = null; }
        if (nextIndex > finishIndex) nextIndex = finishIndex;
        var f = pool.Count > 0 ? pool.Pop() : new Chunk();
        f.index = finishIndex; f.phase = phase; f.z0 = z0 + finishIndex * Chase.ChunkLen; f.z1 = f.z0 + Chase.FinishLen;
        f.dropping = false; f.finish = true; f.gapZ0.Clear(); f.gapZ1.Clear();
        f.root = new GameObject("ChaseFinish");
        f.root.transform.SetParent(levelRoot);
        f.root.AddComponent<ChaseChunk>().index = finishIndex;
        LevelKit.ChaseFinishPlatform(f.root.transform, kind, config, floorY, f.z0, true);
        chunks.Add(f);
        warpZ = f.z1 - 2.6f;
    }

    /// The static finish platform at the buffers: the end of the course if the boss was never caught, and the doors'
    /// destination when he broke early. Built at build time under the level root: static, so the weld takes it
    public void BuildFinishPlatform(Transform root, float z, bool warp)
    {
        nominalZ = z;
        LevelKit.ChaseFinishPlatform(root, kind, config, floorY, z, warp);
    }

    /// Through the doors: the same floor, the same speed, the platform at the buffers (a Pac-Man wrap: Warp keeps the air)
    void Warp()
    {
        warpZ = -1f;
        Vector3 p = motor.CurrPosition;
        motor.Warp(new Vector3(p.x, p.y, nominalZ + 2.5f));
        if (GameFeel.Instance != null) GameFeel.Instance.PlayPortal();
    }

    /// From the front: everything it has passed falls (never the checkpoint's chunk, never the finish)
    public void DropBehind(float frontZ)
    {
        for (int i = 0; i < chunks.Count; i++)
        {
            var c = chunks[i];
            if (c.dropping || c.finish || c.index == protectedIndex || c.z1 > frontZ - 1f) continue;
            c.dropping = true;
            c.dropAt = Time.time;
        }
    }

    /// A death (the front, a fall, the R key): back to the phase's first chunk. Everything but that chunk is rebuilt
    /// from the seed; the front stands forty metres behind it; the boss waits his lead ahead of it
    void OnRespawned()
    {
        if (!engaged || broken) return;
        var keep = Find(protectedIndex);
        for (int i = chunks.Count - 1; i >= 0; i--) if (chunks[i] != keep) Release(i);
        if (building != null) { if (building.root != null) Destroy(building.root); pool.Push(building); building = null; }
        float at = keep != null ? keep.z0 : z0 + Mathf.Max(0, protectedIndex) * Chase.ChunkLen;
        nextIndex = keep != null ? keep.index + 1 : Mathf.Max(0, protectedIndex);
        front?.ResetTo(at - Chase.FrontBehindStart);
        boss?.ResetTo(at + Chase.BossLead);
    }

    // ------------------------------------------------------------ for the boss's drops

    /// The chunk root an obstacle at `z` belongs to (it falls with it), or the level root when none is built there
    public Transform ParentFor(float z)
    {
        for (int i = 0; i < chunks.Count; i++) if (z >= chunks[i].z0 && z < chunks[i].z1 && chunks[i].root != null) return chunks[i].root.transform;
        return levelRoot;
    }

    /// A z at or after `z` that is floor, not gap (a dropped obstacle never lands in a hole); −1 when nothing is built there
    public float FloorAfter(float z, float need)
    {
        for (int i = 0; i < chunks.Count; i++)
        {
            var c = chunks[i];
            if (z < c.z0 || z >= c.z1 || c.finish) continue;
            float at = z;
            for (int g = 0; g < c.gapZ0.Count; g++)
                if (at + need > c.gapZ0[g] - 1f && at < c.gapZ1[g] + 1f) at = c.gapZ1[g] + 1f;
            return at + need <= c.z1 - 1f ? at : -1f;
        }
        return -1f;
    }
}
