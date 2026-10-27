using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// THE CITY. One vocabulary for Halcyon's city wherever it is seen — behind
/// the main menu at night, out of the first act's windows by sepia day — so it
/// is the same city every time: the company's tower, the HOUSE (its stone is
/// MenuCity.HouseMass's, a Deco cathedral of commerce in charcoal and
/// limestone under a brass crown; the company's mark and name are kept here,
/// since they breathe), the viaducts, the zeppelins with lit gondolas, the
/// mail planes, the police blimps, and the masses of the skyline beyond. Dark
/// Deco since 2026-10-07: stone, brass and bronze; amber windows, cream lamps,
/// red only on an aircraft lamp. Init picks the hour: dusk lights the floors
/// up, day keeps them faint, and a gain brightens them again as a level's sky
/// goes down. The cyberpunk vocabulary (curtain-wall towers, neon signs, LED
/// facades, sky trams, dishes, holo lanes) is gone.
///
/// EVERYTHING STILL IS BAKED. Every piece that never moves goes into a
/// MeshBaker instead of becoming a GameObject, so a whole tower's worth of
/// stone is one mesh and one draw. Only what LIVES — the airships, the planes,
/// the blimps, the roof beacons — keeps a transform of its own, and those go
/// to CityLife, which drives all of them from one loop.
/// </summary>
public static class CityKit
{
    public const float TowerH = 190f;
    public static bool Dusk { get; private set; }
    static Material black, blackGloss, seam, seamDim, gold, mass, massFar, street, hollow, beam;
    static Material beaconRed, trainWindow, adPanel, foliage, lampWarm, girder, creamHull;
    static Material sweepBeam;
    /// A searchlight's beam: additive, over white, the beam sheet's fade along it (one material for every sweep)
    static Material SweepBeam => sweepBeam != null ? sweepBeam : sweepBeam = MenuCity.Sheet(CityTextures.Beam(), new Color(1.7f, 1.75f, 1.9f, 0.5f), (int)UnityEngine.Rendering.RenderQueue.Transparent, true);
    /// The ink the company's name is painted in on a lit panel
    static readonly Color Ink = new Color(0.07f, 0.07f, 0.1f);

    public static Material BeaconRed => beaconRed;
    public static Material Mass => mass;
    /// An emptied tower: stained, unlit, the one thing in the white city that is not clean
    public static Material Hollow => hollow;
    /// Where the House's lantern stands, once Tower has built it (world), and the House's scale: the flare goes here
    public static Vector3? LanternAt { get; private set; }
    public static float LanternScale { get; private set; } = 1f;
    /// The clock the city's smoke and smog run on: the menu's is unscaled
    public static bool Unscaled { get; private set; }

    /// `windowGain` brightens the lit floors: 1 by day, more as a level's sky goes down; `unscaled` runs the smoke
    /// on the unscaled clock (the menu)
    public static void Init(bool dusk, float windowGain = 1f, bool unscaled = false)
    {
        Dusk = dusk;
        Unscaled = unscaled;
        LanternAt = null;
        MenuCity.ClearStacks();   // a new build: no stack of the last one smokes here
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        black = Lit(lit, new Color(0.11f, 0.11f, 0.13f), 0.45f);     // charcoal stone
        blackGloss = Lit(lit, new Color(0.06f, 0.06f, 0.07f), 0.8f); // black marble
        // THE CITY IS STONE. By night charcoal and dark limestone that go to black paper; by day the same in sepia
        // daylight, limestone and cream. The House is charcoal in every light, the one black thing in it.
        mass = dusk ? Lit(lit, new Color(0.12f, 0.115f, 0.12f), 0.3f) : Lit(lit, new Color(0.6f, 0.56f, 0.48f), 0.35f);
        massFar = dusk ? Lit(lit, new Color(0.07f, 0.07f, 0.09f), 0.2f) : Lit(lit, new Color(0.58f, 0.54f, 0.48f), 0.2f);
        hollow = dusk ? Lit(lit, new Color(0.05f, 0.05f, 0.06f), 0.15f) : Lit(lit, new Color(0.36f, 0.34f, 0.33f), 0.1f);
        beam = HazeSheet(new Color(1.5f, 1.45f, 1.3f, 0.15f));   // wider than it was and softer: a beam in smog, not a rod
        seam = Unlit(unlit, new Color(1.6f, 1.5f, 1.25f));     // lamp cream
        seamDim = Unlit(unlit, new Color(0.9f, 0.72f, 0.45f));  // amber, dim
        gold = Unlit(unlit, new Color(2.2f, 1.7f, 0.4f));
        street = Unlit(unlit, dusk ? new Color(1.2f, 0.9f, 0.5f) : new Color(0.62f, 0.58f, 0.5f));
        beaconRed = Unlit(unlit, new Color(2.6f, 0.2f, 0.12f));
        trainWindow = Unlit(unlit, new Color(1.7f, 1.3f, 0.8f));   // amber
        adPanel = Unlit(unlit, new Color(1.6f, 1.3f, 0.8f));       // a lit name on a gondola
        foliage = dusk ? Lit(lit, new Color(0.06f, 0.055f, 0.04f), 0.2f) : Lit(lit, new Color(0.25f, 0.22f, 0.12f), 0.2f); // the terraces' trees: sepia olive by day, black by night
        creamHull = Lit(lit, new Color(0.78f, 0.72f, 0.6f), 0.35f);   // the company's own zeppelins: cream doped fabric
        lampWarm = Unlit(unlit, new Color(2.2f, 1.4f, 0.7f));
        girder = Lit(lit, new Color(0.3f, 0.2f, 0.1f), 0.35f);       // the cranes' rusted iron
    }

