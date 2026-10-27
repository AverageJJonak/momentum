using UnityEngine;
using Plan = CityDistrict.DistrictPlan;
using Block = CityDistrict.Block;
using EK = CityDistrict.EntranceKind;

/// <summary>
/// HALCYON HOUSE'S OWN BLOCK (Level 5): THE ARCADE — a glazed shopping street THROUGH the block. Local frame: x across
/// the front, z from the front (−hd, the street it faces) to the back (+hd), y up from the street. The CASINO, the
/// CLUB and the REGISTRY are in G2, G3 and G4; the pieces they share are HouseKit (Level5DistrictHouse.cs).
/// </summary>
public static partial class Establishments
{
    static readonly string[] HouseShops =
    {
        "HALCYON TOBACCO", "THE HOUSE TAILOR", "HALCYON WIRELESS", "THE HOUSE FLORIST", "HALCYON STATIONERS", "THE HOUSE PERFUMER",
        "HALCYON GRAMOPHONE", "THE HOUSE CHEMIST", "HALCYON HATTERS", "THE HOUSE JEWELLER", "HALCYON TRAVEL", "THE HOUSE CONFECTIONER",
    };

    /// The facades of a House building drawn by hand (no string course across a tall mouth): punched windows over
    /// `y0`, a string course between the floors, the cornice at the roof line; the low string course on `lowFaces`
    static void HouseFacades(Block b, BlockFrame f, float h, int floors, float y0, bool lowFront, bool lowBack, bool lowSides)
    {
        float storeyH = (h - y0) / Mathf.Max(1, floors);
        Face[] faces = { Face.South, Face.North, Face.East, Face.West };
        for (int i = 0; i < faces.Length; i++)
        {
            bool side = faces[i] == Face.East || faces[i] == Face.West;
            float width = side ? f.blockHD * 2f : f.blockHW * 2f;
            Vector3 n = BlockFrame.Normal(faces[i]);
            Vector3 at = f.P(n.x * f.blockHW, 0f, n.z * f.blockHD), outward = f.D(n);
            LevelKit.PunchedWindows(at, outward, width - 2f, y0, floors, storeyH, b.bx * 7 + b.bz * 3 + i);
            for (int s = 1; s < floors; s++) LevelKit.StringCourse(at, outward, width, y0 + s * storeyH);
            LevelKit.Cornice(at, outward, width, h + 1f);
            if (i == 0 ? lowFront : i == 1 ? lowBack : lowSides) LevelKit.StringCourse(at, outward, width, y0 - 0.2f, 0.5f);
        }
    }

