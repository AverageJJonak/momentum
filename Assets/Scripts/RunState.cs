using UnityEngine;

/// <summary>
/// THE RUN (2026-10-06, runs 2026-10-12; one kit since the simplification 2026-10-16): a run is the game. NEW RUN
/// starts at Level 1; finishing a level advances the run — the next level's index, the total time, the score, the
/// level's own line — and SAVES, so the menu's CONTINUE RUN picks it up where it stands (a continue restarts the
/// level: checkpoints are not saved). The last level's finish ends the run (RUN COMPLETE) and records the best run.
/// Static so it survives scene loads, mirrored in PlayerPrefs under run_* so it survives a quit. There is nothing to
/// choose: the knuckles, the kick, the parry and the whip are every run's.
///
/// FREE PLAY is the Programme page: a level launched from a card for itself. FreePlay is set (never saved) so the
/// bill never touches the run; the menu clears it.
/// </summary>
public static class RunState
{
    // The run: every key is run_*, read back by Load
    const string ActiveKey = "run_active", CompleteKey = "run_complete", LevelKey = "run_level", TimeKey = "run_time",
        ScoreKey = "run_score", StartedKey = "run_started", ReachedKey = "run_reached";
    // Per-level results: run_lv{i}_time / _score / _grade, present only for the levels the run has finished
    const string LevelPrefix = "run_lv";
    // The best run: run_best_time, run_best_score
    const string BestTimeKey = "run_best_time", BestScoreKey = "run_best_score";

    /// One level's line on the run: its time, its bill's score and grade
    public struct LevelResult { public bool done; public float time; public int score; public string grade; }

    /// A level played for itself from the Programme (or anywhere off the run's path): nothing here is saved while it
    /// is set. The menu clears it.
    public static bool FreePlay { get; set; }

    /// A run exists (in progress or complete)
    public static bool Active { get; private set; }
    /// The run's last level was finished: the run is over, its record kept until NEW RUN
    public static bool Complete { get; private set; }
    /// The index into LevelCatalog.All of the NEXT level to play (the one in progress while it is being played)
    public static int Level { get; private set; }
    public static float TotalTime { get; private set; }
    public static int Score { get; private set; }
    /// When the run began, as a date string
    public static string StartedAt { get; private set; } = "";
    /// The high-water mark across every run: the highest level index any run reached (free play opens up to it)
    public static int Reached { get; private set; }

    static LevelResult[] results;

    /// A run exists and is not over: the menu's CONTINUE RUN
    public static bool InProgress => Active && !Complete;
    /// This play counts toward the run: a run in progress, and the level is being played for it (not free play)
    public static bool InRun => Active && !Complete && !FreePlay;
    /// How many levels a run is
    public static int LevelCount => LevelCatalog.All.Length;
    /// The scene a continue loads: the run's next level
    public static string NextScene => LevelCatalog.All[Mathf.Clamp(Level, 0, LevelCatalog.All.Length - 1)].scene;
    /// The run's line for a level (done = false until the run finished it)
    public static LevelResult Result(int level) => results != null && level >= 0 && level < results.Length ? results[level] : default;
    /// How many levels the run has finished
    public static int LevelsDone
    {
        get
        {
            int n = 0;
            if (results != null) foreach (var r in results) if (r.done) n++;
            return n;
        }
    }

    /// NEW RUN: Level 1, the clock and the score at nothing — saved at once
    public static void NewRun()
    {
        Active = true;
        Complete = false;
        Level = 0;
        TotalTime = 0f;
        Score = 0;
        StartedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        FreePlay = false;
        results = new LevelResult[LevelCount];
        Reached = Mathf.Max(Reached, 0);
        Save();
    }

    /// A level finished in the run: its line is written, the totals grow, the run moves to the next level — saved
    public static void Advance(int levelIndex, float time, int score, string grade)
    {
        if (!InRun || levelIndex < 0 || levelIndex >= LevelCount) return;
        if (results == null || results.Length != LevelCount) results = new LevelResult[LevelCount];
        results[levelIndex] = new LevelResult { done = true, time = time, score = score, grade = grade };
        TotalTime += time;
        Score += score;
        Level = levelIndex + 1;
        Reached = Mathf.Max(Reached, Mathf.Min(Level, LevelCount - 1));
        Save();
    }

