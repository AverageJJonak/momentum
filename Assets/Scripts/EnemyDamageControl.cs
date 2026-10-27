using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE REST OF THE RESPONSE'S BODIES: HOUSEKEEPING (DamageControl — a wide,
/// squat black-lacquer frame under a roll cage of work lights, a steam tank
/// across its back, the hose and the spreader; a big footman now) and THE
/// DOORMAN (Sentry — a gun housing on a brass post, the lightbar on its face,
/// its coolant cell out of the BACK of it: THE SHOOTER, EnemyRoles). Every
/// Response unit's weakpoint is its own shape (Weakpoint): what they have in
/// common is the colour. Cyan means cut here. The response's issue heads for
/// the old civic units out in the district are built here too (IssueHead).
/// </summary>
public partial class Enemy
{
    // The staff roles these read as since the House overhaul (EnemyLivery.StaffName); the code names never change
    public const string DamageControlName = "HOUSEKEEPING", SentryName = "DOORMAN";

    public static Enemy CreateDamageControl(Vector3 groundPos, MovementConfig config)
    {
        var e = Create(EnemyKind.DamageControl, groundPos, config);
        if (e == null) return null;   // no units (Enemy.Units): nothing to shape
        e.transform.localScale = Vector3.one * 1.4f;
        return e;
    }

    public static Enemy CreateSentry(Vector3 groundPos, MovementConfig config)
        => Create(EnemyKind.Sentry, groundPos, config);

    // ---------------------------------------------------------------- weakpoints

    /// A Response weakpoint: the cyan piece itself (`rig.head` — it keeps its collider: it is what an execute finds) and an
    /// empty cage pivot round it (`rig.ring` — what flares open when the blade's aim rests on it). The caller hangs the
    /// cage's pieces on rig.ring. No two units share a shape; all of them share the colour.
    static void Weakpoint(Rig rig, Transform pivot, PrimitiveType type, Vector3 size, Quaternion rot)
    {
        rig.head = Part(pivot, type, Vector3.zero, size, WeakMat(), "Head", keepCollider: true);
        rig.head.transform.localRotation = rot;
        rig.head.gameObject.AddComponent<EnemyWeakspot>();
        rig.ring = Pivot(pivot, "Cage", Vector3.zero);
    }

