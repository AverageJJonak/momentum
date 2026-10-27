using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE STATIONMASTER's body (the Foreman re-dressed for the Underground, 2026-10-12):
/// the terminus's own towering automaton on a railway trolley — a charcoal
/// FROCK COAT for a body with three coat panels on a ring round it (the shield),
/// a brass LANTERN on its chest with two shutters that swing open on the CORE
/// (the cyan mainspring), a SIGNAL-BOX HEAD — a glass-sided brass box with the
/// escapement ticking inside and two amber lamps for eyes — under a PEAKED CAP
/// with a beacon, a steam WHISTLE on the cap, a TICKET PUNCH on the right
/// shoulder that fires punched brass tokens, a SIGNAL LEVER for a right arm (the
/// counterweight block is what lands), a lamp boom with a coal scuttle for a
/// left, two whistles on its back whose caps blow open (the STACKS) when it
/// stalls, and the wind-up key between them. Charcoal enamel, brass, cream
/// piping, amber lamps: a railwayman, only the biggest one. Same skeleton as
/// every other unit (torso, head, arm and leg pivots), so the animator, the
/// lock-on and the ragdoll all work — with the Concierge's extras mapped onto
/// it so the intro, the exit and the bar drive it unchanged: the core is the
/// throat, the whistle caps are the cuffs, the coat panels are the buttons, the
/// lantern shutters are the coat fronts, the badge is the company plate on its
/// chest. Hitbox, pivots, weakspots, plates and tells exactly as the Foreman's.
/// Nothing sits in the plane of another part.
/// </summary>
public partial class Enemy
{
    static Material fmMolten, fmFurnace, fmWhite, fmSteel, fmGlow;
    static Material ForemanMolten() => fmMolten ??= Unlit(new Color(2.6f, 0.5f, 0.12f));      // hot coals: red-orange, danger
    static Material ForemanFurnace() => fmFurnace ??= Unlit(new Color(2.2f, 0.95f, 0.3f));   // the lantern's own light: amber
    static Material ForemanWhite() => fmWhite ??= Lit(new Color(0.85f, 0.8f, 0.68f), 0.05f, 0.45f);   // the company plate, the piping: cream enamel
    static Material ForemanSteel() => fmSteel ??= Lit(new Color(0.5f, 0.51f, 0.54f), 0.85f, 0.6f);
    static Material ForemanGlow() => fmGlow ??= Unlit(new Color(1.6f, 1.0f, 0.4f));           // the underglow of the trolley's pads

    public const string ForemanName = "THE STATIONMASTER";

    static Rig BuildForemanRig(Transform root)
    {
        var rig = new Rig();
        var plates = new List<Renderer>();
        var coat = new List<Renderer>();
        Material cloth = ChassisMat(), lamp = WorksLamp(), iron = WorksIron(), joint = JointMat(), cream = ForemanWhite(),
            steel = ForemanSteel(), molten = ForemanMolten(), furnace = ForemanFurnace();
        rig.coatRest = cloth;
        rig.eyeRest = lamp;

        // Hitbox: one box, trolley to cap; the head is solid too (a body hit)
        var hitbox = new GameObject("Hitbox");
        hitbox.transform.SetParent(root, false);
        var box = hitbox.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 1.15f, 0f);
        box.size = new Vector3(2.2f, 2.1f, 2.2f);

