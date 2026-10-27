using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ARTICULATION, THE POSTER CUT (2026-10-16; DARK DECO 2026-10-07 under it). A
/// room is a box with six faces; a poster room is a box with a few BIG shapes
/// drawn on it, the way the Animated Series paints a hall: flat colour fields,
/// one string course, a bold cornice, a pilaster where the wall needs a beat,
/// one chandelier or one statue. Every civic, office and underbelly room gets:
/// ONE stone string course a third of the way up, a two-step cornice at the
/// top, fluted pilasters no closer than six metres, a stepped soffit over
/// every doorway (a sunburst of five fat brass rays over the widest one), a
/// torchiere no closer than eight metres, a shaft where two built walls meet,
/// ONE ring of stepped ceiling tray with a lit lip in the tall rooms, one row
/// of globe pendants, and ONE hero prop (a bronze figure on a stepped plinth
/// against the wall, a clock face, a stack of crates — the first and last are
/// lips: gameplay). What went with the cut: the skirting and its brass line,
/// the dado rail, the chevron frieze, the inset panels, the radiators, the
/// jamb pilasters, the ceiling beams and their lamp strips, the two inner
/// tray rings and the wall-wash lights, the second pendant row, the floor
/// border and its corner plates, the furniture (desks, partitions, urns,
/// benches, drums, pallets, spools, floor torchieres). How much of what is
/// left a room gets is ONE knob, LevelKit.Ornament (MovementConfig.ornament,
/// 0..1, 0.35 the poster default). Every trim is a look (welded, two meshes
/// per room); the hero prop is solid and placed only where the room has left
/// the space. Room() calls this for every room in the utopia, the underbelly
/// and the office; the Works and the surface have their own density pass
/// (LevelKitDense). Cyan and red are not used here: every light is warm.
/// </summary>
public static partial class LevelKit
{
    /// Rooms get their trims and their props (off for previews that want the bare box)
    public static bool Detail = true;

    static float ornamentSet = -1f;

    /// THE ORNAMENT KNOB (2026-10-16): 0 = bare masses (the cornice, the string course, the gameplay props and nothing
    /// else), 0.35 = the poster default, 1 = every ornament the kit knows. Read at build time from the loaded
    /// MovementConfig's `ornament` (MovementConfigLook.cs); set it here to override for one build (a preview, a
    /// thumbnail), set it negative to go back to the config
    public static float Ornament
    {
        get => ornamentSet >= 0f ? ornamentSet : MovementConfig.LookSource != null ? Mathf.Clamp01(MovementConfig.LookSource.ornament) : 0.35f;
        set => ornamentSet = value < 0f ? -1f : Mathf.Clamp01(value);
    }

    /// Counts climb with the knob up to the poster default and no further: one pilaster per 6 m is the ceiling
    static float OrnamentScale => Mathf.Clamp01(Ornament / 0.35f);
    /// Is the knob at least this far up? (what each ornament asks before it draws itself)
    static bool Ornate(float atLeast) => Ornament >= atLeast - 1e-4f;

    struct Style
    {
        public Material trim, shaft, rail, lamp, post, inlay;
        public Color glow;
        public bool tray, torchieres;
    }

    static readonly Dictionary<Zone, Style> styles = new Dictionary<Zone, Style>();
    static System.Random arng = new System.Random(7);
    static float AR(float a, float b) => a + (float)arng.NextDouble() * (b - a);

    static Style StyleFor(Zone z)
    {
        if (styles.TryGetValue(z, out var s) && s.trim != null) return s;
        switch (z)
        {
            case Zone.Underbelly:
                // Iron and brick under the city: riveted trims, brick piers, bronze rails, gas-lamp amber
                s = new Style
                {
                    trim = RivetIron, shaft = Brick, rail = Bronze, lamp = WindowAmber, post = RivetIron, inlay = Bronze,
                    glow = new Color(1f, 0.62f, 0.3f), tray = false, torchieres = true,
                };
                break;
            case Zone.Office:
                // The company's tower: black marble, fluted plaster, brass everywhere
                s = new Style
                {
                    trim = Marble, shaft = Fluted, rail = Brass, lamp = LampCream, post = Marble, inlay = Brass,
                    glow = new Color(1f, 0.86f, 0.62f), tray = true, torchieres = true,
                };
                break;
            default:
                // The civic halls: limestone trims, fluted pilasters, brass rails
                s = new Style
                {
                    trim = Limestone, shaft = Fluted, rail = Brass, lamp = LampCream, post = Limestone, inlay = Brass,
                    glow = new Color(1f, 0.88f, 0.68f), tray = true, torchieres = true,
                };
                break;
        }
        styles[z] = s;
        return s;
    }

