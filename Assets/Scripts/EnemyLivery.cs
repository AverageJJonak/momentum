using UnityEngine;

/// <summary>
/// THE STAFF'S DRESSING, POSTER CUT (2026-10-16). Every unit is a clockwork
/// dummy in the House's livery (EnemyMannequin), and a livery is now a FLAT
/// COLOUR FIELD: one hue per kind plus brass — the bellhop's oxblood with a
/// cream chest panel and a pillbox, the doorman's charcoal greatcoat with brass
/// frogging and a peaked cap, the bouncer's wide double-breasted coat and
/// bowler, the dummy's cream canvas with a target — never a greeble. The five
/// platform silhouettes (footman, shooter, bouncer, turret, dummy) are cut in
/// EnemyShell (the body passes called from Livery below); every other kind
/// keeps its own body, its NAME PLATE and a colour variation of the family —
/// a hat for its post, the prop that explains it — so variety stays cheap.
/// Nothing here touches behaviour: Look-only primitives under the EXISTING
/// pivots, so the animator carries them and the ragdoll copier finds every one
/// (Enemy.BoneFor); no colliders, nothing on a centre, an eye or a muzzle; the
/// tells untouched (the plates flash, never the livery; cyan weakspot, red
/// unparryable, amber own light). Names: the EnemyKind enum and every code
/// name stay (other files switch on them); StaffName is what the player reads,
/// and the HUD takes it from here.
/// </summary>
public partial class Enemy
{
    public const string DummyName = "FOOTMAN", TurretName = "PAGE", ChaserName = "BELLHOP";

    /// The staff role a kind wears, as the HUD and the name plate print it. The boss's own name is on its controller
    /// (Boss.Current.BossName, or StaffTitle on the body); by kind alone it reads as the front desk. In the Underground
    /// (the Works zone) the floor manager is the track gang's INSPECTOR.
    public static string StaffName(EnemyKind k) => k switch
    {
        EnemyKind.Dummy => DummyName,
        EnemyKind.Turret => TurretName,
        EnemyKind.Chaser => ChaserName,
        EnemyKind.Boss => "CONCIERGE",
        EnemyKind.Janitor => JanitorName,
        EnemyKind.Borer => BorerName,
        EnemyKind.Rigger => RiggerName,
        EnemyKind.Blaster => BlasterName,
        EnemyKind.Pursuit => PursuitName,
        EnemyKind.Hound => HoundName,
        EnemyKind.Interceptor => InterceptorName,
        EnemyKind.Kettler => KettlerName,
        EnemyKind.DamageControl => DamageControlName,
        EnemyKind.Sentry => SentryName,
        EnemyKind.Skitter => SkitterName,
        EnemyKind.Marshal => MarshalName,
        EnemyKind.Warden => WardenName,
        EnemyKind.Lurker => LurkerName,
        EnemyKind.Moth => MothName,
        EnemyKind.Ocular => OcularName,
        EnemyKind.Overseer => LevelKit.Current == Zone.Works ? InspectorName : OverseerName,
        EnemyKind.Scourer => ScourerName,
        _ => k.ToString().ToUpperInvariant(),
    };

    /// This unit's name as the player reads it (a boss answers with its own)
    public string StaffTitle => kind == EnemyKind.Boss ? (bossKind == BossKind.Foreman ? BossForeman.Name : BossConcierge.Name) : StaffName(kind);

    /// The brass name plate's text: the company first, then the role ("HALCYON · BELLHOP")
    static string Plate(EnemyKind k) => LevelKit.Company + " · " + StaffName(k);

    /// The coat cloth (the Stationmaster's cap crown is cut from it too): charcoal, flat
    static Material Cloth() => Coat();

    // ---------------------------------------------------------------- the pieces

    /// A pillbox: an oxblood drum, its bottom at `at` on the parent, tilted `tiltX` so it can sit on a curved crown.
    /// Cylinder heights are twice their y scale. [1]
    static void Pillbox(Transform parent, Vector3 at, float radius, float tiltX = 0f)
    {
        Transform hat = Pivot(parent, "Hat", at);
        hat.localRotation = Quaternion.Euler(tiltX, 0f, 0f);
        Part(hat, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(radius * 2f, 0.05f, radius * 2f), OxbloodDrum(), "Pillbox");
    }

    /// A bow tie in three pieces, for the Janitor's barrel (the mannequins wear the one-piece Tie: EnemyMannequin)
    static void BowTie(Transform parent, Vector3 at, float width, Material cloth)
    {
        float w = width * 0.42f, h = width * 0.22f;
        Turned(Part(parent, PrimitiveType.Cube, at + new Vector3(-width * 0.28f, 0f, 0f), new Vector3(w, h, 0.012f), cloth, "BowL"), 0f, 0f, 14f);
        Turned(Part(parent, PrimitiveType.Cube, at + new Vector3(width * 0.28f, 0f, 0f), new Vector3(w, h, 0.012f), cloth, "BowR"), 0f, 0f, -14f);
        Part(parent, PrimitiveType.Sphere, at + new Vector3(0f, 0f, 0.004f), Vector3.one * (h * 0.9f), cloth, "BowKnot");
    }

