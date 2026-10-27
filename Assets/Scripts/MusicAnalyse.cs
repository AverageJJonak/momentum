using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// THE EAR of the remix score: measures a recording so a bed can be synthesised on its
/// grid. Pure C# (no Unity types) so the same code runs on MusicRemix's worker thread
/// and in the offline chart baker.
///   tempo     spectral-flux onset envelope (11 kHz, 512-point frames, 128 hop) and its
///             autocorrelation in the 80–220 bpm band with a comb over the harmonics and
///             a prior around house tempo (112 bpm, half an octave wide): a 2/2 hot-jazz
///             side lands on its half-note pulse, a medium swing on its quarters
///   beats     Ellis's dynamic-programming beat tracker on that envelope, so a band that
///             rushes stays on the grid; every bed is placed on these beats, not a period
///   downbeat  the onset strength, the bass-band onsets and the chroma change folded over
///             the four beat phases
///   key       a 12-bin chroma from 4096-point frames against the Krumhansl profiles
///   roots     one chord root (and quality) per bar from that bar's chroma, sticky, with
///             the key's scale degrees favoured: the sub bass and the stabs follow them
///   swing     the off-beat onset peak between beats (0.5 straight … 0.67 triplet)
/// </summary>
public static class MusicAnalyse
{
    public class Result
    {
        public float bpm;              // mean tempo of the tracked beats
        public double[] beats;         // beat times in seconds from sample 0 (ascending)
        public int downbeat;           // index into beats of the first downbeat
        public int tonic; public bool minor;
        public float keyConfidence;    // the margin between the best and the second key (0..1-ish)
        public float tempoConfidence;  // the chosen lag's score over the mean score
        public float jitter;           // std/mean of the beat intervals (0 = metronomic)
        public int[] roots, kinds;     // per bar from the downbeat; kind 0 maj7 1 min7 2 dom7 3 m7b5
        public float swing;
        public float rms;
        public float tuning;           // cents the recording sits above A440 (−50..50): the sub and the stabs follow it
        public float[] phaseScore = new float[4]; // the downbeat fold, for the log
        public string log = "";
    }

    /// Overrides from a "<name>.chart.txt": bpm > 0 is the tracker's period (the beats still follow the band unless
    /// fixedGrid), offset >= 0 the first downbeat, tonic >= 0 the key, swing > 0 the swing
    public struct Hints { public float bpm, offset, swing; public int tonic; public bool minor, fixedGrid; public static Hints None => new Hints { offset = -1f, tonic = -1 }; }

    const int OnsetN = 512, OnsetHop = 128, ChromaN = 4096, ChromaHop = 1024;
    const float MinBpm = 80f, MaxBpm = 220f, PriorBpm = 112f, PriorOctaves = 0.55f;
    const float Alpha = 100f; // the tracker's stiffness: how dearly a beat that is not one period on costs

    static readonly float[] MajorProfile = { 6.35f, 2.23f, 3.48f, 2.33f, 4.38f, 4.09f, 2.52f, 5.19f, 2.39f, 3.66f, 2.29f, 2.88f };
    static readonly float[] MinorProfile = { 6.33f, 2.68f, 3.52f, 5.38f, 2.60f, 3.53f, 2.54f, 4.75f, 3.98f, 2.69f, 3.34f, 3.17f };
    static readonly int[] MajorScale = { 0, 2, 4, 5, 7, 9, 11 }, MinorScale = { 0, 2, 3, 5, 7, 8, 10 };

