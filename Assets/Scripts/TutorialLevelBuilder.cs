using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE TUTORIAL, ROOM BY ROOM. Twelve walled courts in a row under the open
/// sky, one subject each, and a hall with the ways out at the end. Every room
/// opens the same way: two billboards turned in toward the door you came
/// through — the LESSON on the left, the room's BOARD on the right — and you
/// walk between them into the room. The board is written up again, large, over
/// the far door, so the count is in front of you while you practise. The door
/// is locked until the board is done; then it shatters (TutorialRoom).
/// Every room is its own checkpoint, a fall puts you straight back at its
/// door with your stamina, and in here you cannot die and you heal on your own
/// (the bootstrap sets both) — the first sign says so, and says that out
/// there neither is true.
///   0 ORIENTATION   move, look, jump          7 MOMENTUM        edge boost, glass
///   1 BUNNY HOP     hops (bonus: the timing)  8 THE WALL PARRY  a timed press at a wall
///   2 SLIDE         slide, slide-jump         9 THE TRICK       the beat at the top, land it, perfect pays
///   3 DASH & DOUBLE the gap, the ledge       10 GRADUATION      all of it, at once
///   4 WALLS         kick, run, jump
///   5 SLAM          slam, slam storage
///   6 THE WHIP      two gold eyes, two drops
/// The signs read the player's OWN bindings and the live numbers. Every room
/// is sized against what the motor really does: a jump clears 1.6 m and 5.8 m,
/// a double 3.1 m, an air dash carries 18 m/s, a slide-jump is FASTER but
/// SHORTER than a jump, the grapple is a held swing that costs two pips of
/// three, glass wants 12 m/s. No units anywhere since 2026-10-26 (the factory
/// makes none): the lessons are the movement and THE TRICK on the left button.
/// </summary>
public static class TutorialLevelBuilder
{
    public const float CX = 100f, HW = 12f;
    const float PanelW = 8.4f, PanelH = 5.2f, PanelT = 0.3f, PanelYaw = 20f;
    const float DoorW = 7f, DoorH = 4.4f;

    /// For the bootstrap's doors and the city outside
    public static Vector3 Min, Max, MenuPortal, RangePortal, ChapterPortal;

    static Transform root;
    static MovementConfig cfg;
    static Vector3 spawn;
    static float cursor;
    static int roomNo;

    class Ctx
    {
        public string title;
        public float z0, z1, wallH;
        public readonly List<TutorialRoom.Task> tasks = new List<TutorialRoom.Task>();
        public readonly List<float> restarts = new List<float>();
    }

    struct Line { public string text; public Color ink; }
    static Line Key(string s) => new Line { text = s, ink = TutorialRoom.Bright };   // what to press, what it is called
    static Line Say(string s) => new Line { text = s, ink = TutorialRoom.Dim };      // what it is for
    static Line Gold(string s) => new Line { text = s, ink = TutorialRoom.GoldInk }; // the colours, in their own colour
    static Line Red(string s) => new Line { text = s, ink = TutorialRoom.RedInk };
    static Line Cyan(string s) => new Line { text = s, ink = TutorialRoom.CyanInk };

    static string K(GameAction a) => Keybinds.LabelAll(a);
    static string K1(GameAction a) => Keybinds.Label(a);

    public static Vector3 Build(MovementConfig config)
    {
        LevelKit.Init();
        LevelKit.SetZone(Zone.Utopia);
        TutorialGate.ClearRecency();
        cfg = config;
        root = new GameObject("TutorialLevel").transform;
        cursor = -14f;
        roomNo = 0;
        spawn = new Vector3(CX, 1.5f, -8f);

        // The wall behind the start, and the way out of the course in front of it
        LevelKit.Slab(root, new Vector3(CX, 3f, cursor - 0.5f), new Vector3(HW * 2f + 2f, 6f, 1f), LevelKit.Wall, "BackWall");
        MenuPortal = new Vector3(CX, 2f, cursor + 1.6f);

        Orientation();
        BunnyHop();
        Slide();
        DashAndDouble();
        Walls();
        Slam();
        Grapple();
        Momentum();
        WallParry();
        TrickRoom();
        Graduation();
        ExitHall();

        Min = new Vector3(CX - HW - 1f, 0f, -16f);
        Max = new Vector3(CX + HW + 1f, 14f, cursor + 2f);
        return spawn;
    }

    // ------------------------------------------------------------ the rooms

