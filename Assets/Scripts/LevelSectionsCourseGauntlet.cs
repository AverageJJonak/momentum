using System.Collections.Generic;
using UnityEngine;
using Cursor = LevelSections.Cursor;

/// <summary>
/// THE COURSE SETS (2026-10-26/27, user: "look to vholume for level design", three times). What VHOLUME's courses are,
/// from its reviews, its guides and its players' screenshots: SHORT (a minute or two, timed against an author's time),
/// CLEAN (big simple shapes you can read at speed — a stair, a pillar or a parapet that catches you erases seconds, so
/// nothing stands in the lane that is not meant to), built on a CHAIN of speed ("drop into a downhill section, build
/// speed, launch from the edge, aim at a wall, wall run the gap, bounce off it, land into a slide"), with BRANCHES (an
/// upper line for the tech, a lower line that is slower and safe, both real routes), through INTERIORS and EXTERIORS
/// in turn (a room entered by its window, a corridor, a stair down, out onto roofs, up a shaft, along a tall wall),
/// with poles to ping-pong between and ledges to clamber, no machines — the hazards are the fall and the collision.
/// Every set here is one of those ideas, standing on a STREET nine metres under the line (a miss lands there, a pad
/// throws you back on), in the act's Deco materials, lit. A level is five of them, the DROP always first.
///
/// The words a score uses (GauntletLevels):
///   DROP        the opening: a slope down, a launch off its edge over a gap, a wall to run the gap along, a landing
///   TERRACES    a stepped building: blocks from the street at their own heights, gaps where steps are missing
///   MONOLITHS   a hall: beams the size of houses on columns between two great walls, under a slab ceiling
///   CANYON      two faces eleven metres apart, forty tall: ledges alternate sides, the faces are the run
///   SHAFT       a shaft twenty-six metres tall with openings, two pits, lips up the faces, one light each
///   CHUTE       a slide down a building's flank, a gap at its foot
///   DRAUGHT     a tower climbed from inside: the fan's updraft the easy way, the lip ladder the fast one
///   STAIRS      a grand stair up as a launch ramp: a long flight, a landing, the drop off its top into a slide
///   POLES       poles to ping-pong between over the street: the upper line; the street is the lower
///   ROOFS       building tops at their own heights, chimneys to kick, ledges to clamber, the street between
///   APARTMENTS  a building run through: in by a window, across a room, down through its floor, along a corridor, out
/// </summary>
public static partial class Course
{
    public enum SetKind { Drop, Terraces, Monoliths, Canyon, Shaft, Chute, Draught, Stairs, Poles, Roofs, Apartments, Hall, Well, Plaza, Street, Span }

    public static readonly SetKind[] DefaultOrder = { SetKind.Drop, SetKind.Terraces, SetKind.Canyon, SetKind.Stairs, SetKind.Roofs };

    const float GroundDrop = 9f;     // the street under every set
    const float DeckLen = 8f;        // the landing deck after every set
    const float PadUp = 19f, PadOn = 6f;

    /// `sets` sets along the cursor (`order` names them; null cycles the default), the lane `halfW` each side;
    /// returns the cursor open on the last landing deck
    public static Cursor Gauntlet(Transform root, MovementConfig config, Cursor c, int sets, float halfW = 12f, string name = "THE COURSE", SetKind[] order = null)
    {
        Begin(c);
        if (order != null && order.Length > 0) sets = order.Length;
        sets = Mathf.Clamp(sets, 1, 20);
        halfW = Mathf.Max(halfW, 9f);
        float y = c.floorY, z = c.z, zStart = z, yMin = y, yMax = y;
        Threshold(root, c, halfW, y, Head);
        Deck(root, halfW, y, z - T, DeckLen + T);
        z += DeckLen;
        for (int i = 0; i < sets; i++)
        {
            float d = sets > 1 ? i / (float)(sets - 1) : 0.5f;
            var kind = order != null && order.Length > 0 ? order[i] : DefaultOrder[i % DefaultOrder.Length];
            var cs = c; cs.z = z - 5f; cs.floorY = y; cs.halfW = halfW;   // the checkpoint stands on the deck behind the set: a respawn lands on stone
            Checkpoint(root, cs, name + " · " + Roman(i + 1) + " · " + Title(kind));
            cs.z = z;
            Pylons(root, halfW, y, z);
            switch (kind)
            {
                case SetKind.Drop: z = Drop(root, config, cs, halfW, ref y, d); break;
                case SetKind.Terraces: z = Terraces(root, config, cs, halfW, ref y, d); break;
                case SetKind.Monoliths: z = Monoliths(root, config, cs, halfW, ref y, d); break;
                case SetKind.Canyon: z = Canyon(root, config, cs, halfW, ref y, d); break;
                case SetKind.Shaft: z = ShaftSet(root, config, cs, halfW, ref y, d); break;
                case SetKind.Chute: z = ChuteSet(root, config, cs, halfW, ref y, d); break;
                case SetKind.Draught: z = Draught(root, config, cs, halfW, ref y, d); break;
                case SetKind.Stairs: z = Stairs(root, config, cs, halfW, ref y, d); break;
                case SetKind.Poles: z = Poles(root, config, cs, halfW, ref y, d); break;
                case SetKind.Roofs: z = Roofs(root, config, cs, halfW, ref y, d); break;
                case SetKind.Hall: z = HallOfBeams(root, config, cs, halfW, ref y, d); break;
                case SetKind.Well: z = Well(root, config, cs, halfW, ref y, d); break;
                case SetKind.Plaza: z = Plaza(root, config, cs, halfW, ref y, d); break;
                case SetKind.Street: z = Street(root, config, cs, halfW, ref y, d); break;
                case SetKind.Span: z = Span(root, config, cs, halfW, ref y, d); break;
                default: z = Apartments(root, config, cs, halfW, ref y, d); break;
            }
            Deck(root, halfW, y, z, DeckLen);
            z += DeckLen;
            yMin = Mathf.Min(yMin, y); yMax = Mathf.Max(yMax, y);
        }
        var cd = c; cd.floorY = yMin;
        Dressing(root, cd, halfW + 14f, yMin - GroundDrop - 2f, yMax - yMin + GroundDrop + 40f, zStart, z, true, true);
        if (c.zone == Zone.Brutal) LevelKit.BrutalVista(root, zStart, z, yMin, halfW);
        LevelSections.Postcard(new Vector3(-halfW * 1.2f, c.floorY + 12f, zStart + 4f), new Vector3(0f, c.floorY - 4f, zStart + 60f), 74f);
        return Open(c, z, y, halfW, Head);
    }

