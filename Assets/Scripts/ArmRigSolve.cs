using UnityEngine;

/// <summary>
/// The two-bone IK. Shoulder → elbow → wrist from the hand's target, analytic: the elbow lies on the circle the two
/// bone lengths allow, placed by a pole. The pole FOLLOWS THE WRIST (2026-10-12): the elbow swings to the side
/// opposite the way the knuckles point, so the forearm runs into the hand along the hand's own axis and the wrist
/// stays nearly straight — a fist continues the forearm's line whatever pose was authored, and a weapon's hand is
/// never changed for it. Where the hand points along the arm (no preference) the pole rests out and down; a pole
/// that would put the elbow on the far side is faded back to that rest; the pole is sprung over time so the elbow
/// never pops. A target past the arm's length is met first by the shoulder leaning toward it (the body behind the
/// cut), then — for holds that ask for it, the cane's cuts and the shove — by a stretch of both bones, and only
/// then by pulling the hand in: never a hyperextended elbow. The hand's rotation is the target's (a fist alone may
/// be straightened onto the forearm by viewmodelFistWristFlex); the forearm pronates with it.
/// </summary>
public partial class ArmRig
{
    partial class Hand
    {
        public Vector3 pole, poleVel;   // the elbow's pole, sprung over time
        public bool poleInit;
    }

    static readonly Vector3 DefaultShoulder = new Vector3(0.27f, -0.30f, -0.12f);

    /// The shoulder pivot's rest, mirrored for the left, with the layers' share and the tackle on it
    Vector3 ShoulderBase(Hand h)
    {
        Vector3 s = config != null ? config.viewmodelShoulder : DefaultShoulder;
        if (h.side == HandSide.Left) s.x = -s.x;
        return s + ShoulderOffset(h);
    }

