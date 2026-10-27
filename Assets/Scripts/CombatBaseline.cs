using System.Globalization;
using UnityEngine;

/// <summary>
/// THE BASELINE (2026-10-06): numbers for "combat doesn't feel right", taken before the classes change it. Per
/// SEGMENT (checkpoint to checkpoint, CourseFlow; since 2026-10-09 there are no rooms) and per level: kills and the
/// speed you had at each; cuts that hit, missed and executed; parries, and whiffs by cause; wall hits and what they
/// took; STOPS (a fall from running speed to a standstill with a live unit within forty metres); time spent slow in
/// a fight; hops perfect and lazy; the exit speed. Each segment is logged as you leave it and the level is summed at
/// the finish into the console and PlayerPrefs (baseline_&lt;scene&gt;, with the previous run kept beside it), so a
/// change to the ladder or a new class is judged against a number and not a feeling. The player never sees it; the
/// F3 readout carries one live line. Allocates only when a segment ends. (`loops` stays in the line, always 0 now.)
/// </summary>
public class CombatBaseline : MonoBehaviour
{
    public static CombatBaseline Instance { get; private set; }
    public PlayerMotor motor;
    public CombatDirector director;

    /// One line of the readout, rebuilt twice a second (never in OnGUI)
    public string Live { get; private set; } = "";

    class Tally
    {
        public int kills, cutsHit, cutsMiss, executes, shots, parries, whiffEmpty, whiffCommit, whiffCut, wallHits, stops, loops, hopsPerfect, hopsLazy, rooms;
        public float killSpeedSum, killSpeedMin = float.MaxValue, wallLost, slowTime, fightTime, time, exitSpeedSum;
        public void Reset() { kills = cutsHit = cutsMiss = executes = shots = parries = whiffEmpty = whiffCommit = whiffCut = wallHits = stops = loops = hopsPerfect = hopsLazy = rooms = 0; killSpeedSum = wallLost = slowTime = fightTime = time = exitSpeedSum = 0f; killSpeedMin = float.MaxValue; }
        public void Add(Tally t)
        {
            kills += t.kills; cutsHit += t.cutsHit; cutsMiss += t.cutsMiss; executes += t.executes; shots += t.shots; parries += t.parries;
            whiffEmpty += t.whiffEmpty; whiffCommit += t.whiffCommit; whiffCut += t.whiffCut; wallHits += t.wallHits; stops += t.stops; loops += t.loops;
            hopsPerfect += t.hopsPerfect; hopsLazy += t.hopsLazy; rooms += t.rooms;
            killSpeedSum += t.killSpeedSum; killSpeedMin = Mathf.Min(killSpeedMin, t.killSpeedMin); wallLost += t.wallLost;
            slowTime += t.slowTime; fightTime += t.fightTime; time += t.time; exitSpeedSum += t.exitSpeedSum;
        }
        public float AvgKillSpeed => kills > 0 ? killSpeedSum / kills : 0f;
        public float SlowShare => fightTime > 0.5f ? slowTime / fightTime : 0f;
        public int Whiffs => whiffEmpty + whiffCommit + whiffCut;
        public float WhiffRate => parries + Whiffs > 0 ? Whiffs / (float)(parries + Whiffs) : 0f;
        public float MissRate => cutsHit + cutsMiss > 0 ? cutsMiss / (float)(cutsHit + cutsMiss) : 0f;
    }

    Tally level = new Tally(), room = new Tally();
    readonly Tally liveTally = new Tally();
    int roomIdx = -1;
    float roomEnteredAt, nextLive, nextFightScan;
    bool fastArmed, reported, fight;
    const float FightRange = 40f;   // a live unit within this and you are fighting (the music's rule too)

    void Awake() { Instance = this; }

    void OnEnable()
    {
        MovementEvents.MoveExecuted += OnMove;
        CombatDirector.KillReported += OnKill;
    }

    void OnDisable()
    {
        MovementEvents.MoveExecuted -= OnMove;
        CombatDirector.KillReported -= OnKill;
        if (Instance == this) Instance = null;
    }

    float HSpeed => motor != null ? new Vector2(motor.Velocity.x, motor.Velocity.z).magnitude : 0f;

    void OnKill(KillMethod method, float hSpeed, bool airborne)
    {
        room.kills++;
        room.killSpeedSum += hSpeed;
        room.killSpeedMin = Mathf.Min(room.killSpeedMin, hSpeed);
    }

