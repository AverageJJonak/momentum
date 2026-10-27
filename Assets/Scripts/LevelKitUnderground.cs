using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE UNDERGROUND's kit (Act II, 2026-10-12, act2-underground): Halcyon Railways' metro and what lies beneath it.
/// The station vocabulary — the oxblood tile band at 1.2 m with its brass rail, mosaic station names, incandescent
/// globes in brass cages, brass turnstile banks (LIPS: a metre, a perfect corner boost), ticket windows, escalators
/// (slides down, a driven slope up, brass balustrades), benches, roundels, lever frames, signal gantries (the posts are
/// kick walls, the beam a lip), the cable tray (a lip line along a tunnel wall), refuge niches, weirs (a lip on a slide),
/// brick tunnel portals — and THE HAZARDS, every one telegraphed, every one RED where it is dangerous, none of them a
/// kill: they hurt and they THROW, and every throw goes through the motor's attributed paths (Launch / AddImpulse).
///   THIRD RAIL    a live rail along every track: touch it = 25 and a throw up and away; it arcs and hums
///   TRAIN         through a platform or a tunnel on a cycle: 3 s of headlamps on the wall, the horn, the hum rising,
///                 then it comes through at 20 m/s: 40 and a throw along its travel. Recesses and the tray are safe
///   STEAM VENT    a 2 s hiss (amber), a 0.4 s burst (red): a launch 12 m/s up — a timed pad — 15 if you stood in it
///   WATERFALL     pushes you down: the drop you choose
///   SLUICE GATE   open 3 s in 8: a wall when it is shut, a shove if it shuts on you
///   COLLAPSING SLAB  drops 0.4 s after you land on it, back after 6 s: a line you cannot stop on
///   WEIR          a slide with a lip (geometry: no behaviour)
///   FAN UPDRAFT   lifts you (FactoryLife's updraft, under a bigger fan)
/// Each hazard is its own MonoBehaviour (below), IWeldDynamic where anything moves or swaps a material, so the weld
/// leaves those parts alone. Nothing here allocates per frame. Colliders only on what you stand on or run into.
/// </summary>
public static partial class LevelKit
{
    public static readonly Color TileCream = new Color(0.82f, 0.76f, 0.62f);

    static Material ugRef, ugTile, ugTileOx, ugBrassRail, ugRailCover, ugArc, ugHeadlamp, ugSignalRed, ugSignalAmber, ugLampOff, ugCarCream, ugCarOx, ugCarDark, ugCarGlass, ugWaterFall, ugDial, ugSteamBurstMat;

    /// The Underground's materials belong to the level: they are rebuilt whenever the palette is (Init makes a new works.wall)
    static void EnsureUnderground()
    {
        if (ugRef != null && ugRef == works.wall) return;
        EnsureFactory();
        ugRef = works.wall;
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        ugTile = Surface(TileCream, 0.85f, Surf.Tile, 0.6f, 0.7f);
        ugTileOx = Surface(OxbloodRed, 0.6f, Surf.Tile, 0.6f, 0.7f);
        ugBrassRail = Lit(lit, BrassGold, 0.75f); ugBrassRail.SetFloat("_Metallic", 0.9f);
        ugRailCover = Lit(lit, new Color(0.5f, 0.09f, 0.07f), 0.35f);       // the third rail's cover board: danger red, painted
        ugArc = Unlit(unlit, new Color(3.2f, 2.9f, 2.4f));                  // the arc: amber-white, the one flicker down here
        ugHeadlamp = Unlit(unlit, new Color(3f, 2.7f, 2.1f));               // a train's headlamps: cream
        ugSignalRed = Unlit(unlit, new Color(2.6f, 0.22f, 0.14f));          // signal lamps: red
        ugSignalAmber = Unlit(unlit, new Color(2.4f, 1.3f, 0.3f));          // and amber: the only two colours a signal shows
        ugLampOff = Unlit(unlit, new Color(0.22f, 0.2f, 0.19f));            // a lamp, dark
        ugCarCream = Lit(lit, new Color(0.8f, 0.74f, 0.6f), 0.6f);          // the livery: cream over oxblood with a brass waist
        ugCarOx = Lit(lit, OxbloodRed, 0.55f);
        ugCarDark = Lit(lit, new Color(0.08f, 0.08f, 0.09f), 0.5f); ugCarDark.SetFloat("_Metallic", 0.6f);
        ugCarGlass = Lit(lit, new Color(0.05f, 0.05f, 0.06f), 0.95f);
        ugWaterFall = WaterSurface(new Color(0.07f, 0.075f, 0.085f, 0.72f), 0.9f, 2f, 0.6f);
        ugDial = Lit(lit, new Color(0.09f, 0.09f, 0.1f), 0.6f);
    }

    public static Material Tile { get { EnsureUnderground(); return ugTile; } }
    public static Material OxbloodTile { get { EnsureUnderground(); return ugTileOx; } }
    public static Material BlackWater { get { EnsureWorks(); return blackWaterMat; } }
    public static Material SignalRed { get { EnsureUnderground(); return ugSignalRed; } }
    public static Material SignalAmber { get { EnsureUnderground(); return ugSignalAmber; } }

    // ------------------------------------------------------------ the station vocabulary

    /// The tile band: an oxblood skirting at the floor, the oxblood band at 1.2 m between two brass hairlines, and the
    /// brass handrail at a metre on brackets, along both side walls of a lane from `z0` to `z1` (decor, one mesh a side)
    public static void TileBand(Transform parent, float halfW, float y, float z0, float z1, bool skipW = false, bool skipE = false)
    {
        EnsureUnderground();
        float len = z1 - z0, mid = (z0 + z1) * 0.5f;
        if (len < 2f) return;
        for (int s = -1; s <= 1; s += 2)
        {
            if (s < 0 && skipW || s > 0 && skipE) continue;
            float x = s * halfW;
            var b = new MeshBaker();
            b.Add(ugTileOx, new Vector3(x - s * 0.03f, y + 0.15f, mid), new Vector3(0.03f, 0.3f, len));
            b.Add(ugTileOx, new Vector3(x - s * 0.03f, y + 1.23f, mid), new Vector3(0.03f, 0.36f, len));
            b.Add(ugBrassRail, new Vector3(x - s * 0.04f, y + 1.42f, mid), new Vector3(0.04f, 0.02f, len));
            b.Add(ugBrassRail, new Vector3(x - s * 0.04f, y + 1.04f, mid), new Vector3(0.04f, 0.02f, len));
            b.Add(ugBrassRail, new Vector3(x - s * 0.11f, y + 0.98f, mid), new Vector3(0.06f, 0.06f, len), Quaternion.Euler(90f, 0f, 0f), PrimitiveType.Cylinder);
            for (float z = z0 + 1.2f; z < z1 - 0.6f; z += 2.4f)
                b.Add(ugBrassRail, new Vector3(x - s * 0.06f, y + 0.98f, z), new Vector3(0.1f, 0.04f, 0.04f));
            b.Flush(parent, "TileBand");
        }
    }

    /// A mosaic station name: a cream tile plate with an oxblood border and the name in oxblood, `w` wide, on a wall
    public static void Mosaic(Transform parent, Vector3 wallPoint, Vector3 facing, string name, float w = 6f, float h = 1.1f)
    {
        EnsureUnderground();
        Vector3 n = facing.normalized;
        Quaternion rot = Quaternion.LookRotation(n);
        Vector3 right = rot * Vector3.right;
        Decor(parent, PrimitiveType.Cube, wallPoint + n * 0.035f, rot, new Vector3(w, h, 0.05f), ugTile, "MosaicPlate");
        Decor(parent, PrimitiveType.Cube, wallPoint + n * 0.045f + Vector3.up * (h * 0.5f - 0.06f), rot, new Vector3(w, 0.12f, 0.03f), ugTileOx, "MosaicBorder");
        Decor(parent, PrimitiveType.Cube, wallPoint + n * 0.045f - Vector3.up * (h * 0.5f - 0.06f), rot, new Vector3(w, 0.12f, 0.03f), ugTileOx, "MosaicBorder");
        Decor(parent, PrimitiveType.Cube, wallPoint + n * 0.045f + right * (w * 0.5f - 0.06f), rot, new Vector3(0.12f, h, 0.03f), ugTileOx, "MosaicBorder");
        Decor(parent, PrimitiveType.Cube, wallPoint + n * 0.045f - right * (w * 0.5f - 0.06f), rot, new Vector3(0.12f, h, 0.03f), ugTileOx, "MosaicBorder");
        Text(parent, wallPoint + n * 0.07f, Quaternion.LookRotation(-n), name, 0.11f, new Color(0.38f, 0.09f, 0.09f), "Mosaic", w - 0.7f, true, 80f, 160f);
    }

    /// An incandescent globe in a brass cage, hung from the ceiling on a rod (decor + a warm light)
    public static void GlobeCage(Transform parent, Vector3 pos, float ceilingY)
    {
        EnsureUnderground();
        float rod = Mathf.Max(0.2f, ceilingY - pos.y - 0.45f);
        Decor(parent, PrimitiveType.Cylinder, pos + Vector3.up * (0.45f + rod * 0.5f), Quaternion.identity, new Vector3(0.05f, rod * 0.5f, 0.05f), ugBrassRail, "GlobeRod");
        Decor(parent, PrimitiveType.Cylinder, pos + Vector3.up * 0.38f, Quaternion.identity, new Vector3(0.62f, 0.02f, 0.62f), ugBrassRail, "CageRing");
        Decor(parent, PrimitiveType.Cylinder, pos - Vector3.up * 0.34f, Quaternion.identity, new Vector3(0.5f, 0.02f, 0.5f), ugBrassRail, "CageRing");
        for (int i = 0; i < 4; i++)
        {
            Quaternion r = Quaternion.Euler(0f, i * 90f + 45f, 0f);
            Decor(parent, PrimitiveType.Cube, pos + r * new Vector3(0.3f, 0f, 0f), r, new Vector3(0.025f, 0.74f, 0.025f), ugBrassRail, "CageRib");
        }
        Decor(parent, PrimitiveType.Sphere, pos, Quaternion.identity, Vector3.one * 0.5f, LampCream, "Globe");
        var l = Light(parent, pos - Vector3.up * 0.2f, 15f, 1.7f);
        l.color = LampCreamLight;
    }

