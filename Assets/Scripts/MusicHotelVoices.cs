using System;

/// <summary>
/// The HOTEL ROOM's instruments and its tape (MusicHotel). A hotel room at 3 am, 1974:
///   Bass     a plucked electric bass — a sine with its second and third harmonic, a pick, the pitch
///            settling in the first 20 ms, a slow decay, everything above 1.5 kHz rolled off
///   Kick     a soft 46 Hz thud, no click; Brush — the brushed snare: a low swish with a head tone;
///            Hat — the foot hat; BrushBed — the brushes circling the head under everything
///   Rhodes   additive tines (the fundamental, a fast-fading second, a tine partial at 3.5x, a bell at
///            5.4x), a hammer, tremolo, the notes of a chord landing a few ms apart like a hand
///   Strings  three detuned band-limited saws a note through a one-pole low-pass that opens slowly,
///            a long attack and release, a swell across the note (also the beds' strings)
///   Horn     the muted trumpet, slowed and softened: a band-limited pulse through a resonant
///            low-pass that opens over the note (the wah), vibrato arriving late, a scoop into the
///            first note of a phrase, a bend up into a note, a fall at the end of a phrase
///   Tape     the lo-fi: a slow pitch wobble (±6 cents at 0.4 Hz over a ±5 cent drift), warmth (a
///            12 dB/oct low-pass), a little saturation, a vinyl bed (hiss and sparse clicks)
/// Every voice writes with bounds checks and no wrap: a song, not a loop. Mono at `rate`.
/// </summary>
public static class MusicHotelVoices
{
    const int TabSize = 4096;
    static readonly float[] SinTab = BuildSin();
    static float[] BuildSin() { var t = new float[TabSize]; for (int i = 0; i < TabSize; i++) t[i] = (float)Math.Sin(2.0 * Math.PI * i / TabSize); return t; }
    static float Osc(double cycles) { double p = cycles - Math.Floor(cycles); return SinTab[(int)(p * TabSize) & (TabSize - 1)]; }
    public static float Freq(int midi) => 440f * (float)Math.Pow(2.0, (midi - 69) / 12.0);
    public static float Freq(float midi) => 440f * (float)Math.Pow(2.0, (midi - 69f) / 12.0);
    static float Noise(ref uint s) { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return (s >> 8) * (1f / 8388608f) - 1f; }
    public static uint Seed(int seed) => ((uint)seed * 2654435761u) | 1u;

    /// The electric bass, plucked: `len` samples of sustain, then a 60 ms release
    public static void Bass(float[] buf, int rate, int at, int len, int midi, float amp, ref uint noise)
    {
        float f0 = Freq(midi), dt = 1f / rate;
        int n = len + (int)(0.06f * rate);
        float lp = 0f, k = 1f - (float)Math.Exp(-2.0 * Math.PI * 1500.0 / rate);
        double ph = 0;
        for (int i = 0; i < n; i++)
        {
            int j = at + i; if (j >= buf.Length) break;
            float t = i * dt;
            ph += f0 * (1.0 + 0.012 * Math.Exp(-t * 50.0)) * dt;
            float env = Math.Min(1f, i / (0.004f * rate)) * (float)Math.Exp(-t / 1.3f) * (i < len ? 1f : Math.Max(0f, 1f - (i - len) / (0.06f * rate)));
            float v = Osc(ph) + 0.35f * Osc(ph * 2.0) * (float)Math.Exp(-t * 2.5) + 0.1f * Osc(ph * 3.0) * (float)Math.Exp(-t * 4.0);
            if (i < 0.005f * rate) v += Noise(ref noise) * (1f - i / (0.005f * rate)) * 0.35f; // the pick
            lp += (v - lp) * k;
            if (j >= 0) buf[j] += (float)Math.Tanh(lp * 1.4) * env * amp;
        }
    }

    /// A soft kick: 46 Hz with a short drop, no click
    public static void Kick(float[] buf, int rate, int at, float amp)
    {
        int n = (int)(0.3f * rate);
        float dt = 1f / rate;
        double ph = 0;
        for (int i = 0; i < n; i++)
        {
            int j = at + i; if (j >= buf.Length) break;
            float t = i * dt;
            ph += (46.0 + 50.0 * Math.Exp(-t / 0.03)) * dt;
            float env = Math.Min(1f, i / (0.003f * rate)) * (float)Math.Exp(-t / 0.18f);
            if (j >= 0) buf[j] += (float)Math.Tanh(Osc(ph) * 1.3) * env * amp;
        }
    }

    /// The brushed snare: a low-passed swish with a soft head tone, slightly different every time
    public static void Brush(float[] buf, int rate, int at, float amp, ref uint noise)
    {
        int n = (int)(0.14f * rate);
        float lp = 0f, dt = 1f / rate, vel = 0.85f + 0.3f * (Noise(ref noise) * 0.5f + 0.5f);
        double ph = 0;
        for (int i = 0; i < n; i++)
        {
            int j = at + i; if (j >= buf.Length) break;
            float t = i * dt;
            lp += (Noise(ref noise) - lp) * 0.2f;
            ph += 190.0 * dt;
            float v = lp * (float)Math.Exp(-t * 28.0) * 1.4f + Osc(ph) * 0.3f * (float)Math.Exp(-t * 55.0);
            if (j >= 0) buf[j] += v * amp * vel;
        }
    }

