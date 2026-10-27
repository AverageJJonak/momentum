using System.Collections.Generic;
using UnityEngine;
using Cursor = LevelSections.Cursor;

/// <summary>
/// THE DISTRICT (district-grammar, 2026-10-12; user: "levels should be way less linear, different paths and open
/// areas, streets and alleyways and speakeasies and hotels"). A level is a DISTRICT: a grid of city BLOCKS with
/// STREETS (≥ 14 m: open ground, traffic, trams, lamp standards) and ALLEYS (3–5 m: fire escapes, lips, a tech line)
/// between them, the start on the south edge and the finish on the north, and MANY ways between — along the streets,
/// down the alleys, and THROUGH the ESTABLISHMENTS that fill the blocks (Establishments.*), each enterable from two
/// to four sides at different heights (a door, a window, a roof hatch, a cellar hatch, a skylight, a balcony, a
/// marquee). Junction plazas sit where streets cross so the player sees three ways on. Checkpoints are the junctions'
/// VOLUMES (CourseFlow reads boxes) and the respawn sends you along the authored ROUTE's direction there. The course
/// rules stand inside: open everywhere, no gate, lanes ≥ 6 m on the streets, fuel on lines, a miss never kills.
///
/// The class is CityDistrict (the bible's `District` is Act III's beacons-and-updrafts MonoBehaviour, District.cs):
///   DistrictPlan Plan(seed, blocksX, blocksZ, blockW, blockD, streetW) · Place(plan, bx, bz, kind, rotation) ·
///   Cursor Build(root, config, c, plan, zone) · Route(plan, from, to) / Route(plan, waypoints) ·
///   Checkpoint(root, name, Bounds) · Checkpoints(root, plan) · Fuel(root, config, kind, at, line) · Lint(plan)
/// A level builder: the Ward/cloakroom opening on the Cursor chain, then Plan → Place the establishments → Route →
/// Build (lays the grid at the cursor, returns the cursor open at the north edge, x = 0) → Checkpoints → Finish.
/// </summary>
public static partial class CityDistrict
{
    /// What fills a block. Open ground kinds first; every other one is a building you pass through
    public enum Establishment { Open, Plaza, Park, Market, RailYard, ServiceYard, Garage, Speakeasy, Hotel, Theatre, Store, Casino, Bank, Diner, BilliardHall, DanceHall, Cinema, Tenement, Kitchen, Laundry, Restaurant, Arcade, Club, Registry, Bathhouse }
    public enum EntranceKind { Door, Window, RoofHatch, CellarHatch, Skylight, Balcony, Marquee, Chute, FireEscape, Open }

    /// A way into (and out of) an establishment: where it is, how high over the street, which way it faces, what it
    /// is. `tech`: reaching it from the street takes earned momentum (a window four metres up, a roof); leaving by it
    /// is always free (a drop lands on something)
    public class Entrance
    {
        public Block block;
        public string name;
        public Vector3 at, facing;
        public float height;
        public EntranceKind kind;
        public bool tech;
        /// False for a roof you can only reach from inside (no fire escape, no awning chain): a way OUT onto the
        /// street (a drop), never a way in from it
        public bool fromStreet = true;
        /// Which side of the block it opens on, in the world
        public Face side;
        public override string ToString() => block.Name + ": " + name;
    }

    /// A line through an establishment (recorded for the HUD and the lint; the floor carries the lit hairline)
    public struct Line { public string name; public Vector3 a, b; public bool fast; }

    /// One block of the grid: its bounds, what stands on it, how it is turned, its entrances and lines
    public class Block
    {
        public int bx, bz;
        public Establishment kind;
        /// 0..3: the front faces south (−z), west (−x), north (+z), east (+x)
        public int rotation;
        public Bounds bounds;
        public string name;
        public readonly List<Entrance> entrances = new List<Entrance>();
        public readonly List<Line> lines = new List<Line>();
        public Vector3 Centre => new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        public string Name => string.IsNullOrEmpty(name) ? kind.ToString().ToUpperInvariant() + " " + bx + "," + bz : name;
        public bool IsOpenGround => kind <= Establishment.Garage;
    }

