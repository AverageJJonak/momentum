using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// THE LOOK. Everything about a level's light that is not geometry: the sky
/// (painted, MenuSky.Paint, on the Momentum/Sky skybox; the procedural one when
/// that shader is missing), the sun (colour, angle, strength), the rim (a second
/// directional light, low and opposite the key, no shadows), the ambient
/// (three-colour, per zone, set so a floor in the ramp's shadow band still
/// reads: LevelKitLook.cs has the luminance maths; the black paper is the sky,
/// the far city and the fog's far end, never the ground), the
/// fog (LINEAR per zone, the first 25–30 m clear in every zone: the dense low
/// band the smog sheets take from its colour, and the distance haze the streets
/// vanish into), the post-processing
/// (a VolumeProfile built in code per zone), and the striped light cookies on
/// the floods. THREE PALETTES since 2026-10-16, after the user's references:
/// SEPIA (umber, olive, ochre on the one lit face, near-black shadow, a dark
/// mauve-grey overcast, pale yellow-white windows: the city by day, the menu),
/// VIOLET (a violet sky, a purple fog layer over the streets, black masses,
/// orange windows, white searchlights: the night city and the rooftops),
/// STEEL (steel blue, dark teal, near-black: the cold high city, the
/// underground's tile), and COBALT for the House's interiors (cobalt walls and
/// arches, chandelier yellow). The grade's split follows the palette. The cel
/// shading itself is the Momentum/Toon shader; WorldLook.Toon is its switch.
/// Levels that alternate zones room by room cross-fade (WorldLookBlend).
/// Neutral tonemapping on purpose: the colour language (cyan cuts, gold
/// parries, red danger, amber units) must keep its hues.
/// </summary>
public static class WorldLook
{
    // ---- THE TOON SWITCH: PlayerPrefs "look_toon", default on; read on first use (never in an Awake). LevelKit reads it
    // when it makes a material, so a change in Settings takes effect at the next level build.
    static int toonPref = -1;
    public static bool Toon
    {
        get
        {
            if (toonPref < 0) toonPref = PlayerPrefs.GetInt("look_toon", 1) != 0 ? 1 : 0;
            return toonPref == 1;
        }
    }
    public static void SetToon(bool on)
    {
        toonPref = on ? 1 : 0;
        PlayerPrefs.SetInt("look_toon", toonPref);
    }

    // ---- THE BRIGHTNESS KNOB (readability, 2026-10-16): PlayerPrefs "look_exposure", a multiplier 0.7–1.4 (default 1)
    // applied as post-exposure (log2 of it, in EV) in every grade this file builds, so the whole picture lifts or drops
    // in play without a rebuild: the Settings row "BRIGHTNESS". Every grade made by Profile() registers its colour
    // adjustments and the preset's own EV here, so SetExposure reaches the volumes that already exist.
    public const float ExposureMin = 0.7f, ExposureMax = 1.4f;
    static float exposurePref = -1f;
    static readonly List<(ColorAdjustments grade, float baseEv)> grades = new List<(ColorAdjustments, float)>();

    public static float Exposure
    {
        get
        {
            if (exposurePref < 0f) exposurePref = Mathf.Clamp(PlayerPrefs.GetFloat("look_exposure", 1f), ExposureMin, ExposureMax);
            return exposurePref;
        }
    }

    public static void SetExposure(float multiplier)
    {
        exposurePref = Mathf.Clamp(multiplier, ExposureMin, ExposureMax);
        PlayerPrefs.SetFloat("look_exposure", exposurePref);
        float ev = ExposureEv;
        for (int i = grades.Count - 1; i >= 0; i--)
        {
            if (grades[i].grade == null) { grades.RemoveAt(i); continue; }   // a level's volumes went with it
            grades[i].grade.postExposure.value = grades[i].baseEv + ev;
        }
    }

    /// The knob in stops: 1.4 is +0.49 EV, 0.7 is −0.51 EV
    static float ExposureEv => Mathf.Log(Exposure, 2f);

    /// Which of the reference paintings a zone is in
    public enum Palette { Sepia, Violet, Steel, Cobalt }

    /// One zone's light: ambient, fog and grade
    public class Preset
    {
        public Color sky, equator, ground;            // trilight ambient, crushed: the key does the work
        public Color fogColor; public float fogDensity; // exponential-squared (unused since the fog went linear; kept for the thumbnails)
        public float fogStart, fogEnd;                  // fogEnd > 0: LINEAR haze between these — every zone now
        public float bloom, threshold, scatter;
        public float exposure, contrast, saturation; public Color filter;
        public float temperature, tint;
        public float vignette; public Color vignetteColor;
        public float grain, aberration;
        public float flare;                            // the screen-space lens flare off the brightest lights
        public Color shadowsTint, highlightsTint;      // the grade's split: what the dark leans to, what the bright leans to
        public float shadowsDown, highlightsUp;        // the split's offsets: how far the shadows are pulled toward black, the highlights lifted
        public float curve;                            // the cel feel: the strength of the S on the master curve and the drain of colour from the darks (0 = none)
        public Palette palette;
    }

    // THE GRADE, shared: contrast up, a little desaturation, the vignette closing in, the split pulled further
    const float Contrast = 25f, Saturation = -10f, Vignette = 0.35f, SplitDown = 0.04f, SplitUp = 0.04f;

    // THE SPLITS, one per palette: what the dark leans to / what the bright leans to
    static readonly Color SepiaShadow = new Color(0.98f, 0.94f, 0.88f), SepiaLight = new Color(1.1f, 1.02f, 0.82f);      // umber under, ochre over
    static readonly Color VioletShadow = new Color(0.95f, 0.9f, 1.1f), VioletLight = new Color(1.12f, 0.98f, 0.84f);     // violet under, orange over
    static readonly Color SteelShadow = new Color(0.9f, 1f, 1.06f), SteelLight = new Color(1f, 1.04f, 1.1f);              // teal under, pale blue over
    static readonly Color CobaltShadow = new Color(0.88f, 0.92f, 1.14f), CobaltLight = new Color(1.12f, 1.06f, 0.86f);   // cobalt under, chandelier yellow over
    static readonly Color AmberLight = new Color(1.08f, 1f, 0.86f);                                                      // the sewers' lamps

