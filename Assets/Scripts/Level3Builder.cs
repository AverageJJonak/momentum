using UnityEngine;
using Kind = CityDistrict.Establishment;

/// <summary>
/// LEVEL 3 — BELOW STAIRS, THROUGH THE CITY (district-grammar, 2026-10-16). The opening as it was: THE SERVANTS' HALL
/// upstairs (the spawn, y 20, the last cream you see), THE AREA STEPS drop you into the brick, THE COAL CHUTE (−20)
/// delivers you DOWN into THE SERVICE DISTRICT — the back of the city, its lower street level: a four-by-three grid
/// of blocks at −12 with only the boundary streets wide and every inner gap a five-metre ALLEY (fire escapes,
/// dumpsters, washing lines: the tech lines). The south row: the CABMEN'S SHELTER (a diner turned onto the west mews,
/// its kitchen door in the alley), THE MEWS tenement (its fire escape down that alley, its roof over to the laundry's),
/// THE STEAM LAUNDRY (mangles, presses, vats, the drying racks, the chute from its roof) and THE COAL HOLE (a speakeasy
/// behind a laundry front, its coal chute in the alley). The middle row, alleys on every side: THE PUBLIC BATHS, THE
/// LOWER MARKET, THE ASTOR'S YARD (loading docks, the goods hoist, the vans, the cellar ramp) and THE GOODS SIDING.
/// The north row, fronting the upper street: THE MOTOR GARAGE (its deck over the alley at the kitchens' coal hatch),
/// THE ASTOR'S KITCHENS (the pass, the ranges, the cold store, the pantry, the dumb waiter, the scullery window facing
/// the hotel's balcony, the boiler room's steam vent), THE ASTOR'S BACK (the hotel turned so its service door faces
/// the yard) and THE PORTERS' LODGINGS (its fire escape in the alley behind). Then THE LAUNDRY HOIST (+16), THE LEADS
/// under the sky and THE WASHROOM where the Janitor is waiting on the line. ≈ 670 m by the authored route.
/// </summary>
public static class Level3Builder
{
    /// THE SCORE (2026-10-27): the level is GauntletLevels' line for it; what follows is the old build, kept for reference
    public static Vector3 Build(MovementConfig config) => GauntletLevels.Build(LevelKind.Level3, config);

