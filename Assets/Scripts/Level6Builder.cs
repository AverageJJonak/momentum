using UnityEngine;

/// <summary>
/// LEVEL 6 — LAST TRAIN · HALCYON METRO. Act II opens at street level behind the House at sundown: the terminus's
/// forecourt, and the lift head cut through the end of it — LIFTS OUT OF SERVICE. You drop forty metres down the
/// tiled shaft past the lamps to the bottom landing, and from there the course never closes: ONE CONTINUOUS COURSE of
/// 963 m (course-grammar) — the ticket hall (turnstiles as lips, the waiting room's glass, the escalators down),
/// Platform 1 (the first train, the live rail, the footbridge), the escalator hall down (banister slides), the running
/// tunnel (trains, the cable tray, the refuges), the curve, THE VENT SHAFT (the fan lifts you), THE ESCALATORS (a 100 m
/// slide: the speed set-piece), Platform 2, a drop into the cable vault, the crossover bridge over the tracks, and the
/// running tunnel out. The track gang stand on the lines as fuel. Checkpoints are line crossings, named for the line.
/// </summary>
public static class Level6Builder
{
    /// THE SCORE (2026-10-27): the level is GauntletLevels' line for it; what follows is the old build, kept for reference
    public static Vector3 Build(MovementConfig config) => GauntletLevels.Build(LevelKind.Level6, config);

    public static Vector3 BuildBefore(MovementConfig config)
    {
        LevelKit.Init();
        Transform root = new GameObject("Level6").transform;
        var c = LevelSections.Begin(66, 40f);
        c = LevelSections.Forecourt(root, config, LevelSections.Utopia(c), 46f);                              // z 0 → 28: the forecourt, the lift head (the cursor stops at the hole)
        c = LevelSections.LiftShaft(root, config, LevelSections.Works(c), 40f);                               // 28 → 44, floor 0: the drop
        Course.Checkpoint(root, c, "THE TICKET HALL");                                                        // 44
        c = LevelSections.TicketHall(root, config, c, "HOUSE STATION", 60f, 20f, 12f, 6f);                     // 44 → 104, floor −6: the barrier line, the escalators
        Course.Checkpoint(root, c, "PLATFORM 1");                                                             // 104
        c = LevelSections.Platform(root, config, c, "PLATFORM 1  ·  HOUSE STATION", 120f, 18f, 12f, 16f, 0f, true);   // 104 → 224: the first train
        LevelSections.Flavour(LevelSections.UndergroundFlavour.Station, "HOUSE STATION");
        c = Course.Stair(root, config, c, -10f, 60f, 14f);                                                    // 224 → 284, floor −16: the escalator hall down (banister slides)
        Course.Checkpoint(root, c, "THE TUNNELS");                                                            // 284
        c = LevelSections.TunnelGallery(root, config, c, "RUNNING TUNNEL 4", 110f, 11f, 9f, 14f, 0.2f, true);  // 284 → 394: the live rail, the refuges
        LevelSections.Flavour(LevelSections.UndergroundFlavour.Tunnel, "RUNNING TUNNEL 4");
        c = Course.Bank(root, config, c, 80f, 10f, 8f);                                                       // 394 → 474: the curve of the line
        Course.Checkpoint(root, c, "THE VENT SHAFT");                                                         // 474
        c = LevelSections.VentShaft(root, config, c, 22f, 7f);                                                // 474 → 500, floor 6: the fan lifts you
        Course.Checkpoint(root, c, "THE ESCALATORS");                                                         // 500
        c = Course.Chute(root, config, c, 24f, 100f, 9f);                                                     // 500 → 600, floor −18: the speed set-piece
        Course.Checkpoint(root, c, "PLATFORM 2");                                                             // 600
        c = LevelSections.Platform(root, config, c, "PLATFORM 2  ·  JUNCTION", 120f, 18f, 12f, 15f, 0.4f, false);   // 600 → 720
        LevelSections.Flavour(LevelSections.UndergroundFlavour.Tunnel, "CABLE VAULT 2");
        c = Course.DropIn(root, config, c, 10f, 10f);                                                         // 720 → 752.7, floor −28: into the cable vault
        Course.Checkpoint(root, c, "THE CROSSOVER");                                                          // 752.7
        LevelSections.Flavour(LevelSections.UndergroundFlavour.Tunnel, "THE CROSSOVER");
        c = Course.Bridge(root, config, c, 90f, 11f, 6f);                                                     // 752.7 → 842.7: the crossover bridge over the tracks
        LevelSections.Flavour(LevelSections.UndergroundFlavour.Tunnel, "RUNNING TUNNEL 5");
        c = Course.Gallery(root, config, c, 100f, 12f, 10f);                                                  // 842.7 → 942.7: the running tunnel out
        LevelSections.Finish(root, config, c);                                                                // → 962.7
        return LevelSections.Last.spawn;
    }
}
