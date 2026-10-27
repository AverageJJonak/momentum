using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// TEXTURED QUADS, WELDED. MeshBaker welds the kit's primitives, and a primitive
/// carries its own 0..1 UVs — fine for a flat colour, useless for anything that
/// has to TILE: a facade forty windows wide, a street six hundred metres long, a
/// cloud deck five kilometres across. This is the other half: hand it quads with
/// the UVs you want (in windows, in metres, in tiles) and Flush welds every quad
/// sharing a material into one mesh. A whole city's lit windows are then one
/// draw per window style, and because they are a TEXTURE they are mip-mapped —
/// a facade a kilometre off melts into an even glow instead of shimmering.
/// Normals are written per face, so the same quads serve lit walls.
/// </summary>
public class SheetBaker
{
    class Group
    {
        public readonly List<Vector3> verts = new List<Vector3>();
        public readonly List<Vector3> normals = new List<Vector3>();
        public readonly List<Vector2> uvs = new List<Vector2>();
        public readonly List<int> tris = new List<int>();
    }

    readonly Dictionary<Material, Group> groups = new Dictionary<Material, Group>();

    public int Quads { get; private set; }

    /// THE LEAN (the city's forced perspective, MenuCity.LeanOn): while this is set, every corner of every quad is
    /// pushed `Shear` metres sideways per metre it stands over `ShearBase`, so a whole building leans as one. Zero
    /// (the default, and whatever a level builds under) leaves every quad where it was put
    public static Vector3 Shear;
    public static float ShearBase;

    static Vector3 Leaned(Vector3 p)
    {
        float dy = p.y - ShearBase;
        return dy > 0f ? p + Shear * dy : p;
    }

    /// One quad. Corners in order bottom-left, top-left, top-right, bottom-right AS SEEN FROM ITS FRONT (that is
    /// clockwise, which is the front in Unity), a UV for each.
    public void Quad(Material mat, Vector3 bl, Vector3 tl, Vector3 tr, Vector3 br, Vector2 uvBL, Vector2 uvTL, Vector2 uvTR, Vector2 uvBR)
    {
        if (mat == null) return;
        if (Shear.x != 0f || Shear.z != 0f) { bl = Leaned(bl); tl = Leaned(tl); tr = Leaned(tr); br = Leaned(br); }
        if (!groups.TryGetValue(mat, out var g)) groups[mat] = g = new Group();
        int i = g.verts.Count;
        Vector3 n = Vector3.Cross(tl - bl, tr - bl).normalized;
        g.verts.Add(bl); g.verts.Add(tl); g.verts.Add(tr); g.verts.Add(br);
        g.normals.Add(n); g.normals.Add(n); g.normals.Add(n); g.normals.Add(n);
        g.uvs.Add(uvBL); g.uvs.Add(uvTL); g.uvs.Add(uvTR); g.uvs.Add(uvBR);
        g.tris.Add(i); g.tris.Add(i + 1); g.tris.Add(i + 2);
        g.tris.Add(i); g.tris.Add(i + 2); g.tris.Add(i + 3);
        Quads++;
    }

    /// The same, with a plain UV rectangle: `uv0` at the bottom-left corner, `uv1` at the top-right
    public void Quad(Material mat, Vector3 bl, Vector3 tl, Vector3 tr, Vector3 br, Vector2 uv0, Vector2 uv1)
        => Quad(mat, bl, tl, tr, br, uv0, new Vector2(uv0.x, uv1.y), uv1, new Vector2(uv1.x, uv0.y));

    /// An upright rectangle facing `normal` (horizontal, unit length), `centre` at its middle
    public void Wall(Material mat, Vector3 centre, Vector3 normal, float width, float height, Vector2 uv0, Vector2 uv1)
    {
        Vector3 r = Vector3.Cross(normal, Vector3.up) * (width * 0.5f);   // the viewer's right, standing in front of it
        Vector3 u = Vector3.up * (height * 0.5f);
        Quad(mat, centre - r - u, centre - r + u, centre + r + u, centre + r - u, uv0, uv1);
    }

