using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DENSITY, THE POSTER CUT (2026-10-16; DARK DECO 2026-10-07 under it). A
/// Works hall is a few BIG shapes in a dark box, the way the Animated Series
/// paints a plant: BRICK PIERS no closer than seven metres under ONE iron
/// girder band at the roof; most bays BLANK (a wall is one colour field);
/// every third bay ONE feature (a brick arch with a gas lamp hung in it, a lit
/// control window, one enamel sign per hall); a walkable HIGH GALLERY along
/// each wall with its rail and its stair (gameplay: a lip and a ramp);
/// overhead the saw-tooth roof lights, two or three BIG pipe runs on hangers,
/// a cross duct every eighteen metres with one lamp under it, rows of shaded
/// lamps, the gantry's two rails and its trolley beam; on the floor the two
/// mustard lanes and, in a quarter of the bays, ONE solid thing to boost off
/// (a riveted container, a stack of pallets); a bank of three tanks floor to
/// roof; a suspended crossing between the galleries. What went with the cut:
/// the flanges and every rivet row, the stripes and the stencils, the
/// manifolds and valve wheels, the switch cabinets, the conduit fields, the
/// louvres, the bulkheads, the ladders, the cable tray and its cables, the
/// braces, the pipe elbows and valves, the duct grilles, the drain and the
/// puddles, the crossing stripes, the drums, pipe stacks, spools, gratings
/// and carts, the hook block, the red lamps. Everything still is WELDED (two
/// bakers: what casts a shadow and what does not); everything you can stand on
/// or walk into is a kit slab or a bare collider. Nothing is placed where the
/// room already put something: every solid is PROBED against the colliders
/// first (after a physics sync). Deterministic per room. LevelKit.Ornament
/// scales what is left: the bay features, the clutter. Red is danger, amber is
/// a lamp, nothing here is cyan.
/// </summary>
public static partial class LevelKit
{
    static Material dSteel, dPlate, dLamp, dLampCold, dYellow, dConcrete, dDuct, dContainer, dContainerB;
    static System.Random drng = new System.Random(1);
    static bool denseSign;   // one enamel sign per hall

    static float DR(float a, float b) => a + (float)drng.NextDouble() * (b - a);
    static int DI(int n) => drng.Next(n);

    static void EnsureDense()
    {
        if (dSteel != null) return;
        EnsureOffice();
        EnsureWorks();
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        dSteel = Lit(lit, new Color(0.17f, 0.17f, 0.19f), 0.5f); dSteel.SetFloat("_Metallic", 0.7f);   // structural iron
        dPlate = RivetIron;
        dLamp = Unlit(unlit, new Color(2.1f, 1.8f, 1.35f));        // a cream lamp
        dLampCold = Unlit(unlit, new Color(2.2f, 1.5f, 0.7f));     // a gas lamp's amber (nothing cold down here any more)
        dYellow = mustardMat;
        dConcrete = Limestone;
        dDuct = Lit(lit, new Color(0.26f, 0.25f, 0.26f), 0.5f); dDuct.SetFloat("_Metallic", 0.6f);
        dContainer = Lit(lit, new Color(0.46f, 0.17f, 0.14f), 0.35f);    // oxblood paint on riveted iron (lifted to luminance 0.23: you stand on a container)
        dContainerB = Lit(lit, new Color(0.34f, 0.29f, 0.2f), 0.35f);    // and bronze-brown (0.29)
    }

    /// A bare collider: what a welded thing needs to be solid
    static void Bare(Transform parent, Vector3 centre, Vector3 size, Quaternion? rot = null)
    {
        var go = new GameObject("Solid");
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(centre, rot ?? Quaternion.identity);
        go.transform.localScale = size;
        go.AddComponent<BoxCollider>();
    }

    /// Nothing solid in this box already? (the room's own props, its walls, what this pass placed before)
    static bool Clear(Vector3 centre, Vector3 size, Quaternion? rot = null)
        => !Physics.CheckBox(centre, size * 0.5f - Vector3.one * 0.06f, rot ?? Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);   // a hair inside: a floor or a wall it merely touches is not in its way

