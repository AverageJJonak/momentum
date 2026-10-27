using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE PLANT, DARK DECO (the machine vocabulary THE UNDERGROUND draws on for its pipe galleries, gauges, valves, steam
/// vents, wall fans, beacons, trusses, hoists and control desks; the foundry's own machines stay here unused by Act II).
/// What makes the Works a factory and not a cave: machines that move, in 1930s iron. Presses that stamp (and crush what is
/// under them), brick furnaces with glowing mouths and flues, runners of molten
/// metal on the floor that burn anyone standing in them, ladles crossing
/// overhead on rails, arms working the lines, gantry cranes crossing the halls
/// with a load under them, turbines with their fans turning, wall fans, klaxon
/// beacons, steam vents on a timer, brick chimneys, riveted bulkheads, pipe
/// galleries with brass valves and cream dials, lattice trusses under the roof,
/// and ore and stamped parts riding the belts. Hazard marks are black and
/// mustard, lamps are amber, red is the ram coming down.
/// Everything is decor (no colliders) except what you can walk into
/// or onto: the turbine casings, saddles and decks, the furnace bodies and
/// bases, the press columns and feet, the control desks. The moving parts and
/// the hazards are checked, never collided. One FactoryLife per level drives all of
/// it from flat lists in a single Update — and it stops with hitstop.
/// </summary>
public static partial class LevelKit
{
    static Material moltenMat, furnaceMat, panelMat, rivetMat, beaconMat, pressMat, ramLampMat, ramRedMat, gaugeMat, steamMat, partMat;
    static FactoryLife factoryLife;

    static void EnsureFactory()
    {
        if (moltenMat != null) return;
        EnsureWorks();
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        moltenMat = Unlit(unlit, new Color(2.6f, 0.5f, 0.12f));     // molten metal: red-orange — danger
        furnaceMat = Unlit(unlit, new Color(2.2f, 0.95f, 0.3f));    // a furnace mouth: the machine's own light
        beaconMat = Unlit(unlit, new Color(2.4f, 1.3f, 0.25f));     // a klaxon's amber
        panelMat = Lit(lit, new Color(0.19f, 0.19f, 0.21f), 0.5f); panelMat.SetFloat("_Metallic", 0.7f);
        rivetMat = Lit(lit, new Color(0.14f, 0.14f, 0.16f), 0.6f); rivetMat.SetFloat("_Metallic", 0.8f);
        pressMat = Lit(lit, new Color(0.1f, 0.1f, 0.12f), 0.45f); pressMat.SetFloat("_Metallic", 0.75f);   // boiler black
        ramLampMat = Unlit(unlit, new Color(0.3f, 0.28f, 0.27f));   // the ram's lamp, dark
        ramRedMat = Unlit(unlit, new Color(2.4f, 0.25f, 0.15f));    // and lit: red — the ram is coming down
        gaugeMat = Unlit(unlit, new Color(1.9f, 1.7f, 1.3f));       // a dial's cream face, lit
        partMat = Lit(lit, new Color(0.5f, 0.47f, 0.42f), 0.7f); partMat.SetFloat("_Metallic", 0.9f);
    }

    public static Material Molten { get { EnsureFactory(); return moltenMat; } }

    /// The one animator for every moving machine in the level (made on first use, forgotten at Init)
    public static FactoryLife Life(Transform parent)
    {
        if (factoryLife != null) return factoryLife;
        var go = new GameObject("FactoryLife");
        go.transform.SetParent(parent);
        factoryLife = go.AddComponent<FactoryLife>();
        return factoryLife;
    }

    /// A lattice girder between two points: top and bottom chords, posts and diagonals (decor)
    public static void Truss(Transform parent, Vector3 a, Vector3 b, float depth = 1.2f)
    {
        EnsureFactory();
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 2f) return;
        Vector3 dir = d / len;
        Quaternion rot = Quaternion.LookRotation(dir);
        Vector3 mid = (a + b) * 0.5f;
        Decor(parent, PrimitiveType.Cube, mid, rot, new Vector3(0.3f, 0.18f, len), girderMat, "TrussTop");
        Decor(parent, PrimitiveType.Cube, mid + Vector3.down * depth, rot, new Vector3(0.3f, 0.18f, len), girderMat, "TrussBottom");
        int bays = Mathf.Max(1, Mathf.RoundToInt(len / 2.4f));
        float bay = len / bays;
        float diag = Mathf.Sqrt(bay * bay + depth * depth);
        float ang = Mathf.Atan2(bay, depth) * Mathf.Rad2Deg;
        for (int i = 0; i < bays; i++)
        {
            Vector3 c = a + dir * (bay * (i + 0.5f)) + Vector3.down * (depth * 0.5f);
            float sign = i % 2 == 0 ? 1f : -1f;
            Decor(parent, PrimitiveType.Cube, c, rot * Quaternion.Euler(sign * ang, 0f, 0f), new Vector3(0.1f, diag, 0.1f), Iron, "TrussDiag");
            Decor(parent, PrimitiveType.Cube, a + dir * (bay * i) + Vector3.down * (depth * 0.5f), rot, new Vector3(0.1f, depth, 0.1f), Iron, "TrussPost");
        }
        Decor(parent, PrimitiveType.Cube, b + Vector3.down * (depth * 0.5f), rot, new Vector3(0.1f, depth, 0.1f), Iron, "TrussPost");
    }

    /// A riveted steel panel standing off a wall, framed, with its bay number stencilled on (decor)
    public static void Bulkhead(Transform parent, Vector3 wallBase, Vector3 facing, float width, float height, string label = null)
    {
        EnsureFactory();
        Vector3 n = facing.normalized;
        Quaternion rot = Quaternion.LookRotation(n);
        Vector3 right = rot * Vector3.right;
        Vector3 c = wallBase + n * 0.09f + Vector3.up * (height * 0.5f);
        Decor(parent, PrimitiveType.Cube, c, rot, new Vector3(width, height, 0.12f), panelMat, "Bulkhead");
        Decor(parent, PrimitiveType.Cube, c + right * (width * 0.5f - 0.12f) + n * 0.08f, rot, new Vector3(0.24f, height, 0.1f), rivetMat, "BulkheadRib");
        Decor(parent, PrimitiveType.Cube, c - right * (width * 0.5f - 0.12f) + n * 0.08f, rot, new Vector3(0.24f, height, 0.1f), rivetMat, "BulkheadRib");
        Decor(parent, PrimitiveType.Cube, c + Vector3.up * (height * 0.5f - 0.12f) + n * 0.08f, rot, new Vector3(width, 0.24f, 0.1f), rivetMat, "BulkheadRib");
        Decor(parent, PrimitiveType.Cube, c - Vector3.up * (height * 0.5f - 0.12f) + n * 0.08f, rot, new Vector3(width, 0.24f, 0.1f), rivetMat, "BulkheadRib");
        Decor(parent, PrimitiveType.Cube, c + right * (width * 0.5f - 0.36f) + n * 0.15f, rot, new Vector3(0.06f, height - 0.7f, 0.03f), railMat, "RivetRow");
        Decor(parent, PrimitiveType.Cube, c - right * (width * 0.5f - 0.36f) + n * 0.15f, rot, new Vector3(0.06f, height - 0.7f, 0.03f), railMat, "RivetRow");
        if (!string.IsNullOrEmpty(label))
            Stencil(parent, c + n * 0.16f + Vector3.up * (height * 0.16f), n, label, 0.08f, width - 1f);
    }

    /// A round dial with a needle, facing `facing` (decor)
    public static void Gauge(Transform parent, Vector3 pos, Vector3 facing)
    {
        EnsureFactory();
        Vector3 n = facing.normalized;
        Quaternion face = Quaternion.LookRotation(n);
        Quaternion axial = face * Quaternion.Euler(90f, 0f, 0f);
        Decor(parent, PrimitiveType.Cylinder, pos, axial, new Vector3(0.34f, 0.03f, 0.34f), Brass, "GaugeRim");
        Decor(parent, PrimitiveType.Cylinder, pos + n * 0.025f, axial, new Vector3(0.27f, 0.02f, 0.27f), gaugeMat, "GaugeFace");
        Decor(parent, PrimitiveType.Cube, pos + n * 0.05f + face * new Vector3(-0.03f, 0.05f, 0f), face * Quaternion.Euler(0f, 0f, 35f), new Vector3(0.02f, 0.2f, 0.01f), Charcoal, "GaugeNeedle");
    }

    /// Pipes stacked up a wall from `a` to `b`, bracketed, with a valve and a gauge every so often (decor)
    public static void PipeGallery(Transform parent, Vector3 a, Vector3 b, Vector3 facing, int count = 3)
    {
        EnsureFactory();
        Vector3 n = facing.normalized;
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 2f) return;
        Vector3 dir = d / len;
        for (int i = 0; i < count; i++)
        {
            float thick = i == 0 ? 0.42f : i == 1 ? 0.3f : 0.2f;
            Vector3 off = n * (0.35f + i * 0.04f) + Vector3.up * (i * 0.62f);
            Pipe(parent, a + off, b + off, thick);
        }
        Quaternion rot = Quaternion.LookRotation(n);
        for (float s = 3f; s < len - 1f; s += 6f)
            Decor(parent, PrimitiveType.Cube, a + dir * s + n * 0.3f + Vector3.up * ((count - 1) * 0.31f), rot, new Vector3(0.6f, (count - 1) * 0.62f + 0.7f, 0.14f), Iron, "PipeBracket");
        for (float s = 5f; s < len - 3f; s += 10f)
        {
            Vector3 p = a + dir * s + n * 0.39f + Vector3.up * (count > 1 ? 0.62f : 0f);
            Valve(parent, p, dir);
            Gauge(parent, p + dir * 1.3f + n * 0.3f, n);
        }
    }

    /// A klaxon beacon: an amber lamp turning under a hood on a base, and its light pulsing (decor + light)
    public static void Beacon(Transform parent, Vector3 pos, float phase = 0f)
    {
        EnsureFactory();
        var life = Life(parent);
        Decor(parent, PrimitiveType.Cylinder, pos + Vector3.up * 0.08f, Quaternion.identity, new Vector3(0.36f, 0.08f, 0.36f), Iron, "BeaconBase");
        var pivot = new GameObject("BeaconPivot").transform;
        pivot.SetParent(parent);
        pivot.position = pos + Vector3.up * 0.32f;
        Decor(pivot, PrimitiveType.Cylinder, pivot.position, Quaternion.identity, new Vector3(0.26f, 0.16f, 0.26f), beaconMat, "BeaconLamp");
        Decor(pivot, PrimitiveType.Cube, pivot.position + Vector3.back * 0.1f, Quaternion.identity, new Vector3(0.3f, 0.34f, 0.1f), Iron, "BeaconHood");
        Decor(parent, PrimitiveType.Cylinder, pos + Vector3.up * 0.52f, Quaternion.identity, new Vector3(0.3f, 0.02f, 0.3f), Iron, "BeaconCap");
        var l = Light(parent, pos + Vector3.up * 0.6f, 9f, 1.4f);
        l.color = new Color(1f, 0.62f, 0.15f);
        life.AddSpin(pivot, Vector3.up, 200f);
        life.AddPulse(l, 0.8f, 2.2f, phase);
    }

    /// A fan in a wall: a housing, six blades on a hub that turns, a grille over it (decor)
    public static void WallFan(Transform parent, Vector3 wallPoint, Vector3 facing, float radius = 1.6f)
    {
        EnsureFactory();
        Vector3 n = facing.normalized;
        Quaternion face = Quaternion.LookRotation(n);
        Quaternion axial = face * Quaternion.Euler(90f, 0f, 0f);
        Vector3 c = wallPoint + n * 0.3f;
        Decor(parent, PrimitiveType.Cylinder, c, axial, new Vector3(radius * 2.2f, 0.25f, radius * 2.2f), Iron, "FanHousing");
        Decor(parent, PrimitiveType.Cylinder, c + n * 0.06f, axial, new Vector3(radius * 2f, 0.2f, radius * 2f), Graphite, "FanThroat");
        var hub = new GameObject("FanHub").transform;
        hub.SetParent(parent);
        hub.position = c + n * 0.24f;
        hub.rotation = face;
        Decor(hub, PrimitiveType.Cylinder, hub.position, axial, new Vector3(0.4f, 0.12f, 0.4f), railMat, "FanHub");
        for (int i = 0; i < 3; i++)
            Decor(hub, PrimitiveType.Cube, hub.position, face * Quaternion.Euler(0f, 0f, i * 60f) * Quaternion.Euler(0f, 28f, 0f), new Vector3(0.34f, radius * 1.75f, 0.05f), panelMat, "FanBlade");
        for (int i = 0; i < 3; i++)
            Decor(parent, PrimitiveType.Cube, c + n * 0.42f, face * Quaternion.Euler(0f, 0f, i * 60f), new Vector3(0.07f, radius * 2.2f, 0.05f), Iron, "FanGrille");
        Life(parent).AddSpin(hub, Vector3.forward, 240f);
    }

    /// A furnace against a wall: a block with a glowing mouth on its facing side, gauges, a pipe in, its light breathing,
    /// sparks out of the mouth, and a flue up to `flueTop` that bends back into the wall (decor + light)
    public static void Furnace(Transform parent, Vector3 groundPos, Vector3 facing, float width = 5f, float height = 6f, float flueTop = 0f)
    {
        EnsureFactory();
        Vector3 n = facing.normalized;
        Quaternion rot = Quaternion.LookRotation(n);
        Vector3 right = rot * Vector3.right;
        float depth = width * 0.7f;
        Vector3 c = groundPos + Vector3.up * (height * 0.5f);
        Solid(parent, PrimitiveType.Cube, c, rot, new Vector3(width, height, depth), Brick, "FurnaceBody");   // a brick furnace, iron-bound
        Decor(parent, PrimitiveType.Cube, c + Vector3.up * (height * 0.5f - 0.3f), rot, new Vector3(width + 0.3f, 0.6f, depth + 0.3f), rivetMat, "FurnaceCrown");
        Decor(parent, PrimitiveType.Cube, c + Vector3.up * (height * 0.5f - 0.9f), rot, new Vector3(width + 0.1f, 0.4f, depth + 0.1f), sootMat, "FurnaceSoot");
        Solid(parent, PrimitiveType.Cube, groundPos + Vector3.up * 0.3f, rot, new Vector3(width + 0.4f, 0.6f, depth + 0.4f), Iron, "FurnaceBase");
        Decor(parent, PrimitiveType.Cube, c, rot, new Vector3(width + 0.06f, 0.35f, depth + 0.06f), Iron, "FurnaceBand");
        Decor(parent, PrimitiveType.Cube, c + Vector3.down * (height * 0.22f), rot, new Vector3(width + 0.06f, 0.2f, depth + 0.06f), Iron, "FurnaceBand");
        Vector3 mouth = groundPos + n * (depth * 0.5f) + Vector3.up * (height * 0.36f);
        Decor(parent, PrimitiveType.Cube, mouth + n * 0.1f, rot, new Vector3(width * 0.55f, height * 0.4f, 0.2f), Charcoal, "MouthFrame");
        Decor(parent, PrimitiveType.Cube, mouth + n * 0.21f, rot, new Vector3(width * 0.45f, height * 0.3f, 0.02f), furnaceMat, "MouthGlow");
        Decor(parent, PrimitiveType.Cube, mouth + n * 0.24f + Vector3.up * (height * 0.17f), rot, new Vector3(width * 0.5f, 0.12f, 0.06f), Iron, "MouthLintel");
        Gauge(parent, mouth + right * (width * 0.36f) + Vector3.up * (height * 0.3f) + n * 0.04f, n);
        Gauge(parent, mouth - right * (width * 0.36f) + Vector3.up * (height * 0.3f) + n * 0.04f, n);
        Pipe(parent, groundPos + right * (width * 0.5f) + Vector3.up * 1.4f, groundPos + right * (width * 0.5f + 2.2f) + Vector3.up * 1.4f, 0.3f);
        if (flueTop > groundPos.y + height + 0.5f)
        {
            Vector3 flueBase = groundPos + Vector3.up * height - n * (depth * 0.2f);
            Vector3 flueHead = new Vector3(flueBase.x, flueTop, flueBase.z);
            Decor(parent, PrimitiveType.Cube, (flueBase + flueHead) * 0.5f, rot, new Vector3(1.4f, flueTop - flueBase.y, 1.4f), Iron, "Flue");
            Decor(parent, PrimitiveType.Cube, flueBase + Vector3.up * 0.4f, rot, new Vector3(1.5f, 0.2f, 1.5f), rivetMat, "FlueCollar");
            Duct(parent, flueHead - Vector3.up * 0.7f, flueHead - Vector3.up * 0.7f - n * (depth * 0.3f + 0.8f), 1.3f, 1.3f); // back into the wall
        }
        var l = Light(parent, mouth + n * 1.2f, width * 2.6f, 2.6f);
        l.color = new Color(1f, 0.55f, 0.2f);
        Life(parent).AddPulse(l, 0.35f, 6f, groundPos.x * 0.37f + groundPos.z * 0.11f);
        Embers(parent, mouth + n * 0.5f + Vector3.up * (height * 0.05f));
    }

    /// A runner of molten metal in the floor between two points: a kerb, the melt, hazard lips, its light — and it burns anyone in it
    public static void MoltenChannel(Transform parent, Vector3 from, Vector3 to, float width = 2.4f, bool hazard = true)
    {
        EnsureFactory();
        Vector3 d = to - from;
        float len = d.magnitude;
        if (len < 2f) return;
        Quaternion rot = Quaternion.LookRotation(d / len);
        Vector3 mid = (from + to) * 0.5f, side = rot * Vector3.right;
        Decor(parent, PrimitiveType.Cube, mid + Vector3.up * 0.1f, rot, new Vector3(width + 0.9f, 0.2f, len + 0.9f), rivetMat, "ChannelBed");
        Decor(parent, PrimitiveType.Cube, mid + Vector3.up * 0.21f, rot, new Vector3(width, 0.04f, len), moltenMat, "Molten");
        for (int s = -1; s <= 1; s += 2)
        {
            Decor(parent, PrimitiveType.Cube, mid + side * (s * (width * 0.5f + 0.2f)) + Vector3.up * 0.3f, rot, new Vector3(0.4f, 0.2f, len + 0.9f), Iron, "ChannelLip");
            Stripes(parent, mid + side * (s * (width * 0.5f + 0.2f)) + Vector3.up * 0.41f, new Vector3(0.42f, 0.02f, len + 0.9f), rot.eulerAngles.y);
        }
        var life = Life(parent);
        int lamps = Mathf.Max(1, Mathf.RoundToInt(len / 7f));
        for (int i = 0; i < lamps; i++)
        {
            var l = Light(parent, from + d * ((i + 0.5f) / lamps) + Vector3.up * 1.2f, Mathf.Max(6f, width * 3f), 2.2f);
            l.color = new Color(1f, 0.5f, 0.15f);
            life.AddPulse(l, 0.25f, 5f, i * 1.7f);
        }
        if (hazard) life.AddHot(mid + Vector3.up * 0.4f, rot, new Vector3(width, 1.2f, len));
    }

    /// A ladle on an overhead rail, crossing from `from` to `to` and back, the melt glowing in it (decor + light)
    public static void LadleRail(Transform parent, Vector3 from, Vector3 to, float hang = 3.5f, float period = 14f, float phase = 0f)
    {
        EnsureFactory();
        Vector3 d = to - from;
        float len = d.magnitude;
        if (len < 4f) return;
        Quaternion rot = Quaternion.LookRotation(d / len);
        Beam(parent, from, to, 0.5f, 0.7f, girderMat);
        var carriage = new GameObject("LadleCarriage").transform;
        carriage.SetParent(parent);
        carriage.position = from;
        carriage.rotation = rot;
        Decor(carriage, PrimitiveType.Cube, from + Vector3.down * 0.6f, rot, new Vector3(1.4f, 0.5f, 1.8f), Iron, "Trolley");
        Decor(carriage, PrimitiveType.Cube, from + Vector3.down * 0.9f, rot, new Vector3(0.9f, 0.1f, 1.2f), MustardPaint, "TrolleyBand");
        Vector3 hook = from + Vector3.down * hang;
        Decor(carriage, PrimitiveType.Cube, (from + Vector3.down * 0.95f + hook) * 0.5f, Quaternion.identity, new Vector3(0.08f, hang - 0.95f, 0.08f), railMat, "LadleChain");
        Decor(carriage, PrimitiveType.Cylinder, hook + Vector3.down * 0.5f, Quaternion.identity, new Vector3(1.8f, 0.5f, 1.8f), Iron, "LadleBowl");
        Decor(carriage, PrimitiveType.Cylinder, hook + Vector3.down * 0.02f, Quaternion.identity, new Vector3(1.9f, 0.06f, 1.9f), rivetMat, "LadleLip");
        Decor(carriage, PrimitiveType.Cylinder, hook + Vector3.up * 0.02f, Quaternion.identity, new Vector3(1.5f, 0.02f, 1.5f), moltenMat, "LadleMelt");
        Decor(carriage, PrimitiveType.Cube, hook + rot * new Vector3(1f, -0.2f, 0f), rot, new Vector3(0.5f, 0.2f, 0.4f), Iron, "LadleSpout");
        var l = Light(carriage, hook + Vector3.up * 0.6f, 8f, 2f);
        l.color = new Color(1f, 0.5f, 0.15f);
        Life(parent).AddSlide(carriage, from, to, period, phase);
    }

    /// An industrial arm on a base: the turret turns, the shoulder and elbow nod, the tool rolls — all on their own periods (decor)
    public static void RobotArm(Transform parent, Vector3 groundPos, float yaw, float reach = 3.5f, float period = 5f, float phase = 0f)
    {
        EnsureFactory();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Decor(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * 0.25f, Quaternion.identity, new Vector3(1.6f, 0.25f, 1.6f), Iron, "ArmBase");
        Decor(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * 0.55f, Quaternion.identity, new Vector3(1.62f, 0.04f, 1.62f), MustardPaint, "ArmBaseBand");
        var turret = new GameObject("ArmTurret").transform;
        turret.SetParent(parent);
        turret.position = groundPos + Vector3.up * 0.5f;
        turret.rotation = rot;
        Decor(turret, PrimitiveType.Cube, turret.position + Vector3.up * 0.45f, rot, new Vector3(1f, 0.9f, 1.2f), girderMat, "ArmTurret");
        var shoulder = new GameObject("ArmShoulder").transform;
        shoulder.SetParent(turret);
        shoulder.position = groundPos + Vector3.up * 1.4f;
        shoulder.rotation = rot;
        float upper = reach * 0.55f, lower = reach * 0.5f;
        Decor(shoulder, PrimitiveType.Cube, shoulder.position + Vector3.up * (upper * 0.5f), rot, new Vector3(0.5f, upper, 0.6f), girderMat, "UpperArm");
        Decor(shoulder, PrimitiveType.Cylinder, shoulder.position, rot * Quaternion.Euler(0f, 0f, 90f), new Vector3(0.8f, 0.4f, 0.8f), Iron, "ShoulderJoint");
        var elbow = new GameObject("ArmElbow").transform;
        elbow.SetParent(shoulder);
        elbow.position = shoulder.position + Vector3.up * upper;
        elbow.rotation = rot;
        Decor(elbow, PrimitiveType.Cylinder, elbow.position, rot * Quaternion.Euler(0f, 0f, 90f), new Vector3(0.62f, 0.34f, 0.62f), Iron, "ElbowJoint");
        Decor(elbow, PrimitiveType.Cube, elbow.position + rot * new Vector3(0f, 0f, lower * 0.5f), rot, new Vector3(0.36f, 0.42f, lower), girderMat, "Forearm");
        var wrist = new GameObject("ArmWrist").transform;
        wrist.SetParent(elbow);
        wrist.position = elbow.position + rot * new Vector3(0f, 0f, lower);
        wrist.rotation = rot;
        Decor(wrist, PrimitiveType.Cube, wrist.position + rot * new Vector3(0f, 0f, 0.25f), rot, new Vector3(0.5f, 0.3f, 0.5f), Iron, "Wrist");
        Decor(wrist, PrimitiveType.Cube, wrist.position + rot * new Vector3(0.18f, -0.3f, 0.45f), rot, new Vector3(0.08f, 0.5f, 0.3f), railMat, "Finger");
        Decor(wrist, PrimitiveType.Cube, wrist.position + rot * new Vector3(-0.18f, -0.3f, 0.45f), rot, new Vector3(0.08f, 0.5f, 0.3f), railMat, "Finger");
        Decor(wrist, PrimitiveType.Cube, wrist.position + rot * new Vector3(0f, 0.1f, 0.52f), rot, new Vector3(0.12f, 0.08f, 0.08f), beaconMat, "ToolLamp");
        Cable(parent, turret.position + Vector3.up * 0.9f - rot * Vector3.forward * 0.6f, shoulder.position + Vector3.up * 0.8f - rot * Vector3.forward * 0.6f, 0.06f);
        var life = Life(parent);
        life.AddSwing(turret, Vector3.up, 55f, period * 1.7f, phase);
        life.AddSwing(shoulder, Vector3.right, 28f, period, phase + 0.2f);
        life.AddSwing(elbow, Vector3.right, 40f, period * 0.8f, phase + 0.45f);
        life.AddSwing(wrist, Vector3.forward, 90f, period * 0.6f, phase);
    }

    /// A hydraulic press straddling a line: two columns, a crown, and the ram that comes down on the deck at `bedY` on a
    /// cycle — the lamp on its face goes red first; anything under it when it lands is hit (decor + hazard)
    public static void Press(Transform parent, Vector3 groundCenter, float yaw, float width = 5f, float height = 7f, float period = 3.2f, float phase = 0f, float bedY = float.NaN)
    {
        EnsureFactory();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Vector3 right = rot * Vector3.right;
        const float colW = 0.9f;
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 col = groundCenter + right * (s * (width * 0.5f)) + Vector3.up * (height * 0.5f);
            Solid(parent, PrimitiveType.Cube, col, rot, new Vector3(colW, height, 1.6f), pressMat, "PressColumn");
            Stripes(parent, col + rot * new Vector3(0f, 0f, 0.82f), new Vector3(colW + 0.06f, 0.3f, 0.02f), yaw);
            Stripes(parent, col - rot * new Vector3(0f, 0f, 0.82f), new Vector3(colW + 0.06f, 0.3f, 0.02f), yaw);
            Decor(parent, PrimitiveType.Cube, col + Vector3.up * (height * 0.5f - 0.4f), rot, new Vector3(colW + 0.08f, 0.2f, 1.68f), Brass, "PressColumnCollar");
            Solid(parent, PrimitiveType.Cube, groundCenter + right * (s * (width * 0.5f)) + Vector3.up * 0.25f, rot, new Vector3(colW + 0.5f, 0.5f, 2.2f), Iron, "PressFoot");
        }
        Vector3 crown = groundCenter + Vector3.up * (height - 0.6f);
        Decor(parent, PrimitiveType.Cube, crown, rot, new Vector3(width + colW, 1.2f, 1.8f), pressMat, "PressCrown");
        Decor(parent, PrimitiveType.Cube, crown + Vector3.up * 0.8f, rot, new Vector3(width * 0.4f, 0.4f, 1.2f), rivetMat, "PressHead");
        Decor(parent, PrimitiveType.Cylinder, crown + Vector3.up * 1.45f, Quaternion.identity, new Vector3(0.9f, 0.5f, 0.9f), Iron, "PressAccumulator");
        Gauge(parent, crown + rot * new Vector3(width * 0.3f, 0f, 0.92f), rot * Vector3.forward);
        float ramH = 1.4f, ramW = width - colW - 0.4f;
        float bed = float.IsNaN(bedY) ? groundCenter.y : bedY;
        float top = crown.y - 0.6f - ramH * 0.5f - 0.1f;
        float bottom = bed + ramH * 0.5f + 0.06f;
        var ram = new GameObject("PressRam").transform;
        ram.SetParent(parent);
        ram.position = new Vector3(groundCenter.x, top, groundCenter.z);
        ram.rotation = rot;
        Decor(ram, PrimitiveType.Cube, ram.position, rot, new Vector3(ramW, ramH, 1.5f), pressMat, "Ram");
        Decor(ram, PrimitiveType.Cube, ram.position + Vector3.down * (ramH * 0.5f - 0.1f), rot, new Vector3(ramW + 0.04f, 0.2f, 1.54f), MustardPaint, "RamBand");
        Decor(ram, PrimitiveType.Cube, ram.position + Vector3.up * (ramH * 0.5f + 1.1f), rot, new Vector3(0.6f, 2.2f, 0.6f), railMat, "Piston");
        var lamp = Decor(ram, PrimitiveType.Cube, ram.position + rot * new Vector3(0f, 0.2f, 0.78f), rot, new Vector3(ramW * 0.6f, 0.16f, 0.04f), ramLampMat, "RamLamp").GetComponent<Renderer>();
        Life(parent).AddStamp(ram, lamp, top, bottom, period, phase, new Vector2(ramW * 0.5f, 0.75f), rot, bed, ramLampMat, ramRedMat);
        var l = Light(parent, crown + Vector3.down * 2f, width * 2f, 1.4f);
        l.color = new Color(1f, 0.86f, 0.64f);
    }

    /// An overhead travelling crane: rails along both walls at `height`, a truss bridge that crosses the hall from `z0` to
    /// `z1` and back, a trolley on it at `trolleyX` with a hook and a bucket of ore hanging, a beacon on the trolley (decor)
    public static void GantryCrane(Transform parent, float halfW, float y, float z0, float z1, float height, float period = 22f, float phase = 0f, float trolleyX = 0f)
    {
        EnsureFactory();
        var life = Life(parent);
        float railY = y + height;
        Beam(parent, new Vector3(-halfW + 0.6f, railY, z0 - 2f), new Vector3(-halfW + 0.6f, railY, z1 + 2f), 0.5f, 0.6f, girderMat);
        Beam(parent, new Vector3(halfW - 0.6f, railY, z0 - 2f), new Vector3(halfW - 0.6f, railY, z1 + 2f), 0.5f, 0.6f, girderMat);
        for (float z = z0; z <= z1; z += 8f)
            for (int s = -1; s <= 1; s += 2)
                Decor(parent, PrimitiveType.Cube, new Vector3(s * (halfW - 0.35f), railY - 0.6f, z), Quaternion.identity, new Vector3(0.7f, 0.5f, 0.5f), Iron, "RailBracket");
        var bridge = new GameObject("GantryBridge").transform;
        bridge.SetParent(parent);
        bridge.position = new Vector3(0f, railY, z0);
        Truss(bridge, bridge.position + Vector3.left * (halfW - 0.9f) + Vector3.up * 0.6f, bridge.position + Vector3.right * (halfW - 0.9f) + Vector3.up * 0.6f, 1.3f);
        for (int s = -1; s <= 1; s += 2)
            Decor(bridge, PrimitiveType.Cube, bridge.position + Vector3.right * (s * (halfW - 0.9f)), Quaternion.identity, new Vector3(1.6f, 0.8f, 2.4f), Iron, "EndTruck");
        Vector3 trolley = bridge.position + Vector3.right * trolleyX + Vector3.down * 0.9f;
        Decor(bridge, PrimitiveType.Cube, trolley, Quaternion.identity, new Vector3(2f, 0.7f, 1.6f), Iron, "Trolley");
        Decor(bridge, PrimitiveType.Cube, trolley + Vector3.down * 0.4f, Quaternion.identity, new Vector3(1.2f, 0.1f, 1f), MustardPaint, "TrolleyBand");
        Decor(bridge, PrimitiveType.Cube, trolley + Vector3.down * 2.2f, Quaternion.identity, new Vector3(0.08f, 3.2f, 0.08f), railMat, "HoistChain");
        Decor(bridge, PrimitiveType.Cube, trolley + Vector3.down * 4.1f, Quaternion.identity, new Vector3(0.5f, 0.7f, 0.2f), railMat, "Hook");
        Decor(bridge, PrimitiveType.Cube, trolley + Vector3.down * 5.5f, Quaternion.identity, new Vector3(2.4f, 1.8f, 2.2f), girderMat, "Bucket");
        Decor(bridge, PrimitiveType.Sphere, trolley + Vector3.down * 4.7f, Quaternion.identity, new Vector3(2.2f, 0.6f, 2f), rockMat, "BucketOre");
        Beacon(bridge, trolley + Vector3.up * 0.35f, phase);
        life.AddSlide(bridge, new Vector3(0f, railY, z0), new Vector3(0f, railY, z1), period, phase);
    }

    /// A steam vent: a stub of pipe with a flange, blowing a plume along `dir` for part of every cycle (decor)
    public static void SteamVent(Transform parent, Vector3 pos, Vector3 dir, float period = 6f, float phase = 0f, float burst = 0.3f)
    {
        EnsureFactory();
        Vector3 n = dir.normalized;
        Quaternion axial = Quaternion.LookRotation(n) * Quaternion.Euler(90f, 0f, 0f);
        Decor(parent, PrimitiveType.Cylinder, pos - n * 0.3f, axial, new Vector3(0.36f, 0.3f, 0.36f), Iron, "VentStub");
        Decor(parent, PrimitiveType.Cylinder, pos - n * 0.05f, axial, new Vector3(0.46f, 0.04f, 0.46f), rivetMat, "VentFlange");
        Decor(parent, PrimitiveType.Cylinder, pos - n * 0.1f, axial, new Vector3(0.3f, 0.06f, 0.3f), Graphite, "VentThroat");
        var ps = Steam(parent, pos + n * 0.2f, n * 5f + Vector3.up * 1.2f, 45f, 1.6f, 0.5f, 1.4f);
        if (ps != null) Life(parent).AddVent(ps, period, phase, burst);
    }

    /// A chimney: tall brick, iron-banded, soot round its mouth, a stone cap and a red lamp, a plume off the top (decor + particles)
    public static void Smokestack(Transform parent, Vector3 groundPos, float height = 30f, float diameter = 3f, bool smoke = true)
    {
        EnsureFactory();
        Decor(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * (height * 0.5f), Quaternion.identity, new Vector3(diameter, height * 0.5f, diameter), Brick, "Stack");
        Decor(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * 1f, Quaternion.identity, new Vector3(diameter * 1.4f, 1f, diameter * 1.4f), Limestone, "StackBase");
        Decor(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * 2.2f, Quaternion.identity, new Vector3(diameter * 1.2f, 0.2f, diameter * 1.2f), Limestone, "StackPlinth");
        for (float h = height * 0.3f; h < height - 1f; h += height * 0.3f)
            Decor(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * h, Quaternion.identity, new Vector3(diameter + 0.1f, 0.12f, diameter + 0.1f), Iron, "StackBand");
        Decor(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * (height - 1.6f), Quaternion.identity, new Vector3(diameter + 0.02f, 1.1f, diameter + 0.02f), sootMat, "StackSoot");
        Decor(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * (height - 0.3f), Quaternion.identity, new Vector3(diameter + 0.4f, 0.3f, diameter + 0.4f), Limestone, "StackCap");
        Decor(parent, PrimitiveType.Sphere, groundPos + Vector3.up * (height + 0.2f) + Vector3.right * (diameter * 0.55f), Quaternion.identity, Vector3.one * 0.35f, dotMat, "StackLamp");
        if (!smoke) return;
        var ps = Steam(parent, groundPos + Vector3.up * (height + 0.5f), Vector3.up * 2.2f + Vector3.right * 0.8f, 9f, 7f, 2.2f, 4.5f);
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = new Color(0.3f, 0.29f, 0.3f, 0.32f);   // coal smoke
        }
    }

    /// A turbine lying between two ground points: the casing, its bands and saddles, a solid deck to run along on top,
    /// a fan turning in each open end, gauges on the flank (decor; the deck is solid)
    public static void Turbine(Transform parent, Vector3 a, Vector3 b, float diameter = 5.4f)
    {
        EnsureFactory();
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 6f) return;
        Vector3 dir = d / len;
        Quaternion rot = Quaternion.LookRotation(dir);
        Quaternion axial = rot * Quaternion.Euler(90f, 0f, 0f);
        Vector3 side = rot * Vector3.right;
        float r = diameter * 0.5f;
        Vector3 axis = (a + b) * 0.5f + Vector3.up * (r + 0.5f);
        Solid(parent, PrimitiveType.Cylinder, axis, axial, new Vector3(diameter, len * 0.5f - 1.2f, diameter), RivetIron, "TurbineCasing"); // you can run into it, and up onto it from the deck
        for (int i = 0; i < 4; i++)
            Decor(parent, PrimitiveType.Cylinder, a + dir * (len * (0.15f + 0.23f * i)) + Vector3.up * (r + 0.5f), axial, new Vector3(diameter + 0.14f, 0.25f, diameter + 0.14f), i % 2 == 0 ? Brass : rivetMat, "TurbineBand");
        for (int i = 0; i < 3; i++)
        {
            Vector3 p = a + dir * (len * (0.2f + 0.3f * i));
            Solid(parent, PrimitiveType.Cube, p + Vector3.up * 0.55f, rot, new Vector3(diameter * 0.9f, 1.1f, 1.4f), Iron, "TurbineSaddle");
        }
        Solid(parent, PrimitiveType.Cube, axis + Vector3.up * (r + 0.15f), rot, new Vector3(diameter * 0.55f, 0.3f, len - 2.4f), Graphite, "TurbineDeck");
        Decor(parent, PrimitiveType.Cube, axis + Vector3.up * (r + 0.35f) + side * (diameter * 0.275f + 0.06f), rot, new Vector3(0.06f, 0.08f, len - 2.4f), Led, "DeckEdge");
        Decor(parent, PrimitiveType.Cube, axis + Vector3.up * (r + 0.35f) - side * (diameter * 0.275f + 0.06f), rot, new Vector3(0.06f, 0.08f, len - 2.4f), Led, "DeckEdge");
        var life = Life(parent);
        for (int e = -1; e <= 1; e += 2)
        {
            Vector3 end = axis + dir * (e * (len * 0.5f - 1.2f));
            Vector3 n = dir * e;
            Decor(parent, PrimitiveType.Cylinder, end + n * 0.15f, axial, new Vector3(diameter * 0.9f, 0.3f, diameter * 0.9f), Graphite, "TurbineThroat");
            var hub = new GameObject("TurbineHub").transform;
            hub.SetParent(parent);
            hub.position = end + n * 0.4f;
            hub.rotation = Quaternion.LookRotation(n);
            Decor(hub, PrimitiveType.Cylinder, hub.position, Quaternion.LookRotation(n) * Quaternion.Euler(90f, 0f, 0f), new Vector3(1f, 0.2f, 1f), railMat, "TurbineHubCap");
            for (int i = 0; i < 4; i++)
                Decor(hub, PrimitiveType.Cube, hub.position, Quaternion.LookRotation(n) * Quaternion.Euler(0f, 0f, i * 45f) * Quaternion.Euler(0f, 30f, 0f), new Vector3(0.5f, diameter * 0.78f, 0.06f), panelMat, "TurbineBlade");
            life.AddSpin(hub, Vector3.forward, e * 300f);
            Decor(parent, PrimitiveType.Cylinder, end + n * 0.5f, axial, new Vector3(diameter * 0.92f, 0.06f, diameter * 0.92f), rivetMat, "TurbineRing");
        }
        Gauge(parent, axis + side * (r + 0.02f) + Vector3.up * 0.4f - dir * (len * 0.2f), side);
        Gauge(parent, axis - side * (r + 0.02f) + Vector3.up * 0.4f + dir * (len * 0.2f), -side);
        Pipe(parent, axis + Vector3.up * (r - 0.4f) - dir * (len * 0.5f - 1.2f), axis + Vector3.up * (r - 0.4f) - dir * (len * 0.5f + 3f), 0.5f);
    }

    /// A control desk: a base, a sloped panel of gauges, a screen and a lamp (decor)
    public static void ControlDesk(Transform parent, Vector3 groundPos, float yaw)
    {
        EnsureFactory();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Solid(parent, PrimitiveType.Cube, groundPos + Vector3.up * 0.5f, rot, new Vector3(3f, 1f, 1f), Iron, "DeskBase");
        Decor(parent, PrimitiveType.Cube, groundPos + Vector3.up * 1.15f, rot * Quaternion.Euler(-25f, 0f, 0f), new Vector3(3f, 0.6f, 0.9f), panelMat, "DeskPanel");
        Decor(parent, PrimitiveType.Cube, groundPos + Vector3.up * 1.15f, rot * Quaternion.Euler(-25f, 0f, 0f), new Vector3(3.04f, 0.04f, 0.94f), Brass, "DeskTrim");
        Vector3 n = rot * Quaternion.Euler(-25f, 0f, 0f) * Vector3.forward;
        for (int i = -1; i <= 1; i++)
            Gauge(parent, groundPos + Vector3.up * 1.3f + rot * new Vector3(i * 0.8f, 0f, 0f) + n * 0.42f, n);
        // A board of dials and switches over the desk, in a brass frame, lit from behind
        Decor(parent, PrimitiveType.Cube, groundPos + Vector3.up * 1.9f + rot * new Vector3(0f, 0f, -0.3f), rot, new Vector3(1.7f, 1f, 0.08f), Brass, "DeskBoardFrame");
        Decor(parent, PrimitiveType.Cube, groundPos + Vector3.up * 1.9f + rot * new Vector3(0f, 0f, -0.25f), rot, new Vector3(1.55f, 0.85f, 0.02f), Charcoal, "DeskBoard");
        for (int i = -1; i <= 1; i++)
            Gauge(parent, groundPos + Vector3.up * 2.1f + rot * new Vector3(i * 0.5f, 0f, -0.23f), rot * Vector3.forward);
        for (int i = 0; i < 5; i++)
            Decor(parent, PrimitiveType.Cube, groundPos + Vector3.up * 1.62f + rot * new Vector3(-0.5f + i * 0.25f, 0f, -0.22f), rot, new Vector3(0.05f, 0.14f, 0.03f), i % 2 == 0 ? Amber : Brass, "DeskSwitch");
        Decor(parent, PrimitiveType.Cube, groundPos + Vector3.up * 1.42f + rot * new Vector3(0f, 0f, -0.3f), rot, new Vector3(0.2f, 0.12f, 0.2f), Iron, "ScreenMount");
        Decor(parent, PrimitiveType.Cube, groundPos + Vector3.up * 1.05f + rot * new Vector3(1.3f, 0f, 0.2f), rot, new Vector3(0.1f, 0.06f, 0.1f), beaconMat, "DeskLamp");
    }

    /// Ore lumps (or stamped parts) riding a conveyor from `from` to `to`, round and round (decor)
    public static void BeltLoad(Transform parent, Vector3 from, Vector3 to, int count, float speed = 2.2f, bool parts = false)
    {
        EnsureFactory();
        var life = Life(parent);
        var rng = new System.Random(Mathf.RoundToInt(from.x * 7f + from.z * 3f + to.z));
        float len = (to - from).magnitude;
        for (int i = 0; i < count; i++)
        {
            float s = 0.35f + 0.35f * (float)rng.NextDouble();
            float lift = 1.05f + (parts ? 0.07f : s * 0.3f);
            Vector3 start = from + Vector3.up * lift;
            GameObject go = parts
                ? Decor(parent, PrimitiveType.Cube, start, Quaternion.identity, new Vector3(1.1f, 0.14f, 0.7f), partMat, "BeltPart")
                : Decor(parent, PrimitiveType.Cube, start, Quaternion.Euler((float)rng.NextDouble() * 40f, (float)rng.NextDouble() * 360f, 15f), new Vector3(s, s * 0.7f, s * 0.9f), rockMat, "BeltOre");
            life.AddLoop(go.transform, start, to + Vector3.up * lift, speed, len * (i + 0.5f) / count);
        }
    }

    /// A plume: white, rising along `drift`, gone in `life` seconds (null while a postcard is being shot)
    static ParticleSystem Steam(Transform parent, Vector3 at, Vector3 drift, float rate, float life, float sizeMin, float sizeMax)
    {
        if (!Atmosphere) return null;
        steamMat ??= Particle(new Color(0.85f, 0.87f, 0.9f, 0.35f), false);
        var ps = Cloud(parent, "Steam", at, new Vector3(0.5f, 0.5f, 0.5f), steamMat, rate, life, sizeMin, sizeMax, new Color(0.85f, 0.87f, 0.9f, 0.35f), drift, -0.1f, 0.5f);
        var main = ps.main;
        main.prewarm = false;
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.4f, 1f, 1f));
        return ps;
    }
}

