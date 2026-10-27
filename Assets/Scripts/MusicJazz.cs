using System;
using System.Collections.Generic;

/// <summary>
/// THE HOUSE BAND, synthesised. A jazz band rendered into float buffers: the
/// procedural LEAD of the remix score (MusicRemix) until real recordings are
/// dropped into Resources/Music/jazz, and the lead the boss fallback plays over.
/// Music is the one place synthesis is allowed in this project; every SFX is a
/// recording.
///   Walking bass   a plucked sine with a little second and third harmonic and a thumb thump;
///                  roots, thirds, fifths, chromatic approaches, the odd octave skip
///   Drums          ride (inharmonic partials + band-passed sizzle, the swung skip), brushed
///                  snare (a circling swish, or taps on 2 and 4), a feathered kick, a foot hat
///   Piano          rootless 7th/9th shells in Charleston cells; additive partials with a hammer
///   Muted trumpet  a band-limited pulse through a sweeping resonant low-pass (the wah), vibrato,
///                  a scoop into the note; seeded phrases from the chord scales, with rests
///   Brass (boss)   detuned saw stacks with a short decay, on the accents
///   Room           early reflections and two damped combs, on the band bus only
/// A render is deterministic (seeded), runs on whatever thread calls it (MusicRemix's
/// worker), and comes back as a seamless LOOP (wrap = true, every tail crosses the
/// join) or as a SONG (wrap = false: nothing wraps, the band stops on the first beat
/// of the last bar and rings out, so the next song can start clean on the bar).
/// </summary>
public static class MusicJazz
{
    public enum Feel { None, Lobby, Ballad, Explore, Combat, Flow, Boss, Hot }

    public struct Chart
    {
        public Feel feel;
        public float bpm;     // the band's tempo; the beat count is rounded to fit the clip exactly, so the real tempo can differ by a hair
        public int tonic;     // pitch class of the key, 0 = C
        public bool minor;
        public bool clipGrid; // the clip is whole beats at a known tempo (the beat count is rounded to beats, not bars)
        public float swing;   // where the off-beat eighth falls, in beats (0 = by tempo)
    }

    static int Beats(Chart c, int samples, int rate)
    {
        double seconds = samples / (double)rate;
        if (c.clipGrid) return Math.Max(4, (int)Math.Round(seconds * c.bpm / 60.0));
        return 4 * Math.Max(1, (int)Math.Round(seconds * c.bpm / 240.0));
    }

    /// The chord root (pitch class) and kind (0 maj7, 1 min7, 2 dom7, 3 m7b5) of every bar the band would play: the bed's sub and stabs follow them
    public static int[] RootsPerBar(Chart c, int bars, out int[] kinds)
    {
        var chords = ChordsPerBeat(c, bars * 4, bars);
        var roots = new int[bars];
        kinds = new int[bars];
        for (int b = 0; b < bars; b++) { roots[b] = chords[b * 4].root; kinds[b] = chords[b * 4].kind; }
        return roots;
    }

    // ============================================================ the render

    /// A band loop (wrap) or song (!wrap) of exactly `samples` samples at `rate`, mono, peak 0.9
    public static float[] Render(Chart c, int samples, int rate, int seed, bool wrap = true)
    {
        var ctx = new Ctx(samples, rate, seed, wrap);
        var rng = new System.Random(seed);
        int beats = Beats(c, samples, rate);
        ctx.beat = samples / (double)beats;
        double bpm = 60.0 * rate / ctx.beat;
        ctx.swing = c.swing > 0f ? c.swing : Lerp(0.667f, 0.58f, Clamp01(((float)bpm - 100f) / 80f)); // triplet swing, straightening as the tempo climbs
        int bars = beats / 4;
        var p = ParamsOf(c.feel);
        var chords = ChordsPerBeat(c, beats, bars);
        int play = wrap || beats <= 8 ? beats : beats - 4; // a song keeps its last bar for the final hit and the ring-out
        Drums(ctx, p, play, rng);
        BassLine(ctx, p, chords, play, rng);
        Comping(ctx, p, chords, play, rng);
        if (p.horn > 0f) HornSolo(ctx, c, p, chords, play, play / 4, rng);
        if (p.brass) BrassHits(ctx, chords, play, play / 4, rng);
        if (play < beats) FinalHit(ctx, c, p, play, rng);
        Room(ctx, p.room);
        Finish(ctx.buf);
        return ctx.buf;
    }

    const int TabSize = 4096;
    static readonly float[] SinTab = BuildSin();
    static float[] BuildSin()
    {
        var t = new float[TabSize];
        for (int i = 0; i < TabSize; i++) t[i] = (float)Math.Sin(2.0 * Math.PI * i / TabSize);
        return t;
    }
    static float Osc(double cycles) { double p = cycles - Math.Floor(cycles); return SinTab[(int)(p * TabSize) & (TabSize - 1)]; }
    static float Tab(float[] table, double cycles) { double p = cycles - Math.Floor(cycles); return table[(int)(p * TabSize) & (TabSize - 1)]; }
    static float Freq(int midi) => 440f * (float)Math.Pow(2.0, (midi - 69) / 12.0);
    static float Lerp(float a, float b, float t) => a + (b - a) * t;
    static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    static int Clamp(int v, int lo, int hi) => v < lo ? lo : v > hi ? hi : v;