    static readonly string[] WorksLines =
    {
        "HALCYON WORKS  ·  PRODUCTION IS SAFETY", "SHIFT 3 OF 3  ·  OUTPUT IS UP", "NO ONE IS EXPENDABLE  ·  EVERYONE IS REPLACEABLE",
        "DIG DEEPER", "REPORT ALL SEEPAGE", "THE CORE PROVIDES", "HARD HAT  ·  HARD LINE", "ZERO INCIDENTS THIS QUARTER",
    };

    // ------------------------------------------------------------ the pass

    /// Fill a Works room. `skipW` / `skipE`: a wall the section keeps for itself (a glass wall, a fallen tower)
    /// `furnish` false: walls and roof only (a boss arena keeps its floor and its air to itself)
    public static void Densify(Transform root, float halfW, float y, float height, float z0, float z1, bool openSky, bool skipW, bool skipE, bool furnish = true)
    {
        float len = z1 - z0;
        if (len < 10f || halfW < 3f || height < 4f) return;
        EnsureDense();
        Physics.SyncTransforms();
        drng = new System.Random(Mathf.RoundToInt(z0 * 7.3f + halfW * 13f + height));
        denseSign = false;
        var cast = new MeshBaker();   // what throws a shadow: structure
        var fine = new MeshBaker();   // what does not: pipes, rails, lamps
        float top = y + height, mid = z0 + len * 0.5f;
        bool big = halfW >= 10f && height >= 9f && len >= 40f;
        float galleryY = big && furnish ? Mathf.Min(top - 2.8f, y + height * 0.72f) : 0f;

        for (int side = -1; side <= 1; side += 2)
        {
            if (side < 0 ? skipW : skipE) continue;
            DenseWall(root, cast, fine, side, halfW, y, height, z0, z1, galleryY);
        }
        if (!openSky && height >= 6f) Ceiling(root, cast, fine, halfW, y, height, z0, z1);
        DenseFloor(root, cast, fine, halfW, y, z0, z1, skipW || !furnish, skipE || !furnish);
        if (big && !openSky && furnish)
        {
            TankBank(root, cast, fine, halfW, y, height, z0, z1, skipE ? -1 : 1);
            Crossing(root, cast, fine, halfW, y, galleryY, mid, top);
        }
        cast.Flush(root, "DenseMass", true);
        fine.Flush(root, "DenseFine");
        Physics.SyncTransforms();
    }

    // ------------------------------------------------------------ the walls

    static void DenseWall(Transform root, MeshBaker cast, MeshBaker fine, int side, float halfW, float y, float height, float z0, float z1, float galleryY)
    {
        float x = side * halfW, top = y + height, len = z1 - z0;
        Vector3 inward = Vector3.right * -side;
        int bays = Mathf.Max(1, Mathf.FloorToInt(len / 7f));   // piers never closer than seven metres
        float pitch = len / bays;
        // ONE girder band: riveted plate under the roof (at reach height in a low hall)
        float by = height >= 8f ? top - 0.8f : y + Mathf.Min(4.6f, height - 0.8f);
        cast.Add(dPlate, new Vector3(x + inward.x * 0.14f, by, z0 + len * 0.5f), new Vector3(0.28f, 0.6f, len - 0.4f));
        for (int i = 0; i <= bays; i++)
        {
            // The pier: brick on a stone foot under a stone cap. Shallow, like everything on these walls: a wall-run
            // goes over them, never into them
            float z = z0 + i * pitch;
            if (i == 0) z += 0.7f; else if (i == bays) z -= 0.7f;
            cast.Add(Brick, new Vector3(x + inward.x * 0.15f, y + height * 0.5f, z), new Vector3(0.3f, height - 0.4f, 1.2f));
            cast.Add(Limestone, new Vector3(x + inward.x * 0.19f, y + 0.2f, z), new Vector3(0.38f, 0.4f, 1.4f));
            cast.Add(Limestone, new Vector3(x + inward.x * 0.2f, top - 0.2f, z), new Vector3(0.4f, 0.16f, 1.52f));
        }
        // The bays: blank but for every third (every other when the knob is high), which carries ONE feature
        if (Ornate(0.1f))
        {
            int every = Ornate(0.6f) ? 2 : 3;
            for (int i = 0; i < bays; i++)
            {
                if (i % every != 1) continue;
                float bz = z0 + (i + 0.5f) * pitch;
                float w = pitch - 1.6f;
                int kind = !denseSign && i >= bays / 2 ? 2 : DI(2);
                if (kind == 2) denseSign = true;
                Bay(root, cast, fine, kind, x, inward, y, height, bz, w);
            }
        }
        if (galleryY > 0f) Gallery(root, cast, fine, side, halfW, y, height, z0, z1, galleryY);
    }

