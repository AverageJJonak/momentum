using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE DENSE CITY. A city as dense as Gotham's has no bare roof and no bare
/// gap: this half fills both. EVERY ROOF gets its furniture (a brick stair head
/// with a lit door, vent cowls in a row, a water tank on legs, a chimney, a
/// mast with a red lamp, a skylight, iron rails); the shafts stand on stone
/// PODIUMS with shop arcades; rows of brick and brownstone tenements fill every
/// gap between the rings; the belt gets kiosks under bronze canopies. The outer
/// districts (OuterBlock) get the same: a building, two or three low blocks
/// round it in the same cell, furniture on all of it. The furniture of the last
/// tower built is handed back (LastFurniture) so a roof you run on (Level 12)
/// can make it solid. All welded, all deterministic.
/// </summary>
public static partial class MenuCity
{
    /// A piece of roof furniture: where it stands, how big, which way (for whoever makes it solid)
    public struct Placed { public Vector3 centre, size; public Quaternion rot; }

    /// What the last OuterTower put on its roofs
    public static readonly List<Placed> LastFurniture = new List<Placed>();
    static bool recordFurniture;

    static Material cowlMat, ventMat, tankMat, cabinMat, railMat, fasciaAmber, fasciaCream, kioskMat;

    static void DenseInit()
    {
        cowlMat = Flat(new Color(0.22f, 0.22f, 0.24f), 0.5f);
        ventMat = Flat(new Color(0.14f, 0.14f, 0.16f), 0.4f);
        tankMat = Flat(new Color(0.2f, 0.19f, 0.2f), 0.45f);
        cabinMat = Flat(new Color(0.34f, 0.17f, 0.12f), 0.25f);   // brick
        railMat = Flat(new Color(0.18f, 0.18f, 0.2f), 0.5f);     // iron
        kioskMat = Flat(new Color(0.74f, 0.68f, 0.56f), 0.4f);   // cream plaster
        // The fascias: amber and cream, the shopfronts' own light
        fasciaAmber = Glow(G(new Color(1.6f, 1.15f, 0.55f)));
        fasciaCream = Glow(G(new Color(1.6f, 1.45f, 1.15f)));
    }

    // ------------------------------------------------------------ roofs

