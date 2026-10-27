// A small software renderer: enough of URP's behaviour (linear light, exp² fog by view depth, queue-ordered alpha
// blending with no depth write, trilight ambient, two directional lights, a fresnel sky reflection, a threshold bloom,
// the menu's grade and veil) to LOOK at the menu city from the menu camera's own orbit without opening Unity.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using UnityEngine;
using Color = UnityEngine.Color;

class Tri
{
    public Vector3 a, b, c, n;
    public Vector2 ua, ub, uc;
    public Material m;
}

static class Preview
{
    static int W = 1280, H = 720;
    static float[] depth, fr, fg, fb;
    static Vector3 camPos, camR, camU, camF;
    static float fx, fy;
    static Color fogLin;
    const float FogDensity = 0.00095f;
    static Texture2D dome;
    static readonly Vector3 SunL = new Vector3(0.338f, 0.156f, 0.928f), FillL = new Vector3(-0.204f, 0.616f, -0.761f);
    static Vector3 sunC, fillC, ambSky, ambEq, ambGround;

    static Vector3 Lin(float r, float g, float b) => new Vector3(Mathf.GammaToLinearSpace(r), Mathf.GammaToLinearSpace(g), Mathf.GammaToLinearSpace(b));

    // ------------------------------------------------------------ geometry

    static void AddQuad(List<Tri> outTris, Material m, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector2 u0, Vector2 u1, Vector2 u2, Vector2 u3)
    {
        Vector3 n = Vector3.Cross(p1 - p0, p2 - p0).normalized;
        outTris.Add(new Tri { a = p0, b = p1, c = p2, ua = u0, ub = u1, uc = u2, n = n, m = m });
        outTris.Add(new Tri { a = p0, b = p2, c = p3, ua = u0, ub = u2, uc = u3, n = n, m = m });
    }

    static void AddBox(List<Tri> t, MeshBaker.Box bx)
    {
        Func<float, float, float, Vector3> P = (x, y, z) => bx.pos + bx.rot * new Vector3(x * bx.size.x, y * bx.size.y, z * bx.size.z);
        Vector2 z0 = new Vector2(0, 0), z1 = new Vector2(0, 1), z2 = new Vector2(1, 1), z3 = new Vector2(1, 0);
        if (bx.type == PrimitiveType.Cube)
        {
            float h = 0.5f;
            AddQuad(t, bx.mat, P(-h, -h, -h), P(-h, h, -h), P(h, h, -h), P(h, -h, -h), z0, z1, z2, z3);
            AddQuad(t, bx.mat, P(h, -h, h), P(h, h, h), P(-h, h, h), P(-h, -h, h), z0, z1, z2, z3);
            AddQuad(t, bx.mat, P(-h, -h, h), P(-h, h, h), P(-h, h, -h), P(-h, -h, -h), z0, z1, z2, z3);
            AddQuad(t, bx.mat, P(h, -h, -h), P(h, h, -h), P(h, h, h), P(h, -h, h), z0, z1, z2, z3);
            AddQuad(t, bx.mat, P(-h, h, -h), P(-h, h, h), P(h, h, h), P(h, h, -h), z0, z1, z2, z3);
            return;
        }
        if (bx.type == PrimitiveType.Cylinder)
        {
            const int N = 14;
            for (int i = 0; i < N; i++)
            {
                float a0 = i * Mathf.PI * 2f / N, a1 = (i + 1) * Mathf.PI * 2f / N;
                float x0 = Mathf.Sin(a0) * 0.5f, zz0 = Mathf.Cos(a0) * 0.5f, x1 = Mathf.Sin(a1) * 0.5f, zz1 = Mathf.Cos(a1) * 0.5f;
                AddQuad(t, bx.mat, P(x1, -1, zz1), P(x1, 1, zz1), P(x0, 1, zz0), P(x0, -1, zz0), z0, z1, z2, z3);
                AddQuad(t, bx.mat, P(0, 1, 0), P(x0, 1, zz0), P(x1, 1, zz1), P(0, 1, 0), z0, z1, z2, z3);
                AddQuad(t, bx.mat, P(0, -1, 0), P(x1, -1, zz1), P(x0, -1, zz0), P(0, -1, 0), z0, z1, z2, z3);
            }
            return;
        }
        // sphere
        const int A = 10, B = 6;
        for (int j = 0; j < B; j++)
            for (int i = 0; i < A; i++)
            {
                Func<int, int, Vector3> S = (ii, jj) =>
                {
                    float az = ii * Mathf.PI * 2f / A, el = (jj / (float)B - 0.5f) * Mathf.PI;
                    return P(Mathf.Sin(az) * Mathf.Cos(el) * 0.5f, Mathf.Sin(el) * 0.5f, Mathf.Cos(az) * Mathf.Cos(el) * 0.5f);
                };
                AddQuad(t, bx.mat, S(i + 1, j), S(i + 1, j + 1), S(i, j + 1), S(i, j), z0, z1, z2, z3);
            }
    }