    class Ctx
    {
        public readonly float[] buf;
        public readonly int len, rate;
        public readonly bool wrap; // a loop wraps every tail round the join; a song drops what runs past the end
        public double beat;   // samples per beat
        public float swing;   // where the off-beat eighth falls, in beats (0.5 = straight, 0.667 = triplet)
        uint noise;
        public Ctx(int len, int rate, int seed, bool wrap) { buf = new float[len]; this.len = len; this.rate = rate; this.wrap = wrap; noise = ((uint)seed * 2654435761u) | 1u; }
        public float Noise() { noise ^= noise << 13; noise ^= noise >> 17; noise ^= noise << 5; return (noise >> 8) * (1f / 8388608f) - 1f; }
        public int At(double beatPos) { int i = (int)Math.Round(beatPos * beat) % len; return i < 0 ? i + len : i; }
        public double Eighth(int e) => (e >> 1) + ((e & 1) == 1 ? swing : 0.0);
        /// The next write index, or -1 when a song has run off its end
        public int Step(int at) { if (++at == len) return wrap ? 0 : -1; return at; }
    }

    struct Chord { public int root, kind; } // kind: 0 maj7, 1 min7, 2 dom7, 3 m7b5
    static readonly int[][] Tones = { new[] { 0, 4, 7, 11 }, new[] { 0, 3, 7, 10 }, new[] { 0, 4, 7, 10 }, new[] { 0, 3, 6, 10 } };
    static readonly int[] MajorScale = { 0, 2, 3, 4, 5, 7, 9, 10 }; // the major blues: Mixolydian with the flat third beside the third
    static readonly int[] MinorScale = { 0, 2, 3, 5, 7, 8, 10 };    // natural minor: Aeolian, no raised seventh anywhere

    struct Feels
    {
        public bool ride, rideLite, rideBusy, bed, taps, hat, bombs, brass;
        public float rideAmp, kickAmp, kickAll, bassAmp, walk, piano, pianoAmp, pianoDecay, horn, room;
        public int hornEntry; // bars of silence before the horn's first phrase
    }

    static Feels ParamsOf(Feel f)
    {
        switch (f)
        {
            case Feel.Lobby:   return new Feels { ride = true, rideLite = true, bed = true, rideAmp = 0.05f, kickAmp = 0.12f, bassAmp = 0.5f, walk = 0f, piano = 0.45f, pianoAmp = 0.5f, pianoDecay = 1.3f, horn = 0.4f, hornEntry = 8, room = 0.3f };
            case Feel.Ballad:  return new Feels { bed = true, kickAmp = 0.1f, bassAmp = 0.45f, walk = 0f, piano = 0.4f, pianoAmp = 0.45f, pianoDecay = 1.5f, horn = 0.3f, hornEntry = 4, room = 0.32f };
            case Feel.Explore: return new Feels { ride = true, rideLite = true, bed = true, hat = true, rideAmp = 0.07f, kickAmp = 0.14f, bassAmp = 0.42f, walk = 0f, piano = 0.3f, pianoAmp = 0.4f, pianoDecay = 0.6f, horn = 0f, room = 0.24f }; // in 2: the head, through the wall
            case Feel.Combat:  return new Feels { ride = true, taps = true, hat = true, rideAmp = 0.1f, kickAmp = 0.16f, bassAmp = 0.5f, walk = 1f, piano = 0.6f, pianoAmp = 0.5f, pianoDecay = 0.4f, horn = 0.55f, hornEntry = 2, room = 0.18f };
            case Feel.Flow:    return new Feels { ride = true, rideBusy = true, taps = true, hat = true, bombs = true, rideAmp = 0.11f, kickAmp = 0.18f, kickAll = 0.6f, bassAmp = 0.55f, walk = 1f, piano = 0.7f, pianoAmp = 0.5f, pianoDecay = 0.3f, horn = 0.75f, hornEntry = 0, room = 0.14f };
            case Feel.Boss:    return new Feels { ride = true, hat = true, brass = true, rideAmp = 0.1f, kickAmp = 0.2f, bassAmp = 0.55f, walk = 1f, piano = 0.45f, pianoAmp = 0.42f, pianoDecay = 0.35f, horn = 0.35f, hornEntry = 8, room = 0.16f };
            case Feel.Hot:     // the lead of the remix: a hot band in 4, the ride riding, taps on 2 and 4, the horn from the second phrase, a bomb now and then
                return new Feels { ride = true, taps = true, hat = true, bombs = true, rideAmp = 0.1f, kickAmp = 0.15f, bassAmp = 0.52f, walk = 1f, piano = 0.6f, pianoAmp = 0.5f, pianoDecay = 0.35f, horn = 0.65f, hornEntry = 4, room = 0.2f };
        }
        return default;
    }

    // ------------------------------------------------------------ harmony

