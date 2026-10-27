using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/// <summary>
/// THE END OF CHAPTER 2 — THE LIFT (act2-underground): the Underground's mains go, and you ride out of it. The
/// Stationmaster is scrap and the brass gates are open; past them is the lift car at the foot of the shaft to the
/// street, and beside it the steam mains' relief valve. Walk up to the post and this plays, forty-eight seconds you
/// can skip after the first second and a half — ALL OF IT FROM YOUR OWN EYES. The camera never leaves them: the mouse
/// is yours the whole way, the view is only pulled toward what matters (PlayerLook.Steer, which the mouse can always
/// fight), and your feet are carried (the motor stands down; the feet go where the car goes). The timings and the
/// Steer calls are the cage ride's, unchanged; only the set is new.
///   THE VALVE     (0–4.5)     the last step up to the post, the eyes down on the lever as it slams; the gauges peg,
///                             the dome lights, the alarms come on through the whole terminus.
///   THE MAINS     (4.5–11)    the eyes pulled round to the platform behind you: the four steam mains along its wall
///                             glow, leak, throw sparks, and burst one after another; the floor jolts.
///   THE RUN       (11–15.5)   carried from the post into the lift car; the gate folds shut, the latch, the winding
///                             gear, and the car lifts away up the tiled shaft with you standing in it.
///   THE SHAFT     (15.5–22)   the lamps going by, the depth dial climbing; the eyes drawn down to the open-work floor
///                             under your feet: the bottom flashes, the shaft shakes, light and steam come up the
///                             shaft, then the main's column of steam comes up after you, catches the car and throws
///                             it — white-out.
///   THE STREET    (22–27.5)   the white clears on the car breaking the street at dawn into the terminus's forecourt,
///                             the House nine kilometres north with the whole city between: the lift head BURSTS under
///                             you in slow time — steam, paving, the canopy flung — the car is thrown clear in an arc
///                             and comes down hard in the forecourt, while the clock tower comes down across it.
///   THE FORECOURT (27.5–38)   down on your knees in the car, getting up; the gate falls open on the fires and the
///                             steam standing over the lift head; you step out onto the forecourt and look up.
///   THE HOUSE     (38–48)     the House over all of it, the sun behind it: the eyes climb its height to the top. That
///                             is the walk Chapter 3 makes.
/// The railway's incident log prints along the bottom; five times the vector has its own line — RUN, FASTER, HOLD ON,
/// PRESUME AGAIN, GO AND ASK THEM. The level ends when it ends: the report card comes down over the last frame, you
/// standing at the car's gate. Built by LevelSections.LiftLanding; the surface is EndingSet's.
/// </summary>
public class MineBlast : MonoBehaviour, IWeldDynamic
{
    /// The cage, the set's flung parts and the override post all move at the end: the weld leaves them alone
    public void DynamicParts(List<Transform> into)
    {
        if (cage != null) into.Add(cage);
        if (set != null)
        {
            if (set.headframe != null) into.Add(set.headframe);
            if (set.winch != null) into.Add(set.winch);
            if (set.stack != null) into.Add(set.stack);
            if (set.gantry != null) into.Add(set.gantry);
            if (set.pit != null) into.Add(set.pit.transform);
        }
        if (post != null)
        {
            if (post.lever != null) into.Add(post.lever);
            if (post.dome != null) into.Add(post.dome.transform);
        }
        if (alarms != null) foreach (var a in alarms) if (a != null && a.dome != null) into.Add(a.dome.transform);   // the alarm domes go red at the slam
    }
    public static bool Active { get; private set; }

    public MovementConfig config;
    public EndingProps.Post post;
    public Transform cage;
    public List<EndingProps.Alarm> alarms;
    public List<Light> furnaceLights;
    public EndingSet.Parts set;
    public float floorY, bayZ0, bayZ1, wellTop, furnaceZ, arenaZ0;
    public Vector3 wellCentre;

    const float Length = 48f;
    const float SlamAt = 1.2f, FurnacesAt = 4.5f, RunAt = 11f, LiftAt = 13.4f, ShaftAt = 15.5f, ShaftBlastAt = 18f, CatchAt = 20.8f, WhiteAt = 21.2f;
    const float SurfaceAt = 22f, EruptAt = 22.3f, StackAt = 23f, FiresAt = 23.5f, YardAt = 27.5f, StandAt = 29f, DoorsAt = 30.2f, StepAt = 31.8f, HouseAt = 38f;
    const float StepUp = 0.8f;             // the last step up to the post
    const float Reach = 1.8f, SkipAfter = 1.5f;
    const float CageSpeed = 6f, CageBlastSpeed = 30f;
    const float FlightTime = 2.2f, Gravity = 22f;   // the arc out of the mouth, in the eruption's own slow seconds
    const float LandDrop = 0.35f;          // the crouch: the eyes come down this much on the landing
    const float SurfaceFar = 12500f;      // how far the camera draws on the surface: the house is 9 km out, the far city past 11
    static readonly float[] FurnaceBursts = { 8.5f, 9.6f, 10.4f };
    static readonly float[] LeakTimes = { 9.4f, 14f, 18.2f, 35.5f, 42.5f };
    static readonly float[] LeakHolds = { 1.6f, 1.4f, 2.2f, 2.4f, 3.6f };
    static readonly string[] LeakLines = { "RUN.", "FASTER.", "HOLD ON.", "PRESUME AGAIN.", "GO AND ASK THEM." };

