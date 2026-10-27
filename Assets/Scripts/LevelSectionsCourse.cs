using System.Collections.Generic;
using UnityEngine;
using Cursor = LevelSections.Cursor;

/// <summary>
/// THE COURSE (2026-10-09, course-grammar: "get rid of the room to room style, do something more like Bloodthief,
/// the movement is the main attraction"). A level is ONE CONTINUOUS COURSE of LINES: spawn to finish, 700–1200 m,
/// never a sealed space, never kill-to-open, never a door that closes. Every space here is OPEN — side walls, a
/// floor, a ceiling where it is roofed, and NO north wall, NO door, nothing across the lane — and is designed as
/// lines: a MAIN LINE anyone runs at 8 m/s and FAST LINES that need earned momentum (a bhop chain over a gap, a wall
/// run into an EDGE BOOST up a corner, a CORNER BOOST onto a lip, a slide-hop, glass at speed, a grapple swing, a
/// pad), each saving metres or height. A missed fast line drops you onto the main line, never into a pit. Units are
/// FUEL on the lines, never gates. Thresholds are arches. Checkpoints are invisible line crossings (CourseFlow).
///
/// The dimensions (from the ladder): jump 9 rises 1.6 m; a corner boost at run speed rises ~2.4 m, ~3.3 m at 18 m/s,
/// ×1.6 within 2 s of a wall jump (WALL COMBO), capped at 6.5 m; a bhop at 18 m/s covers ~16 m, a run jump ~7 m.
/// Ceilings ≥ 8 m, ≥ 12 m where lips stack; lanes ≥ 6 m; gaps 6 m = main line, 9–14 m = fast line; lips 0.9–1.8 m
/// above their approach; kick walls 3–6 m tall; wall-run walls ≥ 12 m; chutes 20–35°; drops ≤ 12 m onto a slide or
/// a pad; edge-boost corners are real 90° outside corners ≥ 4 m tall.
///
/// Every space: Begin (zone, its own deterministic seed, the spawn if nothing has it yet) → Threshold (what keeps the
/// outside out, never across the lane; the arch) → the shell and the lines → fuel → the zone's dressing → Open (the
/// cursor on, marked open: the next space lands on this floor). Builders call exactly these:
///   Hall · Stair · Shaft · Roof · Chute · Court · Gallery · DropIn · Bank · Bridge · Checkpoint · Fuel
/// (LevelSections.Begin and LevelSections.Finish stay the ends of every chain.)
/// </summary>
public static partial class Course
{
    // The dimensions
    public const float Head = 8f;          // the least headroom over a hall's floor
    public const float HeadLips = 12f;     // where lips stack
    public const float LaneMin = 6f;
    public const float RunGap = 6f;        // a gap the main line jumps at run speed
    public const float FastGap = 11f;      // a gap a fast line hops at 18 m/s (9–14)
    public const float WallRunMin = 12f;
    public const float ChuteMin = 20f, ChuteMax = 35f;
    public const float DropMax = 12f;
    const float RampDeg = 18f;             // a ramp the main line runs without losing its speed
    const float T = LevelKit.T;
    const LevelKit.Faces Sides = LevelKit.Faces.East | LevelKit.Faces.West;
    const LevelKit.Faces Roofed = Sides | LevelKit.Faces.Floor | LevelKit.Faces.Ceiling;
    const LevelKit.Faces Walled = Sides | LevelKit.Faces.Ceiling;    // the floor drawn by hand
    const LevelKit.Faces Yard = Sides | LevelKit.Faces.Floor;        // open sky

    static System.Random rng = new System.Random(1);
    static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
    static float Run(float rise) => Mathf.Abs(rise) / Mathf.Tan(RampDeg * Mathf.Deg2Rad);

    // ------------------------------------------------------------ the chain

    /// Every space starts here: the zone, its own seed (the level's and its place on the chain, so the same course
    /// builds the same way every run whatever is built before it), the spawn if nothing has claimed it yet
    static void Begin(Cursor c)
    {
        LevelKit.SetZone(c.zone);
        rng = new System.Random(unchecked(LevelSections.Seed * 7919 + Mathf.RoundToInt(c.z * 10f)));
        var last = LevelSections.Last;
        if (last != null && !last.spawnLocked && last.spawn == Vector3.zero && c.z < 0.01f)
        {
            last.spawn = new Vector3(0f, c.floorY + 1f, c.z + 6f);
            last.menuPortal = new Vector3(0f, c.floorY + 2f, c.z + 1.7f);
        }
    }

    /// The entry plane: whatever keeps the outside out (never across the lane: LevelSections.Seal), the balcony for
    /// a high glass in a wall behind, and the arch that says you crossed into a new space
    static void Threshold(Transform root, Cursor c, float halfW, float floorY, float height)
    {
        LevelSections.Seal(root, c, halfW, floorY, height);
        LevelSections.LandGlass(root, c);
        LevelKit.Arch(root, halfW, floorY, height, c.z + 1.3f);
    }

    /// The way on: no wall, no door. The next space lands on `floorY` inside a lane `halfW` wide under `height`
    static Cursor Open(Cursor c, float z1, float floorY, float halfW, float height)
    {
        var next = LevelSections.Exit(c, z1, floorY, halfW, floorY, floorY + height);
        next.open = true;
        return next;
    }

