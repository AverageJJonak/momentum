using UnityEngine;

/// <summary>
/// THE CHASE KIT: the pieces a chase chunk is drawn with, in the course kit's language (LevelKitCourse: brass on a
/// lip, cream flutes on a kick wall or a corner, a hairline on a fast line) — with one difference: every tech MARKER
/// (LipMarker, CornerMarker) is parented to the chunk, where TechMarkers' are loose in the scene, because a chunk is
/// destroyed at runtime and its markers must go with it. Solids are Slab (a collider); marks are Prim (none), a few
/// millimetres proud of the face they sit on; anything standing on a floor is sunk five centimetres into it so no two
/// faces share a plane. The centre channel (±Chase.ChanHalf) carries only the rail or the track.
/// </summary>
public static partial class LevelKit
{
    const float Sink = 0.05f;   // what stands on a floor is sunk this far into it (coplanar rule)

    static Material ChaseWallMat(ChaseKind k) => k == ChaseKind.Concierge ? Cream : Tile;
    static Material ChaseSolidMat(ChaseKind k) => k == ChaseKind.Concierge ? Limestone : RivetIron;
    static Material ChaseDadoMat(ChaseKind k) => k == ChaseKind.Concierge ? Marble : OxbloodTile;
    static Material ChaseEdgeMat(ChaseKind k) => k == ChaseKind.Concierge ? Brass : Oxblood;
    static float PlatW => Chase.HalfW - Chase.ChanHalf;                 // a platform's width, channel edge to wall
    static float PlatX(int s) => s * (Chase.ChanHalf + PlatW * 0.5f);   // a platform's centre x

    // ------------------------------------------------------------ markers that belong to the chunk

    static void ChaseMarker<T>(Transform parent, Vector3 pos, Vector3 size) where T : Component
    {
        var go = new GameObject(typeof(T).Name);
        go.layer = 2;   // Ignore Raycast: invisible to the motor's movement casts, as TechMarkers makes them
        go.transform.SetParent(parent);
        go.transform.position = pos;
        var box = go.AddComponent<BoxCollider>();
        box.size = size;
        box.isTrigger = true;
        go.AddComponent<T>();
    }

    static void ChaseMarkCorners(Transform parent, Vector3 c, Vector3 s)
    {
        for (int dx = -1; dx <= 1; dx += 2)
            for (int dz = -1; dz <= 1; dz += 2)
                ChaseMarker<CornerMarker>(parent, c + new Vector3(dx * s.x * 0.5f, 0f, dz * s.z * 0.5f), new Vector3(0.4f, s.y, 0.4f));
    }

    static void ChaseMarkLip(Transform parent, Vector3 top, Vector3 size)
        => ChaseMarker<LipMarker>(parent, top + Vector3.up * (size.y * 0.5f - 0.2f), size);

    // ------------------------------------------------------------ floors

    /// A floor stretch z0..z1: the two platforms (into the walls), the channel floor sunk `drop` under them, and the
    /// platform edge line (brass in the House, oxblood at the terminus)
    public static void ChaseFloor(Transform parent, ChaseKind k, float y, float drop, float z0, float z1)
    {
        float len = z1 - z0, zm = (z0 + z1) * 0.5f;
        if (len < 0.2f) return;
        float pw = PlatW + 0.5f;
        for (int s = -1; s <= 1; s += 2)
        {
            Slab(parent, new Vector3(s * (Chase.ChanHalf + pw * 0.5f), y - 0.5f, zm), new Vector3(pw, 1f, len), Floor, "ChasePlatform");
            Prim(parent, new Vector3(s * (Chase.ChanHalf + 0.25f), y + MarkProud, zm), new Vector3(0.14f, 0.006f, len - 0.06f), ChaseEdgeMat(k), "EdgeLine");
        }
        Slab(parent, new Vector3(0f, y - drop - 0.5f, zm), new Vector3(Chase.ChanHalf * 2f, 1f, len), Charcoal, "ChaseChannel");
    }