    /// One bay's feature, between two piers
    static void Bay(Transform root, MeshBaker cast, MeshBaker fine, int kind, float x, Vector3 inward, float y, float height, float bz, float w)
    {
        float ix = inward.x;
        Vector3 At(float proud, float up, float along) => new Vector3(x + ix * proud, y + up, bz + along);
        switch (kind)
        {
            case 0:
            {
                // A brick arch: a recess of soot-dark brick with a gas lamp hung in it, five fat voussoirs and a keystone
                float ah = Mathf.Min(height - 2f, 5f), aw = Mathf.Min(w - 0.4f, 4.5f);
                cast.Add(Brick, At(0.1f, 0.5f + ah * 0.5f, -aw * 0.5f + 0.3f), new Vector3(0.2f, ah, 0.6f));
                cast.Add(Brick, At(0.1f, 0.5f + ah * 0.5f, aw * 0.5f - 0.3f), new Vector3(0.2f, ah, 0.6f));
                fine.Add(sootMat, At(0.03f, 0.5f + ah * 0.5f, 0f), new Vector3(0.04f, ah, aw - 1.2f));
                const int segs = 5;
                float rise = Mathf.Min(aw * 0.35f, 1.4f), half = aw * 0.5f - 0.3f;
                for (int k = 0; k < segs; k++)
                {
                    float a0 = Mathf.PI * (1f - k / (float)segs), a1 = Mathf.PI * (1f - (k + 1) / (float)segs);
                    Vector3 p0 = At(0.12f, 0.5f + ah + Mathf.Sin(a0) * rise, Mathf.Cos(a0) * half), p1 = At(0.12f, 0.5f + ah + Mathf.Sin(a1) * rise, Mathf.Cos(a1) * half);
                    Vector3 d = p1 - p0;
                    Quaternion rot = Quaternion.FromToRotation(Vector3.forward, d.normalized);
                    cast.Add(Brick, (p0 + p1) * 0.5f, new Vector3(0.24f, 0.6f, d.magnitude + 0.08f), rot);
                }
                cast.Add(Limestone, At(0.16f, 0.5f + ah + rise, 0f), new Vector3(0.32f, 0.7f, 0.5f));
                GasLamp(root, At(0.14f, 0.5f + ah - 0.6f, 0f), inward, 1.1f);
                break;
            }
            case 2:
            {
                // An enamel sign on an iron board, one of the plant's lines
                cast.Add(dSteel, At(0.1f, 3.1f, 0f), new Vector3(0.12f, 2f, Mathf.Min(w - 0.6f, 6f)));
                HoloSign(root, At(0.22f, 3.1f, 0f), inward, Mathf.Min(w - 1f, 5.4f), 1.1f, WorksLines[DI(WorksLines.Length)]);
                break;
            }
            default:
            {
                // A lit control window: someone is in there, in amber light, behind a brass cross of mullions
                float wh = Mathf.Min(height - 3f, 2f), ww = Mathf.Min(w - 1f, 4.4f);
                cast.Add(dSteel, At(0.1f, 3.2f, 0f), new Vector3(0.12f, wh + 0.4f, ww + 0.4f));
                fine.Add(WindowAmber, At(0.17f, 3.2f, 0f), new Vector3(0.02f, wh, ww));
                fine.Add(Brass, At(0.2f, 3.2f, 0f), new Vector3(0.04f, wh, 0.08f));
                fine.Add(Brass, At(0.2f, 3.2f, 0f), new Vector3(0.04f, 0.08f, ww));
                break;
            }
        }
    }

