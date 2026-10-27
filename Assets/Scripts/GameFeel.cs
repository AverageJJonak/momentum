using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Audio + camera feedback. Sound design v6 — RECORDINGS ONLY. Every sound is a
/// clip from Resources/Impacts, bucketed by filename (see the README there),
/// picked at random with pitch jitter. No synthesis anywhere in the SFX; the
/// music is MusicDirector's; nothing here is generated any more.
///
/// Deadlock rule for movement: the whoosh IS the effort meter. Low-effort moves
/// (jump, double jump, slide-jump) are cloth rustles; medium (edge boost,
/// perfect hops) are soft swishes; high (dash, wall kick, slam, corner boost)
/// are hard swishes with a thump. Slide starts are SILENT, a kept bhop a quiet rustle —
/// not everything needs a sound. Big hits go through the crash channel (reverb
/// + distortion DSP) and shake the camera. The parry is a real sword clash,
/// pitched up as you chain them. Speed is seen (radial speed lines), not heard.
/// </summary>
public class GameFeel : MonoBehaviour
{
    public MovementConfig config;
    public PlayerMotor motor;
    public PlayerLook look;
    public StyleSystem style;
    public GrappleAbility grapple;

    public static GameFeel Instance { get; private set; }

    AudioSource source;       // plain one-shots
    readonly Dictionary<string, int> lastPick = new Dictionary<string, int>();
    AudioSource pitchSource;  // one-shots that carry a pitch (parries, chings, stingers)
    AudioSource impactSource; // CRASH channel: its own object with reverb + distortion DSP
    AudioSource[] partSources; // THE PARTS: gears, springs and shards scattering — a small pool, each piece at its own pitch
    int partNext;
    float lastSpillAt = float.NegativeInfinity;

    // Loaded ONCE per session and kept for good. The menu has no GameFeel, so on
    // every level → menu load these 265 clips fell out of use and Unity's unused-
    // asset sweep deleted them one by one — close to a minute of frozen game.
    static readonly Dictionary<string, List<AudioClip>> samples = new Dictionary<string, List<AudioClip>>();

    int resetCombo;
    float lastResetTime = float.NegativeInfinity;
    int parryCombo;
    float lastParryTime = float.NegativeInfinity;
    float flashAlpha;
    float hurtAlpha;
    float flowVis;
    float speedLineSmooth;
    float speedLineBurst;
    float stepTimer;
    float slideTimer;
    Texture2D vignette;
    Texture2D[] speedLineTex;
    static Material sparkMat, cyanSparkMat;

    /// Explicit buckets: a clip named "<bucket>_anything" lands here first
    static readonly string[] Buckets =
    {
        "parry", "whoosh_soft", "whoosh_hard", "swing", "slice", "shot", "projhit", "boom", "sub", "warp", "portal",
        "stagger", "tick", "rankup", "rankdown", "dud", "ching", "warn", "flowin", "flowout", "glitch", "slide",
        "finish", "spawn", "cloth", "belt", "leather", "step", "bowdraw", "bowshot", "bownock", "thunder",
        "shing", "clash", "slash",
    };

    void Awake()
    {
        Instance = this;
        source = NewSource(gameObject);
        pitchSource = NewSource(gameObject);
        partSources = new AudioSource[3];
        for (int i = 0; i < partSources.Length; i++) partSources[i] = NewSource(gameObject);

        // The crash channel lives on its own object: Unity's audio filters apply
        // to every source on a GameObject, and only big hits want the room + grit
        var crashGo = new GameObject("CrashChannel");
        crashGo.transform.SetParent(transform, false);
        impactSource = NewSource(crashGo);
        var reverb = crashGo.AddComponent<AudioReverbFilter>();
        reverb.reverbPreset = AudioReverbPreset.User;
        reverb.dryLevel = 0f;
        reverb.room = -700f;
        reverb.roomHF = -500f;
        reverb.decayTime = 0.9f;
        reverb.decayHFRatio = 0.55f;
        reverb.reflectionsLevel = -400f;
        reverb.reflectionsDelay = 0.006f;
        reverb.reverbLevel = -250f;
        reverb.reverbDelay = 0.018f;
        reverb.diffusion = 100f;
        reverb.density = 100f;
        var grit = crashGo.AddComponent<AudioDistortionFilter>();
        grit.distortionLevel = 0.28f;

        LoadSamples();
        BuildVignette();
        BuildSpeedLines();
    }

    static AudioSource NewSource(GameObject host)
    {
        var s = host.AddComponent<AudioSource>();
        s.spatialBlend = 0f;
        return s;
    }

    /// Every AudioClip under Resources/Impacts, bucketed by name: explicit
    /// "<bucket>_" prefixes first, then material words for the impact packs
    /// A clip from a bucket, for anything outside gameplay (the menu's thunder): the library loads on the first ask
    public static AudioClip Clip(string key)
    {
        LoadSamples();
        return samples.TryGetValue(key, out var list) && list.Count > 0 ? list[Random.Range(0, list.Count)] : null;
    }

    static void LoadSamples()
    {
        if (samples.Count > 0) return; // already loaded this session
        int total = 0;
        foreach (var clip in Resources.LoadAll<AudioClip>("Impacts"))
        {
            total++;
            string n = clip.name.ToLowerInvariant();
            string key = null;
            foreach (var b in Buckets)
                if (n.StartsWith(b + "_")) { key = b; break; }
            if (key == null)
                key = n.Contains("parry") ? "parry"
                    : n.Contains("footstep") ? "step"
                    : n.Contains("swing") || n.Contains("drawknife") ? "swing"
                    : n.Contains("slice") || n.Contains("knife") || n.Contains("chop") ? "slice"
                    : n.Contains("cloth") ? "cloth"
                    : n.Contains("belt") ? "belt"
                    : n.Contains("leather") ? "leather"
                    : n.Contains("glass") ? "glass"
                    : n.Contains("bell") ? "bell"
                    : n.Contains("click") || n.Contains("latch") ? "click"
                    : n.Contains("coins") ? "coins"       // a handful of small brass: the gear spill
                    : n.Contains("impacttin") ? "tin"     // THE BUCKET (a tin pail): its own key now, and nothing plays it
                    : n.Contains("metalpot") ? "pot"      // a cooking pot: likewise
                    : n.Contains("metal") || n.Contains("plate") ? "metal"
                    : n.Contains("punch") || n.Contains("soft") ? "punch"
                    : n.Contains("wood") || n.Contains("plank") ? "wood"
                    : n.Contains("stone") || n.Contains("rock") || n.Contains("mining") || n.Contains("concrete") ? "stone"
                    : "any";
            Add(key, clip);
            // The Kenney impact packs by MATERIAL and WEIGHT as well ("impactPlate_heavy_003" → plate, plate_heavy), so the
            // destruction hooks can ask for a ceramic crack, a glass break or a light brass tink by name, never "some metal"
            if (n.StartsWith("impact"))
            {
                var parts = n.Substring(6).Split('_');
                string mat = parts[0];
                if (mat != key) Add(mat, clip);
                if (parts.Length > 2) Add(mat + "_" + parts[1], clip);
            }
            else if (n.Contains("metal-small")) Add("metal_light", clip); // the OGA small tinks: more gears for the scatter
        }
        Debug.Log($"GameFeel: {total} recorded clips in {samples.Count} buckets.");
    }

