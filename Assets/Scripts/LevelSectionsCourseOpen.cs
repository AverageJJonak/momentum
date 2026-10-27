using UnityEngine;
using Cursor = LevelSections.Cursor;

/// <summary>
/// THE COURSE, the open ones: ROOF, COURT, BANK, BRIDGE. See LevelSectionsCourse.cs for the grammar.
/// </summary>
public static partial class Course
{
    /// ROOF. Open sky. Decks with gaps between them: 6 m across the middle (the main line's run jump), 11 m on the
    /// west third (a parapet LIP before it: corner-boost and hop it), a kick stack on the east third's edge (kick it
    /// mid-gap). Under every gap a catch floor with pads that throw you back onto the next deck; along both sides
    /// a service gantry 4.5 m down with a pad per deck, and the neighbours' walls past it (their tops are lips).
    /// Parapets along the decks' outer edges, skylight boxes and chimneys to boost on and kick. Nothing falls off.
    /// FUEL: a page over every fast gap (parry-pogo across), a bellhop on the parapet lip, footmen on the decks.
    public static Cursor Roof(Transform root, MovementConfig config, Cursor c, float len, float halfW, int gaps)
    {
        Begin(c);
        halfW = Mathf.Max(halfW, 8f);
        gaps = Mathf.Clamp(gaps, 0, 6);
        len = Mathf.Max(len, 40f + gaps * 24f);
        float z0 = c.z, z1 = z0 + len, y = c.floorY;
        const float deckT = 5.5f, catchY = -5f, gantryY = -4.5f, gantryW = 3f;
        float ext = (FastGap - RunGap) * 0.5f, third = halfW * 2f / 3f;
        Threshold(root, c, halfW, y, Head);
        var zone = c.zone;
        float segLen = (len - gaps * RunGap) / (gaps + 1);
        for (int i = 0; i <= gaps; i++)
        {
            float a = z0 + i * (segLen + RunGap), b = a + segLen;
            float cutS = i > 0 ? ext : 0f, cutN = i < gaps ? ext : 0f;
            // The deck: its centre and east two thirds the full segment, its west third cut back at the gaps; the
            // first and the last run a wall's thickness into the seams
            float ex0 = -halfW + third, ex1 = halfW;
            float zs = a - (i == 0 ? T : 0f), ze = b + (i == gaps ? T : 0f);
            LevelKit.FloorSlab(root, new Vector3((ex0 + ex1) * 0.5f, y - deckT * 0.5f, (zs + ze) * 0.5f), new Vector3(ex1 - ex0, deckT, ze - zs), "RoofDeck");
            float wa = a + cutS, wb = b - cutN;
            float ws = wa - (i == 0 ? T : 0f), we = wb + (i == gaps ? T : 0f);
            LevelKit.FloorSlab(root, new Vector3(-halfW + third * 0.5f, y - deckT * 0.5f, (ws + we) * 0.5f), new Vector3(third, deckT, we - ws), "RoofDeck");
            // Parapets along the outer edges, the lip before the fast gap, the stack at the east edge of the gap
            for (int s = -1; s <= 1; s += 2)
            {
                float pa = (s < 0 ? wa : a) + (i == 0 ? 2.6f : 0.5f), pb = (s < 0 ? wb : b) - 0.5f;   // clear of the arch's piers at the start
                LevelKit.Parapet(root, new Vector3(s * (halfW - 0.4f), y + 0.55f, (pa + pb) * 0.5f), new Vector3(0.8f, 1.1f, pb - pa));
            }
            if (i < gaps)
            {
                LevelKit.Lip(root, new Vector3(-halfW + third * 0.5f + 0.4f, y + 0.6f, wb - 0.5f), new Vector3(third - 1.6f, 1.2f, 0.8f), LevelKit.Limestone, "ParapetLedge");
                LevelKit.KickStack(root, new Vector3(halfW - third * 0.5f, y, b - 1.4f), 4.5f, 1.8f);
                LevelKit.LineRun(root, new Vector3(-halfW + third * 0.5f, y, wa + 3f), new Vector3(-halfW + third * 0.5f, y, wb - 1.2f), zone);
                Fuel(root, config, c, EnemyKind.Turret, new Vector3(-halfW + third * 0.5f, y + 3f, b + 3f), "fast gap " + (i + 1));
                Fuel(root, config, c, EnemyKind.Chaser, new Vector3(-halfW + third * 0.5f, y + 1.2f, wb - 0.5f), "parapet lip " + (i + 1));
                // The catch floor under the gap and its pads: 6 m under the deck, thrown back up and on
                float ca = b - 3f, cb = b + RunGap + ext + 3f;
                LevelKit.FloorSlab(root, new Vector3(0f, y + catchY - 0.5f, (ca + cb) * 0.5f), new Vector3(halfW * 2f, 1f, cb - ca), "CatchFloor");
                LevelKit.JumpPad(root, new Vector3(0f, y + catchY, b + RunGap - 1.5f), 0f, 17.5f, 7f);              // in the main gap, under its far edge
                LevelKit.JumpPad(root, new Vector3(-halfW + third * 0.5f, y + catchY, b + RunGap + 1f), 0f, 17.5f, 7f);   // in the fast gap, under its far edge
            }
            // The roof's furniture: a skylight box (a lip), a chimney (a kick), a water tower (a look)
            if (segLen >= 18f)
            {
                LevelKit.Lip(root, new Vector3(-halfW * 0.45f, y + 0.75f, a + segLen * 0.55f), new Vector3(4f, 1.5f, 3f), LevelKit.Limestone, "SkylightLedge");
                LevelKit.KickStack(root, new Vector3(halfW * 0.3f, y, a + segLen * 0.3f), 3.5f, 1.5f);
                if (segLen >= 28f)
                {
                    // The tower is a look; the core under its tank is solid, so nothing runs through it
                    LevelKit.WaterTower(root, new Vector3(halfW * 0.62f, y, a + segLen * 0.7f), 6f);
                    LevelKit.Slab(root, new Vector3(halfW * 0.62f, y + 1.5f, a + segLen * 0.7f), new Vector3(1.4f, 3f, 1.4f), LevelKit.RivetIron, "TowerCore");
                }
            }
            if (i > 0) PoolFuel(config, new Vector3(R(-3f, 3f), y, a + segLen * 0.5f), "deck " + (i + 1));
        }
        // The gantries down both sides, a pad per deck, the neighbours' walls past them, capped at both ends
        for (int s = -1; s <= 1; s += 2)
        {
            float gx = s * (halfW + gantryW * 0.5f);
            LevelKit.FloorSlab(root, new Vector3(gx, y + gantryY - 0.5f, (z0 + z1) * 0.5f), new Vector3(gantryW, 1f, len), "GantryDeck");
            for (int i = 0; i <= gaps; i++) LevelKit.JumpPad(root, new Vector3(gx, y + gantryY, z0 + i * (segLen + RunGap) + segLen * 0.5f), 0f, 16.5f, 4f);
            float wx = s * (halfW + gantryW + 0.5f), wy0 = y + gantryY - 1f, wy1 = y + 1.5f;
            LevelKit.Slab(root, new Vector3(wx, (wy0 + wy1) * 0.5f, (z0 + z1) * 0.5f), new Vector3(1f, wy1 - wy0, len), LevelKit.Wall, "Wall");
            LevelKit.Slab(root, new Vector3(s * (halfW + gantryW * 0.5f + 0.25f), (wy0 + wy1) * 0.5f, z0 + 0.6f), new Vector3(gantryW + 0.5f, wy1 - wy0, 1f), LevelKit.Wall, "Wall");
            LevelKit.Slab(root, new Vector3(s * (halfW + gantryW * 0.5f + 0.25f), (wy0 + wy1) * 0.5f, z1 - 0.6f), new Vector3(gantryW + 0.5f, wy1 - wy0, 1f), LevelKit.Wall, "Wall");
        }
        Dressing(root, c, halfW, y, Head, z0, z1, true, true);   // bare: a roof has no walls for the zone's trims
        LevelSections.Postcard(new Vector3(-halfW * 0.7f, y + 6f, z0 + 4f), new Vector3(halfW * 0.2f, y + 1f, z0 + len * 0.5f), 78f);
        return Open(c, z1, y, halfW, Head);
    }

