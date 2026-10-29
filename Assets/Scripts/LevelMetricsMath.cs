using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// THE MOVEMENT'S ENVELOPE (Phase 1, 2026-10-27; the user: "a separate LevelMetrics ScriptableObject that reads my
/// movement values without changing them, and calculates the numbers a level designer needs"). Plain C#, no Unity in
/// it: the same sums run inside the editor (LevelMetrics.asset) and in Tools/LevelMetrics on the command line. The
/// numbers come from a tick-by-tick replay of the motor's own rules (PlayerMotor: gravity added each physics tick,
/// the air strafe's perpendicular push capped at airMaxWishSpeed, the wall run's gravity sagging back as it ages, the
/// perfect tiers fading with speed), not from textbook ballistics, so a gap sized from them is a gap the motor can
/// actually make. Nothing here writes to the config; `Values` is a copy.
/// </summary>
public static class LevelMetricsMath
{
    /// The motor's values the sums need, named exactly as MovementConfig names them (LevelMetrics copies them by
    /// name; the command-line tool reads them by name from the asset). The defaults are the code's defaults.
    [Serializable]
    public class Values
    {
        public float baseRunSpeed = 8f, groundAcceleration = 60f, groundFriction = 8f, bhopFrictionGraceTime = 0.1f;
        public float gravity = 25f, jumpImpulse = 9f, coyoteTime = 0.15f, jumpBufferTime = 0.15f;
        public float airAcceleration = 90f, airMaxWishSpeed = 1.2f, airControlTurnRate = 90f;
        public float slideActivationSpeed = 6f, slideFriction = 0.6f, slideSlopeAccelMultiplier = 1.5f, slideJumpVerticalImpulse = 7f;
        public float slideJumpHorizontalMultiplier = 1.15f, perfectSlideJumpMinMultiplier = 1.04f, slideJumpFadeStartSpeed = 14f, slideJumpFadeEndSpeed = 22f;
        public float slideEndSpeed = 1f, slideCapsuleHeight = 0.9f, fastfallExtraGravity = 10f;
        public float wallRunMinSpeed = 6f, wallRunMaxDuration = 1.6f, wallRunGravityFactor = 0.15f, wallRunEntryPop = 1.5f, wallRunCatchSpeed = 1f, wallRunRange = 0.7f;
        public float wallRunJumpNormalImpulse = 6f, wallRunJumpUpImpulse = 7f, wallRunJumpTangentBoost = 1.08f, wallRunJumpInputImpulse = 3f;
        public float wallKickNormalImpulse = 7f, wallKickUpImpulse = 6f, wallKickTangentKeep = 0.8f, wallKickInputImpulse = 3.5f;
        public float dashSpeed = 18f, dashDuration = 0.15f, doubleJumpImpulse = 8.5f, doubleJumpMinGroundClearance = 0.5f;
        public float slamDownSpeed = 30f, slamStorageConversion = 0.6f;
        public float perfectBhopSpeedMultiplier = 1.18f, perfectBhopMinMultiplier = 1.08f, perfectBhopFadeStartSpeed = 18f, perfectBhopFadeEndSpeed = 30f, perfectBhopWindow = 0.05f;
        public float capsuleHeight = 1.8f, capsuleRadius = 0.4f, maxSlopeAngle = 45f;
        public float cameraRollFall = 14f;
        public float styleSpeedPerRank = 0.035f, padSpeedPerRank = 0.12f;
        public float trickPerfectMultiplier = 1.12f, trickPerfectMinMultiplier = 1.05f, trickLandFloorSpeed = 8f;
        public int staminaMaxPips = 3;
        public float fixedDeltaTime = 0.02f;   // the physics tick the motor integrates at
    }

    public struct Arc { public float distance, time, apex; }

    // ---------------------------------------------------------------- the arcs

