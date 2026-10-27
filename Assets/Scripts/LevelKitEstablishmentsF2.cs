using UnityEngine;
using Plan = CityDistrict.DistrictPlan;
using Block = CityDistrict.Block;

/// <summary>
/// THE RESTAURANT's pieces (LevelKitEstablishmentsF): the floors in pieces round the cellar's stair and the chute,
/// the roof deck round the dome's well and the hatch, the tables in rings, the stepped dome itself, the roof terrace.
/// Helpers are prefixed Rst so no other establishment's helper can collide with them in the partial.
/// </summary>
public static partial class Establishments
{
    /// The block floor in pieces: the restaurant DIGS (Establishments.Digs), so the dispatcher lays it none and the
    /// floor is laid here as the dining room's slab and four kitchen bands round the chute's opening (west) and the stair's
    static void RstFloors(Transform root, BlockFrame f, float passZ, float chuteZ, float chuteOpen, float stairRun)
    {
        float hw = f.hw, hd = f.hd, bw = f.blockHW, bd = f.blockHD;
        RstBand(root, f, -bd, passZ, -bw, bw);                                          // the dining room
        RstBand(root, f, passZ, chuteZ - 1.8f, -bw, bw);
        RstBand(root, f, chuteZ - 1.8f, chuteZ + 1.8f, -bw, -hw);                       // under the west wall, beside the chute's mouth
        RstBand(root, f, chuteZ - 1.8f, chuteZ + 1.8f, -hw + chuteOpen, bw);            // the chute's opening
        RstBand(root, f, chuteZ + 1.8f, hd - 4f, -bw, bw);
        RstBand(root, f, hd - 4f, hd - 0.5f, -bw, -hw + 2f);                            // the ledge at the stair's head
        RstBand(root, f, hd - 4f, hd - 0.5f, -hw + 2f + stairRun + 1.2f, bw);           // the stair's opening
        RstBand(root, f, hd - 0.5f, bd, -bw, bw);                                       // under the back wall
    }

    static void RstBand(Transform root, BlockFrame f, float z0, float z1, float x0, float x1)
    {
        if (x1 - x0 < 0.05f || z1 - z0 < 0.05f) return;
        Floor(root, f, new Vector3((x0 + x1) * 0.5f, -0.5f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, 1f, z1 - z0), "BlockFloor");
    }

    /// The roof deck (top at h + 1) in pieces round the dome's well (domeR square about (0, zc)) and the hatch
    static void RstRoofDeck(Transform root, BlockFrame f, float h, float domeR, float zc, Vector2 hatch)
    {
        float rw = f.hw + T, rd = f.hd + T, y = h + 0.5f;
        float ox0 = -domeR, ox1 = domeR, oz0 = zc - domeR, oz1 = zc + domeR;
        float hx0 = hatch.x - 1.5f, hx1 = hatch.x + 1.5f, hz0 = hatch.y - 1.5f, hz1 = hatch.y + 1.5f;
        RstDeck(root, f, -rw, ox0, -rd, rd, y);
        RstDeck(root, f, ox0, ox1, -rd, oz0, y);
        RstDeck(root, f, ox0, ox1, oz1, rd, y);
        RstDeck(root, f, ox1, rw, -rd, hz0, y);
        RstDeck(root, f, ox1, hx0, hz0, hz1, y);
        RstDeck(root, f, hx1, rw, hz0, hz1, y);
        RstDeck(root, f, ox1, rw, hz1, rd, y);
    }

    static void RstDeck(Transform root, BlockFrame f, float x0, float x1, float z0, float z1, float y)
    {
        if (x1 - x0 < 0.05f || z1 - z0 < 0.05f) return;
        LevelKit.Slab(root, f.P((x0 + x1) * 0.5f, y, (z0 + z1) * 0.5f), f.S(x1 - x0, 1f, z1 - z0), LevelKit.Charcoal, "RoofDeck");
    }

    /// A ring of tables (0.9 m lips) at radius `r` round (0, zc) at the angles given (none on the axes: the aisles
    /// along them stay 6 m clear); footmen beside every other table when `fuel`
    static void RstTables(Transform root, MovementConfig config, BlockFrame f, float zc, float r, float[] degs, bool fuel)
    {
        for (int i = 0; i < degs.Length; i++)
        {
            float a = degs[i] * Mathf.Deg2Rad, cx = Mathf.Cos(a), sz = Mathf.Sin(a);
            Lip(root, f, new Vector3(cx * r, 0.45f, zc + sz * r), new Vector3(2.2f, 0.9f, 2.2f), LevelKit.WoodPanel, "Table");
            if (fuel && i % 2 == 0) Fuel(root, config, f, EnemyKind.Dummy, new Vector3(cx * (r + 1.9f), 0f, zc + sz * (r + 1.9f)), "the tables");
        }
    }