    /// The foot hat: 18 ms of high-passed noise
    public static void Hat(float[] buf, int rate, int at, float amp, ref uint noise)
    {
        int n = (int)(0.018f * rate);
        float last = 0f;
        for (int i = 0; i < n; i++)
        {
            int j = at + i; if (j >= buf.Length) break;
            float nz = Noise(ref noise), hp = nz - last; last = nz;
            if (j >= 0) buf[j] += hp * (float)Math.Exp(-i / (0.004f * rate)) * amp;
        }
    }

    /// The brushes circling the snare head under everything: low-passed noise swelling on 2 and 4
    public static void BrushBed(float[] buf, int rate, double beatSamples, float amp, ref uint noise)
    {
        float lp = 0f;
        for (int i = 0; i < buf.Length; i++)
        {
            lp += (Noise(ref noise) - lp) * 0.16f;
            float swell = 0.5f + 0.5f * Osc((i / beatSamples - 1.0) * 0.5 + 0.25);
            buf[i] += lp * (0.25f + 0.75f * swell * swell) * amp;
        }
    }

    /// A Rhodes chord: additive tines with a hammer and tremolo; `len` samples until the hand lifts
    public static void Rhodes(float[] buf, int rate, int at, int len, int[] midis, float amp, float tremHz, Random rng)
    {
        float dt = 1f / rate, per = amp / (float)Math.Sqrt(Math.Max(1, midis.Length));
        foreach (int midi in midis)
        {
            float f = Freq(midi);
            float tau = 2.4f * (1f - 0.35f * Math.Max(0f, Math.Min(1f, (midi - 52) / 24f))); // the top fades faster
            int start = at + (int)(rng.NextDouble() * 0.012 * rate);
            int n = Math.Min(len + (int)(0.08f * rate), (int)(tau * 4f * rate));
            double ph = 0;
            float vel = per * (0.85f + 0.3f * (float)rng.NextDouble());
            uint nz = Seed(midi * 7 + start);
            for (int i = 0; i < n; i++)
            {
                int j = start + i; if (j >= buf.Length) break;
                float t = i * dt;
                ph += f * dt;
                float env = Math.Min(1f, i / (0.003f * rate)) * (float)Math.Exp(-t / tau) * (i < len ? 1f : Math.Max(0f, 1f - (i - len) / (0.08f * rate)));
                float v = Osc(ph) + 0.22f * Osc(ph * 2.0) * (float)Math.Exp(-t * 1.6) + 0.07f * Osc(ph * 3.0) * (float)Math.Exp(-t * 2.0)
                        + 0.05f * Osc(ph * 3.52) * (float)Math.Exp(-t * 6.0) + 0.02f * Osc(ph * 5.4) * (float)Math.Exp(-t * 9.0);
                if (i < 0.004f * rate) v += Noise(ref nz) * (1f - i / (0.004f * rate)) * 0.12f; // the hammer
                float trem = 1f - 0.3f * 0.5f * (1f - Osc(tremHz * t + 0.75));
                if (j >= 0) buf[j] += v * env * trem * vel;
            }
        }
    }

