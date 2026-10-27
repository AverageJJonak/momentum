using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A BLOCK'S FRAME: every establishment is authored once in its own coordinates — x across the front, z from the
/// front (−z, the street it faces) to the back (+z), y up from the street — and the frame turns it onto its block
/// (rotation 0..3 = the front faces south, west, north, east). Boxes stay axis-aligned under quarter turns, so solids
/// are LevelKit slabs with swapped extents, rooms are LevelKit rooms (Articulate runs from them), ramps are slabs
/// turned by the frame's yaw. Entrances and lines are recorded in the world through the same frame.
/// </summary>
public struct BlockFrame
{
    /// The block's centre at street level
    public Vector3 origin;
    public int rot;
    /// The interior half extents (inside the walls) and the block's own
    public float hw, hd, blockHW, blockHD;
    public Quaternion Q => Quaternion.Euler(0f, rot * 90f, 0f);
    public float Yaw => rot * 90f;
    public Vector3 P(float x, float y, float z) => origin + Q * new Vector3(x, 0f, z) + Vector3.up * y;
    public Vector3 P(Vector3 l) => P(l.x, l.y, l.z);
    public Vector3 S(Vector3 s) => (rot & 1) == 0 ? s : new Vector3(s.z, s.y, s.x);
    public Vector3 S(float x, float y, float z) => S(new Vector3(x, y, z));
    public Vector3 D(Vector3 d) => Q * d;
    /// A local face in the world
    public Face F(Face local)
    {
        Vector3 n = D(Normal(local));
        return Mathf.Abs(n.x) > Mathf.Abs(n.z) ? (n.x > 0f ? Face.East : Face.West) : (n.z > 0f ? Face.North : Face.South);
    }
    public static Vector3 Normal(Face f) => f == Face.North ? Vector3.forward : f == Face.South ? Vector3.back : f == Face.East ? Vector3.right : Vector3.left;
    public LevelKit.Faces F(LevelKit.Faces local)
    {
        LevelKit.Faces w = local & (LevelKit.Faces.Floor | LevelKit.Faces.Ceiling);
        if ((local & LevelKit.Faces.North) != 0) w |= Flag(F(Face.North));
        if ((local & LevelKit.Faces.South) != 0) w |= Flag(F(Face.South));
        if ((local & LevelKit.Faces.East) != 0) w |= Flag(F(Face.East));
        if ((local & LevelKit.Faces.West) != 0) w |= Flag(F(Face.West));
        return w;
    }
    public static LevelKit.Faces Flag(Face f) => f == Face.North ? LevelKit.Faces.North : f == Face.South ? LevelKit.Faces.South : f == Face.East ? LevelKit.Faces.East : LevelKit.Faces.West;
    /// A hole in a local face of a box whose interior spans `min`..`max` (local), in the world
    public Hole H(Face local, float u0, float u1, float v0, float v1, Vector3 min, Vector3 max)
    {
        bool alongX = local == Face.North || local == Face.South;
        float plane = local == Face.North ? max.z : local == Face.South ? min.z : local == Face.East ? max.x : min.x;
        Vector3 a = alongX ? P(u0, 0f, plane) : P(plane, 0f, u0), b = alongX ? P(u1, 0f, plane) : P(plane, 0f, u1);
        Face wf = F(local);
        bool wAlongX = wf == Face.North || wf == Face.South;
        float wa = wAlongX ? a.x : a.z, wb = wAlongX ? b.x : b.z;
        return new Hole(wf, Mathf.Min(wa, wb), Mathf.Max(wa, wb), origin.y + v0, origin.y + v1);
    }
}

/// <summary>
/// THE ESTABLISHMENTS (district-grammar): the buildings a district's blocks hold, each a pass-through with two to four
/// entrances at different heights and interior LINES (a main line at eight metres a second, fast lines that take
/// earned momentum) with lips, flutes, kick walls and orbs, fuel on the lines, Deco dressing; no gate, a miss never
/// kills. This file: the pieces every establishment is drawn with, through the block's frame.
/// </summary>
public static partial class Establishments
{
    const float T = LevelKit.T;
    static System.Random rng = new System.Random(1);
    static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

