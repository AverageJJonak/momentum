using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The variety's bodies (EnemyVariety), CLOCKWORK MANNEQUINS since the redesign
/// (EnemyMannequin): enamel shells on brass ball joints, a wind-up key, the cyan
/// weakpoint in a GLASS CELL where it is a globe (the busboy's cell, the valet's
/// eye, the moth's lantern, the usher's eye) or in a brass cage of its own shape
/// where it is not (the officer's lamp, the bouncer's core, the manager's lens,
/// the sweeper's tank). Every one keeps the six pivots the ragdoll wants (torso,
/// head, two arms, two legs), one clear hitbox and the weakpoint exactly where
/// and as big as it was. No two alike. The bracketed numbers are renderers.
/// </summary>
public partial class Enemy
{
    static Rig BuildVarietyRig(Transform root, EnemyKind kind, Material plate)
    {
        switch (kind)
        {
            case EnemyKind.Skitter: return BuildSkitterRig(root, plate);
            case EnemyKind.Marshal: return BuildMarshalRig(root, plate);
            case EnemyKind.Warden: return BuildWardenRig(root, plate);
            case EnemyKind.Lurker: return BuildLurkerRig(root, plate);
            case EnemyKind.Moth: return BuildMothRig(root, plate);
            case EnemyKind.Ocular: return BuildOcularRig(root, plate);
            case EnemyKind.Overseer: return BuildOverseerRig(root, plate);
            default: return BuildScourerRig(root, plate);
        }
    }

    /// One clear body collider: the hitbox everything but the weakpoint answers to
    static void Capsule(Transform root, Vector3 centre, float radius, float height)
    {
        var hitbox = new GameObject("Hitbox");
        hitbox.transform.SetParent(root, false);
        var c = hitbox.AddComponent<CapsuleCollider>();
        c.center = centre;
        c.radius = radius;
        c.height = height;
    }

    static void Box(Transform root, Vector3 centre, Vector3 size)
    {
        var hitbox = new GameObject("Hitbox");
        hitbox.transform.SetParent(root, false);
        var b = hitbox.AddComponent<BoxCollider>();
        b.center = centre;
        b.size = size;
    }

