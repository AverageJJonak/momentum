using UnityEngine;

/// <summary>
/// The one list of playable levels. The level-select menu, the report card's
/// time targets and grades, and the per-level records (best time, best score)
/// all read from here — adding a level is one line: scene name, title,
/// tagline, blurb, gold and par times. Scenes must also be in the build list
/// (Momentum → Fix Scene List).
/// </summary>
public static class LevelCatalog
{
    public struct LevelInfo
    {
        public string scene, title, subtitle, blurb;
        public float gold, par; // seconds: full speed marks at gold, none at par
        /// The postcard: where the level-select thumbnail is shot from, and what it looks at
        public Vector3 camPos, camLookAt;
        public float camFov;

        public LevelInfo(string scene, string title, string subtitle, string blurb, float gold, float par,
            Vector3 camPos, Vector3 camLookAt, float camFov = 70f)
        {
            this.scene = scene;
            this.title = title;
            this.subtitle = subtitle;
            this.blurb = blurb;
            this.gold = gold;
            this.par = par;
            this.camPos = camPos;
            this.camLookAt = camLookAt;
            this.camFov = camFov;
        }
    }

    /// A chapter: a run of consecutive catalog levels under one name
    public struct ChapterInfo
    {
        public string name, subtitle;
        public int first, count;
        public ChapterInfo(string name, string subtitle, int first, int count)
        {
            this.name = name;
            this.subtitle = subtitle;
            this.first = first;
            this.count = count;
        }
    }

    public static readonly ChapterInfo[] Chapters =
    {
        new ChapterInfo("ACT I", "THE HOUSE", 0, 5),
        new ChapterInfo("ACT II", "THE WORKS", 5, 5),
        new ChapterInfo("ACT III", "THE HEIGHTS", 10, 3),
    };

    public static readonly LevelInfo[] All =
    {
        new LevelInfo("Level1", "FIRST FLIGHT", "THE SANATORIUM",
            "The ward you wake in, then the first course: the drop, the hall of beams (under them sliding, over them hopping, or the gallery along the wall), the plaza and its monument (the stair over it, the tunnel through it, the long way round), the street between two faces. Three ways through everything. A minute if you are clean.",
            40f, 88f, new Vector3(-60f, 35f, 69f), new Vector3(5.75f, 17f, 128f), 68f),
        new LevelInfo("Level2", "CURTAIN UP", "THE THEATRE DISTRICT",
            "The theatre district: the drop, the street between fifty-metre faces (the ledges, the cornice, or the street itself), the well and its drop-offs or its chute, the span with its gaps and the lower deck beneath.",
            44f, 96f, new Vector3(-30f, 9.5f, 164f), new Vector3(6f, 8.5f, 206f), 78f),
        new LevelInfo("Level3", "BELOW STAIRS", "THROUGH THE CITY",
            "Through the city: the drop, the plaza and its monument, the hall of beams, the well down to the end.",
            48f, 105f, new Vector3(-37.6f, -9.6f, 185f), new Vector3(-39.6f, -3f, 220f), 68f),
        new LevelInfo("Level4", "THE GRAND HOTELS", "THE ASTOR & THE MAJESTIC",
            "The grand hotels: the drop, the span, the street between the faces, the hall of beams to end.",
            52f, 114f, new Vector3(-22.75f, 4f, 23f), new Vector3(20.25f, 14f, 85f), 78f),
        new LevelInfo("Level5", "CHECKING IN", "HALCYON HOUSE",
            "Halcyon House: the drop, the hall of beams, the well, the plaza, and then the Concierge rides ahead and the chase builds itself as you run. The first act over.",
            56f, 123f, new Vector3(-23.8f, 7f, 449f), new Vector3(0f, 3f, 477f), 70f),
        new LevelInfo("Level6", "LAST TRAIN", "HALCYON METRO",
            "Halcyon Metro, the long middle begins: the drop, the well, the street, the span. Eleven wide.",
            60f, 132f, new Vector3(10.8f, -1f, 132f), new Vector3(-3f, -5f, 174f), 76f),
        new LevelInfo("Level7", "THIRD RAIL", "THE TUNNELS",
            "The tunnels: the drop, the hall of beams, the span, the well.",
            64f, 140f, new Vector3(7.5f, 17.5f, 186f), new Vector3(-8f, 15.2f, 235f), 76f),
        new LevelInfo("Level8", "HIGH WATER", "THE SEWERS",
            "The sewers: the drop, the street, the plaza, the span.",
            68f, 149f, new Vector3(-4.8f, 5.5f, 99.1f), new Vector3(0f, -10f, 130f), 76f),
        new LevelInfo("Level9", "THE FOUNDATIONS", "OLD TOWN",
            "Old Town at the foundations: the drop, the well, the hall of beams, the street.",
            72f, 158f, new Vector3(10.8f, 19f, 88f), new Vector3(-3f, 15f, 130f), 76f),
        new LevelInfo("Level10", "TERMINUS", "THE STATIONMASTER",
            "The terminus: the drop, the plaza, the span, the well down to the gates, where the Stationmaster's locomotive is waiting to be chased. Then the lift to the surface.",
            76f, 167f, new Vector3(-22.4f, 23f, 346f), new Vector3(2f, 16f, 388f), 78f),
        new LevelInfo("Level11", "THE BREACH", "ABOVE THE TERMINUS",
            "Above the terminus, the open air: the drop, the span, the hall of beams, the plaza. Thirteen wide.",
            80f, 176f, new Vector3(-34f, 16f, -44f), new Vector3(0f, 24f, 300f), 74f),
        new LevelInfo("Level12", "SKYLINE", "THE ROOFTOPS",
            "The rooftops: the drop, the street, the span, the hall of beams, the sun going down.",
            84f, 184f, new Vector3(-150f, 215f, 560f), new Vector3(0f, 190f, 1150f), 74f),
        new LevelInfo("Level13", "CROSSTOWN", "THE BLOCKS",
            "The blocks, crosstown after dark: the drop, the plaza, the well, the street. Fourteen wide, the widest lane in the game.",
            88f, 193f, new Vector3(-190f, 310f, 520f), new Vector3(20f, 280f, 950f), 74f),
    };