    /// A jump's arc: `speed` along the line at launch, `up` the vertical impulse, the landing `dy` metres above the
    /// launch (negative: below), with or without the air strafe (the perpendicular push every tick, the velocity
    /// turned back along the line: the theoretical best). Gravity is added before the move, as the motor does.
    public static Arc Jump(Values v, float speed, float up, float dy, bool strafe)
    {
        float dt = v.fixedDeltaTime, w = Math.Min(v.airAcceleration * dt, v.airMaxWishSpeed);
        float x = 0f, y = 0f, vx = speed, vy = up, t = 0f, apex = 0f;
        for (int i = 0; i < 20000; i++)
        {
            if (strafe) vx = (float)Math.Sqrt(vx * vx + w * w);
            vy -= v.gravity * dt;
            float ny = y + vy * dt;
            if (vy < 0f && ny <= dy && t > 0f)
            {
                float f = (y - dy) / (y - ny);   // the part of the tick down to the landing height
                x += vx * dt * f; t += dt * f; y = dy;
                break;
            }
            x += vx * dt; y = ny; t += dt;
            if (y > apex) apex = y;
        }
        return new Arc { distance = x, time = t, apex = apex };
    }

    /// The double jump's arc: the first jump, then the air jump (vertical set to doubleJumpImpulse, horizontal kept)
    /// at the apex (`late` false: the safe press) or at the last tick still above the landing height (`late` true:
    /// the furthest, over a gap where no ground is under you to turn the press into a buffered hop)
    public static Arc DoubleJump(Values v, float speed, float dy, bool late, bool strafe)
    {
        float dt = v.fixedDeltaTime, w = Math.Min(v.airAcceleration * dt, v.airMaxWishSpeed);
        float x = 0f, y = 0f, vx = speed, vy = v.jumpImpulse, t = 0f, apex = 0f;
        bool jumped = false;
        for (int i = 0; i < 20000; i++)
        {
            if (strafe) vx = (float)Math.Sqrt(vx * vx + w * w);
            float vyBefore = vy;
            vy -= v.gravity * dt;
            if (!jumped)
            {
                bool atApex = vyBefore > 0f && vy <= 0f;
                bool lastAbove = late && vy < 0f && y + vy * dt <= dy;   // the next tick would pass the landing height
                if ((!late && atApex) || lastAbove) { vy = v.doubleJumpImpulse; jumped = true; }
            }
            float ny = y + vy * dt;
            if (jumped && vy < 0f && ny <= dy)
            {
                float f = (y - dy) / (y - ny);
                x += vx * dt * f; t += dt * f; y = dy;
                break;
            }
            x += vx * dt; y = ny; t += dt;
            if (y > apex) apex = y;
        }
        return new Arc { distance = x, time = t, apex = apex };
    }

    /// The perfect bhop's multiplier at a speed (full below the fade's start, the floor past its end)
    public static float BhopMultiplier(Values v, float speed) => Lerp(v.perfectBhopSpeedMultiplier, v.perfectBhopMinMultiplier, InverseLerp(v.perfectBhopFadeStartSpeed, v.perfectBhopFadeEndSpeed, speed));

    /// The perfect slide jump's horizontal multiplier at a speed (the same fade; the lazy press fades to ×1 between the slide-jump fade speeds)
    public static float SlideJumpMultiplier(Values v, float speed, bool perfect) => perfect
        ? Lerp(v.slideJumpHorizontalMultiplier, v.perfectSlideJumpMinMultiplier, InverseLerp(v.perfectBhopFadeStartSpeed, v.perfectBhopFadeEndSpeed, speed))
        : Lerp(v.slideJumpHorizontalMultiplier, 1f, InverseLerp(v.slideJumpFadeStartSpeed, v.slideJumpFadeEndSpeed, speed));

    /// A perfect trick landing's pay at a landing speed: the floor, the tier, the cap of the perfect hop
    public static float TrickLandingSpeed(Values v, float h)
    {
        float tier = Lerp(v.trickPerfectMultiplier, v.trickPerfectMinMultiplier, InverseLerp(v.perfectBhopFadeStartSpeed, v.perfectBhopFadeEndSpeed, h));
        float cap = Math.Max(h, v.baseRunSpeed) * BhopMultiplier(v, h);
        return Math.Min(Math.Max(h, v.trickLandFloorSpeed) * tier, cap);
    }

