using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WARD 7 — where the first level starts, and where you were made. A white
/// clinical room in the company's house: your pod, shut, with you in it, and
/// across the aisle, turned to face it, the family's three — the record's tag
/// over each, the rot through them and out across the floor — the observation
/// desks and their monitors behind the family, the board over the way out
/// reading TRIAL 07 · DAY 212 until the release — and between the desks and
/// the way out, two of the company's dummies, with the door sealed until the
/// FIRST PUNCH THAT CONNECTS (the one pause in the game, course-grammar): you
/// wake with nothing in your hands, and the first lesson is that the hands are
/// enough (the kit waits on THE RACK, after the lobby). The
/// opening plays here behind your own eyes (OpeningCutscene): you wake looking
/// through your own glass at them. The MENU door is behind your pod; the spawn
/// is inside your pod, and nothing after this room may move it.
/// </summary>
public static partial class LevelSections
{
    class PodParts { public TextMesh tag; public GameObject glass; public Light light; public PlayerAvatar figure; }

    public static Cursor Ward(Transform root, MovementConfig config, Cursor c, float len = 40f, float halfW = 17f, float height = 9f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY, top = y + height;
        var door = Door(y);
        Seal(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, top, z1), NoSouth, door);
        LevelKit.DoorFrame(root, door, z1);

        // THE STAGING. Yours is where the player spawns, its door toward the way out; the family's three stand across
        // the aisle in a shallow arc, each turned to face your glass, so the first thing you see when you wake is
        // them — the father on the left, the mother square in front, the sibling on the right. 0038..0040, then yours
        float podZ = z0 + 12f, familyZ = podZ + 7.5f;
        var pods = new Vector3[4];
        pods[0] = new Vector3(3.2f, y, familyZ);
        pods[1] = new Vector3(9f, y, familyZ + 0.7f);
        pods[2] = new Vector3(14.8f, y, familyZ);
        pods[3] = new Vector3(9f, y, podZ);
        var tags = new TextMesh[4];
        var family = new PlayerAvatar[3];
        var familyLights = new Light[3];
        PodParts yours = null;
        BreakableGlass podDoor = null;
        for (int i = 0; i < 4; i++)
        {
            bool mine = i == 3;
            Vector3 toYours = pods[3] - pods[i];
            float yaw = mine ? 0f : Mathf.Atan2(toYours.x, toYours.z) * Mathf.Rad2Deg; // the family face your pod; yours faces the way out
            var parts = Pod(root, pods[i], yaw, mine ? "0041  ·  CHECKING IN" : i == 0 ? "0038  ·  ACCOMMODATED" : i == 1 ? "0039  ·  ACCOMMODATED" : "0040  ·  ACCOMMODATED", !mine, i);
            tags[i] = parts.tag;
            if (!mine) { family[i] = parts.figure; familyLights[i] = parts.light; }
            if (mine)
            {
                yours = parts;
                podDoor = LevelKit.Glass(root, pods[i] + new Vector3(0f, 1.5f, 1.05f), new Vector3(1.6f, 2.6f, 0.12f), config);
            }
        }
        // The rot, out of the three: over the partition to the desks that watched them, up the near wall — and one
        // that came across the aisle for the fourth and stopped at your rim
        LevelKit.Tendril(root, pods[0] + new Vector3(-0.6f, 0.1f, 0.9f), new Vector3(-2f, y + 0.1f, z0 + 22.1f), 0.28f, 7);
        LevelKit.Tendril(root, new Vector3(-2f, y + 0.1f, z0 + 22.1f), new Vector3(-2.6f, y + 1.45f, z0 + 22.4f), 0.22f, 4);
        LevelKit.Tendril(root, new Vector3(-2.6f, y + 1.45f, z0 + 22.4f), new Vector3(-5.6f, y + 0.82f, z0 + 24f), 0.2f, 6);
        LevelKit.Tendril(root, pods[1] + new Vector3(0.5f, 0.1f, 1f), new Vector3(10.2f, y + 0.1f, z0 + 23.2f), 0.24f, 6);
        LevelKit.Tendril(root, new Vector3(10.2f, y + 0.1f, z0 + 23.2f), new Vector3(6.4f, y + 0.82f, z0 + 24.1f), 0.2f, 6);
        LevelKit.Tendril(root, pods[2] + new Vector3(0.7f, 0.1f, 0.6f), new Vector3(halfW - 0.4f, y + 0.2f, z0 + 21.5f), 0.26f, 6);
        LevelKit.Tendril(root, new Vector3(halfW - 0.4f, y + 0.2f, z0 + 21.5f), new Vector3(halfW - 0.3f, y + height * 0.7f, z0 + 20.5f), 0.22f, 8);
        LevelKit.Tendril(root, pods[1] + new Vector3(-0.7f, 0.1f, -1f), pods[3] + new Vector3(-0.9f, 0.1f, 0.95f), 0.14f, 6); // it reached for the fourth