    /// The response's issue, for the old units that serve out in the district: the civic globe-and-halo comes off and
    /// each gets a head that says what it is for.
    ///   SNIPER  an OPTIC — a cyan tube down the line it looks along, between two rails, a plate behind it
    ///   BLADE   a YOKE — an upright cyan cell between two prongs under a cap: a lantern with shoulders
    /// (The crew's is a caged lamp: EnemyWorks.WorksHead.)
    static void IssueHead(Rig rig, EnemyKind kind, Material chassis)
    {
        if (kind == EnemyKind.Turret)
        {
            Weakpoint(rig, rig.headPivot, PrimitiveType.Cylinder, new Vector3(0.28f, 0.26f, 0.28f), Quaternion.Euler(90f, 0f, 0f));
            for (int s = -1; s <= 1; s += 2)
                Part(rig.ring, PrimitiveType.Cube, new Vector3(0f, s * 0.17f, 0f), new Vector3(0.1f, 0.05f, 0.48f), chassis, "OpticRail");
            Part(rig.headPivot, PrimitiveType.Cube, new Vector3(0f, 0f, -0.29f), new Vector3(0.32f, 0.32f, 0.07f), chassis, "OpticBack");
            Turned(Part(rig.headPivot, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.265f), new Vector3(0.31f, 0.012f, 0.31f), VisorMat(), "OpticLip"), 90f);
            return;
        }
        Weakpoint(rig, rig.headPivot, PrimitiveType.Cylinder, new Vector3(0.3f, 0.2f, 0.3f), Quaternion.identity);
        for (int s = -1; s <= 1; s += 2)
            Part(rig.ring, PrimitiveType.Cube, new Vector3(s * 0.2f, 0.02f, 0f), new Vector3(0.05f, 0.5f, 0.2f), chassis, "YokeProng");
        Part(rig.ring, PrimitiveType.Cube, new Vector3(0f, 0.27f, 0f), new Vector3(0.45f, 0.05f, 0.2f), chassis, "YokeCap");
        Part(rig.headPivot, PrimitiveType.Cube, new Vector3(0f, 0.22f, 0.11f), new Vector3(0.3f, 0.05f, 0.05f), VisorMat(), "Brow");
    }

    // ---------------------------------------------------------------- the rigs

    static Rig BuildDamageControlRig(Transform root, Material plate)
    {
        var rig = new Rig();
        var plates = new List<Renderer>();
        Material dark = ResponseDark(), joint = JointMat(), white = ResponseWhite(), lamp = ResponseLamp();

        var hitbox = new GameObject("Hitbox");
        hitbox.transform.SetParent(root, false);
        var capsule = hitbox.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 1.05f, 0f);
        capsule.radius = 0.55f;
        capsule.height = 2f;

        // HOUSEKEEPING: a wide black lacquer mannequin — the hip shell, brass-jointed legs in lacquer shins and boots   [1 + 5 × 2]
        plates.Add(Part(root, PrimitiveType.Sphere, new Vector3(0f, 0.94f, 0f), new Vector3(0.9f, 0.26f, 0.6f), plate, "Pelvis"));
        rig.legL = Pivot(root, "LegL", new Vector3(-0.36f, 0.9f, 0f));
        rig.legR = Pivot(root, "LegR", new Vector3(0.36f, 0.9f, 0f));
        foreach (var leg in new[] { rig.legL, rig.legR })
        {
            Ball(leg, Vector3.zero, 0.34f, "Hip");
            plates.Add(Shell(leg, new Vector3(0f, -0.22f, 0f), new Vector3(0.3f, 0.46f, 0.3f), plate, "Thigh"));
            Ball(leg, new Vector3(0f, -0.46f, 0f), 0.28f, "Knee");
            Shell(leg, new Vector3(0f, -0.62f, 0.02f), new Vector3(0.26f, 0.4f, 0.26f), Satin(), "Shin");
            Part(leg, PrimitiveType.Cube, new Vector3(0f, -0.84f, 0.1f), new Vector3(0.4f, 0.12f, 0.62f), Satin(), "Boot");
        }

        // The chest shell with its brass band, two cream checks, the name plate; the roll cage over it with the work lights
        // (no head under there, only lamps); the STEAM tank across its back   [2 + 2 + 2 + 5 + 1 + 2]
        rig.torso = Pivot(root, "Torso", new Vector3(0f, 1.05f, 0f));
        rig.torsoRest = rig.torso.localPosition;
        plates.Add(Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.36f, 0.02f), new Vector3(1.05f, 0.72f, 0.72f), plate, "Chest"));
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.36f, 0.02f), new Vector3(1.08f, 0.1f, 0.75f), joint, "ChestBand");
        for (int s = -1; s <= 1; s += 2)
            Part(rig.torso, PrimitiveType.Cube, new Vector3(s * 0.4f, 0.14f, 0.385f), new Vector3(0.15f, 0.15f, 0.012f), white, "Check");
        NamePlate(rig.torso, new Vector3(0f, 0.56f, 0.386f), 0.5f, EnemyKind.DamageControl, 0.014f);
        for (int s = -1; s <= 1; s += 2)
        {
            Part(rig.torso, PrimitiveType.Cube, new Vector3(s * 0.46f, 0.92f, 0.02f), new Vector3(0.07f, 0.4f, 0.07f), joint, "CagePost");
            Part(rig.torso, PrimitiveType.Cube, new Vector3(s * 0.46f, 1.12f, 0.02f), new Vector3(0.07f, 0.07f, 0.74f), joint, "CageRail");
        }
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 1.12f, 0.36f), new Vector3(0.99f, 0.07f, 0.07f), joint, "CageBar");
        rig.status = Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 1.04f, 0.38f), new Vector3(0.8f, 0.1f, 0.06f), lamp, "WorkLights");
        Turned(Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0f, 0.62f, -0.5f), new Vector3(0.62f, 0.58f, 0.62f), joint, "Tank"), 0f, 0f, 90f);
        plates.Add(Turned(Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0f, 0.62f, -0.5f), new Vector3(0.65f, 0.12f, 0.65f), plate, "TankBand"), 0f, 0f, 90f));

        // THE STEAM HOSE arm (a brass nozzle on a lacquer shoulder), and the spreader: two long jaws on their own pivots
        // (handL / handR)   [3 + 2 + 2 × 2]
        rig.armR = Pivot(rig.torso, "ArmR", new Vector3(0.66f, 0.52f, 0.05f));
        Part(rig.armR, PrimitiveType.Cube, new Vector3(0f, -0.08f, 0.05f), new Vector3(0.32f, 0.36f, 0.4f), dark, "UpperArm");
        plates.Add(Turned(Part(rig.armR, PrimitiveType.Cylinder, new Vector3(0f, -0.12f, 0.56f), new Vector3(0.3f, 0.42f, 0.3f), plate, "Hose"), 90f));
        Turned(Part(rig.armR, PrimitiveType.Cylinder, new Vector3(0f, -0.12f, 0.98f), new Vector3(0.36f, 0.03f, 0.36f), joint, "Nozzle"), 90f);
        rig.muzzle = Pivot(rig.armR, "Muzzle", new Vector3(0f, -0.12f, 1.02f));
        rig.armL = Pivot(rig.torso, "ArmL", new Vector3(-0.66f, 0.52f, 0.05f));
        Part(rig.armL, PrimitiveType.Cube, new Vector3(0f, -0.08f, 0.05f), new Vector3(0.28f, 0.34f, 0.36f), dark, "UpperArm");
        Turned(Part(rig.armL, PrimitiveType.Cylinder, new Vector3(0f, -0.12f, 0.36f), new Vector3(0.16f, 0.14f, 0.16f), joint, "Ram"), 90f);
        rig.handL = Pivot(rig.armL, "JawL", new Vector3(-0.05f, -0.12f, 0.5f));
        rig.handR = Pivot(rig.armL, "JawR", new Vector3(0.05f, -0.12f, 0.5f));
        foreach (var jaw in new[] { rig.handL, rig.handR })
        {
            plates.Add(Part(jaw, PrimitiveType.Cube, new Vector3(0f, 0f, 0.45f), new Vector3(0.07f, 0.12f, 0.9f), plate, "Jaw"));
            Part(jaw, PrimitiveType.Cube, new Vector3(0f, 0f, 0.92f), new Vector3(0.09f, 0.14f, 0.1f), joint, "JawTip");
        }

        // THE VALVE: a cyan wheel on the back of the tank (where a key would be: it is the winding), brass spokes across
        // it, a guard plate over the lot until the pressure drops (unchanged)   [4 + 1]
        rig.headPivot = Pivot(rig.torso, "HeadPivot", new Vector3(0f, 0.62f, -0.84f));
        Weakpoint(rig, rig.headPivot, PrimitiveType.Cylinder, new Vector3(0.46f, 0.04f, 0.46f), Quaternion.Euler(90f, 0f, 0f));
        Part(rig.ring, PrimitiveType.Cube, new Vector3(0f, 0f, -0.06f), new Vector3(0.5f, 0.05f, 0.03f), joint, "Spoke");
        Part(rig.ring, PrimitiveType.Cube, new Vector3(0f, 0f, -0.06f), new Vector3(0.05f, 0.5f, 0.03f), joint, "Spoke");
        Part(rig.ring, PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.08f), new Vector3(0.12f, 0.03f, 0.12f), joint, "Boss").transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        rig.hatch = Pivot(rig.headPivot, "Guard", new Vector3(0f, 0.32f, -0.1f));
        plates.Add(Part(rig.hatch, PrimitiveType.Cube, new Vector3(0f, -0.32f, -0.02f), new Vector3(0.62f, 0.64f, 0.05f), plate, "GuardPlate"));
        rig.plates = plates.ToArray();
        return rig;
    }

    static Rig BuildSentryRig(Transform root, Material plate)
    {
        var rig = new Rig();
        var plates = new List<Renderer>();
        Material dark = ResponseDark(), joint = JointMat(), white = ResponseWhite(), lamp = ResponseLamp();

        var hitbox = new GameObject("Hitbox");
        hitbox.transform.SetParent(root, false);
        var capsule = hitbox.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 0.75f, 0f);
        capsule.radius = 0.34f;
        capsule.height = 1.5f;

        // THE DOORMAN stands his post: a brass post on a plate, two brass braces (its legs), the gun housing on top with
        // the company's name on it   [3 + 2 + 4 + 2 + 2]
        Part(root, PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(1f, 0.04f, 1f), dark, "Pelvis");
        Part(root, PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 0f), new Vector3(0.22f, 0.5f, 0.22f), joint, "Post");
        Part(root, PrimitiveType.Cube, new Vector3(0f, 0.3f, 0f), new Vector3(0.34f, 0.12f, 0.34f), plate, "PostBand");
        rig.legL = Pivot(root, "LegL", new Vector3(-0.12f, 0.62f, 0f));
        rig.legR = Pivot(root, "LegR", new Vector3(0.12f, 0.62f, 0f));
        Turned(Part(rig.legL, PrimitiveType.Cube, new Vector3(-0.16f, -0.3f, 0f), new Vector3(0.06f, 0.66f, 0.08f), joint, "Brace"), 0f, 0f, -28f);
        Turned(Part(rig.legR, PrimitiveType.Cube, new Vector3(0.16f, -0.3f, 0f), new Vector3(0.06f, 0.66f, 0.08f), joint, "Brace"), 0f, 0f, 28f);

        rig.torso = Pivot(root, "Torso", new Vector3(0f, 1.18f, 0f));
        rig.torsoRest = rig.torso.localPosition;
        plates.Add(Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0f, 0.02f), new Vector3(0.5f, 0.32f, 0.62f), plate, "Housing"));
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0f, 0.02f), new Vector3(0.52f, 0.07f, 0.64f), joint, "HousingBand");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, -0.2f, 0f), new Vector3(0.2f, 0.1f, 0.2f), joint, "Gimbal");
        rig.status = Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.19f, 0.05f), new Vector3(0.42f, 0.05f, 0.12f), lamp, "Lightbar");
        for (int s = -1; s <= 1; s += 2)
            Part(rig.torso, PrimitiveType.Cube, new Vector3(s * 0.253f, 0.06f, -0.14f), new Vector3(0.012f, 0.1f, 0.1f), white, "Check");
        NamePlate(rig.torso, new Vector3(0f, 0.1f, 0.333f), 0.3f, EnemyKind.Sentry);
        rig.armL = Pivot(rig.torso, "ArmL", new Vector3(-0.14f, -0.02f, 0.3f));
        rig.armR = Pivot(rig.torso, "ArmR", new Vector3(0.14f, -0.02f, 0.3f));
        foreach (var arm in new[] { rig.armL, rig.armR })
        {
            Turned(Part(arm, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.3f), new Vector3(0.09f, 0.32f, 0.09f), dark, "Barrel"), 90f);
            Turned(Part(arm, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.62f), new Vector3(0.12f, 0.02f, 0.12f), joint, "MuzzleRing"), 90f);
        }
        rig.muzzle = Pivot(rig.torso, "Muzzle", new Vector3(0f, -0.02f, 0.95f));

        // The coolant cell out of the back of it, fins either side
        rig.headPivot = Pivot(rig.torso, "HeadPivot", new Vector3(0f, 0.02f, -0.46f));
        Weakpoint(rig, rig.headPivot, PrimitiveType.Cube, new Vector3(0.2f, 0.2f, 0.32f), Quaternion.identity);
        for (int s = -1; s <= 1; s += 2)
            Part(rig.ring, PrimitiveType.Cube, new Vector3(s * 0.14f, 0f, 0f), new Vector3(0.025f, 0.28f, 0.3f), dark, "Fin");
        rig.plates = plates.ToArray();
        return rig;
    }
}