    /// The high gallery: a walkway near the roof along the wall, in segments that are clear, with its rail, a gas lamp
    /// every ten metres, and a stair up from the floor at one end where there is room
    static void Gallery(Transform root, MeshBaker cast, MeshBaker fine, int side, float halfW, float y, float height, float z0, float z1, float gy)
    {
        float x = side * halfW, len = z1 - z0;
        float ix = -side;
        float gx = x + ix * 1.15f;
        int segs = Mathf.Max(1, Mathf.RoundToInt((len - 8f) / 10f));
        float segLen = (len - 8f) / segs;
        bool any = false;
        for (int i = 0; i < segs; i++)
        {
            float cz = z0 + 4f + (i + 0.5f) * segLen;
            if (!Clear(new Vector3(gx, gy + 1.2f, cz), new Vector3(2.3f, 2.4f, segLen - 0.2f))) continue;
            any = true;
            FloorSlab(root, new Vector3(gx, gy - 0.25f, cz), new Vector3(2.3f, 0.5f, segLen), "HighGallery");
            // The rail on the open edge
            float rx = x + ix * 2.25f;
            fine.Add(Iron, new Vector3(rx, gy + 0.55f, cz), new Vector3(0.06f, 0.06f, segLen));
            fine.Add(Brass, new Vector3(rx, gy + 1.05f, cz), new Vector3(0.07f, 0.07f, segLen));   // a brass top rail
            for (float pz = cz - segLen * 0.5f + 1.5f; pz < cz + segLen * 0.5f; pz += 3f)
                fine.Add(Iron, new Vector3(rx, gy + 0.55f, pz), new Vector3(0.08f, 1.1f, 0.08f));
            // A gas lamp on the wall over it
            for (float lz = cz - segLen * 0.5f + 5f; lz < cz + segLen * 0.5f; lz += 10f)
                GasLamp(root, new Vector3(x, gy + 2.5f, lz), Vector3.right * ix, 1f);
        }
        if (!any) return;
        // The stair: a ramp along the wall from the floor to the gallery at the near end, where the floor is clear
        float rise = gy - y, run = Mathf.Max(rise * 2.2f, 12f);
        if (len - 8f > run + 6f && Clear(new Vector3(gx, y + rise * 0.5f, z0 + 4f + run * 0.5f), new Vector3(2.3f, rise, run)))
        {
            Ramp(root, gx, 2.2f, z0 + 4f, y, z0 + 4f + run, gy - 0.02f, Slope);
            for (float pz = z0 + 5.5f; pz < z0 + 4f + run; pz += 3f)
            {
                float py = y + rise * ((pz - z0 - 4f) / run);
                fine.Add(Iron, new Vector3(x + ix * 2.25f, py + 0.55f, pz), new Vector3(0.08f, 1.1f, 0.08f));
            }
        }
    }

    // ------------------------------------------------------------ the roof