    /// THE ARCADE (h 14). A glazed street 10 m wide straight through the block, OPEN at both ends (12 m mouths under
    /// stepped brass portals: a pass-through at run speed), ground-floor SHOPS both sides behind window walls with
    /// a door gap each (a footman in every door), oxblood AWNINGS over the windows (lips: the way up), GALLERIES at
    /// 5 m along both sides (brass rails; the lit edge a lip over the awnings) reached by a STAIR at each end (the
    /// west from the south mouth, the east from the north; banisters to slide), upper shopfronts along them, a
    /// PORTHOLE passage at the crossing out through each side wall onto the side street (entrances at 5 m: the awning
    /// under each is the step), a FOUNTAIN at the crossing with the CLOCK hung over it, and THE LANTERN: a stepped
    /// glass roof — flat flanks at 13.4 over the galleries with CUT PANES (cyan bars: cut one and drop 8 m onto the
    /// gallery), clerestory risers, the crown at 14.75 (a lip between the flanks), the roof decks at 15 either side;
    /// an open bay over the west gallery's end is the way up (a kick-wall ladder from the gallery) and the lantern's
    /// way in from the roof. MAIN: mouth → the street → mouth. FAST: awning → gallery → the stair's banister down the
    /// far end; the portholes; the lantern. FUEL: a doorman at each mouth, footmen in the shop doors, a bellhop on
    /// the west gallery, a page under the clock, a roster unit on the street.
    static void Arcade(Transform root, MovementConfig config, Plan plan, Block b, BlockFrame f)
    {
        const float h = 14f, gy = 5f, flank = 13.4f, crown = 14.75f, lanT = 0.3f, sw = 5f, gw = 4.5f, stair = 16.4f;
        float hw = f.hw, hd = f.hd, gx1 = sw + gw, deckY = h + 0.5f;
        Vector3 min = new Vector3(-hw, 0f, -hd), max = new Vector3(hw, h, hd);
        var zone = plan.zone;
        Room(root, f, min, max, LevelKit.Faces.North | LevelKit.Faces.South | LevelKit.Faces.East | LevelKit.Faces.West,
            Door(f, Face.South, 0f, 0f, min, max, 12f, 5.4f), Door(f, Face.North, 0f, 0f, min, max, 12f, 5.4f),
            Window(f, Face.West, 0f, 3.2f, gy, gy + 3.2f, min, max), Window(f, Face.East, 0f, 3.2f, gy, gy + 3.2f, min, max));
        for (int s = -1; s <= 1; s += 2)
        {
            bool front = s < 0;   // the west stair climbs from the south mouth, the east from the north
            float shop0 = front ? -hd + stair : -hd, shop1 = front ? hd : hd - stair, bayL = (shop1 - shop0) / 3f;
            float gal0 = front ? -hd + stair : -hd + 0.6f, gal1 = front ? hd - 0.6f : hd - stair, gz = (gal0 + gal1) * 0.5f, gl = gal1 - gal0;
            float x = s * (sw + gw * 0.5f), mx = s * (gx1 + hw) * 0.5f, mw = hw - gx1;
            // The shop body (solid behind the shops), the upper shops over it in two pieces round the porthole passage
            Box(root, f, new Vector3(mx, 2.25f, 0f), new Vector3(mw, 4.5f, hd * 2f), LevelKit.Wall, "ShopBody");
            Box(root, f, new Vector3(mx, (gy + deckY) * 0.5f, (-hd - 2f) * 0.5f), new Vector3(mw, deckY - gy, hd - 2f), LevelKit.Wall, "UpperShops");
            Box(root, f, new Vector3(mx, (gy + deckY) * 0.5f, (hd + 2f) * 0.5f), new Vector3(mw, deckY - gy, hd - 2f), LevelKit.Wall, "UpperShops");
            Box(root, f, new Vector3(mx, (8.5f + deckY) * 0.5f, 0f), new Vector3(mw, deckY - 8.5f, 4f), LevelKit.Wall, "UpperShops");
            Floor(root, f, new Vector3(s * (gx1 - 0.05f + hw) * 0.5f, gy - 0.5f, 0f), new Vector3(hw - gx1 + 0.05f, 1f, 4f), "Passage");
            // The ground shops: a window wall a bay with the door gap before it, a counter and a lamp in each
            for (int i = 0; i < 3; i++)
            {
                float z0 = shop0 + i * bayL, zd = z0 + 1.2f, zw = z0 + 2.4f + (bayL - 2.4f) * 0.5f;
                Box(root, f, new Vector3(s * (sw + 0.5f), 2.25f, zw), new Vector3(1f, 4.5f, bayL - 2.4f), LevelKit.Wall, "ShopWall");
                LevelKit.Shopfront(root, f.P(s * sw, 0f, zw), f.D(Vector3.left * s), bayL - 2.8f, HouseShops[i + (front ? 0 : 3)], 2.6f, true);
                LevelKit.Awning(root, f.P(s * sw, 0f, zw), f.D(Vector3.left * s), bayL - 2.8f, 3.7f, 2f);
                LevelKit.Shopfront(root, f.P(s * gx1, gy, zw), f.D(Vector3.left * s), bayL - 1f, HouseShops[6 + i + (front ? 0 : 3)], 3f, true);
                Lip(root, f, new Vector3(s * (gx1 - 1f), 0.55f, zw), new Vector3(1f, 1.1f, bayL - 3.2f), LevelKit.WoodPanel, "Counter");
                Lamp(root, f, new Vector3(s * (sw + 2.5f), 3.3f, zw), gy - 1f);
                Fuel(root, config, f, EnemyKind.Dummy, new Vector3(s * (sw + 0.7f), 0f, zd), "the shop doors");
            }
            // The gallery: its slab a hair proud of the shop wall, the lit edge a lip, the rail
            Floor(root, f, new Vector3(s * (sw - 0.05f + gx1 - 0.05f) * 0.5f, gy - 0.5f, gz), new Vector3(gw, 1f, gl), "Gallery");
            LevelKit.BrassStrip(root, f.P(s * (sw + 0.05f), gy, gal0 + 0.3f), f.P(s * (sw + 0.05f), gy, gal1 - 0.3f), 0.14f);
            TechMarkers.MarkLip(f.P(s * (sw + 0.35f), gy, gz), f.S(0.9f, 0.9f, gl));
            HouseKit.Rail(root, f.P(s * (sw + 0.15f), gy, gal0 + 0.2f), f.P(s * (sw + 0.15f), gy, gal1 - 0.2f), 1f);
            // The stair: 18°, nosed, up from the mouth to the gallery's end; a banister on its street side
            float zLow = front ? -hd + 1.2f : hd - 1.2f, zHigh = front ? -hd + stair - 0.2f : hd - stair + 0.2f;
            Ramp(root, f, x, gw - 0.1f, Mathf.Min(zLow, zHigh), front ? 0f : gy, Mathf.Max(zLow, zHigh), front ? gy : 0f, LevelKit.Floor, true);
            Banister(root, f, s * (sw + 0.7f), Mathf.Min(zLow, zHigh), front ? 0f : gy, Mathf.Max(zLow, zHigh), front ? gy : 0f);
            Line(root, b, f, front ? "the west stair" : "the east stair", new Vector3(x, 0f, front ? -hd + 0.5f : hd - 0.5f), new Vector3(x, 0f, zLow), zone, false);
            Line(root, b, f, front ? "the west awnings" : "the east awnings", new Vector3(s * 4.2f, 0f, shop0 + 1f), new Vector3(s * 4.2f, 0f, shop0 + bayL), zone);
            // The porthole: the ring outside, the awning under it (the step up to the sill at 5)
            HouseKit.Porthole(root, f.P(s * (hw + T * 0.5f), gy + 1.6f, 0f), f.D(Vector3.right * s), 2.1f);
            LevelKit.Awning(root, f.P(s * (hw + T * 0.5f), 0f, 0f), f.D(Vector3.right * s), 6f);
            Line(root, b, f, s < 0 ? "the west porthole" : "the east porthole", new Vector3(s * (hw + 8f), 0f, 0f), new Vector3(s * (hw + 1.5f), 0f, 0f), zone);
            // The roof deck over the upper shops, its parapets, the lit inner edge (a lip over the flank)
            LevelKit.Slab(root, f.P(s * (gx1 - 0.05f + hw + T) * 0.5f, deckY, 0f), f.S(hw + T - gx1 + 0.05f, 1f, (hd + T) * 2f), LevelKit.Charcoal, "RoofDeck");
            LevelKit.Parapet(root, f.P(s * (hw + T - 0.35f), deckY + 0.95f, 0f), f.S(0.7f, 0.9f, (hd + T) * 2f - 1.6f));
            for (int e = -1; e <= 1; e += 2) LevelKit.Parapet(root, f.P(s * (gx1 + hw + T) * 0.5f, deckY + 0.95f, e * (hd + T - 0.35f)), f.S(hw + T - gx1 - 1f, 0.9f, 0.7f));
            LevelKit.BrassStrip(root, f.P(s * (gx1 + 0.1f), deckY + 0.5f, -hd + 0.5f), f.P(s * (gx1 + 0.1f), deckY + 0.5f, hd - 0.5f), 0.14f);
        }
        // THE LANTERN: the crown, the risers and the flanks a bay at a time; brass mullions between the bays
        int bays = Mathf.CeilToInt(hd * 2f / 3.1f);
        float bayZ = hd * 2f / bays;
        int[] cutWest = { 7, 9 }, cutEast = { 2, 4 };
        int openBay = bays - 1;
        for (int i = 0; i < bays; i++)
        {
            float zc = -hd + bayZ * (i + 0.5f), zl = bayZ - 0.1f;
            LevelKit.Slab(root, f.P(0f, crown - lanT * 0.5f, zc), f.S(6.7f, lanT, zl), LevelKit.FrostGlass, "CrownPane");
            LevelKit.Prim(root, f.P(0f, crown + 0.06f, zc - bayZ * 0.5f), f.S(6.9f, 0.12f, 0.12f), LevelKit.Brass, "Mullion");
            for (int s = -1; s <= 1; s += 2)
            {
                LevelKit.Slab(root, f.P(s * 3.5f, (flank - lanT + crown - lanT) * 0.5f, zc), f.S(lanT, crown - lanT - (flank - lanT), zl), LevelKit.FrostGlass, "RiserPane");
                LevelKit.Prim(root, f.P(s * (3.65f + gx1 - 0.05f) * 0.5f, flank + 0.06f, zc - bayZ * 0.5f), f.S(gx1 - 3.7f, 0.12f, 0.12f), LevelKit.Brass, "Mullion");
                bool cut = System.Array.IndexOf(s < 0 ? cutWest : cutEast, i) >= 0;
                if (s < 0 && i == openBay) continue;   // the open bay: the lantern's way in and out
                if (!cut) { LevelKit.Slab(root, f.P(s * (3.65f + gx1 - 0.05f) * 0.5f, flank - lanT * 0.5f, zc), f.S(gx1 - 3.7f, lanT, zl), LevelKit.FrostGlass, "FlankPane"); continue; }
                LevelKit.Slab(root, f.P(s * 4.575f, flank - lanT * 0.5f, zc), f.S(1.85f, lanT, zl), LevelKit.FrostGlass, "FlankPane");
                LevelKit.Slab(root, f.P(s * (8.5f + gx1 - 0.05f) * 0.5f, flank - lanT * 0.5f, zc), f.S(gx1 - 8.55f, lanT, zl), LevelKit.FrostGlass, "FlankPane");
                HouseKit.CutPane(root, f.P(s * 7f, flank - lanT * 0.5f, zc), f.S(3f, lanT, zl), config, Vector3.up);
            }
        }
        for (int e = -1; e <= 1; e += 2) LevelKit.Slab(root, f.P(0f, (flank - lanT + crown) * 0.5f, e * (hd - 0.3f)), f.S(7.3f, crown - flank + lanT, lanT), LevelKit.FrostGlass, "LanternGable");
        for (int s = -1; s <= 1; s += 2) LevelKit.BrassStrip(root, f.P(s * 3.2f, crown, -hd + 0.5f), f.P(s * 3.2f, crown, hd - 0.5f), 0.14f);
        TechMarkers.MarkLip(f.P(0f, crown, 0f), f.S(7f, 0.9f, hd * 2f));
        // The way up to the lantern: a kick-wall ladder from the west gallery's end to the open bay
        float lz = -hd + bayZ * (openBay + 0.5f);
        Corner(root, f, new Vector3(-8.6f, (gy + flank) * 0.5f, lz), new Vector3(0.6f, flank - gy, 2.8f), LevelKit.Charcoal);
        Kick(root, f, new Vector3(-5.8f, gy + 2.5f, lz), new Vector3(0.6f, 5f, 2.8f));
        Flute(root, f, new Vector3(-gx1, gy + 6.5f, lz), Vector3.right, 4f);
        Line(root, b, f, "the lantern", new Vector3(-7.2f, gy, lz - 6f), new Vector3(-7.2f, gy, lz - 1.8f), zone);
        // The crossing: the fountain, the clock hung from the crown over it, the page under it
        LevelKit.Fountain(root, f.P(0f, 0f, 0f), 5f);
        LevelKit.Prim(root, f.P(0f, 12.4f, 0f), new Vector3(0.1f, 1.9f, 0.1f), LevelKit.Bronze, "ClockChain", PrimitiveType.Cylinder);
        LevelKit.Prim(root, f.P(0f, 10.2f, 0f), new Vector3(2.8f, 0.5f, 0.5f), LevelKit.Brass, "ClockCase");
        LevelKit.ClockFace(root, f.P(0f, 10.2f, -0.3f), f.D(Vector3.back), 2.4f);
        LevelKit.ClockFace(root, f.P(0f, 10.2f, 0.3f), f.D(Vector3.forward), 2.4f);
        Fuel(root, config, f, EnemyKind.Turret, new Vector3(0f, 7f, 0f), "under the clock");
        for (float z = -hd + 5f; z < hd - 4f; z += 9f) for (int s = -1; s <= 1; s += 2) Lamp(root, f, new Vector3(s * 7.2f, 12f, z), flank - lanT);
        // The roof's furniture, the fuel, the lines, the facades and the portals
        LevelKit.KickStack(root, f.P(-hw + 4f, deckY + 0.5f, hd * 0.55f), 3.6f, 1.6f);
        LevelKit.KickStack(root, f.P(hw - 4f, deckY + 0.5f, -hd * 0.55f), 4.2f, 1.6f);
        Vector3 tower = f.P(hw * 0.7f, deckY + 0.5f, hd * 0.45f);
        LevelKit.WaterTower(root, tower, 5.5f);
        LevelKit.Slab(root, tower + Vector3.up * 1.5f, new Vector3(1.4f, 3f, 1.4f), LevelKit.RivetIron, "TowerCore");
        Fuel(root, config, f, EnemyKind.Sentry, new Vector3(-2.5f, 0f, -hd + 3f), "the south mouth");
        Fuel(root, config, f, EnemyKind.Sentry, new Vector3(2.5f, 0f, hd - 3f), "the north mouth");
        Fuel(root, config, f, EnemyKind.Chaser, new Vector3(-7.2f, gy, 6f), "the west gallery");
        PoolFuel(config, f, new Vector3(R(-3f, 3f), 0f, -hd + 9f), "the arcade");
        Line(root, b, f, "the arcade", new Vector3(0f, 0f, -hd + 1f), new Vector3(0f, 0f, hd - 1f), zone, false);
        HouseFacades(b, f, h, 1, 9f, false, false, true);   // one row of windows, over the portals' sunbursts
        HouseKit.Portal(root, f.P(0f, 0f, -f.blockHD), f.D(Vector3.back), 12f, 5.4f, "THE HALCYON ARCADE");
        HouseKit.Portal(root, f.P(0f, 0f, f.blockHD), f.D(Vector3.forward), 12f, 5.4f, "THE HALCYON ARCADE");
        LevelKit.BladeSign(root, f.P(hw * 0.5f, h - 1f, -hd - 0.5f), f.D(Vector3.back), "ARCADE", 1.1f);
        Entrance(plan, b, f, "the south mouth", new Vector3(0f, 0f, -hd), Vector3.back, EK.Open);
        Entrance(plan, b, f, "the north mouth", new Vector3(0f, 0f, hd), Vector3.forward, EK.Open);
        Entrance(plan, b, f, "the west porthole", new Vector3(-hw, gy, 0f), Vector3.left, EK.Window, true);
        Entrance(plan, b, f, "the east porthole", new Vector3(hw, gy, 0f), Vector3.right, EK.Window, true);
        Entrance(plan, b, f, "the lantern", new Vector3(-7.2f, flank, lz), Vector3.forward, EK.Skylight, true, false);
        if (string.IsNullOrEmpty(b.name)) b.name = "THE ARCADE";
    }
}
