using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// The opening, behind your own eyes. The first level starts in Ward 7 of the
/// Elevation Programme, where the company has been trialling a techno-organic
/// vector on one family: three pods across the aisle from yours, taken by the
/// rot — ELEVATED, the record says; HOLLOW, the rot says when the record
/// glitches — and you in yours, the one who kept the vector and came out of it
/// faster than anything they had measured. Thirty-two seconds you can skip
/// after the first, and the camera never leaves your eyes: a company card, the
/// lids coming open on the three of them through your own glass, the father,
/// the mother and the sibling each in turn — the look steered to each beat by
/// a nudge the mouse can always fight — your own hands raised into the view
/// with the vector waking in them, the ward's board flipping from TRIAL to
/// RELEASE, and then you do not wait for the door: both hands on the glass,
/// the veins flaring, the right fist through it. The glass goes and the body is
/// yours: the motor comes on, the clock starts, the title lands, and you walk
/// out past them yourself, the look nudged to each as you pass and then
/// forward. The record's captions leak three times: the rot has the last word
/// on consent, on what ELEVATED means, and on containment.
/// Built by LevelSections.Ward; runs the first frame the player exists.
/// </summary>
public class OpeningCutscene : MonoBehaviour
{
    /// True while the opening owns the body: from the first frame until the glass goes
    public static bool Active { get; private set; }

    /// The four pods' floor points, 0038 → 0041 (the last one is yours, where you stand)
    public Vector3[] pods;
    /// The tag over each pod, and the board over the way out
    public TextMesh[] tags;
    public TextMesh board;
    /// The family in theirs (0038, 0039, 0040), and the rot's red light in each of their pods
    public PlayerAvatar[] family;
    public Light[] familyLights;
    /// Your pod's door, and its glass: both go when you do
    public BreakableGlass door;
    public GameObject podGlass;
    public Light podLight;
    /// The ward's lamps, dark until the release
    public Light[] lamps;
    public float floorY;
    public Vector3 exit;

    const float Length = 32f;
    const float Card0End = 3f, PassEnd = 8.5f, FatherEnd = 12f, MotherEnd = 15.6f, SiblingEnd = 18.2f, WakeEnd = 23.2f, BoardEnd = 26.2f;
    const float BustAt = 28.4f, GlassAt = BustAt + 0.21f, HandsBackAt = 29.3f;
    const float LeakA = 9.6f, LeakB = 15f, LeakHold = 1.6f, Flashes = 0.18f, LeakC = GlassAt, LeakCHold = 2.4f;
    const float BoardAt = 24f, LampsAt = 24.2f;
    const float LidsOpen = 1.6f;    // the eyes come open over this, after the card

    bool played, done, bust, tail, handed, skipped;
    float t, cardUntil = -10f;
    float nextBeat = -10f, nextSpark = -10f, nextCrack = 27.2f;
    bool boardFlipped, lampsOn, tagsHollow;
    float glow = 0.12f, podFlash;
    PlayerLook look;
    PlayerMotor motor;
    GrappleAbility grapple;
    Camera cam;
    AudioSource sfx;
    // The hands: the gauntlets, held by the opening until the punch is through
    FistRig fists;
    Transform gauntlets, handR, handL;
    Renderer[] knuckles;
    Material[] knucklesWere;
    Material vein;
    Light vectorLight;
    string levelTitle = "", levelSubtitle = "";
    string lastCaption;
    float captionAt, captionW;
    GUIStyle titleStyle, subStyle, hintStyle, capStyle, brandStyle, docStyle;

    void OnDisable()
    {
        if (Active) End();
        HandBack();
        if (look != null) look.ClearSteer();
    }

    void Start()
    {
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.spatialBlend = 0f;
        if (LevelCatalog.TryFind(SceneManager.GetActiveScene().name, out var info))
        {
            levelTitle = info.title;
            levelSubtitle = info.subtitle;
        }
    }

    void Update()
    {
        if (done) return;
        if (!played)
        {
            var d = CombatDirector.Instance;
            if (d == null || d.motor == null) return; // the thumbnail build has no player: nothing plays
            Begin(d.motor);
            return;
        }
        if (PauseMenu.IsPaused || (!Active && !tail)) return;

        t += Time.deltaTime;
        Step();

        if (Active)
        {
            // Any key or click after the first second skips straight to the glass going — not Esc, that is the pause's
            if (t > 1f)
            {
                var kb = Keyboard.current;
                var mouse = Mouse.current;
                bool skip = (kb != null && kb.anyKey.wasPressedThisFrame && !kb.escapeKey.wasPressedThisFrame) || (mouse != null && mouse.leftButton.wasPressedThisFrame);
                if (skip) End();
            }
            if (Active && t >= GlassAt) End(); // the fist is through: the glass goes and the body is yours
        }
        else if (t >= Length) Finish();
    }

