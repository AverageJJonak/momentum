using UnityEngine;
using Cursor = LevelSections.Cursor;

/// Which boss rides ahead: the Concierge on his luggage trolley down the House's galleries (Level 5), the
/// Stationmaster on a tank locomotive down the terminus (Level 10)
public enum ChaseKind { Concierge, Stationmaster }

/// <summary>
/// THE CHASE (2026-10-16, user: "we could do a continuously generating parkour level where we have to keep moving
/// and defeat a moving boss"). It replaces the two gated boss arenas. One section on the +z chain, in three parts:
///   THE COURSE (ChaseCourse) — chunks of 48 × 18 m generated from a seed AHEAD of you as you run, from the course
///     grammar's vocabulary (gaps with a catch lane under every one, lips, kick walls, fluted pillars, glass, orbs,
///     footmen as springboards), stepping up with the phase; dressed as the House's galleries or the terminus.
///   THE PRESSURE (ChasePressure) — a collapse front behind you: the course falls away, red; caught = the one death
///     here; a respawn is the start of the current phase.
///   THE BOSS (ChaseBoss) — rides ahead on the course's centre channel and holds a gap only a hop chain, a
///     springboard or the whip closes; a jab or a kick on his body is a HIT; three hits a phase, three phases, and
///     his vehicle breaks: the chase ends on a finish platform and the level's own tail follows.
/// Every number is here, a const with its reason; the motor's own numbers come from MovementConfig (never 8 by hand).
/// The section returns a Cursor whose z is the finish platform's end and whose floor is its floor, like any section;
/// the tail (Level 5's Finish, Level 10's lift) is built there at build time. When the boss breaks early the finish
/// platform is generated where you are, and its doors (the House's lifts / the tunnel mouth) carry you to that tail.
/// </summary>
public static class Chase
{
    // ------------------------------------------------------------ THE COURSE
    public const float ChunkLen = 48f;        // one chunk along +z (six seconds at run speed)
    public const float HalfW = 9f;            // the lane: 18 m wide
    public const float ChanHalf = 1.6f;       // the centre channel the vehicle rides (the luggage way / the track): features stay off it
    public const float ChanDropHouse = 0.3f;  // how far the channel floor sits under the platforms: a sunken luggage way...
    public const float ChanDropTerminus = 1f; // ...or a real track bed under two platforms
    public const float HeightHouse = 18f;     // the galleries' hall (the Atrium was 26; its chandeliers hang here)
    public const float HeightTerminus = 14f;  // the terminus vault
    public const float WallBelow = 5.5f;      // the side walls run this far under the floor: the catch lanes are walled
    public const int Ahead = 4;               // chunks kept built ahead of the one you are in (the far one finishes 150 m off)
    public const int Behind = 2;              // chunks this far behind you are destroyed whatever the front did
    public const float BudgetMs = 2f;         // the generator's work per frame; a chunk takes a few frames
    public const int MaxChunks = 30;          // 1440 m to the buffers: a boss never caught stops there and takes his hits standing
    public const float CatchDrop = 4f;        // the catch lane under every gap: a miss costs time, not life
    public const float CatchPadUp = 16f;      // the pad on the catch lane (the kit's own numbers: rises 5 m, over the 3 m riser)
    public const float CatchPadAlong = 6f;
    public const float FinishLen = 40f;       // the finish platform: a wide floor, the front dies on it
    public const float DropSeconds = 2.2f;    // a chunk the front passed falls for this long, then is destroyed

    // Per phase (index = phase − 1). Gaps are capped by the motor's own jump (RunJump) so the table never asks more than
    // the ladder gives: phase 1 ≤ 0.7 × a run jump, phase 2 ≤ a run jump, phase 3 up to a run jump and a fifth (the
    // double jump, the dash). Lips climb the corner-boost ladder. Kick walls arrive in phase 2 and chain in phase 3.
    public static readonly float[] GapMin = { 2.5f, 4f, 5.5f };
    public static readonly float[] GapMax = { 4f, 5.8f, 7f };
    public static readonly float[] GapCapMult = { 0.7f, 1f, 1.2f };
    public static readonly int[] GapsMin = { 1, 2, 2 };
    public static readonly int[] GapsMax = { 2, 2, 3 };
    public static readonly float[] LipMin = { 0.6f, 0.8f, 1f };
    public static readonly float[] LipMax = { 0.9f, 1.1f, 1.3f };
    public static readonly int[] Footmen = { 1, 2, 2 };