    static void Ceiling(Transform root, MeshBaker cast, MeshBaker fine, float halfW, float y, float height, float z0, float z1)
    {
        float top = y + height, len = z1 - z0, mid = z0 + len * 0.5f;
        bool sawtooth = height >= 9f && halfW >= 8f;
        float field = sawtooth ? top - 1.2f : top;   // the pipe runs hang under the roof lights when there are any
        // The saw-tooth roof lights: a sloped iron plate and an amber pane every six metres, the plant's north light
        if (sawtooth)
            for (float tz = z0 + 3f; tz + 6f <= z1 - 2f; tz += 6f)
            {
                cast.Add(dDuct, new Vector3(0f, top - 0.6f, tz + 3.6f), new Vector3(halfW * 2f - 1.6f, 0.1f, 5f), Quaternion.Euler(-13f, 0f, 0f));
                fine.Add(WindowAmber, new Vector3(0f, top - 0.6f, tz + 1.05f), new Vector3(halfW * 2f - 2f, 1.05f, 0.04f), Quaternion.Euler(14f, 0f, 0f));
                var l = Light(root, new Vector3(0f, top - 1.6f, tz + 1.5f), halfW * 1.8f, 1.3f);
                l.color = new Color(1f, 0.72f, 0.4f);
            }
        // The pipe runs: two or three BIG ones along the hall on hangers
        int pipes = Mathf.Clamp(Mathf.RoundToInt(halfW / 6f), 2, 3);
        for (int i = 0; i < pipes; i++)
        {
            float px = Mathf.Lerp(-halfW * 0.7f, halfW * 0.7f, (i + 0.5f) / pipes) + DR(-0.6f, 0.6f);
            float dia = DR(0.6f, 0.9f);
            float py = field - 1f - (i % 2) * 0.7f - dia * 0.5f;
            fine.Add(Iron, new Vector3(px, py, mid), new Vector3(dia, len * 0.5f - 1f, dia), Quaternion.Euler(90f, 0f, 0f), PrimitiveType.Cylinder);
            for (float hz = z0 + 6f; hz < z1 - 2f; hz += 12f)
                fine.Add(dSteel, new Vector3(px, (py + top) * 0.5f, hz), new Vector3(0.1f, top - py, 0.1f));
        }
        // Cross ducts, one lamp under each in a tall hall
        for (float dz = z0 + 9f; dz < z1 - 6f; dz += 18f)
        {
            cast.Add(dDuct, new Vector3(0f, field - 2.3f, dz), new Vector3(halfW * 2f - 1.8f, 1f, 1.4f));
            if (height >= 9f)
            {
                fine.Add(dDuct, new Vector3(0f, field - 3.4f, dz), new Vector3(0.6f, 1.2f, 0.6f));
                fine.Add(dLampCold, new Vector3(0f, field - 4.02f, dz), new Vector3(0.5f, 0.02f, 0.5f));
            }
        }
        // Rows of lamps in iron shades, hung under the field
        for (float lz = z0 + 6f; lz < z1 - 4f; lz += 12f)
            for (int s = -1; s <= 1; s += 2)
            {
                float lx = s * halfW * 0.45f;
                fine.Add(dSteel, new Vector3(lx, field - 2.6f, lz), new Vector3(2.6f, 0.14f, 0.34f));
                fine.Add(dLamp, new Vector3(lx, field - 2.68f, lz), new Vector3(2.4f, 0.04f, 0.26f));
                fine.Add(dSteel, new Vector3(lx, field - 1.3f, lz), new Vector3(0.06f, 2.6f, 0.06f));
            }
        // The gantry: two rails the length of the hall in mustard, a trolley beam across them
        if (halfW >= 8f && len >= 30f)
        {
            float ry = field - 1.5f, rx = halfW * 0.58f;
            cast.Add(dYellow, new Vector3(-rx, ry, mid), new Vector3(0.3f, 0.45f, len - 6f));
            cast.Add(dYellow, new Vector3(rx, ry, mid), new Vector3(0.3f, 0.45f, len - 6f));
            float tz = z0 + len * DR(0.3f, 0.7f);
            cast.Add(dYellow, new Vector3(0f, ry - 0.5f, tz), new Vector3(rx * 2f + 0.6f, 0.5f, 0.7f));
        }
    }

    // ------------------------------------------------------------ the floor

