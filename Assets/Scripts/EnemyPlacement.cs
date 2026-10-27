using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WHERE A UNIT STANDS, AND THAT YOU CAN GET TO IT.
///
/// Every enemy is placed by writing a transform position — the squad tables
/// pick a spot inside a room, the hand-placed ones name a point — and nothing
/// checked what was already there, so units spawned inside each other, inside
/// hedges and planters (which are solid: LevelKit.Slab keeps its collider) and
/// half inside walls.
///
/// SETTLE fixes that at build time: the unit is dropped onto the floor, and if
/// its body overlaps a solid or another unit it steps outward until it finds
/// clear ground. Two rules keep that honest, and both matter more than the
/// search itself:
///   IT NEVER CROSSES ANYTHING. Each candidate is swept from where the unit
///   already is, so it can never be relocated THROUGH a wall to the far side.
///   A unit that lands outside the room is a unit you cannot see or reach, and
///   the room's counter still counts it — "2 ACTIVE" with an empty floor.
///   IT NEEDS FLOOR. A spot with nothing under it is refused, so nothing is
///   left standing in the air or over a pit.
/// If no candidate passes, it stays where it was told: a visible mistake beats
/// an invisible one.
///
/// SEPARATE keeps it true afterwards: units that walk push gently out of each
/// other every tick, swept so nobody is pushed through a wall, so a crowd
/// converging on you spreads into a ring instead of collapsing into one body.
/// </summary>
public partial class Enemy
{
    /// Every enemy that exists right now, in build order
    public static readonly List<Enemy> Live = new List<Enemy>();

    /// The page (the turret) keeps its height; everything else — the old flyers included, they are footmen now — is
    /// dropped onto the floor: a springboard is a body on the line at kickable height, never one in the air
    bool Hovers => role == EnemyRole.Turret;

    /// How wide this unit stands, for spacing purposes
    float Girth => 0.42f * Mathf.Max(0.5f, transform.lossyScale.y);

    /// Put it somewhere it fits and somewhere you can reach. Called by Create, so it covers every spawn in the game.
    void Settle()
    {
        // The walls built a moment ago, WHERE THEY ARE: a collider made this frame sits at the origin at scale one until the
        // physics scene syncs, and a unit settled against that lands inside the real wall. Cheap: only what changed since the last sync
        Physics.SyncTransforms();
        Vector3 at = transform.position;
        if (!Hovers) at = Ground(at);
        if (Fits(at)) { Land(at); return; }

        // Rings outward from where it was asked to stand, nearest first. The first angle comes from the spot itself,
        // so two units told to stand in the same place step off in different directions with no randomness.
        float seed = Mathf.Abs(at.x * 7.31f + at.z * 3.17f);
        for (int ring = 1; ring <= 4; ring++)
        {
            float r = ring * 1.4f;
            for (int i = 0; i < 12; i++)
            {
                float a = (seed + i * 30f + ring * 17f) * Mathf.Deg2Rad;
                Vector3 p = at + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * r;
                if (!Hovers) p = Ground(p);
                if (!Reachable(at, p) || !Fits(p)) continue;
                Land(p);
                return;
            }
        }
        Land(at);   // nowhere better: stand where you were told, in plain sight
    }

    void Land(Vector3 p)
    {
        transform.position = p;
        spawnPos = p;
    }

    /// Move somewhere and settle there — the room loop uses this to pull a unit back inside its own room
    public void PlaceNear(Vector3 at)
    {
        transform.position = at;
        Settle();
    }

    /// True if the body is standing inside something solid right now — a wall, a pillar, a crate. Wherever that
    /// happens the unit is invisible and unkillable, and the room still counts it, so the room can never be cleared.
    public bool Stuck
    {
        get
        {
            float s = Mathf.Max(0.5f, transform.lossyScale.y);
            if (colliders != null) foreach (var c in colliders) if (c != null) c.enabled = false;
            // Deep inside, not brushing: a narrow core of the body, and never a mine (they hang over the ground it walks)
            bool blocked = Physics.CheckCapsule(
                transform.position + Vector3.up * (0.55f * s), transform.position + Vector3.up * (1.25f * s),
                Girth * 0.5f, Physics.DefaultRaycastLayers & ~(1 << HoverMine.Layer), QueryTriggerInteraction.Ignore);
            if (colliders != null) foreach (var c in colliders) if (c != null) c.enabled = true;
            return blocked;
        }
    }

    float unstickAt;
    int unstickFails;