    void Begin(PlayerMotor m)
    {
        played = true;
        Active = true;
        t = 0f;
        motor = m;
        look = FindFirstObjectByType<PlayerLook>();
        grapple = FindFirstObjectByType<GrappleAbility>();
        cam = look != null ? look.cam : Camera.main;
        fists = cam != null ? cam.GetComponent<FistRig>() : null;
        if (grapple != null) { grapple.Cancel(); grapple.enabled = false; }
        if (motor != null)
        {
            if (pods != null && pods.Length > 3) motor.Teleport(pods[3] + Vector3.up * 0.02f); // on your pod's floor: the eyes where they wake
            motor.enabled = false;
        }
        // The look stays on: the mouse is free from the first frame, and the beats only steer it
        if (lamps != null) foreach (var l in lamps) if (l != null) l.enabled = false;
        // The rot in the family's pods is infection red (red = danger; never violet): the cutscene owns the colour
        if (familyLights != null) foreach (var l in familyLights) if (l != null) l.color = RotLight;
        if (cam != null)
        {
            // The vector's light on your hands and the glass around you, as it wakes
            var go = new GameObject("VectorLight");
            go.transform.SetParent(cam.transform, false);
            go.transform.localPosition = new Vector3(0.05f, -0.22f, 0.5f);
            vectorLight = go.AddComponent<Light>();
            vectorLight.type = LightType.Point;
            vectorLight.range = 3f;
            vectorLight.intensity = 0f;
            vectorLight.color = new Color(1f, 0.6f, 0.2f);
            vectorLight.shadows = LightShadows.None;
        }
        MusicDirector.Hush = true;
        Play("click", 0.5f, 1.1f);
    }

    // ------------------------------------------------------------ the beats

    void Step()
    {
        if (cam == null || pods == null || pods.Length < 4) return;
        float dt = Time.deltaTime;
        var feel = GameFeel.Instance;

        TakeHands();
        Glow(dt);
        if (Active) SteerBeat();
        else SteerTail();
        if (!handed)
        {
            PoseHands();
            if (tail && t >= HandsBackAt) HandBack(); // the punch is home: the rigs take the hands back where they left them
        }
        AnimateFamily(dt);

        // The heartbeat while the vector wakes
        if (t >= SiblingEnd && t < WakeEnd && t >= nextBeat)
        {
            nextBeat = t + Mathf.Lerp(1f, 0.55f, (t - SiblingEnd) / (WakeEnd - SiblingEnd));
            Play("sub", 0.45f, 0.8f);
        }
        // The record's tags tick HOLLOW through the second leak, then say ELEVATED again
        bool hollow = t >= LeakB && t < LeakB + LeakHold;
        if (hollow != tagsHollow)
        {
            tagsHollow = hollow;
            for (int i = 0; i < 3 && tags != null && i < tags.Length; i++)
                if (tags[i] != null) tags[i].text = hollow ? TagHollow[i] : TagElevated[i];
        }
        if (!boardFlipped && t >= BoardAt)
        {
            boardFlipped = true;
            if (board != null) board.text = "GUEST 0041  ·  RELEASED FOR EVALUATION";
            Play("bell", 0.5f, 1.15f);
        }
        if (!lampsOn && t >= LampsAt)
        {
            lampsOn = true;
            if (lamps != null) foreach (var l in lamps) if (l != null) l.enabled = true;
            Play("click", 0.5f, 0.9f);
        }
        if (lampsOn && lamps != null)
            foreach (var l in lamps) if (l != null) l.intensity = 2.4f * (0.75f + 0.25f * Mathf.PerlinNoise(t * 9f, l.transform.position.x));

        // Bracing on the glass: the vector flares at the fists, the glass cracks three times under them
        if (Active && t >= BoardEnd && t < BustAt)
        {
            if (t >= nextSpark && t > BoardEnd + 1f && feel != null)
            {
                nextSpark = t + 0.22f;
                feel.Sparks(cam.transform.TransformPoint(new Vector3(0.22f, -0.16f, 1.02f)), 6, false);
                feel.Sparks(cam.transform.TransformPoint(new Vector3(-0.22f, -0.16f, 1.02f)), 4, false);
            }
            if (t >= nextCrack)
            {
                nextCrack += 0.45f;
                Play("glass", 0.35f, 1.4f);
                if (look != null) look.AddShake(0.35f);
                podFlash = 2.8f;
            }
        }
    }

