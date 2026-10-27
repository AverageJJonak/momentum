using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The level grammar. A level is a chain of SECTIONS along +Z, each built at a moving Cursor and handing the cursor
/// on. Since 2026-10-09 (course-grammar) a level is ONE CONTINUOUS COURSE: the spaces in `Course` (LevelSectionsCourse*)
/// build no exit wall and no door — thresholds are arches and openings, checkpoints are invisible line crossings
/// (CourseFlow), units are fuel on the lines, not gates. The sections in this file and its old partials are the
/// room grammar that came before: they still build their north wall with a door in it, but nothing gates any more
/// (RoomLoop is gone), so an old chain runs as a course with doorways in it until its level is rebuilt.
/// Every section seals only the part of its entry plane the previous section's wall (or open lane) does not already
/// cover, so seams are single walls with no gaps and no double slabs. A high glass in an exit wall is remembered so
/// the next section lands it on a balcony. Company ads, signs and stencils come with each section by zone.
/// </summary>
public static partial class LevelSections
{
    public struct Cursor
    {
        public float z, floorY;
        public float wallX0, wallX1, wallY0, wallY1; // the wall the previous section built on plane z (or, open, the extents of its lane)
        public float glassY;                          // > 0: a high glass sits in that wall at this height
        public bool hasWall;
        public Zone zone;
        public Zone prevZone; // the zone of the section that built the wall behind you
        /// The lane behind you: its half width and the headroom over its exit floor (course-grammar)
        public float halfW, height;
        /// The section behind built NO exit wall (a course space): its lane runs straight into this one
        public bool open;
    }

    /// Where the bootstrap puts the doors and the menu shoots the postcard
    public class Layout
    {
        public Vector3 spawn, menuPortal, finishPortal;
        public Vector3 postcardPos, postcardLookAt;
        public float postcardFov = 72f;
        public bool hasPostcard, postcardSignature;
        /// A section that must be the start (the ward) locks the spawn: rooms after it leave it alone
        public bool spawnLocked;
        public float length;
        /// Every section, in order: its span (the zone of every point, the lint's lanes, the segments when no checkpoint is named)
        public readonly List<RoomSpan> rooms = new List<RoomSpan>();
        /// Every window and open sky, so nothing is built where nobody can look
        public readonly List<Aperture> apertures = new List<Aperture>();
        /// The course's named checkpoints, in order (Course.Checkpoint): where a respawn puts you, moving
        public readonly List<CheckpointMark> checkpoints = new List<CheckpointMark>();
        /// Every unit a space put on a named line (Course.Fuel)
        public readonly List<FuelMark> fuel = new List<FuelMark>();
    }
    public static Layout Last;
    /// A rack with cases stands in this scene (Rack): the bootstrap arms nothing until the run has chosen
    public static bool RackPlanned;
    /// The seed Begin was given: every course space derives its own from it and its place on the chain
    public static int Seed { get; private set; } = 1;

    /// One section's span: its planes, its floors at each end, its width, and a high glass in its exit if any
    public struct RoomSpan { public float z0, z1, entryY, exitY, halfW, glassY; public Zone zone; }

    /// A checkpoint: a VOLUME you enter (district-grammar, 2026-10-12: `bounds`, a box in the world; CourseFlow reads
    /// boxes), where a respawn stands you up (`at`) and the way it sends you (`forward`: the authored route's direction
    /// there, +z on a chain). A mark made by z alone (Course.Checkpoint on the old chains) is a thin slab across the
    /// lane at `z`: `volume` is false and the flow orders it by z as it always did
    public struct CheckpointMark
    {
        public string name; public float z; public Vector3 at;
        public Bounds bounds; public Vector3 forward; public bool volume;
        /// The slab across a lane: `halfW` each side of x = 0 at `z`, thirty metres tall over the floor
        public static CheckpointMark Line(string name, float z, Vector3 at, float halfW)
            => new CheckpointMark { name = name, z = z, at = at, forward = Vector3.forward, volume = false,
                bounds = new Bounds(new Vector3(0f, at.y + 12f, z), new Vector3(halfW * 2f + 4f, 30f, 1.2f)) };
        /// A box in the world (a junction, a plaza, a lobby): entering it makes it the respawn
        public static CheckpointMark Volume(string name, Bounds box, Vector3 at, Vector3 forward)
            => new CheckpointMark { name = name, z = box.center.z, at = at, forward = forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward, volume = true, bounds = box };
    }

    /// A unit of fuel and the line it stands on
    public struct FuelMark { public Enemy unit; public string line; }

    /// A stretch of one side the player can SEE OUT OF — a ribbon window, or a room with no ceiling.
    /// side −1 = the west wall, +1 = the east wall. The city is raised here and nowhere else.
    public struct Aperture { public int side; public float z0, z1, y0, y1; }

    const float T = LevelKit.T;
    const LevelKit.Faces NoSouth = LevelKit.Faces.North | LevelKit.Faces.East | LevelKit.Faces.West
                                   | LevelKit.Faces.Floor | LevelKit.Faces.Ceiling;
    static readonly Vector3 PortalSize = new Vector3(3.2f, 4f, 1.2f);

    static System.Random rng = new System.Random(1);
    static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

    // ------------------------------------------------------------ the chain

    /// Start a level: seeded so the same level builds the same way every run
    public static Cursor Begin(int seed, float floorY = 0f)
    {
        rng = new System.Random(seed);
        Seed = seed;
        Last = new Layout();
        RackPlanned = false;
        return new Cursor { z = 0f, floorY = floorY, zone = Zone.Utopia };
    }

    public static Cursor Under(Cursor c) { c.zone = Zone.Underbelly; return c; }
    public static Cursor Utopia(Cursor c) { c.zone = Zone.Utopia; return c; }
    public static Cursor Surface(Cursor c) { c.zone = Zone.Surface; return c; }   // the roofs (2026-10-27: Act III's courses)
    public static Cursor Brutal(Cursor c) { c.zone = Zone.Brutal; return c; }   // 2026-10-27: raw concrete, the courses

    /// The exit wall this section built: the next section seals around it. A course space calls this with the
    /// extents of its lane instead (it built no wall) and marks the cursor open (Course.Open)
    internal static Cursor Exit(Cursor c, float z1, float floorY, float halfW, float wallY0, float wallY1, float glassY = 0f)
    {
        // Every section ends here, so every section is a span the layout knows about
        if (Last != null && z1 > c.z)
            Last.rooms.Add(new RoomSpan { z0 = c.z, z1 = z1, entryY = c.floorY, exitY = floorY, halfW = halfW, glassY = glassY, zone = c.zone });
        return new Cursor
        {
            z = z1, floorY = floorY, wallX0 = -halfW - T * 0.5f, wallX1 = halfW + T * 0.5f,
            wallY0 = wallY0, wallY1 = wallY1, glassY = glassY, hasWall = true, zone = c.zone, prevZone = c.zone,
            halfW = halfW, height = wallY1 - floorY, open = false,
        };
    }

