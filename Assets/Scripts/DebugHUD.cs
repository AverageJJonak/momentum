using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// THE INSTRUMENT PANEL (spec §6, dressed 2026-10-07 for THE HOUSE; stripped to its essentials with the one kit,
/// 2026-10-16): a brass-bezelled SPEED DIAL with an oxblood needle and the perfect-hop band marked in brass, STAMINA
/// as brass lift lamps, HEALTH as a mercury column in a brass tube, RANK as a marquee letter card, the TIMER as a
/// hotel clock with the course segment under it, the event feed on ticker tape, and under the crosshair THREE LAMPS
/// — KICK, PARRY, WHIP — that read each verb's readiness (gold while the parry window is open; red while a whiff's
/// cooldown drains; gold for a beat after a perfect springboard). The crosshair's brackets close on what the next
/// press would meet: cyan on a weakspot (EXECUTE) or a cyan target (BREAK), white on a body (the combo's next step).
/// Every semantic colour is untouched (cyan = the thing you break, gold = perfect / flow, red = danger). The panel
/// shows state, never instructions. Every figure is a cached string rebuilt in Update when the value it shows
/// changes; nothing is allocated in OnGUI; every face is a texture made once by MenuArt and swung with UIScale's
/// canvas-space pivots.
/// </summary>
public class DebugHUD : MonoBehaviour
{
    public PlayerMotor motor;
    public StaminaSystem stamina;
    public StyleSystem style;
    public LevelTimer timer;
    public PlayerHealth health;
    public Fists fists;
    public GrappleAbility grapple;

    const int FeedMax = 6;
    const float FeedLifetime = 2.5f;
    const float DialMax = 30f;            // the speed dial's full sweep, m/s (the perfect-hop band fades 18–30)

    struct FeedEntry
    {
        public string text;
        public float time;
        public bool highlight;
        public bool glitch;               // hidden tech: the one glitch that is yours
        public float width;               // measured once, at add time (or at first draw if the style was not built yet)
    }

    readonly List<FeedEntry> feed = new List<FeedEntry>();
    GUIStyle bigStyle, labelStyle, feedStyle, timerStyle, timerSubStyle, rankStyle, dialStyle, lampLabelStyle, aimLabelStyle, executeStyle;
    static readonly GUIContent scratch = new GUIContent();
    float fps;
    static bool readoutForced;   // F3 toggles the speed/fps readout without touching the config

    // Animation state, advanced in Update and only read in OnGUI (Layout and Repaint must agree)
    float ringAngle;                  // the crosshair ring: spun by your own speed
    int lastPips = -1;
    float pipPopAt = -10f;            // the last lamp to come on pops
    string lastRank;
    float rankChangedAt = -10f;       // the marquee card punches on a change
    string lastPhase;
    float phaseChangedAt = -10f;      // the boss bar's scripted beat on Phase-2 entry
    float lastStampUntil = -1f, stampAt = -10f; // the checkpoint stamp's burst over its first 0.3 s
    string lastSummary;               // the practice line, and its chip's width measured on change
    float summaryWidth;
    float hitAt = -10f;               // unscaled time of the last connect (the hit marker, the EXECUTE stamp)
    bool hitExecute;
    float needleSpeed;                // the dial's needle, eased toward the true speed in Update (a real needle has mass)
    // Every figure the HUD prints that is not a literal: built in Update (RefreshText) when the value it shows
    // changes, or with the event that sets it — never in OnGUI
    string bestText = "";            float bestShown = float.NaN;
    string roomText = "";            int roomShownIndex = -1, roomShownAlive = -1, roomShownTenths = -1;   // the course segment's line
    string speedText = "0.0";        int speedShownTenths = -1;
    string styleLine1 = "", styleLine2 = "";
    int styleShownPts = -1, styleShownMult = -1, styleShownGain = -1, styleShownMoves = -1;
    string hpText = "";              int hpShown = -1, hpMaxShown = -1;
    string chainText = "";           int chainShown = -1, chainShownNext = -1;
    string healText = "";            float healShownAt = float.NaN;
    string bossHitText = "";         float bossHitShownAt = float.NaN;
    string executeText = "";         string executeShownSpot;

    void OnEnable()
    {
        MovementEvents.MoveExecuted += OnMove;
        ResetDetector.ResetFired += OnReset;
    }

    void OnDisable()
    {
        MovementEvents.MoveExecuted -= OnMove;
        ResetDetector.ResetFired -= OnReset;
    }

    float chainFiredAt = float.NegativeInfinity;
    float graceSeenAt = float.NegativeInfinity;
    void OnReset(string label, bool scores)
    {
        bool pip = stamina != null && !stamina.InfinitePips;   // in flow the lamps are pinned lit: nothing was refunded, so nothing is claimed
        AddFeed(pip ? (scores ? $"RESET · {label} · +LAMP" : $"RESET · {label} · REPEAT · +LAMP") : $"RESET · {label}", scores, false);
        if (label.StartsWith("CHAIN")) chainFiredAt = Time.time;
    }

    /// One measurement per event; width 0 means the style was not built yet and the first draw measures it
    void AddFeed(string text, bool highlight, bool glitch)
    {
        float width = feedStyle != null ? MenuStyle.Measure(feedStyle, text).x : 0f;
        feed.Add(new FeedEntry { text = text, time = Time.time, highlight = highlight, glitch = glitch, width = width });
        if (feed.Count > FeedMax) feed.RemoveAt(0);
    }

    void OnMove(MoveType type, string detail)
    {
        if (type == MoveType.SlamLand || type == MoveType.Jump || type == MoveType.Land) return; // plumbing, not tricks
        if ((type == MoveType.Fist || type == MoveType.Kick) && detail != null && detail != "MISS")
        {
            // A connect: the hit marker; an execute its diamond and stamp
            hitAt = Time.unscaledTime;
            hitExecute = detail == "EXECUTE";
        }
        string label = type switch
        {
            MoveType.Bhop => "BHOP",
            MoveType.PerfectBhop => "PERFECT BHOP",
            MoveType.Slide => "SLIDE",
            MoveType.SlideJump => "SLIDE-JUMP",
            MoveType.PerfectSlideJump => "PERFECT SLIDE-JUMP",
            MoveType.EdgeBoost => "EDGE BOOST",
            MoveType.WallKick => "WALL KICK",
            MoveType.WallRun => "WALL RUN",
            MoveType.WallJump => "WALL JUMP",
            MoveType.CornerBoost => "CORNER BOOST",
            MoveType.Dash => "DASH",
            MoveType.DoubleJump => "DOUBLE JUMP",
            MoveType.Slam => "SLAM",
            MoveType.SlamStorageJump => "SLAM STORAGE",
            MoveType.GrappleYank => "WHIP YANK",
            MoveType.Kick => detail == null || detail == "MISS" ? "KICK (whiff)" : detail.StartsWith("SPRINGBOARD") ? detail : $"KICK · {detail}",
            MoveType.Trick => detail ?? "TRICK",   // FRONT FLIP / PERFECT BACKFLIP / LANDED  +x m/s / STUMBLE: the detail is the line
            MoveType.GlitchTech => "??? HIDDEN TECH",
            MoveType.Parry => "PARRY",
            MoveType.ParryWhiff => $"PARRY WHIFF · {detail}",
            MoveType.WallHit => $"WALL · {detail} m/s",
            MoveType.GlassBreak => "GLASS BREAK",
            MoveType.GlassBump => "GLASS · too slow",
            MoveType.Fist => detail == "EXECUTE" ? (Fists.LastStep == "HOOK" && Fists.LastHookHits > 1 ? $"HOOK · EXECUTE ×{Fists.LastHookHits}" : "EXECUTE")
                : detail == "PERFECT TACKLE" ? "PERFECT TACKLE"
                : detail == "TACKLE" ? "TACKLE"
                : detail == "HIT" ? (Fists.LastStep == "HOOK" && Fists.LastHookHits > 1 ? $"HOOK ×{Fists.LastHookHits}" : Fists.LastStep) : $"{Fists.LastStep} (whiff)",
            _ => type.ToString().ToUpperInvariant(),
        };
        bool highlight = (type == MoveType.SlamStorageJump && detail != null && detail.EndsWith("PERFECT")) || type == MoveType.PerfectBhop || type == MoveType.PerfectSlideJump
                         || type == MoveType.EdgeBoost || type == MoveType.CornerBoost
                         || type == MoveType.GlitchTech || type == MoveType.Parry
                         || type == MoveType.GlassBreak || type == MoveType.WallJump
                         || (type == MoveType.Fist && (detail == "EXECUTE" || detail == "PERFECT TACKLE"))
                         || (type == MoveType.Kick && detail != null && (detail == "EXECUTE" || detail.StartsWith("SPRINGBOARD")))
                         || (type == MoveType.Trick && detail != null && (detail.StartsWith("PERFECT") || detail.StartsWith("LANDED")))
                         || (detail != null && detail.EndsWith("PERFECT"));
        if (!string.IsNullOrEmpty(detail) && type != MoveType.ParryWhiff && type != MoveType.Fist && type != MoveType.WallHit && type != MoveType.Kick && type != MoveType.Trick)
            label += $"  {detail}";   // a perfect carries its figure
        AddFeed(label, highlight, type == MoveType.GlitchTech);
    }