    static void Add(string key, AudioClip clip)
    {
        if (!samples.TryGetValue(key, out var list)) samples[key] = list = new List<AudioClip>();
        list.Add(clip);
    }

    /// A clip from a bucket, never the same one twice running: a run of cuts is a run of different cuts
    AudioClip Pick(string key)
    {
        if (!samples.TryGetValue(key, out var list) || list.Count == 0) return null;
        int i = Random.Range(0, list.Count);
        if (list.Count > 1 && lastPick.TryGetValue(key, out int last) && i == last) i = (i + 1) % list.Count;
        lastPick[key] = i;
        return list[i];
    }

    /// A recording from a bucket. Silent (and harmless) if the bucket is empty.
    void Play(string key, float volume, float pitch = 1f, bool crash = false)
    {
        source.volume = pitchSource.volume = impactSource.volume = GameSettings.SfxVolume; // the SOUND FX slider
        var clip = Pick(key);
        if (clip == null) return;
        if (crash) { impactSource.pitch = pitch * Random.Range(0.96f, 1.04f); impactSource.PlayOneShot(clip, volume); }
        else if (pitch != 1f) { pitchSource.pitch = pitch * Random.Range(0.97f, 1.03f); pitchSource.PlayOneShot(clip, volume); }
        else { source.pitch = Random.Range(0.94f, 1.06f); source.PlayOneShot(clip, volume); }
    }

    /// One PIECE of a unit (a gear, a shard, a spring) on the parts pool: every piece carries its own pitch, so a scatter
    /// is three different tinks and not one tink three times (the pitched one-shot source is shared; these are not)
    void Part(string key, float volume, float pitch)
    {
        var clip = Pick(key);
        if (clip == null) return;
        var s = partSources[partNext++ % partSources.Length];
        s.volume = GameSettings.SfxVolume;
        s.pitch = pitch * Random.Range(0.9f, 1.1f);
        s.PlayOneShot(clip, volume);
    }

    /// A SCATTER: `count` pieces a few dozen milliseconds apart (real time, so they fall through a hitstop), each a
    /// little quieter and a little different in pitch than the last
    void Scatter(string key, int count, float volume, float pitch)
    {
        Part(key, volume, pitch);
        if (count > 1 && isActiveAndEnabled) StartCoroutine(ScatterRest(key, count - 1, volume, pitch));
    }

    IEnumerator ScatterRest(string key, int count, float volume, float pitch)
    {
        for (int i = 0; i < count; i++)
        {
            yield return new WaitForSecondsRealtime(Random.Range(0.035f, 0.085f));
            volume *= 0.8f;
            Part(key, volume, pitch * Random.Range(0.92f, 1.12f));
        }
    }

    void OnEnable()
    {
        MovementEvents.MoveExecuted += OnMove;
        ResetDetector.ResetFired += OnReset;
    }

    void OnDisable()
    {
        MovementEvents.MoveExecuted -= OnMove;
        ResetDetector.ResetFired -= OnReset;
        if (Instance == this) Instance = null;
        if (Time.timeScale < 1f) Time.timeScale = 1f; // never strand a hitstop across scene loads
    }

    void Update()
    {
        bool flow = style != null && style.FlowState; // the flow loop is the score's (MusicDirector); this is the picture
        flowVis = Mathf.MoveTowards(flowVis, flow ? 1f : 0f, Time.unscaledDeltaTime * 2f);

        flashAlpha = Mathf.Max(0f, flashAlpha - Time.unscaledDeltaTime * 2.5f);
        hurtAlpha = Mathf.Max(0f, hurtAlpha - Time.unscaledDeltaTime * 1.8f);

        // Speed lines: the speed signal. They follow horizontal speed quickly
        // and every big move bursts them for a beat.
        float hSpeed = motor != null ? new Vector2(motor.Velocity.x, motor.Velocity.z).magnitude : 0f;
        float target = Mathf.InverseLerp(config.speedLinesStartSpeed, config.speedLinesFullSpeed, hSpeed)
                       * config.speedLinesMaxAlpha;
        speedLineSmooth = Mathf.MoveTowards(speedLineSmooth, target, Time.unscaledDeltaTime * 1.5f);
        speedLineBurst = Mathf.Max(0f, speedLineBurst - Time.unscaledDeltaTime * 1.6f);

        UpdateContinuousAudio(hSpeed);
    }

    // Footsteps and slide scrapes — recorded one-shots on a stride, no loops
    void UpdateContinuousAudio(float hSpeed)
    {
        if (motor == null || PauseMenu.IsPaused) return;
        float dt = Time.deltaTime;

        bool running = motor.Grounded && !motor.Sliding && hSpeed > 2f;
        if (running)
        {
            stepTimer -= dt;
            if (stepTimer <= 0f)
            {
                Play("step", Mathf.Clamp(0.3f + hSpeed * 0.015f, 0.3f, 0.6f));
                stepTimer = Mathf.Clamp(0.5f - hSpeed * 0.012f, 0.2f, 0.5f);
            }
        }
        else stepTimer = 0.05f;

        if (((motor.Sliding && motor.Grounded) || motor.WallRunning) && hSpeed > 3f) // slides and wall runs both scrape
        {
            slideTimer -= dt;
            if (slideTimer <= 0f)
            {
                Play("slide", Mathf.Clamp(hSpeed / 30f, 0.15f, 0.5f), 0.9f + Mathf.Clamp01(hSpeed / 30f) * 0.3f);
                slideTimer = Mathf.Clamp(0.22f - hSpeed * 0.004f, 0.09f, 0.22f);
            }
        }
        else slideTimer = 0f;
    }

