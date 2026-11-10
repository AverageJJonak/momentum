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
///
/// THE CONTROL PASS (2026-10-29; the user: "the gym is too easy because I can spam bhops and wall runs to snowball
/// speed, then fly through everything. Every beat only checks if I'm fast enough, never if I can control my speed").
/// The motor cannot be made to shed speed (a head hit under a roof only zeroes the rise; the hop chain goes on), so
/// control is geometry an overshoot hits or flies off: every gap now lands on a SHORT LANDING with a PORTAL at its end
/// (the arc that is still in the air at the lintel stops there; the run passes under), hop stones are spaced for one
/// speed and no other, the lit line under the hidden route and the safe side of the fork are COLONNADES (a lintel every
/// eight metres caps a hop chain near 11 m/s), the launch floors are four metres (one hop at most before a gap), the
/// BREATHER is gone ("no long flat straights to build speed for free"), and CONTROL is a beat of its own. The validator's
/// snowball rider (LevelValidator: THE SNOWBALL) tries every gap at the speed a hop chain builds and reports how much
/// more than its sized speed each one takes before its portal or its void says no.
/// </summary>
public abstract class Beat
{
    public abstract string Name { get; }
    /// The speed the beat is sized for at difficulty `d` (1–3)
    public abstract float EntrySpeed(LevelMetricsMath.Values v, int d);
    /// The speed the beat's gap is sized for: the entry speed, unless the beat makes its own on the way (the ramp's lip)
    public virtual float SizedSpeed(LevelMetricsMath.Values v, int d) => EntrySpeed(v, d);
    /// What the sign over the reset pad says about this one
    public abstract string Note(LevelMetricsMath.Values v, int d);
    /// Builds the beat from the frame's origin; moves the frame to the exit; returns the speed it hands on
    public abstract float Build(Frame f, int d);
    protected const float HalfW = 5f;
    protected static float Pick(int d, float a, float b, float c) => d <= 1 ? a : d == 2 ? b : c;
    protected static string F1(float x) => x.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
    protected static string F0(float x) => x.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
}

/// GAP JUMP: a four-metre launch, a gap at 70 / 80 / 90% of the reach at the expected speed, and a PITCHED LANDING: a roof
/// eight metres long falling ten degrees, then four metres of flat. Jump, land on the pitch, SLIDE it (2026-11-10: the
/// slide slot that stood at the landing's end went with the other landing gates: the user, having played them, "they
/// kill the flow". The slide is still the beat's second move, now as a roof that pays for it and never stops anyone)
public class GapJump : Beat
{
    public override string Name => "GAP JUMP";
    public override float EntrySpeed(LevelMetricsMath.Values v, int d) => Pick(d, v.baseRunSpeed, 12f, 16f);
    public override string Note(LevelMetricsMath.Values v, int d) { float s = EntrySpeed(v, d), dy = Pick(d, 0f, -1f, -2f); return "a gap of " + F1(BeatMath.Gap(v, s, dy, Pick(d, 0.7f, 0.8f, 0.9f))) + " m at " + F0(s) + " m/s · land on the pitched roof and SLIDE it"; }
    public override float Build(Frame f, int d)
    {
        float s = EntrySpeed(f.v, d), dy = Pick(d, 0f, -1f, -2f), gap = BeatMath.Gap(f.v, s, dy, Pick(d, 0.7f, 0.8f, 0.9f));
        const float deg = 10f, roof = 8f;
        float dz = roof * BeatMath.Cos(deg), fall = roof * BeatMath.Sin(deg);
        f.Deck(0f, 4f, 0f, HalfW, "Launch"); f.Line(0f, 0.5f, 3.5f, 0f);
        f.Slope(4f + gap, dy, 4f + gap + dz, dy - fall, HalfW, "LandingSlope"); f.LineRun(0f, 4f + gap, dy, 4f + gap + dz, dy - fall);
        f.Deck(4f + gap + dz, 4f + gap + dz + 4f, dy - fall, HalfW, "Landing"); f.Line(0f, 4.5f + gap + dz, 3.5f + gap + dz + 4f, dy - fall);
        f.Advance(4f + gap + dz + 4f, dy - fall);
        return BeatMath.SlideGain(f.v, s, deg, roof * 0.6f);
    }
}