    /// A globe on a brass wall bracket (decor + light)
    public static void WallGlobe(Transform parent, Vector3 wallPoint, Vector3 facing)
    {
        EnsureUnderground();
        Vector3 n = facing.normalized;
        Quaternion rot = Quaternion.LookRotation(n);
        Decor(parent, PrimitiveType.Cube, wallPoint + n * 0.21f, rot, new Vector3(0.06f, 0.06f, 0.4f), ugBrassRail, "GlobeArm");
        Decor(parent, PrimitiveType.Cylinder, wallPoint + n * 0.4f + Vector3.up * 0.2f, Quaternion.identity, new Vector3(0.44f, 0.015f, 0.44f), ugBrassRail, "CageRing");
        Decor(parent, PrimitiveType.Sphere, wallPoint + n * 0.4f, Quaternion.identity, Vector3.one * 0.36f, LampCream, "Globe");
        var l = Light(parent, wallPoint + n * 0.7f, 10f, 1.3f);
        l.color = LampCreamLight;
    }

    /// A bank of brass turnstiles across a gate line: `count` drums a metre high with their tripod arms and the brass
    /// guide rails between them — the whole bank is one solid LIP a metre high (hop it; a perfect corner boost on it)
    public static void TurnstileBank(Transform parent, Vector3 groundCentre, float yaw, int count = 3)
    {
        EnsureUnderground();
        count = Mathf.Clamp(count, 1, 6);
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Vector3 right = rot * Vector3.right, fwd = rot * Vector3.forward;
        float pitch = 1.2f, w = count * pitch;
        for (int i = 0; i < count; i++)
        {
            Vector3 at = groundCentre + right * ((i + 0.5f) * pitch - w * 0.5f);
            Solid(parent, PrimitiveType.Cylinder, at + Vector3.up * 0.5f, rot, new Vector3(0.5f, 0.5f, 0.5f), ugBrassRail, "Turnstile");
            Decor(parent, PrimitiveType.Cylinder, at + Vector3.up * 1.01f, rot, new Vector3(0.54f, 0.015f, 0.54f), Cream, "TurnstilePlate");
            for (int k = 0; k < 3; k++)
            {
                Quaternion arm = rot * Quaternion.Euler(k * 120f, 0f, 0f);
                Decor(parent, PrimitiveType.Cylinder, at + Vector3.up * 0.95f + arm * new Vector3(0f, 0.3f, 0f), arm, new Vector3(0.04f, 0.3f, 0.04f), ugBrassRail, "TurnstileArm");
            }
            if (i < count - 1)
                Solid(parent, PrimitiveType.Cube, at + right * (pitch * 0.5f) + Vector3.up * 0.5f, rot, new Vector3(0.08f, 1f, 0.9f), ugBrassRail, "TurnstileRail");
        }
        for (int s = -1; s <= 1; s += 2)
            Solid(parent, PrimitiveType.Cylinder, groundCentre + right * (s * (w * 0.5f + 0.1f)) + Vector3.up * 0.55f, rot, new Vector3(0.16f, 0.55f, 0.16f), ugBrassRail, "TurnstilePost");
        Decor(parent, PrimitiveType.Cube, groundCentre + Vector3.up * 1.14f, rot, new Vector3(w + 0.4f, 0.04f, 0.06f), ugBrassRail, "TurnstileTopRail");
        TechMarkers.MarkLip(groundCentre + Vector3.up * 1f, new Vector3(w + 0.6f, 0.9f, 1.5f));
        Stripes(parent, groundCentre + Vector3.up * 0.02f + fwd * 0.7f, new Vector3(w + 0.4f, 0.02f, 0.12f), yaw);
    }

    /// A ticket window in a wall: a brass frame, frosted glass, a walnut counter, TICKETS over it, a lamp inside (decor)
    public static void TicketWindow(Transform parent, Vector3 wallPoint, Vector3 facing)
    {
        EnsureUnderground();
        Vector3 n = facing.normalized;
        Quaternion rot = Quaternion.LookRotation(n);
        Decor(parent, PrimitiveType.Cube, wallPoint + n * 0.07f + Vector3.up * 1.9f, rot, new Vector3(2.4f, 2.4f, 0.12f), ugBrassRail, "TicketFrame");
        Decor(parent, PrimitiveType.Cube, wallPoint + n * 0.1f + Vector3.up * 1.9f, rot, new Vector3(2.1f, 2.1f, 0.06f), FrostGlass, "TicketGlass");
        Decor(parent, PrimitiveType.Cube, wallPoint + n * 0.28f + Vector3.up * 1.02f, rot, new Vector3(2.5f, 0.1f, 0.56f), WoodPanel, "TicketCounter");
        Decor(parent, PrimitiveType.Cube, wallPoint + n * 0.12f + Vector3.up * 1.52f, rot, new Vector3(1.4f, 0.26f, 0.06f), ugBrassRail, "TicketSlot");
        Text(parent, wallPoint + n * 0.14f + Vector3.up * 3.35f, Quaternion.LookRotation(-n), "TICKETS", 0.1f, SignCream, "TicketsText", 2.2f, true, 60f, 120f);
        var l = Light(parent, wallPoint + n * 0.35f + Vector3.up * 2.4f, 7f, 1.2f);
        l.color = LampCreamLight;
    }

    /// An escalator flight from (`z0`, `y0`) to (`z1`, `y1`): the ramp (iron treads, brass nosings), a brass balustrade down
    /// each edge with its lit handrail, comb plates at both ends. `driven`: it carries you toward `z1` at `speed` (the
    /// UP escalator is a slope that runs with you); not driven, a down flight is a slide, its balustrades banister slides
    public static void Escalator(Transform parent, float x, float width, float z0, float y0, float z1, float y1, bool driven, float speed = 4.5f)
    {
        EnsureUnderground();
        Ramp(parent, x, width, z0, y0, z1, y1, Slope);
        Nosings(parent, x, width - 0.6f, z0 + 0.3f, y0, z1 - 0.3f, y1, 0.42f);
        Vector3 a = new Vector3(x, y0, z0), b = new Vector3(x, y1, z1), d = b - a;
        float len = d.magnitude;
        Quaternion along = Quaternion.LookRotation(d / len);
        for (int s = -1; s <= 1; s += 2)
        {
            float bx = x + s * (width * 0.5f - 0.15f);
            Banister(parent, bx, 0.3f, z0, y0 + 0.95f, z1, y1 + 0.95f);
            var rail = Decor(parent, PrimitiveType.Cube, new Vector3(bx, (y0 + y1) * 0.5f + 1.02f, (z0 + z1) * 0.5f), along, new Vector3(0.06f, 0.02f, len - 0.4f), LampCream, "HandrailLight");
            rail.name = "HandrailLight";
        }
        Stripes(parent, new Vector3(x, y0 + 0.02f, z0 - 0.5f), new Vector3(width, 0.02f, 0.5f));
        Stripes(parent, new Vector3(x, y1 + 0.02f, z1 + 0.5f), new Vector3(width, 0.02f, 0.5f));
        if (driven)
        {
            Vector3 normal = along * Vector3.up;
            Life(parent).AddBelt((a + b) * 0.5f + normal * 0.7f, along, new Vector3(width * 0.5f, 1f, len * 0.5f), d / len * speed);
        }
    }

    /// A station bench: walnut slats on brass ends, 0.45 m — a hop, not a lip (solid)
    public static GameObject StationBench(Transform parent, Vector3 groundPos, float yaw)
    {
        EnsureUnderground();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        var seat = Solid(parent, PrimitiveType.Cube, groundPos + Vector3.up * 0.4f, rot, new Vector3(2.4f, 0.1f, 0.5f), WoodPanel, "Bench");
        for (int s = -1; s <= 1; s += 2) Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(s * 1.1f, 0.18f, 0f), rot, new Vector3(0.08f, 0.36f, 0.46f), ugBrassRail, "BenchEnd");
        Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(0f, 0.65f, -0.22f), rot, new Vector3(2.4f, 0.4f, 0.06f), WoodPanel, "BenchBack");
        return seat;
    }

    /// The company's roundel: an oxblood disc, a brass bar across it, the words on the bar (decor)
    public static void Roundel(Transform parent, Vector3 wallPoint, Vector3 facing, string text, float dia = 1.8f)
    {
        EnsureUnderground();
        Vector3 n = facing.normalized;
        Quaternion rot = Quaternion.LookRotation(n);
        Decor(parent, PrimitiveType.Cylinder, wallPoint + n * 0.04f, rot * Quaternion.Euler(90f, 0f, 0f), new Vector3(dia, 0.03f, dia), ugTileOx, "Roundel");
        Decor(parent, PrimitiveType.Cylinder, wallPoint + n * 0.06f, rot * Quaternion.Euler(90f, 0f, 0f), new Vector3(dia * 0.62f, 0.02f, dia * 0.62f), ugTile, "RoundelCore");
        Decor(parent, PrimitiveType.Cube, wallPoint + n * 0.09f, rot, new Vector3(dia * 1.3f, dia * 0.28f, 0.04f), ugBrassRail, "RoundelBar");
        Text(parent, wallPoint + n * 0.12f, Quaternion.LookRotation(-n), text, 0.09f, new Color(0.1f, 0.09f, 0.08f), "RoundelText", dia * 1.15f, true, 60f, 120f);
    }
}

public static partial class LevelKit
{
    // ------------------------------------------------------------ the tunnel vocabulary

    /// A lever frame: a riveted plinth 1.2 m high with a brass top rail (a LIP) and a row of signal levers on it, the
    /// pulled ones red (solid plinth, the levers decor)
    public static void LeverFrame(Transform parent, Vector3 groundPos, float yaw, int levers = 6)
    {
        EnsureUnderground();
        levers = Mathf.Clamp(levers, 2, 12);
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        float w = levers * 0.4f + 0.4f;
        Solid(parent, PrimitiveType.Cube, groundPos + Vector3.up * 0.6f, rot, new Vector3(w, 1.2f, 0.9f), RivetIron, "LeverFrame");
        Decor(parent, PrimitiveType.Cube, groundPos + Vector3.up * 1.22f, rot, new Vector3(w + 0.1f, 0.04f, 0.96f), ugBrassRail, "LeverFrameTop");
        for (int i = 0; i < levers; i++)
        {
            float lx = (i + 0.5f) * 0.4f - levers * 0.2f;
            bool pulled = (i * 7 + 3) % 5 < 2;
            Quaternion lean = rot * Quaternion.Euler(pulled ? -32f : 18f, 0f, 0f);
            Vector3 foot = groundPos + rot * new Vector3(lx, 1.2f, 0f);
            Decor(parent, PrimitiveType.Cylinder, foot + lean * new Vector3(0f, 0.55f, 0f), lean, new Vector3(0.05f, 0.55f, 0.05f), pulled ? ugRailCover : ugBrassRail, "Lever");
            Decor(parent, PrimitiveType.Sphere, foot + lean * new Vector3(0f, 1.1f, 0f), lean, Vector3.one * 0.12f, pulled ? ugSignalRed : Cream, "LeverKnob");
            Decor(parent, PrimitiveType.Cube, groundPos + rot * new Vector3(lx, 0.9f, 0.46f), rot, new Vector3(0.3f, 0.2f, 0.02f), Cream, "LeverPlate");
        }
        TechMarkers.MarkLip(groundPos + Vector3.up * 1.2f, new Vector3(w + 0.6f, 0.9f, 1.5f));
    }