    /// THE CATCH LANE under a gap z0..z1: a floor four metres down between two risers (the far one the three-metre
    /// face you clear off the pads: fluted, a kick wall), a pad on each platform side at its far end, and the
    /// hairline on the approach that says the line is the jump
    public static void ChaseCatch(Transform parent, ChaseKind k, Zone zone, float y, float z0, float z1)
    {
        float w = Chase.HalfW * 2f + 0.4f, cy = y - Chase.CatchDrop;
        float a = z0 - 3f, b = z1 + 3f;
        Slab(parent, new Vector3(0f, cy - 0.5f, (a + b) * 0.5f), new Vector3(w, 1f, b - a), Floor, "CatchLane");
        float riserH = Chase.CatchDrop - 1f, riserY = cy + riserH * 0.5f;
        Slab(parent, new Vector3(0f, riserY, z0 - 1.5f), new Vector3(w, riserH, 3f), ChaseSolidMat(k), "CatchRiser");
        Slab(parent, new Vector3(0f, riserY, z1 + 1.5f), new Vector3(w, riserH, 3f), ChaseSolidMat(k), "CatchRiser");
        for (int s = -1; s <= 1; s += 2)
        {
            Flute(parent, new Vector3(s * 5.2f, riserY, z1), Vector3.back, riserH - 0.4f, 0.7f);
            JumpPad(parent, new Vector3(s * 5.2f, cy, z1 - 1.7f), 0f, Chase.CatchPadUp, Chase.CatchPadAlong);
            LineRun(parent, new Vector3(s * 3.6f, y, z0 - 7f), new Vector3(s * 3.6f, y, z0 - 0.2f), zone);
        }
        var l = Light(parent, new Vector3(0f, cy + 2.5f, (z0 + z1) * 0.5f), 14f, 1.2f);
        l.color = new Color(1f, 0.62f, 0.3f);
    }

    // ------------------------------------------------------------ the verticals

