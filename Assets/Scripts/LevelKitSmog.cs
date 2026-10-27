using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// SMOG AND SPARKLE (2026-10-08, after the first play: "visually needs to be
/// more sparkle, smog, atmosphere"). The air of Gotham 1939, drawn the house
/// way, on primitives and particles, no Lights:
///   SMOG      drifting sheets of soft noise lying in every open-sky room and
///             over the whole city at street, mid and roof height (SmogSheets:
///             one texture offset per layer per frame, the colour following the
///             scene's fog so day is sepia and night burnt indigo, a fade when
///             the camera is about to pass through a sheet).
///   SMOKE     plumes off the powerhouse stacks and the rooftop chimneys
///             (MenuCity.Stacks), the roofs' chimneys, the zeppelins' engines,
///             steam off the sublevels' grates and the plant's vents: particle
///             systems with a painted puff, two dozen particles each.
///   SPARKLE   glints — tiny bright cubes on the lamps' brass and the House's
///             crown, welded in a few groups that twinkle on a hash (Glints);
///             the city's window sheets breathing and crackling through their
///             shared materials (WindowTwinkle); motes thicker in the lamps'
///             light (LampMotes); lens flares on the House's lantern and the
///             plant's floods (the pipeline's data-driven flare, a handful per
///             scene); the lamps' light caught in the smog as a faint cone sheet.
///   FLECKS    paper and ash blowing at street level.
/// Everything that moves carries a behaviour or is a particle system, so the
/// weld leaves it; everything see-through sorts in the transparent queue; none
/// of it has a collider; none of it exists in a thumbnail.
/// </summary>
public static partial class LevelKit
{
    /// The smog sheets draw after the city's own see-through things (windows 2950, cloud decks 2980) and before the
    /// beams, the rain and the particles at 3000
    public const int QSmog = 2995;

    static Texture2D smogTex, puffTex;
    static Material glintMat, coneMat, fleckMat, steamPuffMat, smokePuffMat, exhaustMat;
    static LensFlareDataSRP flareData;
    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    // ---------------------------------------------------------------- textures

    /// Tiling soft noise: three octaves of wrapped value noise thresholded to patches, so a sheet of it is sheets of
    /// smog with clear air between, not a flat tint
    static Texture2D SmogTex()
    {
        if (smogTex != null) return smogTex;
        var tile = Resources.Load<Texture2D>("Smoke/SmogTile");   // REAL SMOKE (2026-10-23): Kenney's puffs tiled by Tools/MakeSmoke.ps1, when present
        if (tile != null) { smogTex = tile; return smogTex; }
        const int n = 256;
        smogTex = new Texture2D(n, n, TextureFormat.RGBA32, true, false) { name = "Smog", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.HideAndDontSave };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float f = Wrapped(x, y, n, 128, 501) * 0.55f + Wrapped(x, y, n, 64, 502) * 0.3f + Wrapped(x, y, n, 32, 503) * 0.15f;
                float a = Mathf.Clamp01((f - 0.36f) / 0.34f);
                a = a * a * (3f - 2f * a);
                px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        smogTex.SetPixels32(px);
        smogTex.Apply(true, true);
        return smogTex;
    }

    /// A puff: a soft round body, its edge eaten by noise, so a chain of them reads as smoke and not as balls
    static Texture2D PuffTex()
    {
        if (puffTex != null) return puffTex;
        var real = Resources.Load<Texture2D>("Smoke/Puff");   // one of Kenney's puffs, when present
        if (real != null) { puffTex = real; return puffTex; }
        const int n = 64;
        puffTex = new Texture2D(n, n, TextureFormat.RGBA32, true, false) { name = "Puff", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.HideAndDontSave };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float edge = 0.72f + 0.28f * Wrapped(x, y, n, 16, 504);
                float a = Mathf.Clamp01(1f - r / edge);
                a = a * a * (3f - 2f * a) * (0.8f + 0.2f * Wrapped(x, y, n, 8, 505));
                px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        puffTex.SetPixels32(px);
        puffTex.Apply(true, true);
        return puffTex;
    }

    // ---------------------------------------------------------------- materials

    /// One smog layer's material: unlit, alpha-blended, the noise sheet, its own so its offset can drift on its own
    static Material SmogMat()
    {
        // LIT SMOKE (2026-10-25, user: "upgrade the lights to be actual light beams"): the sheet takes the scene's light
        // (the template's toon twin when the look is on), so the searchlights, the lamps' spots and the floods fall on
        // it; its own colour, the fog's, rides in the emission (SmogSheets sets both as the fog moves)
        var m = FromTemplate("LitTransparent", "Universal Render Pipeline/Lit");
        bool premul = m.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON");   // the Lit template blends premultiplied, the toon twin straight
        m.name = "Smog";
        m.SetTexture("_BaseMap", SmogTex());
        m.SetColor("_BaseColor", new Color(SmokeAlbedo.r, SmokeAlbedo.g, SmokeAlbedo.b, 0.14f));
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", premul ? 1f : 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)(premul ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.SrcAlpha));
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.SetFloat("_Smoothness", 0f);
        m.SetFloat("_Metallic", 0f);
        m.SetColor("_EmissionColor", new Color(0.5f, 0.42f, 0.34f) * SmokeEmission);
        m.EnableKeyword("_EMISSION");
        m.renderQueue = QSmog;
        return m;
    }

