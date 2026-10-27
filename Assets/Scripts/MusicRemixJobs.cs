using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// The worker, the requests and the pump grids of MusicRemix (see MusicRemix.cs). Two workers: leads (the hotel
/// room, the band, a recording's measurement) never wait behind a bed render. Finished jobs become clips on the
/// main thread in Poll, which also runs the watchdogs
public static partial class MusicRemix
{
    abstract class Job { public Song song; public string error; public abstract void Run(); public abstract void Finish(); }

    class AnalyseJob : Job
    {
        public float[] pcm; public int channels, rate; public MusicAnalyse.Hints hints; public bool analyse; public float target; MusicAnalyse.Result r; float gain;
        public override void Run()
        {
            float rms;
            if (analyse) { r = MusicAnalyse.Run(pcm, channels, rate, hints); rms = r.rms; }
            else { double sq = 0; for (int i = 0; i < pcm.Length; i++) sq += pcm[i] * (double)pcm[i]; rms = (float)Math.Sqrt(sq / Math.Max(1, pcm.Length)); }
            gain = Mathf.Clamp(target / Mathf.Max(0.002f, rms), 0.1f, 8f);
            Master(pcm, rate, channels, gain);
        }
        public override void Finish()
        {
            measuring.Remove(song);
            if (song.raw) { if (song.live) { pcm = null; return; } song.raw = false; song.boost = 1f; Debug.Log("MUSIC: " + song.name + " measured late — the next time it plays it has its bed"); }
            if (analyse)
            {
                song.beats = r.beats; song.downbeat = r.downbeat; song.tonic = r.tonic; song.minor = r.minor; song.swing = r.swing; song.bpm = r.bpm; song.tuning = r.tuning;
                song.roots = r.roots; song.kinds = r.kinds; song.rms = r.rms;
                song.plan = MusicHotel.MakePlan(new MusicHotel.Chart { name = song.name, bpm = song.bpm, tonic = song.tonic, bars = Math.Max(16, song.Bars), seed = Seed(song.name), swing = song.swing, stringsFrom = 16 }, Rate); // a measured lead gets a plan on its key so the beds have kicks, a riff and chords
                for (int i = 0; i < BedCount; i++) song.bedQueued[i] = false;
                Debug.Log("MusicRemix: " + song.name + " — " + r.log + ", mastered x" + gain.ToString("0.00"));
            }
            if (song.source != null && song.source.loadState == AudioDataLoadState.Loaded) song.source.SetData(pcm, 0);
            song.gain = 1f; song.lead = song.source; song.state = 3; pcm = null;
        }
    }

    class LeadJob : Job
    {
        float[] result; float rms;
        public override void Run()
        {
            result = song.hotel != null ? MusicHotel.Render(song.plan, Rate) : MusicJazz.Render(song.band, song.samples, Rate, song.seed, false);
            double sq = 0; for (int i = 0; i < result.Length; i++) sq += result[i] * (double)result[i];
            rms = (float)Math.Sqrt(sq / Math.Max(1, result.Length));
        }
        public override void Finish()
        {
            measuring.Remove(song);
            song.samples = result.Length;
            song.lead = MakeClip("Lead_" + song.name, result, Rate);
            song.gain = Mathf.Clamp(TargetRms / Mathf.Max(0.005f, rms), 0.2f, 4f);
            song.state = 3; result = null;
        }
    }

    class BedJob : Job
    {
        public MusicBeds.Spec spec; float[] result;
        public override void Run() { result = MusicBeds.Render(spec); }
        public override void Finish()
        {
            song.bedQueued[(int)spec.kind] = false;
            if ((song.state != 3 && song.state != 0) || song.raw) { result = null; return; }
            song.beds[(int)spec.kind] = MakeClip("Bed_" + spec.kind + "_" + song.name, result, Rate);
            result = null;
        }
    }

    class ShotsJob : Job
    {
        public float beat; public int seed; float[] riser, impact;
        public override void Run() { riser = MusicBeds.RenderRiser(beat, seed); impact = MusicBeds.RenderImpact(seed); }
        public override void Finish() { song.riser = MakeClip("Riser_" + song.name, riser, Rate); song.impact = MakeClip("Impact_" + song.name, impact, Rate); riser = impact = null; }
    }

    class CrackleJob : Job
    {
        float[] result;
        public override void Run() { result = MusicBeds.RenderCrackle(77); }
        public override void Finish() { Crackle = MakeClip("Crackle", result, Rate); result = null; }
    }

    static readonly ConcurrentQueue<Job> todoLead = new ConcurrentQueue<Job>(), todoBed = new ConcurrentQueue<Job>(), done = new ConcurrentQueue<Job>();
    static Task leadWorker, bedWorker;
    static readonly object clock = new object();
    static double clockMs;
    static long bytes;

