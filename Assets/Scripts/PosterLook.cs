using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// THE POSTER LOOK: the palettes the thirteen posters draw from (deco-direction's three — sepia, violet night, steel
/// blue — with cobalt for the House, umber for the foundations, and the dawn / black / brick variants), the poster's
/// own grade (a VolumeProfile built in code: neutral tonemapping, contrast up, a strong vignette, a touch of bloom
/// for the lamps and beams; no halftone, no film grain of the pipeline's: the grain is a quad of our own), the
/// shared generated textures (the soft disc, the beam falloff, the fog band, the grain, the vignette: made once,
/// released with the menu) and the finishing layer every poster ends with. The sky gradients are per poster.
/// </summary>
public static class PosterLook
{
    public struct Palette
    {
        public string name;
        public Color skyTop, skyBottom, haze, ink, shade, mid, light, window, lamp, beam, moon, accent;
    }

    static Color C(float r, float g, float b) => new Color(r, g, b, 1f);

    /// The danger red: a saturated glowing light on signal lamps, never a surface
    public static readonly Color SignalRed = C(1f, 0.1f, 0.06f);
    /// The company's brass
    public static readonly Color Brass = C(0.78f, 0.58f, 0.25f);

    /// The city by day and the menu: umber, olive, ochre; a mauve overcast over an ochre horizon; pale windows
    public static readonly Palette Sepia = new Palette
    {
        name = "Sepia", skyTop = C(0.22f, 0.17f, 0.23f), skyBottom = C(0.72f, 0.47f, 0.18f), haze = C(0.52f, 0.38f, 0.22f),
        ink = C(0.07f, 0.055f, 0.04f), shade = C(0.19f, 0.145f, 0.09f), mid = C(0.34f, 0.28f, 0.17f), light = C(0.64f, 0.53f, 0.3f),
        window = C(1f, 0.88f, 0.58f), lamp = C(1f, 0.9f, 0.7f), beam = C(0.95f, 0.9f, 0.75f), moon = C(0.95f, 0.9f, 0.8f), accent = C(0.32f, 0.08f, 0.08f),
    };

    /// The terrace painting: a black sky, a pale silver tower, a gold one; the windows yellow
    public static readonly Palette SepiaGold = new Palette
    {
        name = "SepiaGold", skyTop = C(0.03f, 0.025f, 0.035f), skyBottom = C(0.3f, 0.21f, 0.11f), haze = C(0.34f, 0.27f, 0.17f),
        ink = C(0.04f, 0.03f, 0.03f), shade = C(0.17f, 0.14f, 0.1f), mid = C(0.42f, 0.38f, 0.29f), light = C(0.8f, 0.73f, 0.56f),
        window = C(1f, 0.85f, 0.48f), lamp = C(1f, 0.9f, 0.68f), beam = C(0.95f, 0.92f, 0.8f), moon = C(0.95f, 0.92f, 0.8f), accent = C(0.78f, 0.58f, 0.25f),
    };

    /// The night city and the rooftops: a violet sky and fog, black masses, orange windows, a full moon
    public static readonly Palette Violet = new Palette
    {
        name = "Violet", skyTop = C(0.06f, 0.03f, 0.15f), skyBottom = C(0.4f, 0.14f, 0.42f), haze = C(0.33f, 0.18f, 0.42f),
        ink = C(0.03f, 0.02f, 0.05f), shade = C(0.12f, 0.07f, 0.18f), mid = C(0.22f, 0.12f, 0.3f), light = C(0.44f, 0.26f, 0.54f),
        window = C(1f, 0.55f, 0.15f), lamp = C(1f, 0.8f, 0.5f), beam = C(0.9f, 0.86f, 1f), moon = C(0.96f, 0.93f, 0.82f), accent = C(1f, 0.62f, 0.2f),
    };