    void OnGUI()
    {
        var full = new Rect(0f, 0f, Screen.width, Screen.height);
        float lines = Mathf.Min(0.95f, speedLineSmooth + speedLineBurst);
        if (lines > 0.004f && speedLineTex != null)
        {
            var tex = speedLineTex[(int)(Time.unscaledTime * 24f) % speedLineTex.Length];
            GUI.color = new Color(1f, 0.96f, 0.9f, lines);   // a warm white: the picture leans warm everywhere now
            GUI.DrawTexture(full, tex, ScaleMode.StretchToFill);
            // Tunnel: the edges fall away as speed climbs, the eye goes to the line
            GUI.color = new Color(0f, 0f, 0f, speedLineSmooth * 0.5f);
            GUI.DrawTexture(full, vignette, ScaleMode.StretchToFill);
        }
        if (flowVis > 0f && vignette != null)
        {
            float pulse = 0.3f + 0.08f * Mathf.Sin(Time.unscaledTime * 3f);
            GUI.color = new Color(1f, 0.8f, 0.3f, flowVis * pulse);
            GUI.DrawTexture(full, vignette, ScaleMode.StretchToFill);
        }
        if (hurtAlpha > 0f)
        {
            GUI.color = new Color(0.9f, 0.15f, 0.1f, hurtAlpha * 0.5f);
            GUI.DrawTexture(full, Texture2D.whiteTexture);
        }
        if (flashAlpha > 0f)
        {
            GUI.color = new Color(1f, 0.88f, 0.55f, flashAlpha);
            GUI.DrawTexture(full, Texture2D.whiteTexture);
        }
        GUI.color = Color.white;
    }

    void Burst(float amount) => speedLineBurst = Mathf.Max(speedLineBurst, amount);
    void Kick(float amount) { if (look != null) look.AddFovKick(amount); }
    void Shake(float amount) { if (look != null) look.AddShake(amount); }

    void OnMove(MoveType type, string detail)
    {
        switch (type)
        {
            // --- low effort: cloth ---
            case MoveType.Jump: Play("cloth", 0.35f); break;
            case MoveType.SlideJump: Play("cloth", 0.3f); break;
            case MoveType.DoubleJump: Play("cloth", 0.45f); Play("whoosh_soft", 0.25f); break;
            // --- silent: the rhythm is the feedback ---
            case MoveType.Bhop: Play("cloth", 0.35f, 1.05f); break;   // a kept hop is as present as a plain jump; the pitch carries the difference
            case MoveType.Slide: break;
            case MoveType.GlassBump: Shake(0.12f); break; // the pane thuds by itself
            // --- medium effort: soft swish ---
            case MoveType.PerfectBhop: Kick(4f); Burst(0.3f); Play("whoosh_soft", 0.4f, 1.1f); break;
            case MoveType.PerfectSlideJump: Kick(5f); Burst(0.3f); Play("whoosh_soft", 0.4f, 1.1f); break;
            case MoveType.EdgeBoost: Kick(10f); Burst(0.4f); Shake(0.3f); Play("whoosh_hard", 0.8f, 0.95f); Play("metal", 0.45f, 1f, true); break;   // the kick off a vertical corner: the big one
            case MoveType.WallRun: Kick(2f); Burst(0.15f); Play("whoosh_soft", 0.45f); Play("slide", 0.35f, 1.1f); break;
            case MoveType.WallJump: Kick(5f); Burst(0.3f); Shake(0.15f); Play("whoosh_hard", 0.65f); Play("punch", 0.3f); break;
            // --- high effort: hard swish, sometimes a thump ---
            case MoveType.Dash:
                if (detail != null && detail.StartsWith("REDIRECT")) { Kick(2f); Play("whoosh_hard", 0.45f, 1.1f); }   // a redirect at speed (flat, UP or DOWN): no burst, you kept what you had
                else { Kick(6f); Burst(0.3f); Shake(0.12f); Play("whoosh_hard", 0.7f); }
                break;
            case MoveType.WallKick: Kick(3f); Burst(0.15f); Shake(0.15f); Play("whoosh_hard", 0.5f); Play("punch", 0.35f); break;
            case MoveType.CornerBoost:   // the lip: speed into height, a lighter sound than the corner kick
                Kick(5f); Burst(0.25f); Play("whoosh_soft", 0.6f);
                if (detail != null && detail.StartsWith("WALL COMBO")) { Kick(4f); Burst(0.2f); Play("bell", 0.4f, 1.3f); } // the chain paid out
                break;
            case MoveType.SlamStorageJump: Kick(7f); Burst(0.4f); Shake(0.2f); Play("whoosh_hard", 0.8f, 0.9f); Play("sub", 0.5f, 1f, true); break;
            case MoveType.Slam: Play("whoosh_hard", 0.6f, 0.8f); break;
            case MoveType.GrappleYank: Kick(5f); Burst(0.25f); Shake(0.2f); Play("belt", 0.6f); Play("whoosh_hard", 0.5f); break;
            case MoveType.GlitchTech: Kick(8f); Burst(0.45f); Shake(0.25f); Play("whoosh_hard", 0.6f, 0.8f); break;
            // --- impacts ---
            case MoveType.Land:
            {
                float fall = 0f;
                float.TryParse(detail, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out fall);
                if (fall < 8f) break; // a step covers it
                float heavy = Mathf.Clamp01((fall - 8f) / 16f);
                Play("leather", 0.3f + 0.4f * heavy, 1f - 0.15f * heavy);
                if (heavy > 0.5f) { Play("stone", heavy * 0.7f, 1f, true); Shake(heavy * 0.45f); }
                break;
            }
            case MoveType.WallHit:
            {
                // The wall took your speed: the same voice as a hard landing, weighted by what it took
                float into = 0f;
                float.TryParse(detail, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out into);
                float heavy = Mathf.Clamp01((into - 5f) / 20f);
                Play("leather", 0.3f + 0.4f * heavy, 1f - 0.15f * heavy);
                if (heavy > 0.4f) { Play("stone", heavy * 0.6f, 1f, true); Shake(heavy * 0.35f); Kick(-3f * heavy); }
                break;
            }
            case MoveType.SlamLand:
                Kick(5f); Burst(0.3f); Shake(0.9f);
                Play("sub", 1f, 1f, true); Play("stone", 0.9f); Play("boom", 0.5f, 0.9f, true);
                break;
            case MoveType.Fist:
                FistHit(detail);
                break;
            case MoveType.Kick:
                KickHit(detail);
                break;
            case MoveType.Trick:
                TrickFeel(detail);
                break;
            case MoveType.ParryWhiff:
                Kick(-3f); Shake(0.12f);
                Play("cloth", 0.5f, 0.8f);   // a swish that met nothing
                break;
            case MoveType.GlassBreak:
                Kick(7f); Burst(0.45f); Shake(0.6f);
                flashAlpha = 0.3f;
                Play("glass", 1f, 1f, true);
                break;
            case MoveType.Parry:
            {
                // A real sword clash, pitched up as you chain them. A VOLLEY (the next connect inside the same guard)
                // is a lighter clash on top of the first: one freeze, one flash, not six
                bool volley = Time.time - lastParryTime <= 0.35f;
                parryCombo = Time.time - lastParryTime <= 2.5f ? parryCombo + 1 : 0;
                lastParryTime = Time.time;
                float chain = 1f + 0.05f * Mathf.Min(parryCombo, 4);
                Play("parry", volley ? 0.9f : 1.4f, chain + (volley ? 0.06f : 0f));
                if (!volley) Play("parry", 0.5f, chain, true); // the same clash again through the room
                Kick(volley ? 5f : 12f); Burst(volley ? 0.25f : 0.5f); Shake(volley ? 0.4f : 1f);
                if (!volley) flashAlpha = 0.85f;
                if (motor != null) ParryBurst(motor.LastParryPoint);
                if (!volley) StartCoroutine(Hitstop(config.parryHitstopDuration));
                break;
            }
        }
    }

