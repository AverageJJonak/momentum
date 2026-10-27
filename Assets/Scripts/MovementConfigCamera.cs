using UnityEngine;

/// <summary>
/// THE BODY CAMERA's numbers (2026-10-23, CameraBody.cs): every layer the camera lays over the mouse look — the
/// stride, the landings and the roll, the wall run's yaw, the slide, the dash, the air, the look-ahead (Mirror's
/// Edge: the camera looks where the body goes) and the speed lean. Every amplitude here is scaled by the Settings'
/// CAMERA MOTION slider (CameraBody.Motion, 0..1, default 0.7); at 0 none of it exists. Nothing here is read in Awake.
/// Sign language, Unity's: a positive pitch looks DOWN, a positive roll leans LEFT (the wall run's lean off a wall
/// on the right is +roll). The limits at the foot keep it out of motion sickness.
/// </summary>
public partial class MovementConfig
{
    [Header("The speed lens")]
    [Tooltip("Degrees the view widens at full speed")]
    public float cameraSpeedFov = 7f;
    [Tooltip("The speed where it starts to widen, m/s")]
    public float cameraSpeedFovFrom = 8f;
    [Tooltip("The speed where it is all the way wide, m/s")]
    public float cameraSpeedFovAt = 18f;
    [Tooltip("How quickly it follows the speed (per second)")]
    public float cameraSpeedFovSmoothing = 6f;

    [Header("BODY CAMERA · STRIDE (the head bob: a figure-eight, only on the ground, growing with speed)")]
    [Tooltip("Vertical amplitude at run speed, m (twice the lateral frequency)")]
    public float cameraStrideVertical = 0.035f;
    [Tooltip("Lateral amplitude at run speed, m")]
    public float cameraStrideLateral = 0.02f;
    [Tooltip("Roll with the stride at run speed, degrees")]
    public float cameraStrideRoll = 0.6f;
    [Tooltip("The bob is silent under this speed and full at run speed, m/s")]
    public float cameraStrideStartSpeed = 2f;
    [Tooltip("Strides per second at run speed (the frequency scales with speed)")]
    public float cameraStrideRate = 3f;
    [Tooltip("Hz the bob fades in and out at (a landing, a lift-off)")]
    public float cameraStrideBlend = 7f;

    [Header("BODY CAMERA · LANDINGS (a dip and a nod by the impact, recovering on a spring) and THE ROLL (a hard landing tumbles the view forward)")]
    [Tooltip("Fall speed, m/s, at which the dip is the small one, and the dip then, m")]
    public float cameraLandFallMin = 6f;
    public float cameraLandDipMin = 0.06f;
    [Tooltip("Fall speed, m/s, at which the dip is the big one, and the dip then, m")]
    public float cameraLandFallMax = 14f;
    public float cameraLandDipMax = 0.16f;
    [Tooltip("The nod down with the small and the big dip, degrees")]
    public float cameraLandPitchMin = 1.5f;
    public float cameraLandPitchMax = 5f;
    [Tooltip("Seconds the dip takes to recover")]
    public float cameraLandRecover = 0.25f;
    [Tooltip("A fall at or over this, m/s (or any slam), is a HARD landing: the roll")]
    public float cameraRollFall = 14f;
    [Tooltip("The roll's length, s; its forward tumble, degrees down; the lean back it overshoots to, degrees; the crouch through it, m")]
    public float cameraRollSeconds = 0.42f;
    public float cameraRollDown = 38f;
    public float cameraRollBack = 6f;
    public float cameraRollDrop = 0.12f;

    [Header("BODY CAMERA · WALL RUN, SLIDE, DASH")]
    [Tooltip("Yaw toward the run's direction on a wall, degrees (the existing roll into the wall stays)")]
    public float cameraWallYaw = 6f;
    [Tooltip("The slide: the nod forward, degrees; the roll into its turn, degrees at full; the extra drop, m; the spring up, s")]
    public float cameraSlidePitch = 4f;
    public float cameraSlideRoll = 2f;
    public float cameraSlideDrop = 0.06f;
    public float cameraSlideUp = 0.2f;
    [Tooltip("The dash: the push along it, m; the FOV punch, degrees, in over the first and back over the second time; the roll into a lateral dash, degrees")]
    public float cameraDashPush = 0.06f;
    public float cameraDashFov = 6f;
    public float cameraDashFovIn = 0.1f;
    public float cameraDashFovOut = 0.25f;
    public float cameraDashRoll = 5f;

    [Header("BODY CAMERA · AIR (a drift down as the fall grows, a float at the apex, the double jump's and the springboard's lifts)")]
    [Tooltip("The nod down at a long fall, degrees, reached at this fall speed, m/s")]
    public float cameraAirPitchMax = 3f;
    public float cameraAirPitchFall = 20f;
    [Tooltip("The lift at the apex, m (full when the vertical speed is inside the band, m/s)")]
    public float cameraApexLift = 0.02f;
    public float cameraApexBand = 3f;
    [Tooltip("The double jump's kick up, m; the springboard's lift after the boot's dip, m")]
    public float cameraDoubleJumpLift = 0.03f;
    public float cameraSpringboardLift = 0.05f;

    [Header("BODY CAMERA · LOOK-AHEAD (the camera looks where the body goes; the mouse always wins)")]
    [Tooltip("Yaw toward the velocity when it turns away from the look: the cap, degrees; the fraction of the divergence taken")]
    public float cameraLookAheadYaw = 8f;
    public float cameraLookAheadYawGain = 0.25f;
    [Tooltip("The yaw bias is nothing under the first speed and full at the second, m/s")]
    public float cameraLookAheadSpeedStart = 4f;
    public float cameraLookAheadSpeedFull = 14f;
    [Tooltip("In the air: the nod toward the landing point, degrees at most, never up")]
    public float cameraLookAheadPitch = 6f;
    [Tooltip("Any mouse movement switches the look-ahead off for this long, s")]
    public float cameraLookAheadQuiet = 0.3f;
    [Tooltip("Hz the look-ahead drifts at (slow: a drift, never a snap)")]
    public float cameraLookAheadFreq = 1.2f;

    [Header("BODY CAMERA · SPEED LEAN (the body leans into speed, strafes and turns)")]
    [Tooltip("Forward lean at run speed, degrees, rising to the second at the fast speed, m/s")]
    public float cameraLeanPitchRun = 1.5f;
    public float cameraLeanPitchFast = 3f;
    public float cameraLeanFastSpeed = 16f;
    [Tooltip("Roll into a full sideways input, degrees")]
    public float cameraStrafeRoll = 2f;
    [Tooltip("Roll into a turn at speed: degrees per degree-per-second of yaw, and its cap, degrees")]
    public float cameraTurnRollGain = 0.012f;
    public float cameraTurnRollMax = 4f;

    [Header("BODY CAMERA · THE SPRINGS AND THE LIMITS (no motion sickness: everything springs, nothing snaps)")]
    [Tooltip("Hz and damping of the spring every continuous layer rides (lean, slide, air, wall)")]
    public float cameraBodyFreq = 5f;
    public float cameraBodyDamping = 0.9f;
    [Tooltip("Hz of the impulse springs (landing dip, dash push, kicks) — the recovery time follows the landing's")]
    public float cameraImpulseFreq = 4f;
    [Tooltip("The body's pitch and roll never pass these, degrees (the roll and the tricks are on top); the offset never passes this, m")]
    public float cameraBodyPitchMax = 8f;
    public float cameraBodyRollMax = 8f;
    public float cameraBodyOffsetMax = 0.2f;
}
