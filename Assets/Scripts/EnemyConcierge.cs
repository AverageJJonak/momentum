using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE CONCIERGE's body. Not a steward in a bigger size: a tall thing in a navy
/// double-breasted suit — satin lapels with the company pin, four gold buttons,
/// a white shirt, a burgundy waistcoat and tie, a lanyard with his ID card on
/// it, white gloves, a headset, hair combed back over a porcelain face that is
/// always smiling. The front desk of Halcyon Civic. Friendly at a glance; too
/// tall, too many fingers, a smile a little too wide.
/// Same skeleton as every other unit (torso, head, arm and leg pivots), so the
/// animator, the lock-on and the ragdoll all work — with extras the boss fight
/// drives: the jacket fronts swing open, the arms stretch, and the weakspots
/// (throat, cuffs) open and close. Nothing sits in the plane of another part:
/// coplanar faces flicker.
/// </summary>
public partial class Enemy
{
    static Material navyC, charcoalC, shirtC, porcelainC, wineC, goldC, inkC, blushC, satinC, gloveC;
    static Material Navy() => navyC ??= Lit(new Color(0.1f, 0.11f, 0.19f), 0.15f, 0.6f);
    static Material Charcoal() => charcoalC ??= Lit(new Color(0.13f, 0.13f, 0.15f), 0.1f, 0.4f);
    static Material Shirt() => shirtC ??= Lit(new Color(0.96f, 0.95f, 0.9f), 0.05f, 0.55f);
    static Material Porcelain() => porcelainC ??= Lit(new Color(0.97f, 0.94f, 0.9f), 0.05f, 0.88f);
    static Material Burgundy() => wineC ??= Lit(new Color(0.45f, 0.06f, 0.1f), 0.1f, 0.35f);
    static Material Gold() => goldC ??= Lit(new Color(0.78f, 0.58f, 0.25f), 0.9f, 0.7f);     // brass: the pins, the buttons, the cuffs
    static Material Ink() => inkC ??= Unlit(new Color(0.02f, 0.02f, 0.03f));
    static Material Blush() => blushC ??= Lit(new Color(0.5f, 0.18f, 0.16f), 0.05f, 0.25f);   // a doll's painted rouge
    static Material Satin() => satinC ??= Lit(new Color(0.03f, 0.03f, 0.04f), 0.3f, 0.92f);
    static Material Glove() => gloveC ??= Lit(new Color(0.98f, 0.98f, 0.96f), 0.05f, 0.4f);

    // The boss controller drives these directly
    public Renderer BossThroat => rig?.throat;
    public Renderer BossCuffL => rig?.cuffL;
    public Renderer BossCuffR => rig?.cuffR;
    public Renderer BossBadge => rig?.badge;
    public Renderer BossEyeL => rig?.eyeL;
    public Renderer BossEyeR => rig?.eyeR;
    public Transform BossCoatL => rig?.coatL;
    public Transform BossCoatR => rig?.coatR;
    public Transform BossHandL => rig?.handL;
    public Transform BossHandR => rig?.handR;
    public Transform BossForeL => rig?.foreL;
    public Transform BossForeR => rig?.foreR;
    public Transform BossSmile => rig?.smile;
    public Renderer[] BossButtons => rig?.buttons;
    public Transform BossBeacon => rig?.beacon;
    public Transform BossShield => rig?.shield;
    public Renderer BossShieldBand => rig?.shieldBand;

    /// Open or seal one weakspot other than the throat (the cuffs): collider on, cyan; or off, dark
    public void SetWeakspot(Renderer spot, bool open)
    {
        if (spot == null) return;
        var col = spot.GetComponent<Collider>();
        if (col != null) col.enabled = open && Alive;
        spot.sharedMaterial = open ? WeakMat() : SealedMat();
    }

    public bool WeakspotIsOpen(Renderer spot)
    {
        if (spot == null) return false;
        var col = spot.GetComponent<Collider>();
        return col != null && col.enabled;
    }