    /// Strings: `len` samples of sustain (a 1.2 s attack inside it), a 0.9 s release after; the level and the cutoff ramp across
    public static void Strings(float[] buf, int rate, int at, int len, int[] midis, float a0, float a1, float cut0, float cut1)
    {
        if (len <= 0) return;
        float dt = 1f / rate, per = 1f / (float)Math.Sqrt(Math.Max(1, midis.Length)) * 0.33f;
        int rel = (int)(0.9f * rate), n = len + rel, att = (int)(1.2f * rate);
        foreach (int midi in midis)
        {
            float f0 = Freq(midi);
            var table = MusicJazz.SawTable(Math.Max(4, Math.Min(40, (int)(6000f / f0))));
            double pa = 0.1, pb = 0.45, pc = 0.8;
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                int j = at + i; if (j >= buf.Length) break;
                float k = Math.Min(1f, i / (float)len);
                pa += f0 * 0.995 * dt; pb += f0 * dt; pc += f0 * 1.005 * dt;
                float x = MusicJazz.Wave(table, pa) + MusicJazz.Wave(table, pb) + MusicJazz.Wave(table, pc);
                float cut = cut0 * (float)Math.Pow(cut1 / cut0, k);
                lp += (x - lp) * (1f - (float)Math.Exp(-2.0 * Math.PI * cut / rate));
                float env = i < att ? 0.5f - 0.5f * (float)Math.Cos(Math.PI * i / att) : i < len ? 1f : Math.Max(0f, 1f - (i - len) / (float)rel);
                if (j >= 0) buf[j] += lp * env * (a0 + (a1 - a0) * k) * per;
            }
        }
    }

    /// The muted trumpet: a band-limited pulse through a resonant low-pass that opens over the note (the wah), vibrato
    /// arriving late, a scoop up into the note, a bend up into it (semitones), a fall at its end (semitones)
    public static void Horn(float[] buf, int rate, int at, int len, int midi, float amp, bool scoop, float bendSemis, float fallSemis)
    {
        float f0 = Freq(midi);
        var table = MusicJazz.PulseTable(Math.Max(3, Math.Min(40, (int)(6000f / f0))), 0.35f);
        float dur = len / (float)rate, dt = 1f / rate;
        int n = len + (int)(0.09f * rate);
        double ph = 0;
        float low = 0f, band = 0f, q = 0.75f;
        float openTime = Math.Max(0.12f, dur * 0.55f);
        for (int i = 0; i < n; i++)
        {
            int j = at + i; if (j >= buf.Length) break;
            float t = i * dt;
            float semis = 0f;
            if (bendSemis != 0f) semis -= bendSemis * (float)Math.Exp(-t / 0.09f);           // from below, settling
            if (scoop) semis -= 0.6f * (float)Math.Exp(-t / 0.035f);
            if (fallSemis != 0f && t > dur - 0.22f) semis -= fallSemis * Math.Min(1f, (t - (dur - 0.22f)) / 0.22f); // the fall
            float vib = 1f + 0.0045f * Osc(4.8 * t) * Math.Min(1f, Math.Max(0f, (t - 0.25f) * 3f));
            ph += f0 * (float)Math.Pow(2.0, semis / 12.0) * vib * dt;
            float raw = MusicJazz.Wave(table, ph);
            float open = Math.Min(1f, t / openTime);
            float cut = 550f + 1500f * open * open + 200f * Osc(4.8 * t) * open;
            if (t > dur - 0.15f) cut *= Math.Max(0.5f, 1f - (t - (dur - 0.15f)) / 0.3f); // the mute closing
            float fc = 2f * (float)Math.Sin(Math.PI * Math.Min(cut, 4000f) / rate);
            low += fc * band;
            float high = raw - low - q * band;
            band += fc * high;
            float swell = 0.82f + 0.18f * (float)Math.Sin(Math.PI * Math.Min(1f, t / Math.Max(0.2f, dur)));
            float env = Math.Min(1f, i / (0.03f * rate)) * (i < len ? 1f : Math.Max(0f, 1f - (i - len) / (0.09f * rate)));
            if (j >= 0) buf[j] += (float)Math.Tanh(low * 1.8) * 0.5f * env * swell * amp;
        }
    }

    /// The tape and the record: wobble, warmth, a little saturation, a vinyl bed. In place
    public static void Tape(float[] buf, int rate, float wobbleCents, float lpHz, float vinyl, int seed)
    {
        int n = buf.Length;
        var src = (float[])buf.Clone();
        double D = 0.012 * rate, A1 = wobbleCents / 6f * 0.0014 * rate, A2 = 0.005 * rate; // ±6 cents at 0.4 Hz, ±5 cents at 0.09 Hz
        for (int i = 0; i < n; i++)
        {
            double t = i / (double)rate;
            double pos = i - D + A1 * Math.Sin(2.0 * Math.PI * 0.4 * t + 1.1) + A2 * Math.Sin(2.0 * Math.PI * 0.09 * t);
            int p0 = (int)Math.Floor(pos); float fr = (float)(pos - p0);
            float a = p0 >= 0 && p0 < n ? src[p0] : 0f, b = p0 + 1 >= 0 && p0 + 1 < n ? src[p0 + 1] : 0f;
            buf[i] = a + (b - a) * fr;
        }
        float k = 1f - (float)Math.Exp(-2.0 * Math.PI * lpHz / rate), l1 = 0f, l2 = 0f;
        float norm = 1f / (float)Math.Tanh(1.25);
        for (int i = 0; i < n; i++)
        {
            l1 += (buf[i] - l1) * k; l2 += (l1 - l2) * k;
            buf[i] = (float)Math.Tanh(l2 * 1.25) * norm * 0.9f;
        }
        if (vinyl > 0f)
        {
            uint nz = Seed(seed + 99);
            var rng = new Random(seed + 7);
            float hiss = 0f;
            for (int i = 0; i < n; i++) { hiss += (Noise(ref nz) - hiss) * 0.3f; buf[i] += hiss * vinyl; }
            int clicks = (int)(n / (double)rate * 2.5); // two or three a second
            for (int c = 0; c < clicks; c++)
            {
                int at = rng.Next(n), w = 1 + rng.Next(3);
                float a = (float)Math.Pow(rng.NextDouble(), 2.5) * vinyl * 18f + vinyl * 2f;
                for (int i = 0; i < w * 3 && at + i < n; i++) buf[at + i] += a * (float)Math.Exp(-i / (float)w) * (i % 2 == 0 ? 1f : -0.6f);
            }
        }
    }
}
