using System.Collections.Generic;
using UnityEngine;
using Cursor = LevelSections.Cursor;

/// <summary>
/// THE ASCENT (2026-10-27, user: "chapter 1 is still too linear, im expecting big changes here"). Until now every
/// course ran down one straight line: a space's three ways were three lanes of the same corridor. Here the course
/// TURNS. After the drop it wraps round a block in a square spiral — each leg one of the spaces, each corner a
/// right turn on a square landing that climbs three metres — so the course looks back on where it came from across
/// the block, the city stands on every side of it, the finish is a storey above the start, and no two legs face the
/// same way. The pieces are the spaces as they are: each leg is built flat at a staging origin far from everything
/// and then STOOD at the pen (moved and turned as one), its pads re-aimed; the checkpoints are volumes facing each
/// leg; the finish is built the same way and its portal moved with it; the city is told the whole footprint.
/// </summary>
public static partial class Course
{
    const float Staging = -5000f;   // where a leg is built before it is stood in the world

    /// The drop straight on from the cursor, then `legs` spaces round the block, the corners between them, the finish
    public static Cursor Ascent(Transform root, MovementConfig config, Cursor c, SetKind[] legs, float halfW, string name)
    {
        Begin(c);
        halfW = Mathf.Max(halfW, 9f);
        float y = c.floorY, z = c.z, zStart = z;
        Threshold(root, c, halfW, y, Head);
        Deck(root, halfW, y, z - T, DeckLen + T);
        z += DeckLen;
        var cs = c; cs.z = z - 5f; cs.floorY = y; cs.halfW = halfW;
        Checkpoint(root, cs, name + " · THE DROP");
        cs.z = z;
        Pylons(root, halfW, y, z);
        z = Drop(root, config, cs, halfW, ref y, 0f);
        Deck(root, halfW, y, z, DeckLen);
        z += DeckLen;

        // THE PEN: where the course is in the world and which way it faces
        Vector3 pen = new Vector3(0f, y, z);
        float heading = 0f;
        var footprint = new Bounds(pen, new Vector3(halfW * 2f, 4f, 2f));
        footprint.Encapsulate(new Vector3(-halfW - 14f, c.floorY - 12f, zStart));
        footprint.Encapsulate(new Vector3(halfW + 14f, c.floorY + 30f, z));
        var life = LevelKit.Life(root);
        for (int i = 0; i < legs.Length; i++)
        {
            float d = legs.Length > 1 ? i / (float)(legs.Length - 1) : 0.5f;
            var rot = Quaternion.Euler(0f, heading, 0f);
            Vector3 fwd = rot * Vector3.forward, right = rot * Vector3.right;
            // The checkpoint at the leg's start: a volume across the lane on the corner's landing behind it, facing the leg
            CityDistrict.Checkpoint(root, name + " · " + Roman(i + 1) + " · " + Title(legs[i]), Volume(pen - fwd * 3f + Vector3.up * 3f, halfW * 2f + 2f, 8f, 5f, heading), fwd, pen - fwd * 3f + Vector3.up * 0.6f);
            // The run-up onto the leg, then the leg: built flat at the staging origin, then stood at the pen
            EntryDeck(root, pen, rot, halfW);
            pen += fwd * 6f;
            var legRoot = new GameObject("Leg " + (i + 1) + " " + Title(legs[i])).transform;
            legRoot.SetParent(root, false);
            var lc = c; lc.z = Staging; lc.floorY = 0f; lc.halfW = halfW;
            float yl = 0f;
            int padsBefore = life.PadCount;
            float zl = BuildSpace(legRoot, config, lc, legs[i], halfW, ref yl, d) - Staging;
            legRoot.SetPositionAndRotation(pen - rot * new Vector3(0f, 0f, Staging), rot);
            life.RelocatePads(padsBefore, legRoot);
            Encapsulate(ref footprint, pen, rot, halfW + 32f, -14f, 48f, zl);
            // The pen at the leg's end, then the corner: a square landing, the turn to the right, three metres up
            pen += rot * new Vector3(0f, yl, zl);
            pen = Corner(root, pen, heading, halfW, i == legs.Length - 1, out heading);
            Encapsulate(ref footprint, pen, Quaternion.Euler(0f, heading, 0f), halfW + 10f, -12f, 20f, 0f);
        }

        // THE FINISH: built flat, stood at the pen, its portal moved with it
        var finRoot = new GameObject("Finish").transform;
        finRoot.SetParent(root, false);
        var fc = c; fc.z = Staging; fc.floorY = 0f; fc.halfW = halfW; fc.hasWall = false; fc.open = true;
        LevelSections.Finish(finRoot, config, fc);
        var frot = Quaternion.Euler(0f, heading, 0f);
        finRoot.SetPositionAndRotation(pen - frot * new Vector3(0f, 0f, Staging), frot);
        var last = LevelSections.Last;
        if (last != null)
        {
            last.finishPortal = finRoot.TransformPoint(last.finishPortal);
            if (last.rooms.Count > 0 && last.rooms[last.rooms.Count - 1].z0 < Staging + 1000f) last.rooms.RemoveAt(last.rooms.Count - 1);   // the finish registered its span at the staging origin: not a place
            Encapsulate(ref footprint, pen, frot, halfW + 8f, -4f, 16f, 20f);
        }

        // The world's frame: the sky on both sides of the whole footprint, the air over it, the city told to keep clear
        LevelSections.See(-1, footprint.min.z, footprint.max.z, footprint.min.y, footprint.max.y + 220f);
        LevelSections.See(1, footprint.min.z, footprint.max.z, footprint.min.y, footprint.max.y + 220f);
        LevelKit.Air(root, c.zone, footprint.min, footprint.max, true);
        CityVista.Override = footprint;
        LevelSections.Postcard(new Vector3(-halfW * 1.2f, c.floorY + 12f, zStart + 4f), new Vector3(0f, c.floorY - 4f, zStart + 70f), 74f);
        var next = LevelSections.Exit(c, footprint.max.z, pen.y, halfW, pen.y, pen.y + Head);
        next.open = true;
        if (last != null) last.length = footprint.max.z;
        return next;
    }

