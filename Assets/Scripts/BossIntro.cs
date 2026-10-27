using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The boss's entrance — and yours. Walking into the arena seals the door
/// behind you, takes the camera, and plays ten seconds you can skip: YOU, on
/// screen, blade in hand, from over your own shoulder as the floor opens and
/// the thing rises out of it beyond the desk; the rise from the floor, low,
/// tilting up its body to the face as it straightens and opens its arms; the
/// reverse — your face, close, the eyes narrowing, the vector waking at your
/// temple, the blade coming up into guard; the title card; and the cut back
/// behind your own eyes as the groove drops and the fight begins.
/// </summary>
public class BossIntro : MonoBehaviour
{
    public static bool Active { get; private set; }

    public Boss boss;
    public Vector3 triggerCenter, triggerSize;

    const float Length = 10.2f;
    const float Shot1End = 3.9f, Shot2End = 6.6f, Shot3End = 8.4f;
    const float RiseFrom = 0.4f, RiseFor = 4.6f;
    bool played, landed;
    float t, shake;
    PlayerLook look;
    PlayerMotor motor;
    GrappleAbility grapple;
    Camera cam;
    Vector3 camPos0;
    Quaternion camRot0;
    float fov0 = 95f;
    PlayerAvatar you;
    Vector3 youFeet, toBoss, side;
    float nextSpark;
    GUIStyle titleStyle, subStyle, hintStyle;

    void OnDisable() { if (Active) End(); }

    void Update()
    {
        if (!played)
        {
            var d = CombatDirector.Instance;
            if (d == null || d.motor == null) return;
            if (!new Bounds(triggerCenter, triggerSize).Contains(d.motor.CurrPosition + Vector3.up * 0.9f)) return;
            Begin(d.motor);
            return;
        }
        if (!Active || PauseMenu.IsPaused) return;

        t += Time.deltaTime;
        Step();

        // Any key or click after the first second skips straight to the fight
        if (t > 1f)
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool skip = (kb != null && kb.anyKey.wasPressedThisFrame) || (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (skip) End();
        }
        if (Active && t >= Length) End();
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
        if (cam != null)
        {
            camPos0 = cam.transform.position;
            camRot0 = cam.transform.rotation;
            fov0 = cam.fieldOfView;
        }
        if (grapple != null) { grapple.Cancel(); grapple.enabled = false; }
        if (look != null) look.enabled = false;
        if (motor != null) motor.enabled = false;
        boss.SealEntry();
        boss.BeginIntro();
        boss.SetRise(0f);

        // You, where you stand, facing it, the blade in your hand
        youFeet = motor.CurrPosition;
        Vector3 ground = new Vector3(boss.arenaCenter.x, boss.floorY, boss.arenaCenter.z);
        toBoss = ground - youFeet; toBoss.y = 0f;
        toBoss = toBoss.sqrMagnitude > 0.01f ? toBoss.normalized : Vector3.forward;
        side = Vector3.Cross(Vector3.up, toBoss);
        you = PlayerAvatar.Build(transform, youFeet, Quaternion.LookRotation(toBoss, Vector3.up).eulerAngles.y);
        you.SetGlow(0.35f);
        you.Blade(false); // the hands are the kit: no blade
        you.Animate(PlayerAvatar.Act.Idle, 1f, 0f, 10f);
        you.LookAt(ground + Vector3.up * 2f);
        if (BossMusic.Instance != null) BossMusic.Instance.Play(BossMusic.Section.Approach);
    }

