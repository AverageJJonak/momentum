using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// THE VIEWMODEL CAMERA (2026-10-08). WHY: the arms and whatever they hold are children of the main camera and were
/// drawn by it, so every change to its field of view (the settings' FOV override, the speed stretch, the dash, parry
/// and blast kicks) stretched the hands and slid them around the frame: "the arms dont adjust to fov".
/// THE FIX: a second camera, a URP OVERLAY stacked on the base and parented to it at identity, that draws ONLY the
/// Viewmodel layer (30, Viewmodel.Layer) at its own field of view, over a cleared depth (the hands never cut into a
/// wall), with the post stack applied once to the whole picture by the base. The base camera no longer draws the
/// layer at all, so nothing is drawn twice.
/// THE FOV RULE: viewmodelFov (58) + viewmodelFovKickFraction (0.3) x (the base's field of view right now - the
/// settings' base FOV): the settings cannot resize the hands, but a dash or a shot still punches them a little, in
/// step with the world (a cutscene's zoom pulls them the same way). Set every LateUpdate after PlayerLook and the
/// cutscenes have set the base's (execution order 100).
/// Built by GameBootstrap right after PlayerLook, before the rig, so the base never draws a frame of the layer; the
/// rig builder puts the arms, weapons and trails on the layer, GameFeel the muzzle smoke and the flash at the mouth.
/// Untagged (Camera.main must keep returning the base), no AudioListener (the base has the ears). A disabled or
/// inactive base simply does not render its stack; this needs nothing for that.
/// </summary>
[DefaultExecutionOrder(100)]
public class ViewmodelCamera : MonoBehaviour
{
    public static ViewmodelCamera Instance { get; private set; }

    /// The overlay camera itself: screen-space maths for anything in the hands' picture goes through this, not Camera.main
    public Camera Cam { get; private set; }

    const float DefaultFov = 58f, DefaultKickFraction = 0.3f;
    const float Near = 0.02f, Far = 12f;   // the hands are within arm's reach, the cane's chain and the whip's start reach ~8 m, the smoke a few metres past

    MovementConfig config;
    Camera main;

    /// Make the overlay under `mainCamera` and stack it on it. Never from Awake: the config comes in here
    public static ViewmodelCamera Create(MovementConfig config, Camera mainCamera)
    {
        if (mainCamera == null) return null;
        var go = new GameObject("ViewmodelCamera");
        go.transform.SetParent(mainCamera.transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        var vm = go.AddComponent<ViewmodelCamera>();
        vm.Init(config, mainCamera);
        return vm;
    }

    public void Init(MovementConfig config, Camera mainCamera)
    {
        this.config = config;
        main = mainCamera;
        Instance = this;

        Cam = GetComponent<Camera>();
        if (Cam == null) Cam = gameObject.AddComponent<Camera>();
        Cam.cullingMask = Viewmodel.Mask;
        Cam.nearClipPlane = Near;
        Cam.farClipPlane = Far;
        Cam.clearFlags = CameraClearFlags.Depth;   // URP reads clearDepth below; this is what the inspector shows
        Cam.allowHDR = main.allowHDR;
        Cam.allowMSAA = main.allowMSAA;
        Cam.useOcclusionCulling = false;           // nothing in the hands' picture is ever behind anything
        Cam.depth = main.depth + 1f;
        Cam.fieldOfView = Fov(0f);

        var data = Cam.GetUniversalAdditionalCameraData();   // adds the URP data to a fresh camera
        data.renderType = CameraRenderType.Overlay;
        // clearDepth is read-only at runtime in URP 17 and its serialized default is TRUE (m_ClearDepth = true): a
        // camera made here clears the depth before it draws, which is what the hands need (never cut by a wall).
        // Should URP ever default it to false, the hands would start sinking into walls: that is the sign
        if (!data.clearDepth) Debug.LogWarning("ViewmodelCamera: URP made the overlay with clearDepth off; the hands will be cut by the world.");
        data.renderPostProcessing = false;         // the base's post applies to the stacked picture
        data.renderShadows = true;                 // the hands shadow themselves (Viewmodel casters only, six metres deep: cheap)
        data.requiresDepthOption = CameraOverrideOption.Off;
        data.requiresColorOption = CameraOverrideOption.Off;
        data.antialiasing = AntialiasingMode.None; // post AA is the base's
        data.dithering = false;

        var baseData = main.GetUniversalAdditionalCameraData();
        if (baseData.renderType != CameraRenderType.Base) baseData.renderType = CameraRenderType.Base;
        var stack = baseData.cameraStack;   // null when the base's renderer cannot stack (the 2D renderer): then the layer is not drawn at all
        if (stack == null) Debug.LogWarning("ViewmodelCamera: the base camera's renderer does not support camera stacking; the Viewmodel layer will not draw.");
        else if (!stack.Contains(Cam)) stack.Add(Cam);

        // The world's camera must not draw the hands any more (they would be drawn twice, once at the wrong FOV)
        main.cullingMask &= ~Viewmodel.Mask;
    }

    /// The rule: the hands' own FOV, plus a fraction of whatever is laid over the settings' FOV right now
    /// (the kicks, the speed stretch, a cutscene's zoom)
    float Fov(float kick)
    {
        float vm = config != null ? config.viewmodelFov : DefaultFov;
        float frac = config != null ? config.viewmodelFovKickFraction : DefaultKickFraction;
        return Mathf.Clamp(vm + frac * kick, 20f, 120f);
    }

    void LateUpdate()
    {
        if (Cam == null || main == null) return;
        Cam.fieldOfView = Fov(main.fieldOfView - GameSettings.Fov(config));
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (main == null || Cam == null) return;   // the base went first (a scene unload): nothing left to unhook
        var baseData = main.GetComponent<UniversalAdditionalCameraData>();
        if (baseData == null || baseData.renderType != CameraRenderType.Base) return;
        var stack = baseData.cameraStack;
        if (stack != null) stack.Remove(Cam);
    }
}