    /// Walls off whatever part of this section's entry plane the previous
    /// section's exit wall does not already cover (wider, taller or lower rooms).
    /// Behind an OPEN space (no exit wall, only the extents of its lane) it also walls the part of THAT lane's end
    /// that lies outside this one — beside a narrower lane, over a lower ceiling, under a higher floor — so the
    /// outside stays out. Never across the lane itself: the way on is always open.
    internal static void Seal(Transform root, Cursor c, float halfW, float floorY, float height)
    {
        float x0 = -halfW - T * 0.5f, x1 = halfW + T * 0.5f, y0 = floorY, y1 = floorY + height;
        if (!c.hasWall)
        {
            LevelKit.WallFace(root, Face.South, c.z, x0, x1, y0, y1);
            return;
        }
        if (c.wallX0 > x0) LevelKit.WallFace(root, Face.South, c.z, x0, Mathf.Min(c.wallX0, x1), y0, y1);
        if (c.wallX1 < x1) LevelKit.WallFace(root, Face.South, c.z, Mathf.Max(c.wallX1, x0), x1, y0, y1);
        float mx0 = Mathf.Max(x0, c.wallX0), mx1 = Mathf.Min(x1, c.wallX1);
        if (mx1 > mx0)
        {
            if (c.wallY1 < y1) LevelKit.WallFace(root, Face.South, c.z, mx0, mx1, Mathf.Max(c.wallY1, y0), y1);
            if (c.wallY0 > y0) LevelKit.WallFace(root, Face.South, c.z, mx0, mx1, y0, Mathf.Min(c.wallY0, y1));
        }
        if (!c.open) return;
        float oy0 = Mathf.Min(c.wallY0, y0), oy1 = Mathf.Max(c.wallY1, y1);
        if (c.wallX0 < x0) LevelKit.WallFace(root, Face.South, c.z, c.wallX0, x0, oy0, oy1);   // beside a narrower lane
        if (c.wallX1 > x1) LevelKit.WallFace(root, Face.South, c.z, x1, c.wallX1, oy0, oy1);
        if (mx1 > mx0)
        {
            if (c.wallY1 > y1) LevelKit.WallFace(root, Face.South, c.z, mx0, mx1, y1, c.wallY1);   // over a lower ceiling
            if (c.wallY0 < y0) LevelKit.WallFace(root, Face.South, c.z, mx0, mx1, c.wallY0, y0);   // under a higher floor
        }
    }

    /// THE CITY OUTSIDE. Utopia's rooms get a ribbon window down each side wall, set high so nothing the wall
    /// carries below it is in the way: the cut is made with the room (With), the glass and its frame right after
    /// (Windows). Anything under the city, or in the company's house, keeps its walls
    internal static Hole[] WindowHoles(Cursor c, float halfW, float y, float height, float z0, float z1)
    {
        if (c.zone != Zone.Utopia || height < 9f || z1 - z0 < 16f) return new Hole[0];
        float v0 = y + Mathf.Max(5f, height * 0.42f), v1 = y + height - 1.8f;
        See(-1, z0 + 3f, z1 - 3f, v0, v1);
        See(1, z0 + 3f, z1 - 3f, v0, v1);
        return new[] { new Hole(Face.West, z0 + 3f, z1 - 3f, v0, v1), new Hole(Face.East, z0 + 3f, z1 - 3f, v0, v1) };
    }

    /// This room can see out, here: the city outside is raised to match
    internal static void See(int side, float z0, float z1, float y0, float y1)
    {
        if (Last == null || z1 <= z0) return;
        Last.apertures.Add(new Aperture { side = side, z0 = z0, z1 = z1, y0 = y0, y1 = y1 });
    }

    internal static Hole[] With(Hole[] windows, params Hole[] more)
    {
        var all = new Hole[windows.Length + more.Length];
        windows.CopyTo(all, 0);
        more.CopyTo(all, windows.Length);
        return all;
    }

    internal static void Windows(Transform root, Hole[] windows, float halfW)
    {
        foreach (var h in windows) LevelKit.Window(root, h, h.face == Face.West ? -halfW : halfW);
    }

    /// A high glass in the wall behind lands on this
    internal static void LandGlass(Transform root, Cursor c)
    {
        if (c.glassY <= 0f) return;
        LevelKit.FloorSlab(root, new Vector3(0f, c.glassY - 0.5f, c.z + 5f), new Vector3(8f, 1f, 10f), "GlassBalcony");
    }

    internal static Hole Door(float floorY, float x = 0f) => new Hole(Face.North, x - 3.25f, x + 3.25f, floorY, floorY + 4.25f);
    internal static Hole HighGlass(float glassY) => new Hole(Face.North, -3f, 3f, glassY, glassY + 4f);

    /// The postcard: the first room to claim it keeps it — unless a later room is the level's SIGNATURE, the one it is
    /// named for, which takes it over any plain claim (levels that share an opening room still get their own picture)
    internal static void Postcard(Vector3 pos, Vector3 look, float fov = 72f, bool signature = false)
    {
        if (Last == null) return;
        if (Last.hasPostcard && (Last.postcardSignature || !signature)) return;
        Last.postcardPos = pos;
        Last.postcardLookAt = look;
        Last.postcardFov = fov;
        Last.hasPostcard = true;
        Last.postcardSignature = signature;
    }

    // ------------------------------------------------------------ enemies

    static Vector3 Spot(float x0, float x1, float z0, float z1, float y, List<Vector3> avoid)
    {
        for (int tries = 0; tries < 12; tries++)
        {
            var p = new Vector3(R(x0, x1), y, R(z0, z1));
            bool ok = true;
            if (avoid != null)
                foreach (var a in avoid)
                    if (Mathf.Abs(a.x - p.x) < 3.5f && Mathf.Abs(a.z - p.z) < 3.5f) { ok = false; break; }
            if (ok) return p;
        }
        return new Vector3((x0 + x1) * 0.5f, y, (z0 + z1) * 0.5f);
    }

    internal static void Squad(MovementConfig config, float x0, float x1, float z0, float z1, float floorY, float turretY,
        int chasers, int turrets, int dummies, List<Vector3> avoid = null)
    {
        for (int i = 0; i < chasers; i++) EnemyPool.Ground(Spot(x0, x1, z0, z1, floorY, avoid), config);
        for (int i = 0; i < turrets; i++)
        {
            var p = Spot(x0, x1, z0, z1, floorY, avoid);
            EnemyPool.Air(new Vector3(p.x, turretY, p.z), config);
        }
        for (int i = 0; i < dummies; i++) Enemy.CreateDummy(Spot(x0, x1, z0, z1, floorY, avoid), config);
    }

    // ------------------------------------------------------------ dressing

