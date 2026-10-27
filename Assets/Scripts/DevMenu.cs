using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// THE BACK DOOR. Not in any menu and not on any key you would press by
/// accident: hold CTRL and SHIFT and tap F12, on the main menu or in play, and
/// a list comes up of the places that are expensive to reach — the cutscenes.
/// A number jumps to one: the level loads, the bootstrap builds it, and
/// DevJump puts you where the scene begins with everything in the way cleared
/// out of it. For the person making the game, so a cutscene can be looked at
/// forty times in an afternoon without playing the floor forty times.
/// </summary>
public class DevMenu : MonoBehaviour
{
    const string Title = "BACK DOOR   ·   CTRL + SHIFT + F12   ·   ESC CLOSES";
    static readonly string[] Lines =
    {
        "1    THE OPENING   ·   the ward, Level 1",
        "2    THE CONCIERGE   ·   the atrium door, Level 5",
        "3    THE FOREMAN   ·   the casting floor door, Level 10",
        "4    THE ENDING   ·   the override post, Level 10, the Foreman already scrap",
        "5    GROUND ZERO   ·   Level 11, the yard",
        "6    GROUND ZERO   ·   the viaduct halt",
        "7    GROUND ZERO   ·   the island in the sinkhole",
        "8    GROUND ZERO   ·   the crown of Tower 9",
        "9    GROUND ZERO   ·   the plaza under the relay tower",
        "0    SKYLINE   ·   Level 12, the gate roof",
        "-    SKYLINE   ·   the arcology, its second terrace",
        "=    SKYLINE   ·   the top of the spire",
    };
    static readonly Color Gold = new Color(1f, 0.82f, 0.35f);
    static readonly Color Paper = new Color(0.92f, 0.9f, 0.85f);

    bool open;
    GUIStyle box, title, line;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        bool chord = (kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed) && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
        if (chord && kb.f12Key.wasPressedThisFrame) { open = !open; return; }
        if (!open) return;
        if (kb.escapeKey.wasPressedThisFrame) { open = false; return; }
        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) DevJump.Go(DevJump.Where.Opening);
        else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) DevJump.Go(DevJump.Where.Concierge);
        else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) DevJump.Go(DevJump.Where.Foreman);
        else if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) DevJump.Go(DevJump.Where.Ending);
        else if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) DevJump.Go(DevJump.Where.Surface);
        else if (kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame) DevJump.Go(DevJump.Where.Halt);
        else if (kb.digit7Key.wasPressedThisFrame || kb.numpad7Key.wasPressedThisFrame) DevJump.Go(DevJump.Where.Island);
        else if (kb.digit8Key.wasPressedThisFrame || kb.numpad8Key.wasPressedThisFrame) DevJump.Go(DevJump.Where.Crown);
        else if (kb.digit9Key.wasPressedThisFrame || kb.numpad9Key.wasPressedThisFrame) DevJump.Go(DevJump.Where.Plaza);
        else if (kb.digit0Key.wasPressedThisFrame || kb.numpad0Key.wasPressedThisFrame) DevJump.Go(DevJump.Where.Rooftops);
        else if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame) DevJump.Go(DevJump.Where.Arcology);
        else if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame) DevJump.Go(DevJump.Where.Spire);
        else if (kb.leftBracketKey.wasPressedThisFrame) DevJump.Go(DevJump.Where.Hall);
        else if (kb.rightBracketKey.wasPressedThisFrame) DevJump.Go(DevJump.Where.Exchange);
        else if (kb.backslashKey.wasPressedThisFrame) DevJump.Go(DevJump.Where.Terminal);
    }

    void OnGUI()
    {
        if (!open) return;
        if (box == null)
        {
            box = new GUIStyle(GUI.skin.box);
            box.normal.background = Texture2D.blackTexture;
            title = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            title.normal.textColor = Gold;
            line = new GUIStyle(GUI.skin.label);
            line.normal.textColor = Paper;
        }
        float s = Screen.height / 1080f;
        title.fontSize = Mathf.RoundToInt(20f * s);
        line.fontSize = Mathf.RoundToInt(17f * s);
        float rowH = 32f * s, pad = 18f * s;
        float h = pad * 2f + 40f * s + rowH * Lines.Length;
        float w = 760f * s;
        Rect r = new Rect(40f * s, Screen.height - 40f * s - h, w, h);
        GUI.Box(r, GUIContent.none, box);
        GUI.Label(new Rect(r.x + pad, r.y + pad, w - pad * 2f, 30f * s), Title, title);
        for (int i = 0; i < Lines.Length; i++)
            GUI.Label(new Rect(r.x + pad, r.y + pad + 40f * s + i * rowH, w - pad * 2f, rowH), Lines[i], line);
    }
}

