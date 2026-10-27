using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE CITY OUTSIDE. The first act happens up in Halcyon's civic buildings,
/// and their windows and open yards look out on THE CITY THE MENU SHOWS: the
/// Deco city, whole, in sepia daylight (HalcyonCity / MenuCity). The level
/// stands out among its rings, seven hundred metres from the plaza, and it
/// stands IN the city, not over it: its open-sky rooms are about six floors
/// over the street (Vantage), so the blocks across the way rise past the eye
/// line and hide the far masses, the elevated passes at window height, and
/// only the shafts, the Court and the House show over the roofs. Through its
/// east windows the terraces, the Court and the House; through its west the
/// rings and the city beyond them. Nothing of the plan is raised where the
/// level is (MenuCity.Reserve): the traffic, the promenades and the rings
/// simply stop at its walls. By day the city's light is turned down to the
/// level's sun (it comes up as the first act's sky goes down) and it stands
/// on dry stone. Everything still is welded per square; what lives in it
/// (MenuCityLife) is built only when the level is played, never for a
/// thumbnail.
/// </summary>
public static class CityVista
{
    /// The menu's thumbnails build every level to photograph it: they get the shape of the city and none of its life
    public static bool Preview;

    /// The first act, and the two courses that share its sky
    /// Set by LevelKit.BrutalVista for the level it stands round; GameBootstrap clears it before every build
    public static bool Suppressed;
    /// A course that turns (Course.Ascent) says where it stands in the world; the city keeps clear of it and frames it
    public static Bounds? Override;

    /// Every course stands in the city (2026-10-27: all thirteen levels are open courses now), the two hand-built ones too
    public static bool Wants(LevelKind kind) => kind != LevelKind.MainMenu;

    /// The plaza is this far east of the level's east wall: the level stands where the blades ring gives way to the spires
    const float PlanEast = 740f;

    /// THE VANTAGE. The first act looks across the city from its own storeys, not down on it from a tower: the city's
    /// ground goes `Storeys` under the open-sky rooms' floor (the plaza, the yards, the crown terrace: the middle one
    /// when a level climbs), so they stand about six floors over the street, the near blocks rise past the eye line
    /// and hide the far masses, and the House still towers over all of it. Never nearer than `Under` to the lowest
    /// room's floor (a skybridge's pit is sixteen metres under its deck), or the plan's paving would cut through
    /// the level
    const float Storeys = 22f, Under = 18f;

    /// A stretch of one side the player can see out of, and the lowest sightline in it
    struct Span { public float z0, z1, y0; }

