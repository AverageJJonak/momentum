using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HALCYON RESPONSE — the company's law, met from Chapter 3: the patrolman
/// (Pursuit), the hound, the courier (Interceptor), the stoker (Kettler), and
/// with them housekeeping (DamageControl) and the doorman (Sentry). Since the
/// simplification (2026-10-16) only their BODIES live here (the rigs: black
/// lacquer, a lightbar where a face would be, the hound's forelegs are its
/// arms, the courier's rotors are): what they DO is a role in EnemyRoles — the
/// doorman is THE SHOOTER, every other one a footman on its line.
/// </summary>
public partial class Enemy
{
    // The staff roles the response read as since the House overhaul (EnemyLivery.StaffName); the code names never change
    public const string PursuitName = "PATROLMAN", HoundName = "HOUND", InterceptorName = "COURIER", KettlerName = "STOKER";

    static bool IsResponse(EnemyKind k) => k == EnemyKind.Pursuit || k == EnemyKind.Hound || k == EnemyKind.Interceptor || k == EnemyKind.Kettler
        || k == EnemyKind.DamageControl || k == EnemyKind.Sentry;

    static Material respPlate, respDark, respWhite, respLamp, respRed, respLine;
    static Material ResponsePlate() => respPlate ??= Lit(new Color(0.05f, 0.05f, 0.06f), 0.3f, 0.9f);    // black lacquer: the cruiser's paint
    static Material ResponseDark() => respDark ??= Lit(new Color(0.09f, 0.095f, 0.11f), 0.3f, 0.5f);    // charcoal
    static Material ResponseWhite() => respWhite ??= Lit(new Color(0.88f, 0.82f, 0.68f), 0.1f, 0.8f);   // the tape: cream enamel
    static Material ResponseLamp() => respLamp ??= Unlit(new Color(2.2f, 1.3f, 0.3f));                   // amber: a unit's own light
    static Material ResponseRed() => respRed ??= Unlit(new Color(2.4f, 0.25f, 0.15f));                   // red: the shot is coming
    static Material ResponseLine() => respLine ??= Unlit(new Color(2.2f, 1.4f, 0.4f));                   // the patrolman's line spool (look only)

    public static Enemy CreatePursuit(Vector3 groundPos, MovementConfig config)
    {
        var e = Create(EnemyKind.Pursuit, groundPos, config);
        if (e == null) return null;   // no units (Enemy.Units): nothing to shape
        e.transform.localScale = Vector3.one * 1.08f;
        return e;
    }

    public static Enemy CreateHound(Vector3 groundPos, MovementConfig config)
        => Create(EnemyKind.Hound, groundPos, config);

    /// Flyers: `pos` is where the body sits
    public static Enemy CreateInterceptor(Vector3 pos, MovementConfig config)
        => Create(EnemyKind.Interceptor, pos - Vector3.up * ChestHeight, config);

    public static Enemy CreateKettler(Vector3 groundPos, MovementConfig config)
    {
        var e = Create(EnemyKind.Kettler, groundPos, config);
        if (e == null) return null;   // no units (Enemy.Units): nothing to shape
        e.transform.localScale = Vector3.one * 1.2f;
        return e;
    }

    // ---------------------------------------------------------------- the rigs

    static Rig BuildResponseRig(Transform root, EnemyKind kind, Material plate)
    {
        switch (kind)
        {
            case EnemyKind.Pursuit: return BuildPursuitRig(root, plate);
            case EnemyKind.Hound: return BuildHoundRig(root, plate);
            case EnemyKind.Interceptor: return BuildInterceptorRig(root, plate);
            case EnemyKind.DamageControl: return BuildDamageControlRig(root, plate);
            case EnemyKind.Sentry: return BuildSentryRig(root, plate);
            default: return BuildKettlerRig(root, plate);
        }
    }

    static Renderer Turned(Renderer r, float x, float y = 0f, float z = 0f)
    {
        r.transform.localRotation = Quaternion.Euler(x, y, z);
        return r;
    }

