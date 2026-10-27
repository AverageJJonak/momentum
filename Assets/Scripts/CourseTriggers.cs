using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// COURSE TRIGGERS (2026-10-26): the level's logic as components for hand-placed geometry, each a box checked against
/// the player every tick (as the portals are). The clock, the bests, the respawn and the report card are the game's
/// own (LevelTimer, RunState, GameBootstrap, LevelResults): these only tell them when.
///   START LINE    crossing it the first time restarts the clock (a level's own clock starts at load; this is for a
///                 course whose start is not its spawn)
///   FINISH LINE   crossing it finishes the level: the best is saved, the report card comes up, the next scene after
///   SPAWN POINT   crossing it makes it the respawn: a fall or the R key stands you here, facing its forward
///   RESET HEIGHT  the fall line for the level: below `y` you are put back at the last spawn point
/// </summary>
public class StartLine : MonoBehaviour
{
    public Vector3 size = new Vector3(12f, 6f, 1.5f);
    bool crossed;

    void Update()
    {
        var motor = PlayerMotor.Current;
        if (crossed || motor == null) return;
        if (!CourseZones.Bounds(transform, size).Contains(motor.CurrPosition + Vector3.up * 0.9f)) return;
        crossed = true;
        if (LevelTimer.Instance != null) LevelTimer.Instance.Restart();
    }
}

public class FinishLine : MonoBehaviour
{
    public Vector3 size = new Vector3(12f, 6f, 1.5f);
    [Tooltip("The scene after the report card; empty = the run's next scene, or the menu")]
    public string targetScene = "";
    bool done;

    void Update()
    {
        var motor = PlayerMotor.Current;
        if (done || motor == null || LevelResults.Active) return;
        if (!CourseZones.Bounds(transform, size).Contains(motor.CurrPosition + Vector3.up * 0.9f)) return;
        done = true;
        string next = !string.IsNullOrEmpty(targetScene) ? targetScene : RunState.InProgress ? RunState.NextScene : "MainMenu";
        if (LevelTimer.Instance != null) LevelTimer.Instance.SaveBest();
        if (GameFeel.Instance != null) GameFeel.Instance.PlayFinish();
        if (LevelResults.Instance != null) { LevelResults.Instance.Finish(next); return; }
        Time.timeScale = 1f;
        SceneManager.LoadScene(next);
    }
}

public class SpawnPoint : MonoBehaviour
{
    public Vector3 size = new Vector3(12f, 6f, 3f);
    [Tooltip("Where you stand after a respawn here (local to this object)")]
    public Vector3 standAt = new Vector3(0f, 0.1f, 2.5f);
    bool held;

    void Update()
    {
        var motor = PlayerMotor.Current;
        if (motor == null) return;
        bool inside = CourseZones.Bounds(transform, size).Contains(motor.CurrPosition + Vector3.up * 0.9f);
        if (inside && !held)
        {
            GameBootstrap.Checkpoint = transform.TransformPoint(standAt);
            GameBootstrap.CheckpointForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        }
        held = inside;
    }
}

public class ResetHeight : MonoBehaviour
{
    [Tooltip("Below this height the player is put back at the last spawn point")]
    public float y = -35f;

    void Start() => GameBootstrap.KillFloorY = y;
}
