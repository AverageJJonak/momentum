using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE DECO DRESSING (2026-10-07): the pieces every mass in the city wears and
/// the great buildings of the rings, all of it Gotham 1939. The small pieces:
/// a stepped CONE for spires, a CROWN SPIRE with gilt ribs and a finial,
/// GARGOYLES as stepped abstractions at the setback corners, WATER TOWERS on
/// legs, CHIMNEYS, RADIO MASTS with a red lamp, FLAGPOLES, CLOCK FACES (a lit
/// disc, two hands), bronze STATUES as stepped abstractions on plinths, FIRE
/// ESCAPES zigzagging down a brick face, SEARCHLIGHTS with their beams in the
/// cloud, a pitched slate roof. The great buildings: a BANK with a colonnade
/// and a stepped attic, a STATION under a bronze dome with gilt ribs and four
/// pylons, a CATHEDRAL in stone with an amber rose window, a TENEMENT block
/// round a court with fire escapes, a POWERHOUSE with chimney stacks, a CLOCK
/// TOWER with a lantern. Partial of MenuCity, compiled by the menu harness too:
/// plain UnityEngine, no LINQ, no Mathf.Abs(int). Nothing here is cyan, green,
/// magenta or violet; red is an aircraft lamp.
/// </summary>
public static partial class MenuCity
{
    static Material oxblood, clockGlow;

    static void DecoInit()
    {
        oxblood = Flat(new Color(0.32f, 0.08f, 0.08f), 0.4f);      // flags, awnings
        clockGlow = Glow(G(new Color(1.35f, 1.22f, 0.95f)));      // a clock face, lit from behind
    }

    /// WHERE THE SMOKE COMES FROM: the mouth of every stack and chimney the plan builds (the powerhouses', the
    /// roofs'), and how wide it is, for the plumes (LevelKit.Plumes) a level or the menu puts on the nearest of
    /// them. Kept across Init (a level records its own blocks' chimneys before it raises the plan); cleared by
    /// whoever consumes it, and by CityKit.Init at the start of every build
    public static List<Vector3> Stacks { get; private set; } = new List<Vector3>();
    public static List<float> StackWidths { get; private set; } = new List<float>();

    public static void ClearStacks() { Stacks.Clear(); StackWidths.Clear(); }

    static void Stack(Vector3 mouth, float width) { Stacks.Add(mouth); StackWidths.Add(width); }

    // ------------------------------------------------------------ the small pieces

    /// A STEPPED CONE: cylinders shrinking up to a point. Reads as a spire from any distance the plan is seen at.
    /// `rib` lights it: a ring at the foot and four ribs up to the point, so the spire is a shape at night
    static void Cone(MeshBaker boxes, Material m, Vector3 foot, float r, float h, int steps = 6, Material rib = null)
    {
        float sh = h / steps;
        for (int i = 0; i < steps; i++)
        {
            float rr = r * (1f - (float)i / steps) + 0.25f;
            Box(boxes, m, foot + Vector3.up * (sh * (i + 0.5f)), new Vector3(rr * 2f, sh + 0.02f, rr * 2f), null, PrimitiveType.Cylinder);
        }
        if (rib == null) return;
        Box(boxes, rib, foot + Vector3.up * 0.15f, new Vector3(r * 2f + 0.5f, 0.25f, r * 2f + 0.5f), null, PrimitiveType.Cylinder);
        for (int k = 0; k < 4; k++) Bar(boxes, rib, foot + Radial(r + 0.2f, k * 90f + 45f) + Vector3.up * 0.15f, foot + Vector3.up * (h + 0.3f), 0.25f);
    }

    /// A CROWN SPIRE: a stepped stone cone with gilt ribs, a brass rod and a gold ball on the point
    static void CrownSpire(MeshBaker boxes, Vector3 top, float r, float h, Material stone, Material gilt)
    {
        if (Cut(top, r + 1f)) return;
        Cone(boxes, stone, top, r, h, 6, gilt);
        Box(boxes, brass, top + Vector3.up * (h + 1.2f), new Vector3(0.5f, 2.4f, 0.5f));
        Box(boxes, glowGold, top + Vector3.up * (h + 2.8f), Vector3.one * 1.6f, null, PrimitiveType.Sphere);
    }

