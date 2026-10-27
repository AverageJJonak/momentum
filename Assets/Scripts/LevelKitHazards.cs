using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE PLANT BITES BACK — and gives back. The Works' first environmental dangers and the things in it you can use
/// (THE UNDERGROUND's own — the third rail, the trains, the steam vents, the waterfalls, the sluice gates, the
/// collapsing slabs — live in LevelKitUnderground; the pads, the updraft and the carts here are still its pads, its
/// vent-shaft fan and its shunting locos):
///  JUMP PADS   stand on one and it throws you, up and along its chevrons; your
///              own speed comes with you (a pad at speed is a launch, not a hop)
///  FAN SHAFTS  an updraft off a floor fan carries you up its column; steer off
///              it onto whatever ledge it was put under
///  HAUL CARTS  ore carts driving a rail at speed with red lamps on both ends —
///              jump them, step out of the rail, or dash through (i-frames);
///              a pass that misses you by a hand is a NEAR MISS (a pip)
///  SWEEPERS    a boom turning round a pylon at knee height: jump it or slide it
///  JAWS        two blocks that slam shut across a passage on a cycle; the lamp
///              goes red first; shut, they push you back out
///  ARC GAPS    a live arc between two posts for part of every cycle, red while
///              it is: slide under it, or wait
///  DRIVEN BELTS a conveyor that carries you: with you it is free speed, against
///              you it is a fight
/// All decor except what you stand on (the pad plates, the fan housings, the
/// sweeper pylons, the arc posts); every hazard is a check against the player in
/// FactoryLife, never a moving collider. Colour: red is danger, amber is the
/// machine's own light, hazard marks are black and mustard, nothing here is cyan or gold.
/// </summary>
public static partial class LevelKit
{
    static Material padMat, padRingMat, padFlashMat, cartRedMat, arcMat, sweepRingMat, coilMat, airMat;

    static void EnsureHazards()
    {
        if (padMat != null) return;
        EnsureFactory();
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        padMat = Lit(lit, new Color(0.22f, 0.23f, 0.26f), 0.6f); padMat.SetFloat("_Metallic", 0.8f);
        padRingMat = Unlit(unlit, new Color(2.2f, 1.4f, 0.35f));   // amber: the pad's own light
        padFlashMat = Unlit(unlit, new Color(3.5f, 3.2f, 2.6f));   // white-hot the instant it fires
        cartRedMat = Unlit(unlit, new Color(2.6f, 0.2f, 0.12f));   // red: danger, both ends of every cart
        arcMat = Unlit(unlit, new Color(2.6f, 0.4f, 0.25f));       // the arc, live
        sweepRingMat = Unlit(unlit, new Color(0.8f, 0.2f, 0.14f)); // the sweep's reach on the floor
        coilMat = Lit(lit, new Color(0.75f, 0.42f, 0.2f), 0.6f); coilMat.SetFloat("_Metallic", 0.9f);
    }