    bool played, done;
    bool slammed, alarmsOn, running, shut, lifting, shaftBlown, caught, surfaced, erupted, stackFalling, stackDown, firesLit, flying, landed, opened, stepping;
    int bursts;
    float t;                              // the cutscene's own clock: unscaled seconds from Begin
    float white, flash, shake, stackAngle;
    float cageLift, doorShut, eyeDrop;
    Vector3 feet;                         // where you stand this frame: handed to the motor, and the look puts the eyes on it
    Vector3 walkFrom, walkTo;             // the carried walks: up to the post, into the cage, out of it
    Vector3 flyPos, flyVel, land;
    Quaternion cageRot;                   // the cage's own rotation, before the rattle
    float klaxonAt = -1f, rumbleAt = -1f, sparkAt = -1f, stepAt = -1f, windAt = -1f, ringAt = -1f, rubbleAt = -1f, crackleAt = -1f;
    PlayerLook look;
    PlayerMotor motor;
    GrappleAbility grapple;
    Camera cam;
    float farWas = -1f;                   // the camera's own far clip, put back if the ending is cut short
    AudioSource sfx;
    Transform cable;
    EndingProps.FireColumn shaftFire;
    EndingProps.SmokeColumn shaftDust;
    string lastCaption;
    float captionAt, captionW;
    GUIStyle hintStyle, capStyle;

    void OnDisable()
    {
        if (!Active) return;
        Active = false;
        if (cam != null && farWas > 0f) cam.farClipPlane = farWas;
        Time.timeScale = PauseMenu.IsPaused ? 0f : 1f;
        MusicDirector.Hush = false;
    }

    void Start()
    {
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.spatialBlend = 0f;
    }

    void Update()
    {
        if (done) return;
        if (!played)
        {
            // The walk-up: the floor's own boss must be scrap, no other cutscene running, and you within reach of the post
            var d = CombatDirector.Instance;
            if (d == null || d.motor == null || post == null) return;
            if (BossIntro.Active || BossOutro.Active || OpeningCutscene.Active || LevelResults.Active) return;
            if (Boss.Current != null && !Boss.Current.IsDead) return;
            Vector3 at = d.motor.CurrPosition;
            Vector3 flat = at - post.at; flat.y = 0f;
            if (flat.magnitude > Reach || Mathf.Abs(at.y - post.at.y) > 2f) return;
            Begin(d.motor);
            return;
        }
        if (!Active || PauseMenu.IsPaused) return;

        float dt = Time.unscaledDeltaTime;
        t += dt;
        Time.timeScale = TimeScaleAt(t);
        Step(dt, Time.deltaTime);

        if (t > SkipAfter)
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool skip = (kb != null && kb.anyKey.wasPressedThisFrame && !kb.escapeKey.wasPressedThisFrame) || (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (skip) { End(); return; }
        }
        if (Active && t >= Length) End();
    }

    /// Time runs true except through the eruption, which plays slow and comes back to speed as the rock comes down
    static float TimeScaleAt(float t)
    {
        if (t < EruptAt) return 1f;
        if (t < EruptAt + 1.4f) return 0.3f;
        return Mathf.Lerp(0.3f, 1f, Mathf.Clamp01((t - EruptAt - 1.4f) / 0.8f));
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
        if (grapple != null) { grapple.Cancel(); grapple.enabled = false; }
        // The look stays on: the eyes are yours. The motor stands down and the feet are carried (Carry, every frame)
        if (motor != null) { motor.Teleport(motor.CurrPosition); motor.enabled = false; feet = motor.CurrPosition; }
        if (LevelTimer.Instance != null) LevelTimer.Instance.Freeze(); // the run ends at the override; the ending costs no time
        MusicDirector.Hush = true;

        // The last step up to the post, from wherever you reached it
        walkFrom = feet;
        walkTo = post.at + post.facing * 1.15f;
        walkTo.y = feet.y;
        cageRot = cage != null ? cage.rotation : Quaternion.identity;
        // The cable from the cage's hook to the top of the well: it shortens as the cage climbs
        cable = EndingProps.Piece(transform, Vector3.zero, Vector3.one, LevelKit.Iron, "CageCable", PrimitiveType.Cylinder).transform;
        PlaceCable();
        Play("click", 0.5f, 1.1f);
    }

    // ------------------------------------------------------------ the beats

    void Step(float dt, float ds)
    {
        Override(dt);
        Alarms();
        if (t >= FurnacesAt) Meltdown(dt);
        if (t >= RunAt) Bay(dt);
        if (t >= ShaftAt) Shaft(dt);
        if (t >= SurfaceAt) Surface(dt, ds);
        Sounds();
        Carry(dt);
        white = Mathf.MoveTowards(white, 0f, dt * (t < SurfaceAt ? 0f : 1.8f)); // the white-out holds until the cut lands, then clears
        flash = Mathf.MoveTowards(flash, 0f, dt * 2.5f);
    }