    // ------------------------------------------------------------ the sets

    /// THE DROP, the opening of every course and the chain the whole game is built on: a 28° slope down twelve
    /// metres, a lip at its foot to launch from, a gap of nine to twelve metres, a wall down one side of the gap to
    /// run it along (and kick off at its end), the landing a little lower so the jump carries, the street under it
    /// with a pad. Taken flat out it is one movement; taken slow it is a slide, a jump and a wall.
    static float Drop(Transform root, MovementConfig config, Cursor c, float halfW, ref float y, float d)
    {
        float drop = 12f, tan = Mathf.Tan(28f * Mathf.Deg2Rad), len = drop / tan, z0 = c.z, z1 = z0 + len, w = halfW * 2f;
        float yFoot = y - drop, gap = 9f + 3f * d, yLand = yFoot - 2f, ground = yLand - GroundDrop;
        LevelKit.Ramp(root, 0f, w, z0, y, z1, yFoot);
        Block(root, 0f, ground, yFoot - 1.2f, (z0 + z1) * 0.5f, w, len, "DropPlinth");
        LevelKit.FloorSlab(root, new Vector3(0f, yFoot - 0.5f, z1 + 1.5f), new Vector3(w, 1f, 3f), "DropLip");
        Block(root, 0f, ground, yFoot - 1f, z1 + 1.5f, w, 3f, "LipPlinth");
        LevelKit.LineRun(root, new Vector3(0f, yFoot, z1 + 0.3f), new Vector3(0f, yFoot, z1 + 2.7f), c.zone);
        // The wall along the gap: a face eight metres tall down one side, the run across; the street and its pad under
        int side = rng.NextDouble() < 0.5 ? -1 : 1;
        float wallZ0 = z1 - 2f, wallZ1 = z1 + 3f + gap + 4f;
        LevelKit.KickWall(root, new Vector3(side * (halfW - 0.5f), yFoot + 3f, (wallZ0 + wallZ1) * 0.5f), new Vector3(1f, 8f, wallZ1 - wallZ0), LevelKit.Wall);
        Ground(root, halfW, ground, z1 - 1f, z1 + 3f + gap + 3f);
        LevelKit.JumpPad(root, new Vector3(0f, ground, z1 + 3f + gap - 1.5f), 0f, PadUp, PadOn);
        for (float lz = z0 + 6f; lz < z1 + gap; lz += 12f) LevelKit.Light(root, new Vector3(-side * (halfW - 1.5f), y - (Mathf.Min(lz, z1) - z0) * tan + 3.5f, lz), 18f, 1.4f);
        y = yLand;
        return z1 + 3f + gap;
    }

    /// THE TERRACES: a building stepped down and up again — 6–9 blocks standing on the street, each its own height
    /// and footprint, the gaps between them from a table the difficulty picks from (a step missing), a lip on every
    /// third to corner-boost, the long blocks' flanks walls to run. Down one side a chain of three narrow piers,
    /// launched by a pad: the upper line, 12–13 m hops. Pads on the street under every gap.
    static float Terraces(Transform root, MovementConfig config, Cursor c, float halfW, ref float y, float d)
    {
        float z = c.z, x = 0f, yPad = y, ground = y - GroundDrop;
        int n = 6 + Mathf.RoundToInt(d * 3f);
        float[] easy = { 5f, 6f, 7f }, mid = { 6f, 7.5f, 9f }, hard = { 7f, 9f, 11f, 13f };
        float zFar = z;
        var gaps = new List<(float x, float z, float gap)>();
        for (int i = 0; i < n; i++)
        {
            float gap = Pick(d < 0.34f ? easy : d < 0.67f ? mid : hard);
            float w = R(6f, 10f), dp = R(5f, 12f);
            x = Mathf.Clamp(x + R(-4f, 4f), -(halfW - 5f), halfW - 5f);
            float dy = rng.NextDouble() < 0.45 ? R(0.6f, 1.4f) : R(-2.5f, 0f);
            if (gap > 10f) dy = Mathf.Min(dy, 0f);
            if (i == 0) dy = 0f;
            yPad += dy;
            float near = zFar + gap, far = near + dp, zc = (near + far) * 0.5f;
            Block(root, x, ground, yPad, zc, w, dp, "Terrace");
            LevelKit.LineRun(root, new Vector3(x, yPad, near + 0.3f), new Vector3(x, yPad, far - 0.3f), c.zone);
            if (i % 3 == 2) LevelKit.Lip(root, new Vector3(x, yPad + 0.5f, far - 0.4f), new Vector3(Mathf.Max(w - 1.5f, 2f), 1f, 0.8f), LevelKit.Limestone, "TerraceLip");
            gaps.Add((x, near, gap));
            zFar = far;
        }
        Ground(root, halfW, ground, c.z - 2f, zFar + 2f);
        foreach (var g in gaps) if (g.gap > 5.5f) LevelKit.JumpPad(root, new Vector3(g.x, ground, g.z + g.gap - 1.5f), 0f, PadUp, PadOn);
        int side = rng.NextDouble() < 0.5 ? -1 : 1;
        float xs = side * (halfW - 2f), zs = c.z + 14f, ys = y + 0.5f;
        LevelKit.JumpPad(root, new Vector3(xs, y, c.z + 4f), 0f, 15f, 9.5f);
        for (int k = 0; k < 3; k++)
        {
            Block(root, xs, ground, ys, zs, 3f, 3.2f, "Pier");
            LevelKit.BrassStrip(root, new Vector3(xs, ys + 0.01f, zs - 1.4f), new Vector3(xs, ys + 0.01f, zs + 1.4f));
            zs += R(12f, 13f); ys += 0.7f;
        }
        y = yPad;
        for (float lz = c.z + 8f; lz < zFar; lz += 18f) for (int s = -1; s <= 1; s += 2) LevelKit.Light(root, new Vector3(s * (halfW - 1f), ground + 4f, lz), 22f, 1.6f);
        return Mathf.Max(zFar, zs - 12f + 2f);
    }

