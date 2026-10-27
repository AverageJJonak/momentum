using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// AIR IN THE ROOMS. The one thing a finished box has none of. Each section
/// gets at most one particle system sized to it — dust motes hanging in the
/// civic halls, dried blossom drifting down in the yards, ash lit amber under
/// the city, dust and the odd spark in the Works, drips off the pump hall's
/// ducts — a fog bank lying on the Underbelly's floors, and shafts of light
/// hanging under the headquarters' skylights. Dozens of particles per room, one
/// draw each, none of it geometry with a collider, none of it during a thumbnail.
/// </summary>
public static partial class LevelKit
{
    static Material moteMat, sporeMat, dustMat, petalMat, emberMat, dripMat, bankMat, shaftMat;
    static Texture2D shaftTex;

    /// Nothing in the air while a postcard is being shot
    static bool Atmosphere => !CityVista.Preview;

    /// A particle material: the particles shader when it is there (per-particle fade), the unlit one when it is not
    static Material Particle(Color c, bool additive)
    {
        // The keyword set comes from the template, so the build keeps the variant this asks for (see LevelKit.FromTemplate)
        var t = Template("ParticlesTransparent") ?? Template("UnlitTransparent");
        var m = t != null
            ? new Material(t)
            : new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetColor("_BaseColor", c);
        m.SetTexture("_BaseMap", MenuArt.Glow(32));
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", additive ? 2f : 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)(additive ? BlendMode.One : BlendMode.SrcAlpha));
        m.SetInt("_DstBlend", (int)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
        m.SetInt("_ZWrite", 0);
        m.SetInt("_Cull", (int)CullMode.Off);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        return m;
    }

    /// A see-through unlit sheet: fog banks and light shafts
    static Material Sheet(Color c, Texture2D tex, bool additive)
    {
        var m = FromTemplate("UnlitTransparent", "Universal Render Pipeline/Unlit");
        m.SetColor("_BaseColor", c);
        if (tex != null) m.SetTexture("_BaseMap", tex);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", additive ? 2f : 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)(additive ? BlendMode.One : BlendMode.SrcAlpha));
        m.SetInt("_DstBlend", (int)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
        m.SetInt("_ZWrite", 0);
        m.SetInt("_Cull", (int)CullMode.Off);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        return m;
    }

    /// A vertical fade: bright at the drawn top, gone at the bottom
    static Texture2D ShaftTex()
    {
        if (shaftTex != null) return shaftTex;
        const int h = 64;
        shaftTex = new Texture2D(4, h, TextureFormat.RGBA32, false) { name = "Shaft", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
        var px = new Color32[4 * h];
        for (int y = 0; y < h; y++)
        {
            float t = y / (float)(h - 1);
            byte a = (byte)Mathf.RoundToInt(t * t * 255f);
            for (int x = 0; x < 4; x++) px[y * 4 + x] = new Color32(255, 255, 255, a);
        }
        shaftTex.SetPixels32(px);
        shaftTex.Apply(false, true);
        return shaftTex;
    }

    /// One particle system filling a box, world-space, with a soft fade in and out over each particle's life
    static ParticleSystem Cloud(Transform parent, string name, Vector3 centre, Vector3 box, Material mat, float rate, float life,
        float sizeMin, float sizeMax, Color color, Vector3 drift, float gravity, float turbulence, float spin = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = centre;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = life;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startColor = color;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
        main.maxParticles = Mathf.CeilToInt(rate * life) + 8;
        main.gravityModifier = gravity;
        main.loop = true;
        main.prewarm = true;
        var emission = ps.emission;
        emission.rateOverTime = rate;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = box;
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(drift.x - 0.08f, drift.x + 0.08f);
        vel.y = new ParticleSystem.MinMaxCurve(drift.y - 0.05f, drift.y + 0.05f);
        vel.z = new ParticleSystem.MinMaxCurve(drift.z - 0.08f, drift.z + 0.08f);
        if (turbulence > 0f)
        {
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = turbulence;
            noise.frequency = 0.25f;
            noise.scrollSpeed = 0.15f;
            noise.quality = ParticleSystemNoiseQuality.Low;
        }
        if (spin != 0f)
        {
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-spin * Mathf.Deg2Rad, spin * Mathf.Deg2Rad);
        }
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        ps.Play();
        return ps;
    }

    /// The air a room gets, by zone: motes (thicker in the lamps' light), blossom, ash over a fog bank with steam
    /// off the grates, dust and steam in the plant; under an open sky the smog lying in the room and paper on the
    /// wind. Small rooms get none.
    public static void Air(Transform parent, Zone zone, Vector3 min, Vector3 max, bool openSky)
    {
        if (!Atmosphere) return;
        Vector3 size = max - min;
        if (size.x * size.y * size.z < 400f || size.y < 4f) return;
        Vector3 centre = (min + max) * 0.5f;
        Vector3 box = new Vector3(size.x - 1f, size.y - 1f, size.z - 1f);
        float len = size.z;
        if (openSky)
        {
            // THE SMOG: three drifting sheets in the room, the fog's own colour; and the street's paper blowing through
            Smog(parent, min, max);
            Flecks(parent, min, max, Mathf.Clamp(len / 6f, 4f, 14f));
        }
        switch (zone)
        {
            case Zone.Utopia:
                moteMat ??= Particle(new Color(1f, 0.97f, 0.9f, 0.35f), false);
                MotesAtLamps(Cloud(parent, "Motes", centre, box, moteMat, Mathf.Clamp(len / 9f, 2.5f, 10f), 12f, 0.04f, 0.09f, new Color(1f, 0.97f, 0.9f, 0.35f), new Vector3(0.15f, 0.03f, 0f), 0f, 0.25f), parent, min, max);
                if (openSky && size.x >= 40f)
                {
                    // The yard's blossom, dried to sepia, drifting down off the trees
                    petalMat ??= Particle(new Color(0.72f, 0.58f, 0.42f, 0.9f), false);
                    Cloud(parent, "Petals", new Vector3(centre.x, min.y + 7f, centre.z), new Vector3(size.x - 6f, 2f, size.z - 6f), petalMat,
                        4f, 9f, 0.14f, 0.22f, new Color(0.72f, 0.58f, 0.42f, 0.9f), new Vector3(0.4f, -0.7f, 0f), 0f, 0.3f, 90f);
                }
                break;
            case Zone.Underbelly:
                // Ash in the air, lit by the amber lamps (the spores were violet: nothing under the city is now)
                sporeMat ??= Particle(new Color(0.6f, 0.42f, 0.25f, 0.4f), false);
                MotesAtLamps(Cloud(parent, "Ash", centre, box, sporeMat, 6f, 9f, 0.06f, 0.12f, new Color(0.6f, 0.42f, 0.25f, 0.4f), new Vector3(0.05f, 0.08f, 0f), 0f, 0.35f), parent, min, max);
                // A bank of haze lying on the floor: one sheet, no collider
                bankMat ??= Sheet(new Color(0.22f, 0.1f, 0.05f, 0.16f), null, false);
                Prim(parent, new Vector3(centre.x, min.y + 0.5f, centre.z), new Vector3(size.x - 0.5f, 0.02f, size.z - 0.5f), bankMat, "FogBank");
                // Steam off the grates along the walls' feet, a chain of puffs each
                for (int i = 0; i < 2; i++)
                    SteamPuff(parent, new Vector3(i == 0 ? min.x + 1.4f : max.x - 1.4f, min.y + 0.25f, Mathf.Lerp(min.z, max.z, i == 0 ? 0.32f : 0.68f)), 0.9f);
                break;
            case Zone.Office:
                if (size.x < 16f || size.y < 8f) return; // the headquarters is clean: only its big rooms have anything in the air
                moteMat ??= Particle(new Color(1f, 0.97f, 0.9f, 0.35f), false);
                MotesAtLamps(Cloud(parent, "Motes", centre, box, moteMat, 2.5f, 12f, 0.04f, 0.08f, new Color(1f, 0.95f, 0.85f, 0.3f), new Vector3(0.1f, 0.02f, 0f), 0f, 0.2f), parent, min, max);
                break;
            case Zone.Works:
                dustMat ??= Particle(new Color(0.85f, 0.75f, 0.6f, 0.22f), false);
                MotesAtLamps(Cloud(parent, "Dust", centre, box, dustMat, 6f, 10f, 0.12f, 0.24f, new Color(0.85f, 0.75f, 0.6f, 0.22f), new Vector3(0.1f, 0f, 0.05f), 0.003f, 0.2f), parent, min, max);
                // The plant's vents: steam off a grate at each wall's foot
                for (int i = 0; i < 2; i++)
                    SteamPuff(parent, new Vector3(i == 0 ? min.x + 1.6f : max.x - 1.6f, min.y + 0.25f, Mathf.Lerp(min.z, max.z, i == 0 ? 0.6f : 0.25f)), 1.2f);
                break;
            case Zone.Brutal:
                // The concrete city: grey dust hanging in the shafts of light, drifting slowly with the wind
                dustMat ??= Particle(new Color(0.85f, 0.75f, 0.6f, 0.22f), false);
                MotesAtLamps(Cloud(parent, "Dust", centre, box, dustMat, Mathf.Clamp(len / 12f, 2f, 7f), 11f, 0.1f, 0.2f, new Color(0.82f, 0.83f, 0.86f, 0.16f), new Vector3(0.25f, 0f, 0.08f), 0f, 0.2f), parent, min, max);
                break;
            case Zone.Surface:
                // The open city: a little dust in the air, the lamps' motes where there are lamps
                moteMat ??= Particle(new Color(1f, 0.97f, 0.9f, 0.35f), false);
                MotesAtLamps(Cloud(parent, "Motes", centre, box, moteMat, Mathf.Clamp(len / 14f, 2f, 6f), 12f, 0.04f, 0.09f, new Color(0.95f, 0.88f, 0.78f, 0.3f), new Vector3(0.3f, 0.02f, 0.1f), 0f, 0.3f), parent, min, max);
                break;
        }
    }

    /// Sparks off a machine, thrown down and out and falling: the furnaces' embers, thick
    public static void Embers(Transform parent, Vector3 at)
    {
        if (!Atmosphere) return;
        emberMat ??= Particle(new Color(2.2f, 1.6f, 1f, 1f), true);
        var ps = Cloud(parent, "Embers", at, new Vector3(1.4f, 0.4f, 1.4f), emberMat, 11f, 1.8f, 0.05f, 0.11f, new Color(2.2f, 1.6f, 1f, 1f), new Vector3(0f, -2.5f, 0f), 0.3f, 0.35f);
        var main = ps.main;
        main.startSpeed = 2f;
        main.prewarm = false;
    }

    /// Water off the ducts: thin drops falling the height of the hall
    public static void Drips(Transform parent, Vector3 min, Vector3 max)
    {
        if (!Atmosphere) return;
        dripMat ??= Particle(new Color(0.8f, 0.82f, 0.85f, 0.7f), false);
        Vector3 centre = (min + max) * 0.5f;
        var ps = Cloud(parent, "Drips", centre, max - min, dripMat, 6f, 1.6f, 0.03f, 0.03f, new Color(0.8f, 0.82f, 0.85f, 0.7f), Vector3.zero, 1f, 0f);
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.lengthScale = 4f;
        r.velocityScale = 0f;
    }

    /// Light hanging under a skylight: two crossed sheets, amber-cream at the glass and gone six metres down
    public static void Shaft(Transform parent, Vector3 center, Vector2 size, float depth = 6f)
    {
        if (!Atmosphere) return;
        shaftMat ??= Sheet(new Color(1f, 0.9f, 0.7f, 0.12f), ShaftTex(), true);
        Vector3 mid = center - Vector3.up * (depth * 0.5f + 0.1f);
        Prim(parent, mid, new Vector3(size.x, depth, 0.02f), shaftMat, "Shaft");
        Prim(parent, mid, new Vector3(0.02f, depth, size.y), shaftMat, "Shaft");
    }
}
