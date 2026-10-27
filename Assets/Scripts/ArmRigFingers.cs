using UnityEngine;

/// <summary>
/// The fingers: five curl states, each a set of joint angles, blended per finger with a critically damped spring and
/// a ripple (the index moves first, the pinky last) so a fist closes in a wave; a jab snaps every finger at once.
/// The thumb has its own base turn and tip curl per state. Angles are degrees about the joints' local X (positive
/// folds toward the palm); the spread is a turn about Y at the knuckle.
/// </summary>
public partial class ArmRig
{
    partial class Hand
    {
        public Fingers state = Fingers.Relaxed, prevState = Fingers.Relaxed;
        public float stateChangedAt = -10f;
        public readonly float[] curl = new float[12], curlVel = new float[12];
        public readonly float[] spread = new float[4], spreadVel = new float[4];
        public Vector3 thumbEuler, thumbEulerVel;
        public float thumbTipCurl, thumbTipVel;
        public bool fingersInit;
    }

    // (MCP, PIP, DIP) per state: Relaxed, Open, Fist, Grip, Pinch
    static readonly float[,] Curl =
    {
        { 22f, 28f, 14f },
        { 2f, 6f, 2f },
        { 80f, 100f, 60f },   // the fist: tight, the fingers under the palm toward the body, four knuckle ridges in front
        { 62f, 82f, 46f },
        { 40f, 48f, 30f },
    };
    // The pinch: the index nearly straight to meet the thumb, the others curled away under it
    static readonly float[] PinchMul = { 0.8f, 1f, 1.6f, 1.9f };
    static readonly float[] Spread = { 4f, 9f, 0f, 2f, 3f };
    static readonly float[] SpreadDir = { -1.5f, -0.5f, 0.5f, 1.5f };
    // The thumb's base: (pitch toward the palm, yaw out from the hand — negative folds it IN across the fingers) and
    // its tip's curl, per state. The fist lays the thumb across the fronts of the curled fingers on the inner side
    static readonly Vector2[] ThumbBase = { new Vector2(14f, 38f), new Vector2(4f, 55f), new Vector2(25f, -60f), new Vector2(40f, -30f), new Vector2(30f, 10f) };
    static readonly float[] ThumbTip = { 15f, 5f, 70f, 45f, 30f };

    /// One joint toward its target: landed outright on the first frame, sprung after
    static void Joint(ref float x, ref float v, float target, float omega, float dt, bool jump)
    {
        if (jump) { x = target; v = 0f; }
        else Spring(ref x, ref v, target, omega, 1f, dt);
    }

    void UpdateFingers(Hand h, Fingers f, bool snap, float dt)
    {
        float now = Time.unscaledTime;
        if (f != h.state)
        {
            h.prevState = h.state;
            h.state = f;
            h.stateChangedAt = now;
        }
        float omega = config != null ? Mathf.Max(1f, snap ? config.viewmodelFingerSnapRate : config.viewmodelFingerRate) : (snap ? 70f : 22f);
        float ripple = snap ? 0f : (config != null ? Mathf.Max(0f, config.viewmodelFingerRipple) : 0.025f);
        bool jump = !h.fingersInit;   // the first frame lands every joint on its target: no uncurling from nothing
        h.fingersInit = true;
        for (int i = 0; i < 4; i++)
        {
            Fingers fi = jump || now - h.stateChangedAt >= i * ripple ? h.state : h.prevState;
            int si = (int)fi;
            float mul = fi == Fingers.Pinch ? PinchMul[i] : 1f;
            for (int j = 0; j < 3; j++)
            {
                int k = i * 3 + j;
                Joint(ref h.curl[k], ref h.curlVel[k], Mathf.Min(100f, Curl[si, j] * mul), omega, dt, jump);
            }
            Joint(ref h.spread[i], ref h.spreadVel[i], Spread[si] * SpreadDir[i], omega, dt, jump);
            h.phalanx[i, 0].localRotation = Quaternion.Euler(h.curl[i * 3], h.sign * h.spread[i], 0f);
            h.phalanx[i, 1].localRotation = Quaternion.Euler(h.curl[i * 3 + 1], 0f, 0f);
            h.phalanx[i, 2].localRotation = Quaternion.Euler(h.curl[i * 3 + 2], 0f, 0f);
        }
        int st = (int)h.state;
        Vector2 tb = ThumbBase[st];
        Vector3 thumbTarget = new Vector3(tb.x, -h.sign * tb.y, 0f);
        if (jump) { h.thumbEuler = thumbTarget; h.thumbEulerVel = Vector3.zero; }
        else Spring(ref h.thumbEuler, ref h.thumbEulerVel, thumbTarget, omega, 1f, dt);
        Joint(ref h.thumbTipCurl, ref h.thumbTipVel, ThumbTip[st], omega, dt, jump);
        h.thumbBase.localRotation = Quaternion.Euler(h.thumbEuler);
        h.thumbTip.localRotation = Quaternion.Euler(h.thumbTipCurl, 0f, 0f);
    }
}
