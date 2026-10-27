using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// COURSE ZONES (2026-10-26): three components for the level editor's hand-placed geometry (and the generators), each a
/// box the player is in or not, checked against the player's position every tick like every hazard is (no physics
/// triggers: the capsule is kinematic). Put one on any object; with a Collider on it the collider's bounds are the
/// box, else `size` round the transform.
///   GLIDE CHUTE   over a great drop or a long chasm: inside it, mid-trick or sliding, gravity eases and you sail
///   STYLE ZONE    framing a jump worth taking (past a statue, between the pillars): the trick's window widens and its
///                 style doubles while you are in it
///   MOMENTUM PAD  a surface that takes your incoming speed, multiplies it by your grade, and throws you on: the
///                 higher the grade the bigger the sequence break
/// </summary>
public class GlideChute : MonoBehaviour
{
    static readonly List<GlideChute> all = new List<GlideChute>();

    [Tooltip("Gravity inside, as a fraction of the level's")]
    [Range(0.05f, 1f)] public float gravityScale = 0.45f;
    [Tooltip("Only while mid-trick (or having tricked this air) or sliding; off = always")]
    public bool requireTrickOrSlide = true;
    [Tooltip("The box round the transform when there is no Collider on this object")]
    public Vector3 size = new Vector3(20f, 30f, 60f);

    void OnEnable() => all.Add(this);
    void OnDisable() => all.Remove(this);

    /// The gravity scale at the player's position: the lowest of the chutes it is in that apply, else 1
    public static float ScaleAt(PlayerMotor motor)
    {
        if (all.Count == 0 || motor == null) return 1f;
        float s = 1f;
        Vector3 p = motor.CurrPosition + Vector3.up * 0.9f;
        bool tricking = TrickSystem.Instance != null && (TrickSystem.Instance.Active || TrickSystem.Instance.ComboIndex > 0);
        bool allowed = tricking || motor.Sliding;
        for (int i = 0; i < all.Count; i++)
        {
            var c = all[i];
            if (c == null || (c.requireTrickOrSlide && !allowed)) continue;
            if (CourseZones.Bounds(c.transform, c.size).Contains(p)) s = Mathf.Min(s, c.gravityScale);
        }
        return s;
    }
}

public class StyleZone : MonoBehaviour
{
    static readonly List<StyleZone> all = new List<StyleZone>();

    [Tooltip("The trick window's width inside, as a multiple")]
    public float windowScale = 1.8f;
    [Tooltip("The trick's style inside, as a multiple")]
    public float styleScale = 2f;
    public Vector3 size = new Vector3(16f, 16f, 30f);

    void OnEnable() => all.Add(this);
    void OnDisable() => all.Remove(this);

    public static float WindowScaleAt(Vector3 feet) => Best(feet, true);
    public static float StyleScaleAt(Vector3 feet) => Best(feet, false);

    static float Best(Vector3 feet, bool window)
    {
        float s = 1f;
        if (all.Count == 0) return s;
        Vector3 p = feet + Vector3.up * 0.9f;
        for (int i = 0; i < all.Count; i++)
        {
            var z = all[i];
            if (z == null || !CourseZones.Bounds(z.transform, z.size).Contains(p)) continue;
            s = Mathf.Max(s, window ? z.windowScale : z.styleScale);
        }
        return s;
    }
}

public class MomentumPad : MonoBehaviour
{
    [Tooltip("The least upward speed it throws with, m/s (an incoming slide has none of its own)")]
    public float lift = 6f;
    [Tooltip("Throw along the pad's own forward instead of the way you came in")]
    public bool alongForward = false;
    [Tooltip("The incoming speed's floor before the grade multiplies it")]
    public float floorSpeed = 8f;
    public Vector3 size = new Vector3(4f, 1f, 4f);

    float readyAt;

    void Update()
    {
        var motor = PlayerMotor.Current;
        if (motor == null || Time.time < readyAt || PauseMenu.IsPaused) return;
        var b = CourseZones.Bounds(transform, size);
        b.Expand(new Vector3(0f, 1.2f, 0f));
        if (!b.Contains(motor.CurrPosition + Vector3.up * 0.3f)) return;
        readyAt = Time.time + 0.6f;
        Vector3 v = motor.Velocity;
        float h = new Vector2(v.x, v.z).magnitude;
        Vector3 dir = alongForward ? new Vector3(transform.forward.x, 0f, transform.forward.z).normalized
                    : h > 0.5f ? new Vector3(v.x, 0f, v.z) / h
                    : motor.look != null ? Quaternion.Euler(0f, motor.look.Yaw, 0f) * Vector3.forward : transform.forward;
        int rank = StyleSystem.Current != null ? StyleSystem.Current.RankIndex : 0;
        float perRank = motor.config != null ? motor.config.padSpeedPerRank : 0.12f;
        float speed = Mathf.Max(h, floorSpeed) * (1f + perRank * rank);
        motor.Launch(dir * speed + Vector3.up * Mathf.Max(lift, v.y));
        MovementEvents.Emit(MoveType.BlastJump, "MOMENTUM PAD  ×" + (1f + perRank * rank).ToString("0.00"));
        if (GameFeel.Instance != null) GameFeel.Instance.PlayPad();
    }
}

static class CourseZones
{
    /// The box a zone covers: its collider's bounds when it has one, else `size` round its transform
    public static Bounds Bounds(Transform t, Vector3 size)
    {
        var col = t.GetComponent<Collider>();
        return col != null ? col.bounds : new Bounds(t.position, Vector3.Scale(size, t.lossyScale));
    }
}
