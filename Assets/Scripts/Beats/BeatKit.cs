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
    public const float Reveal = 0.3f;
    /// How tall a run wall (a facade) and a portal (a gate) stand: by default the heights the metrics call safe (a
    /// lift's reach over a jump's apex, and half a metre; a gate stands taller by three metres of landing drop), so
    /// nothing a wall run can lift the body onto has a top. A level may ask for more as a look: the gym stands 14 m
    public float facadeHeight, gateHeight;

    public Frame(Transform root, LevelMetricsMath.Values v, Pose at)
    {
        this.root = root; this.v = v; origin = at.pos; yaw = at.yaw;
        lowest = highest = at.pos.y;
        built = new Bounds(at.pos, Vector3.one);
        facadeHeight = LevelMetricsMath.SafeWallHeight(v); gateHeight = LevelMetricsMath.SafeGateHeight(v);
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
    public Frame Sub(float x, float y, float z, float deg) => new Frame(root, v, new Pose(P(x, y, z), yaw + deg, 0f)) { zone = zone, lowest = lowest, highest = highest, built = built, groundY = groundY, facadeHeight = facadeHeight, gateHeight = gateHeight };
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
    public GameObject Wall(float x, float z0, float z1, float yBottom, float height, string name = "KickWall", float dress0 = float.NaN, float dress1 = float.NaN)
    {
        float h = Structural ? Mathf.Max(height, facadeHeight) : height;
        var band = Box(P(x, yBottom + h * 0.5f, (z0 + z1) * 0.5f), new Vector3(1f, h, z1 - z0), LevelKit.Wall, name, Rot);
        Body(x - 0.5f, x + 0.5f, yBottom - 0.05f, z0, z1, "FacadeBody");
        DressFacade(x, float.IsNaN(dress0) ? z0 : dress0, float.IsNaN(dress1) ? z1 : dress1, yBottom, h);   // the span to dress: a bend passes its facet without the overlaps
        return band;
    }

    /// THE DRESSING of a run wall (DecoKit, 2026-11-05; the modeller's brief): fluted panels 2 × 4 m over the face toward the
    /// line, sunk so their ribs stand only 5 cm off the collider the player runs on (the box's face fills the grooves), as
    /// many whole panels as fit, centred; the top row a window band when there is more than one; a 2 m frieze row over them
    /// when two metres are left; a cornice along the top edge, proud. Nothing is placed while the pieces are not in Resources/Models
    void DressFacade(float x, float z0, float z1, float yBottom, float h)
    {
        if (!DecoKit.Enabled) return;
        float side = x > 0f ? -1f : 1f;              // the face toward the line (x = 0)
        float faceX = x + side * 0.5f;               // the band is a metre thick, centred on x
        Quaternion rot = Quaternion.LookRotation(R * side, Vector3.up);
        float len = z1 - z0;
        int cols = Mathf.FloorToInt(len / 2f + 0.001f), rows = Mathf.FloorToInt(h / 4f + 0.001f);
        float zStart = z0 + (len - cols * 2f) * 0.5f;
        float panelX = faceX - side * (DecoKit.FacadeRelief - DecoKit.Proud);
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                DecoKit.Place(root, r == rows - 1 && rows > 1 ? "facade_window_2x4" : "facade_flute_2x4", P(panelX, yBottom + r * 4f, zStart + c * 2f + 1f), rot, new Vector3(2f, 4f, DecoKit.FacadeRelief));
        if (h - rows * 4f >= 1.9f)   // THE FRIEZE: walls are 6 m (real levels) or 14 m (the gym), panels are 4 m: the two metres left under the cornice take a frieze row, or the bare box shows
            for (int c = 0; c < cols; c++)
                DecoKit.Place(root, "facade_frieze_2x2", P(panelX, yBottom + rows * 4f, zStart + c * 2f + 1f), rot, new Vector3(2f, 2f, DecoKit.FacadeRelief));
        int n = Mathf.FloorToInt(len + 0.001f);
        float cStart = z0 + (len - n) * 0.5f;
        for (int i = 0; i < n; i++)
            DecoKit.Place(root, "cornice_1m", P(faceX + side * DecoKit.Proud, yBottom + h - 0.4f, cStart + i + 0.5f), rot, new Vector3(1f, 0.4f, 0.5f));
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
        if (Structural) top = Mathf.Max(top, gateHeight);   // a gate: nothing an arc or a lift passes over
        float foot = Structural ? groundY : y - 1f;
        float x0 = x - halfW - (leftPost ? 0.7f : 0.1f), x1 = x + halfW + (rightPost ? 0.7f : 0.1f);
        Mass(leftPost ? x - halfW - 0.1f : x0, rightPost ? x + halfW + 0.1f : x1, y + clear, y + top, z - d, z + d, "Lintel");
        if (leftPost) Mass(x0, x - halfW - 0.1f, foot, y + top, z - d, z + d, "Post");
        if (rightPost) Mass(x + halfW + 0.1f, x1, foot, y + top, z - d, z + d, "Post");
        DressGate(z, y, x - halfW - 0.1f, x + halfW + 0.1f, clear, top, leftPost, rightPost);
    }

    /// THE DRESSING of a gate (DecoKit): a shaft tile per metre up each post from a metre under the floor (the body below
    /// stays the dark building), a moulded lintel per metre over the opening hung 5 cm under the mass's underside, and the
    /// transom over the middle of it on both faces
    void DressGate(float z, float y, float openL, float openR, float clear, float top, bool leftPost, bool rightPost)
    {
        if (!DecoKit.Enabled) return;
        int tiles = Mathf.Min(Mathf.FloorToInt(top + 1f), 16);
        for (int i = 0; i < tiles; i++)
        {
            if (leftPost) DecoKit.Place(root, "gate_post_shaft", P(openL - 0.3f, y - 1f + i, z), Rot, new Vector3(0.7f, 1f, 1f), null, true);
            if (rightPost) DecoKit.Place(root, "gate_post_shaft", P(openR + 0.3f, y - 1f + i, z), Rot, new Vector3(0.7f, 1f, 1f), null, true);
        }
        int n = Mathf.FloorToInt(openR - openL + 0.001f);
        float s = openL + (openR - openL - n) * 0.5f;
        for (int i = 0; i < n; i++) DecoKit.Place(root, "gate_lintel_1m", P(s + i + 0.5f, y + clear - DecoKit.Proud, z), Rot, new Vector3(1f, 1f, 1f), null, true);
        if (top - clear < 2f) return;
        for (int face = -1; face <= 1; face += 2)
            DecoKit.Place(root, "gate_transom", P((openL + openR) * 0.5f, y + clear + 1f - DecoKit.Proud, z + face * (0.45f + DecoKit.Proud)), Quaternion.LookRotation(F * face, Vector3.up), new Vector3(2f, 1f, 0.1f));
    }

    /// A BAR across the line at `z` (2026-11-10; the user, having played the gates: "they kill the flow", so a low slot
    /// stands only where the slide IS the move): a beam a metre deep whose underside is `clear` over the floor at `y`, a
    /// post under each end down to the ground. Only a slide passes under; nothing stands above it, so it reads as a beam
    /// to duck, and a second jump clears it. Named like a portal's pieces: the validator reads the beam as a ceiling
    public void Bar(float z, float y, float halfW, float clear = 1.25f, bool leftPost = true, bool rightPost = true, float x = 0f)
    {
        const float d = 0.45f;
        float foot = Structural ? groundY : y - 1f;
        float x0 = x - halfW - (leftPost ? 0.7f : 0.1f), x1 = x + halfW + (rightPost ? 0.7f : 0.1f);
        Mass(leftPost ? x - halfW - 0.1f : x0, rightPost ? x + halfW + 0.1f : x1, y + clear, y + clear + 1f, z - d, z + d, "Lintel");
        if (leftPost) Mass(x0, x - halfW - 0.1f, foot, y + clear + 1f, z - d, z + d, "Post");
        if (rightPost) Mass(x + halfW + 0.1f, x1, foot, y + clear + 1f, z - d, z + d, "Post");
    }

    /// A RAIL to grind (STEP 3, 2026-11-10): a brass rail whose top runs from (z0, y0) to (z1, y1) at `x`, a lit line
    /// along it, on posts to the ground. What is stood on is a lane `catchHalf` each side of the rail with no picture:
    /// the rail pulls the feet onto its line once they land, so the lane is the magnet's reach and a rail is a big
    /// landing, never a precision one. Marked for the motor (MoveSurface) and named for the validator like a beam: a
    /// narrow way that is meant
    public GameObject Rail(float x, float z0, float y0, float z1, float y1, float catchHalf = 1.5f)
    {
        float dz = z1 - z0, dy = y1 - y0, len = Mathf.Sqrt(dz * dz + dy * dy);
        var rot = Rot * Quaternion.Euler(Mathf.Atan2(-dy, dz) * Mathf.Rad2Deg, 0f, 0f);
        Vector3 topMid = P(x, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f), up = rot * Vector3.up;
        var lane = Box(topMid - up * 0.15f, new Vector3(catchHalf * 2f, 0.3f, len), LevelKit.Dark, "ControlRail", rot);
        lane.GetComponent<MeshRenderer>().enabled = false;   // (the welder leaves a renderer that is off alone)
        MoveSurface.Mark(lane, MoveSurface.Kind.Grind);
        LevelKit.Prim(root, topMid - up * 0.07f, new Vector3(0.14f, 0.14f, len), LevelKit.Brass, "Rail", PrimitiveType.Cube, rot);
        LevelKit.LineRun(root, P(x, y0, z0), P(x, y1, z1), zone, 0.05f);
        int posts = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(dz) / 7f) + 1);
        for (int i = 0; i < posts; i++)
        {
            float t = (float)i / (posts - 1), inset = 0.3f / Mathf.Max(0.6f, Mathf.Abs(dz));
            t = Mathf.Lerp(inset, 1f - inset, t);
            Post(x, Mathf.Lerp(z0, z1, t), Mathf.Lerp(y0, y1, t) - 0.1f, 0.3f);
        }
        return lane;
    }

    /// A HOP STONE: a floor `wide` across and `deep` along the line, centred at (x, z), its top at y. Its depth is the
    /// landing's tolerance: the arc the stone is spaced for lands sixty percent of the way across it
    public GameObject Stone(float x, float z, float y, float wide, float deep, string name = "ControlStone")
    {
        var stone = Deck(z - deep * 0.5f, z + deep * 0.5f, y, wide * 0.5f, name, x);
        DecoKit.Place(root, "capital_3m", P(x, y - 1.95f, z), Rot, new Vector3(3f, 1f, 3f), new Vector3(wide / 3f * 1.03f, 1f, deep / 3f * 1.03f), true);   // the capital UNDER the stone, round the top metre of its pillar: its top 5 cm up into the slab (the slab is a metre thick), its sides a few centimetres proud
        return stone;
    }

    /// A LAMP STANDARD (DecoKit) just outside a deck's edge: the piece on a post of its own down to the ground, its base
    /// 30 cm under the deck so the post's top is a drop and never a step. Dressing only: no collider of its own
    public void LampPost(float x, float y, float z)
    {
        DecoKit.Place(root, "lamp_4m", P(x, y - 0.3f, z), Rot, new Vector3(0.25f, 4f, 0.25f), null, true);
        Post(x, z, y - 0.3f, 0.4f);
    }

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
    /// The speed a rail of `len` metres falling `deg` hands on from `from` (STEP 3: the rail's own gain and gravity's share of the fall; the rail's least speed if less was brought)
    public static float RailGain(LevelMetricsMath.Values v, float from, float deg, float len) { float s = Mathf.Max(from, v.grindMinSpeed); return Mathf.Sqrt(s * s + 2f * (v.grindAccel + v.gravity * Sin(deg)) * len); }
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
