using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE JANITOR — SANITATION UNIT. Halcyon's cleaner: a barrel of safety-yellow
/// armour on short legs, pauldrons like boulders, a diving-bell dome with three
/// amber portholes for a face, a floor buffer on one arm and a hose nozzle on
/// the other, a tank on its back with the core in it. Since the simplification
/// (2026-10-16) only the BODY lives here: it is a big footman on its line
/// (EnemyRoles), its core open like every glass head — the execute.
/// </summary>
public partial class Enemy
{
    /// The HUD's heavy bar reads this list; nothing joins it now (no unit is a miniboss), so the bar never shows
    public static readonly List<Enemy> Janitors = new List<Enemy>();
    public const string JanitorName = "JANITOR";   // the House's staff since the overhaul: the role stays, the "unit" goes

    static Material janitorPlate, janitorAmber, janitorRed, janitorRust;
    static Material JanitorPlate() => janitorPlate ??= Lit(new Color(0.95f, 0.72f, 0.12f), 0.35f, 0.55f);
    static Material JanitorAmber() => janitorAmber ??= Unlit(new Color(2.2f, 1.3f, 0.3f));
    static Material JanitorRed() => janitorRed ??= Unlit(new Color(2.4f, 0.25f, 0.15f));
    static Material JanitorRust() => janitorRust ??= Lit(new Color(0.45f, 0.24f, 0.12f), 0.2f, 0.25f);

    /// A janitor at 1.35× the base body: the barrel comes to your shoulder, the dome well above you
    public static Enemy CreateJanitor(Vector3 groundPos, MovementConfig config)
    {
        var e = Create(EnemyKind.Janitor, groundPos, config);
        if (e == null) return null;   // no units (Enemy.Units): nothing to shape
        e.transform.localScale = Vector3.one * 1.35f;
        return e;
    }

    // ---------------------------------------------------------------- the rig

    static Rig BuildJanitorRig(Transform root, Material plate)
    {
        var rig = new Rig();
        var plates = new List<Renderer>();
        Material charcoal = Charcoal(), joint = JointMat(), amber = JanitorAmber(), rust = JanitorRust(), black = Satin();

        // Hitbox: one thick capsule, boots to pauldrons; the dome is solid too (a body hit)
        var hitbox = new GameObject("Hitbox");
        hitbox.transform.SetParent(root, false);
        var capsule = hitbox.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 1.05f, 0f);
        capsule.radius = 0.5f;
        capsule.height = 1.9f;

        Part(root, PrimitiveType.Cube, new Vector3(0f, 0.95f, 0f), new Vector3(0.7f, 0.25f, 0.5f), charcoal, "Pelvis");
        Part(root, PrimitiveType.Cylinder, new Vector3(0f, 0.98f, 0f), new Vector3(0.74f, 0.03f, 0.54f), black, "WaistBand");
        rig.legL = JanitorLeg(root, -0.22f, charcoal, joint, black);
        rig.legR = JanitorLeg(root, 0.22f, charcoal, joint, black);

