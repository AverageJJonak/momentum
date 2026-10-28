using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE BEAT LIBRARY (Phase 3, 2026-10-27). Eleven beats, each a thing the movement does, each sized from the motor's
/// own numbers (LevelMetricsMath through BeatMath) for the speed it expects, at three difficulties: I is for anyone
/// at run speed, II wants 10–12 m/s, III wants 14–16. Every beat builds forward from the frame's origin on a floor
/// that ends where the beat begins, and leaves the frame at its exit on the floor that follows, with the heading
/// and the speed it hands on. THE RULES EVERY BEAT KEEPS (the user's, from the Phase 2 findings): no uphill exits,
/// no lips or kerbs, no flat landing after a big drop (a drop lands on a slope that turns the fall into speed), no gap
/// that run speed clears with tons to spare (gaps sit at 70–90% of the reach at the expected speed), and walls that
/// chain wall runs stand 4–7 m apart.
/// </summary>
public abstract class Beat
{
    public abstract string Name { get; }
    /// The speed the beat is sized for at difficulty `d` (1–3)
    public abstract float EntrySpeed(LevelMetricsMath.Values v, int d);
    /// What the sign over the reset pad says about this one
    public abstract string Note(LevelMetricsMath.Values v, int d);
    /// Builds the beat from the frame's origin; moves the frame to the exit; returns the speed it hands on
    public abstract float Build(Frame f, int d);
    protected const float HalfW = 5f;
    protected static float Pick(int d, float a, float b, float c) => d <= 1 ? a : d == 2 ? b : c;
    protected static string F1(float x) => x.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
    protected static string F0(float x) => x.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
}

/// GAP JUMP: a floor, a gap at 70 / 80 / 90% of the reach at the expected speed, a floor level or a little lower
public class GapJump : Beat
{
    public override string Name => "GAP JUMP";
    public override float EntrySpeed(LevelMetricsMath.Values v, int d) => Pick(d, v.baseRunSpeed, 12f, 16f);
    public override string Note(LevelMetricsMath.Values v, int d) { float s = EntrySpeed(v, d), dy = Pick(d, 0f, -1f, -2f); return "a gap of " + F1(BeatMath.Gap(v, s, dy, Pick(d, 0.7f, 0.8f, 0.9f))) + " m at " + F0(s) + " m/s"; }
    public override float Build(Frame f, int d)
    {
        float s = EntrySpeed(f.v, d), dy = Pick(d, 0f, -1f, -2f), gap = BeatMath.Gap(f.v, s, dy, Pick(d, 0.7f, 0.8f, 0.9f));
        f.Deck(0f, 8f, 0f, HalfW, "Launch"); f.Line(0f, 0.5f, 7.5f, 0f);
        f.Deck(8f + gap, 18f + gap, dy, HalfW, "Landing"); f.Line(0f, 8.5f + gap, 17.5f + gap, dy);
        f.Advance(18f + gap, dy);
        return s;
    }
}

/// WALLRUN CHAIN: two to four walls staggered left and right over a void, 4 / 5.5 / 7 m apart along the line, each
/// eight metres long; you run one, jump across to the next, land on the far floor a little lower
public class WallrunChain : Beat
{
    public override string Name => "WALLRUN CHAIN";
    public override float EntrySpeed(LevelMetricsMath.Values v, int d) => Pick(d, v.baseRunSpeed, 10f, 12f);
    public override string Note(LevelMetricsMath.Values v, int d) => Pick(d, 2, 3, 4) + " walls, " + F1(Pick(d, 4f, 5.5f, 7f)) + " m apart";
    public override float Build(Frame f, int d)
    {
        float s = EntrySpeed(f.v, d), between = Pick(d, 4f, 5.5f, 7f);
        int n = (int)Pick(d, 2, 3, 4);
        const float lane = 2.6f, wallLen = 8f;
        f.Deck(0f, 8f, 0f, lane, "Launch"); f.Line(0f, 0.5f, 7.5f, 0f);
        float z = 6f;
        for (int i = 0; i < n; i++)
        {
            float side = i % 2 == 0 ? 1f : -1f;
            f.Wall(side * (lane + 0.6f), z, z + wallLen, 0.3f - i * 0.4f, 4f);
            z += wallLen + between;
        }
        float dy = -1f - 0.4f * n;
        float zLand = z - between + 2f;
        f.Deck(zLand, zLand + 12f, dy, HalfW, "Landing"); f.Line(0f, zLand + 0.5f, zLand + 11.5f, dy);
        f.Advance(zLand + 12f, dy);
        return s;
    }
}

