using System;
using System.Collections.Generic;

/// <summary>
/// THE HOTEL ROOM: the score's lead, a procedural noir-jazz song in the mould of Hareton
/// Salvanini's "Quarto de Hotel" (1974) — slow (70–82), smoky, a hotel room at 3 am. A song
/// is a PLAN (the harmony, the drum feel and the bass ostinato, bar by bar — shared with the
/// beds so their kicks, 808 sub and stabs follow the lead exactly) rendered by the voices in
/// MusicHotelVoices:
///   form      16 bars, two 8-bar phrases, each ending in a ii–V turnaround: i i iv iv | bVI (or
///             bVII7) V7 i iiø–V7 | i bIII bVI iv | bII (the Neapolitan, borrowed) V7 i iiø–V7;
///             a song is 2–3 forms: A (bass, brushes, Rhodes; the horn from bar 9), B (the horn
///             and the strings), A' (the strings stay); the last two bars thin out
///   bass      a hypnotic two-bar ostinato (root, octave, b7, 5 / root, b3, 4, 5, b7) transposed
///             to each chord, legato, the pick on every note
///   drums     a lazy swung boom-bap: a soft kick on 1 and the 'a' of 2 (and 3-and some bars), the
///             brushed snare lazily behind 2 and 4, ghost taps, eighth hats, the brushes circling
///   Rhodes    rootless minor-9th / major-7th / 7b9 shells in lazy cells, pushes into the next bar
///   strings   a pad on the chord, swelling across each phrase
///   horn      phrases of two or three bars with long rests, long notes, a scoop, one bend, a fall
///   tape      a ±6 cent wobble, warmth, a vinyl bed (MusicHotelVoices.Tape)
/// Rendered on MusicRemix's worker at 32 kHz, deterministic by seed, no loop (a song).
/// </summary>
public static class MusicHotel
{
    public class Chart { public string name, pool; public float bpm = 76f; public int tonic = 2; public int bars = 48; public int seed; public float swing = 0.62f; public int stringsFrom = 16; }
    public struct Chord { public float pos; public int root, kind; } // kind 0 maj7, 1 min9, 2 dom7b9, 3 m7b5
    public struct Note { public float pos, len; public int midi; public float vel; }

    public class Plan
    {
        public Chart chart; public int bars, beatSamples; public float bpm, swing;
        public Chord[][] chords; public int[] roots, kinds; public bool[] kick35, ghost; public Note[][] riff;
        public const int Form = 16;
        public int Section(int bar) => bar / Form;
        public bool Turnaround(int bar) => (bar & 7) >= 6;
        public Chord ChordAt(int bar, double beat)
        {
            var cs = chords[Math.Max(0, Math.Min(bars - 1, bar))];
            var c = cs[0];
            for (int i = 1; i < cs.Length; i++) if (cs[i].pos <= beat + 1e-4) c = cs[i];
            return c;
        }
        public double S(int bar, double beat) => (bar * 4 + beat) * beatSamples;
        /// An off-beat eighth lands late
        public double Swung(double beat) { double b = Math.Floor(beat), f = beat - b; return f > 0.49 && f < 0.51 ? b + swing : beat; }
    }

    /// The sides, seeded, per pool: the lobby slow, the floors at walking pace, the boss a shade faster
    public static Chart[] Catalogue() => new[]
    {
        new Chart { name = "three_am", pool = "menu", bpm = 70f, tonic = 7, bars = 32, seed = 11, stringsFrom = 8 },
        new Chart { name = "the_corridor", pool = "menu", bpm = 72f, tonic = 2, bars = 32, seed = 12, stringsFrom = 8 },
        new Chart { name = "quarto_lower_town", pool = "city", bpm = 76f, tonic = 2, bars = 48, seed = 21 },
        new Chart { name = "neon_in_the_rain", pool = "city", bpm = 78f, tonic = 5, bars = 48, seed = 22 },
        new Chart { name = "last_tram", pool = "city", bpm = 74f, tonic = 0, bars = 48, seed = 23 },
        new Chart { name = "steam_mains", pool = "under", bpm = 72f, tonic = 4, bars = 48, seed = 31 },
        new Chart { name = "the_platform", pool = "under", bpm = 76f, tonic = 10, bars = 48, seed = 32 },
        new Chart { name = "sluice_gate", pool = "under", bpm = 80f, tonic = 7, bars = 48, seed = 33 },
        new Chart { name = "the_programme", pool = "office", bpm = 78f, tonic = 9, bars = 48, seed = 41 },
        new Chart { name = "mezzanine", pool = "office", bpm = 80f, tonic = 2, bars = 48, seed = 42 },
        new Chart { name = "ledger", pool = "office", bpm = 76f, tonic = 5, bars = 48, seed = 43 },
        new Chart { name = "the_concierge", pool = "boss", bpm = 78f, tonic = 0, bars = 48, seed = 51, stringsFrom = 0 },
        new Chart { name = "the_stationmaster", pool = "boss", bpm = 82f, tonic = 5, bars = 48, seed = 52, stringsFrom = 0 },
    };