    static void DenseFloor(Transform root, MeshBaker cast, MeshBaker fine, float halfW, float y, float z0, float z1, bool skipW, bool skipE)
    {
        float len = z1 - z0, mid = z0 + len * 0.5f;
        // The lanes: two mustard lines the length of the hall
        for (int s = -1; s <= 1; s += 2)
            fine.Add(dYellow, new Vector3(s * halfW * 0.34f, y + 0.02f, mid), new Vector3(0.14f, 0.02f, len - 6f));
        // Clutter in the bays, against the walls, where the floor is clear: a quarter of them at the default
        int bays = Mathf.Max(1, Mathf.FloorToInt(len / 7f));
        float pitch = len / bays;
        float chance = 0.28f * OrnamentScale;
        for (int side = -1; side <= 1; side += 2)
        {
            if (side < 0 ? skipW : skipE) continue;
            for (int i = 0; i < bays; i++)
            {
                if (DR(0f, 1f) > chance) continue;
                float bz = z0 + (i + 0.5f) * pitch;
                float bx = side * (halfW - 2.4f);
                Clutter(root, cast, fine, DI(2), new Vector3(bx, y, bz), side, pitch - 1.6f);
            }
        }
    }

    static void Clutter(Transform root, MeshBaker cast, MeshBaker fine, int kind, Vector3 at, int side, float room)
    {
        float yaw = DR(-8f, 8f);
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        switch (kind)
        {
            case 0:
            {
                // A container: one flat painted box, solid (a vault, a wall to kick)
                float L = Mathf.Min(6f, room), H = 2.6f, W = 2.4f;
                if (!Clear(at + Vector3.up * (H * 0.5f), new Vector3(W, H, L), rot)) return;
                cast.Add(DI(2) == 0 ? dContainer : dContainerB, at + Vector3.up * (H * 0.5f), new Vector3(W, H, L), rot);
                Bare(root, at + Vector3.up * (H * 0.5f), new Vector3(W, H, L), rot);
                break;
            }
            default:
            {
                // Pallets: two stacks of crates (lips)
                for (int k = 0; k < 2; k++)
                {
                    Vector3 p = at + rot * new Vector3(0f, 0f, (k - 0.5f) * 1.6f);
                    int layers = 2 + DI(2);
                    if (!Clear(p + Vector3.up * (layers * 0.6f), new Vector3(1.3f, layers * 1.2f, 1.3f), rot)) continue;
                    fine.Add(woodMat, p + Vector3.up * 0.07f, new Vector3(1.3f, 0.14f, 1.3f), rot);
                    for (int l = 0; l < layers; l++) cast.Add(l % 2 == 0 ? WoodPanel : dContainer, p + Vector3.up * (0.14f + 0.6f + l * 1.2f), new Vector3(1.2f, 1.2f, 1.2f), rot * Quaternion.Euler(0f, l * 7f, 0f));
                    Bare(root, p + Vector3.up * (0.14f + layers * 0.6f), new Vector3(1.25f, layers * 1.2f + 0.14f, 1.25f), rot);
                }
                break;
            }
        }
    }

    // ------------------------------------------------------------ the big things

