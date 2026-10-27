using UnityEngine;

/// <summary>
/// THE STREET KIT (district-grammar, 2026-10-12): what a district's streets and facades are drawn with, in the Deco
/// vocabulary MenuCityGotham gives the far city — shopfronts under awnings, MARQUEES on brass chains, punched windows
/// in blank piers with string courses between the floors, stepped cornices, LAMP STANDARDS (iron posts: kick posts,
/// every one an authored corner), blade signs. Rules as LevelKitCourse: solids are Slab/Solid, looks are baked
/// (Facades: one MeshBaker per district, flushed by the builder) or Prim, nothing on a wall stands more than 0.35 m
/// proud so a wall-run passes over it, the coplanar rule everywhere (3 mm off every face).
/// </summary>
public static partial class LevelKit
{
    static Material settsMat, pavementMat, paneMat, canvasMat;
    static MeshBaker facades;

    /// A material kept across the level and remade after a scene change took it (Unity's null, not C#'s)
    static Material Keep(ref Material m, System.Func<Material> make) { if (m == null) m = make(); return m; }
    /// Granite setts: the carriageway, charcoal with the speed grid's brass lines kept
    public static Material Setts => Keep(ref settsMat, () => Surface(new Color(0.1f, 0.1f, 0.11f), 0.3f, Surf.Concrete, 2f, 0.9f, Grid(), new Color(0.5f, 0.37f, 0.16f)));
    /// Limestone flags: the pavements and the plazas, the grid in brass
    public static Material Pavement => Keep(ref pavementMat, () => Surface(new Color(0.36f, 0.34f, 0.3f), 0.3f, Surf.Ashlar, 2f, 0.6f, Grid(), new Color(0.6f, 0.45f, 0.2f)));
    /// A dark pane: a window nobody has lit
    public static Material Pane => Keep(ref paneMat, () => Unlit(Shader.Find("Universal Render Pipeline/Unlit"), new Color(0.05f, 0.05f, 0.07f)));
    /// Oxblood canvas: awnings
    public static Material Canvas => Keep(ref canvasMat, () => Surface(OxbloodRed, 0.15f, Surf.Panel, 1f, 0.5f));

    /// The district's facade looks are baked into one mesh per material: Begin before the blocks, Flush after
    public static void BeginFacades() => facades = new MeshBaker { CellSize = 60f };
    public static void FlushFacades(Transform parent) { facades?.Flush(parent, "Facades", true); facades = null; }
    static void Bake(Material mat, Vector3 pos, Vector3 size, Quaternion? rot = null, PrimitiveType type = PrimitiveType.Cube)
    {
        if (facades != null) facades.Add(mat, pos, size, rot, type);
        else Prim(null, pos, size, mat, "Facade", type, rot);
    }

    static void ResetStreet() { settsMat = pavementMat = paneMat = canvasMat = null; facades = null; }

    /// A frame along a wall: `right` runs along it, `up`, `outward` is the way off it into the street
    static void Axes(Vector3 outward, out Vector3 right, out Quaternion turn)
    {
        outward.y = 0f; outward.Normalize();
        turn = Quaternion.LookRotation(outward, Vector3.up);
        right = turn * Vector3.right;
    }

    /// A grid district's walls face along x or along z: an axis-aligned box `size` (x across the face, z out of it)
    /// turned to face `outward` without rotating it (the lips' strips and markers stay axis-aligned)
    static bool AlongX(Vector3 outward) => Mathf.Abs(outward.x) > Mathf.Abs(outward.z);
    static Vector3 Sz(Vector3 size, Vector3 outward) => AlongX(outward) ? new Vector3(size.z, size.y, size.x) : size;
    static Vector3 Sz(Vector3 size, float yaw) => Mathf.RoundToInt(yaw / 90f) % 2 != 0 ? new Vector3(size.z, size.y, size.x) : size;

    // ------------------------------------------------------------ facades (looks, baked)

