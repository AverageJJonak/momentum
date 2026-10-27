using UnityEngine;
using Kind = CityDistrict.Establishment;

/// <summary>
/// LEVEL 5 — CHECKING IN · HALCYON HOUSE (district-grammar, 2026-10-16). THE ENTRANCE HALL with the doormen on its
/// line (the spawn), then THE HOUSE'S OWN QUARTER: a three-by-three district of the company's establishments —
/// the HALCYON BANK, THE ARCADE (a glazed street through its block) and HALCYON & CO. on House Street; the HALCYON
/// CASINO, THE FORECOURT (the House's plaza, the company mark on twin pylons) and the HALCYON CLUB (its mews) on
/// Arcade Row; THE HOUSE GARDEN, THE REGISTRY and THE MOTOR COURT on Registry Row — the main route up Halcyon
/// Avenue, along Arcade Row past the forecourt and up Concord Avenue to Directors' Row, the straight line through
/// the arcade, the forecourt and the registry one of the many ways. Then THE VAULTS (a drop-in, the long vault
/// gallery), THE DIRECTORS' STAIR (+10) and THE ATRIUM, where the Concierge is waiting: the one gated space in the
/// act, at the end of the course. Office for the House's own rooms; the quarter in the city's cream and terrazzo.
/// </summary>
public static class Level5Builder
{
    /// THE SCORE (2026-10-27): the level is GauntletLevels' line for it; what follows is the old build, kept for reference
    public static Vector3 Build(MovementConfig config) => GauntletLevels.Build(LevelKind.Level5, config);

    public static Vector3 BuildBefore(MovementConfig config)
    {
        LevelKit.Init();
        Transform root = new GameObject("Level5").transform;
        var c = LevelSections.Begin(55, 0f);
        var doors = LevelSections.Office(c);
        c = Course.Hall(root, config, doors, 90f, 14f, 12f);                           // THE ENTRANCE HALL: the spawn, two galleries, the chandelier
        Course.Fuel(root, config, doors, EnemyKind.Sentry, new Vector3(-3.5f, doors.floorY, doors.z + 25f), "the doors");   // the doormen
        Course.Fuel(root, config, doors, EnemyKind.Sentry, new Vector3(3.5f, doors.floorY, doors.z + 25f), "the doors");
        Course.Checkpoint(root, c, "THE STREET DOORS");

        // THE HOUSE'S OWN QUARTER: three blocks by three, 42 × 38 m, every gap a 16 m street (an alley on an inner
        // gap would shift the middle column off the lane in at x = 0; the club keeps its own mews inside its block)
        var plan = CityDistrict.Plan(55, 3, 3, 42f, 38f, 16f);
        for (int g = 1; g < 3; g++) { CityDistrict.Alley(plan, true, g, false); CityDistrict.Alley(plan, false, g, false); }
        plan.streetsZ = new[] { "CROWN AVENUE", "HALCYON AVENUE", "CONCORD AVENUE", "ASTOR AVENUE" };
        plan.streetsX = new[] { "HOUSE STREET", "ARCADE ROW", "REGISTRY ROW", "DIRECTORS' ROW" };
        plan.exitHalfW = 10f;
        CityDistrict.Place(plan, 0, 0, Kind.Bank, 0, "THE HALCYON BANK");             // its portico south, onto House Street
        CityDistrict.Place(plan, 1, 0, Kind.Arcade, 0, "THE ARCADE");                 // straight through: House Street to Arcade Row
        CityDistrict.Place(plan, 2, 0, Kind.Store, 0, "HALCYON & CO.");
        CityDistrict.Place(plan, 0, 1, Kind.Casino, 3, "THE HALCYON CASINO");         // its marquee east, onto Halcyon Avenue
        CityDistrict.Place(plan, 1, 1, Kind.Plaza, 0, "THE FORECOURT");
        CityDistrict.Place(plan, 2, 1, Kind.Club, 1, "THE HALCYON CLUB");             // its marquee west, onto Concord Avenue; the mews along Registry Row
        CityDistrict.Place(plan, 0, 2, Kind.Park, 0, "THE HOUSE GARDEN");
        CityDistrict.Place(plan, 1, 2, Kind.Registry, 0, "THE REGISTRY");             // its doors south onto the forecourt, its back door onto the lane out
        CityDistrict.Place(plan, 2, 2, Kind.Garage, 0, "THE MOTOR COURT");
        c = CityDistrict.Build(root, config, c, plan, Zone.Utopia);
        CityDistrict.Route(plan, new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(2, 2), new Vector2Int(2, 3));
        CityDistrict.Checkpoints(root, plan, false, true);   // the route's junctions only: the House's own establishments are the named checkpoints
        Forecourt(root, plan);
        HouseCheckpoints(root, plan);
        // The district's own framing, a plain claim: the Atrium's signature line at the end of the level takes the postcard
        LevelSections.Postcard(new Vector3(-28f, plan.floorY + 21f, plan.z0 + 1f), new Vector3(3f, plan.floorY + 7f, plan.z0 + 96f), 74f);

        // THE TAIL: down into the vaults, the vault gallery, the directors' stair, the Concierge
        c = Course.DropIn(root, config, c, 12f, 10f);                                  // THE VAULTS: −12, off Directors' Row
        c = LevelSections.Office(c);
        Course.Checkpoint(root, c, "THE VAULTS");
        c = Course.Gallery(root, config, c, 90f, 10f, 9f);                             // the vault gallery
        c = Course.Stair(root, config, c, 10f, 50f, 12f);                              // THE DIRECTORS' STAIR: +10
        Course.Checkpoint(root, c, "THE ATRIUM");
        var atrium = c;
        c = Chase.Build(root, config, c, ChaseKind.Concierge);                         // THE CHASE: the Concierge rides ahead down the House's galleries; the course generates as you run
        LevelSections.Postcard(new Vector3(-7f, atrium.floorY + 5.5f, atrium.z + 4f), new Vector3(0f, atrium.floorY + 3f, atrium.z + 44f), 70f, true);
        LevelSections.Finish(root, config, c);
        return LevelSections.Last.spawn;
    }