    /// The tunnels and the terminus: violet gone to black, amber for the rail and the lamps
    public static readonly Palette VioletBlack = new Palette
    {
        name = "VioletBlack", skyTop = C(0.02f, 0.015f, 0.05f), skyBottom = C(0.22f, 0.09f, 0.26f), haze = C(0.2f, 0.11f, 0.26f),
        ink = C(0.02f, 0.015f, 0.04f), shade = C(0.09f, 0.055f, 0.14f), mid = C(0.17f, 0.1f, 0.24f), light = C(0.36f, 0.22f, 0.44f),
        window = C(1f, 0.68f, 0.25f), lamp = C(1f, 0.85f, 0.6f), beam = C(1f, 0.92f, 0.7f), moon = C(0.96f, 0.93f, 0.82f), accent = C(1f, 0.62f, 0.2f),
    };

    /// The high city: teal shadows, pale blue highlights, a star field
    public static readonly Palette Steel = new Palette
    {
        name = "Steel", skyTop = C(0.03f, 0.05f, 0.1f), skyBottom = C(0.2f, 0.32f, 0.44f), haze = C(0.22f, 0.34f, 0.46f),
        ink = C(0.03f, 0.04f, 0.06f), shade = C(0.08f, 0.13f, 0.2f), mid = C(0.16f, 0.26f, 0.36f), light = C(0.46f, 0.6f, 0.72f),
        window = C(0.95f, 0.86f, 0.62f), lamp = C(1f, 0.92f, 0.7f), beam = C(0.85f, 0.95f, 1f), moon = C(0.9f, 0.94f, 1f), accent = C(0.32f, 0.08f, 0.08f),
    };

    /// The metro: steel blue with the cream glazed tile and an oxblood band
    public static readonly Palette SteelCream = new Palette
    {
        name = "SteelCream", skyTop = C(0.03f, 0.05f, 0.09f), skyBottom = C(0.16f, 0.26f, 0.36f), haze = C(0.2f, 0.3f, 0.4f),
        ink = C(0.03f, 0.04f, 0.06f), shade = C(0.08f, 0.13f, 0.2f), mid = C(0.15f, 0.24f, 0.33f), light = C(0.86f, 0.8f, 0.64f),
        window = C(1f, 0.88f, 0.6f), lamp = C(1f, 0.92f, 0.7f), beam = C(0.95f, 0.95f, 0.9f), moon = C(0.9f, 0.94f, 1f), accent = C(0.32f, 0.08f, 0.08f),
    };

    /// The sewers: steel blue over brick umber and black water
    public static readonly Palette SteelBrick = new Palette
    {
        name = "SteelBrick", skyTop = C(0.02f, 0.04f, 0.07f), skyBottom = C(0.14f, 0.24f, 0.32f), haze = C(0.16f, 0.26f, 0.34f),
        ink = C(0.02f, 0.025f, 0.04f), shade = C(0.16f, 0.1f, 0.07f), mid = C(0.3f, 0.19f, 0.13f), light = C(0.5f, 0.36f, 0.26f),
        window = C(1f, 0.82f, 0.5f), lamp = C(1f, 0.88f, 0.62f), beam = C(0.85f, 0.95f, 1f), moon = C(0.9f, 0.94f, 1f), accent = C(0.46f, 0.58f, 0.66f),
    };

    /// The House: cobalt, yellow chandelier bursts, a chequer floor
    public static readonly Palette Cobalt = new Palette
    {
        name = "Cobalt", skyTop = C(0.015f, 0.015f, 0.05f), skyBottom = C(0.09f, 0.14f, 0.48f), haze = C(0.12f, 0.18f, 0.5f),
        ink = C(0.02f, 0.02f, 0.05f), shade = C(0.06f, 0.08f, 0.28f), mid = C(0.12f, 0.18f, 0.55f), light = C(0.3f, 0.4f, 0.8f),
        window = C(1f, 0.85f, 0.35f), lamp = C(1f, 0.9f, 0.5f), beam = C(1f, 0.95f, 0.7f), moon = C(0.95f, 0.92f, 0.8f), accent = C(0.78f, 0.58f, 0.25f),
    };

