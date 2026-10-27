using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// THE LEVEL EDITOR (2026-10-20): fix a level by hand. The game builds every level from code when its scene loads, so
/// this builds the same level in the editor, outside Play Mode, under one root called Level_Holder; you move, turn,
/// scale, delete, duplicate (Ctrl+D) and paint its pieces like any scene objects; and what you changed is saved as a
/// small per-level file (Resources/LevelEdits, see LevelEdits.cs) that the game lays over the level it builds.
///
/// The working copy is never written into the .unity file (forty thousand generated pieces with their generated
/// meshes and textures would bloat every scene and build the level twice on Play): it is rebuilt, with your edits,
/// whenever the scene opens and whenever you come back from Play, so it never disappears. Ctrl+S saves your edits (the
/// scene is marked changed when you change the level); Play uses unsaved edits the way it uses an unsaved scene.
/// </summary>
[InitializeOnLoad]
public static partial class LevelEditor
{
    public const string HolderName = "Level_Holder";
    const string ConfigPath = "Assets/Config/MovementConfig.asset";
    const string AutoPref = "Momentum.LevelEditor.AutoGenerate";
    const string DirtyKey = "Momentum.LevelEditor.Dirty";
    const string RegenKey = "Momentum.LevelEditor.RegenAfterPlay";
    const string StartedKey = "Momentum.LevelEditor.Started";
    const string Menu = "Momentum/Level Editor/";

    /// Build the level by itself when a level scene opens
    public static bool AutoGenerate { get => EditorPrefs.GetBool(AutoPref, true); set => EditorPrefs.SetBool(AutoPref, value); }

    /// The working copy has changes the saved file does not
    public static bool Dirty
    {
        get => SessionState.GetBool(DirtyKey, false);
        private set { SessionState.SetBool(DirtyKey, value); Changed?.Invoke(); }
    }

    /// The window repaints on it
    public static event Action Changed;

    /// Generating, saving or refreshing: the changes are ours, not the user's
    static bool working;
    /// The change events our own work publishes arrive a moment after it (the undo registration of the new root): ignored
    static double quietUntil;
    static void Quiet() => quietUntil = EditorApplication.timeSinceStartup + 0.75;

    static LevelEditor()
    {
        LevelEditorHooks.Generate = b => Generate(b.level, b.config);
        LevelEditorHooks.Save = _ => Save(true);
        LevelEditorHooks.Clear = _ => Clear(true);
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorSceneManager.sceneSaving += OnSceneSaving;
        EditorSceneManager.sceneOpened += (s, mode) => EditorApplication.delayCall += AutoForOpenScene;
        ObjectChangeEvents.changesPublished += OnChanges;
        Undo.undoRedoPerformed += OnUndoRedo;
        if (!SessionState.GetBool(StartedKey, false))
        {
            // A new editor session: unsaved edits from a session that crashed are not trusted, and the open scene's level is built
            SessionState.SetBool(StartedKey, true);
            Dirty = false;
            try { string pending = Path.GetDirectoryName(LevelEdits.PendingPath(LevelKind.Level1)); if (Directory.Exists(pending)) Directory.Delete(pending, true); }
            catch (Exception) { }
            EditorApplication.delayCall += AutoForOpenScene;
        }
    }

    // ------------------------------------------------------------ menus

    [MenuItem(Menu + "Open Level Editor", false, 0)]
    public static void OpenWindow() => LevelEditorWindow.Open();

    [MenuItem(Menu + "Generate This Scene's Level", false, 20)]
    public static void GenerateThisScene()
    {
        var b = FindBootstrap();
        if (b == null) { Debug.LogError("Momentum: no GameBootstrap in the open scene. Open a level scene (Level1 … Level13, Tutorial, PracticeRange)."); return; }
        Generate(b.level, b.config);
    }

    [MenuItem(Menu + "Save Edits", false, 21)]
    public static void SaveMenu() => Save(true);

