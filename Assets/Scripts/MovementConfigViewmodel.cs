using UnityEngine;

/// <summary>
/// THE ARM RIG's numbers (2026-10-08, ArmRig.cs): the arms' proportions, the two-bone IK's reach rules, the fingers,
/// and every procedural layer's amplitude and spring — all tunable without code. The camera's own two fields
/// (viewmodelFov, viewmodelFovKickFraction) live in MovementConfig.cs beside the blunderbuss. Every pose in the rig
/// is authored for the hands' fixed 58° camera (ViewmodelCamera); these scale what moves on top of those poses.
/// </summary>
public partial class MovementConfig
{
    [Header("ARM RIG — proportions (camera space, metres; the left arm mirrors the right)")]
    [Tooltip("The right shoulder's pivot in the camera's space (x right, y up, z forward): behind and below the eye, outside the frame")]
    public Vector3 viewmodelShoulder = new Vector3(0.27f, -0.30f, -0.12f);
    public float viewmodelUpperArm = 0.32f;
    public float viewmodelForearm = 0.29f;
    [Tooltip("Where the elbow rests when the hand points along the arm: out to the side (1 = fully out) and down (1 = fully down). Elbows IN and down (2026-10-12: THE FINALS' close hold)")]
    public float viewmodelElbowOut = 0.5f;
    public float viewmodelElbowDown = 0.9f;
    [Tooltip("How much of the hand's roll turns that rest pole (0 = never)")]
    public float viewmodelElbowTwist = 0.15f;
    [Tooltip("How far the elbow swings to the side opposite the knuckles' lean so the forearm runs straight into the hand (1 = fully: a straight wrist whenever the arm allows it)")]
    public float viewmodelElbowFollowWrist = 0.8f;
    [Tooltip("A bare fist is straightened onto its forearm past this many degrees of wrist flex (0 = never); a hand on a weapon is never touched")]
    public float viewmodelFistWristFlex = 20f;
    [Tooltip("The arm is never straighter than this fraction of its full length: no hyperextended elbow")]
    public float viewmodelReachClamp = 0.985f;
    [Tooltip("Before a hand out of reach is pulled in, the shoulder comes forward toward it by up to this (the body leaning into a cut or a punch)")]
    public float viewmodelShoulderLead = 0.22f;
    [Tooltip("A hold that asks to stretch (the cane's cuts, the shove, the gun's recoil) may lengthen the arm up to this multiple before the hand is pulled in")]
    public float viewmodelMaxStretch = 1.8f;
    [Tooltip("The share of the shared layers' offset (sway, bob, land...) the shoulders take, so the whole body reads as moving while the hands move more")]
    public float viewmodelShoulderFollow = 0.35f;
    [Tooltip("The rate every state change in the rig is blended at (a critically damped catch-up, never a pop); higher = quicker")]
    public float viewmodelBlendRate = 14f;

    [Header("ARM RIG — fingers")]
    [Tooltip("How fast the fingers settle into a new curl (soft), and on a jab (snap)")]
    public float viewmodelFingerRate = 22f;
    public float viewmodelFingerSnapRate = 70f;
    [Tooltip("Seconds between one finger starting to move and the next: a fist closes in a ripple")]
    public float viewmodelFingerRipple = 0.025f;

    [Header("ARM RIG — sway (the hands lag the look)")]
    [Tooltip("Metres of lag per degree of look, and degrees of hand rotation per degree of look")]
    public float viewmodelSwayPos = 0.0022f;
    public float viewmodelSwayRot = 0.16f;
    [Tooltip("The sway spring: Hz, and its damping ratio (1 = no overshoot)")]
    public float viewmodelSwayFreq = 7f;
    public float viewmodelSwayDamping = 0.7f;
    public float viewmodelSwayMax = 0.07f;

    [Header("ARM RIG — bob (a stride-synced figure-eight on the ground)")]
    [Tooltip("Metres of bob at the run speed; the amplitude keeps rising with speed to viewmodelBobMax times this")]
    public float viewmodelBobAmp = 0.011f;
    public float viewmodelBobMax = 1.7f;
    [Tooltip("Metres of ground per stride cycle (one left-right); the figure-eight's clock")]
    public float viewmodelBobStride = 2.7f;
    public float viewmodelBobRot = 1.4f;
    [Tooltip("How fast the bob fades in on landing and out in the air")]
    public float viewmodelBobBlend = 9f;

