using UnityEngine;

/// <summary>
/// The outer city: the plan's own buildings one at a time, for the districts
/// beyond the plan that a cutscene or Chapter 3 looks across (the ending's
/// surface, the district past the cordon). The same skins, the same light,
/// facing the House as the plan's do, so the city is one city all the way out.
/// </summary>
public static partial class MenuCity
{
    /// How many variants there are: `variant` is taken modulo this, so any int is a building
    public const int Variants = 25;

    /// The lit plate a stack's roof carries, seen from the street: a level that lays its own slab on that roof turns it off
    public static bool RoofPlates = true;

    /// One building at `foot`, facing bearing `az`. The ENVELOPES of variants 0, 1, 2, 3 and 5 are what RoofKit builds
    /// its solids and roofs to, and never change: 0 twin slabs (two slabs at ±8 m, 9 m deep, one h and one 0.86 h),
    /// 1 a spire (tiers 0.38/0.24/0.17/0.12/0.09 of h shrinking by 0.8), 2 a rotunda (drums 0.72 h at d and 0.28 h
    /// at 0.7 d, decks at 0.45 h, 0.72 h and h+16), 3 a stack (0.62 h at w×d, 0.38 h at 0.82 w × 0.82 d), 4 one of
    /// the dead ones, 5 a setback mass (tiers 0.3/0.2/0.16/0.13/0.11/0.1 of h shrinking 0.9 in w and 0.78 in d).
    /// The Deco forms: 6 a setback mass with its own steps, 7 a bank, 8 a station, 9 a radio tower, 10 a cathedral,
    /// 11 a tenement block, 12 a powerhouse, 13 a clock tower. The fabric (MenuCityGotham): 14 a brownstone row,
    /// 15 a cornice block, 16 a hotel, 17 a department store, 18 a theatre, 19 a club, 20 a city hall, 21 a railway
    /// terminus, 22 a gasometer; 23 a gothic-crowned shaft, 24 a stack tower with its smokestack pair (2026-10-16).
    /// `lit` puts its light on. Returns the height a bridge can land on.
    public static float OuterTower(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, float az, int variant, bool lit)
    {
        int v = ((variant % Variants) + Variants) % Variants;
        int seed = Mathf.RoundToInt(foot.x * 0.37f + foot.z * 0.11f);
        LastFurniture.Clear();
        recordFurniture = true;
        try
        {
            if (v == 4) { Dead(sheets, foot, w, d, h, az); return h; }
            if (v == 5) { Arcology(boxes, sheets, foot, w, d, h, az); return h; }
            if (v == 6) { Setback(boxes, sheets, foot, w, d, h, az, seed); return h; }
            if (v == 7) { float bh = Mathf.Min(h, 120f); Bank(boxes, sheets, foot, w, d, bh, az, seed); return bh; }
            if (v == 8) { float sh = Mathf.Min(h * 0.5f, 90f); Station(boxes, sheets, foot, Mathf.Min(w, d), sh, az, seed); return sh * 0.5f; }
            if (v == 9) { RadioTower(boxes, sheets, foot, Mathf.Min(h, 200f), seed); return 7.5f; }   // a transmitter mast, never a needle: the plan's own stop at 220
            if (v == 10) { Cathedral(boxes, sheets, foot, w, Mathf.Max(d, w * 1.6f), h, az, seed); return h * 0.42f; }
            if (v == 11) { float th = Mathf.Min(h * 0.4f, 60f); Tenement(boxes, sheets, foot, w, d, th, az, seed); return th; }
            if (v == 12) { float ph = Mathf.Min(h * 0.25f, 44f); Powerhouse(boxes, sheets, foot, w, Mathf.Max(d, w * 1.3f), ph, az, seed); return ph; }
            if (v == 13) { ClockTower(boxes, sheets, foot, Mathf.Min(w, d) * 0.7f, h, az, seed); return h * 0.73f; }
            if (v == 14) return BrownstoneRow(boxes, sheets, foot, Mathf.Max(w, 36f), Mathf.Min(d, 18f), 15f + 5f * H01(seed, 1), az, seed, lit);
            if (v == 15) return CorniceBlock(boxes, sheets, foot, w, d, Mathf.Clamp(h * 0.2f, 20f, 40f), az, seed, lit);
            if (v == 16) return Hotel(boxes, sheets, foot, w, d, Mathf.Clamp(h * 0.22f, 34f, 50f), az, seed, lit);
            if (v == 17) return DepartmentStore(boxes, sheets, foot, Mathf.Max(w, 40f), d, Mathf.Clamp(h * 0.15f, 24f, 36f), az, seed, lit);
            if (v == 18) return Theatre(boxes, sheets, foot, w, Mathf.Max(d, 30f), Mathf.Clamp(h * 0.14f, 22f, 32f), az, seed, lit);
            if (v == 19) return Club(boxes, sheets, foot, Mathf.Min(w, 36f), Mathf.Min(d, 30f), Mathf.Clamp(h * 0.12f, 16f, 26f), az, seed, lit);
            if (v == 20) return CityHall(boxes, sheets, foot, Mathf.Max(w, 80f), Mathf.Max(d, 60f), Mathf.Clamp(h * 0.6f, 100f, 150f), az, seed, lit);
            if (v == 21) return Terminus(boxes, sheets, foot, Mathf.Max(w, 70f), Mathf.Max(d, 40f), Mathf.Clamp(h * 0.4f, 80f, 110f), az, seed, lit);
            if (v == 22) return Gasometer(boxes, sheets, foot, Mathf.Clamp(Mathf.Min(w, d), 40f, 54f), Mathf.Clamp(h * 0.25f, 48f, 62f), seed, lit);
            Skin skin = !lit ? plain : v == 2 ? arcade : (int)(az / 15f) % 2 == 0 ? pier : charcoal;
            if (v == 23) { GothicTower(boxes, sheets, foot, w, d, h, skin, skin == charcoal || skin == plain ? stoneChar : stoneLime, az, seed, out _); return h; }
            if (v == 24) { float sh = Mathf.Clamp(h * 0.5f, 40f, 90f); StackTower(boxes, sheets, foot, w, d, sh, skin, skin == charcoal || skin == plain ? stoneChar : stoneLime, az, seed, out _); return sh; }
            if (v == 0) { Blades(boxes, sheets, foot, w, h, skin, az, lit ? glowWarm : roofSlate); return h; }
            if (v == 1) { Spire(boxes, sheets, foot, w, d, h, skin, az, lit ? glowGold : roofSlate, false); return h; }
            if (v == 2) { Rotunda(boxes, sheets, foot, Mathf.Min(w, d), h, skin, lit ? glowWarm : roofSlate); return h * 0.72f; }
            // A STACK: two tiers under stepped cornices, a water tank and a mast on top
            Material stone = skin == charcoal || skin == plain ? stoneChar : stoneLime;
            Tier(sheets, foot, w, d, h * 0.62f, skin, lit, az, 0, true, false);
            Tier(sheets, foot + Vector3.up * (h * 0.62f), w * 0.82f, d * 0.82f, h * 0.38f, lit ? pier : plain, lit, az, 0, false, true);
            Cornice(boxes, foot + Vector3.up * (h * 0.62f), w, d, az, stone, lit ? glowWarm : null);
            Cornice(boxes, foot + Vector3.up * h, w * 0.82f, d * 0.82f, az, stone, lit ? glowWarm : null);
            if (lit) Gargoyles(boxes, foot + Vector3.up * (h * 0.62f), w, d, az, stone);
            if (lit && RoofPlates) Box(boxes, glowWarm, foot + Vector3.up * (h + 0.3f), new Vector3(w * 0.82f, 0.45f, d * 0.82f + 0.4f), Quaternion.Euler(0f, az, 0f));
            RoofFurniture(boxes, foot + Vector3.up * h, w * 0.82f, d * 0.82f, az, Mathf.RoundToInt(foot.x * 0.4f + foot.z * 0.6f), 2);
            return h;
        }
        finally { recordFurniture = false; }
    }