    [MenuItem(Menu + "Clear Level", false, 22)]
    public static void ClearMenu() => Clear(true);

    [MenuItem(Menu + "Generate Another Level/Practice Range", false, 40)] static void G0() => Generate(LevelKind.PracticeRange, null);
    [MenuItem(Menu + "Generate Another Level/Tutorial", false, 41)] static void G1() => Generate(LevelKind.Tutorial, null);
    [MenuItem(Menu + "Generate Another Level/Level 1", false, 42)] static void G2() => Generate(LevelKind.Level1, null);
    [MenuItem(Menu + "Generate Another Level/Level 2", false, 43)] static void G3() => Generate(LevelKind.Level2, null);
    [MenuItem(Menu + "Generate Another Level/Level 3", false, 44)] static void G4() => Generate(LevelKind.Level3, null);
    [MenuItem(Menu + "Generate Another Level/Level 4", false, 45)] static void G5() => Generate(LevelKind.Level4, null);
    [MenuItem(Menu + "Generate Another Level/Level 5", false, 46)] static void G6() => Generate(LevelKind.Level5, null);
    [MenuItem(Menu + "Generate Another Level/Level 6", false, 47)] static void G7() => Generate(LevelKind.Level6, null);
    [MenuItem(Menu + "Generate Another Level/Level 7", false, 48)] static void G8() => Generate(LevelKind.Level7, null);
    [MenuItem(Menu + "Generate Another Level/Level 8", false, 49)] static void G9() => Generate(LevelKind.Level8, null);
    [MenuItem(Menu + "Generate Another Level/Level 9", false, 50)] static void G10() => Generate(LevelKind.Level9, null);
    [MenuItem(Menu + "Generate Another Level/Level 10", false, 51)] static void G11() => Generate(LevelKind.Level10, null);
    [MenuItem(Menu + "Generate Another Level/Level 11", false, 52)] static void G12() => Generate(LevelKind.Level11, null);
    [MenuItem(Menu + "Generate Another Level/Level 12", false, 53)] static void G13() => Generate(LevelKind.Level12, null);
    [MenuItem(Menu + "Generate Another Level/Level 13", false, 54)] static void G14() => Generate(LevelKind.Level13, null);

    // ------------------------------------------------------------ the working copy

    public static GameBootstrap FindBootstrap() => Object.FindAnyObjectByType<GameBootstrap>(FindObjectsInactive.Include);

    public static LevelHolder Holder()
    {
        foreach (var h in Object.FindObjectsByType<LevelHolder>(FindObjectsInactive.Include))
            if (h != null) return h;
        return null;
    }

