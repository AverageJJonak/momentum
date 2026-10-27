using UnityEngine;

/// <summary>
/// THE HOUSE, Halcyon's tower, a DECO CATHEDRAL OF COMMERCE (2026-10-07; it was
/// a black monolith with a ring on its shoulders). Its STONE is here, in plan
/// units that CityKit.Tower scales (the menu shows it three and a half times
/// the plan: 154 by 119 metres at the foot, 665 high), so the menu's offline
/// harness builds the same House box for box without a port: a charcoal body
/// in FIVE TIERS stepping back from sixty per cent of its height, black marble
/// piers on the corners of the first, LIMESTONE FLUTED PIERS up every face of
/// every tier with the windows only in the bays between them (HouseLight
/// hangs those, punched in the stone and lit in runs), a monumental colonnade
/// and a great arched entrance at the foot of the south face, a stepped BRASS
/// CROWN with gilt lips and ribs, a cream LANTERN on it, a slim mast with the
/// aircraft lamp, and the brass cartouche high on the south face that the
/// company's mark sits in (CityKit keeps the mark and the name: they breathe).
/// Partial of MenuCity, compiled by the menu harness too.
/// </summary>
public static partial class MenuCity
{
    /// The tiers, in plan units: where each starts and ends, how wide (x) and deep (z)
    static readonly float[] HouseY = { 0f, 114f, 142f, 162f, 178f, 190f };
    static readonly float[] HouseW = { 44f, 38f, 32f, 25f, 18f }, HouseD = { 34f, 29.5f, 25f, 19.5f, 14f };
    /// The piers' pitch and width, the chamfer the corner piers take off the first tier's faces
    const float PierPitch = 4.4f, PierW = 1.2f, Chamfer = 3.6f;
    /// Where the mark sits: the third tier's south face
    public const float MarkY = 153f, MarkZ = -13.3f, MarkSize = 7f;

