using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// THE GYM'S TEST TOOLS (2026-11-11; the user: "the beat gym is painfully long and tedious to test. Make it fast to
/// use": a teleport menu, a reset to the start of the current station, a mark on the stations already approved and a
/// "new or changed only" list). Nothing here builds or changes a beat: the builder only tells this file what it built.
///
/// THE STATIONS: the builder registers every station as it builds it (its name, its pad, the pieces you can touch in
/// it). A piece's owner is its station, so THE STATION YOU ARE IN is the one whose floor is under your feet, and the
/// reset (R, the respawn key the game already has) goes to that station's pad even when you flew over the pad itself.
/// A fall is caught twenty-five metres under the station instead of at the city floor hundreds of metres down.
///
/// THE MENU (TAB): every beat a row, its three difficulties across; arrows move, ENTER goes there, X marks a station
/// approved (or takes the mark off), N lists only what is new or changed. The game stands still while it is up.
///
/// APPROVED, NEW, CHANGED: approving a station keeps a print of it (the name, place and size of every piece you can
/// touch, measured from the station's own pad, so a change further up the row moves nothing). A station with no print
/// is NEW; one whose pieces no longer match its print is CHANGED; the prints live in a file beside the saves.
/// </summary>
public static class GymStations
{
    public class Station
    {
        public string title, beat;
        public int difficulty;
        public float lowY = float.MaxValue;   // the lowest floor top in it: a fall is caught under this
        public string names = "";             // the print: the pieces' names in the order built, and six numbers each (centre and size, centimetres, from the pad)
        public readonly List<int> nums = new List<int>();
        public int first, beatEnd;            // its pad, slope and beat among the root's children (the print's span)
        public Vector3 origin; public Quaternion rot;   // its pad's pose when it was built
    }

    public static readonly List<Station> All = new List<Station>();
    static readonly Dictionary<Collider, int> owner = new Dictionary<Collider, int>();

    public static void Begin() { All.Clear(); owner.Clear(); }

    /// The station that owns this piece, or -1 (the start, the finish, nothing)
    public static int Of(Collider c) => c != null && owner.TryGetValue(c, out int k) ? k : -1;

    public static int Find(string title)
    {
        for (int i = 0; i < All.Count; i++) if (All[i].title == title) return i;
        return -1;
    }

    /// A station built: the children of `root` from `first` to `beatEnd` are its pad, slope and beat (the print), and on
    /// to `end` the bend after it (owned, so standing on the bend is still this station; not printed: every bend is the same)
    public static void Add(string title, string beat, int difficulty, Transform root, int first, int beatEnd, int end, Vector3 origin, Quaternion rot)
    {
        var s = new Station { title = title, beat = beat, difficulty = difficulty, first = first, beatEnd = beatEnd, origin = origin, rot = rot };
        int k = All.Count;
        s.names = Print(root, first, beatEnd, origin, rot, s.nums);
        for (int i = first; i < end && i < root.childCount; i++)
        {
            var t = root.GetChild(i);
            var col = t.GetComponent<Collider>();
            if (col == null) continue;   // a picture, a sign, a lamp, a pole: nothing to stand on
            {
                owner[col] = k;
                string n = t.name;
                bool floorish = !n.EndsWith("Body") && n != "Post" && n != "KickWall" && n != "Lintel" && n != "Divider";
                if (floorish)
                {
                    Vector3 half = t.localScale * 0.5f;   // the top of a turned box, from its own axes (a collider's bounds are not to be trusted the moment it is made)
                    s.lowY = Mathf.Min(s.lowY, t.position.y + Mathf.Abs(t.right.y) * half.x + Mathf.Abs(t.up.y) * half.y + Mathf.Abs(t.forward.y) * half.z);
                }
            }
        }
        if (s.lowY == float.MaxValue) s.lowY = origin.y;
        All.Add(s);
    }

    /// THE PRINT of the children `first`..`end` of `root`: every piece you can touch (a collider, or a marked pole), its
    /// name, and its centre and size in centimetres measured from the pose (`origin`, `rot`) the station was built at.
    /// Returns the names; the numbers go into `nums`, six a piece
    public static string Print(Transform root, int first, int end, Vector3 origin, Quaternion rot, List<int> nums)
    {
        var names = new System.Text.StringBuilder();
        Quaternion inv = Quaternion.Inverse(rot);
        nums.Clear();
        for (int i = first; i < end && i < root.childCount; i++)
        {
            var t = root.GetChild(i);
            if (t.GetComponent<Collider>() == null && t.GetComponent<MoveSurface>() == null) continue;
            Vector3 p = inv * (t.position - origin), sz = t.localScale;
            names.Append(t.name).Append(';');
            nums.Add(Mathf.RoundToInt(p.x * 100f)); nums.Add(Mathf.RoundToInt(p.y * 100f)); nums.Add(Mathf.RoundToInt(p.z * 100f));
            nums.Add(Mathf.RoundToInt(sz.x * 100f)); nums.Add(Mathf.RoundToInt(sz.y * 100f)); nums.Add(Mathf.RoundToInt(sz.z * 100f));
        }
        return names.ToString();
    }
}

