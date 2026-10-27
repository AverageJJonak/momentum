using System;

/// <summary>
/// The BEDS' drum machine and synths (MusicBeds), everything at MusicRemix.Rate, mono, bounds-checked:
/// a modern kick (a sine dropping 160→48 Hz into a soft clip, a click), a clap (four bursts and a tail
/// through a band-pass), a snare (noise over a body), a rim, hats (square partials and noise through a
/// high-pass), an 808 sub (a sine that glides from the last note, soft-clipped, long), brass stabs (two
/// detuned band-limited saws a note through a closing resonant low-pass), a riser, a crash with a boom,
/// and the stutter edit (a slice of the lead repeated on a grid).
/// </summary>
public static class MusicBedVoices
{
    public class Gen
    {
        uint noise;
        public Gen(int seed) { noise = ((uint)seed * 2654435761u) | 1u; }
        public float Noise() { noise ^= noise << 13; noise ^= noise >> 17; noise ^= noise << 5; return (noise >> 8) * (1f / 8388608f) - 1f; }
    }

    const int Rate = MusicRemix.Rate;
    public static float Freq(int midi, float cents) => 440f * (float)Math.Pow(2.0, (midi - 69) / 12.0 + cents / 1200.0);

    /// A modern kick: a sine dropping from 160 to 48 Hz through a soft clip, a click on the front
    public static void Kick(Gen g, float[] buf, int at, float amp)
    {
        int n = (int)(0.3f * Rate);
        float dt = 1f / Rate, last = 0f;
        double ph = 0;
        for (int i = 0; i < n; i++)
        {
            int j = at + i; if (j >= buf.Length) break;
            float t = i * dt;
            ph += (48f + 112f * (float)Math.Exp(-t / 0.028f)) * dt;
            float v = (float)Math.Tanh(Math.Sin(2.0 * Math.PI * ph) * 1.7) * (float)Math.Exp(-t / 0.2f);
            if (i < 110) { float nz = g.Noise(); v += (nz - last) * (1f - i / 110f) * 0.4f; last = nz; }
            if (j >= 0) buf[j] += v * amp;
        }
    }

    /// Four noise bursts a few ms apart and a tail, through a band-pass around 1.4 kHz
    public static void Clap(Gen g, float[] buf, int at, float amp)
    {
        int n = (int)(0.17f * Rate);
        float low = 0f, band = 0f, f = 2f * (float)Math.Sin(Math.PI * 1400.0 / Rate), q = 0.6f;
        int[] starts = { 0, (int)(0.009f * Rate), (int)(0.018f * Rate), (int)(0.027f * Rate) };
        for (int i = 0; i < n; i++)
        {
            float env = 0f;
            for (int b = 0; b < 4; b++) { int d = i - starts[b]; if (d >= 0 && d < (int)(0.008f * Rate)) env = Math.Max(env, (float)Math.Exp(-d / (0.0025f * Rate))); }
            int tail = i - starts[3];
            if (tail >= 0) env = Math.Max(env, 0.8f * (float)Math.Exp(-tail / (0.045f * Rate)));
            float x = g.Noise() * env;
            low += f * band;
            float high = x - low - q * band;
            band += f * high;
            int j = at + i; if (j >= buf.Length) break; if (j < 0) continue;
            buf[j] += band * amp * 1.6f;
        }
    }

