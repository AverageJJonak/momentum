using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE ROOF KIT, dressed DARK DECO since 2026-10-07 (Dress, RoofKitJazz: the solids never moved).
/// What Chapter 3's sky levels are built from, once: the plan's
/// towers made solid with a slab on every top you run on (a stack, a twin
/// blade, a round tower with its halo decks, an arcology, and a SPLIT tower
/// with a sky lobby open to the air through the middle of it), the ways
/// between (girder bridges, broken ones, a tram viaduct, a covered court, a
/// fire escape up a face), the district's own things (sweep-light masts,
/// beacons, mines, thermals), the dressing of a roof (a garden), and the
/// NEIGHBOURS: the city within a jump of a route, blocks at the route's own
/// height with roofs you can land on, links between them, and signs on the
/// faces that look at you. A level calls Begin with its root, config,
/// district and sheet baker, then builds; Level12Builder and Level13Builder
/// both stand on this.
/// </summary>
public static partial class RoofKit
{
    static Transform root;
    static MovementConfig cfg;
    static District district;
    static SheetBaker sheets;
    static float groundY;

    /// How far above a tower's drawn top the slab you run on sits. The plan draws a roof quad at exactly the height a
    /// level names, and a slab on the same plane fights it, flickering: everything the kit puts on a roof (the slab,
    /// a bridge's end, a beacon, a garden, a lobby floor) stands this much higher. Things a level places by the
    /// number (a pad, a rail) snap or add it themselves.
    public const float Proud = 0.05f;

    /// EVERY ROOF THE ROUTE TOUCHES, recorded as it is laid (Roof): what RoofLife dresses once the whole route stands
    public struct RoofRec { public Vector3 top; public float w, d, az; public bool neighbour; }
    public static readonly List<RoofRec> Roofs = new List<RoofRec>();
    /// Every fast line drawn on a roof (FastLine): RoofLife keeps its pieces three metres off them
    public struct LineRec { public Vector3 a, b; }
    public static readonly List<LineRec> Lines = new List<LineRec>();
    /// Rectangles nothing may be put in (a roof garden): centre, half extents, yaw
    public struct KeepRec { public Vector3 centre; public float hx, hz, az; }
    public static readonly List<KeepRec> KeepOut = new List<KeepRec>();
    static bool taggingNeighbour;

    public static void Begin(Transform levelRoot, MovementConfig config, District d, SheetBaker sheetBaker, float ground)
    {
        root = levelRoot;
        cfg = config;
        district = d;
        sheets = sheetBaker;
        groundY = ground;
        Roofs.Clear();
        Lines.Clear();
        KeepOut.Clear();
        taggingNeighbour = false;
    }

    // ------------------------------------------------------------ towers you can stand on

    /// The plan's plain stack (two storey-stacks, the upper set back), with solids inside it and a slab on each top.
    /// Returns the roof height. The setback is a ledge all the way round.
    public static float StackTower(Vector3 foot, float w, float d, float h, float az, bool lit = true)
    {
        MenuCity.RoofPlates = false;   // the slab you run on is the roof: no lit plate over it
        MenuCity.OuterTower(SurfaceKit.Far, sheets, foot, w, d, h, az, 3, lit);
        MenuCity.RoofPlates = true;
        Furnish();
        Quaternion rot = Quaternion.Euler(0f, az, 0f);
        float h1 = h * 0.62f, w2 = w * 0.82f, d2 = d * 0.82f;
        SurfaceKit.Solid(foot + Vector3.up * (h1 * 0.5f), new Vector3(w, h1, d), rot);
        SurfaceKit.Solid(foot + Vector3.up * (h1 + (h - h1) * 0.5f), new Vector3(w2, h - h1, d2), rot);
        Roof(foot + Vector3.up * h, w2 - 0.2f, d2 - 0.2f, az);
        Roof(foot + Vector3.up * h1, w - 0.2f, d - 0.2f, az);   // the ledge: the slab goes under the upper stack, which is fine
        Dress(foot, w, d, 0f, h1, az, false, false);
        Dress(foot, w2, d2, h1, h - h1, az, true, true);
        int jz = JazzSeed(foot);
        Jazz(foot, w, d, 0f, h1, az, jz, false);
        Jazz(foot, w2, d2, h1, h - h1, az, jz + 1, true);
        return h;
    }

    static Material ivory, gold;

    static void EnsureDress()
    {
        if (ivory != null) return;
        ivory = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        ivory.SetColor("_BaseColor", new Color(0.13f, 0.13f, 0.15f));   // charcoal stone: the dark body the brass is drawn on
        ivory.SetFloat("_Smoothness", 0.45f);
        gold = Sign(new Color(1.6f, 1.25f, 0.5f));
    }

