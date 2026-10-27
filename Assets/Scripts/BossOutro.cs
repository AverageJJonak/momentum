using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The Concierge's exit. His health hits zero and he does NOT drop: the game
/// takes the camera and nine seconds play out, skippable after the first
/// second and a half —
///   THE BLOW    (0.0–1.3)  time crawls; a low close-up as he jolts, the smile
///                          goes flat, the eyes flicker, the headset sparks.
///   THE BOW     (1.3–4.3)  the camera circles him at chest height while he
///                          folds into one last deep bow; the badge and the
///                          cuffs shed, the coat swings open, and the throat
///                          core behind the tie charges — brighter, faster —
///                          as the eyes come back red and the smile widens.
///   THE BURST   (4.3)      a white-out, two rings across the floor, and the
///                          body comes apart into the air; hard cut to a wide
///                          low shot over the debris with a handheld shake,
///                          the music cut dead under it.
///   THE STAMP   (4.9–9.4)  CHECKED OUT slams down over the wide shot — you in
///                          it, over the debris, the blade coming down — with
///                          the fight's numbers; the bars lift, the way out
///                          opens, and the house has the last word.
/// The body's controller hands over on death (BossConcierge.OnDied) and takes
/// the two calls back: Fall() at the burst, Dismissed() at the end.
/// </summary>
public class BossOutro : MonoBehaviour
{
    public static bool Active { get; private set; }

    public Boss boss;

    const float Blow = 1.3f, Burst = 4.3f, Card = 4.9f, Length = 9.4f;
    const float SkipAfter = 1.5f;

    float t;
    bool fallen;
    PlayerLook look;
    PlayerMotor motor;
    GrappleAbility grapple;
    Camera cam;
    Vector3 camPos0;
    Quaternion camRot0;
    float fov0 = 95f;
    float shake;
    Vector3 ground, toPlayer, side;
    PlayerAvatar you;                    // you, in the last frame: standing over what is left of it
    Vector3 youFeet;
    float torsoPitch0;
    Vector3 smileRest = Vector3.one, throatRest = Vector3.one;
    Color throatColor = Color.white;
    MaterialPropertyBlock throatBlock;
    float nextSpark, nextTick, nextEyeFlip;
    bool eyesRed;
    int shed;
    string stats = "", stamp = "", lastWord = "", nameLine = "", lastLine = "";      // the boss's own words, read once at Begin: nothing is built per frame
    GUIStyle stampStyle, nameStyle, lineStyle, hintStyle, wordStyle, docStyle;

    void OnDisable() { if (Active) End(); }

    /// Called by the boss the frame his health runs out
    public void Begin()
    {
        if (Active || boss == null || boss.body == null) return;
        Active = true;
        fallen = false;
        t = 0f;
        shed = 0;
        var d = CombatDirector.Instance;
        motor = d != null ? d.motor : FindFirstObjectByType<PlayerMotor>();
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

        var body = boss.body;
        ground = new Vector3(boss.arenaCenter.x, boss.floorY, boss.arenaCenter.z);
        toPlayer = (camPos0 - body.Center); toPlayer.y = 0f;
        toPlayer = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : -body.transform.forward;
        side = Vector3.Cross(Vector3.up, toPlayer);
        torsoPitch0 = body.BossTorso != null ? body.BossTorso.localRotation.eulerAngles.x : 0f;
        if (torsoPitch0 > 180f) torsoPitch0 -= 360f;
        if (body.BossSmile != null) smileRest = body.BossSmile.localScale;
        if (body.BossThroat != null)
        {
            throatRest = body.BossThroat.transform.localScale;
            var m = body.BossThroat.sharedMaterial;
            throatColor = m != null && m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : new Color(0.4f, 1.6f, 1.8f);
            throatBlock = new MaterialPropertyBlock();
        }
        body.SetCoatSkin(null);
        nextSpark = 0.1f;
        nextTick = Blow;
        nextEyeFlip = 0f;

        float fight = Mathf.Max(0f, Time.time - boss.EngagedAt);
        stats = "FIGHT " + LevelTimer.Format(fight) + "   ·   HITS LANDED " + boss.HitsTaken + "   ·   COUNTERS " + boss.Counters;
        stamp = boss.ExitStamp;
        lastWord = boss.LastWord;
        nameLine = boss.BossName + "   ·   " + boss.ExitLine;
        lastLine = boss.LastLine;
        Time.timeScale = 0.12f;
    }

