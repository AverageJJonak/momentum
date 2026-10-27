using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// THE TOWER. Behind the main menu: Halcyon's House — the one the whole first
/// act walks toward — seen whole, in a storm, at the last of the light, from a
/// slow orbit through the city it stands in. This class is the director: it
/// lights the scene, calls in the parts, and runs the weather.
///   THE CITY   MenuCity: GOTHAM, 1939, painted on black paper — twelve
///              avenues of lamps from the plaza to the horizon through a sea of
///              low masonry (brownstone rows, cornice blocks, hotels, theatres,
///              clubs), the Court's twenty-four Deco shafts round the plaza no
///              two alike, stepped shafts and a few crowned giants rising out
///              of the fabric, the landmarks placed by hand, the elevated on
///              stone viaducts; windows punched in stone and few of them lit.
///              Built round the camera's orbit, so the tower is never hidden
///              and the lens is never filled.
///   THE HOUSE  a Deco cathedral of commerce: charcoal stone between limestone
///              piers stepping back four times under a brass crown and a
///              lantern, the company's gold mark high on its south face.
///   THE SKY    MenuSky: a painted dusk, two decks of storm cloud sliding over
///              each other above the crown, lightning inside them.
///   THE WET    the ground is see-through and glossy, and everything that
///              stands on it stands on it TWICE — once the right way up, once
///              mirrored underneath — so every street shines with the city over
///              it. No second camera, no render texture: the still city is a few
///              dozen welded meshes, and drawing them again upside down is cheap.
///   THE LIFE   MenuCityLife and CityLife: the elevated's trains, zeppelins and
///              mail planes on their lanes, police blimps with searchlights, the
///              company's name in lit letters on the Court with its tell, floors
///              filling and emptying, beacons, searchlights in the cloud.
/// Rain over the camera; the company's mark breathing; thunder a few seconds
/// after the flash. The level thumbnails are shot in this scene too: Daylight()
/// puts the storm aside for them and Restore() brings it back.
/// </summary>
public static class MenuBackdrop
{
    static Light sun, fill;

    /// The tower behind the menu is a MEGASTRUCTURE: three times the city's tower, taller than anything else
    public const float TowerScale = 3.5f;   // the castle: bigger and more daunting than the plan drew it (2026-09-21)

    /// The camera: a slow orbit round the tower, high up among the lesser towers, rising and falling as it goes,
    /// always looking a little below the halfway mark so the crown looms out of the top of the frame — nothing stays
    /// hidden behind the band for long, and the thing never fits on the screen. (MenuCity is built round this path:
    /// change the radius or the heights here and the belt it keeps clear has to move with them.)
    public static void Drift(Camera cam)
    {
        float t = Time.unscaledTime;
        float ang = t * 0.024f;                                          // one orbit every four and a half minutes
        float radius = 470f + 40f * Mathf.Sin(t * 0.031f);
        float height = 150f + 110f * Mathf.Sin(t * 0.017f + 1f);
        float h = CityKit.TowerH * TowerScale;
        cam.fieldOfView = 64f;
        cam.transform.position = new Vector3(Mathf.Sin(ang) * radius, height, -Mathf.Cos(ang) * radius);
        cam.transform.LookAt(new Vector3(0f, h * (0.44f + 0.08f * Mathf.Sin(t * 0.021f)), 0f));
        cam.transform.rotation *= Quaternion.Euler(0f, 0f, 1.5f + Mathf.Sin(t * 0.13f) * 0.8f);
    }

