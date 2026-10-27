using UnityEngine;

/// <summary>
/// THE STREETS AT NIGHT. The plan keeps its compass (the avenues, the rings,
/// the Court, the House at the centre) and this half builds what the rings are
/// made of at street level and in the middle distance: SETBACK MASSES stepping
/// back three to five times under stepped cornices with gargoyles at the
/// corners and gilt fluting round the crown; RADIO TOWERS with red lamps on
/// their arms; rows of brick and brownstone TENEMENTS shoulder to shoulder
/// between the rings, three to thirteen storeys, party walls blind, a fire
/// escape zigzagging down the odd street face, a water tank or a chimney on
/// every roof, a lit shop fascia at the foot and now and then one red neon
/// word; and the ELEVATED on stone VIADUCTS, arched bay after bay on stone
/// piers with a gas lamp every few bays, the trains streaming over them. The
/// light is amber and cream and gilt; the one red thing is a lamp on a mast,
/// or that word.
/// </summary>
public static partial class MenuCity
{
    static Material signBack, viaductStone;

    static void NightInit()
    {
        signBack = Flat(new Color(0.08f, 0.08f, 0.09f), 0.3f);
        viaductStone = Flat(new Color(0.36f, 0.34f, 0.31f), 0.3f);   // the viaducts' stone, sooted
    }

    // ------------------------------------------------------------ the small pieces

    /// A bar from `a` to `b`: a box turned along the line. Never for a vertical segment (its up-hint is world up)
    static void Bar(MeshBaker boxes, Material m, Vector3 a, Vector3 b, float thick)
        => Box(boxes, m, (a + b) * 0.5f, new Vector3(thick, thick, Vector3.Distance(a, b)), Quaternion.LookRotation(b - a, Vector3.up));

    /// The odd RED NEON WORD over a shop: a dark backing and a row of lit letters, the city's one red that is not a lamp
    static void NeonWord(MeshBaker boxes, Vector3 at, Vector3 n, float width, int seed)
    {
        if (Cut(at, width)) return;
        if (neonWords++ > 0) return;   // the one red word in the city (the sign budget, MenuCityGotham)
        Quaternion turn = Quaternion.LookRotation(n, Vector3.up);
        Box(boxes, signBack, at, new Vector3(width + 0.4f, 1.6f, 0.2f), turn);
        int letters = 3 + ((seed % 3) + 3) % 3;
        for (int k = 0; k < letters; k++)
        {
            float x = -width * 0.5f + width * (k + 0.5f) / letters;
            Box(boxes, glowRed, at + n * 0.15f + turn * new Vector3(x, 0f, 0f), new Vector3(width / letters * 0.6f, 1f, 0.12f), turn);
        }
    }