    /// The company's tower, its foot at `at`, its face to the south (-z): the stone is MenuCity.HouseMass's (so the
    /// menu's offline harness builds the same House); here the company's mark in its brass cartouche high on the
    /// south face, the name under it, the crown's beacons and (if asked) the floods, which stay their own objects.
    /// `scale` makes it a megastructure: every dimension multiplied
    public static void Tower(Transform root, Vector3 at, MeshBaker b, bool floods = true, List<Renderer> beacons = null, List<float> beaconPhases = null, float scale = 1f)
    {
        float s = Mathf.Max(0.1f, scale);
        Quaternion face = Quaternion.LookRotation(Vector3.forward); // props on the front face: forward into the wall, seen from -z
        float h = TowerH;
        Vector3 P(float x, float y, float z) => at + new Vector3(x, y, z) * s;
        MenuCity.HouseMass(b, at, s);
        LanternAt = P(0f, h + 15.1f, 0f);   // the cream lantern on the crown (MenuCityHouse: h + 13.5, its glass 1.6 up)
        LanternScale = s;
        CrownGlints(root, at, s);
        LevelKit.Emblem(root, P(0f, MenuCity.MarkY, MenuCity.MarkZ), face, MenuCity.MarkSize * s, gold, blackGloss);
        LevelKit.Text(root, P(0f, MenuCity.MarkY - 6.6f, MenuCity.MarkZ - 0.05f), face, LevelKit.Company, 1.6f * s, new Color(2.2f, 1.7f, 0.4f), "TowerMark", 9f * s, true, 4000f, 5000f);
        // The crown's beacons: the one thing on it that moves
        if (beacons != null)
            for (int i = 0; i < 4; i++)
            {
                var go = LevelKit.Prim(root, P(i % 2 == 0 ? -8f : 8f, h + 5f, i < 2 ? -6.2f : 6.2f),
                    Vector3.one * 1.4f * s, beaconRed, "Beacon", PrimitiveType.Sphere);
                beacons.Add(go.GetComponent<Renderer>());
                beaconPhases?.Add(i * 0.25f);
            }
        if (!floods) return;
        LevelKit.Floodlight(root, P(-28f, 1.2f, -48f), new Vector3(14f, 60f, 26f), 110f * s, 3.5f);
        LevelKit.Floodlight(root, P(28f, 1.2f, -48f), new Vector3(-14f, 60f, 26f), 110f * s, 3.5f);
    }

