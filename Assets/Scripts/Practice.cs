using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// THE RANGE'S CONSOLE. The practice range is a sandbox, and these are its
/// switches: god mode, flow state held, stamina that never runs out, slow
/// motion, enemies frozen, kills that stick; the range cleared or respawned
/// wholesale, a heal; and any unit in the game spawned at the crosshair.
/// Nothing here reaches a level: Active is true only in the range, every
/// switch is gated on it, and the bootstrap clears them on the way into
/// anything else. F-keys in play, or the PRACTICE page of the pause menu.
/// </summary>
public static class Practice
{
    public static bool Active;
    public static bool God, AlwaysFlow, InfiniteStamina, SlowMotion, Frozen, NoRespawn;

    public static float TimeScale => Active && SlowMotion ? 0.35f : 1f;
    public static bool Any => Active && (God || AlwaysFlow || InfiniteStamina || SlowMotion || Frozen || NoRespawn);

    public static void Reset()
    {
        God = AlwaysFlow = InfiniteStamina = SlowMotion = Frozen = NoRespawn = false;
    }

    /// The switches take effect: the clock, and whether the dead come back
    public static void Apply()
    {
        Time.timeScale = PauseMenu.IsPaused ? 0f : TimeScale;
        if (Active) Enemy.RespawnEnabled = !NoRespawn;
    }

    /// What is on, for the HUD
    public static string Summary
    {
        get
        {
            var s = new System.Text.StringBuilder("RANGE CONSOLE");
            if (God) s.Append("  ·  GOD");
            if (AlwaysFlow) s.Append("  ·  FLOW HELD");
            if (InfiniteStamina) s.Append("  ·  STAMINA");
            if (SlowMotion) s.Append("  ·  SLOW");
            if (Frozen) s.Append("  ·  FROZEN");
            if (NoRespawn) s.Append("  ·  KILLS STICK");
            return s.ToString();
        }
    }

    public static void RespawnAll()
    {
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) e.Revive();
    }

    public static void ClearRange()
    {
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) e.Vanish();
    }

    public static void Heal()
    {
        var h = Object.FindFirstObjectByType<PlayerHealth>();
        if (h != null) h.Restore();
    }
}

/// <summary>
/// The console's keys, in play: F1–F6 the switches, F7 respawn everything, F8
/// clear the range, F9 heal, 1–7 a unit at the crosshair. Lives on the player
/// in the range only.
/// </summary>
public class PracticeConsole : MonoBehaviour
{
    public MovementConfig config;
    public PlayerMotor motor;
    public PlayerLook look;
    public PlayerHealth health;

    public static PracticeConsole Instance { get; private set; }
    public static readonly string[] Units = { "DUMMY", "TURRET", "CHASER", "JANITOR", "BORER", "RIGGER", "BLASTER", "PURSUIT", "K-UNIT", "INTERCEPTOR", "KETTLER", "DAMAGE CONTROL", "SENTRY", "SKITTER", "MARSHAL", "WARDEN", "LURKER", "MOTH", "OCULAR", "OVERSEER", "SCOURER" };

    void Awake() { Instance = this; }
    void OnDisable() { if (Instance == this) Instance = null; }

    void Update()
    {
        if (!Practice.Active || PauseMenu.IsPaused || LevelResults.Active) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        bool changed = false;
        if (kb.f1Key.wasPressedThisFrame) { Practice.God = !Practice.God; changed = true; }
        if (kb.f2Key.wasPressedThisFrame) { Practice.AlwaysFlow = !Practice.AlwaysFlow; changed = true; }
        if (kb.f3Key.wasPressedThisFrame) { Practice.InfiniteStamina = !Practice.InfiniteStamina; changed = true; }
        if (kb.f4Key.wasPressedThisFrame) { Practice.SlowMotion = !Practice.SlowMotion; changed = true; }
        if (kb.f5Key.wasPressedThisFrame) { Practice.Frozen = !Practice.Frozen; changed = true; }
        if (kb.f6Key.wasPressedThisFrame) { Practice.NoRespawn = !Practice.NoRespawn; changed = true; }
        if (kb.f7Key.wasPressedThisFrame) { Practice.RespawnAll(); Click(); }
        if (kb.f8Key.wasPressedThisFrame) { Practice.ClearRange(); Click(); }
        if (kb.f9Key.wasPressedThisFrame) { Practice.Heal(); Click(); }
        if (changed) { Practice.Apply(); Click(); }
        for (int i = 0; i < Units.Length && i < 10; i++)   // 1..9 then 0: there are only ten digits (the rest are on the pause page's spawn row)
            if (kb[Key.Digit1 + i].wasPressedThisFrame) Spawn(i);
    }

