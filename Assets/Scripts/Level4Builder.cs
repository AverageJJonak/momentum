using UnityEngine;
using Kind = CityDistrict.Establishment;

/// <summary>
/// LEVEL 4 — THE GRAND HOTELS (district-grammar, 2026-10-16). You spawn on WARD STREET under the arch, THE HOTELS'
/// SQUARE ahead; THE GRAND QUARTER is three blocks by three, 44 × 38 m, streets 18 m, one service alley: THE FIRST
/// HALCYON BANK and HALCYON & CO. either side of the square on Ward Street; THE PALM COURT restaurant, THE ASTOR and
/// THE MAJESTIC (turned to face Second Street, its balcony over Concord Avenue facing the Astor's) on the middle row;
/// THE RESIDENCES, THE PARK and THE HOUSE GARAGE on the north row. THE SKYBRIDGE spans Concord Avenue from the
/// Astor's roof garden to the Majestic's, the tram running under it. The route runs up Concord Avenue; the Astor's
/// lobby, its palm court and the bridge are checkpoints of their own. Out the north arch: THE PENTHOUSE LIFT (+12,
/// into the House's own marble), THE PENTHOUSE with the house officer and the Janitor on its floor, THE LONG DROP
/// (−30) and THE PROMENADE to the finish.
/// </summary>
public static class Level4Builder
{
    /// THE SCORE (2026-10-27): the level is GauntletLevels' line for it; what follows is the old build, kept for reference
    public static Vector3 Build(MovementConfig config) => GauntletLevels.Build(LevelKind.Level4, config);

