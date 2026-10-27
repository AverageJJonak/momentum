using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// THE BILL: the end-of-scene bill of performance. Three things are scored out of 100:
///  · STYLE  (35) — the rank you HELD across the scene (time-weighted) plus the
///                  peak you reached; cruising at D scores nothing, living at S+
///                  scores it all.
///  · TIME   (30) — your time against the scene's gold and par: gold or better is
///                  full marks, par or slower is zero, linear between.
///  · COMBAT (35) — the share of the scene's staff you put down, and how little
///                  damage you took (two health bars' worth = zero); every down
///                  costs five.
/// The finish portal hands over to this screen instead of loading straight
/// through; the best score is saved per scene and shown in the programme.
///
/// Printed as the House's bill: a night veil with projector grain, a stepped
/// brass frame with plate corners around the page, the band with its
/// searchlight sweep and drop-frames, three cream lines (TIME, STYLE, COMBAT)
/// that land and print with their figures right-aligned on brass rules, the
/// total under SETTLED with the company's sun behind it, the grade a red wax
/// seal whose slam shakes the page. A second after the seal, the rot may take a
/// line's LABEL for a moment — never the figures, the rules or the seal: the
/// result is the player's, only the commentary is the House's.
/// </summary>
public class LevelResults : MonoBehaviour
{
    public static LevelResults Instance { get; private set; }
    /// True while the bill is up: gameplay, the HUD and Esc all stand down
    public static bool Active { get; private set; }

    public StyleSystem style;
    public LevelTimer timer;
    public CombatDirector combat;
    public PlayerHealth health;

    public const float StyleMax = 35f, SpeedMax = 30f, CombatMax = 35f;
    const float CountUpTime = 1.2f;   // the total rolls up over this long
    const float MinShowTime = 0.8f;   // no accidental skips

    /// Per-scene time targets (seconds) from the catalog: full time marks at gold, none at par
    static (float gold, float par) Targets(string scene)
        => LevelCatalog.TryFind(scene, out var info) ? (info.gold, info.par) : (60f, 200f);

    int totalEnemies;
    string nextScene;
    float shownAt;
    bool stamped;

    // The report
    float styleScore, speedScore, combatScore;
    int total;
    string grade = "D";
    string styleLine = "", speedLine = "", combatLine = "";
    int previousBest = -1;
    bool newBest;
    // Built once in Finish(): the document line under the title and the best-score tag
    string docLine = "BILL OF PERFORMANCE";
    string bestLine = "";
    const string MemoLine = "PRESENTED WITH THE COMPLIMENTS OF THE HOUSE  ·  FORM 09";
    // Text that only changes while a number rolls: rebuilt when the shown value changes, never per frame
    readonly string[] valueText = new string[3];
    readonly int[] valueShown = { -1, -1, -1 };
    string totalText = "0";
    int totalShown = -1;
    // The band's drop-frame and the lines' rot: advanced only in Update, read in OnGUI
    MenuStyle.RotClock bandClock, rotClock;
    // Own styles: the menu kit's are left-aligned and shared, and the bill needs right-aligned values,
    // a centred seal letter and a number size of its own (in the condensed face: stamped figures, not a masthead)
    GUIStyle numberStyle, stampStyle, valueStyle, memoStyle, runValueStyle;

    // THE RUN: the scene's place in the catalog (the run counts levels by it) and whether this bill advances the run
    int levelIndex = -1;
    bool inRun;
    // RUN COMPLETE: the card after the last level's bill — every line it prints is built once when it opens
    bool runCard;
    string runDocLine = "", runOutOfText = "", runTimeLine = "", runClassLine = "", runBestLine = "", runGrade = "D";
    bool runNewBest;
    string[] runLineL, runLineR;
    int runLines;
    const string RunMemoLine = "SETTLED IN FULL  ·  THE HOUSE THANKS YOU FOR YOUR CUSTOM  ·  FORM 11";

    void Start()
    {
        Instance = this;
        Active = false;
        // Every member of staff the builder placed: the kill share is measured against this
        totalEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length;
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;
        Active = false;
    }

