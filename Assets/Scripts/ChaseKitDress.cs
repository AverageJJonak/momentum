using UnityEngine;

/// <summary>
/// THE CHASE'S DRESSING, by kind. THE HOUSE (the Concierge): the Atrium's look flying past — cream walls over a black
/// marble dado with a brass hairline, fluted pilasters, a brass cornice, a gallery each side at six metres behind
/// brass-and-glass rails (the fast line's height), chandeliers overhead (each one a whip orb in a ring of globes),
/// the company's mark inlaid in the floor, its taglines stencilled on the walls. THE TERMINUS (the Stationmaster):
/// cream glazed tile with the oxblood band and the brass handrail, mosaic platform names, globe cages, cream ribs
/// under the vault, a footbridge over the track, the track itself with its amber third rail. And the FINISH PLATFORM:
/// a wide floor past the buffers; when it is the early one its far wall holds the doors (the House's lifts, the
/// tunnel mouth) that carry you to the platform at the buffers where the level's tail stands.
/// </summary>
public static partial class LevelKit
{
    /// The walls (down past the catch lanes), the ceiling, the dado and pilasters, the cornice or the ribs
    public static void ChaseShell(Transform parent, ChaseKind k, float y, float height, float z0, float z1)
    {
        float len = z1 - z0, zm = (z0 + z1) * 0.5f, hw = Chase.HalfW;
        float wallH = height + Chase.WallBelow, wallY = y - Chase.WallBelow + wallH * 0.5f;
        for (int s = -1; s <= 1; s += 2)
        {
            Slab(parent, new Vector3(s * (hw + 0.5f), wallY, zm), new Vector3(1f, wallH, len), ChaseWallMat(k), "ChaseWall");
            for (float z = z0 + 6f; z < z1; z += 12f)
                Prim(parent, new Vector3(s * (hw - 0.03f), y + height * 0.5f + 0.5f, z), new Vector3(0.06f, height - 1.4f, 0.9f), Fluted, "Pilaster");
            if (k == ChaseKind.Concierge)
            {
                Prim(parent, new Vector3(s * (hw - 0.02f), y + 0.6f, zm), new Vector3(0.04f, 1.2f, len - 0.1f), Marble, "Dado");
                Prim(parent, new Vector3(s * (hw - 0.03f), y + 1.22f, zm), new Vector3(0.02f, 0.04f, len - 0.1f), Brass, "DadoLine");
                Prim(parent, new Vector3(s * (hw - 0.15f), y + height - 0.5f, zm), new Vector3(0.3f, 0.25f, len - 0.1f), Brass, "Cornice");
            }
        }
        Slab(parent, new Vector3(0f, y + height + 0.5f, zm), new Vector3(hw * 2f + 2f, 1f, len), Ceil, "ChaseCeiling");
        if (k == ChaseKind.Stationmaster)
        {
            TileBand(parent, hw, y, z0 + 0.2f, z1 - 0.2f);
            for (float z = z0 + 12f; z < z1; z += 24f)
                Beam(parent, new Vector3(-hw + 0.5f, y + height - 0.5f, z), new Vector3(hw - 0.5f, y + height - 0.5f, z), 0.7f, 0.6f, Cream);
        }
    }

    /// The House's galleries at six metres each side (brass-and-glass rails, bronze brackets); the terminus's
    /// footbridge over the track at the chunk's middle (a lip on its near edge)
    public static void ChaseGallery(Transform parent, ChaseKind k, float y, float z0, float z1, int index)
    {
        float len = z1 - z0, zm = (z0 + z1) * 0.5f, hw = Chase.HalfW;
        if (k == ChaseKind.Concierge)
        {
            const float G = 6f;
            for (int s = -1; s <= 1; s += 2)
            {
                Slab(parent, new Vector3(s * (hw - 1.4f), y + G - 0.5f, zm), new Vector3(3.2f, 1f, len), Limestone, "Gallery");
                GlassRail(parent, new Vector3(s * (hw - 3.05f), y + G, z0 + 0.3f), new Vector3(s * (hw - 3.05f), y + G, z1 - 0.3f));
                Prim(parent, new Vector3(s * (hw - 2.95f), y + G + MarkProud, zm), new Vector3(0.1f, 0.006f, len - 0.2f), Brass, "BrassInlay");
                for (float z = z0 + 8f; z < z1; z += 16f)
                    Prim(parent, new Vector3(s * (hw - 1.1f), y + G - 1.2f, z), new Vector3(1.8f, 0.3f, 0.4f), Bronze, "GalleryBracket");
            }
            return;
        }
        const float B = 4.6f;
        float bz = zm;
        Slab(parent, new Vector3(0f, y + B - 0.25f, bz), new Vector3(hw * 2f + 0.4f, 0.5f, 2.6f), Limestone, "Footbridge");
        GlassRail(parent, new Vector3(-hw + 0.3f, y + B, bz + 1.2f), new Vector3(hw - 0.3f, y + B, bz + 1.2f));
        Prim(parent, new Vector3(0f, y + B + MarkProud + 0.012f, bz - 1.21f), new Vector3(hw * 2f - 0.2f, 0.024f, 0.14f), Brass, "LipBrass");
        ChaseMarkLip(parent, new Vector3(0f, y + B, bz - 0.9f), new Vector3(hw * 2f, 0.9f, 1.8f));
        for (int s = -1; s <= 1; s += 2) Flute(parent, new Vector3(s * 4f, y + B - 0.25f, bz - 1.3f), Vector3.back, 0.4f, 0.8f);
    }

