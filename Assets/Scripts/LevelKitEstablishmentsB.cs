using UnityEngine;
using Plan = CityDistrict.DistrictPlan;
using Block = CityDistrict.Block;
using EK = CityDistrict.EntranceKind;

/// <summary>
/// THE ESTABLISHMENTS: the HOTEL (a lobby with a grand stair, a palm court open to the sky, a ballroom with a
/// balcony ring, a roof garden, the lift a kick shaft) and the THEATRE (a foyer under a marquee you can slide, an
/// auditorium with box tiers, the stage and its flies a kick-wall ladder).
/// </summary>
public static partial class Establishments
{
    /// A LIFT SHAFT: two tall boost pillars `gap` apart from `y0` to `y1` with a rest lip every 4.6 m on alternate
    /// sides (a wall-kick ladder; a corner boost on every lip), a pad at its foot for the slow way up
    static void LiftShaft(Transform root, BlockFrame f, Vector3 lFoot, float y0, float y1, float gap = 3.2f)
    {
        float h = y1 - y0;
        for (int s = -1; s <= 1; s += 2) Corner(root, f, lFoot + new Vector3(s * (gap * 0.5f + 0.3f), y0 + h * 0.5f, 0f), new Vector3(0.6f, h, 3.2f), LevelKit.Charcoal);
        int k = 0;
        for (float y = y0 + 4.6f; y < y1 - 1f; y += 4.6f, k++)
            Lip(root, f, lFoot + new Vector3((k % 2 == 0 ? -1f : 1f) * (gap * 0.5f - 0.6f), y - 0.15f, 0f), new Vector3(1.2f, 0.3f, 3f), LevelKit.Brass, "LiftLedge");
        Mark(root, f, lFoot + new Vector3(0f, y0 + 2.4f, 1.7f), new Vector3(1f, 1f, 0.06f), LevelKit.Brass, "LiftDial");
        Pad(root, f, lFoot + Vector3.up * (y0 - lFoot.y), Mathf.Sqrt(2f * 25f * Mathf.Min(h + 1.2f, 10f)), 1f);
    }