    static void Orientation()
    {
        var c = Begin("ORIENTATION", 46f, 6f);
        float a = c.z0;
        Floor(a, c.z1);
        Hurdle(a + 31f);
        Hurdle(a + 38f);
        c.tasks.Add(Move("JUMP", 3, MoveType.Jump, MoveType.Bhop, MoveType.PerfectBhop));
        End(c, 24f,   // the billboards stand further in: you start six metres inside this one
            Key("MOVE  " + K1(GameAction.Forward) + " " + K1(GameAction.Left) + " " + K1(GameAction.Back) + " " + K1(GameAction.Right) + "   ·   LOOK  MOUSE"),
            Key("JUMP  " + K(GameAction.Jump)),
            Say("IN HERE YOU CANNOT DIE,"),
            Say("AND YOU HEAL ON YOUR OWN."),
            Say("OUT THERE, NEITHER IS TRUE."),
            Key(K(GameAction.Respawn) + "  PUTS YOU BACK AT THE ROOM'S DOOR"),
            Say("DO WHAT THE BOARD ASKS AND THE DOOR OPENS."));
    }

    static void BunnyHop()
    {
        var c = Begin("BUNNY HOP", 86f, 6f);
        float a = c.z0;
        Floor(a, c.z1);
        for (float z = a + 22f; z < c.z1 - 8f; z += 8f) Guide(z);   // a rhythm to land on
        c.tasks.Add(Move("BUNNY HOP", 4, MoveType.Bhop, MoveType.PerfectBhop));
        c.tasks.Add(Bonus(Move("PERFECT HOP", 1, MoveType.PerfectBhop)));
        c.tasks.Add(Bonus(Speed("REACH 12 M/S", 12f)));
        End(c, 16f,
            Key("JUMP AGAIN THE MOMENT YOU LAND"),
            Say("A HOP KEEPS YOUR SPEED. THE GROUND TAKES IT."),
            Say("PRESS " + K(GameAction.Jump) + " EACH TIME. HOLDING IT DOES NOTHING."),
            Say("TIME THE PRESS EXACTLY ON THE LANDING"),
            Say("AND THE HOP ADDS SPEED INSTEAD."),
            Say("THAT IS THE RULE OF THIS PLACE:"),
            Key("THE HARDER THE TIMING, THE MORE IT GIVES."));
    }

    static void Slide()
    {
        var c = Begin("SLIDE", 72f, 6f);
        float a = c.z0;
        Floor(a, c.z1);
        // Two beams with 1.2 m under them: a slide is 0.9 m tall, you are 1.8
        Guide(a + 21f);
        LevelKit.Slab(root, new Vector3(CX, 2.6f, a + 27f), new Vector3(HW * 2f, 2.8f, 1.5f), LevelKit.Accent, "SlideBeam");
        Guide(a + 43f);
        LevelKit.Slab(root, new Vector3(CX, 2.6f, a + 49f), new Vector3(HW * 2f, 2.8f, 1.5f), LevelKit.Accent, "SlideBeam");
        c.tasks.Add(Move("SLIDE", 2, MoveType.Slide));
        c.tasks.Add(Move("SLIDE-JUMP", 2, MoveType.SlideJump, MoveType.PerfectSlideJump));
        c.tasks.Add(Bonus(Move("PERFECT SLIDE-JUMP", 1, MoveType.PerfectSlideJump)));
        End(c, 16f,
            Key(K(GameAction.Crouch) + " WHILE RUNNING — SLIDE"),
            Say("IT GOES UNDER THINGS, AND IT KEEPS YOUR SPEED."),
            Key(K(GameAction.Jump) + " OUT OF A SLIDE — SLIDE-JUMP"),
            Say("IT THROWS YOU OUT FASTER THAN YOU WENT IN."),
            Say("JUMP JUST AFTER THE SLIDE STARTS FOR THE MOST."),
            Say("SLIDE DOWNHILL AND IT ONLY GETS FASTER."));
    }

    static void DashAndDouble()
    {
        var c = Begin("DASH & DOUBLE JUMP", 76f, 6f);
        float a = c.z0;
        Floor(a, a + 26f);
        Floor(a + 34f, c.z1);                                                                            // the gap: 8 m, and a run-jump is 5.8
        Edge(a + 25.6f, 0f);
        LevelKit.Slab(root, new Vector3(CX, 1.2f, a + 53f), new Vector3(HW * 2f, 2.4f, 10f), LevelKit.Accent, "Ledge");   // 2.4 m, and a jump is 1.6
        c.tasks.Add(Move("DASH", 2, MoveType.Dash));
        c.tasks.Add(Move("DOUBLE JUMP", 2, MoveType.DoubleJump));
        End(c, 16f,
            Key(K(GameAction.Dash) + " — DASH, THE WAY YOU ARE MOVING"),
            Say("JUMP THE GAP, THEN DASH IN THE AIR."),
            Key(K(GameAction.Jump) + " AGAIN IN THE AIR — DOUBLE JUMP"),
            Say("THE LEDGE IS TOO HIGH FOR ONE."),
            Say("EACH COSTS A PIP OF STAMINA. PIPS COME BACK"),
            Say("SLOWLY — OR AT ONCE, FOR A TRICK DONE WELL."));
    }