    /// COURT. An open yard under the sky: a stepped plinth in the middle (its tiers are lips, the orb over it),
    /// and exits at different heights — the lane itself, and up to two terraces along the walls, each ending in a
    /// 30° slide down to the lane at the way out. The low terrace (3.4 m) takes a perfect corner boost on its
    /// plinth lip; the high one (8 m) a pad, or the orb.
    /// FUEL: three roster units on the floor, a page over the plinth, a bellhop on its top tier, footmen on the terraces.
    public static Cursor Court(Transform root, MovementConfig config, Cursor c, float len, float halfW, int exits)
    {
        Begin(c);
        halfW = Mathf.Max(halfW, 11f);
        len = Mathf.Max(len, 60f);
        exits = Mathf.Clamp(exits, 1, 3);
        const float height = 14f;
        float z0 = c.z, z1 = z0 + len, y = c.floorY;
        Threshold(root, c, halfW, y, height);
        Shell(root, c, halfW, y, height, z0, z1, Yard);
        var zone = c.zone;
        Vector3 mid = new Vector3(0f, y, z0 + len * 0.5f);
        LevelKit.SteppedPlinth(root, mid, 9f, 2, 1.2f);
        LevelKit.Orb(root, mid + Vector3.up * 9f);
        if (exits >= 2) Terrace(root, config, c, -1, 3.4f, halfW, y, z0, z1, len, zone);
        if (exits >= 3) Terrace(root, config, c, 1, 8f, halfW, y, z0, z1, len, zone);
        // Three roster units on the floor, clear of the plinth
        PoolFuel(config, new Vector3(-halfW * 0.5f + R(-2f, 2f), y, z0 + len * 0.2f), "yard");
        PoolFuel(config, new Vector3(halfW * 0.6f + R(-1f, 1f), y, z0 + len * 0.45f), "yard");
        PoolFuel(config, new Vector3(-halfW * 0.3f + R(-2f, 2f), y, z0 + len * 0.78f), "yard");
        Fuel(root, config, c, EnemyKind.Turret, mid + Vector3.up * 6f + Vector3.forward * 5f, "over the plinth");
        Fuel(root, config, c, EnemyKind.Chaser, mid + Vector3.up * 2.4f, "plinth lip");
        for (int s = -1; s <= 1; s += 2) LevelKit.GlobeLamp(root, new Vector3(s * (halfW - 1.2f), y + 6f, z0 + len * 0.25f), y + 7f);
        Dressing(root, c, halfW, y, height, z0, z1, true);
        LevelSections.Postcard(new Vector3(-halfW * 0.6f, y + 9f, z0 + 5f), mid + Vector3.up * 3f, 76f);
        return Open(c, z1, y, halfW, height);
    }

