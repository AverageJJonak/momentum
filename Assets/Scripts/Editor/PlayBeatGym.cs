using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Momentum → Play Beat Gym: enters Play Mode and goes straight to the beat gym, whatever scene is open and whatever
/// Play From Main Menu says. The boot is not touched: the game still starts from the main menu as it always does,
/// and the moment play has begun this swaps the gym in (by its asset path, so the build scene list does not
/// matter). The flag lives for one press.
/// </summary>
[InitializeOnLoad]
public static class PlayBeatGym
{
    const string Flag = "Momentum.PlayBeatGym";
    const string ScenePath = "Assets/Scenes/BeatGym.unity";

    static PlayBeatGym() => EditorApplication.playModeStateChanged += OnPlayMode;

    [MenuItem("Momentum/Play Beat Gym", false, 1)]
    static void Play()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (EditorApplication.isPlaying) EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) { Debug.LogError("PlayBeatGym: " + ScenePath + " not found."); return; }
        SessionState.SetBool(Flag, true);
        EditorApplication.EnterPlaymode();
    }

    static void OnPlayMode(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Flag, false)) return;
        SessionState.SetBool(Flag, false);
        EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        Debug.Log("PlayBeatGym: the gym, straight in.");
    }
}
