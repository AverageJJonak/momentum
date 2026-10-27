using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// The Concierge's cues, on the Risk of Rain 2 shape: a long, patient approach that
/// turns into a driving groove, then keeps escalating with the fight. Six sections,
/// switched at bar lines by the fight itself:
///   Approach  — the cutscene: the boss pool's side alone, through the gramophone.
///   Pulse     — the first groove (the director plays its main bed).
///   Drop      — phase one: a riser, a silent beat, the full boss bed.
///   Breakdown — the plates come off: half time, pulled behind a filter.
///   Climax    — phase two: another drop into the double-time break under the stabs.
///   Tail      — the kill: one hit, the bed cut dead, the calm side.
/// In normal play every cue is BRIDGED to MusicDirector, which scores the fight from
/// the remix (MusicRemix): the boss pool's recording with the boss beds swapped on
/// the bar. The synth here is the FALLBACK for a director that is switched off
/// (MusicDirector.Enabled = false): bed-only sections rendered by MusicRemix at 132
/// in D minor, no lead, the same drum palette, rendered once per session and kept.
/// </summary>
public class BossMusic : MonoBehaviour
{
    public enum Section { Approach, Pulse, Drop, Breakdown, Climax, Tail }

    public static BossMusic Instance { get; private set; }

    const float Bpm = 78f; // the hotel room's pace
    const float Beat = 60f / Bpm;
    const float Bar = Beat * 4f;
    static readonly int[] Bars = { 8, 8, 16, 8, 16, 4 };
    static readonly MusicRemix.Bed[] Beds = { MusicRemix.Bed.BossBreak, MusicRemix.Bed.Hard, MusicRemix.Bed.Boss, MusicRemix.Bed.BossBreak, MusicRemix.Bed.BossClimax, MusicRemix.Bed.BossBreak };

    public float volume = 0.55f;

    // Rendered ONCE per session and kept alive for good: static references keep the
    // clips out of the unused-asset sweep that used to freeze the game for a minute
    static AudioClip[] clips;
    static Task<float[][]> renderTask;
    AudioSource a, b;
    bool useA;
    Section current = Section.Approach;
    bool playing;
    double sectionStartDsp;
    int sectionBars;

    public bool Ready => clips != null;
    public Section Current => current;
    public bool Playing => playing;

    void Awake()
    {
        Instance = this;
        a = NewSource();
        b = NewSource();
        if (MusicDirector.Enabled) MusicDirector.Ensure().PrepareBoss(); // the boss side and its beds render now, before the cutscene ends
        else if (clips == null && renderTask == null) renderTask = Task.Run(RenderAll);
    }

    void OnDisable()
    {
        if (a != null) a.Stop();
        if (b != null) b.Stop();
        playing = false;
        if (Instance == this) Instance = null;
    }

    AudioSource NewSource()
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.spatialBlend = 0f;
        s.loop = true;
        s.playOnAwake = false;
        s.volume = volume * GameSettings.MusicVolume;
        return s;
    }

    void Update()
    {
        if (playing) a.volume = b.volume = volume * GameSettings.MusicVolume; // the MUSIC slider, live
        if (clips == null && renderTask != null && renderTask.IsCompleted)
        {
            float[][] rendered = renderTask.IsCompletedSuccessfully ? renderTask.Result : null;
            renderTask = null;
            if (rendered == null) return;
            var made = new AudioClip[rendered.Length];
            for (int i = 0; i < rendered.Length; i++)
            {
                made[i] = AudioClip.Create("Concierge_" + (Section)i, rendered[i].Length, 1, MusicRemix.Rate, false);
                made[i].hideFlags = HideFlags.HideAndDontSave; // survives every scene load with us
                made[i].SetData(rendered[i], 0);
            }
            clips = made;
        }
    }

    /// Start the theme from a section (right now, on the next frame)
    public void Play(Section s)
    {
        if (Bridge(s)) return;
        if (clips == null) return;
        var src = useA ? a : b;
        var other = useA ? b : a;
        other.Stop();
        src.clip = clips[(int)s];
        src.loop = s != Section.Tail;
        src.volume = volume * GameSettings.MusicVolume;
        double now = AudioSettings.dspTime + 0.05;
        src.PlayScheduled(now);
        sectionStartDsp = now;
        sectionBars = Bars[(int)s];
        current = s;
        playing = true;
        useA = !useA;
    }

    /// Switch to a section on the next bar line — the fight changes gear on the beat
    public void Transition(Section s)
    {
        if (Bridge(s)) return;
        if (clips == null || !playing) { Play(s); return; }
        if (s == current) return;
        double now = AudioSettings.dspTime;
        double elapsed = now - sectionStartDsp;
        double bar = Bar;
        double nextBar = sectionStartDsp + Math.Ceiling((elapsed + 0.02) / bar) * bar;
        var src = useA ? a : b;   // the free one
        var live = useA ? b : a;  // the one playing
        live.SetScheduledEndTime(nextBar);
        src.clip = clips[(int)s];
        src.loop = s != Section.Tail;
        src.volume = volume * GameSettings.MusicVolume;
        src.PlayScheduled(nextBar);
        sectionStartDsp = nextBar;
        sectionBars = Bars[(int)s];
        current = s;
        useA = !useA;
    }

    public void Stop()
    {
        if (MusicDirector.Instance != null) MusicDirector.Instance.BossStop();
        a.Stop();
        b.Stop();
        playing = false;
    }

    /// The director scores the fight from these cues; the synth here only plays when it is switched off
    bool Bridge(Section s)
    {
        if (!MusicDirector.Enabled) return false;
        var d = MusicDirector.Ensure();
        if (d == null) return false;
        d.BossCue(s);
        current = s;
        playing = s != Section.Tail;
        return true;
    }

    // ============================================================ the fallback

    /// Bed-only sections at 78 in D minor on a hotel-room plan: half time for the approach, the hard beat for the pulse, the boss beds for the rest
    static float[][] RenderAll()
    {
        var all = new float[Bars.Length][];
        for (int i = 0; i < Bars.Length; i++) all[i] = MusicBeds.RenderSection(Beds[i], Bpm, Bars[i], 2, 7919 * (i + 1) + 101);
        return all;
    }
}