    /// One wall's trims: the u-spans left once the holes that reach the band [ya, yb] are cut out
    static List<(float a, float b)> Spans(Face face, float u0, float u1, float ya, float yb, Hole[] holes)
    {
        var spans = new List<(float a, float b)> { (u0, u1) };
        if (holes == null) return spans;
        foreach (var h in holes)
        {
            if (h.face != face || h.v1 < ya - 0.15f || h.v0 > yb + 0.15f) continue;
            float ha = h.u0 - 0.2f, hb = h.u1 + 0.2f;
            for (int i = spans.Count - 1; i >= 0; i--)
            {
                var (a, b) = spans[i];
                if (hb <= a || ha >= b) continue;
                spans.RemoveAt(i);
                if (ha > a + 0.3f) spans.Add((a, ha));
                if (hb < b - 0.3f) spans.Add((hb, b));
            }
        }
        return spans;
    }

    /// Is `u` on this face clear of every hole that reaches the band [ya, yb]?
    static bool Free(Face face, float u, float ya, float yb, Hole[] holes, float margin = 0.5f)
    {
        if (holes == null) return true;
        foreach (var h in holes)
            if (h.face == face && h.v1 >= ya - 0.15f && h.v0 <= yb + 0.15f && u > h.u0 - margin && u < h.u1 + margin) return false;
        return true;
    }

    /// A point on a wall's inner face: `u` along it, `y` up, `depth` into the room; and the box size for a trim
    /// `len` along the wall, `tall`, `deep` into the room
    static Vector3 OnFace(Face face, Vector3 min, Vector3 max, float u, float y, float depth) => face switch
    {
        Face.North => new Vector3(u, y, max.z - depth),
        Face.South => new Vector3(u, y, min.z + depth),
        Face.East => new Vector3(max.x - depth, y, u),
        _ => new Vector3(min.x + depth, y, u),
    };
    static Vector3 Along(Face face, float len, float tall, float deep) => face == Face.North || face == Face.South ? new Vector3(len, tall, deep) : new Vector3(deep, tall, len);
    static bool Has(Faces faces, Face f) => (faces & (f == Face.North ? Faces.North : f == Face.South ? Faces.South : f == Face.East ? Faces.East : Faces.West)) != 0;
    /// A turn in the wall's own plane: `deg` from the along axis toward up (the chevron, the sunburst rays)
    static Quaternion InPlane(Face face, float deg) => face == Face.North || face == Face.South ? Quaternion.Euler(0f, 0f, deg) : Quaternion.Euler(-deg, 0f, 0f);
    /// A turn that lays a cylinder's axis along the wall's normal (a clock face, a disc on the wall)
    static Quaternion FaceOn(Face face) => face == Face.North || face == Face.South ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.Euler(0f, 0f, 90f);
    /// The way into the room off this face
    static Vector3 Inward(Face face) => face == Face.North ? Vector3.back : face == Face.South ? Vector3.forward : face == Face.East ? Vector3.left : Vector3.right;