    /// THE HANDS. A whiff is a swing of air. A punch is the weight (punch) and the leather of the glove, with the
    /// blade's short freeze. A tackle is a body through a body: the boom and the metal of a slam. A perfect tackle
    /// adds the parry's bell and the kick. The execute is the sword's, with the hands. Volumes sit with the sword's.
    void FistHit(string detail)
    {
        if (detail == "MISS")
        {
            Kick(2f); Shake(0.06f);
            Play("whoosh_soft", 0.5f, 0.9f);
            return;
        }
        if (look != null) look.AddRollKick((Fists.JabIndex == 0 ? -1f : 1f) * config.swordCameraRoll * 0.5f); // the shoulder turns into it
        if (detail == "EXECUTE")
        {
            Kick(10f); Burst(0.5f); Shake(1f);
            flashAlpha = 0.6f;
            Play("punch", 1f, 0.7f);
            Play("leather", 0.6f, 0.8f);
            Play("boom", 1f, 1f, true);
            Play("parry", 0.7f, 1.1f);
            Play("sub", 0.8f, 0.9f, true);
            MusicDirector.Duck(Mathf.Clamp01(config.slashDuck * 1.5f), config.slashDuckSeconds * 2f);
            SparkBurst(Fists.LastHitPoint, 30, CyanSparkMat());
            StartCoroutine(Hitstop(config.executeHitstop));
            return;
        }
        if (detail == "TACKLE" || detail == "PERFECT TACKLE")
        {
            bool perfect = detail == "PERFECT TACKLE";
            if (look != null) { look.AddRollKick(-config.swordCameraRoll * 1.6f); look.AddYawKick(-config.swordCameraYaw * 0.8f); } // the view leans into the shoulder
            Kick(5f); Burst(0.4f); Shake(perfect ? 0.8f : 0.6f);
            Play("boom", 0.8f, 1f, true);
            Play("metal", 0.5f, 1f, true);
            Play("punch", 0.6f, 0.8f);
            MusicDirector.Duck(config.slashDuck, config.slashDuckSeconds);
            if (perfect)
            {
                Kick(8f);
                flashAlpha = 0.35f;
                Play("bell", 0.5f, 1.2f); // the parry's bell: the timing paid
                SparkBurst(Fists.LastHitPoint, 20, GoldSparkMat());
                StartCoroutine(Hitstop(config.tackleHitstop));
            }
            else SparkBurst(Fists.LastHitPoint, 10, CyanSparkMat());
            return;
        }
        // A punch that landed: the weight, the glove's leather, and the brass bar's own tick on the plate; THE HOOK
        // sweeps (a hard swish under it) and a crowd met adds the weight of each
        if (Fists.LastStep == "HOOK")
        {
            Play("whoosh_hard", 0.6f, 0.9f);
            if (Fists.LastHookHits > 1) { Kick(2f); Burst(0.2f); Play("boom", 0.35f, 1.2f, true); }
        }
        Kick(4f); Shake(0.35f);
        Play("punch", 0.8f, Fists.JabIndex == 0 ? 0.9f : 0.8f); // the cross is the heavier hand
        Play("leather", 0.5f, 1f);
        Play("metal", 0.22f, 1.8f);
        Play("sub", 0.3f, 1.1f, true);
        MusicDirector.Duck(config.slashDuck, config.slashDuckSeconds);
        SparkBurst(Fists.LastHitPoint, 8, CyanSparkMat());
        StartCoroutine(Hitstop(config.swordHitstop));
    }