    /// One chord a bar in two four-bar cycles. Major keys play the blues (dominant sevenths); minor keys stay modal
    /// (natural minor, no raised seventh). A tag of leftover beats gets the cadence chord
    static Chord[] ChordsPerBeat(Chart c, int beats, int bars)
    {
        var at = new Chord[beats];
        bool heavy = c.feel == Feel.Boss;
        for (int b = 0; b < beats; b++)
        {
            int bar = b / 4;
            Chord ch;
            if (bar >= bars) ch = c.minor ? Degree(c, 10, 2) : Degree(c, 7, 2);
            else
            {
                int cycle = (bar / 4) & 1, pos = bar % 4;
                if (!c.minor)
                    ch = cycle == 0 ? (pos == 0 ? Degree(c, 0, 2) : pos == 1 ? Degree(c, 5, 2) : pos == 2 ? Degree(c, 0, 2) : Degree(c, 7, 2))   // I7  IV7 I7  V7
                                    : (pos == 0 ? Degree(c, 5, 2) : pos == 1 ? Degree(c, 5, 2) : pos == 2 ? Degree(c, 0, 2) : Degree(c, 7, 2));  // IV7 IV7 I7  V7
                else if (heavy)
                    ch = cycle == 0 ? (pos == 0 ? Degree(c, 0, 1) : pos == 1 ? Degree(c, 0, 1) : pos == 2 ? Degree(c, 5, 1) : Degree(c, 10, 2))  // i  i   iv  VII7
                                    : (pos == 0 ? Degree(c, 0, 1) : pos == 1 ? Degree(c, 8, 0) : pos == 2 ? Degree(c, 2, 3) : Degree(c, 7, 1));  // i  VI  iiø v7
                else
                    ch = cycle == 0 ? (pos == 0 ? Degree(c, 0, 1) : pos == 1 ? Degree(c, 5, 1) : pos == 2 ? Degree(c, 0, 1) : Degree(c, 10, 2))  // i  iv  i   VII7
                                    : (pos == 0 ? Degree(c, 0, 1) : pos == 1 ? Degree(c, 8, 0) : pos == 2 ? Degree(c, 2, 3) : Degree(c, 7, 1));  // i  VI  iiø v7
            }
            at[b] = ch;
        }
        return at;
    }

    static Chord Degree(Chart c, int semis, int kind) => new Chord { root = (c.tonic + semis) % 12, kind = kind };

    /// Rootless shells for the piano (3 5 7 9 / b3 5 b7 9 / 3 b7 9 13 / b3 b5 b7 11), root-position stacks for the brass
    static int[] Voicing(Chord ch, bool brass)
    {
        int[] iv;
        if (brass) iv = Tones[ch.kind];
        else switch (ch.kind)
        {
            case 0: iv = new[] { 4, 7, 11, 14 }; break;
            case 1: iv = new[] { 3, 7, 10, 14 }; break;
            case 2: iv = new[] { 4, 10, 14, 21 }; break;
            default: iv = new[] { 3, 6, 10, 17 }; break;
        }
        int lo = brass ? 55 : 57, hi = brass ? 72 : 76;
        var notes = new int[iv.Length];
        for (int i = 0; i < iv.Length; i++)
        {
            int n = 48 + ch.root + iv[i];
            while (n < lo) n += 12;
            while (n > hi) n -= 12;
            notes[i] = n;
        }
        Array.Sort(notes);
        return notes;
    }

    static bool InScale(int midi, Chart c, int[] scale)
    {
        int pc = ((midi - c.tonic) % 12 + 12) % 12;
        return Array.IndexOf(scale, pc) >= 0;
    }

    static bool IsChordTone(int midi, Chord ch)
    {
        int pc = ((midi - ch.root) % 12 + 12) % 12;
        var t = Tones[ch.kind];
        return pc == t[0] || pc == t[1] || pc == t[2] || pc == t[3] || (pc == 2 && ch.kind != 3); // the ninth belongs, except over the half-diminished
    }

    static int Snap(int n, Chord ch)
    {
        int up = n, down = n;
        while (!IsChordTone(up, ch)) up++;
        while (!IsChordTone(down, ch)) down--;
        return up - n <= n - down ? up : down;
    }

    // ------------------------------------------------------------ drums

    static void Drums(Ctx ctx, Feels p, int beats, System.Random rng)
    {
        if (p.bed) BrushBed(ctx, 0.05f);
        for (int b = 0; b < beats; b++)
        {
            int inBar = b & 3;
            bool strong = inBar == 0 || inBar == 2;
            if (strong) Kick(ctx, b, p.kickAmp * (inBar == 0 ? 1f : 0.85f));
            else if (p.kickAll > 0f) Kick(ctx, b, p.kickAmp * p.kickAll);
            if (p.hat && !strong) Chick(ctx, b, 0.06f);
            if (p.taps && !strong) Tap(ctx, b, 0.11f, rng);
            if (p.ride)
            {
                if (p.rideLite) { if (!strong) RideHit(ctx, b, p.rideAmp, true); }               // just the "ding" on 2 and 4
                else
                {
                    RideHit(ctx, b, p.rideAmp * (strong ? 0.9f : 1.1f), !strong);               // ding, DING, ding, DING
                    if (!strong || p.rideBusy) RideHit(ctx, b + ctx.swing, p.rideAmp * (strong ? 0.5f : 0.65f), false); // -a (the skip)
                }
            }
            if (p.bombs && inBar == 3 && (b / 4) % 2 == 1) { Tap(ctx, b + ctx.swing, 0.2f, rng); Kick(ctx, b + ctx.swing, p.kickAmp * 0.8f); }
        }
    }

