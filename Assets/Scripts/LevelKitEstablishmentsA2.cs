using UnityEngine;
using Plan = CityDistrict.DistrictPlan;
using Block = CityDistrict.Block;
using Kind = CityDistrict.Establishment;
using EK = CityDistrict.EntranceKind;

/// <summary>
/// THE ESTABLISHMENTS: the TENEMENT (stairs, a light well, a fire escape, the roof) and OPEN GROUND (a plaza, a park,
/// a market, a rail yard, a garage, a bare lot: no walls, enterable from every side).
/// </summary>
public static partial class Establishments
{
    /// TENEMENT. Four storeys round a LIGHT WELL (a 6 × 6 void from the ground to the sky through every floor, its
    /// edges parapet lips, a pad at its foot that throws you back to the second floor), the STAIR a switchback of 18°
    /// flights up the east wall with a ledge lip at every turn, the FIRE ESCAPE down the west face into the alley
    /// (its second landing a window into the second floor, its top landing a step onto the roof), the roof with its
    /// stair head, three chimneys, a water tower and the washing. MAIN: the stair, floor to floor. FAST: wall-kick up
    /// the well (its four walls are kick walls), drop it, the fire escape's landings as a lip ladder.
    /// FUEL: a footman on every landing, a page in the well, a doorman at the street door, a bellhop at the window.
    static void Tenement(Transform root, MovementConfig config, Plan plan, Block b, BlockFrame f)
    {
        const float storey = 4f; const int floors = 4;
        float h = storey * floors, hw = f.hw, hd = f.hd, well = 3f, escape = storey - 0.1f;
        Vector3 min = new Vector3(-hw, 0f, -hd), max = new Vector3(hw, h, hd);
        // The stair: two lanes of flights side by side along the east wall, the stairwell cut from every gallery
        float legW = 3f, run = storey / Mathf.Tan(18f * Mathf.Deg2Rad), z0 = -hd + 3f, z1 = Mathf.Min(hd - 3f, z0 + run), wellX = hw - 2f * legW - 1f;
        Vector2 hatch = new Vector2(hw - legW * 0.5f, z0 - 1.5f);
        Shell(root, f, h, hatch,
            Door(f, Face.South, 0f, 0f, min, max), Door(f, Face.North, 0f, 0f, min, max),
            Window(f, Face.West, 0f, 3.4f, escape * 2f + 0.2f, escape * 2f + 3f, min, max));
        // The floors: four slabs a storey round the well, a parapet lip along each well edge, the well's walls fluted
        for (int k = 1; k < floors; k++)
        {
            float y = k * storey;
            Floor(root, f, new Vector3((-hw - well) * 0.5f, y - 0.5f, 0f), new Vector3(hw - well, 1f, hd * 2f), "Gallery");
            Floor(root, f, new Vector3((wellX + well) * 0.5f, y - 0.5f, 0f), new Vector3(wellX - well, 1f, hd * 2f), "Gallery");
            if (hd - (z1 + 3f) > 0.3f) Floor(root, f, new Vector3((wellX + hw) * 0.5f, y - 0.5f, (z1 + 3f + hd) * 0.5f), new Vector3(hw - wellX, 1f, hd - z1 - 3f), "Gallery");
            Floor(root, f, new Vector3(0f, y - 0.5f, (-hd - well) * 0.5f), new Vector3(well * 2f, 1f, hd - well), "Gallery");
            Floor(root, f, new Vector3(0f, y - 0.5f, (hd + well) * 0.5f), new Vector3(well * 2f, 1f, hd - well), "Gallery");
            LevelKit.Parapet(root, f.P(-well - 0.35f, y + 0.45f, 0f), f.S(0.7f, 0.9f, well * 2f + 1.4f));
            LevelKit.Parapet(root, f.P(well + 0.35f, y + 0.45f, 0f), f.S(0.7f, 0.9f, well * 2f + 1.4f));
            LevelKit.Parapet(root, f.P(0f, y + 0.45f, -well - 0.35f), f.S(well * 2f, 0.9f, 0.7f));
            LevelKit.Parapet(root, f.P(0f, y + 0.45f, well + 0.35f), f.S(well * 2f, 0.9f, 0.7f));
            Fuel(root, config, f, EnemyKind.Dummy, new Vector3(-hw + 3f, y, -hd * 0.5f + k * 4f), "floor " + k);
            Lamp(root, f, new Vector3(-hw * 0.5f, y + storey - 1f, hd * 0.5f), y + storey);
        }
        // The well's floor pad, the page in it; the well's own walls are the floors' edges (flutes mark the kicks)
        Pad(root, f, new Vector3(0f, 0f, 0f), Mathf.Sqrt(2f * 25f * (storey * 2f + 1.2f)), 2f);
        Fuel(root, config, f, EnemyKind.Turret, new Vector3(0f, storey * 2.5f, 0f), "the well");
        for (int k = 1; k < floors; k++) for (int s = -1; s <= 1; s += 2) Flute(root, f, new Vector3(s * (well + 0.6f), k * storey + 2f, 0f), Vector3.left * s, 2.6f, 0.4f);
        // The flights: a storey each over Run(storey) at 18°, the even ones north up the wall lane, the odd ones south
        // up the lane beside it, a ledge lip across both lanes at every turn (the last one under the hatch)
        float ledgeW = legW * 2f + 1f, ledgeX = hw - ledgeW * 0.5f;
        for (int k = 0; k < floors; k++)
        {
            float ya = k * storey, yb = ya + storey;
            bool north = k % 2 == 0;
            float legX = north ? hw - legW * 0.5f : hw - legW * 1.5f - 1f;
            if (north) Ramp(root, f, legX, legW, z0, ya, z1, yb, LevelKit.Floor, true);
            else Ramp(root, f, legX, legW, z0, yb, z1, ya, LevelKit.Floor, true);
            Lip(root, f, new Vector3(ledgeX, yb - 0.5f, north ? z1 + 1.5f : z0 - 1.5f), new Vector3(ledgeW, 1f, 3f), LevelKit.Floor, "Ledge");
            Banister(root, f, hw - legW - 0.5f, z0, north ? ya : yb, z1, north ? yb : ya);
        }
        Line(root, b, f, "the stair", new Vector3(hw - legW * 0.5f, 0f, -hd + 1f), new Vector3(hw - legW * 0.5f, 0f, z0 - 0.3f), plan.zone, false);
        Line(root, b, f, "up the well", new Vector3(0f, 0f, -hd + 1f), new Vector3(0f, 0f, -well - 1f), plan.zone);
        Fuel(root, config, f, EnemyKind.Sentry, new Vector3(2.5f, 0f, -hd + 3f), "the street door");
        Fuel(root, config, f, EnemyKind.Chaser, new Vector3(-hw + 3f, storey * 2f, 0f), "the window");
        Roof(root, f, h, hatch, 3, true);
        LevelKit.WashingLine(root, f.P(-hw + 4f, h + 3.2f, hd * 0.55f), f.P(-hw + 11f, h + 3.2f, hd * 0.55f), 4);
        Facades(root, b, f, h, floors - 1, null, null);
        LevelKit.FireEscape(root, f.P(-hw - 0.5f, 0f, 0f), f.D(Vector3.left), floors, escape);   // its top landing 0.4 m under the roof deck's edge: a jump onto the parapet
        Entrance(plan, b, f, "the street door", new Vector3(0f, 0f, -hd), Vector3.back, EK.Door);
        Entrance(plan, b, f, "the yard door", new Vector3(0f, 0f, hd), Vector3.forward, EK.Door);
        Entrance(plan, b, f, "the fire escape", new Vector3(-hw, escape * 2f, 0f), Vector3.left, EK.FireEscape, true);
        Entrance(plan, b, f, "the roof", new Vector3(-hw, h + 1f, 0f), Vector3.left, EK.RoofHatch, true);
        if (string.IsNullOrEmpty(b.name)) b.name = "THE TENEMENT";
    }

