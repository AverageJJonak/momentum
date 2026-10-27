using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Single source of truth for every movement number in the MOMENTUM spec.
/// All values are STARTING VALUES — tune freely in the inspector (spec §6).
/// </summary>
[CreateAssetMenu(fileName = "MovementConfig", menuName = "Momentum/Movement Config")]
public partial class MovementConfig : ScriptableObject
{
    [Header("Simulation")]
    [Tooltip("Fixed physics tick rate in Hz (spec §6: 90+ for input feel)")]
    public float physicsTickRate = 90f;

    [Header("Ground movement (spec §2.1)")]
    public float baseRunSpeed = 8f;
    public float groundAcceleration = 60f;
    [Tooltip("Exponential decay rate per second")]
    public float groundFriction = 8f;
    [Tooltip("Friction is disabled for this long after landing — the bhop window")]
    public float bhopFrictionGraceTime = 0.1f;
    [Tooltip("Positive magnitude; applied downward")]
    public float gravity = 25f;

    [Header("Jumping & air control (spec §2.2)")]
    public float jumpImpulse = 9f;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.15f;
    [Tooltip("Quake-style: accel only applies to the wishdir component not already moving in")]
    public float airAcceleration = 90f;
    [Tooltip("Per-tick cap on wishdir speed gain — classic air-strafe model, no total airspeed cap")]
    public float airMaxWishSpeed = 1.2f;
    [Tooltip("CPM-style air control: forward input steers velocity this fast (deg per s) without gaining speed. W-held bhops keep steering while A or D strafing still gains")]
    public float airControlTurnRate = 90f;

    [Header("Slide (spec §2.3)")]
    public float slideActivationSpeed = 6f;
    [Tooltip("Flat-ground slide bleed; fades to zero when pointed down-slope")]
    public float slideFriction = 0.6f;
    [Tooltip("Downhill gravity gain multiplier while sliding — slopes are free speed")]
    public float slideSlopeAccelMultiplier = 1.5f;
    [Tooltip("Slide steering: WASD rotates the slide this many degrees per second (momentum preserved)")]
    public float slideSteerRate = 90f;
    [Tooltip("Pressing back against a slide brakes it at this friction")]
    public float slideBrakeFriction = 3f;
    public float slideJumpVerticalImpulse = 7f;
    public float slideJumpHorizontalMultiplier = 1.15f;
    [Tooltip("Slide ends and you stand up below this speed")]
    public float slideEndSpeed = 1f;
    [Tooltip("Capsule height while sliding / air-crouching (shorter hitbox)")]
    public float slideCapsuleHeight = 0.9f;
    [Tooltip("How fast the capsule height changes, m/s")]
    public float crouchTransitionSpeed = 8f;
    [Tooltip("Extra downward accel while fastfalling (crouch in air)")]
    public float fastfallExtraGravity = 10f;

    [Header("Corner boost — a timed jump on a ledge's 90° lip turns speed into HEIGHT (Deadlock's corner boost)")]
    [Tooltip("A wall contact whose top surface lies within this far above the feet is a lip (the 90° edge you can boost on)")]
    public float cornerBoostLipReach = 0.45f;
    [Tooltip("A jump press within ±this of the lip contact is the PERFECT tier: the full conversion")]
    public float cornerBoostWindow = 0.06f;
    [Tooltip("A press within ±this (outside the perfect window) is the LAZY tier; later than this it is a plain jump")]
    public float cornerBoostLazyWindow = 0.15f;
    [Tooltip("Perfect tier: vertical = max(jumpImpulse, horizontal speed × this) + cornerBoostPop")]
    public float cornerBoostConversion = 0.6f;
    [Tooltip("Lazy tier's conversion of horizontal speed into vertical")]
    public float cornerBoostLazyConversion = 0.3f;
    [Tooltip("Vertical added on top of the conversion (the edge pushes you up)")]
    public float cornerBoostPop = 2f;
    [Tooltip("Fraction of the pre-contact horizontal speed kept — never more than you brought (the hop chain owns horizontal speed)")]
    public float cornerBoostKeep = 0.85f;
    [Tooltip("THE CAP: the vertical result never exceeds jumpImpulse × this, combo included")]
    public float cornerBoostMaxLift = 2f;

    [Header("Stamina (spec §3.1, §3.3)")]
    public int staminaMaxPips = 3;
    [Tooltip("Seconds of passive regen per pip — slow on purpose, spam should feel starved")]
    public float staminaRegenSecondsPerPip = 4f;
    public float staminaRegenPauseAfterSpend = 0.75f;
    [Tooltip("Temporary overcharge pip from overfilled refunds decays after this if unused")]
    public float overchargeDecaySeconds = 5f;

    [Header("Wall kick (spec §4.1)")]
    [Tooltip("Wall must be within this distance while airborne")]
    public float wallKickRange = 0.6f;
    public float wallKickNormalImpulse = 7f;
    public float wallKickUpImpulse = 6f;
    [Tooltip("Fraction of tangential speed preserved on a plain wall kick")]
    public float wallKickTangentKeep = 0.8f;
    public int wallKickCost = 1;

    [Header("Edge boost — a wall kick on the VERTICAL corner of a wall, where two faces meet (Deadlock's edge boost)")]
    [Tooltip("Wall kick off a vertical corner: impulse multiplier, 100% tangential, leaves along the sum of both normals, pip refunded — this IS the reset")]
    [FormerlySerializedAs("cornerBoostImpulseMultiplier")]
    public float edgeBoostImpulseMultiplier = 1.4f;
    [Tooltip("A second wall face whose normal is more than this many degrees from the kick wall's = a corner (two facing walls never are)")]
    [FormerlySerializedAs("cornerDetectionAngle")]
    public float edgeBoostCornerAngle = 30f;
    [Tooltip("The second face's contact must lie within this of the kick's contact point")]
    [FormerlySerializedAs("cornerDetectionRadius")]
    public float edgeBoostCornerReach = 0.5f;
    [Tooltip("Authored CornerMarker hitboxes within this of the kick point guarantee an edge boost")]
    public float cornerMarkerRadius = 0.8f;

    [Header("Wall kick feel (Deadlock-inspired)")]
    [Tooltip("Extra impulse along held movement input at kick time — hold away from the wall to fly further")]
    public float wallKickInputImpulse = 3.5f;
    [Tooltip("Wall kicks per airtime that cost no stamina (first kick off the ground is free)")]
    public int freeWallKicksPerAirtime = 1;
    [Tooltip("Brief gravity reduction on wall contact to make kicks readable (spec §4.1, toggleable)")]
    public bool wallClingEnabled = true;
    public float wallClingDuration = 0.25f;
    [Tooltip("Gravity multiplier while clinging (lower = floatier)")]
    public float wallClingGravityFactor = 0.4f;

    [Header("Wall run (Titanfall-flavoured) — keep moving along a wall and you stick to it")]
    public bool wallRunEnabled = true;
    [Tooltip("A wall within this distance to your left or right can be run")]
    public float wallRunRange = 0.7f;
    [Tooltip("Horizontal speed needed to start and keep a run")]
    public float wallRunMinSpeed = 6f;
    [Tooltip("Seconds a run lasts before gravity takes over again")]
    public float wallRunMaxDuration = 1.6f;
    [Tooltip("Gravity fraction at the start of a run (ramps to full at its end)")]
    public float wallRunGravityFactor = 0.15f;
    [Tooltip("Lean into the wall (m/s²) so the run holds the surface")]
    public float wallRunStickAccel = 4f;
    [Tooltip("Downward speed is capped to this on attach — the wall catches you")]
    public float wallRunCatchSpeed = 1f;
    [Tooltip("Small hop on attach")]
    public float wallRunEntryPop = 1.5f;
    [Tooltip("Holding forward keeps the pace: acceleration along the wall")]
    public float wallRunAccel = 12f;
    [Tooltip("Can't re-attach to the wall you just left for this long")]
    public float wallRunReattachLockout = 0.35f;
    [Tooltip("Jump still counts as a wall jump this long after the run ended (running off the end of the wall)")]
    public float wallRunJumpCoyote = 0.15f;
    public float wallRunJumpNormalImpulse = 6f;
    public float wallRunJumpUpImpulse = 7f;
    [Tooltip("Tangential speed multiplier on a wall jump — the run's reward")]
    public float wallRunJumpTangentBoost = 1.08f;
    public float wallRunJumpInputImpulse = 3f;
    [Tooltip("A corner boost within this long after a wall jump is a WALL COMBO")]
    [FormerlySerializedAs("wallJumpEdgeComboWindow")]
    public float wallJumpCornerComboWindow = 2f;
    [Tooltip("The corner boost's vertical is multiplied by this when it combos off a wall jump (still under cornerBoostMaxLift)")]
    [FormerlySerializedAs("wallJumpEdgeComboMultiplier")]
    public float wallJumpCornerComboMultiplier = 1.6f;
    [Tooltip("Camera roll (degrees) leaning off the wall during a run")]
    public float wallRunCameraRoll = 8f;