/// The approvals: one entry a station, the print it was approved with. Kept in a file beside the saves
public static class GymApprovals
{
    public enum State { New, Changed, Approved }

    [Serializable] class Entry { public string title; public string names; public int[] nums; }
    [Serializable] class Store { public List<Entry> items = new List<Entry>(); }

    const int Tolerance = 3;   // centimetres: the same station built further down the row differs by a hair of rounding, a real change by far more
    static Store store;
    static string FilePath => Path.Combine(Application.persistentDataPath, "beatgym-approved.json");

    static void Load()
    {
        if (store != null) return;
        store = new Store();
        try { if (File.Exists(FilePath)) store = JsonUtility.FromJson<Store>(File.ReadAllText(FilePath)) ?? new Store(); }
        catch (Exception e) { Debug.LogWarning("BeatGym approvals could not be read: " + e.Message); store = new Store(); }
        if (store.items == null) store.items = new List<Entry>();
    }

    static void Save()
    {
        try { File.WriteAllText(FilePath, JsonUtility.ToJson(store)); }
        catch (Exception e) { Debug.LogWarning("BeatGym approvals could not be saved: " + e.Message); }
    }

    static Entry Find(string title) { Load(); foreach (var e in store.items) if (e.title == title) return e; return null; }

    public static State Of(GymStations.Station s)
    {
        var e = Find(s.title);
        if (e == null) return State.New;
        if (e.names != s.names || e.nums == null || e.nums.Length != s.nums.Count) return State.Changed;
        for (int i = 0; i < e.nums.Length; i++) if (Mathf.Abs(e.nums[i] - s.nums[i]) > Tolerance) return State.Changed;
        return State.Approved;
    }

    public static void Approve(GymStations.Station s, bool save = true)
    {
        var e = Find(s.title);
        if (e == null) { e = new Entry { title = s.title }; store.items.Add(e); }
        e.names = s.names;
        e.nums = s.nums.ToArray();
        if (save) Save();
    }

    public static void Clear(GymStations.Station s)
    {
        var e = Find(s.title);
        if (e != null) { store.items.Remove(e); Save(); }
    }

    public static void ApproveAll(List<GymStations.Station> all) { foreach (var s in all) Approve(s, false); Save(); }
}

/// The component: the station under the feet, the fall line, the menu. On the gym's root, added by the builder
[DefaultExecutionOrder(-50)]   // before the bootstrap reads the respawn key: the station is settled first
public class BeatGymTools : MonoBehaviour
{
    const float FallBelow = 25f, RepeatDelay = 0.35f, RepeatEvery = 0.07f;

    public static bool MenuOpen { get; private set; }
    /// The frame the menu last took an ESC for itself (the pause menu leaves that press alone)
    public static int EscapeFrame { get; private set; } = -1;

    int current = -1;       // the station under the feet (-1: the start, the finish)
    int row, col;           // the menu's cursor: a beat, a difficulty
    bool onlyOpen;          // the list shows only what is new or changed
    float baseKill = float.NaN, restoreScale = 1f;
    float heldSince; int heldDir;
    readonly List<string> beats = new List<string>();   // the rows, in the gym's order
    int[,] cell;                                         // row, difficulty 0..2 → station index, or -1
    GymApprovals.State[] state;
    GUIStyle box, title, hint, nameStyle, tagStyle;
    Texture2D white;

    void OnDisable() { if (MenuOpen) Close(); }
    void OnDestroy() { if (hooked) GameBootstrap.Respawned -= Refill; }

    bool hooked;
    /// A reset in the gym (R, a fall, a station picked from the menu) fills the pips: the whip costs two of three, and a
    /// station that asks for it has to be there to try again at once. The gym only: a level's respawn leaves the pips alone
    void Refill()
    {
        var m = PlayerMotor.Current;
        var pips = m != null ? m.GetComponent<StaminaSystem>() : null;
        if (pips != null && pips.Pips < pips.MaxPips) pips.Refund(pips.MaxPips - pips.Pips);
    }

    void Layout()
    {
        beats.Clear();
        foreach (var s in GymStations.All) if (!beats.Contains(s.beat)) beats.Add(s.beat);
        cell = new int[beats.Count, 3];
        for (int r = 0; r < beats.Count; r++) for (int c = 0; c < 3; c++) cell[r, c] = -1;
        for (int i = 0; i < GymStations.All.Count; i++)
        {
            var s = GymStations.All[i];
            int c = Mathf.Clamp(s.difficulty, 1, 3) - 1;
            cell[beats.IndexOf(s.beat), c] = i;
        }
        Refresh();
    }