    /// A flat rectangle facing up, its long axis on z
    public void Flat(Material mat, Vector3 centre, float sizeX, float sizeZ, Vector2 uv0, Vector2 uv1)
    {
        Vector3 x = Vector3.right * (sizeX * 0.5f), z = Vector3.forward * (sizeZ * 0.5f);
        Quad(mat, centre - x - z, centre - x + z, centre + x + z, centre + x - z, uv0, uv1);
    }

    /// A flat strip from `a` to `b`, `width` across, facing up: u runs ACROSS it (0..1), v ALONG it (v0 at a, v1 at b)
    public void Strip(Material mat, Vector3 a, Vector3 b, float width, float v0, float v1, float u0 = 0f, float u1 = 1f)
    {
        Vector3 d = b - a;
        d.y = 0f;
        if (d.sqrMagnitude < 1e-4f) return;
        Vector3 side = Vector3.Cross(Vector3.up, d.normalized) * (width * 0.5f);
        Quad(mat, a - side, b - side, b + side, a + side, new Vector2(u0, v0), new Vector2(u0, v1), new Vector2(u1, v1), new Vector2(u1, v0));
    }

    /// A flat sheet cut into a grid (fog is worked out per corner by some shaders: one quad five kilometres wide
    /// would carry four fog values). UVs run in TILES of `tile` metres from the world origin, so sheets line up
    public void Grid(Material mat, Vector3 centre, float sizeX, float sizeZ, int cellsX, int cellsZ, float tile)
    {
        float x0 = centre.x - sizeX * 0.5f, z0 = centre.z - sizeZ * 0.5f, dx = sizeX / cellsX, dz = sizeZ / cellsZ;
        for (int i = 0; i < cellsX; i++)
            for (int j = 0; j < cellsZ; j++)
            {
                float xa = x0 + i * dx, xb = xa + dx, za = z0 + j * dz, zb = za + dz;
                Quad(mat, new Vector3(xa, centre.y, za), new Vector3(xa, centre.y, zb), new Vector3(xb, centre.y, zb), new Vector3(xb, centre.y, za),
                    new Vector2(xa / tile, za / tile), new Vector2(xb / tile, zb / tile));
            }
    }

    /// A flat ring (or a disc, with `r0` zero) round `centre`, facing up, cut into sectors and bands; UVs in tiles
    /// of `tile` metres from the world origin
    public void Ring(Material mat, Vector3 centre, float r0, float r1, int sectors, int bands, float tile)
    {
        for (int b = 0; b < bands; b++)
        {
            float ra = Mathf.Lerp(r0, r1, b / (float)bands), rb = Mathf.Lerp(r0, r1, (b + 1) / (float)bands);
            for (int s = 0; s < sectors; s++)
            {
                float a0 = s * Mathf.PI * 2f / sectors, a1 = (s + 1) * Mathf.PI * 2f / sectors;
                Vector3 d0 = new Vector3(Mathf.Sin(a0), 0f, Mathf.Cos(a0)), d1 = new Vector3(Mathf.Sin(a1), 0f, Mathf.Cos(a1));
                Vector3 p0 = centre + d0 * ra, p1 = centre + d0 * rb, p2 = centre + d1 * rb, p3 = centre + d1 * ra;
                Quad(mat, p0, p1, p2, p3, new Vector2(p0.x, p0.z) / tile, new Vector2(p1.x, p1.z) / tile, new Vector2(p2.x, p2.z) / tile, new Vector2(p3.x, p3.z) / tile);
            }
        }
    }

    /// Weld and empty: one child of `parent` per material. Returns what it made.
    public List<Renderer> Flush(Transform parent, string name)
    {
        var made = new List<Renderer>();
        foreach (var pair in groups)
        {
            var g = pair.Value;
            if (g.verts.Count == 0) continue;
            var mesh = new Mesh { name = name + "Mesh", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(g.verts);
            mesh.SetNormals(g.normals);
            mesh.SetUVs(0, g.uvs);
            mesh.SetTriangles(g.tris, 0);
            mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = pair.Key;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off;
            made.Add(r);
        }
        groups.Clear();
        Quads = 0;
        return made;
    }
}