    /// What the smoke is when light falls on it (its own colour is emission); how much of the old colour the emission keeps
    public static readonly Color SmokeAlbedo = new Color(0.38f, 0.35f, 0.32f);
    public const float SmokeEmission = 0.85f;

    /// A particle material with the painted puff on it
    static Material PuffParticle(Color c)
    {
        var m = Particle(c, false);
        m.SetTexture(BaseMapId, PuffTex());
        return m;
    }

    /// A glint: lamp cream, well over white, so the bloom takes it and nothing else about it is seen
    static Material GlintMat => glintMat ??= Unlit(Shader.Find("Universal Render Pipeline/Unlit"), new Color(3.4f, 3f, 2.3f));

    /// The light a lamp throws into the smog: a soft cone, alpha-blended toward cream (the beam sheet's fade along it)
    static Material ConeMat => coneMat ??= Cone(CityTextures.Beam());

    static Material Cone(Texture2D tex)
    {
        var m = Sheet(new Color(1.35f, 1.2f, 0.9f, 0.14f), tex, true);   // additive and over white: light, not a veil (2026-10-25)
        m.name = "LampCone";
        return m;
    }

    // ---------------------------------------------------------------- smog

    /// THE SMOG IN A ROOM with no ceiling: three sheets at a fifth, half and three quarters of the walls' height,
    /// each a grid of cells (the fog is worked out per vertex) in its own drift, a little lighter and warmer low down
    /// where the lamps light it. The colour follows the fog (sepia by day, soot in the plant, burnt indigo at night)
    public static SmogSheets Smog(Transform parent, Vector3 min, Vector3 max, bool unscaled = false)
    {
        if (!Atmosphere) return null;
        float w = max.x - min.x - 1f, d = max.z - min.z - 1f, h = max.y - min.y;
        if (w < 8f || d < 8f || h < 6f) return null;
        Vector3 c = (min + max) * 0.5f;
        float[] ys = { min.y + h * 0.2f, min.y + h * 0.46f, min.y + h * 0.76f };
        float[] alpha = { 0.18f, 0.13f, 0.1f };
        Vector2[] wind = { new Vector2(0.011f, 0.004f), new Vector2(-0.007f, 0.006f), new Vector2(0.005f, -0.009f) };
        Color[] mul = { new Color(1.15f, 1.08f, 1f), Color.white, new Color(0.9f, 0.92f, 1f) };
        Color[] bias = { new Color(0.05f, 0.035f, 0.02f), new Color(0.02f, 0.015f, 0.01f), Color.black };
        return Sheets(parent, "RoomSmog", c, w, d, ys, 18f, 26f, alpha, wind, mul, bias, null, unscaled, 3f);
    }

    /// THE SMOG OVER THE CITY: three sheets at street, mid and roof height over the whole plan (`radius` out from
    /// `origin`, `groundY` its street), cut into cells, none of them over `keep` (x0, x1, z0, z1: where a level
    /// stands; its rooms have their own). The far rings sit in it; the House rises out of it
    public static SmogSheets CitySmog(Transform parent, Vector3 origin, float groundY, float radius, Vector4? keep, bool unscaled)
    {
        if (!Atmosphere) return null;
        float[] ys = { groundY + 12f, groundY + 34f, groundY + 72f };
        float[] alpha = { 0.18f, 0.14f, 0.1f };
        Vector2[] wind = { new Vector2(0.0036f, 0.0012f), new Vector2(-0.0024f, 0.002f), new Vector2(0.0018f, -0.003f) };
        Color[] mul = { new Color(1.2f, 1.1f, 1f), new Color(1.05f, 1.02f, 1f), new Color(0.9f, 0.92f, 1f) };
        Color[] bias = { new Color(0.06f, 0.04f, 0.02f), new Color(0.03f, 0.02f, 0.012f), Color.black };
        return Sheets(parent, "CitySmog", new Vector3(origin.x, 0f, origin.z), radius * 2f, radius * 2f, ys, radius / 8f, 460f, alpha, wind, mul, bias, keep, unscaled, 8f);
    }