    [Header("ARM RIG — breath (idle)")]
    public float viewmodelBreathAmp = 0.0045f;
    public float viewmodelBreathRate = 0.27f;

    [Header("ARM RIG — land, jump, dash")]
    [Tooltip("Metres of dip per m/s of landing speed, the cap, and the dip's spring (Hz, damping: under 1 bounces back)")]
    public float viewmodelLandImpulse = 0.011f;
    public float viewmodelLandMax = 0.14f;
    public float viewmodelLandFreq = 5.5f;
    public float viewmodelLandDamping = 0.42f;
    [Tooltip("The lift of the arms on a jump, metres")]
    public float viewmodelJumpLift = 0.045f;
    [Tooltip("A dash pulls the arms back this far against it, and the spring throws them forward past rest (damping under 1)")]
    public float viewmodelDashPull = 0.1f;
    public float viewmodelDashFreq = 4.5f;
    public float viewmodelDashDamping = 0.38f;

    [Header("ARM RIG — slide, wall run, sprint pump, tackle")]
    [Tooltip("Both hands drop this far in a slide; the free hand reaches for the ground at this camera-space point (x is mirrored for the left)")]
    public float viewmodelSlideDrop = 0.06f;
    public Vector3 viewmodelSlideReach = new Vector3(0.3f, -0.46f, 0.34f);
    [Tooltip("The wall is looked for this far to the side; the near free hand plants on it, open; the other arm pumps this much")]
    public float viewmodelWallReach = 1.2f;
    public float viewmodelWallPump = 0.05f;
    [Tooltip("Above the run speed times this the arms pump counter-phase, this many metres at full (at viewmodelPumpFullSpeed)")]
    public float viewmodelPumpOverRun = 0.55f;   // 2026-10-27: the arms pump from half the run speed (Mirror's Edge), not only above it
    public float viewmodelPumpAmp = 0.04f;
    public float viewmodelPumpFullSpeed = 12f;
    [Tooltip("The tackle: the near shoulder drops into the frame by this (camera space; x toward the centre) and springs back")]
    public Vector3 viewmodelTackleShoulder = new Vector3(-0.12f, -0.07f, 0.17f);
    public float viewmodelTackleFreq = 5f;
    public float viewmodelTackleDamping = 0.5f;

    [Header("ARM RIG — hit shudder (a connect)")]
    public float viewmodelShudderAmp = 0.012f;
    public float viewmodelShudderFreq = 26f;
    public float viewmodelShudderDecay = 0.13f;

    [Header("ARM RIG — the jab (the knuckles; Fists.cs registers the hit on the press, so the snap is everything)")]
    [Tooltip("The pull-back before the snap. Fists.cs lands the hit the frame you press, so anything here delays the fist past the hit: keep it a flick")]
    public float viewmodelJabAnticipation = 0.03f;
    [Tooltip("Seconds from the press to full extension (the elbow straight, the shoulder rolled in): ULTRAKILL-quick")]
    public float viewmodelJabSnap = 0.06f;
    [Tooltip("The follow-through past the target, and the recovery back to the stance with its overshoot")]
    public float viewmodelJabFollow = 0.06f;
    public float viewmodelJabRecover = 0.22f;
    public float viewmodelJabOvershoot = 0.18f;
    [Tooltip("How far past the fist's own extension the cross and the lunge reach (1 = the jab)")]
    public float viewmodelCrossScale = 1.15f;

