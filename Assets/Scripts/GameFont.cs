using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The game's type and the company's mark, in one place.
///  · UI: Barlow Condensed Bold Italic for titles and slash items (the Persona
///    voice), Barlow Medium for readouts — from Resources/Fonts, falling back to
///    Unity's built-in font if a file is missing.
///  · Brand: Michroma, the HALCYON wordmark face — wide, corporate, calm.
///  · World text draws with Momentum/WorldText: depth-tested (no labels
///    through walls), one shared material per font that follows atlas rebuilds,
///    and a distance fade so far-off lettering never sparkles.
///  · The logo: a sun on the horizon — a gold ring with a bar through its lower
///    third — as a procedural texture for menus (see LevelKit.Emblem for signs).
/// Fonts are SIL Open Font License (Google Fonts); OFL.txt sits beside each.
/// </summary>
public static class GameFont
{
    static Font title, body, brand, fallback;

    /// Titles, slash items, stamps: condensed, bold, italic
    public static Font Title => title != null ? title : Load(ref title, "Fonts/BarlowCondensed-BoldItalic");
    /// Readouts, hints, details
    public static Font Body => body != null ? body : Load(ref body, "Fonts/Barlow-Medium");
    /// The company wordmark
    public static Font Brand => brand != null ? brand : Load(ref brand, "Fonts/Michroma-Regular");

    /// The title face is already bold italic; only the fallback needs synthesising
    public static FontStyle TitleStyle => Title == fallback ? FontStyle.BoldAndItalic : FontStyle.Normal;

    static Font Load(ref Font slot, string path)
    {
        slot = Resources.Load<Font>(path);
        if (slot == null)
        {
            if (fallback == null) fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            slot = fallback;
        }
        return slot;
    }

    /// Points the IMGUI skin at the body face so every style made from it inherits
    /// the font; call at the top of OnGUI before styles are built
    public static void Apply()
    {
        var skin = GUI.skin;
        if (skin.label.font != Body)
        {
            skin.font = Body;
            skin.label.font = Body;
        }
    }

    // ------------------------------------------------------------ world text

    static readonly Dictionary<(Font font, float fade), Material> worldMats = new Dictionary<(Font, float), Material>();
    static bool hooked;

    /// The shared depth-tested material for a font. `fadeStart`/`fadeEnd` are the
    /// metres at which its lettering begins to fade and is gone.
    public static Material WorldMaterial(Font font, float fadeStart = 60f, float fadeEnd = 140f)
    {
        var key = (font, fadeStart);
        if (worldMats.TryGetValue(key, out var m) && m != null) return m;
        var shader = Shader.Find("Momentum/WorldText");
        m = new Material(shader != null ? shader : font.material.shader) { name = "WorldText_" + font.name };
        m.mainTexture = font.material.mainTexture;
        if (shader != null)
        {
            m.SetFloat("_FadeStart", fadeStart);
            m.SetFloat("_FadeEnd", fadeEnd);
        }
        worldMats[key] = m;
        if (!hooked)
        {
            hooked = true;
            Font.textureRebuilt += OnRebuilt; // a dynamic font grows its atlas as new glyphs appear
        }
        return m;
    }

    static void OnRebuilt(Font f)
    {
        foreach (var kv in worldMats)
            if (kv.Key.font == f && kv.Value != null) kv.Value.mainTexture = f.material.mainTexture;
    }

    /// Rough advance per glyph as a fraction of the glyph height, for fitting text to a width
    public static float Advance(Font font) => font == Brand ? 0.92f : font == Title ? 0.5f : 0.6f;

    // ------------------------------------------------------------ the logo

    static Texture2D logo;

    /// The sun on the horizon, white on transparent — tint it with GUI.color
    public static Texture2D Logo => logo != null ? logo : logo = MakeLogo(128);

    static Texture2D MakeLogo(int n)
    {
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false)
        {
            name = "HalcyonLogo",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        float c = n * 0.5f, rO = n * 0.42f, rI = n * 0.28f;
        float barY0 = n * 0.30f, barY1 = n * 0.38f, barX0 = n * 0.06f, barX1 = n * 0.94f;
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float fx = x + 0.5f, fy = y + 0.5f;
                float d = Mathf.Sqrt((fx - c) * (fx - c) + (fy - c) * (fy - c));
                float ring = Mathf.Clamp01(rO - d + 0.5f) * Mathf.Clamp01(d - rI + 0.5f);
                float bar = Mathf.Clamp01(fy - barY0 + 0.5f) * Mathf.Clamp01(barY1 - fy + 0.5f)
                            * Mathf.Clamp01(fx - barX0 + 0.5f) * Mathf.Clamp01(barX1 - fx + 0.5f);
                px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Max(ring, bar));
            }
        }
        t.SetPixels(px);
        t.Apply();
        return t;
    }
}