/// <summary>
/// Where the back door goes. `Go` picks the destination and loads its level; the bootstrap calls `Apply` once the
/// level stands and the player is on it, and the player is moved to where the scene starts — with the boss's
/// doors open and, for the ending, the boss already gone, since the ending only plays over its scrap.
/// </summary>
public static class DevJump
{
    public enum Where { None, Opening, Concierge, Foreman, Ending, Surface, Halt, Island, Crown, Plaza, Rooftops, Arcology, Spire, Hall, Exchange, Terminal }

    /// Set before the load, read once by the bootstrap after the build
    public static Where Pending { get; private set; }

    public static void Go(Where w)
    {
        Pending = w;
        RunState.Rehearse(); // a jump is a rehearsal: the scene for itself, bare (the ward's cloakroom builds live), the saved run untouched
        Time.timeScale = 1f;
        SceneManager.LoadScene(w == Where.Opening ? "Level1" : w == Where.Concierge ? "Level5" : w >= Where.Hall ? "Level13" : w >= Where.Rooftops ? "Level12" : w >= Where.Surface ? "Level11" : "Level10");
    }

    static void Land(PlayerMotor motor, Vector3 at)
    {
        motor.Teleport(at);
        GameBootstrap.Checkpoint = at;
    }

    public static void Apply(PlayerMotor motor)
    {
        var w = Pending;
        Pending = Where.None;
        if (w == Where.None || motor == null) return;
        var boss = Boss.Current;
        switch (w)
        {
            case Where.Halt: Land(motor, Level11Builder.DevHalt); break;
            case Where.Island: Land(motor, Level11Builder.DevIsland); break;
            case Where.Crown: Land(motor, Level11Builder.DevCrown); break;
            case Where.Plaza: Land(motor, Level11Builder.DevPlaza); break;
            case Where.Rooftops: Land(motor, Level12Builder.DevGate); break;
            case Where.Arcology: Land(motor, Level12Builder.DevArcology); break;
            case Where.Spire: Land(motor, Level12Builder.DevSpire); break;
            case Where.Hall: Land(motor, Level13Builder.DevHall); break;
            case Where.Exchange: Land(motor, Level13Builder.DevExchange); break;
            case Where.Terminal: Land(motor, Level13Builder.DevTerminal); break;
            case Where.Concierge:
            case Where.Foreman:
                // Just inside the arena's door: the intro plays as you walk in
                if (boss == null) return;
                motor.Teleport(boss.checkpoint);
                GameBootstrap.Checkpoint = boss.checkpoint;
                break;
            case Where.Ending:
            {
                var blast = Object.FindFirstObjectByType<MineBlast>();
                if (blast == null || blast.post == null) return;
                if (boss != null)
                {
                    // The Foreman is scrap already: its doors open, its body and its keeper gone
                    if (boss.exitSeal != null) boss.exitSeal.SetActive(false);
                    if (boss.entrySeal != null) boss.entrySeal.SetActive(false);
                    if (boss.body != null) Object.Destroy(boss.body.gameObject);
                    Object.Destroy(boss.gameObject);
                }
                Vector3 at = blast.post.at + blast.post.facing * 1.4f;   // within reach of the lever: it starts on its own
                motor.Teleport(at);
                GameBootstrap.Checkpoint = at;
                break;
            }
        }
    }
}
