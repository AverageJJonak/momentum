using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HARD LINE (2026-09-29), THE POSTER CUT (2026-10-16): the ink line of the style, and the pass that puts it on the
/// masses. The style in one sentence now: EVERY MASS IS DRAWN. A black bead on the outside edges of every SOLID MASS
/// (a block at least 0.8 m in two of its dimensions, an edge at least 1.5 m long), so a wall end, a pillar, a plinth,
/// a roof slab reads as a drawn shape against the next colour field; and one lit hairline just inside the top edges of
/// anything you can stand on, in the zone's light, so the edge of every floor, roof, ledge and deck reads from any
/// distance (readability: kept on every walkable edge a metre or longer). What went with the cut: beads on trims,
/// rails, props and small ornaments (they were noise), the bolt heads on the corners of big slabs, the shadow strips
/// under small uprights. Rooms keep their own trims (Articulate, Densify): walls are left alone here. Everything made
/// here is decor, welded into a few meshes, no colliders.
/// </summary>
public static partial class LevelKit
{
    static Material beadMat;
    static readonly Dictionary<Zone, Material> lineMats = new Dictionary<Zone, Material>();

    /// A solid is a MASS when two of its three dimensions are at least this (a trim, a rail, a prop is not)
    public const float BeadMass = 0.8f;
    /// An edge is beaded when it is at least this long
    public const float BeadEdge = 1.5f;
    /// A walkable edge gets its lit line when it is at least this long
    public const float LineEdge = 1f;

    /// The bead: black, matte, a little metal so a highlight still finds it
    static Material Bead()
    {
        if (beadMat != null) return beadMat;
        beadMat = Lit(Shader.Find("Universal Render Pipeline/Lit"), new Color(0.045f, 0.045f, 0.052f), 0.35f);
        beadMat.SetFloat("_Metallic", 0.25f);
        beadMat.name = "HardLineBead";
        return beadMat;
    }

    /// The line: the zone's light, unlit, kept low so it draws and does not bloom
    static Material LineFor(Zone z)
    {
        if (lineMats.TryGetValue(z, out var m) && m != null) return m;
        Color c = z switch
        {
            Zone.Utopia => new Color(1.4f, 1.25f, 0.95f),      // lamp cream
            Zone.Underbelly => new Color(1.2f, 0.62f, 0.3f),   // gaslight amber, dim
            Zone.Office => new Color(1.3f, 1.2f, 1.02f),       // warm cream, the house's own
            Zone.Works => new Color(1.4f, 1f, 0.42f),           // amber
            _ => new Color(1.5f, 1.15f, 0.5f),   // the surface, the roofs: brass
        };
        m = Unlit(Shader.Find("Universal Render Pipeline/Unlit"), c);
        m.name = "HardLine" + z;
        lineMats[z] = m;
        return m;
    }

    static readonly string[] walkable = { "Floor", "Roof", "Ledge", "Landing", "Bench", "Catwalk", "Deck", "Lintel", "Rim", "Cross", "Top", "Step", "Platform", "Gallery", "Balcony", "Tier", "Terrace", "Stage", "Walk", "Bridge", "Span", "Ramp", "Slab" };
    static readonly string[] never = { "Wall", "Pad", "Hazard", "Glass", "Sign", "Beacon", "Text", "Emblem", "Portal", "Door", "Milk", "Orb", "Water", "Lamp", "Light", "Neon", "Strip", "Pinch", "Kerb", "Rail", "Cart", "FarGround", "Ash", "Ground" };

    static bool NameHas(string name, string[] words) { foreach (var w in words) if (name.Contains(w)) return true; return false; }

    /// Which zone a point is in: the section its z falls in when the level was built by sections, else the level's own
    static Zone ZoneAt(Vector3 p, LevelSections.Layout layout, Zone fallback)
    {
        if (layout != null)
            foreach (var r in layout.rooms)
                if (p.z >= r.z0 - 0.5f && p.z <= r.z1 + 0.5f) return r.zone;
        return fallback;
    }