    /// The House's stone, into `b`, its foot at `at`, every plan unit `scale` metres
    public static void HouseMass(MeshBaker b, Vector3 at, float scale)
    {
        float s = scale, h = CityKit.TowerH;
        Vector3 P(float x, float y, float z) => at + new Vector3(x, y, z) * s;
        Vector3 S(float x, float y, float z) => new Vector3(x, y, z) * s;
        for (int t = 0; t < 5; t++)
        {
            float y0 = HouseY[t], y1 = HouseY[t + 1], w = HouseW[t], d = HouseD[t];
            b.Add(stoneChar, P(0f, (y0 + y1) * 0.5f, 0f), S(w, y1 - y0, d));
            // The piers: limestone, a rib of cream up the middle of each, the full height of the tier, on every face
            for (int f = 0; f < 4; f++)
            {
                bool alongX = f < 2;
                Vector3 n = f == 0 ? Vector3.back : f == 1 ? Vector3.forward : f == 2 ? Vector3.left : Vector3.right;
                Vector3 along = alongX ? Vector3.right : Vector3.forward;
                float width = alongX ? w : d, half = (alongX ? d : w) * 0.5f;
                float usable = width - (t == 0 ? 2f * Chamfer : 1.6f);
                int count = Mathf.Max(2, Mathf.FloorToInt(usable / PierPitch));
                for (int i = 0; i < count; i++)
                {
                    float x = (i - (count - 1) * 0.5f) * PierPitch;
                    if (t == 2 && f == 0 && Mathf.Abs(x) < 5f) continue;   // the cartouche's bay on the third tier's south face
                    Vector3 c = at + (n * (half + 0.22f) + along * x) * s + Vector3.up * ((y0 + y1) * 0.5f * s);
                    b.Add(stoneLime, c, S(alongX ? PierW : 0.45f, y1 - y0, alongX ? 0.45f : PierW));
                    b.Add(stoneCream, c + n * (0.26f * s), S(alongX ? 0.4f : 0.14f, y1 - y0, alongX ? 0.14f : 0.4f));
                }
            }
            // A stepped cornice at every setback, the top one lipped in gilt
            if (t < 4)
            {
                b.Add(stoneLime, P(0f, y1 + 0.35f, 0f), S(w + 1.2f, 0.7f, d + 1.2f));
                b.Add(stoneLime, P(0f, y1 + 0.95f, 0f), S(w + 0.5f, 0.5f, d + 0.5f));
                if (t == 3) b.Add(glowGold, P(0f, y1 + 1.25f, 0f), S(w + 0.6f, 0.12f, d + 0.6f));
            }
        }
        // The corner piers of the first tier: black marble, turned through forty-five degrees
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                b.Add(marble, P(sx * 22f, HouseY[1] * 0.5f, sz * 17f), S(5f, HouseY[1], 5f), Quaternion.Euler(0f, 45f, 0f));
        // The foot of the south face: a colonnade of ten columns two storeys high under an entablature, the great
        // arch in the middle of it, the doors lit under the arch
        for (int i = 0; i < 10; i++)
        {
            float x = (i - 4.5f) * 3.5f;
            b.Add(stoneLime, P(x, 5.4f, -19.2f), S(1.4f, 9.6f, 1.4f), null, PrimitiveType.Cylinder);
            b.Add(stoneLime, P(x, 10.5f, -19.2f), S(2f, 0.6f, 2f));
        }
        b.Add(stoneLime, P(0f, 11.6f, -19.2f), S(35f, 1.6f, 3.6f));                                           // the entablature
        b.Add(stoneLime, P(0f, 0.3f, -19.6f), S(36f, 0.6f, 5f));                                              // the stylobate
        b.Add(glowWhite, P(0f, 5.5f, -17.15f), S(7f, 0.3f, 7f), Quaternion.Euler(90f, 0f, 0f), PrimitiveType.Cylinder);   // the great arch, lit
        b.Add(stoneChar, P(0f, 7.6f, -17.3f), S(7.6f, 0.4f, 0.5f));                                           // the arch's keystone course
        b.Add(glowWhite, P(0f, 2.2f, -17.25f), S(4f, 4.4f, 0.3f));                                            // the doors
        // THE CROWN: four steps of brass with a gilt lip along the top edge of each, gilt ribs round the first
        float[] cw = { 17f, 13f, 9.5f, 6.5f }, cd = { 13f, 10f, 7.5f, 5f }, cy0 = { 0f, 4.5f, 8f, 11f }, cy1 = { 4.5f, 8f, 11f, 13.5f };
        for (int i = 0; i < 4; i++)
        {
            b.Add(brass, P(0f, h + (cy0[i] + cy1[i]) * 0.5f, 0f), S(cw[i], cy1[i] - cy0[i], cd[i]));
            b.Add(glowGold, P(0f, h + cy1[i] - 0.15f, 0f), S(cw[i] + 0.5f, 0.3f, cd[i] + 0.5f));
        }
        for (int i = 0; i < 9; i++)
        {
            b.Add(glowGold, P(-8f + i * 2f, h + 2.2f, -6.65f), S(0.35f, 4f, 0.35f));
            b.Add(glowGold, P(-8f + i * 2f, h + 2.2f, 6.65f), S(0.35f, 4f, 0.35f));
        }
        for (int i = 0; i < 6; i++)
        {
            b.Add(glowGold, P(-8.65f, h + 2.2f, -5f + i * 2f), S(0.35f, 4f, 0.35f));
            b.Add(glowGold, P(8.65f, h + 2.2f, -5f + i * 2f), S(0.35f, 4f, 0.35f));
        }
        // THE LANTERN on the crown: cream glass between eight brass columns under a brass cap
        float lanternY = h + 13.5f;
        b.Add(glowWhite, P(0f, lanternY + 1.6f, 0f), S(4.2f, 3.2f, 4.2f), null, PrimitiveType.Cylinder);
        for (int k = 0; k < 8; k++)
        {
            float a = k * 45f * Mathf.Deg2Rad;
            b.Add(brass, P(Mathf.Sin(a) * 2.3f, lanternY + 1.6f, Mathf.Cos(a) * 2.3f), S(0.4f, 3.2f, 0.4f));
        }
        b.Add(brass, P(0f, lanternY + 3.5f, 0f), S(5f, 0.6f, 5f), null, PrimitiveType.Cylinder);
        b.Add(brass, P(0f, lanternY + 4.1f, 0f), S(3.2f, 0.6f, 3.2f), null, PrimitiveType.Cylinder);
        // THE MAST: slim iron, a collar a third of the way up, the aircraft lamp on top (world metres: a mast is a mast)
        float mastFoot = (lanternY + 4.4f) * s, mastH = 110f;
        b.Add(iron, at + new Vector3(0f, mastFoot + mastH * 0.5f, 0f), new Vector3(1.6f, mastH, 1.6f));
        b.Add(iron, at + new Vector3(0f, mastFoot + mastH * 0.35f, 0f), new Vector3(6f, 1f, 6f));
        Beacons.Add(at + new Vector3(0f, mastFoot + mastH + 1.5f, 0f));
        Beacons.Add(at + new Vector3(3f, mastFoot + mastH * 0.35f + 1.4f, 0f));
        // THE CARTOUCHE the mark sits in: a brass plate in a stepped brass frame on the third tier's south face
        float cz = -HouseD[2] * 0.5f;
        b.Add(brass, P(0f, MarkY, cz - 0.55f), S(10f, 11f, 0.5f));
        b.Add(brass, P(0f, MarkY + 5.9f, cz - 0.7f), S(11.4f, 0.6f, 0.6f));
        b.Add(brass, P(0f, MarkY - 5.9f, cz - 0.7f), S(11.4f, 0.6f, 0.6f));
        b.Add(glowGold, P(0f, MarkY + 6.5f, cz - 0.6f), S(12f, 0.25f, 0.3f));
        b.Add(glowGold, P(0f, MarkY - 6.5f, cz - 0.6f), S(12f, 0.25f, 0.3f));
    }