    /// The vector's light, on the ramp the body ran: asleep, waking on the heartbeat, lit, flaring on the glass,
    /// white-hot through it and settling — in the hands' veins, on the light they throw, and in your pod's own lamp
    void Glow(float dt)
    {
        if (t < SiblingEnd) glow = 0.12f + 0.04f * Mathf.Sin(t * 1.7f);
        else if (t < WakeEnd)
        {
            float k = (t - SiblingEnd) / (WakeEnd - SiblingEnd);
            glow = Mathf.Lerp(0.12f, 0.6f, k) + 0.08f * Mathf.Sin(t * (2f + 6f * k));
        }
        else if (t < BoardEnd) glow = 0.6f + 0.1f * Mathf.Sin(t * 8f);
        else if (t < BustAt)
        {
            float k = (t - BoardEnd) / (BustAt - BoardEnd);
            glow = Mathf.Lerp(0.6f, 1.6f, k * k) + 0.12f * Mathf.Sin(t * 14f);
        }
        else glow = Mathf.Lerp(2f, 1f, Mathf.Clamp01((t - BustAt) / 1.5f)) + 0.1f * Mathf.Sin(t * 9f);

        if (vein != null) vein.SetColor("_BaseColor", FistRig.VectorAt(glow));
        if (vectorLight != null)
        {
            vectorLight.intensity = Mathf.Max(0f, glow - 0.1f) * 1.6f;
            vectorLight.color = Color.Lerp(new Color(1f, 0.6f, 0.2f), Color.white, Mathf.Clamp01(glow - 1f));
        }
        if (podLight != null)
        {
            // Your pod's own light takes the vector's colour as it wakes, and jumps on every crack
            podFlash = Mathf.MoveTowards(podFlash, 0f, dt * 8f);
            podLight.color = Color.Lerp(new Color(1f, 0.9f, 0.7f), new Color(1f, 0.7f, 0.3f), Mathf.Clamp01(glow) * 0.8f);   // lamp cream to the vector's amber
            podLight.intensity = 1.2f + 1.4f * Mathf.Clamp(glow, 0f, 1.6f) + podFlash;
        }
    }

    // ------------------------------------------------------------ the steer

    /// The gentle pull on the look toward a point in the world: PlayerLook's own additive steer, retargeted every
    /// frame the beat lasts with a short window, so it follows a moving face and stops on its own when the beats do.
    /// `strength` is the look's 0..1: the mouse always wins a fight with it, and it finishes the turn when the mouse lets go
    void Steer(Vector3 point, float strength)
    {
        if (look != null) look.Steer(point, strength, 0.25f);
    }

    /// The look, steered to each beat: across the three as the lids open, each of them in turn, your own hands,
    /// the board, the glass in front of you
    void SteerBeat()
    {
        if (t < Card0End) return;
        Vector3 father = Figure(0, pods[0] + Vector3.up * 1.6f), mother = Figure(1, pods[1] + Vector3.up * 1.6f), sibling = Figure(2, pods[2] + Vector3.up * 1.2f);
        if (t < PassEnd)
        {
            // The eyes come open on the sibling and drift across the mother to the father
            float k = Mathf.SmoothStep(0f, 1f, (t - Card0End) / (PassEnd - Card0End));
            Steer(k < 0.5f ? Vector3.Lerp(sibling, mother, k * 2f) : Vector3.Lerp(mother, father, (k - 0.5f) * 2f), 0.35f);
        }
        else if (t < FatherEnd) Steer(father, 0.4f);
        else if (t < MotherEnd) Steer(mother, 0.4f);
        else if (t < SiblingEnd)
        {
            // Small in a pod built for someone taller: the look finds them from the cap down
            float k = Mathf.SmoothStep(0f, 1f, (t - MotherEnd) / (SiblingEnd - MotherEnd));
            Steer(Vector3.Lerp(pods[2] + Vector3.up * 2.6f, sibling, k), 0.4f);
        }
        else if (t < WakeEnd) Steer(HandsPoint(), 0.4f);                              // your own hands, the vector waking in them
        else if (t < BoardEnd) Steer(new Vector3(0f, floorY + 6.4f, exit.z), 0.4f);   // the board over the way out
        else Steer(pods[3] + new Vector3(0f, 1.45f, 1.05f), 0.45f);                    // the glass, a hand's reach ahead
    }

    /// Once the body is yours: a nudge to whichever of them you are passing, then forward to the way out
    void SteerTail()
    {
        if (motor == null || family == null) return;
        Vector3 feet = motor.CurrPosition;
        float rowZ = Mathf.Min(pods[0].z, pods[2].z);
        if (feet.z < rowZ + 0.8f)
        {
            int near = -1;
            float best = float.PositiveInfinity;
            for (int i = 0; i < 3 && i < family.Length; i++)
            {
                if (family[i] == null) continue;
                Vector3 d = pods[i] - feet; d.y = 0f;
                if (d.sqrMagnitude < best) { best = d.sqrMagnitude; near = i; }
            }
            if (near >= 0) Steer(family[near].Face, 0.25f);
        }
        else Steer(new Vector3(0f, floorY + 2.4f, exit.z), 0.2f);
    }

    /// A point ahead and below the view, where the hands are at any pitch (they turn with the view)
    Vector3 HandsPoint()
    {
        Vector3 fwd = Quaternion.Euler(0f, look != null ? look.Yaw : 0f, 0f) * Vector3.forward;
        return cam.transform.position + fwd * 0.8f + Vector3.down * 0.55f;
    }

    // ------------------------------------------------------------ the hands

    /// A pose in the camera's space
    struct VmPose
    {
        public Vector3 pos;
        public Quaternion rot;
        public VmPose(float x, float y, float z, float rx, float ry, float rz) { pos = new Vector3(x, y, z); rot = Quaternion.Euler(rx, ry, rz); }
        public VmPose(Vector3 p, Quaternion r) { pos = p; rot = r; }
    }

    static VmPose Lerp(VmPose a, VmPose b, float k) => new VmPose(Vector3.Lerp(a.pos, b.pos, k), Quaternion.Slerp(a.rot, b.rot, k));