    static void Walls()
    {
        var c = Begin("WALLS", 102f, 10f);
        float a = c.z0;
        Floor(a, a + 58f);
        Floor(a + 74f, c.z1);                                                                            // the pit: 16 m, and nothing jumps that
        // The kick: two walls facing each other and a barrier between them — a kick clears it, and so does a double
        LevelKit.Slab(root, new Vector3(CX - 4.5f, 4f, a + 30f), new Vector3(1f, 8f, 14f), LevelKit.Wall, "KickWall");
        LevelKit.Slab(root, new Vector3(CX + 4.5f, 4f, a + 30f), new Vector3(1f, 8f, 14f), LevelKit.Wall, "KickWall");
        LevelKit.Slab(root, new Vector3(CX, 1.1f, a + 30f), new Vector3(8f, 2.2f, 1f), LevelKit.Accent, "KickBarrier");
        LevelKit.Slab(root, new Vector3(CX - 8.5f, 3f, a + 30f), new Vector3(7f, 6f, 1f), LevelKit.Wall, "KickBlock");
        LevelKit.Slab(root, new Vector3(CX + 8.5f, 3f, a + 30f), new Vector3(7f, 6f, 1f), LevelKit.Wall, "KickBlock");
        // The hidden one: a pillar whose corners are marked
        LevelKit.Pillar(root, new Vector3(CX - 7f, 3f, a + 45f), new Vector3(2f, 6f, 2f), LevelKit.Wall);
        // The run: two piers out of the pit, eight metres apart, with a lead-in either end
        LevelKit.Slab(root, new Vector3(CX - 4.5f, -1f, a + 66f), new Vector3(1f, 22f, 30f), LevelKit.Wall, "RunPier");
        LevelKit.Slab(root, new Vector3(CX + 4.5f, -1f, a + 66f), new Vector3(1f, 22f, 30f), LevelKit.Wall, "RunPier");
        Edge(a + 57.6f, 0f);
        Restart(c, a + 49f);   // past the kick and the pillar: the run is its own try
        c.tasks.Add(Move("WALL KICK", 2, MoveType.WallKick, MoveType.EdgeBoost));
        c.tasks.Add(Move("WALL RUN", 1, MoveType.WallRun));
        c.tasks.Add(Move("WALL JUMP", 1, MoveType.WallJump));
        c.tasks.Add(Bonus(Move("EDGE BOOST", 1, MoveType.EdgeBoost)));   // a kick on the pillar's vertical corner
        End(c, 16f,
            Key("IN THE AIR BY A WALL, " + K(GameAction.Jump) + " — WALL KICK"),
            Say("OFF THE WALL AND UP. THE FIRST EACH JUMP IS FREE."),
            Key("JUMP ALONG A WALL HOLDING " + K1(GameAction.Forward) + " — WALL RUN"),
            Key(K(GameAction.Jump) + " OFF THE RUN — WALL JUMP"),
            Say("THE PIT IS TOO WIDE TO JUMP. RUN A WALL OVER IT."),
            Say("THE MARKED PILLAR HIDES SOMETHING."),
            Say("NOT EVERYTHING HERE IS EXPLAINED."));
    }

    static void Slam()
    {
        var c = Begin("SLAM", 68f, 6f);
        float a = c.z0;
        Floor(a, c.z1);
        LevelKit.JumpPad(root, new Vector3(CX, 0f, a + 24f), 0f, 17f, 3f);                                // 5.8 m up: over the tower
        LevelKit.Slab(root, new Vector3(CX, 2.25f, a + 37f), new Vector3(14f, 4.5f, 12f), LevelKit.Accent, "SlamTower");
        Edge(a + 42.6f, 4.5f, 13f);
        Guide(a + 50f);
        c.tasks.Add(Move("SLAM", 2, MoveType.Slam));
        c.tasks.Add(Move("SLAM STORAGE", 1, MoveType.SlamStorageJump));
        End(c, 16f,
            Key("IN THE AIR, " + K(GameAction.Slam) + " — SLAM"),
            Say("STRAIGHT DOWN, HARD. TAKE THE PAD UP AND TRY IT."),
            Key(K(GameAction.Jump) + " AS YOU LAND — SLAM STORAGE"),
            Say("THE FALL COMES BACK AS SPEED,"),
            Say("AND THE PIP COMES BACK WITH IT."),
            Say("PADS THROW YOU THE WAY YOU ARE GOING."));
    }

