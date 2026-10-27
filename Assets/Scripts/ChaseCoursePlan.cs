using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE PLAN of a chunk (ChaseCourse's other half): a list of FEATURES decided in one go from the chunk's own
/// System.Random (the level's seed, the chunk's index, its phase: the same chunk every time), then built one feature
/// per Step inside the budget. The vocabulary is the course grammar's: floors with GAPS (every gap has a CATCH LANE
/// four metres down with a pad back up), LIPS (the corner-boost ladder, two blocks either side of the channel), KICK
/// WALLS (fins on the platforms; pairs in phase 2, a zig-zag chain in phase 3), FLUTED PILLARS (edge-boost corners),
/// DECKS (a raised split deck: a 22° ramp on the inner half, a lip on the outer), GLASS to hop through (a pane each
/// side of the channel: the channel is the slow way round), ORBS for the whip (the House's chandeliers, the terminus's
/// drones), FOOTMEN on the gap edges as springboards, PADS up to the galleries, and the terminus's signal gantries;
/// in phase 3 the terminus's third rail goes LIVE (the Underground's own hazard: it hurts and throws, never kills).
/// The centre channel (±Chase.ChanHalf) is the vehicle's: nothing stands in it but the rail or the track.
/// </summary>
public partial class ChaseCourse
{
    enum FK : byte { Floor, Catch, Lip, Kick, Pillar, Deck, Glass, Orb, Footman, Pad, Gantry, LiveRail, Rail, Shell, Gallery, Lamps, Mark }
    struct Feature { public FK k; public float a, b, x, h; public int n; }

    readonly List<Feature> features = new List<Feature>(48);
    int featureIndex;

    static float R(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);
    void Add(FK k, float a = 0f, float b = 0f, float x = 0f, float h = 0f, int n = 0) => features.Add(new Feature { k = k, a = a, b = b, x = x, h = h, n = n });

    void Plan(Chunk c)
    {
        features.Clear();
        var rng = new System.Random(unchecked(seed * 7919 + c.index * 104729 + c.phase * 31));
        int p = c.phase - 1;
        float gapCap = Chase.RunJump(config) * Chase.GapCapMult[p];
        float cz0 = c.z0, cz1 = c.z1;

        // The gaps: inside [10, 40] of the chunk so every seam is solid, spaced by slot so a floor of nine metres or
        // more lies between any two; sized by the phase and capped by the motor's own jump
        int gaps = c.index == 0 ? 0 : rng.Next(Chase.GapsMin[p], Chase.GapsMax[p] + 1);
        float lo = 10f, hi = Chase.ChunkLen - 8f;
        float slot = (hi - lo) / Mathf.Max(1, gaps);
        for (int i = 0; i < gaps; i++)
        {
            float len = Mathf.Min(gapCap, R(rng, Chase.GapMin[p], Chase.GapMax[p]));
            float a = lo + slot * i + R(rng, 1.5f, Mathf.Max(1.5f, slot - len - 1.5f));
            c.gapZ0.Add(cz0 + a);
            c.gapZ1.Add(cz0 + a + len);
        }
        float from = cz0;
        for (int g = 0; g < gaps; g++)
        {
            Add(FK.Floor, from, c.gapZ0[g]);
            Add(FK.Catch, c.gapZ0[g], c.gapZ1[g]);
            from = c.gapZ1[g];
        }
        Add(FK.Floor, from, cz1);
        Add(FK.Rail, cz0, cz1);
        Add(FK.Shell, cz0, cz1);
        Add(FK.Gallery, cz0, cz1, 0f, 0f, c.index);
        Add(FK.Lamps, cz0, cz1, 0f, 0f, c.index);
        Add(FK.Mark, cz0, cz1, 0f, 0f, c.index);

        // The lines on the floors: one feature per floor stretch long enough to hold it, chosen by the phase
        int footmen = 0, side = (c.index & 1) == 0 ? 1 : -1;
        from = cz0;
        for (int g = 0; g <= gaps; g++)
        {
            float to = g < gaps ? c.gapZ0[g] : cz1;
            float len = to - from;
            if (len >= 9f && c.index > 0)
            {
                int pick = rng.Next(0, 100);
                float z = from + R(rng, 3f, Mathf.Max(3.5f, len - 6f));
                if (p == 0)
                {
                    if (pick < 45) Add(FK.Lip, z, 1.6f, 0f, R(rng, Chase.LipMin[0], Chase.LipMax[0]));
                    else if (pick < 75) Add(FK.Pillar, z, 0f, side * 4.2f, 6f);
                    else if (len >= 17f) Add(FK.Deck, from + 2f, 1.6f);
                    else Add(FK.Lip, z, 1.6f, 0f, R(rng, Chase.LipMin[0], Chase.LipMax[0]));
                }
                else if (p == 1)
                {
                    if (pick < 25) Add(FK.Lip, z, 1.6f, 0f, R(rng, Chase.LipMin[1], Chase.LipMax[1]));
                    else if (pick < 50) { Add(FK.Kick, z, 0f, -5f, 4f); Add(FK.Kick, z, 0f, 5f, 4f); }
                    else if (pick < 70) Add(FK.Glass, z);
                    else if (pick < 85 && len >= 17f) Add(FK.Deck, from + 2f, 1.6f);
                    else Add(FK.Pillar, z, 0f, side * 4.2f, 6f);
                    if (rng.Next(0, 100) < 50 && len >= 12f) Add(FK.Pad, from + len - 3f, 0f, -side * 6.5f, 18f);
                }
                else
                {
                    if (pick < 40 && len >= 17f) { float k0 = from + 3f; Add(FK.Kick, k0, 0f, -5.2f, 4.5f); Add(FK.Kick, k0 + 6f, 0f, 5.2f, 4.5f); Add(FK.Kick, k0 + 12f, 0f, -5.2f, 4.5f); }
                    else if (pick < 60) Add(FK.Lip, z, 1.6f, 0f, R(rng, Chase.LipMin[2], Chase.LipMax[2]));
                    else if (pick < 80) Add(FK.Glass, z);
                    else if (len >= 17f) Add(FK.Deck, from + 2f, 1.6f);
                    else { Add(FK.Kick, z, 0f, -5f, 4f); Add(FK.Kick, z, 0f, 5f, 4f); }
                    if (rng.Next(0, 100) < 60 && len >= 12f) Add(FK.Pad, from + len - 3f, 0f, -side * 6.5f, 18f);
                }
                side = -side;
            }
            // A footman on the edge before the gap: a springboard across
            if (g < gaps && footmen < Chase.Footmen[p] && len >= 6f)
            {
                Add(FK.Footman, to - 2.6f, 0f, (footmen == 0 ? 1f : -1f) * 3.2f);
                footmen++;
            }
            from = g < gaps ? c.gapZ1[g] : cz1;
        }
        // The whip's anchor over the longest gap; the terminus's gantry; the live rail in phase 3
        if (gaps > 0)
        {
            int big = 0;
            for (int g = 1; g < gaps; g++) if (c.gapZ1[g] - c.gapZ0[g] > c.gapZ1[big] - c.gapZ0[big]) big = g;
            Add(FK.Orb, (c.gapZ0[big] + c.gapZ1[big]) * 0.5f);
        }
        if (kind == ChaseKind.Stationmaster)
        {
            if (c.index > 0) Add(FK.Gantry, cz0 + 5f);
            if (p == 2) Add(FK.LiveRail, cz0, cz1);
        }
    }

    /// One feature into the world, under the chunk's root (its markers, its units, its pads with it)
    void BuildFeature(Chunk c, Feature f)
    {
        Transform t = c.root.transform;
        float y = floorY, drop = Chase.ChanDrop(kind);
        switch (f.k)
        {
            case FK.Floor: LevelKit.ChaseFloor(t, kind, y, drop, f.a, f.b); break;
            case FK.Catch: LevelKit.ChaseCatch(t, kind, zone, y, f.a, f.b); break;
            case FK.Lip: LevelKit.ChaseLip(t, kind, y, f.a, f.h, f.b); break;
            case FK.Kick: LevelKit.ChaseKick(t, kind, f.x, y, f.a, f.h); break;
            case FK.Pillar: LevelKit.ChasePillar(t, kind, f.x, y, f.a, f.h); break;
            case FK.Deck: LevelKit.ChaseDeck(t, kind, y, f.a, f.b); break;
            case FK.Glass: LevelKit.ChaseGlass(t, config, y, f.a); break;
            case FK.Orb:
                if (kind == ChaseKind.Concierge) LevelKit.Chandelier(t, new Vector3(0f, y + height - 7f, f.a), y + height);
                else LevelKit.Orb(t, new Vector3(0f, y + 8.5f, f.a));
                break;
            case FK.Footman: Footman(c, new Vector3(f.x, y, f.a)); break;
            case FK.Pad: LevelKit.JumpPad(t, new Vector3(f.x, y, f.a), 0f, f.h, 5f); break;
            case FK.Gantry: LevelKit.ChaseGantry(t, y, f.a); break;
            case FK.LiveRail: LevelKit.ThirdRail(t, new Vector3(1.25f, y - drop, f.a + 0.5f), new Vector3(1.25f, y - drop, f.b - 0.5f)); break;
            case FK.Rail: LevelKit.ChaseRail(t, kind, y - drop, f.a, f.b); break;
            case FK.Shell: LevelKit.ChaseShell(t, kind, y, height, f.a, f.b); break;
            case FK.Gallery: LevelKit.ChaseGallery(t, kind, y, f.a, f.b, f.n); break;
            case FK.Lamps: LevelKit.ChaseLamps(t, kind, y, height, f.a, f.b, f.n); break;
            case FK.Mark: LevelKit.ChaseMark(t, kind, y, f.a, f.n); break;
        }
    }

    /// A footman on a line: the project's own factory (what Course.Fuel calls), noted as fuel, parented to the chunk
    void Footman(Chunk c, Vector3 at)
    {
        var e = Enemy.CreateDummy(at, config);
        if (e == null) return;
        e.transform.SetParent(c.root.transform, true);
        LevelSections.Last?.fuel.Add(new LevelSections.FuelMark { unit = e, line = "the chase" });
    }
}