    static void AddMeshes(List<Tri> t, List<Renderer> made)
    {
        foreach (var r in made)
        {
            var mesh = r.gameObject.GetComponent<MeshFilter>().sharedMesh;
            for (int i = 0; i < mesh.tris.Count; i += 3)
            {
                int a = mesh.tris[i], b = mesh.tris[i + 1], c = mesh.tris[i + 2];
                t.Add(new Tri { a = mesh.verts[a], b = mesh.verts[b], c = mesh.verts[c], ua = mesh.uvs[a], ub = mesh.uvs[b], uc = mesh.uvs[c], n = mesh.normals[a], m = r.sharedMaterial });
            }
        }
    }

    static readonly Dictionary<Material, Material> mirrorMats = new Dictionary<Material, Material>();

    static List<Tri> Mirrored(List<Tri> src)
    {
        var o = new List<Tri>(src.Count);
        foreach (var s in src)
        {
            Material m = s.m;
            if (m.renderQueue >= 2500)
            {
                if (!mirrorMats.TryGetValue(m, out var mm)) { mm = new Material(m) { renderQueue = 2880 }; mirrorMats[m] = mm; }
                m = mm;
            }
            Func<Vector3, Vector3> F = v => new Vector3(v.x, -v.y, v.z);
            o.Add(new Tri { a = F(s.a), b = F(s.b), c = F(s.c), ua = s.ua, ub = s.ub, uc = s.uc, n = F(s.n), m = m });
        }
        return o;
    }

    // ------------------------------------------------------------ textures

    static void Texel(Texture2D t, int lvl, int x, int y, out float r, out float g, out float b, out float a)
    {
        int w = t.mipW[lvl], h = t.mipH[lvl];
        x = t.wrapModeU == TextureWrapMode.Repeat ? ((x % w) + w) % w : Mathf.Clamp(x, 0, w - 1);
        y = t.wrapModeV == TextureWrapMode.Repeat ? ((y % h) + h) % h : Mathf.Clamp(y, 0, h - 1);
        var d = t.mips[lvl];
        int i = (y * w + x) * 4;
        r = d[i]; g = d[i + 1]; b = d[i + 2]; a = d[i + 3];
    }

    static void Bilinear(Texture2D t, int lvl, float u, float v, out float r, out float g, out float b, out float a)
    {
        int w = t.mipW[lvl], h = t.mipH[lvl];
        float x = u * w - 0.5f, y = v * h - 0.5f;
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float tx = x - x0, ty = y - y0;
        float r00, g00, b00, a00, r10, g10, b10, a10, r01, g01, b01, a01, r11, g11, b11, a11;
        Texel(t, lvl, x0, y0, out r00, out g00, out b00, out a00); Texel(t, lvl, x0 + 1, y0, out r10, out g10, out b10, out a10);
        Texel(t, lvl, x0, y0 + 1, out r01, out g01, out b01, out a01); Texel(t, lvl, x0 + 1, y0 + 1, out r11, out g11, out b11, out a11);
        r = (r00 * (1 - tx) + r10 * tx) * (1 - ty) + (r01 * (1 - tx) + r11 * tx) * ty;
        g = (g00 * (1 - tx) + g10 * tx) * (1 - ty) + (g01 * (1 - tx) + g11 * tx) * ty;
        b = (b00 * (1 - tx) + b10 * tx) * (1 - ty) + (b01 * (1 - tx) + b11 * tx) * ty;
        a = (a00 * (1 - tx) + a10 * tx) * (1 - ty) + (a01 * (1 - tx) + a11 * tx) * ty;
    }

    static void Sample(Texture2D t, float u, float v, float lod, out float r, out float g, out float b, out float a)
    {
        int top = t.mips.Count - 1;
        lod = Mathf.Clamp(lod, 0f, top);
        int l0 = (int)lod, l1 = Math.Min(top, l0 + 1);
        float k = lod - l0;
        float r0, g0, b0, a0, r1, g1, b1, a1;
        Bilinear(t, l0, u, v, out r0, out g0, out b0, out a0);
        if (k < 0.01f || l1 == l0) { r = r0; g = g0; b = b0; a = a0; return; }
        Bilinear(t, l1, u, v, out r1, out g1, out b1, out a1);
        r = r0 + (r1 - r0) * k; g = g0 + (g1 - g0) * k; b = b0 + (b1 - b0) * k; a = a0 + (a1 - a0) * k;
    }

