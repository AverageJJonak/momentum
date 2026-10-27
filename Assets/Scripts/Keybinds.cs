using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// THE ONE KIT's keys (the simplification, 2026-10-16): Strike is the JAB, Kick is THE KICK (it keeps the alt cast's
/// RMB), Parry is the MOVEMENT PARRY (everyone's), Grapple is THE WHIP. Nothing switches, nothing has a slot or a mode
public enum GameAction { Forward, Back, Left, Right, Jump, Crouch, Dash, Slam, Grapple, Parry, Strike, Kick, Respawn, Trick }

/// <summary>
/// Rebindable input: two slots per action (primary + secondary), keyboard keys
/// or mouse buttons, persisted in PlayerPrefs. Every gameplay input reads
/// through Down()/Held(). Esc is reserved for pause and cannot be bound.
/// </summary>
public static class Keybinds
{
    public struct Bind
    {
        public Key key;   // Key.None when unused
        public int mouse; // 0/1/2 = LMB/RMB/MMB, -1 when unused
        public bool IsNone => key == Key.None && mouse < 0;
        public static Bind K(Key k) => new Bind { key = k, mouse = -1 };
        public static Bind M(int b) => new Bind { key = Key.None, mouse = b };
        public static Bind None => new Bind { key = Key.None, mouse = -1 };
    }

    static readonly Dictionary<GameAction, Bind[]> Defaults = new Dictionary<GameAction, Bind[]>
    {
        { GameAction.Forward, new[] { Bind.K(Key.W), Bind.K(Key.UpArrow) } },
        { GameAction.Back,    new[] { Bind.K(Key.S), Bind.K(Key.DownArrow) } },
        { GameAction.Left,    new[] { Bind.K(Key.A), Bind.K(Key.LeftArrow) } },
        { GameAction.Right,   new[] { Bind.K(Key.D), Bind.K(Key.RightArrow) } },
        { GameAction.Jump,    new[] { Bind.K(Key.Space), Bind.None } },
        { GameAction.Crouch,  new[] { Bind.K(Key.LeftCtrl), Bind.K(Key.C) } },
        { GameAction.Dash,    new[] { Bind.K(Key.LeftShift), Bind.None } },
        { GameAction.Slam,    new[] { Bind.K(Key.Q), Bind.None } },
        { GameAction.Grapple, new[] { Bind.K(Key.E), Bind.None } },
        { GameAction.Parry,   new[] { Bind.K(Key.F), Bind.M(2) } },
        { GameAction.Strike,  new[] { Bind.None, Bind.None } },   // the jab is unbound (2026-10-26: no units; the left button is THE TRICK)
        { GameAction.Kick,    new[] { Bind.M(1), Bind.None } },
        { GameAction.Respawn, new[] { Bind.K(Key.R), Bind.None } },
        { GameAction.Trick,   new[] { Bind.M(0), Bind.K(Key.G) } },   // THE TRICK (2026-10-26, user: "use the left click for qte events, for landing tricks"): the left button, or G
    };

    static Dictionary<GameAction, Bind[]> binds;
    // The layout's version: the kick took the parry's old RMB (2), the trick arrived (3), so bindings saved under an
    // older layout are let go once and the defaults land
    const string VersionKey = "bind_version";
    const int Version = 4;   // 4: the trick on the left button, the jab unbound

    static string PrefKey(GameAction a, int slot) => $"bind_{a}_{slot}";
    static string Serialize(Bind b) => b.IsNone ? "NONE" : b.mouse >= 0 ? $"M:{b.mouse}" : $"K:{b.key}";

    static Bind Parse(string s, Bind fallback)
    {
        if (s == "NONE") return Bind.None;
        if (s.StartsWith("M:") && int.TryParse(s.Substring(2), out int m)) return Bind.M(m);
        if (s.StartsWith("K:") && Enum.TryParse(s.Substring(2), out Key k)) return Bind.K(k);
        return fallback;
    }

    static void EnsureLoaded()
    {
        if (binds != null) return;
        if (PlayerPrefs.GetInt(VersionKey, 1) < Version)
        {
            foreach (var kv in Defaults) for (int s = 0; s < 2; s++) PlayerPrefs.DeleteKey(PrefKey(kv.Key, s));
            PlayerPrefs.SetInt(VersionKey, Version);
            PlayerPrefs.Save();
        }
        binds = new Dictionary<GameAction, Bind[]>();
        foreach (var kv in Defaults)
        {
            var arr = new Bind[2];
            for (int s = 0; s < 2; s++)
            {
                string saved = PlayerPrefs.GetString(PrefKey(kv.Key, s), "");
                arr[s] = string.IsNullOrEmpty(saved) ? kv.Value[s] : Parse(saved, kv.Value[s]);
            }
            binds[kv.Key] = arr;
        }
    }

    public static Bind Get(GameAction a, int slot) { EnsureLoaded(); return binds[a][slot]; }

    public static void Set(GameAction a, int slot, Bind b)
    {
        EnsureLoaded();
        binds[a][slot] = b;
        PlayerPrefs.SetString(PrefKey(a, slot), Serialize(b));
        PlayerPrefs.Save();
    }

    public static void ResetDefaults()
    {
        EnsureLoaded();
        foreach (var kv in Defaults)
            for (int s = 0; s < 2; s++)
            {
                binds[kv.Key][s] = kv.Value[s];
                PlayerPrefs.DeleteKey(PrefKey(kv.Key, s));
            }
        PlayerPrefs.Save();
    }