/// SLIDE TUNNEL: a roof a slide passes under and a run cannot, over a floor that drops ten degrees so the slide
/// gains; the sign says slide
public class SlideTunnel : Beat
{
    public override string Name => "SLIDE TUNNEL";
    public override float EntrySpeed(LevelMetricsMath.Values v, int d) => Pick(d, v.baseRunSpeed, 10f, 12f);
    public override string Note(LevelMetricsMath.Values v, int d) => F0(Pick(d, 10f, 16f, 24f)) + " m under a " + F1(1.25f) + " m roof, downhill: SLIDE";
    public override float Build(Frame f, int d)
    {
        float s = EntrySpeed(f.v, d), len = Pick(d, 10f, 16f, 24f);
        const float deg = 10f, clear = 1.25f;
        float dz = len * BeatMath.Cos(deg), drop = len * BeatMath.Sin(deg);
        f.Deck(0f, 3f, 0f, HalfW, "Mouth");
        f.Slope(3f, 0f, 3f + dz, -drop, HalfW, "TunnelFloor");
        f.Roof(2f, 0f + 0.18f, 3f + dz, -drop + 0.18f, clear, HalfW + 1.5f, "TunnelRoof");
        for (int side = -1; side <= 1; side += 2)
            f.Slope(2f, 0.5f + clear, 3f + dz, 0.5f + clear - drop, 0.75f, "TunnelSide", side * (HalfW + 0.75f), clear + 2f);
        f.Line(0f, 3.5f, 3f + dz - 0.5f, -drop * 0.5f);
        f.Advance(3f + dz, -drop);
        return BeatMath.SlideGain(f.v, s, deg, len);
    }
}

/// RAMP LAUNCH: a slope you slide to gather speed, its edge the lip of a gap sized for that speed, a slope to land on
public class RampLaunch : Beat
{
    public override string Name => "RAMP LAUNCH";
    public override float EntrySpeed(LevelMetricsMath.Values v, int d) => v.baseRunSpeed;
    const float Deg = 14f;
    public override string Note(LevelMetricsMath.Values v, int d) { float len = Pick(d, 8f, 12f, 16f), lip = BeatMath.SlideGain(v, v.baseRunSpeed, Deg, len); return "SLIDE the " + F0(len) + " m ramp to " + F0(lip) + " m/s, jump the " + F1(BeatMath.Gap(v, lip, -2f, Pick(d, 0.7f, 0.8f, 0.9f))) + " m gap"; }
    public override float Build(Frame f, int d)
    {
        float len = Pick(d, 8f, 12f, 16f), lip = BeatMath.SlideGain(f.v, f.v.baseRunSpeed, Deg, len);
        float dz = len * BeatMath.Cos(Deg), drop = len * BeatMath.Sin(Deg);
        float gap = BeatMath.Gap(f.v, lip, -2f, Pick(d, 0.7f, 0.8f, 0.9f));
        f.Slope(0f, 0f, dz, -drop, HalfW, "LaunchRamp");
        float yLand = -drop - 2f, landLen = 10f, landDrop = landLen * BeatMath.Sin(10f), landDz = landLen * BeatMath.Cos(10f);
        f.Slope(dz + gap, yLand, dz + gap + landDz, yLand - landDrop, HalfW, "LandingSlope");
        f.Deck(dz + gap + landDz, dz + gap + landDz + 6f, yLand - landDrop, HalfW, "Landing");
        f.Advance(dz + gap + landDz + 6f, yLand - landDrop);
        return lip;
    }
}

