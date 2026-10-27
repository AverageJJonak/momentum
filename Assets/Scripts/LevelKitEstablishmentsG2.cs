using UnityEngine;
using Plan = CityDistrict.DistrictPlan;
using Block = CityDistrict.Block;
using EK = CityDistrict.EntranceKind;

/// <summary>
/// HALCYON HOUSE'S OWN BLOCK (Level 5): THE CASINO. Local frame as the Arcade's (LevelKitEstablishmentsG.cs).
/// </summary>
public static partial class Establishments
{
    /// THE HALCYON CASINO (h 12). The doors under a MARQUEE; the entrance hall under THE PRIVATE ROOM (a floor at 8
    /// over the front eight metres, its BALCONY out over the avenue at 8: the marquee is the step, a corner boost the
    /// rise — an entrance, tech); THE GAMING FLOOR — eight tables as 0.9 m lips in a horseshoe round the ROULETTE
    /// inlay, a chandelier orb over it; a MEZZANINE at 4.6 along both walls (a plinth lip under each start: the
    /// corner boost; the west one runs up a nosed stair to the private room), THE CAGE (a bronze box behind brass
    /// bars with a counter lip, its roof the step to the vault's top), THE VAULT across the back (riveted, its top a
    /// floor at 5 with a brass lip, a 6.5 m doorway through it to the back door), THE CABARET stage along the east
    /// wall (a 1.2 m lip; the stage door opens onto it), the kick-wall ladder from the east mezzanine to the roof.
    /// MAIN: doors → the floor → through the vault → the back door. FAST: the marquee to the balcony; mezzanine to
    /// suite; the cage's roof to the vault's top; the ladder. FUEL: a doorman at the doors, bouncers at the cage,
    /// footmen at the tables and in the private room, a page over the cabaret, a bellhop on the mezzanine, a roster unit.
    static void Casino(Transform root, MovementConfig config, Plan plan, Block b, BlockFrame f)
    {
        const float h = 12f, mezz = 4.6f, suite = 8f;
        float hw = f.hw, hd = f.hd, suiteZ = -hd + 8f, vaultZ = hd - 7f, cageX = -hw + 7.6f, wheelZ = -hd + 19f;
        Vector3 min = new Vector3(-hw, 0f, -hd), max = new Vector3(hw, h, hd);
        Vector2 hatch = new Vector2(hw - 2.7f, vaultZ - 3f);
        var zone = plan.zone;
        Shell(root, f, h, hatch,
            Door(f, Face.South, 0f, 0f, min, max, 8f, 5f), Door(f, Face.North, 0f, 0f, min, max), Door(f, Face.East, -hd + 16f, 0f, min, max),
            Window(f, Face.South, 0f, 4f, suite, suite + 3.2f, min, max));
        // THE PRIVATE ROOM over the entrance: its floor, the rail along its edge, the card table, the balcony outside
        Floor(root, f, new Vector3(0f, suite - 0.5f, (-hd + suiteZ) * 0.5f), new Vector3(hw * 2f, 1f, suiteZ + hd), "Suite");
        HouseKit.Rail(root, f.P(-hw + 3.6f, suite, suiteZ - 0.15f), f.P(hw - 0.3f, suite, suiteZ - 0.15f), 1f);
        Lip(root, f, new Vector3(4f, suite + 0.45f, -hd + 4f), new Vector3(2.4f, 0.9f, 2.4f), LevelKit.WoodPanel, "CardTable");
        for (int i = 0; i < 2; i++) Lip(root, f, new Vector3(-2f - i * 2.2f, suite + 0.45f, -hd + 3f + i * 2.4f), new Vector3(1f, 0.9f, 1f), LevelKit.Oxblood, "Armchair");
        Lip(root, f, new Vector3(0f, suite - 0.3f, -hd - 1.1f), new Vector3(4f, 0.6f, 1.2f), LevelKit.Limestone, "Balcony");
        Fuel(root, config, f, EnemyKind.Dummy, new Vector3(-3f, suite, -hd + 5.5f), "the private room");
        Lamp(root, f, new Vector3(0f, suite + 3f, -hd + 4f), h);
        for (int s = -1; s <= 1; s += 2) Lamp(root, f, new Vector3(s * 5f, suite - 2f, -hd + 4f), suite - 1f);
        // The stair down from the private room to the west mezzanine (nosed, a banister on its inner edge)
        Ramp(root, f, -hw + 1.75f, 3.5f, suiteZ, suite, suiteZ + 10f, mezz, LevelKit.Floor, true);
        Banister(root, f, -hw + 4.2f, suiteZ, suite, suiteZ + 10f, mezz);
        // THE MEZZANINE: the west strip from the stair's foot, the east strip past the cabaret; plinth lips under their
        // starts; rails; the ladder to the roof off the east strip's end
        float wz0 = suiteZ + 10f, ez0 = -hd + 25.5f, mz1 = vaultZ - 0.1f;
        Floor(root, f, new Vector3(-hw + 1.75f, mezz - 0.5f, (wz0 + mz1) * 0.5f), new Vector3(3.5f, 1f, mz1 - wz0), "Mezzanine");
        Floor(root, f, new Vector3(hw - 1.75f, mezz - 0.5f, (ez0 + mz1) * 0.5f), new Vector3(3.5f, 1f, mz1 - ez0), "Mezzanine");
        HouseKit.Rail(root, f.P(-hw + 3.4f, mezz, wz0 + 0.2f), f.P(-hw + 3.4f, mezz, mz1 - 0.2f), 1f);
        HouseKit.Rail(root, f.P(hw - 3.4f, mezz, ez0 + 0.2f), f.P(hw - 3.4f, mezz, mz1 - 0.2f), 1f);
        Lip(root, f, new Vector3(hw - 1.75f, 0.65f, ez0 - 1.8f), new Vector3(3.1f, 1.3f, 3f));   // before the east strip's start, past the stage: the corner boost up; the west strip is the stair's, the vault's top and the cage's roof
        Fuel(root, config, f, EnemyKind.Chaser, new Vector3(-hw + 1.75f, mezz, wz0 + 8f), "the mezzanine");
        Fuel(root, config, f, EnemyKind.Dummy, new Vector3(hw - 1.75f, mezz, ez0 + 6f), "the mezzanine");
        Corner(root, f, new Vector3(hw - 3.9f, h * 0.5f - 0.03f, vaultZ - 3f), new Vector3(0.6f, h - 0.06f, 3f), LevelKit.Charcoal);
        Kick(root, f, new Vector3(hw - 1.5f, mezz + 2.5f, vaultZ - 3f), new Vector3(0.6f, 5f, 3f));
        Flute(root, f, new Vector3(hw, h - 2f, vaultZ - 3f), Vector3.left, 4f);
        // THE GAMING FLOOR: the roulette inlay, the horseshoe of tables, the chandelier over the wheel
        Mark(root, f, new Vector3(0f, LevelKit.MarkProud, wheelZ), new Vector3(7.4f, 0.006f, 7.4f), LevelKit.Brass, "RouletteRing", PrimitiveType.Cylinder);
        Mark(root, f, new Vector3(0f, LevelKit.MarkProud + 0.004f, wheelZ), new Vector3(7f, 0.006f, 7f), LevelKit.Oxblood, "RouletteField", PrimitiveType.Cylinder);
        Mark(root, f, new Vector3(0f, LevelKit.MarkProud + 0.008f, wheelZ), new Vector3(0.12f, 0.006f, 7.2f), LevelKit.Brass, "RouletteLine");
        Mark(root, f, new Vector3(0f, LevelKit.MarkProud + 0.008f, wheelZ), new Vector3(7.2f, 0.006f, 0.12f), LevelKit.Brass, "RouletteLine");
        float[] angles = { 30f, 70f, 110f, 150f, 210f, 250f, 290f, 330f };
        for (int i = 0; i < angles.Length; i++)
        {
            float a = angles[i] * Mathf.Deg2Rad;
            Vector3 t = new Vector3(Mathf.Sin(a) * 7.5f, 0.45f, wheelZ + Mathf.Cos(a) * 7.5f);
            Lip(root, f, t, new Vector3(2.4f, 0.9f, 2.4f), LevelKit.WoodPanel, "Table");
            if (i % 3 == 0) Fuel(root, config, f, EnemyKind.Dummy, new Vector3(t.x * 0.75f, 0f, t.z), "the tables");
        }
        LevelKit.Chandelier(root, f.P(0f, 8.5f, wheelZ), f.origin.y + h);
        // THE CAGE: a bronze box behind brass bars, its counter a lip; the bouncers in front of it
        Box(root, f, new Vector3(cageX, 2f, vaultZ - 1.7f), new Vector3(7f, 4f, 3f), LevelKit.Bronze, "Cage");
        Lip(root, f, new Vector3(cageX, 0.55f, vaultZ - 3.75f), new Vector3(7f, 1.1f, 1f), LevelKit.WoodPanel, "CageCounter");
        for (int i = 0; i <= 12; i++) Mark(root, f, new Vector3(cageX - 3.3f + i * 0.55f, 2.55f, vaultZ - 3.24f), new Vector3(0.06f, 2.8f, 0.06f), LevelKit.Brass, "CageBar");
        LevelKit.Text(root, f.P(cageX, 3.6f, vaultZ - 3.28f), Quaternion.LookRotation(f.D(Vector3.forward)), "THE CAGE", 0.4f, new Color(2.2f, 1.7f, 0.4f), "CageName", 5f, true);
        for (int s = -1; s <= 1; s += 2) Fuel(root, config, f, EnemyKind.Warden, new Vector3(cageX + s * 2.5f, 0f, vaultZ - 6.5f), "the cage");
        // THE VAULT across the back, the doorway through it, its top a floor at 5 with the brass lip
        Box(root, f, new Vector3(-hw * 0.5f - 3.25f, 2.5f, (vaultZ + hd) * 0.5f), new Vector3(hw - 3.25f, 5f, hd - vaultZ), LevelKit.RivetIron, "Vault");
        Box(root, f, new Vector3(hw * 0.5f + 3.25f, 2.5f, (vaultZ + hd) * 0.5f), new Vector3(hw - 3.25f, 5f, hd - vaultZ), LevelKit.RivetIron, "Vault");
        Box(root, f, new Vector3(0f, 4.6f, (vaultZ + hd) * 0.5f), new Vector3(6.5f, 0.8f, hd - vaultZ), LevelKit.RivetIron, "VaultLintel");
        Mark(root, f, new Vector3(0f, 2.1f, vaultZ - 0.45f), new Vector3(7f, 4.4f, 0.1f), LevelKit.Brass, "VaultFrame");
        Lip(root, f, new Vector3(0f, 5.3f, vaultZ + 1.2f), new Vector3(hw * 2f - 2f, 0.6f, 2f), LevelKit.Brass, "VaultTop");
        // THE CABARET: the stage lip along the east wall, its footlights, the page over it
        Lip(root, f, new Vector3(hw - 2f, 0.6f, -hd + 16f), new Vector3(4f, 1.2f, 12f), LevelKit.WoodPanel, "Stage");
        for (int i = 0; i < 6; i++) Mark(root, f, new Vector3(hw - 4.2f, 1.26f, -hd + 11f + i * 2f), new Vector3(0.3f, 0.12f, 0.3f), LevelKit.LampCream, "Footlight");
        Mark(root, f, new Vector3(hw - 0.2f, 3.2f, -hd + 16f), new Vector3(0.3f, 4f, 11f), LevelKit.Oxblood, "StageCurtain");
        Fuel(root, config, f, EnemyKind.Turret, new Vector3(hw - 7f, 3.5f, -hd + 16f), "the cabaret");
        // The rest of the fuel, the lamps, the lines
        Fuel(root, config, f, EnemyKind.Sentry, new Vector3(3f, 0f, -hd + 3f), "the doors");
        PoolFuel(config, f, new Vector3(R(-3f, 3f), 0f, -hd + 11f), "the floor");
        for (int s = -1; s <= 1; s += 2) Lamp(root, f, new Vector3(s * 7f, h - 1.5f, -hd + 13f), h);
        Lamp(root, f, new Vector3(0f, h - 1.5f, vaultZ - 5f), h);
        Line(root, b, f, "the marquee", new Vector3(0f, 0f, -hd - 9f), new Vector3(0f, 0f, -hd - 5.6f), zone);
        Line(root, b, f, "the tables", new Vector3(0f, 0f, -hd + 1f), new Vector3(0f, 0f, -hd + 11f), zone, false);
        Line(root, b, f, "the vault", new Vector3(0f, 0f, vaultZ - 6f), new Vector3(0f, 0f, vaultZ - 0.5f), zone);
        // The roof, the facades, the marquee under the balcony, the sign
        Roof(root, f, h, hatch, 1, true);
        HouseFacades(b, f, h, 1, 4.4f, false, false, false);
        LevelKit.Marquee(root, f.P(0f, 0f, -hd - 0.5f), f.D(Vector3.back), 12f, string.IsNullOrEmpty(b.name) ? "THE HALCYON CASINO" : b.name, 5.2f, 5.4f);
        LevelKit.BladeSign(root, f.P(hw * 0.33f, h - 1f, -hd - 0.5f), f.D(Vector3.back), "CASINO", 1.1f);
        Entrance(plan, b, f, "the doors", new Vector3(0f, 0f, -hd), Vector3.back, EK.Door);
        Entrance(plan, b, f, "the balcony", new Vector3(0f, suite, -hd), Vector3.back, EK.Balcony, true);
        Entrance(plan, b, f, "the back door", new Vector3(0f, 0f, hd), Vector3.forward, EK.Door);
        Entrance(plan, b, f, "the stage door", new Vector3(hw, 0f, -hd + 16f), Vector3.right, EK.Door);
        Entrance(plan, b, f, "the roof", new Vector3(hatch.x, h + 1f, hatch.y), Vector3.forward, EK.RoofHatch, true, false);
        if (string.IsNullOrEmpty(b.name)) b.name = "THE HALCYON CASINO";
    }
}
