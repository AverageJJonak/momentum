// A stand-in for just enough of UnityEngine to run the menu city's generator outside Unity and look at what it makes.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public static class Mathf
    {
        public const float PI = (float)Math.PI, Deg2Rad = PI / 180f, Rad2Deg = 180f / PI;
        public static float Sin(float x) => (float)Math.Sin(x);
        public static float Cos(float x) => (float)Math.Cos(x);
        public static float Tan(float x) => (float)Math.Tan(x);
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static float Asin(float x) => (float)Math.Asin(x);
        public static float Exp(float x) => (float)Math.Exp(x);
        public static float Pow(float x, float p) => (float)Math.Pow(x, p);
        public static float Sqrt(float x) => (float)Math.Sqrt(x);
        public static float Abs(float x) => Math.Abs(x);
        public static float Sign(float x) => x >= 0f ? 1f : -1f;
        public static float Min(float a, float b) => a < b ? a : b;
        public static float Max(float a, float b) => a > b ? a : b;
        public static int Min(int a, int b) => a < b ? a : b;
        public static int Max(int a, int b) => a > b ? a : b;
        public static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;
        public static int Clamp(int v, int a, int b) => v < a ? a : v > b ? b : v;
        public static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float Repeat(float t, float len) => Clamp(t - (float)Math.Floor(t / len) * len, 0f, len);
        public static float DeltaAngle(float a, float b) { float d = Repeat(b - a, 360f); if (d > 180f) d -= 360f; return d; }
        public static int FloorToInt(float x) => (int)Math.Floor(x);
        public static int CeilToInt(float x) => (int)Math.Ceiling(x);
        public static int RoundToInt(float x) => (int)Math.Round(x);
        public static float Round(float x) => (float)Math.Round(x);
        public static float LinearToGammaSpace(float v) => v <= 0f ? 0f : v <= 0.0031308f ? 12.92f * v : v < 1f ? 1.055f * Pow(v, 0.4166667f) - 0.055f : Pow(v, 0.45454545f);
        public static float GammaToLinearSpace(float v) => v <= 0.04045f ? v / 12.92f : v < 1f ? Pow((v + 0.055f) / 1.055f, 2.4f) : Pow(v, 2.2f);
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float magnitude => Mathf.Sqrt(x * x + y * y);
        public static Vector2 zero => new Vector2(0f, 0f);
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator *(Vector2 a, float k) => new Vector2(a.x * k, a.y * k);
        public static Vector2 operator /(Vector2 a, float k) => new Vector2(a.x / k, a.y / k);
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public float magnitude => Mathf.Sqrt(x * x + y * y + z * z);
        public float sqrMagnitude => x * x + y * y + z * z;
        public Vector3 normalized { get { float m = magnitude; return m > 1e-9f ? this / m : zero; } }
        public static Vector3 zero => new Vector3(0f, 0f, 0f);
        public static Vector3 one => new Vector3(1f, 1f, 1f);
        public static Vector3 up => new Vector3(0f, 1f, 0f);
        public static Vector3 down => new Vector3(0f, -1f, 0f);
        public static Vector3 forward => new Vector3(0f, 0f, 1f);
        public static Vector3 back => new Vector3(0f, 0f, -1f);
        public static Vector3 right => new Vector3(1f, 0f, 0f);
        public static Vector3 left => new Vector3(-1f, 0f, 0f);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator -(Vector3 a) => new Vector3(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float k) => new Vector3(a.x * k, a.y * k, a.z * k);
        public static Vector3 operator *(float k, Vector3 a) => new Vector3(a.x * k, a.y * k, a.z * k);
        public static Vector3 operator /(Vector3 a, float k) => new Vector3(a.x / k, a.y / k, a.z / k);
        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static Vector3 Cross(Vector3 a, Vector3 b) => new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        public static float Distance(Vector3 a, Vector3 b) => (a - b).magnitude;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) { t = Mathf.Clamp01(t); return a + (b - a) * t; }
        public static Vector3 Scale(Vector3 a, Vector3 b) => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
    }

    public struct Quaternion
    {
        public float x, y, z, w;
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion identity => new Quaternion(0f, 0f, 0f, 1f);
        public static Quaternion AngleAxis(float deg, Vector3 axis)
        {
            axis = axis.normalized;
            float h = deg * Mathf.Deg2Rad * 0.5f, s = Mathf.Sin(h);
            return new Quaternion(axis.x * s, axis.y * s, axis.z * s, Mathf.Cos(h));
        }
        /// Unity's order: z, then x, then y
        public static Quaternion Euler(float x, float y, float z) => AngleAxis(y, Vector3.up) * AngleAxis(x, Vector3.right) * AngleAxis(z, Vector3.forward);
        public static Quaternion operator *(Quaternion a, Quaternion b) => new Quaternion(
            a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
            a.w * b.y + a.y * b.w + a.z * b.x - a.x * b.z,
            a.w * b.z + a.z * b.w + a.x * b.y - a.y * b.x,
            a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z);
        public static Vector3 operator *(Quaternion q, Vector3 v)
        {
            Vector3 u = new Vector3(q.x, q.y, q.z);
            return v + 2f * Vector3.Cross(u, Vector3.Cross(u, v) + q.w * v);
        }
        public static Quaternion LookRotation(Vector3 fwd, Vector3 up)
        {
            fwd = fwd.normalized;
            Vector3 r = Vector3.Cross(up, fwd).normalized;
            if (r.sqrMagnitude < 1e-6f) r = Vector3.right;
            Vector3 u = Vector3.Cross(fwd, r);
            // rotation matrix columns r, u, fwd -> quaternion
            float m00 = r.x, m01 = u.x, m02 = fwd.x, m10 = r.y, m11 = u.y, m12 = fwd.y, m20 = r.z, m21 = u.z, m22 = fwd.z;
            float tr = m00 + m11 + m22;
            if (tr > 0f) { float s = Mathf.Sqrt(tr + 1f) * 2f; return new Quaternion((m21 - m12) / s, (m02 - m20) / s, (m10 - m01) / s, 0.25f * s); }
            if (m00 > m11 && m00 > m22) { float s = Mathf.Sqrt(1f + m00 - m11 - m22) * 2f; return new Quaternion(0.25f * s, (m01 + m10) / s, (m02 + m20) / s, (m21 - m12) / s); }
            if (m11 > m22) { float s = Mathf.Sqrt(1f + m11 - m00 - m22) * 2f; return new Quaternion((m01 + m10) / s, 0.25f * s, (m12 + m21) / s, (m02 - m20) / s); }
            { float s = Mathf.Sqrt(1f + m22 - m00 - m11) * 2f; return new Quaternion((m02 + m20) / s, (m12 + m21) / s, 0.25f * s, (m10 - m01) / s); }
        }
        public static Quaternion LookRotation(Vector3 fwd) => LookRotation(fwd, Vector3.up);
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1f, 1f, 1f, 1f);
        public static Color black => new Color(0f, 0f, 0f, 1f);
        public Color linear => new Color(Mathf.GammaToLinearSpace(r), Mathf.GammaToLinearSpace(g), Mathf.GammaToLinearSpace(b), a);
        public static Color operator +(Color a, Color b) => new Color(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a);
        public static Color operator *(Color a, float k) => new Color(a.r * k, a.g * k, a.b * k, a.a * k);
        public static Color operator *(Color a, Color b) => new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);
        public static Color operator /(Color a, float k) => new Color(a.r / k, a.g / k, a.b / k, a.a / k);
        public static Color Lerp(Color a, Color b, float t) { t = Mathf.Clamp01(t); return new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t); }
    }

    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
    }

    public static class Random
    {
        static System.Random rng = new System.Random(1907);
        public static void InitState(int seed) { rng = new System.Random(seed); }
        public static float value => (float)rng.NextDouble();
        public static float Range(float a, float b) => a + (b - a) * (float)rng.NextDouble();
        public static int Range(int a, int b) => b <= a ? a : a + rng.Next(b - a);
    }

    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }
    public enum TextureFormat { RGBA32, RGBAHalf }
    public enum FilterMode { Point, Bilinear, Trilinear }
    public enum TextureWrapMode { Repeat, Clamp }
    public enum HideFlags { None, HideAndDontSave }
    public enum CubemapFace { PositiveX, NegativeX, PositiveY, NegativeY, PositiveZ, NegativeZ }

    public class Object { public string name; public HideFlags hideFlags; }

    public class Texture : Object
    {
        public FilterMode filterMode;
        public int anisoLevel;
        public TextureWrapMode wrapModeU, wrapModeV;
    }

    public class Texture2D : Texture
    {
        public int width, height;
        public bool linear;
        public List<float[]> mips = new List<float[]>();   // linear RGBA, row 0 at the bottom
        public List<int> mipW = new List<int>(), mipH = new List<int>();
        public Texture2D(int w, int h, TextureFormat f, bool mip) : this(w, h, f, mip, false) { }
        public Texture2D(int w, int h, TextureFormat f, bool mip, bool linear) { width = w; height = h; this.linear = linear; }
        float[] baseLevel;
        public void SetPixels32(Color32[] px)
        {
            baseLevel = new float[px.Length * 4];
            for (int i = 0; i < px.Length; i++)
            {
                baseLevel[i * 4] = Mathf.GammaToLinearSpace(px[i].r / 255f);
                baseLevel[i * 4 + 1] = Mathf.GammaToLinearSpace(px[i].g / 255f);
                baseLevel[i * 4 + 2] = Mathf.GammaToLinearSpace(px[i].b / 255f);
                baseLevel[i * 4 + 3] = px[i].a / 255f;
            }
        }
        public void SetPixels(Color[] px)
        {
            baseLevel = new float[px.Length * 4];
            for (int i = 0; i < px.Length; i++) { baseLevel[i * 4] = px[i].r; baseLevel[i * 4 + 1] = px[i].g; baseLevel[i * 4 + 2] = px[i].b; baseLevel[i * 4 + 3] = px[i].a; }
        }
        public void Apply(bool mip, bool unreadable)
        {
            mips.Clear(); mipW.Clear(); mipH.Clear();
            mips.Add(baseLevel); mipW.Add(width); mipH.Add(height);
            int w = width, h = height;
            var cur = baseLevel;
            while (mip && (w > 1 || h > 1))
            {
                int nw = Math.Max(1, w / 2), nh = Math.Max(1, h / 2);
                var next = new float[nw * nh * 4];
                for (int y = 0; y < nh; y++)
                    for (int x = 0; x < nw; x++)
                        for (int c = 0; c < 4; c++)
                        {
                            int x0 = Math.Min(w - 1, x * 2), x1 = Math.Min(w - 1, x * 2 + 1), y0 = Math.Min(h - 1, y * 2), y1 = Math.Min(h - 1, y * 2 + 1);
                            next[(y * nw + x) * 4 + c] = 0.25f * (cur[(y0 * w + x0) * 4 + c] + cur[(y0 * w + x1) * 4 + c] + cur[(y1 * w + x0) * 4 + c] + cur[(y1 * w + x1) * 4 + c]);
                        }
                mips.Add(next); mipW.Add(nw); mipH.Add(nh);
                cur = next; w = nw; h = nh;
            }
        }
    }

    public class Cubemap : Texture
    {
        public Cubemap(int n, TextureFormat f, bool mip) { }
        public void SetPixels(Color[] px, CubemapFace face) { }
        public void Apply(bool a, bool b) { }
    }

    public class Shader : Object { public static Shader Find(string n) => new Shader { name = n }; }

    public class Material : Object
    {
        public Shader shader;
        public int renderQueue = 2000;
        public Color color = Color.white;
        public Texture2D map;
        public Vector2 offset;
        public bool transparent;
        public Dictionary<string, float> floats = new Dictionary<string, float>();
        public Material(Shader s) { shader = s; }
        public Material(Material m) { shader = m.shader; renderQueue = m.renderQueue; color = m.color; map = m.map; transparent = m.transparent; name = m.name; foreach (var p in m.floats) floats[p.Key] = p.Value; }
        public bool Lit => shader != null && shader.name.EndsWith("/Lit");
        public void SetTexture(string p, Texture t) { if (p == "_BaseMap") map = t as Texture2D; }
        public void SetColor(string p, Color c) { if (p == "_BaseColor") color = c; }
        public void SetFloat(string p, float v) { floats[p] = v; if (p == "_Surface" && v > 0.5f) transparent = true; }
        public void SetInt(string p, int v) { floats[p] = v; }
        public void SetOverrideTag(string a, string b) { }
        public void EnableKeyword(string k) { }
        public void SetShaderPassEnabled(string p, bool on) { }
        public float Get(string p, float d) => floats.TryGetValue(p, out var v) ? v : d;
    }

    public class Component : Object { public GameObject gameObject; public Transform transform => gameObject.transform; }
    public class Transform : Component { public Vector3 position, localScale, localPosition; public void SetParent(Transform p, bool stay) { } public void SetParent(Transform p) { } }
    public static class RenderSettings { public static Rendering.DefaultReflectionMode defaultReflectionMode; public static Texture customReflectionTexture; public static float reflectionIntensity; }
    public class Mesh : Object
    {
        public Rendering.IndexFormat indexFormat;
        public List<Vector3> verts, normals; public List<Vector2> uvs; public List<int> tris;
        public void SetVertices(List<Vector3> v) { verts = new List<Vector3>(v); }
        public void SetNormals(List<Vector3> v) { normals = new List<Vector3>(v); }
        public void SetUVs(int ch, List<Vector2> v) { uvs = new List<Vector2>(v); }
        public void SetTriangles(List<int> t, int sub) { tris = new List<int>(t); }
        public void RecalculateBounds() { }
    }
    public class MeshFilter : Component { public Mesh sharedMesh; }
    public class Renderer : Component
    {
        public bool enabled = true; public Material sharedMaterial; public Rendering.ShadowCastingMode shadowCastingMode; public bool receiveShadows; public Rendering.LightProbeUsage lightProbeUsage;
    }
    public class MeshRenderer : Renderer { }
    public class GameObject : Object
    {
        public Transform transform;
        public List<Component> parts = new List<Component>();
        public GameObject(string n) { name = n; transform = new Transform { gameObject = this }; }
        public T AddComponent<T>() where T : Component, new() { var c = new T { gameObject = this }; parts.Add(c); return c; }
        public T GetComponent<T>() where T : Component { foreach (var c in parts) if (c is T t) return t; return null; }
    }
}

namespace UnityEngine.Rendering
{
    public enum CullMode { Off, Front, Back }
    public enum BlendMode { Zero, One, DstColor, SrcColor, OneMinusDstColor, SrcAlpha, OneMinusSrcColor, DstAlpha, OneMinusDstAlpha, SrcAlphaSaturate, OneMinusSrcAlpha }
    public enum IndexFormat { UInt16, UInt32 }
    public enum ShadowCastingMode { Off, On }
    public enum LightProbeUsage { Off }
    public enum DefaultReflectionMode { Skybox, Custom }
    public enum RenderQueue { Background = 1000, Geometry = 2000, Transparent = 3000 }
}
