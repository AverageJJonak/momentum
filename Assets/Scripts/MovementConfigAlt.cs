using UnityEngine;

/// <summary>
/// Units in the air (2026-10-12): the kick's launch and the hook's knock put a unit on a ballistic arc (EnemyToss);
/// this is the gravity it falls under. The alt casts this file once held (the haymaker, the anchor, both barrels,
/// the maul) went with their weapons in the simplification (2026-10-16).
/// </summary>
public partial class MovementConfig
{
    [Header("Units in the air: the kick's launch and the hook's knock")]
    [Tooltip("Gravity on a tossed unit, m/s² (lighter than yours: the juggle hangs)")]
    public float enemyTossGravity = 14f;
}