        // The railway trolley it rides: the iron deck with its brass edge, the bogies either side with their lit pads
        Part(root, PrimitiveType.Cube, new Vector3(0f, 0.09f, 0f), new Vector3(2.2f, 0.14f, 2.6f), iron, "Deck");
        Part(root, PrimitiveType.Cube, new Vector3(0f, 0.09f, 0f), new Vector3(2.24f, 0.05f, 2.64f), joint, "DeckBand");
        Part(root, PrimitiveType.Cube, new Vector3(0f, 0.025f, 0f), new Vector3(2f, 0.02f, 2.4f), ForemanGlow(), "Underglow");
        rig.legL = Pivot(root, "SkidL", new Vector3(-0.72f, 0.24f, 0f));
        rig.legR = Pivot(root, "SkidR", new Vector3(0.72f, 0.24f, 0f));
        foreach (var leg in new[] { rig.legL, rig.legR })
        {
            Part(leg, PrimitiveType.Cube, Vector3.zero, new Vector3(0.44f, 0.36f, 2.1f), cloth, "Bogie");
            Part(leg, PrimitiveType.Cube, new Vector3(0f, -0.02f, 0f), new Vector3(0.47f, 0.14f, 2.13f), joint, "BogieBand");
            for (int i = 0; i < 3; i++)
                Part(leg, PrimitiveType.Cube, new Vector3(0f, -0.19f, -0.65f + i * 0.65f), new Vector3(0.4f, 0.04f, 0.45f), lamp, "MagPad");
        }
        Part(root, PrimitiveType.Cube, new Vector3(0f, 0.6f, 0f), new Vector3(1.1f, 0.34f, 1.5f), iron, "Pelvis");
        Part(root, PrimitiveType.Cylinder, new Vector3(0f, 0.8f, 0f), new Vector3(1.2f, 0.05f, 1.2f), joint, "Turntable");