    /// Everything a room gets on top of its six faces. `min`..`max` are the interior extents.
    public static void Articulate(Transform parent, Vector3 min, Vector3 max, Faces faces, Hole[] holes)
    {
        Zone z = Current;
        if (z == Zone.Works || z == Zone.Surface) return;   // the density pass dresses those
        float dx = max.x - min.x, dz = max.z - min.z, h = max.y - min.y;
        if (dx < 4f || dz < 4f || h < 3f) return;
        var s = StyleFor(z);
        arng = new System.Random(Mathf.RoundToInt(min.x * 7f + min.z * 13f + max.y * 3f));
        var art = new MeshBaker();
        var fine = new MeshBaker();
        float floor = min.y, ceil = max.y;
        bool tall = h >= 6f;
        float course = floor + h / 3f;                     // the one string course
        float capTop = ceil - (tall ? 0.9f : 0.7f);         // where the pilasters stop: under the cornice
        float scale = OrnamentScale;
        float pilPitch = scale > 0.01f ? 6f / scale : 0f;   // one per 6 m at the default, further apart below it, none at 0
        float torchPitch = scale > 0.01f ? 8f / scale : 0f;
        Face[] all = { Face.North, Face.South, Face.East, Face.West };
        foreach (var face in all)
        {
            if (!Has(faces, face)) continue;
            bool alongX = face == Face.North || face == Face.South;
            float u0 = alongX ? min.x : min.z, u1 = alongX ? max.x : max.z, len = u1 - u0;
            // The string course: one stone band a third of the way up, cut at every hole
            foreach (var (a, b) in Spans(face, u0, u1, course - 0.15f, course + 0.15f, holes))
                art.Add(s.trim, OnFace(face, min, max, (a + b) * 0.5f, course, 0.05f), Along(face, b - a, 0.3f, 0.1f));
            // The cornice: two bold steps, the upper deeper than the lower
            foreach (var (a, b) in Spans(face, u0, u1, ceil - 0.7f, ceil, holes))
            {
                art.Add(s.trim, OnFace(face, min, max, (a + b) * 0.5f, ceil - 0.5f, 0.07f), Along(face, b - a, 0.22f, 0.14f));
                art.Add(s.trim, OnFace(face, min, max, (a + b) * 0.5f, ceil - 0.22f, 0.13f), Along(face, b - a, 0.44f, 0.26f));
            }
            // Pilasters into bays, never closer than the pitch
            if (pilPitch > 0f)
            {
                int bays = Mathf.Max(1, Mathf.FloorToInt(len / pilPitch));
                for (int i = 1; i < bays; i++)
                {
                    float u = u0 + len * i / bays;
                    if (Free(face, u, floor, ceil, holes, 0.8f)) DecoPilaster(art, s, face, min, max, u, floor, capTop, 0.7f);
                }
            }
            // A stepped soffit over every doorway; the sunburst over the widest one on this wall
            if (holes != null)
            {
                int widest = -1;
                float wide = 0f;
                for (int i = 0; i < holes.Length; i++)
                {
                    var hole = holes[i];
                    if (hole.face != face || hole.v0 > floor + 0.3f) continue;   // a doorway stands on the floor; a window does not
                    Soffit(art, s, face, min, max, hole, ceil);
                    if (hole.u1 - hole.u0 > wide) { wide = hole.u1 - hole.u0; widest = i; }
                }
                if (widest >= 0 && Ornate(0.3f)) Sunburst(fine, s.rail, face, min, max, holes[widest], ceil - 0.75f);
            }
            // Torchieres, never closer than the pitch
            if (s.torchieres && torchPitch > 0f)
            {
                int n = Mathf.Max(1, Mathf.FloorToInt(len / torchPitch));
                for (int i = 0; i < n; i++)
                {
                    float u = u0 + len * (i + 0.5f) / n;
                    if (!Free(face, u, floor + 1.2f, floor + 2.9f, holes, 0.6f)) continue;
                    Torchiere(parent, fine, s, face, min, max, u, floor);
                }
            }
            // One big chevron at the centre of a long wall over the course, when the knob is well up
            if (Ornate(0.6f) && len >= 8f && Free(face, (u0 + u1) * 0.5f, course + 0.3f, course + 1.7f, holes, 1.4f))
                Chevron(fine, s.rail, face, min, max, (u0 + u1) * 0.5f, course + 0.95f, 1.3f);
        }
        // A shaft where two built walls meet: fluted, on a stone foot, under a stone cap
        if (Ornate(0.25f))
            foreach (var (cx, cz, ok) in new[]
            {
                (max.x - 0.18f, max.z - 0.18f, Has(faces, Face.North) && Has(faces, Face.East)),
                (min.x + 0.18f, max.z - 0.18f, Has(faces, Face.North) && Has(faces, Face.West)),
                (max.x - 0.18f, min.z + 0.18f, Has(faces, Face.South) && Has(faces, Face.East)),
                (min.x + 0.18f, min.z + 0.18f, Has(faces, Face.South) && Has(faces, Face.West)),
            })
            {
                if (!ok) continue;
                art.Add(s.shaft, new Vector3(cx, floor + h * 0.5f, cz), new Vector3(0.36f, h, 0.36f));
                art.Add(s.post, new Vector3(cx, floor + 0.2f, cz), new Vector3(0.46f, 0.4f, 0.46f));
                art.Add(s.post, new Vector3(cx, ceil - 0.2f, cz), new Vector3(0.54f, 0.22f, 0.54f));
            }

        // The ceiling: one ring of stepped tray with a lit lip in the tall rooms, one row of globe pendants
        if ((faces & Faces.Ceiling) != 0)
        {
            bool alongZ = dx <= dz;   // the pendant row runs the long way
            float run = alongZ ? dz : dx;
            if (s.tray && tall && Ornate(0.3f)) Tray(art, fine, s, min, max, faces, holes);
            if (h >= 4.5f)
            {
                int cols = Mathf.Max(1, Mathf.RoundToInt(run / 12f));
                float drop = Mathf.Clamp(h * 0.22f, 1f, 2.2f);
                for (int i = 0; i < cols; i++)
                {
                    float p = (alongZ ? min.z : min.x) + run * (i + 0.5f) / cols;
                    float q = alongZ ? (min.x + max.x) * 0.5f : (min.z + max.z) * 0.5f;
                    Pendant(parent, fine, s, alongZ ? new Vector3(q, ceil, p) : new Vector3(p, ceil, q), drop);
                }
            }
        }

        // The floor: a brass sunburst at the room's centre, only when the knob is well up (a floor is a colour field)
        if ((faces & Faces.Floor) != 0 && Ornate(0.6f))
        {
            float radius = Mathf.Min(dx, dz) * 0.28f;
            if (radius >= 2.5f) FloorSunburst(fine, s.inlay, new Vector3((min.x + max.x) * 0.5f, floor + 0.008f, (min.z + max.z) * 0.5f), Mathf.Min(radius, 5f));
        }

        art.Flush(parent, "Articulation", true);
        fine.Flush(parent, "ArticulationFine");
        if (Ornate(0.2f)) Hero(parent, min, max, faces, holes, z, s);
    }