    /// GLINTS ON THE CROWN: a bright point at every corner and mid-edge of the four brass steps' gilt lips, on the
    /// tops of the gilt ribs round the first, and on the lantern's cap, twinkling (LevelKit.Glints). Sized to be a
    /// pixel of sparkle from the menu's orbit and the first act's windows
    static void CrownGlints(Transform root, Vector3 at, float s)
    {
        if (CityVista.Preview) return;
        float h = TowerH, size = 0.3f * s;
        Vector3 P(float x, float y, float z) => at + new Vector3(x, y, z) * s;
        var set = new LevelKit.GlintSet(6);
        float[] cw = { 17f, 13f, 9.5f, 6.5f }, cd = { 13f, 10f, 7.5f, 5f }, cy1 = { 4.5f, 8f, 11f, 13.5f };
        for (int i = 0; i < 4; i++)
        {
            float hx = (cw[i] + 0.5f) * 0.5f, hz = (cd[i] + 0.5f) * 0.5f, y = h + cy1[i];
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2) set.Add(P(sx * hx, y, sz * hz), size);
            for (int sz = -1; sz <= 1; sz += 2) { set.Add(P(-hx * 0.5f, y, sz * hz), size); set.Add(P(hx * 0.5f, y, sz * hz), size); }
        }
        for (int i = 0; i < 9; i += 2) { set.Add(P(-8f + i * 2f, h + 4.25f, -6.65f), size); set.Add(P(-8f + i * 2f, h + 4.25f, 6.65f), size); }
        for (int i = 0; i < 6; i += 2) { set.Add(P(-8.65f, h + 4.25f, -5f + i * 2f), size); set.Add(P(8.65f, h + 4.25f, -5f + i * 2f), size); }
        float cap = h + 13.5f + 3.85f;
        set.Add(P(2.4f, cap, 0f), size); set.Add(P(-2.4f, cap, 0f), size); set.Add(P(0f, cap, 2.4f), size); set.Add(P(0f, cap, -2.4f), size);
        set.Flush(root, "CrownGlints");
    }

    /// An elevated road between two points at their height: a deck with lit kerbs, columns down to the ground
    public static void Viaduct(MeshBaker b, Vector3 from, Vector3 to, float width, float groundY)
    {
        Vector3 d = to - from;
        float len = d.magnitude;
        if (len < 1f) return;
        Vector3 dir = d / len, mid = (from + to) * 0.5f;
        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
        Vector3 side = Vector3.Cross(dir, Vector3.up).normalized;
        b.Add(mass, mid - Vector3.up * 0.6f, new Vector3(width, 1.2f, len), rot);
        b.Add(street, mid + side * (width * 0.5f - 0.3f) + Vector3.up * 0.05f, new Vector3(0.3f, 0.1f, len), rot);
        b.Add(street, mid - side * (width * 0.5f - 0.3f) + Vector3.up * 0.05f, new Vector3(0.3f, 0.1f, len), rot);
        for (float s = 20f; s < len - 10f; s += 60f)
        {
            Vector3 p = from + dir * s;
            float h = p.y - 1.2f - groundY;
            if (h > 2f) b.Add(mass, new Vector3(p.x, groundY + h * 0.5f, p.z), new Vector3(3f, h, 3f));
        }
    }

    /// A ZEPPELIN over the city, 1939: a long hull (charcoal, or cream for the company's own) with tail fins, a lit
    /// gondola of amber windows slung under it, the company's name in ink on a lit panel down each flank, a lamp
    /// under the nose, a red lamp on top, and a cream SEARCHLIGHT under the gondola walking the roofs on a slow
    /// sweep; slow, high, unmissable. From `from` to `to`, and round again
    public static void Airship(Transform root, CityLife life, Vector3 from, Vector3 to, float speed, float phase, bool cream = false)
    {
        var ship = AirshipBody(root, life, from, cream, out Renderer lamp);
        life.AddAircraft(ship, from, to, speed, phase, lamp);
    }

    /// The same on a closed path (the menu's lanes round the Court: nothing ever pops in or out at the end of a lane)
    public static void Airship(Transform root, CityLife life, Vector3[] path, float speed, float phase, bool cream = false)
    {
        if (path == null || path.Length < 2) return;
        var ship = AirshipBody(root, life, path[0], cream, out Renderer lamp);
        life.AddPath(ship, path, speed, phase);
        life.AddAircraft(null, Vector3.zero, Vector3.zero, 0f, 0f, lamp);   // the lamp only: it blinks with the others
    }

    static Transform AirshipBody(Transform root, CityLife life, Vector3 at, bool cream, out Renderer lamp)
    {
        var ship = new GameObject("Airship");
        ship.transform.SetParent(root);
        ship.transform.position = at;
        Material hull = cream ? creamHull : mass;
        LevelKit.Prim(ship.transform, at, new Vector3(18f, 16f, 72f), hull, "Hull", PrimitiveType.Sphere).transform.localPosition = Vector3.zero;
        LevelKit.Prim(ship.transform, at, new Vector3(1f, 14f, 16f), hull, "Fin").transform.localPosition = new Vector3(0f, 0f, -28f);
        LevelKit.Prim(ship.transform, at, new Vector3(16f, 1f, 16f), hull, "Fin").transform.localPosition = new Vector3(0f, 0f, -28f);
        LevelKit.Prim(ship.transform, at, new Vector3(4f, 3f, 16f), blackGloss, "Gondola").transform.localPosition = new Vector3(0f, -8.5f, 2f);
        LevelKit.Prim(ship.transform, at, new Vector3(4.2f, 1f, 13f), trainWindow, "GondolaWindows").transform.localPosition = new Vector3(0f, -8.3f, 2f);
        // The company's name down each flank: a lit panel, the letters painted on it in ink, read from that side.
        // (The text is made at the ship's own place and turned after: the ship has unit scale, so local is world here)
        for (int s = -1; s <= 1; s += 2)
        {
            LevelKit.Prim(ship.transform, at, new Vector3(0.3f, 3.4f, 22f), adPanel, "Name").transform.localPosition = new Vector3(s * 9.1f, 1f, 4f);
            var tm = LevelKit.Text(ship.transform, at, Quaternion.identity, LevelKit.Company, 0.42f, Ink, "HullName", 20f, true, 900f, 1400f);
            tm.transform.localPosition = new Vector3(s * 9.32f, 1f, 4f);
            tm.transform.localRotation = Quaternion.LookRotation(new Vector3(-s, 0f, 0f), Vector3.up);   // readable from its own side of the hull
        }
        LevelKit.Prim(ship.transform, at, Vector3.one * 1.2f, seam, "NoseLamp", PrimitiveType.Sphere).transform.localPosition = new Vector3(0f, -7.2f, 30f);
        var top = LevelKit.Prim(ship.transform, at, Vector3.one * 1.6f, beaconRed, "Strobe", PrimitiveType.Sphere);
        top.transform.localPosition = new Vector3(0f, 8.6f, 0f);
        lamp = top.GetComponent<Renderer>();
        Sweep(ship.transform, new Vector3(0f, -10.2f, 6f), 120f, 4.5f, 48f, Random.Range(4f, 7f) * (Random.value < 0.5f ? 1f : -1f), Random.Range(0f, 360f), life);
        // The engines' exhaust off the back of the gondola: a trail hanging in the air where the ship was
        if (life != null) LevelKit.Exhaust(ship.transform, new Vector3(0f, -8.6f, -6.5f), Unscaled);
        return ship.transform;
    }

    /// A SEARCHLIGHT THAT WALKS: a cream lamp on a pivot `local` under `parent`, its beam `length` long and `width`
    /// wide pitched `down` degrees below level, the pivot turning `degPerSec` (CityLife spins it; whatever it hangs
    /// under carries it wherever it goes). The beam is an unlit see-through box, no Light: it costs nothing
    static void Sweep(Transform parent, Vector3 local, float length, float width, float down, float degPerSec, float phase, CityLife life)
    {
        var pivot = new GameObject("Sweep");
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = local;
        LevelKit.Prim(pivot.transform, parent.position, Vector3.one * 1.3f, seam, "Lamp", PrimitiveType.Sphere).transform.localPosition = Vector3.zero;
        // THE BEAM IS LIGHT (2026-10-25, user: "actual light beams instead of translucent things"): an additive crossed
        // sheet, over white so the bloom takes it, fading along its length, and a spot light down the same line, so the
        // smog (lit since the same day) and whatever the beam crosses are lit by it
        var b = MenuCity.Beam(pivot.transform, length, width * 1.6f, SweepBeam);
        b.localRotation = Quaternion.Euler(down, 0f, 0f);
        var spotGo = new GameObject("Spot");
        spotGo.transform.SetParent(pivot.transform, false);
        spotGo.transform.localRotation = Quaternion.Euler(down, 0f, 0f);
        var spot = spotGo.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.range = length;
        spot.spotAngle = Mathf.Max(7f, 2f * Mathf.Atan2(width * 0.5f, length) * Mathf.Rad2Deg + 4f);
        spot.innerSpotAngle = spot.spotAngle * 0.45f;
        spot.intensity = 12f;
        spot.color = new Color(1f, 0.96f, 0.86f);
        spot.shadows = LightShadows.None;
        if (life != null) life.AddSpinner(pivot.transform, degPerSec, Vector3.up, phase);
    }

    /// A MAIL PLANE crossing the sky, high and not slow: a dark fuselage and wing, a RED lamp on the port wingtip and a
    /// CREAM one on the starboard (never green here), the red strobe on its back. From `from` to `to`, and round again
    public static void Aircraft(Transform root, CityLife life, Vector3 from, Vector3 to, float speed, float phase)
    {
        var body = PlaneBody(root, from, out Renderer strobe);
        life.AddAircraft(body, from, to, speed, phase, strobe);
    }

    /// The same on a closed path
    public static void Aircraft(Transform root, CityLife life, Vector3[] path, float speed, float phase)
    {
        if (path == null || path.Length < 2) return;
        var body = PlaneBody(root, path[0], out Renderer strobe);
        life.AddPath(body, path, speed, phase);
        life.AddAircraft(null, Vector3.zero, Vector3.zero, 0f, 0f, strobe);
    }

    static Transform PlaneBody(Transform root, Vector3 at, out Renderer strobe)
    {
        // An empty at unit scale carries the pieces: a child of a stretched box would be stretched with it
        var plane = new GameObject("Aircraft");
        plane.transform.SetParent(root);
        plane.transform.position = at;
        LevelKit.Prim(plane.transform, at, new Vector3(1.7f, 1.7f, 12f), mass, "Fuselage").transform.localPosition = Vector3.zero;
        LevelKit.Prim(plane.transform, at, new Vector3(17f, 0.35f, 2.8f), mass, "Wing").transform.localPosition = new Vector3(0f, -0.2f, 1.2f);
        LevelKit.Prim(plane.transform, at, new Vector3(6f, 0.25f, 1.6f), mass, "Tailplane").transform.localPosition = new Vector3(0f, 0.3f, -5f);
        LevelKit.Prim(plane.transform, at, new Vector3(0.25f, 2.2f, 2f), mass, "Fin").transform.localPosition = new Vector3(0f, 1.3f, -5f);
        LevelKit.Prim(plane.transform, at, Vector3.one * 0.7f, beaconRed, "PortLamp", PrimitiveType.Sphere).transform.localPosition = new Vector3(-8.4f, -0.2f, 1.2f);
        LevelKit.Prim(plane.transform, at, Vector3.one * 0.7f, seam, "StarboardLamp", PrimitiveType.Sphere).transform.localPosition = new Vector3(8.4f, -0.2f, 1.2f);
        var top = LevelKit.Prim(plane.transform, at, Vector3.one * 0.9f, beaconRed, "Strobe", PrimitiveType.Sphere);
        top.transform.localPosition = new Vector3(0f, 1f, 0f);
        strobe = top.GetComponent<Renderer>();
        return plane.transform;
    }

    /// The city beyond: masses on an arc about `center` at `baseY` — one mesh, one draw. With `sheets` (a ring near
    /// enough that a window can be told from a wall: anything under two and a half kilometres of where it is seen
    /// from) every mass is a BUILDING in the plan's own stone and light, MenuCity.FarRing's low masonry with a few
    /// setback shafts and the odd crowned giant among it. Without, the horizon's plain stepped masses, all but lost
    /// in the haze: every one steps back once; a few of them are giants, twice the height, a stepped crown with a
    /// lit lip. The giants stop well short of the House: it stays the biggest thing in any frame
    public static void Skyline(MeshBaker b, Vector3 center, float baseY, int count, float rMin, float rMax, float fromDeg, float toDeg, float heightScale = 1f, SheetBaker sheets = null)
    {
        if (sheets != null) { MenuCity.FarRing(b, sheets, center, baseY, count, rMin, rMax, fromDeg, toDeg, heightScale, true); return; }
        for (int i = 0; i < count; i++)
        {
            float a = Mathf.Lerp(fromDeg, toDeg, i / (float)Mathf.Max(1, count - 1)) * Mathf.Deg2Rad;
            float r = Random.Range(rMin, rMax);
            bool mega = i % 12 == 5;
            float h = (mega ? Random.Range(170f, 250f) : Random.Range(30f, 140f)) * heightScale;
            float w = (mega ? Random.Range(50f, 70f) : Random.Range(26f, 60f)) * Mathf.Sqrt(heightScale);
            Vector3 p = new Vector3(center.x + Mathf.Cos(a) * r, baseY + h * 0.4f, center.z + Mathf.Sin(a) * r);
            b.Add(massFar, p, new Vector3(w, h * 0.8f, w));
            b.Add(massFar, new Vector3(p.x, baseY + h * 0.9f, p.z), new Vector3(w * 0.7f, h * 0.2f, w * 0.7f));   // the setback
            if (mega)
            {
                b.Add(massFar, new Vector3(p.x, baseY + h + 14f, p.z), new Vector3(w * 0.4f, 28f, w * 0.4f));
                b.Add(massFar, new Vector3(p.x, baseY + h + 36f, p.z), new Vector3(w * 0.2f, 16f, w * 0.2f));
                b.Add(seamDim, new Vector3(p.x, baseY + h + 0.4f, p.z), new Vector3(w * 0.7f + 1f, 0.8f, w * 0.7f + 1f));
                b.Add(seamDim, new Vector3(p.x, baseY + h + 28.4f, p.z), new Vector3(w * 0.4f + 1f, 0.8f, w * 0.4f + 1f));
            }
        }
    }

    /// A see-through sheet: the haze that lies between the towers below the level
    public static Material HazeSheet(Color c)
    {
        var m = LevelKit.FromTemplate("UnlitTransparent", "Universal Render Pipeline/Unlit");
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.SetInt("_Cull", (int)CullMode.Off);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        return m;
    }

    // ---------------------------------------------------------------- terraces, bridges, cranes, the air

    /// A terrace of green on a roof or podium: dark foliage in mounds, a hedge line, lamps among them
    public static void Terrace(MeshBaker b, Vector3 at, float y, Vector2 size)
    {
        int mounds = Mathf.Clamp(Mathf.RoundToInt(size.x * size.y / 120f), 3, 14);
        for (int i = 0; i < mounds; i++)
        {
            float r = Random.Range(2.2f, 4.5f);
            Vector3 p = new Vector3(at.x + Random.Range(-0.42f, 0.42f) * size.x, y + r * 0.35f, at.z + Random.Range(-0.42f, 0.42f) * size.y);
            b.Add(foliage, p, new Vector3(r * Random.Range(1f, 1.5f), r, r * Random.Range(1f, 1.5f)), Quaternion.identity, PrimitiveType.Sphere);
            if (i % 3 == 0) b.Add(lampWarm, p + new Vector3(r * 0.6f, 1.6f, 0f), new Vector3(0.3f, 0.3f, 0.3f));
        }
        b.Add(foliage, new Vector3(at.x, y + 0.5f, at.z - size.y * 0.45f), new Vector3(size.x * 0.9f, 1f, 0.9f)); // the hedge along the edge
        b.Add(seamDim, new Vector3(at.x, y + 0.08f, at.z - size.y * 0.47f), new Vector3(size.x * 0.92f, 0.08f, 0.12f)); // and its light
    }

    /// A skybridge between two towers: a deck with lit rails
    public static void Bridge(MeshBaker b, Vector3 a, Vector3 c, float width)
    {
        Vector3 d = c - a;
        float len = d.magnitude;
        if (len < 4f) return;
        Vector3 dir = d / len, mid = (a + c) * 0.5f;
        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
        Vector3 side = Vector3.Cross(dir, Vector3.up).normalized;
        b.Add(blackGloss, mid, new Vector3(width, 1.6f, len), rot);
        b.Add(seamDim, mid + side * (width * 0.5f) + Vector3.up * 1.3f, new Vector3(0.12f, 1f, len), rot);
        b.Add(seamDim, mid - side * (width * 0.5f) + Vector3.up * 1.3f, new Vector3(0.12f, 1f, len), rot);
    }

    /// A construction crane on a roof: the mast baked, the jib turning
    public static void Crane(Transform root, MeshBaker b, CityLife life, Vector3 at, float height)
    {
        b.Add(girder, at + Vector3.up * (height * 0.5f), new Vector3(1.6f, height, 1.6f));
        var jib = new GameObject("Jib");
        jib.transform.SetParent(root);
        jib.transform.position = at + Vector3.up * height;
        LevelKit.Prim(jib.transform, jib.transform.position, new Vector3(1.2f, 1.2f, 34f), girder, "Arm").transform.localPosition = new Vector3(0f, 0f, 12f);
        LevelKit.Prim(jib.transform, jib.transform.position, new Vector3(2.4f, 2.4f, 4f), mass, "Counterweight").transform.localPosition = new Vector3(0f, -0.6f, -5f);
        LevelKit.Prim(jib.transform, jib.transform.position, new Vector3(0.12f, 12f, 0.12f), blackGloss, "Hook").transform.localPosition = new Vector3(0f, -6f, 22f);
        var strobe = LevelKit.Prim(jib.transform, jib.transform.position, Vector3.one * 0.7f, beaconRed, "Strobe", PrimitiveType.Sphere);
        strobe.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        life.AddSpinner(jib.transform, Random.Range(1.5f, 3f), Vector3.up, Random.Range(0f, 360f));
        life.AddAircraft(null, Vector3.zero, Vector3.zero, 0f, 0f, strobe.GetComponent<Renderer>()); // the strobe only
    }

    /// A searchlight: a pale beam sweeping the city from a roof, slow and patient. Lively from a window, and the
    /// first thing in the skyline that says somebody is looking
    public static void Searchlight(Transform root, CityLife life, Vector3 at, float length, float tilt, float degPerSec, float phase)
    {
        var pivot = new GameObject("Searchlight");
        pivot.transform.SetParent(root);
        pivot.transform.position = at;
        var b = LevelKit.Prim(pivot.transform, at, new Vector3(4.5f, 4.5f, length), beam, "Beam");
        b.transform.localRotation = Quaternion.Euler(tilt, 0f, 0f);
        b.transform.localPosition = b.transform.localRotation * new Vector3(0f, 0f, length * 0.5f);
        b.GetComponent<Renderer>().receiveShadows = false;
        LevelKit.Prim(pivot.transform, at, Vector3.one * 2.4f, seam, "Lamp", PrimitiveType.Sphere).transform.localPosition = Vector3.zero;
        life.AddSpinner(pivot.transform, degPerSec, Vector3.up, phase);
    }

    // ---------------------------------------------------------------- the police blimp and the cones (2026-10-16)

    static Mesh coneMesh;
    static Material coneBeam;

    /// A CONE: its apex at the origin, its base a unit circle at z = 1, open, so scaled (r, r, length) it is a
    /// searchlight's beam. UVs run v along it (0 at the apex), so a wash that is solid at v = 0 and gone at v = 1
    /// fades the beam out along its length; drawn from both sides (the material culls nothing) the near and far
    /// walls add up to a brighter core
    public static Mesh ConeMesh()
    {
        if (coneMesh != null) return coneMesh;
        const int Seg = 20;
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        for (int i = 0; i <= Seg; i++)
        {
            float a = i * Mathf.PI * 2f / Seg;
            verts.Add(Vector3.zero); uvs.Add(new Vector2(i / (float)Seg, 0f));
            verts.Add(new Vector3(Mathf.Cos(a), Mathf.Sin(a), 1f)); uvs.Add(new Vector2(i / (float)Seg, 1f));
        }
        for (int i = 0; i < Seg; i++)
        {
            int b = i * 2;
            tris.Add(b); tris.Add(b + 1); tris.Add(b + 3);
            tris.Add(b); tris.Add(b + 3); tris.Add(b + 2);
        }
        coneMesh = new Mesh { name = "BeamCone" };
        coneMesh.SetVertices(verts);
        coneMesh.SetUVs(0, uvs);
        coneMesh.SetTriangles(tris, 0);
        coneMesh.RecalculateBounds();
        return coneMesh;
    }

    /// The cones' material: additive, a cold pale wash that fades along the beam, no depth write
    static Material ConeBeam()
    {
        if (coneBeam != null) return coneBeam;
        coneBeam = MenuCity.Sheet(CityTextures.Wash(), new Color(0.72f, 0.8f, 0.95f, 0.11f), (int)RenderQueue.Transparent, true);
        coneBeam.name = "SearchlightCone";
        return coneBeam;
    }

    /// A searchlight's beam as a cone under `parent`, `length` long and `radius` wide at its far end, down +z
    public static Transform Cone(Transform parent, float length, float radius)
    {
        var go = new GameObject("Cone");
        go.transform.SetParent(parent, false);
        go.transform.localScale = new Vector3(radius, radius, length);
        go.AddComponent<MeshFilter>().sharedMesh = ConeMesh();
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = ConeBeam();
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.Off;
        return go.transform;
    }

    /// A SEARCHLIGHT FROM THE GROUND that sweeps: a lamp at `at` and a thin cone `length` long aimed `pitch` degrees
    /// up on bearing `az`, swinging `swing` degrees either way of it at `rate` cycles a second (slow: a twentieth),
    /// nodding a little as it goes (BeamSweep). No Light: it costs a draw
    public static Transform GroundSearchlight(Transform root, Vector3 at, float az, float pitch, float length, float swing, float rate, float phase)
    {
        var pivot = new GameObject("GroundSearchlight");
        pivot.transform.SetParent(root);
        pivot.transform.position = at;
        LevelKit.Prim(pivot.transform, at, Vector3.one * 1.8f, seam, "Lamp", PrimitiveType.Sphere).transform.localPosition = Vector3.zero;
        Cone(pivot.transform, length, length * 0.022f);
        var s = pivot.AddComponent<BeamSweep>();
        s.Init(az, pitch, swing, rate, phase, Unscaled);
        return pivot.transform;
    }

    /// THE POLICE BLIMP: one, on a slow round about `centre` at `radius` and `y`: a FAT envelope (an ellipsoid with a
    /// fuller nose and tail, three spheres), cross fins, a gondola of amber windows slung under it with POLICE on a
    /// lit panel each side, a red lamp on the tail, and a SEARCHLIGHT CONE under the gondola sweeping the roofs
    /// (BlimpSearchlight). Dark: the one dark thing in the air. Far enough out and up that nothing stands in its way
    public static void PoliceBlimp(Transform root, CityLife life, Vector3 centre, float radius, float y)
    {
        const int pts = 24;
        var path = new Vector3[pts + 1];
        for (int i = 0; i <= pts; i++)
        {
            float a = i * 360f / pts * Mathf.Deg2Rad;
            path[i] = new Vector3(centre.x + Mathf.Cos(a) * radius, y + Mathf.Sin(a * 3f) * 6f, centre.z + Mathf.Sin(a) * radius);
        }
        var blimp = new GameObject("PoliceBlimp");
        blimp.transform.SetParent(root);
        blimp.transform.position = path[0];
        LevelKit.Prim(blimp.transform, path[0], new Vector3(26f, 22f, 64f), mass, "Envelope", PrimitiveType.Sphere).transform.localPosition = Vector3.zero;
        LevelKit.Prim(blimp.transform, path[0], new Vector3(21f, 18f, 24f), mass, "Nose", PrimitiveType.Sphere).transform.localPosition = new Vector3(0f, -0.5f, 24f);
        LevelKit.Prim(blimp.transform, path[0], new Vector3(15f, 13f, 20f), mass, "Tail", PrimitiveType.Sphere).transform.localPosition = new Vector3(0f, 0.5f, -27f);
        LevelKit.Prim(blimp.transform, path[0], new Vector3(1f, 16f, 14f), mass, "Fin").transform.localPosition = new Vector3(0f, 0f, -30f);
        LevelKit.Prim(blimp.transform, path[0], new Vector3(18f, 1f, 14f), mass, "Fin").transform.localPosition = new Vector3(0f, 0f, -30f);
        LevelKit.Prim(blimp.transform, path[0], new Vector3(5f, 3.6f, 18f), blackGloss, "Gondola").transform.localPosition = new Vector3(0f, -11.8f, 4f);
        LevelKit.Prim(blimp.transform, path[0], new Vector3(5.2f, 1.1f, 14f), trainWindow, "GondolaWindows").transform.localPosition = new Vector3(0f, -11.4f, 4f);
        for (int s = -1; s <= 1; s += 2)
        {
            LevelKit.Prim(blimp.transform, path[0], new Vector3(0.3f, 4.2f, 24f), adPanel, "Panel").transform.localPosition = new Vector3(s * 13.1f, 0.5f, 2f);
            var tm = LevelKit.Text(blimp.transform, path[0], Quaternion.identity, "POLICE", 0.5f, Ink, "PoliceName", 22f, true, 900f, 1600f);
            tm.transform.localPosition = new Vector3(s * 13.32f, 0.5f, 2f);
            tm.transform.localRotation = Quaternion.LookRotation(new Vector3(-s, 0f, 0f), Vector3.up);
        }
        LevelKit.Prim(blimp.transform, path[0], Vector3.one * 1.4f, beaconRed, "TailLamp", PrimitiveType.Sphere).transform.localPosition = new Vector3(0f, 1f, -37f);
        LevelKit.Prim(blimp.transform, path[0], Vector3.one * 1.2f, seam, "NoseLamp", PrimitiveType.Sphere).transform.localPosition = new Vector3(0f, -9f, 35f);
        // The searchlight: a pivot under the gondola, the cone pitched down, sweeping
        var pivot = new GameObject("BlimpSearchlight");
        pivot.transform.SetParent(blimp.transform, false);
        pivot.transform.localPosition = new Vector3(0f, -13.8f, 6f);
        LevelKit.Prim(pivot.transform, path[0], Vector3.one * 1.6f, seam, "Lamp", PrimitiveType.Sphere).transform.localPosition = Vector3.zero;
        Cone(pivot.transform, 420f, 420f * 0.06f);
        pivot.AddComponent<BeamSweep>().Init(0f, -58f, 38f, 0.04f, Random.value * 6f, Unscaled, true);
        if (life != null)
        {
            life.AddPath(blimp.transform, path, 6f / (2f * Mathf.PI * Mathf.Max(10f, radius)), Random.value);
            LevelKit.Exhaust(blimp.transform, new Vector3(0f, -10f, -8f), Unscaled);
        }
    }

    /// POLICE BLIMPS: a few on a slow circuit round a tower at the height of its windows (`y` is absolute; `centre`
    /// gives the ring its x and z), a small dark envelope with fins, a gondola of amber windows under it, a cream
    /// searchlight walking the roofs below, a red lamp on the tail. The city's eyes. The polite word is safety.
    /// Seven metres a second whatever the ring's size
    public static void Patrol(Transform root, CityLife life, Vector3 centre, float radius, float y, int count)
    {
        const int pts = 16;
        var path = new Vector3[pts + 1];
        for (int i = 0; i <= pts; i++)
        {
            float a = i * 360f / pts * Mathf.Deg2Rad;
            path[i] = new Vector3(centre.x + Mathf.Cos(a) * radius, y + Mathf.Sin(a * 2f) * 4f, centre.z + Mathf.Sin(a) * radius);
        }
        float speed = 7f / (2f * Mathf.PI * Mathf.Max(10f, radius));
        for (int i = 0; i < count; i++)
        {
            var blimp = new GameObject("PatrolBlimp");
            blimp.transform.SetParent(root);
            blimp.transform.position = path[0];
            LevelKit.Prim(blimp.transform, path[0], new Vector3(5f, 4f, 16f), mass, "Envelope", PrimitiveType.Sphere).transform.localPosition = Vector3.zero;
            LevelKit.Prim(blimp.transform, path[0], new Vector3(0.4f, 4f, 4f), mass, "Fin").transform.localPosition = new Vector3(0f, 0f, -6.5f);
            LevelKit.Prim(blimp.transform, path[0], new Vector3(4.6f, 0.4f, 4f), mass, "Fin").transform.localPosition = new Vector3(0f, 0f, -6.5f);
            LevelKit.Prim(blimp.transform, path[0], new Vector3(1.4f, 1f, 4f), blackGloss, "Gondola").transform.localPosition = new Vector3(0f, -2.3f, 0.5f);
            LevelKit.Prim(blimp.transform, path[0], new Vector3(1.5f, 0.4f, 3.4f), trainWindow, "GondolaWindows").transform.localPosition = new Vector3(0f, -2.2f, 0.5f);
            LevelKit.Prim(blimp.transform, path[0], Vector3.one * 0.5f, beaconRed, "TailLamp", PrimitiveType.Sphere).transform.localPosition = new Vector3(0f, 0.4f, -8f);
            Sweep(blimp.transform, new Vector3(0f, -2.9f, 1.5f), 130f, 2.4f, 50f, Random.Range(8f, 14f) * (i % 2 == 0 ? 1f : -1f), Random.Range(0f, 360f), life);
            life.AddPath(blimp.transform, path, speed, i / (float)count);
        }
    }

    /// A flock: a loose V of birds on a lazy loop over the roofs — the one thing in the sky the company did not build
    public static void Flock(Transform root, CityLife life, Vector3 centre, float rx, float rz, float y, int birds)
    {
        const int pts = 12;
        var path = new Vector3[pts + 1];
        for (int i = 0; i <= pts; i++)
        {
            float a = i * 360f / pts * Mathf.Deg2Rad;
            path[i] = new Vector3(centre.x + Mathf.Cos(a) * rx, y + Mathf.Sin(a * 3f) * 6f, centre.z + Mathf.Sin(a) * rz);
        }
        var flock = new GameObject("Flock");
        flock.transform.SetParent(root);
        flock.transform.position = path[0];
        for (int i = 0; i < birds; i++)
        {
            int row = (i + 1) / 2;
            float wing = i % 2 == 0 ? 1f : -1f;
            var bird = LevelKit.Prim(flock.transform, path[0], new Vector3(1.4f, 0.12f, 0.7f), black, "Bird");
            bird.transform.localPosition = new Vector3(wing * row * 2.2f + Random.Range(-0.4f, 0.4f), Random.Range(-0.8f, 0.8f), -row * 2.6f);
        }
        life.AddPath(flock.transform, path, 0.01f, Random.value);
    }

    /// PIGEONS ON A ROOST (rooftops alive, 2026-10-12): `count` birds perched along `along` about `at`, within
    /// `spread` of it, who STARTLE when the player comes within six metres — up and round the roost in a loose ring,
    /// then back to the perch one by one (Roost). Perched they cost a distance check; nothing else
    public static Roost Roost(Transform root, Vector3 at, Vector3 along, float spread, int count, int seed)
    {
        var go = new GameObject("Roost");
        go.transform.SetParent(root, false);
        go.transform.position = at;
        var r = go.AddComponent<Roost>();
        r.Init(along, spread, count, seed, black != null ? black : LevelKit.Charcoal);
        return r;
    }

    static Material Lit(Shader shader, Color c, float smoothness)
    {
        var m = new Material(shader);
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smoothness);
        return m;
    }

    static Material Unlit(Shader shader, Color c)
    {
        var m = new Material(shader);
        m.SetColor("_BaseColor", c);
        return m;
    }
}