    public static Transform Build(MovementConfig config, Camera cam)
    {
        LevelKit.Init();
        LevelKit.SetZone(Zone.Utopia);   // whatever level was played last left its own zone behind: the kit lights by zone
        CityKit.Init(true, 1f, true);    // the menu's smoke runs on the unscaled clock, as the rain does
        MenuCity.Init();
        Random.InitState(1907); // the same city every time the menu opens
        Transform root = new GameObject("MenuBackdrop").transform;
        // Three families: what stands still and is mirrored under the wet ground, what lies flat ON the ground (and is
        // not), and what lives
        Transform still = new GameObject("CityStill").transform;
        still.SetParent(root, false);
        Transform flat = new GameObject("CityGround").transform;
        flat.SetParent(root, false);
        Transform living = new GameObject("CityLife").transform;
        living.SetParent(root, false);
        var life = living.gameObject.AddComponent<CityLife>();
        life.UseUnscaledTime();
        Dusk(root, cam);
        WorldLook.ApplyMenu(root); // the storm's grade: bloom for the windows and the rain, a vignette, grain
        var sky = MenuSky.Build(root);

        var baker = new MeshBaker { CellSize = 500f };   // welded per square so the orbit only draws the squares in front of it
        var sheets = new SheetBaker();
        var ground = new SheetBaker();
        var flickers = new List<Renderer>();
        var fPeriod = new List<float>(); var fPhase = new List<float>(); var fDuty = new List<float>();
        var beacons = new List<Renderer>(); var bPhase = new List<float>();

        // The ground, wet, out past everything the fog lets you see
        ground.Grid(MenuCity.Ground, Vector3.zero, 9600f, 9600f, 40, 40, 260f);
        CityKit.Tower(still, Vector3.zero, baker, true, null, null, TowerScale);
        MenuCity.BuildStill(baker, sheets, ground);
        // The city beyond the city: two rings of the same fabric, all but lost in the haze, so the skyline never just stops
        CityKit.Skyline(baker, Vector3.zero, 0f, 220, 1400f, 1950f, 0f, 360f, 1.5f, sheets);   // with sheets: real fabric, not plain masses
        CityKit.Skyline(baker, Vector3.zero, 0f, 170, 1950f, 2600f, 3f, 363f, 2.2f, sheets);
        // The rot, well up the foot of it now, and round the flank
        float ts = TowerScale;
        LevelKit.Tendril(still, new Vector3(-26f, 0f, -24f) * ts, new Vector3(-13f, 96f, -17.6f) * ts, 1.5f * ts, 16);
        LevelKit.Tendril(still, new Vector3(-32f, 0f, -14f) * ts, new Vector3(-22.6f, 70f, -6f) * ts, 1.1f * ts, 14);
        LevelKit.Tendril(still, new Vector3(22f, 0f, -28f) * ts, new Vector3(15f, 60f, -17.6f) * ts, 1f * ts, 12);
        LevelKit.Tendril(still, new Vector3(34f, 0f, 10f) * ts, new Vector3(22.6f, 48f, -2f) * ts, 0.9f * ts, 10);
        LevelKit.Tendril(still, new Vector3(-6f, 0f, -34f) * ts, new Vector3(-3f, 30f, -21.6f) * ts, 0.8f * ts, 9);
        baker.Flush(still, "CityMass");
        var windows = HalcyonCity.Windows(sheets.Flush(still, "CitySheets"));
        ground.Flush(flat, "CityFlat");

        // THE SMOG: three sheets over the whole plan at street, mid and roof height, lit a warm brown from the city
        // under them (their own colour: the menu's fog turns with the camera), drifting; the far rings sit in it and
        // the House rises out of it. And the smoke: plumes off the powerhouses' stacks and the roofs' chimneys
        // nearest the plaza, the zeppelins' exhaust (CityKit), the windows breathing (WindowTwinkle)
        var smog = LevelKit.CitySmog(living, Vector3.zero, 0f, 2700f, null, true);
        if (smog != null) smog.Tint(new Color(0.24f, 0.19f, 0.17f));
        LevelKit.Plumes(living, Vector3.zero, 1600f, 48, true);
        if (windows.Count > 0) living.gameObject.AddComponent<WindowTwinkle>().Init(windows, true);
        if (CityKit.LanternAt.HasValue) LevelKit.Flare(living, CityKit.LanternAt.Value, 1f, 0.8f, 6000f, 2.2f * CityKit.LanternScale);   // the lantern's flare

        // The sun on the shoulders and the name under it get their own gold, so they can breathe
        var emblem = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        Color gold = new Color(2.2f, 1.7f, 0.4f);
        emblem.SetColor("_BaseColor", gold);
        TextMesh mark = null;
        foreach (var r in still.GetComponentsInChildren<Renderer>())
        {
            if (r.name == "EmblemRing" || r.name == "EmblemBar") r.sharedMaterial = emblem;
            else if (r.name == "TowerMark") mark = r.GetComponent<TextMesh>();
        }
        if (mark != null) mark.GetComponent<MeshRenderer>().sharedMaterial = GameFont.WorldMaterial(mark.font, 4000f, 5000f);

        Mirror(root, still);

        MenuCity.BuildLife(living, life, flickers, fPeriod, fPhase, fDuty, beacons, bPhase);
        var searchlights = Searchlights(living);
        life.SetFlickers(flickers, fPeriod, fPhase, fDuty);
        life.SetBeacons(beacons, bPhase);

        // The rain, and the weather that runs all of it
        Transform rain = Rain(root);
        var weather = root.gameObject.AddComponent<MenuWeather>();
        weather.Init(cam, fill, rain, emblem, gold, mark, searchlights, sky, MenuCity.Scrolling);
        // The menu's lights are the menu's: the level thumbnails, shot on their own layer, never see them
        foreach (var l in root.GetComponentsInChildren<Light>(true)) l.cullingMask &= ~(1 << MenuThumbnails.Layer);
        Random.InitState(System.Environment.TickCount);
        return root;
    }