    /// A LIP across both platforms at `z` (the channel left clear): `h` tall against the floor, `len` along the lane,
    /// brass strips on its approach and far edges, the LipMarker over its top
    public static void ChaseLip(Transform parent, ChaseKind k, float y, float z, float h, float len)
    {
        float pw = PlatW - 0.4f;
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 c = new Vector3(PlatX(s), y + h * 0.5f - Sink, z), size = new Vector3(pw, h, len);
            Slab(parent, c, size, ChaseSolidMat(k), "LipLedge");
            float top = y + h - Sink;
            for (int e = -1; e <= 1; e += 2)
                Prim(parent, new Vector3(c.x, top + MarkProud + 0.012f, z + e * (len * 0.5f - 0.09f)), new Vector3(pw - 0.08f, 0.024f, 0.14f), Brass, "LipBrass");
            ChaseMarkLip(parent, new Vector3(c.x, top, z), new Vector3(pw + 0.6f, 0.9f, len + 0.6f));
        }
    }

    /// A KICK WALL: a fin on a platform at `x`, `h` tall, its broad faces looking across the lane, a cream flute on each
    public static void ChaseKick(Transform parent, ChaseKind k, float x, float y, float z, float h)
    {
        h = Mathf.Clamp(h, 3f, 6f);
        Vector3 c = new Vector3(x, y + h * 0.5f - Sink, z), size = new Vector3(0.4f, h, 3.2f);
        Slab(parent, c, size, ChaseWallMat(k), "KickWall");
        for (int s = -1; s <= 1; s += 2)
            Prim(parent, new Vector3(x + s * 0.22f, c.y, z), new Vector3(0.04f, h - 0.3f, 0.7f), Fluted, "KickFlute");
        ChaseMarkCorners(parent, c, size);
    }

    /// A FLUTED PILLAR: an edge-boost corner on a platform, every vertical edge fluted and marked
    public static void ChasePillar(Transform parent, ChaseKind k, float x, float y, float z, float h)
    {
        h = Mathf.Max(h, 4f);
        Vector3 c = new Vector3(x, y + h * 0.5f - Sink, z), size = new Vector3(1.2f, h, 1.2f);
        Slab(parent, c, size, ChaseSolidMat(k), "BoostPillar");
        Flutes(parent, c, size);
        Prim(parent, new Vector3(x, y + h - Sink + 0.15f, z), new Vector3(1.5f, 0.3f, 1.5f), Brass, "PillarCapital");
        ChaseMarkCorners(parent, c, size);
    }

    /// A DECK: a raised split deck ten metres long on each platform, `h` up: a 22° ramp on the inner half (the slide
    /// line: a slide-hop up it keeps the speed), a lip on the outer half (the corner boost), a drop at its far end
    public static void ChaseDeck(Transform parent, ChaseKind k, float y, float z, float h)
    {
        const float DeckLen = 10f;
        float run = h / Mathf.Tan(22f * Mathf.Deg2Rad);
        float innerW = PlatW * 0.46f, outerW = PlatW - innerW - 0.4f;
        float top = y + h - Sink;
        for (int s = -1; s <= 1; s += 2)
        {
            float xi = s * (Chase.ChanHalf + 0.1f + innerW * 0.5f), xo = s * (Chase.HalfW - 0.2f - outerW * 0.5f);
            Slab(parent, new Vector3(xi, y + h * 0.5f - Sink, z + run + DeckLen * 0.5f), new Vector3(innerW, h, DeckLen), ChaseSolidMat(k), "DeckInner");
            Ramp(parent, xi, innerW - 0.1f, z, y + 0.012f, z + run, top + 0.012f, ChaseSolidMat(k));
            Vector3 lc = new Vector3(xo, y + h * 0.5f - Sink, z + run * 0.5f + DeckLen * 0.5f);
            Slab(parent, lc, new Vector3(outerW, h, run + DeckLen), ChaseSolidMat(k), "DeckLip");
            Prim(parent, new Vector3(xo, top + MarkProud + 0.012f, z + 0.09f), new Vector3(outerW - 0.08f, 0.024f, 0.14f), Brass, "LipBrass");
            ChaseMarkLip(parent, new Vector3(xo, top, z + 0.8f), new Vector3(outerW + 0.6f, 0.9f, 2.2f));
            Prim(parent, new Vector3(xo, top + MarkProud, z + run * 0.5f + DeckLen * 0.5f), new Vector3(0.1f, 0.006f, run + DeckLen - 0.3f), Brass, "BrassInlay");
        }
    }

    /// GLASS at `z`: a pane across each platform, floor to 3.9 m (the channel between them is the way round)
    public static void ChaseGlass(Transform parent, MovementConfig config, float y, float z)
    {
        for (int s = -1; s <= 1; s += 2)
            Glass(parent, new Vector3(PlatX(s), y + 2f, z), new Vector3(PlatW - 0.6f, 3.8f, 0.1f), config);
    }

    /// A SIGNAL GANTRY over the channel: two riveted posts on the platform edges (kick walls, corners marked), a girder
    /// at five metres with a brass lip line on it, two signal heads, a red lamp
    public static void ChaseGantry(Transform parent, float y, float z)
    {
        const float H = 5f;
        float px = Chase.ChanHalf + 1.3f;
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 c = new Vector3(s * px, y + H * 0.5f - Sink, z), size = new Vector3(0.9f, H, 0.9f);
            Slab(parent, c, size, RivetIron, "GantryPost");
            Flute(parent, new Vector3(s * (px - 0.45f), c.y, z), Vector3.left * s, H - 1f, 0.5f);
            ChaseMarkCorners(parent, c, size);
            Vector3 head = new Vector3(s * (px - 0.2f), y + H - 1.2f, z - 0.5f);
            Prim(parent, head, new Vector3(0.4f, 1.3f, 0.3f), Graphite, "SignalHead");
            Prim(parent, head + new Vector3(0f, 0.32f, -0.17f), Vector3.one * 0.26f, SignalRed, "SignalRed", PrimitiveType.Sphere);
            Prim(parent, head - new Vector3(0f, 0.32f, 0.17f), Vector3.one * 0.26f, SignalAmber, "SignalAmber", PrimitiveType.Sphere);
        }
        Slab(parent, new Vector3(0f, y + H + 0.25f - Sink, z), new Vector3(px * 2f + 0.9f, 0.5f, 0.7f), Girder, "GantryBeam");
        Prim(parent, new Vector3(0f, y + H + 0.5f - Sink + MarkProud + 0.012f, z), new Vector3(px * 2f + 0.8f, 0.024f, 0.14f), Brass, "LipBrass");
        ChaseMarkLip(parent, new Vector3(0f, y + H + 0.5f - Sink, z), new Vector3(px * 2f + 1.4f, 0.9f, 1.3f));
        var l = Light(parent, new Vector3(0f, y + H - 1f, z - 1f), 9f, 1.1f);
        l.color = new Color(1f, 0.32f, 0.18f);
    }

    /// What the vehicle rides, the length of the chunk (over the gaps too: a narrow line the brave may walk): the
    /// House's brass luggage rail down the channel; the terminus's track — bed, two rails, the third rail on its pots
    /// in a live amber (the look; phase 3 lays the real one over it)
    public static void ChaseRail(Transform parent, ChaseKind k, float railY, float z0, float z1)
    {
        float len = z1 - z0, zm = (z0 + z1) * 0.5f;
        if (k == ChaseKind.Concierge)
        {
            Slab(parent, new Vector3(0f, railY + 0.1f, zm), new Vector3(0.3f, 0.2f, len), Brass, "LuggageRail");
            for (float z = z0 + 2f; z < z1 - 1f; z += 4f)
                Prim(parent, new Vector3(0f, railY + 0.03f, z), new Vector3(1.2f, 0.06f, 0.24f), Bronze, "RailTie");
            return;
        }
        Prim(parent, new Vector3(0f, railY + 0.04f, zm), new Vector3(2.2f, 0.08f, len), Charcoal, "SleeperBed");
        for (int s = -1; s <= 1; s += 2)
        {
            Slab(parent, new Vector3(s * 0.75f, railY + 0.25f, zm), new Vector3(0.08f, 0.14f, len), RivetIron, "Rail");
            Prim(parent, new Vector3(s * 0.75f, railY + 0.19f, zm), new Vector3(0.16f, 0.02f, len), RivetIron, "RailFoot");
        }
        Prim(parent, new Vector3(1.25f, railY + 0.32f, zm), new Vector3(0.09f, 0.1f, len), SignalAmber, "ThirdRailLook");
        for (float z = z0 + 1.5f; z < z1 - 1f; z += 6f)
            Prim(parent, new Vector3(1.25f, railY + 0.15f, z), new Vector3(0.12f, 0.16f, 0.12f), Cream, "RailPot", PrimitiveType.Cylinder);
    }

    /// THE CONCIERGE'S LUGGAGE, dropped off the trolley: a trunk 1.2 m tall (a lip) with a hatbox on it (a step), brass bands
    public static void ChaseLuggage(Transform parent, float y, float x, float z)
    {
        Vector3 c = new Vector3(x, y + 0.6f - Sink, z);
        Slab(parent, c, new Vector3(2.4f, 1.2f, 1.6f), WoodPanel, "LuggageLip");
        for (int s = -1; s <= 1; s += 2)
            Prim(parent, new Vector3(x + s * 0.7f, c.y, z), new Vector3(0.1f, 1.24f, 1.64f), Brass, "TrunkBand");
        float top = y + 1.2f - Sink;
        Prim(parent, new Vector3(x, top + MarkProud + 0.012f, z - 0.71f), new Vector3(2.32f, 0.024f, 0.14f), Brass, "LipBrass");
        ChaseMarkLip(parent, new Vector3(x, top, z), new Vector3(3f, 0.9f, 2.2f));
        Slab(parent, new Vector3(x + 0.5f, top + 0.3f - Sink, z + 0.2f), new Vector3(0.9f, 0.6f, 0.9f), Oxblood, "Hatbox");
    }
}