/// DROP: an edge, a fall of 4 / 8 / 14 m onto a 28-degree slope that turns the fall into speed down the line
public class Drop : Beat
{
    public override string Name => "DROP";
    public override float EntrySpeed(LevelMetricsMath.Values v, int d) => v.baseRunSpeed;
    public override string Note(LevelMetricsMath.Values v, int d) { float h = Pick(d, 4f, 8f, 14f); return "a " + F0(h) + " m drop onto a slope: " + F0(LevelMetricsMath.FallSpeed(v, h)) + " m/s of fall becomes speed"; }
    public override float Build(Frame f, int d)
    {
        float h = Pick(d, 4f, 8f, 14f);
        const float deg = 28f, slopeLen = 14f;
        f.Deck(0f, 8f, 0f, HalfW, "Edge"); f.Line(0f, 0.5f, 7.5f, 0f);
        float carry = BeatMath.FallCarry(f.v, f.v.baseRunSpeed, h);
        float z0 = 8f + Mathf.Max(1f, carry - 4f);   // the slope starts just before a walk-off lands
        float dz = slopeLen * BeatMath.Cos(deg), drop = slopeLen * BeatMath.Sin(deg);
        f.Slope(z0, -h, z0 + dz, -h - drop, HalfW + 1f, "LandingSlope");
        f.Deck(z0 + dz, z0 + dz + 8f, -h - drop, HalfW, "RunOut");
        f.Advance(z0 + dz + 8f, -h - drop);
        return Mathf.Max(f.v.baseRunSpeed, LevelMetricsMath.FallSpeed(f.v, h) * BeatMath.Sin(deg) + f.v.baseRunSpeed * BeatMath.Cos(deg));
    }
}

/// BREATHER: a straight, wide floor with nothing in it, 16 / 24 / 32 m: where a run is kept or hopped back up
public class Breather : Beat
{
    public override string Name => "BREATHER";
    public override float EntrySpeed(LevelMetricsMath.Values v, int d) => v.baseRunSpeed;
    public override string Note(LevelMetricsMath.Values v, int d) => F0(Pick(d, 16f, 24f, 32f)) + " m of nothing";
    public override float Build(Frame f, int d)
    {
        float len = Pick(d, 16f, 24f, 32f);
        f.Deck(0f, len, 0f, HalfW + 2f, "Breather"); f.Line(0f, 0.5f, len - 0.5f, 0f);
        f.Advance(len, 0f);
        return f.v.baseRunSpeed;
    }
}

/// FORK: the line splits round a divider — the SAFE side a whole floor that ends in a slope down, the RISKY side
/// one to three gaps at 80% of the reach at 12 m/s, each landing a metre lower — and they meet again on one floor
public class Fork : Beat
{
    public override string Name => "FORK";
    public override float EntrySpeed(LevelMetricsMath.Values v, int d) => 12f;
    public override string Note(LevelMetricsMath.Values v, int d) => "left: safe and slow · right: " + (int)Pick(d, 1, 2, 3) + " gap(s) of " + F1(BeatMath.Gap(v, 12f, -1f, 0.8f)) + " m";
    public override float Build(Frame f, int d)
    {
        int n = (int)Pick(d, 1, 2, 3);
        float gap = BeatMath.Gap(f.v, 12f, -1f, 0.8f);
        const float seg = 6f, lane = 2.75f, off = 4.25f;
        float len = seg + n * (gap + seg);
        f.Deck(0f, 4f, 0f, HalfW, "ForkMouth");
        f.Mass(-1.5f, 1.5f, -1f, 4f, 4f, 4f + len, "Divider");
        // the risky right side: floors and gaps, each landing a metre lower
        float z = 4f, y = 0f;
        f.Deck(z, z + seg, y, lane, "RiskyFloor", off);
        z += seg;
        for (int i = 0; i < n; i++)
        {
            z += gap; y -= 1f;
            f.Deck(z, z + seg, y, lane, "RiskyFloor", off);
            z += seg;
        }
        // the safe left side: whole, flat, then down a slope to the same height
        float slopeLen = 10f;
        f.Deck(4f, 4f + len - slopeLen, 0f, lane, "SafeFloor", -off);
        f.Slope(4f + len - slopeLen, 0f, 4f + len, y, lane, "SafeSlope", -off);
        f.Deck(4f + len, 4f + len + 10f, y, HalfW, "Rejoin"); f.Line(0f, 4.5f + len, 13.5f + len, y);
        f.Advance(4f + len + 10f, y);
        return 12f;
    }
}