    /// THE PASS. Run once after the level is built and the coplanar fixer has settled the boxes
    public static int Precision(Zone fallback, LevelSections.Layout layout)
    {
        var baker = new MeshBaker { CellSize = 60f };
        var bead = Bead();
        int done = 0;
        foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (done >= 9000) break;
            var mf = mr.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null || mf.sharedMesh.name != "Cube") continue;
            if (mr.GetComponent<Collider>() == null) continue;                                   // a look: baked things and decor never get here anyway
            if (mr.GetComponentInParent<Enemy>() != null || mr.GetComponentInParent<Rigidbody>() != null) continue;
            var mat = mr.sharedMaterial;
            if (mat == null || mat.shader == null || mat.shader.name.Contains("Unlit")) continue;   // glows draw their own line
            string name = mr.gameObject.name;
            if (NameHas(name, never)) continue;
            var t = mr.transform;
            Vector3 s = t.lossyScale;
            float maxDim = Mathf.Max(s.x, Mathf.Max(s.y, s.z));
            if (maxDim < LineEdge || maxDim > 400f) continue;
            if (s.x <= 0f || s.y <= 0f || s.z <= 0f) continue;
            // A mass: two of the three dimensions at least BeadMass (the middle one of the three sorted)
            float mid = s.x + s.y + s.z - maxDim - Mathf.Min(s.x, Mathf.Min(s.y, s.z));
            bool mass = mid >= BeadMass;
            Quaternion q = t.rotation;
            Vector3 c = t.position, e = s * 0.5f;
            // The block's up: the local axis that points most nearly up, and whether it is the thin one (a flat) or not
            int upAxis = 0; float best = -1f; float sign = 1f;
            for (int a = 0; a < 3; a++)
            {
                Vector3 dir = q * Axis(a);
                float d = Vector3.Dot(dir, Vector3.up);
                if (Mathf.Abs(d) > best) { best = Mathf.Abs(d); upAxis = a; sign = d < 0f ? -1f : 1f; }
            }
            int thin = s.x <= s.y && s.x <= s.z ? 0 : s.y <= s.z ? 1 : 2;
            bool flat = thin == upAxis && best > 0.7f;
            bool walk = flat && NameHas(name, walkable);
            if (!mass && !walk) continue;
            float b = maxDim >= 4f ? 0.05f : 0.035f;
            Zone zone = ZoneAt(c, layout, fallback);
            var line = LineFor(zone);
            float inset = name.Contains("Roof") ? 0.62f : 0.06f;
            int a1 = (upAxis + 1) % 3, a2 = (upAxis + 2) % 3;
            bool any = false;
            // The four edges round the top face: a bead on each long enough, a lit line just inside each when you can stand here
            for (int k = 0; k < 2; k++)
            {
                int run = k == 0 ? a1 : a2, across = k == 0 ? a2 : a1;
                float len = s[run];
                for (int sd = -1; sd <= 1; sd += 2)
                {
                    Vector3 local = Vector3.zero;
                    local[upAxis] = sign * e[upAxis];
                    local[across] = sd * e[across];
                    if (mass && len >= BeadEdge)
                    {
                        Vector3 size = Vector3.one * b; size[run] = len + b;
                        baker.Add(bead, c + q * local, size, q);
                        if (flat && !name.StartsWith("Floor") && s[upAxis] >= 0.25f) { Vector3 under = local; under[upAxis] = -sign * e[upAxis]; baker.Add(bead, c + q * under, size, q); }   // a catwalk, a roof, a deck is seen from below
                        any = true;
                    }
                    if (!walk || len < LineEdge) continue;
                    Vector3 ll = local;
                    ll[across] = sd * (e[across] - inset);
                    ll[upAxis] = sign * (e[upAxis] + 0.008f);
                    Vector3 ls = new Vector3(0.02f, 0.02f, 0.02f); ls[run] = Mathf.Max(0.3f, len - inset * 2f); ls[upAxis] = 0.016f;
                    baker.Add(line, c + q * ll, ls, q);
                    any = true;
                }
            }
            // An upright mass (a pillar, a stack, a block you run into) gets its four vertical edges beaded too, and a
            // dark strip round its foot on whatever it stands on (the contact shadow a painter puts under a mass)
            if (!flat && mass)
            {
                float len = s[upAxis];
                if (len >= BeadEdge)
                    for (int s1 = -1; s1 <= 1; s1 += 2)
                        for (int s2 = -1; s2 <= 1; s2 += 2)
                        {
                            Vector3 local = Vector3.zero;
                            local[a1] = s1 * e[a1]; local[a2] = s2 * e[a2];
                            Vector3 size = Vector3.one * b; size[upAxis] = len + b;
                            baker.Add(bead, c + q * local, size, q);
                            any = true;
                        }
                for (int k = 0; k < 2; k++)
                {
                    int run = k == 0 ? a1 : a2, across = k == 0 ? a2 : a1;
                    if (s[run] < BeadEdge) continue;
                    for (int sd = -1; sd <= 1; sd += 2)
                    {
                        Vector3 local = Vector3.zero;
                        local[upAxis] = -sign * (e[upAxis] - 0.006f);
                        local[across] = sd * (e[across] + 0.07f);
                        Vector3 size = Vector3.one * 0.14f; size[run] = s[run] + 0.28f; size[upAxis] = 0.012f;
                        baker.Add(bead, c + q * local, size, q);
                        any = true;
                    }
                }
            }
            if (any) done++;
        }
        var root = new GameObject("HardLine").transform;
        baker.Flush(root, "HardLine", false);
        return done;
    }

    static Vector3 Axis(int a) => a == 0 ? Vector3.right : a == 1 ? Vector3.up : Vector3.forward;
}
