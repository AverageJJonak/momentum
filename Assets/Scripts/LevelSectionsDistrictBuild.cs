using System.Collections.Generic;
using UnityEngine;
using Cursor = LevelSections.Cursor;

/// <summary>
/// THE DISTRICT, built: the grid laid at the cursor (the south edge's street takes the lane in at x = 0, the north
/// edge's street gives the lane out at x = 0, both the plan's half widths), the perimeter facades (the walls that
/// keep the fabric out, dressed as the street fronts across the way: wall-run walls), the streets (setts, pavements
/// with kerbs, lamp standards as kick posts, hydrants), the alleys (dumpsters and crates as lips, washing lines),
/// the junction plazas (a fountain, a kiosk or a statue, benches), the establishments on every block, period traffic
/// on the avenues (CityTraffic: cars and walkers as a look) and a TRAM that hurts and throws on the longest avenue,
/// the open sky (CityVista raises the city round it), and the cursor on, open.
/// </summary>
public static partial class CityDistrict
{
    static readonly string[] ShopNames = { "TOBACCONIST", "PHARMACY", "BARBER", "TAILOR", "HALCYON RADIO", "DELICATESSEN", "FLORIST", "HABERDASHER", "WIRELESS & GRAMOPHONE", "CHOP HOUSE" };

    /// Lay the plan at the cursor. The district's floor is the cursor's; its lane in is the cursor's lane; the lane
    /// out is `plan.exitHalfW` wide at x = 0 on the north edge, open (the next section seals round it). Returns the
    /// cursor there. `zone` dresses the establishments (Utopia: the city's cream and terrazzo)
    public static Cursor Build(Transform root, MovementConfig config, Cursor c, DistrictPlan plan, Zone zone = Zone.Utopia)
    {
        plan.zone = zone;
        c.zone = zone;
        LevelKit.SetZone(zone);
        rng = new System.Random(unchecked(plan.seed * 7919 + Mathf.RoundToInt(c.z * 10f)));
        var last = LevelSections.Last;
        if (last != null && !last.spawnLocked && last.spawn == Vector3.zero && c.z < 0.01f)
        {
            last.spawn = new Vector3(0f, c.floorY + 1f, c.z + 6f);
            last.menuPortal = new Vector3(0f, c.floorY + 2f, c.z + 1.7f);
        }
        Layout(plan, c.z, c.floorY);
        plan.entryHalfW = c.halfW > 1f ? c.halfW : plan.entryHalfW;
        float halfX = plan.HalfX, z0 = plan.z0, z1 = plan.Z1, y = plan.floorY, H = plan.height;
        plan.entrances.Clear();
        foreach (var b in plan.blocks) { b.entrances.Clear(); b.lines.Clear(); }

        // The threshold: the south plane walled round the lane in, the arch over the lane; the east and west perimeter
        LevelSections.Seal(root, c, halfX, y, H);
        LevelSections.LandGlass(root, c);
        LevelKit.Arch(root, plan.entryHalfW, y, Course.Head, c.z + 1.3f);
        LevelKit.WallFace(root, Face.West, -halfX, z0 - T * 0.5f, z1 + T * 0.5f, y, y + H);
        LevelKit.WallFace(root, Face.East, halfX, z0 - T * 0.5f, z1 + T * 0.5f, y, y + H);
        LevelKit.BeginFacades();
        Perimeter(root, plan, halfX, z0, z1, y, H);

        // The ground: the streets along x (full width) and along z (between the rows), paved; the pavements and kerbs
        Streets(root, plan, halfX, y);
        // The blocks
        for (int bx = 0; bx < plan.blocksX; bx++)
            for (int bz = 0; bz < plan.blocksZ; bz++)
                Establishments.Build(root, config, plan, plan.blocks[bx, bz]);
        Physics.SyncTransforms();
        // The streets' furniture, the alleys', the plazas'; then the looks are welded
        Furniture(root, config, plan, halfX, y);
        LevelKit.FlushFacades(root);
        Traffic(root, plan, halfX, y);

        // The sky: open over the whole district (the city is raised round it), the air in it
        LevelSections.See(-1, z0, z1, y + H, y + H + 220f);
        LevelSections.See(1, z0, z1, y + H, y + H + 220f);
        LevelKit.Air(root, zone, new Vector3(-halfX, y, z0), new Vector3(halfX, y + H, z1), true);
        LevelKit.Clouds(root, new Vector3(0f, 0f, (z0 + z1) * 0.5f), halfX * 1.6f, (z1 - z0) * 0.7f, y + H + 30f, 7);
        if (plan.route.Count == 0) Route(plan, new Vector2Int(NearestGap(plan, 0f), 0), new Vector2Int(NearestGap(plan, 0f), plan.blocksZ));
        LevelSections.Postcard(new Vector3(-halfX * 0.3f, y + 14f, z0 + 4f), new Vector3(halfX * 0.1f, y + 2f, z0 + (z1 - z0) * 0.5f), 78f, false);   // a PLAIN claim: the level's own signature postcard (before or after Build, the Atrium's at the end of Level 5) wins over it
        plan.built = true;
        if (!Built.Contains(plan)) Built.Add(plan);
        // The way on: the lane out at x = 0 on the north edge; the next section walls the rest of the plane
        var next = LevelSections.Exit(c, z1, y, halfX, y, y + H);
        next.open = true;
        next.halfW = plan.exitHalfW;
        next.wallX0 = -halfX - T * 0.5f; next.wallX1 = halfX + T * 0.5f;
        return next;
    }

