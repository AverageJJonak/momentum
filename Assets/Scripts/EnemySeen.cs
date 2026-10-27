using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE LAST FEW, MARKED. Hunting the final enemy in a big room is the one part
/// of a room that is not a fight — it is a search, back down corridors you have
/// already cleared, looking for whatever is standing behind a crate. So once a
/// room is nearly clear the units still up are SEEN.
///
/// The mark is the unit's OWN SHAPE: every part of its rig gets a twin that
/// rides the part it copies, drawn with Momentum/Seen — a glowing amber figure
/// wherever a wall hides it, a bright edge where you can already see it — so
/// what shows through the wall is the unit itself, mid-stride, arm raised,
/// wherever it is. Over its head a diamond that turns and holds its size on
/// screen at any distance; at its feet a ring. Amber because in this game's
/// colour language amber is a unit's own light: cyan cuts, gold is the beat you
/// answer, red is the thing that hurts, and this is a unit giving itself away.
///
/// Decoration only: layer 2, no colliders, so no cast, cut or shot can ever hit
/// it, and the ragdoll never copies it (it copies the rig's own parts, cached at
/// birth). Nothing marks units on a course (they are fuel, never a hunt); the mark stays for whoever asks (SetSeen).
/// </summary>
public partial class Enemy
{
    static readonly Renderer[] NoRenderers = new Renderer[0];
    static Material seenMat;
    static Camera seenCam;
    Transform seenRoot, seenMarker;
    Renderer[] seenSrc = NoRenderers, seenCopy = NoRenderers;
    bool seen;

    public bool Seen => seen;

    /// Marked, or not. Cheap to call every frame: it only does work when the answer changes.
    public void SetSeen(bool on)
    {
        on = on && Alive;
        if (on == seen) return;
        seen = on;
        if (on && seenRoot == null) BuildSeen();
        if (seenRoot == null) return;
        seenRoot.gameObject.SetActive(on);
        for (int i = 0; i < seenCopy.Length; i++)
            if (seenCopy[i] != null) seenCopy[i].enabled = on && seenSrc[i] != null && seenSrc[i].enabled;
    }

    void BuildSeen()
    {
        if (seenMat == null)
        {
            var sh = Shader.Find("Momentum/Seen");
            if (sh == null) return;                       // no shader, no mark: the fight is unaffected
            seenMat = new Material(sh);
            seenMat.SetColor("_Color", new Color(2.2f, 1.3f, 0.35f, 0.6f));
        }

        // The silhouette: a twin of every part, a child of the part, so it moves with it
        var src = new List<Renderer>();
        var copy = new List<Renderer>();
        foreach (var r in GetComponentsInChildren<MeshRenderer>(true))
        {
            if (r == null || r.gameObject.layer == 2) continue;   // lasers, dots and marks are already on 2
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;
            if (r.GetComponent<TextMesh>() != null) continue;
            string n = r.name;
            if (n == "WeakspotRing" || n == "ThrusterHalo" || n == "Underglow" || n == "Lane" || n == "Link" || n == "SeenMark") continue;
            var go = new GameObject("SeenMark") { layer = 2 };
            go.transform.SetParent(r.transform, false);           // identity: the same shape in the same place
            go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = seenMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            src.Add(r);
            copy.Add(mr);
        }
        seenSrc = src.ToArray();
        seenCopy = copy.ToArray();

        // The diamond over the head and the ring at the feet
        var root = new GameObject("Seen") { layer = 2 };
        root.transform.SetParent(transform, false);
        seenRoot = root.transform;
        var mark = new GameObject("SeenMarker") { layer = 2 };
        mark.transform.SetParent(seenRoot, false);
        mark.transform.localPosition = new Vector3(0f, 2.35f, 0f);
        seenMarker = mark.transform;
        SeenShape(seenMarker, PrimitiveType.Cube, Vector3.zero, Vector3.one * 0.26f, Quaternion.Euler(45f, 0f, 45f));
        SeenShape(seenMarker, PrimitiveType.Cube, new Vector3(0f, -0.38f, 0f), new Vector3(0.05f, 0.42f, 0.05f), Quaternion.identity);
        SeenShape(seenRoot, PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(1.5f, 0.02f, 1.5f), Quaternion.identity);
    }

    void SeenShape(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Quaternion rot)
    {
        var go = LevelKit.Prim(parent, Vector3.zero, scale, seenMat, "SeenMark", type, rot);
        go.layer = 2;
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.transform.localScale = scale;
    }

    /// Every frame while marked: the twins follow their parts' visibility (popped plates, hidden units), the diamond
    /// turns, bobs and holds its size on screen whatever the distance. Called from Update, so only while alive.
    void TickSeen()
    {
        if (!seen || seenRoot == null) return;
        for (int i = 0; i < seenCopy.Length; i++)
        {
            var c = seenCopy[i];
            if (c == null) continue;
            var s = seenSrc[i];
            bool want = s != null && s.enabled;
            if (c.enabled != want) c.enabled = want;
        }
        if (seenMarker == null) return;
        if (seenCam == null) seenCam = Camera.main;
        float d = seenCam != null ? Vector3.Distance(seenCam.transform.position, seenMarker.position) : 10f;
        float k = Mathf.Clamp(d * 0.09f, 0.8f, 5f) / Mathf.Max(0.5f, transform.lossyScale.y);
        seenMarker.localScale = Vector3.one * k;
        seenMarker.localRotation = Quaternion.Euler(0f, Time.time * 90f, 0f);
        seenMarker.localPosition = new Vector3(0f, 2.35f + 0.08f * Mathf.Sin(Time.time * 2.4f), 0f);
    }

    /// Death or vanishing: the mark goes with it
    void DropSeen()
    {
        seen = false;
        for (int i = 0; i < seenCopy.Length; i++) if (seenCopy[i] != null) Destroy(seenCopy[i].gameObject);
        seenCopy = NoRenderers;
        seenSrc = NoRenderers;
        if (seenRoot != null) { Destroy(seenRoot.gameObject); seenRoot = null; seenMarker = null; }
    }
}