/// SKIP: a gap straight ahead at 90% of the reach at 14 / 16 / 18 m/s that cuts out the long way round it
public class Skip : Beat
{
    public override string Name => "SKIP";
    public override float EntrySpeed(LevelMetricsMath.Values v, int d) => Pick(d, 14f, 16f, 18f);
    public override string Note(LevelMetricsMath.Values v, int d) { float s = EntrySpeed(v, d); return "straight: " + F1(BeatMath.Gap(v, s, -1f, 0.9f)) + " m at " + F0(s) + " m/s · or the long way right"; }
    public override float Build(Frame f, int d)
    {
        float s = EntrySpeed(f.v, d), gap = BeatMath.Gap(f.v, s, -1f, 0.9f);
        const float laneW = 2f, outX = 11f;
        f.Deck(0f, 8f, 0f, HalfW, "Launch"); f.Line(0f, 0.5f, 7.5f, 0f);
        f.Deck(8f + gap, 18f + gap, -1f, HalfW, "Landing"); f.Line(0f, 8.5f + gap, 17.5f + gap, -1f);
        // the long way: a floor out to the right from the launch, a slope forward and down beside the gap, a floor back in to the landing
        f.Mass(HalfW - 0.2f, outX + laneW, -1f, 0f, 3f, 8f, "WayOut", LevelKit.Floor);
        f.Slope(8f, 0f, 12f + gap, -1f, laneW, "WayAlong", outX);
        f.Mass(HalfW - 0.2f, outX + laneW, -2f, -1f, 12f + gap, 17f + gap, "WayBack", LevelKit.Floor);
        f.Advance(18f + gap, -1f);
        return s;
    }
}

/// TECH CHECK: I a gap only a wall run crosses · II a slot only a slide passes, then a slide jump · III a gap only a double jump makes
public class TechCheck : Beat
{
    public override string Name => "TECH CHECK";
    public override float EntrySpeed(LevelMetricsMath.Values v, int d) => Pick(d, v.baseRunSpeed, 10f, 12f);
    public override string Note(LevelMetricsMath.Values v, int d) => d <= 1 ? "WALL RUN the gap" : d == 2 ? "SLIDE under the bar, SLIDE-JUMP the gap" : "DOUBLE JUMP the gap (a pip)";
    public override float Build(Frame f, int d)
    {
        float s = EntrySpeed(f.v, d);
        if (d <= 1)
        {
            const float lane = 2.6f;
            float gap = 11f, dy = -3f;
            f.Deck(0f, 8f, 0f, lane, "Launch"); f.Line(0f, 0.5f, 7.5f, 0f);
            f.Wall(lane + 0.6f, 6f, 18f, 0.3f, 4f);
            f.Deck(8f + gap, 20f + gap, dy, HalfW, "Landing"); f.Line(0f, 8.5f + gap, 19.5f + gap, dy);
            f.Advance(20f + gap, dy);
            return s;
        }
        if (d == 2)
        {
            float gap = BeatMath.SlideJumpReach(f.v, s, -0.5f) * 0.8f;
            f.Deck(0f, 8f, 0f, HalfW, "Launch"); f.Line(0f, 0.5f, 7.5f, 0f);
            f.Mass(-HalfW - 1f, HalfW + 1f, 1.25f, 2.25f, 3f, 6.5f, "Bar");
            f.Deck(8f + gap, 18f + gap, -0.5f, HalfW, "Landing"); f.Line(0f, 8.5f + gap, 17.5f + gap, -0.5f);
            f.Advance(18f + gap, -0.5f);
            return s;
        }
        float dj = BeatMath.DoubleReach(f.v, s, -1f) * 0.85f;
        f.Deck(0f, 8f, 0f, HalfW, "Launch"); f.Line(0f, 0.5f, 7.5f, 0f);
        f.Deck(8f + dj, 18f + dj, -1f, HalfW, "Landing"); f.Line(0f, 8.5f + dj, 17.5f + dj, -1f);
        f.Advance(18f + dj, -1f);
        return s;
    }
}