    /// What stands on a roof. `top` is the roof's centre, `w` across the building's x and `d` along its z, turned by
    /// `az`. `level` 0 is a house (a vent, a tank or a chimney), 1 a tower (cowls in a row, a stair head, a mast,
    /// rails), 2 a big roof (all of it, and a water tower, a chimney, a skylight). A lane down the middle and a margin
    /// at the rim stay clear: bridges land at rims and pads sit on the axis.
    static void RoofFurniture(MeshBaker boxes, Vector3 top, float w, float d, float az, int seed, int level)
    {
        if (w < 5f || d < 3.6f || cowlMat == null) return;
        if (Cut(top, Mathf.Max(w, d) * 0.5f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        float hw = w * 0.5f - 2.2f, hd = d * 0.5f - 1.6f, lane = w * 0.18f;
        float H(int k) => CityTextures.Hash01(seed, k, 11);
        void Put(Material m, float lx, float lz, float sx, float sy, float sz, PrimitiveType type = PrimitiveType.Cube, float yaw = 0f, float lift = 0f)
        {
            Quaternion rot = turn * Quaternion.Euler(0f, yaw, 0f);
            Vector3 c = top + turn * new Vector3(lx, lift + sy * 0.5f, lz);
            Box(boxes, m, c, new Vector3(sx, sy, sz), rot, type);
            if (recordFurniture && sy >= 0.4f) LastFurniture.Add(new Placed { centre = c, size = new Vector3(sx, sy, sz), rot = rot });
        }
        // A water tank on legs: the one thing on every Gotham roof
        void Tank(float lx, float lz, float dia, float h)
        {
            float legH = h * 0.4f, tankH = h * 0.45f;
            for (int k = 0; k < 4; k++) Put(railMat, lx + (k % 2 == 0 ? -1f : 1f) * dia * 0.32f, lz + (k < 2 ? -1f : 1f) * dia * 0.32f, 0.2f, legH, 0.2f);
            Put(tankMat, lx, lz, dia, tankH, dia, PrimitiveType.Cylinder, 0f, legH);
            Put(roofSlate, lx, lz, dia * 0.7f, h * 0.15f, dia * 0.7f, PrimitiveType.Cylinder, 0f, legH + tankH);
        }
        if (level == 0)
        {
            // A house: a vent, and a tank or a chimney
            float sx = H(1) > 0.5f ? 1f : -1f;
            Put(ventMat, sx * Mathf.Min(hw, 3f), -hd * 0.4f, 0.5f, 1.8f, 0.5f, PrimitiveType.Cylinder);
            if (H(2) > 0.5f) Tank(-sx * Mathf.Min(hw, 3f), hd * 0.3f, 1.8f, 3.6f);
            else { Put(cabinMat, -sx * Mathf.Min(hw, 3f), hd * 0.3f, 1f, 2.6f, 1f); Put(ventMat, -sx * Mathf.Min(hw, 3f), hd * 0.3f, 1.3f, 0.25f, 1.3f, PrimitiveType.Cube, 0f, 2.6f); }
            return;
        }
        // The rails round the rim: not on a roof somebody else rails (Level 12 parapets its own)
        if (!recordFurniture)
        {
            Put(railMat, -(w * 0.5f - 0.2f), 0f, 0.08f, 1.1f, d - 0.4f);
            Put(railMat, w * 0.5f - 0.2f, 0f, 0.08f, 1.1f, d - 0.4f);
            Put(railMat, 0f, -(d * 0.5f - 0.2f), w - 0.4f, 1.1f, 0.08f);
            Put(railMat, 0f, d * 0.5f - 0.2f, w - 0.4f, 1.1f, 0.08f);
        }
        // The stair head, in a corner, brick, its door lit
        float cx = H(3) > 0.5f ? hw - 1.6f : -hw + 1.6f;
        if (Mathf.Abs(cx) > lane + 1.5f)
        {
            Put(cabinMat, cx, -hd + 1.7f, 2.8f, 2.7f, 3f);
            Put(glowWarm, cx + (cx > 0f ? -1.5f : 1.5f), -hd + 1.7f, 0.12f, 1.6f, 0.8f, PrimitiveType.Cube, 0f, 0.3f);
        }
        // Vent cowls in a row along the far edge, out of the lane
        int units = Mathf.Clamp(Mathf.FloorToInt(w / 4.5f), 2, 7);
        for (int k = 0; k < units; k++)
        {
            float lx = Mathf.Lerp(-hw + 1.2f, hw - 1.2f, units == 1 ? 0.5f : k / (float)(units - 1));
            if (Mathf.Abs(lx) < lane) continue;
            Put(cowlMat, lx, hd - 1f, 0.9f, 1.6f, 0.9f, PrimitiveType.Cylinder);
            Put(ventMat, lx, hd - 1f, 1.3f, 0.5f, 1.3f, PrimitiveType.Cylinder, 0f, 1.6f);
        }
        // Chimneys, one or two
        Put(cabinMat, -hw + 0.8f, H(4) * hd * 0.8f, 0.9f, 2.6f, 0.9f);
        if (H(5) > 0.4f) Put(cabinMat, hw - 0.8f, -H(6) * hd * 0.8f, 1f, 3.2f, 1f);
        // The mast: a pole, two arms, the red lamp on top
        if (level >= 2 || H(7) > 0.45f)
        {
            float mx = H(8) > 0.5f ? hw - 0.9f : -hw + 0.9f;
            Put(ventMat, mx, hd - 0.9f, 0.16f, 7f, 0.16f, PrimitiveType.Cylinder);
            Put(railMat, mx, hd - 0.9f, 1.6f, 0.12f, 0.12f, PrimitiveType.Cube, 0f, 4.8f);
            Put(glowRed, mx, hd - 0.9f, 0.4f, 0.4f, 0.4f, PrimitiveType.Cube, 0f, 7f);
        }
        // A cable tray across the roof
        Put(ventMat, lane + 0.6f, 0f, 0.4f, 0.12f, d - 3.6f);
        if (level < 2) return;
        // The water tower, the second chimney, the skylight
        float tx = -hw + 2.4f;
        if (Mathf.Abs(tx) > lane + 1.4f) Tank(tx, hd * 0.2f, 2.8f, 5.5f);
        if (H(9) > 0.35f) Put(cabinMat, lane + 2.2f, -hd * 0.45f, 1.2f, 4f, 1.2f);
        if (H(10) < 0.5f && d > 12f) Put(glowWhite, 0f, hd * 0.55f, Mathf.Min(6f, w * 0.3f), 0.5f, 3f);
    }

    /// A podium under a tower: a stone plinth half again its footprint with a shop arcade lit round its foot, a cornice
    /// with a lamp lip, planters and cowls on its roof
    static void Podium(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float az)
    {
        float pw = w * 1.5f, pd = d * 1.5f, ph = 9f;
        if (Cut(foot, Mathf.Max(pw, pd) * 0.5f)) return;
        Tier(sheets, foot, pw, pd, ph, arcade, true, az);
        Cornice(boxes, foot + Vector3.up * ph, pw, pd, az, stoneLime, glowWarm, 2);
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        Vector3 top = foot + Vector3.up * (ph + 0.9f);
        for (int s = -1; s <= 1; s += 2)
        {
            Box(boxes, foliage, top + turn * new Vector3(s * (w * 0.5f + (pw - w) * 0.25f), 0.9f, 0f), new Vector3((pw - w) * 0.42f, 1.8f, pd * 0.9f), turn);
            Box(boxes, foliage, top + turn * new Vector3(0f, 0.9f, s * (d * 0.5f + (pd - d) * 0.25f)), new Vector3(w * 0.9f, 1.8f, (pd - d) * 0.42f), turn);
        }
        for (int k = 0; k < 3; k++)
            Box(boxes, cowlMat, top + turn * new Vector3((k - 1) * (w * 0.3f), 0.6f, pd * 0.5f - 1.6f), new Vector3(1f, 1.2f, 1f), turn, PrimitiveType.Cylinder);
    }

    // ------------------------------------------------------------ between the rings

    /// The belt's kiosks: a cream box under a bronze canopy on posts, a strip of light along the canopy, two per avenue sector
    static void Pavilions(MeshBaker boxes, SheetBaker sheets)
    {
        for (int a = 0; a < Avenues; a++)
            for (int k = 0; k < 2; k++)
            {
                float az = (a + 0.5f) * 360f / Avenues + (k == 0 ? -4f : 4f);
                float r = k == 0 ? 432f : 522f;
                Vector3 foot = Polar(r, az);
                if (Cut(foot, 6f)) continue;
                Quaternion turn = Quaternion.Euler(0f, az, 0f);
                Tier(sheets, foot, 6f, 4f, 4.2f, cream, true, az);
                Box(boxes, bronze, foot + Vector3.up * 5.6f, new Vector3(10f, 0.25f, 8f), turn);
                for (int sx = -1; sx <= 1; sx += 2) for (int sz = -1; sz <= 1; sz += 2)
                    Box(boxes, bronze, foot + turn * new Vector3(sx * 4.6f, 2.8f, sz * 3.6f), new Vector3(0.2f, 5.6f, 0.2f), turn);
                Box(boxes, glowWarm, foot + turn * new Vector3(0f, 5.4f, 3.9f), new Vector3(10f, 0.12f, 0.12f), turn);
            }
    }

    /// Everything between the rings: the rows, the kiosks
    static void DenseCity(MeshBaker boxes, SheetBaker sheets)
    {
        float[] mr = { 665f, 705f, 770f, 812f, 962f, 1012f, 1078f, 1132f, 1185f, 1248f, 1300f };
        float[] mh = { 18f, 20f, 22f, 24f, 24f, 26f, 26f, 28f, 28f, 30f, 30f };
        for (int i = 0; i < mr.Length; i++) Tenements(boxes, sheets, mr[i], mh[i], 15f, i);   // the rows between the rings (MenuCityNight)
        Pavilions(boxes, sheets);
    }

    // ------------------------------------------------------------ the outer districts

    /// A cell of the outer city: the building, two or three low blocks round it, furniture on every roof. `cellHalf`
    /// is half the grid pitch. Returns the building's roof height.
    public static float OuterBlock(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, float az, int variant, bool lit, float cellHalf)
    {
        float roof = OuterTower(boxes, sheets, foot, w, d, h, az, variant, lit);
        if (cellHalf <= 0f) return roof;   // the far cells: the building alone
        int n = cellHalf > 60f ? 3 : cellHalf > 45f ? 2 : 1;
        int seed = Mathf.RoundToInt(foot.x * 0.37f + foot.z * 0.11f);
        for (int k = 0; k < n; k++)
        {
            float around = az + 90f + k * (360f / (n + 1)) + 20f * CityTextures.Hash01(seed, k, 3);
            float reach = Mathf.Max(w, d) * 0.5f + 10f + cellHalf * 0.35f * CityTextures.Hash01(seed, k, 4);
            Vector3 bf = foot + Radial(reach, around);
            float bw = 16f + 14f * CityTextures.Hash01(seed, k, 5), bd = 14f + 10f * CityTextures.Hash01(seed, k, 6);
            float bh = Mathf.Min(h * 0.55f, 16f + 24f * CityTextures.Hash01(seed, k, 7));
            if (Cut(bf, Mathf.Max(bw, bd) * 0.5f)) continue;
            Quaternion turn = Quaternion.Euler(0f, az, 0f);
            Skin skin = !lit ? plain : k % 3 == 0 ? brick : k % 3 == 1 ? brownstone : limestone;
            Tier(sheets, bf, bw, bd, bh, skin, lit, az, BlindSides);
            Box(boxes, skin == limestone || skin == plain ? stoneLime : brownFlat, bf + Vector3.up * (bh + 0.3f), new Vector3(bw + 1f, 0.6f, bd + 1f), turn);   // the cornice
            if (lit) Box(boxes, fasciaAmber, bf + Radial(bd * 0.5f + 0.2f, az) + Vector3.up * 3.8f, new Vector3(bw * 0.8f, 0.5f, 0.14f), turn);   // the shopfront's light
            if (lit && k == 1) FireEscape(boxes, bf, bw, bd, bh, az);
            RoofFurniture(boxes, bf + Vector3.up * bh, bw, bd, az, seed + k * 31, 1);
        }
        return roof;
    }
}
