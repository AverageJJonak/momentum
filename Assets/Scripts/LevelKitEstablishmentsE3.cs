using UnityEngine;
using Plan = CityDistrict.DistrictPlan;
using Block = CityDistrict.Block;
using EK = CityDistrict.EntranceKind;

/// <summary>
/// BELOW STAIRS, THROUGH THE CITY (Level 3): the SERVICE YARD, open ground. See LevelKitEstablishmentsE.cs for the
/// shared pieces.
/// </summary>
public static partial class Establishments
{
    /// A piece of the yard's SETTS over local x0..x1, z0..z1, a metre thick, its top at the street
    static void Setts(Transform root, BlockFrame f, float x0, float x1, float z0, float z1)
    {
        if (x1 - x0 < 0.05f || z1 - z0 < 0.05f) return;
        LevelKit.Slab(root, f.P((x0 + x1) * 0.5f, -0.5f, (z0 + z1) * 0.5f), f.S(x1 - x0, 1f, z1 - z0), LevelKit.Setts, "BlockFloor");
    }

    /// A DELIVERY VAN parked along local z, `lc` its ground centre: an oxblood body on wheels (its roof a 1.9 m lip), a
    /// charcoal cab in front, a cream band along its flanks
    static void Van(Transform root, BlockFrame f, Vector3 lc)
    {
        Lip(root, f, lc + new Vector3(0f, 1.15f, 0.9f), new Vector3(2.4f, 1.5f, 4f), LevelKit.Oxblood, "VanBody");
        Box(root, f, lc + new Vector3(0f, 1.05f, -1.9f), new Vector3(2.3f, 1.3f, 1.5f), LevelKit.Charcoal, "VanCab");
        for (int s = -1; s <= 1; s += 2)
            for (int k = -1; k <= 1; k += 2)
                Pipe(root, f, lc + new Vector3(s * 1.25f, 0.4f, k * 1.6f), 0.3f, 0.8f, Vector3.right, LevelKit.Charcoal, "Wheel");
        Mark(root, f, lc + new Vector3(0f, 1.2f, 0.9f), new Vector3(2.46f, 0.5f, 3.6f), LevelKit.Cream, "VanBand");
    }