        // The floor: hazard tape along both edges of the aisle, chevrons from your door through the gap between the
        // father and the mother, the runner from there to the way out
        LevelKit.Stripes(root, new Vector3(9f, y + 0.04f, podZ + 2.2f), new Vector3(16f, 0.06f, 0.2f));
        LevelKit.Stripes(root, new Vector3(9f, y + 0.04f, familyZ - 2.2f), new Vector3(16f, 0.06f, 0.2f));
        LevelKit.Chevrons(root, new Vector3(8.6f, y, podZ + 2.9f), new Vector3(-2.9f, 0f, 5f), 3);
        LevelKit.Runner(root, new Vector3(0f, y, z0 + 29.5f), new Vector2(3.4f, 17f));

        // The observation desks, their monitors, the racks that hold the record; the frosted wall behind your pod
        for (int i = -1; i <= 1; i++) LevelKit.Workstation(root, new Vector3(i * 6f, y, z0 + 24f), 180f, i + 1);
        LevelKit.Partition(root, new Vector3(0f, y, z0 + 22.4f), 0f, 19f, 1.3f);
        LevelKit.MonitorWall(root, new Vector3(-halfW + 0.1f, y + 4f, z0 + 24f), Vector3.right, 6f, 2.6f);
        LevelKit.MonitorWall(root, new Vector3(halfW - 0.1f, y + 4f, z0 + 24f), Vector3.left, 6f, 2.6f);
        LevelKit.ServerRack(root, new Vector3(halfW - 1.4f, y, z1 - 3f), -90f);
        LevelKit.ServerRack(root, new Vector3(halfW - 1.4f, y, z1 - 5.2f), -90f);
        LevelKit.ServerRack(root, new Vector3(-halfW + 1.4f, y, z1 - 3f), 90f);
        LevelKit.CurtainWall(root, new Vector3(0f, y + 4.5f, z0 + 0.1f), Vector3.forward, 20f, 7f);
        LevelKit.Bench(root, new Vector3(-halfW + 3f, y, z0 + 31f), 90f);
        LevelKit.WaterCooler(root, new Vector3(halfW - 2.2f, y, z0 + 31f));

        // Overhead: skylights over your pod, the family's row and the desks, the company's banners, the mark over the way out
        LevelKit.Skylight(root, new Vector3(9f, top, podZ), new Vector2(4f, 3f));
        LevelKit.Skylight(root, new Vector3(9f, top, familyZ), new Vector2(14f, 3f));
        LevelKit.Skylight(root, new Vector3(0f, top, z0 + 27f), new Vector2(8f, 3f));
        LevelKit.HoloBanner(root, new Vector3(-8f, top - 0.5f, z0 + 31f), 3f, 2.6f, top, 3);
        LevelKit.HoloBanner(root, new Vector3(8f, top - 0.5f, z0 + 31f), 3f, 2.6f, top, 3);
        LevelKit.Emblem(root, new Vector3(0f, y + 7.9f, z1 - 0.2f), Quaternion.LookRotation(Vector3.back), 1.4f, LevelKit.OrbMat, LevelKit.Graphite);
        TextMesh board = LevelKit.Display(root, new Vector3(0f, y + 6.2f, z1 - 0.1f), Vector3.back, 7f, 1.5f, "WARD 7   ·   DAY 212");
        LevelKit.WallLight(root, new Vector3(-halfW + 0.1f, y + 3.5f, z0 + 8f), Vector3.right);
        LevelKit.WallLight(root, new Vector3(halfW - 0.1f, y + 3.5f, z0 + 8f), Vector3.left);
        LevelKit.WallLight(root, new Vector3(-halfW + 0.1f, y + 3.5f, z1 - 8f), Vector3.right);
        LevelKit.WallLight(root, new Vector3(halfW - 0.1f, y + 3.5f, z1 - 8f), Vector3.left);
        LevelKit.Poster(root, new Vector3(-halfW + 0.1f, y + 4.6f, z0 + 33f), Vector3.right, 2f, 2.8f, 1);
        LevelKit.Poster(root, new Vector3(halfW - 0.1f, y + 4.6f, z0 + 33f), Vector3.left, 2f, 2.8f, 2);
        LevelKit.Stencil(root, new Vector3(-halfW + 0.1f, y + 2.2f, z0 + 16f), Vector3.right, "SANATORIUM  ·  WARD 7", 0.09f, 8f);
        LevelKit.Stencil(root, new Vector3(halfW - 0.1f, y + 2.2f, z0 + 12f), Vector3.left, "STAFF ONLY  ·  NO ADMITTANCE", 0.09f, 6f);
        // The ward's lamps: amber, and dark until the release (the cutscene lights them)
        var lamps = new Light[2];
        for (int s = 0; s < 2; s++)
        {
            float x = s == 0 ? -halfW + 1f : halfW - 1f;
            lamps[s] = LevelKit.Light(root, new Vector3(x, y + height - 1.2f, z1 - 6f), 16f, 2.2f);
            lamps[s].color = new Color(1f, 0.62f, 0.15f);
            lamps[s].enabled = false;
        }