    // THE AMBIENTS (readability, 2026-10-16; the maths in LevelKitLook.cs): the sky colour is what a FLOOR gets, the equator
    // what a WALL gets, the ground what an underside gets, so the sky runs a step over the equator in every zone and the
    // eye finds the ground. Set per zone so a floor in the key's shadow band reaches 0.18 linear luminance and a wall 0.12
    // with that zone's floor and wall albedos (LevelKit's palettes) and that zone's key. Before this pass they were crushed
    // to 0.03–0.22 and the floors under a raking key (always in the shadow band) went to black.

    /// The civic streets by day: sepia light, the ambient umber (a floor's sky 0.30 luminance, a wall's equator 0.18), the
    /// haze the level's own horizon (set in Apply)
    public static readonly Preset Utopia = new Preset
    {
        sky = new Color(0.34f, 0.30f, 0.22f), equator = new Color(0.22f, 0.18f, 0.12f), ground = new Color(0.08f, 0.06f, 0.04f),
        fogColor = new Color(0.42f, 0.36f, 0.26f), fogDensity = 0f, fogStart = 30f, fogEnd = 700f,
        bloom = 0.55f, threshold = 1.2f, scatter = 0.7f,
        exposure = 0.05f, contrast = Contrast, saturation = Saturation, filter = new Color(1f, 0.95f, 0.85f),
        temperature = 10f, tint = 0f,
        vignette = Vignette, vignetteColor = new Color(0.02f, 0.015f, 0.01f),
        grain = 0.08f, aberration = 0f,
        flare = 0.3f, shadowsTint = SepiaShadow, highlightsTint = SepiaLight, shadowsDown = SplitDown, highlightsUp = SplitUp, curve = 0.6f,
        palette = Palette.Sepia,
    };
    /// The sewers: a dark warm ambient (a floor's 0.22, a wall's 0.16), the first twenty-five metres clear and a dark umber
    /// haze (never black) closing at ninety, the amber lamps carrying it, heavier grain
    public static readonly Preset Underbelly = new Preset
    {
        sky = new Color(0.26f, 0.21f, 0.16f), equator = new Color(0.20f, 0.15f, 0.11f), ground = new Color(0.06f, 0.045f, 0.035f),
        fogColor = new Color(0.09f, 0.065f, 0.05f), fogDensity = 0f, fogStart = 25f, fogEnd = 90f,
        bloom = 0.7f, threshold = 0.9f, scatter = 0.74f,
        exposure = 0f, contrast = Contrast, saturation = Saturation, filter = new Color(1f, 0.9f, 0.8f),
        temperature = 8f, tint = 0f,
        vignette = Vignette, vignetteColor = new Color(0.01f, 0.005f, 0.005f),
        grain = 0.25f, aberration = 0.08f,
        flare = 0.25f, shadowsTint = SepiaShadow, highlightsTint = AmberLight, shadowsDown = SplitDown, highlightsUp = SplitUp, curve = 0.7f,
        palette = Palette.Sepia,
    };
    /// The company's house, after the casino painting: cobalt in the ambient (a floor's 0.21, a wall's 0.14) and the
    /// shadows, the chandeliers' yellow in the highlights, thirty clear metres and a deep cobalt haze that closes the
    /// halls at a hundred and twenty
    public static readonly Preset Office = new Preset
    {
        sky = new Color(0.16f, 0.20f, 0.42f), equator = new Color(0.10f, 0.13f, 0.30f), ground = new Color(0.03f, 0.035f, 0.09f),
        fogColor = new Color(0.05f, 0.07f, 0.17f), fogDensity = 0f, fogStart = 30f, fogEnd = 120f,
        bloom = 0.6f, threshold = 1.1f, scatter = 0.66f,
        exposure = 0f, contrast = Contrast, saturation = Saturation, filter = new Color(0.95f, 0.97f, 1f),
        temperature = -4f, tint = 0f,
        vignette = Vignette, vignetteColor = new Color(0.01f, 0.012f, 0.03f),
        grain = 0.06f, aberration = 0f,
        flare = 0.35f, shadowsTint = CobaltShadow, highlightsTint = CobaltLight, shadowsDown = SplitDown, highlightsUp = 0.045f, curve = 0.6f,
        palette = Palette.Cobalt,
    };
    /// THE UNDERGROUND (the Works slot), after the blue towers: steel blue and dark teal in the ambient (a floor's 0.31, a
    /// wall's 0.18), the tile cool, thirty clear metres and a cold steel haze closing at a hundred and ten, the globes
    /// and the third rail's arc carrying the picture
    public static readonly Preset Works = new Preset
    {
        sky = new Color(0.26f, 0.32f, 0.40f), equator = new Color(0.14f, 0.18f, 0.23f), ground = new Color(0.04f, 0.05f, 0.07f),
        fogColor = new Color(0.07f, 0.095f, 0.12f), fogDensity = 0f, fogStart = 30f, fogEnd = 110f,
        bloom = 0.7f, threshold = 1f, scatter = 0.7f,
        exposure = 0f, contrast = Contrast, saturation = Saturation, filter = new Color(0.9f, 0.95f, 1f),
        temperature = -6f, tint = 0f,
        vignette = Vignette, vignetteColor = new Color(0.008f, 0.012f, 0.02f),
        grain = 0.12f, aberration = 0f,
        flare = 0.5f, shadowsTint = SteelShadow, highlightsTint = SteelLight, shadowsDown = SplitDown, highlightsUp = SplitUp, curve = 0.65f,
        palette = Palette.Steel,
    };

    /// Chapter 3, the surface at dawn: the ending's own light (MineBlast.Dawn). The haze stays kilometres deep: the house
    /// is nine out and has to come through it (the low band is the smog sheets')
    public static readonly Preset Surface = new Preset
    {
        sky = new Color(0.36f, 0.30f, 0.24f), equator = new Color(0.25f, 0.20f, 0.15f), ground = new Color(0.08f, 0.06f, 0.045f),
        fogColor = new Color(0.5f, 0.34f, 0.22f), fogDensity = 0f, fogStart = 90f, fogEnd = 14000f,
        bloom = 0.6f, threshold = 1.1f, scatter = 0.7f,
        exposure = 0.05f, contrast = Contrast, saturation = Saturation, filter = new Color(1f, 0.94f, 0.86f),
        temperature = 8f, tint = 0f,
        vignette = Vignette, vignetteColor = new Color(0.02f, 0.015f, 0.01f),
        grain = 0.1f, aberration = 0.03f,
        flare = 0.45f, shadowsTint = SepiaShadow, highlightsTint = SepiaLight, shadowsDown = SplitDown, highlightsUp = SplitUp, curve = 0.6f,
        palette = Palette.Sepia,
    };

