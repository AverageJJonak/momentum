using UnityEngine;
using Kind = CityDistrict.Establishment;

/// <summary>
/// THE WORKED EXAMPLE (district-grammar, 2026-10-12): what a district level is, for the level builders to copy.
/// The Ward and the cloakroom open it on the Cursor chain as every Act I level does; then THE SANATORIUM'S STREETS —
/// a four-by-three district: the DINER, the SPEAKEASY, a bare lot and the TENEMENT on the first row (the tenement's
/// fire escape down the alley), the MARKET, the PLAZA, the ASTOR hotel and the department STORE on the second, the
/// BANK, the REGENT theatre, a PARK and the GARAGE on the third; two alleys down the grid; a checkpoint at every
/// plaza junction named by its streets; the route up Meridian Avenue, along Second Street and up Astor Avenue to
/// the lane out; then the Finish. The recipe a level builder follows:
///   1. LevelSections.Begin(seed, floorY); Ward; Rack (or any Course spaces) — the chain as before.
///   2. var plan = CityDistrict.Plan(seed, blocksX, blocksZ, blockW, blockD, streetW); CityDistrict.Alley(plan, …)
///      to choose the alleys; CityDistrict.Place(plan, bx, bz, kind, rotation, name) for every block that is not
///      open ground (a building's front faces a STREET, never an alley: the bank's portico and the hotel's
///      porte-cochère stand six metres into it).
///   3. c = CityDistrict.Build(root, config, c, plan, zone) — the grid at the cursor, the cursor back open at x = 0.
///   4. CityDistrict.Route(plan, waypoints…) — the main route by junction (gx, gz), south edge to north edge; then
///      CityDistrict.Checkpoints(root, plan) (one per plaza junction), or CityDistrict.Checkpoint(root, plan, name,
///      bounds) for a lobby or a court by hand.
///   5. More Course spaces or LevelSections.Finish. CityDistrict.Lint runs with LevelLint; read its line in the console.
/// </summary>
public static class Level0DistrictDemo
{
    public static Vector3 Build(MovementConfig config)
    {
        LevelKit.Init();
        return Build(new GameObject("Level0District").transform, config);
    }

    public static Vector3 Build(Transform root, MovementConfig config)
    {
        var c = LevelSections.Begin(41, 14f);
        c = LevelSections.Ward(root, config, LevelSections.Office(c));                 // WARD 7: the opening; the spawn is your pod
        Course.Checkpoint(root, c, "THE STREET DOOR");

        // THE SANATORIUM'S STREETS: four blocks by three, 36 × 30 m, streets 16 m, two alleys
        var plan = CityDistrict.Plan(41, 4, 3, 36f, 30f, 16f);
        plan.alleyW = 5f;
        for (int g = 1; g < 4; g++) CityDistrict.Alley(plan, true, g, g >= 2);          // the alleys between the second and third, third and fourth columns
        for (int g = 1; g < 3; g++) CityDistrict.Alley(plan, false, g, false);           // every cross street a street
        CityDistrict.Place(plan, 0, 0, Kind.Diner, 0, "THE DINER");
        CityDistrict.Place(plan, 1, 0, Kind.Speakeasy, 0, "THE LAUNDRY");                // the coal chute west onto Meridian, the office window east into the alley
        CityDistrict.Place(plan, 2, 0, Kind.Open, 0, "THE LOT");
        CityDistrict.Place(plan, 3, 0, Kind.Tenement, 0, "THE TENEMENT");                // its fire escape west, down the alley
        CityDistrict.Place(plan, 0, 1, Kind.Market, 0, "THE MARKET");
        CityDistrict.Place(plan, 1, 1, Kind.Plaza, 0, "THE PLAZA");
        CityDistrict.Place(plan, 2, 1, Kind.Hotel, 0, "THE ASTOR");                      // its balcony east over the alley
        CityDistrict.Place(plan, 3, 1, Kind.Store, 0, "HALCYON & CO.");
        CityDistrict.Place(plan, 0, 2, Kind.Bank, 2, "THE FIRST HALCYON BANK");          // its portico north, onto Third Street
        CityDistrict.Place(plan, 1, 2, Kind.Theatre, 2, "THE REGENT");                   // its marquee north
        CityDistrict.Place(plan, 2, 2, Kind.Park, 0, "THE PARK");
        CityDistrict.Place(plan, 3, 2, Kind.Garage, 0, "THE GARAGE");
        c = CityDistrict.Build(root, config, c, plan, Zone.Utopia);
        CityDistrict.Route(plan, new Vector2Int(1, 0), new Vector2Int(1, 2), new Vector2Int(4, 2), new Vector2Int(4, 3));
        CityDistrict.Checkpoints(root, plan);

        c = Course.Hall(root, config, c, 60f, 10f, 10f, 0, false);                      // THE DISCHARGE HALL: the way out
        LevelSections.Finish(root, config, c);
        return LevelSections.Last.spawn;
    }
}