    void Refresh()
    {
        state = new GymApprovals.State[GymStations.All.Count];
        for (int i = 0; i < state.Length; i++) state[i] = GymApprovals.Of(GymStations.All[i]);
    }

    bool Shown(int r, int c) => r >= 0 && r < beats.Count && c >= 0 && c < 3 && cell[r, c] >= 0 && (!onlyOpen || state[cell[r, c]] != GymApprovals.State.Approved);
    bool RowShown(int r) => Shown(r, 0) || Shown(r, 1) || Shown(r, 2);

    void Update()
    {
        var kb = Keyboard.current;
        var motor = PlayerMotor.Current;
        var flow = CourseFlow.Instance;
        if (kb == null || motor == null || flow == null || GymStations.All.Count == 0) return;
        if (cell == null) Layout();
        if (!hooked) { GameBootstrap.Respawned += Refill; hooked = true; }
        if (float.IsNaN(baseKill)) baseKill = GameBootstrap.KillFloorY;
        if (PauseMenu.IsPaused || LevelResults.Active) { if (MenuOpen) { MenuOpen = false; } return; }   // the pause owns the clock now: leave it stopped

        if (kb.tabKey.wasPressedThisFrame) { if (MenuOpen) Close(); else Open(); return; }
        if (MenuOpen) { MenuKeys(kb, flow); return; }

        // the station under the feet is the one a reset and a fall go back to
        int k = GymStations.Of(motor.GroundCollider);
        if (k >= 0 && k != current) { current = k; flow.Take(GymStations.All[k].title); }
        GameBootstrap.KillFloorY = current >= 0 ? Mathf.Max(baseKill, GymStations.All[current].lowY - FallBelow) : baseKill;
    }

    void Open()
    {
        Refresh();
        MenuOpen = true;
        heldDir = -1;   // W is all but certainly held as the menu opens: the cursor waits for the keys to come up before it listens
        restoreScale = Time.timeScale;
        Time.timeScale = 0f;
        if (current >= 0)
        {
            var s = GymStations.All[current];
            row = beats.IndexOf(s.beat); col = Mathf.Clamp(s.difficulty, 1, 3) - 1;
        }
        Settle();
    }

    void Close()
    {
        MenuOpen = false;
        Time.timeScale = restoreScale > 0f ? restoreScale : 1f;
    }

    /// The cursor onto something that is listed (the nearest row from where it is, the nearest difficulty in it)
    void Settle()
    {
        if (Shown(row, col)) return;
        for (int d = 0; d < beats.Count; d++)
            foreach (int r in new[] { row + d, row - d })
            {
                if (!RowShown(r)) continue;
                row = r;
                for (int e = 0; e < 3; e++) { if (Shown(r, col + e)) { col += e; return; } if (Shown(r, col - e)) { col -= e; return; } }
            }
    }

    void Step(int dr, int dc)
    {
        if (dr != 0)
        {
            for (int r = row + dr; r >= 0 && r < beats.Count; r += dr)
                if (RowShown(r)) { row = r; Settle(); return; }
        }
        else for (int c = col + dc; c >= 0 && c < 3; c += dc) if (Shown(row, c)) { col = c; return; }
    }