    /// THE KICK: the boot's thud (a punch) and the coat (cloth); the body it launches adds the boom of it leaving; the
    /// execute is the knuckles' own; a SPRINGBOARD is the soft whoosh of you leaving the body, and a PERFECT one (the
    /// apex) rings the parry's bell (the timing paid: gold)
    void KickHit(string detail)
    {
        if (detail == null) detail = "MISS";
        bool sprang = detail.StartsWith("SPRINGBOARD");
        bool perfect = detail.EndsWith("PERFECT");
        Play("cloth", 0.5f, 0.8f);
        Play("punch", 0.5f, 0.7f);
        if (look != null) look.AddRollKick(config.swordCameraRoll * 0.4f);
        if (detail == "EXECUTE") FistHit("EXECUTE");
        else if (detail == "MISS") { Kick(2f); Shake(0.1f); Play("whoosh_soft", 0.5f, 0.8f); return; }
        else
        {
            Kick(5f); Shake(0.5f); Burst(0.25f);
            Play("punch", 1f, 0.65f);
            Play("sub", 0.5f, 0.9f, true);
            if (detail == "LAUNCH") Play("boom", 0.5f, 1.1f, true);
            MusicDirector.Duck(config.slashDuck, config.slashDuckSeconds);
            SparkBurst(Fists.LastHitPoint, 12, CyanSparkMat());
            StartCoroutine(Hitstop(config.tackleHitstop));
        }
        if (!sprang) return;
        Kick(5f); Burst(0.35f);
        Play("whoosh_soft", 0.7f, 1.05f);
        Play("cloth", 0.4f, 1.1f);
        if (!perfect) return;
        Kick(5f);
        flashAlpha = Mathf.Max(flashAlpha, 0.3f);
        Play("bell", 0.55f, 1.25f);
        SparkBurst(Fists.LastHitPoint, 16, GoldSparkMat());
    }

    /// THE TRICKS (2026-10-23): the press is the hard whoosh of the turn and the coat's cloth as the arms tuck, a PERFECT
    /// press a touch brighter; the landing that pays rings the parry's bell with gold sparks at the feet (the timing
    /// paid: gold); a STUMBLE is the hard landing's leather and stone with the view knocked
    void TrickFeel(string detail)
    {
        if (detail == null) return;
        if (detail == "STUMBLE")
        {
            Shake(0.5f); Kick(-4f);
            Play("leather", 0.7f, 0.85f); Play("cloth", 0.5f, 0.75f); Play("stone", 0.45f, 0.9f, true);
            return;
        }
        if (detail.StartsWith("LANDED"))
        {
            Kick(5f); Burst(0.35f);
            flashAlpha = Mathf.Max(flashAlpha, 0.3f);
            Play("bell", 0.55f, 1.25f);
            Vector3 feet = motor != null ? motor.CurrPosition : look != null && look.cam != null ? look.cam.transform.position + Vector3.down * 1.5f : Vector3.zero;
            SparkBurst(feet + Vector3.up * 0.2f, 16, GoldSparkMat());
            return;
        }
        bool perfect = detail.StartsWith("PERFECT");
        Burst(0.25f);
        Play("whoosh_hard", 0.7f, perfect ? 1.1f : 0.95f);
        Play("cloth", 0.45f, 0.9f);
    }

    /// A tossed unit comes down: ONE crack of the enamel on the floor, the weight of it, and a couple of parts off it
    public void PlayUnitLand(Vector3 at) { Play("plate_heavy", 0.65f, 0.85f, true); Play("punch", 0.5f, 0.7f); Play("cloth", 0.3f, 0.7f); Scatter("metal_light", 2, 0.18f, 1.2f); Shake(0.2f); }

    void OnReset(string label, bool scores)
    {
        if (!scores) return; // the pip came back; the trick did not count twice, and the HUD says so
        resetCombo = Time.time - lastResetTime <= 4f ? resetCombo + 1 : 0;
        lastResetTime = Time.time;
        float pitch = (1f + 0.09f * Mathf.Min(resetCombo, 6)) * (style != null && style.FlowState ? 1.05f : 1f);
        Play("bell", 0.6f, pitch);
        Burst(0.3f);
    }

    // ---- Public one-shots for other systems --------------------------------
    public void PlayShatter() { Play("glass", 0.9f, 1f, true); Shake(0.3f); }
    public void PlayThunk() => Play("punch", 0.3f);
    /// IRON shrugging a hit (the Janitor's plate, the Stationmaster's ring, the level's machinery), scaled by how hard
    public void PlayClank(float v) => Play(v > 0.5f ? "metal_heavy" : "metal_medium", 0.12f + 0.35f * v, 0.9f + 0.2f * v);
    /// An ENAMEL SHELL knocked (a mannequin's bone on the floor, the Concierge's porcelain shrugging a slash): a ceramic plate, by how hard
    public void PlayShellClank(float v) => Play(v > 0.65f ? "plate_heavy" : v > 0.3f ? "plate_medium" : "plate_light", 0.1f + 0.35f * v, 0.9f + 0.2f * v);
    /// The GLASS HEAD knocked: the bell jar rings, and at speed it chips
    public void PlayGlassClank(float v) => Play(v > 0.5f ? "glass_medium" : "glass_light", 0.08f + 0.3f * v, 0.95f + 0.2f * v);
    public void PlayWhiff() => Play("whoosh_soft", 0.3f);
    public void PlayLockOn() => Play("metal", 0.3f, 1.6f);   // the gun's own click
    public void PlayParryWarn() => Play("cloth", 0.35f, 1.2f);   // something coming: a whip of air, no chirp
    /// A parry put health back: a ching that climbs with the chain
    public void PlayParryHeal(int chain) => Play("bell", 0.4f, 1f + 0.05f * Mathf.Min(chain, 8));
    public void PlayFeed(float k) { }   // the feed is on the bar; it makes no noise
    /// A kill, whatever did it: the punctuation. The body's own material is PlayUnitBreak, from the ragdoll
    public void PlayKill() { Kick(4f); Burst(0.35f); Shake(0.4f); Play("boom", 0.8f, 1.1f, true); }
    public void PlayHurt() { hurtAlpha = 0.9f; Kick(-6f); Shake(0.7f); Play("punch", 1f, 0.9f); Play("sub", 0.4f, 1f, true); }
    public void PlaySnap() => Play("belt", 0.4f);
    public void PlayEnemyShot() => Play("shot", 0.5f);
    /// The sniper's shot: instant, loud, and felt
    public void PlaySniperShot() { Play("shot", 0.9f, 0.55f); Play("boom", 0.35f, 1.4f, true); Play("metal", 0.4f, 1.6f, true); Kick(3f); Shake(0.35f); }
    public void PlayChaserStrike() => Play("whoosh_hard", 0.7f, 0.85f);
    public void PlayStagger() => PlayStagger(false);
    /// A windup parried: iron rings; a mannequin's enamel shell rattles instead, and a part comes loose
    public void PlayStagger(bool shell) { Play(shell ? "plate_medium" : "metal_medium", 0.45f, 0.75f, true); Play("cloth", 0.4f, 0.8f); if (shell) Part("metal_light", 0.15f, 1.3f); }
    public void PlayEnemySpawn() => Play("whoosh_hard", 0.4f, 0.7f);
    public void PlayProjectileImpact() => Play("projhit", 0.5f);
    public void PlayDowned() { hurtAlpha = 1f; Shake(0.8f); Play("sub", 1f, 0.8f, true); Play("punch", 0.8f, 0.8f); }
    public void PlayRespawn() { Play("whoosh_hard", 0.7f, 0.6f); Play("sub", 0.4f, 0.8f, true); }
    public void PlayPipRegen() { }   // the pip is on the HUD
    public void PlayFizzle() => Play("cloth", 0.35f, 0.7f);

