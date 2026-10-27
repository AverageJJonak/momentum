using System.Collections.Generic;
using UnityEngine;
using Cursor = LevelSections.Cursor;

/// <summary>
/// THE SPACES (2026-10-27, the user's notes on VHOLUME: "linear tracks with branching paths... multiple routes, hidden
/// shortcuts, and vertical variations"; "platform gaps, drop-offs, and low crawl-spaces are spaced out to punish
/// button-mashing"; "oppressive scale and emptiness: cold, sparse, and monumental, designed to make the player feel
/// small"; "its way too linear and platformy"). A space is not a set of platforms: it is ONE MONUMENTAL VOLUME —
/// a hall, a well, a plaza, a street between façades, a span — sparse inside, with THREE WAYS THROUGH it that start
/// together and meet again at its far end: OVER (the high way, fast, the tech), THROUGH (the shortcut, a crawl under
/// a beam or a tunnel through a monument, taken sliding or not at all) and AROUND (the long way on the floor, slow
/// and safe). Drop-offs between heights are real drops, crawl-spaces punish a mistimed hop, gaps are never on a
/// rhythm. Everything is big and far apart; nothing stands in a lane that is not meant to. The materials are the
/// act's Deco zone's; the city stands round it all.
///
/// The words a score uses:
///   HALL     the hall of beams: the floor under low beams to slide beneath, the beams overhead to hop, a gallery along a wall
///   WELL     a descent: ledges spiralling down the walls in drop-offs, or the chute straight down, out at the bottom
///   PLAZA    an open square with a monument: the stair over it, the tunnel through it, the long way round it
///   STREET   two façades fifty metres tall: the street, the ledges with the faces between, the cornice high up
///   SPAN     a bridge the size of a viaduct with gaps on no rhythm, the lower deck beneath for the slow way
/// </summary>
public static partial class Course
{
    const float Crawl = 1.35f;   // the clearance under a crawl beam: a slide passes, a hop hits