    /// The sheets themselves: one material and one welded grid per layer, under one SmogSheets
    static SmogSheets Sheets(Transform parent, string name, Vector3 centre, float sizeX, float sizeZ, float[] ys, float cell, float repeat,
        float[] alpha, Vector2[] wind, Color[] mul, Color[] bias, Vector4? keep, bool unscaled, float fadeBand)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var comp = go.AddComponent<SmogSheets>();
        int n = ys.Length;
        var mats = new Material[n];
        var rends = new Renderer[n][];
        int cx = Mathf.Max(1, Mathf.CeilToInt(sizeX / cell)), cz = Mathf.Max(1, Mathf.CeilToInt(sizeZ / cell));
        float cw = sizeX / cx, cd = sizeZ / cz;
        for (int i = 0; i < n; i++)
        {
            mats[i] = SmogMat();
            var sb = new SheetBaker();
            for (int ix = 0; ix < cx; ix++)
                for (int iz = 0; iz < cz; iz++)
                {
                    float x0 = centre.x - sizeX * 0.5f + cw * ix, z0 = centre.z - sizeZ * 0.5f + cd * iz;
                    if (keep.HasValue && x0 + cw > keep.Value.x && x0 < keep.Value.y && z0 + cd > keep.Value.z && z0 < keep.Value.w) continue;
                    sb.Flat(mats[i], new Vector3(x0 + cw * 0.5f, ys[i], z0 + cd * 0.5f), cw, cd,
                        new Vector2(x0 / repeat, z0 / repeat), new Vector2((x0 + cw) / repeat, (z0 + cd) / repeat));
                }
            rends[i] = sb.Flush(go.transform, name + "Layer").ToArray();
        }
        comp.Init(mats, rends, ys, wind, alpha, mul, bias, unscaled, fadeBand);
        comp.Reach(centre, keep.HasValue || sizeX > 1000f ? float.PositiveInfinity : Mathf.Max(sizeX, sizeZ) + 160f);
        return comp;
    }

    // ---------------------------------------------------------------- smoke

    /// A PLUME off a stack's mouth at `at`: coal smoke, a chain of puffs rising, growing and leaning down the wind,
    /// gone after a quarter of a minute. `scale` 1 is a powerhouse stack three metres across; a rooftop chimney is 0.4
    public static ParticleSystem Plume(Transform parent, Vector3 at, float scale, bool unscaled, Vector3? wind = null)
    {
        if (!Atmosphere) return null;
        var c = new Color(0.4f, 0.37f, 0.36f, 0.3f);
        smokePuffMat ??= PuffParticle(c);
        Vector3 w = wind ?? new Vector3(1.4f, 0f, 0.5f);
        var ps = Cloud(parent, "Plume", at, Vector3.one * (0.8f * scale), smokePuffMat, 1.6f, 14f, 2.2f * scale, 3.4f * scale, c,
            new Vector3(w.x, 1.6f + 0.8f * scale, w.z), -0.02f, 0.5f * scale, 10f);
        Shape(ps, unscaled, 0.5f * scale, 0.32f);
        return ps;
    }

    /// The exhaust off a zeppelin's engines: a thin trail left behind the gondola in world space, so it hangs where
    /// the ship was. `local` is the engine's place under the ship
    public static void Exhaust(Transform ship, Vector3 local, bool unscaled)
    {
        if (!Atmosphere) return;
        var c = new Color(0.5f, 0.47f, 0.45f, 0.22f);
        exhaustMat ??= PuffParticle(c);
        var ps = Cloud(ship, "Exhaust", ship.position, Vector3.one * 0.8f, exhaustMat, 2.5f, 8f, 1.4f, 2.2f, c, new Vector3(0f, -0.15f, 0f), -0.01f, 0.25f, 8f);
        ps.transform.localPosition = local;
        Shape(ps, unscaled, 0.3f, 0.3f);
        var main = ps.main;
        main.prewarm = false;
    }

    /// STEAM off a grate or a vent: a short chain of puffs, warm grey, a couple of metres tall, always going
    public static ParticleSystem SteamPuff(Transform parent, Vector3 at, float scale = 1f, bool unscaled = false)
    {
        if (!Atmosphere) return null;
        var c = new Color(0.82f, 0.79f, 0.75f, 0.28f);
        steamPuffMat ??= PuffParticle(c);
        var ps = Cloud(parent, "SteamPuff", at, new Vector3(0.6f, 0.1f, 0.6f) * scale, steamPuffMat, 2.4f, 2.8f, 0.5f * scale, 0.9f * scale, c,
            new Vector3(0.15f, 1.1f * scale, 0.1f), -0.04f, 0.4f, 25f);
        Shape(ps, unscaled, 0.3f, 0.35f);
        return ps;
    }

    /// What every puff chain shares: it grows along its life (from `grow` of its size), it spreads sideways, it runs
    /// on the clock it is told to
    static void Shape(ParticleSystem ps, bool unscaled, float spread, float grow)
    {
        var main = ps.main;
        main.useUnscaledTime = unscaled;
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, grow, 1f, 1f));
        var vel = ps.velocityOverLifetime;
        var x = vel.x; var z = vel.z;
        vel.x = new ParticleSystem.MinMaxCurve(x.constantMin - spread, x.constantMax + spread);
        vel.z = new ParticleSystem.MinMaxCurve(z.constantMin - spread, z.constantMax + spread);
    }

    /// Plumes on the stacks the plan recorded (MenuCity.Stacks: the powerhouses' and the roofs' chimneys), the `max`
    /// nearest `focus` within `range`; the big stacks always, the rooftop chimneys as the budget allows. Consumes the list.
    public static int Plumes(Transform parent, Vector3 focus, float range, int max, bool unscaled)
    {
        var tops = MenuCity.Stacks;
        var widths = MenuCity.StackWidths;
        if (!Atmosphere || tops == null || tops.Count == 0) { MenuCity.ClearStacks(); return 0; }
        var order = new List<int>();
        for (int i = 0; i < tops.Count; i++)
        {
            float dx = tops[i].x - focus.x, dz = tops[i].z - focus.z;
            if (dx * dx + dz * dz <= range * range) order.Add(i);
        }
        // The big stacks first (a powerhouse's are the smoke in a skyline), then by distance
        order.Sort((a, b) =>
        {
            bool bigA = widths[a] >= 2.5f, bigB = widths[b] >= 2.5f;
            if (bigA != bigB) return bigA ? -1 : 1;
            float da = (tops[a] - focus).sqrMagnitude, db = (tops[b] - focus).sqrMagnitude;
            return da.CompareTo(db);
        });
        int made = 0;
        Vector3 wind = new Vector3(1.4f, 0f, 0.5f);
        foreach (int i in order)
        {
            if (made >= max) break;
            float scale = Mathf.Clamp(widths[i] / 3f, 0.35f, 1.4f);
            Plume(parent, tops[i] + Vector3.up * 0.4f, scale, unscaled, wind);
            made++;
        }
        MenuCity.ClearStacks();
        return made;
    }

    // ---------------------------------------------------------------- flecks

    /// Paper and ash blowing through a room at street level: small sepia scraps tumbling on the wind
    public static void Flecks(Transform parent, Vector3 min, Vector3 max, float rate)
    {
        if (!Atmosphere) return;
        var c = new Color(0.72f, 0.62f, 0.48f, 0.85f);
        fleckMat ??= PuffParticle(c);
        Vector3 centre = (min + max) * 0.5f;
        float h = Mathf.Min(max.y - min.y, 9f);
        var ps = Cloud(parent, "Flecks", new Vector3(centre.x, min.y + h * 0.5f, centre.z), new Vector3(max.x - min.x - 2f, h, max.z - min.z - 2f), fleckMat,
            rate, 8f, 0.06f, 0.14f, c, new Vector3(1.1f, -0.1f, 0.4f), 0.004f, 1.3f, 240f);
        var vel = ps.velocityOverLifetime;
        vel.x = new ParticleSystem.MinMaxCurve(0.5f, 1.8f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.5f, 0.6f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.5f, 1f);
    }

    /// The same, riding with the camera: the open levels, where there is no room to fill
    public static void FlecksAround(Transform parent)
    {
        if (!Atmosphere) return;
        var c = new Color(0.72f, 0.62f, 0.48f, 0.85f);
        fleckMat ??= PuffParticle(c);
        var ps = Cloud(parent, "Flecks", Vector3.zero, new Vector3(48f, 14f, 48f), fleckMat, 12f, 7f, 0.07f, 0.15f, c, new Vector3(1.2f, -0.1f, 0.4f), 0.004f, 1.3f, 240f);
        var vel = ps.velocityOverLifetime;
        vel.x = new ParticleSystem.MinMaxCurve(0.6f, 2f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.5f, 0.6f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.5f, 1f);
        var main = ps.main;
        main.prewarm = false;
        ps.gameObject.AddComponent<FleckWind>().Init(ps);
    }

    // ---------------------------------------------------------------- the lamps

    /// The lamps in a box: a torchiere is a wide spot thrown up, a pendant a small point. `lamps` gets the lamp's
    /// place; `up` is true for the torchieres. At most `max` of them
    public static int Lamps(Transform under, Vector3 min, Vector3 max, List<Vector3> lamps, List<bool> up, int most)
    {
        if (under == null) return 0;
        int found = 0;
        foreach (var l in under.GetComponentsInChildren<Light>())
        {
            Vector3 p = l.transform.position;
            if (p.x < min.x - 0.5f || p.x > max.x + 0.5f || p.y < min.y || p.y > max.y || p.z < min.z - 0.5f || p.z > max.z + 0.5f) continue;
            if (!Torchiere(l) && !Pendant(l)) continue;
            lamps.Add(p);
            up.Add(Torchiere(l));
            if (++found >= most) break;
        }
        return found;
    }

    /// The kit's lamps, told from every other light in a level (a unit's, a shot's, a boss's): LevelKit.Light names
    /// its object "Light"; a torchiere is a wide spot thrown up (LevelKitDetail), a pendant a point of exactly eleven
    /// metres, a floodlight the one spot that casts a shadow (LevelKitWorks)
    static bool Torchiere(Light l) => l.name == "Light" && l.type == LightType.Spot && l.spotAngle >= 100f;
    static bool Pendant(Light l) => l.name == "Light" && l.type == LightType.Point && Mathf.Abs(l.range - 11f) < 0.01f;
    static bool Flood(Light l) => l.name == "Light" && l.type == LightType.Spot && l.spotAngle < 100f && l.shadows != LightShadows.None;

    /// Motes thicker in the lamps' light: the room's own mote cloud gets a LampMotes that seeds it round each lamp
    static void MotesAtLamps(ParticleSystem motes, Transform under, Vector3 min, Vector3 max)
    {
        if (motes == null) return;
        var lamps = new List<Vector3>();
        var up = new List<bool>();
        if (Lamps(under, min, max, lamps, up, 12) == 0) return;
        var pts = new Vector3[lamps.Count];
        for (int i = 0; i < pts.Length; i++) pts[i] = lamps[i] + Vector3.up * (up[i] ? 0.9f : -0.3f);
        var main = motes.main;
        main.maxParticles = main.maxParticles + pts.Length * 12;
        motes.gameObject.AddComponent<LampMotes>().Init(motes, pts);
    }

    /// A cone of light into a sheet baker: two crossed quads from `foot` along `dir`, `w0` wide at the lamp and `w1`
    /// at the far end, the beam texture bright at the lamp and gone at the end
    static void ConeQuads(SheetBaker sb, Material mat, Vector3 foot, Vector3 dir, float length, float w0, float w1)
    {
        Vector3 side = Vector3.Cross(dir, Mathf.Abs(dir.y) > 0.9f ? Vector3.forward : Vector3.up).normalized;
        Vector3 other = Vector3.Cross(dir, side).normalized;
        Vector3 top = foot + dir * length;
        for (int q = 0; q < 2; q++)
        {
            Vector3 s = q == 0 ? side : other;
            sb.Quad(mat, foot - s * w0, top - s * w1, top + s * w1, foot + s * w0, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f));
        }
    }

    // ---------------------------------------------------------------- glints

    /// A set of glints: tiny bright cubes welded into `groups` meshes under one Glints, which twinkles each group on
    /// its own hash. Add() takes a point and a size; Flush() welds them
    public class GlintSet
    {
        readonly MeshBaker[] bakers;
        int n;
        public GlintSet(int groups = 6) { bakers = new MeshBaker[groups]; for (int i = 0; i < groups; i++) bakers[i] = new MeshBaker(); }
        public void Add(Vector3 at, float size) { bakers[n++ % bakers.Length].Add(GlintMat, at, Vector3.one * size); }
        public int Count => n;

        public Glints Flush(Transform parent, string name)
        {
            if (n == 0) return null;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var glints = go.AddComponent<Glints>();
            var rends = new List<Renderer>();
            foreach (var b in bakers) rends.AddRange(b.Flush(go.transform, name));
            glints.Init(rends, GlintMat.GetColor("_BaseColor"));
            return glints;
        }
    }

    // ---------------------------------------------------------------- flares

    /// A lens flare on a lamp: the pipeline's data-driven flare (a soft warm disc, a hot core, brass ghosts down the
    /// axis), occluded by what stands in front of it. A handful per scene, never one per lamp
    public static void Flare(Transform parent, Vector3 at, float scale, float intensity, float far, float occlusionRadius)
    {
        if (!Atmosphere) return;
        var go = new GameObject("Flare");
        go.transform.SetParent(parent, false);
        go.transform.position = at;
        var f = go.AddComponent<LensFlareComponentSRP>();
        f.lensFlareData = FlareData();
        f.intensity = intensity;
        f.scale = scale;
        f.maxAttenuationDistance = far;
        f.distanceAttenuationCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.35f);
        f.maxAttenuationScale = far;
        f.scaleByDistanceCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.55f);
        f.attenuationByLightShape = false;
        f.useOcclusion = true;
        f.occlusionRadius = occlusionRadius;
        f.sampleCount = 16;
        f.occlusionOffset = occlusionRadius * 0.5f;
        f.allowOffScreen = false;
    }

    static LensFlareDataSRP FlareData()
    {
        if (flareData != null) return flareData;
        flareData = ScriptableObject.CreateInstance<LensFlareDataSRP>();
        flareData.name = "LampFlare";
        flareData.hideFlags = HideFlags.HideAndDontSave;
        flareData.elements = new[]
        {
            FlareElement(0f, 0.32f, 0.8f, new Color(1f, 0.86f, 0.62f), 0.95f),     // the glow on the lamp
            FlareElement(0f, 0.1f, 1.3f, new Color(1f, 0.95f, 0.85f), 0.85f),      // its hot core
            FlareElement(0.45f, 0.045f, 0.22f, new Color(0.95f, 0.72f, 0.4f), 0.5f), // brass ghosts down the axis
            FlareElement(0.9f, 0.08f, 0.16f, new Color(0.9f, 0.6f, 0.35f), 0.4f),
            FlareElement(1.45f, 0.03f, 0.2f, new Color(1f, 0.85f, 0.6f), 0.6f),
            FlareElement(-0.55f, 0.06f, 0.14f, new Color(0.9f, 0.7f, 0.45f), 0.4f),
        };
        return flareData;
    }

    static LensFlareDataElementSRP FlareElement(float pos, float size, float intensity, Color tint, float fallOff)
    {
        var e = new LensFlareDataElementSRP
        {
            flareType = SRPLensFlareType.Circle,
            position = pos,
            uniformScale = size,
            sizeXY = Vector2.one,
            tint = tint,
            blendMode = SRPLensFlareBlendMode.Additive,
            modulateByLightColor = false,
            autoRotate = false,
        };
        e.localIntensity = intensity;
        e.fallOff = fallOff;
        e.edgeOffset = 0.1f;
        return e;
    }

    // ---------------------------------------------------------------- the pass over a built level

    /// SPARKLE, after a level is built and lit (WorldLook.Apply): every torchiere gets a glint on its bowl's brass
    /// and a cone of its light caught in the smog, every pendant a glint on its cap and a soft fall of light under
    /// the globe, the plant's floods a beam each and a flare on the first few; the House's lantern a flare; the
    /// roofs' chimneys (LevelKit.Chimney, which never smoked) a plume; the open levels paper blowing round the camera
    public static void Sparkle(LevelKind kind, Zone primary, bool night)
    {
        if (!Atmosphere) return;
        var root = new GameObject("Sparkle").transform;
        var glints = new GlintSet(8);
        var cones = new SheetBaker();
        Material cone = Cone(CityTextures.Beam());
        var cc = cone.GetColor("_BaseColor");
        cc.a = night ? 0.09f : 0.06f;
        cone.SetColor("_BaseColor", cc);
        int floods = 0, lamps = 0;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (!l.enabled || l.type == LightType.Directional) continue;
            Vector3 p = l.transform.position;
            if (Torchiere(l))
            {
                // A torchiere: the bowl under the light, its brass rim toward the room; the light goes up
                Vector3 inward = l.transform.up; inward.y = 0f;
                if (inward.sqrMagnitude > 0.01f) inward.Normalize();
                glints.Add(p + Vector3.down * 0.2f + inward * 0.06f, 0.05f);
                ConeQuads(cones, cone, p + Vector3.down * 0.12f, Vector3.up, 2.4f, 0.16f, 0.9f);
                lamps++;
            }
            else if (Pendant(l))
            {
                // A pendant: the globe is over the light, the brass cap over the globe
                glints.Add(p + Vector3.up * 0.68f, 0.05f);
                ConeQuads(cones, cone, p + Vector3.up * 0.1f, Vector3.down, 2f, 0.3f, 1.1f);
                lamps++;
            }
            else if (Flood(l))
            {
                // A floodlight: its beam in the plant's smoke, a glint on its brass rim, a flare on the first few
                Vector3 dir = l.transform.forward;
                float len = Mathf.Min(l.range * 0.6f, 18f);
                ConeQuads(cones, cone, p + dir * 0.2f, dir, len, 0.3f, len * Mathf.Tan(l.spotAngle * 0.5f * Mathf.Deg2Rad) * 0.7f);
                glints.Add(p - dir * 0.3f + Vector3.up * 0.3f, 0.06f);
                if (floods++ < 4) Flare(root, p - dir * 0.2f, 0.45f, 0.5f, 120f, 0.5f);
            }
        }
        cones.Flush(root, "LampCones");
        glints.Flush(root, "LampGlints");
        // The House's lantern, wherever the city stands
        if (CityKit.LanternAt.HasValue) Flare(root, CityKit.LanternAt.Value, 0.9f, 0.7f, 14000f, 2.2f * CityKit.LanternScale);
        // The roofs' chimneys never smoked: a plume off each cap
        int plumes = 0;
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (r.name != "StackCap" || plumes >= 40) continue;
            Vector3 top = r.transform.position + Vector3.up * (r.transform.localScale.y + 0.3f);
            Plume(root, top, Mathf.Clamp(r.transform.localScale.x / 3f, 0.35f, 1.2f), false);
            plumes++;
        }
        if (primary == Zone.Surface)
        {
            // The open city: paper round the camera, and the smog over the route and the blocks about it (the plan's
            // own smog stands round the House, kilometres off)
            FlecksAround(root);
            CitySmog(root, Vector3.zero, 0f, 2600f, null, false);
        }
        Debug.Log("SPARKLE  " + lamps + " lamps dressed, " + glints.Count + " glints, " + floods + " floods, " + plumes + " roof plumes");
    }
}

