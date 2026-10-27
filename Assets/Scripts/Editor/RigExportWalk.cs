using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The rig walk behind RigExporter: a transform without a renderer is a BONE (a pivot; its local pose is given
/// relative to the nearest bone above it), a transform with a mesh is a PROXY (type, local pose relative to its
/// bone, size, colour); hitboxes (collider, no mesh), lines, trails, lights and text are skipped. A name a rig
/// repeats (both hands' F00) is written as a dotted path under its bone ("HandR.F00") so every name is unique:
/// Blender needs that, and the runtime resolves a dotted name by walking it. JsonUtility writes the result.
/// </summary>
[Serializable]
public class RigBoneJson
{
    public string name, parent;
    public Vector3 pos;
    public Quaternion rot = Quaternion.identity;
    public Vector3 scale = Vector3.one;
    public bool active = true, animatable = true;
}

[Serializable]
public class RigProxyJson
{
    public string name, bone, type;
    public Vector3 pos;
    public Quaternion rot = Quaternion.identity;
    public Vector3 scale = Vector3.one;
    public Color color = Color.grey;
    public bool active = true;
}

[Serializable]
public class RigJson
{
    public string rig, display, root;
    public string units = "metres";
    public string axes = "unity: x right, y up, z forward, left-handed; rotations are quaternions x y z w; poses are local to the parent bone";
    public int fps = 60;
    public int version = 1;
    public List<RigBoneJson> bones = new List<RigBoneJson>();
    public List<RigProxyJson> proxies = new List<RigProxyJson>();
    public List<string> notes = new List<string>();
}

public static class RigExportWalk
{
    public static RigJson Capture(Transform root, string rigName, string display)
    {
        var j = new RigJson { rig = rigName, display = display, root = root.name };
        var counts = new Dictionary<string, int>();
        CountNames(root, counts);
        var rootBone = new RigBoneJson { name = root.name, parent = "", animatable = false };
        j.bones.Add(rootBone);
        var used = new HashSet<string> { root.name };
        Walk(root, rootBone, root, j, counts, used);
        return j;
    }

    static void CountNames(Transform t, Dictionary<string, int> counts)
    {
        for (int i = 0; i < t.childCount; i++)
        {
            var c = t.GetChild(i);
            counts[c.name] = counts.TryGetValue(c.name, out int n) ? n + 1 : 1;
            CountNames(c, counts);
        }
    }

    static string Unique(Transform c, RigBoneJson parentBone, Dictionary<string, int> counts, HashSet<string> used)
    {
        string n = c.name;
        if (counts.TryGetValue(n, out int k) && k > 1) n = parentBone.name + "." + n;
        while (used.Contains(n)) n += "_";
        used.Add(n);
        return n;
    }

    static void Walk(Transform t, RigBoneJson bone, Transform boneT, RigJson j, Dictionary<string, int> counts, HashSet<string> used)
    {
        for (int i = 0; i < t.childCount; i++)
        {
            var c = t.GetChild(i);
            var mr = c.GetComponent<MeshRenderer>();
            if (mr == null)
            {
                if (c.GetComponent<Collider>() != null) continue;   // a hitbox: neither a pivot nor a shape
                if (c.GetComponent<Renderer>() != null || c.GetComponent<Light>() != null)
                {
                    Walk(c, bone, boneT, j, counts, used);          // lines, trails, particles, lights: skipped, their children kept
                    continue;
                }
                var b = new RigBoneJson { name = Unique(c, bone, counts, used), parent = bone.name, active = c.gameObject.activeSelf };
                Local(boneT, c, out b.pos, out b.rot, out b.scale);
                j.bones.Add(b);
                Walk(c, b, c, j, counts, used);
                continue;
            }
            var mf = c.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null && c.GetComponent<TextMesh>() == null)
            {
                var p = new RigProxyJson { name = c.name, bone = bone.name, active = c.gameObject.activeSelf && mr.enabled };
                Local(boneT, c, out p.pos, out p.rot, out p.scale);
                string mn = mf.sharedMesh.name;
                p.type = mn == "Cube" || mn == "Sphere" || mn == "Cylinder" || mn == "Capsule" || mn == "Quad" ? mn : "Mesh";
                if (p.type == "Mesh")
                {
                    // Any other mesh is shown as its bounding box
                    var bb = mf.sharedMesh.bounds;
                    p.pos += p.rot * Vector3.Scale(p.scale, bb.center);
                    p.scale = Vector3.Scale(p.scale, bb.size);
                }
                p.color = ColorOf(mr.sharedMaterial);
                j.proxies.Add(p);
            }
            Walk(c, bone, boneT, j, counts, used);
        }
    }

    static void Local(Transform boneT, Transform c, out Vector3 pos, out Quaternion rot, out Vector3 scale)
    {
        Matrix4x4 m = boneT.worldToLocalMatrix * c.localToWorldMatrix;
        pos = m.GetPosition();
        rot = m.rotation;
        scale = m.lossyScale;
    }

    static Color ColorOf(Material m)
    {
        Color c = new Color(0.6f, 0.6f, 0.6f, 1f);
        if (m == null) return c;
        if (m.HasProperty("_BaseColor")) c = m.GetColor("_BaseColor");
        else if (m.HasProperty("_Color")) c = m.GetColor("_Color");
        if (m.HasProperty("_EmissionColor"))
        {
            var e = m.GetColor("_EmissionColor");
            if (e.maxColorComponent > c.maxColorComponent) c = e;   // a lit part shows its glow's hue
        }
        float mx = c.maxColorComponent;
        if (mx > 1f) c /= mx;
        c.a = 1f;
        return c;
    }

    /// The arm rig is built with everything at the origin (the solver places it every frame): give Blender a
    /// rest — the idle hands, the shoulders where the config puts them, the elbows from a two-bone solve
    public static void PoseArmsAtRest(RigJson j, MovementConfig config)
    {
        var idleR = new ArmRig.ArmPose(new Vector3(0.21f, -0.2f, 0.38f), new Vector3(-20f, -12f, -40f));
        SetBone(j, "HandR", idleR.pos, idleR.rot);
        var idleL = idleR.Mirrored();
        SetBone(j, "HandL", idleL.pos, idleL.rot);
        SolveArmRest(j, "R", 1f, config);
        SolveArmRest(j, "L", -1f, config);
    }

    static RigBoneJson Find(RigJson j, string name)
    {
        foreach (var b in j.bones) if (b.name == name) return b;
        return null;
    }

    static void SetBone(RigJson j, string name, Vector3 pos, Quaternion rot)
    {
        var b = Find(j, name);
        if (b == null) return;
        b.pos = pos;
        b.rot = rot;
    }

    static void SolveArmRest(RigJson j, string side, float sign, MovementConfig c)
    {
        var hand = Find(j, "Hand" + side);
        var sh = Find(j, "Shoulder" + side);
        var el = Find(j, "Elbow" + side);
        if (hand == null || sh == null || el == null) return;
        Vector3 S = c.viewmodelShoulder;
        S.x *= sign;
        Vector3 H = hand.pos;
        float L1 = Mathf.Max(0.05f, c.viewmodelUpperArm), L2 = Mathf.Max(0.05f, c.viewmodelForearm);
        Vector3 d = H - S;
        float dist = Mathf.Clamp(d.magnitude, 1e-3f, (L1 + L2) * 0.999f);
        Vector3 dir = d.sqrMagnitude > 1e-8f ? d.normalized : Vector3.forward;
        float a = (L1 * L1 - L2 * L2 + dist * dist) / (2f * dist);
        float h = Mathf.Sqrt(Mathf.Max(0f, L1 * L1 - a * a));
        Vector3 pole = new Vector3(sign * c.viewmodelElbowOut, -c.viewmodelElbowDown, 0f);
        pole -= dir * Vector3.Dot(pole, dir);
        if (pole.sqrMagnitude < 1e-6f) pole = Vector3.down;
        pole.Normalize();
        Vector3 E = S + dir * a + pole * h;
        sh.pos = S;
        sh.rot = Quaternion.LookRotation(E - S, Vector3.up);
        sh.animatable = false;
        el.pos = E;
        el.rot = Quaternion.LookRotation(H - E, Vector3.up);
        el.animatable = false;
    }
}
