using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Momentum → Level Editor → Open Level Editor: the level's buttons in one dockable window — generate, save, throw
/// away, revert, select what you changed, add a block, paint with the level's own materials, redraw the HARD LINE.
/// </summary>
public class LevelEditorWindow : EditorWindow
{
    List<LevelEditor.Swatch> palette = new List<LevelEditor.Swatch>();
    int picked;
    string search = "";
    Vector2 scroll;
    bool showOrphans;
    GUIStyle wrap;

    public static void Open()
    {
        var w = GetWindow<LevelEditorWindow>("Level Editor");
        w.minSize = new Vector2(300f, 420f);
        w.RefreshPalette();
    }

    void OnEnable() { LevelEditor.Changed += Repaint; Selection.selectionChanged += Repaint; RefreshPalette(); }
    void OnDisable() { LevelEditor.Changed -= Repaint; Selection.selectionChanged -= Repaint; }

    void RefreshPalette()
    {
        palette = LevelEditor.Palette();
        if (picked >= palette.Count) picked = 0;
    }

    void OnGUI()
    {
        if (wrap == null) wrap = new GUIStyle(EditorStyles.label) { wordWrap = true };
        bool playing = EditorApplication.isPlayingOrWillChangePlaymode;
        var h = LevelEditor.Holder();
        var b = LevelEditor.FindBootstrap();

        EditorGUILayout.Space(4f);
        if (playing) { EditorGUILayout.HelpBox("The Level Editor works outside Play Mode. Leave Play and the level comes back with your edits.", MessageType.Info); return; }

        // The level
        string sceneLevel = b != null ? b.level.ToString() : "none (no GameBootstrap in this scene)";
        EditorGUILayout.LabelField("This scene builds", sceneLevel, EditorStyles.boldLabel);
        if (h != null)
        {
            EditorGUILayout.LabelField("In the editor", h.kind + " · " + h.keys.Count.ToString("N0") + " pieces");
            var c = LevelEditor.LastCounts;
            string state = LevelEditor.Dirty ? "UNSAVED changes (Ctrl+S saves them)" : "everything saved";
            EditorGUILayout.LabelField("Edits", state, LevelEditor.Dirty ? EditorStyles.boldLabel : EditorStyles.label);
            EditorGUILayout.LabelField(" ", c.edits + " edit(s) · " + c.adds + " addition(s)" + (c.spawn ? " · spawn moved" : "") + (c.unsupported > 0 ? " · " + c.unsupported + " cannot be kept" : ""), EditorStyles.miniLabel);
        }
        else EditorGUILayout.LabelField("In the editor", "nothing built yet");

        EditorGUILayout.Space(4f);
        using (new EditorGUI.DisabledScope(b == null || b.level == LevelKind.MainMenu))
        {
            if (GUILayout.Button(h == null ? "Generate Level Visuals" : "Regenerate (keeps your edits)", GUILayout.Height(30f))) { LevelEditor.Generate(b.level, b.config); RefreshPalette(); }
        }
        using (new EditorGUI.DisabledScope(h == null))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save Edits", GUILayout.Height(24f))) LevelEditor.Save(true);
                using (new EditorGUI.DisabledScope(!LevelEditor.Dirty))
                    if (GUILayout.Button("Throw Away Unsaved", GUILayout.Height(24f))) { LevelEditor.Discard(true); RefreshPalette(); }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Clear Level")) LevelEditor.Clear(true);
                if (GUILayout.Button("Reset Level To Code…")) { LevelEditor.ResetToCode(); RefreshPalette(); }
            }

            // Edits the saved file holds that no longer find a piece
            if (h != null && h.orphans.Count > 0)
            {
                showOrphans = EditorGUILayout.Foldout(showOrphans, h.orphans.Count + " saved edit(s) found no piece", true);
                if (showOrphans)
                {
                    EditorGUILayout.HelpBox("A builder has changed since these were made. Save Edits drops them; fix the place by hand again if needed.", MessageType.Warning);
                    foreach (var o in h.orphans) EditorGUILayout.LabelField(o, EditorStyles.miniLabel);
                }
            }

            // The selection
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Pieces", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select What I Changed")) LevelEditor.SelectChanged();
                using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0))
                    if (GUILayout.Button("Revert Selection")) LevelEditor.RevertSelection();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Block")) LevelEditor.AddBlock(picked < palette.Count ? palette[picked].material : null);
                if (GUILayout.Button("Redraw Hard Lines")) LevelEditor.RefreshLines();
            }

            // The palette
            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Paint (the level's materials)", EditorStyles.boldLabel);
                if (GUILayout.Button("↻", GUILayout.Width(26f))) RefreshPalette();
            }
            search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
            using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0 || picked >= palette.Count))
                if (GUILayout.Button(picked < palette.Count ? "Paint Selection: " + palette[picked].name : "Paint Selection")) LevelEditor.Paint(palette[picked].material);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(120f));
            for (int i = 0; i < palette.Count; i++)
            {
                var s = palette[i];
                if (!string.IsNullOrEmpty(search) && s.name.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                var row = EditorGUILayout.GetControlRect(false, 20f);
                if (i == picked) EditorGUI.DrawRect(row, new Color(0.24f, 0.37f, 0.6f, 0.5f));
                EditorGUI.DrawRect(new Rect(row.x + 2f, row.y + 2f, 16f, 16f), new Color(s.colour.r, s.colour.g, s.colour.b, 1f));
                GUI.Label(new Rect(row.x + 24f, row.y + 2f, row.width - 80f, 18f), s.name);
                GUI.Label(new Rect(row.xMax - 52f, row.y + 2f, 50f, 18f), s.uses.ToString("N0"), EditorStyles.miniLabel);
                if (Event.current.type == EventType.MouseDown && row.Contains(Event.current.mousePosition)) { picked = i; Event.current.Use(); Repaint(); }
            }
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.Space(6f);
        LevelEditor.AutoGenerate = EditorGUILayout.ToggleLeft("Build the level when a level scene opens", LevelEditor.AutoGenerate);
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Move, rotate and scale pieces as usual. Delete or untick a piece to remove it. Ctrl+D copies a piece. Add Block makes a new one; Paint gives it a material. Move the gold capsule to move the spawn. Ctrl+S saves; Play uses your edits even before you save.", wrap);
    }
}