    static void Click()
    {
        if (GameFeel.Instance != null) GameFeel.Instance.PlayPipRegen();
    }

    /// A unit at the crosshair: on the floor under where you are looking (a turret hangs a little above it)
    public void Spawn(int unit)
    {
        if (config == null || motor == null) return;
        Vector3 origin = motor.CurrPosition + Vector3.up * motor.EyeHeight;
        Vector3 dir = look != null ? Quaternion.Euler(look.Pitch, look.Yaw, 0f) * Vector3.forward : Vector3.forward;
        Vector3 at = Physics.Raycast(origin, dir, out RaycastHit hit, 80f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            ? hit.point + hit.normal * 0.6f : origin + dir * 12f;
        Vector3 ground = Physics.Raycast(at + Vector3.up * 0.5f, Vector3.down, out RaycastHit g, 80f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            ? g.point : at;
        switch (Mathf.Clamp(unit, 0, Units.Length - 1))
        {
            case 0: Enemy.CreateDummy(ground, config); break;
            case 1: Enemy.CreateTurret(ground + Vector3.up * 3f, config); break;
            case 2: Enemy.CreateChaser(ground, config); break;
            case 3: Enemy.CreateJanitor(ground, config); break;
            case 4: Enemy.CreateBorer(ground, config); break;
            case 5: Enemy.CreateRigger(ground, config); break;
            case 6: Enemy.CreateBlaster(ground, config); break;
            case 7: Enemy.CreatePursuit(ground, config); break;
            case 8: for (int k = 0; k < 2; k++) Enemy.CreateHound(ground + Quaternion.Euler(0f, k * 180f, 0f) * Vector3.forward * 1.6f, config); break; // they come in pairs
            case 9: Enemy.CreateInterceptor(ground + Vector3.up * 6f, config); break;
            case 10: Enemy.CreateKettler(ground, config); break;
            case 11: Enemy.CreateDamageControl(ground, config); break;
            case 12: Enemy.CreateSentry(ground, config); break;
            case 13: Enemy.CreateSkitters(ground, config); break;   // three, like the pool fields them
            case 14: Enemy.CreateMarshal(ground, config); break;
            case 15: Enemy.CreateWarden(ground, config); break;
            case 16: Enemy.CreateLurker(ground, config); break;
            case 17: Enemy.CreateMoth(ground + Vector3.up * 4f, config); break;
            case 18: Enemy.CreateOcular(ground + Vector3.up * 5f, config); break;
            case 19: Enemy.CreateOverseer(ground + Vector3.up * 7f, config); break;
            default: Enemy.CreateScourer(ground, config); break;
        }
        Click();
    }
}

/// <summary>
/// The PRACTICE page of the pause menu, in the range: the switches with their
/// state, the wholesale actions, a spawn row (left and right pick the unit),
/// and back. Keyboard and mouse, like the settings pages.
/// </summary>
public class PracticePanel
{
    public System.Action OnClosed;
    public bool IsOpen { get; private set; }
    public string Title => "PRACTICE";
    public string DocLine => "RANGE CONSOLE · FORM 07";
    public string Hint => "IN PLAY: F1–F6 THE SWITCHES · F7 RESPAWN · F8 CLEAR · F9 HEAL · 1–7 SPAWN AT THE CROSSHAIR";

    static readonly string[] Rows =
    {
        "GOD MODE", "FLOW STATE HELD", "INFINITE STAMINA", "SLOW MOTION", "FREEZE ENEMIES", "KILLS STICK",
        "RESPAWN EVERYTHING", "CLEAR THE RANGE", "HEAL", "SPAWN AT THE CROSSHAIR", "BACK",
    };
    const int SpawnRow = 9;

    readonly AudioSource sfx;
    readonly AudioClip moveBlip, confirmBlip;
    int selected, unit;
    float openedAt;

    public PracticePanel(AudioSource sfx, AudioClip moveBlip, AudioClip confirmBlip)
    {
        this.sfx = sfx;
        this.moveBlip = moveBlip;
        this.confirmBlip = confirmBlip;
    }

    public void Open()
    {
        IsOpen = true;
        selected = 0;
        openedAt = Time.unscaledTime;
        Blip(0.7f);
    }

    void Close()
    {
        IsOpen = false;
        OnClosed?.Invoke();
    }

    void Blip(float volume)
    {
        if (sfx != null && moveBlip != null) sfx.PlayOneShot(moveBlip, volume);
    }

    public void Update(Keyboard kb)
    {
        if (kb.escapeKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame) { Close(); return; }
        int dir = 0;
        if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) dir = 1;
        if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) dir = -1;
        if (dir != 0)
        {
            selected = (selected + dir + Rows.Length) % Rows.Length;
            MenuStyle.Bump();
            Blip(0.7f);
        }
        if (selected == SpawnRow)
        {
            int turn = 0;
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) turn = -1;
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) turn = 1;
            if (turn != 0)
            {
                unit = (unit + turn + PracticeConsole.Units.Length) % PracticeConsole.Units.Length;
                Blip(0.5f);
            }
        }
        if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) Activate(selected);
    }

    void Activate(int i)
    {
        if (sfx != null && confirmBlip != null) sfx.PlayOneShot(confirmBlip, 0.8f);
        switch (i)
        {
            case 0: Practice.God = !Practice.God; break;
            case 1: Practice.AlwaysFlow = !Practice.AlwaysFlow; break;
            case 2: Practice.InfiniteStamina = !Practice.InfiniteStamina; break;
            case 3: Practice.SlowMotion = !Practice.SlowMotion; break;
            case 4: Practice.Frozen = !Practice.Frozen; break;
            case 5: Practice.NoRespawn = !Practice.NoRespawn; break;
            case 6: Practice.RespawnAll(); break;
            case 7: Practice.ClearRange(); break;
            case 8: Practice.Heal(); break;
            case SpawnRow: if (PracticeConsole.Instance != null) PracticeConsole.Instance.Spawn(unit); break;
            default: Close(); return;
        }
        Practice.Apply();
    }

    static string OnOff(bool on) => on ? "ON" : "OFF";

    public void Draw(float w, float h, float startY)
    {
        if (!IsOpen) return;
        float itemW = Mathf.Min(760f, w * 0.62f);
        float rowH = Mathf.Clamp((h - startY - 30f) / (Rows.Length + 0.4f), 34f, 56f);
        float y = startY;
        for (int i = 0; i < Rows.Length; i++)
        {
            float slide = (Time.unscaledTime - openedAt) * 4.5f - i * 0.08f;
            Rect r = new Rect(w * 0.09f, y, itemW, rowH - 10f);
            if (MenuStyle.Hovered(r, slide) && selected != i)
            {
                selected = i;
                MenuStyle.Bump();
                Blip(0.5f);
            }
            string value = i switch
            {
                0 => OnOff(Practice.God),
                1 => OnOff(Practice.AlwaysFlow),
                2 => OnOff(Practice.InfiniteStamina),
                3 => OnOff(Practice.SlowMotion),
                4 => OnOff(Practice.Frozen),
                5 => OnOff(Practice.NoRespawn),
                SpawnRow => "<  " + PracticeConsole.Units[unit] + "  >",
                _ => null,
            };
            // ON reads gold, OFF reads sick: the switch's state at a glance
            if (MenuStyle.SlashItem(r, Rows[i], value, selected == i, slide, index: i + 1, subDim: value == "OFF")) Activate(i);
            y += rowH;
        }
    }
}