    // ------------------------------------------------------------ the city beyond the plan

    /// THE CITY BEYOND THE PLAN, AS FLAT CUT-OUTS (2026-10-16): `count` masses on a ring about `centre` (radius
    /// `rMin`..`rMax`, bearings `fromDeg` to `toDeg`, standing on `baseY`). Beyond the second ring nothing is
    /// detailed: every mass is a SILHOUETTE in one flat near-black stone (or the red street's oxblood, RedFabric),
    /// no window, no cornice, cut out against the sky the way a painted backdrop has them: three in four the low
    /// masonry (a block, a parapet step, a water tower or a chimney on a third of the roofs), one in four a setback
    /// shaft of three steps, one in twenty-five a crowned giant of four, never two giants side by side; every one
    /// faces the centre. `heightScale` lifts the shafts and the giants (a farther ring shows over a nearer one);
    /// the low fabric never scales. `lit` is kept for the callers; a cut-out carries no light. CityKit.Skyline
    /// builds the plain masses of the horizon beyond these
    public static void FarRing(MeshBaker boxes, SheetBaker sheets, Vector3 centre, float baseY, int count, float rMin, float rMax, float fromDeg, float toDeg, float heightScale, bool lit)
    {
        int prev = -1;
        Material m = RedFabric ? oxbloodFlat : cutout;
        for (int i = 0; i < count; i++)
        {
            float a = Mathf.Lerp(fromDeg, toDeg, i / (float)Mathf.Max(1, count - 1)) * Mathf.Deg2Rad;
            float r = Random.Range(rMin, rMax);
            Vector3 foot = new Vector3(centre.x + Mathf.Cos(a) * r, baseY, centre.z + Mathf.Sin(a) * r);
            float az = Mathf.Atan2(foot.x - centre.x, foot.z - centre.z) * Mathf.Rad2Deg;   // its bearing: turned by it, its front is outward and its back toward the House
            int seed = Mathf.RoundToInt(foot.x * 0.37f + foot.z * 0.11f);
            float pick = H01(seed, 1);
            int cls = pick < 0.74f ? 0 : pick < 0.96f ? 1 : 2;
            if (cls == 2 && prev == 2) cls = 1;
            prev = cls;
            float hv = H01(seed, 2), wv = H01(seed, 3), dv = H01(seed, 4);
            if (cls == 0) FarBlock(boxes, m, foot, 30f + 44f * wv, 22f + 24f * dv, 14f + 34f * hv, az, seed);
            else if (cls == 1) FarShaft(boxes, m, foot, 30f + 22f * wv, 26f + 16f * dv, (60f + 80f * hv) * Mathf.Sqrt(heightScale), az, seed);
            else FarGiant(boxes, m, foot, 44f + 22f * wv, 36f + 14f * dv, (160f + 90f * hv) * Mathf.Min(heightScale, 1.8f), az, seed);
        }
    }

