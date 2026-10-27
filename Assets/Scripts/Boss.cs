using UnityEngine;

/// <summary>
/// What every boss is to the rest of the game. The Concierge (Chapter 1) and
/// the Foreman (Chapter 2) each drive their own body their own way; this is the
/// surface the HUD's bar, the intro, the exit and the body itself talk to, so
/// none of them know which one is on the floor. One boss per scene: Current.
/// </summary>
public abstract class Boss : MonoBehaviour
{
    public static Boss Current { get; protected set; }

    public MovementConfig config;
    public Enemy body;
    public Vector3 arenaCenter;
    public float arenaRadius = 27f;
    public float floorY;
    public Vector3 checkpoint;
    public GameObject entrySeal, exitSeal;

    // ------------------------------------------------------------ the cards

    /// The name on the bar and the title card
    public abstract string BossName { get; }
    /// The company line under the name on the intro card
    public abstract string CompanyLine { get; }
    /// What the intro card reads on its one rot tick
    public abstract string IntroBeat { get; }
    /// The stamp that slams down over the exit (CHECKED OUT)
    public abstract string ExitStamp { get; }
    /// After the name on the exit card (SERVICE TERMINATED)
    public abstract string ExitLine { get; }
    /// What the stamp reads when the slab ticks red as the bars lift
    public abstract string LastWord { get; }
    /// The boss's last line, under the bars, while the exit plays
    public abstract string LastLine { get; }
    /// The phase whose arrival ticks the HUD red, once, and what it reads on that tick
    public abstract string BeatPhase { get; }
    public abstract string BeatWord { get; }

    // ------------------------------------------------------------ the bar

    public abstract bool Engaged { get; }
    public abstract bool IsDead { get; }
    /// The row of plates (buttons, hull plates) under the bar, while they are still on
    public abstract bool ShowPlates { get; }
    public abstract int PlateCount { get; }
    public abstract int PlatesLeft { get; }
    public abstract string PhaseName { get; }
    /// Seconds the timed weakspot stays open after a stagger
    public abstract float StaggerWindow { get; }

    public float Health01 => body != null ? body.Health01 : 0f;
    /// The live bar: a ghost fill that catches up after each bite, the bite's size and name
    public float DisplayHealth01 { get; protected set; } = 1f;
    public float LastHitTime { get; protected set; } = float.NegativeInfinity;
    public float LastHitFraction { get; protected set; }
    public string LastHitLabel { get; protected set; } = "";
    /// The weakspot open right now, named for the bar (empty when none)
    public string OpenSpot { get; protected set; } = "";
    /// The timed weakspot (the throat, the core) is open: the bar runs its timer down
    public bool WindowOpen { get; protected set; }
    public float StaggerLeft { get; protected set; }
    public string Subtitle { get; protected set; }
    public float SubtitleUntil { get; protected set; }
    /// The fight's numbers, for the exit stamp
    public float EngagedAt { get; protected set; }
    public int HitsTaken { get; protected set; }
    public int Counters { get; protected set; }

    // ------------------------------------------------------------ the choreography's calls

    /// 0 = under the floor, 1 = standing: the intro raises the body
    public abstract void SetRise(float k);
    public abstract void SealEntry();
    public abstract void BeginIntro();
    /// The cutscene ends: the fight is on
    public abstract void Engage();
    /// The body asks before taking damage; every answer shows on the bar
    public abstract bool FilterDamage(ref float amount, KillMethod method);
    /// A parry landed on a countered attack (from the controller, or the director's melee parry near the body)
    public abstract void OnCountered();
    /// The exit's burst: the body comes apart (the outro throws the pieces)
    public abstract GameObject Fall();
    /// The exit is over: the way out opens
    public abstract void Dismissed();
}
