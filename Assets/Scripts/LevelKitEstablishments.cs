using UnityEngine;
using Plan = CityDistrict.DistrictPlan;
using Block = CityDistrict.Block;
using Kind = CityDistrict.Establishment;
using EK = CityDistrict.EntranceKind;

/// <summary>
/// THE ESTABLISHMENTS, the dispatcher and the pieces they share: the block's floor, the SHELL (walls and a roof deck
/// with a hatch), the FACADES (shopfronts under awnings on the street faces, punched windows above, string courses,
/// a stepped cornice), the ROOF (parapet lips, a stair head that is a kick wall, chimneys that are kick stacks, a
/// water tower), the generic HALL (what the casino, the billiard hall, the dance hall and the cinema are until they
/// are drawn apart) and OPEN GROUND (a plaza, a park, a market, a rail yard, a garage, a bare lot).
/// Each builder: (root, config, plan, block) → records its entrances and lines on the block.
/// </summary>
public static partial class Establishments
{
    /// Build whatever stands on `block`. Every block ends with a floor at street level, two entrances at least, and
    /// its facades dressed; an open-ground block is enterable from every side
    public static void Build(Transform root, MovementConfig config, Plan plan, Block block)
    {
        rng = new System.Random(unchecked(plan.seed * 131 + block.bx * 17 + block.bz * 29));
        LevelKit.SetZone(plan.zone);
        var f = FrameFor(block);
        if (!block.IsOpenGround && !Digs(block.kind))   // a kind that digs lays its own floor in pieces round its cellar or pools
            Floor(root, f, new Vector3(0f, -0.5f, 0f), new Vector3(f.blockHW * 2f, 1f, f.blockHD * 2f), "BlockFloor");
        switch (block.kind)
        {
            case Kind.Speakeasy: Speakeasy(root, config, plan, block, f); break;
            case Kind.Diner: Diner(root, config, plan, block, f); break;
            case Kind.Tenement: Tenement(root, config, plan, block, f); break;
            case Kind.Hotel: Hotel(root, config, plan, block, f); break;
            case Kind.Theatre: Theatre(root, config, plan, block, f); break;
            case Kind.Store: Store(root, config, plan, block, f); break;
            case Kind.Bank: Bank(root, config, plan, block, f); break;
            case Kind.Casino: Casino(root, config, plan, block, f); break;
            case Kind.BilliardHall: BilliardHall(root, config, plan, block, f); break;
            case Kind.DanceHall: DanceHall(root, config, plan, block, f); break;
            case Kind.Cinema: Cinema(root, config, plan, block, f); break;
            case Kind.Kitchen: Kitchen(root, config, plan, block, f); break;
            case Kind.Laundry: Laundry(root, config, plan, block, f); break;
            case Kind.ServiceYard: ServiceYard(root, config, plan, block, f); break;
            case Kind.Restaurant: Restaurant(root, config, plan, block, f); break;
            case Kind.Arcade: Arcade(root, config, plan, block, f); break;
            case Kind.Club: Club(root, config, plan, block, f); break;
            case Kind.Registry: Registry(root, config, plan, block, f); break;
            case Kind.Bathhouse: Bathhouse(root, config, plan, block, f); break;
            default: OpenGround(root, config, plan, block, f); break;
        }
        if (block.entrances.Count < 2 && !block.IsOpenGround)
            Debug.LogWarning("Establishments: " + block.Name + " has " + block.entrances.Count + " entrance(s)");
    }

    /// The kinds that dig under the street (a cellar, pools): the dispatcher lays them no block floor, they lay their own
    /// in pieces round the opening. Add a kind here rather than taking the dispatcher's floor back up
    public static bool Digs(Kind k) => k == Kind.Speakeasy || k == Kind.Bathhouse || k == Kind.Restaurant || k == Kind.BilliardHall;

    // ------------------------------------------------------------ the shell, the facades, the roof

    /// The walls of a building `h` tall with `holes` in its local faces, and its ROOF DECK (top at h + 1) with a hatch
    /// opening 3 × 3 at local `hatch` (x, z). The interior floor is the block floor at street level
    static void Shell(Transform root, BlockFrame f, float h, Vector2 hatch, params Hole[] holes)
    {
        Vector3 min = new Vector3(-f.hw, 0f, -f.hd), max = new Vector3(f.hw, h, f.hd);
        Room(root, f, min, max, LevelKit.Faces.North | LevelKit.Faces.South | LevelKit.Faces.East | LevelKit.Faces.West, holes);
        RoofDeck(root, f, h, hatch);
    }