    public static Result Run(float[] pcm, int channels, int rate, Hints h)
    {
        var r = new Result();
        var sb = new StringBuilder();
        int frames = pcm.Length / Math.Max(1, channels);
        if (frames < rate) throw new ArgumentException("too short");

        // ---- mono, rms, decimation to ~11 kHz
        int d = Math.Max(1, (int)Math.Round(rate / 11025.0));
        int ar = rate / d;
        int n = frames / d;
        var a = new float[n];
        double sq = 0;
        for (int i = 0; i < n; i++)
        {
            float s = 0f;
            for (int k = 0; k < d; k++)
            {
                int f = (i * d + k) * channels;
                float m = 0f;
                for (int c = 0; c < channels; c++) m += pcm[f + c];
                m /= channels;
                s += m;
                sq += m * (double)m;
            }
            a[i] = s / d;
        }
        r.rms = (float)Math.Sqrt(sq / Math.Max(1, (long)n * d));

        // ---- onset envelope
        int nf = Math.Max(2, (n - OnsetN) / OnsetHop + 1);
        var flux = new float[nf];
        var low = new float[nf];
        Stft(a, OnsetN, OnsetHop, nf, 0, (f, mag) =>
        {
            // mag: magnitudes for bins 0..N/2; log-compressed flux against the previous frame
            float fl = 0f, lo = 0f;
            int loBins = Math.Max(2, (int)(220.0 * OnsetN / ar));
            for (int k = 1; k <= OnsetN / 2; k++)
            {
                float v = (float)Math.Log(1.0 + 20.0 * mag[k]);
                float dv = v - prevLog[k];
                prevLog[k] = v;
                if (dv > 0f) { fl += dv; if (k < loBins) lo += dv; }
            }
            flux[f] = fl; low[f] = lo;
        });
        float fps = ar / (float)OnsetHop;
        Normalise(flux); Normalise(low);
        var ls = Smooth(flux, 1.2f);

        // ---- tempo
        double period; // frames per beat
        if (h.bpm > 0f) { period = fps * 60.0 / h.bpm; r.tempoConfidence = 1f; sb.Append("tempo pinned " + h.bpm.ToString("0.00") + "; "); }
        else
        {
            int minLag = (int)Math.Floor(fps * 60.0 / MaxBpm), maxLag = (int)Math.Ceiling(fps * 60.0 / MinBpm);
            int acfLen = maxLag * 3 + 2;
            var acf = Acf(ls, acfLen);
            double bestScore = -1, bestLag = minLag, mean = 0; int count = 0;
            var score = new double[maxLag + 1];
            for (int L = minLag; L <= maxLag; L++)
            {
                double bpm = fps * 60.0 / L;
                double oct = Math.Log(bpm / PriorBpm, 2.0) / PriorOctaves;
                double w = Math.Exp(-0.5 * oct * oct);
                double s = acf[L];
                if (2 * L < acfLen) s += 0.5 * acf[2 * L];
                if (3 * L < acfLen) s += 0.25 * acf[3 * L];
                score[L] = s * w;
                mean += score[L]; count++;
                if (score[L] > bestScore) { bestScore = score[L]; bestLag = L; }
            }
            int bl = (int)bestLag;
            if (bl > minLag && bl < maxLag)
            {   // parabolic refinement of the peak
                double y0 = score[bl - 1], y1 = score[bl], y2 = score[bl + 1], den = y0 - 2 * y1 + y2;
                if (Math.Abs(den) > 1e-9) bestLag = bl + 0.5 * (y0 - y2) / den;
            }
            period = bestLag;
            r.tempoConfidence = (float)(mean > 0 ? bestScore / (mean / count) : 0);
            sb.Append("acf " + (fps * 60.0 / period).ToString("0.0") + " bpm x" + r.tempoConfidence.ToString("0.0") + "; ");
        }

        // ---- beats
        double[] beats;
        if (h.bpm > 0f && h.fixedGrid)
        {
            double beat = 60.0 / h.bpm;
            double first = h.offset >= 0f ? h.offset : 0.0; while (first - beat >= 0.0) first -= beat;
            var list = new List<double>();
            for (double t = first; t < frames / (double)rate; t += beat) list.Add(t);
            beats = list.ToArray();
        }
        else
        {
            var beatFrames = Track(ls, period, Alpha);
            beats = new double[beatFrames.Length];
            for (int i = 0; i < beats.Length; i++)
            {   // sub-frame: the onset peak's parabola around the tracked frame
                int f = beatFrames[i];
                double frac = 0;
                if (f > 0 && f + 1 < ls.Length)
                {
                    double y0 = ls[f - 1], y1 = ls[f], y2 = ls[f + 1], den = y0 - 2 * y1 + y2;
                    if (y1 >= y0 && y1 >= y2 && Math.Abs(den) > 1e-9) frac = Math.Max(-0.5, Math.Min(0.5, 0.5 * (y0 - y2) / den));
                }
                beats[i] = ((f + frac) * OnsetHop + 0.75 * OnsetN) / ar;
            }
            // a light smoothing against frame noise: each beat moves halfway to the line through its neighbours
            var sm = (double[])beats.Clone();
            for (int i = 0; i < beats.Length; i++)
            {
                int lo = Math.Max(0, i - 3), hi = Math.Min(beats.Length - 1, i + 3);
                double sx = 0, sy = 0, sxx = 0, sxy = 0; int c = 0;
                for (int j = lo; j <= hi; j++) { sx += j; sy += beats[j]; sxx += j * (double)j; sxy += j * beats[j]; c++; }
                double den = c * sxx - sx * sx;
                if (c >= 3 && Math.Abs(den) > 1e-9)
                {
                    double slope = (c * sxy - sx * sy) / den, icpt = (sy - slope * sx) / c;
                    sm[i] = 0.5 * beats[i] + 0.5 * (slope * i + icpt);
                }
            }
            beats = sm;
        }
        if (beats.Length < 8) throw new Exception("no beats");
        {   // strictly ascending, never closer than a tenth of a beat: the grid must always move forward
            double minIv = 0.1 * 60.0 / Math.Max(30.0, fps * 60.0 / Math.Max(1.0, period));
            for (int i = 1; i < beats.Length; i++) if (beats[i] < beats[i - 1] + minIv) beats[i] = beats[i - 1] + minIv;
        }
        double sum = 0, sumSq = 0;
        for (int i = 1; i < beats.Length; i++) { double iv = beats[i] - beats[i - 1]; sum += iv; sumSq += iv * iv; }
        double meanIv = sum / (beats.Length - 1);
        r.jitter = (float)(Math.Sqrt(Math.Max(0, sumSq / (beats.Length - 1) - meanIv * meanIv)) / meanIv);
        r.bpm = (float)(60.0 / meanIv);
        r.beats = beats;

        // ---- chroma per frame
        int cf = Math.Max(1, (n - ChromaN) / ChromaHop + 1);
        var chroma = new float[cf][];
        var global = new float[12];
        var binPc = new int[ChromaN / 2 + 1];
        var binCent = new int[ChromaN / 2 + 1]; // the bin's offset from its nearest semitone, in 5-cent steps (-10..9)
        for (int k = 0; k <= ChromaN / 2; k++)
        {
            double f = k * (double)ar / ChromaN;
            if (f >= 60.0 && f <= 2500.0)
            {
                double midi = 12.0 * Math.Log(f / 440.0, 2.0) + 69.0;
                int nearest = (int)Math.Round(midi);
                binPc[k] = (nearest % 12 + 12) % 12;
                binCent[k] = Math.Max(-10, Math.Min(9, (int)Math.Floor((midi - nearest) * 20.0)));
            }
            else binPc[k] = -1;
        }
        var tune = new double[20];
        Stft(a, ChromaN, ChromaHop, cf, 1, (f, mag) =>
        {
            var c = new float[12];
            for (int k = 1; k <= ChromaN / 2; k++)
                if (binPc[k] >= 0)
                {
                    c[binPc[k]] += mag[k];
                    if (k > 1 && k + 1 <= ChromaN / 2 && mag[k] > mag[k - 1] && mag[k] >= mag[k + 1]) tune[binCent[k] + 10] += mag[k]; // spectral peaks only
                }
            chroma[f] = c;
            for (int p = 0; p < 12; p++) global[p] += c[p];
        });
        {   // the tuning: the circular mean of where the peaks sit between semitones
            double sx = 0, sy = 0;
            for (int i = 0; i < 20; i++) { double ang = 2.0 * Math.PI * (i - 10 + 0.5) / 20.0; sx += tune[i] * Math.Cos(ang); sy += tune[i] * Math.Sin(ang); }
            r.tuning = sx == 0 && sy == 0 ? 0f : (float)(Math.Atan2(sy, sx) / (2.0 * Math.PI) * 100.0);
        }

        // ---- key
        if (h.tonic >= 0) { r.tonic = h.tonic; r.minor = h.minor; r.keyConfidence = 1f; }
        else
        {
            double best = -2, second = -2; int bt = 0; bool bm = false;
            for (int t = 0; t < 12; t++)
                for (int m = 0; m < 2; m++)
                {
                    double c = Correlate(global, m == 1 ? MinorProfile : MajorProfile, t);
                    if (c > best) { second = best; best = c; bt = t; bm = m == 1; }
                    else if (c > second) second = c;
                }
            r.tonic = bt; r.minor = bm; r.keyConfidence = (float)(best - second);
        }

        // ---- downbeat: fold onset strength, bass onsets and chroma change over the four phases
        double chromaFps = ar / (double)ChromaHop;
        var beatChroma = new float[beats.Length][];
        for (int i = 0; i < beats.Length; i++)
        {
            double t0 = beats[i], t1 = i + 1 < beats.Length ? beats[i + 1] : t0 + meanIv;
            beatChroma[i] = ChromaBetween(chroma, chromaFps, t0, t1);
        }
        var phase = new double[4];
        for (int i = 1; i < beats.Length; i++)
        {
            int f = (int)Math.Round((beats[i] * ar - 0.75 * OnsetN) / OnsetHop);
            float on = PeakNear(flux, f, 1), lo = PeakNear(low, f, 1);
            float change = 1f - Cosine(beatChroma[i], beatChroma[i - 1]);
            phase[i & 3] += on + 1.5f * lo + 4f * change;
        }
        // first the strong pair (beats 1 and 3 against 2 and 4), then which of the pair starts the bar
        int db = phase[0] + phase[2] >= phase[1] + phase[3] ? (phase[0] >= phase[2] ? 0 : 2) : (phase[1] >= phase[3] ? 1 : 3);
        for (int p = 0; p < 4; p++) r.phaseScore[p] = (float)phase[p];
        if (h.offset >= 0f)
        {   // the chart's downbeat: the beat nearest the given time
            int nearest = 0; double bestD = double.MaxValue;
            for (int i = 0; i < beats.Length; i++) { double dd = Math.Abs(beats[i] - h.offset); if (dd < bestD) { bestD = dd; nearest = i; } }
            db = nearest;
        }
        r.downbeat = db;

        // ---- roots per bar
        int bars = Math.Max(1, (beats.Length - db) / 4);
        r.roots = new int[bars]; r.kinds = new int[bars];
        int[] scale = r.minor ? MinorScale : MajorScale;
        int prevRoot = r.tonic;
        for (int k = 0; k < bars; k++)
        {
            int b0 = db + 4 * k;
            double t0 = beats[b0], t1 = b0 + 4 < beats.Length ? beats[b0 + 4] : t0 + 4 * meanIv;
            var c = ChromaBetween(chroma, chromaFps, t0, t1);
            double bestS = -1; int bestR = prevRoot; double prevS = 0;
            for (int root = 0; root < 12; root++)
            {
                double s = c[root] + 0.6 * Math.Max(c[(root + 3) % 12], c[(root + 4) % 12]) + 0.5 * c[(root + 7) % 12] + 0.2 * c[(root + 10) % 12];
                int deg = ((root - r.tonic) % 12 + 12) % 12;
                if (Array.IndexOf(scale, deg) >= 0) s *= 1.25;
                if (root == r.tonic) s *= 1.1;
                if (root == prevRoot) prevS = s;
                if (s > bestS) { bestS = s; bestR = root; }
            }
            if (prevS >= 0.92 * bestS) bestR = prevRoot; // sticky: a chord does not change for a whisker
            bool minorThird = c[(bestR + 3) % 12] > c[(bestR + 4) % 12] * 1.08f;
            r.roots[k] = bestR;
            r.kinds[k] = minorThird ? 1 : bestR == r.tonic && !r.minor ? 0 : bestR == r.tonic && r.minor ? 1 : 2;
            prevRoot = bestR;
        }

        // ---- swing: where the off-beat onsets sit between beats
        if (h.swing > 0f) r.swing = h.swing;
        else
        {
            const int steps = 40;
            var fold = new double[steps];
            for (int i = 0; i + 1 < beats.Length; i++)
                for (int s = 0; s < steps; s++)
                {
                    double t = beats[i] + (beats[i + 1] - beats[i]) * (0.3 + 0.5 * s / steps);
                    int f = (int)Math.Round((t * ar - 0.75 * OnsetN) / OnsetHop);
                    if (f >= 0 && f < nf) fold[s] += ls[f];
                }
            int bestI = -1; double bestV = 0, straight = fold[(int)((0.5 - 0.3) / 0.5 * steps)];
            for (int s = 0; s < steps; s++) { double frac = 0.3 + 0.5 * s / steps; if (frac >= 0.5 && frac <= 0.7 && fold[s] > bestV) { bestV = fold[s]; bestI = s; } }
            r.swing = bestI < 0 || bestV < straight * 1.08 ? 0.58f : (float)(0.3 + 0.5 * bestI / steps);
        }

        double phaseAll = phase[0] + phase[1] + phase[2] + phase[3];
        sb.Append("tracked " + r.bpm.ToString("0.00") + " bpm, " + beats.Length + " beats, jitter " + (r.jitter * 100f).ToString("0.0") + "%, downbeat #" + db + " @" + beats[db].ToString("0.00") + "s (phases " + (100 * phase[0] / phaseAll).ToString("0") + "/" + (100 * phase[1] / phaseAll).ToString("0") + "/" + (100 * phase[2] / phaseAll).ToString("0") + "/" + (100 * phase[3] / phaseAll).ToString("0") + "%), key " + KeyName(r.tonic, r.minor) + " +" + r.keyConfidence.ToString("0.00") + ", tuning " + r.tuning.ToString("+0;-0") + "c, swing " + r.swing.ToString("0.00") + ", rms " + (20.0 * Math.Log10(Math.Max(1e-6, r.rms))).ToString("0.0") + " dBFS");
        r.log = sb.ToString();
        return r;
    }

