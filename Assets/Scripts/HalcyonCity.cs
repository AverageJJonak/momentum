using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ONE CITY. The Deco city the menu shows (MenuCity) is the city the whole
/// game is set in, and this raises it whole wherever a level or a cutscene
/// needs it: the House at `origin`, the Court, the terraces, the belt, the
/// rings, the setback masses, the far skyline, and, when a CityLife is given,
/// everything that lives in it. The first act's windows look out on it from
/// among its rings (CityVista); the ending and Chapter 3 see it nine
/// kilometres off with the House at the centre of it (EndingSet,
/// Level11Builder). By day its light is turned down (lightGain), it is sepia
/// and it stands on dry stone (wet = false); the menu alone mirrors it in a
/// wet black street.
/// </summary>
public static class HalcyonCity
{
    /// Raise the plan at `origin`. `keep` (x0, x1, z0, z1, world) is a box nothing is built in: where a level stands.
    /// The tower's crown beacons and the plan's go into `beacons` for the caller's CityLife. Still only when `life` is null.
    public static Transform Raise(Transform parent, Vector3 origin, float lightGain, bool wet, CityLife life,
        List<Renderer> beacons, List<float> bPhase, Vector4? keep = null, bool day = false)
    {
        MenuCity.Init(lightGain, wet, origin, day);
        if (keep.HasValue) MenuCity.Reserve(keep.Value.x, keep.Value.y, keep.Value.z, keep.Value.w);
        var root = new GameObject("HalcyonCity").transform;
        root.SetParent(parent, false);
        var still = new GameObject("CityStill").transform;
        still.SetParent(root, false);
        var flat = new GameObject("CityGround").transform;
        flat.SetParent(root, false);
        var baker = new MeshBaker { CellSize = 600f };   // welded per square: a window pays only for the squares it looks at
        var sheets = new SheetBaker();
        var ground = new SheetBaker();
        ground.Grid(MenuCity.Ground, origin, 6000f, 6000f, 24, 24, 260f);
        CityKit.Tower(still, origin, baker, false, beacons, bPhase, MenuBackdrop.TowerScale);
        MenuCity.BuildStill(baker, sheets, ground);
        // The city beyond the city: two rings of the same masonry, the odd shaft and giant among it, so the skyline
        // never just stops and no plain box stands where a window could be told from a wall (the first act's rooms
        // look straight at these rings by day)
        CityKit.Skyline(baker, origin, 0f, 220, 1400f, 1950f, 0f, 360f, 1.5f, sheets);
        CityKit.Skyline(baker, origin, 0f, 170, 1950f, 2600f, 3f, 363f, 2.2f, sheets);
        baker.Flush(still, "CityMass");
        var cityWindows = Windows(sheets.Flush(still, "CitySheets"));
        ground.Flush(flat, "CityFlat");
        // THE SMOG over the whole plan and its rings, at street, mid and roof height, none of it where the level stands
        if (!CityVista.Preview) LevelKit.CitySmog(root, origin, origin.y, 2700f, keep, false);
        if (life != null)
        {
            var living = new GameObject("CityLife").transform;
            living.SetParent(root, false);
            var flickers = new List<Renderer>();
            var fPeriod = new List<float>(); var fPhase = new List<float>(); var fDuty = new List<float>();
            MenuCity.BuildLife(living, life, flickers, fPeriod, fPhase, fDuty, beacons, bPhase);
            life.SetFlickers(flickers, fPeriod, fPhase, fDuty);
            root.gameObject.AddComponent<SheetScroll>().Init(MenuCity.Scrolling);
            // The windows breathe (the lit sheets' shared materials, one colour each a frame): the city's sparkle after dark
            if (!day && cityWindows.Count > 0) living.gameObject.AddComponent<WindowTwinkle>().Init(cityWindows, false);
            // Smoke off the stacks nearest the level (or the plan's heart), the big ones first; pigeons over the roofs
            Vector3 focus = keep.HasValue ? new Vector3((keep.Value.x + keep.Value.y) * 0.5f, origin.y, (keep.Value.z + keep.Value.w) * 0.5f) : Vector3.zero;
            LevelKit.Plumes(living, focus, 2500f, 40, false);
            for (int i = 0; i < 3; i++)
            {
                float az = 70f + i * 115f + Random.Range(-25f, 25f), r = Random.Range(360f, 620f);
                Vector3 c = origin + new Vector3(Mathf.Sin(az * Mathf.Deg2Rad) * r, 0f, Mathf.Cos(az * Mathf.Deg2Rad) * r);
                CityKit.Flock(living, life, c, Random.Range(70f, 120f), Random.Range(90f, 160f), origin.y + Random.Range(50f, 95f), 7 + i * 2);
            }
        }
        MenuCity.NoReserve();
        return root;
    }

    /// The lit-window materials among a flush of city sheets (every quad of a kind shares one), for the twinkle
    public static List<Material> Windows(List<Renderer> sheets)
    {
        var mats = new List<Material>();
        foreach (var r in sheets)
        {
            var m = r.sharedMaterial;
            if (m != null && m.renderQueue == MenuCity.QWindows && !mats.Contains(m)) mats.Add(m);
        }
        return mats;
    }
}

/// <summary>
/// The city's sky lanes move: one texture scroll per direction. The menu's
/// weather does this for the menu; a level or a cutscene does it here.
/// </summary>
public class SheetScroll : MonoBehaviour
{
    List<Material> mats;

    public void Init(List<Material> m) => mats = m;

    void Update()
    {
        if (mats == null) return;
        float t = Time.time;
        for (int i = 0; i < mats.Count; i++)
            if (mats[i] != null) mats[i].SetTextureOffset("_BaseMap", new Vector2(0f, Mathf.Repeat(-t * (0.052f + 0.009f * i), 1f)));
    }
}