    /// SERVICE YARD. Open ground, the back of the Astor: LOADING DOCKS along the north side (the hotel's service door
    /// across the alley) and the east side (1.2 m lips under a solid canopy you can land on), THE GOODS HOIST between
    /// them (two boost pillars to a gantry at eight with rails, a rest lip, a pad at the foot — and a 30° slide off the
    /// gantry's west end down into the yard), two delivery VANS (1.9 m lips) with a six-metre lane between, crate stacks
    /// (corners) and dumpsters, and THE CELLAR RAMP in the west: a 25° slide three metres down into the coal cellar's
    /// area (its walls kick walls, the cellar's arch at the end), a pad in it that throws you back up and out.
    /// Enterable from every side. FUEL: a porter on the dock, a driver by the vans, a page over the hoist, a roster
    /// unit on the setts.
    static void ServiceYard(Transform root, MovementConfig config, Plan plan, Block b, BlockFrame f)
    {
        float hw = f.blockHW, hd = f.blockHD;
        var zone = plan.zone;
        // The setts, laid round the cellar ramp's pit (the west side, the front half)
        float px0 = -hw + 2f, px1 = -hw + 8f, pz0 = -hd + 6f, pz1 = 0.5f, pxm = (px0 + px1) * 0.5f;
        Setts(root, f, -hw, hw, -hd, pz0); Setts(root, f, -hw, hw, pz1, hd);
        Setts(root, f, -hw, px0, pz0, pz1); Setts(root, f, px1, hw, pz0, pz1);
        // THE CELLAR RAMP: the slide down into the area at −3, the retaining walls (brick, fluted: kick them), the coal
        // cellar's arch and its coal at the end, the lamp over it, the pad that throws you back up over the ramp
        float rampRun = 3f / Mathf.Tan(25f * Mathf.Deg2Rad);
        Ramp(root, f, pxm, 4.9f, pz0, 0f, pz0 + rampRun, -3f, LevelKit.Slope);
        Floor(root, f, new Vector3(pxm, -3.5f, (pz0 + rampRun + pz1) * 0.5f), new Vector3(6f, 1f, pz1 - pz0 - rampRun), "Area");
        Box(root, f, new Vector3(px0 + 0.25f, -2.3f, (pz0 + pz1) * 0.5f), new Vector3(0.5f, 2.5f, pz1 - pz0), LevelKit.Brick, "RetainingWall");
        Box(root, f, new Vector3(px1 - 0.25f, -2.3f, (pz0 + pz1) * 0.5f), new Vector3(0.5f, 2.5f, pz1 - pz0), LevelKit.Brick, "RetainingWall");
        Box(root, f, new Vector3(pxm, -2.3f, pz1 - 0.25f), new Vector3(4.9f, 2.5f, 0.5f), LevelKit.Brick, "RetainingWall");
        Flute(root, f, new Vector3(px0 + 0.5f, -2.2f, pz1 - 3.5f), Vector3.right, 2f, 0.5f);
        Flute(root, f, new Vector3(px1 - 0.5f, -2.2f, pz1 - 3.5f), Vector3.left, 2f, 0.5f);
        Mark(root, f, new Vector3(pxm, -2.2f, pz1 - 0.54f), new Vector3(2.8f, 2.4f, 0.06f), LevelKit.Charcoal, "CellarArch");
        Mark(root, f, new Vector3(pxm, -2.9f, pz1 - 1.5f), new Vector3(3.4f, 0.4f, 1.4f), LevelKit.Charcoal, "Coal");
        var lamp = LevelKit.Light(root, f.P(pxm, -1.2f, pz1 - 1.6f), 8f, 1.2f);
        lamp.color = LevelKit.WindowAmberLight;
        LevelKit.JumpPad(root, f.P(pxm, -3f, pz1 - 2.2f), f.Yaw + 180f, 16f, 7f);
        // THE LOADING DOCKS: the north one across the yard's back, the east one down its side, a solid canopy over each
        // on iron posts, a porter on the north dock
        float dx0 = -hw + 4f, ez1 = hd - 5.05f;
        Lip(root, f, new Vector3((dx0 + hw) * 0.5f, 0.6f, hd - 2.5f), new Vector3(hw - dx0, 1.2f, 5f), LevelKit.Limestone, "LoadingDock");
        Lip(root, f, new Vector3((dx0 + hw) * 0.5f, 4.35f, hd - 2.5f), new Vector3(hw - dx0, 0.3f, 5f), LevelKit.RivetIron, "DockCanopy");
        for (float x = dx0 + 2f; x < hw - 1f; x += 6f) Pipe(root, f, new Vector3(x, 2.7f, hd - 4.8f), 3f, 0.2f, Vector3.up, LevelKit.RivetIron, "CanopyPost");
        Lip(root, f, new Vector3(hw - 2.5f, 0.6f, (-hd + 4f + ez1) * 0.5f), new Vector3(5f, 1.2f, ez1 + hd - 4f), LevelKit.Limestone, "LoadingDock");
        Lip(root, f, new Vector3(hw - 2.5f, 4.35f, (-hd + 4f + ez1) * 0.5f), new Vector3(5f, 0.3f, ez1 + hd - 4f), LevelKit.RivetIron, "DockCanopy");
        for (float z = -hd + 6f; z < ez1 - 1f; z += 6f) Pipe(root, f, new Vector3(hw - 4.8f, 2.7f, z), 3f, 0.2f, Vector3.up, LevelKit.RivetIron, "CanopyPost");
        Fuel(root, config, f, EnemyKind.Chaser, new Vector3(0f, 1.2f, hd - 2.5f), "the dock");
        // THE GOODS HOIST: the kick shaft (LiftShaft: the pillars, a rest lip, the dial, the pad), the gantry carried on
        // the pillars' south ends with rails along its south and east edges, the slide off its west end to the setts
        Vector3 foot = new Vector3(hw - 8.5f, 0f, hd - 8.5f);
        LiftShaft(root, f, foot, 0f, 8.5f);
        float gz = hd - 12f;
        Box(root, f, new Vector3(foot.x, 8.5f, gz), new Vector3(5f, 1f, 4.4f), LevelKit.RivetIron, "Gantry");
        Lip(root, f, new Vector3(foot.x, 9.45f, gz - 1.95f), new Vector3(5f, 0.9f, 0.5f), LevelKit.RivetIron, "GantryRail");
        Lip(root, f, new Vector3(foot.x + 2.25f, 9.45f, gz + 0.275f), new Vector3(0.5f, 0.9f, 3.85f), LevelKit.RivetIron, "GantryRail");
        float slideRun = 8f / Mathf.Tan(30f * Mathf.Deg2Rad);
        Slope(root, f, new Vector3(foot.x - 2.5f, 9f, gz), new Vector3(foot.x - 2.5f - slideRun, 1f, gz), 3f, LevelKit.Slope, "HoistSlide");
        Box(root, f, new Vector3(foot.x - 3.5f - slideRun, 0.5f, gz), new Vector3(2f, 1f, 3f), LevelKit.Floor, "ChuteFoot");
        Fuel(root, config, f, EnemyKind.Turret, new Vector3(foot.x, 5.5f, gz - 4f), "over the hoist");
        // THE VANS with the lane between them, the crate stacks, the dumpsters, the lamps
        Van(root, f, new Vector3(-4f, 0f, -9f));
        Van(root, f, new Vector3(5f, 0f, -10f));
        Fuel(root, config, f, EnemyKind.Dummy, new Vector3(2.5f, 0f, -6.5f), "the vans");
        Corner(root, f, new Vector3(-6f, 2f, 12f), new Vector3(1.8f, 4f, 1.8f), LevelKit.WoodPanel);
        Corner(root, f, new Vector3(-7f, 2f, 3f), new Vector3(1.8f, 4f, 1.8f), LevelKit.WoodPanel);
        Corner(root, f, new Vector3(-12f, 2f, 9f), new Vector3(1.8f, 4f, 1.8f), LevelKit.WoodPanel);
        LevelKit.Dumpster(root, f.P(-14f, 0f, 14.5f), f.Yaw + 90f);
        LevelKit.Dumpster(root, f.P(10f, 0f, -15.5f), f.Yaw);
        LevelKit.LampStandard(root, f.P(-8f, 0f, -15.5f), 6f, true);
        LevelKit.LampStandard(root, f.P(3f, 0f, 10.5f), 6f, false);
        PoolFuel(config, f, new Vector3(R(-4f, 4f), 0f, 3f), "the setts");
        // The lines, the entrances (every side)
        Line(root, b, f, "across", new Vector3(0.5f, 0f, -hd + 1f), new Vector3(0.5f, 0f, -hd * 0.3f), zone, false);
        Line(root, b, f, "the hoist", new Vector3(foot.x, 0f, -5f), new Vector3(foot.x, 0f, foot.z - 2f), zone);
        Line(root, b, f, "the cellar ramp", new Vector3(pxm, 0f, -hd + 1f), new Vector3(pxm, 0f, pz0 - 0.5f), zone);
        Entrance(plan, b, f, "south side", new Vector3(0f, 0f, -hd), Vector3.back, EK.Open);
        Entrance(plan, b, f, "north side", new Vector3(0f, 0f, hd), Vector3.forward, EK.Open);
        Entrance(plan, b, f, "west side", new Vector3(-hw, 0f, 0f), Vector3.left, EK.Open);
        Entrance(plan, b, f, "east side", new Vector3(hw, 0f, 0f), Vector3.right, EK.Open);
        if (string.IsNullOrEmpty(b.name)) b.name = "THE SERVICE YARD";
    }
}