    /// A fluted pilaster: a stone plinth, the shaft (the fluting is in its surface), one stone cap
    static void DecoPilaster(MeshBaker art, Style s, Face face, Vector3 min, Vector3 max, float u, float floor, float capTop, float width)
    {
        float shaft0 = floor + 0.4f, shaft1 = capTop - 0.3f;
        if (shaft1 - shaft0 < 1.5f) return;
        art.Add(s.trim, OnFace(face, min, max, u, floor + 0.2f, 0.12f), Along(face, width + 0.2f, 0.4f, 0.24f));
        art.Add(s.shaft, OnFace(face, min, max, u, (shaft0 + shaft1) * 0.5f, 0.09f), Along(face, width, shaft1 - shaft0 + 0.02f, 0.18f));
        art.Add(s.trim, OnFace(face, min, max, u, capTop - 0.15f, 0.13f), Along(face, width + 0.3f, 0.3f, 0.26f));
    }

    /// A stepped soffit over a doorway: two stone steps on the wall over the frame, the lower the wider
    static void Soffit(MeshBaker art, Style s, Face face, Vector3 min, Vector3 max, Hole hole, float ceil)
    {
        float w = hole.u1 - hole.u0;
        if (w < 1.2f || hole.v1 > ceil - 0.8f) return;
        float uc = (hole.u0 + hole.u1) * 0.5f, y = hole.v1 + 0.01f;
        art.Add(s.trim, OnFace(face, min, max, uc, y + 0.2f, 0.09f), Along(face, w + 0.9f, 0.4f, 0.18f));
        art.Add(s.trim, OnFace(face, min, max, uc, y + 0.5f, 0.06f), Along(face, w + 0.3f, 0.2f, 0.12f));
    }

