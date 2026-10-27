using UnityEngine;

/// <summary>
/// THE BODY CAMERA's layers (CameraBody.cs has the springs and the composition). Each one writes a target for the
/// body spring (targetRot in degrees, targetPos in the yaw frame), or its own state: the stride bob is added directly
/// (it is already smooth), the look-ahead has its own slow springs. Every number is a camera* field.
/// </summary>
public partial class CameraBody
{
    void Layers(float dt, float now)
    {
        var c = config;
        bool grounded = motor.Grounded, sliding = motor.Sliding, wall = motor.WallRunning;
        Vector3 v = motor.Velocity;
        float h = new Vector2(v.x, v.z).magnitude;
        float run = Mathf.Max(1f, c.baseRunSpeed);
        bool trick = TrickSystem.Instance != null && TrickSystem.Instance.Active;

        // The look's yaw rate and the velocity's heading rate, degrees per second, smoothed (a frozen frame reads as stillness)
        if (!yawInit) { prevYaw = look.Yaw; yawInit = true; }
        float dYaw = Mathf.DeltaAngle(prevYaw, look.Yaw);
        prevYaw = look.Yaw;
        float rawYawRate = dt > 1e-4f ? Mathf.Clamp(dYaw / dt, -720f, 720f) : 0f;
        yawRate = Mathf.Lerp(yawRate, rawYawRate, 1f - Mathf.Exp(-12f * dt));
        float heading = h > 0.5f ? Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg : look.Yaw;
        if (!headingInit) { prevHeading = heading; headingInit = true; }
        float rawHeadingRate = dt > 1e-4f && h > 0.5f ? Mathf.Clamp(Mathf.DeltaAngle(prevHeading, heading) / dt, -720f, 720f) : 0f;
        prevHeading = heading;
        headingRate = Mathf.Lerp(headingRate, rawHeadingRate, 1f - Mathf.Exp(-12f * dt));

        // STRIDE: the clock runs on speed, in the air too, so the bob picks up across a landing where it would have
        // been; the weight is on the ground (and on a wall, where the feet are), silent on a slide and in the air
        float stride01 = Mathf.InverseLerp(c.cameraStrideStartSpeed, run, h);
        if (h > c.cameraStrideStartSpeed) stridePhase += c.cameraStrideRate * (h / run) * 2f * Mathf.PI * dt;
        if (stridePhase > 2000f * Mathf.PI) stridePhase -= 2000f * Mathf.PI;
        bool stepping = (grounded && !sliding) || wall;
        Spring(ref strideW, ref strideWVel, stepping ? 1f : 0f, Omega(c.cameraStrideBlend), 1f, dt);
        float amp = stride01 * strideW;
        float lat = Mathf.Sin(stridePhase) * c.cameraStrideLateral * amp;
        float vert = Mathf.Sin(2f * stridePhase) * c.cameraStrideVertical * amp;
        strideRoll = Mathf.Sin(stridePhase) * c.cameraStrideRoll * amp;
        if (wall)
        {
            // The figure-eight's plane tilts with the lean off the wall: the feet are on the wall, not under you
            float r = motor.WallRunSide * c.wallRunCameraRoll * Mathf.Deg2Rad;
            float cs = Mathf.Cos(r), sn = Mathf.Sin(r);
            float l2 = lat * cs - vert * sn, v2 = lat * sn + vert * cs;
            lat = l2; vert = v2;
        }
        strideBob = new Vector3(lat, vert, 0f);

        // WALL RUN: a yaw toward the run's direction (the roll into the wall is PlayerLook's wallRoll, kept)
        if (wall) targetRot.y += Mathf.Clamp(Mathf.DeltaAngle(look.Yaw, heading) * 0.5f, -c.cameraWallYaw, c.cameraWallYaw);

        // SLIDE: down, a nod forward, a roll into the turn; the spring up when it ends
        Spring(ref slideW, ref slideWVel, sliding ? 1f : 0f, 4.7f / Mathf.Max(0.05f, c.cameraSlideUp), 1f, dt);
        targetRot.x += c.cameraSlidePitch * slideW;
        targetRot.z -= Mathf.Clamp(headingRate * 0.02f, -1f, 1f) * c.cameraSlideRoll * slideW;   // a turn to the right leans right (negative roll)
        targetPos.y -= c.cameraSlideDrop * slideW;

        // AIR: the nod down grows with the fall; a float at the apex
        if (!grounded && !wall)
        {
            float fall = Mathf.Max(0f, -v.y);
            targetRot.x += c.cameraAirPitchMax * Mathf.InverseLerp(0f, Mathf.Max(0.1f, c.cameraAirPitchFall), fall);
            float apex = 1f - Mathf.Clamp01(Mathf.Abs(v.y) / Mathf.Max(0.1f, c.cameraApexBand));
            targetPos.y += c.cameraApexLift * apex;
        }

        // LOOK-AHEAD: the camera looks where the body goes — a yaw toward the velocity when it turns away from the
        // look, a nod toward the landing point in the air. Any mouse movement blends both out for a beat (the mouse
        // always wins), and a trick owns the view. A body going backwards (past 120° off) is not looked after
        bool quiet = now >= mouseQuietUntil && !trick;
        aheadYawTarget = 0f;
        aheadPitchTarget = 0f;
        if (quiet)
        {
            if (!wall && h > c.cameraLookAheadSpeedStart)
            {
                float d = Mathf.DeltaAngle(look.Yaw, heading);
                float strength = Mathf.InverseLerp(c.cameraLookAheadSpeedStart, c.cameraLookAheadSpeedFull, h) * Mathf.Clamp01((120f - Mathf.Abs(d)) / 40f);
                aheadYawTarget = Mathf.Clamp(d * c.cameraLookAheadYawGain, -c.cameraLookAheadYaw, c.cameraLookAheadYaw) * strength;
            }
            var tricks = TrickSystem.Instance;
            if (!grounded && !wall && v.y < 0f && tricks != null && tricks.HasLanding)
            {
                Vector3 eye = motor.CurrPosition + Vector3.up * motor.EyeHeight;
                Vector3 to = tricks.LandingPoint - eye;
                float flat = new Vector2(to.x, to.z).magnitude;
                float pitchTo = Mathf.Atan2(-to.y, Mathf.Max(0.5f, flat)) * Mathf.Rad2Deg;   // + = down, Unity's way
                float fallIn = Mathf.Clamp01(-v.y / 6f);                                     // comes in with the fall
                aheadPitchTarget = Mathf.Clamp(pitchTo - look.Pitch, 0f, c.cameraLookAheadPitch) * fallIn;   // never up: only a nod toward the ground you are heading for
            }
        }

        // SPEED LEAN: a nod into speed, a roll into a strafe, a roll into a turn at speed
        float leanPitch = h <= run
            ? c.cameraLeanPitchRun * (h / run)
            : Mathf.Lerp(c.cameraLeanPitchRun, c.cameraLeanPitchFast, Mathf.InverseLerp(run, Mathf.Max(run + 0.1f, c.cameraLeanFastSpeed), h));
        targetRot.x += leanPitch;
        float strafe = (Keybinds.Held(GameAction.Right) ? 1f : 0f) - (Keybinds.Held(GameAction.Left) ? 1f : 0f);
        targetRot.z -= strafe * c.cameraStrafeRoll;
        targetRot.z -= Mathf.Clamp(yawRate * c.cameraTurnRollGain, -c.cameraTurnRollMax, c.cameraTurnRollMax) * Mathf.InverseLerp(2f, run, h);
    }
}