        // The torso: THE FROCK COAT, its brass bands, the three coat panels on the ring (the plates that come off), the
        // company's plate on its chest, the key between the whistles on its back
        rig.torso = Pivot(root, "Torso", new Vector3(0f, 0.85f, 0f));
        rig.torsoRest = rig.torso.localPosition;
        Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 0f), new Vector3(1.3f, 0.55f, 1.3f), cloth, "Coat");
        Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0f, 0.22f, 0f), new Vector3(1.34f, 0.04f, 1.34f), joint, "CoatBand");
        Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0f, 0.9f, 0f), new Vector3(1.34f, 0.04f, 1.34f), joint, "CoatBand");
        Key(rig, rig.torso, new Vector3(0f, 0.55f, -0.65f), 0.44f);
        // THE SHIELD: three coat panels on a ring round the body, turning, one quadrant open — the open quadrant is the
        // only place a hit lands, and the cyan band on the coat shows through it: cut here. The ring sits below the
        // shoulders and outside the lantern, so nothing on the body passes through it as it turns
        rig.shieldBand = Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0f, 0.475f, 0f), new Vector3(1.44f, 0.05f, 1.44f), WeakMat(), "ShieldBand"); // just outside the core, just inside the shutters
        rig.shield = Pivot(rig.torso, "Shield", new Vector3(0f, 0.475f, 0f));
        for (int i = 0; i < 3; i++)
        {
            Quaternion rot = Quaternion.Euler(0f, 90f * i, 0f);
            var plate = Part(rig.shield, PrimitiveType.Cube, rot * new Vector3(0f, 0f, 0.9f), new Vector3(1.36f, 0.75f, 0.08f), cloth, "CoatPanel");
            plate.transform.localRotation = rot;
            Part(plate.transform, PrimitiveType.Cube, new Vector3(0f, 0f, 0.55f), new Vector3(1.02f, 0.14f, 0.2f), cream, "PanelPiping");
            Part(plate.transform, PrimitiveType.Cube, new Vector3(0f, 0.44f, 0.4f), new Vector3(1.02f, 0.06f, 0.25f), joint, "PanelRim");
            Part(plate.transform, PrimitiveType.Cube, new Vector3(0f, -0.44f, 0.4f), new Vector3(1.02f, 0.06f, 0.25f), joint, "PanelRim");
            plates.Add(plate);
        }
        Part(rig.shield, PrimitiveType.Cylinder, new Vector3(0f, 0.4f, 0f), new Vector3(1.86f, 0.025f, 1.86f), iron, "ShieldRail");
        Part(rig.shield, PrimitiveType.Cylinder, new Vector3(0f, -0.4f, 0f), new Vector3(1.86f, 0.025f, 1.86f), iron, "ShieldRail");
        rig.badge = Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.92f, 0.6f), new Vector3(0.4f, 0.16f, 0.03f), cream, "Badge");
        rig.badge.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
        LevelKit.Text(rig.torso, rig.torso.TransformPoint(new Vector3(0f, 0.92f, 0.62f)), rig.torso.rotation * Quaternion.Euler(-12f, 180f, 0f),
            "HALCYON RAILWAYS", 0.006f, new Color(0.05f, 0.05f, 0.06f), "BadgeText", 0.36f, true, 30f, 80f); // the Underground's name in the House's register

        // THE LANTERN on its chest: a brass frame open at the front, the amber glass at the back, the CORE inside (the
        // mainspring: cyan), the two shutters over it
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.45f, 0.3f), new Vector3(0.72f, 0.72f, 0.04f), joint, "LanternBack");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.81f, 0.55f), new Vector3(0.72f, 0.04f, 0.5f), joint, "LanternTop");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.09f, 0.55f), new Vector3(0.72f, 0.04f, 0.5f), joint, "LanternBottom");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(-0.36f, 0.45f, 0.55f), new Vector3(0.04f, 0.72f, 0.5f), joint, "LanternSideL");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0.36f, 0.45f, 0.55f), new Vector3(0.04f, 0.72f, 0.5f), joint, "LanternSideR");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.45f, 0.325f), new Vector3(0.62f, 0.62f, 0.01f), furnace, "LanternGlow");
        Renderer core = Part(rig.torso, PrimitiveType.Sphere, new Vector3(0f, 0.45f, 0.55f), Vector3.one * 0.3f, WeakMat(), "Core", keepCollider: true);
        core.gameObject.AddComponent<EnemyWeakspot>();
        rig.head = core;
        rig.throat = core;
        Transform halo = Pivot(rig.torso, "HaloPivot", new Vector3(0f, 0.45f, 0.55f));
        halo.localScale = Vector3.one * 0.45f;
        rig.ring = Part(halo, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.9f, 0.015f, 0.9f), RingMat(), "WeakspotRing").transform;
        rig.coatL = Pivot(rig.torso, "DoorL", new Vector3(-0.36f, 0.45f, 0.8f));
        rig.coatR = Pivot(rig.torso, "DoorR", new Vector3(0.36f, 0.45f, 0.8f));
        coat.Add(Part(rig.coatL, PrimitiveType.Cube, new Vector3(0.18f, 0f, 0f), new Vector3(0.36f, 0.7f, 0.06f), cloth, "ShutterL"));
        coat.Add(Part(rig.coatR, PrimitiveType.Cube, new Vector3(-0.18f, 0f, 0f), new Vector3(0.36f, 0.7f, 0.06f), cloth, "ShutterR"));
        Part(rig.coatL, PrimitiveType.Cube, new Vector3(0.18f, 0f, 0.035f), new Vector3(0.34f, 0.08f, 0.02f), cream, "ShutterPipingL");
        Part(rig.coatR, PrimitiveType.Cube, new Vector3(-0.18f, 0f, 0.035f), new Vector3(0.34f, 0.08f, 0.02f), cream, "ShutterPipingR");
        Part(rig.coatL, PrimitiveType.Cube, new Vector3(0.3f, -0.15f, 0.05f), new Vector3(0.04f, 0.18f, 0.04f), joint, "ShutterHandleL");
        Part(rig.coatR, PrimitiveType.Cube, new Vector3(-0.3f, -0.15f, 0.05f), new Vector3(0.04f, 0.18f, 0.04f), joint, "ShutterHandleR");

        // Shoulders on brass balls, and THE TICKET PUNCH on the right one: a brass box, the token hopper behind it, the barrel
        Part(rig.torso, PrimitiveType.Cube, new Vector3(-0.82f, 1.05f, 0f), new Vector3(0.36f, 0.22f, 0.3f), iron, "ShoulderMountL");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0.82f, 1.05f, 0f), new Vector3(0.36f, 0.22f, 0.3f), iron, "ShoulderMountR");
        Part(rig.torso, PrimitiveType.Sphere, new Vector3(-0.95f, 1.05f, 0f), Vector3.one * 0.34f, joint, "ShoulderL");
        Part(rig.torso, PrimitiveType.Sphere, new Vector3(0.95f, 1.05f, 0f), Vector3.one * 0.34f, joint, "ShoulderR");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0.95f, 1.3f, 0f), new Vector3(0.22f, 0.18f, 0.34f), joint, "PunchBody");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0.95f, 1.2f, -0.14f), new Vector3(0.16f, 0.14f, 0.16f), iron, "TokenHopper");
        Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0.95f, 1.3f, 0.42f), new Vector3(0.1f, 0.3f, 0.1f), steel, "Barrel").transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0.95f, 1.3f, 0.72f), new Vector3(0.14f, 0.015f, 0.14f), lamp, "MuzzleRing").transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        rig.muzzle = Pivot(rig.torso, "Muzzle", new Vector3(0.95f, 1.3f, 0.76f));

        // THE WHISTLES on its back, and the caps on them: the STACKS (the same spheres, in the same places)
        for (int s = -1; s <= 1; s += 2)
        {
            Part(rig.torso, PrimitiveType.Cylinder, new Vector3(s * 0.3f, 0.7f, -0.5f), new Vector3(0.14f, 0.45f, 0.14f), joint, "Whistle");
            Part(rig.torso, PrimitiveType.Cylinder, new Vector3(s * 0.3f, 1.08f, -0.5f), new Vector3(0.16f, 0.03f, 0.16f), cream, "WhistleBand");
            Part(rig.torso, PrimitiveType.Cube, new Vector3(s * 0.3f, 1.18f, -0.42f), new Vector3(0.05f, 0.04f, 0.08f), joint, "CapHinge");
            Renderer lid = Part(rig.torso, PrimitiveType.Sphere, new Vector3(s * 0.3f, 1.2f, -0.5f), Vector3.one * 0.17f, WeakMat(), s < 0 ? "StackL" : "StackR", keepCollider: true);
            lid.gameObject.AddComponent<EnemyWeakspot>();
            if (s < 0) rig.cuffL = lid; else rig.cuffR = lid;
        }

        // The arms: the signal lever on the right, the lamp boom and the coal scuttle on the left
        rig.armR = ForemanHammer(rig.torso, cloth, cream, iron, steel, joint, coat, out rig.foreR, out rig.handR);
        rig.armL = ForemanBoom(rig.torso, cloth, iron, joint, molten, coat, out rig.foreL, out rig.handL);

        // THE SIGNAL-BOX HEAD: a glass-sided brass box (solid: it keeps its collider) with the balance wheel ticking
        // inside, two amber lamps for eyes, under the peaked cap with its beacon, the whistle, the aerial, the number
        rig.headPivot = Pivot(rig.torso, "HeadPivot", new Vector3(0f, 1.35f, 0.05f));
        var head = Part(rig.headPivot, PrimitiveType.Cube, new Vector3(0f, 0.22f, 0f), new Vector3(0.8f, 0.5f, 0.7f), Glass(), "Cab", keepCollider: true);
        head.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Part(rig.headPivot, PrimitiveType.Cube, new Vector3(0f, 0.22f, 0f), new Vector3(0.82f, 0.09f, 0.72f), joint, "CabFrame");
        rig.balance = Pivot(rig.headPivot, "Balance", new Vector3(0f, 0.22f, 0.12f));
        Part(rig.balance, PrimitiveType.Cube, Vector3.zero, new Vector3(0.44f, 0.04f, 0.02f), joint, "BalanceArm");
        Part(rig.headPivot, PrimitiveType.Cube, new Vector3(0f, 0.3f, 0.355f), new Vector3(0.6f, 0.12f, 0.02f), joint, "Brow");
        rig.eyeL = Part(rig.headPivot, PrimitiveType.Sphere, new Vector3(-0.27f, 0.44f, 0.34f), Vector3.one * 0.11f, lamp, "LampL");
        rig.eyeR = Part(rig.headPivot, PrimitiveType.Sphere, new Vector3(0.27f, 0.44f, 0.34f), Vector3.one * 0.11f, lamp, "LampR");
        Part(rig.headPivot, PrimitiveType.Sphere, new Vector3(0f, 0.52f, 0f), new Vector3(0.86f, 0.36f, 0.8f), Cloth(), "CapCrown");
        Part(rig.headPivot, PrimitiveType.Cylinder, new Vector3(0f, 0.49f, 0.06f), new Vector3(0.96f, 0.015f, 0.92f), joint, "CapBand");
        Turned(Part(rig.headPivot, PrimitiveType.Cube, new Vector3(0f, 0.47f, 0.52f), new Vector3(0.6f, 0.02f, 0.3f), Satin(), "CapPeak"), 8f);
        Part(rig.headPivot, PrimitiveType.Sphere, new Vector3(0f, 0.6f, 0.02f), Vector3.one * 0.12f, joint, "CapButton");
        rig.beacon = Pivot(rig.headPivot, "Beacon", new Vector3(0f, 0.74f, 0f));
        Part(rig.beacon, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.12f, 0.06f, 0.12f), lamp, "BeaconLamp");
        Part(rig.beacon, PrimitiveType.Cube, new Vector3(0f, 0f, -0.05f), new Vector3(0.14f, 0.13f, 0.05f), Satin(), "BeaconHood");
        Part(rig.headPivot, PrimitiveType.Cylinder, new Vector3(0.44f, 0.36f, -0.1f), new Vector3(0.07f, 0.12f, 0.07f), joint, "Whistle").transform.localRotation = Quaternion.Euler(0f, 0f, -70f);
        Part(rig.headPivot, PrimitiveType.Cylinder, new Vector3(-0.36f, 0.62f, -0.2f), new Vector3(0.02f, 0.16f, 0.02f), joint, "Aerial");
        LevelKit.Text(rig.headPivot, rig.headPivot.TransformPoint(new Vector3(-0.41f, 0.18f, 0.1f)), rig.headPivot.rotation * Quaternion.Euler(0f, 90f, 0f),
            "No 1", 0.012f, new Color(1f, 0.9f, 0.7f), "UnitNumber", 0.3f, true, 30f, 80f); // cream on the glass

        rig.buttons = plates.ToArray();
        rig.plates = rig.buttons;
        rig.coat = coat.ToArray();
        return rig;
    }

    /// THE SIGNAL LEVER arm: two coat-sleeved sections on brass rods, the counterweight block that lands (with a
    /// semaphore blade off it), a work lamp on the wrist
    static Transform ForemanHammer(Transform torso, Material cloth, Material cream, Material iron, Material steel, Material joint,
        List<Renderer> coat, out Transform fore, out Transform hand)
    {
        Transform shoulder = Pivot(torso, "ArmR", new Vector3(0.95f, 1.05f, 0f));
        coat.Add(Part(shoulder, PrimitiveType.Cube, new Vector3(0f, -0.3f, 0f), new Vector3(0.28f, 0.55f, 0.28f), cloth, "UpperArm"));
        Part(shoulder, PrimitiveType.Cylinder, new Vector3(0.17f, -0.3f, 0.1f), new Vector3(0.06f, 0.24f, 0.06f), joint, "LeverRod");
        Part(shoulder, PrimitiveType.Sphere, new Vector3(0f, -0.6f, 0f), Vector3.one * 0.3f, joint, "Elbow");
        var forearm = Part(shoulder, PrimitiveType.Cube, new Vector3(0f, -0.88f, 0f), new Vector3(0.26f, 0.5f, 0.26f), cloth, "Forearm");
        coat.Add(forearm);
        fore = forearm.transform;
        Part(shoulder, PrimitiveType.Cylinder, new Vector3(-0.16f, -0.88f, 0.09f), new Vector3(0.06f, 0.22f, 0.06f), joint, "LeverRod");
        Part(shoulder, PrimitiveType.Cube, new Vector3(0f, -1.12f, 0.14f), new Vector3(0.1f, 0.06f, 0.06f), WorksLamp(), "WristLamp");
        hand = Pivot(shoulder, "Hammer", new Vector3(0f, -1.36f, 0f));
        Part(hand, PrimitiveType.Cube, Vector3.zero, new Vector3(0.7f, 0.42f, 0.55f), iron, "LeverBlock");
        Part(hand, PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(0.72f, 0.1f, 0.57f), cream, "LeverBand");
        Part(hand, PrimitiveType.Cube, new Vector3(0f, -0.225f, 0f), new Vector3(0.66f, 0.03f, 0.5f), cloth, "LeverFace");
        Part(hand, PrimitiveType.Cube, new Vector3(0f, 0.36f, -0.1f), new Vector3(0.06f, 0.3f, 0.18f), Trim(), "SemaphoreBlade");
        return shoulder;
    }

    /// THE LAMP BOOM arm: a lattice boom, a chain, and the coal scuttle with the coals glowing in it (the pour)
    static Transform ForemanBoom(Transform torso, Material cloth, Material iron, Material joint, Material molten,
        List<Renderer> coat, out Transform fore, out Transform hand)
    {
        Transform shoulder = Pivot(torso, "ArmL", new Vector3(-0.95f, 1.05f, 0f));
        for (int s = -1; s <= 1; s += 2)
            coat.Add(Part(shoulder, PrimitiveType.Cube, new Vector3(s * 0.09f, -0.48f, 0f), new Vector3(0.06f, 0.95f, 0.06f), cloth, "BoomRail"));
        for (int i = 0; i < 4; i++)
            Part(shoulder, PrimitiveType.Cube, new Vector3(0f, -0.14f - i * 0.24f, 0f), new Vector3(0.24f, 0.04f, 0.04f), joint, "BoomTie");
        Part(shoulder, PrimitiveType.Sphere, new Vector3(0f, -0.06f, 0f), Vector3.one * 0.2f, joint, "BoomPin");
        fore = Pivot(shoulder, "BoomTip", new Vector3(0f, -0.96f, 0f));
        Part(fore, PrimitiveType.Cube, Vector3.zero, new Vector3(0.24f, 0.08f, 0.1f), iron, "TipBlock");
        Part(fore, PrimitiveType.Cube, new Vector3(0f, -0.16f, 0f), new Vector3(0.03f, 0.3f, 0.03f), joint, "Chain");
        hand = Pivot(fore, "Ladle", new Vector3(0f, -0.36f, 0f));
        Part(hand, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.36f, 0.15f, 0.36f), iron, "ScuttleBowl");
        Part(hand, PrimitiveType.Cylinder, new Vector3(0f, 0.14f, 0f), new Vector3(0.38f, 0.02f, 0.38f), joint, "ScuttleLip");
        Part(hand, PrimitiveType.Cylinder, new Vector3(0f, 0.156f, 0f), new Vector3(0.3f, 0.01f, 0.3f), molten, "Coals");
        Part(hand, PrimitiveType.Cube, new Vector3(0.2f, 0.06f, 0f), new Vector3(0.08f, 0.04f, 0.04f), joint, "ScuttleSpout");
        return shoulder;
    }
}