    /// A jump pad: a plate FLUSH with the floor (a centimetre proud, so a slide carries straight over it), an amber
    /// ring, chevrons along `yaw`; stand on it, slide onto it, land on it, and it throws you (interactable)
    /// A pad that throws you up and along your own line: `yaw` only turns its face now, `forward` is how hard it
    /// pushes you the way you are already going (or looking, from a standstill)
    public static void JumpPad(Transform parent, Vector3 groundPos, float yaw, float up = 16f, float forward = 6f)
    {
        EnsureHazards();
        // Snap to whatever is under the mark: a level names the height a roof was drawn at, and the slab you actually
        // run on sits a hair above it (RoofKit.Proud); a pad a hair under the floor is a pad you cannot see
        Physics.SyncTransforms();
        if (Physics.Raycast(groundPos + Vector3.up * 1f, Vector3.down, out RaycastHit under, 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) groundPos = under.point;
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Solid(parent, PrimitiveType.Cube, groundPos - Vector3.up * 0.09f, rot, new Vector3(2.4f, 0.2f, 2.4f), padMat, "JumpPad");   // sunk into the floor, its top a centimetre up
        for (int e = 0; e < 4; e++)   // the hazard band: a flat striped frame round the plate's edge, nothing to trip on
        {
            Quaternion edge = rot * Quaternion.Euler(0f, e * 90f, 0f);
            Stripes(parent, groundPos + Vector3.up * 0.015f + edge * new Vector3(0f, 0f, 1.17f), new Vector3(2.46f, 0.01f, 0.12f), yaw + e * 90f);
        }
        var ring = Decor(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * 0.025f, Quaternion.identity, new Vector3(2.1f, 0.02f, 2.1f), padRingMat, "PadRing").GetComponent<Renderer>();
        Decor(parent, PrimitiveType.Cylinder, groundPos + Vector3.up * 0.04f, Quaternion.identity, new Vector3(1.7f, 0.02f, 1.7f), padMat, "PadInner");
        for (int q = 0; q < 4; q++)
        {
            Quaternion face = rot * Quaternion.Euler(0f, q * 90f, 0f);
            Vector3 tip = groundPos + Vector3.up * 0.055f + face * new Vector3(0f, 0f, 0.55f);
            Decor(parent, PrimitiveType.Cube, tip + face * new Vector3(-0.22f, 0f, -0.22f), face * Quaternion.Euler(0f, -45f, 0f), new Vector3(0.07f, 0.02f, 0.62f), padRingMat, "PadChevron");
            Decor(parent, PrimitiveType.Cube, tip + face * new Vector3(0.22f, 0f, -0.22f), face * Quaternion.Euler(0f, 45f, 0f), new Vector3(0.07f, 0.02f, 0.62f), padRingMat, "PadChevron");
        }
        var l = Light(parent, groundPos + Vector3.up * 1.1f, 6f, 1.5f);
        l.color = new Color(1f, 0.7f, 0.2f);
        var life = Life(parent);
        life.AddPulse(l, 0.35f, 3f, groundPos.x + groundPos.z);
        life.AddPad(groundPos + Vector3.up * 0.02f, rot, 1.2f, up, forward, ring, padRingMat, padFlashMat);
    }

    /// A floor fan you stand over, and the column of air above it that carries you up `height` (interactable)
    public static void FanShaft(Transform parent, Vector3 groundCenter, float height, float size = 3.4f)
    {
        EnsureHazards();
        Solid(parent, PrimitiveType.Cube, groundCenter + Vector3.up * 0.18f, Quaternion.identity, new Vector3(size, 0.36f, size), RivetIron, "FanHousing");
        Stripes(parent, groundCenter + Vector3.up * 0.18f, new Vector3(size + 0.06f, 0.12f, size + 0.06f));
        var hub = new GameObject("FloorFanHub").transform;
        hub.SetParent(parent);
        hub.position = groundCenter + Vector3.up * 0.4f;
        Decor(hub, PrimitiveType.Cylinder, hub.position, Quaternion.identity, new Vector3(0.5f, 0.05f, 0.5f), railMat, "FanHub");
        for (int i = 0; i < 3; i++)
            Decor(hub, PrimitiveType.Cube, hub.position, Quaternion.Euler(0f, i * 60f, 0f) * Quaternion.Euler(28f, 0f, 0f), new Vector3(size * 0.82f, 0.04f, 0.32f), panelMat, "FanBlade");
        int bars = Mathf.Max(3, Mathf.RoundToInt(size / 0.5f));
        for (int i = 0; i < bars; i++)
            Decor(parent, PrimitiveType.Cube, groundCenter + Vector3.up * 0.5f + Vector3.forward * ((i + 0.5f) / bars * size - size * 0.5f), Quaternion.identity, new Vector3(size, 0.04f, 0.06f), Iron, "FanGuard");
        for (int i = 1; i <= 3; i++)
            Decor(parent, PrimitiveType.Cylinder, groundCenter + Vector3.up * (height * i / 4f), Quaternion.identity, new Vector3(size * 0.9f, 0.02f, size * 0.9f), holoMat, "AirGate");
        var l = Light(parent, groundCenter + Vector3.up * 1.2f, size * 2.5f, 1.8f);
        l.color = new Color(1f, 0.72f, 0.25f);
        var life = Life(parent);
        life.AddSpin(hub, Vector3.up, 420f);
        life.AddPulse(l, 0.3f, 4f, groundCenter.z);
        if (Atmosphere)
        {
            airMat ??= Particle(new Color(0.8f, 0.85f, 0.9f, 0.22f), false);
            var ps = Cloud(parent, "Updraft", groundCenter + Vector3.up * 0.6f, new Vector3(size * 0.8f, 0.3f, size * 0.8f), airMat, 40f, height / 8f, 0.25f, 0.5f,
                new Color(0.8f, 0.85f, 0.9f, 0.22f), Vector3.up * 8f, 0f, 0.4f);
            var main = ps.main;
            main.prewarm = true;
        }
        life.AddUpdraft(groundCenter, size * 0.5f + 0.3f, groundCenter.y, groundCenter.y + height);
    }

    /// A haul cart driving a rail through `points` at `speed`, round and round (or back and forth): a hopper of ore
    /// (or a pot of slag) on mag skids, red lamps at both ends, a red light ahead of it; it hits anything in its way (hazard)
    public static void HaulCart(Transform parent, Vector3[] points, float speed, float phase = 0f, bool pingPong = false, bool slag = false)
    {
        EnsureHazards();
        if (points == null || points.Length < 2) return;
        Vector3 from = points[0];
        Quaternion rot = Quaternion.LookRotation((points[1] - points[0]).normalized);
        var cart = new GameObject(slag ? "SlagPot" : "HaulCart").transform;
        cart.SetParent(parent);
        cart.position = from;
        cart.rotation = rot;
        Decor(cart, PrimitiveType.Cube, from + Vector3.up * 0.18f, rot, new Vector3(1.8f, 0.16f, 3.8f), Iron, "CartFrame");
        Decor(cart, PrimitiveType.Cube, from + Vector3.up * 0.08f, rot, new Vector3(1.7f, 0.02f, 3.7f), Led, "CartGlow");
        for (int s = -1; s <= 1; s += 2)
            Decor(cart, PrimitiveType.Cube, from + rot * new Vector3(s * 0.95f, 0.25f, 0f), rot, new Vector3(0.16f, 0.16f, 3.8f), railMat, "Skid");
        if (slag)
        {
            Decor(cart, PrimitiveType.Cylinder, from + Vector3.up * 0.75f, rot, new Vector3(2.3f, 0.5f, 2.3f), RivetIron, "PotBowl");
            Decor(cart, PrimitiveType.Cylinder, from + Vector3.up * 0.9f, rot, new Vector3(2.36f, 0.06f, 2.36f), MustardPaint, "PotBand");
            Decor(cart, PrimitiveType.Cylinder, from + Vector3.up * 1.26f, rot, new Vector3(2.4f, 0.04f, 2.4f), rivetMat, "PotLip");
            Decor(cart, PrimitiveType.Cylinder, from + Vector3.up * 1.27f, rot, new Vector3(2f, 0.02f, 2f), moltenMat, "PotMelt");
            var glow = Light(cart, from + Vector3.up * 2f, 7f, 2.2f);
            glow.color = new Color(1f, 0.5f, 0.15f);
        }
        else
        {
            Decor(cart, PrimitiveType.Cube, from + Vector3.up * 0.72f, rot, new Vector3(2.4f, 1f, 4.2f), RivetIron, "CartHull");
            Decor(cart, PrimitiveType.Cube, from + Vector3.up * 0.72f, rot, new Vector3(2.46f, 0.18f, 4.26f), Iron, "CartBand");
            Decor(cart, PrimitiveType.Cube, from + Vector3.up * 0.4f, rot, new Vector3(2.44f, 0.14f, 4.24f), MustardPaint, "CartStripe");
            Decor(cart, PrimitiveType.Sphere, from + Vector3.up * 1.22f, rot, new Vector3(2.2f, 0.7f, 3.6f), rockMat, "OreHeap");
            for (int i = 0; i < 3; i++)
                Decor(cart, PrimitiveType.Cube, from + rot * new Vector3(-0.6f + i * 0.6f, 1.5f, -0.9f + i * 0.9f), rot * Quaternion.Euler(20f * i, 35f * i, 15f), Vector3.one * 0.3f, oreMat, "OreLump");
        }
        for (int e = -1; e <= 1; e += 2)
            for (int s = -1; s <= 1; s += 2)
                Decor(cart, PrimitiveType.Cube, from + rot * new Vector3(s * 0.75f, 0.95f, e * 2.12f), rot, new Vector3(0.4f, 0.14f, 0.05f), cartRedMat, "CartLamp");
        var l = Light(cart, from + rot * new Vector3(0f, 0.9f, 2.8f), 9f, 1.8f);
        l.color = new Color(1f, 0.25f, 0.15f);
        Life(parent).AddCart(cart, points, speed, phase, pingPong, slag ? 1.3f : 1.35f);
    }

    /// A dark mouth in a wall where a rail runs in or out: a recess, a hazard frame, a red signal over it (decor)
    public static void TunnelMouth(Transform parent, Vector3 wallPoint, Vector3 facing, float w = 3.6f, float h = 2.6f)
    {
        EnsureHazards();
        Vector3 n = facing.normalized;
        Quaternion rot = Quaternion.LookRotation(n);
        Vector3 right = rot * Vector3.right;
        Decor(parent, PrimitiveType.Cube, wallPoint + n * 0.05f + Vector3.up * (h * 0.5f), rot, new Vector3(w, h, 0.08f), Soot, "TunnelMouth");
        Decor(parent, PrimitiveType.Cube, wallPoint + n * 0.12f + Vector3.up * (h + 0.15f), rot, new Vector3(w + 0.6f, 0.3f, 0.16f), Limestone, "MouthLintel");
        Stripes(parent, wallPoint + n * 0.21f + Vector3.up * (h + 0.15f), new Vector3(w + 0.6f, 0.22f, 0.02f), rot.eulerAngles.y);
        for (int s = -1; s <= 1; s += 2)
            Decor(parent, PrimitiveType.Cube, wallPoint + n * 0.12f + right * (s * (w * 0.5f + 0.15f)) + Vector3.up * (h * 0.5f), rot, new Vector3(0.3f, h + 0.3f, 0.16f), Brick, "MouthPost");
        Decor(parent, PrimitiveType.Sphere, wallPoint + n * 0.3f + Vector3.up * (h + 0.65f), Quaternion.identity, Vector3.one * 0.32f, cartRedMat, "MouthSignal");
        Decor(parent, PrimitiveType.Cube, wallPoint + n * 0.15f + Vector3.up * (h + 0.65f), rot, new Vector3(0.44f, 0.44f, 0.2f), Iron, "SignalHousing");
    }

    /// A boom turning round a pylon at `height` off the floor, a red line down it and a red lamp on its tip; jump it or slide it (hazard)
    public static void Sweeper(Transform parent, Vector3 groundCenter, float radius = 6f, float height = 1.15f, float degPerSec = 90f)
    {
        EnsureHazards();
        Solid(parent, PrimitiveType.Cylinder, groundCenter + Vector3.up * ((height + 0.7f) * 0.5f), Quaternion.identity, new Vector3(0.9f, (height + 0.7f) * 0.5f, 0.9f), Iron, "SweeperPylon");
        Decor(parent, PrimitiveType.Cylinder, groundCenter + Vector3.up * (height + 0.72f), Quaternion.identity, new Vector3(1.1f, 0.06f, 1.1f), MustardPaint, "PylonCap");
        Decor(parent, PrimitiveType.Cylinder, groundCenter + Vector3.up * 0.3f, Quaternion.identity, new Vector3(1.6f, 0.3f, 1.6f), Iron, "PylonBase");
        FloorRing(parent, groundCenter, radius + 0.3f, sweepRingMat);
        var pivot = new GameObject("SweeperPivot").transform;
        pivot.SetParent(parent);
        pivot.position = groundCenter + Vector3.up * height;
        Decor(pivot, PrimitiveType.Cylinder, pivot.position, Quaternion.identity, new Vector3(0.7f, 0.16f, 0.7f), railMat, "SweeperHub");
        Decor(pivot, PrimitiveType.Cube, pivot.position + Vector3.forward * (radius * 0.5f), Quaternion.identity, new Vector3(0.3f, 0.22f, radius), Iron, "SweeperBoom");
        Decor(pivot, PrimitiveType.Cube, pivot.position + Vector3.forward * (radius * 0.5f) + Vector3.up * 0.115f, Quaternion.identity, new Vector3(0.12f, 0.02f, radius - 0.2f), cartRedMat, "BoomLine");
        Decor(pivot, PrimitiveType.Sphere, pivot.position + Vector3.forward * radius, Quaternion.identity, Vector3.one * 0.36f, cartRedMat, "BoomLamp");
        Decor(pivot, PrimitiveType.Cube, pivot.position + Vector3.back * 1.1f, Quaternion.identity, new Vector3(0.6f, 0.4f, 1.6f), Iron, "Counterweight");
        var l = Light(pivot, pivot.position + Vector3.forward * (radius - 0.4f) + Vector3.up * 0.3f, 5f, 1.6f);
        l.color = new Color(1f, 0.25f, 0.15f);
        Life(parent).AddSweep(pivot, groundCenter, radius, height, degPerSec);
    }

    /// Two jaws either side of a passage that slam shut across it on a cycle — the lamps go red first (hazard)
    public static void Jaws(Transform parent, Vector3 groundCenter, float yaw, float gap = 5f, float height = 4f, float period = 4f, float phase = 0f)
    {
        EnsureHazards();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Vector3 right = rot * Vector3.right, fwd = rot * Vector3.forward;
        Decor(parent, PrimitiveType.Cube, groundCenter + Vector3.up * 0.06f, rot, new Vector3(gap + 4.4f, 0.12f, 2.4f), Iron, "JawRail");
        Stripes(parent, groundCenter + Vector3.up * 0.06f, new Vector3(gap + 4.4f, 0.13f, 0.2f), yaw);
        for (int s = -1; s <= 1; s += 2)
            Decor(parent, PrimitiveType.Cube, groundCenter + right * (s * (gap * 0.5f + 2.1f)) + Vector3.up * ((height + 1.2f) * 0.5f), rot, new Vector3(0.6f, height + 1.2f, 0.6f), pressMat, "JawPost");
        Decor(parent, PrimitiveType.Cube, groundCenter + Vector3.up * (height + 0.9f), rot, new Vector3(gap + 4.8f, 0.6f, 0.8f), pressMat, "JawBeam");
        Transform[] jaws = new Transform[2];
        Renderer[] lamps = new Renderer[2];
        Vector3[] open = new Vector3[2], closed = new Vector3[2];
        for (int i = 0; i < 2; i++)
        {
            float s = i == 0 ? -1f : 1f;
            open[i] = groundCenter + right * (s * (gap * 0.5f + 0.8f));
            closed[i] = groundCenter + right * (s * 0.8f);
            var jaw = new GameObject(i == 0 ? "JawL" : "JawR").transform;
            jaw.SetParent(parent);
            jaw.position = open[i];
            jaw.rotation = rot;
            Decor(jaw, PrimitiveType.Cube, open[i] + Vector3.up * (height * 0.5f), rot, new Vector3(1.6f, height, 2f), pressMat, "Jaw");
            Decor(jaw, PrimitiveType.Cube, open[i] + Vector3.up * (height * 0.5f) - right * (s * 0.82f), rot, new Vector3(0.06f, height - 0.2f, 1.9f), MustardPaint, "JawFace");
            for (int k = 0; k < 3; k++)
                Decor(jaw, PrimitiveType.Cube, open[i] + Vector3.up * (height * (0.25f + 0.25f * k)) - right * (s * 0.9f), rot, new Vector3(0.14f, 0.16f, 1.6f), railMat, "JawTooth");
            lamps[i] = Decor(jaw, PrimitiveType.Cube, open[i] + Vector3.up * (height - 0.4f) + fwd * 1.02f, rot, new Vector3(0.9f, 0.16f, 0.04f), ramLampMat, "JawLamp").GetComponent<Renderer>();
            jaws[i] = jaw;
        }
        Life(parent).AddClamp(jaws[0], jaws[1], open[0], open[1], closed[0], closed[1], lamps, period, phase, groundCenter + Vector3.up * (height * 0.5f), rot, new Vector3(gap * 0.5f, height * 0.5f + 0.5f, 1.1f), fwd, ramLampMat, ramRedMat);
    }

    /// A live arc between two posts for part of every cycle: slide under it, or wait for it (hazard)
    public static void ArcGap(Transform parent, Vector3 a, Vector3 b, float period = 3f, float phase = 0f, float live = 0.4f)
    {
        EnsureHazards();
        const float arcH = 1.7f;
        foreach (var p in new[] { a, b })
        {
            Solid(parent, PrimitiveType.Cylinder, p + Vector3.up * 0.9f, Quaternion.identity, new Vector3(0.5f, 0.9f, 0.5f), Iron, "ArcPost");
            Decor(parent, PrimitiveType.Cylinder, p + Vector3.up * 1.2f, Quaternion.identity, new Vector3(0.7f, 0.22f, 0.7f), coilMat, "ArcCoil");
            Decor(parent, PrimitiveType.Cylinder, p + Vector3.up * 1.55f, Quaternion.identity, new Vector3(0.3f, 0.12f, 0.3f), ForemanWhiteStandIn(), "ArcInsulator");
            Decor(parent, PrimitiveType.Sphere, p + Vector3.up * arcH, Quaternion.identity, Vector3.one * 0.26f, railMat, "ArcTip");
            Decor(parent, PrimitiveType.Cube, p + Vector3.up * 0.25f, Quaternion.identity, new Vector3(0.9f, 0.5f, 0.9f), Iron, "ArcPostBase");
        }
        Vector3 ta = a + Vector3.up * arcH, tb = b + Vector3.up * arcH;
        Vector3 d = tb - ta;
        float len = d.magnitude;
        if (len < 1f) return;
        Quaternion rot = Quaternion.LookRotation(d / len);
        var arc = Decor(parent, PrimitiveType.Cube, (ta + tb) * 0.5f, rot, new Vector3(0.09f, 0.09f, len - 0.2f), arcMat, "Arc").GetComponent<Renderer>();
        Stripes(parent, (a + b) * 0.5f + Vector3.up * 0.02f, new Vector3(0.3f, 0.02f, len - 0.8f), rot.eulerAngles.y);
        var l = Light(parent, (ta + tb) * 0.5f, len + 3f, 2f);
        l.color = new Color(1f, 0.3f, 0.2f);
        Life(parent).AddArc(arc, l, ta, tb, period, phase, live);
    }

    /// A conveyor that carries whoever stands on it: `speed` along from → to over the belt's top (interactable)
    public static void BeltDrive(Transform parent, Vector3 from, Vector3 to, float width, float speed)
    {
        Vector3 d = to - from;
        float len = d.magnitude;
        if (len < 2f) return;
        Quaternion rot = Quaternion.LookRotation(d / len);
        Life(parent).AddBelt((from + to) * 0.5f + Vector3.up * 1.05f, rot, new Vector3(width * 0.5f, 0.7f, len * 0.5f), d / len * speed);
    }

    static Material insulatorMat;
    static Material ForemanWhiteStandIn() => insulatorMat ??= Lit(Shader.Find("Universal Render Pipeline/Lit"), new Color(0.92f, 0.92f, 0.9f), 0.5f);
}

/// The hazards' half of the plant's animator: pads, updrafts, carts, sweeps, jaws, arcs and driven belts
public partial class FactoryLife : IWeldDynamic
{
    /// Everything the animator moves, swaps or switches: the weld leaves these alone
    public void DynamicParts(List<Transform> into)
    {
        foreach (var s in spins) if (s.t != null) into.Add(s.t);
        foreach (var s in swings) if (s.t != null) into.Add(s.t);     // the robot arms
        foreach (var s in slides) if (s.t != null) into.Add(s.t);     // the ladle carriages, the gantry bridges
        foreach (var l in loops) if (l.t != null) into.Add(l.t);      // the ore and parts riding the belts
        foreach (var s in stamps) if (s.ram != null) into.Add(s.ram); // the press rams (their lamps hang under them)
        foreach (var c in carts) if (c.t != null) into.Add(c.t);
        foreach (var s in sweeps) if (s.pivot != null) into.Add(s.pivot);
        foreach (var c in clamps) { if (c.a != null) into.Add(c.a); if (c.b != null) into.Add(c.b); if (c.lamps != null) foreach (var l in c.lamps) if (l != null) into.Add(l.transform); }
        foreach (var p in pads) if (p.ring != null) into.Add(p.ring.transform);
        foreach (var a in arcs) if (a.r != null) into.Add(a.r.transform);
    }
    class Pad { public Vector3 centre; public Quaternion inv; public float half; public float lift, push; public Renderer ring; public Material rest, flash; public float readyAt, flashUntil; }
    struct Updraft { public Vector3 centre; public float half, y0, y1; }
    class Cart { public Transform t; public Vector3[] pts; public float[] cum; public float len, speed, phase, top, hitReadyAt, hornAt; public bool pingPong, passed; public int side; }
    class Sweep { public Transform pivot; public Vector3 centre; public float radius, y, rate, angle, hitReadyAt; }
    class Clamp { public Transform a, b; public Vector3 openA, openB, closedA, closedB, centre, half, fwd; public Quaternion rot, inv; public Renderer[] lamps; public Material off, on; public float period, phase; public bool shut, lit; }
    class Arc { public Renderer r; public Light l; public Vector3 a, b; public float period, phase, live, hitReadyAt; public bool on; }
    struct Belt { public Vector3 centre, half, vel; public Quaternion inv; }

