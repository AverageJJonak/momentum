using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// THE RUN RECORDER (Phase 2, 2026-10-27; the user: "a run recorder that logs position and speed every frame and
/// exports a CSV I can send back", "yes to the recorder marker key (stop / snag / flow)"). On every level it writes
/// Docs/Runs/<level>-<date>.csv (next to the project; the persistent data folder in a build), one row per frame:
/// time, position, horizontal and vertical speed, the motor's state (grounded, sliding, wall running, dashing), the
/// course segment, the style rank, the movement events of that frame, and the marker. THE MARKER KEYS: F9 = STOP
/// (you were stopped dead), F10 = SNAG (something caught you), F11 = FLOW (this felt right). A press stamps the row
/// and flashes the word on screen. The file is flushed every two seconds and closed when the level ends, so a crash
/// loses at most two seconds. It reads the motor; it never writes to it.
/// </summary>
public class RunRecorder : MonoBehaviour
{
    public const Key StopKey = Key.F9, SnagKey = Key.F10, FlowKey = Key.F11;

    StreamWriter writer;
    string path, pendingEvents = "", pendingMarker = "", shown = "";
    float flushAt, shownUntil, startedAt;
    int rows;
    PlayerMotor motor;
    GUIStyle style;

    void Start()
    {
        var boot = GetComponent<GameBootstrap>();
        string level = boot != null ? boot.level.ToString() : "Level";
        string dir = Application.isEditor ? Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Docs", "Runs") : Path.Combine(Application.persistentDataPath, "Runs");
        try
        {
            Directory.CreateDirectory(dir);
            path = Path.Combine(dir, level + "-" + System.DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".csv");
            writer = new StreamWriter(path, false, new UTF8Encoding(false));
            writer.WriteLine("t,x,y,z,hspeed,vspeed,grounded,sliding,wallrun,dashing,segment,rank,events,marker");
        }
        catch (System.Exception e) { Debug.LogWarning("RunRecorder: could not open " + path + " (" + e.Message + ")"); writer = null; return; }
        MovementEvents.MoveExecuted += OnMove;
        startedAt = Time.unscaledTime;
        shownUntil = startedAt + 4f;
        shown = "REC  F9 stop · F10 snag · F11 flow";
        Debug.Log("RunRecorder: recording to " + path + "  (F9 = STOP, F10 = SNAG, F11 = FLOW)");
    }

    void OnMove(MoveType type, string detail)
    {
        string e = type.ToString() + (string.IsNullOrEmpty(detail) ? "" : ":" + detail.Replace(',', ' ').Replace('"', '\''));
        pendingEvents = pendingEvents.Length == 0 ? e : pendingEvents + "|" + e;
    }

    void Update()
    {
        if (writer == null) return;
        if (motor == null) { motor = PlayerMotor.Current; if (motor == null) motor = FindAnyObjectByType<PlayerMotor>(); if (motor == null) return; }
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb[StopKey].wasPressedThisFrame) Mark("STOP");
            else if (kb[SnagKey].wasPressedThisFrame) Mark("SNAG");
            else if (kb[FlowKey].wasPressedThisFrame) Mark("FLOW");
        }
        var v = motor.Velocity;
        var p = motor.transform.position;
        var flow = CourseFlow.Instance;
        var style = StyleSystem.Current;
        var ci = CultureInfo.InvariantCulture;
        float t = LevelTimer.Instance != null ? LevelTimer.Instance.Elapsed : Time.time;
        writer.Write(t.ToString("0.000", ci)); writer.Write(',');
        writer.Write(p.x.ToString("0.00", ci)); writer.Write(','); writer.Write(p.y.ToString("0.00", ci)); writer.Write(','); writer.Write(p.z.ToString("0.00", ci)); writer.Write(',');
        writer.Write(new Vector2(v.x, v.z).magnitude.ToString("0.00", ci)); writer.Write(','); writer.Write(v.y.ToString("0.00", ci)); writer.Write(',');
        writer.Write(motor.Grounded ? '1' : '0'); writer.Write(','); writer.Write(motor.Sliding ? '1' : '0'); writer.Write(','); writer.Write(motor.WallRunning ? '1' : '0'); writer.Write(','); writer.Write(motor.Dashing ? '1' : '0'); writer.Write(',');
        writer.Write('"'); writer.Write(flow != null ? flow.CurrentName.Replace('"', '\'') : ""); writer.Write("\",");
        writer.Write(style != null ? style.Rank : ""); writer.Write(',');
        writer.Write('"'); writer.Write(pendingEvents); writer.Write("\",");
        writer.WriteLine(pendingMarker);
        pendingEvents = ""; pendingMarker = "";
        rows++;
        if (Time.unscaledTime >= flushAt) { writer.Flush(); flushAt = Time.unscaledTime + 2f; }
    }

    void Mark(string tag)
    {
        pendingMarker = tag;
        shown = "MARK: " + tag;
        shownUntil = Time.unscaledTime + 1.2f;
        Debug.Log("RunRecorder: marker " + tag + " at " + (LevelTimer.Instance != null ? LevelTimer.Instance.Elapsed : Time.time).ToString("0.00", CultureInfo.InvariantCulture) + " s");
    }

    void OnGUI()
    {
        if (writer == null || Time.unscaledTime > shownUntil || Event.current.type != EventType.Repaint) return;
        if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.LowerRight };
        GUI.color = new Color(1f, 0.35f, 0.3f, 0.9f);
        GUI.Label(new Rect(Screen.width - 420f, Screen.height - 34f, 400f, 24f), shown, style);
        GUI.color = Color.white;
    }

    void OnDestroy() => Close();
    void OnApplicationQuit() => Close();

    void Close()
    {
        MovementEvents.MoveExecuted -= OnMove;
        if (writer == null) return;
        try { writer.Flush(); writer.Dispose(); } catch (System.Exception) { }
        writer = null;
        Debug.Log("RunRecorder: wrote " + rows + " rows to " + path);
    }
}