    [Header("Dash (spec §3.4)")]
    public float dashSpeed = 18f;
    [Tooltip("Seconds of overridden control before normal physics resume")]
    public float dashDuration = 0.15f;
    [Tooltip("Jumping within this of dash start carries the burst into airspeed (friction is held off)")]
    public float dashMomentumWindow = 0.2f;
    [Tooltip("Reserved for combat — brief invulnerability after dash (spec: tune later, may cut)")]
    public float dashIFrames = 0.2f;
    public int dashCost = 1;

    [Header("Double jump (spec §3.5)")]
    [Tooltip("Air jump vertical impulse; horizontal is fully redirected to wishdir at current speed")]
    public float doubleJumpImpulse = 8.5f;
    public int doubleJumpCost = 1;
    public int maxAirJumps = 1;
    [Tooltip("With ground closer than this (plus fall distance over the buffer window), a jump press buffers for the bhop instead of double jumping")]
    public float doubleJumpMinGroundClearance = 0.5f;

    [Header("Slam + slam storage (spec §3.6)")]
    public float slamDownSpeed = 30f;
    public int slamCost = 1;
    [Tooltip("Jumping within this of slam landing converts impact speed to horizontal + refunds the pip")]
    public float slamStorageWindow = 0.15f;
    [Tooltip("Fraction of impact speed converted to horizontal boost on a storage jump")]
    public float slamStorageConversion = 0.6f;
    [Tooltip("Shockwave radius on landing (damage/knockback arrives with combat)")]
    public float slamShockwaveRadius = 4f;
    [Tooltip("A slam STORES WHAT IT FELL: the storable impact is the speed the drop alone would have given you under gravity, capped at the slam's own speed — a hop stores a hop, a real drop stores everything. Off = the old fixed slam speed for every slam")]
    public bool slamStorageFromDrop = true;
    [Tooltip("A slam scores style only when it fell at least this far (m): the press is not the trick, the drop is")]
    public float slamStyleMinDrop = 4f;

    [Header("Grapple-dagger (spec §3.7, ULTRAKILL-ified: instant attach, accelerating yank)")]
    [Tooltip("Costs 2 — the yank-kill full refill is the payoff for spending big")]
    public int grappleCost = 2;
    [Tooltip("Yank TOP speed — the pull accelerates up to this")]
    public float grappleYankSpeed = 32f;
    [Tooltip("Yank starts at your current speed or this, whichever is higher")]
    public float grappleYankStartSpeed = 15f;
    [Tooltip("How hard the yank ramps toward top speed")]
    public float grappleYankAcceleration = 60f;
    [Tooltip("Sphere radius swept along your aim — enemies inside it get magnetized")]
    public float grappleAimAssistRadius = 1.5f;
    [Tooltip("Cooldown after retrieval — short, the pip cost is the real limiter")]
    public float grappleCooldown = 0.6f;
    [Tooltip("Dagger sticks to any surface (v0 gym); later restrict to enemies + marked surfaces")]
    public bool grappleAnySurface = true;
    public float grappleMaxRange = 60f;
    [Tooltip("Yank auto-releases within this distance of the stick point")]
    public float grappleArriveDistance = 1.5f;

    [Header("Grapple SWING (Spider-Man — surface/orb grapples)")]
    [Tooltip("Rope is your distance to the anchor on attach, capped here")]
    public float grappleSwingMaxLength = 34f;
    [Tooltip("Shortest the rope reels to while you hold — reel in to gain height & speed")]
    public float grappleSwingMinLength = 6f;
    [Tooltip("How fast holding E reels the rope in")]
    public float grappleSwingReelSpeed = 9f;
    [Tooltip("Inward tug on attach so you swing off the ground into the arc")]
    public float grappleSwingInitialTug = 8f;

    [Header("Style system (spec §5) — thresholds tuned down until kill volume rises")]
    public float styleThresholdC = 120f;
    public float styleThresholdB = 350f;
    public float styleThresholdA = 700f;
    public float styleThresholdS = 1200f;
    public float styleThresholdSPlus = 1800f;
    [Tooltip("Points per fresh (not-in-window) move in a chain")]
    public float styleFreshMovePoints = 20f;
    [Tooltip("Points per reset (pip-refunding action)")]
    public float styleResetPoints = 30f;
    [Tooltip("The same MOVEMENT reset (slam storage, corner boost, edge boost, perfect slide-jump, the chain) scores nothing again inside this window — the pip still comes back. Combat resets are enemy-paced and always score")]
    public float styleResetRepeatWindow = 6f;
    [Tooltip("Base points per kill (arrives with combat)")]
    public float styleKillPoints = 50f;
    [Tooltip("Rolling window for movement freshness")]
    public float freshnessWindowSeconds = 6f;
    [Tooltip("Multiplier grows this much per distinct move in the window, from 1.0x")]
    public float freshnessMultiplierPerDistinct = 0.25f;
    public float freshnessMultiplierMax = 3f;
    [Tooltip("Grounded on flat ground this long before style starts decaying")]
    public float styleFlatDecayDelay = 1.5f;
    [Tooltip("Style lost per second while decaying")]
    public float styleDecayRate = 100f;
    [Tooltip("Flat style loss on taking a hit (arrives with combat)")]
    public float styleHitPenalty = 150f;
    [Tooltip("Grace period after dropping below S before Flow State effects end")]
    public float flowStateGraceTime = 0.75f;
    [Tooltip("Where the hold line sits between S (0) and S+ (1): at 1, style has to STAY at S+ or flow ends after the grace")]
    public float flowHoldStrictness = 0.7f;
    [Tooltip("FLOW is a fuse: this long without a style gain and style starts pouring out")]
    public float flowHoldWindow = 2.2f;
    [Tooltip("Style lost per second while the flow fuse is burnt out")]
    public float flowDrainRate = 600f;

    [Header("Rank pressure — each rank is harder to earn AND to keep")]
    [Tooltip("Style gains shrink by this fraction per rank above D (D=1.0, S+=1-5×this)")]
    public float styleGainFalloffPerRank = 0.1f;
    [Tooltip("Gains never shrink below this fraction")]
    public float styleGainFalloffMin = 0.4f;
    [Tooltip("Flat-ground decay grows by this fraction per rank above D")]
    public float styleDecayGrowthPerRank = 0.4f;
    [Tooltip("Rank index where passive drain begins (0=D … 3=A, 4=S, 5=S+)")]
    public int stylePassiveDrainStartRank = 3;
    [Tooltip("Always-on drain per second, per rank at/above the start rank — even airborne. High ranks demand constant play")]
    public float stylePassiveDrainPerRank = 8f;

