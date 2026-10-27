using UnityEngine;

/// <summary>
/// THE SKYBRIDGE (district-grammar, Level 4): a glazed Deco footbridge between two blocks' roofs across the street
/// between them. A terrazzo deck 4 m wide whose ends ride over both roofs' parapets (5 cm proud of their tops),
/// parapet lips along both sides, a clerestory of breakable panes over the lips, a glass BARREL over that in facets
/// between brass ribs (every pane a cut target: at speed it is a door), a PORTAL at each end (two fluted limestone
/// piers — edge-boost corners — under a stepped lintel with a globe lamp hung from it), and a 20° slide off each end
/// down to its roof deck. Solids are slabs (the weld takes them); the ribs, the caps and the stringers are looks.
/// Nothing hangs under the deck: the street below keeps its tram and its lamp standards (the deck's underside is
/// nineteen metres up on the hotels; the tallest thing in an Act I street is a lamp standard at six).
/// </summary>
public static partial class LevelKit
{
    /// The bridge between `a` and `b` (neighbouring blocks along x or along z) whose roof decks both stand at `y`
    public static void Skybridge(Transform root, MovementConfig config, CityDistrict.Block a, CityDistrict.Block b, float y)
        => Skybridge(root, config, a, b, y, y, float.NaN);

    /// The bridge between two roof decks at `roofA` and `roofB` (the deck rides 5 cm over the higher one's parapet
    /// top, a slide runs down to each); `across` is the world coordinate along the shared edge the deck is centred
    /// on (NaN: the middle of the two blocks' overlap)
    public static void Skybridge(Transform root, MovementConfig config, CityDistrict.Block a, CityDistrict.Block b, float roofA, float roofB, float across)
    {
        const float W = 4f, DeckT = 0.45f, Over = 1.4f, LipH = 1f, Spring = 2.2f, R = 2f, Pier = 4.5f;
        Bounds ba = a.bounds, bb = b.bounds;
        bool alongX = Mathf.Abs(ba.center.x - bb.center.x) > Mathf.Abs(ba.center.z - bb.center.z);
        if ((alongX ? ba.center.x : ba.center.z) > (alongX ? bb.center.x : bb.center.z)) { var t = ba; ba = bb; bb = t; float tr = roofA; roofA = roofB; roofB = tr; }
        Vector3 along = alongX ? Vector3.right : Vector3.forward, side = alongX ? Vector3.forward : Vector3.right;
        float u0 = (alongX ? ba.max.x : ba.max.z) - Over, u1 = (alongX ? bb.min.x : bb.min.z) + Over, len = u1 - u0, mid = (u0 + u1) * 0.5f;
        float v0 = Mathf.Max(alongX ? ba.min.z : ba.min.x, alongX ? bb.min.z : bb.min.x), v1 = Mathf.Min(alongX ? ba.max.z : ba.max.x, alongX ? bb.max.z : bb.max.x);
        float v = float.IsNaN(across) ? (v0 + v1) * 0.5f : across;
        float top = Mathf.Max(roofA, roofB) + 0.95f;   // the parapets stand 0.9 over a roof deck: the deck 5 cm proud of them
        Vector3 Pt(float u, float yy, float vv) => alongX ? new Vector3(u, yy, vv) : new Vector3(vv, yy, u);
        Vector3 Ext(float lu, float ly, float lv) => alongX ? new Vector3(lu, ly, lv) : new Vector3(lv, ly, lu);

        // The deck and its parapet lips; the clerestory panes over the lips in bays, the barrel's facets over them
        FloorSlab(root, Pt(mid, top - DeckT * 0.5f, v), Ext(len, DeckT, W), "SkybridgeDeck");
        for (int s = -1; s <= 1; s += 2) Parapet(root, Pt(mid, top + LipH * 0.5f, v + s * (W * 0.5f - 0.25f)), Ext(len, LipH, 0.5f));
        int bays = Mathf.Max(2, Mathf.CeilToInt(len / 7f));
        float bayLen = len / bays;
        for (int i = 0; i < bays; i++)
        {
            float bu = u0 + bayLen * (i + 0.5f);
            for (int s = -1; s <= 1; s += 2)
                Glass(root, Pt(bu, top + (LipH + Spring) * 0.5f, v + s * (W * 0.5f - 0.1f)), Ext(bayLen - 0.36f, Spring - LipH - 0.2f, 0.12f), config);
            Vector3 c = Pt(bu, top + Spring, v);
            for (int k = 0; k < 6; k++)
            {
                float th = (15f + 30f * k) * Mathf.Deg2Rad;
                Vector3 n = -Mathf.Cos(th) * side + Mathf.Sin(th) * Vector3.up;
                SkybridgePane(root, c + n * (R * Mathf.Cos(15f * Mathf.Deg2Rad)), new Vector3(2f * R * Mathf.Sin(15f * Mathf.Deg2Rad) + 0.05f, 0.12f, bayLen - 0.36f), Quaternion.LookRotation(along, n), config);
            }
        }
        // The brass ribs: an arc at every bay edge and mid-bay, posts down the clerestory, the ridge along the crown
        for (int i = 0; i <= bays * 2; i++)
        {
            float u = u0 + bayLen * 0.5f * i;
            Vector3 c = Pt(u, top + Spring, v);
            for (int j = 0; j < 12; j++)
            {
                float th = (7.5f + 15f * j) * Mathf.Deg2Rad;
                Vector3 n = -Mathf.Cos(th) * side + Mathf.Sin(th) * Vector3.up;
                Prim(root, c + n * (R + 0.14f), new Vector3(0.6f, 0.14f, 0.14f), Brass, "SkybridgeRib", PrimitiveType.Cube, Quaternion.LookRotation(along, n));
            }
            for (int s = -1; s <= 1; s += 2) Prim(root, Pt(u, top + (LipH + Spring) * 0.5f, v + s * (W * 0.5f - 0.02f)), Ext(0.14f, Spring - LipH, 0.14f), Brass, "SkybridgeRib");
        }
        Prim(root, Pt(mid, top + Spring + R + 0.14f, v), Ext(len, 0.12f, 0.22f), Brass, "SkybridgeRidge");

        // The portals: piers, the stepped lintel, the lamp; the slide off each end down to its roof deck
        for (int e = 0; e < 2; e++)
        {
            float inward = e == 0 ? 1f : -1f, uEnd = e == 0 ? u0 : u1, u = uEnd + inward * 0.5f, roof = e == 0 ? roofA : roofB;
            for (int s = -1; s <= 1; s += 2)
            {
                BoostCorner(root, Pt(u, top + Pier * 0.5f, v + s * (W * 0.5f - 0.3f)), Ext(0.6f, Pier, 0.6f), Limestone);
                Prim(root, Pt(u, top + Pier - 0.2f, v + s * (W * 0.5f - 0.3f)), new Vector3(0.9f, 0.3f, 0.9f), Brass, "PierCapital");
            }
            Slab(root, Pt(u, top + Pier + 0.35f, v), Ext(0.7f, 0.7f, W + 0.2f), Limestone, "PortalLintel");
            Prim(root, Pt(u, top + Pier + 0.85f, v), Ext(0.8f, 0.3f, W * 0.72f), Limestone, "PortalStep");
            Prim(root, Pt(u, top + Pier + 1.12f, v), Ext(0.9f, 0.24f, W * 0.46f), Limestone, "PortalStep");
            GlobeLamp(root, Pt(u, top + Pier - 0.9f, v), top + Pier);
            if (top - roof > 0.3f)
            {
                float run = (top - roof) / Mathf.Tan(20f * Mathf.Deg2Rad);
                SkybridgeRamp(root, Pt(uEnd, top, v), Pt(uEnd - inward * run, roof, v), W);
            }
        }
    }

