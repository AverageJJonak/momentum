using UnityEngine;

/// <summary>
/// THE BEAT KIT (Phase 3, 2026-10-27; the user: "parametric level beats that generate geometry from LevelMetrics").
/// A beat is built in a FRAME: an origin on the line's floor and a heading, so every piece is placed in the frame's
/// own x (right), y (up) and z (forward) and the same beat stands anywhere, facing anywhere. The primitives are the
/// game's solid pieces (LevelKit.SlabRot: a box with a collider, the floor, slope, wall and dark materials) placed
/// with the frame's rotation; the sizing comes from LevelMetricsMath, never from a literal. Nothing here has a lip, a
/// kerb or a plinth top: a floor is a slab, a drop is an edge, a climb is a slope.
/// </summary>
public struct Pose
{
    public Vector3 pos; public float yaw; public float speed;
    public Pose(Vector3 pos, float yaw, float speed) { this.pos = pos; this.yaw = yaw; this.speed = speed; }
}

public class Frame
{
    public readonly Transform root;
    public readonly LevelMetricsMath.Values v;
    public Zone zone = Zone.Utopia;
    public Vector3 origin;
    public float yaw;
    public float lowest, highest;   // the floor heights built so far, for the kill floor and the vista
    public Bounds built;            // everything placed, for the vista's footprint
    /// THE GROUND (2026-10-29; the user: "no floating platforms: every surface must belong to a structure"). When
    /// set, every floor stands on a BODY down to this height (a rooftop on its building, a stone on its pillar), every
    /// beam on posts, every run wall is a FACADE twelve metres tall with its building under it, every portal a gate
    /// as tall, so nothing floats and nothing a wall run's lift can reach has a top (LevelMetricsMath.WallLift: jumped
    /// at, a run and its jumps reach eleven metres). NaN: the old bare slabs
    public float groundY = float.NaN;
    public bool Structural => !float.IsNaN(groundY);
    public const float Reveal = 0.3f, FacadeHeight = 14f, GateHeight = 15f;   // the lift reaches 11.4 m; a facade or a gate may stand up to three metres below the floor a run is lifted from

    public Frame(Transform root, LevelMetricsMath.Values v, Pose at)
    {
        this.root = root; this.v = v; origin = at.pos; yaw = at.yaw;
        lowest = highest = at.pos.y;
        built = new Bounds(at.pos, Vector3.one);
    }

    public Quaternion Rot => Quaternion.Euler(0f, yaw, 0f);
    public Vector3 F => Rot * Vector3.forward;
    public Vector3 R => Rot * Vector3.right;
    /// A frame point: `x` to the right, `y` up, `z` forward of the origin
    public Vector3 P(float x, float y, float z) => origin + R * x + Vector3.up * y + F * z;
    public Pose At(float x, float y, float z, float speed) => new Pose(P(x, y, z), yaw, speed);
    /// The origin moves `z` forward and `dy` up: the next thing starts there
    public void Advance(float z, float dy) { origin = P(0f, dy, z); Note(origin); }
    /// The heading turns right by `deg` (negative: left), about the origin
    public void Turn(float deg) => yaw += deg;
    /// A frame standing at `at` in this frame, turned `deg` right: a leg off the line
    public Frame Sub(float x, float y, float z, float deg) => new Frame(root, v, new Pose(P(x, y, z), yaw + deg, 0f)) { zone = zone, lowest = lowest, highest = highest, built = built, groundY = groundY };
    public void Note(Vector3 p) { lowest = Mathf.Min(lowest, p.y); highest = Mathf.Max(highest, p.y); built.Encapsulate(p); }

    // ---------------------------------------------------------------- the solid pieces

    GameObject Box(Vector3 centre, Vector3 size, Material mat, string name, Quaternion rot)
    {
        Note(centre + Vector3.up * size.y * 0.5f); Note(centre - Vector3.up * size.y * 0.5f);
        built.Encapsulate(centre + R * size.x * 0.5f + F * size.z * 0.5f); built.Encapsulate(centre - R * size.x * 0.5f - F * size.z * 0.5f);
        return LevelKit.SlabRot(root, centre, size, mat, name, rot);
    }