    public static string KeyName(int tonic, bool minor)
    {
        string[] names = { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };
        return names[((tonic % 12) + 12) % 12] + (minor ? "m" : "");
    }

    /// "F", "F#", "Bb", "Fm", "Bbmin", "Eb minor" → tonic and mode; false when it does not parse
    public static bool ParseKey(string s, out int tonic, out bool minor)
    {
        tonic = -1; minor = false;
        if (string.IsNullOrEmpty(s)) return false;
        s = s.Trim();
        int[] letters = { 9, 11, 0, 2, 4, 5, 7 }; // A B C D E F G
        char c = char.ToUpperInvariant(s[0]);
        if (c < 'A' || c > 'G') return false;
        tonic = letters[c - 'A'];
        int i = 1;
        if (i < s.Length && (s[i] == '#' || s[i] == 's')) { tonic = (tonic + 1) % 12; i++; }
        else if (i < s.Length && s[i] == 'b') { tonic = (tonic + 11) % 12; i++; }
        string rest = s.Substring(i).Trim().ToLowerInvariant();
        minor = rest.StartsWith("m") && !rest.StartsWith("maj");
        return true;
    }

    // ------------------------------------------------------------ pieces

    [ThreadStatic] static float[] prevLog;

    /// Hann-windowed STFT; the callback gets the frame index and the magnitudes of bins 0..N/2 (shared buffer)
    static void Stft(float[] x, int N, int hop, int frames, int tag, Action<int, float[]> onFrame)
    {
        prevLog = new float[N / 2 + 1];
        var re = new float[N]; var im = new float[N]; var mag = new float[N / 2 + 1];
        var win = Hann(N);
        var tw = Twiddles(N);
        for (int f = 0; f < frames; f++)
        {
            int s0 = f * hop;
            for (int i = 0; i < N; i++) { int j = s0 + i; re[i] = j < x.Length ? x[j] * win[i] : 0f; im[i] = 0f; }
            Fft(re, im, tw);
            for (int k = 0; k <= N / 2; k++) mag[k] = (float)Math.Sqrt(re[k] * re[k] + im[k] * im[k]);
            onFrame(f, mag);
        }
    }