    // ------------------------------------------------------------ solids and looks through the frame

    static GameObject Box(Transform root, BlockFrame f, Vector3 lc, Vector3 ls, Material m, string name) => LevelKit.Slab(root, f.P(lc), f.S(ls), m, name);
    static GameObject Floor(Transform root, BlockFrame f, Vector3 lc, Vector3 ls, string name = "Floor") => LevelKit.FloorSlab(root, f.P(lc), f.S(ls), name);
    static GameObject Lip(Transform root, BlockFrame f, Vector3 lc, Vector3 ls, Material m = null, string name = "LipLedge") => LevelKit.Lip(root, f.P(lc), f.S(ls), m, name);
    static GameObject Kick(Transform root, BlockFrame f, Vector3 lc, Vector3 ls, Material m = null) => LevelKit.KickWall(root, f.P(lc), f.S(ls), m);
    static GameObject Corner(Transform root, BlockFrame f, Vector3 lc, Vector3 ls, Material m = null) => LevelKit.BoostCorner(root, f.P(lc), f.S(ls), m);
    static GameObject Mark(Transform root, BlockFrame f, Vector3 lc, Vector3 ls, Material m, string name, PrimitiveType type = PrimitiveType.Cube) => LevelKit.Prim(root, f.P(lc), f.S(ls), m, name, type);
    static void Orb(Transform root, BlockFrame f, Vector3 lc) => LevelKit.Orb(root, f.P(lc));
    static void Pad(Transform root, BlockFrame f, Vector3 lc, float up, float forward) => LevelKit.JumpPad(root, f.P(lc), f.Yaw, up, forward);
    static void Lamp(Transform root, BlockFrame f, Vector3 lc, float ceilY) => LevelKit.GlobeLamp(root, f.P(lc), f.origin.y + ceilY);
    static void Flute(Transform root, BlockFrame f, Vector3 lAt, Vector3 lFacing, float height, float width = 0.6f) => LevelKit.Flute(root, f.P(lAt), f.D(lFacing), height, width);

    /// A RAMP whose top runs from (lz0, y0) to (lz1, y1) at local x, turned with the frame: stairs (nosed in brass
    /// when `nosed`), slides (`mat` = LevelKit.Slope), banisters (brass). A stringer under each edge
    static GameObject Ramp(Transform root, BlockFrame f, float lx, float width, float lz0, float y0, float lz1, float y1, Material mat = null, bool nosed = false)
    {
        const float RampT = 0.35f;
        float dz = lz1 - lz0, dy = y1 - y0, len = Mathf.Sqrt(dz * dz + dy * dy);
        Quaternion rot = f.Q * Quaternion.Euler(Mathf.Atan2(-dy, dz) * Mathf.Rad2Deg, 0f, 0f);
        Vector3 topMid = f.P(lx, (y0 + y1) * 0.5f, (lz0 + lz1) * 0.5f);
        var go = LevelKit.Slab(root, topMid - rot * Vector3.up * (RampT * 0.5f), new Vector3(width, RampT, len), mat ?? LevelKit.Slope, "Ramp");
        go.transform.rotation = rot;
        for (int s = -1; s <= 1; s += 2)
            LevelKit.Prim(root, topMid + rot * (Vector3.right * (s * (width * 0.5f - 0.2f)) - Vector3.up * (RampT + 0.14f)), new Vector3(0.26f, 0.26f, len - 0.3f), LevelKit.Bronze, "RampStringer", PrimitiveType.Cube, rot);
        if (nosed)
        {
            int n = Mathf.FloorToInt(len / 1.6f);
            for (int i = 1; i < n; i++)
                LevelKit.Prim(root, Vector3.Lerp(f.P(lx, y0, lz0), f.P(lx, y1, lz1), i / (float)n) + rot * Vector3.up * 0.008f, new Vector3(width - 0.2f, 0.008f, 0.09f), LevelKit.Brass, "Nosing", PrimitiveType.Cube, rot);
        }
        return go;
    }

