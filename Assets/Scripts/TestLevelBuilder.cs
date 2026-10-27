using UnityEngine;

/// <summary>
/// The HUB: an enclosed 140×140 arena that supports every mechanic at once —
/// open bhop floor, a raised central plaza with slide ramps and edge-boost lips,
/// a corner-boost pillar ring, a wall-kick canyon, a west-wall slide run,
/// a pogo pad, grapple orbs overhead, and training enemies throughout.
/// The tutorial door stands right at the spawn entrance.
/// </summary>
public static class TestLevelBuilder
{
    public static Vector3 Build(MovementConfig config)
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        Material stripeA = MakeMat(lit, new Color(0.75f, 0.75f, 0.78f));
        Material stripeB = MakeMat(lit, new Color(0.45f, 0.45f, 0.50f));
        Material wallMat = MakeMat(lit, new Color(0.80f, 0.70f, 0.50f));
        Material pillarMat = MakeMat(lit, new Color(0.90f, 0.50f, 0.20f));
        Material slopeMat = MakeMat(lit, new Color(0.55f, 0.52f, 0.46f));
        Material platMat = MakeMat(lit, new Color(0.32f, 0.08f, 0.08f));
        Material padMat = MakeMat(lit, new Color(0.95f, 0.75f, 0.25f));
        Material targetMat = MakeMat(lit, new Color(1f, 0.8f, 0.3f));

        Transform root = new GameObject("HubLevel").transform;

        // Striped floor — speed stays readable everywhere
        for (int i = 0; i < 14; i++)
            Box(root, new Vector3(0f, -0.5f, -65f + i * 10f), new Vector3(140f, 1f, 10f),
                i % 2 == 0 ? stripeA : stripeB, "Floor");

        // Perimeter walls: enclosed, and every wall is a wall-kick / wall-parry surface
        Box(root, new Vector3(0f, 6f, 70.5f), new Vector3(142f, 12f, 1f), wallMat, "WallN");
        Box(root, new Vector3(0f, 6f, -70.5f), new Vector3(142f, 12f, 1f), wallMat, "WallS");
        Box(root, new Vector3(70.5f, 6f, 0f), new Vector3(1f, 12f, 142f), wallMat, "WallE");
        Box(root, new Vector3(-70.5f, 6f, 0f), new Vector3(1f, 12f, 142f), wallMat, "WallW");

        // Central plaza: 4m high, slide ramps on all four sides, sharp lips
        // between the ramps for edge boosts — all four lips are marked
        Box(root, new Vector3(0f, 2f, 0f), new Vector3(30f, 4f, 30f), platMat, "Plaza");
        for (int i = 0; i < 4; i++)
            PlazaRamp(root, i * 90f, slopeMat);
        TechMarkers.MarkEdgeStrip(new Vector3(15f, 4f, 0f), new Vector3(1f, 0.8f, 30f));
        TechMarkers.MarkEdgeStrip(new Vector3(-15f, 4f, 0f), new Vector3(1f, 0.8f, 30f));
        TechMarkers.MarkEdgeStrip(new Vector3(0f, 4f, 15f), new Vector3(30f, 0.8f, 1f));
        TechMarkers.MarkEdgeStrip(new Vector3(0f, 4f, -15f), new Vector3(30f, 0.8f, 1f));