    /// True for scenes that get a timer and a report card
    public static bool IsCampaignScene(string scene) => TryFind(scene, out _);

    public static bool TryFind(string scene, out LevelInfo info)
    {
        foreach (var l in All)
            if (l.scene == scene) { info = l; return true; }
        info = default;
        return false;
    }

    /// The scene's index in All (-1 if it is off the catalog): the run counts levels by this
    public static int IndexOf(string scene)
    {
        for (int i = 0; i < All.Length; i++) if (All[i].scene == scene) return i;
        return -1;
    }

    /// The scene after this one in the programme's order, or null for the last (and for a scene off the catalog)
    public static string NextScene(string scene)
    {
        int i = IndexOf(scene);
        return i >= 0 && i + 1 < All.Length ? All[i + 1].scene : null;
    }

    /// Where a level index sits in the programme: its chapter and its 1-based scene number within it
    public static bool Place(int index, out int chapter, out int sceneNo)
    {
        chapter = ChapterOfIndex(index);
        sceneNo = chapter >= 0 ? index - Chapters[chapter].first + 1 : 0;
        return chapter >= 0;
    }

    // Records live in PlayerPrefs under the scene name: the timer and the report card write them
    public static float BestTime(string scene) => PlayerPrefs.GetFloat("best_" + scene, -1f);
    public static int BestScore(string scene) => PlayerPrefs.GetInt("score_" + scene, -1);

    /// Letter for a score out of 100 — the same scale the report card stamps
    public static string Grade(int score) => score >= 90 ? "S" : score >= 75 ? "A" : score >= 60 ? "B" : score >= 45 ? "C" : "D";

    /// How many levels have a finish time on record
    public static int Cleared
    {
        get
        {
            int n = 0;
            foreach (var l in All) if (BestTime(l.scene) >= 0f) n++;
            return n;
        }
    }

    /// Which chapter a scene belongs to (-1 if none)
    public static int ChapterOf(string scene)
    {
        for (int c = 0; c < Chapters.Length; c++)
            for (int i = 0; i < Chapters[c].count; i++)
                if (All[Chapters[c].first + i].scene == scene) return c;
        return -1;
    }

    /// Which chapter a level index belongs to (-1 if none)
    public static int ChapterOfIndex(int index)
    {
        for (int c = 0; c < Chapters.Length; c++)
            if (index >= Chapters[c].first && index < Chapters[c].first + Chapters[c].count) return c;
        return -1;
    }

    /// FREE PLAY's rule: a level can be launched from its card once its chapter is open (the rule as it was) or once
    /// any run has reached it (RunState.Reached, the high-water mark)
    public static bool LevelOpen(int index)
    {
        if (index < 0 || index >= All.Length) return false;
        return index <= RunState.Reached || ChapterUnlocked(ChapterOfIndex(index));
    }

    /// A chapter is finished once its last level has a finish on record
    public static bool ChapterFinished(int c)
    {
        if (c < 0 || c >= Chapters.Length) return false;
        var ch = Chapters[c];
        return BestTime(All[ch.first + ch.count - 1].scene) >= 0f;
    }

    /// The first chapter is open; every other one opens when the chapter before it is finished, or when a run has
    /// reached it (Momentum -> Unlock All Chapters in the editor sets an override for testing)
    public static bool ChapterUnlocked(int c)
    {
        if (c < 0 || c >= Chapters.Length) return false;
        if (c == 0 || PlayerPrefs.GetInt("unlock_all", 0) == 1) return true;
        return ChapterFinished(c - 1) || Chapters[c].first <= RunState.Reached;
    }

