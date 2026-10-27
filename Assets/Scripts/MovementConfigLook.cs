using UnityEngine;

/// <summary>
/// THE LOOK KNOBS (2026-10-16, the poster cut: "decrease the amount of details and make the game more artistic").
/// Build-time numbers the level kit reads while it builds (LevelKit.Ornament → Articulate, Densify, the Deco pieces,
/// the street props), never in Awake and never per frame: change one here, the next level built carries it. The last
/// MovementConfig loaded is the one the kit reads (the scene's; the menu and the editor preview see the same asset).
/// </summary>
public partial class MovementConfig
{
    [Header("LOOK · ORNAMENT (build time): how much a room is dressed. 0 = bare masses (the cornice, the string course, the gameplay props); 0.35 = the poster default (one pilaster per 6 m, one torchiere per 8 m, one hero prop per room, every third Works bay dressed, half the street props); 1 = every ornament the kit knows")]
    [Range(0f, 1f)]
    public float ornament = 0.35f;

    /// The config the kit's look knobs come from at build time: the last one loaded (a ScriptableObject is enabled when
    /// its scene loads it, before any bootstrap's Awake; the code-default fallback the bootstrap makes counts too)
    public static MovementConfig LookSource { get; private set; }

    void OnEnable() { LookSource = this; }
}