    /// A block of the far fabric, cut out: a mass, a parapet step on half of them, a water tower on stilts or a
    /// chimney on a third of the roofs
    static void FarBlock(MeshBaker boxes, Material m, Vector3 foot, float w, float d, float h, float az, int seed)
    {
        if (Cut(foot, Mathf.Max(w, d) * 0.5f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        Box(boxes, m, foot + Vector3.up * (h * 0.5f), new Vector3(w, h, d), turn);
        float roof = H01(seed, 6);
        if (roof < 0.5f) Box(boxes, m, foot + Vector3.up * (h + 0.8f), new Vector3(w * 0.7f, 1.6f, d * 0.7f), turn);   // the parapet step
        if (roof < 0.33f)
        {
            // A water tower: a post for the stilts, the tank over it
            Vector3 at = foot + Vector3.up * h + turn * new Vector3(w * 0.3f, 0f, -d * 0.2f);
            Box(boxes, m, at + Vector3.up * 1.8f, new Vector3(1.2f, 3.6f, 1.2f), turn);
            Box(boxes, m, at + Vector3.up * 5f, new Vector3(3.6f, 3.2f, 3.6f), null, PrimitiveType.Cylinder);
        }
        else if (roof < 0.66f) Box(boxes, m, foot + Vector3.up * (h + 1.6f) + turn * new Vector3(-w * 0.3f, 0f, d * 0.25f), new Vector3(1.6f, 3.2f, 1.6f), turn);   // the chimney
    }

    /// A shaft of the far fabric, cut out: three masses stepping in, a mast on the top
    static void FarShaft(MeshBaker boxes, Material m, Vector3 foot, float w, float d, float h, float az, int seed)
    {
        if (Cut(foot, Mathf.Max(w, d) * 0.5f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        float[] part = { 0.5f, 0.26f, 0.24f };
        float y = 0f, cw = w, cd = d;
        for (int t = 0; t < 3; t++)
        {
            float th = h * part[t];
            Box(boxes, m, foot + Vector3.up * (y + th * 0.5f), new Vector3(cw, th, cd), turn);
            y += th;
            cw *= 0.76f; cd *= 0.76f;
        }
        float mast = Mathf.Min(h * 0.12f, 18f);
        Box(boxes, m, foot + Vector3.up * (y + mast * 0.5f), new Vector3(0.8f, mast, 0.8f), turn);
    }

    /// A giant of the far fabric, cut out: four masses stepping in, a stepped crown and a spike
    static void FarGiant(MeshBaker boxes, Material m, Vector3 foot, float w, float d, float h, float az, int seed)
    {
        if (Cut(foot, Mathf.Max(w, d) * 0.5f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        float[] part = { 0.4f, 0.24f, 0.16f, 0.2f };
        float y = 0f, cw = w, cd = d;
        for (int t = 0; t < 4; t++)
        {
            float th = h * part[t];
            Box(boxes, m, foot + Vector3.up * (y + th * 0.5f), new Vector3(cw, th, cd), turn);
            y += th;
            cw *= 0.76f; cd *= 0.76f;
        }
        float cap = h * 0.05f, spike = h * 0.08f;
        Box(boxes, m, foot + Vector3.up * (y + cap * 0.5f), new Vector3(cw * 0.9f, cap, cd * 0.9f), turn);
        Box(boxes, m, foot + Vector3.up * (y + cap + spike * 0.5f), new Vector3(1.4f, spike, 1.4f), turn);
        if (y + cap + spike > 230f) Beacons.Add(foot + Vector3.up * (y + cap + spike + 1f));
    }
}