    /// Ads up top, the truth down below: every section gets the company on its walls
    internal static void Dress(Transform root, Cursor c, float halfW, float floorY, float height, float z0, float z1, bool openSky = false)
    {
        // No ceiling: the sky, and everything in it, is visible over the walls on both sides
        if (openSky) { See(-1, z0, z1, floorY + height, floorY + height + 220f); See(1, z0, z1, floorY + height, floorY + height + 220f); }
        float len = z1 - z0;
        Detail(root, c, halfW, floorY, height, z0, z1, openSky);
        LevelKit.Air(root, c.zone, new Vector3(-halfW, floorY, z0), new Vector3(halfW, floorY + height, z1), openSky); // what hangs in the room
        if (c.zone == Zone.Works)
        {
            DressWorks(root, c, halfW, floorY, height, z0, z1, openSky);
            return;
        }
        if (c.zone == Zone.Office)
        {
            DressOffice(root, c, halfW, floorY, height, z0, z1, openSky);
            return;
        }
        if (c.zone == Zone.Utopia)
        {
            LevelKit.Billboard(root, new Vector3(-halfW + 0.1f, floorY + height * 0.6f, z0 + len * R(0.25f, 0.45f)), Mathf.Min(12f, len * 0.16f), Mathf.Min(5f, height * 0.3f), 90f);
            LevelKit.Billboard(root, new Vector3(halfW - 0.1f, floorY + height * 0.6f, z0 + len * R(0.55f, 0.8f)), Mathf.Min(10f, len * 0.14f), Mathf.Min(4f, height * 0.26f), 90f);
            int strips = openSky ? 0 : Mathf.Max(1, Mathf.RoundToInt(len / 30f));
            for (int i = 0; i < strips; i++)
                LevelKit.LightStrip(root, new Vector3(0f, floorY + height - 0.1f, z0 + len * (i + 0.5f) / strips), Mathf.Min(14f, halfW * 1.2f), true);
            LevelKit.CameraDome(root, new Vector3(halfW - 0.4f, floorY + height - 1f, z0 + len * 0.2f), new Vector3(-1f, -0.6f, 0.4f));
            if (c.prevZone == Zone.Underbelly && c.hasWall)
            {
                // The rot follows you up: vines breach the doorway and climb the clean wall
                LevelKit.Tendril(root, new Vector3(3.6f, floorY, z0 + 0.6f), new Vector3(halfW * 0.5f, floorY + height * 0.55f, z0 + len * 0.12f), 0.45f, 9);
                LevelKit.Tendril(root, new Vector3(-3.4f, floorY + 0.2f, z0 + 0.6f), new Vector3(-halfW * 0.7f, floorY + height * 0.3f, z0 + len * 0.08f), 0.35f, 7);
            }
        }
        else
        {
            // The vines: up one wall, across the ceiling, down the other
            LevelKit.Tendril(root, new Vector3(-halfW + 0.35f, floorY, z0 + len * 0.15f), new Vector3(-halfW + 0.35f, floorY + height - 0.4f, z0 + len * 0.45f), 0.4f, 10);
            LevelKit.Tendril(root, new Vector3(-halfW + 0.6f, floorY + height - 0.35f, z0 + len * 0.45f), new Vector3(halfW - 0.6f, floorY + height - 0.35f, z0 + len * 0.7f), 0.3f, 9);
            LevelKit.Tendril(root, new Vector3(halfW - 0.35f, floorY + height - 0.4f, z0 + len * 0.7f), new Vector3(halfW - 0.35f, floorY, z1 - len * 0.1f), 0.32f, 8);
            int lamps = Mathf.Max(1, Mathf.RoundToInt(len / 18f));
            for (int i = 0; i < lamps; i++)
                LevelKit.EmergencyLight(root, new Vector3((i % 2 == 0 ? -1f : 1f) * halfW * 0.4f, floorY + height - 0.4f, z0 + len * (i + 0.5f) / lamps), Mathf.Min(14f, height * 2f));
            LevelKit.Cable(root, new Vector3(halfW - 0.3f, floorY + height - 0.2f, z0 + 1f), new Vector3(halfW - 0.3f, floorY + height - 0.5f, z1 - 1f));
            LevelKit.Cable(root, new Vector3(-halfW + 0.3f, floorY + height - 0.3f, z0 + 1f), new Vector3(-halfW + 0.3f, floorY + height - 0.2f, z1 - 1f), 0.06f);
            LevelKit.Stencil(root, new Vector3(-halfW + 0.1f, floorY + height * 0.5f, z0 + len * R(0.2f, 0.4f)), Vector3.right);
            LevelKit.Stencil(root, new Vector3(halfW - 0.1f, floorY + height * 0.45f, z0 + len * R(0.55f, 0.85f)), Vector3.left);
        }
    }

    // ------------------------------------------------------------ sections

