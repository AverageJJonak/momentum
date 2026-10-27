using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE REMIX. A song is a LEAD and a BED. The lead is the HOTEL ROOM (MusicHotel): a procedural
/// noir-jazz side in the mould of "Quarto de Hotel", seeded per pool, rendered on a worker; the
/// bed (MusicBeds) is synthesised on the same plan — the same kicks, the same bass ostinato an
/// octave down, the same chords — one clip per kind of exactly the lead's length, so
/// MusicDirector can arm a lead and a bed on one DSP clock and swap beds on a bar line while the
/// lead keeps playing. The 78s in Resources/Music/jazz are the GRAMOPHONE: off by default
/// (MusicDirector.Gramophone), a rare low, band-passed layer in the lobby; a side whose chart says
/// lead=1 is instead measured (MusicAnalyse), mastered and played as a lead the old way.
/// NEVER SILENT: a lead whose render or measurement fails, or whose worker is late, falls back
/// (a recording plays RAW; a failed side drops out of its pool; an empty pool plays MusicJazz's
/// band) after ONE warning. Clips are static HideAndDontSave; the beds of a song that is neither
/// playing nor next are released. This file: the library and the loading; MusicRemixJobs.cs: the
/// worker, the requests and the pump grids.
/// </summary>
public static partial class MusicRemix
{
    public const int Rate = 32000;
    public enum Bed { None, Hard, Breaks, Boss, BossBreak, BossClimax }
    public const int BedCount = 6;
    public static readonly string[] Pools = { "menu", "city", "under", "office", "boss" };
    const float TargetRms = 0.178f; // −15 dBFS
    const float MeasureTimeout = 3f, LoadTimeout = 6f, RenderTimeout = 12f;

    public class Song
    {
        public string name, pool;
        public bool recorded, gramophone;   // a 78: gramophone = the lobby layer, else a lead to measure
        public AudioClip source;            // the Resources clip (recorded)
        public AudioClip lead;              // what plays
        public int rate, samples;
        public float gain = 1f, trim = 1f;
        public double[] beats; public int downbeat;
        public int tonic; public bool minor = true; public float swing = 0.62f, bpm = 76f, tuning;
        public float rms, hintRms;
        public int[] roots, kinds;
        public MusicAnalyse.Hints hints = MusicAnalyse.Hints.None;
        public MusicHotel.Chart hotel; public MusicHotel.Plan plan; // the hotel room
        public MusicJazz.Chart band; public bool isBand; public int seed; // the last resort
        public readonly AudioClip[] beds = new AudioClip[BedCount];
        public readonly bool[] bedQueued = new bool[BedCount];
        public AudioClip riser, impact; public bool shotsQueued;
        public int state;                   // 0 idle, 1 loading audio, 2 measuring/rendering, 3 ready, -1 failed
        public bool raw, live, lateWarned;
        public float boost = 1f;
        public double queuedAt;
        public string error;

        public float Length => rate > 0 ? samples / (float)rate : 0f;
        public bool Ready => state == 3 && lead != null;
        public double BeatSeconds => 60.0 / Math.Max(30f, bpm);
        public string LeadState => raw ? "RAW" : isBand ? "BAND" : "OK";
        public double Beat(int i)
        {
            if (beats == null || beats.Length == 0) return i * BeatSeconds;
            if (beats.Length == 1) return beats[0] + i * BeatSeconds;
            if (i < 0) return beats[0] + i * (beats[1] - beats[0]);
            int last = beats.Length - 1;
            if (i > last) return beats[last] + (i - last) * (beats[last] - beats[last - 1]);
            return beats[i];
        }
        public double BeatAt(double x) { int i = (int)Math.Floor(x); double f = x - i; return Beat(i) + f * (Beat(i + 1) - Beat(i)); }
        public double BarStart(int bar) => Beat(downbeat + 4 * bar);
        public double BarLength(int bar) => Beat(downbeat + 4 * bar + 4) - BarStart(bar);
        public int Bars
        {
            get
            {
                if (rate <= 0) return 0;
                int k = Math.Max(0, (int)((Length - BarStart(0)) / Math.Max(0.1, BarLength(0))) - 2);
                while (k > 0 && BarStart(k) >= Length - 0.05) k--;
                while (BarStart(k) < Length - 0.05 && k < 100000) k++;
                return k;
            }
        }
        public int BarAt(double t)
        {
            if (t < BarStart(0)) return -1;
            int k = Math.Max(0, (int)((t - BarStart(0)) / Math.Max(0.1, BarLength(0))) - 2);
            while (k > 0 && BarStart(k) > t) k--;
            while (BarStart(k + 1) <= t && k < 100000) k++;
            return k;
        }
        /// The first bar line strictly after t, or the clip's end when none is left
        public double NextBar(double t)
        {
            double first = BarStart(0);
            if (t < first) return Math.Min(first, Length);
            double at = BarStart(BarAt(t) + 1);
            return at < Length - 0.05 ? at : Length;
        }
        /// The first bar line OR half-bar line (beat 3) after t: at 76 bpm a bar is three seconds, a plain bed switch cannot wait that long
        public double NextHalfBar(double t)
        {
            double first = BarStart(0);
            if (t < first) return Math.Min(first, Length);
            int k = BarAt(t);
            double half = BarStart(k) + 0.5 * BarLength(k);
            double at = half > t + 0.001 ? half : BarStart(k + 1);
            return at < Length - 0.05 ? at : Length;
        }
    }