    // ---------------------------------------------------------------- the wall

    public struct WallRun { public float distance, drop, levelDistance, seconds, exitVy; }

    /// A wall run from `speed` along the wall, entered falling at `vyIn` (the wall catches the fall at
    /// -wallRunCatchSpeed, then the entry pop): the run keeps its pace (the motor only re-accelerates along the wall
    /// up to the speed you had), gravity sags from the run's factor back to full as it ages, and it ends at
    /// wallRunMaxDuration. `levelDistance` is how far you get before you are back below the entry height.
    public static WallRun Run(Values v, float speed, float vyIn)
    {
        float dt = v.fixedDeltaTime, T = v.wallRunMaxDuration;
        float vy = Math.Max(vyIn, -v.wallRunCatchSpeed) + v.wallRunEntryPop, y = 0f, x = 0f, t = 0f, level = -1f;
        for (int i = 0; t < T && i < 20000; i++)
        {
            float life = Math.Min(1f, t / T);
            float g = v.gravity * Lerp(v.wallRunGravityFactor, 1f, life * life);
            vy -= g * dt;
            y += vy * dt; x += speed * dt; t += dt;
            if (level < 0f && vy < 0f && y <= 0f) level = x;
        }
        return new WallRun { distance = x, drop = -y, levelDistance = level < 0f ? x : level, seconds = t, exitVy = vy };
    }

    /// The wall jump off a run: tangent × the boost, the normal impulse plus the input held away from the wall, the
    /// up impulse plus half of any rise you had. Returns (horizontal speed, vertical speed)
    public static (float h, float vy) WallJump(Values v, float speed, float vy)
    {
        float along = speed * v.wallRunJumpTangentBoost, away = v.wallRunJumpNormalImpulse + v.wallRunJumpInputImpulse;
        return ((float)Math.Sqrt(along * along + away * away), Math.Max(vy, 0f) * 0.5f + v.wallRunJumpUpImpulse);
    }

    /// A wall kick mid-air: the tangent kept at wallKickTangentKeep, the normal impulse plus the input held away
    public static (float h, float vy) WallKick(Values v, float speed)
    {
        float along = speed * v.wallKickTangentKeep, away = v.wallKickNormalImpulse + v.wallKickInputImpulse;
        return ((float)Math.Sqrt(along * along + away * away), v.wallKickUpImpulse);
    }

    public struct Lift { public float rise, tRise, exitVy, run, afterJump, afterDouble, afterKick; public bool toTop; }