    /// A barrel facet: a breakable pane turned to `rot` with its frame turned with it (the frame is laid axis-aligned
    /// round the pane; it is the last four children of the parent when the pane is made)
    static void SkybridgePane(Transform parent, Vector3 c, Vector3 size, Quaternion rot, MovementConfig config)
    {
        int first = parent.childCount;
        var g = Glass(parent, c, size, config);
        g.transform.rotation = rot;
        for (int i = first + 1; i < parent.childCount; i++)
        {
            var bar = parent.GetChild(i);
            if (bar.name != "GlassFrame") continue;
            bar.position = c + rot * (bar.position - c);
            bar.rotation = rot;
        }
    }

    /// A slide whose top runs from `a` down to `b`, `width` across: a slope slab turned to the line, bronze stringers under its edges
    static void SkybridgeRamp(Transform parent, Vector3 a, Vector3 b, float width)
    {
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 0.3f) return;
        Quaternion rot = Quaternion.LookRotation(d / len, Vector3.up);
        var go = Slab(parent, (a + b) * 0.5f - rot * Vector3.up * 0.175f, new Vector3(width, 0.35f, len), Slope, "SkybridgeRamp");
        go.transform.rotation = rot;
        for (int s = -1; s <= 1; s += 2)
            Prim(parent, (a + b) * 0.5f + rot * (Vector3.right * (s * (width * 0.5f - 0.15f)) - Vector3.up * 0.48f), new Vector3(0.2f, 0.26f, len - 0.3f), Bronze, "RampStringer", PrimitiveType.Cube, rot);
    }
}
