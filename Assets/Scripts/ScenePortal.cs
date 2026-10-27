using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// A physical doorway between scenes: frame + glowing pane + label. Walking
/// through the pane loads the target scene — no keybind. Overlap is checked
/// manually against the motor, same as tutorial gates.
/// </summary>
public class ScenePortal : MonoBehaviour
{
    public PlayerMotor motor;
    public string targetScene;
    public Vector3 size;
    public bool isFinish; // finish doors save the level's best time

    bool loading;

    void FixedUpdate()
    {
        if (loading || motor == null || LevelResults.Active) return;
        Vector3 chest = motor.CurrPosition + Vector3.up * 0.9f;
        if (!new Bounds(transform.position, size).Contains(chest)) return;
        loading = true;
        if (isFinish && LevelTimer.Instance != null) LevelTimer.Instance.SaveBest();
        if (GameFeel.Instance != null)
        {
            if (isFinish) GameFeel.Instance.PlayFinish(); else GameFeel.Instance.PlayPortal();
        }
        if (isFinish && LevelResults.Instance != null)
        {
            // The report card takes over; it loads the next scene when dismissed
            LevelResults.Instance.Finish(targetScene);
            return;
        }
        string scene = targetScene;
        if (!isFinish && LevelCatalog.IsCampaignScene(scene))
        {
            // A door into the campaign (the tutorial's CHAPTER ONE): a run in progress goes on where it stands; otherwise
            // a new run starts at Level 1, bare — the saved run is never silently thrown away by a door
            RunState.FreePlay = false;
            if (RunState.InProgress) scene = RunState.NextScene;
            else RunState.NewRun();
        }
        Time.timeScale = 1f; // never carry a parry freeze across a scene load
        SceneManager.LoadScene(scene);
    }

    public static void Create(Vector3 center, Vector3 size, string label, string targetScene,
        PlayerMotor motor, bool isFinish = false)
    {
        var root = new GameObject($"Portal_{label}");
        root.transform.position = center;
        var portal = root.AddComponent<ScenePortal>();
        portal.motor = motor;
        portal.targetScene = targetScene;
        portal.size = size;
        portal.isFinish = isFinish;

        var frameMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        frameMat.SetColor("_BaseColor", new Color(0.25f, 0.27f, 0.3f));
        var paneMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        paneMat.SetColor("_BaseColor", new Color(1f, 0.82f, 0.35f));

        bool wideOnX = size.x > size.z; // pane's wide axis
        Vector3 postScale = wideOnX ? new Vector3(0.3f, size.y, 0.5f) : new Vector3(0.5f, size.y, 0.3f);
        Vector3 postOffset = wideOnX
            ? new Vector3(size.x * 0.5f + 0.15f, 0f, 0f)
            : new Vector3(0f, 0f, size.z * 0.5f + 0.15f);
        Piece(root.transform, center + postOffset, postScale, frameMat);
        Piece(root.transform, center - postOffset, postScale, frameMat);
        Piece(root.transform, center + Vector3.up * (size.y * 0.5f + 0.2f),
            wideOnX ? new Vector3(size.x + 0.9f, 0.4f, 0.5f) : new Vector3(0.5f, 0.4f, size.z + 0.9f),
            frameMat);
        Piece(root.transform, center,
            wideOnX ? new Vector3(size.x, size.y, 0.1f) : new Vector3(0.1f, size.y, size.z),
            paneMat);

        var textGo = new GameObject("PortalLabel");
        textGo.transform.SetParent(root.transform);
        textGo.transform.position = center + Vector3.up * (size.y * 0.5f + 0.9f);
        var tm = textGo.AddComponent<TextMesh>();
        Font font = GameFont.Title;
        tm.text = label;
        tm.font = font;
        tm.fontSize = 64;
        tm.characterSize = 0.12f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(1f, 0.85f, 0.4f);
        textGo.GetComponent<MeshRenderer>().sharedMaterial = GameFont.WorldMaterial(font, 40f, 100f); // depth-tested: not through walls
    }

    static void Piece(Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(go.GetComponent<Collider>()); // walk-through
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }
}