    /// Tailcoat skirts: two panels hanging off the back of a body from `at`, flared back a little [2]
    static void Tails(Transform torso, Vector3 at, float width, float length)
    {
        for (int s = -1; s <= 1; s += 2)
            Turned(Part(torso, PrimitiveType.Cube, at + new Vector3(s * width * 0.62f, -length * 0.5f, 0f), new Vector3(width, length, 0.016f), Coat(), "Tail"), 16f);
    }

    /// Brass buttons from `first`, one `step` apart (the first cut's painted coat still calls it: EnemySuit; the civic
    /// body passes drop them again — the poster draws no buttons)
    static void Buttons(Transform parent, Vector3 first, int count, Vector3 step)
    {
        for (int i = 0; i < count; i++)
            Part(parent, PrimitiveType.Sphere, first + step * i, Vector3.one * 0.026f, BrassBall(), "Button");
    }

    // ---------------------------------------------------------------- the livery

    /// Called by Create after the rig is built and before the component is added. The five platform silhouettes are
    /// cut here (EnemyShell); every other kind is dressed. Every piece hangs under a pivot the ragdoll knows (torso,
    /// head, arms, legs) or under the root (the pelvis bone).
    static void Livery(Transform root, Rig rig, EnemyKind kind)
    {
        if (rig == null || rig.torso == null) return;
        switch (kind)
        {
            case EnemyKind.Dummy: DummyBody(rig); break;
            case EnemyKind.Turret: PageBody(rig); break;
            case EnemyKind.Chaser: BellhopBody(rig); break;
            case EnemyKind.Sentry: DoormanBody(rig); break;
            case EnemyKind.Warden: BouncerBody(rig); break;
            case EnemyKind.Marshal: OfficerBody(rig); break;
            case EnemyKind.Janitor: JanitorLivery(rig); break;
            case EnemyKind.Borer: PlatelayerLivery(rig); break;
            case EnemyKind.Rigger: SignalmanLivery(rig); break;
            case EnemyKind.Blaster: GangerLivery(rig); break;
            case EnemyKind.Pursuit: PatrolmanLivery(rig); break;
            case EnemyKind.Hound: HoundLivery(rig); break;
            case EnemyKind.Interceptor: CourierLivery(rig); break;
            case EnemyKind.Kettler: StokerLivery(rig); break;
            case EnemyKind.DamageControl: HousekeeperLivery(rig); break;
            case EnemyKind.Skitter: BusboyLivery(rig); break;
            case EnemyKind.Lurker: ValetLivery(rig); break;
            case EnemyKind.Moth: MothLivery(rig); break;
            case EnemyKind.Ocular: UsherLivery(rig); break;
            case EnemyKind.Overseer: FloorManagerLivery(rig); break;
            case EnemyKind.Scourer: SweeperLivery(rig); break;
            default: break;   // the bosses dress themselves (EnemyConcierge, EnemyForeman)
        }
    }

    /// Where a civic hat's bottom sits on the head pivot: on the glass oval in the House, on the response's issue (the
    /// optic, the yoke's cap) out in the district
    static float CivicHatY(EnemyKind k) => LevelKit.Current == Zone.Surface ? (k == EnemyKind.Turret ? 0.2f : 0.3f) : 0.17f;

    /// JANITOR (unchanged: he is already a period machine): a black bow tie on the barrel, under the dome
    static void JanitorLivery(Rig rig) => BowTie(rig.torso, new Vector3(0f, 0.96f, 0.518f), 0.18f, Lacquer());