    /// Noise through a high-pass over a 190 Hz body that drops in
    public static void Snare(Gen g, float[] buf, int at, float amp)
    {
        int n = (int)(0.2f * Rate);
        float dt = 1f / Rate, lp = 0f;
        double ph = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i * dt;
            float nz = g.Noise();
            lp += (nz - lp) * 0.3f;
            float snap = (nz - lp) * (float)Math.Exp(-t / 0.08f);
            ph += (180.0 + 60.0 * Math.Exp(-t / 0.02)) * dt;
            float body = (float)Math.Sin(2.0 * Math.PI * ph) * (float)Math.Exp(-t / 0.045f) * 0.7f;
            int j = at + i; if (j >= buf.Length) break; if (j < 0) continue;
            buf[j] += (snap + body) * amp;
        }
    }

    /// A rimshot: a 2 kHz knock, 25 ms
    public static void Rim(Gen g, float[] buf, int at, float amp)
    {
        int n = (int)(0.025f * Rate);
        float dt = 1f / Rate;
        double ph = 0;
        for (int i = 0; i < n; i++)
        {
            int j = at + i; if (j >= buf.Length) break;
            float t = i * dt;
            ph += 2000.0 * dt;
            float v = ((float)Math.Sin(2.0 * Math.PI * ph) * 0.6f + g.Noise() * 0.4f) * (float)Math.Exp(-t / 0.006f);
            if (j >= 0) buf[j] += v * amp;
        }
    }

    /// Six square partials and noise through a high-pass: closed at 9–12 ms, open at 140–160
    public static void Hat(Gen g, float[] buf, int at, float amp, float decay)
    {
        int n = (int)(decay * 5f * Rate) + 8;
        float dt = 1f / Rate, lp = 0f, k = 1f - (float)Math.Exp(-2.0 * Math.PI * 6500.0 / Rate);
        double[] ph = new double[6];
        float[] fr = { 3010f, 4110f, 5450f, 6870f, 8300f, 9900f };
        for (int i = 0; i < n; i++)
        {
            float t = i * dt, v = 0f;
            for (int h = 0; h < 6; h++) { ph[h] += fr[h] * dt; v += (ph[h] - Math.Floor(ph[h])) < 0.5 ? 0.5f : -0.5f; }
            v = v * 0.25f + g.Noise() * 0.6f;
            lp += (v - lp) * k;
            float hp = v - lp;
            int j = at + i; if (j >= buf.Length) break; if (j < 0) continue;
            buf[j] += hp * (float)Math.Exp(-t / decay) * amp;
        }
    }

    /// The 808: a sine gliding in from the last note over 45 ms, a touch of second harmonic, soft-clipped, a long decay
    public static void Sub808(float[] buf, int at, int len, int midi, int fromMidi, float amp, float tuning)
    {
        if (len <= 0) return;
        float f0 = Freq(midi, tuning), f1 = fromMidi > 0 ? Freq(fromMidi, tuning) : f0, dt = 1f / Rate;
        int n = len + (int)(0.03f * Rate), attack = (int)(0.004f * Rate), release = (int)(0.03f * Rate);
        double ph = 0;
        for (int i = 0; i < n; i++)
        {
            int j = at + i; if (j >= buf.Length) break;
            float t = i * dt;
            float f = f0 + (f1 - f0) * (float)Math.Exp(-t / 0.018f);
            ph += f * dt;
            float env = (i < attack ? i / (float)attack : 1f) * (float)Math.Exp(-t / 0.9f) * (i > len ? Math.Max(0f, 1f - (i - len) / (float)release) : 1f);
            float v = (float)Math.Sin(2.0 * Math.PI * ph) + 0.1f * (float)Math.Sin(4.0 * Math.PI * ph);
            if (j >= 0) buf[j] += (float)Math.Tanh(v * 1.3) * env * amp;
        }
    }

    /// A brass stab: two detuned band-limited saws a note through a resonant low-pass that closes over the hit
    public static void Stab(float[] buf, int at, int len, int[] midi, float amp, float tuning, float cut0, float cut1, float decay)
    {
        if (len <= 0) return;
        float dt = 1f / Rate, per = amp / (float)Math.Sqrt(midi.Length);
        int release = (int)(0.01f * Rate);
        foreach (int m in midi)
        {
            float f0 = Freq(m, tuning);
            var table = MusicJazz.SawTable(Math.Max(4, Math.Min(48, (int)(7000f / f0))));
            double pa = 0, pb = 0.4;
            float low = 0f, band = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = i * dt;
                pa += f0 * 1.004 * dt; pb += f0 * 0.996 * dt;
                float x = MusicJazz.Wave(table, pa) + MusicJazz.Wave(table, pb);
                float cut = cut1 + (cut0 - cut1) * (float)Math.Exp(-t / 0.09f);
                float fc = 2f * (float)Math.Sin(Math.PI * Math.Min(cut, 9000f) / Rate);
                low += fc * band;
                float high = x - low - 0.5f * band;
                band += fc * high;
                float env = Math.Min(1f, i / (0.003f * Rate)) * (float)Math.Exp(-t / decay) * (i > len - release ? (len - i) / (float)release : 1f);
                int j = at + i; if (j >= buf.Length) break; if (j < 0) continue;
                buf[j] += (float)Math.Tanh(low * 1.4) * env * per * 0.5f;
            }
        }
    }

    /// White noise through a band-pass sweeping 300 Hz to 6 kHz, rising to the last sample, with a sine sweep inside it
    public static void Riser(Gen g, float[] buf, int from, int to, float amp)
    {
        int n = to - from;
        if (n <= 0) return;
        float low = 0f, band = 0f, dt = 1f / Rate;
        double ph = 0;
        for (int i = 0; i < n; i++)
        {
            float k = i / (float)n;
            float cut = 300f * (float)Math.Pow(20.0, k);
            float fc = 2f * (float)Math.Sin(Math.PI * cut / Rate);
            low += fc * band;
            float high = g.Noise() - low - 0.5f * band;
            band += fc * high;
            ph += (180.0 + 1300.0 * k * k) * dt;
            float v = band * 1.2f + (float)Math.Sin(2.0 * Math.PI * ph) * 0.1f * k;
            int j = from + i; if (j >= buf.Length) break; if (j < 0) continue;
            buf[j] += v * (float)Math.Pow(k, 1.6) * amp;
        }
    }

    /// The impact: a crash (noise high-passed twice, a long tail) over a boom; boomOnly = the timpani
    public static void Crash(Gen g, float[] buf, int at, float amp, bool boomOnly = false)
    {
        int n = (int)(1.6f * Rate);
        float dt = 1f / Rate, lp1 = 0f, lp2 = 0f, k = 1f - (float)Math.Exp(-2.0 * Math.PI * 3000.0 / Rate);
        double ph = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i * dt;
            float nz = g.Noise();
            lp1 += (nz - lp1) * k; float h1 = nz - lp1;
            lp2 += (h1 - lp2) * k; float h2 = h1 - lp2;
            ph += (38.0 + 30.0 * Math.Exp(-t / 0.08)) * dt;
            float boom = (float)Math.Sin(2.0 * Math.PI * ph) * (float)Math.Exp(-t / 0.25f) * 0.9f;
            int j = at + i; if (j >= buf.Length) break; if (j < 0) continue;
            buf[j] += ((boomOnly ? 0f : h2 * (float)Math.Exp(-t / 0.45f) * 0.55f) + boom) * amp;
        }
    }

    /// A slice of the lead repeated on a grid, resampled to the bed's rate, 2 ms fades, closing = each repeat quieter
    public static void Stutter(float[] buf, int at, float[] slice, int sliceRate, int repeats, int spacing, float amp, bool closing)
    {
        if (slice == null || slice.Length < 8 || spacing <= 0) return;
        double ratio = sliceRate / (double)Rate;
        int n = Math.Min(spacing, (int)(slice.Length / ratio));
        int fade = Math.Max(1, (int)(0.002f * Rate));
        for (int r = 0; r < repeats; r++)
        {
            float a = amp * (closing ? 1f - 0.18f * r : 1f);
            for (int i = 0; i < n; i++)
            {
                double pos = i * ratio; int p0 = (int)pos; float fr = (float)(pos - p0);
                if (p0 + 1 >= slice.Length) break;
                float v = slice[p0] + (slice[p0 + 1] - slice[p0]) * fr;
                float env = i < fade ? i / (float)fade : i > n - fade ? (n - i) / (float)fade : 1f;
                int j = at + r * spacing + i; if (j >= buf.Length) break; if (j < 0) continue;
                buf[j] += v * env * a;
            }
        }
    }
}
