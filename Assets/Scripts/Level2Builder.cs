using UnityEngine;
using Kind = CityDistrict.Establishment;

/// <summary>
/// LEVEL 2 — THE THEATRE DISTRICT (district-grammar, 2026-10-16). The BOOKING HALL (the spawn, bare-handed on a fresh
/// run) and the cloakroom as before, then out of the street door into a four-by-three district under THE ELEVATED,
/// an iron railway down Halcyon Avenue (x = 0, over the tram) from the south edge to the north with a station over
/// the theatre square: the DINER, the CAROM ROOMS (billiards, a cellar bar), the PALAIS (the dance hall) and the
/// speakeasy behind it on the first row; the ASTOR, the THEATRE SQUARE, THE REGENT and the players' tenement on the
/// second; the bank, THE RIALTO (the picture palace), the flower market and the department store on the third; one
/// alley down the east side. Every house is a pass-through at two heights or more; the deck is a second route the
/// whole length. Then THE LONG DROP (−30), THE SKYBRIDGE and THE UPPER FOYER home. Utopia throughout.
/// </summary>
public static class Level2Builder
{
    /// THE SCORE (2026-10-27): the level is GauntletLevels' line for it; what follows is the old build, kept for reference
    public static Vector3 Build(MovementConfig config) => GauntletLevels.Build(LevelKind.Level2, config);

    public static Vector3 BuildBefore(MovementConfig config)
    {
        LevelKit.Init();
        Transform root = new GameObject("Level2").transform;
        var c = LevelSections.Begin(22, 0f);
        c = Course.Hall(root, config, c, 80f, 13f, 12f);                               // THE BOOKING HALL: the spawn, the first fight bare-handed
        Course.Checkpoint(root, c, "THE STREET DOOR");

        // THE THEATRE DISTRICT: four blocks by three, 36 × 32 m, streets 16 m, one alley down the east side
        var plan = CityDistrict.Plan(22, 4, 3, 36f, 32f, 16f);
        plan.alleyW = 5f;
        CityDistrict.Alley(plan, true, 1, false);                                      // Regent Avenue
        CityDistrict.Alley(plan, true, 2, false);                                      // Halcyon Avenue: the tram, the elevated
        CityDistrict.Alley(plan, true, 3, true);                                       // the alley behind the Palais and the Regent
        CityDistrict.Alley(plan, false, 1, false);
        CityDistrict.Alley(plan, false, 2, false);
        plan.streetsZ[1] = "REGENT AVENUE";
        CityDistrict.Place(plan, 0, 0, Kind.Diner, 3, "THE INTERVAL");                 // its window east onto Regent Avenue
        CityDistrict.Place(plan, 1, 0, Kind.BilliardHall, 3, "THE CAROM ROOMS");       // its doors east under the elevated, the cellar hatch on First Street
        CityDistrict.Place(plan, 2, 0, Kind.DanceHall, 1, "THE PALAIS");               // its doors west under the elevated, the cloakroom window over the alley
        CityDistrict.Place(plan, 3, 0, Kind.Speakeasy, 0, "THE PROMPT CORNER");        // behind the Palais: the coal chute west into the alley
        CityDistrict.Place(plan, 0, 1, Kind.Hotel, 3, "THE ASTOR");                    // its doors east onto the square, the balcony north
        CityDistrict.Place(plan, 1, 1, Kind.Plaza, 0, "THE THEATRE SQUARE");
        CityDistrict.Place(plan, 2, 1, Kind.Theatre, 1, "THE REGENT");                 // its marquee west under the elevated, the stage door into the alley
        CityDistrict.Place(plan, 3, 1, Kind.Tenement, 0, "THE PLAYERS' LODGINGS");     // its fire escape west, down the alley
        CityDistrict.Place(plan, 0, 2, Kind.Bank, 2, "THE PLAYERS' BANK");             // its portico north onto Third Street
        CityDistrict.Place(plan, 1, 2, Kind.Cinema, 3, "THE RIALTO");                  // its marquee east under the elevated
        CityDistrict.Place(plan, 2, 2, Kind.Market, 0, "THE FLOWER MARKET");
        CityDistrict.Place(plan, 3, 2, Kind.Store, 2, "HALCYON & CO.");                // its doors north onto Third Street
        // The postcard (the thumbnail): from the theatre square, north-east at the station with the Regent's marquee
        // under the deck. Set before Build so it is the signature one (Build's own would stand otherwise). The alley
        // on the east side puts Halcyon Avenue's centre at x = 5.5, not 0 (the grid is centred as a whole)
        float z0 = c.z;
        LevelSections.Postcard(new Vector3(-30f, c.floorY + 9.5f, z0 + 60f), new Vector3(6f, c.floorY + 8.5f, z0 + 102f), 78f, true);
        c = CityDistrict.Build(root, config, c, plan, Zone.Utopia);
        LevelKit.Elevated(root, config, plan, 2, 9f);                                  // THE ELEVATED: down Halcyon Avenue, over the tram
        CityDistrict.Route(plan, new Vector2Int(2, 0), new Vector2Int(2, 1), new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(2, 2), new Vector2Int(2, 3));
        Checkpoints(root, plan);

        // The tail: the big slide, the bridge, the upper foyer home
        Course.Checkpoint(root, c, "THE LONG DROP");
        c = Course.Chute(root, config, c, 30f, 80f, 9f);                               // −30: sixty-two metres of slide, the lip, the glass, the pads
        c = Course.Bridge(root, config, c, 80f, 10f, 6f);                              // THE SKYBRIDGE: the gap, the span over, the slide under
        Course.Checkpoint(root, c, "THE UPPER FOYER");
        c = Course.Hall(root, config, c, 90f, 13f, 12f);                               // THE UPPER FOYER: two galleries, the way out
        LevelSections.Finish(root, config, c);
        return LevelSections.Last.spawn;
    }

    /// One checkpoint per plaza junction, as CityDistrict.Checkpoints lays them, except that on the tram's avenue
    /// the respawn stands you beside the rails (x + 3.2), never on them
    static void Checkpoints(Transform root, CityDistrict.DistrictPlan plan)
    {
        Physics.SyncTransforms();
        foreach (var j in plan.junctions)
        {
            if (!j.plaza) continue;
            float back = Mathf.Max(0f, Mathf.Min(j.volume.size.x, j.volume.size.z) * 0.5f - 3f);
            Vector3 at = j.at - j.forward * back + Vector3.up * 0.6f;
            if (j.gx == 2) at.x += 3.2f;
            CityDistrict.Checkpoint(root, j.name, j.volume, j.forward, at);
        }
    }
}
