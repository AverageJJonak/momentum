using UnityEngine;

/// <summary>
/// THE ELEVATED, the ways on and off it and the station (see LevelKitStreetElevated.cs for the frame): a STAIR on
/// the pavement band at every street junction — 2.3 m wide, fourteen metres long, brass-nosed, rising north on the
/// east side and south on the west (both north at the south edge), its LANDING from the deck's edge to the block
/// face; the SLIDE at the north end, 30° from the deck down to the lane out; the STATION at the plaza junction: two
/// platforms flush with the deck, a canopy on iron posts (its roof a line from the carriage's roof), a lit fascia
/// with the name, benches, and the CARRIAGE parked on the track, a 2.5 m lip. Every piece records the parapet
/// opening it needs so the deck's lips stop at it.
/// </summary>
public static partial class LevelKit
{
    /// A stair at every street junction, the slide at the north end
    static void ElevatedStairs(Transform root, ElevatedFrame e)
    {
        var plan = e.plan;
        for (int gz = 0; gz <= plan.blocksZ; gz++)
        {
            if (plan.IsAlley(false, gz)) continue;
            float jz = plan.GapMid(false, gz);
            if (gz == 0) ElevatedStair(root, e, 1, e.z0 + 2.5f, true);                                                   // east rising north at the south edge (the west band is the lane in)
            else if (gz == plan.blocksZ) ElevatedStair(root, e, 1, Mathf.Min(jz + 7.5f, e.z1 - 2.5f), false);             // east rising south at the north edge: the slide takes the west
            else { ElevatedStair(root, e, 1, jz - 7.5f, true); ElevatedStair(root, e, -1, jz + 7.5f, false); }              // east rising north, west rising south
        }
        ElevatedSlide(root, e);
        ElevatedOpenings(root, e);
    }

    /// The deck opens opposite every WINDOW at its height on the avenue's faces (a run jump off the deck lands it:
    /// the Rialto's porthole) and opposite every ROOF beside the avenue no higher than two metres over it (a hop
    /// onto its parapet: the Carom Rooms); a hairline on the deck points at each
    static void ElevatedOpenings(Transform root, ElevatedFrame e)
    {
        var plan = e.plan;
        foreach (var en in plan.entrances)
        {
            bool roof = en.kind == CityDistrict.EntranceKind.RoofHatch || en.kind == CityDistrict.EntranceKind.Skylight;
            float y = plan.floorY + en.height;
            int side;
            float z;
            if (roof)
            {
                if (y > e.top + 2f || y < e.top - 1f) continue;
                if (en.block.bounds.max.x < e.ax && en.block.bounds.max.x > e.ax - e.half - 1f) side = 0;
                else if (en.block.bounds.min.x > e.ax && en.block.bounds.min.x < e.ax + e.half + 1f) side = 1;
                else continue;
                z = en.block.bounds.center.z;
            }
            else
            {
                if (en.kind != CityDistrict.EntranceKind.Window && en.kind != CityDistrict.EntranceKind.Marquee && en.kind != CityDistrict.EntranceKind.Balcony) continue;
                if (y > e.top + 1.2f || y < e.top - 2.5f) continue;
                if (Mathf.Abs(en.at.x - (e.ax - e.half)) < 1f) side = 0;
                else if (Mathf.Abs(en.at.x - (e.ax + e.half)) < 1f) side = 1;
                else continue;
                z = en.at.z;
            }
            if (z < e.zs + 4f || z > e.ze - 4f) continue;
            float w = roof ? 3f : 2.2f;
            int s = side == 0 ? -1 : 1;
            e.cut[side].Add(new Vector2(z - w, z + w));
            LineRun(root, new Vector3(e.ax - s * 0.6f, e.top, z - 6f), new Vector3(e.ax + s * (ElevDeckHW - 0.9f), e.top, z), plan.zone);
            Bake(Brass, new Vector3(e.ax + s * (ElevDeckHW - 0.2f), e.top + 0.02f, z), new Vector3(0.4f, 0.03f, w * 2f - 0.4f));   // the brass threshold where the lip stops
        }
    }

    /// The stair's width on the pavement band and its run; the landing is three metres
    const float ElevStairW = 2.3f, ElevStairRun = 14f;

    /// The landing's inner edge: the deck's edge, or the platform's at the station
    static float ElevatedInner(ElevatedFrame e, float z) => Mathf.Abs(z - e.stationZ) < 12.5f ? ElevDeckHW + ElevPlatW : ElevDeckHW;

