using System;

/// <summary>
/// Typed movement events (spec §6: events over polling). ResetDetector, StyleSystem,
/// audio, and VFX will all consume this bus; for now the DebugHUD shows a feed.
/// </summary>
public enum MoveType
{
    Bhop,
    PerfectBhop,
    Slide,
    SlideJump,
    PerfectSlideJump,
    EdgeBoost,   // a wall kick on the VERTICAL corner of a wall, where two faces meet: out of the corner at ×1.4; detail = "×1.4"
    Dash,
    DoubleJump,
    WallKick,
    CornerBoost, // a timed jump ON a ledge's 90° lip: speed becomes height; detail = "+x v  ±ms [PERFECT]", "WALL COMBO ×1.6: ..." off a wall jump
    WallRun,     // attached to a wall while airborne; detail = left / right
    WallJump,    // left a wall run with a push; detail = tangent multiplier
    Slam,
    SlamStorageJump,
    GrappleYank,
    GlitchTech,
    Parry,
    SlamLand, // impact moment, not a freshness move — shockwave + audio hook
    Jump,     // plain jump (audio plumbing, not a freshness move)
    Land,     // landing; detail = fall speed (audio plumbing)
    WallHit,  // ran into a wall at speed; detail = speed into the surface (audio plumbing, not a freshness move)
    SwordStrike, // LMB slash; detail = MISS / HIT / EXECUTE (not a freshness move)
    BowShot,     // a card landed (the deck keeps the bow's names); detail = MISS / HIT / ACE / PINNED / BULLSEYE / POP / BOOM (not a freshness move)
    ParryWhiff,  // the guard came down on nothing or was thrown away; detail = the cause: EMPTY / ARM COMMITTED / CUT THREW IT
    GlassBreak,  // crashed through breakable glass; detail = speed (a freshness move)
    GlassBump,   // too slow for a pane; detail = speed / needed (plumbing)
    Pickup,      // the weapon taken from its case; detail = its name (plumbing, not a freshness move)
    Fist,        // the bare hands; detail = MISS / HIT / TACKLE / PERFECT TACKLE / EXECUTE (not a freshness move)
    BlunderbussShot, // the blunderbuss cone landed; detail = MISS / HIT / HIT ×N / EXECUTE / EXECUTE ×N (not a freshness move)
    BlastJump,   // the blunderbuss kicked you the other way; detail = BLAST, BOTH BARRELS, or "+x m/s  PERFECT" for a shot at a jump's apex (a freshness move)
    Haymaker,    // the knuckles' alt cast: the wound-up hook; detail = MISS / HIT / LAUNCH / EXECUTE, "  PERFECT" appended on a full-charge release (not a freshness move)
    Anchor,      // the deck's alt cast: the anchor card landed; detail = STUCK (a surface) / UNIT (into a body) / MISS (a freshness move)
    MaulSwing,   // the maul's overhead or its charge's blast; detail = MISS / HIT / EXECUTE / CRACK / GLASS, CHARGE / CHARGE MISS / CHARGE HIT ×N / CHARGE EXECUTE ×N (not a freshness move)
    RocketJump,  // the maul's charge threw you; detail = ROCKET JUMP, or "+x m/s  PERFECT CHARGE" for a charge landed as you touched the ground (a freshness move)
    Kick,        // the knuckles' alt cast (v3): the front kick; detail = MISS / HIT / LAUNCH / EXECUTE, or SPRINGBOARD ("SPRINGBOARD  PERFECT" at a jump's apex: you were thrown off the body) (not a freshness move)
    HarpoonShot, // the harpoon gun: the harpoon landed; detail = MISS / HIT / EXECUTE / PINNED / STUCK (a surface) / PULL (the reel pulled a light unit to you) / CUT (the line let go) (not a freshness move)
    Reel,        // the harpoon's REEL zipped you along the line; detail = "ZIP" / "ZIP  UNIT" (to a heavy unit) (a freshness move)
    Glide,       // the umbrella: detail = OPEN (the canopy opened in the air: the glide), POP (a tap: the fall cancelled, the hop) (not a freshness move)
    Trick,       // THE TRICKS (TrickSystem): on the press FRONT FLIP / BACKFLIP / SIDE FLIP / SPIN, "PERFECT " ahead of it when timed to the apex; on the landing "LANDED  +x m/s" (the perfect's pay) or STUMBLE (touched down mid-arc) (the press is a freshness move)
}

public static class MovementEvents
{
    public static event Action<MoveType, string> MoveExecuted;

    public static void Emit(MoveType type, string detail = null)
        => MoveExecuted?.Invoke(type, detail);
}