    /// A signal gantry across a lane: two riveted posts (KICK WALLS, 0.9 m square) and a beam at `height` (a LIP) with
    /// two signal heads hung under it, red over amber (solid posts and beam; the heads decor)
    public static void SignalGantry(Transform parent, Vector3 groundCentre, float width, float height, float yaw = 0f)
    {
        EnsureUnderground();
        height = Mathf.Clamp(height, 3.5f, 6.5f);
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Vector3 right = rot * Vector3.right, back = rot * Vector3.back;
        for (int s = -1; s <= 1; s += 2)
        {
            var post = Solid(parent, PrimitiveType.Cube, groundCentre + right * (s * width * 0.5f) + Vector3.up * (height * 0.5f), rot, new Vector3(0.9f, height, 0.9f), RivetIron, "GantryPost");
            TechMarkers.MarkBoxCorners(post);
            Decor(parent, PrimitiveType.Cube, groundCentre + right * (s * width * 0.5f) + Vector3.up * 0.2f, rot, new Vector3(1.2f, 0.4f, 1.2f), Graphite, "GantryFoot");
            Flute(parent, groundCentre + right * (s * (width * 0.5f - 0.45f)) + Vector3.up * (height * 0.5f), -right * s, Mathf.Min(4f, height - 0.5f), 0.5f);
        }
        Solid(parent, PrimitiveType.Cube, groundCentre + Vector3.up * (height + 0.25f), rot, new Vector3(width + 0.9f, 0.5f, 0.7f), Girder, "GantryBeam");
        Decor(parent, PrimitiveType.Cube, groundCentre + Vector3.up * (height + 0.52f), rot, new Vector3(width + 0.9f, 0.03f, 0.14f), Brass, "GantryBrass");
        TechMarkers.MarkLip(groundCentre + Vector3.up * (height + 0.5f), new Vector3(width + 1.4f, 0.9f, 1.3f));
        for (int i = -1; i <= 1; i += 2)
        {
            Vector3 head = groundCentre + right * (i * width * 0.22f) + Vector3.up * (height - 0.75f);
            Decor(parent, PrimitiveType.Cube, head + Vector3.up * 0.75f, rot, new Vector3(0.1f, 0.5f, 0.1f), RivetIron, "SignalDrop");
            Decor(parent, PrimitiveType.Cube, head, rot, new Vector3(0.4f, 1.3f, 0.3f), ugCarDark, "SignalHead");
            Decor(parent, PrimitiveType.Sphere, head + Vector3.up * 0.32f + back * 0.17f, rot, Vector3.one * 0.26f, ugSignalRed, "SignalRed");
            Decor(parent, PrimitiveType.Sphere, head - Vector3.up * 0.32f + back * 0.17f, rot, Vector3.one * 0.26f, ugSignalAmber, "SignalAmber");
        }
        var l = Light(parent, groundCentre + Vector3.up * (height - 0.9f) + back * 0.8f, 9f, 1.1f);
        l.color = new Color(1f, 0.32f, 0.18f);
    }

    /// The cable tray along a tunnel wall from `a` to `b` (both on the wall's inner face, along z, at the tray's height
    /// — 1.3 to 1.5 m up): an iron ledge 0.7 m wide, the cables on it, brackets under it. A LIP line you run along,
    /// out of the third rail's reach and the train's. `side` −1 = the west wall, +1 = the east
    public static void CableTray(Transform parent, Vector3 a, Vector3 b, int side)
    {
        EnsureUnderground();
        float len = Mathf.Abs(b.z - a.z);
        if (len < 2f) return;
        Vector3 mid = (a + b) * 0.5f;
        Lip(parent, new Vector3(mid.x - side * 0.35f, mid.y - 0.1f, mid.z), new Vector3(0.7f, 0.2f, len), RivetIron, "CableTray");
        for (int i = 0; i < 3; i++)
        {
            float cx = a.x - side * (0.15f + i * 0.2f);
            Cable(parent, new Vector3(cx, a.y + 0.06f, Mathf.Min(a.z, b.z) + 0.2f), new Vector3(cx, a.y + 0.06f, Mathf.Max(a.z, b.z) - 0.2f), 0.07f);
        }
        for (float z = Mathf.Min(a.z, b.z) + 1f; z < Mathf.Max(a.z, b.z) - 0.5f; z += 2.5f)
            Decor(parent, PrimitiveType.Cube, new Vector3(a.x - side * 0.3f, a.y - 0.35f, z), Quaternion.identity, new Vector3(0.5f, 0.3f, 0.08f), RivetIron, "TrayBracket");
    }

    /// A refuge niche: two brick pilasters 1.3 m proud of a tunnel wall, a lamp between them, REFUGE over the pocket —
    /// which is out of a passing train's reach (solid pilasters)
    public static void Refuge(Transform parent, Vector3 wallPoint, Vector3 facing)
    {
        EnsureUnderground();
        Vector3 n = facing.normalized;
        Quaternion rot = Quaternion.LookRotation(n);
        Vector3 right = rot * Vector3.right;
        for (int s = -1; s <= 1; s += 2)
        {
            var pier = Slab(parent, wallPoint + n * 0.66f + right * (s * 1.1f) + Vector3.up * 1.6f, new Vector3(0.6f, 3.2f, 1.3f), Brick, "RefugePier");
            pier.transform.rotation = rot;
        }
        Decor(parent, PrimitiveType.Cube, wallPoint + n * 0.66f + Vector3.up * 3.35f, rot, new Vector3(2.8f, 0.3f, 1.3f), Brick, "RefugeLintel");
        Decor(parent, PrimitiveType.Sphere, wallPoint + n * 0.3f + Vector3.up * 2.5f, Quaternion.identity, Vector3.one * 0.3f, LampCream, "RefugeLamp");
        var l = Light(parent, wallPoint + n * 0.6f + Vector3.up * 2.3f, 6f, 1.2f);
        l.color = LampCreamLight;
        Text(parent, wallPoint + n * 1.32f + Vector3.up * 3.35f, Quaternion.LookRotation(-n), "REFUGE", 0.08f, SignCream, "RefugeText", 2.4f, true, 50f, 100f);
        Stripes(parent, wallPoint + n * 0.65f + Vector3.up * 0.02f, new Vector3(1.6f, 0.02f, 1.3f), rot.eulerAngles.y);
    }

    /// A weir across a sewer slide: an iron plate `height` proud of the slope at `surfacePoint` (a LIP on a slide — hit it
    /// sliding and press jump for a corner boost, or ride over it), a cream line along its crest (solid)
    public static GameObject Weir(Transform parent, Vector3 surfacePoint, Vector3 slopeDir, float width, float height = 0.45f)
    {
        EnsureUnderground();
        Vector3 d = slopeDir.normalized;
        Quaternion rot = Quaternion.LookRotation(d, Vector3.up);
        Vector3 normal = rot * Vector3.up;
        var go = Slab(parent, surfacePoint + normal * (height * 0.5f - 0.15f), new Vector3(width, height + 0.3f, 1.2f), RivetIron, "Weir");
        go.transform.rotation = rot;
        Decor(parent, PrimitiveType.Cube, surfacePoint + normal * (height + 0.012f), rot, new Vector3(width - 0.2f, 0.02f, 0.12f), LampCream, "WeirCrest");
        TechMarkers.MarkLip(surfacePoint + normal * height, new Vector3(width + 0.6f, 0.9f, 1.8f));
        return go;
    }

    /// A tunnel portal round a mouth `w` wide and `h` tall in a wall: brick piers, a brick arch, a soot halo (decor —
    /// the hole is the wall's own)
    public static void TunnelPortal(Transform parent, Vector3 groundCentre, Vector3 facing, float w, float h)
    {
        Vector3 n = facing.normalized;
        Quaternion rot = Quaternion.LookRotation(n);
        Vector3 right = rot * Vector3.right;
        BrickPier(parent, groundCentre - right * (w * 0.5f + 0.45f), n, h + 0.4f, 0.9f);
        BrickPier(parent, groundCentre + right * (w * 0.5f + 0.45f), n, h + 0.4f, 0.9f);
        BrickArch(parent, groundCentre - right * (w * 0.5f + 0.45f) + Vector3.up * (h + 0.2f), groundCentre + right * (w * 0.5f + 0.45f) + Vector3.up * (h + 0.2f), n, 1.4f);
        Decor(parent, PrimitiveType.Cube, groundCentre + n * 0.05f + Vector3.up * (h + 2f), rot, new Vector3(w + 2.8f, 1.3f, 0.08f), Soot, "PortalSoot");
    }

    // ------------------------------------------------------------ the hazards

