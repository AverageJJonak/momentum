using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// The game MUST start from the main menu: pressing Play in the editor always
/// launches MainMenu.unity regardless of which scene is open (builds already
/// boot from it via build index 0). Toggle off via Momentum → Play From
/// Main Menu when deliberately iterating on a single scene.
/// </summary>
[InitializeOnLoad]
public static class PlayFromMenu
{
    const string MenuPath = "Momentum/Play From Main Menu";
    const string PrefKey = "momentum_play_from_menu";
    const string ScenePath = "Assets/Scenes/MainMenu.unity";

    static PlayFromMenu()
    {
        EditorApplication.delayCall += Apply;
    }

    static bool Enabled
    {
        get => EditorPrefs.GetBool(PrefKey, true);
        set { EditorPrefs.SetBool(PrefKey, value); Apply(); }
    }

    [MenuItem(MenuPath)]
    static void Toggle() => Enabled = !Enabled;

    [MenuItem(MenuPath, true)]
    static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, Enabled);
        return true;
    }

    static void Apply()
    {
        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        EditorSceneManager.playModeStartScene = Enabled ? scene : null;
        if (Enabled && scene == null)
            Debug.LogWarning("PlayFromMenu: Assets/Scenes/MainMenu.unity not found.");
    }
}