    public static Vector3 BuildBefore(MovementConfig config)
    {
        LevelKit.Init();
        Transform root = new GameObject("Level3").transform;
        var c = LevelSections.Begin(33, 20f);
        c = Course.Hall(root, config, c, 70f, 12f, 12f);                               // THE SERVANTS' HALL, y 20: the spawn, two galleries
        Course.Checkpoint(root, c, "THE AREA STEPS");
        c = Course.DropIn(root, config, LevelSections.Under(c), 12f, 10f);             // −12 off the edge onto the slide; the brick begins
        c = Course.Chute(root, config, c, 20f, 70f, 9f);                               // THE COAL CHUTE: −20, fifty-two metres of slide
        Course.Checkpoint(root, c, "THE COAL CELLARS");

        // THE SERVICE DISTRICT: four blocks by three, 34 × 36 m, the ring of streets 16 m, every inner gap an alley
        var plan = CityDistrict.Plan(33, 4, 3, 34f, 36f, 16f);
        plan.alleyW = 5f;
        for (int g = 1; g < 4; g++) CityDistrict.Alley(plan, true, g, true);
        for (int g = 1; g < 3; g++) CityDistrict.Alley(plan, false, g, true);
        plan.streetsX[0] = "LOWER WARD STREET"; plan.streetsX[3] = "LOWER THIRD STREET";
        plan.streetsZ[0] = "REGENT MEWS"; plan.streetsZ[4] = "ASTOR MEWS";
        CityDistrict.Place(plan, 0, 0, Kind.Diner, 1, "THE CABMEN'S SHELTER");          // its window on the mews, its kitchen door east into the alley
        CityDistrict.Place(plan, 1, 0, Kind.Tenement, 0, "THE MEWS");                   // its fire escape west, down the same alley; its roof over to the laundry's
        CityDistrict.Place(plan, 2, 0, Kind.Laundry, 0, "THE STEAM LAUNDRY");           // the chute's mouth on its roof, west; the vat-room window east
        CityDistrict.Place(plan, 3, 0, Kind.Speakeasy, 0, "THE COAL HOLE");             // the coal chute west into the alley, the office window on the mews
        CityDistrict.Place(plan, 0, 1, Kind.Bathhouse, 1, "THE PUBLIC BATHS");
        CityDistrict.Place(plan, 1, 1, Kind.Market, 0, "THE LOWER MARKET");
        CityDistrict.Place(plan, 2, 1, Kind.ServiceYard, 0, "THE ASTOR'S YARD");         // its docks north (the hotel's service door) and east
        CityDistrict.Place(plan, 3, 1, Kind.RailYard, 0, "THE GOODS SIDING");
        CityDistrict.Place(plan, 0, 2, Kind.Garage, 0, "THE MOTOR GARAGE");             // its deck east, over the alley at the coal hatch
        CityDistrict.Place(plan, 1, 2, Kind.Kitchen, 2, "THE ASTOR'S KITCHENS");        // the coal hatch west, the scullery window east at the hotel's balcony
        CityDistrict.Place(plan, 2, 2, Kind.Hotel, 2, "THE ASTOR'S BACK");               // its service door south onto the yard, its balcony west over the alley
        CityDistrict.Place(plan, 3, 2, Kind.Tenement, 3, "THE PORTERS' LODGINGS");       // its fire escape south into the alley, its yard door west
        c = CityDistrict.Build(root, config, c, plan, Zone.Utopia);
        // The route: up the fire-escape alley past the cabmen's kitchen door, between the baths and the market, along
        // the kitchens' back and the yard's dock to the Astor's service door, up the porters' alley, west to the hoist
        CityDistrict.Route(plan, new Vector2Int(2, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2),
            new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(3, 3), new Vector2Int(2, 3));
        CityDistrict.Checkpoints(root, plan);                                            // the four corners, named by their streets
        var alley = plan.JunctionAt(1, 1);
        CityDistrict.Checkpoint(root, "THE BACK ALLEY", alley.volume, alley.forward);
        CityDistrict.Checkpoint(root, plan, "THE LOWER MARKET", plan.BlockAt(1, 1).bounds);
        CityDistrict.Checkpoint(root, plan, "THE STEAM LAUNDRY", plan.BlockAt(2, 0).bounds);
        CityDistrict.Checkpoint(root, plan, "THE ASTOR'S YARD", plan.BlockAt(2, 1).bounds);
        CityDistrict.Checkpoint(root, plan, "THE KITCHENS", plan.BlockAt(1, 2).bounds);
        var back = plan.JunctionAt(3, 2);
        CityDistrict.Checkpoint(root, "THE ASTOR'S BACK", back.volume, back.forward);
        // The postcard: up the fire-escape alley from the south street — the Mews' landings, the washing across it
        float ax = plan.GapMid(true, 1), y = plan.floorY, z0 = plan.z0;
        LevelSections.Postcard(new Vector3(ax + 1.4f, y + 2.4f, z0 + 9f), new Vector3(ax - 0.6f, y + 9f, z0 + 44f), 68f, true);

        Course.Checkpoint(root, c, "THE LAUNDRY HOIST");
        c = Course.Shaft(root, config, LevelSections.Under(c), 16f, 6f);               // THE LAUNDRY HOIST: +16, the brick again
        Course.Checkpoint(root, c, "THE LEADS");
        c = Course.Roof(root, config, c, 90f, 10f, 2);                                 // THE LEADS: two gaps under the sky over the laundry's roofs
        Course.Checkpoint(root, c, "THE WASHROOM");
        var wash = c;
        c = Course.Hall(root, config, c, 80f, 12f, 12f);                               // THE WASHROOM: the last hall
        Course.Fuel(root, config, wash, EnemyKind.Janitor, new Vector3(0f, wash.floorY, wash.z + 65f), "washroom floor");   // the Janitor, on the line
        LevelSections.Finish(root, config, c);
        return LevelSections.Last.spawn;
    }
}