    static void Grapple()
    {
        var c = Begin("THE WHIP", 112f, 6f);
        float a = c.z0;
        Floor(a, a + 26f);
        Floor(a + 44f, a + 70f);                                                                         // the island: long, because a good swing throws you a long way
        Floor(a + 88f, c.z1);
        Edge(a + 25.6f, 0f);
        Edge(a + 69.6f, 0f);
        Restart(c, a + 45f);   // the island
        LevelKit.Orb(root, new Vector3(CX, 11f, a + 35f));
        LevelKit.Orb(root, new Vector3(CX, 11f, a + 79f));
        c.tasks.Add(Move("WHIP", 2, MoveType.GrappleYank));
        End(c, 16f,
            Key("AIM AT THE GOLD EYE AND HOLD " + K(GameAction.Grapple) + " — SWING"),
            Say("GRAVITY BUILDS SPEED THROUGH THE ARC."),
            Key("LET GO ON THE WAY UP AND KEEP ALL OF IT"),
            Say("IT TAKES TWO PIPS, AND IT STICKS TO ANYTHING."),
            Say("ON AN ENEMY IT DRAGS YOU TO THEM INSTEAD."));
    }

    static void Momentum()
    {
        var c = Begin("MOMENTUM", 82f, 6f);
        float a = c.z0;
        Floor(a, c.z1);
        // The ledge: 1.8 m, and a jump rises 1.6 — you meet its face with the 90° edge just above your feet. Jump
        // AGAIN as you meet it and the edge turns your speed into height (the corner boost): over it, speed kept
        LevelKit.Slab(root, new Vector3(CX, 0.9f, a + 38f), new Vector3(10f, 1.8f, 8f), LevelKit.Accent, "Ledge");
        Edge(a + 41.7f, 1.8f, 9.6f);
        // The combo: a wall to run on the way in, past the billboards; a wall jump off its end into the ledge's
        // edge is the WALL COMBO (the run sags you low, so you meet the edge, not the top)
        LevelKit.Slab(root, new Vector3(CX - 6.5f, 2f, a + 25.5f), new Vector3(1f, 4f, 11f), LevelKit.Wall, "ComboWall");
        // The pane: across the middle of the room, with a way round it either side for the slow
        LevelKit.Glass(root, new Vector3(CX, 2f, a + 62f), new Vector3(12f, 4f, 0.3f), cfg);
        LevelKit.Slab(root, new Vector3(CX - 6.5f, 2f, a + 62f), new Vector3(1f, 4f, 1f), LevelKit.Wall, "Jamb");
        LevelKit.Slab(root, new Vector3(CX + 6.5f, 2f, a + 62f), new Vector3(1f, 4f, 1f), LevelKit.Wall, "Jamb");
        c.tasks.Add(Move("CORNER BOOST", 1, MoveType.CornerBoost));
        c.tasks.Add(Bonus(MoveDetail("WALL COMBO", 1, "WALL COMBO", null, MoveType.CornerBoost)));
        c.tasks.Add(Move("BREAK THE GLASS", 1, MoveType.GlassBreak));
        End(c, 16f,
            Key("JUMP AT THE LEDGE. " + K(GameAction.Jump) + " AGAIN AS YOU MEET"),
            Key("ITS EDGE WITH YOUR FEET — CORNER BOOST"),
            Say("SPEED BECOMES HEIGHT, AND A PIP COMES BACK."),
            Say("EXACTLY ON THE EDGE IT GIVES THE MOST."),
            Say("OFF A WALL JUMP, MORE STILL — RUN THE WALL IN."),
            Key("GLASS BREAKS AT " + cfg.glassBreakSpeed.ToString("0") + " M/S. SLOWER, IT IS A WALL."),
            Say("RUNNING IS " + cfg.baseRunSpeed.ToString("0") + ". A DASH IS " + cfg.dashSpeed.ToString("0") + ". BOOSTS ADD UP."));
    }

