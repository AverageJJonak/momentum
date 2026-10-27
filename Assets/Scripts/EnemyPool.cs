using UnityEngine;

/// <summary>
/// WHICH UNITS A LEVEL FIELDS. The section builders ask for "something on the
/// ground here" or "something in the air there" and the pool answers from the
/// level's roster, in turn, so a room is never the same three runners twice
/// and the roster grows with the game: the first floor is runners, skitters
/// and a moth; by the plant it is all of them. Set per level by
/// GameBootstrap.BuildLevel. The tutorial and the range do not use it (a
/// lesson names its unit) and the district (Chapter 3) names most of its own
/// by hand. A "Skitters" slot is three units in a knot: they are the feed.
/// </summary>
public static class EnemyPool
{
    enum Slot { Chaser, Skitters, Marshal, Warden, Lurker, Scourer, Turret, Moth, Ocular, Overseer }

    static Slot[] ground = { Slot.Chaser };
    static Slot[] air = { Slot.Turret };
    static int gi, ai;

    public static void Configure(LevelKind level)
    {
        gi = ai = 0;
        switch (level)
        {
            case LevelKind.Level1:
                ground = new[] { Slot.Chaser, Slot.Skitters, Slot.Chaser, Slot.Skitters, Slot.Chaser, Slot.Chaser };
                air = new[] { Slot.Turret, Slot.Turret, Slot.Moth };
                break;
            case LevelKind.Level2:
                ground = new[] { Slot.Chaser, Slot.Skitters, Slot.Marshal, Slot.Chaser, Slot.Skitters, Slot.Chaser, Slot.Marshal };
                air = new[] { Slot.Turret, Slot.Moth, Slot.Turret, Slot.Ocular };
                break;
            case LevelKind.Level3:
                ground = new[] { Slot.Chaser, Slot.Lurker, Slot.Skitters, Slot.Marshal, Slot.Chaser, Slot.Skitters, Slot.Lurker, Slot.Chaser };
                air = new[] { Slot.Turret, Slot.Moth, Slot.Ocular, Slot.Turret, Slot.Moth };
                break;
            case LevelKind.Level4:
                ground = new[] { Slot.Warden, Slot.Chaser, Slot.Skitters, Slot.Marshal, Slot.Lurker, Slot.Chaser, Slot.Skitters, Slot.Marshal };
                air = new[] { Slot.Turret, Slot.Ocular, Slot.Overseer, Slot.Moth, Slot.Turret };
                break;
            case LevelKind.Level5:
                ground = new[] { Slot.Chaser, Slot.Marshal, Slot.Warden, Slot.Skitters };
                air = new[] { Slot.Turret, Slot.Ocular };
                break;
            case LevelKind.Level6:
            case LevelKind.Level7:
            case LevelKind.Level8:
            case LevelKind.Level9:
            case LevelKind.Level10:
                ground = new[] { Slot.Chaser, Slot.Skitters, Slot.Marshal, Slot.Warden, Slot.Lurker, Slot.Scourer, Slot.Chaser, Slot.Skitters, Slot.Marshal, Slot.Lurker };
                air = new[] { Slot.Turret, Slot.Moth, Slot.Ocular, Slot.Overseer, Slot.Turret, Slot.Moth };
                break;
            case LevelKind.Level11:
            case LevelKind.Level12:
            case LevelKind.Level13:
                ground = new[] { Slot.Chaser, Slot.Marshal, Slot.Skitters, Slot.Warden, Slot.Scourer, Slot.Lurker };
                air = new[] { Slot.Turret, Slot.Ocular, Slot.Moth, Slot.Overseer };
                break;
            default:
                ground = new[] { Slot.Chaser };
                air = new[] { Slot.Turret };
                break;
        }
    }

    /// Something that walks, standing here
    public static Enemy Ground(Vector3 feet, MovementConfig config)
    {
        Slot s = ground[gi++ % ground.Length];
        switch (s)
        {
            case Slot.Skitters: return Enemy.CreateSkitters(feet, config);
            case Slot.Marshal: return Enemy.CreateMarshal(feet, config);
            case Slot.Warden: return Enemy.CreateWarden(feet, config);
            case Slot.Lurker: return Enemy.CreateLurker(feet, config);
            case Slot.Scourer: return Enemy.CreateScourer(feet, config);
            default: return Enemy.CreateChaser(feet, config);
        }
    }

    /// Something that flies, its body here (where a sentinel's chest would sit)
    public static Enemy Air(Vector3 pos, MovementConfig config)
    {
        Slot s = air[ai++ % air.Length];
        switch (s)
        {
            case Slot.Moth: return Enemy.CreateMoth(pos, config);
            case Slot.Ocular: return Enemy.CreateOcular(pos, config);
            case Slot.Overseer: return Enemy.CreateOverseer(pos + Vector3.up * 2f, config);
            default: return Enemy.CreateTurret(pos, config);
        }
    }
}
