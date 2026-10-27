using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The SETTINGS page (mouse sensitivity, field of view, music, sound fx, controls, back)
/// and its CONTROLS sub-page (two-slot key rebinding) as one reusable panel.
/// The main menu and the pause menu both host it: the host calls Update(kb)
/// while the panel is open, Draw() under its own band and title, and gets
/// OnClosed when BACK or Esc leaves the page. House style throughout: indexed
/// slash rows with the rot clock on the settings page, straight chip rows on
/// the controls page (so the chips align), a chevron-ruled ink banner while rebinding.
/// </summary>
public class SettingsPanel
{
    enum Page { Settings, Controls }

    const int SettingsRowCount = 10; // sensitivity, fov, music, sound fx, brightness, camera motion, toon shading, photo surfaces, controls, back
    const int BrightnessRow = 4, MotionRow = 5, ToonRow = 6, PhotoRow = 7, ControlsRow = 8;
    const int SliderRows = 6;       // the first six rows carry a slider
    const float RotMean = 14f;      // seconds between one idle row saying a word it should not
    static readonly GameAction[] ControlActions = (GameAction[])System.Enum.GetValues(typeof(GameAction));
    static int ControlsRowCount => ControlActions.Length + 2; // + reset, back

    readonly MovementConfig config;
    readonly AudioSource sfx;
    readonly AudioClip moveBlip, confirmBlip;

    /// Raised when the page is left (BACK or Esc); the host returns to its own list
    public System.Action OnClosed;
    public bool IsOpen { get; private set; }

    Page page;
    int selected;
    int selectedSlot;          // which chip Enter will rebind
    int listeningRow = -1;
    int listeningSlot;
    float listeningStartedAt;
    bool listenArmed;          // key phase only after all input is released
    string listenMsg;          // the banner's armed message, built once per rebind
    float openedAt;
    GUIStyle chipStyle, docRight;
    MenuStyle.RotClock rotClock; // advanced in Update only, read in Draw

    public SettingsPanel(MovementConfig config, AudioSource sfx, AudioClip moveBlip, AudioClip confirmBlip)
    {
        this.config = config;
        this.sfx = sfx;
        this.moveBlip = moveBlip;
        this.confirmBlip = confirmBlip;
    }

    public string Title => page == Page.Settings ? "SETTINGS" : "CONTROLS";
    /// The document line under the title, for the host's TitleBlock
    public string DocLine => page == Page.Settings ? "SETTINGS  ·  HOUSE RULES" : "CONTROLS  ·  THE HOUSE KEYS";
    public string Hint => page == Page.Settings
        ? "←/→ adjust · Esc back"
        : "↑↓ action · ←→ slot · Enter or click: rebind · Backspace clears secondary · Esc back";

    public void Open()
    {
        IsOpen = true;
        page = Page.Settings;
        selected = 0;
        listeningRow = -1;
        openedAt = Time.unscaledTime;
        RearmRot(); // re-arm with a fresh interval so the first event never lands on the throw-in
        MenuStyle.Bump();
    }

    void Close()
    {
        IsOpen = false;
        listeningRow = -1;
        PlayerPrefs.Save();
        OnClosed?.Invoke();
    }

    // The host blip source follows the SOUND FX slider, so nudging it is heard at the new level at once
    void Blip(float volume) { if (sfx == null || moveBlip == null) return; sfx.volume = GameSettings.SfxVolume; sfx.PlayOneShot(moveBlip, volume); }
    void Confirm(float volume) { if (sfx == null || confirmBlip == null) return; sfx.volume = GameSettings.SfxVolume; sfx.PlayOneShot(confirmBlip, volume); }