    /// One stair on side `s` from `zFoot` on the street up to the deck, rising north or south, with its landing
    static void ElevatedStair(Transform root, ElevatedFrame e, int s, float zFoot, bool north)
    {
        float x = e.ax + s * (e.half - ElevStairW * 0.5f), y0 = e.y + 0.05f;
        float zTop = zFoot + (north ? ElevStairRun : -ElevStairRun);
        // Ramp and Nosings take their ends in ascending z (the slab hangs under the line it is given that way)
        var flight = north ? Ramp(root, x, ElevStairW, zFoot, y0, zTop, e.top, RivetIron) : Ramp(root, x, ElevStairW, zTop, e.top, zFoot, y0, RivetIron);
        flight.name = "ElevatedStair";
        if (north) Nosings(root, x, ElevStairW, zFoot, y0, zTop, e.top, 1.2f);
        else Nosings(root, x, ElevStairW, zTop, e.top, zFoot, y0, 1.2f);
        // The handrail along the kerb-side edge (a look), a brass newel at the foot
        Vector3 a = new Vector3(x - s * (ElevStairW * 0.5f - 0.1f), y0 + 1.1f, zFoot), b = new Vector3(a.x, e.top + 1.1f, zTop);
        Bake(Brass, (a + b) * 0.5f, new Vector3(0.06f, 0.06f, (b - a).magnitude), Quaternion.LookRotation((b - a).normalized, Vector3.up));
        Bake(Brass, new Vector3(a.x, y0 + 0.6f, zFoot), new Vector3(0.16f, 1.2f, 0.16f));
        float inner = ElevatedInner(e, zTop);
        float la = north ? zTop : zTop - 3f, lb = la + 3f;
        Slab(root, new Vector3(e.ax + s * (inner + e.half) * 0.5f, e.top - ElevThick * 0.5f, (la + lb) * 0.5f), new Vector3(e.half - inner, ElevThick, 3f), RivetIron, "ElevatedLanding");
        Bake(Brass, new Vector3(e.ax + s * (e.half - 0.15f), e.top + 0.5f, (la + lb) * 0.5f), new Vector3(0.06f, 1.0f, 3f));   // the rail along the block face
        e.cut[s < 0 ? 0 : 1].Add(new Vector2(la, lb));
    }

    /// The SLIDE at the north end: 30° from the deck's WEST edge (the side nearer x = 0, where the lane out is) down
    /// to the street four metres short of the lane out, beside the tram's rails (never over them), low brass rails
    /// along its edges
    static void ElevatedSlide(Transform root, ElevatedFrame e)
    {
        const float W = 3.2f;
        float x = e.ax - (ElevDeckHW + 1.5f), zTop = e.z1 - 20f, zFoot = e.z1 - 4.4f;
        var go = Ramp(root, x, W, zTop, e.top, zFoot, e.y + 0.05f, Slope);
        go.name = "ElevatedSlide";
        for (int q = -1; q <= 1; q += 2)
        {
            Vector3 a = new Vector3(x + q * (W * 0.5f - 0.08f), e.top + 0.5f, zTop), b = new Vector3(a.x, e.y + 0.55f, zFoot);
            Bake(Brass, (a + b) * 0.5f, new Vector3(0.06f, 0.06f, (b - a).magnitude), Quaternion.LookRotation((b - a).normalized, Vector3.up));
        }
        e.cut[0].Add(new Vector2(zTop - 1.5f, zTop + 2.2f));
        LineRun(root, new Vector3(e.ax - 1.2f, e.top, zTop - 7f), new Vector3(x + 0.6f, e.top, zTop + 0.4f), e.plan.zone);
    }

    // ------------------------------------------------------------ the station

