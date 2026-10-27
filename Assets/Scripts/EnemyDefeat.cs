using UnityEngine;

/// <summary>
/// The fall. Every unit but the boss comes apart the frame its health runs
/// out; the Concierge is kept on his feet by BossOutro and falls when his exit
/// says so. Both come through here: the ragdoll is born from whatever pose the
/// body is in, the live rig goes dark, and the corpse root is handed back for
/// anyone who wants to throw it.
/// </summary>
public partial class Enemy
{
    public GameObject Collapse()
    {
        var root = SpawnRagdoll(DeathMethod);
        foreach (var r in renderers) if (r != null) r.enabled = false;
        foreach (var c in colliders) if (c != null) c.enabled = false;
        return root;
    }
}