    /// The jacket takes the telegraph colours: gold for a parry window, red for a move-or-else
    public void SetCoatSkin(Material m)
    {
        if (rig?.coat == null) return;
        foreach (var r in rig.coat) if (r != null) r.sharedMaterial = m ?? rig.coatRest ?? Navy();
    }

    public void SetEyes(Material m)
    {
        if (rig == null) return;
        if (rig.eyeL != null) rig.eyeL.sharedMaterial = m ?? rig.eyeRest ?? Ink();
        if (rig.eyeR != null) rig.eyeR.sharedMaterial = m ?? rig.eyeRest ?? Ink();
    }

    public static Material EyeRed() => CorruptMat();
    public static Material TelegraphGold() => WindupMat();

    /// A windup the director can counter (parry near the body = COUNTER)
    public void SetWindup(float until) => windupEndsAt = until;

    /// The sword's aim rests on a weakspot: every open one flares, not just the throat
    void LockOnExtras(bool targeted)
    {
        if (rig == null) return;
        if (rig.cuffL != null && WeakspotIsOpen(rig.cuffL)) rig.cuffL.sharedMaterial = targeted ? TargetMat() : WeakMat();
        if (rig.cuffR != null && WeakspotIsOpen(rig.cuffR)) rig.cuffR.sharedMaterial = targeted ? TargetMat() : WeakMat();
    }

    // ---------------------------------------------------------------- the rig

    static Rig BuildConciergeRig(Transform root)
    {
        var rig = new Rig();
        var buttons = new List<Renderer>();
        var coat = new List<Renderer>();
        Material navy = Navy(), charcoal = Charcoal(), shirt = Shirt(), skin = Porcelain(), wine = Burgundy(), gold = Gold(), ink = Ink(), glove = Glove(), satin = Satin();

        // Hitbox: one body capsule, shoes to shoulders; the face is solid too (a body hit)
        var hitbox = new GameObject("Hitbox");
        hitbox.transform.SetParent(root, false);
        var capsule = hitbox.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 0.95f, 0f);
        capsule.radius = 0.32f;
        capsule.height = 1.7f;

        // Trousers, belt, legs, shoes
        Part(root, PrimitiveType.Cube, new Vector3(0f, 1.02f, 0f), new Vector3(0.32f, 0.16f, 0.22f), charcoal, "Pelvis");
        Part(root, PrimitiveType.Cube, new Vector3(0f, 1.14f, 0f), new Vector3(0.3f, 0.06f, 0.24f), satin, "Belt");
        Part(root, PrimitiveType.Cube, new Vector3(0f, 1.14f, 0.128f), new Vector3(0.06f, 0.05f, 0.015f), gold, "Buckle");
        rig.legL = ConciergeLeg(root, -0.11f, charcoal);
        rig.legR = ConciergeLeg(root, 0.11f, charcoal);