    static void Terrace(Transform root, MovementConfig config, Cursor c, int side, float h, float halfW, float y, float z0, float z1, float len, Zone zone)
    {
        const float w = 7f;
        float x = side * (halfW - w * 0.5f);
        float slideRun = h / Mathf.Tan(30f * Mathf.Deg2Rad);
        float tz1 = z1 - 4f - slideRun, tz0 = z0 + len * 0.45f;
        LevelKit.FloorSlab(root, new Vector3(x, y + h - 0.5f, (tz0 + tz1) * 0.5f), new Vector3(w, 1f, tz1 - tz0), "Terrace");
        LevelKit.Ramp(root, x, w, tz1, y + h, z1 - 4f, y, LevelKit.Slope);
        LevelKit.Parapet(root, new Vector3(x - side * (w * 0.5f - 0.3f), y + h + 0.45f, (tz0 + tz1) * 0.5f), new Vector3(0.6f, 0.9f, tz1 - tz0 - 1f));
        if (h < 5f)
        {
            LevelKit.Lip(root, new Vector3(x, y + 0.65f, tz0 - 3.5f), new Vector3(w - 1f, 1.3f, 3f));
            LevelKit.LineRun(root, new Vector3(x, y, tz0 - 14f), new Vector3(x, y, tz0 - 5.2f), zone);
            Fuel(root, config, c, EnemyKind.Chaser, new Vector3(x, y + 1.3f, tz0 - 3.5f), "terrace lip");
        }
        else LevelKit.JumpPad(root, new Vector3(x, y, tz0 - 5f), 0f, Mathf.Sqrt(2f * 25f * (h + 1.2f)), 6f);
        for (int i = 0; i < 2; i++) Fuel(root, config, c, EnemyKind.Dummy, new Vector3(x, y + h, tz0 + 4f + i * 6f), "terrace " + (side < 0 ? "west" : "east"));
    }