    /// THE THIRD RAIL: a live rail from `a` to `b` on the track bed (beside the running rails) on cream insulators under a
    /// RED cover board, a red lamp every twelve metres, arcs that spit along it, a hum. Touch it: 25 and a throw up and
    /// away (ThirdRailHazard)
    public static ThirdRailHazard ThirdRail(Transform parent, Vector3 a, Vector3 b)
    {
        EnsureUnderground();
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 2f) return null;
        Vector3 dir = d / len, mid = (a + b) * 0.5f;
        Quaternion rot = Quaternion.LookRotation(dir);
        Vector3 right = rot * Vector3.right;
        var bk = new MeshBaker();
        bk.Add(railMat, mid + Vector3.up * 0.32f, new Vector3(0.09f, 0.1f, len), rot);
        bk.Add(ugRailCover, mid + Vector3.up * 0.47f, new Vector3(0.36f, 0.04f, len), rot);
        bk.Add(ugRailCover, mid + right * 0.17f + Vector3.up * 0.38f, new Vector3(0.03f, 0.16f, len), rot);
        for (float s = 0.6f; s < len - 0.3f; s += 1.5f)
        {
            Vector3 p = a + dir * s;
            bk.Add(Cream, p + Vector3.up * 0.15f, new Vector3(0.12f, 0.16f, 0.12f), rot, PrimitiveType.Cylinder);
            bk.Add(RivetIron, p + Vector3.up * 0.04f, new Vector3(0.3f, 0.06f, 0.2f), rot);
        }
        bk.Flush(parent, "ThirdRail");
        for (float s = 4f; s < len - 2f; s += 12f)
        {
            Vector3 p = a + dir * s + right * 0.5f;
            Decor(parent, PrimitiveType.Cylinder, p + Vector3.up * 0.4f, Quaternion.identity, new Vector3(0.06f, 0.4f, 0.06f), RivetIron, "RailLampPost");
            Decor(parent, PrimitiveType.Sphere, p + Vector3.up * 0.85f, Quaternion.identity, Vector3.one * 0.16f, ugSignalRed, "RailLamp");
        }
        var host = new GameObject("ThirdRailHazard");
        host.transform.SetParent(parent);
        host.transform.position = mid;
        var arcs = new Renderer[3];
        for (int i = 0; i < 3; i++)
        {
            Vector3 p = a + dir * (len * (0.2f + 0.3f * i));
            arcs[i] = Decor(host.transform, PrimitiveType.Cube, p + Vector3.up * 0.45f, rot * Quaternion.Euler(0f, 0f, 20f + 35f * i), new Vector3(0.05f, 0.5f, 0.05f), ugArc, "Arc").GetComponent<Renderer>();
            arcs[i].enabled = false;
        }
        var l = Light(host.transform, mid + Vector3.up * 0.9f, 7f, 0f);
        l.color = new Color(1f, 0.92f, 0.75f);
        l.enabled = false;
        var hz = host.AddComponent<ThirdRailHazard>();
        hz.Init(a + Vector3.up * 0.35f, b + Vector3.up * 0.35f, arcs, l);
        return hz;
    }

    /// One tunnel box for a train line: a sealed dark iron box (`za`..`zb`, `xw`..`xe`, `h` tall) with a brick mouth wall
    /// at its inner end round a `mw` × `mh` hole, and the portal on that wall's face. Its outer extents are 5 cm inside
    /// whatever walls it stands against (never two faces on one plane); the floor is the caller's bed
    static void TrainBox(Transform parent, float x, float y, float za, float zb, float xw, float xe, float h, float mw, float mh, bool mouthNorth)
    {
        float xm = (xw + xe) * 0.5f, w = xe - xw, zm = (za + zb) * 0.5f, len = zb - za;
        float backZ = mouthNorth ? za + T * 0.5f : zb - T * 0.5f, mouthZ = mouthNorth ? zb - T * 0.5f : za + T * 0.5f;
        Slab(parent, new Vector3(xm, y + h * 0.5f, backZ), new Vector3(w, h, T), RivetIron, "TunnelBack");
        Slab(parent, new Vector3(xw + T * 0.5f, y + h * 0.5f, zm), new Vector3(T, h, len), RivetIron, "TunnelSide");
        Slab(parent, new Vector3(xe - T * 0.5f, y + h * 0.5f, zm), new Vector3(T, h, len), RivetIron, "TunnelSide");
        Slab(parent, new Vector3(xm, y + h + T * 0.5f, zm), new Vector3(w, T, len), RivetIron, "TunnelRoof");
        float lw = (x - mw * 0.5f) - xw, rw = xe - (x + mw * 0.5f);
        if (lw > 0.05f) Slab(parent, new Vector3(xw + lw * 0.5f, y + h * 0.5f, mouthZ), new Vector3(lw, h, T), Brick, "TunnelFace");
        if (rw > 0.05f) Slab(parent, new Vector3(xe - rw * 0.5f, y + h * 0.5f, mouthZ), new Vector3(rw, h, T), Brick, "TunnelFace");
        Slab(parent, new Vector3(x, y + mh + (h - mh) * 0.5f, mouthZ), new Vector3(mw, h - mh, T), Brick, "TunnelFace");
        Vector3 face = mouthNorth ? Vector3.forward : Vector3.back;
        TunnelPortal(parent, new Vector3(x, y, mouthZ) + face * (T * 0.5f), face, mw, mh);
    }

    /// One car of a Halcyon Railways train, built at the origin facing +z: the oxblood skirt, the cream body, the brass
    /// waist, the dark roof, the windows, the wheels; a CAB at the north end (`northCab`) or the south (`southCab`) or
    /// both — headlamps, red tail lamps and a light ("HeadLightN" / "HeadLightS": the hazard lights the leading one)
    static Transform TrainCar(Transform parent, bool northCab, bool southCab, float carLen)
    {
        var car = new GameObject(northCab || southCab ? "Cab" : "Car").transform;
        car.SetParent(parent);
        const float w = 2.8f;
        Decor(car, PrimitiveType.Cube, new Vector3(0f, 0.75f, 0f), Quaternion.identity, new Vector3(w, 1.1f, carLen), ugCarOx, "CarSkirt");
        Decor(car, PrimitiveType.Cube, new Vector3(0f, 1.95f, 0f), Quaternion.identity, new Vector3(w, 1.3f, carLen), ugCarCream, "CarBody");
        Decor(car, PrimitiveType.Cube, new Vector3(0f, 1.32f, 0f), Quaternion.identity, new Vector3(w + 0.02f, 0.08f, carLen), ugBrassRail, "CarWaist");
        Decor(car, PrimitiveType.Cube, new Vector3(0f, 2.78f, 0f), Quaternion.identity, new Vector3(w - 0.3f, 0.36f, carLen - 0.2f), ugCarDark, "CarRoof");
        for (float z = -carLen * 0.5f + 1.3f; z < carLen * 0.5f - 0.9f; z += 1.6f)
            for (int s = -1; s <= 1; s += 2)
                Decor(car, PrimitiveType.Cube, new Vector3(s * (w * 0.5f + 0.01f), 2.05f, z), Quaternion.identity, new Vector3(0.02f, 0.8f, 1.1f), ugCarGlass, "CarWindow");
        for (int s = -1; s <= 1; s += 2)
            for (int e = -1; e <= 1; e += 2)
                Decor(car, PrimitiveType.Cylinder, new Vector3(s * 1.1f, 0.4f, e * carLen * 0.36f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.8f, 0.14f, 0.8f), ugCarDark, "Wheel");
        for (int e = -1; e <= 1; e += 2) Decor(car, PrimitiveType.Cube, new Vector3(0f, 0.6f, e * (carLen * 0.5f + 0.2f)), Quaternion.identity, new Vector3(0.2f, 0.2f, 0.5f), ugCarDark, "Coupling");
        for (int e = -1; e <= 1; e += 2)
        {
            if (e > 0 ? !northCab : !southCab) continue;
            float z = e * (carLen * 0.5f + 0.03f);
            for (int s = -1; s <= 1; s += 2)
            {
                Decor(car, PrimitiveType.Cube, new Vector3(s * 0.85f, 1f, z), Quaternion.identity, new Vector3(0.3f, 0.3f, 0.06f), ugHeadlamp, "Headlamp");
                Decor(car, PrimitiveType.Sphere, new Vector3(s * 0.85f, 0.55f, z + e * 0.02f), Quaternion.identity, Vector3.one * 0.2f, ugSignalRed, "TailLamp");
            }
            Decor(car, PrimitiveType.Cube, new Vector3(0f, 2.1f, z), Quaternion.identity, new Vector3(1.6f, 0.5f, 0.05f), Cream, "DestinationBoard");
            var l = Light(car, new Vector3(0f, 1.2f, e * (carLen * 0.5f + 0.6f)), 36f, 3.2f);
            l.color = new Color(1f, 0.93f, 0.8f);
            l.gameObject.name = e > 0 ? "HeadLightN" : "HeadLightS";
        }
        return car;
    }

    /// A TRAIN LINE along z at `x`, its bed at `y`, from `z0` to `z1`: the track, a sealed dark box at each end (`boxLen`
    /// long, `xw`..`xe` wide, `height` tall — the tunnel the train waits in and runs into) with a brick portal and a signal
    /// lamp, a headlamp light inside each mouth, the horn, the hum — and the train, `cars` cars of cream over oxblood,
    /// through the visible run at `speed` every `period` seconds, back the other way the cycle after (TrainHazard). The
    /// box must hold the train: `boxLen` ≥ 9 × cars + 2. `northbound`: the first run leaves the south box
    public static TrainHazard TrainLine(Transform parent, float x, float y, float z0, float z1, float xw, float xe, float boxLen, float height, float period, float phase, int cars = 3, float speed = 20f, bool northbound = true)
    {
        EnsureUnderground();
        const float mouthW = 4.6f, mouthH = 4.8f, carLen = 8.4f, pitch = 9.1f;
        cars = Mathf.Clamp(cars, 1, 4);
        boxLen = Mathf.Max(boxLen, cars * pitch + 2f);
        TrainBox(parent, x, y, z0, z0 + boxLen, xw, xe, height, mouthW, mouthH, true);
        TrainBox(parent, x, y, z1 - boxLen, z1, xw, xe, height, mouthW, mouthH, false);
        Rail(parent, new Vector3(x, y, z0 + T + 0.2f), new Vector3(x, y, z1 - T - 0.2f));
        var host = new GameObject("TrainLine");
        host.transform.SetParent(parent);
        host.transform.position = new Vector3(x, y, (z0 + z1) * 0.5f);
        var fleet = new Transform[cars];
        for (int i = 0; i < cars; i++) fleet[i] = TrainCar(host.transform, i == 0, i == cars - 1, carLen);   // a cab at each end: it runs both ways
        // The mouths: a signal lamp over each (dark until a train is due), a headlamp light inside each box
        var lamps = new Renderer[2];
        var heads = new Light[2];
        for (int m = 0; m < 2; m++)
        {
            float mz = m == 0 ? z0 + boxLen : z1 - boxLen;
            Vector3 face = m == 0 ? Vector3.forward : Vector3.back;
            Vector3 top = new Vector3(x, y + mouthH + 0.9f, mz) + face * 0.3f;
            Decor(parent, PrimitiveType.Cube, top, Quaternion.identity, new Vector3(0.44f, 0.44f, 0.24f), RivetIron, "MouthSignalHousing");
            lamps[m] = Decor(parent, PrimitiveType.Sphere, top + face * 0.16f, Quaternion.identity, Vector3.one * 0.34f, ugLampOff, "MouthSignal").GetComponent<Renderer>();
            heads[m] = Light(host.transform, new Vector3(x, y + 2.6f, mz) - face * 5f, 30f, 0f);
            heads[m].color = new Color(1f, 0.93f, 0.8f);
            heads[m].enabled = false;
        }
        var hz = host.AddComponent<TrainHazard>();
        hz.Init(new Vector3(x, y, z0 + T), new Vector3(x, y, z1 - T), fleet, carLen, pitch, speed, period, phase, 3f, northbound, lamps, heads, ugLampOff, ugSignalRed, ugSignalAmber);
        return hz;
    }

    /// A STEAM VENT in the floor: a grate in a brass frame with red edge lines and a state lamp, dark; every `period`
    /// seconds it hisses for two (the lamp amber, steam leaking) and bursts for 0.4 (the lamp red, a column): the burst
    /// launches whoever is in it 12 m/s up — a timed pad — and hurts (15) only whoever stood in it (SteamVentHazard)
    public static SteamVentHazard SteamVentPad(Transform parent, Vector3 groundPos, float period = 6f, float phase = 0f)
    {
        EnsureUnderground();
        Physics.SyncTransforms();
        if (Physics.Raycast(groundPos + Vector3.up, Vector3.down, out RaycastHit under, 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) groundPos = under.point;
        Grate(parent, groundPos, new Vector2(1.8f, 1.8f));
        var host = new GameObject("SteamVent");
        host.transform.SetParent(parent);
        host.transform.position = groundPos;
        var lines = new Renderer[4];
        for (int i = 0; i < 4; i++)
        {
            bool alongX = i < 2;
            float s = i % 2 == 0 ? -1f : 1f;
            Vector3 off = alongX ? new Vector3(0f, 0f, s * 1.05f) : new Vector3(s * 1.05f, 0f, 0f);
            Decor(parent, PrimitiveType.Cube, groundPos + off + Vector3.up * 0.012f, Quaternion.identity, alongX ? new Vector3(2.3f, 0.02f, 0.12f) : new Vector3(0.12f, 0.02f, 2.3f), ugBrassRail, "VentFrame");
            lines[i] = Decor(host.transform, PrimitiveType.Cube, groundPos + off * 1.12f + Vector3.up * 0.014f, Quaternion.identity, alongX ? new Vector3(2.5f, 0.02f, 0.06f) : new Vector3(0.06f, 0.02f, 2.5f), ugSignalRed, "VentLine").GetComponent<Renderer>();
            lines[i].enabled = false;
        }
        Decor(parent, PrimitiveType.Cylinder, groundPos + new Vector3(1.25f, 0.45f, 1.25f), Quaternion.identity, new Vector3(0.07f, 0.45f, 0.07f), ugBrassRail, "VentLampPost");
        var lamp = Decor(host.transform, PrimitiveType.Sphere, groundPos + new Vector3(1.25f, 1f, 1.25f), Quaternion.identity, Vector3.one * 0.26f, ugLampOff, "VentLamp").GetComponent<Renderer>();
        var hiss = Steam(host.transform, groundPos + Vector3.up * 0.1f, Vector3.up * 2.2f, 70f, 1.3f, 0.35f, 1.1f);
        var burst = Steam(host.transform, groundPos + Vector3.up * 0.2f, Vector3.up * 16f, 600f, 0.9f, 0.9f, 2.4f);
        var l = Light(host.transform, groundPos + Vector3.up * 1.2f, 9f, 0f);
        l.color = new Color(1f, 0.95f, 0.85f);
        l.enabled = false;
        var hz = host.AddComponent<SteamVentHazard>();
        hz.Init(groundPos, period, phase, hiss, burst, lamp, lines, l, ugLampOff, ugSignalAmber, ugSignalRed);
        return hz;
    }

    /// A WATERFALL pouring over a lip at `topCentre`, `width` wide, `drop` deep, lying along x (or z): the sheet, its lip,
    /// the spray at its foot; inside it you are pushed DOWN — a drop you choose (WaterfallHazard)
    public static WaterfallHazard Waterfall(Transform parent, Vector3 topCentre, float width, float drop, bool alongX = true)
    {
        EnsureUnderground();
        Vector3 size = alongX ? new Vector3(width, drop, 0.4f) : new Vector3(0.4f, drop, width);
        Vector3 centre = topCentre - Vector3.up * (drop * 0.5f);
        var sheet = Decor(parent, PrimitiveType.Cube, centre, Quaternion.identity, size, ugWaterFall, "Waterfall");
        sheet.GetComponent<MeshRenderer>().sharedMaterial = Tiled(ugWaterFall, size);
        sheet.AddComponent<WaterFlow>().speed = new Vector2(0f, -0.9f);
        Decor(parent, PrimitiveType.Cube, topCentre + Vector3.up * 0.08f, Quaternion.identity, alongX ? new Vector3(width + 0.4f, 0.16f, 1f) : new Vector3(1f, 0.16f, width + 0.4f), ugWaterFall, "WaterfallLip");
        if (Atmosphere)
        {
            steamMat ??= Particle(new Color(0.85f, 0.87f, 0.9f, 0.35f), false);
            Vector3 box = alongX ? new Vector3(width, 0.6f, 2.6f) : new Vector3(2.6f, 0.6f, width);
            Cloud(parent, "Spray", topCentre - Vector3.up * (drop - 0.5f), box, steamMat, 8f * width, 1.6f, 0.6f, 1.8f, new Color(0.85f, 0.87f, 0.9f, 0.28f), Vector3.up * 1.6f, -0.05f, 0.5f);
        }
        var host = new GameObject("WaterfallHazard");
        host.transform.SetParent(parent);
        host.transform.position = centre;
        var hz = host.AddComponent<WaterfallHazard>();
        hz.Init(centre, new Vector3(size.x * 0.5f + 0.35f, drop * 0.5f, size.z * 0.5f + 0.35f));
        return hz;
    }

    /// A SLUICE GATE across a lane: riveted posts and a lintel with the housing over it, an iron leaf that drops shut —
    /// open `open` seconds in every `period` (3 in 8); the lamp over it amber as it is about to fall, red while it is
    /// shut; shut, it is a wall that shoves; shutting on you, 10 and a shove (SluiceGateHazard)
    public static SluiceGateHazard SluiceGate(Transform parent, Vector3 groundCentre, float yaw, float width, float height, float period = 8f, float open = 3f, float phase = 0f)
    {
        EnsureUnderground();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Vector3 right = rot * Vector3.right, fwd = rot * Vector3.forward;
        for (int s = -1; s <= 1; s += 2)
        {
            var post = Slab(parent, groundCentre + right * (s * (width * 0.5f + 0.3f)) + Vector3.up * (height + 0.5f), new Vector3(0.6f, 2f * height + 1f, 0.8f), RivetIron, "SluicePost");
            post.transform.rotation = rot;
        }
        var lintel = Slab(parent, groundCentre + Vector3.up * (height + 0.4f), new Vector3(width + 1.2f, 0.8f, 0.8f), RivetIron, "SluiceLintel");
        lintel.transform.rotation = rot;
        Decor(parent, PrimitiveType.Cube, groundCentre + Vector3.up * (height * 1.5f + 0.9f), rot, new Vector3(width + 0.6f, height + 0.2f, 0.5f), pressMat, "SluiceHousing");
        Stripes(parent, groundCentre + Vector3.up * 0.02f, new Vector3(width, 0.02f, 0.6f), yaw);
        var leaf = new GameObject("SluiceLeaf").transform;
        leaf.SetParent(parent);
        leaf.SetPositionAndRotation(groundCentre, rot);
        Decor(leaf, PrimitiveType.Cube, groundCentre + Vector3.up * (height * 0.5f), rot, new Vector3(width, height, 0.2f), pressMat, "Leaf");
        Decor(leaf, PrimitiveType.Cube, groundCentre + Vector3.up * 0.18f, rot, new Vector3(width - 0.1f, 0.3f, 0.22f), ugRailCover, "LeafEdge");
        for (int i = 1; i < 3; i++) Decor(leaf, PrimitiveType.Cube, groundCentre + Vector3.up * (height * i / 3f), rot, new Vector3(width - 0.1f, 0.12f, 0.24f), RivetIron, "LeafRib");
        var lamp = Decor(parent, PrimitiveType.Sphere, groundCentre + Vector3.up * (height + 0.95f) + fwd * 0.44f, Quaternion.identity, Vector3.one * 0.32f, ugLampOff, "SluiceLamp").GetComponent<Renderer>();
        var l = Light(parent, groundCentre + Vector3.up * (height + 0.6f) + fwd * 1.2f, 9f, 0f);
        l.color = new Color(1f, 0.25f, 0.15f);
        l.enabled = false;
        var hz = leaf.gameObject.AddComponent<SluiceGateHazard>();
        hz.Init(leaf, groundCentre, groundCentre + Vector3.up * (height + 0.85f), rot, new Vector3(width * 0.5f, height * 0.5f, 0.6f), fwd, lamp, l, ugLampOff, ugSignalAmber, ugSignalRed, period, open, phase);
        return hz;
    }

    /// A COLLAPSING SLAB: a paving slab `size` wide with its top at `topCentre`, cracked, red lines round its edge that come
    /// on when it is armed. Land on it and 0.4 s later it drops to `dropTo` (its collider off: you fall with it); six
    /// seconds on it comes back (CollapsingSlabHazard). Under it the course puts a pad or a slide, never a pit
    public static CollapsingSlabHazard CollapsingSlab(Transform parent, Vector3 topCentre, Vector2 size, float dropTo)
    {
        EnsureUnderground();
        var root = new GameObject("CollapsingSlab").transform;
        root.SetParent(parent);
        root.position = topCentre - Vector3.up * 0.25f;
        var body = Solid(root, PrimitiveType.Cube, root.position, Quaternion.identity, new Vector3(size.x, 0.5f, size.y), Limestone, "Slab");
        int seed = Mathf.RoundToInt(topCentre.x * 3.1f + topCentre.z * 7.7f);
        for (int i = 0; i < 3; i++)
        {
            float ang = MenuArt.Hash01(seed, i, 301) * 180f;
            Vector3 at = topCentre + new Vector3((MenuArt.Hash01(seed, i, 302) - 0.5f) * size.x * 0.6f, 0.012f, (MenuArt.Hash01(seed, i, 303) - 0.5f) * size.y * 0.6f);
            Decor(root, PrimitiveType.Cube, at, Quaternion.Euler(0f, ang, 0f), new Vector3(0.05f, 0.02f, Mathf.Min(size.x, size.y) * 0.7f), Soot, "Crack");
        }
        var lines = new Renderer[4];
        for (int i = 0; i < 4; i++)
        {
            bool alongX = i < 2;
            float s = i % 2 == 0 ? -1f : 1f;
            Vector3 at = topCentre + Vector3.up * 0.015f + (alongX ? new Vector3(0f, 0f, s * (size.y * 0.5f - 0.1f)) : new Vector3(s * (size.x * 0.5f - 0.1f), 0f, 0f));
            lines[i] = Decor(root, PrimitiveType.Cube, at, Quaternion.identity, alongX ? new Vector3(size.x - 0.1f, 0.02f, 0.08f) : new Vector3(0.08f, 0.02f, size.y - 0.1f), ugSignalRed, "SlabEdge").GetComponent<Renderer>();
            lines[i].enabled = false;
        }
        var hz = root.gameObject.AddComponent<CollapsingSlabHazard>();
        hz.Init(root, body.GetComponent<Collider>(), topCentre, dropTo, new Vector2(size.x * 0.5f, size.y * 0.5f), lines);
        return hz;
    }

    /// A DEPTH DIAL on a wall: a dark face in a brass rim, five depths round its top half, a red needle that follows
    /// `target` (the lift car) or, with none, you — from `topY` (the needle hard left) to `bottomY` (hard right)
    public static DepthDial DepthDial(Transform parent, Vector3 wallPoint, Vector3 facing, float topY, float bottomY, Transform target = null, float dia = 1.6f)
    {
        EnsureUnderground();
        Vector3 n = facing.normalized;
        Quaternion rot = Quaternion.LookRotation(n);
        var root = new GameObject("DepthDial").transform;
        root.SetParent(parent);
        root.SetPositionAndRotation(wallPoint, rot);
        Decor(root, PrimitiveType.Cylinder, wallPoint + n * 0.03f, rot * Quaternion.Euler(90f, 0f, 0f), new Vector3(dia + 0.16f, 0.03f, dia + 0.16f), ugBrassRail, "DialRim");
        Decor(root, PrimitiveType.Cylinder, wallPoint + n * 0.05f, rot * Quaternion.Euler(90f, 0f, 0f), new Vector3(dia, 0.03f, dia), ugDial, "DialFace");
        for (int i = 0; i < 5; i++)
        {
            float ang = Mathf.Lerp(-80f, 80f, i / 4f);
            Vector3 dir = rot * new Vector3(Mathf.Sin(ang * Mathf.Deg2Rad), Mathf.Cos(ang * Mathf.Deg2Rad), 0f);
            Decor(root, PrimitiveType.Cube, wallPoint + n * 0.075f + dir * (dia * 0.43f), rot * Quaternion.Euler(0f, 0f, -ang), new Vector3(0.03f, 0.12f, 0.02f), floodMat, "DialMark");
            int depth = Mathf.RoundToInt(Mathf.Lerp(0f, topY - bottomY, i / 4f));
            Text(root, wallPoint + n * 0.075f + dir * (dia * 0.33f), Quaternion.LookRotation(-n), depth == 0 ? "0" : "-" + depth, 0.045f, SignCream, "DialText", 0.4f, true, 40f, 80f);
        }
        var pivot = new GameObject("Needle").transform;
        pivot.SetParent(root);
        pivot.SetPositionAndRotation(wallPoint + n * 0.09f, rot);
        Decor(pivot, PrimitiveType.Cube, pivot.position + rot * new Vector3(0f, dia * 0.2f, 0f), rot, new Vector3(0.05f, dia * 0.42f, 0.02f), ugRailCover, "NeedleHand");
        Decor(pivot, PrimitiveType.Cylinder, pivot.position, rot * Quaternion.Euler(90f, 0f, 0f), new Vector3(0.14f, 0.03f, 0.14f), ugBrassRail, "NeedleBoss");
        var l = Light(root, wallPoint + n * 0.5f, 4f, 0.8f);
        l.color = LampCreamLight;
        var dial = root.gameObject.AddComponent<DepthDial>();
        dial.Init(pivot, topY, bottomY, target);
        return dial;
    }
}