    /// THE MONOLITHS: a hall. Two great walls thirty metres tall either side, a slab ceiling at twenty-four with a
    /// skylight down its middle, columns every twelve metres; between them 5–8 beams the size of houses, each on its
    /// own columns, each its own height, every third tilted eight degrees into a slide, gaps growing with the
    /// difficulty. The street under it all, pads under the gaps.
    static float Monoliths(Transform root, MovementConfig config, Cursor c, float halfW, ref float y, float d)
    {
        float z = c.z + 2f, x = 0f, yTop = y, tan8 = Mathf.Tan(8f * Mathf.Deg2Rad), ground = y - GroundDrop;
        int n = 5 + Mathf.RoundToInt(d * 3f);
        var padZ = new List<(float x, float z)>();
        for (int i = 0; i < n; i++)
        {
            float w = R(6f, 9f), h = R(5f, 7f), len = R(12f, 18f);
            float gap = i == 0 ? 0f : Mathf.Lerp(5f, 9f, d) + R(-1f, 1.5f);
            x = Mathf.Clamp(x + R(-4f, 4f), -(halfW - 5f), halfW - 5f);
            float dy = i == 0 ? 0f : rng.NextDouble() < 0.45 ? R(0.6f, 1.4f) : R(-3f, -0.5f);
            if (gap > 8f) dy = Mathf.Min(dy, 0f);
            yTop += dy;
            float z0 = z + gap, zc = z0 + len * 0.5f;
            bool tilted = i % 3 == 2;
            if (tilted)
            {
                float drop = len * tan8;
                LevelKit.SlabRot(root, new Vector3(x, yTop - drop * 0.5f - h * 0.5f, zc), new Vector3(w, h, len), LevelKit.Dark, "MonolithSlide", Quaternion.Euler(8f, 0f, 0f));
                yTop -= drop;
            }
            else
            {
                LevelKit.Slab(root, new Vector3(x, yTop - h * 0.5f, zc), new Vector3(w, h, len), LevelKit.Dark, "Monolith");
                LevelKit.LineRun(root, new Vector3(x, yTop, z0 + 1f), new Vector3(x, yTop, z0 + len - 1f), c.zone);
                if (i % 4 == 1) LevelKit.Lip(root, new Vector3(x, yTop + 0.5f, z0 + len - 0.5f), new Vector3(w - 2f, 1f, 1f), LevelKit.Limestone, "MonolithLip");
            }
            for (int e = 0; e < 2; e++)
            {
                float cz = e == 0 ? z0 + 1.5f : z0 + len - 1.5f, top = yTop - h + (tilted && e == 0 ? len * tan8 : 0f);
                LevelKit.Slab(root, new Vector3(x, (ground + top) * 0.5f, cz), new Vector3(2.2f, Mathf.Max(1f, top - ground), 2.2f), LevelKit.Dark, "MonolithColumn");
            }
            if (i > 0) padZ.Add((x, z0 - gap * 0.5f));
            z = z0 + len;
        }
        Ground(root, halfW, ground, c.z - 2f, z + 2f);
        foreach (var p in padZ) LevelKit.JumpPad(root, new Vector3(p.x, ground, p.z), 0f, PadUp, PadOn);
        Hall(root, halfW + 6f, ground, y + 24f, c.z - 2f, z + 2f, true);
        y = yTop;
        return z + 1f;
    }