    /// The roof slab, a metre thick over the walls' top, with the hatch cut out of it (four slabs round the opening)
    static void RoofDeck(Transform root, BlockFrame f, float h, Vector2 hatch)
    {
        float hw = f.hw + T, hd = f.hd + T, hx0 = hatch.x - 1.5f, hx1 = hatch.x + 1.5f, hz0 = hatch.y - 1.5f, hz1 = hatch.y + 1.5f, y = h + 0.5f;
        LevelKit.Slab(root, f.P((-hw + hx0) * 0.5f, y, 0f), f.S(hx0 + hw, 1f, hd * 2f), LevelKit.Charcoal, "RoofDeck");
        LevelKit.Slab(root, f.P((hx1 + hw) * 0.5f, y, 0f), f.S(hw - hx1, 1f, hd * 2f), LevelKit.Charcoal, "RoofDeck");
        LevelKit.Slab(root, f.P((hx0 + hx1) * 0.5f, y, (-hd + hz0) * 0.5f), f.S(hx1 - hx0, 1f, hz0 + hd), LevelKit.Charcoal, "RoofDeck");
        LevelKit.Slab(root, f.P((hx0 + hx1) * 0.5f, y, (hz1 + hd) * 0.5f), f.S(hx1 - hx0, 1f, hd - hz1), LevelKit.Charcoal, "RoofDeck");
    }

    /// The ROOF's furniture: parapet lips along every edge, a stair head (a kick wall) beside the hatch, kick stacks,
    /// a water tower (a look on a solid core), a pigeon loft. The deck top is at h + 1
    static void Roof(Transform root, BlockFrame f, float h, Vector2 hatch, int stacks = 2, bool tower = true)
    {
        float y = h + 1f, hw = f.hw + T, hd = f.hd + T;
        LevelKit.Parapet(root, f.P(0f, y + 0.45f, -hd + 0.35f), f.S(hw * 2f - 1.6f, 0.9f, 0.7f));
        LevelKit.Parapet(root, f.P(0f, y + 0.45f, hd - 0.35f), f.S(hw * 2f - 1.6f, 0.9f, 0.7f));
        LevelKit.Parapet(root, f.P(-hw + 0.35f, y + 0.45f, 0f), f.S(0.7f, 0.9f, hd * 2f));
        LevelKit.Parapet(root, f.P(hw - 0.35f, y + 0.45f, 0f), f.S(0.7f, 0.9f, hd * 2f));
        // The stair head: a solid box beside the hatch (on its inward side), 3 m tall: a kick wall and a lip
        Kick(root, f, new Vector3(hatch.x + (hatch.x > 0f ? -3f : 3f), y + 1.5f, hatch.y), new Vector3(2.6f, 3f, 3.4f), LevelKit.Brick);
        for (int i = 0; i < stacks; i++)
            LevelKit.KickStack(root, f.P(-hw + 4f + i * 7f, y, hd * 0.55f), 3.6f + i * 0.6f, 1.6f);
        if (tower && hw > 9f)
        {
            Vector3 foot = f.P(hw * 0.55f, y, -hd * 0.5f);
            LevelKit.WaterTower(root, foot, 5.5f);
            LevelKit.Slab(root, foot + Vector3.up * 1.5f, new Vector3(1.4f, 3f, 1.4f), LevelKit.RivetIron, "TowerCore");
        }
    }

    /// The FACADES on the block's four outer faces: punched windows over a string course, a string course every two
    /// floors, the cornice at the roof line; on the front, a SHOPFRONT under an awning where `shop` says (its centre
    /// along the front, its width — beside the door, never over it; `pane` false over a real window)
    static void Facades(Transform root, Block b, BlockFrame f, float h, int floors, string name, Vector2? shop, bool pane = true)
    {
        float storeyH = (h - 4.4f) / Mathf.Max(1, floors);
        Face[] faces = { Face.South, Face.North, Face.East, Face.West };
        for (int i = 0; i < faces.Length; i++)
        {
            bool side = faces[i] == Face.East || faces[i] == Face.West;
            float width = side ? f.blockHD * 2f : f.blockHW * 2f;
            Vector3 n = BlockFrame.Normal(faces[i]);
            Vector3 at = f.P(n.x * f.blockHW, 0f, n.z * f.blockHD);
            Vector3 outward = f.D(n);
            if (floors > 0) LevelKit.PunchedWindows(at, outward, width - 2f, 4.4f, floors, storeyH, b.bx * 7 + b.bz * 3 + i);
            for (int s = 2; s < floors; s += 2) LevelKit.StringCourse(at, outward, width, 4.4f + s * storeyH);
            LevelKit.Cornice(at, outward, width, h + 1f);
            if (shop.HasValue && i == 0)
            {
                Vector3 c = f.P(shop.Value.x, 0f, -f.blockHD) + outward * 0.02f;
                LevelKit.Shopfront(root, c, outward, shop.Value.y, name, 3.6f, pane);
                LevelKit.Awning(root, c, outward, shop.Value.y);
            }
            else LevelKit.StringCourse(at, outward, width, 4.2f, 0.5f);
        }
    }

    // ------------------------------------------------------------ the generic hall