    void Update()
    {
        fps = Mathf.Lerp(fps, 1f / Mathf.Max(Time.unscaledDeltaTime, 1e-4f), 0.05f);
        if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame) readoutForced = !readoutForced;   // F3: the readout, whatever the config says
        float now = Time.unscaledTime;

        // The ring: your own speed is the spin. The dial's needle follows the true speed with a little mass, and its
        // figure is one string per tenth.
        if (motor != null)
        {
            Vector3 v = motor.Velocity;
            float hSpeed = new Vector2(v.x, v.z).magnitude;
            ringAngle += hSpeed * 12f * Time.unscaledDeltaTime;
            needleSpeed = Mathf.Lerp(needleSpeed, hSpeed, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            int tenths = Mathf.RoundToInt(hSpeed * 10f);
            if (tenths != speedShownTenths) { speedShownTenths = tenths; speedText = (tenths / 10f).ToString("0.0"); }
        }

        if (stamina != null)
        {
            int pips = stamina.Pips;
            if (lastPips >= 0 && pips > lastPips) pipPopAt = now;
            lastPips = pips;
        }
        if (style != null)
        {
            string rank = style.Rank;
            if (!ReferenceEquals(rank, lastRank))
            {
                if (lastRank != null) rankChangedAt = now;
                lastRank = rank;
            }
        }
        var boss = Boss.Current;
        string phase = boss != null ? boss.PhaseName : null; // interned literals: a reference compare, no allocation
        if (!ReferenceEquals(phase, lastPhase))
        {
            phaseChangedAt = now;
            lastPhase = phase;
        }
        if (boss != null)
        {
            // The bite's line and the EXECUTE line: one string per bite, one per open spot
            if (boss.LastHitTime != bossHitShownAt)
            {
                bossHitShownAt = boss.LastHitTime;
                bossHitText = boss.LastHitFraction > 0f ? boss.LastHitLabel + "  −" + Mathf.RoundToInt(boss.LastHitFraction * 100f) + "%" : boss.LastHitLabel;
            }
            if (boss.OpenSpot != executeShownSpot) { executeShownSpot = boss.OpenSpot; executeText = "EXECUTE · " + boss.OpenSpot; }
        }
        RefreshText();
        var flow = CourseFlow.Instance;
        if (flow != null && flow.StampUntil != lastStampUntil)
        {
            lastStampUntil = flow.StampUntil;
            stampAt = now;
        }
        if (Practice.Any)
        {
            string summary = Practice.Summary;        // the getter's StringBuilder is existing cost; the compare is not
            if (summary != lastSummary)
            {
                lastSummary = summary;
                summaryWidth = feedStyle != null ? MenuStyle.Measure(feedStyle, summary).x : 0f;
            }
        }
        else lastSummary = null;
    }

    /// The printed figures, rebuilt only when the number they show changes (each keyed on its rounded value)
    void RefreshText()
    {
        if (timer != null && timer.Best != bestShown) { bestShown = timer.Best; bestText = "best " + LevelTimer.Format(timer.Best); }
        // The course segment: its name, the fuel still standing in it, how long you have been in it (never a count-down: nothing gates)
        var flow = CourseFlow.Instance;
        var seg = flow != null ? flow.Current : null;
        if (seg != null)
        {
            int alive = seg.Alive, tenths = Mathf.RoundToInt(flow.SegmentTime * 10f);
            if (seg.index != roomShownIndex || alive != roomShownAlive || tenths != roomShownTenths)
            {
                roomShownIndex = seg.index; roomShownAlive = alive; roomShownTenths = tenths;
                roomText = seg.name + (alive > 0 ? "  ·  " + alive + " FUEL" : "") + "  ·  " + flow.SegmentTime.ToString("0.0");
            }
        }
        if (style != null)
        {
            int pts = Mathf.RoundToInt(style.Points), mult = Mathf.RoundToInt(style.Multiplier * 100f), gain = Mathf.RoundToInt(style.RankGainScale * 10f);
            if (pts != styleShownPts || mult != styleShownMult || gain != styleShownGain)
            {
                styleShownPts = pts; styleShownMult = mult; styleShownGain = gain;
                styleLine1 = style.Points.ToString("0") + " pts   ×" + style.Multiplier.ToString("0.00") + "   gains ×" + style.RankGainScale.ToString("0.0");
            }
            int moves = style.DistinctMoves;
            if (moves != styleShownMoves)
            {
                styleShownMoves = moves;
                styleLine2 = moves + " distinct moves";
            }
        }
        if (health != null)
        {
            int hp = Mathf.RoundToInt(health.Current), hpMax = Mathf.RoundToInt(health.Max);
            if (hp != hpShown || hpMax != hpMaxShown) { hpShown = hp; hpMaxShown = hpMax; hpText = "HP " + health.Current.ToString("0") + " / " + health.Max.ToString("0"); }
            if (health.Chain > 0)
            {
                int chain = health.Chain, next = Mathf.RoundToInt(health.NextHeal);
                if (chain != chainShown || next != chainShownNext) { chainShown = chain; chainShownNext = next; chainText = "PARRY ×" + chain + "   NEXT +" + health.NextHeal.ToString("0"); }
            }
            if (health.LastHealAt != healShownAt) { healShownAt = health.LastHealAt; healText = "+" + health.LastHeal.ToString("0"); }
        }
    }

    static void Fill(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
    }

