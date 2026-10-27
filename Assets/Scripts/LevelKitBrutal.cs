using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE BRUTALIST CITY (2026-10-27, user: "generate the levels exactly like VHOLUME... they are a brutalist movement
/// game", "take actual assets like spot lights and whatever to make the city feel alive"). What the courses stand in:
/// a ground lost in the fog far below, MONOLITHS of raw concrete either side and beyond both ends (thirty to ninety
/// metres on a side, fifty to two hundred and sixty tall, a few turned a little off the grid), BRIDGES of concrete
/// spanning the whole city high over the course, and the LIFE on all of it: searchlights sweeping from the near
/// tops with real spot lights down their beams, beacons blinking on the tallest, lit window bands on the near faces,
/// a tram crossing a bridge, two airships on their rounds, a flock over the roofs. Everything the fog can swallow it
/// does: the vista is cheap and the course reads clearly against it. Replaces CityVista for a brutalist course.
/// </summary>
public static partial class LevelKit
{
    static Material bandLit, vistaBeaconMat;

    /// A real SPOT LIGHT: at `pos`, down `dir`, no shadows (the brutal palette's cool white unless a colour is given)
    public static Light Spot(Transform parent, Vector3 pos, Vector3 dir, float range, float angle, float intensity, Color? colour = null)
    {
        var go = new GameObject("Spot");
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.LookRotation(dir.sqrMagnitude > 0.0001f ? dir : Vector3.down);
        var l = go.AddComponent<Light>();
        l.type = LightType.Spot;
        l.range = range;
        l.spotAngle = angle;
        l.innerSpotAngle = angle * 0.45f;
        l.intensity = intensity;
        l.color = colour ?? new Color(0.92f, 0.94f, 1f);
        l.shadows = LightShadows.None;
        return l;
    }

    /// The vista round a course from `z0` to `z1` whose lane floor is at `floorY` and `halfW` wide
    public static void BrutalVista(Transform root, float z0, float z1, float floorY, float halfW)
    {
        CityVista.Suppressed = true;
        CityKit.Init(false, 1f);   // the airships and the flock draw on the city kit's materials
        var vista = new GameObject("BrutalVista").transform;
        vista.SetParent(root, false);
        var life = vista.gameObject.AddComponent<CityLife>();
        var rng = new System.Random(unchecked(LevelSections.Seed * 31 + 7));
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        float groundY = floorY - 48f, cz = (z0 + z1) * 0.5f, span = Mathf.Max(600f, (z1 - z0) * 0.5f + 400f);

        Prim(vista, new Vector3(0f, groundY - 1f, cz), new Vector3(3400f, 2f, 3400f), Dark, "VistaGround");

        // THE MONOLITHS, in two fields either side of the course and a third beyond its end
        var tops = new List<Vector3>();
        var beacons = new List<Renderer>();
        var beaconPhase = new List<float>();
        bandLit ??= Unlit(Shader.Find("Universal Render Pipeline/Unlit"), new Color(1.7f, 1.55f, 1.2f));
        vistaBeaconMat ??= Unlit(Shader.Find("Universal Render Pipeline/Unlit"), new Color(2.6f, 0.25f, 0.15f));
        for (int i = 0; i < 72; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            bool beyond = i % 9 == 8;
            float dist = R(halfW + 70f, 900f);
            float x = beyond ? R(-500f, 500f) : side * dist;
            float z = beyond ? z1 + R(120f, 700f) : cz + R(-span, span);
            float w = R(30f, 90f), dd = R(30f, 90f), h = R(50f, 170f);
            if (rng.NextDouble() < 0.12) h = R(180f, 260f);
            var mat = i % 3 == 0 ? Wall : i % 3 == 1 ? Dark : Slope;
            var rot = Quaternion.Euler(0f, R(-6f, 6f), 0f);
            Prim(vista, new Vector3(x, groundY + h * 0.5f, z), new Vector3(w, h, dd), mat, "Monolith", PrimitiveType.Cube, rot);
            Vector3 top = new Vector3(x, groundY + h, z);
            tops.Add(top);
            // Window bands on the near faces: strips of lit glass three storeys apart, the life inside the concrete
            if (!beyond && dist < 420f && rng.NextDouble() < 0.65)
            {
                float fx = x - side * (w * 0.5f + 0.15f);
                for (float by = groundY + 12f; by < groundY + h - 6f; by += R(9f, 14f))
                    Prim(vista, new Vector3(fx, by, z), new Vector3(0.3f, 1.1f, dd * R(0.4f, 0.85f)), bandLit, "WindowBand");
            }
            // A beacon on the tall ones: a red lamp that blinks (CityLife), and a point light under it
            if (h > 150f)
            {
                var b = Prim(vista, top + Vector3.up * 1.2f, Vector3.one * 2.2f, vistaBeaconMat, "Beacon", PrimitiveType.Sphere);
                beacons.Add(b.GetComponent<Renderer>());
                beaconPhase.Add(R(0f, 1f));
                var pl = Light(vista, top + Vector3.up * 3f, 60f, 3f);
                pl.color = new Color(1f, 0.25f, 0.15f);
            }
        }
        life.SetBeacons(beacons, beaconPhase);

        // THE BRIDGES: concrete spans across the whole city, high over the course, a tram on the lowest
        float lowest = float.MaxValue, lowestZ = cz;
        for (int i = 0; i < 7; i++)
        {
            float by = floorY + R(55f, 140f), bz = cz + R(-span * 0.85f, span * 0.85f), len = R(500f, 1100f);
            Prim(vista, new Vector3(R(-80f, 80f), by, bz), new Vector3(len, 8f, R(10f, 18f)), i % 2 == 0 ? Wall : Slope, "Bridge");
            if (by < lowest) { lowest = by; lowestZ = bz; }
        }
        if (lowest < float.MaxValue) Tram(vista, new Vector3(-380f, lowest + 4f, lowestZ), new Vector3(380f, lowest + 4f, lowestZ), 16f, 3, 5f, LevelSections.Seed);

        // THE SEARCHLIGHTS: four of the near tops sweep the sky, each a real spot light down its beam
        int lit = 0;
        for (int i = 0; i < tops.Count && lit < 4; i++)
        {
            var t = tops[i];
            if (Mathf.Abs(t.x) > 420f || Mathf.Abs(t.z - cz) > span * 0.7f) continue;
            BrutalSearchlight(vista, life, t, R(4f, 7f) * (i % 2 == 0 ? 1f : -1f), R(0f, 360f));
            lit++;
        }

        // THE AIR: two airships on their rounds over the city, a flock over the near roofs
        CityKit.Airship(vista, life, new Vector3(-700f, floorY + R(90f, 130f), z0 - 200f), new Vector3(700f, floorY + R(90f, 130f), z1 + 200f), 9f, R(0f, 1f));
        CityKit.Airship(vista, life, new Vector3(600f, floorY + R(70f, 110f), z1 + 300f), new Vector3(-600f, floorY + R(70f, 110f), z0 - 100f), 7f, R(0f, 1f));
        CityKit.Flock(vista, life, new Vector3(R(-200f, 200f), 0f, cz), 160f, 220f, floorY + R(30f, 60f), 11);
    }

    /// A SEARCHLIGHT TOWER for the Deco city (2026-10-27, user: "keep the gotham dark deco aesthetic... spot lights and
    /// whatever to make the city feel alive"): a fluted limestone tower `height` tall with a brass cap and a lamp
    /// globe, the searchlight on its crown sweeping the sky with a real spot light down the beam
    public static void SearchlightTower(Transform parent, Vector3 foot, float height, CityLife life, float degPerSec, float phase)
    {
        Slab(parent, foot + Vector3.up * (height * 0.5f), new Vector3(5f, height, 5f), Limestone, "SearchlightTower");
        Slab(parent, foot + Vector3.up * (height - 1.5f), new Vector3(6f, 0.8f, 6f), Brass, "TowerCap");
        for (int i = 0; i < 4; i++)
            Slab(parent, foot + Vector3.up * (height * 0.5f) + Quaternion.Euler(0f, 90f * i, 0f) * new Vector3(2.6f, 0f, 0f), new Vector3(0.4f, height - 4f, 1.2f), Limestone, "TowerFlute");
        var lamp = Light(parent, foot + Vector3.up * (height + 1f), 40f, 2.4f);
        lamp.color = new Color(1f, 0.82f, 0.55f);
        BrutalSearchlight(parent, life, foot + Vector3.up * height, degPerSec, phase);
    }

    /// A searchlight on a top: a lamp, a long additive beam, a spot light the length of it, the pivot turned by the
    /// city's life at `degPerSec`, pitched up a little so it rakes the undersides of whatever spans the sky
    static void BrutalSearchlight(Transform parent, CityLife life, Vector3 at, float degPerSec, float phase)
    {
        var pivot = new GameObject("Searchlight");
        pivot.transform.SetParent(parent);
        pivot.transform.position = at + Vector3.up * 1.5f;
        pivot.transform.rotation = Quaternion.Euler(0f, phase, 0f);
        Prim(pivot.transform, pivot.transform.position, new Vector3(2.4f, 2.4f, 2.4f), Accent, "Housing");
        var head = new GameObject("Head");
        head.transform.SetParent(pivot.transform, false);
        head.transform.localRotation = Quaternion.Euler(-22f, 0f, 0f);   // up, into the fog
        var beamMat = MenuCity.Sheet(CityTextures.Beam(), new Color(1.6f, 1.65f, 1.8f, 0.45f), (int)UnityEngine.Rendering.RenderQueue.Transparent, true);
        MenuCity.Beam(head.transform, 420f, 26f, beamMat);
        var spot = Spot(head.transform, pivot.transform.position, head.transform.forward, 420f, 9f, 18f, new Color(0.95f, 0.97f, 1f));
        spot.transform.SetParent(head.transform, true);
        if (life != null) life.AddSpinner(pivot.transform, degPerSec, Vector3.up, phase);
    }
}