    /// A BANISTER beside a flight: a brass ramp 1.4 m wide a metre over the flight's edge (a slide down, a lip up)
    static void Banister(Transform root, BlockFrame f, float lx, float lz0, float y0, float lz1, float y1)
    {
        var b = Ramp(root, f, lx, 1.4f, lz0, y0 + 1f, lz1, y1 + 1f, LevelKit.Brass);
        b.name = "BanisterRamp";
    }

    /// A ROOM in the frame: interior `min`..`max` (local), the faces asked for, holes in local faces. LevelKit.Room
    /// builds it axis-aligned in the world and Articulate dresses it by zone
    static void Room(Transform root, BlockFrame f, Vector3 min, Vector3 max, LevelKit.Faces faces, params Hole[] holes)
    {
        Vector3 a = f.P(min), b = f.P(max);
        LevelKit.Room(root, Vector3.Min(a, b), Vector3.Max(a, b), f.F(faces), holes);
    }

    /// A DOOR hole in a local face at `u` along it on floor `y`: 6.5 m wide, 4.25 tall (the lane rule)
    static Hole Door(BlockFrame f, Face local, float u, float y, Vector3 min, Vector3 max, float width = 6.5f, float height = 4.25f) => f.H(local, u - width * 0.5f, u + width * 0.5f, y, y + height, min, max);
    /// A WINDOW hole: `u` along the face, from `y0` to `y1`
    static Hole Window(BlockFrame f, Face local, float u, float width, float y0, float y1, Vector3 min, Vector3 max) => f.H(local, u - width * 0.5f, u + width * 0.5f, y0, y1, min, max);

    // ------------------------------------------------------------ the record

    /// An ENTRANCE: at local `lAt` (on the threshold, at its floor), facing out along `lFacing`, `height` over the
    /// street; `tech` when getting to it from the street takes earned momentum
    static CityDistrict.Entrance Entrance(CityDistrict.DistrictPlan plan, CityDistrict.Block block, BlockFrame f, string name, Vector3 lAt, Vector3 lFacing, CityDistrict.EntranceKind kind, bool tech = false, bool fromStreet = true)
    {
        Vector3 facing = f.D(lFacing);
        var e = new CityDistrict.Entrance
        {
            block = block, name = name, at = f.P(lAt), facing = facing, height = lAt.y, kind = kind, tech = tech, fromStreet = fromStreet,
            side = Mathf.Abs(facing.x) > Mathf.Abs(facing.z) ? (facing.x > 0f ? Face.East : Face.West) : (facing.z > 0f ? Face.North : Face.South),
        };
        block.entrances.Add(e);
        plan.entrances.Add(e);
        return e;
    }

    /// A LINE: the lit hairline on the floor from `a` to `b` (local), recorded on the block
    static void Line(Transform root, CityDistrict.Block block, BlockFrame f, string name, Vector3 a, Vector3 b, Zone zone, bool fast = true)
    {
        Vector3 wa = f.P(a), wb = f.P(b);
        LevelKit.LineRun(root, wa, wb, zone);
        block.lines.Add(new CityDistrict.Line { name = name, a = wa, b = wb, fast = fast });
    }

    static void Fuel(Transform root, MovementConfig config, BlockFrame f, EnemyKind kind, Vector3 lAt, string line)
        => CityDistrict.Fuel(root, config, kind, f.P(lAt), line);

    /// Something from the level's roster, on a line
    static void PoolFuel(MovementConfig config, BlockFrame f, Vector3 lAt, string line, bool air = false)
    {
        var e = air ? EnemyPool.Air(f.P(lAt), config) : EnemyPool.Ground(f.P(lAt), config);
        if (e != null) LevelSections.Last?.fuel.Add(new LevelSections.FuelMark { unit = e, line = line });
    }

    /// The frame for a block: its interior a wall's half thickness inside the block's bounds
    static BlockFrame FrameFor(CityDistrict.Block b)
    {
        bool even = (b.rotation & 1) == 0;   // a quarter turn swaps the block's extents in the frame
        float bhw = even ? b.bounds.extents.x : b.bounds.extents.z, bhd = even ? b.bounds.extents.z : b.bounds.extents.x;
        return new BlockFrame { origin = b.Centre, rot = b.rotation, blockHW = bhw, blockHD = bhd, hw = bhw - T * 0.5f, hd = bhd - T * 0.5f };
    }
}