    static readonly Dictionary<Material, Material> mirrored = new Dictionary<Material, Material>();

    /// THE WET STREET. Everything still, once more, upside down under the see-through ground. Whatever in it is
    /// itself see-through (the windows, above all) gets a copy of its material that draws BEFORE the ground — it is
    /// under it — and nothing in the copy keeps a light, a blink or a shadow.
    static void Mirror(Transform root, Transform still)
    {
        mirrored.Clear();
        var copy = Object.Instantiate(still.gameObject, root);
        copy.name = "CityMirror";
        copy.transform.localPosition = Vector3.zero;
        copy.transform.localScale = new Vector3(1f, -1f, 1f);
        // Behaviours first (a flicker script REQUIRES its light, and would not let it go), then the lights
        foreach (var b in copy.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(b);
        foreach (var l in copy.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(l);
        foreach (var r in copy.GetComponentsInChildren<Renderer>(true))
        {
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            var m = r.sharedMaterial;
            if (m == null || m.renderQueue < 2500) continue;
            if (!mirrored.TryGetValue(m, out var under))
            {
                under = new Material(m) { name = m.name + "_Mirror", renderQueue = MenuCity.QMirror };
                mirrored[m] = under;
            }
            r.sharedMaterial = under;
        }
    }

    /// Two searchlights on the crown's brow: a spot light each and a beam you can see, sweeping the sky and the street
    static Transform[] Searchlights(Transform parent)
    {
        var list = new Transform[2];
        for (int i = 0; i < 2; i++)
        {
            var pivot = new GameObject("Searchlight");
            pivot.transform.SetParent(parent);
            pivot.transform.position = new Vector3(i == 0 ? -13f : 13f, CityKit.TowerH + 7.5f, -9f) * TowerScale;
            var l = pivot.AddComponent<Light>();
            l.type = LightType.Spot;
            l.range = 260f * TowerScale;
            l.spotAngle = 11f;
            l.innerSpotAngle = 6f;
            l.intensity = 9f;
            l.color = new Color(0.85f, 0.92f, 1f);
            l.shadows = LightShadows.None;
            MenuCity.Beam(pivot.transform, 240f * TowerScale, 240f * TowerScale * 0.058f, new Color(0.8f, 0.9f, 1f, 0.22f));   // wide and soft: a beam in smog
            list[i] = pivot.transform;
        }
        return list;
    }

    /// The rain: a box of stretched drops above the camera, falling with a lean, in world space; the weather keeps it over the camera
    static Transform Rain(Transform root)
    {
        var go = new GameObject("Rain");
        go.transform.SetParent(root);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = 1.7f;
        main.startSpeed = 0f;
        main.startSize = 0.05f;
        main.maxParticles = 8000;
        main.gravityModifier = 0f;
        main.loop = true;
        main.useUnscaledTime = true;
        var emission = ps.emission;
        emission.rateOverTime = 2800f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(96f, 1f, 96f);
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-3f, -1.5f);
        vel.y = new ParticleSystem.MinMaxCurve(-25f, -19f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.lengthScale = 12f;
        r.velocityScale = 0f;
        r.sharedMaterial = Glassy(new Color(0.75f, 0.85f, 0.95f, 0.4f));
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        ps.Play();
        return go.transform;
    }

    /// An unlit see-through material: the rain
    static Material Glassy(Color c)
    {
        var m = LevelKit.FromTemplate("UnlitTransparent", "Universal Render Pipeline/Unlit");
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        return m;
    }

    /// Gotham's night: the sun low behind the House and all but gone (a burnt rim down one flank, a band of sienna
    /// left on the horizon), a dark indigo haze that the far city dissolves into, the slate of the cloud for fill
    /// (MenuSky holds the colours; the city's offline preview lights by the same ones)
    static void Dusk(Transform root, Camera cam)
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = MenuSky.AmbientSky;
        RenderSettings.ambientEquatorColor = MenuSky.AmbientEquator;
        RenderSettings.ambientGroundColor = MenuSky.AmbientGround;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0011f;   // a touch thicker than it was: the rings beyond the belt go to smoke, the House stays
        RenderSettings.fogColor = CityTextures.Haze(90f);
        if (cam != null)
        {
            // The dome covers every pixel; this is what is behind it if it ever does not
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = CityTextures.Haze(90f);
            cam.nearClipPlane = 1f;      // nothing comes within a metre of this lens, and the depth buffer is spent on four kilometres
            cam.farClipPlane = 5200f;    // the cloud decks run out to two and a half
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null)
            {
                data.dithering = true;   // a dusk sky in eight bits is a staircase
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality = AntialiasingQuality.High;
            }
        }
        sun = null;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional && l != fill) { sun = l; break; }
        if (sun != null)
        {
            sun.useColorTemperature = false;
            sun.transform.rotation = Quaternion.Euler(9f, 180f + MenuSky.SunAzimuth, 0f); // low, behind the tower, a little to the right: a rim on one flank
            sun.color = MenuSky.SunColor;
            sun.intensity = MenuSky.SunIntensity;
            sun.shadows = LightShadows.None;   // nothing in the city casts one (it is welded, and it is dusk)
        }
        var fillGo = new GameObject("CityFill");
        fillGo.transform.SetParent(root);
        fillGo.transform.rotation = Quaternion.Euler(38f, 15f, 0f);
        fill = fillGo.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = MenuSky.FillColor;
        fill.intensity = MenuSky.FillIntensity;
        fill.shadows = LightShadows.None;
    }

    /// What Daylight() puts aside: the storm's fog, ambient, sun and sky reflection, whole
    public struct Held
    {
        public bool fog, fillOn;
        public FogMode fogMode;
        public float fogDensity, fogStart, fogEnd, sunIntensity;
        public AmbientMode mode;
        public Color fogColor, ambient, sky, equator, ground, sunColor;
        public Quaternion sunRot;
        public DefaultReflectionMode reflection;
    }

    /// The menu's storm, put aside so the level thumbnails can be shot in plain daylight (a level's own haze
    /// may replace it while it is built); Restore() puts the storm back exactly
    public static Held Daylight()
    {
        var h = new Held
        {
            fog = RenderSettings.fog, fogMode = RenderSettings.fogMode, fogDensity = RenderSettings.fogDensity,
            fogStart = RenderSettings.fogStartDistance, fogEnd = RenderSettings.fogEndDistance, fogColor = RenderSettings.fogColor,
            mode = RenderSettings.ambientMode, ambient = RenderSettings.ambientLight,
            sky = RenderSettings.ambientSkyColor, equator = RenderSettings.ambientEquatorColor, ground = RenderSettings.ambientGroundColor,
            fillOn = fill != null && fill.enabled,
            reflection = RenderSettings.defaultReflectionMode,
        };
        RenderSettings.fog = false;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.57f, 0.62f);
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;   // a level's gloss mirrors the scene's own noon, not the menu's storm
        if (sun != null)
        {
            h.sunRot = sun.transform.rotation;
            h.sunColor = sun.color;
            h.sunIntensity = sun.intensity;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            sun.color = new Color(1f, 0.96f, 0.9f);
            sun.intensity = 1f;
        }
        if (fill != null) fill.enabled = false;
        return h;
    }

    public static void Restore(Held h)
    {
        RenderSettings.fog = h.fog;
        RenderSettings.fogMode = h.fogMode;
        RenderSettings.fogDensity = h.fogDensity;
        RenderSettings.fogStartDistance = h.fogStart;
        RenderSettings.fogEndDistance = h.fogEnd;
        RenderSettings.fogColor = h.fogColor;
        RenderSettings.ambientMode = h.mode;
        RenderSettings.ambientLight = h.ambient;
        RenderSettings.ambientSkyColor = h.sky;
        RenderSettings.ambientEquatorColor = h.equator;
        RenderSettings.ambientGroundColor = h.ground;
        RenderSettings.defaultReflectionMode = h.reflection;
        if (sun != null)
        {
            sun.transform.rotation = h.sunRot;
            sun.color = h.sunColor;
            sun.intensity = h.sunIntensity;
        }
        if (fill != null) fill.enabled = h.fillOn;
    }
}

