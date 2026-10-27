using UnityEngine;
using Plan = CityDistrict.DistrictPlan;
using Block = CityDistrict.Block;
using EK = CityDistrict.EntranceKind;

/// <summary>
/// THE ESTABLISHMENTS: the DEPARTMENT STORE (an atrium, escalators as banister slides, floors as galleries, a display
/// window out) and the BANK (a hall of columns, a vault, the roof via the columns).
/// </summary>
public static partial class Establishments
{
    /// DEPARTMENT STORE. Three floors as GALLERIES round an ATRIUM (a 10 × 10 well through them to a skylight in the
    /// roof), ESCALATORS between the floors — brass ramps you slide (down) or run (up), a banister beside each — on
    /// alternate sides of the well, counters (lips) on every floor, a chandelier in the atrium, the DISPLAY WINDOW on
    /// the street (glass: at speed it is a door), the front doors, a loading dock at the back (its platform a lip), the
    /// roof by the top escalator and the skylight's hatch. MAIN: doors → the ground floor → the dock. FAST: the window,
    /// escalator to escalator to the roof, or straight up the well on its kick walls and the chandelier.
    /// FUEL: a floorwalker at the doors, shopgirls behind the counters, footmen on the galleries, a page in the well.
    static void Store(Transform root, MovementConfig config, Plan plan, Block b, BlockFrame f)
    {
        const float storey = 4.6f; const int floors = 3;
        float h = storey * floors, hw = f.hw, hd = f.hd, well = Mathf.Min(5f, hw - 7f);
        Vector3 min = new Vector3(-hw, 0f, -hd), max = new Vector3(hw, h, hd);
        Vector2 hatch = new Vector2(0f, 0f);   // the skylight's open pane over the well
        Room(root, f, min, max, LevelKit.Faces.North | LevelKit.Faces.South | LevelKit.Faces.East | LevelKit.Faces.West,
            Door(f, Face.South, hw * 0.45f, 0f, min, max), Door(f, Face.North, 0f, 0f, min, max, 7f, 5f),
            Window(f, Face.South, -hw * 0.3f, 10f, 0.6f, 4f, min, max));
        LevelKit.Glass(root, f.P(-hw * 0.3f, 2.3f, -hd), f.S(9.8f, 3.4f, 0.3f), config);
        // The roof deck round the skylight (the well's opening, glass-less: the way in from the roof), the roof's lips
        float rw = hw + T, rd = hd + T;
        LevelKit.Slab(root, f.P((-rw - well) * 0.5f, h + 0.5f, 0f), f.S(rw - well, 1f, rd * 2f), LevelKit.Charcoal, "RoofDeck");
        LevelKit.Slab(root, f.P((rw + well) * 0.5f, h + 0.5f, 0f), f.S(rw - well, 1f, rd * 2f), LevelKit.Charcoal, "RoofDeck");
        LevelKit.Slab(root, f.P(0f, h + 0.5f, (-rd - well) * 0.5f), f.S(well * 2f, 1f, rd - well), LevelKit.Charcoal, "RoofDeck");
        LevelKit.Slab(root, f.P(0f, h + 0.5f, (rd + well) * 0.5f), f.S(well * 2f, 1f, rd - well), LevelKit.Charcoal, "RoofDeck");
        for (int s = -1; s <= 1; s += 2)
        {
            LevelKit.Parapet(root, f.P(s * (well + 0.35f), h + 1.45f, 0f), f.S(0.7f, 0.9f, well * 2f + 1.4f));
            LevelKit.Parapet(root, f.P(0f, h + 1.45f, s * (well + 0.35f)), f.S(well * 2f, 0.9f, 0.7f));
        }
        // The galleries: four slabs a floor round the well, brass rails, counters, the escalators on alternate sides
        for (int k = 1; k < floors; k++)
        {
            float y = k * storey;
            int side = k % 2 == 0 ? -1 : 1;
            float escZ0 = -hd + 4f, escZ1 = escZ0 + storey / Mathf.Tan(24f * Mathf.Deg2Rad);
            Floor(root, f, new Vector3((-hw - well) * 0.5f, y - 0.5f, side < 0 ? (hd + escZ1) * 0.5f : 0f), new Vector3(hw - well, 1f, side < 0 ? hd - escZ1 : hd * 2f), "Gallery");   // the escalator's top meets its edge
            Floor(root, f, new Vector3((hw + well) * 0.5f, y - 0.5f, side > 0 ? (hd + escZ1) * 0.5f : 0f), new Vector3(hw - well, 1f, side > 0 ? hd - escZ1 : hd * 2f), "Gallery");
            Floor(root, f, new Vector3(0f, y - 0.5f, (-hd - well) * 0.5f), new Vector3(well * 2f, 1f, hd - well), "Gallery");
            Floor(root, f, new Vector3(0f, y - 0.5f, (hd + well) * 0.5f), new Vector3(well * 2f, 1f, hd - well), "Gallery");
            // The escalator up to this floor: along the side wall from the floor below, its banister beside it
            float ex = side * (hw - 2.6f);
            var esc = Ramp(root, f, ex, 3.2f, escZ0, y - storey, escZ1, y, LevelKit.Brass);
            esc.name = "BanisterRamp";
            Banister(root, f, ex - side * 2.3f, escZ0, y - storey, escZ1, y);
            Mark(root, f, new Vector3(ex - side * 1.65f, y - storey * 0.5f + 0.6f, (escZ0 + escZ1) * 0.5f), new Vector3(0.1f, 1.2f, escZ1 - escZ0), LevelKit.Pane, "EscalatorSide");
            for (int s = -1; s <= 1; s += 2) LevelKit.Parapet(root, f.P(s * (well + 0.35f), y + 0.45f, 0f), f.S(0.7f, 0.9f, well * 2f + 1.4f));
            Lip(root, f, new Vector3(-side * hw * 0.5f, y + 0.5f, hd * 0.5f), new Vector3(6f, 1f, 1.4f), LevelKit.WoodPanel, "Counter");
            Fuel(root, config, f, EnemyKind.Dummy, new Vector3(-side * hw * 0.5f, y, hd * 0.5f - 2f), "floor " + k);
            Fuel(root, config, f, EnemyKind.Chaser, new Vector3(ex, y, escZ1 + 2f), "escalator " + k);
            Lamp(root, f, new Vector3(side * hw * 0.5f, y + storey - 1f, 0f), y + storey);
        }
        // The top escalator to the roof: up the west side of the well itself, from the top gallery's south edge to the
        // skylight's north parapet (hop it onto the roof)
        var top = Ramp(root, f, -well + 1.6f, 3.2f, -well - 0.3f, h - storey, well, h + 1f, LevelKit.Brass);
        top.name = "BanisterRamp";
        for (int s = -1; s <= 1; s += 2) Flute(root, f, new Vector3(s * well, h * 0.5f, 0f), Vector3.left * s, 4f, 0.5f);
        LevelKit.Chandelier(root, f.P(0f, h - 0.5f, 0f), f.origin.y + h + 1f, 2.2f);
        Pad(root, f, new Vector3(0f, 0f, 0f), Mathf.Sqrt(2f * 25f * (storey + 1.2f)), 1f);
        Lip(root, f, new Vector3(0f, 0.55f, -hd + 4f), new Vector3(8f, 1.1f, 1.4f), LevelKit.WoodPanel, "Counter");
        Lip(root, f, new Vector3(0f, 0.6f, hd - 1.6f), new Vector3(hw * 1.2f, 1.2f, 3f), LevelKit.RivetIron, "LoadingDock");
        Fuel(root, config, f, EnemyKind.Sentry, new Vector3(hw * 0.45f, 0f, -hd + 3f), "the doors");
        Fuel(root, config, f, EnemyKind.Turret, new Vector3(0f, storey * 1.5f, 0f), "the well");
        Line(root, b, f, "the window", new Vector3(-hw * 0.3f, 0f, -hd - 8f), new Vector3(-hw * 0.3f, 0f, -hd - 1f), plan.zone);
        Line(root, b, f, "the escalators", new Vector3(hw - 2.6f, 0f, -hd + 1f), new Vector3(hw - 2.6f, 0f, -hd + 3.6f), plan.zone, false);
        Facades(root, b, f, h, floors - 1, "HALCYON & CO.", new Vector2(-hw * 0.3f, 11f), false);   // the fascia and the awning over the display window
        LevelKit.ClockFace(root, f.P(hw - 2f, 9f, -hd - 0.45f), f.D(Vector3.back), 2.4f);
        Entrance(plan, b, f, "the front doors", new Vector3(hw * 0.45f, 0f, -hd), Vector3.back, EK.Door);
        Entrance(plan, b, f, "the display window", new Vector3(-hw * 0.3f, 0.6f, -hd), Vector3.back, EK.Window, true);
        Entrance(plan, b, f, "the loading dock", new Vector3(0f, 0f, hd), Vector3.forward, EK.Door);
        Entrance(plan, b, f, "the skylight", new Vector3(0f, h + 1f, 0f), Vector3.forward, EK.Skylight, true, false);
        if (string.IsNullOrEmpty(b.name)) b.name = "HALCYON & CO.";
    }