    /// The pursuit strider: two and a half metres of reverse-jointed leg under a wedge of a body, no head on it —
    /// a lightbar, and the lens slung under it — and a baton for a right arm
    static Rig BuildPursuitRig(Transform root, Material plate)
    {
        var rig = new Rig();
        var plates = new List<Renderer>();
        Material dark = ResponseDark(), joint = JointMat(), white = ResponseWhite(), lamp = ResponseLamp();

        var hitbox = new GameObject("Hitbox");
        hitbox.transform.SetParent(root, false);
        var capsule = hitbox.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 1.2f, 0f);
        capsule.radius = 0.32f;
        capsule.height = 2.1f;

        // THE PATROLMAN: a black lacquer mannequin on its strider's legs (brass hip, knee and hock balls), the key in the
        // small of its back   [1 + 7 × 2 + 2]
        plates.Add(Part(root, PrimitiveType.Sphere, new Vector3(0f, 1.4f, 0f), new Vector3(0.36f, 0.2f, 0.28f), plate, "Pelvis"));
        Key(rig, root, new Vector3(0f, 1.4f, -0.13f), 0.14f);
        rig.legL = PursuitLeg(root, -0.17f, dark, joint, plate, plates);
        rig.legR = PursuitLeg(root, 0.17f, dark, joint, plate, plates);