    /// THE FORECOURT: twin pylons carrying the company mark at the plaza's south entry, the mark inlaid between them
    static void Forecourt(Transform root, CityDistrict.DistrictPlan plan)
    {
        var plaza = plan.BlockAt(1, 1);
        if (plaza == null) return;
        Vector3 pc = plaza.Centre;
        float south = plaza.bounds.min.z;
        for (int s = -1; s <= 1; s += 2) HouseKit.Pylon(root, new Vector3(pc.x + s * 7f, pc.y, south + 4.5f), Vector3.back, 7f);
        HouseKit.FloorMark(root, new Vector3(pc.x, pc.y, south + 4.5f), 5f, Vector3.forward);
    }

    /// The House's own checkpoints: a volume in each of its four establishments and the forecourt, the respawn standing
    /// just inside the front and facing in (the block's own forward, whichever way it is turned)
    static void HouseCheckpoints(Transform root, CityDistrict.DistrictPlan plan)
    {
        Volume(root, plan.BlockAt(1, 0), "THE ARCADE", new Vector3(12f, 14f, plan.blockD - 2f), new Vector3(0f, 0f, -14f));
        Volume(root, plan.BlockAt(1, 1), "THE FORECOURT", new Vector3(plan.blockW, 14f, plan.blockD), new Vector3(-8f, 0f, -13f));
        Volume(root, plan.BlockAt(0, 1), "THE CASINO", new Vector3(plan.blockW - 2f, 12f, plan.blockD - 2f), new Vector3(0f, 0f, -15.5f));
        Volume(root, plan.BlockAt(2, 1), "THE CLUB", new Vector3(plan.blockW - 2f, 16f, plan.blockD - 2f), new Vector3(3f, 0f, -16.5f));
        Volume(root, plan.BlockAt(1, 2), "THE REGISTRY", new Vector3(plan.blockW - 2f, 12f, plan.blockD - 2f), new Vector3(0f, 0f, -14f));
    }

    static void Volume(Transform root, CityDistrict.Block b, string name, Vector3 size, Vector3 standLocal)
    {
        if (b == null) return;
        Quaternion q = Quaternion.Euler(0f, b.rotation * 90f, 0f);
        Vector3 c = b.Centre;
        var bounds = new Bounds(c + Vector3.up * (size.y * 0.5f), size);
        CityDistrict.Checkpoint(root, name, bounds, q * Vector3.forward, c + q * standLocal + Vector3.up * 0.6f);
    }
}
