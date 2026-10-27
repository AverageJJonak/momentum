using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE LEVELS AS SCORES (2026-10-27, the mandate: "design it in the text window", no hand placement; the user:
/// "you will be generating most of the level"). Every level is ONE LINE OF TEXT: a word per set, in the order the
/// sets come, read left to right the way the course runs. The parser turns the words into Course.SetKind and the
/// gauntlet builds them, each set harder than the last, under the brutalist sky with the city's monoliths round it.
/// To redesign a level, rewrite its line. The Level Editor's hand fixes lay over the result as before.
///
/// The words:  FIELD  MONOLITHS  WALLS  SHAFT  PRESS  CHUTE  DRAUGHT  BRIDGE   (Course.SetKind, the titles in the HUD)
/// </summary>
public static class GauntletLevels
{
    /// A level: its title (the checkpoints' prefix), its score, the lane's half width, its seed
    class Score
    {
        public string title, line;
        public float halfW;
        public int seed;
        public Score(string title, string line, float halfW, int seed) { this.title = title; this.line = line; this.halfW = halfW; this.seed = seed; }
    }

    // THE SCORES. Act I learns the vocabulary one word at a time and ends on the Concierge's chase; Act II is the
    // long middle, more shafts and bridges, the Stationmaster's chase at its end; Act III is the open air, fields
    // and monoliths at scale. Lanes narrow as the acts go on.
    // SHORT COURSES (2026-10-27, VHOLUME: a course is a minute or two against an author's time, not a quarter hour).
    // Five sets each, THE DROP always first, so every course opens on the chain the game is built on; interiors and
    // exteriors in turn; the words from LevelSectionsCourseGauntlet. The acts narrow the lane as they go.
    static readonly Dictionary<LevelKind, Score> Scores = new Dictionary<LevelKind, Score>
    {
        // THE SPACES (2026-10-27, the user's VHOLUME notes: branching paths, drop-offs, crawl-spaces, oppressive scale):
        // after the drop, three monumental spaces each with an over, a through and an around (LevelSectionsCourseSpaces)
        { LevelKind.Level1,  new Score("THE SANATORIUM",        "DROP HALL PLAZA STREET", 12f, 11) },
        { LevelKind.Level2,  new Score("THE THEATRE DISTRICT",  "DROP STREET WELL SPAN", 12f, 22) },
        { LevelKind.Level3,  new Score("THROUGH THE CITY",      "DROP PLAZA HALL WELL", 12f, 33) },
        { LevelKind.Level4,  new Score("THE GRAND HOTELS",      "DROP SPAN STREET HALL", 12f, 44) },
        { LevelKind.Level5,  new Score("HALCYON HOUSE",         "DROP HALL WELL PLAZA", 12f, 55) },
        { LevelKind.Level6,  new Score("HALCYON METRO",         "DROP WELL STREET SPAN", 11f, 66) },
        { LevelKind.Level7,  new Score("THE TUNNELS",           "DROP HALL SPAN WELL", 11f, 77) },
        { LevelKind.Level8,  new Score("THE SEWERS",            "DROP STREET PLAZA SPAN", 11f, 88) },
        { LevelKind.Level9,  new Score("OLD TOWN",              "DROP WELL HALL STREET", 11f, 99) },
        { LevelKind.Level10, new Score("THE TERMINUS",          "DROP PLAZA SPAN WELL", 12f, 110) },
        { LevelKind.Level11, new Score("ABOVE THE TERMINUS",    "DROP SPAN HALL PLAZA", 13f, 121) },
        { LevelKind.Level12, new Score("THE ROOFTOPS",          "DROP STREET SPAN HALL", 13f, 132) },
        { LevelKind.Level13, new Score("THE BLOCKS",            "DROP PLAZA WELL STREET", 14f, 143) },
    };

    /// A score's words as set kinds; an unknown word is skipped with a warning, so a typo costs a set, not the level
    public static Course.SetKind[] Parse(string line)
    {
        var kinds = new List<Course.SetKind>();
        foreach (string word in line.Split(' '))
        {
            if (string.IsNullOrWhiteSpace(word)) continue;
            switch (word.Trim().ToUpperInvariant())
            {
                case "DROP": kinds.Add(Course.SetKind.Drop); break;
                case "TERRACES": case "FIELD": kinds.Add(Course.SetKind.Terraces); break;
                case "MONOLITHS": kinds.Add(Course.SetKind.Monoliths); break;
                case "CANYON": case "WALLS": kinds.Add(Course.SetKind.Canyon); break;
                case "SHAFT": kinds.Add(Course.SetKind.Shaft); break;
                case "CHUTE": kinds.Add(Course.SetKind.Chute); break;
                case "DRAUGHT": kinds.Add(Course.SetKind.Draught); break;
                case "STAIRS": kinds.Add(Course.SetKind.Stairs); break;
                case "POLES": kinds.Add(Course.SetKind.Poles); break;
                case "ROOFS": kinds.Add(Course.SetKind.Roofs); break;
                case "APARTMENTS": kinds.Add(Course.SetKind.Apartments); break;
                case "HALL": kinds.Add(Course.SetKind.Hall); break;
                case "WELL": kinds.Add(Course.SetKind.Well); break;
                case "PLAZA": kinds.Add(Course.SetKind.Plaza); break;
                case "STREET": kinds.Add(Course.SetKind.Street); break;
                case "SPAN": kinds.Add(Course.SetKind.Span); break;
                default: Debug.LogWarning("GauntletLevels: no set called '" + word + "'"); break;
            }
        }
        return kinds.ToArray();
    }

    /// The zone a level's course is dressed in: Act I the civic streets (the House's own block in its black and brass),
    /// Act II the Underground's brick and iron, Act III the roofs' limestone
    static LevelSections.Cursor ZoneFor(LevelKind kind, LevelSections.Cursor c)
    {
        switch (kind)
        {
            case LevelKind.Level5: return LevelSections.Office(c);
            case LevelKind.Level6: case LevelKind.Level8: return LevelSections.Under(c);
            case LevelKind.Level7: case LevelKind.Level9: case LevelKind.Level10: return LevelSections.Works(c);
            case LevelKind.Level11: case LevelKind.Level12: case LevelKind.Level13: return LevelSections.Surface(c);
            default: return LevelSections.Utopia(c);
        }
    }

    /// The level: its opening (Level 1 wakes in the Ward and leaves it on a landed trick), its score as a gauntlet in
    /// its act's zone, its ending (the two chases where the bosses are), the finish
    public static Vector3 Build(LevelKind kind, MovementConfig config)
    {
        if (!Scores.TryGetValue(kind, out var score)) score = new Score(kind.ToString().ToUpperInvariant(), "FIELD WALLS CHUTE BRIDGE", 14f, 7);
        LevelKit.Init();
        Transform root = new GameObject(kind.ToString()).transform;
        var c = LevelSections.Begin(score.seed, 14f);
        if (kind == LevelKind.Level1)
        {
            c = LevelSections.Ward(root, config, LevelSections.Office(c));   // the opening plays here; the seal drops on the first trick landed
            Course.Checkpoint(root, c, "THE STREET DOOR");
        }
        var order = Parse(score.line);
        // THE LOOK IS GOTHAM DARK DECO (user 2026-10-27: "keep the gotham dark deco aesthetic, just look to vholume for
        // level design and layout"): each act's own zone dresses the course — the civic cream and terrazzo of Act I, the
        // House's black and brass for its own block, the brick and iron of the Underground's acts, the limestone of the
        // roofs — under the painted skies, with the Deco city round every one of them (CityVista)
        // THE COURSE TURNS (user, the same day: "chapter 1 is still too linear"): a course that ends on a chase or a lift
        // must come back to the straight line those are built on, so it runs straight; every other course is an ASCENT —
        // the drop, then its spaces as the legs of a square spiral round a block, a corner and a storey between each
        bool straight = kind == LevelKind.Level5 || kind == LevelKind.Level10;
        if (straight) c = Course.Gauntlet(root, config, ZoneFor(kind, c), order.Length, score.halfW, score.title, order);
        else
        {
            var legs = new List<Course.SetKind>(order);
            if (legs.Count > 0 && legs[0] == Course.SetKind.Drop) legs.RemoveAt(0);   // the ascent opens on the drop itself
            Course.Ascent(root, config, ZoneFor(kind, c), legs.ToArray(), score.halfW, score.title);
            return LevelSections.Last.spawn;   // the ascent builds its own finish
        }
        if (kind == LevelKind.Level5)
        {
            c = LevelSections.Office(c);
            Course.Checkpoint(root, c, "THE ATRIUM");
            c = Chase.Build(root, config, c, ChaseKind.Concierge);   // THE CHASE: the Concierge rides ahead; the course generates as you run
        }
        else if (kind == LevelKind.Level10)
        {
            c = LevelSections.Works(c);
            Course.Checkpoint(root, c, "THE TERMINUS GATES");
            c = Chase.Build(root, config, c, ChaseKind.Stationmaster);
            LevelSections.LiftLanding(root, config, c);   // the lift to the surface: the act's ending
            return LevelSections.Last.spawn;
        }
        LevelSections.Finish(root, config, c);
        return LevelSections.Last.spawn;
    }
}
