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

/// GAP JUMP: a four-metre launch, a gap at 70 / 80 / 90% of the reach at the expected speed, a SHORT LANDING (7 / 6 / 5 m)
/// with a portal at its end: the arc the beat is sized for lands on the first half of it; an arc a few metres a second
/// faster is still up at the lintel and stops against it
public class GapJump : Beat
{
    public override string Name => "GAP JUMP";
    public override float EntrySpeed(LevelMetricsMath.Values v, int d) => Pick(d, v.baseRunSpeed, 12f, 16f);
    public override string Note(LevelMetricsMath.Values v, int d) { float s = EntrySpeed(v, d), dy = Pick(d, 0f, -1f, -2f); return "a gap of " + F1(BeatMath.Gap(v, s, dy, Pick(d, 0.7f, 0.8f, 0.9f))) + " m at " + F0(s) + " m/s · land BEFORE the portal"; }
    public override float Build(Frame f, int d)
    {
        float s = EntrySpeed(f.v, d), dy = Pick(d, 0f, -1f, -2f), gap = BeatMath.Gap(f.v, s, dy, Pick(d, 0.7f, 0.8f, 0.9f)), land = Pick(d, 4f, 6f, 5f);   // the lintel takes the arc at about 1.4× the sized speed
        f.Deck(0f, 4f, 0f, HalfW, "Launch"); f.Line(0f, 0.5f, 3.5f, 0f);
        f.Deck(4f + gap, 4f + gap + land, dy, HalfW, "Landing"); f.Line(0f, 4.5f + gap, 3.5f + gap + land, dy);
        f.Portal(4f + gap + land, dy, HalfW);
        f.Advance(4f + gap + land, dy);
        return s;
    }
}