    static readonly Dictionary<int, float[]> hanns = new Dictionary<int, float[]>();
    static float[] Hann(int N)
    {
        lock (hanns)
        {
            if (hanns.TryGetValue(N, out var w)) return w;
            w = new float[N];
            for (int i = 0; i < N; i++) w[i] = 0.5f - 0.5f * (float)Math.Cos(2.0 * Math.PI * i / N);
            hanns[N] = w;
            return w;
        }
    }

    static readonly Dictionary<int, float[][]> twiddles = new Dictionary<int, float[][]>();
    static float[][] Twiddles(int N)
    {
        lock (twiddles)
        {
            if (twiddles.TryGetValue(N, out var t)) return t;
            var c = new float[N / 2]; var s = new float[N / 2];
            for (int i = 0; i < N / 2; i++) { c[i] = (float)Math.Cos(-2.0 * Math.PI * i / N); s[i] = (float)Math.Sin(-2.0 * Math.PI * i / N); }
            t = new[] { c, s };
            twiddles[N] = t;
            return t;
        }
    }

    /// In-place iterative radix-2 FFT (N a power of two)
    public static void Fft(float[] re, float[] im, float[][] tw)
    {
        int N = re.Length;
        for (int i = 1, j = 0; i < N; i++)
        {
            int bit = N >> 1;
            for (; (j & bit) != 0; bit >>= 1) j ^= bit;
            j ^= bit;
            if (i < j) { float t = re[i]; re[i] = re[j]; re[j] = t; t = im[i]; im[i] = im[j]; im[j] = t; }
        }
        var cos = tw[0]; var sin = tw[1];
        for (int len = 2; len <= N; len <<= 1)
        {
            int half = len >> 1, step = N / len;
            for (int i = 0; i < N; i += len)
                for (int k = 0; k < half; k++)
                {
                    float wr = cos[k * step], wi = sin[k * step];
                    int u = i + k, v = u + half;
                    float xr = re[v] * wr - im[v] * wi, xi = re[v] * wi + im[v] * wr;
                    re[v] = re[u] - xr; im[v] = im[u] - xi;
                    re[u] += xr; im[u] += xi;
                }
        }
    }

