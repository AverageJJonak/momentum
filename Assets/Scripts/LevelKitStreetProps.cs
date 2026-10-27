using UnityEngine;

/// <summary>
/// THE STREET KIT, the furniture: FIRE ESCAPES (zigzag iron flights on an alley wall, their landings lips, the bottom
/// flight a jump off the ground), DUMPSTERS and CRATES (lips), KIOSKS (a solid box under an awning, its roof a lip),
/// FOUNTAINS (a basin, a stepped plinth, water), HYDRANTS, BENCHES, washing lines (looks). Everything you can run into
/// is solid; nothing on a wall stands more than 0.35 m proud but the fire escape, which is in an alley on purpose
/// (the alley's 3 m lane is a tech line, district-grammar). THE POSTER CUT (2026-10-16): hydrants, crate stacks and
/// dumpsters at HALF density (Sparse: every other one by where it stands, deterministic; LevelKit.Ornament opens it
/// up again), the fire escape without its wall brackets, the hydrant a stub and a cap. Lamp standards (kick posts),
/// fire escapes (lips), kiosks, fountains and washing lines as they were.
/// </summary>
public static partial class LevelKit
{
    /// Half density, deterministic: is this prop one of the ones that stays? (hashed on where it stands, so a level is
    /// the same every build; at the default knob half stay, at 0.7 and over all of them)
    static bool Sparse(Vector3 at)
        => MenuArt.Hash01(Mathf.RoundToInt(at.x * 2f), Mathf.RoundToInt(at.z * 2f), 77) >= Mathf.Clamp01(0.5f * Ornament / 0.35f);

    /// A FIRE ESCAPE on a wall: `foot` is the wall's point at street level under it, `outward` the way off the wall,
    /// `floors` landings `storeyH` apart (the first at storeyH), each a 7 × 1.3 m iron lip with a rail, flights of 30°
    /// zigzagging between them, the bottom flight hanging to 1.5 m over the street (jump it; corner-boost its landing).
    /// Returns the landings' heights. A ladder up the alley: wall-kick the wall it hangs on
    public static float[] FireEscape(Transform parent, Vector3 foot, Vector3 outward, int floors, float storeyH, float width = 7f)
    {
        Axes(outward, out var right, out var turn);
        const float depth = 1.3f;
        float deg = 30f, flightW = 1.1f;
        var heights = new float[floors];
        Vector3 centre = foot + outward * (depth * 0.5f + 0.05f);
        for (int f = 0; f < floors; f++)
        {
            float y = storeyH * (f + 1);
            heights[f] = y;
            Lip(parent, centre + Vector3.up * (y - 0.1f), Sz(new Vector3(width, 0.2f, depth), outward), RivetIron, "EscapeLanding");
            Bake(RivetIron, centre + Vector3.up * (y + 0.45f) + outward * (depth * 0.5f - 0.05f), new Vector3(width, 0.06f, 0.06f), turn);   // the rail
            for (int p = 0; p <= 3; p++) Bake(RivetIron, centre + right * (-width * 0.5f + width * p / 3f) + Vector3.up * (y + 0.2f) + outward * (depth * 0.5f - 0.05f), new Vector3(0.05f, 0.9f, 0.05f), turn);
        }
        // The flights: each from one landing's end up to the next landing's other end, the bottom one down to 1.5 m
        float run = Mathf.Min(width - flightW - 0.4f, storeyH / Mathf.Tan(deg * Mathf.Deg2Rad));
        for (int f = -1; f < floors - 1; f++)
        {
            float yA = f < 0 ? 1.5f : storeyH * (f + 1), yB = storeyH * (f + 2);
            if (f < 0) { yB = storeyH; run = Mathf.Min(run, (yB - yA) / Mathf.Tan(deg * Mathf.Deg2Rad)); }
            int side = (f + 1) % 2 == 0 ? -1 : 1;   // alternate ends
            Vector3 a = centre + right * (side * (width * 0.5f - flightW * 0.5f - 0.2f)) + Vector3.up * yA;
            Vector3 b = a - right * (side * run) + Vector3.up * (yB - yA);
            Vector3 d = b - a;
            var flight = Slab(parent, (a + b) * 0.5f + Vector3.down * 0.08f, new Vector3(flightW, 0.14f, d.magnitude), RivetIron, "EscapeFlight");
            flight.transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
            run = Mathf.Min(width - flightW - 0.4f, storeyH / Mathf.Tan(deg * Mathf.Deg2Rad));
        }
        Flute(parent, foot + Vector3.up * (storeyH * floors * 0.5f), outward, storeyH * floors - 1f, 0.5f);   // the wall it hangs on is a kick wall
        return heights;
    }

    /// A DUMPSTER: a riveted iron bin 1.4 m tall, a lip (brass along its rim: corner-boost it in the alley). Every other
    /// one stays (Sparse); null when this one does not
    public static GameObject Dumpster(Transform parent, Vector3 foot, float yaw = 0f)
    {
        if (Sparse(foot)) return null;
        // No lid: the charcoal lid sat over the lip's brass strips and was unreadable itself (2026-10-16); the bin's own
        // top, with its brass edges, is the surface you land on
        return Lip(parent, foot + Vector3.up * 0.7f, Sz(new Vector3(2.4f, 1.4f, 1.5f), yaw), RivetIron, "Dumpster");
    }