    /// A feathered kick: barely there, felt on 1 and 3
    static void Kick(Ctx ctx, double beatPos, float amp)
    {
        int at = ctx.At(beatPos), n = (int)(0.16f * ctx.rate);
        float dt = 1f / ctx.rate;
        double ph = 0;
        for (int i = 0; i < n && at >= 0; i++, at = ctx.Step(at))
        {
            float t = i * dt;
            ph += (44.0 + 40.0 * Math.Exp(-t * 35.0)) * dt;
            float env = (float)Math.Exp(-t * 16.0) * Math.Min(1f, i / 30f);
            ctx.buf[at] += Osc(ph) * env * amp;
        }
    }

    /// The hi-hat under the foot: a 20 ms chick
    static void Chick(Ctx ctx, double beatPos, float amp)
    {
        int at = ctx.At(beatPos), n = (int)(0.02f * ctx.rate);
        float last = 0f, dt = 1f / ctx.rate;
        for (int i = 0; i < n && at >= 0; i++, at = ctx.Step(at))
        {
            float nz = ctx.Noise(), hp = nz - last; last = nz;
            ctx.buf[at] += hp * (float)Math.Exp(-i * dt * 220.0) * amp;
        }
    }

    /// A brush tap on the snare: low noise with a soft head tone
    static void Tap(Ctx ctx, double beatPos, float amp, System.Random rng)
    {
        int at = ctx.At(beatPos), n = (int)(0.09f * ctx.rate);
        float lp = 0f, dt = 1f / ctx.rate;
        double ph = 0;
        float vel = 0.8f + 0.2f * (float)rng.NextDouble();
        for (int i = 0; i < n && at >= 0; i++, at = ctx.Step(at))
        {
            float t = i * dt;
            lp += (ctx.Noise() - lp) * 0.22f;
            ph += 185.0 * dt;
            float v = lp * (float)Math.Exp(-t * 45.0) + Osc(ph) * 0.35f * (float)Math.Exp(-t * 60.0);
            ctx.buf[at] += v * amp * vel;
        }
    }

    /// The brushes circling the snare head: low-passed noise swelling on 2 and 4, under everything
    static void BrushBed(Ctx ctx, float amp)
    {
        float lp = 0f;
        double inv = 1.0 / ctx.beat;
        for (int i = 0; i < ctx.len; i++)
        {
            lp += (ctx.Noise() - lp) * 0.18f;
            double ph = (i * inv - 1.0) * 0.5;                 // one circle every two beats, peaking on 2 and 4
            float swell = 0.5f + 0.5f * Osc(ph + 0.25);
            float e = 0.25f + 0.75f * swell * swell;
            ctx.buf[i] += lp * e * amp;
        }
    }

    /// The ride: four inharmonic partials for the bell and band-passed noise for the sizzle, a long shimmer
    static void RideHit(Ctx ctx, double beatPos, float amp, bool accent)
    {
        int at = ctx.At(beatPos), n = (int)(0.75f * ctx.rate);
        float dt = 1f / ctx.rate;
        double p1 = 0, p2 = 0, p3 = 0, p4 = 0;
        float low = 0f, band = 0f, f = 2f * (float)Math.Sin(Math.PI * 4200.0 / ctx.rate), q = 0.7f;
        float a = amp * (accent ? 1.2f : 1f);
        for (int i = 0; i < n && at >= 0; i++, at = ctx.Step(at))
        {
            float t = i * dt;
            p1 += 2470.0 * dt; p2 += 3190.0 * dt; p3 += 4030.0 * dt; p4 += 5460.0 * dt;
            float ring = (Osc(p1) + 0.7f * Osc(p2) + 0.5f * Osc(p3) + 0.3f * Osc(p4)) * (float)Math.Exp(-t * 2.8) * 0.22f;
            low += f * band;
            float high = ctx.Noise() - low - q * band;
            band += f * high;
            float sizzle = band * ((float)Math.Exp(-t * 9.0) * 0.8f + (float)Math.Exp(-t * 2.2) * 0.25f);
            float env = Math.Min(1f, i / 12f);
            ctx.buf[at] += (ring + sizzle) * env * a;
        }
    }

    // ------------------------------------------------------------ bass