    /// Brass nosings across a slope from local `a` to `b` every 1.6 m (the slope stays a slope)
    static void RstNosings(Transform root, BlockFrame f, Vector3 a, Vector3 b, float width, Quaternion rot)
    {
        Vector3 wa = f.P(a), wb = f.P(b);
        int n = Mathf.FloorToInt((wb - wa).magnitude / 1.6f);
        for (int i = 1; i < n; i++)
            LevelKit.Prim(root, Vector3.Lerp(wa, wb, i / (float)n) + rot * Vector3.up * 0.008f, new Vector3(width - 0.2f, 0.008f, 0.09f), LevelKit.Brass, "Nosing", PrimitiveType.Cube, rot);
    }

    /// THE STEPPED DOME: the well glazed at the ceiling (breakable: the drop from the oculus lands on it at twenty
    /// metres a second and goes through), `tiers` square ring tiers over it, each a lip stepping in 1.2 m and up 1.2,
    /// corbelled inward so the ceiling you look up at is the stepped underside; the oculus the last ring's hole; a
    /// brass band round the drum; the lamp-cream light in the lantern
    static void RstDome(Transform root, MovementConfig config, BlockFrame f, float h, float domeR, float zc, int tiers, float ringW, float tierH)
    {
        LevelKit.Glass(root, f.P(0f, h + 0.15f, zc), f.S(domeR * 2f - 0.6f, 0.3f, domeR * 2f - 0.6f), config);
        for (int k = 0; k < tiers; k++)
        {
            float o = domeR - 1.2f * k, i = o - ringW, yc = h + 1f + tierH * (k + 0.5f);
            Lip(root, f, new Vector3(0f, yc, zc - o + ringW * 0.5f), new Vector3(o * 2f, tierH, ringW), LevelKit.Limestone, "DomeTier");
            Lip(root, f, new Vector3(0f, yc, zc + o - ringW * 0.5f), new Vector3(o * 2f, tierH, ringW), LevelKit.Limestone, "DomeTier");
            Lip(root, f, new Vector3(-o + ringW * 0.5f, yc, zc), new Vector3(ringW, tierH, i * 2f), LevelKit.Limestone, "DomeTier");
            Lip(root, f, new Vector3(o - ringW * 0.5f, yc, zc), new Vector3(ringW, tierH, i * 2f), LevelKit.Limestone, "DomeTier");
        }
        float band = h + 1f + tierH * 0.5f;
        LevelKit.Prim(root, f.P(0f, band, zc - domeR - 0.02f), f.S(domeR * 2f + 0.1f, 0.14f, 0.03f), LevelKit.Brass, "DrumBand");
        LevelKit.Prim(root, f.P(0f, band, zc + domeR + 0.02f), f.S(domeR * 2f + 0.1f, 0.14f, 0.03f), LevelKit.Brass, "DrumBand");
        LevelKit.Prim(root, f.P(-domeR - 0.02f, band, zc), f.S(0.03f, 0.14f, domeR * 2f + 0.1f), LevelKit.Brass, "DrumBand");
        LevelKit.Prim(root, f.P(domeR + 0.02f, band, zc), f.S(0.03f, 0.14f, domeR * 2f + 0.1f), LevelKit.Brass, "DrumBand");
        var l = LevelKit.Light(root, f.P(0f, h + 1f + tierH * tiers - 1.5f, zc), 26f, 1.8f);
        l.color = new Color(1f, 0.88f, 0.66f);
    }

    /// THE ROOF: the parapets and the stair head (Roof), the TERRACE over the kitchens (pergola lips, a hedge along
    /// its front, lamp standards, a footman), the lit line from the fire escape's parapet to the drum
    static void RstRoof(Transform root, MovementConfig config, BlockFrame f, Block b, Plan plan, float h, Vector2 hatch, float passZ, float zc, float domeR, float hw, float hd)
    {
        Roof(root, f, h, hatch, 0, false);
        float tz = (passZ + hd) * 0.5f;
        for (int s = -1; s <= 1; s += 2) Lip(root, f, new Vector3(s * hw * 0.45f, h + 1.6f, tz), new Vector3(6f, 1.2f, 2.6f), LevelKit.WoodPanel, "Pergola");
        LevelKit.Hedge(root, f.P(0f, h + 1f, passZ + 1.2f), f.S(hw * 1.1f, 0.9f, 0.8f));
        LevelKit.LampStandard(root, f.P(-hw * 0.8f, h + 1f, hd - 3f), 3.2f, true);
        LevelKit.LampStandard(root, f.P(hw * 0.8f, h + 1f, passZ + 2.5f), 3.2f, false);
        Fuel(root, config, f, EnemyKind.Dummy, new Vector3(0f, h + 1f, tz), "the terrace");
        Line(root, b, f, "the dome", new Vector3(hw - 2f, h + 1f, zc), new Vector3(domeR + 0.6f, h + 1f, zc), plan.zone);
    }
}
