using UnityEngine;

/// <summary>
/// THE POSTER SILHOUETTE (2026-10-16). The rig builders (Enemy.BuildRig,
/// EnemyDamageControl.BuildSentryRig, EnemyVarietyRigs.BuildWardenRig and
/// BuildMarshalRig) lay down the LAW — the pivots, the hitbox capsule, the
/// weakspot collider, the muzzle, the lamp, the plates — and build the first
/// cut's look round it. This file runs AFTER them (from Enemy.Livery, before
/// the component's Awake caches anything) and RESHAPES that look into the 1930s
/// poster figure the references ask for (Docs/Reference, the couple at the
/// rail): elongated, a torso of two stacked blocks, squared shoulders, slim
/// box limbs, wedge gloves, a small dark oval of a head. It finds every piece
/// by the name its builder gave it under the pivot it rides, and gives it a new
/// mesh, size, place and colour IN PLACE: the piece keeps its name, its pivot,
/// its spot in rig.plates (so the red beat, the daze and the gold still flash
/// exactly what they flashed) and its ragdoll bone (Enemy.BoneFor). Greebles
/// the poster has no use for are dropped before Awake; a few flat colour
/// fields are added (a chest panel, a shoulder board, frogging, a cap). Never a
/// collider, never a pivot, never a hitbox, never the weakspot renderer (its
/// transform IS its collider), never a muzzle. Counts per kind are in the
/// method comments (renderers, the text label aside).
/// </summary>
public partial class Enemy
{
    // ---------------------------------------------------------------- reshaping what the builders made

    static readonly Mesh[] primitiveMeshes = new Mesh[4];

    /// The built-in mesh of a primitive, cached once: what a reshaped part is given. The ragdoll copier and the mark read
    /// the mesh's name ("Cube", "Sphere", "Cylinder", "Capsule"), so a reshaped part is copied as what it now is.
    static Mesh PrimitiveMesh(PrimitiveType t)
    {
        int i = t == PrimitiveType.Sphere ? 0 : t == PrimitiveType.Cylinder ? 1 : t == PrimitiveType.Capsule ? 2 : 3;
        if (primitiveMeshes[i] == null)
        {
            var go = GameObject.CreatePrimitive(i == 3 ? PrimitiveType.Cube : t);
            primitiveMeshes[i] = go.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(go);
        }
        return primitiveMeshes[i];
    }

    /// A direct child part of a pivot by the name its builder gave it, or null (a rig that lacks it is simply not reshaped there)
    static Renderer Piece(Transform pivot, string name)
    {
        if (pivot == null) return null;
        var t = pivot.Find(name);
        return t != null ? t.GetComponent<Renderer>() : null;
    }

    /// RESHAPE a built part in place: a new mesh, place, size, turn and (if given) material. Its name, its pivot and its
    /// place in rig.plates stay. Null-safe.
    static Renderer Reshape(Renderer r, PrimitiveType type, Vector3 at, Vector3 size, Material mat = null, float rx = 0f, float ry = 0f, float rz = 0f)
    {
        if (r == null) return null;
        var mf = r.GetComponent<MeshFilter>();
        if (mf != null) mf.sharedMesh = PrimitiveMesh(type);
        r.transform.localPosition = at;
        r.transform.localScale = size;
        r.transform.localRotation = Quaternion.Euler(rx, ry, rz);
        if (mat != null) r.sharedMaterial = mat;
        return r;
    }

    /// Reshape a part into a STRUT: a box of `thickness` laid from `a` to `b` (both local to its pivot) — a sleeve
    /// from a shoulder to a hand
    static Renderer ReshapeStrut(Renderer r, Vector3 a, Vector3 b, float thickness, Material mat = null)
    {
        if (r == null) return null;
        Vector3 d = b - a;
        Reshape(r, PrimitiveType.Cube, (a + b) * 0.5f, new Vector3(thickness, thickness, d.magnitude), mat);
        r.transform.localRotation = Quaternion.LookRotation(d);
        return r;
    }

    /// A built part the poster cut has no use for (a greeble, a halo, a prop): gone now, before Awake caches the
    /// renderers. Every child of that name goes (both lapels, every button). NEVER a plate: rig.plates would hold a null.
    static void Drop(Transform pivot, string name)
    {
        if (pivot == null) return;
        for (int i = pivot.childCount - 1; i >= 0; i--)
        {
            var c = pivot.GetChild(i);
            if (c.name == name) Object.DestroyImmediate(c.gameObject);
        }
    }

