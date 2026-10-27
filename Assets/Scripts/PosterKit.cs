using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// THE POSTER KIT. The level cards' pictures are KEY ART, not photographs (user 2026-10-16: "dedicated images for the
/// thumbnails, look to ULTRAKILL"): a 1930s travel poster / Batman: The Animated Series cut-out illustration, built
/// as a diorama of flat UNLIT shapes in front of an orthographic camera on the thumbnail layer. The camera sees
/// 16 × 9 units (x ±8, y ±4.5); the layers sit at z = 1 (nearest) .. 8 (farthest) with the sky behind at 9. Depth
/// gives the layering; atmospheric perspective is colour: every opaque shape is mixed toward the palette's haze by
/// its depth (Tone), lights are not. No Light anywhere: three shared URP Unlit materials (opaque, alpha, additive),
/// the colour and the texture per renderer through one property block. Every shape placed sits a hair nearer than
/// the one before, so the painter's order is the draw order and nothing z-fights. Built once, shot once, released:
/// the meshes and textures made for a poster are owned by it (Release); the shared textures live in PosterLook.
/// One System.Random per poster, seeded from the scene name. This file: the diorama, the mesh batch, the primitives,
/// the sky; PosterKitShapes: masses, crowns, windows, arches, viaducts, stairs; PosterKitProps: beams, lamps,
/// figures, vehicles, smoke, floors, clocks, rain. The thirteen compositions: PosterActI / II / III.
/// </summary>
public sealed partial class PosterKit
{
    public const float HalfW = 8f, HalfH = 4.5f, SkyZ = 9f;
    public enum Blend { Opaque, Alpha, Add }

    public readonly Transform root;
    public readonly int layer;
    public readonly System.Random rng;
    public PosterLook.Palette pal;
    /// Atmospheric perspective: an opaque colour at depth z is mixed toward `haze` by hazeStrength × ((z − 1) / 7)^1.2
    public Color haze;
    public float hazeStrength = 0.72f;

    readonly List<Object> owned = new List<Object>();
    readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
    readonly Material opaque, alpha, additive;
    Texture2D skyTex;
    int order;

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor"), BaseMap = Shader.PropertyToID("_BaseMap"), BaseMapST = Shader.PropertyToID("_BaseMap_ST");

    public PosterKit(Transform root, int layer, int seed, PosterLook.Palette palette)
    {
        this.root = root;
        this.layer = layer;
        rng = new System.Random(seed);
        Use(palette);
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        opaque = new Material(shader) { name = "Poster/Opaque", hideFlags = HideFlags.HideAndDontSave };
        opaque.SetFloat("_Cull", (float)CullMode.Off);
        alpha = Transparent("Poster/Alpha", BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha);
        additive = Transparent("Poster/Add", BlendMode.SrcAlpha, BlendMode.One);
    }

    static Material Transparent(string name, BlendMode src, BlendMode dst)
    {
        var m = LevelKit.FromTemplate("UnlitTransparent", "Universal Render Pipeline/Unlit");
        m.name = name;
        m.hideFlags = HideFlags.HideAndDontSave;
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", dst == BlendMode.One ? 2f : 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)src);
        m.SetInt("_DstBlend", (int)dst);
        m.SetInt("_ZWrite", 0);
        m.SetInt("_Cull", (int)CullMode.Off);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        m.SetColor(BaseColor, Color.white);
        return m;
    }

    /// Everything this poster made: its meshes, its textures, its three materials. The GameObjects are the caller's
    public void Release()
    {
        foreach (var o in owned) if (o != null) Object.DestroyImmediate(o);
        owned.Clear();
        Object.DestroyImmediate(opaque);
        Object.DestroyImmediate(alpha);
        Object.DestroyImmediate(additive);
    }

    // ------------------------------------------------------------ colour

    public void Use(PosterLook.Palette palette)
    {
        pal = palette;
        haze = palette.haze;
    }