/// WALLRUN CHAIN (rebuilt 2026-11-10; the user, having played it: "the wall jump goes wrong", and the recording showed
/// why: at 26 m/s the jump off one wall carried the body into the blunt leading end of the next, head on). Two to four
/// facades alternating left and right over a void, and now they INTERLOCK: every wall begins beside the one before it,
/// seven or six metres before that one ends, so the next wall is already alongside when you leave and there is no end
/// to fly into; whatever the speed, the jump across meets a face. Each wall then runs on alone for 4 / 6 / 8 m (the
/// stretch you hold) before the next begins. Run one, jump across when the other is beside you, run that, again; the
/// landing is fourteen metres of floor a little lower, with no gate on it
public class WallrunChain : Beat
{
    public override string Name => "WALLRUN CHAIN";
    public override float EntrySpeed(LevelMetricsMath.Values v, int d) => Pick(d, v.baseRunSpeed, 10f, 12f);
    static float Sole(int d) => Pick(d, 4f, 6f, 8f);
    static float Overlap(int d) => Pick(d, 7f, 7f, 6f);
    public override string Note(LevelMetricsMath.Values v, int d) => (int)Pick(d, 2, 3, 4) + " facades that overlap · run one, JUMP ACROSS when the next is beside you (it always is before this one ends)";
    public override float Build(Frame f, int d)
    {
        float s = EntrySpeed(f.v, d), sole = Sole(d), overlap = Overlap(d), step = sole + overlap;
        int n = (int)Pick(d, 2, 3, 4);
        const float lane = 2.6f, land = 14f;
        f.Deck(0f, 6f, 0f, lane, "Launch"); f.Line(0f, 0.5f, 5.5f, 0f);
        float zEnd = 0f;
        for (int i = 0; i < n; i++)
        {
            // the first wall stands beside the launch and runs `sole` alone; every later one begins `overlap` before the one before it ends
            float side = i % 2 == 0 ? 1f : -1f;
            float a = i == 0 ? 4f : 4f + sole + (i - 1) * step;
            zEnd = a + (i == 0 ? sole + overlap : sole + 2f * overlap);
            f.Wall(side * (lane + 0.6f), a, zEnd, 0.3f - i * 0.4f, 4f);
        }
        float dy = -1f - 0.4f * n, zLand = zEnd - 2f;
        f.Deck(zLand, zLand + land, dy, HalfW, "Landing"); f.Line(0f, zLand + 0.5f, zLand + land - 0.5f, dy);
        f.Advance(zLand + land, dy);
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
        if (f.Structural)   // the building the tunnel runs through: a pitched mass over the roof, as tall as a facade, its underside a hair into the roof (a pitched box's vertical depth is its thickness over the cosine)
        {
            float vert = f.facadeHeight / BeatMath.Cos(deg) - 0.01f;
            f.Slope(2f, 0.18f + clear + 1f + vert, 3f + dz, -drop + 0.18f + clear + 1f + vert, HalfW + 1.5f, "Facade", 0f, f.facadeHeight, LevelKit.Dark);
        }
        f.Line(0f, 3.5f, 3f + dz - 0.5f, -drop * 0.5f);
        f.Advance(3f + dz, -drop);
        return BeatMath.SlideGain(f.v, s, deg, len);
    }
}

/// RAMP LAUNCH: a slope you slide to gather speed, its edge the lip of a gap sized for that speed, a slope to land on,
/// with a portal eight metres down the landing slope: an arc that overshoots the slope's upper half meets the lintel
public class RampLaunch : Beat
{
    public override string Name => "RAMP LAUNCH";
    public override float EntrySpeed(LevelMetricsMath.Values v, int d) => v.baseRunSpeed;
    public override float SizedSpeed(LevelMetricsMath.Values v, int d) => BeatMath.SlideGain(v, v.baseRunSpeed, Deg, Pick(d, 8f, 12f, 16f));   // the lip
    const float Deg = 14f;
    public override string Note(LevelMetricsMath.Values v, int d) { float len = Pick(d, 8f, 12f, 16f), lip = BeatMath.SlideGain(v, v.baseRunSpeed, Deg, len); return "SLIDE the " + F0(len) + " m ramp to " + F0(lip) + " m/s, jump the " + F1(BeatMath.Gap(v, lip, -2f, Pick(d, 0.7f, 0.8f, 0.9f))) + " m gap · land on the slope"; }
    public override float Build(Frame f, int d)
    {
        float len = Pick(d, 8f, 12f, 16f), lip = BeatMath.SlideGain(f.v, f.v.baseRunSpeed, Deg, len);
        float dz = len * BeatMath.Cos(Deg), drop = len * BeatMath.Sin(Deg);
        float gap = BeatMath.Gap(f.v, lip, -2f, Pick(d, 0.7f, 0.8f, 0.9f));
        f.Slope(0f, 0f, dz, -drop, HalfW, "LaunchRamp");
        float yLand = -drop - 2f, landLen = 10f, landDrop = landLen * BeatMath.Sin(10f), landDz = landLen * BeatMath.Cos(10f);
        f.Slope(dz + gap, yLand, dz + gap + landDz, yLand - landDrop, HalfW, "LandingSlope");
        f.Deck(dz + gap + landDz, dz + gap + landDz + 4f, yLand - landDrop, HalfW, "Landing");
        f.Advance(dz + gap + landDz + 4f, yLand - landDrop);
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
        f.Deck(0f, 4f, 0f, HalfW, "Edge"); f.Line(0f, 0.5f, 3.5f, 0f);
        float carry = BeatMath.FallCarry(f.v, f.v.baseRunSpeed, h);
        float z0 = 4f + Mathf.Max(1f, carry - 4f);   // the slope starts just before a walk-off lands
        float dz = slopeLen * BeatMath.Cos(deg), drop = slopeLen * BeatMath.Sin(deg);
        f.Slope(z0, -h, z0 + dz, -h - drop, HalfW + 1f, "LandingSlope");
        f.Deck(z0 + dz, z0 + dz + 8f, -h - drop, HalfW, "RunOut");
        f.Advance(z0 + dz + 8f, -h - drop);
        return Mathf.Max(f.v.baseRunSpeed, LevelMetricsMath.FallSpeed(f.v, h) * BeatMath.Sin(deg) + f.v.baseRunSpeed * BeatMath.Cos(deg));
    }
}