    // ------------------------------------------------------------ the masses
    /// A SETBACK MASS, the drawn shaft seen from below: three to five storey-stacks stepping in, every step a fifth
    /// to a quarter narrower than the one under it and the last TALL AND THIN (tapered in its top storeys on the
    /// tall ones), a stepped cornice at every setback (the top ones lit), gargoyles on the first two, FLUTED PIERS
    /// the height of the first two masses, a water tank and a chimney on the first ledge, finned BUTTRESSES at the
    /// street corners of the giants, and a CROWN from the vocabulary by seed: a finial spire, a crown of stacked
    /// slabs under a mast, a sunburst of brass fins, a ziggurat cap, a gothic crown of winged gargoyles, or a flat
    /// top with a water tower on stilts and a smokestack pair. A clock on some, a searchlight drum on some
    static void Setback(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, float az, int seed)
    {
        if (Cut(foot, Mathf.Max(w, d) * 0.5f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        int s0 = ((seed % 7) + 7) % 7, s1 = ((seed % 6) + 6) % 6;
        Podium(boxes, sheets, foot, w, d, az);
        int steps = 3 + s0 % 3;
        float[] part = steps == 3 ? new[] { 0.44f, 0.24f, 0.32f } : steps == 4 ? new[] { 0.36f, 0.2f, 0.16f, 0.28f } : new[] { 0.32f, 0.18f, 0.14f, 0.12f, 0.24f };
        Skin skin = s0 % 4 == 0 ? pierDark : s0 % 4 == 1 ? pier : s0 % 4 == 2 ? limestone : granite;
        Material stone = skin == pierDark || skin == granite ? stoneChar : stoneLime;
        Material rib = stone == stoneChar ? stoneGranite : stoneCream;
        int blind = TowerBlind(w, d, seed);
        bool tall = h >= 150f;
        float y = 0f, cw = w, cd = d, tw = w, td = d;
        for (int i = 0; i < steps; i++)
        {
            float th = h * part[i];
            bool last = i == steps - 1;
            if (last) TopMass(sheets, foot + Vector3.up * y, cw, cd, th, skin, az, blind, tall);
            else Tier(sheets, foot + Vector3.up * y, cw, cd, th, skin, true, az, blind, false, false);
            if (i < 2) FlutedPiers(boxes, foot + Vector3.up * y, cw, cd, th, az, rib);
            y += th;
            float k2 = last && tall ? 0.9f : 1f;
            Cornice(boxes, foot + Vector3.up * y, cw * k2, cd * k2, az, stone, i >= steps - 2 ? glowWarm : null);
            if (i < 2) Gargoyles(boxes, foot + Vector3.up * y, cw, cd, az, stone);
            tw = cw * k2; td = cd * k2;
            float k = i == 0 ? 0.78f : 0.75f;
            if (i == 0 && steps > 3)
            {
                // The first ledge: a water tank on one side, a chimney on the other
                float lx = cw * (1f + k) * 0.25f;
                WaterTower(boxes, foot + Vector3.up * (y + 1.3f) + turn * new Vector3(lx, 0f, cd * 0.2f), 3f, 4.5f, iron);
                Chimney(boxes, foot + Vector3.up * (y + 1.3f) + turn * new Vector3(-lx, 0f, -cd * 0.2f), 4f);
            }
            cw *= k; cd *= k;
        }
        // The buttresses the giants stand behind: a finned wedge at each street corner
        if (h >= 170f)
        {
            float bw = w * 0.24f, bd = d * 0.5f, bh = Mathf.Min(h * 0.3f, 58f);
            for (int s = -1; s <= 1; s += 2)
                Buttress(boxes, foot + turn * new Vector3(s * (w * 0.5f - bw * 0.5f), 0f, d * 0.5f + bd * 0.5f - 0.3f), bw, bd, bh, az, stone, 3);
        }
        RoofFurniture(boxes, foot + Vector3.up * y, tw, td, az, seed, 1);
        Vector3 roof = foot + Vector3.up * (y + 1.3f);
        float top;
        switch (s1)
        {
            case 0:
                CrownSpire(boxes, roof, Mathf.Min(tw, td) * 0.3f, h * 0.1f, stone, glowGold);
                top = y + 1.3f + h * 0.1f + 2f;
                break;
            case 1:
                SlabCrown(boxes, roof, tw * 0.9f, td * 0.9f, az, h * 0.06f, brass, glowGold);
                RadioMast(boxes, roof + Vector3.up * (h * 0.06f + 3.5f), Mathf.Min(h * 0.12f, 24f), true);
                top = y + 1.3f + h * 0.06f + 3.5f + Mathf.Min(h * 0.12f, 24f);
                break;
            case 2:
                SunburstCrown(boxes, roof, tw * 0.9f, td * 0.9f, az, h * 0.08f, brass);
                top = y + 1.3f + h * 0.08f + 3.5f;
                break;
            case 3:
                ZigguratCap(boxes, roof, tw, td, az, h * 0.07f, stone);
                Flagpole(boxes, roof + Vector3.up * (h * 0.07f) + turn * new Vector3(tw * 0.15f, 0f, 0f), 9f);
                top = y + 1.3f + h * 0.07f + 9f;
                break;
            case 4:
                GothicCrown(boxes, foot + Vector3.up * y, tw, td, az, h * 0.09f, stone);
                top = y + 1.3f + h * 0.09f + 3f;
                break;
            default:
                WaterTower(boxes, roof + turn * new Vector3(-tw * 0.22f, 0f, td * 0.15f), 4f, 9f, iron);
                StackPair(boxes, roof + turn * new Vector3(tw * 0.18f, 0f, -td * 0.2f), az, Mathf.Clamp(h * 0.16f, 10f, 24f), tw * 0.3f);
                top = y + 1.3f + Mathf.Clamp(h * 0.16f, 10f, 24f);
                break;
        }
        if (s0 % 5 == 0) ClockFaces(boxes, foot + Vector3.up * (y - h * part[steps - 1] * 0.5f), tw, td, az, Mathf.Min(Mathf.Min(tw, td), h * part[steps - 1]) * 0.5f);
        if (s0 % 4 == 3) SearchlightDrum(boxes, roof + turn * new Vector3(-tw * 0.3f, 0f, td * 0.3f), az + 200f + 17f * s0, 48f + 4f * s0);
        if (top > 230f && s0 % 2 == 0) Beacons.Add(foot + Vector3.up * top);
        WashUp(sheets, foot, turn * Vector3.back, d * 0.5f, w * 0.8f, h * 0.3f, washAmber);
    }
    /// A RADIO TOWER: a brick transmitter house with a lit door, and over it a lattice mast in four tapering stages
    /// (two crossed plates a stage, which from any distance is a lattice), cross arms with a red lamp on each,
    /// guy wires to the ground, a whip with the aircraft lamp on top
    static void RadioTower(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float h, int seed)
    {
        if (Cut(foot, h * 0.3f)) return;
        Tier(sheets, foot, 14f, 10f, 7f, brick, true, 0f);
        Box(boxes, stoneLime, foot + Vector3.up * 7.25f, new Vector3(14.5f, 0.5f, 10.5f));
        Box(boxes, glowWhite, foot + new Vector3(0f, 1.6f, -5.15f), new Vector3(2f, 3.2f, 0.2f));   // the door
        float y = 7.5f;
        const int stages = 4;
        for (int i = 0; i < stages; i++)
        {
            float sh = (h - 7.5f) / stages, wdt = 5f * (1f - 0.2f * i);
            Box(boxes, iron, foot + Vector3.up * (y + sh * 0.5f), new Vector3(wdt, sh, 0.35f));
            Box(boxes, iron, foot + Vector3.up * (y + sh * 0.5f), new Vector3(0.35f, sh, wdt));
            y += sh;
            Box(boxes, iron, foot + Vector3.up * y, new Vector3(wdt * 2.4f, 0.4f, 0.4f));   // the arms
            Box(boxes, iron, foot + Vector3.up * y, new Vector3(0.4f, 0.4f, wdt * 2.4f));
            if (i < stages - 1)
            {
                Box(boxes, glowRed, foot + Vector3.up * (y + 0.6f) + new Vector3(wdt * 1.2f, 0f, 0f), Vector3.one * 0.8f);
                Box(boxes, glowRed, foot + Vector3.up * (y + 0.6f) - new Vector3(wdt * 1.2f, 0f, 0f), Vector3.one * 0.8f);
            }
        }
        Box(boxes, iron, foot + Vector3.up * (y + 6f), new Vector3(0.5f, 12f, 0.5f));   // the whip
        Beacons.Add(foot + Vector3.up * (y + 13f));
        for (int k = 0; k < 4; k++)   // the guys: from the third stage down to an anchor block (never into the rows next door)
        {
            Vector3 anchor = foot + Radial(Mathf.Min(h * 0.28f, 22f), k * 90f + 45f + seed * 7f);
            Bar(boxes, iron, foot + Vector3.up * (7.5f + (h - 7.5f) * 0.75f), anchor + Vector3.up * 0.8f, 0.14f);
            Box(boxes, stoneLime, anchor + Vector3.up * 0.5f, new Vector3(1.6f, 1f, 1.6f));
        }
    }

    // ------------------------------------------------------------ the tenements between the rings

    /// A ROW of tenements round a ring: brick and brownstone shoulder to shoulder, three to thirteen storeys, every
    /// one its own height, a third of the windows lit, party walls blind, a stone coping, a tank or a chimney on the
    /// roof, a fire escape down the odd street face, a lit shop fascia along every third, the odd red word, an
    /// alley through now and then
    static void Tenements(MeshBaker boxes, SheetBaker sheets, float r, float h, float depth, int ring)
    {
        float sector = 360f / Avenues, gap = Mathf.Clamp(Mathf.Atan2(18f, r) * Mathf.Rad2Deg, 1f, 8f);
        float chordDeg = Mathf.Atan2(22f, r) * Mathf.Rad2Deg;   // fewer, bigger blocks since the silhouette pass
        for (int a = 0; a < Avenues; a++)
        {
            float from = a * sector + gap, to = (a + 1) * sector - gap;
            int n = Mathf.Max(1, Mathf.FloorToInt((to - from) / chordDeg));
            float step = (to - from) / n;
            for (int i = 0; i < n; i++)
            {
                if (n > 6 && i % 7 == 5) continue;
                float az = from + (i + 0.5f) * step;
                float width = 2f * r * Mathf.Sin(step * 0.5f * Mathf.Deg2Rad) * 0.98f;
                float hh = h * (0.6f + 0.8f * CityTextures.Hash01(a, i, ring));
                Vector3 foot = Polar(r, az);
                if (Cut(foot, width * 0.5f)) continue;
                int seed = a * 131 + i * 17 + ring;
                float pick = CityTextures.Hash01(a, i, ring + 1);
                Skin skin = pick < 0.45f ? brick : pick < 0.75f ? brownstone : pick < 0.87f ? charcoal : cream;
                Tier(sheets, foot, width, depth, hh, skin, true, az, BlindSides);
                Quaternion turn = Quaternion.Euler(0f, az, 0f);
                Box(boxes, skin == brick ? brickFlat : skin == brownstone ? brownFlat : stoneLime, foot + Vector3.up * (hh + 0.25f), new Vector3(width + 0.4f, 0.5f, depth + 0.4f), turn);   // the coping
                if (i % 2 == 0) RoofFurniture(boxes, foot + Vector3.up * hh, width, depth, az, seed, 0);
                else Chimney(boxes, foot + Vector3.up * (hh + 0.5f) + turn * new Vector3(width * 0.3f, 0f, -depth * 0.3f), 2.4f + 1.2f * CityTextures.Hash01(seed, 2, 9));
                if (i % 6 == 1) FireEscape(boxes, foot, width, depth, hh, az);
                Vector3 nrm = Radial(1f, az);
                if (i % 9 == 4 && width > 10f) NeonWord(boxes, foot + nrm * (depth * 0.5f + 0.3f) + Vector3.up * 4.6f, nrm, Mathf.Min(width * 0.7f, 8f), seed);
                else if (i % 3 == 0) Box(boxes, glowWarm, foot + nrm * (depth * 0.5f + 0.25f) + Vector3.up * 3.6f, new Vector3(width * 0.8f, 0.3f, 0.2f), turn);   // the shop fascia, lit
            }
        }
    }

    /// THE ELEVATED ON A STONE VIADUCT round a ring (the painted bridge of ARCHES far down a street): a deck between
    /// stone parapets on fluted stone piers every bay, a tall ARCH under the deck in every bay (a keystone and two
    /// pairs of voussoirs leaning down to the piers), a gas lamp on the parapet every sixth bay, the trains streaming
    /// over it (a texture scroll). It flies over the avenues
    static void Viaduct(MeshBaker boxes, SheetBaker ground, float r, float y, float width)
    {
        int segs = Mathf.RoundToInt(2f * Mathf.PI * r / 24f);
        for (int i = 0; i < segs; i++)
        {
            float az0 = i * 360f / segs, az1 = (i + 1) * 360f / segs;
            Vector3 a = Polar(r, az0, y), b = Polar(r, az1, y);
            Vector3 c = (a + b) * 0.5f;
            float len = Vector3.Distance(a, b) * 1.02f;
            if (Cut(c, width)) continue;
            Quaternion turn = Quaternion.LookRotation(b - a, Vector3.up);
            Vector3 along = (b - a).normalized;
            Box(boxes, viaductStone, c, new Vector3(width, 1.6f, len), turn);                                                   // the deck
            Box(boxes, viaductStone, c + turn * new Vector3(width * 0.5f - 0.25f, 1.3f, 0f), new Vector3(0.5f, 1f, len), turn);     // the parapets
            Box(boxes, viaductStone, c + turn * new Vector3(-width * 0.5f + 0.25f, 1.3f, 0f), new Vector3(0.5f, 1f, len), turn);
            // The ARCH under the bay: a ring of seven voussoirs on a half-ellipse from pier to pier, its crown under
            // the deck's middle and its springing halfway down the piers, so the bay is open under it
            float ring = Mathf.Clamp(y * 0.1f, 1.4f, 3.2f), rise = y * 0.5f - ring, hs = len * 0.5f - 1.6f;
            const int Vouss = 7;
            Vector3 deckUnder = c - Vector3.up * (0.8f + ring * 0.5f);
            Vector3 prev = deckUnder + along * (-hs) - Vector3.up * rise;
            for (int v = 1; v <= Vouss; v++)
            {
                float t = v * Mathf.PI / Vouss;
                Vector3 p = deckUnder + along * (-hs * Mathf.Cos(t)) - Vector3.up * (rise * (1f - Mathf.Sin(t)));
                Vector3 seg = p - prev;
                Box(boxes, viaductStone, (p + prev) * 0.5f, new Vector3(width - 0.8f, ring, seg.magnitude + ring * 0.6f), Quaternion.LookRotation(seg.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, 0f));
                prev = p;
            }
            if (OffAvenue(az0) > 2.2f)   // the piers: never in an avenue, which the viaduct flies over on a longer span
            {
                Box(boxes, viaductStone, Polar(r, az0, (y - 0.8f) * 0.5f), new Vector3(width - 0.6f, y - 0.8f, 3.2f), Quaternion.Euler(0f, az0 + 90f, 0f));   // the pier
                Box(boxes, stoneLime, Polar(r, az0, (y - 0.8f) * 0.5f), new Vector3(width - 0.2f, y - 2.4f, 0.6f), Quaternion.Euler(0f, az0 + 90f, 0f));      // its fluted face, a shade lighter
                Box(boxes, stoneLime, Polar(r, az0, 0.5f), new Vector3(width + 0.6f, 1f, 4.2f), Quaternion.Euler(0f, az0 + 90f, 0f));          // its footing
            }
            if (i % 6 == 0)
            {
                Vector3 post = c + turn * new Vector3(width * 0.5f - 0.25f, 1.8f, 0f);
                Box(boxes, bronze, post + Vector3.up * 1.8f, new Vector3(0.25f, 3.6f, 0.25f), turn);
                Box(boxes, glowWhite, post + Vector3.up * 3.9f, Vector3.one * 0.8f, null, PrimitiveType.Sphere);
            }
        }
        RingLine(ground, carriages, r, y + 1.9f, 3.2f, 512f, 0.37f, true);
    }
}