    /// The rooftops at dusk (Level 12): the last orange low in the west under a violet sky, a plum haze the far city goes
    /// black in at three kilometres, the ambient violet (a roof's 0.28, a parapet's 0.17)
    public static readonly Preset Skyline = new Preset
    {
        sky = new Color(0.34f, 0.25f, 0.40f), equator = new Color(0.24f, 0.15f, 0.18f), ground = new Color(0.06f, 0.04f, 0.05f),
        fogColor = new Color(0.26f, 0.13f, 0.28f), fogDensity = 0f, fogStart = 80f, fogEnd = 2800f,
        bloom = 0.65f, threshold = 1.05f, scatter = 0.7f,
        exposure = 0f, contrast = Contrast, saturation = Saturation, filter = new Color(1f, 0.93f, 0.9f),
        temperature = 4f, tint = 0f,
        vignette = Vignette, vignetteColor = new Color(0.02f, 0.01f, 0.02f),
        grain = 0.1f, aberration = 0.03f,
        flare = 0.5f, shadowsTint = VioletShadow, highlightsTint = VioletLight, shadowsDown = SplitDown, highlightsUp = SplitUp, curve = 0.6f,
        palette = Palette.Violet,
    };

    /// The district after dark (Level 13), after the rain-and-searchlights painting: violet in the ambient, a PURPLE fog
    /// layer the streets vanish into (the smog sheets take this colour) and the far ranks go black in at 1.5 km, the
    /// windows orange through a bloom that takes only them. The ambient violet and HIGH for a night (a roof's 0.27, a
    /// parapet's 0.16): the key is 0.8 here, and the roofs are run on in the dark; the masses still go black in the haze
    public static readonly Preset Nocturne = new Preset
    {
        sky = new Color(0.32f, 0.24f, 0.44f), equator = new Color(0.20f, 0.14f, 0.26f), ground = new Color(0.04f, 0.025f, 0.05f),
        fogColor = new Color(0.12f, 0.07f, 0.18f), fogDensity = 0f, fogStart = 50f, fogEnd = 1500f,
        bloom = 0.85f, threshold = 0.9f, scatter = 0.74f,
        exposure = 0.1f, contrast = Contrast, saturation = Saturation, filter = new Color(0.98f, 0.95f, 1f),
        temperature = 0f, tint = 0f,
        vignette = Vignette, vignetteColor = new Color(0.01f, 0.005f, 0.02f),
        grain = 0.14f, aberration = 0.04f,
        flare = 0.6f, shadowsTint = VioletShadow, highlightsTint = VioletLight, shadowsDown = SplitDown, highlightsUp = SplitUp, curve = 0.7f,
        palette = Palette.Violet,
    };

    /// THE POSTCARD (the level-select thumbnails): the grade of a game's thumbnail, not of play. Only the thumbnail
    /// camera's volume uses it; the ambient and haze fields are unused (the stage sets those from the zone's preset)
    public static readonly Preset Postcard = new Preset
    {
        bloom = 1f, threshold = 0.9f, scatter = 0.74f,
        exposure = 0.2f, contrast = 26f, saturation = -2f, filter = new Color(1f, 0.95f, 0.86f),
        temperature = 6f, tint = 0f,
        vignette = 0.52f, vignetteColor = new Color(0.01f, 0.01f, 0.015f),
        grain = 0.24f, aberration = 0.08f,
        flare = 0.5f, shadowsTint = SepiaShadow, highlightsTint = SepiaLight, shadowsDown = 0.035f, highlightsUp = 0.045f, curve = 0.75f,
        palette = Palette.Sepia,
    };

    /// THE BRUTALIST COURSES (2026-10-27): an overcast day in a concrete city — a flat grey-white sky, a pale fog that
    /// swallows the monoliths at a kilometre, the shadows cool and the highlights colourless, little bloom, no sepia
    public static readonly Preset Brutal = new Preset
    {
        sky = new Color(0.46f, 0.47f, 0.5f), equator = new Color(0.36f, 0.37f, 0.4f), ground = new Color(0.2f, 0.2f, 0.21f),
        fogColor = new Color(0.66f, 0.68f, 0.72f), fogDensity = 0f, fogStart = 50f, fogEnd = 1100f,
        bloom = 0.3f, threshold = 1.3f, scatter = 0.6f,
        exposure = 0.05f, contrast = Contrast, saturation = -18f, filter = new Color(0.97f, 0.98f, 1f),
        temperature = -6f, tint = 0f,
        vignette = Vignette, vignetteColor = new Color(0.02f, 0.02f, 0.025f),
        grain = 0.1f, aberration = 0f,
        flare = 0.12f, shadowsTint = new Color(0.45f, 0.48f, 0.55f), highlightsTint = new Color(0.56f, 0.56f, 0.56f), shadowsDown = SplitDown * 0.5f, highlightsUp = SplitUp * 0.4f, curve = 0.5f,
        palette = Palette.Steel,
    };

    public static Preset For(Zone zone) => zone == Zone.Underbelly ? Underbelly : zone == Zone.Office ? Office : zone == Zone.Works ? Works : zone == Zone.Surface ? Surface : zone == Zone.Brutal ? Brutal : Utopia;

    /// A copy of a preset with its haze recoloured (the shared presets are never mutated)
    static Preset WithFog(Preset p, Color fog) => new Preset
    {
        sky = p.sky, equator = p.equator, ground = p.ground, fogColor = fog, fogDensity = p.fogDensity, fogStart = p.fogStart, fogEnd = p.fogEnd,
        bloom = p.bloom, threshold = p.threshold, scatter = p.scatter,
        exposure = p.exposure, contrast = p.contrast, saturation = p.saturation, filter = p.filter,
        temperature = p.temperature, tint = p.tint, vignette = p.vignette, vignetteColor = p.vignetteColor,
        grain = p.grain, aberration = p.aberration,
        flare = p.flare, shadowsTint = p.shadowsTint, highlightsTint = p.highlightsTint,
        shadowsDown = p.shadowsDown, highlightsUp = p.highlightsUp, curve = p.curve, palette = p.palette,
    };

