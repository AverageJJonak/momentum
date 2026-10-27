using UnityEngine;

/// <summary>
/// The held weapon riding in its hand while an arm clip plays (ArmClipDriver). The rig re-poses a weapon from
/// its hand only for the hand's own hold; a clip holds the hand on an anchor instead, so this runs AFTER the rig
/// has solved (execution order +10) and puts the weapon where the solved hand says. Where the wrist sits on the
/// weapon is MEASURED, not asked: on the frame the clip begins the rig still solves the weapon's own hold and
/// re-poses the weapon from the hand, so hand = weapon ∘ handInWeapon exactly, and the inverse of that is kept
/// for the clip's length. Public API only: RootPose, HandTransform, Holding, Root.
/// </summary>
[DefaultExecutionOrder(10)]
public class ArmClipCarry : MonoBehaviour
{
    ArmRig rig;
    Transform weapon;
    ArmRig.HandSide side;
    bool active, measured, warned;
    Vector3 hiwPos;
    Quaternion hiwRot = Quaternion.identity;

    public bool Carrying => active && measured;

    public void Begin(ArmRig r, Transform w, ArmRig.HandSide s)
    {
        rig = r;
        weapon = w;
        side = s;
        active = w != null;
        measured = false;
    }

    public void End()
    {
        active = false;
        measured = false;
        weapon = null;
    }

    void LateUpdate()
    {
        if (!active || weapon == null || rig == null || !rig.Built) { active = false; return; }
        var hand = rig.HandTransform(side);
        if (hand == null) { active = false; return; }
        if (!measured)
        {
            if (!rig.Holding(side, weapon))
            {
                // The hand was not on this weapon when the clip began: nothing rides, the clip still plays
                if (!warned) { warned = true; Debug.LogWarning("ArmClipCarry: the " + side + " hand is not holding " + weapon.name + "; the clip plays without it"); }
                active = false;
                return;
            }
            var w = rig.RootPose(weapon);
            var h = rig.RootPose(hand);
            Quaternion iw = Quaternion.Inverse(w.rot);
            hiwPos = iw * (h.pos - w.pos);
            hiwRot = iw * h.rot;
            measured = true;
            return;
        }
        // weapon = hand ∘ inverse(handInWeapon)
        var hp = rig.RootPose(hand);
        Quaternion wr = hp.rot * Quaternion.Inverse(hiwRot);
        Vector3 wp = hp.pos - wr * hiwPos;
        weapon.SetPositionAndRotation(rig.Root.TransformPoint(wp), rig.Root.rotation * wr);
    }
}
