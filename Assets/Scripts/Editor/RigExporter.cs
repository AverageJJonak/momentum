using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Momentum → Animation → Export Rig Proxies: builds the first-person arm rig and one enemy of every kind with the
/// game's own factories in a temporary additive scene (edit mode, nothing left behind), walks each hierarchy and
/// writes Tools/Animation/rig_arms.json and rig_&lt;kind&gt;.json — every pivot (name, parent, local pose) and
/// every primitive hanging off it (type, local pose, size, colour) — so Blender rebuilds the exact proportions
/// (Tools/Blender/momentum_rig_import.py). JSON only: the FBX Exporter package is not in the project.
/// </summary>
public static class RigExporter
{
    const string OutDir = "Tools/Animation";
    const string ConfigPath = "Assets/Config/MovementConfig.asset";

    // (file suffix = the clip naming's enemy kind, what the player reads, how the game makes one)
    static readonly (string file, string display, Func<MovementConfig, Enemy> make)[] Kinds =
    {
        ("dummy", "footman", c => Enemy.CreateDummy(Vector3.zero, c)),
        ("turret", "page", c => Enemy.CreateTurret(Vector3.up * 1.5f, c)),
        ("chaser", "bellhop", c => Enemy.CreateChaser(Vector3.zero, c)),
        ("janitor", "sanitation", c => Enemy.CreateJanitor(Vector3.zero, c)),
        ("borer", "platelayer", c => Enemy.CreateBorer(Vector3.zero, c)),
        ("rigger", "signalman", c => Enemy.CreateRigger(Vector3.zero, c)),
        ("blaster", "ganger", c => Enemy.CreateBlaster(Vector3.zero, c)),
        ("pursuit", "patrolman", c => Enemy.CreatePursuit(Vector3.zero, c)),
        ("hound", "hound", c => Enemy.CreateHound(Vector3.zero, c)),
        ("interceptor", "courier", c => Enemy.CreateInterceptor(Vector3.up * 1.5f, c)),
        ("kettler", "stoker", c => Enemy.CreateKettler(Vector3.zero, c)),
        ("damagecontrol", "housekeeping", c => Enemy.CreateDamageControl(Vector3.zero, c)),
        ("sentry", "doorman", c => Enemy.CreateSentry(Vector3.zero, c)),
        ("skitter", "busboy", c => Enemy.CreateSkitter(Vector3.zero, c)),
        ("marshal", "house officer", c => Enemy.CreateMarshal(Vector3.zero, c)),
        ("warden", "bouncer", c => Enemy.CreateWarden(Vector3.zero, c)),
        ("lurker", "valet", c => Enemy.CreateLurker(Vector3.zero, c)),
        ("moth", "moth", c => Enemy.CreateMoth(Vector3.up * 1.5f, c)),
        ("ocular", "usher", c => Enemy.CreateOcular(Vector3.up * 1.5f, c)),
        ("overseer", "floor manager", c => Enemy.CreateOverseer(Vector3.up * 2f, c)),
        ("scourer", "sweeper", c => Enemy.CreateScourer(Vector3.zero, c)),
        ("boss_concierge", "the concierge (reference only: bosses are not clip-driven)", c => Enemy.CreateBoss(Vector3.zero, c, 1f, Enemy.BossKind.Concierge)),
        ("boss_stationmaster", "the stationmaster (reference only: bosses are not clip-driven)", c => Enemy.CreateBoss(Vector3.zero, c, 1f, Enemy.BossKind.Foreman)),
    };