    /// Called by the finish portal: score the run, freeze the game, show the bill
    public void Finish(string next)
    {
        if (Active) return;
        nextScene = next;
        runCard = false;
        Compute();

        string scene = SceneManager.GetActiveScene().name;
        levelIndex = LevelCatalog.IndexOf(scene);
        inRun = RunState.InRun; // decided here, before anything else can change it: this bill is the run's, or the level's own
        if (CombatBaseline.Instance != null) CombatBaseline.Instance.Report(scene);   // the scene's numbers, to the console and PlayerPrefs
        string key = "score_" + scene;
        previousBest = PlayerPrefs.GetInt(key, -1);
        newBest = total > previousBest;
        if (newBest)
        {
            PlayerPrefs.SetInt(key, total);
            PlayerPrefs.Save();
        }

        // Everything the bill prints as text is set here, once
        bestLine = newBest ? "NEW BEST" : previousBest >= 0 ? "BEST " + previousBest : "";
        docLine = inRun ? DocLine(scene) : DocLine(scene) + "  ·  FREE PLAY"; // a level played for itself says so on its bill
        for (int i = 0; i < valueShown.Length; i++) valueShown[i] = -1;
        totalShown = -1;
        bandClock = default;
        rotClock = default;

        Active = true;
        stamped = false;
        shownAt = Time.unscaledTime;
        MenuStyle.KickAt = shownAt; // the title's plates kick on open, like every page
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// 'BILL OF PERFORMANCE · ACT I · SCENE 2 · VERTIGO' — or just the heading when the scene is off the catalog
    static string DocLine(string scene)
    {
        int chapter = LevelCatalog.ChapterOf(scene);
        if (chapter < 0 || !LevelCatalog.TryFind(scene, out var info)) return "BILL OF PERFORMANCE";
        var ch = LevelCatalog.Chapters[chapter];
        int index = 0;
        for (int i = 0; i < ch.count; i++)
            if (LevelCatalog.All[ch.first + i].scene == scene) { index = i; break; }
        return $"BILL OF PERFORMANCE · {ch.name} · SCENE {index + 1} · {info.title}";
    }

    void Compute()
    {
        string scene = SceneManager.GetActiveScene().name;
        string[] names = { "D", "C", "B", "A", "S", "S+" };

        // STYLE: 25 for the rank you held, 10 for the peak
        float avgRank = style != null ? style.AverageRank : 0f;
        int peak = style != null ? Mathf.Clamp(style.PeakRankIndex, 0, 5) : 0;
        styleScore = 25f * Mathf.Clamp01(avgRank / 5f) + 10f * (peak / 5f);
        styleLine = $"held {names[Mathf.Clamp(Mathf.RoundToInt(avgRank), 0, 5)]} on average  ·  peak {names[peak]}"
                    + (style != null && style.FlowSeconds > 0.5f ? $"  ·  {style.FlowSeconds:0}s in flow" : "");

        // TIME: gold = all of it, par = none
        float t = timer != null ? timer.Elapsed : 0f;
        var (gold, par) = Targets(scene);
        speedScore = SpeedMax * Mathf.Clamp01((par - t) / Mathf.Max(1f, par - gold));
        speedLine = $"{LevelTimer.Format(t)}  ·  gold {LevelTimer.Format(gold)}  ·  par {LevelTimer.Format(par)}";

        // COMBAT: 20 for the kill share, 15 for staying whole, −5 per down
        int kills = combat != null ? combat.Kills : 0;
        float killShare = totalEnemies > 0 ? Mathf.Clamp01(kills / (float)totalEnemies) : 1f;
        float dmg = health != null ? health.TotalDamage : 0f;
        float maxHp = health != null ? Mathf.Max(1f, health.Max) : 100f;
        int downs = health != null ? health.Downs : 0;
        combatScore = Mathf.Clamp(20f * killShare + 15f * Mathf.Clamp01(1f - dmg / (2f * maxHp)) - 5f * downs, 0f, CombatMax);
        combatLine = $"{kills} / {totalEnemies} staff  ·  {dmg:0} damage taken" + (downs > 0 ? $"  ·  {downs} down" : "");

        total = Mathf.Clamp(Mathf.RoundToInt(styleScore + speedScore + combatScore), 0, 100);
        grade = LevelCatalog.Grade(total);
    }

    void Update()
    {
        if (!Active) return;
        float now = Time.unscaledTime;
        float age = now - shownAt;

        if (!stamped && age >= CountUpTime)
        {
            stamped = true;
            if (GameFeel.Instance != null)
                GameFeel.Instance.PlayRankUp(grade == "S" ? 5 : grade == "A" ? 4 : grade == "B" ? 3 : grade == "C" ? 2 : 1);
        }

        // The band's drop-frame, and — a second after the seal lands — the rot over the three line labels
        MenuStyle.AdvanceRot(ref bandClock, now, 12f, 1, -1, -1, 0.1f);
        MenuStyle.AdvanceRot(ref rotClock, now, 10f, stamped && age >= CountUpTime + 1f ? 3 : 0, -1);

        if (age < MinShowTime) return;
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        bool go = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))
                  || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        if (go) Continue();
    }

    void Continue()
    {
        if (runCard) { Leave("MainMenu"); return; } // RUN COMPLETE dismissed: the directory, the run on the slip

        if (inRun && levelIndex >= 0)
        {
            // THE RUN moves on: this level's line is written and saved, and the next level in the programme's order
            // follows — or, past the last, the run is complete: its card comes up in the bill's place
            float prevBestT = RunState.BestTime;
            int prevBestS = RunState.BestScore;
            RunState.Advance(levelIndex, timer != null ? timer.Elapsed : 0f, total, grade);
            if (RunState.Level >= LevelCatalog.All.Length)
            {
                RunState.Finish();
                OpenRunCard(prevBestT, prevBestS);
                return;
            }
            Leave(LevelCatalog.All[RunState.Level].scene);
            return;
        }

        if (levelIndex >= 0)
        {
            // FREE PLAY (or a level off the run's path): the level's bests are kept, the run untouched — back to the
            // programme on this level's act, record in hand
            MainMenuUI.StartOnLevels = true;
            MainMenuUI.StartChapter = LevelCatalog.ChapterOfIndex(levelIndex);
            Leave("MainMenu");
            return;
        }
        Leave(nextScene); // a scene off the catalog: wherever its finish door points
    }

    void Leave(string scene)
    {
        Active = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(scene);
    }

    /// RUN COMPLETE: the card after the last bill — one line per level (the act, the scene, the title; its time, score
    /// and grade), the run's score rolling up, its clock, its class, and the best-run tag. Everything built here, once.
    void OpenRunCard(float prevBestT, int prevBestS)
    {
        runCard = true;
        int n = LevelCatalog.All.Length;
        if (runLineL == null || runLineL.Length != n) { runLineL = new string[n]; runLineR = new string[n]; }
        runLines = n;
        for (int i = 0; i < n; i++)
        {
            var r = RunState.Result(i);
            LevelCatalog.Place(i, out int c, out int no);
            runLineL[i] = (c >= 0 ? LevelCatalog.Chapters[c].name + "  ·  " : "") + "SCENE " + no + "  ·  " + LevelCatalog.All[i].title;
            runLineR[i] = r.done ? LevelTimer.Format(r.time) + "  ·  " + r.score + " / 100  ·  " + r.grade : "—";
        }
        runGrade = LevelCatalog.Grade(RunState.Score / Mathf.Max(1, RunState.LevelsDone));
        runOutOfText = "OUT OF " + (n * 100);
        runTimeLine = "TOTAL TIME  ·  " + LevelTimer.Format(RunState.TotalTime);
        runClassLine = "BEGUN  ·  " + RunState.StartedAt;
        runDocLine = "RUN COMPLETE  ·  " + LevelTimer.FormatShort(RunState.TotalTime) + "  ·  " + RunState.Score;
        bool fastest = prevBestT < 0f || RunState.TotalTime < prevBestT;
        bool highest = prevBestS < 0 || RunState.Score > prevBestS;
        runNewBest = fastest || highest;
        runBestLine = fastest && highest ? "FASTEST RUN  ·  BEST SCORE" : fastest ? "FASTEST RUN" : highest ? "BEST SCORE"
            : "BEST " + LevelTimer.FormatShort(prevBestT) + "  ·  " + prevBestS;
        grade = runGrade; // the seal's letter, and the stamp's note
        totalShown = -1;
        bandClock = default;
        rotClock = default;
        stamped = false;
        shownAt = Time.unscaledTime;
        MenuStyle.KickAt = shownAt;
        if (GameFeel.Instance != null) GameFeel.Instance.PlayFinish();
    }

    void OnGUI()
    {
        if (!Active) return;
        MenuStyle.EnsureStyles();
        MenuStyle.Use(MenuStyle.LevelSkin); // the bill is printed by the property that graded you
        if (numberStyle == null) MakeStyles();
        UIScale.Begin();
        float now = Time.unscaledTime;
        float age = now - shownAt;
        float sinceStamp = age - CountUpTime;
        GUI.depth = -10; // over the HUD
        float w = UIScale.W, h = UIScale.H;
        if (runCard) { DrawRunCard(w, h, now, age, sinceStamp); UIScale.End(); return; }

        // The veil, the grain, the kick, the frame, the band and THE BILL on it
        float x = w * 0.09f;
        float y = Chrome(w, h, now, age, sinceStamp, "THE BILL", docLine, out float bandY);
        DrawDocTag(x, w, bandY, "THE BILL", docLine);
        MenuStyle.BandDropFrame(in bandClock, w, bandY);

        // The three lines, sliding in one after another: TIME, STYLE, COMBAT
        float rowW = Mathf.Min(600f, w * 0.52f);
        float rowH = 66f;
        DrawRow(0, x, y, rowW, rowH, "TIME", speedScore, SpeedMax, speedLine, Slide(age, 0f), now);
        y += rowH + 16f;
        DrawRow(1, x, y, rowW, rowH, "STYLE", styleScore, StyleMax, styleLine, Slide(age, 0.12f), now);
        y += rowH + 16f;
        DrawRow(2, x, y, rowW, rowH, "COMBAT", combatScore, CombatMax, combatLine, Slide(age, 0.24f), now);
        y += rowH + 16f;

        // The right column stacks top-down: SETTLED over the total as it rolls up, OUT OF 100 under it,
        // then the wax seal below with the best-score tag beside it
        float roll = 1f - Mathf.Pow(1f - Mathf.Clamp01(age / CountUpTime), 3f);
        int shown = Mathf.RoundToInt(total * roll);
        if (shown != totalShown) { totalShown = shown; totalText = shown.ToString(); }
        float tx = Mathf.Max(x + rowW + 40f, w * 0.66f);
        float ty = bandY + 128f;
        numberStyle.fontSize = (int)Mathf.Min(150f, h * 0.17f);
        float numH = numberStyle.fontSize * 1.25f; // room for the glyphs, no spill onto the line below
        Rect totalRect = new Rect(tx, ty, w - tx - 30f, numH);
        // SETTLED over the figure, ruled; the company's sun turning behind the number; the plates shiver while it
        // rolls and snap into register when it lands
        MenuStyle.DrawRotLabel(new Rect(tx + 8f, ty - 30f, 300f, 30f), -6f, "SETTLED", MenuStyle.Item, MenuStyle.S.accent);
        {
            Matrix4x4 saved = GUI.matrix;
            UIScale.RotateAround(-6f, new Vector2(tx + 158f, ty - 15f));
            MenuStyle.DoubleRule(tx + 8f, ty - 2f, 300f, A(MenuStyle.S.accent, 0.8f));
            GUI.matrix = saved;
            GUI.color = Color.white;
        }
        MenuStyle.SlowBurst(totalRect.center, 520f, 4f, A(MenuStyle.Gold, 0.12f));
        MenuStyle.DrawRotLabel(new Rect(totalRect.x + 5f, totalRect.y + 5f, totalRect.width, totalRect.height), -6f,
            totalText, numberStyle, new Color(0f, 0f, 0f, 0.7f));
        float o = roll < 1f ? 5f : 1f;
        MenuStyle.DrawRotLabel(new Rect(totalRect.x - o, totalRect.y, totalRect.width, totalRect.height), -6f,
            totalText, numberStyle, A(MenuStyle.Oxblood, 0.85f));
        MenuStyle.DrawRotLabel(new Rect(totalRect.x + o, totalRect.y + 1f, totalRect.width, totalRect.height), -6f,
            totalText, numberStyle, A(MenuStyle.DeepTeal, 0.85f));
        MenuStyle.DrawRotLabel(totalRect, -6f, totalText, numberStyle, MenuStyle.Paper);
        float oy = ty + numH + 4f;
        MenuStyle.DrawRotLabel(new Rect(tx + 8f, oy, 300f, 34f), -6f, "OUT OF 100", MenuStyle.Item, MenuStyle.S.accent);

        if (stamped)
        {
            // The seal: the sun bursts, then the wax lands with the grade pressed into it and the punch settles
            float punch = 1f + 0.35f * Mathf.Pow(1f - Mathf.Clamp01(sinceStamp / 0.14f), 2f);
            Vector2 pivot = new Vector2(tx + 70f, oy + 34f + 20f + 70f); // a 140 px seal, 20 px under OUT OF 100
            MenuStyle.StampBurst(pivot, 140f, sinceStamp / 0.25f, A(MenuStyle.Foil, 0.6f));
            MenuStyle.WaxSeal(pivot, 140f, grade, stampStyle, 1f, punch, -8f);
            if (bestLine.Length > 0)
            {
                Rect br = new Rect(tx + 160f, pivot.y - 15f, 400f, 30f);
                MenuStyle.DrawRotLabel(br, -8f, bestLine, MenuStyle.Item, newBest ? MenuStyle.Gold : MenuStyle.Paper);
                if (newBest)
                {
                    // A brass rule draws itself under the new record
                    Matrix4x4 saved = GUI.matrix;
                    UIScale.RotateAround(-8f, br.center);
                    Fill(new Rect(br.x, br.yMax, 150f * Mathf.Clamp01(sinceStamp / 0.2f), 2f), MenuStyle.Gold);
                    GUI.matrix = saved;
                    GUI.color = Color.white;
                }
            }
        }

        // The memo line, right-anchored at the margin in a box that runs from clear of the prompt: one line, measured
        GUI.color = A(MenuStyle.S.subDim, 0.9f);
        MenuStyle.FitLabel(new Rect(x + 380f, h - 46f, w * 0.91f - x - 380f, 18f), MemoLine, memoStyle);
        GUI.color = Color.white;

        // Footer: the company's mark, steady, and the prompt blinking beside it
        if (age >= MinShowTime)
        {
            GUI.color = MenuStyle.Gold;
            GUI.DrawTexture(new Rect(x, h - 52f, 14f, 14f), GameFont.Logo);
            GUI.color = Color.white;
            if (Mathf.Repeat(age, 1f) < 0.7f)
            {
                GUI.color = MenuStyle.Paper;
                UIScale.Label(new Rect(x, h - 60f, w * 0.8f, 30f), "ENTER · CLICK · SPACE  —  CONTINUE", MenuStyle.Tagline);
                GUI.color = Color.white;
            }
        }
        UIScale.End();
    }

    /// The tag at the document line's right end, in the title's own frame: a double rule. The line is measured
    /// once per bill; the seat maths mirrors TitleBlock's so the tag sits on the same line.
    void DrawDocTag(float x, float w, float bandY, string title, string doc)
    {
        // The line's width at the rasterised size (kept by MenuStyle), capped at the box TitleBlock fits it to
        float docLineW = Mathf.Min(MenuStyle.FitWidth(MenuStyle.Doc, doc), MenuStyle.DocLineWidth - 2f);
        Vector2 titleSize = MenuStyle.TitleSize(title);
        const float angle = MenuStyle.BandAngle;
        float centerX = x + titleSize.x * 0.5f;
        float bandLineY = bandY + (centerX - w * 0.5f) * Mathf.Tan(angle * Mathf.Deg2Rad);
        Rect r = new Rect(x, bandLineY - titleSize.y * 0.5f, titleSize.x, titleSize.y);
        float ruleY = r.center.y + MenuStyle.Masthead.fontSize * 0.42f;
        float docY = Mathf.Min(ruleY + 9f, r.center.y + 41f);
        Matrix4x4 saved = GUI.matrix;
        UIScale.RotateAround(angle, r.center);
        MenuStyle.DoubleRule(r.x + 6f + docLineW + 12f, docY + 5f, 90f, A(MenuStyle.Ink, 0.8f));
        GUI.matrix = saved;
        GUI.color = Color.white;
    }

    void MakeStyles()
    {
        numberStyle = new GUIStyle(MenuStyle.Item) { alignment = TextAnchor.MiddleLeft };
        stampStyle = new GUIStyle(MenuStyle.Item) { alignment = TextAnchor.MiddleCenter, fontSize = 92 };
        valueStyle = new GUIStyle(MenuStyle.ItemSelected) { alignment = TextAnchor.MiddleRight };
        memoStyle = new GUIStyle(MenuStyle.Doc) { alignment = TextAnchor.MiddleRight };
        runValueStyle = new GUIStyle(MenuStyle.Small) { alignment = TextAnchor.MiddleRight };
    }

    /// The card's chrome, shared by the bill and RUN COMPLETE: the veil over the frozen frame and grain that re-seats
    /// eight times a second; the page kick (the seal's slam shoves the card down 6 px and it settles back — the veil
    /// stays put); the stepped brass frame with plate corners, under the band like letterhead; the band, whose first
    /// sweep crosses the title while the lines throw in; the title and the document line. Returns where the lines
    /// stack from.
    float Chrome(float w, float h, float now, float age, float sinceStamp, string title, string doc, out float bandY)
    {
        Rect full = new Rect(0f, 0f, w, h);
        Fill(full, A(MenuStyle.Veil, 0.84f));
        int frame = Mathf.FloorToInt(now * 8f);
        GUI.color = new Color(1f, 1f, 1f, 0.14f);
        MenuArt.Tile(full, MenuArt.GrainTex, 1f, new Vector2(MenuArt.Hash01(frame, 1, 9) * 128f, MenuArt.Hash01(frame, 2, 9) * 128f));
        GUI.color = Color.white;
        if (stamped && sinceStamp < 0.14f)
        {
            float kick = 1f - Mathf.Clamp01(sinceStamp / 0.14f);
            GUI.matrix *= Matrix4x4.Translate(new Vector3(0f, 6f * kick * kick, 0f));
        }
        Rect page = new Rect(14f, 14f, w - 28f, h - 28f);
        // THE BORROWED FRAME (2026-10-27): the plated corners of Kenney's border round the page in the skin's accent; the drawn frame without it
        if (!UIAssets.Frame(page, UIAssets.Border("border-029"), 28f, A(MenuStyle.S.accent, 0.6f)))
        {
            MenuStyle.StepFrame(page, A(MenuStyle.S.accent, 0.45f), 1f, 8f, 2);
            MenuStyle.CornerPlates(page, A(MenuStyle.S.accent, 0.5f), 6f, 20f);
        }
        bandY = h * 0.2f;
        MenuStyle.StripeBand(w, bandY, MenuStyle.BandAngle, 1f, age);
        MenuStyle.Title.fontSize = (int)Mathf.Min(96f, h * 0.11f);
        return MenuStyle.TitleBlock(w * 0.09f, w, bandY, title, MenuStyle.BandAngle, doc) + 26f;
    }

    /// RUN COMPLETE: the bill's chrome; one cream strip per level down the left, thrown in one after another — the
    /// act, the scene and the title, with the time, the score and the grade right-aligned (a dash for a level the run
    /// never finished); the run's score rolling up on the right with the clock and the class under it; the seal
    /// pressed with the run's grade and the best-run tag beside it.
    void DrawRunCard(float w, float h, float now, float age, float sinceStamp)
    {
        float x = w * 0.09f;
        float y = Chrome(w, h, now, age, sinceStamp, "RUN COMPLETE", runDocLine, out float bandY);
        DrawDocTag(x, w, bandY, "RUN COMPLETE", runDocLine);
        MenuStyle.BandDropFrame(in bandClock, w, bandY);

        float rowW = Mathf.Min(640f, w * 0.54f);
        float rowH = Mathf.Clamp((h - 70f - y) / Mathf.Max(1, runLines), 18f, 32f);
        const float tilt = MenuStyle.RowTilt;
        for (int i = 0; i < runLines; i++)
        {
            float slide = Slide(age, i * 0.05f);
            if (slide <= 0f) continue;
            float ease = MenuStyle.Ease(slide);
            float a = Mathf.Clamp01(ease);
            Rect r = new Rect(x - (1f - ease) * 340f, y + i * rowH, rowW, rowH - 4f);
            bool landing = ease >= 0.9f && ease < 1f;
            Color panel = landing ? MenuStyle.Foil : MenuStyle.S.sheet;
            panel.a = a;
            MenuStyle.DrawRotRect(new Rect(r.x + 4f, r.y + 4f, r.width, r.height), tilt, new Color(0f, 0f, 0f, 0.5f * a));
            MenuStyle.DrawRotRect(r, tilt, panel);
            Matrix4x4 saved = GUI.matrix;
            UIScale.RotateAround(tilt, r.center);
            Fill(new Rect(r.x, r.y, 5f, r.height), A(MenuStyle.S.stripe, a));
            GUI.color = A(MenuStyle.Ink, a);
            MenuStyle.FitLabel(new Rect(r.x + 16f, r.y, r.width * 0.56f, r.height), runLineL[i], MenuStyle.Small);
            GUI.color = A(MenuStyle.S.sheetDim, 0.95f * a);
            MenuStyle.FitLabel(new Rect(r.x + r.width * 0.56f, r.y, r.width * 0.44f - 14f, r.height), runLineR[i], runValueStyle);
            GUI.matrix = saved;
            GUI.color = Color.white;
        }

        // The right column: SETTLED over the run's score as it rolls up, OUT OF under it, the clock and the class,
        // then the seal with the best-run tag beside it
        float roll = 1f - Mathf.Pow(1f - Mathf.Clamp01(age / CountUpTime), 3f);
        int shown = Mathf.RoundToInt(RunState.Score * roll);
        if (shown != totalShown) { totalShown = shown; totalText = shown.ToString(); }
        float tx = Mathf.Max(x + rowW + 40f, w * 0.68f);
        float ty = bandY + 128f;
        numberStyle.fontSize = (int)Mathf.Min(120f, h * 0.14f);
        float numH = numberStyle.fontSize * 1.25f;
        Rect totalRect = new Rect(tx, ty, w - tx - 30f, numH);
        MenuStyle.DrawRotLabel(new Rect(tx + 8f, ty - 30f, 300f, 30f), -6f, "SETTLED", MenuStyle.Item, MenuStyle.S.accent);
        {
            Matrix4x4 saved = GUI.matrix;
            UIScale.RotateAround(-6f, new Vector2(tx + 158f, ty - 15f));
            MenuStyle.DoubleRule(tx + 8f, ty - 2f, 300f, A(MenuStyle.S.accent, 0.8f));
            GUI.matrix = saved;
            GUI.color = Color.white;
        }
        MenuStyle.SlowBurst(totalRect.center, 520f, 4f, A(MenuStyle.Gold, 0.12f));
        MenuStyle.DrawRotLabel(new Rect(totalRect.x + 5f, totalRect.y + 5f, totalRect.width, totalRect.height), -6f,
            totalText, numberStyle, new Color(0f, 0f, 0f, 0.7f));
        float o = roll < 1f ? 5f : 1f;
        MenuStyle.DrawRotLabel(new Rect(totalRect.x - o, totalRect.y, totalRect.width, totalRect.height), -6f,
            totalText, numberStyle, A(MenuStyle.Oxblood, 0.85f));
        MenuStyle.DrawRotLabel(new Rect(totalRect.x + o, totalRect.y + 1f, totalRect.width, totalRect.height), -6f,
            totalText, numberStyle, A(MenuStyle.DeepTeal, 0.85f));
        MenuStyle.DrawRotLabel(totalRect, -6f, totalText, numberStyle, MenuStyle.Paper);
        float oy = ty + numH + 4f;
        MenuStyle.DrawRotLabel(new Rect(tx + 8f, oy, 300f, 34f), -6f, runOutOfText, MenuStyle.Item, MenuStyle.S.accent);
        {
            // The clock and the class, one line each, measured into what the column has
            Matrix4x4 saved = GUI.matrix;
            UIScale.RotateAround(-6f, new Vector2(tx + 158f, oy + 60f));
            GUI.color = MenuStyle.Paper;
            MenuStyle.FitLabel(new Rect(tx + 8f, oy + 38f, w - tx - 48f, 26f), runTimeLine, MenuStyle.Item);
            GUI.color = A(MenuStyle.Paper, 0.8f);
            MenuStyle.FitLabel(new Rect(tx + 8f, oy + 64f, w - tx - 48f, 22f), runClassLine, MenuStyle.Small);
            GUI.matrix = saved;
            GUI.color = Color.white;
        }

        if (stamped)
        {
            float punch = 1f + 0.35f * Mathf.Pow(1f - Mathf.Clamp01(sinceStamp / 0.14f), 2f);
            Vector2 pivot = new Vector2(tx + 70f, oy + 92f + 20f + 70f); // a 140 px seal, 20 px under the class line
            MenuStyle.StampBurst(pivot, 140f, sinceStamp / 0.25f, A(MenuStyle.Foil, 0.6f));
            MenuStyle.WaxSeal(pivot, 140f, runGrade, stampStyle, 1f, punch, -8f);
            Rect br = new Rect(tx + 160f, pivot.y - 15f, w - tx - 190f, 30f);
            Matrix4x4 saved = GUI.matrix;
            UIScale.RotateAround(-8f, br.center);
            GUI.color = runNewBest ? MenuStyle.Gold : MenuStyle.Paper;
            float drawn = MenuStyle.FitLabel(br, runBestLine, MenuStyle.Item);
            if (runNewBest) Fill(new Rect(br.x, br.yMax, Mathf.Min(drawn, 150f) * Mathf.Clamp01(sinceStamp / 0.2f), 2f), MenuStyle.Gold);
            GUI.matrix = saved;
            GUI.color = Color.white;
        }

        GUI.color = A(MenuStyle.S.subDim, 0.9f);
        MenuStyle.FitLabel(new Rect(x + 380f, h - 46f, w * 0.91f - x - 380f, 18f), RunMemoLine, memoStyle);
        GUI.color = Color.white;
        if (age >= MinShowTime)
        {
            GUI.color = MenuStyle.Gold;
            GUI.DrawTexture(new Rect(x, h - 52f, 14f, 14f), GameFont.Logo);
            GUI.color = Color.white;
            if (Mathf.Repeat(age, 1f) < 0.7f)
            {
                GUI.color = MenuStyle.Paper;
                UIScale.Label(new Rect(x, h - 60f, w * 0.8f, 30f), "ENTER · CLICK · SPACE  —  THE DIRECTORY", MenuStyle.Tagline);
                GUI.color = Color.white;
            }
        }
    }

    static float Slide(float age, float delay) => Mathf.Clamp01((age - delay) * 3.5f);

    /// One line of the bill: the cream sheet throws in with a little overshoot, lands lit brass, prints; the label
    /// in ink, the figure right-aligned on a brass rule, the detail under it, the rule filled to the share taken.
    /// `row` is the rot scheduler's index — a tick swaps the LABEL for the company's word and touches nothing else.
    void DrawRow(int row, float x, float y, float w, float h, string label, float score, float max, string detail, float slide, float now)
    {
        if (slide <= 0f) return;
        float ease = MenuStyle.Ease(slide);
        float a = Mathf.Clamp01(ease);
        float restX = x;
        x -= (1f - ease) * 340f;
        const float tilt = MenuStyle.RowTilt;
        Rect r = new Rect(x, y, w, h);
        MenuStyle.DrawRotRect(new Rect(r.x + 6f, r.y + 6f, r.width, r.height), tilt, new Color(0f, 0f, 0f, 0.55f * a));
        bool landing = ease >= 0.9f && ease < 1f; // the two frames before the overshoot: the sheet lands lit, then prints
        Color panel = landing ? MenuStyle.Foil : MenuStyle.S.sheet;
        panel.a = a;
        MenuStyle.DrawRotRect(r, tilt, panel);

        Matrix4x4 saved = GUI.matrix;
        UIScale.RotateAround(tilt, r.center);
        MenuStyle.Flute(r, 0.05f * a);
        // The sliver, seated at rest: the sheet slides in under it (oxblood; a chevron frieze on the Works)
        if (MenuStyle.S.chevron) MenuStyle.Frieze(new Rect(restX, r.y, 7f, r.height), MenuStyle.S.stripe, a, 0.45f);
        else Fill(new Rect(restX, r.y, 7f, r.height), A(MenuStyle.S.stripe, a));

        // The label — or, while the rot has this line, its word in the label's place
        int tick = MenuStyle.RotTick(in rotClock, row, now);
        GUI.color = tick >= 0 ? A(MenuStyle.Rot, a) : A(MenuStyle.Ink, a);
        UIScale.Label(new Rect(r.x + 22f, r.y + 4f, r.width * 0.5f, 34f), tick >= 0 ? MenuStyle.RotWord(in rotClock) : label, MenuStyle.Item);

        // The figure counts up with the landing, right-aligned; its plates shiver until it is in register
        int v = Mathf.RoundToInt(score * a);
        if (valueShown[row] != v) { valueShown[row] = v; valueText[row] = v + " / " + max.ToString("0"); }
        Rect vr = new Rect(r.x, r.y + 4f, r.width - 18f, 34f);
        if (ease < 1f)
        {
            GUI.color = A(MenuStyle.Oxblood, 0.7f * a);
            UIScale.Label(new Rect(vr.x - 3f, vr.y, vr.width, vr.height), valueText[row], valueStyle);
            GUI.color = A(MenuStyle.DeepTeal, 0.7f * a);
            UIScale.Label(new Rect(vr.x + 3f, vr.y + 1f, vr.width, vr.height), valueText[row], valueStyle);
        }
        GUI.color = A(MenuStyle.Ink, a);
        UIScale.Label(vr, valueText[row], valueStyle);

        GUI.color = A(MenuStyle.S.sheetDim, 0.85f * a);
        UIScale.Label(new Rect(r.x + 22f, r.y + 36f, r.width - 40f, 22f), detail, MenuStyle.Small);

        // The brass rule along the foot: bronze, filled brass to the share you took — tape crawls over the fill while
        // it grows, then holds; a foil tick at its end
        Rect bar = new Rect(r.x + 22f, r.y + h - 10f, r.width - 44f, 5f);
        Fill(bar, A(MenuStyle.S.trim, 0.45f * a));
        Rect fill = new Rect(bar.x, bar.y, Mathf.Min(bar.width, bar.width * Mathf.Clamp01(score / max) * ease), bar.height);
        Fill(fill, A(MenuStyle.S.accent, a));
        MenuStyle.Hatch(fill, A(MenuStyle.Ink, 0.25f * a), ease < 1f ? 48f : 0f);
        Fill(new Rect(fill.xMax - 1f, bar.y - 2f, 2f, 9f), A(MenuStyle.Foil, a));
        Fill(new Rect(r.x + 7f, r.yMax - 2f, r.width - 7f, 1f), A(MenuStyle.S.trim, 0.7f * a));

        GUI.matrix = saved;
        GUI.color = Color.white;
    }

    static Color A(Color c, float alpha) => new Color(c.r, c.g, c.b, alpha);

    static void Fill(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
    }
}
