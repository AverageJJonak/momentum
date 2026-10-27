using UnityEngine;

/// <summary>
/// LEVEL 1 — FIRST FLIGHT · THE SANATORIUM (district-grammar, 2026-10-16). The teaching level, and the first one in
/// the streets. It starts in the ward you were made in (the opening plays here; the seal drops on the first punch that
/// connects, the one pause in the game), through the cloakroom where the run picks its class, out of THE STREET DOOR
/// onto WARD STREET — and THE SANATORIUM'S STREETS are a DISTRICT (Level1District): three blocks by three with two
/// alleys; the forecourt's fountain straight across the street (the first open fight), the diner, the garage, the
/// laundry that is a speakeasy (the door-in-a-door, the cellar, the coal chute, the office window over the avenue),
/// THE HYDRO (the bathhouse: empty pools you drop into, the mezzanine's slide into the plunge, the skylight, the
/// boiler room out the back), the tenement whose fire escape is the first tech climb and whose roof is the high way
/// onto the hydro's, the market, the park and the siding along the back alley, the tram up Halcyon Avenue; many ways
/// through, the route up the avenue for the respawns. Then the course's big speed: THE LAUNDRY CHUTE (twenty-four
/// metres of slide), THE LINEN BRIDGE and THE DISCHARGE HALL out. Nothing closes; checkpoints are the junctions'
/// volumes and the brass lines on the floor.
/// Zones: the ward is the House's own (Office); everything after it is the sanatorium's cream and terrazzo (Utopia).
/// </summary>
public static class Level1Builder
{
    /// THE SCORE (2026-10-27): the level is GauntletLevels' line for it; what follows is the old build, kept for reference
    public static Vector3 Build(MovementConfig config) => GauntletLevels.Build(LevelKind.Level1, config);

    public static Vector3 BuildBefore(MovementConfig config)
    {
        LevelKit.Init();
        Transform root = new GameObject("Level1").transform;
        var c = LevelSections.Begin(11, 14f);
        c = LevelSections.Ward(root, config, LevelSections.Office(c));                 // WARD 7, y14: the opening plays here; the spawn is your pod
        Course.Checkpoint(root, c, "THE STREET DOOR");

        // THE SANATORIUM'S GAUNTLET (2026-10-26, user: "more platformy with hazards, vholume"): the streets give way
        // to a platform course hung over the city — eight sets of irregular pads, wall runs, presses, chutes, draughts
        // and falling bridges, the hazards throwing and never killing, a catch deck under every gap, the clock the
        // score. The district (Level1District + CityDistrict) stands in code for the other levels until they follow.
        c = Course.Gauntlet(root, config, LevelSections.Utopia(c), 8, 14f, "THE SANATORIUM");   // the Sanatorium's cream and terrazzo, not the ward's House black (the ward leaves the cursor in Office)

        // The way out: the course's speed set-pieces after the streets
        Course.Checkpoint(root, c, "THE LAUNDRY CHUTE");
        c = Course.Chute(root, config, c, 24f, 80f, 9f);                               // −24: the lip's wings, the glass, the pads
        Course.Checkpoint(root, c, "THE LINEN BRIDGE");
        c = Course.Bridge(root, config, c, 80f, 10f, 6f);                              // the deck and its gap, the span over, the slide under
        Course.Checkpoint(root, c, "THE DISCHARGE HALL");
        c = Course.Hall(root, config, c, 70f, 10f, 10f);                               // pillar rows, two galleries, the chandelier: the way out
        LevelSections.Finish(root, config, c);
        return LevelSections.Last.spawn;
    }
}