/// <summary>
/// The storm, running: the rain and the sky kept over the camera; the cloud
/// decks sliding; the traffic running its streets (one texture offset per
/// direction moves every car in the city); the fog taking the colour of the sky
/// the camera is facing; the sun on the shoulders breathing and now and then
/// going out for a beat (the tell); the searchlights sweeping; and lightning —
/// a double flash INSIDE the cloud, somewhere new each time, that lights the
/// decks, the sky and the whole city from above, the thunder rolling in a few
/// seconds later from the recorded booms. The beacons, the trains and the air
/// traffic are CityLife's. Unscaled time: the menu's clock.
/// </summary>
public class MenuWeather : MonoBehaviour
{
    Camera cam;
    Light fill;
    Transform rain;
    Material emblem;
    Color gold;
    TextMesh mark;
    Transform[] searchlights;
    MenuSky.Parts sky;
    List<Material> scrolling;
    AudioSource thunder;
    float fillBase, nextFlash, flashStart = -10f, thunderAt = float.PositiveInfinity;
    Color ambientBase;

    public void Init(Camera cam, Light fill, Transform rain, Material emblem, Color gold, TextMesh mark, Transform[] searchlights, MenuSky.Parts sky, List<Material> scrolling)
    {
        this.cam = cam;
        this.fill = fill;
        this.rain = rain;
        this.emblem = emblem;
        this.gold = gold;
        this.mark = mark;
        this.searchlights = searchlights;
        this.sky = sky;
        this.scrolling = scrolling;
        fillBase = fill != null ? fill.intensity : 0f;
        ambientBase = RenderSettings.ambientSkyColor;
        nextFlash = Time.unscaledTime + 3f;
        thunder = gameObject.AddComponent<AudioSource>();
        thunder.spatialBlend = 0f;
    }