    /// A level's sky and sun. `look` picks the painting (MenuSky.Paint) drawn on Momentum/Sky; the procedural fields are
    /// the fallback when that shader is missing (`tint` is a scattering bias: 0.5 neutral, a channel above it scatters
    /// more; `thickness` over 1 reddens the horizon). `ground` is the HORIZON: the haze the far city dissolves into, so it
    /// is the fog's colour too (Horizon). The sun's light travels along `sunEuler`; the disc is painted behind, where the
    /// sun sits; `sunSize` 0 paints no disc (a night's key is the sodium glow, not a sun). THE KEY RAKES: elevation
    /// 20–30°, azimuth 35° off the street axis (+z), so a stepped mass gets one lit face and one black one.
    public struct Sky
    {
        public Color tint, ground, sunColor;
        public float thickness, exposure, sunSize, convergence, sunIntensity;
        public Vector3 sunEuler;
        public bool shadows;
        public SkyLook look;
        public float moonAz, moonElev, moonSize;   // a moon when moonSize > 0 (degrees of radius; a painted moon is big)
        public Color rimColor;                      // the back light's colour (cool; per palette)
    }

    // The rims, per palette: cool, so a silhouette reads against black
    static readonly Color SepiaRim = new Color(0.7f, 0.72f, 0.85f), VioletRim = new Color(0.62f, 0.55f, 1f), SteelRim = new Color(0.55f, 0.75f, 0.95f);
    // The horizons, per palette (sRGB, as the fog wants them)
    static readonly Color SepiaHorizon = new Color(0.42f, 0.36f, 0.26f), DuskHorizon = new Color(0.4f, 0.17f, 0.26f), NightHorizon = new Color(0.16f, 0.08f, 0.22f);

    // THE KEYS (readability, 2026-10-16): day 1.3–1.4, the overcast 1.3, dusk 1.3, Night 0.8, the Underground 0.6; the rake
    // kept (elevation 12–30°, yaw 35° off the axis). Under the ramp a key's shadow band is 0.45 of its lit band, so these
    // are the fill of every roofed room too: LevelKitLook.cs works the numbers through
    /// The first act's noon: sepia daylight under a mauve overcast, the sun low enough to rake
    static Sky Noon => new Sky { tint = new Color(0.52f, 0.48f, 0.36f), ground = SepiaHorizon, thickness = 1.3f, exposure = 0.9f, sunSize = 0.03f, convergence = 5f, sunEuler = new Vector3(26f, 35f, 0f), sunColor = new Color(1f, 0.86f, 0.62f), sunIntensity = 1.4f, shadows = true, look = SkyLook.Sepia, rimColor = SepiaRim };

    /// The first act is an arc: a sepia morning, an overcast noon, late light, the company's grey, sundown at the shaft; the Works see no sky
    /// THE COURSES' DAY (2026-10-27): the ten brutalist courses stand under an overcast white-grey, the sun a bright
    /// smudge somewhere different on each, the horizon pale so the monoliths dissolve into it; the rooftop chapter
    /// keeps its dawn, sunset and night
    public static Sky SkyFor(LevelKind kind) => SkyForChapters(kind);   // the painted skies stand (user 2026-10-27: keep the Gotham Dark Deco look); Overcast is kept for a brutalist zone only

    static bool IsCourse(LevelKind kind) => kind == LevelKind.Level1 || kind == LevelKind.Level2 || kind == LevelKind.Level3 || kind == LevelKind.Level4 || kind == LevelKind.Level5
        || kind == LevelKind.Level6 || kind == LevelKind.Level7 || kind == LevelKind.Level8 || kind == LevelKind.Level9 || kind == LevelKind.Level10;

    static Sky Overcast(LevelKind kind)
    {
        int n = (int)kind;
        return new Sky
        {
            tint = new Color(0.62f, 0.64f, 0.68f), ground = new Color(0.66f, 0.68f, 0.72f), thickness = 1.1f, exposure = 0.95f, sunSize = 0.02f, convergence = 3f,
            sunEuler = new Vector3(48f + 6f * (n % 3), 20f + 35f * (n % 4), 0f), sunColor = new Color(0.96f, 0.97f, 1f), sunIntensity = 1.25f, shadows = true,
            look = SkyLook.Procedural, rimColor = SteelRim,
        };
    }

