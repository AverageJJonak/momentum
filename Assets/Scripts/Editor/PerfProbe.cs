using System.Diagnostics;
using UnityEditor;
using UnityEngine;

/// <summary>
/// What leaving play mode has to tear down, in numbers: at ExitingPlayMode the
/// probe logs the counts of everything alive (objects, renderers, colliders,
/// rigidbodies, joints, lights, text, audio sources) and of every runtime
/// asset (materials, textures, meshes, clips), then times the wall clock until
/// the editor is back. Momentum → Perf Probe → Count Now does the counts on
/// demand while playing. Read the Console after a session.
/// </summary>
[InitializeOnLoad]
public static class PerfProbe
{
    static Stopwatch exit;

    static PerfProbe()
    {
        EditorApplication.playModeStateChanged += OnChange;
    }

    static void OnChange(PlayModeStateChange s)
    {
        if (s == PlayModeStateChange.ExitingPlayMode)
        {
            Count("EXITING PLAY MODE");
            exit = Stopwatch.StartNew();
        }
        else if (s == PlayModeStateChange.EnteredEditMode && exit != null)
        {
            UnityEngine.Debug.Log($"PerfProbe: leaving play mode took {exit.Elapsed.TotalSeconds:0.0} s");
            exit = null;
        }
    }

    [MenuItem("Momentum/Perf Probe/Count Now")]
    static void CountNow() => Count("NOW");

    static void Count(string when)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("PerfProbe ").Append(when).Append(": scene — ");
        sb.Append("GameObjects ").Append(Scene<Transform>()).Append(", ");
        sb.Append("Renderers ").Append(Scene<Renderer>()).Append(", ");
        sb.Append("Colliders ").Append(Scene<Collider>()).Append(", ");
        sb.Append("Rigidbodies ").Append(Scene<Rigidbody>()).Append(", ");
        sb.Append("Joints ").Append(Scene<Joint>()).Append(", ");
        sb.Append("Lights ").Append(Scene<Light>()).Append(", ");
        sb.Append("TextMeshes ").Append(Scene<TextMesh>()).Append(", ");
        sb.Append("AudioSources ").Append(Scene<AudioSource>()).Append("; assets — ");
        sb.Append("Materials ").Append(All<Material>()).Append(", ");
        sb.Append("Textures ").Append(All<Texture2D>()).Append(", ");
        sb.Append("Meshes ").Append(All<Mesh>()).Append(", ");
        sb.Append("AudioClips ").Append(All<AudioClip>()).Append(", ");
        sb.Append("RenderTextures ").Append(All<RenderTexture>());
        UnityEngine.Debug.Log(sb.ToString());
    }

    static int Scene<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
    static int All<T>() where T : Object => Resources.FindObjectsOfTypeAll<T>().Length;
}
