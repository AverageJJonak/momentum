using UnityEngine;

/// <summary>
/// MORE TRICKS (2026-11-10; the user: "let's add more types of tricks, like full twists and gainers and stuff"). The
/// four tricks of 2026-10-23 stay exactly as they were; these are chosen by what is held at the trick's second press:
/// S while travelling fast is a GAINER (a backflip thrown forward: more of its burst goes forward); W with A or D is a
/// FULL TWIST and S with A or D a CORK (a flip with a twist: slower, worth more style); JUMP held at the press makes a
/// DOUBLE (a double front, a double back, a 720: the arc takes longer, pays more, and is easier to land short of). A
/// trick of a different kind from the last one thrown pays more style: variety. Off, the game has its four tricks.
/// (Crouch would be the natural tuck, but crouch is held in the air to land in a slide: it would double every trick.)
/// </summary>
public partial class MovementConfig
{
    [Header("MORE TRICKS · gainer, full twist, cork, doubles")]
    [Tooltip("Off: only the front flip, the backflip, the side flip and the spin")]
    public bool trickMoreKinds = true;
    [Tooltip("A backflip thrown while travelling at least this fast is a GAINER")]
    public float trickGainerSpeed = 12f;
    [Tooltip("The gainer's burst forward, as a share of trickBurstForward (the backflip's is 0.4)")]
    public float trickGainerForward = 0.9f;
    [Tooltip("The gainer's burst up, as a share of trickBurstUp (the backflip's is trickBackUpScale)")]
    public float trickGainerUp = 1.4f;
    [Tooltip("Seconds a FULL TWIST or a CORK takes")]
    public float trickTwistSeconds = 0.75f;
    [Tooltip("What a full twist or a cork pays in style over a plain flip")]
    public float trickTwistStyleScale = 1.5f;
    [Tooltip("A DOUBLE (jump held at the press) takes this many times as long as the single")]
    public float trickDoubleSecondsScale = 1.6f;
    [Tooltip("A double's burst over the single's")]
    public float trickDoubleBurstScale = 1.25f;
    [Tooltip("A double's style over the single's")]
    public float trickDoubleStyleScale = 1.8f;
    [Tooltip("A trick of a different kind from the last one thrown pays this much more style")]
    public float trickVarietyStyleScale = 1.25f;
}
