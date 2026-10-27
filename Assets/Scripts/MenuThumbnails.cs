using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Level-select thumbnails: one POSTER per level (user 2026-10-16: "dedicated images for the thumbnails, look to
/// ULTRAKILL" — key art, not screenshots). On the menu each catalog level's poster (LevelCatalog.Poster → Posters.Build)
/// is built as a diorama of flat unlit shapes (PosterKit) on a hidden layer in front of an orthographic camera,
/// graded through the poster's own volume (PosterLook.Profile: contrast up, a strong vignette, a touch of bloom; no
/// halftone), photographed into a 960 × 540 RenderTexture with a single-camera render request, and torn down in the
/// same Update — one poster per frame after a short grace period, so the menu appears first; a card that asks for its
/// picture before it is built goes to the front of the line. Shots live for the session. Nothing of a poster reaches
/// the menu: its layer is one the menu camera neither draws nor is graded by, nothing in it is lit or heard, and it is
/// gone before anything else in the frame can see it. The levels themselves are never built here any more (the
/// ThumbnailStage path that shot them live was retired with this).
/// </summary>
public class MenuThumbnails : MonoBehaviour
{
    /// Kept for the menu's plumbing (MainMenuUI sets it); a poster reads nothing from it
    public MovementConfig config;

    /// An unnamed user layer the menu camera never draws (nor is graded by); the posters are shot on it
    public const int Layer = 31;
    const int Width = 960, Height = 540;   // the cards' 16:9 print, sharp on a 4K programme
    const int GraceFrames = 4;
    const float CameraZ = -10f;
    static readonly Vector3 StageAt = new Vector3(0f, -3000f, 0f);   // far under the menu's city: nothing of it is ever in the way

    static readonly Dictionary<string, RenderTexture> shots = new Dictionary<string, RenderTexture>();
    static readonly List<string> wanted = new List<string>();   // cards that asked before their poster was built: first in line

    /// The picture for a scene, or null while it is still queued (asking puts it at the front of the line)
    public static Texture Get(string scene)
    {
        if (shots.TryGetValue(scene, out var rt) && rt != null) return rt;
        if (!wanted.Contains(scene)) wanted.Add(scene);
        return null;
    }

    int next, frames;
    Transform stage;
    Camera shooter;

    void Start()
    {
        var main = Camera.main;
        if (main != null)
        {
            main.cullingMask &= ~(1 << Layer);
            var mainData = main.GetUniversalAdditionalCameraData();
            if (mainData != null) mainData.volumeLayerMask &= ~(1 << Layer);   // the poster grade is the shooter's alone
        }
        var sg = new GameObject("PosterStage") { layer = Layer };
        sg.transform.position = StageAt;
        stage = sg.transform;

        var go = new GameObject("PosterCamera") { layer = Layer };
        go.transform.SetParent(stage, false);
        go.transform.localPosition = new Vector3(0f, 0f, CameraZ);
        shooter = go.AddComponent<Camera>();
        shooter.enabled = false;   // only ever renders on request
        shooter.orthographic = true;
        shooter.orthographicSize = PosterKit.HalfH;   // 16 × 9 units at the cards' aspect
        shooter.cullingMask = 1 << Layer;
        shooter.clearFlags = CameraClearFlags.SolidColor;
        shooter.backgroundColor = Color.black;
        shooter.nearClipPlane = 0.5f;
        shooter.farClipPlane = 30f;
        shooter.allowHDR = true;
        shooter.allowMSAA = true;
        var data = shooter.GetUniversalAdditionalCameraData();
        data.renderType = CameraRenderType.Base;
        data.renderPostProcessing = true;
        data.volumeLayerMask = 1 << Layer;
        data.renderShadows = false;
        data.antialiasing = AntialiasingMode.None;   // the texture's own MSAA
        data.dithering = true;

        // The poster grade: a global volume on the hidden layer, so only the shooter is graded by it
        var vg = new GameObject("PosterGrade") { layer = Layer };
        vg.transform.SetParent(stage, false);
        var grade = vg.AddComponent<Volume>();
        grade.isGlobal = true;
        grade.priority = 100f;
        grade.weight = 1f;
        grade.sharedProfile = PosterLook.Profile();
    }

    void OnDestroy()
    {
        if (stage != null) Destroy(stage.gameObject);
        PosterLook.ReleaseShared();
    }

    void Update()
    {
        if (shooter == null) return;
        if (frames++ < GraceFrames) return;
        // A card that is waiting goes first, then the catalog in order; one poster per frame
        for (int i = wanted.Count - 1; i >= 0; i--)
        {
            string s = wanted[i];
            if (shots.TryGetValue(s, out var done) && done != null) { wanted.RemoveAt(i); continue; }
            if (!LevelCatalog.TryFind(s, out var w)) { wanted.RemoveAt(i); continue; }
            Shoot(w);
            return;
        }
        while (next < LevelCatalog.All.Length)
        {
            var info = LevelCatalog.All[next++];
            if (shots.TryGetValue(info.scene, out var rt) && rt != null) continue;
            Shoot(info);
            break;
        }
    }

    /// The poster built, shot and gone, all in this call. A composition that throws leaves a plain plate in the
    /// palette's ink so the card never waits forever
    void Shoot(LevelCatalog.LevelInfo info)
    {
        var poster = LevelCatalog.Poster(info.scene);
        var root = new GameObject("Poster_" + info.scene) { layer = Layer };
        root.transform.SetParent(stage, false);
        var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32)
        {
            name = "Poster_" + info.scene,
            antiAliasing = 4,
        };
        rt.Create();
        shots[info.scene] = rt;
        wanted.Remove(info.scene);
        PosterKit kit = null;
        bool fog = RenderSettings.fog;
        try
        {
            kit = new PosterKit(root.transform, Layer, Seed(info.scene), PosterLook.Sepia);
            Posters.Build(poster.composition, kit);
            PosterLook.Finish(kit);
            shooter.backgroundColor = kit.pal.ink;
            RenderSettings.fog = false;   // URP's Unlit mixes the scene's fog; a poster's haze is its own
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(shooter, request))
            {
                RenderPipeline.SubmitRenderRequest(shooter, request);
            }
            else
            {
                shooter.targetTexture = rt;
                shooter.Render();
                shooter.targetTexture = null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("MenuThumbnails: " + info.scene + "'s poster was not drawn: " + e.Message);
            var was = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, kit != null ? kit.pal.ink : Color.black);
            RenderTexture.active = was;
        }
        finally
        {
            RenderSettings.fog = fog;
            kit?.Release();
            DestroyImmediate(root);   // gone before anything else in the frame can see it
        }
    }

    /// A stable seed per scene (FNV-1a), so a poster is the same picture every launch
    static int Seed(string s)
    {
        unchecked
        {
            int h = (int)2166136261;
            foreach (char c in s) h = (h ^ c) * 16777619;
            return h;
        }
    }
}