    /// A HALL: a box 10 m tall with a door in every face (the main line straight through, 6.5 m wide), a gallery
    /// 4.6 m up along both side walls (a plinth lip at its start: a corner boost onto it; a pad at the far end for
    /// the slow way), rows of tables (0.9 m lips) down the floor, a chandelier orb, a kick-wall ladder in the back
    /// corner from the east gallery to the roof hatch. `tables` 0 a dance floor, 1 billiard tables, 2 gaming
    /// tables, 3 a cinema's raked seats (a ramp). FUEL: a doorman at the front, footmen on the galleries, a page under
    /// the chandelier, a roster unit on the floor
    static void Hall(Transform root, MovementConfig config, Plan plan, Block b, BlockFrame f, string name, int tables)
    {
        const float h = 10f, gy = 4.6f;
        Vector3 min = new Vector3(-f.hw, 0f, -f.hd), max = new Vector3(f.hw, h, f.hd);
        Vector2 hatch = new Vector2(f.hw - 3f, f.hd - 3f);
        Shell(root, f, h, hatch, Door(f, Face.South, 0f, 0f, min, max), Door(f, Face.North, 0f, 0f, min, max), Door(f, Face.West, 0f, 0f, min, max), Door(f, Face.East, 0f, 0f, min, max));
        var zone = plan.zone;
        float gw = 3.4f, gx = f.hw - gw * 0.5f;
        for (int s = -1; s <= 1; s += 2)
        {
            float x = s * gx;
            Lip(root, f, new Vector3(x, 0.65f, -f.hd + 5.5f), new Vector3(gw - 0.4f, 1.3f, 3f));
            Floor(root, f, new Vector3(x, gy - 0.5f, 1.5f), new Vector3(gw, 1f, f.hd * 2f - 10f), "Gallery");
            Mark(root, f, new Vector3(x - s * (gw * 0.5f - 0.1f), gy + 0.5f, 1.5f), new Vector3(0.08f, 1f, f.hd * 2f - 10f), LevelKit.Brass, "GalleryRail");
            Line(root, b, f, s < 0 ? "west gallery" : "east gallery", new Vector3(x, 0f, -f.hd + 1f), new Vector3(x, 0f, -f.hd + 3.6f), zone);
            Fuel(root, config, f, EnemyKind.Dummy, new Vector3(x, gy, 3f), s < 0 ? "west gallery" : "east gallery");
        }
        // The kick-wall ladder to the roof: a full-height boost pillar off the east gallery's edge and a kick wall
        // standing on the gallery's end, 2.6 m apart, the hatch over them
        Corner(root, f, new Vector3(f.hw - 3f - 1.6f, h * 0.5f - 0.03f, f.hd - 3f), new Vector3(0.6f, h - 0.06f, 3f), LevelKit.Charcoal);
        Kick(root, f, new Vector3(f.hw - 3f + 1.6f, gy + 2.5f, f.hd - 3f), new Vector3(0.6f, 5f, 3f));
        Flute(root, f, new Vector3(f.hw, h - 2f, f.hd - 3f), Vector3.left, 4f);
        // The floor: tables as lips, or the raked seats, or a clear dance floor with the band stand
        if (tables == 3) { Ramp(root, f, 0f, f.hw * 2f - 2f * gw - 1f, -f.hd + 8f, 0f, f.hd - 8f, 3.2f, LevelKit.Floor, true); Lip(root, f, new Vector3(0f, 0.6f, -f.hd + 5f), new Vector3(10f, 1.2f, 2f)); }
        else if (tables == 0) Lip(root, f, new Vector3(0f, 0.6f, f.hd - 5f), new Vector3(12f, 1.2f, 4f), LevelKit.WoodPanel, "BandStand");
        else
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * f.hw * 0.42f, z = -f.hd * 0.5f + (i / 2) * f.hd;
                Lip(root, f, new Vector3(x, 0.45f, z), tables == 1 ? new Vector3(3f, 0.9f, 1.6f) : new Vector3(2.4f, 0.9f, 2.4f), LevelKit.WoodPanel, "Table");
            }
        LevelKit.Chandelier(root, f.P(0f, h * 0.7f, 0f), f.origin.y + h);
        Fuel(root, config, f, EnemyKind.Sentry, new Vector3(2.5f, 0f, -f.hd + 3f), "front door");
        Fuel(root, config, f, EnemyKind.Turret, new Vector3(0f, h * 0.5f, 5f), "chandelier");
        PoolFuel(config, f, new Vector3(R(-3f, 3f), 0f, 2f), "main");
        Roof(root, f, h, hatch, 2, true);
        Facades(root, b, f, h, 1, name, new Vector2(-f.hw * 0.57f, f.hw * 0.68f));   // beside the doors
        Entrance(plan, b, f, "front doors", new Vector3(0f, 0f, -f.hd), Vector3.back, EK.Door);
        Entrance(plan, b, f, "back doors", new Vector3(0f, 0f, f.hd), Vector3.forward, EK.Door);
        Entrance(plan, b, f, "west door", new Vector3(-f.hw, 0f, 0f), Vector3.left, EK.Door);
        Entrance(plan, b, f, "east door", new Vector3(f.hw, 0f, 0f), Vector3.right, EK.Door);
        Entrance(plan, b, f, "roof hatch", new Vector3(hatch.x, h + 1f, hatch.y), Vector3.forward, EK.RoofHatch, true, false);
        if (string.IsNullOrEmpty(b.name)) b.name = name;
    }
}