/// Shared lookups for the Underground's hazards (the player and the director, once a frame each), and spatial speakers
/// that play the kit's own sample buckets where the hazard is
static class UndergroundRun
{
    public static bool Resolve(out PlayerMotor motor, out CombatDirector director, out Vector3 feet)
    {
        director = CombatDirector.Instance;
        motor = director != null ? director.motor : null;
        feet = motor != null ? motor.CurrPosition : new Vector3(0f, -9999f, 0f);
        return motor != null;
    }

    /// A speaker on `go`: a clip from `bucket`, heard to `range`; looping ones start at once at `volume`
    public static AudioSource Speaker(GameObject go, string bucket, float range, bool loop, float volume, float pitch)
    {
        var src = go.AddComponent<AudioSource>();
        src.clip = GameFeel.Clip(bucket);
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 3f;
        src.maxDistance = range;
        src.loop = loop;
        src.playOnAwake = false;
        src.dopplerLevel = 0f;
        src.volume = volume * GameSettings.SfxVolume;
        src.pitch = pitch;
        if (loop && src.clip != null) src.Play();
        return src;
    }

    public static void Shot(AudioSource src, string bucket, float volume, float pitch)
    {
        if (src == null) return;
        var clip = GameFeel.Clip(bucket);
        if (clip == null) return;
        src.pitch = pitch;
        src.PlayOneShot(clip, volume * GameSettings.SfxVolume);
    }

