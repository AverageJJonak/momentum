using UnityEngine;

/// <summary>
/// LEVEL 8 — HIGH WATER · THE SEWERS. ONE CONTINUOUS COURSE of 897 m (course-grammar) under the metro: the flooded
/// ticket hall at Waterside, the drop into the outfall, THE WEIR (a 25° brick channel under black water with weirs
/// across it — lips on a slide — and the waterfall at its end: the speed set-piece), THE CISTERN (sluice gates across
/// the lanes on a cycle, the hooks over the water, the inlet's fall through the gallery), Sanitation with the Janitor
/// on the line, THE STEAM MAINS (vents that burst and launch you), THE PUMP TOWER (a 30 m shaft climb: the vertical
/// set-piece), the bank, the second weir, the bridge over the black water, Cistern 2, and the stair up to the station.
/// Sweepers on the slides, stokers at the valves, housekeeping on the lanes.
/// </summary>
public static class Level8Builder
{
    /// THE SCORE (2026-10-27): the level is GauntletLevels' line for it; what follows is the old build, kept for reference
    public static Vector3 Build(MovementConfig config) => GauntletLevels.Build(LevelKind.Level8, config);

    public static Vector3 BuildBefore(MovementConfig config)
    {
        LevelKit.Init();
        Transform root = new GameObject("Level8").transform;
        var c = LevelSections.Begin(88, 20f);
        c = LevelSections.TicketHall(root, config, LevelSections.Works(c), "WATERSIDE", 60f, 20f, 12f, 6f);     // 0 → 60, floor 14
        LevelSections.Flood(root, c, 10f);                                                                    // the landing under a foot of water: the station is flooding
        Course.Checkpoint(root, c, "THE OUTFALL");                                                            // 60
        c = Course.DropIn(root, config, LevelSections.Under(c), 12f, 10f);                                     // 60 → 96.1, floor 2: into the sewer (the Underbelly's brick from here)
        Course.Checkpoint(root, c, "THE WEIR");                                                               // 96.1
        c = LevelSections.WeirRun(root, config, c, 20f, 80f, 8f);                                              // 96.1 → 176.1, floor −18: the speed set-piece
        Course.Checkpoint(root, c, "THE CISTERN");                                                            // 176.1
        c = LevelSections.SluiceCourt(root, config, c, 100f, 24f, 16f);                                        // 176.1 → 276.1: the sluices, the hooks
        Course.Checkpoint(root, c, "SANITATION");                                                             // 276.1
        c = LevelSections.Sanitation(root, config, c, 70f, 20f, 10f);                                          // 276.1 → 346.1: the Janitor's court
        Course.Checkpoint(root, c, "THE STEAM MAINS");                                                        // 346.1
        c = LevelSections.SteamGallery(root, config, c, 100f, 14f, 12f);                                       // 346.1 → 446.1: the vents
        Course.Checkpoint(root, c, "THE PUMP TOWER");                                                         // 446.1
        c = Course.Shaft(root, config, c, 30f, 7f);                                                           // 446.1 → 472.6, floor 12: the climb
        c = Course.Bank(root, config, c, 80f, 10f, 8f);                                                       // 472.6 → 552.6
        Course.Checkpoint(root, c, "THE SECOND WEIR");                                                        // 552.6
        c = LevelSections.WeirRun(root, config, c, 24f, 84f, 8f);                                              // 552.6 → 636.6, floor −12
        c = Course.Bridge(root, config, c, 90f, 11f, 6f);                                                     // 636.6 → 726.6: over the black water
        Course.Checkpoint(root, c, "CISTERN 2");                                                              // 726.6
        c = LevelSections.SluiceCourt(root, config, c, 90f, 24f, 16f);                                         // 726.6 → 816.6
        Course.Checkpoint(root, c, "THE WAY UP");                                                             // 816.6
        c = Course.Stair(root, config, LevelSections.Works(c), 12f, 60f, 14f);                                 // 816.6 → 876.6, floor 0: up to the station's tile
        LevelSections.Finish(root, config, c);                                                                // → 896.6
        return LevelSections.Last.spawn;
    }
}
