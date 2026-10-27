using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Speedrun clock for real levels: starts on spawn, shown by the HUD, and a
/// finish portal saves the best time per scene (PlayerPrefs).
/// </summary>
public class LevelTimer : MonoBehaviour
{
    public static LevelTimer Instance { get; private set; }

    float startTime, frozenAt;
    bool frozen;
    public float Elapsed => frozen ? frozenAt - startTime : Time.time - startTime;
    public float Best { get; private set; } = -1f;

    string Key => "best_" + SceneManager.GetActiveScene().name;

    void Awake()
    {
        Instance = this;
        startTime = Time.time;
        Best = PlayerPrefs.GetFloat(Key, -1f);
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    /// The clock starts over: the opening cutscene calls this the frame control comes back, so it costs no time
    public void Restart() { startTime = Time.time; frozen = false; }

    /// The clock stops here and reads this from now on: a level that ends in a cutscene freezes it at the trigger
    public void Freeze()
    {
        if (frozen) return;
        frozen = true;
        frozenAt = Time.time;
    }

    public void SaveBest()
    {
        float t = Elapsed;
        if (Best < 0f || t < Best)
        {
            Best = t;
            PlayerPrefs.SetFloat(Key, t);
            PlayerPrefs.Save();
        }
    }

    public static string Format(float t)
        => t < 0f ? "--:--.--" : $"{(int)(t / 60f):0}:{t % 60f:00.00}";

    /// The run's clock as the register prints it: whole seconds, hours when a run gets long ("12:40", "1:02:40")
    public static string FormatShort(float t)
    {
        if (t < 0f) return "--:--";
        int s = Mathf.FloorToInt(t);
        return s >= 3600 ? $"{s / 3600}:{s / 60 % 60:00}:{s % 60:00}" : $"{s / 60}:{s % 60:00}";
    }
}