    static Sky SkyForChapters(LevelKind kind) => kind switch
    {
        LevelKind.Level2 => new Sky { tint = new Color(0.5f, 0.47f, 0.36f), ground = new Color(0.46f, 0.38f, 0.27f), thickness = 1.25f, exposure = 0.95f, sunSize = 0.035f, convergence = 5f, sunEuler = new Vector3(26f, 125f, 0f), sunColor = new Color(1f, 0.84f, 0.6f), sunIntensity = 1.4f, shadows = true, look = SkyLook.Sepia, rimColor = SepiaRim },
        LevelKind.Level3 => new Sky { tint = new Color(0.5f, 0.48f, 0.42f), ground = new Color(0.36f, 0.33f, 0.3f), thickness = 1.8f, exposure = 0.7f, sunSize = 0.02f, convergence = 6f, sunEuler = new Vector3(30f, 305f, 0f), sunColor = new Color(0.9f, 0.86f, 0.78f), sunIntensity = 1.3f, shadows = true, look = SkyLook.Sepia, rimColor = SepiaRim },
        LevelKind.Level4 => new Sky { tint = new Color(0.52f, 0.45f, 0.36f), ground = new Color(0.5f, 0.34f, 0.22f), thickness = 1.5f, exposure = 0.9f, sunSize = 0.04f, convergence = 3f, sunEuler = new Vector3(20f, 215f, 0f), sunColor = new Color(1f, 0.74f, 0.5f), sunIntensity = 1.4f, shadows = true, look = SkyLook.Sepia, rimColor = SepiaRim },
        LevelKind.Level5 => new Sky { tint = new Color(0.48f, 0.48f, 0.44f), ground = new Color(0.4f, 0.37f, 0.33f), thickness = 1.5f, exposure = 0.75f, sunSize = 0.025f, convergence = 5f, sunEuler = new Vector3(28f, 35f, 0f), sunColor = new Color(0.96f, 0.94f, 0.88f), sunIntensity = 1.3f, shadows = true, look = SkyLook.Sepia, rimColor = SepiaRim },
        LevelKind.Level6 => new Sky { tint = new Color(0.37f, 0.38f, 0.52f), ground = DuskHorizon, thickness = 2.3f, exposure = 0.85f, sunSize = 0.06f, convergence = 2.5f, sunEuler = new Vector3(12f, 125f, 0f), sunColor = new Color(1f, 0.52f, 0.28f), sunIntensity = 1.3f, shadows = true, look = SkyLook.Violet, rimColor = VioletRim },
        // The Underground sees no sky: a cold cream key from high up (45°: the floors are in its LIT band, the one zone
        // where they are), no shadows (cost), 0.6 so a tiled wall and a terrazzo floor read with the globes off
        LevelKind.Level7 or LevelKind.Level8 or LevelKind.Level9 or LevelKind.Level10 => new Sky { tint = new Color(0.35f, 0.3f, 0.25f), ground = new Color(0.07f, 0.095f, 0.12f), thickness = 1.2f, exposure = 0.25f, sunSize = 0.01f, convergence = 6f, sunEuler = new Vector3(45f, 35f, 0f), sunColor = new Color(0.9f, 0.94f, 1f), sunIntensity = 0.6f, shadows = false, look = SkyLook.Procedural, rimColor = SteelRim },
        LevelKind.Level11 => Dawn,   // Chapter 3 starts in the ending's last frame
        LevelKind.Level12 => Sunset,    // the roofs at the end of the day: the sun going down in the west
        LevelKind.Level13 => Night,     // crosstown after dark: the city lit, as it was drawn to be
        _ => Noon,
    };

    /// The end of the Works, and Chapter 3: a sepia dawn at ground level, the sun just up behind the house, raking
    public static Sky Dawn => new Sky { tint = new Color(0.5f, 0.45f, 0.4f), ground = new Color(0.5f, 0.34f, 0.22f), thickness = 1.9f, exposure = 0.9f, sunSize = 0.03f, convergence = 3f, sunEuler = new Vector3(14f, 215f, 0f), sunColor = new Color(1f, 0.7f, 0.45f), sunIntensity = 1.4f, shadows = true, look = SkyLook.Sepia, rimColor = SepiaRim };
    /// The roofs at the end of the day: the last orange low in the west under a violet sky, a thin moon low opposite
    public static Sky Sunset => new Sky { tint = new Color(0.37f, 0.38f, 0.56f), ground = DuskHorizon, thickness = 2.3f, exposure = 0.8f, sunSize = 0.05f, convergence = 2.5f, sunEuler = new Vector3(12f, 125f, 0f), sunColor = new Color(1f, 0.55f, 0.3f), sunIntensity = 1.4f, shadows = true, look = SkyLook.Violet, moonAz = 300f, moonElev = 16f, moonSize = 1.8f, rimColor = VioletRim };
    /// The district after dark, after the red-street painting: a violet sky with a full moon high in it and flat clouds
    /// across it, the key the city's own sodium amber raking from the north-east, no sun disc
    public static Sky Night => new Sky { tint = new Color(0.3f, 0.36f, 0.62f), ground = NightHorizon, thickness = 1.6f, exposure = 0.35f, sunSize = 0f, convergence = 8f, sunEuler = new Vector3(25f, 215f, 0f), sunColor = new Color(1f, 0.7f, 0.38f), sunIntensity = 0.8f, shadows = true, look = SkyLook.Violet, moonAz = 20f, moonElev = 42f, moonSize = 2.4f, rimColor = VioletRim };

    /// An hour into Chapter 3: the sun up behind the house, the roofs in a muted morning, the streets still in shadow
    public static Sky Morning => new Sky { tint = new Color(0.5f, 0.47f, 0.38f), ground = new Color(0.5f, 0.4f, 0.28f), thickness = 1.4f, exposure = 0.95f, sunSize = 0.035f, convergence = 3.5f, sunEuler = new Vector3(18f, 215f, 0f), sunColor = new Color(1f, 0.82f, 0.6f), sunIntensity = 1.4f, shadows = true, look = SkyLook.Sepia, rimColor = SepiaRim };

    /// A sky and its sun, on their own: a cutscene that cuts to somewhere with a different sky calls this at the cut
    public static void ApplySky(Sky sky)
    {
        Sun(sky);
        Skybox(sky);
    }

    /// The colour the far city dissolves into: the sky's own horizon colour, so haze and horizon agree
    public static Color Horizon(LevelKind kind) => SkyFor(kind).ground;

    /// The level is built: light it. Sky, sun and rim, ambient and fog from the level's primary zone, one volume per
    /// zone the level contains (cross-faded room by room by WorldLookBlend), the stripes on the floods.
    public static void Apply(LevelKind kind, LevelSections.Layout layout)
    {
        var sky = SkyFor(kind);
        var root = new GameObject("WorldLook");
        Sun(sky, root.transform);
        Skybox(sky);

        // Which zones does this level contain? Sealed levels without a section chain are all Utopia.
        var zones = new List<Zone>();
        Zone primary = kind == LevelKind.Level7 || kind == LevelKind.Level8 || kind == LevelKind.Level9 || kind == LevelKind.Level10 ? Zone.Works : kind == LevelKind.Level5 ? Zone.Office
            : kind == LevelKind.Level11 || kind == LevelKind.Level12 || kind == LevelKind.Level13 ? Zone.Surface : Zone.Utopia;
        if (layout != null && layout.rooms.Count > 0)
        {
            primary = layout.rooms[0].zone;
            foreach (var r in layout.rooms) if (!zones.Contains(r.zone)) zones.Add(r.zone);
        }
        if (zones.Contains(Zone.Brutal)) primary = Zone.Brutal;   // a course with an opening in another zone is still the course's day
        if (!zones.Contains(primary)) zones.Insert(0, primary);

        var blend = root.AddComponent<WorldLookBlend>();
        blend.rooms = layout != null ? layout.rooms : null;
        foreach (var z in zones)
        {
            var go = new GameObject("Look " + z);
            go.transform.SetParent(root.transform, false);
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 10f;
            vol.weight = z == primary ? 1f : 0f;
            var preset = For(z);
            if (z == Zone.Surface && kind == LevelKind.Level12) preset = Skyline;    // the roofs at dusk, not the wound
            if (z == Zone.Surface && kind == LevelKind.Level13) preset = Nocturne;   // the district after dark
            // The day streets' haze takes the level's own horizon colour, so the far city dissolves into its sky (the
            // House's interiors keep their cobalt-black: a hall closes in the dark, not in the sky)
            if (z == Zone.Utopia || z == Zone.Brutal) preset = WithFog(preset, sky.ground);
            vol.sharedProfile = Profile(preset);
            blend.Add(z, vol, preset);
        }
        blend.SetImmediate(primary);
        // The sparkle, now that the lamps all exist: glints and cones on them, flares on the biggest, smoke off the
        // roofs' chimneys, the open city's smog and paper (LevelKitSmog)
        bool night = primary == Zone.Underbelly || primary == Zone.Works || kind == LevelKind.Level13 || kind == LevelKind.Level12;
        LevelKit.Sparkle(kind, primary, night);
        Bands();                     // the striped light across walls and floors
        LevelKit.ToonRouteUnits();   // the units' shells onto the ramp, by their names
    }