    static void BassLine(Ctx ctx, Feels p, Chord[] chords, int beats, System.Random rng)
    {
        int prev = -1;
        int all = chords.Length;
        for (int b = 0; b < beats; b++)
        {
            var ch = chords[b];
            var next = chords[(b + 1) % all];
            int inBar = b & 3;
            int root = BassRange(36 + ch.root);
            int third = BassRange(root + Tones[ch.kind][1]);
            int fifth = BassRange(root + Tones[ch.kind][2]);
            int seventh = BassRange(root + Tones[ch.kind][3]);
            int nextRoot = BassRange(36 + next.root);
            bool changes = next.root != ch.root || next.kind != ch.kind;
            float amp = p.bassAmp * (0.85f + 0.15f * (float)rng.NextDouble());
            int note;
            if (p.walk < 0.5f)
            {
                // the lobby and the ballad: roots and fifths in half notes, a walk-up when the chord is about to change
                if (inBar == 0) { note = root; BassNote(ctx, b, 1.9, note, amp); }
                else if (inBar == 2)
                {
                    note = rng.NextDouble() < 0.7 ? fifth : third;
                    if (changes && rng.NextDouble() < 0.5) { BassNote(ctx, b, 0.95, note, amp * 0.9f); BassNote(ctx, b + 1, 0.95, Approach(nextRoot, rng), amp * 0.85f); }
                    else BassNote(ctx, b, 1.9, note, amp * 0.9f);
                }
                else continue;
                prev = note;
                continue;
            }
            // walking: the root, a step, the fifth (or a skip up the octave), an approach to the next root
            switch (inBar)
            {
                case 0: note = prev == root && rng.NextDouble() < 0.4 ? fifth : root; break;
                case 1: note = rng.NextDouble() < 0.6 ? third : BassRange(root + 2); break;
                case 2: note = rng.NextDouble() < 0.12 ? BassRange(root + 12) : rng.NextDouble() < 0.7 ? fifth : seventh; break;
                default: note = changes ? Approach(nextRoot, rng) : rng.NextDouble() < 0.5 ? fifth : BassRange(root - 2); break;
            }
            BassNote(ctx, b, 0.95, note, amp * (inBar == 0 ? 1f : 0.9f));
            prev = note;
        }
    }

    static int BassRange(int midi)
    {
        while (midi < 33) midi += 12;
        while (midi > 52) midi -= 12;
        return midi;
    }

    /// The note before a new root: a semitone below, a semitone above, or its own dominant
    static int Approach(int target, System.Random rng)
    {
        double r = rng.NextDouble();
        return BassRange(r < 0.45 ? target - 1 : r < 0.75 ? target + 1 : target + 7);
    }

    /// A plucked upright: fast attack, a long decay, the pitch settling in the first 20 ms, a thumb thump at the start
    static void BassNote(Ctx ctx, double beatPos, double durBeats, int midi, float amp)
    {
        int at = ctx.At(beatPos);
        float dur = (float)(durBeats * ctx.beat) / ctx.rate;
        int n = (int)((dur + 0.06f) * ctx.rate);
        float f0 = Freq(midi), dt = 1f / ctx.rate;
        double ph = 0;
        for (int i = 0; i < n && at >= 0; i++, at = ctx.Step(at))
        {
            float t = i * dt;
            ph += f0 * (1.0 + 0.015 * Math.Exp(-t * 60.0)) * dt;
            float env = Math.Min(1f, i / 40f) * (float)Math.Exp(-t * 2.2) * (t < dur ? 1f : Math.Max(0f, 1f - (t - dur) / 0.06f));
            float v = Osc(ph) + 0.3f * Osc(ph * 2.0) * (float)Math.Exp(-t * 7.0) + 0.12f * Osc(ph * 3.0) * (float)Math.Exp(-t * 11.0);
            if (i < 90) v += ctx.Noise() * (1f - i / 90f) * 0.25f;
            ctx.buf[at] += v * env * amp;
        }
    }

    // ------------------------------------------------------------ piano

    // Comping cells, in eighths of the bar (odd eighths swing): the Charleston, pushes, half notes; 7 = the and-of-four, playing the chord that is coming
    static readonly int[][] Cells = { new int[0], new[] { 0, 3 }, new[] { 3 }, new[] { 1, 5 }, new[] { 0, 4 }, new[] { 0, 3, 7 }, new[] { 2, 7 } };
    static readonly int[][] SlowCells = { new int[0], new[] { 0 }, new[] { 0, 4 }, new[] { 3 }, new[] { 1 }, new[] { 0, 7 } };

    static void Comping(Ctx ctx, Feels p, Chord[] chords, int beats, System.Random rng)
    {
        if (p.piano <= 0f) return;
        bool slow = p.walk < 0.5f;
        var cells = slow ? SlowCells : Cells;
        int all = chords.Length;
        for (int bar = 0; bar * 4 < beats; bar++)
        {
            int b0 = bar * 4;
            int barBeats = Math.Min(4, beats - b0);
            int[] cell = rng.NextDouble() > p.piano ? cells[0] : cells[1 + rng.Next(cells.Length - 1)];
            if (barBeats < 4) cell = new[] { 0 }; // the tag: one hit on the dominant
            for (int k = 0; k < cell.Length; k++)
            {
                int e = cell[k];
                int beatInBar = e >> 1;
                if (beatInBar >= barBeats) continue;
                double pos = b0 + ctx.Eighth(e);
                bool push = e == 7;
                var ch = push ? chords[(b0 + 4) % all] : chords[b0 + beatInBar];
                double untilNext = k + 1 < cell.Length ? ctx.Eighth(cell[k + 1]) - ctx.Eighth(e) : slow ? 4.0 - ctx.Eighth(e) : 1.2;
                double dur = Math.Max(0.3, Math.Min(untilNext, slow ? 3.5 : 1.5));
                var notes = Voicing(ch, false);
                if (!slow && rng.NextDouble() < 0.3) notes = Drop(notes, rng);
                PianoChord(ctx, pos, dur, notes, p.pianoAmp * 0.16f * (push ? 0.9f : 1f), p.pianoDecay, rng);
            }
        }
    }

    static int[] Drop(int[] notes, System.Random rng)
    {
        var r = new int[notes.Length - 1];
        int skip = rng.Next(notes.Length), j = 0;
        for (int i = 0; i < notes.Length; i++) if (i != skip) r[j++] = notes[i];
        return r;
    }