    public void PlayRankUp(int rankIndex) { Burst(0.35f); Shake(0.15f); Play("bell", 0.7f, 1f + 0.08f * rankIndex); }
    public void PlayRankDown() => Play("leather", 0.5f, 0.7f);
    public void PlayFlowIn() { Kick(8f); Burst(0.5f); Shake(0.3f); Play("whoosh_hard", 0.7f, 0.8f); Play("sub", 0.5f, 0.8f, true); }
    public void PlayFlowOut() => Play("whoosh_soft", 0.5f, 0.7f);
    public void PlayFlowWarn() => Play("leather", 0.5f, 0.8f);

    /// Sounds that must survive a scene load (doors, finish)
    // The Concierge: big recorded hits, nothing electronic
    public void PlayBossSweep() { Play("whoosh_hard", 1f, 0.6f); Play("belt", 0.5f, 0.7f); }
    public void PlayBossStomp() { Play("boom", 1f, 0.75f, true); Play("sub", 0.9f, 0.8f, true); Play("stone", 0.7f, 0.8f); Shake(1.2f); Kick(6f); }
    public void PlayBossCrack() { Play("glass", 1f, 0.9f); Play("metal_heavy", 0.8f, 0.8f, true); Play("wood", 0.5f, 0.7f); Kick(5f); Shake(0.5f); }
    public void PlayBossStagger() { Play("metal", 0.9f, 0.6f, true); Play("cloth", 0.5f, 0.7f); Shake(0.4f); }
    public void PlayBossHit() { Play("slice", 1.3f, 0.85f); Play("boom", 0.9f, 0.9f, true); Play("glass", 0.7f); Shake(1f); Kick(10f); StartCoroutine(Hitstop(0.16f)); }
    public void PlayBossKill() { Play("boom", 1.2f, 0.6f, true); Play("sub", 1f, 0.7f, true); Play("glass", 1f, 0.8f); Play("metal", 0.9f, 0.7f, true); Shake(2f); Kick(14f); Burst(0.8f); flashAlpha = 0.9f; StartCoroutine(Hitstop(0.35f)); }
    public void PlayBossGreetWindup() { Play("belt", 0.7f, 0.6f); Play("leather", 0.5f, 0.7f); }
    public void PlayBossClap() { Play("whoosh_hard", 1f, 0.55f); Play("boom", 0.9f, 0.85f, true); Play("punch", 0.8f, 0.7f); Shake(0.9f); Kick(5f); }
    public void PlayBossReach() { Play("belt", 0.6f, 0.5f); Play("metal", 0.4f, 0.5f, true); }
    public void PlayBossSlam() { Play("boom", 1.1f, 0.7f, true); Play("stone", 0.9f, 0.75f); Play("sub", 0.9f, 0.85f, true); Shake(1.3f); Kick(7f); }
    public void PlayBossBell() { Play("bell", 1f, 1f); Play("bell", 0.6f, 1.3f); Play("boom", 0.8f, 0.7f, true); Play("sub", 0.8f, 0.8f, true); Shake(1f); Kick(5f); }
    /// The Concierge overloading: a tick and a warning that climb with the charge (k = 0..1)
    public void PlayBossOverload(float k) { Play("metal", 0.4f + 0.4f * k, 0.8f + 0.9f * k); if (k > 0.45f) Play("glass", 0.35f, 0.7f + 0.7f * k); }
    /// The Concierge coming apart: the big one, and the white-out
    public void PlayBossCollapse() { Play("boom", 1.2f, 0.5f, true); Play("boom", 1f, 0.85f, true); Play("sub", 1f, 0.55f, true); Play("glass", 1f, 0.7f); Play("metal", 1f, 0.5f, true); Play("bell", 0.8f, 0.6f); Shake(2.5f); Kick(16f); Burst(1f); flashAlpha = 1f; }
    /// A shower of sparks anywhere, gold or cyan, for scripted moments
    public void Sparks(Vector3 at, int count, bool cyan) => SparkBurst(at, count, cyan ? CyanSparkMat() : GoldSparkMat());
    /// The Concierge's PORCELAIN: a button popped off him (and BossOutro's) — a ceramic crack, the glass of it, the click
    public void PlayBossButton() { Play("click", 0.9f, 1.1f); Play("plate_heavy", 0.7f, 0.9f, true); Play("glass", 0.5f, 1.2f); Kick(3f); }
    /// The Stationmaster's IRON: a coat panel off the shield ring — heavy iron, never a plate or a pail
    public void PlayBossPlatePop() { Play("click", 0.7f, 0.9f); Play("metal_heavy", 0.8f, 0.7f, true); Play("metal_medium", 0.4f, 0.9f); Kick(3f); }
    // The plant: a jump pad firing, a cart coming, an arc going live
    public void PlayPad() { Play("whoosh_hard", 0.7f, 1.25f); Play("belt", 0.45f, 1.5f); Kick(3f); }
    public void PlayCartHorn() { Play("bell", 0.6f, 0.5f); Play("sub", 0.35f, 0.9f, true); }
    public void PlayArc() { Play("glass", 0.25f, 1.6f); Play("belt", 0.3f, 1.8f); }
    public void PlayBossCounter() { Play("parry", 1.2f, 0.9f); Play("metal", 0.6f, 0.8f, true); Shake(0.7f); Kick(6f); Burst(0.4f); }
    /// The boss tells: gold rings (answer it), red thuds (move). One sound each, so the ear learns them as fast as the eye
    public void PlayTellGold() { Play("parry", 0.6f, 1.7f); Play("cloth", 0.4f, 1.1f); }
    public void PlayTellRed() { Play("sub", 0.75f, 0.85f, true); Play("belt", 0.5f, 0.5f); }
    // Rooms: the wrap round to the start, and the clear
    public void PlayLoop() { Play("whoosh_hard", 0.8f, 0.6f); Play("cloth", 0.4f, 0.8f); Kick(-6f); Shake(0.3f); }
    public void PlayRoomClear() { Play("bell", 0.8f, 1.1f); Kick(5f); Burst(0.4f); }
    // The Janitor: heavy steps, a floor buffer, a charge, a slam, a hatch that vents
    public void PlayJanitorStep() { Play("step", 0.7f, 0.55f); Play("sub", 0.25f, 1f, true); }
    public void PlayJanitorSwing() => Play("whoosh_hard", 0.9f, 0.6f);
    public void PlayJanitorTell() { Play("belt", 0.7f, 0.5f); Play("leather", 0.5f, 0.6f); }
    public void PlayJanitorCharge() { Play("metal", 0.7f, 0.5f, true); Play("sub", 0.5f, 0.9f, true); }
    public void PlayJanitorSlam() { Play("boom", 1f, 0.75f, true); Play("stone", 0.9f, 0.8f); Play("sub", 0.8f, 0.85f, true); Shake(1f); Kick(4f); }
    public void PlayJanitorClang() { Play("metal", 1f, 0.5f, true); Play("boom", 0.6f, 0.8f, true); Shake(0.8f); }
    public void PlayJanitorVent() { Play("slide", 0.8f, 0.45f); Play("metal", 0.5f, 0.7f, true); }
    public void PlayJanitorHatch() => Play("metal", 0.8f, 0.9f, true);
    public void PlayJanitorCoreHit() { Play("slice", 1.2f, 0.8f); Play("boom", 0.8f, 0.9f, true); Play("glass", 0.6f); Shake(0.8f); Kick(8f); StartCoroutine(Hitstop(0.1f)); }
    /// A unit bisected by the cleave: a crash, a shower of cyan, a beat of hitstop
    public void PlayCleaveKill(Vector3 at) { Play("boom", 1.1f, 0.8f, true); Play("glass", 0.9f, 0.9f); Play("slice", 1f, 0.7f); Play("plate_heavy", 0.9f, 0.85f, true); Play("plate_medium", 0.5f, 1f); Play("bell", 0.25f, 1.2f); Scatter("metal_light", 3, 0.25f, 1.25f); Shake(0.9f); Kick(7f); Burst(0.5f); SparkBurst(at, 40, CyanSparkMat()); StartCoroutine(Hitstop(0.07f)); }