    /// The main menu's night gets its own grade (the sepia painting): the bloom for the windows, beacons and
    /// searchlights, the blacks crushed, umber under and ochre over, the vignette, grain
    public static void ApplyMenu(Transform root)
    {
        var go = new GameObject("Look Menu");
        go.transform.SetParent(root, false);
        var vol = go.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 10f;
        vol.weight = 1f;
        vol.sharedProfile = Profile(new Preset
        {
            // A city of lit windows is a great deal of light: the bloom is for what is genuinely bright (windows, lamps,
            // beacons, searchlights), not a wash over the stone
            bloom = 0.8f, threshold = 0.9f, scatter = 0.74f,
            exposure = 0.1f, contrast = Contrast, saturation = Saturation, filter = new Color(1f, 0.95f, 0.86f),
            temperature = 6f, tint = 0f,
            vignette = Vignette, vignetteColor = new Color(0.01f, 0.01f, 0.015f),
            grain = 0.16f, aberration = 0.05f,
            flare = 0.6f, shadowsTint = SepiaShadow, highlightsTint = SepiaLight, shadowsDown = SplitDown, highlightsUp = SplitUp, curve = 0.7f,
            palette = Palette.Sepia,
        });
    }

    /// One VolumeProfile from a preset, every component overridden explicitly
    public static VolumeProfile Profile(Preset p)
    {
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "WorldLook";
        profile.hideFlags = HideFlags.HideAndDontSave;
        var tone = profile.Add<Tonemapping>(true);
        tone.mode.value = TonemappingMode.Neutral;
        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.value = p.bloom;
        bloom.threshold.value = p.threshold;
        bloom.scatter.value = p.scatter;
        bloom.highQualityFiltering.value = true;
        bloom.clamp.value = 8f;
        var grade = profile.Add<ColorAdjustments>(true);
        // The preset's own EV plus the BRIGHTNESS knob (the thumbnails' postcard grade stays as authored)
        bool knob = p != Postcard;
        grade.postExposure.value = p.exposure + (knob ? ExposureEv : 0f);
        if (knob) grades.Add((grade, p.exposure));
        grade.contrast.value = p.contrast;
        grade.saturation.value = p.saturation;
        grade.colorFilter.value = p.filter == default ? Color.white : p.filter;
        var wb = profile.Add<WhiteBalance>(true);
        wb.temperature.value = p.temperature;
        wb.tint.value = p.tint;
        var vig = profile.Add<Vignette>(true);
        vig.intensity.value = p.vignette;
        vig.color.value = p.vignetteColor;
        vig.smoothness.value = 0.35f;
        vig.rounded.value = false;
        var grain = profile.Add<FilmGrain>(true);
        grain.type.value = FilmGrainLookup.Medium1;
        grain.intensity.value = p.grain;
        grain.response.value = 0.8f;
        var ca = profile.Add<ChromaticAberration>(true);
        ca.intensity.value = p.aberration;
        // The flare: what the brightest lights throw across the lens, warm, a touch of it, and the streak of a beacon or a sun
        var flare = profile.Add<ScreenSpaceLensFlare>(true);
        flare.intensity.value = p.flare;
        flare.tintColor.value = new Color(1f, 0.86f, 0.66f);
        flare.bloomMip.value = 1;
        flare.firstFlareIntensity.value = 0.6f;
        flare.secondaryFlareIntensity.value = 0.4f;
        flare.warpedFlareIntensity.value = 0.3f;
        flare.samples.value = 2;
        flare.sampleDimmer.value = 0.5f;
        flare.vignetteEffect.value = 0.8f;
        flare.streaksIntensity.value = p.flare * 0.6f;
        flare.streaksLength.value = 0.6f;
        flare.streaksThreshold.value = 0.35f;
        flare.chromaticAbberationIntensity.value = 0.2f;
        // The split: the palette's cast in its shadows and another in its highlights; the shadows pulled toward black
        // (the crushed blacks of the style), the highlights lifted a hair; the shadow band kept to the true darks
        var smh = profile.Add<ShadowsMidtonesHighlights>(true);
        smh.shadows.value = Tint(p.shadowsTint, -p.shadowsDown);
        smh.midtones.value = new Vector4(1f, 1f, 1f, 0f);
        smh.highlights.value = Tint(p.highlightsTint, p.highlightsUp);
        smh.shadowsStart.value = 0f;
        smh.shadowsEnd.value = 0.3f;
        smh.highlightsStart.value = 0.55f;
        smh.highlightsEnd.value = 1f;
        // The cel feel: an S on the master curve (the darks darker, the lights lighter, the midtones steeper) and the
        // colour drained from the darks, so a shadow is a flat dark wash the way an animator paints one
        if (p.curve > 0f)
        {
            var curves = profile.Add<ColorCurves>(true);
            curves.master.value = new TextureCurve(SCurve(p.curve), 0f, false, new Vector2(0f, 1f));
            curves.lumVsSat.value = new TextureCurve(DarkDrain(p.curve), 0.5f, false, new Vector2(0f, 1f));
        }
        return profile;
    }