    /// A chord on the piano: additive partials that fade fastest at the top, a hammer, the notes landing a few ms apart like a hand
    static void PianoChord(Ctx ctx, double beatPos, double durBeats, int[] notes, float amp, float decay, System.Random rng)
    {
        float dur = (float)(durBeats * ctx.beat) / ctx.rate;
        int n = (int)Math.Min((dur + 0.05f) * ctx.rate, decay * 4f * ctx.rate);
        float dt = 1f / ctx.rate;
        float per = amp / (float)Math.Sqrt(notes.Length);
        foreach (int midi in notes)
        {
            int at = ctx.At(beatPos + rng.NextDouble() * 0.012);
            float f = Freq(midi);
            double ph = 0;
            float vel = per * (0.85f + 0.3f * (float)rng.NextDouble());
            for (int i = 0; i < n && at >= 0; i++, at = ctx.Step(at))
            {
                float t = i * dt;
                ph += f * dt;
                float env = Math.Min(1f, i / 50f) * (float)Math.Exp(-t / decay) * (t < dur ? 1f : Math.Max(0f, 1f - (t - dur) / 0.05f));
                float v = Osc(ph) + 0.5f * Osc(ph * 2.0) * (float)Math.Exp(-t * 3.0) + 0.28f * Osc(ph * 3.0) * (float)Math.Exp(-t * 5.0)
                        + 0.14f * Osc(ph * 4.0) * (float)Math.Exp(-t * 8.0) + 0.07f * Osc(ph * 6.0) * (float)Math.Exp(-t * 14.0);
                if (i < 150) v += ctx.Noise() * (1f - i / 150f) * 0.2f;
                ctx.buf[at] += v * env * vel;
            }
        }
    }

    // ------------------------------------------------------------ the horn

    /// Phrases of two to four bars with rests between, swung eighths and quarters, chord tones on the strong beats, a blue note now and then
    static void HornSolo(Ctx ctx, Chart c, Feels p, Chord[] chords, int beats, int bars, System.Random rng)
    {
        int[] scale = c.minor ? MinorScale : MajorScale;
        bool fast = ctx.beat / ctx.rate < 0.42; // over ~140: more quarters, fewer runs
        int pitch = 67;
        int bar = p.hornEntry;
        int all = chords.Length;
        while (bar < bars)
        {
            int phraseLen = Math.Min(2 + rng.Next(3), bars - bar);
            int e0 = bar * 8, eEnd = (bar + phraseLen) * 8 - 2; // the phrase ends before the last beat of its last bar
            int e = e0 + (rng.NextDouble() < 0.4 ? 1 : 0) + (rng.NextDouble() < 0.3 ? 2 : 0);
            float arc = 0.75f + 0.25f * (float)rng.NextDouble();
            int count = 0;
            while (e < eEnd)
            {
                int beat = e >> 1;
                bool onBeat = (e & 1) == 0;
                var ch = chords[beat % all];
                double r = rng.NextDouble();
                int len; bool rest = false;
                if (!onBeat) len = 1;
                else if (r < (fast ? 0.42 : 0.3)) len = 2;
                else if (r < 0.75) len = 1;
                else if (r < 0.88) len = 3;
                else { len = 1; rest = true; }
                if (!rest)
                {
                    bool strong = onBeat && (beat & 1) == 0;
                    pitch = NextPitch(pitch, c, ch, scale, strong, rng);
                    bool last = e + len >= eEnd;
                    if (last) { len = 3 + rng.Next(3); pitch = Snap(pitch, ch); }
                    double start = ctx.Eighth(e), end = ctx.Eighth(e + len);
                    float vel = arc * (0.8f + 0.2f * (float)Math.Sin(Math.PI * (e - e0) / (double)(eEnd - e0)));
                    HornNote(ctx, start, (end - start) * (last ? 0.96 : 0.9), pitch, vel * p.horn * 0.5f, count == 0 || last);
                    count++;
                }
                e += len;
                if (rng.NextDouble() < 0.12) e += 1 + rng.Next(2); // breathe
            }
            bar += phraseLen + (p.horn > 0.6f ? 1 + rng.Next(2) : 2 + rng.Next(3));
        }
    }

    static int NextPitch(int prev, Chart c, Chord ch, int[] scale, bool strong, System.Random rng)
    {
        double r = rng.NextDouble();
        if (r < 0.06) return Clamp(prev + (rng.Next(2) == 0 ? 1 : -1), 58, 81); // a blue note: a chromatic neighbour, passing
        int dir = rng.NextDouble() < 0.5 ? 1 : -1;
        if (prev > 76) dir = -1; else if (prev < 61) dir = 1;
        int steps = r < 0.7 ? 1 : r < 0.9 ? 2 + rng.Next(2) : 4 + rng.Next(2); // a step, a third, a leap
        int n = prev;
        for (int s = 0; s < steps; s++)
            do { n += dir; } while (!InScale(n, c, scale));
        if (strong && !IsChordTone(n, ch) && rng.NextDouble() < 0.8) n = Snap(n, ch);
        return Clamp(n, 58, 81);
    }