    static Vector3 SkyAt(Vector3 dir)
    {
        float az = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        if (az < 0f) az += 360f;
        float el = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
        float r, g, b, a;
        Sample(dome, az / 360f, (el + 90f) / 180f, 0f, out r, out g, out b, out a);
        return new Vector3(r, g, b);
    }

    static Vector3 EnvAt(Vector3 dir)
    {
        float el = Mathf.Abs(Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg);
        float r, g, b, a;
        Sample(dome, (MenuSky.SunAzimuth + 90f) / 360f, (el + 90f) / 180f, 0f, out r, out g, out b, out a);
        return new Vector3(r, g, b) * 1.15f;
    }

    // ------------------------------------------------------------ raster

    struct V { public float x, y, z, iw, uw, vw; public Vector3 wp; }   // screen x,y; view z; 1/z; u/z; v/z

    static V Project(Vector3 view, Vector2 uv, Vector3 world)
    {
        float iz = 1f / view.z;
        return new V { x = (view.x * fx * iz * 0.5f + 0.5f) * W, y = (1f - (view.y * fy * iz * 0.5f + 0.5f)) * H, z = view.z, iw = iz, uw = uv.x * iz, vw = uv.y * iz, wp = world };
    }

    static Vector3 ToView(Vector3 p) { Vector3 d = p - camPos; return new Vector3(Vector3.Dot(d, camR), Vector3.Dot(d, camU), Vector3.Dot(d, camF)); }

    static readonly List<Vector3> clipV = new List<Vector3>(), clipW = new List<Vector3>();
    static readonly List<Vector2> clipUV = new List<Vector2>();

    static void Draw(Tri t, bool transparentPass)
    {
        const float Near = 1f;
        Vector3[] vw = { ToView(t.a), ToView(t.b), ToView(t.c) };
        Vector3[] ww = { t.a, t.b, t.c };
        Vector2[] uv = { t.ua, t.ub, t.uc };
        if (vw[0].z < Near && vw[1].z < Near && vw[2].z < Near) return;
        clipV.Clear(); clipW.Clear(); clipUV.Clear();
        for (int i = 0; i < 3; i++)
        {
            int j = (i + 1) % 3;
            bool inI = vw[i].z >= Near, inJ = vw[j].z >= Near;
            if (inI) { clipV.Add(vw[i]); clipW.Add(ww[i]); clipUV.Add(uv[i]); }
            if (inI != inJ)
            {
                float k = (Near - vw[i].z) / (vw[j].z - vw[i].z);
                clipV.Add(vw[i] + (vw[j] - vw[i]) * k); clipW.Add(ww[i] + (ww[j] - ww[i]) * k); clipUV.Add(uv[i] + (uv[j] - uv[i]) * k);
            }
        }
        for (int i = 1; i + 1 < clipV.Count; i++)
            Raster(t, Project(clipV[0], clipUV[0], clipW[0]), Project(clipV[i], clipUV[i], clipW[i]), Project(clipV[i + 1], clipUV[i + 1], clipW[i + 1]), transparentPass);
    }

