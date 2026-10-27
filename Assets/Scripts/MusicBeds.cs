using System;
using System.Collections.Generic;

/// <summary>
/// THE BEDS: what the remix puts under the hotel room's lead when the game ramps — "EDM jazz" in
/// the jazz-hop sense, never a four-on-the-floor rave. Every bed follows the lead's PLAN (the
/// same kick pattern, the same bass ostinato, the same chords), on the lead's beats, exactly the
/// lead's length, one clip per kind:
///   Hard        COMBAT: the beat hardens — a modern kick on the lead's kicks, snare + clap lazily on
///               2 and 4, an 808 sub doubling the bass ostinato, swung eighth hats with 16th/32nd
///               rolls into the phrase, brass stabs on the ii–V turnarounds, the strings swelling
///               across each phrase, a crash on the form, a riser and the stutter edit into it
///   Breaks      FLOW: double time — a two-step break at twice the pulse (the snare lands on the
///               lead's backbeat), ghost notes, 16th hats, everything the hard bed has
///   Boss        the strings cinematic (with the low octave) and the hardest beat: kick layered on
///               more of the bar, snare + clap + rim, rolls every four bars, the stab figure every
///               other bar, a timpani boom on every phrase
///   BossBreak   half time: kick, snare on 3, the 808 in whole notes, the strings held, a riser
///   BossClimax  the break under the boss's stabs on every bar, the strings fortissimo, crashes
/// The 808 is side-chained under every kick in the render; the lead is pumped live by MusicPump.
/// </summary>
public static class MusicBeds
{
    const int Rate = MusicRemix.Rate;

    public class Spec
    {
        public MusicHotel.Plan plan; public double[] beats; public int downbeat; public float length, bpm; public MusicRemix.Bed kind; public int seed; public MusicRemix.Slices slices;
        public double Beat(int i)
        {
            if (beats == null || beats.Length < 2) return i * 60.0 / bpm;
            if (i < 0) return beats[0] + i * (beats[1] - beats[0]);
            int last = beats.Length - 1;
            if (i > last) return beats[last] + (i - last) * (beats[last] - beats[last - 1]);
            return beats[i];
        }
        public double BeatAt(double x) { int i = (int)Math.Floor(x); double f = x - i; return Beat(i) + f * (Beat(i + 1) - Beat(i)); }
        public double S(int bar, double beat) => BeatAt(downbeat + 4 * bar + beat) * Rate;
    }

    static readonly float[] Accent16 = { 1f, 0.42f, 0.68f, 0.42f };