/// <summary>
/// A searchlight that SWEEPS (CityKit.GroundSearchlight, the police blimp's cone): the pivot it sits on swings
/// `swing` degrees either side of its bearing `az` at `rate` cycles a second and nods a few degrees about its
/// `pitch` (degrees up; negative is down, the blimp's), slow and patient. `local` sweeps about the parent's own
/// axes (under a blimp that turns with it); otherwise about the world's. No allocation, one transform a frame
/// </summary>
public class BeamSweep : MonoBehaviour
{
    float az, pitch, swing, rate, phase;
    bool unscaled, local;

    public void Init(float bearing, float pitchDeg, float swingDeg, float cyclesPerSec, float phase0, bool unscaledTime, bool localAxes = false)
    {
        az = bearing; pitch = pitchDeg; swing = swingDeg; rate = cyclesPerSec; phase = phase0; unscaled = unscaledTime; local = localAxes;
        Update();
    }

    void Update()
    {
        float t = (unscaled ? Time.unscaledTime : Time.time) * rate * 6.2832f + phase;
        float yaw = az + swing * Mathf.Sin(t);
        float nod = pitch + 5f * Mathf.Sin(t * 0.37f + 1.3f);
        var rot = Quaternion.Euler(-nod, yaw, 0f);
        if (local) transform.localRotation = rot;
        else transform.rotation = rot;
    }
}