    public class Slices { public int[] bars = new int[0]; public float[][] eighth = new float[0][], sixteenth = new float[0][]; }

    // ============================================================ the library

    static readonly List<Song> songs = new List<Song>();       // the hotel room (and any lead=1 recording)
    static readonly List<Song> gramophoneSides = new List<Song>();
    static readonly List<Song> band = new List<Song>();        // MusicJazz: the last resort
    static readonly Dictionary<string, List<Song>> pools = new Dictionary<string, List<Song>>();
    static readonly List<Song> loading = new List<Song>(), measuring = new List<Song>();
    static bool inited;
    public static bool Recorded { get; private set; }         // any 78 at all
    public static AudioClip Crackle { get; private set; }
    static bool crackleQueued;

    public static IReadOnlyList<Song> Songs { get { Init(); return songs; } }
    public static IReadOnlyList<Song> GramophoneSides { get { Init(); return gramophoneSides; } }

    public static void Init()
    {
        if (inited) return;
        inited = true;
        foreach (var p in Pools) pools[p] = new List<Song>();
        foreach (var c in MusicHotel.Catalogue())
        {
            var plan = MusicHotel.MakePlan(c, Rate);
            var s = new Song { name = c.name, pool = c.pool, hotel = c, plan = plan, rate = Rate, samples = c.bars * 4 * plan.beatSamples, bpm = plan.bpm, tonic = c.tonic, swing = c.swing, seed = c.seed, roots = plan.roots, kinds = plan.kinds };
            s.beats = new double[c.bars * 4 + 1];
            for (int i = 0; i < s.beats.Length; i++) s.beats[i] = i * (double)plan.beatSamples / Rate;
            songs.Add(s);
        }
        AddBand("the_band", "city", MusicJazz.Feel.Hot, 126f, 5, true, 32, 101);
        AddBand("lobby_number", "menu", MusicJazz.Feel.Lobby, 92f, 5, false, 24, 505);
        var clips = Resources.LoadAll<AudioClip>("Music/jazz");
        var charts = new Dictionary<string, string>();
        foreach (var t in Resources.LoadAll<TextAsset>("Music/jazz")) if (t.name.EndsWith(".chart")) charts[t.name.Substring(0, t.name.Length - 6)] = t.text;
        var list = new List<AudioClip>(clips);
        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        foreach (var c in list)
        {
            if (c == null || c.samples < c.frequency * 8) continue;
            var s = new Song { name = c.name, recorded = true, gramophone = true, source = c, rate = c.frequency, samples = c.samples, pool = "gramophone" };
            if (charts.TryGetValue(c.name, out var text)) ParseChart(s, text);
            if (s.gramophone) gramophoneSides.Add(s); else songs.Add(s);
        }
        Recorded = gramophoneSides.Count > 0;
        int robin = 0;
        foreach (var s in songs)
        {
            if (string.IsNullOrEmpty(s.pool) || !pools.ContainsKey(s.pool)) { s.pool = Pools[1 + robin % 4]; robin++; }
            pools[s.pool].Add(s);
        }
        Debug.Log("MusicRemix: " + songs.Count + " leads (hotel room) — menu " + pools["menu"].Count + ", city " + pools["city"].Count + ", under " + pools["under"].Count + ", office " + pools["office"].Count + ", boss " + pools["boss"].Count + "; " + gramophoneSides.Count + " gramophone sides");
    }