    /// One space, by its kind, at the cursor (the straight-line builders, used flat at the staging origin)
    static float BuildSpace(Transform root, MovementConfig config, Cursor c, SetKind kind, float halfW, ref float y, float d)
    {
        switch (kind)
        {
            case SetKind.Hall: return HallOfBeams(root, config, c, halfW, ref y, d);
            case SetKind.Well: return Well(root, config, c, halfW, ref y, d);
            case SetKind.Plaza: return Plaza(root, config, c, halfW, ref y, d);
            case SetKind.Street: return Street(root, config, c, halfW, ref y, d);
            case SetKind.Span: return Span(root, config, c, halfW, ref y, d);
            case SetKind.Terraces: return Terraces(root, config, c, halfW, ref y, d);
            case SetKind.Monoliths: return Monoliths(root, config, c, halfW, ref y, d);
            case SetKind.Canyon: return Canyon(root, config, c, halfW, ref y, d);
            case SetKind.Shaft: return ShaftSet(root, config, c, halfW, ref y, d);
            case SetKind.Chute: return ChuteSet(root, config, c, halfW, ref y, d);
            case SetKind.Stairs: return Stairs(root, config, c, halfW, ref y, d);
            case SetKind.Poles: return Poles(root, config, c, halfW, ref y, d);
            case SetKind.Roofs: return Roofs(root, config, c, halfW, ref y, d);
            case SetKind.Apartments: return Apartments(root, config, c, halfW, ref y, d);
            default: return Drop(root, config, c, halfW, ref y, d);
        }
    }