    /// THE CANYON: two faces eleven metres apart and forty tall run the length of the set, the street nine metres
    /// down between them. The line is LEDGES on alternating faces, 10–16 m long, the jump between them growing with
    /// the difficulty: run the ledge, run the face, jump to the other side. The last ledge ends in a BOOST CORNER and
    /// the high way out is a balcony above the landing deck. Pads on the street.
    static float Canyon(Transform root, MovementConfig config, Cursor c, float halfW, ref float y, float d)
    {
        float z = c.z + 2f, ground = y - GroundDrop, faceX = 5.5f, faceH = 40f;
        int ledges = 4 + Mathf.RoundToInt(d * 2f);
        float gapBase = Mathf.Lerp(4f, 7f, d);
        int side = rng.NextDouble() < 0.5 ? -1 : 1;
        var padZ = new List<float>();
        float z0 = z - 2f;
        for (int i = 0; i < ledges; i++)
        {
            float len = R(10f, 16f);
            float zc = z + len * 0.5f, ly = y + R(-0.8f, 1.2f);
            LevelKit.Lip(root, new Vector3(side * (faceX - 1.1f), ly - 0.4f, zc), new Vector3(2.2f, 0.8f, len), LevelKit.Limestone, "CanyonLedge");
            LevelKit.LineRun(root, new Vector3(side * (faceX - 1.1f), ly, z + 1f), new Vector3(side * (faceX - 1.1f), ly, z + len - 1f), c.zone);
            padZ.Add(zc);
            z += len;
            if (i < ledges - 1) z += gapBase + R(-0.6f, 0.6f);
            else
            {
                LevelKit.BoostCorner(root, new Vector3(side * faceX, y + 2.5f, z + 0.6f), new Vector3(1.4f, 7f, 1.2f), LevelKit.Wall);
                LevelKit.Lip(root, new Vector3(side * (faceX - 2.6f), y + 4.6f, z + 6f), new Vector3(5f, 0.7f, 6f), LevelKit.Limestone, "Balcony");
                LevelKit.BrassStrip(root, new Vector3(side * (faceX - 2.6f), y + 4.96f, z + 3.2f), new Vector3(side * (faceX - 2.6f), y + 4.96f, z + 8.8f));
            }
            side = -side;
        }
        float z1 = z + 2f;
        for (int s = -1; s <= 1; s += 2)
        {
            LevelKit.KickWall(root, new Vector3(s * (faceX + 0.5f), ground + faceH * 0.5f, (z0 + z1) * 0.5f), new Vector3(1f, faceH, z1 - z0), LevelKit.Wall);
            for (float wy = y + 6f; wy < ground + faceH - 4f; wy += 7f)
                for (float wz = z0 + 4f; wz < z1 - 4f; wz += 9f)
                    LevelKit.Slab(root, new Vector3(s * (faceX - 0.05f), wy, wz), new Vector3(0.12f, 2.6f, 1.6f), LevelKit.Accent, "CanyonWindow");
            for (float lz = z0 + 6f; lz < z1; lz += 16f) LevelKit.Light(root, new Vector3(s * (faceX - 1.5f), y + 3f, lz), 18f, 1.4f);
        }
        Ground(root, faceX, ground, z0, z1);
        foreach (float pz in padZ) LevelKit.JumpPad(root, new Vector3(0f, ground, pz), 0f, PadUp, PadOn);
        LevelKit.JumpPad(root, new Vector3(0f, ground, z - 2f), 0f, PadUp, PadOn);
        return z + 1f;
    }