    public static void Loop(AudioSource src, float volume)
    {
        if (src == null) return;
        src.volume = volume * GameSettings.SfxVolume;
    }
}

/// THE THIRD RAIL, live: a line at the rail's height; the feet within half a metre of it at that height = 25 and a
/// throw up and away (your run speed mostly lost: it is a punishment, never a kill). It arcs along its length every few
/// seconds (the flash and the crack are the tell that it is live) and hums
public class ThirdRailHazard : MonoBehaviour, IWeldDynamic
{
    public const float Damage = 25f, ThrowUp = 8f, ThrowAway = 9f, Reach = 0.55f;
    Vector3 a, ab;
    float len2, y, hitReadyAt, arcAt, arcUntil;
    int arcIndex;
    Renderer[] arcs;
    Light glow;
    AudioSource hum, crack;

    public void DynamicParts(List<Transform> into) { if (arcs != null) foreach (var r in arcs) if (r != null) into.Add(r.transform); }

    public void Init(Vector3 from, Vector3 to, Renderer[] arcRenderers, Light arcLight)
    {
        a = from;
        ab = to - from;
        len2 = Mathf.Max(0.01f, ab.sqrMagnitude);
        y = from.y;
        arcs = arcRenderers;
        glow = arcLight;
        hum = UndergroundRun.Speaker(gameObject, "sub", 26f, true, 0.16f, 0.55f);
        crack = UndergroundRun.Speaker(gameObject, "glass", 34f, false, 0.4f, 1.7f);
        arcAt = Time.time + 1f + Mathf.Repeat(from.x + from.z, 2f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        float t = Time.time;
        if (arcUntil > 0f && t >= arcUntil)
        {
            arcUntil = 0f;
            if (arcs[arcIndex] != null) arcs[arcIndex].enabled = false;
            if (glow != null) glow.enabled = false;
        }
        bool live = UndergroundRun.Resolve(out var motor, out var director, out Vector3 feet);
        if (t >= arcAt)
        {
            // The arc: a flash somewhere along it, the light with it, the crack if you are near enough to hear it
            arcAt = t + 2.2f + Mathf.PerlinNoise(t * 0.37f, a.x) * 3f;
            arcIndex = (arcIndex + 1) % arcs.Length;
            arcUntil = t + 0.08f;
            var arc = arcs[arcIndex];
            if (arc != null)
            {
                arc.enabled = true;
                if (glow != null) { glow.enabled = true; glow.transform.position = arc.transform.position + Vector3.up * 0.4f; glow.intensity = 3f; }
                if (live && (feet - arc.transform.position).sqrMagnitude < 1100f) { crack.transform.position = arc.transform.position; UndergroundRun.Shot(crack, "glass", 0.3f, 1.6f + arcIndex * 0.1f); }
            }
        }
        if (!live || director == null || t < hitReadyAt || motor.HasIFrames) return;
        float k = Mathf.Clamp01(Vector3.Dot(feet - a, ab) / len2);
        Vector3 closest = a + ab * k;
        Vector3 flat = feet - closest;
        flat.y = 0f;
        if (flat.sqrMagnitude > Reach * Reach) return;
        if (feet.y < y - 0.6f || feet.y > y + 0.35f) return;   // over it (the tray, the platform) or under it: clear
        hitReadyAt = t + 0.8f;
        Vector3 away = flat.sqrMagnitude > 0.001f ? flat.normalized : Vector3.Cross(ab.normalized, Vector3.up);
        Vector3 v = motor.Velocity;
        director.ReportPlayerHit(away, Damage);
        motor.Launch(new Vector3(v.x * 0.4f + away.x * ThrowAway, Mathf.Max(v.y, 0f) + ThrowUp, v.z * 0.4f + away.z * ThrowAway));
        var feel = GameFeel.Instance;
        if (feel != null) { feel.Sparks(closest + Vector3.up * 0.3f, 40, false); feel.PlayArc(); }
        ShockRing.Spawn(closest + Vector3.up * 0.1f, 0.4f, 2.5f, 0.25f, false);
        var flash = arcs[arcIndex];
        if (flash != null) { flash.enabled = true; arcUntil = t + 0.16f; }
        if (glow != null) { glow.enabled = true; glow.transform.position = closest + Vector3.up * 0.6f; glow.intensity = 5f; }
    }
}

/// A TRAIN on a line between two tunnel boxes: it waits parked in one, and every `period` seconds — three seconds of
/// headlamps on the mouth, the horn, the hum rising — it runs to the other at `speed` and parks there; the cycle after,
/// it comes back. Contact with any car = 40 and a throw along its travel. A pass within a hand of its side at its height
/// is a NEAR MISS. The cars always face north: southbound they run backwards, the far end's lamp leading
public class TrainHazard : MonoBehaviour, IWeldDynamic
{
    public const float Damage = 40f, ThrowAlong = 16f, ThrowUp = 7f;
    Vector3 south, dirN;
    Quaternion rotN, invN;
    Transform[] cars;
    Light[] ends = new Light[2];
    Renderer[] lamps;
    Light[] heads;
    Material off, red, amber;
    float carLen, pitch, trainLen, speed, period, phase, tell, len, hitReadyAt;
    bool northbound, hornDone, passed, lampLit;
    int cycle = -1, side;
    AudioSource horn, hum;

    public void DynamicParts(List<Transform> into)
    {
        foreach (var c in cars) if (c != null) into.Add(c);
        foreach (var r in lamps) if (r != null) into.Add(r.transform);
    }

    public void Init(Vector3 s, Vector3 n, Transform[] fleet, float carLength, float carPitch, float runSpeed, float cyclePeriod, float cyclePhase, float tellSeconds, bool firstNorth,
        Renderer[] mouthLamps, Light[] headlamps, Material lampOff, Material lampRed, Material lampAmber)
    {
        south = s;
        len = (n - s).magnitude;
        dirN = (n - s) / Mathf.Max(0.01f, len);
        rotN = Quaternion.LookRotation(dirN);
        invN = Quaternion.Inverse(rotN);
        cars = fleet;
        carLen = carLength;
        pitch = carPitch;
        trainLen = (cars.Length - 1) * pitch + carLen;
        speed = runSpeed;
        period = Mathf.Max(6f, cyclePeriod);
        phase = cyclePhase;
        tell = tellSeconds;
        northbound = firstNorth;
        lamps = mouthLamps;
        heads = headlamps;
        off = lampOff; red = lampRed; amber = lampAmber;
        var north = cars[0].Find("HeadLightN");
        var southEnd = cars[cars.Length - 1].Find("HeadLightS");
        ends[0] = north != null ? north.GetComponent<Light>() : null;
        ends[1] = southEnd != null ? southEnd.GetComponent<Light>() : null;
        foreach (var l in ends) if (l != null) l.enabled = false;
        horn = UndergroundRun.Speaker(gameObject, "bell", 110f, false, 0.7f, 0.5f);
        hum = UndergroundRun.Speaker(cars[0].gameObject, "sub", 70f, true, 0f, 0.7f);
        Tick(Time.time, true);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        Tick(Time.time, false);
    }

