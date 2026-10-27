using System.Collections.Generic;
using UnityEngine;
using Plan = CityDistrict.DistrictPlan;

/// <summary>
/// THE ELEVATED (district-grammar, Level 2): an iron railway down an avenue of a district, the south edge to the
/// north, over the tram. TWO ROWS of riveted iron PIERS (1.2 m boost pillars, every corner authored, a brass bracket
/// lip on the avenue face at 4.6 and one on the north face at 6.9: the corner-boost ladder) stand on the pavement
/// lines, three to a block, probed clear of the lamp standards, awnings and marquees (a blocked one is nudged, then
/// skipped: the girder spans further); CROSS GIRDERS between the rows whose ends, outside the deck, are lips; plate
/// girders under the deck's edges; the DECK, 7 m wide, with riveted parapet lips both sides and the track inlaid; a
/// STAIR up from the pavement band at every street junction (the lazy way) and a SLIDE down to the lane out at the
/// north end; one STATION with platforms, a canopy and a parked carriage (a 2.5 m lip) at the plaza junction; brass
/// lamps, cream globes. The deck is a second route the whole length; every drop off it lands on a street.
/// Solids are slabs (the weld takes them); every rivet, rail and sleeper is baked, no collider. This file: the frame,
/// the piers and girders, the deck and its furniture. Level2DistrictElevated.cs: the stairs, the slide, the station.
/// </summary>
public static partial class LevelKit
{
    const float ElevDeckHW = 3.5f, ElevThick = 0.5f, ElevGirder = 0.9f, ElevPierW = 1.2f, ElevPlatW = 2f;

    /// One elevated's numbers, shared by its pieces
    class ElevatedFrame
    {
        public Plan plan;
        /// The avenue's centre x and half width, the street floor, the deck's top, the district's planes, the deck's
        /// ends, the station's junction z
        public float ax, half, y, top, z0, z1, zs, ze, stationZ;
        /// The parapets' openings per side (0 west, 1 east): (z from, z to)
        public readonly List<Vector2>[] cut = { new List<Vector2>(), new List<Vector2>() };
        public float PierX => half - 1.25f;
        /// The piers' top: the cross girders sit on them, 5 cm into the deck's slab
        public float PierTop => top - ElevThick - ElevGirder + 0.05f;
    }

    /// THE ELEVATED down avenue `gx` of a built plan, its deck `deckY` over the street. Call after CityDistrict.Build
    public static void Elevated(Transform root, MovementConfig config, Plan plan, int gx, float deckY = 9f)
    {
        if (plan == null || !plan.built || gx < 0 || gx > plan.blocksX || plan.IsAlley(true, gx)) { Debug.LogWarning("LevelKit.Elevated: no avenue " + gx + " to build over"); return; }
        var e = new ElevatedFrame
        {
            plan = plan, ax = plan.GapMid(true, gx), half = plan.GapW(true, gx) * 0.5f, y = plan.floorY,
            top = plan.floorY + deckY, z0 = plan.z0, z1 = plan.Z1,
        };
        e.zs = e.z0 + 0.8f; e.ze = e.z1 - 0.8f;
        // The station: the inner street junction nearest the district's middle (the later one on a tie)
        float mid = (e.z0 + e.z1) * 0.5f, best = float.MaxValue;
        e.stationZ = mid;
        for (int gz = 1; gz < plan.blocksZ; gz++)
        {
            if (plan.IsAlley(false, gz)) continue;
            float d = Mathf.Abs(plan.GapMid(false, gz) - mid);
            if (d <= best) { best = d; e.stationZ = plan.GapMid(false, gz); }
        }
        SetZone(plan.zone);
        Physics.SyncTransforms();
        BeginFacades();                            // the rivets, sleepers, lamps and rails bake into one mesh per material
        ElevatedStairs(root, e);                   // every junction's stair and landing, the slide at the north end
        ElevatedStation(root, config, e);          // the platforms, the canopy, the parked carriage
        Physics.SyncTransforms();
        ElevatedPiers(root, e);                    // two rows, probed clear; the cross girders and the plate girders
        ElevatedDeck(root, config, e);             // the deck, the parapets round the openings, the track, the lamps, the fuel
        FlushFacades(root);
        Physics.SyncTransforms();
    }