    /// HOTEL. The LOBBY (nine metres tall, the grand stair up to a mezzanine ring at 5, a lip at its top, banisters
    /// down both edges, the front doors under a porte-cochère), the PALM COURT behind it OPEN TO THE SKY (palms, a
    /// fountain, galleries at 5, 10 and 14 along its sides with parapet lips: a climb to the roof garden, or a drop
    /// onto a pad), the BALLROOM at the back (nine metres, a balcony ring at 5, the band stand, a chandelier, balcony
    /// doors onto the east street at 4.6), the ROOF GARDEN over it all (pergolas as lips, the court's well as the
    /// skylight down), the LIFT a kick shaft from the mezzanine to the roof.
    /// MAIN: doors → lobby → the court's arch → the court → the ballroom's arch → the back door. FAST: the banister
    /// slide, the court's galleries, the balcony out, the shaft to the roof and over the parapets anywhere.
    /// FUEL: a doorman at the doors, a bellhop on the stair lip, pages over the court and under the chandelier,
    /// footmen on the mezzanine and the balcony ring, a roster unit in the lobby.
    static void Hotel(Transform root, MovementConfig config, Plan plan, Block b, BlockFrame f)
    {
        const float h = 18f, mezz = 4f;
        float hw = f.hw, hd = f.hd, lobbyZ = -hd + 13f, ballZ = Mathf.Max(lobbyZ + 8f, hd - 8f), courtHW = Mathf.Min(8f, hw - 6f);
        Vector3 min = new Vector3(-hw, 0f, -hd), max = new Vector3(hw, h, hd);
        Vector2 hatch = new Vector2(hw - 3.5f, -hd + 2.5f);
        Room(root, f, min, max, LevelKit.Faces.North | LevelKit.Faces.South | LevelKit.Faces.East | LevelKit.Faces.West,
            Door(f, Face.South, 0f, 0f, min, max, 8f, 5f), Door(f, Face.North, hw * 0.3f, 0f, min, max),
            Window(f, Face.East, hd - 5f, 4f, 4.6f, 8.2f, min, max));
        // The roof deck in pieces round the court's well and the lift's hatch
        float rw = hw + T, rd = hd + T, hx0 = hatch.x - 1.5f, hx1 = hatch.x + 1.5f, hz0 = hatch.y - 1.5f, hz1 = hatch.y + 1.5f;
        LevelKit.Slab(root, f.P((-rw + hx0) * 0.5f, h + 0.5f, (lobbyZ - rd) * 0.5f), f.S(hx0 + rw, 1f, lobbyZ + rd), LevelKit.Charcoal, "RoofDeck");   // the lobby's roof round the lift's hatch
        LevelKit.Slab(root, f.P((hx0 + rw) * 0.5f, h + 0.5f, (-rd + hz0) * 0.5f), f.S(rw - hx0, 1f, hz0 + rd), LevelKit.Charcoal, "RoofDeck");
        LevelKit.Slab(root, f.P((hx0 + rw) * 0.5f, h + 0.5f, (hz1 + lobbyZ) * 0.5f), f.S(rw - hx0, 1f, lobbyZ - hz1), LevelKit.Charcoal, "RoofDeck");
        LevelKit.Slab(root, f.P((hx1 + rw) * 0.5f, h + 0.5f, hatch.y), f.S(rw - hx1, 1f, 3f), LevelKit.Charcoal, "RoofDeck");
        LevelKit.Slab(root, f.P(0f, h + 0.5f, (ballZ + rd) * 0.5f), f.S(rw * 2f, 1f, rd - ballZ), LevelKit.Charcoal, "RoofDeck");
        LevelKit.Slab(root, f.P((-rw - courtHW) * 0.5f, h + 0.5f, (lobbyZ + ballZ) * 0.5f), f.S(rw - courtHW, 1f, ballZ - lobbyZ), LevelKit.Charcoal, "RoofDeck");
        LevelKit.Slab(root, f.P((rw + courtHW) * 0.5f, h + 0.5f, (lobbyZ + ballZ) * 0.5f), f.S(rw - courtHW, 1f, ballZ - lobbyZ), LevelKit.Charcoal, "RoofDeck");
        // THE LOBBY: the partition to the court with its arch, the grand stair, the mezzanine ring, the lift shaft
        Box(root, f, new Vector3(-hw * 0.5f - 3.25f, h * 0.5f, lobbyZ), new Vector3(hw - 3.25f, h, 0.6f), LevelKit.Wall, "Partition");
        Box(root, f, new Vector3(hw * 0.5f + 3.25f, h * 0.5f, lobbyZ), new Vector3(hw - 3.25f, h, 0.6f), LevelKit.Wall, "Partition");
        Box(root, f, new Vector3(0f, (h + 8f) * 0.5f, lobbyZ), new Vector3(6.5f, h - 8f, 0.6f), LevelKit.Wall, "Partition");
        // The grand stair: 20°, eight metres wide, from two metres inside the doors up to its lip (the landing across
        // the whole width, 1.2 m over the flight's end: jump it, corner-boost it), the side mezzanines off the landing
        float stairW = 8f, stairZ0 = -hd + 2f, stairZ1 = stairZ0 + (mezz - 1.2f) / Mathf.Tan(20f * Mathf.Deg2Rad);
        Ramp(root, f, 0f, stairW, stairZ0, 0f, stairZ1, mezz - 1.2f, LevelKit.Floor, true);
        Banister(root, f, -stairW * 0.5f - 0.7f, stairZ0, 0f, stairZ1, mezz - 1.2f);
        Banister(root, f, stairW * 0.5f + 0.7f, stairZ0, 0f, stairZ1, mezz - 1.2f);
        Lip(root, f, new Vector3(0f, mezz - 0.5f, stairZ1 + 1.5f), new Vector3(hw * 2f - 1f, 1f, 3f), LevelKit.Floor, "StairLip");
        for (int s = -1; s <= 1; s += 2)
        {
            float mz0 = s > 0 ? -hd + 4f : -hd, mz1 = stairZ1;   // the east one starts past the lift's hatch
            Floor(root, f, new Vector3(s * (hw - 2f), mezz - 0.5f, (mz0 + mz1) * 0.5f), new Vector3(4f, 1f, mz1 - mz0), "Mezzanine");
            Mark(root, f, new Vector3(s * (hw - 4.05f), mezz + 0.5f, (mz0 + mz1) * 0.5f), new Vector3(0.08f, 1f, mz1 - mz0), LevelKit.Brass, "MezzRail");
            Fuel(root, config, f, EnemyKind.Dummy, new Vector3(s * (hw - 2f), mezz, -hd + 7f), "the mezzanine");
        }
        Fuel(root, config, f, EnemyKind.Chaser, new Vector3(0f, mezz, stairZ1 + 1.5f), "the stair lip");
        Fuel(root, config, f, EnemyKind.Sentry, new Vector3(5f, 0f, -hd + 3f), "the doors");
        PoolFuel(config, f, new Vector3(R(-5f, 5f), 0f, stairZ1 + 5f), "the lobby");
        LiftShaft(root, f, new Vector3(hw - 3.5f, 0f, -hd + 2.5f), 0f, h + 1f);
        LevelKit.Chandelier(root, f.P(0f, 8f, -hd + 6f), f.origin.y + h);
        // THE PALM COURT: open sky, palms, a fountain, galleries at 5, 10 and 14 along both sides (lips), a pad
        for (int s = -1; s <= 1; s += 2)
        {
            float gx = s * (courtHW + (hw - courtHW) * 0.5f), gw = hw - courtHW;   // from the court's edge to the wall
            foreach (float gy in new[] { 5f, 10f, 14f })
            {
                Floor(root, f, new Vector3(gx, gy - 0.5f, (lobbyZ + ballZ) * 0.5f), new Vector3(gw, 1f, ballZ - lobbyZ - 1.2f), "Gallery");
                LevelKit.Parapet(root, f.P(s * (courtHW + 0.35f), gy + 0.45f, (lobbyZ + ballZ) * 0.5f), f.S(0.7f, 0.9f, ballZ - lobbyZ - 1.2f));
            }
            Flute(root, f, new Vector3(s * courtHW, 7.5f, (lobbyZ + ballZ) * 0.5f), Vector3.left * s, 4f, 0.5f);
            LevelKit.Tree(root, f.P(s * courtHW * 0.6f, 0f, lobbyZ + 2.5f), 6f, false);
        }
        LevelKit.Fountain(root, f.P(0f, 0f, (lobbyZ + ballZ) * 0.5f), 6f);
        Pad(root, f, new Vector3(0f, 0f, ballZ - 2.5f), Mathf.Sqrt(2f * 25f * 11.2f), 1f);
        Fuel(root, config, f, EnemyKind.Turret, new Vector3(0f, 8f, (lobbyZ + ballZ) * 0.5f), "over the court");
        Line(root, b, f, "the court", new Vector3(0f, 0f, lobbyZ + 0.5f), new Vector3(0f, 0f, ballZ - 0.5f), plan.zone, false);
        // THE BALLROOM: its partition and arch, a balcony ring at 5, the band stand, the balcony doors east
        Box(root, f, new Vector3(-hw * 0.5f - 3.25f, h * 0.5f, ballZ), new Vector3(hw - 3.25f, h, 0.6f), LevelKit.Wall, "Partition");
        Box(root, f, new Vector3(hw * 0.5f + 3.25f, h * 0.5f, ballZ), new Vector3(hw - 3.25f, h, 0.6f), LevelKit.Wall, "Partition");
        Box(root, f, new Vector3(0f, (h + 8f) * 0.5f, ballZ), new Vector3(6.5f, h - 8f, 0.6f), LevelKit.Wall, "Partition");
        Floor(root, f, new Vector3(0f, 9.5f, (ballZ + hd) * 0.5f), new Vector3(hw * 2f, 1f, hd - ballZ), "BallroomCeiling");
        for (int s = -1; s <= 1; s += 2)
        {
            Floor(root, f, new Vector3(s * (hw - 1.6f), mezz - 0.5f, (ballZ + hd) * 0.5f), new Vector3(3.2f, 1f, hd - ballZ), "Balcony");
            Mark(root, f, new Vector3(s * (hw - 3.25f), mezz + 0.5f, (ballZ + hd) * 0.5f), new Vector3(0.08f, 1f, hd - ballZ), LevelKit.Brass, "BalconyRail");
            Lip(root, f, new Vector3(s * (hw - 1.6f), 0.65f, ballZ + 3f), new Vector3(2.8f, 1.3f, 2.5f));   // the plinth lip under each balcony's start
        }
        Floor(root, f, new Vector3(0f, mezz - 0.5f, hd - 1.6f), new Vector3(hw * 2f - 6.4f, 1f, 3.2f), "Balcony");   // across the back, between the side balconies
        Lip(root, f, new Vector3(0f, 0.6f, hd - 4f), new Vector3(10f, 1.2f, 4f), LevelKit.WoodPanel, "BandStand");
        LevelKit.Chandelier(root, f.P(0f, 6.5f, (ballZ + hd) * 0.5f), f.origin.y + 9f);
        Fuel(root, config, f, EnemyKind.Turret, new Vector3(0f, 3.5f, (ballZ + hd) * 0.5f), "under the chandelier");
        Fuel(root, config, f, EnemyKind.Chaser, new Vector3(hw - 1.6f, mezz, hd - 5f), "the balcony doors");
        Fuel(root, config, f, EnemyKind.Dummy, new Vector3(-hw + 1.6f, mezz, hd - 5f), "the balcony ring");
        Line(root, b, f, "over the band stand", new Vector3(hw - 1.6f, 0f, ballZ + 0.5f), new Vector3(hw - 1.6f, 0f, ballZ + 1.6f), plan.zone);
        // THE ROOF GARDEN: pergolas (lips), hedges, the court's well parapets, the lift head; the facades, the porte-cochère
        Roof(root, f, h, hatch, 0, true);
        for (int s = -1; s <= 1; s += 2) LevelKit.Parapet(root, f.P(s * (courtHW + 0.35f), h + 1.45f, (lobbyZ + ballZ) * 0.5f), f.S(0.7f, 0.9f, ballZ - lobbyZ));
        LevelKit.Parapet(root, f.P(0f, h + 1.45f, lobbyZ + 0.35f), f.S(courtHW * 2f, 0.9f, 0.7f));
        LevelKit.Parapet(root, f.P(0f, h + 1.45f, ballZ - 0.35f), f.S(courtHW * 2f, 0.9f, 0.7f));
        Lip(root, f, new Vector3(-hw * 0.5f, h + 1.6f, hd - 4f), new Vector3(6f, 1.2f, 3f), LevelKit.WoodPanel, "Pergola");
        LevelKit.Hedge(root, f.P(hw * 0.5f, h + 1f, hd - 4f), f.S(6f, 0.9f, 1f));
        Facades(root, b, f, h, 3, null, null);
        LevelKit.Marquee(root, f.P(0f, 0f, -hd - 0.5f), f.D(Vector3.back), 12f, string.IsNullOrEmpty(b.name) ? "THE ASTOR" : b.name, 5.6f, 6f);   // the block's own name on the marquee (two hotels in one district read differently)
        Entrance(plan, b, f, "the front doors", new Vector3(0f, 0f, -hd), Vector3.back, EK.Door);
        Entrance(plan, b, f, "the service door", new Vector3(hw * 0.3f, 0f, hd), Vector3.forward, EK.Door);
        Entrance(plan, b, f, "the balcony doors", new Vector3(hw, 4.6f, hd - 5f), Vector3.right, EK.Balcony, true);
        Entrance(plan, b, f, "the roof garden", new Vector3(0f, h + 1f, (lobbyZ + ballZ) * 0.5f), Vector3.forward, EK.Skylight, true, false);
        LevelKit.Awning(root, f.P(hw + 0.5f, 0f, hd - 5f), f.D(Vector3.right), 6f);   // under the balcony doors
        Lip(root, f, new Vector3(hw + 1.1f, 4.4f, hd - 5f), new Vector3(1.2f, 0.4f, 5f), LevelKit.Limestone, "Balcony");
        if (string.IsNullOrEmpty(b.name)) b.name = "THE ASTOR";
    }

