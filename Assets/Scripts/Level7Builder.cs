using UnityEngine;

/// <summary>
/// LEVEL 7 — THIRD RAIL · THE TUNNELS. ONE CONTINUOUS COURSE of 969 m (course-grammar) along the live rails: the
/// ticket hall at Junction St, Platform 3, the running tunnel (trains as timed hazards, the cable tray as the safe
/// line, the refuges), the crossover bridge, the curve, the escalators down (the speed set-piece), THE JUNCTION's
/// signal gallery (gantries to kick and boost, lever frames as lips, the signal box), THE VENT SHAFT (the vertical
/// set-piece: the fan or the shelves), the second running tunnel the other way, a drop, and Platform 4, terminus
/// bound. Signalmen on the gantries, platelayers on the beds, gangers in the lanes.
/// </summary>
public static class Level7Builder
{
    /// THE SCORE (2026-10-27): the level is GauntletLevels' line for it; what follows is the old build, kept for reference
    public static Vector3 Build(MovementConfig config) => GauntletLevels.Build(LevelKind.Level7, config);

    public static Vector3 BuildBefore(MovementConfig config)
    {
        LevelKit.Init();
        Transform root = new GameObject("Level7").transform;
        var c = LevelSections.Begin(77, 20f);
        c = LevelSections.TicketHall(root, config, LevelSections.Works(c), "JUNCTION ST", 60f, 20f, 12f, 6f);   // 0 → 60, floor 14
        Course.Checkpoint(root, c, "PLATFORM 3");                                                             // 60
        c = LevelSections.Platform(root, config, c, "PLATFORM 3  ·  JUNCTION ST", 120f, 18f, 12f, 15f, 0f, true);   // 60 → 180
        Course.Checkpoint(root, c, "THE TUNNELS");                                                            // 180
        c = LevelSections.TunnelGallery(root, config, c, "RUNNING TUNNEL 6", 110f, 11f, 9f, 14f, 0.3f, true);  // 180 → 290
        Course.Checkpoint(root, c, "THE CROSSOVER");                                                          // 290
        LevelSections.Flavour(LevelSections.UndergroundFlavour.Tunnel, "THE CROSSOVER");
        c = Course.Bridge(root, config, c, 90f, 11f, 6f);                                                     // 290 → 380: the crossover bridge
        LevelSections.Flavour(LevelSections.UndergroundFlavour.Tunnel, "THE CURVE");
        c = Course.Bank(root, config, c, 80f, 10f, 8f);                                                       // 380 → 460: the curve
        Course.Checkpoint(root, c, "THE ESCALATORS");                                                         // 460
        c = Course.Chute(root, config, c, 24f, 100f, 9f);                                                     // 460 → 560, floor −10: the speed set-piece
        Course.Checkpoint(root, c, "THE JUNCTION");                                                           // 560
        c = LevelSections.SignalGallery(root, config, c, 100f, 12f, 11f, 15f, 0.3f);                           // 560 → 660: the gantries, the signal box
        Course.Checkpoint(root, c, "THE VENT SHAFT");                                                         // 660
        c = LevelSections.VentShaft(root, config, c, 24f, 7f);                                                // 660 → 686, floor 14: the vertical set-piece
        Course.Checkpoint(root, c, "RUNNING TUNNEL 7");                                                       // 686
        c = LevelSections.TunnelGallery(root, config, c, "RUNNING TUNNEL 7", 110f, 11f, 9f, 13f, 0.6f, false); // 686 → 796: the trains the other way
        LevelSections.Flavour(LevelSections.UndergroundFlavour.Tunnel, "RUNNING TUNNEL 7");
        c = Course.DropIn(root, config, c, 10f, 10f);                                                         // 796 → 828.7, floor 4
        Course.Checkpoint(root, c, "PLATFORM 4");                                                             // 828.7
        c = LevelSections.Platform(root, config, c, "PLATFORM 4  ·  TERMINUS BOUND", 120f, 18f, 12f, 16f, 0.5f, false);   // 828.7 → 948.7
        LevelSections.Finish(root, config, c);                                                                // → 968.7
        return LevelSections.Last.spawn;
    }
}
