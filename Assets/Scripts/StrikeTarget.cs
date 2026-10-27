using UnityEngine;

/// <summary>
/// Something in the world that a punch can DO something to: a tie-in breaker, a
/// locking bar, a relief valve, a pane. CYAN IS THE THING YOU BREAK: the jab's
/// fan (Fists.Probe) finds it in reach and the press strikes it (Fists.Punch;
/// the kick too). Returning true means the strike did something: the hands then
/// read it as a HIT. `arrow` is a leftover of the deck's cards (always false now).
/// </summary>
public interface IStrikeTarget
{
    bool Struck(Vector3 point, bool arrow);
}
