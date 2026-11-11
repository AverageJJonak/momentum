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

    [Header("POLE SWING (STEP 3) · hold the whip key beside a marked pole")]
    [Tooltip("Off: a marked pole is a post like any other and the whip key is only the whip")]
    public bool poleSwingEnabled = true;
    [Tooltip("How near the pole the whip key takes hold, metres (the gym's posts stand 5.4 m from the middle of the line)")]
    public float poleReach = 5.5f;
    [Tooltip("The reach grows by this many seconds of your speed (at 20 m/s and 0.1 s: two metres more), so a fast pass has the same time to press")]
    public float poleReachSeconds = 0.1f;
    [Tooltip("The least speed for a swing")]
    public float poleMinSpeed = 5f;
    [Tooltip("The tightest circle a swing is held to, metres from the pole")]
    public float poleRadius = 1f;
    [Tooltip("A swing lets go by itself after this many degrees")]
    public float poleMaxTurn = 300f;
    [Tooltip("A swing must have turned this many degrees for the release to pay")]
    public float poleMinTurn = 45f;
    [Tooltip("Letting go multiplies the speed held through the swing (the gain above 1 scales with freshness)")]
    public float poleReleaseMultiplier = 1.06f;
}
