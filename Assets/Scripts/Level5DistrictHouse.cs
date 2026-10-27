using UnityEngine;

/// <summary>
/// HALCYON HOUSE'S OWN PIECES (Level 5, the House's quarter). The CUT PANE: a glass pane with a CYAN bar across it —
/// CYAN MEANS CUT HERE — cut the bar (a blade's reach, a card's flight) and the pane shatters as if crashed through
/// (BreakableGlass pays the style; a crash at speed still breaks it; the bar rides on the pane and goes with it).
/// The COMPANY MARK: the brass sun on black marble with the wordmark under it, plain brass and ink, never a
/// Hologram, so it never glitches; the stepped PYLONS that carry it; a flat FLOOR MARK in the paving; stepped brass
/// PORTALS round a doorway; brass RAILS; book and ledger STACKS (every vertical edge an authored corner, cream flutes
/// on the ends, brass shelf lines); pneumatic TUBES; octagonal PORTHOLE rings. Looks are LevelKit.Prim (no
/// collider); solids are LevelKit slabs; nothing here moves but the cut pane, which carries its own MonoBehaviour.
/// </summary>
public class HouseCutPane : MonoBehaviour, IStrikeTarget
{
    public BreakableGlass glass;
    public GameObject bar;
    public Vector3 normal = Vector3.up;
    Renderer pane;

    void Start() { pane = glass != null ? glass.GetComponent<Renderer>() : null; }

    /// The strike lands: the pane breaks the way a crash would, fast enough to count
    public bool Struck(Vector3 point, bool arrow)
    {
        if (glass == null || glass.config == null) return false;
        float speed = glass.config.glassBreakSpeed + 2f;
        bool broke = glass.TryBreak(-normal * speed, normal, point);
        if (broke && bar != null) bar.SetActive(false);
        return broke;
    }

    void Update()
    {
        if (bar != null && bar.activeSelf && (glass == null || pane == null || !pane.enabled)) bar.SetActive(false);
    }
}

public static class HouseKit
{
    const float Proud = LevelKit.MarkProud;
    static readonly Color Gold = new Color(2.2f, 1.7f, 0.4f);

    /// A CUT PANE: breakable glass `size` at `centre`, the cyan bar on the face along `normal` (up for a roof light,
    /// a wall's outward for a window). The bar is the pane's child, so it goes when the glass goes
    public static BreakableGlass CutPane(Transform root, Vector3 centre, Vector3 size, MovementConfig config, Vector3 normal)
    {
        var glass = LevelKit.Glass(root, centre, size, config);
        normal = normal.normalized;
        float thick = Mathf.Abs(normal.x) * size.x + Mathf.Abs(normal.y) * size.y + Mathf.Abs(normal.z) * size.z;
        Vector3 barSize;
        if (Mathf.Abs(normal.y) > 0.5f) barSize = size.z >= size.x ? new Vector3(0.16f, 0.04f, size.z - 0.5f) : new Vector3(size.x - 0.5f, 0.04f, 0.16f);
        else if (Mathf.Abs(normal.z) > 0.5f) barSize = new Vector3(size.x - 0.5f, 0.16f, 0.04f);
        else barSize = new Vector3(0.04f, 0.16f, size.z - 0.5f);
        var bar = LevelKit.Prim(null, centre + normal * (thick * 0.5f + 0.03f), barSize, LevelKit.NeonCyan, "GlassCutBar");
        bar.transform.SetParent(glass.transform, true);
        var cut = glass.gameObject.AddComponent<HouseCutPane>();
        cut.glass = glass; cut.bar = bar; cut.normal = normal;
        return glass;
    }

    /// THE COMPANY MARK on a surface at `at`, facing `facing`: the brass sun on marble, the wordmark in gold under it
    public static void Mark(Transform root, Vector3 at, Vector3 facing, float size, string word = null)
    {
        Quaternion rot = Quaternion.LookRotation(-facing.normalized, Vector3.up);
        LevelKit.Emblem(root, at, rot, size, LevelKit.Brass, LevelKit.Marble);
        if (!string.IsNullOrEmpty(word))
            LevelKit.Text(root, at - Vector3.up * (size * 0.74f) + facing.normalized * 0.01f, rot, word, size * 0.07f, Gold, "HouseMark", size * 1.7f, true);
    }

