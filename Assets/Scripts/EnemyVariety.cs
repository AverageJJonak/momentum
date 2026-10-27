using UnityEngine;

/// <summary>
/// THE VARIETY: the eight civic bodies after ULTRAKILL's roster — the busboy
/// (Skitter), the house officer (Marshal), the bouncer (Warden), the valet
/// (Lurker), the moth, the usher (Ocular), the floor manager (Overseer), the
/// sweeper (Scourer). Since the simplification (2026-10-16) only their BODIES
/// remain (EnemyVarietyRigs: the dressing stays, so a gallery still reads as a
/// crowd of different staff): what they DO is one of the roles in EnemyRoles —
/// the bouncer is THE WALL, the valet a footman that stands, every other one a
/// footman on its line. All ride the Enemy skeleton (torso, head, arms, legs),
/// so they lock on, ragdoll and count like the rest.
/// </summary>
public partial class Enemy
{
    // The staff roles these read as since the House overhaul (EnemyLivery.StaffName); the code names above never change
    public const string SkitterName = "BUSBOY", MarshalName = "HOUSE OFFICER", WardenName = "BOUNCER", LurkerName = "VALET",
        MothName = "MOTH", OcularName = "USHER", OverseerName = "FLOOR MANAGER", InspectorName = "INSPECTOR", ScourerName = "SWEEPER";

    static bool IsVariety(EnemyKind k) => k == EnemyKind.Skitter || k == EnemyKind.Marshal || k == EnemyKind.Warden || k == EnemyKind.Lurker
        || k == EnemyKind.Moth || k == EnemyKind.Ocular || k == EnemyKind.Overseer || k == EnemyKind.Scourer;

    static Material maPlate, waPlate, lkPlate, moPlate, ovPlate, scPlate, guardMat, flameMat, beamMat;
    static Material VarietyPlate(EnemyKind k) => k switch
    {
        EnemyKind.Skitter => Enamel(),                                                     // the busboy's cloche: cream enamel
        EnemyKind.Marshal => maPlate ??= Lit(new Color(0.05f, 0.05f, 0.06f), 0.3f, 0.9f),   // black lacquer: the rank
        EnemyKind.Warden => waPlate ??= Lit(new Color(0.06f, 0.06f, 0.07f), 0.5f, 0.75f),    // black lacquer: the bouncer's double-breasted coat (poster cut 2026-10-16)
        EnemyKind.Lurker => lkPlate ??= Lit(new Color(0.07f, 0.065f, 0.065f), 0.1f, 0.3f),  // soot: matte black, for the dark
        EnemyKind.Moth => moPlate ??= Lit(new Color(0.82f, 0.78f, 0.62f), 0.2f, 0.6f),      // ivory: the wings
        EnemyKind.Ocular => Enamel(),                                                      // the usher's ball: cream enamel
        EnemyKind.Overseer => ovPlate ??= Lit(new Color(0.85f, 0.82f, 0.72f), 0.5f, 0.8f),  // bone
        _ => scPlate ??= Lit(new Color(0.5f, 0.22f, 0.08f), 0.4f, 0.7f),                    // rust: oxide primer
    };
    static Material GuardMat() => guardMat ??= Lit(new Color(0.16f, 0.16f, 0.18f), 0.9f, 0.6f);     // the bouncer's plate: iron
    static Material FlameMat() => flameMat ??= Unlit(new Color(2.6f, 0.9f, 0.15f));                 // the sweeper's pilot lamp (look only)
    static Material BeamMat() => beamMat ??= Unlit(new Color(2.6f, 0.2f, 0.1f));                    // the usher's dark beam pieces (never lit now)

    public static Enemy CreateSkitter(Vector3 groundPos, MovementConfig config) => Create(EnemyKind.Skitter, groundPos, config);

    /// Three, in a knot round the spot: the first one is handed back
    public static Enemy CreateSkitters(Vector3 groundPos, MovementConfig config)
    {
        Enemy first = null;
        for (int i = 0; i < 3; i++)
        {
            var e = Create(EnemyKind.Skitter, groundPos + Quaternion.Euler(0f, i * 120f, 0f) * Vector3.forward * 1.5f, config);
            if (e == null) return null;   // no units (Enemy.Units): nothing to shape
            if (first == null) first = e;
        }
        return first;
    }

    public static Enemy CreateMarshal(Vector3 groundPos, MovementConfig config) => Create(EnemyKind.Marshal, groundPos, config);

    /// THE BOUNCER: the wall, at 1.15× (EnemyRoles)
    public static Enemy CreateWarden(Vector3 groundPos, MovementConfig config)
    {
        var e = Create(EnemyKind.Warden, groundPos, config);
        if (e == null) return null;   // no units (Enemy.Units): nothing to shape
        e.transform.localScale = Vector3.one * 1.15f;
        return e;
    }

    public static Enemy CreateLurker(Vector3 groundPos, MovementConfig config) => Create(EnemyKind.Lurker, groundPos, config);

    /// The old flyers: `pos` is where the body was asked to sit; they are footmen now, and Settle drops them to the floor under it
    public static Enemy CreateMoth(Vector3 pos, MovementConfig config) => Create(EnemyKind.Moth, pos - Vector3.up * ChestHeight, config);
    public static Enemy CreateOcular(Vector3 pos, MovementConfig config) => Create(EnemyKind.Ocular, pos - Vector3.up * ChestHeight, config);
    public static Enemy CreateOverseer(Vector3 pos, MovementConfig config) => Create(EnemyKind.Overseer, pos - Vector3.up * ChestHeight, config);

    public static Enemy CreateScourer(Vector3 groundPos, MovementConfig config)
    {
        var e = Create(EnemyKind.Scourer, groundPos, config);
        if (e == null) return null;   // no units (Enemy.Units): nothing to shape
        e.transform.localScale = Vector3.one * 1.1f;
        return e;
    }
}
