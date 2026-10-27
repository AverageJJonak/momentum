using UnityEngine;

/// <summary>
/// BEYOND THE CORDON: the city round Level 11, the rooftops north, the House, and what is in the air. (Level11Builder partial)
/// </summary>
public static partial class Level11Builder
{
    /// The city you cannot reach, in every direction: the working city's fabric right up to the cordon, lit where it
    /// is near enough to read, the elevated line going on both ways with its traffic, the rooftops north to the House,
    /// the House, the skyline round all of it, and what is in the air — the police blimps over the cut and the house,
    /// the searchlights on the cordon's corners. Welded per 600 m square, no colliders, no shadows.
    static void Beyond()
    {
        var far = SurfaceKit.Far;
        var farSheets = new SheetBaker();
        Vector3 house = new Vector3(0f, 0f, 9000f);
        MenuCity.Init(1f, false, house);   // the plan's own towers for everything beyond the cordon (HalcyonCity raises the house and calls Init again for itself)
        for (float gx = -2100f; gx <= 2100f; gx += 140f)
            for (float gz = -1300f; gz <= 3400f; gz += 140f)
            {
                float x = gx + Random.Range(-34f, 34f), z = gz + Random.Range(-34f, 34f);
                if (x > X0 - 60f && x < X1 + 60f && z > Z0 - 60f && z < Z1 + 60f) continue;   // the district
                if (Mathf.Abs(x) < 120f && z > Z1) continue;                                 // the way north: the rooftops stand in it
                Vector3 foot = new Vector3(x, 0f, z);
                float r = (foot - Head).magnitude;
                float w = Random.Range(30f, 62f), d = Random.Range(30f, 54f);
                bool near = r < 1050f;
                bool lit = near || z > 600f && Mathf.Abs(x) < 1100f && r < 2700f;
                float h = near ? Random.Range(40f, 160f) : Random.Range(120f, 360f);   // the blocks round the terminus: the city's ordinary masonry; the giants further off
                float bearing = Mathf.Atan2(house.x - x, house.z - z) * Mathf.Rad2Deg;   // every tower faces the house, as the plan's do
                MenuCity.OuterBlock(far, farSheets, foot, w, d, h, bearing, Random.value < 0.06f ? 4 : Random.Range(0, 4), lit, lit && r < 1900f ? (near ? 30f : 70f) : 0f);
            }

        // The elevated line, going on past the cordon both ways
        CityKit.Viaduct(far, new Vector3(-900f, DeckY, DeckZ - 30f), new Vector3(X0 - 3f, DeckY, DeckZ), DeckW, 0f);
        CityKit.Viaduct(far, new Vector3(X1 + 3f, DeckY, DeckZ), new Vector3(900f, DeckY + 6f, DeckZ + 40f), DeckW, 0f);

        // The rooftops north: the chain Chapter 3 crosses, rising the whole way to the house
        Vector3 prev = new Vector3(0f, RoofY, Z1 + 2f);
        float endZ = 9000f - 2700f;   // the plan's own skyline takes over from there
        int n = 0;
        for (float z = 900f; z < endZ; z += Random.Range(130f, 180f), n++)
        {
            float k = Mathf.InverseLerp(900f, endZ, z);
            float x = (n % 2 == 0 ? 1f : -1f) * Random.Range(12f, 70f);
            float h = Mathf.Lerp(RoofY + 6f, 330f, k) + Random.Range(-12f, 22f), w = Random.Range(34f, 58f), d = Random.Range(34f, 52f);
            Vector3 foot = new Vector3(x, 0f, z);
            float roof = MenuCity.OuterTower(far, farSheets, foot, w, d, h, Mathf.Atan2(house.x - x, house.z - z) * Mathf.Rad2Deg, n % 3 == 0 ? 3 : n % 3 == 1 ? 0 : 2, true);
            Vector3 top = foot + Vector3.up * roof;
            float y = n == 0 ? RoofY - 0.6f : Mathf.Min(prev.y, top.y) - 2f;
            CityKit.Bridge(far, new Vector3(prev.x, y, prev.z), new Vector3(top.x, y, top.z), 6f);
            if (n % 2 == 1) CityKit.Terrace(far, foot, top.y + 0.2f, new Vector2(w * 0.8f, d * 0.8f));
            if (life != null && n % 5 == 2) CityKit.Crane(root, far, life, new Vector3(foot.x + w * 0.3f, top.y, foot.z - d * 0.25f), 26f);
            if (life != null && n % 6 == 1) CityKit.Searchlight(root, life, new Vector3(foot.x, top.y + 3f, foot.z), 300f, 12f, Random.Range(4f, 8f), Random.Range(0f, 360f));
            prev = top;
        }

        // The house, nine kilometres off, and its quarter; then city to the end of the world, all the way round
        // In the middle of the city it was drawn for: the menu's plan, whole (HalcyonCity)
        HalcyonCity.Raise(root, house, 1f, false, life, cityBeacons, cityBeaconPhases);
        farSheets.Flush(root, "FarSheets");
        CityKit.Skyline(far, Vector3.zero, 0f, 260, 9800f, 11800f, 14f, 166f, 2.2f);
        CityKit.Skyline(far, new Vector3(0f, 0f, 400f), 0f, 250, 3600f, 6200f, 125f, 415f, 1.5f);   // everywhere but north: nothing stands in front of the house
        LevelKit.Clouds(root, new Vector3(0f, 0f, 4200f), 2600f, 8400f, 380f, 26);

        if (life == null) return;
        CityKit.Airship(root, life, new Vector3(-900f, 260f, 1500f), new Vector3(900f, 280f, 2300f), 0.005f, 0.2f);
        CityKit.Airship(root, life, new Vector3(700f, 300f, 2700f), new Vector3(-700f, 300f, 1100f), 0.004f, 0.7f);
        for (int i = 0; i < 3; i++)
            CityKit.Aircraft(root, life, new Vector3(-1500f + i * 300f, 420f + i * 60f, 1000f + i * 500f), new Vector3(1500f, 440f + i * 60f, 2500f - i * 300f), 0.012f + i * 0.003f, i * 0.33f);
        // The police: a blimp over the cut and one over the house, their searchlights on the corners of their own wall
        CityKit.Patrol(root, life, Cut, CutHalfZ + 40f, 70f, 2);
        CityKit.Patrol(root, life, Goal, 90f, 120f, 2);
        CityKit.Searchlight(root, life, new Vector3(X0, CordonH + 8f, Z0), 420f, 28f, 5f, 40f);
        CityKit.Searchlight(root, life, new Vector3(X1, CordonH + 8f, Z0), 420f, 28f, 6f, 130f);
        CityKit.Searchlight(root, life, new Vector3(X0, CordonH + 8f, Z1), 420f, 28f, 4.5f, 300f);
        CityKit.Searchlight(root, life, new Vector3(X1, CordonH + 8f, Z1), 420f, 28f, 5.5f, 210f);
        // ROOFTOPS ALIVE: the city's traffic on the elevated road past the cordon, both stubs, both ways (CityTraffic:
        // instanced boxes, headlamps and tail lamps, asleep beyond six hundred metres)
        var traffic = CityTraffic.Create(root, false);
        traffic.Avenue(new Vector3(-900f, DeckY + 0.02f, DeckZ - 30f), new Vector3(X0 - 6f, DeckY + 0.02f, DeckZ), 11f, 1.6f, 0f, 1101, DeckW * 0.5f - 0.5f, 0f);
        traffic.Avenue(new Vector3(X1 + 6f, DeckY + 0.02f, DeckZ), new Vector3(900f, DeckY + 6.02f, DeckZ + 40f), 11f, 1.6f, 0f, 1102, DeckW * 0.5f - 0.5f, 0f);
        traffic.Finish();
    }
}
