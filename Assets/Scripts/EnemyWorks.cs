using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HALCYON WORKS — the track gang of the Underground: the platelayer (Borer, a
/// low iron frame on rail bogies with a rotary pick), the signalman (Rigger,
/// tall on stilts with flags and a lantern arm), the ganger (Blaster, with the
/// detonator). Since the simplification (2026-10-16) only their BODIES live
/// here: what they DO is a role in EnemyRoles — the signalman and the ganger
/// are SHOOTERS (they were the ranged crew), the platelayer a footman that
/// rolls its line. All three ride the Enemy skeleton (torso, head, arms, legs).
/// </summary>
public partial class Enemy
{
    // THE TRACK GANG: the staff roles the crew read as since the Underground (EnemyLivery.StaffName); the code names never change
    public const string BorerName = "PLATELAYER", RiggerName = "SIGNALMAN", BlasterName = "GANGER";

    static bool IsWorks(EnemyKind k) => k == EnemyKind.Borer || k == EnemyKind.Rigger || k == EnemyKind.Blaster;

    static Material worksPlate, worksBlack, worksLamp, worksDrill, worksRed, worksIron, worksMustard;
    static Material WorksPlate() => worksPlate ??= Lit(new Color(0.35f, 0.24f, 0.12f), 0.6f, 0.5f);     // the plant's plate: dark bronze
    static Material WorksBlack() => worksBlack ??= Lit(new Color(0.12f, 0.12f, 0.12f), 0.2f, 0.35f);   // the stripe: black
    static Material WorksMustard() => worksMustard ??= Lit(new Color(0.7f, 0.55f, 0.2f), 0.2f, 0.4f);  // the hazard bands: mustard
    static Material WorksLamp() => worksLamp ??= Unlit(new Color(2.2f, 1.3f, 0.3f));                  // amber: a unit's own light
    static Material WorksDrill() => worksDrill ??= Lit(new Color(0.6f, 0.6f, 0.62f), 0.9f, 0.55f);
    static Material WorksRed() => worksRed ??= Unlit(new Color(2.4f, 0.25f, 0.15f));                   // red: the ganger's charges in his hopper (look only)
    static Material WorksIron() => worksIron ??= Lit(new Color(0.16f, 0.16f, 0.18f), 0.6f, 0.5f);     // iron

    public static Enemy CreateBorer(Vector3 groundPos, MovementConfig config)
    {
        var e = Create(EnemyKind.Borer, groundPos, config);
        if (e == null) return null;   // no units (Enemy.Units): nothing to shape
        e.transform.localScale = Vector3.one * 1.4f;
        return e;
    }

    public static Enemy CreateRigger(Vector3 groundPos, MovementConfig config)
    {
        var e = Create(EnemyKind.Rigger, groundPos, config);
        if (e == null) return null;   // no units (Enemy.Units): nothing to shape
        e.transform.localScale = Vector3.one * 1.25f;
        return e;
    }

    public static Enemy CreateBlaster(Vector3 groundPos, MovementConfig config)
    {
        var e = Create(EnemyKind.Blaster, groundPos, config);
        if (e == null) return null;   // no units (Enemy.Units): nothing to shape
        e.transform.localScale = Vector3.one * 1.1f;
        return e;
    }

    // ---------------------------------------------------------------- the rigs

    static Rig BuildWorksRig(Transform root, EnemyKind kind, Material plate)
    {
        switch (kind)
        {
            case EnemyKind.Borer: return BuildBorerRig(root, plate);
            case EnemyKind.Rigger: return BuildRiggerRig(root, plate);
            default: return BuildBlasterRig(root, plate);
        }
    }

    /// The standard weakspot: a cyan sphere with its halo, on a pivot. Out in the district (Chapter 3) the crew carry the
    /// response's issue instead: no halo — a caged lamp, its core a cyan capsule behind three guard bars, open at the front
    static void WorksHead(Rig rig, Transform pivot, float size)
    {
        if (LevelKit.Current == Zone.Surface)
        {
            Weakpoint(rig, pivot, PrimitiveType.Capsule, new Vector3(size * 0.78f, size * 0.52f, size * 0.78f), Quaternion.identity);
            for (int i = 0; i < 3; i++)
            {
                float a = (i * 120f + 60f) * Mathf.Deg2Rad;
                Part(rig.ring, PrimitiveType.Cube, new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * (size * 0.52f), new Vector3(0.035f, size * 1.15f, 0.035f), JointMat(), "GuardBar");
            }
            Part(rig.ring, PrimitiveType.Cylinder, new Vector3(0f, -size * 0.54f, 0f), new Vector3(size * 0.85f, 0.02f, size * 0.85f), JointMat(), "LampBase");
            return;
        }
        GlassHead(rig, pivot, size, -0.4f, 0.8f, 2.1f);   // the glass head: the same sphere, collider and halo, the escapement inside (EnemyMannequin)
    }

    static Rig BuildBorerRig(Transform root, Material plate)
    {
        var rig = new Rig();
        var plates = new List<Renderer>();
        Material black = WorksBlack(), joint = JointMat(), lamp = WorksLamp(), drill = WorksDrill(), iron = WorksIron();

        var hitbox = new GameObject("Hitbox");
        hitbox.transform.SetParent(root, false);
        var box = hitbox.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.75f, 0.1f);
        box.size = new Vector3(1.5f, 1.3f, 2.4f);

        // THE PLATELAYER rides RAIL BOGIES: a bronze frame beam either side (the plate) on three flanged brass wheels
        // with an iron axle bar through them   [5 × 2]
        rig.legL = Pivot(root, "TrackL", new Vector3(-0.62f, 0.32f, 0f));
        rig.legR = Pivot(root, "TrackR", new Vector3(0.62f, 0.32f, 0f));
        foreach (var leg in new[] { rig.legL, rig.legR })
        {
            plates.Add(Part(leg, PrimitiveType.Cube, new Vector3(0f, 0.1f, 0f), new Vector3(0.3f, 0.26f, 1.9f), plate, "Frame"));
            Part(leg, PrimitiveType.Cube, new Vector3(0f, -0.14f, 0f), new Vector3(0.1f, 0.1f, 1.5f), iron, "Axle");
            for (int i = 0; i < 3; i++)
                Part(leg, PrimitiveType.Cylinder, new Vector3(0f, -0.14f, -0.6f + i * 0.6f), new Vector3(0.36f, 0.06f, 0.36f), joint, "Wheel").transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }
        Part(root, PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 0.3f, 1.4f), iron, "Pelvis");   // [1]

        // The low iron frame: the hull with its hazard band, the cab with its lamps and the key out of its side, exhaust pipes   [10]
        rig.torso = Pivot(root, "Torso", new Vector3(0f, 0.6f, 0f));
        rig.torsoRest = rig.torso.localPosition;
        plates.Add(Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.3f, -0.1f), new Vector3(1f, 0.6f, 1.7f), plate, "Hull"));
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.31f, -0.1f), new Vector3(1.03f, 0.12f, 1.72f), WorksMustard(), "HullBand");
        plates.Add(Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.85f, -0.45f), new Vector3(0.7f, 0.5f, 0.7f), plate, "Cab"));
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.9f, -0.09f), new Vector3(0.6f, 0.3f, 0.02f), VisorMat(), "Windscreen");
        rig.status = Part(rig.torso, PrimitiveType.Cube, new Vector3(-0.22f, 1.14f, -0.2f), new Vector3(0.14f, 0.08f, 0.14f), lamp, "LampL");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0.22f, 1.14f, -0.2f), new Vector3(0.14f, 0.08f, 0.14f), lamp, "LampR");
        Part(rig.torso, PrimitiveType.Cylinder, new Vector3(-0.42f, 0.7f, 0.2f), new Vector3(0.1f, 0.5f, 0.1f), joint, "PipeL").transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0.42f, 0.7f, 0.2f), new Vector3(0.1f, 0.5f, 0.1f), joint, "PipeR").transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Key(rig, rig.torso, new Vector3(0.35f, 0.85f, -0.45f), 0.24f, Vector3.right);

        // THE PICK out the front (armR, the drill): a boom and a rotary pick head on it — a brass hub with three steel
        // spokes, six points, that spins (bDrillSpin); a hydraulic ram beside it (armL)   [5 + 1]
        rig.armR = Pivot(rig.torso, "DrillArm", new Vector3(0f, 0.35f, 0.75f));
        Part(rig.armR, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.25f), new Vector3(0.28f, 0.25f, 0.28f), iron, "Boom").transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        rig.drill = Pivot(rig.armR, "Drill", new Vector3(0f, 0f, 0.5f));
        Part(rig.drill, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.3f), new Vector3(0.3f, 0.14f, 0.3f), joint, "PickHub").transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        for (int i = 0; i < 3; i++)
            Part(rig.drill, PrimitiveType.Cube, new Vector3(0f, 0f, 0.48f), new Vector3(0.05f, 0.78f, 0.06f), drill, "PickSpoke").transform.localRotation = Quaternion.Euler(0f, 0f, i * 60f);
        rig.muzzle = Pivot(rig.drill, "DrillTip", new Vector3(0f, 0f, 0.95f));
        rig.armL = Pivot(rig.torso, "Ram", new Vector3(-0.35f, 0.5f, 0.45f));
        Part(rig.armL, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.15f), new Vector3(0.08f, 0.3f, 0.08f), joint, "RamRod").transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        // The exhaust stack at the back: the glass head in it is the weakspot, under a lid that blows open when it jams   [1 + 5 + 1]
        rig.headPivot = Pivot(rig.torso, "Stack", new Vector3(0f, 1.05f, -0.72f));
        Part(rig.headPivot, PrimitiveType.Cylinder, new Vector3(0f, -0.2f, 0f), new Vector3(0.36f, 0.25f, 0.36f), iron, "Chimney");
        WorksHead(rig, rig.headPivot, 0.34f);
        rig.hatch = Pivot(rig.headPivot, "Lid", new Vector3(0f, 0.22f, -0.2f));
        Part(rig.hatch, PrimitiveType.Cube, new Vector3(0f, 0f, 0.2f), new Vector3(0.44f, 0.05f, 0.44f), black, "LidPlate");

        // The charge lane: a red slab laid on the floor while it lines you up; hidden otherwise, never a ragdoll piece
        rig.lane = Part(root, PrimitiveType.Cube, Vector3.zero, Vector3.one, WorksRed(), "Lane");
        rig.lane.enabled = false;
        rig.plates = plates.ToArray();
        return rig;
    }

    static Rig BuildRiggerRig(Transform root, Material plate)
    {
        var rig = new Rig();
        var plates = new List<Renderer>();
        Material black = WorksBlack(), joint = JointMat(), lamp = WorksLamp(), iron = WorksIron();

        var hitbox = new GameObject("Hitbox");
        hitbox.transform.SetParent(root, false);
        var capsule = hitbox.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 1.3f, 0f);
        capsule.radius = 0.4f;
        capsule.height = 2.4f;

        // THE SIGNALMAN stands tall on stilt legs: a brass hip ball, the thigh shell (the plate), the knee ball, the brass
        // stilt, a lacquer foot; the key in the small of its back   [1 + 5 × 2 + 2]
        Part(root, PrimitiveType.Cube, new Vector3(0f, 1.05f, 0f), new Vector3(0.7f, 0.18f, 0.4f), iron, "Pelvis");
        Key(rig, root, new Vector3(0f, 1.05f, -0.2f), 0.18f);
        rig.legL = Pivot(root, "LegL", new Vector3(-0.28f, 1.0f, 0f));
        rig.legR = Pivot(root, "LegR", new Vector3(0.28f, 1.0f, 0f));
        foreach (var leg in new[] { rig.legL, rig.legR })
        {
            Ball(leg, Vector3.zero, 0.2f, "Hip");
            plates.Add(Shell(leg, new Vector3(0f, -0.25f, 0.02f), new Vector3(0.2f, 0.44f, 0.2f), plate, "Thigh"));
            Ball(leg, new Vector3(0f, -0.5f, 0f), 0.17f, "Knee");
            Part(leg, PrimitiveType.Cylinder, new Vector3(0f, -0.76f, 0f), new Vector3(0.1f, 0.24f, 0.1f), joint, "Stilt");
            Part(leg, PrimitiveType.Cube, new Vector3(0f, -1.0f, 0.05f), new Vector3(0.24f, 0.08f, 0.4f), Satin(), "Foot");
        }

        // The chest shell with its hazard band, the name plate, the lamp, the winch drum on its back   [6]
        rig.torso = Pivot(root, "Torso", new Vector3(0f, 1.2f, 0f));
        rig.torsoRest = rig.torso.localPosition;
        plates.Add(Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.32f, 0f), new Vector3(0.6f, 0.6f, 0.5f), plate, "Chest"));
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.33f, 0f), new Vector3(0.63f, 0.1f, 0.53f), WorksMustard(), "ChestBand");
        NamePlate(rig.torso, new Vector3(0f, 0.48f, 0.256f), 0.26f, EnemyKind.Rigger);
        rig.status = Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0.256f), new Vector3(0.16f, 0.06f, 0.012f), lamp, "Lamp");
        Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0f, 0.3f, -0.42f), new Vector3(0.36f, 0.18f, 0.36f), iron, "Winch").transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

        // THE SIGNAL ARM (armR, the boom) out of the right shoulder, up and forward: a semaphore arm with its flags near
        // the pulley the chain runs off; THE LANTERN ARM (armL, the counterweight) out the back, the lamp lit in a brass cage   [5 + 3]
        rig.armR = Pivot(rig.torso, "Boom", new Vector3(0.36f, 0.55f, 0f));
        rig.boom = rig.armR;
        Part(rig.armR, PrimitiveType.Cube, new Vector3(0f, 0f, 0.85f), new Vector3(0.14f, 0.14f, 1.7f), plate, "SignalArm");
        Part(rig.armR, PrimitiveType.Cube, new Vector3(0f, 0.09f, 0.85f), new Vector3(0.05f, 0.05f, 1.6f), ForemanWhite(), "ArmStripe");
        Part(rig.armR, PrimitiveType.Cube, new Vector3(0f, 0.2f, 1.3f), new Vector3(0.03f, 0.26f, 0.4f), Trim(), "FlagRed");
        Part(rig.armR, PrimitiveType.Cube, new Vector3(0f, -0.2f, 0.75f), new Vector3(0.03f, 0.26f, 0.4f), ForemanWhite(), "FlagCream");
        Part(rig.armR, PrimitiveType.Cylinder, new Vector3(0f, 0f, 1.72f), new Vector3(0.22f, 0.08f, 0.22f), joint, "Pulley").transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        rig.muzzle = Pivot(rig.armR, "BoomTip", new Vector3(0f, -0.1f, 1.75f));
        rig.armL = Pivot(rig.torso, "Counter", new Vector3(-0.36f, 0.45f, 0f));
        Part(rig.armL, PrimitiveType.Cube, new Vector3(0f, 0f, -0.45f), new Vector3(0.14f, 0.14f, 0.9f), iron, "LanternArm");
        Part(rig.armL, PrimitiveType.Cube, new Vector3(0f, -0.1f, -0.9f), new Vector3(0.3f, 0.3f, 0.3f), joint, "LanternCage");
        Part(rig.armL, PrimitiveType.Sphere, new Vector3(0f, -0.1f, -0.9f), Vector3.one * 0.34f, lamp, "LanternLamp");

        // The head: the glass head on the chest's top, sealed until it is staggered   [5]
        rig.headPivot = Pivot(rig.torso, "HeadPivot", new Vector3(0f, 0.78f, 0f));
        WorksHead(rig, rig.headPivot, 0.3f);

        // The chain: links laid between the boom tip and the ball while one is out
        rig.chain = new GameObject("Chain").transform;
        rig.chain.SetParent(root, false);
        for (int i = 0; i < 10; i++) Part(rig.chain, PrimitiveType.Cube, Vector3.zero, new Vector3(0.09f, 0.09f, 0.2f), joint, "Link");
        rig.chain.gameObject.SetActive(false);
        rig.plates = plates.ToArray();
        return rig;
    }

    static Rig BuildBlasterRig(Transform root, Material plate)
    {
        var rig = new Rig();
        var plates = new List<Renderer>();
        Material black = WorksBlack(), joint = JointMat(), lamp = WorksLamp(), iron = WorksIron(), red = WorksRed();

        var hitbox = new GameObject("Hitbox");
        hitbox.transform.SetParent(root, false);
        var capsule = hitbox.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 0.85f, 0f);
        capsule.radius = 0.38f;
        capsule.height = 1.4f;

        // THE GANGER: a stocky mannequin — hip shell, brass-jointed legs in lacquer shins and boots, the detonator box
        // on its left hip with its brass plunger   [1 + 5 × 2 + 2]
        plates.Add(Part(root, PrimitiveType.Sphere, new Vector3(0f, 0.95f, 0f), new Vector3(0.5f, 0.22f, 0.32f), plate, "Pelvis"));
        Part(root, PrimitiveType.Cube, new Vector3(-0.3f, 0.85f, 0.1f), new Vector3(0.16f, 0.14f, 0.12f), black, "Detonator");
        Part(root, PrimitiveType.Cylinder, new Vector3(-0.3f, 0.97f, 0.1f), new Vector3(0.03f, 0.08f, 0.03f), joint, "Plunger");
        rig.legL = Pivot(root, "LegL", new Vector3(-0.17f, 0.9f, 0f));
        rig.legR = Pivot(root, "LegR", new Vector3(0.17f, 0.9f, 0f));
        foreach (var leg in new[] { rig.legL, rig.legR })
        {
            Ball(leg, Vector3.zero, 0.2f, "Hip");
            plates.Add(Shell(leg, new Vector3(0f, -0.25f, 0f), new Vector3(0.2f, 0.44f, 0.2f), plate, "Thigh"));
            Ball(leg, new Vector3(0f, -0.5f, 0f), 0.18f, "Knee");
            Shell(leg, new Vector3(0f, -0.7f, 0.02f), new Vector3(0.18f, 0.4f, 0.18f), Satin(), "Shin");
            Part(leg, PrimitiveType.Cube, new Vector3(0f, -0.9f, 0.06f), new Vector3(0.24f, 0.08f, 0.34f), Satin(), "Boot");
        }

        // A stocky chest with its hazard band and name plate, the hopper on its back with two charges showing and the key
        // out of it   [8]
        rig.torso = Pivot(root, "Torso", new Vector3(0f, 1.05f, 0f));
        rig.torsoRest = rig.torso.localPosition;
        plates.Add(Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.28f, 0f), new Vector3(0.7f, 0.55f, 0.42f), plate, "Chest"));
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.28f, 0f), new Vector3(0.73f, 0.1f, 0.45f), WorksMustard(), "ChestBand");
        rig.status = Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.44f, 0.216f), new Vector3(0.18f, 0.05f, 0.012f), lamp, "StatusLight");
        NamePlate(rig.torso, new Vector3(0f, 0.1f, 0.216f), 0.3f, EnemyKind.Blaster);
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.3f, -0.36f), new Vector3(0.5f, 0.5f, 0.3f), iron, "Hopper");
        for (int i = 0; i < 2; i++)
            Part(rig.torso, PrimitiveType.Sphere, new Vector3(-0.1f + i * 0.2f, 0.52f, -0.36f), Vector3.one * 0.14f, red, "Charge");
        Key(rig, rig.torso, new Vector3(0f, 0.22f, -0.51f), 0.2f);

        // Arms on their shoulder balls: upper shell, elbow, lacquer forearm, the white glove   [5 × 2]
        rig.armL = Pivot(rig.torso, "ArmL", new Vector3(-0.42f, 0.48f, 0f));
        rig.armR = Pivot(rig.torso, "ArmR", new Vector3(0.42f, 0.48f, 0f));
        foreach (var arm in new[] { rig.armL, rig.armR })
        {
            Ball(arm, Vector3.zero, 0.22f, "Shoulder");
            plates.Add(Shell(arm, new Vector3(0f, -0.22f, 0f), new Vector3(0.16f, 0.4f, 0.16f), plate, "UpperArm"));
            Ball(arm, new Vector3(0f, -0.44f, 0f), 0.16f, "Elbow");
            Shell(arm, new Vector3(0f, -0.62f, 0.04f), new Vector3(0.14f, 0.34f, 0.14f), Satin(), "Forearm");
            Part(arm, PrimitiveType.Cube, new Vector3(0f, -0.8f, 0.06f), new Vector3(0.16f, 0.1f, 0.16f), Glove(), "Glove"); // white gloves: staff
        }
        rig.muzzle = Pivot(rig.armR, "Hand", new Vector3(0f, -0.85f, 0.1f));

        // The glass head under a hard hat's brim: open, like any crew unit's   [5 + 1]
        rig.headPivot = Pivot(rig.torso, "HeadPivot", new Vector3(0f, 0.72f, 0f));
        WorksHead(rig, rig.headPivot, 0.36f);
        plates.Add(Part(rig.headPivot, PrimitiveType.Cylinder, new Vector3(0f, 0.12f, 0f), new Vector3(0.52f, 0.02f, 0.52f), plate, "Brim"));
        rig.plates = plates.ToArray();
        return rig;
    }
}