    static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);

    void OnGUI()
    {
        if (motor == null || PauseMenu.IsPaused || LevelResults.Active || BossIntro.Active || BossOutro.Active || OpeningCutscene.Active || MineBlast.Active) return; // the pause veil, the report card and the cutscenes replace the HUD
        if (Event.current.type != EventType.Repaint) return; // nothing here is a control: the Layout pass has nothing to do
        UIScale.Begin();
        GameFont.Apply();
        if (bigStyle == null)
        {
            bigStyle = new GUIStyle(GUI.skin.label) { font = GameFont.Title, fontSize = 36, fontStyle = GameFont.TitleStyle };
            rankStyle = new GUIStyle(bigStyle) { alignment = TextAnchor.MiddleCenter };
            dialStyle = new GUIStyle(bigStyle) { fontSize = 40, alignment = TextAnchor.LowerLeft };
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            lampLabelStyle = new GUIStyle(GUI.skin.label) { font = GameFont.Brand, fontSize = 9, alignment = TextAnchor.MiddleCenter };
            aimLabelStyle = new GUIStyle(GUI.skin.label) { font = GameFont.Title, fontSize = 16, fontStyle = GameFont.TitleStyle, alignment = TextAnchor.MiddleCenter };
            executeStyle = new GUIStyle(GUI.skin.label) { font = GameFont.Title, fontSize = 46, fontStyle = GameFont.TitleStyle, alignment = TextAnchor.MiddleCenter };
            feedStyle = new GUIStyle(GUI.skin.label) { font = GameFont.Title, fontSize = 20, fontStyle = GameFont.TitleStyle };
            timerStyle = new GUIStyle(GUI.skin.label)
            {
                font = GameFont.Title,
                fontSize = 34,
                fontStyle = GameFont.TitleStyle,
                alignment = TextAnchor.UpperRight,
            };
            timerSubStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                alignment = TextAnchor.UpperRight,
            };
        }

        Vector3 v = motor.Velocity;
        float hSpeed = new Vector2(v.x, v.z).magnitude;

        // The top-left readout is tuning gear, off by default: the tutorial teaches
        // the controls, and speed is felt (lines, FOV) rather than read
        if ((motor.config != null && motor.config.showDebugReadout) || readoutForced)
        {
            GUI.color = MenuStyle.Paper;
            float ly = 16f;
            const float lx = 16f, lw = 480f;
            void Line(string text, GUIStyle st)
            {
                scratch.text = text;
                float lh = st.CalcHeight(scratch, lw);
                UIScale.Label(new Rect(lx, ly, lw, lh), text, st);
                ly += lh;
            }
            Line($"{hSpeed:0.0} m/s", bigStyle);
            Line($"vertical: {v.y:0.0} m/s", labelStyle);
            Line(motor.Sliding ? "SLIDING" : motor.Grounded ? "GROUNDED" : "AIRBORNE", labelStyle);
            if (motor.FrictionDisabled) Line("BHOP WINDOW — friction off", labelStyle);
            if (CombatBaseline.Instance != null) Line(CombatBaseline.Instance.Live, labelStyle);
            Line(MusicDirector.Live, labelStyle);   // the score: side · mood · lead OK/RAW/BAND · bed
            if (LevelWeld.Instance != null) Line(LevelWeld.Instance.Live, labelStyle);
            Line($"fps: {fps:0}   tick: {1f / Time.fixedDeltaTime:0} Hz", labelStyle);
            GUI.color = Color.white;
        }

        DrawCrosshair();
        DrawAim();
        DrawStylePanel();
        DrawSpeedDial();
        DrawHealthBar();
        DrawBossBar();
        DrawRoomStamp();
        DrawPracticeState();

        if (timer != null)
        {
            // THE HOTEL CLOCK: the chip leans under the time with its brass sliver on the RIGHT edge (the slash mirrored
            // for the right margin); the clock face at its left, the minute hand turning once an hour and the second
            // hand once a minute; the time right-aligned beside it, the best beneath
            float tw = 300f;
            Rect chip = new Rect(UIScale.W - tw - 28f, 8f, tw + 16f, 54f);
            Matrix4x4 saved = GUI.matrix;
            UIScale.SkewAround(-16f, chip.center);
            Fill(new Rect(chip.x + 4f, chip.y + 4f, chip.width, chip.height), A(MenuStyle.Ink, 0.5f));
            Fill(chip, A(MenuStyle.Panel, 0.7f));
            Fill(new Rect(chip.xMax - 5f, chip.y, 5f, chip.height), MenuStyle.Brass);
            Fill(new Rect(chip.x, chip.yMax - 1f, chip.width - 5f, 1f), A(MenuStyle.Brass, 0.6f));
            Fill(new Rect(chip.x + 4f, chip.y + 4f, 5f, 5f), A(MenuStyle.Brass, 0.8f));
            GUI.matrix = saved;
            float elapsed = timer.Elapsed;
            Vector2 clock = new Vector2(chip.x + 34f, chip.y + 27f);
            MenuStyle.Instrument(MenuArt.Face.Clock, clock, 46f, 1f);
            MenuStyle.Needle(clock, 13f, elapsed / 3600f * 360f, MenuStyle.Ink, 3f);
            MenuStyle.Needle(clock, 18f, Mathf.Repeat(elapsed, 60f) * 6f, MenuStyle.Oxblood, 2f);
            MenuStyle.Hub(clock, 6f, 1f);
            GUI.color = MenuStyle.Paper;
            UIScale.Label(new Rect(UIScale.W - tw - 20f, 14f, tw, 48f),
                LevelTimer.Format(elapsed), timerStyle);
            GUI.color = Color.white;
            if (timer.Best >= 0f)
            {
                GUI.color = MenuStyle.HudGold;
                UIScale.Label(new Rect(UIScale.W - tw - 20f, 58f, tw, 24f), bestText, timerSubStyle);
                GUI.color = Color.white;
            }
            // The segment: where on the course you are, what fuel stands in it, how long you have been in it
            var flow = CourseFlow.Instance;
            if (flow != null && flow.Current != null)
            {
                GUI.color = A(MenuStyle.Paper, 0.7f);
                UIScale.Label(new Rect(UIScale.W - tw - 20f, 82f, tw, 24f), roomText, timerSubStyle);
                GUI.color = Color.white;
            }
            // The district (an open level has no rooms to count): what is tied in, of what there is — state, never an instruction
            var district = District.Instance;
            if (district != null && district.breakers.Count > 0)
            {
                GUI.color = A(MenuStyle.Paper, district.Powered ? 1f : 0.75f);
                UIScale.Label(new Rect(UIScale.W - tw - 20f, 82f, tw, 24f), district.StateLine, timerSubStyle);
                GUI.color = Color.white;
            }
        }

        // Event feed, newest at the top, fading out
        float y = 120f;
        DrawFeed(ref y);
        UIScale.End();
    }

    // The crosshair cluster: the ring, the dot, the lift lamps (spec §3.1; the overcharge lamp in gold), the grace
    // bar, the chain ticks, and THE THREE LAMPS — KICK / PARRY / WHIP — each verb's readiness. Nothing here is
    // chipped, patterned or backed: at 40 m/s the crosshair cluster is the one clean thing on screen
    void DrawCrosshair()
    {
        float cx = UIScale.W * 0.5f;
        float cy = UIScale.H * 0.5f;
        float t = Time.unscaledTime;
        Texture2D white = Texture2D.whiteTexture;
        float whiff = motor.ParryWhiffCooldownFraction;

        // The ring: a speedometer you feel — spun by your own speed (the 90° gap makes the spin legible), breathing
        // at 0.8 Hz, coloured by the parry state and nothing else
        {
            Vector2 centre = new Vector2(cx, cy);
            float breath = 1f + 0.04f * Mathf.Sin(t * 5f);
            Matrix4x4 saved = GUI.matrix;
            UIScale.RotateAround(ringAngle, centre);
            UIScale.ScaleAround(new Vector2(breath, breath), centre);
            GUI.color = motor.ParryWindowOpen ? A(MenuStyle.HudGold, 0.9f)
                : whiff > 0f ? A(MenuStyle.HudRed, 0.8f)
                : new Color(1f, 1f, 1f, 0.35f);
            GUI.DrawTexture(new Rect(cx - 11f, cy - 11f, 22f, 22f), MenuArt.RingTex);
            GUI.matrix = saved;
        }

        // THE TRICK PROMPT: the bar under the crosshair (DrawTrickBar; the closing ring went on 2026-10-27 for readability)
        DrawTrickBar(cx, cy);

        // The dot itself goes cyan when a weakspot or a cyan target is in reach
        GUI.color = fists != null && (fists.Aim == Fists.AimKind.Weakspot || fists.TargetInWindow)
            ? A(MenuStyle.HudCyan, 1f) : new Color(1f, 1f, 1f, 0.9f);
        GUI.DrawTexture(new Rect(cx - 2f, cy - 2f, 4f, 4f), white);

        if (stamina != null)
        {
            // THE LIFT LAMPS: a brass ring each, the lamp inside lit white when the pip is there, coming on with the
            // regen, dark when spent; the lamp that just came on pops; the overcharge is a fourth lamp, gold
            const float lampW = 12f, gap = 6f;
            float total = stamina.MaxPips * lampW + (stamina.MaxPips - 1) * gap;
            float x0 = cx - total * 0.5f + lampW * 0.5f;
            float py = cy + 28f;
            float pop = 1f + 0.3f * (1f - Mathf.Clamp01((t - pipPopAt) / 0.1f));
            Color ring = A(MenuStyle.Brass, 0.75f);

            for (int i = 0; i < stamina.MaxPips; i++)
            {
                Vector2 p = new Vector2(x0 + i * (lampW + gap), py);
                bool popping = pop > 1f && i == stamina.Pips - 1;
                Matrix4x4 saved = GUI.matrix;
                if (popping) UIScale.ScaleAround(new Vector2(pop, pop), p);
                if (i < stamina.Pips) MenuStyle.Lamp(p, lampW, new Color(1f, 1f, 1f, 0.95f), 1f, ring);
                else if (i == stamina.Pips && stamina.RegenProgress > 0f) MenuStyle.Lamp(p, lampW, new Color(1f, 1f, 1f, 0.6f), stamina.RegenProgress, ring);
                else MenuStyle.Lamp(p, lampW, new Color(1f, 1f, 1f, 0.22f), 1f, ring);
                if (popping) GUI.matrix = saved;
            }
            if (stamina.Overcharge)
                MenuStyle.Lamp(new Vector2(x0 + stamina.MaxPips * (lampW + gap), py), lampW, A(MenuStyle.HudGold, 0.95f), 1f, A(MenuStyle.HudGold, 0.8f));
        }

        // (The three readiness lamps with their words KICK / PARRY / WHIP went on 2026-10-27: "too much clutter in the screen")

        // The landing grace, draining: the window a bhop keeps speed in. White, above the lamps, gone when friction is back
        float grace = motor.BhopGraceFraction;
        if (grace > 0f) graceSeenAt = Time.time;
        float graceTail = 1f - Mathf.Clamp01((Time.time - graceSeenAt) / 0.35f);   // the track lingers after the 100 ms drain, so it is a bar and not a blink
        if (graceTail > 0f)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.22f * graceTail);
            GUI.DrawTexture(new Rect(cx - 30f, cy + 18f, 60f, 3f), white);
        }
        if (grace > 0f)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            GUI.DrawTexture(new Rect(cx - 30f, cy + 18f, 60f * grace, 3f), white);
        }
        // The chain of three: a tick per distinct airborne move, gold for a beat when it pays
        {
            int target = Mathf.Clamp(ResetDetector.ChainTarget, 1, 6), progress = ResetDetector.ChainProgress;
            bool paid = Time.time - chainFiredAt < 0.3f;
            for (int i = 0; i < target; i++)
            {
                GUI.color = paid ? A(MenuStyle.HudGold, 0.95f) : new Color(1f, 1f, 1f, i < progress ? 0.95f : 0.22f);
                GUI.DrawTexture(new Rect(cx - 50f - i * 8f, cy + 47f, 5f, 5f), white);
            }
        }
        GUI.color = Color.white;
    }

    // What the next press would meet, around the crosshair: four brackets close in on it — CYAN + "EXECUTE" on a
    // weakspot, CYAN + "BREAK" on a cyan target, white + the combo's next step on a body; dim while the arms recover.
    // A connect pops a hit marker; an execute adds an expanding diamond and stamps EXECUTE over the view
    void DrawAim()
    {
        if (fists == null) return;
        Texture2D white = Texture2D.whiteTexture;
        float cx = UIScale.W * 0.5f, cy = UIScale.H * 0.5f;
        Vector2 center = new Vector2(cx, cy);
        float now = Time.unscaledTime;
        Color cyan = MenuStyle.HudCyan;

        var aim = fists.Aim;
        bool target = aim == Fists.AimKind.None && fists.TargetInWindow;
        if (aim != Fists.AimKind.None || target)
        {
            bool weak = aim == Fists.AimKind.Weakspot;
            bool cyanRead = weak || target;
            float half = weak ? 13f + Mathf.Sin(now * 16f) * 1.5f : 19f;
            float arm = weak ? 7f : 5f, thick = 2f;
            float ready = 0.35f + 0.65f * fists.Readiness;
            Color c = cyanRead ? new Color(cyan.r, cyan.g, cyan.b, ready) : new Color(1f, 1f, 1f, 0.8f * ready);
            GUI.color = c;
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    float x = cx + sx * half, y = cy + sy * half;
                    GUI.DrawTexture(new Rect(sx < 0 ? x : x - arm, y - thick * 0.5f, arm, thick), white);
                    GUI.DrawTexture(new Rect(x - thick * 0.5f, sy < 0 ? y : y - arm, thick, arm), white);
                }
            }
            aimLabelStyle.normal.textColor = c;
            // The word for what a press would do: the execute, the break, or the combo's next step
            UIScale.Label(new Rect(cx - 80f, cy - 54f, 160f, 22f), weak ? "EXECUTE" : target ? "BREAK" : Fists.NextStep, aimLabelStyle);
        }

        float age = now - hitAt;
        // Hit marker: four ticks kicked outward (cyan + a diamond ring on executes)
        if (age >= 0f && age < 0.25f)
        {
            float k = age / 0.25f;
            float alpha = 1f - k;
            float dist = Mathf.Lerp(6f, hitExecute ? 26f : 16f, 1f - Mathf.Pow(1f - k, 3f));
            float tick = hitExecute ? 12f : 8f;
            GUI.color = hitExecute ? new Color(cyan.r, cyan.g, cyan.b, alpha) : new Color(1f, 1f, 1f, alpha);
            for (int i = 0; i < 4; i++)
            {
                Matrix4x4 saved = GUI.matrix;
                UIScale.RotateAround(45f + 90f * i, center);
                GUI.DrawTexture(new Rect(cx + dist, cy - 1f, tick, 2f), white);
                GUI.matrix = saved;
            }
            if (hitExecute)
            {
                float r = Mathf.Lerp(10f, 70f, 1f - Mathf.Pow(1f - k, 2f));
                Matrix4x4 saved = GUI.matrix;
                UIScale.RotateAround(45f, center);
                GUI.color = new Color(cyan.r, cyan.g, cyan.b, alpha * 0.8f);
                GUI.DrawTexture(new Rect(cx - r, cy - r, 2f * r, 2f), white);
                GUI.DrawTexture(new Rect(cx - r, cy + r - 2f, 2f * r, 2f), white);
                GUI.DrawTexture(new Rect(cx - r, cy - r, 2f, 2f * r), white);
                GUI.DrawTexture(new Rect(cx + r - 2f, cy - r, 2f, 2f * r), white);
                GUI.matrix = saved;
            }
        }

        // EXECUTE stamp: Persona tilt, punches in big and settles
        if (age >= 0f && age < 0.6f && hitExecute)
        {
            float alpha = age < 0.42f ? 1f : 1f - (age - 0.42f) / 0.18f;
            float punch = 1f + 0.35f * Mathf.Pow(1f - Mathf.Clamp01(age / 0.12f), 2f);
            Vector2 pivot = new Vector2(cx, cy - 120f);
            Matrix4x4 saved = GUI.matrix;
            UIScale.RotateAround(-6f, pivot);
            UIScale.ScaleAround(new Vector2(punch, punch), pivot);
            executeStyle.normal.textColor = new Color(cyan.r, cyan.g, cyan.b, alpha);
            UIScale.Label(new Rect(cx - 200f, cy - 150f, 400f, 60f), "EXECUTE", executeStyle);
            GUI.matrix = saved;
        }
        GUI.color = Color.white;
    }

    // THE SPEED DIAL, bottom-left above the health tube: brass bezel, cream face, 0–30 m/s round a 270° sweep with the
    // perfect-hop band (18–30) marked in brass; the oxblood needle, the figure beside it in big type with the units under it
    void DrawSpeedDial()
    {
        const float size = 112f;
        float H = UIScale.H;
        Vector2 pivot = new Vector2(24f + size * 0.5f, H - 126f - size * 0.5f);
        MenuStyle.Instrument(MenuArt.Face.Dial, pivot, size, 1f);
        MenuStyle.Needle(pivot, size * 0.38f, MenuStyle.Sweep(needleSpeed / DialMax), MenuStyle.Oxblood, 5f);
        MenuStyle.Hub(pivot, 11f, 1f);
        // The figure: the true speed, one string per tenth
        float fx = pivot.x + size * 0.5f + 14f;
        GUI.color = MenuStyle.Paper;
        UIScale.Label(new Rect(fx, pivot.y - 40f, 120f, 50f), speedText, dialStyle);
        GUI.color = A(MenuStyle.Paper, 0.7f);
        UIScale.Label(new Rect(fx + 2f, pivot.y + 10f, 120f, 20f), "m/s", labelStyle);
        GUI.color = Color.white;
    }

    // Style rank, progress rule, and freshness multiplier (spec §5) on a leaning slab in the corner, the rank a marquee
    // letter card beside the readouts; gold screen frame (an inner hairline, hatch crawling) while FLOW STATE is active
    void DrawStylePanel()
    {
        if (style == null) return;
        Texture2D white = Texture2D.whiteTexture;
        float t = Time.unscaledTime;
        float w = 300f;
        float x = UIScale.W - w - 24f;
        float y = UIScale.H - 110f;
        float blink = style.FlowDraining ? 0.55f + 0.45f * Mathf.Sin(t * 22f) : 1f;

        if (style.FlowState)
        {
            float W = UIScale.W, H = UIScale.H;
            Color frame = A(MenuStyle.HudGold, 0.65f);
            Color crawl = A(MenuStyle.Ink, 0.30f);
            Rect top = new Rect(0, 0, W, 6f), bottom = new Rect(0, H - 6f, W, 6f), left = new Rect(0, 0, 6f, H), right = new Rect(W - 6f, 0, 6f, H);
            GUI.color = frame;
            GUI.DrawTexture(top, white);
            GUI.DrawTexture(bottom, white);
            GUI.DrawTexture(left, white);
            GUI.DrawTexture(right, white);
            // A hairline inset from the border: the double rule, gold on gold
            GUI.DrawTexture(new Rect(10f, 10f, W - 20f, 1f), white);
            GUI.DrawTexture(new Rect(10f, H - 11f, W - 20f, 1f), white);
            GUI.DrawTexture(new Rect(10f, 10f, 1f, H - 20f), white);
            GUI.DrawTexture(new Rect(W - 11f, 10f, 1f, H - 20f), white);
            MenuStyle.Hatch(top, crawl, 24f);
            MenuStyle.Hatch(bottom, crawl, 24f);
            MenuStyle.Hatch(left, crawl, 24f);
            MenuStyle.Hatch(right, crawl, 24f);

            GUI.color = A(MenuStyle.HudGold, 1f);
            UIScale.Label(new Rect(x, y - 34f, w, 30f), "FLOW STATE", bigStyle);
            // The fuse: it burns down while nothing fresh happens; burnt out, style pours away
            Rect fuse = new Rect(x, y - 8f, w, 5f);
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(fuse, white);
            GUI.color = style.FlowDraining ? new Color(1f, 0.25f, 0.2f, blink) : style.FlowTimer01 < 0.3f ? new Color(1f, 0.5f, 0.2f) : MenuStyle.HudGold;
            GUI.DrawTexture(new Rect(fuse.x, fuse.y, fuse.width * style.FlowTimer01, fuse.height), white);
            if (style.FlowDraining)
            {
                GUI.color = A(MenuStyle.HudRed, blink);
                UIScale.Label(new Rect(x, y - 58f, w, 24f), "FLOW FADING", feedStyle);
            }
        }

        // The slab, leaning at the band's angle: shadow, charcoal, the brass sliver, a hairline along its top, a
        // plate in its far corner, the rank rule
        Rect slab = new Rect(x - 12f, y - 6f, 324f, 62f);
        Matrix4x4 m = GUI.matrix;
        UIScale.SkewAround(-16f, slab.center);
        Fill(new Rect(slab.x + 4f, slab.y + 4f, slab.width, slab.height), A(MenuStyle.Ink, 0.5f));
        Fill(slab, A(MenuStyle.Panel, 0.72f));
        Fill(new Rect(slab.x, slab.y, 5f, slab.height), MenuStyle.Brass);
        if (!UIAssets.Frame(slab, UIAssets.Border("border-005"), 12f, A(MenuStyle.Brass, 0.85f)))   // THE BORROWED FRAME (2026-10-27): the plated corners of Kenney's border, brass; the drawn hairline and plate without it
        {
            Fill(new Rect(slab.x + 5f, slab.y, slab.width - 5f, 1f), A(MenuStyle.Brass, 0.6f));
            Fill(new Rect(slab.xMax - 9f, slab.y + 4f, 5f, 5f), A(MenuStyle.Brass, 0.8f));
        }
        Rect bar = new Rect(x + 56f, y + 48f, w - 56f, 6f);
        Fill(bar, A(MenuStyle.Ink, 0.6f));
        Fill(new Rect(bar.x, bar.y, bar.width * style.RankProgress, bar.height), style.FlowState ? MenuStyle.HudGold : A(MenuStyle.Paper, 0.9f));
        Fill(new Rect(bar.x, bar.yMax, bar.width, 1f), A(MenuStyle.Brass, 0.5f));
        GUI.matrix = m;
        GUI.color = Color.white;

        // THE MARQUEE: punches in on a rank change; gold and ink in flow, blinking with the fuse while it drains
        float punch = 1f + 0.35f * Mathf.Pow(1f - Mathf.Clamp01((t - rankChangedAt) / 0.14f), 2f);
        MenuStyle.MarqueeCard(new Rect(x, y, 48f, 48f), style.Rank, style.FlowState, style.FlowState ? blink : 1f, punch, 1f, rankStyle, t);
        GUI.color = MenuStyle.Paper;
        UIScale.Label(new Rect(x + 60f, y + 2f, w - 60f, 24f), styleLine1, labelStyle);
        UIScale.Label(new Rect(x + 60f, y + 22f, w - 60f, 24f), styleLine2, labelStyle);
        GUI.color = Color.white;
    }

    // The host's gauge, and it is live: every jab, parry, counter and execute takes
    // a bite — the bar flashes white, jolts on the big ones, shows the bite's name
    // and size, and a ghost band drains after it. A brass pressure gauge at its
    // left end reads the same pressure with a needle. Under it: the buttons still on
    // his coat in phase one, the phase name, and EXECUTE in cyan (with the spot
    // and the timer) while a weakspot is open. The subtitle at the end.
    GUIStyle bossNameStyle, bossNoteStyle, bossSubStyle, bossHitStyle, bossPhaseStyle;
    void DrawBossBar()
    {
        var boss = Boss.Current;
        if (boss == null) return;
        bool dead = boss.IsDead;
        if (!boss.Engaged && !dead) return;
        if (bossNameStyle == null)
        {
            bossNameStyle = new GUIStyle(GUI.skin.label) { font = GameFont.Title, fontSize = 26, fontStyle = GameFont.TitleStyle, alignment = TextAnchor.MiddleLeft };
            bossPhaseStyle = new GUIStyle(GUI.skin.label) { font = GameFont.Brand, fontSize = 13, alignment = TextAnchor.MiddleRight };
            bossNoteStyle = new GUIStyle(GUI.skin.label) { font = GameFont.Title, fontSize = 18, fontStyle = GameFont.TitleStyle, alignment = TextAnchor.MiddleCenter };
            bossHitStyle = new GUIStyle(GUI.skin.label) { font = GameFont.Title, fontSize = 20, fontStyle = GameFont.TitleStyle, alignment = TextAnchor.MiddleLeft };
            bossSubStyle = new GUIStyle(GUI.skin.label) { font = GameFont.Brand, fontSize = 20, alignment = TextAnchor.MiddleCenter };
        }
        Texture2D white = Texture2D.whiteTexture;
        float now = Time.unscaledTime;
        float sinceHit = now - boss.LastHitTime;
        float flash = Mathf.Clamp01(1f - sinceHit / 0.25f);
        float jolt = boss.LastHitFraction >= 0.08f ? Mathf.Clamp01(1f - sinceHit / 0.22f) : 0f;
        float jx = jolt * Mathf.Sin(now * 90f) * 5f, jy = jolt * Mathf.Cos(now * 70f) * 3f;
        float cx = UIScale.W * 0.5f;
        float w = Mathf.Min(680f, UIScale.W * 0.52f), y = 22f;
        Color gold = new Color(1f, 0.85f, 0.3f);
        Color cyan = MenuStyle.HudCyan;
        Color red = new Color(1f, 0.32f, 0.28f);

        if (!dead)
        {
            // The scripted beat: on the tick the voice says you've been elevated, the slab ticks rot and ink and
            // the phase reads ELEVATED — the only red the house is allowed to put on the HUD, once
            bool beat = boss.PhaseName == boss.BeatPhase && now - phaseChangedAt < 0.3f; // two literals: no allocation
            int state = beat ? MenuStyle.RotState((int)((now - phaseChangedAt) / 0.03f)) : -1;

            bossNameStyle.normal.textColor = gold;
            UIScale.Label(new Rect(cx - w * 0.5f + jx, y + jy, w * 0.6f, 30f), boss.BossName, bossNameStyle);
            bossPhaseStyle.normal.textColor = beat ? MenuStyle.Rot : A(MenuStyle.Paper, 0.75f); // the word stays red on any ground; only the slab ticks
            UIScale.Label(new Rect(cx - w * 0.1f + jx, y + 6f + jy, w * 0.6f, 22f), beat ? boss.BeatWord : boss.PhaseName, bossPhaseStyle);

            Rect bar = new Rect(cx - w * 0.5f + jx, y + 36f + jy, w, 13f);
            DrawBarSlab(bar, beat ? (state == 0 ? A(MenuStyle.Rot, 0.85f) : A(MenuStyle.Ink, 0.85f)) : A(MenuStyle.Panel, 0.85f));
            Color pressure = boss.OpenSpot != "" ? cyan : red;
            // The ghost band: what the last bite took, draining after it
            GUI.color = new Color(1f, 0.9f, 0.6f, 0.85f);
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * boss.DisplayHealth01, bar.height), white);
            // The bar itself: red, cyan while a weakspot is open
            GUI.color = pressure;
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * boss.Health01, bar.height), white);
            if (flash > 0f)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.9f * flash);
                GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * boss.DisplayHealth01, bar.height), white);
            }
            for (int i = 1; i < 4; i++)
            {
                GUI.color = A(MenuStyle.Ink, 0.8f);
                GUI.DrawTexture(new Rect(bar.x + bar.width * i / 4f - 1f, bar.y - 2f, 2f, bar.height + 4f), white);
            }
            // The brass tube round the bar, and the gauge at its left end reading the same pressure
            MenuStyle.Tube(new Rect(bar.x - 5f, bar.y - 5f, bar.width + 10f, bar.height + 10f), 1f);
            DrawGauge(new Vector2(bar.x - 34f, bar.y + 6f), 44f, boss.Health01, pressure);
            GUI.color = Color.white;

            // THE FRONT (the chase): a red strip under the bar filling as the collapse closes on you — 0 forty metres
            // back, full when it is at your heels; the one red the chase puts on the panel, and it pulses as it fills
            var front = ChasePressure.Instance;
            float danger = front != null ? front.Danger01 : 0f;
            if (danger > 0f)
            {
                Rect fr = new Rect(bar.x, bar.yMax + 8f, bar.width, 4f);
                GUI.color = A(MenuStyle.Panel, 0.7f);
                GUI.DrawTexture(fr, white);
                float pulse = 0.7f + 0.3f * Mathf.Sin(now * (6f + 10f * danger));
                GUI.color = new Color(red.r, red.g, red.b, Mathf.Lerp(0.5f, 1f, danger) * pulse);
                GUI.DrawTexture(new Rect(fr.x, fr.y, fr.width * danger, fr.height), white);
                GUI.color = Color.white;
            }

            // The bite: its name and size, rising off the bar's edge and fading
            if (sinceHit < 0.9f && !string.IsNullOrEmpty(boss.LastHitLabel))
            {
                float a = Mathf.Clamp01(1f - (sinceHit - 0.45f) / 0.45f);
                float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(sinceHit / 0.35f)) * 14f;
                float hx = Mathf.Clamp(bar.x + bar.width * boss.Health01 + 8f, bar.x, bar.xMax - 220f);
                bossHitStyle.normal.textColor = A(MenuStyle.Paper, a);
                UIScale.Label(new Rect(hx, bar.yMax + 2f - rise, 240f, 26f), bossHitText, bossHitStyle); // the bite and its size, or SHIELDED: a bite of nothing
            }

            // Under the bar: buttons in phase one, the open spot in cyan while it lasts
            float uy = bar.yMax + 10f;
            if (boss.ShowPlates)
            {
                int n = boss.PlateCount;
                for (int i = 0; i < n; i++)
                {
                    bool on = i < boss.PlatesLeft;
                    GUI.color = on ? gold : A(MenuStyle.Paper, 0.18f);
                    GUI.DrawTexture(new Rect(bar.xMax - n * 18f + i * 18f, uy + 4f, 12f, 12f), white);
                }
                GUI.color = Color.white;
            }
            if (boss.OpenSpot != "")
            {
                float pulse = 0.7f + 0.3f * Mathf.Sin(now * 18f);
                bossNoteStyle.normal.textColor = new Color(cyan.r, cyan.g, cyan.b, pulse);
                UIScale.Label(new Rect(cx - w * 0.5f, uy, w, 24f), executeText, bossNoteStyle);
                if (boss.WindowOpen)
                {
                    float frac = Mathf.Clamp01(boss.StaggerLeft / Mathf.Max(0.1f, boss.StaggerWindow));
                    GUI.color = new Color(cyan.r, cyan.g, cyan.b, 0.9f);
                    GUI.DrawTexture(new Rect(cx - w * 0.25f, uy + 26f, w * 0.5f * frac, 4f), white);
                    GUI.color = Color.white;
                }
            }
        }

        if (!string.IsNullOrEmpty(boss.Subtitle) && Time.time < boss.SubtitleUntil)
        {
            float a = Mathf.Clamp01((boss.SubtitleUntil - Time.time) / 0.8f);
            bossSubStyle.normal.textColor = new Color(gold.r, gold.g, gold.b, a);
            UIScale.Label(new Rect(cx - UIScale.W * 0.4f, UIScale.H - 130f, UIScale.W * 0.8f, 30f), boss.Subtitle, bossSubStyle);
        }
    }

    /// A brass pressure gauge: the face, the needle in the bar's own colour (its meaning), the hub
    static void DrawGauge(Vector2 pivot, float size, float value01, Color needle)
    {
        MenuStyle.Instrument(MenuArt.Face.Gauge, pivot, size, 1f);
        MenuStyle.Needle(pivot, size * 0.36f, MenuStyle.Sweep(value01), needle, 3f);
        MenuStyle.Hub(pivot, 6f, 1f);
    }

    /// The slab under a unit's bar, leaning at the band's angle: a bronze frame 3 px proud, then the charcoal
    void DrawBarSlab(Rect bar, Color slabColor)
    {
        Rect slab = new Rect(bar.x - 10f, bar.y - 8f, bar.width + 20f, bar.height + 16f);
        Matrix4x4 saved = GUI.matrix;
        UIScale.SkewAround(-16f, slab.center);
        Fill(new Rect(slab.x - 3f, slab.y - 3f, slab.width + 6f, slab.height + 6f), A(MenuStyle.Bullion, 0.85f));
        Fill(slab, slabColor);
        GUI.matrix = saved;
        GUI.color = Color.white;
    }

    // THE CHECKPOINT'S NAME: a stamp that pops and fades under the boss bar line, the company's sun bursting behind it as it lands
    GUIStyle roomStampStyle;
    void DrawRoomStamp()
    {
        var loop = CourseFlow.Instance;
        if (loop == null || Time.unscaledTime >= loop.StampUntil || string.IsNullOrEmpty(loop.Stamp)) return;
        if (roomStampStyle == null)
            roomStampStyle = new GUIStyle(GUI.skin.label) { font = GameFont.Title, fontSize = 44, fontStyle = GameFont.TitleStyle, alignment = TextAnchor.MiddleCenter };
        float left = loop.StampUntil - Time.unscaledTime;
        float alpha = Mathf.Clamp01(left / 0.35f);
        float age = Mathf.Clamp01(1f - left / 0.9f);
        float punch = 1f + 0.25f * Mathf.Exp(-age * 12f);
        Vector2 pivot = new Vector2(UIScale.W * 0.5f, 130f);
        MenuStyle.StampBurst(pivot, 120f, (Time.unscaledTime - stampAt) / 0.3f, A(loop.StampColor, 0.25f));
        Matrix4x4 saved = GUI.matrix;
        UIScale.ScaleAround(new Vector2(punch, punch), pivot);
        roomStampStyle.normal.textColor = new Color(loop.StampColor.r, loop.StampColor.g, loop.StampColor.b, alpha);
        UIScale.Label(new Rect(pivot.x - 300f, pivot.y - 30f, 600f, 60f), loop.Stamp, roomStampStyle);
        GUI.matrix = saved; // back to the plain canvas
    }

    // THE MERCURY COLUMN: bottom-left, a brass tube on a leaning slab with a bulb at its left end — the column red
    // (orange and hatched when low), white flash on a hit, gold on a parry's heal; the hard damage a grey hatched band
    // at the top the column cannot fill until it has decayed; HP above it; beside it the parry chain in its own chip:
    // how many in a row and what the next one puts back, greying as the chain goes cold
    GUIStyle healStyle;
    void DrawHealthBar()
    {
        if (health == null) return;
        if (healStyle == null) healStyle = new GUIStyle(GUI.skin.label) { font = GameFont.Title, fontSize = 16, fontStyle = GameFont.TitleStyle };
        Texture2D white = Texture2D.whiteTexture;
        float x = 28f, y = UIScale.H - 64f, w = 268f, h = 14f;
        bool flash = Time.time - health.LastHitTime < 0.15f;
        bool healed = Time.time - health.LastHealAt < 0.25f;

        Rect slab = new Rect(8f, y - 30f, 296f, 56f);
        Matrix4x4 saved = GUI.matrix;
        UIScale.SkewAround(-16f, slab.center);
        Fill(new Rect(slab.x + 4f, slab.y + 4f, slab.width, slab.height), A(MenuStyle.Ink, 0.5f));
        Fill(slab, A(MenuStyle.Panel, 0.75f));
        Fill(new Rect(slab.x, slab.y, 5f, slab.height), MenuStyle.Brass);
        if (!UIAssets.Frame(slab, UIAssets.Border("border-005"), 12f, A(MenuStyle.Brass, 0.85f)))   // THE BORROWED FRAME (2026-10-27): the plated corners of Kenney's border, brass; the drawn hairline and plate without it
        {
            Fill(new Rect(slab.x + 5f, slab.y, slab.width - 5f, 1f), A(MenuStyle.Brass, 0.6f));
            Fill(new Rect(slab.xMax - 9f, slab.y + 4f, 5f, 5f), A(MenuStyle.Brass, 0.8f));
        }
        GUI.matrix = saved;
        // The glass and the column, straight (a tube does not lean)
        Fill(new Rect(x, y, w, h), A(MenuStyle.Ink, 0.7f));
        Rect fill = new Rect(x, y, w * health.Fraction, h);
        Color column = flash ? Color.white : healed ? (health.LastHealParry ? MenuStyle.HudGold : Color.Lerp(MenuStyle.HudHealth, Color.white, 0.45f))
            : health.Fraction > 0.35f ? MenuStyle.HudHealth : MenuStyle.HudLow;
        Fill(fill, column);
        if (health.Fraction < 0.35f) MenuStyle.Hatch(fill, A(MenuStyle.Ink, 0.30f), 48f);
        if (health.HardFraction > 0.005f)
        {
            // Hard damage: the top of the column the feed cannot fill until it has decayed (grey, hatched)
            Rect hard = new Rect(x + w * (1f - health.HardFraction), y, w * health.HardFraction, h);
            Fill(hard, A(new Color(0.45f, 0.42f, 0.38f), 0.9f));
            MenuStyle.Hatch(hard, A(MenuStyle.Ink, 0.35f), 32f);
        }
        // The mercury's meniscus: a lit edge at the column's end
        if (health.Fraction > 0.01f) Fill(new Rect(fill.xMax - 1f, y, 2f, h), A(Color.white, 0.5f));
        MenuStyle.Tube(new Rect(x - 5f, y - 5f, w + 10f, h + 10f), 1f);
        // The bulb at the left end: a brass ring with the column's colour in it
        MenuStyle.Lamp(new Vector2(x - 6f, y + h * 0.5f), 22f, health.Fraction > 0.01f ? column : A(MenuStyle.Ink, 0.7f), 1f, A(MenuStyle.Brass, 0.95f));
        if (health.Chain > 0)
        {
            Rect chip = new Rect(x + w + 10f, y - 8f, 270f, 26f);
            UIScale.SkewAround(-16f, chip.center);
            Fill(chip, A(MenuStyle.Ink, 0.6f));
            GUI.matrix = saved;
        }
        GUI.color = MenuStyle.Paper;
        UIScale.Label(new Rect(x - 8f, y - 26f, 220f, 22f), hpText, labelStyle);
        GUI.color = Color.white;
        if (health.Chain > 0)
        {
            healStyle.normal.textColor = Color.Lerp(A(MenuStyle.Sick, 0.45f), A(MenuStyle.HudGold, 1f), health.ChainWarmth);
            UIScale.Label(new Rect(x + w + 16f, y - 4f, 260f, 22f), chainText, healStyle);
        }
        float age = Time.time - health.LastHealAt;
        if (age < 1f && health.LastHeal > 0f)
        {
            healStyle.normal.textColor = A(health.LastHealParry ? MenuStyle.HudGold : MenuStyle.HudHealth, 1f - age);
            UIScale.Label(new Rect(x + w + 16f, y - 30f - age * 20f, 120f, 22f), healText, healStyle);
        }
    }

    // The range's console: what is switched on, top left, gold on an ink chip — state, so you never forget you are cheating
    void DrawPracticeState()
    {
        if (!Practice.Any || lastSummary == null) return;
        if (summaryWidth <= 0f) summaryWidth = MenuStyle.Measure(feedStyle, lastSummary).x; // the style was built after Update measured
        Rect chip = new Rect(12f, 14f, summaryWidth + 20f, 26f);
        Matrix4x4 saved = GUI.matrix;
        UIScale.SkewAround(-16f, chip.center);
        Fill(chip, A(MenuStyle.Ink, 0.55f));
        GUI.matrix = saved;
        GUI.color = MenuStyle.HudGold;
        UIScale.Label(new Rect(20f, 14f, 800f, 26f), lastSummary, feedStyle);
        GUI.color = Color.white;
    }

    // THE TICKER: each line on a cream tape chip that slides in under it, ink type; highlights go the other way — an
    // ink chip with a gold sliver and gold type (gold never sits on cream); hidden tech lands as a rot beat first —
    // the one glitch that is yours
    void DrawFeed(ref float y)
    {
        float x = UIScale.W * 0.5f + 60f;
        for (int i = feed.Count - 1; i >= 0; i--)
        {
            FeedEntry e = feed[i];
            float age = Time.time - e.time;
            if (age > FeedLifetime) continue;
            if (y + 26f > UIScale.H * 0.5f - 150f) break; // nothing backed inside the crosshair's clear zone
            if (e.width <= 0f) { e.width = MenuStyle.Measure(feedStyle, e.text).x; feed[i] = e; } // measured once, written back
            float alpha = Mathf.Clamp01(1f - age / FeedLifetime);
            float slideIn = 30f * (1f - Mathf.Clamp01(age / 0.12f));
            bool beat = e.glitch && age < 0.18f;
            int state = beat ? MenuStyle.RotState((int)(age / 0.03f)) : -1;
            Color text = beat ? (state == 0 ? MenuStyle.Ink : MenuStyle.Rot)
                : e.highlight ? A(MenuStyle.HudGold, alpha) : A(MenuStyle.Ink, alpha);

            Rect chip = new Rect(x - 8f - slideIn, y, e.width + 20f, 26f);
            Matrix4x4 saved = GUI.matrix;
            UIScale.SkewAround(-16f, chip.center);
            if (beat) Fill(chip, state == 0 ? A(MenuStyle.Rot, 0.9f) : A(MenuStyle.Ink, 0.9f));
            else if (e.highlight)
            {
                Fill(chip, A(MenuStyle.Ink, 0.7f * alpha));
                Fill(new Rect(chip.x, chip.y, 4f, chip.height), A(MenuStyle.HudGold, alpha));
            }
            else MenuStyle.TapeChip(chip, MenuStyle.Paper, 0.85f * alpha);
            GUI.matrix = saved;
            GUI.color = Color.white;

            feedStyle.normal.textColor = text;
            UIScale.Label(new Rect(x, y, 500f, 30f), e.text, feedStyle);
            y += 28f;
        }
        feedStyle.normal.textColor = Color.white;
    }
    // THE TRICK BAR (2026-10-27; the user: "change the qte to a bar style qte instead of a circle closing in, helps
    // with readability"). Under the crosshair, a track the marker runs along, left to right, over the window: the
    // brass band is the hit zone, the gold sliver at its middle the perfect beat, the hairline the beat itself; the
    // trick key's glyph (Kenney's prompt) at the left end. A press stamps where the marker stood — a gold star on a
    // perfect, a cream tick on a lazy press, the rot's red cross on a miss (the marker stops just past the band when
    // the beat went by unpressed) — and a landing that pays lights the whole bar gold. While a trick could still be
    // armed, the glyph alone sits there, dim: the invitation. Never cyan: cyan is the thing you break
    void DrawTrickBar(float cx, float cy)
    {
        var trick = TrickSystem.Instance;
        if (trick == null) return;
        const float w = 240f, h = 14f;
        float now = Time.unscaledTime;
        float y = cy + 64f;
        float sincePress = now - trick.PressedAt, sinceLand = now - trick.LandedAt;
        bool open = trick.PromptOpen;
        bool failed = trick.FailedAt >= trick.PressedAt;   // Fail() stamps both in one frame; a hit stamps the press alone
        float tailFor = failed ? 0.55f : 0.35f;
        bool pressTail = sincePress >= 0f && sincePress < tailFor;
        bool payTail = trick.LandedPaid && sinceLand >= 0f && sinceLand < 0.45f;
        if (!open && !pressTail && !payTail)
        {
            bool canArm = !trick.Active && !trick.Armed && !motor.Grounded && !motor.WallRunning && trick.config != null
                && trick.RemainingAir >= trick.config.trickQteLead + 0.05f;
            if (canArm) UIAssets.Draw(new Rect(cx - 17f, y + h * 0.5f - 17f, 34f, 34f), UIAssets.Glyph(GameAction.Trick), A(MenuStyle.Paper, 0.35f));
            GUI.color = Color.white;
            return;
        }

        float x = cx - w * 0.5f;
        if (failed && pressTail && sincePress < 0.18f) x += Mathf.Repeat(sincePress * 40f, 1f) < 0.5f ? 2f : -2f;   // the miss jolts the bar
        Rect track = new Rect(x, y, w, h);
        float half = Mathf.Clamp(trick.WindowFraction, 0.02f, 0.5f);
        float perfect = Mathf.Clamp(trick.PerfectFraction, 0.01f, half);
        float u = open ? trick.Prompt01 : trick.Pressed01;
        float mx = x + w * u;

        // the glass: ink, the hit zone in brass, the beat in gold with a hairline through it, the quarter ticks
        Fill(new Rect(x + 3f, y + 3f, w, h), A(MenuStyle.Ink, 0.45f));
        Fill(track, A(MenuStyle.Ink, 0.78f));
        Fill(new Rect(x + w * (0.5f - half), y, w * 2f * half, h), A(MenuStyle.Brass, 0.55f));
        Fill(new Rect(x + w * (0.5f - perfect), y, w * 2f * perfect, h), A(MenuStyle.HudGold, 0.95f));
        Fill(new Rect(x + w * 0.5f - 0.5f, y - 4f, 1f, h + 8f), new Color(1f, 1f, 1f, 0.7f));
        Fill(new Rect(x + w * 0.25f, y + h - 3f, 1f, 3f), A(MenuStyle.Sick, 0.6f));
        Fill(new Rect(x + w * 0.75f, y + h - 3f, 1f, 3f), A(MenuStyle.Sick, 0.6f));
        if (payTail) Fill(track, A(MenuStyle.HudGold, 0.85f * (1f - sinceLand / 0.45f)));
        else if (failed && pressTail) Fill(track, A(MenuStyle.HudRed, 0.6f * (1f - sincePress / tailFor)));

        // the frame: the borrowed double rule in brass (the drawn tube when the sprite is missing)
        Rect frame = new Rect(x - 5f, y - 5f, w + 10f, h + 10f);
        if (!UIAssets.Frame(frame, UIAssets.Border("border-004"), 8f, A(MenuStyle.Brass, 0.95f))) MenuStyle.Tube(frame, 1f);

        // the marker: the pack's slider handle riding over the track (a needle when it is missing)
        Color mc = open ? A(MenuStyle.Paper, 0.95f) : failed ? A(MenuStyle.HudRed, 0.9f) : trick.PressedPerfect ? MenuStyle.HudGold : MenuStyle.Paper;
        if (!UIAssets.Draw(new Rect(mx - 9f, y - 9f, 18f, 24f), UIAssets.Pack("slide_hangle"), mc)) Fill(new Rect(mx - 1.5f, y - 6f, 3f, h + 12f), mc);

        // the key: the prompt's glyph at the left end (its label when the glyph is missing)
        if (!UIAssets.Draw(new Rect(x - 46f, y + h * 0.5f - 17f, 34f, 34f), UIAssets.Glyph(GameAction.Trick), A(MenuStyle.Paper, open ? 0.95f : 0.6f)))
        {
            lampLabelStyle.normal.textColor = A(MenuStyle.Paper, 0.9f);
            UIScale.Label(new Rect(x - 52f, y, 44f, h), Keybinds.Label(GameAction.Trick), lampLabelStyle);
        }

        // the verdict, popping off the bar under where the press landed; the pay's star under the middle
        if (pressTail)
        {
            float k = 1f - sincePress / tailFor;
            float size = 26f + 10f * (1f - k);
            Rect stamp = new Rect(Mathf.Clamp(mx, x + 12f, x + w - 12f) - size * 0.5f, y + h + 8f, size, size);   // under the bar: the lamps and the chain ticks sit above it
            if (failed) { if (!UIAssets.Draw(stamp, UIAssets.Icon("cross"), A(MenuStyle.HudRed, k))) Fill(new Rect(stamp.x, stamp.center.y - 2f, size, 4f), A(MenuStyle.HudRed, k)); }
            else if (trick.PressedPerfect) { if (!UIAssets.Draw(stamp, UIAssets.Icon("star"), A(MenuStyle.HudGold, k))) Fill(stamp, A(MenuStyle.HudGold, k)); }
            else UIAssets.Draw(stamp, UIAssets.Icon("checkmark"), A(MenuStyle.Paper, 0.9f * k));
        }
        if (payTail)
        {
            float k = 1f - sinceLand / 0.45f;
            float size = 30f + 16f * (1f - k);
            UIAssets.Draw(new Rect(cx - size * 0.5f, y + h + 8f, size, size), UIAssets.Icon("star"), A(MenuStyle.HudGold, k));
        }
        GUI.color = Color.white;
    }
}