    [Header("Movement parry — turn momentum-killing impacts into tech")]
    [Tooltip("Press RMB/F up to this long BEFORE the impact (Expedition 33-style timing)")]
    public float parryWindow = 0.15f;
    [Tooltip("THE GUARD. From the press, everything parryable that arrives inside this many seconds is parried — every bolt, every sniper's shot, every windup in reach, all of them at once (Deadlock's window). The movement beat above is the first part of it")]
    public float parryGuardWindow = 0.35f;
    [Tooltip("The second and later connects inside one guard heal this share and do not lengthen the chain: a wall of snipers is one parry, not six")]
    public float parryHealBurstScale = 0.35f;
    [Tooltip("Cooldown after a WHIFFED parry (the window closed on nothing) — the HUD dot goes red and drains")]
    public float parryWhiffCooldown = 1.3f;
    [Tooltip("Short cooldown once a guard that CONNECTED comes down — chains stay possible")]
    public float parrySuccessCooldown = 0.3f;
    [Tooltip("Minimum speed for wall/land parries — low on purpose, parrying should feel available")]
    public float parryMinImpactSpeed = 5f;
    [Tooltip("COUNTER an enemy's windup (the gold telegraph) within this range of your chest: damaged, staggered, its attack cancelled. An enemy that is not attacking cannot be parried — that press is a whiff. Falling onto one at speed = POGO off its head")]
    public float enemyParryRadius = 3f;
    public float parryMeleeDamage = 25f;
    [Tooltip("Minimum upward speed from an enemy pogo, even from a soft fall")]
    public float parryPogoMinUpSpeed = 9f;
    [Tooltip("Wall parry = a little explosion off the surface: fraction of your REFLECTED speed kept — a redirect tool, not a free momentum keep")]
    public float parryBlastSpeedKeep = 1f;   // the blast is a floor under the reflection now: a wall parry saves your line, never grows it
    [Tooltip("Blast impulse away from the parried surface")]
    public float parryBlastPower = 8f;
    [Tooltip("Upward assist on a wall blast")]
    public float parryBlastUpPop = 3f;
    [Tooltip("Enemy & projectile parries ADD momentum along your current travel direction")]
    public float parryEnemyMomentumBoost = 6f;
    [Tooltip("Floor parry / enemy pogo: fraction of fall speed reflected upward")]
    public float parryPogoBounce = 0.9f;
    [Tooltip("Real-time freeze frame on a successful parry (ULTRAKILL-style)")]
    public float parryHitstopDuration = 0.15f;

    [Header("Tutorial gates")]
    [Tooltip("A gate accepts a required move performed within this many seconds of passing through")]
    public float gateRecentMoveWindow = 0.8f;

    [Header("Reset system (spec §3.3)")]
    [Tooltip("Distinct move types without touching flat ground for the chain reset; re-arms on landing")]
    public int chainResetCount = 3;

    [Header("Combat")]
    public float enemyDummyHealth = 100f;
    public float enemyTurretHealth = 150f;
    public float enemyRespawnSeconds = 5f;
    public float slamShockwaveDamage = 40f;
    [Tooltip("Low on purpose — the dagger tags, the blade kills")]
    public float grappleDaggerDamage = 10f;
    [Tooltip("Kill during a grapple pull, or within this after it, = full stamina refill (spec §3.3)")]
    public float grappleYankKillWindow = 0.3f;
    public float projectileSpeed = 12f;
    public float projectileFireInterval = 2.5f;
    public float projectileRange = 45f;
    [Tooltip("Projectile passing within this without hitting = near-miss reset (spec §3.3)")]
    public float projectileNearMissRadius = 1.5f;
    [Tooltip("Parry a projectile within this radius to reflect it back")]
    public float projectileParryRadius = 1.8f;
    [Tooltip("Projectiles glow gold and chirp inside this radius: 'this is parryable, now'")]
    public float projectileParryWarnRadius = 4.5f;
    [Tooltip("Training dummies fire slow homing orbs where they respawn (the tutorial, the range): parry practice. In the levels no regular unit shoots orbs — they are saved for the bosses")]
    public bool dummiesFireProjectiles = true;

    [Header("Sniper — the Sentinel's tracking laser: move slower than it and it has you")]
    [Tooltip("The laser tracks you inside this range, with line of sight")]
    public float sniperRange = 45f;
    [Tooltip("How fast the dot chases you (m/s). Move slower than this and it catches you; faster, and it never does")]
    public float sniperTrackSpeed = 11.5f;
    [Tooltip("The dot is ON you within this of your chest: the lock builds")]
    public float sniperLockRadius = 0.9f;
    [Tooltip("Seconds of the dot on you to lock — then the gold beat, then the shot")]
    public float sniperLockTime = 1f;
    [Tooltip("Seconds off you for a full lock to fade to nothing")]
    public float sniperLockDecay = 0.6f;
    [Tooltip("The gold beat between the lock and the shot. ANY parry inside it answers the shot, so this is the reaction window: long enough to see gold and act")]
    public float sniperTell = 0.45f;
    [Tooltip("The shot lands if the dot is within this of your chest the instant it fires")]
    public float sniperHitRadius = 1.1f;
    [Tooltip("Most of your health: the price of being slow")]
    public float sniperDamage = 60f;
    public float sniperKnockback = 9f;
    [Tooltip("After a shot the laser is off this long")]
    public float sniperCooldown = 2.2f;
    public float dummyFireInterval = 4f;
    public float dummyProjectileRange = 25f;
    public float reflectedProjectileSpeedMultiplier = 2.5f;
    public float reflectedProjectileDamage = 60f;
    [Tooltip("Small shove when taking a hit (style penalty is the real cost)")]
    public float hitKnockback = 4f;
    [Tooltip("Player health — enemies are a real threat now")]
    public float playerMaxHealth = 100f;
    [Header("Parry heals — the only healing there is")]
    [Tooltip("Health a parry puts back: the first of a chain, and how much more each one after it")]
    public float parryHealBase = 10f;
    public float parryHealStep = 5f;
    [Tooltip("The most one parry can heal (0 = no cap)")]
    public float parryHealMax = 35f;
    [Tooltip("Seconds between parries before the chain goes cold (0 = only a hit breaks it)")]
    public float parryChainWindow = 6f;
    [Tooltip("Wall and floor parries heal this share of a combat parry — they keep the chain warm between fights")]
    public float parryHealMoveScale = 0.5f;
    public float projectileDamage = 20f;
    public float chaserStrikeDamage = 30f;
    [Tooltip("0 = aim at where you are, 1 = full lead on where you will be")]
    public float projectileLeadFactor = 0.8f;
    [Tooltip("Shots HOME: they steer toward you at this rate (deg/s) — dodge late or parry")]
    public float projectileTurnRate = 140f;
    [Tooltip("Inside this distance a shot stops steering and flies straight: the dodge/parry window")]
    public float projectileTrackingStopDistance = 3.5f;
    [Tooltip("Reflected shots home onto their shooter at this rate (deg/s)")]
    public float reflectedProjectileTurnRate = 400f;
    [Tooltip("DECISION (spec §3.4): dash grants i-frames vs projectiles")]
    public bool dashGivesIFrames = true;
    [Header("Breakable glass — the fast routes are behind it")]
    [Tooltip("Speed straight INTO a pane needed to crash through — a slide-hop or a short bhop chain gets here; a walk does not")]
    public float glassBreakSpeed = 12f;
    [Tooltip("Fraction of speed kept when crashing through")]
    public float glassBreakSpeedKeep = 0.9f;
    public float glassBreakStyleBonus = 30f;

