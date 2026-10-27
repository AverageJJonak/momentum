using UnityEngine;

/// <summary>
/// A UNIT IN THE AIR: THE KICK LAUNCHES the body it lands on (Fists.Kick → Toss), the hook knocks one sideways. The
/// unit's own transform flies (so its hitboxes fly with it) under enemyTossGravity, the horizontal stopped by the
/// first wall, and it lands on whatever floor is under it; while it flies it is dazed (its own stagger) and ticks
/// nothing else (Enemy.FixedUpdate hands the tick here). A FOOTMAN comes apart where it lands: a kick launch is a
/// kill, and fuel. Bosses are five times your size and never leave the ground; the page hovers on its own; the
/// BOUNCER is a wall and the best springboard in the game — the kick pays the full launch and it stays standing.
/// The signatures (Toss, Pin) are the kick's: they do not change.
/// </summary>
public partial class Enemy
{
    Vector3 tossVel;
    float tossLandDamage, tossLandRadius;
    bool tossed;
    static readonly Collider[] tossHits = new Collider[24];

    /// True while the unit is in the air off a toss
    public bool Tossed => tossed && Alive;

    /// A footman, a shooter or the dummy can be thrown; the page (it hovers), the bouncer (it is a wall) and a boss cannot
    bool Tossable => Alive && role != EnemyRole.Boss && role != EnemyRole.Turret && role != EnemyRole.Bouncer;

    /// Throw the unit: `velocity` in m/s (world); where it lands, every other unit within `landRadius` takes `landDamage`
    public void Toss(Vector3 velocity, float landDamage = 0f, float landRadius = 0f)
    {
        if (!Tossable) return;
        tossed = true;
        tossVel = velocity;
        tossLandDamage = landDamage;
        tossLandRadius = landRadius;
        // Dazed for the flight: the attack it was winding up is gone, and it comes down dizzy
        Stagger();
        float g = config != null ? Mathf.Max(1f, config.enemyTossGravity) : 14f;
        float flight = Mathf.Max(0.3f, 2f * Mathf.Max(0f, velocity.y) / g + 0.4f);
        staggeredUntil = Mathf.Max(staggeredUntil, Time.time + flight);
    }

    void TickToss(float dt)
    {
        float g = config != null ? Mathf.Max(1f, config.enemyTossGravity) : 14f;
        tossVel.y -= g * dt;
        Vector3 pos = transform.position;
        Vector3 step = tossVel * dt;
        float scale = Mathf.Max(0.1f, transform.lossyScale.y);
        float waist = CentreHeight * scale;   // the body's middle: the casts start here
        Vector3 body = pos + Vector3.up * waist;
        // The horizontal: the first wall stops it (the body slides down the face)
        Vector3 h = new Vector3(step.x, 0f, step.z);
        float hLen = h.magnitude;
        if (hLen > 1e-5f)
        {
            if (Physics.SphereCast(body, 0.3f * scale, h / hLen, out RaycastHit wall, hLen + 0.3f * scale, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && wall.collider.GetComponentInParent<Enemy>() == null && wall.collider.GetComponentInParent<Boss>() == null)
            {
                tossVel.x = 0f; tossVel.z = 0f;
                h = Vector3.zero;
            }
        }
        pos += h;
        // The vertical: up freely; down onto the first floor under the feet
        if (step.y >= 0f)
        {
            if (Physics.Raycast(body, Vector3.up, out RaycastHit ceiling, step.y + (CentreHeight + 0.4f) * scale, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && ceiling.collider.GetComponentInParent<Enemy>() == null)
            {
                pos.y = Mathf.Min(pos.y + step.y, ceiling.point.y - (CentreHeight + 0.4f) * scale);
                tossVel.y = Mathf.Min(tossVel.y, 0f);
            }
            else pos.y += step.y;
            transform.position = pos;
            return;
        }
        float drop = -step.y;
        if (Physics.Raycast(pos + Vector3.up * 0.3f, Vector3.down, out RaycastHit floor, drop + 0.3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && floor.collider.GetComponentInParent<Enemy>() == null)
        {
            pos.y = floor.point.y;
            transform.position = pos;
            TossLand(pos);
            return;
        }
        pos.y -= drop;
        transform.position = pos;
        if (pos.y < -200f) { tossed = false; transform.position = spawnPos; } // fell out of the world: back where it stood
    }

    /// Down. A launched footman comes apart on landing (credited: the kick's kill); whatever a body was carrying
    /// lands on everyone around it
    void TossLand(Vector3 at)
    {
        tossed = false;
        tossVel = Vector3.zero;
        if (GameFeel.Instance != null) GameFeel.Instance.PlayUnitLand(at);
        if (role == EnemyRole.Footman && Alive) { Perish(KillMethod.Fist, true); return; }
        if (tossLandDamage <= 0f || tossLandRadius <= 0f) return;
        int n = Physics.OverlapSphereNonAlloc(at + Vector3.up * 0.6f, tossLandRadius, tossHits, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var e = tossHits[i].GetComponentInParent<Enemy>();
            if (e == null || e == this || !e.Alive || e.role == EnemyRole.Boss) continue;
            bool seen = false;
            for (int j = 0; j < i; j++) if (tossHits[j].GetComponentInParent<Enemy>() == e) { seen = true; break; }
            if (seen) continue;
            e.Damage(tossLandDamage, KillMethod.Fist);   // the knuckles' doing: the body was the fist
        }
        tossLandDamage = 0f;
    }

    /// PINNED: thrown at `velocity` — into the wall behind it, where the toss's own wall stop holds it — and dazed
    /// for `seconds` on top of the flight. A unit the toss cannot move is dazed where it stands (never the bouncer:
    /// it stays standing); a boss is neither. Kept for the kick's callers; nothing in the kit pins today.
    public void Pin(Vector3 velocity, float seconds)
    {
        if (!Alive || role == EnemyRole.Boss || role == EnemyRole.Bouncer) return;
        Toss(velocity);
        if (!tossed) Stagger();
        staggeredUntil = Mathf.Max(staggeredUntil, Time.time + Mathf.Max(0f, seconds));
    }

    /// A respawn or a reset puts the feet back on the ground
    void TossReset() { tossed = false; tossVel = Vector3.zero; tossLandDamage = 0f; }
}