    public static void Build(LevelKind kind)
    {
        if (Suppressed || !Wants(kind)) return;   // a brutalist course builds its own vista (LevelKit.BrutalVista)
        var layout = LevelSections.Last;
        var apertures = new List<LevelSections.Aperture>();
        Vector3 min, max;
        switch (kind)
        {
            // The two hand-built courses: open ground, so the sky is theirs on both sides
            case LevelKind.Tutorial:
                min = TutorialLevelBuilder.Min; max = TutorialLevelBuilder.Max;   // the course says how long it is
                apertures.Add(new LevelSections.Aperture { side = -1, z0 = min.z, z1 = max.z, y0 = 2f, y1 = 60f });
                apertures.Add(new LevelSections.Aperture { side = 1, z0 = min.z, z1 = max.z, y0 = 2f, y1 = 60f });
                break;
            case LevelKind.PracticeRange:
                min = new Vector3(-71f, 0f, -71f); max = new Vector3(71f, 14f, 71f);
                apertures.Add(new LevelSections.Aperture { side = -1, z0 = min.z, z1 = max.z, y0 = 12f, y1 = 60f });
                apertures.Add(new LevelSections.Aperture { side = 1, z0 = min.z, z1 = max.z, y0 = 12f, y1 = 60f });
                break;
            default:
            {
                if (layout == null || layout.rooms.Count == 0 || layout.apertures.Count == 0) return; // sealed level: no city
                apertures.AddRange(layout.apertures);
                float hw = 0f, y0 = float.MaxValue, y1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
                foreach (var r in layout.rooms)
                {
                    hw = Mathf.Max(hw, r.halfW);
                    y0 = Mathf.Min(y0, Mathf.Min(r.entryY, r.exitY));
                    y1 = Mathf.Max(y1, Mathf.Max(r.entryY, r.exitY) + 20f);
                    z0 = Mathf.Min(z0, r.z0);
                    z1 = Mathf.Max(z1, r.z1);
                }
                min = new Vector3(-hw, y0, z0);
                max = new Vector3(hw, y1, z1);
                break;
            }
        }
        if (Override.HasValue) { min = Override.Value.min; max = Override.Value.max; }
        var west = Spans(apertures, -1, 45f);
        var east = Spans(apertures, 1, 45f);
        if (west.Count == 0 && east.Count == 0) return;

        LevelKit.Init();
        var sky = WorldLook.SkyFor(kind);
        // The city's own light comes up as the level's sun goes down: faint at noon, the whole of it at sundown
        float gain = Mathf.Clamp(0.35f + (1.9f - sky.sunIntensity) * 1.2f, 0.35f, 1.3f);
        CityKit.Init(false, gain);
        Random.InitState(4100 + (int)kind); // the same city outside every time
        Transform root = new GameObject("CityVista").transform;
        float groundY = Vantage(layout, apertures, min);
        Vector3 centre = (min + max) * 0.5f;
        Vector3 origin = new Vector3(max.x + PlanEast, groundY, centre.z);
        CityLife life = null;
        if (!Preview)
        {
            var living = new GameObject("CityLife");
            living.transform.SetParent(root, false);
            life = living.AddComponent<CityLife>();
        }
        var beacons = new List<Renderer>();
        var bPhase = new List<float>();
        // THE RED STREET: the third level's far fabric is flat matte oxblood with small dark windows (the lighting's
        // violet sky and full moon go with it); every other level's is the near-black cut-out
        MenuCity.RedFabric = kind == LevelKind.Level3;
        HalcyonCity.Raise(root, origin, gain, false, life, beacons, bPhase, new Vector4(min.x - 40f, max.x + 40f, min.z - 80f, max.z + 80f), true);   // by day: sepia stone, the windows nearly out, the parks olive
        MenuCity.RedFabric = false;
        // THE SEARCHLIGHTS (2026-10-27, user: "spot lights and whatever to make the city feel alive"): four limestone
        // towers round the level's corners, each sweeping the sky with an additive beam and a real spot light
        if (!Preview)
            for (int i = 0; i < 4; i++)
            {
                float sx = i % 2 == 0 ? min.x - 60f : max.x + 60f, sz = Mathf.Lerp(min.z, max.z, i < 2 ? 0.18f : 0.82f);
                LevelKit.SearchlightTower(root, new Vector3(sx, groundY, sz), 46f + 9f * (i % 3), life, i % 2 == 0 ? 5f : -4f, 90f * i);
            }

        // The haze lying in the streets below the level: one sheet in the sky's own colour a few metres over the
        // paving, for depth down every avenue; as wide as the plan, so its edge is lost in the fog too. It stays
        // under the lowest room: the level stands over it, never in it
        var haze = CityKit.HazeSheet(new Color(sky.ground.r, sky.ground.g, sky.ground.b, 0.22f));
        haze.renderQueue = LevelKit.QSmog - 7;   // under the smog sheets (LevelKitSmog), which lie over it
        var sheet = LevelKit.Prim(root, new Vector3(origin.x, groundY + 5f, centre.z), new Vector3(6000f, 0.02f, 6000f), haze, "HazeFloor");
        sheet.GetComponent<Renderer>().receiveShadows = false;

        if (life != null)
        {
            // Birds over the roofs by the windows: the one thing in the sky the company did not build
            for (int i = 0; i < 2; i++)
            {
                var spans = i == 0 ? west : east;
                if (spans.Count == 0) continue;
                var s = spans[spans.Count / 2];
                float x = (i == 0 ? min.x : max.x) + (i == 0 ? -1f : 1f) * 160f;
                CityKit.Flock(root, life, new Vector3(x, 0f, Mathf.Lerp(s.z0, s.z1, 0.5f)), 90f, 140f, min.y + Random.Range(10f, 40f), 9);
            }
            // Two drones on their rounds close in, on the sides anyone can see (their lights are gone: unlit rotors read fine)
            for (int i = 0; i < 2; i++)
            {
                var spans = i == 0 ? west : east;
                if (spans.Count == 0) continue;
                var s = spans[spans.Count / 2];
                float x = (i == 0 ? min.x : max.x) + (i == 0 ? -1f : 1f) * Random.Range(30f, 55f);
                LevelKit.Drone(root, new Vector3(x, s.y0 + Random.Range(2f, 16f), Mathf.Lerp(s.z0, s.z1, 0.5f)), 14f, i == 1);
            }
            foreach (var l in root.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(l);
            life.SetBeacons(beacons, bPhase);
        }
        Haze(kind);
        Random.InitState(System.Environment.TickCount);
    }

    /// Where the city's ground goes for this level: `Storeys` under the open-sky rooms' floor (the middle one when a
    /// level climbs; an open sky is two hundred metres of aperture over a yard's wall, a window is not), never less
    /// than `Under` under the lowest room. A course with no rooms (the tutorial, the range) is its own floor
    static float Vantage(LevelSections.Layout layout, List<LevelSections.Aperture> apertures, Vector3 min)
    {
        var floors = new List<float>();
        foreach (var a in apertures)
        {
            if (a.y1 - a.y0 < 150f) continue;
            float floor = a.y0 - 20f;   // a yard's wall is twenty to thirty metres: the floor if no room claims the aperture
            if (layout != null)
                foreach (var r in layout.rooms)
                    if (a.z0 >= r.z0 - 0.5f && a.z1 <= r.z1 + 0.5f) { floor = Mathf.Min(r.entryY, r.exitY); break; }
            floors.Add(floor);
        }
        float reference = min.y;
        if (floors.Count > 0) { floors.Sort(); reference = floors[floors.Count / 2]; }
        return Mathf.Min(reference - Storeys, min.y - Under);
    }

    /// The stretches of one side that a window or an open sky looks out of, merged, with a margin for the angle
    static List<Span> Spans(List<LevelSections.Aperture> apertures, int side, float margin)
    {
        var raw = new List<Span>();
        foreach (var a in apertures)
            if (a.side == side) raw.Add(new Span { z0 = a.z0 - margin, z1 = a.z1 + margin, y0 = a.y0 });
        raw.Sort((p, q) => p.z0.CompareTo(q.z0));
        var merged = new List<Span>();
        foreach (var s in raw)
        {
            if (merged.Count > 0 && s.z0 <= merged[merged.Count - 1].z1)
            {
                var last = merged[merged.Count - 1];
                last.z1 = Mathf.Max(last.z1, s.z1);
                last.y0 = Mathf.Min(last.y0, s.y0);
                merged[merged.Count - 1] = last;
                continue;
            }
            merged.Add(s);
        }
        return merged;
    }

    /// A haze in the level's own horizon colour: the far towers fade into the sky. WorldLook takes over in play;
    /// this is what a thumbnail sees.
    static void Haze(LevelKind kind)
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 140f;
        RenderSettings.fogEndDistance = 8000f;   // the whole skyline shows in a thumbnail, not a wall of haze
        RenderSettings.fogColor = WorldLook.Horizon(kind);
    }
}