    static readonly Dictionary<int, float[]> pulseTables = new Dictionary<int, float[]>();
    static readonly Dictionary<int, float[]> sawTables = new Dictionary<int, float[]>();

    /// A band-limited pulse (one period), harmonics up to the given count, so no note aliases (shared with the hotel room's horn)
    public static float[] PulseTable(int harmonics, float width)
    {
        lock (pulseTables)
        {
            if (pulseTables.TryGetValue(harmonics, out var t)) return t;
            t = new float[TabSize];
            float peak = 0f;
            for (int i = 0; i < TabSize; i++)
            {
                double x = i / (double)TabSize, v = 0;
                for (int h = 1; h <= harmonics; h++) v += 2.0 / (Math.PI * h) * Math.Sin(Math.PI * h * width) * Math.Cos(2.0 * Math.PI * h * x);
                t[i] = (float)v;
                peak = Math.Max(peak, Math.Abs(t[i]));
            }
            for (int i = 0; i < TabSize; i++) t[i] /= peak;
            pulseTables[harmonics] = t;
            return t;
        }
    }

    /// A band-limited saw (one period of TabSize samples, peak 1), shared with the bed's stabs
    public static float[] SawTable(int harmonics)
    {
        lock (sawTables)
        {
            if (sawTables.TryGetValue(harmonics, out var t)) return t;
            t = new float[TabSize];
            float peak = 0f;
            for (int i = 0; i < TabSize; i++)
            {
                double x = i / (double)TabSize, v = 0;
                for (int h = 1; h <= harmonics; h++) v += (h % 2 == 1 ? 1.0 : -1.0) / h * Math.Sin(2.0 * Math.PI * h * x);
                t[i] = (float)v;
                peak = Math.Max(peak, Math.Abs(t[i]));
            }
            for (int i = 0; i < TabSize; i++) t[i] /= peak;
            sawTables[harmonics] = t;
            return t;
        }
    }

    /// Read a wavetable at a phase in cycles
    public static float Wave(float[] table, double cycles) => Tab(table, cycles);

    /// The muted trumpet: a pulse through a resonant low-pass that opens over the note (the wah), vibrato arriving late, a scoop up into the first note of a phrase
    static void HornNote(Ctx ctx, double beatPos, double durBeats, int midi, float amp, bool scoop)
    {
        float f0 = Freq(midi);
        var table = PulseTable(Math.Max(3, Math.Min(40, (int)(6500f / f0))), 0.35f);
        float dur = (float)(durBeats * ctx.beat) / ctx.rate;
        int n = (int)((dur + 0.07f) * ctx.rate);
        int at = ctx.At(beatPos);
        float dt = 1f / ctx.rate;
        double ph = 0;
        float low = 0f, band = 0f, q = 0.7f;
        float attack = 0.02f * ctx.rate, openTime = Math.Max(0.08f, dur * 0.6f);
        for (int i = 0; i < n && at >= 0; i++, at = ctx.Step(at))
        {
            float t = i * dt;
            float vib = 1f + 0.0045f * Osc(5.6 * t) * Math.Min(1f, Math.Max(0f, (t - 0.12f) * 4f));
            float glide = scoop ? 1f - 0.03f * (float)Math.Exp(-t * 45.0) : 1f;
            ph += f0 * vib * glide * dt;
            float raw = Tab(table, ph);
            float open = Math.Min(1f, t / openTime);
            float cut = 650f + 1900f * open * open + 300f * Osc(5.6 * t) * open;
            float fc = 2f * (float)Math.Sin(Math.PI * Math.Min(cut, 4500f) / ctx.rate);
            low += fc * band;
            float high = raw - low - q * band;
            band += fc * high;
            float env = Math.Min(1f, i / attack) * (t < dur ? 1f : Math.Max(0f, 1f - (t - dur) / 0.07f));
            ctx.buf[at] += (float)Math.Tanh(low * 2.2) * 0.6f * env * amp;
        }
    }

    // ------------------------------------------------------------ brass

    static void BrassHits(Ctx ctx, Chord[] chords, int beats, int bars, System.Random rng)
    {
        int all = chords.Length;
        for (int bar = 0; bar < bars; bar++)
        {
            int b0 = bar * 4;
            var here = Voicing(chords[b0], true);
            var coming = Voicing(chords[(b0 + 4) % all], true);
            if (bar % 8 == 7)
            {   // the figure into the next eight: bap, ba-DAH
                BrassHit(ctx, b0 + ctx.Eighth(4), here, 0.5f, 0.18f);
                BrassHit(ctx, b0 + ctx.Eighth(6), here, 0.5f, 0.18f);
                BrassHit(ctx, b0 + ctx.Eighth(7), coming, 0.7f, 0.35f);
                continue;
            }
            if (bar % 2 == 0) BrassHit(ctx, b0, here, 0.55f, 0.22f);
            else if (rng.NextDouble() < 0.6) BrassHit(ctx, b0 + ctx.Eighth(7), coming, 0.6f, 0.3f);
            if (bar % 4 == 2 && rng.NextDouble() < 0.5) BrassHit(ctx, b0 + ctx.Eighth(3), here, 0.45f, 0.16f);
        }
    }