    public static int UnlockedChapters
    {
        get
        {
            int n = 0;
            for (int c = 0; c < Chapters.Length; c++) if (ChapterUnlocked(c)) n++;
            return n;
        }
    }

    /// Levels of a chapter with a finish on record
    public static int ClearedIn(int c)
    {
        if (c < 0 || c >= Chapters.Length) return 0;
        int n = 0;
        var ch = Chapters[c];
        for (int i = 0; i < ch.count; i++) if (BestTime(All[ch.first + i].scene) >= 0f) n++;
        return n;
    }

    /// The chapter's grade: the average of its scored levels' best scores, or null until one is scored
    public static string ChapterGrade(int c)
    {
        if (c < 0 || c >= Chapters.Length) return null;
        var ch = Chapters[c];
        int sum = 0, scored = 0;
        for (int i = 0; i < ch.count; i++)
        {
            int s = BestScore(All[ch.first + i].scene);
            if (s >= 0) { sum += s; scored++; }
        }
        return scored == 0 ? null : Grade(sum / scored);
    }

    // ------------------------------------------------------------ the posters (MenuThumbnails builds these; Posters.Build draws them)

    /// Which poster a level's card carries: the thirteen compositions in PosterActI / PosterActII / PosterActIII
    public enum PosterKind { Sanatorium, TheatreDistrict, BelowStairs, GrandHotels, HalcyonHouse, Metro, Tunnels, Sewers, Foundations, Terminus, Breach, Rooftops, Finale }

    /// ONE dedicated poster per level (user 2026-10-16: "dedicated images for the thumbnails, look to ULTRAKILL"):
    /// key art, not a photograph — a flat cut-out illustration in the level's one hue with its landmark on a third,
    /// its light and a tiny figure, drawn by the composition named here. Replaces the staged live shots.
    public struct PosterInfo
    {
        public string scene;
        public PosterKind composition;

        public PosterInfo(string scene, PosterKind composition)
        {
            this.scene = scene;
            this.composition = composition;
        }
    }

    public static readonly PosterInfo[] Posters =
    {
        new PosterInfo("Level1", PosterKind.Sanatorium),        // sepia dawn: the gate pylons and the Hydro's marquee, a lit tram, the fountain, a bellhop on the steps
        new PosterInfo("Level2", PosterKind.TheatreDistrict),   // violet night: the elevated's deck on its piers with the station and a carriage, a marquee under it, the Regent's blade sign, a moon
        new PosterInfo("Level3", PosterKind.BelowStairs),       // steel blue: an alley canyon, fire escapes up both walls, washing lines, one lamp, steam from a grate, a slot of sky
        new PosterInfo("Level4", PosterKind.GrandHotels),       // sepia gold: two fluted towers and the glass skybridge, searchlight fans, the porte-cochère's lamps, the couple at the rail
        new PosterInfo("Level5", PosterKind.HalcyonHouse),      // cobalt: the twin pylons framing the mark, the arcade's lantern, chandelier bursts, a chequer floor, a doorman
        new PosterInfo("Level6", PosterKind.Metro),             // steel blue and cream tile: the depth dial as a huge gauge, the escalators as bands, a headlamp beam, a doorman's lantern
        new PosterInfo("Level7", PosterKind.Tunnels),           // violet-black: a tunnel bore, the third rail's amber arc, two headlamps with beams, a signal red over amber
        new PosterInfo("Level8", PosterKind.Sewers),            // steel over brick: egg-shaped arches in a row, a weir with the water falling as pale bands, black water, a sluice gate
        new PosterInfo("Level9", PosterKind.Foundations),       // umber: a buried street of shopfronts under a vault, a chasm, columns, a slab tilting, a lantern
        new PosterInfo("Level10", PosterKind.Terminus),         // violet with amber: the clock huge and dark, platforms converging, the locomotive's headlamp and smoke, a red tail lamp
        new PosterInfo("Level11", PosterKind.Breach),           // sepia dawn: the steam column from the open shaft, the fallen clock tower across the street, the viaduct behind
        new PosterInfo("Level12", PosterKind.Rooftops),         // violet night: water towers, parapets and a gothic spire, the police blimp's searchlight, rain, orange windows
        new PosterInfo("Level13", PosterKind.Finale),           // black and steel: the House in worm's-eye with its fluting, searchlight fans from its crown, stars, a figure on a ledge
    };

    /// The level's poster, or the finale's for a scene without one
    public static PosterInfo Poster(string scene)
    {
        foreach (var p in Posters) if (p.scene == scene) return p;
        return new PosterInfo(scene, PosterKind.Finale);
    }
}