    void Update()
    {
        if (!Active || PauseMenu.IsPaused) return;
        float dt = Time.unscaledDeltaTime;
        t += dt;

        Time.timeScale = TimeScaleAt(t);
        Choreograph(dt);
        Shoot(dt);

        if (t > SkipAfter)
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool skip = (kb != null && kb.anyKey.wasPressedThisFrame) || (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (skip) { End(); return; }
        }
        if (Active && t >= Length) End();
    }

    /// Time crawls through the blow, creeps back through the bow, hangs at the burst, and is whole again for the stamp
    static float TimeScaleAt(float t)
    {
        if (t < 0.3f) return 0.05f; // the freeze on the blow
        if (t < Blow) return 0.12f;
        if (t < Burst) return Mathf.Lerp(0.12f, 0.45f, Mathf.SmoothStep(0f, 1f, (t - Blow) / (Burst - Blow)));
        if (t < Burst + 1.2f) return 0.3f;
        return Mathf.Lerp(0.3f, 1f, Mathf.Clamp01((t - Burst - 1.2f) / 1.6f));
    }

    // ------------------------------------------------------------ the body

    void Choreograph(float dt)
    {
        var body = boss.body;
        if (body == null) return;
        var feel = GameFeel.Instance;
        float s = body.Scale;

        if (!fallen && t >= Burst) { DoBurst(); return; }
        if (fallen) return;

        Vector3 head = body.BossHead != null ? body.BossHead.position : body.Center + Vector3.up * (0.6f * s);
        Vector3 throat = body.BossThroat != null ? body.BossThroat.transform.position : body.Center + Vector3.up * (0.35f * s);

        if (t < Blow)
        {
            // The jolt: eyes strobe, the smile drops to a line, the headset throws sparks
            if (t >= nextEyeFlip)
            {
                nextEyeFlip = t + 0.08f;
                eyesRed = !eyesRed;
                body.SetEyes(eyesRed ? Enemy.EyeRed() : null);
            }
            if (body.BossSmile != null) body.BossSmile.localScale = new Vector3(smileRest.x, smileRest.y * 0.22f, smileRest.z);
            if (t >= nextSpark && feel != null)
            {
                nextSpark = t + 0.2f;
                feel.Sparks(head + Vector3.up * (0.05f * s), 12, false);
            }
            if (body.BossTorso != null)
                body.BossTorso.localRotation = Quaternion.Euler(torsoPitch0 + Mathf.Sin(t * 41f) * 2.5f, 0f, Mathf.Sin(t * 29f) * 1.5f);
            return;
        }

        // The bow: fold forward, coat open, everything trembling; the core charges
        float k = Mathf.SmoothStep(0f, 1f, (t - Blow) / (Burst - Blow));
        float charge = Mathf.Clamp01((t - Blow - 0.6f) / (Burst - Blow - 0.6f));
        float tremor = Mathf.Sin(t * 37f) * 1.5f * k;
        if (body.BossTorso != null)
            body.BossTorso.localRotation = Quaternion.Euler(Mathf.Lerp(torsoPitch0, 58f, k) + tremor, 0f, Mathf.Sin(t * 23f) * 1.2f * k);
        if (body.BossHead != null)
            body.BossHead.localRotation = Quaternion.Euler(32f * k + tremor, Mathf.Sin(t * 11f) * 6f * k, 0f);
        float coat = Mathf.Clamp01(k * 1.6f);
        if (body.BossCoatL != null) body.BossCoatL.localRotation = Quaternion.Euler(0f, 105f * coat, 0f);
        if (body.BossCoatR != null) body.BossCoatR.localRotation = Quaternion.Euler(0f, -105f * coat, 0f);

        // The badge goes first, then the cuffs — pieces of the uniform coming off him
        if (shed == 0 && t > Blow + 0.6f) { Shed(body.BossBadge, body.transform.forward * 3f + Vector3.up * 4f); shed++; }
        if (shed == 1 && t > Blow + 1.3f) { Shed(body.BossCuffL, -body.transform.right * 3f + Vector3.up * 3f); shed++; }
        if (shed == 2 && t > Blow + 1.7f) { Shed(body.BossCuffR, body.transform.right * 3f + Vector3.up * 3f); shed++; }

        // Eyes dark through the bow, then back red as the core charges; the smile comes back wider than ever
        if (charge <= 0f) { if (eyesRed) { eyesRed = false; body.SetEyes(null); } }
        else if (charge > 0.55f && !eyesRed) { eyesRed = true; body.SetEyes(Enemy.EyeRed()); }
        if (body.BossSmile != null)
        {
            float grin = Mathf.Clamp01((charge - 0.55f) / 0.3f);
            body.BossSmile.localScale = new Vector3(Mathf.Lerp(smileRest.x, smileRest.x * 1.45f, grin), Mathf.Lerp(smileRest.y * 0.22f, smileRest.y * 1.25f, grin), smileRest.z);
        }

        // The throat core: pulsing faster and brighter until it cannot hold
        if (body.BossThroat != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * (5f + 26f * charge));
            body.BossThroat.transform.localScale = throatRest * (1f + charge * (0.5f + 0.7f * pulse));
            Color hot = Color.Lerp(throatColor, new Color(3f, 3f, 3f), charge * (0.55f + 0.45f * pulse));
            throatBlock.SetColor("_BaseColor", hot);
            body.BossThroat.SetPropertyBlock(throatBlock);
        }
        if (t >= nextSpark && feel != null && charge > 0f)
        {
            nextSpark = t + Mathf.Lerp(0.36f, 0.09f, charge);
            feel.Sparks(throat, 6 + Mathf.RoundToInt(10f * charge), true);
        }
        if (t >= nextTick && feel != null)
        {
            nextTick = t + Mathf.Lerp(0.5f, 0.11f, charge);
            feel.PlayBossOverload(charge);
        }
    }

    /// A piece of the uniform comes off: the renderer goes dark and a copy drops away with a spin
    void Shed(Renderer r, Vector3 velocity)
    {
        if (r == null || !r.enabled) return;
        r.enabled = false;
        var mf = r.GetComponent<MeshFilter>();
        string mesh = mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "Cube";
        PrimitiveType type = mesh == "Sphere" ? PrimitiveType.Sphere : mesh == "Cylinder" ? PrimitiveType.Cylinder : PrimitiveType.Cube;
        var piece = GameObject.CreatePrimitive(type);
        piece.name = "Shed " + r.name;
        piece.layer = 2;
        Destroy(piece.GetComponent<Collider>());
        piece.transform.SetPositionAndRotation(r.transform.position, r.transform.rotation);
        piece.transform.localScale = r.transform.lossyScale;
        piece.GetComponent<MeshRenderer>().sharedMaterial = r.sharedMaterial;
        var rb = piece.AddComponent<Rigidbody>();
        rb.mass = 1.5f;
        rb.linearVelocity = velocity + Random.insideUnitSphere * 1.5f;
        rb.angularVelocity = Random.insideUnitSphere * 10f;
        piece.AddComponent<ShardFade>();
        if (GameFeel.Instance != null) GameFeel.Instance.PlayBossButton();
    }

    /// The burst: rings across the floor, a white-out, and the body into the air
    void DoBurst()
    {
        fallen = true;
        var body = boss.body;
        float s = body != null ? body.Scale : 1f;
        Vector3 chest = body != null ? body.Center : ground + Vector3.up * 2f;
        Vector3 floor = new Vector3(chest.x, boss.floorY + 0.05f, chest.z);
        var feel = GameFeel.Instance;

        var root = boss.Fall(); // the vines go, the music cuts, the ragdoll is born from the bowed pose
        if (root != null)
        {
            foreach (var rb in root.GetComponentsInChildren<Rigidbody>())
            {
                Vector3 away = rb.worldCenterOfMass - chest;
                Vector3 dir = (away.normalized * 0.7f + Vector3.up * 0.55f + Random.insideUnitSphere * 0.3f).normalized;
                rb.linearVelocity = dir * Random.Range(9f, 17f);
                rb.angularVelocity = Random.insideUnitSphere * 12f;
            }
        }
        ShockRing.Spawn(floor, 0.6f * s, 18f, 0.6f, false);
        ShockRing.Spawn(floor, 0.3f * s, 10f, 0.4f, true);
        MuzzleFlash.Spawn(chest, 7f);
        if (feel != null)
        {
            feel.Sparks(chest, 90, false);
            feel.Sparks(chest, 60, true);
            feel.PlayBossCollapse();
        }
        shake = 1f;

        // And you, where you stood: the blade still up, then lowering, over what is left of it
        youFeet = motor != null ? motor.CurrPosition : camPos0 - Vector3.up * 1.6f;
        Vector3 face = ground - youFeet; face.y = 0f;
        you = PlayerAvatar.Build(transform, youFeet, face.sqrMagnitude > 0.01f ? Quaternion.LookRotation(face.normalized, Vector3.up).eulerAngles.y : 0f);
        you.Blade(false); // the hands are the kit: no blade
        you.SetGlow(1f);
        you.Animate(PlayerAvatar.Act.Guard, 1f, 0f, 10f);
        you.LookAt(ground + Vector3.up * (0.6f * s));
    }

    // ------------------------------------------------------------ the camera

    void Shoot(float dt)
    {
        if (cam == null || boss == null || boss.body == null) return;
        var body = boss.body;
        float s = body.Scale;
        Vector3 chest = fallen ? ground + Vector3.up * (0.55f * s) : body.Center;
        Vector3 face = body.BossHead != null && !fallen ? body.BossHead.position : chest + Vector3.up * (0.6f * s);
        Vector3 pos, lookAt;
        float fov = fov0;

        if (t < Blow)
        {
            // Low and close from his front-right, tilted up at the face, creeping in
            float k = t / Blow;
            pos = chest + toPlayer * (1.7f * s - 0.15f * s * k) + side * (0.95f * s) + Vector3.up * (0.05f * s);
            lookAt = Vector3.Lerp(chest, face, 0.7f);
        }
        else if (t < Burst)
        {
            // The circle: round him at chest height as he bows, closing in and rising to the face
            float k = (t - Blow) / (Burst - Blow);
            float a = Mathf.Lerp(-15f, 120f, Mathf.SmoothStep(0f, 1f, k)) * Mathf.Deg2Rad;
            Vector3 dir = toPlayer * Mathf.Cos(a) + side * Mathf.Sin(a);
            pos = chest + dir * Mathf.Lerp(2.5f * s, 1.9f * s, k) + Vector3.up * Mathf.Lerp(0.1f * s, 0.5f * s, k);
            lookAt = Vector3.Lerp(chest, face, k);
            fov = Mathf.Lerp(fov0, fov0 - 10f, k);
        }
        else
        {
            // Wide and low from behind your shoulder over the debris, drifting; the burst kicks it. You in the frame,
            // the blade coming down as the stamp lands
            float since = t - Burst;
            float drift = Mathf.Clamp01(since / (Length - Burst));
            if (you != null)
            {
                you.Animate(since < 1.6f ? PlayerAvatar.Act.Guard : PlayerAvatar.Act.Idle, 1f, t, dt);
                you.SetGlow(Mathf.Lerp(1f, 0.5f, Mathf.Clamp01((since - 1.6f) / 2f)));
            }
            Vector3 feet = you != null ? you.Feet : ground + toPlayer * (4f * s);
            pos = feet + toPlayer * 2.6f + side * Mathf.Lerp(2.1f, 1.5f, drift) + Vector3.up * 1.7f;
            lookAt = Vector3.Lerp(ground + Vector3.up * (0.7f * s), feet + Vector3.up * 1.5f, 0.3f);
            fov = fov0 - 22f + 6f * drift - 16f * Mathf.Exp(-since * 2.2f);
        }

        shake = Mathf.MoveTowards(shake, 0f, dt * 1.1f);
        Vector3 jolt = shake > 0f ? new Vector3(Mathf.PerlinNoise(t * 23f, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t * 27f) - 0.5f, 0f) * (0.5f * s * shake * shake) : Vector3.zero;
        cam.transform.SetPositionAndRotation(pos + jolt, Quaternion.LookRotation((lookAt - pos).normalized, Vector3.up));
        cam.fieldOfView = fov;
    }

    void End()
    {
        if (!Active) return;
        if (!fallen) DoBurst(); // skipped early: he still comes apart
        Active = false;
        if (you != null) { Destroy(you.gameObject); you = null; }
        Time.timeScale = PauseMenu.IsPaused ? 0f : 1f;
        if (cam != null)
        {
            cam.transform.SetPositionAndRotation(camPos0, camRot0);
            cam.fieldOfView = fov0;
        }
        if (look != null) look.enabled = true;
        if (motor != null) motor.enabled = true;
        if (grapple != null) grapple.enabled = true;
        if (boss != null) boss.Dismissed();
    }

    // ------------------------------------------------------------ the frame

    // The stamp, the last word and the name line come from the boss (Begin reads them once)
    const float TickLength = 0.06f;

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
        if (stampStyle == null)
        {
            stampStyle = new GUIStyle(MenuStyle.Title) { fontSize = 108, alignment = TextAnchor.MiddleCenter };
            nameStyle = new GUIStyle(MenuStyle.Title) { fontSize = 40, alignment = TextAnchor.UpperCenter };
            lineStyle = new GUIStyle(MenuStyle.Brand) { fontSize = 18, alignment = TextAnchor.UpperCenter };
            wordStyle = new GUIStyle(MenuStyle.Brand) { fontSize = 22, alignment = TextAnchor.LowerCenter };
            hintStyle = new GUIStyle(MenuStyle.Sub) { alignment = TextAnchor.LowerRight };
            docStyle = new GUIStyle(MenuStyle.Doc) { alignment = TextAnchor.UpperCenter };
            // Every colour below goes through GUI.color, so the faces themselves print white
            stampStyle.normal.textColor = nameStyle.normal.textColor = lineStyle.normal.textColor = wordStyle.normal.textColor = hintStyle.normal.textColor = docStyle.normal.textColor = Color.white;
        }
        UIScale.Begin();
        GUI.depth = -20;
        float w = UIScale.W, h = UIScale.H;

        // Letterbox: in over the first beat, out over the last
        float barIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.35f));
        float barOut = t > Length - 0.6f ? Mathf.Clamp01((Length - t) / 0.6f) : 1f;
        float bar = h * 0.13f * barIn * barOut;
        Fill(new Rect(0f, 0f, w, bar), Color.black);
        Fill(new Rect(0f, h - bar, w, bar), Color.black);
        GUI.color = Color.white;

        // His last line, under the bars, while he bows
        if (t > Blow + 0.3f && t < Burst)
        {
            float a = Mathf.Min(Mathf.Clamp01((t - Blow - 0.3f) / 0.3f), Mathf.Clamp01((Burst - t) / 0.25f));
            GUI.color = A(MenuStyle.Paper, 0.9f * a);
            UIScale.Label(new Rect(0f, h - bar - 44f, w, 30f), lastLine, wordStyle);
            GUI.color = Color.white;
        }

        // The stamp: slams down, settles, and fades with the bars
        float card = Mathf.Clamp01((t - Card) / 0.32f);
        float fade = Mathf.Min(barOut, t > Length - 1f ? Mathf.Clamp01((Length - t) / 1f) : 1f);
        if (card > 0f)
        {
            float ease = 1f - Mathf.Pow(1f - card, 3f);
            float scale = Mathf.Lerp(2.6f, 1f, ease) + 0.06f * Mathf.Sin(ease * Mathf.PI);
            float alpha = ease * fade;
            float k = (t - Card - 0.32f) / 0.3f;                          // the impact: negative on the way down, 0 the frame it lands, 1 once settled
            // As the bars lift the house has the last word: the slab ticks red twice and reads HOLLOW
            bool tick = (t >= Length - 1f && t < Length - 1f + TickLength) || (t >= Length - 0.75f && t < Length - 0.75f + TickLength);
            Vector2 pivot = new Vector2(w * 0.5f, h * 0.42f);
            Matrix4x4 m = GUI.matrix;
            // Canvas-space pivots: GUIUtility's take window pixels and the slam misses its mark at any Factor but 1
            UIScale.RotateAround(-7f, pivot);
            UIScale.ScaleAround(new Vector2(scale, scale), pivot);
            float sw = Mathf.Min(760f, w * 0.7f), sh = 150f;
            Rect slab = new Rect(pivot.x - sw * 0.5f, pivot.y - sh * 0.5f, sw, sh);
            Color body = A(MenuStyle.Gold, alpha);
            Color ink = A(MenuStyle.Ink, alpha);
            Color foil = A(MenuStyle.Foil, alpha);

            MenuStyle.StampBurst(pivot, sh * 2.4f, k, A(MenuStyle.Foil, 0.6f));            // the company's sun, on impact, behind everything
            MenuStyle.StepFrame(new Rect(slab.x - 7f, slab.y - 7f, sw + 14f, sh + 14f), body, 2f, 8f, 2);   // the ziggurat edge standing proud of the slab
            Fill(slab, body);
            MenuStyle.Flute(slab, 0.07f * alpha);                                          // the sheet's ribs, faint
            // The double frame: the two ink rules, each with a foil hairline just inside it
            Fill(new Rect(slab.x + 10f, slab.y + 10f, sw - 20f, 5f), ink);
            Fill(new Rect(slab.x + 10f, slab.yMax - 15f, sw - 20f, 5f), ink);
            Fill(new Rect(slab.x + 10f, slab.y + 16f, sw - 20f, 1f), foil);
            Fill(new Rect(slab.x + 10f, slab.yMax - 17f, sw - 20f, 1f), foil);
            if (tick)
            {
                Fill(new Rect(slab.x, pivot.y - 30f, sw, 60f), A(MenuStyle.Rot, alpha)); // a band torn across the middle
                GUI.color = ink;
                UIScale.Label(slab, lastWord, stampStyle);
            }
            else
            {
                // The letters land misregistered and pull into register as the slab settles
                float o = 1f + 9f * (1f - Mathf.Clamp01(k));
                GUI.color = A(MenuStyle.Rot, 0.7f * alpha);
                UIScale.Label(new Rect(slab.x - o, slab.y, sw, sh), stamp, stampStyle);
                GUI.color = A(MenuStyle.DeepTeal, 0.7f * alpha);
                UIScale.Label(new Rect(slab.x + o, slab.y + 1f, sw, sh), stamp, stampStyle);
                GUI.color = ink;
                UIScale.Label(slab, stamp, stampStyle);
            }
            GUI.matrix = m;

            // Under it, straight: who, and the numbers
            float settle = Mathf.Clamp01((t - Card - 0.35f) / 0.3f) * fade;
            GUI.color = A(MenuStyle.Paper, settle);
            UIScale.Label(new Rect(0f, pivot.y + 95f, w, 50f), nameLine, nameStyle);
            GUI.color = A(MenuStyle.Gold, settle);
            UIScale.Label(new Rect(0f, pivot.y + 146f, w, 26f), stats, lineStyle);
            // The programme's last line, in the House's voice
            GUI.color = A(MenuStyle.Sick, 0.9f * settle);
            UIScale.Label(new Rect(0f, pivot.y + 176f, w, 20f), "WITH THE COMPLIMENTS OF THE HOUSE", docStyle);
            GUI.color = Color.white;
        }

        if (t > SkipAfter)
        {
            GUI.color = A(MenuStyle.Sick, fade);
            UIScale.Label(new Rect(w - 340f, h - bar - 34f, 300f, 24f), "any key · skip", hintStyle);
            GUI.color = Color.white;
        }
        UIScale.End();
    }
}