    /// The House's LIGHT: the stone of the bays between the piers (the House kind of the wall atlas, punched) and the
    /// few windows lit in it, on every face of every tier; the top tier's floors always lit, and gold; the floods'
    /// wash up the south face in amber and the others in cream; amber up the crown from its own setbacks
    static void HouseLight(SheetBaker sheets, float s)
    {
        float k = 1f / CityTextures.AtlasCells;
        for (int t = 0; t < 5; t++)
        {
            float y0 = HouseY[t] * s, y1 = HouseY[t + 1] * s, w = HouseW[t] * s, d = HouseD[t] * s;
            for (int f = 0; f < 4; f++)
            {
                bool alongX = f < 2;
                Vector3 n = f == 0 ? Vector3.back : f == 1 ? Vector3.forward : f == 2 ? Vector3.left : Vector3.right;
                float width = (alongX ? w : d) - (t == 0 ? 2f * Chamfer * s : 1.6f * s), half = (alongX ? d : w) * 0.5f;
                int cells = Mathf.Max(1, Mathf.RoundToInt(width / Bay)), rows = Mathf.Max(1, Mathf.RoundToInt((y1 - y0) / Floor));
                int ou = Random.Range(0, CityTextures.AtlasCells), ov = Random.Range(0, CityTextures.AtlasCells);
                Vector2 uv0 = new Vector2(ou * k, ov * k), uv1 = new Vector2((ou + cells) * k, (ov + rows) * k);
                Vector3 c = Origin + Vector3.up * ((y0 + y1) * 0.5f) + n * half;
                sheets.Wall(house.wall, c + n * 0.3f, n, width, y1 - y0, uv0, uv1);
                sheets.Wall(t == 4 ? lightGold : house.light, c + n * 0.6f, n, width, y1 - y0, uv0, uv1);
            }
        }
        // The floods' wash, ON the surfaces it lights (a sheet hung in front of a wall slides about against it as the
        // camera moves): amber up the south face, cream round the flanks and the back
        void Flood(Vector3 n, float faceHalf, float width, float top, Material m)
            => sheets.Wall(m, Origin + new Vector3(0f, top * 0.5f, 0f) + n * (faceHalf + 0.75f), n, width, top, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f));
        float w0 = HouseW[0] * s, d0 = HouseD[0] * s;
        Flood(Vector3.back, d0 * 0.5f, w0 * 0.8f, 230f, washAmber);
        Flood(Vector3.forward, d0 * 0.5f, w0 * 0.8f, 170f, washCream);
        Flood(Vector3.left, w0 * 0.5f, d0 * 0.75f, 170f, washCream);
        Flood(Vector3.right, w0 * 0.5f, d0 * 0.75f, 170f, washCream);
        // The crown is floodlit from the top tier's setback: amber up the top tier and the first brass step
        float h = CityKit.TowerH * s;
        void CrownWash(Vector3 n, float faceHalf, float width, float ya, float yb)
            => sheets.Wall(washAmber, Origin + new Vector3(0f, (ya + yb) * 0.5f, 0f) + n * (faceHalf + 0.8f), n, width, yb - ya, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f));
        foreach (var n in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
        {
            bool z = n.z != 0f;
            CrownWash(n, (z ? HouseD[4] : HouseW[4]) * 0.5f * s, (z ? HouseW[4] : HouseD[4]) * 0.9f * s, HouseY[4] * s, h);
            CrownWash(n, (z ? 6.5f : 8.5f) * s, (z ? 16f : 12f) * s, h, h + 4.5f * s);
        }
    }
}