    [Header("Sword strike (LMB) — precision melee, weakspots execute")]
    [Tooltip("How far the blade reaches from the eye")]
    public float swordReach = 6f;
    [Tooltip("Cast radius — thin on purpose, landing weakspots at speed is the skill")]
    public float swordCastRadius = 0.15f;
    public float swordDamage = 25f;
    public float swordCooldown = 0.5f;
    [Tooltip("A cut that meets nothing: the blade has to be gathered back before the next — longer than the cooldown, and the combo starts over. Aim.")]
    public float swordWhiffRecovery = 0.85f;
    [Tooltip("From the press, the arm is committed to the cut for this long (÷ swing speed): a parry pressed inside it has no guard to raise and is thrown away as a whiff, cooldown and all. Mashing both buttons is what this costs")]
    public float swordCommit = 0.28f;
    [Tooltip("Extra style on a weakspot execute (also refunds a pip)")]
    public float executeStyleBonus = 40f;
    public float executeHitstop = 0.12f;
    [Tooltip("Tiny freeze on a normal sword hit — the cut bites")]
    public float swordHitstop = 0.045f;
    [Header("FISTS — the bare hands a run opens with; a tackle at speed is the brawler's cut")]
    [Tooltip("How far a punch reaches from the eye")]
    public float fistReach = 2.2f;
    [Tooltip("The punch's cast radius: a fist is forgiving where the blade is thin (every line of THE WINDOW's fan is this fat)")]
    public float fistRadius = 0.45f;
    [Tooltip("THE WINDOW: a fist is not precise. Every frame the probe fans fistSweepCasts sphere casts across ± this many degrees either side of the crosshair (the cane's cut is one line; the knuckles are a wall of them): a weakspot anywhere in the fan and in reach is the execute, else the nearest body in it takes the punch, and the HUD brackets and the knuckle light read the whole fan")]
    public float fistSweepAngle = 40f;
    [Tooltip("...and the fan is three rows deep: level, and ± this many degrees above and below, so a head over the crosshair or a crouched body under it is in the window too (0 = one row)")]
    public float fistSweepPitch = 18f;
    [Tooltip("Casts per row of the fan, the centre cast among them (an even count rounds up so the centre line exists)")]
    public int fistSweepCasts = 9;
    [Tooltip("The closer's own window: a body OUT of reach (inside fistLungeRange) is lunged at only within ± this many degrees of the crosshair, the line nearest the centre first; a body in reach is always closed on. The lunge's speed floor and cap are unchanged")]
    public float fistLungeSweepAngle = 40f;
    [Tooltip("A punch's damage before SpeedDamage multiplies it")]
    public float fistDamage = 30f;
    [Tooltip("Seconds between punches that land")]
    public float fistCooldown = 0.22f;
    [Tooltip("From the press, the arm is committed for this long: a parry pressed inside it is a whiff (parry THEN punch)")]
    public float fistCommit = 0.1f;
    [Tooltip("A punch that meets nothing is gathered back slower than one that lands")]
    public float fistWhiffRecovery = 0.35f;
    [Tooltip("A live unit under the crosshair inside this range makes the press a LUNGE")]
    public float fistLungeRange = 6f;
    [Tooltip("The lunge sets your speed along the aim to at least this, capped at a hop's worth over your run: a closer, never a hop, and under tackleMinSpeed so a tackle needs speed you brought")]
    public float fistLungeSpeed = 11f;
    [Tooltip("How long the lunge holds its line")]
    public float fistLungeDuration = 0.12f;
    [Tooltip("Below this horizontal speed a unit is a wall; at or above it, with Strike down, you go THROUGH it")]
    public float tackleMinSpeed = 12f;
    [Tooltip("A tackle deals your speed times this (36 at 12 m/s, 90 at 30)")]
    public float tackleDamagePerSpeed = 3f;
    [Tooltip("At or above this speed a tackle that lands at chest height is an execute")]
    public float tackleExecuteSpeed = 20f;
    [Tooltip("Strike pressed inside this many seconds before the contact (not held) is a PERFECT tackle; held is lazy")]
    public float tacklePerfectWindow = 0.12f;
    [Tooltip("A perfect tackle multiplies horizontal speed by this at low speed (under the perfect hop at every speed)")]
    public float tacklePerfectMultiplier = 1.12f;
    [Tooltip("...fading to this at the hop's fade-end speed")]
    public float tacklePerfectMinMultiplier = 1.05f;
    [Tooltip("The same unit cannot be tackled twice inside this: one pass, one hit")]
    public float tackleUnitCooldown = 0.3f;
    [Tooltip("The freeze on a perfect tackle")]
    public float tackleHitstop = 0.08f;

    [Header("The blade's sound — loud and satisfying")]
    [Tooltip("The cut's own layers (slash, clash, shing) play at this: the loudest thing in the mix. Everything under them (punch, crunch, bass, ring) is scaled to it")]
    public float slashVolume = 0.9f;
    [Tooltip("How far a swing's sound crosses the stereo field with the swing (0 = centred)")]
    public float slashPan = 0.35f;
    [Tooltip("How much the score ducks on a cut (0.4 = down to 60%) and for how long: the cut owns the room")]
    public float slashDuck = 0.4f;
    public float slashDuckSeconds = 0.16f;

    [Header("Chain blade — the sword dislocates into a whip on every swing")]
    [Tooltip("Half-angle (deg) of the sweep fan along the cut line: everything inside takes body damage")]
    public float swordSweepAngle = 45f;
    public int swordSweepCasts = 7;
    public float swordSweepRadius = 0.5f;
    [Tooltip("How many times its assembled length the visible chain stretches at full whip")]
    public float chainExtension = 4f;

    [Header("Ragdolls")]
    [Tooltip("Seconds until a corpse freezes where it lies (physics off)")]
    public float ragdollSettle = 5f;
    [Tooltip("Seconds until a corpse sinks away")]
    public float ragdollLife = 25f;
    [Tooltip("Camera roll (degrees) leaning into each sword cut")]
    public float swordCameraRoll = 4f;
    [Tooltip("Camera yaw (degrees) the body turns into each full swing; springs back, aim untouched")]
    public float swordCameraYaw = 3f;

    [Header("Rank & flow — the blade gets faster as you climb; FLOW STATE cuts everything")]
    [Tooltip("Swing speed multiplier at S+ (animation AND cooldown); D = 1.0, ranks between lerp")]
    public float swordRankSpeedMax = 1.45f;
    [Tooltip("Extra swing speed while in FLOW STATE")]
    public float swordFlowSpeedMult = 1.15f;
    [Tooltip("FLOW STATE reach multiplier for the blade and its sweep")]
    public float swordFlowReachMult = 2.2f;
    [Tooltip("FLOW STATE sweep half-angle (deg) — the whole front of you")]
    public float swordFlowSweepAngle = 90f;
    public int swordFlowSweepCasts = 15;
    public float swordFlowSweepRadius = 1f;
    [Tooltip("FLOW STATE damage multiplier on every cut")]
    public float swordFlowDamageMult = 2f;

    [Header("UI")]
    [Tooltip("HUD/menu scale on top of the automatic window scaling (1 = the HUD fills the same share of any window)")]
    public float uiScale = 1f;
    [Tooltip("Top-left speed/state/fps readout — tuning gear only; the tutorial teaches the controls")]
    public bool showDebugReadout = false;

    [Header("Chaser (melee enemy)")]
    public float chaserHealth = 120f;
    public float chaserSpeed = 9f;
    public float chaserAggroRange = 32f;
    [Tooltip("Starts its telegraphed strike inside this range")]
    public float chaserMeleeRange = 2.2f;
    [Tooltip("Gold-flash telegraph before the strike — the parry window's target")]
    public float chaserWindup = 0.45f;
    public float chaserStrikeRange = 2.8f;
    public float chaserCooldown = 1.2f;
    [Tooltip("Parrying the windup cancels the attack and staggers it this long")]
    public float chaserStaggerTime = 2f;

    [Header("The Concierge (Chapter 1 boss)")]
    public float bossHealth = 220f;
    [Tooltip("Gold buttons: one pops per reflected shot; the last one opens the coat (phase two)")]
    public int bossButtonCount = 4;
    [Tooltip("Every bite shows on the bar: a body slash, a reflected shot, a counter, a cut cuff, an executed throat")]
    public float bossSlashDamage = 3f;
    public float bossReflectDamage = 10f;
    public float bossCounterDamage = 6f;
    public float bossCuffDamage = 18f;
    public float bossThroatDamage = 36f;
    public int bossVolleyShots = 3;
    public float bossVolleyInterval = 3.6f;
    public float bossSweepInterval = 7f;
    public float bossSweepDamage = 22f;
    [Tooltip("Reach of the red sweep (metres); jump it or slide under it; the desk shields you")]
    public float bossSweepLength = 26f;
    [Tooltip("The bell: a red ring out across the floor; be in the air or on the desk")]
    public float bossBellInterval = 8f;
    public float bossBellDamage = 18f;
    public float bossBellRadius = 26f;
    [Tooltip("The greeting: within this range he spreads both arms (gold, parry it) and claps")]
    public float bossGreetRange = 12f;
    public float bossGreetWindup = 0.8f;
    public float bossGreetDamage = 25f;
    public float bossGreetCooldown = 6f;
    [Tooltip("The handshake: a red disc where the hand lands; the hand rests there with its cuff open")]
    public float bossHandshakeRange = 18f;
    public float bossHandshakeTelegraph = 0.6f;
    public float bossHandshakeRadius = 3.5f;
    public float bossHandshakeDamage = 20f;
    public float bossHandshakeRest = 1.4f;
    public float bossHandshakeCooldown = 9f;
    [Tooltip("Phase two: the announcement orb that drags you in unless parried")]
    public float bossPullInterval = 6.5f;
    public float bossPullSpeed = 11f;
    public float bossPullImpulse = 28f;
    [Tooltip("Seconds he stays bowed with the throat open after a stagger")]
    public float bossStaggerWindow = 2.4f;
    public float bossLashDamage = 14f;
    [Tooltip("Attack cadence multiplier in phase two")]
    public float bossPhase2Speed = 1.3f;
    [Tooltip("Style on the kill")]
    public float bossStyleBonus = 300f;