/// FORK: the line splits round a divider — the SAFE side a colonnade (a whole floor under a lintel every eight metres:
/// slow for a hopper too) that ends in a slope down, the RISKY side a BAR to slide under and a SLIDE JUMP (2026-11-04;
/// the user: "every jump sequence mixes in at least one other move") to the first of one to three hop stones, each a
/// metre lower, three metres deep, spaced for the speed the slide jump leaves: that speed and not much more — and they
/// meet again under a portal
public class Fork : Beat
{
    public override string Name => "FORK";
    public override float EntrySpeed(LevelMetricsMath.Values v, int d) => 12f;
    public override string Note(LevelMetricsMath.Values v, int d) => "left: safe and slow, a colonnade · right: SLIDE the bar, slide-jump " + F1(BeatMath.SlideJumpReach(v, 12f, -1f) * 0.8f) + " m to the first stone, hop " + (int)Pick(d, 0, 1, 2) + " more";
    public override float Build(Frame f, int d)
    {
        int n = (int)Pick(d, 1, 2, 3);
        const float first = 4f, stone = 3f, lane = 2.75f, off = 4.25f, s = 12f;
        // the first gap is 80% of the slide jump's reach at 12 m/s; the hops after it are sized for the speed between the lazy
        // and the perfect slide jump's, so the lazy one lands a little short of each stone's middle and the perfect one a little past it
        float gap1 = BeatMath.SlideJumpReach(f.v, s, -1f) * 0.8f;
        float hop = s * 0.5f * (LevelMetricsMath.SlideJumpMultiplier(f.v, s, false) + LevelMetricsMath.SlideJumpMultiplier(f.v, s, true));
        float gap = BeatMath.Gap(f.v, hop, -1f, 0.8f);
        float len = first + gap1 + n * (stone + gap);   // the last gap lands on the rejoin
        f.Deck(0f, 4f, 0f, HalfW, "ForkMouth");
        f.Mass(-1.5f, 1.5f, f.Structural ? f.groundY : -1f, f.Structural ? f.facadeHeight : 4f, 4f, 4f + len, "Divider");   // a building between the two ways, too tall for a run to top
        // the risky right side: a floor, then stones, each a metre lower, the rejoin a metre lower still
        float z = 4f, y = 0f;
        f.Deck(z, z + first, y, lane, "RiskyFloor", off);
        f.Bar(z + first - 1.75f, y, lane - 0.2f, 1.25f, false, true, off);   // the bar off the divider: a slide passes under, a run or a hop into it stops (the slide is this side's move)
        z += first;
        for (int i = 0; i < n; i++)
        {
            z += i == 0 ? gap1 : gap; y -= 1f;
            f.Deck(z, z + stone, y, lane, "RiskyStone", off);
            z += stone;
        }
        y -= 1f;
        // the safe left side: whole, flat, under lintels, then down a slope to the same height
        float slopeLen = 10f;
        f.Deck(4f, 4f + len - slopeLen, 0f, lane, "SafeFloor", -off);
        for (float pz = 8f; pz < 4f + len - slopeLen - 2f; pz += 8f) f.Portal(pz, 0f, lane - 0.2f, 2.2f, 5.5f, true, false, -off);
        f.Slope(4f + len - slopeLen, 0f, 4f + len, y, lane, "SafeSlope", -off);
        f.Deck(4f + len, 4f + len + 6f, y, HalfW, "Rejoin"); f.Line(0f, 4.5f + len, 9.5f + len, y);
        f.Advance(4f + len + 6f, y);
        return 12f;
    }
}