/// WALLRUN CHAIN: two to four walls staggered left and right over a void, 4 / 5.5 / 7 m apart along the line, each
/// eight metres long; you run one, jump across to the next, land on the far floor a little lower. The landing runs
/// from two metres past the last wall to where the jump off that wall's END lands at the sized speed, plus 3 / 3.5 / 3.5 m,
/// and a portal stands there: the jump off the last wall has to be a landing and not a flight
public class WallrunChain : Beat
{
    public override string Name => "WALLRUN CHAIN";
    public override float EntrySpeed(LevelMetricsMath.Values v, int d) => Pick(d, v.baseRunSpeed, 10f, 12f);
    public override string Note(LevelMetricsMath.Values v, int d) => Pick(d, 2, 3, 4) + " walls, " + F1(Pick(d, 4f, 5.5f, 7f)) + " m apart · land before the portal";
    public override float Build(Frame f, int d)
    {
        float s = EntrySpeed(f.v, d), between = Pick(d, 4f, 5.5f, 7f);
        int n = (int)Pick(d, 2, 3, 4);
        float dyLand = -1f - 0.4f * n;
        var off = LevelMetricsMath.WallJump(f.v, s, 0f);
        float land = LevelMetricsMath.Jump(f.v, off.h, off.vy, dyLand, false).distance - 2f + Pick(d, 3f, 3.5f, 3.5f);   // from the wall's end: the arc, less the two metres of void, plus the slack (about 1.3× the sized speed)
        const float lane = 2.6f, wallLen = 8f;
        f.Deck(0f, 6f, 0f, lane, "Launch"); f.Line(0f, 0.5f, 5.5f, 0f);
        float z = 4f;
        for (int i = 0; i < n; i++)
        {
            float side = i % 2 == 0 ? 1f : -1f;
            f.Wall(side * (lane + 0.6f), z, z + wallLen, 0.3f - i * 0.4f, 4f);
            z += wallLen + between;
        }
        float dy = dyLand;
        float zLand = z - between + 2f;
        f.Deck(zLand, zLand + land, dy, HalfW, "Landing"); f.Line(0f, zLand + 0.5f, zLand + land - 0.5f, dy);
        f.Portal(zLand + land, dy, HalfW);
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
    public override string Note(LevelMetricsMath.Values v, int d) { float len = Pick(d, 8f, 12f, 16f), lip = BeatMath.SlideGain(v, v.baseRunSpeed, Deg, len); return "SLIDE the " + F0(len) + " m ramp to " + F0(lip) + " m/s, jump the " + F1(BeatMath.Gap(v, lip, -2f, Pick(d, 0.7f, 0.8f, 0.9f))) + " m gap · land high on the slope, before the portal"; }
    public override float Build(Frame f, int d)
    {
        float len = Pick(d, 8f, 12f, 16f), lip = BeatMath.SlideGain(f.v, f.v.baseRunSpeed, Deg, len);
        float dz = len * BeatMath.Cos(Deg), drop = len * BeatMath.Sin(Deg);
        float gap = BeatMath.Gap(f.v, lip, -2f, Pick(d, 0.7f, 0.8f, 0.9f));
        f.Slope(0f, 0f, dz, -drop, HalfW, "LaunchRamp");
        float yLand = -drop - 2f, landLen = 10f, landDrop = landLen * BeatMath.Sin(10f), landDz = landLen * BeatMath.Cos(10f);
        f.Slope(dz + gap, yLand, dz + gap + landDz, yLand - landDrop, HalfW, "LandingSlope");
        const float portalDown = 9.5f;   // along the landing slope: at its foot
        f.Portal(dz + gap + portalDown * BeatMath.Cos(10f), yLand - portalDown * BeatMath.Sin(10f), HalfW);
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
/// slow for a hopper too) that ends in a slope down, the RISKY side one to three hop stones at 80% of the reach at
/// 12 m/s, each a metre lower, three metres deep: that speed and not much more — and they meet again under a portal
public class Fork : Beat
{
    public override string Name => "FORK";
    public override float EntrySpeed(LevelMetricsMath.Values v, int d) => 12f;
    public override string Note(LevelMetricsMath.Values v, int d) => "left: safe and slow, a colonnade · right: " + (int)Pick(d, 1, 2, 3) + " stone(s) " + F1(BeatMath.Gap(v, 12f, -1f, 0.8f)) + " m apart at 12 m/s";
    public override float Build(Frame f, int d)
    {
        int n = (int)Pick(d, 1, 2, 3);
        float gap = BeatMath.Gap(f.v, 12f, -1f, 0.8f);
        const float first = 4f, stone = 3f, lane = 2.75f, off = 4.25f;
        float len = first + n * (gap + stone) + gap;   // the last gap lands on the rejoin
        f.Deck(0f, 4f, 0f, HalfW, "ForkMouth");
        f.Mass(-1.5f, 1.5f, -1f, 4f, 4f, 4f + len, "Divider");
        // the risky right side: a floor, then stones, each a metre lower, the rejoin a metre lower still
        float z = 4f, y = 0f;
        f.Deck(z, z + first, y, lane, "RiskyFloor", off);
        z += first;
        for (int i = 0; i < n; i++)
        {
            z += gap; y -= 1f;
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
        f.Portal(4f + len + 3f, y, HalfW);
        f.Advance(4f + len + 6f, y);
        return 12f;
    }
}

/// SKIP: a gap straight ahead at 90% of the reach at 14 / 16 / 18 m/s that cuts out the long way round it; the landing
/// is five metres under a portal (the long way comes in under the lintel's end)
public class Skip : Beat
{
    public override string Name => "SKIP";
    public override float EntrySpeed(LevelMetricsMath.Values v, int d) => Pick(d, 14f, 16f, 18f);
    public override string Note(LevelMetricsMath.Values v, int d) { float s = EntrySpeed(v, d); return "straight: " + F1(BeatMath.Gap(v, s, -1f, 0.9f)) + " m at " + F0(s) + " m/s, land before the portal · or the long way right"; }
    public override float Build(Frame f, int d)
    {
        float s = EntrySpeed(f.v, d), gap = BeatMath.Gap(f.v, s, -1f, 0.9f);
        const float laneW = 2f, outX = 11f, land = 5f;
        f.Deck(0f, 4f, 0f, HalfW, "Launch"); f.Line(0f, 0.5f, 3.5f, 0f);
        f.Deck(4f + gap, 4f + gap + land, -1f, HalfW, "Landing"); f.Line(0f, 4.5f + gap, 3.5f + gap + land, -1f);
        f.Portal(4f + gap + land, -1f, HalfW, 2.2f, 5.5f, true, false);
        // the long way: a floor out to the right from the launch, a slope forward and down beside the gap, a floor back in to the landing
        f.Mass(HalfW - 0.2f, outX + laneW, -1f, 0f, 1f, 4f, "WayOut", LevelKit.Floor);
        f.Slope(4f, 0f, 6f + gap, -1f, laneW, "WayAlong", outX);
        f.Mass(HalfW - 0.2f, outX + laneW, -2f, -1f, 6f + gap, 8.5f + gap, "WayBack", LevelKit.Floor);   // ends half a metre short of the landing's end, so the long way comes back onto the landing and not past it
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
    public override string Note(LevelMetricsMath.Values v, int d) => (d <= 1 ? "WALL RUN the gap" : d == 2 ? "SLIDE under the bar, SLIDE-JUMP the gap" : "DOUBLE JUMP the gap (a pip)") + " · land before the portal";
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
            f.Portal(4f + gap + land, dy, HalfW);
            f.Advance(4f + gap + land, dy);
            return s;
        }
        if (d == 2)
        {
            float gap = BeatMath.SlideJumpReach(f.v, s, -0.5f) * 0.8f, land = 5f;
            f.Deck(0f, 4f, 0f, HalfW, "Launch"); f.Line(0f, 0.5f, 3.5f, 0f);
            f.Mass(-HalfW - 1f, HalfW + 1f, 1.25f, 2.25f, 0.5f, 3f, "Bar");
            f.Deck(4f + gap, 4f + gap + land, -0.5f, HalfW, "Landing"); f.Line(0f, 4.5f + gap, 3.5f + gap + land, -0.5f);
            f.Portal(4f + gap + land, -0.5f, HalfW);
            f.Advance(4f + gap + land, -0.5f);
            return s;
        }
        float dj = BeatMath.DoubleReach(f.v, s, -1f) * 0.85f, land3 = 7f;   // the double jump's arc is long: seven metres take it to about 1.25× the sized speed
        f.Deck(0f, 4f, 0f, HalfW, "Launch"); f.Line(0f, 0.5f, 3.5f, 0f);
        f.Deck(4f + dj, 4f + dj + land3, -1f, HalfW, "Landing"); f.Line(0f, 4.5f + dj, 3.5f + dj + land3, -1f);
        f.Portal(4f + dj + land3, -1f, HalfW);
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

/// CONTROL (2026-10-29; the user: "a new beat type built around precision instead of speed: tight platforms, short
/// jumps, turns, where going too fast makes you fail"). FIVE HOP STONES, three metres square, stepping left and right
/// of the line, each spaced so the clean arc at the speed the beat names lands halfway across the next: slower falls
/// short, a fifth faster (a perfect hop's worth, ×1.18) is at the far edge and more is off it — the speed has to be
/// KEPT with lazy hops, and steered. Then I lands on a square and turns the corner on the ground (a turn at speed is a flight off
/// the square) to a short hop and the exit; II runs a 1.2 m beam with a kink (a pad to turn on) and III a 1 m beam
/// with two, and both end on a short landing under a portal. The stones and beams stand over nothing: too fast is a fall
public class Control : Beat
{
    public override string Name => "CONTROL";
    public override float EntrySpeed(LevelMetricsMath.Values v, int d) => Pick(d, v.baseRunSpeed, 10f, 12f);
    public override string Note(LevelMetricsMath.Values v, int d) => "KEEP " + F0(EntrySpeed(v, d)) + " m/s, no more: five stones " + F1(StoneGap(v, d)) + " m apart" + (d <= 1 ? ", the corner, a hop" : d == 2 ? ", a beam with a kink, the portal" : ", a 1 m beam with two kinks, the portal") + " · too fast is a fall";
    static float StoneDeep(int d) => 3f;
    static float StoneGap(LevelMetricsMath.Values v, int d) => BeatMath.Reach(v, Pick(d, v.baseRunSpeed, 10f, 12f), 0f) - 0.5f * StoneDeep(d);
    public override float Build(Frame f, int d)
    {
        float s = EntrySpeed(f.v, d), deep = StoneDeep(d), gap = StoneGap(f.v, d);
        const float wide = 3f, offset = 1.25f, beamLen = 5f;
        f.Deck(0f, 4f, 0f, HalfW, "ControlLaunch"); f.Line(0f, 0.5f, 3.5f, 0f);
        // the stones: left, right, left… of the line, one arc apart
        float z = 4f;
        for (int i = 0; i < 5; i++)
        {
            z += gap;
            f.Stone(i % 2 == 0 ? offset : -offset, z + deep * 0.5f, 0f, wide, deep);
            z += deep;
        }
        z += gap;
        if (d <= 1)
        {
            // the square and the corner: the run turns LEFT on the ground (the last stone stands right of the line, so the
            // exit is as far from it as the square allows), hops a four-metre gap and runs the exit deck; from the stone the
            // exit is eight metres off, a hop the stones' speed cannot make, so the corner cannot be skipped
            f.Deck(z, z + 6f, 0f, 3f, "ControlLanding"); f.LineBetween(0f, z + 0.5f, 0f, z + 3f, 0f); f.LineBetween(0f, z + 3f, -2.5f, z + 3f, 0f);
            f.Advance(z + 3f, 0f); f.Turn(-90f); f.Advance(3f + 4f, 0f);
            f.Deck(0f, 8f, 0f, 3f, "ControlExit"); f.Line(0f, 0.5f, 7.5f, 0f);
            f.Advance(8f, 0f);
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
            g.Portal(4f, 0f, 2f);
            g.Advance(4f, 0f);
            f.origin = g.origin; f.Turn(90f);
            return s;
        }
        // III: a second kink back to the line's heading, a third leg, the landing and the portal straight on
        f.Deck(zk - pad * 0.5f, zk + pad * 0.5f, 0f, pad * 0.5f, "ControlKink", xk + pad * 0.5f); f.LineBetween(xk, zk, xk + pad * 0.5f, zk, 0f); f.LineBetween(xk + pad * 0.5f, zk, xk + pad * 0.5f, zk + pad * 0.5f, 0f);
        xk += pad * 0.5f; zk += pad * 0.5f;
        f.Beam(xk, zk, xk, zk + beamLen, 0f, beamW); f.LineBetween(xk, zk, xk, zk + beamLen, 0f); zk += beamLen;
        f.Deck(zk, zk + 4f, 0f, 2f, "ControlLanding", xk); f.LineBetween(xk, zk + 0.5f, xk, zk + 3.5f, 0f);
        f.Portal(zk + 4f, 0f, 2f, 2.2f, 5.5f, true, true, xk);
        f.origin = f.P(xk, 0f, zk + 4f);
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

    /// Every beat, in the order the gym lays them out (the BREATHER went on 2026-10-29: a long flat straight is speed for nothing)
    public static readonly List<Beat> All = new List<Beat>
    {
        new GapJump(), new WallrunChain(), new SlideTunnel(), new RampLaunch(), new Drop(), new Control(),
        new Fork(), new Skip(), new TechCheck(), new HiddenRoute(), new Turn(),
    };
}