    /// Three riveted tanks floor to roof in one bay, one brass band each, a header across their tops
    static void TankBank(Transform root, MeshBaker cast, MeshBaker fine, float halfW, float y, float height, float z0, float z1, int side)
    {
        float len = z1 - z0;
        float bz = z0 + len * DR(0.6f, 0.8f);
        float tx = side * (halfW - 1.8f), dia = 2.4f, th = height - 1.4f;
        if (!Clear(new Vector3(tx, y + th * 0.5f, bz), new Vector3(3.2f, th, 9f))) return;
        for (int k = -1; k <= 1; k++)
        {
            Vector3 c = new Vector3(tx, y + th * 0.5f, bz + k * 3f);
            cast.Add(dPlate, c, new Vector3(dia, th * 0.5f, dia), null, PrimitiveType.Cylinder);
            fine.Add(Brass, new Vector3(tx, y + th * 0.5f, bz + k * 3f), new Vector3(dia + 0.1f, 0.08f, dia + 0.1f), null, PrimitiveType.Cylinder);
            var go = new GameObject("Tank");
            go.transform.SetParent(root, false);
            go.transform.position = c;
            go.transform.localScale = new Vector3(dia, th * 0.5f, dia);
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = PrimMesh(PrimitiveType.Cylinder);
            mc.convex = true;
        }
        fine.Add(Iron, new Vector3(tx, y + th + 0.3f, bz), new Vector3(0.36f, 0.36f, 7.4f));   // the header
        for (int k = -1; k <= 1; k++)
            fine.Add(Iron, new Vector3(tx, y + th + 0.1f, bz + k * 3f), new Vector3(0.3f, 0.5f, 0.3f), null, PrimitiveType.Cylinder);
    }

    /// A catwalk across the hall between the galleries, hung on cables, rails both sides; only where the air is clear
    static void Crossing(Transform root, MeshBaker cast, MeshBaker fine, float halfW, float y, float gy, float mid, float top)
    {
        float cz = mid + DR(-8f, 8f);
        float span = halfW * 2f - 4.6f;
        if (!Clear(new Vector3(0f, gy + 1.2f, cz), new Vector3(span, 2.4f, 2.2f))) return;
        FloorSlab(root, new Vector3(0f, gy - 0.25f, cz), new Vector3(span, 0.5f, 2f), "Crossing");
        for (int s = -1; s <= 1; s += 2)
        {
            fine.Add(Iron, new Vector3(0f, gy + 0.55f, cz + s * 0.97f), new Vector3(span, 0.06f, 0.06f));
            fine.Add(Brass, new Vector3(0f, gy + 1.05f, cz + s * 0.97f), new Vector3(span, 0.07f, 0.07f));
            for (float px = -span * 0.5f + 1.5f; px < span * 0.5f; px += 3f) fine.Add(Iron, new Vector3(px, gy + 0.55f, cz + s * 0.97f), new Vector3(0.08f, 1.1f, 0.08f));
        }
        for (int k = -1; k <= 1; k += 2)
            for (int s = -1; s <= 1; s += 2)
            {
                float hx = k * span * 0.3f;
                fine.Add(cableMat, new Vector3(hx, (gy + top) * 0.5f, cz + s * 0.9f), new Vector3(0.05f, top - gy, 0.05f));
            }
        cast.Add(dSteel, new Vector3(0f, gy - 0.6f, cz), new Vector3(span, 0.3f, 0.3f));
    }

    // ------------------------------------------------------------ the yard