    [Header("The Foreman (Chapter 2 boss)")]
    public float foremanHealth = 260f;
    [Tooltip("The shield: three plates on a ring round the boiler turning this fast (deg/s); only the open quadrant takes a hit; an EXECUTE on the core knocks one off, the third opens overtime")]
    public float foremanShieldSpin = 40f;
    public int foremanPlateCount = 3;
    [Tooltip("Every bite shows on the bar: a body slash, a reflected rivet, a counter, a cut stack, an executed core")]
    public float foremanSlashDamage = 3f;
    public float foremanReflectDamage = 10f;
    public float foremanCounterDamage = 6f;
    public float foremanStackDamage = 18f;
    public float foremanCoreDamage = 36f;
    [Tooltip("The rivet gun on its shoulder: gold shots, parry them back")]
    public int foremanVolleyShots = 3;
    public float foremanVolleyInterval = 3.4f;
    [Tooltip("The hammer: within this range it raises the ram (gold, parry it for a COUNTER) and brings it down; the slam sends a red ring across the floor")]
    public float foremanHammerRange = 9f;
    public float foremanHammerWindup = 1.15f;
    public float foremanHammerDamage = 26f;
    public float foremanSlamRadius = 11f;
    public float foremanSlamDamage = 16f;
    public float foremanHammerCooldown = 6f;
    [Tooltip("The pour: the ladle swings out, a red disc where it will land, then a pool of molten metal that stays; stand in it and it burns")]
    public float foremanPourRange = 24f;
    public float foremanPourTelegraph = 0.95f;
    public float foremanPourRadius = 3.4f;
    public float foremanPourDamage = 12f;
    public float foremanPoolLife = 7f;
    public float foremanPourRest = 1.1f;
    public float foremanPourCooldown = 8f;
    [Tooltip("The charge: it lines you up (gold body, parry near it to counter; a red lane on the floor, do not be on it) and drives the lane out; a wall or the lane's end stalls it with the STACKS open")]
    public float foremanChargeTell = 1.35f;
    public float foremanChargeSpeed = 17f;
    public float foremanChargeRange = 30f;
    public float foremanChargeHitRange = 3.8f;
    public float foremanChargeDamage = 24f;
    public float foremanStallTime = 2.6f;
    public float foremanChargeCooldown = 7f;
    [Tooltip("The shift whistle: a red ring out across the floor from the body; be in the air, or up on the gantry")]
    public float foremanWhistleInterval = 8.5f;
    public float foremanWhistleDamage = 16f;
    public float foremanWhistleRadius = 26f;
    [Tooltip("Phase two: the winch hook, a gold orb that drags you in unless parried")]
    public float foremanHookInterval = 6.5f;
    public float foremanHookSpeed = 11f;
    public float foremanHookImpulse = 28f;
    [Tooltip("Seconds the furnace doors stay open on the core after a stagger")]
    public float foremanStaggerWindow = 2.4f;
    public float foremanVentDamage = 14f;
    [Tooltip("Attack cadence multiplier in phase two")]
    public float foremanPhase2Speed = 1.3f;
    [Tooltip("The sweep: the hammer round the whole body at knee height (red: jump it or slide it)")]
    public float foremanSweepInterval = 7f;
    public float foremanSweepTell = 1.25f;
    public float foremanSweepSwing = 1f;
    public float foremanSweepReach = 9f;
    public float foremanSweepDamage = 22f;
    [Tooltip("The stomp: it rises on its skids and drops; a red ring out across the floor")]
    public float foremanStompInterval = 9f;
    public float foremanStompRadius = 14f;
    public float foremanStompDamage = 18f;
    [Tooltip("Overtime: three strikes in a row — hammer, boom, both — the first two gold, the last red")]
    public float foremanFlurryInterval = 8f;
    public float foremanFlurryDamage = 16f;
    [Tooltip("It walks you down at this speed between attacks; the rivet gun only reaches for you past the leash")]
    public float foremanWalkSpeed = 5.5f;
    public float foremanLeash = 18f;
    [Tooltip("Style on the kill")]
    public float foremanStyleBonus = 320f;

    [Header("The plant (Chapter 2): the presses and the molten metal")]
    [Tooltip("Under a press ram when it comes down")]
    public float factoryPressDamage = 20f;
    [Tooltip("Standing in molten metal, every half second")]
    public float factoryMoltenDamage = 10f;
    [Tooltip("Hit by a haul cart")]
    public float factoryCartDamage = 22f;
    [Tooltip("Caught by a sweeper's boom")]
    public float factorySweepDamage = 14f;
    [Tooltip("Between the jaws when they shut")]
    public float factoryJawDamage = 24f;
    [Tooltip("Through a live arc")]
    public float factoryArcDamage = 12f;
    [Tooltip("An updraft's lift over gravity (m/s²) through the lower three quarters of its column")]
    public float factoryLift = 14f;

    [Header("Rooms (Levels 1-4): the way out counts once the room is clear; leave early and you loop back to its start")]
    public bool roomLoopEnabled = true;
    [Tooltip("Metres per second the room's par time assumes")]
    public float roomParSpeed = 10f;
    [Tooltip("Seconds the par time allows per enemy in the room")]
    public float roomParPerEnemy = 2f;
    [Tooltip("With this many or fewer still standing, the room's survivors show through the walls (their own shape, amber): the end of a room should be a fight, not a search")]
    public int roomSeenAt = 3;
    public float roomClearStyleBonus = 60f;
    [Tooltip("Style multiplier for an instant clear; falls to ×1 at par")]
    public float roomFastClearMult = 2f;
    public int roomClearRefundPips = 1;
    [Tooltip("Leaving a cleared room faster than this pays again")]
    public float roomExitSpeedMin = 14f;
    public float roomExitSpeedBonus = 40f;

    [Header("The Janitor (sanitation unit): armoured, slow, a core in its back that vents after every two attacks")]
    public float janitorHealth = 320f;
    [Tooltip("Body slashes and slams do this fraction; the core takes janitorCoreDamage per execute")]
    public float janitorArmor = 0.35f;
    public float janitorCoreDamage = 110f;
    public float janitorSpeed = 4.5f;
    public float janitorAggroRange = 40f;
    public float janitorMeleeRange = 3.4f;
    public float janitorWindup = 0.6f;
    public float janitorStrikeRange = 4f;
    public float janitorStrikeDamage = 35f;
    public float janitorSlamRange = 6f;
    public float janitorSlamWindup = 0.7f;
    public float janitorSlamRadius = 7f;
    public float janitorSlamDamage = 30f;
    public float janitorChargeMinRange = 7f;
    public float janitorChargeRange = 20f;
    public float janitorChargeTell = 0.5f;
    public float janitorChargeSpeed = 14f;
    public float janitorChargeHitRange = 2.4f;
    public float janitorChargeDamage = 40f;
    public float janitorAttackCooldown = 1.4f;
    [Tooltip("Attacks between vents")]
    public int janitorVentEvery = 2;
    public float janitorVentSeconds = 3.5f;
    public float janitorStaggerTime = 2.5f;

    [Header("The bow — the sword folds into it (G / MMB / the wheel); press LMB and it draws itself and looses at full draw; hold past full to wait")]
    public float bowDamage = 22f;
    [Tooltip("Seconds the draw takes: it runs itself from the press")]
    public float bowDrawTime = 0.4f;
    [Tooltip("The bow is hitscan: the hit is decided the instant you loose. This is only how fast the streak you see flies the line")]
    public float bowArrowSpeed = 300f;
    public float bowRange = 90f;
    [Tooltip("A weakspot (the cyan head, an open core) multiplies the arrow")]
    public float bowWeakspotMult = 2.5f;
    [Tooltip("An arrow pops any enemy orb this close to its line")]
    public float bowOrbRadius = 0.7f;
    public float bowCooldown = 0.18f;
    [Tooltip("Seconds for the sword to fold into the bow, and back")]
    public float weaponSwitchTime = 0.5f;
    [Tooltip("The arrow's hitbox: a sphere this wide is cast down the line, so a near miss still lands")]
    public float bowArrowRadius = 0.55f;