    /// A bed of exactly the lead's length, mono, peak 0.9
    public static float[] Render(Spec sp)
    {
        var p = sp.plan;
        int len = (int)Math.Round(sp.length * (double)Rate);
        var buf = new float[len];
        var sub = new float[len];
        var g = new MusicBedVoices.Gen(sp.seed);
        var rng = new Random(sp.seed);
        var kicks = new List<int>();
        var kind = sp.kind;
        bool two = kind == MusicRemix.Bed.Breaks || kind == MusicRemix.Bed.BossClimax;
        bool boss = kind == MusicRemix.Bed.Boss || kind == MusicRemix.Bed.BossClimax;
        bool hard = kind == MusicRemix.Bed.Hard || kind == MusicRemix.Bed.Boss;
        bool half = kind == MusicRemix.Bed.BossBreak;
        bool stutter = kind != MusicRemix.Bed.BossBreak;
        float swing = p.swing;
        int bars = 0;
        while (sp.S(bars, 0) < len - Rate * 0.05 && bars < p.bars) bars++;
        int prevSub = -1;

        for (int k = 0; k < bars; k++)
        {
            int f = k % MusicHotel.Plan.Form, p8 = k & 7;
            bool turn = p8 >= 6, formStart = f == 0, phraseStart = p8 == 0;
            int At(double beat) => (int)sp.S(k, beat);
            void K(double beat, float amp) { int at = At(beat); MusicBedVoices.Kick(g, buf, at, amp); kicks.Add(at); }

            // ---- kicks: the lead's own pattern, hardened
            if (hard)
            {
                K(0, 0.62f); K(1.75, 0.5f);
                if (p.kick35[k] || boss) K(2.5, 0.45f);
                if (boss && (k & 1) == 1) K(0.5, 0.33f);
                if (p8 == 3 || p8 == 7) K(3.5, 0.4f);
            }
            if (half) { K(0, 0.6f); if ((k & 1) == 1) K(2.5, 0.4f); }
            if (two)
                for (int h = 0; h < 2; h++)
                {   // the two-step at twice the pulse: K . . . S . . K . . K . S . . . per half bar in 32nds
                    double hb = 2 * h;
                    K(hb, 0.6f); K(hb + 7 / 8.0, 0.42f); K(hb + 10 / 8.0, 0.48f);
                    MusicBedVoices.Snare(g, buf, At(hb + 0.5), 0.4f); MusicBedVoices.Clap(g, buf, At(hb + 0.5), 0.2f);
                    MusicBedVoices.Snare(g, buf, At(hb + 1.5), 0.44f); MusicBedVoices.Clap(g, buf, At(hb + 1.5), 0.22f);
                    if (rng.NextDouble() < 0.55) MusicBedVoices.Snare(g, buf, At(hb + 0.25), 0.12f);
                    if (rng.NextDouble() < 0.5) MusicBedVoices.Snare(g, buf, At(hb + 1.125), 0.11f);
                    if (rng.NextDouble() < 0.4) MusicBedVoices.Snare(g, buf, At(hb + 1.75), 0.13f);
                }

            // ---- the backbeat, lazily behind 2 and 4
            if (hard)
            {
                MusicBedVoices.Snare(g, buf, At(1.03), 0.36f); MusicBedVoices.Clap(g, buf, At(1.03), 0.22f);
                MusicBedVoices.Snare(g, buf, At(3.03), 0.38f); MusicBedVoices.Clap(g, buf, At(3.03), 0.24f);
                if (boss) { MusicBedVoices.Rim(g, buf, At(1.03), 0.2f); MusicBedVoices.Rim(g, buf, At(3.03), 0.2f); }
                if (p.ghost[k]) MusicBedVoices.Snare(g, buf, At(2.75), 0.1f);
            }
            if (half) { MusicBedVoices.Snare(g, buf, At(2.03), 0.4f); MusicBedVoices.Clap(g, buf, At(2.03), 0.25f); }

            // ---- hats: swung eighths with rolls into the phrase; sixteenths under the break
            if (hard || half)
            {
                for (int q = 0; q < 8; q++) MusicBedVoices.Hat(g, buf, At((q >> 1) + ((q & 1) == 1 ? swing : 0.0)), 0.1f * ((q & 1) == 0 ? 1f : 0.55f), 0.011f);
                if ((k & 1) == 1) MusicBedVoices.Hat(g, buf, At(3 + swing), 0.12f, 0.15f);
                if (p8 == 3 || p8 == 7 || (boss && (k & 3) == 3))
                {
                    for (int q = 0; q < 4; q++) MusicBedVoices.Hat(g, buf, At(3 + q * 0.25), 0.07f + 0.015f * q, 0.011f);
                    for (int q = 4; q < 8; q++) MusicBedVoices.Hat(g, buf, At(3.5 + (q - 4) * 0.125), 0.09f + 0.01f * q, 0.009f);
                }
            }
            if (two)
                for (int q = 0; q < 16; q++)
                {
                    bool open = q == 14;
                    if (open) MusicBedVoices.Hat(g, buf, At(q / 4.0), 0.13f, 0.15f);
                    else MusicBedVoices.Hat(g, buf, At(q / 4.0), 0.095f * Accent16[q & 3], 0.01f);
                }

            // ---- the 808, doubling the bass ostinato an octave down
            if (half)
            {
                var n0 = p.riff[k][0];
                int m = n0.midi - 12; while (m < 24) m += 12;
                MusicBedVoices.Sub808(sub, At(0), (int)(3.6 * (sp.S(k, 1) - sp.S(k, 0))), m, prevSub, 0.45f, 0f);
                prevSub = m;
            }
            else
                foreach (var n in p.riff[k])
                {
                    int m = n.midi - 12; while (m < 24) m += 12;
                    MusicBedVoices.Sub808(sub, At(n.pos), (int)(n.len * (sp.S(k, 1) - sp.S(k, 0))), m, prevSub, 0.5f * n.vel, 0f);
                    prevSub = m;
                }

            // ---- brass stabs: the turnaround figure, and the boss's answer every other bar
            if (turn && !half)
            {
                double[] pos = p8 == 6 ? new[] { 1.5, 3.0 } : new[] { 0.5, 1.5, 2.5 };
                foreach (double q in pos)
                {
                    var ch = p.ChordAt(k, q);
                    MusicBedVoices.Stab(buf, At(q), (int)(0.45 * (sp.S(k, 1) - sp.S(k, 0))), MusicHotel.Voicing(ch, 52, 66, true), boss ? 0.32f : 0.26f, 0f, 3500f, 800f, 0.18f);
                }
            }
            if (boss && !turn && ((k & 1) == 0 || kind == MusicRemix.Bed.BossClimax))
                foreach (double q in new[] { 2.5, 3.0 })
                    MusicBedVoices.Stab(buf, At(q), (int)(0.4 * (sp.S(k, 1) - sp.S(k, 0))), MusicHotel.Voicing(p.ChordAt(k, q), 52, 66, true), 0.28f, 0f, 3000f, 700f, 0.16f);

            // ---- the strings, swelling across each phrase (the boss gets the low octave)
            {
                float lo0 = boss ? 0.22f : half ? 0.18f : 0.07f, hi0 = kind == MusicRemix.Bed.BossClimax ? 0.45f : boss ? 0.4f : half ? 0.18f : 0.22f;
                float a0 = lo0 + (hi0 - lo0) * p8 / 7f, a1 = lo0 + (hi0 - lo0) * Math.Min(7, p8 + 1) / 7f;
                for (int i = 0; i < p.chords[k].Length; i++)
                {
                    var ch = p.chords[k][i];
                    double end = i + 1 < p.chords[k].Length ? p.chords[k][i + 1].pos : 4.0;
                    int l = (int)((end - ch.pos) * (sp.S(k, 1) - sp.S(k, 0)));
                    MusicHotelVoices.Strings(buf, Rate, At(ch.pos), l, MusicHotel.Voicing(ch, 48, 64, false), a0, a1, 600f, boss ? 2500f : 1800f);
                    if (boss) MusicHotelVoices.Strings(buf, Rate, At(ch.pos), l, MusicHotel.Voicing(ch, 36, 50, false), a0 * 0.6f, a1 * 0.6f, 300f, 900f);
                }
            }

            // ---- the form: a crash, the timpani, a riser into it, the stutter edit before it
            if (formStart) MusicBedVoices.Crash(g, buf, At(0), boss ? 0.34f : 0.28f);
            else if (boss && phraseStart) MusicBedVoices.Crash(g, buf, At(0), 0.22f, true);
            if (f == 14) MusicBedVoices.Riser(g, buf, At(0), (int)sp.S(k + 2, 0), half ? 0.26f : 0.2f);
            if (stutter && f == 15 && sp.slices != null)
            {
                int idx = Array.IndexOf(sp.slices.bars, k);
                if (idx >= 0)
                {
                    int e8 = (int)(sp.S(k, 2.5) - sp.S(k, 2)), s16 = (int)(sp.S(k, 3.25) - sp.S(k, 3));
                    MusicBedVoices.Stutter(buf, At(2), sp.slices.eighth[idx], Rate, 2, e8, 0.65f, false);
                    MusicBedVoices.Stutter(buf, At(3), sp.slices.sixteenth[idx], Rate, 4, s16, 0.7f, true);
                }
            }
        }

        // the side-chain on the 808: 70 ms under every kick
        int width = (int)(0.075f * Rate), attack = (int)(0.003f * Rate);
        foreach (int kick in kicks)
            for (int j = 0; j < width; j++)
            {
                int i = kick + j; if (i < 0 || i >= len) break;
                float env = j < attack ? j / (float)attack : 1f - (j - attack) / (float)(width - attack);
                sub[i] *= 1f - 0.85f * env;
            }
        for (int i = 0; i < len; i++) buf[i] += sub[i];
        MusicJazz.Finish(buf);
        return buf;
    }