    /// THE MARK inlaid flat in a floor at `at` (the sun's bar toward `forward`): three prims, millimetres proud
    public static void FloorMark(Transform root, Vector3 at, float size, Vector3 forward)
    {
        forward.y = 0f;
        Quaternion rot = Quaternion.LookRotation(forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward, Vector3.up);
        LevelKit.Prim(root, at + Vector3.up * Proud, new Vector3(size, 0.006f, size), LevelKit.Brass, "BrassInlay", PrimitiveType.Cylinder);
        LevelKit.Prim(root, at + Vector3.up * (Proud + 0.004f), new Vector3(size * 0.62f, 0.006f, size * 0.62f), LevelKit.Marble, "InlayField", PrimitiveType.Cylinder);
        LevelKit.Prim(root, at + Vector3.up * (Proud + 0.008f) - rot * Vector3.forward * (size * 0.16f), new Vector3(size * 1.15f, 0.006f, size * 0.09f), LevelKit.Brass, "BrassInlay", PrimitiveType.Cube, rot);
    }

    /// A PYLON: a stepped limestone pier `h` tall — its foot a lip, its shaft an edge-boost corner — carrying the mark
    /// on the face toward `facing`, a cream globe lit on top
    public static void Pylon(Transform root, Vector3 foot, Vector3 facing, float h)
    {
        Vector3 n = facing.normalized;
        float shaft = h - 1.9f;
        LevelKit.Lip(root, foot + Vector3.up * 0.7f, new Vector3(3f, 1.4f, 3f), LevelKit.Limestone, "PylonFoot");
        LevelKit.BoostCorner(root, foot + Vector3.up * (1.35f + shaft * 0.5f), new Vector3(2f, shaft, 2f), LevelKit.Limestone);
        LevelKit.Prim(root, foot + Vector3.up * (h - 0.25f), new Vector3(2.5f, 0.5f, 2.5f), LevelKit.Limestone, "PylonCap");
        LevelKit.Prim(root, foot + Vector3.up * (h + 0.2f), new Vector3(1.4f, 0.4f, 1.4f), LevelKit.Brass, "PylonCrown");
        LevelKit.Prim(root, foot + Vector3.up * (h + 0.85f), new Vector3(0.9f, 0.9f, 0.9f), LevelKit.LampCream, "PylonGlobe", PrimitiveType.Sphere);
        var l = LevelKit.Light(root, foot + Vector3.up * (h + 0.5f), 16f, 1.4f);
        l.color = LevelKit.LampCreamLight;
        Mark(root, foot + Vector3.up * (1.35f + shaft * 0.62f) + n * 1.02f, n, 1.3f, LevelKit.Company);
    }

    /// A stepped brass PORTAL round a doorway `width` × `height` whose foot is `at` on the wall, facing `outward`:
    /// jamb strips, a three-step lintel (the marble band carries `name`), a sunburst of rays over it. Looks
    public static void Portal(Transform root, Vector3 at, Vector3 outward, float width, float height, string name = null)
    {
        outward.y = 0f; outward.Normalize();
        Quaternion turn = Quaternion.LookRotation(outward, Vector3.up);
        Vector3 right = turn * Vector3.right;
        for (int s = -1; s <= 1; s += 2)
            LevelKit.Prim(root, at + right * (s * (width * 0.5f + 0.3f)) + Vector3.up * (height * 0.5f) + outward * 0.1f, new Vector3(0.6f, height, 0.2f), LevelKit.Brass, "PortalJamb", PrimitiveType.Cube, turn);
        float[] tall = { 0.4f, 0.9f, 0.4f };
        float y = height + 0.1f;
        for (int i = 0; i < 3; i++)
        {
            float w = width + 1.2f - i * 1.4f;
            LevelKit.Prim(root, at + Vector3.up * (y + tall[i] * 0.5f) + outward * (0.1f + i * 0.03f), new Vector3(w, tall[i], 0.2f + i * 0.06f), i == 1 ? LevelKit.Marble : LevelKit.Brass, "PortalStep", PrimitiveType.Cube, turn);
            if (i == 1 && !string.IsNullOrEmpty(name))
                LevelKit.Text(root, at + Vector3.up * (y + tall[i] * 0.5f) + outward * 0.27f, Quaternion.LookRotation(-outward), name, 0.5f, Gold, "PortalName", w - 1f, true);
            y += tall[i];
        }
        for (int i = -3; i <= 3; i++)
        {
            Quaternion rot = turn * Quaternion.Euler(0f, 0f, i * 20f);
            Vector3 dir = rot * Vector3.up;
            LevelKit.Prim(root, at + Vector3.up * (y + 0.1f) + outward * 0.09f + dir * 0.8f, new Vector3(0.06f, 1.5f, 0.03f), LevelKit.Brass, "SunburstRay", PrimitiveType.Cube, rot);
        }
    }