/// <summary>
/// The smog's drift: each layer's noise slides on its own wind (one texture offset per layer per frame), the colour
/// follows the scene's fog (the fog is what the day or the night is painted in: sepia, soot, burnt indigo), the
/// low layers a little lighter and warmer where the lamps reach them, and a layer the camera is about to pass
/// through fades out over `fadeBand` metres so a sheet never flips over the lens.
/// </summary>
public class SmogSheets : MonoBehaviour
{
    /// The knobs: the drift's speed, the whole thing's opacity
    public float speed = 1f, alpha = 1f;
    public bool followFog = true;

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor"), BaseMap = Shader.PropertyToID("_BaseMap"), EmissionColor = Shader.PropertyToID("_EmissionColor");
    static Camera cam; static int camFrame = -1;

    Material[] mats; Renderer[][] rends;
    float[] ys, baseAlpha; Vector2[] wind; Color[] mul, bias;
    bool[] faded; bool unscaled; float fadeBand;
    Color fogSeen = new Color(-1f, 0f, 0f, 0f), fixedTint; float alphaSeen = -1f;
    MaterialPropertyBlock block;

    public void Init(Material[] mats, Renderer[][] rends, float[] ys, Vector2[] wind, float[] alpha, Color[] mul, Color[] bias, bool unscaled, float fadeBand)
    {
        this.mats = mats; this.rends = rends; this.ys = ys; this.wind = wind; baseAlpha = alpha; this.mul = mul; this.bias = bias;
        this.unscaled = unscaled; this.fadeBand = fadeBand;
        faded = new bool[mats.Length];
        block = new MaterialPropertyBlock();
    }

