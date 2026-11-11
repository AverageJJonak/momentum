using UnityEngine;

/// <summary>
/// ARCHITECTURE MOVES (STEP 3, 2026-11-10; the movement identity programme). Three moves the motor does only on pieces
/// the level kit has marked (MoveSurface): the LEDGE VAULT (a ledge up to two metres over the feet is gone over with
/// the speed you brought: a jump pressed as you reach it is graded like the other timed moves, and with no press at all
/// the ledge still lets you over, slower, so a marked ledge is never a dead stop), the RAIL GRIND (land on a rail and
/// it carries you along its line, gaining a little; jump off whenever) and the POLE SWING (hold the whip key beside a
/// pole to swing round it at the speed you have and let go facing a new way). Each has its own switch; off, the piece
/// is a plain wall, a narrow floor or a post. Nothing here is read in Awake.
/// </summary>
public partial class MovementConfig
{
    [Header("LEDGE VAULT (STEP 3) · over a marked ledge with your speed")]
    [Tooltip("Off: a marked ledge is a wall like any other")]
    public bool vaultEnabled = true;
    [Tooltip("The highest ledge a vault takes, metres over the feet (a jump's apex is 1.6)")]
    public float vaultLipReach = 2f;
    [Tooltip("The least speed into the ledge for a vault")]
    public float vaultMinSpeed = 4f;
    [Tooltip("A jump pressed within this many seconds of meeting the ledge is a TIMED vault and keeps all the speed. With no press the vault still happens, at the untimed keep")]
    public float vaultWindow = 0.2f;
    [Tooltip("Inside this many seconds the timed vault is PERFECT (then the good band, then late)")]
    public float vaultPerfectWindow = 0.06f;
    [Tooltip("A perfect vault multiplies the speed carried over (the gain above 1 scales with freshness; a good vault pays the good fraction of it)")]
    public float vaultPerfectMultiplier = 1.06f;
    [Tooltip("What an untimed vault keeps of the speed")]
    public float vaultAutoKeep = 0.8f;
    [Tooltip("The hop the vault leaves the ledge with, metres a second up")]
    public float vaultUp = 3f;

    [Header("RAIL GRIND (STEP 3) · a marked rail carries you along its line")]
    [Tooltip("Off: a marked rail is a narrow floor like any other")]
    public bool grindEnabled = true;
    [Tooltip("The least speed a rail carries you at: land slower and the rail brings you up to this")]
    public float grindMinSpeed = 8f;
    [Tooltip("What a level rail adds each second, metres a second (a rail that falls adds gravity's share on top, one that climbs takes it)")]
    public float grindAccel = 2f;
    [Tooltip("The rail's own gain stops at this speed (speed brought to the rail above it is kept)")]
    public float grindMaxSpeed = 40f;
    [Tooltip("How hard the rail pulls the feet onto its line, per second")]
    public float grindSnap = 8f;
    [Tooltip("A jump off a rail multiplies the speed (the gain above 1 scales with freshness)")]
    public float grindJumpMultiplier = 1.05f;
    [Tooltip("The rail must have been ridden this long for the jump off it to pay (a touch and a hop is a hop)")]
    public float grindJumpMinTime = 0.3f;
}