    /// ONE big chevron: two brass bars leaning together to an apex at `u`
    static void Chevron(MeshBaker fine, Material rail, Face face, Vector3 min, Vector3 max, float u, float y, float arm)
    {
        float dx = arm * 0.5f * Mathf.Cos(42f * Mathf.Deg2Rad);
        fine.Add(rail, OnFace(face, min, max, u - dx, y, 0.03f), Along(face, arm, 0.12f, 0.04f), InPlane(face, 42f));
        fine.Add(rail, OnFace(face, min, max, u + dx, y, 0.035f), Along(face, arm, 0.12f, 0.04f), InPlane(face, -42f));
    }

    /// A sunburst over a doorway: a brass keystone over the soffit, five fat rays fanning from it (seven when the knob is high)
    static void Sunburst(MeshBaker fine, Material rail, Face face, Vector3 min, Vector3 max, Hole hole, float under)
    {
        float w = hole.u1 - hole.u0;
        if (w < 1.6f) return;
        float y = hole.v1 + 0.65f;
        float r = Mathf.Min(w * 0.5f, under - y - 0.1f, 2.6f);
        if (r < 0.9f) return;
        float uc = (hole.u0 + hole.u1) * 0.5f;
        fine.Add(rail, OnFace(face, min, max, uc, y + 0.1f, 0.05f), Along(face, 0.5f, 0.28f, 0.1f));
        int rays = Ornate(0.6f) ? 7 : 5;
        float len = r - 0.4f;
        for (int i = 0; i < rays; i++)
        {
            float a = Mathf.Lerp(18f, 162f, i / (rays - 1f)) * Mathf.Deg2Rad;
            fine.Add(rail, OnFace(face, min, max, uc + Mathf.Cos(a) * (0.3f + len * 0.5f), y + Mathf.Sin(a) * (0.3f + len * 0.5f), 0.03f), Along(face, len, 0.14f, 0.04f), InPlane(face, a * Mathf.Rad2Deg));
        }
    }

    /// A torchiere on the wall: a bronze stem, a bronze bowl with a cream lamp in it, and a warm light thrown up
    static void Torchiere(Transform parent, MeshBaker fine, Style s, Face face, Vector3 min, Vector3 max, float u, float floor)
    {
        fine.Add(Bronze, OnFace(face, min, max, u, floor + 1.98f, 0.1f), Along(face, 0.06f, 0.45f, 0.06f), null, PrimitiveType.Cylinder);   // a cylinder is twice its y scale tall
        fine.Add(Bronze, OnFace(face, min, max, u, floor + 2.48f, 0.17f), Along(face, 0.28f, 0.1f, 0.28f), null, PrimitiveType.Cylinder);
        fine.Add(s.lamp, OnFace(face, min, max, u, floor + 2.585f, 0.17f), Along(face, 0.22f, 0.012f, 0.22f), null, PrimitiveType.Cylinder);
        Vector3 inward = Inward(face);
        var l = Light(parent, OnFace(face, min, max, u, floor + 2.75f, 0.3f), 7f, 1.1f);
        l.color = s.glow;
        l.type = LightType.Spot;
        l.spotAngle = 120f;
        l.innerSpotAngle = 50f;
        l.transform.rotation = Quaternion.LookRotation(Vector3.up + inward * 0.25f, inward);
    }

    /// A globe pendant: a brass canopy on the ceiling, a chain, a cream globe, warm light under it
    static void Pendant(Transform parent, MeshBaker fine, Style s, Vector3 ceilingAt, float drop)
    {
        fine.Add(s.rail, ceilingAt + Vector3.down * 0.025f, new Vector3(0.32f, 0.04f, 0.32f), null, PrimitiveType.Cylinder);
        fine.Add(Bronze, ceilingAt + Vector3.down * ((drop - 0.26f) * 0.5f + 0.04f), new Vector3(0.03f, (drop - 0.26f) * 0.5f, 0.03f), null, PrimitiveType.Cylinder);
        fine.Add(s.rail, ceilingAt + Vector3.down * (drop - 0.3f), new Vector3(0.14f, 0.08f, 0.14f), null, PrimitiveType.Cylinder);
        fine.Add(s.lamp, ceilingAt + Vector3.down * drop, new Vector3(0.64f, 0.64f, 0.64f), null, PrimitiveType.Sphere);
        var l = Light(parent, ceilingAt + Vector3.down * (drop + 0.35f), 14f, 1.6f);   // one row now: each carries a little more
        l.color = s.glow;
    }