    readonly List<Pad> pads = new List<Pad>();
    readonly List<Updraft> updrafts = new List<Updraft>();
    readonly List<Cart> carts = new List<Cart>();
    readonly List<Sweep> sweeps = new List<Sweep>();
    readonly List<Clamp> clamps = new List<Clamp>();
    readonly List<Arc> arcs = new List<Arc>();
    readonly List<Belt> belts = new List<Belt>();

    public void AddPad(Vector3 centre, Quaternion rot, float half, float lift, float push, Renderer ring, Material rest, Material flash)
        => pads.Add(new Pad { centre = centre, inv = Quaternion.Inverse(rot), half = half, lift = lift, push = push, ring = ring, rest = rest, flash = flash });
    public int PadCount => pads.Count;
    /// Pads made at a staging origin and moved with their leg (Course.Ascent): their centres and frames follow `t`
    public void RelocatePads(int from, Transform t)
    {
        for (int i = Mathf.Max(0, from); i < pads.Count; i++)
        {
            var p = pads[i];
            p.centre = t.TransformPoint(p.centre);
            p.inv = Quaternion.Inverse(t.rotation * Quaternion.Inverse(p.inv));
        }
    }
    public void AddUpdraft(Vector3 centre, float half, float y0, float y1) => updrafts.Add(new Updraft { centre = centre, half = half, y0 = y0, y1 = y1 });
    public void AddCart(Transform t, Vector3[] pts, float speed, float phase, bool pingPong, float top)
    {
        var cum = new float[pts.Length];
        for (int i = 1; i < pts.Length; i++) cum[i] = cum[i - 1] + Vector3.Distance(pts[i - 1], pts[i]);
        carts.Add(new Cart { t = t, pts = pts, cum = cum, len = Mathf.Max(0.1f, cum[pts.Length - 1]), speed = speed, phase = phase, pingPong = pingPong, top = top, side = 0 });
    }
    public void AddSweep(Transform pivot, Vector3 centre, float radius, float y, float degPerSec)
        => sweeps.Add(new Sweep { pivot = pivot, centre = centre, radius = radius, y = centre.y + y, rate = degPerSec });
    public void AddClamp(Transform a, Transform b, Vector3 openA, Vector3 openB, Vector3 closedA, Vector3 closedB, Renderer[] lamps, float period, float phase,
        Vector3 centre, Quaternion rot, Vector3 half, Vector3 fwd, Material off, Material on)
        => clamps.Add(new Clamp { a = a, b = b, openA = openA, openB = openB, closedA = closedA, closedB = closedB, lamps = lamps, period = Mathf.Max(1f, period), phase = phase,
            centre = centre, rot = rot, inv = Quaternion.Inverse(rot), half = half, fwd = fwd, off = off, on = on });
    public void AddArc(Renderer r, Light l, Vector3 a, Vector3 b, float period, float phase, float live)
    {
        r.enabled = false;
        l.enabled = false;
        arcs.Add(new Arc { r = r, l = l, a = a, b = b, period = Mathf.Max(0.5f, period), phase = phase, live = Mathf.Clamp01(live) });
    }
    public void AddBelt(Vector3 centre, Quaternion rot, Vector3 half, Vector3 vel) => belts.Add(new Belt { centre = centre, half = half, vel = vel, inv = Quaternion.Inverse(rot) });

