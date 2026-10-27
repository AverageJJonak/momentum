using System;
using UnityEngine;

/// <summary>
/// The LEAD's audio-thread treatment, one per lead source (MusicDirector): the
/// side-chain PUMP under every kick of the bed (a 70 ms duck, 3 ms in, linear out),
/// gain WINDOWS (the silent beat before a drop, the lead pulled down under a stutter
/// edit), and a micro PITCH SHIFT of a few cents for FLOW — a two-tap modulated delay
/// line (25 ms window, raised-cosine crossfade) so the lead lifts without drifting
/// off the bed's grid the way AudioSource.pitch would. The main thread hands over an
/// immutable Grid (kick times in DSP seconds) and a target shift; everything else
/// happens in OnAudioFilterRead. Sits after the lead's high- and low-pass filters.
/// </summary>
public class MusicPump : MonoBehaviour
{
    public class Grid
    {
        public double[] ducks = new double[0]; // DSP times of the kicks, ascending
        public float depth;                    // 0 = no pump
        public float width = 0.07f;            // seconds to recover
        public double[] winFrom = new double[0], winTo = new double[0];
        public float[] winGain = new float[0];
    }

    static readonly Grid Empty = new Grid();
    volatile Grid grid = Empty;
    volatile float cents;   // target shift
    volatile float boost = 1f; // the raw path: an unmastered 78 brought up through a soft clip (1 = off)
    volatile int outRate = 48000;

    // audio-thread state
    Grid last;
    int cursor;
    float mix;              // 0 dry … 1 shifted
    float[][] ring;
    int ringLen, write;
    double phase;           // tap A's position in its sawtooth, 0..1 (tap B is half a cycle on)

    public Grid Current => grid;

    /// Main thread: the kick grid (and windows) this lead is played against
    public void SetGrid(Grid g) => grid = g ?? Empty;
    /// Main thread: the pitch lift in cents (0 = off)
    public void SetCents(float c) => cents = c;
    /// Main thread: a gain above 1 for a lead that could not be mastered, soft-clipped (tanh) so it never cracks
    public void SetBoost(float b) => boost = b < 1f ? 1f : b > 8f ? 8f : b;

    void Awake() { outRate = AudioSettings.outputSampleRate; if (outRate <= 0) outRate = 48000; }

    void OnAudioFilterRead(float[] data, int channels)
    {
        var g = grid;
        if (!ReferenceEquals(g, last)) { last = g; cursor = 0; }
        int frames = data.Length / Math.Max(1, channels);
        double t0 = AudioSettings.dspTime;
        double dt = 1.0 / outRate;
        float target = cents;
        bool shifting = target != 0f || mix > 0.0005f;
        if (shifting && (ring == null || ring.Length != channels))
        {
            ringLen = Math.Max(256, outRate / 32); // ~31 ms
            ring = new float[channels][];
            for (int c = 0; c < channels; c++) ring[c] = new float[ringLen];
            write = 0; phase = 0; mix = 0f;
        }
        float ratio = target != 0f ? (float)Math.Pow(2.0, target / 1200.0) : 1f;
        float window = ringLen > 0 ? ringLen * 0.8f : 1f;      // the sawtooth's span in samples
        float phaseStep = (ratio - 1f) / window;               // per sample
        float mixTarget = target != 0f ? 1f : 0f;

        var ducks = g.ducks;
        float lift = boost;
        if (lift > 1f) for (int i = 0; i < data.Length; i++) data[i] = (float)Math.Tanh(data[i] * lift);
        for (int i = 0; i < frames; i++)
        {
            double t = t0 + i * dt;
            // the pump: the latest kick at or before t
            float gain = 1f;
            if (g.depth > 0f && ducks.Length > 0)
            {
                while (cursor + 1 < ducks.Length && ducks[cursor + 1] <= t) cursor++;
                while (cursor > 0 && ducks[cursor] > t) cursor--;
                double since = t - ducks[cursor];
                if (since >= 0 && since < g.width)
                {
                    float env = since < 0.003 ? (float)(since / 0.003) : 1f - (float)((since - 0.003) / (g.width - 0.003));
                    gain = 1f - g.depth * Math.Max(0f, Math.Min(1f, env));
                }
            }
            for (int w = 0; w < g.winFrom.Length; w++)
            {
                double from = g.winFrom[w], to = g.winTo[w];
                if (t < from || t >= to) continue;
                double edge = Math.Min(t - from, to - t);
                float k = edge < 0.004 ? (float)(edge / 0.004) : 1f;
                gain *= 1f - (1f - g.winGain[w]) * k;
            }
            if (shifting)
            {
                mix += (mixTarget - mix) * 0.0004f;
                // tap A at phase, tap B half a cycle on; delays shrink (pitch up) or grow (pitch down) along the sawtooth
                float pA = (float)phase, pB = pA + 0.5f; if (pB >= 1f) pB -= 1f;
                float dA = window * (1f - pA), dB = window * (1f - pB);
                float gA = 0.5f - 0.5f * (float)Math.Cos(2.0 * Math.PI * pA), gB = 1f - gA;
                for (int c = 0; c < channels; c++)
                {
                    var r = ring[c];
                    float x = data[i * channels + c];
                    r[write] = x;
                    float y = Read(r, write, dA) * gA + Read(r, write, dB) * gB;
                    data[i * channels + c] = (x * (1f - mix) + y * mix) * gain;
                }
                write++; if (write == ringLen) write = 0;
                phase += phaseStep; if (phase >= 1.0) phase -= 1.0; else if (phase < 0.0) phase += 1.0;
            }
            else if (gain != 1f) for (int c = 0; c < channels; c++) data[i * channels + c] *= gain;
        }
    }

    /// A linearly interpolated read `delay` samples behind the write head
    float Read(float[] r, int w, float delay)
    {
        float pos = w - delay;
        while (pos < 0f) pos += ringLen;
        int i0 = (int)pos; float frac = pos - i0;
        int i1 = i0 + 1; if (i1 >= ringLen) i1 -= ringLen;
        if (i0 >= ringLen) i0 -= ringLen;
        return r[i0] + (r[i1] - r[i0]) * frac;
    }
}