    /// BANK. An S-curve inside the section: two half-walls make a chicane (the first from the west wall, the second
    /// from the east; each 4 m tall, a kick wall, its free end an edge-boost corner), and on the outside of each
    /// turn a BANKED wall leaning in over the lane so speed survives the turn. The box is `halfW + swing` wide.
    /// MAIN: round the chicane, lane ≥ halfW + 0.6·swing. FAST: edge-boost round each half-wall's end, straight
    /// across. The racing line is lit on the floor.
    /// FUEL: a doorman in the first lane, a bellhop in the second, a page over the middle.
    public static Cursor Bank(Transform root, MovementConfig config, Cursor c, float len, float halfW, float swing)
    {
        Begin(c);
        halfW = Mathf.Max(halfW, 7f);
        len = Mathf.Max(len, 60f);
        swing = Mathf.Clamp(swing, 4f, halfW);
        float boxW = halfW + swing;
        float z0 = c.z, z1 = z0 + len, y = c.floorY, height = Head;
        Threshold(root, c, boxW, y, height);
        Shell(root, c, boxW, y, height, z0, z1, Roofed);
        var zone = c.zone;
        float k1 = z0 + len * 0.3f, k2 = z0 + len * 0.68f;
        HalfWall(root, -1, boxW, swing * 0.4f, y, k1);
        HalfWall(root, 1, boxW, -swing * 0.4f, y, k2);
        Banked(root, 1, boxW, y, k1 + 10f, 16f);
        Banked(root, -1, boxW, y, k2 + 10f, 16f);
        LevelKit.LineRun(root, new Vector3(0f, y, z0 + 6f), new Vector3(swing * 0.75f, y, k1), zone);
        LevelKit.LineRun(root, new Vector3(swing * 0.75f, y, k1), new Vector3(-swing * 0.75f, y, k2), zone);
        LevelKit.LineRun(root, new Vector3(-swing * 0.75f, y, k2), new Vector3(0f, y, z1 - 4f), zone);
        Fuel(root, config, c, EnemyKind.Sentry, new Vector3(swing * 0.7f, y, k1 + 3f), "first turn");
        Fuel(root, config, c, EnemyKind.Chaser, new Vector3(-swing * 0.7f, y, k2 + 3f), "second turn");
        Fuel(root, config, c, EnemyKind.Turret, new Vector3(0f, y + 4f, (k1 + k2) * 0.5f), "over the bank");
        for (float z = z0 + 12f; z < z1 - 8f; z += 24f) LevelKit.GlobeLamp(root, new Vector3(0f, y + height - 2.6f, z), y + height);
        Dressing(root, c, boxW, y, height, z0, z1, false);
        return Open(c, z1, y, boxW, height);
    }

    /// A half-wall from the `side` wall to `endX`: a kick wall whose free end is a corner
    static void HalfWall(Transform root, int side, float boxW, float endX, float y, float z)
    {
        float from = side * boxW;
        float w = Mathf.Abs(from - endX), xc = (from + endX) * 0.5f;
        var wall = LevelKit.KickWall(root, new Vector3(xc, y + 2f, z), new Vector3(w, 4f, 1.2f));
        TechMarkers.MarkBoxCorners(wall);
        LevelKit.Flutes(root, new Vector3(xc, y + 2f, z), new Vector3(w, 4f, 1.2f));
    }

    /// A banked wall on the `side` wall: a slab leaning 20° in over the lane, 6 m tall, `len` long, centred on `z`
    static void Banked(Transform root, int side, float boxW, float y, float z, float len)
    {
        const float h = 6f, lean = 20f;
        Quaternion rot = Quaternion.Euler(0f, 0f, side * lean);
        Vector3 foot = new Vector3(side * (boxW - 0.35f), y, z);
        Vector3 up = rot * Vector3.up;
        var go = LevelKit.Slab(root, foot + up * (h * 0.5f), new Vector3(0.6f, h, len), LevelKit.Wall, "BankWall");
        go.transform.rotation = rot;
    }