    /// IN PLAY: a unit that finds itself inside something solid (pushed there, built there before the walls were where they
    /// are, carried there by a slide it did not sweep) gets itself out. Every beat and a half it checks; stuck, it looks for
    /// clear ground round itself, nearest you first, so it comes out where you can fight it. Three beats with nowhere to
    /// go and it burns out on its own: a room can never be held hostage by a unit nobody can reach. Nobody is credited.
    void TickUnstick()
    {
        if (Time.time < unstickAt || role == EnemyRole.Boss) return;
        unstickAt = Time.time + 1.5f;
        if (!Stuck) { unstickFails = 0; return; }
        var director = CombatDirector.Instance;
        Vector3 toward = director != null && director.motor != null ? director.motor.CurrPosition : transform.position + transform.forward;
        Vector3 at = transform.position;
        Vector3 best = at;
        float bestD = float.MaxValue;
        for (int ring = 1; ring <= 5; ring++)
            for (int i = 0; i < 12; i++)
            {
                float a = (i * 30f + ring * 17f) * Mathf.Deg2Rad;
                for (int lift = 0; lift < (Hovers ? 3 : 1); lift++)
                {
                    Vector3 p = at + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * (ring * 1.6f) + Vector3.up * (lift == 1 ? 3f : lift == 2 ? -3f : 0f);
                    if (!Hovers) p = Ground(p);
                    if (!Fits(p)) continue;
                    float d = Vector3.Distance(p, toward);
                    if (d < bestD) { bestD = d; best = p; }
                }
            }
        if (bestD < float.MaxValue)
        {
            unstickFails = 0;
            Land(best);
            if (GameFeel.Instance != null) GameFeel.Instance.PlayFizzle();
            return;
        }
        if (++unstickFails < 3) return;
        if (GameFeel.Instance != null) GameFeel.Instance.Sparks(Center, 24, false);
        Perish(KillMethod.Blast, false);
    }

    /// The floor under a point, if there is one within a few metres either way — or, for a unit told to stand in the
    /// air (the old flyers' spots are up to a dozen metres over a floor), the first floor below it
    Vector3 Ground(Vector3 p)
    {
        // Its own colliders step out of the way, or the ray lands on its own hitbox and it stands on top of itself
        if (colliders != null) foreach (var c in colliders) if (c != null) c.enabled = false;
        bool found = Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 9f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            || Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out hit, 40f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (colliders != null) foreach (var c in colliders) if (c != null) c.enabled = true;
        return found ? new Vector3(p.x, hit.point.y, p.z) : p;
    }

    /// Nothing solid in the body's space, nobody else in it, and — for anything that walks — floor underneath
    bool Fits(Vector3 feet)
    {
        float s = Mathf.Max(0.5f, transform.lossyScale.y);
        float r = Girth;
        if (!Hovers && !Physics.Raycast(feet + Vector3.up * 0.6f, Vector3.down, 2.5f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return false;                                   // nothing to stand on

        if (colliders != null) foreach (var c in colliders) if (c != null) c.enabled = false;
        Vector3 p1 = feet + Vector3.up * (0.45f * s), p2 = feet + Vector3.up * (1.35f * s);
        bool blocked = Physics.CheckCapsule(p1, p2, r, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (colliders != null) foreach (var c in colliders) if (c != null) c.enabled = true;
        if (blocked) return false;

        foreach (var e in Live)
        {
            if (e == null || e == this) continue;
            float want = r + e.Girth + 0.25f;
            Vector3 d = e.transform.position - feet;
            d.y = 0f;
            if (d.sqrMagnitude < want * want) return false;
        }
        return true;
    }

    /// Can the body get from A to B without going through anything? This is what stops a unit being relocated to
    /// the far side of a wall, where it is invisible, unreachable, and still counted by the room.
    bool Reachable(Vector3 from, Vector3 to)
    {
        Vector3 step = to - from;
        step.y = 0f;
        if (step.sqrMagnitude < 1e-4f) return true;
        float s = Mathf.Max(0.5f, transform.lossyScale.y);
        Vector3 p1 = from + Vector3.up * (0.45f * s), p2 = from + Vector3.up * (1.35f * s);
        if (colliders != null) foreach (var c in colliders) if (c != null) c.enabled = false;
        bool hit = Physics.CapsuleCast(p1, p2, Girth * 0.9f, step.normalized, out _, step.magnitude,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (colliders != null) foreach (var c in colliders) if (c != null) c.enabled = true;
        return !hit;
    }

    /// Every tick, for units that walk: ease out of anyone standing in the same place. Gentle, swept, and capped,
    /// so it reads as a crowd spreading out rather than as being shoved.
    void Separate(float dt)
    {
        float r = Girth;
        Vector3 push = Vector3.zero;
        int n = 0;
        foreach (var e in Live)
        {
            if (e == null || e == this || !e.Alive) continue;
            Vector3 d = transform.position - e.transform.position;
            d.y = 0f;
            float want = r + e.Girth + 0.2f;
            float sq = d.sqrMagnitude;
            if (sq >= want * want) continue;
            float dist = Mathf.Sqrt(Mathf.Max(0.0001f, sq));
            // Two units exactly on top of each other have no direction between them: their own spot picks one
            float spin = (transform.position.x * 12.9898f + transform.position.z * 78.233f) * Mathf.Deg2Rad;
            Vector3 away = dist > 0.05f ? d / dist : new Vector3(Mathf.Sin(spin), 0f, Mathf.Cos(spin));
            push += away * (want - dist);
            n++;
        }
        if (n == 0) return;

        Vector3 step = push / n * Mathf.Min(1f, dt * 6f);
        if (step.sqrMagnitude < 1e-6f) return;
        if (step.magnitude > 0.25f) step = step.normalized * 0.25f;   // never a shove
        if (!Reachable(transform.position, transform.position + step)) return;

        Vector3 next = transform.position + step;
        if (!Hovers) next = Ground(next);
        transform.position = next;
    }
}