    /// THE HALL OF BEAMS, 110 m: walls thirty-six metres tall, a ceiling at thirty with skylights, columns every
    /// eighteen metres. AROUND: the floor, under three LOW BEAMS you must slide beneath, up a ramp at the end.
    /// OVER: a ramp-block at the start up onto two great beams nine metres up, their segments broken by gaps on no
    /// rhythm, ending in a drop onto the exit terrace. GALLERY: a ledge along the left wall at five, the face above
    /// it to run, the same drop at the end.
    static float HallOfBeams(Transform root, MovementConfig config, Cursor c, float halfW, ref float y, float d)
    {
        float z0 = c.z, len = 110f, z1 = z0 + len, hw = 28f, wallH = 36f, ground = y - GroundDrop;
        float yExit = y + 2f;
        // The floor: the whole hall, the slow way; the crawl beams across it
        LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, (z0 + z1 - 12f) * 0.5f), new Vector3(hw * 2f, 1f, len - 12f), "HallFloor");
        Block(root, 0f, ground, y - 1f, (z0 + z1) * 0.5f, hw * 2f + 2f, len, "HallPlinth");
        float[] crawlZ = { z0 + 26f, z0 + 48f, z0 + 83f };
        foreach (float cz in crawlZ)
        {
            LevelKit.Slab(root, new Vector3(0f, y + Crawl + 0.6f, cz), new Vector3(hw * 2f, 1.2f, 1.4f), LevelKit.Dark, "CrawlBeam");
            LevelKit.LineRun(root, new Vector3(-6f, y, cz - 4f), new Vector3(6f, y, cz - 4f), c.zone);
        }
        LevelKit.Ramp(root, 0f, hw * 2f, z1 - 12f, y, z1, yExit);
        // OVER: the ramp-block up, then two beams nine metres up with gaps on no rhythm
        float beamY = y + 9f;
        LevelKit.Ramp(root, -hw + 8f, 10f, z0 + 2f, y, z0 + 20f, beamY);
        Block(root, -hw + 8f, y, y + 0.6f, z0 + 11f, 10f, 18f, "RampCore");
        for (int b = 0; b < 2; b++)
        {
            float bx = b == 0 ? -hw + 8f : hw - 10f, z = z0 + 20f;
            bool first = true;
            while (z < z1 - 16f)
            {
                float seg = R(12f, 20f), gap = first ? 0f : Pick(new[] { 6f, 7.5f, 9f, 11f, 13f });
                float zs = z + gap, ze = Mathf.Min(zs + seg, z1 - 16f);
                LevelKit.Slab(root, new Vector3(bx, beamY - 1.5f, (zs + ze) * 0.5f), new Vector3(6f, 3f, ze - zs), LevelKit.Dark, "GreatBeam");
                LevelKit.LineRun(root, new Vector3(bx, beamY, zs + 0.5f), new Vector3(bx, beamY, ze - 0.5f), c.zone);
                z = ze; first = false;
            }
            // The drop at the end: a lip to launch from, the exit terrace below and beyond
            LevelKit.Lip(root, new Vector3(bx, beamY + 0.4f, z - 0.5f), new Vector3(5f, 0.8f, 1f), LevelKit.Limestone, "BeamLip");
        }
        // The bridge between the two beams at midway: the crossing, a corner boost at each end
        LevelKit.Slab(root, new Vector3(0f, beamY - 1.5f, z0 + 58f), new Vector3(hw * 2f - 20f, 3f, 5f), LevelKit.Dark, "CrossBeam");
        // GALLERY: a ledge along the left wall at five, the face to run above it
        LevelKit.Lip(root, new Vector3(-hw + 1.2f, y + 4.6f, (z0 + z1 - 20f) * 0.5f), new Vector3(2.4f, 0.8f, len - 28f), LevelKit.Limestone, "Gallery");
        LevelKit.BrassStrip(root, new Vector3(-hw + 1.2f, y + 5.01f, z0 + 6f), new Vector3(-hw + 1.2f, y + 5.01f, z1 - 22f));
        // The exit terrace: everyone lands here
        LevelKit.FloorSlab(root, new Vector3(0f, yExit - 0.5f, z1 - 6f), new Vector3(hw * 2f, 1f, 12f), "HallExit");
        Hall(root, hw, ground, ground + wallH + GroundDrop, z0, z1, true);
        for (float lz = z0 + 10f; lz < z1; lz += 22f) LevelKit.Spot(root, new Vector3(0f, ground + wallH + GroundDrop - 2f, lz), Vector3.down, 60f, 40f, 12f, new Color(1f, 0.9f, 0.7f));
        y = yExit;
        return z1;
    }

    /// THE WELL, a descent of thirty metres: a square well forty on a side, walls to the sky, one skylight. You come
    /// in at the top on a terrace. AROUND: ledges spiral down the walls, each a drop-off of six or seven metres onto
    /// the next, the faces between them to run. THROUGH: the chute, a slide from the terrace's edge straight to the
    /// floor, committed in one go. The floor at the bottom is the way out, under an arch in the far wall.
    static float Well(Transform root, MovementConfig config, Cursor c, float halfW, ref float y, float d)
    {
        float z0 = c.z, side = 40f, hw = side * 0.5f, depth = 30f, z1 = z0 + side + 16f, wallH = 44f;
        float yFloor = y - depth, ground = yFloor - 1f;
        // The terrace you enter on, its edge over the well
        LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, z0 + 5f), new Vector3(hw * 2f, 1f, 10f), "WellTerrace");
        Block(root, 0f, ground, y - 1f, z0 + 5f, hw * 2f, 10f, "TerraceMass");
        // The floor, and the arch out under the far wall
        LevelKit.FloorSlab(root, new Vector3(0f, yFloor - 0.5f, z0 + 10f + side * 0.5f), new Vector3(hw * 2f, 1f, side), "WellFloor");
        LevelKit.FloorSlab(root, new Vector3(0f, yFloor - 0.5f, z1 - 3f), new Vector3(halfW * 2f, 1f, 6f), "WellOut");
        // The walls: four faces, the near one over the terrace, the far one with the arch, the sides whole
        float wz0 = z0 + 10f, wz1 = z0 + 10f + side, top = yFloor + wallH;
        for (int s = -1; s <= 1; s += 2) LevelKit.KickWall(root, new Vector3(s * (hw + 0.6f), (yFloor + top) * 0.5f, (wz0 + wz1) * 0.5f), new Vector3(1.2f, top - yFloor, side), LevelKit.Wall);
        LevelKit.Slab(root, new Vector3(0f, (y + 1f + top) * 0.5f, wz0 - 0.6f), new Vector3(hw * 2f + 2.4f, top - y - 1f, 1.2f), LevelKit.Wall, "WellNearWall");
        for (int s = -1; s <= 1; s += 2) LevelKit.Slab(root, new Vector3(s * (halfW + (hw - halfW) * 0.5f), (yFloor + top) * 0.5f, wz1 + 0.6f), new Vector3(hw - halfW, top - yFloor, 1.2f), LevelKit.Wall, "WellFarWall");
        LevelKit.Slab(root, new Vector3(0f, (yFloor + 6f + top) * 0.5f, wz1 + 0.6f), new Vector3(halfW * 2f, top - yFloor - 6f, 1.2f), LevelKit.Wall, "WellArchHead");
        // AROUND: the ledges spiralling down, a drop-off each
        int walls = 4; float ly = y - 2f, lz = wz0 + 6f;
        for (int i = 0; i < 5; i++)
        {
            int w = i % walls;
            float dropTo = ly - R(6f, 7.5f);
            Vector3 centre; Vector3 size;
            switch (w)
            {
                case 0: centre = new Vector3(hw - 1.4f, ly - 0.4f, wz0 + side * 0.5f); size = new Vector3(2.8f, 0.8f, side - 8f); break;
                case 1: centre = new Vector3(0f, ly - 0.4f, wz1 - 1.4f); size = new Vector3(side - 8f, 0.8f, 2.8f); break;
                case 2: centre = new Vector3(-hw + 1.4f, ly - 0.4f, wz0 + side * 0.5f); size = new Vector3(2.8f, 0.8f, side - 8f); break;
                default: centre = new Vector3(0f, ly - 0.4f, wz0 + 1.4f); size = new Vector3(side - 8f, 0.8f, 2.8f); break;
            }
            if (ly - 0.8f > yFloor + 1f) LevelKit.Lip(root, centre, size, LevelKit.Limestone, "WellLedge");
            ly = dropTo;
        }
        // THROUGH: the chute from the terrace's edge to the floor, down the middle
        LevelKit.Ramp(root, 0f, 8f, wz0, y, wz0 + side * 0.55f, yFloor + 0.3f);
        LevelKit.BrassStrip(root, new Vector3(0f, y + 0.02f, z0 + 7f), new Vector3(0f, y + 0.02f, z0 + 9.8f));
        // The light: one skylight's shaft down the well, the spot with it; lamps on the ledges' walls
        LevelKit.Shaft(root, new Vector3(0f, top - 2f, wz0 + side * 0.5f), new Vector2(10f, 10f), depth + 12f);
        LevelKit.Spot(root, new Vector3(0f, top, wz0 + side * 0.5f), Vector3.down, wallH + 10f, 40f, 18f, new Color(1f, 0.9f, 0.72f));
        for (float wy = yFloor + 5f; wy < y; wy += 8f) for (int s = -1; s <= 1; s += 2) LevelKit.Light(root, new Vector3(s * (hw - 2f), wy, wz0 + side * 0.5f), 18f, 1.3f);
        y = yFloor;
        return z1;
    }

    /// THE PLAZA, 120 m: an open square under the sky, sixty wide, the street level nine metres under the entry
    /// terrace, a MONUMENT in its middle forty on a side and fourteen tall. OVER: the grand stair up its front, the
    /// run across its top, the drop off its back onto the exit terrace. THROUGH: the tunnel at street level straight
    /// through it, one and a third metres high — a slide, or nothing. AROUND: the square either side of it, long and
    /// safe, up a ramp at the far end. Colonnade stubs and lamps at the edges; nothing else.
    static float Plaza(Transform root, MovementConfig config, Cursor c, float halfW, ref float y, float d)
    {
        float z0 = c.z, len = 120f, z1 = z0 + len, hw = 30f, street = y - GroundDrop, yExit = y - 4f;
        // The square
        LevelKit.FloorSlab(root, new Vector3(0f, street - 0.5f, (z0 + z1) * 0.5f), new Vector3(hw * 2f, 1f, len), "Square");
        // The entry terrace with its two ramps down to the square either side
        LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, z0 + 5f), new Vector3(halfW * 2f, 1f, 10f), "PlazaTerrace");
        Block(root, 0f, street, y - 1f, z0 + 5f, halfW * 2f, 10f, "TerraceMass");
        for (int s = -1; s <= 1; s += 2) LevelKit.Ramp(root, s * (halfW + 4f), 8f, z0 + 10f, y, z0 + 28f, street);
        // THE MONUMENT: a block, a grand stair up its front, the tunnel through it
        float mz0 = z0 + 36f, mSide = 40f, mz1 = mz0 + mSide, mTop = street + 14f, tunnelW = 5f;
        for (int s = -1; s <= 1; s += 2)
            LevelKit.Slab(root, new Vector3(s * (tunnelW * 0.5f + (mSide * 0.5f - tunnelW * 0.5f) * 0.5f), (street + mTop) * 0.5f, (mz0 + mz1) * 0.5f), new Vector3(mSide * 0.5f - tunnelW * 0.5f, mTop - street, mSide), LevelKit.Dark, "Monument");
        LevelKit.Slab(root, new Vector3(0f, (street + Crawl + mTop) * 0.5f, (mz0 + mz1) * 0.5f), new Vector3(tunnelW, mTop - street - Crawl, mSide), LevelKit.Dark, "MonumentOverTunnel");
        LevelKit.FloorSlab(root, new Vector3(0f, mTop - 0.2f, (mz0 + mz1) * 0.5f), new Vector3(mSide, 0.4f, mSide), "MonumentTop");
        LevelKit.Ramp(root, 0f, 12f, mz0 - 20f, street, mz0, mTop);
        LevelKit.Nosings(root, 0f, 12f, mz0 - 20f, street, mz0, mTop, 1.6f);
        Block(root, 0f, street, street + 0.6f, mz0 - 10f, 12f, 20f, "StairMass");
        LevelKit.LineRun(root, new Vector3(0f, street, mz0 - 20f), new Vector3(0f, street, mz0 - 0.5f), c.zone);
        LevelKit.LineRun(root, new Vector3(0f, mTop, mz0 + 0.5f), new Vector3(0f, mTop, mz1 - 0.5f), c.zone);
        LevelKit.Lip(root, new Vector3(0f, mTop + 0.4f, mz1 - 0.5f), new Vector3(10f, 0.8f, 1f), LevelKit.Limestone, "MonumentLip");
        LevelKit.Emblem(root, new Vector3(0f, street + 9f, mz0 - 0.3f), Quaternion.LookRotation(Vector3.back), 4f, LevelKit.Brass, LevelKit.Dark);
        // The exit terrace: the monument's drop lands on it, the square's ramp climbs to it
        LevelKit.FloorSlab(root, new Vector3(0f, yExit - 0.5f, z1 - 8f), new Vector3(halfW * 2f + 8f, 1f, 16f), "PlazaExit");
        Block(root, 0f, street, yExit - 1f, z1 - 8f, halfW * 2f + 8f, 16f, "ExitMass");
        for (int s = -1; s <= 1; s += 2) LevelKit.Ramp(root, s * (halfW + 6f), 6f, z1 - 30f, street, z1 - 16f, yExit);
        // The edges: colonnade stubs and lamps, the square's only furniture
        for (float cz = z0 + 12f; cz < z1 - 12f; cz += 24f)
            for (int s = -1; s <= 1; s += 2)
            {
                LevelKit.Slab(root, new Vector3(s * (hw - 3f), street + 7f, cz), new Vector3(2.4f, 14f, 2.4f), LevelKit.Limestone, "Colonnade");
                LevelKit.Light(root, new Vector3(s * (hw - 6f), street + 5f, cz + 12f), 24f, 1.6f);
            }
        LevelKit.Spot(root, new Vector3(0f, mTop + 16f, mz0 - 6f), new Vector3(0f, -1f, 0.3f), 40f, 50f, 14f, new Color(1f, 0.9f, 0.7f));
        y = yExit;
        return z1;
    }

    /// THE STREET, 100 m: two façades fifty metres tall and twelve apart, lit windows up them. AROUND: the street,
    /// nine metres down, under the ledges, a ramp up at the end. ALONG: ledges on alternating faces at the line, the
    /// faces between them to run. OVER: the cornice twelve metres up, reached by a corner boost at the first
    /// ledge's end, a straight run to a drop at the far end.
    static float Street(Transform root, MovementConfig config, Cursor c, float halfW, ref float y, float d)
    {
        float z0 = c.z, len = 100f, z1 = z0 + len, faceX = 6f, faceH = 50f, street = y - GroundDrop, cornice = y + 12f;
        LevelKit.FloorSlab(root, new Vector3(0f, street - 0.5f, (z0 + z1) * 0.5f), new Vector3(faceX * 2f, 1f, len), "StreetFloor");
        LevelKit.Ramp(root, 0f, faceX * 2f, z1 - 18f, street, z1, y);
        int side = rng.NextDouble() < 0.5 ? -1 : 1;
        float z = z0 + 2f;
        bool first = true;
        while (z < z1 - 14f)
        {
            float ledgeLen = R(10f, 16f);
            LevelKit.Lip(root, new Vector3(side * (faceX - 1.1f), y - 0.4f, z + ledgeLen * 0.5f), new Vector3(2.2f, 0.8f, ledgeLen), LevelKit.Limestone, "StreetLedge");
            LevelKit.LineRun(root, new Vector3(side * (faceX - 1.1f), y, z + 1f), new Vector3(side * (faceX - 1.1f), y, z + ledgeLen - 1f), c.zone);
            if (first) LevelKit.BoostCorner(root, new Vector3(side * faceX, y + 3f, z + ledgeLen + 0.6f), new Vector3(1.4f, 8f, 1.2f), LevelKit.Wall);
            z += ledgeLen + Mathf.Lerp(4f, 7f, d) + R(-0.6f, 0.8f);
            side = -side; first = false;
        }
        // The cornice: the high way, both sides, to the drop
        for (int s = -1; s <= 1; s += 2)
        {
            LevelKit.Lip(root, new Vector3(s * (faceX - 1f), cornice - 0.4f, (z0 + z1 - 10f) * 0.5f), new Vector3(2f, 0.8f, len - 14f), LevelKit.Limestone, "Cornice");
            LevelKit.KickWall(root, new Vector3(s * (faceX + 0.5f), street + faceH * 0.5f, (z0 + z1) * 0.5f), new Vector3(1f, faceH, len), LevelKit.Wall);
            for (float wy = y + 5f; wy < street + faceH - 4f; wy += 7f)
                for (float wz = z0 + 4f; wz < z1 - 4f; wz += 8f)
                    LevelKit.Slab(root, new Vector3(s * (faceX - 0.05f), wy, wz), new Vector3(0.12f, 2.6f, 1.6f), LevelKit.Accent, "Window");
            for (float lz = z0 + 8f; lz < z1; lz += 16f) LevelKit.Light(root, new Vector3(s * (faceX - 1.5f), y + 3f, lz), 18f, 1.4f);
        }
        // The exit: the street's ramp and the ledges arrive at the line; the cornice drops onto the deck beyond
        LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, z1 - 4f), new Vector3(faceX * 2f, 1f, 8f), "StreetExit");
        return z1;
    }

    /// THE SPAN, 110 m: a viaduct of stone at the line, twelve wide, its deck broken by gaps on no rhythm (seven,
    /// ten, thirteen), a drop of six at its far end; the LOWER DECK nine metres under it, whole, the slow way, up
    /// a ramp at the end; the piers to the ground far below; a tower at each end.
    static float Span(Transform root, MovementConfig config, Cursor c, float halfW, ref float y, float d)
    {
        float z0 = c.z, len = 110f, z1 = z0 + len, w = 12f, lower = y - GroundDrop, ground = lower - 24f, yExit = y - 6f;
        float[] gaps = { 7f, 10f, 13f };
        float z = z0;
        for (int i = 0; i <= gaps.Length; i++)
        {
            float seg = i == gaps.Length ? z1 - 14f - z : R(16f, 26f);
            LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, z + seg * 0.5f), new Vector3(w, 1f, seg), "SpanDeck");
            LevelKit.Slab(root, new Vector3(0f, y - 2.5f, z + seg * 0.5f), new Vector3(w + 1f, 3f, seg), LevelKit.Dark, "SpanBeam");
            LevelKit.LineRun(root, new Vector3(0f, y, z + 0.5f), new Vector3(0f, y, z + seg - 0.5f), c.zone);
            // A pier under each segment's middle, to the ground
            LevelKit.Slab(root, new Vector3(0f, (ground + y - 4f) * 0.5f, z + seg * 0.5f), new Vector3(5f, y - 4f - ground, 5f), LevelKit.Dark, "Pier");
            z += seg;
            if (i < gaps.Length) z += gaps[i] + d * 1.5f;
        }
        // The lower deck: whole, the slow way, its ramp up to the exit
        LevelKit.FloorSlab(root, new Vector3(0f, lower - 0.5f, (z0 + z1 - 18f) * 0.5f), new Vector3(w + 6f, 1f, len - 18f), "LowerDeck");
        LevelKit.Ramp(root, 0f, w + 6f, z1 - 18f, lower, z1, yExit);
        LevelKit.Slab(root, new Vector3(0f, lower - 2f, (z0 + z1 - 18f) * 0.5f), new Vector3(w + 7f, 3f, len - 18f), LevelKit.Dark, "LowerBeam");
        // The drop from the deck's far end onto the exit, the towers at both ends
        LevelKit.FloorSlab(root, new Vector3(0f, yExit - 0.5f, z1 - 4f), new Vector3(w + 6f, 1f, 8f), "SpanExit");
        for (int s = -1; s <= 1; s += 2)
            for (int e = 0; e < 2; e++)
                LevelKit.Slab(root, new Vector3(s * (w * 0.5f + 7f), (ground + y + 26f) * 0.5f, e == 0 ? z0 - 4f : z1 + 2f), new Vector3(8f, y + 26f - ground, 8f), LevelKit.Limestone, "SpanTower");
        LevelKit.FloorSlab(root, new Vector3(0f, ground - 0.5f, (z0 + z1) * 0.5f), new Vector3(60f, 1f, len + 20f), "Ground");
        for (float lz = z0 + 10f; lz < z1; lz += 20f) for (int s = -1; s <= 1; s += 2) LevelKit.Light(root, new Vector3(s * (w * 0.5f + 1.5f), y + 3.5f, lz), 18f, 1.3f);
        y = yExit;
        return z1;
    }
}
