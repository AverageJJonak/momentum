using UnityEngine;
using Cursor = LevelSections.Cursor;

/// <summary>
/// THE COURSE, the climbs and the drops: STAIR, SHAFT (a climb, or a drop chute), CHUTE, DROP-IN. See
/// LevelSectionsCourse.cs for the grammar. Height is a currency: every one of these pays or costs it, and every
/// drop lands on a slide or a pad.
/// </summary>
public static partial class Course
{
    /// STAIR. A sweeping stair never walked: a full-width flight between two landings (nosed in brass, a ramp
    /// under 18° so the main line runs it), a brass BANISTER down each edge (a lip a metre over the flight; a slide
    /// on the way down), wall-run walls either side of the flight, kick pillars standing in it (lanes 24 m and
    /// wider), and — climbing — a 1.2 m LIP at the top: jump it at run speed, corner-boost it for height.
    /// `rise` > 0 climbs, < 0 descends; `len` grows if the flight would be steeper than the main line runs.
    /// MAIN: the flight. FAST, up: wall run → pillar kick → the top lip's WALL COMBO boost. Down: hop onto a
    /// banister, slide it, slide-jump off its foot (×1.15).
    /// FUEL: a bouncer at the top (climbing) or the foot (descending), two footmen mid-flight, a page over it.
    public static Cursor Stair(Transform root, MovementConfig config, Cursor c, float rise, float len, float halfW)
    {
        Begin(c);
        halfW = Mathf.Max(halfW, 8f);
        const float landing = 7f;
        len = Mathf.Max(len, Mathf.Max(40f, 2f * landing + Run(rise) + 2f));
        float z0 = c.z, z1 = z0 + len, y0 = c.floorY, y1 = y0 + rise;
        float low = Mathf.Min(y0, y1), top = Mathf.Max(y0, y1);
        bool climb = rise > 0f;
        float boxH = top + HeadLips - low;
        Threshold(root, c, halfW, y0, top + HeadLips - y0);
        Shell(root, c, halfW, low, boxH, z0, z1, Walled);
        var zone = c.zone;

        // The landings (the top one thick enough to hide the flight's end under it), the flight, the nosings
        float sz0 = z0 + landing, sz1 = z1 - landing;
        float flightTop = climb ? y1 - 1.2f : y1;
        float fw = halfW * 2f - 3.2f;
        LevelKit.FloorSlab(root, new Vector3(0f, y0 - 0.8f, z0 + landing * 0.5f), new Vector3(halfW * 2f + 2f * T, 1.6f, landing + T), "Landing");
        LevelKit.FloorSlab(root, new Vector3(0f, y1 - 0.8f, z1 - landing * 0.5f), new Vector3(halfW * 2f + 2f * T, 1.6f, landing + T), "Landing");
        LevelKit.Ramp(root, 0f, fw, sz0, y0, sz1, flightTop, LevelKit.Floor);
        LevelKit.Nosings(root, 0f, fw, sz0, y0, sz1, flightTop);
        if (climb)
        {
            // The lip at the top: the landing's own edge over the flight's end, brass along it, authored
            LevelKit.BrassStrip(root, new Vector3(-fw * 0.5f + 0.2f, y1, sz1 + 0.12f), new Vector3(fw * 0.5f - 0.2f, y1, sz1 + 0.12f));
            TechMarkers.MarkLip(new Vector3(0f, y1, sz1 + 0.5f), new Vector3(fw, 0.9f, 1.2f));
        }
        // The banisters: brass, a metre over the flight, down both edges
        for (int s = -1; s <= 1; s += 2)
            LevelKit.Banister(root, s * (halfW - 0.8f), 1.4f, sz0, y0 + 1f, sz1, flightTop + 1f);
        // The walls beside the flight are wall-run walls: a flute at each end of the run
        float wr0 = sz0 + 4f, wr1 = Mathf.Max(wr0 + WallRunMin, sz1 - 4f);
        for (int s = -1; s <= 1; s += 2)
        {
            float ya = Mathf.Lerp(y0, flightTop, Mathf.InverseLerp(sz0, sz1, wr0)), yb = Mathf.Lerp(y0, flightTop, Mathf.InverseLerp(sz0, sz1, wr1));
            LevelKit.Flute(root, new Vector3(s * halfW, ya + 2.5f, wr0), Vector3.left * s, 4f);
            LevelKit.Flute(root, new Vector3(s * halfW, yb + 2.5f, wr1), Vector3.left * s, 4f);
        }
        // Kick pillars standing in the flight, two rows, where the flight is wide enough to keep a 6 m lane between
        if (halfW >= 12f)
            for (float z = sz0 + 10f; z < sz1 - 6f; z += 12f)
            {
                float ys = Mathf.Lerp(y0, flightTop, Mathf.InverseLerp(sz0, sz1, z));
                for (int s = -1; s <= 1; s += 2)
                    LevelKit.BoostCorner(root, new Vector3(s * halfW * 0.3f, ys + 2.6f, z), new Vector3(1.2f, 6f, 1.2f), LevelKit.Wall);
            }
        // Fuel
        float midZ = (sz0 + sz1) * 0.5f, midY = (y0 + flightTop) * 0.5f;
        Fuel(root, config, c, EnemyKind.Warden, new Vector3(0f, y1, z1 - landing * 0.5f), climb ? "top landing" : "stair foot");   // the far landing either way: over the lip, or at the slide's exit
        for (int s = -1; s <= 1; s += 2) Fuel(root, config, c, EnemyKind.Dummy, new Vector3(s * fw * 0.25f, midY + 0.15f, midZ), "flight");
        Fuel(root, config, c, EnemyKind.Turret, new Vector3(0f, midY + 4.5f, midZ + 6f), "over the flight");
        if (!climb) LevelKit.LineRun(root, new Vector3(-(halfW - 0.8f), y0, z0 + 2f), new Vector3(-(halfW - 0.8f), y0, sz0 - 1f), zone);
        LevelKit.GlobeLamp(root, new Vector3(0f, top + HeadLips - 3f, midZ), top + HeadLips);
        Dressing(root, c, halfW, low, boxH, z0, z1, false);
        return Open(c, z1, y1, halfW, top + HeadLips - y1);
    }