    /// The light: the House's chandelier-lit hall (warm points high up, globe pendants under the galleries); the
    /// terminus's globe cages and its floods
    public static void ChaseLamps(Transform parent, ChaseKind k, float y, float height, float z0, float z1, int index)
    {
        int side = (index & 1) == 0 ? -1 : 1;
        for (float z = z0 + 12f; z < z1; z += 24f) Light(parent, new Vector3(0f, y + height - 3f, z), 34f, 1.7f);
        if (k == ChaseKind.Concierge)
        {
            GlobeLamp(parent, new Vector3(side * (Chase.HalfW - 1.4f), y + 3.8f, z0 + 16f), y + 5f);
            GlobeLamp(parent, new Vector3(-side * (Chase.HalfW - 1.4f), y + 3.8f, z0 + 32f), y + 5f);
            return;
        }
        GlobeCage(parent, new Vector3(side * 4.5f, y + 7f, z0 + 10f), y + height);
        GlobeCage(parent, new Vector3(-side * 4.5f, y + 7f, z0 + 38f), y + height);
    }

    /// The House's mark in the floor and a tagline on the wall; the terminus's mosaic name and its warning
    public static void ChaseMark(Transform parent, ChaseKind k, float y, float z0, int index)
    {
        int side = (index & 1) == 0 ? -1 : 1;
        float hw = Chase.HalfW;
        if (k == ChaseKind.Concierge)
        {
            HouseKit.FloorMark(parent, new Vector3(side * 5.3f, y, z0 + 4f), 3.2f, Vector3.forward);
            Stencil(parent, new Vector3(-side * (hw - 0.1f), y + 2.4f, z0 + 20f), Vector3.right * side, Taglines[index % Taglines.Length], 0.09f, 8f);
            return;
        }
        Mosaic(parent, new Vector3(-hw, y + 2.75f, z0 + 12f), Vector3.right, "TERMINUS  ·  PLATFORM 10", 7f, 1.1f);
        Stencil(parent, new Vector3(hw - 0.1f, y + 2.4f, z0 + 30f), Vector3.left, index % 2 == 0 ? "NOTHING IS LATE BUT YOU" : "THE TRAINS RUN ON TIME", 0.09f, 8f);
    }

    /// THE FINISH PLATFORM at `z`: the platforms run eight metres to the buffers, then one wide floor; the shell, the
    /// light, the mark. `doors`: the far wall with the doors in it (the early finish: ChaseCourse warps you through)
    public static void ChaseFinishPlatform(Transform parent, ChaseKind k, MovementConfig config, float y, float z, bool doors)
    {
        float z1 = z + Chase.FinishLen, height = Chase.Height(k), drop = Chase.ChanDrop(k), hw = Chase.HalfW;
        const float Buffers = 8f;
        ChaseFloor(parent, k, y, drop, z, z + Buffers);
        ChaseRail(parent, k, y - drop, z, z + Buffers - 1.2f);
        Slab(parent, new Vector3(0f, y - drop + 0.7f, z + Buffers - 1f), new Vector3(2.4f, 1.4f, 0.8f), RivetIron, "BufferStop");
        Prim(parent, new Vector3(0f, y - drop + 1.6f, z + Buffers - 1f), Vector3.one * 0.3f, SignalRed, "BufferLamp", PrimitiveType.Sphere);
        Slab(parent, new Vector3(0f, y - 0.5f, (z + Buffers + z1) * 0.5f), new Vector3(hw * 2f + 1f, 1f, z1 - z - Buffers), Floor, "FinishFloor");
        ChaseShell(parent, k, y, height, z, z1);
        ChaseLamps(parent, k, y, height, z, z1, 0);
        if (k == ChaseKind.Concierge) HouseKit.FloorMark(parent, new Vector3(0f, y, z + 22f), 6f, Vector3.forward);
        else Chevrons(parent, new Vector3(0f, y, z + 12f), Vector3.forward, 3);
        Sign(parent, new Vector3(0f, y + 7f, z1 - 0.2f), Vector3.back, 10f, 1.8f, k == ChaseKind.Concierge ? "CHECKED OUT" : "ALL CHANGE");
        if (!doors) return;
        // The far wall and the doors in it: lifts for the House, the tunnel mouth for the terminus, black behind
        float ow = 3.2f, oh = 4.5f;
        for (int s = -1; s <= 1; s += 2)
            Slab(parent, new Vector3(s * (ow + (hw + 1f - ow) * 0.5f), y + height * 0.5f, z1 + 0.5f), new Vector3(hw + 1f - ow, height, 1f), ChaseWallMat(k), "FinishWall");
        Slab(parent, new Vector3(0f, y + oh + (height - oh) * 0.5f, z1 + 0.5f), new Vector3(ow * 2f, height - oh, 1f), ChaseWallMat(k), "FinishLintel");
        Prim(parent, new Vector3(0f, y + oh * 0.5f, z1 + 0.98f), new Vector3(ow * 2f, oh, 0.02f), Graphite, "DoorBlind");
        if (k == ChaseKind.Concierge)
        {
            LiftDoors(parent, new Vector3(-1.6f, y + 2.3f, z1 + 0.92f), Vector3.back, "P");
            LiftDoors(parent, new Vector3(1.6f, y + 2.3f, z1 + 0.92f), Vector3.back, "P");
        }
        else TunnelPortal(parent, new Vector3(0f, y, z1 + 0.5f), Vector3.back, ow * 2f, oh);
        var l = Light(parent, new Vector3(0f, y + 4f, z1 - 2f), 12f, 1.6f);
        l.color = new Color(1f, 0.8f, 0.5f);
    }
}