    /// The box round a space: the faces asked for (never South, never North), the Utopia ribbon windows where it is
    /// roofed, the zone's trims (Articulate runs from Room)
    static void Shell(Transform root, Cursor c, float halfW, float y, float height, float z0, float z1, LevelKit.Faces faces)
    {
        var win = (faces & LevelKit.Faces.Ceiling) != 0 ? LevelSections.WindowHoles(c, halfW, y, height, z0, z1) : new Hole[0];
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), faces, win);
        LevelSections.Windows(root, win, halfW);
    }

    /// The zone's dressing (ads, lamps, the rot, the plant's density). `bare`: a space with no walls of its own (a
    /// roof) takes only the air and its apertures, as the Surface zone always does
    static void Dressing(Transform root, Cursor c, float halfW, float y, float height, float z0, float z1, bool openSky, bool bare = false)
    {
        if (bare || c.zone == Zone.Surface)
        {
            if (openSky) { LevelSections.See(-1, z0, z1, y + height, y + height + 220f); LevelSections.See(1, z0, z1, y + height, y + height + 220f); }
            LevelKit.Air(root, c.zone, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), openSky);
            return;
        }
        LevelSections.Dress(root, c, halfW, y, height, z0, z1, openSky);
    }

    // ------------------------------------------------------------ checkpoints and fuel

    /// A CHECKPOINT at the cursor: an invisible line across the lane. Crossing it makes it the respawn: CourseFlow
    /// stands you up two and a half metres past it, at run speed, facing on. A brass inlay marks the line (state).
    public static void Checkpoint(Transform root, Cursor c, string name)
    {
        var last = LevelSections.Last;
        if (last == null) return;
        last.checkpoints.Add(new LevelSections.CheckpointMark { name = name, z = c.z, at = new Vector3(0f, c.floorY + 0.6f, c.z + 2.5f) });
        float halfW = c.halfW > 1f ? c.halfW : 6f;
        LevelKit.CheckpointInlay(root, halfW - 1.2f, c.floorY, c.z + 0.9f);
    }

    /// FUEL: a unit of `kind` standing (or, an airborne kind, hovering) at `at`, on the line called `line`. Killing
    /// it is optional; where it stands is the point: on the line where cutting through keeps or gives speed
    public static void Fuel(Transform root, MovementConfig config, Cursor c, EnemyKind kind, Vector3 at, string line)
    {
        // The kind is the body and the name; what the unit DOES is one of four roles, mapped inside the factory
        // (Enemy.Create → Enemy.RoleOf): a footman on its line, a shooter, the bouncer, or the page's laser. Settle
        // puts it on the floor of its line (a springboard is a body at kickable height, never one in the air).
        var e = Spawn(kind, at, config);
        if (e == null) return;
        LevelSections.Last?.fuel.Add(new LevelSections.FuelMark { unit = e, line = line });
    }

    static Enemy Spawn(EnemyKind kind, Vector3 at, MovementConfig config)
    {
        switch (kind)
        {
            case EnemyKind.Dummy: return Enemy.CreateDummy(at, config);
            case EnemyKind.Turret: return Enemy.CreateTurret(at, config);
            case EnemyKind.Chaser: return Enemy.CreateChaser(at, config);
            case EnemyKind.Janitor: return Enemy.CreateJanitor(at, config);
            case EnemyKind.Borer: return Enemy.CreateBorer(at, config);
            case EnemyKind.Rigger: return Enemy.CreateRigger(at, config);
            case EnemyKind.Blaster: return Enemy.CreateBlaster(at, config);
            case EnemyKind.Pursuit: return Enemy.CreatePursuit(at, config);
            case EnemyKind.Hound: return Enemy.CreateHound(at, config);
            case EnemyKind.Interceptor: return Enemy.CreateInterceptor(at, config);
            case EnemyKind.Kettler: return Enemy.CreateKettler(at, config);
            case EnemyKind.DamageControl: return Enemy.CreateDamageControl(at, config);
            case EnemyKind.Sentry: return Enemy.CreateSentry(at, config);
            case EnemyKind.Skitter: return Enemy.CreateSkitter(at, config);
            case EnemyKind.Marshal: return Enemy.CreateMarshal(at, config);
            case EnemyKind.Warden: return Enemy.CreateWarden(at, config);
            case EnemyKind.Lurker: return Enemy.CreateLurker(at, config);
            case EnemyKind.Moth: return Enemy.CreateMoth(at, config);
            case EnemyKind.Ocular: return Enemy.CreateOcular(at, config);
            case EnemyKind.Overseer: return Enemy.CreateOverseer(at, config);
            case EnemyKind.Scourer: return Enemy.CreateScourer(at, config);
            default: Debug.LogWarning("Course.Fuel: " + kind + " is not fuel"); return null;
        }
    }

    /// Something from the level's roster, on a line (the pool turns, so no two spaces field the same three)
    static void PoolFuel(MovementConfig config, Vector3 at, string line, bool air = false)
    {
        var e = air ? EnemyPool.Air(at, config) : EnemyPool.Ground(at, config);
        if (e != null) LevelSections.Last?.fuel.Add(new LevelSections.FuelMark { unit = e, line = line });
    }
}
