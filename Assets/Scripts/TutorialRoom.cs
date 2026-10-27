using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ONE ROOM OF THE TUTORIAL. A room teaches one subject: its boards list what
/// it wants from you and count your reps as you do them, the door at the far
/// end is locked until the board is done, and then it shatters open.
/// Walking in makes the room your checkpoint, so a respawn or a fall puts you
/// back at ITS door and not at the start of the course; falling out of a room
/// puts you back at once, with nothing lost, because in here nothing is — and
/// your stamina comes back with you, so a failed try never leaves you waiting
/// on pips before the next one.
/// Reps only count while you are standing in the room that asked for them.
/// Bonus lines (the harder timing, the hidden tech) are counted and lit when
/// you find them, and never hold the door.
/// The words are on the WALL, not the HUD: the HUD shows state, never
/// instructions. A board is one text mesh a line (the kit's Fit measures one
/// line, not a block), and every line that changes is refitted.
/// </summary>
public class TutorialRoom : MonoBehaviour
{
    public class Task
    {
        public string label;
        public int need = 1, have;
        public bool bonus;
        public MoveType[] moves;            // a move task: any of these
        public string starts, notStarts;    // ... whose detail starts with this / with none of these ('|' separated)
        public float speed;                 // a speed task: reach this, in metres a second, inside the room
        public Enemy[] kill;                // a kill task: over these units
        public int bestShown;
        public bool Done => have >= need;
    }

    /// One place the room's tasks are written up: a label and a count for every task, and a line under them
    public class Board
    {
        public TextMesh[] labels, counts;
        public TextMesh foot;
        public float size, countWidth, footSize, footWidth;
    }

    // The course's inks. Gold, red and cyan are only ever used to MEAN gold, red and cyan
    public static readonly Color Bright = new Color(0.93f, 0.97f, 1f), Dim = new Color(0.56f, 0.66f, 0.72f),
        GoldInk = new Color(1f, 0.82f, 0.3f), RedInk = new Color(1f, 0.33f, 0.27f), CyanInk = new Color(0.35f, 0.9f, 1f);

    static readonly List<TutorialRoom> All = new List<TutorialRoom>();
    public static TutorialRoom Current { get; private set; }

    public string title;
    public int number;
    public float z0, z1, floorY;
    public Vector3 entry;
    public List<Task> tasks = new List<Task>();
    public readonly List<Board> boards = new List<Board>();
    public GameObject door;
    /// Lines across the room (ascending z): land past one and a fall puts you back THERE, not at the door — the pit
    /// you already crossed stays crossed. Your pips come back at each, as they do at the door.
    public float[] restarts = new float[0];

    bool[] wasAlive;
    Enemy[] watched = new Enemy[0];
    bool open, inside;
    int restartsPassed;
    StaminaSystem stamina;

    void OnEnable()
    {
        All.Add(this);
        MovementEvents.MoveExecuted += OnMove;
    }

    void OnDisable()
    {
        All.Remove(this);
        MovementEvents.MoveExecuted -= OnMove;
        if (Current == this) Current = null;
    }

    void Start()
    {
        // Every unit any kill task names, watched for the moment it drops
        var list = new List<Enemy>();
        foreach (var t in tasks) if (t.kill != null) foreach (var e in t.kill) if (e != null && !list.Contains(e)) list.Add(e);
        watched = list.ToArray();
        wasAlive = new bool[watched.Length];
        for (int i = 0; i < watched.Length; i++) wasAlive[i] = watched[i].Alive;
        if (Required() == 0) Open(false);   // nothing asked: the door was never locked
        Refresh();
    }

    int Required()
    {
        int n = 0;
        foreach (var t in tasks) if (!t.bonus) n++;
        return n;
    }

    void Update()
    {
        var motor = TutorialGate.Motor;
        if (motor == null) return;
        Vector3 p = motor.CurrPosition;
        bool now = p.z >= z0 && p.z < z1;
        if (now && !inside)
        {
            // In through the door: this room is where you come back to, and you start it fresh
            Current = this;
            GameBootstrap.Checkpoint = entry;
            TopUp();
        }
        inside = now;
        if (!inside) return;

        while (restartsPassed < restarts.Length && p.z >= restarts[restartsPassed] && motor.OnFlatGround)
        {
            entry = new Vector3(entry.x, entry.y, restarts[restartsPassed] + 1f);
            restartsPassed++;
            GameBootstrap.Checkpoint = entry;
            TopUp();
        }

        // Out of the room the wrong way: straight back, nothing lost
        if (p.y < floorY - 8f)
        {
            if (TutorialGate.Grapple != null) TutorialGate.Grapple.Cancel();
            motor.Teleport(entry);
            TopUp();
            if (GameFeel.Instance != null) GameFeel.Instance.PlayRespawn();
            return;
        }

        bool changed = false;
        Vector3 v = motor.Velocity;
        float speed = Mathf.Sqrt(v.x * v.x + v.z * v.z);
        foreach (var t in tasks)
        {
            if (t.speed <= 0f || t.Done) continue;
            int shown = Mathf.FloorToInt(speed);
            if (speed >= t.speed) { t.have = t.need; changed = true; }
            else if (shown > t.bestShown) { t.bestShown = shown; Refresh(); }   // the best so far climbs quietly
        }
        for (int i = 0; i < watched.Length; i++)
        {
            var e = watched[i];
            if (e == null) continue;
            bool alive = e.Alive;
            if (wasAlive[i] && !alive)
                foreach (var t in tasks)
                    if (t.kill != null && !t.Done && System.Array.IndexOf(t.kill, e) >= 0) { t.have++; changed = true; }
            wasAlive[i] = alive;
        }
        if (changed) Changed();
    }