    /// The colour at depth z: mixed toward the haze the farther back it sits (the cut-out painter's aerial perspective)
    public Color Tone(Color c, float z)
    {
        float k = Mathf.Pow(Mathf.Clamp01((z - 1f) / 7f), 1.2f) * hazeStrength;
        return new Color(Mathf.Lerp(c.r, haze.r, k), Mathf.Lerp(c.g, haze.g, k), Mathf.Lerp(c.b, haze.b, k), c.a);
    }

    /// A light at depth z: dimmed toward the haze by half the strength a surface would be
    public Color ToneLit(Color c, float z)
    {
        float k = Mathf.Pow(Mathf.Clamp01((z - 1f) / 7f), 1.2f) * hazeStrength * 0.5f;
        return new Color(Mathf.Lerp(c.r, haze.r, k), Mathf.Lerp(c.g, haze.g, k), Mathf.Lerp(c.b, haze.b, k), c.a);
    }

    public static Color Glow(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);
    public static Color With(Color c, float a) => new Color(c.r, c.g, c.b, a);
    public static Color Mix(Color a, Color b, float k) => new Color(Mathf.Lerp(a.r, b.r, k), Mathf.Lerp(a.g, b.g, k), Mathf.Lerp(a.b, b.b, k), a.a);
    public float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);
    public int RandInt(int a, int b) => rng.Next(a, b);

    // ------------------------------------------------------------ the batch: many flat polygons into one mesh

    public sealed class Batch
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int> t = new List<int>();
        bool explicitUv, frameUv;

        public int Count => v.Count;
        /// UVs by the frame (x ±8 → u, y ±4.5 → v) instead of the batch's own bounds: a hole that shows the sky's gradient
        public void FrameUv() => frameUv = true;

        public void Poly(params Vector2[] p)
        {
            int b = v.Count;
            for (int i = 0; i < p.Length; i++) { v.Add(p[i]); uv.Add(Vector2.zero); }
            for (int i = 1; i < p.Length - 1; i++) { t.Add(b); t.Add(b + i); t.Add(b + i + 1); }
        }

        public void PolyUv(Vector2[] p, Vector2[] u)
        {
            explicitUv = true;
            int b = v.Count;
            for (int i = 0; i < p.Length; i++) { v.Add(p[i]); uv.Add(u[i]); }
            for (int i = 1; i < p.Length - 1; i++) { t.Add(b); t.Add(b + i); t.Add(b + i + 1); }
        }

        public void Rect(float x0, float y0, float x1, float y1) => Poly(new Vector2(x0, y0), new Vector2(x0, y1), new Vector2(x1, y1), new Vector2(x1, y0));
        public void Box(float cx, float cy, float w, float h) => Rect(cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f);
        public void Tri(float ax, float ay, float bx, float by, float cx, float cy) => Poly(new Vector2(ax, ay), new Vector2(bx, by), new Vector2(cx, cy));

        public void Line(float x0, float y0, float x1, float y1, float width)
        {
            var d = new Vector2(x1 - x0, y1 - y0);
            float len = d.magnitude;
            if (len < 1e-5f) return;
            var n = new Vector2(-d.y, d.x) / len * (width * 0.5f);
            var a = new Vector2(x0, y0);
            var b = new Vector2(x1, y1);
            Poly(a + n, b + n, b - n, a - n);
        }

        public void Disc(float cx, float cy, float r, int n = 32) => Ellipse(cx, cy, r, r, n);

        public void Ellipse(float cx, float cy, float rx, float ry, int n = 32)
        {
            var p = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                p[i] = new Vector2(cx + Mathf.Cos(a) * rx, cy + Mathf.Sin(a) * ry);
            }
            Poly(p);
        }

        /// The fan of an arc from a0 to a1 degrees (a half disc for 0..180), closed by its chord
        public void Sector(float cx, float cy, float rx, float ry, float a0, float a1, int n = 16)
        {
            var p = new Vector2[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float a = Mathf.Lerp(a0, a1, i / (float)n) * Mathf.Deg2Rad;
                p[i] = new Vector2(cx + Mathf.Cos(a) * rx, cy + Mathf.Sin(a) * ry);
            }
            Poly(p);
        }

        /// A ring (or an arc of one) of `thickness` inside the outer radii, from a0 to a1 degrees
        public void Ring(float cx, float cy, float rx, float ry, float thickness, float a0 = 0f, float a1 = 360f, int n = 48)
        {
            float irx = Mathf.Max(0f, rx - thickness), iry = Mathf.Max(0f, ry - thickness);
            for (int i = 0; i < n; i++)
            {
                float u0 = Mathf.Lerp(a0, a1, i / (float)n) * Mathf.Deg2Rad, u1 = Mathf.Lerp(a0, a1, (i + 1) / (float)n) * Mathf.Deg2Rad;
                Poly(new Vector2(cx + Mathf.Cos(u0) * rx, cy + Mathf.Sin(u0) * ry), new Vector2(cx + Mathf.Cos(u1) * rx, cy + Mathf.Sin(u1) * ry),
                     new Vector2(cx + Mathf.Cos(u1) * irx, cy + Mathf.Sin(u1) * iry), new Vector2(cx + Mathf.Cos(u0) * irx, cy + Mathf.Sin(u0) * iry));
            }
        }

        /// The wall left over a round-headed opening: the two spandrels between the arc (centre cx, springing sy,
        /// radii rx/ry) and the flat top yTop, each a fan from its outer top corner — a true hole, whatever is behind shows
        public void Spandrels(float cx, float sy, float rx, float ry, float yTop, int n = 12)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var p = new Vector2[n + 3];
                p[0] = new Vector2(cx + side * rx, yTop);
                p[1] = new Vector2(cx, yTop);
                for (int i = 0; i <= n; i++)
                {
                    float a = Mathf.Lerp(90f, side < 0 ? 180f : 0f, i / (float)n) * Mathf.Deg2Rad;
                    p[2 + i] = new Vector2(cx + Mathf.Cos(a) * rx, sy + Mathf.Sin(a) * ry);
                }
                Poly(p);
            }
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.HideAndDontSave };
            if (!explicitUv)
            {
                float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
                if (frameUv) { x0 = -HalfW; x1 = HalfW; y0 = -HalfH; y1 = HalfH; }
                else foreach (var p in v) { x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); y0 = Mathf.Min(y0, p.y); y1 = Mathf.Max(y1, p.y); }
                float w = Mathf.Max(1e-4f, x1 - x0), h = Mathf.Max(1e-4f, y1 - y0);
                for (int i = 0; i < v.Count; i++) uv[i] = new Vector2((v[i].x - x0) / w, (v[i].y - y0) / h);
            }
            mesh.SetVertices(v);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    // ------------------------------------------------------------ placing

    /// A mesh on the diorama at depth z with a flat colour (and a texture), a hair nearer than everything placed before it
    public GameObject Put(Mesh mesh, float z, Color c, Blend blend = Blend.Opaque, Texture tex = null, Vector4 tiling = default, string name = "Shape")
    {
        owned.Add(mesh);
        var go = new GameObject(name) { layer = layer };
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(0f, 0f, z - (++order) * 0.0004f);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = blend == Blend.Opaque ? opaque : blend == Blend.Alpha ? alpha : additive;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = LightProbeUsage.Off;
        mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
        block.Clear();
        block.SetColor(BaseColor, c);
        if (tex != null)
        {
            block.SetTexture(BaseMap, tex);
            block.SetVector(BaseMapST, tiling == default ? new Vector4(1f, 1f, 0f, 0f) : tiling);
        }
        mr.SetPropertyBlock(block);
        return go;
    }

    /// A one-polygon shape. Opaque shapes take the haze of their depth unless `lit`
    public GameObject Poly(Color c, float z, bool lit, params Vector2[] pts)
    {
        var b = new Batch();
        b.Poly(pts);
        return Put(b.Build("Poly"), z, lit ? c : Tone(c, z));
    }

    public GameObject Box(float x0, float y0, float x1, float y1, Color c, float z, bool lit = false, Blend blend = Blend.Opaque)
    {
        var b = new Batch();
        b.Rect(x0, y0, x1, y1);
        return Put(b.Build("Box"), z, blend == Blend.Opaque && !lit ? Tone(c, z) : c, blend);
    }

    public GameObject Disc(float x, float y, float r, Color c, float z, bool lit = false, Blend blend = Blend.Opaque)
    {
        var b = new Batch();
        b.Disc(x, y, r);
        return Put(b.Build("Disc"), z, blend == Blend.Opaque && !lit ? Tone(c, z) : c, blend);
    }

    public GameObject Line(float x0, float y0, float x1, float y1, float width, Color c, float z, bool lit = false, Blend blend = Blend.Opaque)
    {
        var b = new Batch();
        b.Line(x0, y0, x1, y1, width);
        return Put(b.Build("Line"), z, blend == Blend.Opaque && !lit ? Tone(c, z) : c, blend);
    }

    public GameObject Ring(float x, float y, float rx, float ry, float thickness, Color c, float z, float a0 = 0f, float a1 = 360f, bool lit = false)
    {
        var b = new Batch();
        b.Ring(x, y, rx, ry, thickness, a0, a1);
        return Put(b.Build("Ring"), z, lit ? c : Tone(c, z));
    }

    /// A textured quad (the soft things: halos, fog, grain, the vignette)
    public GameObject Tex(float x0, float y0, float x1, float y1, Texture tex, Color c, float z, Blend blend, Vector4 tiling = default)
    {
        var b = new Batch();
        b.Rect(x0, y0, x1, y1);
        return Put(b.Build("Tex"), z, c, blend, tex, tiling);
    }

    // ------------------------------------------------------------ the sky

    /// The whole frame, a vertical gradient (a 1 × 64 texture, bilinear); `power` < 1 keeps the glow to the horizon
    public void Sky(Color top, Color bottom, float power = 0.6f)
    {
        skyTex = PosterLook.Gradient(top, bottom, power);
        owned.Add(skyTex);
        Tex(-HalfW - 0.5f, -HalfH - 0.5f, HalfW + 0.5f, HalfH + 0.5f, skyTex, Color.white, SkyZ, Blend.Opaque);
    }

    public void Stars(int count, float yMin = -1f, float bright = 1f)
    {
        var faint = new Batch();
        var sharp = new Batch();
        for (int i = 0; i < count; i++)
        {
            float x = Rand(-HalfW, HalfW), y = Rand(yMin, HalfH), s = Rand(0.02f, 0.05f);
            if (rng.NextDouble() < 0.3) sharp.Box(x, y, s * 1.3f, s * 1.3f);
            else faint.Box(x, y, s, s);
        }
        var c = new Color(0.82f, 0.86f, 0.95f) * bright;
        Put(faint.Build("Stars"), SkyZ - 0.1f, With(c, 0.55f), Blend.Alpha);
        Put(sharp.Build("Stars"), SkyZ - 0.1f, c);
    }

    public void Moon(float x, float y, float r, Color c, float halo = 0.8f)
    {
        Tex(x - r * (1f + halo) * 2f, y - r * (1f + halo) * 2f, x + r * (1f + halo) * 2f, y + r * (1f + halo) * 2f, PosterLook.HaloTex, With(c, 0.3f), SkyZ - 0.14f, Blend.Add);
        Disc(x, y, r, c, SkyZ - 0.15f, true);
    }

    /// A flat cloud: a bar with rounded ends
    public void CloudBar(float x, float y, float w, float h, Color c, float z)
    {
        var b = new Batch();
        b.Rect(x - w * 0.5f, y - h * 0.5f, x + w * 0.5f, y + h * 0.5f);
        b.Ellipse(x - w * 0.5f, y, h * 0.6f, h * 0.5f);
        b.Ellipse(x + w * 0.5f, y, h * 0.6f, h * 0.5f);
        Put(b.Build("Cloud"), z, Tone(c, z));
    }

    /// A soft horizontal haze between layers, `h` tall about y
    public void FogBand(float y, float h, Color c, float alpha, float z)
        => Tex(-HalfW - 0.5f, y - h * 0.5f, HalfW + 0.5f, y + h * 0.5f, PosterLook.BandTex, With(c, alpha), z, Blend.Alpha);

    /// The floor plane: everything below y
    public void Ground(float y, Color c, float z) => Box(-HalfW - 0.5f, -HalfH - 0.5f, HalfW + 0.5f, y, c, z);
}