    /// A brass RAIL along an edge from `a` to `b`: a top bar at `height`, a post every two metres. Looks
    public static void Rail(Transform root, Vector3 a, Vector3 b, float height = 1f)
    {
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 0.5f) return;
        Quaternion rot = Quaternion.LookRotation(d / len, Vector3.up);
        LevelKit.Prim(root, (a + b) * 0.5f + Vector3.up * height, new Vector3(0.08f, 0.08f, len), LevelKit.Brass, "RailTop", PrimitiveType.Cube, rot);
        int posts = Mathf.Max(1, Mathf.RoundToInt(len / 2f));
        for (int i = 0; i <= posts; i++)
            LevelKit.Prim(root, Vector3.Lerp(a, b, i / (float)posts) + Vector3.up * (height * 0.5f), new Vector3(0.06f, height, 0.06f), LevelKit.Brass, "RailPost", PrimitiveType.Cube, rot);
    }

    /// A STACK: a book or ledger stack `size` at `centre` (2.4 m tall): a solid with every vertical edge an authored
    /// corner, a cream flute on each end (the kick), three brass shelf lines along each broad face (looks)
    public static GameObject Stack(Transform root, Vector3 centre, Vector3 size, Material mat = null)
    {
        var box = LevelKit.Pillar(root, centre, size, mat ?? LevelKit.WoodPanel);
        box.name = "Stack";
        bool alongZ = size.z >= size.x;
        float len = alongZ ? size.z : size.x, thick = alongZ ? size.x : size.z;
        Vector3 run = alongZ ? Vector3.forward : Vector3.right, across = alongZ ? Vector3.right : Vector3.forward;
        for (int s = -1; s <= 1; s += 2)
        {
            LevelKit.Prim(root, centre + run * (s * (len * 0.5f + 0.02f)), alongZ ? new Vector3(thick * 0.6f, size.y - 0.3f, 0.04f) : new Vector3(0.04f, size.y - 0.3f, thick * 0.6f), LevelKit.Fluted, "StackFlute");
            for (int k = 1; k <= 3; k++)
                LevelKit.Prim(root, centre + across * (s * (thick * 0.5f + 0.015f)) + Vector3.up * (-size.y * 0.5f + k * size.y * 0.25f),
                    alongZ ? new Vector3(0.03f, 0.05f, len - 0.2f) : new Vector3(len - 0.2f, 0.05f, 0.03f), LevelKit.Brass, "ShelfLine");
        }
        return box;
    }

    /// A pneumatic TUBE: a brass pipe from `a` to `b` (looks, no collider)
    public static void Tube(Transform root, Vector3 a, Vector3 b, float dia = 0.25f)
    {
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 0.2f) return;
        LevelKit.Prim(root, (a + b) * 0.5f, new Vector3(dia, len * 0.5f, dia), LevelKit.Brass, "PneumaticTube", PrimitiveType.Cylinder, Quaternion.FromToRotation(Vector3.up, d / len));
    }

    /// A PORTHOLE ring: eight brass segments round a window on a face, `centre` on the wall plane, facing `outward`
    public static void Porthole(Transform root, Vector3 centre, Vector3 outward, float radius)
    {
        outward.y = 0f; outward.Normalize();
        Quaternion turn = Quaternion.LookRotation(outward, Vector3.up);
        float seg = 2f * radius * Mathf.Tan(Mathf.PI / 8f) + 0.12f;
        for (int i = 0; i < 8; i++)
        {
            Quaternion r = turn * Quaternion.Euler(0f, 0f, i * 45f);
            LevelKit.Prim(root, centre + r * Vector3.up * radius + outward * 0.12f, new Vector3(seg, 0.3f, 0.2f), LevelKit.Brass, "Porthole", PrimitiveType.Cube, r);
        }
    }
}