    /// A stack of CRATES against a wall: two lips, the top one 1.8 m up (a corner boost on a corner boost). Every other
    /// stack stays (Sparse)
    public static void Crates(Transform parent, Vector3 foot, float yaw = 0f)
    {
        if (Sparse(foot)) return;
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Lip(parent, foot + Vector3.up * 0.5f, Sz(new Vector3(2.2f, 1f, 1.2f), yaw), WoodPanel, "Crate");
        Lip(parent, foot + rot * new Vector3(-0.4f, 0f, 0f) + Vector3.up * 1.4f, Sz(new Vector3(1.2f, 0.8f, 1f), yaw), WoodPanel, "Crate");
    }

    /// A newspaper KIOSK on a plaza: a solid oxblood box 3 × 3.2 × 3 under an awning, its flat roof a lip, a lamp on it
    public static GameObject Kiosk(Transform parent, Vector3 foot, Vector3 facing)
    {
        Axes(facing, out _, out var turn);
        var box = Pillar(parent, foot + Vector3.up * 1.5f, new Vector3(3f, 3f, 3f), Oxblood);
        box.name = "Kiosk";
        Lip(parent, foot + Vector3.up * 3.2f, new Vector3(3.4f, 0.4f, 3.4f), Limestone, "KioskRoof");
        Bake(Pane, foot + facing.normalized * 1.52f + Vector3.up * 1.7f, new Vector3(2.2f, 1.2f, 0.04f), turn);
        Bake(Brass, foot + facing.normalized * 1.53f + Vector3.up * 0.9f, new Vector3(2.6f, 0.3f, 0.06f), turn);
        Awning(parent, foot + facing.normalized * 1.5f, facing, 3.2f, 2.5f, 1.4f);
        Bake(LampCream, foot + Vector3.up * 3.7f, new Vector3(0.4f, 0.4f, 0.4f), null, PrimitiveType.Sphere);
        return box;
    }

    /// A FOUNTAIN: a limestone basin 8 m across, a two-tier plinth with a bronze figure on it (the tiers are lips),
    /// black water lying in the basin, a light in it
    public static void Fountain(Transform parent, Vector3 centre, float dia = 8f)
    {
        Solid(parent, PrimitiveType.Cylinder, centre + Vector3.up * 0.3f, Quaternion.identity, new Vector3(dia, 0.6f, dia), Limestone, "FountainBasin");
        Prim(parent, centre + Vector3.up * (0.6f + MarkProud), new Vector3(dia - 0.8f, 0.01f, dia - 0.8f), WaterSurface(new Color(0.02f, 0.02f, 0.025f, 0.86f)), "FountainWater", PrimitiveType.Cylinder);
        SteppedPlinth(parent, centre + Vector3.up * 0.6f, dia * 0.42f, 2, 1.1f);
        var l = Light(parent, centre + Vector3.up * 3.6f, 14f, 1.2f);
        l.color = LampCreamLight;
    }

    /// A fire HYDRANT: a brass stub 0.8 m tall under a cap, solid (walk-into-able), never a lip. Every other one stays (Sparse)
    public static void Hydrant(Transform parent, Vector3 foot)
    {
        if (Sparse(foot)) return;
        Slab(parent, foot + Vector3.up * 0.4f, new Vector3(0.34f, 0.8f, 0.34f), Brass, "Hydrant");
        Bake(Brass, foot + Vector3.up * 0.85f, new Vector3(0.26f, 0.14f, 0.26f), null, PrimitiveType.Sphere);
    }

    /// A WASHING LINE across an alley between two walls at `a` and `b` (looks: a cord and sheets)
    public static void WashingLine(Transform parent, Vector3 a, Vector3 b, int sheets = 3)
    {
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 2f) return;
        Quaternion rot = Quaternion.LookRotation(d / len, Vector3.up);
        Bake(Bronze, (a + b) * 0.5f, new Vector3(0.03f, 0.03f, len), rot);
        for (int i = 0; i < sheets; i++)
        {
            float t = (i + 0.5f) / sheets;
            Vector3 c = Vector3.Lerp(a, b, t) + Vector3.down * 0.55f;
            Bake(i % 2 == 0 ? Cream : Canvas, c, new Vector3(0.04f, 1.1f, Mathf.Min(1.4f, len / sheets - 0.4f)), rot);
        }
    }

    /// A PAVEMENT: a limestone kerb slab 0.12 m over the carriageway, from `min` to `max` on the street's floor `y`
    public static void PavementSlab(Transform parent, float x0, float x1, float z0, float z1, float y)
    {
        if (x1 - x0 < 0.2f || z1 - z0 < 0.2f) return;
        Slab(parent, new Vector3((x0 + x1) * 0.5f, y + 0.06f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, 0.12f, z1 - z0), Pavement, "Pavement");
    }
}