    /// The stutter bars of a plan: the last bar of every form
    public static int[] StutterBars(MusicHotel.Plan p)
    {
        var l = new List<int>();
        for (int k = MusicHotel.Plan.Form - 1; k < p.bars; k += MusicHotel.Plan.Form) l.Add(k);
        return l.ToArray();
    }

    /// Three beats of riser, cut dead: the fourth beat is the silence before the drop
    public static float[] RenderRiser(float beat, int seed)
    {
        int n = Math.Max(Rate / 4, (int)(3f * beat * Rate));
        var buf = new float[n];
        MusicBedVoices.Riser(new MusicBedVoices.Gen(seed), buf, 0, n, 0.5f);
        MusicJazz.Finish(buf);
        return buf;
    }

    /// The drop itself: a crash and a kick on the first sample
    public static float[] RenderImpact(int seed)
    {
        var buf = new float[(int)(1.7f * Rate)];
        var g = new MusicBedVoices.Gen(seed + 1);
        MusicBedVoices.Crash(g, buf, 0, 0.6f);
        MusicBedVoices.Kick(g, buf, 0, 0.8f);
        MusicJazz.Finish(buf);
        return buf;
    }

    /// Eight seconds of vinyl for the lobby: sparse clicks, a low hiss, a rumble, the groove's tick once a turn (seamless)
    public static float[] RenderCrackle(int seed)
    {
        int n = 8 * Rate;
        var buf = new float[n];
        var g = new MusicBedVoices.Gen(seed);
        var rng = new Random(seed);
        float hiss = 0f, rumble = 0f;
        for (int i = 0; i < n; i++)
        {
            float nz = g.Noise();
            hiss += (nz - hiss) * 0.35f;
            rumble += (nz - rumble) * 0.008f;
            buf[i] = hiss * 0.02f + rumble * 0.08f;
        }
        for (int c = 0; c < 56; c++)
        {
            int at = rng.Next(n), w = 1 + rng.Next(3);
            float a = (float)Math.Pow(rng.NextDouble(), 2.2) * 0.55f + 0.03f;
            if (rng.NextDouble() < 0.06) a = 0.9f;
            for (int i = 0; i < w * 3; i++) { int j = (at + i) % n; buf[j] += a * (float)Math.Exp(-i / (float)w) * (i % 2 == 0 ? 1f : -0.6f); }
        }
        for (float t = 0.3f; t < 8f; t += 60f / 78f)
        {
            int at = (int)(t * Rate);
            for (int i = 0; i < 90; i++) { int j = (at + i) % n; buf[j] += 0.22f * (float)Math.Exp(-i / 25f) * (i % 2 == 0 ? 1f : -0.8f); }
        }
        float peak = 0f;
        for (int i = 0; i < n; i++) peak = Math.Max(peak, Math.Abs(buf[i]));
        if (peak > 1e-5f) for (int i = 0; i < n; i++) buf[i] *= 0.5f / peak;
        return buf;
    }

    /// A bed-only section for BossMusic's synth fallback: a hotel-room plan at bpm in a minor key, fixed grid, no lead
    public static float[] RenderSection(MusicRemix.Bed kind, float bpm, int bars, int tonic, int seed)
    {
        var plan = MusicHotel.MakePlan(new MusicHotel.Chart { name = "section", bpm = bpm, tonic = tonic, bars = bars, seed = seed, stringsFrom = 0 }, Rate);
        var beats = new double[bars * 4 + 1];
        for (int i = 0; i < beats.Length; i++) beats[i] = i * (double)plan.beatSamples / Rate;
        return Render(new Spec { plan = plan, beats = beats, downbeat = 0, length = bars * 4 * plan.beatSamples / (float)Rate, bpm = plan.bpm, kind = kind, seed = seed, slices = new MusicRemix.Slices() });
    }
}