    /// The step up and the slam: the lever over, the needles pegged, the dome red, the readout flipped — the eyes on the lever
    void Override(float dt)
    {
        if (post == null) return;
        Vector3 lever = post.at + Vector3.up * 1.35f;
        if (t < SlamAt)
        {
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / StepUp));
            feet = Vector3.Lerp(walkFrom, walkTo, k) + Vector3.up * (Mathf.Abs(Mathf.Sin(t * 9f)) * 0.03f * (1f - k));
            Eyes(lever, 0.6f);
            return;
        }
        float slam = Mathf.Clamp01((t - SlamAt) / 0.14f);
        if (post.lever != null) post.lever.localRotation = Quaternion.Euler(Mathf.Lerp(-38f, 44f, slam), 0f, 0f);
        if (post.needles != null)
            for (int i = 0; i < post.needles.Length; i++)
            {
                float swing = Mathf.Clamp01((t - SlamAt - 0.05f * i) / 0.5f);
                float over = Mathf.Sin(swing * Mathf.PI) * 18f;
                if (post.needles[i] != null) post.needles[i].localRotation = Quaternion.Euler(-72f, 0f, Mathf.Lerp(40f, -125f, swing) - over + Mathf.Sin(t * 37f + i) * 3f * swing);
            }
        if (!slammed)
        {
            slammed = true;
            if (post.dome != null) post.dome.sharedMaterial = EndingProps.AlarmRed;
            if (post.lamp != null) { post.lamp.enabled = true; post.lamp.intensity = 3f; }
            if (post.readout != null) { post.readout.text = "RELIEF   ·   DISABLED"; LevelKit.Fit(post.readout, post.readout.characterSize, 2.1f); }
            if (GameFeel.Instance != null) GameFeel.Instance.Sparks(post.at + Vector3.up * 1.4f, 14, false);
            Play("metal", 1f, 0.7f);
            Play("metal", 0.6f, 0.7f);
            Jolt(0.5f);
            klaxonAt = t + 0.3f;
        }
        if (post.lamp != null) post.lamp.intensity = 2f + 1.6f * Mathf.Max(0f, Mathf.Sin(t * 9f));
        if (t < RunAt)
        {
            // At the post with the lever hauled down: the eyes stay on it a moment, then go to the furnaces as they
            // start to go, through the doors behind you
            feet = walkTo;
            if (t < SlamAt + 0.8f) Eyes(lever, 0.8f);
            else if (t > FurnacesAt + 1.5f) Eyes(new Vector3(0f, floorY + 6f, furnaceZ), 0.35f);
        }
    }

    /// Every alarm in the plant, strobing together from the slam until the plant is gone
    void Alarms()
    {
        if (alarms == null) return;
        bool on = slammed && t < SurfaceAt && Mathf.Sin(t * 9.4f) > -0.2f;
        if (on == alarmsOn) return;
        alarmsOn = on;
        foreach (var a in alarms) EndingProps.SetAlarm(a, on ? 1f : 0f);
    }

    /// The casting floor going: the furnaces' light climbing and flickering, sparks and small bursts off the flues,
    /// then the three that go — each a fireball, a ring across the floor, the floor jolted under you
    void Meltdown(float dt)
    {
        float k = Mathf.Clamp01((t - FurnacesAt) / (EruptAt - FurnacesAt));
        if (furnaceLights != null)
            for (int i = 0; i < furnaceLights.Count; i++)
            {
                var l = furnaceLights[i];
                if (l == null) continue;
                l.enabled = true;
                l.intensity = Mathf.Lerp(0.6f, 7f, k) * (0.8f + 0.2f * Mathf.PerlinNoise(t * 11f, i * 3f));
            }
        var feel = GameFeel.Instance;
        if (t >= sparkAt && t > FurnacesAt + 1f && t < SurfaceAt)
        {
            sparkAt = t + Mathf.Lerp(0.6f, 0.18f, k);
            int i = Random.Range(0, 4);
            Vector3 mouth = new Vector3(-21f + i * 14f, floorY + 3f, furnaceZ - 3.2f);
            if (feel != null) feel.Sparks(mouth, 10 + (int)(20f * k), false);
            if (k > 0.25f) EndingProps.SteamBurst.Spawn(new Vector3(-21f + i * 14f, floorY + 9.6f, furnaceZ), 0.6f + 1f * k);   // the mains leaking at their elbows
            shake = Mathf.Max(shake, 0.15f + 0.3f * k);
        }
        while (bursts < FurnaceBursts.Length && t >= FurnaceBursts[bursts])
        {
            int i = bursts == 0 ? 0 : bursts == 1 ? 3 : 1;
            Vector3 at = new Vector3(-21f + i * 14f, floorY + 3.5f, furnaceZ - 1f);
            EndingProps.SteamBurst.Spawn(at, 4f + bursts * 1.2f);   // a main lets go: the column of steam, the ring across the floor
            ShockRing.Spawn(new Vector3(at.x, floorY + 0.05f, at.z), 1f, 16f + bursts * 4f, 0.6f, false);
            MuzzleFlash.Spawn(at, 7f);
            if (feel != null) feel.Sparks(at, 60, false);
            Play("boom", 0.9f + 0.1f * bursts, 0.8f - 0.08f * bursts);
            Play("whoosh_hard", 0.8f, 0.6f);
            Play("sub", 0.8f, 0.7f);
            Play("metal", 0.6f, 0.5f);
            Jolt(0.9f + 0.2f * bursts);
            bursts++;
        }
    }

    /// The bay: carried from the post into the cage, the doors shut, the latch, and the lift up the well
    void Bay(float dt)
    {
        if (t >= SurfaceAt) return; // past the cut the surface has the cage, and you
        if (!running)
        {
            running = true;
            walkFrom = feet;
            walkTo = cage != null ? cage.TransformPoint(EndingProps.Seat) : wellCentre + EndingProps.Seat;
        }
        if (t < LiftAt)
        {
            float k = Mathf.Clamp01((t - RunAt) / (LiftAt - 0.3f - RunAt));
            // Round to the front of the cage first, then straight in through its doors
            Vector3 via = new Vector3(walkTo.x, walkFrom.y, walkTo.z) + cageRot * Vector3.forward * 3f;
            Vector3 pos = k < 0.7f ? Vector3.Lerp(walkFrom, via, k / 0.7f) : Vector3.Lerp(via, walkTo, (k - 0.7f) / 0.3f);
            pos.y = Mathf.Lerp(walkFrom.y, walkTo.y, Mathf.Clamp01((k - 0.85f) / 0.15f)); // the step up onto the cage floor
            if (k < 1f)
            {
                // The run: the footfalls, the eyes on the cage ahead
                pos.y += Mathf.Abs(Mathf.Sin(t * 11f)) * 0.06f;
                Eyes(walkTo + Vector3.up * 1.5f, 0.6f);
                if (t >= stepAt) { stepAt = t + 0.2f; Play("step", 0.45f, 1.1f); }
            }
            else
            {
                // In the cage: the doors swing shut across the front, the eyes on them and the bay beyond
                if (!shut) { shut = true; Play("metal", 0.6f, 1.1f); }
                doorShut = Mathf.MoveTowards(doorShut, 1f, dt / 0.3f);
                EndingProps.CageDoors(cage, doorShut);
                Eyes(walkTo + Vector3.up * 1.4f + cageRot * Vector3.forward * 4f, 0.3f);
            }
            feet = pos;
            return;
        }
        if (!lifting)
        {
            lifting = true;
            doorShut = 1f;
            EndingProps.CageDoors(cage, 1f);
            Play("metal", 0.7f, 1.2f);
            Play("whoosh_hard", 0.6f, 0.7f);
            Jolt(0.3f);
        }
        cageLift += (caught ? CageBlastSpeed : CageSpeed) * dt;
        PlaceCage(wellCentre + Vector3.up * cageLift, cageRot);
        PlaceCable();
        if (t >= ringAt) { ringAt = t + 8f / CageSpeed; Play("click", 0.25f, 0.9f); }
        if (t < ShaftAt) Eyes(wellCentre + Vector3.up * (cageLift + 30f), 0.25f);   // the eyes drawn up the well as it starts; the Shaft takes them from there
    }

    /// The shaft: the bottom goes, light and dust come up the well, the fire comes up after the cage, and catches it
    void Shaft(float dt)
    {
        if (t >= SurfaceAt) return;
        var feel = GameFeel.Instance;
        Vector3 below = wellCentre + Vector3.up * (cageLift - 4f) + cageRot * Vector3.forward * 0.9f;   // the well under the grate, just past your feet
        if (!shaftBlown && t >= ShaftBlastAt)
        {
            shaftBlown = true;
            EndingProps.SteamBurst.Spawn(wellCentre + Vector3.up * 1.5f, 7f);
            MuzzleFlash.Spawn(wellCentre + Vector3.up * 2f, 26f);
            ShockRing.Spawn(wellCentre + Vector3.up * 0.1f, 1f, 30f, 0.7f, false);
            // The steam main's column climbs at the speed that meets the car at the catch, and on to the top after
            float rise = (CageSpeed * (CatchAt - LiftAt)) / (CatchAt - ShaftBlastAt);
            shaftFire = EndingProps.FireColumn.Spawn(wellCentre + Vector3.up * 0.5f, 6f, wellTop - floorY, (wellTop - floorY) / rise, 16, true);
            // The steam ahead of it, driven up the shaft faster than the car: past you and on to the cap
            shaftDust = EndingProps.SmokeColumn.Spawn(transform, wellCentre + Vector3.up * 1f, 2.5f, wellTop - floorY - 2f, 16, 0.4f, EndingProps.Steam);
            Play("boom", 1.2f, 0.5f);
            Play("whoosh_hard", 1f, 0.5f);
            Play("sub", 1f, 0.6f);
            Play("metal", 0.8f, 0.45f);
            Jolt(1.3f);
            flash = 0.6f;
            if (feel != null) feel.Sparks(wellCentre + Vector3.up * 3f, 80, false);
        }
        if (!shaftBlown) Eyes(below, 0.25f);   // before it: the eyes drift down to the grate
        else if (!caught)
        {
            // The shaft shakes harder as the fire climbs; the eyes pulled down hard to what the grate shows, then held there
            float k = Mathf.Clamp01((t - ShaftBlastAt) / (CatchAt - ShaftBlastAt));
            shake = Mathf.Max(shake, 0.35f + 0.4f * k);
            Eyes(below, t < ShaftBlastAt + 1.2f ? 0.8f : 0.45f);
        }
        if (!caught && t >= CatchAt)
        {
            caught = true;
            Vector3 floor = wellCentre + Vector3.up * cageLift;
            if (feel != null) { feel.Sparks(floor, 50, false); feel.Sparks(floor + Vector3.up * 1.5f, 30, true); }
            MuzzleFlash.Spawn(floor, 7f);
            Play("boom", 0.8f, 1.1f);
            Play("metal", 1f, 0.6f);
            Jolt(1.6f);
            flash = 0.8f;
            if (look != null) look.AddFovKick(8f);
        }
        if (caught) Eyes(new Vector3(wellCentre.x, wellTop, wellCentre.z), 0.5f);   // thrown: the eyes go up to the cap coming down at you
        if (caught && t >= WhiteAt && white <= 0f)
        {
            white = 1f;
            Play("sub", 1f, 0.5f);
        }
    }

    /// The surface: the dawn, the cage out of the mouth in the eruption's slow time, the arc and the landing, the stack,
    /// the fires; then the getting up, the doors, the step out, and the house
    void Surface(float dt, float ds)
    {
        if (!surfaced) Dawn();
        if (!erupted && t >= EruptAt) Erupt();
        Vector3 house = set.towerFoot + Vector3.up * (EndingSet.TowerH * 0.4f);

        if (!erupted)
        {
            // Up through the mouth: the last metres of the climb, the house ahead as the white clears
            cageLift += CageBlastSpeed * ds;
            PlaceCage(set.mouth + Vector3.up * cageLift, cageRot);
            Eyes(house, 1f);
        }
        else if (flying)
        {
            flyVel += Vector3.down * Gravity * ds;
            flyPos += flyVel * ds;
            if ((flyPos.y <= land.y + 0.02f && flyVel.y < 0f) || t >= YardAt - 0.2f) Land();   // down, or down in time for the yard
            else
            {
                // Thrown: the cage swings on nothing, the yard and the house wheel past the bars
                float ut = Time.unscaledTime;
                var sway = Quaternion.Euler((Mathf.PerlinNoise(ut * 1.1f, 0.2f) - 0.5f) * 20f, (Mathf.PerlinNoise(0.4f, ut * 0.9f) - 0.5f) * 14f, (Mathf.PerlinNoise(ut * 1.3f, 7f) - 0.5f) * 20f);
                PlaceCage(flyPos, cageRot * sway);
                Eyes(house, 0.5f);
            }
        }
        if (landed)
        {
            // Down: on your knees in the cage, then up; the doors fall open; the step out onto the yard
            PlaceCage(land, cageRot);
            float up = Mathf.Clamp01((t - StandAt) / 1.4f);
            eyeDrop = LandDrop * (1f - Mathf.SmoothStep(0f, 1f, up));
            if (!opened && t >= DoorsAt) { opened = true; Play("metal", 0.9f, 0.5f); Play("metal", 0.5f, 0.8f); }
            if (opened && doorShut > 0f) { doorShut = Mathf.MoveTowards(doorShut, 0f, dt / 0.5f); EndingProps.CageDoors(cage, doorShut); }
            if (!stepping && t >= StepAt) { stepping = true; walkFrom = feet; walkTo = DoorStep(); }
            if (stepping)
            {
                float k = Mathf.Clamp01((t - StepAt) / 1.8f);
                Vector3 pos = Vector3.Lerp(walkFrom, walkTo, k);
                pos.y = Mathf.Lerp(walkFrom.y, walkTo.y, Mathf.Clamp01((k - 0.35f) / 0.3f)); // the step down off the grate
                if (k < 1f)
                {
                    pos.y += Mathf.Abs(Mathf.Sin(t * 7f)) * 0.04f;
                    if (t >= stepAt) { stepAt = t + 0.45f; Play("step", 0.4f, 0.9f); }
                }
                feet = pos;
            }
            if (t >= HouseAt) Eyes(set.towerFoot + Vector3.up * Mathf.Lerp(480f, 640f, Mathf.SmoothStep(0f, 1f, (t - HouseAt) / (Length - HouseAt))), 0.3f);   // up its height to the top
            else if (stepping) Eyes(set.towerFoot + Vector3.up * (EndingSet.TowerH * 0.35f), 0.35f);
            else if (up > 0.3f) Eyes(set.mouth + Vector3.up * 12f, 0.3f);   // the smoke standing over the mouth
        }

        // The near stack comes down across the yard, and hits
        if (!stackFalling && t >= StackAt)
        {
            stackFalling = true;
            Play("metal", 0.9f, 0.4f);
            Play("metal", 0.5f, 0.6f);
        }
        if (stackFalling && !stackDown && set != null && set.stack != null)
        {
            float k = Mathf.Clamp01((t - StackAt) / 2.6f);
            stackAngle = 92f * k * k;
            set.stack.localRotation = Quaternion.Euler(0f, 0f, stackAngle);
            if (k >= 1f)
            {
                stackDown = true;
                Vector3 top = set.stack.position + set.stack.up * 28f;
                Vector3 hit = new Vector3(top.x, set.origin.y, top.z);
                ShockRing.Spawn(hit + Vector3.up * 0.05f, 2f, 16f, 0.5f, false);
                LevelKit.Rubble(set.root, hit, 5f, 12);
                EndingProps.Throw(hit + Vector3.up * 1f, 12, 8f, 6f);
                EndingProps.Flames.Spawn(set.root, hit + new Vector3(2f, 0f, 1f), 1.8f);
                Play("boom", 1f, 0.6f);
                Play("stone", 1f, 0.5f);
                Jolt(1.2f);
            }
        }
        if (!firesLit && t >= FiresAt) LightFires();
    }

    /// The cage comes down in the yard, canted, and you with it: on your knees on the grate
    void Land()
    {
        flying = false;
        landed = true;
        cageRot = Quaternion.Euler(0f, 0f, 3f);   // down on its feet, not quite square
        PlaceCage(land, cageRot);
        eyeDrop = LandDrop;
        ShockRing.Spawn(land + Vector3.up * 0.05f, 0.3f, 3.5f, 0.35f, false);
        if (GameFeel.Instance != null) GameFeel.Instance.Sparks(land + Vector3.up * 0.4f, 16, false);
        Play("punch", 0.8f, 0.7f);
        Play("stone", 0.5f, 0.8f);
        Play("metal", 0.7f, 0.5f);
        Jolt(1.2f);
    }

    /// The dawn: the sky, the sun low in the north behind the house, the haze, the grade — and the cut itself: the cage
    /// and you out of the well and into the mouth of Shaft 1, ten metres down and rising
    void Dawn()
    {
        surfaced = true;
        WorldLook.ApplySky(WorldLook.Dawn);
        // The surface is kilometres deep, the house three out and the far city past five, but the scene's camera only
        // draws a thousand metres. Out here it draws six. The set is built eight kilometres from the level (EndingSet)
        // so the level stays out of that reach, and the haze takes the edge of the world.
        if (cam != null && farWas < 0f) { farWas = cam.farClipPlane; cam.farClipPlane = SurfaceFar; }
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 120f;
        RenderSettings.fogEndDistance = 20000f;   // the haze has nine kilometres to carry, and the house has to come through it
        RenderSettings.fogColor = WorldLook.Dawn.ground;   // the redder horizon: the dawn's own, not the level's
        // The light and the grade are Chapter 3's Surface preset — DARK DECO's sepia day: crushed blacks, warm
        // highlights, a little desaturation, the vignette and grain — so the level opens in this cutscene's last frame.
        // Only the sun (lower, in WorldLook.Dawn) and the horizon above differ.
        var day = WorldLook.Surface;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = day.sky;
        RenderSettings.ambientEquatorColor = day.equator;
        RenderSettings.ambientGroundColor = day.ground;
        var go = new GameObject("Look Dawn");
        go.transform.SetParent(transform, false);
        var vol = go.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 20f;
        vol.weight = 1f;
        vol.sharedProfile = WorldLook.Profile(day);
        // The cable, the fire and the dust stay in the well; the cage comes with you, its doors to the north and the house
        if (cable != null) cable.gameObject.SetActive(false);
        if (shaftFire != null) Destroy(shaftFire.gameObject);
        if (shaftDust != null) Destroy(shaftDust.gameObject);
        land = set.land;
        cageLift = -10f;
        cageRot = Quaternion.identity;
        PlaceCage(set.mouth + Vector3.up * cageLift, cageRot);
        if (alarms != null) foreach (var a in alarms) EndingProps.SetAlarm(a, 0f);
        alarmsOn = false;
    }

    /// The mouth goes: fire and rock out of it, the headframe and the winch flung — and the cage, with you in it,
    /// thrown clear on the arc that comes down in the yard
    void Erupt()
    {
        erupted = true;
        Vector3 M = set.mouth;
        var feel = GameFeel.Instance;
        EndingProps.SteamBurst.Spawn(M + Vector3.up * 3f, 20f);
        EndingProps.SteamBurst.Spawn(M + Vector3.up * 16f, 12f);
        MuzzleFlash.Spawn(M + Vector3.up * 5f, 44f);
        ShockRing.Spawn(M + Vector3.up * 0.1f, 4f, 80f, 1.2f, false);
        ShockRing.Spawn(M + Vector3.up * 0.1f, 2f, 44f, 0.7f, false);
        EndingProps.FireColumn.Spawn(M, 12f, 90f, 2.4f, 16, true);
        EndingProps.Throw(M + Vector3.up * 1f, 48, 34f, 14f);
        EndingProps.Fling(set.headframe, M, 22f);
        EndingProps.Fling(set.winch, M, 14f);
        Scorch(M);
        EndingProps.SmokeColumn.Spawn(set.root, M + Vector3.up * 2f, 9f, 110f, 18, 0.09f, EndingProps.Steam);
        LevelKit.Embers(set.root, M + Vector3.up * 8f);
        // The cage, thrown: the arc from here to the yard's spot, in the eruption's slow seconds
        flying = true;
        flyPos = cage != null ? cage.position : M + Vector3.up * 2f;
        flyVel = (land - flyPos) / FlightTime + Vector3.up * (0.5f * Gravity * FlightTime);
        if (feel != null) { feel.Sparks(M + Vector3.up * 6f, 160, false); feel.Sparks(M + Vector3.up * 10f, 60, true); }
        Play("boom", 1.2f, 0.5f);
        Play("boom", 1f, 0.7f);
        Play("sub", 1.2f, 0.5f);
        Play("stone", 0.9f, 0.6f);
        Play("metal", 0.9f, 0.5f);
        Jolt(2.2f);
        flash = 0.5f;
        if (look != null) look.AddFovKick(10f);
        rubbleAt = t + 0.8f;
        crackleAt = t + 3f;
    }

    /// Fires in the yard where the rock landed, embers over them
    void LightFires()
    {
        firesLit = true;
        Vector3 M = set.mouth;
        EndingProps.Flames.Spawn(set.root, M + new Vector3(-10f, 0f, -9f), 2.4f);
        EndingProps.Flames.Spawn(set.root, M + new Vector3(11f, 0f, -6f), 2f);
        EndingProps.Flames.Spawn(set.root, M + new Vector3(-13f, 0f, 9f), 1.8f);
        EndingProps.Flames.Spawn(set.root, M + new Vector3(9f, 0f, 12f), 2.2f);
        EndingProps.Flames.Spawn(set.root, set.origin + new Vector3(-18f, 0f, -20f), 1.5f);
    }

    /// The ruin, all at once: for a skip before the mouth has gone
    void Aftermath()
    {
        erupted = true;
        Vector3 M = set.mouth;
        if (set.stack != null) set.stack.localRotation = Quaternion.Euler(0f, 0f, 92f);
        if (set.headframe != null) set.headframe.gameObject.SetActive(false);
        if (set.winch != null) set.winch.gameObject.SetActive(false);
        Scorch(M);
        EndingProps.SmokeColumn.Spawn(set.root, M + Vector3.up * 2f, 9f, 110f, 18, 0.09f, EndingProps.Steam);
        LevelKit.Embers(set.root, M + Vector3.up * 8f);
        LevelKit.Rubble(set.root, M + new Vector3(-12f, 0f, -10f), 6f, 12);
        LevelKit.Rubble(set.root, M + new Vector3(13f, 0f, -7f), 5f, 10);
        LevelKit.Rubble(set.root, M + new Vector3(-4f, 0f, 14f), 7f, 12);
        LevelKit.Rubble(set.root, M + new Vector3(14f, 0f, 11f), 5f, 9);
        LevelKit.Rubble(set.root, M + new Vector3(-2f, 0f, 4f), 6f, 12);
        if (!firesLit) LightFires();
        stackDown = true;
    }

    /// The burn round the mouth: four sheets of black, the hole left open in the middle
    void Scorch(Vector3 M)
    {
        float h = EndingSet.MouthHalf;
        LevelKit.Prim(set.root, M + new Vector3(-h - 5f, 0.03f, 0f), new Vector3(10f, 0.04f, h * 2f + 20f), EndingProps.Scorch, "Scorch");
        LevelKit.Prim(set.root, M + new Vector3(h + 5f, 0.03f, 0f), new Vector3(10f, 0.04f, h * 2f + 20f), EndingProps.Scorch, "Scorch");
        LevelKit.Prim(set.root, M + new Vector3(0f, 0.03f, -h - 5f), new Vector3(h * 2f, 0.04f, 10f), EndingProps.Scorch, "Scorch");
        LevelKit.Prim(set.root, M + new Vector3(0f, 0.03f, h + 5f), new Vector3(h * 2f, 0.04f, 10f), EndingProps.Scorch, "Scorch");
    }

    void PlaceCable()
    {
        if (cable == null || cage == null) return;
        Vector3 a = cage.position + Vector3.up * 3.65f, b = new Vector3(cage.position.x, wellTop - 0.6f, cage.position.z);
        float len = Mathf.Max(0.1f, b.y - a.y);
        cable.position = (a + b) * 0.5f;
        cable.localScale = new Vector3(0.1f, len * 0.5f, 0.1f);
    }

    // ------------------------------------------------------------ you

    /// The cage where it is this frame, with the rattle the shake puts into it — and your feet on its grate
    void PlaceCage(Vector3 pos, Quaternion rot)
    {
        if (cage == null) { feet = pos + EndingProps.Seat; return; }
        float r = Mathf.Min(1f, shake);
        if (r > 0f)
        {
            float ut = Time.unscaledTime;
            pos += new Vector3(Mathf.PerlinNoise(ut * 31f, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, ut * 37f) - 0.5f, Mathf.PerlinNoise(ut * 29f, 5f) - 0.5f) * (0.08f * r);
            rot *= Quaternion.Euler((Mathf.PerlinNoise(ut * 13f, 2f) - 0.5f) * 3f * r, 0f, (Mathf.PerlinNoise(9f, ut * 11f) - 0.5f) * 3f * r);
        }
        cage.SetPositionAndRotation(pos, rot);
        feet = cage.TransformPoint(EndingProps.Seat);
    }

    /// Where you stand at the end: on the yard at the cage's open doors, the house ahead
    Vector3 DoorStep() => new Vector3(land.x, land.y, land.z + 2.75f);

    /// The feet to the motor and the shake to the look, every frame. The eyes are the look's own (PlayerLook.LateUpdate
    /// puts them on the feet), the mouse is yours; Eyes is the pull on it
    void Carry(float dt)
    {
        if (motor != null) motor.Warp(feet + Vector3.down * eyeDrop);
        shake = Mathf.MoveTowards(shake, 0f, dt * 1.2f);
        if (look != null && shake > 0f) look.AddShake(Mathf.Min(1.5f, shake * 0.6f) * (1f - Mathf.Exp(-9f * dt))); // held at the rumble's level against the look's own decay
    }

    /// The pull on the view toward what matters this frame; the mouse can always fight it
    void Eyes(Vector3 at, float strength)
    {
        if (look != null) look.Steer(at, strength, 0.25f);
    }

    /// A jolt through the cage and the view: the rumble's level, and a punch into the look on top of it
    void Jolt(float amount)
    {
        shake = Mathf.Max(shake, amount);
        if (look != null) look.AddShake(amount * 0.6f);
    }

    // ------------------------------------------------------------ the sounds that run

    void Sounds()
    {
        if (slammed && t < SurfaceAt && t >= klaxonAt)
        {
            klaxonAt = t + 1.25f;
            Play("bell", 0.45f, 0.5f);   // the klaxon is a bell now
        }
        if (t >= FurnacesAt && t < SurfaceAt && t >= rumbleAt)
        {
            rumbleAt = t + 0.7f;
            Play("sub", Mathf.Lerp(0.3f, 0.8f, Mathf.Clamp01((t - FurnacesAt) / 14f)), 0.9f);
        }
        if (t >= ShaftAt && t < SurfaceAt && t >= windAt)
        {
            windAt = t + 0.6f;
            Play("whoosh_soft", 0.35f, 0.7f);
        }
        if (erupted && t < YardAt + 1f && t >= rubbleAt)
        {
            rubbleAt = t + Random.Range(0.25f, 0.55f);
            Play("stone", Random.Range(0.35f, 0.65f), Random.Range(0.6f, 1.1f));
        }
        if (erupted && t >= crackleAt)
        {
            crackleAt = t + Random.Range(0.35f, 0.7f);
            Play("wood", 0.22f, Random.Range(0.55f, 0.75f));
        }
    }

    void Play(string bucket, float vol, float pitch)
    {
        var clip = GameFeel.Clip(bucket);
        if (clip == null || sfx == null) return;
        sfx.pitch = pitch;
        sfx.PlayOneShot(clip, vol * GameSettings.SfxVolume);
    }

    /// The end: the ruin, the cage down in the yard with its doors open, you at its door with the house over you — and
    /// the report card comes down on that frame. The motor is yours again where you stand
    void End()
    {
        if (!Active) return;
        Active = false;
        done = true;
        Time.timeScale = 1f;
        white = 0f;
        flash = 0f;
        if (!surfaced) Dawn();
        if (!erupted) Aftermath();
        if (!stackDown && set != null && set.stack != null) { set.stack.localRotation = Quaternion.Euler(0f, 0f, 92f); stackDown = true; }
        flying = false;
        shake = 0f;
        eyeDrop = 0f;
        cageRot = Quaternion.Euler(0f, 0f, 3f);
        PlaceCage(land, cageRot);
        EndingProps.CageDoors(cage, 0f);
        feet = DoorStep();
        if (motor != null) { motor.Teleport(feet); motor.enabled = true; }
        if (look != null) { look.enabled = true; look.Steer(set.towerFoot + Vector3.up * 640f, 1f, 1.5f); } // the last frame: the house
        if (grapple != null) grapple.enabled = true;
        MusicDirector.Hush = false;
        if (LevelTimer.Instance != null) LevelTimer.Instance.SaveBest();
        if (GameFeel.Instance != null) GameFeel.Instance.PlayFinish();
        if (LevelResults.Instance != null) LevelResults.Instance.Finish("MainMenu");
        else UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    // ------------------------------------------------------------ the frame

    static void Fill(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
    }

    static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);

    /// What the incident log says at this second (literals only: nothing is built per frame)
    static string CaptionAt(float at)
    {
        if (at < 0.6f) return null;
        if (at < 2.6f) return "STEAM MAINS · RELIEF · MANUAL OVERRIDE.";
        if (at < 4.6f) return "ALL MAINS · RELIEF DISABLED.";
        if (at < 5f) return null;
        if (at < 7.4f) return "MAIN 1 · 140 % OF RATED PRESSURE.";
        if (at < 9.4f) return "MAIN 2 · 170 %.";
        if (at < 11.4f) return null;
        if (at < 14f) return "GUEST 0041 · LEAVING THE PLATFORM.";
        if (at < 16f) return null;
        if (at < 18.2f) return "LIFT 3 · ASCENDING.";
        if (at < 21f) return null;
        if (at < 22.3f) return "TERMINUS · LOSS OF CONTAINMENT.";
        if (at < 27.5f) return null;
        if (at < 30.5f) return "LIFT 3 · STREET LEVEL · STRUCTURAL FAILURE.";
        if (at < 33f) return "HALCYON METRO · WRITTEN OFF.";
        if (at < 35.5f) return "GUEST 0041 · PRESUMED LOST.";
        if (at < 39f) return null;
        if (at < 42.5f) return "THE HOUSE · 9.1 KM.";
        return null;
    }

    void OnGUI()
    {
        if (!Active || PauseMenu.IsPaused) return;
        MenuStyle.EnsureStyles();
        if (capStyle == null)
        {
            hintStyle = new GUIStyle(MenuStyle.Sub) { alignment = TextAnchor.LowerRight };
            capStyle = new GUIStyle(MenuStyle.Tagline) { fontSize = 24, alignment = TextAnchor.MiddleLeft };
            hintStyle.normal.textColor = capStyle.normal.textColor = Color.white;
        }
        UIScale.Begin();
        GUI.depth = -20;
        float w = UIScale.W, h = UIScale.H;

        float bar = h * 0.13f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.35f));
        Fill(new Rect(0f, 0f, w, bar), Color.black);
        Fill(new Rect(0f, h - bar, w, bar), Color.black);
        Color hair = A(MenuStyle.Brass, 0.5f);   // a thin brass rule along each inner edge
        Fill(new Rect(0f, bar - 1f, w, 1f), hair);
        Fill(new Rect(0f, h - bar, w, 1f), hair);
        DrawCaptions(w, h, bar);
        if (flash > 0f) Fill(new Rect(0f, 0f, w, h), new Color(1f, 0.85f, 0.6f, flash * 0.7f));   // the blast's light up the well, on the eyes
        if (white > 0f) Fill(new Rect(0f, 0f, w, h), new Color(1f, 1f, 1f, white));
        if (t > SkipAfter)
        {
            GUI.color = MenuStyle.Sick;
            UIScale.Label(new Rect(w - 340f, h * 0.87f - 34f, 300f, 24f), "any key · skip", hintStyle);
        }
        GUI.color = Color.white;
        UIScale.End();
    }

    /// The incident log's line, printed onto the lower bar as it is read — and five times the vector has its own
    void DrawCaptions(float w, float h, float bar)
    {
        GUI.color = A(MenuStyle.Sick, 0.9f);
        UIScale.Label(new Rect(w * 0.06f, bar - 30f, 560f, 22f), "RAILWAY INCIDENT LOG   ·   TERMINUS   ·   SERVICE 0041", MenuStyle.Doc);
        MenuStyle.ChevronRule(new Rect(w * 0.06f + 400f, bar - 24f, 90f, 8f), A(MenuStyle.Gold, 0.7f));

        float leakAt = -1f;
        string leak = null;
        for (int i = 0; i < LeakTimes.Length; i++)
            if (t >= LeakTimes[i] && t < LeakTimes[i] + LeakHolds[i]) { leakAt = LeakTimes[i]; leak = LeakLines[i]; break; }
        string cap = leak ?? CaptionAt(t);
        // The line changes on the Repaint pass only, so Layout and Repaint never disagree about what is printed
        if (!ReferenceEquals(cap, lastCaption) && Event.current.type == EventType.Repaint)
        {
            lastCaption = cap;
            captionAt = t;
            captionW = cap != null ? MenuStyle.Measure(capStyle, cap).x : 0f;
            if (cap != null) Play(leak != null ? "warn" : "tick", leak != null ? 0.5f : 0.3f, leak != null ? 0.8f : 1.3f);
        }
        if (cap == null || !ReferenceEquals(cap, lastCaption)) { GUI.color = Color.white; return; }

        float y = h - bar + 18f;
        if (leak != null)
        {
            float since = t - leakAt;
            int state = since < 0.18f ? MenuStyle.RotState((int)(since / 0.03f)) : 1;
            Rect panel = new Rect(w * 0.5f - captionW * 0.5f - 18f + Mathf.Sin(t * 131f) * 3f, y - 4f, captionW + 36f, 38f);
            Fill(panel, state == 0 ? A(MenuStyle.Rot, 0.9f) : A(MenuStyle.Ink, 0.9f));
            GUI.color = state == 0 ? MenuStyle.Ink : MenuStyle.Rot;
            UIScale.Label(new Rect(panel.x + 18f, y, captionW + 4f, 30f), leak, capStyle);
            GUI.color = Color.white;
            return;
        }
        // The log's line prints itself left to right on an ink strip that grows with it; no GUI group (UIScale.Label
        // rescales the matrix, and a group would seat it wrong at any window size but the reference one)
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
}