    /// The foundations: umber on black, a lantern's warmth
    public static readonly Palette Umber = new Palette
    {
        name = "Umber", skyTop = C(0.04f, 0.025f, 0.015f), skyBottom = C(0.3f, 0.17f, 0.08f), haze = C(0.3f, 0.2f, 0.12f),
        ink = C(0.045f, 0.03f, 0.02f), shade = C(0.16f, 0.1f, 0.06f), mid = C(0.32f, 0.21f, 0.12f), light = C(0.56f, 0.4f, 0.24f),
        window = C(1f, 0.75f, 0.4f), lamp = C(1f, 0.85f, 0.55f), beam = C(0.95f, 0.8f, 0.6f), moon = C(0.95f, 0.9f, 0.8f), accent = C(0.6f, 0.45f, 0.25f),
    };

    /// The finale: black with steel, the House's crown throwing light
    public static readonly Palette BlackSteel = new Palette
    {
        name = "BlackSteel", skyTop = C(0.01f, 0.012f, 0.025f), skyBottom = C(0.1f, 0.16f, 0.26f), haze = C(0.14f, 0.22f, 0.32f),
        ink = C(0.02f, 0.025f, 0.04f), shade = C(0.07f, 0.1f, 0.16f), mid = C(0.14f, 0.2f, 0.3f), light = C(0.42f, 0.54f, 0.66f),
        window = C(0.95f, 0.88f, 0.66f), lamp = C(1f, 0.92f, 0.7f), beam = C(0.9f, 0.96f, 1f), moon = C(0.9f, 0.94f, 1f), accent = C(0.78f, 0.58f, 0.25f),
    };

    // ------------------------------------------------------------ the grade

    static VolumeProfile profile;

    /// The poster's own grade, only the thumbnail camera's: contrast up, a strong vignette, a touch of bloom
    public static VolumeProfile Profile()
    {
        if (profile != null) return profile;
        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "PosterLook";
        profile.hideFlags = HideFlags.HideAndDontSave;
        var tone = profile.Add<Tonemapping>(true);
        tone.mode.value = TonemappingMode.Neutral;
        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.value = 0.5f;
        bloom.threshold.value = 1.05f;
        bloom.scatter.value = 0.65f;
        bloom.highQualityFiltering.value = true;
        bloom.clamp.value = 6f;
        var grade = profile.Add<ColorAdjustments>(true);
        grade.postExposure.value = 0f;
        grade.contrast.value = 32f;
        grade.saturation.value = 6f;
        var vig = profile.Add<Vignette>(true);
        vig.intensity.value = 0.45f;
        vig.smoothness.value = 0.45f;
        vig.rounded.value = false;
        vig.color.value = Color.black;
        return profile;
    }

    // ------------------------------------------------------------ the shared textures

    static Texture2D grain, vignette, beam, halo, band;

    /// 256 × 256 grey noise, tiled over the frame at 8 % by Finish
    public static Texture2D GrainTex => grain != null ? grain : grain = Noise(256, 7919);
    /// A radial darkening, clear in the middle, full at the corners
    public static Texture2D VignetteTex => vignette != null ? vignette : vignette = Radial(128, 0.4f, 1.05f, false);
    /// A searchlight's falloff: soft across (u), fading along (v)
    public static Texture2D BeamTex => beam != null ? beam : beam = BeamFalloff(64);
    /// A soft disc
    public static Texture2D HaloTex => halo != null ? halo : halo = Radial(64, 0f, 1f, true);
    /// A soft horizontal band (1 × 64: clear at the edges, full in the middle)
    public static Texture2D BandTex => band != null ? band : band = Band(64);