    const float T = LevelKit.T;

    static int NearestGap(DistrictPlan plan, float x)
    {
        int best = 0; float bd = float.MaxValue;
        for (int g = 0; g <= plan.blocksX; g++) { float d = Mathf.Abs(plan.GapMid(true, g) - x); if (d < bd) { bd = d; best = g; } }
        return best;
    }

    /// The perimeter walls' inner faces dressed as the street fronts across the way: shop bases, punched windows,
    /// string courses, cornices; the north face has the exit's arch drawn round the lane out
    static void Perimeter(Transform root, DistrictPlan plan, float halfX, float z0, float z1, float y, float H)
    {
        int floors = Mathf.Max(1, Mathf.FloorToInt((H - 4.4f) / 3.9f));
        float depth = z1 - z0;
        float face = halfX - T * 0.5f;   // the walls stand centred on the planes: their inner faces half a wall in
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 inward = Vector3.left * s;
            int bays = Mathf.Max(1, Mathf.RoundToInt(depth / 26f));
            float bayW = depth / bays;
            for (int i = 0; i < bays; i++)
            {
                Vector3 c = new Vector3(s * face, y, z0 + bayW * (i + 0.5f));
                int fl = floors - (i % 3 == 1 ? 1 : 0);
                LevelKit.PunchedWindows(c, inward, bayW - 3f, 4.4f, fl, (H - 4.4f) / floors, plan.seed + i * 3 + s);
                LevelKit.StringCourse(c, inward, bayW - 1f, 4.2f, 0.5f);
                LevelKit.Cornice(c, inward, bayW - 1f, 4.4f + fl * (H - 4.4f) / floors);
            }
            for (int i = 0; i < bays; i += 2) LevelKit.Shopfront(root, new Vector3(s * face, y, z0 + bayW * (i + 0.5f)), inward, Mathf.Min(bayW - 6f, 14f), ShopNames[(i + s + 7) % ShopNames.Length]);
        }
        // The north face (the next section's seal stands there): windows either side of the lane out, the arch's piers
        float w = halfX - plan.exitHalfW - 2f;
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 c = new Vector3(s * (plan.exitHalfW + 2f + w * 0.5f), y, z1 - T * 0.5f);
            LevelKit.PunchedWindows(c, Vector3.back, w - 2f, 4.4f, floors, (H - 4.4f) / floors, plan.seed + 50 + s);
            LevelKit.Cornice(c, Vector3.back, w, H);
        }
        LevelKit.Arch(root, plan.exitHalfW, y, Course.Head, z1 - 1.3f);
    }

    /// The streets: setts down every gap, pavements 2.5 m wide with a kerb along every block face on a street (none
    /// in the alleys), the plazas paved in flags
    static void Streets(Transform root, DistrictPlan plan, float halfX, float y)
    {
        for (int gz = 0; gz <= plan.blocksZ; gz++)
        {
            float za = plan.Gap0(false, gz), zb = za + plan.GapW(false, gz);
            LevelKit.Slab(root, new Vector3(0f, y - 0.5f, (za + zb) * 0.5f), new Vector3(halfX * 2f, 1f, zb - za), plan.IsAlley(false, gz) ? LevelKit.Charcoal : LevelKit.Setts, "Street");
        }
        for (int gx = 0; gx <= plan.blocksX; gx++)
            for (int bz = 0; bz < plan.blocksZ; bz++)
            {
                float xa = plan.Gap0(true, gx), xb = xa + plan.GapW(true, gx), za = plan.Block0(false, bz), zb = za + plan.blockD;
                LevelKit.Slab(root, new Vector3((xa + xb) * 0.5f, y - 0.5f, (za + zb) * 0.5f), new Vector3(xb - xa, 1f, zb - za), plan.IsAlley(true, gx) ? LevelKit.Charcoal : LevelKit.Setts, "Street");
            }
        // Pavements: along every block's street-facing sides, and along the perimeter walls
        const float pw = 2.5f;
        foreach (var b in plan.blocks)
        {
            var bb = b.bounds;
            if (!plan.IsAlley(false, b.bz)) LevelKit.PavementSlab(root, bb.min.x - pw, bb.max.x + pw, bb.min.z - pw, bb.min.z, y);
            if (!plan.IsAlley(false, b.bz + 1)) LevelKit.PavementSlab(root, bb.min.x - pw, bb.max.x + pw, bb.max.z, bb.max.z + pw, y);
            if (!plan.IsAlley(true, b.bx)) LevelKit.PavementSlab(root, bb.min.x - pw, bb.min.x, bb.min.z, bb.max.z, y);
            if (!plan.IsAlley(true, b.bx + 1)) LevelKit.PavementSlab(root, bb.max.x, bb.max.x + pw, bb.min.z, bb.max.z, y);
        }
        LevelKit.PavementSlab(root, -halfX, -halfX + pw, plan.z0, plan.Z1, y);
        LevelKit.PavementSlab(root, halfX - pw, halfX, plan.z0, plan.Z1, y);
    }

    /// Lamp standards along the avenues (kick posts, lit every other one), hydrants, the plazas' fountains, kiosks and
    /// benches, the alleys' dumpsters, crates and washing; the junctions' inlays come with their checkpoints
    static void Furniture(Transform root, MovementConfig config, DistrictPlan plan, float halfX, float y)
    {
        int n = 0;
        foreach (var b in plan.blocks)
        {
            var bb = b.bounds;
            for (int side = 0; side < 4; side++)
            {
                bool alongX = side < 2;
                int g = alongX ? (side == 0 ? b.bz : b.bz + 1) : (side == 2 ? b.bx : b.bx + 1);
                bool alley = plan.IsAlley(!alongX, g);
                float edge = side == 0 ? bb.min.z : side == 1 ? bb.max.z : side == 2 ? bb.min.x : bb.max.x;
                float sign = side == 0 || side == 2 ? -1f : 1f;
                if (!alley)
                {
                    float len = alongX ? bb.size.x : bb.size.z, start = alongX ? bb.min.x : bb.min.z;
                    for (float t = 6f; t < len - 5f; t += 12f)
                    {
                        Vector3 foot = alongX ? new Vector3(start + t, y + 0.12f, edge + sign * 1.6f) : new Vector3(edge + sign * 1.6f, y + 0.12f, start + t);
                        if (Physics.CheckBox(foot + Vector3.up * 3f, new Vector3(0.6f, 2.8f, 0.6f), Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                        LevelKit.LampStandard(root, foot, 6f, (n++ % 2) == 0);
                        if (n % 3 == 0) LevelKit.Hydrant(root, foot + (alongX ? Vector3.right : Vector3.forward) * 3f);
                    }
                }
                else if (side == 2 || side == 0)
                {
                    // An alley on this side: lips along it and the washing over it (on the block's low side only, once)
                    float len = alongX ? bb.size.x : bb.size.z, start = alongX ? bb.min.x : bb.min.z;
                    float gapW = plan.GapW(!alongX, g), mid = edge + sign * gapW * 0.5f;
                    for (float t = 5f; t < len - 4f; t += 11f)
                    {
                        Vector3 foot = alongX ? new Vector3(start + t, y, edge + sign * 1.1f) : new Vector3(edge + sign * 1.1f, y, start + t);
                        if (Physics.CheckBox(foot + Vector3.up * 1f, new Vector3(1.4f, 0.9f, 1.4f), Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                        if ((n++ % 2) == 0) LevelKit.Dumpster(root, foot, alongX ? 0f : 90f);
                        else LevelKit.Crates(root, foot, alongX ? 0f : 90f);
                        Vector3 a = alongX ? new Vector3(start + t + 3f, y + 4.5f, edge) : new Vector3(edge, y + 4.5f, start + t + 3f);
                        LevelKit.WashingLine(root, a, a + (alongX ? Vector3.forward : Vector3.right) * (sign * gapW), 3);
                        Physics.SyncTransforms();
                    }
                }
            }
        }
        // The plazas: where two streets cross inside the grid, a fountain or a kiosk and benches; the edge junctions lamps
        foreach (var j in plan.junctions)
        {
            if (!j.plaza) continue;
            bool inner = j.gx > 0 && j.gx < plan.blocksX && j.gz > 0 && j.gz < plan.blocksZ;
            float wx = plan.GapW(true, j.gx), wz = plan.GapW(false, j.gz);
            if (inner && (j.gx + j.gz) % 2 == 0 && Mathf.Min(wx, wz) >= 18f)
            {
                LevelKit.Fountain(root, j.at, Mathf.Min(wx, wz) * 0.4f);
                Fuel(root, config, EnemyKind.Chaser, j.at + Vector3.up * 2.3f, j.name + " fountain");
            }
            else if (inner && Mathf.Min(wx, wz) >= 16f)
            {
                LevelKit.Kiosk(root, j.at + new Vector3(wx * 0.25f, 0f, -wz * 0.25f), Vector3.back);
                LevelKit.Bench(root, j.at + new Vector3(-wx * 0.25f, 0f, wz * 0.25f), 90f);
                PoolFuel(config, j.at + new Vector3(-wx * 0.2f, 0f, -wz * 0.2f), j.name);
            }
            else if (!inner) LevelKit.Bench(root, j.at + new Vector3(0f, 0f, j.gz == 0 ? -wz * 0.3f : wz * 0.3f), 0f);
        }
    }

    static void PoolFuel(MovementConfig config, Vector3 at, string line)
    {
        var e = EnemyPool.Ground(at, config);
        if (e != null) LevelSections.Last?.fuel.Add(new LevelSections.FuelMark { unit = e, line = line });
    }

    /// Period traffic on the avenues (CityTraffic: cars both ways, walkers on the pavements — a look, no collider)
    /// and a TRAM up and down the longest avenue along z (LevelKit.Tram: contact hurts and throws, a pass is a near miss)
    static void Traffic(Transform root, DistrictPlan plan, float halfX, float y)
    {
        if (CityVista.Preview) return;
        var traffic = CityTraffic.Create(root, false);
        int seed = plan.seed;
        for (int gx = 0; gx <= plan.blocksX; gx++)
        {
            if (plan.IsAlley(true, gx)) continue;
            float x = plan.GapMid(true, gx), w = plan.GapW(true, gx);
            traffic.Avenue(new Vector3(x, y, plan.z0 + 6f), new Vector3(x, y, plan.Z1 - 6f), 8f, 0.6f, 2.5f, seed++, w * 0.5f - 3.5f, w * 0.5f - 1.4f);
        }
        for (int gz = 0; gz <= plan.blocksZ; gz++)
        {
            if (plan.IsAlley(false, gz)) continue;
            float z = plan.GapMid(false, gz), w = plan.GapW(false, gz);
            traffic.Avenue(new Vector3(-halfX + 6f, y, z), new Vector3(halfX - 6f, y, z), 7f, 0.5f, 2.5f, seed++, w * 0.5f - 3.5f, w * 0.5f - 1.4f);
        }
        traffic.Finish();
        // The tram: the avenue along z nearest the middle (never an alley), rails baked, two cars
        int best = -1; float bd = float.MaxValue;
        for (int g = 0; g <= plan.blocksX; g++) { float d = Mathf.Abs(plan.GapMid(true, g)); if (!plan.IsAlley(true, g) && d < bd) { bd = d; best = g; } }
        if (best < 0) return;
        float tx = plan.GapMid(true, best);
        plan.tramGx = best; plan.tramX = tx;
        LevelKit.Tram(root, new Vector3(tx, y, plan.z0 + 4f), new Vector3(tx, y, plan.Z1 - 4f), 9f, 2, 6f, plan.seed);
    }
}