    [Header("THE BLUNDERBUSS — THE SET's third weapon (Switch cycles cane → deck → blunderbuss); Strike fires; two shells, then the breech opens")]
    [Tooltip("Damage to every body in the cone before the falloff (1 inside blunderbussNearRange, blunderbussFarFalloff at the reach)")]
    public float blunderbussDamage = 70f;
    [Tooltip("How far the cone reaches; walls stop it")]
    public float blunderbussReach = 7f;
    [Tooltip("The cone's half-angle, degrees (2026-10-08: 22 → 32, the wider spread)")]
    public float blunderbussHalfAngle = 32f;
    [Tooltip("Inside this the falloff is 1 and a weakspot in the cone is an execute")]
    public float blunderbussNearRange = 3f;
    [Tooltip("The falloff at the reach: damage × this at blunderbussReach")]
    public float blunderbussFarFalloff = 0.4f;
    [Tooltip("The cone is a fan of sphere casts: a centre, then this many rings at even steps out to the full angle (3 at 32°: 10.7°, 21.3°, 32°)")]
    public int blunderbussRings = 3;
    [Tooltip("Casts on the innermost ring; each ring out has that many more (8, 16, 24), so at the reach the spheres stay a diameter apart along every ring and no body slips between them; each cast this wide")]
    public int blunderbussRingCasts = 8;
    public float blunderbussCastRadius = 0.5f;
    [Tooltip("THE BLAST JUMP: the shot kicks you this fast along the line away from the aim, through the motor's attributed paths. Horizontal speed after the kick is capped at max(current, run) × the perfect-hop tier at your speed (the fists' lunge cap): no blast beats a perfect bhop chain. 2026-10-08: 9 → 10.5, a heavier shove that only shows below the cap and in redirects")]
    public float blunderbussKick = 10.5f;
    [Tooltip("Aiming straight down from the ground the blast launches you at jumpImpulse × this (a jump and a third); the upward speed after any blast is clamped to it")]
    public float blunderbussLiftMult = 1.3f;
    [Tooltip("A shot inside this many seconds either side of a jump's apex (vertical speed crossing zero in the air) is a PERFECT BLAST")]
    public float blunderbussPerfectWindow = 0.05f;
    [Tooltip("A perfect blast multiplies the kick by this")]
    public float blunderbussPerfectMult = 1.12f;
    [Tooltip("Seconds between the two barrels")]
    public float blunderbussCooldown = 0.18f;
    [Tooltip("After the second shot the breech opens for this long: the shells drop, two fresh ones go in, the breech closes")]
    public float blunderbussReload = 1.1f;
    [Tooltip("A shot commits the arm for this long: a parry press inside it has no guard to raise (the sword's own rule)")]
    public float blunderbussCommit = 0.15f;
    [Tooltip("THE RECOIL (the viewmodel, ParryArmBlunderbuss): the muzzle rises this many degrees per shot, the gun pivoting about the butt plate in the shoulder; the second barrel stacks on the first's unsettled pose")]
    public float blunderbussRecoilPitch = 12f;
    [Tooltip("The gun comes straight back along the bore this far (metres) per shot. The camera's near plane is 0.3 m: past about 0.12 the breech starts to clip at the peak")]
    public float blunderbussRecoilBack = 0.08f;
    [Tooltip("The twist with the rise, degrees: the muzzle swings out to the right by this and rolls by 0.6 × this")]
    public float blunderbussRecoilTwist = 6f;
    [Tooltip("Seconds for the recoil to settle back into the rest pose (a damped spring)")]
    public float blunderbussRecoilSettle = 0.35f;
    [Tooltip("How far past the rest pose the settle dips on the way home, as a fraction of the rise (0.15 = a little overshoot, 0.01 = dead)")]
    public float blunderbussRecoilOvershoot = 0.15f;
    [Tooltip("Stacked shots never raise the muzzle past this many degrees")]
    public float blunderbussRecoilMax = 20f;
    [Tooltip("THE CAMERA on a shot (GameFeel.BlastShot): a pitch kick UP, degrees, that springs back through PlayerLook.AddPitchKick; the aim is untouched")]
    public float blunderbussCameraPitchKick = 3.5f;
    [Tooltip("The shot's roll and yaw kicks, as a multiple of the cane's (swordCameraRoll × 1.4, swordCameraYaw × 0.6)")]
    public float blunderbussCameraKickMult = 1.5f;
    [Tooltip("The shot's camera shake (0..1.5) and its FOV kick, degrees")]
    public float blunderbussCameraShake = 1.05f;
    public float blunderbussCameraFov = 10.5f;
    [Tooltip("The muzzle flash's size, and how many sepia smoke quads a shot throws off the mouth (0 = none)")]
    public float blunderbussFlashSize = 0.8f;
    public int blunderbussSmokePuffs = 6;

    [Header("VIEWMODEL — the hands' own camera")]
    [Tooltip("The field of view the arms and weapons are drawn at, whatever the world's FOV (ViewmodelCamera draws layer Viewmodel at this)")]
    public float viewmodelFov = 58f;
    [Tooltip("How much of the world camera's FOV kick (dashes, speed stretch, zooms) the hands' camera takes: 0 none, 1 all of it")]
    public float viewmodelFovKickFraction = 0.3f;

    [Header("FLOW STATE — the bow burns: one flame arrow through everything on its line, and a burst where it stops")]
    [Tooltip("The burning arrow's damage to every body it passes through, as a multiple of a plain arrow")]
    public float bowFlameDamageMult = 2f;
    [Tooltip("The burning arrow's hitbox")]
    public float bowFlameRadius = 1f;
    [Tooltip("The burst at the end of its line: radius, damage (× the speed multiplier) to everything in it")]
    public float bowFlameBlastRadius = 6f;
    public float bowFlameBlastDamage = 70f;

    [Header("Speed is damage — the sword and the bow; never the grapple")]
    [Tooltip("The damage multiplier grows by this per m/s (horizontal speed, plus half of any vertical)")]
    public float speedDamagePerMps = 0.1f;
    [Tooltip("The multiplier's ceiling")]
    public float speedDamageMaxMult = 5f;
    [Tooltip("A kill at or above this multiplier is EXPRESS")]
    public float speedDamageExpressMult = 3f;

    [Header("Halcyon Works — the excavation division (Chapter 2)")]
    [Tooltip("BORER: armoured until the drill jams in a wall or it runs its lane out; then the stack opens")]
    public float borerHealth = 300f;
    public float borerArmor = 0.35f;
    public float borerCoreDamage = 120f;
    public float borerAggroRange = 30f;
    public float borerTell = 0.9f;
    public float borerChargeSpeed = 22f;
    public float borerChargeRange = 26f;
    public float borerHitRange = 2.2f;
    public float borerChargeDamage = 35f;
    public float borerJamTime = 3f;
    public float borerStallTime = 1.3f;
    public float borerCooldown = 2.2f;
    [Tooltip("RIGGER: keeps its distance and swings a wrecking ball on a chain; parry it and it goes home")]
    public float riggerHealth = 200f;
    public float riggerAggroRange = 36f;
    public float riggerNear = 7f;
    public float riggerFar = 16f;
    public float riggerSpeed = 3.5f;
    public float riggerRange = 18f;
    public float riggerWindup = 0.7f;
    public float riggerBallSpeed = 16f;
    public float riggerChainLength = 20f;
    public float riggerBallDamage = 30f;
    public float riggerBallKnockback = 14f;
    public float riggerCooldown = 3.2f;
    public float riggerReturnDamage = 130f;
    public float riggerStaggerTime = 2.4f;
    [Tooltip("BLASTER: lobs demolition charges — gold in the air (parry them back), red on the ground (move)")]
    public float blasterHealth = 160f;
    public float blasterAggroRange = 34f;
    public float blasterNear = 6f;
    public float blasterFar = 14f;
    public float blasterSpeed = 4f;
    public float blasterRange = 20f;
    public float blasterWindup = 0.5f;
    public float blasterCooldown = 2.6f;
    public float blasterFuse = 1.5f;
    public float blastRadius = 3.5f;
    public float blastDamage = 90f;
    public float blastPlayerDamage = 30f;
    public float blastKnockback = 12f;
    public float chargeReturnSpeed = 22f;

    [Tooltip("Distinct kill methods in this rolling window raise the kill multiplier")]
    public float killVarietyWindow = 10f;
    public float killVarietyPerMethod = 0.25f;