/// <summary>
/// A roost of pigeons (CityKit.Roost). Perched, the birds sit on their perches and the component does one distance
/// check a frame; when the player (CityLife.Focus) comes within `startleRange` they go up in a stagger, circle the
/// roost in a loose rising ring for `flightTime` seconds or so each, flapping, and settle back where they were. Then
/// they can be startled again. Slate birds, no sound (the clip, if one is recorded, goes on this object).
/// </summary>
public class Roost : MonoBehaviour
{
    public float startleRange = 6f, flightTime = 9f;
    Transform[] birds;
    Vector3[] perch;
    Quaternion[] perchRot;
    float[] phase;
    float startledAt = -100f;
    bool flying;

    public void Init(Vector3 along, float spread, int count, int seed, Material mat)
    {
        along = along.sqrMagnitude > 1e-4f ? along.normalized : Vector3.right;
        birds = new Transform[count];
        perch = new Vector3[count];
        perchRot = new Quaternion[count];
        phase = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? i / (count - 1f) : 0.5f;
            float h1 = CityTextures.Hash01(seed, i, 71), h2 = CityTextures.Hash01(seed, i, 72);
            perch[i] = transform.position + along * (spread * (t * 2f - 1f)) + Vector3.up * 0.07f + Vector3.Cross(along, Vector3.up) * ((h1 - 0.5f) * 0.2f);
            perchRot[i] = Quaternion.LookRotation(Quaternion.Euler(0f, (h2 - 0.5f) * 120f, 0f) * Vector3.Cross(along, Vector3.up), Vector3.up);
            phase[i] = h1;
            var bird = LevelKit.Prim(transform, perch[i], new Vector3(0.24f, 0.13f, 0.32f), mat, "Pigeon");
            bird.transform.rotation = perchRot[i];
            LevelKit.Prim(bird.transform, perch[i], new Vector3(0.45f, 0.8f, 0.3f), mat, "PigeonHead").transform.localPosition = new Vector3(0f, 0.4f, 0.55f);   // in the body's own scale: a ten-centimetre head
            birds[i] = bird.transform;
        }
    }

    void Update()
    {
        var f = CityLife.Focus;
        if (f == null || birds == null) return;
        float t = Time.time;
        if (!flying)
        {
            if (t - startledAt < 4f) return;
            Vector3 d = f.position - transform.position;
            if (d.sqrMagnitude > startleRange * startleRange) return;
            flying = true;
            startledAt = t;
        }
        float age = t - startledAt;
        bool allHome = true;
        for (int i = 0; i < birds.Length; i++)
        {
            if (birds[i] == null) continue;
            float a = age - i * 0.12f;
            if (a < 0f) { allHome = false; continue; }
            float dur = flightTime + phase[i] * 3f;
            if (a >= dur) { birds[i].SetPositionAndRotation(perch[i], perchRot[i]); continue; }
            allHome = false;
            float k = a / dur, env = Mathf.Sin(k * Mathf.PI);   // out, round, and back
            float ang = phase[i] * 6.2832f + a * (1.1f + phase[i] * 0.5f);
            float r = 3f + 12f * env, h = 1.5f + 9f * env;
            Vector3 p = transform.position + new Vector3(Mathf.Cos(ang) * r, h + Mathf.Sin(a * 7f + phase[i]) * 0.3f, Mathf.Sin(ang) * r);
            Vector3 vel = new Vector3(-Mathf.Sin(ang), 0.1f * Mathf.Cos(a * 3f), Mathf.Cos(ang));
            float flap = Mathf.Sin(a * 14f + phase[i] * 6f) * 35f;
            birds[i].SetPositionAndRotation(p, Quaternion.LookRotation(vel, Vector3.up) * Quaternion.Euler(0f, 0f, flap));
        }
        if (allHome) flying = false;
    }
}