    /// BRIDGE. An 8 m deck over a void 9 m deep, a gap in the middle of it (the main line's run jump at 6 m); over
    /// it a 3 m span 4.4 m up with an 11 m gap (the fast line over: a corner boost on the plinth lip at its start,
    /// a hop at speed across, a ramp off its end); under it the void's floor is a slide down to a pad that throws
    /// you back onto the exit landing (the fast line under: drop, slide, pad). Nothing falls further than the slide.
    /// FUEL: a page in the gap (parry-pogo across), two footmen on the deck, a valet on the floor below.
    public static Cursor Bridge(Transform root, MovementConfig config, Cursor c, float len, float halfW, float gap)
    {
        Begin(c);
        halfW = Mathf.Max(halfW, 8f);
        len = Mathf.Max(len, 70f);
        gap = Mathf.Clamp(gap, 4f, 8f);
        const float deep = 9f, landing = 6f, dw = 9f, uw = 3f;   // the deck keeps a 6 m lane beside the span's lip
        float z0 = c.z, z1 = z0 + len, y = c.floorY, low = y - deep, height = Head;
        Threshold(root, c, halfW, y, height);
        Shell(root, c, halfW, low, deep + height, z0, z1, Walled);
        var zone = c.zone;
        float mid = z0 + len * 0.5f;
        // The landings, and the void closed under each of them
        LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, z0 + landing * 0.5f), new Vector3(halfW * 2f + 2f * T, 1f, landing + T), "Landing");
        LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, z1 - landing * 0.5f), new Vector3(halfW * 2f + 2f * T, 1f, landing + T), "Landing");
        LevelKit.WallFace(root, Face.North, z0 + landing, -halfW - T * 0.5f, halfW + T * 0.5f, low, y - 1f);
        LevelKit.WallFace(root, Face.South, z1 - landing, -halfW - T * 0.5f, halfW + T * 0.5f, low, y - 1f);
        // The deck and its gap
        float dA0 = z0 + landing - 0.5f, dA1 = mid - gap * 0.5f, dB0 = mid + gap * 0.5f, dB1 = z1 - landing + 0.5f;
        LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, (dA0 + dA1) * 0.5f), new Vector3(dw, 1f, dA1 - dA0), "BridgeDeck");
        LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, (dB0 + dB1) * 0.5f), new Vector3(dw, 1f, dB1 - dB0), "BridgeDeck");
        // The span over: the plinth lip, the two halves, the ramp off the end; bronze posts under it
        float uy = y + 4.4f, ux = -dw * 0.5f + uw * 0.5f;
        float u0 = z0 + landing + 6f, uEnd = z1 - landing - Run(4.4f) - 1f;
        LevelKit.Lip(root, new Vector3(ux, y + 0.7f, u0 - 3.5f), new Vector3(uw, 1.4f, 3f));
        LevelKit.FloorSlab(root, new Vector3(ux, uy - 0.4f, (u0 + mid - FastGap * 0.5f) * 0.5f), new Vector3(uw, 0.8f, mid - FastGap * 0.5f - u0), "SpanDeck");
        LevelKit.FloorSlab(root, new Vector3(ux, uy - 0.4f, (mid + FastGap * 0.5f + uEnd) * 0.5f), new Vector3(uw, 0.8f, uEnd - mid - FastGap * 0.5f), "SpanDeck");
        LevelKit.Ramp(root, ux, uw, uEnd, uy, z1 - landing - 1f, y);
        for (float z = u0 + 2f; z < uEnd - 2f; z += 8f)
            if (Mathf.Abs(z - mid) > FastGap * 0.5f + 1f)
                LevelKit.Prim(root, new Vector3(ux, y + 2.2f, z), new Vector3(0.25f, 3.6f, 0.25f), LevelKit.Bronze, "SpanPost");
        LevelKit.LineRun(root, new Vector3(ux, y, dA0 + 1f), new Vector3(ux, y, u0 - 5.2f), zone);
        // The void's floor: a slide from 4 m under the south landing to the bottom, a flat with the pad at the end
        float v0 = z0 + landing, v1 = z1 - landing - 6f;
        LevelKit.Ramp(root, 0f, halfW * 2f, v0, y - 4f, v1, low, LevelKit.Slope);
        LevelKit.FloorSlab(root, new Vector3(0f, low - 0.5f, (v1 + z1 - landing) * 0.5f), new Vector3(halfW * 2f + 2f * T, 1f, z1 - landing - v1 + T), "Landing");
        LevelKit.JumpPad(root, new Vector3(0f, low, z1 - landing - 2.5f), 0f, 23f, 8f);
        for (int s = -1; s <= 1; s += 2) LevelKit.Flute(root, new Vector3(s * halfW, y - 2f, mid), Vector3.left * s, 4f);
        Fuel(root, config, c, EnemyKind.Turret, new Vector3(0f, y + 2.5f, mid), "the gap");
        for (int s = -1; s <= 1; s += 2) Fuel(root, config, c, EnemyKind.Dummy, new Vector3(s * 2.5f, y, dA0 + 8f + (s + 1) * 2f), "deck");
        Fuel(root, config, c, EnemyKind.Lurker, new Vector3(R(-4f, 4f), Mathf.Lerp(y - 4f, low, 0.6f) + 0.2f, Mathf.Lerp(v0, v1, 0.6f)), "below");
        LevelKit.GlobeLamp(root, new Vector3(0f, y + height - 2.6f, mid), y + height);
        Dressing(root, c, halfW, low, deep + height, z0, z1, false);
        return Open(c, z1, y, halfW, height);
    }
}