    /// THE BUSBOY (Skitter): a cream dish cover on a brass tray, on three brass stilts, two serving forks in front,
    /// and its cell in a glass bell in a square brass basket on its back. Knee-high. [25]
    static Rig BuildSkitterRig(Transform root, Material plate)
    {
        var rig = new Rig();
        var plates = new List<Renderer>();
        Material joint = JointMat();
        Capsule(root, new Vector3(0f, 0.5f, 0f), 0.3f, 1f);

        Part(root, PrimitiveType.Sphere, new Vector3(0f, 0.5f, -0.06f), Vector3.one * 0.16f, joint, "Pelvis");
        rig.legL = SkitterLeg(root, "LegL", new Vector3(-0.14f, 0.5f, -0.12f), -1f, joint);
        rig.legR = SkitterLeg(root, "LegR", new Vector3(0.14f, 0.5f, -0.12f), 1f, joint);
        SkitterLeg(root, "LegB", new Vector3(0f, 0.5f, 0.16f), 0f, joint);   // the third stilt rides the pelvis in the fall

        rig.torso = Pivot(root, "Torso", new Vector3(0f, 0.6f, 0f));
        rig.torsoRest = rig.torso.localPosition;
        plates.Add(Part(rig.torso, PrimitiveType.Sphere, new Vector3(0f, 0.02f, 0f), new Vector3(0.36f, 0.22f, 0.44f), plate, "Shell"));   // the cloche
        Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0f, -0.08f, 0f), new Vector3(0.42f, 0.008f, 0.5f), joint, "Tray");
        rig.status = Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.02f, 0.215f), new Vector3(0.14f, 0.04f, 0.02f), ResponseLamp(), "Lamp");
        rig.armL = Pivot(rig.torso, "ArmL", new Vector3(-0.09f, -0.03f, 0.2f));
        rig.armR = Pivot(rig.torso, "ArmR", new Vector3(0.09f, -0.03f, 0.2f));
        Part(rig.armL, PrimitiveType.Cube, new Vector3(-0.03f, 0f, 0.11f), new Vector3(0.035f, 0.05f, 0.24f), joint, "Fork");
        Part(rig.armR, PrimitiveType.Cube, new Vector3(0.03f, 0f, 0.11f), new Vector3(0.035f, 0.05f, 0.24f), joint, "Fork");
        rig.muzzle = Pivot(rig.torso, "Bite", new Vector3(0f, -0.03f, 0.32f));
        Key(rig, rig.torso, new Vector3(0f, -0.02f, -0.21f), 0.1f);

        // THE CELL: the glass bell in a square brass basket on its back
        rig.headPivot = Pivot(rig.torso, "HeadPivot", new Vector3(0f, 0.2f, -0.02f));
        GlassCage(rig, rig.headPivot, 0.24f);
        for (int s = -1; s <= 1; s += 2)
        {
            Part(rig.ring, PrimitiveType.Cube, new Vector3(s * 0.15f, 0f, 0f), new Vector3(0.02f, 0.02f, 0.32f), joint, "Basket");
            Part(rig.ring, PrimitiveType.Cube, new Vector3(0f, 0f, s * 0.15f), new Vector3(0.32f, 0.02f, 0.02f), joint, "Basket");
        }
        Part(rig.ring, PrimitiveType.Cylinder, new Vector3(0f, -0.13f, 0f), new Vector3(0.14f, 0.015f, 0.14f), joint, "Collar");
        rig.plates = plates.ToArray();
        return rig;
    }

    /// A stilt: the brass ball it hangs from, the brass rod, a lacquer foot [3]
    static Transform SkitterLeg(Transform root, string name, Vector3 at, float side, Material joint)
    {
        Transform hip = Pivot(root, name, at);
        float splay = side == 0f ? 0f : side * 26f;
        float back = side == 0f ? -22f : 8f;
        Turned(Part(hip, PrimitiveType.Cylinder, new Vector3(side * 0.09f, -0.25f, side == 0f ? 0.08f : 0f), new Vector3(0.035f, 0.25f, 0.035f), joint, "Stilt"), back, 0f, -splay);
        Ball(hip, Vector3.zero, 0.08f, "Joint");
        Part(hip, PrimitiveType.Cube, new Vector3(side * 0.2f, -0.49f, side == 0f ? 0.18f : 0f), new Vector3(0.08f, 0.03f, 0.1f), Satin(), "Foot");
        return hip;
    }

    /// THE HOUSE OFFICER (Marshal): a tall mannequin in black lacquer on the steward's legs, a long gun for a right arm
    /// and, where the head would be, the WARRANT LAMP: a cyan column on a brass mast under a hood, three rails round it.
    /// [34 + cap, waistcoat, buttons = 40]
    static Rig BuildMarshalRig(Transform root, Material plate)
    {
        var rig = new Rig();
        var plates = new List<Renderer>();
        Material joint = JointMat();
        Capsule(root, new Vector3(0f, 0.9f, 0f), 0.3f, 1.5f);

        plates.Add(Part(root, PrimitiveType.Sphere, new Vector3(0f, 0.99f, 0f), new Vector3(0.36f, 0.2f, 0.26f), plate, "Pelvis"));
        rig.legL = Leg(root, -0.12f, plate, plates);
        rig.legR = Leg(root, 0.12f, plate, plates);

        rig.torso = Pivot(root, "Torso", new Vector3(0f, 1.15f, 0f));
        rig.torsoRest = rig.torso.localPosition;
        plates.Add(Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0f), new Vector3(0.5f, 0.44f, 0.3f), plate, "Chest"));
        Key(rig, rig.torso, new Vector3(0f, 0.2f, -0.15f), 0.16f);
        rig.status = Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.3f, 0.152f), new Vector3(0.16f, 0.04f, 0.012f), ResponseLamp(), "StatusLight");
        NamePlate(rig.torso, new Vector3(0f, 0.0f, 0.156f), 0.22f, EnemyKind.Marshal);
        rig.armL = Arm(rig.torso, -0.3f, plate, plates, EnemyKind.Marshal, false, out _);
        // The gun: a long brass barrel down the right arm, its muzzle at the end
        rig.armR = Pivot(rig.torso, "ArmR", new Vector3(0.3f, 0.32f, 0f));
        Ball(rig.armR, Vector3.zero, 0.19f, "Shoulder");
        plates.Add(Shell(rig.armR, new Vector3(0f, -0.18f, 0f), new Vector3(0.12f, 0.3f, 0.12f), plate, "UpperArm"));
        Ball(rig.armR, new Vector3(0f, -0.36f, 0f), 0.14f, "Elbow");
        Part(rig.armR, PrimitiveType.Cube, new Vector3(0f, -0.56f, 0.02f), new Vector3(0.16f, 0.36f, 0.2f), Satin(), "Receiver");
        Part(rig.armR, PrimitiveType.Cylinder, new Vector3(0f, -0.86f, 0.04f), new Vector3(0.07f, 0.26f, 0.07f), joint, "Barrel");
        Part(rig.armR, PrimitiveType.Cylinder, new Vector3(0f, -1.1f, 0.04f), new Vector3(0.1f, 0.012f, 0.1f), ResponseLamp(), "MuzzleRing");
        rig.muzzle = Pivot(rig.armR, "Muzzle", new Vector3(0f, -1.14f, 0.04f));

        // THE WARRANT LAMP (its shape and place unchanged)
        rig.headPivot = Pivot(rig.torso, "HeadPivot", new Vector3(0f, 0.62f, 0f));
        Part(rig.headPivot, PrimitiveType.Cylinder, new Vector3(0f, -0.14f, 0f), new Vector3(0.06f, 0.08f, 0.06f), joint, "Mast");
        Weakpoint(rig, rig.headPivot, PrimitiveType.Cylinder, new Vector3(0.16f, 0.13f, 0.16f), Quaternion.identity);
        for (int i = 0; i < 3; i++)
        {
            float a = i * 120f * Mathf.Deg2Rad;
            Part(rig.ring, PrimitiveType.Cube, new Vector3(Mathf.Sin(a) * 0.13f, 0f, Mathf.Cos(a) * 0.13f), new Vector3(0.025f, 0.3f, 0.025f), joint, "Rail");
        }
        Part(rig.ring, PrimitiveType.Cylinder, new Vector3(0f, 0.17f, 0f), new Vector3(0.34f, 0.02f, 0.34f), Satin(), "Hood");
        rig.plates = plates.ToArray();
        return rig;
    }

    /// THE BOUNCER (Warden): broad, bronze, on the steward's legs with iron balls for shoulders; a steel GUARD on the
    /// left arm held square in front, a lance on the right, a dark visor in an iron helm where a face would be, and its
    /// CORE, cyan, between two bars on its BACK. The key is in the small of its back. [34 + tie, waistcoat, buttons = 38]
    static Rig BuildWardenRig(Transform root, Material plate)
    {
        var rig = new Rig();
        var plates = new List<Renderer>();
        Material joint = JointMat(), steel = GuardMat();
        Capsule(root, new Vector3(0f, 0.9f, 0f), 0.36f, 1.5f);

        plates.Add(Part(root, PrimitiveType.Sphere, new Vector3(0f, 0.99f, 0f), new Vector3(0.44f, 0.22f, 0.3f), plate, "Pelvis"));
        Key(rig, root, new Vector3(0f, 0.99f, -0.13f), 0.14f);
        rig.legL = Leg(root, -0.14f, plate, plates);
        rig.legR = Leg(root, 0.14f, plate, plates);

        rig.torso = Pivot(root, "Torso", new Vector3(0f, 1.15f, 0f));
        rig.torsoRest = rig.torso.localPosition;
        plates.Add(Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0f), new Vector3(0.6f, 0.46f, 0.34f), plate, "Chest"));
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.46f, 0.02f), new Vector3(0.4f, 0.14f, 0.3f), steel, "Helm");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.45f, 0.17f), new Vector3(0.3f, 0.05f, 0.03f), VisorMat(), "Visor");
        rig.status = Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.3f, 0.175f), new Vector3(0.18f, 0.04f, 0.012f), ResponseLamp(), "StatusLight");
        NamePlate(rig.torso, new Vector3(0f, 0.0f, 0.176f), 0.26f, EnemyKind.Warden);
        Part(rig.torso, PrimitiveType.Sphere, new Vector3(-0.38f, 0.36f, 0f), Vector3.one * 0.24f, steel, "ShoulderL");   // iron shoulders
        Part(rig.torso, PrimitiveType.Sphere, new Vector3(0.38f, 0.36f, 0f), Vector3.one * 0.24f, steel, "ShoulderR");

        // THE GUARD: the left arm, and the plate it holds out in front, square to you
        rig.armL = Pivot(rig.torso, "ArmL", new Vector3(-0.38f, 0.32f, 0f));
        plates.Add(Shell(rig.armL, new Vector3(0f, -0.18f, 0.06f), new Vector3(0.13f, 0.3f, 0.13f), plate, "UpperArm"));
        Part(rig.armL, PrimitiveType.Cube, new Vector3(0.08f, -0.3f, 0.3f), new Vector3(0.11f, 0.11f, 0.4f), joint, "Forearm");
        Part(rig.armL, PrimitiveType.Cube, new Vector3(0.2f, -0.3f, 0.5f), new Vector3(0.7f, 0.9f, 0.06f), steel, "Guard");
        Part(rig.armL, PrimitiveType.Cube, new Vector3(0.2f, -0.3f, 0.535f), new Vector3(0.5f, 0.7f, 0.01f), plate, "GuardFace");
        Part(rig.armL, PrimitiveType.Cube, new Vector3(0.2f, 0.14f, 0.5f), new Vector3(0.72f, 0.03f, 0.08f), joint, "GuardRim");
        // THE LANCE: the right arm, the shaft along it
        rig.armR = Pivot(rig.torso, "ArmR", new Vector3(0.38f, 0.32f, 0f));
        plates.Add(Shell(rig.armR, new Vector3(0f, -0.18f, 0f), new Vector3(0.13f, 0.3f, 0.13f), plate, "UpperArm"));
        Ball(rig.armR, new Vector3(0f, -0.36f, 0f), 0.15f, "Elbow");
        plates.Add(Shell(rig.armR, new Vector3(0f, -0.54f, 0f), new Vector3(0.12f, 0.3f, 0.12f), plate, "Forearm"));
        Part(rig.armR, PrimitiveType.Cylinder, new Vector3(0f, -0.9f, 0f), new Vector3(0.05f, 0.6f, 0.05f), joint, "Lance");
        Part(rig.armR, PrimitiveType.Cube, new Vector3(0f, -1.55f, 0f), new Vector3(0.09f, 0.22f, 0.02f), steel, "LanceHead");
        rig.muzzle = Pivot(rig.armR, "Point", new Vector3(0f, -1.66f, 0f));

        // THE CORE: on its back, between two brass bars (unchanged)
        rig.headPivot = Pivot(rig.torso, "HeadPivot", new Vector3(0f, 0.24f, -0.27f));
        Weakpoint(rig, rig.headPivot, PrimitiveType.Cube, new Vector3(0.22f, 0.22f, 0.14f), Quaternion.Euler(0f, 0f, 45f));
        for (int s = -1; s <= 1; s += 2)
            Part(rig.ring, PrimitiveType.Cube, new Vector3(s * 0.2f, 0f, 0f), new Vector3(0.04f, 0.4f, 0.12f), joint, "CoreBar");
        rig.plates = plates.ToArray();
        return rig;
    }

    /// THE VALET (Lurker): a flat soot-black shell on four splayed brass-jointed legs, serving forks for mandibles, and
    /// the EYE CLUSTER at the front: the glass cell under a brow, four dark lenses round it. [32 + tie, buttons, tails = 37]
    static Rig BuildLurkerRig(Transform root, Material plate)
    {
        var rig = new Rig();
        var plates = new List<Renderer>();
        Material joint = JointMat();
        Box(root, new Vector3(0f, 0.42f, 0f), new Vector3(0.8f, 0.5f, 1f));

        plates.Add(Part(root, PrimitiveType.Sphere, new Vector3(0f, 0.42f, -0.3f), new Vector3(0.3f, 0.18f, 0.26f), plate, "Pelvis"));
        rig.legL = LurkerLeg(root, "LegL", new Vector3(-0.26f, 0.42f, -0.25f), -1f, joint, plate, plates);
        rig.legR = LurkerLeg(root, "LegR", new Vector3(0.26f, 0.42f, -0.25f), 1f, joint, plate, plates);

        rig.torso = Pivot(root, "Torso", new Vector3(0f, 0.45f, 0f));
        rig.torsoRest = rig.torso.localPosition;
        plates.Add(ShellZ(rig.torso, new Vector3(0f, 0f, 0.05f), new Vector3(0.5f, 0.8f, 0.22f), plate, "Body"));
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, -0.13f, 0.05f), new Vector3(0.36f, 0.06f, 0.6f), Satin(), "Belly");
        rig.status = Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.12f, -0.2f), new Vector3(0.1f, 0.03f, 0.3f), ResponseLamp(), "Lamp");
        rig.armL = LurkerLeg(rig.torso, "ArmL", new Vector3(-0.26f, -0.03f, 0.25f), -1f, joint, plate, plates);
        rig.armR = LurkerLeg(rig.torso, "ArmR", new Vector3(0.26f, -0.03f, 0.25f), 1f, joint, plate, plates);
        Part(rig.torso, PrimitiveType.Cube, new Vector3(-0.1f, -0.06f, 0.5f), new Vector3(0.04f, 0.05f, 0.26f), joint, "Fork");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0.1f, -0.06f, 0.5f), new Vector3(0.04f, 0.05f, 0.26f), joint, "Fork");
        rig.muzzle = Pivot(rig.torso, "Bite", new Vector3(0f, -0.05f, 0.62f));
        Key(rig, rig.torso, new Vector3(0f, 0.1f, -0.1f), 0.12f, Vector3.up);   // out of the top of the shell

        // THE EYE CLUSTER: the glass cell at the front under a brow, four dark lenses round it
        rig.headPivot = Pivot(rig.torso, "HeadPivot", new Vector3(0f, 0.06f, 0.4f));
        GlassCage(rig, rig.headPivot, 0.22f);
        Part(rig.ring, PrimitiveType.Cube, new Vector3(0f, 0.15f, -0.04f), new Vector3(0.36f, 0.05f, 0.16f), Satin(), "Brow");
        for (int i = 0; i < 4; i++)
        {
            float a = (45f + i * 90f) * Mathf.Deg2Rad;
            Part(rig.ring, PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * 0.17f, Mathf.Sin(a) * 0.12f, 0.06f), Vector3.one * 0.07f, VisorMat(), "Lens");
        }
        rig.plates = plates.ToArray();
        return rig;
    }

    /// A crawler's leg: the upper shell (a plate), the brass joint, the brass lower rod, a lacquer foot [4]
    static Transform LurkerLeg(Transform parent, string name, Vector3 at, float side, Material joint, Material plate, List<Renderer> plates)
    {
        Transform hip = Pivot(parent, name, at);
        plates.Add(Turned(Part(hip, PrimitiveType.Capsule, new Vector3(side * 0.16f, 0.06f, 0f), new Vector3(0.09f, 0.18f, 0.09f), plate, "Upper"), 0f, 0f, side * 28f + 90f));
        Ball(hip, new Vector3(side * 0.3f, 0.14f, 0f), 0.1f, "Joint");
        Turned(Part(hip, PrimitiveType.Cylinder, new Vector3(side * 0.36f, -0.12f, 0f), new Vector3(0.05f, 0.25f, 0.05f), joint, "Lower"), 0f, 0f, side * -12f);
        Part(hip, PrimitiveType.Cube, new Vector3(side * 0.41f, -0.38f, 0f), new Vector3(0.1f, 0.03f, 0.14f), Satin(), "Foot");
        return hip;
    }

    /// THE MOTH: a brass LAMP for a body — a drum with a glass lens on the nose and its amber lamp lit inside — two
    /// wide ivory wings on brass spars (its arms: the tell shows on them), a key out of its back, a tail, and the
    /// LANTERN hung under it in a hoop: the glass cell. Built round where a chest would be. [17 + pillbox, girdle = 19]
    static Rig BuildMothRig(Transform root, Material plate)
    {
        var rig = new Rig();
        var plates = new List<Renderer>();
        Material joint = JointMat();
        Box(root, new Vector3(0f, ChestHeight, 0f), new Vector3(1.2f, 0.4f, 0.7f));

        Part(root, PrimitiveType.Cube, new Vector3(0f, ChestHeight - 0.02f, -0.3f), new Vector3(0.1f, 0.08f, 0.2f), joint, "Pelvis");
        rig.legL = Pivot(root, "LegL", new Vector3(-0.04f, ChestHeight - 0.02f, -0.38f));
        rig.legR = Pivot(root, "LegR", new Vector3(0.04f, ChestHeight - 0.02f, -0.38f));
        Turned(Part(rig.legL, PrimitiveType.Cube, new Vector3(-0.04f, 0f, -0.14f), new Vector3(0.02f, 0.02f, 0.28f), joint, "Tail"), 0f, -12f, 0f);
        Turned(Part(rig.legR, PrimitiveType.Cube, new Vector3(0.04f, 0f, -0.14f), new Vector3(0.02f, 0.02f, 0.28f), joint, "Tail"), 0f, 12f, 0f);

        rig.torso = Pivot(root, "Torso", new Vector3(0f, ChestHeight, 0f));
        rig.torsoRest = rig.torso.localPosition;
        Turned(Part(rig.torso, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.24f, 0.32f, 0.24f), joint, "LampBody"), 90f);
        var lens = Part(rig.torso, PrimitiveType.Sphere, new Vector3(0f, 0f, 0.34f), Vector3.one * 0.22f, Glass(), "Lens");
        lens.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rig.status = Part(rig.torso, PrimitiveType.Sphere, new Vector3(0f, 0f, 0.34f), Vector3.one * 0.12f, ResponseLamp(), "Lamp");   // lit inside the lens
        Key(rig, rig.torso, new Vector3(0f, 0.12f, -0.02f), 0.1f, Vector3.up);
        // THE WINGS (its arms): thin, wide, ivory, on brass spars; the tell shows on them
        rig.armL = Pivot(rig.torso, "ArmL", new Vector3(-0.12f, 0.06f, 0f));
        rig.armR = Pivot(rig.torso, "ArmR", new Vector3(0.12f, 0.06f, 0f));
        plates.Add(Part(rig.armL, PrimitiveType.Cube, new Vector3(-0.34f, 0f, -0.02f), new Vector3(0.66f, 0.015f, 0.4f), plate, "Wing"));
        plates.Add(Part(rig.armR, PrimitiveType.Cube, new Vector3(0.34f, 0f, -0.02f), new Vector3(0.66f, 0.015f, 0.4f), plate, "Wing"));
        Part(rig.armL, PrimitiveType.Cube, new Vector3(-0.34f, 0.01f, 0.16f), new Vector3(0.6f, 0.012f, 0.03f), joint, "Spar");
        Part(rig.armR, PrimitiveType.Cube, new Vector3(0.34f, 0.01f, 0.16f), new Vector3(0.6f, 0.012f, 0.03f), joint, "Spar");
        // THE LANTERN: the glass cell hung under the body in a brass hoop
        rig.headPivot = Pivot(rig.torso, "HeadPivot", new Vector3(0f, -0.24f, 0.04f));
        Part(rig.headPivot, PrimitiveType.Cube, new Vector3(0f, 0.1f, 0f), new Vector3(0.03f, 0.1f, 0.03f), joint, "Hanger");
        GlassCage(rig, rig.headPivot, 0.22f);
        Part(rig.ring, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.32f, 0.012f, 0.32f), joint, "Hoop");
        rig.muzzle = rig.headPivot;
        rig.plates = plates.ToArray();
        return rig;
    }

    /// THE USHER (Ocular): a cream enamel ball of a body with two lacquer lids, four fins, a key in its back, and THE
    /// EYE, the glass cell in a brass iris at the front. The beam and its line hang off the root, dark until it charges.
    /// [17 + pillbox, torch = 21]
    static Rig BuildOcularRig(Transform root, Material plate)
    {
        var rig = new Rig();
        var plates = new List<Renderer>();
        Material joint = JointMat();
        Box(root, new Vector3(0f, ChestHeight, 0f), new Vector3(0.9f, 0.9f, 0.9f));

        Part(root, PrimitiveType.Cube, new Vector3(0f, ChestHeight - 0.42f, 0f), new Vector3(0.14f, 0.1f, 0.14f), joint, "Pelvis");
        rig.legL = Pivot(root, "LegL", new Vector3(-0.2f, ChestHeight - 0.36f, -0.1f));
        rig.legR = Pivot(root, "LegR", new Vector3(0.2f, ChestHeight - 0.36f, -0.1f));
        Turned(Part(rig.legL, PrimitiveType.Cube, new Vector3(-0.08f, -0.1f, 0f), new Vector3(0.16f, 0.24f, 0.03f), Satin(), "Fin"), 0f, 0f, 25f);
        Turned(Part(rig.legR, PrimitiveType.Cube, new Vector3(0.08f, -0.1f, 0f), new Vector3(0.16f, 0.24f, 0.03f), Satin(), "Fin"), 0f, 0f, -25f);

        rig.torso = Pivot(root, "Torso", new Vector3(0f, ChestHeight, 0f));
        rig.torsoRest = rig.torso.localPosition;
        plates.Add(Part(rig.torso, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.78f, plate, "Body"));
        rig.status = Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.4f, 0f), new Vector3(0.12f, 0.03f, 0.12f), ResponseLamp(), "Lamp");
        Key(rig, rig.torso, new Vector3(0f, 0.05f, -0.38f), 0.14f);
        rig.armL = Pivot(rig.torso, "ArmL", new Vector3(-0.38f, 0.1f, -0.1f));
        rig.armR = Pivot(rig.torso, "ArmR", new Vector3(0.38f, 0.1f, -0.1f));
        Turned(Part(rig.armL, PrimitiveType.Cube, new Vector3(-0.12f, 0f, 0f), new Vector3(0.28f, 0.03f, 0.2f), Satin(), "Fin"), 0f, 0f, 20f);
        Turned(Part(rig.armR, PrimitiveType.Cube, new Vector3(0.12f, 0f, 0f), new Vector3(0.28f, 0.03f, 0.2f), Satin(), "Fin"), 0f, 0f, -20f);
        // The lids: two plates over and under the eye that narrow it while it charges
        rig.coatL = Pivot(rig.torso, "LidUp", new Vector3(0f, 0.22f, 0.18f));
        rig.coatR = Pivot(rig.torso, "LidDown", new Vector3(0f, -0.22f, 0.18f));
        Turned(Part(rig.coatL, PrimitiveType.Cube, new Vector3(0f, 0.04f, 0.14f), new Vector3(0.5f, 0.05f, 0.3f), Satin(), "Lid"), -20f);
        Turned(Part(rig.coatR, PrimitiveType.Cube, new Vector3(0f, -0.04f, 0.14f), new Vector3(0.5f, 0.05f, 0.3f), Satin(), "Lid"), 20f);

        // THE EYE: the glass cell in a brass iris, at the front
        rig.headPivot = Pivot(rig.torso, "HeadPivot", new Vector3(0f, 0f, 0.3f));
        GlassCage(rig, rig.headPivot, 0.34f);
        Turned(Part(rig.ring, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.06f), new Vector3(0.46f, 0.02f, 0.46f), joint, "Iris"), 90f);
        rig.muzzle = Pivot(rig.headPivot, "Muzzle", new Vector3(0f, 0f, 0.18f));

        // The beam and its line, off the root; dark until it charges
        var line = Part(root, PrimitiveType.Cube, Vector3.zero, new Vector3(0.05f, 0.05f, 1f), BeamMat(), "BeamLine");
        var beam = Part(root, PrimitiveType.Cube, Vector3.zero, new Vector3(0.5f, 0.5f, 1f), BeamMat(), "Beam");
        line.enabled = false;
        beam.enabled = false;
        rig.plates = plates.ToArray();
        return rig;
    }

    /// THE FLOOR MANAGER (Overseer; the INSPECTOR in the Underground): a bone enamel disc with a brass rim on four
    /// lacquer fins, a lamp on top, a key out of the disc, THE LENS, cyan, underneath in four brass posts, and the cover
    /// light. [16 + tie, watch chain = 18]
    static Rig BuildOverseerRig(Transform root, Material plate)
    {
        var rig = new Rig();
        var plates = new List<Renderer>();
        Material joint = JointMat();
        Box(root, new Vector3(0f, ChestHeight, 0f), new Vector3(1.4f, 0.4f, 1.4f));

        Part(root, PrimitiveType.Cylinder, new Vector3(0f, ChestHeight - 0.1f, 0f), new Vector3(0.5f, 0.04f, 0.5f), joint, "Pelvis");
        rig.torso = Pivot(root, "Torso", new Vector3(0f, ChestHeight, 0f));
        rig.torsoRest = rig.torso.localPosition;
        plates.Add(Part(rig.torso, PrimitiveType.Cylinder, Vector3.zero, new Vector3(1.3f, 0.06f, 1.3f), plate, "Disc"));
        Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f), new Vector3(1.36f, 0.025f, 1.36f), joint, "Rim");
        Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0f, 0.1f, 0f), new Vector3(0.4f, 0.06f, 0.4f), Satin(), "Turret");
        rig.status = Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0f, 0.18f, 0f), new Vector3(0.2f, 0.03f, 0.2f), ResponseLamp(), "Lamp");
        Key(rig, rig.torso, new Vector3(0f, 0.03f, -0.3f), 0.14f, Vector3.up);
        rig.armL = Pivot(rig.torso, "ArmL", new Vector3(-0.6f, 0f, 0f));
        rig.armR = Pivot(rig.torso, "ArmR", new Vector3(0.6f, 0f, 0f));
        rig.legL = Pivot(rig.torso, "LegL", new Vector3(0f, 0f, -0.6f));
        rig.legR = Pivot(rig.torso, "LegR", new Vector3(0f, 0f, 0.6f));
        Turned(Part(rig.armL, PrimitiveType.Cube, new Vector3(-0.12f, -0.06f, 0f), new Vector3(0.24f, 0.03f, 0.3f), Satin(), "Fin"), 0f, 0f, -18f);
        Turned(Part(rig.armR, PrimitiveType.Cube, new Vector3(0.12f, -0.06f, 0f), new Vector3(0.24f, 0.03f, 0.3f), Satin(), "Fin"), 0f, 0f, 18f);
        Turned(Part(rig.legL, PrimitiveType.Cube, new Vector3(0f, -0.06f, -0.12f), new Vector3(0.3f, 0.03f, 0.24f), Satin(), "Fin"), 18f);
        Turned(Part(rig.legR, PrimitiveType.Cube, new Vector3(0f, -0.06f, 0.12f), new Vector3(0.3f, 0.03f, 0.24f), Satin(), "Fin"), -18f);

        // THE LENS: under the disc, looking down, in four brass posts (unchanged)
        rig.headPivot = Pivot(rig.torso, "HeadPivot", new Vector3(0f, -0.2f, 0f));
        Weakpoint(rig, rig.headPivot, PrimitiveType.Cylinder, new Vector3(0.4f, 0.05f, 0.4f), Quaternion.identity);
        for (int i = 0; i < 4; i++)
        {
            float a = (45f + i * 90f) * Mathf.Deg2Rad;
            Part(rig.ring, PrimitiveType.Cube, new Vector3(Mathf.Cos(a) * 0.28f, 0.08f, Mathf.Sin(a) * 0.28f), new Vector3(0.04f, 0.24f, 0.04f), joint, "Post");
        }
        rig.muzzle = rig.headPivot;
        // The cover light: amber, down onto whoever it covers
        var lg = new GameObject("CoverLight");
        lg.transform.SetParent(rig.headPivot, false);
        lg.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var light = lg.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = new Color(1f, 0.72f, 0.3f);
        light.intensity = 6f;
        light.range = 40f;
        light.spotAngle = 80f;
        light.shadows = LightShadows.None;
        rig.plates = plates.ToArray();
        return rig;
    }

    /// THE SWEEPER (Scourer): a heavy mannequin on the steward's legs with an iron hood and a visor, the nozzle on the
    /// right arm with its pilot lamp, and THE TANK, cyan, strapped to its back under a valve (so no key: the valve is
    /// where it winds). [33 + pillbox, apron, buttons = 37]
    static Rig BuildScourerRig(Transform root, Material plate)
    {
        var rig = new Rig();
        var plates = new List<Renderer>();
        Material joint = JointMat(), iron = WorksIron();
        Capsule(root, new Vector3(0f, 0.9f, 0f), 0.36f, 1.5f);

        plates.Add(Part(root, PrimitiveType.Sphere, new Vector3(0f, 0.99f, 0f), new Vector3(0.44f, 0.22f, 0.3f), plate, "Pelvis"));
        rig.legL = Leg(root, -0.15f, plate, plates);
        rig.legR = Leg(root, 0.15f, plate, plates);

        rig.torso = Pivot(root, "Torso", new Vector3(0f, 1.15f, 0f));
        rig.torsoRest = rig.torso.localPosition;
        plates.Add(Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0.02f), new Vector3(0.58f, 0.5f, 0.38f), plate, "Boiler"));
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.5f, 0.04f), new Vector3(0.36f, 0.16f, 0.32f), iron, "Hood");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.48f, 0.2f), new Vector3(0.28f, 0.05f, 0.03f), VisorMat(), "Visor");
        NamePlate(rig.torso, new Vector3(0f, -0.02f, 0.215f), 0.26f, EnemyKind.Scourer);
        rig.armL = Arm(rig.torso, -0.36f, plate, plates, EnemyKind.Scourer, false, out _);
        // THE NOZZLE: down the right arm and forward off the hand, the pilot lamp on it
        rig.armR = Pivot(rig.torso, "ArmR", new Vector3(0.36f, 0.32f, 0f));
        Ball(rig.armR, Vector3.zero, 0.2f, "Shoulder");
        plates.Add(Shell(rig.armR, new Vector3(0f, -0.18f, 0f), new Vector3(0.14f, 0.3f, 0.14f), plate, "UpperArm"));
        Ball(rig.armR, new Vector3(0f, -0.36f, 0f), 0.15f, "Elbow");
        Part(rig.armR, PrimitiveType.Cube, new Vector3(0f, -0.54f, 0.04f), new Vector3(0.16f, 0.3f, 0.2f), iron, "Forearm");
        Turned(Part(rig.armR, PrimitiveType.Cylinder, new Vector3(0f, -0.56f, 0.42f), new Vector3(0.1f, 0.3f, 0.1f), joint, "Nozzle"), 90f);
        Turned(Part(rig.armR, PrimitiveType.Cylinder, new Vector3(0f, -0.56f, 0.72f), new Vector3(0.14f, 0.02f, 0.14f), iron, "NozzleRing"), 90f);
        rig.status = Part(rig.armR, PrimitiveType.Sphere, new Vector3(0f, -0.46f, 0.6f), Vector3.one * 0.06f, ResponseLamp(), "PilotLamp");
        Part(rig.armR, PrimitiveType.Cylinder, new Vector3(0.06f, -0.4f, 0.1f), new Vector3(0.03f, 0.22f, 0.03f), joint, "Hose");
        rig.muzzle = Pivot(rig.armR, "Muzzle", new Vector3(0f, -0.56f, 0.76f));

        // THE TANK: cyan, on its back, two brass straps over it and a valve on top (unchanged)
        rig.headPivot = Pivot(rig.torso, "HeadPivot", new Vector3(0f, 0.24f, -0.34f));
        Weakpoint(rig, rig.headPivot, PrimitiveType.Cylinder, new Vector3(0.22f, 0.26f, 0.22f), Quaternion.identity);
        for (int s = -1; s <= 1; s += 2)
            Part(rig.ring, PrimitiveType.Cube, new Vector3(0f, s * 0.12f, 0f), new Vector3(0.3f, 0.03f, 0.3f), joint, "Strap");
        Part(rig.ring, PrimitiveType.Cylinder, new Vector3(0f, 0.3f, 0f), new Vector3(0.08f, 0.04f, 0.08f), joint, "Valve");
        // The flame, in eight pieces off the root, dark until it fires
        for (int i = 0; i < 8; i++)
        {
            var f = Part(root, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.4f, FlameMat(), "Flame" + i);
            f.enabled = false;
            f.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        rig.plates = plates.ToArray();
        return rig;
    }
}