    /// A brass sunburst inlaid in the floor: a disc and eight fat rays
    static void FloorSunburst(MeshBaker fine, Material inlay, Vector3 centre, float r)
    {
        fine.Add(inlay, centre, new Vector3(0.9f, 0.006f, 0.9f), null, PrimitiveType.Cylinder);
        const int rays = 8;
        float len = r - 0.7f;
        for (int i = 0; i < rays; i++)
        {
            Quaternion rot = Quaternion.Euler(0f, i * 360f / rays, 0f);
            fine.Add(inlay, centre + rot * Vector3.forward * (0.55f + len * 0.5f), new Vector3(0.16f, 0.012f, len), rot);
        }
    }

    /// The stepped ceiling tray: ONE ring stepping down along every built wall, a lit cream lip under it. The ring
    /// stops at anything cut up to the ceiling.
    static void Tray(MeshBaker art, MeshBaker fine, Style s, Vector3 min, Vector3 max, Faces faces, Hole[] holes)
    {
        float ceil = max.y, dx = max.x - min.x, dz = max.z - min.z;
        const float inset = 0.5f, width = 1.4f, drop = 0.7f;
        if (inset + width > Mathf.Min(dx, dz) * 0.3f) return;
        Face[] all = { Face.North, Face.South, Face.East, Face.West };
        foreach (var face in all)
        {
            if (!Has(faces, face)) continue;
            bool alongX = face == Face.North || face == Face.South;
            // The x-running sides run the full span; the z-running ones fill in between them
            float u0 = (alongX ? min.x : min.z) + (alongX ? inset : inset + width), u1 = (alongX ? max.x : max.z) - (alongX ? inset : inset + width);
            if (u1 - u0 < 1f) continue;
            foreach (var (a, b) in Spans(face, u0, u1, ceil - drop - 0.1f, ceil, holes))
            {
                art.Add(s.trim, OnFace(face, min, max, (a + b) * 0.5f, ceil - drop * 0.5f + 0.005f, inset + width * 0.5f), Along(face, b - a, drop + 0.01f, width));
                fine.Add(s.lamp, OnFace(face, min, max, (a + b) * 0.5f, ceil - drop + 0.02f, inset + width + 0.015f), Along(face, b - a - 0.2f, 0.04f, 0.03f));
            }
        }
    }

    /// THE HERO: one prop per room, solid where it can be touched, only where the room left the space. A hall gets a
    /// bronze figure on a stepped plinth against the long wall (its tiers are lips); the office a clock face on the
    /// wall over the string course (a look); the underbelly a stack of crates (two lips)
    static void Hero(Transform parent, Vector3 min, Vector3 max, Faces faces, Hole[] holes, Zone z, Style s)
    {
        float floor = min.y, h = max.y - min.y;
        Physics.SyncTransforms();
        // The wall it goes against: the first long side that is built; a point along it the holes leave free
        bool alongZ = max.z - min.z >= max.x - min.x;
        Face[] order = alongZ ? new[] { Face.West, Face.East, Face.South, Face.North } : new[] { Face.South, Face.North, Face.West, Face.East };
        foreach (var face in order)
        {
            if (!Has(faces, face)) continue;
            bool alongX = face == Face.North || face == Face.South;
            float u0 = alongX ? min.x : min.z, u1 = alongX ? max.x : max.z;
            if (u1 - u0 < 8f) continue;
            float u = AR(u0 + 3f, u1 - 3f);
            switch (z)
            {
                case Zone.Office:
                {
                    // The clock sits in the middle bay, between the pilasters, well over the course
                    float scale = OrnamentScale, len = u1 - u0;
                    int bays = scale > 0.01f ? Mathf.Max(1, Mathf.FloorToInt(len * scale / 6f)) : 1;
                    u = u0 + len * (bays / 2 + 0.5f) / bays;
                    float course = floor + h / 3f, dia = Mathf.Clamp(h * 0.28f, 1.6f, 3.2f), y = course + 0.7f + dia * 0.5f;
                    if (y + dia * 0.5f > max.y - 1.1f || !Free(face, u, course + 0.3f, y + dia * 0.5f + 0.2f, holes, dia * 0.5f + 0.6f)) continue;
                    var fine = new MeshBaker();
                    Clock(fine, s, face, min, max, u, y, dia);
                    fine.Flush(parent, "ArticulationHero");
                    return;
                }
                case Zone.Underbelly:
                {
                    if (!Free(face, u, floor, floor + 2.5f, holes, 2f)) continue;
                    Vector3 at = OnFace(face, min, max, u, floor, 0.75f);
                    Vector3 big = Along(face, 2.2f, 1f, 1.2f), small = Along(face, 1.2f, 0.8f, 1f);
                    if (!Clear(at + Vector3.up * 0.95f, Along(face, 2.4f, 1.9f, 1.4f))) continue;
                    Lip(parent, at + Vector3.up * 0.5f, big, WoodPanel, "Crate");
                    Lip(parent, at + Vector3.up * 1.4f + (alongX ? Vector3.left : Vector3.back) * 0.4f, small, WoodPanel, "Crate");
                    Physics.SyncTransforms();
                    return;
                }
                default:
                {
                    if (!Free(face, u, floor, floor + 3.5f, holes, 2.4f)) continue;
                    float size = 3.2f;
                    Vector3 at = OnFace(face, min, max, u, floor, size * 0.5f + 0.2f);
                    if (!Clear(at + Vector3.up * 2f, new Vector3(size + 0.4f, 4f, size + 0.4f))) continue;
                    SteppedPlinth(parent, at, size, 2, 0.9f);
                    Physics.SyncTransforms();
                    return;
                }
            }
        }
    }

