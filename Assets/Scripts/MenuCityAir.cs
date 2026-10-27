using UnityEngine;

/// <summary>
/// THE AIR OVER THE CITY, STILL (2026-10-16, after the user's reference
/// paintings): the FOG BANKS lying between the ranks of towers (flat sheets of
/// soft-edged haze over the low fabric between the rings, dark and warm, the
/// way the painted clouds are done), the THREE SEARCHLIGHTS that cross the sky
/// from the ground in the far city (iron drums on the roofs of the outer
/// rings, their beams long and thin; the living half swaps the beams for cones
/// that sweep), and the FANS of thin additive beams the Court's tallest crowns
/// throw. Partial of MenuCity, compiled by the menu harness too: plain
/// UnityEngine, no LINQ, no Mathf.Abs(int).
/// </summary>
public static partial class MenuCity
{
    /// Where the three ground searchlights stand and how they aim (for MenuCityLife's sweeping cones)
    public struct GroundLight { public Vector3 at; public float az, pitch, length; }
    public static System.Collections.Generic.List<GroundLight> GroundLights { get; private set; } = new System.Collections.Generic.List<GroundLight>();

    // ------------------------------------------------------------ the fog between the ranks

    /// THE FOG BANKS: rings of flat haze a little over the low roofs between the rings of towers (and out over the
    /// cut-outs beyond, where they hide the feet of the silhouettes), the tallest shafts rising out of them. Each
    /// is a ring of quads with the bank texture tiled round it, so no bank is a sheet. None where a level stands
    static void HazeBanks(SheetBaker sheets)
    {
        float[] r0 = { 296f, 556f, 650f, 800f, 955f, 1070f, 1176f, 1380f, 1800f, 2300f };
        float[] r1 = { 404f, 650f, 730f, 900f, 1025f, 1150f, 1320f, 1750f, 2300f, 2750f };
        float[] ys = { 17f, 24f, 27f, 31f, 30f, 32f, 34f, 38f, 44f, 52f };
        for (int b = 0; b < r0.Length; b++) HazeRing(sheets, r0[b], r1[b], ys[b], 150f + b * 20f, b * 0.37f);
    }

    /// One ring of fog, `r0` to `r1` at height `y`, in sectors; `tile` metres to a repeat of the bank, `shift` turns it
    static void HazeRing(SheetBaker sheets, float r0, float r1, float y, float tile, float shift)
    {
        const int Seg = 48;
        float per = Mathf.Max(1f, Mathf.Round(2f * Mathf.PI * (r0 + r1) * 0.5f / tile)) / Seg;
        for (int i = 0; i < Seg; i++)
        {
            float a0 = i * 360f / Seg, a1 = (i + 1) * 360f / Seg;
            Vector3 p0 = Polar(r0, a0, y), p1 = Polar(r1, a0, y), p2 = Polar(r1, a1, y), p3 = Polar(r0, a1, y);
            if (CutSeg(p0, p2) || CutSeg(p1, p3)) continue;
            float u0 = i * per + shift, u1 = (i + 1) * per + shift;
            sheets.Quad(hazeBank, p0, p1, p2, p3, new Vector2(u0, 0f), new Vector2(u0, 1f), new Vector2(u1, 1f), new Vector2(u1, 0f));
        }
    }

    // ------------------------------------------------------------ the searchlights

    /// THREE SEARCHLIGHTS FROM THE GROUND: on the roofs of three far blocks on the outer rings, their beams crossing
    /// over the plaza from three sides. Still here (the harness sees them); the living half puts a cone that sweeps
    /// on each drum and hides the still beam
    static void GroundSearchlights(MeshBaker boxes, SheetBaker sheets)
    {
        GroundLights.Clear();
        float[] rs = { 1010f, 1170f, 1290f };
        float[] bearings = { 41f, 163f, 281f };
        float[] pitches = { 64f, 70f, 61f };
        for (int i = 0; i < 3; i++)
        {
            Vector3 at = Polar(rs[i], bearings[i], 24f);
            if (Cut(at, 6f)) continue;
            // The block it stands on: a plain mass of the outer city's stone, a parapet round the drum
            Box(boxes, stoneChar, at - Vector3.up * 12f, new Vector3(18f, 24f, 14f), Quaternion.Euler(0f, bearings[i], 0f));
            float az = bearings[i] + 180f + (i - 1) * 14f;   // over the plaza, a little past it, so the three cross
            Searchlight(boxes, sheets, at, az, pitches[i], 1500f);
            GroundLights.Add(new GroundLight { at = at + Vector3.up * 1.6f, az = az, pitch = pitches[i], length = 1500f });
        }
    }

    /// The crowns' FANS: from the finial of every tall Court shaft (FanSites), five thin beams spread across the
    /// ring's tangent, fifteen to sixty degrees off the vertical each way, additive, long. A drum of lamps at the
    /// hub. Still: a fan is a drawn thing
    static void CrownFans(MeshBaker boxes, SheetBaker sheets)
    {
        foreach (var site in FanSites)
        {
            if (site.height < 200f) continue;   // the two heroes flanking the south approach: a fan is a rare thing
            if (Cut(site.at, 4f)) continue;
            Box(boxes, iron, site.at + Vector3.up * 0.6f, new Vector3(2.2f, 1.2f, 2.2f), null, PrimitiveType.Cylinder);
            Box(boxes, glowSearch, site.at + Vector3.up * 1.4f, Vector3.one * 1.3f, null, PrimitiveType.Sphere);
            const int Beams = 4;
            float length = site.height * 2.2f, wide = 5f;
            for (int k = 0; k < Beams; k++)
            {
                float ang = -42f + 84f * k / (Beams - 1);
                Quaternion tilt = Quaternion.AngleAxis(ang, Vector3.Cross(site.across, Vector3.up));   // leaned across the tangent
                Vector3 dir = tilt * Vector3.up;
                Vector3 lamp = site.at + Vector3.up * 1.4f + dir * 1.2f;
                for (int q = 0; q < 2; q++)
                {
                    Vector3 side = (q == 0 ? site.across : Vector3.Cross(dir, site.across).normalized) * (wide * 0.5f);
                    sheets.Quad(fanBeam, lamp - side * 0.2f, lamp + dir * length - side, lamp + dir * length + side, lamp + side * 0.2f, new Vector2(0f, 0f), new Vector2(1f, 1f));
                }
            }
        }
    }
}