        // THE LESSON (2026-10-26, no units: the trick is the lesson): a pad before the way out throws you six metres
        // up, the ring closes at the top, the left button at its beat throws a flip, and the way out is sealed until
        // a flip is LANDED. You wake with nothing in your hands, so the first thing the run does is fly
        LevelKit.JumpPad(root, new Vector3(0f, y, z1 - 14f), 0f, 17.4f, 3f);   // 17.4 up under gravity 25: six metres, 1.4 s of air
        // THE DOOR IS OPEN (2026-10-27, user: "I cant even walk through the door in the first level"): no seal any more; the pad and the
        // sign teach the trick to whoever wants it, and the way out never waits
        var sealSign = new StateSign(root, new Vector3(0f, y + 5.3f, z1 - 0.2f), Vector3.back, 5.2f, 1.2f, "TRICK");
        sealSign.Set("OPEN", MenuStyle.Gold);
        var lessonHost = new GameObject("WardLesson");
        lessonHost.transform.SetParent(root);
        var lesson = lessonHost.AddComponent<WardLesson>();
        lesson.sign = sealSign;
        Dress(root, c, halfW, y, height, z0, z1);

        // The spawn is your pod, and nothing after this room moves it; the MENU door is behind it
        Last.spawn = pods[3] + Vector3.up;
        Last.menuPortal = new Vector3(0f, y + 2f, z0 + 1.7f);
        Last.spawnLocked = true;