    /// Worker time spent rendering and measuring this session (both workers)
    public static double RenderMilliseconds { get { lock (clock) return clockMs; } }
    /// Float PCM held in synthesised clips right now
    public static long BytesHeld => bytes;

    static void Enqueue(Job j, bool lead) { (lead ? todoLead : todoBed).Enqueue(j); Kick(); }

    static void Kick()
    {
        if (!todoLead.IsEmpty && (leadWorker == null || leadWorker.IsCompleted)) leadWorker = Task.Run(() => Drain(todoLead));
        if (!todoBed.IsEmpty && (bedWorker == null || bedWorker.IsCompleted)) bedWorker = Task.Run(() => Drain(todoBed));
    }

    static void Drain(ConcurrentQueue<Job> queue)
    {
        while (queue.TryDequeue(out var job))
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try { job.Run(); }
            catch (Exception e) { job.error = e.ToString(); }
            lock (clock) clockMs += sw.Elapsed.TotalMilliseconds;
            done.Enqueue(job);
        }
    }

    /// Main thread, every frame: recordings that finished loading get measured (or go raw), finished jobs become clips, late ones fall back
    public static void Poll()
    {
        Kick();
        float now = Time.realtimeSinceStartup;
        for (int i = loading.Count - 1; i >= 0; i--)
        {
            var s = loading[i];
            var st = s.source != null ? s.source.loadState : AudioDataLoadState.Failed;
            if (st == AudioDataLoadState.Loaded) { loading.RemoveAt(i); if (s.gramophone) Raw(s); else Measure(s); }
            else if (st == AudioDataLoadState.Failed) { loading.RemoveAt(i); Fail(s, "Unity could not load its audio data"); }
            else if (now - s.queuedAt > LoadTimeout) { loading.RemoveAt(i); Fail(s, "audio still loading after " + LoadTimeout + " s"); }
        }
        for (int i = measuring.Count - 1; i >= 0; i--)
        {
            var s = measuring[i];
            if (s.state != 2) { measuring.RemoveAt(i); continue; }
            float late = (float)(now - s.queuedAt);
            if (s.recorded && late > MeasureTimeout) { measuring.RemoveAt(i); Fail(s, "the worker had not measured it after " + MeasureTimeout + " s"); }
            else if (!s.recorded && late > RenderTimeout && !s.lateWarned) { s.lateWarned = true; Debug.LogWarning("MUSIC: " + s.name + " still rendering after " + RenderTimeout + " s — waiting"); }
        }
        while (done.TryDequeue(out var job))
        {
            if (job.error != null)
            {
                if (job is AnalyseJob || job is LeadJob) { if (job.song.state == 2 || !job.song.raw) Fail(job.song, (job is AnalyseJob ? "measurement threw: " : "render threw: ") + FirstLine(job.error)); }
                else if (job is BedJob b) { job.song.bedQueued[(int)b.spec.kind] = false; Debug.LogWarning("MUSIC: " + job.song.name + " " + b.spec.kind + " bed failed: " + FirstLine(job.error)); }
                else if (job is ShotsJob) job.song.shotsQueued = false;
                else Debug.LogWarning("MUSIC: crackle failed: " + FirstLine(job.error));
                continue;
            }
            try { job.Finish(); }
            catch (Exception e) { Debug.LogWarning("MUSIC: finish failed: " + e); }
        }
    }

    static string FirstLine(string s) { int i = s.IndexOf('\n'); return i > 0 ? s.Substring(0, i) : s; }

    static AudioClip MakeClip(string name, float[] data, int rate)
    {
        var clip = AudioClip.Create(name, data.Length, 1, rate, false);
        clip.hideFlags = HideFlags.HideAndDontSave;
        clip.SetData(data, 0);
        bytes += (long)data.Length * 4;
        return clip;
    }

    // ============================================================ requests

    /// A song's bed of a kind (main thread): null until rendered; asking queues the render once the lead is ready. A raw side has none
    public static AudioClip RequestBed(Song s, Bed kind)
    {
        if (s == null || kind == Bed.None || s.raw || s.plan == null) return null;
        int k = (int)kind;
        if (s.beds[k] != null) return s.beds[k];
        if (s.bedQueued[k] || !s.Ready) return null;
        s.bedQueued[k] = true;
        Enqueue(new BedJob { song = s, spec = new MusicBeds.Spec { plan = s.plan, beats = s.beats, downbeat = s.downbeat, length = s.Length, bpm = s.bpm, kind = kind, seed = Seed(s.name) * 31 + k, slices = kind == Bed.BossBreak ? new Slices() : ReadSlices(s) } }, false);
        return null;
    }

    /// The drop one-shots at this song's tempo (the riser is three beats long, the impact lands on the drop)
    public static void RequestShots(Song s)
    {
        if (s == null || s.shotsQueued || s.riser != null || !s.Ready) return;
        s.shotsQueued = true;
        Enqueue(new ShotsJob { song = s, beat = (float)s.BeatSeconds, seed = Seed(s.name) }, false);
    }

    public static void RequestCrackle()
    {
        if (Crackle != null || crackleQueued) return;
        crackleQueued = true;
        Enqueue(new CrackleJob(), false);
    }

    static int Seed(string key) { unchecked { int h = 17; foreach (char ch in key) h = h * 31 + ch; return h; } }

    /// The lead's own audio for the stutter edits: the eighth on beat 3 and the sixteenth on beat 4 of the last bar of every form
    static Slices ReadSlices(Song s)
    {
        var sl = new Slices();
        if (s.lead == null || s.plan == null) return sl;
        var barList = new List<int>(); var e = new List<float[]>(); var x = new List<float[]>();
        int ch = Math.Max(1, s.lead.channels);
        try
        {
            foreach (int k in MusicBeds.StutterBars(s.plan))
            {
                double b3 = s.downbeat + 4 * k + 2, b4 = b3 + 1;
                float[] a = Read(s.lead, ch, s.BeatAt(b3), s.BeatAt(b3 + 0.5) - s.BeatAt(b3));
                float[] b = Read(s.lead, ch, s.BeatAt(b4), s.BeatAt(b4 + 0.25) - s.BeatAt(b4));
                if (a == null || b == null) continue;
                barList.Add(k); e.Add(a); x.Add(b);
            }
        }
        catch (Exception ex) { Debug.LogWarning("MUSIC: " + s.name + " stutter slices unavailable: " + ex.Message); }
        sl.bars = barList.ToArray(); sl.eighth = e.ToArray(); sl.sixteenth = x.ToArray();
        return sl;
    }

    static float[] Read(AudioClip clip, int ch, double t, double seconds)
    {
        int from = (int)(t * clip.frequency), n = (int)(seconds * clip.frequency);
        if (n <= 0 || from < 0 || from + n > clip.samples) return null;
        var buf = new float[n * ch];
        if (!clip.GetData(buf, from)) return null;
        if (ch == 1) return buf;
        var mono = new float[n];
        for (int i = 0; i < n; i++) { float v = 0f; for (int c = 0; c < ch; c++) v += buf[i * ch + c]; mono[i] = v / ch; }
        return mono;
    }

    // ============================================================ the pump grid for the lead

    public static float PumpDepth(Bed k) => k switch { Bed.Hard => 0.45f, Bed.Breaks => 0.55f, Bed.Boss => 0.5f, Bed.BossBreak => 0.25f, Bed.BossClimax => 0.55f, _ => 0f };

    /// The bed's kicks as DSP times (the plan's pattern, bar by bar), and the windows where the lead comes down (the stutter beats, a drop's silent beat)
    public static MusicPump.Grid PumpGrid(Song s, Bed kind, double startDsp, double gateFrom = 0, double gateTo = 0)
    {
        var g = new MusicPump.Grid { depth = PumpDepth(kind), width = 0.07f };
        var ducks = new List<double>();
        var from = new List<double>(); var to = new List<double>(); var gain = new List<float>();
        if (s != null && kind != Bed.None && !s.raw && s.plan != null)
        {
            var p = s.plan;
            bool two = kind == Bed.Breaks || kind == Bed.BossClimax, boss = kind == Bed.Boss || kind == Bed.BossClimax, half = kind == Bed.BossBreak;
            int bars = Math.Min(p.bars, s.Bars);
            for (int k = 0; k < bars; k++)
            {
                void Add(double beat) { double t = s.BeatAt(s.downbeat + 4 * k + beat); if (t < s.Length) ducks.Add(startDsp + t); }
                int p8 = k & 7;
                if (two) { Add(0); Add(7 / 8.0); Add(10 / 8.0); Add(2); Add(2 + 7 / 8.0); Add(2 + 10 / 8.0); }
                else if (half) { Add(0); if ((k & 1) == 1) Add(2.5); }
                else { Add(0); Add(1.75); if (p.kick35[k] || boss) Add(2.5); if (boss && (k & 1) == 1) Add(0.5); if (p8 == 3 || p8 == 7) Add(3.5); }
            }
            if (kind != Bed.BossBreak)
                foreach (int k in MusicBeds.StutterBars(p)) if (k < bars) { from.Add(startDsp + s.BeatAt(s.downbeat + 4 * k + 2)); to.Add(startDsp + s.BarStart(k + 1)); gain.Add(0.3f); }
        }
        if (gateTo > gateFrom) { from.Add(gateFrom); to.Add(gateTo); gain.Add(0f); }
        ducks.Sort();
        g.ducks = ducks.ToArray(); g.winFrom = from.ToArray(); g.winTo = to.ToArray(); g.winGain = gain.ToArray();
        return g;
    }
}