    void Step()
    {
        if (cam == null || boss == null || boss.body == null) return;
        float dt = Time.deltaTime;
        float s = boss.body.Scale;
        Vector3 ground = new Vector3(boss.arenaCenter.x, boss.floorY, boss.arenaCenter.z);
        var feel = GameFeel.Instance;

        // The rise: the floor opens (a ring and sparks at the seam), the body comes up, and lands with a jolt
        float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - RiseFrom) / RiseFor));
        boss.SetRise(rise);
        if (t >= RiseFrom && t < RiseFrom + RiseFor)
        {
            shake = Mathf.Max(shake, 0.25f);
            if (t >= nextSpark && feel != null)
            {
                nextSpark = t + 0.14f;
                feel.Sparks(ground + new Vector3(Random.Range(-1.2f, 1.2f) * s, 0.2f, Random.Range(-1.2f, 1.2f) * s), 8, false);
            }
            if (t < RiseFrom + 0.05f) ShockRing.Spawn(ground + Vector3.up * 0.06f, 0.5f * s, 3.5f * s, 0.9f, false);
        }
        if (!landed && t >= RiseFrom + RiseFor)
        {
            landed = true;
            shake = 1f;
            ShockRing.Spawn(ground + Vector3.up * 0.06f, 0.8f * s, 5f * s, 0.6f, false);
            if (feel != null) { feel.Sparks(ground + Vector3.up * 0.3f, 40, false); feel.PlayBossStomp(); }
        }
        // It opens its arms as it straightens: the welcome, or the threat
        float spread = Mathf.Clamp01((t - RiseFrom - RiseFor * 0.7f) / 1.4f);
        if (boss.body.BossArmL != null) boss.body.BossArmL.localRotation = Quaternion.Slerp(boss.body.BossArmL.localRotation, Quaternion.Euler(-20f, 0f, 70f * spread), 1f - Mathf.Exp(-4f * dt));
        if (boss.body.BossArmR != null) boss.body.BossArmR.localRotation = Quaternion.Slerp(boss.body.BossArmR.localRotation, Quaternion.Euler(-20f, 0f, -70f * spread), 1f - Mathf.Exp(-4f * dt));

        // You: idle and watching, then the blade comes up into guard as the reverse lands
        if (you != null)
        {
            bool guard = t >= Shot2End - 0.4f;
            you.Animate(guard ? PlayerAvatar.Act.Guard : PlayerAvatar.Act.Idle, 1f, t, dt);
            you.SetGlow(guard ? Mathf.Lerp(0.35f, 1f, Mathf.Clamp01((t - Shot2End + 0.4f) / 1.2f)) : 0.35f);
            you.LookAt(boss.body.BossHead != null ? boss.body.BossHead.position : boss.body.Center);
        }

        Vector3 chest = boss.body.Center;
        Vector3 badge = boss.body.BossBadge != null ? boss.body.BossBadge.transform.position : chest;
        Vector3 face = boss.body.BossHead != null ? boss.body.BossHead.position : chest + Vector3.up * (0.6f * s);
        Vector3 pos, lookAt;
        float fov = fov0;
        if (t < Shot1End)
        {
            // Shot one: over your shoulder, low, pushing past you as the floor opens and it comes up beyond the desk
            float k = Mathf.SmoothStep(0f, 1f, t / Shot1End);
            Vector3 a = youFeet - toBoss * 2.4f + side * 0.9f + Vector3.up * 1.75f;
            Vector3 b = youFeet + toBoss * 2.2f + side * 1.3f + Vector3.up * 1.4f;
            pos = Vector3.Lerp(a, b, k);
            lookAt = Vector3.Lerp(ground + Vector3.up * 1.5f, chest, k);
            fov = Mathf.Lerp(62f, 58f, k);
        }
        else if (t < Shot2End)
        {
            // Shot two: from the floor at its feet, tilting up the body — the badge, the arms opening — to the face
            float k = Mathf.SmoothStep(0f, 1f, (t - Shot1End) / (Shot2End - Shot1End));
            pos = ground - toBoss * (1.5f * s) + Vector3.up * Mathf.Lerp(0.5f * s, 1.7f * s, k) + side * (0.4f * s * (1f - k));
            lookAt = Vector3.Lerp(badge, face, k);
            fov = Mathf.Lerp(52f, 46f, k);
        }
        else if (t < Shot3End)
        {
            // Shot three: the reverse — your face, close, from its side of you: the eyes narrow, the vector wakes, the blade comes up
            float k = Mathf.SmoothStep(0f, 1f, (t - Shot2End) / (Shot3End - Shot2End));
            Vector3 fp = you != null ? you.Face : youFeet + Vector3.up * 1.6f;
            pos = fp + toBoss * Mathf.Lerp(1.5f, 1.15f, k) - side * 0.55f + Vector3.up * Mathf.Lerp(0.05f, 0.12f, k);
            lookAt = fp;
            fov = Mathf.Lerp(40f, 34f, k);
        }
        else
        {
            // Shot four: back behind your own eyes as the beat drops
            float k = Mathf.SmoothStep(0f, 1f, (t - Shot3End) / (Length - Shot3End));
            if (you != null && k > 0.5f) you.Hide();
            Vector3 fp = you != null ? you.Face : youFeet + Vector3.up * 1.6f;
            Vector3 from = fp + toBoss * 1.15f - side * 0.55f + Vector3.up * 0.12f;
            Quaternion fromRot = Quaternion.LookRotation((fp - from).normalized, Vector3.up);
            cam.transform.SetPositionAndRotation(Vector3.Lerp(from, camPos0, k), Quaternion.Slerp(fromRot, camRot0, k));
            cam.fieldOfView = Mathf.Lerp(34f, fov0, k);
            return;
        }
        shake = Mathf.MoveTowards(shake, 0f, dt * 1.3f);
        float jolt = shake * shake;
        if (jolt > 0f) pos += new Vector3(Mathf.PerlinNoise(t * 31f, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t * 37f) - 0.5f, 0f) * (0.25f * jolt);
        cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation((lookAt - pos).normalized, Vector3.up));
        cam.fieldOfView = fov;
    }

    void End()
    {
        if (!Active) return;
        Active = false;
        if (cam != null)
        {
            cam.transform.SetPositionAndRotation(camPos0, camRot0);
            cam.fieldOfView = fov0;
        }
        if (you != null) { Destroy(you.gameObject); you = null; }
        if (boss != null && boss.body != null)
        {
            if (boss.body.BossArmL != null) boss.body.BossArmL.localRotation = Quaternion.identity;
            if (boss.body.BossArmR != null) boss.body.BossArmR.localRotation = Quaternion.identity;
        }
        if (look != null) look.enabled = true;
        if (motor != null) motor.enabled = true;
        if (grapple != null) grapple.enabled = true;
        if (boss != null) boss.Engage();
    }

    // ------------------------------------------------------------ the card

    // The one rot beat: as shot four cuts back to your own eyes, the card says what the house will say later (boss.IntroBeat)
    const float BeatAt = 8.5f, BeatLength = 0.2f;
    float subW;                                                              // the subtitle's width, measured once on the first draw

    static void Fill(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
    }

    static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);

    void OnGUI()
    {
        if (!Active || PauseMenu.IsPaused) return; // the pause veil covers the card
        MenuStyle.EnsureStyles();
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(MenuStyle.Title) { fontSize = 78, alignment = TextAnchor.LowerLeft };
            subStyle = new GUIStyle(MenuStyle.Brand) { fontSize = 18, alignment = TextAnchor.UpperLeft };
            hintStyle = new GUIStyle(MenuStyle.Sub) { alignment = TextAnchor.LowerRight };
            // Every colour below goes through GUI.color, so the faces themselves print white
            titleStyle.normal.textColor = subStyle.normal.textColor = hintStyle.normal.textColor = Color.white;
            subW = MenuStyle.Measure(subStyle, boss.CompanyLine).x;
        }
        UIScale.Begin();
        GUI.depth = -20;
        float w = UIScale.W, h = UIScale.H;

        // Letterbox, with a thin brass rule along each inner edge
        float bar = h * 0.13f;
        Fill(new Rect(0f, 0f, w, bar), Color.black);
        Fill(new Rect(0f, h - bar, w, bar), Color.black);
        Color hair = A(MenuStyle.Brass, 0.5f);
        Fill(new Rect(0f, bar - 1f, w, 1f), hair);
        Fill(new Rect(0f, h - bar, w, 1f), hair);
        GUI.color = Color.white;

        // The card: the name throws in from the left on the beat and settles a hair past its mark, the company under it
        float card = Mathf.Clamp01((t - 4.2f) / 0.35f);
        float fade = t > 9.2f ? Mathf.Clamp01((Length - t) / 0.8f) : 1f;
        if (card > 0f)
        {
            float ease = MenuStyle.Ease(card);
            float a = Mathf.Clamp01(ease) * fade;
            float x = w * 0.08f - (1f - ease) * 260f;
            float baseY = h - bar - 30f;
            bool beat = t >= BeatAt && t < BeatAt + BeatLength;
            int state = beat ? MenuStyle.RotState((int)((t - BeatAt) / 0.03f)) : -1;

            // The charcoal plate behind the name (fluted, in a stepped brass frame), then the gold slab on its ink
            // shadow (red / ink on the beat)
            MenuStyle.TitlePlate(new Rect(x - 20f, baseY - 104f, 700f, 110f), a);
            Fill(new Rect(x - 10f, baseY - 92f, 10f, 118f), A(MenuStyle.Ink, a));
            Fill(new Rect(x - 14f, baseY - 96f, 10f, 118f), A(state == 0 ? MenuStyle.Rot : state == 1 ? MenuStyle.Ink : MenuStyle.Gold, a));

            // The name, misregistered: the plates land wide and pull into register as the sheet settles; the beat kicks them
            float o = beat ? 8f : 1f + 9f * (1f - Mathf.Clamp01(ease));
            Rect name = new Rect(x + 6f, baseY - 100f, w * 0.75f, 92f);
            GUI.color = A(MenuStyle.Rot, 0.7f * a);
            UIScale.Label(new Rect(name.x - o, name.y, name.width, name.height), boss.BossName, titleStyle);
            GUI.color = A(MenuStyle.DeepTeal, 0.7f * a);
            UIScale.Label(new Rect(name.x + o, name.y + 1f, name.width, name.height), boss.BossName, titleStyle);
            GUI.color = A(MenuStyle.Paper, a);
            UIScale.Label(name, boss.BossName, titleStyle);
            MenuStyle.DoubleRule(x + 6f, baseY - 8f, w * 0.5f, A(MenuStyle.Gold, a));   // the double brass rule under the name

            // The programme's billing over the name: who presents
            GUI.color = A(MenuStyle.Brass, 0.9f * a);
            UIScale.Label(new Rect(x + 8f, baseY - 122f, 400f, 18f), "THE HOUSE PRESENTS", MenuStyle.Doc);
            MenuStyle.ChevronRule(new Rect(x + 8f + 150f, baseY - 117f, 60f, 6f), A(MenuStyle.Brass, 0.7f * a));
            // The company line with a chevron rule after it; on the beat it reads what it should not
            GUI.color = A(beat ? MenuStyle.Rot : MenuStyle.Gold, a);
            UIScale.Label(new Rect(x + 8f, baseY - 2f, w * 0.75f, 26f), beat ? boss.IntroBeat : boss.CompanyLine, subStyle);
            MenuStyle.ChevronRule(new Rect(x + 8f + subW + 12f, baseY + 5f, 90f, 8f), A(MenuStyle.Gold, 0.8f * a));
            GUI.color = Color.white;
        }
        if (t > 1f)
        {
            GUI.color = A(MenuStyle.Sick, fade);
            UIScale.Label(new Rect(w - 340f, h - bar - 34f, 300f, 24f), "any key · skip", hintStyle);
            GUI.color = Color.white;
        }
        UIScale.End();
    }
}