    /// A shell panel added after the build joins the plates (what flashes), so the body flashes whole
    static void AddPlate(Rig rig, Renderer r)
    {
        if (r == null) return;
        var p = new Renderer[rig.plates.Length + 1];
        rig.plates.CopyTo(p, 0);
        p[rig.plates.Length] = r;
        rig.plates = p;
    }

    /// The name plate and its engraved line, moved to a new chest face (`at` = the plate's centre; the text sits 7 mm proud)
    static void PlaceNamePlate(Transform torso, Vector3 at)
    {
        var plate = torso.Find("NamePlate");
        if (plate != null) plate.localPosition = at;
        var label = torso.Find("ChestLabel");
        if (label != null) label.localPosition = at + new Vector3(0f, 0f, 0.007f);
    }

    /// The unit's own lamp as ONE FLAT DISC of emission facing +z, its back buried in the face it sits on: `at` is the
    /// disc's centre, 2 mm inside that face. (The material is the roles' — amber braced or aiming, red for the last beat.)
    static void LampDisc(Renderer lamp, Vector3 at, float d)
        => Reshape(lamp, PrimitiveType.Cylinder, at, new Vector3(d, 0.005f, d), null, 90f);

    /// Brass FROGGING drawn as bars across a chest face: `first` is the top bar's centre (its back 5 mm inside the face)
    static void Frogging(Transform torso, Vector3 first, float width, int count, float step)
    {
        for (int i = 0; i < count; i++)
            Part(torso, PrimitiveType.Cube, first + Vector3.down * (step * i), new Vector3(width, 0.025f, 0.012f), Brass(), "Frogging");
    }

    /// A flat colour field painted on a chest face: a thin panel whose back is buried 4 mm in and whose front is 8 mm proud
    static Renderer ChestPanel(Transform torso, Vector3 centre, float width, float height, Material mat, string name = "ChestPanel")
        => Part(torso, PrimitiveType.Cube, centre, new Vector3(width, height, 0.012f), mat, name);

    /// The head as a figure's: a small near-black glass oval, no clockwork in it (for the units whose cell is elsewhere)
    static Renderer HeadOval(Transform parent, Vector3 at, float w, float h)
    {
        var r = Part(parent, PrimitiveType.Sphere, at, new Vector3(w, h, w), GlassDark(), "HeadOval");
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return r;
    }

    /// A bowler: the lacquer dome over the crown of a head and the brim round it. `at` = the head oval's centre. Two renderers.
    static void Bowler(Transform parent, Vector3 at, float headW)
    {
        Part(parent, PrimitiveType.Sphere, at + new Vector3(0f, headW * 0.47f, 0f), new Vector3(headW, headW * 0.8f, headW), LacquerDome(), "BowlerDome");
        Part(parent, PrimitiveType.Cylinder, at + new Vector3(0f, headW * 0.3f, 0f), new Vector3(headW * 1.5f, 0.008f, headW * 1.5f), LacquerDome(), "BowlerBrim");
    }

    // ---------------------------------------------------------------- THE CIVIC MANNEQUIN: footman, page, bellhop, house officer

