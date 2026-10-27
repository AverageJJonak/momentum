using UnityEngine;
using Plan = CityDistrict.DistrictPlan;
using Block = CityDistrict.Block;
using Kind = CityDistrict.Establishment;
using EK = CityDistrict.EntranceKind;

/// <summary>
/// THE GRAND HOTELS (Level 4): the RESTAURANT — THE PALM COURT. A DINING ROOM under a STEPPED DECO DOME (concentric
/// ring tiers stepping in over a glazed well: from the floor you look up through the glass into the corbelled dome;
/// from the roof the tiers are a lip ladder to the OCULUS, and the drop through the oculus lands on the glass at speed
/// and goes through it — the skylight entrance), tables as 0.9 m lips in two rings round the band's DAIS (a 1.2 m
/// lip), a GALLERY at 5 along both side walls with a brass rail lip (a plinth lip and a wall kick under its back end:
/// the WALL COMBO boost; a nosed stair the slow way), the KITCHENS behind through THE PASS (a brass counter lip in the
/// opening: slide under or hop it), the cold store a kick box, the service lift a kick shaft to the roof hatch, the
/// service door to the alley, the loading door to the back street; THE WINE CELLAR down a stair under the kitchens,
/// the COAL CHUTE up out of it to the west avenue (a pad at its foot); the ROOF TERRACE over the kitchens with pergola
/// lips, the fire escape in the alley a lip ladder to the gallery window and the roof.
/// MAIN: doors → round the dais → the pass → the kitchens → the loading door. FAST: the galleries by the combo, the
/// chute at speed, the fire escape to the roof, the tiers to the oculus and the drop through the glass.
/// FUEL: doormen at both doors, footmen at the tables, a bellhop on the gallery, a page under the dome, a bouncer at
/// the pass, a roster unit in the cellar, a footman on the terrace. This file: the rooms; F2: the floors, the dome, the roof.
/// </summary>
public static partial class Establishments
{
    static void Restaurant(Transform root, MovementConfig config, Plan plan, Block b, BlockFrame f)
    {
        const float h = 14f, gy = 5f, cellar = -6f, chuteDeg = 25f, stairDeg = 26f, ringW = 2f, tierH = 1.2f;
        float hw = f.hw, hd = f.hd;
        float passZ = hd - 10.5f, zc = (-hd + passZ) * 0.5f;                                  // the pass partition; the dining room's centre, the dome's axis
        float chuteZ = passZ + 3.3f, chuteRun = -cellar / Mathf.Tan(chuteDeg * Mathf.Deg2Rad), chuteOpen = 8.5f;
        float stairZ = hd - 2.1f, stairRun = -cellar / Mathf.Tan(stairDeg * Mathf.Deg2Rad);
        float escZ = zc - 2f, escStorey = (h + 1f - 0.3f) / 3f;                               // the fire escape's landings: the third 0.3 m under the roof deck
        float domeR = Mathf.Min(10f, (passZ + hd) * 0.5f - 1f, hw - 3f);                       // the drum's half size (the glazed well is domeR square)
        int tiers = Mathf.Max(2, Mathf.FloorToInt((domeR - 4f) / tierH) + 1);
        float oculusY = h + 1f + tiers * tierH;
        Vector3 min = new Vector3(-hw, 0f, -hd), max = new Vector3(hw, h, hd);
        Vector2 hatch = new Vector2(hw - 3.5f, hd - 2.5f);

        // The floors in pieces (the cellar's stair and the chute cut through them), the shell with its openings
        RstFloors(root, f, passZ, chuteZ, chuteOpen, stairRun);
        Room(root, f, min, max, LevelKit.Faces.North | LevelKit.Faces.South | LevelKit.Faces.East | LevelKit.Faces.West,
            Door(f, Face.South, 0f, 0f, min, max, 8f, 5f), Door(f, Face.North, hw * 0.3f, 0f, min, max), Door(f, Face.East, chuteZ, 0f, min, max),
            f.H(Face.West, chuteZ - 1.8f, chuteZ + 1.8f, 0f, 2.6f, min, max), Window(f, Face.East, escZ, 3.4f, gy, gy + 3f, min, max));
        RstRoofDeck(root, f, h, domeR, zc, hatch);

        // THE DINING ROOM: the dais under the dome, the tables in rings round it, the chandelier hung from the glass
        Lip(root, f, new Vector3(0f, 0.6f, zc), new Vector3(8f, 1.2f, 8f), LevelKit.WoodPanel, "BandStand");
        RstTables(root, config, f, zc, 7.6f, new[] { 35f, 55f, 125f, 145f, 215f, 235f, 305f, 325f }, true);
        RstTables(root, config, f, zc, 12f, new[] { 22f, 68f, 112f, 158f, 202f, 248f, 292f, 338f }, false);
        LevelKit.Chandelier(root, f.P(0f, 9f, zc), f.origin.y + h, 3f);
        Fuel(root, config, f, EnemyKind.Turret, new Vector3(0f, 7.5f, zc + 5f), "under the dome");
        Fuel(root, config, f, EnemyKind.Sentry, new Vector3(3f, 0f, -hd + 3f), "the front doors");
        for (int sx = -1; sx <= 1; sx += 2) for (int sz = -1; sz <= 1; sz += 2) Lamp(root, f, new Vector3(sx * (domeR + 4f), h - 3f, zc + sz * 6f), h);
        Line(root, b, f, "the pass", new Vector3(0f, 0f, -hd + 1f), new Vector3(0f, 0f, passZ - 1f), plan.zone, false);
        // THE GALLERIES at 5 along both side walls: the brass rail lip on the edge, the nosed stair down at the back,
        // the plinth lip and the wall's kick under the back end (the combo), the window out onto the fire escape (east)
        for (int s = -1; s <= 1; s += 2)
        {
            float gx = s * (hw - 2f), gz0 = -hd + 1f, gz1 = -3.5f;
            Floor(root, f, new Vector3(gx, gy - 0.5f, (gz0 + gz1) * 0.5f), new Vector3(4f, 1f, gz1 - gz0), "Gallery");
            Lip(root, f, new Vector3(s * (hw - 3.8f), gy + 0.45f, (gz0 + gz1) * 0.5f), new Vector3(0.4f, 0.9f, gz1 - gz0), LevelKit.Brass, "GalleryRail");
            Ramp(root, f, gx, 4f, gz1, gy, gz1 + gy / Mathf.Tan(stairDeg * Mathf.Deg2Rad), 0f, LevelKit.Floor, true);
            Lip(root, f, new Vector3(s * (hw - 6.8f), 0.65f, -2f), new Vector3(3f, 1.3f, 3f));
            Flute(root, f, new Vector3(s * hw, 2.4f, -7f), Vector3.left * s, 4f);
            Line(root, b, f, s < 0 ? "west gallery" : "east gallery", new Vector3(s * (hw - 0.6f), 0f, -hd + 2f), new Vector3(s * (hw - 0.6f), 0f, -5f), plan.zone);
        }
        Fuel(root, config, f, EnemyKind.Chaser, new Vector3(hw - 2f, gy, zc - 4f), "the gallery");
        // THE PASS: the partition with its 6.5 m opening, the counter lip in it, the brass frame over
        Box(root, f, new Vector3(-hw * 0.5f - 3.25f, h * 0.5f, passZ), new Vector3(hw - 3.25f, h, 0.5f), LevelKit.Wall, "Partition");
        Box(root, f, new Vector3(hw * 0.5f + 3.25f, h * 0.5f, passZ), new Vector3(hw - 3.25f, h, 0.5f), LevelKit.Wall, "Partition");
        Box(root, f, new Vector3(0f, (h + 4.25f) * 0.5f, passZ), new Vector3(6.5f, h - 4.25f, 0.5f), LevelKit.Wall, "Partition");
        Lip(root, f, new Vector3(0f, 0.55f, passZ), new Vector3(6.5f, 1.1f, 0.6f), LevelKit.Brass, "Pass");
        Mark(root, f, new Vector3(0f, 4.4f, passZ), new Vector3(7f, 0.3f, 0.7f), LevelKit.Brass, "Frame");
        Fuel(root, config, f, EnemyKind.Warden, new Vector3(2.5f, 0f, passZ + 2.5f), "the pass");
        // THE KITCHENS: the ranges and the counter (lips), the cold store (a kick box with a lip roof), the service
        // lift (a kick shaft under the hatch), the service door east into the alley, the loading door north
        Lip(root, f, new Vector3(-1.5f, 0.45f, hd - 1.3f), new Vector3(8f, 0.9f, 1.6f), LevelKit.RivetIron, "Range");
        Lip(root, f, new Vector3(hw * 0.35f, 0.55f, passZ + 4.5f), new Vector3(hw * 0.5f, 1.1f, 1.4f), LevelKit.WoodPanel, "Counter");
        Kick(root, f, new Vector3(hw - 9.5f, 1.75f, hd - 2.5f), new Vector3(4f, 3.5f, 3.4f), LevelKit.RivetIron);
        Lip(root, f, new Vector3(hw - 9.5f, 3.8f, hd - 2.5f), new Vector3(4.4f, 0.6f, 3.8f), LevelKit.Limestone, "ColdStoreRoof");
        LiftShaft(root, f, new Vector3(hw - 3.5f, 0f, hd - 2.5f), 0f, h + 1f);
        Fuel(root, config, f, EnemyKind.Sentry, new Vector3(hw - 3f, 0f, chuteZ), "the service door");
        for (float z = passZ + 3f; z < hd - 2f; z += 5f) Lamp(root, f, new Vector3(-hw * 0.3f, h - 2.5f, z), h);
        // THE WINE CELLAR under the kitchens: the stair down along the back wall (west), the racks, the coal chute up
        // and out through the west wall (a pad at its foot for the slow way up), a chandelier
        Room(root, f, new Vector3(-hw, cellar, passZ), new Vector3(hw, -1f, hd), LevelKit.Faces.North | LevelKit.Faces.South | LevelKit.Faces.East | LevelKit.Faces.West | LevelKit.Faces.Floor);
        var stair = Slope(root, f, new Vector3(-hw + 2f, 0f, stairZ), new Vector3(-hw + 2f + stairRun, cellar, stairZ), 3.6f, LevelKit.Floor, "CellarStair");
        RstNosings(root, f, new Vector3(-hw + 2f, 0f, stairZ), new Vector3(-hw + 2f + stairRun, cellar, stairZ), 3.6f, stair.transform.rotation);
        Slope(root, f, new Vector3(-hw + 2f, 1f, stairZ + 2.05f), new Vector3(-hw + 2f + stairRun, cellar + 1f, stairZ + 2.05f), 0.3f, LevelKit.Brass, "BanisterRamp");   // the handrail a metre over the flight's edge: a slide
        Slope(root, f, new Vector3(-hw + 0.4f, 0f, chuteZ), new Vector3(-hw + 0.4f + chuteRun, cellar, chuteZ), 3.4f, LevelKit.Slope, "CoalChute");
        Pad(root, f, new Vector3(-hw + 2f + chuteRun + 1.5f, cellar, chuteZ), 14f, 9f);
        Lip(root, f, new Vector3(0f, cellar + 0.6f, passZ + 1.1f), new Vector3(hw * 1.2f, 1.2f, 1.4f), LevelKit.WoodPanel, "WineRack");
        Lip(root, f, new Vector3(hw - 1.2f, cellar + 0.6f, (passZ + hd) * 0.5f), new Vector3(1.4f, 1.2f, hd - passZ - 4f), LevelKit.WoodPanel, "WineRack");
        LevelKit.Chandelier(root, f.P(0f, cellar + 3.4f, (passZ + hd) * 0.5f), f.origin.y - 1f, 1.8f);
        PoolFuel(config, f, new Vector3(R(-5f, 5f), cellar, (passZ + hd) * 0.5f), "the wine cellar");
        Line(root, b, f, "the coal chute", new Vector3(-hw + chuteRun + 6f, cellar, chuteZ), new Vector3(-hw + chuteRun + 1.5f, cellar, chuteZ), plan.zone);
        Line(root, b, f, "the cellar stair", new Vector3(-hw + 2f, 0f, passZ + 1f), new Vector3(-hw + 2f, 0f, stairZ - 2f), plan.zone, false);

        // THE DOME over the dining room, the roof and its terrace, the fire escape, the facades, the record
        RstDome(root, config, f, h, domeR, zc, tiers, ringW, tierH);
        RstRoof(root, config, f, b, plan, h, hatch, passZ, zc, domeR, hw, hd);
        LevelKit.FireEscape(root, f.P(hw + 0.5f, 0f, escZ), f.D(Vector3.right), 3, escStorey, 7f);
        Facades(root, b, f, h, 2, "THE PALM COURT", new Vector2(-hw * 0.55f, 10f));
        LevelKit.Marquee(root, f.P(0f, 0f, -hd - 0.5f), f.D(Vector3.back), 11f, string.IsNullOrEmpty(b.name) ? "THE PALM COURT" : b.name, 5.6f, 5f);
        LevelKit.BladeSign(root, f.P(hw * 0.45f, h - 1.6f, -hd - 0.5f), f.D(Vector3.back), "RESTAURANT", 0.9f);
        Entrance(plan, b, f, "the front doors", new Vector3(0f, 0f, -hd), Vector3.back, EK.Door);
        Entrance(plan, b, f, "the loading door", new Vector3(hw * 0.3f, 0f, hd), Vector3.forward, EK.Door);
        Entrance(plan, b, f, "the service door", new Vector3(hw, 0f, chuteZ), Vector3.right, EK.Door);
        Entrance(plan, b, f, "the coal chute", new Vector3(-hw, 0f, chuteZ), Vector3.left, EK.CellarHatch);
        Entrance(plan, b, f, "the gallery window", new Vector3(hw, gy, escZ), Vector3.right, EK.Window, true);
        Entrance(plan, b, f, "the oculus", new Vector3(0f, oculusY, zc), Vector3.right, EK.Skylight, true);
        if (string.IsNullOrEmpty(b.name)) b.name = "THE PALM COURT";
    }
}