        // The opening: it plays the first frame the player exists, behind the player's own eyes
        var host = new GameObject("OpeningCutscene");
        host.transform.SetParent(root);
        var cut = host.AddComponent<OpeningCutscene>();
        cut.pods = pods;
        cut.tags = tags;
        cut.board = board;
        cut.family = family;
        cut.familyLights = familyLights;
        cut.door = podDoor;
        cut.podGlass = yours != null ? yours.glass : null;
        cut.podLight = yours != null ? yours.light : null;
        cut.lamps = lamps;
        cut.floorY = y;
        cut.exit = new Vector3(0f, y, z1);
        return Exit(c, z1, y, halfW, y, top);
    }

    /// A pod: a steel base with a lit rim, a glass cylinder, a cap, the tag over it, the figure inside — and on the
    /// taken ones the rot wound round the figure and the pod lit from inside in the rot's red. `yaw` turns the
    /// pod (its figure, its tag, its rot): the family's face your pod, yours faces the way out. Nothing here has a
    /// collider (yours has the door, built separately)
    static PodParts Pod(Transform root, Vector3 at, float yaw, string tag, bool taken, int index)
    {
        var parts = new PodParts();
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        LevelKit.Prim(root, at + Vector3.up * 0.06f, new Vector3(2.6f, 0.06f, 2.6f), LevelKit.Steel, "PodBase", PrimitiveType.Cylinder);
        LevelKit.Prim(root, at + Vector3.up * 0.13f, new Vector3(2.7f, 0.01f, 2.7f), taken ? LevelKit.Amber : LevelKit.Led, "PodRim", PrimitiveType.Cylinder);
        parts.glass = LevelKit.Prim(root, at + Vector3.up * 1.65f, new Vector3(2.2f, 1.5f, 2.2f), LevelKit.GlassPane, "PodGlass", PrimitiveType.Cylinder); // clear: you can see what is in them
        LevelKit.Prim(root, at + Vector3.up * 3.25f, new Vector3(2.4f, 0.05f, 2.4f), LevelKit.Steel, "PodCap", PrimitiveType.Cylinder);
        if (taken)
        {
            // THE FAMILY: the father, the mother, the sibling — hanging in the gowns they were elevated in, the rot wound
            // round them, the pod lit in its red (infection red: the rot is danger). The same rig as you, in their own builds (PlayerAvatar)
            parts.figure = PlayerAvatar.Build(root, at, yaw, index == 0 ? PlayerAvatar.Figure.Father : index == 1 ? PlayerAvatar.Figure.Mother : PlayerAvatar.Figure.Sibling);
            parts.figure.Animate(PlayerAvatar.Act.Hollow, 1f, index * 1.7f, 10f);
            parts.figure.SetGlow(0.55f);
            LevelKit.Tendril(root, at + rot * new Vector3(0.6f, 0.15f, -0.3f), at + rot * new Vector3(0.25f, 1.9f, 0.05f), 0.13f, 6);
            LevelKit.Tendril(root, at + rot * new Vector3(-0.55f, 0.15f, 0.35f), at + rot * new Vector3(-0.2f, 1.6f, -0.1f), 0.11f, 5);
            parts.light = LevelKit.Light(root, at + Vector3.up * 2.6f, 5f, 1.4f);
            parts.light.color = new Color(1f, 0.15f, 0.125f);   // the hue the opening lights them in (2.0, 0.3, 0.25): nothing pops when it hands over
        }
        else
        {
            // Yours: you are standing in it, under a lamp's cream (the opening plays behind your eyes: no figure)
            parts.light = LevelKit.Light(root, at + Vector3.up * 2.6f, 5f, 1.2f);
            parts.light.color = new Color(1f, 0.9f, 0.7f);
        }
        // The tag: a dark board over the cap with the record's line, facing the way the pod does — the family's are
        // read from your pod, yours from the way out
        LevelKit.Prim(root, at + Vector3.up * 3.85f, new Vector3(2.3f, 0.55f, 0.08f), LevelKit.Graphite, "PodTag", PrimitiveType.Cube, rot);
        parts.tag = LevelKit.Text(root, at + rot * new Vector3(0f, 3.85f, 0.05f), rot * Quaternion.LookRotation(Vector3.back), tag, 0.05f, new Color(2.2f, 1.2f, 0.3f), "PodTagText", 2.1f, true, 60f, 130f);
        return parts;
    }
}

/// <summary>
/// The ward's seal: since 2026-10-26 it drops on the FIRST TRICK LANDED (MoveType.Trick "LANDED" or "LANDED  +…":
/// the pad before the door throws you, the left button at the top of the throw flips you, the landing opens the
/// way). The one pause in the game. The line over it reads the state in gold until then. (Before: the first punch
/// that connected, on two dummies; there are no units now.)
/// </summary>
public class WardLesson : MonoBehaviour, IWeldDynamic
{
    public void DynamicParts(List<Transform> into) { if (seal != null) into.Add(seal.transform); }
    public Enemy[] dummies;
    public GameObject seal;
    public LevelSections.StateSign sign;
    bool open;

    void OnEnable()
    {
        MovementEvents.MoveExecuted += OnMove;
        CombatDirector.KillReported += OnKill;
    }

    void OnDisable()
    {
        MovementEvents.MoveExecuted -= OnMove;
        CombatDirector.KillReported -= OnKill;
    }

    void OnMove(MoveType type, string detail)
    {
        if (type == MoveType.Trick && detail != null && detail.StartsWith("LANDED")) Release();   // a flip landed clean (perfect or not); a stumble keeps you in
    }

    void OnKill(KillMethod method, float hSpeed, bool airborne) => Release();

    void Update()
    {
        if (open || dummies == null) return;
        // Both down by any other means (a thrown card, a fall): nothing left to punch, so the way opens
        foreach (var e in dummies) if (e != null && e.Alive) return;
        Release();
    }

    void Release()
    {
        if (open) return;
        open = true;
        sign?.Set("OPEN");
        if (seal != null)
        {
            Destroy(seal);
            if (GameFeel.Instance != null) GameFeel.Instance.PlayJanitorHatch();
        }
    }
}