    /// A clock face on the wall: a brass ring, a lit cream disc, bronze hands at ten to two
    static void Clock(MeshBaker fine, Style s, Face face, Vector3 min, Vector3 max, float u, float y, float dia)
    {
        Quaternion on = FaceOn(face);
        fine.Add(s.rail, OnFace(face, min, max, u, y, 0.03f), new Vector3(dia + 0.3f, 0.03f, dia + 0.3f), on, PrimitiveType.Cylinder);
        fine.Add(s.lamp, OnFace(face, min, max, u, y, 0.06f), new Vector3(dia, 0.02f, dia), on, PrimitiveType.Cylinder);
        fine.Add(Bronze, OnFace(face, min, max, u, y, 0.1f), new Vector3(0.16f, 0.02f, 0.16f), on, PrimitiveType.Cylinder);
        float hour = 120f, minute = 60f;   // ten to two
        fine.Add(Bronze, OnFace(face, min, max, u + Mathf.Cos(hour * Mathf.Deg2Rad) * dia * 0.14f, y + Mathf.Sin(hour * Mathf.Deg2Rad) * dia * 0.14f, 0.1f), Along(face, dia * 0.3f, 0.07f, 0.02f), InPlane(face, hour));
        fine.Add(Bronze, OnFace(face, min, max, u + Mathf.Cos(minute * Mathf.Deg2Rad) * dia * 0.2f, y + Mathf.Sin(minute * Mathf.Deg2Rad) * dia * 0.2f, 0.125f), Along(face, dia * 0.42f, 0.05f, 0.02f), InPlane(face, minute));
    }

    /// A desk lamp: a brass stem, a cream shade, a little warm light (the office kit's desks still ask for one)
    static void DeskLamp(Transform parent, Vector3 at, Color glow)
    {
        Decor(parent, PrimitiveType.Cylinder, at + Vector3.up * 0.02f, Quaternion.identity, new Vector3(0.16f, 0.02f, 0.16f), Brass, "LampFoot");
        Decor(parent, PrimitiveType.Cylinder, at + Vector3.up * 0.2f, Quaternion.identity, new Vector3(0.025f, 0.18f, 0.025f), Brass, "LampStem");
        Decor(parent, PrimitiveType.Cylinder, at + Vector3.up * 0.42f, Quaternion.identity, new Vector3(0.26f, 0.08f, 0.26f), LampCream, "LampShade");
        var l = Light(parent, at + Vector3.up * 0.35f, 3f, 0.8f);
        l.color = glow;
    }
}