/// SKIP: a gap straight ahead at 14 / 16 / 18 m/s that cuts out the long way round it, crossed on a WALL RUN along the
/// facade on its left (2026-11-04; the user: "every jump sequence mixes in at least one other move"): the gap is more
/// than a plain jump at that speed makes (1.4 times the clean reach) and three quarters of what the run and the jump
/// off it carry, the wall ending where that jump lands two metres onto the landing; the landing is five metres under a
/// portal (the long way comes in under the lintel's end)
public class Skip : Beat
{
    public override string Name => "SKIP";
    public override float EntrySpeed(LevelMetricsMath.Values v, int d) => Pick(d, 14f, 16f, 18f);
    public override string Note(LevelMetricsMath.Values v, int d) { float s = EntrySpeed(v, d); return "straight: WALL RUN the left facade across " + F1(SkipGap(v, s)) + " m at " + F0(s) + " m/s · or the long way right"; }
    /// More than any hop at this speed, three quarters of the wall run's carry
    static float SkipGap(LevelMetricsMath.Values v, float s) => Mathf.Max(1.4f * BeatMath.Reach(v, s, -1f) + 0.5f, 0.75f * BeatMath.WallRunCarry(v, s, -1f));
    public override float Build(Frame f, int d)
    {
        float s = EntrySpeed(f.v, d), gap = SkipGap(f.v, s);
        const float laneW = 2f, outX = 11f, land = 5f;
        var offWall = LevelMetricsMath.WallJump(f.v, s, 0f);
        float wallEnd = 4f + gap + 2f - LevelMetricsMath.Jump(f.v, offWall.h, offWall.vy, -1f, false).distance;   // the jump off the wall's end lands two metres onto the landing
        f.Deck(0f, 4f, 0f, HalfW, "Launch"); f.Line(0f, 0.5f, 3.5f, 0f);
        f.Wall(-(HalfW + 0.6f), 2f, wallEnd, 0.3f, 4f);   // the facade on the left: the way across
        f.Deck(4f + gap, 4f + gap + land, -1f, HalfW, "Landing"); f.Line(0f, 4.5f + gap, 3.5f + gap + land, -1f);
        // the long way: a floor out to the right from the launch, a slope forward and down beside the gap, a floor back in to the landing
        f.Mass(HalfW - 0.2f, outX + laneW, -1f, 0f, 1f, 4f, "WayOut", LevelKit.Floor); f.Body(HalfW - 0.2f, outX + laneW, -1.05f, 1f, 4f, "WayOutBody");
        f.Slope(4f, 0f, 6f + gap, -1f, laneW, "WayAlong", outX);
        for (float pz = 12f; pz < 6f + gap - 3f; pz += 8f) f.Portal(pz, -(pz - 4f) / (2f + gap), laneW - 0.2f, 2.2f, 5.5f, false, true, outX);   // the long way is a COLONNADE (2026-11-04): a lintel every eight metres along it caps a hop chain near 11 m/s, as the fork's safe side does; the validator read the longer way as a straight that snowballs
        f.Mass(HalfW - 0.2f, outX + laneW, -2f, -1f, 6f + gap, 8.5f + gap, "WayBack", LevelKit.Floor); f.Body(HalfW - 0.2f, outX + laneW, -2.05f, 6f + gap, 8.5f + gap, "WayBackBody");   // ends half a metre short of the landing's end, so the long way comes back onto the landing and not past it
        f.Advance(4f + gap + land, -1f);
        return s;
    }
}

/// TECH CHECK: I a gap only a wall run crosses · II a slot only a slide passes, then a slide jump · III a gap only a
/// double jump makes; every landing short and under a portal
public class TechCheck : Beat
{
    public override string Name => "TECH CHECK";
    public override float EntrySpeed(LevelMetricsMath.Values v, int d) => Pick(d, v.baseRunSpeed, 10f, 12f);
    public override string Note(LevelMetricsMath.Values v, int d) => (d <= 1 ? "WALL RUN the gap" : d == 2 ? "SLIDE under the bar, SLIDE-JUMP the gap" : "DOUBLE JUMP the gap (a pip)");
    public override float Build(Frame f, int d)
    {
        float s = EntrySpeed(f.v, d);
        if (d <= 1)
        {
            const float lane = 2.6f, wallEnd = 14f;
            float gap = 11f, dy = -3f;
            var off = LevelMetricsMath.WallJump(f.v, s, 0f);
            float land = wallEnd + LevelMetricsMath.Jump(f.v, off.h, off.vy, dy, false).distance - (4f + gap) + 1f;   // to where the jump off the wall's end lands, and a metre
            f.Deck(0f, 4f, 0f, lane, "Launch"); f.Line(0f, 0.5f, 3.5f, 0f);
            f.Wall(lane + 0.6f, 2f, wallEnd, 0.3f, 4f);
            f.Deck(4f + gap, 4f + gap + land, dy, HalfW, "Landing"); f.Line(0f, 4.5f + gap, 3.5f + gap + land, dy);
            f.Advance(4f + gap + land, dy);
            return s;
        }
        if (d == 2)
        {
            float gap = BeatMath.SlideJumpReach(f.v, s, -0.5f) * 0.8f, land = 5f;
            f.Deck(0f, 4f, 0f, HalfW, "Launch"); f.Line(0f, 0.5f, 3.5f, 0f);
            f.Bar(1.75f, 0f, HalfW);   // the bar: the slide is this check's move
            f.Deck(4f + gap, 4f + gap + land, -0.5f, HalfW, "Landing"); f.Line(0f, 4.5f + gap, 3.5f + gap + land, -0.5f);
            f.Advance(4f + gap + land, -0.5f);
            return s;
        }
        float dj = BeatMath.DoubleReach(f.v, s, -1f) * 0.85f, land3 = 7f;   // the double jump's arc is long: seven metres take it to about 1.25× the sized speed
        f.Deck(0f, 4f, 0f, HalfW, "Launch"); f.Line(0f, 0.5f, 3.5f, 0f);
        f.Deck(4f + dj, 4f + dj + land3, -1f, HalfW, "Landing"); f.Line(0f, 4.5f + dj, 3.5f + dj + land3, -1f);
        f.Advance(4f + dj + land3, -1f);
        return s;
    }
}