    /// The shared textures, gone with the menu
    public static void ReleaseShared()
    {
        foreach (var t in new[] { grain, vignette, beam, halo, band }) if (t != null) Object.DestroyImmediate(t);
        grain = vignette = beam = halo = band = null;
    }

    static Texture2D Make(int w, int h, string name, TextureWrapMode wrap = TextureWrapMode.Clamp)
        => new Texture2D(w, h, TextureFormat.RGBA32, false, true) { name = name, hideFlags = HideFlags.HideAndDontSave, wrapMode = wrap, filterMode = FilterMode.Bilinear };

    /// A sky: 1 × 64, the bottom colour at v = 0 rising to the top by v^power (power < 1 keeps the glow to the horizon)
    public static Texture2D Gradient(Color top, Color bottom, float power)
    {
        var t = Make(1, 64, "PosterSky");
        var px = new Color[64];
        for (int y = 0; y < 64; y++) px[y] = Color.Lerp(bottom, top, Mathf.Pow(y / 63f, power));
        t.SetPixels(px);
        t.Apply(false, true);
        return t;
    }

    static Texture2D Noise(int n, int seed)
    {
        var t = Make(n, n, "PosterGrain", TextureWrapMode.Repeat);
        var r = new System.Random(seed);
        var px = new Color[n * n];
        for (int i = 0; i < px.Length; i++)
        {
            float g = 0.5f + ((float)r.NextDouble() - 0.5f) * 0.9f;
            px[i] = new Color(g, g, g, 1f);
        }
        t.SetPixels(px);
        t.Apply(false, true);
        return t;
    }

    /// A radial alpha: `inner` to `outer` of the half-diagonal (a vignette rises outward; a halo falls inward)
    static Texture2D Radial(int n, float inner, float outer, bool falling)
    {
        var t = Make(n, n, falling ? "PosterHalo" : "PosterVignette");
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / (falling ? 1f : 1.4142f);
                float k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(inner, outer, d));
                float a = falling ? Mathf.Pow(1f - k, 1.6f) : k;
                px[y * n + x] = new Color(1f, 1f, 1f, a);
            }
        t.SetPixels(px);
        t.Apply(false, true);
        return t;
    }

    static Texture2D BeamFalloff(int n)
    {
        var t = Make(n, n, "PosterBeam");
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n, v = (y + 0.5f) / n;
                float across = Mathf.Pow(1f - Mathf.Abs(u * 2f - 1f), 1.6f), along = Mathf.Pow(1f - v, 0.8f);
                px[y * n + x] = new Color(1f, 1f, 1f, across * along);
            }
        t.SetPixels(px);
        t.Apply(false, true);
        return t;
    }

    static Texture2D Band(int n)
    {
        var t = Make(1, n, "PosterBand");
        var px = new Color[n];
        for (int y = 0; y < n; y++) px[y] = new Color(1f, 1f, 1f, Mathf.Sin((y + 0.5f) / n * Mathf.PI));
        t.SetPixels(px);
        t.Apply(false, true);
        return t;
    }

    // ------------------------------------------------------------ the last layer

    /// Grain and vignette over everything: the tinted vignette in the palette's ink, the noise tiled at 8 %
    public static void Finish(PosterKit p)
    {
        p.Tex(-PosterKit.HalfW - 0.5f, -PosterKit.HalfH - 0.5f, PosterKit.HalfW + 0.5f, PosterKit.HalfH + 0.5f, VignetteTex, PosterKit.With(p.pal.ink, 0.5f), 0.35f, PosterKit.Blend.Alpha);
        p.Tex(-PosterKit.HalfW - 0.5f, -PosterKit.HalfH - 0.5f, PosterKit.HalfW + 0.5f, PosterKit.HalfH + 0.5f, GrainTex, new Color(1f, 1f, 1f, 0.08f), 0.3f, PosterKit.Blend.Alpha, new Vector4(3.75f, 2.11f, 0f, 0f));
    }
}
