using UnityEngine;
using Kind = CityDistrict.Establishment;

/// <summary>
/// LEVEL 1's district: THE SANATORIUM'S STREETS. Three blocks by three, 34 × 32 m, streets 16 m, two alleys (the one
/// between the middle and the east column, and the back alley between the second and third rows: they cross at THE
/// BACK ALLEYS). The street door opens onto WARD STREET with THE FORECOURT (the plaza, the fountain: the first open
/// fight) straight across it; THE HYDRO stands behind the forecourt with its doors on FIRST STREET, the tenement
/// across the alley from it (its fire escape the first tech climb; from its roof, the hydro's skylight); the laundry
/// (the speakeasy) on the west of First Street with its office window over HALCYON AVENUE; the diner at the
/// south-west corner, the garage at the south-east; the market, the park and the siding along the back alley; the
/// lane out at x = 0 on THIRD STREET. The route up Halcyon Avenue (the tram's avenue); checkpoints at every plaza
/// junction, and by hand in the forecourt, the hydro and the back alley.
/// </summary>
public static class Level1District
{
    public const int Seed = 11;

    /// The plan: the grid, the alleys, the street names, every block's establishment, its front onto a street
    public static CityDistrict.DistrictPlan Plan()
    {
        var plan = CityDistrict.Plan(Seed, 3, 3, 34f, 32f, 16f);
        plan.alleyW = 4.5f;
        plan.exitHalfW = 9f;
        CityDistrict.Alley(plan, true, 1, false);    // HALCYON AVENUE: the main avenue, the tram
        CityDistrict.Alley(plan, true, 2, true);     // the alley between the hydro and the tenement
        CityDistrict.Alley(plan, false, 1, false);   // FIRST STREET
        CityDistrict.Alley(plan, false, 2, true);    // the back alley behind the hydro, the laundry and the tenement
        plan.streetsZ[0] = "MERIDIAN AVENUE"; plan.streetsZ[1] = "HALCYON AVENUE"; plan.streetsZ[3] = "CROWN AVENUE";
        plan.streetsX[0] = "WARD STREET"; plan.streetsX[1] = "FIRST STREET"; plan.streetsX[3] = "THIRD STREET";
        CityDistrict.Place(plan, 0, 0, Kind.Diner, 0, "THE DINER");                        // its window and door on Ward Street, the side door on Meridian
        CityDistrict.Place(plan, 1, 0, Kind.Plaza, 0, "THE SANATORIUM FORECOURT");         // the fountain, straight across from the street door
        CityDistrict.Place(plan, 2, 0, Kind.Garage, 0, "THE GARAGE");                      // the ramp to its deck: the first height
        CityDistrict.Place(plan, 0, 1, Kind.Speakeasy, 0, "THE LAUNDRY");                  // the door-in-a-door on First Street, the coal chute west onto Meridian, the office window east over Halcyon
        CityDistrict.Place(plan, 1, 1, Kind.Bathhouse, 0, "THE HYDRO");                    // the doors on First Street, the avenue door on Halcyon, the boiler door into the back alley, the window over the alley
        CityDistrict.Place(plan, 2, 1, Kind.Tenement, 0, "THE TENEMENT");                  // its fire escape west, down the alley beside the hydro; its roof over the hydro's
        CityDistrict.Place(plan, 0, 2, Kind.Market, 2, "THE MARKET");
        CityDistrict.Place(plan, 1, 2, Kind.Park, 0, "THE PARK");                          // from the hydro's boiler door across the alley, through the park to the lane out
        CityDistrict.Place(plan, 2, 2, Kind.RailYard, 0, "THE SIDING");
        return plan;
    }

    /// The thumbnail: from high over the Diner's corner, across the forecourt's fountain to the Hydro's marquee with
    /// the tenement's fire escape behind. Claimed as the level's signature BEFORE Build (a signature claim is kept
    /// against every later one, and Build makes its own)
    public static void Postcard(CityDistrict.DistrictPlan plan, LevelSections.Cursor c)
    {
        // THE SHOT (the stage drops the mark to eye height on the floor under it, so a high mark is a street-level lens):
        // on First Street twenty metres from the Hydro's doors, oblique, its marquee and lit front filling the frame, the
        // avenue's lamps and the tram wire off to the left
        float y = c.floorY, z0 = c.z;
        float hydroX = plan.Block0(true, 1) + plan.blockW * 0.5f, hydroZ = z0 + plan.Block0(false, 1);
        LevelSections.Postcard(new Vector3(hydroX + 12f, y + 1.6f, hydroZ - 13f), new Vector3(hydroX - 2f, y + 5.5f, hydroZ + 1f), 68f, true);
    }

    /// The route and the checkpoints, after Build: up Halcyon Avenue from Ward Street to Third Street; a checkpoint at
    /// every plaza junction, and by hand: the forecourt, the hydro, the back alley (each sending you north)
    public static void Checkpoints(Transform root, CityDistrict.DistrictPlan plan)
    {
        CityDistrict.Route(plan, new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(1, 3));
        CityDistrict.Checkpoints(root, plan);
        float y = plan.floorY;
        var fore = plan.BlockAt(1, 0);
        var hydro = plan.BlockAt(1, 1);
        CityDistrict.Checkpoint(root, "THE FORECOURT", fore.bounds, Vector3.forward, new Vector3(fore.bounds.center.x, y + 0.6f, fore.bounds.min.z + 4f));
        CityDistrict.Checkpoint(root, "THE HYDRO", hydro.bounds, Vector3.forward, new Vector3(hydro.bounds.center.x, y + 0.6f, hydro.bounds.min.z + 2.5f));
        float alleyZ = plan.GapMid(false, 2), alleyW = plan.GapW(false, 2);
        var alley = new Bounds(new Vector3(0f, y + 5f, alleyZ), new Vector3(plan.HalfX * 2f, 12f, alleyW + 1f));
        CityDistrict.Checkpoint(root, "THE BACK ALLEY", alley, Vector3.forward, new Vector3(hydro.bounds.center.x, y + 0.6f, alleyZ));
    }
}