    static void WallParry()
    {
        var c = Begin("THE WALL PARRY", 72f, 8f);
        float a = c.z0;
        Floor(a, c.z1);
        // Two walls to run at, one each side of the lane, so the second try needs no walk back
        var wall = new Vector3(CX - 6f, 3f, a + 30f);
        LevelKit.Slab(root, wall, new Vector3(9f, 6f, 1f), LevelKit.Wall, "ParryWall");
        Words(new Vector3(wall.x, 4.2f, wall.z - 0.53f), "RUN AT THIS WALL", 0.09f, TutorialRoom.Bright, 8f);
        Words(new Vector3(wall.x, 3.3f, wall.z - 0.53f), K(GameAction.Parry) + "  AS YOU REACH IT", 0.075f, TutorialRoom.GoldInk, 8f);
        var wall2 = new Vector3(CX + 6f, 3f, a + 52f);
        LevelKit.Slab(root, wall2, new Vector3(9f, 6f, 1f), LevelKit.Wall, "ParryWall");
        Words(new Vector3(wall2.x, 4.2f, wall2.z - 0.53f), "AND THIS ONE", 0.09f, TutorialRoom.Bright, 8f);
        Words(new Vector3(wall2.x, 3.3f, wall2.z - 0.53f), K(GameAction.Parry) + "  AT SPEED", 0.075f, TutorialRoom.GoldInk, 8f);
        c.tasks.Add(MoveDetail("PARRY A WALL", 2, "BLAST", null, MoveType.Parry));
        c.tasks.Add(Bonus(Speed("LEAVE A WALL AT 14 M/S", 14f)));
        End(c, 16f,
            Key("RUN AT A WALL AND " + K(GameAction.Parry) + " AS YOU REACH IT — WALL PARRY"),
            Say("IT BLASTS YOU OFF THE WALL WITH YOUR SPEED KEPT."),
            Say("THE PRESS IS TIMED: EARLY OR LATE, THE WALL JUST STOPS YOU."),
            Gold("GOLD IS THE WINDOW. RED IS A GUARD THAT MET NOTHING."),
            Say("A GUARD ON NOTHING LOCKS THE ARMS FOR A MOMENT."),
            Say("OUT THERE, A PARRY IS THE ONLY THING THAT HEALS,"),
            Say("AND EACH ONE IN A ROW HEALS MORE."));
    }

    /// THE TRICK (2026-10-26, user: "use the left click for qte events, this is for landing tricks"): three pads in a
    /// row throw you high; at the top of each throw the ring closes on the HUD and the left button at its beat throws
    /// a flip (W front, S back, A/D side, nothing = spin); land it before the arc ends and it is LANDED, on the beat
    /// and it is PERFECT and pays speed and a pip on touchdown; land it mid-arc and you stumble. The lips after each
    /// pad are the landings
    static void TrickRoom()
    {
        var c = Begin("THE TRICK", 96f, 11f);
        float a = c.z0;
        Floor(a, c.z1);
        for (int i = 0; i < 3; i++)
        {
            // 17.4 m/s up under gravity 25 is six metres and 1.4 s of air: one flip on the beat (the test level's number)
            float z = a + 24f + i * 22f;
            LevelKit.JumpPad(root, new Vector3(CX, 0f, z), 0f, 17.4f + i * 0.6f, 3f);
            LevelKit.Lip(root, new Vector3(CX, 0.3f, z + 7f + i * 0.5f), new Vector3(6f, 0.6f, 3f), LevelKit.Accent, "TrickLanding");
        }
        c.tasks.Add(MoveDetail("THROW A TRICK", 1, null, "STUMBLE", MoveType.Trick));
        c.tasks.Add(MoveDetail("LAND A TRICK", 1, "LANDED", null, MoveType.Trick));
        c.tasks.Add(Bonus(MoveDetail("PERFECT TRICK", 1, "LANDED  +", null, MoveType.Trick)));
        End(c, 16f,
            Key("IN THE AIR, " + K(GameAction.Trick) + " STARTS THE BAR — " + K1(GameAction.Trick) + " AGAIN AS THE MARKER HITS THE GOLD"),
            Say("W IS A FRONT FLIP, S A BACKFLIP, A OR D A SIDE FLIP, NOTHING A SPIN."),
            Say("LAND BEFORE THE FLIP ENDS AND IT IS LANDED."),
            Gold("ON THE BEAT IT IS PERFECT: SPEED AND A PIP ON TOUCHDOWN."),
            Red("LAND MID-FLIP AND YOU STUMBLE."),
            Say("A LONG ENOUGH AIR TAKES TWO."));
    }

    static void Graduation()
    {
        var c = Begin("GRADUATION", 102f, 9f);
        float a = c.z0;
        Floor(a, c.z1);
        Screen(a + 22f);
        // All of it at once: pads, orbs, cover, a stand, a pane — and a room full of what you have met
        LevelKit.JumpPad(root, new Vector3(CX - 7f, 0f, a + 30f), 0f, 15f, 6f);
        LevelKit.JumpPad(root, new Vector3(CX + 7f, 0f, a + 56f), 0f, 15f, 6f);
        for (int i = 0; i < 3; i++) LevelKit.Orb(root, new Vector3(CX + (i - 1) * 6f, 10f, a + 40f + i * 16f));
        for (int i = 0; i < 4; i++)
            LevelKit.Slab(root, new Vector3(CX + (i % 2 == 0 ? -4f : 4f), 1.5f, a + 36f + i * 12f), new Vector3(3f, 3f, 1.2f), LevelKit.Accent, "Cover");
        LevelKit.Slab(root, new Vector3(CX - 9f, 1.5f, a + 64f), new Vector3(6f, 3f, 12f), LevelKit.Accent, "Stand");
        LevelKit.Glass(root, new Vector3(CX + 6f, 2f, a + 82f), new Vector3(8f, 4f, 0.3f), cfg);
        Physics.SyncTransforms();
        c.tasks.Add(MoveDetail("LAND A TRICK OFF A PAD", 1, "LANDED", null, MoveType.Trick));
        c.tasks.Add(Move("YANK AN ORB", 1, MoveType.GrappleYank));
        c.tasks.Add(Bonus(Speed("REACH 20 M/S", 20f)));
        End(c, 16f,
            Say("EVERYTHING YOU HAVE MET IS IN THIS ROOM."),
            Say("NOTHING IN IT IS NEW."),
            Red("RED MEANS MOVE."),
            Gold("GOLD MEANS PARRY."),
            Cyan("CYAN MEANS BREAK."),
            Key("KEEP MOVING. SPEED IS THE SCORE."));
    }