    static void Raster(Tri t, V a, V b, V c, bool transparentPass)
    {
        float minX = Math.Min(a.x, Math.Min(b.x, c.x)), maxX = Math.Max(a.x, Math.Max(b.x, c.x));
        float minY = Math.Min(a.y, Math.Min(b.y, c.y)), maxY = Math.Max(a.y, Math.Max(b.y, c.y));
        if (maxX < 0 || maxY < 0 || minX >= W || minY >= H) return;
        int x0 = Math.Max(0, (int)Math.Floor(minX)), x1 = Math.Min(W - 1, (int)Math.Ceiling(maxX));
        int y0 = Math.Max(0, (int)Math.Floor(minY)), y1 = Math.Min(H - 1, (int)Math.Ceiling(maxY));
        float area = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        if (Math.Abs(area) < 1e-6f) return;
        float inv = 1f / area;
        Material m = t.m;
        Texture2D tex = m.map;
        int tw = tex != null ? tex.width : 1, th = tex != null ? tex.height : 1;
        // gradients of the perspective-divided quantities are constant over the triangle
        float ex1 = b.x - a.x, ey1 = b.y - a.y, ex2 = c.x - a.x, ey2 = c.y - a.y;
        float iwX = ((b.iw - a.iw) * ey2 - (c.iw - a.iw) * ey1) * inv, iwY = ((c.iw - a.iw) * ex1 - (b.iw - a.iw) * ex2) * inv;
        float uwX = ((b.uw - a.uw) * ey2 - (c.uw - a.uw) * ey1) * inv, uwY = ((c.uw - a.uw) * ex1 - (b.uw - a.uw) * ex2) * inv;
        float vwX = ((b.vw - a.vw) * ey2 - (c.vw - a.vw) * ey1) * inv, vwY = ((c.vw - a.vw) * ex1 - (b.vw - a.vw) * ex2) * inv;
        Vector3 mc = new Vector3(m.color.r, m.color.g, m.color.b);
        Vector3 colLin = new Vector3(Mathf.GammaToLinearSpace(mc.x), Mathf.GammaToLinearSpace(mc.y), Mathf.GammaToLinearSpace(mc.z));
        bool lit = m.Lit;
        float smooth = m.Get("_Smoothness", 0.5f);
        Vector3 n = t.n;

        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float w0 = ((b.x - px) * (c.y - py) - (b.y - py) * (c.x - px)) * inv;
                float w1 = ((c.x - px) * (a.y - py) - (c.y - py) * (a.x - px)) * inv;
                float w2 = 1f - w0 - w1;
                if (w0 < -1e-5f || w1 < -1e-5f || w2 < -1e-5f) continue;
                float iw = w0 * a.iw + w1 * b.iw + w2 * c.iw;
                if (iw <= 0f) continue;
                float z = 1f / iw;
                int idx = y * W + x;
                if (z >= depth[idx]) continue;
                float uw = w0 * a.uw + w1 * b.uw + w2 * c.uw, vw2 = w0 * a.vw + w1 * b.vw + w2 * c.vw;
                float u = uw * z, v = vw2 * z;
                float tr = 1f, tg = 1f, tb = 1f, ta = 1f;
                if (tex != null)
                {
                    float zx = 1f / (iw + iwX), zy = 1f / (iw + iwY);
                    float dux = ((uw + uwX) * zx - u) * tw, dvx = ((vw2 + vwX) * zx - v) * th, duy = ((uw + uwY) * zy - u) * tw, dvy = ((vw2 + vwY) * zy - v) * th;
                    float rx = Mathf.Sqrt(dux * dux + dvx * dvx), ry = Mathf.Sqrt(duy * duy + dvy * dvy);
                    float major = Math.Max(rx, ry), minor = Math.Min(rx, ry);
                    float rho = Math.Max(minor, major / Math.Max(1, tex.anisoLevel));
                    float lod = rho > 1f ? (float)Math.Log(rho, 2.0) : 0f;
                    Sample(tex, u + m.offset.x, v + m.offset.y, lod, out tr, out tg, out tb, out ta);
                }
                Vector3 wp = a.wp * (w0 * a.iw * z) + b.wp * (w1 * b.iw * z) + c.wp * (w2 * c.iw * z);
                Vector3 rgb;
                float alpha = ta * m.color.a;
                if (lit)
                {
                    Vector3 albedo = new Vector3(tr * colLin.x, tg * colLin.y, tb * colLin.z);
                    Vector3 view = (camPos - wp).normalized;
                    Vector3 nn = Vector3.Dot(n, view) < 0f ? -n : n;
                    Vector3 amb = nn.y >= 0f ? ambEq + (ambSky - ambEq) * nn.y : ambEq + (ambGround - ambEq) * (-nn.y);
                    Vector3 light = amb + sunC * Math.Max(0f, Vector3.Dot(nn, SunL)) + fillC * Math.Max(0f, Vector3.Dot(nn, FillL));
                    Vector3 diffuse = Vector3.Scale(albedo, light) * 0.96f;
                    float nv = Mathf.Clamp01(Vector3.Dot(nn, view));
                    float fres = Mathf.Pow(1f - nv, 4f);
                    float grazing = Mathf.Clamp01(smooth + 0.04f);
                    float rough = (1f - smooth) * (1f - smooth);
                    Vector3 refl = EnvAt(2f * Vector3.Dot(nn, view) * nn - view) * ((0.04f + (grazing - 0.04f) * fres) / (rough * rough + 1f));
                    rgb = m.transparent ? diffuse * alpha + refl : diffuse + refl;
                }
                else rgb = new Vector3(tr * colLin.x, tg * colLin.y, tb * colLin.z);
                float fogK = Mathf.Exp(-(FogDensity * z) * (FogDensity * z));
                Vector3 fogged = new Vector3(fogLin.r, fogLin.g, fogLin.b);
                if (lit && m.transparent) rgb = rgb * fogK + fogged * (alpha * (1f - fogK));
                else rgb = rgb * fogK + fogged * (1f - fogK);

                if (!transparentPass)
                {
                    depth[idx] = z;
                    fr[idx] = rgb.x; fg[idx] = rgb.y; fb[idx] = rgb.z;
                }
                else if (lit && m.transparent)
                {
                    fr[idx] = rgb.x + fr[idx] * (1f - alpha); fg[idx] = rgb.y + fg[idx] * (1f - alpha); fb[idx] = rgb.z + fb[idx] * (1f - alpha);
                }
                else
                {
                    fr[idx] = rgb.x * alpha + fr[idx] * (1f - alpha); fg[idx] = rgb.y * alpha + fg[idx] * (1f - alpha); fb[idx] = rgb.z * alpha + fb[idx] * (1f - alpha);
                }
            }
    }

    // ------------------------------------------------------------ post

    static float[] Blur(float[] src, int w, int h, int radius)
    {
        var tmp = new float[src.Length]; var dst = new float[src.Length];
        float sigma = radius * 0.5f;
        var k = new float[radius * 2 + 1]; float sum = 0f;
        for (int i = -radius; i <= radius; i++) { k[i + radius] = (float)Math.Exp(-(i * i) / (2f * sigma * sigma)); sum += k[i + radius]; }
        for (int i = 0; i < k.Length; i++) k[i] /= sum;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) { float s = 0f; for (int i = -radius; i <= radius; i++) s += src[y * w + Mathf.Clamp(x + i, 0, w - 1)] * k[i + radius]; tmp[y * w + x] = s; }
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) { float s = 0f; for (int i = -radius; i <= radius; i++) s += tmp[Mathf.Clamp(y + i, 0, h - 1) * w + x] * k[i + radius]; dst[y * w + x] = s; }
        return dst;
    }

    static void Post(string path, float bloomIntensity, float bloomThreshold, bool veil)
    {
        int qw = W / 4, qh = H / 4;
        var br = new float[qw * qh]; var bg = new float[qw * qh]; var bb = new float[qw * qh];
        for (int y = 0; y < qh; y++) for (int x = 0; x < qw; x++)
        {
            float r = 0, g = 0, b = 0;
            for (int j = 0; j < 4; j++) for (int i = 0; i < 4; i++) { int idx = (y * 4 + j) * W + x * 4 + i; r += Math.Min(fr[idx], 8f); g += Math.Min(fg[idx], 8f); b += Math.Min(fb[idx], 8f); }
            r /= 16f; g /= 16f; b /= 16f;
            float lum = Math.Max(r, Math.Max(g, b));
            float soft = Mathf.Clamp(lum - bloomThreshold + bloomThreshold * 0.5f, 0f, bloomThreshold);
            soft = soft * soft / (2f * bloomThreshold + 1e-4f);
            float contrib = Math.Max(soft, lum - bloomThreshold) / Math.Max(lum, 1e-4f);
            br[y * qw + x] = r * contrib; bg[y * qw + x] = g * contrib; bb[y * qw + x] = b * contrib;
        }
        int[] radii = { 2, 6, 16, 36 };
        var accR = new float[qw * qh]; var accG = new float[qw * qh]; var accB = new float[qw * qh];
        foreach (int rad in radii)
        {
            var r2 = Blur(br, qw, qh, rad); var g2 = Blur(bg, qw, qh, rad); var b2 = Blur(bb, qw, qh, rad);
            for (int i = 0; i < accR.Length; i++) { accR[i] += r2[i] / radii.Length; accG[i] += g2[i] / radii.Length; accB[i] += b2[i] / radii.Length; }
        }
        using (var bmp = new Bitmap(W, H, PixelFormat.Format24bppRgb))
        {
            var data = bmp.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            var bytes = new byte[data.Stride * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int idx = y * W + x, q = Math.Min(qh - 1, y / 4) * qw + Math.Min(qw - 1, x / 4);
                    float r = fr[idx] + accR[q] * bloomIntensity, g = fg[idx] + accG[q] * bloomIntensity, b = fb[idx] + accB[q] * bloomIntensity;
                    // neutral-ish shoulder
                    r = Shoulder(r); g = Shoulder(g); b = Shoulder(b);
                    // to display space, then the grade: contrast +12, saturation -8, a cool filter
                    r = Mathf.LinearToGammaSpace(r); g = Mathf.LinearToGammaSpace(g); b = Mathf.LinearToGammaSpace(b);
                    r = (r - 0.5f) * 1.12f + 0.5f; g = (g - 0.5f) * 1.12f + 0.5f; b = (b - 0.5f) * 1.12f + 0.5f;
                    float l = 0.2126f * r + 0.7152f * g + 0.0722f * b;
                    r = l + (r - l) * 0.92f; g = l + (g - l) * 0.92f; b = l + (b - l) * 0.92f;
                    r *= 0.96f; g *= 0.98f;
                    // vignette
                    float vx = (x + 0.5f) / W - 0.5f, vy = (y + 0.5f) / H - 0.5f;
                    float vig = 1f - 0.3f * Mathf.Clamp01((vx * vx + vy * vy) * 2.2f);
                    r *= vig; g *= vig; b *= vig;
                    if (veil) { r = r * 0.7f + 0.03f * 0.3f; g = g * 0.7f + 0.05f * 0.3f; b = b * 0.7f + 0.06f * 0.3f; }
                    int o = y * data.Stride + x * 3;
                    bytes[o] = (byte)(Mathf.Clamp01(b) * 255f); bytes[o + 1] = (byte)(Mathf.Clamp01(g) * 255f); bytes[o + 2] = (byte)(Mathf.Clamp01(r) * 255f);
                }
            System.Runtime.InteropServices.Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
            bmp.UnlockBits(data);
            bmp.Save(path, ImageFormat.Png);
        }
    }

    static float Shoulder(float x) => x < 0.7f ? x : 0.7f + 0.3f * (float)Math.Tanh((x - 0.7f) / 0.3f * 0.75f);

    static void SaveTexture(Texture2D t, string path, bool showAlphaOnBlack)
    {
        int w = t.width, h = t.height;
        using (var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb))
        {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = ((h - 1 - y) * w + x) * 4;
                    float a = showAlphaOnBlack ? t.mips[0][i + 3] : 1f;
                    float r = Mathf.LinearToGammaSpace(t.mips[0][i] * a), g = Mathf.LinearToGammaSpace(t.mips[0][i + 1] * a), b = Mathf.LinearToGammaSpace(t.mips[0][i + 2] * a);
                    bmp.SetPixel(x, y, System.Drawing.Color.FromArgb((int)(Mathf.Clamp01(r) * 255), (int)(Mathf.Clamp01(g) * 255), (int)(Mathf.Clamp01(b) * 255)));
                }
            bmp.Save(path, ImageFormat.Png);
        }
    }

    // ------------------------------------------------------------ main

    static void Main(string[] args)
    {
        string outDir = args.Length > 0 ? args[0] : ".";
        if (args.Length > 1) { W = int.Parse(args[1]); H = W * 9 / 16; }
        var sw = System.Diagnostics.Stopwatch.StartNew();
        CityKit.Init(true);
        MenuCity.Init();
        UnityEngine.Random.InitState(1907);
        var baker = new MeshBaker(); var sheets = new SheetBaker(); var ground = new SheetBaker(); var clouds = new SheetBaker();
        ground.Grid(MenuCity.Ground, Vector3.zero, 9600f, 9600f, 40, 40, 260f);
        CityKit.Tower(baker, Vector3.zero, MenuBackdrop.TowerScale);
        MenuCity.BuildStill(baker, sheets, ground);
        // SMOKE: the levels build their route towers and neighbours through OuterTower/OuterBlock WEST and SOUTH of their
        // origin (negative coordinates), where a seed made from a position is negative. Every variant is built once there,
        // lit and unlit, into bakers that are thrown away: a bad seed index throws here and not in a level
        {
            var smokeBoxes = new MeshBaker(); var smokeSheets = new SheetBaker();
            int beaconsBefore = MenuCity.Beacons.Count;
            for (int v = -1; v <= MenuCity.Variants; v++)   // one past each end too: the modulus must take any int
                foreach (bool lit in new[] { true, false })
                {
                    MenuCity.OuterTower(smokeBoxes, smokeSheets, new Vector3(-137f, 0f, -213f), 40f, 32f, 180f, 27f, v, lit);
                    MenuCity.OuterBlock(smokeBoxes, smokeSheets, new Vector3(-1730f, 0f, -905f), 48f, 40f, 260f, -63f, v, lit, 75f);
                }
            while (MenuCity.Beacons.Count > beaconsBefore) MenuCity.Beacons.RemoveAt(MenuCity.Beacons.Count - 1);   // the smoke towers' beacons are not the menu's
            Console.WriteLine("smoke: every OuterTower/OuterBlock variant built at negative coordinates, lit and unlit");
        }
        // The two rings beyond the plan, as HalcyonCity raises them for the levels: buildings, not boxes (MenuBackdrop
        // still passes no sheets, so the menu's own two rings stay the plain masses until it does)
        int farBoxes = baker.boxes.Count, farQuads = sheets.Quads;
        CityKit.Skyline(baker, Vector3.zero, 0f, 110, 1400f, 1950f, 0f, 360f, 1.5f, sheets);
        CityKit.Skyline(baker, Vector3.zero, 0f, 90, 1950f, 2600f, 3f, 363f, 2.2f, sheets);
        Console.WriteLine("far rings: " + (baker.boxes.Count - farBoxes) + " boxes, " + (sheets.Quads - farQuads) + " wall/window quads for 200 masses");
        MenuSky.AddDecks(null, clouds);
        Console.WriteLine("built: " + baker.boxes.Count + " boxes, " + sheets.Quads + " wall/window quads, " + ground.Quads + " ground quads, heroes " + MenuCity.Heroes.Count + ", beacons " + MenuCity.Beacons.Count + "  (" + sw.ElapsedMilliseconds + " ms incl. textures)");

        var still = new List<Tri>();
        foreach (var bx in baker.boxes) AddBox(still, bx);
        AddMeshes(still, sheets.Flush(null, "s"));
        foreach (var p in MenuCity.Beacons) AddBox(still, new MeshBaker.Box { mat = CityKit.beaconRed, pos = p, size = Vector3.one * 3f, rot = Quaternion.identity, type = PrimitiveType.Cube });
        var all = new List<Tri>(still);
        all.AddRange(Mirrored(still));
        AddMeshes(all, ground.Flush(null, "g"));
        AddMeshes(all, clouds.Flush(null, "c"));
        { int sp = 0, cy = 0, cu = 0; foreach (var bx in baker.boxes) { if (bx.type == PrimitiveType.Sphere) sp++; else if (bx.type == PrimitiveType.Cylinder) cy++; else cu++; } Console.WriteLine("boxes: cubes " + cu + ", spheres " + sp + ", cylinders " + cy); }
        Console.WriteLine("triangles: " + all.Count);
        {
            // The facades' arithmetic: of the atlas's cells how many are windows, and of those how many are lit
            string[] styles = { "rows", "strip", "homes", "arch", "house", "attic", "shop" };
            var line = new System.Text.StringBuilder("windows: ");
            for (int s = 0; s < styles.Length; s++)
            {
                CityTextures.Stats(s, out float windows, out float lit);
                line.Append(styles[s] + " " + (windows * 100f).ToString("0") + "% cells, " + (lit * 100f).ToString("0") + "% lit; ");
            }
            Console.WriteLine(line.ToString());
        }

        dome = CityTextures.SkyDome(MenuSky.SunAzimuth);
        if (args.Length > 2 && args[2] == "tex")
        {
            SaveTexture(CityTextures.Windows(CityTextures.Rows), outDir + "/tex_windows_rows.png", true);
            SaveTexture(CityTextures.Windows(CityTextures.Strip), outDir + "/tex_windows_strip.png", true);
            SaveTexture(CityTextures.Windows(CityTextures.Homes), outDir + "/tex_windows_homes.png", true);
            SaveTexture(CityTextures.Windows(CityTextures.Arch), outDir + "/tex_windows_arch.png", true);
            SaveTexture(CityTextures.Windows(CityTextures.House), outDir + "/tex_windows_house.png", true);
            SaveTexture(CityTextures.BandLight(false), outDir + "/tex_bandlight_shop.png", true);
            SaveTexture(CityTextures.BandLight(true), outDir + "/tex_bandlight_attic.png", true);
            SaveTexture(CityTextures.WallAtlas(CityTextures.KindLime), outDir + "/tex_wall_lime.png", false);
            SaveTexture(CityTextures.WallAtlas(CityTextures.KindBrick), outDir + "/tex_wall_brick.png", false);
            SaveTexture(CityTextures.WallAtlas(CityTextures.KindPierLime), outDir + "/tex_wall_piers.png", false);
            SaveTexture(CityTextures.WallAtlas(CityTextures.KindBrownstone), outDir + "/tex_wall_brownstone.png", false);
            SaveTexture(CityTextures.WallAtlas(CityTextures.KindBlindBrick), outDir + "/tex_wall_blindbrick.png", false);
            SaveTexture(CityTextures.WallBand(CityTextures.KindLime, false), outDir + "/tex_band_base_lime.png", false);
            SaveTexture(CityTextures.WallBand(CityTextures.KindLime, true), outDir + "/tex_band_attic_lime.png", false);
            SaveTexture(CityTextures.Clouds(), outDir + "/tex_clouds.png", true);
            SaveTexture(CityTextures.Promenade(), outDir + "/tex_promenade.png", true);
            SaveTexture(CityTextures.Carriages(), outDir + "/tex_carriages.png", true);
            SaveTexture(CityTextures.Traffic(), outDir + "/tex_traffic.png", true);
            SaveTexture(dome, outDir + "/tex_sky.png", false);
        }

        sunC = Lin(MenuSky.SunColor.r, MenuSky.SunColor.g, MenuSky.SunColor.b) * MenuSky.SunIntensity; fillC = Lin(MenuSky.FillColor.r, MenuSky.FillColor.g, MenuSky.FillColor.b) * MenuSky.FillIntensity;
        ambSky = Lin(MenuSky.AmbientSky.r, MenuSky.AmbientSky.g, MenuSky.AmbientSky.b); ambEq = Lin(MenuSky.AmbientEquator.r, MenuSky.AmbientEquator.g, MenuSky.AmbientEquator.b); ambGround = Lin(MenuSky.AmbientGround.r, MenuSky.AmbientGround.g, MenuSky.AmbientGround.b);

        var opaque = new List<Tri>(); var trans = new List<Tri>();
        foreach (var t in all) (t.m.renderQueue >= 2500 || t.m.transparent ? trans : opaque).Add(t);
        trans.Sort((p, q) => p.m.renderQueue.CompareTo(q.m.renderQueue));

        float[] times = { 0f, 38f, 95f, 150f, 215f, 300f };
        if (args.Length > 3) { var l = new List<float>(); foreach (var s in args[3].Split(',')) l.Add(float.Parse(s, System.Globalization.CultureInfo.InvariantCulture)); times = l.ToArray(); }
        foreach (float time in times)
        {
            var one = System.Diagnostics.Stopwatch.StartNew();
            float ang = time * 0.024f, radius = 470f + 40f * Mathf.Sin(time * 0.031f), height = 150f + 110f * Mathf.Sin(time * 0.017f + 1f);
            float th = CityKit.TowerH * MenuBackdrop.TowerScale;
            camPos = new Vector3(Mathf.Sin(ang) * radius, height, -Mathf.Cos(ang) * radius);
            Vector3 look = new Vector3(0f, th * (0.44f + 0.08f * Mathf.Sin(time * 0.021f)), 0f);
            if (args.Length > 4) { var c = args[4].Split(','); camPos = new Vector3(float.Parse(c[0]), float.Parse(c[1]), float.Parse(c[2])); look = new Vector3(float.Parse(c[3]), float.Parse(c[4]), float.Parse(c[5])); }
            camF = (look - camPos).normalized; camR = Vector3.Cross(Vector3.up, camF).normalized; camU = Vector3.Cross(camF, camR);
            fy = 1f / Mathf.Tan(64f * 0.5f * Mathf.Deg2Rad); fx = fy / ((float)W / H);
            Vector3 flat = camF; flat.y = 0f;
            float az = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
            fogLin = CityTextures.Haze(Mathf.Abs(Mathf.DeltaAngle(az, MenuSky.SunAzimuth))).linear;

            depth = new float[W * H]; fr = new float[W * H]; fg = new float[W * H]; fb = new float[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float sx = ((x + 0.5f) / W * 2f - 1f) / fx, sy = (1f - (y + 0.5f) / H * 2f) / fy;
                    Vector3 dir = (camF + camR * sx + camU * sy).normalized;
                    Vector3 c = SkyAt(dir);
                    int idx = y * W + x;
                    depth[idx] = float.MaxValue; fr[idx] = c.x; fg[idx] = c.y; fb[idx] = c.z;
                }
            foreach (var t in opaque) Draw(t, false);
            foreach (var t in trans) Draw(t, true);
            string name = outDir + "/menu_t" + ((int)time).ToString("000");
            Post(name + ".png", 0.6f, 1.0f, false);
            Post(name + "_veil.png", 0.6f, 1.0f, true);
            Console.WriteLine("t=" + time + " cam " + camPos.x.ToString("0") + "," + camPos.y.ToString("0") + "," + camPos.z.ToString("0") + "  " + one.ElapsedMilliseconds + " ms");
        }
    }
}