    static Chord D(int t, int semis, int kind, float pos = 0f) => new Chord { pos = pos, root = (t + semis) % 12, kind = kind };

    public static Plan MakePlan(Chart c, int rate)
    {
        var rng = new Random(c.seed);
        int t = c.tonic;
        var p = new Plan { chart = c, bars = c.bars, bpm = c.bpm, swing = c.swing, beatSamples = (int)Math.Round(rate * 60.0 / c.bpm) };
        p.bpm = (float)(rate * 60.0 / p.beatSamples);
        p.chords = new Chord[c.bars][]; p.roots = new int[c.bars]; p.kinds = new int[c.bars]; p.kick35 = new bool[c.bars]; p.ghost = new bool[c.bars]; p.riff = new Note[c.bars][];
        bool fourSeven = rng.NextDouble() < 0.4, neapolitan = rng.NextDouble() < 0.65;
        for (int bar = 0; bar < c.bars; bar++)
        {
            int f = bar % Plan.Form;
            bool lastForm = bar + Plan.Form > c.bars;
            Chord[] cs;
            switch (f)
            {
                case 0: case 1: case 6: case 8: case 14: cs = new[] { D(t, 0, 1) }; break;
                case 2: case 3: case 11: cs = new[] { D(t, 5, 1) }; break;
                case 4: cs = new[] { fourSeven ? D(t, 10, 2) : D(t, 8, 0) }; break;
                case 5: case 13: cs = new[] { D(t, 7, 2) }; break;
                case 9: cs = new[] { D(t, 3, 0) }; break;
                case 10: cs = new[] { D(t, 8, 0) }; break;
                case 12: cs = new[] { neapolitan ? D(t, 1, 0) : D(t, 5, 1) }; break;
                default: cs = lastForm && f == 15 ? new[] { D(t, 0, 1) } : new[] { D(t, 2, 3), D(t, 7, 2, 2f) }; break; // the ii–V
            }
            p.chords[bar] = cs; p.roots[bar] = cs[0].root; p.kinds[bar] = cs[0].kind;
            p.kick35[bar] = rng.NextDouble() < 0.4;
            p.ghost[bar] = rng.NextDouble() < 0.3;
            p.riff[bar] = Riff(cs, bar, rng);
        }
        return p;
    }

    static int BassRoot(int pc) { int m = 36 + pc; while (m < 31) m += 12; while (m > 42) m -= 12; return m; }
    static int Deg(int kind, int deg) => deg == 3 ? (kind == 0 || kind == 2 ? 4 : 3) : deg == 4 ? 5 : deg == 5 ? (kind == 3 ? 6 : 7) : deg == 7 ? (kind == 0 ? 11 : 10) : 0;