    /// Pressed this frame on either slot
    public static bool Down(GameAction a) { EnsureLoaded(); var b = binds[a]; return Pressed(b[0], true) || Pressed(b[1], true); }
    /// Held on either slot
    public static bool Held(GameAction a) { EnsureLoaded(); var b = binds[a]; return Pressed(b[0], false) || Pressed(b[1], false); }
    /// Released this frame on either slot
    public static bool Up(GameAction a) { EnsureLoaded(); var b = binds[a]; return Released(b[0]) || Released(b[1]); }

    static bool Pressed(Bind b, bool edge)
    {
        if (b.IsNone) return false;
        if (b.mouse >= 0)
        {
            var m = Mouse.current;
            if (m == null) return false;
            var btn = MouseButton(m, b.mouse);
            if (btn == null) return false;
            return edge ? btn.wasPressedThisFrame : btn.isPressed;
        }
        var kb = Keyboard.current;
        if (kb == null) return false;
        var kc = kb[b.key];
        if (kc == null) return false;
        return edge ? kc.wasPressedThisFrame : kc.isPressed;
    }

    static bool Released(Bind b)
    {
        if (b.IsNone) return false;
        if (b.mouse >= 0)
        {
            var m = Mouse.current;
            if (m == null) return false;
            var btn = MouseButton(m, b.mouse);
            return btn != null && btn.wasReleasedThisFrame;
        }
        var kb = Keyboard.current;
        if (kb == null) return false;
        var kc = kb[b.key];
        return kc != null && kc.wasReleasedThisFrame;
    }

    static UnityEngine.InputSystem.Controls.ButtonControl MouseButton(Mouse m, int index) => index switch
    {
        0 => m.leftButton,
        1 => m.rightButton,
        2 => m.middleButton,
        3 => m.backButton,
        4 => m.forwardButton,
        _ => null,
    };

    // Manual edge detection for mouse capture (independent of frame timing)
    static readonly bool[] prevMouse = new bool[5];

    /// Snapshot mouse state at the moment capture arms, so a button held
    /// through arming is not read as a fresh press.
    public static void PrimeCapture()
    {
        var m = Mouse.current;
        for (int i = 0; i < 5; i++)
        {
            var btn = m != null ? MouseButton(m, i) : null;
            prevMouse[i] = btn != null && btn.isPressed;
        }
    }

    /// For the rebind UI: whatever was pressed this frame. Esc = cancel.
    public static bool TryCapture(out Bind bind, out bool cancel)
    {
        bind = Bind.None;
        cancel = false;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.escapeKey.wasPressedThisFrame) { cancel = true; return false; }
            foreach (var kc in kb.allKeys)
            {
                if (kc.wasPressedThisFrame && kc.keyCode != Key.None && kc.keyCode != Key.Escape)
                {
                    bind = Bind.K(kc.keyCode);
                    return true;
                }
            }
        }
        var m = Mouse.current;
        if (m != null)
        {
            int hit = -1;
            for (int i = 0; i < 5; i++)
            {
                var btn = MouseButton(m, i);
                bool now = btn != null && btn.isPressed;
                if (now && !prevMouse[i] && hit < 0) hit = i;
                prevMouse[i] = now;
            }
            if (hit >= 0) { bind = Bind.M(hit); return true; }
        }
        return false;
    }

    public static string Label(GameAction a) => Label(Get(a, 0));

    /// "RMB/F" style — both slots
    public static string LabelAll(GameAction a)
    {
        Bind p = Get(a, 0), s = Get(a, 1);
        if (s.IsNone) return Label(p);
        if (p.IsNone) return Label(s);
        return Label(p) + "/" + Label(s);
    }

    static readonly string[] keyLabel = new string[512];

    public static string Label(Bind b)
    {
        if (b.IsNone) return "—";
        if (b.mouse >= 0) return b.mouse switch { 0 => "LMB", 1 => "RMB", 2 => "MMB", 3 => "M4", _ => "M5" };
        switch (b.key)
        {
            case Key.Space: return "SPACE";
            case Key.LeftShift: return "L-SHIFT";
            case Key.RightShift: return "R-SHIFT";
            case Key.LeftCtrl: return "L-CTRL";
            case Key.RightCtrl: return "R-CTRL";
            case Key.LeftAlt: return "L-ALT";
            case Key.RightAlt: return "R-ALT";
            case Key.UpArrow: return "UP";
            case Key.DownArrow: return "DOWN";
            case Key.LeftArrow: return "LEFT";
            case Key.RightArrow: return "RIGHT";
            case Key.Enter: return "ENTER";
            case Key.Tab: return "TAB";
            case Key.Backspace: return "BKSP";
        }
        string n = b.key.ToString();
        int k = (int)b.key;
        if (k >= 0 && k < keyLabel.Length && keyLabel[k] != null) return keyLabel[k];
        string made = n.StartsWith("Digit") ? n.Substring(5) : n.ToUpperInvariant();
        if (k >= 0 && k < keyLabel.Length) keyLabel[k] = made;
        return made;
    }

    public static string DisplayName(GameAction a) => a switch
    {
        GameAction.Forward => "MOVE FORWARD",
        GameAction.Back => "MOVE BACK",
        GameAction.Left => "MOVE LEFT",
        GameAction.Right => "MOVE RIGHT",
        GameAction.Jump => "JUMP / WALL KICK",
        GameAction.Crouch => "CROUCH / SLIDE",
        GameAction.Dash => "DASH",
        GameAction.Slam => "SLAM",
        GameAction.Grapple => "WHIP",
        GameAction.Parry => "PARRY",
        GameAction.Strike => "JAB",
        GameAction.Kick => "KICK",
        GameAction.Respawn => "RESPAWN",
        GameAction.Trick => "TRICK",
        _ => a.ToString().ToUpperInvariant(),
    };
}