    /// Where a cart is along its rail, and which way it faces, for a distance along it
    static Vector3 Along(Cart c, float s, out Vector3 dir)
    {
        s = Mathf.Clamp(s, 0f, c.len);
        int i = 1;
        while (i < c.cum.Length - 1 && c.cum[i] < s) i++;
        Vector3 a = c.pts[i - 1], b = c.pts[i];
        float seg = Mathf.Max(0.001f, c.cum[i] - c.cum[i - 1]);
        dir = (b - a) / seg;
        return Vector3.Lerp(a, b, (s - c.cum[i - 1]) / seg);
    }

    void TickHazards(float t, float dt, CombatDirector director, PlayerMotor motor, Vector3 feet)
    {
        bool live = motor != null && director != null;
        float capH = motor != null ? motor.CurrentHeight : (cfg != null ? cfg.capsuleHeight : 1.8f);   // the capsule as it is, so a fastfall crouch under a fence is a real pass
        var feel = GameFeel.Instance;

        // Jump pads: stand on one and it throws you UP and ALONG YOUR OWN LINE — the pad does not care which way it
        // faces, so you pick the direction by how you arrive. Standing still on one, it throws you where you look.
        for (int i = 0; i < pads.Count; i++)
        {
            var p = pads[i];
            if (p.flashUntil > 0f && t >= p.flashUntil) { p.flashUntil = 0f; if (p.ring != null) p.ring.sharedMaterial = p.rest; }
            if (!live || t < p.readyAt) continue;
            Vector3 local = p.inv * (feet - p.centre);
            if (Mathf.Abs(local.x) > p.half || Mathf.Abs(local.z) > p.half || local.y < -0.35f || local.y > 0.7f) continue;
            Vector3 v = motor.Velocity;
            Vector3 flat = new Vector3(v.x, 0f, v.z);
            Vector3 dir = flat.sqrMagnitude > 0.5f ? flat.normalized
                : motor.look != null ? Quaternion.Euler(0f, motor.look.Yaw, 0f) * Vector3.forward : Vector3.zero;
            motor.Launch(new Vector3(v.x + dir.x * p.push, Mathf.Max(v.y, 0f) + p.lift, v.z + dir.z * p.push));
            p.readyAt = t + 0.5f;
            p.flashUntil = t + 0.18f;
            if (p.ring != null) p.ring.sharedMaterial = p.flash;
            ShockRing.Spawn(p.centre + Vector3.up * 0.05f, 0.6f, 3f, 0.3f, false);
            if (feel != null) feel.PlayPad();
        }

        // Updrafts: inside the column you are carried up; the top quarter only holds you, so you can steer off it
        if (live)
        {
            float g = cfg != null ? cfg.gravity : 25f, lift = cfg != null ? cfg.factoryLift : 14f;
            for (int i = 0; i < updrafts.Count; i++)
            {
                var u = updrafts[i];
                if (Mathf.Abs(feet.x - u.centre.x) > u.half || Mathf.Abs(feet.z - u.centre.z) > u.half || feet.y < u.y0 - 0.5f || feet.y > u.y1) continue;
                float k = (feet.y - u.y0) / Mathf.Max(1f, u.y1 - u.y0);
                float accel = k > 0.75f ? g : g + lift;
                if (motor.Velocity.y < 16f) motor.AddImpulse(Vector3.up * (accel * dt));
                break;
            }
        }

        // Carts: along the rail, and into whoever is on it; a pass that just misses is a NEAR MISS
        for (int i = 0; i < carts.Count; i++)
        {
            var c = carts[i];
            if (c.t == null) continue;
            float s;
            bool back = false;
            if (c.pingPong)
            {
                float period = 2f * c.len / Mathf.Max(0.1f, c.speed);
                float u = Mathf.Repeat(t / period + c.phase, 1f);
                back = u >= 0.5f;
                s = back ? (1f - u) * 2f * c.len : u * 2f * c.len;
            }
            else s = Mathf.Repeat(t * c.speed + c.phase * c.len, c.len);
            Vector3 pos = Along(c, s, out Vector3 dir);
            if (back) dir = -dir;
            Quaternion rot = Quaternion.LookRotation(dir);
            c.t.SetPositionAndRotation(pos, rot);
            if (!live) continue;
            Vector3 local = Quaternion.Inverse(rot) * (feet - pos);
            bool band = feet.y > pos.y - 0.6f && feet.y < pos.y + c.top;
            bool inside = band && Mathf.Abs(local.x) <= 1.45f && Mathf.Abs(local.z) <= 2.35f;
            if (inside && !motor.HasIFrames && t >= c.hitReadyAt)
            {
                c.hitReadyAt = t + 0.8f;
                c.passed = true;
                director.ReportPlayerHit((dir + Vector3.up * 0.5f).normalized, cfg != null ? cfg.factoryCartDamage : 22f);
                motor.AddImpulse(dir * (c.speed * 0.6f) + Vector3.up * 7f);
            }
            // The horn as it comes at you
            if (local.z > 0f && local.z < 18f && Mathf.Abs(local.x) < 5f && t >= c.hornAt && feel != null)
            {
                c.hornAt = t + 3f;
                feel.PlayCartHorn();
            }
            // The near miss: it passed you within a hand of its hull, at its height, and did not touch you
            int side = local.z > 0f ? 1 : -1;
            if (Mathf.Abs(local.z) > 6f) { c.passed = false; c.side = side; continue; }
            if (c.side > 0 && side < 0 && !c.passed && band && Mathf.Abs(local.x) > 1.45f && Mathf.Abs(local.x) < 2.7f)
            {
                c.passed = true;
                director.NearMiss();
            }
            c.side = side;
        }

        // Sweepers: the boom comes round; over it, under it, or it takes you with it
        for (int i = 0; i < sweeps.Count; i++)
        {
            var s = sweeps[i];
            if (s.pivot == null) continue;
            float prev = s.angle;
            s.angle = Mathf.Repeat(s.angle + s.rate * dt, 360f);
            s.pivot.rotation = Quaternion.Euler(0f, s.angle, 0f);
            if (!live || motor.HasIFrames || t < s.hitReadyAt) continue;
            Vector3 flat = feet - s.centre; flat.y = 0f;
            float dist = flat.magnitude;
            if (dist < 0.9f || dist > s.radius + 0.5f) continue;
            if (feet.y > s.y + 0.2f || feet.y + capH < s.y - 0.2f) continue;
            float pAng = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
            float da = Mathf.DeltaAngle(prev, pAng), db = Mathf.DeltaAngle(prev, s.angle);
            bool crossed = s.rate >= 0f ? da >= -2f && da <= db + 2f : da <= 2f && da >= db - 2f;
            if (!crossed) continue;
            s.hitReadyAt = t + 0.6f;
            Vector3 push = Quaternion.Euler(0f, s.angle + (s.rate >= 0f ? 90f : -90f), 0f) * Vector3.forward;
            director.ReportPlayerHit(push, cfg != null ? cfg.factorySweepDamage : 14f);
            motor.AddImpulse(push * 6f + Vector3.up * 4f);
        }

        // Jaws: open, the lamps go red, shut — whoever is between them takes it, and shut they push you out
        for (int i = 0; i < clamps.Count; i++)
        {
            var c = clamps[i];
            if (c.a == null || c.b == null) continue;
            float u = Mathf.Repeat(t / c.period + c.phase, 1f);
            float h;
            if (u < 0.62f) h = 1f;
            else if (u < 0.68f) { float k = (u - 0.62f) / 0.06f; h = 1f - k * k; }
            else if (u < 0.84f) h = 0f;
            else h = Mathf.SmoothStep(0f, 1f, (u - 0.84f) / 0.16f);
            c.a.position = Vector3.Lerp(c.closedA, c.openA, h);
            c.b.position = Vector3.Lerp(c.closedB, c.openB, h);
            bool lit = u >= 0.5f && u < 0.68f;
            if (lit != c.lit) { c.lit = lit; foreach (var r in c.lamps) if (r != null) r.sharedMaterial = lit ? c.on : c.off; }
            bool shut = u >= 0.68f && u < 0.84f;
            if (live)
            {
                Vector3 local = c.inv * (feet - c.centre);
                bool inside = Mathf.Abs(local.x) <= c.half.x && Mathf.Abs(local.z) <= c.half.z && local.y >= -c.half.y && local.y <= c.half.y;
                if (shut && !c.shut)
                {
                    float near = Vector3.Distance(feet, c.centre);
                    if (near < 30f && feel != null) feel.PlayClank(Mathf.Clamp01(1f - near / 32f));
                    if (inside && !motor.HasIFrames)
                    {
                        Vector3 outDir = c.fwd * (local.z >= 0f ? 1f : -1f);
                        director.ReportPlayerHit(outDir, cfg != null ? cfg.factoryJawDamage : 24f);
                        motor.AddImpulse(outDir * 9f + Vector3.up * 5f);
                    }
                }
                else if (shut && inside)
                {
                    motor.AddImpulse(c.fwd * ((local.z >= 0f ? 1f : -1f) * 30f * dt)); // shut: it is a wall, and it shoves
                }
            }
            c.shut = shut;
        }

        // Arcs: live for part of the cycle; standing in one hurts and throws you back
        for (int i = 0; i < arcs.Count; i++)
        {
            var a = arcs[i];
            if (a.r == null) continue;
            bool on = Mathf.Repeat(t / a.period + a.phase, 1f) < a.live;
            if (on != a.on)
            {
                a.on = on;
                a.r.enabled = on;
                if (a.l != null) a.l.enabled = on;
                if (on && live && feel != null && Vector3.Distance(feet, (a.a + a.b) * 0.5f) < 22f) feel.PlayArc();
            }
            if (!on || !live || motor.HasIFrames || t < a.hitReadyAt) continue;
            Vector3 ab = a.b - a.a;
            float k = Mathf.Clamp01(Vector3.Dot(feet - a.a, ab) / Mathf.Max(0.01f, ab.sqrMagnitude));
            Vector3 closest = a.a + ab * k;
            Vector2 flat = new Vector2(feet.x - closest.x, feet.z - closest.z);
            if (flat.magnitude > 0.6f) continue;
            if (closest.y < feet.y - 0.1f || closest.y > feet.y + capH + 0.1f) continue; // slid under it, or over it
            a.hitReadyAt = t + 0.7f;
            Vector3 v = motor.Velocity; v.y = 0f;
            Vector3 back = v.sqrMagnitude > 0.5f ? -v.normalized : Vector3.Cross(ab.normalized, Vector3.up);
            director.ReportPlayerHit(back, cfg != null ? cfg.factoryArcDamage : 12f);
            motor.AddImpulse(back * 7f + Vector3.up * 3f);
            if (feel != null) feel.Sparks(closest + Vector3.up * 0.2f, 16, false);
        }

        // Driven belts: on one, grounded, it carries you
        if (live && motor.Grounded)
        {
            // The push matches the friction that is actually on: a slide bleeds at 0.6, not 8, so pushing at 8 made a belt a rocket
            float friction = motor.Sliding ? (cfg != null ? cfg.slideFriction : 0.6f) : (cfg != null ? cfg.groundFriction : 8f);
            float beltCap = (cfg != null ? cfg.baseRunSpeed : 8f);
            for (int i = 0; i < belts.Count; i++)
            {
                var b = belts[i];
                Vector3 local = b.inv * (feet - b.centre);
                if (Mathf.Abs(local.x) > b.half.x || Mathf.Abs(local.z) > b.half.z || local.y < -0.35f || local.y > b.half.y) continue;
                Vector3 vh = motor.Velocity; vh.y = 0f;
                if (Vector3.Dot(vh, b.vel.normalized) < b.vel.magnitude + beltCap) motor.AddImpulse(b.vel * (friction * dt));   // it carries you to its speed, never past it
                break;
            }
        }
    }
}