    /// THE LIFT (2026-10-29; the user: "wall running gives too much vertical momentum"). A wall run keeps whatever rise
    /// it is entered with (the motor only catches a fall at -wallRunCatchSpeed) and adds the entry pop; gravity is
    /// nearly off at first; the wall jump off it adds half of any rise still there to its own impulse; a wall touch
    /// refunds the double jump. So a run entered with `vyIn` up (jumpImpulse when you jump at the wall, 0 running in
    /// level, negative when it catches a fall) beside a wall whose top is `wallTopAbove` the attach height (the run
    /// ends when the feet pass it; the jump off still comes within the coyote) — or under a `cornice` that high (the
    /// head stops the rise there, as against any ceiling) — lifts: the run's rise to its best moment to jump, the
    /// wall jump's apex over the attach height, the double jump after it (a pip), a wall kick off a second wall (a pip)
    public static Lift WallLift(Values v, float vyIn, float wallTopAbove, float cornice = float.PositiveInfinity)
    {
        float dt = v.fixedDeltaTime, T = v.wallRunMaxDuration;
        float vy = Math.Max(vyIn, -v.wallRunCatchSpeed) + v.wallRunEntryPop, y = 0f, t = 0f;
        float headroom = Math.Max(0f, cornice - v.capsuleHeight);
        var best = new Lift(); float bestApex = float.NegativeInfinity;
        void Consider()
        {
            float up = Math.Max(vy, 0f) * 0.5f + v.wallRunJumpUpImpulse;
            float apex = y + up * up / (2f * v.gravity);
            if (apex > bestApex) { bestApex = apex; best.rise = y; best.tRise = t; best.exitVy = vy; best.afterJump = apex; }
        }
        Consider();
        for (int i = 0; t < T && i < 20000; i++)
        {
            float life = Math.Min(1f, t / T);
            float g = v.gravity * Lerp(v.wallRunGravityFactor, 1f, life * life);
            vy -= g * dt;
            float ny = y + vy * dt;
            if (ny > headroom) { ny = headroom; vy = Math.Min(vy, 0f); }
            y = ny; t += dt;
            if (y > wallTopAbove) { best.toTop = true; break; }
            Consider();
        }
        best.run = t;
        best.afterDouble = best.afterJump + v.doubleJumpImpulse * v.doubleJumpImpulse / (2f * v.gravity);
        best.afterKick = best.afterDouble + v.wallKickUpImpulse * v.wallKickUpImpulse / (2f * v.gravity);
        if (!float.IsPositiveInfinity(cornice))   // a ceiling over the run is over the jumps too: the head stops every rise at it
        {
            best.afterJump = Math.Min(best.afterJump, headroom);
            best.afterDouble = Math.Min(best.afterDouble, headroom);
            best.afterKick = Math.Min(best.afterKick, headroom);
        }
        return best;
    }

    // ---------------------------------------------------------------- the ground

    /// A flat slide from `speed`: the bleed is exponential at slideFriction, the slide ends at slideEndSpeed.
    /// Returns (distance, seconds)
    public static (float distance, float seconds) SlideFlat(Values v, float speed)
    {
        if (speed <= v.slideEndSpeed) return (0f, 0f);
        return ((speed - v.slideEndSpeed) / v.slideFriction, (float)Math.Log(speed / v.slideEndSpeed) / v.slideFriction);
    }

    /// Sliding straight down a slope of `degrees`: the friction fades to nothing pointed downhill and gravity's
    /// along-slope part × the multiplier is added — speed gained per second
    public static float SlideSlopeGain(Values v, float degrees) => v.gravity * (float)Math.Sin(degrees * Math.PI / 180.0) * v.slideSlopeAccelMultiplier;

    /// Sliding UP a slope of `degrees` from `speed`: the same along-slope gravity against you (friction off while
    /// moving, the motor's down-slope test reads uphill as flat friction too, so a little bleed) — the run before you stop, in metres
    public static float SlideUpRun(Values v, float speed, float degrees)
    {
        float a = SlideSlopeGain(v, degrees);
        return a <= 0f ? float.PositiveInfinity : speed * speed / (2f * a);
    }

    /// On the ground above the run speed without a hop: friction's share of the speed kept after `seconds`
    public static float GroundKeep(Values v, float seconds) => (float)Math.Exp(-v.groundFriction * seconds);

    // ---------------------------------------------------------------- drops, dashes, slams, pads

    public static float FallSpeed(Values v, float height) => (float)Math.Sqrt(2f * v.gravity * height);
    public static float FallTime(Values v, float height, bool fastfall) => (float)Math.Sqrt(2f * height / (v.gravity + (fastfall ? v.fastfallExtraGravity : 0f)));
    public static float RollHeight(Values v) => v.cameraRollFall * v.cameraRollFall / (2f * v.gravity);
    public static float Height(Values v, float up) => up * up / (2f * v.gravity);

    /// What a slam stores from a drop (capped at the slam's own speed) and pays on a perfect press
    public static float SlamPay(Values v, float drop) => Math.Min(v.slamDownSpeed, FallSpeed(v, drop)) * v.slamStorageConversion;

    /// The run speed at a style rank
    public static float RunSpeed(Values v, int rank) => v.baseRunSpeed * (1f + v.styleSpeedPerRank * rank);