    /// A try should never start on empty: the pips come back at the door
    void TopUp()
    {
        var motor = TutorialGate.Motor;
        if (motor == null) return;
        if (stamina == null) stamina = motor.GetComponent<StaminaSystem>();
        if (stamina != null && stamina.Pips < stamina.MaxPips) stamina.Refund(stamina.MaxPips - stamina.Pips);
    }

    void OnMove(MoveType type, string detail)
    {
        if (!inside) return;
        bool changed = false;
        foreach (var t in tasks)
        {
            if (t.moves == null || t.Done || System.Array.IndexOf(t.moves, type) < 0) continue;
            if (!string.IsNullOrEmpty(t.starts) && (detail == null || !detail.StartsWith(t.starts))) continue;
            if (!string.IsNullOrEmpty(t.notStarts) && detail != null)
            {
                bool barred = false;
                foreach (string no in t.notStarts.Split('|')) if (no.Length > 0 && detail.StartsWith(no)) { barred = true; break; }
                if (barred) continue;
            }
            t.have++;
            changed = true;
        }
        if (changed) Changed();
    }

    void Changed()
    {
        Refresh();
        if (GameFeel.Instance != null) GameFeel.Instance.PlayThunk();
        if (open) return;
        foreach (var t in tasks) if (!t.bonus && !t.Done) return;
        Open(true);
    }

    /// The boards: a line a task — done in gold, the bonus ones dim until they are found
    void Refresh()
    {
        foreach (var b in boards)
        {
            for (int i = 0; i < tasks.Count && i < b.labels.Length; i++)
            {
                var t = tasks[i];
                bool done = t.Done;
                Color ink = done ? GoldInk : t.bonus ? Dim : Bright;
                string count = done ? "DONE" : t.speed > 0f ? "BEST " + t.bestShown : t.have + " / " + t.need;
                if (b.labels[i] != null) b.labels[i].color = ink;
                var c = b.counts[i];
                if (c == null) continue;
                c.color = ink;
                if (c.text != count) { c.text = count; LevelKit.Fit(c, b.size, b.countWidth); }
            }
            if (b.foot == null) continue;
            string foot = open ? "THE DOOR IS OPEN" : "FINISH THE BOARD TO OPEN THE DOOR";
            b.foot.color = open ? GoldInk : Dim;
            if (b.foot.text != foot) { b.foot.text = foot; LevelKit.Fit(b.foot, b.footSize, b.footWidth); }
        }
    }

    /// The lock goes: the door in shards, the way on clear
    void Open(bool loud)
    {
        if (open) return;
        open = true;
        if (loud && GameFeel.Instance != null) GameFeel.Instance.PlayRoomClear();
        if (door != null)
        {
            if (loud)
            {
                Shards(door.transform.position + Vector3.up * 2.2f, new Vector3(7f, 4.4f, 0.6f));
                if (GameFeel.Instance != null) GameFeel.Instance.PlayShatter();
            }
            Destroy(door);
        }
        Refresh();
    }

    static Material shardMat;

    static void Shards(Vector3 centre, Vector3 size)
    {
        if (shardMat == null)
        {
            shardMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            shardMat.SetColor("_BaseColor", new Color(1.9f, 1.5f, 0.5f));
        }
        for (int i = 0; i < 40; i++)
        {
            var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard.name = "DoorShard";
            shard.layer = 2;
            Object.Destroy(shard.GetComponent<Collider>());
            shard.transform.position = centre + new Vector3(Random.Range(-0.5f, 0.5f) * size.x, Random.Range(-0.5f, 0.5f) * size.y, 0f);
            shard.transform.rotation = Random.rotation;
            shard.transform.localScale = new Vector3(Random.Range(0.15f, 0.5f), Random.Range(0.15f, 0.5f), 0.05f);
            shard.GetComponent<MeshRenderer>().sharedMaterial = shardMat;
            var rb = shard.AddComponent<Rigidbody>();
            rb.mass = 0.05f;
            rb.linearVelocity = Vector3.forward * Random.Range(2f, 7f) + Random.insideUnitSphere * 3f + Vector3.up * 2f;
            rb.angularVelocity = Random.insideUnitSphere * 12f;
            shard.AddComponent<ShardFade>();
        }
    }
}