    [Header("ARM RIG — THE ROOT (2026-10-12, THE FINALS' grammar: the whole rig is an implied torso that leans into swings, drops in a sprint, compresses on a landing and rolls with the look; one spring, metres and degrees about the camera's axes)")]
    [Tooltip("The root's spring: Hz, and its damping ratio (under 1 = a weighted recovery that overshoots)")]
    public float viewmodelRootFreq = 4f;
    public float viewmodelRootDamping = 0.55f;
    [Tooltip("Every attack's lean (the cane's cuts, the cross, the hook, the shot, the maul) is scaled by this (0 = the root never leans)")]
    public float viewmodelRootLeanScale = 1f;
    [Tooltip("Over the run speed the root drops this far, tilts forward this many degrees and comes in this far (the sprint)")]
    public float viewmodelRootSprintDrop = 0.035f;
    public float viewmodelRootSprintPitch = 2.5f;
    public float viewmodelRootSprintPush = 0.02f;
    [Tooltip("The share of the landing's dip the root takes (the hands take all of it), and the forward tilt per metre of that dip (the compression)")]
    public float viewmodelRootLandShare = 0.5f;
    public float viewmodelRootLandPitch = 25f;
    [Tooltip("A slide drops the root, rolls it and tilts it forward (metres, degrees, degrees)")]
    public float viewmodelRootSlideDrop = 0.04f;
    public float viewmodelRootSlideRoll = 3.5f;
    public float viewmodelRootSlidePitch = 2f;
    [Tooltip("The share of the dash's pull-and-throw the root takes, and the degrees it turns per metre of it")]
    public float viewmodelRootDashShare = 0.5f;
    public float viewmodelRootDashRot = 30f;
    [Tooltip("The root rolls into a turn and nods with the pitch: degrees per degree-per-second of look")]
    public float viewmodelRootLookRoll = 0.012f;
    public float viewmodelRootLookPitch = 0.006f;
    [Tooltip("Idle: the slow shift of weight from foot to foot (metres sideways, Hz, degrees of roll)")]
    public float viewmodelRootShiftAmp = 0.006f;
    public float viewmodelRootShiftRate = 0.11f;
    public float viewmodelRootShiftRoll = 0.5f;

    [Header("ARM RIG — THE CAMERA'S FOLLOW (every attack kicks the view a few degrees IN the swing's direction and pushes it forward; PlayerLook's kicks, decaying; on top of GameFeel's)")]
    [Tooltip("Degrees of yaw and pitch per unit of the swing's screen travel, and the forward push in metres")]
    public float viewmodelFollowYaw = 6f;
    public float viewmodelFollowPitch = 3f;
    public float viewmodelFollowPush = 0.03f;
    [Tooltip("The maul's impact dips the view this many degrees; a blunderbuss shot pushes it back this far")]
    public float viewmodelMaulImpactDip = 3f;
    public float viewmodelGunCameraPushBack = 0.03f;

    [Header("ARM RIG — THE WEAPONS' OWN MOTION (THE FINALS' grammar per weapon)")]
    [Tooltip("THE CANE: the root leans this far along every cut (metres) and rolls this much into it; the free left hand sweeps the other way this far as a counterweight")]
    public float viewmodelCaneLean = 0.035f;
    public float viewmodelCaneLeanRoll = 2.5f;
    public float viewmodelCaneCounterSweep = 0.16f;
    [Tooltip("THE KNUCKLES: the cross turns the whole root this many degrees; the hook this many; the jab counters by a third")]
    public float viewmodelCrossRootYaw = 6f;
    public float viewmodelHookRootYaw = 9f;
    [Tooltip("THE DECK: the flick's arc (metres up at its top) and how far forward the hand sails")]
    public float viewmodelDeckFlickArc = 0.08f;
    public float viewmodelDeckFlickReach = 0.1f;
    [Tooltip("THE BLUNDERBUSS: the viewmodel's muzzle climb is the recoil's pitch times this (so a shot throws the mouth past the frame's top); the off hand slides back along the fore-end this far and re-seats; the root kicks back this far")]
    public float viewmodelGunRecoilMul = 2f;
    public float viewmodelGunReseat = 0.05f;
    public float viewmodelGunLean = 0.04f;
    [Tooltip("THE MAUL: the share of the windup the head spends going UP (the rest is the downswing, timed to land on the hit); the whiff's follow-through past the strike; the recovery's seconds")]
    public float viewmodelMaulWindShare = 0.55f;
    public float viewmodelMaulFollowThrough = 0.18f;
    public float viewmodelMaulRecover = 0.3f;   // 2026-10-16 feel pass: matches the 0.4 swing cooldown (was 0.55)
}