    /// Past the last door: the two ways on, and what is different out there
    static void ExitHall()
    {
        var c = Begin("WAYS OUT", 22f, 9f);
        Floor(c.z0, c.z1);
        float z = c.z1;
        LevelKit.Slab(root, new Vector3(CX, c.wallH * 0.5f, z), new Vector3(HW * 2f + 2f, c.wallH, 1f), LevelKit.Wall, "EndWall");
        RangePortal = new Vector3(CX - 6f, 2f, z - 2.2f);
        ChapterPortal = new Vector3(CX + 6f, 2f, z - 2.2f);
        Words(new Vector3(CX, 7.6f, z - 0.53f), "EVALUATION COMPLETE", 0.16f, TutorialRoom.Bright, 20f);
        Words(new Vector3(CX, 6.5f, z - 0.53f), "OUT THERE YOU CAN DIE, AND NOTHING HEALS YOU BUT A PARRY.", 0.07f, TutorialRoom.Dim, 20f);
        Room(c, null);
        cursor = z;
    }

    // ------------------------------------------------------------ the grammar of a room

    static Ctx Begin(string title, float len, float wallH)
    {
        var c = new Ctx { title = title, z0 = cursor, z1 = cursor + len, wallH = wallH };
        float mid = (c.z0 + c.z1) * 0.5f;
        LevelKit.Slab(root, new Vector3(CX - HW - 0.5f, wallH * 0.5f, mid), new Vector3(1f, wallH, len), LevelKit.Wall, "WallW");
        LevelKit.Slab(root, new Vector3(CX + HW + 0.5f, wallH * 0.5f, mid), new Vector3(1f, wallH, len), LevelKit.Wall, "WallE");
        return c;
    }

    /// The far wall and its locked door, the two billboards inside the way in, the board over the way out
    static void End(Ctx c, float billboardsAt, params Line[] lesson)
    {
        float z = c.z1, h = c.wallH + 0.02f, side = (HW * 2f + 2f - DoorW) * 0.5f;   // a hair over the side walls: no two tops share a plane
        LevelKit.Slab(root, new Vector3(CX - DoorW * 0.5f - side * 0.5f, h * 0.5f, z), new Vector3(side, h, 1f), LevelKit.Wall, "EndWallW");
        LevelKit.Slab(root, new Vector3(CX + DoorW * 0.5f + side * 0.5f, h * 0.5f, z), new Vector3(side, h, 1f), LevelKit.Wall, "EndWallE");
        if (h > DoorH) LevelKit.Slab(root, new Vector3(CX, (DoorH + h) * 0.5f, z), new Vector3(DoorW, h - DoorH, 1f), LevelKit.Wall, "Lintel");

        // The door: a holder at unit scale, so the words on it are never children of a stretched slab
        var door = new GameObject("RoomDoor");
        door.transform.SetParent(root);
        door.transform.position = new Vector3(CX, 0f, z);
        LevelKit.Slab(door.transform, new Vector3(CX, DoorH * 0.5f, z), new Vector3(DoorW, DoorH, 0.6f), LevelKit.Iron, "DoorSlab");
        LevelKit.HoloSign(door.transform, new Vector3(CX, 2.9f, z - 0.36f), Vector3.back, 5.6f, 1.1f, "LOCKED  ·  FINISH THE BOARD");
        LevelKit.Chevrons(root, new Vector3(CX, 0f, z - 9f), Vector3.forward, 3);

        float bz = c.z0 + billboardsAt;
        Lesson(c, bz, lesson);
        var room = Room(c, door);
        room.boards.Add(NearBoard(c, room.number, bz));
        room.boards.Add(FarBoard(c));
        cursor = z;
    }

