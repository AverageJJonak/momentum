using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// The full main menu. Spawned by GameBootstrap (level = MainMenu), so the
/// scene needs nothing but the standard bootstrap object. Persona bones in a
/// Dark Deco dress: the diagonal band with its cornice, a letterspaced masthead
/// on a sunburst, slash entries on charcoal, cards as brochure pages in stepped
/// brass mats — printed by Halcyon Civic, and the print is going bad: the
/// title's plates kick, a sheet lands cream before it prints, one idle row
/// every fourteen-odd seconds ticks red and says a word it should not. Behind
/// the UI: the Halcyon tower from the street at night (MenuBackdrop), a
/// drifting camera, falling embers, and the score's menu track (MusicDirector).
/// Panels: main list, chapter and level cards, and a working settings page.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    public MovementConfig config;

    /// THE DIRECTORY's entries: CONTINUE RUN heads the board only while a run is in progress; the rest are fixed.
    /// A RUN is the game (RunState): NEW RUN starts at Level 1 bare; CONTINUE picks the saved run up at its next
    /// level with its class; PROGRAMME is free play — any reached level, with a kit picked first.
    enum Act { Continue, NewRun, Programme, Range, Tutorial, Settings, Quit }
    static readonly (string label, Act act, string scene)[] AllEntries =
    {
        ("CONTINUE RUN", Act.Continue, null),
        ("NEW RUN", Act.NewRun, null),
        ("PROGRAMME", Act.Programme, null),
        ("PRACTICE RANGE", Act.Range, "PracticeRange"),
        ("ORIENTATION", Act.Tutorial, "Tutorial"),
        ("SETTINGS", Act.Settings, null),
        ("QUIT", Act.Quit, null),
    };
    const string MainDocLine = "THE HOUSE  ·  DIRECTORY OF SERVICES";
    // The board as it stands this page open: the entries shown (CONTINUE RUN only with a run in progress) and their acts
    readonly string[] mainLabels = new string[AllEntries.Length], mainScenes = new string[AllEntries.Length];
    readonly Act[] mainActs = new Act[AllEntries.Length];
    int mainCount;
    GUIStyle cardNumberStyle, cardDocStyle, thumbNoteStyle;
    // Everything the pages print that is not a literal, built once when a page opens (PageOpened): the rows' record
    // subs, the line over the cards, each card's code / time / score / grade, each chapter's state. Nothing reads
    // PlayerPrefs or builds a string in OnGUI.
    readonly string[] mainSubs = new string[AllEntries.Length];
    string levelsLine = "", chaptersLine = "";
    string[] levelCode, levelTime, levelScore, levelGrade;
    string[] chapterRecord, chapterCount, chapterLock, chapterGrade;
    bool[] chapterOpen;
    SettingsPanel settings; // the SETTINGS / CONTROLS pages, shared with the pause menu
    int chapter; // which LevelCatalog.Chapters entry the level page shows (Q / E or the arrows turn it)
    float headerT; // 0 = the big band and title, 1 = the compact top-centre header of the chapter page
    enum Panel { Main, Chapters, Levels, Settings }

    /// Set before loading the menu scene to open straight onto the level select (the report card does this)
    public static bool StartOnLevels;
    /// The chapter of the level just finished, so the menu comes back to its page (or to the chapter screen if it opened the next chapter)
    public static int StartChapter;
    float lockShakeAt = -10f;
    int lockShakeIndex = -1;

    // NEW RUN over a run in progress asks once: a two-line register card with KEEP THE RUN picked out (the one
    // register card left: a scene on the programme launches directly, there is no kit to pick)
    static readonly string[] ConfirmNames = { "KEEP THE RUN", "ABANDON IT" };
    static readonly string[] ConfirmNotes = { "BACK TO THE DIRECTORY", "A NEW RUN FROM LEVEL 1" };
    bool confirmOpen;
    int confirmSel;
    float confirmOpenedAt;
    /// A register card is up: the page under it draws but takes no pick
    bool Modal => confirmOpen;
    // The programme: which card (in the chapter on show) the run stands at, or -1 — the RUN chip goes on it
    int runCardIndex = -1;

    Panel panel = Panel.Main;
    int selected;
    float openedAt, panelOpenedAt;
    AudioSource sfx;
    AudioClip moveBlip, confirmBlip;
    Camera cam;
    float sparkTimer;
    static Material sparkMat;

    // The rot scheduler: one idle row of the main list every fourteen-odd seconds, and the band's drop-frame
    // every twelve. Both advance in Update only; OnGUI just reads them.
    MenuStyle.RotClock rotClock, bandClock;
    // Where the selected item was last drawn, so a pick leaves its afterimage on the spot
    Rect lastSelectedRect;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
        GameSettings.Apply();
        // The directory reads the run as it was saved: free play's forced class (if the bootstrap's load kept one) is over
        RunState.FreePlay = false;
        RunState.Load();
        openedAt = Time.unscaledTime;
        if (StartOnLevels)
        {
            StartOnLevels = false;
            headerT = 1f;
            chapter = Mathf.Clamp(StartChapter, 0, LevelCatalog.Chapters.Length - 1);
            // A finish that opened the next chapter lands on the chapter screen, the new one picked out
            if (LevelCatalog.ChapterFinished(chapter) && chapter + 1 < LevelCatalog.Chapters.Length) { panel = Panel.Chapters; selected = chapter + 1; }
            else panel = Panel.Levels;
        }
        PageOpened(); // once the page and chapter are known: the page's text is built here

        sfx = gameObject.AddComponent<AudioSource>();
        sfx.spatialBlend = 0f;
        sfx.volume = GameSettings.SfxVolume;
        moveBlip = UIAssets.Clip("rollover2") ?? MenuStyle.MakeMoveBlip();      // THE BORROWED CLICKS (2026-10-27): Kenney's ui-audio when it is there, the synthesised blips without it
        confirmBlip = UIAssets.Clip("click1") ?? MenuStyle.MakeConfirm();
        settings = new SettingsPanel(config, sfx, moveBlip, confirmBlip) { OnClosed = ClosePanel };

        cam = Camera.main;
        BuildDiorama();
        // Level-select thumbnails: shot live from the real levels, one per frame, behind the menu
        var thumbs = gameObject.AddComponent<MenuThumbnails>();
        thumbs.config = config;
    }

    // The Halcyon tower from the street: the thing the whole first act walks toward
    void BuildDiorama() { MenuBackdrop.Build(config, cam); MaterialNet.Reset(); MaterialNet.Run(); }   // the net over the menu's city too: nothing pink, and the console says what was

    // THE REGISTER SLIP beside the directory: the guest's line is a literal (you are guest 0041); the record is
    // built when the page opens and FITTED once the canvas is known (FitSlip) — one letterspaced line when it
    // sets inside the card at this scale, else two aligned lines (the act, then the count). Never a wrap.
    const string GuestLine = "GUEST  ·  0041";
    string slipAct = "", slipCount = "", slipLine = "";
    bool slipTwoLines;
    float slipFitFactor = -1f, slipFitW = -1f;

    /// The act's numeral as the programme prints it ("ACT III" → "III"), from the catalogue's own names
    static string Numeral(int count)
    {
        string name = LevelCatalog.Chapters[Mathf.Clamp(count, 1, LevelCatalog.Chapters.Length) - 1].name;
        return name.StartsWith("ACT ") ? name.Substring(4) : count.ToString();
    }

    /// A page opens: the throw-in and the band's light sweep restart, the title's fringes kick, and the record is read
    void PageOpened()
    {
        panelOpenedAt = MenuStyle.KickAt = Time.unscaledTime;
        BuildPageText();
    }

    /// The record, read once per page open: the board, the rows' subs, the lines over the cards and every card's
    /// own figures. A level finishes through a scene load, so nothing here goes stale while the menu is up.
    void BuildPageText()
    {
        int cleared = LevelCatalog.Cleared, unlocked = LevelCatalog.UnlockedChapters, n = LevelCatalog.Chapters.Length;
        // THE RUN on the slip: the act and scene it stands at and its clock — RUN COMPLETE once it is over, NO RUN IN
        // PROGRESS when there is none. Split for the slip's two-line fallback.
        string runLine = null;
        if (RunState.Active)
        {
            string tail = LevelTimer.FormatShort(RunState.TotalTime);
            if (RunState.Complete) slipAct = "RUN COMPLETE";
            else if (LevelCatalog.Place(RunState.Level, out int rc, out int rs)) slipAct = LevelCatalog.Chapters[rc].name + " · SCENE " + rs;
            else slipAct = "ACT " + Numeral(1) + " · SCENE 1";
            slipCount = tail;
            runLine = slipAct + " · " + tail;
        }
        else { slipAct = "NO RUN"; slipCount = "IN PROGRESS"; }
        slipLine = runLine ?? "NO RUN IN PROGRESS";
        slipFitFactor = -1f; // the record changed: fit it again on the next frame

        // The board: CONTINUE RUN only with a run in progress, then the fixed entries, each with its record
        float bestRun = RunState.BestTime;
        mainCount = 0;
        for (int i = 0; i < AllEntries.Length; i++)
        {
            var e = AllEntries[i];
            if (e.act == Act.Continue && !RunState.InProgress) continue;
            string sub = null;
            switch (e.act)
            {
                case Act.Continue: sub = runLine; break;
                case Act.NewRun: if (bestRun >= 0f) sub = "BEST " + LevelTimer.FormatShort(bestRun); break;
                case Act.Programme: sub = cleared + " / " + LevelCatalog.All.Length + " SCENES"; break;
                case Act.Range:
                case Act.Tutorial:
                {
                    float best = LevelCatalog.BestTime(e.scene);
                    int score = LevelCatalog.BestScore(e.scene);
                    if (best >= 0f) sub = LevelTimer.Format(best);
                    if (score >= 0) sub = (sub == null ? "" : sub + "  ·  ") + score + "/100";
                    break;
                }
            }
            mainLabels[mainCount] = e.label;
            mainActs[mainCount] = e.act;
            mainScenes[mainCount] = e.scene;
            mainSubs[mainCount] = sub;
            mainCount++;
        }
        if (panel == Panel.Main && selected >= mainCount) selected = 0;
        chaptersLine = cleared + " / " + LevelCatalog.All.Length + " SCENES CLEARED   ·   " + unlocked + " OF " + n + (n == 1 ? " ACT OPEN" : " ACTS OPEN");
        if (chapterRecord == null || chapterRecord.Length != n)
        {
            chapterRecord = new string[n]; chapterCount = new string[n]; chapterLock = new string[n]; chapterGrade = new string[n]; chapterOpen = new bool[n];
        }
        for (int c = 0; c < n; c++)
        {
            var ch = LevelCatalog.Chapters[c];
            chapterOpen[c] = LevelCatalog.ChapterUnlocked(c);
            chapterGrade[c] = LevelCatalog.ChapterGrade(c);
            chapterRecord[c] = LevelCatalog.ClearedIn(c) + " / " + ch.count + " CLEARED";
            chapterCount[c] = LevelCatalog.ChapterFinished(c) ? "FINISHED" : ch.count + (ch.count == 1 ? " SCENE" : " SCENES");
            chapterLock[c] = c > 0 ? "CLEAR " + LevelCatalog.Chapters[c - 1].name + " TO OPEN" : "";
        }
        var cur = LevelCatalog.Chapters[chapter];
        if (levelCode == null || levelCode.Length != cur.count)
        {
            levelCode = new string[cur.count]; levelTime = new string[cur.count]; levelScore = new string[cur.count]; levelGrade = new string[cur.count];
        }
        int clearedHere = 0;
        for (int i = 0; i < cur.count; i++)
        {
            var info = LevelCatalog.All[cur.first + i];
            float bestT = LevelCatalog.BestTime(info.scene);
            int bestS = LevelCatalog.BestScore(info.scene);
            if (bestT >= 0f) clearedHere++;
            levelCode[i] = "SCENE " + (i + 1);
            levelTime[i] = bestT >= 0f ? LevelTimer.Format(bestT) : "NEW";
            levelScore[i] = bestS >= 0 ? bestS + " / 100" : "gold " + LevelTimer.Format(info.gold);
            levelGrade[i] = bestS >= 0 ? LevelCatalog.Grade(bestS) : null;
        }
        levelsLine = cur.subtitle + "   ·   " + cur.count + (cur.count == 1 ? " SCENE   ·   " : " SCENES   ·   ") + clearedHere + " CLEARED";
        // The card the run stands at, if it is on this page
        runCardIndex = RunState.InProgress && RunState.Level >= cur.first && RunState.Level < cur.first + cur.count ? RunState.Level - cur.first : -1;
    }

    void Update()
    {
        DriftCamera();
        RainSparks();
        // The header tucks up out of the way while the chapter page is open
        headerT = Mathf.MoveTowards(headerT, (panel == Panel.Levels || panel == Panel.Chapters) ? 1f : 0f, Time.unscaledDeltaTime / 0.35f);

        // The clocks. The list only glitches on the main page, once every row has landed, and never the selected
        // row; the cards never (their glitch is the lock shake) and the settings pages keep their own clock.
        float now = Time.unscaledTime;
        MenuStyle.AdvanceRot(ref bandClock, now, 12f, 1, -1, -1, 0.1f);
        bool landed = now - panelOpenedAt > mainCount * 0.14f + 1f / 3.4f;
        MenuStyle.AdvanceRot(ref rotClock, now, 14f, panel == Panel.Main && landed && !Modal ? mainCount : 0, selected);

        var kb = Keyboard.current;
        if (kb == null) return;

        // The settings pages own their input (including Esc) while they are up
        if (panel == Panel.Settings)
        {
            settings.Update(kb);
            return;
        }

        // A register card up owns the keys: the NEW RUN question on the directory
        if (confirmOpen) { ConfirmKeys(kb); return; }

        int rows = panel == Panel.Main ? mainCount : panel == Panel.Chapters ? LevelCatalog.Chapters.Length + 1 : LevelCatalog.Chapters[chapter].count + 1;
        int dir = 0;
        if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) dir = 1;
        if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) dir = -1;
        if (dir != 0)
        {
            selected = (selected + dir + rows) % rows;
            sfx.PlayOneShot(moveBlip, 0.7f);
            MenuStyle.Bump();
        }

        if (panel == Panel.Levels || panel == Panel.Chapters)
        {
            // The panels sit side by side: left/right walks them, up/down still reaches BACK
            int count = panel == Panel.Levels ? LevelCatalog.Chapters[chapter].count : LevelCatalog.Chapters.Length;
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)
            {
                selected = selected >= count ? 0 : (selected + 1) % count;
                sfx.PlayOneShot(moveBlip, 0.7f);
                MenuStyle.Bump();
            }
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)
            {
                selected = selected >= count ? count - 1 : (selected + count - 1) % count;
                sfx.PlayOneShot(moveBlip, 0.7f);
                MenuStyle.Bump();
            }
            if (panel == Panel.Levels)
            {
                if (kb.eKey.wasPressedThisFrame || kb.pageDownKey.wasPressedThisFrame) TurnChapter(1);
                if (kb.qKey.wasPressedThisFrame || kb.pageUpKey.wasPressedThisFrame) TurnChapter(-1);
                if (kb.escapeKey.wasPressedThisFrame) OpenChapters();
            }
            else if (kb.escapeKey.wasPressedThisFrame) ClosePanel();
        }

        if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
            Activate(selected);
    }

    /// The next or previous chapter's page, wrapping; the panels slide in again
    void TurnChapter(int dir)
    {
        int n = LevelCatalog.Chapters.Length;
        if (n < 2) return;
        int next = chapter;
        for (int i = 0; i < n; i++)
        {
            next = (next + dir + n) % n;
            if (LevelCatalog.ChapterUnlocked(next)) break; // locked chapters are skipped over
        }
        if (next == chapter) return;
        chapter = next;
        selected = 0;
        PageOpened();
        sfx.PlayOneShot(confirmBlip, 0.6f);
    }

    /// The chapter screen, the chapter you were on picked out
    void OpenChapters()
    {
        panel = Panel.Chapters;
        selected = Mathf.Clamp(chapter, 0, LevelCatalog.Chapters.Length - 1);
        PageOpened();
    }

    /// A chapter's level page
    void OpenLevels(int c)
    {
        chapter = c;
        panel = Panel.Levels;
        selected = 0;
        PageOpened();
    }

    void DriftCamera()
    {
        if (cam != null) MenuBackdrop.Drift(cam);
    }

    void RainSparks()
    {
        sparkTimer -= Time.unscaledDeltaTime;
        if (sparkTimer > 0f) return;
        sparkTimer = 0.35f;
        if (sparkMat == null)
        {
            sparkMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            sparkMat.SetColor("_BaseColor", new Color(1f, 0.8f, 0.3f));
        }
        var spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
        spark.name = "MenuSpark";
        spark.layer = 2;
        Destroy(spark.GetComponent<Collider>());
        // In front of the LENS, wherever it is pointing: the camera orbits now, and a fixed offset in the world was
        // behind it for half of every lap
        Transform lens = cam != null ? cam.transform : null;
        spark.transform.position = lens != null
            ? lens.position + lens.forward * Random.Range(6f, 16f) + lens.right * Random.Range(-9f, 9f) + lens.up * Random.Range(1f, 5f)
            : new Vector3(Random.Range(-10f, 10f), Random.Range(3f, 7f), Random.Range(3f, 14f));
        spark.transform.rotation = Random.rotation;
        spark.transform.localScale = new Vector3(0.06f, 0.06f, Random.Range(0.2f, 0.5f));
        spark.GetComponent<MeshRenderer>().sharedMaterial = sparkMat;
        var rb = spark.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearVelocity = new Vector3(Random.Range(-0.3f, 0.3f), -1.6f, Random.Range(-0.3f, 0.3f));
        rb.angularVelocity = Random.insideUnitSphere * 2f;
        spark.AddComponent<ShardFade>();
    }

    void Activate(int index)
    {
        if (panel == Panel.Chapters)
        {
            if (index >= LevelCatalog.Chapters.Length) { sfx.PlayOneShot(confirmBlip, 0.8f); MenuStyle.Confirm(lastSelectedRect); ClosePanel(); return; }
            if (LevelCatalog.ChapterUnlocked(index)) { sfx.PlayOneShot(confirmBlip, 0.8f); MenuStyle.Confirm(lastSelectedRect); OpenLevels(index); }
            else
            {
                // Shut: the panel shakes its head
                lockShakeAt = Time.unscaledTime;
                lockShakeIndex = index;
                sfx.PlayOneShot(moveBlip, 0.4f);
            }
            return;
        }
        sfx.PlayOneShot(confirmBlip, 0.8f);
        MenuStyle.Confirm(lastSelectedRect);
        if (panel == Panel.Levels)
        {
            var ch = LevelCatalog.Chapters[chapter];
            if (index < ch.count) LaunchFree(ch.first + index); // FREE PLAY: the scene, for itself
            else OpenChapters(); // BACK: up to the chapter screen
            return;
        }

        if (index < 0 || index >= mainCount) return;
        switch (mainActs[index])
        {
            case Act.Continue: ContinueRun(); break;
            case Act.NewRun: if (RunState.InProgress) OpenConfirm(); else StartNewRun(); break;
            case Act.Programme: OpenChapters(); break;
            case Act.Settings: panel = Panel.Settings; settings.Open(); break;
            case Act.Quit: Application.Quit(); break;
            default: RunState.FreePlay = false; LaunchScene(mainScenes[index]); break; // the range, the tutorial: the run is not touched
        }
    }

    // ---------------------------------------------------------------- the launches
    /// CONTINUE RUN: the run's next level
    void ContinueRun()
    {
        RunState.FreePlay = false;
        LaunchScene(RunState.NextScene);
    }

    /// NEW RUN: Level 1 — saved before the scene loads (a run in progress was asked about first)
    void StartNewRun()
    {
        RunState.NewRun();
        LaunchScene(LevelCatalog.All[0].scene);
    }

    /// FREE PLAY: the scene for itself (never saved); the bill leaves the run alone (the level's bests still count).
    /// A scene the programme has not opened yet only blips
    void LaunchFree(int level)
    {
        if (!LevelCatalog.LevelOpen(level)) { sfx.PlayOneShot(moveBlip, 0.4f); return; }
        RunState.FreePlay = true;
        LaunchScene(LevelCatalog.All[level].scene);
    }

    void LaunchScene(string scene)
    {
        if (Application.CanStreamedLevelBeLoaded(scene)) { PlayerPrefs.Save(); SceneManager.LoadScene(scene); return; }
        // Say it on screen instead of failing silently into the console
        toastMessage = $"'{scene}' ISN'T IN THE BUILD SCENE LIST — run Momentum → Fix Scene List, then retry";
        toastUntil = Time.unscaledTime + 5f;
        Debug.LogError($"MainMenu: scene '{scene}' is not loadable (missing from Build Settings).");
    }

    string toastMessage;
    float toastUntil;

    // ---------------------------------------------------------------- the register card
    /// NEW RUN with a run in progress: the question, KEEP THE RUN picked out
    void OpenConfirm()
    {
        confirmOpen = true;
        confirmSel = 0;
        confirmOpenedAt = MenuStyle.KickAt = Time.unscaledTime;
    }

    void ConfirmKeys(Keyboard kb)
    {
        if (kb.escapeKey.wasPressedThisFrame) { confirmOpen = false; sfx.PlayOneShot(moveBlip, 0.7f); return; }
        if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame
            || kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)
        {
            confirmSel = 1 - confirmSel;
            sfx.PlayOneShot(moveBlip, 0.7f);
            MenuStyle.Bump();
        }
        if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) ConfirmPick(confirmSel);
    }

    void ConfirmPick(int i)
    {
        if (!confirmOpen) return;
        sfx.PlayOneShot(confirmBlip, 0.8f);
        confirmOpen = false;
        if (i == 1) StartNewRun();
    }

    /// Where the card up now sits: centred, dropping into place with its throw-in
    Rect CardRect(float w, float h, out int count, out float ease)
    {
        count = ConfirmNames.Length;
        ease = MenuStyle.Ease((Time.unscaledTime - confirmOpenedAt) * 4f);
        float cw = Mathf.Min(520f, w * 0.46f);
        float chh = MenuStyle.RegisterCardHeight(count, true);
        return new Rect((w - cw) * 0.5f, (h - chh) * 0.5f + (1f - ease) * 60f, cw, chh);
    }

    /// The card's mouse, taken BEFORE the page draws so nothing under the card can take the click: a hover moves
    /// the pick, a click on a line takes it, a click anywhere else is spent
    void CardInput(float w, float h)
    {
        var e = Event.current;
        Rect r = CardRect(w, h, out int count, out float ease);
        int hovered = ease >= 0.9f ? MenuStyle.RegisterCardHit(r, true, count, e.mousePosition) : -1;
        if (hovered >= 0 && hovered != confirmSel)
        {
            confirmSel = hovered;
            sfx.PlayOneShot(moveBlip, 0.5f);
            MenuStyle.Bump();
        }
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            e.Use();
            if (hovered >= 0) ConfirmPick(hovered);
        }
    }

    /// The card over the page: the veil, then the register card — the NEW RUN question (the run in progress under it)
    void DrawCard(float w, float h)
    {
        Rect r = CardRect(w, h, out int count, out float ease);
        float a = Mathf.Clamp01(ease);
        GUI.color = new Color(MenuStyle.Veil.r, MenuStyle.Veil.g, MenuStyle.Veil.b, 0.55f * a);
        GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;
        MenuStyle.RegisterCard(r, a, "A RUN IS IN PROGRESS", slipLine, ConfirmNames, ConfirmNotes, count, confirmSel);
    }

    /// THE RUN's chip on the card it stands at: gold, the word in the company's face, tilted with the card
    void RunChip(Rect chip, Rect card, float tilt, float a)
    {
        Matrix4x4 m = GUI.matrix;
        UIScale.RotateAround(tilt, card.center);
        Fill(new Rect(chip.x + 2f, chip.y + 2f, chip.width, chip.height), new Color(0f, 0f, 0f, 0.45f * a));
        Fill(chip, Fade(MenuStyle.Gold, a));
        GUI.color = Fade(MenuStyle.Ink, a);
        UIScale.Label(chip, "RUN", cardDocStyle);
        GUI.matrix = m;
        GUI.color = Color.white;
    }

    void ClosePanel()
    {
        PlayerPrefs.Save();
        panel = Panel.Main;
        selected = 0;
        PageOpened();
        sfx.PlayOneShot(moveBlip, 0.7f);
    }

    void OnGUI()
    {
        MenuStyle.EnsureStyles();
        MenuStyle.Use(panel == Panel.Levels ? MenuStyle.SkinFor(chapter) : MenuStyle.Civic); // a chapter's page prints in its property's material
        if (cardNumberStyle == null)
        {
            cardNumberStyle = new GUIStyle(MenuStyle.ItemSelected) { alignment = TextAnchor.MiddleCenter, fontSize = 34 };
            cardDocStyle = new GUIStyle(MenuStyle.Doc) { alignment = TextAnchor.MiddleCenter };
            thumbNoteStyle = new GUIStyle(MenuStyle.Sub) { alignment = TextAnchor.MiddleCenter };
        }
        UIScale.Begin();
        float w = UIScale.W, h = UIScale.H;
        float t = Time.unscaledTime;
        if (panel == Panel.Main) FitSlip(w); // measured once per record / scale / canvas width, not per frame

        // The veil: the street goes night-dark enough that brass and cream stay the loudest things (the tower is dark already)
        GUI.color = new Color(MenuStyle.Veil.r, MenuStyle.Veil.g, MenuStyle.Veil.b, 0.3f);
        GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // The header: on the chapter page the band flattens and thins and the title
        // slides up to the top centre, so the level panels get the screen
        float k = headerT * headerT * (3f - 2f * headerT);
        float bandY = Mathf.Lerp(h * 0.21f, h * 0.085f, k);
        float bandAngle = Mathf.Lerp(MenuStyle.BandAngle, -4f, k);
        float bandScale = Mathf.Lerp(1f, 0.55f, k);
        MenuStyle.StripeBand(w, bandY, bandAngle, bandScale, t - panelOpenedAt);
        MenuStyle.Title.fontSize = (int)Mathf.Lerp(Mathf.Min(104f, h * 0.12f), 58f, k);
        string title = panel == Panel.Main ? "MOMENTUM" : panel == Panel.Levels ? LevelCatalog.Chapters[chapter].name : panel == Panel.Chapters ? "PROGRAMME" : settings.Title;
        float titleW = MenuStyle.TitleWidth(title);
        float titleX = Mathf.Lerp(w * 0.09f, (w - titleW) * 0.5f, k);
        // The document line goes under the full-size title only; the tucked header has no room for it
        string docLine = panel == Panel.Main ? MainDocLine : panel == Panel.Settings ? settings.DocLine : null;
        float titleBottom = MenuStyle.TitleBlock(titleX, w, bandY, title, bandAngle, docLine, bandScale);
        MenuStyle.BandDropFrame(in bandClock, w, bandY, bandAngle, bandScale);
        // Tucked, the band still dips toward the left: the page stacks under its lowest edge there
        float headerBottom = bandY + 88f * bandScale + (w * 0.5f - w * 0.09f) * Mathf.Tan(Mathf.Abs(bandAngle) * Mathf.Deg2Rad);
        float stackY = Mathf.Max(titleBottom, Mathf.Lerp(titleBottom, headerBottom, k)) + 30f;

        bool modal = Modal;
        if (modal) CardInput(w, h); // the card's mouse first: the page under it takes no click while it is up
        if (panel == Panel.Main) DrawMain(w, h, stackY);
        else if (panel == Panel.Levels) DrawLevels(w, h, stackY);
        else if (panel == Panel.Chapters) DrawChapters(w, h, stackY);
        else settings.Draw(w, h, stackY);
        if (modal) DrawCard(w, h);

        // The hint, right-anchored, in a box that runs from clear of the company mark to the margin: one line, measured
        GUI.color = Fade(MenuStyle.Paper, 0.45f);
        float hintX = w * 0.09f + 380f;
        string hint = confirmOpen ? "↑↓ choose · Enter or click · Esc keeps the run"
            : panel == Panel.Main ? "v0.1 — the movement is the weapon"
            : panel == Panel.Chapters ? "←→ pick an act · Enter or click: open · Esc back"
            : panel == Panel.Levels ? "←→ pick a scene · Q/E act · Enter or click: free play · Esc back"
            : settings.Hint;
        MenuStyle.FitLabel(new Rect(hintX, h - 30f, w - 20f - hintX, 24f), hint, MenuStyle.Sub);
        GUI.color = Color.white;

        // The company, small, bottom-left: this city has an owner. The mark never glitches.
        GUI.color = new Color(MenuStyle.Gold.r, MenuStyle.Gold.g, MenuStyle.Gold.b, 0.85f);
        GUI.DrawTexture(new Rect(w * 0.09f - 2f, h - 46f, 28f, 28f), GameFont.Logo);
        UIScale.Label(new Rect(w * 0.09f + 32f, h - 45f, 320f, 26f), LevelKit.CompanyFull, MenuStyle.Brand);
        GUI.color = Color.white;

        if (Time.unscaledTime < toastUntil && !string.IsNullOrEmpty(toastMessage))
        {
            GUI.color = new Color(0.95f, 0.3f, 0.25f);
            UIScale.Label(new Rect(w * 0.09f, h - 92f, w * 0.85f, 26f), toastMessage, MenuStyle.Small);
            GUI.color = Color.white;
        }
        MenuStyle.DrawAfterimage();
        UIScale.End();
    }

    void DrawMain(float w, float h, float startY)
    {
        float t = Time.unscaledTime;
        float itemW = Mathf.Min(560f, w * 0.5f);
        // Rows compress on short screens so the list never runs off the bottom
        float rowH = Mathf.Clamp((h - startY - 30f) / Mathf.Max(1, mainCount), 42f, 62f);
        float y = startY;
        string rotWord = MenuStyle.RotWord(in rotClock);
        for (int i = 0; i < mainCount; i++)
        {
            float slide = (t - panelOpenedAt) * 3.4f - i * 0.14f;
            Rect r = new Rect(w * 0.09f, y, itemW, rowH - 12f);
            string sub = mainSubs[i]; // the record, read when the page opened

            if (!Modal && MenuStyle.Hovered(r, slide) && selected != i)
            {
                selected = i;
                sfx.PlayOneShot(moveBlip, 0.5f);
                MenuStyle.Bump();
            }
            if (selected == i) lastSelectedRect = new Rect(r.x - 14f, r.y - 3f, r.width + 34f, r.height + 6f);
            // A row still sliding never glitches, whatever the clock says
            int rotTick = slide >= 1f ? MenuStyle.RotTick(in rotClock, i, t) : -1;
            // THE DIRECTORY: brass letters on black felt, an index per entry
            if (MenuStyle.DirectoryItem(r, mainLabels[i], sub, selected == i, slide, null, i + 1, rotTick, rotWord))
                Activate(i);
            y += rowH;
        }
        DrawSlip(w, startY, itemW);
    }

    /// The slip's width on this canvas
    static float SlipWidth(float w) => Mathf.Min(440f, w * 0.3f);

    /// The record line is fitted to the slip once per record, scale and canvas width: on one tracked line when it
    /// sets inside the card, else split into the act and the count — IMGUI never gets the chance to wrap it
    void FitSlip(float w)
    {
        if (slipFitFactor == UIScale.Factor && slipFitW == w) return;
        slipFitFactor = UIScale.Factor;
        slipFitW = w;
        slipTwoLines = !MenuStyle.SlipFits(slipLine, SlipWidth(w));
    }

    // THE REGISTER SLIP beside the directory: the House's register, the card the desk keeps on each guest — cream,
    // ink type, brass rules, the wax seal — tilted with the rows and thrown in after them. GUEST · 0041 is you; the
    // record line under it is the admission (the act you are admitted to, the scenes, the scenes cleared).
    void DrawSlip(float w, float startY, float itemW)
    {
        float t = Time.unscaledTime;
        float ease = MenuStyle.Ease((t - panelOpenedAt) * 3.4f - 0.7f);
        float a = Mathf.Clamp01(ease);
        if (a <= 0f) return;
        float bw = SlipWidth(w);
        float bx = Mathf.Min(w * 0.09f + itemW + 90f, w - bw - 40f);
        Rect r = new Rect(bx + (1f - ease) * 340f, startY + 6f, bw, MenuStyle.SlipHeight(slipTwoLines));
        MenuStyle.RegisterSlip(r, a, GuestLine, slipTwoLines ? slipAct : slipLine, slipTwoLines ? slipCount : null);
    }

    // The chapter page: the chapter's levels side by side as tall panels — a live
    // thumbnail shot from the level itself, the level code and name, and the
    // record (rank stamp, best time, best score) — with BACK underneath. Adding a
    // level to a chapter in LevelCatalog adds a panel here.
    void DrawLevels(float w, float h, float startY)
    {
        var ch = LevelCatalog.Chapters[chapter];
        float x0 = w * 0.09f;
        float usable = w * 0.82f;
        const float gap = 18f;
        float pw = (usable - gap * (ch.count - 1)) / ch.count;
        float ph = (pw - 16f) * 9f / 16f + 168f;
        float y = startY + 34f;

        // The chapter line (built when the page opened), with the asset tag at its right end
        MenuStyle.DrawRotLabel(new Rect(x0, startY, usable, 28f), 0f, levelsLine, MenuStyle.Item, MenuStyle.S.accent);
        Tag(new Rect(x0 + usable - 64f, startY + 11f, 64f, 7f), MenuStyle.S.subDim, 0.5f);
        // The chapter arrows either end of the line: click one (or Q / E) to turn the page
        if (LevelCatalog.Chapters.Length > 1)
        {
            var e = Event.current;
            Rect prev = new Rect(x0 - 44f, startY - 4f, 36f, 36f), next = new Rect(x0 + usable + 8f, startY - 4f, 36f, 36f);
            bool overPrev = prev.Contains(e.mousePosition), overNext = next.Contains(e.mousePosition);
            MenuStyle.DrawRotLabel(prev, 0f, "<", MenuStyle.Item, overPrev ? Color.white : MenuStyle.S.accent);
            MenuStyle.DrawRotLabel(next, 0f, ">", MenuStyle.Item, overNext ? Color.white : MenuStyle.S.accent);
            if (e.type == EventType.MouseDown && e.button == 0 && (overPrev || overNext))
            {
                TurnChapter(overNext ? 1 : -1);
                e.Use();
            }
        }

        for (int i = 0; i < ch.count; i++)
        {
            float slide = (Time.unscaledTime - panelOpenedAt) * 4f - i * 0.1f;
            Rect r = new Rect(x0 + i * (pw + gap), y, pw, ph);
            if (!Modal && MenuStyle.Hovered(r, slide) && selected != i)
            {
                selected = i;
                sfx.PlayOneShot(moveBlip, 0.5f);
                MenuStyle.Bump();
            }
            if (LevelPanel(r, i, LevelCatalog.All[ch.first + i], selected == i, slide)) Activate(i);
        }

        int backIndex = ch.count;
        float slideB = (Time.unscaledTime - panelOpenedAt) * 4f - backIndex * 0.1f;
        Rect back = new Rect(x0, y + ph + 26f, Mathf.Min(440f, w * 0.42f), 44f);
        if (!Modal && MenuStyle.Hovered(back, slideB) && selected != backIndex)
        {
            selected = backIndex;
            sfx.PlayOneShot(moveBlip, 0.5f);
            MenuStyle.Bump();
        }
        if (selected == backIndex) lastSelectedRect = new Rect(back.x - 14f, back.y - 3f, back.width + 34f, back.height + 6f);
        if (MenuStyle.SlashItem(back, "BACK", null, selected == backIndex, slideB)) Activate(backIndex);
    }

    // The chapter screen: one tall panel per chapter — its first level's picture,
    // the number, the name, the tagline, how much of it is cleared and its best
    // grade — with BACK underneath. A chapter opens when the one before it is
    // finished; until then its panel is dark and stamped LOCKED, and a pick on it
    // only shakes it.
    void DrawChapters(float w, float h, float startY)
    {
        int n = LevelCatalog.Chapters.Length;
        float x0 = w * 0.09f;
        float usable = w * 0.82f;
        const float gap = 18f;
        float pw = Mathf.Min((usable - gap * (n - 1)) / n, 440f);
        float ph = (pw - 16f) * 9f / 16f + 190f;
        float y = startY + 34f;

        MenuStyle.DrawRotLabel(new Rect(x0, startY, usable, 28f), 0f, chaptersLine, MenuStyle.Item, MenuStyle.S.accent);
        Tag(new Rect(x0 + usable - 64f, startY + 11f, 64f, 7f), MenuStyle.S.subDim, 0.5f);

        for (int i = 0; i < n; i++)
        {
            float slide = (Time.unscaledTime - panelOpenedAt) * 4f - i * 0.12f;
            Rect r = new Rect(x0 + i * (pw + gap), y, pw, ph);
            if (!Modal && MenuStyle.Hovered(r, slide) && selected != i)
            {
                selected = i;
                sfx.PlayOneShot(moveBlip, 0.5f);
                MenuStyle.Bump();
            }
            MenuStyle.Use(MenuStyle.SkinFor(i)); // each chapter's card wears its own property's skin
            bool pick = ChapterPanel(r, i, selected == i, slide);
            MenuStyle.Use(MenuStyle.Civic);
            if (pick) Activate(i);
        }

        int backIndex = n;
        float slideB = (Time.unscaledTime - panelOpenedAt) * 4f - backIndex * 0.1f;
        Rect back = new Rect(x0, y + ph + 26f, Mathf.Min(440f, w * 0.42f), 44f);
        if (!Modal && MenuStyle.Hovered(back, slideB) && selected != backIndex)
        {
            selected = backIndex;
            sfx.PlayOneShot(moveBlip, 0.5f);
            MenuStyle.Bump();
        }
        if (selected == backIndex) lastSelectedRect = new Rect(back.x - 14f, back.y - 3f, back.width + 34f, back.height + 6f);
        if (MenuStyle.SlashItem(back, "BACK", null, selected == backIndex, slideB)) Activate(backIndex);
    }

    // ---------------------------------------------------------------- the cards
    // A card is a brochure page: a photograph printed through a halftone screen in
    // a stepped brass mat over a chevron rule, the asset code and its tag, the
    // name, the tagline, the rubber-stamped grade and the record. Picked, the
    // sheet goes cream, the photo comes into focus, a glint crosses it once and
    // the company's seal punches in. Shut, it is a flat dark sheet with an oxblood
    // hatch crawling over it, and a push on it makes the door glitch.

    bool ChapterPanel(Rect r, int c, bool selected, float slide)
    {
        var ch = LevelCatalog.Chapters[c];
        bool open = chapterOpen[c];
        float t = Time.unscaledTime;
        float ease = MenuStyle.Ease(slide);
        float a = Mathf.Clamp01(ease);
        r.y += (1f - ease) * 120f; // rises into place, a hair past its mark
        if (selected) r = new Rect(r.x - 6f, r.y - 10f, r.width + 12f, r.height + 10f);
        Jitter(ref r, selected, t);
        bool shaking = lockShakeIndex == c && t - lockShakeAt < 0.35f;
        if (shaking)
        {
            float s = 1f - (t - lockShakeAt) / 0.35f;
            r.x += Mathf.Sin(t * 60f) * 9f * s;
        }
        if (selected) lastSelectedRect = r;
        const float tilt = -2f;

        Rect pic = new Rect(r.x + 8f, r.y + 8f, r.width - 16f, (r.width - 16f) * 9f / 16f);
        float ty = pic.yMax + 10f;
        Rect codeR = new Rect(r.x + 12f, ty, 120f, 24f);
        Rect barR = new Rect(r.x + 70f, ty + 7f, 64f, 10f);
        Rect nameR = new Rect(r.x + 12f, ty + 22f, r.width - 24f, 34f);
        Rect tagR = new Rect(r.x + 12f, ty + 56f, r.width - 24f, 20f);
        float ry = ty + 84f;
        Rect stamp = new Rect(r.x + 12f, ry, 54f, 54f);
        float sx = stamp.xMax + 12f;
        Rect line1 = new Rect(sx, ry - 2f, r.width - (sx - r.x) - 8f, 26f);
        Rect line2 = new Rect(sx, ry + 24f, r.width - (sx - r.x) - 8f, 20f);

        if (!open)
        {
            // A refused pick ticks the sheet red and ink with the head-shake: three flashes, then it holds
            int state = shaking ? MenuStyle.RotState((int)((t - lockShakeAt) / 0.03f)) : -1;
            Color sheet = Fade(MenuStyle.Locked, a);
            Color locked = state == 0 ? Fade(MenuStyle.Ink, a) : Fade(MenuStyle.Rot, a);
            Color sick = Fade(MenuStyle.S.subDim, a);
            MenuStyle.DrawRotRect(r, tilt, sheet); // flat: no shadow
            Matrix4x4 m = GUI.matrix;
            UIScale.RotateAround(tilt, r.center);
            MenuStyle.Hatch(r, Fade(MenuStyle.Oxblood, 0.45f * a), 8f);
            GUI.matrix = m;
            // The picture blacked out and framed in red; during a tick the plate follows the sheet so LOCKED reads
            GUI.color = Fade(MenuStyle.Ink, a);
            GUI.DrawTexture(pic, Texture2D.whiteTexture);
            Frame(pic, 2f, Fade(MenuStyle.Rot, a), 6f);
            if (state >= 0)
            {
                // The refused pick: a slab behind the word ticks red and ink — the door glitches when you push on it
                GUI.color = state == 0 ? Fade(MenuStyle.Rot, 0.85f * a) : Fade(MenuStyle.Ink, a);
                GUI.DrawTexture(new Rect(pic.x + 8f, pic.center.y - 22f, pic.width - 16f, 44f), Texture2D.whiteTexture);
            }
            GUI.color = locked;
            UIScale.Label(pic, "LOCKED", cardNumberStyle);
            GUI.color = sick;
            UIScale.Label(new Rect(pic.x, pic.center.y + 26f - 9f, pic.width, 18f), "CLEARANCE REQUIRED", cardDocStyle);
            GUI.color = Color.white;
            MenuStyle.DrawRotLabel(codeR, tilt, MenuStyle.IndexLabel(c + 1), MenuStyle.Code, sick);
            CardTag(codeR, tilt, barR, MenuStyle.S.subDim, 0.5f * a);
            CardName(nameR, tilt, ch.name, sick, false, a);
            MenuStyle.DrawRotLabel(tagR, tilt, ch.subtitle, MenuStyle.Small, Fade(MenuStyle.S.subDim, 0.85f * a));
            MenuStyle.Medallion(stamp, null, -8f, 1f, a, cardNumberStyle);
            MenuStyle.DrawRotLabel(line1, tilt, "LOCKED", MenuStyle.Item, state >= 0 ? locked : sick);
            MenuStyle.DrawRotLabel(line2, tilt, chapterLock[c], MenuStyle.Small, sick);
            return Picked(r);
        }

        Color ink = Fade(selected ? MenuStyle.Ink : MenuStyle.Paper, a);
        Color dim = Fade(selected ? MenuStyle.S.sheetDim : MenuStyle.S.subDim, a);
        Color body = CardBody(r, tilt, a, ease, selected, t);
        // The picture: the chapter's first level
        CardPicture(pic, MenuThumbnails.Get(LevelCatalog.All[ch.first].scene), a, selected, t, body);

        // Number and asset tag, name, tagline, the seal on the picked card
        MenuStyle.DrawRotLabel(codeR, tilt, MenuStyle.IndexLabel(c + 1), MenuStyle.Code, Fade(selected ? MenuStyle.Ink : MenuStyle.S.accent, a));
        CardTag(codeR, tilt, barR, selected ? MenuStyle.S.sheetDim : MenuStyle.S.subDim, 0.7f * a);
        CardName(nameR, tilt, ch.name, ink, selected, a);
        MenuStyle.DrawRotLabel(tagR, tilt, ch.subtitle, MenuStyle.Small, dim);
        if (selected) CardSeal(r, ty, tilt, a, t);

        // The record: the act's grade on a brass medallion and how much of it is cleared (read when the page opened)
        MenuStyle.Medallion(stamp, chapterGrade[c], -8f, 1f, a, cardNumberStyle);
        MenuStyle.DrawRotLabel(line1, tilt, chapterRecord[c], MenuStyle.Item, ink);
        MenuStyle.DrawRotLabel(line2, tilt, chapterCount[c], MenuStyle.Small, dim);
        return Picked(r);
    }

    bool LevelPanel(Rect r, int i, LevelCatalog.LevelInfo info, bool selected, float slide)
    {
        float t = Time.unscaledTime;
        float ease = MenuStyle.Ease(slide);
        float a = Mathf.Clamp01(ease);
        r.y += (1f - ease) * 120f; // rises into place, a hair past its mark
        if (selected) r = new Rect(r.x - 6f, r.y - 10f, r.width + 12f, r.height + 10f);
        Jitter(ref r, selected, t);
        if (selected) lastSelectedRect = r;
        const float tilt = -2f;
        Color ink = Fade(selected ? MenuStyle.Ink : MenuStyle.Paper, a);
        Color dim = Fade(selected ? MenuStyle.S.sheetDim : MenuStyle.S.subDim, a);

        Color body = CardBody(r, tilt, a, ease, selected, t);
        // The picture: a photo pinned flat to the tilted panel
        Rect pic = new Rect(r.x + 8f, r.y + 8f, r.width - 16f, (r.width - 16f) * 9f / 16f);
        CardPicture(pic, MenuThumbnails.Get(info.scene), a, selected, t, body);

        // Code and asset tag, name, tagline, the seal on the picked card
        float ty = pic.yMax + 10f;
        Rect codeR = new Rect(r.x + 12f, ty, 80f, 24f);
        MenuStyle.DrawRotLabel(codeR, tilt, levelCode[i], MenuStyle.Code, Fade(selected ? MenuStyle.Ink : MenuStyle.S.accent, a));
        CardTag(codeR, tilt, new Rect(r.x + 70f, ty + 7f, 64f, 10f), selected ? MenuStyle.S.sheetDim : MenuStyle.S.subDim, 0.7f * a);
        CardName(new Rect(r.x + 12f, ty + 22f, r.width - 24f, 34f), tilt, info.title, ink, selected, a);
        MenuStyle.DrawRotLabel(new Rect(r.x + 12f, ty + 56f, r.width - 24f, 20f), tilt, info.subtitle, MenuStyle.Small, dim);
        if (selected) CardSeal(r, ty, tilt, a, t);
        // THE RUN stands here: its chip, left of where the seal sits
        if (runCardIndex == i) RunChip(new Rect(r.xMax - 36f - 12f - 48f, ty + 1f, 48f, 20f), r, tilt, a);

        // The record: the grade on a brass medallion, the best time, the best score — or NEW and the gold time to chase (all read when the page opened)
        float ry = ty + 84f;
        Rect stamp = new Rect(r.x + 12f, ry, 54f, 54f);
        MenuStyle.Medallion(stamp, levelGrade[i], -8f, 1f, a, cardNumberStyle);
        float sx = stamp.xMax + 12f;
        MenuStyle.DrawRotLabel(new Rect(sx, ry - 2f, r.width - (sx - r.x) - 8f, 26f), tilt, levelTime[i], MenuStyle.Item, ink);
        MenuStyle.DrawRotLabel(new Rect(sx, ry + 24f, r.width - (sx - r.x) - 8f, 20f), tilt, levelScore[i], MenuStyle.Small, dim);
        return Picked(r);
    }

    /// The 0.1 s shiver a card gives when it is picked out, the same as the rows
    static void Jitter(ref Rect r, bool selected, float t)
    {
        float since = t - MenuStyle.SelectedAt;
        if (!selected || since >= 0.1f) return;
        float j = Mathf.Exp(-since / 0.04f);
        r.x += Mathf.Sin(t * 95f) * 4f * j;
        r.y += Mathf.Sin(t * 95f) * 2f * j;
    }

    /// The card's sheet: the hard shadow and the panel — cream for the two frames it lands, then charcoal, or the
    /// lit sheet when picked. Returns the sheet's colour, so the picture's mat can match it.
    static Color CardBody(Rect r, float tilt, float a, float ease, bool selected, float t)
    {
        bool landing = ease >= 0.9f && ease < 1f;
        float shadow = selected ? 12f : 7f;
        MenuStyle.DrawRotRect(new Rect(r.x + shadow, r.y + shadow, r.width, r.height), tilt, new Color(0f, 0f, 0f, 0.55f * a));
        Color sheet = Fade(landing ? MenuStyle.Paper : selected ? MenuStyle.S.sheet : MenuStyle.S.slate, a);
        MenuStyle.DrawRotRect(r, tilt, sheet);
        return sheet;
    }

    /// The card's picture: the photograph (or the ink plate saying one is coming) printed through a halftone
    /// screen that clears when the card is picked, the glint that crosses it once on the pick, the stepped brass
    /// mat (the sheet covers the photo's corners in two steps, the brass runs the staircase) and the chevron rule
    /// under it
    void CardPicture(Rect pic, Texture tex, float a, bool selected, float t, Color sheet)
    {
        // THE POSTCARD: a cream mat round the photograph, whatever the sheet (the frame's corner steps are cut from it)
        sheet = Fade(MenuStyle.Paper, a);
        Fill(new Rect(pic.x - 6f, pic.y - 6f, pic.width + 12f, pic.height + 12f), sheet);
        if (tex != null)
        {
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.DrawTexture(pic, tex, ScaleMode.ScaleAndCrop);
        }
        else
        {
            GUI.color = Fade(MenuStyle.Ink, a);
            GUI.DrawTexture(pic, Texture2D.whiteTexture);
            GUI.color = new Color(MenuStyle.S.accent.r, MenuStyle.S.accent.g, MenuStyle.S.accent.b, a * (0.5f + 0.3f * Mathf.Sin(t * 4f)));
            UIScale.Label(pic, "BUILDING PREVIEW", thumbNoteStyle);
        }
        // The picture is a poster now (PosterKit): no halftone print screen over it (2026-10-16)
        // Once on the pick the print catches the light: a diagonal glint crossing the picture. The strip is
        // rotated about its own centre so it travels straight across, and its soft ends fall on the edges.
        float since = t - MenuStyle.SelectedAt;
        if (selected && since < 0.45f)
        {
            float k = since / 0.45f;
            GUI.BeginGroup(pic); // the picture is the clip: the glint never leaves it
            Rect glint = new Rect(Mathf.Lerp(-120f, pic.width, k), -pic.height * 0.125f, 120f, pic.height * 1.25f);
            Matrix4x4 m = GUI.matrix;
            UIScale.RotateAround(-30f, glint.center);
            GUI.color = Fade(MenuStyle.Paper, 0.30f * a * Mathf.Sin(Mathf.PI * k));
            GUI.DrawTexture(glint, MenuArt.StreakTex, ScaleMode.StretchToFill);
            GUI.matrix = m;
            GUI.EndGroup();
        }
        const float step = 6f;
        GUI.color = sheet;
        Texture2D white = Texture2D.whiteTexture;
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
            {
                float cxp = sx < 0 ? pic.x : pic.xMax, cyp = sy < 0 ? pic.y : pic.yMax;
                GUI.DrawTexture(new Rect(sx < 0 ? cxp : cxp - 2f * step, sy < 0 ? cyp : cyp - step, 2f * step, step), white);
                GUI.DrawTexture(new Rect(sx < 0 ? cxp : cxp - step, sy < 0 ? cyp : cyp - 2f * step, step, 2f * step), white);
            }
        MenuStyle.StepFrame(new Rect(pic.x - 1f, pic.y - 1f, pic.width + 2f, pic.height + 2f), Fade(MenuStyle.S.accent, (selected ? 0.9f : 0.5f) * a), 1f, step, 2);
        MenuStyle.AccentStrip(new Rect(pic.x, pic.yMax, pic.width, 6f), a);
    }

    /// The card's name: cream on charcoal, ink over its two plates on the picked sheet, limestone when shut
    static void CardName(Rect nr, float tilt, string name, Color c, bool fringed, float a)
    {
        if (fringed)
        {
            MenuStyle.DrawRotLabel(new Rect(nr.x - 2f, nr.y, nr.width, nr.height), tilt, name, MenuStyle.ItemSelected, Fade(MenuStyle.Oxblood, 0.7f * a));
            MenuStyle.DrawRotLabel(new Rect(nr.x + 2f, nr.y + 1f, nr.width, nr.height), tilt, name, MenuStyle.ItemSelected, Fade(MenuStyle.DeepTeal, 0.7f * a));
        }
        MenuStyle.DrawRotLabel(nr, tilt, name, MenuStyle.ItemSelected, c);
    }

    /// The asset tag beside a card's code, tilted with it
    void CardTag(Rect codeR, float tilt, Rect bar, Color c, float alpha)
    {
        Matrix4x4 m = GUI.matrix;
        UIScale.RotateAround(tilt, codeR.center);
        Tag(new Rect(bar.x, bar.y + 1f, bar.width, 7f), c, alpha);
        GUI.matrix = m;
    }

    /// The company's seal on the picked card, rotated with it and punched in
    static void CardSeal(Rect r, float ty, float tilt, float a, float t)
    {
        Rect seal = new Rect(r.xMax - 36f, ty, 22f, 22f);
        float punch = 1f + 0.4f * Mathf.Pow(1f - Mathf.Clamp01((t - MenuStyle.SelectedAt) / 0.12f), 2f);
        Matrix4x4 m = GUI.matrix;
        UIScale.RotateAround(tilt, r.center);
        UIScale.ScaleAround(new Vector2(punch, punch), seal.center);
        GUI.color = Fade(MenuStyle.Ink, a);
        GUI.DrawTexture(seal, GameFont.Logo);
        GUI.matrix = m;
        GUI.color = Color.white;
    }

    /// The tag: a double rule across `r` — corporate ornament, never information
    static void Tag(Rect r, Color c, float alpha)
    {
        MenuStyle.DoubleRule(r.x, r.y, r.width, Fade(c, alpha));
    }

    /// Four rects `thick` px wide around `r`, `inset` px inside its edge (negative = outside it)
    static void Frame(Rect r, float thick, Color c, float inset)
    {
        GUI.color = c;
        Rect f = new Rect(r.x + inset, r.y + inset, r.width - inset * 2f, r.height - inset * 2f);
        GUI.DrawTexture(new Rect(f.x, f.y, f.width, thick), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(f.x, f.yMax - thick, f.width, thick), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(f.x, f.y, thick, f.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(f.xMax - thick, f.y, thick, f.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    static Color Fade(Color c, float a) => new Color(c.r, c.g, c.b, a);

    static void Fill(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    /// A click on the card (the drawn rect, expansion and shake included)
    static bool Picked(Rect r)
    {
        var e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition))
        {
            e.Use();
            return true;
        }
        return false;
    }
}
