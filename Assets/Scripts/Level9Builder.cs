using UnityEngine;

/// <summary>
/// LEVEL 9 — THE FOUNDATIONS. ONE CONTINUOUS COURSE of 932 m (course-grammar) through the city under the city: the
/// disused ticket hall at Old Town, its platform (the ghost trains still run), the drop into the foundations, THE
/// CRYPT (the buried street's columns, the old street's deck over it cut by collapsing slabs, the chasm: keep moving),
/// THE BURIED STAIR (a 110 m slide: the speed set-piece), the chasm bridge, a second crypt, THE FOOTINGS (a vent shaft
/// up through the House's footings: the vertical set-piece), the House's steam mains, the stair up to the approach,
/// and the undercroft hall under the terminus. Platelayers on the low roads, gangers on the high.
/// </summary>
public static class Level9Builder
{
    /// THE SCORE (2026-10-27): the level is GauntletLevels' line for it; what follows is the old build, kept for reference
    public static Vector3 Build(MovementConfig config) => GauntletLevels.Build(LevelKind.Level9, config);

    public static Vector3 BuildBefore(MovementConfig config)
    {
        LevelKit.Init();
        Transform root = new GameObject("Level9").transform;
        var c = LevelSections.Begin(99, 20f);
        c = LevelSections.TicketHall(root, config, LevelSections.Works(c), "OLD TOWN", 60f, 20f, 12f, 6f);      // 0 → 60, floor 14
        Course.Checkpoint(root, c, "THE DISUSED PLATFORM");                                                   // 60
        c = LevelSections.Platform(root, config, c, "OLD TOWN  ·  CLOSED 1922", 120f, 18f, 12f, 24f, 0.3f, true);   // 60 → 180: the ghost trains
        LevelSections.Flavour(LevelSections.UndergroundFlavour.Crypt, "OLD TOWN");
        c = Course.DropIn(root, config, c, 12f, 10f);                                                         // 180 → 216.1, floor 2: into the foundations
        Course.Checkpoint(root, c, "THE CRYPT");                                                              // 216.1
        c = LevelSections.Crypt(root, config, c, 100f, 18f, 16f);                                              // 216.1 → 316.1: the collapsing floors, the chasm
        Course.Checkpoint(root, c, "THE BURIED STAIR");                                                       // 316.1
        LevelSections.Flavour(LevelSections.UndergroundFlavour.Crypt, "THE BURIED STAIR");
        c = Course.Chute(root, config, c, 30f, 110f, 9f);                                                     // 316.1 → 426.1, floor −28: the speed set-piece
        Course.Checkpoint(root, c, "THE CHASM");                                                              // 426.1
        LevelSections.Flavour(LevelSections.UndergroundFlavour.Crypt, "THE CHASM");
        c = Course.Bridge(root, config, c, 90f, 11f, 6f);                                                     // 426.1 → 516.1: the chasm bridge
        c = LevelSections.Crypt(root, config, c, 90f, 18f, 16f);                                               // 516.1 → 606.1
        Course.Checkpoint(root, c, "THE FOOTINGS");                                                           // 606.1
        c = LevelSections.VentShaft(root, config, c, 26f, 7f);                                                // 606.1 → 632.1, floor −2: the vertical set-piece
        Course.Checkpoint(root, c, "THE HOUSE MAINS");                                                        // 632.1
        c = LevelSections.SteamGallery(root, config, c, 100f, 14f, 12f);                                       // 632.1 → 732.1: the vents
        Course.Checkpoint(root, c, "THE APPROACH");                                                           // 732.1
        LevelSections.Flavour(LevelSections.UndergroundFlavour.Station, "TERMINUS APPROACH");
        c = Course.Stair(root, config, c, 20f, 80f, 14f);                                                     // 732.1 → 812.1, floor 18: the climb
        LevelSections.Flavour(LevelSections.UndergroundFlavour.Station, "TERMINUS APPROACH");
        c = Course.Hall(root, config, c, 100f, 16f, 14f);                                                     // 812.1 → 912.1: the undercroft hall
        LevelSections.Finish(root, config, c);                                                                // → 932.1
        return LevelSections.Last.spawn;
    }
}