    static void AddBand(string name, string pool, MusicJazz.Feel feel, float bpm, int tonic, bool minor, int bars, int seed)
    {
        int beatSamples = (int)Math.Round(Rate * 60.0 / bpm);
        var chart = new MusicJazz.Chart { feel = feel, bpm = bpm, tonic = tonic, minor = minor, clipGrid = true, swing = 0.6f };
        var s = new Song { name = name, pool = pool, band = chart, isBand = true, rate = Rate, samples = bars * 4 * beatSamples, seed = seed, tonic = tonic, minor = minor, swing = 0.6f, bpm = (float)(Rate * 60.0 / beatSamples) };
        s.beats = new double[bars * 4 + 1];
        for (int i = 0; i < s.beats.Length; i++) s.beats[i] = i * (double)beatSamples / Rate;
        s.roots = MusicJazz.RootsPerBar(chart, bars, out s.kinds);
        band.Add(s);
    }

    /// "bpm=… offset=… key=… swing=… gain=… rms=… pool=… fixed=1 gramophone=1 lead=1", one per line, # comments
    static void ParseChart(Song s, string text)
    {
        var h = MusicAnalyse.Hints.None;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        foreach (var raw in text.Split('\n'))
        {
            string line = raw.Trim();
            int hash = line.IndexOf('#'); if (hash >= 0) line = line.Substring(0, hash).Trim();
            int eq = line.IndexOf('='); if (eq <= 0) continue;
            string k = line.Substring(0, eq).Trim().ToLowerInvariant(), v = line.Substring(eq + 1).Trim();
            bool on = v == "1" || v.ToLowerInvariant() == "true";
            float f;
            switch (k)
            {
                case "bpm": if (float.TryParse(v, System.Globalization.NumberStyles.Float, inv, out f) && f > 20f) h.bpm = f; break;
                case "offset": if (float.TryParse(v, System.Globalization.NumberStyles.Float, inv, out f) && f >= 0f) h.offset = f; break;
                case "swing": if (float.TryParse(v, System.Globalization.NumberStyles.Float, inv, out f)) h.swing = Mathf.Clamp(f, 0.5f, 0.7f); break;
                case "gain": if (float.TryParse(v, System.Globalization.NumberStyles.Float, inv, out f)) s.trim = Mathf.Clamp(f, 0.1f, 4f); break;
                case "rms": if (float.TryParse(v, System.Globalization.NumberStyles.Float, inv, out f)) s.hintRms = f <= 0f ? (float)Math.Pow(10.0, f / 20.0) : f; break;
                case "key": if (MusicAnalyse.ParseKey(v, out int tonic, out bool minor)) { h.tonic = tonic; h.minor = minor; } break;
                case "pool": s.pool = v.ToLowerInvariant(); break;
                case "fixed": h.fixedGrid = on; break;
                case "gramophone": s.gramophone = on; break;
                case "lead": if (on) s.gramophone = false; break;
            }
        }
        s.hints = h;
    }

    /// A pool's sides that can still play; the band when nothing in it can
    public static List<Song> Pool(string pool)
    {
        Init();
        var l = new List<Song>();
        if (pool != null && pools.TryGetValue(pool, out var p)) foreach (var s in p) if (s.state >= 0) l.Add(s);
        if (l.Count > 0) return l;
        foreach (var s in band) if (s.state >= 0 && (s.pool == pool || pool == null)) l.Add(s);
        if (l.Count > 0) return l;
        foreach (var s in band) if (s.state >= 0) l.Add(s);
        return l.Count > 0 ? l : new List<Song>(band);
    }

    // ============================================================ loading

    /// Start producing a song's lead (main thread): the hotel room and the band render, a recording loads and gets measured
    public static void Prepare(Song s)
    {
        if (s == null || s.state != 0) return;
        s.queuedAt = Time.realtimeSinceStartup;
        if (!s.recorded) { s.state = 2; measuring.Add(s); Enqueue(new LeadJob { song = s }, true); return; }
        if (s.source == null) { Fail(s, "no clip"); return; }
        if (s.source.loadState == AudioDataLoadState.Loaded) { if (s.gramophone) Raw(s); else Measure(s); return; }
        if (s.source.loadState == AudioDataLoadState.Failed) { Fail(s, "Unity could not load its audio data"); return; }
        if (s.source.loadState == AudioDataLoadState.Unloaded) s.source.LoadAudioData();
        s.state = 1;
        loading.Add(s);
    }