/// HIDDEN ROUTE (rebuilt 2026-10-28; the user: "harder alternative routes that are faster if executed correctly").
/// The lit line goes DOWN a slope and along the low floor. A HIGH LINE hangs on the left over that floor, with no lit
/// line, only a lamp: off the approach's left edge a WALL RUN carries to a ledge, and the ledge runs on as a chain:
///   I   wall run · a jump · a drop-slope                                                     (10 m/s)
///   II  wall run · a slide under a bar · a slide jump · three hop stones (a tight bhop line) · a drop-slope (12 m/s)
///   III wall run · a second wall run · the bar and the slide jump · a narrow chute (a precise drop)  (14 m/s)
/// The drop at the end turns the high line's height into speed at the rejoin, and a run-out is where that speed
/// pays. Every piece of it stands over the main floor, so a fall is a landing on the lit line a little behind, never a
/// reset. The lit line goes down by STEPS, not a slope (a slope would be a free slide and the faster line for anyone
/// who keeps speed): as long as the high line, lower, and slower for everyone; since 2026-10-29 its low floor is a
/// COLONNADE (a lintel every eight metres over the lit line, clear of the high line's lane), so a hop chain along it is
/// capped near 11 m/s and the high line's speed is the only way to arrive fast. The validator times both lines from
/// reset pad to reset pad and reports the saving (LevelValidator: HIDDEN ROUTE). Names matter to it: Main… is the lit
/// line's own, Hidden… the high line's.
public class HiddenRoute : Beat
{
    public override string Name => "HIDDEN ROUTE";
    const float LaneX = -5.1f, LaneHalf = 1.3f;
    /// The bar across the high line's lane, centred at `z`: a slide or a stop. With a ground it is a gate off the facade, nothing to stand on
    static void HiddenBar(Frame f, float z)
    {
        f.Bar(z, 0f, LaneHalf + 0.3f, 1.25f, false, true, LaneX);
    }
    public override float EntrySpeed(LevelMetricsMath.Values v, int d) => Pick(d, 10f, 12f, 14f);
    public override string Note(LevelMetricsMath.Values v, int d) => "the lit line goes down · a HIGH LINE on the left: " + (d <= 1 ? "wall run, jump, drop" : d == 2 ? "wall run, SLIDE the bar, slide-jump, hop the stones, drop" : "wall run, wall run, slide the bar, slide-jump, the chute");
    public override float Build(Frame f, int d)
    {
        float s = EntrySpeed(f.v, d);
        const float wide = 6.5f, laneX = -5.1f, laneHalf = 1.3f, wallX = -7.1f, dropDeg = 28f, wallH = 4.5f;
        float h = Pick(d, 4f, 6f, 8f);                                   // the main line's descent, and the high line's drop at its end
        float carry = BeatMath.WallRunCarry(f.v, s, 0f) * 0.85f;        // the wall run and the jump off it, to a ledge at the same height
        float dropDz = h / Mathf.Tan(dropDeg * Mathf.Deg2Rad);

        // the lit line: the approach, three steps down (the low floor runs on to the rejoin, laid after the high line's length is known).
        // The steps keep clear of the facade (the lane under the high line is void there), so the run up the facade can only be entered from the approach
        f.Deck(0f, 6f, 0f, wide, "Approach"); f.Line(0f, 0.5f, 5.5f, 0f);
        f.Deck(6f, 10f, -h / 3f, HalfW, "MainStep", 1.5f); f.Line(0f, 6.5f, 9.5f, -h / 3f);
        f.Deck(10f, 14f, -2f * h / 3f, HalfW, "MainStep", 1.5f); f.Line(0f, 10.5f, 13.5f, -2f * h / 3f);

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
            HiddenBar(f, z + 8.25f);   // a slide, or a stop
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
            HiddenBar(f, z + 8.25f);
            z += 10f;
            z += BeatMath.SlideJumpReach(f.v, s, 0f) * 0.8f;
        }
        // the drop at the end: a slope the lane's width (a chute 1.8 m wide on III) down to the rejoin
        f.Slope(z, 0f, z + dropDz, -h, d >= 3 ? 0.9f : laneHalf, d >= 3 ? "HiddenChute" : "HiddenDrop", laneX);
        f.Lamp(laneX, 3f, z + 2f, 18f, 2.4f);
        float zr = z + dropDz;