    /// A flat floor from z0 to z1, its top at `y`, `halfW` each side of `x`; a metre thick. On a body to the ground when the frame has one: a rooftop, a pillar's top
    public GameObject Deck(float z0, float z1, float y, float halfW, string name = "Deck", float x = 0f, float thick = 1f)
    {
        var slab = Box(P(x, y - thick * 0.5f, (z0 + z1) * 0.5f), new Vector3(halfW * 2f, thick, z1 - z0), LevelKit.Floor, name, Rot);
        Body(x - halfW, x + halfW, y - thick - 0.05f, z0, z1, name + "Body");
        return slab;
    }

    /// A solid block between two heights (a mass, a divider, a wall across): dark
    public GameObject Mass(float x0, float x1, float y0, float y1, float z0, float z1, string name = "Mass", Material mat = null)
        => Box(P((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, y1 - y0, z1 - z0), mat ?? LevelKit.Dark, name, Rot);

    /// A BODY under a floor, from the ground up to `yTop`, inset from the floor's edges so the slab reads as its cornice
    /// (a sliver of the body is inside the slab: no two faces on one plane). Nothing without a ground
    public void Body(float x0, float x1, float yTop, float z0, float z1, string name = "Body")
    {
        if (!Structural || yTop - groundY < 1.5f) return;
        float xi = Mathf.Min(Reveal, (x1 - x0) * 0.25f), zi = Mathf.Min(Reveal, (z1 - z0) * 0.25f);
        Mass(x0 + xi, x1 - xi, groundY, yTop, z0 + zi, z1 - zi, name, LevelKit.Dark);
    }

    /// A POST from the ground up to `yTop` at (x, z), `size` square: under a beam's end, beside a bar. Named for the validator: no floor, no wall to run
    public void Post(float x, float z, float yTop, float size = 1f)
    {
        if (!Structural || yTop - groundY < 1.5f) return;
        Mass(x - size * 0.5f, x + size * 0.5f, groundY, yTop, z - size * 0.5f, z + size * 0.5f, "Post", LevelKit.Dark);
    }

    /// THE GROUND itself: a dark plain at the frame's ground, three kilometres across, with no collider (the kill floor stands above it)
    public void Ground()
    {
        if (!Structural) return;
        LevelKit.Prim(root, new Vector3(origin.x, groundY - 0.5f, origin.z), new Vector3(3000f, 1f, 3000f), LevelKit.Dark, "Ground");
    }

    /// A slope whose TOP runs from (z0, y0) to (z1, y1), `halfW` each side of `x`; the pitch follows the line. A slope of
    /// the kit's own material gets a body under its lower end (a pitched roof over its building); one given another material (a roof) does not
    public GameObject Slope(float z0, float y0, float z1, float y1, float halfW, string name = "Slope", float x = 0f, float thick = 1f, Material mat = null)
    {
        float dz = z1 - z0, dy = y1 - y0, len = Mathf.Sqrt(dz * dz + dy * dy);
        float pitch = Mathf.Atan2(-dy, dz) * Mathf.Rad2Deg;   // a lower far end pitches the nose down
        var rot = Rot * Quaternion.Euler(pitch, 0f, 0f);
        Vector3 topMid = P(x, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f);
        Note(topMid);
        var slab = Box(topMid - rot * Vector3.up * (thick * 0.5f), new Vector3(halfW * 2f, thick, len), mat ?? LevelKit.Slope, name, rot);
        if (mat == null) Body(x - halfW, x + halfW, Mathf.Min(y0, y1) - thick - 0.05f, Mathf.Min(z0, z1), Mathf.Max(z0, z1), name + "Body");
        return slab;
    }

    /// A ceiling parallel to a slope: its UNDERSIDE runs `clear` metres over the slope's top from z0 to z1
    public GameObject Roof(float z0, float y0, float z1, float y1, float clear, float halfW, string name = "Roof", float x = 0f)
        => Slope(z0, y0 + clear + 1f, z1, y1 + clear + 1f, halfW, name, x, 1f, LevelKit.Dark);

    /// A wall to run: a metre thick, along z at `x`, standing from `yBottom` up `height`. Named for the validator. With a
    /// ground it is a FACADE: twelve metres tall at least (nothing a run's lift can reach has a top) on a building below
    public GameObject Wall(float x, float z0, float z1, float yBottom, float height, string name = "KickWall")
    {
        float h = Structural ? Mathf.Max(height, FacadeHeight) : height;
        var band = Box(P(x, yBottom + h * 0.5f, (z0 + z1) * 0.5f), new Vector3(1f, h, z1 - z0), LevelKit.Wall, name, Rot);
        Body(x - 0.5f, x + 0.5f, yBottom - 0.05f, z0, z1, "FacadeBody");
        return band;
    }

    /// A wall across the line at `z`, from x0 to x1
    public GameObject WallAcross(float x0, float x1, float z, float yBottom, float height, string name = "Wall")
        => Box(P((x0 + x1) * 0.5f, yBottom + height * 0.5f, z), new Vector3(x1 - x0, height, 1f), LevelKit.Wall, name, Rot);

    /// A PORTAL across the line at `z` (2026-10-29; the user: "landing spots that punish overshooting"): a lintel whose
    /// underside is `clear` metres over the floor at `y`, solid up to `top`, a post each side just outside the deck. A
    /// run or a slide passes under it; an arc still in the air here hits the lintel and stops. So a short landing with a
    /// portal at its end takes every speed up to the one whose arc is still up at the lintel, and no more. Portals every
    /// eight metres along a floor (a colonnade) cap a hop chain at about 11 m/s: a longer hop meets the next lintel. The
    /// pieces are 0.9 m thick so the validator reads the lintel as a ceiling and the posts as nothing (not as walls to run)
    public void Portal(float z, float y, float halfW, float clear = 2.2f, float top = 5.5f, bool leftPost = true, bool rightPost = true, float x = 0f)
    {
        const float d = 0.45f;
        if (Structural) top = Mathf.Max(top, GateHeight);   // a gate: nothing an arc or a lift passes over
        float foot = Structural ? groundY : y - 1f;
        float x0 = x - halfW - (leftPost ? 0.7f : 0.1f), x1 = x + halfW + (rightPost ? 0.7f : 0.1f);
        Mass(leftPost ? x - halfW - 0.1f : x0, rightPost ? x + halfW + 0.1f : x1, y + clear, y + top, z - d, z + d, "Lintel");
        if (leftPost) Mass(x0, x - halfW - 0.1f, foot, y + top, z - d, z + d, "Post");
        if (rightPost) Mass(x + halfW + 0.1f, x1, foot, y + top, z - d, z + d, "Post");
    }

    /// A HOP STONE: a floor `wide` across and `deep` along the line, centred at (x, z), its top at y. Its depth is the
    /// landing's tolerance: the arc the stone is spaced for lands sixty percent of the way across it
    public GameObject Stone(float x, float z, float y, float wide, float deep, string name = "ControlStone")
        => Deck(z - deep * 0.5f, z + deep * 0.5f, y, wide * 0.5f, name, x);

    /// A BEAM: a floor `wide` across, from (x0, z0) to (x1, z1) along the frame's x or z (the other equal), its top at y;
    /// it ends exactly at its ends, so a pad it meets is touched and not overlapped. A walkway: posts under its ends
    public GameObject Beam(float x0, float z0, float x1, float z1, float y, float wide, string name = "ControlBeam")
    {
        float hw = wide * 0.5f;
        var beam = Mathf.Abs(z1 - z0) >= Mathf.Abs(x1 - x0)
            ? Mass(x0 - hw, x0 + hw, y - 1f, y, Mathf.Min(z0, z1), Mathf.Max(z0, z1), name, LevelKit.Floor)
            : Mass(Mathf.Min(x0, x1), Mathf.Max(x0, x1), y - 1f, y, z0 - hw, z0 + hw, name, LevelKit.Floor);
        Vector3 d = new Vector3(x1 - x0, 0f, z1 - z0); float len = d.magnitude; d /= Mathf.Max(0.01f, len);
        Post(x0 + d.x * hw, z0 + d.z * hw, y - 1.05f, wide * 0.8f);
        Post(x1 - d.x * hw, z1 - d.z * hw, y - 1.05f, wide * 0.8f);
        return beam;
    }

    // ---------------------------------------------------------------- the marks

    /// The lit line down a floor (HARD LINE): where the run goes
    public void Line(float x, float z0, float z1, float y) => LevelKit.LineRun(root, P(x, y, z0), P(x, y, z1), zone);
    /// The lit line down a slope: from (z0, y0) to (z1, y1)
    public void LineRun(float x, float z0, float y0, float z1, float y1) => LevelKit.LineRun(root, P(x, y0, z0), P(x, y1, z1), zone);
    /// The lit line between two points of the floor at `y`: across the frame as well as along it
    public void LineBetween(float x0, float z0, float x1, float z1, float y) => LevelKit.LineRun(root, P(x0, y, z0), P(x1, y, z1), zone);

    /// A sign over the line, read by whoever comes along it
    /// A sign over the line, read from the reset pad ten metres back: `size` 1 = 40 cm glyphs (LevelKit.Text's size is
    /// glyph height ÷ 6.4 m: 1.0 there is a six-metre letter, which is what the first gym stood up), the lines shrunk
    /// to fit the deck's width
    public void Sign(float x, float y, float z, string text, float size = 1.1f) => LevelKit.Text(root, P(x, y, z), Rot, text, 0.0625f * size, MenuStyle.Paper, "BeatSign", 9f, false, 90f, 220f);

    /// A jump pad on the floor at (x, z), throwing `up` and `forward` metres a second along the line
    public void Pad(float x, float z, float y, float up, float forward) => LevelKit.JumpPad(root, P(x, y, z), yaw, up, forward);

    /// A reset pad: the checkpoint volume across the line (a respawn stands you a metre past it, facing on), a brass strip on the floor
    public void Checkpoint(string name, float z, float y, float halfW)
    {
        Vector3 centre = P(0f, y + 7f, z);
        float box = halfW * 2f + 6f;
        CityDistrict.Checkpoint(root, name, new Bounds(centre, new Vector3(box, 16f, box)), F, P(0f, y, z + 1f));
        LevelKit.BrassStrip(root, P(-halfW + 0.3f, y, z), P(halfW - 0.3f, y, z), 0.2f);
    }

    /// A lamp over the line
    public void Lamp(float x, float y, float z, float range = 24f, float intensity = 2f) => LevelKit.Light(root, P(x, y, z), range, intensity);
}

/// <summary>The sums every beat sizes itself with: reach, gaps, the slide's gains, the walls' spacing</summary>
public static class BeatMath
{
    /// A clean jump's reach at `speed` to a landing `dy` above the launch (negative: below)
    public static float Reach(LevelMetricsMath.Values v, float speed, float dy) => LevelMetricsMath.Jump(v, speed, v.jumpImpulse, dy, false).distance;
    /// The gap that asks `fraction` of the reach: 0.7 safe, 0.8 real, 0.9 a skip
    public static float Gap(LevelMetricsMath.Values v, float speed, float dy, float fraction) => Reach(v, speed, dy) * fraction;
    /// The slide jump's reach at `speed` (its lower arc, its horizontal pay)
    public static float SlideJumpReach(LevelMetricsMath.Values v, float speed, float dy) => LevelMetricsMath.Jump(v, speed * LevelMetricsMath.SlideJumpMultiplier(v, speed, false), v.slideJumpVerticalImpulse, dy, false).distance;
    /// The double jump's reach at `speed` over a gap (the late press)
    public static float DoubleReach(LevelMetricsMath.Values v, float speed, float dy) => LevelMetricsMath.DoubleJump(v, speed, dy, true, false).distance;
    /// Metres along a `deg` slope that take a slide from `from` to `to` m/s
    public static float SlideRun(LevelMetricsMath.Values v, float from, float to, float deg)
    {
        float a = LevelMetricsMath.SlideSlopeGain(v, deg);
        return a <= 0f ? 0f : Mathf.Max(0f, (to * to - from * from) / (2f * a));
    }
    /// The speed a slide of `len` metres down a `deg` slope reaches from `from`
    public static float SlideGain(LevelMetricsMath.Values v, float from, float deg, float len) => Mathf.Sqrt(from * from + 2f * LevelMetricsMath.SlideSlopeGain(v, deg) * len);
    /// How far a wall run at `speed` stays level, and how far the jump off it then carries to a landing `dy` below
    public static float WallRunCarry(LevelMetricsMath.Values v, float speed, float dy)
    {
        var run = LevelMetricsMath.Run(v, speed, 0f);
        var off = LevelMetricsMath.WallJump(v, speed, 0f);
        return run.levelDistance + LevelMetricsMath.Jump(v, off.h, off.vy, dy, false).distance;
    }
    /// The horizontal a walk-off carries while falling `h`
    public static float FallCarry(LevelMetricsMath.Values v, float speed, float h) => speed * LevelMetricsMath.FallTime(v, h, false);
    public static float Sin(float deg) => Mathf.Sin(deg * Mathf.Deg2Rad);
    public static float Cos(float deg) => Mathf.Cos(deg * Mathf.Deg2Rad);
    public static string Roman(int d) => d switch { 1 => "I", 2 => "II", 3 => "III", _ => d.ToString() };
}