    /// A grade tint as the split-toning wants it, with its offset; an unset colour is no tint
    static Vector4 Tint(Color c, float offset) => c == default ? new Vector4(1f, 1f, 1f, offset) : new Vector4(c.r, c.g, c.b, offset);

    /// The master curve's S: the quarter tones pulled down and the three-quarter tones up by `k` × 0.07, smooth through
    static AnimationCurve SCurve(float k)
    {
        float d = 0.07f * k;
        var c = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.25f, 0.25f - d), new Keyframe(0.75f, 0.75f + d), new Keyframe(1f, 1f));
        for (int i = 0; i < c.length; i++) c.SmoothTangents(i, 0f);
        return c;
    }

    /// Luminance against saturation: the darks lose up to `k` × 0.3 of their colour, the rest keeps it (0.5 is neutral)
    static AnimationCurve DarkDrain(float k)
    {
        var c = new AnimationCurve(new Keyframe(0f, 0.5f - 0.3f * k), new Keyframe(0.3f, 0.5f), new Keyframe(1f, 0.5f));
        for (int i = 0; i < c.length; i++) c.SmoothTangents(i, 0f);
        return c;
    }

    // ---- THE KEY AND THE RIM. The scene's directional light is the key; a second one, "WorldLookRim", low and opposite,
    // no shadows, is the back light that picks out the far edge of every mass. Under the toon ramp a flat lit band gives a
    // back face the WHOLE of it, and the key's shadow band is now 0.45 of its lit band (_ShadowTone), so the back light
    // at a full 0.6 would put a night's back face (0.45 x 0.59 + 0.6 x 0.6 = 0.63) OVER its lit face (0.59): the mass
    // would light from the wrong side. Half of it keeps the lit face the lit face (back 0.45 of lit by day, 0.75 at
    // night); the shader's own silhouette rim (_RimStrength 0.6) draws the edge.
    const string RimName = "WorldLookRim";
    public const float RimStrength = 0.6f;
    const float ToonRimScale = 0.5f;
    const float RimElevation = 10f;

    static void Sun(Sky sky, Transform parent = null)
    {
        Light sun = null, rim = null;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional) continue;
            if (l.name == RimName) { rim = l; continue; }
            if (sun == null) sun = l;
        }
        if (sun == null) return;
        sun.useColorTemperature = false; // the scene's 5000 K was warming every colour authored here
        sun.color = sky.sunColor;
        sun.intensity = sky.sunIntensity;
        sun.transform.rotation = Quaternion.Euler(sky.sunEuler);
        sun.shadows = sky.shadows ? LightShadows.Soft : LightShadows.None;
        sun.shadowStrength = 0.9f;
        RenderSettings.sun = sun;

        if (rim == null)
        {
            var go = new GameObject(RimName);
            if (parent != null) go.transform.SetParent(parent, false);   // dies with the level's look
            rim = go.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.shadows = LightShadows.None;
        }
        rim.useColorTemperature = false;
        rim.color = sky.rimColor == default ? new Color(0.6f, 0.7f, 1f) : sky.rimColor;
        rim.intensity = RimStrength * (Toon ? ToonRimScale : 1f);
        rim.transform.rotation = Quaternion.Euler(RimElevation, sky.sunEuler.y + 180f, 0f);
        rim.cullingMask = sun.cullingMask;
    }

    // ---- THE SKY: painted (MenuSky.Paint on Momentum/Sky) when that shader compiled, the procedural one when not.
    const string SkyShaderName = "Momentum/Sky";
    static Material skyMat, paintedMat;
    static Texture2D painted;
    static string paintedKey;
    static int paintedState = -1;   // -1 unchecked, 0 missing or unsupported, 1 ok

    static bool PaintedOk()
    {
        if (paintedState < 0)
        {
            var s = Shader.Find(SkyShaderName);
            paintedState = s != null && s.isSupported ? 1 : 0;
            if (paintedState == 0) Debug.LogWarning("MomentumSky: shader missing or unsupported, procedural sky fallback");
        }
        return paintedState == 1;
    }

    /// The painting a level's sky is: the haze its own horizon (the fog's colour, so the far city dissolves into it),
    /// the disc where the sun SITS (its light travels along sunEuler, so it sits behind), the moon where the sky says
    static MenuSky.SkyPaint PaintFor(Sky sky) => new MenuSky.SkyPaint
    {
        look = sky.look, hazeAway = sky.ground, hazeToward = sky.ground,
        sunAz = sky.sunEuler.y + 180f, sunElev = sky.sunEuler.x, sunSize = sky.sunSize, sunColor = sky.sunColor,
        moonAz = sky.moonAz, moonElev = sky.moonElev, moonSize = sky.moonSize, seed = 1939,
    };

    static void Skybox(Sky sky)
    {
        if (sky.look != SkyLook.Procedural && PaintedOk())
        {
            string key = sky.look + "/" + sky.sunEuler + "/" + sky.ground + "/" + sky.moonAz + "/" + sky.moonElev + "/" + sky.sunSize;   // once per level
            if (painted == null || paintedKey != key)
            {
                if (painted != null) { if (Application.isPlaying) Object.Destroy(painted); else Object.DestroyImmediate(painted); }
                painted = MenuSky.Paint(PaintFor(sky), "WorldLookSky " + sky.look);
                paintedKey = key;
            }
            if (paintedMat == null) paintedMat = new Material(Shader.Find(SkyShaderName)) { name = "WorldLookSkyPainted", hideFlags = HideFlags.HideAndDontSave };
            paintedMat.SetTexture("_MainTex", painted);
            paintedMat.SetFloat("_Exposure", 1f);
            RenderSettings.skybox = paintedMat;
        }
        else
        {
            // The procedural sky: it ships because every scene's default skybox uses the same shader
            var shader = Shader.Find("Skybox/Procedural");
            if (shader == null) return;
            if (skyMat == null) skyMat = new Material(shader) { name = "WorldLookSky", hideFlags = HideFlags.HideAndDontSave };
            skyMat.SetColor("_SkyTint", sky.tint);
            skyMat.SetColor("_GroundColor", sky.ground);
            skyMat.SetFloat("_AtmosphereThickness", sky.thickness);
            skyMat.SetFloat("_Exposure", sky.exposure);
            skyMat.SetFloat("_SunSize", sky.sunSize);
            skyMat.SetFloat("_SunSizeConvergence", sky.convergence);
            RenderSettings.skybox = skyMat;
        }
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        DynamicGI.UpdateEnvironment(); // the reflections refresh once, so the gloss floors mirror this sky
    }

    // ---- THE LIGHT BANDS (the viaduct and the searchlight paintings): striped light falling across walls and floors as
    // clean hard bands. A procedural cookie of CookieBands louvre stripes on every FLOOD (a spot under 100° that casts
    // shadows: LevelKitSmog's definition; torchieres and the lamps are not touched) that has no cookie of its own. The
    // pipeline's light cookies are on in both RP assets (Momentum/Look/Check Toon Shader confirms and fixes that).
    public const bool LightBands = true;
    const int CookieBands = 4;          // stripes across the cone
    const float CookieOpen = 0.55f;     // the open share of each stripe: the rest is the slat
    static Texture2D stripes;

    /// The louvre: a square cookie of hard stripes with a round edge (the cone's own), made once
    public static Texture2D StripeCookie
    {
        get
        {
            if (stripes == null) stripes = MakeStripes();
            return stripes;
        }
    }

    static Texture2D MakeStripes()
    {
        const int n = 128;
        float texelsPerCell = n / (float)CookieBands;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "WorldLookStripes", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n, v = (y + 0.5f) / n;
                float cell = (u * CookieBands) % 1f;
                float band = cell < CookieOpen ? 1f : 0f;
                // A one-texel soft edge at each slat: hard, not aliased
                float toEdge = Mathf.Min(Mathf.Abs(cell - CookieOpen), Mathf.Min(cell, 1f - cell)) * texelsPerCell;
                band = Mathf.Lerp(0.5f, band, Mathf.Clamp01(toEdge / 1.5f));
                float dx = u - 0.5f, dy = v - 0.5f;
                float rim = 1f - Mathf.Clamp01((Mathf.Sqrt(dx * dx + dy * dy) - 0.4f) / 0.1f);   // round, so the cookie never shows a square
                byte g = (byte)Mathf.RoundToInt(255f * band * rim);
                px[y * n + x] = new Color32(g, g, g, 255);
            }
        t.SetPixels32(px);
        t.Apply(true, true);
        return t;
    }

    /// The stripes onto the floods: once per level, after the build (Apply)
    public static void Bands()
    {
        if (!LightBands) return;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type != LightType.Spot || l.spotAngle >= 100f || l.shadows == LightShadows.None || l.cookie != null) continue;
            l.cookie = StripeCookie;
        }
    }
}

