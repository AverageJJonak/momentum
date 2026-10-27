using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Persona-styled pause: Esc freezes time, a gold slash cuts the dimmed frame,
/// tilted PAUSED title, slash-panel entries with keyboard + mouse navigation.
/// Added by GameBootstrap in every gameplay scene; IsPaused gates gameplay input.
/// In the practice range a PRACTICE page joins the list: the range's console.
/// The frozen frame goes to night under a veil and projector grain; your live
/// rank watermarks the page; a stepped brass frame with plate corners rules the
/// edges; and every seven-odd seconds one idle row does what the city's
/// holograms do.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    public MovementConfig config; // the settings page reads the field of view from it

    static readonly string[] LevelLabels = { "RESUME", "SETTINGS", "RESTART SCENE", "THE DIRECTORY", "QUIT" };
    static readonly string[] RangeLabels = { "RESUME", "PRACTICE", "SETTINGS", "RESTART SCENE", "THE DIRECTORY", "QUIT" };
    static string DocLine => MenuStyle.S.pauseLine;       // the property you are standing in has the words
    static string ServiceLine => MenuStyle.S.serviceLine;
    string[] labels = LevelLabels;
    SettingsPanel settings;
    PracticePanel practice;

    int selected;
    float openedAt;
    AudioSource audioSource;
    AudioClip moveBlip, confirmBlip;

    // The rot schedulers: the band's drop-frame and the list's glitch. Advanced ONLY in Update, read in OnGUI.
    MenuStyle.RotClock bandClock, rotClock;
    // Where each row was last drawn (its selected shape), so a pick can burn its afterimage there
    Rect[] rowRects;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.volume = GameSettings.SfxVolume;
        moveBlip = UIAssets.Clip("rollover2") ?? MenuStyle.MakeMoveBlip();      // THE BORROWED CLICKS (2026-10-27): Kenney's ui-audio when it is there, the synthesised blips without it
        confirmBlip = UIAssets.Clip("click1") ?? MenuStyle.MakeConfirm();
        labels = Practice.Active ? RangeLabels : LevelLabels;
        rowRects = new Rect[labels.Length];
        settings = new SettingsPanel(config, audioSource, moveBlip, confirmBlip)
        {
            OnClosed = () => Back("SETTINGS"),
        };
        practice = new PracticePanel(audioSource, moveBlip, confirmBlip)
        {
            OnClosed = () => Back("PRACTICE"),
        };
    }

    /// A page closed: the list comes back on that page's row
    void Back(string label)
    {
        selected = Mathf.Max(0, System.Array.IndexOf(labels, label));
        openedAt = Time.unscaledTime;
        MenuStyle.KickAt = openedAt;
        audioSource.PlayOneShot(moveBlip, 0.7f);
    }

    void OnDisable()
    {
        // Scene unload while paused must never strand a frozen timescale
        if (IsPaused)
        {
            IsPaused = false;
            Time.timeScale = 1f;
        }
    }

    bool PageOpen => (settings != null && settings.IsOpen) || (practice != null && practice.IsOpen);

    void Update()
    {
        if (IsPaused) AdvanceClocks();

        var kb = Keyboard.current;
        if (kb == null || LevelResults.Active) return; // the report card owns the screen

        // The pages own their input (including Esc) while they are up
        if (IsPaused && settings != null && settings.IsOpen)
        {
            settings.Update(kb);
            return;
        }
        if (IsPaused && practice != null && practice.IsOpen)
        {
            practice.Update(kb);
            return;
        }

        if (kb.escapeKey.wasPressedThisFrame)
        {
            if (IsPaused) Resume();
            else Pause();
            return;
        }
        if (!IsPaused) return;

        int dir = 0;
        if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) dir = 1;
        if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) dir = -1;
        if (dir != 0)
        {
            selected = (selected + dir + labels.Length) % labels.Length;
            MenuStyle.Bump();
            audioSource.PlayOneShot(moveBlip, 0.7f);
        }
        if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
            Activate(selected);
    }

    /// The band tears a frame every twelve-odd seconds; the list glitches every seven, once it has landed and
    /// only while it is the page on screen — never the selected row
    void AdvanceClocks()
    {
        float now = Time.unscaledTime;
        MenuStyle.AdvanceRot(ref bandClock, now, 12f, 1, -1, -1, 0.1f);
        bool landed = now - openedAt > labels.Length * 0.1f + 1f / 4.5f;
        int rows = landed && !PageOpen ? labels.Length : 0;
        MenuStyle.AdvanceRot(ref rotClock, now, 7f, rows, selected);
    }

    void Pause()
    {
        IsPaused = true;
        selected = 0;
        openedAt = Time.unscaledTime;
        MenuStyle.KickAt = openedAt;
        bandClock = new MenuStyle.RotClock();
        rotClock = new MenuStyle.RotClock();
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Resume()
    {
        IsPaused = false;
        Time.timeScale = Practice.TimeScale; // 1, unless the range's console is running slow
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Activate(int index)
    {
        if (rowRects != null && index >= 0 && index < rowRects.Length) MenuStyle.Confirm(rowRects[index]);
        audioSource.PlayOneShot(confirmBlip, 0.8f);
        switch (labels[index])
        {
            case "RESUME": Resume(); break;
            case "PRACTICE": MenuStyle.KickAt = Time.unscaledTime; practice.Open(); break;
            case "SETTINGS": MenuStyle.KickAt = Time.unscaledTime; settings.Open(); break;
            case "RESTART SCENE": LoadScene(SceneManager.GetActiveScene().name); break;
            case "THE DIRECTORY": LoadScene("MainMenu"); break;
            case "QUIT": Application.Quit(); break;
        }
    }

    void LoadScene(string scene)
    {
        IsPaused = false;
        Time.timeScale = 1f;
        // RESTART SCENE restarts the level and nothing else: the run stays where it is. THE DIRECTORY leaves the run
        // saved at this level.
        SceneManager.LoadScene(scene);
    }

    void OnGUI()
    {
        if (!IsPaused) return;
        MenuStyle.EnsureStyles();
        MenuStyle.Use(MenuStyle.LevelSkin); // the pause prints in the material of the floor you are on
        GUI.depth = -10;
        UIScale.Begin();
        float w = UIScale.W, h = UIScale.H;
        float t = Time.unscaledTime;
        Rect full = new Rect(0f, 0f, w, h);

        // (1) The veil: the frozen frame goes to night
        GUI.color = new Color(MenuStyle.Veil.r, MenuStyle.Veil.g, MenuStyle.Veil.b, 0.78f);
        GUI.DrawTexture(full, Texture2D.whiteTexture);
        GUI.color = Color.white;
        // (2) Projector grain, re-seated eight times a second
        int frame = Mathf.FloorToInt(t * 8f);
        GUI.color = new Color(1f, 1f, 1f, 0.14f);
        MenuArt.Tile(full, MenuArt.GrainTex, 1f, new Vector2(MenuArt.Hash01(frame, 1, 9) * 128f, MenuArt.Hash01(frame, 2, 9) * 128f));
        GUI.color = Color.white;
        // (4) Your live rank, watermarked into the page
        DrawWatermark(w, h, t);
        // (5) The document frame
        DrawDocumentFrame(w, h);

        // (6) The band, the title on its centreline, and the drop-frame torn over both
        float bandY = h * 0.23f;
        MenuStyle.StripeBand(w, bandY, MenuStyle.BandAngle, 1f, t - openedAt);
        MenuStyle.Title.fontSize = (int)Mathf.Min(84f, h * 0.1f);
        bool inSettings = settings != null && settings.IsOpen;
        bool inPractice = practice != null && practice.IsOpen;
        bool onList = !inSettings && !inPractice;
        string title = inSettings ? settings.Title : inPractice ? practice.Title : "INTERMISSION";
        string doc = inSettings ? settings.DocLine : inPractice ? practice.DocLine : onList ? DocLine : null;
        float y = MenuStyle.TitleBlock(w * 0.09f, w, bandY, title, MenuStyle.BandAngle, doc) + 30f;
        MenuStyle.BandDropFrame(in bandClock, w, bandY);

        if (!onList)
        {
            if (inSettings) settings.Draw(w, h, y); else practice.Draw(w, h, y);
            // The hint, right of the company mark so the two never cross: one line, measured (the range's is long)
            GUI.color = A(MenuStyle.Paper, 0.45f);
            string hint = inSettings ? settings.Hint : practice.Hint;
            float hintX = w * 0.09f + 360f;
            MenuStyle.FitLabel(new Rect(hintX, h - 30f, w * 0.91f - hintX, 24f), hint, MenuStyle.Sub);
            GUI.color = Color.white;
            DrawBrand(w, h, false);
            MenuStyle.DrawAfterimage();
            UIScale.End();
            return;
        }

        // (7) The list
        if (rowRects == null || rowRects.Length != labels.Length) rowRects = new Rect[labels.Length];
        string rotWord = MenuStyle.RotWord(in rotClock);
        float itemW = Mathf.Min(440f, w * 0.42f);
        float rowH = Mathf.Clamp((h - y - 30f) / labels.Length, 42f, 62f);
        for (int i = 0; i < labels.Length; i++)
        {
            float slide = (t - openedAt) * 4.5f - i * 0.1f;
            Rect r = new Rect(w * 0.09f, y, itemW, rowH - 12f);
            rowRects[i] = new Rect(r.x - 14f, r.y - 3f, r.width + 34f, r.height + 6f);
            if (MenuStyle.Hovered(r, slide) && selected != i)
            {
                selected = i;
                MenuStyle.Bump();
                audioSource.PlayOneShot(moveBlip, 0.5f);
            }
            int tick = slide >= 1f ? MenuStyle.RotTick(in rotClock, i, t) : -1; // a row still sliding never glitches
            if (MenuStyle.SlashItem(r, labels[i], null, selected == i, slide, null, i + 1, tick, rotWord))
                Activate(i);
            y += rowH;
        }

        // (8) The company, bottom-left, and what it expects of you
        DrawBrand(w, h, true);
        // (9) The afterimage of the last pick, over whatever came next
        MenuStyle.DrawAfterimage();
        UIScale.End();
    }

    /// The live rank letter, huge and faint, slanted with the band: a 300 pt glyph scaled up, never a bigger font
    void DrawWatermark(float w, float h, float t)
    {
        var director = CombatDirector.Instance;
        var style = director != null ? director.style : null;
        if (style == null) return;
        string rank = style.Rank;
        if (string.IsNullOrEmpty(rank)) return;
        Vector2 centre = new Vector2(w * 0.86f, h * 0.62f);
        Color c = style.FlowState ? MenuStyle.HudGold : MenuStyle.Paper;
        c.a = 0.06f + 0.015f * Mathf.Sin(t * 1.9f);
        Matrix4x4 saved = GUI.matrix;
        UIScale.ScaleAround(new Vector2(1.7f, 1.7f), centre);
        MenuStyle.DrawRotLabel(new Rect(centre.x - 200f, centre.y - 200f, 400f, 400f), MenuStyle.BandAngle, rank, MenuStyle.Watermark, c);
        GUI.matrix = saved;
        GUI.color = Color.white;
    }

    /// The document frame: a stepped brass hairline inset from the edges, a plate in each corner
    static void DrawDocumentFrame(float w, float h)
    {
        Rect page = new Rect(14f, 14f, w - 28f, h - 28f);
        // THE BORROWED FRAME (2026-10-27): the plated corners of Kenney's border round the page in the skin's accent; the drawn frame without it
        if (!UIAssets.Frame(page, UIAssets.Border("border-029"), 28f, A(MenuStyle.S.accent, 0.6f)))
        {
            MenuStyle.StepFrame(page, A(MenuStyle.S.accent, 0.4f), 1f, 8f, 2);
            MenuStyle.CornerPlates(page, A(MenuStyle.S.accent, 0.5f), 6f, 20f);
        }
    }

    static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);

    /// The company mark and its name, bottom-left: the one thing on the page that never glitches
    void DrawBrand(float w, float h, bool serviceLine)
    {
        float x = w * 0.09f;
        GUI.color = new Color(MenuStyle.Gold.r, MenuStyle.Gold.g, MenuStyle.Gold.b, 0.85f);
        GUI.DrawTexture(new Rect(x - 2f, h - 46f, 28f, 28f), GameFont.Logo);
        GUI.color = MenuStyle.Gold;
        UIScale.Label(new Rect(x + 32f, h - 45f, 320f, 26f), LevelKit.CompanyFull, MenuStyle.Brand);
        if (serviceLine)
        {
            // Beside the name (measured at the rasterised size, kept), on one line out to the margin
            float sx = x + 32f + MenuStyle.FitWidth(MenuStyle.Brand, LevelKit.CompanyFull) + 18f;
            GUI.color = MenuStyle.S.subDim;
            MenuStyle.FitLabel(new Rect(sx, h - 44f, w * 0.91f - sx, 24f), ServiceLine, MenuStyle.Doc);
        }
        GUI.color = Color.white;
    }
}
