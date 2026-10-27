using UnityEditor;
using UnityEngine;

/// <summary>
/// Progress, for testing: chapters open in order (a chapter opens when the one
/// before it is finished), which is right for players and slow for anyone
/// building Chapter 2. Momentum → Unlock All Chapters sets the override the
/// catalog honours; Lock Chapters removes it; Reset Progress wipes every
/// level's best time and score (settings and bindings are left alone).
/// </summary>
public static class ProgressTools
{
    const string Menu = "Momentum/";

    [MenuItem(Menu + "Unlock All Chapters", false, 60)]
    public static void UnlockAll()
    {
        PlayerPrefs.SetInt("unlock_all", 1);
        PlayerPrefs.Save();
        Debug.Log("Momentum: every chapter is open (override set). Momentum → Lock Chapters puts the rule back.");
    }

    [MenuItem(Menu + "Lock Chapters (use records)", false, 61)]
    public static void LockChapters()
    {
        PlayerPrefs.DeleteKey("unlock_all");
        PlayerPrefs.Save();
        Debug.Log("Momentum: chapters open in order again, from the records.");
    }

    [MenuItem(Menu + "Reset Progress", false, 62)]
    public static void ResetProgress()
    {
        if (!EditorUtility.DisplayDialog("Reset progress", "Wipe every level's best time and score? Settings and key bindings stay.", "Wipe", "Keep"))
            return;
        foreach (var l in LevelCatalog.All)
        {
            PlayerPrefs.DeleteKey("best_" + l.scene);
            PlayerPrefs.DeleteKey("score_" + l.scene);
        }
        PlayerPrefs.DeleteKey("unlock_all");
        PlayerPrefs.Save();
        Debug.Log("Momentum: progress wiped.");
    }
}