    /// SHAFT. `rise` > 0: a vertical climb. Ramps under 18° switch back up the two long walls (the main line), with
    /// a cross ledge at each end — a LIP every ~6 m; the shaft's own walls are the kick walls between the ramps, an
    /// orb hangs at the top. FAST: straight up the middle, wall kick to wall kick, a WALL COMBO corner boost on each
    /// ledge, or the orb. A footman on every ledge, a page hovering mid-shaft. `rise` < 0: a drop chute (see ShaftDrop).
    /// `halfW` 5–7: the kick walls 10–14 m apart.
    public static Cursor Shaft(Transform root, MovementConfig config, Cursor c, float rise, float halfW)
    {
        Begin(c);
        halfW = Mathf.Clamp(halfW, 5f, 7f);
        if (rise < 0f) return ShaftDrop(root, config, c, -rise, halfW);
        rise = Mathf.Max(rise, 6f);
        int legs = Mathf.CeilToInt(rise / 6.5f);
        if (legs % 2 == 0) legs++;
        float legRise = rise / legs;
        const float ledge = 3f, legW = 2.8f;
        float len = Mathf.Max(26f, Run(legRise) + 2f * ledge + 2f);
        float z0 = c.z, z1 = z0 + len, y = c.floorY, topY = y + rise, height = rise + HeadLips;
        Threshold(root, c, halfW, y, height);
        Shell(root, c, halfW, y, height, z0, z1, Roofed);
        // The north face under the top ledge is this space's own (the lane leaves at the top)
        LevelKit.WallFace(root, Face.North, z1, -halfW - T * 0.5f, halfW + T * 0.5f, y, topY - 1f);
        var zone = c.zone;
        float rz0 = z0 + ledge, rz1 = z1 - ledge, midZ = z0 + len * 0.5f;
        for (int i = 0; i < legs; i++)
        {
            bool west = i % 2 == 0;   // even legs climb north along the west wall, odd legs south along the east
            float x = west ? -(halfW - legW * 0.5f) : (halfW - legW * 0.5f);
            float ya = y + i * legRise, yb = ya + legRise;
            if (west) LevelKit.Ramp(root, x, legW, rz0, ya, rz1, yb, LevelKit.Floor);
            else LevelKit.Ramp(root, x, legW, rz0, yb, rz1, ya, LevelKit.Floor);   // always the lower-z end first
            // The ledge the leg arrives at: full width, a lip. The last one is the exit floor
            float lz = west ? z1 - ledge * 0.5f : z0 + ledge * 0.5f;
            if (i < legs - 1)
            {
                LevelKit.Lip(root, new Vector3(0f, yb - 0.5f, lz), new Vector3(halfW * 2f, 1f, ledge), LevelKit.Floor, "ShaftLedge");
                Fuel(root, config, c, EnemyKind.Dummy, new Vector3(0f, yb, lz), "ledge " + (i + 1));
            }
            else LevelKit.FloorSlab(root, new Vector3(0f, yb - 0.5f, z1 - ledge * 0.5f + T * 0.25f), new Vector3(halfW * 2f + 2f * T, 1f, ledge + T * 0.5f), "Landing");
        }
        // The kick walls between the legs: a flute mid-height on each wall; the orb at the top
        for (int s = -1; s <= 1; s += 2) LevelKit.Flute(root, new Vector3(s * halfW, y + rise * 0.5f, midZ), Vector3.left * s, 4f);
        LevelKit.Orb(root, new Vector3(0f, topY + 2.5f, midZ));
        Fuel(root, config, c, EnemyKind.Turret, new Vector3(0f, y + rise * 0.5f, midZ), "mid-shaft");
        LevelKit.LineRun(root, new Vector3(0f, y, z0 + 2f), new Vector3(-(halfW - legW * 0.5f), y, rz0 - 0.5f), zone);
        LevelKit.GlobeLamp(root, new Vector3(0f, topY + HeadLips - 3f, midZ), topY + HeadLips);
        Dressing(root, c, halfW, y, height, z0, z1, false);
        return Open(c, z1, topY, halfW, HeadLips);
    }