        // The barrel, striped; the chest plate with the unit's name; pauldrons; the beacon
        rig.torso = Pivot(root, "Torso", new Vector3(0f, 1.05f, 0f));
        rig.torsoRest = rig.torso.localPosition;
        plates.Add(Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 0f), new Vector3(1.0f, 0.55f, 1.0f), plate, "Barrel"));
        Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0f, 0.3f, 0f), new Vector3(1.03f, 0.04f, 1.03f), black, "HazardBand");
        Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0f, 0.8f, 0f), new Vector3(1.03f, 0.04f, 1.03f), black, "HazardBand");
        Part(rig.torso, PrimitiveType.Cube, new Vector3(0f, 0.62f, 0.42f), new Vector3(0.6f, 0.55f, 0.22f), charcoal, "ChestPlate");
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
                Part(rig.torso, PrimitiveType.Sphere, new Vector3(sx * 0.24f, 0.62f + sy * 0.21f, 0.535f), Vector3.one * 0.05f, joint, "Rivet");
        LevelKit.Text(rig.torso, rig.torso.TransformPoint(new Vector3(0f, 0.62f, 0.54f)), rig.torso.rotation * Quaternion.Euler(0f, 180f, 0f),
            Plate(EnemyKind.Janitor), 0.012f, new Color(1.8f, 1.2f, 0.3f), "ChestLabel", 0.5f, true, 20f, 45f);
        plates.Add(Part(rig.torso, PrimitiveType.Sphere, new Vector3(-0.6f, 1.0f, 0f), Vector3.one * 0.5f, plate, "PauldronL"));
        plates.Add(Part(rig.torso, PrimitiveType.Sphere, new Vector3(0.6f, 1.0f, 0f), Vector3.one * 0.5f, plate, "PauldronR"));
        Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0.52f, 1.24f, 0f), new Vector3(0.18f, 0.02f, 0.18f), black, "BeaconBase");
        var beacon = Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0.52f, 1.3f, 0f), new Vector3(0.14f, 0.06f, 0.14f), amber, "Beacon");
        var pulse = beacon.gameObject.AddComponent<PulseGlow>();
        pulse.rate = 2.2f;
        pulse.depth = 0.7f;

        // The tank on its back, and in the tank's back the CORE under a hatch
        Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0f, 0.6f, -0.62f), new Vector3(0.55f, 0.36f, 0.55f), charcoal, "Tank");
        Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0f, 0.98f, -0.62f), new Vector3(0.3f, 0.06f, 0.3f), joint, "TankCap");
        Part(rig.torso, PrimitiveType.Cylinder, new Vector3(0f, 0.6f, -0.62f), new Vector3(0.58f, 0.03f, 0.58f), rust, "TankBand");
        Renderer core = Part(rig.torso, PrimitiveType.Sphere, new Vector3(0f, 0.55f, -0.88f), Vector3.one * 0.26f, WeakMat(), "Core", keepCollider: true);
        core.gameObject.AddComponent<EnemyWeakspot>();
        rig.head = core;
        rig.throat = core;
        Transform halo = Pivot(rig.torso, "HaloPivot", new Vector3(0f, 0.55f, -0.88f));
        halo.localScale = Vector3.one * 0.5f;
        rig.ring = Part(halo, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.9f, 0.015f, 0.9f), RingMat(), "WeakspotRing").transform;
        rig.hatch = Pivot(rig.torso, "Hatch", new Vector3(0f, 0.78f, -0.9f));
        Part(rig.hatch, PrimitiveType.Cube, new Vector3(0f, -0.22f, -0.04f), new Vector3(0.42f, 0.44f, 0.06f), plate, "HatchPlate");
        Part(rig.hatch, PrimitiveType.Cube, new Vector3(0f, -0.22f, -0.075f), new Vector3(0.3f, 0.06f, 0.02f), black, "HatchStripe");
        Part(rig.hatch, PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.05f), new Vector3(0.06f, 0.22f, 0.06f), joint, "Hinge").transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

        // Arms: a hose nozzle on the left, the floor buffer on the right
        rig.armL = JanitorArm(rig.torso, -0.62f, plate, charcoal, joint, black, true, out _);
        rig.armR = JanitorArm(rig.torso, 0.62f, plate, charcoal, joint, black, false, out rig.buffer);
        rig.muzzle = null;

        // The dome: a diving bell with three amber portholes — nothing to cut here
        rig.headPivot = Pivot(rig.torso, "HeadPivot", new Vector3(0f, 1.22f, 0.12f));
        Part(rig.headPivot, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.5f, charcoal, "Dome", keepCollider: true);
        Part(rig.headPivot, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.54f, 0.03f, 0.54f), joint, "DomeRim");
        Vector3[] ports = { new Vector3(-0.13f, 0.02f, 0.235f), new Vector3(0f, 0.05f, 0.245f), new Vector3(0.13f, 0.02f, 0.235f) };
        foreach (var p in ports)
        {
            Part(rig.headPivot, PrimitiveType.Cylinder, p - new Vector3(0f, 0f, 0.008f), new Vector3(0.15f, 0.012f, 0.15f), black, "PortRim").transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part(rig.headPivot, PrimitiveType.Cylinder, p, new Vector3(0.11f, 0.014f, 0.11f), amber, "Porthole").transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        rig.plates = plates.ToArray();
        return rig;
    }

    static Transform JanitorLeg(Transform root, float x, Material charcoal, Material joint, Material black)
    {
        Transform hip = Pivot(root, x < 0f ? "LegL" : "LegR", new Vector3(x, 0.85f, 0f));
        Part(hip, PrimitiveType.Cylinder, new Vector3(0f, -0.2f, 0f), new Vector3(0.3f, 0.2f, 0.3f), charcoal, "Thigh");
        Part(hip, PrimitiveType.Sphere, new Vector3(0f, -0.4f, 0f), Vector3.one * 0.3f, joint, "Knee");
        Part(hip, PrimitiveType.Cylinder, new Vector3(0f, -0.62f, 0f), new Vector3(0.28f, 0.2f, 0.28f), charcoal, "Shin");
        Part(hip, PrimitiveType.Cube, new Vector3(0f, -0.79f, 0.08f), new Vector3(0.36f, 0.12f, 0.5f), black, "Boot");
        return hip;
    }

    static Transform JanitorArm(Transform torso, float x, Material plate, Material charcoal, Material joint, Material black, bool nozzle, out Transform tool)
    {
        Transform shoulder = Pivot(torso, x < 0f ? "ArmL" : "ArmR", new Vector3(x, 0.95f, 0f));
        Part(shoulder, PrimitiveType.Cylinder, new Vector3(0f, -0.3f, 0f), new Vector3(0.3f, 0.25f, 0.3f), charcoal, "UpperArm");
        Part(shoulder, PrimitiveType.Sphere, new Vector3(0f, -0.6f, 0f), Vector3.one * 0.32f, joint, "Elbow");
        Part(shoulder, PrimitiveType.Cylinder, new Vector3(0f, -0.85f, 0f), new Vector3(0.28f, 0.22f, 0.28f), charcoal, "Forearm");
        Part(shoulder, PrimitiveType.Cylinder, new Vector3(0f, -0.72f, 0f), new Vector3(0.3f, 0.02f, 0.3f), black, "ArmBand");
        tool = null;
        if (nozzle)
        {
            Part(shoulder, PrimitiveType.Cylinder, new Vector3(0f, -1.1f, 0f), new Vector3(0.22f, 0.15f, 0.22f), joint, "Nozzle");
            Part(shoulder, PrimitiveType.Cylinder, new Vector3(0f, -1.28f, 0f), new Vector3(0.32f, 0.05f, 0.32f), black, "NozzleTip");
            return shoulder;
        }
        Part(shoulder, PrimitiveType.Sphere, new Vector3(0f, -1.06f, 0.05f), Vector3.one * 0.16f, joint, "Hub");
        tool = Part(shoulder, PrimitiveType.Cylinder, new Vector3(0f, -1.13f, 0.05f), new Vector3(0.7f, 0.06f, 0.7f), charcoal, "Buffer").transform;
        Part(tool, PrimitiveType.Cylinder, Vector3.zero, new Vector3(1.03f, 0.35f, 1.03f), plate, "BufferRim");
        return shoulder;
    }

}