    static void Normalise(float[] v)
    {
        double m = 0; for (int i = 0; i < v.Length; i++) m += v[i];
        m /= Math.Max(1, v.Length);
        if (m <= 1e-9) return;
        float inv = (float)(1.0 / m);
        for (int i = 0; i < v.Length; i++) v[i] *= inv;
    }

    static float[] Smooth(float[] v, float sigma)
    {
        int half = Math.Max(1, (int)Math.Ceiling(sigma * 3f));
        var k = new float[2 * half + 1]; float ks = 0f;
        for (int i = -half; i <= half; i++) { k[i + half] = (float)Math.Exp(-0.5 * i * i / (sigma * sigma)); ks += k[i + half]; }
        var o = new float[v.Length];
        for (int i = 0; i < v.Length; i++)
        {
            float s = 0f;
            for (int j = -half; j <= half; j++) { int p = i + j; if (p >= 0 && p < v.Length) s += v[p] * k[j + half]; }
            o[i] = s / ks;
        }
        return o;
    }

    static double[] Acf(float[] v, int maxLag)
    {
        double mean = 0; for (int i = 0; i < v.Length; i++) mean += v[i]; mean /= v.Length;
        var acf = new double[maxLag];
        for (int L = 0; L < maxLag; L++)
        {
            double s = 0; int c = 0;
            for (int i = 0; i + L < v.Length; i++) { s += (v[i] - mean) * (v[i + L] - mean); c++; }
            acf[L] = c > 0 ? s / c : 0;
        }
        double a0 = acf[0] > 1e-12 ? acf[0] : 1;
        for (int L = 0; L < maxLag; L++) acf[L] /= a0;
        return acf;
    }