    // ------------------------------------------------------------ THE PRESSURE
    public const float FrontStart = 6.5f;      // the front's speed when the chase begins, m/s
    public const float FrontRise = 0.15f;      // and what it gains...
    public const float FrontRiseEvery = 10f;   // ...every ten seconds
    public static readonly float[] FrontCap = { 0.9f, 1f, 1.1f };   // × run speed by phase: 7.2, 8, 8.8 — phase 3 only hop chains stay clear
    public const float FrontBehindStart = 40f; // where the front begins behind the start, and resets to behind a checkpoint
    public const float FrontKillGrace = 1f;    // seconds between catches (one death, not sixty)

    // ------------------------------------------------------------ THE BOSS
    public const float BossSpeedMult = 1.04f;  // × run speed when you are far: 8.32
    public const float HoldGap = 14f;          // the gap he holds when you run at run speed
    public const float FarGap = 46f;           // beyond this he eases off so you can find him again
    public const float HoldGain = 0.6f;        // how hard he corrects toward the gap, per metre of error
    public const float BossAccel = 12f;        // m/s² toward his wanted speed
    public const float SurgeDist = 25f;        // a hit throws him this far ahead...
    public const float SurgeTime = 1f;         // ...over this long: you cannot land two in a row
    public const int HitsPerPhase = 3, Phases = 3;
    public const int HitsToBreak = HitsPerPhase * Phases;   // 9
    public const float HitCooldown = 0.5f;     // a jab and a kick on one press count once
    public const float IntroSeconds = 2f;      // the view pulled to him as he pulls away
    public const float BossLead = 30f;         // where he waits ahead of the start, and resets to ahead of a checkpoint
    public const float BossScaleHouse = 2.2f;  // the Concierge's rig scale on the trolley (the arena's was 4.5)
    public const float BossScaleTerminus = 2.4f;
    public const float WreckSlide = 12f;       // how far the broken vehicle slides before it stops
    public const float WreckSeconds = 2.5f;    // the derail
    public const float DropAheadOfYou = 4f;    // an obstacle lands this far ahead of you (never on you)

    /// The motor's run jump: how far a jump at run speed carries (jumpImpulse × 2 / gravity × run): 5.76 m by default
    public static float RunJump(MovementConfig c) => c.jumpImpulse * 2f / Mathf.Max(1f, c.gravity) * c.baseRunSpeed;
    public static float Height(ChaseKind k) => k == ChaseKind.Concierge ? HeightHouse : HeightTerminus;
    public static float ChanDrop(ChaseKind k) => k == ChaseKind.Concierge ? ChanDropHouse : ChanDropTerminus;

    /// THE CHASE as a section: the threshold, the checkpoint, the course's first chunks (whole, at build time), the
    /// finish platform at the buffers (static: the warp's target when the boss breaks early), the boss, the front.
    /// Returns the cursor at the finish platform's end, open, on the same floor
    public static Cursor Build(Transform root, MovementConfig config, Cursor c, ChaseKind kind)
    {
        LevelKit.SetZone(c.zone);
        LevelKit.Life(root);   // the pads' animator lives under the level root, never under a chunk that will be destroyed
        float height = Height(kind), y = c.floorY, z0 = c.z;
        float zEnd = z0 + MaxChunks * ChunkLen;
        LevelSections.Seal(root, c, HalfW, y, height);
        LevelSections.LandGlass(root, c);
        LevelKit.Arch(root, HalfW, y, height, z0 + 1.3f);
        var cp = c; cp.halfW = HalfW;
        Course.Checkpoint(root, cp, "THE CHASE");
        LevelKit.Air(root, c.zone, new Vector3(-HalfW, y - WallBelow, z0), new Vector3(HalfW, y + height, zEnd + FinishLen), false);

        var go = new GameObject("TheChase");
        go.transform.SetParent(root);
        var course = go.AddComponent<ChaseCourse>();
        course.Init(root, config, kind, c.zone, y, z0, LevelSections.Seed);
        course.BuildFinishPlatform(root, zEnd, false);
        var boss = ChaseBoss.Create(root, config, course, kind, y, z0 + BossLead, zEnd - 8f);
        var front = ChasePressure.Create(root, config, course, kind, y, z0 - FrontBehindStart);
        course.Attach(boss, front);
        course.Prebuild();

        var next = LevelSections.Exit(c, zEnd + FinishLen, y, HalfW, y, y + height);
        next.open = true;
        return next;
    }
}