    /// Where two gaps cross: `gx` along x (0..blocksX), `gz` along z (0..blocksZ). A plaza when both are streets
    public class Junction
    {
        public int gx, gz;
        public Vector3 at;
        public Bounds volume;
        public string name;
        public bool plaza;
        /// The way on from here: along the route, else toward the finish
        public Vector3 forward = Vector3.forward;
        public int routeIndex = -1;
    }

    public class DistrictPlan
    {
        public int seed, blocksX, blocksZ;
        /// The avenue the tram runs on (its gap index along x, −1 = none) and the rails' world x: respawns and furniture keep off it
        public int tramGx = -1; public float tramX;
        public float blockW, blockD, streetW, alleyW = 4f;
        /// The perimeter facades' height, and the street-level floor
        public float height = 26f, floorY, z0;
        /// Which inner gaps are alleys (index 1..blocks−1; the boundary gaps are always streets)
        public bool[] alleyX, alleyZ;
        public Block[,] blocks;
        public readonly List<Junction> junctions = new List<Junction>();
        public readonly List<Entrance> entrances = new List<Entrance>();
        /// The authored main route, junction by junction, south to north
        public readonly List<Junction> route = new List<Junction>();
        public string[] streetsX, streetsZ;   // the streets running along x (by gz) and along z (by gx)
        public float entryHalfW = 8f, exitHalfW = 8f;
        public Zone zone = Zone.Utopia;
        public bool built;

        public float HalfX { get { float w = 0f; for (int g = 0; g <= blocksX; g++) w += GapW(true, g); return (w + blocksX * blockW) * 0.5f; } }
        public float Depth { get { float d = 0f; for (int g = 0; g <= blocksZ; g++) d += GapW(false, g); return d + blocksZ * blockD; } }
        public float Z1 => z0 + Depth;
        public float GapW(bool alongX, int g)
        {
            var alley = alongX ? alleyX : alleyZ;
            int n = alongX ? blocksX : blocksZ;
            return g > 0 && g < n && alley != null && alley[g] ? alleyW : streetW;
        }
        /// Where gap `g` starts: the x of a gap along z (alongX = true: the gaps indexed along x), the z of one along x
        public float Gap0(bool alongX, int g)
        {
            float p = alongX ? -HalfX : z0, cell = alongX ? blockW : blockD;
            for (int i = 0; i < g; i++) p += GapW(alongX, i) + cell;
            return p;
        }
        public float GapMid(bool alongX, int g) => Gap0(alongX, g) + GapW(alongX, g) * 0.5f;
        /// Where block column `bx` (or row `bz`) starts
        public float Block0(bool alongX, int b) => Gap0(alongX, b) + GapW(alongX, b);
        public bool IsAlley(bool alongX, int g) => GapW(alongX, g) < streetW - 0.01f;
        public Junction JunctionAt(int gx, int gz) { foreach (var j in junctions) if (j.gx == gx && j.gz == gz) return j; return null; }
        public Block BlockAt(int bx, int bz) => bx >= 0 && bz >= 0 && bx < blocksX && bz < blocksZ ? blocks[bx, bz] : null;
    }

    /// Every plan built this level, for the lint
    public static readonly List<DistrictPlan> Built = new List<DistrictPlan>();

    static System.Random rng = new System.Random(1);
    static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
    static int RI(int n) => rng.Next(n);

    static readonly string[] AvenueNames = { "HALCYON AVENUE", "MERIDIAN AVENUE", "CROWN AVENUE", "REGENT AVENUE", "CONCORD AVENUE", "ASTOR AVENUE" };
    static readonly string[] StreetNames = { "WARD STREET", "FIRST STREET", "SECOND STREET", "THIRD STREET", "FOURTH STREET", "FIFTH STREET", "SIXTH STREET" };

    // ------------------------------------------------------------ the plan

