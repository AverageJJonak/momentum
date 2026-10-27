using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE SUBJECT — the protagonist's body, for the moments the game looks at you
/// instead of through you — and the FAMILY, the three the vector took, built
/// from the same rig in their own builds.
///
/// DESIGN. Nothing on it is round. A doll is spheres: a sphere head, sphere
/// eyes, sphere joints. This is PLATES over a dark under-suit, the way V1 and
/// the survivors read: bone-white armour on charcoal — a chest plate with the
/// company's chevron in gold, pauldrons tilted out over the shoulders, a
/// containment collar with its light, thigh and shin guards, forearm guards,
/// gloves with a plate on the back, big angular boots with a toe cap and a
/// heel — and every joint a HINGE, a dark cylinder across the axis it bends on,
/// the way a machine's would be. Heroic proportions: seven and a half heads,
/// wide at the shoulders, narrow at the waist, long in the leg, the hands at
/// mid-thigh. The head is an angular skull (an octagon in plan, not a ball),
/// a jaw, brows that are plates, and EYES THAT ARE THE VECTOR: two glowing
/// slits in the vein's own colour, dim when it sleeps, gold when it wakes.
/// Swept hair in blocks, not a cap. The vector runs out of the heart across
/// the chest plate, up the neck to the temple, down both arms to the backs
/// of the hands, nodes at the hinges. Two coat tails hang from the belt and
/// fly out behind on a run.
///
/// The FAMILY: 0038 the father, broad, greying, a beard; 0039 the mother, long
/// hair; 0040 the sibling, small — in the programme's pale gowns, bare-armed,
/// barefoot, no plates: patients. Their veins and their eyes run violet.
///
/// ANIMATION is a set of ACTS blended into over time, with what a pose alone
/// cannot give: the pelvis and the shoulders counter-twist on a stride, the
/// knees flex through the swing and lock on the stance, the feet roll heel to
/// toe, the arms pump bent and cross a little, the body bounces on every
/// footfall, the tails lag behind, the head stays level and can be told to
/// LOOK at a point in the world over any act, the eyes blink, the fingers grip.
/// Hidden the frame the game cuts back behind the eyes.
/// </summary>
public class PlayerAvatar : MonoBehaviour
{
    public enum Act { Hang, Wake, Brace, Punch, Step, Run, Ride, Tumble, Land, Stand, Idle, Walk, Reach, Pull, Shield, Guard, Hollow }
    public enum Figure { Subject, Father, Mother, Sibling }

    /// One build of the rig: who it is, in the proportions and the colours that say so
    struct Look
    {
        public float scale, shoulder, chestW;
        public Color under, plate, gold, skin, hair, veinHot;
        public bool gown, fringe, beard, longHair;
        public string number;
    }

    static readonly Color VeinDim = new Color(0.34f, 0.2f, 0.08f), VeinGold = new Color(2.6f, 1.5f, 0.45f), VeinWhite = new Color(3.6f, 3.2f, 2.4f);
    static readonly Color VeinRot = new Color(2f, 0.3f, 0.25f), VeinRotDim = new Color(0.3f, 0.08f, 0.06f);
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

    Figure figure;
    Look look;
    Transform pelvis, torso, chest, head, armL, armR, foreL, foreR, handL, handR, fingersL, fingersR, thumbL, thumbR;
    Transform legL, legR, shinL, shinR, footL, footR, browL, browR, eyeL, eyeR, tailL, tailR, blade;
    Transform[] knuckL, knuckR;           // the second joint of each finger
    Material veinMat;
    float glow, gripL, gripR, eyeOpen = 1f, browTilt, tailSwing;
    bool hasLook;
    Vector3 lookPoint;
    float blinkAt = 2f;

    public float Glow => glow;
    public Figure Who => figure;
    public Vector3 Feet => transform.position;
    public Vector3 Chest => chest != null ? chest.TransformPoint(new Vector3(0f, 0.16f, 0.16f)) : transform.position + Vector3.up * 1.4f;
    public Vector3 Head => head != null ? head.TransformPoint(new Vector3(0f, 0.11f, 0f)) : transform.position + Vector3.up * 1.78f;
    public Vector3 Face => head != null ? head.TransformPoint(new Vector3(0f, 0.11f, 0.11f)) : Head;
    public Vector3 HandR => handR != null ? handR.TransformPoint(new Vector3(0f, -0.07f, 0f)) : Chest;
    public Vector3 HandL => handL != null ? handL.TransformPoint(new Vector3(0f, -0.07f, 0f)) : Chest;

    public static PlayerAvatar Build(Transform parent, Vector3 feet, float yaw, Figure figure = Figure.Subject)
    {
        var root = new GameObject(figure == Figure.Subject ? "PlayerAvatar" : "Figure " + figure);
        root.transform.SetParent(parent);
        root.transform.SetPositionAndRotation(feet, Quaternion.Euler(0f, yaw, 0f));
        var a = root.AddComponent<PlayerAvatar>();
        a.figure = figure;
        a.look = LookFor(figure);
        root.transform.localScale = Vector3.one * a.look.scale;
        a.Construct();
        return a;
    }

    static Look LookFor(Figure f)
    {
        Color gownCloth = new Color(0.8f, 0.8f, 0.76f), stripe = new Color(0.2f, 0.36f, 0.62f);
        switch (f)
        {
            case Figure.Father: return new Look
            {
                scale = 1.02f, shoulder = 0.32f, chestW = 0.44f, gown = true, beard = true,
                under = gownCloth, plate = gownCloth, gold = stripe, skin = new Color(0.78f, 0.62f, 0.52f), hair = new Color(0.32f, 0.3f, 0.29f),
                veinHot = VeinRot, number = "0038",
            };
            case Figure.Mother: return new Look
            {
                scale = 0.94f, shoulder = 0.27f, chestW = 0.36f, gown = true, fringe = true, longHair = true,
                under = gownCloth, plate = gownCloth, gold = stripe, skin = new Color(0.86f, 0.7f, 0.6f), hair = new Color(0.34f, 0.19f, 0.1f),
                veinHot = VeinRot, number = "0039",
            };
            case Figure.Sibling: return new Look
            {
                scale = 0.76f, shoulder = 0.26f, chestW = 0.36f, gown = true, fringe = true,
                under = gownCloth, plate = gownCloth, gold = stripe, skin = new Color(0.85f, 0.7f, 0.6f), hair = new Color(0.1f, 0.08f, 0.06f),
                veinHot = VeinRot, number = "0040",
            };
            default: return new Look
            {
                scale = 1f, shoulder = 0.3f, chestW = 0.38f, fringe = true,
                under = new Color(0.09f, 0.09f, 0.11f), plate = new Color(0.84f, 0.82f, 0.76f), gold = new Color(0.92f, 0.72f, 0.3f),
                skin = new Color(0.84f, 0.68f, 0.58f), hair = new Color(0.08f, 0.06f, 0.05f),
                veinHot = VeinGold, number = "0041",
            };
        }
    }