    // ------------------------------------------------------------ piers and girders

    /// Three pier pairs a block row at 2, half and depth − 2 along it, each pair nudged (±2, ±4, ±6) until both piers
    /// stand clear of what the street already holds, else left out; a cross girder on every pair; the plate girders
    static void ElevatedPiers(Transform root, ElevatedFrame e)
    {
        var plan = e.plan;
        float h = e.PierTop - e.y;
        float[] nudge = { 0f, 2f, -2f, 4f, -4f, 6f, -6f };
        for (int bz = 0; bz < plan.blocksZ; bz++)
        {
            float b0 = plan.Block0(false, bz), bd = plan.blockD;
            foreach (float k in new[] { 2f, bd * 0.5f, bd - 2f })
            {
                bool found = false; float pz = 0f;
                foreach (float d in nudge)
                {
                    float z = b0 + k + d;
                    if (z < b0 + 1.2f || z > b0 + bd - 1.2f) continue;
                    if (ElevatedPierClear(e, -1, z) && ElevatedPierClear(e, 1, z)) { found = true; pz = z; break; }
                }
                if (!found) continue;
                for (int s = -1; s <= 1; s += 2) ElevatedPier(root, e, s, pz, h);
                ElevatedGirder(root, e, pz);
            }
        }
        // The plate girders under the deck's edges, their outer faces 5 cm inside the deck's, riveted
        float len = e.ze - e.zs - 1f, gy = e.top - ElevThick + 0.05f - 0.4f, zm = (e.zs + e.ze) * 0.5f;
        for (int s = -1; s <= 1; s += 2)
        {
            Slab(root, new Vector3(e.ax + s * (ElevDeckHW - 0.3f), gy, zm), new Vector3(0.5f, 0.8f, len), RivetIron, "PlateGirder");
            for (float z = e.zs + 1.5f; z < e.ze - 1f; z += 1.6f)
            {
                Bake(Bronze, new Vector3(e.ax + s * (ElevDeckHW - 0.05f + 0.035f), gy + 0.22f, z), new Vector3(0.07f, 0.07f, 0.07f));
                Bake(Bronze, new Vector3(e.ax + s * (ElevDeckHW - 0.05f + 0.035f), gy - 0.22f, z), new Vector3(0.07f, 0.07f, 0.07f));
            }
        }
        // The deck bears on the threshold arches' lintels at both ends (looks)
        foreach (float z in new[] { e.z0 + 1.3f, e.z1 - 1.3f })
            for (int s = -1; s <= 1; s += 2) Bake(RivetIron, new Vector3(e.ax + s * 2.4f, e.top - ElevThick + 0.05f - 0.425f, z), new Vector3(0.6f, 0.85f, 0.6f));
    }