    /// Read the decoded recording and send it to the worker: measured the first time, MASTERED every time (an AudioSource cannot be turned up past 1)
    static void Measure(Song s)
    {
        var src = s.source;
        int ch = Math.Max(1, src.channels);
        float[] pcm;
        try
        {
            pcm = new float[(long)src.samples * ch];
            if (!src.GetData(pcm, 0)) { Fail(s, "GetData refused — its Load Type is not Decompress On Load (select the file, set Decompress On Load, reimport)"); return; }
        }
        catch (Exception e) { Fail(s, "GetData threw: " + e.Message); return; }
        s.rate = src.frequency; s.samples = src.samples;
        s.state = 2;
        s.queuedAt = Time.realtimeSinceStartup;
        measuring.Add(s);
        Enqueue(new AnalyseJob { song = s, pcm = pcm, channels = ch, rate = src.frequency, hints = s.hints, analyse = s.beats == null || s.raw, target = TargetRms * s.trim }, true);
    }

    /// Gain into a look-ahead peak limiter (3 ms ahead, 0.8 ms in, 80 ms out, ceiling 0.95), in place
    static void Master(float[] x, int rate, int channels, float gain)
    {
        const float ceiling = 0.95f;
        int look = Math.Max(8, (int)(0.003f * rate)) * channels;
        int blocks = (x.Length + look - 1) / look;
        var bmax = new float[blocks];
        for (int i = 0; i < x.Length; i++) { float a = Math.Abs(x[i]) * gain; int b = i / look; if (a > bmax[b]) bmax[b] = a; }
        float env = 1f, attack = 1f - (float)Math.Exp(-1.0 / (0.0008 * rate)), release = 1f - (float)Math.Exp(-1.0 / (0.08 * rate));
        for (int i = 0; i < x.Length; i++)
        {
            int b = i / look;
            float peak = Math.Max(bmax[b], b + 1 < blocks ? bmax[b + 1] : 0f);
            float want = peak > ceiling ? ceiling / peak : 1f;
            env += (want - env) * (want < env ? attack : release);
            float v = x[i] * gain * env;
            x[i] = v > 0.98f ? 0.98f : v < -0.98f ? -0.98f : v;
        }
    }

    /// A recording played as decoded: no measurement, no bed, a nominal grid, the pump bringing it up. The gramophone's way, and the fallback's
    static void Raw(Song s)
    {
        measuring.Remove(s); loading.Remove(s);
        s.raw = true; s.lead = s.source; s.rate = s.source.frequency; s.samples = s.source.samples; s.gain = 1f;
        s.boost = s.hintRms > 0f ? Mathf.Clamp(TargetRms / s.hintRms, 1f, 4f) : 3f;
        s.bpm = s.hints.bpm > 20f ? s.hints.bpm : 120f;
        if (s.hints.tonic >= 0) { s.tonic = s.hints.tonic; s.minor = s.hints.minor; }
        double beat = 60.0 / s.bpm, first = s.hints.offset >= 0f ? s.hints.offset % beat : 0.0;
        var grid = new List<double>();
        for (double t = first; t < s.Length + beat; t += beat) grid.Add(t);
        s.beats = grid.ToArray(); s.downbeat = 0;
        s.roots = new[] { s.tonic }; s.kinds = new[] { s.minor ? 1 : 2 };
        for (int i = 0; i < BedCount; i++) s.bedQueued[i] = false;
        s.state = 3;
    }

    /// The never-silent path, with its one warning: a recording plays RAW, anything else is skipped
    static void Fail(Song s, string why)
    {
        bool canRaw = s.recorded && s.source != null;
        Debug.LogWarning("MUSIC: " + s.name + " lead unavailable: " + why + (canRaw ? " — playing it RAW (no bed) until it is next reloaded" : " — skipped"));
        measuring.Remove(s); loading.Remove(s);
        s.error = why;
        if (canRaw) Raw(s); else s.state = -1;
    }

    /// Drop what a song no longer needs: its beds (re-rendered when it comes round again) and a recording's audio
    public static void Release(Song s)
    {
        if (s == null || s.live) return;
        for (int i = 0; i < BedCount; i++) { if (s.beds[i] != null) { bytes -= (long)s.beds[i].samples * 4; UnityEngine.Object.Destroy(s.beds[i]); } s.beds[i] = null; s.bedQueued[i] = false; }
        if (s.recorded && s.state == 3 && s.source != null) { s.source.UnloadAudioData(); s.lead = null; s.state = 0; }
    }
}
