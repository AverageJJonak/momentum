using UnityEngine;

/// <summary>
/// LEVEL 10 — TERMINUS · THE STATIONMASTER. ONE CONTINUOUS COURSE of 832 m to the terminus gates (course-grammar),
/// then the one gated space of Act II: the ticket hall at Gate 1, THE CONCOURSE (the grand hall, the departures
/// board), THE SIGNAL GALLERY, the station's throat (the curve), THE TURNTABLE COURT (shunters crossing, the through
/// line, the gantry and the tower), a drop into the cable vault and the bridge over it, the escalators down (the speed
/// set-piece), Platform 9 (the last train in), the stair to the gates (the climb), the brass gates with the gang
/// between you and them — and the Stationmaster's platform where he is waiting, then the lift to the surface.
/// </summary>
public static class Level10Builder
{
    /// THE SCORE (2026-10-27): the level is GauntletLevels' line for it; what follows is the old build, kept for reference
    public static Vector3 Build(MovementConfig config) => GauntletLevels.Build(LevelKind.Level10, config);

    public static Vector3 BuildBefore(MovementConfig config)
    {
        LevelKit.Init();
        Transform root = new GameObject("Level10").transform;
        var c = LevelSections.Begin(110, 20f);
        c = LevelSections.TicketHall(root, config, LevelSections.Works(c), "TERMINUS  ·  GATE 1", 60f, 20f, 12f, 6f);   // 0 → 60, floor 14
        Course.Checkpoint(root, c, "THE CONCOURSE");                                                          // 60
        c = LevelSections.TerminusHall(root, config, c, 100f, 18f, 16f);                                       // 60 → 160: the grand hall
        Course.Checkpoint(root, c, "THE SIGNAL GALLERY");                                                     // 160
        c = LevelSections.SignalGallery(root, config, c, 100f, 12f, 11f, 15f, 0.1f);                           // 160 → 260
        Course.Checkpoint(root, c, "THE THROAT");                                                             // 260
        LevelSections.Flavour(LevelSections.UndergroundFlavour.Tunnel, "THE THROAT");
        c = Course.Bank(root, config, c, 80f, 10f, 8f);                                                       // 260 → 340: the station's throat
        Course.Checkpoint(root, c, "THE TURNTABLE");                                                          // 340
        c = LevelSections.TurntableCourt(root, config, c, 96f, 32f, 16f);                                      // 340 → 436: the shunters, the through line
        LevelSections.Flavour(LevelSections.UndergroundFlavour.Tunnel, "CABLE VAULT 9");
        c = Course.DropIn(root, config, c, 12f, 10f);                                                         // 436 → 472.1, floor 2
        Course.Checkpoint(root, c, "THE CABLE VAULT");                                                        // 472.1
        LevelSections.Flavour(LevelSections.UndergroundFlavour.Tunnel, "CABLE VAULT 9");
        c = Course.Bridge(root, config, c, 90f, 11f, 6f);                                                     // 472.1 → 562.1
        Course.Checkpoint(root, c, "THE ESCALATORS");                                                         // 562.1
        c = Course.Chute(root, config, c, 20f, 80f, 9f);                                                      // 562.1 → 642.1, floor −18: the speed set-piece
        Course.Checkpoint(root, c, "PLATFORM 9");                                                             // 642.1
        c = LevelSections.Platform(root, config, c, "PLATFORM 9  ·  TERMINUS", 120f, 18f, 12f, 14f, 0f, false);   // 642.1 → 762.1: the last train in
        LevelSections.Flavour(LevelSections.UndergroundFlavour.Station, "TERMINUS");
        c = Course.Stair(root, config, c, 16f, 70f, 14f);                                                     // 762.1 → 832.1, floor −2: the climb to the gates
        Course.Checkpoint(root, c, "THE TERMINUS GATES");                                                     // 832.1
        c = LevelSections.TerminusGate(root, config, c, 36f);                                                 // 832.1 → 868.1: the gang, the brass leaves
        c = Chase.Build(root, config, c, ChaseKind.Stationmaster);                                            // 868.1 →: THE CHASE: the Stationmaster's locomotive ahead down the terminus; the course generates as you run
        LevelSections.LiftLanding(root, config, c);                                                           // the lift to the surface, and the relief valve: the ending
        return LevelSections.Last.spawn;
    }
}