    /// What BuildRig (and BuildMarshalRig, the same skeleton with a gun) built, cut to the poster figure:
    ///   the TORSO = two stacked blocks — the chest (the plate, `chestW` across the squared shoulders, its top at the
    ///   shoulder line) and the waist (the hip plate, narrower, on the root) — a setback, never a barrel;
    ///   the LIMBS slimmed to long straight boxes on their brass balls, lacquer wedge shoes;
    ///   the LAMP one flat disc on the chest, the name plate on the chest face, the key's mount brought in to the back;
    ///   the painted coat of the first cut (EnemySuit: lapels, waistcoat, buttons, tie) dropped — the liveries paint flat
    ///   fields instead (EnemyLivery). `board` lays a shoulder yoke across the chest top (wide, squared: the footman and
    ///   the officer; the page goes without, narrow). The head (EnemyMannequin.GlassHead) is already the oval.
    static void PosterBody(Rig rig, float chestW, float chestH, bool board)
    {
        Transform root = rig.torso.parent;
        const float front = 0.11f;   // the chest block's front face
        float chestTop = 0.32f;      // the shoulder line (the arm pivots' height on the torso)
        Reshape(Piece(root, "Pelvis"), PrimitiveType.Cube, new Vector3(0f, 1.01f, 0f), new Vector3(chestW * 0.68f, 0.26f, 0.2f));
        var chest = Reshape(Piece(rig.torso, "Chest"), PrimitiveType.Cube, new Vector3(0f, chestTop - chestH * 0.5f, 0f), new Vector3(chestW, chestH, front * 2f));
        if (board && chest != null)
            AddPlate(rig, Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, chestTop + 0.01f, 0f), new Vector3(chestW + 0.16f, 0.05f, 0.2f), chest.sharedMaterial, "ShoulderBoard"));
        LampDisc(rig.status, new Vector3(0f, chestTop - 0.06f, front + 0.002f), 0.1f);
        PlaceNamePlate(rig.torso, new Vector3(0f, -0.1f, front + 0.008f));
        var mount = rig.torso.Find("KeyMount");
        if (mount != null) mount.localPosition = new Vector3(0f, 0.2f, -front + 0.005f);
        Drop(rig.torso, "BowTie");
        Drop(rig.torso, "Lapel");
        Drop(rig.torso, "Waistcoat");
        Drop(rig.torso, "Button");
        foreach (var leg in new[] { rig.legL, rig.legR })
        {
            Reshape(Piece(leg, "Thigh"), PrimitiveType.Cube, new Vector3(0f, -0.22f, 0f), new Vector3(0.13f, 0.36f, 0.14f));
            Reshape(Piece(leg, "Shin"), PrimitiveType.Cube, new Vector3(0f, -0.66f, 0f), new Vector3(0.11f, 0.36f, 0.12f));
            Reshape(Piece(leg, "Shoe"), PrimitiveType.Cube, new Vector3(0f, -0.9f, 0.06f), new Vector3(0.11f, 0.045f, 0.27f), Lacquer());
        }
        foreach (var arm in new[] { rig.armL, rig.armR })
        {
            Reshape(Piece(arm, "UpperArm"), PrimitiveType.Cube, new Vector3(0f, -0.18f, 0f), new Vector3(0.11f, 0.3f, 0.11f));
            Reshape(Piece(arm, "Forearm"), PrimitiveType.Cube, new Vector3(0f, -0.54f, 0f), new Vector3(0.095f, 0.3f, 0.1f));
        }
    }

    /// THE BELLHOP (Chaser; the FOOTMAN behaviour): oxblood shell, a cream chest panel with the name plate on it, the
    /// shoulder board, a pillbox; the luggage hook of the first cut is gone (its hands are free: a footman braces and
    /// shoves, it swings nothing). Was 39 (36 + pillbox and hook) → 33.
    static void BellhopBody(Rig rig)
    {
        PosterBody(rig, 0.44f, 0.4f, true);
        Drop(rig.armR, "Hook");
        ChestPanel(rig.torso, new Vector3(0f, 0.06f, 0.108f), 0.2f, 0.28f, Cream());
        PlaceNamePlate(rig.torso, new Vector3(0f, -0.05f, 0.12f));
        Pillbox(rig.headPivot, new Vector3(0f, CivicHatY(EnemyKind.Chaser), 0f), 0.13f);
    }

    /// THE PAGE (Turret; the sniper's lamp): a smaller footman — the narrow chest, no board — in the bell staff's oxblood
    /// with the cream panel and the pillbox, its rotor-hat turning on the pillbox (rig.handL: AnimateSentinel spins it),
    /// and down its right arm THE LAMP it aims: the cannon as a black drum with a brass rim, the laser out of its mouth
    /// (rig.muzzle, unchanged). The thruster halo under it is gone: the rotor is its lift, and a halo is a sprite the
    /// poster never draws. Was 40 (38 + 2) → 35.
    static void PageBody(Rig rig)
    {
        PosterBody(rig, 0.4f, 0.36f, false);
        Drop(rig.torso.parent, "ThrusterHalo");
        ChestPanel(rig.torso, new Vector3(0f, 0.08f, 0.108f), 0.16f, 0.24f, Cream());
        PlaceNamePlate(rig.torso, new Vector3(0f, -0.02f, 0.12f));
        Reshape(Piece(rig.armR, "Barrel"), PrimitiveType.Cylinder, new Vector3(0f, -0.66f, 0f), new Vector3(0.15f, 0.2f, 0.15f), LacquerDome());
        Reshape(Piece(rig.armR, "MuzzleRing"), PrimitiveType.Cylinder, new Vector3(0f, -0.87f, 0f), new Vector3(0.17f, 0.012f, 0.17f), Brass());
        float y = CivicHatY(EnemyKind.Turret);
        Pillbox(rig.headPivot, new Vector3(0f, y, 0f), 0.13f);
        rig.handL = Pivot(rig.headPivot, "Rotor", new Vector3(0f, y + 0.115f, 0f));
        Part(rig.handL, PrimitiveType.Cube, Vector3.zero, new Vector3(0.36f, 0.008f, 0.035f), Brass(), "RotorBlade");
    }

    /// THE DUMMY (Dummy; its plate reads FOOTMAN): cream canvas with a TARGET on its chest — an oxblood ring and a cream
    /// bull — the shoulder board, no hat; it keeps the flashbulb gun on its right forearm for the range. Was 40 → 35.
    static void DummyBody(Rig rig)
    {
        PosterBody(rig, 0.44f, 0.4f, true);
        Turned(Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0f, 0.1f, 0.113f), new Vector3(0.22f, 0.002f, 0.22f), OxbloodShell(), "TargetRing"), 90f);
        Turned(Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0f, 0.1f, 0.117f), new Vector3(0.1f, 0.002f, 0.1f), Cream(), "TargetBull"), 90f);
        Reshape(Piece(rig.armR, "Barrel"), PrimitiveType.Cube, new Vector3(0.05f, -0.62f, 0.06f), new Vector3(0.05f, 0.3f, 0.05f), Lacquer());
    }

    /// THE HOUSE OFFICER (Marshal; a footman): the civic figure in the company's black lacquer with brass frogging across
    /// the chest and the peaked cap on its warrant lamp's hood; the gun arm as built. Was 40 → 39.
    static void OfficerBody(Rig rig)
    {
        PosterBody(rig, 0.5f, 0.44f, true);
        Frogging(rig.torso, new Vector3(0f, 0.17f, 0.113f), 0.3f, 3, 0.1f);
        Cap(rig.headPivot, new Vector3(0f, 0.185f, 0f), 0.15f);
    }

    // ---------------------------------------------------------------- THE DOORMAN: the shooter

    /// What BuildSentryRig built (a brass post under a gun housing: the law's hitbox, pivots, muzzle, lamp and the cell on
    /// its back are all there), cut to THE DOORMAN of the posters: a long CHARCOAL GREATCOAT to the knee — the chest block
    /// (the plate: it goes red on the last beat) on the torso that pitches to aim, the skirt (the post, reshaped) holding
    /// still on the root, trousers (the braces) and lacquer shoes under the hem — a shoulder board across it, brass
    /// frogging as three bars, the name plate under them, a dark oval of a head under a peaked cap. THE LAMP it aims
    /// with is in its hand, not on the cap: the muzzle (law) sits 0.95 m ahead of the chest, so the right arm reaches out
    /// to the centre line holding a brass-banded black LANTERN whose lens is the unit's lamp (rig.status, reparented
    /// under the arm so it recoils with it): amber while it aims, red for the last beat, the bolt out of it. The left arm
    /// hangs. The cell on its back keeps its place; its fins become two brass straps that reach the coat. Was 24 → 24.
    static void DoormanBody(Rig rig)
    {
        Transform root = rig.torso.parent;
        Material coat = Coat();
        Drop(root, "Pelvis");
        Drop(root, "PostBand");
        Reshape(Piece(root, "Post"), PrimitiveType.Cube, new Vector3(0f, 0.78f, 0f), new Vector3(0.44f, 0.64f, 0.28f), coat);
        foreach (var leg in new[] { rig.legL, rig.legR })
        {
            Reshape(Piece(leg, "Brace"), PrimitiveType.Cube, new Vector3(0f, -0.31f, 0f), new Vector3(0.13f, 0.62f, 0.16f), coat);
            Part(leg, PrimitiveType.Cube, new Vector3(0f, -0.6f, 0.05f), new Vector3(0.12f, 0.045f, 0.27f), Lacquer(), "Shoe");
        }

        // The torso: the chest block, the shoulder board (the housing band), the frogging, the plate; the gimbal and the checks go
        Reshape(Piece(rig.torso, "Housing"), PrimitiveType.Cube, Vector3.zero, new Vector3(0.5f, 0.4f, 0.3f));
        Reshape(Piece(rig.torso, "HousingBand"), PrimitiveType.Cube, new Vector3(0f, 0.2f, 0f), new Vector3(0.6f, 0.06f, 0.28f), coat);
        Drop(rig.torso, "Gimbal");
        Drop(rig.torso, "Check");
        Frogging(rig.torso, new Vector3(0f, 0.1f, 0.153f), 0.3f, 3, 0.1f);
        PlaceNamePlate(rig.torso, new Vector3(0f, -0.14f, 0.153f));
        HeadOval(rig.torso, new Vector3(0f, 0.4f, 0f), 0.3f, 0.36f);
        Cap(rig.torso, new Vector3(0f, 0.54f, 0f), 0.15f);

        // THE LANTERN ARM (right): the sleeve from the shoulder in to the centre line, the glove, the lantern on the muzzle line
        Vector3 shoulderR = new Vector3(0.11f, 0.22f, -0.3f), handR = new Vector3(-0.12f, 0f, 0.48f);   // arm-local: the pivot sits at torso (0.14, -0.02, 0.3)
        ReshapeStrut(Piece(rig.armR, "Barrel"), shoulderR, handR, 0.12f, coat);
        Part(rig.armR, PrimitiveType.Cube, new Vector3(-0.13f, -0.01f, 0.52f), new Vector3(0.1f, 0.07f, 0.14f), GloveWhite(), "Hand");
        Reshape(Piece(rig.armR, "MuzzleRing"), PrimitiveType.Cylinder, new Vector3(-0.14f, 0f, 0.53f), new Vector3(0.14f, 0.09f, 0.14f), LacquerDome(), 90f);
        Turned(Part(rig.armR, PrimitiveType.Cylinder, new Vector3(-0.14f, 0f, 0.53f), new Vector3(0.15f, 0.012f, 0.15f), Brass(), "LanternBand"), 90f);
        if (rig.status != null)
        {
            rig.status.transform.SetParent(rig.armR, false);
            LampDisc(rig.status, new Vector3(-0.14f, 0f, 0.618f), 0.1f);
        }
        // The other arm hangs at its side: the sleeve down from the shoulder, the glove at the hem
        Vector3 shoulderL = new Vector3(-0.11f, 0.22f, -0.3f), handL = new Vector3(-0.14f, -0.44f, -0.3f);
        ReshapeStrut(Piece(rig.armL, "Barrel"), shoulderL, handL, 0.12f, coat);
        Reshape(Piece(rig.armL, "MuzzleRing"), PrimitiveType.Cube, new Vector3(-0.14f, -0.5f, -0.3f), new Vector3(0.1f, 0.13f, 0.05f), GloveWhite());

        // The cell on its back: the fins as two brass straps from it to the coat (under rig.ring: they flare with it)
        if (rig.ring != null)
            for (int i = 0; i < rig.ring.childCount; i++)
            {
                var fin = rig.ring.GetChild(i);
                if (fin.name == "Fin") Reshape(fin.GetComponent<Renderer>(), PrimitiveType.Cube, new Vector3(fin.localPosition.x, 0f, 0.14f), new Vector3(0.03f, 0.26f, 0.36f), Brass());
            }
    }

    // ---------------------------------------------------------------- THE BOUNCER: the wall

    /// What BuildWardenRig built (the guard, the lance, the iron helm: the law's hitbox, pivots, the core on its back),
    /// cut to THE BOUNCER of the posters: a DOUBLE-BREASTED COAT two sizes wider — the chest block (the plate) with the
    /// widest shoulder board in the game across it and two brass button bars down the front, the skirt (the hip plate)
    /// hanging from the hips, straight box legs — a bowler on a dark oval of a head (the helm and the visor, reshaped),
    /// white gloves on arms that hang (the guard and the lance are gone: a wall shoves, it holds nothing). The coat
    /// wears the Warden's plate colour (EnemyVariety.VarietyPlate: bronze); black is one line there. Was 38 → 33.
    static void BouncerBody(Rig rig)
    {
        Transform root = rig.torso.parent;
        var chest = Piece(rig.torso, "Chest");
        Material shell = chest != null ? chest.sharedMaterial : OxbloodShell();
        Reshape(Piece(root, "Pelvis"), PrimitiveType.Cube, new Vector3(0f, 0.93f, 0f), new Vector3(0.6f, 0.36f, 0.36f));
        var mount = root.Find("KeyMount");
        if (mount != null) mount.localPosition = new Vector3(0f, 0.99f, -0.17f);
        foreach (var leg in new[] { rig.legL, rig.legR })
        {
            Reshape(Piece(leg, "Thigh"), PrimitiveType.Cube, new Vector3(0f, -0.22f, 0f), new Vector3(0.16f, 0.36f, 0.17f));
            Reshape(Piece(leg, "Shin"), PrimitiveType.Cube, new Vector3(0f, -0.66f, 0f), new Vector3(0.14f, 0.36f, 0.15f));
            Reshape(Piece(leg, "Shoe"), PrimitiveType.Cube, new Vector3(0f, -0.9f, 0.07f), new Vector3(0.14f, 0.05f, 0.3f), Lacquer());
        }

        // The torso: the wide chest, the board, the bars, the lamp disc, the plate; the helm and visor become the head and the bowler's dome, the iron shoulders the brim and the board
        const float front = 0.2f;
        Reshape(chest, PrimitiveType.Cube, new Vector3(0f, 0.16f, 0f), new Vector3(0.66f, 0.5f, front * 2f));
        var board = Reshape(Piece(rig.torso, "ShoulderR"), PrimitiveType.Cube, new Vector3(0f, 0.41f, 0f), new Vector3(0.9f, 0.07f, 0.42f), shell);
        AddPlate(rig, board);
        for (int s = -1; s <= 1; s += 2)
            Part(rig.torso, PrimitiveType.Cube, new Vector3(s * 0.1f, 0.16f, front + 0.003f), new Vector3(0.03f, 0.36f, 0.012f), Brass(), "ButtonBar");
        LampDisc(rig.status, new Vector3(0f, 0.3f, front + 0.002f), 0.12f);
        PlaceNamePlate(rig.torso, new Vector3(0f, -0.1f, front + 0.005f));
        var head = Reshape(Piece(rig.torso, "Helm"), PrimitiveType.Sphere, new Vector3(0f, 0.6f, 0f), new Vector3(0.3f, 0.36f, 0.3f), GlassDark());
        if (head != null) head.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Reshape(Piece(rig.torso, "Visor"), PrimitiveType.Sphere, new Vector3(0f, 0.74f, 0f), new Vector3(0.3f, 0.24f, 0.3f), LacquerDome());
        Reshape(Piece(rig.torso, "ShoulderL"), PrimitiveType.Cylinder, new Vector3(0f, 0.69f, 0f), new Vector3(0.45f, 0.008f, 0.45f), LacquerDome());

        // The arms hang: sleeves on the shoulder balls' line, brass elbows, white gloves; the guard and the lance go
        Reshape(Piece(rig.armL, "UpperArm"), PrimitiveType.Cube, new Vector3(0f, -0.2f, 0f), new Vector3(0.16f, 0.34f, 0.16f));
        Reshape(Piece(rig.armL, "Forearm"), PrimitiveType.Cube, new Vector3(0f, -0.56f, 0f), new Vector3(0.15f, 0.34f, 0.15f), shell);
        Ball(rig.armL, new Vector3(0f, -0.38f, 0f), 0.15f, "Elbow");
        Drop(rig.armL, "Guard");
        Drop(rig.armL, "GuardRim");
        Reshape(Piece(rig.armL, "GuardFace"), PrimitiveType.Cube, new Vector3(0f, -0.77f, 0.02f), new Vector3(0.15f, 0.16f, 0.07f), GloveWhite());
        Reshape(Piece(rig.armR, "UpperArm"), PrimitiveType.Cube, new Vector3(0f, -0.2f, 0f), new Vector3(0.16f, 0.34f, 0.16f));
        Reshape(Piece(rig.armR, "Elbow"), PrimitiveType.Sphere, new Vector3(0f, -0.38f, 0f), Vector3.one * 0.15f);
        Reshape(Piece(rig.armR, "Forearm"), PrimitiveType.Cube, new Vector3(0f, -0.56f, 0f), new Vector3(0.15f, 0.34f, 0.15f));
        Reshape(Piece(rig.armR, "Lance"), PrimitiveType.Cube, new Vector3(0f, -0.77f, 0.02f), new Vector3(0.15f, 0.16f, 0.07f), GloveWhite());
        Drop(rig.armR, "LanceHead");
    }
}