    /// The plant over the yard's walls (Level 6's surface): tanks, a smokestack, a pipe bridge over the whole yard,
    /// gantry masts with arc lamps, catwalks along the wall tops, a brick cooling tower — the skyline the way down
    /// begins under, as a few big silhouettes. Looks only.
    public static void DensifyYard(Transform root, float halfW, float y, float height, float z0, float z1)
    {
        EnsureDense();
        drng = new System.Random(66);
        var cast = new MeshBaker();
        var fine = new MeshBaker();
        float len = z1 - z0, mid = z0 + len * 0.5f, top = y + height;
        for (int side = -1; side <= 1; side += 2)
        {
            // Tanks and stacks in a row beyond the wall, one band each
            for (int i = 0; i < 4; i++)
            {
                float tz = z0 + len * (0.12f + 0.24f * i), tx = side * (halfW + 9f + DR(0f, 6f));
                float dia = DR(5f, 8f), th = DR(14f, 24f);
                cast.Add(dPlate, new Vector3(tx, y + th * 0.5f, tz), new Vector3(dia, th * 0.5f, dia), null, PrimitiveType.Cylinder);
                fine.Add(railMat, new Vector3(tx, y + th * 0.5f, tz), new Vector3(dia + 0.1f, 0.1f, dia + 0.1f), null, PrimitiveType.Cylinder);
                fine.Add(dSteel, new Vector3(tx, y + th + 0.3f, tz), new Vector3(dia * 0.5f, 0.6f, dia * 0.5f), null, PrimitiveType.Cylinder);
            }
            Smokestack(root, new Vector3(side * (halfW + 22f), y, z0 + len * 0.5f), 34f, 3.4f);
            // Catwalk along the wall top, a rail on it, a gas lamp under it now and then
            fine.Add(dSteel, new Vector3(side * (halfW + 1.2f), top + 0.2f, mid), new Vector3(2.2f, 0.3f, len));
            fine.Add(Iron, new Vector3(side * (halfW + 2.25f), top + 0.9f, mid), new Vector3(0.06f, 0.06f, len));
            for (float pz = z0 + 1f; pz < z1; pz += 4f) fine.Add(Iron, new Vector3(side * (halfW + 2.25f), top + 0.55f, pz), new Vector3(0.08f, 1.1f, 0.08f));
            for (float lz = z0 + 8f; lz < z1; lz += 16f) fine.Add(dLampCold, new Vector3(side * (halfW + 0.6f), top - 0.4f, lz), new Vector3(0.6f, 0.08f, 0.3f));
            // Gantry masts, with arc lamps
            for (int i = 0; i < 2; i++)
            {
                float mz = z0 + len * (0.3f + 0.4f * i), mx = side * (halfW + 4f);
                cast.Add(dSteel, new Vector3(mx, y + (height + 6f) * 0.5f, mz), new Vector3(0.8f, height + 6f, 0.8f));
                fine.Add(dSteel, new Vector3(mx - side * 1.5f, top + 5.6f, mz), new Vector3(3.2f, 0.4f, 0.4f));
                Floodlight(root, new Vector3(mx - side * 3f, top + 5.3f, mz), new Vector3(-side * 0.5f, -0.8f, 0f), 40f, 2.2f);
            }
        }
        // The pipe bridge over the yard: a truss across, three pipes on it, and a conveyor housing beside it
        float bz = z0 + len * 0.62f, by = top + 4f;
        cast.Add(dSteel, new Vector3(0f, by, bz), new Vector3(halfW * 2f + 16f, 0.5f, 2.6f));
        for (int k = -1; k <= 1; k++) fine.Add(Iron, new Vector3(0f, by + 0.7f, bz + k * 0.8f), new Vector3(0.5f, halfW + 8f, 0.5f), Quaternion.Euler(0f, 0f, 90f), PrimitiveType.Cylinder);
        cast.Add(dDuct, new Vector3(0f, by + 3.2f, bz - 6f), new Vector3(halfW * 2f + 16f, 2.2f, 2.4f));
        for (int k = 0; k < 3; k++) fine.Add(dLampCold, new Vector3(-halfW - 6f + k * (halfW * 2f + 12f) / 2f, by + 2.05f, bz - 6f), new Vector3(0.9f, 0.04f, 0.6f));
        // The cooling tower, beyond the far end: brick, a stone rim
        cast.Add(Brick, new Vector3(halfW * 0.6f, y + 12f, z1 + 26f), new Vector3(16f, 12f, 16f), null, PrimitiveType.Cylinder);
        cast.Add(Brick, new Vector3(halfW * 0.6f, y + 30f, z1 + 26f), new Vector3(11f, 6f, 11f), null, PrimitiveType.Cylinder);
        fine.Add(Limestone, new Vector3(halfW * 0.6f, y + 24.2f, z1 + 26f), new Vector3(16.6f, 0.5f, 16.6f), null, PrimitiveType.Cylinder);
        fine.Add(Limestone, new Vector3(halfW * 0.6f, y + 36.2f, z1 + 26f), new Vector3(11.6f, 0.5f, 11.6f), null, PrimitiveType.Cylinder);
        cast.Flush(root, "YardMass", true);
        fine.Flush(root, "YardFine");
    }
}