    /// RUN COMPLETE: the run is over; the fastest run and the best score go on record
    public static void Finish()
    {
        if (!Active || Complete) return;
        Complete = true;
        Level = LevelCount;
        float best = PlayerPrefs.GetFloat(BestTimeKey, -1f);
        if (best < 0f || TotalTime < best) PlayerPrefs.SetFloat(BestTimeKey, TotalTime);
        if (Score > PlayerPrefs.GetInt(BestScoreKey, -1)) PlayerPrefs.SetInt(BestScoreKey, Score);
        Save();
    }

    /// The run is given up: nothing in progress, the best-run records untouched
    public static void Abandon()
    {
        Active = false;
        Complete = false;
        Level = 0;
        TotalTime = 0f;
        Score = 0;
        StartedAt = "";
        results = new LevelResult[LevelCount];
        Save();
    }

    /// The fastest complete run (-1 if none), and the best score of all
    public static float BestTime => PlayerPrefs.GetFloat(BestTimeKey, -1f);
    public static int BestScore => PlayerPrefs.GetInt(BestScoreKey, -1);

    /// A rehearsal (the back door): the scene played for itself, the saved run untouched
    public static void Rehearse() { FreePlay = true; }

    /// The bootstrap, on a campaign level: the run's next level is played for the run; any other level is played for
    /// itself (free play) — and Level 1 with no run in progress starts one, as the menu would
    public static void Arrive(int levelIndex)
    {
        if (FreePlay || levelIndex < 0) return;
        if (!Active || Complete) { if (levelIndex == 0) NewRun(); else FreePlay = true; return; }
        if (Level != levelIndex) FreePlay = true;
    }

    /// A fresh process (or a scene load) picks the run back up where the last save left it
    public static void Load()
    {
        Active = PlayerPrefs.GetInt(ActiveKey, 0) == 1;
        Complete = PlayerPrefs.GetInt(CompleteKey, 0) == 1;
        Level = PlayerPrefs.GetInt(LevelKey, 0);
        TotalTime = PlayerPrefs.GetFloat(TimeKey, 0f);
        Score = PlayerPrefs.GetInt(ScoreKey, 0);
        StartedAt = PlayerPrefs.GetString(StartedKey, "");
        Reached = PlayerPrefs.GetInt(ReachedKey, 0);
        int n = LevelCount;
        if (results == null || results.Length != n) results = new LevelResult[n];
        for (int i = 0; i < n; i++)
        {
            string k = LevelPrefix + i;
            bool done = PlayerPrefs.HasKey(k + "_time");
            results[i] = done
                ? new LevelResult { done = true, time = PlayerPrefs.GetFloat(k + "_time", 0f), score = PlayerPrefs.GetInt(k + "_score", 0), grade = PlayerPrefs.GetString(k + "_grade", "D") }
                : default;
        }
    }

    static void Save()
    {
        PlayerPrefs.SetInt(ActiveKey, Active ? 1 : 0);
        PlayerPrefs.SetInt(CompleteKey, Complete ? 1 : 0);
        PlayerPrefs.SetInt(LevelKey, Level);
        PlayerPrefs.SetFloat(TimeKey, TotalTime);
        PlayerPrefs.SetInt(ScoreKey, Score);
        PlayerPrefs.SetString(StartedKey, StartedAt ?? "");
        PlayerPrefs.SetInt(ReachedKey, Reached);
        int n = results != null ? results.Length : 0;
        for (int i = 0; i < Mathf.Max(n, LevelCount); i++)
        {
            string k = LevelPrefix + i;
            if (i < n && results[i].done)
            {
                PlayerPrefs.SetFloat(k + "_time", results[i].time);
                PlayerPrefs.SetInt(k + "_score", results[i].score);
                PlayerPrefs.SetString(k + "_grade", results[i].grade ?? "D");
            }
            else
            {
                PlayerPrefs.DeleteKey(k + "_time");
                PlayerPrefs.DeleteKey(k + "_score");
                PlayerPrefs.DeleteKey(k + "_grade");
            }
        }
        PlayerPrefs.Save();
    }
}