    /// Where the train's south end is this frame, in metres from the south box's back wall
    void Tick(float t, bool first)
    {
        float cyc = t / period + phase;
        int idx = Mathf.FloorToInt(cyc);
        float u = (cyc - idx) * period;
        bool north = northbound == (idx % 2 == 0);
        if (idx != cycle)
        {
            cycle = idx;
            hornDone = passed = false;
            side = 0;
        }
        float parkS = 1f, parkN = len - 1f - trainLen;
        float tail;
        bool running;
        if (u < tell) { tail = north ? parkS : parkN; running = false; }
        else
        {
            float gone = (u - tell) * speed;
            tail = north ? Mathf.Min(parkN, parkS + gone) : Mathf.Max(parkS, parkN - gone);
            running = north ? tail < parkN : tail > parkS;
        }
        bool arrived = u >= tell && !running;
        for (int i = 0; i < cars.Length; i++)
        {
            Vector3 p = south + dirN * (tail + carLen * 0.5f + (cars.Length - 1 - i) * pitch);
            cars[i].SetPositionAndRotation(p, rotN);
        }
        // The tell and the pass: the leaving mouth's lamp red, its headlamp light climbing, the leading car's light on,
        // the horn once, the hum rising then full; arrived, everything dark and quiet
        int leaving = north ? 0 : 1;
        bool lit = !arrived;
        if (lit != lampLit || first)
        {
            lampLit = lit;
            for (int m = 0; m < lamps.Length; m++) if (lamps[m] != null) lamps[m].sharedMaterial = lit && m == leaving ? red : arrived && m != leaving ? amber : off;
            for (int m = 0; m < heads.Length; m++) if (heads[m] != null) heads[m].enabled = lit && m == leaving;
            var lead = ends[north ? 0 : 1];
            var rear = ends[north ? 1 : 0];
            if (lead != null) lead.enabled = lit;
            if (rear != null) rear.enabled = false;
        }
        if (!arrived)
        {
            float k = Mathf.Clamp01(u / tell);
            if (heads[leaving] != null) heads[leaving].intensity = running ? Mathf.MoveTowards(heads[leaving].intensity, 0f, 6f * Time.deltaTime) : 3.2f * k;
            UndergroundRun.Loop(hum, running ? 0.7f : 0.45f * k);
            if (!hornDone && u >= 0.4f)
            {
                hornDone = true;
                horn.transform.position = south + dirN * (north ? parkS + trainLen : parkN) + Vector3.up * 2f;
                UndergroundRun.Shot(horn, "bell", 0.7f, 0.5f);
                UndergroundRun.Shot(horn, "sub", 0.4f, 0.9f);
            }
        }
        else UndergroundRun.Loop(hum, 0f);
        if (!running || first) return;
        if (!UndergroundRun.Resolve(out var motor, out var director, out Vector3 feet) || director == null) return;
        Vector3 travel = north ? dirN : -dirN;
        for (int i = 0; i < cars.Length; i++)
        {
            Vector3 local = invN * (feet - cars[i].position);
            bool band = local.y > -0.6f && local.y < 3.1f;
            if (!band) continue;
            bool inside = Mathf.Abs(local.x) <= 1.75f && Mathf.Abs(local.z) <= carLen * 0.5f + 0.45f;
            if (inside && !motor.HasIFrames && t >= hitReadyAt)
            {
                hitReadyAt = t + 1.2f;
                passed = true;
                director.ReportPlayerHit((travel + Vector3.up * 0.4f).normalized, Damage);
                motor.AddImpulse(travel * ThrowAlong + Vector3.up * ThrowUp);
                var feel = GameFeel.Instance;
                if (feel != null) { feel.Sparks(feet + Vector3.up * 1f, 24, false); feel.PlayClank(1f); }
                return;
            }
            if (i == 0 || i == cars.Length - 1)
            {
                // The near miss: the leading car's end passed your z within a hand of its flank, and nothing touched you
                float along = Vector3.Dot(feet - cars[i].position, travel);
                int s = along > 0f ? 1 : -1;
                if (side > 0 && s < 0 && !passed && Mathf.Abs(local.x) > 1.75f && Mathf.Abs(local.x) < 2.9f) { passed = true; director.NearMiss(); }
                if (Mathf.Abs(along) < 6f) side = s;
            }
        }
    }
}

/// A STEAM VENT: dark for most of its cycle, then the hiss (two seconds: the lamp amber, steam leaking, the red lines
/// on), then the burst (0.4 s: the lamp red, the column). Anyone in the column at the burst is launched 12 m/s up,
/// their own speed kept — a timed pad — and whoever STOOD in it through the hiss takes 15 as well
public class SteamVentHazard : MonoBehaviour, IWeldDynamic
{
    public const float Damage = 15f, Lift = 12f, Radius = 1.3f, Hiss = 2f, Burst = 0.4f, StoodFor = 0.8f;
    Vector3 centre;
    float period, phase, standSince = -1f, launchReadyAt;
    int state = -1;
    ParticleSystem hiss, burst;
    Renderer lamp;
    Renderer[] lines;
    Light glow;
    Material off, amber, red;
    AudioSource hissSrc, burstSrc;

    public void DynamicParts(List<Transform> into)
    {
        if (lamp != null) into.Add(lamp.transform);
        foreach (var r in lines) if (r != null) into.Add(r.transform);
    }

    public void Init(Vector3 at, float cyclePeriod, float cyclePhase, ParticleSystem hissPs, ParticleSystem burstPs, Renderer stateLamp, Renderer[] edgeLines, Light light, Material lampOff, Material lampAmber, Material lampRed)
    {
        centre = at;
        period = Mathf.Max(3f, cyclePeriod);
        phase = cyclePhase;
        hiss = hissPs;
        burst = burstPs;
        lamp = stateLamp;
        lines = edgeLines;
        glow = light;
        off = lampOff; amber = lampAmber; red = lampRed;
        if (hiss != null) { var em = hiss.emission; em.enabled = false; }
        if (burst != null) { var em = burst.emission; em.enabled = false; }
        hissSrc = UndergroundRun.Speaker(gameObject, "slide", 30f, true, 0f, 1.35f);
        burstSrc = UndergroundRun.Speaker(gameObject, "whoosh_hard", 50f, false, 0.8f, 0.8f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        float t = Time.time;
        float u = Mathf.Repeat(t / period + phase, 1f) * period;
        int s = u < period - Hiss - Burst ? 0 : u < period - Burst ? 1 : 2;
        if (s != state) { state = s; Switch(s); }
        if (!UndergroundRun.Resolve(out var motor, out var director, out Vector3 feet)) return;
        Vector3 flat = feet - centre;
        flat.y = 0f;
        bool inside = flat.sqrMagnitude <= Radius * Radius && feet.y >= centre.y - 0.3f && feet.y <= centre.y + 4f;
        bool standing = inside && motor.Grounded && feet.y <= centre.y + 0.6f;
        if (standing) { if (standSince < 0f) standSince = t; }
        else standSince = -1f;
        if (s == 2 && inside && t >= launchReadyAt)
        {
            launchReadyAt = t + period * 0.5f;
            Vector3 v = motor.Velocity;
            motor.Launch(new Vector3(v.x, Mathf.Max(v.y, 0f) + Lift, v.z));
            bool stood = standSince >= 0f && t - standSince >= StoodFor;
            if (stood && director != null && !motor.HasIFrames) director.ReportPlayerHit(Vector3.up, Damage);
            ShockRing.Spawn(centre + Vector3.up * 0.05f, 0.5f, 2.6f, 0.3f, false);
            var feel = GameFeel.Instance;
            if (feel != null) feel.PlayPad();
            if (glow != null) glow.intensity = 5f;
            standSince = -1f;
        }
        if (glow != null && glow.enabled) glow.intensity = Mathf.MoveTowards(glow.intensity, s == 2 ? 3f : 0.6f, dt * 8f);
    }

    void Switch(int s)
    {
        if (hiss != null) { var em = hiss.emission; em.enabled = s == 1; }
        if (burst != null) { var em = burst.emission; em.enabled = s == 2; }
        if (lamp != null) lamp.sharedMaterial = s == 0 ? off : s == 1 ? amber : red;
        foreach (var r in lines) if (r != null) r.enabled = s != 0;
        if (glow != null) { glow.enabled = s != 0; if (s == 2) glow.intensity = 3f; }
        UndergroundRun.Loop(hissSrc, s == 1 ? 0.35f : 0f);
        if (s == 2) { UndergroundRun.Shot(burstSrc, "whoosh_hard", 0.8f, 0.8f); UndergroundRun.Shot(burstSrc, "sub", 0.4f, 1f); }
    }
}

/// A WATERFALL's column: inside it, airborne, you are pushed down hard — the fast way off a high line, if you choose it
public class WaterfallHazard : MonoBehaviour
{
    public const float Push = 24f;
    Vector3 centre, half;

    public void Init(Vector3 c, Vector3 halfExtents)
    {
        centre = c;
        half = halfExtents;
        UndergroundRun.Speaker(gameObject, "whoosh_soft", 44f, true, 0.3f, 0.55f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        if (!UndergroundRun.Resolve(out var motor, out _, out Vector3 feet)) return;
        Vector3 local = feet - centre;
        if (Mathf.Abs(local.x) > half.x || Mathf.Abs(local.z) > half.z || local.y < -half.y - 0.5f || local.y > half.y + 1.5f) return;
        if (!motor.Grounded) motor.AddImpulse(Vector3.down * (Push * dt));
    }
}

/// A SLUICE GATE's leaf: up (open) for `openFor` seconds in every `period`, the lamp amber for the last 0.7 of them,
/// then it falls in 0.3 and stands shut (red) until 0.45 s before the cycle ends. Shut, it shoves whoever is in its
/// plane back the way they came; shutting on you, 10 and the shove
public class SluiceGateHazard : MonoBehaviour, IWeldDynamic
{
    public const float Damage = 10f;
    Transform leaf;
    Vector3 shut, open, centre, half, fwd;
    Quaternion inv;
    Renderer lamp;
    Light glow;
    Material off, amber, red;
    float period, openFor, phase;
    int state = -1;
    bool wasShut;
    AudioSource clank;

    public void DynamicParts(List<Transform> into) { if (leaf != null) into.Add(leaf); if (lamp != null) into.Add(lamp.transform); }

