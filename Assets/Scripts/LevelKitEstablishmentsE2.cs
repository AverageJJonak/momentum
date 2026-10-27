using UnityEngine;
using Plan = CityDistrict.DistrictPlan;
using Block = CityDistrict.Block;
using EK = CityDistrict.EntranceKind;

/// <summary>
/// BELOW STAIRS, THROUGH THE CITY (Level 3): the steam LAUNDRY. See LevelKitEstablishmentsE.cs for the shared pieces.
/// </summary>
public static partial class Establishments
{
    /// LAUNDRY. A steam laundry twelve metres to its roof. THE FRONT: the receiving counter beside the street door (a
    /// lip). THE HALL: the MANGLES down the west side (iron beds under copper rollers, 1.3 m lips), the PRESSES in a row
    /// off the middle (1.6 m lips under their heads), the VATS along the east wall (a 1.5 m rim on three sides: hop in,
    /// drop; the fourth side a 25° slide up and OUT — the slide-hop off its rim is the line; the window over the middle
    /// one onto the alley), the DRYING RACKS on rails at four metres: six thin lips, each a hand higher than the last,
    /// a ladder across the room from the mangles to the window (a pad at its foot for the slow way). THE LAUNDRY CHUTE:
    /// a 30° slide from a mouth in the roof down the west wall into the hall (the way IN from the tenement's roof next
    /// door, over the alley). THE HOIST: two boost pillars in the front-east corner to the roof hatch (LiftShaft). THE
    /// DRYING YARD on the roof: four washing lines between iron posts, the parapet, the boiler's chimney.
    /// MAIN: the front door → the floor → the yard door. FAST: a mangle → the rack ladder → the window; the vats in a
    /// chain; the chute from the roof straight at the front door.
    /// FUEL: a clerk at the counter, footmen at the mangles, a bellhop on a press, a page over the vats, THE JANITOR on
    /// the floor between them.
    static void Laundry(Transform root, MovementConfig config, Plan plan, Block b, BlockFrame f)
    {
        const float h = 12f, chuteDeg = 30f, sill = 3.8f;
        float hw = f.hw, hd = f.hd, deckY = h + 1f, counterZ = -hd + 6f, doorU = -hd * 0.55f, windowU = 4.4f, vx = hw - 3.7f;
        Vector3 min = new Vector3(-hw, 0f, -hd), max = new Vector3(hw, h, hd);
        Vector2 hatch = new Vector2(hw - 3.5f, -hd + 2.5f), mouth = new Vector2(-hw + 2.2f, hd - 4f);
        var zone = plan.zone;
        Room(root, f, min, max, LevelKit.Faces.North | LevelKit.Faces.South | LevelKit.Faces.East | LevelKit.Faces.West,
            Door(f, Face.South, -hw * 0.3f, 0f, min, max), Door(f, Face.North, 0f, 0f, min, max), Door(f, Face.East, doorU, 0f, min, max),
            Window(f, Face.East, windowU, 4f, sill, sill + 2.4f, min, max));
        // The roof deck in seven pieces round the hoist's hatch (front-east) and the chute's mouth (back-west)
        float rw = hw + T, rd = hd + T, dy = h + 0.5f;
        float ax0 = hatch.x - 1.5f, ax1 = hatch.x + 1.5f, az0 = hatch.y - 1.5f, az1 = hatch.y + 1.5f;
        float bx0 = mouth.x - 1.5f, bx1 = mouth.x + 1.5f, bz0 = mouth.y - 1.5f, bz1 = mouth.y + 1.5f;
        Deck(root, f, -rw, bx1, bz1, rd, dy); Deck(root, f, -rw, bx1, -rd, bz0, dy); Deck(root, f, -rw, bx0, bz0, bz1, dy);
        Deck(root, f, bx1, ax0, -rd, rd, dy);
        Deck(root, f, ax0, rw, az1, rd, dy); Deck(root, f, ax0, rw, -rd, az0, dy); Deck(root, f, ax1, rw, az0, az1, dy);
        // THE FRONT: the receiving counter beside the door, the clerk behind it
        Lip(root, f, new Vector3(4.25f, 0.55f, counterZ), new Vector3(11.5f, 1.1f, 1.2f), LevelKit.WoodPanel, "Counter");
        Fuel(root, config, f, EnemyKind.Sentry, new Vector3(4f, 0f, counterZ + 2.2f), "the counter");
        // THE MANGLES and THE PRESSES, the Janitor between them
        for (int i = 0; i < 4; i++)
        {
            float z = -hd + 12f + i * 5f;
            Lip(root, f, new Vector3(-hw + 6.5f, 0.65f, z), new Vector3(2.6f, 1.3f, 3.2f), LevelKit.RivetIron, "Mangle");
            for (int s = -1; s <= 1; s += 2) Pipe(root, f, new Vector3(-hw + 6.5f, 1.55f, z + s * 0.5f), 2.8f, 0.5f, Vector3.right, Copper, "Roller");
            if (i % 2 == 1) Fuel(root, config, f, EnemyKind.Dummy, new Vector3(-hw + 9.6f, 0f, z), "the mangles");
        }
        for (int i = 0; i < 3; i++)
        {
            float z = -3f + i * 7f;
            Lip(root, f, new Vector3(-3f, 0.8f, z), new Vector3(2.4f, 1.6f, 2.4f), LevelKit.RivetIron, "Press");
            Mark(root, f, new Vector3(-3f, 3.6f, z), new Vector3(2.6f, 0.3f, 2.6f), Copper, "PressHead");
            Pipe(root, f, new Vector3(-3f, (3.75f + h) * 0.5f, z), h - 3.75f, 0.3f, Vector3.up, LevelKit.RivetIron, "PressColumn");
        }
        Fuel(root, config, f, EnemyKind.Chaser, new Vector3(-3f, 1.6f, 4f), "the presses");
        Fuel(root, config, f, EnemyKind.Janitor, new Vector3(3f, 0f, 1f), "the floor");
        // THE VATS along the east wall: three rims (lips), the slide up and out, suds; the window over the middle one
        for (int i = 0; i < 3; i++)
        {
            float vz = -3f + i * 8f;
            Lip(root, f, new Vector3(vx, 0.75f, vz - 2.8f), new Vector3(6f, 1.5f, 0.4f), LevelKit.RivetIron, "VatRim");
            for (int s = -1; s <= 1; s += 2) Lip(root, f, new Vector3(vx + s * 2.8f, 0.75f, vz + 0.225f), new Vector3(0.4f, 1.5f, 5.55f), LevelKit.RivetIron, "VatRim");
            Ramp(root, f, vx, 4.9f, vz - 0.7f, 0f, vz + 2.5f, 1.5f, LevelKit.Slope);
            Mark(root, f, new Vector3(vx, 0.5f, vz - 1.6f), new Vector3(5.1f, 0.02f, 2.1f), LevelKit.FrostGlass, "Suds");
        }
        Fuel(root, config, f, EnemyKind.Turret, new Vector3(5f, 7.5f, 9f), "over the vats");
        // THE DRYING RACKS: the ladder across the room at four metres, sheets on them, the rail over, the pad at its foot
        for (int i = 0; i < 6; i++)
        {
            float x = -7f + i * 3.5f, y = 3.4f + i * 0.4f;
            Lip(root, f, new Vector3(x, y + 0.15f, 2f), new Vector3(1.4f, 0.3f, 7f), LevelKit.WoodPanel, "DryingRack");
            Pipe(root, f, new Vector3(x, (y + 0.3f + h - 0.6f) * 0.5f, 2f), h - 0.9f - y, 0.08f, Vector3.up, LevelKit.RivetIron, "RackHanger");
            for (int s = -1; s <= 1; s += 2) Mark(root, f, new Vector3(x + s * 0.5f, y - 0.4f, 2f + s * 1.5f), new Vector3(0.04f, 1.1f, 2.4f), s < 0 ? LevelKit.Cream : LevelKit.Canvas, "Sheet");
        }
        Pipe(root, f, new Vector3(1.75f, h - 0.6f, 2f), 21f, 0.12f, Vector3.right, Copper, "RackRail");
        Pad(root, f, new Vector3(-7f, 0f, -4.5f), 14f, 4f);
        // THE LAUNDRY CHUTE down the west wall, its foot a metre over the floor (the hop); THE HOIST in the front-east corner
        float run = (h - 1.1f) / Mathf.Tan(chuteDeg * Mathf.Deg2Rad), top = mouth.y - 1.6f;
        Slope(root, f, new Vector3(mouth.x, h - 0.1f, top), new Vector3(mouth.x, 1f, top - run), 3f, LevelKit.Slope, "LaundryChute");
        Box(root, f, new Vector3(mouth.x, 0.5f, top - run - 1f), new Vector3(3f, 1f, 2f), LevelKit.Floor, "ChuteFoot");
        LiftShaft(root, f, new Vector3(hatch.x, 0f, hatch.y), 0f, deckY);
        // The lines, the lamps, the tile
        Line(root, b, f, "the floor", new Vector3(-hw * 0.3f, 0f, -hd + 1f), new Vector3(-hw * 0.3f, 0f, counterZ - 1.5f), zone, false);
        Line(root, b, f, "the racks", new Vector3(-7f, 0f, -11f), new Vector3(-7f, 0f, -6.2f), zone);
        Line(root, b, f, "the vats", new Vector3(vx, 0f, -hd + 8f), new Vector3(vx, 0f, -6.6f), zone);
        Line(root, b, f, "the chute", new Vector3(mouth.x, 0f, top - run - 2.2f), new Vector3(mouth.x, 0f, -hd + 1f), zone);
        for (float z = -hd + 4.75f; z < hd - 3f; z += 8f) Lamp(root, f, new Vector3(7f, h - 1.8f, z), h);
        Dado(root, f, Face.West, -hd + 8f, hd - 0.4f);
        Dado(root, f, Face.East, -hd + 0.4f, doorU - 3.4f);
        Dado(root, f, Face.East, doorU + 3.4f, hd - 0.4f);
        // THE DRYING YARD: the parapets, the stair head and the stack (Roof), the chute's hopper lips round its mouth,
        // the boiler's chimney, the washing lines between their posts
        Roof(root, f, h, hatch, 1, false);
        Lip(root, f, new Vector3(mouth.x, deckY + 0.45f, bz0 - 0.45f), new Vector3(3.9f, 0.9f, 0.7f), LevelKit.RivetIron, "ChuteHopper");
        Lip(root, f, new Vector3(mouth.x, deckY + 0.45f, bz1 + 0.45f), new Vector3(3.9f, 0.9f, 0.7f), LevelKit.RivetIron, "ChuteHopper");
        Lip(root, f, new Vector3(bx1 + 0.45f, deckY + 0.45f, mouth.y), new Vector3(0.7f, 0.9f, 3f), LevelKit.RivetIron, "ChuteHopper");
        LevelKit.KickStack(root, f.P(hw * 0.3f, deckY, hd - 3.5f), 6f, 2.2f);
        for (int i = 0; i < 4; i++)
        {
            float z = -hd + 7f + i * 6f;
            LevelKit.WashingLine(root, f.P(-hw + 5f, deckY + 2.1f, z), f.P(hw - 7.5f, deckY + 2.1f, z), 6);
            Pipe(root, f, new Vector3(-hw + 5f, deckY + 1.1f, z), 2.2f, 0.12f, Vector3.up, LevelKit.RivetIron, "LinePost");
            Pipe(root, f, new Vector3(hw - 7.5f, deckY + 1.1f, z), 2.2f, 0.12f, Vector3.up, LevelKit.RivetIron, "LinePost");
        }
        Facades(root, b, f, h, 1, "HALCYON STEAM LAUNDRY", new Vector2(hw * 0.35f, hw * 0.6f));   // beside the front door
        Entrance(plan, b, f, "the front door", new Vector3(-hw * 0.3f, 0f, -hd), Vector3.back, EK.Door);
        Entrance(plan, b, f, "the yard door", new Vector3(0f, 0f, hd), Vector3.forward, EK.Door);
        Entrance(plan, b, f, "the alley door", new Vector3(hw, 0f, doorU), Vector3.right, EK.Door);
        Entrance(plan, b, f, "the vat-room window", new Vector3(hw, sill, windowU), Vector3.right, EK.Window, true);
        Entrance(plan, b, f, "the laundry chute", new Vector3(-hw, deckY, mouth.y), Vector3.left, EK.Chute, true);
        Entrance(plan, b, f, "the drying yard", new Vector3(hatch.x, deckY, hatch.y), Vector3.forward, EK.RoofHatch, true, false);
        if (string.IsNullOrEmpty(b.name)) b.name = "THE STEAM LAUNDRY";
    }
}
