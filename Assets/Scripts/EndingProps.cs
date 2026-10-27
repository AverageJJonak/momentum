using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// THE END OF THE UNDERGROUND — the props the ending needs, and the first of what Chapter 3 will be made of: things
/// burst, things fallen over, steam standing over a hole in the street. The brass LIFT CAR (a root you raise and
/// everything rides: brass posts, cream panels with an oxblood band, an open-work floor the shaft shows through, a
/// collapsible brass gate), the RELIEF VALVE post (a lever the cutscene slams, its gauges, its dome, the valve wheel),
/// alarm lamps (dark until the override), the forecourt's CLOCK TOWER on a pivot so it can come down, fires that
/// flicker, a column of smoke or steam that stands over the mouth for as long as the level lasts, a column of fire or
/// steam that climbs a shaft and blows off, a steam burst, and the flinging of whatever was standing. Every still piece
/// is a primitive with no collider; only the debris gets colliders, because it has to land in the forecourt and stay.
/// </summary>
public static class EndingProps
{
    static Material concrete, ash, scorch, black, alarm, fire, ember, smoke, dust, dark, steam, steamWhite;
    public static Material Concrete => concrete != null ? concrete : concrete = Lit(new Color(0.3f, 0.29f, 0.28f), 0.15f);
    public static Material Ash => ash != null ? ash : ash = Lit(new Color(0.24f, 0.22f, 0.21f), 0.05f);   // ash, not a black void: the dawn has to land on it
    public static Material Scorch => scorch != null ? scorch : scorch = Lit(new Color(0.03f, 0.03f, 0.03f), 0.1f);
    public static Material Black => black != null ? black : black = Unlit(new Color(0.008f, 0.008f, 0.01f));
    public static Material Dark => dark != null ? dark : dark = Lit(new Color(0.06f, 0.06f, 0.07f), 0.3f);
    /// The danger red: alarms, the override's knob and dome — never a unit's own light
    public static Material AlarmRed => alarm != null ? alarm : alarm = Unlit(new Color(2.6f, 0.25f, 0.15f));
    public static Material Fire => fire != null ? fire : fire = Unlit(new Color(2.6f, 1.2f, 0.3f));
    public static Material Ember => ember != null ? ember : ember = Unlit(new Color(3f, 2.2f, 1f));
    public static Material Smoke => smoke != null ? smoke : smoke = Fade(new Color(0.08f, 0.07f, 0.07f, 0.55f));
    /// The dust a blast drives up a shaft: the rock's own colour, thinner than the smoke
    public static Material Dust => dust != null ? dust : dust = Fade(new Color(0.5f, 0.42f, 0.34f, 0.45f));
    /// Steam: white, and a steam burst's own hot white
    public static Material Steam => steam != null ? steam : steam = Fade(new Color(0.9f, 0.9f, 0.92f, 0.5f));
    public static Material SteamWhite => steamWhite != null ? steamWhite : steamWhite = Unlit(new Color(2.4f, 2.4f, 2.5f));

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    static Material Lit(Color c, float smooth)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor(BaseColor, c);
        m.SetFloat("_Smoothness", smooth);
        return m;
    }

    static Material Unlit(Color c)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetColor(BaseColor, c);
        return m;
    }

    /// An unlit material that blends: the smoke
    static Material Fade(Color c)
    {
        var m = LevelKit.FromTemplate("UnlitTransparent", "Universal Render Pipeline/Unlit");
        m.SetColor(BaseColor, c);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        return m;
    }

    /// A primitive in a parent's own frame, no collider, no shadow
    public static GameObject Piece(Transform parent, Vector3 localPos, Vector3 scale, Material mat, string name,
        PrimitiveType type = PrimitiveType.Cube, float rx = 0f, float ry = 0f, float rz = 0f)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(rx, ry, rz);
        go.transform.localScale = scale;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        return go;
    }

    // ------------------------------------------------------------ the cage

    /// Where you stand in the cage, in its own frame: on the grate, a little back from the doors
    public static readonly Vector3 Seat = new Vector3(0f, 0.2f, -0.2f);

    /// The lift car: four brass posts, an open-work floor you can see the shaft through, a cream roof with a lamp under
    /// it, a back and two sides of cream panel under an oxblood band with brass bars above them, a collapsible brass
    /// gate on the front (+z) hinged on its posts and folded back open along the sides (CageDoors shuts it), the hook on
    /// the roof. Its root is the floor's centre
    public static Transform Cage(Transform parent, Vector3 at, float yaw)
    {
        var root = new GameObject("LiftCar").transform;
        root.SetParent(parent);
        root.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
        Material girder = LevelKit.Brass, iron = LevelKit.Bronze, panel = LevelKit.Cream;
        Piece(root, new Vector3(0f, 1.12f, -1.33f), new Vector3(2.7f, 0.2f, 0.03f), LevelKit.Oxblood, "CarBand");
        Piece(root, new Vector3(-1.33f, 1.12f, 0f), new Vector3(0.03f, 0.2f, 2.7f), LevelKit.Oxblood, "CarBand");
        Piece(root, new Vector3(1.33f, 1.12f, 0f), new Vector3(0.03f, 0.2f, 2.7f), LevelKit.Oxblood, "CarBand");
        Piece(root, new Vector3(0f, 2.5f, -1.34f), new Vector3(1.2f, 0.3f, 0.02f), LevelKit.Cream, "CarPlate");
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Piece(root, new Vector3(sx * 1.3f, 1.5f, sz * 1.3f), new Vector3(0.14f, 3f, 0.14f), girder, "CagePost");
        // The floor: a rim and a grate in two layers (never two faces on one plane), so the well shows between your feet
        Piece(root, new Vector3(0f, 0.1f, -1.4f), new Vector3(2.9f, 0.2f, 0.1f), iron, "CageFloorRim");
        Piece(root, new Vector3(0f, 0.1f, 1.4f), new Vector3(2.9f, 0.2f, 0.1f), iron, "CageFloorRim");
        Piece(root, new Vector3(-1.4f, 0.1f, 0f), new Vector3(0.1f, 0.2f, 2.7f), iron, "CageFloorRim");
        Piece(root, new Vector3(1.4f, 0.1f, 0f), new Vector3(0.1f, 0.2f, 2.7f), iron, "CageFloorRim");
        for (int i = -2; i <= 2; i++)
        {
            Piece(root, new Vector3(i * 0.5f, 0.17f, 0f), new Vector3(0.05f, 0.06f, 2.7f), iron, "CageGrate");
            Piece(root, new Vector3(0f, 0.11f, i * 0.5f), new Vector3(2.7f, 0.06f, 0.05f), iron, "CageGrate");
        }
        Piece(root, new Vector3(0f, 3.05f, 0f), new Vector3(2.9f, 0.14f, 2.9f), panel, "CageRoof");
        Piece(root, new Vector3(0f, 2.97f, 0f), new Vector3(2.6f, 0.02f, 2.6f), LevelKit.LampCream, "CageLamp");
        Piece(root, new Vector3(0f, 0.65f, -1.3f), new Vector3(2.7f, 0.9f, 0.06f), panel, "CageBack");
        Piece(root, new Vector3(-1.3f, 0.65f, 0f), new Vector3(0.06f, 0.9f, 2.7f), panel, "CageSide");
        Piece(root, new Vector3(1.3f, 0.65f, 0f), new Vector3(0.06f, 0.9f, 2.7f), panel, "CageSide");
        for (int i = -2; i <= 2; i++)
        {
            Piece(root, new Vector3(i * 0.55f, 2f, -1.3f), new Vector3(0.04f, 1.8f, 0.04f), iron, "CageMesh");
            Piece(root, new Vector3(-1.3f, 2f, i * 0.55f), new Vector3(0.04f, 1.8f, 0.04f), iron, "CageMesh");
            Piece(root, new Vector3(1.3f, 2f, i * 0.55f), new Vector3(0.04f, 1.8f, 0.04f), iron, "CageMesh");
        }
        Piece(root, new Vector3(0f, 1.9f, -1.3f), new Vector3(2.7f, 0.05f, 0.05f), iron, "CageRail");
        // The doors: a leaf of bars on each front post, lying open along its side; the hinge turns it across the front
        for (int s = -1; s <= 1; s += 2)
        {
            var hinge = new GameObject(s < 0 ? "CageDoorL" : "CageDoorR").transform;
            hinge.SetParent(root, false);
            hinge.localPosition = new Vector3(s * 1.22f, 1.55f, 1.15f);
            Piece(hinge, new Vector3(0f, 1.3f, -0.6f), new Vector3(0.05f, 0.05f, 1.2f), iron, "CageDoorRail");
            Piece(hinge, new Vector3(0f, -1.3f, -0.6f), new Vector3(0.05f, 0.05f, 1.2f), iron, "CageDoorRail");
            for (int i = 0; i < 4; i++) Piece(hinge, new Vector3(0f, 0f, -0.15f - i * 0.3f), new Vector3(0.04f, 2.65f, 0.04f), iron, "CageDoorBar");
            Piece(hinge, new Vector3(0f, -0.55f, -0.6f), new Vector3(0.06f, 0.1f, 1.2f), LevelKit.Oxblood, "CageDoorBand");
        }
        Piece(root, new Vector3(0f, 3.4f, 0f), new Vector3(0.3f, 0.5f, 0.3f), iron, "CageHook");
        var l = LevelKit.Light(root, at + Vector3.up * 2.6f, 5f, 1.4f);
        l.color = new Color(1f, 0.9f, 0.7f);   // lamp cream: every lamp is warm
        return root;
    }

    /// The cage's doors, `shut` 0 (folded open along the sides) to 1 (closed across the front)
    public static void CageDoors(Transform cage, float shut)
    {
        if (cage == null) return;
        var l = cage.Find("CageDoorL");
        var r = cage.Find("CageDoorR");
        float a = 90f * Mathf.Clamp01(shut);
        if (l != null) l.localRotation = Quaternion.Euler(0f, -a, 0f);
        if (r != null) r.localRotation = Quaternion.Euler(0f, a, 0f);
    }

    // ------------------------------------------------------------ the override post

    public class Post
    {
        public Vector3 at, facing;
        public Transform lever;
        public Transform[] needles;
        public Renderer dome;
        public Light lamp;
        public TextMesh readout;
    }

    /// The relief valve: a riveted pedestal with the mains' valve wheel on its side and a sloped panel of three gauges,
    /// a lever on a pivot at the back with the danger red on its knob, a dome lamp on a stalk (dark until the slam), a
    /// readout, and the red sign over it
    public static Post OverridePost(Transform parent, Vector3 at, Vector3 facing)
    {
        var p = new Post { at = at, facing = facing.normalized };
        var root = new GameObject("ReliefValvePost").transform;
        root.SetParent(parent);
        root.SetPositionAndRotation(at, Quaternion.LookRotation(p.facing, Vector3.up));
        LevelKit.Slab(parent, at + Vector3.up * 0.55f, new Vector3(1.2f, 1.1f, 0.8f), LevelKit.RivetIron, "PostBody");
        Vector3 side = Quaternion.LookRotation(p.facing, Vector3.up) * Vector3.right;
        LevelKit.Valve(parent, at + side * 0.75f + Vector3.up * 0.7f, side);
        LevelKit.Pipe(parent, at + side * 0.75f + Vector3.up * 0.25f, at + side * 0.75f - Vector3.up * 1.5f, 0.3f);
        Piece(root, new Vector3(0f, 1.16f, 0.05f), new Vector3(1.1f, 0.14f, 0.72f), LevelKit.Graphite, "PostPanel", PrimitiveType.Cube, -18f);
        Piece(root, new Vector3(0f, 0.2f, 0.41f), new Vector3(1.1f, 0.06f, 0.02f), LevelKit.Amber, "PostTrim");
        p.needles = new Transform[3];
        for (int i = 0; i < 3; i++)
        {
            float x = -0.32f + i * 0.32f;
            Piece(root, new Vector3(x, 1.25f, 0.2f), new Vector3(0.22f, 0.22f, 0.02f), LevelKit.Led, "GaugeFace", PrimitiveType.Cube, -72f);
            var pivot = new GameObject("Needle").transform;
            pivot.SetParent(root, false);
            pivot.localPosition = new Vector3(x, 1.26f, 0.205f);
            pivot.localRotation = Quaternion.Euler(-72f, 0f, 40f);
            Piece(pivot, new Vector3(0f, 0.07f, 0f), new Vector3(0.02f, 0.14f, 0.012f), Dark, "NeedleHand");
            p.needles[i] = pivot;
        }
        var lever = new GameObject("Lever").transform;
        lever.SetParent(root, false);
        lever.localPosition = new Vector3(0f, 1.15f, -0.22f);
        lever.localRotation = Quaternion.Euler(-38f, 0f, 0f);
        Piece(lever, new Vector3(0f, 0.45f, 0f), new Vector3(0.08f, 0.9f, 0.08f), LevelKit.Iron, "LeverShaft");
        Piece(lever, new Vector3(0f, 0.92f, 0f), new Vector3(0.24f, 0.24f, 0.24f), AlarmRed, "LeverKnob", PrimitiveType.Sphere);
        Piece(root, new Vector3(0f, 1.12f, -0.22f), new Vector3(0.3f, 0.12f, 0.24f), LevelKit.Iron, "LeverBoss");
        p.lever = lever;
        Piece(root, new Vector3(0.48f, 1.36f, -0.28f), new Vector3(0.06f, 0.5f, 0.06f), LevelKit.Iron, "LampStalk");
        p.dome = Piece(root, new Vector3(0.48f, 1.64f, -0.28f), Vector3.one * 0.28f, LevelKit.Graphite, "Dome", PrimitiveType.Sphere).GetComponent<Renderer>();
        p.lamp = LevelKit.Light(root, at + Vector3.up * 1.7f, 9f, 0f);
        p.lamp.color = new Color(1f, 0.2f, 0.12f);
        p.lamp.enabled = false;
        p.readout = LevelKit.Display(parent, at + Vector3.up * 2.35f - p.facing * 0.3f, p.facing, 2.4f, 0.5f, "RELIEF   ·   ARMED");
        LevelKit.HoloSign(parent, at + Vector3.up * 3.3f - p.facing * 0.3f, p.facing, 5.2f, 1f, "STEAM MAINS  ·  RELIEF VALVE  ·  HAND LEVER", true);
        return p;
    }

    // ------------------------------------------------------------ alarms

    public class Alarm
    {
        public Renderer dome;
        public Light light;
    }

    /// A red dome on a wall bracket, dark until the override; the cutscene strobes it
    public static Alarm AlarmLamp(Transform parent, Vector3 at, Vector3 facing)
    {
        var a = new Alarm();
        var root = new GameObject("AlarmLamp").transform;
        root.SetParent(parent);
        root.SetPositionAndRotation(at, Quaternion.LookRotation(facing.normalized, Vector3.up));
        Piece(root, new Vector3(0f, 0f, 0.08f), new Vector3(0.4f, 0.3f, 0.16f), LevelKit.Iron, "AlarmBracket");
        a.dome = Piece(root, new Vector3(0f, 0f, 0.28f), new Vector3(0.34f, 0.34f, 0.3f), LevelKit.Graphite, "AlarmDome", PrimitiveType.Sphere).GetComponent<Renderer>();
        a.light = LevelKit.Light(root, at + facing.normalized * 0.6f, 14f, 0f);
        a.light.color = new Color(1f, 0.18f, 0.1f);
        a.light.enabled = false;
        return a;
    }

    /// The alarms lit or dark; `k` = how bright, 0 puts them out
    public static void SetAlarm(Alarm a, float k)
    {
        if (a == null) return;
        bool on = k > 0.01f;
        if (a.dome != null) a.dome.sharedMaterial = on ? AlarmRed : LevelKit.Graphite;
        if (a.light != null) { a.light.enabled = on; a.light.intensity = 3.2f * k; }
    }

    // ------------------------------------------------------------ the stack that comes down

    /// The forecourt's clock tower on a pivot at its foot, so it can be tilted over: a brick shaft with limestone
    /// string courses, the clock's cream face near the top, a limestone cap, a brass lantern, a red aircraft lamp
    public static Transform Stack(Transform parent, Vector3 at, float height, float dia)
    {
        var pivot = new GameObject("TowerPivot").transform;
        pivot.SetParent(parent);
        pivot.position = at;
        Piece(pivot, new Vector3(0f, height * 0.5f, 0f), new Vector3(dia, height, dia), LevelKit.Brick, "Tower");
        Piece(pivot, new Vector3(0f, 0.6f, 0f), new Vector3(dia + 1.2f, 1.2f, dia + 1.2f), LevelKit.Limestone, "TowerFoot");
        for (int i = 1; i <= 3; i++)
            Piece(pivot, new Vector3(0f, height * (0.22f * i), 0f), new Vector3(dia + 0.16f, 0.3f, dia + 0.16f), LevelKit.Limestone, "StringCourse");
        for (int k = 0; k < 4; k++)
        {
            Quaternion face = Quaternion.Euler(0f, k * 90f, 0f);
            Piece(pivot, face * new Vector3(0f, height - 2.6f, dia * 0.5f + 0.02f), new Vector3(dia * 0.7f, dia * 0.7f, 0.04f), LevelKit.LampCream, "ClockFace", PrimitiveType.Cylinder, 90f, k * 90f);
            Piece(pivot, face * new Vector3(0f, height - 2.2f, dia * 0.5f + 0.06f), new Vector3(0.06f, dia * 0.26f, 0.02f), Dark, "ClockHand", PrimitiveType.Cube, 0f, k * 90f);
        }
        Piece(pivot, new Vector3(0f, height + 0.2f, 0f), new Vector3(dia + 0.6f, 0.4f, dia + 0.6f), LevelKit.Limestone, "TowerCap");
        Piece(pivot, new Vector3(0f, height + 1f, 0f), new Vector3(0.9f, 1.2f, 0.9f), LevelKit.Brass, "Lantern");
        Piece(pivot, new Vector3(0f, height + 1.9f, 0f), Vector3.one * 0.5f, AlarmRed, "TowerBeacon", PrimitiveType.Sphere);
        return pivot;
    }

    // ------------------------------------------------------------ steam

    /// A steam burst: white spheres thrown up from `at`, swelling and thinning to nothing in a second and a half, a
    /// flash of light with them; then gone
    public class SteamBurst : MonoBehaviour
    {
        Transform[] balls;
        Renderer[] rends;
        Vector3[] vel;
        MaterialPropertyBlock block;
        Light glow;
        float size, born;

        public static SteamBurst Spawn(Vector3 at, float size)
        {
            var go = new GameObject("SteamBurst") { layer = 2 };
            go.transform.position = at;
            var s = go.AddComponent<SteamBurst>();
            s.size = size;
            s.born = Time.time;
            s.block = new MaterialPropertyBlock();
            int n = 9;
            s.balls = new Transform[n];
            s.rends = new Renderer[n];
            s.vel = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                var b = Piece(go.transform, Vector3.zero, Vector3.one * 0.01f, SteamWhite, "Puff", PrimitiveType.Sphere);
                b.layer = 2;
                s.balls[i] = b.transform;
                s.rends[i] = b.GetComponent<Renderer>();
                float a = i * 0.7f;
                s.vel[i] = new Vector3(Mathf.Sin(a) * size * 1.2f, size * (2.2f + 1.4f * ((i * 7) % 3)), Mathf.Cos(a) * size * 1.2f);
            }
            s.glow = LevelKit.Light(go.transform, at + Vector3.up * size, size * 6f, 6f);
            s.glow.color = new Color(1f, 0.96f, 0.9f);
            return s;
        }

        void Update()
        {
            float age = Time.time - born;
            float k = Mathf.Clamp01(age / 1.6f);
            for (int i = 0; i < balls.Length; i++)
            {
                balls[i].position = transform.position + vel[i] * age - Vector3.up * (0.6f * age * age);
                balls[i].localScale = Vector3.one * (size * (0.5f + 2.5f * k));
                block.SetColor(BaseColor, new Color(2.4f, 2.4f, 2.5f, 1f) * (1f - k) + new Color(0.3f, 0.3f, 0.32f, 1f) * k);
                rends[i].SetPropertyBlock(block);
            }
            if (glow != null) glow.intensity = 6f * (1f - k);
            if (k >= 1f) Destroy(gameObject);
        }
    }

    // ------------------------------------------------------------ fire

    /// A fire on the ground: tongues that lean and stretch on their own noise, embers, a light that breathes
    public class Flames : MonoBehaviour
    {
        Transform[] tongues;
        Vector3[] rest;
        Light glow;
        float size, seed;

        public static Flames Spawn(Transform parent, Vector3 at, float size)
        {
            var go = new GameObject("Fire");
            go.transform.SetParent(parent);
            go.transform.position = at;
            var f = go.AddComponent<Flames>();
            f.size = size;
            f.seed = Random.Range(0f, 100f);
            int n = 6;
            f.tongues = new Transform[n];
            f.rest = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float a = i * 1.05f;
                f.rest[i] = new Vector3(Mathf.Sin(a) * size * 0.35f, size * 0.4f, Mathf.Cos(a) * size * 0.35f);
                f.tongues[i] = Piece(go.transform, f.rest[i], new Vector3(size * 0.3f, size * 0.9f, size * 0.3f), i % 3 == 0 ? Ember : Fire, "Tongue").transform;
            }
            Piece(go.transform, new Vector3(0f, 0.04f, 0f), new Vector3(size * 1.6f, 0.06f, size * 1.6f), Scorch, "Scorch", PrimitiveType.Cylinder);
            f.glow = LevelKit.Light(go.transform, at + Vector3.up * (size * 0.8f), size * 7f, 2.5f);
            f.glow.color = new Color(1f, 0.55f, 0.2f);
            LevelKit.Embers(go.transform, at + Vector3.up * (size * 0.5f));
            return f;
        }

        void Update()
        {
            float now = Time.time;
            for (int i = 0; i < tongues.Length; i++)
            {
                float n1 = Mathf.PerlinNoise(seed + i * 3.1f, now * 6f), n2 = Mathf.PerlinNoise(now * 5f, seed + i * 2.3f);
                var t = tongues[i];
                t.localPosition = rest[i] + new Vector3((n2 - 0.5f) * size * 0.2f, size * 0.25f * n1, 0f);
                t.localScale = new Vector3(size * (0.2f + 0.2f * n2), size * (0.6f + 0.9f * n1), size * (0.2f + 0.2f * n1));
                t.localRotation = Quaternion.Euler((n1 - 0.5f) * 40f, (n2 - 0.5f) * 30f, (n2 - 0.5f) * 20f);
            }
            if (glow != null) glow.intensity = 1.8f + Mathf.PerlinNoise(now * 9f, seed) * 2.2f;
        }
    }

    // ------------------------------------------------------------ smoke

    /// A column of smoke standing over a hole: puffs born at the foot, rising, widening and thinning to nothing at
    /// the top, then born again — the same few spheres forever
    public class SmokeColumn : MonoBehaviour
    {
        Transform[] puffs;
        Renderer[] rends;
        float[] phase, drift;
        MaterialPropertyBlock block;
        float width, height, speed;
        Vector3 foot;
        Color tint;

        /// `mat` is the smoke's own unless given: the dust up a shaft is the same column in the rock's colour
        public static SmokeColumn Spawn(Transform parent, Vector3 at, float width, float height, int count = 18, float speed = 0.09f, Material mat = null)
        {
            if (mat == null) mat = Smoke;
            var go = new GameObject("SmokeColumn");
            go.transform.SetParent(parent);
            go.transform.position = at;
            var s = go.AddComponent<SmokeColumn>();
            s.foot = at;
            s.width = width;
            s.height = height;
            s.speed = speed;
            s.tint = mat.GetColor(BaseColor);
            s.block = new MaterialPropertyBlock();
            s.puffs = new Transform[count];
            s.rends = new Renderer[count];
            s.phase = new float[count];
            s.drift = new float[count];
            for (int i = 0; i < count; i++)
            {
                var p = Piece(go.transform, Vector3.zero, Vector3.one, mat, "Puff", PrimitiveType.Sphere);
                s.puffs[i] = p.transform;
                s.rends[i] = p.GetComponent<Renderer>();
                s.phase[i] = i / (float)count;
                s.drift[i] = Random.Range(0f, 6.28f);
            }
            s.Place(0f);
            return s;
        }

        void Update() => Place(Time.time * speed);

        void Place(float clock)
        {
            for (int i = 0; i < puffs.Length; i++)
            {
                float age = (clock + phase[i]) % 1f;
                float y = age * height;
                float w = width * (0.5f + 2.2f * age);
                float lean = age * age * width * 1.5f;
                puffs[i].position = foot + new Vector3(Mathf.Sin(drift[i] + age * 3f) * width * 0.3f * (1f + age) + lean * 0.3f, y, Mathf.Cos(drift[i] + age * 2f) * width * 0.3f * (1f + age) + lean);
                puffs[i].localScale = Vector3.one * w;
                puffs[i].localRotation = Quaternion.Euler(age * 40f, drift[i] * 57f + age * 90f, 0f);
                float a = tint.a * Mathf.Min(1f, age * 6f) * (1f - age * age);
                block.SetColor(BaseColor, new Color(tint.r, tint.g, tint.b, a));
                rends[i].SetPropertyBlock(block);
            }
        }
    }

    // ------------------------------------------------------------ the blast up a shaft

    /// A column of fire that climbs from `at` to `at + up * height` over `seconds`, widening as it goes, then burns
    /// off: a stack of spheres, white-hot at the front, gold behind it, dark at the tail, and a light riding the front
    public class FireColumn : MonoBehaviour
    {
        Transform[] balls;
        Renderer[] rends;
        MaterialPropertyBlock block;
        Vector3 at;
        float width, height, seconds, born;
        Light glow;
        public Vector3 Front { get; private set; }

        bool steam;

        /// `steam`: the column is a steam main's — white-hot at the front, grey behind, cream light — not fire
        public static FireColumn Spawn(Vector3 at, float width, float height, float seconds, int count = 16, bool steam = false)
        {
            var go = new GameObject(steam ? "SteamColumn" : "FireColumn") { layer = 2 };
            go.transform.position = at;
            var f = go.AddComponent<FireColumn>();
            f.at = at;
            f.width = width;
            f.height = height;
            f.seconds = Mathf.Max(0.2f, seconds);
            f.born = Time.time;
            f.steam = steam;
            f.block = new MaterialPropertyBlock();
            f.balls = new Transform[count];
            f.rends = new Renderer[count];
            for (int i = 0; i < count; i++)
            {
                var b = Piece(go.transform, Vector3.zero, Vector3.one * 0.01f, steam ? SteamWhite : Fire, "Ball", PrimitiveType.Sphere);
                b.layer = 2;
                f.balls[i] = b.transform;
                f.rends[i] = b.GetComponent<Renderer>();
            }
            f.glow = LevelKit.Light(go.transform, at, width * 6f, 12f);
            f.glow.color = steam ? new Color(1f, 0.96f, 0.9f) : new Color(1f, 0.6f, 0.25f);
            f.Front = at;
            f.Step();
            return f;
        }

        void Update() => Step();

        void Step()
        {
            float age = Time.time - born;
            float k = Mathf.Clamp01(age / seconds);
            float front = height * k; // a steady climb: the shaft's is timed to meet the cage
            float burn = Mathf.Clamp01((age - seconds) / 0.8f); // after it has climbed, it burns off
            Front = at + Vector3.up * front;
            for (int i = 0; i < balls.Length; i++)
            {
                float u = (i + 0.5f) / balls.Length;          // 0 tail, 1 front
                float y = front * u;
                float wob = 1f + 0.35f * Mathf.Sin(age * 23f + i * 1.7f);
                float w = width * (0.6f + 0.8f * u) * wob * (1f - burn);
                balls[i].position = at + new Vector3(Mathf.Sin(i * 2.1f + age * 9f) * width * 0.25f, y, Mathf.Cos(i * 1.3f + age * 7f) * width * 0.25f);
                balls[i].localScale = Vector3.one * Mathf.Max(0.01f, w);
                Color c = steam ? (u > 0.8f ? new Color(2.8f, 2.8f, 2.9f) : u > 0.35f ? new Color(1.7f, 1.7f, 1.75f) : new Color(0.55f, 0.55f, 0.58f))
                                : (u > 0.8f ? new Color(3f, 2.4f, 1.3f) : u > 0.35f ? new Color(2.6f, 1.2f, 0.3f) : new Color(0.6f, 0.25f, 0.1f));
                block.SetColor(BaseColor, Color.Lerp(c, steam ? new Color(0.3f, 0.3f, 0.32f) : new Color(0.15f, 0.08f, 0.05f), burn));
                rends[i].SetPropertyBlock(block);
            }
            if (glow != null)
            {
                glow.transform.position = Front;
                glow.intensity = 12f * (1f - burn) * (0.85f + 0.15f * Mathf.Sin(age * 40f));
            }
            if (burn >= 1f) Destroy(gameObject);
        }
    }

    // ------------------------------------------------------------ debris

    /// Whatever was standing under `parent` is thrown: every piece gets a body and a box, a velocity away from
    /// `from` and up, a spin — and lands wherever it lands, and stays
    public static void Fling(Transform parent, Vector3 from, float power)
    {
        if (parent == null) return;
        var pieces = new List<Renderer>(parent.GetComponentsInChildren<Renderer>());
        foreach (var r in pieces)
        {
            if (r == null) continue;
            var go = r.gameObject;
            go.transform.SetParent(null, true);
            if (go.GetComponent<Collider>() == null)
            {
                var box = go.AddComponent<BoxCollider>();
                box.size = Vector3.one;
            }
            var rb = go.AddComponent<Rigidbody>();
            Vector3 s = go.transform.lossyScale;
            rb.mass = Mathf.Clamp(s.x * s.y * s.z * 40f, 2f, 400f);
            Vector3 away = go.transform.position - from;
            away.y = 0f;
            Vector3 dir = (away.normalized * 0.5f + Vector3.up * 0.9f + Random.insideUnitSphere * 0.3f).normalized;
            rb.linearVelocity = dir * power * Random.Range(0.6f, 1.2f);
            rb.angularVelocity = Random.insideUnitSphere * 6f;
        }
    }

    /// Rock and slag out of the mouth: chunks thrown up from `at`, with bodies and boxes, to rain back on the yard
    public static void Throw(Vector3 at, int count, float up, float spread)
    {
        for (int i = 0; i < count; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Debris";
            go.transform.position = at + new Vector3(Random.Range(-3f, 3f), Random.Range(0f, 3f), Random.Range(-3f, 3f));
            go.transform.rotation = Random.rotation;
            float s = Random.Range(0.3f, 1.4f);
            go.transform.localScale = new Vector3(s, s * Random.Range(0.5f, 1f), s * Random.Range(0.6f, 1.2f));
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = i % 4 == 0 ? LevelKit.Iron : i % 4 == 1 ? Scorch : LevelKit.Limestone;   // the street's paving and the mains' iron
            r.shadowCastingMode = ShadowCastingMode.Off;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = s * 30f;
            Vector3 dir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
            rb.linearVelocity = dir * Random.Range(spread * 0.3f, spread) + Vector3.up * Random.Range(up * 0.5f, up);
            rb.angularVelocity = Random.insideUnitSphere * 8f;
        }
    }
}