    /// THEATRE SQUARE: two platforms flush with the deck, 24 m long, a canopy on four posts each side (its roof a
    /// solid: hop onto it from the carriage), a lit fascia with the name, a brass edge line, brass rails along the
    /// outer edges (open at the landings), a bench each; the carriage at the platform
    static void ElevatedStation(Transform root, MovementConfig config, ElevatedFrame e)
    {
        float jz = e.stationZ;
        const float Len = 24f;
        for (int s = -1; s <= 1; s += 2)
        {
            int side = s < 0 ? 0 : 1;
            float px = e.ax + s * (ElevDeckHW + ElevPlatW * 0.5f), outer = ElevDeckHW + ElevPlatW;
            Slab(root, new Vector3(px, e.top - ElevThick * 0.5f, jz), new Vector3(ElevPlatW, ElevThick, Len), Limestone, "Platform");
            e.cut[side].Add(new Vector2(jz - Len * 0.5f, jz + Len * 0.5f));
            BrassStrip(root, new Vector3(e.ax + s * (ElevDeckHW + 0.12f), e.top, jz - Len * 0.5f + 0.3f), new Vector3(e.ax + s * (ElevDeckHW + 0.12f), e.top, jz + Len * 0.5f - 0.3f), 0.14f);
            foreach (float k in new[] { -11f, -5f, 1f, 7f })
                Slab(root, new Vector3(e.ax + s * (outer - 0.3f), e.top + 1.7f, jz + k), new Vector3(0.3f, 3.4f, 0.3f), RivetIron, "CanopyPost");
            Slab(root, new Vector3(e.ax + s * (ElevDeckHW + 1.2f), e.top + 3.55f, jz), new Vector3(2.6f, 0.3f, Len), Charcoal, "CanopyRoof");
            Bake(LampCream, new Vector3(e.ax + s * (ElevDeckHW - 0.06f), e.top + 3.2f, jz), new Vector3(0.06f, 0.45f, Len - 0.4f));
            Text(root, new Vector3(e.ax + s * (ElevDeckHW - 0.1f), e.top + 3.2f, jz), Quaternion.LookRotation(Vector3.right * s), "THEATRE SQUARE", 0.4f, new Color(0.1f, 0.08f, 0.05f), "StationName", 10f, true);
            var l = Light(root, new Vector3(e.ax + s * (ElevDeckHW + 1f), e.top + 2.8f, jz), 16f, 1.4f);
            l.color = LampCreamLight;
            // The outer rails (looks), open where the landing meets the platform
            float gapA = s > 0 ? jz + 6.3f : jz - 9.7f, gapB = s > 0 ? jz + 9.7f : jz - 6.3f;
            ElevatedRail(e, s, outer - 0.1f, jz - Len * 0.5f, gapA);
            ElevatedRail(e, s, outer - 0.1f, gapB, jz + Len * 0.5f);
            Bench(root, new Vector3(e.ax + s * (outer - 0.9f), e.top, jz + (s > 0 ? -3f : 3f)), 90f);
        }
        ElevatedCarriage(root, e, jz);
    }

    static void ElevatedRail(ElevatedFrame e, int s, float x, float za, float zb)
    {
        if (zb - za < 0.5f) return;
        Bake(Brass, new Vector3(e.ax + s * x, e.top + 1.0f, (za + zb) * 0.5f), new Vector3(0.06f, 0.06f, zb - za));
        Bake(Brass, new Vector3(e.ax + s * x, e.top + 0.5f, (za + zb) * 0.5f), new Vector3(0.06f, 0.06f, zb - za));
    }

    /// THE CARRIAGE parked at the platform: an oxblood body over a cream band on riveted bogies, its roof the lip
    /// 2.5 m over the deck, amber windows, a cream lamp at each end
    static void ElevatedCarriage(Transform root, ElevatedFrame e, float z)
    {
        float y = e.top;
        Lip(root, new Vector3(e.ax, y + 1.3f, z), new Vector3(2.5f, 2.0f, 13f), Oxblood, "ElevatedCarriage");
        Lip(root, new Vector3(e.ax, y + 2.4f, z), new Vector3(2.3f, 0.2f, 12.6f), Charcoal, "CarriageRoof");
        Bake(Cream, new Vector3(e.ax, y + 0.75f, z), new Vector3(2.54f, 0.6f, 12.8f));
        for (int s = -1; s <= 1; s += 2)
        {
            Bake(WindowAmber, new Vector3(e.ax + s * 1.27f, y + 1.6f, z), new Vector3(0.04f, 0.8f, 11.6f));
            Bake(LampCream, new Vector3(e.ax, y + 1.3f, z + s * 6.52f), new Vector3(0.3f, 0.3f, 0.08f));
            Bake(RivetIron, new Vector3(e.ax, y + 0.16f, z + s * 4f), new Vector3(2f, 0.3f, 2f));
        }
        Text(root, new Vector3(e.ax + 1.29f, y + 2.1f, z), Quaternion.LookRotation(Vector3.left), "HALCYON RAILWAYS", 0.22f, new Color(0.9f, 0.82f, 0.6f), "CarriageName", 8f, true);
        Text(root, new Vector3(e.ax - 1.29f, y + 2.1f, z), Quaternion.LookRotation(Vector3.right), "HALCYON RAILWAYS", 0.22f, new Color(0.9f, 0.82f, 0.6f), "CarriageName", 8f, true);
    }
}