    /// A colour of its own instead of the fog's (the menu, whose fog turns with the camera)
    public void Tint(Color c) { followFog = false; fixedTint = c; fogSeen = new Color(-1f, 0f, 0f, 0f); }

    Vector3 centre; float reachSq = float.PositiveInfinity;

    /// A room's smog only drifts while the camera is within `radius` of `at`: the rest of the level's rooms cost nothing
    public void Reach(Vector3 at, float radius) { centre = at; reachSq = radius * radius; }

    Color Colour(int i, Color fog)
    {
        float lum = fog.r * 0.4f + fog.g * 0.5f + fog.b * 0.1f;
        float dark = 1f - Mathf.Clamp01(lum * 2.2f);   // the lamps only show on smog the day does not light
        return new Color(fog.r * mul[i].r + bias[i].r * dark, fog.g * mul[i].g + bias[i].g * dark, fog.b * mul[i].b + bias[i].b * dark, baseAlpha[i] * alpha);
    }

    void Update()
    {
        if (mats == null) return;
        if (camFrame != Time.frameCount) { cam = Camera.main; camFrame = Time.frameCount; }
        if (cam != null && reachSq < float.PositiveInfinity)
        {
            Vector3 d = cam.transform.position - centre; d.y = 0f;
            if (d.sqrMagnitude > reachSq) return;
        }
        float t = unscaled ? Time.unscaledTime : Time.time;
        for (int i = 0; i < mats.Length; i++)
            if (mats[i] != null) mats[i].SetTextureOffset(BaseMap, new Vector2(Mathf.Repeat(t * wind[i].x * speed, 1f), Mathf.Repeat(t * wind[i].y * speed, 1f)));
        // The colour: the fog's, with each layer's cast, set only when it moves
        Color fog = followFog ? RenderSettings.fogColor : fixedTint;
        if (Mathf.Abs(fog.r - fogSeen.r) + Mathf.Abs(fog.g - fogSeen.g) + Mathf.Abs(fog.b - fogSeen.b) > 0.004f || alphaSeen != alpha)
        {
            fogSeen = fog; alphaSeen = alpha;
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] != null)
                {
                    var c = Colour(i, fog);   // the smoke's own colour is its emission; the albedo is what the lights find
                    mats[i].SetColor(BaseColor, new Color(LevelKit.SmokeAlbedo.r, LevelKit.SmokeAlbedo.g, LevelKit.SmokeAlbedo.b, c.a));
                    mats[i].SetColor(EmissionColor, new Color(c.r, c.g, c.b) * LevelKit.SmokeEmission);
                }
        }
        // The fade when the lens is near a sheet's height
        if (cam == null) return;
        float cy = cam.transform.position.y;
        for (int i = 0; i < mats.Length; i++)
        {
            float d = Mathf.Abs(cy - ys[i]);
            bool near = d < fadeBand;
            if (!near && !faded[i]) continue;
            if (near)
            {
                float k = Mathf.Clamp01(d / fadeBand);
                Color c = Colour(i, fog);
                c.a *= k * k;
                block.SetColor(BaseColor, new Color(LevelKit.SmokeAlbedo.r, LevelKit.SmokeAlbedo.g, LevelKit.SmokeAlbedo.b, c.a));
                foreach (var r in rends[i]) if (r != null) r.SetPropertyBlock(block);
            }
            else foreach (var r in rends[i]) if (r != null) r.SetPropertyBlock(null);
            faded[i] = near;
        }
    }
}