    /// A grid of `blocksX` × `blocksZ` blocks, each `blockW` across (x) by `blockD` deep (z), streets `streetW` wide
    /// between them and round the edge, some inner gaps alleys (3–5 m, seeded). Every block starts as open ground;
    /// Place puts establishments on them. The grid is laid at the cursor by Build, centred on x = 0
    public static DistrictPlan Plan(int seed, int blocksX, int blocksZ, float blockW, float blockD, float streetW)
    {
        rng = new System.Random(seed);
        var plan = new DistrictPlan
        {
            seed = seed, blocksX = Mathf.Max(1, blocksX), blocksZ = Mathf.Max(1, blocksZ),
            blockW = Mathf.Max(18f, blockW), blockD = Mathf.Max(18f, blockD), streetW = Mathf.Max(14f, streetW),
            alleyW = Mathf.Clamp(3f + (float)rng.NextDouble() * 2f, 3f, 5f),
        };
        plan.alleyX = new bool[plan.blocksX + 1];
        plan.alleyZ = new bool[plan.blocksZ + 1];
        // Alleys: about a third of the inner gaps, never two side by side, never the gap a route most wants (the middle)
        for (int g = 1; g < plan.blocksX; g++) plan.alleyX[g] = rng.NextDouble() < 0.38 && !plan.alleyX[g - 1];
        for (int g = 1; g < plan.blocksZ; g++) plan.alleyZ[g] = rng.NextDouble() < 0.3 && !plan.alleyZ[g - 1];
        plan.blocks = new Block[plan.blocksX, plan.blocksZ];
        for (int bx = 0; bx < plan.blocksX; bx++)
            for (int bz = 0; bz < plan.blocksZ; bz++)
                plan.blocks[bx, bz] = new Block { bx = bx, bz = bz, kind = Establishment.Open, rotation = (bx + bz) % 4 };
        plan.streetsX = new string[plan.blocksZ + 1];
        plan.streetsZ = new string[plan.blocksX + 1];
        for (int g = 0; g <= plan.blocksZ; g++) plan.streetsX[g] = plan.IsAlley(false, g) ? "THE ALLEY" : StreetNames[g % StreetNames.Length];
        for (int g = 0; g <= plan.blocksX; g++) plan.streetsZ[g] = plan.IsAlley(true, g) ? "THE ALLEY" : AvenueNames[(g + seed) % AvenueNames.Length];
        return plan;
    }

    /// An alley instead of a street in inner gap `g` along x (true) or z (false), or a street instead of an alley
    public static void Alley(DistrictPlan plan, bool alongX, int g, bool alley = true)
    {
        int n = alongX ? plan.blocksX : plan.blocksZ;
        if (g <= 0 || g >= n) return;
        (alongX ? plan.alleyX : plan.alleyZ)[g] = alley;
        (alongX ? plan.streetsZ : plan.streetsX)[g] = alley ? "THE ALLEY" : alongX ? AvenueNames[(g + plan.seed) % AvenueNames.Length] : StreetNames[g % StreetNames.Length];
    }

    /// `kind` on block (bx, bz), its front turned `rotation` (0 south, 1 west, 2 north, 3 east), optionally named
    public static Block Place(DistrictPlan plan, int bx, int bz, Establishment kind, int rotation, string name = null)
    {
        var b = plan.BlockAt(bx, bz);
        if (b == null) { Debug.LogWarning("CityDistrict.Place: no block " + bx + "," + bz); return null; }
        b.kind = kind;
        b.rotation = ((rotation % 4) + 4) % 4;
        b.name = name;
        return b;
    }