    public void Update(Keyboard kb)
    {
        if (!IsOpen) return;
        AdvanceRot();
        if (kb == null) return;

        // Rebinding: swallow all input until a key or mouse button is pressed
        if (listeningRow >= 0)
        {
            var mouse = Mouse.current;
            bool anyHeld = kb.anyKey.isPressed || (mouse != null &&
                (mouse.leftButton.isPressed || mouse.rightButton.isPressed || mouse.middleButton.isPressed
                 || mouse.backButton.isPressed || mouse.forwardButton.isPressed));
            if (!listenArmed)
            {
                // SELECT phase ends only once the confirming input is fully released
                if (!anyHeld && Time.unscaledTime - listeningStartedAt > 0.15f)
                {
                    listenArmed = true;
                    Keybinds.PrimeCapture();
                }
                return;
            }
            if (Keybinds.TryCapture(out Keybinds.Bind captured, out bool cancel))
            {
                Keybinds.Set(ControlActions[listeningRow], listeningSlot, captured);
                Confirm(0.8f);
                listeningRow = -1;
            }
            else if (cancel)
            {
                listeningRow = -1;
                Blip(0.6f);
            }
            return;
        }

        int rows = page == Page.Settings ? SettingsRowCount : ControlsRowCount;
        int dir = 0;
        if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) dir = 1;
        if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) dir = -1;
        if (dir != 0)
        {
            selected = (selected + dir + rows) % rows;
            Blip(0.7f);
            MenuStyle.Bump();
        }

        if (page == Page.Settings)
        {
            float adjust = 0f;
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) adjust = 1f;
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) adjust = -1f;
            if (adjust != 0f) AdjustSetting(selected, adjust);
            if (kb.escapeKey.wasPressedThisFrame) { Close(); return; }
        }
        else
        {
            // SELECT phase: left/right picks the slot the next Enter will rebind
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) { selectedSlot = 1; Blip(0.5f); }
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) { selectedSlot = 0; Blip(0.5f); }
            if (kb.backspaceKey.wasPressedThisFrame && selected < ControlActions.Length && selectedSlot == 1)
            {
                Keybinds.Set(ControlActions[selected], 1, Keybinds.Bind.None); // clear the secondary
                Blip(0.6f);
            }
            if (kb.escapeKey.wasPressedThisFrame) { BackToSettings(); return; }
        }

        if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
            Activate(selected);
    }

    // The rot scheduler, advanced here and nowhere else. No events while the rows are still landing,
    // while a slider is hot, or while a key is being rebound; the selected row is never picked.
    /// Re-arm with a fresh interval and drop any event in flight: the next OnGUI must not show a stale target on the new page
    readonly string[] valueText = new string[SliderRows];
    readonly float[] lastValue = { float.NaN, float.NaN, float.NaN, float.NaN, float.NaN, float.NaN };
    /// A slider's readout, rebuilt only when its value changes
    string Readout(int i, float v, string format, string suffix)
    {
        if (v != lastValue[i] || valueText[i] == null) { lastValue[i] = v; valueText[i] = v.ToString(format) + suffix; }
        return valueText[i];
    }

    void RearmRot() { rotClock.armed = false; rotClock.target = -1; rotClock.until = 0f; }

    void AdvanceRot()
    {
        float now = Time.unscaledTime;
        int rows = 0;
        if (GUIUtility.hotControl == 0 && listeningRow < 0)
        {
            if (page == Page.Settings)
            {
                bool landed = (now - openedAt) * 4.5f - (SettingsRowCount - 1) * 0.1f >= 1f;
                if (landed) rows = SettingsRowCount;
            }
            else rows = ControlsRowCount; // the controls rows do not slide
        }
        MenuStyle.AdvanceRot(ref rotClock, now, RotMean, rows, selected);
    }

    void Activate(int index)
    {
        Confirm(0.8f);
        if (page == Page.Settings)
        {
            if (index == ToonRow) { WorldLook.SetToon(!WorldLook.Toon); PlayerPrefs.Save(); }   // the cel look: takes effect at the next level
            else if (index == PhotoRow) { LevelKit.PhotoSets = !LevelKit.PhotoSets; PlayerPrefs.Save(); }   // the imported texture sets: takes effect at the next level
            else if (index == ControlsRow) { page = Page.Controls; selected = 0; openedAt = Time.unscaledTime; RearmRot(); MenuStyle.Bump(); }
            else if (index == SettingsRowCount - 1) Close();
            return;
        }
        if (index < ControlActions.Length) StartListening(index, selectedSlot);
        else if (index == ControlActions.Length) Keybinds.ResetDefaults();
        else BackToSettings();
    }

    void BackToSettings()
    {
        page = Page.Settings;
        selected = ControlsRow;
        openedAt = Time.unscaledTime;
        RearmRot();
        MenuStyle.Bump();
        Blip(0.7f);
    }

    void AdjustSetting(int row, float dir)
    {
        switch (row)
        {
            case 0: GameSettings.SetSensitivity(GameSettings.SensitivityMult + dir * 0.1f); break;
            case 1: GameSettings.SetFov(GameSettings.Fov(config) + dir * 2f); break;
            case 2: GameSettings.SetMusicVolume(GameSettings.MusicVolume + dir * 0.05f); break;
            case 3: GameSettings.SetSfxVolume(GameSettings.SfxVolume + dir * 0.05f); break;
            case BrightnessRow: WorldLook.SetExposure(WorldLook.Exposure + dir * 0.05f); break;   // the whole picture, at once
            case MotionRow: CameraBody.SetMotion(CameraBody.Motion + dir * 0.1f); break;   // the body camera's every layer, live; 0 = the mouse look alone
            case ToonRow: WorldLook.SetToon(dir > 0f); break;   // right: on, left: off
            case PhotoRow: LevelKit.PhotoSets = dir > 0f; break;
            default: return;
        }
        PlayerPrefs.Save();
        Blip(0.5f);
    }

    // SELECT phase → KEY phase. Capture doesn't arm until everything is
    // released and a beat passes, so the confirming Enter/click can never be
    // read as the new binding.
    void StartListening(int row, int slot)
    {
        listeningRow = row;
        listeningSlot = slot;
        selected = row;
        selectedSlot = slot;
        listeningStartedAt = Time.unscaledTime;
        listenArmed = false;
        listenMsg = $"PRESS A KEY OR MOUSE BUTTON FOR  {Keybinds.DisplayName(ControlActions[row])}  ({(slot == 0 ? "PRIMARY" : "SECONDARY")})   ·   ESC CANCELS";
        Confirm(0.6f);
    }

    public void Draw(float w, float h, float startY)
    {
        if (!IsOpen) return;
        if (page == Page.Settings) DrawSettings(w, h, startY);
        else DrawControls(w, h, startY);
    }

    static void Fill(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
    }

    static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);

    void DrawSettings(float w, float h, float startY)
    {
        // Wide rows: label left, slider mid-right, value at the far edge
        float now = Time.unscaledTime;
        float itemW = Mathf.Min(980f, w * 0.8f);
        // The rows fit the height left under the band and above the company mark at the foot: tall rows when there is
        // room, down to 34 on a square window, and the list starts higher still when even that is too much for it
        float foot = h - 56f;
        float rowH = Mathf.Clamp((foot - startY) / (SettingsRowCount + 0.4f), 34f, 68f);
        float y = Mathf.Min(startY, foot - rowH * (SettingsRowCount + 0.4f));
        string rotWord = MenuStyle.RotWord(in rotClock);
        for (int i = 0; i < SettingsRowCount; i++)
        {
            float slide = (now - openedAt) * 4.5f - i * 0.1f;
            Rect r = new Rect(w * 0.09f, y, itemW, rowH - Mathf.Min(12f, rowH * 0.25f));
            if (MenuStyle.Hovered(r, slide) && selected != i)
            {
                selected = i;
                Blip(0.5f);
                MenuStyle.Bump();
            }

            (string label, string value) = i switch
            {
                0 => ("MOUSE SENSITIVITY", Readout(0, GameSettings.SensitivityMult, "0.0", "×")),
                1 => ("FIELD OF VIEW", Readout(1, GameSettings.Fov(config), "0", "°")),
                2 => ("MUSIC", Readout(2, GameSettings.MusicVolume * 100f, "0", "%")),
                3 => ("SOUND FX", Readout(3, GameSettings.SfxVolume * 100f, "0", "%")),
                BrightnessRow => ("BRIGHTNESS", Readout(4, WorldLook.Exposure * 100f, "0", "%")),   // post-exposure, live: the picture lifts as the slider moves
                MotionRow => ("CAMERA MOTION", Readout(5, CameraBody.Motion * 100f, "0", "%")),   // the body camera (stride, landings, lean, look-ahead), live; 0 = none of it
                ToonRow => ("TOON SHADING", WorldLook.Toon ? "ON" : "OFF"),   // the cel look (Momentum/Toon); off = the lit look; from the next level
                PhotoRow => ("PHOTO SURFACES", LevelKit.PhotoSets ? "ON" : "OFF"),   // the ambientCG sets on plaster, concrete, marble, brick, stone, tile, wood; off = the drawn maps; from the next level
                ControlsRow => ("CONTROLS", "rebind keys"),
                _ => ("BACK", null),
            };

            // Slider lives inside the box's right half; the row ignores clicks there
            Rect sr = new Rect(r.x + itemW * 0.44f, r.y + r.height * 0.5f - 6f, itemW * 0.36f, 12f);
            bool hasSlider = i < SliderRows;
            if (MenuStyle.SlashItem(r, label, value, selected == i, slide, hasSlider ? MenuStyle.SliderHit(sr) : (Rect?)null,
                i + 1, MenuStyle.RotTick(in rotClock, i, now), rotWord, subDim: (i == ToonRow && !WorldLook.Toon) || (i == PhotoRow && !LevelKit.PhotoSets)))
                Activate(i);

            if (hasSlider && slide > 0.9f)
            {
                bool sel = selected == i;
                switch (i)
                {
                    case 0:
                    {
                        float v = MenuStyle.SlashSlider(sr, GameSettings.SensitivityMult, 0.3f, 3f, sel, r.center);
                        if (!Mathf.Approximately(v, GameSettings.SensitivityMult)) GameSettings.SetSensitivity(v);
                        break;
                    }
                    case 1:
                    {
                        float v = MenuStyle.SlashSlider(sr, GameSettings.Fov(config), 80f, 110f, sel, r.center);
                        if (!Mathf.Approximately(v, GameSettings.Fov(config))) GameSettings.SetFov(v);
                        break;
                    }
                    case 2:
                    {
                        float v = MenuStyle.SlashSlider(sr, GameSettings.MusicVolume, 0f, 1f, sel, r.center);
                        if (!Mathf.Approximately(v, GameSettings.MusicVolume)) GameSettings.SetMusicVolume(v);
                        break;
                    }
                    case 3:
                    {
                        float v = MenuStyle.SlashSlider(sr, GameSettings.SfxVolume, 0f, 1f, sel, r.center);
                        if (!Mathf.Approximately(v, GameSettings.SfxVolume)) GameSettings.SetSfxVolume(v);
                        break;
                    }
                    case MotionRow:
                    {
                        float v = MenuStyle.SlashSlider(sr, CameraBody.Motion, 0f, 1f, sel, r.center);
                        if (!Mathf.Approximately(v, CameraBody.Motion)) CameraBody.SetMotion(v);
                        break;
                    }
                    default:
                    {
                        float v = MenuStyle.SlashSlider(sr, WorldLook.Exposure, WorldLook.ExposureMin, WorldLook.ExposureMax, sel, r.center);
                        if (!Mathf.Approximately(v, WorldLook.Exposure)) WorldLook.SetExposure(v);
                        break;
                    }
                }
            }
            y += rowH;
        }
    }

    // Compact rebinding rows: action · primary chip · secondary chip. The rows stay straight so the
    // chips align. The targeted slot is gold under a foil frame with a paper diamond over it; Enter
    // (or a chip click) starts the key phase, and the chip ticks red and ink at 4 Hz until a key lands.
    void DrawControls(float w, float h, float startY)
    {
        if (chipStyle == null)
            chipStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        if (docRight == null && MenuStyle.Doc != null)
            docRight = new GUIStyle(MenuStyle.Doc) { alignment = TextAnchor.MiddleRight };

        float t = Time.unscaledTime;
        float rowH = Mathf.Clamp((h - 56f - startY) / ControlsRowCount, 24f, 34f); // clear of the company mark at the foot
        float x = w * 0.09f;
        float labelW = Mathf.Min(300f, w * 0.28f);
        float chipW = Mathf.Min(150f, w * 0.13f);
        float y = startY;
        var e = Event.current;
        bool listening = listeningRow >= 0;
        string rotWord = MenuStyle.RotWord(in rotClock);
        bool listenPhaseA = Mathf.Repeat(t, 0.25f) < 0.125f;
        float listenShake = Mathf.Sin(t * 131f) * 2f;

        for (int i = 0; i < ControlsRowCount; i++)
        {
            Rect row = new Rect(x, y, labelW + chipW * 2f + 32f, rowH - 4f);
            bool sel = selected == i;
            if (!listening && !sel && row.Contains(e.mousePosition))
            {
                selected = i;
                sel = true;
                Blip(0.4f);
                MenuStyle.Bump();
            }

            // The row: a programme line on the lit sheet with an oxblood sliver and a bronze rule; the picked one
            // inverts to ink with brass letters
            Fill(row, sel ? MenuStyle.Ink : A(MenuStyle.S.sheet, 0.94f));
            Fill(new Rect(row.x, row.y, 5f, row.height), sel ? MenuStyle.S.accent : MenuStyle.S.stripe);
            Fill(new Rect(row.x + 5f, row.yMax - 1f, row.width - 5f, 1f), sel ? A(MenuStyle.Foil, 0.5f) : A(MenuStyle.S.trim, 0.6f));
            Color text = sel ? MenuStyle.Gold : MenuStyle.Ink;

            // The rot clock only touches an idle row's name, and never while a key is being rebound
            int rotTick = listening || sel ? -1 : MenuStyle.RotTick(in rotClock, i, t);
            bool rotting = rotTick >= 0 && MenuStyle.RotState(rotTick) == 0;
            Rect labelRect = new Rect(x + 12f, y, i < ControlActions.Length ? labelW : labelW * 2f, rowH - 4f);

            if (i < ControlActions.Length)
            {
                GameAction a = ControlActions[i];
                GUI.color = rotting ? MenuStyle.Rot : text;
                UIScale.Label(labelRect, rotting ? rotWord : Keybinds.DisplayName(a), MenuStyle.Small);
                for (int slot = 0; slot < 2; slot++)
                {
                    Rect chip = new Rect(x + labelW + 12f + slot * (chipW + 8f), y + 3f, chipW, rowH - 10f);
                    bool isListeningChip = listening && listeningRow == i && listeningSlot == slot;
                    bool targeted = sel && selectedSlot == slot && !listening;
                    Keybinds.Bind bind = Keybinds.Get(a, slot);

                    if (isListeningChip)
                    {
                        // Red and ink, four times a second, shivering: the slot is open
                        Rect c = new Rect(chip.x + listenShake, chip.y, chip.width, chip.height);
                        Fill(c, listenPhaseA ? MenuStyle.Rot : MenuStyle.Ink);
                        GUI.color = listenPhaseA ? MenuStyle.Ink : MenuStyle.Rot;
                        UIScale.Label(c, listenArmed ? "PRESS A KEY" : "...", chipStyle);
                    }
                    else if (targeted)
                    {
                        // Gold under a foil frame, a paper diamond over it: Enter's target is obvious
                        Fill(new Rect(chip.x - 2f, chip.y - 2f, chip.width + 4f, chip.height + 4f), MenuStyle.S.accent);
                        Fill(chip, MenuStyle.S.sheet);
                        MenuStyle.DrawRotRect(new Rect(chip.center.x - 3f, chip.y - 9f, 6f, 6f), 45f, MenuStyle.Paper);
                        GUI.color = MenuStyle.Ink;
                        if (!UIAssets.Draw(new Rect(chip.x, chip.y + 1f, chip.width, chip.height - 2f), UIAssets.Glyph(bind), GUI.color)) UIScale.Label(chip, Keybinds.Label(bind), chipStyle);   // THE BORROWED GLYPH (2026-10-27): the key or mouse picture when the prompt pack has one, its label otherwise
                    }
                    else if (bind.IsNone)
                    {
                        // An empty secondary: a dashed frame and a dash
                        MenuStyle.DashFrame(chip, A(sel ? MenuStyle.S.accent : MenuStyle.S.sheetDim, 0.5f));
                        GUI.color = sel ? MenuStyle.S.accent : MenuStyle.S.sheetDim;
                        if (!UIAssets.Draw(new Rect(chip.x, chip.y + 1f, chip.width, chip.height - 2f), UIAssets.Glyph(bind), GUI.color)) UIScale.Label(chip, Keybinds.Label(bind), chipStyle);   // THE BORROWED GLYPH (2026-10-27): the key or mouse picture when the prompt pack has one, its label otherwise
                    }
                    else if (sel)
                    {
                        // On the picked ink line: a charcoal chip in a brass frame, cream type
                        Fill(new Rect(chip.x - 1f, chip.y - 1f, chip.width + 2f, chip.height + 2f), A(MenuStyle.S.accent, 0.7f));
                        Fill(chip, MenuStyle.Panel);
                        GUI.color = MenuStyle.Paper;
                        if (!UIAssets.Draw(new Rect(chip.x, chip.y + 1f, chip.width, chip.height - 2f), UIAssets.Glyph(bind), GUI.color)) UIScale.Label(chip, Keybinds.Label(bind), chipStyle);   // THE BORROWED GLYPH (2026-10-27): the key or mouse picture when the prompt pack has one, its label otherwise
                    }
                    else
                    {
                        // On the sheet: an ink chip in a bronze frame, cream type
                        Fill(chip, MenuStyle.S.trim);
                        Fill(new Rect(chip.x + 1f, chip.y + 1f, chip.width - 2f, chip.height - 2f), MenuStyle.Ink);
                        GUI.color = MenuStyle.Paper;
                        if (!UIAssets.Draw(new Rect(chip.x, chip.y + 1f, chip.width, chip.height - 2f), UIAssets.Glyph(bind), GUI.color)) UIScale.Label(chip, Keybinds.Label(bind), chipStyle);   // THE BORROWED GLYPH (2026-10-27): the key or mouse picture when the prompt pack has one, its label otherwise
                    }

                    if (!listening && e.type == EventType.MouseDown && e.button == 0 && chip.Contains(e.mousePosition))
                    {
                        StartListening(i, slot);
                        e.Use();
                    }
                }
            }
            else
            {
                bool reset = i == ControlActions.Length;
                GUI.color = rotting ? MenuStyle.Rot : text;
                UIScale.Label(labelRect, rotting ? rotWord : reset ? "RESET TO DEFAULTS" : "BACK", MenuStyle.Small);
                if (reset && docRight != null)
                {
                    // The one static red in the menus, because it is one — right-anchored in a box wide enough never to wrap
                    GUI.color = MenuStyle.Rot;
                    MenuStyle.FitLabel(new Rect(x + labelW * 2f - 250f, y, 240f, rowH - 4f), "IRREVERSIBLE", docRight);
                }
                if (!listening && e.type == EventType.MouseDown && e.button == 0 && row.Contains(e.mousePosition))
                {
                    Activate(i);
                    e.Use();
                }
            }
            GUI.color = Color.white;
            y += rowH;
        }

        // KEY phase banner — unmistakable: an ink band with a chevron rule along each edge and a hairline inside them
        if (listening)
        {
            Rect band = new Rect(0f, h * 0.5f - 34f, w, 68f);
            Fill(band, A(MenuStyle.Ink, 0.85f));
            MenuStyle.ChevronRule(new Rect(0f, band.y - 8f, w, 8f), MenuStyle.S.accent);
            MenuStyle.ChevronRule(new Rect(0f, band.yMax, w, 8f), MenuStyle.S.accent);
            Fill(new Rect(0f, band.y + 4f, w, 1f), A(MenuStyle.S.accent, 0.5f));
            Fill(new Rect(0f, band.yMax - 5f, w, 1f), A(MenuStyle.S.accent, 0.5f));
            if (listenArmed)
            {
                GUI.color = A(MenuStyle.Rot, 0.7f);
                UIScale.Label(new Rect(band.x - 2f, band.y, band.width, band.height), listenMsg, MenuStyle.Tagline);
                GUI.color = A(MenuStyle.DeepTeal, 0.7f);
                UIScale.Label(new Rect(band.x + 2f, band.y + 1f, band.width, band.height), listenMsg, MenuStyle.Tagline);
                GUI.color = MenuStyle.Paper;
                UIScale.Label(band, listenMsg, MenuStyle.Tagline);
            }
            else
            {
                GUI.color = MenuStyle.S.subDim;
                UIScale.Label(band, "RELEASE ALL KEYS...", MenuStyle.Tagline);
            }
            GUI.color = Color.white;
        }
    }
}