/// <summary>
/// The plant's one animator: spinners, swings, slides, belt loops, press stamps,
/// hot floors, breathing lights and steam vents, each a flat list walked once a
/// frame. The hazards are checked here too — under a ram when it lands, or
/// standing in molten metal — against the player, never with colliders.
/// Runs on scaled time: hitstop stops the machines.
/// </summary>
public partial class FactoryLife : MonoBehaviour
{
    struct Spin { public Transform t; public Vector3 axis; public float rate; }
    struct Swing { public Transform t; public Quaternion rest; public Vector3 axis; public float amp, period, phase; }
    struct Slide { public Transform t; public Vector3 a, b; public float period, phase; }
    struct Loop { public Transform t; public Vector3 from, to; public float speed, offset, len; }
    class Stamp { public Transform ram; public Renderer lamp; public Material off, on; public float top, bottom, period, phase, bed; public Vector2 half; public Quaternion inv, rot; public bool down, lit; }
    struct Hot { public Vector3 centre, half; public Quaternion rot, inv; }
    struct Pulse { public Light l; public float baseI, depth, rate, phase; }
    class Vent { public ParticleSystem ps; public float period, phase, burst; public bool on; }

    readonly List<Spin> spins = new List<Spin>();
    readonly List<Swing> swings = new List<Swing>();
    readonly List<Slide> slides = new List<Slide>();
    readonly List<Loop> loops = new List<Loop>();
    readonly List<Stamp> stamps = new List<Stamp>();
    readonly List<Hot> hots = new List<Hot>();
    readonly List<Pulse> pulses = new List<Pulse>();
    readonly List<Vent> vents = new List<Vent>();
    MovementConfig cfg;
    float nextHotAt;