    /// Ellis (2007): the beat sequence that maximises onset strength while each gap stays near one period
    static int[] Track(float[] ls, double period, float alpha)
    {
        int n = ls.Length;
        var score = new double[n]; var back = new int[n];
        int pMin = (int)Math.Round(period * 0.5), pMax = (int)Math.Round(period * 2.0);
        for (int i = 0; i < n; i++)
        {
            double best = 0; int bp = -1;
            for (int p = i - pMax; p <= i - pMin; p++)
            {
                if (p < 0) continue;
                double lg = Math.Log((i - p) / period);
                double v = score[p] - alpha * lg * lg;
                if (bp < 0 || v > best) { best = v; bp = p; }
            }
            score[i] = ls[i] + (bp >= 0 ? best : 0);
            back[i] = bp;
        }
        int end = n - 1; double bestEnd = double.NegativeInfinity;
        for (int i = Math.Max(0, n - pMax); i < n; i++) if (score[i] > bestEnd) { bestEnd = score[i]; end = i; }
        var list = new List<int>();
        for (int i = end; i >= 0; i = back[i]) { list.Add(i); if (back[i] < 0) break; }
        list.Reverse();
        return list.ToArray();
    }

    static float PeakNear(float[] v, int f, int radius)
    {
        float m = 0f;
        for (int i = f - radius; i <= f + radius; i++) if (i >= 0 && i < v.Length && v[i] > m) m = v[i];
        return m;
    }

