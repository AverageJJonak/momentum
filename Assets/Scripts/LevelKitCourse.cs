using UnityEngine;

/// <summary>
/// THE COURSE KIT (2026-10-09, course-grammar): the pieces a course space is drawn with, in the language the bible
/// fixes. A LIP you corner-boost on wears a BRASS strip along its top edge; a KICK WALL and an EDGE-BOOST CORNER wear
/// CREAM FLUTES; a fast line is traced on the floor by HARD LINE's lit hairline in the zone's light; a banister is a
/// brass slide; a chandelier is a grapple orb in a ring of lamps; a threshold is an arch, never a door; a checkpoint
/// is a brass inlay across the lane. State, never instructions. Everything you can run into is solid (Slab/Solid);
/// every mark is decor with no collider (Prim/Decor), a few millimetres proud of the face it sits on so nothing
/// fights it for the plane (coplanar rule). Nothing here moves, so the weld takes all of it.
/// </summary>
public static partial class LevelKit
{
    /// How far a mark stands off the face it sits on (the inlay rule: 3 mm, never on the plane)
    public const float MarkProud = 0.003f;

    // ------------------------------------------------------------ lips, kick walls, corners

    /// A LIP: a low solid block (0.9–1.8 m tall against its approach) whose walkable top is a corner boost. Its top's
    /// long edges carry a brass strip, and a LipMarker trigger sits along the top so the tech is authored, not guessed.
    /// `size.x` runs across the lane, `size.z` along it, unless `alongX` is false (then the brass runs along z).
    public static GameObject Lip(Transform parent, Vector3 center, Vector3 size, Material mat = null, string name = "LipLedge")
    {
        var lip = Slab(parent, center, size, mat ?? Wall, name);
        float top = center.y + size.y * 0.5f;
        bool wide = size.x >= size.z;
        float run = wide ? size.x : size.z, across = wide ? size.z : size.x;
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 strip = wide ? new Vector3(center.x, top + MarkProud + 0.012f, center.z + s * (across * 0.5f - 0.09f))
                                 : new Vector3(center.x + s * (across * 0.5f - 0.09f), top + MarkProud + 0.012f, center.z);
            Prim(parent, strip, wide ? new Vector3(run - 0.08f, 0.024f, 0.14f) : new Vector3(0.14f, 0.024f, run - 0.08f), Brass, "LipBrass");
        }
        TechMarkers.MarkLip(new Vector3(center.x, top, center.z), new Vector3(size.x + 0.6f, 0.9f, size.z + 0.6f));
        return lip;
    }

    /// A PARAPET: a lip along a roof's edge (the same block, named for the roof's own lit edge)
    public static GameObject Parapet(Transform parent, Vector3 center, Vector3 size) => Lip(parent, center, size, Limestone, "ParapetLedge");

    /// A KICK WALL: a solid upright slab 3–6 m tall with a cream flute down the middle of each broad face
    public static GameObject KickWall(Transform parent, Vector3 center, Vector3 size, Material mat = null)
    {
        size.y = Mathf.Clamp(size.y, 3f, 6f);
        var wall = Slab(parent, center, size, mat ?? Wall, "KickWall");
        bool broadX = size.x >= size.z;   // the broad faces look along z when the wall runs along x
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 at = broadX ? new Vector3(center.x, center.y, center.z + s * (size.z * 0.5f + 0.02f))
                                : new Vector3(center.x + s * (size.x * 0.5f + 0.02f), center.y, center.z);
            Prim(parent, at, broadX ? new Vector3(0.7f, size.y - 0.3f, 0.04f) : new Vector3(0.04f, size.y - 0.3f, 0.7f), Fluted, "KickFlute");
        }
        return wall;
    }

    /// An EDGE-BOOST CORNER: a real 90° outside corner at least 4 m tall (a pillar, a wall end), its four vertical
    /// edges authored (CornerMarker) and each dressed with a cream flute on both faces
    public static GameObject BoostCorner(Transform parent, Vector3 center, Vector3 size, Material mat = null)
    {
        size.y = Mathf.Max(size.y, 4f);
        var p = Pillar(parent, center, size, mat ?? Wall);
        p.name = "BoostPillar";
        Flutes(parent, center, size);
        return p;
    }

    /// Cream flutes on both faces of every vertical edge of an upright box (the corner's own mark)
    public static void Flutes(Transform parent, Vector3 center, Vector3 size)
    {
        float h = size.y - 0.3f;
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                // The flute on the x-facing face, hugging the edge; and the one on the z-facing face
                Prim(parent, new Vector3(center.x + sx * (size.x * 0.5f + 0.02f), center.y, center.z + sz * (size.z * 0.5f - 0.2f)), new Vector3(0.04f, h, 0.32f), Fluted, "CornerFlute");
                Prim(parent, new Vector3(center.x + sx * (size.x * 0.5f - 0.2f), center.y, center.z + sz * (size.z * 0.5f + 0.02f)), new Vector3(0.32f, h, 0.04f), Fluted, "CornerFlute");
            }
    }

    /// A cream flute band on a wall face: `at` is the point on the wall, `facing` the way into the room
    public static void Flute(Transform parent, Vector3 at, Vector3 facing, float height, float width = 0.6f)
    {
        bool alongX = Mathf.Abs(facing.z) > Mathf.Abs(facing.x);
        Vector3 pos = at + facing.normalized * 0.02f;
        Prim(parent, pos, alongX ? new Vector3(width, height, 0.04f) : new Vector3(0.04f, height, width), Fluted, "WallFlute");
    }

    /// A KICK STACK: a chimney you kick off mid-gap on a roof: a solid brick block with a stone cap, 3–6 m tall
    public static GameObject KickStack(Transform parent, Vector3 foot, float height, float size = 1.8f)
    {
        height = Mathf.Clamp(height, 3f, 6f);
        var stack = Slab(parent, foot + Vector3.up * height * 0.5f, new Vector3(size, height, size), Brick, "KickStack");
        Prim(parent, foot + Vector3.up * (height + 0.1f), new Vector3(size + 0.3f, 0.2f, size + 0.3f), Limestone, "StackCap");
        TechMarkers.MarkBoxCorners(stack);
        return stack;
    }

    // ------------------------------------------------------------ lines and marks

    /// HARD LINE on a fast line: a lit hairline in the zone's light laid on the floor from `a` to `b` (both on the
    /// surface it is drawn on), a centimetre proud. Reads from thirty metres: the line is where the movement is
    public static void LineRun(Transform parent, Vector3 a, Vector3 b, Zone zone, float width = 0.07f)
    {
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 0.3f) return;
        Quaternion rot = Quaternion.LookRotation(d / len, Vector3.up);
        Prim(parent, (a + b) * 0.5f + Vector3.up * 0.012f, new Vector3(width, 0.012f, len), LineFor(zone), "HardLineRun", PrimitiveType.Cube, rot);
    }

    /// A brass inlay strip on a floor from `a` to `b` (a checkpoint's line, a banister's trace), 3 mm proud
    public static void BrassStrip(Transform parent, Vector3 a, Vector3 b, float width = 0.16f)
    {
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 0.3f) return;
        Quaternion rot = Quaternion.LookRotation(d / len, Vector3.up);
        Prim(parent, (a + b) * 0.5f + Vector3.up * MarkProud, new Vector3(width, 0.006f, len), Brass, "BrassInlay", PrimitiveType.Cube, rot);
    }

    /// A checkpoint's mark: a brass line across the lane at `z` on floor `y`, a chevron pair pointing on. Decor.
    public static void CheckpointInlay(Transform parent, float halfW, float y, float z)
    {
        BrassStrip(parent, new Vector3(-halfW + 0.3f, y, z), new Vector3(halfW - 0.3f, y, z), 0.2f);
        for (int s = -1; s <= 1; s += 2)
        {
            Prim(parent, new Vector3(s * 0.6f, y + MarkProud, z + 0.9f), new Vector3(0.08f, 0.006f, 1.5f), Brass, "BrassInlay", PrimitiveType.Cube, Quaternion.Euler(0f, s * 35f, 0f));
        }
    }
}