    public void Init(Transform l, Vector3 shutPos, Vector3 openPos, Quaternion rot, Vector3 halfExtents, Vector3 forward, Renderer lampR, Light light, Material lampOff, Material lampAmber, Material lampRed, float p, float o, float ph)
    {
        leaf = l;
        shut = shutPos;
        open = openPos;
        centre = shutPos + Vector3.up * halfExtents.y;
        half = halfExtents;
        fwd = forward;
        inv = Quaternion.Inverse(rot);
        lamp = lampR;
        glow = light;
        off = lampOff; amber = lampAmber; red = lampRed;
        period = Mathf.Max(3f, p);
        openFor = Mathf.Clamp(o, 1f, period - 1.5f);
        phase = ph;
        clank = UndergroundRun.Speaker(gameObject, "metal", 44f, false, 0.6f, 0.7f);
        leaf.position = open;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        float t = Time.time;
        float u = Mathf.Repeat(t / period + phase, 1f) * period;
        const float fall = 0.3f, rise = 0.45f;
        float k;
        int s;
        if (u < openFor) { k = 0f; s = u > openFor - 0.7f ? 1 : 0; }
        else if (u < openFor + fall) { float q = (u - openFor) / fall; k = q * q; s = 2; }
        else if (u < period - rise) { k = 1f; s = 2; }
        else { k = 1f - Mathf.SmoothStep(0f, 1f, (u - (period - rise)) / rise); s = 0; }
        leaf.position = Vector3.Lerp(open, shut, k);
        bool isShut = k >= 0.999f;
        if (s != state)
        {
            state = s;
            if (lamp != null) lamp.sharedMaterial = s == 0 ? off : s == 1 ? amber : red;
            if (glow != null) { glow.enabled = s == 2; glow.intensity = 2.4f; }
        }
        bool live = UndergroundRun.Resolve(out var motor, out var director, out Vector3 feet);
        if (isShut && !wasShut)
        {
            if (live)
            {
                float near = Vector3.Distance(feet, centre);
                if (near < 42f) UndergroundRun.Shot(clank, "metal", Mathf.Clamp01(1f - near / 44f) * 0.8f, 0.7f);
                if (director != null && !motor.HasIFrames)
                {
                    Vector3 local = inv * (feet - centre);
                    if (Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.z) <= half.z && local.y >= -half.y - 0.2f && local.y <= half.y)
                    {
                        Vector3 outDir = fwd * (local.z >= 0f ? 1f : -1f);
                        director.ReportPlayerHit(outDir, Damage);
                        motor.AddImpulse(outDir * 9f + Vector3.up * 4f);
                    }
                }
            }
        }
        else if (isShut && live)
        {
            Vector3 local = inv * (feet - centre);
            if (Mathf.Abs(local.x) <= half.x + 0.3f && Mathf.Abs(local.z) <= half.z && local.y >= -half.y - 0.2f && local.y <= half.y)
                motor.AddImpulse(fwd * ((local.z >= 0f ? 1f : -1f) * 30f * dt));   // shut: a wall, and it shoves
        }
        wasShut = isShut;
    }
}

/// A COLLAPSING SLAB: rest → armed the moment you stand on it (0.4 s: it shivers, the red lines come on, a creak) →
/// falling (its collider off, so you go with it) → gone (6 s) → rising back, solid again once nobody is where it lands
public class CollapsingSlabHazard : MonoBehaviour, IWeldDynamic
{
    public const float Arm = 0.4f, Gone = 6f, RiseTime = 0.8f;
    Transform root;
    Collider body;
    Vector3 restPos;
    Vector2 half;
    Renderer[] lines;
    float top, dropTo, since, vy;
    int state;
    AudioSource creak;

    public void DynamicParts(List<Transform> into) { if (root != null) into.Add(root); }

    public void Init(Transform r, Collider c, Vector3 topCentre, float dropToY, Vector2 halfExtents, Renderer[] edge)
    {
        root = r;
        body = c;
        restPos = r.position;
        top = topCentre.y;
        dropTo = dropToY;
        half = halfExtents;
        lines = edge;
        creak = UndergroundRun.Speaker(gameObject, "wood", 34f, false, 0.6f, 0.5f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        float t = Time.time;
        bool live = UndergroundRun.Resolve(out var motor, out _, out Vector3 feet);
        switch (state)
        {
            case 0:
                if (live && motor.Grounded && Mathf.Abs(feet.x - restPos.x) <= half.x + 0.3f && Mathf.Abs(feet.z - restPos.z) <= half.y + 0.3f && feet.y >= top - 0.2f && feet.y <= top + 0.5f)
                {
                    state = 1;
                    since = t;
                    foreach (var r in lines) if (r != null) r.enabled = true;
                    UndergroundRun.Shot(creak, "wood", 0.7f, 0.45f);
                    UndergroundRun.Shot(creak, "stone", 0.4f, 0.8f);
                }
                break;
            case 1:
                root.position = restPos + new Vector3(Mathf.Sin(t * 61f) * 0.02f, -0.03f * (t - since) / Arm, Mathf.Cos(t * 53f) * 0.02f);
                if (t - since >= Arm)
                {
                    state = 2;
                    since = t;
                    vy = 0f;
                    body.enabled = false;
                    UndergroundRun.Shot(creak, "stone", 0.9f, 0.6f);
                    UndergroundRun.Shot(creak, "sub", 0.5f, 0.8f);
                }
                break;
            case 2:
                vy += 25f * dt;
                root.position += Vector3.down * (vy * dt);
                if (root.position.y <= dropTo - 0.25f)
                {
                    root.position = new Vector3(restPos.x, dropTo - 0.25f, restPos.z);
                    state = 3;
                    since = t;
                    foreach (var r in lines) if (r != null) r.enabled = false;
                    ShockRing.Spawn(new Vector3(restPos.x, dropTo + 0.05f, restPos.z), 0.5f, 3f, 0.4f, false);
                    UndergroundRun.Shot(creak, "stone", 0.7f, 0.5f);
                }
                break;
            case 3:
                if (t - since >= Gone) { state = 4; since = t; }
                break;
            default:
            {
                float k = Mathf.SmoothStep(0f, 1f, (t - since) / RiseTime);
                root.position = Vector3.Lerp(new Vector3(restPos.x, dropTo - 0.25f, restPos.z), restPos, k);
                if (k >= 1f)
                {
                    bool clear = !live || Mathf.Abs(feet.x - restPos.x) > half.x + 0.6f || Mathf.Abs(feet.z - restPos.z) > half.y + 0.6f || feet.y > top + 0.3f || feet.y < top - 2.5f;
                    if (clear) { body.enabled = true; state = 0; UndergroundRun.Shot(creak, "metal", 0.3f, 0.9f); }
                }
                break;
            }
        }
    }
}

/// A DEPTH DIAL's needle: hard left at `topY`, hard right at `bottomY`, following `target` (the lift car) or you
public class DepthDial : MonoBehaviour, IWeldDynamic
{
    Transform needle, target;
    float topY, bottomY, angle = -80f;

    public void DynamicParts(List<Transform> into) { if (needle != null) into.Add(needle); }

    public void Init(Transform n, float top, float bottom, Transform t)
    {
        needle = n;
        topY = top;
        bottomY = bottom;
        target = t;
    }

    void Update()
    {
        float y;
        if (target != null) y = target.position.y;
        else if (UndergroundRun.Resolve(out _, out _, out Vector3 feet)) y = feet.y;
        else return;
        float want = Mathf.Lerp(-80f, 80f, Mathf.InverseLerp(topY, bottomY, y));
        angle = Mathf.MoveTowards(angle, want, Time.deltaTime * 140f);
        if (needle != null) needle.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }
}

public static partial class LevelKit
{
    /// A blind tunnel portal: the brick arch round a dark mouth painted on a solid wall (decor: for roads that run into
    /// the walls, where the shunters come and go)
    public static void BlindPortal(Transform parent, Vector3 groundCentre, Vector3 facing, float w, float h)
    {
        Vector3 n = facing.normalized;
        Decor(parent, PrimitiveType.Cube, groundCentre + n * 0.05f + Vector3.up * (h * 0.5f), Quaternion.LookRotation(n), new Vector3(w, h, 0.08f), Soot, "BlindMouth");
        TunnelPortal(parent, groundCentre, facing, w, h);
    }

    /// A shunting loco driving a rail through `points` at `speed`, out and back, its horn sounding as it comes
    /// (FactoryLife's cart: contact = the cart damage and a throw along its travel; a pass within a hand is a NEAR MISS)
    public static void ShuntingLoco(Transform parent, Vector3[] points, float speed, float phase = 0f)
    {
        EnsureUnderground();
        if (points == null || points.Length < 2) return;
        var loco = TrainCar(parent, true, true, 7.5f);
        loco.name = "ShuntingLoco";
        loco.SetPositionAndRotation(points[0], Quaternion.LookRotation((points[1] - points[0]).normalized));
        Life(parent).AddCart(loco, points, speed, phase, true, 3.2f);
    }

    /// A turntable: the pit's dark ring, the deck a hair proud of the floor in its brass rim, the table's bridge and
    /// rails, and a loco turning slowly on it (decor; the spin is FactoryLife's)
    public static void Turntable(Transform parent, Vector3 centre, float radius)
    {
        EnsureUnderground();
        Decor(parent, PrimitiveType.Cylinder, centre + Vector3.up * 0.015f, Quaternion.identity, new Vector3(radius * 2f + 1.6f, 0.015f, radius * 2f + 1.6f), ugCarDark, "TurntablePit");
        Decor(parent, PrimitiveType.Cylinder, centre + Vector3.up * 0.035f, Quaternion.identity, new Vector3(radius * 2f + 0.4f, 0.015f, radius * 2f + 0.4f), ugBrassRail, "TurntableRim");
        Decor(parent, PrimitiveType.Cylinder, centre + Vector3.up * 0.06f, Quaternion.identity, new Vector3(radius * 2f, 0.02f, radius * 2f), RivetIron, "TurntableDeck");
        var pivot = new GameObject("TurntablePivot").transform;
        pivot.SetParent(parent);
        pivot.position = centre;
        Decor(pivot, PrimitiveType.Cube, centre + Vector3.up * 0.09f, Quaternion.identity, new Vector3(2.6f, 0.03f, radius * 2f - 0.6f), ugCarDark, "TableBridge");
        for (int s = -1; s <= 1; s += 2) Decor(pivot, PrimitiveType.Cube, centre + new Vector3(s * 0.75f, 0.17f, 0f), Quaternion.identity, new Vector3(0.08f, 0.14f, radius * 2f - 1f), railMat, "TableRail");
        var loco = TrainCar(pivot, true, true, 8.4f);
        loco.position = centre + Vector3.up * 0.12f;
        Life(parent).AddSpin(pivot, Vector3.up, 4f);
    }
}