/// <summary>
/// The city's windows breathing: each lit-sheet material (one per window kind, shared by every quad of that kind)
/// swells and dims a few percent on two slow sines and crackles a little on a hashed step, so through the bloom the
/// whole skyline shimmers without one quad being touched. One SetColor per material per frame.
/// </summary>
public class WindowTwinkle : MonoBehaviour
{
    /// The knobs: how far the light swings (0.12 = ±12 %), and how fast
    public float depth = 0.12f, speed = 1f;

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    Material[] mats; Color[] baseCol; float[] phase, rate;
    bool unscaled;

    public void Init(List<Material> materials, bool unscaled)
    {
        this.unscaled = unscaled;
        mats = materials.ToArray();
        baseCol = new Color[mats.Length]; phase = new float[mats.Length]; rate = new float[mats.Length];
        for (int i = 0; i < mats.Length; i++)
        {
            baseCol[i] = mats[i].GetColor(BaseColor);
            phase[i] = i * 1.37f;
            rate[i] = 0.09f + 0.035f * (i % 4);
        }
    }

    void Update()
    {
        if (mats == null) return;
        float t = (unscaled ? Time.unscaledTime : Time.time) * speed;
        for (int i = 0; i < mats.Length; i++)
        {
            if (mats[i] == null) continue;
            float s = Mathf.Sin(t * rate[i] * 6.2832f + phase[i]) * 0.6f + Mathf.Sin(t * rate[i] * 2.7f + phase[i] * 1.7f) * 0.4f;
            int step = (int)(t * 6.7f) + i * 97;
            float crackle = (MenuArt.Hash01(step, i, 311) - 0.5f) * 0.5f;
            float k = 1f + depth * (s + crackle);
            mats[i].SetColor(BaseColor, new Color(baseCol[i].r * k, baseCol[i].g * k, baseCol[i].b * k, baseCol[i].a));
        }
    }