    /// OPEN GROUND: no walls, paved, enterable from every side. A PLAZA (a fountain in the middle, lamp standards
    /// at the corners, a kiosk, benches), a PARK (lawns, trees, a bandstand plinth, hedges as lips), a MARKET (rows
    /// of stall counters under awnings: lips in rows, a cold store box with a roof lip), a RAIL YARD (rails inlaid,
    /// wagons as lips and kick walls, a buffer stop), a GARAGE (a ramp to a first-floor deck: a slide down, a pad up),
    /// or a bare LOT (crates, a hoarding, a kick wall). Every one has a line through it and fuel on it
    static void OpenGround(Transform root, MovementConfig config, Plan plan, Block b, BlockFrame f)
    {
        float hw = f.blockHW, hd = f.blockHD;
        LevelKit.Slab(root, f.P(0f, -0.5f, 0f), f.S(hw * 2f, 1f, hd * 2f), LevelKit.Pavement, "BlockFloor");
        var zone = plan.zone;
        switch (b.kind)
        {
            case Kind.Plaza:
                LevelKit.Fountain(root, f.P(0f, 0f, 0f), Mathf.Min(10f, hw * 0.5f));
                for (int sx = -1; sx <= 1; sx += 2) for (int sz = -1; sz <= 1; sz += 2) LevelKit.LampStandard(root, f.P(sx * (hw - 2.5f), 0f, sz * (hd - 2.5f)), 6f, sx == sz);
                LevelKit.Kiosk(root, f.P(-hw * 0.55f, 0f, hd * 0.5f), f.D(Vector3.back));
                for (int i = 0; i < 3; i++) LevelKit.Bench(root, f.P(hw * 0.5f, 0f, -hd * 0.6f + i * hd * 0.6f), f.Yaw + 90f);
                Fuel(root, config, f, EnemyKind.Chaser, new Vector3(0f, 2.3f, -hd * 0.4f), "the fountain's rim");
                break;
            case Kind.Park:
                LevelKit.Lawn(root, f.P(-hw * 0.5f, 0f, 0f), new Vector2(hw * 0.8f, hd * 1.6f));
                LevelKit.Lawn(root, f.P(hw * 0.5f, 0f, 0f), new Vector2(hw * 0.8f, hd * 1.6f));
                for (int i = 0; i < 6; i++) LevelKit.Tree(root, f.P((i % 2 == 0 ? -1f : 1f) * hw * 0.55f, 0f, -hd * 0.7f + (i / 2) * hd * 0.7f), R(5f, 7f), i % 3 != 1);
                LevelKit.SteppedPlinth(root, f.P(0f, 0f, 0f), 7f, 2, 1.1f);
                LevelKit.Hedge(root, f.P(0f, 0f, -hd * 0.75f), f.S(hw * 1.2f, 0.9f, 0.8f));
                Fuel(root, config, f, EnemyKind.Chaser, new Vector3(0f, 2.2f, 0f), "the bandstand");
                break;
            case Kind.Market:
                for (int r = 0; r < 3; r++)
                    for (int c = 0; c < 2; c++)
                    {
                        Vector3 at = new Vector3(-hw * 0.5f + c * hw, 0f, -hd * 0.6f + r * hd * 0.6f);
                        Lip(root, f, at + Vector3.up * 0.55f, new Vector3(hw * 0.7f, 1.1f, 1.6f), LevelKit.WoodPanel, "Stall");
                        var awn = LevelKit.Awning(root, f.P(at + Vector3.forward * 1.2f), f.D(Vector3.back), hw * 0.7f, 2.9f, 2.8f);
                        awn.name = "StallAwning";
                    }
                Kick(root, f, new Vector3(0f, 2f, hd - 4f), new Vector3(6f, 4f, 4f), LevelKit.RivetIron);
                Lip(root, f, new Vector3(0f, 4.3f, hd - 4f), new Vector3(6.4f, 0.6f, 4.4f), LevelKit.Limestone, "ColdStoreRoof");
                Fuel(root, config, f, EnemyKind.Dummy, new Vector3(-hw * 0.5f, 0f, 0f), "the stalls");
                Fuel(root, config, f, EnemyKind.Dummy, new Vector3(hw * 0.5f, 0f, 0f), "the stalls");
                break;
            case Kind.RailYard:
                for (int t = 0; t < 3; t++)
                {
                    float x = -hw * 0.6f + t * hw * 0.6f;
                    LevelKit.BrassStrip(root, f.P(x - 0.72f, 0f, -hd + 1f), f.P(x - 0.72f, 0f, hd - 1f), 0.12f);
                    LevelKit.BrassStrip(root, f.P(x + 0.72f, 0f, -hd + 1f), f.P(x + 0.72f, 0f, hd - 1f), 0.12f);
                    if (t < 2) Lip(root, f, new Vector3(x, 0.9f, -hd * 0.4f + t * hd * 0.8f), new Vector3(2.6f, 1.8f, 9f), LevelKit.RivetIron, "Wagon");
                    else Kick(root, f, new Vector3(x, 1.75f, 0f), new Vector3(2.6f, 3.5f, 12f), LevelKit.Oxblood);
                }
                Lip(root, f, new Vector3(0f, 0.5f, hd - 1.5f), new Vector3(hw * 1.6f, 1f, 1f), LevelKit.RivetIron, "BufferStop");
                Fuel(root, config, f, EnemyKind.Sentry, new Vector3(0f, 0f, 0f), "between the tracks");
                break;
            case Kind.Garage:
                Floor(root, f, new Vector3(hw * 0.25f, 4.5f, 0f), new Vector3(hw * 1.5f, 1f, hd * 2f), "Deck");
                for (int i = 0; i < 4; i++) Corner(root, f, new Vector3(hw * 0.25f + (i % 2 == 0 ? -1f : 1f) * hw * 0.5f, 2f, (i / 2 == 0 ? -1f : 1f) * hd * 0.5f), new Vector3(0.8f, 4f, 0.8f), LevelKit.Charcoal);
                Ramp(root, f, -hw * 0.75f + 2.5f, 5f, -hd + 1f, 0f, hd - 1f, 5f, LevelKit.Slope);
                LevelKit.Parapet(root, f.P(hw * 0.25f, 5.45f, -hd + 0.4f), f.S(hw * 1.5f, 0.9f, 0.7f));
                Pad(root, f, new Vector3(hw * 0.5f, 0f, -hd + 3f), 13f, 3f);
                Fuel(root, config, f, EnemyKind.Dummy, new Vector3(hw * 0.25f, 5f, 0f), "the deck");
                break;
            default:
                LevelKit.Crates(root, f.P(-hw * 0.6f, 0f, hd * 0.3f), f.Yaw);
                Kick(root, f, new Vector3(0f, 2f, hd * 0.7f), new Vector3(8f, 4f, 0.5f), LevelKit.Brick);
                LevelKit.Dumpster(root, f.P(hw * 0.6f, 0f, -hd * 0.3f), f.Yaw);
                Fuel(root, config, f, EnemyKind.Chaser, new Vector3(0f, 0f, 0f), "the lot");
                break;
        }
        Line(root, b, f, "across", new Vector3(0f, 0f, -hd + 1f), new Vector3(0f, 0f, -hd * 0.3f), zone, false);
        Entrance(plan, b, f, "south side", new Vector3(0f, 0f, -hd), Vector3.back, EK.Open);
        Entrance(plan, b, f, "north side", new Vector3(0f, 0f, hd), Vector3.forward, EK.Open);
        Entrance(plan, b, f, "west side", new Vector3(-hw, 0f, 0f), Vector3.left, EK.Open);
        Entrance(plan, b, f, "east side", new Vector3(hw, 0f, 0f), Vector3.right, EK.Open);
        if (string.IsNullOrEmpty(b.name)) b.name = "THE " + b.kind.ToString().ToUpperInvariant();
    }
}
