using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// THE BORROWED UI (2026-10-27; the user: "for the ui, lets use assets too, I want to use as many borrowable assets
/// as possible"). Kenney's CC0 packs live under Assets/Resources/UI (Incoming/LICENSES.md): the ornate frames and
/// dividers (fantasy-ui-borders), the slider handle, the track and the stars (ui-pack), the white glyphs
/// (game-icons), the key and mouse glyphs (input-prompts, filled and outline), the clicks (ui-audio). Everything is
/// white line art tinted in code with the house palette, loaded once and cached, and every call is null-safe: a
/// missing file draws nothing and says so by its return, so the caller keeps its drawn version and nothing on screen
/// breaks on a folder that has not been imported yet.
/// </summary>
public static class UIAssets
{
    static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
    static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

    /// A texture under Resources/UI by its path ("Border/border-005"); null when it is not there, asked once
    public static Texture2D Tex(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (textures.TryGetValue(path, out var t)) return t;
        t = Resources.Load<Texture2D>("UI/" + path);
        textures[path] = t;
        return t;
    }

    public static Texture2D Border(string name) => Tex("Border/" + name);   // border-000…031, divider-000…005, divider-fade-000…005
    public static Texture2D Pack(string name) => Tex("Pack/" + name);       // slide_hangle, slide_horizontal_color, star, button_rectangle_line…
    public static Texture2D Icon(string name) => Tex("Icons/" + name);      // star, checkmark, cross, medal1, trophy, target, gear, warning…
    public static Texture2D Prompt(string name) => Tex("Prompts/" + name);  // keyboard_g, mouse_left, keyboard_space_outline…

    /// A click under Resources/UI/Audio (click1…5, rollover1…6, switch1…5, mouseclick1, mouserelease1); null when it is not there
    public static AudioClip Clip(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (clips.TryGetValue(name, out var c)) return c;
        c = Resources.Load<AudioClip>("UI/Audio/" + name);
        clips[name] = c;
        return c;
    }

    /// The texture drawn into `r` (scaled to fit, centred) in `tint`; false when there is nothing to draw
    public static bool Draw(Rect r, Texture2D tex, Color tint)
    {
        if (tex == null) return false;
        GUI.color = tint;
        GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
        GUI.color = Color.white;
        return true;
    }

    /// A 9-sliced frame: the texture's corners (`texCorner` px of it — a third of the packs' 48 px tiles) drawn
    /// `corner` canvas units square, the edges stretched between them, the middle left open unless `centre`: the
    /// frames are lines round a clear middle and the slab under them is the game's own. Draws under whatever
    /// GUI.matrix holds (the HUD's leaning slabs lean their frames with them). False when the texture is missing,
    /// so the caller draws its own frame instead
    public static bool Frame(Rect r, Texture2D tex, float corner, Color tint, float texCorner = 16f, bool centre = false)
    {
        if (tex == null) return false;
        float cu = Mathf.Clamp01(texCorner / tex.width), cv = Mathf.Clamp01(texCorner / tex.height);
        float c = Mathf.Min(corner, r.width * 0.5f, r.height * 0.5f);
        float mw = r.width - 2f * c, mh = r.height - 2f * c;
        GUI.color = tint;
        // the texture's v runs up from its bottom row, so the top pieces sample from 1 - cv
        Piece(new Rect(r.x, r.y, c, c), tex, 0f, 1f - cv, cu, cv);
        Piece(new Rect(r.x + c, r.y, mw, c), tex, cu, 1f - cv, 1f - 2f * cu, cv);
        Piece(new Rect(r.xMax - c, r.y, c, c), tex, 1f - cu, 1f - cv, cu, cv);
        Piece(new Rect(r.x, r.y + c, c, mh), tex, 0f, cv, cu, 1f - 2f * cv);
        if (centre) Piece(new Rect(r.x + c, r.y + c, mw, mh), tex, cu, cv, 1f - 2f * cu, 1f - 2f * cv);
        Piece(new Rect(r.xMax - c, r.y + c, c, mh), tex, 1f - cu, cv, cu, 1f - 2f * cv);
        Piece(new Rect(r.x, r.yMax - c, c, c), tex, 0f, 0f, cu, cv);
        Piece(new Rect(r.x + c, r.yMax - c, mw, c), tex, cu, 0f, 1f - 2f * cu, cv);
        Piece(new Rect(r.xMax - c, r.yMax - c, c, c), tex, 1f - cu, 0f, cu, cv);
        GUI.color = Color.white;
        return true;
    }

    static void Piece(Rect r, Texture2D tex, float u, float v, float uw, float vh)
    {
        if (r.width <= 0f || r.height <= 0f) return;
        GUI.DrawTextureWithTexCoords(r, tex, new Rect(u, v, uw, vh), true);
    }

