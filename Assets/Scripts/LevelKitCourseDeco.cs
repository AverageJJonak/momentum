using UnityEngine;

/// <summary>
/// THE COURSE KIT, the Deco pieces: banisters (brass slides), chandeliers (a grapple orb in a ring of lamps hung on a
/// chain), arches (the thresholds: fluted piers and a stepped lintel with a sunburst, never a door), stair nosings,
/// stepped plinths, pad shelves. Same rules as LevelKitCourse: solids are Slab/Solid, marks are Prim, nothing moves.
/// THE POSTER CUT (2026-10-16): every ornament here is fewer and bigger — the chandelier six big globes (eight when
/// LevelKit.Ornament is high) with no drops, the arch's sunburst five fat rays (seven when high) and no flute strips
/// on the piers (the fluting is in the stone), the plinth's figure as it was. The nosings keep their function.
/// </summary>
public static partial class LevelKit
{
    // ------------------------------------------------------------ banisters and stairs

    /// A BANISTER: a narrow brass ramp beside a stair, its top running from (z0, y0) to (z1, y1) at lateral `x`. A
    /// slide: crouch onto it and it carries you; hop off its foot for the slide-jump. Named for the lit edge.
    public static GameObject Banister(Transform parent, float x, float width, float z0, float y0, float z1, float y1)
    {
        var b = Ramp(parent, x, width, z0, y0, z1, y1, Brass);
        b.name = "BanisterRamp";
        return b;
    }

    /// Stair NOSINGS: brass lines across a ramp's surface every `step` metres, so a slope reads as a sweeping stair.
    /// (The ramp stays a ramp: the main line runs it at eight metres a second.)
    public static void Nosings(Transform parent, float x, float width, float z0, float y0, float z1, float y1, float step = 1.6f)
    {
        if (z1 < z0) { float t = z0; z0 = z1; z1 = t; t = y0; y0 = y1; y1 = t; }   // as Ramp: the ends either way round
        float dz = z1 - z0, dy = y1 - y0, len = Mathf.Sqrt(dz * dz + dy * dy);
        if (len < step * 2f) return;
        Quaternion rot = Quaternion.Euler(Mathf.Atan2(-dy, dz) * Mathf.Rad2Deg, 0f, 0f);
        Vector3 up = rot * Vector3.up;
        int n = Mathf.FloorToInt(len / step);
        for (int i = 1; i < n; i++)
        {
            float t = i / (float)n;
            Vector3 at = new Vector3(x, Mathf.Lerp(y0, y1, t), Mathf.Lerp(z0, z1, t)) + up * (MarkProud + 0.004f);
            Prim(parent, at, new Vector3(width - 0.2f, 0.008f, 0.09f), Brass, "Nosing", PrimitiveType.Cube, rot);
        }
    }

    /// A stepped PLINTH: tiers of solid stone, each a lip on the one below (a Deco fountain base, a statue's foot)
    public static void SteppedPlinth(Transform parent, Vector3 groundCenter, float baseSize, int tiers = 2, float tierHeight = 1.2f)
    {
        for (int i = 0; i < tiers; i++)
        {
            float size = baseSize - i * baseSize * 0.34f;
            var c = groundCenter + Vector3.up * (tierHeight * (i + 0.5f));
            if (i == 0) Slab(parent, c, new Vector3(size, tierHeight, size), Limestone, "PlinthTier");
            else Lip(parent, c, new Vector3(size, tierHeight, size), Limestone, "PlinthLedge");
        }
        // The figure: a bronze stepped abstraction (look only)
        float top = tierHeight * tiers;
        float fig = Mathf.Max(1.6f, baseSize * 0.2f);
        Prim(parent, groundCenter + Vector3.up * (top + fig * 0.9f), new Vector3(fig * 0.55f, fig * 1.8f, fig * 0.55f), Bronze, "StatueBody", PrimitiveType.Cube, null, true);
        Prim(parent, groundCenter + Vector3.up * (top + fig * 2.05f), new Vector3(fig * 0.9f, fig * 0.5f, fig * 0.9f), Bronze, "StatueCrown", PrimitiveType.Cube, Quaternion.Euler(0f, 45f, 0f), true);
    }

    // ------------------------------------------------------------ the chandelier