    /// GARGOYLES on the four corners of a setback: a HUNCHED block leaning out over the street, a head thrust
    /// forward of it, and (`winged`: the gothic crowns and the Court) two thin slabs of wing swept up and back. A
    /// few primitives each, read as a silhouette
    static void Gargoyles(MeshBaker boxes, Vector3 top, float w, float d, float az, Material stone, bool winged = false)
    {
        if (Cut(top, Mathf.Max(w, d) * 0.5f + 3f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        float s = Mathf.Clamp(Mathf.Min(w, d) * 0.06f, 1.4f, 3f) * (winged ? 1.35f : 1f);
        for (int c = 0; c < 4; c++)
        {
            float sx = c % 2 == 0 ? -1f : 1f, sz = c < 2 ? -1f : 1f;
            Vector3 corner = top + turn * new Vector3(sx * (w * 0.5f + 0.9f), 1.3f, sz * (d * 0.5f + 0.9f));
            // Facing out along the corner's diagonal, hunched forward
            Quaternion face = turn * Quaternion.Euler(0f, sx * sz > 0f ? (sx > 0f ? 45f : 225f) : (sx > 0f ? 135f : 315f), 0f);
            Vector3 outDir = face * Vector3.forward;
            Box(boxes, stone, corner + Vector3.up * (s * 0.6f), new Vector3(s, s * 1.2f, s), turn);                                   // the plinth
            Box(boxes, stone, corner + Vector3.up * (s * 1.5f) + outDir * (s * 0.45f), new Vector3(s * 0.7f, s * 0.7f, s * 1.5f), face * Quaternion.Euler(-30f, 0f, 0f));   // the hunched body
            Box(boxes, stone, corner + Vector3.up * (s * 1.95f) + outDir * (s * 1.2f), new Vector3(s * 0.45f, s * 0.4f, s * 0.55f), face * Quaternion.Euler(-10f, 0f, 0f));   // the head, thrust forward
            if (!winged) continue;
            for (int k = -1; k <= 1; k += 2)   // the wings: thin slabs swept up and back off the shoulders
                Box(boxes, stone, corner + Vector3.up * (s * 2.3f) + face * new Vector3(k * s * 0.75f, 0f, -s * 0.3f), new Vector3(s * 1.2f, s * 0.9f, 0.14f), face * Quaternion.Euler(-20f, k * 35f, k * 38f));
        }
    }

    /// A FINNED BUTTRESS: the wedge-shaped pylon of the painted foregrounds. A mass tapering in four steps to a
    /// ridge, `w` across the street face and `d` deep at the foot, `h` high, with thin FINS standing proud of its
    /// outward face the full height, each stepping in with the mass. Faces outward along `az`
    static void Buttress(MeshBaker boxes, Vector3 foot, float w, float d, float h, float az, Material stone, int fins = 4)
    {
        if (Cut(foot, Mathf.Max(w, d) * 0.5f + 1f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        const int Steps = 4;
        float y = 0f;
        float[] part = { 0.4f, 0.28f, 0.2f, 0.12f };
        for (int i = 0; i < Steps; i++)
        {
            float k = 1f - 0.2f * i, th = h * part[i];
            float cw = w * k, cd = d * (1f - 0.17f * i);
            // The mass leans in on its back: its centre walks forward (outward) as it rises, so the street face stays
            Box(boxes, stone, foot + Vector3.up * (y + th * 0.5f) + turn * new Vector3(0f, 0f, (d - cd) * 0.5f), new Vector3(cw, th, cd), turn);
            for (int f = 0; f < fins; f++)   // the fins on the street face, thin slabs proud of it
            {
                float x = (f - (fins - 1) * 0.5f) * (cw / fins);
                Box(boxes, stone, foot + Vector3.up * (y + th * 0.5f) + turn * new Vector3(x, 0f, d * 0.5f + 0.45f), new Vector3(0.6f, th, 1.1f), turn);
            }
            y += th;
        }
        Box(boxes, stone, foot + Vector3.up * (y + 0.6f) + turn * new Vector3(0f, 0f, d * 0.5f - d * (1f - 0.17f * (Steps - 1)) * 0.5f), new Vector3(w * 0.3f, 1.2f, d * 0.4f), turn);   // the ridge cap
    }

    /// A BRIDGE thrown between two towers at height: a stone deck from `a` to `b` with parapets, a segmental ARCH of
    /// five voussoirs under its middle, a lamp at each end. `width` across. A look: it carries nothing
    static void SkyBridge(MeshBaker boxes, Vector3 a, Vector3 b, float width, Material stone)
    {
        if (CutSeg(a, b)) return;
        Vector3 dv = b - a;
        dv.y = 0f;
        float len = dv.magnitude;
        if (len < 8f) return;
        Vector3 dir = dv / len, mid = (a + b) * 0.5f;
        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
        Vector3 side = Vector3.Cross(dir, Vector3.up);
        Box(boxes, stone, mid, new Vector3(width, 1.4f, len), rot);                                                     // the deck
        Box(boxes, stone, mid + side * (width * 0.5f - 0.25f) + Vector3.up * 1.2f, new Vector3(0.5f, 1.1f, len), rot);   // the parapets
        Box(boxes, stone, mid - side * (width * 0.5f - 0.25f) + Vector3.up * 1.2f, new Vector3(0.5f, 1.1f, len), rot);
        // The arch: a keystone box under the middle, two voussoirs leaning down to each end
        float rise = Mathf.Min(len * 0.22f, 14f), ring = 1.6f;
        Box(boxes, stone, mid - Vector3.up * (ring * 0.5f + 0.7f), new Vector3(width, ring, len * 0.28f), rot);
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 p1 = mid + dir * (s * len * 0.26f) - Vector3.up * (0.7f + ring * 0.5f + rise * 0.33f);
            Box(boxes, stone, p1, new Vector3(width, ring, len * 0.26f), rot * Quaternion.Euler(s * Mathf.Atan2(rise * 0.66f, len * 0.24f) * Mathf.Rad2Deg, 0f, 0f));
            Vector3 p2 = mid + dir * (s * len * 0.44f) - Vector3.up * (0.7f + ring * 0.5f + rise * 0.8f);
            Box(boxes, stone, p2, new Vector3(width, ring, len * 0.2f), rot * Quaternion.Euler(s * Mathf.Atan2(rise * 0.6f, len * 0.14f) * Mathf.Rad2Deg, 0f, 0f));
            Vector3 lamp = mid + dir * (s * (len * 0.5f - 1.5f)) + side * (width * 0.5f - 0.25f) + Vector3.up * 1.8f;
            Box(boxes, bronze, lamp + Vector3.up * 1.2f, new Vector3(0.25f, 2.4f, 0.25f), rot);
            Box(boxes, glowWhite, lamp + Vector3.up * 2.7f, Vector3.one * 0.7f, null, PrimitiveType.Sphere);
        }
    }

    /// A SMOKESTACK PAIR on a roof: two banded brick stacks `h` tall and a PAINTED column of smoke off each, the way
    /// a painted skyline has it: a stack of soft dark lumps rising and drifting downwind, still. Their mouths are
    /// not given to the plumes (LevelKit.Plumes): the painted column is the smoke
    static void StackPair(MeshBaker boxes, Vector3 top, float az, float h, float apart)
    {
        if (Cut(top, apart + 3f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        Vector3 drift = turn * new Vector3(0.55f, 1f, -0.25f);   // the wind: across and a little back
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 p = top + turn * new Vector3(s * apart * 0.5f, 0f, 0f);
            float y = 0f;
            for (int t = 0; t < 3; t++)
            {
                float th = h * (t == 0 ? 0.42f : t == 1 ? 0.33f : 0.25f), dia = 2.6f - 0.45f * t;
                Box(boxes, brickFlat, p + Vector3.up * (y + th * 0.5f), new Vector3(dia, th, dia), null, PrimitiveType.Cylinder);
                y += th;
                Box(boxes, iron, p + Vector3.up * y, new Vector3(dia + 0.4f, 0.4f, dia + 0.4f), null, PrimitiveType.Cylinder);
            }
            Vector3 mouth = p + Vector3.up * (y + 0.3f);
            for (int k = 0; k < 4; k++)   // the painted smoke: lumps growing as they rise and drift
            {
                float r = 1.6f + 1.3f * k;
                Box(boxes, smokePaint, mouth + drift * (2.2f + 3.2f * k) + Vector3.up * (r * 0.4f), new Vector3(r * 1.5f, r * 1.1f, r * 1.3f), turn * Quaternion.Euler(0f, k * 37f, 0f), PrimitiveType.Sphere);
            }
        }
    }

    /// A CROWN OF STACKED SLABS: thin horizontal slabs stepping in as they rise on a slim core, the finial over the
    /// last (the crown of the painted fluted tower). `m` is the slabs' stone or brass
    static void SlabCrown(MeshBaker boxes, Vector3 top, float w, float d, float az, float h, Material m, Material finial)
    {
        if (Cut(top, Mathf.Max(w, d) * 0.5f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        const int Slabs = 6;
        float pitch = h / Slabs;
        Box(boxes, stoneChar, top + Vector3.up * (h * 0.5f), new Vector3(w * 0.3f, h, d * 0.3f), turn);   // the core
        for (int i = 0; i < Slabs; i++)
        {
            float k = 1f - 0.12f * i;
            Box(boxes, m, top + Vector3.up * (pitch * (i + 0.5f)), new Vector3(w * k, pitch * 0.38f, d * k), turn);
        }
        Box(boxes, m, top + Vector3.up * (h + 1.2f), new Vector3(0.6f, 2.4f, 0.6f), turn);
        Box(boxes, finial, top + Vector3.up * (h + 2.8f), Vector3.one * 1.6f, null, PrimitiveType.Sphere);
    }

    /// A SUNBURST CROWN: thin fins radiating from the top of a shaft's two long faces like a rising sun (the gold
    /// tower's crown), a zigzag of them on the short faces, a finial between. Brass, lit at the tips
    static void SunburstCrown(MeshBaker boxes, Vector3 top, float w, float d, float az, float h, Material m)
    {
        if (Cut(top, Mathf.Max(w, d) * 0.5f + 2f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        Box(boxes, stoneChar, top + Vector3.up * (h * 0.3f), new Vector3(w * 0.5f, h * 0.6f, d * 0.5f), turn);   // the drum the fins stand on
        const int Fins = 5;
        for (int f = 0; f < 2; f++)
        {
            Vector3 n = turn * (f == 0 ? Vector3.forward : Vector3.back);
            Vector3 hub = top + n * (d * 0.25f + 0.2f) + Vector3.up * (h * 0.3f);
            for (int i = 0; i < Fins; i++)
            {
                float ang = -50f + 100f * i / (Fins - 1);   // fanned about the hub, in the face's plane
                Quaternion rot = turn * Quaternion.Euler(0f, f == 0 ? 0f : 180f, 0f) * Quaternion.Euler(0f, 0f, ang);
                float len = h * (0.95f - 0.25f * Mathf.Abs(ang) / 50f);
                Box(boxes, m, hub + rot * Vector3.up * (len * 0.5f), new Vector3(1.5f, len, 0.3f), rot);
                Box(boxes, glowGold, hub + rot * Vector3.up * (len + 0.4f), Vector3.one * 0.5f, null, PrimitiveType.Sphere);
            }
        }
        for (int s = -1; s <= 1; s += 2)   // the zigzag on the short faces: three fins each
            for (int i = -1; i <= 1; i++)
                Box(boxes, m, top + turn * new Vector3(s * (w * 0.25f + 0.2f), h * 0.3f + h * 0.3f, i * d * 0.16f), new Vector3(0.25f, h * 0.6f, 0.9f), turn * Quaternion.Euler(i * 22f, 0f, 0f));
        Box(boxes, m, top + Vector3.up * (h + 1.5f), new Vector3(0.6f, 3f, 0.6f), turn);
        Box(boxes, glowGold, top + Vector3.up * (h + 3.3f), Vector3.one * 1.6f, null, PrimitiveType.Sphere);
    }

    /// A ZIGGURAT CAP: three stone steps over a flat roof, a lantern on the last
    static void ZigguratCap(MeshBaker boxes, Vector3 top, float w, float d, float az, float h, Material stone)
    {
        if (Cut(top, Mathf.Max(w, d) * 0.5f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        float y = 0f;
        for (int i = 0; i < 3; i++)
        {
            float k = 0.82f - 0.22f * i, th = h / 3f;
            Box(boxes, stone, top + Vector3.up * (y + th * 0.5f), new Vector3(w * k, th, d * k), turn);
            y += th;
        }
        Box(boxes, glowWhite, top + Vector3.up * (y + 1.2f), new Vector3(2.4f, 2.4f, 2.4f), turn);
        Box(boxes, brass, top + Vector3.up * (y + 2.7f), new Vector3(3f, 0.5f, 3f), turn);
    }

    /// A GOTHIC CROWN: a winged gargoyle on every corner and a pinnacle behind each, a stepped stone finial between
    static void GothicCrown(MeshBaker boxes, Vector3 top, float w, float d, float az, float h, Material stone)
    {
        if (Cut(top, Mathf.Max(w, d) * 0.5f + 3f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        Gargoyles(boxes, top, w, d, az, stone, true);
        for (int c = 0; c < 4; c++)
        {
            Vector3 at = top + Vector3.up * 1.3f + turn * new Vector3((c % 2 == 0 ? -1f : 1f) * (w * 0.5f - 2.2f), 0f, (c < 2 ? -1f : 1f) * (d * 0.5f - 2.2f));
            Box(boxes, stone, at + Vector3.up * (h * 0.25f), new Vector3(2.4f, h * 0.5f, 2.4f), turn);
            Cone(boxes, stone, at + Vector3.up * (h * 0.5f), 1.4f, h * 0.4f, 4);
            Box(boxes, glowGold, at + Vector3.up * (h * 0.9f + 0.6f), Vector3.one * 0.6f, null, PrimitiveType.Sphere);
        }
        CrownSpire(boxes, top + Vector3.up * 1.3f, Mathf.Min(w, d) * 0.28f, h, stone, glowGold);
    }

    /// A TINY CROWD on a terrace: ink figures in a loose group about `at`, `count` of them within `spread`, still.
    /// From the orbit, or from a window a street away, they are people on a lit terrace
    static void Crowd(MeshBaker boxes, Vector3 at, float spread, int count, int seed)
    {
        if (Cut(at, spread)) return;
        for (int i = 0; i < count; i++)
        {
            float a = CityTextures.Hash01(seed, i, 51) * 6.2832f, r = Mathf.Sqrt(CityTextures.Hash01(seed, i, 52)) * spread;
            Vector3 p = at + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            Quaternion yaw = Quaternion.Euler(0f, CityTextures.Hash01(seed, i, 53) * 360f, 0f);
            Box(boxes, marble, p + Vector3.up * 0.85f, new Vector3(0.42f, 1.7f, 0.26f), yaw);
            Box(boxes, marble, p + Vector3.up * 1.82f, new Vector3(0.24f, 0.24f, 0.24f), yaw);
        }
    }

    /// A WATER TOWER: a tank on four legs under a conical lid
    static void WaterTower(MeshBaker boxes, Vector3 at, float dia, float h, Material legs)
    {
        if (Cut(at, dia)) return;
        float legH = h * 0.45f, tankH = h * 0.45f;
        for (int k = 0; k < 4; k++)
            Box(boxes, legs, at + Radial(dia * 0.4f, k * 90f + 45f) + Vector3.up * (legH * 0.5f), new Vector3(0.22f, legH, 0.22f));
        Box(boxes, iron, at + Vector3.up * (legH + tankH * 0.5f), new Vector3(dia, tankH, dia), null, PrimitiveType.Cylinder);
        Box(boxes, iron, at + Vector3.up * (legH + tankH * 0.5f), new Vector3(dia + 0.2f, 0.3f, dia + 0.2f), null, PrimitiveType.Cylinder);   // the hoop
        Box(boxes, roofSlate, at + Vector3.up * (legH + tankH + h * 0.05f), new Vector3(dia * 0.7f, h * 0.1f, dia * 0.7f), null, PrimitiveType.Cylinder);   // the lid
    }

    /// A CHIMNEY: a brick stack with an iron cap
    static void Chimney(MeshBaker boxes, Vector3 at, float h)
    {
        if (Cut(at, 1f)) return;
        Box(boxes, brickFlat, at + Vector3.up * (h * 0.5f), new Vector3(1.1f, h, 1.1f));
        Box(boxes, iron, at + Vector3.up * (h + 0.15f), new Vector3(1.4f, 0.3f, 1.4f));
        Stack(at + Vector3.up * (h + 0.3f), 1.1f);
    }

    /// A RADIO MAST on a roof: an iron pole with two cross arms, and if asked the red aircraft lamp on top
    static void RadioMast(MeshBaker boxes, Vector3 at, float h, bool lamp)
    {
        if (Cut(at, 2f)) return;
        Box(boxes, iron, at + Vector3.up * (h * 0.5f), new Vector3(0.5f, h, 0.5f));
        Box(boxes, iron, at + Vector3.up * (h * 0.7f), new Vector3(3f, 0.25f, 0.25f));
        Box(boxes, iron, at + Vector3.up * (h * 0.88f), new Vector3(2f, 0.25f, 0.25f));
        if (lamp) Beacons.Add(at + Vector3.up * (h + 0.8f));
    }

    /// A FLAGPOLE with the company's flag, oxblood, stiff in the wind
    static void Flagpole(MeshBaker boxes, Vector3 at, float h)
    {
        if (Cut(at, 2f)) return;
        Box(boxes, brass, at + Vector3.up * (h * 0.5f), new Vector3(0.25f, h, 0.25f));
        Box(boxes, glowGold, at + Vector3.up * (h + 0.3f), Vector3.one * 0.6f, null, PrimitiveType.Sphere);
        Box(boxes, oxblood, at + Vector3.up * (h - 1.2f) + new Vector3(1.7f, 0f, 0f), new Vector3(3.2f, 2f, 0.1f));
    }

    /// A CLOCK FACE on a wall whose outward normal is `n`: a DARK face (the painted clock tower's) in a thin lit ring,
    /// two cream hands at ten past ten
    static void ClockFace(MeshBaker boxes, Vector3 centre, Vector3 n, float dia)
    {
        Quaternion lie = Quaternion.LookRotation(n, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);   // a cylinder's axis along the normal
        Quaternion face = Quaternion.LookRotation(n, Vector3.up);
        Box(boxes, clockGlow, centre + n * 0.3f, new Vector3(dia + 0.6f, 0.5f, dia + 0.6f), lie, PrimitiveType.Cylinder);   // the lit ring: what shows of it round the face
        Box(boxes, stoneChar, centre + n * 0.62f, new Vector3(dia, 0.3f, dia), lie, PrimitiveType.Cylinder);                // the face, dark
        Box(boxes, clockGlow, centre + n * 0.85f + face * new Vector3(-dia * 0.12f, dia * 0.18f, 0f), new Vector3(dia * 0.08f, dia * 0.42f, 0.14f), face * Quaternion.Euler(0f, 0f, 30f));   // the hour hand
        Box(boxes, clockGlow, centre + n * 0.85f + face * new Vector3(dia * 0.1f, dia * 0.2f, 0f), new Vector3(dia * 0.06f, dia * 0.56f, 0.14f), face * Quaternion.Euler(0f, 0f, -24f));   // the minute hand
    }

    /// A clock on all four faces of a storey-stack
    static void ClockFaces(MeshBaker boxes, Vector3 centre, float w, float d, float az, float dia)
    {
        if (Cut(centre, Mathf.Max(w, d) * 0.5f + 1f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        for (int f = 0; f < 4; f++)
        {
            Vector3 n = turn * (f == 0 ? Vector3.back : f == 1 ? Vector3.forward : f == 2 ? Vector3.left : Vector3.right);
            float half = (f < 2 ? d : w) * 0.5f;
            ClockFace(boxes, centre + n * (half + 0.5f), n, dia);
        }
    }

    /// A STATUE: a bronze figure as a stepped abstraction (feet, body, shoulders, head) on a stone plinth
    static void Statue(MeshBaker boxes, Vector3 at, float h, float az)
    {
        if (Cut(at, 2f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        float ph = h * 0.3f, bh = h * 0.7f;
        Box(boxes, stoneLime, at + Vector3.up * (ph * 0.5f), new Vector3(h * 0.4f, ph, h * 0.4f), turn);
        Box(boxes, bronze, at + Vector3.up * (ph + bh * 0.3f), new Vector3(h * 0.22f, bh * 0.6f, h * 0.16f), turn);
        Box(boxes, bronze, at + Vector3.up * (ph + bh * 0.72f), new Vector3(h * 0.3f, bh * 0.24f, h * 0.18f), turn);
        Box(boxes, bronze, at + Vector3.up * (ph + bh * 0.92f), new Vector3(h * 0.12f, bh * 0.16f, h * 0.12f), turn);
    }

    /// A FIRE ESCAPE down a street face (+z of a building turned by `az`): iron landings every three floors with a
    /// stair slanting from each to the next, `across` metres off the face's middle
    static void FireEscape(MeshBaker boxes, Vector3 foot, float width, float depth, float hh, float az, float across = 0f)
    {
        if (Cut(foot, Mathf.Max(width, depth) * 0.5f + 2f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        Vector3 n = turn * Vector3.forward, right = turn * Vector3.right;
        Vector3 face = foot + n * (depth * 0.5f + 0.75f) + right * across;
        float span = Mathf.Min(3.6f, width * 0.4f);
        int landings = Mathf.Clamp(Mathf.FloorToInt((hh - 4f) / 11.4f), 1, 6);
        for (int k = 0; k < landings; k++)
        {
            float y = 4f + k * 11.4f;
            Box(boxes, iron, face + Vector3.up * y, new Vector3(span, 0.12f, 1.3f), turn);
            Box(boxes, iron, face + Vector3.up * (y + 0.55f) + n * 0.6f, new Vector3(span, 0.08f, 0.08f), turn);   // the rail
            if (k < landings - 1) Bar(boxes, iron, face + Vector3.up * y + right * (span * 0.4f), face + Vector3.up * (y + 11.4f) - right * (span * 0.4f), 0.3f);
        }
        Bar(boxes, iron, face + Vector3.up * 4f + right * (span * 0.4f), face + Vector3.up * 0.3f + right * (span * 0.4f) + n * 1.6f, 0.25f);   // the ladder down to the street
    }

    /// A PITCHED ROOF over a `w` by `d` top, the ridge along `w`, `rise` high: two slate slabs leaning together
    /// under a brass ridge cap
    static void Pitched(MeshBaker boxes, Vector3 top, float w, float d, float az, float rise)
    {
        if (Cut(top, Mathf.Max(w, d) * 0.5f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        float half = d * 0.5f, slant = Mathf.Sqrt(half * half + rise * rise), pitch = Mathf.Atan2(rise, half) * Mathf.Rad2Deg;
        for (int s = -1; s <= 1; s += 2)
            Box(boxes, roofSlate, top + turn * new Vector3(0f, rise * 0.5f, s * half * 0.5f), new Vector3(w * 1.04f, 0.45f, slant + 0.4f), turn * Quaternion.Euler(-s * pitch, 0f, 0f));
        Box(boxes, brass, top + Vector3.up * (rise + 0.2f), new Vector3(w * 1.04f, 0.3f, 0.5f), turn);
    }

    /// A PITCHED ROOF the old way (a cube turned on its ridge), for the narrow roofs where a steep pitch is wanted
    static void Gable(MeshBaker boxes, Vector3 top, float w, float d, float az, Material ridge)
    {
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        float a = d * 0.98f / 1.41421f;
        Box(boxes, stoneLime, top + Vector3.up * 0.3f, new Vector3(w * 1.06f, 0.35f, d * 1.12f), turn);       // the eaves
        Box(boxes, roofSlate, top, new Vector3(w * 0.995f, a, a), turn * Quaternion.Euler(45f, 0f, 0f));    // the pitch
        if (ridge != null) Box(boxes, ridge, top + Vector3.up * (d * 0.49f + 0.05f), new Vector3(w, 0.28f, 0.28f), turn);   // the ridge cap
    }

    /// A SEARCHLIGHT: an iron drum on a roof, a lamp in it, and its beam into the cloud, `pitch` degrees up on
    /// bearing `az`. The beam is two crossed sheets that fade along their length, so it reads from any side
    static void Searchlight(MeshBaker boxes, SheetBaker sheets, Vector3 at, float az, float pitch, float length)
    {
        if (Cut(at, 3f)) return;
        Quaternion aim = Quaternion.Euler(-pitch, az, 0f);
        Vector3 dir = aim * Vector3.forward;
        Box(boxes, iron, at + Vector3.up * 0.4f, new Vector3(2.6f, 0.8f, 2.6f), null, PrimitiveType.Cylinder);
        Box(boxes, iron, at + Vector3.up * 1.6f, new Vector3(2.4f, 2.2f, 2.4f), aim * Quaternion.Euler(90f, 0f, 0f), PrimitiveType.Cylinder);
        Box(boxes, glowSearch, at + Vector3.up * 1.6f + dir * 1.2f, Vector3.one * 1.6f, null, PrimitiveType.Sphere);
        Vector3 lamp = at + Vector3.up * 1.6f + dir * 1.4f;
        float wide = length * 0.045f;
        for (int q = 0; q < 2; q++)
        {
            Vector3 side = (q == 0 ? aim * Vector3.right : aim * Vector3.up) * (wide * 0.5f);
            sheets.Quad(beam, lamp - side * 0.3f, lamp + dir * length - side, lamp + dir * length + side, lamp + side * 0.3f, new Vector2(0f, 0f), new Vector2(1f, 1f));
        }
    }

    /// A searchlight DRUM on a roof with no beam of its own (the three beams that cross the sky are the ground's,
    /// MenuCityAir): an iron drum and its lamp, turned to `az`
    static void SearchlightDrum(MeshBaker boxes, Vector3 at, float az, float pitch)
    {
        if (Cut(at, 3f)) return;
        Quaternion aim = Quaternion.Euler(-pitch, az, 0f);
        Box(boxes, iron, at + Vector3.up * 0.4f, new Vector3(2.6f, 0.8f, 2.6f), null, PrimitiveType.Cylinder);
        Box(boxes, iron, at + Vector3.up * 1.6f, new Vector3(2.4f, 2.2f, 2.4f), aim * Quaternion.Euler(90f, 0f, 0f), PrimitiveType.Cylinder);
        Box(boxes, glowSearch, at + Vector3.up * 1.6f + aim * Vector3.forward * 1.2f, Vector3.one * 1.6f, null, PrimitiveType.Sphere);
    }

    // ------------------------------------------------------------ the great buildings

    /// A BANK: a limestone block of tall arched windows with a colonnade on the face that looks at the House, its
    /// door lit under a gilt sunburst, a stepped attic in two tiers with cornices, bronze figures on the parapet
    /// corners, a flag on top. `az` faces outward; the House is behind it
    static void Bank(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, float az, int seed)
    {
        if (Cut(foot, Mathf.Max(w, d) * 0.5f + 4f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        Vector3 fwd = turn * Vector3.forward;
        float baseH = h * 0.5f, atticH = h * 0.3f, topH = h * 0.2f;
        Tier(sheets, foot, w, d, baseH, arcade, true, az);
        Cornice(boxes, foot + Vector3.up * baseH, w, d, az, stoneLime, glowWarm);
        // The colonnade: columns the height of the base under an entablature, on a stylobate of steps
        int cols = Mathf.Clamp(Mathf.RoundToInt(w / 6f), 4, 12);
        Vector3 face = foot - fwd * (d * 0.5f + 2.6f);
        float colH = baseH - 3.4f;
        for (int k = 0; k < cols; k++)
        {
            float x = -w * 0.42f + w * 0.84f * k / (cols - 1);
            Box(boxes, stoneLime, face + turn * new Vector3(x, colH * 0.5f + 0.6f, 0f), new Vector3(2.2f, colH, 2.2f), null, PrimitiveType.Cylinder);
            Box(boxes, stoneLime, face + turn * new Vector3(x, colH + 0.9f, 0f), new Vector3(3f, 0.6f, 3f), turn);   // the capital
        }
        Box(boxes, stoneLime, face + Vector3.up * (colH + 2.2f), new Vector3(w * 0.9f, 2f, 4.4f), turn);   // the entablature
        Box(boxes, stoneLime, face + Vector3.up * 0.3f, new Vector3(w * 0.9f, 0.6f, 5f), turn);           // the stylobate
        Vector3 door = foot - fwd * (d * 0.5f + 0.3f);
        Box(boxes, glowWhite, door + Vector3.up * 5f, new Vector3(6f, 10f, 0.3f), turn);                   // the great door, lit
        Box(boxes, glowGold, door + Vector3.up * 15f, new Vector3(9f, 0.5f, 9f), turn * Quaternion.Euler(90f, 0f, 0f), PrimitiveType.Cylinder);   // the sunburst, half sunk in the wall
        // The stepped attic
        float aw = w * 0.72f, ad = d * 0.72f;
        Tier(sheets, foot + Vector3.up * baseH, aw, ad, atticH, limestone, true, az);
        Cornice(boxes, foot + Vector3.up * (baseH + atticH), aw, ad, az, stoneLime, null);
        float tw = w * 0.45f, td = d * 0.45f;
        Tier(sheets, foot + Vector3.up * (baseH + atticH), tw, td, topH, pier, true, az);
        Cornice(boxes, foot + Vector3.up * h, tw, td, az, stoneLime, glowGold);
        for (int c = 0; c < 4; c++)
            Statue(boxes, foot + Vector3.up * (baseH + 1.3f) + turn * new Vector3((c % 2 == 0 ? -1f : 1f) * (w * 0.5f - 3f), 0f, (c < 2 ? -1f : 1f) * (d * 0.5f - 3f)), 7f, az);
        Flagpole(boxes, foot + Vector3.up * (h + 1.3f), 12f);
        RoofFurniture(boxes, foot + Vector3.up * h, tw, td, az, seed, 1);
        if (h + 14f > 230f) Beacons.Add(foot + Vector3.up * (h + 14f));
        WashUp(sheets, foot, -fwd, d * 0.5f, w * 0.9f, baseH * 0.7f, washCream);
    }

    /// A STATION: a drum of arched windows under a bronze dome ribbed in gilt with a lit lantern on top, an arcade
    /// of piers round it under a canopy with a lamp on each, four square pylons at the corners with lamps, a clock
    /// on the face that looks at the House
    static void Station(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float h, float az, int seed)
    {
        if (Cut(foot, w * 0.8f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        float drumH = h * 0.5f, dome = w * 0.92f;
        RoundTier(boxes, sheets, foot, w, drumH, arcade, true);
        Box(boxes, stoneLime, foot + Vector3.up * (drumH + 0.3f), new Vector3(w + 1.6f, 0.6f, w + 1.6f), null, PrimitiveType.Cylinder);
        Box(boxes, stoneLime, foot + Vector3.up * (drumH + 0.8f), new Vector3(w + 2.6f, 0.5f, w + 2.6f), null, PrimitiveType.Cylinder);
        Box(boxes, glowWarm, foot + Vector3.up * (drumH + 1.13f), new Vector3(w + 2.9f, 0.18f, w + 2.9f), null, PrimitiveType.Cylinder);   // the cornice, lit
        float domeFoot = drumH + 1.3f;
        Box(boxes, bronze, foot + Vector3.up * domeFoot, new Vector3(dome, dome, dome), null, PrimitiveType.Sphere);
        Box(boxes, brass, foot + Vector3.up * (domeFoot + dome * 0.2f), new Vector3(dome * 0.93f, 0.4f, dome * 0.93f), null, PrimitiveType.Cylinder);   // the brass bands round the dome
        Box(boxes, brass, foot + Vector3.up * (domeFoot + dome * 0.38f), new Vector3(dome * 0.66f, 0.35f, dome * 0.66f), null, PrimitiveType.Cylinder);
        float lantern = domeFoot + dome * 0.5f;
        Box(boxes, glowWhite, foot + Vector3.up * (lantern + 1.2f), new Vector3(3f, 2.4f, 3f), null, PrimitiveType.Cylinder);   // the lantern
        Cone(boxes, bronze, foot + Vector3.up * (lantern + 2.4f), 2f, 3.2f, 3);
        Box(boxes, glowGold, foot + Vector3.up * (lantern + 6.2f), Vector3.one * 1.4f, null, PrimitiveType.Sphere);
        const int piers = 12;
        for (int i = 0; i < piers; i++)
        {
            float a = az + i * 360f / piers;
            Vector3 p = foot + Radial(w * 0.62f, a);
            Box(boxes, stoneLime, p + Vector3.up * 5f, new Vector3(1.8f, 10f, 1.8f), Quaternion.Euler(0f, a, 0f));
            Box(boxes, glowWhite, p + Vector3.up * 9.3f, new Vector3(2.2f, 0.3f, 2.2f), Quaternion.Euler(0f, a, 0f));
        }
        Box(boxes, stoneLime, foot + Vector3.up * 10.4f, new Vector3(w * 1.3f, 0.8f, w * 1.3f), null, PrimitiveType.Cylinder);   // the canopy
        Box(boxes, bronze, foot + Vector3.up * 10.95f, new Vector3(w * 1.3f + 0.6f, 0.3f, w * 1.3f + 0.6f), null, PrimitiveType.Cylinder);
        float ph = h * 0.95f;
        for (int c = 0; c < 4; c++)
        {
            Vector3 dir = turn * new Vector3(c % 2 == 0 ? -0.7071f : 0.7071f, 0f, c < 2 ? -0.7071f : 0.7071f);
            Vector3 m = foot + dir * (w * 0.62f);
            Box(boxes, stoneLime, m + Vector3.up * (ph * 0.5f), new Vector3(4.5f, ph, 4.5f), turn);
            Cornice(boxes, m + Vector3.up * ph, 4.5f, 4.5f, az, stoneLime, glowWarm, 2);
            Box(boxes, glowWhite, m + Vector3.up * (ph + 2.2f), Vector3.one * 1.4f, null, PrimitiveType.Sphere);
        }
        Vector3 nrm = turn * Vector3.back;
        ClockFace(boxes, foot + nrm * (w * 0.5f + 0.5f) + Vector3.up * (drumH * 0.62f), nrm, Mathf.Min(w * 0.3f, drumH * 0.5f));
        if (lantern + 7f > 230f) Beacons.Add(foot + Vector3.up * (lantern + 7f));
        WashUp(sheets, foot, nrm, w * 0.5f, w * 0.9f, drumH * 0.7f, washCream);
    }

    /// A CATHEDRAL in Gotham gothic: a nave of tall arched windows under a pitched slate roof, buttresses down both
    /// flanks with a pinnacle on each and a flying bar up to the clerestory, an amber rose window on the face that
    /// looks at the House and the great door lit under it, twin towers either side under stepped stone spires with
    /// gilt finials, a spire over the crossing, a round apse at the far end. `az` is the way the nave runs (the
    /// House end is the back of it)
    static void Cathedral(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, float az, int seed)
    {
        if (Cut(foot, Mathf.Max(w, d) * 0.5f + 4f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        Vector3 fwd = turn * Vector3.forward, right = turn * Vector3.right;
        float naveW = w * 0.56f, naveH = h * 0.42f, naveD = d * 0.78f, tw = w * 0.2f, towerH = h * 0.7f;
        Vector3 naveC = foot + fwd * (d * 0.5f - naveD * 0.5f);   // the nave runs from the towers to the apse
        Tier(sheets, naveC, naveW, naveD, naveH, arcade, true, az);
        Gable(boxes, naveC + Vector3.up * naveH, naveD, naveW, az + 90f, brass);   // the ridge along the nave
        int nb = Mathf.Max(3, Mathf.RoundToInt(naveD / 9f));
        for (int i = 0; i < nb; i++)
        {
            float z = -naveD * 0.5f + naveD * (i + 0.5f) / nb;
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 b = naveC + fwd * z + right * (s * (naveW * 0.5f + 2.2f));
                Box(boxes, stoneLime, b + Vector3.up * (naveH * 0.33f), new Vector3(4.4f, naveH * 0.66f, 1.6f), turn);   // the pier
                Box(boxes, stoneLime, b - right * (s * 1.1f) + Vector3.up * (naveH * 0.78f), new Vector3(2.2f, naveH * 0.24f, 1.6f), turn);
                Cone(boxes, stoneLime, b + Vector3.up * (naveH * 0.66f), 1.2f, 7f, 4);                                   // the pinnacle
                Box(boxes, glowGold, b + Vector3.up * (naveH * 0.66f + 7.5f), Vector3.one * 0.7f, turn);
                Bar(boxes, stoneLime, b + Vector3.up * (naveH * 0.66f), naveC + fwd * z + right * (s * naveW * 0.5f) + Vector3.up * (naveH * 0.95f), 1.4f);   // the flyer
            }
        }
        Vector3 face = naveC - fwd * (naveD * 0.5f + 0.4f);
        Quaternion lie = turn * Quaternion.Euler(90f, 0f, 0f);
        Vector3 rose = face + Vector3.up * (naveH * 0.62f);
        Box(boxes, stoneChar, rose, new Vector3(naveW * 0.46f, 0.5f, naveW * 0.46f), lie, PrimitiveType.Cylinder);                       // the bezel
        Box(boxes, glowWarm, rose - fwd * 0.45f, new Vector3(naveW * 0.38f, 0.2f, naveW * 0.38f), lie, PrimitiveType.Cylinder);         // the rose, amber, proud of it
        Box(boxes, stoneChar, rose - fwd * 0.65f, new Vector3(naveW * 0.38f, 0.5f, 0.2f), turn);                                        // the tracery: a cross and a diagonal cross
        Box(boxes, stoneChar, rose - fwd * 0.65f, new Vector3(0.5f, naveW * 0.38f, 0.2f), turn);
        Box(boxes, stoneChar, rose - fwd * 0.65f, new Vector3(naveW * 0.38f, 0.4f, 0.2f), turn * Quaternion.Euler(0f, 0f, 45f));
        Box(boxes, stoneChar, rose - fwd * 0.65f, new Vector3(0.4f, naveW * 0.38f, 0.2f), turn * Quaternion.Euler(0f, 0f, 45f));
        Box(boxes, glowWhite, face - fwd * 0.1f + Vector3.up * 6f, new Vector3(naveW * 0.22f, 12f, 0.3f), turn);   // the great door, lit
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 tf = naveC - fwd * (naveD * 0.5f - tw * 0.5f) + right * (s * (naveW * 0.5f + tw * 0.5f));
            Tier(sheets, tf, tw, tw, towerH, pier, true, az);
            Cornice(boxes, tf + Vector3.up * towerH, tw, tw, az, stoneLime, glowWarm);
            Gargoyles(boxes, tf + Vector3.up * towerH, tw, tw, az, stoneLime);
            Cone(boxes, stoneLime, tf + Vector3.up * (towerH + 1.3f), tw * 0.5f, h * 0.28f, 7, glowGold);
            Box(boxes, glowGold, tf + Vector3.up * (towerH + 1.3f + h * 0.28f + 1.2f), Vector3.one * 1.6f, null, PrimitiveType.Sphere);
        }
        Vector3 crossing = naveC + fwd * (naveD * 0.1f) + Vector3.up * (naveH + naveW * 0.49f);
        Cone(boxes, stoneLime, crossing, naveW * 0.16f, h * 0.36f, 8, glowGold);
        Box(boxes, glowGold, crossing + Vector3.up * (h * 0.36f + 1.4f), Vector3.one * 1.8f, null, PrimitiveType.Sphere);
        Vector3 apse = naveC + fwd * (naveD * 0.5f);
        RoundTier(boxes, sheets, apse, naveW * 0.8f, naveH * 0.8f, arcade, true);
        Cone(boxes, roofSlate, apse + Vector3.up * (naveH * 0.8f), naveW * 0.4f, naveW * 0.3f, 5);
        float top = towerH + 1.3f + h * 0.28f;
        if (top > 230f) Beacons.Add(foot + Vector3.up * (top + 3f));
        WashUp(sheets, naveC, -fwd, naveD * 0.5f, naveW, naveH * 0.6f, washAmber);
    }

    /// A TENEMENT BLOCK: brick round a court, a long range across the front with two wings back from its ends,
    /// fire escapes down the street faces, water tanks and chimneys on the roofs, a row of lit shops along the
    /// foot, the odd red word. `az` faces outward
    static void Tenement(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, float az, int seed)
    {
        if (Cut(foot, Mathf.Max(w, d) * 0.5f + 3f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        int s0 = ((seed % 5) + 5) % 5;
        Vector3 fwd = turn * Vector3.forward, right = turn * Vector3.right;
        float wingW = Mathf.Max(8f, w * 0.3f), frontD = Mathf.Max(8f, d * 0.35f);
        Vector3 front = foot + fwd * (d * 0.5f - frontD * 0.5f);
        Tier(sheets, front, w, frontD, h, brick, true, az);
        Box(boxes, brickFlat, front + Vector3.up * (h + 0.25f), new Vector3(w + 0.4f, 0.5f, frontD + 0.4f), turn);
        RoofFurniture(boxes, front + Vector3.up * h, w, frontD, az, seed, 1);
        WaterTower(boxes, front + Vector3.up * (h + 0.5f) + turn * new Vector3(w * 0.3f, 0f, 0f), 3.2f, 5.5f, iron);
        for (int s = -1; s <= 1; s += 2)
        {
            float wh = h * (0.88f + 0.12f * CityTextures.Hash01(seed, s + 2, 7));
            Vector3 wing = foot + right * (s * (w * 0.5f - wingW * 0.5f)) - fwd * (frontD * 0.5f);
            Tier(sheets, wing, wingW, d - frontD, wh, brick, true, az);
            Box(boxes, brickFlat, wing + Vector3.up * (wh + 0.25f), new Vector3(wingW + 0.4f, 0.5f, d - frontD + 0.4f), turn);
            Chimney(boxes, wing + Vector3.up * (wh + 0.5f) + turn * new Vector3(0f, 0f, -(d - frontD) * 0.3f), 3f);
            FireEscape(boxes, wing, wingW, d - frontD, wh, az + (s < 0 ? -90f : 90f));   // down the outer flank of each wing
        }
        FireEscape(boxes, front, w, frontD, h, az, -w * 0.25f);
        FireEscape(boxes, front, w, frontD, h, az, w * 0.25f);
        Vector3 shop = front + fwd * (frontD * 0.5f + 0.25f);
        Box(boxes, glowWarm, shop + Vector3.up * 3.6f, new Vector3(w * 0.9f, 0.3f, 0.2f), turn);   // the shop fascias, lit
        if (s0 == 0) NeonWord(boxes, shop + fwd * 0.1f + Vector3.up * 5.2f, fwd, Mathf.Min(w * 0.4f, 8f), seed);
    }

    /// A POWERHOUSE: a long brick hall with tall arched windows under a shallow slate roof, chimney stacks banded in
    /// iron down one flank, the boiler doors lit along the front, a coal gantry off the back. `az` faces outward
    static void Powerhouse(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, float az, int seed)
    {
        if (Cut(foot, Mathf.Max(w, d) * 0.5f + 6f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        int s0 = ((seed % 5) + 5) % 5;
        Vector3 fwd = turn * Vector3.forward, right = turn * Vector3.right;
        Tier(sheets, foot, w, d, h, brickArch, true, az);
        Cornice(boxes, foot + Vector3.up * h, w, d, az, brickFlat, null, 2);
        Pitched(boxes, foot + Vector3.up * (h + 0.9f), w, d, az, d * 0.18f);
        // THE STACKS: three or four down the flank, a chimney's proportions and never a pipe's — half again the
        // hall's height and never past fifty-six metres (the hall itself may be forty-four), each three brick drums
        // tapering up from four and a half metres with an iron band at every joint and a cap, a red lamp on the first
        int stacks = 3 + s0 % 2;
        float sh = Mathf.Min(h * 1.5f, 56f);
        for (int k = 0; k < stacks; k++)
        {
            Vector3 p = foot + right * (w * 0.5f + 2.8f) + fwd * (-d * 0.38f + d * 0.76f * k / (stacks - 1));
            float y = 0f;
            for (int t = 0; t < 3; t++)
            {
                float th = sh * (t == 0 ? 0.4f : t == 1 ? 0.34f : 0.26f), dia = 4.6f - 0.7f * t;
                Box(boxes, brickFlat, p + Vector3.up * (y + th * 0.5f), new Vector3(dia, th, dia), null, PrimitiveType.Cylinder);
                y += th;
                Box(boxes, iron, p + Vector3.up * y, new Vector3(dia + 0.5f, 0.6f, dia + 0.5f), null, PrimitiveType.Cylinder);   // the band at the joint; the cap on top
            }
            if (k == 0) Box(boxes, glowRed, p + Vector3.up * (y + 1f), Vector3.one * 0.8f);
            Stack(p + Vector3.up * (y + 0.3f), 3.2f);   // its mouth: the smoke comes from here
        }
        for (int k = 0; k < 3; k++)   // the boiler doors
            Box(boxes, glowWarm, foot + fwd * (d * 0.5f + 0.2f) + right * ((k - 1) * w * 0.28f) + Vector3.up * 2.4f, new Vector3(3.4f, 4.4f, 0.3f), turn);
        Vector3 gantry = foot - fwd * (d * 0.5f + 5f);   // the coal gantry: two posts, a beam, a hopper
        for (int s = -1; s <= 1; s += 2) Box(boxes, iron, gantry + right * (s * 6f) + Vector3.up * (h * 0.6f), new Vector3(0.8f, h * 1.2f, 0.8f), turn);
        Box(boxes, iron, gantry + Vector3.up * (h * 1.2f), new Vector3(13.6f, 0.8f, 0.8f), turn);
        Box(boxes, iron, gantry + Vector3.up * (h * 0.9f), new Vector3(5f, 4f, 3f), turn);
        WashUp(sheets, foot, fwd, d * 0.5f, w * 0.9f, h * 0.8f, washAmber);
    }

    /// A CLOCK TOWER: a limestone base, a fluted shaft, a clock stage with a face on all four sides, a lit lantern
    /// and a stepped spire. `w` is the shaft's width
    static void ClockTower(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float h, float az, int seed)
    {
        if (Cut(foot, w * 0.9f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        float baseH = h * 0.15f, shaftH = h * 0.58f, clockH = h * 0.12f, lanternH = h * 0.05f;
        Tier(sheets, foot, w * 1.6f, w * 1.6f, baseH, arcade, true, az);
        Cornice(boxes, foot + Vector3.up * baseH, w * 1.6f, w * 1.6f, az, stoneLime, glowWarm);
        Gargoyles(boxes, foot + Vector3.up * baseH, w * 1.6f, w * 1.6f, az, stoneLime);
        float y = baseH;
        Tier(sheets, foot + Vector3.up * y, w, w, shaftH, pier, true, az);
        y += shaftH;
        Cornice(boxes, foot + Vector3.up * y, w, w, az, stoneLime, null);
        Tier(sheets, foot + Vector3.up * y, w * 1.1f, w * 1.1f, clockH, charcoal, false, az);
        ClockFaces(boxes, foot + Vector3.up * (y + clockH * 0.5f), w * 1.1f, w * 1.1f, az, Mathf.Min(w * 0.75f, clockH * 0.8f));
        y += clockH;
        Cornice(boxes, foot + Vector3.up * y, w * 1.1f, w * 1.1f, az, stoneLime, glowGold);
        y += 1.3f;
        Box(boxes, glowWhite, foot + Vector3.up * (y + lanternH * 0.5f), new Vector3(w * 0.5f, lanternH, w * 0.5f), turn);   // the lantern
        for (int c = 0; c < 4; c++)
            Box(boxes, stoneLime, foot + Vector3.up * (y + lanternH * 0.5f) + turn * new Vector3((c % 2 == 0 ? -1f : 1f) * w * 0.3f, 0f, (c < 2 ? -1f : 1f) * w * 0.3f), new Vector3(1.2f, lanternH, 1.2f), turn);
        y += lanternH;
        Box(boxes, stoneLime, foot + Vector3.up * (y + 0.3f), new Vector3(w * 0.8f, 0.6f, w * 0.8f), turn);
        CrownSpire(boxes, foot + Vector3.up * (y + 0.6f), w * 0.32f, h * 0.1f, stoneLime, glowGold);
        if (y + 0.6f + h * 0.1f + 3f > 230f) Beacons.Add(foot + Vector3.up * (y + 0.6f + h * 0.1f + 3f));
        WashUp(sheets, foot + Vector3.up * baseH, turn * Vector3.back, w * 0.5f, w * 0.9f, shaftH * 0.5f, washCream);
    }
}