/// HIDDEN ROUTE (rebuilt 2026-10-28; the user: "harder alternative routes that are faster if executed correctly").
/// The lit line goes DOWN a slope and along the low floor. A HIGH LINE hangs on the left over that floor, with no lit
/// line, only a lamp: off the approach's left edge a WALL RUN carries to a ledge, and the ledge runs on as a chain:
///   I   wall run · a jump · a drop-slope                                                     (10 m/s)
///   II  wall run · a slide under a bar · a slide jump · three hop stones (a tight bhop line) · a drop-slope (12 m/s)
///   III wall run · a second wall run · the bar and the slide jump · a narrow chute (a precise drop)  (14 m/s)
/// The drop at the end turns the high line's height into speed at the rejoin, and a long run-out is where that speed
/// pays. Every piece of it stands over the main floor, so a fall is a landing on the lit line a little behind, never a
/// reset. The lit line goes down by STEPS, not a slope (a slope would be a free slide and the faster line for anyone
/// who keeps speed): as long as the high line, lower, and slower for everyone. The validator times both lines from
/// reset pad to reset pad and reports the saving (LevelValidator: HIDDEN ROUTE). Names matter to it: Main… is the lit
/// line's own, Hidden… the high line's.
public class HiddenRoute : Beat
{
    public override string Name => "HIDDEN ROUTE";
    public override float EntrySpeed(LevelMetricsMath.Values v, int d) => Pick(d, 10f, 12f, 14f);
    public override string Note(LevelMetricsMath.Values v, int d) => "the lit line goes down · a HIGH LINE on the left: " + (d <= 1 ? "wall run, jump, drop" : d == 2 ? "wall run, SLIDE the bar, slide-jump, hop the stones, drop" : "wall run, wall run, slide the bar, slide-jump, the chute");
    public override float Build(Frame f, int d)
    {
        float s = EntrySpeed(f.v, d);
        const float wide = 6.5f, laneX = -5.1f, laneHalf = 1.3f, wallX = -7.1f, dropDeg = 28f, wallH = 4.5f;
        float h = Pick(d, 4f, 6f, 8f);                                   // the main line's descent, and the high line's drop at its end
        float carry = BeatMath.WallRunCarry(f.v, s, 0f) * 0.85f;        // the wall run and the jump off it, to a ledge at the same height
        float dropDz = h / Mathf.Tan(dropDeg * Mathf.Deg2Rad);

        // the lit line: the approach, three steps down (the low floor runs on to the rejoin, laid after the high line's length is known)
        f.Deck(0f, 6f, 0f, wide, "Approach"); f.Line(0f, 0.5f, 5.5f, 0f);
        f.Deck(6f, 10f, -h / 3f, wide, "MainStep"); f.Line(0f, 6.5f, 9.5f, -h / 3f);
        f.Deck(10f, 14f, -2f * h / 3f, wide, "MainStep"); f.Line(0f, 10.5f, 13.5f, -2f * h / 3f);

        // the high line: the wall off the approach's left edge, the first ledge where the run's jump lands
        f.Wall(wallX, 4f, 6f + carry - 0.5f, -1f, wallH);
        float z = 6f + carry;
        f.Lamp(laneX, 4f, z + 1f, 18f, 2.4f);
        if (d <= 1)
        {
            f.Deck(z, z + 8f, 0f, laneHalf, "HiddenLedge", laneX); z += 8f;
            z += BeatMath.Gap(f.v, s, 0f, 0.8f);
            f.Deck(z, z + 6f, 0f, laneHalf, "HiddenLedge", laneX); z += 6f;
        }
        else if (d == 2)
        {
            f.Deck(z, z + 10f, 0f, laneHalf, "HiddenLedge", laneX);
            f.Mass(laneX - 1.6f, laneX + 1.6f, 1.25f, 2.25f, z + 6.5f, z + 10f, "HiddenBar");   // a slide, or a stop
            z += 10f;
            z += BeatMath.SlideJumpReach(f.v, s, 0f) * 0.8f;
            float period = BeatMath.Reach(f.v, s, 0f), stone = 3.5f;   // a stone every clean arc at this speed: land, hop inside the grace, land
            for (int i = 0; i < 3; i++) { f.Deck(z, z + stone, 0f, laneHalf, "HiddenHop", laneX); z += period; }
            f.Deck(z, z + 6f, 0f, laneHalf, "HiddenLedge", laneX); z += 6f;
        }
        else
        {
            f.Deck(z, z + 8f, 0f, laneHalf, "HiddenLedge", laneX); z += 8f;
            f.Wall(wallX, z - 1.5f, z - 1.5f + carry - 1f, -1f, wallH);   // the second wall begins before the ledge ends: run straight off onto it
            z += carry;
            f.Deck(z, z + 10f, 0f, laneHalf, "HiddenLedge", laneX);
            f.Mass(laneX - 1.6f, laneX + 1.6f, 1.25f, 2.25f, z + 6.5f, z + 10f, "HiddenBar");
            z += 10f;
            z += BeatMath.SlideJumpReach(f.v, s, 0f) * 0.8f;
        }
        // the drop at the end: a slope the lane's width (a chute 1.8 m wide on III) down to the rejoin
        f.Slope(z, 0f, z + dropDz, -h, d >= 3 ? 0.9f : laneHalf, d >= 3 ? "HiddenChute" : "HiddenDrop", laneX);
        f.Lamp(laneX, 3f, z + 2f, 18f, 2.4f);
        float zr = z + dropDz;

        // the low floor under all of it, the rejoin where the drop lands
        f.Deck(14f, zr, -h, wide, "MainFloor"); f.Line(0f, 14.5f, zr - 0.5f, -h);
        f.Deck(zr, zr + 16f, -h, wide, "Rejoin"); f.Line(0f, zr + 0.5f, zr + 15.5f, -h);   // the run-out: sixteen metres where the high line's speed shows
        f.Advance(zr + 16f, -h);
        return f.v.baseRunSpeed;
    }
}