    static float[] ChromaBetween(float[][] chroma, double fps, double t0, double t1)
    {
        var c = new float[12];
        int f0 = Math.Max(0, (int)Math.Floor(t0 * fps)), f1 = Math.Min(chroma.Length - 1, (int)Math.Ceiling(t1 * fps));
        if (f1 < f0) f1 = f0;
        for (int f = f0; f <= f1 && f < chroma.Length; f++) { var x = chroma[f]; if (x != null) for (int p = 0; p < 12; p++) c[p] += x[p]; }
        float s = 0f; for (int p = 0; p < 12; p++) s += c[p];
        if (s > 0f) for (int p = 0; p < 12; p++) c[p] /= s;
        return c;
    }

    static float Cosine(float[] a, float[] b)
    {
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < 12; i++) { dot += a[i] * b[i]; na += a[i] * a[i]; nb += b[i] * b[i]; }
        return na > 0 && nb > 0 ? (float)(dot / Math.Sqrt(na * nb)) : 1f;
    }

    static double Correlate(float[] chroma, float[] profile, int shift)
    {
        double mx = 0, my = 0;
        for (int i = 0; i < 12; i++) { mx += chroma[i]; my += profile[i]; }
        mx /= 12; my /= 12;
        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < 12; i++)
        {
            double x = chroma[(i + shift) % 12] - mx, y = profile[i] - my;
            sxy += x * y; sxx += x * x; syy += y * y;
        }
        return sxx > 0 && syy > 0 ? sxy / Math.Sqrt(sxx * syy) : 0;
    }
}