    void OnDestroy()
    {
        if (mats == null) return;
        for (int i = 0; i < mats.Length; i++) if (mats[i] != null) mats[i].SetColor(BaseColor, baseCol[i]);
    }
}

/// <summary>
/// Glints twinkling: a few welded groups of tiny bright cubes, each group's brightness a smooth walk between hashed
/// values (mostly dim, now and then a flash), set through one property block per group per frame.
/// </summary>
public class Glints : MonoBehaviour
{
    /// The knobs: how bright the flashes get, how often
    public float brightness = 1f, rate = 1f;

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    Renderer[] groups; float[] phase; Color baseColor; MaterialPropertyBlock block;

    public void Init(List<Renderer> rends, Color colour)
    {
        groups = rends.ToArray();
        phase = new float[groups.Length];
        for (int i = 0; i < phase.Length; i++) phase[i] = i * 0.731f;
        baseColor = colour;
        block = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (groups == null) return;
        float t = Time.time * rate;
        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] == null) continue;
            float u = t * (0.9f + 0.17f * i) * 2.5f + phase[i];
            int step = Mathf.FloorToInt(u);
            float f = u - step;
            f = f * f * (3f - 2f * f);
            float v = Mathf.Lerp(MenuArt.Hash01(step, i, 77), MenuArt.Hash01(step + 1, i, 77), f);
            float k = (0.2f + 0.8f * v * v * v) * brightness;
            block.SetColor(BaseColor, new Color(baseColor.r * k, baseColor.g * k, baseColor.b * k, 1f));
            groups[i].SetPropertyBlock(block);
        }
    }
}

