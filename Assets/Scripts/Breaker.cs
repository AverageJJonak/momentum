using UnityEngine;

/// <summary>
/// A TIE-IN: a breaker cabinet with a cyan bar locked across its door. CYAN
/// MEANS CUT HERE — cut the bar, from a sword's reach or from as far as an
/// arrow flies, and the tie-in is made: the bar goes, the cabinet's lamp comes
/// on, the marker beam over the site goes out, and the district counts it.
/// Anything with a line to the cabinet can throw it. That is on purpose: from
/// the right spot an arrow saves you the whole trip.
/// </summary>
public class Breaker : MonoBehaviour, IStrikeTarget
{
    public District district;
    public int index;
    /// Where it is, in the company's words: THE VIADUCT, SHAFT 2 VENT, TOWER 9 CROWN
    public string site;
    public Renderer lamp;
    public Material lampOn;
    public GameObject bar;
    public GameObject beam;
    public Light glow;

    public bool Thrown { get; private set; }

    public bool Struck(Vector3 point, bool arrow)
    {
        if (Thrown) return false;
        Thrown = true;
        if (bar != null) Destroy(bar);
        if (beam != null) beam.SetActive(false);
        if (lamp != null && lampOn != null) lamp.sharedMaterial = lampOn;
        if (glow != null) glow.enabled = true;
        var feel = GameFeel.Instance;
        if (feel != null)
        {
            feel.Sparks(point, 40, true);
            feel.PlayClank(1f);
        }
        ShockRing.Spawn(transform.position + Vector3.up * 0.1f, 0.5f, 6f, 0.45f, true);
        if (district != null) district.TieIn(this);
        return true;
    }
}