    /// Bends the arm to `t` (camera space), writing the shoulder, elbow and their geometry; `t` is pulled in when
    /// the hand cannot be reached. `wristClamp` > 0 straightens the hand onto the forearm past that many degrees
    /// (a bare fist only: a held weapon's hand is sacred)
    void SolveArm(Hand h, ref ArmPose t, bool stretchOk, bool follower, float wristClamp, float dt)
    {
        var c = config;
        float L1 = c != null ? Mathf.Max(0.05f, c.viewmodelUpperArm) : 0.32f;
        float L2 = c != null ? Mathf.Max(0.05f, c.viewmodelForearm) : 0.29f;
        float clamp = c != null ? Mathf.Clamp(c.viewmodelReachClamp, 0.5f, 1f) : 0.985f;
        float leadMax = c != null ? Mathf.Max(0f, c.viewmodelShoulderLead) : 0.22f;
        float maxStretch = c != null ? Mathf.Max(1f, c.viewmodelMaxStretch) : 1.8f;

        Vector3 S = ShoulderBase(h);
        Vector3 a = t.pos - S;
        float d = a.magnitude;
        if (d < 1e-4f) { a = Vector3.forward * 1e-4f; d = 1e-4f; }
        Vector3 dir = a / d;
        float full = (L1 + L2) * clamp;
        float stretch = 1f;
        if (d > full)
        {
            float lead = Mathf.Min(d - full, leadMax);   // the shoulder comes forward toward the hand
            S += dir * lead;
            d -= lead;
            if (d > full)
            {
                if (stretchOk) stretch = Mathf.Min(d / full, maxStretch);
                float reach = full * stretch;
                if (d > reach) { d = reach; t.pos = S + dir * reach; }   // pulled in: the elbow never locks past straight
            }
        }
        float l1 = L1 * stretch, l2 = L2 * stretch;

        // THE POLE. Rest: out and down, turned a little by the hand's roll. Wrist: opposite the knuckles' lean off
        // the arm's line, so the forearm comes into the hand along the hand's axis. Blended by how much the hand
        // leans (none along the line) and faded where it would throw the elbow to the far side; then sprung
        float outAmt = c != null ? c.viewmodelElbowOut : 0.75f;
        float downAmt = c != null ? c.viewmodelElbowDown : 0.85f;
        float twistK = c != null ? c.viewmodelElbowTwist : 0.15f;
        float follow = c != null ? Mathf.Clamp01(c.viewmodelElbowFollowWrist) : 0.8f;
        Vector3 handZ = t.rot * Vector3.forward;
        Vector3 handUp = t.rot * Vector3.up;
        Vector3 upPerp = Vector3.ProjectOnPlane(Vector3.up, dir);
        Vector3 handUpPerp = Vector3.ProjectOnPlane(handUp, dir);
        float twist = upPerp.sqrMagnitude > 1e-4f && handUpPerp.sqrMagnitude > 1e-4f
            ? Vector3.SignedAngle(upPerp, handUpPerp, dir) * twistK : 0f;
        Vector3 basePole = Vector3.ProjectOnPlane(Quaternion.AngleAxis(twist, dir) * new Vector3(h.sign * outAmt, -downAmt, -0.2f), dir);
        if (basePole.sqrMagnitude < 1e-6f) basePole = Vector3.ProjectOnPlane(Vector3.down, dir);
        basePole.Normalize();
        Vector3 lean = Vector3.ProjectOnPlane(handZ, dir);
        Vector3 want = basePole;
        float leanMag = lean.magnitude;
        if (leanMag > 1e-4f)
        {
            Vector3 wristPole = -lean / leanMag;
            float w = follow * Mathf.Clamp01(leanMag / 0.25f) * Mathf.Clamp01(Vector3.Dot(wristPole, basePole) + 0.5f);
            want = Vector3.Slerp(basePole, wristPole, w);
        }
        if (!h.poleInit) { h.pole = want; h.poleVel = Vector3.zero; h.poleInit = true; }
        else Spring(ref h.pole, ref h.poleVel, want, (c != null ? Mathf.Max(1f, c.viewmodelBlendRate) : 14f) * 2f, 1f, dt);
        Vector3 poleProj = Vector3.ProjectOnPlane(h.pole, dir);
        if (poleProj.sqrMagnitude < 1e-6f) poleProj = basePole;
        poleProj.Normalize();

        float cosA = Mathf.Clamp((l1 * l1 + d * d - l2 * l2) / (2f * l1 * d), -1f, 1f);
        float sinA = Mathf.Sqrt(Mathf.Max(0f, 1f - cosA * cosA));
        Vector3 E = S + dir * (l1 * cosA) + poleProj * (l1 * sinA);

        Vector3 foreDir = t.pos - E;
        if (foreDir.sqrMagnitude < 1e-8f) foreDir = dir;
        foreDir.Normalize();
        if (wristClamp > 0f)
        {
            // A fist is straightened onto the forearm past the allowed flex, keeping its roll
            float flex = Vector3.Angle(handZ, foreDir);
            if (flex > wristClamp)
            {
                Vector3 to = Vector3.Slerp(handZ, foreDir, (flex - wristClamp) / flex);
                t.rot = Quaternion.FromToRotation(handZ, to) * t.rot;
                handUp = t.rot * Vector3.up;
            }
        }

        // THE BONES: the upper arm looks at the elbow, the forearm at the wrist and rolls with the hand
        Vector3 upperDir = E - S;
        if (upperDir.sqrMagnitude < 1e-8f) upperDir = dir;
        h.shoulder.localPosition = S;
        h.shoulder.localRotation = Quaternion.LookRotation(upperDir, -poleProj);
        Vector3 foreUp = Vector3.ProjectOnPlane(handUp, foreDir);
        if (foreUp.sqrMagnitude < 1e-6f) foreUp = -poleProj;
        h.elbow.localPosition = E;
        h.elbow.localRotation = Quaternion.LookRotation(foreDir, foreUp);
        // The geometry follows the stretch
        h.upperGeo.localPosition = new Vector3(0f, 0f, l1 * 0.5f);
        h.upperGeo.localScale = new Vector3(0.088f, 0.094f, l1);
        h.foreGeo.localPosition = new Vector3(0f, 0f, l2 * 0.5f - 0.02f);
        h.foreGeo.localScale = new Vector3(0.076f, 0.072f, Mathf.Max(0.05f, l2 - 0.05f));
        h.cuff.localPosition = new Vector3(0f, 0f, l2 - 0.028f);
        h.link.localPosition = new Vector3(h.sign * 0.044f, 0.012f, l2 - 0.028f);
    }

    /// The held weapon re-posed from the hand after the solve: the layers and the reach carry it
    void RePose(Hand h, ArmPose handPose)
    {
        if (h.holdWeapon == null) return;
        ArmPose w = Compose(handPose, Inverse(h.handInWeapon));
        h.holdWeapon.SetPositionAndRotation(Root.TransformPoint(w.pos), Root.rotation * w.rot);
    }

    /// A camera-space point of the hands' picture, moved to where the WORLD camera projects it to the same place on
    /// screen: a world-space line (the whip) or effect that must meet the hand on screen starts here, not at the
    /// hand's own world position (the two cameras differ in FOV)
    public static Vector3 WorldLineAnchor(Transform cameraTransform, Camera worldCam, Vector3 camLocal)
    {
        float k = 1f;
        var vm = ViewmodelCamera.Instance;
        if (worldCam != null && vm != null && vm.Cam != null)
        {
            float tw = Mathf.Tan(worldCam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float tv = Mathf.Tan(vm.Cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            if (tv > 1e-4f) k = tw / tv;
        }
        return cameraTransform.TransformPoint(new Vector3(camLocal.x * k, camLocal.y * k, camLocal.z));
    }

    /// The right hand's grip socket as a world-space line anchor (the whip's start). Camera-local, not root-local:
    /// the root leans off the camera (ArmRigLayers' ROOT), and the anchor must sit where the hand is drawn
    public Vector3 GripLineAnchor(HandSide s, Camera worldCam)
    {
        var g = HandOf(s).grip;
        Vector3 local = transform.InverseTransformPoint(g.position);
        return WorldLineAnchor(transform, worldCam, local);
    }
}