    /// The drop: the floor runs to an edge, a 32° chute takes the whole drop, a pad at its foot throws you on. The
    /// walls are the kick walls (a controlled descent), the orb over the edge is the line down the middle. FAST:
    /// jump the edge far, land low on the chute (a slide landing turns fall into speed), or swing the orb.
    /// FUEL: a bouncer at the chute's foot, a page over its middle.
    static Cursor ShaftDrop(Transform root, MovementConfig config, Cursor c, float drop, float halfW)
    {
        drop = Mathf.Max(drop, 4f);
        const float lipRun = 6f, foot = 7f;
        float slideRun = drop / Mathf.Tan(32f * Mathf.Deg2Rad);
        float len = lipRun + slideRun + foot;
        float z0 = c.z, z1 = z0 + len, y = c.floorY, low = y - drop, height = drop + Head;
        Threshold(root, c, halfW, y, Head);
        Shell(root, c, halfW, low, height, z0, z1, Walled);
        var zone = c.zone;
        float sz0 = z0 + lipRun, sz1 = sz0 + slideRun;
        LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, z0 + lipRun * 0.5f), new Vector3(halfW * 2f + 2f * T, 1f, lipRun + T), "Landing");
        LevelKit.Ramp(root, 0f, halfW * 2f, sz0, y, sz1, low, LevelKit.Slope);
        LevelKit.FloorSlab(root, new Vector3(0f, low - 0.5f, z1 - foot * 0.5f), new Vector3(halfW * 2f + 2f * T, 1f, foot + T), "Landing");
        LevelKit.JumpPad(root, new Vector3(0f, low, z1 - 3f), 0f, 9f, 9f);
        for (int i = 0; i < 4; i++)
        {
            float z = sz0 + 2f + i * (slideRun - 4f) / 3f, yr = Mathf.Lerp(y, low, (z - sz0) / slideRun) + 1.2f;
            LevelKit.Stripes(root, new Vector3(-halfW + 0.1f, yr, z), new Vector3(0.1f, 0.1f, 4f));
            LevelKit.Stripes(root, new Vector3(halfW - 0.1f, yr, z), new Vector3(0.1f, 0.1f, 4f));
        }
        for (int s = -1; s <= 1; s += 2) LevelKit.Flute(root, new Vector3(s * halfW, y - drop * 0.4f + 2f, (sz0 + sz1) * 0.5f), Vector3.left * s, 4f);
        LevelKit.Orb(root, new Vector3(0f, y + 3.5f, sz0 + slideRun * 0.4f));
        Fuel(root, config, c, EnemyKind.Warden, new Vector3(0f, low, sz1 + 2.5f), "chute foot");
        Fuel(root, config, c, EnemyKind.Turret, new Vector3(0f, Mathf.Lerp(y, low, 0.5f) + 3f, (sz0 + sz1) * 0.5f), "over the chute");
        LevelKit.LineRun(root, new Vector3(0f, y, z0 + 2f), new Vector3(0f, y, sz0 - 0.4f), zone);
        Dressing(root, c, halfW, low, height, z0, z1, false);
        return Open(c, z1, low, halfW, Head + drop);
    }

    /// CHUTE. A 20–35° slide between brass banister lips, its foot a metre over the run-out (the hop), a brass lip
    /// across the run-out with the lane open in the middle; over each wing of the lip a glass pane with a pad behind.
    /// MAIN: the slide, the hop, the opening. FAST: corner-boost the lip's wing, through the glass at speed, onto
    /// the pad — thrown into the next space. Pitch under 20° and the slide takes what it needs, the run-out the rest.
    /// FUEL: a bouncer at the slide's exit, two footmen on the slide, a page over it.
    public static Cursor Chute(Transform root, MovementConfig config, Cursor c, float drop, float len, float halfW)
    {
        Begin(c);
        halfW = Mathf.Max(halfW, 7f);
        drop = Mathf.Max(drop, 6f);
        const float mouth = 4f, tail = 14f;
        float runMin = drop / Mathf.Tan(ChuteMax * Mathf.Deg2Rad), runMax = drop / Mathf.Tan(ChuteMin * Mathf.Deg2Rad);
        float run = Mathf.Clamp(len - mouth - tail, runMin, runMax);
        len = Mathf.Max(len, mouth + run + tail);
        float z0 = c.z, z1 = z0 + len, y = c.floorY, low = y - drop, height = drop + Head, foot = low + 1f;
        Threshold(root, c, halfW, y, Head);
        Shell(root, c, halfW, low, height, z0, z1, Walled);
        var zone = c.zone;
        float sz0 = z0 + mouth, sz1 = sz0 + run, sw = halfW * 2f - 2.8f;
        LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, z0 + mouth * 0.5f), new Vector3(halfW * 2f + 2f * T, 1f, mouth + T), "Landing");
        LevelKit.Ramp(root, 0f, sw, sz0, y, sz1, foot, LevelKit.Slope);
        LevelKit.Slab(root, new Vector3(0f, low + 0.5f, sz1 - 1f), new Vector3(sw, 1f, 2f), LevelKit.Floor, "ChuteFoot");
        for (int s = -1; s <= 1; s += 2) LevelKit.Banister(root, s * (halfW - 0.7f), 1.4f, sz0, y + 1f, sz1, foot + 1f);
        LevelKit.FloorSlab(root, new Vector3(0f, low - 0.5f, (sz1 + z1) * 0.5f), new Vector3(halfW * 2f + 2f * T, 1f, z1 - sz1 + T), "Landing");
        // The lip across the run-out, open in the middle; the glass over its wings; the pads behind
        float lipZ = sz1 + 5f, open = 3.5f;
        for (int s = -1; s <= 1; s += 2)
        {
            float xc = s * (open + halfW) * 0.5f, w = halfW - open;
            LevelKit.Lip(root, new Vector3(xc, low + 0.5f, lipZ), new Vector3(w, 1.0f, 0.8f));
            LevelKit.Glass(root, new Vector3(xc, low + 2.5f, lipZ), new Vector3(w - 0.2f, 3f, 0.3f), config);
            LevelKit.JumpPad(root, new Vector3(s * (open + 3f), low, lipZ + 4f), 0f, 12f, 8f);
            LevelKit.LineRun(root, new Vector3(s * (open + 3f), low, sz1 + 0.6f), new Vector3(s * (open + 3f), low, lipZ - 0.6f), zone);
        }
        for (int i = 0; i < 4; i++)
        {
            float z = sz0 + 2f + i * (run - 4f) / 3f, yr = Mathf.Lerp(y, foot, (z - sz0) / run) + 1.6f;
            LevelKit.Stripes(root, new Vector3(-halfW + 0.1f, yr, z), new Vector3(0.1f, 0.1f, 4f));
            LevelKit.Stripes(root, new Vector3(halfW - 0.1f, yr, z), new Vector3(0.1f, 0.1f, 4f));
        }
        Fuel(root, config, c, EnemyKind.Warden, new Vector3(0f, low, lipZ + 3f), "slide exit");
        for (int s = -1; s <= 1; s += 2) Fuel(root, config, c, EnemyKind.Dummy, new Vector3(s * halfW * 0.35f, Mathf.Lerp(y, foot, 0.45f) + 0.2f, sz0 + run * 0.45f), "slide");
        Fuel(root, config, c, EnemyKind.Turret, new Vector3(0f, Mathf.Lerp(y, foot, 0.6f) + 3.5f, sz0 + run * 0.6f), "over the slide");
        Dressing(root, c, halfW, low, height, z0, z1, false);
        return Open(c, z1, low, halfW, Head + drop);
    }

    /// DROP-IN. The floor ends at an edge; 4 m under it a 30° slide starts, runs the rest of the drop and stops a
    /// metre over the run-out (the hop); a glass pane stands over the hop's middle. The edge's own face is the
    /// recovery wall (a cream flute marks it): fall short and you kick off it onto the slide.
    /// MAIN: off the edge, down the slide, the hop. FAST: a dash-jump far out lands low on the slide for more speed;
    /// the hop through the glass at speed. Drops are clamped to 6–12 m.
    /// FUEL: a bouncer past the hop, a page over the slide.
    public static Cursor DropIn(Transform root, MovementConfig config, Cursor c, float drop, float halfW)
    {
        Begin(c);
        halfW = Mathf.Max(halfW, 7f);
        drop = Mathf.Clamp(drop, 6f, DropMax);
        const float approach = 10f, fall = 4f, tail = 14f;
        float run = (drop - fall - 1f) / Mathf.Tan(30f * Mathf.Deg2Rad);
        float len = approach + run + tail;
        float z0 = c.z, z1 = z0 + len, y = c.floorY, low = y - drop, height = drop + Head, foot = low + 1f;
        Threshold(root, c, halfW, y, Head);
        Shell(root, c, halfW, low, height, z0, z1, Walled);
        var zone = c.zone;
        float ez = z0 + approach;
        LevelKit.FloorSlab(root, new Vector3(0f, y - 0.5f, z0 + approach * 0.5f), new Vector3(halfW * 2f + 2f * T, 1f, approach + T), "Landing");
        // The recovery wall: the edge's face, from under the approach down to the slide's start
        LevelKit.KickWall(root, new Vector3(0f, y - 1f - (fall - 1f) * 0.5f, ez - 0.5f), new Vector3(halfW * 2f, fall - 1f, 1f));
        LevelKit.Ramp(root, 0f, halfW * 2f, ez, y - fall, ez + run, foot, LevelKit.Slope);
        LevelKit.Slab(root, new Vector3(0f, low + 0.5f, ez + run - 1f), new Vector3(halfW * 2f, 1f, 2f), LevelKit.Floor, "ChuteFoot");
        LevelKit.FloorSlab(root, new Vector3(0f, low - 0.5f, (ez + run + z1) * 0.5f), new Vector3(halfW * 2f + 2f * T, 1f, z1 - ez - run + T), "Landing");
        LevelKit.Glass(root, new Vector3(0f, low + 1.4f, ez + run + 1.2f), new Vector3(4f, 2.8f, 0.3f), config);
        for (int s = -1; s <= 1; s += 2) LevelKit.Flute(root, new Vector3(s * halfW, y - fall + 2f, ez + run * 0.5f), Vector3.left * s, 4f);
        LevelKit.LineRun(root, new Vector3(0f, y, z0 + 3f), new Vector3(0f, y, ez - 0.3f), zone);
        Fuel(root, config, c, EnemyKind.Warden, new Vector3(0f, low, ez + run + 6f), "hop exit");
        Fuel(root, config, c, EnemyKind.Turret, new Vector3(0f, y - 1f, ez + run * 0.5f), "over the slide");
        Dressing(root, c, halfW, low, height, z0, z1, false);
        return Open(c, z1, low, halfW, Head + drop);
    }
}