    static Material Lit(string key, Color c, float smooth, float metal = 0f)
    {
        if (mats.TryGetValue(key, out var m) && m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", metal);
        mats[key] = m;
        return m;
    }

    // ------------------------------------------------------------ the body

    void Construct()
    {
        string who = figure.ToString();
        bool gown = look.gown;
        Material under = Lit("under" + who, look.under, gown ? 0.15f : 0.25f);
        Material plate = Lit("plate" + who, look.plate, gown ? 0.15f : 0.55f, gown ? 0f : 0.15f);
        Material gold = Lit("gold" + who, look.gold, gown ? 0.2f : 0.8f, gown ? 0f : 0.85f);
        Material skin = Lit("skin" + who, look.skin, 0.25f);
        Material hair = Lit("hair" + who, look.hair, 0.3f);
        Material joint = Lit("joint", new Color(0.05f, 0.05f, 0.06f), 0.45f, 0.3f);
        Material boot = Lit("boot", new Color(0.07f, 0.07f, 0.08f), 0.4f);
        Material lip = Lit("lip", new Color(0.42f, 0.2f, 0.18f), 0.3f);
        veinMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        veinMat.SetColor("_BaseColor", figure == Figure.Subject ? VeinDim : VeinRotDim);
        Transform root = transform;
        float sh = look.shoulder, cw = look.chestW;
        Material limb = gown ? skin : under;          // the family are bare-armed and barefoot

        // THE PELVIS: the belt, the pouches, the tails
        pelvis = Pivot(root, "Pelvis", new Vector3(0f, 1.02f, 0f));
        Part(pelvis, PrimitiveType.Cube, new Vector3(0f, 0.02f, 0f), new Vector3(0.3f, 0.16f, 0.2f), under, "Hips");
        if (!gown)
        {
            Part(pelvis, PrimitiveType.Cube, new Vector3(0f, 0.1f, 0f), new Vector3(0.32f, 0.04f, 0.22f), plate, "Belt");
            Part(pelvis, PrimitiveType.Cube, new Vector3(0f, 0.1f, 0.115f), new Vector3(0.05f, 0.035f, 0.01f), gold, "Buckle");
            Part(pelvis, PrimitiveType.Cube, new Vector3(-0.17f, 0.02f, 0.04f), new Vector3(0.06f, 0.09f, 0.07f), under, "PouchL");
            Part(pelvis, PrimitiveType.Cube, new Vector3(0.17f, 0.02f, 0.04f), new Vector3(0.06f, 0.09f, 0.07f), under, "PouchR");
            Part(pelvis, PrimitiveType.Cube, new Vector3(0f, 0.02f, 0.11f), new Vector3(0.14f, 0.12f, 0.03f), plate, "CodPlate");
            tailL = Pivot(pelvis, "TailL", new Vector3(-0.08f, 0.08f, -0.1f));
            tailR = Pivot(pelvis, "TailR", new Vector3(0.08f, 0.08f, -0.1f));
            Part(tailL, PrimitiveType.Cube, new Vector3(0f, -0.2f, -0.01f), new Vector3(0.12f, 0.4f, 0.025f), under, "Tail").transform.localRotation = Quaternion.Euler(0f, 0f, -4f);
            Part(tailR, PrimitiveType.Cube, new Vector3(0f, -0.2f, -0.01f), new Vector3(0.12f, 0.4f, 0.025f), under, "Tail").transform.localRotation = Quaternion.Euler(0f, 0f, 4f);
        }
        legL = Leg(pelvis, -0.12f, limb, plate, boot, joint, out shinL, out footL);
        legR = Leg(pelvis, 0.12f, limb, plate, boot, joint, out shinR, out footR);

        // THE WAIST AND THE CHEST: the under-suit, then the plates on it
        torso = Pivot(root, "Torso", new Vector3(0f, 1.12f, 0f));
        Part(torso, PrimitiveType.Cube, new Vector3(0f, 0.1f, 0f), new Vector3(0.26f, 0.2f, 0.2f), under, "Abdomen");
        chest = Pivot(torso, "Chest", new Vector3(0f, 0.2f, 0f));
        Part(chest, PrimitiveType.Cube, new Vector3(0f, 0.16f, 0f), new Vector3(cw, 0.32f, 0.24f), under, "ChestUnder");
        Part(chest, PrimitiveType.Cube, new Vector3(0f, 0.16f, 0.055f), new Vector3(cw * 0.7f, 0.33f, 0.14f), under, "ChestUnderFront").transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        if (gown)
        {
            // The programme's gown: a loose tunic from the collarbone to mid-thigh, the company's stripe and the number
            Part(torso, PrimitiveType.Cube, new Vector3(0f, 0.06f, 0f), new Vector3(cw + 0.06f, 0.7f, 0.28f), plate, "Tunic");
            Part(chest, PrimitiveType.Cube, new Vector3(0f, 0.3f, 0.147f), new Vector3(cw * 0.9f, 0.035f, 0.008f), gold, "Stripe");
            Part(chest, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0.147f), new Vector3(0.12f, 0.05f, 0.008f), gold, "NumberPlate");
            LevelKit.Text(chest, chest.TransformPoint(new Vector3(0f, 0.2f, 0.153f)), chest.rotation * Quaternion.Euler(0f, 180f, 0f),
                look.number, 0.006f, new Color(0.95f, 0.95f, 0.97f), "Number", 0.11f, true, 30f, 80f);
        }
        else
        {
            // The armour: the chest plate and the chevron on it, the back plate, the abdomen plate, the collar
            Part(chest, PrimitiveType.Cube, new Vector3(0f, 0.17f, 0.135f), new Vector3(cw * 0.92f, 0.28f, 0.06f), plate, "ChestPlate").transform.localRotation = Quaternion.Euler(-6f, 0f, 0f);
            Part(chest, PrimitiveType.Cube, new Vector3(0f, 0.15f, -0.125f), new Vector3(cw * 0.88f, 0.3f, 0.05f), plate, "BackPlate");
            Part(torso, PrimitiveType.Cube, new Vector3(0f, 0.08f, 0.11f), new Vector3(0.22f, 0.13f, 0.05f), plate, "AbdomenPlate");
            Part(chest, PrimitiveType.Cube, new Vector3(-0.058f, 0.17f, 0.172f), new Vector3(0.026f, 0.15f, 0.008f), gold, "ChevronL").transform.localRotation = Quaternion.Euler(0f, 0f, -34f);
            Part(chest, PrimitiveType.Cube, new Vector3(0.058f, 0.17f, 0.172f), new Vector3(0.026f, 0.15f, 0.008f), gold, "ChevronR").transform.localRotation = Quaternion.Euler(0f, 0f, 34f);
            Part(chest, PrimitiveType.Cube, new Vector3(0.12f, 0.07f, 0.17f), new Vector3(0.09f, 0.04f, 0.008f), under, "NumberPlate");
            LevelKit.Text(chest, chest.TransformPoint(new Vector3(0.12f, 0.07f, 0.176f)), chest.rotation * Quaternion.Euler(0f, 180f, 0f),
                look.number, 0.006f, new Color(0.92f, 0.74f, 0.32f), "Number", 0.08f, true, 30f, 80f);
            Part(chest, PrimitiveType.Cylinder, new Vector3(-0.12f, 0.27f, 0.172f), new Vector3(0.035f, 0.004f, 0.035f), gold, "Badge").transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part(chest, PrimitiveType.Cylinder, new Vector3(0f, 0.35f, 0f), new Vector3(0.27f, 0.03f, 0.27f), plate, "Collar");
            Part(chest, PrimitiveType.Cylinder, new Vector3(0f, 0.365f, 0f), new Vector3(0.28f, 0.006f, 0.28f), gold, "CollarLight");
            // Pauldrons: tilted out over the shoulders, the outer edge down
            Part(chest, PrimitiveType.Cube, new Vector3(-(sh + 0.02f), 0.34f, 0f), new Vector3(0.2f, 0.09f, 0.24f), plate, "PauldronL").transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
            Part(chest, PrimitiveType.Cube, new Vector3(sh + 0.02f, 0.34f, 0f), new Vector3(0.2f, 0.09f, 0.24f), plate, "PauldronR").transform.localRotation = Quaternion.Euler(0f, 0f, -24f);
        }
        armL = Arm(chest, -sh, limb, under, plate, skin, joint, out foreL, out handL, out fingersL, out thumbL, out knuckL);
        armR = Arm(chest, sh, limb, under, plate, skin, joint, out foreR, out handR, out fingersR, out thumbR, out knuckR);

        // THE HEAD: the neck, an angular skull (an octagon in plan), the jaw, the hair in blocks, the brows, the eyes, the mouth
        Part(chest, PrimitiveType.Cylinder, new Vector3(0f, 0.37f, 0f), new Vector3(0.1f, 0.05f, 0.1f), gown ? skin : under, "Neck");
        head = Pivot(chest, "Head", new Vector3(0f, 0.38f, 0f));
        Part(head, PrimitiveType.Cube, new Vector3(0f, 0.1f, 0f), new Vector3(0.2f, 0.23f, 0.21f), skin, "Skull");
        Part(head, PrimitiveType.Cube, new Vector3(0f, 0.1f, 0f), new Vector3(0.2f, 0.23f, 0.21f), skin, "SkullB").transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        Part(head, PrimitiveType.Cube, new Vector3(0f, 0.0f, 0.03f), new Vector3(0.15f, 0.07f, 0.14f), look.beard ? hair : skin, look.beard ? "Beard" : "Jaw").transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
        Part(head, PrimitiveType.Cube, new Vector3(0f, 0.245f, -0.01f), new Vector3(0.22f, 0.07f, 0.24f), hair, "HairTop").transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
        Part(head, PrimitiveType.Cube, new Vector3(-0.115f, 0.14f, -0.03f), new Vector3(0.04f, 0.14f, 0.16f), hair, "HairSideL");
        Part(head, PrimitiveType.Cube, new Vector3(0.115f, 0.14f, -0.03f), new Vector3(0.04f, 0.14f, 0.16f), hair, "HairSideR");
        Part(head, PrimitiveType.Cube, new Vector3(0f, 0.16f, -0.11f), new Vector3(0.2f, 0.16f, 0.05f), hair, "HairBack");
        if (look.fringe) Part(head, PrimitiveType.Cube, new Vector3(0.02f, 0.225f, 0.1f), new Vector3(0.2f, 0.05f, 0.07f), hair, "Fringe").transform.localRotation = Quaternion.Euler(28f, 0f, -6f);
        if (look.longHair)
        {
            Part(head, PrimitiveType.Cube, new Vector3(0f, -0.04f, -0.1f), new Vector3(0.24f, 0.4f, 0.09f), hair, "HairFall");
            Part(head, PrimitiveType.Cube, new Vector3(-0.13f, 0.0f, 0.02f), new Vector3(0.05f, 0.3f, 0.12f), hair, "LockL");
            Part(head, PrimitiveType.Cube, new Vector3(0.13f, 0.0f, 0.02f), new Vector3(0.05f, 0.3f, 0.12f), hair, "LockR");
        }
        browL = Part(head, PrimitiveType.Cube, new Vector3(-0.05f, 0.175f, 0.108f), new Vector3(0.06f, 0.012f, 0.012f), hair, "BrowL").transform;
        browR = Part(head, PrimitiveType.Cube, new Vector3(0.05f, 0.175f, 0.108f), new Vector3(0.06f, 0.012f, 0.012f), hair, "BrowR").transform;
        eyeL = Pivot(head, "EyeL", new Vector3(-0.05f, 0.14f, 0.107f));
        eyeR = Pivot(head, "EyeR", new Vector3(0.05f, 0.14f, 0.107f));
        Part(eyeL, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.05f, 0.022f, 0.012f), veinMat, "Eye");   // the vector, looking out
        Part(eyeR, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.05f, 0.022f, 0.012f), veinMat, "Eye");
        Part(head, PrimitiveType.Cube, new Vector3(0f, 0.055f, 0.1f), new Vector3(0.05f, 0.006f, 0.008f), lip, "Mouth");

        // THE VECTOR: out of the heart, across the chest, up the neck to the temple, down both arms to the backs of the hands
        Vector3 heart = new Vector3(-0.06f, 0.2f, 0.178f);
        Node(chest, heart, 0.04f);
        Vein(chest, heart, new Vector3(-(sh - 0.04f), 0.31f, 0.1f), 0.013f);
        Vein(chest, heart, new Vector3(sh - 0.05f, 0.32f, 0.09f), 0.013f);
        Vein(chest, heart, new Vector3(0.02f, 0.36f, 0.08f), 0.011f);
        Vein(chest, heart, new Vector3(-0.1f, 0.03f, 0.17f), 0.011f);
        Vein(torso, new Vector3(-0.1f, 0.17f, 0.125f), new Vector3(0.05f, 0.02f, 0.125f), 0.009f);
        Node(chest, new Vector3(-(sh - 0.04f), 0.31f, 0.1f), 0.024f);
        Node(chest, new Vector3(sh - 0.05f, 0.32f, 0.09f), 0.024f);
        Vein(head, new Vector3(0.03f, -0.02f, 0.06f), new Vector3(0.09f, 0.1f, 0.09f), 0.009f);
        Vein(head, new Vector3(0.09f, 0.1f, 0.09f), new Vector3(0.11f, 0.2f, 0.03f), 0.008f);
        Node(head, new Vector3(0.11f, 0.2f, 0.03f), 0.018f);
        foreach (var (arm, fore, hand) in new[] { (armL, foreL, handL), (armR, foreR, handR) })
        {
            float x = arm == armL ? 1f : -1f;
            Vein(arm, new Vector3(x * 0.02f, -0.04f, 0.062f), new Vector3(0f, -0.32f, 0.06f), 0.011f);
            Node(arm, new Vector3(0f, -0.35f, 0.062f), 0.024f);
            Vein(fore, new Vector3(0f, -0.02f, 0.056f), new Vector3(x * -0.01f, -0.29f, 0.05f), 0.009f);
            Node(fore, new Vector3(x * -0.01f, -0.31f, 0.05f), 0.018f);
            Vein(hand, new Vector3(0f, -0.01f, -0.032f), new Vector3(0f, -0.08f, -0.034f), 0.007f);
        }
        SetGlow(0f);
    }

    Transform Leg(Transform pelvis, float x, Material limb, Material plate, Material boot, Material joint, out Transform shin, out Transform foot)
    {
        bool gown = look.gown;
        Transform hip = Pivot(pelvis, x < 0f ? "LegL" : "LegR", new Vector3(x, 0f, 0f));
        Part(hip, PrimitiveType.Cube, new Vector3(0f, -0.24f, 0f), new Vector3(0.16f, 0.46f, 0.17f), gown ? plate : limb, "Thigh");
        if (!gown) Part(hip, PrimitiveType.Cube, new Vector3(0f, -0.25f, 0.08f), new Vector3(0.16f, 0.3f, 0.05f), plate, "ThighPlate");
        Part(hip, PrimitiveType.Cylinder, new Vector3(0f, -0.47f, 0f), new Vector3(0.12f, 0.085f, 0.12f), joint, "Knee").transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        shin = Pivot(hip, "Shin", new Vector3(0f, -0.47f, 0f));
        Part(shin, PrimitiveType.Cube, new Vector3(0f, -0.2f, 0f), new Vector3(0.13f, 0.42f, 0.14f), limb, "Shin");
        if (!gown) Part(shin, PrimitiveType.Cube, new Vector3(0f, -0.2f, 0.07f), new Vector3(0.15f, 0.34f, 0.05f), plate, "ShinGuard");
        foot = Pivot(shin, "Foot", new Vector3(0f, -0.42f, 0f));
        if (gown)
        {
            Part(foot, PrimitiveType.Cube, new Vector3(0f, -0.09f, 0.04f), new Vector3(0.12f, 0.07f, 0.24f), limb, "Foot");
        }
        else
        {
            Part(foot, PrimitiveType.Cube, new Vector3(0f, -0.06f, 0.04f), new Vector3(0.15f, 0.12f, 0.28f), boot, "Boot");
            Part(foot, PrimitiveType.Cube, new Vector3(0f, -0.075f, 0.18f), new Vector3(0.15f, 0.08f, 0.1f), plate, "ToeCap").transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
            Part(foot, PrimitiveType.Cube, new Vector3(0f, -0.02f, 0.02f), new Vector3(0.155f, 0.06f, 0.16f), plate, "BootCuff");
            Part(foot, PrimitiveType.Cube, new Vector3(0f, -0.08f, -0.07f), new Vector3(0.14f, 0.08f, 0.09f), boot, "Heel");
            Part(foot, PrimitiveType.Cube, new Vector3(0f, -0.115f, 0.04f), new Vector3(0.16f, 0.03f, 0.31f), boot, "Sole");
        }
        return hip;
    }

    Transform Arm(Transform chest, float x, Material limb, Material under, Material plate, Material skin, Material joint,
        out Transform fore, out Transform hand, out Transform fingers, out Transform thumb, out Transform[] knuckles)
    {
        bool gown = look.gown;
        Transform shoulder = Pivot(chest, x < 0f ? "ArmL" : "ArmR", new Vector3(x, 0.3f, 0f));
        Part(shoulder, PrimitiveType.Cube, new Vector3(0f, -0.17f, 0f), new Vector3(0.11f, 0.36f, 0.11f), gown ? plate : limb, "UpperArm");
        if (!gown) Part(shoulder, PrimitiveType.Cube, new Vector3(0f, -0.13f, 0f), new Vector3(0.125f, 0.16f, 0.125f), plate, "BicepPlate");
        Part(shoulder, PrimitiveType.Cylinder, new Vector3(0f, -0.36f, 0f), new Vector3(0.1f, 0.07f, 0.1f), joint, "Elbow").transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        fore = Pivot(shoulder, "Fore", new Vector3(0f, -0.36f, 0f));
        Part(fore, PrimitiveType.Cube, new Vector3(0f, -0.15f, 0f), new Vector3(0.1f, 0.32f, 0.1f), limb, "Forearm");
        if (!gown) Part(fore, PrimitiveType.Cube, new Vector3(0f, -0.21f, 0f), new Vector3(0.13f, 0.2f, 0.13f), plate, "ForearmGuard");
        hand = Pivot(fore, "Hand", new Vector3(0f, -0.33f, 0f));
        Material glove = gown ? skin : under;
        // THE HAND, pronated the way a hanging arm's is: the palm faces back and in, the plate rides the back of it,
        // the fingers curl toward the palm in two knuckles and rest a little curled and a little splayed — never a
        // comb — and the thumb sits on the inner edge, angled forward and down, and wraps over the fingers in a fist
        Part(hand, PrimitiveType.Cube, new Vector3(0f, -0.045f, 0f), new Vector3(0.088f, 0.09f, 0.046f), glove, "Palm");
        if (!gown) Part(hand, PrimitiveType.Cube, new Vector3(0f, -0.035f, 0.028f), new Vector3(0.082f, 0.055f, 0.014f), plate, "HandPlate");
        fingers = Pivot(hand, "Fingers", new Vector3(0f, -0.088f, 0f));
        float inward = x < 0f ? 1f : -1f;   // toward the body's centre line
        knuckles = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            float len = i == 0 ? 0.036f : i == 1 ? 0.04f : i == 2 ? 0.038f : 0.032f;      // index, middle, ring, little
            Transform prox = Pivot(fingers, "Finger", new Vector3(inward * (0.031f - i * 0.0207f), 0f, 0f));
            prox.localRotation = Quaternion.Euler(0f, 0f, inward * (1.5f - i) * 5f);       // the splay, out from the middle
            Part(prox, PrimitiveType.Cube, new Vector3(0f, -len * 0.5f, 0f), new Vector3(0.019f, len, 0.02f), glove, "Proximal");
            knuckles[i] = Pivot(prox, "Knuckle", new Vector3(0f, -len, 0f));
            Part(knuckles[i], PrimitiveType.Cube, new Vector3(0f, -len * 0.42f, 0f), new Vector3(0.018f, len * 0.86f, 0.019f), glove, "Distal");
        }
        thumb = Pivot(hand, "Thumb", new Vector3(inward * 0.048f, -0.028f, -0.006f));
        Part(thumb, PrimitiveType.Cube, new Vector3(0f, -0.028f, 0f), new Vector3(0.019f, 0.058f, 0.02f), glove, "ThumbBone");
        Part(thumb, PrimitiveType.Cube, new Vector3(0f, -0.07f, 0f), new Vector3(0.017f, 0.032f, 0.018f), glove, "ThumbTip");
        thumb.localRotation = Quaternion.Euler(28f, 0f, inward * 38f);
        return shoulder;
    }

    /// A filament of the vector between two local points: two straight runs with a kink, in the vein material
    void Vein(Transform parent, Vector3 a, Vector3 b, float thick)
    {
        Vector3 mid = (a + b) * 0.5f + Vector3.Cross((b - a).normalized, Vector3.forward) * 0.02f;
        Run(parent, a, mid, thick);
        Run(parent, mid, b, thick * 0.85f);
    }

    void Run(Transform parent, Vector3 a, Vector3 b, float thick)
    {
        Vector3 d = b - a;
        var r = Part(parent, PrimitiveType.Cube, (a + b) * 0.5f, new Vector3(thick, d.magnitude, thick), veinMat, "Vein");
        r.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
    }

    void Node(Transform parent, Vector3 at, float size) => Part(parent, PrimitiveType.Cube, at, Vector3.one * size, veinMat, "VeinNode").transform.localRotation = Quaternion.Euler(45f, 0f, 45f);

    static Transform Pivot(Transform parent, string name, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        return go.transform;
    }

    static Renderer Part(Transform parent, PrimitiveType type, Vector3 localPos, Vector3 scale, Material mat, string name)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.layer = 2;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        return r;
    }

    // ------------------------------------------------------------ the vector's light, the face, the blade, the look

    /// 0 = dim in the suit, 1 = lit (the flow's gold; the rot's violet in the family), 2 = white-hot. The eyes go with it.
    public void SetGlow(float g)
    {
        glow = g;
        Color dim = figure == Figure.Subject ? VeinDim : VeinRotDim;
        Color c = g <= 1f ? Color.Lerp(dim, look.veinHot, g) : Color.Lerp(look.veinHot, VeinWhite, Mathf.Clamp01(g - 1f));
        veinMat.SetColor("_BaseColor", c);
    }

    /// The eyes: 0 shut, 1 open, 1.3 wide. The brows: -1 down (anger, effort), 1 up (fear, surprise)
    public void SetFace(float open, float brow)
    {
        eyeOpen = open;
        browTilt = brow;
    }

    /// The head follows a point in the world on top of whatever act is playing; null lets the act have it
    public void LookAt(Vector3? point)
    {
        hasLook = point.HasValue;
        if (point.HasValue) lookPoint = point.Value;
    }

    /// The hands are the kit: the avatar carries no blade (the cane went with the simplification; the hook stays for the callers)
    public void Blade(bool on)
    {
        if (blade != null) blade.gameObject.SetActive(false);
    }

    public void Hide() => gameObject.SetActive(false);
    public void Show() => gameObject.SetActive(true);

    // ------------------------------------------------------------ the acts

    /// One animated joint: its angles, their velocity, and the spring that carries them toward the act's pose
    struct Joint { public Transform t; public Vector3 e, v; public bool live; }

    Joint jPelvis, jTorso, jChest, jHead, jArmL, jArmR, jForeL, jForeR, jLegL, jLegR, jShinL, jShinR, jFootL, jFootR;
    Vector3 torsoOff = new Vector3(0f, 1.12f, 0f), pelvisOff = new Vector3(0f, 1.02f, 0f), torsoVel, pelvisVel;
    float phase, travelSpeed, seed;
    Vector3 lastPos;
    bool hasLast, jointsReady;

    void ReadyJoints()
    {
        if (jointsReady) return;
        jointsReady = true;
        seed = Mathf.Repeat(transform.position.x * 3.7f + transform.position.z * 1.3f, 100f);
        jPelvis.t = pelvis; jTorso.t = torso; jChest.t = chest; jHead.t = head;
        jArmL.t = armL; jArmR.t = armR; jForeL.t = foreL; jForeR.t = foreR;
        jLegL.t = legL; jLegR.t = legR; jShinL.t = shinL; jShinR.t = shinR; jFootL.t = footL; jFootR.t = footR;
    }

    /// A damped spring toward the target angles. Stiffness is `omega` (rad/s); `zeta` under one overshoots and settles,
    /// which is the follow-through a body has and a lerp never does. A huge `dt` snaps: the builders pose a figure
    /// once with dt 10, and the first frame of a live one lands on its pose rather than swinging into it.
    static void Spring(ref Joint j, Vector3 target, float omega, float zeta, float dt)
    {
        if (j.t == null) return;
        if (dt >= 0.5f || !j.live) { j.e = target; j.v = Vector3.zero; j.live = true; j.t.localRotation = Quaternion.Euler(j.e); return; }
        Damp(ref j.e.x, ref j.v.x, target.x, omega, zeta, dt);
        Damp(ref j.e.y, ref j.v.y, target.y, omega, zeta, dt);
        Damp(ref j.e.z, ref j.v.z, target.z, omega, zeta, dt);
        j.t.localRotation = Quaternion.Euler(j.e);
    }

    static void SpringV(ref Vector3 x, ref Vector3 v, Vector3 target, float omega, float zeta, float dt)
    {
        if (dt >= 0.5f) { x = target; v = Vector3.zero; return; }
        Damp(ref x.x, ref v.x, target.x, omega, zeta, dt);
        Damp(ref x.y, ref v.y, target.y, omega, zeta, dt);
        Damp(ref x.z, ref v.z, target.z, omega, zeta, dt);
    }

    /// One damped-oscillator step in CLOSED FORM: the exact solution for the frame's length, whatever it is. A
    /// stepped integrator goes unstable when omega × dt gets near two — a stiff joint on a heavy frame, which is
    /// exactly the frame the glass shatters and sixty sparks spawn on — and this cannot.
    static void Damp(ref float x, ref float v, float target, float omega, float zeta, float dt)
    {
        float e = x - target;
        if (zeta < 0.999f)
        {
            float wd = omega * Mathf.Sqrt(1f - zeta * zeta);
            float decay = Mathf.Exp(-zeta * omega * dt);
            float c = Mathf.Cos(wd * dt), s = Mathf.Sin(wd * dt);
            float a = e, b = (v + zeta * omega * e) / wd;
            x = target + decay * (a * c + b * s);
            v = decay * ((b * wd - zeta * omega * a) * c - (a * wd + zeta * omega * b) * s);
        }
        else
        {
            float decay = Mathf.Exp(-omega * dt);
            float b = v + omega * e;
            x = target + decay * (e + b * dt);
            v = decay * (v - omega * dt * b);
        }
    }

    /// Organic drift, -1..1: Perlin, not sine, so nothing a still body does ever repeats
    float Drift(float t, float f, float c) => (Mathf.PerlinNoise(seed + t * f, c) - 0.5f) * 2f;

    /// One act per moment. `k` is the act's own progress (the big acts wind up before they go), `t` the clock the
    /// still acts breathe and drift on. The gait does not run on the clock at all: it runs on the distance the body
    /// is actually moved through the world, so the stance foot stays where it was put and a body that stops stands.
    public void Animate(Act act, float k, float t, float dt)
    {
        ReadyJoints();

        // How far the body was moved since the last frame, and how fast
        Vector3 p = transform.position;
        float travel = 0f;
        if (hasLast)
        {
            Vector3 d = p - lastPos; d.y = 0f;
            travel = d.magnitude;
            if (travel > 2f) travel = 0f;                              // a teleport is not a step
            travelSpeed = Mathf.Lerp(travelSpeed, dt > 1e-4f ? travel / dt : 0f, 1f - Mathf.Exp(-10f * dt));
        }
        lastPos = p;
        hasLast = true;

        float tp = 0f, ty = 0f, cp = 0f, hp = 0f, hy = 0f, py = 0f, pr = 0f;          // waist, chest, head, pelvis
        Vector3 aL = Vector3.zero, aR = Vector3.zero, fL = Vector3.zero, fR = Vector3.zero;
        float lL = 4f, lR = 4f, sL = 5f, sR = 5f, ftL = 0f, ftR = 0f;                  // hips, shins, feet: never dead straight
        float gL = 0f, gR = 0f, bob = 0f, sway = 0f, eyes = 1f, brow = 0f, tails = 2f;
        float breath = 0.25f, breathAmp = 1f;
        float omega = 11f;                                                             // the core's stiffness for this act
        bool blink = false, gait = false, still = false;
        float hipAmp = 0f;
        switch (act)
        {
            case Act.Hang:
                // Slack in the pod: the head down, the arms hanging, the knees soft, the drift of something not awake
                tp = 12f; cp = 6f; hp = 44f;
                aL = new Vector3(8f, 0f, 8f); aR = new Vector3(8f, 0f, -8f);
                fL = fR = new Vector3(-10f, 0f, 0f); sL = sR = 6f; lL = lR = 3f;
                eyes = 0.04f; brow = 0.1f; still = true; breath = 0.16f; breathAmp = 0.6f;
                omega = 6f;
                break;
            case Act.Hollow:
                // What the vector left: hanging, the head fallen, a sway that is not a breath
                tp = 20f; cp = 10f; hp = 52f; hy = 12f * Drift(t, 0.08f, 11f);
                aL = new Vector3(5f, 0f, 14f); aR = new Vector3(5f, 0f, -14f);
                fL = fR = new Vector3(-6f, 0f, 0f); sL = sR = 12f; lL = lR = 7f;
                eyes = 0.12f; brow = 0.3f; still = true; breath = 0.09f; breathAmp = 0.5f;
                omega = 5f;
                break;
            case Act.Wake:
                // The head comes up past level and settles, the eyes come open on a flicker, the hands find the glass
                tp = Mathf.Lerp(12f, 2f, k); cp = Mathf.Lerp(6f, 0f, k);
                hp = k < 0.85f ? Mathf.Lerp(44f, -13f, k / 0.85f) : Mathf.Lerp(-13f, -6f, (k - 0.85f) / 0.15f);
                aL = new Vector3(Mathf.Lerp(8f, -20f, k), 0f, 10f); aR = new Vector3(Mathf.Lerp(8f, -20f, k), 0f, -10f);
                fL = fR = new Vector3(Mathf.Lerp(-10f, -40f, k), 0f, 0f); sL = sR = 6f * (1f - k) + 4f;
                eyes = Mathf.SmoothStep(0f, 1.25f, k) + 0.35f * Mathf.Max(0f, Mathf.Sin(t * 23f)) * (1f - k) * Mathf.Clamp01(k * 4f);
                brow = Mathf.Lerp(0.1f, 0.8f, k); still = true; breath = Mathf.Lerp(0.16f, 0.6f, k); breathAmp = Mathf.Lerp(0.6f, 1.8f, k);
                omega = 9f;
                break;
            case Act.Brace:
                // Both hands flat on the glass, the shoulders into it, the head down and the jaw set
                tp = 10f; cp = 8f; hp = 8f;
                aL = new Vector3(-80f, 0f, 10f); aR = new Vector3(-80f, 0f, -10f);
                fL = fR = new Vector3(-12f, 0f, 0f); lL = -8f; lR = 10f; sL = 10f; sR = 14f;
                eyes = 0.7f; brow = -0.9f; still = true; breath = 0.7f; breathAmp = 2f;
                omega = 13f;
                break;
            case Act.Punch:
                if (k < 0.3f)
                {
                    // The wind-up: the fist drawn back to the hip, the shoulders turned away from it, the weight back
                    tp = 6f; ty = -26f; py = -12f; cp = 2f; hp = 2f; hy = 10f;
                    aR = new Vector3(38f, 0f, -22f); fR = new Vector3(-72f, 0f, 0f); gR = 1f;
                    aL = new Vector3(-48f, 0f, 24f); fL = new Vector3(-60f, 0f, 0f); gL = 0.8f;
                    lL = -6f; lR = 14f; sL = 6f; sR = 20f;   // the weight back on the right leg
                    omega = 22f;
                }
                else
                {
                    // The strike: through, the shoulder behind it, the hips turned into it, the left back for the balance
                    float kk = (k - 0.3f) / 0.7f;
                    tp = 12f; ty = Mathf.Lerp(-26f, 18f, kk); py = Mathf.Lerp(-12f, 10f, kk); cp = 6f; hp = 0f; hy = 8f;
                    aR = new Vector3(-100f, 6f, -6f); fR = new Vector3(-4f, 0f, 0f); gR = 1f;
                    aL = new Vector3(-30f, 0f, 22f); fL = new Vector3(-70f, 0f, 0f); gL = 0.8f;
                    lL = 14f; lR = -34f; sL = 10f; sR = 26f; ftR = -10f;   // the right foot steps into it
                    tp = 16f;
                    omega = 42f;
                }
                eyes = 0.6f; brow = -1f;
                break;
            case Act.Step:
            case Act.Walk:
                gait = true; hipAmp = 26f; breath = 0.35f;
                break;
            case Act.Run:
                gait = true; hipAmp = 54f; breath = 0.8f; breathAmp = 2f;
                break;
            case Act.Ride:
                // Standing in the cage: one hand up on the rail and gripping it, the other loose, the head up and turning
                tp = 3f; hp = -12f;
                aR = new Vector3(-118f, 0f, -28f); fR = new Vector3(-20f, 0f, 0f); gR = 1f;
                aL = new Vector3(8f, 0f, 10f); fL = new Vector3(-12f, 0f, 0f);
                lL = -4f; lR = 5f; sL = 6f; sR = 8f;
                still = true; breath = 0.5f; breathAmp = 1.6f;
                omega = 12f;
                break;
            case Act.Tumble:
                // Thrown: curled, and flailing — the arms grabbing at nothing, the legs kicking (the root does the spinning)
                tp = 30f; cp = 10f; hp = 26f;
                aL = new Vector3(-70f + 40f * Mathf.Sin(t * 13f), 0f, 30f + 15f * Mathf.Sin(t * 7f)); aR = new Vector3(-70f + 40f * Mathf.Cos(t * 11f), 0f, -30f - 15f * Mathf.Cos(t * 6f));
                fL = new Vector3(-90f + 30f * Mathf.Sin(t * 9f), 0f, 0f); fR = new Vector3(-90f + 30f * Mathf.Cos(t * 10f), 0f, 0f);
                lL = -50f + 25f * Mathf.Sin(t * 9f); lR = -40f + 25f * Mathf.Cos(t * 8f); sL = 90f + 20f * Mathf.Sin(t * 12f); sR = 80f + 20f * Mathf.Cos(t * 11f);
                gL = gR = 0.6f; eyes = 1.3f; brow = 1f; tails = 70f + 20f * Mathf.Sin(t * 15f);
                omega = 20f;
                break;
            case Act.Land:
                // Down on one knee and a hand, the head coming up, the breath hard
                tp = 38f; cp = 6f; hp = -6f;
                aL = new Vector3(-52f, 0f, 26f); fL = new Vector3(-28f, 0f, 0f); gL = 0.2f;
                aR = new Vector3(-16f, 0f, -12f); fR = new Vector3(-58f, 0f, 0f);
                lL = -72f; lR = -36f; sL = 104f; sR = 82f; ftL = 20f;
                eyes = 0.6f; brow = -0.6f; tails = 10f; still = true; breath = 0.9f; breathAmp = 2.4f;
                omega = 15f;
                break;
            case Act.Stand:
                // Up, and looking up: the arms loose at the sides, the breath coming down, the eyes wide on what is over you
                tp = 1f; cp = -2f; hp = -24f;
                aL = new Vector3(6f, 0f, 9f); aR = new Vector3(6f, 0f, -9f);
                fL = fR = new Vector3(-10f, 0f, 0f);
                eyes = 1.15f; brow = 0.5f; blink = true; still = true; breath = 0.55f; breathAmp = 1.8f;
                omega = 5f;
                break;
            case Act.Idle:
                // Standing: breathing, the weight shifting, the head drifting, a blink now and then
                tp = 2f; hp = 2f;
                aL = new Vector3(4f, 0f, 9f); aR = new Vector3(4f, 0f, -9f);
                fL = fR = new Vector3(-12f, 0f, 0f);
                lL = lR = 3f; sL = sR = 5f;
                blink = true; still = true;
                omega = 6f;
                break;
            case Act.Reach:
                // The right hand out and up for the lever, the body leaning in behind it, the left back for balance
                tp = 8f; ty = -10f; py = 6f; cp = 6f; hp = -4f; hy = 6f;
                aR = new Vector3(-102f, -10f, -16f); fR = new Vector3(-8f, 0f, 0f); gR = 0.15f;
                aL = new Vector3(18f, 0f, 16f); fL = new Vector3(-16f, 0f, 0f);
                lL = 10f; lR = -12f; sL = 12f; sR = 6f;
                eyes = 0.9f; brow = -0.4f; still = true; breath = 0.5f; breathAmp = 1.4f;
                omega = 12f;
                break;
            case Act.Pull:
                if (k < 0.15f)
                {
                    // The hand closes on the lever first: a beat of grip before the haul
                    tp = 8f; ty = -10f; py = 6f; cp = 6f; hp = -4f; hy = 6f;
                    aR = new Vector3(-106f, -10f, -16f); fR = new Vector3(-8f, 0f, 0f); gR = 1f;
                    aL = new Vector3(18f, 0f, 16f); fL = new Vector3(-16f, 0f, 0f);
                    lL = 10f; lR = -12f; sL = 12f; sR = 6f;
                    omega = 18f;
                }
                else
                {
                    // The haul: the arm comes from up to down with the shoulder and the whole weight on it
                    float kk = (k - 0.15f) / 0.85f;
                    tp = Mathf.Lerp(8f, 26f, kk); ty = Mathf.Lerp(-10f, 4f, kk); py = Mathf.Lerp(6f, -4f, kk); cp = Mathf.Lerp(6f, 12f, kk); hp = Mathf.Lerp(-4f, 12f, kk);
                    aR = new Vector3(Mathf.Lerp(-106f, -30f, kk), -10f, -16f); fR = new Vector3(-8f, 0f, 0f); gR = 1f;
                    aL = new Vector3(Mathf.Lerp(18f, 30f, kk), 0f, 20f); fL = new Vector3(-40f, 0f, 0f); gL = 0.7f;
                    lL = 12f; lR = -14f; sL = 16f; sR = 8f;
                    omega = 34f;
                }
                eyes = 0.55f; brow = -1f; breath = 0.8f; breathAmp = 2f;
                break;
            case Act.Shield:
                // The forearm thrown across the face, turned from it, a step back
                tp = 10f; ty = 18f; py = 8f; cp = 6f; hp = 14f; hy = 30f;
                aL = new Vector3(-124f, 0f, 44f); fL = new Vector3(-112f, 0f, 0f); gL = 0.5f;
                aR = new Vector3(-30f, 0f, -30f); fR = new Vector3(-40f, 0f, 0f); gR = 0.6f;
                lL = 6f; lR = -22f; sL = 8f; sR = 22f;
                eyes = 0.3f; brow = 1f; tails = 30f;
                omega = 28f;
                break;
            case Act.Guard:
                // The stance: knees soft and the weight low, the blade held low and forward in the right fist, the
                // left arm out for the balance, the eyes narrowed on whatever is in front of you
                tp = 7f; ty = -14f; py = -8f; cp = 4f; hp = 2f; hy = 6f;
                aR = new Vector3(-54f, 0f, -26f); fR = new Vector3(-72f, 0f, 16f); gR = 1f;
                aL = new Vector3(-32f, 0f, 38f); fL = new Vector3(-48f, 0f, 0f); gL = 0.45f;
                lL = 14f; lR = -12f; sL = 20f; sR = 22f;
                eyes = 0.8f; brow = -0.7f; blink = true; still = true; breath = 0.45f; breathAmp = 1.4f;
                omega = 11f;
                break;
        }

        if (gait)
        {
            // THE STRIDE, on the ground it covers: the phase advances by distance travelled over the cycle's length,
            // so the stance foot stays where it was put and the cycle slows and stops when the body does. The hips
            // swing against the shoulders, the knee snaps up through the swing and locks on the stance, the foot
            // rolls off the toe and lands on the heel, the arms pump opposite and bent, the pelvis rolls and shifts
            // over the stance foot, the body bounces — up over the straight leg on a walk, down into the stance on a run
            bool run = act == Act.Run;
            float ampRad = hipAmp * Mathf.Deg2Rad;
            float stride = ampRad * 3.62f;
            phase += travel / stride * Mathf.PI * 2f;
            float g = Mathf.Clamp01(travelSpeed / (run ? 1.2f : 0.5f));
            float sn = Mathf.Sin(phase), cs = Mathf.Cos(phase);
            float knL = Mathf.Max(0f, -Mathf.Sin(phase - 0.9f)), knR = Mathf.Max(0f, Mathf.Sin(phase - 0.9f));
            float lean = run ? 4f : 0f, kneeBase = run ? 22f : 8f, kneeSwing = run ? 78f : 48f, toe = run ? 32f : 18f, heel = run ? 16f : 12f;
            lL = Mathf.Lerp(4f, sn * hipAmp - lean, g); lR = Mathf.Lerp(4f, -sn * hipAmp - lean, g);
            sL = Mathf.Lerp(5f, kneeBase + kneeSwing * knL, g); sR = Mathf.Lerp(5f, kneeBase + kneeSwing * knR, g);
            ftL = (toe * Mathf.Max(0f, sn) - heel * Mathf.Max(0f, -sn)) * g; ftR = (toe * Mathf.Max(0f, -sn) - heel * Mathf.Max(0f, sn)) * g;
            float armAmp = run ? 60f : 22f, armBack = run ? 22f : 0f, armIn = run ? 16f : 8f;
            aL = new Vector3(Mathf.Lerp(4f, -sn * armAmp - armBack, g), run ? -10f * g : 0f, armIn);
            aR = new Vector3(Mathf.Lerp(4f, sn * armAmp - armBack, g), run ? 10f * g : 0f, -armIn);
            fL = fR = new Vector3(Mathf.Lerp(-12f, run ? -96f : -28f, g), 0f, 0f);
            gL = gR = run ? 0.55f * g : 0f;
            tp = Mathf.Lerp(2f, run ? 17f : 3f, g); cp = run ? 4f * g : 0f; hp = Mathf.Lerp(2f, run ? -16f : -2f, g);
            ty = -sn * (run ? 8f : 4f) * g; py = sn * (run ? 10f : 6f) * g; pr = cs * 4f * g;
            bob = (run ? Mathf.Abs(sn) * 0.06f : (1f - Mathf.Abs(sn)) * 0.03f) * g;
            sway = cs * (run ? 0.02f : 0.014f) * g;
            tails = Mathf.Lerp(2f, (run ? 42f : 14f) + (run ? 10f : 6f) * Mathf.Abs(sn), g);
            eyes = run ? 0.85f : 1f; brow = run ? -0.5f : 0f;
            omega = run ? 20f : 14f;
        }
        if (still)
        {
            // A body that is standing still is never still: the weight shifts, the head drifts, the arms hang and move
            tp += Drift(t, 0.23f, 1f) * 1.2f; ty += Drift(t, 0.17f, 2f) * 2f; hp += Drift(t, 0.31f, 3f) * 2.5f; hy += Drift(t, 0.27f, 4f) * 4f;
            aL.x += Drift(t, 0.2f, 5f) * 2.5f; aR.x += Drift(t, 0.2f, 6f) * 2.5f; aL.z += Drift(t, 0.15f, 7f) * 1.5f; aR.z -= Drift(t, 0.15f, 8f) * 1.5f;
            sway += Drift(t, 0.12f, 9f) * 0.008f; py += Drift(t, 0.1f, 10f) * 1.5f;
            lL += Drift(t, 0.09f, 12f) * 1.5f; lR -= Drift(t, 0.09f, 12f) * 1.5f;
        }
        // Breathing: the chest lifts and opens, faster and deeper after effort
        float br = Mathf.Sin(t * Mathf.PI * 2f * breath);
        cp += br * 1.1f * breathAmp;
        bob += br * 0.003f * breathAmp;
        // A blink, for the acts that stand still long enough to need one
        if (blink)
        {
            if (t >= blinkAt) blinkAt = t + 2.8f + Mathf.Repeat(t * 7.31f, 2.4f);
            if (t > blinkAt - 0.14f) eyes = Mathf.Min(eyes, 0.05f);
        }

        // The springs: the core critically damped, the extremities under-damped so they overshoot and settle — the
        // follow-through — and the limbs stiff on a stride so the gait is not filtered away
        float limbOmega = gait ? 45f : omega * 1.15f;
        float limbZeta = gait ? 0.9f : 0.8f, tipZeta = gait ? 0.9f : 0.72f;
        Spring(ref jPelvis, new Vector3(0f, py, pr), omega, 1f, dt);
        Spring(ref jTorso, new Vector3(tp, ty, 0f), omega, 1f, dt);
        Spring(ref jChest, new Vector3(cp, 0f, 0f), omega, 0.95f, dt);
        Spring(ref jArmL, aL, limbOmega, limbZeta, dt);
        Spring(ref jArmR, aR, limbOmega, limbZeta, dt);
        Spring(ref jForeL, fL, limbOmega, tipZeta, dt);
        Spring(ref jForeR, fR, limbOmega, tipZeta, dt);
        Spring(ref jLegL, new Vector3(lL, 0f, 0f), limbOmega, limbZeta, dt);
        Spring(ref jLegR, new Vector3(lR, 0f, 0f), limbOmega, limbZeta, dt);
        Spring(ref jShinL, new Vector3(sL, 0f, 0f), limbOmega, tipZeta, dt);
        Spring(ref jShinR, new Vector3(sR, 0f, 0f), limbOmega, tipZeta, dt);
        Spring(ref jFootL, new Vector3(ftL, 0f, 0f), limbOmega, gait ? 0.9f : 0.7f, dt);
        Spring(ref jFootR, new Vector3(ftR, 0f, 0f), limbOmega, gait ? 0.9f : 0.7f, dt);
        SpringV(ref torsoOff, ref torsoVel, new Vector3(sway, 1.12f + bob, 0f), omega + 4f, 0.9f, dt);
        SpringV(ref pelvisOff, ref pelvisVel, new Vector3(sway * 0.6f, 1.02f + bob * 0.8f, 0f), omega + 4f, 0.9f, dt);
        if (torso != null) torso.localPosition = torsoOff;
        if (pelvis != null) pelvis.localPosition = pelvisOff;

        // The tails lag behind the body and fly out on a run
        tailSwing = Mathf.Lerp(tailSwing, tails + bob * 120f, dt >= 0.5f ? 1f : 1f - Mathf.Exp(-5f * dt));
        if (tailL != null) tailL.localRotation = Quaternion.Euler(-tailSwing, 0f, 6f + tailSwing * 0.1f);
        if (tailR != null) tailR.localRotation = Quaternion.Euler(-tailSwing, 0f, -6f - tailSwing * 0.1f);

        // The head: the act's pose, or the point it was told to look at, within what a neck can do — and it leads
        if (head != null)
        {
            Vector3 want = new Vector3(hp, hy, 0f);
            if (hasLook && chest != null)
            {
                Vector3 dir = lookPoint - head.position;
                if (dir.sqrMagnitude > 0.01f)
                {
                    Quaternion local = Quaternion.Inverse(chest.rotation) * Quaternion.LookRotation(dir.normalized, Vector3.up);
                    Vector3 e = local.eulerAngles;
                    float ex = e.x > 180f ? e.x - 360f : e.x, ey = e.y > 180f ? e.y - 360f : e.y;
                    want = new Vector3(Mathf.Clamp(ex, -45f, 40f), Mathf.Clamp(ey, -75f, 75f), 0f);
                }
            }
            Spring(ref jHead, want, omega * 1.3f, 0.85f, dt);
        }

        // The hands and the face
        float s = dt >= 0.5f ? 1f : 1f - Mathf.Exp(-omega * dt);
        gripL = Mathf.Lerp(gripL, gL, s);
        gripR = Mathf.Lerp(gripR, gR, s);
        float curlL = 0.12f + gripL * 0.88f, curlR = 0.12f + gripR * 0.88f;   // a hand at rest is never flat
        if (fingersL != null) fingersL.localRotation = Quaternion.Euler(curlL * 80f, 0f, 0f);
        if (fingersR != null) fingersR.localRotation = Quaternion.Euler(curlR * 80f, 0f, 0f);
        if (knuckL != null) foreach (var kn in knuckL) if (kn != null) kn.localRotation = Quaternion.Euler(curlL * 95f, 0f, 0f);
        if (knuckR != null) foreach (var kn in knuckR) if (kn != null) kn.localRotation = Quaternion.Euler(curlR * 95f, 0f, 0f);
        if (thumbL != null) thumbL.localRotation = Quaternion.Euler(28f + gripL * 45f, 0f, 38f - gripL * 30f);
        if (thumbR != null) thumbR.localRotation = Quaternion.Euler(28f + gripR * 45f, 0f, -38f + gripR * 30f);
        eyeOpen = Mathf.Lerp(eyeOpen, eyes, Mathf.Min(1f, s * 2.5f));
        browTilt = Mathf.Lerp(browTilt, brow, s);
        if (eyeL != null) eyeL.localScale = new Vector3(1f, Mathf.Clamp(eyeOpen, 0.04f, 1.35f), 1f);
        if (eyeR != null) eyeR.localScale = new Vector3(1f, Mathf.Clamp(eyeOpen, 0.04f, 1.35f), 1f);
        if (browL != null) { browL.localRotation = Quaternion.Euler(0f, 0f, -browTilt * 14f); browL.localPosition = new Vector3(-0.05f, 0.175f + browTilt * 0.012f, 0.108f); }
        if (browR != null) { browR.localRotation = Quaternion.Euler(0f, 0f, browTilt * 14f); browR.localPosition = new Vector3(0.05f, 0.175f + browTilt * 0.012f, 0.108f); }
    }
}