    public static Vector3 BuildBefore(MovementConfig config)
    {
        LevelKit.Init();
        Transform root = new GameObject("Level4").transform;
        var c = LevelSections.Begin(44, 0f);
        Course.Checkpoint(root, c, "WARD STREET");                                      // the spawn: the lane in, the square ahead

        // THE GRAND QUARTER: 3 × 3 blocks, 44 × 38 m, streets 18 m, the service alley between the first two columns
        var plan = CityDistrict.Plan(44, 3, 3, 44f, 38f, 18f);
        plan.alleyW = 4.5f;
        CityDistrict.Alley(plan, true, 1, true);
        CityDistrict.Alley(plan, true, 2, false);
        CityDistrict.Alley(plan, false, 1, false);
        CityDistrict.Alley(plan, false, 2, false);
        CityDistrict.Place(plan, 0, 0, Kind.Bank, 0, "THE FIRST HALCYON BANK");         // its portico south onto Ward Street, its side door west
        CityDistrict.Place(plan, 1, 0, Kind.Plaza, 0, "THE HOTELS' SQUARE");            // the square: the fountain, the kiosk, the Astor across the street
        CityDistrict.Place(plan, 2, 0, Kind.Store, 0, "HALCYON & CO.");                 // its display window south, the loading dock north
        CityDistrict.Place(plan, 0, 1, Kind.Restaurant, 0, "THE PALM COURT");           // its service door and fire escape in the alley, the coal chute west
        CityDistrict.Place(plan, 1, 1, Kind.Hotel, 0, "THE ASTOR");                     // the porte-cochère south onto First Street, the balcony east
        CityDistrict.Place(plan, 2, 1, Kind.Hotel, 2, "THE MAJESTIC");                  // turned: the porte-cochère north onto Second Street, the balcony west
        CityDistrict.Place(plan, 0, 2, Kind.Tenement, 2, "THE RESIDENCES");             // its fire escape east, down the alley
        CityDistrict.Place(plan, 1, 2, Kind.Park, 0, "THE PARK");
        CityDistrict.Place(plan, 2, 2, Kind.Garage, 0, "THE HOUSE GARAGE");
        c = CityDistrict.Build(root, config, c, plan, Zone.Utopia);
        CityDistrict.Route(plan, new Vector2Int(2, 0), new Vector2Int(2, 3));           // up Concord Avenue: the square's east side, between the hotels, under the bridge
        CityDistrict.Checkpoints(root, plan);

        // The hotels' own checkpoints, the skybridge between their roof gardens, the Majestic's own name on its marquee
        var astor = plan.blocks[1, 1]; var majestic = plan.blocks[2, 1];
        Bounds ab = astor.bounds;
        CityDistrict.Checkpoint(root, "THE ASTOR'S LOBBY", new Bounds(new Vector3(ab.center.x, 5f, ab.min.z + 6.5f), new Vector3(ab.size.x - 2f, 10f, 13f)), Vector3.forward, new Vector3(ab.center.x, 0.6f, ab.min.z + 4f));
        CityDistrict.Checkpoint(root, "THE PALM COURT", new Bounds(new Vector3(ab.center.x, 9.5f, ab.center.z + 2.5f), new Vector3(16f, 19f, 16f)), Vector3.forward, new Vector3(ab.center.x, 0.6f, ab.min.z + 15f));
        const float roofGarden = 19f;                                                   // the hotel: walls 18, the deck a metre over them
        LevelKit.Skybridge(root, config, astor, majestic, roofGarden, roofGarden, ab.center.z);
        float bridgeX = (ab.max.x + majestic.bounds.min.x) * 0.5f;
        CityDistrict.Checkpoint(root, "THE SKYBRIDGE", new Bounds(new Vector3(bridgeX, roofGarden + 3.5f, ab.center.z), new Vector3(majestic.bounds.min.x - ab.max.x + 2f, 7f, 6f)), Vector3.right, new Vector3(ab.max.x - 0.5f, roofGarden + 1.55f, ab.center.z));
        Rename(root, majestic.bounds.center + new Vector3(0f, 5.6f, majestic.bounds.extents.z + 5f), "THE ASTOR", "THE MAJESTIC", 10f);
        // The postcard: from the square's south-west corner, the Astor's front left of frame, the bridge and the Majestic beyond
        Bounds sq = plan.blocks[1, 0].bounds;
        LevelSections.Postcard(new Vector3(sq.min.x + 6f, 4f, sq.min.z + 5f), new Vector3(bridgeX - 4f, 14f, ab.center.z - 8f), 78f, true);

        // The tail: out the north arch into the House's own marble
        Course.Checkpoint(root, c, "THE PENTHOUSE LIFT");
        c = Course.Shaft(root, config, LevelSections.Office(c), 12f, 6f);              // +12: three legs, or kick wall to kick wall
        var pent = c;
        c = Course.Hall(root, config, c, 90f, 14f, 14f);                               // THE PENTHOUSE: pillars, two galleries, the chandelier
        Course.Fuel(root, config, pent, EnemyKind.Marshal, new Vector3(4f, pent.floorY, pent.z + 6f), "penthouse landing");   // the house officer
        Course.Fuel(root, config, pent, EnemyKind.Janitor, new Vector3(0f, pent.floorY, pent.z + 75f), "penthouse floor");    // the cleaner, on the line
        Course.Checkpoint(root, c, "THE LONG DROP");
        c = Course.Chute(root, config, c, 30f, 90f, 9f);                               // −30: seventy-two metres of slide
        c = Course.Bank(root, config, c, 70f, 10f, 6f);                                // THE PROMENADE: the chicane home
        LevelSections.Finish(root, config, c);
        return LevelSections.Last.spawn;
    }

    /// A marquee's name, corrected: the hotel builder letters every marquee THE ASTOR; the one nearest `near` that
    /// says `from` is refitted to say `to` (the lettering shrinks to the fascia's width)
    static void Rename(Transform root, Vector3 near, string from, string to, float width)
    {
        TextMesh best = null; float bd = 400f;
        foreach (var tm in root.GetComponentsInChildren<TextMesh>())
        {
            if (tm.name != "MarqueeName" || tm.text != from) continue;
            float d = (tm.transform.position - near).sqrMagnitude;
            if (d < bd) { bd = d; best = tm; }
        }
        if (best == null) return;
        best.text = to;
        LevelKit.Fit(best, 0.6f, width);
    }
}