    /// A CHANDELIER: the grapple orb (GrappleDrone) at `pos`, hung from `ceilingY` on a chain, in a brass ring of
    /// lamp-cream globes with a warm light in it. The orb is the target; the rest is look.
    public static void Chandelier(Transform parent, Vector3 pos, float ceilingY, float radius = 2.4f)
    {
        Orb(parent, pos);
        float drop = ceilingY - pos.y;
        if (drop > 0.5f)
            Prim(parent, pos + Vector3.up * (drop * 0.5f + 0.6f), new Vector3(0.08f, drop - 1.2f, 0.08f), Bronze, "ChandelierChain", PrimitiveType.Cylinder);
        Prim(parent, pos + Vector3.up * 0.9f, new Vector3(1.1f, 0.5f, 1.1f), Brass, "ChandelierCrown", PrimitiveType.Cylinder);
        int n = Ornate(0.6f) ? 8 : 6;   // fewer, bigger globes: the yellow burst of the casino painting
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            Vector3 arm = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            Quaternion rot = Quaternion.LookRotation(arm.normalized, Vector3.up);
            Prim(parent, pos + arm * 0.5f + Vector3.down * 0.3f, new Vector3(0.08f, 0.08f, radius), Brass, "ChandelierArm", PrimitiveType.Cube, rot);
            Prim(parent, pos + arm + Vector3.down * 0.05f, new Vector3(0.6f, 0.6f, 0.6f), LampCream, "ChandelierGlobe", PrimitiveType.Sphere);
        }
        var l = Light(parent, pos + Vector3.down * 0.3f, 26f, 2.4f);
        l.color = new Color(1f, 0.86f, 0.62f);
    }

    /// A globe PENDANT lamp: look and light, hung from the ceiling
    public static void GlobeLamp(Transform parent, Vector3 pos, float ceilingY)
    {
        float drop = ceilingY - pos.y;
        if (drop > 0.4f) Prim(parent, pos + Vector3.up * (drop * 0.5f + 0.3f), new Vector3(0.05f, drop - 0.6f, 0.05f), Bronze, "PendantChain", PrimitiveType.Cylinder);
        Prim(parent, pos, new Vector3(0.7f, 0.7f, 0.7f), LampCream, "PendantGlobe", PrimitiveType.Sphere);
        Prim(parent, pos + Vector3.up * 0.4f, new Vector3(0.3f, 0.16f, 0.3f), Brass, "PendantCap", PrimitiveType.Cylinder);
        var l = Light(parent, pos, 14f, 1.6f);
        l.color = new Color(1f, 0.88f, 0.66f);
    }

    // ------------------------------------------------------------ thresholds

    /// An ARCH over the lane at plane `z`: two solid fluted piers just inside the walls (the lane keeps its width less
    /// a metre each side), a stepped lintel at the clear height (never under eight metres where the room allows it),
    /// a sunburst of brass rays under it. A threshold, never a door: nothing closes, nothing counts.
    public static void Arch(Transform parent, float halfW, float y, float height, float z)
    {
        float clear = Mathf.Min(height - 1.1f, Mathf.Max(8f, height * 0.8f));
        if (clear < 4f) clear = height - 0.6f;
        float pierW = 0.9f;
        for (int s = -1; s <= 1; s += 2)
        {
            float x = s * (halfW - pierW * 0.5f - 0.1f);
            var pier = Slab(parent, new Vector3(x, y + clear * 0.5f, z), new Vector3(pierW, clear, pierW), Fluted, "ArchPier");
            TechMarkers.MarkBoxCorners(pier);
            Prim(parent, new Vector3(x, y + clear - 0.25f, z), new Vector3(pierW + 0.3f, 0.3f, pierW + 0.3f), Brass, "PierCapital");
        }
        float span = halfW * 2f;
        Slab(parent, new Vector3(0f, y + clear + 0.4f, z), new Vector3(span + 0.2f, 0.8f, pierW), Limestone, "ArchLintel");
        Prim(parent, new Vector3(0f, y + clear + 1.0f, z), new Vector3(span * 0.8f, 0.4f, pierW - 0.2f), Limestone, "ArchStep");
        Prim(parent, new Vector3(0f, y + clear + 1.35f, z), new Vector3(span * 0.55f, 0.3f, pierW - 0.4f), Limestone, "ArchStep");
        // The sunburst: a few fat rays fanning from the lintel's centre, under it, on the south face
        int rays = Ornate(0.6f) ? 7 : 5;
        for (int i = 0; i < rays; i++)
        {
            float deg = Mathf.Lerp(-64f, 64f, i / (rays - 1f));
            Quaternion rot = Quaternion.Euler(0f, 0f, deg);
            Vector3 dir = rot * Vector3.up;
            Prim(parent, new Vector3(0f, y + clear - 0.05f, z - pierW * 0.5f - 0.02f) - dir * 0.95f, new Vector3(0.14f, 1.7f, 0.03f), Brass, "SunburstRay", PrimitiveType.Cube, rot);
        }
        Prim(parent, new Vector3(0f, y + clear - 0.05f, z - pierW * 0.5f - 0.02f), new Vector3(0.6f, 0.6f, 0.03f), Brass, "SunburstHub", PrimitiveType.Sphere);
    }

    // ------------------------------------------------------------ shelves and pads

    /// A pad SHELF: a flat solid ledge cut into the side of a slope with a jump pad on it, so a pad on a chute sits
    /// flush with something level (a pad on a 30° slope would stand proud of it)
    public static void PadShelf(Transform parent, Vector3 top, float size, float up, float forward)
    {
        Slab(parent, top + Vector3.down * 0.6f, new Vector3(size, 1.2f, size), Limestone, "ShelfLedge");
        Physics.SyncTransforms();
        JumpPad(parent, top, 0f, up, forward);
    }
}