    /// A section hit: three detuned band-limited saws a note, a short decay, a slight fall
    static void BrassHit(Ctx ctx, double beatPos, int[] notes, float amp, float decay)
    {
        int n = (int)(decay * 3.5f * ctx.rate);
        float dt = 1f / ctx.rate;
        float per = amp / (float)Math.Sqrt(notes.Length) * 0.33f;
        foreach (int midi in notes)
        {
            float f0 = Freq(midi);
            var table = SawTable(Math.Max(4, Math.Min(48, (int)(7000f / f0))));
            int at = ctx.At(beatPos);
            double pa = 0, pb = 0.33, pc = 0.66;
            float lp = 0f;
            for (int i = 0; i < n && at >= 0; i++, at = ctx.Step(at))
            {
                float t = i * dt;
                double step = f0 * (1f - 0.02f * t / decay) * dt;
                pa += step * 1.006; pb += step * 0.994; pc += step;
                float v = Tab(table, pa) + Tab(table, pb) + Tab(table, pc);
                lp += (v - lp) * 0.45f;
                float env = Math.Min(1f, i / (0.006f * ctx.rate)) * (float)Math.Exp(-t / decay);
                ctx.buf[at] += lp * env * per;
            }
        }
    }

    // ------------------------------------------------------------ the ending

    /// A song's last bar: the band lands together on its first beat — the tonic shell on the piano, the root on the bass,
    /// a crash on the ride, the horn holding the fifth — and rings out through the rest of the bar
    static void FinalHit(Ctx ctx, Chart c, Feels p, int atBeat, System.Random rng)
    {
        var tonic = new Chord { root = c.tonic, kind = c.minor ? 1 : 0 };
        Kick(ctx, atBeat, p.kickAmp * 1.3f);
        if (p.taps) Tap(ctx, atBeat, 0.16f, rng);
        RideHit(ctx, atBeat, Math.Max(p.rideAmp, 0.08f) * 1.6f, true);
        BassNote(ctx, atBeat, 3.4, BassRange(36 + c.tonic), p.bassAmp);
        if (p.piano > 0f) PianoChord(ctx, atBeat, 3.6, Voicing(tonic, false), p.pianoAmp * 0.2f, Math.Max(p.pianoDecay, 1.2f), rng);
        if (p.horn > 0f) HornNote(ctx, atBeat + 0.02, 2.6, 67 + ((c.tonic + 7) % 12 + 5 - 67 % 12 + 12) % 12, p.horn * 0.45f, true); // the fifth, nearest to G4
    }

    // ------------------------------------------------------------ the room, the finish

    /// Early reflections (seven taps, alternating sign) and two damped feedback combs; a loop runs them twice round so the tail crosses the join
    static void Room(Ctx ctx, float mix)
    {
        if (mix <= 0f) return;
        int len = ctx.len, rate = ctx.rate;
        var dry = (float[])ctx.buf.Clone();
        var wet = new float[len];
        int[] taps = { Ms(rate, 13.7f), Ms(rate, 21.3f), Ms(rate, 29.9f), Ms(rate, 41.1f), Ms(rate, 53.7f), Ms(rate, 67.3f), Ms(rate, 89.1f) };
        float[] gains = { 0.42f, -0.36f, 0.3f, -0.26f, 0.2f, -0.16f, 0.12f };
        for (int k = 0; k < taps.Length; k++)
        {
            float g = gains[k];
            if (ctx.wrap)
            {
                int j = len - taps[k];
                for (int i = 0; i < len; i++) { wet[i] += dry[j] * g; if (++j == len) j = 0; }
            }
            else for (int i = taps[k]; i < len; i++) wet[i] += dry[i - taps[k]] * g;
        }
        int[] combs = { Ms(rate, 31.7f), Ms(rate, 44.3f) };
        float[] fb = { 0.52f, 0.48f };
        var tail = new float[len];
        int passes = ctx.wrap ? 2 : 1;
        for (int k = 0; k < combs.Length; k++)
        {
            int d = combs[k], w = 0;
            var ring = new float[d];
            float damp = 0f;
            for (int pass = 0; pass < passes; pass++)
                for (int i = 0; i < len; i++)
                {
                    float y = ring[w];
                    damp += (y - damp) * 0.35f;
                    ring[w] = wet[i] * 0.6f + dry[i] * 0.3f + damp * fb[k];
                    if (++w == d) w = 0;
                    if (pass == passes - 1) tail[i] += y;
                }
        }
        for (int i = 0; i < len; i++) ctx.buf[i] = dry[i] + (wet[i] + tail[i] * 0.5f) * mix;
    }

    static int Ms(int rate, float ms) => Math.Max(1, (int)(rate * ms / 1000f));

    /// Normalise, then a gentle soft knee: peak 0.9, never a clip (shared with the beds)
    public static void Finish(float[] buf)
    {
        float peak = 0f;
        for (int i = 0; i < buf.Length; i++) { float a = buf[i]; if (float.IsNaN(a) || float.IsInfinity(a)) buf[i] = a = 0f; peak = Math.Max(peak, Math.Abs(a)); }
        if (peak < 1e-5f) return;
        float g = 1.25f / peak, norm = 0.9f / (float)Math.Tanh(1.25);
        for (int i = 0; i < buf.Length; i++) buf[i] = (float)Math.Tanh(buf[i] * g) * norm;
    }
}