    /// A horizontal rule from one of the dividers: its end caps (`texCap` px of the texture) at their own
    /// proportion, the run between stretched; the rule stands `r.height` tall. False when the texture is missing
    public static bool Rule(Rect r, Texture2D tex, Color tint, float texCap = 22f)
    {
        if (tex == null) return false;
        float cu = Mathf.Clamp01(texCap / tex.width);
        float cap = Mathf.Min(r.height * texCap / Mathf.Max(1f, tex.height), r.width * 0.5f);
        GUI.color = tint;
        Piece(new Rect(r.x, r.y, cap, r.height), tex, 0f, 0f, cu, 1f);
        Piece(new Rect(r.x + cap, r.y, r.width - 2f * cap, r.height), tex, cu, 0f, 1f - 2f * cu, 1f);
        Piece(new Rect(r.xMax - cap, r.y, cap, r.height), tex, 1f - cu, 0f, cu, 1f);
        GUI.color = Color.white;
        return true;
    }

    // ---------------------------------------------------------------- the key glyphs

    /// The glyph for a binding — the mouse with its button lit, the key with its cap — outline (line art for the
    /// HUD's dark ground) or filled (the menus' sheets); null for a binding the pack has no picture of, so the caller
    /// falls back to Keybinds.Label
    public static Texture2D Glyph(Keybinds.Bind b, bool outline = true)
    {
        string name = GlyphName(b);
        if (name == null) return null;
        var t = outline ? Prompt(name + "_outline") : null;
        return t != null ? t : Prompt(name);
    }

    /// The glyph of an action's first binding
    public static Texture2D Glyph(GameAction a, bool outline = true) => Glyph(Keybinds.Get(a, 0), outline);

    /// The prompt pack's file for a binding (its naming: keyboard_a, keyboard_space, mouse_left…), null when it has none
    public static string GlyphName(Keybinds.Bind b)
    {
        if (b.IsNone) return null;
        if (b.mouse >= 0) return b.mouse switch { 0 => "mouse_left", 1 => "mouse_right", 2 => "mouse_scroll", 3 => "mouse_side_back", 4 => "mouse_side_forward", _ => "mouse_small" };
        Key k = b.key;
        if (k >= Key.A && k <= Key.Z) return "keyboard_" + (char)('a' + (k - Key.A));
        if (k >= Key.Digit1 && k <= Key.Digit9) return "keyboard_" + (char)('1' + (k - Key.Digit1));
        if (k >= Key.Numpad1 && k <= Key.Numpad9) return "keyboard_" + (char)('1' + (k - Key.Numpad1));
        if (k >= Key.F1 && k <= Key.F12) return "keyboard_f" + (1 + (k - Key.F1));
        switch (k)
        {
            case Key.Digit0: case Key.Numpad0: return "keyboard_0";
            case Key.Space: return "keyboard_space";
            case Key.Enter: return "keyboard_enter";
            case Key.NumpadEnter: return "keyboard_numpad_enter";
            case Key.Tab: return "keyboard_tab";
            case Key.Escape: return "keyboard_escape";
            case Key.Backspace: return "keyboard_backspace";
            case Key.LeftShift: case Key.RightShift: return "keyboard_shift";
            case Key.LeftCtrl: case Key.RightCtrl: return "keyboard_ctrl";
            case Key.LeftAlt: case Key.RightAlt: return "keyboard_alt";
            case Key.LeftMeta: case Key.RightMeta: return "keyboard_win";
            case Key.CapsLock: return "keyboard_capslock";
            case Key.NumLock: return "keyboard_numlock";
            case Key.ScrollLock: return "keyboard_scroll_lock";
            case Key.PrintScreen: return "keyboard_printscreen";
            case Key.Pause: return "keyboard_pause";
            case Key.Insert: return "keyboard_insert";
            case Key.Delete: return "keyboard_delete";
            case Key.Home: return "keyboard_home";
            case Key.End: return "keyboard_end";
            case Key.PageUp: return "keyboard_page_up";
            case Key.PageDown: return "keyboard_page_down";
            case Key.LeftArrow: return "keyboard_arrow_left";
            case Key.RightArrow: return "keyboard_arrow_right";
            case Key.UpArrow: return "keyboard_arrow_up";
            case Key.DownArrow: return "keyboard_arrow_down";
            case Key.Backquote: return "keyboard_tilde";
            case Key.Quote: return "keyboard_apostrophe";
            case Key.Semicolon: return "keyboard_semicolon";
            case Key.Comma: return "keyboard_comma";
            case Key.Period: case Key.NumpadPeriod: return "keyboard_period";
            case Key.Slash: case Key.NumpadDivide: return "keyboard_slash_forward";
            case Key.Backslash: return "keyboard_slash_back";
            case Key.LeftBracket: return "keyboard_bracket_open";
            case Key.RightBracket: return "keyboard_bracket_close";
            case Key.Minus: case Key.NumpadMinus: return "keyboard_minus";
            case Key.Equals: case Key.NumpadEquals: return "keyboard_equals";
            case Key.NumpadPlus: return "keyboard_numpad_plus";
            case Key.NumpadMultiply: return "keyboard_asterisk";
            default: return null;
        }
    }
}
