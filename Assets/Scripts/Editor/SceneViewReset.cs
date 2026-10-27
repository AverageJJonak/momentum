using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// SCENE VIEW RESET (2026-10-23, user: "this is the preview screen right now, no change has happened"). The game's own
/// camera drew no magenta (the census read 0.0%) while the Scene view showed the city pink: the Scene view and the
/// Rendering Debugger draw the pipeline's STOCK shaders (Lit, Unlit, Particles) through their DEBUG DISPLAY variant,
/// which the game never uses and the game's own shaders (Toon, Sky, Sheet, WorldText) do not have, and on this
/// machine that variant comes out as the error shader. So: once per editor session, and from the menu, every Scene
/// view is put back to plain Shaded with scene lighting on, the Rendering Debugger's settings are reset, and its
/// runtime overlay (Ctrl+Backspace in Play) is closed. The console says what was changed.
/// </summary>
[InitializeOnLoad]
public static class SceneViewReset
{
    const string DoneKey = "Momentum.SceneViewReset.Done";

    static SceneViewReset()
    {
        if (SessionState.GetBool(DoneKey, false)) return;
        SessionState.SetBool(DoneKey, true);
        EditorApplication.delayCall += () => Reset(false);
    }

    [MenuItem("Momentum/Look/Reset Scene View And Debugger", false, 52)]
    static void ResetFromMenu() => Reset(true);

    static void Reset(bool always)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var o in SceneView.sceneViews)
        {
            if (!(o is SceneView sv)) continue;
            bool changed = false;
            if (!sv.sceneLighting) { sv.sceneLighting = true; changed = true; sb.Append(" scene lighting back on;"); }
            if (sv.cameraMode.drawMode != DrawCameraMode.Textured)
            {
                sb.Append(" draw mode was " + sv.cameraMode.name + ", now Shaded;");
                sv.cameraMode = SceneView.GetBuiltinCameraMode(DrawCameraMode.Textured);
                changed = true;
            }
            if (changed) sv.Repaint();
        }
        var debug = UniversalRenderPipelineDebugDisplaySettings.Instance;
        if (debug != null && debug.AreAnySettingsActive)
        {
            debug.Reset();
            sb.Append(" the Rendering Debugger's settings were active and are reset;");
        }
        var manager = DebugManager.instance;
        if (manager != null && (manager.displayRuntimeUI || manager.displayPersistentRuntimeUI))
        {
            manager.displayRuntimeUI = false;
            manager.displayPersistentRuntimeUI = false;
            sb.Append(" the debugger's in-game overlay is closed;");
        }
        if (sb.Length > 0) Debug.Log("SceneViewReset:" + sb.ToString().TrimEnd(';') + ". The pipeline's stock shaders draw their debug variant in those modes, and on this machine that variant is the error shader (magenta).");
        else if (always) Debug.Log("SceneViewReset: nothing to reset — every Scene view is Shaded with lighting on and the Rendering Debugger is idle. If the Scene view is still magenta, send its Shading dropdown and the Rendering Debugger window.");
    }
}