    /// Nothing solid in the pier's column (from 0.4 over the street, so the pavement does not count)
    static bool ElevatedPierClear(ElevatedFrame e, int side, float z)
    {
        float h = e.PierTop - e.y - 0.4f;
        Vector3 c = new Vector3(e.ax + side * e.PierX, e.y + 0.4f + h * 0.5f, z);
        return !Physics.CheckBox(c, new Vector3(0.65f, h * 0.5f, 0.65f), Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }

    /// One pier: a riveted boost pillar on a base plate, a bearing under the girder, the two brass bracket lips
    static void ElevatedPier(Transform root, ElevatedFrame e, int side, float z, float h)
    {
        float px = e.ax + side * e.PierX;
        BoostCorner(root, new Vector3(px, e.y + h * 0.5f, z), new Vector3(ElevPierW, h, ElevPierW), RivetIron);
        Bake(RivetIron, new Vector3(px, e.y + 0.2f, z), new Vector3(1.5f, 0.4f, 1.5f));                 // the base plate
        Bake(RivetIron, new Vector3(px, e.y + h - 0.2f, z), new Vector3(1.6f, 0.4f, 1.6f));             // the bearing
        for (int r = 0; r < 3; r++)
            for (int q = -1; q <= 1; q += 2)
                Bake(Bronze, new Vector3(px - side * (ElevPierW * 0.5f + 0.035f), e.y + 1.6f + r * 2f, z + q * 0.4f), new Vector3(0.07f, 0.07f, 0.07f));
        // The brackets: the first on the avenue face (its top at 4.6: a wall combo off the facade lands it), the
        // second on the north face (6.9: a corner boost up from the first), the girder's end lip over them (8.55)
        Lip(root, new Vector3(px - side * (ElevPierW * 0.5f + 0.5f), e.y + 4.45f, z), new Vector3(1.0f, 0.3f, 1.2f), Brass, "PierBracket");
        Bake(Brass, new Vector3(px - side * (ElevPierW * 0.5f + 0.3f), e.y + 4.05f, z), new Vector3(0.5f, 0.5f, 0.5f));
        Lip(root, new Vector3(px, e.y + 6.75f, z + ElevPierW * 0.5f + 0.5f), new Vector3(1.2f, 0.3f, 1.0f), Brass, "PierBracket");
        Bake(Brass, new Vector3(px, e.y + 6.35f, z + ElevPierW * 0.5f + 0.3f), new Vector3(0.5f, 0.5f, 0.5f));
    }

    /// A cross girder from pier to pier under the deck; its ends outside the deck are lips (brass along their edges)
    static void ElevatedGirder(Transform root, ElevatedFrame e, float z)
    {
        float gy = e.PierTop + ElevGirder * 0.5f, reach = e.PierX + ElevPierW * 0.5f;
        Slab(root, new Vector3(e.ax, gy, z), new Vector3(reach * 2f, ElevGirder, 1.0f), RivetIron, "CrossGirder");
        float top = gy + ElevGirder * 0.5f;
        for (int s = -1; s <= 1; s += 2)
        {
            float x0 = ElevDeckHW + 0.1f, x1 = reach - 0.05f, xm = (x0 + x1) * 0.5f;
            for (int q = -1; q <= 1; q += 2) Prim(root, new Vector3(e.ax + s * xm, top + MarkProud + 0.012f, z + q * 0.41f), new Vector3(x1 - x0, 0.024f, 0.14f), Brass, "LipBrass");
            TechMarkers.MarkLip(new Vector3(e.ax + s * xm, top, z), new Vector3(x1 - x0 + 0.6f, 0.9f, 1.6f));
        }
        for (float x = -reach + 0.8f; x <= reach - 0.8f; x += 1.5f) Bake(Bronze, new Vector3(e.ax + x, gy, z - 0.535f), new Vector3(0.07f, 0.07f, 0.07f));
    }

    // ------------------------------------------------------------ the deck

    /// The deck slab, the parapet lips in runs between the openings (a brass post at every opening's end), the
    /// buffer stops, the track (brass rails on baked sleepers, the lit hairline between them), the lamps, the fuel
    static void ElevatedDeck(Transform root, MovementConfig config, ElevatedFrame e)
    {
        float len = e.ze - e.zs, zm = (e.zs + e.ze) * 0.5f;
        Slab(root, new Vector3(e.ax, e.top - ElevThick * 0.5f, zm), new Vector3(ElevDeckHW * 2f, ElevThick, len), Charcoal, "ElevatedDeck");
        for (int side = 0; side < 2; side++)
        {
            int s = side == 0 ? -1 : 1;
            float x = e.ax + s * (ElevDeckHW - 0.35f);
            var cuts = e.cut[side];
            cuts.Sort((a, b) => a.x.CompareTo(b.x));
            float z = e.zs + 0.3f;
            foreach (var c in cuts)
            {
                if (c.x > z + 0.6f) ElevatedParapet(root, e, x, z, c.x);
                z = Mathf.Max(z, c.y);
            }
            if (e.ze - 0.3f > z + 0.6f) ElevatedParapet(root, e, x, z, e.ze - 0.3f);
        }
        foreach (float z in new[] { e.zs + 1.4f, e.ze - 1.4f })
            Lip(root, new Vector3(e.ax, e.top + 0.6f, z), new Vector3(5.4f, 1.2f, 0.8f), RivetIron, "BufferStop");
        // The track: two brass rails, sleepers baked under them, the hairline down the middle (the deck is a fast line)
        for (int s = -1; s <= 1; s += 2)
            BrassStrip(root, new Vector3(e.ax + s * 0.72f, e.top, e.zs + 2.2f), new Vector3(e.ax + s * 0.72f, e.top, e.ze - 2.2f), 0.12f);
        for (float z = e.zs + 2.6f; z < e.ze - 2.4f; z += 1.2f) Bake(WoodPanel, new Vector3(e.ax, e.top + 0.002f, z), new Vector3(2.3f, 0.004f, 0.24f));
        LineRun(root, new Vector3(e.ax, e.top, e.zs + 3f), new Vector3(e.ax, e.top, e.ze - 3f), e.plan.zone);
        // Brass lamps on the parapets' tops every 24 m, alternate sides, a warm light on every other one
        int n = 0;
        for (float z = e.zs + 12f; z < e.ze - 6f; z += 24f, n++)
        {
            int side = n % 2, s = side == 0 ? -1 : 1;
            if (ElevatedCut(e, side, z)) continue;
            Vector3 foot = new Vector3(e.ax + s * (ElevDeckHW - 0.35f), e.top + 0.9f, z);
            Bake(Brass, foot + Vector3.up * 1.3f, new Vector3(0.14f, 2.6f, 0.14f));
            Bake(Brass, foot + Vector3.up * 2.5f - Vector3.right * (s * 0.45f), new Vector3(0.9f, 0.1f, 0.1f));
            Bake(LampCream, foot + Vector3.up * 2.55f - Vector3.right * (s * 0.9f), new Vector3(0.5f, 0.5f, 0.5f), null, PrimitiveType.Sphere);
            if (n % 2 == 0) { var l = Light(root, foot + Vector3.up * 2.3f - Vector3.right * (s * 0.9f), 14f, 1.3f); l.color = LampCreamLight; }
        }
        // FUEL on the deck's line: footmen along it, a bellhop on the carriage's roof, a page over the platforms, a
        // doorman on the first landing
        float jz1 = e.plan.GapMid(false, 1);
        CityDistrict.Fuel(root, config, EnemyKind.Dummy, new Vector3(e.ax + 1.9f, e.top, (jz1 + e.stationZ) * 0.5f), "the deck");
        CityDistrict.Fuel(root, config, EnemyKind.Dummy, new Vector3(e.ax - 1.9f, e.top, (e.stationZ + e.ze) * 0.5f + 6f), "the deck");
        CityDistrict.Fuel(root, config, EnemyKind.Chaser, new Vector3(e.ax, e.top + 2.5f, e.stationZ), "the carriage roof");
        CityDistrict.Fuel(root, config, EnemyKind.Turret, new Vector3(e.ax, e.top + 4.5f, e.stationZ - 15f), "over the platforms");
        CityDistrict.Fuel(root, config, EnemyKind.Sentry, new Vector3(e.ax + 5.8f, e.top, e.z0 + 18f), "the south stair");
    }

    static void ElevatedParapet(Transform root, ElevatedFrame e, float x, float za, float zb)
    {
        Lip(root, new Vector3(x, e.top + 0.45f, (za + zb) * 0.5f), new Vector3(0.7f, 0.9f, zb - za), RivetIron, "ElevatedParapet");
        Bake(Brass, new Vector3(x, e.top + 0.55f, za + 0.11f), new Vector3(0.22f, 1.1f, 0.22f));
        Bake(Brass, new Vector3(x, e.top + 0.55f, zb - 0.11f), new Vector3(0.22f, 1.1f, 0.22f));
    }

    /// True inside one of that side's parapet openings
    static bool ElevatedCut(ElevatedFrame e, int side, float z)
    {
        foreach (var c in e.cut[side]) if (z >= c.x - 0.5f && z <= c.y + 0.5f) return true;
        return false;
    }
}