    /// The grid's numbers, once the cursor says where it stands: block bounds, junctions, names
    internal static void Layout(DistrictPlan plan, float z0, float floorY)
    {
        plan.z0 = z0;
        plan.floorY = floorY;
        for (int bx = 0; bx < plan.blocksX; bx++)
            for (int bz = 0; bz < plan.blocksZ; bz++)
            {
                var b = plan.blocks[bx, bz];
                float x0 = plan.Block0(true, bx), zb = plan.Block0(false, bz);
                b.bounds = new Bounds(new Vector3(x0 + plan.blockW * 0.5f, floorY + 10f, zb + plan.blockD * 0.5f), new Vector3(plan.blockW, 20f, plan.blockD));
            }
        plan.junctions.Clear();
        for (int gx = 0; gx <= plan.blocksX; gx++)
            for (int gz = 0; gz <= plan.blocksZ; gz++)
            {
                float wx = plan.GapW(true, gx), wz = plan.GapW(false, gz);
                var j = new Junction
                {
                    gx = gx, gz = gz, at = new Vector3(plan.GapMid(true, gx), floorY, plan.GapMid(false, gz)),
                    plaza = !plan.IsAlley(true, gx) && !plan.IsAlley(false, gz),
                };
                j.volume = new Bounds(j.at + Vector3.up * 6f, new Vector3(wx + 2f, 14f, wz + 2f));
                j.name = plan.IsAlley(true, gx) && plan.IsAlley(false, gz) ? "THE BACK ALLEYS"
                    : plan.IsAlley(true, gx) ? plan.streetsX[gz] + " ALLEY" : plan.IsAlley(false, gz) ? plan.streetsZ[gx] + " ALLEY"
                    : plan.streetsZ[gx] + " & " + plan.streetsX[gz];
                plan.junctions.Add(j);
            }
    }

    // ------------------------------------------------------------ checkpoints and fuel

    /// A CHECKPOINT: a box in the world; entering it makes it the respawn, which sends you along `forward` (the route's
    /// direction there). The respawn stands you at `standAt` (the box's floor centre when none is given, on whatever
    /// the floor is there); a brass inlay marks the floor there
    public static void Checkpoint(Transform root, string name, Bounds volume, Vector3 forward, Vector3? standAt = null)
    {
        var last = LevelSections.Last;
        if (last == null) return;
        Vector3 at = standAt ?? new Vector3(volume.center.x, volume.min.y + 0.6f, volume.center.z);
        Vector3 from = new Vector3(at.x, volume.max.y, at.z);
        if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, volume.size.y + 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.point.y < volume.min.y + 5f) at.y = hit.point.y + 0.6f;
        last.checkpoints.Add(LevelSections.CheckpointMark.Volume(name, volume, at, forward));
        LevelKit.JunctionInlay(root, at + Vector3.down * 0.6f, Mathf.Min(volume.size.x, volume.size.z) * 0.3f, forward);
    }

    /// A checkpoint on a volume whose forward is the plan's at the nearest junction (the route's way on)
    public static void Checkpoint(Transform root, DistrictPlan plan, string name, Bounds volume)
        => Checkpoint(root, name, volume, ForwardAt(plan, volume.center));

    /// One checkpoint per junction, named by its streets (the HUD's segment names), each sending you along the route.
    /// The respawn stands at the junction's near edge (behind its fountain or kiosk), facing in along the route
    public static void Checkpoints(Transform root, DistrictPlan plan, bool alleysToo = false, bool routeOnly = false)
    {
        Physics.SyncTransforms();
        foreach (var j in plan.junctions)
        {
            if (!alleysToo && !j.plaza) continue;
            if (routeOnly && j.routeIndex < 0) continue;   // only the junctions the authored route passes: fewer HUD segment names in a quarter whose establishments are the checkpoints
            float back = Mathf.Min(j.volume.size.x, j.volume.size.z) * 0.5f - 3f;
            Vector3 stand = j.at - j.forward * Mathf.Max(0f, back) + Vector3.up * 0.6f;
            if (j.gx == plan.tramGx) stand.x = plan.tramX + 3.2f;   // never on the rails: a respawn on the tram's avenue stands beside them
            Checkpoint(root, j.name, j.volume, j.forward, stand);
        }
    }

    /// FUEL: a unit of `kind` at `at` on the line called `line` (the courses' Fuel; killing it is optional)
    public static void Fuel(Transform root, MovementConfig config, EnemyKind kind, Vector3 at, string line)
        => Course.Fuel(root, config, default(Cursor), kind, at, line);
}