        // the low floor under all of it, a colonnade over the lit line's part of it (the lintels stop short of the high line's lane), the rejoin where the drop lands
        f.Deck(14f, zr, -h, wide, "MainFloor"); f.Line(0f, 14.5f, zr - 0.5f, -h);
        for (float pz = 18f; pz < zr - 3f; pz += 8f) f.Portal(pz, -h, 4.6f, 2.2f, 3.2f, true, true, 1.9f);   // the left post at x -3.5…-2.8, the lane's edge is -3.8
        f.Deck(zr, zr + 10f, -h, wide, "Rejoin"); f.Line(0f, zr + 0.5f, zr + 9.5f, -h);   // the run-out: ten metres where the high line's speed shows
        f.Advance(zr + 10f, -h);
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
            float exitLen = 4f, exitDrop = exitLen * Mathf.Tan(pitch * Mathf.Deg2Rad);   // short: with the floor after it, a longer exit is a straight a hop chain grows on
            f.Slope(-over, lift, exitLen, -exitDrop, HalfW, "Exit");   // one more pitch of the same slope, straight: the joint stays flush
            f.Advance(exitLen, -exitDrop);
            return 10f;
        }
        if (d == 2)
        {
            f.Deck(0f, 10f, 0f, HalfW, "Leg"); f.Line(0f, 0.5f, 9.5f, 0f);
            f.Wall(-(HalfW + 0.6f), 4f, 15f, 0.3f, 4f);
            f.Mass(2f, 16f, -2.5f, -1.5f, 7f, 17f, "NewLeg", LevelKit.Floor); f.Body(2f, 16f, -2.55f, 7f, 17f, "NewLegBody");   // the exit floor, a metre and a half down, out to the right
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

/// CONTROL (2026-10-29; the user: "a new beat type built around precision instead of speed … where going too fast makes
/// you fail"). THE BEAM WALK since 2026-11-10: the five hop stones and the wall run between them are gone (the user,
/// after playing: "the platforms are way too small and awkwardly spaced out, there's nothing unique or fun about it":
/// small pads spaced to the centimetre do not work in first person at speed; the TRICK DESCENT took their place as the
/// gym's platforming). What is left is VHOLUME's kind of control, a thin walkway taken at pace: I a straight 1.5 m beam
/// eight metres long; II a 1.2 m beam with a kink (a pad to turn on); III a 1 m beam with two. Each ends on a short
/// landing under a portal. The beams stand over nothing: too fast into a kink is a fall
public class Control : Beat
{
    public override string Name => "CONTROL";
    public override float EntrySpeed(LevelMetricsMath.Values v, int d) => Pick(d, v.baseRunSpeed, 10f, 12f);
    public override string Note(LevelMetricsMath.Values v, int d) => "KEEP " + F0(EntrySpeed(v, d)) + " m/s along the beam" + (d <= 1 ? ": 1.5 m wide and straight" : d == 2 ? ": 1.2 m wide with a kink" : ": 1 m wide with two kinks") + " · too fast is a fall";
    public override float Build(Frame f, int d)
    {
        float s = EntrySpeed(f.v, d);
        const float beamLen = 5f;
        f.Deck(0f, 4f, 0f, HalfW, "ControlLaunch"); f.Line(0f, 0.5f, 3.5f, 0f);
        float z = 4f;
        if (d <= 1)
        {
            // I: the walkway on its own, wide and straight: eight metres to learn that a beam is only a narrow floor
            f.Deck(z, z + 4f, 0f, 2f, "ControlLanding"); f.Line(0f, z + 0.5f, z + 4f, 0f); z += 4f;
            f.Beam(0f, z, 0f, z + 8f, 0f, 1.5f); f.Line(0f, z, z + 8f, 0f); z += 8f;
            f.Deck(z, z + 4f, 0f, 2f, "ControlLanding"); f.Line(0f, z + 0.5f, z + 3.5f, 0f);
            f.Advance(z + 4f, 0f);
            return s;
        }
        float beamW = d == 2 ? 1.2f : 1f, pad = d == 2 ? 2.4f : 2f;
        f.Deck(z, z + 4f, 0f, 2f, "ControlLanding"); f.Line(0f, z + 0.5f, z + 4f, 0f); z += 4f;
        // the first leg, straight on; the kink pad; the second leg to the right
        f.Beam(0f, z, 0f, z + beamLen, 0f, beamW); f.Line(0f, z, z + beamLen, 0f); z += beamLen;
        f.Deck(z, z + pad, 0f, pad * 0.5f, "ControlKink"); f.LineBetween(0f, z, 0f, z + pad * 0.5f, 0f); f.LineBetween(0f, z + pad * 0.5f, pad * 0.5f, z + pad * 0.5f, 0f);
        float zk = z + pad * 0.5f, xk = pad * 0.5f;
        f.Beam(xk, zk, xk + beamLen, zk, 0f, beamW); f.LineBetween(xk, zk, xk + beamLen, zk, 0f); xk += beamLen;
        if (d == 2)
        {
            // the landing and the portal lie along the second leg: the beat leaves turned right
            var g = f.Sub(xk, 0f, zk, 90f);
            g.Deck(0f, 4f, 0f, 2f, "ControlLanding"); g.Line(0f, 0.5f, 3.5f, 0f);
            g.Advance(4f, 0f);
            f.origin = g.origin; f.Turn(90f);
            return s;
        }
        // III: a second kink back to the line's heading, a third leg, the landing and the portal straight on
        f.Deck(zk - pad * 0.5f, zk + pad * 0.5f, 0f, pad * 0.5f, "ControlKink", xk + pad * 0.5f); f.LineBetween(xk, zk, xk + pad * 0.5f, zk, 0f); f.LineBetween(xk + pad * 0.5f, zk, xk + pad * 0.5f, zk + pad * 0.5f, 0f);
        xk += pad * 0.5f; zk += pad * 0.5f;
        f.Beam(xk, zk, xk, zk + beamLen, 0f, beamW); f.LineBetween(xk, zk, xk, zk + beamLen, 0f); zk += beamLen;
        f.Deck(zk, zk + 4f, 0f, 2f, "ControlLanding", xk); f.LineBetween(xk, zk + 0.5f, xk, zk + 3.5f, 0f);
        f.origin = f.P(xk, 0f, zk + 4f);
        return s;
    }
}

/// TRICK DESCENT (2026-11-10; the user threw out the hop stones and picked this in their place; played the same day:
/// "too easy", and asked for MOVES MIXED IN BETWEEN THE AIRS). Platforming as big airs onto big roofs, a tower's
/// setbacks taken as a cascade, and each roof asks for something different before the next jump:
///   the flat EDGE        a plain jump: the first air, over a second of it (the trick's stage: its prompt is at the apex)
///   ROOF ONE (24 m)      land anywhere, SLIDE it (its lit line runs down the right side, to the wall); a FACADE stands beside its last six metres and runs on eight metres
///                        over the gap: slide-jump at the edge onto it, WALL RUN it out over the void, jump off its end.
///                        The gap is 1.1 times what a lazy slide jump covers: the wall is the way across
///   ROOF TWO (24 m)      II and III: a low BEAM across it three metres before its edge: slide under it, then jump in
///                        the three metres that are left (a second jump clears the beam, for a pip)
///   ROOF THREE (16 m)    III: NARROW, five metres wide and not twelve: line it up in the air and hold it
/// The roofs fall 4 / 5 / 6 m each and are pitched 20°, so every landing turns the fall into speed and the slide adds
/// its own; a gap is 70 / 75 / 80% of what the arc off the edge before it covers at the speed that roof makes. The
/// first roof is long so that a fast arrival still finds it. No gate anywhere: arrive too fast and the roof is missed
public class TrickDescent : Beat
{
    public override string Name => "TRICK DESCENT";
    public override float EntrySpeed(LevelMetricsMath.Values v, int d) => v.baseRunSpeed;
    const float Deg = 20f, FirstRoof = 24f, RoofLen = 16f, Edge = 6f, WallBack = 6f, WallOut = 8f;
    static int Airs(int d) => (int)Pick(d, 2, 3, 3);
    static float Step(int d) => Pick(d, 4f, 5f, 6f);
    static float Frac(int d) => Pick(d, 0.7f, 0.75f, 0.8f);
    public override string Note(LevelMetricsMath.Values v, int d) => d <= 1
        ? "2 big airs · JUMP the edge, TRICK in the air · SLIDE the roof, then WALL RUN the facade off its end and jump"
        : d == 2 ? "3 big airs · roof one: slide, WALL RUN off its end · roof two: slide UNDER THE BEAM, then jump · TRICK every air"
        : "3 big airs · roof one: WALL RUN off its end · roof two: slide UNDER THE BEAM, then jump · roof three is NARROW: hold it";
    public override float Build(Frame f, int d)
    {
        var v = f.v;
        float cos = BeatMath.Cos(Deg), sin = BeatMath.Sin(Deg), tan = sin / cos;
        int n = Airs(d);
        f.Deck(0f, Edge, 0f, HalfW, "DescentEdge"); f.Line(0f, 0.5f, Edge - 0.5f, 0f);
        f.LampPost(HalfW + 0.4f, 0f, Edge - 1f);
        float z = Edge, y = 0f, s = v.baseRunSpeed;
        for (int k = 0; k < n; k++)
        {
            float h = Step(d);
            float roofLen = k <= 1 ? FirstRoof : RoofLen, dz = roofLen * cos, fall = roofLen * sin;   // the first roof is long for a fast arrival, the second for a wall run caught early (it leaves higher and flies further)
            bool narrow = d >= 3 && k == n - 1;
            float halfW = narrow ? 2.5f : HalfW + 1f;
            float along, up, reach, gap, airTime;
            if (k == 0)
            {
                // off the flat edge: a plain jump
                along = s; up = v.jumpImpulse;
                var arc = LevelMetricsMath.Jump(v, along, up, -h, false);
                reach = arc.distance; airTime = arc.time; gap = reach * Frac(d);
            }
            else if (k == 1)
            {
                // THE WALL RUN off roof one: the facade beside its last metres runs on over the gap; the slide jump at the edge
                // puts the body on it (the kept rise and the pop), it is run to its end, and the jump off it is the air
                float lip = s * cos;
                f.Wall(HalfW + 1f + 0.6f, z - WallBack, z + WallOut, y - 1.5f, 4f);
                float t = WallOut / Mathf.Max(1f, lip), wy = 0f;
                float wvy = Mathf.Min(v.slideJumpVerticalImpulse, Mathf.Max(0f, v.wallRunEntryRiseKeep)) + v.wallRunEntryPop;
                for (float u = 0f; u < t; u += v.fixedDeltaTime)
                {
                    float life = Mathf.Min(1f, u / v.wallRunMaxDuration);
                    wvy -= v.gravity * Mathf.Lerp(v.wallRunGravityFactor, 1f, life * life) * v.fixedDeltaTime;
                    wy += wvy * v.fixedDeltaTime;
                }
                along = lip * v.wallRunJumpTangentBoost; up = Mathf.Max(wvy, 0f) * 0.5f + v.wallRunJumpUpImpulse;
                var arc = LevelMetricsMath.Jump(v, along, up, -(h + wy), false);
                reach = WallOut + arc.distance; airTime = arc.time;
                float slideOnly = LevelMetricsMath.Jump(v, lip, v.slideJumpVerticalImpulse, -h, false).distance;
                gap = Mathf.Min(slideOnly * 1.1f, reach * 0.82f);   // a tenth more than the lazy slide jump covers: the wall is the way, and a perfect slide jump with a trick's burst just makes it without
            }
            else
            {
                // off roof two: the slide jump in the three metres after its beam (the lazy one: no multiplier counted)
                along = s * cos; up = v.slideJumpVerticalImpulse;
                var arc = LevelMetricsMath.Jump(v, along, up, -h, false);
                reach = arc.distance; airTime = arc.time; gap = reach * Frac(d);
            }
            z += gap; y -= h;
            f.Slope(z, y, z + dz, y - fall, halfW, narrow ? "DescentRidge" : "DescentRoof"); f.LineRun(k == 0 ? HalfW + 0.2f : 0f, z, y, z + dz, y - fall);   // roof one's lit line runs down its right side, a body's width from the facade it leads to
            f.Lamp(0f, y - fall + 6f, z + dz);
            if (k == 1 && n >= 3)
            {
                // THE BEAM on roof two: a metre deep, its underside 1.4 m over the pitched roof, three metres before the edge
                float zb = z + dz - 3f, yb = y - (dz - 3f) * tan;
                f.Mass(-halfW - 0.1f, halfW + 0.1f, yb + 1.4f, yb + 2.4f, zb - 0.45f, zb + 0.45f, "Lintel");
                f.Mass(-halfW - 0.8f, -halfW - 0.1f, f.Structural ? f.groundY : yb - 1f, yb + 2.4f, zb - 0.45f, zb + 0.45f, "Post");
                f.Mass(halfW + 0.1f, halfW + 0.8f, f.Structural ? f.groundY : yb - 1f, yb + 2.4f, zb - 0.45f, zb + 0.45f, "Post");
            }
            // the speed at this roof's lower edge: the landing turns the fall into speed down the pitch, the slide adds the rest of the roof
            float vyLand = Mathf.Max(0f, v.gravity * airTime - up);
            float landed = Mathf.Max(v.baseRunSpeed, along * cos + vyLand * sin);
            float landIn = Mathf.Clamp((reach - gap) / cos, 0f, roofLen);
            s = BeatMath.SlideGain(v, landed, Deg, roofLen - landIn);
            z += dz; y -= fall;
        }
        f.Deck(z, z + 8f, y, HalfW, "RunOut"); f.Line(0f, z + 0.5f, z + 7.5f, y);
        f.Advance(z + 8f, y);
        return s;
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

    /// The speed a beat's gap is sized for, from a checkpoint named for it: the validator's snowball judges a gap against it
    public static float SizedSpeed(string checkpointName, LevelMetricsMath.Values v)
    {
        if (string.IsNullOrEmpty(checkpointName)) return v.baseRunSpeed;
        foreach (var b in All)
        {
            if (!checkpointName.StartsWith(b.Name)) continue;
            string rest = checkpointName.Substring(b.Name.Length).Trim();
            int d = rest == "III" ? 3 : rest == "II" ? 2 : 1;
            return b.SizedSpeed(v, d);
        }
        return v.baseRunSpeed;
    }

    /// Every beat, in the order the gym lays them out (the BREATHER went on 2026-10-29: a long flat straight is speed for nothing; the TRICK DESCENT came on 2026-11-10 when CONTROL lost its hop stones)
    public static readonly List<Beat> All = new List<Beat>
    {
        new GapJump(), new WallrunChain(), new SlideTunnel(), new RampLaunch(), new Drop(), new TrickDescent(), new Control(),
        new Fork(), new Skip(), new TechCheck(), new HiddenRoute(), new Turn(),
    };
}