/// A red ring that runs out across the floor and is gone: the slam's shockwave, drawn
public class ShockRing : MonoBehaviour
{
    float born, life, r0, r1;
    static Material ringMat, cyanMat;

    /// Red for a threat on the floor; cyan for the cut's own shock
    public static void Spawn(Vector3 at, float fromRadius, float toRadius, float seconds, bool cyan = false)
    {
        if (ringMat == null)
        {
            ringMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            ringMat.SetColor("_BaseColor", new Color(2.6f, 0.2f, 0.12f));
            cyanMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            cyanMat.SetColor("_BaseColor", new Color(0.3f, 2f, 2.4f));
        }
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "ShockRing";
        go.layer = 2;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.position = at;
        go.GetComponent<MeshRenderer>().sharedMaterial = cyan ? cyanMat : ringMat;
        var ring = go.AddComponent<ShockRing>();
        ring.born = Time.time;
        ring.life = seconds;
        ring.r0 = fromRadius;
        ring.r1 = toRadius;
    }

    void Update()
    {
        float k = (Time.time - born) / life;
        if (k >= 1f) { Destroy(gameObject); return; }
        float r = Mathf.Lerp(r0, r1, 1f - Mathf.Pow(1f - k, 2f));
        transform.localScale = new Vector3(r * 2f, 0.05f, r * 2f);
    }
}