        // Corner-boost pillar ring — every pillar edge is an authored corner hitbox
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            GameObject pillar = Box(root, new Vector3(Mathf.Sin(a) * 38f, 4f, Mathf.Cos(a) * 38f),
                new Vector3(2f, 8f, 2f), pillarMat, "Pillar");
            TechMarkers.MarkBoxCorners(pillar);
        }

        // Wall-kick canyon (NE): 6m gap, chain kicks to climb or cross;
        // the wall ends are marked corners
        GameObject canyonL = Box(root, new Vector3(40f, 4f, 42f), new Vector3(1f, 8f, 26f), wallMat, "CanyonL");
        GameObject canyonR = Box(root, new Vector3(47f, 4f, 42f), new Vector3(1f, 8f, 26f), wallMat, "CanyonR");
        TechMarkers.MarkBoxCorners(canyonL);
        TechMarkers.MarkBoxCorners(canyonR);

        // West slide run: high ledge into a long descent — wall-kick up the gap
        // beside the west wall (or grapple the orb) to reach the top
        Box(root, new Vector3(-61f, 7.5f, 47.5f), new Vector3(12f, 1f, 25f), platMat, "SlideLedge");
        TechMarkers.MarkEdgeStrip(new Vector3(-55f, 8f, 47.5f), new Vector3(1f, 0.8f, 25f)); // ledge lip
        GameObject run = Box(root, new Vector3(-61f, 3.75f, 15f), new Vector3(12f, 1f, 40.8f), slopeMat, "SlideRun");
        run.transform.rotation = Quaternion.Euler(-11.3f, 0f, 0f);

        // Pogo pad (SE): grapple the orb above, release, slam, parry-pogo
        Box(root, new Vector3(40f, 0.1f, -40f), new Vector3(14f, 0.2f, 14f), padMat, "PogoPad");

        // THE TRICK STATION (2026-10-23): a pad south of the pogo pad that throws you six metres up, and a sign that
        // says when. At the top the prompt opens (the brass ring closing onto the crosshair); W/S/A/D or nothing picks
        // the flip; the press as the ring meets the crosshair is PERFECT and pays on the landing. 17.4 m/s up under
        // gravity 25 is 6.05 m: enough air for one flip pressed on the beat, a stumble for one pressed late
        LevelKit.JumpPad(root, new Vector3(40f, 0f, -56f), 0f, 17.4f, 2f);
        Material signMat = MakeMat(lit, new Color(0.12f, 0.1f, 0.09f));
        Box(root, new Vector3(40f, 2.4f, -63f), new Vector3(7.5f, 1.6f, 0.15f), signMat, "TrickSign");
        LevelKit.Text(root, new Vector3(40f, 2.4f, -62.9f), Quaternion.LookRotation(Vector3.back),
            "TRICK  ·  click in the air, click again at the beat\nW flip  ·  S backflip  ·  A/D side  ·  nothing = spin", 0.11f, new Color(2.2f, 1.7f, 0.4f), "TrickSignText", 7f);

        // Grapple orbs threading the airspace
        Orb(root, new Vector3(0f, 14f, 0f), targetMat);
        Orb(root, new Vector3(-61f, 14f, 47f), targetMat);
        Orb(root, new Vector3(40f, 14f, -40f), targetMat);
        Orb(root, new Vector3(20f, 12f, 55f), targetMat);
        Orb(root, new Vector3(-30f, 12f, -50f), targetMat);

        // Training dummies: bodies to jab, to kick, and to springboard off; the walls and the pogo pad are the parry's
        Enemy.CreateDummy(new Vector3(20f, 0f, -20f), config);
        Enemy.CreateDummy(new Vector3(-25f, 0f, 15f), config);
        Enemy.CreateDummy(new Vector3(55f, 0f, 10f), config);
        Enemy.CreateDummy(new Vector3(5f, 4f, 5f), config); // on the plaza
        Enemy.CreateDummy(new Vector3(-40f, 0f, -25f), config);
        Enemy.CreateTurret(new Vector3(30f, 8f, 25f), config);
        Enemy.CreateTurret(new Vector3(-35f, 7f, 40f), config);
        Enemy.CreateTurret(new Vector3(50f, 7f, -30f), config);
        Enemy.CreateChaser(new Vector3(15f, 0f, -25f), config);
        Enemy.CreateChaser(new Vector3(-20f, 0f, -10f), config);

        return new Vector3(0f, 1.5f, -62f); // spawn at the south entrance
    }

    static void PlazaRamp(Transform root, float yaw, Material mat)
    {
        Quaternion facing = Quaternion.AngleAxis(yaw, Vector3.up);
        GameObject ramp = Box(root, facing * (Vector3.forward * 22.4f) + Vector3.up * 1.74f,
            new Vector3(12f, 0.5f, 15.9f), mat, "PlazaRamp");
        ramp.transform.rotation = facing * Quaternion.Euler(15f, 0f, 0f);
    }

    /// The range's grapple points are the company's drones too (`mat` is what the old gold ball wore: the drone brings its own)
    static void Orb(Transform root, Vector3 pos, Material mat) => GrappleDrone.Build(root, pos);

    static GameObject Box(Transform parent, Vector3 pos, Vector3 scale, Material mat, string name)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static Material MakeMat(Shader shader, Color color)
    {
        var mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        return mat;
    }
}