    /// The company's own storeys on a tier, as geometry at arm's length, DARK DECO: a charcoal slab edge at every
    /// floor, a FLUTED shaft between every window column with a row of amber windows in each bay, a fluted corner
    /// shaft; on a top tier a TRIPLE stepped stone cornice under the roof's edge with a gold lip, stepped gargoyles
    /// on the corners, a fire escape zigzagging down one face, flagpoles, and the roofline's things (a water tower,
    /// a chimney, a mast) on brackets OUTSIDE the walkable roof; and a spine of gold light up the two faces that
    /// look across the route. Nothing here is solid: every piece stays inside the reach of a wall-run's clearance,
    /// and nothing stands on the roof you run on.
    public static void Dress(Vector3 foot, float w, float d, float y0, float h, float az, bool top, bool spine)
    {
        EnsureJazz();   // the gargoyles' stone and the dress's own materials
        Quaternion rot = Quaternion.Euler(0f, az, 0f);
        const float off = 0.1f;   // the window sheet sits 0.03 off the wall on these levels; the slabs sit on the sheet
        int rows = Mathf.Max(1, Mathf.RoundToInt(h / 3.8f));
        int seed = JazzSeed(foot) + Mathf.RoundToInt(y0);
        for (int f = 0; f < 4; f++)
        {
            Vector3 nrm = rot * (f == 0 ? Vector3.back : f == 1 ? Vector3.forward : f == 2 ? Vector3.left : Vector3.right);
            Vector3 along = Vector3.Cross(Vector3.up, nrm);
            bool alongX = f < 2;
            float width = alongX ? w : d, half = alongX ? d * 0.5f : w * 0.5f;
            Quaternion faceRot = Quaternion.LookRotation(nrm, Vector3.up);
            Vector3 face = foot + nrm * (half + off);
            int cols = Mathf.Max(1, Mathf.RoundToInt(width / 3.4f));
            for (int j = 0; j <= rows; j++)
                SurfaceKit.Look(ivory, face + Vector3.up * (y0 + h * j / rows), new Vector3(width + 0.2f, 0.5f, 0.22f), faceRot);
            for (int i = 1; i < cols; i++)
                SurfaceKit.Look(LevelKit.Fluted, face + nrm * 0.1f + Vector3.up * (y0 + h * 0.5f) + along * (width * (i / (float)cols - 0.5f)), new Vector3(0.6f, h, 0.3f), faceRot);
            // A row of amber windows in every bay of every storey: the picture at night
            for (int j = 0; j < rows; j++)
                for (int i = 0; i < cols; i++)
                {
                    float bw = width / cols - 1.3f;
                    if (bw < 0.8f) continue;
                    // A third of them in RoofLife's SWITCHING groups: a group goes dark for a while now and then (WindowSwitch)
                    SurfaceKit.Look(RoofLife.WindowMat(seed, f, j, i), face + nrm * 0.04f + Vector3.up * (y0 + h * (j + 0.5f) / rows) + along * (width * ((i + 0.5f) / cols - 0.5f)), new Vector3(bw, Mathf.Min(1.6f, h / rows * 0.45f), 0.08f), faceRot);
                    SurfaceKit.Look(ivory, face + nrm * 0.06f + Vector3.up * (y0 + h * (j + 0.5f) / rows) + along * (width * ((i + 0.5f) / cols - 0.5f)), new Vector3(0.08f, Mathf.Min(1.6f, h / rows * 0.45f), 0.06f), faceRot);   // the glazing bar
                }
            if (top)
            {
                // The triple stepped cornice under the roof's edge, each step deeper, the gold lip on the top one
                SurfaceKit.Look(LevelKit.Limestone, face + nrm * 0.1f + Vector3.up * (y0 + h - 1.3f), new Vector3(width + 0.4f, 0.4f, 0.42f), faceRot);
                SurfaceKit.Look(LevelKit.Limestone, face + nrm * 0.2f + Vector3.up * (y0 + h - 0.85f), new Vector3(width + 0.8f, 0.45f, 0.62f), faceRot);
                SurfaceKit.Look(LevelKit.Limestone, face + nrm * 0.32f + Vector3.up * (y0 + h - 0.38f), new Vector3(width + 1.2f, 0.5f, 0.86f), faceRot);
                SurfaceKit.Look(gold, face + nrm * 0.74f + Vector3.up * (y0 + h + 0.02f), new Vector3(width + 1.2f, 0.06f, 0.1f), faceRot);   // its gold lip
                int dentils = Mathf.Max(3, Mathf.RoundToInt(width / 2f));
                for (int i = 0; i < dentils; i++)
                    SurfaceKit.Look(LevelKit.Limestone, face + nrm * 0.12f + Vector3.up * (y0 + h - 1.7f) + along * (width * ((i + 0.5f) / dentils - 0.5f)), new Vector3(0.5f, 0.4f, 0.3f), faceRot);
                // Flagpoles off the two front corners of the face that looks down the route, an oxblood pennant on each
                if (f == 0)
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Vector3 b = face + along * (s * (width * 0.5f - 1.5f)) + Vector3.up * (y0 + h - 2f);
                        Quaternion lean = faceRot * Quaternion.Euler(-55f, 0f, 0f);
                        SurfaceKit.Look(LevelKit.Brass, b + lean * Vector3.forward * 2.2f, new Vector3(0.1f, 0.1f, 4.4f), lean);
                        SurfaceKit.Look(LevelKit.Brass, b + lean * Vector3.forward * 4.5f, Vector3.one * 0.26f, lean, PrimitiveType.Sphere);
                        SurfaceKit.Look(LevelKit.Oxblood, b + lean * Vector3.forward * 3.6f + Vector3.down * 0.5f, new Vector3(0.04f, 0.8f, 1.5f), lean);
                    }
                // A fire escape zigzagging down the back face, a quarter of the way along it: iron, looks only
                if (f == 1 && h >= 12f) FireEscape(face + along * (width * -0.25f), nrm, along, y0 + 1f, y0 + h - 2.5f);
            }
            if (spine && f >= 2)
                SurfaceKit.Look(gold, face + nrm * 0.16f + Vector3.up * (y0 + h * 0.5f), new Vector3(0.36f, h - 1.2f, 0.1f), faceRot);
        }
        for (int c = 0; c < 4; c++)
        {
            Vector3 corner = rot * new Vector3((c % 2 == 0 ? -1f : 1f) * (w * 0.5f + 0.12f), 0f, (c < 2 ? -1f : 1f) * (d * 0.5f + 0.12f));
            SurfaceKit.Look(LevelKit.Fluted, foot + corner + Vector3.up * (y0 + h * 0.5f), new Vector3(0.7f, h, 0.7f), rot);
            SurfaceKit.Look(LevelKit.Limestone, foot + corner + Vector3.up * (y0 + 0.35f), new Vector3(0.9f, 0.7f, 0.9f), rot);
            // A stepped gargoyle on every corner under the tier line (a setback's edge, or the roof's), looking out along the diagonal
            Vector3 diag = (rot * new Vector3(c % 2 == 0 ? -1f : 1f, 0f, c < 2 ? -1f : 1f)).normalized;
            Gargoyle(foot + corner + Vector3.up * (y0 + h - 2.6f), diag, Quaternion.LookRotation(diag, Vector3.up), top ? 1.1f : 0.9f);
        }
        if (top && w >= 18f && d >= 18f) RoofProps(foot, w, d, y0 + h, rot, seed);
    }

    /// A fire escape down a face: flights of iron stair leaning left and right in turn, a landing with a rail at every
    /// storey, a hand off the wall. Looks only; nothing to stand on (the Escape() platforms are the real ones).
    static void FireEscape(Vector3 at, Vector3 nrm, Vector3 along, float yBottom, float yTop)
    {
        EnsureJazz();
        const float storey = 3.4f;
        int flights = Mathf.FloorToInt((yTop - yBottom) / storey);
        if (flights < 2) return;
        Quaternion faceRot = Quaternion.LookRotation(nrm, Vector3.up);
        for (int i = 0; i < flights; i++)
        {
            float y = yBottom + i * storey;
            float side = i % 2 == 0 ? 1f : -1f;
            Vector3 landing = at + nrm * 0.75f + Vector3.up * y;
            SurfaceKit.Look(LevelKit.RivetIron, landing, new Vector3(5.2f, 0.12f, 1.4f), faceRot);
            SurfaceKit.Look(LevelKit.Iron, landing + nrm * 0.68f + Vector3.up * 1f, new Vector3(5.2f, 0.05f, 0.05f), faceRot);   // the rail
            for (int p = -2; p <= 2; p++) SurfaceKit.Look(LevelKit.Iron, landing + nrm * 0.68f + along * (p * 1.25f) + Vector3.up * 0.5f, new Vector3(0.05f, 1f, 0.05f), faceRot);
            Vector3 a = landing + along * (side * 2.2f) + Vector3.up * 0.06f, b = landing - along * (side * 2.2f) + Vector3.up * (storey + 0.06f);
            SurfaceKit.Look(LevelKit.Iron, (a + b) * 0.5f, new Vector3(1.1f, 0.12f, Vector3.Distance(a, b)), SurfaceKit.SpanRot(a, b));   // the flight
            SurfaceKit.Look(LevelKit.Iron, (a + b) * 0.5f + nrm * 0.6f + Vector3.up * 0.9f, new Vector3(0.05f, 0.05f, Vector3.Distance(a, b)), SurfaceKit.SpanRot(a, b));   // its rail
            SurfaceKit.Look(LevelKit.Iron, landing - nrm * 0.6f + along * (side * 2.4f) + Vector3.up * -0.6f, new Vector3(0.12f, 0.12f, 1.4f), faceRot * Quaternion.Euler(45f, 0f, 0f));   // a bracket into the wall
        }
    }

    /// The roofline's things on brackets off the top tier's corners, OUTSIDE the roof you run on: a water tower, a
    /// chimney, now and then a radio mast. Picked by the seed so every tower differs. Looks only, never a collider.
    static void RoofProps(Vector3 foot, float w, float d, float roofY, Quaternion rot, int seed)
    {
        int pick = ((seed % 4) + 4) % 4;
        for (int c = 0; c < 4; c++)
        {
            Vector3 diag = (rot * new Vector3(c % 2 == 0 ? -1f : 1f, 0f, c < 2 ? -1f : 1f)).normalized;
            Vector3 corner = foot + rot * new Vector3((c % 2 == 0 ? -1f : 1f) * (w * 0.5f), 0f, (c < 2 ? -1f : 1f) * (d * 0.5f));
            Vector3 bracket = corner + diag * 3.4f + Vector3.up * (roofY - 0.8f);
            int what = (c + pick) % 4;
            if (what == 3) continue;   // one corner left bare
            // The bracket: two iron arms out from the corner under a plate
            SurfaceKit.Look(LevelKit.RivetIron, bracket, new Vector3(4f, 0.3f, 4f), rot);
            SurfaceKit.Look(LevelKit.Iron, corner + diag * 1.7f + Vector3.up * (roofY - 2.2f), new Vector3(0.3f, 0.3f, 4.2f), Quaternion.LookRotation(diag, Vector3.up) * Quaternion.Euler(-40f, 0f, 0f));
            SurfaceKit.Look(LevelKit.Iron, corner + diag * 1.7f + Vector3.up * (roofY - 0.95f), new Vector3(0.3f, 0.3f, 3.6f), Quaternion.LookRotation(diag, Vector3.up));
            Vector3 at = bracket + Vector3.up * 0.15f;
            if (what == 0) LevelKit.WaterTower(root, at, 8f, rot.eulerAngles.y + 45f);
            else if (what == 1) LevelKit.Chimney(root, at, 7f, 1.5f);
            else LevelKit.RadioMast(root, at, 11f);
        }
    }

    /// A searchlight tower on a bracket off one corner (0 south-west, 1 south-east, 2 north-west, 3 north-east) of a
    /// roof `roofY` high on a tier `w` by `d` round `foot`: outside the roof you run on, sweeping the sky. Looks only.
    public static void Searchlight(Vector3 foot, float w, float d, float roofY, float az, int corner, float phase = 0f)
    {
        Quaternion rot = Quaternion.Euler(0f, az, 0f);
        int c = Mathf.Clamp(corner, 0, 3);
        Vector3 diag = (rot * new Vector3(c % 2 == 0 ? -1f : 1f, 0f, c < 2 ? -1f : 1f)).normalized;
        Vector3 cornerAt = foot + rot * new Vector3((c % 2 == 0 ? -1f : 1f) * (w * 0.5f), 0f, (c < 2 ? -1f : 1f) * (d * 0.5f));
        Vector3 bracket = cornerAt + diag * 3.4f + Vector3.up * (roofY - 0.8f);
        SurfaceKit.Look(LevelKit.RivetIron, bracket, new Vector3(4f, 0.3f, 4f), rot);
        SurfaceKit.Look(LevelKit.Iron, cornerAt + diag * 1.7f + Vector3.up * (roofY - 2.2f), new Vector3(0.3f, 0.3f, 4.2f), Quaternion.LookRotation(diag, Vector3.up) * Quaternion.Euler(-40f, 0f, 0f));
        SurfaceKit.Look(LevelKit.Iron, cornerAt + diag * 1.7f + Vector3.up * (roofY - 0.95f), new Vector3(0.3f, 0.3f, 3.6f), Quaternion.LookRotation(diag, Vector3.up));
        LevelKit.SearchlightTower(root, bracket + Vector3.up * 0.15f, 7f, phase);
    }

    /// The plan's twin blades: two slabs with the slot between them. Solids in both, a slab on each top, and the three
    /// decks across the slot solid too. `outward` (+z turned by az) is the taller blade's side.
    public static void BladeTower(Vector3 foot, float w, float h, float az)
    {
        MenuCity.OuterTower(SurfaceKit.Far, sheets, foot, w, 9f, h, az, 0, true);
        Furnish();
        Quaternion rot = Quaternion.Euler(0f, az, 0f);
        Vector3 outward = rot * Vector3.forward;
        float low = h * 0.86f;
        SurfaceKit.Solid(foot + outward * 8f + Vector3.up * (h * 0.5f), new Vector3(w, h, 9f), rot);
        SurfaceKit.Solid(foot - outward * 8f + Vector3.up * (low * 0.5f), new Vector3(w, low, 9f), rot);
        Roof(foot + outward * 8f + Vector3.up * h, w - 0.2f, 8.8f, az);
        Roof(foot - outward * 8f + Vector3.up * low, w - 0.2f, 8.8f, az);
        for (int i = 1; i <= 3; i++)
            SurfaceKit.Solid(foot + Vector3.up * (h * 0.22f * i), new Vector3(w * 0.3f, 2.6f, 8f), rot);
        Jazz(foot + outward * 8f, w, 9f, 0f, h, az, JazzSeed(foot), true, 1);
        Jazz(foot - outward * 8f, w, 9f, 0f, low, az, JazzSeed(foot) + 2, true, 0);
    }

    /// The plan's round tower with its halo decks. Round solids (convex mesh colliders on the kit's cylinder) for the
    /// two tiers and the three decks: the decks are rings you can run round, the tiers are drums you can stand on.
    public static void RoundTower(Vector3 foot, float d, float h)
    {
        MenuCity.OuterTower(SurfaceKit.Far, sheets, foot, d, d, h, 0f, 2, true);
        Furnish();
        float h1 = h * 0.72f;
        Drum(foot + Vector3.up * (h1 * 0.5f), d, h1);
        Drum(foot + Vector3.up * (h1 + (h - h1) * 0.5f), d * 0.7f, h - h1);
        Drum(foot + Vector3.up * (h * 0.45f), d * 1.55f, 0.62f);
        Drum(foot + Vector3.up * h1, d * 1.3f, 0.62f);
        Drum(foot + Vector3.up * (h + 16f), d * 0.9f, 0.62f);
    }

    /// A cylinder collider: the kit's cylinder mesh, convex, scaled. (A box would leave corners in the air.)
    public static void Drum(Vector3 centre, float diameter, float height)
    {
        var go = new GameObject("Drum");
        go.transform.SetParent(root, false);
        go.transform.position = centre;
        go.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
        var mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = LevelKit.PrimMesh(PrimitiveType.Cylinder);
        mc.convex = true;
    }

    /// The plan's arcology, with every terrace solid and a slab on each: the tiers step back on both long sides
    /// (the ledges are along the building's own z), and the top is a plateau. Returns the top.
    public static float ArcologyTower(Vector3 foot, float w, float d, float h, float az)
    {
        MenuCity.OuterTower(SurfaceKit.Far, sheets, foot, w, d, h, az, 5, true);
        Furnish();
        Quaternion rot = Quaternion.Euler(0f, az, 0f);
        float[] part = { 0.3f, 0.2f, 0.16f, 0.13f, 0.11f, 0.1f };
        float y = 0f, cw = w, cd = d;
        for (int i = 0; i < part.Length; i++)
        {
            float th = h * part[i];
            SurfaceKit.Solid(foot + Vector3.up * (y + th * 0.5f), new Vector3(cw, th, cd), rot);
            Jazz(foot, cw, cd, y, th, az, JazzSeed(foot) + i, i == part.Length - 1, 3);
            y += th;
            Roof(foot + Vector3.up * y, cw - 0.2f, cd - 0.2f, az);   // the whole top; the next stack stands on the middle of it
            cw *= 0.9f;
            cd *= 0.78f;
        }
        return y;
    }

    /// The plan's spire: tiers stepping in by `shrink`, a slab on each, the needle over the top. Returns the top.
    public static float SpireTower(Vector3 foot, float w, float d, float h, float shrink = 0.8f, bool needle = true)
    {
        MenuCity.OuterTower(SurfaceKit.Far, sheets, foot, w, d, h, 0f, 1, true);
        Furnish();
        float[] part = { 0.38f, 0.24f, 0.17f, 0.12f, 0.09f };
        float y = 0f, cw = w, cd = d;
        for (int i = 0; i < part.Length; i++)
        {
            float th = h * part[i];
            SurfaceKit.Solid(foot + Vector3.up * (y + th * 0.5f), new Vector3(cw, th, cd));
            Jazz(foot, cw, cd, y, th, 0f, JazzSeed(foot) + i, i == part.Length - 1, 1);
            y += th;
            Roof(foot + Vector3.up * y, cw - 0.2f, cd - 0.2f, 0f);
            cw *= shrink;
            cd *= shrink;
        }
        if (needle) SurfaceKit.Solid(foot + Vector3.up * (h + 20f), new Vector3(1.1f, 40f, 1.1f));
        return y;
    }

    /// A SPLIT tower: a stack, a sky lobby open to the air through the middle of it, and a second stack standing on
    /// the lobby's ceiling. Returns the lobby's floor height. The lobby is as wide as the lower stack's upper storeys.
    public static float SplitTower(Vector3 foot, float w, float d, float hLower, float lobbyH, float hUpper, float az, bool lit = true)
    {
        StackTower(foot, w, d, hLower, az, lit);
        float w2 = w * 0.82f - 0.2f, d2 = d * 0.82f - 0.2f;
        Vector3 floor = foot + Vector3.up * hLower;
        Lobby(floor + Vector3.up * Proud, w2, d2, lobbyH, az);
        StackTower(floor + Vector3.up * lobbyH, w2, d2, hUpper, az, lit);
        return hLower;
    }

    /// A sky lobby on `floor` (its centre): the ceiling the tower above stands on, columns on a grid, the core with
    /// the lifts that do not come, planters and a tree or two, warm light, a balustrade with the ways in and out
    public static void Lobby(Vector3 floor, float w, float d, float h, float az)
    {
        Quaternion rot = Quaternion.Euler(0f, az, 0f);
        SurfaceKit.Block(LevelKit.Ceil, floor + Vector3.up * (h + 0.3f), new Vector3(w, 0.6f, d), rot);
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 3; j++)
            {
                float x = -w * 0.5f + w * (i + 0.5f) / 4f, z = -d * 0.5f + d * (j + 0.5f) / 3f;
                if (Mathf.Abs(x) < w * 0.16f && Mathf.Abs(z) < d * 0.2f) continue;   // the core stands there
                SurfaceKit.Block(LevelKit.Fluted, floor + rot * new Vector3(x, h * 0.5f, z), new Vector3(1.2f, h, 1.2f), rot);
                SurfaceKit.Look(LevelKit.Limestone, floor + rot * new Vector3(x, 0.3f, z), new Vector3(1.5f, 0.6f, 1.5f), rot);         // a stone foot
                SurfaceKit.Look(LevelKit.Limestone, floor + rot * new Vector3(x, h - 0.3f, z), new Vector3(1.6f, 0.4f, 1.6f), rot);     // a stepped cap
                SurfaceKit.Look(LevelKit.Brass, floor + rot * new Vector3(x, 3f, z), new Vector3(1.26f, 0.06f, 1.26f), rot);            // a brass collar
            }
        SurfaceKit.Block(LevelKit.Marble, floor + rot * new Vector3(0f, h * 0.5f, 0f), new Vector3(w * 0.26f, h, d * 0.3f), rot);
        SurfaceKit.Look(LevelKit.Brass, floor + rot * new Vector3(0f, 1.15f, 0f), new Vector3(w * 0.26f + 0.06f, 0.06f, d * 0.3f + 0.06f), rot);   // a brass dado round the core
        SurfaceKit.Look(LevelKit.Brass, floor + rot * new Vector3(0f, h - 0.5f, 0f), new Vector3(w * 0.26f + 0.06f, 0.06f, d * 0.3f + 0.06f), rot);
        LevelKit.HoloSign(root, floor + rot * new Vector3(0f, 2.6f, -d * 0.15f - 0.3f), rot * Vector3.back, 7f, 0.9f, "LIFTS  ·  OUT OF SERVICE");
        LevelKit.HoloSign(root, floor + rot * new Vector3(0f, 2.6f, d * 0.15f + 0.3f), rot * Vector3.forward, 7f, 0.9f, "SKY LOBBY  ·  RESIDENTS ONLY");
        // Planters along the long sides, a tree at each end of them, benches between
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 row = floor + rot * new Vector3(s * w * 0.36f, 0f, 0f);
            LevelKit.Hedge(root, row + rot * new Vector3(0f, 0f, -d * 0.28f), new Vector3(1.4f, 0.9f, d * 0.22f));
            LevelKit.Hedge(root, row + rot * new Vector3(0f, 0f, d * 0.28f), new Vector3(1.4f, 0.9f, d * 0.22f));
            LevelKit.Tree(root, row, Mathf.Min(h - 2f, 6f), s < 0);
            SurfaceKit.Block(LevelKit.Limestone, row + rot * new Vector3(-s * 2.4f, 0.25f, 0f), new Vector3(0.6f, 0.5f, 4f), rot);
        }
        for (int s = -1; s <= 1; s += 2)
            for (int t = -1; t <= 1; t += 2)
                LevelKit.Light(root, floor + rot * new Vector3(s * w * 0.25f, h - 1f, t * d * 0.25f), 26f, 2.2f);
        Balustrade(floor, w, d, az, 8f);
    }

    /// What the plan just put on a roof you run on, made solid: air handlers, a stair head, a tank, a mast
    public static void Furnish()
    {
        foreach (var p in MenuCity.LastFurniture) SurfaceKit.Solid(p.centre, p.size, p.rot);
    }

    /// A kit slab, flat, its top at `top`: the thing you actually run on, with the speed grid on it
    public static GameObject Roof(Vector3 top, float w, float d, float az)
    {
        var slab = LevelKit.FloorSlab(root, top + Vector3.up * (Proud - 0.25f), new Vector3(w, 0.5f, d), "Roof");
        slab.transform.rotation = Quaternion.Euler(0f, az, 0f);
        Roofs.Add(new RoofRec { top = top, w = w, d = d, az = az, neighbour = taggingNeighbour });
        return slab;
    }

    /// A low wall round a roof's edge, so the edge reads: four blocks, a lit lip on each
    public static void Parapet(Vector3 top, float w, float d, float az, float gapNorth = 0f)
    {
        Quaternion rot = Quaternion.Euler(0f, az, 0f);
        Side(top, rot, new Vector3(-w * 0.5f + 0.25f, 0f, 0f), new Vector3(0.5f, 1f, d));
        Side(top, rot, new Vector3(w * 0.5f - 0.25f, 0f, 0f), new Vector3(0.5f, 1f, d));
        Side(top, rot, new Vector3(0f, 0f, -d * 0.5f + 0.25f), new Vector3(w, 1f, 0.5f));
        if (gapNorth <= 0f) { Side(top, rot, new Vector3(0f, 0f, d * 0.5f - 0.25f), new Vector3(w, 1f, 0.5f)); return; }
        float side = (w - gapNorth) * 0.5f;
        Side(top, rot, new Vector3(-w * 0.5f + side * 0.5f, 0f, d * 0.5f - 0.25f), new Vector3(side, 1f, 0.5f));
        Side(top, rot, new Vector3(w * 0.5f - side * 0.5f, 0f, d * 0.5f - 0.25f), new Vector3(side, 1f, 0.5f));
    }

    /// The same, with the ways in and out: a gap in the middle of the south side and of the north
    public static void Balustrade(Vector3 top, float w, float d, float az, float gap)
    {
        Quaternion rot = Quaternion.Euler(0f, az, 0f);
        Side(top, rot, new Vector3(-w * 0.5f + 0.25f, 0f, 0f), new Vector3(0.5f, 1f, d));
        Side(top, rot, new Vector3(w * 0.5f - 0.25f, 0f, 0f), new Vector3(0.5f, 1f, d));
        float side = (w - gap) * 0.5f;
        for (int s = -1; s <= 1; s += 2)
        {
            Side(top, rot, new Vector3(-w * 0.5f + side * 0.5f, 0f, s * (d * 0.5f - 0.25f)), new Vector3(side, 1f, 0.5f));
            Side(top, rot, new Vector3(w * 0.5f - side * 0.5f, 0f, s * (d * 0.5f - 0.25f)), new Vector3(side, 1f, 0.5f));
        }
    }

    static void Side(Vector3 top, Quaternion rot, Vector3 local, Vector3 size)
    {
        size.y = 0.45f;   // knee high: a kerb, under any landing that crosses it, in nobody's way
        SurfaceKit.Block(LevelKit.Graphite, top + rot * (local + Vector3.up * 0.225f), size, rot);
        SurfaceKit.Look(LevelKit.Led, top + rot * (local + Vector3.up * 0.49f), new Vector3(size.x + 0.1f, 0.06f, size.z + 0.1f), rot);
    }

    /// A roof garden on a roof you run on: a lawn, a pool in the middle of it, trees, hedges for cover
    public static void Garden(Vector3 top, float w, float d, float az)
    {
        top += Vector3.up * Proud;
        Quaternion rot = Quaternion.Euler(0f, az, 0f);
        KeepOut.Add(new KeepRec { centre = top, hx = w * 0.5f - 1f, hz = d * 0.5f - 1f, az = az });   // RoofLife puts nothing in a garden
        LevelKit.Lawn(root, top, new Vector2(w - 4f, d - 4f));
        LevelKit.Pool(root, top + Vector3.up * 0.06f, new Vector2(Mathf.Min(w * 0.3f, 14f), Mathf.Min(d * 0.25f, 10f)));
        for (int s = -1; s <= 1; s += 2)
            for (int t = -1; t <= 1; t += 2)
            {
                LevelKit.Tree(root, top + rot * new Vector3(s * w * 0.34f, 0.06f, t * d * 0.32f), 7f, (s + t) == 0);
                LevelKit.Hedge(root, top + rot * new Vector3(s * w * 0.2f, 0.06f, t * d * 0.36f), new Vector3(w * 0.16f, 1f, 1.2f));
            }
    }

    // ------------------------------------------------------------ the ways between

    /// A girder bridge from `a` to `b` (its top surface): the deck you run on, a parapet either side, lit. Seven metres
    /// wide since course-grammar (2026-10-11): the kerbs take 0.4 m a side, and no lane on a route runs under six
    public static void Bridge(Vector3 a, Vector3 b, float w = 7f) => Span(a + Vector3.up * Proud, b + Vector3.up * Proud, w);

    static Material deckMat, paintMat;
    const float DeckT = 0.6f, LandingLen = 6f, RampLen = 3f;

    /// The deck's own steel, and the paint on it
    static void EnsureDeck()
    {
        if (deckMat != null) return;
        deckMat = LevelKit.Surface(new Color(0.4f, 0.4f, 0.42f), 0.5f, LevelKit.Surf.Plate, 2f, 1.1f);
        paintMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        paintMat.SetColor("_BaseColor", new Color(0.88f, 0.86f, 0.8f));
        paintMat.SetFloat("_Smoothness", 0.3f);
    }

    /// A bridge from `a` to `b`: a deck that BEARS ON the roofs it joins, not one pushed into them. Each end is
    /// trimmed to its roof's edge; a level landing lies six metres onto the roof there, on top of it, with a ramp plate
    /// up onto it and its own railing; the span leaves from the edge on top of the landing, a bearing block under it
    /// against the tower's face and a wing wall either side; the deck carries lane lines, joints, a kerb with a lit
    /// lip, a railing you cannot fall through, a fascia, a gantry with a lamp every twenty-four metres, and the truss
    static void Span(Vector3 a, Vector3 b, float w, string name = "Bridge")
    {
        EnsureDeck();
        Ends(ref a, ref b, w, DeckT, deckMat, out bool onA, out bool onB, out bool upA, out bool upB);
        if (Vector3.Distance(a, b) < 3f) return;
        SurfaceKit.SpanSlab(root, a, b, w, DeckT, deckMat, name);
        Rails(a, b, w, DeckT, true);
        Truss(a, b, w, DeckT);
        Abutment(a, (b - a).normalized, w, DeckT, onA, upA);
        Abutment(b, (a - b).normalized, w, DeckT, onB, upB);
    }

    /// Both ends of a deck: trimmed to the roofs' edges, and where an end is on a roof, a landing built there. The
    /// endpoints come back as the span's real ends (on a roof: at the edge, a deck's thickness up)
    static void Ends(ref Vector3 a, ref Vector3 b, float w, float thick, Material mat, out bool onA, out bool onB, out bool upA, out bool upB)
    {
        Physics.SyncTransforms();
        Vector3 line = (b - a).normalized;
        onA = Trim(ref a, line);
        onB = Trim(ref b, -line);
        Vector3 a0 = a, b0 = b;
        upA = onA && LandingAt(a0, (b0 - a0).normalized, w, thick, mat, out a);
        upB = onB && LandingAt(b0, (a0 - b0).normalized, w, thick, mat, out b);
    }

    /// Is there a roof under `p`, within a couple of metres of the deck line?
    static bool Under(Vector3 p) => Physics.Raycast(p + Vector3.up * 2f, Vector3.down, 4.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

    /// An end is walked toward the other end until the roof under it runs out, so the deck leaves from the roof's edge
    /// and not from somewhere on top of it; an end authored a hair short of its roof is walked back onto it first.
    /// Returns whether the end is on a roof at all.
    static bool Trim(ref Vector3 end, Vector3 toward)
    {
        Vector3 flat = new Vector3(toward.x, 0f, toward.z).normalized;
        if (!Under(end))
        {
            bool found = false;
            for (float t = 0.25f; t <= 2.5f && !found; t += 0.25f)
                if (Under(end - flat * t)) { end -= flat * t; found = true; }
            if (!found) return false;
        }
        Vector3 last = end;
        for (float t = 0.5f; t <= 40f; t += 0.5f)
        {
            Vector3 q = end + flat * t;
            if (!Under(q)) break;
            last = q;
        }
        end = new Vector3(last.x, end.y, last.z);
        return true;
    }

    /// A landing on a roof at `edge`: a level slab lying `LandingLen` metres back onto the roof from the edge, on top
    /// of it, a ramp plate up onto its inner end with a line of light at its foot, a railing over it. `dir` points out
    /// along the span. Returns the span's end: the edge, a deck's thickness up
    static bool LandingAt(Vector3 edge, Vector3 dir, float w, float thick, Material mat, out Vector3 spanEnd)
    {
        spanEnd = edge;
        Vector3 flat = new Vector3(dir.x, 0f, dir.z).normalized;
        // How deep the roof runs behind the edge along the line: a blade, a ledge or a drum's rim is met flush
        float depth = 0f;
        for (float t = 0.5f; t <= 12f; t += 0.5f) { if (!Under(edge - flat * t)) break; depth = t; }
        if (depth < 9f) return false;
        float landing = Mathf.Clamp(depth * 0.35f, 2.5f, LandingLen), ramp = Mathf.Min(RampLen, depth - landing - 1f);
        // Nothing already there: a pad the landing would bury, another bridge's landing it would stack on
        Quaternion rot = Quaternion.LookRotation(flat, Vector3.up);
        Vector3 probe = edge - flat * ((landing + ramp) * 0.5f) + Vector3.up * 0.4f;
        foreach (var c in Physics.OverlapBox(probe, new Vector3(w * 0.5f + 0.5f, 0.6f, (landing + ramp) * 0.5f), rot, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            string nm = c.gameObject.name;
            if (nm == "JumpPad" || nm == "Landing" || nm == "LandingRamp") return false;
        }
        Vector3 up = Vector3.up * thick;
        Vector3 inner = edge - flat * landing;
        SurfaceKit.SpanSlab(root, inner + up, edge + up, w, thick, mat, "Landing");
        SurfaceKit.SpanSlab(root, inner - flat * ramp + Vector3.up * 0.02f, inner + up, w - 0.5f, 0.25f, mat, "LandingRamp");
        Vector3 side = Vector3.Cross(Vector3.up, flat);
        for (int s = -1; s <= 1; s += 2)   // the cheeks of the ramp
            SurfaceKit.Look(LevelKit.Graphite, inner - flat * (ramp * 0.5f) + side * (s * (w * 0.5f - 0.15f)) + Vector3.up * (thick * 0.5f), new Vector3(0.2f, thick, ramp), rot);
        SurfaceKit.Look(LevelKit.Led, inner - flat * (ramp + 0.1f) + Vector3.up * 0.036f, new Vector3(w - 0.8f, 0.02f, 0.12f), rot);
        Rails(inner + up, edge + up, w, thick, false);
        spanEnd = edge + up;
        return true;
    }

    /// What a deck carries: a kerb with a lit lip and a railing (posts, two rails, a solid top rail) either side, the
    /// fascia on the deck's edge, and on a span its lane lines, its joints and a gantry every twenty-four metres
    static void Rails(Vector3 a, Vector3 b, float w, float thick, bool span)
    {
        Quaternion rot = SurfaceKit.SpanRot(a, b);
        float len = Vector3.Distance(a, b);
        int posts = Mathf.Max(1, Mathf.RoundToInt(len / 3f)), bays = Mathf.Max(1, Mathf.FloorToInt(len / 12f));
        for (int s = -1; s <= 1; s += 2)
        {
            float e = s * (w * 0.5f - 0.2f);
            SurfaceKit.Block(LevelKit.Graphite, SurfaceKit.OnSpan(a, b, 0.5f, e, 0.125f), new Vector3(0.36f, 0.25f, len), rot);
            SurfaceKit.Look(LevelKit.Led, SurfaceKit.OnSpan(a, b, 0.5f, e, 0.28f), new Vector3(0.4f, 0.05f, len), rot);
            SurfaceKit.Block(LevelKit.Graphite, SurfaceKit.OnSpan(a, b, 0.5f, e, 1.1f), new Vector3(0.1f, 0.1f, len), rot);
            SurfaceKit.Look(LevelKit.Graphite, SurfaceKit.OnSpan(a, b, 0.5f, e, 0.5f), new Vector3(0.05f, 0.05f, len), rot);
            SurfaceKit.Look(LevelKit.Graphite, SurfaceKit.OnSpan(a, b, 0.5f, e, 0.8f), new Vector3(0.05f, 0.05f, len), rot);
            for (int i = 0; i <= posts; i++)
                SurfaceKit.Look(LevelKit.Graphite, SurfaceKit.OnSpan(a, b, i / (float)posts, e, 0.68f), new Vector3(0.12f, 0.86f, 0.12f), rot);
            SurfaceKit.Look(LevelKit.Graphite, SurfaceKit.OnSpan(a, b, 0.5f, s * (w * 0.5f + 0.03f), -thick * 0.5f), new Vector3(0.06f, thick, len), rot);   // the fascia: against the deck's edge, never over its top (two faces on one plane fight)
            if (span) SurfaceKit.Look(paintMat, SurfaceKit.OnSpan(a, b, 0.5f, s * w * 0.22f, 0.012f), new Vector3(0.12f, 0.012f, len - 1f), rot);
        }
        if (!span) return;
        for (int i = 1; i < bays; i++)
            SurfaceKit.Look(LevelKit.Dark, SurfaceKit.OnSpan(a, b, i / (float)bays, 0f, 0.01f), new Vector3(w - 0.9f, 0.012f, 0.16f), rot);
        int gantries = Mathf.FloorToInt(len / 24f);
        for (int i = 1; i <= gantries; i++)
        {
            float t = (i - 0.5f) / gantries;
            for (int s = -1; s <= 1; s += 2)
                SurfaceKit.Look(LevelKit.Girder, SurfaceKit.OnSpan(a, b, t, s * (w * 0.5f - 0.25f), 1.9f), new Vector3(0.3f, 3.8f, 0.3f), rot);
            SurfaceKit.Look(LevelKit.Girder, SurfaceKit.OnSpan(a, b, t, 0f, 3.85f), new Vector3(w + 0.1f, 0.3f, 0.3f), rot);
            SurfaceKit.Look(LevelKit.Led, SurfaceKit.OnSpan(a, b, t, 0f, 3.66f), new Vector3(1.4f, 0.06f, 0.26f), rot);
        }
    }

    /// The truss under a span: a girder across every twelve metres, the spine along it
    static void Truss(Vector3 a, Vector3 b, float w, float thick)
    {
        Quaternion rot = SurfaceKit.SpanRot(a, b);
        int bays = Mathf.Max(1, Mathf.FloorToInt(Vector3.Distance(a, b) / 12f));
        for (int i = 0; i <= bays; i++)
            SurfaceKit.Look(LevelKit.Girder, SurfaceKit.OnSpan(a, b, i / (float)bays, 0f, -thick - 1.2f), new Vector3(w * 0.9f, 0.4f, 0.4f), rot);
        SurfaceKit.SpanBlock(LevelKit.Girder, a + Vector3.down * (thick + 2.6f), b + Vector3.down * (thick + 2.6f), 0.5f, 0.5f);
    }

    /// Where a span leaves a roof: an end post with a light on each railing (every end), and on a roof a bearing block
    /// under the deck against the tower's face and a wing wall either side turning along the edge. `dir` runs from
    /// this end toward the other; `end` is the span's end, a deck's thickness above the roof
    static void Abutment(Vector3 end, Vector3 dir, float w, float thick, bool onRoof, bool raised)
    {
        float drop = raised ? thick : 0f;   // how far the roof is under this end
        Vector3 flat = new Vector3(dir.x, 0f, dir.z).normalized;
        Vector3 side = Vector3.Cross(Vector3.up, flat);
        Quaternion rot = Quaternion.LookRotation(flat, Vector3.up);
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 e = end + side * (s * (w * 0.5f - 0.2f));
            SurfaceKit.Block(LevelKit.Graphite, e + Vector3.up * 0.65f, new Vector3(0.3f, 1.3f, 0.3f), rot);
            SurfaceKit.Look(LevelKit.Led, e + Vector3.up * 1.33f, new Vector3(0.36f, 0.05f, 0.36f), rot);
            if (onRoof) SurfaceKit.Block(LevelKit.Graphite, e + side * (s * 1.35f) - flat * 0.6f + Vector3.up * (0.6f - drop), new Vector3(2.4f, 1.2f, 0.36f), rot);
        }
        if (!onRoof) return;
        SurfaceKit.Look(LevelKit.Graphite, end + flat * 0.6f - Vector3.up * (thick + 0.55f), new Vector3(w + 0.6f, 1.1f, 1.2f), rot);   // the bearing, under the deck against the face
    }

    /// The same bridge with `gap` metres of it gone in the middle: the response's work. The two halves keep their line.
    /// COURSE-GRAMMAR (2026-10-11): the gap is a FAST LINE, never a wall. A brass LIP stands across the near torn end
    /// (a corner boost off it carries a run over eleven to fifteen metres, a hopped run over more; the drones over the
    /// hole stay the other line), and under the whole gap hangs the maintenance CATCH: a gantry eight metres down with
    /// a pad at its far end that throws a miss back up onto the far half. A missed line lands on the gantry, never in
    /// the street. The web round the gap is the main line: nothing here has to be jumped.
    public static void BrokenBridge(Vector3 a, Vector3 b, float gap, float w = 7f)
    {
        a += Vector3.up * Proud; b += Vector3.up * Proud;
        float len = Vector3.Distance(a, b);
        float k0 = Mathf.Clamp01(0.5f - gap / len * 0.5f), k1 = Mathf.Clamp01(0.5f + gap / len * 0.5f);
        Vector3 e0 = Vector3.Lerp(a, b, k0), e1 = Vector3.Lerp(a, b, k1);
        Span(a, e0, w, "BridgeTorn");
        Span(e1, b, w, "BridgeTorn");
        // The torn ends: a girder hanging off each, a warning stripe across each
        Quaternion rot = SurfaceKit.SpanRot(a, b);
        SurfaceKit.Look(LevelKit.CordonBand, e0 + Vector3.up * 0.3f, new Vector3(w, 0.2f, 0.3f), rot);
        SurfaceKit.Look(LevelKit.CordonBand, e1 + Vector3.up * 0.3f, new Vector3(w, 0.2f, 0.3f), rot);
        Vector3 dir = (b - a).normalized;
        SurfaceKit.SpanBlock(LevelKit.Girder, e0 + Vector3.down * 0.4f, e0 + dir * 5f + Vector3.down * 7f, 0.5f, 0.5f);
        SurfaceKit.SpanBlock(LevelKit.Girder, e1 + Vector3.down * 0.4f, e1 - dir * 4f + Vector3.down * 6f, 0.5f, 0.5f);
        TornLip(e0, dir, w);
        Catch(e0, e1, dir, w);
    }

    /// A brass lip across the near torn end, 1.14 m over the deck, its back face 0.3 m short of the tear: the corner
    /// boost over the gap (LipMarker, brass strips: the bible's mark for it). Solid, welded, clear of the end posts.
    static void TornLip(Vector3 end, Vector3 dir, float w)
    {
        Vector3 flat = new Vector3(dir.x, 0f, dir.z).normalized;
        Quaternion rot = Quaternion.LookRotation(flat, Vector3.up);
        const float tall = 1.2f, deep = 0.8f;
        float wide = w - 1.4f;
        Vector3 centre = end - flat * (deep * 0.5f + 0.3f) + Vector3.up * (tall * 0.5f - 0.06f);   // sunk a hair: the deck under it slopes
        SurfaceKit.Block(LevelKit.Limestone, centre, new Vector3(wide, tall, deep), rot);
        float top = centre.y + tall * 0.5f;
        for (int s = -1; s <= 1; s += 2)
            SurfaceKit.Look(LevelKit.Brass, new Vector3(centre.x, top + 0.015f, centre.z) + rot * new Vector3(0f, 0f, s * (deep * 0.5f - 0.09f)), new Vector3(wide - 0.08f, 0.024f, 0.14f), rot);
        TechMarkers.MarkLip(new Vector3(centre.x, top, centre.z), new Vector3(wide + 0.6f, 0.9f, deep + 0.6f));
    }

    /// The maintenance gantry under a gap: a slab eight metres under the deck line from four metres before the near
    /// torn end to two past the far one, a kerb with a lit lip either side, hangers up to the halves' trusses at both
    /// ends, and a pad six metres short of the far tear that throws whoever lands here up and on, over the torn end
    /// onto the far half (23 up clears the deck and its rail with two metres to spare; the deck plane is crossed
    /// three metres before the tear, so nothing is hit on the way)
    static void Catch(Vector3 e0, Vector3 e1, Vector3 dir, float w)
    {
        const float drop = 8f;
        Vector3 flat = new Vector3(dir.x, 0f, dir.z).normalized;
        Vector3 a = e0 - flat * 4f + Vector3.down * drop, b = e1 + flat * 2f + Vector3.down * drop;
        float wide = w - 1f, len = Vector3.Distance(a, b);
        SurfaceKit.SpanSlab(root, a, b, wide, 0.4f, LevelKit.Floor, "CatchGantry");
        Quaternion rot = SurfaceKit.SpanRot(a, b);
        for (int s = -1; s <= 1; s += 2)
        {
            SurfaceKit.Block(LevelKit.Graphite, SurfaceKit.OnSpan(a, b, 0.5f, s * (wide * 0.5f - 0.15f), 0.2f), new Vector3(0.3f, 0.4f, len), rot);   // a kerb: the edge reads under your feet
            SurfaceKit.Look(LevelKit.Led, SurfaceKit.OnSpan(a, b, 0.5f, s * (wide * 0.5f - 0.15f), 0.43f), new Vector3(0.36f, 0.06f, len), rot);
        }
        // Hung from the halves over its ends (over the gap itself there is nothing to hang from: it cantilevers)
        foreach (float along in new[] { 1f, 3f, len - 1f })
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 foot = SurfaceKit.OnSpan(a, b, along / len, s * (wide * 0.5f - 0.6f), 0f);
                SurfaceKit.Look(LevelKit.Girder, foot + Vector3.up * 2.2f, new Vector3(0.3f, 4.4f, 0.3f));
            }
        LevelKit.JumpPad(root, e1 - flat * 6f + Vector3.down * drop, Quaternion.LookRotation(flat, Vector3.up).eulerAngles.y, 23f, 7f);
    }

    /// A tram line in the sky from `a` to `b`: the deck, its parapets, piers to the ground every fifty metres, the rails
    /// Where the last viaduct's deck actually runs (its ends bear on the roofs): the rails and the trams go on this line
    public static Vector3 LastDeckA, LastDeckB;

    public static void Viaduct(Vector3 a, Vector3 b, float w)
    {
        EnsureDeck();
        a += Vector3.up * Proud; b += Vector3.up * Proud;
        Ends(ref a, ref b, w, 0.8f, deckMat, out bool onA, out bool onB, out bool upA, out bool upB);
        LastDeckA = a; LastDeckB = b;
        Abutment(a, (b - a).normalized, w, 0.8f, onA, upA);
        Abutment(b, (a - b).normalized, w, 0.8f, onB, upB);
        SurfaceKit.SpanSlab(root, a, b, w, 0.8f, deckMat, "Deck");
        Quaternion rot = SurfaceKit.SpanRot(a, b);
        float len = Vector3.Distance(a, b);
        int posts = Mathf.Max(1, Mathf.RoundToInt(len / 4f)), joints = Mathf.Max(1, Mathf.FloorToInt(len / 25f));
        for (int s = -1; s <= 1; s += 2)
        {
            float e = s * (w * 0.5f - 0.25f);
            SurfaceKit.Block(CityKit.Mass, SurfaceKit.OnSpan(a, b, 0.5f, e, 0.2f), new Vector3(0.5f, 0.4f, len), rot);                     // the kerb
            SurfaceKit.Look(LevelKit.Led, SurfaceKit.OnSpan(a, b, 0.5f, e, 0.43f), new Vector3(0.56f, 0.06f, len), rot);
            SurfaceKit.Block(LevelKit.Graphite, SurfaceKit.OnSpan(a, b, 0.5f, e, 1.15f), new Vector3(0.1f, 0.1f, len), rot);               // the top rail
            SurfaceKit.Look(LevelKit.Graphite, SurfaceKit.OnSpan(a, b, 0.5f, e, 0.78f), new Vector3(0.05f, 0.05f, len), rot);
            for (int i = 0; i <= posts; i++)
                SurfaceKit.Look(LevelKit.Graphite, SurfaceKit.OnSpan(a, b, i / (float)posts, e, 0.8f), new Vector3(0.12f, 0.8f, 0.12f), rot);
            SurfaceKit.Look(LevelKit.Graphite, SurfaceKit.OnSpan(a, b, 0.5f, s * (w * 0.5f + 0.03f), -0.4f), new Vector3(0.06f, 0.8f, len), rot);   // the fascia, against the deck's edge and never over its top
        }
        for (int i = 1; i < joints; i++)
            SurfaceKit.Look(LevelKit.Dark, SurfaceKit.OnSpan(a, b, i / (float)joints, 0f, 0.01f), new Vector3(w - 1.2f, 0.012f, 0.18f), rot);
        // The catenary: a mast either side every twenty-five metres, the arm across, the wire down the middle
        for (int i = 0; i <= joints; i++)
        {
            float t = i / (float)joints;
            for (int s = -1; s <= 1; s += 2)
                SurfaceKit.Look(LevelKit.Girder, SurfaceKit.OnSpan(a, b, t, s * (w * 0.5f - 0.3f), 2.7f), new Vector3(0.25f, 5.4f, 0.25f), rot);
            SurfaceKit.Look(LevelKit.Girder, SurfaceKit.OnSpan(a, b, t, 0f, 5.3f), new Vector3(w - 0.3f, 0.15f, 0.15f), rot);
        }
        SurfaceKit.Look(LevelKit.Dark, SurfaceKit.OnSpan(a, b, 0.5f, 0f, 4.9f), new Vector3(0.04f, 0.04f, len), rot);
        int piers = Mathf.Max(1, Mathf.FloorToInt(len / 50f));
        for (int i = 0; i <= piers; i++)
        {
            Vector3 p = SurfaceKit.OnSpan(a, b, i / (float)piers, 0f, -0.8f);
            SurfaceKit.Look(CityKit.Mass, new Vector3(p.x, (p.y + groundY) * 0.5f, p.z), new Vector3(4f, p.y - groundY, 4f));
        }
    }

    /// A covered court between two towers: a wide deck with a canopy on columns over it, open at both ends and both
    /// sides — cover from the sky, a fight underneath it
    public static void Court(Vector3 a, Vector3 b, float w, float canopyH = 7f)
    {
        EnsureDeck();
        a += Vector3.up * Proud; b += Vector3.up * Proud;
        Ends(ref a, ref b, w, DeckT, LevelKit.Floor, out _, out _, out _, out _);
        SurfaceKit.SpanSlab(root, a, b, w, DeckT, LevelKit.Floor, "Court");
        Quaternion rot = SurfaceKit.SpanRot(a, b);
        float len = Vector3.Distance(a, b);
        SurfaceKit.Look(LevelKit.Ceil, SurfaceKit.OnSpan(a, b, 0.5f, 0f, canopyH), new Vector3(w + 2f, 0.5f, len), rot);
        int bays = Mathf.Max(1, Mathf.FloorToInt(len / 12f));
        for (int i = 0; i <= bays; i++)
            for (int s = -1; s <= 1; s += 2)
                SurfaceKit.Block(LevelKit.Wall, SurfaceKit.OnSpan(a, b, i / (float)bays, s * (w * 0.5f - 0.6f), canopyH * 0.5f), new Vector3(0.8f, canopyH, 0.8f), rot);
        for (int i = 0; i < bays; i++)
            LevelKit.Light(root, SurfaceKit.OnSpan(a, b, (i + 0.5f) / bays, 0f, canopyH - 0.8f), 22f, 2f);
        SurfaceKit.SpanBlock(LevelKit.Girder, a + Vector3.down * 3f, b + Vector3.down * 3f, 0.6f, 0.6f);
    }

    /// A fire escape up a face: platforms every few metres, each a hop from the last, stepping side to side, iron
    /// brackets under them and an iron rail round the far edge of each (the rail is a look)
    public static void Escape(Vector3 foot, float y0, float y1, Vector3 along, float step = 3.2f)
    {
        int n = Mathf.Max(1, Mathf.FloorToInt((y1 - y0) / step));
        Vector3 dir = along.normalized, side = Vector3.Cross(Vector3.up, dir).normalized;
        Physics.SyncTransforms();
        for (int i = 0; i <= n; i++)
        {
            Vector3 p = foot + Vector3.up * (y0 + i * step) + dir * (i % 2 == 0 ? -3f : 3f);
            LevelKit.FloorSlab(root, p - Vector3.up * 0.15f, new Vector3(4f, 0.3f, 3f), "Escape");
            SurfaceKit.Look(LevelKit.Girder, p - Vector3.up * 0.6f, new Vector3(3.6f, 0.3f, 0.3f));
            // Which side the face is on: the rail goes on the other, the bracket leans back to the wall
            Vector3 wall = Physics.Raycast(p + Vector3.up * 0.5f, side, 3.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? side
                : Physics.Raycast(p + Vector3.up * 0.5f, -side, 3.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? -side : Vector3.zero;
            if (wall == Vector3.zero) continue;
            Vector3 a = p + wall * 2f - Vector3.up * 1.9f, b = p - wall * 1.5f - Vector3.up * 0.3f;
            SurfaceKit.Look(LevelKit.Iron, (a + b) * 0.5f, new Vector3(0.2f, 0.2f, Vector3.Distance(a, b)), SurfaceKit.SpanRot(a, b));
            for (int k = -1; k <= 1; k++) SurfaceKit.Look(LevelKit.Iron, p + dir * (k * 1.4f) - wall * 1.9f + Vector3.up * 0.5f, new Vector3(0.05f, 1f, 0.05f));
            SurfaceKit.Look(LevelKit.Iron, p + Vector3.up * 1f - wall * 1.9f, new Vector3(0.05f, 0.05f, 3f), Quaternion.LookRotation(dir, Vector3.up));
        }
    }

    // ------------------------------------------------------------ the lines on the roofs (course-grammar, 2026-10-11)

    /// HARD LINE's lit hairline along a fast line on a roof, `a` to `b` at the roof's DRAWN height (the kit lifts it onto the slab)
    public static void FastLine(Vector3 a, Vector3 b)
    {
        Lines.Add(new LineRec { a = a, b = b });
        LevelKit.LineRun(root, a + Vector3.up * Proud, b + Vector3.up * Proud, Zone.Surface);
    }

    /// A pair of KICK STACKS flanking a line on a roof: chimneys `dx` either side of `x` at `z`, `h` tall, their feet on
    /// the roof's drawn top `y` (a hair into the slab, so no face shares its plane)
    public static void KickPair(float x, float y, float z, float dx = 5.6f, float h = 5f)
    {
        LevelKit.KickStack(root, new Vector3(x - dx, y + Proud - 0.02f, z), h);
        LevelKit.KickStack(root, new Vector3(x + dx, y + Proud - 0.02f, z), h);
    }

    /// A brass LIP on a roof (LipMarker, brass strips): `foot` the centre of its footprint on the roof's drawn top, `size` the block
    public static void Lip(Vector3 foot, Vector3 size) => LevelKit.Lip(root, foot + Vector3.up * (Proud - 0.02f + size.y * 0.5f), size, LevelKit.Limestone);

    /// A KICK WALL on a roof (cream flutes): `foot` the centre of its footprint on the roof's drawn top, `size` the slab (3–6 m tall)
    public static void KickWall(Vector3 foot, Vector3 size) => LevelKit.KickWall(root, foot + Vector3.up * (Proud - 0.02f + size.y * 0.5f), size, LevelKit.Graphite);

    /// A door's two posts as EDGE-BOOST corners (cream flutes, CornerMarkers), `half` either side of `at` on the roof's
    /// drawn top: six metres of way between them, the width of the lane
    public static void Door(Vector3 at, float half = 3.6f)
    {
        for (int s = -1; s <= 1; s += 2)
            LevelKit.BoostCorner(root, at + new Vector3(s * half, Proud - 0.02f + 2.2f, 0f), new Vector3(1.2f, 4.4f, 1.2f), LevelKit.Graphite);
    }

    // ------------------------------------------------------------ the district's own things

    /// A girder mast with a sweep light on an arm at its head: stand in the pool and the mortars come (DistrictDefense)
    public static void Mast(Vector3 foot, float height, Vector3 arm, Vector3 centre, float reachX, float reachZ, float rate, float phase)
    {
        SurfaceKit.Block(LevelKit.Girder, foot + Vector3.up * (height * 0.5f), new Vector3(1.2f, height, 1.2f));
        SurfaceKit.Block(LevelKit.Graphite, foot + Vector3.up * 0.4f, new Vector3(3f, 0.8f, 3f));
        Vector3 head = foot + Vector3.up * (height - 0.3f);
        SurfaceKit.Block(LevelKit.Girder, head + arm * 0.5f, new Vector3(Mathf.Abs(arm.x) + 0.5f, 0.5f, Mathf.Abs(arm.z) + 0.5f));
        SweepLight.Build(root, cfg, head + arm + Vector3.down * 0.9f, centre, reachX, reachZ, rate, phase);
    }

    /// A checkpoint: the response's beacon on the roof, the one you passed last brings you back
    public static void Beacon(Vector3 at) => district.beacons.Add(LevelKit.ResponseBeacon(root, at + Vector3.up * Proud));

    /// Hover mines at (x, z) pairs, each hung over the first thing under it from `fromY` down. The sky levels call this
    /// thirty metres over the roof they mean (RoofA + 30f …): the ray reaches 36 m, enough for that roof and never for
    /// the one below it (a twin blade's low slab is 48 m under the high one's call). Until 2026-10-12 it reached 12 m
    /// and no Act III mine was ever laid.
    public static void Field(float fromY, params float[] xz)
    {
        for (int i = 0; i + 1 < xz.Length; i += 2)
        {
            var top = new Vector3(xz[i], fromY, xz[i + 1]);
            if (!Physics.Raycast(top, Vector3.down, out RaycastHit hit, 36f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            HoverMine.Lay(root, cfg, hit.point);
        }
    }

    /// A thermal up a flank: the look (vent, hoops, embers) and the lift (the district's updraft, always on)
    public static void Thermal(Vector3 centre, float half, float y0, float y1)
    {
        LevelKit.Thermal(root, centre, half, y0, y1);
        district.updrafts.Add(new District.Updraft { centre = centre, half = half, y0 = y0, y1 = y1, needsPower = false });
    }

    // ------------------------------------------------------------ the neighbours

    /// One block of the city beside a route: where it stands, its size, and whether its roof is a real one
    public struct Block { public Vector3 foot; public float w, d, h; public bool blade, full; public int lane, row; }

    static Material signGold, signAmber, signCream;

    static Material Sign(Color c)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetColor("_BaseColor", c);
        return m;
    }

    /// The city within a jump of a route: in `lanes` (x), every `rowStep` from `z0` to `z1`, blocks at the route's own
    /// height (`routeHeight(z)`, a little under mostly, the odd one over). Nothing where the route already stands (every
    /// block is probed against its solids). Lanes within `innerX` of the centre are FULL: gridded roofs and solid
    /// furniture, a roof you can land on; the outer lanes are solid bodies with looks on top. Then the links between
    /// them and the signs. Returns the blocks, for whoever wants to put something on their roofs.
    public static List<Block> Neighbours(System.Func<float, float> routeHeight, float z0, float z1, float[] lanes, float innerX, float rowStep = 38f)
    {
        Physics.SyncTransforms();
        signGold = Sign(new Color(1.6f, 1.25f, 0.5f));
        signAmber = Sign(new Color(1.9f, 1.3f, 0.55f));   // window amber
        signCream = Sign(new Color(1.7f, 1.55f, 1.2f));   // lamp cream
        var blocks = new List<Block>();
        int row = 0;
        for (float z = z0; z < z1; z += rowStep, row++)
        {
            float routeY = routeHeight(z);
            for (int li = 0; li < lanes.Length; li++)
            {
                if ((row + li * 3) % 7 == 3) continue;   // a street through, now and then
                float x = lanes[li] + Random.Range(-6f, 6f), zz = z + Random.Range(-5f, 5f);
                float w = Random.Range(20f, 30f), d = Random.Range(18f, 26f);
                float h = Mathf.Max(30f, routeY + Random.Range(-30f, 6f));
                if (Random.value < 0.12f) h = routeY + Random.Range(10f, 40f);   // the odd one over the route
                bool blade = (row * 3 + li) % 5 == 2 && w > 25f;
                Vector3 foot = new Vector3(x, groundY, zz);
                // The route first: nothing where it stands
                if (Physics.CheckBox(foot + Vector3.up * (h * 0.5f + 2f), new Vector3(w * 0.5f + 3f, h * 0.5f + 2f, d * 0.5f + 3f), Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                bool inner = Mathf.Abs(lanes[li]) < innerX;
                taggingNeighbour = true;   // its roofs are a neighbour's (RoofLife dresses them more lightly)
                NeighbourTower(foot, w, d, h, blade, inner);
                taggingNeighbour = false;
                blocks.Add(new Block { foot = foot, w = w, d = d, h = h, blade = blade, full = inner, lane = li, row = row });
                Physics.SyncTransforms();
            }
        }
        Links(blocks);
        Signs(blocks, innerX);
        return blocks;
    }

    /// One neighbour: the plan's look, a solid body, and (`full`) gridded roofs and solid furniture
    static void NeighbourTower(Vector3 foot, float w, float d, float h, bool blade, bool full)
    {
        if (blade)
        {
            MenuCity.OuterTower(SurfaceKit.Far, sheets, foot, w, 9f, h, 0f, 0, true);
            float low = h * 0.86f;
            SurfaceKit.Solid(foot + Vector3.forward * 8f + Vector3.up * (h * 0.5f), new Vector3(w, h, 9f));
            SurfaceKit.Solid(foot - Vector3.forward * 8f + Vector3.up * (low * 0.5f), new Vector3(w, low, 9f));
            for (int i = 1; i <= 3; i++) SurfaceKit.Solid(foot + Vector3.up * (h * 0.22f * i), new Vector3(w * 0.3f, 2.6f, 8f));
            Jazz(foot + Vector3.forward * 8f, w, 9f, 0f, h, 0f, JazzSeed(foot), true, 1);
            if (full)
            {
                Roof(foot + Vector3.forward * 8f + Vector3.up * h, w - 0.2f, 8.8f, 0f);
                Roof(foot - Vector3.forward * 8f + Vector3.up * low, w - 0.2f, 8.8f, 0f);
                Furnish();
            }
            return;
        }
        MenuCity.RoofPlates = !full;
        MenuCity.OuterTower(SurfaceKit.Far, sheets, foot, w, d, h, 0f, 3, true);
        MenuCity.RoofPlates = true;
        float h1 = h * 0.62f, w2 = w * 0.82f, d2 = d * 0.82f;
        SurfaceKit.Solid(foot + Vector3.up * (h1 * 0.5f), new Vector3(w, h1, d));
        SurfaceKit.Solid(foot + Vector3.up * (h1 + (h - h1) * 0.5f), new Vector3(w2, h - h1, d2));
        Jazz(foot, w, d, 0f, h1, 0f, JazzSeed(foot), false);
        Jazz(foot, w2, d2, h1, h - h1, 0f, JazzSeed(foot) + 1, true);
        if (!full) return;
        Roof(foot + Vector3.up * h, w2 - 0.2f, d2 - 0.2f, 0f);
        Roof(foot + Vector3.up * h1, w - 0.2f, d - 0.2f, 0f);
        Dress(foot, w2, d2, h1, h - h1, 0f, true, true);   // the storeys at the route's height, close enough to see the floors
        Furnish();
    }

    /// The roof's top and its half-extents along x and z (the stack's upper storey, the blade pair's high blade)
    public static void RoofOf(Block b, out Vector3 top, out float hx, out float hz)
    {
        if (b.blade) { top = b.foot + Vector3.forward * 8f + Vector3.up * b.h; hx = b.w * 0.5f; hz = 4.5f; return; }
        top = b.foot + Vector3.up * b.h;
        hx = b.w * 0.41f;
        hz = b.d * 0.41f;
    }

    /// The nearest FULL roof to `from` within `maxDist` (and within `maxDrop` under it, `maxRise` over): where a way
    /// off the route could land. False if there is none.
    public static bool NearestRoof(List<Block> blocks, Vector3 from, float maxDist, float maxDrop, float maxRise, out Block best, out Vector3 top, out float hx, out float hz)
    {
        best = default; top = default; hx = hz = 0f;
        float bestD = float.MaxValue;
        foreach (var b in blocks)
        {
            if (!b.full) continue;
            RoofOf(b, out Vector3 t, out float x, out float z);
            float dy = t.y - from.y;
            if (dy < -maxDrop || dy > maxRise) continue;
            Vector3 flat = t - from; flat.y = 0f;
            float d = flat.magnitude;
            if (d > maxDist || d >= bestD) continue;
            bestD = d; best = b; top = t; hx = x; hz = z;
        }
        return bestD < float.MaxValue;
    }

    /// What ties neighbours together: between blocks side by side and one behind the other, at roof height, a girder
    /// bridge (solid: a way across), a duct on a post, or a bundle of cables with a droop in it
    static void Links(List<Block> blocks)
    {
        for (int i = 0; i < blocks.Count; i++)
            for (int j = i + 1; j < blocks.Count; j++)
            {
                var a = blocks[i]; var b = blocks[j];
                bool beside = a.row == b.row && Mathf.Abs(a.lane - b.lane) == 1;
                bool behind = a.lane == b.lane && b.row == a.row + 1;
                if (!beside && !behind) continue;
                if (Mathf.Abs(a.h - b.h) > 8f) continue;
                RoofOf(a, out Vector3 ta, out float ax, out float az); RoofOf(b, out Vector3 tb, out float bx, out float bz);
                Vector3 dir = tb - ta; dir.y = 0f;
                float gap = dir.magnitude - (beside ? ax + bx : az + bz);
                if (gap < 4f || gap > 26f) continue;
                dir.Normalize();
                Vector3 from = ta + dir * (beside ? ax : az), to = tb - dir * (beside ? bx : bz);
                float pick = Random.value;
                if (pick < 0.3f)
                {
                    Bridge(from + Vector3.up * 0.02f, to + Vector3.up * 0.02f, 3f);
                }
                else if (pick < 0.7f)
                {
                    Vector3 f = from + Vector3.down * 2.4f, t = to + Vector3.down * 2.4f;
                    SurfaceKit.Look(LevelKit.Iron, (f + t) * 0.5f, new Vector3(1.2f, 1f, Vector3.Distance(f, t) + 1f), SurfaceKit.SpanRot(f, t));
                    SurfaceKit.Look(LevelKit.Girder, (f + t) * 0.5f + Vector3.down * 4f, new Vector3(0.3f, 7f, 0.3f));
                }
                else
                {
                    Vector3 mid = (from + to) * 0.5f + Vector3.down * (gap * 0.18f + 1.5f);
                    for (int k = -1; k <= 1; k++)
                    {
                        Vector3 off = Vector3.Cross(dir, Vector3.up) * (k * 0.5f);
                        Vector3 f = from + off + Vector3.up * 1.2f, t = to + off + Vector3.up * 1.2f, m = mid + off;
                        SurfaceKit.Look(LevelKit.Dark, (f + m) * 0.5f, new Vector3(0.06f, 0.06f, Vector3.Distance(f, m)), SurfaceKit.SpanRot(f, m));
                        SurfaceKit.Look(LevelKit.Dark, (m + t) * 0.5f, new Vector3(0.06f, 0.06f, Vector3.Distance(m, t)), SurfaceKit.SpanRot(m, t));
                    }
                }
            }
    }

    /// Lit signs on the faces that look at the route: gold, amber, cream, in brass frames — the city's colours now
    static void Signs(List<Block> blocks, float innerX)
    {
        Material[] mats = { signGold, signAmber, signCream };
        int n = 0;
        foreach (var b in blocks)
        {
            if (Mathf.Abs(b.foot.x) > innerX || (n++ % 3) != 0) continue;
            float side = b.foot.x < 0f ? 1f : -1f;   // toward the route
            float fx = b.foot.x + side * (b.w * 0.5f + 0.2f);
            Vector3 c = new Vector3(fx, b.foot.y + b.h * Random.Range(0.45f, 0.7f), b.foot.z + (b.blade ? 8f : 0f));
            bool banner = Random.value < 0.35f;
            Vector3 size = banner ? new Vector3(0.16f, b.h * 0.4f, 1.4f) : new Vector3(0.16f, 3f, Mathf.Min(b.w * 0.6f, 8f));
            SurfaceKit.Look(mats[n % 3], c, size);
            SurfaceKit.Look(LevelKit.Dark, c - Vector3.right * (side * 0.12f), size + new Vector3(-0.06f, 0.4f, 0.4f));
            SurfaceKit.Look(LevelKit.Brass, c + Vector3.right * (side * 0.02f) + Vector3.up * (size.y * 0.5f + 0.1f), new Vector3(0.22f, 0.14f, size.z + 0.5f));   // the brass frame, top and bottom
            SurfaceKit.Look(LevelKit.Brass, c + Vector3.right * (side * 0.02f) - Vector3.up * (size.y * 0.5f + 0.1f), new Vector3(0.22f, 0.14f, size.z + 0.5f));
        }
    }
}
