using UnityEngine;

/// One bone of a dead mannequin: the enamel shell knocks on what it hits and the
/// glass head rings — only when it hits hard, and only while the corpse is fresh
/// (RagdollFade keeps the count: three a second, six in all). A corpse is not a drum kit.
public class RagdollBone : MonoBehaviour
{
    const float MinSpeed = 3.5f, FullSpeed = 14f;
    float nextAt;
    bool glass;
    RagdollFade corpse;

    void Awake() { glass = name == "head"; }

    void OnCollisionEnter(Collision c)
    {
        float v = c.relativeVelocity.magnitude;
        if (v < MinSpeed || Time.time < nextAt) return;
        nextAt = Time.time + 0.25f;
        if (corpse == null) corpse = GetComponentInParent<RagdollFade>();
        if (corpse != null && !corpse.TryClank(Time.time)) return;
        if (GameFeel.Instance == null) return;
        float k = Mathf.Clamp01((v - MinSpeed) / (FullSpeed - MinSpeed)); // the volume is the impact
        if (glass) GameFeel.Instance.PlayGlassClank(k);
        else GameFeel.Instance.PlayShellClank(k * 0.8f);
    }
}

/// A corpse's life: physics until it settles, then frozen where it lies, then
/// it sinks away — levels don't respawn enemies, so corpses are the record of
/// the run, but they can't be allowed to pile up physics forever.
public class RagdollFade : MonoBehaviour
{
    float born, settle, life;
    bool frozen;
    Vector3 baseScale;

    // The corpse's voice: a clank only in its first 3 s (the throw, the landing, a bounce), three a second at most, six in all
    readonly float[] clankAt = { float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity };
    int clanks;

    public void Init(float settle, float life)
    {
        this.settle = settle;
        this.life = life;
        born = Time.time;
        baseScale = transform.localScale;
    }

    /// May a bone of this corpse make a noise now? (RagdollBone asks before every clank)
    public bool TryClank(float now)
    {
        if (clanks >= 6 || now - born > 3f) return false;
        int slot = clanks % clankAt.Length;        // the stamp from three clanks ago: under a second old = three this second already
        if (now - clankAt[slot] < 1f) return false;
        clankAt[slot] = now;
        clanks++;
        return true;
    }

    void Update()
    {
        float age = Time.time - born;
        if (!frozen && age > settle)
        {
            frozen = true;
            foreach (var rb in GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;
        }
        if (age > life)
        {
            float t = Mathf.Clamp01((age - life) / 0.6f);
            transform.localScale = baseScale * (1f - t);
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