/// <summary>
/// The cross-fade between zones as the player moves room to room: each zone's
/// volume weight, and the ambient and fog, slide toward the zone of the room
/// the player is in over about half a second. No triggers: the rooms are known.
/// </summary>
public class WorldLookBlend : MonoBehaviour
{
    public List<LevelSections.RoomSpan> rooms;

    readonly List<Zone> zones = new List<Zone>();
    readonly List<Volume> volumes = new List<Volume>();
    readonly List<WorldLook.Preset> presets = new List<WorldLook.Preset>();
    readonly List<float> weights = new List<float>();
    Zone target;
    int roomIndex = -1;
    float nextRoute;   // the next sweep of the units' shells onto the ramp (the kinds that spawn after the build)
    const float RouteEvery = 2f;

    public void Add(Zone z, Volume v, WorldLook.Preset p)
    {
        zones.Add(z);
        volumes.Add(v);
        presets.Add(p);
        weights.Add(0f);
    }

    public void SetImmediate(Zone z)
    {
        target = z;
        for (int i = 0; i < zones.Count; i++) weights[i] = zones[i] == z ? 1f : 0f;
        Push();
    }

    void Update()
    {
        float now = Time.unscaledTime;
        if (now >= nextRoute)
        {
            nextRoute = now + RouteEvery;
            LevelKit.ToonRouteUnits();
        }
        // Where is the player? The room whose span holds them decides the zone
        var director = CombatDirector.Instance;
        if (rooms != null && director != null && director.motor != null)
        {
            float z = director.motor.CurrPosition.z;
            int found = roomIndex;
            if (roomIndex < 0 || roomIndex >= rooms.Count || z < rooms[roomIndex].z0 || z >= rooms[roomIndex].z1)
            {
                found = -1;
                for (int i = 0; i < rooms.Count; i++) if (z >= rooms[i].z0 && z < rooms[i].z1) { found = i; break; }
            }
            if (found >= 0) { roomIndex = found; target = rooms[found].zone; }
        }
        bool moved = false;
        float step = Time.unscaledDeltaTime / 0.6f;
        for (int i = 0; i < zones.Count; i++)
        {
            float want = zones[i] == target ? 1f : 0f;
            if (Mathf.Approximately(weights[i], want)) continue;
            weights[i] = Mathf.MoveTowards(weights[i], want, step);
            moved = true;
        }
        if (moved) Push();
    }

    /// The weights onto the volumes, and the mixed ambient and fog onto the render settings
    void Push()
    {
        float total = 0f;
        Color sky = Color.black, eq = Color.black, ground = Color.black, fog = Color.black;
        float density = 0f, start = 0f, end = 0f, linear = 0f;
        for (int i = 0; i < zones.Count; i++)
        {
            volumes[i].weight = weights[i];
            float w = weights[i];
            if (w <= 0f) continue;
            total += w;
            sky += presets[i].sky * w;
            eq += presets[i].equator * w;
            ground += presets[i].ground * w;
            fog += presets[i].fogColor * w;
            density += presets[i].fogDensity * w;
            start += presets[i].fogStart * w;
            end += presets[i].fogEnd * w;
            if (presets[i].fogEnd > 0f) linear += w;
        }
        if (total <= 0f) return;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = sky / total;
        RenderSettings.ambientEquatorColor = eq / total;
        RenderSettings.ambientGroundColor = ground / total;
        RenderSettings.fog = true;
        RenderSettings.fogColor = fog / total;
        if (linear > total * 0.5f)
        {
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = start / total;
            RenderSettings.fogEndDistance = Mathf.Max(start / total + 1f, end / total);
        }
        else
        {
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = density / total;
        }
    }
}