    [Header("Hidden tech — unexplained speed gains are rewarded, not patched")]
    [Tooltip("Horizontal speed gained in one tick beyond all known sources above this = discovered glitch tech")]
    public float glitchSpeedGainThreshold = 2f;
    [Tooltip("Minimum time between hidden-tech rewards")]
    public float glitchRewardCooldown = 1f;
    [Tooltip("Pips refunded when the player discovers hidden tech")]
    public int glitchRefundPips = 1;

    [Header("Skill ladder — the harder the timing, the more momentum it returns")]
    [Tooltip("Jump PRESS within this of the landing instant = perfect bhop. Early buffered presses don't count — only a genuinely timed press")]
    public float perfectBhopWindow = 0.05f;
    [Tooltip("Horizontal multiplier on a perfect bhop. Always the top of the ladder")]
    public float perfectBhopSpeedMultiplier = 1.18f;
    [Tooltip("Perfect tiers gently fade at high speed so compounding stays exponential but tame — this is the floor for perfect bhop (still always a gain, still always best)")]
    public float perfectBhopMinMultiplier = 1.08f;
    [Tooltip("Perfect-tier fade band: full multiplier below this speed")]
    public float perfectBhopFadeStartSpeed = 18f;
    [Tooltip("Perfect-tier fade band: floor multiplier at or above this speed")]
    public float perfectBhopFadeEndSpeed = 30f;
    [Tooltip("Jump pressed within this AFTER slide start = perfect slide-jump (spec §3.3)")]
    public float perfectSlideJumpWindow = 0.12f;
    [Tooltip("Perfect slide-jump's high-speed floor (always below perfect bhop's)")]
    public float perfectSlideJumpMinMultiplier = 1.04f;
    [Tooltip("Lazy (buffered/late) slide-jump multiplier starts fading toward 1.0 at this horizontal speed")]
    public float slideJumpFadeStartSpeed = 14f;
    [Tooltip("Lazy slide-jump multiplier is fully 1.0 (pure maintain) at or above this speed")]
    public float slideJumpFadeEndSpeed = 22f;

    [Header("Body")]
    public float capsuleRadius = 0.4f;
    public float capsuleHeight = 1.8f;
    public float eyeHeight = 1.62f;
    [Tooltip("Surfaces steeper than this are walls, not ground")]
    public float maxSlopeAngle = 45f;

    [Header("Camera")]
    public float mouseSensitivity = 0.12f;
    public float fieldOfView = 95f;

    [Header("Speed feedback — subtle lines + FOV stretch")]
    [Tooltip("Speed lines start fading in at this horizontal speed")]
    public float speedLinesStartSpeed = 10f;
    [Tooltip("Speed lines reach full strength at this speed")]
    public float speedLinesFullSpeed = 28f;
    [Tooltip("Peak opacity of the persistent speed lines — keep it subtle")]
    public float speedLinesMaxAlpha = 0.8f;
    [Tooltip("FOV starts widening at this horizontal speed")]
    public float speedFovStartSpeed = 16f;
    [Tooltip("FOV boost is maxed at this speed")]
    public float speedFovFullSpeed = 32f;
    [Tooltip("Extra degrees of FOV at full speed")]
    public float speedFovMaxBoost = 8f;

    [Header("HALCYON RESPONSE (Chapter 3): the units that come after you")]
    [Tooltip("How many of the units that strike by hand (pursuit, hounds) may be winding up at once: a pack takes turns")]
    public int responseMeleeTokens = 2;
    [Header("Pursuit: the strider with the line")]
    public float pursuitHealth = 110f;
    public float pursuitSpeed = 14.5f;
    public float pursuitAggroRange = 60f;
    [Tooltip("It starts its thrust inside this")]
    public float pursuitKickRange = 6.5f;
    public float pursuitKickWindup = 0.42f;
    public float pursuitLungeSpeed = 26f;
    public float pursuitLungeTime = 0.24f;
    public float pursuitKickDamage = 22f;
    public float pursuitKickReach = 1.8f;
    public float pursuitCooldown = 1.7f;
    public float pursuitZipSpeed = 36f;
    public float pursuitZipRange = 55f;
    public float pursuitZipCooldown = 3.5f;
    [Tooltip("The line hangs there this long before it goes")]
    public float pursuitZipTell = 0.2f;
    public float pursuitDodgeCooldown = 2.6f;
    [Header("K-Unit: the hounds")]
    public float houndHealth = 45f;
    public float houndSpeed = 17f;
    public float houndCircleSpeed = 12f;
    [Tooltip("How far out they ring you")]
    public float houndRing = 8f;
    public float houndAggroRange = 42f;
    public float houndPounceWindup = 0.32f;
    public float houndPounceSpeed = 27f;
    public float houndPounceTime = 0.42f;
    public float houndBiteDamage = 10f;
    public float houndBiteReach = 1.5f;
    [Tooltip("Stamina pips a bite takes")]
    public int houndBitePips = 1;
    public float houndCooldown = 2.4f;
    [Header("Interceptor: the flying lightbar")]
    public float interceptorHealth = 70f;
    public float interceptorSpeed = 24f;
    public float interceptorAccel = 34f;
    [Tooltip("How far out, and how far up, it keeps station")]
    public float interceptorRing = 14f;
    public float interceptorHeight = 6.5f;
    public float interceptorAggroRange = 70f;
    public float interceptorRange = 42f;
    public float interceptorTell = 0.4f;
    public int interceptorBurst = 3;
    public float interceptorBurstGap = 0.14f;
    public float interceptorBoltSpeed = 34f;
    public float interceptorBoltDamage = 9f;
    public float interceptorCooldown = 3.2f;
    [Header("Kettler: the walking pylon")]
    public float kettlerHealth = 200f;
    [Tooltip("Damage multiplier while its lens is shut")]
    public float kettlerArmor = 0.45f;
    public float kettlerSpeed = 5.5f;
    public float kettlerAggroRange = 70f;
    public float kettlerNear = 20f;
    public float kettlerFar = 40f;
    [Tooltip("The red line lies on the ground this long before the fence is there")]
    public float kettlerFenceTell = 0.75f;
    public float kettlerFenceLife = 3.2f;
    public float kettlerFenceWidth = 13f;
    [Tooltip("Seconds of your travel ahead of you that it puts the fence")]
    public float kettlerFenceLead = 1.15f;
    public float kettlerCooldown = 5f;
    [Tooltip("Its lens stays open this long after the fence is up")]
    public float kettlerOpenTime = 1.6f;
    public float kettlerHopCooldown = 7f;
    public float kettlerLandDamage = 14f;
    public float kettlerLandRadius = 4.5f;
    [Header("Damage Control: the heavy")]
    public float dcHealth = 380f;
    [Tooltip("Damage multiplier while its valve is shut")]
    public float dcArmor = 0.4f;
    [Tooltip("The share of its whole health a cut on the open valve takes")]
    public float dcValveShare = 0.4f;
    public float dcSpeed = 6f;
    public float dcAggroRange = 62f;
    public float dcWaveTell = 0.9f;
    public float dcWaveSpeed = 17f;
    public float dcWaveRange = 38f;
    public float dcWaveWidth = 11f;
    [Tooltip("A jump clears 1.6 m")]
    public float dcWaveHeight = 1.25f;
    public float dcWaveDamage = 18f;
    public float dcClawRange = 4.6f;
    public float dcClawWindup = 0.6f;
    public float dcClawReach = 1.9f;
    public float dcClawDamage = 26f;
    public float dcCooldown = 3.4f;
    [Tooltip("The valve stays open this long after the cannon fires")]
    public float dcOpenTime = 2.8f;
    public float dcSkateCooldown = 3.5f;
    [Header("Sentry: the gun on a post")]
    public float sentryHealth = 90f;
    public float sentryRange = 38f;
    [Tooltip("Degrees a second it turns: slower than you can run round it")]
    public float sentryTurn = 130f;
    public float sentryTell = 0.3f;
    public int sentryBurst = 2;
    public float sentryBurstGap = 0.12f;
    public float sentryBoltSpeed = 40f;
    public float sentryBoltDamage = 8f;
    public float sentryCooldown = 1.8f;
    [Header("The district's own defences (Chapter 3)")]
    [Tooltip("Seconds in a searchlight before it calls fire on you")]
    public float searchlightCatch = 0.9f;
    public float searchlightAngry = 3.2f;
    public float mortarFuse = 1.3f;
    public float mortarRadius = 5f;
    public float mortarDamage = 28f;
    public float mineTrigger = 4.5f;
    public float mineFuse = 0.65f;
    public float mineRadius = 5f;
    public float mineDamage = 26f;