    /// The room's own brain
    static TutorialRoom Room(Ctx c, GameObject door)
    {
        var host = new GameObject("Room " + roomNo + " " + c.title);
        host.transform.SetParent(root);
        var room = host.AddComponent<TutorialRoom>();
        room.title = c.title;
        room.number = roomNo;
        room.z0 = c.z0;
        room.z1 = c.z1;
        room.floorY = 0f;
        room.entry = roomNo == 0 ? spawn : new Vector3(CX, 1.2f, c.z0 + 2.5f);   // the first room's door is the start itself
        room.tasks = c.tasks;
        room.restarts = c.restarts.ToArray();
        room.door = door;
        roomNo++;
        return room;
    }

    // ------------------------------------------------------------ billboards and boards

    /// A billboard: a slab standing on the floor, turned in toward the way you came. Solid — you can walk into it
    static Vector3 Billboard(float z, int side, out Quaternion rot)
    {
        rot = Quaternion.Euler(0f, side * PanelYaw, 0f);
        var centre = new Vector3(CX + side * 7.4f, PanelH * 0.5f, z);
        var slab = LevelKit.Slab(root, centre, new Vector3(PanelW, PanelH, PanelT), LevelKit.Graphite, side < 0 ? "LessonPanel" : "BoardPanel");
        slab.transform.rotation = rot;
        LevelKit.Prim(root, centre + Vector3.up * (PanelH * 0.5f + 0.04f), new Vector3(PanelW, 0.08f, PanelT + 0.06f), LevelKit.Led, "PanelCap", PrimitiveType.Cube, rot);
        return centre;
    }

    /// Left of the way in: the subject, then what to press and what it is for
    static void Lesson(Ctx c, float z, Line[] lines)
    {
        Vector3 centre = Billboard(z, -1, out Quaternion rot);
        float w = PanelW - 0.6f;
        On(centre, rot, PanelT, 0f, 1.95f, c.title, 0.1f, TutorialRoom.Bright, w, TextAnchor.MiddleCenter, 70f, 130f);
        Rule(centre, rot, 1.42f);
        for (int i = 0; i < lines.Length; i++)
            On(centre, rot, PanelT, 0f, 0.95f - i * 0.5f, lines[i].text, 0.055f, lines[i].ink, w, TextAnchor.MiddleCenter, 70f, 130f);
    }

    /// Right of the way in: the room's number and its board
    static TutorialRoom.Board NearBoard(Ctx c, int number, float z)
    {
        Vector3 centre = Billboard(z, 1, out Quaternion rot);
        float w = PanelW - 0.6f;
        On(centre, rot, PanelT, 0f, 2.05f, "ROOM " + number.ToString("00") + "  ·  THE BOARD", 0.05f, TutorialRoom.Dim, w, TextAnchor.MiddleCenter, 70f, 130f);
        Rule(centre, rot, 1.62f);
        var board = Rows(c, centre, rot, PanelT, w * 0.5f, 1.05f, 0.62f, 0.062f, 70f, 130f);
        board.footSize = 0.045f;
        board.footWidth = w;
        board.foot = On(centre, rot, PanelT, 0f, -2.2f, "", board.footSize, TutorialRoom.Dim, w, TextAnchor.MiddleCenter, 70f, 130f);
        return board;
    }

    /// Over the far door, large: the same board, in front of you while you practise
    static TutorialRoom.Board FarBoard(Ctx c)
    {
        const float W = 14f, H = 4.2f, T = 0.4f;
        var centre = new Vector3(CX, c.wallH + H * 0.5f, c.z1 - 0.5f + T * 0.5f);
        LevelKit.Slab(root, centre, new Vector3(W, H, T), LevelKit.Graphite, "FarBoard");
        LevelKit.Prim(root, centre + Vector3.up * (H * 0.5f + 0.05f), new Vector3(W, 0.1f, T + 0.06f), LevelKit.Led, "FarBoardCap");
        int n = Mathf.Max(1, c.tasks.Count);
        return Rows(c, centre, Quaternion.identity, T, W * 0.5f - 0.5f, (n - 1) * 0.45f, 0.9f, 0.105f, 170f, 300f);
    }

    /// A label on the left and a count on the right for every task, from `top` down
    static TutorialRoom.Board Rows(Ctx c, Vector3 centre, Quaternion rot, float depth, float half, float top, float pitch, float size, float fadeStart, float fadeEnd)
    {
        int n = c.tasks.Count;
        var board = new TutorialRoom.Board { labels = new TextMesh[n], counts = new TextMesh[n], size = size, countWidth = half * 0.6f };
        for (int i = 0; i < n; i++)
        {
            var t = c.tasks[i];
            float v = top - i * pitch;
            board.labels[i] = On(centre, rot, depth, -half, v, (t.bonus ? "BONUS  ·  " : "") + t.label, size, t.bonus ? TutorialRoom.Dim : TutorialRoom.Bright,
                half * 1.35f, TextAnchor.MiddleLeft, fadeStart, fadeEnd);
            board.counts[i] = On(centre, rot, depth, half, v, "", size, TutorialRoom.Bright, board.countWidth, TextAnchor.MiddleRight, fadeStart, fadeEnd);
        }
        return board;
    }

