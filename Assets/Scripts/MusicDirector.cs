using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// THE SCORE. One player that outlives every scene and follows the game's state
/// instead of being told what to play. The score is a REMIX (MusicRemix): a jazz
/// LEAD — a public-domain 78 from Resources/Music/jazz, or the procedural band —
/// with a synthesised EDM BED under it on the lead's own tracked beats:
///   menu     — the lobby's slow side through a gramophone band-pass (300–5000 Hz),
///              a soft house kick at −12 dB, vinyl crackle
///   explore  — the floor's side, the band-pass opening, four on the floor under it
///   combat   — the bed swaps ON THE BAR LINE to double time, the filters open
///   flow     — a riser, a silent beat, and a drum-and-bass break at twice the pulse;
///              the lead lifted a few cents and pumped hard (MusicPump)
///   boss     — the Concierge's cues (BossMusic) on the boss pool's side: big drops
///              (riser → silence → bed) into the drop and the climax, half time in the
///              breakdown, the kill cuts the bed dead with one hit
///   calm     — the report card and the walk after the kill: the slow side, dry
/// Leads live in POOLS (menu, city, under, office, boss); a floor keeps its own
/// sides, a level starts on its own one, and a change of pool is a crossfade. Inside
/// a pool the lead keeps playing while beds swap on bar lines; the next side is queued
/// sample-accurately at the end of the current one. Lead and bed clips are armed on
/// one DSP clock; a bed that is not rendered yet joins on the next bar line. The MUSIC
/// slider, the duck under a cut and the pause muffle ride on top of everything.
/// </summary>
public class MusicDirector : MonoBehaviour
{
    public static MusicDirector Instance { get; private set; }
    /// A cutscene holds the score down to the calm set while it plays
    public static bool Hush;
    /// False hands the boss fight back to BossMusic's own bed-only synth (never in normal play)
    public static bool Enabled = true;

    public enum Mood { Silent, Menu, Explore, Combat, Flow, BossIntro, Boss, BossBreak, BossClimax, Calm }

    /// The score level under the mix; the MUSIC slider (GameSettings.MusicVolume) rides on top
    public static float Master = 0.8f;

    const float FadeSeconds = 1.2f;
    const float Lookahead = 2.5f;   // the next side is queued this far before the clip's end
    const float FoeRange = 40f;     // a unit awake within this is a fight (course-grammar: units engaged within ~40 m; no room says so any more)
    const float ActionMemory = 5f;  // seconds a kill or a hit keeps the fight alive

    public Mood Current => mood;
    public MusicRemix.Song Song => live >= 0 ? leadSong[live] : null;
    public string PlayingName => Song != null ? Song.name + "/" + bedKind : "";

    // ============================================================ the player

    readonly AudioSource[] lead = new AudioSource[2];
    readonly AudioHighPassFilter[] leadHp = new AudioHighPassFilter[2];
    readonly AudioLowPassFilter[] leadLp = new AudioLowPassFilter[2];
    readonly MusicPump[] pump = new MusicPump[2];
    readonly MusicRemix.Song[] leadSong = new MusicRemix.Song[2];
    readonly double[] leadStart = new double[2];
    readonly float[] fade = new float[2], fadeTarget = new float[2];

    const int BedSources = 4; // live, a pending switch, one fading out with an old lead, one for the queued successor
    readonly AudioSource[] bed = new AudioSource[BedSources];
    readonly MusicRemix.Song[] bedSong = new MusicRemix.Song[BedSources];
    readonly double[] bedStart = new double[BedSources];
    readonly MusicRemix.Bed[] bedKindOf = new MusicRemix.Bed[BedSources];
    readonly int[] bedOwner = new int[BedSources];
    AudioLowPassFilter bedLp;
    AudioSource riser, impact, crackle;

    int live = -1;                 // the lead source carrying the score now
    double songEndDsp;
    bool pending; int pendingIndex; double pendingDsp; int pendingBed = -1; // the queued successor
    string set = "";               // the pool in play
    List<MusicRemix.Song> pool; int poolIndex;
    MusicRemix.Song target;        // the side the score is heading to (a change of pool, or a gap)
    bool cutNext;                  // the next change of side drops the old one dead: the kill

    int bedLive = -1;
    MusicRemix.Bed bedKind = MusicRemix.Bed.None, bedWant = MusicRemix.Bed.None;
    bool bedPending; int bedPendingIndex = -1; double bedPendingDsp; MusicRemix.Bed bedPendingKind;
    double lastBedSwitchDsp = -100.0;

    float leadVol, bedVol, crackleVol;
    float leadLow = 22000f, leadHigh = 10f, bedLow = 22000f;
    float cents;
    Mood mood = Mood.Silent;