    void OnMove(MoveType type, string detail)
    {
        switch (type)
        {
            case MoveType.SwordStrike:
                if (detail == "MISS") room.cutsMiss++;
                else { room.cutsHit++; if (detail == "EXECUTE") room.executes++; }
                break;
            case MoveType.Fist:   // the hands count as cuts: hit, miss, execute
                if (detail == "MISS") room.cutsMiss++;
                else { room.cutsHit++; if (detail == "EXECUTE") room.executes++; }
                break;
            case MoveType.BowShot: room.shots++; break;
            case MoveType.Parry: room.parries++; break;
            case MoveType.ParryWhiff:
                if (detail == "ARM COMMITTED") room.whiffCommit++;
                else if (detail == "CUT THREW IT") room.whiffCut++;
                else room.whiffEmpty++;
                break;
            case MoveType.WallHit:
            {
                room.wallHits++;
                float into;
                if (float.TryParse(detail, NumberStyles.Float, CultureInfo.InvariantCulture, out into)) room.wallLost += into;
                break;
            }
            case MoveType.PerfectBhop: room.hopsPerfect++; break;
            case MoveType.Bhop: room.hopsLazy++; break;
        }
    }

    void Update()
    {
        if (motor == null || reported) return;
        float dt = Time.deltaTime, h = HSpeed;
        var flow = CourseFlow.Instance;
        var cur = flow != null ? flow.Current : null;
        int idx = cur != null ? cur.index : -1;
        if (idx != roomIdx)
        {
            if (roomIdx >= 0) CloseRoom(h);
            roomIdx = idx;
            roomEnteredAt = Time.time;
            fastArmed = false;
        }
        if (Time.time >= nextFightScan)
        {
            nextFightScan = Time.time + 0.25f;
            fight = CourseFlow.UnitNear(motor.CurrPosition, FightRange);
        }
        if (fight)
        {
            room.fightTime += dt;
            if (h < 4f) room.slowTime += dt;
            if (h >= 6f) fastArmed = true;
            else if (fastArmed && h < 1.5f && motor.Grounded) { room.stops++; fastArmed = false; }
        }
        if (Time.time >= nextLive)
        {
            nextLive = Time.time + 0.5f;
            liveTally.Reset(); liveTally.Add(level); liveTally.Add(room);
            var t = liveTally;
            Live = "BASELINE  kill " + t.AvgKillSpeed.ToString("0.0") + " m/s  stops " + t.stops + "  slow " + Mathf.RoundToInt(t.SlowShare * 100f) + "%  whiff " + Mathf.RoundToInt(t.WhiffRate * 100f) + "%  wall " + t.wallHits;
        }
    }

    void CloseRoom(float exitSpeed)
    {
        room.time = Time.time - roomEnteredAt;
        room.exitSpeedSum = exitSpeed;
        room.rooms = 1;
        Debug.Log("BASELINE segment " + roomIdx + "  " + room.time.ToString("0.0") + " s  kills " + room.kills + " @ " + room.AvgKillSpeed.ToString("0.0") + " m/s  cuts " + room.cutsHit + "/" + (room.cutsHit + room.cutsMiss) + " (" + room.executes + " exec)  parry " + room.parries + "  whiff " + room.Whiffs + " (empty " + room.whiffEmpty + ", commit " + room.whiffCommit + ", cut " + room.whiffCut + ")  wall " + room.wallHits + " (-" + room.wallLost.ToString("0") + " m/s)  stops " + room.stops + "  slow " + Mathf.RoundToInt(room.SlowShare * 100f) + "%  loops " + room.loops + "  hops " + room.hopsPerfect + "/" + (room.hopsPerfect + room.hopsLazy) + " perfect  exit " + exitSpeed.ToString("0.0") + " m/s");
        level.Add(room);
        room = new Tally();
    }

    /// The level's numbers, once, at the finish: to the console and to PlayerPrefs beside the previous run's
    public void Report(string scene)
    {
        if (reported) return;
        reported = true;
        if (roomIdx >= 0) CloseRoom(HSpeed);
        var t = level;
        float perRoom = t.rooms > 0 ? t.stops / (float)t.rooms : t.stops;
        string line = "kill " + t.AvgKillSpeed.ToString("0.0") + " m/s (min " + (t.kills > 0 ? t.killSpeedMin : 0f).ToString("0.0") + ")  stops/room " + perRoom.ToString("0.0") + "  slow " + Mathf.RoundToInt(t.SlowShare * 100f) + "% of " + t.fightTime.ToString("0") + " s fighting  cut miss " + Mathf.RoundToInt(t.MissRate * 100f) + "% (" + t.executes + " exec of " + t.cutsHit + ")  whiff " + Mathf.RoundToInt(t.WhiffRate * 100f) + "% (empty " + t.whiffEmpty + ", commit " + t.whiffCommit + ", cut " + t.whiffCut + ")  wall " + t.wallHits + " hits -" + t.wallLost.ToString("0") + " m/s  loops " + t.loops + "  hops " + t.hopsPerfect + "/" + (t.hopsPerfect + t.hopsLazy) + " perfect  rooms " + t.rooms;
        string key = "baseline_" + scene;
        string previous = PlayerPrefs.GetString(key, "");
        Debug.Log("BASELINE " + scene + " NOW   " + line);
        if (previous.Length > 0) Debug.Log("BASELINE " + scene + " WAS   " + previous);
        PlayerPrefs.SetString(key + "_prev", previous);
        PlayerPrefs.SetString(key, line);
        PlayerPrefs.Save();
    }
}