    /// BANK. A PORTICO of six limestone columns on the street (boost corners: up them to the portico's roof at 8,
    /// from it to the main roof at 12), a BANKING HALL twelve metres tall with two rows of fluted columns (corners), the
    /// counter (a lip) across it, the VAULT at the back (a thick box whose top is a floor 5 m up, its door a 6.5 m
    /// opening), galleries along both long walls at 6 reached by the vault's top or a corner boost on the counter, the
    /// side door west, the roof hatch over the east gallery's stair. MAIN: doors → the hall → through the vault → out
    /// the back. FAST: the columns outside to the roof; inside, counter → gallery → the hatch.
    /// FUEL: a guard at the doors, tellers behind the counter, clerks on the galleries, a page over the hall.
    static void Bank(Transform root, MovementConfig config, Plan plan, Block b, BlockFrame f)
    {
        const float h = 12f, gy = 6f, portico = 8f;
        float hw = f.hw, hd = f.hd, vaultZ = hd - 7f;
        Vector3 min = new Vector3(-hw, 0f, -hd), max = new Vector3(hw, h, hd);
        Vector2 hatch = new Vector2(hw - 3f, hd - 3f);
        Shell(root, f, h, hatch, Door(f, Face.South, 0f, 0f, min, max, 8f, 6f), Door(f, Face.North, 0f, 0f, min, max), Door(f, Face.West, -hd * 0.3f, 0f, min, max));
        // The portico: columns 8 m tall on a stylobate, an entablature over them (its top a platform), a lip up to the roof
        float pw = Mathf.Min(hw * 1.4f, 20f);
        LevelKit.Slab(root, f.P(0f, 0.2f, -hd - 3.2f), f.S(pw + 2f, 0.4f, 5.6f), LevelKit.Limestone, "Stylobate");
        for (int i = 0; i < 6; i++)
        {
            float x = -pw * 0.5f + pw * i / 5f;
            Corner(root, f, new Vector3(x, 0.4f + portico * 0.5f, -hd - 4.2f), new Vector3(1.1f, portico, 1.1f), LevelKit.Limestone);
        }
        LevelKit.Slab(root, f.P(0f, portico + 0.9f, -hd - 2.9f), f.S(pw + 2.2f, 1f, 6f), LevelKit.Limestone, "Entablature");
        LevelKit.Parapet(root, f.P(0f, portico + 1.85f, -hd - 5.55f), f.S(pw + 2f, 0.9f, 0.7f));
        Lip(root, f, new Vector3(0f, h + 1.6f, -hd + 1.4f), new Vector3(pw * 0.6f, 1.2f, 2.4f), LevelKit.Limestone, "Pediment");   // on the roof's edge: the step from the portico's top (9.4) to it
        Line(root, b, f, "the columns", new Vector3(-pw * 0.5f + 4f, 0f, -hd - 9f), new Vector3(-pw * 0.5f + 1f, 0f, -hd - 5.5f), plan.zone);
        // The hall: two rows of fluted columns, the counter, the galleries, the vault
        for (float z = -hd + 6f; z < vaultZ - 3f; z += 7f)
            for (int s = -1; s <= 1; s += 2) Corner(root, f, new Vector3(s * hw * 0.45f, h * 0.5f - 0.03f, z), new Vector3(1.3f, h - 0.06f, 1.3f), LevelKit.Fluted);
        Lip(root, f, new Vector3(0f, 0.6f, -hd + 12f), new Vector3(hw * 1.4f, 1.2f, 1.4f), LevelKit.WoodPanel, "Counter");
        for (int s = -1; s <= 1; s += 2)
        {
            Floor(root, f, new Vector3(s * (hw - 1.75f), gy - 0.5f, (-hd + vaultZ) * 0.5f), new Vector3(3.5f, 1f, vaultZ + hd - 6f), "Gallery");
            Mark(root, f, new Vector3(s * (hw - 3.55f), gy + 0.5f, (-hd + vaultZ) * 0.5f), new Vector3(0.08f, 1f, vaultZ + hd - 6f), LevelKit.Brass, "GalleryRail");
            Fuel(root, config, f, EnemyKind.Dummy, new Vector3(s * (hw - 1.75f), gy, 0f), s < 0 ? "west gallery" : "east gallery");
            Fuel(root, config, f, EnemyKind.Chaser, new Vector3(s * 3f, 0f, -hd + 14f), "behind the counter");
        }
        // The vault: a solid-walled box across the back with a 6.5 m doorway through it, its top a floor at 5
        Box(root, f, new Vector3(-hw * 0.5f - 3.25f, 2.5f, (vaultZ + hd) * 0.5f), new Vector3(hw - 3.25f, 5f, hd - vaultZ), LevelKit.RivetIron, "Vault");
        Box(root, f, new Vector3(hw * 0.5f + 3.25f, 2.5f, (vaultZ + hd) * 0.5f), new Vector3(hw - 3.25f, 5f, hd - vaultZ), LevelKit.RivetIron, "Vault");
        Box(root, f, new Vector3(0f, 4.6f, (vaultZ + hd) * 0.5f), new Vector3(6.5f, 0.8f, hd - vaultZ), LevelKit.RivetIron, "VaultLintel");
        Mark(root, f, new Vector3(0f, 2.1f, vaultZ - 0.45f), new Vector3(7f, 4.4f, 0.1f), LevelKit.Brass, "VaultFrame");
        Lip(root, f, new Vector3(0f, 5.3f, vaultZ + 1.2f), new Vector3(hw * 2f - 2f, 0.6f, 2f), LevelKit.Brass, "VaultTop");
        Ramp(root, f, hw - 1.75f, 3.5f, vaultZ - 2f, gy, vaultZ + 3f, 5.6f, LevelKit.Floor, true);   // the east gallery down onto the vault's top
        Kick(root, f, new Vector3(hw - 3f - 1.6f, gy + 2.5f, hd - 3f), new Vector3(0.6f, 5f, 3f));   // the ladder to the hatch, over the vault
        Flute(root, f, new Vector3(hw, gy + 2.5f, hd - 3f), Vector3.left, 4f);
        LevelKit.Chandelier(root, f.P(0f, h - 3.5f, -hd + 7f), f.origin.y + h);
        Fuel(root, config, f, EnemyKind.Sentry, new Vector3(0f, 0f, -hd + 3f), "the doors");
        Fuel(root, config, f, EnemyKind.Turret, new Vector3(0f, 5f, 0f), "over the hall");
        Line(root, b, f, "the counter", new Vector3(0f, 0f, -hd + 2f), new Vector3(0f, 0f, -hd + 10.5f), plan.zone, false);
        Roof(root, f, h, hatch, 0, false);
        Facades(root, b, f, h, 1, null, null);
        LevelKit.ClockFace(root, f.P(0f, h - 1.5f, -hd - 0.45f), f.D(Vector3.back), 3f);
        Entrance(plan, b, f, "the doors", new Vector3(0f, 0f, -hd), Vector3.back, EK.Door);
        Entrance(plan, b, f, "the portico roof", new Vector3(0f, portico + 1.4f, -hd - 3f), Vector3.back, EK.Balcony, true);
        Entrance(plan, b, f, "the back door", new Vector3(0f, 0f, hd), Vector3.forward, EK.Door);
        Entrance(plan, b, f, "the side door", new Vector3(-hw, 0f, -hd * 0.3f), Vector3.left, EK.Door);
        Entrance(plan, b, f, "the roof", new Vector3(hatch.x, h + 1f, hatch.y), Vector3.back, EK.RoofHatch, true);
        if (string.IsNullOrEmpty(b.name)) b.name = "THE FIRST HALCYON BANK";
    }
}