    /// THE CORNER: a square landing the lane's width plus six, entered along `heading`, left along the heading turned
    /// right. FLOW (2026-10-27, user: "there needs to be some flow to it"): the far wall is a KICK WALL, so at speed
    /// you run at it and kick off it into the turn (the course rule: no blind right angle at speed without a kick wall);
    /// the outer wall carries a wall run round; the way out is a ramp up three metres, so every corner is a storey
    /// of the climb and the climb is a slope, never a step. Everything solid (SlabRot / Slab), nothing decor.
    /// `last`: the finish follows, so a straight approach is laid after the ramp.
    static Vector3 Corner(Transform root, Vector3 at, float heading, float halfW, bool last, out float headingOut)
    {
        var rot = Quaternion.Euler(0f, heading, 0f);
        Vector3 fwd = rot * Vector3.forward, right = rot * Vector3.right;
        float s = halfW * 2f + 6f, rise = 3f, ground = at.y - GroundDrop, wallH = 16f;
        Vector3 centre = at + fwd * (s * 0.5f);
        // The landing: flat, solid, on a plinth to the street
        LevelKit.Slab(root, new Vector3(centre.x, at.y - 0.5f, centre.z), new Vector3(s, 1f, s), LevelKit.Floor, "CornerFloor");
        LevelKit.Slab(root, new Vector3(centre.x, (ground + at.y - 1f) * 0.5f, centre.z), new Vector3(s, at.y - 1f - ground, s), LevelKit.Dark, "CornerPlinth");
        // The far wall, a kick wall; the outer (left) wall, a wall to run; both from the landing to sixteen metres up
        Vector3 farC = centre + fwd * (s * 0.5f + 0.6f) + Vector3.up * (wallH * 0.5f - 1f);
        Vector3 outerC = centre - right * (s * 0.5f + 0.6f) + Vector3.up * (wallH * 0.5f - 1f);
        LevelKit.KickWall(root, farC, Box(rot, new Vector3(s + 1.2f, wallH + 2f, 1.2f)), LevelKit.Wall);
        LevelKit.KickWall(root, outerC, Box(rot, new Vector3(1.2f, wallH + 2f, s)), LevelKit.Wall);
        // The lamp tower in the angle, a brass cap, a lamp
        Vector3 tower = centre + fwd * (s * 0.5f - 2.5f) - right * (s * 0.5f - 2.5f);
        LevelKit.Slab(root, tower + Vector3.up * 14f, new Vector3(3f, 30f, 3f), LevelKit.Limestone, "CornerTower");
        LevelKit.Slab(root, tower + Vector3.up * 29.5f, new Vector3(3.6f, 1f, 3.6f), LevelKit.Brass, "TowerCap");
        LevelKit.Light(root, tower + Vector3.up * 6f + right * 3f - fwd * 3f, 26f, 2f);
        // The line through the turn: in to the middle, round to the right
        LevelKit.LineRun(root, at + Vector3.up * 0.02f + fwd * 1f, centre + Vector3.up * 0.02f, LevelKit.Current);
        LevelKit.LineRun(root, centre + Vector3.up * 0.02f, centre + right * (s * 0.5f - 1f) + Vector3.up * 0.02f, LevelKit.Current);
        // The way out: a ramp up `rise` over ten metres along the new heading, on its own plinth
        headingOut = heading + 90f;
        var rotOut = Quaternion.Euler(0f, headingOut, 0f);
        Vector3 outAt = centre + right * (s * 0.5f);
        const float rampLen = 10f;
        float pitch = Mathf.Atan2(rise, rampLen) * Mathf.Rad2Deg, slant = Mathf.Sqrt(rise * rise + rampLen * rampLen);
        LevelKit.SlabRot(root, outAt + rotOut * new Vector3(0f, rise * 0.5f - 0.5f, rampLen * 0.5f), new Vector3(halfW * 2f, 1f, slant), LevelKit.Floor, "CornerRamp", rotOut * Quaternion.Euler(-pitch, 0f, 0f));
        LevelKit.Slab(root, outAt + rotOut * new Vector3(0f, (ground - outAt.y) * 0.5f - 0.5f, rampLen * 0.5f), Box(rotOut, new Vector3(halfW * 2f, outAt.y - ground - 1f, rampLen)), LevelKit.Dark, "RampPlinth");
        outAt += rotOut * new Vector3(0f, rise, rampLen);
        if (last)
        {
            // The approach to the finish: a straight landing on, so the finish stands clear of the corner's walls
            LevelKit.Slab(root, outAt + rotOut * new Vector3(0f, -0.5f, 6f), Box(rotOut, new Vector3(halfW * 2f, 1f, 12f)), LevelKit.Floor, "FinishApproach");
            LevelKit.Slab(root, outAt + rotOut * new Vector3(0f, (ground - outAt.y) * 0.5f - 0.5f, 6f), Box(rotOut, new Vector3(halfW * 2f, outAt.y - ground - 1f, 12f)), LevelKit.Dark, "ApproachPlinth");
            outAt += rotOut * new Vector3(0f, 0f, 12f);
        }
        return outAt;
    }

    /// An entry deck at the pen before a leg: six metres of run-up on a plinth, the lit line down it, so every space
    /// is entered at speed and on stone (the street, the span and the well all want a run at their first move)
    static void EntryDeck(Transform root, Vector3 pen, Quaternion rot, float halfW)
    {
        float ground = pen.y - GroundDrop;
        LevelKit.Slab(root, pen + rot * new Vector3(0f, -0.5f, 3f), Box(rot, new Vector3(halfW * 2f, 1f, 6f)), LevelKit.Floor, "EntryDeck");
        LevelKit.Slab(root, pen + rot * new Vector3(0f, (ground - pen.y) * 0.5f - 0.5f, 3f), Box(rot, new Vector3(halfW * 2f, pen.y - ground - 1f, 6f)), LevelKit.Dark, "EntryPlinth");
        LevelKit.LineRun(root, pen + rot * new Vector3(0f, 0.02f, 0.5f), pen + rot * new Vector3(0f, 0.02f, 5.5f), LevelKit.Current);
    }

    /// A box's world size under a heading that is a multiple of ninety degrees (the course only turns right angles)
    static Vector3 Box(Quaternion rot, Vector3 size)
    {
        Vector3 r = rot * size;
        return new Vector3(Mathf.Abs(r.x), Mathf.Abs(r.y), Mathf.Abs(r.z));
    }

    /// An axis-aligned volume across a lane that faces `heading`
    static Bounds Volume(Vector3 centre, float across, float tall, float deep, float heading)
    {
        bool alongZ = Mathf.Abs(Mathf.DeltaAngle(heading, 0f)) < 45f || Mathf.Abs(Mathf.DeltaAngle(heading, 180f)) < 45f;
        return new Bounds(centre, alongZ ? new Vector3(across, tall, deep) : new Vector3(deep, tall, across));
    }

    /// Grow the footprint by a leg's box: `pen` its start, `rot` its heading, `half` its half width, `below`/`above` its heights, `len` its length
    static void Encapsulate(ref Bounds b, Vector3 pen, Quaternion rot, float half, float below, float above, float len)
    {
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = 0; sz <= 1; sz++)
            {
                Vector3 p = pen + rot * new Vector3(sx * half, 0f, sz * len);
                b.Encapsulate(p + Vector3.up * below);
                b.Encapsulate(p + Vector3.up * above);
            }
    }
}