    /// PUNCHED WINDOWS on a wall face: `at` is the face's centre at its foot, `width` along it, floors from `y0` up
    /// `storeyH` each; bays every 5.5 m with blank piers between, a window 1.6 × 2.4 a bay a floor, a limestone sill,
    /// a quarter of them lit amber (clustered by floor, never a grid), the rest dark panes. 4 cm proud. `seed` picks
    /// which are lit
    public static void PunchedWindows(Vector3 at, Vector3 outward, float width, float y0, int floors, float storeyH, int seed)
    {
        Axes(outward, out var right, out var turn);
        int bays = Mathf.Max(1, Mathf.FloorToInt(width / 5.5f));
        float pitch = width / bays;
        for (int f = 0; f < floors; f++)
        {
            float y = y0 + f * storeyH + storeyH * 0.5f;
            float litRow = CityTextures.Hash01(seed, f, 41) < 0.45f ? 0.55f : 0.12f;   // a lit floor, or a dark one with a window or two
            for (int b = 0; b < bays; b++)
            {
                Vector3 c = at + right * (-width * 0.5f + pitch * (b + 0.5f)) + Vector3.up * y + outward * 0.02f;
                bool lit = CityTextures.Hash01(seed, f * 31 + b, 42) < litRow;
                Bake(lit ? WindowAmber : Pane, c, new Vector3(1.6f, 2.4f, 0.04f), turn);
                Bake(Limestone, c + Vector3.down * 1.3f + outward * 0.08f, new Vector3(1.9f, 0.12f, 0.2f), turn);   // the sill
                Bake(Limestone, c + Vector3.up * 1.32f + outward * 0.05f, new Vector3(1.9f, 0.16f, 0.14f), turn);   // the lintel
            }
        }
    }

    /// A STRING COURSE: a limestone band across a face at `y`, 0.22 m proud
    public static void StringCourse(Vector3 at, Vector3 outward, float width, float y, float tall = 0.3f)
    {
        Axes(outward, out _, out var turn);
        Bake(Limestone, at + Vector3.up * y + outward * 0.11f, new Vector3(width, tall, 0.22f), turn);
    }

    /// A stepped CORNICE along the top of a face at `y` (the roof line): three steps, each deeper, 0.35 m at most
    public static void Cornice(Vector3 at, Vector3 outward, float width, float y)
    {
        Axes(outward, out _, out var turn);
        Bake(Limestone, at + Vector3.up * (y - 0.9f) + outward * 0.07f, new Vector3(width, 0.3f, 0.14f), turn);
        Bake(Limestone, at + Vector3.up * (y - 0.55f) + outward * 0.12f, new Vector3(width + 0.1f, 0.3f, 0.24f), turn);
        Bake(Limestone, at + Vector3.up * (y - 0.18f) + outward * 0.17f, new Vector3(width + 0.2f, 0.36f, 0.34f), turn);
    }

    /// A SHOPFRONT on the ground floor: a band of dark glass between brass mullions under a brass fascia carrying the
    /// name, lit from a lamp strip under the fascia; the glass is a look (the wall behind it is the solid)
    public static void Shopfront(Transform parent, Vector3 at, Vector3 outward, float width, string name, float height = 3.6f, bool pane = true)
    {
        Axes(outward, out var right, out var turn);
        Bake(Limestone, at + Vector3.up * 0.15f + outward * 0.08f, new Vector3(width, 0.3f, 0.16f), turn);   // the stall riser
        if (pane)   // (not over a real window, nor a door)
        {
            Bake(Pane, at + Vector3.up * (height * 0.5f + 0.3f) + outward * 0.03f, new Vector3(width - 0.4f, height - 0.6f, 0.05f), turn);
            int mullions = Mathf.Max(2, Mathf.RoundToInt(width / 3f));
            for (int i = 0; i <= mullions; i++)
                Bake(Brass, at + right * (-width * 0.5f + 0.2f + (width - 0.4f) * i / mullions) + Vector3.up * (height * 0.5f + 0.3f) + outward * 0.07f, new Vector3(0.1f, height - 0.6f, 0.12f), turn);
        }
        Bake(Brass, at + Vector3.up * (height + 0.55f) + outward * 0.1f, new Vector3(width, 0.9f, 0.2f), turn);   // the fascia
        Bake(LampCream, at + Vector3.up * (height + 0.08f) + outward * 0.22f, new Vector3(width - 0.6f, 0.06f, 0.06f), turn);
        if (!string.IsNullOrEmpty(name))
            Text(parent, at + Vector3.up * (height + 0.55f) + outward * 0.22f, Quaternion.LookRotation(-outward), name, 0.55f, new Color(0.08f, 0.07f, 0.05f), "ShopName", width - 1f, true);
    }