    /// The ostinato for a bar: cell A on even bars (root, octave skip, b7, 5), cell B on odd (root, b3, 4, 5, b7 pickup); the ii–V splits the bar
    static Note[] Riff(Chord[] cs, int bar, Random rng)
    {
        float V() => 0.88f + 0.12f * (float)rng.NextDouble();
        if (cs.Length > 1)
        {
            int r1 = BassRoot(cs[0].root), r2 = BassRoot(cs[1].root);
            return new[] { N(0f, 1.4f, r1, V()), N(1.5f, 0.45f, r1 + Deg(cs[0].kind, 3), V()), N(2f, 1.4f, r2, V()), N(3.5f, 0.45f, r2 + Deg(cs[1].kind, 7), V()) };
        }
        int r = BassRoot(cs[0].root), k = cs[0].kind;
        if ((bar & 1) == 0) return new[] { N(0f, 1.4f, r, V()), N(1.5f, 0.45f, r, V() * 0.8f), N(2f, 0.9f, r + Deg(k, 7), V()), N(3f, 0.9f, r + Deg(k, 5), V()), N(3.5f, 0.45f, r + Deg(k, 5), V() * 0.7f) };
        return new[] { N(0f, 0.9f, r, V()), N(1f, 0.45f, r + Deg(k, 3), V() * 0.85f), N(1.5f, 0.45f, r + Deg(k, 4), V() * 0.85f), N(2f, 1.4f, r + Deg(k, 5), V()), N(3.5f, 0.5f, r + Deg(k, 7), V() * 0.8f) };
    }

    static Note N(float pos, float len, int midi, float vel) => new Note { pos = pos, len = len, midi = midi, vel = vel };

    /// Chord notes folded into [lo, hi]: rootless shells with the ninth for the Rhodes and the stabs, root-position for the strings
    public static int[] Voicing(Chord ch, int lo, int hi, bool rootless)
    {
        int[] iv;
        if (rootless) iv = ch.kind == 0 ? new[] { 4, 7, 11, 14 } : ch.kind == 1 ? new[] { 3, 7, 10, 14 } : ch.kind == 2 ? new[] { 4, 10, 13, 7 } : new[] { 3, 6, 10, 13 };
        else iv = ch.kind == 0 ? new[] { 0, 7, 11, 16 } : ch.kind == 1 ? new[] { 0, 7, 10, 15 } : ch.kind == 2 ? new[] { 0, 10, 16, 7 } : new[] { 0, 6, 10, 15 };
        var notes = new int[iv.Length];
        for (int i = 0; i < iv.Length; i++) { int n = 48 + ch.root + iv[i]; while (n < lo) n += 12; while (n > hi) n -= 12; notes[i] = n; }
        Array.Sort(notes);
        return notes;
    }

    static readonly double[][] Cells = { new[] { 0.0 }, new[] { 0.0, 2.5 }, new[] { 0.5 }, new[] { 0.0, 3.5 }, new[] { 1.5, 3.5 }, new[] { 0.5, 2.0 }, new double[0] };

    public static float[] Render(Plan p, int rate)
    {
        int len = p.bars * 4 * p.beatSamples;
        var buf = new float[len];
        uint nz = MusicHotelVoices.Seed(p.chart.seed);
        var rng = new Random(p.chart.seed * 3 + 1);
        double bs = p.beatSamples;
        MusicHotelVoices.BrushBed(buf, rate, bs, 0.02f, ref nz);
        for (int bar = 0; bar < p.bars; bar++)
        {
            int sec = p.Section(bar), p8 = bar & 7;
            bool lastTwo = bar >= p.bars - 2;
            int At(double beat) => (int)p.S(bar, beat);
            // the kit
            MusicHotelVoices.Kick(buf, rate, At(0), 0.2f); MusicHotelVoices.Kick(buf, rate, At(1.75), 0.16f);
            if (p.kick35[bar]) MusicHotelVoices.Kick(buf, rate, At(2.5), 0.13f);
            MusicHotelVoices.Brush(buf, rate, At(1.03), 0.17f, ref nz); MusicHotelVoices.Brush(buf, rate, At(3.03), 0.19f, ref nz);
            if (p.ghost[bar]) MusicHotelVoices.Brush(buf, rate, At(rng.NextDouble() < 0.5 ? 0.75 : 2.75), 0.05f, ref nz);
            for (int q = 0; q < 8; q++) MusicHotelVoices.Hat(buf, rate, At((q >> 1) + ((q & 1) == 1 ? p.swing : 0.0)), 0.04f * ((q & 1) == 0 ? 1f : 0.55f), ref nz);
            // the bass
            foreach (var n in p.riff[bar]) MusicHotelVoices.Bass(buf, rate, At(n.pos), (int)(n.len * bs), n.midi, 0.4f * n.vel, ref nz);
            // the Rhodes, in lazy cells; a push at 3.5 plays the chord that is coming
            var cell = lastTwo ? Cells[0] : Cells[rng.Next(Cells.Length)];
            for (int k = 0; k < cell.Length; k++)
            {
                double pos = cell[k];
                var ch = pos >= 3.5 && bar + 1 < p.bars ? p.chords[bar + 1][0] : p.ChordAt(bar, pos);
                double dur = Math.Min(3.0, (k + 1 < cell.Length ? cell[k + 1] : 4.0) - pos);
                MusicHotelVoices.Rhodes(buf, rate, At(p.Swung(pos)), (int)(dur * bs), Voicing(ch, 55, 74, true), sec == 0 ? 0.15f : 0.19f, 4.6f, rng);
            }
            // the strings, swelling across each phrase
            if (bar >= p.chart.stringsFrom && !lastTwo)
                for (int i = 0; i < p.chords[bar].Length; i++)
                {
                    var ch = p.chords[bar][i];
                    double end = i + 1 < p.chords[bar].Length ? p.chords[bar][i + 1].pos : 4.0;
                    float a0 = 0.06f + 0.06f * p8 / 7f, a1 = 0.06f + 0.06f * Math.Min(7, p8 + 1) / 7f;
                    MusicHotelVoices.Strings(buf, rate, At(ch.pos), (int)((end - ch.pos) * bs), Voicing(ch, 48, 64, false), a0, a1, 650f, 1700f);
                }
        }
        HornPhrases(p, buf, rate, rng);
        MusicHotelVoices.Tape(buf, rate, 6f, 5500f, 0.0035f, p.chart.seed);
        MusicJazz.Finish(buf);
        return buf;
    }