    /// PLATELAYER: a flat cap on the cab roof, its peak over the windscreen (the front is +z) [2]
    static void PlatelayerLivery(Rig rig)
    {
        Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0f, 1.116f, -0.5f), new Vector3(0.5f, 0.013f, 0.5f), CoatDrum(), "FlatCap");
        Turned(Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 1.108f, -0.22f), new Vector3(0.28f, 0.01f, 0.12f), Lacquer(), "CapPeak"), 6f);
    }

    /// SIGNALMAN (a shooter: the Works' doorman): the peaked cap in the track gang's olive on the crown of its glass head [2]
    static void SignalmanLivery(Rig rig) => Cap(rig.headPivot, new Vector3(0f, 0.11f, 0f), 0.1f, OliveDrum());

    /// GANGER (a shooter): an oxblood field painted over the hazard band, no buttons (the gloves are the uniform) [1]
    static void GangerLivery(Rig rig) => ChestPanel(rig.torso, new Vector3(0f, 0.3f, 0.229f), 0.3f, 0.16f, OxbloodShell(), "Waistcoat");

    /// PATROLMAN: the company cap on the lightbar [2]
    static void PatrolmanLivery(Rig rig) => Cap(rig.headPivot, new Vector3(0f, 0.31f, 0f), 0.15f);

    /// HOUND: a brass collar ahead of the shoulders, the tag under it [2]
    static void HoundLivery(Rig rig)
    {
        Turned(Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.56f), new Vector3(0.5f, 0.012f, 0.5f), BrassBall(), "Collar"), 90f);
        Turned(Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0f, -0.23f, 0.56f), new Vector3(0.05f, 0.003f, 0.05f), BrassBall(), "CollarTag"), 90f);
    }

    /// COURIER: a pillbox on the fuselage ahead of the key, an oxblood satchel under the keel with a brass clasp [3]
    static void CourierLivery(Rig rig)
    {
        Pillbox(rig.torso, new Vector3(0f, 0.15f, 0.2f), 0.09f);
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0.44f, -0.17f, 0f), new Vector3(0.2f, 0.17f, 0.08f), OxbloodShell(), "Satchel");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0.44f, -0.2f, 0.046f), new Vector3(0.05f, 0.04f, 0.01f), Brass(), "Clasp");
    }

    /// STOKER: a leather apron painted on the firebox under its door, a fire iron slung on its back beside the key [3]
    static void StokerLivery(Rig rig)
    {
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.1f, 0.258f), new Vector3(0.4f, 0.2f, 0.012f), OxbloodShell(), "LeatherApron");
        Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0.18f, 0.5f, -0.262f), new Vector3(0.025f, 0.42f, 0.025f), WorksIron(), "FireIron");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0.18f, 0.06f, -0.262f), new Vector3(0.06f, 0.05f, 0.025f), Brass(), "IronFoot");
    }

    /// HOUSEKEEPING: the cream apron down the black chest (between the checks, under the name plate) [1]
    static void HousekeeperLivery(Rig rig)
        => Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.03f, 0.393f), new Vector3(0.56f, 0.8f, 0.012f), Cream(), "Apron");

    /// BUSBOY: a black bow tie on the cloche's front, over the lamp [1]
    static void BusboyLivery(Rig rig) => Tie(rig.torso, new Vector3(0f, 0.08f, 0.19f), 0.08f, Lacquer());

    /// VALET: a white tie on its belly (for the dark), tails trailing off the back of the body between the pelvis and
    /// the hind legs (the spine buttons went: no greebles) [3]
    static void ValetLivery(Rig rig)
    {
        Tie(rig.torso, new Vector3(0f, -0.13f, 0.356f), 0.1f, GloveWhite());
        for (int s = -1; s <= 1; s += 2)
            Turned(Part(rig.torso, PrimitiveType.Cube, new Vector3(s * 0.17f, -0.03f, -0.45f), new Vector3(0.12f, 0.016f, 0.24f), Coat(), "Tail"), -20f);
    }

    /// MOTH: a pillbox on the lamp body ahead of the key, and a brass girdle round it [2]
    static void MothLivery(Rig rig)
    {
        Pillbox(rig.torso, new Vector3(0f, 0.11f, 0.12f), 0.06f);
        Turned(Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.06f), new Vector3(0.25f, 0.012f, 0.25f), BrassBall(), "Girdle"), 90f);
    }

    /// USHER: a pillbox on the back of the crown (tilted to sit on the sphere, clear of the lamp on top), and the
    /// torch — a brass hand lamp on the right fin, its amber lens forward. The eye stays the cyan weakspot. [3]
    static void UsherLivery(Rig rig)
    {
        Pillbox(rig.torso, new Vector3(0f, 0.338f, -0.163f), 0.1f, -25f);
        Turned(Part(rig.armR, PrimitiveType.Cylinder, new Vector3(0.1f, 0.06f, 0.04f), new Vector3(0.07f, 0.1f, 0.07f), BrassBall(), "Torch"), 90f);
        Turned(Part(rig.armR, PrimitiveType.Cylinder, new Vector3(0.1f, 0.06f, 0.145f), new Vector3(0.08f, 0.006f, 0.08f), ResponseLamp(), "TorchLens"), 90f);
    }

    /// FLOOR MANAGER / INSPECTOR: a black bow tie standing at the front of the rim, a brass watch chain across the disc [2]
    static void FloorManagerLivery(Rig rig)
    {
        Tie(rig.torso, new Vector3(0f, 0.068f, 0.66f), 0.16f, Lacquer());
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.038f, 0.5f), new Vector3(0.4f, 0.01f, 0.03f), Brass(), "WatchChain");
    }

    /// SWEEPER: a pillbox on the hood, a cream apron on the boiler above the name plate (no buttons: the eight flame
    /// pieces it carries dark count against it) [2]
    static void SweeperLivery(Rig rig)
    {
        Pillbox(rig.torso, new Vector3(0f, 0.583f, 0.04f), 0.12f);
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.16f, 0.218f), new Vector3(0.4f, 0.22f, 0.012f), Cream(), "Apron");
    }
}
