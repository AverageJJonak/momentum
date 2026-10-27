using UnityEditor;
using UnityEngine;

/// <summary>
/// The GameBootstrap inspector gains the level editor's buttons: generate this scene's level outside Play Mode, save
/// the hand edits, clear, open the full Level Editor window. (The same three are on the component's ⋮ context menu.)
/// </summary>
[CustomEditor(typeof(GameBootstrap))]
public class GameBootstrapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var b = (GameBootstrap)target;
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Level Editor", EditorStyles.boldLabel);
        bool playing = EditorApplication.isPlayingOrWillChangePlaymode;
        bool menu = b.level == LevelKind.MainMenu;
        var h = LevelEditor.Holder();
        using (new EditorGUI.DisabledScope(playing || menu))
        {
            if (GUILayout.Button(h == null ? "Generate Level Visuals" : "Regenerate Level Visuals", GUILayout.Height(28f))) LevelEditor.Generate(b.level, b.config);
            using (new EditorGUI.DisabledScope(h == null))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save Edits")) LevelEditor.Save(true);
                if (GUILayout.Button("Clear")) LevelEditor.Clear(true);
            }
            if (GUILayout.Button("Open Level Editor Window")) LevelEditorWindow.Open();
        }
        if (menu) EditorGUILayout.HelpBox("The MainMenu scene builds no level.", MessageType.None);
        else if (h != null) EditorGUILayout.HelpBox(h.kind + " is up under Level_Holder. " + (LevelEditor.Dirty ? "Unsaved edits: Ctrl+S saves them." : "Your edits are saved."), MessageType.None);
    }
}
