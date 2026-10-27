using UnityEngine;

/// <summary>
/// Resolution-independent IMGUI. Every HUD and menu lays out on a VIRTUAL canvas
/// ReferenceHeight tall (the width follows the window's aspect); Begin() scales
/// GUI.matrix so that canvas fills the window whatever its size — a full-screen
/// 4K panel and a small editor Game view get the same composition. Text goes
/// through Label(), which rasterises the font at the scaled size instead of
/// stretching a small bitmap, so type stays crisp at any scale.
/// </summary>
public static class UIScale
{
    /// The canvas height the HUD and menus are authored against
    public const float ReferenceHeight = 900f;
    /// Tuning multiplier on top of the automatic window scale (MovementConfig.uiScale)
    public static float Multiplier = 1f;
    /// Window pixels per canvas unit, as of the last Begin()
    public static float Factor { get; private set; } = 1f;
    /// Canvas size: use these instead of Screen.width / Screen.height inside OnGUI
    public static float W => Screen.width / Factor;
    public static float H => Screen.height / Factor;

    public static void Begin()
    {
        Factor = Mathf.Max(0.5f, Screen.height / ReferenceHeight * Mathf.Max(0.25f, Multiplier));
        GUI.matrix = Matrix4x4.Scale(new Vector3(Factor, Factor, 1f));
    }

    public static void End() => GUI.matrix = Matrix4x4.identity;

    /// Rotate the canvas about a pivot given in CANVAS units. GUIUtility's version
    /// takes its pivot in window pixels, which lands in the wrong place once the
    /// canvas is scaled — these compose in canvas space instead.
    public static void RotateAround(float angle, Vector2 pivot) =>
        GUI.matrix = GUI.matrix * Matrix4x4.TRS(pivot, Quaternion.Euler(0f, 0f, angle), Vector3.one)
                     * Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);

    public static void ScaleAround(Vector2 scale, Vector2 pivot) =>
        GUI.matrix = GUI.matrix * Matrix4x4.TRS(pivot, Quaternion.identity, new Vector3(scale.x, scale.y, 1f))
                     * Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);

    /// Lean the canvas: x' = x + tan(deg) × (y − pivot.y). Baselines stay horizontal, so text drawn under it stays
    /// readable — the HUD's slabs lean at the band's angle without tilting their words.
    public static void SkewAround(float deg, Vector2 pivot)
    {
        float k = Mathf.Tan(deg * Mathf.Deg2Rad);
        Matrix4x4 s = Matrix4x4.identity;
        s.m01 = k;
        s.m03 = -k * pivot.y;
        GUI.matrix = GUI.matrix * s;
    }

    static readonly GUIContent scratch = new GUIContent();

    /// The size `text` takes in `style` on the canvas, measured at the size Label rasterises (fontSize × Factor,
    /// rounded) and scaled back. A box must be tested against THIS, not a base-size measurement: hinting at the
    /// rasterised size is what makes a label a few px wider than base × Factor and wraps its last word. Not for
    /// every frame — measure when the text or Factor changes and keep the answer (MenuStyle.FitWidth does).
    public static Vector2 Measure(GUIStyle style, string text)
    {
        scratch.text = text;
        float s = Factor;
        int baseSize = style.fontSize;
        if (Mathf.Approximately(s, 1f) || baseSize <= 0) return style.CalcSize(scratch);
        style.fontSize = Mathf.RoundToInt(baseSize * s);
        Vector2 size = style.CalcSize(scratch) / s;
        style.fontSize = baseSize;
        return size;
    }

    public static void Label(Rect r, string text) => Label(r, text, GUI.skin.label);

    /// Crisp text: the font is rasterised at fontSize × Factor and drawn in window
    /// pixels, with the canvas scale divided back out of the matrix (any rotation
    /// or punch already in the matrix is kept)
    public static void Label(Rect r, string text, GUIStyle style)
    {
        float s = Factor;
        int baseSize = style.fontSize;
        if (Mathf.Approximately(s, 1f) || baseSize <= 0)
        {
            GUI.Label(r, text, style);
            return;
        }
        Matrix4x4 saved = GUI.matrix;
        GUI.matrix = saved * Matrix4x4.Scale(new Vector3(1f / s, 1f / s, 1f));
        style.fontSize = Mathf.RoundToInt(baseSize * s);

        // Hinting makes a font rasterised at size × s a touch wider and taller than
        // the measured box × s, and IMGUI would wrap the last word or clip the
        // ascenders. Pad the box on the side the alignment leaves free (so the
        // anchored edge stays put) and let glyphs overflow rather than lose edges.
        float padX = r.width * s * 0.04f + 4f, padY = r.height * s * 0.08f + 4f;
        Rect rr = new Rect(r.x * s, r.y * s, r.width * s + padX, r.height * s + padY);
        TextAnchor a = style.alignment;
        bool right = a == TextAnchor.UpperRight || a == TextAnchor.MiddleRight || a == TextAnchor.LowerRight;
        bool centre = a == TextAnchor.UpperCenter || a == TextAnchor.MiddleCenter || a == TextAnchor.LowerCenter;
        bool lower = a == TextAnchor.LowerLeft || a == TextAnchor.LowerCenter || a == TextAnchor.LowerRight;
        bool middle = a == TextAnchor.MiddleLeft || a == TextAnchor.MiddleCenter || a == TextAnchor.MiddleRight;
        if (right) rr.x -= padX; else if (centre) rr.x -= padX * 0.5f;
        if (lower) rr.y -= padY; else if (middle) rr.y -= padY * 0.5f;

        TextClipping clip = style.clipping;
        style.clipping = TextClipping.Overflow;
        GUI.Label(rr, text, style);
        style.clipping = clip;
        style.fontSize = baseSize;
        GUI.matrix = saved;
    }
}
