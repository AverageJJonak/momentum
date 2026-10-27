using UnityEngine;

/// <summary>
/// The arms' geometry: primitives only, no colliders, no shadows, every piece on the Viewmodel layer. The hand's
/// frame: the origin is the WRIST, +Z runs out along the fingers, +Y is the back of the hand, the thumb sits on
/// the inner side (−X for the right hand, +X for the left). Sockets are authored against that frame by the weapon
/// files. The knuckles' brass bar lives under a "Fist" node (the opening cutscene recolours its thin plates by name)
/// and is always shown: the knuckles are the kit. ~30 renderers per arm.
/// </summary>
public partial class ArmRig
{
    partial class Hand
    {
        public Transform shoulder, elbow;          // pivots under the root: the shoulder looks at the elbow, the elbow at the wrist
        public Transform upperGeo, foreGeo, cuff, link;
        public Transform palm, fistNode, grip;
        public Renderer knuckleEdge;
        public readonly Transform[,] phalanx = new Transform[4, 3];
        public Transform thumbBase, thumbTip;
    }

    static Material cream;
    static Material Cream => cream != null ? cream : cream = RigDesign.Lit(new Color(0.86f, 0.82f, 0.72f), 0.05f, 0.4f);
    static Material knuckleDimMat;
    static Material KnuckleDim => knuckleDimMat != null ? knuckleDimMat : knuckleDimMat = RigDesign.Unlit(new Color(0.08f, 0.2f, 0.22f));

    const float FingerW0 = 0.019f, FingerW1 = 0.017f, FingerW2 = 0.015f;
    static readonly float[] FingerScale = { 1f, 1.08f, 1f, 0.82f };
    static readonly float[] FingerLen = { 0.038f, 0.025f, 0.019f };
    static readonly float[] FingerX = { -0.031f, -0.010f, 0.011f, 0.032f };   // index → pinky, thumb side negative (times the hand's sign)

    void Build()
    {
        Root = new GameObject("Gauntlets").transform;
        Root.SetParent(transform, false);
        Root.localPosition = Vector3.zero;
        Root.localRotation = Quaternion.identity;
        Root.localScale = Vector3.one;
        for (int i = 0; i < 2; i++)
        {
            var h = hands[i];
            h.side = (HandSide)i;
            h.sign = i == 0 ? 1f : -1f;
            BuildArm(h);
        }
        Viewmodel.Apply(Root);
    }

    void BuildArm(Hand h)
    {
        int layer = Viewmodel.Layer;
        Material suede = RigDesign.Suede, charcoal = RigDesign.Charcoal, brass = RigDesign.Brass;
        string s = h.side == HandSide.Right ? "R" : "L";
        float L1 = config != null ? config.viewmodelUpperArm : 0.32f, L2 = config != null ? config.viewmodelForearm : 0.29f;

        // THE UPPER ARM: a charcoal sleeve from the shoulder pivot to the elbow, the elbow a ball under the sleeve
        h.shoulder = new GameObject("Shoulder" + s).transform;
        h.shoulder.SetParent(Root, false);
        h.upperGeo = RigDesign.Piece(h.shoulder, new Vector3(0f, 0f, L1 * 0.5f), new Vector3(0.088f, 0.094f, L1), charcoal, layer).transform;
        Ball(h.shoulder, Vector3.zero, 0.096f, charcoal);
        // THE FOREARM: the sleeve to the cream shirt cuff with its brass cufflink on the outer side, a ball at the elbow
        h.elbow = new GameObject("Elbow" + s).transform;
        h.elbow.SetParent(Root, false);
        Ball(h.elbow, Vector3.zero, 0.084f, charcoal);
        h.foreGeo = RigDesign.Piece(h.elbow, new Vector3(0f, 0f, L2 * 0.5f - 0.02f), new Vector3(0.076f, 0.072f, L2 - 0.05f), charcoal, layer).transform;
        h.cuff = RigDesign.Disc(h.elbow, new Vector3(0f, 0f, L2 - 0.028f), 0.084f, 0.042f, Cream, layer).transform;
        h.link = RigDesign.Piece(h.elbow, new Vector3(h.sign * 0.044f, 0.012f, L2 - 0.028f), new Vector3(0.008f, 0.012f, 0.012f), brass, layer).transform;

        // THE HAND, at the wrist: a short flattened suede cylinder for the wrist (narrower than the cuff), the palm, the heel of the thumb
        h.hand = new GameObject("Hand" + s).transform;
        h.hand.SetParent(Root, false);
        var wrist = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.DestroyImmediate(wrist.GetComponent<Collider>());
        wrist.layer = layer;
        wrist.transform.SetParent(h.hand, false);
        wrist.transform.localPosition = new Vector3(0f, 0f, 0.006f);
        wrist.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        wrist.transform.localScale = new Vector3(0.072f, 0.016f, 0.046f);   // wide across, short along the arm, flat top to bottom
        var wr = wrist.GetComponent<MeshRenderer>();
        wr.sharedMaterial = suede;
        wr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        h.palm = RigDesign.Piece(h.hand, new Vector3(0f, 0f, 0.052f), new Vector3(0.085f, 0.036f, 0.092f), suede, layer).transform;
        RigDesign.Piece(h.hand, new Vector3(-h.sign * 0.03f, -0.011f, 0.032f), new Vector3(0.034f, 0.026f, 0.052f), suede, layer, 0f, -h.sign * 12f, 0f);
        // FOUR FINGERS of three phalanges, tapering, each a pivot at its joint
        for (int i = 0; i < 4; i++)
        {
            Transform parent = h.hand;
            Vector3 at = new Vector3(h.sign * FingerX[i], 0.002f, 0.096f);
            for (int j = 0; j < 3; j++)
            {
                float len = FingerLen[j] * FingerScale[i];
                float w = (j == 0 ? FingerW0 : j == 1 ? FingerW1 : FingerW2) * (i == 3 ? 0.9f : 1f);
                var pivot = new GameObject("F" + i + j).transform;
                pivot.SetParent(parent, false);
                pivot.localPosition = at;
                RigDesign.Piece(pivot, new Vector3(0f, 0f, len * 0.5f), new Vector3(w, w * 0.86f, len), suede, layer);
                if (j == 2) Ball(pivot, new Vector3(0f, 0f, len), w * 0.9f, suede);   // the fingertip
                h.phalanx[i, j] = pivot;
                parent = pivot;
                at = new Vector3(0f, 0f, len);
            }
        }
        // THE THUMB: two phalanges off the inner side of the palm
        h.thumbBase = new GameObject("T0").transform;
        h.thumbBase.SetParent(h.hand, false);
        h.thumbBase.localPosition = new Vector3(-h.sign * 0.041f, -0.008f, 0.046f);
        RigDesign.Piece(h.thumbBase, new Vector3(0f, 0f, 0.018f), new Vector3(0.023f, 0.021f, 0.038f), suede, layer);
        h.thumbTip = new GameObject("T1").transform;
        h.thumbTip.SetParent(h.thumbBase, false);
        h.thumbTip.localPosition = new Vector3(0f, 0f, 0.037f);
        RigDesign.Piece(h.thumbTip, new Vector3(0f, 0f, 0.015f), new Vector3(0.02f, 0.018f, 0.031f), suede, layer);
        Ball(h.thumbTip, new Vector3(0f, 0f, 0.03f), 0.018f, suede);

        // THE KNUCKLES: the brass bar across the proximal phalanges with a ring per finger and the cyan edge under
        // it (lit only when a body is in the window). Under "Fist", every plate thinner than 2 cm is what the
        // opening recolours; always shown: the knuckles are the kit
        h.fistNode = new GameObject("Fist").transform;
        h.fistNode.SetParent(h.hand, false);
        h.fistNode.localPosition = new Vector3(0f, -0.02f, 0.11f);   // across the fronts of the curled proximal phalanges
        RigDesign.Piece(h.fistNode, Vector3.zero, new Vector3(0.094f, 0.017f, 0.012f), brass, layer);
        for (int i = 0; i < 4; i++)
            RigDesign.Piece(h.fistNode, new Vector3(h.sign * FingerX[i], -0.003f, 0.004f), new Vector3(0.016f, 0.024f, 0.008f), brass, layer);
        h.knuckleEdge = RigDesign.Piece(h.fistNode, new Vector3(0f, -0.014f, 0.003f), new Vector3(0.09f, 0.005f, 0.008f), KnuckleDim, layer);
        h.fistNode.gameObject.SetActive(false);

        // THE GRIP SOCKET: where a handle (and the whip's line) sits in the hand
        h.grip = new GameObject("Grip").transform;
        h.grip.SetParent(h.hand, false);
        h.grip.localPosition = new Vector3(0f, -0.024f, 0.058f);
    }

