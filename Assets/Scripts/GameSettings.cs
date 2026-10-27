using UnityEngine;

/// <summary>
/// Player-facing settings, persisted in PlayerPrefs and layered over the
/// design config: sensitivity multiplier, base FOV override, music and sound-effect volumes.
/// </summary>
public static class GameSettings
{
    public static float SensitivityMult { get; private set; } = 1f;
    public static float FovOverride { get; private set; } = -1f; // <0 = use config
    public static float MusicVolume { get; private set; } = 1f;
    public static float SfxVolume { get; private set; } = 1f;

    static bool loaded;

    public static void Load()
    {
        SensitivityMult = PlayerPrefs.GetFloat("set_sens", 1f);
        FovOverride = PlayerPrefs.GetFloat("set_fov", -1f);
        MusicVolume = PlayerPrefs.GetFloat("set_music", 1f);
        SfxVolume = PlayerPrefs.GetFloat("set_sfx", 1f);
        loaded = true;
    }

    public static void Apply()
    {
        if (!loaded) Load();
        AudioListener.volume = 1f; // the two sliders scale their own sources; the listener stays at unity
    }

    public static void SetSensitivity(float mult)
    {
        SensitivityMult = Mathf.Clamp(mult, 0.3f, 3f);
        PlayerPrefs.SetFloat("set_sens", SensitivityMult);
    }

    public static void SetFov(float fov)
    {
        FovOverride = Mathf.Clamp(fov, 80f, 110f);
        PlayerPrefs.SetFloat("set_fov", FovOverride);
    }

    /// MUSIC: MusicDirector (and the synth boss fallback) read it every frame
    public static void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("set_music", MusicVolume);
    }

    /// SOUND FX: every effect source (GameFeel, the menu blips) takes it as its source volume
    public static void SetSfxVolume(float volume)
    {
        SfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("set_sfx", SfxVolume);
    }

    public static float Sensitivity(MovementConfig config)
        => (config != null ? config.mouseSensitivity : 0.12f) * SensitivityMult;

    public static float Fov(MovementConfig config)
        => FovOverride > 0f ? FovOverride : (config != null ? config.fieldOfView : 95f);
}