    void MenuKeys(Keyboard kb, CourseFlow flow)
    {
        if (kb.escapeKey.wasPressedThisFrame) { EscapeFrame = Time.frameCount; Close(); return; }
        // the arrows (or WASD), with a repeat when held
        int dir = kb.upArrowKey.isPressed || kb.wKey.isPressed ? 1 : kb.downArrowKey.isPressed || kb.sKey.isPressed ? 2 : kb.leftArrowKey.isPressed || kb.aKey.isPressed ? 3 : kb.rightArrowKey.isPressed || kb.dKey.isPressed ? 4 : 0;
        float now = Time.unscaledTime;
        bool go = false;
        if (heldDir == -1) { if (dir != 0) dir = -1; else heldDir = 0; }
        if (dir == -1) { }
        else if (dir != heldDir) { heldDir = dir; heldSince = now; go = dir != 0; }
        else if (dir != 0 && now - heldSince > RepeatDelay) { heldSince += RepeatEvery; go = true; }
        if (go) Step(dir == 1 ? -1 : dir == 2 ? 1 : 0, dir == 3 ? -1 : dir == 4 ? 1 : 0);

        if (kb.nKey.wasPressedThisFrame) { onlyOpen = !onlyOpen; Settle(); }
        if (!Shown(row, col)) return;
        int k = cell[row, col];
        var s = GymStations.All[k];
        if (kb.xKey.wasPressedThisFrame)
        {
            if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) GymApprovals.ApproveAll(GymStations.All);   // SHIFT + X: the whole row of stations as it stands is the baseline
            else if (state[k] == GymApprovals.State.Approved) GymApprovals.Clear(s);
            else GymApprovals.Approve(s);
            Refresh();
            Settle();
            return;
        }
        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
        {
            Close();
            current = k;
            if (flow.Take(s.title))
            {
                GameBootstrap.RespawnPlayer();   // the game's own respawn: stood on the pad, sent along it
                var look = PlayerMotor.Current != null ? PlayerMotor.Current.look : null;
                if (look != null) look.Face(GameBootstrap.CheckpointForward);   // and the view there at once, not swung round to it
            }
        }
    }

    // ---------------------------------------------------------------- the picture

    void OnGUI()
    {
        if (!MenuOpen || cell == null) return;
        if (box == null)
        {
            white = Texture2D.whiteTexture;
            box = new GUIStyle(GUI.skin.box);
            box.normal.background = Texture2D.blackTexture;
            title = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            hint = new GUIStyle(GUI.skin.label);
            nameStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            tagStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        }
        float u = Screen.height / 1080f;
        title.fontSize = Mathf.RoundToInt(22f * u); hint.fontSize = Mathf.RoundToInt(15f * u); nameStyle.fontSize = Mathf.RoundToInt(17f * u); tagStyle.fontSize = Mathf.RoundToInt(15f * u);
        title.normal.textColor = MenuStyle.Gold; hint.normal.textColor = MenuStyle.Sick;

        int rows = 0, approved = 0, fresh = 0, changed = 0;
        for (int r = 0; r < beats.Count; r++) if (RowShown(r)) rows++;
        foreach (var st in state) { if (st == GymApprovals.State.Approved) approved++; else if (st == GymApprovals.State.New) fresh++; else changed++; }

        float pad = 22f * u, rowH = 34f * u, nameW = 300f * u, cellW = 190f * u, head = 96f * u, foot = 44f * u;
        float w = pad * 2f + nameW + cellW * 3f, h = pad * 2f + head + rowH * Mathf.Max(1, rows) + foot;
        var area = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), white);   // the world dimmed behind it
        GUI.color = Color.white;
        GUI.Box(area, GUIContent.none, box);
        float x = area.x + pad, y = area.y + pad;
        GUI.Label(new Rect(x, y, w - pad * 2f, 32f * u), "THE BEAT GYM   ·   " + (onlyOpen ? "NEW OR CHANGED ONLY" : "EVERY STATION"), title);
        GUI.Label(new Rect(x, y + 34f * u, w - pad * 2f, 24f * u), "ARROWS move   ·   ENTER go there   ·   X approve / take the mark off   ·   N " + (onlyOpen ? "show every station" : "show new or changed only") + "   ·   TAB close", hint);
        GUI.Label(new Rect(x, y + 58f * u, w - pad * 2f, 24f * u), "in play:  R  back to the start of the station you are in   ·   SHIFT + X  approve every station as it stands", hint);
        y += head;

        if (rows == 0)
        {
            nameStyle.normal.textColor = MenuStyle.Paper;
            GUI.Label(new Rect(x, y, w - pad * 2f, rowH), "Nothing new or changed: every station is approved.", nameStyle);
        }
        for (int r = 0; r < beats.Count; r++)
        {
            if (!RowShown(r)) continue;
            nameStyle.normal.textColor = r == row ? MenuStyle.Paper : MenuStyle.Sick;
            GUI.Label(new Rect(x, y, nameW, rowH), beats[r], nameStyle);
            for (int c = 0; c < 3; c++)
            {
                if (!Shown(r, c)) continue;
                int k = cell[r, c];
                var rc = new Rect(x + nameW + cellW * c + 3f * u, y + 3f * u, cellW - 6f * u, rowH - 6f * u);
                bool at = r == row && c == col;
                GUI.color = at ? MenuStyle.Gold : MenuStyle.Panel;
                GUI.DrawTexture(rc, white);
                GUI.color = Color.white;
                var st = state[k];
                string word = st == GymApprovals.State.Approved ? "approved" : st == GymApprovals.State.Changed ? "CHANGED" : "NEW";
                tagStyle.fontStyle = st == GymApprovals.State.Approved ? FontStyle.Normal : FontStyle.Bold;
                tagStyle.normal.textColor = at ? MenuStyle.Ink : st == GymApprovals.State.Approved ? MenuStyle.Sick : st == GymApprovals.State.Changed ? MenuStyle.Rot : MenuStyle.Gold;
                GUI.Label(rc, BeatMath.Roman(c + 1) + "   " + word + (k == current ? "   · here" : ""), tagStyle);
            }
            y += rowH;
        }
        GUI.Label(new Rect(x, area.yMax - pad - 24f * u, w - pad * 2f, 24f * u), approved + " approved   ·   " + fresh + " new   ·   " + changed + " changed", hint);
    }
}