    /// One line of words on the face of a panel: `u` to the reader's right of its centre, `v` up
    static TextMesh On(Vector3 centre, Quaternion rot, float depth, float u, float v, string text, float size, Color ink, float maxWidth,
        TextAnchor anchor, float fadeStart, float fadeEnd)
    {
        Vector3 pos = centre + rot * new Vector3(u, v, -(depth * 0.5f + 0.03f));
        var tm = LevelKit.Text(root, pos, rot, text, size, ink, "Sign", maxWidth, false, fadeStart, fadeEnd);
        tm.anchor = anchor;
        tm.alignment = anchor == TextAnchor.MiddleLeft ? TextAlignment.Left : anchor == TextAnchor.MiddleRight ? TextAlignment.Right : TextAlignment.Center;
        return tm;
    }

    static void Rule(Vector3 centre, Quaternion rot, float v)
        => LevelKit.Prim(root, centre + rot * new Vector3(0f, v, -(PanelT * 0.5f + 0.02f)), new Vector3(PanelW - 1.2f, 0.035f, 0.02f), LevelKit.Led, "Rule", PrimitiveType.Cube, rot);

    /// Words on a wall that faces back down the course
    static void Words(Vector3 pos, string text, float size, Color ink, float maxWidth)
        => LevelKit.Text(root, pos, Quaternion.identity, text, size, ink, "Sign", maxWidth, false, 70f, 160f);

    // ------------------------------------------------------------ the furniture

    static void Floor(float z0, float z1)
        => LevelKit.FloorSlab(root, new Vector3(CX, -0.5f, (z0 + z1) * 0.5f), new Vector3(HW * 2f, 1f, z1 - z0), "Floor");

    /// A low wall to jump: 0.8 m, the full width
    static void Hurdle(float z)
        => LevelKit.Slab(root, new Vector3(CX, 0.4f, z), new Vector3(HW * 2f, 0.8f, 1.2f), LevelKit.Accent, "Hurdle");

    /// A cool line across the floor: start here
    static void Guide(float z)
        => LevelKit.NeonStrip(root, new Vector3(CX, 0.03f, z), new Vector3(HW * 2f - 2f, 0.05f, 0.25f), LevelKit.Led);

    /// A line past which a fall puts you back here (TutorialRoom.restarts)
    static void Restart(Ctx c, float z)
    {
        c.restarts.Add(z);
        LevelKit.Chevrons(root, new Vector3(CX, 0f, z + 0.5f), Vector3.forward, 2);
    }

    /// A hazard line along the lip of a drop
    static void Edge(float z, float y, float width = 0f)
        => LevelKit.HazardBar(root, new Vector3(CX, y + 0.03f, z), new Vector3(width > 0f ? width : HW * 2f - 2f, 0.05f, 0.3f));

    /// A blast screen across the middle of the lane: nothing past it has a line to you while you read
    static void Screen(float z)
        => LevelKit.Slab(root, new Vector3(CX, 1.6f, z), new Vector3(10f, 3.2f, 1f), LevelKit.Accent, "Screen");

    /// A practice target: a dummy that holds its fire
    static Enemy Target(Vector3 groundPos)
    {
        var e = Enemy.CreateDummy(groundPos, cfg);
        if (e != null) e.HoldFire = true;   // null since 2026-10-26: no units
        return e;
    }

    /// A unit that stays down once it is down: a cleared room stays cleared
    static Enemy Once(Enemy e)
    {
        if (e != null) e.StaysDead = true;
        return e;
    }

    // ------------------------------------------------------------ the tasks

    static TutorialRoom.Task Move(string label, int need, params MoveType[] moves)
        => new TutorialRoom.Task { label = label, need = need, moves = moves };

    static TutorialRoom.Task MoveDetail(string label, int need, string starts, string notStarts, params MoveType[] moves)
        => new TutorialRoom.Task { label = label, need = need, moves = moves, starts = starts, notStarts = notStarts };

    static TutorialRoom.Task Speed(string label, float metresPerSecond)
        => new TutorialRoom.Task { label = label, need = 1, speed = metresPerSecond };

    static TutorialRoom.Task Kill(string label, int need, params Enemy[] units)
        => new TutorialRoom.Task { label = label, need = need, kill = units };

    static TutorialRoom.Task Bonus(TutorialRoom.Task t) { t.bonus = true; return t; }
}