    /// The left hand is the right one across the view
    static VmPose Mirror(VmPose p)
    {
        Vector3 e = p.rot.eulerAngles;
        return new VmPose(new Vector3(-p.pos.x, p.pos.y, p.pos.z), Quaternion.Euler(e.x, -e.y, -e.z));
    }

    // The gauntlets: low in the frame while you hang, up in front of the face for the wake, the FistRig's own rest,
    // wind-up and strike for the glass (so the hand-back lands where that rig expects them)
    static readonly VmPose FistSlack = new VmPose(0.38f, -0.5f, 0.55f, 30f, -18f, 8f);
    static readonly VmPose FistRaised = new VmPose(0.17f, -0.1f, 0.44f, -58f, -22f, 14f);
    static readonly VmPose FistRest = new VmPose(0.36f, -0.38f, 0.56f, 12f, -18f, 6f);
    static readonly VmPose FistBrace = new VmPose(0.22f, -0.16f, 0.9f, -10f, -10f, 0f);
    static readonly VmPose FistWindup = new VmPose(0.42f, -0.34f, 0.44f, 0f, -30f, 10f);
    static readonly VmPose FistStrike = new VmPose(0.09f, -0.1f, 0.98f, -4f, -6f, 0f);
    /// The hands are the FistRig's gauntlets, built in that rig's first Update and hidden while the opening is on: the
    /// opening switches the rig off and takes the root under the camera, the knuckle edges carrying the vector
    void TakeHands()
    {
        if (handed || gauntlets != null || fists == null || cam == null) return;
        var root = cam.transform.Find("Gauntlets");
        if (root == null) return;
        fists.enabled = false;
        gauntlets = root;
        handR = root.Find("HandR");
        handL = root.Find("HandL");
        vein = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        vein.SetColor("_BaseColor", FistRig.VectorAt(glow));
        var edges = new List<Renderer>();
        Knuckle(handR, edges);
        Knuckle(handL, edges);
        knuckles = edges.ToArray();
        knucklesWere = new Material[knuckles.Length];
        for (int i = 0; i < knuckles.Length; i++)
        {
            knucklesWere[i] = knuckles[i].sharedMaterial;
            knuckles[i].sharedMaterial = vein;
        }
        root.gameObject.SetActive(true);
    }

    /// The knuckle edge is the one thin plate across the front of the fist
    static void Knuckle(Transform hand, List<Renderer> into)
    {
        var fist = hand != null ? hand.Find("Fist") : null;
        if (fist == null) return;
        for (int i = 0; i < fist.childCount; i++)
        {
            var c = fist.GetChild(i);
            if (c.localScale.z >= 0.02f) continue;
            var r = c.GetComponent<Renderer>();
            if (r != null) into.Add(r);
        }
    }