    static readonly double[] Durs = { 1, 1, 1.5, 2, 2, 3, 0.5 };

    /// Phrases of two or three bars with rests between: long notes near the chord, a scoop on the first, one bend, a fall on the last
    static void HornPhrases(Plan p, float[] buf, int rate, Random rng)
    {
        double bs = p.beatSamples;
        int bar = 8, pitch = 67;
        while (bar < p.bars - 2)
        {
            double chance = p.Section(bar) == 0 ? 0.35 : 0.55;
            if (rng.NextDouble() > chance) { bar++; continue; }
            int phraseLen = Math.Min(2 + rng.Next(2), p.bars - 2 - bar);
            double cursor = bar * 4 + (rng.NextDouble() < 0.5 ? 0.0 : rng.NextDouble() < 0.5 ? 0.5 : 1.0), end = (bar + phraseLen) * 4 - 1.0;
            int count = 0, bendAt = rng.Next(2, 5);
            while (cursor < end)
            {
                double dur = Durs[rng.Next(Durs.Length)];
                bool last = cursor + dur >= end - 1.0;
                if (last) dur = Math.Max(2.0, end - cursor);
                int b = (int)(cursor / 4), beat = (int)(cursor - b * 4);
                var ch = p.ChordAt(b, cursor - b * 4);
                pitch = NextPitch(pitch, ch, rng);
                float bend = count == bendAt ? 1f : 0f;
                MusicHotelVoices.Horn(buf, rate, (int)(p.Swung(cursor) * bs), (int)(dur * bs * 0.92), pitch, 0.22f, count == 0, bend, last ? 2f : 0f);
                count++;
                cursor += dur;
                if (rng.NextDouble() < 0.35) cursor += rng.NextDouble() < 0.5 ? 0.5 : 1.0; // breathe
            }
            bar += phraseLen + 1 + rng.Next(2);
        }
    }

    /// A chord tone or the ninth in [60, 76], a step or a third from the last note, now and then a leap
    static int NextPitch(int prev, Chord ch, Random rng)
    {
        var tones = Voicing(ch, 60, 76, true);
        var all = new List<int>();
        foreach (int n in tones) { all.Add(n); if (n + 12 <= 76) all.Add(n + 12); if (n - 12 >= 60) all.Add(n - 12); }
        int target = prev + (rng.NextDouble() < 0.2 ? (rng.NextDouble() < 0.5 ? 5 : -5) : rng.Next(-3, 4));
        int best = all[0]; int bd = int.MaxValue;
        foreach (int n in all) { int d = Math.Abs(n - target) + (n == prev ? 2 : 0); if (d < bd) { bd = d; best = n; } }
        return best;
    }
}