    [MenuItem("Momentum/Animation/Export Rig Proxies", false, 200)]
    public static void Export()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("RigExporter: run it in edit mode (it builds the rigs itself).");
            return;
        }
        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutDir));
        Directory.CreateDirectory(dir);
        var config = AssetDatabase.LoadAssetAtPath<MovementConfig>(ConfigPath);
        if (config == null) config = ScriptableObject.CreateInstance<MovementConfig>();

        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);   // everything the factories make lands here, and leaves with it
        var written = new List<string>();
        var failed = new List<string>();
        try
        {
            try { ExportArms(config, dir, written); }
            catch (Exception e) { failed.Add("arms: " + e.Message); Debug.LogException(e); }
            foreach (var k in Kinds)
            {
                try { ExportEnemy(k.file, k.display, k.make, config, dir, written); }
                catch (Exception e) { failed.Add(k.file + ": " + e.Message); Debug.LogException(e); }
            }
        }
        finally
        {
            foreach (var go in scene.GetRootGameObjects()) UnityEngine.Object.DestroyImmediate(go);
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
        Debug.Log("RigExporter: wrote " + written.Count + " rig file(s) to " + dir + "\n  " + string.Join("\n  ", written)
            + (failed.Count > 0 ? "\nFAILED: " + string.Join("; ", failed) : ""));
    }

    static void ExportArms(MovementConfig config, string dir, List<string> written)
    {
        var camGo = new GameObject("RigExportCamera");
        try
        {
            var rig = ArmRig.Ensure(camGo, null, config);
            var j = RigExportWalk.Capture(rig.Root, "arms", "the first-person arms: the root is the camera at its origin");
            RigExportWalk.PoseArmsAtRest(j, config);
            j.notes.Add("The root is the CAMERA (58° vertical FOV in the game): no translation keys on it, ever.");
            j.notes.Add("Hand frame: origin = the wrist, +Z along the fingers, +Y the back of the hand, thumb on the inner side.");
            j.notes.Add("Animate HandR/HandL (and the finger pivots F00..F32, T0/T1). ShoulderR/L and ElbowR/L are solved by the game's IK: here they show a rest solve, the export ignores them.");
            j.notes.Add("Fist = the knuckle bar socket (knuckles class only), Grip = where a handle sits: sockets, never moved.");
            j.notes.Add("Rest poses (camera space, metres / degrees): idle R (0.21, -0.20, 0.38) rot (-20, -12, -40), L mirrored (-0.21, -0.20, 0.38) rot (-20, 12, 40).");
            j.notes.Add("Knuckles stance: R lead (0.13, -0.14, 0.42) rot (-20, -12, -45); L rear (-0.20, -0.17, 0.34) rot (-24, 12, 55).");
            j.notes.Add("Reach: shoulders at (+-" + config.viewmodelShoulder.x + ", " + config.viewmodelShoulder.y + ", " + config.viewmodelShoulder.z + "), arm " + config.viewmodelUpperArm + " + " + config.viewmodelForearm + " m; keep wrists within 0.6 m of the camera at rest.");
            Write(j, Path.Combine(dir, "rig_arms.json"), written);
        }
        finally { UnityEngine.Object.DestroyImmediate(camGo); }
    }

    static void ExportEnemy(string file, string display, Func<MovementConfig, Enemy> make, MovementConfig config, string dir, List<string> written)
    {
        Enemy e = null;
        try
        {
            e = make(config);
            if (e == null) throw new Exception("the factory returned nothing");
            var j = RigExportWalk.Capture(e.transform, file, display);
            j.notes.Add("Root at the feet, on the ground, facing +Z: the game moves it, never key it.");
            j.notes.Add("Animate Torso, HeadPivot, ArmL, ArmR, LegL, LegR and this kind's sub-pivots. A position key on Torso overrides the gait's bob.");
            j.notes.Add("Clip names: " + file + "_<action>[_n], e.g. " + file + "_idle (loop), " + file + "_attack_1.");
            Write(j, Path.Combine(dir, "rig_" + file + ".json"), written);
        }
        finally { if (e != null) UnityEngine.Object.DestroyImmediate(e.gameObject); }
    }

    static void Write(RigJson j, string path, List<string> written)
    {
        File.WriteAllText(path, JsonUtility.ToJson(j, true));
        written.Add(Path.GetFileName(path) + " (" + j.bones.Count + " bones, " + j.proxies.Count + " proxies)");
    }
}