/// <summary>
/// Motes seeded in the lamps' light: a room's mote cloud is told where its lamps are and drops a mote or two a second
/// round each, so the air is thickest where it is lit, as dust is.
/// </summary>
public class LampMotes : MonoBehaviour
{
    /// The knob: motes per second per lamp
    public float rate = 1.2f;

    ParticleSystem ps; Vector3[] lamps; float acc;
    ParticleSystem.EmitParams ep;

    public void Init(ParticleSystem system, Vector3[] at) { ps = system; lamps = at; ep = new ParticleSystem.EmitParams(); }

    void Update()
    {
        if (ps == null || lamps == null || lamps.Length == 0) return;
        acc += Time.deltaTime * rate * lamps.Length;
        int n = (int)acc;
        if (n <= 0) return;
        acc -= n;
        if (n > 6) n = 6;
        for (int k = 0; k < n; k++)
        {
            ep.position = lamps[Random.Range(0, lamps.Length)] + Random.insideUnitSphere * 1.1f;
            ep.velocity = new Vector3(Random.Range(-0.07f, 0.07f), Random.Range(0.01f, 0.07f), Random.Range(-0.07f, 0.07f));
            ep.startLifetime = Random.Range(6f, 10f);
            ep.startSize = Random.Range(0.035f, 0.08f);
            ps.Emit(ep, 1);
        }
    }
}

/// <summary>
/// Paper on the wind, round the camera: the emitter box rides a few metres ahead of the lens; what it has emitted
/// stays where it was blown (world space).
/// </summary>
public class FleckWind : MonoBehaviour
{
    ParticleSystem ps;
    public void Init(ParticleSystem system) => ps = system;

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null || ps == null) return;
        Vector3 f = cam.transform.forward; f.y = 0f;
        ps.transform.position = cam.transform.position + (f.sqrMagnitude > 0.01f ? f.normalized : Vector3.forward) * 12f + Vector3.up * 2f;
    }
}