    /// The hands through the opening: slack and low while you hang, raised into the view as the vector wakes, down
    /// while you read the board, both on the glass as you brace, the right drawn back and driven through it, and
    /// home to the rig's own rest for the hand-back. The blade runs the same beats in its own poses
    void PoseHands()
    {
        VmPose slack = FistSlack, raised = FistRaised, rest = FistRest;
        VmPose brace = FistBrace, windup = FistWindup, strike = FistStrike;
        VmPose r, l;
        float breathR = Mathf.Sin(t * 1.9f) * 0.006f, breathL = Mathf.Sin(t * 1.9f + 0.8f) * 0.006f;
        if (t < SiblingEnd)
        {
            r = slack; l = Mirror(slack);
            r.pos.y += breathR; l.pos.y += breathL;
        }
        else if (t < WakeEnd)
        {
            // Up into the view, and turning a little, the way you look at your own hands
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - SiblingEnd) / 1.8f));
            VmPose up = raised;
            up.rot = raised.rot * Quaternion.Euler(Mathf.Sin(t * 0.9f) * 7f, Mathf.Sin(t * 0.7f) * 9f, 0f);
            r = Lerp(slack, up, k); l = Mirror(r);
            r.pos.y += breathR * 0.6f; l.pos.y += breathL * 0.6f;
        }
        else if (t < BoardEnd)
        {
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - WakeEnd) / 1.2f));
            r = Lerp(raised, rest, k); l = Mirror(r);
            r.pos.y += breathR; l.pos.y += breathL;
        }
        else if (t < BustAt)
        {
            // Both on the glass, the push growing: the shudder of arms driving at something that does not give
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - BoardEnd) / 0.7f));
            float push = Mathf.Clamp01((t - BoardEnd - 0.7f) / (BustAt - BoardEnd - 0.7f));
            r = Lerp(rest, brace, k); l = Mirror(r);
            Vector3 shudder = new Vector3(Mathf.Sin(t * 61f), Mathf.Sin(t * 47f), Mathf.Sin(t * 53f) * 0.5f) * (0.004f + 0.012f * push);
            r.pos += shudder; l.pos -= shudder;
        }
        else
        {
            // THE BREAK-OUT. The right draws back for a tenth and drives through — the glass goes ON THE IMPACT, not
            // before — holds through for a beat with the shards falling, and both hands come home
            float since = t - BustAt;
            VmPose braceL = Mirror(brace), restL = Mirror(rest);
            if (since < 0.12f) { r = Lerp(brace, windup, Mathf.SmoothStep(0f, 1f, since / 0.12f)); l = braceL; }
            else if (since < 0.26f)
            {
                float k = 1f - (since - 0.12f) / 0.14f;
                r = Lerp(windup, strike, 1f - k * k * k * k); l = braceL;
            }
            else if (since < 0.55f)
            {
                float k = (since - 0.26f) / 0.29f;
                r = strike;
                r.pos += new Vector3(Mathf.Sin(since * 90f), Mathf.Sin(since * 70f), -0.6f) * (0.02f * (1f - k));
                l = Lerp(braceL, restL, Mathf.SmoothStep(0f, 1f, k));
            }
            else
            {
                r = Lerp(strike, rest, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((since - 0.55f) / (HandsBackAt - BustAt - 0.55f))));
                l = restL;
            }
        }
        if (handR != null) { handR.localPosition = r.pos; handR.localRotation = r.rot; }
        if (handL != null) { handL.localPosition = l.pos; handL.localRotation = l.rot; }
    }

    /// The rig takes the hands back: the gauntlets' knuckles their own again, the vector's light gone
    void HandBack()
    {
        if (handed) return;
        handed = true;
        if (fists != null) fists.enabled = true;
        if (knuckles != null)
            for (int i = 0; i < knuckles.Length; i++)
                if (knuckles[i] != null && knuckles[i].sharedMaterial == vein) knuckles[i].sharedMaterial = knucklesWere[i];
        if (vein != null) Destroy(vein);
        vein = null;
        if (vectorLight != null) Destroy(vectorLight.gameObject);
        vectorLight = null;
    }

    // ------------------------------------------------------------ the family, the glass

    /// The family: hanging in their pods, the rot pulsing red through them — harder while the record leaks
    void AnimateFamily(float dt)
    {
        if (family == null) return;
        bool leaking = (t >= LeakA && t < LeakA + LeakHold) || (t >= LeakB && t < LeakB + LeakHold) || (t >= LeakC && t < LeakC + LeakCHold);
        for (int i = 0; i < family.Length; i++)
        {
            var f = family[i];
            if (f == null) continue;
            float tt = t + i * 1.7f;
            f.Animate(PlayerAvatar.Act.Hollow, 1f, tt, dt);
            f.SetGlow((leaking ? 0.9f : 0.5f) + 0.2f * Mathf.Sin(tt * (leaking ? 9f : 1.4f)));
            if (familyLights != null && i < familyLights.Length && familyLights[i] != null)
                familyLights[i].intensity = Mathf.MoveTowards(familyLights[i].intensity, leaking ? 4.5f + Mathf.Sin(tt * 18f) : 1.4f, dt * 14f);
        }
    }

    /// A family member's face, or a fallback point if the pod is empty
    Vector3 Figure(int i, Vector3 fallback) => family != null && i < family.Length && family[i] != null ? family[i].Face : fallback;

    /// The glass goes: the door and the pod's own glass, in shards, a ring, a flash at the pane, the vector white-hot,
    /// the view kicked
    void Bust()
    {
        bust = true;
        Vector3 pod = pods[3];
        var feel = GameFeel.Instance;
        if (door != null) door.TryBreak(Vector3.forward * 60f, Vector3.back, door.transform.position);
        if (podGlass != null)
        {
            var r = podGlass.GetComponent<Renderer>();
            if (r != null) r.enabled = false;
            GlassShards(pod + Vector3.up * 1.65f, 1.1f, 3f, 34);
        }
        ShockRing.Spawn(pod + Vector3.up * 0.05f, 0.6f, 5f, 0.5f, false);
        MuzzleFlash.Spawn(pod + new Vector3(0f, 1.3f, 1f), 3f);
        if (feel != null)
        {
            feel.Sparks(pod + new Vector3(0f, 1.3f, 1.05f), 40, false);
            if (cam != null) feel.Sparks(cam.transform.TransformPoint(new Vector3(0.09f, -0.1f, 1f)), 30, false);
            feel.PlayBossCrack();
            feel.PlayShatter();
        }
        podFlash = 5f;
        if (look != null) { look.AddShake(1.5f); look.AddFovKick(7f); }
    }

    /// The pod's glass in pieces, thrown out from the cylinder and falling
    static void GlassShards(Vector3 centre, float radius, float height, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            Vector3 n = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard.name = "PodShard";
            shard.layer = 2;
            Object.Destroy(shard.GetComponent<Collider>());
            shard.transform.position = centre + n * radius + Vector3.up * Random.Range(-height * 0.5f, height * 0.5f);
            shard.transform.rotation = Random.rotation;
            shard.transform.localScale = new Vector3(Random.Range(0.15f, 0.45f), Random.Range(0.15f, 0.45f), 0.04f);
            shard.GetComponent<MeshRenderer>().sharedMaterial = LevelKit.GlassPane;
            var rb = shard.AddComponent<Rigidbody>();
            rb.mass = 0.05f;
            rb.linearVelocity = n * Random.Range(4f, 9f) + Vector3.forward * 3f + Vector3.up * Random.Range(1f, 4f);
            rb.angularVelocity = Random.insideUnitSphere * 15f;
            shard.AddComponent<ShardFade>();
        }
    }

    /// The rot's light: infection red, (2.0, 0.3, 0.25) at half — the intensity ramp carries the brightness
    static readonly Color RotLight = new Color(1f, 0.15f, 0.125f);
    // The Ward's pod tags in the House's register (the Ward builds them the same way; the hand-over must not flip them back)
    static readonly string[] TagElevated = { "0038  ·  ACCOMMODATED", "0039  ·  ACCOMMODATED", "0040  ·  ACCOMMODATED" };
    static readonly string[] TagHollow = { "0038  ·  NO LONGER WITH US", "0039  ·  NO LONGER WITH US", "0040  ·  NO LONGER WITH US" };

    void Play(string bucket, float vol, float pitch)
    {
        var clip = GameFeel.Clip(bucket);
        if (clip == null || sfx == null) return;
        sfx.pitch = pitch;
        sfx.PlayOneShot(clip, vol * GameSettings.SfxVolume);
    }

    /// The body is yours: the glass gone, the motor on, the clock started, the title down. The record's last line
    /// and the look's last nudges play out behind it while you walk
    void End()
    {
        if (!Active) return;
        Active = false;
        tail = true;
        MusicDirector.Hush = false;
        if (!bust) Bust(); // skipped early: the glass still goes
        if (t < GlassAt) { t = GlassAt; skipped = true; } // a skip lands on the glass going: the third leak, and the hands come straight back
        if (motor != null) motor.enabled = true;
        if (grapple != null) grapple.enabled = true;
        if (skipped) HandBack();
        if (lamps != null) foreach (var l in lamps) if (l != null) { l.enabled = true; l.intensity = 2.2f; }
        lampsOn = true;
        if (board != null) board.text = "GUEST 0041  ·  RELEASED FOR EVALUATION";
        boardFlipped = true;
        if (tags != null) for (int i = 0; i < 3 && i < tags.Length; i++) if (tags[i] != null) tags[i].text = TagElevated[i];
        if (LevelTimer.Instance != null) LevelTimer.Instance.Restart();
        MenuStyle.KickAt = Time.unscaledTime;
        cardUntil = Time.unscaledTime + CardLength;
    }

    /// The record has said its last word: the overlay goes, and the ward is the ward
    void Finish()
    {
        done = true;
        tail = false;
        HandBack();
        if (look != null) look.ClearSteer();
        if (lamps != null) foreach (var l in lamps) if (l != null) { l.enabled = true; l.intensity = 2.2f; }
        if (podLight != null) { podLight.intensity = 1.2f; podLight.color = new Color(1f, 0.9f, 0.7f); }
    }

    // ------------------------------------------------------------ the cards

    const float CardLength = 3.8f;

    static void Fill(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
    }

    static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);

    /// What the record says at this second (literals only: nothing is built per frame)
    static string CaptionAt(float at)
    {
        if (at < 3.4f) return null;
        if (at < 6.6f) return "A PARTY OF FOUR  ·  GUESTS 0038 – 0041.";
        if (at < 9.6f) return "CONSENT ON FILE.";
        if (at < 11.4f) return null;
        if (at < 13.4f) return "GUEST 0038  ·  ACCOMMODATED.";
        if (at < 15f) return "GUEST 0039  ·  ACCOMMODATED.";
        if (at < 16.8f) return null;
        if (at < 18.2f) return "GUEST 0040  ·  ACCOMMODATED.";
        if (at < 18.6f) return null;
        if (at < 21f) return "ONE GUEST RETAINED THE VECTOR.";
        if (at < 23.4f) return "MOTOR RESPONSE: 4 000 % OF BASELINE.";
        if (at < 23.7f) return null;
        if (at < 26.4f) return "EVALUATION BEGINS.";
        if (at < 26.7f) return null;
        if (at < 31f) return "ALL ROOMS OCCUPIED.";
        return null;
    }

    void OnGUI()
    {
        float now = Time.unscaledTime;
        bool title = !Active && now < cardUntil;
        if (!Active && !tail && !title) return;
        if (PauseMenu.IsPaused) return; // the pause veil covers everything
        MenuStyle.EnsureStyles();
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(MenuStyle.Title) { fontSize = 78, alignment = TextAnchor.LowerLeft };
            subStyle = new GUIStyle(MenuStyle.Brand) { fontSize = 18, alignment = TextAnchor.UpperLeft };
            hintStyle = new GUIStyle(MenuStyle.Sub) { alignment = TextAnchor.LowerRight };
            capStyle = new GUIStyle(MenuStyle.Tagline) { fontSize = 24, alignment = TextAnchor.MiddleLeft };
            brandStyle = new GUIStyle(MenuStyle.Brand) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
            docStyle = new GUIStyle(MenuStyle.Doc) { alignment = TextAnchor.MiddleCenter };
            titleStyle.normal.textColor = subStyle.normal.textColor = hintStyle.normal.textColor = capStyle.normal.textColor
                = brandStyle.normal.textColor = docStyle.normal.textColor = Color.white;
        }
        UIScale.Begin();
        GUI.depth = -20;
        float w = UIScale.W, h = UIScale.H;

        if (Active || tail)
        {
            // The lids: shut under the card, open on a flicker after it, and the bars let go as the glass does
            float release = tail ? Mathf.Clamp01((t - GlassAt) / 0.6f) : 0f;
            float lid = 0.5f;
            if (t >= Card0End)
            {
                float open = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - Card0End) / LidsOpen));
                float flick = (1f - open) * Mathf.Max(0f, Mathf.Sin(t * 23f)) * 0.35f;
                lid = Mathf.Lerp(0.5f, 0.13f, Mathf.Clamp01(open - flick));
            }
            DrawLetterbox(w, h, h * Mathf.Lerp(lid, 0f, release), 1f - release);
            if (t < Card0End) DrawCompanyCard(w, h);
            else DrawCaptions(w, h, 1f - release);
            if (Active && t > 1f)
            {
                GUI.color = MenuStyle.Sick;
                UIScale.Label(new Rect(w - 340f, h * 0.87f - 34f, 300f, 24f), "any key · skip", hintStyle);
                GUI.color = Color.white;
            }
        }
        if (title) DrawTitleCard(w, h, now);
        GUI.color = Color.white;
        UIScale.End();
    }

    /// The letterbox — the lids — with a thin brass rule along each inner edge
    static void DrawLetterbox(float w, float h, float bar, float a)
    {
        if (bar <= 0f || a <= 0f) return;
        Fill(new Rect(0f, 0f, w, bar), A(Color.black, a));
        Fill(new Rect(0f, h - bar, w, bar), A(Color.black, a));
        Color hair = A(MenuStyle.Brass, 0.5f * a);
        Fill(new Rect(0f, bar - 1f, w, 1f), hair);
        Fill(new Rect(0f, h - bar, w, 1f), hair);
        GUI.color = Color.white;
    }

    /// The black, and the company's card on it: who is doing this, and what they call it
    void DrawCompanyCard(float w, float h)
    {
        float a = Mathf.Min(Mathf.Clamp01(t / 0.3f), Mathf.Clamp01((Card0End - t) / 0.4f));
        Fill(new Rect(0f, 0f, w, h), Color.black);
        float cy = h * 0.46f;
        GUI.color = A(MenuStyle.Paper, a);
        UIScale.Label(new Rect(0f, cy - 70f, w, 30f), LevelKit.CompanyFull, brandStyle);
        MenuStyle.DoubleRule(w * 0.5f - 130f, cy - 34f, 260f, A(MenuStyle.Gold, a));   // the double brass rule under the company's name
        // The programme's name lands misregistered and pulls into register
        float o = 1f + 6f * (1f - Mathf.Clamp01((t - 0.2f) / 0.5f));
        Rect name = new Rect(0f, cy - 26f, w, 60f);
        var big = MenuStyle.Tagline;
        int keep = big.fontSize;
        big.fontSize = 40;
        GUI.color = A(MenuStyle.Rot, 0.7f * a);
        UIScale.Label(new Rect(name.x - o, name.y, name.width, name.height), "ACCOMMODATION PROGRAMME  ·  WARD 7", big);
        GUI.color = A(MenuStyle.DeepTeal, 0.7f * a);
        UIScale.Label(new Rect(name.x + o, name.y + 1f, name.width, name.height), "ACCOMMODATION PROGRAMME  ·  WARD 7", big);
        GUI.color = A(MenuStyle.Paper, a);
        UIScale.Label(name, "ACCOMMODATION PROGRAMME  ·  WARD 7", big);
        big.fontSize = keep;
        GUI.color = A(MenuStyle.Sick, a);
        UIScale.Label(new Rect(0f, cy + 40f, w, 20f), "HC-7   ·   GUEST REGISTER   ·   WITH THE COMPLIMENTS OF THE HOUSE", docStyle);
        MenuStyle.Seal(new Vector2(w * 0.5f, cy + 98f), 56f, A(MenuStyle.Brass, 0.85f * a));   // the company's seal on its record
        GUI.color = Color.white;
    }

    /// The record's line, printed onto the lower bar as it is read — and three times the rot has its own line. The
    /// strip keeps the bar's seat after the bars go, so the last line prints where the others did
    void DrawCaptions(float w, float h, float a)
    {
        float bar = h * 0.13f;
        GUI.color = A(MenuStyle.Sick, 0.9f * a);
        UIScale.Label(new Rect(w * 0.06f, bar - 30f, 500f, 22f), "GUEST RECORD   ·   0041   ·   WARD 7", MenuStyle.Doc);
        MenuStyle.ChevronRule(new Rect(w * 0.06f + 320f, bar - 24f, 90f, 8f), A(MenuStyle.Gold, 0.7f * a));

        // The leaks: the caption's panel flashes rot and ink, then holds the rot's line before the record's returns
        float leakAt = t >= LeakA && t < LeakA + LeakHold ? LeakA : t >= LeakB && t < LeakB + LeakHold ? LeakB : t >= LeakC && t < LeakC + LeakCHold ? LeakC : -1f;
        string leak = leakAt == LeakA ? "NO ONE ASKED THEM." : leakAt == LeakB ? "THREE GUESTS HOLLOW." : leakAt == LeakC ? "CONTAINMENT FAILED." : null;
        string cap = leak ?? CaptionAt(t);
        // The line changes on the Repaint pass only, so Layout and Repaint never disagree about what is printed
        if (!ReferenceEquals(cap, lastCaption) && Event.current.type == EventType.Repaint)
        {
            lastCaption = cap;
            captionAt = t;
            captionW = cap != null ? MenuStyle.Measure(capStyle, cap).x : 0f;
            if (cap != null) Play(leak != null ? "warn" : "tick", leak != null ? 0.5f : 0.3f, leak != null ? 0.8f : 1.3f);
        }
        if (cap == null || !ReferenceEquals(cap, lastCaption)) return;

        float y = h - bar + 18f;
        if (leak != null)
        {
            float since = t - leakAt;
            int state = since < Flashes ? MenuStyle.RotState((int)(since / 0.03f)) : 1;
            Rect panel = new Rect(w * 0.5f - captionW * 0.5f - 18f + Mathf.Sin(t * 131f) * 3f, y - 4f, captionW + 36f, 38f);
            Fill(panel, state == 0 ? A(MenuStyle.Rot, 0.9f) : A(MenuStyle.Ink, 0.9f));
            GUI.color = state == 0 ? MenuStyle.Ink : MenuStyle.Rot;
            UIScale.Label(new Rect(panel.x + 18f, y, captionW + 4f, 30f), leak, capStyle);
            GUI.color = Color.white;
            return;
        }
        // The record's caption prints itself left to right, a cursor at its edge, on an ink strip that grows with it.
        // No GUI group here: the text is drawn whole and the bar's own black covers what is not printed yet (a group
        // would seat the scaled text wrong at any window size but the reference one)
        float k = Mathf.Clamp01((t - captionAt) / 0.35f);
        float x = w * 0.5f - captionW * 0.5f;
        float shown = Mathf.Max(1f, captionW * k);
        Fill(new Rect(x - 18f, y - 4f, shown + 36f, 38f), A(MenuStyle.Ink, 0.75f));
        Fill(new Rect(x - 18f, y - 4f, 5f, 38f), A(MenuStyle.Gold, 0.9f));
        GUI.color = MenuStyle.Paper;
        UIScale.Label(new Rect(x, y, captionW + 8f, 30f), cap, capStyle);
        if (k < 1f)
        {
            Fill(new Rect(x + shown + 2f, y - 6f, captionW - shown + 60f, 42f), Color.black);
            Fill(new Rect(x + shown + 2f, y + 5f, 8f, 20f), A(MenuStyle.Gold, 0.9f));
        }
        GUI.color = Color.white;
    }

    /// The level's title, the frame the glass goes: the name thrown in from the left on a gold slab, the tower under it
    void DrawTitleCard(float w, float h, float now)
    {
        float age = CardLength - (cardUntil - now);
        float card = Mathf.Clamp01(age / 0.35f);
        float fade = Mathf.Clamp01((cardUntil - now) / 0.6f);
        float ease = MenuStyle.Ease(card);
        float a = Mathf.Clamp01(ease) * fade;
        float x = w * 0.08f - (1f - ease) * 260f;
        float baseY = h * 0.78f;
        MenuStyle.TitlePlate(new Rect(x - 20f, baseY - 104f, 700f, 110f), a);   // the charcoal plate in its stepped brass frame
        Fill(new Rect(x - 10f, baseY - 92f, 10f, 118f), A(MenuStyle.Ink, a));
        Fill(new Rect(x - 14f, baseY - 96f, 10f, 118f), A(MenuStyle.Gold, a));
        float o = 1f + 9f * (1f - Mathf.Clamp01(ease));
        Rect name = new Rect(x + 6f, baseY - 100f, w * 0.75f, 92f);
        GUI.color = A(MenuStyle.Rot, 0.7f * a);
        UIScale.Label(new Rect(name.x - o, name.y, name.width, name.height), levelTitle, titleStyle);
        GUI.color = A(MenuStyle.DeepTeal, 0.7f * a);
        UIScale.Label(new Rect(name.x + o, name.y + 1f, name.width, name.height), levelTitle, titleStyle);
        GUI.color = A(MenuStyle.Paper, a);
        UIScale.Label(name, levelTitle, titleStyle);
        MenuStyle.DoubleRule(x + 6f, baseY - 8f, w * 0.45f, A(MenuStyle.Gold, a));   // the double brass rule under the name
        GUI.color = A(MenuStyle.Gold, a);
        UIScale.Label(new Rect(x + 8f, baseY - 2f, w * 0.75f, 26f), levelSubtitle, subStyle);
        GUI.color = Color.white;
    }
}