    // The world, as far as the score is concerned
    bool menuScene;
    string levelSet = "city";
    int levelNumber;
    Mood bossMood = Mood.Silent;
    float nextScan;
    bool nearFoe;
    readonly List<(MusicRemix.Song song, MusicRemix.Bed[] beds)> warm = new List<(MusicRemix.Song, MusicRemix.Bed[])>();

    static readonly MusicRemix.Bed[] LevelBeds = { MusicRemix.Bed.Hard, MusicRemix.Bed.Breaks };
    static readonly MusicRemix.Bed[] BossBeds = { MusicRemix.Bed.Boss, MusicRemix.Bed.BossBreak, MusicRemix.Bed.BossClimax };
    static readonly MusicRemix.Bed[] MenuBeds = new MusicRemix.Bed[0]; // the lobby is the lead alone

    /// THE GRAMOPHONE: the 78s in Resources/Music/jazz as a rare, low, band-passed layer in the lobby and the calm. Off by default
    public static bool Gramophone = false;
    AudioSource grama; MusicPump gramaPump; MusicRemix.Song gramaSong; float gramaVol; bool gramaPlayedThisScene; static int gramaIndex;

    public static MusicDirector Ensure()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("MusicDirector");
        DontDestroyOnLoad(go);
        go.AddComponent<MusicDirector>(); // Awake sets Instance
        return Instance;
    }

    // The score exists in every scene, the menu included: GameBootstrap.Awake calls Ensure() before it branches on the
    // scene kind (an engine-level [RuntimeInitializeOnLoadMethod] would do it too, but Tools/compile.ps1 cannot see
    // its base attribute's assembly, and that script is the only check this project has)

    /// The F3 readout: "MUSIC  <side> · <mood> · lead OK/RAW/BAND · bed <kind>/none" (DebugHUD prints it)
    public static string Live => Instance != null ? Instance.LiveLine() : "MUSIC  no director";

    string LiveLine()
    {
        var s = Song;
        string side = s != null ? s.name : (target != null ? "(" + target.name + " " + (target.state < 0 ? "failed" : target.state == 1 ? "loading" : target.state == 2 ? "measuring" : "ready") + ")" : "none");
        string leadState = s == null ? "none" : s.LeadState;
        string bedState = s != null && s.raw ? "none (raw lead)" : bedLive >= 0 && bed[bedLive].clip != null ? bedKind.ToString() : bedLive >= 0 ? bedKind + " (rendering)" : "none";
        float vol = Master * GameSettings.MusicVolume;
        string extra = vol < 0.05f ? " · VOLUME " + vol.ToString("0.00") : "";
        if (grama != null && grama.isPlaying && gramaSong != null) extra += " · gramophone " + gramaSong.name;
        if (faulted) extra += " · UPDATE FAULTED (see log)";
        return "MUSIC  " + side + " · " + mood + " · lead " + leadState + " · bed " + bedState + extra;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        for (int i = 0; i < 2; i++)
        {
            var host = new GameObject(i == 0 ? "Lead A" : "Lead B");
            host.transform.SetParent(transform, false);
            lead[i] = NewSource(host);
            leadHp[i] = host.AddComponent<AudioHighPassFilter>();   // the gramophone's floor
            leadHp[i].cutoffFrequency = leadHigh;
            leadHp[i].highpassResonanceQ = 1f;
            leadLp[i] = host.AddComponent<AudioLowPassFilter>();    // the gramophone's ceiling, and the wall between fights
            leadLp[i].cutoffFrequency = leadLow;
            leadLp[i].lowpassResonanceQ = 1f;
            pump[i] = host.AddComponent<MusicPump>();               // the side-chain, the gates, the lift
            fadeTarget[i] = 1f;
        }
        var bedHost = new GameObject("Bed");
        bedHost.transform.SetParent(transform, false);
        for (int i = 0; i < BedSources; i++) bed[i] = NewSource(bedHost);
        bedLp = bedHost.AddComponent<AudioLowPassFilter>();
        bedLp.cutoffFrequency = bedLow;
        bedLp.lowpassResonanceQ = 1f;
        var fx = new GameObject("Fx");
        fx.transform.SetParent(transform, false);
        riser = NewSource(fx);
        impact = NewSource(fx);
        crackle = NewSource(fx);
        crackle.loop = true;
        var gramaHost = new GameObject("Gramophone"); // the next room: a 78 through a narrow horn, brought up by its own pump
        gramaHost.transform.SetParent(transform, false);
        grama = NewSource(gramaHost);
        var gh = gramaHost.AddComponent<AudioHighPassFilter>(); gh.cutoffFrequency = 500f; gh.highpassResonanceQ = 1f;
        var gl = gramaHost.AddComponent<AudioLowPassFilter>(); gl.cutoffFrequency = 3200f; gl.lowpassResonanceQ = 1f;
        gramaPump = gramaHost.AddComponent<MusicPump>();
        MusicRemix.Init();
        MusicRemix.RequestCrackle();
        SceneManager.sceneLoaded += OnSceneLoaded;
        ReadScene(SceneManager.GetActiveScene().name);
    }

    static AudioSource NewSource(GameObject host)
    {
        var s = host.AddComponent<AudioSource>();
        s.spatialBlend = 0f;
        s.playOnAwake = false;
        s.loop = false;
        s.volume = 0f;
        s.ignoreListenerPause = true;
        return s;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ReadScene(scene.name);

    void ReadScene(string scene)
    {
        menuScene = scene == "MainMenu";
        levelSet = SetFor(scene);
        levelNumber = NumberOf(scene);
        bossMood = Mood.Silent;
        nearFoe = false;
        nextScan = 0f;
        keep.Clear();
        gramaPlayedThisScene = false;
        if (menuScene) Warm("menu", MenuBeds);
        else { Warm(levelSet, LevelBeds); Warm("menu", MenuBeds); } // the floor's side and its beds render now, before the first room
    }

    /// Sides warmed for this scene: never released while the scene lasts
    readonly HashSet<MusicRemix.Song> keep = new HashSet<MusicRemix.Song>();

    /// Start a pool's side for this scene rendering, with the beds it will need
    void Warm(string poolName, MusicRemix.Bed[] beds)
    {
        var p = MusicRemix.Pool(poolName);
        if (p == null || p.Count == 0) return;
        var s = p[StartIndex(poolName, p)];
        MusicRemix.Prepare(s);
        keep.Add(s);
        warm.Add((s, beds));
    }

    /// The boss pool's side and its beds render now, before the cutscene ends (BossMusic.Awake)
    public void PrepareBoss() => Warm("boss", BossBeds);

    /// Which pool scores which floor: the city for the towers, the underbelly below, the office for the spire and HQ
    static string SetFor(string scene) => scene switch
    {
        "Level3" => "under",
        "Level4" => "office",
        "Tutorial" => "city",
        "Level5" => "office",
        "Level6" => "under",
        "Level7" => "under",
        "Level8" => "under",
        "Level9" => "under",
        "Level10" => "under",
        "Level11" => "under",
        "Level12" => "city",
        "Level13" => "city",
        _ => "city",
    };

    static int NumberOf(string scene)
    {
        int n = 0; bool any = false;
        foreach (char c in scene) if (char.IsDigit(c)) { n = n * 10 + (c - '0'); any = true; }
        if (any) return n;
        unchecked { int h = 7; foreach (char c in scene) h = h * 31 + c; return Math.Abs(h); }
    }

    /// A level keeps its side: where a scene starts in a pool
    int StartIndex(string poolName, List<MusicRemix.Song> p) => poolName == "menu" ? 0 : levelNumber % Math.Max(1, p.Count);

    // ============================================================ cues

    /// The fight's cues, from BossMusic: they own the score until the scene changes
    public void BossCue(BossMusic.Section s)
    {
        bossMood = s switch
        {
            BossMusic.Section.Approach => Mood.BossIntro,
            BossMusic.Section.Breakdown => Mood.BossBreak,
            BossMusic.Section.Climax => Mood.BossClimax,
            BossMusic.Section.Tail => Mood.Calm,
            _ => Mood.Boss,
        };
        if (s == BossMusic.Section.Tail) cutNext = true;
    }

    public void BossStop() => bossMood = Mood.Silent;

    public bool BossPlaying => bossMood == Mood.BossIntro || bossMood == Mood.Boss || bossMood == Mood.BossBreak || bossMood == Mood.BossClimax;

    // ============================================================ the frame

    bool faulted;

    void Update()
    {
        try { Apply(Decide()); Tick(); }
        catch (Exception e)
        {
            if (!faulted) Debug.LogError("MUSIC: the director's update threw (logged once, it keeps running): " + e);
            faulted = true;
        }
    }

    Mood Decide()
    {
        if (menuScene) return Mood.Menu;
        if (LevelResults.Active || Hush) return Mood.Calm;
        if (bossMood != Mood.Silent) return bossMood;
        var director = CombatDirector.Instance;
        if (director != null && director.style != null && director.style.FlowState) return Mood.Flow;
        return InCombat(director) ? Mood.Combat : Mood.Explore;
    }

    /// A fight: something just happened, or a unit that fights back is awake within forty metres
    bool InCombat(CombatDirector director)
    {
        if (director != null && Time.time - director.LastActionTime < ActionMemory) return true;
        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + 0.5f;
            nearFoe = FoeNear(director);
        }
        return nearFoe;
    }

    static bool FoeNear(CombatDirector director)
    {
        if (director == null || director.motor == null) return false;
        return CourseFlow.UnitNear(director.motor.CurrPosition, FoeRange);   // alive, awake, not a dummy, not a boss
    }

    /// Which pool a mood plays from
    string PoolFor(Mood m) => m switch
    {
        Mood.Menu => "menu",
        Mood.Calm => "menu",
        Mood.BossIntro => "boss",
        Mood.Boss => "boss",
        Mood.BossBreak => "boss",
        Mood.BossClimax => "boss",
        Mood.Silent => null,
        _ => levelSet,
    };

    /// The bed each mood rides: the hotel room alone until a fight, the beat hardening on the bar, double time in flow
    static MusicRemix.Bed BedFor(Mood m) => m switch
    {
        Mood.Combat => MusicRemix.Bed.Hard,
        Mood.Flow => MusicRemix.Bed.Breaks,
        Mood.Boss => MusicRemix.Bed.Boss,
        Mood.BossBreak => MusicRemix.Bed.BossBreak,
        Mood.BossClimax => MusicRemix.Bed.BossClimax,
        _ => MusicRemix.Bed.None,
    };

    void Apply(Mood m)
    {
        mood = m;
        bool rec = Song != null && Song.recorded;
        float openHigh = rec ? 120f : 10f, openLow = rec ? 7000f : 22000f; // a measured 78 keeps its hiss and its rumble out
        float lv = 0f, bv = 0f, cv = 0f, lo = openLow, hi = openHigh, blo = 22000f, ct = 0f;
        switch (m)
        {
            // THE RAMP, in the hotel room's idiom: the slow groove nearly dry between fights; on the bar the beat hardens
            // (the kick and snare heavy, the 808 under the bass, rolls, the brass stabs, the strings swelling, the low-pass
            // opening, the lead side-chained); flow is the same harmony under double-time breaks, the lead lifted a touch
            case Mood.Menu: lv = 0.85f; cv = 0.35f; hi = 300f; lo = 5000f; break;                             // the lead alone through the gramophone, with crackle
            case Mood.Explore: lv = 0.85f; lo = 9000f; break;                                                  // nearly dry, a little of the room's ceiling kept
            case Mood.Combat: lv = 0.72f; bv = 0.82f; break;                                                   // the beat hardens on the bar
            case Mood.Flow: lv = 0.72f; bv = 0.88f; ct = 6f; break;                                            // double time, lifted
            case Mood.BossIntro: lv = 0.75f; cv = 0.25f; hi = 250f; lo = 6000f; break;                          // the cutscene: the boss side alone, through the horn
            case Mood.Boss: lv = 0.72f; bv = 0.88f; break;                                                     // the strings cinematic, the hardest beat
            case Mood.BossBreak: lv = 0.7f; bv = 0.5f; lo = 3000f; blo = 2500f; break;                          // the plates come off
            case Mood.BossClimax: lv = 0.72f; bv = 0.95f; ct = 5f; break;
            case Mood.Calm: lv = 0.7f; cv = 0.3f; hi = 250f; lo = 6000f; break;
        }
        if (rec) cv *= 0.4f; // the transfers carry their own crackle
        if (PauseMenu.IsPaused) { lo = Mathf.Min(lo, 500f); lv *= 0.5f; blo = Mathf.Min(blo, 500f); bv *= 0.5f; }

        float dt = Time.unscaledDeltaTime;
        leadVol = Mathf.MoveTowards(leadVol, lv, dt * 1.5f);
        bedVol = Mathf.MoveTowards(bedVol, bv, dt * 1.5f);
        crackleVol = Mathf.MoveTowards(crackleVol, cv, dt * 1.5f);
        leadLow = Glide(leadLow, lo, dt);
        leadHigh = Glide(leadHigh, hi, dt, true);
        bedLow = Glide(bedLow, blo, dt);
        cents = Mathf.MoveTowards(cents, ct, dt * 20f);
        for (int i = 0; i < 2; i++) { leadLp[i].cutoffFrequency = leadLow; leadHp[i].cutoffFrequency = leadHigh; pump[i].SetCents(i == live ? cents : 0f); }
        bedLp.cutoffFrequency = bedLow;
        bedWant = BedFor(m);
    }

    /// A filter opens fast (that is the drop) and closes slow, in log frequency
    static float Glide(float from, float to, float dt, bool highPass = false)
    {
        bool opening = highPass ? to < from : to > from;
        float rate = opening ? 7f : 2.5f;
        return Mathf.Exp(Mathf.Lerp(Mathf.Log(Mathf.Max(1f, from)), Mathf.Log(Mathf.Max(1f, to)), 1f - Mathf.Exp(-dt * rate)));
    }

    // ============================================================ the tick

    void Tick()
    {
        double now = AudioSettings.dspTime;
        float dt = Time.unscaledDeltaTime;
        float duckWant = Time.unscaledTime < duckUntil ? 1f - duckDepth : 1f;
        duckMul = Mathf.MoveTowards(duckMul, duckWant, dt * (duckWant < duckMul ? 25f : 3f));

        MusicRemix.Poll();
        if (crackle.clip == null && MusicRemix.Crackle != null) { crackle.clip = MusicRemix.Crackle; crackle.Play(); }
        GramophoneTick(dt);
        for (int i = warm.Count - 1; i >= 0; i--)
        {
            var (s, beds) = warm[i];
            if (s.state < 0) { warm.RemoveAt(i); continue; }
            if (!s.Ready) continue;
            foreach (var b in beds) MusicRemix.RequestBed(s, b);
            MusicRemix.RequestShots(s);
            warm.RemoveAt(i);
        }
        if (cutNext && mood == Mood.Calm) { CutBed(now); cutNext = false; } // the kill: everything stops on one hit, the calm side fades in over its tail

        // ---- which side: the mood's pool, and the next one when this one ends
        string wantSet = PoolFor(mood);
        if (wantSet == null) { if (live >= 0) FadeOutAll(); }
        else
        {
            if (wantSet != set)
            {
                set = wantSet;
                pool = MusicRemix.Pool(set);
                poolIndex = StartIndex(set, pool);
                target = pool[poolIndex];
                MusicRemix.Prepare(target);
                if (pending) CancelPending();
            }
            if (target != null && target != Song)
            {
                if (target.Ready) Crossfade(target, now);
                else if (target.state < 0) { pool = MusicRemix.Pool(set); poolIndex = StartIndex(set, pool); target = pool[poolIndex]; MusicRemix.Prepare(target); } // a side that failed is out of the pool now (the band, if nothing is left)
            }
            else if (live >= 0 && !pending && songEndDsp - now < Lookahead) QueueSuccessor(now);
            else if (live >= 0 && !pending && now >= songEndDsp) { target = NextInPool(); MusicRemix.Prepare(target); } // ended before the next was ready: it comes in with a fade
        }

        // ---- the queued successor takes over at its time; the old lead ended on the same sample
        if (pending && now >= pendingDsp)
        {
            int old = live;
            live = pendingIndex;
            pending = false;
            poolIndex = (poolIndex + 1) % Math.Max(1, pool != null ? pool.Count : 1);
            target = leadSong[live];
            songEndDsp = pendingDsp + leadSong[live].Length;
            fade[live] = 1f; fadeTarget[live] = 1f;
            if (old >= 0 && old != live) StopLead(old);
            int oldBed = bedLive;
            bedLive = pendingBed; pendingBed = -1;
            bedKind = bedLive >= 0 ? bedKindOf[bedLive] : MusicRemix.Bed.None;
            if (oldBed >= 0 && oldBed != bedLive) StopBed(oldBed);
            if (bedPending) { if (bedPendingIndex >= 0 && bedPendingIndex != bedLive) StopBed(bedPendingIndex); bedPending = false; bedPendingIndex = -1; }
            pump[live].SetGrid(MusicRemix.PumpGrid(leadSong[live], bedKind, leadStart[live]));
            AfterStart();
        }

        // ---- the bed: swap on the bar line, with a drop into flow and the boss sections
        if (live >= 0)
        {
            if (bedPending && now >= bedPendingDsp)
            {
                int old = bedLive;
                bedLive = bedPendingKind == MusicRemix.Bed.None ? -1 : bedPendingIndex;
                bedKind = bedPendingKind;
                bedPending = false; bedPendingIndex = -1;
                if (old >= 0 && old != bedLive) StopBed(old);
                pump[live].SetGrid(MusicRemix.PumpGrid(leadSong[live], bedKind, leadStart[live]));
            }
            if (bedPending && bedPendingKind != bedWant && bedPendingDsp - now > 0.15) CancelBedPending(now); // the mood turned before the switch landed
            if (!bedPending && bedWant != bedKind)
            {
                double bar = leadSong[live].BeatSeconds * 4.0;
                if (now - lastBedSwitchDsp >= bar * 0.5) ScheduleBed(now); // never two switches within half a bar
            }
            for (int j = 0; j < BedSources; j++) if (bed[j].clip == null && bedSong[j] != null && InUse(j)) JoinBed(j, now);
        }

        // ---- levels
        for (int i = 0; i < 2; i++)
        {
            fade[i] = Mathf.MoveTowards(fade[i], fadeTarget[i], dt / FadeSeconds);
            lead[i].volume = LeadLevel(i);
            if (fade[i] <= 0f && fadeTarget[i] <= 0f && i != live && !(pending && pendingIndex == i) && lead[i].clip != null) StopLead(i);
        }
        for (int j = 0; j < BedSources; j++)
        {
            bed[j].volume = BedLevel(j);
            if (bed[j].clip != null && !InUse(j)) StopBed(j); // its lead has faded out, or the switch it was armed for was cancelled
        }
        float fx = Master * GameSettings.MusicVolume * Mathf.Max(bedVol, 0.6f) * duckMul;
        riser.volume = fx * 0.8f;
        impact.volume = fx;
        crackle.volume = Master * GameSettings.MusicVolume * crackleVol;
    }

    /// A 78 in the next room, once per lobby visit when the toggle is on: loaded raw, through the horn, fading in and out
    void GramophoneTick(float dt)
    {
        bool want = Gramophone && (mood == Mood.Menu || mood == Mood.Calm) && !PauseMenu.IsPaused;
        var sides = MusicRemix.GramophoneSides;
        if (want && gramaSong == null && !gramaPlayedThisScene && sides.Count > 0) { gramaSong = sides[gramaIndex % sides.Count]; gramaIndex++; MusicRemix.Prepare(gramaSong); }
        if (gramaSong != null && want && !gramaPlayedThisScene && gramaSong.Ready && !grama.isPlaying)
        {
            grama.clip = gramaSong.lead; grama.loop = false; grama.volume = 0f;
            gramaPump.SetBoost(gramaSong.boost);
            grama.Play();
            gramaSong.live = true; gramaPlayedThisScene = true;
        }
        if (gramaSong != null && gramaSong.state < 0) gramaSong = null;
        gramaVol = Mathf.MoveTowards(gramaVol, want && grama.isPlaying ? 0.18f : 0f, dt * 0.12f);
        grama.volume = Master * GameSettings.MusicVolume * gramaVol * duckMul;
        if (grama.isPlaying && !want && gramaVol <= 0f) grama.Stop();
        if (gramaSong != null && gramaPlayedThisScene && !grama.isPlaying) { gramaSong.live = false; MusicRemix.Release(gramaSong); gramaSong = null; }
    }

    float LeadLevel(int i) => Master * GameSettings.MusicVolume * leadVol * (leadSong[i] != null ? leadSong[i].gain : 0f) * fade[i] * duckMul; // the MUSIC slider rides on top
    float BedLevel(int j) => Master * GameSettings.MusicVolume * bedVol * fade[bedOwner[j]] * duckMul;

    bool InUse(int j) => j == bedLive || (bedPending && j == bedPendingIndex) || (pending && j == pendingBed) || (bed[j].clip != null && fade[bedOwner[j]] > 0.001f && bedSong[j] == leadSong[bedOwner[j]]);

    int FreeBed()
    {
        for (int j = 0; j < BedSources; j++) if (!InUse(j)) return j;
        int best = 0; float low = float.MaxValue;
        for (int j = 0; j < BedSources; j++) if (j != bedLive && !(bedPending && j == bedPendingIndex) && fade[bedOwner[j]] < low) { low = fade[bedOwner[j]]; best = j; }
        return best;
    }

    // ============================================================ arming

    void ArmLead(int i, MusicRemix.Song s, double at, float startFade)
    {
        var src = lead[i];
        src.Stop();
        if (leadSong[i] != null && leadSong[i] != s && leadSong[1 - i] != leadSong[i]) leadSong[i].live = false;
        src.clip = s.lead;
        src.timeSamples = 0;
        src.loop = false;
        leadSong[i] = s;
        s.live = true;
        leadStart[i] = at;
        fade[i] = startFade;
        fadeTarget[i] = 1f;
        src.volume = LeadLevel(i);
        pump[i].SetBoost(s.boost); // 1 for a mastered side; a raw one is brought up here
        src.PlayScheduled(at);
    }

    /// A bed on a free source at a DSP time, positioned where its lead will be; a bed not rendered yet joins on a bar line later
    int ArmBed(MusicRemix.Song s, MusicRemix.Bed kind, double at, int owner)
    {
        int j = FreeBed();
        var b = bed[j];
        b.Stop();
        bedSong[j] = s; bedStart[j] = leadStart[owner]; bedKindOf[j] = kind; bedOwner[j] = owner;
        b.clip = kind == MusicRemix.Bed.None ? null : MusicRemix.RequestBed(s, kind);
        if (b.clip != null)
        {
            double into = Math.Max(0.0, at - leadStart[owner]);
            b.timeSamples = Math.Min(b.clip.samples - 1, (int)(into * b.clip.frequency));
            b.loop = false;
            b.volume = BedLevel(j);
            b.PlayScheduled(at);
        }
        return j;
    }

    /// A bed that landed after its lead started joins on the lead's next bar line, at the lead's own position
    void JoinBed(int j, double now)
    {
        var s = bedSong[j];
        var kind = bedKindOf[j];
        if (s == null || kind == MusicRemix.Bed.None) return;
        var clip = MusicRemix.RequestBed(s, kind);
        if (clip == null) return;
        double start = bedStart[j];
        double at = Math.Max(now + 0.1, start);
        if (at > start) at = start + s.NextBar(at - start);
        double into = at - start;
        if (into >= s.Length - 0.05) return; // the side is about to end: its successor brings the bed
        var b = bed[j];
        b.Stop();
        b.clip = clip;
        b.loop = false;
        b.timeSamples = Math.Min(clip.samples - 1, (int)(into * clip.frequency));
        b.volume = BedLevel(j);
        b.PlayScheduled(at);
        if (j == bedLive && bedPending) b.SetScheduledEndTime(Math.Max(at + 0.01, bedPendingDsp));
    }

    void StopLead(int i)
    {
        lead[i].Stop(); lead[i].clip = null;
        var s = leadSong[i];
        leadSong[i] = null;
        if (s != null && leadSong[1 - i] != s) s.live = false;
        fade[i] = 0f; fadeTarget[i] = 0f;
        pump[i].SetGrid(null); pump[i].SetBoost(1f);
    }
    void StopBed(int j) { bed[j].Stop(); bed[j].clip = null; bedSong[j] = null; bedKindOf[j] = MusicRemix.Bed.None; }

    /// Between pools (or after a gap): the new side fades up under the old one with the bed the mood wants
    void Crossfade(MusicRemix.Song s, double now)
    {
        int spare = live >= 0 ? 1 - live : 0;
        int old = live;
        if (pending) CancelPending();
        double at = now + 0.05;
        ArmLead(spare, s, at, 0f);
        if (old >= 0)
        {
            fadeTarget[old] = 0f;
            if (cutNext) StopLead(old);
        }
        if (bedPending) CancelBedPending(now);
        int oldBed = bedLive;
        live = spare;
        songEndDsp = at + s.Length;
        target = s;
        bedKind = bedWant;
        bedLive = bedKind == MusicRemix.Bed.None ? -1 : ArmBed(s, bedKind, at, live);
        if (oldBed >= 0 && (cutNext || old < 0)) StopBed(oldBed); // otherwise it fades with its lead
        lastBedSwitchDsp = at;
        pump[live].SetGrid(MusicRemix.PumpGrid(s, bedKind, at));
        cutNext = false;
        AfterStart();
    }

    /// The side after this one in the pool; a side that failed since the pool was read drops out of it
    MusicRemix.Song NextInPool()
    {
        if (pool == null || pool.Count == 0) pool = MusicRemix.Pool(set);
        var next = pool[(poolIndex + 1) % pool.Count];
        if (next.state < 0)
        {
            var current = Song;
            pool = MusicRemix.Pool(set);
            int i = current != null ? pool.IndexOf(current) : -1;
            poolIndex = i >= 0 ? i : Math.Min(poolIndex, pool.Count - 1);
            next = pool[(poolIndex + 1) % pool.Count];
        }
        return next;
    }

    /// The next side of the pool, sample-accurate at the end of this one, with the bed the mood wants now
    void QueueSuccessor(double now)
    {
        var s = Song;
        if (s == null || pool == null || pool.Count == 0) return;
        var next = NextInPool();
        MusicRemix.Prepare(next);
        if (!next.Ready) return;
        int spare = 1 - live;
        ArmLead(spare, next, songEndDsp, 1f);
        pending = true;
        pendingIndex = spare;
        pendingDsp = songEndDsp;
        var kind = bedPending ? bedPendingKind : bedWant;
        pendingBed = kind == MusicRemix.Bed.None ? -1 : ArmBed(next, kind, songEndDsp, spare);
        pump[spare].SetGrid(MusicRemix.PumpGrid(next, kind, songEndDsp));
    }

    void CancelPending()
    {
        if (!pending) return;
        StopLead(pendingIndex);
        if (pendingBed >= 0) StopBed(pendingBed);
        pending = false; pendingBed = -1;
    }

    /// Once a side is playing: the one after it starts rendering, sides nobody holds are released, its beds and one-shots are asked for
    void AfterStart()
    {
        var s = Song;
        if (s == null) return;
        MusicRemix.Song next = pool != null && pool.Count > 0 ? NextInPool() : null;
        if (next != null) MusicRemix.Prepare(next);
        MusicRemix.RequestShots(s);
        if (set == "boss") foreach (var b in BossBeds) MusicRemix.RequestBed(s, b);
        else if (set != "menu") foreach (var b in LevelBeds) MusicRemix.RequestBed(s, b);
        foreach (var other in MusicRemix.Songs)
        {
            if (other == s || other == next || other == target || other.state != 3 || keep.Contains(other)) continue;
            bool held = false;
            for (int i = 0; i < 2; i++) if (leadSong[i] == other) held = true;
            for (int j = 0; j < BedSources; j++) if (bedSong[j] == other && InUse(j)) held = true;
            if (!held) MusicRemix.Release(other);
        }
    }

    void FadeOutAll()
    {
        if (pending) CancelPending();
        if (live >= 0) fadeTarget[live] = 0f;
        live = -1;
        set = "";
        target = null;
        bedLive = -1; bedKind = MusicRemix.Bed.None;
        if (bedPending) { if (bedPendingIndex >= 0) StopBed(bedPendingIndex); bedPending = false; }
    }

    // ============================================================ the bed switch

    static bool WantsDrop(MusicRemix.Bed from, MusicRemix.Bed to) =>
        to != from && (to == MusicRemix.Bed.Breaks || to == MusicRemix.Bed.Boss || to == MusicRemix.Bed.BossClimax);

    /// The wanted bed on the lead's next bar line; into flow and the boss sections a riser first, a silent beat, then the drop
    void ScheduleBed(double now)
    {
        var s = leadSong[live];
        if (s.raw) { bedKind = bedWant; return; } // a raw side has no beds: the mood's filters and levels still move, nothing to arm
        double start = leadStart[live];
        bool drop = WantsDrop(bedKind, bedWant) && s.riser != null && s.impact != null;
        double at = start + (drop ? s.NextBar(now + 0.08 - start) : s.NextHalfBar(now + 0.08 - start)); // a drop lands on the bar; the beat hardening takes the next half bar
        if (at >= songEndDsp - 0.05) return; // the successor carries the new kind
        if (drop)
        {
            double dropAt = start + s.NextBar(at - start + 0.01);
            if (dropAt < songEndDsp - 0.05)
            {
                double beat = s.BeatSeconds;
                riser.Stop(); riser.clip = s.riser; riser.PlayScheduled(at);
                impact.Stop(); impact.clip = s.impact; impact.PlayScheduled(dropAt);
                pump[live].SetGrid(MusicRemix.PumpGrid(s, bedKind, start, dropAt - beat, dropAt)); // the silent beat
                at = dropAt;
            }
        }
        int j = bedWant == MusicRemix.Bed.None ? -1 : ArmBed(s, bedWant, at, live);
        if (bedLive >= 0) bed[bedLive].SetScheduledEndTime(at);
        bedPending = true;
        bedPendingIndex = j;
        bedPendingDsp = at;
        bedPendingKind = bedWant;
        lastBedSwitchDsp = at;
    }

    void CancelBedPending(double now)
    {
        if (!bedPending) return;
        if (bedPendingIndex >= 0) StopBed(bedPendingIndex);
        riser.Stop(); impact.Stop();
        if (bedLive >= 0) bed[bedLive].SetScheduledEndTime(songEndDsp + 10.0); // un-end it
        bedPending = false; bedPendingIndex = -1;
        if (live >= 0) pump[live].SetGrid(MusicRemix.PumpGrid(leadSong[live], bedKind, leadStart[live]));
    }

    /// The kill: the side and its bed stop dead on one hit (BossMusic's Tail); the calm side then fades in from silence
    void CutBed(double now)
    {
        if (pending) CancelPending();
        if (bedPending) CancelBedPending(now);
        var s = Song;
        if (s != null && s.impact != null) { impact.Stop(); impact.clip = s.impact; impact.PlayScheduled(now + 0.02); }
        if (bedLive >= 0) StopBed(bedLive);
        bedLive = -1; bedKind = MusicRemix.Bed.None;
        if (live >= 0) StopLead(live);
        live = -1;
        Duck(0.85f, 1.4f);
    }

    // THE DUCK: a cut on the blade pushes the score down for a beat, fast in and slow out, so the cut is the loudest
    // thing there is. Latest call wins
    static float duckUntil = float.NegativeInfinity, duckDepth;
    float duckMul = 1f;
    public static void Duck(float depth, float seconds) { duckDepth = Mathf.Clamp(depth, 0f, 0.9f); duckUntil = Time.unscaledTime + Mathf.Min(seconds, 4f); } // never to silence, never for long
}