    /// The start: a bright civic hall with the MENU door behind you and the exit ahead
    public static Cursor Lobby(Transform root, MovementConfig config, Cursor c, float len = 24f, float halfW = 18f, float height = 10f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY;
        var door = Door(y);
        Seal(root, c, halfW, y, height);
        var win = WindowHoles(c, halfW, y, height, z0, z1);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), NoSouth, With(win, door));
        Windows(root, win, halfW);
        LevelKit.DoorFrame(root, door, z1);
        LevelKit.Sign(root, new Vector3(0f, y + Mathf.Min(height - 1.5f, 6.6f), z1), Vector3.back, 8f, 1.6f);
        LevelKit.Lawn(root, new Vector3(-halfW * 0.55f, y, z0 + len * 0.7f), new Vector2(halfW * 0.6f, len * 0.35f));
        LevelKit.Lawn(root, new Vector3(halfW * 0.55f, y, z0 + len * 0.7f), new Vector2(halfW * 0.6f, len * 0.35f));
        LevelKit.Tree(root, new Vector3(-halfW * 0.55f, y, z0 + len * 0.7f), Mathf.Min(height - 1.5f, 6f), true);
        LevelKit.Tree(root, new Vector3(halfW * 0.55f, y, z0 + len * 0.7f), Mathf.Min(height - 1.5f, 6f), false);
        LevelKit.Lattice(root, new Vector3(-halfW + 0.1f, y + height * 0.55f, z0 + len * 0.5f), Vector3.right, len * 0.55f, height * 0.6f);
        LevelKit.Lattice(root, new Vector3(halfW - 0.1f, y + height * 0.55f, z0 + len * 0.5f), Vector3.left, len * 0.55f, height * 0.6f);
        LevelKit.Pillar(root, new Vector3(-halfW * 0.65f, y + height * 0.5f, z0 + 6f), new Vector3(2f, height, 2f), LevelKit.Wall);
        LevelKit.Pillar(root, new Vector3(halfW * 0.65f, y + height * 0.5f, z0 + 6f), new Vector3(2f, height, 2f), LevelKit.Wall);
        Dress(root, c, halfW, y, height, z0, z1);
        Enemy.CreateDummy(new Vector3(-4f, y, z0 + len * 0.55f), config);
        Enemy.CreateDummy(new Vector3(4f, y, z0 + len * 0.55f), config);
        if (!Last.spawnLocked)
        {
            Last.spawn = new Vector3(0f, y + 1f, z0 + 8f);
            Last.menuPortal = new Vector3(0f, y + 2f, z0 + 1.7f);
        }
        return Exit(c, z1, y, halfW, y, y + height);
    }

    /// The ramp IS the floor: a fast slide down `drop`, a landing, a door and a
    /// glass above it for whoever carries the speed off the ramp
    public static Cursor Drop(Transform root, MovementConfig config, Cursor c, float drop, float len = 40f, float halfW = 14f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, top = c.floorY, bottom = c.floorY - drop;
        float height = drop + 8f;
        var door = Door(bottom);
        var glass = HighGlass(bottom + 8f);
        Seal(root, c, halfW, bottom, height);
        LevelKit.Room(root, new Vector3(-halfW, bottom, z0), new Vector3(halfW, bottom + height, z1),
            LevelKit.Faces.North | LevelKit.Faces.East | LevelKit.Faces.West | LevelKit.Faces.Ceiling, door, glass);
        LevelKit.DoorFrame(root, door, z1);
        LevelKit.Glass(root, new Vector3(0f, bottom + 10f, z1), new Vector3(6f, 4f, 0.3f), config);
        LevelKit.FloorSlab(root, new Vector3(0f, top - 0.5f, z0 + 1f), new Vector3(halfW * 2f, 1f, 2f), "DropLip");
        LevelKit.Ramp(root, 0f, halfW * 2f, z0 + 2f, top, z1 - 6f, bottom);
        LevelKit.FloorSlab(root, new Vector3(0f, bottom - 0.5f, z1 - 3f), new Vector3(halfW * 2f, 1f, 6f), "Landing");
        // The pillars stand through the ramp to the floor under it: a foot on the slope would hang over the downhill side
        float pTop = (top + bottom) * 0.5f + 6f, pBase = bottom - 1f;
        LevelKit.Pillar(root, new Vector3(-halfW * 0.5f, (pTop + pBase) * 0.5f, z0 + len * 0.5f), new Vector3(1.5f, pTop - pBase, 1.5f), LevelKit.Wall);
        LevelKit.Pillar(root, new Vector3(halfW * 0.5f, (pTop + pBase) * 0.5f, z0 + len * 0.5f), new Vector3(1.5f, pTop - pBase, 1.5f), LevelKit.Wall);
        for (int i = 0; i < 4; i++)
        {
            float z = z0 + 6f + i * (len - 12f) / 3f;
            float yr = top - drop * (z - z0 - 2f) / (len - 8f) + 2.5f;
            LevelKit.NeonStrip(root, new Vector3(-halfW + 0.1f, yr, z), new Vector3(0.1f, 0.12f, 6f));
            LevelKit.NeonStrip(root, new Vector3(halfW - 0.1f, yr, z), new Vector3(0.1f, 0.12f, 6f));
        }
        Dress(root, c, halfW, bottom, height, z0, z1);
        Enemy.CreateDummy(new Vector3(0f, bottom, z1 - 3f), config);
        return Exit(c, z1, bottom, halfW, bottom, bottom + height, bottom + 8f);
    }

    /// A fight room: crates and pillars to boost off, side galleries in the city,
    /// catwalks in the rot, an orb line overhead, the exit door with a glass over it
    public static Cursor Arena(Transform root, MovementConfig config, Cursor c, float len = 64f, float halfW = 30f, float height = 16f,
        int crates = 5, int chasers = 3, int turrets = 2, int dummies = 3)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY;
        var door = Door(y);
        var glass = HighGlass(y + 8f);
        Seal(root, c, halfW, y, height);
        var win = WindowHoles(c, halfW, y, height, z0, z1);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), NoSouth, With(win, door, glass));
        Windows(root, win, halfW);
        LevelKit.DoorFrame(root, door, z1);
        LevelKit.Glass(root, new Vector3(0f, y + 10f, z1), new Vector3(6f, 4f, 0.3f), config);
        LandGlass(root, c);
        var avoid = new List<Vector3>();
        for (int i = 0; i < crates; i++)
        {
            float s = R(2.5f, 5.5f);
            var p = new Vector3(R(-halfW + 6f, halfW - 6f), y + s * 0.5f, z0 + 10f + (len - 20f) * (i + 0.5f) / crates);
            LevelKit.Pillar(root, p, new Vector3(s, s, s), i % 2 == 0 ? LevelKit.Wall : LevelKit.Dark);
            avoid.Add(p);
        }
        // Side galleries (city) or catwalks (rot) along both walls
        float galleryY = y + height * 0.4f;
        LevelKit.FloorSlab(root, new Vector3(-halfW + 2f, galleryY - 0.5f, z0 + len * 0.5f), new Vector3(4f, 1f, len - 8f), "GalleryW");
        LevelKit.FloorSlab(root, new Vector3(halfW - 2f, galleryY - 0.5f, z0 + len * 0.5f), new Vector3(4f, 1f, len - 8f), "GalleryE");
        LevelKit.NeonStrip(root, new Vector3(-halfW + 4.05f, galleryY + 0.05f, z0 + len * 0.5f), new Vector3(0.1f, 0.08f, len - 8f));
        LevelKit.NeonStrip(root, new Vector3(halfW - 4.05f, galleryY + 0.05f, z0 + len * 0.5f), new Vector3(0.1f, 0.08f, len - 8f));
        LevelKit.Orb(root, new Vector3(0f, y + height - 2f, z0 + len * 0.3f));
        LevelKit.Orb(root, new Vector3(0f, y + height - 2f, z0 + len * 0.7f));
        if (c.zone == Zone.Utopia)
        {
            LevelKit.Planter(root, new Vector3(-halfW * 0.6f, y, z0 + 5f), new Vector3(4f, 0.6f, 1.5f));
            LevelKit.Planter(root, new Vector3(halfW * 0.6f, y, z1 - 5f), new Vector3(4f, 0.6f, 1.5f));
            LevelKit.Lawn(root, new Vector3(-halfW + 6.5f, y, z0 + len * 0.5f), new Vector2(4f, len * 0.5f));
            LevelKit.Lawn(root, new Vector3(halfW - 6.5f, y, z0 + len * 0.5f), new Vector2(4f, len * 0.5f));
            LevelKit.Tree(root, new Vector3(-halfW + 6.5f, y, z0 + len * 0.35f), Mathf.Min(galleryY - y - 0.5f, 6f), true);
            LevelKit.Tree(root, new Vector3(halfW - 6.5f, y, z0 + len * 0.65f), Mathf.Min(galleryY - y - 0.5f, 6f), true);
            avoid.Add(new Vector3(-halfW + 6.5f, y, z0 + len * 0.35f));
            avoid.Add(new Vector3(halfW - 6.5f, y, z0 + len * 0.65f));
            LevelKit.Lattice(root, new Vector3(-halfW + 0.1f, y + height * 0.74f, z0 + len * 0.5f), Vector3.right, len * 0.5f, height * 0.4f, 3f);
        }
        else
        {
            LevelKit.Stripes(root, new Vector3(-halfW + 4.05f, galleryY + 0.05f, z0 + len * 0.5f), new Vector3(0.1f, 0.08f, len - 8f));
            LevelKit.Stripes(root, new Vector3(halfW - 4.05f, galleryY + 0.05f, z0 + len * 0.5f), new Vector3(0.1f, 0.08f, len - 8f));
        }
        Dress(root, c, halfW, y, height, z0, z1);
        Squad(config, -halfW + 5f, halfW - 5f, z0 + 8f, z1 - 8f, y, y + height * 0.6f, chasers, turrets, dummies, avoid);
        Enemy.CreateDummy(new Vector3(-halfW + 2f, galleryY, z0 + len * 0.4f), config);
        Postcard(new Vector3(-halfW * 0.8f, y + height * 0.55f, z0 + 6f), new Vector3(halfW * 0.2f, y + 3f, z0 + len * 0.6f));
        return Exit(c, z1, y, halfW, y, y + height, y + 8f);
    }

    /// A long gallery: kick fins down the middle, a catwalk high line behind a
    /// glass gate on the east, an orb line, the exit door at the end
    public static Cursor Hall(Transform root, MovementConfig config, Cursor c, float len = 100f, float halfW = 18f, float height = 14f, int fins = 3)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY;
        var door = Door(y);
        Seal(root, c, halfW, y, height);
        var win = WindowHoles(c, halfW, y, height, z0, z1);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), NoSouth, With(win, door));
        Windows(root, win, halfW);
        LevelKit.DoorFrame(root, door, z1);
        LandGlass(root, c);
        var avoid = new List<Vector3>();
        // The fins stand off the centre with the room's width, so a wide gallery is a field of cover, not a spine
        float finX = Mathf.Max(3f, halfW * 0.34f);
        for (int i = 0; i < fins; i++)
        {
            float z = z0 + len * (i + 1f) / (fins + 1f);
            LevelKit.Pillar(root, new Vector3(-finX, y + 3f, z), new Vector3(1f, 6f, 8f), LevelKit.Wall);
            LevelKit.Pillar(root, new Vector3(finX, y + 3f, z), new Vector3(1f, 6f, 8f), LevelKit.Wall);
            LevelKit.NeonStrip(root, new Vector3(-finX, y + 6.05f, z), new Vector3(1.1f, 0.06f, 8.2f));
            LevelKit.NeonStrip(root, new Vector3(finX, y + 6.05f, z), new Vector3(1.1f, 0.06f, 8.2f));
            // The open floor between the fins is a launch: every other bay throws you up to the gallery line
            if (i % 2 == 0) LevelKit.JumpPad(root, new Vector3(0f, y, z + 4f), 0f, 15f, 7f);
            avoid.Add(new Vector3(0f, y, z));
            avoid.Add(new Vector3(0f, y, z + 4f));
        }
        // A gallery down the west wall to answer the east catwalk: two high lines through the room, not one
        float galY = y + height * 0.34f;
        LevelKit.FloorSlab(root, new Vector3(-halfW + 3f, galY - 0.5f, z0 + len * 0.5f), new Vector3(6f, 1f, len * 0.7f), "Gallery");
        LevelKit.GlassRail(root, new Vector3(-halfW + 6f, galY, z0 + len * 0.16f), new Vector3(-halfW + 6f, galY, z0 + len * 0.84f));
        LevelKit.Ramp(root, -halfW + 3f, 6f, z0 + len * 0.15f - (galY - y) * 1.8f, y, z0 + len * 0.15f, galY);   // a walkable pitch, meeting the gallery's end
        LevelKit.LightRing(root, new Vector3(0f, y + height - 4f, z0 + len * 0.32f), y + height, true);
        LevelKit.LightRing(root, new Vector3(0f, y + height - 4f, z0 + len * 0.72f), y + height, true);
        float catY = y + height * 0.55f;
        LevelKit.FloorSlab(root, new Vector3(halfW - 2f, catY - 0.5f, z0 + len * 0.55f), new Vector3(4f, 1f, len * 0.8f), "Catwalk");
        LevelKit.Glass(root, new Vector3(halfW - 2f, catY + 2f, z0 + len * 0.55f), new Vector3(4f, 4f, 0.3f), config); // high-line gate
        LevelKit.Slab(root, new Vector3(halfW - 4.15f, catY + 2f, z0 + len * 0.55f), new Vector3(0.3f, 4f, 0.5f), LevelKit.Dark, "GateJamb");
        LevelKit.NeonStrip(root, new Vector3(halfW - 4.05f, catY + 0.05f, z0 + len * 0.55f), new Vector3(0.1f, 0.06f, len * 0.8f));
        LevelKit.Orb(root, new Vector3(0f, y + height - 1.5f, z0 + len * 0.85f));
        Dress(root, c, halfW, y, height, z0, z1);
        Squad(config, -halfW + 2f, halfW - 2f, z0 + 10f, z1 - 10f, y, y + height * 0.7f, 1, 1, 0, avoid);
        Enemy.CreateDummy(new Vector3(halfW - 2f, catY, z0 + len * 0.35f), config);
        Enemy.CreateDummy(new Vector3(halfW - 2f, catY, z0 + len * 0.85f), config);
        return Exit(c, z1, y, halfW, y, y + height);
    }

    /// A low tunnel: slide-under pinches, corner-boost blocks, the rot's signage
    public static Cursor Corridor(Transform root, MovementConfig config, Cursor c, float len = 70f, int pinches = 2, float halfW = 7f, float height = 5f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY;
        var door = Door(y);
        Seal(root, c, halfW, y, height);
        var win = WindowHoles(c, halfW, y, height, z0, z1);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), NoSouth, With(win, door));
        Windows(root, win, halfW);
        LevelKit.DoorFrame(root, door, z1);
        LandGlass(root, c);
        for (int i = 0; i < pinches; i++)
        {
            float z = z0 + len * (i + 1f) / (pinches + 1f);
            float clear = 1.4f;
            LevelKit.Slab(root, new Vector3(0f, y + clear + (height - clear) * 0.5f, z), new Vector3(halfW * 2f, height - clear, 2f), LevelKit.Accent, "Pinch");
            LevelKit.Stripes(root, new Vector3(0f, y + clear + 0.05f, z - 1.1f), new Vector3(halfW * 2f, 0.1f, 0.1f));
            LevelKit.Stripes(root, new Vector3(0f, y + 0.04f, z - 8f), new Vector3(halfW * 2f, 0.06f, 0.3f));   // the tell where the crouch has to begin at speed, not at the lip
        }
        var avoid = new List<Vector3>();
        if (len > 40f)
        {
            var b = new Vector3(R(0f, 1f) > 0.5f ? halfW - 2.5f : -halfW + 2.5f, y + 1.5f, z0 + len * 0.72f);
            LevelKit.Pillar(root, b, new Vector3(3f, 3f, 3f), LevelKit.Dark);
            avoid.Add(b);
        }
        Dress(root, c, halfW, y, height, z0, z1);
        Squad(config, -halfW + 1.5f, halfW - 1.5f, z0 + 8f, z1 - 8f, y, y + height - 1.5f, 1, len > 60f ? 1 : 0, 1, avoid);
        return Exit(c, z1, y, halfW, y, y + height);
    }

    /// Open sky: kick walls down both sides, pads rising to a high glass, orbs, ads
    public static Cursor Yard(Transform root, MovementConfig config, Cursor c, float len = 80f, float halfW = 38f, float height = 30f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY;
        var door = Door(y);
        var glass = HighGlass(y + 8f);
        Seal(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1),
            LevelKit.Faces.North | LevelKit.Faces.East | LevelKit.Faces.West | LevelKit.Faces.Floor, door, glass);
        LevelKit.Clouds(root, new Vector3(0f, 0f, z0 + len * 0.5f), halfW * 1.6f, len * 0.6f, y + height + 8f, 7); // open sky
        LevelKit.DoorFrame(root, door, z1);
        LevelKit.Glass(root, new Vector3(0f, y + 10f, z1), new Vector3(6f, 4f, 0.3f), config);
        LandGlass(root, c);
        float kickX = Mathf.Min(halfW - 6f, 24f);
        LevelKit.Pillar(root, new Vector3(-kickX, y + 4f, z0 + len * 0.5f), new Vector3(1f, 8f, len * 0.5f), LevelKit.Wall);
        LevelKit.Pillar(root, new Vector3(kickX, y + 4f, z0 + len * 0.5f), new Vector3(1f, 8f, len * 0.5f), LevelKit.Wall);
        LevelKit.NeonStrip(root, new Vector3(-kickX, y + 8.05f, z0 + len * 0.5f), new Vector3(1.1f, 0.06f, len * 0.5f + 0.2f));
        LevelKit.NeonStrip(root, new Vector3(kickX, y + 8.05f, z0 + len * 0.5f), new Vector3(1.1f, 0.06f, len * 0.5f + 0.2f));
        var avoid = new List<Vector3>();
        float[] padX = { -12f, 0f, 12f, 0f };
        for (int i = 0; i < 4; i++)
        {
            var p = new Vector3(padX[i], y + 3.5f + 2f * i, z0 + len * (0.2f + 0.2f * i) + (i == 3 ? len * 0.05f : 0f));
            LevelKit.Slab(root, p, new Vector3(8f, 1f, 8f), LevelKit.Wood, "Deck");
            avoid.Add(p);
        }
        LevelKit.Orb(root, new Vector3(0f, y + 22f, z0 + len * 0.45f));
        LevelKit.Orb(root, new Vector3(0f, y + 24f, z0 + len * 0.8f));
        LevelKit.Planter(root, new Vector3(-halfW * 0.35f, y, z0 + 8f), new Vector3(5f, 0.6f, 2f));
        LevelKit.Planter(root, new Vector3(halfW * 0.35f, y, z0 + 8f), new Vector3(5f, 0.6f, 2f));
        // The gardens: lawns under the pads, blossom along the kick walls, a pool to run past
        LevelKit.Lawn(root, new Vector3(-halfW * 0.5f, y, z0 + len * 0.3f), new Vector2(halfW * 0.7f, len * 0.34f));
        LevelKit.Lawn(root, new Vector3(halfW * 0.5f, y, z0 + len * 0.7f), new Vector2(halfW * 0.7f, len * 0.34f));
        LevelKit.Pool(root, new Vector3(-halfW * 0.5f, y, z0 + len * 0.72f), new Vector2(8f, 6f));
        LevelKit.Hedge(root, new Vector3(-halfW * 0.5f, y, z0 + len * 0.72f + 4.5f), new Vector3(10f, 0.8f, 0.8f));
        LevelKit.Hedge(root, new Vector3(halfW * 0.5f, y, z0 + len * 0.7f - len * 0.18f), new Vector3(halfW * 0.7f, 0.8f, 0.8f));
        avoid.Add(new Vector3(-halfW * 0.5f, y, z0 + len * 0.72f));
        for (int i = 0; i < 6; i++)
        {
            var tp = new Vector3((i < 3 ? -1f : 1f) * (kickX - 3.5f), y, z0 + len * (0.2f + 0.3f * (i % 3)));
            LevelKit.Tree(root, tp, R(5f, 7f), i % 3 != 1);
            avoid.Add(tp);
        }
        LevelKit.Light(root, new Vector3(-halfW * 0.5f, y + 20f, z0 + len * 0.25f), 45f, 2f);
        LevelKit.Light(root, new Vector3(halfW * 0.5f, y + 20f, z0 + len * 0.25f), 45f, 2f);
        LevelKit.Light(root, new Vector3(-halfW * 0.5f, y + 20f, z0 + len * 0.8f), 45f, 2f);
        LevelKit.Light(root, new Vector3(halfW * 0.5f, y + 20f, z0 + len * 0.8f), 45f, 2f);
        Dress(root, c, halfW, y, height, z0, z1, true);
        Squad(config, -kickX + 4f, kickX - 4f, z0 + 10f, z1 - 10f, y, y + 12f, 2, 2, 1, avoid);
        Enemy.CreateDummy(new Vector3(0f, y + 6f, z0 + len * 0.4f), config);
        Postcard(new Vector3(-halfW * 0.75f, y + 14f, z0 + 10f), new Vector3(2f, y + 6f, z0 + len * 0.6f), 75f);
        return Exit(c, z1, y, halfW, y, y + height, y + 8f);
    }

    /// A steep slide down into the dark; opens straight into whatever comes next
    public static Cursor Chute(Transform root, MovementConfig config, Cursor c, float drop = 20f, float len = 36f, float halfW = 7f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, top = c.floorY, bottom = c.floorY - drop;
        float height = drop + 5f;
        var mouth = new Hole(Face.North, -halfW - 0.5f, halfW + 0.5f, bottom, bottom + 8f);
        Seal(root, c, halfW, bottom, height);
        LevelKit.Room(root, new Vector3(-halfW, bottom, z0), new Vector3(halfW, bottom + height, z1),
            LevelKit.Faces.North | LevelKit.Faces.East | LevelKit.Faces.West | LevelKit.Faces.Ceiling, mouth);
        LevelKit.FloorSlab(root, new Vector3(0f, top - 0.5f, z0 + 1f), new Vector3(halfW * 2f, 1f, 2f), "ChuteLip");
        LevelKit.Ramp(root, 0f, halfW * 2f, z0 + 2f, top, z1 - 2f, bottom);
        LevelKit.Slab(root, new Vector3(0f, bottom, z1 - 1f), new Vector3(halfW * 2f, 1f, 2f), LevelKit.Floor, "ChuteFoot");
        for (int i = 0; i < 4; i++)
        {
            float z = z0 + 4f + i * (len - 8f) / 3f;
            float yr = top - drop * (z - z0 - 2f) / (len - 4f) + 1.2f;
            LevelKit.Stripes(root, new Vector3(-halfW + 0.1f, yr, z), new Vector3(0.1f, 0.1f, 5f));
            LevelKit.Stripes(root, new Vector3(halfW - 0.1f, yr, z), new Vector3(0.1f, 0.1f, 5f));
        }
        Dress(root, c, halfW, bottom, height, z0, z1);
        return Exit(c, z1, bottom, halfW, bottom, bottom + height);
    }

    /// A wall-kick shaft: the room's own walls are the kick walls, `gap` apart;
    /// a ledge and the exit door at the top, an orb for the assist
    public static Cursor Chimney(Transform root, MovementConfig config, Cursor c, float rise = 12f, float gap = 9f, float len = 14f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY, top = c.floorY + rise;
        float halfW = gap * 0.5f;
        float height = rise + 5f;
        var door = Door(top);
        Seal(root, c, halfW, y, height);
        var win = WindowHoles(c, halfW, y, height, z0, z1);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), NoSouth, With(win, door));
        Windows(root, win, halfW);
        LevelKit.DoorFrame(root, door, z1);
        LevelKit.FloorSlab(root, new Vector3(0f, top - 0.5f, z1 - 1.5f), new Vector3(gap, 1f, 3f), "ShaftLedge");
        LevelKit.Orb(root, new Vector3(0f, top + 2.5f, z0 + len * 0.4f));
        LevelKit.Stripes(root, new Vector3(0f, y + 0.05f, z0 + 2f), new Vector3(gap, 0.08f, 0.1f)); // the chimney floor is marked
        Dress(root, c, halfW, y, height, z0, z1);
        return Exit(c, z1, top, halfW, y, y + height);
    }

    /// Two lanes of long ramps climbing `rise`, a cross ledge between them
    public static Cursor Switchback(Transform root, MovementConfig config, Cursor c, float len = 100f, float rise = 16f, float halfW = 14f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY, top = c.floorY + rise, mid = c.floorY + rise * 0.5f;
        float height = rise + 8f;
        var door = Door(top);
        Seal(root, c, halfW, y, height);
        var win = WindowHoles(c, halfW, y, height, z0, z1);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), NoSouth, With(win, door));
        Windows(root, win, halfW);
        LevelKit.DoorFrame(root, door, z1);
        LandGlass(root, c);
        float laneW = halfW - 1f, laneX = halfW * 0.5f;
        float zA = z0 + 6f, zB = z0 + len * 0.46f, zC = z0 + len * 0.56f, zD = z1 - 4f;
        LevelKit.Ramp(root, -laneX, laneW, zA, y, zB, mid);                                              // west lane up
        LevelKit.FloorSlab(root, new Vector3(0f, mid - 0.5f, (zB + zC) * 0.5f), new Vector3(halfW * 2f, 1f, zC - zB), "CrossLedge");
        LevelKit.Ramp(root, laneX, laneW, zC, mid, zD, top);                                             // east lane up
        LevelKit.FloorSlab(root, new Vector3(0f, top - 0.5f, (zD + z1) * 0.5f), new Vector3(halfW * 2f, 1f, z1 - zD), "TopLedge");
        LevelKit.NeonStrip(root, new Vector3(-0.45f, y + rise * 0.28f, (zA + zB) * 0.5f), new Vector3(0.1f, 0.1f, zB - zA));
        LevelKit.NeonStrip(root, new Vector3(0.45f, mid + rise * 0.28f, (zC + zD) * 0.5f), new Vector3(0.1f, 0.1f, zD - zC));
        LevelKit.Pillar(root, new Vector3(laneX, y + 2f, z0 + len * 0.2f), new Vector3(2f, 4f, 2f), LevelKit.Wall);
        LevelKit.Pillar(root, new Vector3(laneX, y + 2f, z0 + len * 0.36f), new Vector3(2f, 4f, 2f), LevelKit.Wall);
        LevelKit.Orb(root, new Vector3(0f, mid + 6f, z0 + len * 0.3f));
        LevelKit.Orb(root, new Vector3(0f, top + 5f, z0 + len * 0.8f));
        Dress(root, c, halfW, y, height, z0, z1);
        EnemyPool.Air(new Vector3(0f, mid + 5f, z0 + len * 0.5f), config);
        EnemyPool.Ground(new Vector3(laneX, y, z0 + len * 0.28f), config);
        EnemyPool.Ground(new Vector3(-laneX, mid, (zB + zC) * 0.5f), config);
        Enemy.CreateDummy(new Vector3(-laneX, mid, zC), config);
        Enemy.CreateDummy(new Vector3(laneX, top, zD + 1f), config);
        return Exit(c, z1, top, halfW, y, y + height);
    }

    /// A bridge over a drop, open sky: fins to kick along, orbs to swing, a 4 m
    /// gap before the door with a wall-kick chimney under it for whoever falls
    public static Cursor Skybridge(Transform root, MovementConfig config, Cursor c, float len = 50f, float halfW = 20f, float depth = 16f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY, pit = c.floorY - depth;
        float height = depth + 14f;
        var door = Door(y);
        Seal(root, c, halfW, pit, height);
        LevelKit.Room(root, new Vector3(-halfW, pit, z0), new Vector3(halfW, pit + height, z1),
            LevelKit.Faces.North | LevelKit.Faces.East | LevelKit.Faces.West | LevelKit.Faces.Floor, door);
        LevelKit.Clouds(root, new Vector3(0f, 0f, z0 + len * 0.5f), halfW * 1.6f, len * 0.6f, pit + height + 8f, 5); // open sky
        LevelKit.DoorFrame(root, door, z1);
        LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, (z0 + z1 - 6f) * 0.5f), new Vector3(6f, 1f, len - 6f), "Bridge");        // to a 4 m gap
        LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, z1 - 1f), new Vector3(10f, 1f, 2f), "DoorLanding");
        LevelKit.NeonStrip(root, new Vector3(-3.05f, y + 0.05f, (z0 + z1 - 6f) * 0.5f), new Vector3(0.1f, 0.06f, len - 6f));
        LevelKit.NeonStrip(root, new Vector3(3.05f, y + 0.05f, (z0 + z1 - 6f) * 0.5f), new Vector3(0.1f, 0.06f, len - 6f));
        LevelKit.Stripes(root, new Vector3(0f, y + 0.05f, z1 - 6.1f), new Vector3(6f, 0.08f, 0.1f)); // the gap is marked
        LevelKit.Pillar(root, new Vector3(-2.5f, y + 2f, z0 + len * 0.3f), new Vector3(1f, 4f, 6f), LevelKit.Wall);    // fins
        LevelKit.Pillar(root, new Vector3(2.5f, y + 2f, z0 + len * 0.6f), new Vector3(1f, 4f, 6f), LevelKit.Wall);
        LevelKit.Pillar(root, new Vector3(-2.5f, (y + pit) * 0.5f, z1 - 4f), new Vector3(1f, depth, 4f), LevelKit.Wall); // the recovery chimney
        LevelKit.Pillar(root, new Vector3(2.5f, (y + pit) * 0.5f, z1 - 4f), new Vector3(1f, depth, 4f), LevelKit.Wall);
        LevelKit.Stripes(root, new Vector3(0f, pit + 0.05f, z1 - 4f), new Vector3(4f, 0.08f, 0.1f));
        LevelKit.Orb(root, new Vector3(0f, y + 7f, z0 + len * 0.25f));
        LevelKit.Orb(root, new Vector3(0f, y + 7f, z0 + len * 0.75f));
        LevelKit.Light(root, new Vector3(0f, pit + 6f, z0 + len * 0.5f), 30f, 1.2f);
        Dress(root, c, halfW, pit, height, z0, z1, true);
        EnemyPool.Air(new Vector3(-halfW * 0.6f, y + 5f, z0 + len * 0.4f), config);
        EnemyPool.Air(new Vector3(halfW * 0.6f, y + 5f, z0 + len * 0.7f), config);
        EnemyPool.Ground(new Vector3(0f, y, z0 + len * 0.65f), config);
        Enemy.CreateDummy(new Vector3(0f, y, z0 + len * 0.2f), config);
        return Exit(c, z1, y, halfW, pit, pit + height);
    }

    /// A station: a raised platform lane beside a glass-gated track lane, columns to boost off
    public static Cursor TransitDeck(Transform root, MovementConfig config, Cursor c, float len = 100f, float halfW = 18f, float height = 14f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY;
        var door = Door(y);
        Seal(root, c, halfW, y, height);
        var win = WindowHoles(c, halfW, y, height, z0, z1);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), NoSouth, With(win, door));
        Windows(root, win, halfW);
        LevelKit.DoorFrame(root, door, z1);
        LandGlass(root, c);
        float platX0 = -halfW, platX1 = -halfW * 0.25f;
        LevelKit.FloorSlab(root, new Vector3((platX0 + platX1) * 0.5f, y + 1.5f, z0 + len * 0.5f), new Vector3(platX1 - platX0, 1f, len - 10f), "Platform"); // top y+2
        LevelKit.Stripes(root, new Vector3(platX1 + 0.05f, y + 2.05f, z0 + len * 0.5f), new Vector3(0.1f, 0.08f, len - 10f));
        float gateZ = z0 + len * 0.6f;
        LevelKit.WallFace(root, Face.North, gateZ, platX1, halfW + 0.5f, y, y + 4f, new Hole(Face.North, 1f, 7f, y, y + 4f));
        LevelKit.Glass(root, new Vector3(4f, y + 2f, gateZ), new Vector3(6f, 4f, 0.3f), config);
        var avoid = new List<Vector3>();
        // The concourse side of the tracks: pillars spread across whatever width the deck has, and a pad back up
        for (int i = 0; i < 5; i++)
        {
            var p = new Vector3(Mathf.Lerp(platX1 + 3f, halfW - 3f, (i % 2 == 0 ? 0.25f : 0.72f)), y + 3f, z0 + len * (0.16f + 0.17f * i));
            LevelKit.Pillar(root, p, new Vector3(1.5f, 6f, 1.5f), LevelKit.Wall);
            avoid.Add(p);
        }
        LevelKit.JumpPad(root, new Vector3(halfW - 4f, y, z0 + len * 0.45f), 180f, 13f, 5f);
        LevelKit.LightRing(root, new Vector3((platX1 + halfW) * 0.5f, y + height - 3.5f, z0 + len * 0.6f), y + height, true);
        LevelKit.Orb(root, new Vector3(0f, y + height - 3f, z0 + len * 0.25f));
        LevelKit.Orb(root, new Vector3(0f, y + height - 3f, z0 + len * 0.75f));
        LevelKit.Sign(root, new Vector3(0f, y + 6f, z1), Vector3.back, 8f, 1.6f, LevelKit.Company + " RAILWAYS");
        LevelKit.Planter(root, new Vector3(-6f, y + 2f, z0 + 15f), new Vector3(3f, 0.6f, 1.2f));
        LevelKit.Planter(root, new Vector3(-6f, y + 2f, z1 - 15f), new Vector3(3f, 0.6f, 1.2f));
        Dress(root, c, halfW, y, height, z0, z1);
        Squad(config, -1f, halfW - 2f, z0 + 10f, z1 - 10f, y, y + 9f, 2, 2, 1, avoid);
        Enemy.CreateDummy(new Vector3(-6f, y + 2f, z0 + len * 0.3f), config);
        Enemy.CreateDummy(new Vector3(-6f, y + 2f, z0 + len * 0.85f), config);
        EnemyPool.Ground(new Vector3(-6f, y + 2f, z0 + len * 0.55f), config);
        return Exit(c, z1, y, halfW, y, y + height);
    }

    /// The civic street: a planted median, kick fins, a glass-gated catwalk, ads
    public static Cursor Promenade(Transform root, MovementConfig config, Cursor c, float len = 120f, float halfW = 24f, float height = 14f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY;
        var door = Door(y);
        Seal(root, c, halfW, y, height);
        var win = WindowHoles(c, halfW, y, height, z0, z1);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), NoSouth, With(win, door));
        Windows(root, win, halfW);
        LevelKit.DoorFrame(root, door, z1);
        LandGlass(root, c);
        var avoid = new List<Vector3>();
        LevelKit.Planter(root, new Vector3(0f, y, z0 + len * 0.25f), new Vector3(3f, 0.6f, 12f));  // the median
        LevelKit.Planter(root, new Vector3(0f, y, z0 + len * 0.6f), new Vector3(3f, 0.6f, 12f));
        LevelKit.Tree(root, new Vector3(0f, y + 0.6f, z0 + len * 0.25f - 4f), 6f, true);
        LevelKit.Tree(root, new Vector3(0f, y + 0.6f, z0 + len * 0.25f + 4f), 5f, true);
        LevelKit.Tree(root, new Vector3(0f, y + 0.6f, z0 + len * 0.6f - 4f), 5.5f, false);
        LevelKit.Tree(root, new Vector3(0f, y + 0.6f, z0 + len * 0.6f + 4f), 6f, true);
        LevelKit.Lawn(root, new Vector3(-halfW + 4f, y, z0 + len * 0.5f), new Vector2(5f, len * 0.7f));
        LevelKit.Hedge(root, new Vector3(-halfW + 1.8f, y, z0 + len * 0.5f), new Vector3(0.8f, 0.9f, len * 0.6f));
        avoid.Add(new Vector3(0f, y, z0 + len * 0.25f));
        avoid.Add(new Vector3(0f, y, z0 + len * 0.6f));
        // The colonnade stands off the median with the promenade's width
        float colX = Mathf.Max(5f, halfW * 0.42f);
        for (int i = 0; i < 4; i++)
        {
            var p = new Vector3(i % 2 == 0 ? -colX : colX * 0.85f, y + 3f, z0 + len * (0.3f + 0.16f * i));
            LevelKit.Pillar(root, p, new Vector3(1f, 6f, 8f), LevelKit.Wall);
            LevelKit.NeonStrip(root, new Vector3(p.x, y + 6.05f, p.z), new Vector3(1.1f, 0.06f, 8.2f));
            avoid.Add(p);
        }
        LevelKit.JumpPad(root, new Vector3(-halfW + 6f, y, z0 + len * 0.42f), 0f, 14f, 6f);
        LevelKit.JumpPad(root, new Vector3(-halfW + 6f, y, z0 + len * 0.78f), 0f, 14f, 6f);
        float catX = halfW - 4f, catY = y + 6f;
        LevelKit.Slab(root, new Vector3(catX, catY - 0.5f, z0 + len * 0.5f), new Vector3(6f, 1f, len * 0.66f), LevelKit.Wood, "Deck");
        LevelKit.Glass(root, new Vector3(catX, catY + 2f, z0 + len * 0.33f), new Vector3(6f, 4f, 0.3f), config);
        LevelKit.Slab(root, new Vector3(catX - 3.15f, catY + 2f, z0 + len * 0.33f), new Vector3(0.3f, 4f, 0.5f), LevelKit.Dark, "GateJamb");
        LevelKit.NeonStrip(root, new Vector3(catX - 2.95f, catY + 0.05f, z0 + len * 0.5f), new Vector3(0.1f, 0.06f, len * 0.66f));
        LevelKit.Orb(root, new Vector3(0f, y + 11f, z0 + len * 0.25f));
        LevelKit.Orb(root, new Vector3(0f, y + 12f, z0 + len * 0.7f));
        LevelKit.Sign(root, new Vector3(0f, y + 8.5f, z1), Vector3.back, 10f, 2.2f);
        LevelKit.Planter(root, new Vector3(-halfW + 3f, y, z0 + 15f), new Vector3(4f, 0.6f, 1.5f));
        LevelKit.Planter(root, new Vector3(-halfW + 3f, y, z1 - 15f), new Vector3(4f, 0.6f, 1.5f));
        Dress(root, c, halfW, y, height, z0, z1);
        Squad(config, -halfW + 3f, catX - 4f, z0 + 12f, z1 - 12f, y, y + 9f, 3, 2, 1, avoid);
        Enemy.CreateDummy(new Vector3(catX, catY, z0 + len * 0.5f), config);
        return Exit(c, z1, y, halfW, y, y + height);
    }

    /// A mezzanine with a trench cut across it: kick walls, pogo pedestals, a chaser waiting in the dark
    public static Cursor Trench(Transform root, MovementConfig config, Cursor c, float len = 40f, float halfW = 20f, float height = 10f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY, floor = c.floorY - 4f;
        var door = Door(y);
        Seal(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1),
            LevelKit.Faces.North | LevelKit.Faces.East | LevelKit.Faces.West | LevelKit.Faces.Ceiling, door);
        LevelKit.DoorFrame(root, door, z1);
        LandGlass(root, c);
        // The floor in three: two sides, and a bridge at each end of the trench
        float tz0 = z0 + len * 0.25f, tz1 = z1 - len * 0.25f;
        LevelKit.FloorSlab(root, new Vector3(-halfW * 0.5f - 2f, y - 0.5f, z0 + len * 0.5f), new Vector3(halfW - 4f, 1f, len + 1f), "MezzW");
        LevelKit.FloorSlab(root, new Vector3(halfW * 0.5f + 2f, y - 0.5f, z0 + len * 0.5f), new Vector3(halfW - 4f, 1f, len + 1f), "MezzE");
        LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, (z0 - 0.5f + tz0) * 0.5f), new Vector3(8f, 1f, tz0 - z0 + 0.5f), "MezzS");
        LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, (tz1 + z1 + 0.5f) * 0.5f), new Vector3(8f, 1f, z1 - tz1 + 0.5f), "MezzN");
        LevelKit.Stripes(root, new Vector3(-4.05f, y + 0.05f, (tz0 + tz1) * 0.5f), new Vector3(0.1f, 0.08f, tz1 - tz0));
        LevelKit.Stripes(root, new Vector3(4.05f, y + 0.05f, (tz0 + tz1) * 0.5f), new Vector3(0.1f, 0.08f, tz1 - tz0));
        // The trench itself: the rot under the floor
        LevelKit.SetZone(Zone.Underbelly);
        LevelKit.FloorSlab(root, new Vector3(0f, floor - 0.5f, (tz0 + tz1) * 0.5f), new Vector3(8f, 1f, tz1 - tz0), "TrenchFloor");
        LevelKit.Pillar(root, new Vector3(-3.5f, floor + 2f, (tz0 + tz1) * 0.5f), new Vector3(1f, 4f, tz1 - tz0), LevelKit.Wall);
        LevelKit.Pillar(root, new Vector3(3.5f, floor + 2f, (tz0 + tz1) * 0.5f), new Vector3(1f, 4f, tz1 - tz0), LevelKit.Wall);
        LevelKit.Slab(root, new Vector3(0f, floor + 2f, tz0 - 0.5f), new Vector3(8f, 4f, 1f), LevelKit.Wall, "TrenchEnd");
        LevelKit.Slab(root, new Vector3(0f, floor + 2f, tz1 + 0.5f), new Vector3(8f, 4f, 1f), LevelKit.Wall, "TrenchEnd");
        LevelKit.Slab(root, new Vector3(0f, floor + 1.5f, tz0 + (tz1 - tz0) * 0.3f), new Vector3(2f, 3f, 2f), LevelKit.Accent, "Pedestal");
        LevelKit.Slab(root, new Vector3(0f, floor + 1.5f, tz0 + (tz1 - tz0) * 0.7f), new Vector3(2f, 3f, 2f), LevelKit.Accent, "Pedestal");
        LevelKit.EmergencyLight(root, new Vector3(0f, y - 0.4f, (tz0 + tz1) * 0.5f), 8f);
        LevelKit.Stencil(root, new Vector3(-2.9f, floor + 2f, (tz0 + tz1) * 0.5f), Vector3.right);
        Enemy.CreateDummy(new Vector3(0f, floor + 3f, tz0 + (tz1 - tz0) * 0.3f), config);
        Enemy.CreateDummy(new Vector3(0f, floor + 3f, tz0 + (tz1 - tz0) * 0.7f), config);
        EnemyPool.Ground(new Vector3(-1.5f, floor, tz1 - 2f), config);
        LevelKit.SetZone(c.zone);
        Dress(root, c, halfW, y, height, z0, z1);
        EnemyPool.Ground(new Vector3(halfW * 0.5f, y, z0 + len * 0.8f), config);
        return Exit(c, z1, y, halfW, y, y + height);
    }

    /// The end: a vestibule with the FINISH door
    public static Cursor Finish(Transform root, MovementConfig config, Cursor c, float len = 20f, float halfW = 16f, float height = 14f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY;
        Seal(root, c, halfW, y, height);
        var win = WindowHoles(c, halfW, y, height, z0, z1);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), NoSouth, With(win));
        Windows(root, win, halfW);
        LandGlass(root, c);
        LevelKit.Billboard(root, new Vector3(0f, y + 8f, z1 - 0.1f), 10f, 4f, 0f);
        LevelKit.Sign(root, new Vector3(0f, y + 12f, z1), Vector3.back, 8f, 1.6f);
        LevelKit.LightStrip(root, new Vector3(0f, y + height - 0.1f, z0 + len * 0.5f), 12f, true);
        LevelKit.Planter(root, new Vector3(-6f, y, z1 - 4f), new Vector3(3f, 0.6f, 1.2f));
        LevelKit.Planter(root, new Vector3(6f, y, z1 - 4f), new Vector3(3f, 0.6f, 1.2f));
        Last.finishPortal = new Vector3(0f, y + 2f, z1 - 2.5f);
        Last.length = z1;
        return Exit(c, z1, y, halfW, y, y + height);
    }
}