        // The torso: shirt, the jacket in three pieces (the fronts swing open), lapels with the company pin, buttons
        rig.torso = Pivot(root, "Torso", new Vector3(0f, 1.2f, 0f));
        rig.torsoRest = rig.torso.localPosition;
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0.02f), new Vector3(0.24f, 0.44f, 0.24f), shirt, "ShirtFront");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0.143f), new Vector3(0.05f, 0.4f, 0.01f), satin, "Placket");
        coat.Add(Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.2f, -0.1f), new Vector3(0.44f, 0.46f, 0.14f), navy, "JacketBack"));
        rig.coatL = Pivot(rig.torso, "CoatL", new Vector3(-0.23f, 0.2f, 0.02f));
        rig.coatR = Pivot(rig.torso, "CoatR", new Vector3(0.23f, 0.2f, 0.02f));
        coat.Add(Part(rig.coatL, PrimitiveType.Cube, new Vector3(0.085f, 0f, 0f), new Vector3(0.17f, 0.46f, 0.27f), navy, "JacketFrontL"));
        coat.Add(Part(rig.coatR, PrimitiveType.Cube, new Vector3(-0.085f, 0f, 0f), new Vector3(0.17f, 0.46f, 0.27f), navy, "JacketFrontR"));
        Part(rig.coatL, PrimitiveType.Cube, new Vector3(0.12f, 0.12f, 0.168f), new Vector3(0.075f, 0.26f, 0.015f), satin, "LapelL").transform.localRotation = Quaternion.Euler(0f, 0f, 12f);
        Part(rig.coatR, PrimitiveType.Cube, new Vector3(-0.12f, 0.12f, 0.168f), new Vector3(0.075f, 0.26f, 0.015f), satin, "LapelR").transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
        // The company pin on each lapel: the sun on the horizon, in gold
        for (int s = -1; s <= 1; s += 2)
        {
            Transform lapel = s < 0 ? rig.coatL : rig.coatR;
            Part(lapel, PrimitiveType.Cylinder, new Vector3(-s * 0.13f, 0.2f, 0.181f), new Vector3(0.04f, 0.004f, 0.04f), gold, "LapelPin").transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part(lapel, PrimitiveType.Cube, new Vector3(-s * 0.13f, 0.192f, 0.188f), new Vector3(0.05f, 0.005f, 0.004f), gold, "LapelPinBar");
        }
        // Double-breasted: two columns of gold buttons — the four that pop
        for (int i = 0; i < 4; i++)
            buttons.Add(Part(rig.torso, PrimitiveType.Sphere, new Vector3(i % 2 == 0 ? -0.075f : 0.075f, 0.31f - 0.14f * (i / 2), 0.15f), Vector3.one * 0.05f, gold, "Button"));
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, -0.01f, 0.03f), new Vector3(0.25f, 0.1f, 0.23f), wine, "Waistcoat");
        Part(rig.torso, PrimitiveType.Sphere, new Vector3(-0.29f, 0.4f, 0f), Vector3.one * 0.2f, navy, "ShoulderL");
        Part(rig.torso, PrimitiveType.Sphere, new Vector3(0.29f, 0.4f, 0f), Vector3.one * 0.2f, navy, "ShoulderR");
        Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0f, 0.47f, 0f), new Vector3(0.14f, 0.05f, 0.14f), shirt, "Collar");

        // The tie, and behind its knot the THROAT: the weakspot that opens when he bows
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.27f, 0.148f), new Vector3(0.07f, 0.32f, 0.012f), wine, "Tie");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.455f, 0.14f), new Vector3(0.08f, 0.06f, 0.05f), wine, "TieKnot");
        Renderer throat = Part(rig.torso, PrimitiveType.Sphere, new Vector3(0f, 0.455f, 0.105f), Vector3.one * 0.12f, WeakMat(), "Throat", keepCollider: true);
        throat.gameObject.AddComponent<EnemyWeakspot>();
        rig.head = throat;
        rig.throat = throat;
        // The halo under a pivot at half size: the lock-on pulses it at the steward's numbers, and this body is big
        Transform halo = Pivot(rig.torso, "HaloPivot", new Vector3(0f, 0.455f, 0.105f));
        halo.localScale = Vector3.one * 0.45f;
        rig.ring = Part(halo, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.9f, 0.015f, 0.9f), RingMat(), "WeakspotRing").transform;

        // The lanyard and the ID card on it
        for (int s = -1; s <= 1; s += 2)
            Part(rig.torso, PrimitiveType.Cube, new Vector3(s * 0.045f, 0.32f, 0.146f), new Vector3(0.012f, 0.25f, 0.006f), satin, "Lanyard").transform.localRotation = Quaternion.Euler(0f, 0f, -s * 6f);
        rig.badge = Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.13f, 0.157f), new Vector3(0.11f, 0.15f, 0.01f), glove, "Badge");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.19f, 0.163f), new Vector3(0.11f, 0.02f, 0.004f), gold, "BadgeBand");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(-0.028f, 0.148f, 0.163f), new Vector3(0.04f, 0.045f, 0.004f), skin, "BadgePhoto");
        LevelKit.Text(rig.torso, rig.torso.TransformPoint(new Vector3(0.012f, 0.1f, 0.166f)), rig.torso.rotation * Quaternion.Euler(0f, 180f, 0f),
            "CONCIERGE", 0.0045f, new Color(0.05f, 0.05f, 0.06f), "BadgeText", 0.1f, true, 30f, 70f);

        // Arms: white gloves, gold cuffs with the CUFF weakspots inside them, fingers a little long
        rig.armL = ConciergeArm(rig.torso, -0.3f, navy, gold, glove, out rig.foreL, out rig.handL, out rig.cuffL);
        rig.armR = ConciergeArm(rig.torso, 0.3f, navy, gold, glove, out rig.foreR, out rig.handR, out rig.cuffR);
        rig.muzzle = rig.handR;

        // The head: porcelain, hair combed back, a headset, two dark eyes with a glint, rosy cheeks, and the smile
        rig.headPivot = Pivot(rig.torso, "HeadPivot", new Vector3(0f, 0.64f, 0f));
        Part(rig.headPivot, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.4f, skin, "Face", keepCollider: true);
        Part(rig.headPivot, PrimitiveType.Sphere, new Vector3(0f, 0.06f, -0.04f), new Vector3(0.41f, 0.36f, 0.4f), satin, "Hair");
        Part(rig.headPivot, PrimitiveType.Cube, new Vector3(-0.2f, 0.02f, 0.02f), new Vector3(0.06f, 0.08f, 0.06f), satin, "Earpiece");
        Vector3 boomFrom = new Vector3(-0.2f, 0.02f, 0.02f), boomTo = new Vector3(-0.06f, -0.1f, 0.19f);
        var boom = Part(rig.headPivot, PrimitiveType.Cylinder, (boomFrom + boomTo) * 0.5f, new Vector3(0.012f, (boomTo - boomFrom).magnitude * 0.5f, 0.012f), satin, "MicBoom");
        boom.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (boomTo - boomFrom).normalized);
        Part(rig.headPivot, PrimitiveType.Sphere, boomTo, Vector3.one * 0.03f, satin, "Mic");
        Part(rig.headPivot, PrimitiveType.Cube, new Vector3(-0.14f, 0.15f, 0.02f), new Vector3(0.03f, 0.1f, 0.03f), satin, "HeadBand").transform.localRotation = Quaternion.Euler(0f, 0f, -40f);
        Part(rig.headPivot, PrimitiveType.Cube, new Vector3(0f, 0.207f, 0.02f), new Vector3(0.14f, 0.025f, 0.03f), satin, "HeadBand");
        Part(rig.headPivot, PrimitiveType.Cube, new Vector3(0.14f, 0.15f, 0.02f), new Vector3(0.03f, 0.1f, 0.03f), satin, "HeadBand").transform.localRotation = Quaternion.Euler(0f, 0f, 40f);
        rig.eyeL = Part(rig.headPivot, PrimitiveType.Sphere, new Vector3(-0.09f, 0.04f, 0.165f), Vector3.one * 0.075f, ink, "EyeL");
        rig.eyeR = Part(rig.headPivot, PrimitiveType.Sphere, new Vector3(0.09f, 0.04f, 0.165f), Vector3.one * 0.075f, ink, "EyeR");
        Part(rig.headPivot, PrimitiveType.Sphere, new Vector3(-0.075f, 0.06f, 0.2f), Vector3.one * 0.022f, shirt, "Glint");
        Part(rig.headPivot, PrimitiveType.Sphere, new Vector3(0.105f, 0.06f, 0.2f), Vector3.one * 0.022f, shirt, "Glint");
        Part(rig.headPivot, PrimitiveType.Sphere, new Vector3(-0.145f, -0.03f, 0.14f), new Vector3(0.08f, 0.05f, 0.03f), Blush(), "Cheek");
        Part(rig.headPivot, PrimitiveType.Sphere, new Vector3(0.145f, -0.03f, 0.14f), new Vector3(0.08f, 0.05f, 0.03f), Blush(), "Cheek");
        rig.smile = Pivot(rig.headPivot, "Smile", new Vector3(0f, -0.075f, 0f));
        for (int k = 0; k < 7; k++)
        {
            float u = (k - 3f) / 3f;
            var tooth = Part(rig.smile, PrimitiveType.Cube, new Vector3(u * 0.14f, 0.045f * u * u, 0.185f - 0.045f * u * u), new Vector3(0.045f, 0.024f, 0.02f), ink, "Smile");
            tooth.transform.localRotation = Quaternion.Euler(0f, -u * 40f, 0f);
        }

        rig.buttons = buttons.ToArray();
        rig.plates = rig.buttons;
        rig.coat = coat.ToArray();
        return rig;
    }

    static Transform ConciergeLeg(Transform root, float x, Material cloth)
    {
        Transform hip = Pivot(root, x < 0f ? "LegL" : "LegR", new Vector3(x, 0.98f, 0f));
        Part(hip, PrimitiveType.Cube, new Vector3(0f, -0.24f, 0f), new Vector3(0.135f, 0.46f, 0.135f), cloth, "Thigh");
        Part(hip, PrimitiveType.Sphere, new Vector3(0f, -0.48f, 0f), Vector3.one * 0.13f, cloth, "Knee");
        Part(hip, PrimitiveType.Cube, new Vector3(0f, -0.72f, 0f), new Vector3(0.115f, 0.44f, 0.115f), cloth, "Shin");
        Part(hip, PrimitiveType.Cube, new Vector3(0f, -0.955f, 0.07f), new Vector3(0.14f, 0.06f, 0.3f), Satin(), "Shoe");
        return hip;
    }

    static Transform ConciergeArm(Transform torso, float x, Material sleeve, Material gold, Material glove,
        out Transform fore, out Transform hand, out Renderer cuff)
    {
        Transform shoulder = Pivot(torso, x < 0f ? "ArmL" : "ArmR", new Vector3(x, 0.4f, 0f));
        Part(shoulder, PrimitiveType.Cube, new Vector3(0f, -0.2f, 0f), new Vector3(0.12f, 0.34f, 0.12f), sleeve, "UpperArm");
        Part(shoulder, PrimitiveType.Sphere, new Vector3(0f, -0.4f, 0f), Vector3.one * 0.12f, sleeve, "Elbow");
        fore = Part(shoulder, PrimitiveType.Cube, new Vector3(0f, -0.6f, 0f), new Vector3(0.105f, 0.34f, 0.105f), sleeve, "Forearm").transform;
        Part(shoulder, PrimitiveType.Cylinder, new Vector3(0f, -0.78f, 0f), new Vector3(0.135f, 0.022f, 0.135f), gold, "CuffRing");
        cuff = Part(shoulder, PrimitiveType.Sphere, new Vector3(0f, -0.78f, 0f), Vector3.one * 0.1f, WeakMat(), "Cuff", keepCollider: true);
        cuff.gameObject.AddComponent<EnemyWeakspot>();
        hand = Part(shoulder, PrimitiveType.Cube, new Vector3(0f, -0.88f, 0f), new Vector3(0.12f, 0.13f, 0.1f), glove, "Hand").transform;
        for (int k = -1; k <= 1; k++)
            Part(shoulder, PrimitiveType.Cube, new Vector3(k * 0.035f, -0.99f, 0.01f), new Vector3(0.03f, 0.1f, 0.03f), glove, "Finger");
        return shoulder;
    }
}