    [Header("HELP FOR THE PLAYER (after ULTRAKILL): what you deal comes back, hits are softened, mashing is not")]
    [Tooltip("Every hit a unit lands is scaled by this before it reaches your health")]
    public float enemyDamageScale = 0.8f;
    [Tooltip("After a hit lands nothing else can hurt you for this long: a pack cannot land three in one frame")]
    public float hitMercy = 0.35f;
    [Header("The feed: what the blade, the bow, the slam and a counter take off a unit comes back to you - when you are MOVING")]
    [Tooltip("Share of sword damage dealt that comes back as health")]
    public float feedSwordShare = 0.28f;
    [Tooltip("An execute feeds this much, flat")]
    public float feedExecute = 22f;
    [Tooltip("Share of arrow damage that comes back")]
    public float feedArrowShare = 0.12f;
    [Tooltip("Share of slam shockwave damage that comes back")]
    public float feedSlamShare = 0.3f;
    [Tooltip("Share of a parry counter's own damage that comes back (the parry heal is on top)")]
    public float feedParryShare = 0.2f;
    [Tooltip("The share of the feed you get standing still on the ground; the whole of it airborne, or at feedSpeedFull")]
    public float feedStillFactor = 0.35f;
    public float feedSpeedFull = 14f;
    [Tooltip("The most one hit can feed back")]
    public float feedMax = 30f;
    [Header("Hard damage: a share of every hit taken cannot be fed back until it has decayed (a parry heals through it)")]
    [Tooltip("The share of each hit that becomes hard damage")]
    public float hardShare = 0.45f;
    [Tooltip("Hard damage never exceeds this share of max health")]
    public float hardMaxShare = 0.6f;
    [Tooltip("Seconds after the last hit before hard damage starts to decay, and how much of it goes a second")]
    public float hardDecayDelay = 1.4f;
    public float hardDecayRate = 14f;
    [Header("No mashing: cuts thrown standing still get weaker one after the other; a move between them is what resets the blade")]
    [Tooltip("Each stale cut in a row multiplies the blade's damage by this again")]
    public float staleSwingStep = 0.7f;
    [Tooltip("The floor of that multiplier")]
    public float staleSwingFloor = 0.35f;
    [Tooltip("On the ground below this speed a cut is stale")]
    public float staleSwingSpeed = 6f;
    [Tooltip("Every hand-to-hand windup in the game is this much longer: more room to see the gold")]
    public float tellScale = 1.2f;

    [Header("THE VARIETY (every chapter): eight more units, after ULTRAKILL's roster - EnemyPool decides which where")]
    [Header("Skitter: the fodder, in threes")]
    public float skitterHealth = 24f;
    public float skitterSpeed = 10f;
    public float skitterAggroRange = 34f;
    public float skitterReach = 2f;
    public float skitterWindup = 0.32f;
    public float skitterHopSpeed = 16f;
    public float skitterHopTime = 0.25f;
    public float skitterBiteDamage = 8f;
    public float skitterCooldown = 1.6f;
    [Header("Marshal: the fan of bolts, and the cell it drops")]
    public float marshalHealth = 90f;
    public float marshalSpeed = 6.5f;
    public float marshalAggroRange = 50f;
    public float marshalRange = 32f;
    [Tooltip("The distance it keeps: closer than this it backs off, further it comes on")]
    public float marshalNear = 9f;
    public float marshalFar = 18f;
    public float marshalTell = 0.55f;
    public int marshalBolts = 5;
    [Tooltip("Half-angle of the fan, degrees")]
    public float marshalSpread = 14f;
    public float marshalBoltSpeed = 30f;
    public float marshalBoltDamage = 9f;
    public float marshalCooldown = 3f;
    [Tooltip("The cell it drops when it dies: the blast's radius, what it does to units, and to you")]
    public float marshalCellRadius = 4f;
    public float marshalCellDamage = 60f;
    public float marshalCellPlayerDamage = 24f;
    [Header("Warden: the guard in front, the core behind")]
    public float wardenHealth = 160f;
    public float wardenSpeed = 7f;
    [Tooltip("Degrees a second it turns: slower than you can run round it")]
    public float wardenTurn = 210f;
    public float wardenAggroRange = 40f;
    public float wardenReach = 3f;
    public float wardenWindup = 0.42f;
    public float wardenThrustSpeed = 18f;
    public float wardenThrustTime = 0.22f;
    public float wardenThrustDamage = 26f;
    public float wardenCooldown = 1.8f;
    [Tooltip("The guard stays down this long after its thrust")]
    public float wardenOpenTime = 0.9f;
    public float wardenStaggerTime = 2.2f;
    [Header("Lurker: it waits on the walls")]
    public float lurkerHealth = 70f;
    public float lurkerGroundSpeed = 5f;
    public float lurkerClimbSpeed = 14f;
    [Tooltip("How high up a wall it perches, and how far it looks for one")]
    public float lurkerPerchHeight = 5.5f;
    public float lurkerPerchSearch = 14f;
    public float lurkerAggroRange = 26f;
    public float lurkerWindup = 0.5f;
    public float lurkerLeapSpeed = 26f;
    public float lurkerLeapTime = 0.9f;
    public float lurkerReach = 2.2f;
    public float lurkerDamage = 22f;
    [Tooltip("After a leap it is on the ground, slow and open, this long")]
    public float lurkerExposed = 1.6f;
    public float lurkerCooldown = 2.5f;
    [Header("Moth: the one that flies into you")]
    public float mothHealth = 20f;
    public float mothSpeed = 11f;
    public float mothAccel = 20f;
    [Tooltip("How far above your head it flutters")]
    public float mothHeight = 2.5f;
    public float mothAggroRange = 45f;
    public float mothDiveRange = 9f;
    public float mothWindup = 0.45f;
    public float mothDiveSpeed = 30f;
    public float mothDiveTime = 0.8f;
    public float mothBlastRadius = 3f;
    public float mothPlayerDamage = 18f;
    public float mothUnitDamage = 60f;
    [Tooltip("Parried: it goes for the nearest other unit at this speed")]
    public float mothPuntSpeed = 34f;
    [Header("Ocular: the eye and its beam")]
    public float ocularHealth = 60f;
    public float ocularSpeed = 9f;
    public float ocularHeight = 5f;
    [Tooltip("The distance it keeps")]
    public float ocularNear = 14f;
    public float ocularFar = 22f;
    public float ocularAggroRange = 55f;
    public float ocularRange = 40f;
    [Tooltip("The red charge: the line follows you this slowly (deg/s), then freezes, then the beam fires down it")]
    public float ocularCharge = 1.2f;
    public float ocularTrack = 40f;
    public float ocularFreeze = 0.25f;
    public float ocularBeamTime = 0.3f;
    public float ocularBeamRadius = 1.1f;
    public float ocularBeamDamage = 28f;
    public float ocularCooldown = 3.5f;
    [Header("Overseer: it covers the others; put it down first")]
    public float overseerHealth = 35f;
    public float overseerSpeed = 12f;
    public float overseerHeight = 7f;
    [Tooltip("How far from you it keeps")]
    public float overseerKeep = 22f;
    public float overseerAggroRange = 60f;
    [Tooltip("Units within this of it are covered: they take this share of damage (executes and cleaves go through)")]
    public float overseerCover = 26f;
    public float overseerArmour = 0.5f;
    [Header("Scourer: the flame")]
    public float scourerHealth = 140f;
    public float scourerSpeed = 6f;
    public float scourerAggroRange = 40f;
    public float scourerReach = 9f;
    public float scourerTell = 0.6f;
    public float scourerFlameTime = 1.6f;
    [Tooltip("Half-angle of the cone, and how fast it follows you (deg/s)")]
    public float scourerFlameAngle = 16f;
    public float scourerFlameTurn = 60f;
    public float scourerFlameTick = 0.2f;
    public float scourerFlameDamage = 6f;
    public float scourerCooldown = 2.8f;
    [Tooltip("How far it sidesteps a held aim")]
    public float scourerSidestep = 3.5f;
}