/// TURN: ninety degrees right that keeps the speed — I a banked curve that drops as it bends, a wall on its outside;
/// II a wall run along the outside of the corner and a jump off it onto the new leg; III a drop-through: the floor
/// ends and a slope below runs the new way
public class Turn : Beat
{
    public override string Name => "TURN";
    public override float EntrySpeed(LevelMetricsMath.Values v, int d) => Pick(d, 10f, 10f, v.baseRunSpeed);
    public override string Note(LevelMetricsMath.Values v, int d) => d <= 1 ? "a banked curve right, downhill" : d == 2 ? "wall run the outside, jump off to the right" : "drop through: the slope below runs right";
    public override float Build(Frame f, int d)
    {
        if (d <= 1)
        {
            const int segs = 6; const float each = 15f, segLen = 6f, pitch = 5f;
            float over = HalfW * Mathf.Tan(each * 0.5f * Mathf.Deg2Rad) * 0.5f + 0.2f;   // the joints overlap a little so the outer edge has no hole
            float lift = over * Mathf.Tan(pitch * Mathf.Deg2Rad);   // the overlap keeps the pitch: the top at z = 0 is exactly the origin's height
            for (int i = 0; i < segs; i++)
            {
                float segDrop = segLen * BeatMath.Sin(pitch), segDz = segLen * BeatMath.Cos(pitch);
                f.Slope(-over, lift, segDz + over, -segDrop - lift, HalfW, "CurveDeck");
                f.Wall(-(HalfW + 0.6f), -over, segDz + over, 0.3f, 4f);   // the outside of a right turn is the left
                f.Advance(segDz, -segDrop);
                f.Turn(each);
            }
            float exitLen = 10f, exitDrop = exitLen * Mathf.Tan(pitch * Mathf.Deg2Rad);
            f.Slope(-over, lift, exitLen, -exitDrop, HalfW, "Exit");   // one more pitch of the same slope, straight: the joint stays flush
            f.Advance(exitLen, -exitDrop);
            return 10f;
        }
        if (d == 2)
        {
            f.Deck(0f, 10f, 0f, HalfW, "Leg"); f.Line(0f, 0.5f, 9.5f, 0f);
            f.Wall(-(HalfW + 0.6f), 4f, 15f, 0.3f, 4f);
            f.Mass(2f, 16f, -2.5f, -1.5f, 7f, 17f, "NewLeg", LevelKit.Floor);   // the exit floor, a metre and a half down, out to the right
            var g = f.Sub(16f, -1.5f, 12f, 90f);
            f.origin = g.origin; f.Turn(90f);
            return 10f;
        }
        f.Deck(0f, 10f, 0f, HalfW, "Leg"); f.Line(0f, 0.5f, 9.5f, 0f);
        const float h = 6f, deg = 28f, slopeLen = 12f;
        var s = f.Sub(0f, -h, 14f, 90f);
        float dz = slopeLen * BeatMath.Cos(deg), drop = slopeLen * BeatMath.Sin(deg);
        s.Slope(-6f, 0f, dz, -drop * (dz + 6f) / dz, 6f, "DropSlope");   // from under the edge, down the new way
        s.Deck(dz, dz + 8f, -drop * (dz + 6f) / dz, HalfW, "RunOut");
        s.Advance(dz + 8f, -drop * (dz + 6f) / dz);
        f.origin = s.origin; f.Turn(90f);
        return LevelMetricsMath.FallSpeed(f.v, h) * BeatMath.Sin(deg) + f.v.baseRunSpeed;
    }
}

public static class BeatLibrary
{
    /// The speed a beat expects at difficulty `d`, from a checkpoint named for it ("HIDDEN ROUTE II"): the validator times
    /// a hidden line at the speed its beat asks for; run speed for any other name
    public static float ExpectedSpeed(string checkpointName, LevelMetricsMath.Values v)
    {
        if (string.IsNullOrEmpty(checkpointName)) return v.baseRunSpeed;
        foreach (var b in All)
        {
            if (!checkpointName.StartsWith(b.Name)) continue;
            string rest = checkpointName.Substring(b.Name.Length).Trim();
            int d = rest == "III" ? 3 : rest == "II" ? 2 : 1;
            return b.EntrySpeed(v, d);
        }
        return v.baseRunSpeed;
    }

    /// Every beat, in the order the gym lays them out
    public static readonly List<Beat> All = new List<Beat>
    {
        new GapJump(), new WallrunChain(), new SlideTunnel(), new RampLaunch(), new Drop(), new Breather(),
        new Fork(), new Skip(), new TechCheck(), new HiddenRoute(), new Turn(),
    };
}