    /// THEATRE. The FOYER under the MARQUEE (a canopy pitched ten degrees to the street: a slide out of the circle
    /// lobby's window, a platform from a lamp standard's kick), the AUDITORIUM (the stalls a 3 m rake up from the
    /// stage, BOX TIERS at 4 and 8 along both walls — lips at their edges), the STAGE (a 1.2 m lip) and THE FLIES
    /// behind it (two boost pillars 3.5 m apart: a kick-wall ladder to the fly gallery at 13 and the roof hatch), the
    /// stage door at the back, a fire exit west. MAIN: doors → foyer → stalls → over the stage → the stage door. FAST:
    /// the box tiers in a chain, the flies, the marquee slide. FUEL: an usher in the foyer, footmen in the boxes, a
    /// page over the stalls, a stagehand on the stage lip, a roster unit in the stalls.
    static void Theatre(Transform root, MovementConfig config, Plan plan, Block b, BlockFrame f)
    {
        const float h = 18f, circle = 5.4f;
        float hw = f.hw, hd = f.hd, foyerZ = -hd + 8f, stageZ = hd - 8f;
        Vector3 min = new Vector3(-hw, 0f, -hd), max = new Vector3(hw, h, hd);
        Vector2 hatch = new Vector2(-hw + 3f, hd - 3f);
        Shell(root, f, h, hatch,
            Door(f, Face.South, 0f, 0f, min, max, 8f, 4.5f), Door(f, Face.North, hw * 0.5f, 0f, min, max), Door(f, Face.West, -hd * 0.2f, 0f, min, max),
            Window(f, Face.South, 0f, 8f, circle, circle + 3f, min, max));
        // The foyer and the circle lobby over it (its floor at the window's sill), the stair up to it
        Floor(root, f, new Vector3(0f, circle - 0.5f, (-hd + foyerZ) * 0.5f), new Vector3(hw * 2f, 1f, foyerZ + hd - 4f), "CircleLobby");
        Ramp(root, f, hw - 3f, 5f, foyerZ - 2f, circle, foyerZ + 10f, 0f, LevelKit.Floor, true);   // down from the circle lobby into the stalls' side aisle
        Lip(root, f, new Vector3(-hw + 2.5f, 0.65f, -hd + 3f), new Vector3(4f, 1.3f, 2f), LevelKit.WoodPanel, "BoxOffice");
        Fuel(root, config, f, EnemyKind.Sentry, new Vector3(0f, 0f, -hd + 4f), "the foyer");
        LevelKit.Marquee(root, f.P(0f, 0f, -hd - 0.5f), f.D(Vector3.back), 14f, string.IsNullOrEmpty(b.name) ? "THE REGENT" : b.name, circle - 0.4f, 6f, 10f);
        // The auditorium: the rake, the box tiers, the stage, the flies
        Ramp(root, f, 0f, hw * 2f - 8f, foyerZ + 2f, 3f, stageZ - 2f, 0f, LevelKit.Floor, true);   // the rake: down from the back of the stalls to the stage
        Floor(root, f, new Vector3(0f, 2.5f, foyerZ + 1f), new Vector3(hw * 2f, 1f, 2.2f), "RakeTop");
        for (int s = -1; s <= 1; s += 2)
            foreach (float ty in new[] { 4f, 8f })
            {
                Floor(root, f, new Vector3(s * (hw - 1.75f), ty - 0.5f, (foyerZ + stageZ) * 0.5f), new Vector3(3.5f, 1f, stageZ - foyerZ - 4f), "BoxTier");
                LevelKit.Parapet(root, f.P(s * (hw - 3.15f), ty + 0.4f, (foyerZ + stageZ) * 0.5f), f.S(0.7f, 0.8f, stageZ - foyerZ - 4f));
                Fuel(root, config, f, EnemyKind.Dummy, new Vector3(s * (hw - 1.75f), ty, foyerZ + 6f + ty), "the boxes");
            }
        Lip(root, f, new Vector3(0f, 0.6f, (stageZ + hd) * 0.5f), new Vector3(hw * 2f - 2f, 1.2f, hd - stageZ), LevelKit.WoodPanel, "Stage");
        Fuel(root, config, f, EnemyKind.Chaser, new Vector3(-3f, 1.2f, stageZ + 2f), "the stage lip");
        Fuel(root, config, f, EnemyKind.Turret, new Vector3(0f, 6f, (foyerZ + stageZ) * 0.5f), "over the stalls");
        PoolFuel(config, f, new Vector3(R(-4f, 4f), 1.5f, (foyerZ + stageZ) * 0.5f), "the stalls");
        float flyZ = hd - 2.5f;
        for (int s = -1; s <= 1; s += 2) Corner(root, f, new Vector3(-hw + 3f + s * 2.05f, 1.2f + 6.5f, flyZ), new Vector3(0.6f, 13f, 3f), LevelKit.Charcoal);
        Floor(root, f, new Vector3(0f, 13f, hd - 1.6f), new Vector3(hw * 2f, 1f, 3.2f), "FlyGallery");
        Lip(root, f, new Vector3(-hw + 3f, 13.6f, flyZ - 2.5f), new Vector3(3f, 0.3f, 2f), LevelKit.Brass, "FlyLedge");
        Line(root, b, f, "the flies", new Vector3(-hw + 3f, 1.2f, stageZ + 1f), new Vector3(-hw + 3f, 1.2f, flyZ - 1.8f), plan.zone);
        Line(root, b, f, "the stalls", new Vector3(0f, 0f, -hd + 1f), new Vector3(0f, 0f, foyerZ - 0.5f), plan.zone, false);
        for (int i = 0; i < 3; i++) Lamp(root, f, new Vector3(0f, h - 3f, foyerZ + 4f + i * 7f), h);
        Roof(root, f, h, hatch, 1, true);
        Facades(root, b, f, h, 2, null, null);
        LevelKit.BladeSign(root, f.P(hw * 0.5f, h - 2f, -hd - 0.5f), f.D(Vector3.back), "THEATRE", 1.4f);
        Entrance(plan, b, f, "the front doors", new Vector3(0f, 0f, -hd), Vector3.back, EK.Door);
        Entrance(plan, b, f, "the marquee window", new Vector3(0f, circle, -hd), Vector3.back, EK.Marquee, true);
        Entrance(plan, b, f, "the stage door", new Vector3(hw * 0.5f, 0f, hd), Vector3.forward, EK.Door);
        Entrance(plan, b, f, "the fire exit", new Vector3(-hw, 0f, -hd * 0.2f), Vector3.left, EK.Door);
        Entrance(plan, b, f, "the fly roof", new Vector3(hatch.x, h + 1f, hatch.y), Vector3.forward, EK.RoofHatch, true, false);
        if (string.IsNullOrEmpty(b.name)) b.name = "THE REGENT";
    }
}