    // ---------------------------------------------------------------- the report

    /// Every table a designer needs, as text (Markdown tables: the console and the inspector show them as they are)
    public static string Report(Values v, float[] entrySpeeds, float[] slopes, float[] drops)
    {
        var sb = new StringBuilder();
        var ci = CultureInfo.InvariantCulture;
        string F(float x, string f = "0.0") => x.ToString(f, ci);

        sb.AppendLine("LEVEL METRICS — the movement's envelope, from the motor's own rules (tick " + F(v.fixedDeltaTime, "0.000") + " s, gravity " + F(v.gravity) + ", jump " + F(v.jumpImpulse) + ", run " + F(v.baseRunSpeed) + " m/s)");
        sb.AppendLine();
        sb.AppendLine("THE BODY: capsule " + F(v.capsuleHeight) + " m tall, " + F(v.capsuleRadius * 2f) + " m wide; sliding " + F(v.slideCapsuleHeight) + " m tall; NO step-up (any ledge is a wall: bevel it); walkable slope up to " + F(v.maxSlopeAngle, "0") + "°; "
            + "ceilings under " + F(v.capsuleHeight + 0.1f) + " m stop a run, under " + F(v.slideCapsuleHeight + 0.1f) + " m stop a slide; openings under " + F(v.capsuleRadius * 2f + 0.2f) + " m wide stop everything.");
        sb.AppendLine("THE GROUND: friction keeps " + F(GroundKeep(v, 0.1f) * 100f, "0") + "% of any speed over " + F(v.baseRunSpeed) + " after 0.1 s and " + F(GroundKeep(v, 0.25f) * 100f, "0") + "% after 0.25 s — a landing without a hop inside the " + F(v.bhopFrictionGraceTime, "0.00") + " s grace throws the extra away. Running down a slope gains nothing (speed is aligned, not grown); sliding down it does.");
        sb.AppendLine();

        // 1. jumps
        sb.AppendLine("1. JUMPS, flat landing (same height). 'clean' = no air strafe; 'strafe' = the theoretical best strafe every tick. Design gaps at 70–90% of the clean number for the main line, up to the strafe number for a skip.");
        sb.AppendLine("| entry m/s | plain clean | plain strafe | apex | air s | bhop(perfect) next | slide-jump clean | slide-jump apex | double @apex | double late | design gap 70–90% |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (float s in entrySpeeds)
        {
            var plain = Jump(v, s, v.jumpImpulse, 0f, false);
            var strafe = Jump(v, s, v.jumpImpulse, 0f, true);
            float sj = SlideJumpMultiplier(v, s, true) * s;
            var slide = Jump(v, sj, v.slideJumpVerticalImpulse, 0f, false);
            var dApex = DoubleJump(v, s, 0f, false, false);
            var dLate = DoubleJump(v, s, 0f, true, false);
            sb.AppendLine("| " + F(s) + " | " + F(plain.distance) + " m | " + F(strafe.distance) + " m | " + F(plain.apex, "0.00") + " m | " + F(plain.time, "0.00") + " | ×" + F(BhopMultiplier(v, s), "0.00") + " → " + F(s * BhopMultiplier(v, s)) + " | "
                + F(slide.distance) + " m @" + F(sj) + " | " + F(slide.apex, "0.00") + " m | " + F(dApex.distance) + " m | " + F(dLate.distance) + " m | " + F(plain.distance * 0.7f) + "–" + F(plain.distance * 0.9f) + " m |");
        }
        sb.AppendLine();

        // 2. landing lower / higher
        sb.AppendLine("2. PLAIN JUMP TO A LANDING OF ANOTHER HEIGHT (clean): the distance covered when the landing is below (−) or above (+) the launch; '—' = cannot reach that height");
        sb.AppendLine("| entry m/s | −6 m | −3 m | −1 m | 0 | +1 m | +1.5 m |");
        sb.AppendLine("|---|---|---|---|---|---|---|");
        float[] dys = { -6f, -3f, -1f, 0f, 1f, 1.5f };
        foreach (float s in entrySpeeds)
        {
            sb.Append("| " + F(s) + " |");
            foreach (float dy in dys)
            {
                var a = Jump(v, s, v.jumpImpulse, dy, false);
                sb.Append(a.apex + 0.001f < dy ? " — |" : " " + F(a.distance) + " m |");
            }
            sb.AppendLine();
        }
        sb.AppendLine();

        // 3. walls
        sb.AppendLine("3. WALL RUN (entered from level flight) and the jump off it. The run keeps its pace, never gains; gravity sags back over its " + F(v.wallRunMaxDuration, "0.0") + " s; it needs " + F(v.wallRunMinSpeed) + " m/s and a wall within " + F(v.wallRunRange, "0.0") + " m. 'level' = how far before you are back under the height you attached at.");
        sb.AppendLine("| entry m/s | run 0.5 s | run 1.0 s | full run | drop over full | level distance | wall jump → h, up | wall kick → h, up |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (float s in entrySpeeds)
        {
            var r = Run(v, s, 0f);
            var wj = WallJump(v, s, r.exitVy);
            var wk = WallKick(v, s);
            sb.AppendLine("| " + F(s) + " | " + F(s * 0.5f) + " m | " + F(s * 1f) + " m | " + F(r.distance) + " m | " + F(r.drop) + " m | " + F(r.levelDistance) + " m | " + F(wj.h) + ", " + F(wj.vy) + " m/s | " + F(wk.h) + ", " + F(wk.vy) + " m/s |");
        }
        var r8 = Run(v, v.baseRunSpeed, 0f);
        sb.AppendLine("A full run drops " + F(r8.drop) + " m whatever the speed; the first " + F(r8.levelDistance / v.baseRunSpeed, "0.00") + " s stay level. The wall jump's arc then lasts " + F(2f * v.wallRunJumpUpImpulse / v.gravity, "0.00") + " s (" + F(Height(v, v.wallRunJumpUpImpulse), "0.00") + " m up). Staggered walls want a gap of about " + F(2f * v.wallRunJumpUpImpulse / v.gravity * 0.8f * v.baseRunSpeed) + "–" + F(2f * v.wallRunJumpUpImpulse / v.gravity * 0.9f * 14f) + " m between faces at 8–14 m/s.");
        sb.AppendLine();

        // 3b. the lift
        sb.AppendLine("3b. THE LIFT: what a wall run gives in HEIGHT, by how it is entered and how tall the wall is (metres over the point where the body meets the wall; speed does not change the height, only how far along the wall it happens). 'jump' = the run's best jump off it, 'pip' = a double jump after that, 'kick' = a wall kick off a second wall after that. The run keeps the rise it is entered with and adds the pop of " + F(v.wallRunEntryPop) + " m/s; the wall jump adds half of any rise left to its " + F(v.wallRunJumpUpImpulse) + ".");
        sb.AppendLine("| entered | wall 3 m | wall 4 m | wall 5 m | wall 6 m | wall 8 m | no top | under a 3 m cornice | ");
        sb.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (var (name, vyIn) in new[] { ("jumping at it (" + F(v.jumpImpulse) + " up)", v.jumpImpulse), ("half a jump up (" + F(v.jumpImpulse * 0.5f) + ")", v.jumpImpulse * 0.5f), ("level (0)", 0f), ("falling (-3)", -3f) })
        {
            sb.Append("| " + name + " |");
            foreach (float top in new[] { 3f, 4f, 5f, 6f, 8f, 1000f })
            {
                var l = WallLift(v, vyIn, top);
                sb.Append(" rise " + F(l.rise) + " in " + F(l.tRise, "0.00") + " s → jump " + F(l.afterJump) + ", pip " + F(l.afterDouble) + ", kick " + F(l.afterKick) + " |");
            }
            var c = WallLift(v, vyIn, 1000f, 3f);
            sb.AppendLine(" rise " + F(c.rise) + " → jump " + F(c.afterJump) + ", pip " + F(c.afterDouble) + ", kick " + F(c.afterKick) + " |");
        }
        var lj = WallLift(v, v.jumpImpulse, 1000f);
        sb.AppendLine("Along the wall, the best jump off a run entered jumping comes " + F(lj.tRise, "0.00") + " s in: " + F(lj.tRise * 8f) + " m at 8 m/s, " + F(lj.tRise * 12f) + " m at 12, " + F(lj.tRise * 16f) + " m at 16. A wall's top is reached " + F(WallLift(v, v.jumpImpulse, 4f).run, "0.00") + " s in when it is 4 m up. A cornice 3 m over the floor holds the feet to " + F(3f - v.capsuleHeight) + " m.");
        sb.AppendLine();

        // 4. slides and slopes
        sb.AppendLine("4. SLIDES. Flat: the bleed at " + F(v.slideFriction, "0.00") + "/s until " + F(v.slideEndSpeed) + " m/s. Slopes: speed gained per second sliding straight down; the run up a slope before a slide stops, from 12 m/s.");
        sb.AppendLine("| entry m/s | flat slide distance | flat slide seconds |");
        sb.AppendLine("|---|---|---|");
        foreach (float s in entrySpeeds) { var sl = SlideFlat(v, s); sb.AppendLine("| " + F(s) + " | " + F(sl.distance) + " m | " + F(sl.seconds) + " s |"); }
        sb.AppendLine("| slope | gain per second (down) | after 2 s from 8 m/s | run up before stopping, from 12 m/s | from 16 m/s |");
        sb.AppendLine("|---|---|---|---|---|");
        foreach (float deg in slopes) sb.AppendLine("| " + F(deg, "0") + "° | +" + F(SlideSlopeGain(v, deg)) + " m/s² | " + F(v.baseRunSpeed + 2f * SlideSlopeGain(v, deg)) + " m/s | " + F(SlideUpRun(v, 12f, deg)) + " m | " + F(SlideUpRun(v, 16f, deg)) + " m |");
        sb.AppendLine();

        // 5. drops
        sb.AppendLine("5. DROPS. No fall damage. The camera rolls on landings over " + F(v.cameraRollFall) + " m/s, a drop of " + F(RollHeight(v)) + " m. A flat landing keeps all horizontal speed for " + F(v.bhopFrictionGraceTime, "0.00") + " s, then friction; a slope landing keeps the fall's share along the slope. 'carried' = the gap you cross while falling that far at 8 / 12 / 16 m/s.");
        sb.AppendLine("| drop | fall s | fall s (fastfall) | landing m/s | carried @8 | @12 | @16 | slam-jump pay (perfect) |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (float d in drops)
        {
            float t = FallTime(v, d, false);
            sb.AppendLine("| " + F(d) + " m | " + F(t, "0.00") + " | " + F(FallTime(v, d, true), "0.00") + " | " + F(FallSpeed(v, d)) + " | " + F(8f * t) + " m | " + F(12f * t) + " m | " + F(16f * t) + " m | +" + F(SlamPay(v, d)) + " m/s |");
        }
        sb.AppendLine();

        // 6. gains and losses at 12 m/s
        float e = 12f;
        sb.AppendLine("6. WHAT EACH TECHNIQUE DOES TO SPEED, from " + F(e) + " m/s (the pip costs from " + v.staminaMaxPips + " pips):");
        sb.AppendLine("| technique | result | cost |");
        sb.AppendLine("|---|---|---|");
        sb.AppendLine("| plain jump / lazy bhop | " + F(e) + " m/s kept | — |");
        sb.AppendLine("| perfect bhop (±" + F(v.perfectBhopWindow * 1000f, "0") + " ms) | ×" + F(BhopMultiplier(v, e), "0.00") + " → " + F(e * BhopMultiplier(v, e)) + " m/s | — |");
        sb.AppendLine("| perfect slide jump | ×" + F(SlideJumpMultiplier(v, e, true), "0.00") + " → " + F(e * SlideJumpMultiplier(v, e, true)) + " m/s, " + F(Height(v, v.slideJumpVerticalImpulse), "0.00") + " m up | — |");
        sb.AppendLine("| lazy slide jump | ×" + F(SlideJumpMultiplier(v, e, false), "0.00") + " → " + F(e * SlideJumpMultiplier(v, e, false)) + " m/s | — |");
        sb.AppendLine("| wall run | " + F(e) + " m/s kept, " + F(Run(v, e, 0f).drop) + " m lost in height over a full run | — |");
        var wj12 = WallJump(v, e, 0f); var wk12 = WallKick(v, e);
        sb.AppendLine("| wall jump (off a run, held away) | " + F(wj12.h) + " m/s, " + F(wj12.vy) + " m/s up | — |");
        sb.AppendLine("| wall kick (mid-air, held away) | " + F(wk12.h) + " m/s turned off the wall, " + F(wk12.vy) + " m/s up | first free, then 1 pip |");
        sb.AppendLine("| dash | " + F(Math.Max(v.dashSpeed, e)) + " m/s for " + F(v.dashDuration, "0.00") + " s and kept in the air (" + (e < v.dashSpeed ? "+" + F(v.dashSpeed - e) : "no gain above " + F(v.dashSpeed)) + ") | 1 pip |");
        sb.AppendLine("| double jump | " + F(e) + " m/s kept, redirected to the stick, " + F(Height(v, v.doubleJumpImpulse), "0.00") + " m up | 1 pip |");
        sb.AppendLine("| slam then perfect slam-jump, 6 m drop | +" + F(SlamPay(v, 6f)) + " m/s along the stick | 1 pip, refunded |");
        sb.AppendLine("| perfect trick landing | " + F(TrickLandingSpeed(v, e)) + " m/s (×" + F(TrickLandingSpeed(v, e) / e, "0.00") + ") | — |");
        sb.AppendLine("| missed trick | ×0.35 and a stumble | — |");
        sb.AppendLine("| landing without a hop, 0.25 s on the ground | " + F(Math.Max(v.baseRunSpeed, e * GroundKeep(v, 0.25f))) + " m/s | — |");
        sb.AppendLine("| sliding up a 17° ramp | stops after " + F(SlideUpRun(v, e, 17f)) + " m | — |");
        sb.AppendLine("| running up any ramp above run speed | friction: back to " + F(v.baseRunSpeed) + " m/s within ~0.3 s | — |");
        sb.AppendLine();

        // 7. speed ladders
        sb.AppendLine("7. LADDERS. Perfect hops from run speed (each landing timed): " + Chain(v, v.baseRunSpeed, 8, ci) + ". Run speed by style rank: " + Ranks(v, 6, ci) + ".");
        sb.AppendLine("Pads: a jump pad with up U rises U²/(2g): 16 → " + F(Height(v, 16f)) + " m, 19 → " + F(Height(v, 19f)) + " m; a momentum pad fires max(speed, floor 8) × (1 + " + F(v.padSpeedPerRank, "0.00") + " × rank).");
        return sb.ToString();
    }

    static string Chain(Values v, float start, int hops, CultureInfo ci)
    {
        var parts = new List<string>();
        float s = start;
        for (int i = 0; i < hops; i++) { s *= BhopMultiplier(v, s); parts.Add(s.ToString("0.0", ci)); }
        return string.Join(" → ", parts) + " m/s";
    }

    static string Ranks(Values v, int ranks, CultureInfo ci)
    {
        var parts = new List<string>();
        for (int r = 0; r < ranks; r++) parts.Add(r + ": " + RunSpeed(v, r).ToString("0.00", ci));
        return string.Join(", ", parts);
    }

    static float Lerp(float a, float b, float t) => a + (b - a) * Math.Max(0f, Math.Min(1f, t));
    static float InverseLerp(float a, float b, float x) => Math.Abs(b - a) < 1e-6f ? 0f : Math.Max(0f, Math.Min(1f, (x - a) / (b - a)));
}