    static Transform Ball(Transform parent, Vector3 pos, float diameter, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.layer = Viewmodel.Layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = Vector3.one * diameter;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go.transform;
    }

    /// The brass bars across both fists: the knuckles class wears them, nobody else
    public void ShowKnuckles(bool on)
    {
        foreach (var h in hands) if (h.fistNode != null && h.fistNode.gameObject.activeSelf != on) h.fistNode.gameObject.SetActive(on);
    }

    /// The cyan edge under the bar (the knuckles swap its material: dim / cyan / white / gold)
    public Renderer KnuckleEdge(HandSide s) => HandOf(s).knuckleEdge;
    /// The palm block (the fist ghosts copy it)
    public Transform Palm(HandSide s) => HandOf(s).palm;
    /// The grip socket, in the world (the whip starts here)
    public Transform Grip(HandSide s) => HandOf(s).grip;
}

/// <summary>
/// The rig's materials and primitives (what the cane's BladeDesign used to lend the arms): URP lit and unlit materials,
/// a cube piece and a cylinder disc on the Viewmodel layer with no collider and no shadow. The suit's cloth, the
/// gloves' suede and the knuckles' brass are made once.
/// </summary>
public static class RigDesign
{
    static Material brass, suede, charcoal;
    /// Brass: the cufflinks, the knuckle bars, the boot's toe-cap
    public static Material Brass => brass != null ? brass : brass = Lit(new Color(0.78f, 0.58f, 0.25f), 0.9f, 0.7f);
    /// Grey suede: the gloves
    public static Material Suede => suede != null ? suede : suede = Lit(new Color(0.4f, 0.4f, 0.42f), 0f, 0.2f);
    /// Charcoal: the sleeves
    public static Material Charcoal => charcoal != null ? charcoal : charcoal = Lit(new Color(0.14f, 0.14f, 0.16f), 0.1f, 0.35f);

    public static Material Lit(Color c, float metallic, float smooth)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Smoothness", smooth);
        return m;
    }

    public static Material Unlit(Color c)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetColor("_BaseColor", c);
        return m;
    }

    public static Renderer Disc(Transform parent, Vector3 localPos, float diameter, float thickness, Material mat, int layer)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.layer = layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = new Vector3(diameter, thickness * 0.5f, diameter);
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return r;
    }

    public static Renderer Piece(Transform parent, Vector3 localPos, Vector3 scale, Material mat, int layer, float rx = 0f, float ry = 0f, float rz = 0f)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.layer = layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(rx, ry, rz);
        go.transform.localScale = scale;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return r;
    }
}