        // The chest shell with its band and a high collar, two cream checks, the name plate   [7]
        rig.torso = Pivot(root, "Torso", new Vector3(0f, 1.5f, 0f));
        rig.torsoRest = rig.torso.localPosition;
        plates.Add(Turned(Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0.1f), new Vector3(0.42f, 0.32f, 0.66f), plate, "Chest"), 16f));
        Turned(Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0.1f), new Vector3(0.44f, 0.08f, 0.68f), joint, "ChestBand"), 16f);
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.46f, -0.12f), new Vector3(0.05f, 0.26f, 0.5f), dark, "Collar");
        for (int s = -1; s <= 1; s += 2)
            Turned(Part(rig.torso, PrimitiveType.Cube, new Vector3(s * 0.215f, 0.3f, -0.1f), new Vector3(0.012f, 0.09f, 0.1f), white, "Check"), 16f);
        NamePlate(rig.torso, new Vector3(0f, 0.08f, 0.36f), 0.3f, EnemyKind.Pursuit);

        // Where a head would be: the lightbar (the cap sits on it: EnemyLivery), and the lens under it (the weakspot)   [2 + 4]
        rig.headPivot = Pivot(rig.torso, "HeadPivot", new Vector3(0f, 0.5f, 0.36f));
        Part(rig.headPivot, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0f), new Vector3(0.96f, 0.04f, 0.18f), joint, "BarMount");
        rig.status = Part(rig.headPivot, PrimitiveType.Cube, new Vector3(0f, 0.26f, 0f), new Vector3(0.92f, 0.09f, 0.15f), lamp, "Lightbar");
        // THE CELL: a cyan fuse hung under the bar between two brass clamps
        Weakpoint(rig, rig.headPivot, PrimitiveType.Cylinder, new Vector3(0.24f, 0.2f, 0.24f), Quaternion.identity);
        Part(rig.ring, PrimitiveType.Cube, new Vector3(0f, 0.21f, 0f), new Vector3(0.3f, 0.05f, 0.3f), joint, "ClampTop");
        Part(rig.ring, PrimitiveType.Cube, new Vector3(0f, -0.21f, 0f), new Vector3(0.3f, 0.05f, 0.3f), joint, "ClampLow");
        Part(rig.ring, PrimitiveType.Cube, new Vector3(0f, 0f, -0.15f), new Vector3(0.06f, 0.46f, 0.04f), joint, "ClampSpine");

        // THE TRUNCHEON arm (black lacquer, a brass elbow, the amber tip), and a folded stub for the other   [4 + 2]
        rig.armR = Pivot(rig.torso, "ArmR", new Vector3(0.27f, 0.28f, 0.18f));
        Shell(rig.armR, new Vector3(0f, -0.16f, 0f), new Vector3(0.09f, 0.32f, 0.09f), dark, "UpperArm");
        Ball(rig.armR, new Vector3(0f, -0.33f, 0f), 0.12f, "Elbow");
        Part(rig.armR, PrimitiveType.Cube, new Vector3(0f, -0.4f, 0.42f), new Vector3(0.05f, 0.05f, 0.95f), Satin(), "Truncheon");
        Part(rig.armR, PrimitiveType.Sphere, new Vector3(0f, -0.4f, 0.9f), Vector3.one * 0.1f, lamp, "TruncheonTip");
        rig.muzzle = Pivot(rig.armR, "Tip", new Vector3(0f, -0.4f, 0.92f));
        rig.armL = Pivot(rig.torso, "ArmL", new Vector3(-0.27f, 0.28f, 0.18f));
        Shell(rig.armL, new Vector3(0f, -0.16f, 0f), new Vector3(0.09f, 0.32f, 0.09f), dark, "UpperArm");
        plates.Add(Part(rig.armL, PrimitiveType.Cube, new Vector3(0f, -0.38f, 0.14f), new Vector3(0.07f, 0.07f, 0.36f), plate, "Forearm"));
        rig.plates = plates.ToArray();   // 36 + cap = 39
        return rig;
    }

    /// A strider's reverse-jointed leg as a mannequin's: brass balls at the hip, the knee and the hock, shells between [7]
    static Transform PursuitLeg(Transform root, float x, Material dark, Material joint, Material plate, List<Renderer> plates)
    {
        Transform hip = Pivot(root, x < 0f ? "LegL" : "LegR", new Vector3(x, 1.36f, 0f));
        Ball(hip, Vector3.zero, 0.18f, "Hip");
        Turned(Shell(hip, new Vector3(0f, -0.22f, 0.09f), new Vector3(0.13f, 0.52f, 0.16f), dark, "Thigh"), -24f);
        Ball(hip, new Vector3(0f, -0.46f, 0.19f), 0.14f, "Knee");
        plates.Add(Turned(Shell(hip, new Vector3(0f, -0.72f, 0.06f), new Vector3(0.1f, 0.6f, 0.12f), plate, "Shank"), 26f));
        Ball(hip, new Vector3(0f, -0.99f, -0.07f), 0.11f, "Hock");
        Turned(Shell(hip, new Vector3(0f, -1.16f, 0f), new Vector3(0.08f, 0.4f, 0.09f), dark, "Cannon"), -22f);
        Part(hip, PrimitiveType.Cube, new Vector3(0f, -1.33f, 0.12f), new Vector3(0.15f, 0.06f, 0.3f), Satin(), "Toe");
        return hip;
    }

    /// The hound: a wedge on four legs with a clamp where a head would be, a lightbar down its spine, and the core
    /// between its shoulders (the weakspot: from above, from behind, or an arrow from anywhere)
    static Rig BuildHoundRig(Transform root, Material plate)
    {
        var rig = new Rig();
        var plates = new List<Renderer>();
        Material dark = ResponseDark(), joint = JointMat(), white = ResponseWhite(), lamp = ResponseLamp();

        var hitbox = new GameObject("Hitbox");
        hitbox.transform.SetParent(root, false);
        var box = hitbox.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.6f, 0.05f);
        box.size = new Vector3(0.5f, 0.6f, 1.5f);

        // THE CLOCKWORK DOG: a lacquer rump with the key out of it, four brass-jointed legs   [1 + 2 + 5 × 2]
        plates.Add(Part(root, PrimitiveType.Sphere, new Vector3(0f, 0.66f, -0.42f), new Vector3(0.36f, 0.24f, 0.32f), plate, "Pelvis"));
        Key(rig, root, new Vector3(0f, 0.7f, -0.57f), 0.12f);
        rig.legL = HoundLeg(root, "LegL", new Vector3(-0.2f, 0.64f, -0.45f), true, dark, joint, plate, plates);
        rig.legR = HoundLeg(root, "LegR", new Vector3(0.2f, 0.64f, -0.45f), true, dark, joint, plate, plates);

        // The body shell, the lightbar down its spine, two cream checks a side, and THE BRASS JAW: a fixed lower jaw and
        // an upper one on a hinge, cream teeth   [1 + 1 + 1 + 4 + 3]
        rig.torso = Pivot(root, "Torso", new Vector3(0f, 0.7f, 0f));
        rig.torsoRest = rig.torso.localPosition;
        plates.Add(ShellZ(rig.torso, new Vector3(0f, 0f, 0.05f), new Vector3(0.4f, 1.1f, 0.32f), plate, "Body"));
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, -0.19f, 0.05f), new Vector3(0.3f, 0.1f, 0.9f), dark, "Belly");
        rig.status = Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.19f, -0.15f), new Vector3(0.1f, 0.05f, 0.62f), lamp, "Lightbar");
        for (int i = 0; i < 2; i++)
            for (int s = -1; s <= 1; s += 2)
                Part(rig.torso, PrimitiveType.Cube, new Vector3(s * 0.203f, 0.02f, -0.22f + i * 0.4f), new Vector3(0.012f, 0.12f, 0.1f), white, "Check");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, -0.1f, 0.78f), new Vector3(0.3f, 0.06f, 0.42f), joint, "JawLow");
        rig.hatch = Pivot(rig.torso, "JawHinge", new Vector3(0f, 0.04f, 0.6f));
        Part(rig.hatch, PrimitiveType.Cube, new Vector3(0f, 0f, 0.2f), new Vector3(0.32f, 0.07f, 0.44f), joint, "JawHigh");
        Part(rig.hatch, PrimitiveType.Cube, new Vector3(0f, -0.045f, 0.36f), new Vector3(0.3f, 0.03f, 0.08f), white, "Teeth");
        rig.muzzle = Pivot(rig.torso, "Bite", new Vector3(0f, -0.03f, 0.95f));

        rig.armL = HoundLeg(rig.torso, "ArmL", new Vector3(-0.2f, -0.06f, 0.45f), false, dark, joint, plate, plates);   // [5 × 2]
        rig.armR = HoundLeg(rig.torso, "ArmR", new Vector3(0.2f, -0.06f, 0.45f), false, dark, joint, plate, plates);

        rig.headPivot = Pivot(rig.torso, "HeadPivot", new Vector3(0f, 0.3f, 0.3f));
        // THE PACK: a cyan battery strapped to its back between two brass rails (unchanged)   [4]
        Weakpoint(rig, rig.headPivot, PrimitiveType.Cube, new Vector3(0.26f, 0.13f, 0.46f), Quaternion.identity);
        for (int s = -1; s <= 1; s += 2)
            Part(rig.ring, PrimitiveType.Cube, new Vector3(s * 0.16f, 0f, 0f), new Vector3(0.04f, 0.17f, 0.5f), joint, "PackRail");
        Part(rig.ring, PrimitiveType.Cube, new Vector3(0f, 0.075f, 0f), new Vector3(0.36f, 0.03f, 0.09f), joint, "PackStrap");
        rig.plates = plates.ToArray();   // 37 + collar, tag = 39
        return rig;
    }

    /// A dog's leg as a mannequin's: the brass hip ball, the upper shell (a plate), the brass joint, the lower rod, a paw [5]
    static Transform HoundLeg(Transform parent, string name, Vector3 at, bool hind, Material dark, Material joint, Material plate, List<Renderer> plates)
    {
        Transform hip = Pivot(parent, name, at);
        float k = hind ? 1f : -1f;
        Ball(hip, Vector3.zero, 0.13f, "Hip");
        plates.Add(Turned(Shell(hip, new Vector3(0f, -0.16f, -0.04f * k), new Vector3(0.1f, 0.36f, 0.14f), plate, "Upper"), 22f * k));
        Ball(hip, new Vector3(0f, -0.32f, -0.1f * k), 0.1f, "Joint");
        Turned(Part(hip, PrimitiveType.Cylinder, new Vector3(0f, -0.47f, -0.04f * k), new Vector3(0.06f, 0.17f, 0.06f), joint, "Lower"), -20f * k);
        Part(hip, PrimitiveType.Cube, new Vector3(0f, -0.61f, 0.06f), new Vector3(0.12f, 0.05f, 0.2f), Satin(), "Paw");
        return hip;
    }

    /// The interceptor: a lightbar that flies. Two rotors on its ends (its arms), the lens in a pod under the middle (the
    /// weakspot), two skids (its legs). Built round where a chest would be, like the sentinel.
    static Rig BuildInterceptorRig(Transform root, Material plate)
    {
        var rig = new Rig();
        var plates = new List<Renderer>();
        Material dark = ResponseDark(), joint = JointMat(), white = ResponseWhite(), lamp = ResponseLamp();

        var hitbox = new GameObject("Hitbox");
        hitbox.transform.SetParent(root, false);
        var box = hitbox.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, ChestHeight, 0f);
        box.size = new Vector3(1.9f, 0.42f, 0.7f);

        // THE COURIER is a wind-up autogyro: a lacquer fuselage with a key out of its top, brass struts to two skids   [1 + 4]
        Part(root, PrimitiveType.Cube, new Vector3(0f, ChestHeight - 0.12f, -0.1f), new Vector3(0.3f, 0.12f, 0.3f), joint, "Pelvis");
        rig.legL = Pivot(root, "LegL", new Vector3(-0.3f, ChestHeight - 0.1f, 0f));
        rig.legR = Pivot(root, "LegR", new Vector3(0.3f, ChestHeight - 0.1f, 0f));
        foreach (var leg in new[] { rig.legL, rig.legR })
        {
            Part(leg, PrimitiveType.Cube, new Vector3(0f, -0.14f, 0f), new Vector3(0.03f, 0.26f, 0.03f), joint, "Strut");
            Part(leg, PrimitiveType.Cube, new Vector3(0f, -0.28f, 0.02f), new Vector3(0.04f, 0.03f, 0.5f), Satin(), "Skid");
        }

        // The fuselage on a brass outrigger bar, the keel, the lightbar along the bar, the tail fins, the key   [1 + 1 + 1 + 1 + 2 + 2]
        rig.torso = Pivot(root, "Torso", new Vector3(0f, ChestHeight, 0f));
        rig.torsoRest = rig.torso.localPosition;
        plates.Add(ShellZ(rig.torso, Vector3.zero, new Vector3(0.34f, 0.9f, 0.3f), plate, "Fuselage"));
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.04f, 0f), new Vector3(1.5f, 0.06f, 0.08f), joint, "Outrigger");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, -0.07f, 0f), new Vector3(1.3f, 0.04f, 0.22f), dark, "Keel");
        rig.status = Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.1f, 0f), new Vector3(1.36f, 0.09f, 0.2f), lamp, "Lightbar");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.12f, -0.46f), new Vector3(0.02f, 0.22f, 0.2f), white, "TailFin");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.02f, -0.46f), new Vector3(0.4f, 0.02f, 0.18f), white, "TailPlane");
        Key(rig, rig.torso, new Vector3(0f, 0.14f, -0.26f), 0.1f, Vector3.up);

        // The rotors: a lacquer duct each end of the bar, the cream blades on a brass hub that spins (handL / handR)   [4 × 2]
        rig.armL = Pivot(rig.torso, "ArmL", new Vector3(-0.85f, 0f, 0f));
        rig.armR = Pivot(rig.torso, "ArmR", new Vector3(0.85f, 0f, 0f));
        rig.handL = Rotor(rig.armL, dark, joint, white);
        rig.handR = Rotor(rig.armR, dark, joint, white);

        rig.headPivot = Pivot(rig.torso, "HeadPivot", new Vector3(0f, -0.24f, 0.06f));
        Part(rig.headPivot, PrimitiveType.Cube, new Vector3(0f, 0.1f, -0.04f), new Vector3(0.34f, 0.12f, 0.34f), dark, "Pod");   // [1 + 4]
        // THE DIAMOND: a cyan cube on its point, hung in a brass cradle under the pod (unchanged)
        Weakpoint(rig, rig.headPivot, PrimitiveType.Cube, Vector3.one * 0.24f, Quaternion.Euler(45f, 0f, 45f));
        rig.headPivot.localPosition += Vector3.down * 0.08f;
        for (int s = -1; s <= 1; s += 2)
            Part(rig.ring, PrimitiveType.Cube, new Vector3(s * 0.22f, 0.04f, 0f), new Vector3(0.03f, 0.36f, 0.03f), joint, "CradleArm");
        Part(rig.ring, PrimitiveType.Cube, new Vector3(0f, -0.13f, 0f), new Vector3(0.47f, 0.03f, 0.03f), joint, "CradleBar");
        rig.muzzle = Pivot(rig.headPivot, "Muzzle", new Vector3(0f, 0f, 0.2f));
        rig.plates = plates.ToArray();   // 26 + pillbox, satchel, clasp = 29
        return rig;
    }

    static Transform Rotor(Transform arm, Material dark, Material joint, Material white)
    {
        Part(arm, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.56f, 0.045f, 0.56f), dark, "Duct");
        Part(arm, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.12f, 0.03f, 0.12f), joint, "Hub");
        Transform blades = Pivot(arm, "Blades", new Vector3(0f, 0.06f, 0f));
        Part(blades, PrimitiveType.Cube, Vector3.zero, new Vector3(0.5f, 0.012f, 0.06f), white, "Blade");
        Part(blades, PrimitiveType.Cube, Vector3.zero, new Vector3(0.06f, 0.012f, 0.5f), white, "Blade");
        return blades;
    }

    /// The kettler: a pylon that walks. A tapering obelisk on two short legs, a projector fin down either side of it (its
    /// arms: they swing out when it projects), a lightbar band round the top, the lens on top of that — sealed until it projects
    static Rig BuildKettlerRig(Transform root, Material plate)
    {
        var rig = new Rig();
        var plates = new List<Renderer>();
        Material dark = ResponseDark(), joint = JointMat(), white = ResponseWhite(), lamp = ResponseLamp(), red = ResponseRed();

        var hitbox = new GameObject("Hitbox");
        hitbox.transform.SetParent(root, false);
        var capsule = hitbox.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 1.3f, 0f);
        capsule.radius = 0.45f;
        capsule.height = 2.5f;

        // THE STOKER: an iron hip, short brass-jointed legs in lacquer shins   [1 + 5 × 2]
        Part(root, PrimitiveType.Cube, new Vector3(0f, 0.86f, 0f), new Vector3(0.8f, 0.2f, 0.5f), WorksIron(), "Pelvis");
        rig.legL = Pivot(root, "LegL", new Vector3(-0.34f, 0.82f, 0f));
        rig.legR = Pivot(root, "LegR", new Vector3(0.34f, 0.82f, 0f));
        foreach (var leg in new[] { rig.legL, rig.legR })
        {
            Ball(leg, Vector3.zero, 0.24f, "Hip");
            plates.Add(Shell(leg, new Vector3(0f, -0.2f, 0f), new Vector3(0.22f, 0.42f, 0.22f), plate, "Thigh"));
            Ball(leg, new Vector3(0f, -0.42f, 0f), 0.2f, "Knee");
            Shell(leg, new Vector3(0f, -0.58f, 0.02f), new Vector3(0.18f, 0.36f, 0.18f), Satin(), "Shin");
            Part(leg, PrimitiveType.Cube, new Vector3(0f, -0.77f, 0.08f), new Vector3(0.3f, 0.1f, 0.46f), Satin(), "Foot");
        }

        // THE BOILER BODY: the firebox stage with its door glowing amber (its own light), the brass bands, the boiler
        // drum, the top stage with the name plate, the lightbar band; the key out of the firebox's back   [6 + 2 + 2 + 2 + 2]
        rig.torso = Pivot(root, "Torso", new Vector3(0f, 0.95f, 0f));
        rig.torsoRest = rig.torso.localPosition;
        plates.Add(Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.35f, 0f), new Vector3(0.62f, 0.7f, 0.5f), plate, "Firebox"));
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.72f, 0f), new Vector3(0.66f, 0.06f, 0.54f), joint, "BandLow");
        plates.Add(Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0f, 1.08f, 0f), new Vector3(0.5f, 0.33f, 0.5f), plate, "Boiler"));
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 1.43f, 0f), new Vector3(0.54f, 0.06f, 0.46f), joint, "BandHigh");
        plates.Add(Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 1.68f, 0f), new Vector3(0.38f, 0.44f, 0.34f), plate, "StageHigh"));
        rig.status = Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 1.93f, 0f), new Vector3(0.44f, 0.07f, 0.4f), lamp, "Lightbar");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.38f, 0.256f), new Vector3(0.3f, 0.3f, 0.012f), WorksIron(), "FireboxDoor");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.34f, 0.264f), new Vector3(0.2f, 0.16f, 0.008f), lamp, "FireboxGlow");
        for (int s = -1; s <= 1; s += 2)
            Part(rig.torso, PrimitiveType.Cube, new Vector3(s * 0.24f, 0.6f, 0.253f), new Vector3(0.08f, 0.1f, 0.012f), white, "Check");
        NamePlate(rig.torso, new Vector3(0f, 1.7f, 0.176f), 0.3f, EnemyKind.Kettler, 0.012f);
        Key(rig, rig.torso, new Vector3(0f, 0.5f, -0.25f), 0.2f);

        // The projector fins: shut against the body, a red strip down the inside of each   [2 × 2]
        rig.armL = Pivot(rig.torso, "ArmL", new Vector3(-0.36f, 1.5f, 0f));
        rig.armR = Pivot(rig.torso, "ArmR", new Vector3(0.36f, 1.5f, 0f));
        foreach (var arm in new[] { rig.armL, rig.armR })
        {
            float s = arm == rig.armL ? -1f : 1f;
            Part(arm, PrimitiveType.Cube, new Vector3(s * 0.04f, -0.62f, 0f), new Vector3(0.06f, 1.24f, 0.4f), dark, "Fin");
            Part(arm, PrimitiveType.Cube, new Vector3(s * 0.075f, -0.62f, 0f), new Vector3(0.012f, 1.1f, 0.3f), red, "Emitter");
        }

        // THE LENS: a cyan disc in a square brass frame on top of it, facing you — and two shutters across it until it
        // projects (unchanged)   [1 + 1 + 4 + 2]
        rig.headPivot = Pivot(rig.torso, "HeadPivot", new Vector3(0f, 2.3f, 0.02f));
        Part(rig.headPivot, PrimitiveType.Cylinder, new Vector3(0f, -0.4f, 0f), new Vector3(0.26f, 0.06f, 0.26f), joint, "Gimbal");
        Weakpoint(rig, rig.headPivot, PrimitiveType.Cylinder, new Vector3(0.52f, 0.05f, 0.52f), Quaternion.Euler(90f, 0f, 0f));
        for (int s = -1; s <= 1; s += 2)
        {
            Part(rig.ring, PrimitiveType.Cube, new Vector3(0f, s * 0.31f, 0f), new Vector3(0.68f, 0.06f, 0.18f), joint, "LensFrame");
            Part(rig.ring, PrimitiveType.Cube, new Vector3(s * 0.31f, 0f, 0f), new Vector3(0.06f, 0.68f, 0.18f), joint, "LensFrame");
        }
        rig.coatL = Pivot(rig.headPivot, "ShutterL", new Vector3(-0.14f, 0f, 0.08f));
        rig.coatR = Pivot(rig.headPivot, "ShutterR", new Vector3(0.14f, 0f, 0.08f));
        plates.Add(Part(rig.coatL, PrimitiveType.Cube, Vector3.zero, new Vector3(0.28f, 0.56f, 0.03f), plate, "Shutter"));
        plates.Add(Part(rig.coatR, PrimitiveType.Cube, Vector3.zero, new Vector3(0.28f, 0.56f, 0.03f), plate, "Shutter"));
        rig.muzzle = rig.headPivot;
        rig.plates = plates.ToArray();
        return rig;
    }

}
