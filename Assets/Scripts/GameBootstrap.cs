using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

public enum LevelKind { PracticeRange = 0, Tutorial = 1, Level1 = 2, Level2 = 3, MainMenu = 4, Level3 = 5, Level4 = 6, Level5 = 7, Level6 = 8, Level7 = 9, Level8 = 10, Level9 = 11, Level10 = 12, Level11 = 13, Level12 = 14, Level13 = 15, BeatGym = 16 }

/// <summary>
/// Scene entry point: sets the fixed tick rate, builds this scene's level,
/// and spawns the player + camera rig + debug HUD.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    public MovementConfig config;
    [Tooltip("Which level this scene's bootstrap builds")]
    public LevelKind level;

    PlayerMotor motor;
    GrappleAbility grapple;
    PlayerHealth health;
    Vector3 spawnPosition;
    /// Where a respawn lands instead of the level start (the boss arena sets it); cleared every load
    public static Vector3? Checkpoint;
    /// The way a respawn at `Checkpoint` sends you (CourseFlow: the authored route's direction at that checkpoint, +z on a
    /// chain); the flow launches you along it at run speed once Respawn has stood you there
    public static Vector3 CheckpointForward = Vector3.forward;
    /// Fired after every respawn (falls, the R key, being downed): the boss resets on it
    public static System.Action Respawned;
    /// The fall line: below it a fall respawns; a level (ResetHeight in it) may move it, every build puts it back
    public static float KillFloorY = -35f;
    static GameBootstrap instance;

    /// The one respawn path, for things in the world that put you back (the melt in a sinkhole): same as a fall
    public static void RespawnPlayer()
    {
        if (instance != null && instance.motor != null) instance.Respawn();
    }

    /// Which builder makes which level — decided in ONE place. The editor
    /// preview (Momentum → Level Preview) calls this too, so the Scene view
    /// shows exactly what Play would build. Returns the player spawn point.
    public static Vector3 BuildLevel(LevelKind kind, MovementConfig config)
    {
        EnemyPool.Configure(kind); // which units the section builders field here (the tutorial names its own)
        CityVista.Suppressed = false;   // a brutalist course sets it again as it builds
        CityVista.Override = null;
        Vector3 spawn = kind switch
        {
        LevelKind.Tutorial => TutorialLevelBuilder.Build(config),
        LevelKind.Level1 => Level1Builder.Build(config),
        LevelKind.Level2 => Level2Builder.Build(config),
        LevelKind.Level3 => Level3Builder.Build(config),
        LevelKind.Level4 => Level4Builder.Build(config),
        LevelKind.Level5 => Level5Builder.Build(config),
        LevelKind.Level6 => Level6Builder.Build(config),
        LevelKind.Level7 => Level7Builder.Build(config),
        LevelKind.Level8 => Level8Builder.Build(config),
        LevelKind.Level9 => Level9Builder.Build(config),
        LevelKind.Level10 => Level10Builder.Build(config),
        LevelKind.Level11 => Level11Builder.Build(config),
        LevelKind.Level12 => Level12Builder.Build(config),
        LevelKind.Level13 => Level13Builder.Build(config),
        LevelKind.BeatGym => BeatGymBuilder.Build(config),   // THE BEAT GYM (Phase 3, 2026-10-27)
        _ => TestLevelBuilder.Build(config),
        };
        CityVista.Build(kind); // the first act looks out on the city
        return spawn;
    }

    /// The HARD LINE pass with this level's zone and layout (the game and the level editor draw it the same way)
    public static void HardLine(LevelKind level) =>
        LevelKit.Precision(level >= LevelKind.Level11 ? Zone.Surface : level >= LevelKind.Level6 ? Zone.Works : Zone.Utopia, level == LevelKind.Tutorial || level == LevelKind.PracticeRange || level >= LevelKind.Level11 ? null : LevelSections.Last);

    // The level editor from this component's context menu (the '⋮' on the inspector): see Momentum → Level Editor
    [ContextMenu("Generate Level Visuals")] void GenerateLevelVisuals() => LevelEditorHooks.Generate?.Invoke(this);
    [ContextMenu("Save Level Edits")] void SaveLevelEdits() => LevelEditorHooks.Save?.Invoke(this);
    [ContextMenu("Clear Level Visuals")] void ClearLevelVisuals() => LevelEditorHooks.Clear?.Invoke(this);

    void Awake()
    {
        instance = this;
        Checkpoint = null;
        CheckpointForward = Vector3.forward;
        Respawned = null;
        RunState.Load();       // the run, as the last save left it (a fresh process picks it back up)
        if (level != LevelKind.MainMenu) RunState.Arrive(LevelCatalog.IndexOf(SceneManager.GetActiveScene().name)); // the run's next level is played for the run; any other for itself
        Enemy.Janitors.Clear();
        if (config == null)
        {
            Debug.LogWarning("GameBootstrap: no MovementConfig assigned, using code defaults.");
            config = ScriptableObject.CreateInstance<MovementConfig>();
        }

        // An editor level preview left in the scene would duplicate the world
        foreach (var preview in FindObjectsByType<LevelPreviewMarker>(FindObjectsInactive.Include))
            DestroyImmediate(preview.gameObject);

        GameSettings.Apply();
        MusicDirector.Ensure(); // the score outlives every scene and reads the game's state itself
        Enemy.RespawnEnabled = level == LevelKind.PracticeRange || level == LevelKind.Tutorial; // levels: kills stick until the level resets
        Practice.Active = level == LevelKind.PracticeRange; // the range's console: nothing it does reaches a level
        if (!Practice.Active) Practice.Reset();
        Time.timeScale = Practice.TimeScale;

        gameObject.AddComponent<DevMenu>(); // the back door: a chord opens it, on the menu and in play
        if (level == LevelKind.MainMenu)
        {
            // The menu scene builds no gameplay — MainMenuUI owns everything
            Time.timeScale = 1f;
            gameObject.AddComponent<LookInspector>();   // the census (F6) and F7 on the menu's city too
            gameObject.AddComponent<PinkHunter>();   // THE PINK HUNTER (2026-10-27): in play, what draws magenta is put on a stand-in shader and named
            var menu = gameObject.AddComponent<MainMenuUI>();
            menu.config = config;
            return;
        }

        Time.fixedDeltaTime = 1f / config.physicsTickRate;
        Time.maximumDeltaTime = 0.1f; // a long frame (a level build, a GC spike) must never cash in as 30 catch-up physics steps
        var activeScene = SceneManager.GetActiveScene();
        var rootsBefore = new System.Collections.Generic.HashSet<GameObject>(activeScene.GetRootGameObjects());
        MaterialNet.Reset();
        KillFloorY = -35f;
        spawnPosition = BuildLevel(level, config);
        // The hand edits (Momentum → Level Editor): laid on the level as built, before the passes below see it
        var edits = LevelEdits.ApplyFor(level, LevelEdits.NewRoots(activeScene, rootsBefore), config);
        if (edits.spawnMoved)
        {
            spawnPosition = edits.spawn;
            if (LevelSections.Last != null && level != LevelKind.Tutorial && level != LevelKind.PracticeRange) LevelSections.Last.spawn = edits.spawn;
        }
        CoplanarFixer.Run();   // no two faces on one plane: the flicker is gone before the first frame
        Physics.SyncTransforms();
        HardLine(level);       // HARD LINE: a bead on every edge, a line on every floor
        LevelLint.Run();       // what the builders got wrong, in the console, with a position for each
        MenuStyle.LevelSkin = MenuStyle.SkinFor(LevelCatalog.ChapterOf(SceneManager.GetActiveScene().name)); // the pause and the report card print in this chapter's material
        // The light: sky, sun, ambient, fog and the grade, from the level's zones (the two hand-built courses are all one zone)
        WorldLook.Apply(level, level == LevelKind.Tutorial || level == LevelKind.PracticeRange ? null : LevelSections.Last);
        LevelKit.RenderProbes();   // the rooms' reflections, now that the light they show is set
        MaterialNet.Run();   // nothing pink: every renderer in the scene (the city and the sky included); a material on a shader Unity cannot use, or an empty slot, draws lit, and the console says which
        LookInspector.SpawnReport(spawnPosition);   // what stands under the spawn, in the console
        gameObject.AddComponent<LookInspector>();   // F7 in play: what the crosshair is on (object, material, shader), on screen and in the console
        gameObject.AddComponent<PinkHunter>();   // THE PINK HUNTER (2026-10-27): in play, what draws magenta is put on a stand-in shader and named

        // Player: kinematic capsule on Ignore Raycast so the motor's scene queries skip it
        var player = new GameObject("Player") { layer = 2 };
        player.transform.position = spawnPosition;
        var capsule = player.AddComponent<CapsuleCollider>();
        capsule.radius = config.capsuleRadius;
        capsule.height = config.capsuleHeight;
        capsule.center = Vector3.up * (config.capsuleHeight * 0.5f);
        motor = player.AddComponent<PlayerMotor>();
        motor.config = config;
        var stamina = player.AddComponent<StaminaSystem>();
        stamina.config = config;
        motor.stamina = stamina;

        // Order pins event-subscription order: StyleSystem must see MoveExecuted
        // BEFORE ResetDetector fires ResetFired, so reset points are scored with
        // the freshness multiplier that includes the triggering move
        var styleSystem = player.AddComponent<StyleSystem>();
        styleSystem.config = config;
        styleSystem.motor = motor;
        styleSystem.stamina = stamina;

        var resets = player.AddComponent<ResetDetector>();
        resets.config = config;
        resets.stamina = stamina;
        resets.motor = motor;

        grapple = player.AddComponent<GrappleAbility>();
        grapple.config = config;
        grapple.motor = motor;
        grapple.stamina = stamina;
        motor.grapple = grapple;

        // Reuse the scene's Main Camera (keeps URP camera data); create one if missing
        Camera cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Camera") { tag = "MainCamera" };
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }
        // The image: HDR, SMAA (crisp at speed, no ghosting), a dithered grade, the post stack on
        cam.allowHDR = true;
        var camData = cam.GetUniversalAdditionalCameraData();
        if (camData != null)
        {
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camData.antialiasingQuality = AntialiasingQuality.High;
            camData.dithering = true;
        }

        // The open districts of Chapter 3 look nine kilometres across the city at the house: the scene camera draws one
        if (level == LevelKind.Level11 || level == LevelKind.Level12 || level == LevelKind.Level13) cam.farClipPlane = SurfaceKit.FarClip;
        // ...and hold a hundred units, of which you can see a dozen: the far ones sleep, and the farthest are not drawn
        Enemy.FarCulling = level == LevelKind.Level11 || level == LevelKind.Level12 || level == LevelKind.Level13;

        var rig = new GameObject("CameraRig");
        var look = rig.AddComponent<PlayerLook>();
        look.config = config;
        look.motor = motor;
        look.cam = cam;
        motor.look = look;
        grapple.look = look;
        ViewmodelCamera.Create(config, cam);      // the hands' own lens: the Viewmodel layer (30) at a fixed FOV, stacked over the world; from here the base camera does not draw that layer

        // THE ONE KIT: the knuckles (the jab and the kick) on the player, the arm rig and the gloves on the camera.
        // Nothing is chosen, nothing is armed by class: every run carries this and the whip
        var fists = player.AddComponent<Fists>();
        fists.config = config;
        fists.motor = motor;
        fists.look = look;
        FistRig.Add(cam, motor, config);                // the knuckles on the camera: they build the jointed arms (ArmRig) in their Start and drive them

        // THE BODY CAMERA and THE TRICKS (2026-10-23): the body laid over the look (PlayerLook ticks it), and the QTE flips
        var bodyCam = player.AddComponent<CameraBody>();
        bodyCam.config = config;
        bodyCam.motor = motor;
        bodyCam.look = look;
        look.body = bodyCam;
        var tricks = player.AddComponent<TrickSystem>();
        tricks.config = config;
        tricks.motor = motor;
        tricks.look = look;
        tricks.stamina = stamina;
        tricks.style = styleSystem;
        tricks.body = bodyCam;

        gameObject.AddComponent<RunRecorder>();   // THE RUN RECORDER (Phase 2, 2026-10-27): every frame to Docs/Runs/<level>-<date>.csv, F9/F10/F11 the markers
        var hud = gameObject.AddComponent<DebugHUD>();
        hud.motor = motor;
        hud.stamina = stamina;
        hud.style = styleSystem;
        hud.fists = fists;
        hud.grapple = grapple;
        UIScale.Multiplier = config.uiScale;

        var feel = gameObject.AddComponent<GameFeel>();
        feel.config = config;
        feel.motor = motor;
        feel.look = look;
        feel.style = styleSystem;
        feel.grapple = grapple;

        var combat = gameObject.AddComponent<CombatDirector>();
        combat.config = config;
        combat.motor = motor;
        combat.style = styleSystem;
        combat.resets = resets;
        combat.grapple = grapple;
        var baseline = gameObject.AddComponent<CombatBaseline>();   // the numbers behind "combat doesn't feel right"
        baseline.motor = motor;
        baseline.director = combat;

        health = player.AddComponent<PlayerHealth>();
        health.config = config;
        health.OnDowned = Respawn; // zero health = back to the start, timer running
        if (level == LevelKind.Tutorial) { health.Deathless = true; health.RegenPerSecond = 14f; health.RegenDelay = 2f; } // the course cannot kill you, and it heals you
        combat.health = health;
        hud.health = health;
        if (Practice.Active)
        {
            var console = player.AddComponent<PracticeConsole>();
            console.config = config;
            console.motor = motor;
            console.look = look;
            console.health = health;
            Practice.Apply();
        }

        TutorialGate.Motor = motor;
        TutorialGate.Grapple = grapple;

        // Scene doors — walk through the glowing pane to travel
        switch (level)
        {
            case LevelKind.Tutorial:
                ScenePortal.Create(TutorialLevelBuilder.MenuPortal, new Vector3(3.2f, 4f, 1.2f), "EXIT TUTORIAL", "PracticeRange", motor);
                // Past graduation: back to the range, or on into the first chapter
                ScenePortal.Create(TutorialLevelBuilder.RangePortal, new Vector3(3.2f, 4f, 1.2f), "THE RANGE", "PracticeRange", motor);
                ScenePortal.Create(TutorialLevelBuilder.ChapterPortal, new Vector3(3.2f, 4f, 1.2f), "CHAPTER ONE", "Level1", motor);
                break;
            default:
            {
                // A campaign level places its doors itself: the finish names the scene after it in the programme's
                // order (the last names the menu); the bill decides where you actually go — the run's next level, or
                // the Programme page in free play. The range gets the tutorial's door.
                string here = SceneManager.GetActiveScene().name;
                if (LevelCatalog.IsCampaignScene(here)) LayoutPortals(LevelCatalog.NextScene(here) ?? "MainMenu");
                else ScenePortal.Create(new Vector3(0f, 2f, -66f), new Vector3(3.2f, 4f, 1.2f), "TUTORIAL", "Tutorial", motor);
                break;
            }
        }

        if (LevelCatalog.IsCampaignScene(SceneManager.GetActiveScene().name))
        {
            var timer = gameObject.AddComponent<LevelTimer>();
            hud.timer = timer;
            // The report card at the finish: style held, time vs gold/par, kills and damage
            var results = gameObject.AddComponent<LevelResults>();
            results.style = styleSystem;
            results.timer = timer;
            results.combat = combat;
            results.health = health;
        }

        if (level != LevelKind.Tutorial && level != LevelKind.PracticeRange)
        {
            // The course: checkpoints are lines you cross, a respawn stands you at the last one moving; segments for the
            // baseline and the HUD. Nothing gates (CourseFlow; the room loop is gone)
            var flow = gameObject.AddComponent<CourseFlow>();
            flow.config = config;
            flow.motor = motor;
            flow.director = combat;
            flow.Init(LevelSections.Last);
        }

        var pause = gameObject.AddComponent<PauseMenu>(); // Esc pauses, standard videogame stuff
        pause.config = config;                                // its SETTINGS page needs the field of view
        LevelWeld.Run();   // every static primitive welded per material per 40 m cell: the picture unchanged, the draws a hundredth (LevelWeld)
        DevJump.Apply(motor);                                 // the back door, if it was used: straight to the scene
    }

    /// Campaign levels place their own doors: the MENU door behind the spawn, the FINISH door at the end
    void LayoutPortals(string next)
    {
        var l = LevelSections.Last;
        if (l == null) return;
        ScenePortal.Create(l.menuPortal, new Vector3(3.2f, 4f, 1.2f), "MENU", "MainMenu", motor);
        ScenePortal.Create(l.finishPortal, new Vector3(3.2f, 4f, 1.2f), "FINISH", next, motor, isFinish: true);
    }

    // One respawn path for falls, the R key, and being downed: this stands you at the checkpoint; the flow (Respawned)
    // launches you along CheckpointForward at run speed, so you leave it moving the way the route goes
    void Respawn()
    {
        grapple.Cancel();
        motor.Teleport(Checkpoint ?? spawnPosition);
        if (health != null) health.Restore();
        if (GameFeel.Instance != null) GameFeel.Instance.PlayRespawn();
        Respawned?.Invoke();
    }

    void Update()
    {
        if (motor == null || PauseMenu.IsPaused || BossIntro.Active || BossOutro.Active || OpeningCutscene.Active || MineBlast.Active) return; // menu scenes have no player; the cutscenes own the keys
        var kb = Keyboard.current;
        if (Keybinds.Down(GameAction.Respawn))
        {
            grapple.Cancel(); // or the stale tether drags the player from spawn
            Respawn();
        }

        // Kill floor: falling out of a level respawns (the timer keeps running)
        if (motor != null && motor.CurrPosition.y < KillFloorY)
        {
            grapple.Cancel();
            Respawn();
        }
    }
}