    /// An AWNING over a shopfront: oxblood canvas leaning out 20° from the wall at `y`, `depth` out — SOLID: a slide
    /// off a window above, a catch under one, a lip from the street (its outer edge is 0.6 m lower than its root)
    public static GameObject Awning(Transform parent, Vector3 at, Vector3 outward, float width, float y = 3.3f, float depth = 2.2f)
    {
        Axes(outward, out var right, out var turn);
        Quaternion rot = turn * Quaternion.Euler(20f, 0f, 0f);
        var canvas = Slab(parent, at + Vector3.up * y + rot * Vector3.forward * (depth * 0.5f), new Vector3(width, 0.1f, depth), Canvas, "Awning");
        canvas.transform.rotation = rot;
        for (int s = -1; s <= 1; s += 2)   // the arms under its edges
            Bake(Bronze, at + right * (s * (width * 0.5f - 0.15f)) + Vector3.up * (y - 0.1f) + rot * Vector3.forward * (depth * 0.5f), new Vector3(0.08f, 0.08f, depth - 0.2f), rot);
        return canvas;
    }

    /// A MARQUEE over the pavement: a solid canopy at `y`, `depth` out, its fascia lit cream with bulb lips along its
    /// edges, brass chains up to the face, the name on the fascia. A platform (reach it from a lamp standard's kick or
    /// a window above) and a slide when the builder pitches it (`pitch` degrees down toward the street)
    public static GameObject Marquee(Transform parent, Vector3 at, Vector3 outward, float width, string name, float y = 5.2f, float depth = 5.4f, float pitch = 0f)
    {
        Axes(outward, out var right, out var turn);
        Quaternion rot = turn * Quaternion.Euler(pitch, 0f, 0f);
        var canopy = Slab(parent, at + Vector3.up * y + rot * Vector3.forward * (depth * 0.5f), new Vector3(width, 0.35f, depth), Charcoal, "Marquee");
        canopy.transform.rotation = rot;
        Vector3 edge = at + Vector3.up * y + rot * Vector3.forward * (depth - 0.1f);
        Bake(LampCream, edge + Vector3.down * 0.05f, new Vector3(width, 0.9f, 0.16f), rot);                           // the fascia, lit
        Bake(LampCream, edge + Vector3.up * 0.5f, new Vector3(width + 0.2f, 0.12f, 0.12f), rot);                      // the bulb lips
        Bake(LampCream, edge + Vector3.down * 0.62f, new Vector3(width + 0.2f, 0.12f, 0.12f), rot);
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 foot = at + right * (s * (width * 0.5f - 0.4f)) + Vector3.up * (y + 0.2f) + rot * Vector3.forward * (depth - 0.6f);
            Vector3 top = at + right * (s * (width * 0.5f - 0.4f)) + Vector3.up * (y + 4.2f) + outward * 0.1f;
            Vector3 d = top - foot;
            Bake(Brass, (foot + top) * 0.5f, new Vector3(0.1f, 0.1f, d.magnitude), Quaternion.LookRotation(d.normalized, Vector3.up));
        }
        if (!string.IsNullOrEmpty(name))
            Text(parent, edge + outward * 0.1f, Quaternion.LookRotation(-(rot * Vector3.forward)), name, 0.6f, new Color(0.1f, 0.08f, 0.05f), "MarqueeName", width - 1f, true);
        var l = Light(parent, edge + Vector3.down * 1.5f - outward * 1f, 16f, 1.4f);
        l.color = LampCreamLight;
        return canopy;
    }

    /// A BLADE SIGN standing off a face at `at`, reading `word` down it (block letters, lit cream, in a brass frame)
    public static void BladeSign(Transform parent, Vector3 at, Vector3 outward, string word, float letter = 1.2f)
    {
        Axes(outward, out var right, out var turn);
        float h = word.Length * letter + 0.6f, w = letter + 0.5f;
        Vector3 c = at + outward * (w * 0.5f + 0.1f) + Vector3.down * (h * 0.5f);
        Bake(Brass, c, new Vector3(0.26f, h, w), turn);
        Bake(Bronze, at + outward * 0.2f, new Vector3(0.3f, 0.3f, 0.4f), turn);
        for (int i = 0; i < word.Length; i++)
            for (int s = -1; s <= 1; s += 2)
                Text(parent, c + right * (s * 0.16f) + Vector3.up * (h * 0.5f - 0.5f - i * letter), Quaternion.LookRotation(right * s), word[i].ToString(), letter * 0.8f, LampCreamLight, "BladeLetter", w - 0.2f, true);
    }

    // ------------------------------------------------------------ street furniture (solid)

    /// A LAMP STANDARD: a fluted iron post 6 m tall (a kick post, its corners authored) with a brass arm and a cream
    /// globe; a warm light every other one (`lit`)
    public static GameObject LampStandard(Transform parent, Vector3 foot, float height = 6f, bool lit = true)
    {
        var post = Pillar(parent, foot + Vector3.up * (height * 0.5f), new Vector3(0.3f, height, 0.3f), RivetIron);
        post.name = "LampStandard";
        Bake(RivetIron, foot + Vector3.up * 0.3f, new Vector3(0.7f, 0.6f, 0.7f));
        Bake(Brass, foot + Vector3.up * (height - 0.2f), new Vector3(0.12f, 0.12f, 1f));
        Bake(LampCream, foot + Vector3.up * (height + 0.25f), new Vector3(0.6f, 0.6f, 0.6f), null, PrimitiveType.Sphere);
        if (lit) { var l = Light(parent, foot + Vector3.up * (height - 0.4f), 16f, 1.3f); l.color = LampCreamLight; }
        return post;
    }

    /// A junction's mark: a brass compass rose inlaid in the paving, its long ray pointing the route's way. Decor
    public static void JunctionInlay(Transform parent, Vector3 centre, float radius, Vector3 forward)
    {
        forward.y = 0f;
        if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward;
        Quaternion rot = Quaternion.LookRotation(forward.normalized, Vector3.up);
        Prim(parent, centre + Vector3.up * MarkProud, new Vector3(radius * 2f, 0.006f, radius * 2f), Brass, "BrassInlay", PrimitiveType.Cylinder);
        Prim(parent, centre + Vector3.up * (MarkProud + 0.004f), new Vector3(radius * 1.7f, 0.006f, radius * 1.7f), Pavement, "InlayField", PrimitiveType.Cylinder);
        for (int i = 0; i < 4; i++)
        {
            float len = i == 0 ? radius * 1.6f : radius * 0.9f;
            Prim(parent, centre + Vector3.up * (MarkProud + 0.009f) + rot * Quaternion.Euler(0f, i * 90f, 0f) * Vector3.forward * (len * 0.5f), new Vector3(0.14f, 0.006f, len), Brass, "BrassInlay", PrimitiveType.Cube, rot * Quaternion.Euler(0f, i * 90f, 0f));
        }
    }
}
