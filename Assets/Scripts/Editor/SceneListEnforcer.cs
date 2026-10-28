using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Keeps the Build Settings scene list correct no matter what: re-applies the
/// canonical scenes after every recompile/editor load (external edits to
/// EditorBuildSettings.asset can be silently ignored or overwritten while the
/// editor is open — this closes that hole for good). Manual: Momentum →
/// Fix Scene List.
/// The list is READ FROM THE CATALOG: the menu, the range and the tutorial,
/// then every level LevelCatalog knows about, in its order. It used to be
/// typed out here as well, which meant a new level had to be added in two
/// places — and the one that was forgotten was this one, which then quietly
/// took the level back OUT of the build list on the next recompile.
/// </summary>
[InitializeOnLoad]
public static class SceneListEnforcer
{
    static readonly string[] Fixed =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/PracticeRange.unity",
        "Assets/Scenes/Tutorial.unity",
        "Assets/Scenes/BeatGym.unity",
    };

    static string[] Paths()
    {
        var list = new List<string>(Fixed);
        foreach (var level in LevelCatalog.All) list.Add("Assets/Scenes/" + level.scene + ".unity");
        return list.ToArray();
    }

    static SceneListEnforcer()
    {
        EditorApplication.delayCall += Enforce;
    }

    [MenuItem("Momentum/Fix Scene List")]
    public static void Enforce()
    {
        // The game is called MOMENTUM; the window title and build say so even if the settings file is overwritten
        if (PlayerSettings.productName != "MOMENTUM") PlayerSettings.productName = "MOMENTUM";
        var paths = Paths();
        var scenes = paths
            .Where(p => AssetDatabase.LoadAssetAtPath<SceneAsset>(p) != null)
            .Select(p => new EditorBuildSettingsScene(p, true))
            .ToArray();
        if (scenes.Length < paths.Length)
            Debug.LogWarning("SceneListEnforcer: some scenes are missing on disk (" + string.Join(", ", paths.Where(p => AssetDatabase.LoadAssetAtPath<SceneAsset>(p) == null)) + "); enforcing the ones that exist.");

        bool same = EditorBuildSettings.scenes.Length == scenes.Length
            && !EditorBuildSettings.scenes.Where((s, i) => s.path != scenes[i].path || !s.enabled).Any();
        if (same) return;

        EditorBuildSettings.scenes = scenes;
        Debug.Log($"SceneListEnforcer: build scene list set ({scenes.Length} scenes, MainMenu first).");
    }
}