    /// Builds `kind` in the open scene, outside Play Mode, exactly as the game would (the same builders, the saved
    /// edits laid on, the same passes after), under Level_Holder. A working copy already up is replaced, its unsaved
    /// edits carried over
    public static void Generate(LevelKind kind, MovementConfig config, bool quiet = false)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.Log("Momentum: the level is already built while playing; the Level Editor works outside Play Mode."); return; }
        if (kind == LevelKind.MainMenu) { if (!quiet) Debug.Log("Momentum: the MainMenu scene builds no level."); return; }
        if (config == null) config = AssetDatabase.LoadAssetAtPath<MovementConfig>(ConfigPath);
        if (config == null) { Debug.LogError("Momentum: " + ConfigPath + " not found."); return; }

        var old = Holder();
        if (old != null)
        {
            if (Dirty) WritePending(old.kind, Capture(old, null, null));   // regenerating keeps what you have not saved yet
            DestroyHolder(old);
        }

        working = true;
        try
        {
            var scene = SceneManager.GetActiveScene();
            var before = new HashSet<GameObject>(scene.GetRootGameObjects());
            EditorUtility.DisplayProgressBar("Momentum Level Editor", "Building " + kind + " …", 0.15f);
            Vector3 spawn = GameBootstrap.BuildLevel(kind, config);
            var built = LevelEdits.NewRoots(scene, before);

            // 1. Who every piece is, as built (before anything moves)
            EditorUtility.DisplayProgressBar("Momentum Level Editor", "Marking the pieces …", 0.45f);
            var keys = new List<string>();
            var parents = new List<int>();
            var index = new Dictionary<string, int>();
            LevelEdits.Walk(built, (t, key, parentKey) =>
            {
                var p = t.gameObject.AddComponent<LevelPiece>();
                p.key = key;
                p.origin = Origin(t.gameObject);
                p.gParent = parentKey;
                p.gPos = t.localPosition; p.gRot = t.localRotation; p.gScale = t.localScale;
                var r = t.GetComponent<Renderer>();
                p.gMat = r != null && r.sharedMaterial != null ? LevelKit.UntiledOf(r.sharedMaterial).name : "";
                p.gActive = t.gameObject.activeSelf;
                index[key] = keys.Count;
                keys.Add(key);
                parents.Add(!string.IsNullOrEmpty(parentKey) && index.TryGetValue(parentKey, out int pi) ? pi : -1);
            });

            // 2. The edits, laid on as the game lays them (the unsaved ones if there are any)
            EditorUtility.DisplayProgressBar("Momentum Level Editor", "Laying your edits on …", 0.6f);
            var res = LevelEdits.Apply(LevelEdits.Load(kind), built, config);
            foreach (var kv in res.edits) { var p = kv.Key.GetComponent<LevelPiece>(); if (p != null) p.hadEdit = true; }
            foreach (var kv in res.adds)
            {
                var a = kv.Key.GetComponent<LevelAdded>();
                if (a == null) a = kv.Key.AddComponent<LevelAdded>();
                a.source = kv.Value.source;
                a.primitive = kv.Value.primitive;
            }

            // 3. The passes the game runs next. "Untouched" is the level after the coplanar fix, so its nudges are never edits
            EditorUtility.DisplayProgressBar("Momentum Level Editor", "The coplanar fix, the HARD LINE, the probes …", 0.75f);
            CoplanarFixer.Run();
            Physics.SyncTransforms();
            foreach (var r in LevelEdits.NewRoots(scene, before))
                foreach (var p in r.GetComponentsInChildren<LevelPiece>(true))
                {
                    var t = p.transform;
                    p.bPos = t.localPosition; p.bRot = t.localRotation; p.bScale = t.localScale;
                }
            var beforePasses = new HashSet<GameObject>(scene.GetRootGameObjects());
            GameBootstrap.HardLine(kind);
            LevelLint.Run();
            LevelKit.RenderProbes();
            MaterialNet.Run();   // nothing pink in the editor either (the whole scene: the city and the haze too), and the console says why if something was

            // 4. One root holds it all, in the active scene, undoable, never saved
            var holder = new GameObject(HolderName);
            holder.AddComponent<LevelPreviewMarker>();   // GameBootstrap clears anything carrying it on Play
            var data = holder.AddComponent<LevelHolder>();
            data.kind = kind;
            data.keys = keys;
            data.parentOf = parents;
            data.spawn = spawn;
            data.orphans = new List<string>(res.orphans);
            data.applied = res.edited + res.added;
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go == holder || before.Contains(go)) continue;
                go.transform.SetParent(holder.transform, true);
                if (!beforePasses.Contains(go) && go.GetComponent<LevelDerived>() == null) go.AddComponent<LevelDerived>();
            }
            MarkDerived(holder.transform);
            SpawnMarker(holder.transform, res.spawnMoved ? res.spawn : spawn, config);
            NoSave(holder);
            if (holder.scene != scene) SceneManager.MoveGameObjectToScene(holder, scene);
            Undo.RegisterCreatedObjectUndo(holder, "Generate Level Visuals");   // the whole hierarchy: undo removes it in one step

            Dirty = File.Exists(LevelEdits.PendingPath(kind));
            if (Dirty) EditorSceneManager.MarkSceneDirty(scene);
            LastCounts = new Counts(res.edited, res.added, res.spawnMoved, 0);

            var view = SceneView.lastActiveSceneView;
            if (view != null && !quiet) view.LookAt((res.spawnMoved ? res.spawn : spawn) + Vector3.up * 2f, Quaternion.Euler(20f, -35f, 0f), 30f);
            Debug.Log("Momentum Level Editor: " + kind + " built (" + keys.Count + " pieces" + (data.applied > 0 ? ", " + data.applied + " of your edits laid on" : "") + (res.orphans.Count > 0 ? ", " + res.orphans.Count + " edit(s) found no piece: see the Level Editor window" : "") + "). Move, scale, delete or Ctrl+D its pieces; Ctrl+S saves the edits.");
            foreach (var o in res.orphans) Debug.LogWarning("Momentum Level Editor: no piece for " + o);
        }
        finally
        {
            working = false;
            Quiet();
            EditorUtility.ClearProgressBar();
            Changed?.Invoke();
        }
    }

    /// Takes the working copy down. With unsaved edits it asks first (save, throw away, or keep it up)
    public static void Clear(bool ask)
    {
        var h = Holder();
        if (h == null) return;
        if (ask && Dirty)
        {
            int choice = EditorUtility.DisplayDialogComplex("Clear the level", "The level has edits you have not saved.", "Save and clear", "Cancel", "Clear without saving");
            if (choice == 1) return;
            if (choice == 0) Save(true);
            else Discard(false);
        }
        DestroyHolder(h);
        Changed?.Invoke();
    }

    static void DestroyHolder(LevelHolder h)
    {
        working = true;
        try { Object.DestroyImmediate(h.gameObject); }
        finally { working = false; }
        EditorUtility.UnloadUnusedAssetsImmediate();   // the generated meshes, textures and materials go with it
    }

    /// Anything a pass hung on a piece (no key, not an addition) is the pass's: never saved as an edit
    static void MarkDerived(Transform t)
    {
        for (int i = 0; i < t.childCount; i++)
        {
            var c = t.GetChild(i);
            if (c.GetComponent<LevelDerived>() != null || c.GetComponent<LevelAdded>() != null) continue;
            if (c.GetComponent<LevelPiece>() == null) { c.gameObject.AddComponent<LevelDerived>(); continue; }
            MarkDerived(c);
        }
    }

    /// A gold capsule of the player's size on the spawn: move it and the level starts there
    static void SpawnMarker(Transform parent, Vector3 feet, MovementConfig config)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Player Spawn (move me)";
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = feet + Vector3.up * (config.capsuleHeight * 0.5f);
        go.transform.localScale = new Vector3(config.capsuleRadius * 2f, config.capsuleHeight * 0.5f, config.capsuleRadius * 2f);
        go.AddComponent<LevelSpawnMarker>();
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null)
        {
            var gold = new Color(1f, 0.8f, 0.2f);
            var mat = new Material(shader) { name = "Spawn Gold" };
            mat.SetColor("_BaseColor", gold);
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }

    /// Never written into the scene file
    public static void NoSave(GameObject root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.hideFlags = HideFlags.DontSaveInEditor;
            foreach (var c in t.GetComponents<Component>())
                if (c != null) c.hideFlags = HideFlags.DontSaveInEditor;
        }
    }

    // ------------------------------------------------------------ keeping it up: open, save, play, undo

    static void AutoForOpenScene()
    {
        if (Application.isBatchMode || BuildPipeline.isBuildingPlayer) return;   // a player build opens scenes too
        if (!AutoGenerate || EditorApplication.isPlayingOrWillChangePlaymode || Holder() != null) return;
        var b = FindBootstrap();
        if (b == null || b.level == LevelKind.MainMenu) return;
        Generate(b.level, b.config, true);
    }

    static void OnSceneSaving(Scene scene, string path)
    {
        var h = Holder();
        if (h == null || h.gameObject.scene != scene) return;
        NoSave(h.gameObject);   // anything made inside it since: still never saved into the scene
        if (Dirty) Save(true);
    }

    static void OnPlayMode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            var h = Holder();
            if (h == null) return;
            if (Dirty) WritePending(h.kind, Capture(h, null, null));   // Play uses the unsaved edits, as it uses an unsaved scene
            SessionState.SetInt(RegenKey, (int)h.kind + 1);
            DestroyHolder(h);
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            int k = SessionState.GetInt(RegenKey, 0);
            if (k <= 0) return;
            SessionState.SetInt(RegenKey, 0);
            EditorApplication.delayCall += () => { if (Holder() != null) return; var b = FindBootstrap(); Generate((LevelKind)(k - 1), b != null ? b.config : null, true); };
        }
    }

    static void OnUndoRedo()
    {
        if (working) return;
        if (EditorApplication.timeSinceStartup < quietUntil) return;
        var h = Holder();
        if (h != null) Touched(h);
    }

    static void OnChanges(ref ObjectChangeEventStream stream)
    {
        if (working) return;
        if (EditorApplication.timeSinceStartup < quietUntil) return;
        var h = Holder();
        if (h == null) return;
        bool touched = false;
        for (int i = 0; i < stream.length; i++)
        {
            switch (stream.GetEventType(i))
            {
                case ObjectChangeKind.ChangeGameObjectOrComponentProperties:
                    stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out var props);
                    touched |= Under(props.entityId, h);
                    break;
                case ObjectChangeKind.ChangeGameObjectStructure:
                    stream.GetChangeGameObjectStructureEvent(i, out var structure);
                    touched |= Under(structure.entityId, h);
                    break;
                case ObjectChangeKind.ChangeGameObjectStructureHierarchy:
                    stream.GetChangeGameObjectStructureHierarchyEvent(i, out var hierarchy);
                    touched |= Under(hierarchy.entityId, h);
                    break;
                case ObjectChangeKind.CreateGameObjectHierarchy:
                    stream.GetCreateGameObjectHierarchyEvent(i, out var created);
                    if (Under(created.entityId, h)) { touched = true; NoSaveId(created.entityId); }
                    break;
                case ObjectChangeKind.ChangeGameObjectParent:
                    stream.GetChangeGameObjectParentEvent(i, out var moved);
                    if (Under(moved.entityId, h)) { touched = true; NoSaveId(moved.entityId); }
                    else if (Under(moved.previousParentEntityId, h)) touched = true;
                    break;
                case ObjectChangeKind.DestroyGameObjectHierarchy:
                    stream.GetDestroyGameObjectHierarchyEvent(i, out var destroyed);
                    touched |= Under(destroyed.parentEntityId, h);
                    break;
            }
        }
        if (touched) Touched(h);
    }

    static void Touched(LevelHolder h)
    {
        if (!Dirty) Dirty = true;
        else Changed?.Invoke();
        EditorSceneManager.MarkSceneDirty(h.gameObject.scene);   // the * on the scene, so Ctrl+S (and closing) saves your edits
    }

    static bool Under(EntityId id, LevelHolder h)
    {
        var o = EditorUtility.EntityIdToObject(id);
        var go = o as GameObject;
        if (go == null && o is Component c) go = c.gameObject;
        return go != null && (go.transform == h.transform || go.transform.IsChildOf(h.transform));
    }

    static void NoSaveId(EntityId id)
    {
        if (EditorUtility.EntityIdToObject(id) is GameObject go) NoSave(go);
    }

    /// An object's identity as text: what LevelPiece.origin holds (a copy carries its original's)
    public static string Origin(GameObject go) => go.GetEntityId().ToString();
}