    /// THE BREAK: a clockwork mannequin coming apart (Enemy.SpawnRagdoll, every unit but the boss, whose outro has its
    /// own voice). The enamel CRACKS (ceramic plates, the heavy one through the crash channel), the glass head rings —
    /// or, on an execute, the bell jar BURSTS — and there is a touch of brass under it (a bell, low and small). A cut
    /// spills its gears from every severed joint (PlayGearSpill); a blunt kill spills them from the crack here. The
    /// iron machines of the Works (Janitor, platelayer, stoker) break as iron; the armoured mannequins (bouncer,
    /// housekeeping, sweeper) crack like the rest with iron under it. The cleave's own crack is PlayCleaveKill.
    public void PlayUnitBreak(EnemyKind kind, KillMethod method)
    {
        if (kind == EnemyKind.Boss) return;
        bool cut = method == KillMethod.SwordExecute || method == KillMethod.Cleave;
        bool big = method == KillMethod.Blast || method == KillMethod.SlamShockwave || method == KillMethod.Cleave;
        bool iron = kind == EnemyKind.Janitor || kind == EnemyKind.Borer || kind == EnemyKind.Kettler;
        bool armoured = kind == EnemyKind.Warden || kind == EnemyKind.DamageControl || kind == EnemyKind.Scourer;
        if (iron)
        {
            Play("metal_heavy", big ? 0.85f : 0.7f, 0.7f, true);
            Play("metal_medium", 0.4f, 0.9f);
            Scatter("metal_light", 2, 0.2f, 1.1f);
            return;
        }
        if (method != KillMethod.Cleave)
        {
            Play("plate_heavy", big ? 0.85f : 0.7f, big ? 0.85f : 0.95f, true);
            Play("plate_medium", 0.4f, 1.05f);
        }
        if (method == KillMethod.SwordExecute) Play("glass_heavy", 0.8f, 0.95f, true);     // the head clean off: the jar bursts
        else Play(big ? "glass_medium" : "glass_light", big ? 0.4f : 0.22f, 1.1f);          // the head knocked: the jar rings
        Play("bell", 0.16f, 1.4f);                                                            // the brass, a hint
        if (armoured) Play("metal_medium", 0.4f, 0.8f, true);
        if (kind == EnemyKind.Blaster || kind == EnemyKind.Rigger || kind == EnemyKind.Ocular) Play("wood_light", 0.3f, 1.1f); // the detonator box, the flag poles, the torch
        if (!cut) PlayGearSpill();
    }