    const float TwoPi = Mathf.PI * 2f;

    public void AddSpin(Transform t, Vector3 axis, float degPerSec) => spins.Add(new Spin { t = t, axis = axis, rate = degPerSec });
    public void AddSwing(Transform t, Vector3 axis, float amplitude, float period, float phase)
        => swings.Add(new Swing { t = t, rest = t.localRotation, axis = axis, amp = amplitude, period = Mathf.Max(0.1f, period), phase = phase });
    public void AddSlide(Transform t, Vector3 a, Vector3 b, float period, float phase)
        => slides.Add(new Slide { t = t, a = a, b = b, period = Mathf.Max(0.1f, period), phase = phase });
    public void AddLoop(Transform t, Vector3 from, Vector3 to, float speed, float offset)
        => loops.Add(new Loop { t = t, from = from, to = to, speed = speed, offset = offset, len = Mathf.Max(0.1f, (to - from).magnitude) });
    public void AddStamp(Transform ram, Renderer lamp, float top, float bottom, float period, float phase, Vector2 half, Quaternion rot, float bed, Material off, Material on)
        => stamps.Add(new Stamp { ram = ram, lamp = lamp, off = off, on = on, top = top, bottom = bottom, period = Mathf.Max(0.5f, period), phase = phase, half = half, rot = rot, inv = Quaternion.Inverse(rot), bed = bed });
    public void AddHot(Vector3 centre, Quaternion rot, Vector3 size) => hots.Add(new Hot { centre = centre, half = size * 0.5f, rot = rot, inv = Quaternion.Inverse(rot) });
    public void AddPulse(Light l, float depth, float rate, float phase) => pulses.Add(new Pulse { l = l, baseI = l.intensity, depth = depth, rate = rate, phase = phase });
    public void AddVent(ParticleSystem ps, float period, float phase, float burst)
    {
        var em = ps.emission;
        em.enabled = false;
        vents.Add(new Vent { ps = ps, period = Mathf.Max(0.5f, period), phase = phase, burst = Mathf.Clamp01(burst) });
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return; // hitstop: the plant stands still
        float t = Time.time;
        for (int i = 0; i < spins.Count; i++) { var s = spins[i]; if (s.t != null) s.t.Rotate(s.axis, s.rate * dt, Space.Self); }
        for (int i = 0; i < swings.Count; i++)
        {
            var s = swings[i];
            if (s.t == null) continue;
            s.t.localRotation = s.rest * Quaternion.AngleAxis(s.amp * Mathf.Sin((t / s.period + s.phase) * TwoPi), s.axis);
        }
        for (int i = 0; i < slides.Count; i++)
        {
            var s = slides[i];
            if (s.t == null) continue;
            float k = 0.5f - 0.5f * Mathf.Cos((t / s.period + s.phase) * TwoPi);
            s.t.position = Vector3.Lerp(s.a, s.b, k);
        }
        for (int i = 0; i < loops.Count; i++)
        {
            var l = loops[i];
            if (l.t == null) continue;
            float u = Mathf.Repeat(t * l.speed + l.offset, l.len) / l.len;
            l.t.position = Vector3.Lerp(l.from, l.to, u);
        }
        for (int i = 0; i < pulses.Count; i++)
        {
            var p = pulses[i];
            if (p.l != null) p.l.intensity = p.baseI * (1f + p.depth * (Mathf.PerlinNoise(t * p.rate + p.phase, 0.37f) * 2f - 1f));
        }
        for (int i = 0; i < vents.Count; i++)
        {
            var v = vents[i];
            if (v.ps == null) continue;
            bool on = Mathf.Repeat(t / v.period + v.phase, 1f) < v.burst;
            if (on == v.on) continue;
            v.on = on;
            var em = v.ps.emission;
            em.enabled = on;
        }

        var director = CombatDirector.Instance;
        var motor = director != null ? director.motor : null;
        if (cfg == null && director != null) cfg = director.config;
        Vector3 feet = motor != null ? motor.CurrPosition : new Vector3(0f, -9999f, 0f);

        for (int i = 0; i < stamps.Count; i++)
        {
            var s = stamps[i];
            if (s.ram == null) continue;
            float u = Mathf.Repeat(t / s.period + s.phase, 1f);
            float h;
            if (u < 0.62f) h = 1f;
            else if (u < 0.68f) { float k = (u - 0.62f) / 0.06f; h = 1f - k * k; }
            else if (u < 0.82f) h = 0f;
            else h = Mathf.SmoothStep(0f, 1f, (u - 0.82f) / 0.18f);
            Vector3 p = s.ram.position;
            s.ram.position = new Vector3(p.x, Mathf.Lerp(s.bottom, s.top, h), p.z);
            bool lit = u >= 0.5f && u < 0.68f;
            if (lit != s.lit && s.lamp != null) { s.lit = lit; s.lamp.sharedMaterial = lit ? s.on : s.off; }
            bool down = u >= 0.68f && u < 0.82f;
            if (down && !s.down)
            {
                // It lands: whoever is under it takes it, and the floor rings if you are near enough to hear it
                Vector3 local = s.inv * (feet - new Vector3(p.x, feet.y, p.z));
                if (motor != null && director != null && !motor.HasIFrames && Mathf.Abs(local.x) <= s.half.x + 0.3f && Mathf.Abs(local.z) <= s.half.y + 0.3f
                    && feet.y >= s.bed - 0.5f && feet.y <= s.bed + 1.6f)
                {
                    Vector3 away = s.rot * new Vector3(local.x >= 0f ? 1f : -1f, 0f, 0f);
                    director.ReportPlayerHit(away, cfg != null ? cfg.factoryPressDamage : 20f);
                    motor.AddImpulse(away * 8f + Vector3.up * 4f);
                }
                float near = new Vector2(feet.x - p.x, feet.z - p.z).magnitude;
                if (near < 45f)
                {
                    ShockRing.Spawn(new Vector3(p.x, s.bed + 0.05f, p.z), 0.4f, 3.5f, 0.35f, false);
                    if (near < 28f && GameFeel.Instance != null) GameFeel.Instance.PlayClank(Mathf.Clamp01(1f - near / 30f));
                }
            }
            s.down = down;
        }

        if (motor != null && director != null && t >= nextHotAt && !motor.HasIFrames)
        {
            for (int i = 0; i < hots.Count; i++)
            {
                var hz = hots[i];
                Vector3 local = hz.inv * (feet - hz.centre);
                if (Mathf.Abs(local.x) > hz.half.x || Mathf.Abs(local.y) > hz.half.y || Mathf.Abs(local.z) > hz.half.z) continue;
                nextHotAt = t + 0.5f;
                Vector3 away = hz.rot * new Vector3(local.x >= 0f ? 1f : -1f, 0f, 0f);
                director.ReportPlayerHit(away, cfg != null ? cfg.factoryMoltenDamage : 10f);
                motor.AddImpulse(Vector3.up * 8f + away * 5f); // out of it
                if (GameFeel.Instance != null) GameFeel.Instance.Sparks(feet + Vector3.up * 0.3f, 14, false);
                break;
            }
        }
        TickHazards(t, dt, director, motor, feet); // the pads, the updrafts, the carts, the sweeps, the jaws, the arcs, the belts
    }
}