    void Update()
    {
        float t = Time.unscaledTime;

        // The sun on the shoulders breathes, and never goes out: the company's mark does not glitch (the tell is a
        // letter dying on the Court's signs, MenuCityLife)
        float k = 0.82f + 0.18f * Mathf.Sin(t * 0.7f);
        if (emblem != null) emblem.SetColor("_BaseColor", gold * k);
        if (mark != null) mark.color = gold * k;

        // The searchlights sweep the sky over the street, each on its own slow arc
        if (searchlights != null)
            for (int i = 0; i < searchlights.Length; i++)
            {
                if (searchlights[i] == null) continue;
                float s = i == 0 ? 1f : -1f;
                float yaw = 180f + s * Mathf.Sin(t * 0.21f + i * 1.3f) * 55f;
                float pitch = -(26f + 14f * Mathf.Sin(t * 0.17f + i * 0.9f));
                searchlights[i].rotation = Quaternion.Euler(pitch, yaw, 0f);
            }

        // Every car in the city: the lanes are laid so that ONE direction of scroll runs each of them the right way
        if (scrolling != null)
            for (int i = 0; i < scrolling.Count; i++)
                if (scrolling[i] != null) scrolling[i].SetTextureOffset("_BaseMap", new Vector2(0f, Mathf.Repeat(-t * (0.052f + 0.009f * i), 1f)));

        // The decks slide, each its own way
        if (sky != null)
            for (int i = 0; i < sky.decks.Count; i++)
                sky.decks[i].SetTextureOffset("_BaseMap", new Vector2(Mathf.Repeat(t * sky.wind[i].x, 1f), Mathf.Repeat(t * sky.wind[i].y, 1f)));

        // Lightning: a double flash, then the thunder a few seconds behind it
        if (t >= nextFlash)
        {
            flashStart = t;
            nextFlash = t + Random.Range(7f, 16f);
            thunderAt = t + Random.Range(1.2f, 3.2f);
            if (sky != null && sky.flash != null && cam != null)
            {
                // Somewhere in the cloud ahead of the lens, mostly: a strike nobody can see is only a change of light
                Vector3 ahead = cam.transform.forward;
                ahead.y = 0f;
                ahead = ahead.sqrMagnitude > 0.01f ? ahead.normalized : Vector3.forward;
                Vector3 side = Vector3.Cross(Vector3.up, ahead);
                Vector3 at = cam.transform.position + ahead * Random.Range(300f, 1500f) + side * Random.Range(-900f, 900f);
                float size = Random.Range(900f, 1900f);
                sky.flash.position = new Vector3(at.x, sky.flash.position.y, at.z);
                sky.flash.localScale = new Vector3(size, 1f, size);
            }
        }
        float age = t - flashStart;
        float f = age < 0.06f ? 1f : age < 0.11f ? 0f : age < 0.22f ? 0.85f : age < 0.6f ? 0.85f * (1f - (age - 0.22f) / 0.38f) : 0f;
        if (fill != null) fill.intensity = fillBase + 5f * f;
        RenderSettings.ambientSkyColor = ambientBase + new Color(0.5f, 0.5f, 0.6f) * f;
        if (sky != null)
        {
            if (sky.domeMat != null) sky.domeMat.SetColor("_BaseColor", MenuSky.Gain(Color.white, 1f + 1.3f * f));
            for (int i = 0; i < sky.decks.Count; i++) sky.decks[i].SetColor("_BaseColor", MenuSky.Gain(sky.deckTint[i], 1f + 3.5f * f));
            if (sky.flash != null)
            {
                var fr = sky.flash.GetComponent<Renderer>();
                if (fr != null) fr.enabled = f > 0f;
                Color c = MenuSky.Gain(sky.flashTint, 3f);
                c.a = f;
                if (sky.flashMat != null) sky.flashMat.SetColor("_BaseColor", c);
            }
        }
        if (t >= thunderAt)
        {
            thunderAt = float.PositiveInfinity;
            // Real thunder (Resources/Impacts/thunder_*) when the library has it; the old sci-fi boom and sub when it does not
            var real = GameFeel.Clip("thunder");
            if (thunder != null)
            {
                thunder.volume = GameSettings.SfxVolume;
                if (real != null)
                {
                    thunder.pitch = Random.Range(0.92f, 1.06f);
                    thunder.PlayOneShot(real, 1f);
                }
                else
                {
                    var boom = GameFeel.Clip("boom");
                    var sub = GameFeel.Clip("sub");
                    thunder.pitch = Random.Range(0.45f, 0.6f);
                    if (boom != null) thunder.PlayOneShot(boom, 0.9f);
                    if (sub != null) thunder.PlayOneShot(sub, 0.8f);
                }
            }
        }
    }

    /// After the camera has moved (MainMenuUI drifts it in Update): the sky and the rain ride on it, and the haze takes
    /// the colour of the sky it is looking into — warm toward where the sun went down, bruise-blue away from it
    void LateUpdate()
    {
        if (cam == null) return;
        Vector3 p = cam.transform.position;
        if (rain != null) rain.position = p + new Vector3(0f, 28f, 0f) + cam.transform.forward * 14f;
        if (sky != null && sky.dome != null) sky.dome.position = p;
        float age = Time.unscaledTime - flashStart;
        float f = age < 0.06f ? 1f : age < 0.11f ? 0f : age < 0.22f ? 0.85f : age < 0.6f ? 0.85f * (1f - (age - 0.22f) / 0.38f) : 0f;
        RenderSettings.fogColor = Color.Lerp(MenuSky.FogFor(cam.transform.forward), new Color(0.62f, 0.62f, 0.72f), f * 0.45f);
    }
}