    /// THE SHAFT: a shaft 60–70 m long, walls twenty-six metres tall either side cut by openings, a slab roof with
    /// a skylight over each pit, the floor broken by two pits (the main line jumps them; the lips up the walls over
    /// each pit are the fast line, a wall run into a corner boost), one light falling into each pit, a beam across.
    static float ShaftSet(Transform root, MovementConfig config, Cursor c, float halfW, ref float y, float d)
    {
        float len = 60f + d * 10f, z0 = c.z, z1 = z0 + len, wallH = 26f, wx = halfW * 0.62f, ground = y - GroundDrop;
        float pitW = Mathf.Lerp(4f, 6.5f, d);
        float[] pits = { z0 + len * 0.36f, z0 + len * 0.7f };
        float a = z0;
        foreach (float p in pits)
        {
            float b = p - pitW * 0.5f;
            LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, (a + b) * 0.5f), new Vector3(wx * 2f + 1.6f, 1f, b - a), "ShaftFloor");
            LevelKit.JumpPad(root, new Vector3(0f, ground, b + pitW - 1.2f), 0f, PadUp, PadOn);
            a = p + pitW * 0.5f;
        }
        LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, (a + z1) * 0.5f), new Vector3(wx * 2f + 1.6f, 1f, z1 - a), "ShaftFloor");
        Ground(root, wx, ground, z0, z1);
        for (int s = -1; s <= 1; s += 2)
            for (int k = 0; k < 3; k++)
            {
                float zs = z0 + k * (len / 3f) + 1f, ze = z0 + (k + 1) * (len / 3f) - 5f;
                LevelKit.KickWall(root, new Vector3(s * wx, ground + wallH * 0.5f, (zs + ze) * 0.5f), new Vector3(0.8f, wallH, ze - zs), LevelKit.Wall);
            }
        float roofY = ground + wallH, r0 = z0;
        foreach (float p in pits)
        {
            LevelKit.Slab(root, new Vector3(0f, roofY + 0.5f, (r0 + p - 4f) * 0.5f), new Vector3(wx * 2f + 1.6f, 1f, p - 4f - r0), LevelKit.Ceil, "ShaftRoof");
            r0 = p + 4f;
        }
        LevelKit.Slab(root, new Vector3(0f, roofY + 0.5f, (r0 + z1) * 0.5f), new Vector3(wx * 2f + 1.6f, 1f, z1 - r0), LevelKit.Ceil, "ShaftRoof");
        int lipSide = 1;
        foreach (float p in pits)
        {
            LevelKit.Lip(root, new Vector3(lipSide * (wx - 0.9f), y + 3.2f, p - 3f), new Vector3(1.2f, 0.5f, 4f), LevelKit.Limestone, "ShaftLip");
            LevelKit.Lip(root, new Vector3(lipSide * (wx - 0.9f), y + 6.4f, p + 3f), new Vector3(1.2f, 0.5f, 4f), LevelKit.Limestone, "ShaftLip");
            LevelKit.Shaft(root, new Vector3(0f, roofY - 2f, p), new Vector2(7f, 7f), 22f);
            LevelKit.Spot(root, new Vector3(0f, roofY, p), Vector3.down, wallH + 12f, 36f, 16f, new Color(1f, 0.88f, 0.66f));
            LevelKit.Slab(root, new Vector3(0f, y + 9.5f, p + 9f), new Vector3(wx * 2f + 2f, 2f, 3f), LevelKit.Dark, "ShaftBeam");
            lipSide = -lipSide;
        }
        return z1;
    }

    /// THE CHUTE: a slide down a building's flank — a 24° slope 14–22 m down, the building's wall thirty metres
    /// tall on one side, the city open on the other, two bars to hop on the way, then the run gap at its foot with
    /// the street under it. Speed is the point: take the bars at pace and the gap is nothing.
    static float ChuteSet(Transform root, MovementConfig config, Cursor c, float halfW, ref float y, float d)
    {
        float drop = Mathf.Lerp(14f, 22f, d), tan = Mathf.Tan(24f * Mathf.Deg2Rad), len = drop / tan;
        float z0 = c.z, z1 = z0 + len, w = halfW * 2f, ground = y - drop - GroundDrop;
        int side = rng.NextDouble() < 0.5 ? -1 : 1;
        LevelKit.Ramp(root, 0f, w, z0, y, z1, y - drop);
        Block(root, 0f, ground, y - drop - 1.2f, (z0 + z1) * 0.5f, w, len, "ChutePlinth");
        LevelKit.Slab(root, new Vector3(side * (halfW + 0.6f), ground + 15f + drop * 0.5f, (z0 + z1) * 0.5f + 7f), new Vector3(1.2f, 30f + drop, len + 14f), LevelKit.Wall, "ChuteFlank");
        for (float z = z0 + R(12f, 16f); z < z1 - 8f; z += R(14f, 18f))
        {
            float yz = y - (z - z0) * tan;
            LevelKit.Lip(root, new Vector3(0f, yz + 0.45f, z), new Vector3(w - 2f, 0.9f, 0.8f), LevelKit.Limestone, "ChuteBar");
        }
        float yFoot = y - drop, landLen = 10f;
        LevelKit.FloorSlab(root, new Vector3(0f, yFoot - 0.5f, z1 + landLen * 0.5f), new Vector3(w, 1f, landLen), "ChuteFoot");
        Block(root, 0f, ground, yFoot - 1f, z1 + landLen * 0.5f, w, landLen, "FootPlinth");
        float gap = RunGap + d * 3f;
        Ground(root, halfW, ground, z1 + landLen - 3f, z1 + landLen + gap + 3f);
        LevelKit.JumpPad(root, new Vector3(0f, ground, z1 + landLen + gap - 1.5f), 0f, PadUp, PadOn);
        for (float lz = z0 + 10f; lz < z1; lz += 18f) LevelKit.Light(root, new Vector3(side * (halfW - 2f), y - (lz - z0) * tan + 4f, lz), 20f, 1.5f);
        y = yFoot;
        return z1 + landLen + gap;
    }

    /// THE DRAUGHT: a tower climbed from inside — walls twenty metres tall round a well, open where you come in
    /// and out. The main line is the fan's updraft in the middle, carrying you to the deck at the top. The fast line
    /// is the wall beside it: lips every 3.3 m up its face, each a corner boost (perfect ones chain).
    static float Draught(Transform root, MovementConfig config, Cursor c, float halfW, ref float y, float d)
    {
        float rise = Mathf.Lerp(12f, 16f, d), z0 = c.z, ground = y - GroundDrop;
        float yTop = y + rise, towerH = rise + 10f, wx = halfW * 0.7f;
        LevelKit.FanShaft(root, new Vector3(0f, y, z0 + 6f), rise + 2f, 3.6f);
        LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, z0 + 5f), new Vector3(halfW * 2f, 1f, 10f), "DraughtFloor");
        Block(root, 0f, ground, y - 1f, z0 + 5f, halfW * 2f, 10f, "DraughtPlinth");
        float lx = wx - 1f;
        LevelKit.KickWall(root, new Vector3(lx, y + rise * 0.5f, z0 + 7f), new Vector3(0.8f, rise + 1f, 10f), LevelKit.Wall);
        for (float ly = y + 3.3f; ly < yTop - 0.5f; ly += 3.3f)
            LevelKit.Lip(root, new Vector3(lx - 0.9f, ly, z0 + R(4f, 10f)), new Vector3(1.2f, 0.5f, 2.6f), LevelKit.Limestone, "LadderLip");
        float deckZ0 = z0 + 12f, deckLen = 10f;
        LevelKit.FloorSlab(root, new Vector3(0f, yTop - 0.5f, deckZ0 + deckLen * 0.5f), new Vector3(halfW * 2f, 1f, deckLen), "DraughtDeck");
        Block(root, 0f, ground, yTop - 1f, deckZ0 + deckLen * 0.5f, halfW * 2f, deckLen, "DeckPlinth");
        for (int s = -1; s <= 1; s += 2)
        {
            LevelKit.Slab(root, new Vector3(s * (wx + 0.6f), ground + (towerH + GroundDrop) * 0.5f, z0 + 11f), new Vector3(1.2f, towerH + GroundDrop, 24f), LevelKit.Wall, "TowerWall");
            for (float ly = y + 2f; ly < yTop + 6f; ly += 5f) LevelKit.Light(root, new Vector3(s * (wx - 1.2f), ly, z0 + 8f), 16f, 1.3f);
        }
        y = yTop;
        return deckZ0 + deckLen;
    }

    /// THE STAIRS: a grand stair as a launch ramp. A flight up ten metres over thirty, nosed in brass with a banister
    /// to slide down either side; a landing at its top; then the drop off the landing's edge over a gap of twelve to
    /// fourteen metres into a slide that takes the fall (the stair's speed carries it; walked, the lower route is the
    /// street and its pad).
    static float Stairs(Transform root, MovementConfig config, Cursor c, float halfW, ref float y, float d)
    {
        float rise = 10f, len = 30f, z0 = c.z, z1 = z0 + len, w = halfW * 2f - 4f, ground = y - GroundDrop;
        float yTop = y + rise;
        LevelKit.Ramp(root, 0f, w, z0, y, z1, yTop);
        LevelKit.Nosings(root, 0f, w, z0, y, z1, yTop, 1.6f);
        LevelKit.Banister(root, -w * 0.5f - 0.6f, 0.4f, z0, y, z1, yTop);
        LevelKit.Banister(root, w * 0.5f + 0.6f, 0.4f, z0, y, z1, yTop);
        Block(root, 0f, ground, y - 0.6f, (z0 + z1) * 0.5f, w + 2f, len, "StairPlinth");
        float landLen = 6f;
        LevelKit.FloorSlab(root, new Vector3(0f, yTop - 0.5f, z1 + landLen * 0.5f), new Vector3(halfW * 2f, 1f, landLen), "StairLanding");
        Block(root, 0f, ground, yTop - 1f, z1 + landLen * 0.5f, halfW * 2f, landLen, "LandingPlinth");
        LevelKit.LineRun(root, new Vector3(0f, yTop, z1 + 0.5f), new Vector3(0f, yTop, z1 + landLen - 0.5f), c.zone);
        float gap = 12f + 2f * d, zLand = z1 + landLen + gap, yLand = yTop - 8f, slideLen = 14f;
        LevelKit.Ramp(root, 0f, halfW * 2f, zLand, yLand + 3f, zLand + slideLen, yLand);
        Block(root, 0f, ground, yLand - 0.6f, zLand + slideLen * 0.5f, halfW * 2f, slideLen, "SlidePlinth");
        Ground(root, halfW, ground, z1 + landLen - 2f, zLand + 2f);
        LevelKit.JumpPad(root, new Vector3(0f, ground, zLand - 2f), 0f, PadUp, PadOn);
        for (float lz = z0 + 6f; lz < z1; lz += 12f) for (int s = -1; s <= 1; s += 2) LevelKit.Light(root, new Vector3(s * (halfW - 1f), y + (lz - z0) / len * rise + 3f, lz), 16f, 1.3f);
        y = yLand;
        return zLand + slideLen;
    }

    /// THE POLES: four to six brass-capped poles over the street, each a stack to kick, staggered left and right
    /// 7–9 m apart: ping-pong between them without touching the ground, the upper line; the street and its pads are
    /// the lower line and cost the time.
    static float Poles(Transform root, MovementConfig config, Cursor c, float halfW, ref float y, float d)
    {
        float z = c.z + 4f, ground = y - GroundDrop;
        int n = 4 + Mathf.RoundToInt(d * 2f);
        int side = rng.NextDouble() < 0.5 ? -1 : 1;
        var padZ = new List<float>();
        for (int i = 0; i < n; i++)
        {
            float x = side * R(2.5f, 4f);
            LevelKit.KickStack(root, new Vector3(x, ground, z), y + 5f - ground, 1.8f);
            LevelKit.Slab(root, new Vector3(x, y + 5.3f, z), new Vector3(2.2f, 0.4f, 2.2f), LevelKit.Brass, "PoleCap");
            padZ.Add(z);
            z += R(7f, 9f) + d * 1.5f;
            side = -side;
        }
        Ground(root, halfW, ground, c.z - 2f, z + 2f);
        for (int i = 0; i + 1 < padZ.Count; i += 2) LevelKit.JumpPad(root, new Vector3(0f, ground, (padZ[i] + padZ[i + 1]) * 0.5f), 0f, PadUp, PadOn);
        LevelKit.JumpPad(root, new Vector3(0f, ground, z - 1f), 0f, PadUp, PadOn);
        for (float lz = c.z + 6f; lz < z; lz += 14f) for (int s = -1; s <= 1; s += 2) LevelKit.Light(root, new Vector3(s * (halfW - 1f), ground + 4f, lz), 20f, 1.5f);
        return z + 2f;
    }

    /// THE ROOFS: 5–7 building tops at their own heights, 10–16 m on a side, the street between them, gaps of five
    /// to nine metres, a ledge to clamber where the next top is higher, a chimney stack to kick on every other; the
    /// long run of a roof is where the speed comes back. Pads in the streets.
    static float Roofs(Transform root, MovementConfig config, Cursor c, float halfW, ref float y, float d)
    {
        float z = c.z, x = 0f, yTop = y, ground = y - GroundDrop;
        int n = 5 + Mathf.RoundToInt(d * 2f);
        var gaps = new List<(float x, float z)>();
        for (int i = 0; i < n; i++)
        {
            float w = R(10f, 16f), dp = R(10f, 16f), gap = i == 0 ? 1f : Mathf.Lerp(5f, 9f, d) + R(-1f, 1f);
            x = Mathf.Clamp(x + R(-3f, 3f), -(halfW - w * 0.5f), halfW - w * 0.5f);
            float dy = i == 0 ? 0f : rng.NextDouble() < 0.4 ? R(1.6f, 3f) : R(-3f, 0f);
            if (gap > 7.5f) dy = Mathf.Min(dy, 0f);
            float near = z + gap, far = near + dp, zc = (near + far) * 0.5f;
            yTop += dy;
            Block(root, x, ground, yTop, zc, w, dp, "Roof");
            if (dy > 1.5f) LevelKit.Lip(root, new Vector3(x, yTop - 0.3f, near + 0.5f), new Vector3(w - 2f, 0.6f, 1f), LevelKit.Limestone, "ClamberLedge");   // the parapet of the taller roof: grab it, boost it
            if (i % 2 == 1) LevelKit.KickStack(root, new Vector3(x + w * 0.3f, yTop, zc), 3.2f, 1.4f);
            LevelKit.LineRun(root, new Vector3(x, yTop, near + 0.5f), new Vector3(x, yTop, far - 0.5f), c.zone);
            if (i > 0) gaps.Add((x, near - gap * 0.5f));
            z = far;
        }
        Ground(root, halfW, ground, c.z - 2f, z + 2f);
        foreach (var g in gaps) LevelKit.JumpPad(root, new Vector3(g.x, ground, g.z), 0f, PadUp, PadOn);
        for (float lz = c.z + 8f; lz < z; lz += 16f) for (int s = -1; s <= 1; s += 2) LevelKit.Light(root, new Vector3(s * (halfW - 1f), ground + 4f, lz), 20f, 1.4f);
        y = yTop;
        return z + 1f;
    }

    /// THE APARTMENTS: a building run through. In by the window of its upper floor, across a room, down through a
    /// hole in its floor onto the floor below, along a corridor past three partitions with their doors staggered,
    /// and out up a short ramp onto the deck. Walls either side with window openings onto the city, a ceiling, lamps.
    static float Apartments(Transform root, MovementConfig config, Cursor c, float halfW, ref float y, float d)
    {
        float z0 = c.z, len = 54f, z1 = z0 + len, ground = y - GroundDrop, wx = halfW - 1f, ceil = y + 7f;
        float holeZ = z0 + 16f, holeW = 4f + d * 1.5f, yLow = y - 5f;
        // The upper floor to the hole, the lower floor from it to the ramp, the ramp up to the way out
        LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, (z0 + holeZ - holeW * 0.5f) * 0.5f), new Vector3(wx * 2f, 1f, holeZ - holeW * 0.5f - z0), "UpperFloor");
        float lowZ0 = holeZ - holeW * 0.5f - 6f, lowZ1 = z1 - 8f;
        LevelKit.FloorSlab(root, new Vector3(0f, yLow - 0.5f, (lowZ0 + lowZ1) * 0.5f), new Vector3(wx * 2f, 1f, lowZ1 - lowZ0), "LowerFloor");
        LevelKit.Ramp(root, 0f, wx * 2f, lowZ1, yLow, z1, y - 1f);
        Block(root, 0f, ground, yLow - 1f, (z0 + z1) * 0.5f, wx * 2f + 2f, len, "ApartmentPlinth");
        // The partitions across the corridor, their doors staggered
        int doorSide = rng.NextDouble() < 0.5 ? -1 : 1;
        for (int k = 0; k < 3; k++)
        {
            float pz = lowZ0 + 10f + k * 10f, doorX = doorSide * (wx - 3f);
            float leftW = (doorX - 1.6f) + wx, rightW = wx - (doorX + 1.6f);
            if (leftW > 0.5f) LevelKit.Slab(root, new Vector3(-wx + leftW * 0.5f, yLow + 3.2f, pz), new Vector3(leftW, 6.4f, 0.3f), LevelKit.Wall, "Partition");
            if (rightW > 0.5f) LevelKit.Slab(root, new Vector3(wx - rightW * 0.5f, yLow + 3.2f, pz), new Vector3(rightW, 6.4f, 0.3f), LevelKit.Wall, "Partition");
            LevelKit.Slab(root, new Vector3(doorX, yLow + 2.8f, pz), new Vector3(3.2f, 0.2f, 0.34f), LevelKit.Brass, "DoorHead");
            doorSide = -doorSide;
        }
        // The outer walls with window openings, the ceiling, the lamps
        for (int s = -1; s <= 1; s += 2)
            for (int k = 0; k < 4; k++)
            {
                float ws = z0 + k * (len / 4f), we = ws + len / 4f - 4f;
                LevelKit.Slab(root, new Vector3(s * (wx + 0.5f), (ground + ceil) * 0.5f, (ws + we) * 0.5f), new Vector3(1f, ceil - ground, we - ws), LevelKit.Wall, "ApartmentWall");
                LevelKit.Slab(root, new Vector3(s * (wx + 0.5f), ground + (yLow - 1.5f - ground) * 0.5f, we + 2f), new Vector3(1f, yLow - 1.5f - ground, 4f), LevelKit.Wall, "WindowSill");
                LevelKit.Slab(root, new Vector3(s * (wx + 0.5f), (y + 2.6f + ceil) * 0.5f, we + 2f), new Vector3(1f, ceil - y - 2.6f, 4f), LevelKit.Wall, "WindowHead");
            }
        LevelKit.Slab(root, new Vector3(0f, ceil + 0.5f, (z0 + z1) * 0.5f), new Vector3(wx * 2f + 2f, 1f, len), LevelKit.Ceil, "ApartmentCeiling");
        for (float lz = z0 + 6f; lz < z1; lz += 12f) LevelKit.Light(root, new Vector3(0f, ceil - 1.2f, lz), 16f, 1.3f);
        Ground(root, halfW, ground, z0 - 2f, z1 + 2f);
        y = y - 1f;
        return z1;
    }

    // ------------------------------------------------------------ the shared pieces

    /// A landing deck across the lane on a plinth to the street: solid, nothing on it to catch a foot
    static void Deck(Transform root, float halfW, float y, float z0, float len)
    {
        LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, z0 + len * 0.5f), new Vector3(halfW * 2f, 1f, len), "CourseDeck");
        Block(root, 0f, y - GroundDrop, y - 1f, z0 + len * 0.5f, halfW * 2f, len, "DeckPlinth");
        LevelKit.LineRun(root, new Vector3(0f, y, z0 + 0.5f), new Vector3(0f, y, z0 + len - 0.5f), LevelKit.Current);
    }

    /// A block standing on the street with its top at `yTop`: the dark pillar stone for the body, the floor for the cap
    static void Block(Transform root, float x, float ground, float yTop, float zc, float w, float dp, string name)
    {
        float h = yTop - ground;
        if (h <= 0.6f) return;
        LevelKit.Slab(root, new Vector3(x, ground + (h - 0.4f) * 0.5f, zc), new Vector3(w, h - 0.4f, dp), LevelKit.Dark, name);
        LevelKit.FloorSlab(root, new Vector3(x, yTop - 0.2f, zc), new Vector3(w, 0.4f, dp), name + "Top");
    }

    /// The street under a set: the whole lane and more, flat, nothing at its edges to catch a foot (the pads on it
    /// are the way back up)
    static void Ground(Transform root, float halfW, float yGround, float z0, float z1)
    {
        float w = halfW * 2f + 12f;
        LevelKit.FloorSlab(root, new Vector3(0f, yGround - 0.5f, (z0 + z1) * 0.5f), new Vector3(w, 1f, z1 - z0), "Street");
    }

    /// A hall round a set: great walls either side from `yFloor` to `yCeil`, a slab ceiling (with a skylight down its
    /// middle when `skylight`), columns every twelve metres, lamps between them
    static void Hall(Transform root, float halfW, float yFloor, float yCeil, float z0, float z1, bool skylight)
    {
        float h = yCeil - yFloor, zc = (z0 + z1) * 0.5f, len = z1 - z0;
        for (int s = -1; s <= 1; s += 2)
        {
            LevelKit.Slab(root, new Vector3(s * (halfW + 0.6f), yFloor + h * 0.5f, zc), new Vector3(1.2f, h, len), LevelKit.Wall, "HallWall");
            for (float cz = z0 + 6f; cz < z1; cz += 12f)
            {
                LevelKit.Slab(root, new Vector3(s * (halfW - 1.2f), yFloor + h * 0.5f, cz), new Vector3(2f, h, 2f), LevelKit.Dark, "HallColumn");
                LevelKit.Light(root, new Vector3(s * (halfW - 3f), yFloor + Mathf.Min(h - 2f, 6f), cz + 6f), 22f, 1.5f);
            }
        }
        if (skylight)
        {
            float wing = halfW - 4f;
            for (int s = -1; s <= 1; s += 2) LevelKit.Slab(root, new Vector3(s * (4f + wing * 0.5f), yCeil + 0.5f, zc), new Vector3(wing, 1f, len), LevelKit.Ceil, "HallRoof");
        }
        else LevelKit.Slab(root, new Vector3(0f, yCeil + 0.5f, zc), new Vector3(halfW * 2f + 1.2f, 1f, len), LevelKit.Ceil, "HallRoof");
    }

    /// Two pylons either side of a set's start, set well back from the lane: the megastructure's bones, sixty metres
    /// of limestone with a brass band and a lamp each
    static void Pylons(Transform root, float halfW, float y, float z)
    {
        for (int s = -1; s <= 1; s += 2)
        {
            LevelKit.Slab(root, new Vector3(s * (halfW + 10f), y + 14f, z), new Vector3(4f, 60f, 4f), LevelKit.Limestone, "Pylon");
            LevelKit.Slab(root, new Vector3(s * (halfW + 10f), y + 38f, z), new Vector3(4.6f, 1.2f, 4.6f), LevelKit.Brass, "PylonBand");
            LevelKit.Spot(root, new Vector3(s * (halfW + 7.6f), y + 11f, z), new Vector3(-s * 0.35f, -1f, 0.25f), 44f, 60f, 10f, new Color(1f, 0.86f, 0.62f));
        }
    }

    static float Pick(float[] table) => table[rng.Next(table.Length)];

    static string Title(SetKind k) => k switch
    {
        SetKind.Drop => "THE DROP",
        SetKind.Terraces => "THE TERRACES",
        SetKind.Monoliths => "THE MONOLITHS",
        SetKind.Canyon => "THE CANYON",
        SetKind.Shaft => "THE SHAFT",
        SetKind.Chute => "THE CHUTE",
        SetKind.Draught => "THE DRAUGHT",
        SetKind.Stairs => "THE STAIRS",
        SetKind.Poles => "THE POLES",
        SetKind.Roofs => "THE ROOFS",
        SetKind.Hall => "THE HALL OF BEAMS",
        SetKind.Well => "THE WELL",
        SetKind.Plaza => "THE PLAZA",
        SetKind.Street => "THE STREET",
        SetKind.Span => "THE SPAN",
        _ => "THE APARTMENTS",
    };

    static string Roman(int n)
    {
        string[] r = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII", "XIII", "XIV", "XV", "XVI", "XVII", "XVIII", "XIX", "XX" };
        return n >= 0 && n < r.Length ? r[n] : n.ToString();
    }
}