    /// GEARS AND SPRINGS out of a cut (EnemyMannequin.SpillGears): one soft scatter per spill — a handful of small brass
    /// on the floor and two tinks — never one per piece, and a cut through several joints spills once (0.12 s apart at most)
    public void PlayGearSpill()
    {
        if (Time.unscaledTime - lastSpillAt < 0.12f) return;
        lastSpillAt = Time.unscaledTime;
        Part("coins", 0.3f, 1.3f);
        Scatter("metal_light", 2, 0.18f, 1.3f);
    }
    /// Halcyon Works: the Borer lining you up, charging and jamming; the Rigger's swing and its ball coming home; the Blaster's throw, a charge arming, the blast
    public void PlayBorerTell() { Play("belt", 0.8f, 0.6f); Play("leather", 0.5f, 0.55f); }
    public void PlayBorerCharge() { Play("metal", 0.7f, 0.45f, true); Play("sub", 0.6f, 0.8f, true); Shake(0.3f); }
    public void PlayBorerJam() { Play("metal", 1f, 0.5f, true); Play("glass", 0.5f, 0.6f); Play("cloth", 0.5f, 0.7f); Shake(0.9f); Kick(4f); }
    public void PlayRiggerSwing() { Play("whoosh_hard", 1f, 0.5f); Play("belt", 0.5f, 0.7f); }
    public void PlayRiggerHome() { Play("boom", 1f, 0.8f, true); Play("metal", 0.9f, 0.6f, true); Shake(0.8f); Kick(6f); }
    public void PlayBlasterThrow() => Play("whoosh_soft", 0.6f, 0.8f);
    public void PlayChargeArm() { Play("click", 0.6f, 0.9f); Play("metal", 0.3f, 1.2f); }
    public void PlayChargeBlast() { Play("boom", 1.1f, 0.7f, true); Play("sub", 0.9f, 0.7f, true); Play("glass", 0.6f, 0.8f); Shake(1.2f); Kick(7f); Burst(0.4f); }
    /// A blast out in the district: `near` is 1 on top of you and 0 at the edge of earshot. Same clips as a charge, scaled.
    public void PlayBlastAt(float near)
    {
        if (near <= 0.02f) return;
        Play("boom", 1.1f * near, 0.7f, true);
        Play("sub", 0.9f * near, 0.7f, true);
        if (near > 0.5f) Play("glass", 0.6f * near, 0.8f);
        Shake(1.2f * near * near);
        Kick(7f * near * near);
        if (near > 0.6f) Burst(0.4f * near);
    }
    /// The whip's crack as it takes hold
    public void PlayWhipCrack() { Play("leather", 0.8f, 1.15f); Play("belt", 0.5f, 1.35f); Play("whoosh_hard", 0.35f, 1.3f); }
    public void PlayPortal() => PlayPersistent(Pick("portal"), 0.8f);
    public void PlayFinish() { PlayPersistent(Pick("finish"), 0.9f); PlayPersistent(Pick("rankup"), 0.7f); }

    static void PlayPersistent(AudioClip clip, float volume)
    {
        if (clip == null) return;
        var go = new GameObject("PersistentSfx");
        DontDestroyOnLoad(go);
        var s = go.AddComponent<AudioSource>();
        s.spatialBlend = 0f;
        s.volume = GameSettings.SfxVolume;
        s.PlayOneShot(clip, volume);
        Destroy(go, clip.length + 0.1f);
    }

    static void ParryBurst(Vector3 at) => SparkBurst(at + Vector3.up * 0.6f, 26, GoldSparkMat());

    static Material GoldSparkMat()
    {
        if (sparkMat == null)
        {
            sparkMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            sparkMat.SetColor("_BaseColor", new Color(1f, 0.8f, 0.3f));
        }
        return sparkMat;
    }

    static Material CyanSparkMat()
    {
        if (cyanSparkMat == null)
        {
            cyanSparkMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            cyanSparkMat.SetColor("_BaseColor", new Color(0.4f, 1.8f, 2.1f));
        }
        return cyanSparkMat;
    }

    static Material amberSparkMat;
    /// Amber: your own light (the anchor's lamp, the maul's cap) — not the parry's gold
    static Material AmberSparkMat()
    {
        if (amberSparkMat == null)
        {
            amberSparkMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            amberSparkMat.SetColor("_BaseColor", new Color(1.6f, 0.75f, 0.2f));
        }
        return amberSparkMat;
    }

    /// Physical spark shower — parries (gold) and sword cuts (cyan) share it
    static void SparkBurst(Vector3 at, int count, Material mat)
    {
        for (int i = 0; i < count; i++)
        {
            var spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spark.name = "Spark";
            spark.layer = 2;
            Destroy(spark.GetComponent<Collider>());
            Vector3 dir = Random.onUnitSphere;
            if (dir.y < 0f) dir.y = -dir.y * 0.3f;
            dir.Normalize();
            spark.transform.position = at + dir * 0.3f;
            spark.transform.rotation = Quaternion.LookRotation(dir);
            spark.transform.localScale = new Vector3(0.07f, 0.07f, Random.Range(0.4f, 1f));
            spark.GetComponent<MeshRenderer>().sharedMaterial = mat;
            var rb = spark.AddComponent<Rigidbody>();
            rb.mass = 0.02f;
            rb.linearVelocity = dir * Random.Range(7f, 15f) + Vector3.up * 2f;
            spark.AddComponent<ShardFade>();
        }
    }

    IEnumerator Hitstop(float duration)
    {
        if (Time.timeScale < Practice.TimeScale) yield break;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = PauseMenu.IsPaused ? 0f : Practice.TimeScale;
    }

    void BuildVignette()
    {
        const int size = 128;
        vignette = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x / (float)(size - 1)) * 2f - 1f;
                float dy = (y / (float)(size - 1)) * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.62f) / 0.38f));
                vignette.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        vignette.Apply();
    }

    /// Radial anime speed lines: three frames flicker between each other, lines
    /// start well outside the crosshair and sharpen toward the screen edge
    void BuildSpeedLines()
    {
        const int size = 512;
        const float half = size / 2f;
        var rng = new System.Random(777);
        speedLineTex = new Texture2D[3];
        for (int v = 0; v < speedLineTex.Length; v++)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            var clear = new Color32(255, 255, 255, 0);
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            for (int s = 0; s < 72; s++)
            {
                float ang = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                float cos = Mathf.Cos(ang), sin = Mathf.Sin(ang);
                float r0 = 130f + (float)rng.NextDouble() * 100f;
                float r1 = Mathf.Min(362f, r0 + 90f + (float)rng.NextDouble() * 80f);
                float strength = 0.55f + (float)rng.NextDouble() * 0.45f;
                for (float r = r0; r <= r1; r += 1f)
                {
                    float t = (r - r0) / (r1 - r0);
                    byte a = (byte)(255f * t * t * strength);
                    Plot(px, size, Mathf.RoundToInt(half + cos * r), Mathf.RoundToInt(half + sin * r), a);
                    Plot(px, size, Mathf.RoundToInt(half + cos * r - sin), Mathf.RoundToInt(half + sin * r + cos), a);
                    Plot(px, size, Mathf.RoundToInt(half + cos * r + sin), Mathf.RoundToInt(half + sin * r - cos), (byte)(a / 2));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            speedLineTex[v] = tex;
        }
    }

    static void Plot(Color32[] px, int size, int x, int y, byte a)
    {
        if (x < 0 || x >= size || y < 0 || y >= size) return;
        int i = y * size + x;
        if (a > px[i].a) px[i] = new Color32(255, 255, 255, a);
    }
}

