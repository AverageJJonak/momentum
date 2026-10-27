using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// THE CITY BEHIND THE MENU: GOTHAM AS THE ANIMATED SERIES DREW IT (2026-10-16),
/// towers as BLACK SILHOUETTES against a burnt sky, painted on black paper.
/// Halcyon's city is still a PLANNED city: the company's tower, the House,
/// stands in a round plaza at the exact centre of it, twelve avenues run from
/// the plaza to the horizon, and the rings of the plan hold the rest. What
/// stands on the plan is MASSING FIRST: every tower a stack of two to four
/// stepped masses, each step a fifth narrower than the one under it and the
/// top mass tall and thin, under a crown from a small vocabulary (a ziggurat
/// of steps, a finial spire, a drum with a lantern, a gothic crown with four
/// winged gargoyles on its corners, a flat roof with a water tower on stilts,
/// a smokestack pair with a painted column of smoke); the tallest taper in
/// their top storeys and the nearest ring LEANS toward whoever looks at it,
/// so the skyline reads drawn in forced perspective. Fewer, bigger buildings:
/// a few giants, many low blocks. Every facade is near-black stone with a few
/// windows lit in short runs (an office working late) and the rest dark; the
/// string courses and cornices are the lighter bands that let the steps read;
/// brass only on the crowns; neon on five signs in the whole city; one police
/// blimp with a searchlight over it all; the far rings flat black cut-outs.
///   THE COURT      18 Deco shafts round the plaza, no two alike: ziggurats,
///                  pyramid roofs with lanterns, stacked brass crowns, flat
///                  tops under water towers and masts, domed drums, gothic
///                  crowns with gargoyles, twin towers on a podium, civic
///                  blocks behind colonnades, a smokestack pair. The two
///                  flanking the south approach are the tallest. All lean
///                  outward, two degrees
///   THE TERRACES   stepped civic blocks, hotels and clubs round the plaza,
///                  a stone triumphal arch with a statue group over every
///                  avenue where it passes through them.
///   THE BELT       parkland, bronze-ribbed conservatories, a promenade, the
///                  elevated on its masonry viaduct. (This is the camera's own
///                  corridor: it orbits at about 470 m, 40 to 260 m up, so
///                  nothing here stands over 14 m.)
///   BEYOND         the sea: brownstone terraces and brick rows between every
///                  ring, the lots on the rings taken by turns by the low
///                  fabric, by setback shafts and by the giants, the landmarks
///                  where they were set down, elevated railways on stone
///                  viaducts over it all.
/// Light is what is drawn: the windows, the lamps along the avenues, the gilt
/// of the crowns, the marquees and the rooftop names, the traffic on the
/// ground, the trains on the viaducts, the searchlights sweeping the cloud. The
/// House is a Deco cathedral of commerce: charcoal stone between limestone
/// piers, stepping back four times under a crown of brass and a lantern. No
/// neon colour anywhere: amber, cream, gilt, and red only on the aircraft
/// lamps and the odd neon word.
/// Everything still is welded (boxes by MeshBaker, textured quads by
/// SheetBaker): one mesh per material. What LIVES in it is the other half of
/// this class, MenuCityLife, built from the sites this half records.
/// </summary>
public static partial class MenuCity
{
    public const float Extent = 1350f, Plaza = 215f;
    public const float BeltIn = 400f, BeltOut = 545f, RailRadius = 413f, RailY = 22f, OuterRailRadius = 684f, OuterRailY = 44f;
    public const int Avenues = 12;
    /// A bay of the masonry city, and a floor
    public const float Bay = 5.5f, Floor = 3.9f;

    /// A facade: the wall it is cut from, the light laid over it, the blind stone of its party walls, and (if it has
    /// them) the rusticated base of two floors and the attic of one that a tall enough stack wears
    public class Skin { public Material wall, light, blind, baseWall, baseLight, atticWall, atticLight; public bool bands; }

    /// Which faces of a storey-stack are BLIND (party walls, the stair side): bits for the back (-z, toward the House
    /// for a building on its bearing), the front (+z, outward), the left (-x) and the right (+x)
    public const int BlindBack = 1, BlindFront = 2, BlindLeft = 4, BlindRight = 8, BlindSides = 12, BlindAll = 15;

    /// A Court shaft's street face: where a living floor hangs. `outward` faces the orbit
    public struct Site { public Vector3 foot, outward; public float w, d, y0, y1, h; }

    // New lists for every build: a level's vista must not empty the menu's, which its weather still scrolls
    public static List<Site> Heroes { get; private set; } = new List<Site>();
    public static List<Vector3> Beacons { get; private set; } = new List<Vector3>();       // roofs tall enough to need a red light
    public static List<Material> Scrolling { get; private set; } = new List<Material>();   // traffic: the weather scrolls these
    /// WHERE THE PLAN STANDS. The menu builds it round the world origin; a level or a cutscene puts it wherever its
    /// tower is (Init). Every position the plan makes goes through Polar, or adds this by hand.
    public static Vector3 Origin { get; private set; }
    /// How bright the city's own light is: 1 at the menu's night; a level by day dims it
    static float gain = 1f;

    // STONE AND METAL: flat lit colours
    static Material stoneLime, stoneChar, stoneCream, stoneGranite, marble, brickFlat, brownFlat, brass, bronze, iron, roofSlate, dead, foliage, lawn, deck, cutout, oxbloodFlat, smokePaint;
    // THE WALLS, an atlas a kind, AND THEIR LIGHT, laid over them
    static Material wallLime, wallChar, wallCream, wallBrick, wallPierLime, wallPierChar, wallBrownstone, wallGranite, wallHouse;
    static Material wallBlindBrick, wallBlindStone, wallBlindChar, wallSoot, wallOxblood;
    static Material lightRows, lightStrips, lightHomes, lightArches, lightHouse, lightGold, lightRibbons, lightShop, lightAttic;
    static Skin limestone, charcoal, cream, brick, pier, pierDark, arcade, brickArch, brownstone, granite, plain, house, oxbloodSkin;
    // THE GROUND'S LIGHT, THE AIR'S
    static Material promenade, promenadeRing, trafficWarm, trafficHeads, carriages, washAmber, washCream, beam, fanBeam, hazeBank;
    /// The still searchlights' beam material (the three from the ground): the living half swaps them for cones that sweep
    public static Material StillBeam => beam;
    // LAMPS AND GILT: unlit, they carry the picture
    static Material glowWarm, glowWhite, glowGold, glowRed, glowSearch, domeGlow;

    public static Material Ground { get; private set; }

    // Render queues, low to high. Everything see-through in the city is ORDERED BY HAND: they are huge welded
    // meshes, and Unity sorts see-through things by the middle of their bounds, which for a city is the same point.
    public const int QMirror = 2880, QGround = 2900, QStreet = 2910, QTraffic = 2920, QWindows = 2950, QWash = 2954, QCloudHigh = 2960, QFlash = 2970, QCloudLow = 2980;

    static Material Wall(Texture tex, string name, Color tint, float smoothness)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", tint);
        m.SetFloat("_Smoothness", smoothness);
        m.SetInt("_Cull", (int)CullMode.Off);   // a welded wall is never seen from behind; this makes its winding not matter
        return m;
    }

    static Material Wall(int kind, Color tint, float smoothness) => Wall(CityTextures.WallAtlas(kind), "CityWall" + kind, tint, Mathf.Min(smoothness, 0.15f));   // matte: a painted mass has no gloss

    /// A see-through unlit sheet carrying a texture: light on a facade, a promenade, traffic, a wash. Off the keyword template.
    public static Material Sheet(Texture tex, Color tint, int queue, bool additive = false)
    {
        var m = LevelKit.FromTemplate("UnlitTransparent", "Universal Render Pipeline/Unlit");
        m.name = "CitySheet" + queue;
        if (tex != null) m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", tint);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", additive ? 2f : 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", additive ? (int)BlendMode.One : (int)BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.SetInt("_Cull", (int)CullMode.Off);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = queue;
        return m;
    }

    /// A flat stone: matte whatever smoothness is asked for (the painted masses carry no gloss and no bump: one lit
    /// face, the rest black)
    static Material Flat(Color c, float smoothness)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "CityFlat" };
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", Mathf.Min(smoothness, 0.18f));
        m.SetInt("_Cull", (int)CullMode.Off);
        return m;
    }

    /// Brass, bronze, iron: a flat colour that takes the sky as metal does
    static Material Metal(Color c, float smoothness)
    {
        var m = Flat(c, smoothness);
        m.name = "CityMetal";
        m.SetFloat("_Metallic", 0.85f);
        return m;
    }

    static Material Glow(Color c)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "CityGlow" };
        m.SetColor("_BaseColor", c);
        m.SetInt("_Cull", (int)CullMode.Off);
        return m;
    }

    /// A light, dimmed to the gain (a lit sheet keeps its alpha); a glow never goes below a trim's brightness
    static Color L(Color c) => new Color(c.r * gain, c.g * gain, c.b * gain, c.a);
    static Color G(Color c) { float g = Mathf.Max(gain, 0.4f); return new Color(c.r * g, c.g * g, c.b * g, c.a); }

    /// How much further the window sheets float off the walls per metre from the origin: the menu's depth needs it;
    /// a level whose towers are walked past at arm's length sets it to zero after Init
    public static float ProudGrowth = 0.00025f;
    /// How far off the wall the window sheets sit at all: the menu's software depth wants a hand's width; a level
    /// walked past at arm's length sets a few centimetres, or the panes read as slats floating off the wall
    public static float ProudBase = 0.14f;

    /// By day the plan is the same city in SEPIA daylight: the stone warm and muted, the windows barely lit, the
    /// parks olive. Init(..., day: true) asks for it; the menu's storm and the night levels do not
    public static bool Day { get; private set; }

    /// THE RED STREET: one district's far fabric (the rings beyond the plan, FarRing) in flat matte oxblood with small
    /// dark windows, under the lighting's violet sky and full moon. A level that wants it sets this before it raises
    /// the city and clears it after (CityVista does, for one level)
    public static bool RedFabric;

    /// `lightGain` scales every lit sheet and glow (1 at night; a level by day dims them); `wet` is the menu's
    /// see-through mirrored ground, a level or a cutscene stands the city on dry stone instead; `origin` is where
    /// the House stands
    public static void Init(float lightGain = 1f, bool wet = true, Vector3 origin = default, bool day = false)
    {
        Day = day;
        ProudGrowth = 0.00025f;
        ProudBase = 0.14f;
        Heroes = new List<Site>();
        Beacons = new List<Vector3>();
        Scrolling = new List<Material>();
        Origin = origin;
        NoReserve();
        LeanOff();
        gain = Mathf.Max(0.05f, lightGain);

        // The stone: the facades go to black paper (their atlases are near-black, CityTextures); the flat stone here
        // is the cornices, the string courses, the quoins and the trims, a shade LIGHTER than the walls they sit on,
        // so every setback reads as a band. Mid-dark, never black: the grade crushes the darks and wants a ramp
        stoneLime = Flat(day ? new Color(0.5f, 0.46f, 0.39f) : new Color(0.36f, 0.34f, 0.3f), 0.3f);
        stoneChar = Flat(new Color(0.13f, 0.13f, 0.15f), 0.4f);
        stoneCream = Flat(day ? new Color(0.64f, 0.58f, 0.47f) : new Color(0.5f, 0.46f, 0.38f), 0.35f);
        stoneGranite = Flat(new Color(0.17f, 0.17f, 0.19f), 0.6f);
        marble = Flat(new Color(0.05f, 0.05f, 0.06f), 0.8f);
        brickFlat = Flat(new Color(0.26f, 0.13f, 0.1f), 0.2f);
        brownFlat = Flat(new Color(0.24f, 0.14f, 0.1f), 0.25f);
        brass = Metal(new Color(0.78f, 0.58f, 0.25f), 0.7f);
        bronze = Metal(new Color(0.35f, 0.24f, 0.12f), 0.6f);
        iron = Metal(new Color(0.16f, 0.16f, 0.18f), 0.5f);
        roofSlate = Flat(new Color(0.075f, 0.075f, 0.09f), 0.45f);
        deck = iron;
        dead = Flat(new Color(0.07f, 0.065f, 0.07f), 0.12f);
        foliage = Flat(day ? new Color(0.25f, 0.22f, 0.12f) : new Color(0.06f, 0.055f, 0.04f), 0.2f);
        lawn = Flat(day ? new Color(0.3f, 0.27f, 0.14f) : new Color(0.05f, 0.045f, 0.03f), 0.15f);
        cutout = Flat(day ? new Color(0.46f, 0.42f, 0.37f) : new Color(0.055f, 0.055f, 0.07f), 0.1f);   // the far rings: flat silhouettes
        oxbloodFlat = Flat(new Color(0.3f, 0.075f, 0.07f), 0.05f);                                      // the red street's far masses: matte crimson
        smokePaint = Flat(day ? new Color(0.42f, 0.4f, 0.4f) : new Color(0.17f, 0.165f, 0.18f), 0.05f);   // a column of smoke, painted

        // By day the atlases (cut dark for the night) are lifted back toward sepia daylight
        Color sepia = day ? new Color(1.45f, 1.36f, 1.2f) : Color.white;
        wallLime = Wall(CityTextures.KindLime, sepia, 0.3f);
        wallChar = Wall(CityTextures.KindChar, sepia, 0.4f);
        wallCream = Wall(CityTextures.KindCream, sepia, 0.35f);
        wallHouse = Wall(CityTextures.KindHouse, sepia, 0.45f);
        wallBrick = Wall(CityTextures.KindBrick, sepia, 0.2f);
        wallPierLime = Wall(CityTextures.KindPierLime, sepia, 0.3f);
        wallPierChar = Wall(CityTextures.KindPierChar, sepia, 0.4f);
        wallBrownstone = Wall(CityTextures.KindBrownstone, sepia, 0.25f);
        wallGranite = Wall(CityTextures.KindGranite, sepia, 0.6f);
        wallBlindBrick = Wall(CityTextures.KindBlindBrick, sepia, 0.2f);
        wallBlindStone = Wall(CityTextures.KindBlindStone, sepia, 0.3f);
        wallBlindChar = Wall(CityTextures.KindBlindChar, sepia, 0.4f);
        wallSoot = Wall(CityTextures.KindSoot, sepia, 0.1f);
        wallOxblood = Wall(CityTextures.KindOxblood, sepia, 0.05f);   // the red street's masses: flat, matte, never a glow

        // The windows: amber, incandescent, few. By day they are nearly out
        lightRows = Sheet(CityTextures.Windows(CityTextures.Rows), L(day ? new Color(0.5f, 0.42f, 0.3f) : new Color(1.5f, 1.15f, 0.7f)), QWindows);
        lightStrips = Sheet(CityTextures.Windows(CityTextures.Strip), L(day ? new Color(0.5f, 0.42f, 0.3f) : new Color(1.55f, 1.2f, 0.75f)), QWindows);
        lightHomes = Sheet(CityTextures.Windows(CityTextures.Homes), L(day ? new Color(0.45f, 0.38f, 0.28f) : new Color(1.35f, 1f, 0.6f)), QWindows);
        lightArches = Sheet(CityTextures.Windows(CityTextures.Arch), L(day ? new Color(0.55f, 0.48f, 0.36f) : new Color(1.5f, 1.32f, 0.95f)), QWindows);
        lightHouse = Sheet(CityTextures.Windows(CityTextures.House), L(day ? new Color(0.5f, 0.42f, 0.3f) : new Color(1.5f, 1.2f, 0.75f)), QWindows);
        lightGold = Sheet(CityTextures.Windows(CityTextures.Arch), L(new Color(2f, 1.55f, 0.7f)), QWindows);   // the company's top floors: always lit, and gold
        lightShop = Sheet(CityTextures.BandLight(false), L(day ? new Color(0.55f, 0.48f, 0.36f) : new Color(1.5f, 1.3f, 0.9f)), QWindows);   // the shopfronts and lobbies: cream-gold
        lightAttic = Sheet(CityTextures.BandLight(true), L(day ? new Color(0.45f, 0.38f, 0.28f) : new Color(1.3f, 0.95f, 0.55f)), QWindows);
        lightRibbons = lightRows;   // the living floors (MenuCityLife) hang a band of this

        promenade = Sheet(CityTextures.Promenade(), L(new Color(1.25f, 1.08f, 0.8f)), QStreet);        // the avenues: lamp cream
        promenadeRing = Sheet(CityTextures.Promenade(), L(new Color(1.2f, 1f, 0.7f)), QStreet);       // the rings: amber, dimmer
        trafficWarm = Sheet(CityTextures.Traffic(), L(new Color(1.9f, 0.85f, 0.35f)), QTraffic);       // going away: tail lamps
        trafficHeads = Sheet(CityTextures.Traffic(), L(new Color(1.8f, 1.65f, 1.3f)), QTraffic);       // coming: warm white heads
        carriages = Sheet(CityTextures.Carriages(), L(new Color(1.8f, 1.3f, 0.75f)), QTraffic);        // the elevated's lit carriages
        washAmber = Sheet(CityTextures.Wash(), L(new Color(1f, 0.72f, 0.38f, 0.28f)), QWash);
        washCream = Sheet(CityTextures.Wash(), L(new Color(1f, 0.9f, 0.7f, 0.22f)), QWash);
        beam = Sheet(CityTextures.Beam(), L(new Color(1.8f, 1.9f, 2.2f, 0.5f)), (int)RenderQueue.Transparent, true);   // a searchlight: the one cold light, ADDITIVE and over white so the bloom takes it as light (2026-10-25)
        fanBeam = Sheet(CityTextures.Beam(), L(new Color(0.55f, 0.6f, 0.7f, 0.36f)), (int)RenderQueue.Transparent, true);   // a crown's fan of thin beams, additive
        // The fog lying between the ranks: lighter than the black masses it lies against, a shade under the sky's glow
        hazeBank = Sheet(CityTextures.HazeBank(), day ? new Color(0.66f, 0.62f, 0.56f, 0.4f) : new Color(0.38f, 0.31f, 0.35f, 0.5f), QWash + 2);
        Scrolling.Add(trafficWarm);
        Scrolling.Add(trafficHeads);
        Scrolling.Add(carriages);

        glowWarm = Glow(G(new Color(1.6f, 1.15f, 0.6f)));     // a lamp's amber
        glowWhite = Glow(G(new Color(1.7f, 1.55f, 1.25f)));   // lamp cream
        glowGold = Glow(G(new Color(1.8f, 1.42f, 0.6f)));     // gilt, lit
        glowRed = Glow(G(new Color(2.2f, 0.25f, 0.12f)));     // an aircraft lamp, the odd neon word
        glowSearch = Glow(G(new Color(1.6f, 1.7f, 1.85f)));   // a searchlight's lamp
        domeGlow = Glow(G(new Color(0.85f, 0.72f, 0.5f)));    // a conservatory, lit from inside
        DenseInit();

        // The skins. A shaft kind carries its base and attic bands; the rows and the landmarks' arcades do not
        Skin Shaft(Material wall, Material light, Material blind, int kind)
            => new Skin { wall = wall, light = light, blind = blind, bands = true,
                baseWall = Wall(CityTextures.WallBand(kind, false), "CityBase" + kind, sepia, 0.5f), baseLight = lightShop,
                atticWall = Wall(CityTextures.WallBand(kind, true), "CityAttic" + kind, sepia, 0.3f), atticLight = lightAttic };
        limestone = Shaft(wallLime, lightRows, wallBlindStone, CityTextures.KindLime);
        charcoal = Shaft(wallChar, lightRows, wallBlindChar, CityTextures.KindChar);
        cream = Shaft(wallCream, lightRows, wallBlindStone, CityTextures.KindCream);
        brick = Shaft(wallBrick, lightHomes, wallBlindBrick, CityTextures.KindBrick);
        pier = Shaft(wallPierLime, lightStrips, wallBlindStone, CityTextures.KindPierLime);
        pierDark = Shaft(wallPierChar, lightStrips, wallBlindChar, CityTextures.KindPierChar);
        granite = Shaft(wallGranite, lightRows, wallBlindChar, CityTextures.KindGranite);
        arcade = new Skin { wall = wallLime, light = lightArches, blind = wallBlindStone };
        brickArch = new Skin { wall = wallBrick, light = lightArches, blind = wallBlindBrick };
        brownstone = new Skin { wall = wallBrownstone, light = lightHomes, blind = wallBlindBrick };
        plain = new Skin { wall = wallChar, blind = wallBlindChar };   // no light on it at all: the outer city's dark ones
        house = new Skin { wall = wallHouse, light = lightHouse, blind = wallBlindChar };
        oxbloodSkin = new Skin { wall = wallOxblood, blind = wallOxblood };   // small dark windows, no light: the red street
        NightInit();   // the streets' materials (MenuCityNight)
        DecoInit();    // the Deco dressing's (MenuCityDeco)
        GothamInit();  // the sign budget and the fan sites (MenuCityGotham)

        // The ground: wet black stone. It is see-through, and the city stands on it twice, once the right way up and
        // once mirrored underneath (MenuBackdrop), so the whole plan shines. Its alpha is how much of it is stone.
        if (wet)
        {
            Ground = LevelKit.FromTemplate("LitTransparent", "Universal Render Pipeline/Lit");
            Ground.name = "CityWetGround";
            Ground.SetTexture("_BaseMap", CityTextures.Wet());
            Ground.SetColor("_BaseColor", new Color(0.075f, 0.075f, 0.085f, 0.66f));
            Ground.SetFloat("_Smoothness", 0.93f);
            Ground.SetFloat("_Metallic", 0f);
            Ground.renderQueue = QGround;
        }
        else
        {
            // Dry, by day: sepia stone in the same paving, nothing mirrored under it
            Ground = Flat(new Color(0.5f, 0.47f, 0.42f), 0.4f);
            Ground.name = "CityStoneGround";
            Ground.SetTexture("_BaseMap", CityTextures.Wet());
        }
    }

    // ---------------------------------------------------------------- the compass

    /// A point `r` out from the tower on bearing `az` (degrees from north, +z, toward east, +x)
    public static Vector3 Polar(float r, float az, float y = 0f)
    {
        float a = az * Mathf.Deg2Rad;
        return Origin + new Vector3(Mathf.Sin(a) * r, y, Mathf.Cos(a) * r);
    }

    /// The same as a direction (an offset, not a place): `r` along bearing `az`
    public static Vector3 Radial(float r, float az)
    {
        float a = az * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r);
    }

    // ---------------------------------------------------------------- the reserve

    // A box (world coordinates) that nothing of the plan is raised in: where a level stands when the city is its vista
    static float keepX0, keepX1, keepZ0, keepZ1;
    static bool keeping;

    /// Nothing of the plan is built inside this box: a level stands there, and its own walls and windows are the view
    public static void Reserve(float x0, float x1, float z0, float z1)
    {
        keepX0 = x0; keepX1 = x1; keepZ0 = z0; keepZ1 = z1;
        keeping = true;
    }

    public static void NoReserve() => keeping = false;

    /// Does a footprint `half` metres round `foot` reach into the reserve?
    static bool Cut(Vector3 foot, float half)
        => keeping && foot.x + half > keepX0 && foot.x - half < keepX1 && foot.z + half > keepZ0 && foot.z - half < keepZ1;

    /// Does a piece from a to b pass through it (either end, or the middle)?
    static bool CutSeg(Vector3 a, Vector3 b) => Cut(a, 0f) || Cut(b, 0f) || Cut((a + b) * 0.5f, 0f);

    /// How far a bearing is from the nearest avenue, in degrees
    static float OffAvenue(float az)
    {
        float sector = 360f / Avenues;
        return Mathf.Abs(Mathf.DeltaAngle(Mathf.Round(az / sector) * sector, az));
    }

    // ---------------------------------------------------------------- the lean

    // THE LEAN: the forced perspective of the drawn skyline. While it is on, every box (Box) and every sheet
    // (SheetBaker.Shear) of a building is pushed `leanTan` metres sideways along `leanDir` per metre it stands over
    // `leanBase`, and every box is tilted to match, so the whole building leans as one. Only the plan's own rings use
    // it (the Court outward, the first ring inward); a level's towers (OuterTower) are built with it off, and nothing
    // a player walks on ever leans
    static Vector3 leanDir;
    static float leanTan, leanBase;
    static Quaternion leanTilt = Quaternion.identity;

    static void LeanOn(Vector3 dir, float degrees, float baseY)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f || degrees == 0f) { LeanOff(); return; }
        leanDir = dir.normalized;
        leanTan = Mathf.Tan(degrees * Mathf.Deg2Rad);
        leanBase = baseY;
        leanTilt = Quaternion.AngleAxis(degrees, Vector3.Cross(Vector3.up, leanDir));   // up, tilted toward leanDir
        SheetBaker.Shear = leanDir * leanTan;
        SheetBaker.ShearBase = baseY;
    }

    static void LeanOff()
    {
        leanTan = 0f;
        leanTilt = Quaternion.identity;
        SheetBaker.Shear = Vector3.zero;
    }

    /// Every box of the city goes through here: the lean, when it is on, and nothing else
    static void Box(MeshBaker b, Material m, Vector3 pos, Vector3 size, Quaternion? rot = null, PrimitiveType type = PrimitiveType.Cube)
    {
        if (leanTan != 0f)
        {
            float dy = pos.y - leanBase;
            if (dy > 0f) pos += leanDir * (dy * leanTan);
            rot = leanTilt * (rot ?? Quaternion.identity);
        }
        b.Add(m, pos, size, rot, type);
    }

    // ---------------------------------------------------------------- pieces

    /// One storey-stack: four walls cut from the skin's atlas, the light over them from the SAME corner of the same
    /// layout (so the light lights only where the stone has a window), a roof. `w` is across the building's own x, `d`
    /// along its z; `yaw` turns it (a building on bearing az, turned by az, faces outward with +z). `blind` names the
    /// faces that are party walls (no window, no light). A stack with six floors or more that stands on the `ground`
    /// wears the skin's two-floor base (rusticated, arched shopfronts); the `top` one wears its attic of small squares
    static void Tier(SheetBaker sheets, Vector3 foot, float w, float d, float h, Skin skin, bool lights, float yaw = 0f, int blind = 0, bool ground = false, bool top = false)
        => Tier(sheets, foot, w, d, h, skin, lights, yaw, blind, ground, top, 0f);

    /// The same with a TAPER: the top of the stack `taper` (a fraction) narrower than its foot on every side, the
    /// walls leaning in to meet it. The tallest towers' top storeys wear it, so the skyline reads drawn from below.
    /// Zero is the plain stack, box for box what it always was (the envelopes RoofKit builds to never taper)
    static void Tier(SheetBaker sheets, Vector3 foot, float w, float d, float h, Skin skin, bool lights, float yaw, int blind, bool ground, bool top, float taper)
    {
        if (w < 3f || d < 3f || h < 2f) return;
        if (Cut(foot, Mathf.Max(w, d) * 0.5f)) return;
        Quaternion turn = Quaternion.Euler(0f, yaw, 0f);
        int ny = Mathf.Max(1, Mathf.RoundToInt(h / Floor));
        bool bands = skin.bands && ny >= 6 && skin.baseWall != null;
        int nBase = ground && bands ? 2 : 0, nAttic = top && bands ? 1 : 0;
        float baseH = nBase * Floor, atticH = nAttic * Floor, shaftH = h - baseH - atticH;
        int nShaft = Mathf.Max(1, ny - nBase - nAttic);
        float k = 1f / CityTextures.AtlasCells, kb = 1f / CityTextures.BandCells;
        float proud = ProudBase + new Vector2(foot.x - Origin.x, foot.z - Origin.z).magnitude * ProudGrowth;   // further out, further off the wall: depth gets coarse with distance
        float tk = Mathf.Clamp01(taper);
        for (int f = 0; f < 4; f++)
        {
            Vector3 n = turn * (f == 0 ? Vector3.back : f == 1 ? Vector3.forward : f == 2 ? Vector3.left : Vector3.right);
            bool alongX = f < 2;
            float width = alongX ? w : d, half = alongX ? d * 0.5f : w * 0.5f;
            int cells = Mathf.Max(1, Mathf.RoundToInt(width / Bay));
            int ou = Random.Range(0, CityTextures.AtlasCells), ov = Random.Range(0, CityTextures.AtlasCells);
            Vector3 face = foot + n * half;
            if (tk > 0f)
            {
                // A tapered stack: four trapezoids (and the bands it would wear are left off: a crown wears none)
                Vector3 r = Vector3.Cross(n, Vector3.up);
                Vector3 bl = face - r * (width * 0.5f), br = face + r * (width * 0.5f);
                Vector3 topC = foot + Vector3.up * h + n * (half * (1f - tk));
                Vector3 tl = topC - r * (width * 0.5f * (1f - tk)), tr = topC + r * (width * 0.5f * (1f - tk));
                Vector2 tuv0 = new Vector2(ou * k, ov * k), tuv1 = new Vector2((ou + cells) * k, (ov + ny) * k);
                bool blindFace = (blind & (1 << f)) != 0 && skin.blind != null;
                sheets.Quad(blindFace ? skin.blind : skin.wall, bl, tl, tr, br, tuv0, tuv1);
                if (!blindFace && lights && skin.light != null) sheets.Quad(skin.light, bl + n * proud, tl + n * proud, tr + n * proud, br + n * proud, tuv0, tuv1);
                continue;
            }
            if ((blind & (1 << f)) != 0 && skin.blind != null)
            {
                sheets.Wall(skin.blind, face + Vector3.up * (h * 0.5f), n, width, h, new Vector2(ou * k, ov * k), new Vector2((ou + cells) * k, (ov + ny) * k));
                continue;
            }
            Vector2 uv0 = new Vector2(ou * k, ov * k), uv1 = new Vector2((ou + cells) * k, (ov + nShaft) * k);
            Vector3 c = face + Vector3.up * (baseH + shaftH * 0.5f);
            sheets.Wall(skin.wall, c, n, width, shaftH, uv0, uv1);
            if (lights && skin.light != null) sheets.Wall(skin.light, c + n * proud, n, width, shaftH, uv0, uv1);
            if (nBase > 0)
            {
                int bu = Random.Range(0, CityTextures.BandCells);
                Vector2 b0 = new Vector2(bu * kb, 0f), b1 = new Vector2((bu + cells) * kb, 1f);
                Vector3 cb = face + Vector3.up * (baseH * 0.5f);
                sheets.Wall(skin.baseWall, cb, n, width, baseH, b0, b1);
                if (lights && skin.baseLight != null) sheets.Wall(skin.baseLight, cb + n * proud, n, width, baseH, b0, b1);
            }
            if (nAttic > 0)
            {
                int au = Random.Range(0, CityTextures.BandCells);
                Vector2 a0 = new Vector2(au * kb, 0f), a1 = new Vector2((au + cells) * kb, 1f);
                Vector3 ca = face + Vector3.up * (h - atticH * 0.5f);
                sheets.Wall(skin.atticWall, ca, n, width, atticH, a0, a1);
                if (lights && skin.atticLight != null) sheets.Wall(skin.atticLight, ca + n * proud, n, width, atticH, a0, a1);
            }
        }
        // The roof: one texel of the wall's own stone, dark
        Vector3 roofC = foot + Vector3.up * h, rx = turn * Vector3.right * (w * 0.5f * (1f - tk)), rz = turn * Vector3.forward * (d * 0.5f * (1f - tk));
        sheets.Quad(skin.wall, roofC - rx - rz, roofC - rx + rz, roofC + rx + rz, roofC + rx - rz, new Vector2(0.001f, 0.001f), new Vector2(0.002f, 0.002f));
    }

    /// A round storey-stack: the same, bent
    static void RoundTier(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float diameter, float h, Skin skin, bool lights)
    {
        if (Cut(foot, diameter * 0.5f)) return;
        const int Seg = 20;
        float r = diameter * 0.5f;
        int around = Mathf.Max(Seg, Mathf.RoundToInt(Mathf.PI * diameter / Bay) / Seg * Seg), ny = Mathf.Max(1, Mathf.RoundToInt(h / Floor));
        int per = around / Seg;
        float proud = ProudBase + 0.02f + new Vector2(foot.x - Origin.x, foot.z - Origin.z).magnitude * ProudGrowth;
        int ou = Random.Range(0, CityTextures.AtlasCells), ov = Random.Range(0, CityTextures.AtlasCells);
        float k = 1f / CityTextures.AtlasCells;
        for (int i = 0; i < Seg; i++)
        {
            float a0 = i * Mathf.PI * 2f / Seg, a1 = (i + 1) * Mathf.PI * 2f / Seg;
            Vector3 d0 = new Vector3(Mathf.Sin(a0), 0f, Mathf.Cos(a0)), d1 = new Vector3(Mathf.Sin(a1), 0f, Mathf.Cos(a1));
            Vector3 up = Vector3.up * h;
            Vector2 uv0 = new Vector2((ou + i * per) * k, ov * k), uv1 = new Vector2((ou + (i + 1) * per) * k, (ov + ny) * k);
            sheets.Quad(skin.wall, foot + d1 * r, foot + d1 * r + up, foot + d0 * r + up, foot + d0 * r, uv0, uv1);
            if (!lights || skin.light == null) continue;
            float rr = r + proud;
            sheets.Quad(skin.light, foot + d1 * rr, foot + d1 * rr + up, foot + d0 * rr + up, foot + d0 * rr, uv0, uv1);
        }
        Box(boxes, roofSlate, foot + Vector3.up * (h - 0.2f), new Vector3(diameter * 0.995f, 0.25f, diameter * 0.995f), null, PrimitiveType.Cylinder);
    }

    /// A STEPPED CORNICE round the top of a storey-stack: ledges each wider than the one under it, and if asked a
    /// lit lip along the top ledge's edge. One box a ledge, whatever the plan
    static void Cornice(MeshBaker boxes, Vector3 top, float w, float d, float az, Material m, Material lip = null, int steps = 3)
    {
        if (Cut(top, Mathf.Max(w, d) * 0.5f + 2f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        float y = 0f, o = 0f;
        for (int i = 0; i < steps; i++)
        {
            o = 0.5f + 0.55f * i;
            float th = i == steps - 1 ? 0.5f : 0.4f;
            Box(boxes, m, top + Vector3.up * (y + th * 0.5f), new Vector3(w + o * 2f, th, d + o * 2f), turn);
            y += th;
        }
        if (lip != null) Box(boxes, lip, top + Vector3.up * (y - 0.1f), new Vector3(w + o * 2f + 0.3f, 0.16f, d + o * 2f + 0.3f), turn);
    }

    /// QUOINS: a lighter stone post up every corner of a storey-stack, a hand proud of the wall
    static void Quoins(MeshBaker boxes, Vector3 foot, float w, float d, float h, float az, Material m)
    {
        if (Cut(foot, Mathf.Max(w, d) * 0.5f + 1f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        for (int c = 0; c < 4; c++)
        {
            float sx = c % 2 == 0 ? -1f : 1f, sz = c < 2 ? -1f : 1f;
            Box(boxes, m, foot + Vector3.up * (h * 0.5f) + turn * new Vector3(sx * (w * 0.5f - 0.3f), 0f, sz * (d * 0.5f - 0.3f)), new Vector3(1f, h, 1f), turn);
        }
    }

    /// FLUTING: vertical ribs up all four faces of a storey-stack, `pitch` apart, standing `rib` proud of the wall
    /// (the window sheets sit behind them, so the light shows between the ribs). For crowns and the House: a rib
    /// is a box, and a whole city of ribbed shafts would be a million of them. Ordinary shafts are ribbed in their
    /// wall texture instead
    static void Fluting(MeshBaker boxes, Vector3 foot, float w, float d, float h, float az, Material m, float pitch = 2.4f, float rib = 0.5f, float ribW = 0f)
    {
        if (Cut(foot, Mathf.Max(w, d) * 0.5f + 1f)) return;
        if (ribW <= 0f) ribW = rib;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        for (int f = 0; f < 4; f++)
        {
            bool alongX = f < 2;
            Vector3 n = turn * (f == 0 ? Vector3.back : f == 1 ? Vector3.forward : f == 2 ? Vector3.left : Vector3.right);
            Vector3 along = turn * (alongX ? Vector3.right : Vector3.forward);
            float width = alongX ? w : d, half = (alongX ? d : w) * 0.5f;
            int count = Mathf.Max(2, Mathf.RoundToInt(width / pitch));
            for (int i = 0; i < count; i++)
            {
                float t = -width * 0.5f + width * (i + 0.5f) / count;
                Box(boxes, m, foot + Vector3.up * (h * 0.5f) + n * (half + rib * 0.5f - 0.05f) + along * t, new Vector3(alongX ? ribW : rib, h, alongX ? rib : ribW), turn);
            }
        }
    }

    /// FULL-HEIGHT FLUTED PIERS, the worm's-eye look of the nearest towers: slabs a metre and a half wide standing
    /// half a metre proud of every face every four metres, the stone a shade lighter than the wall so the raking
    /// light catches them. Never on the envelopes a level runs on (those are fluted in their wall texture only)
    static void FlutedPiers(MeshBaker boxes, Vector3 foot, float w, float d, float h, float az, Material m)
        => Fluting(boxes, foot, w, d, h, az, m, 4f, 0.5f, 1.5f);

    /// A GALLERY round a round tower: a stone deck with a stone rim, bronze posts round its edge and a lamp on four of
    /// them. (The plan's round towers carried lit rings here; the decks stay where they were, in stone)
    static void Gallery(MeshBaker boxes, Vector3 centre, float diameter)
    {
        if (Cut(centre, diameter * 0.5f)) return;
        Box(boxes, stoneLime, centre, new Vector3(diameter, 0.22f, diameter), null, PrimitiveType.Cylinder);
        Box(boxes, stoneLime, centre + Vector3.up * 0.12f, new Vector3(diameter - 2.2f, 0.4f, diameter - 2.2f), null, PrimitiveType.Cylinder);
        for (int k = 0; k < 8; k++)
        {
            Vector3 p = centre + Radial(diameter * 0.5f - 0.5f, k * 45f + 22.5f);
            Box(boxes, bronze, p + Vector3.up * 0.9f, new Vector3(0.3f, 1.2f, 0.3f), Quaternion.Euler(0f, k * 45f, 0f));
            if (k % 2 == 0) Box(boxes, glowWhite, p + Vector3.up * 1.8f, Vector3.one * 0.7f, null, PrimitiveType.Sphere);
        }
    }

    /// A wash of lamplight up the foot of one face
    static void WashUp(SheetBaker sheets, Vector3 foot, Vector3 n, float half, float width, float height, Material m)
    {
        if (Cut(foot, half + width * 0.5f)) return;
        sheets.Wall(m, foot + Vector3.up * (height * 0.5f) + n * (half + 0.5f), n, width, height, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f));
    }

    /// Which faces of a free-standing tower are blind: a slab's narrow ends, else one side by its seed
    static int TowerBlind(float w, float d, int seed)
    {
        if (d < w * 0.75f) return BlindSides;
        if (w < d * 0.75f) return BlindBack | BlindFront;
        return ((seed % 2) + 2) % 2 == 0 ? BlindLeft : BlindRight;
    }

    // ---------------------------------------------------------------- the towers

    /// A SPIRE: the Deco shaft. Five storey-stacks stepping back (the envelope RoofKit builds its roofs to), the
    /// windows punched between fluted piers, a stepped cornice at every setback, gargoyles on the first two, the
    /// crown tier ribbed in gilt, a stepped spire with a finial, the needle over that with a red lamp.
    /// The Court's are heroes: their street face is recorded for dressing
    static void Spire(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, Skin skin, float az, Material crown, bool hero)
    {
        if (Cut(foot, Mathf.Max(w, d) * 0.5f)) return;
        if (!hero) Podium(boxes, sheets, foot, w, d, az);
        Material stone = skin == pierDark || skin == charcoal ? stoneChar : stoneLime;
        int blind = hero ? 0 : (Mathf.RoundToInt(foot.x * 0.3f + foot.z * 0.7f) % 2 == 0 ? BlindLeft : BlindRight);
        float[] part = { 0.38f, 0.24f, 0.17f, 0.12f, 0.09f };
        float y = 0f, cw = w, cd = d;
        for (int i = 0; i < part.Length; i++)
        {
            float th = h * part[i];
            Tier(sheets, foot + Vector3.up * y, cw, cd, th, skin, true, az, blind, i == 0 && !hero, i == part.Length - 1);
            y += th;
            Cornice(boxes, foot + Vector3.up * y, cw, cd, az, stone, i >= 3 ? crown : null);
            if (i < 2) Gargoyles(boxes, foot + Vector3.up * y, cw, cd, az, stone);
            cw *= 0.8f; cd *= 0.8f;
        }
        float tw = cw / 0.8f, td = cd / 0.8f;   // the top tier's own footprint (cw and cd are the next step in)
        Fluting(boxes, foot + Vector3.up * (y - h * part[4]), tw, td, h * part[4], az, crown, 2.6f, 0.45f);   // the crown: gilt ribs, lit
        RoofFurniture(boxes, foot + Vector3.up * y, tw, td, az, Mathf.RoundToInt(foot.x * 0.7f + foot.z * 0.3f), hero ? 0 : 1);
        CrownSpire(boxes, foot + Vector3.up * (y + 1.3f), Mathf.Min(tw, td) * 0.36f, h * 0.12f, stone, crown);
        float needle = h * 0.16f;
        Box(boxes, iron, foot + Vector3.up * (h + needle * 0.5f), new Vector3(1.1f, needle, 1.1f));
        Box(boxes, glowGold, foot + Vector3.up * (h + needle), Vector3.one * 2.4f, null, PrimitiveType.Sphere);
        if (h + needle > 230f) Beacons.Add(foot + Vector3.up * (h + needle + 2.5f));
        if (hero) Heroes.Add(new Site { foot = foot, outward = Radial(1f, az), w = w, d = d, y0 = 0f, y1 = h * part[0], h = h + needle });
    }

    /// A ROUND TOWER under an observatory dome: two drums of arched windows, stone galleries round it with bronze
    /// posts and lamps, a bronze dome under a gilt ring and a finial, a lantern gallery on columns over the dome.
    /// (The galleries at 0.45 h, 0.72 h and h + 16 are the envelope RoofKit runs on; they stay where they are)
    static void Rotunda(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float diameter, float h, Skin skin, Material rim)
    {
        if (Cut(foot, diameter * 0.8f)) return;
        RoundTier(boxes, sheets, foot, diameter, h * 0.72f, skin, true);
        RoundTier(boxes, sheets, foot + Vector3.up * (h * 0.72f), diameter * 0.7f, h * 0.28f, skin, true);
        RoofFurniture(boxes, foot + Vector3.up * h, diameter * 0.5f, diameter * 0.5f, 0f, Mathf.RoundToInt(foot.x + foot.z * 0.2f), 1);
        Box(boxes, bronze, foot + Vector3.up * h, Vector3.one * (diameter * 0.68f), null, PrimitiveType.Sphere);   // the dome
        Box(boxes, brass, foot + Vector3.up * (h + 0.3f), new Vector3(diameter * 0.7f, 0.3f, diameter * 0.7f), null, PrimitiveType.Cylinder);   // brass at its foot
        Gallery(boxes, foot + Vector3.up * (h * 0.45f), diameter * 1.55f);
        Gallery(boxes, foot + Vector3.up * (h * 0.72f), diameter * 1.3f);
        Gallery(boxes, foot + Vector3.up * (h + 16f), diameter * 0.9f);   // the lantern gallery, on its columns
        for (int k = 0; k < 8; k++) Box(boxes, stoneLime, foot + Radial(diameter * 0.42f, k * 45f + 22.5f) + Vector3.up * (h + 10.5f), new Vector3(0.9f, 11f, 0.9f));
        Box(boxes, glowWhite, foot + Vector3.up * (h + 17.8f), new Vector3(2.6f, 2.4f, 2.6f), null, PrimitiveType.Cylinder);   // the lantern
        Box(boxes, brass, foot + Vector3.up * (h + 20.2f), new Vector3(1f, 2.4f, 1f), null, PrimitiveType.Cylinder);
        Box(boxes, glowGold, foot + Vector3.up * (h + 21.8f), Vector3.one * 1.4f, null, PrimitiveType.Sphere);
        if (h > 210f) Beacons.Add(foot + Vector3.up * (h + 23f));
    }

    /// TWIN SLABS on a podium: two stone slabs with a well between them, bridged three times in iron, blind ends, a
    /// cornice on each (the envelope RoofKit builds the blade pair to)
    static void Blades(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float h, Skin skin, float az, Material core)
    {
        if (Cut(foot, w * 0.5f + 12f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        Vector3 outward = turn * Vector3.forward;
        Material stone = skin == pierDark || skin == charcoal ? stoneChar : stoneLime;
        Tier(sheets, foot + outward * 8f, w, 9f, h, skin, true, az, BlindSides, false, true);
        Tier(sheets, foot - outward * 8f, w, 9f, h * 0.86f, skin, true, az, BlindSides, false, true);
        Podium(boxes, sheets, foot, w, 25f, az);
        Cornice(boxes, foot + outward * 8f + Vector3.up * h, w, 9f, az, stone, core);
        Cornice(boxes, foot - outward * 8f + Vector3.up * (h * 0.86f), w, 9f, az, stone);
        RoofFurniture(boxes, foot + outward * 8f + Vector3.up * h, w, 9f, az, Mathf.RoundToInt(foot.x * 0.5f + foot.z * 0.9f), 1);
        RoofFurniture(boxes, foot - outward * 8f + Vector3.up * (h * 0.86f), w, 9f, az, Mathf.RoundToInt(foot.x * 0.9f + foot.z * 0.5f), 1);
        for (int i = 1; i <= 3; i++)
            Box(boxes, iron, foot + Vector3.up * (h * 0.22f * i), new Vector3(w * 0.3f, 2.6f, 8f), turn);
        WaterTower(boxes, foot - outward * 8f + turn * new Vector3(w * 0.3f, h * 0.86f + 1.3f, 0f), 3.2f, 5f, iron);
        RadioMast(boxes, foot + outward * 8f + turn * new Vector3(-w * 0.32f, h + 1.3f, 0f), Mathf.Min(h * 0.14f, 26f), true);
    }

    /// A SETBACK MASS (the plan's arcology, re-dressed): six storey-stacks stepping back on both long sides, a stone
    /// balustrade along every terrace, a stepped cornice at every setback, a crown spire, a radio mast and a
    /// searchlight at the top
    static void Arcology(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, float az)
    {
        if (Cut(foot, Mathf.Max(w, d) * 0.5f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        float[] part = { 0.3f, 0.2f, 0.16f, 0.13f, 0.11f, 0.1f };
        float y = 0f, cw = w, cd = d;
        for (int i = 0; i < part.Length; i++)
        {
            float th = h * part[i];
            Tier(sheets, foot + Vector3.up * y, cw, cd, th, i % 2 == 0 ? limestone : pier, true, az, 0, i == 0, i == part.Length - 1);
            y += th;
            Cornice(boxes, foot + Vector3.up * y, cw, cd, az, stoneLime, i >= 4 ? glowWarm : null);
            if (i == 0) Gargoyles(boxes, foot + Vector3.up * y, cw, cd, az, stoneLime);
            float nd = cd * 0.78f, nw = cw * 0.9f;
            if (i < part.Length - 1)
                for (int side = -1; side <= 1; side += 2)
                {
                    // The terrace: a balustrade along its edge on top of the cornice
                    Vector3 edge = foot + Vector3.up * (y + 1.3f) + turn * new Vector3(0f, 0f, side * (cd * 0.5f + 1.2f));
                    Box(boxes, stoneLime, edge + Vector3.up * 0.55f, new Vector3(cw + 2.4f, 1.1f, 0.4f), turn);
                }
            cw = nw; cd = nd;
        }
        float tw = cw / 0.9f, td = cd / 0.78f;
        RoofFurniture(boxes, foot + Vector3.up * h, tw, td, az, Mathf.RoundToInt(foot.x * 0.3f + foot.z * 0.7f), 2);
        CrownSpire(boxes, foot + Vector3.up * (h + 1.3f), Mathf.Min(tw, td) * 0.3f, h * 0.1f, stoneLime, glowGold);
        RadioMast(boxes, foot + Vector3.up * (h + 1.3f) + turn * new Vector3(tw * 0.34f, 0f, 0f), 30f, false);
        SearchlightDrum(boxes, foot + Vector3.up * (h + 1.3f) + turn * new Vector3(-tw * 0.34f, 0f, -td * 0.3f), az + 150f, 55f);
        Beacons.Add(foot + Vector3.up * (h + 1.3f + h * 0.1f + 3f));
        WashUp(sheets, foot, turn * Vector3.forward, d * 0.5f, w * 0.9f, h * 0.3f, washAmber);
        WashUp(sheets, foot, turn * Vector3.back, d * 0.5f, w * 0.9f, h * 0.3f, washAmber);
    }

    /// A TERRACE BLOCK of the inner ring: three garden steps down toward the plaza, a cornice on each
    static void Terrace(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, float az)
    {
        if (Cut(foot, Mathf.Max(w, d) * 0.5f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        Skin skin = (int)(az / 10f) % 2 == 0 ? cream : limestone;
        for (int i = 0; i < 3; i++)
        {
            float td = d * (1f - 0.27f * i), th = h * (i == 0 ? 0.45f : 0.275f), y = h * (i == 0 ? 0f : 0.45f + 0.275f * (i - 1));
            Vector3 at = foot + turn * new Vector3(0f, 0f, (d - td) * 0.5f);   // the steps go down on the side that faces the House
            Tier(sheets, at + Vector3.up * y, w, td, th, skin, true, az, BlindSides, i == 0, i == 2);
            Cornice(boxes, at + Vector3.up * (y + th), w, td, az, stoneCream, i == 2 ? glowWarm : null, 2);
            if (i == 2) continue;
            float ledge = d * 0.27f;
            Box(boxes, foliage, foot + Vector3.up * (y + th + 1.4f) + turn * new Vector3(0f, 0f, -d * 0.5f + ledge * (i + 0.5f)), new Vector3(w * 0.92f, 1.6f, ledge * 0.6f), turn);
        }
        RoofFurniture(boxes, foot + turn * new Vector3(0f, 0f, d * 0.27f) + Vector3.up * h, w, d * 0.46f, az, Mathf.RoundToInt(foot.x * 0.6f + foot.z * 0.4f), 1);
        if ((int)(az / 10f) % 3 == 1) Flagpole(boxes, foot + turn * new Vector3(w * 0.3f, h + 1f, d * 0.27f), 9f);
        // A tiny crowd on the first garden step, the one that looks at the House: a lit terrace with people on it
        Crowd(boxes, foot + Vector3.up * (h * 0.45f + 1.3f) + turn * new Vector3(0f, 0f, -d * 0.5f + d * 0.27f * 0.5f + 2f), Mathf.Min(w * 0.3f, 9f), 12, (int)az);
    }

    /// A TRIUMPHAL ARCH over an avenue: two stone pylons and a lintel with a chevron frieze in brass, a stepped
    /// attic over the lintel with a statue group on it, a lamp globe on each pylon
    static void Arch(MeshBaker boxes, SheetBaker sheets, float r, float az, float span, float height, float thick)
    {
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        Vector3 c = Polar(r, az), tangent = turn * Vector3.right;
        if (Cut(c, span * 0.5f + thick)) return;
        Tier(sheets, c + tangent * (span * 0.5f), thick, thick * 1.3f, height, limestone, false, az, BlindAll);
        Tier(sheets, c - tangent * (span * 0.5f), thick, thick * 1.3f, height, limestone, false, az, BlindAll);
        Tier(sheets, c + Vector3.up * height, span + thick, thick * 1.3f, thick, limestone, false, az, BlindAll);
        Cornice(boxes, c + Vector3.up * (height + thick), span + thick, thick * 1.3f, az, stoneLime, null, 2);
        Box(boxes, stoneLime, c + Vector3.up * (height + thick + 0.9f + thick * 0.2f), new Vector3(span * 0.5f, thick * 0.4f, thick * 0.9f), turn);   // the attic
        Box(boxes, glowWhite, c + Vector3.up * (height - 0.4f), new Vector3(span - thick, 0.4f, 0.6f), turn);   // the lamp under the lintel
        int n = Mathf.Max(3, Mathf.RoundToInt(span / 4f));
        for (int k = 0; k < n; k++)   // the chevrons, brass, up and down along both faces of the lintel
        {
            float x = -span * 0.5f + span * (k + 0.5f) / n, dy = k % 2 == 0 ? 0.7f : -0.7f;
            for (int s = -1; s <= 1; s += 2)
                Box(boxes, brass, c + turn * new Vector3(x, height + thick * 0.5f + dy, s * (thick * 0.65f + 0.15f)), new Vector3(1.2f, 1.2f, 0.3f), turn * Quaternion.Euler(0f, 0f, 45f));
        }
        float atticTop = height + thick + 0.9f + thick * 0.4f;
        Statue(boxes, c + Vector3.up * atticTop, thick * 1.3f, az);                                   // the group: a figure in the middle, two beside it
        Statue(boxes, c + Vector3.up * atticTop + tangent * (span * 0.17f), thick * 0.9f, az);
        Statue(boxes, c + Vector3.up * atticTop - tangent * (span * 0.17f), thick * 0.9f, az);
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 p = c + tangent * (s * span * 0.5f) + Vector3.up * (height + thick + 1.3f);
            Box(boxes, bronze, p + Vector3.up * 1.2f, new Vector3(0.5f, 2.4f, 0.5f), turn);
            Box(boxes, glowWhite, p + Vector3.up * 3f, Vector3.one * 1.6f, null, PrimitiveType.Sphere);
        }
    }

    /// A CRESCENT of brownstone: a terrace of joined houses bent round a ring, broken at every avenue, party walls
    /// blind, a stone cornice on each, a party rib between the houses of a run, a stoop and a lit door to every house
    /// on the inner rings, a water tank or a chimney on the roofs
    static void Crescent(MeshBaker boxes, SheetBaker sheets, float r, float h, float depth, bool stoops)
    {
        float sector = 360f / Avenues, gap = Mathf.Clamp(Mathf.Atan2(16f, r) * Mathf.Rad2Deg, 1f, 8f);
        float chordDeg = Mathf.Atan2(48f, r) * Mathf.Rad2Deg;   // fewer, longer runs since the silhouette pass
        for (int a = 0; a < Avenues; a++)
        {
            float from = a * sector + gap, to = (a + 1) * sector - gap;
            int n = Mathf.Max(1, Mathf.FloorToInt((to - from) / chordDeg));
            float step = (to - from) / n;
            for (int i = 0; i < n; i++)
            {
                if (n > 3 && i % 4 == 3) continue;   // a way through, every fourth run
                float az = from + (i + 0.5f) * step;
                float width = 2f * r * Mathf.Sin(step * 0.5f * Mathf.Deg2Rad) * 0.98f;
                float hh = h * (0.8f + 0.45f * CityTextures.Hash01(a, i, (int)r));
                Vector3 foot = Polar(r, az);
                if (Cut(foot, width * 0.5f)) continue;
                Quaternion turn = Quaternion.Euler(0f, az, 0f);
                float pick = CityTextures.Hash01(a, i, (int)r + 3);
                Skin skin = pick < 0.6f ? brownstone : pick < 0.85f ? brick : cream;
                Material coping = skin == cream ? stoneLime : brownFlat;
                Tier(sheets, foot, width, depth, hh, skin, true, az, BlindSides);
                Box(boxes, coping, foot + Vector3.up * (hh + 0.3f), new Vector3(width + 0.6f, 0.6f, depth + 0.6f), turn);   // the cornice
                int houses = Mathf.Max(2, Mathf.RoundToInt(width / 11f));
                Vector3 nrm = turn * Vector3.forward;
                for (int k = 1; k < houses; k++)   // the party ribs, on the street side (outward)
                {
                    float x = -width * 0.5f + width * k / houses;
                    Box(boxes, coping, foot + turn * new Vector3(x, hh * 0.5f, depth * 0.5f + 0.12f), new Vector3(0.5f, hh, 0.3f), turn);
                }
                if (stoops)
                    for (int k = 0; k < houses; k++)
                    {
                        float x = -width * 0.5f + width * (k + 0.5f) / houses + 2.2f;
                        Box(boxes, stoneLime, foot + turn * new Vector3(x, 0.7f, depth * 0.5f + 1.1f), new Vector3(2.2f, 1.4f, 2.2f), turn);   // the stoop
                        Box(boxes, glowWarm, foot + turn * new Vector3(x, 2.6f, depth * 0.5f + 0.15f), new Vector3(1.2f, 2.4f, 0.2f), turn);   // the door, lit
                    }
                if (i % 2 == 0) RoofFurniture(boxes, foot + Vector3.up * hh, width, depth, az, a * 101 + i * 7 + (int)r, 0);
                else Chimney(boxes, foot + Vector3.up * (hh + 0.5f) + turn * new Vector3(width * 0.3f, 0f, -depth * 0.3f), 2.6f);
            }
        }
    }

    /// One of the towers nobody mentions: a derelict masonry block, sooted, unlit, its cornice broken, the tank on
    /// its roof fallen over
    static void Dead(SheetBaker sheets, Vector3 foot, float w, float d, float h, float az)
    {
        if (Cut(foot, Mathf.Max(w, d) * 0.5f)) return;
        var s = new Skin { wall = wallSoot, blind = wallSoot };
        Tier(sheets, foot, w, d, h * 0.6f, s, false, az);
        Tier(sheets, foot + Vector3.up * (h * 0.6f), w * 0.8f, d * 0.8f, h * 0.4f, s, false, az);
    }

    // ---------------------------------------------------------------- the plan

    /// The whole still city. `boxes` and `sheets` are mirrored under the wet ground; `ground` is what lies flat on it
    /// or moves (the traffic), and is not
    public static void BuildStill(MeshBaker boxes, SheetBaker sheets, SheetBaker ground)
    {
        // THE COURT: 24 shafts on a ring, no two alike (MenuCityGotham)
        Court(boxes, sheets);

        // THE TERRACES: civic blocks, hotels and clubs by turns (twenty-four lots, wider since the silhouette pass),
        // and an arch over every avenue where it passes through
        for (int i = 0; i < 24; i++)
        {
            float az = (i + 0.5f) * 15f;
            Vector3 foot = Polar(345f, az);
            switch (i % 6)
            {
                case 0: Terrace(boxes, sheets, foot, 44f, 30f, 58f, az); break;
                case 1: Hotel(boxes, sheets, foot, 44f, 30f, 46f + 8f * CityTextures.Hash01(i, 1, 345), az, i); break;
                case 2: Club(boxes, sheets, foot, 40f, 30f, 24f + 6f * CityTextures.Hash01(i, 2, 345), az, i); break;
                case 3: Terrace(boxes, sheets, foot, 44f, 30f, 42f, az); break;
                case 4: DepartmentStore(boxes, sheets, foot, 46f, 30f, 32f + 8f * CityTextures.Hash01(i, 4, 345), az, i); break;
                default: Theatre(boxes, sheets, foot, 40f, 30f, 26f + 6f * CityTextures.Hash01(i, 5, 345), az, i); break;
            }
        }
        for (int a = 0; a < Avenues; a++) Arch(boxes, sheets, 345f, a * 360f / Avenues, 30f, 40f, 6f);
        Crescent(boxes, sheets, 303f, 17f, 13f, true);
        Crescent(boxes, sheets, 386f, 14f, 12f, true);

        // BEYOND THE BELT: the rings of lots (fewer and bigger since the silhouette pass: a few giants, many low
        // blocks), the landmarks on them placed by hand, the rest taken by the fabric, the setbacks and the giants by
        // turns (MenuCityGotham). The first ring leans in toward the belt
        Ring(boxes, sheets, 600f, 26, 1, 40f);
        Ring(boxes, sheets, 735f, 18, 2, 36f);
        Ring(boxes, sheets, 885f, 18, 3, 110f);
        Ring(boxes, sheets, 1045f, 28, 4, 44f);
        Ring(boxes, sheets, 1215f, 36, 5, 40f);
        for (int a = 0; a < Avenues; a++) Arch(boxes, sheets, 735f, a * 360f / Avenues, 44f, 62f, 9f);
        float[] rings = { 565f, 640f, 985f, 1105f, 1275f, 1325f };
        foreach (float r in rings) Crescent(boxes, sheets, r, 16f + (r - 500f) * 0.012f, 13f, false);

        DenseCity(boxes, sheets);   // the brick rows between the rings, the kiosks (MenuCityDense)
        Belt(boxes, sheets);
        Promenades(ground);
        Traffic(ground);
        ElevatedRail(boxes, ground, 318f, 30f, true);                   // the one that threads between the Court and the terraces, on riveted piers
        ElevatedRail(boxes, ground, RailRadius, RailY, false);          // the two the trains run on (MenuCityLife)
        ElevatedRail(boxes, ground, OuterRailRadius, OuterRailY, false);
        ElevatedRail(boxes, ground, 790f, 34f, true);                   // and two more, their carriages a texture scroll
        ElevatedRail(boxes, ground, 1166f, 38f, true);
        Viaduct(boxes, ground, 540f, 30f, 12f);                          // the elevated on stone viaducts (MenuCityNight)
        Viaduct(boxes, ground, 1150f, 40f, 14f);
        StreetLife(boxes);                                               // lamps, limousines, statues, a fountain (MenuCityGotham)
        GroundSearchlights(boxes, sheets);                               // the three beams crossing the sky from the far city (MenuCityAir)
        CrownFans(boxes, sheets);                                        // the fans of thin beams off the tallest crowns (MenuCityAir)
        HazeBanks(sheets);                                               // the fog lying between the ranks of towers (MenuCityAir)
        PlazaAndTower(boxes, sheets, ground);
    }

    /// The belt, the camera's corridor: lawns, a bronze-ribbed conservatory between every pair of avenues, trees,
    /// nothing over 14 m
    static void Belt(MeshBaker boxes, SheetBaker sheets)
    {
        sheets.Ring(lawn, Origin + new Vector3(0f, 0.25f, 0f), 428f, 488f, 72, 1, 60f);
        sheets.Ring(lawn, Origin + new Vector3(0f, 0.25f, 0f), 512f, 548f, 72, 1, 60f);
        for (int a = 0; a < Avenues; a++)
        {
            float az = (a + 0.5f) * 360f / Avenues;
            if (!Cut(Polar(458f, az), 15f))
            {
                Box(boxes, domeGlow, Polar(458f, az), new Vector3(27f, 25f, 27f), null, PrimitiveType.Sphere);
                Box(boxes, bronze, Polar(458f, az, 0.6f), new Vector3(30f, 0.9f, 30f), null, PrimitiveType.Cylinder);
                for (int k = 0; k < 4; k++) Box(boxes, bronze, Polar(458f, az, 6f) + Radial(12.2f, az + k * 90f), new Vector3(0.5f, 11f, 0.5f), Quaternion.Euler(0f, az + k * 90f, 0f));   // the ribs
                Box(boxes, glowGold, Polar(458f, az, 13.6f), Vector3.one * 1.4f, null, PrimitiveType.Sphere);   // the finial
            }
            for (int t = 0; t < 14; t++)
            {
                float taz = az + (t - 6.5f) * 1.9f, tr = t % 2 == 0 ? 436f : 530f;
                if (OffAvenue(taz) < 2.4f) continue;
                float s = 4f + 3f * CityTextures.Hash01(a, t, 9);
                if (Cut(Polar(tr, taz), s)) continue;
                Box(boxes, foliage, Polar(tr, taz, s * 0.75f), new Vector3(s * 0.8f, s * 1.2f, s * 0.8f), Quaternion.Euler(0f, taz + 45f, 0f));
            }
        }
    }

    /// The light on the ground: lamps down twelve avenues from the plaza to the edge of the city, and round the rings
    static void Promenades(SheetBaker ground)
    {
        for (int a = 0; a < Avenues; a++)
        {
            float az = a * 360f / Avenues;
            Line(ground, promenade, Polar(Plaza, az, 0.1f), Polar(Extent, az, 0.1f), 15f, 16f);
        }
        float[] rings = { Plaza, 500f, 695f, 945f, 1255f };
        foreach (float r in rings) RingLine(ground, promenadeRing, r, 0.12f, r < 300f ? 9f : 12f, 16f, 0f, false);
    }

    /// A strip of texture from a to b in pieces (some shaders work fog out at a quad's corners), `tile` metres to a repeat
    static void Line(SheetBaker sb, Material m, Vector3 a, Vector3 b, float width, float tile, bool upright = false)
    {
        float len = Vector3.Distance(a, b);
        if (len < 1f) return;
        int pieces = Mathf.Max(1, Mathf.CeilToInt(len / 130f));
        float v0 = Random.value;
        for (int i = 0; i < pieces; i++)
        {
            float k0 = i / (float)pieces, k1 = (i + 1) / (float)pieces;
            Vector3 p0 = Vector3.Lerp(a, b, k0), p1 = Vector3.Lerp(a, b, k1);
            if (CutSeg(p0, p1)) continue;
            Lane(sb, m, p0, p1, width, v0 + len * k0 / tile, v0 + len * k1 / tile, upright);
        }
    }

    /// One piece of a lane: flat, and (for a lane seen from the side, the carriages on a viaduct) a second sheet on
    /// edge through it, so it is never edge-on to the lens
    static void Lane(SheetBaker sb, Material m, Vector3 p0, Vector3 p1, float width, float v0, float v1, bool upright)
    {
        sb.Strip(m, p0, p1, width, v0, v1);
        if (!upright) return;
        Vector3 up = Vector3.up * (width * 0.5f);
        sb.Quad(m, p0 - up, p1 - up, p1 + up, p0 + up, new Vector2(0f, v0), new Vector2(0f, v1), new Vector2(1f, v1), new Vector2(1f, v0));
    }

    /// A ring of texture at radius r and height y: a whole number of tiles round it, so whatever scrolls has no seam.
    /// `reverse` lays it the other way round, so the same scroll runs it the other way
    static void RingLine(SheetBaker sb, Material m, float r, float y, float width, float tile, float shift, bool upright, bool reverse = false)
    {
        const int Seg = 96;
        float total = 2f * Seg * r * Mathf.Sin(Mathf.PI / Seg);
        float per = Mathf.Max(1f, Mathf.Round(total / tile)) / Seg;
        for (int i = 0; i < Seg; i++)
        {
            Vector3 p0 = Polar(r, i * 360f / Seg, y), p1 = Polar(r, (i + 1) * 360f / Seg, y);
            if (CutSeg(p0, p1)) continue;
            if (reverse) Lane(sb, m, p1, p0, width, -(i + 1) * per + shift, -i * per + shift, upright);
            else Lane(sb, m, p0, p1, width, i * per + shift, (i + 1) * per + shift, upright);
        }
    }

    /// THE TRAFFIC IS ON THE GROUND. Head lamps and tail lamps down every avenue beyond the terraces and round four
    /// of the rings, out one way and back the other. One texture scroll (MenuWeather) moves every car in the city
    static void Traffic(SheetBaker ground)
    {
        for (int a = 0; a < Avenues; a++)
        {
            float az = a * 360f / Avenues;
            Vector3 side = Radial(4.6f, az + 90f);
            Line(ground, trafficWarm, Polar(Plaza + 30f, az, 0.35f) + side, Polar(Extent - 20f, az, 0.35f) + side, 3.2f, 256f);
            Line(ground, trafficHeads, Polar(Extent - 20f, az, 0.35f) - side, Polar(Plaza + 30f, az, 0.35f) - side, 3.2f, 256f);
        }
        float[] r = { 500f, 695f, 945f, 1255f };
        for (int i = 0; i < r.Length; i++)
        {
            RingLine(ground, trafficWarm, r[i] - 4.2f, 0.35f, 3.2f, 256f, i * 0.31f, false);
            RingLine(ground, trafficHeads, r[i] + 4.2f, 0.35f, 3.2f, 256f, i * 0.47f, false, true);
        }
    }

    /// THE ELEVATED: a rail ring on a riveted iron viaduct, plate girders down both sides, a lamp every few bays,
    /// stone-footed iron piers; and if asked, lit carriages scrolling along it
    static void ElevatedRail(MeshBaker boxes, SheetBaker ground, float r, float y, bool trains)
    {
        var pts = RailPath(r, y);
        for (int i = 1; i < pts.Length; i++)
        {
            if (CutSeg(pts[i - 1], pts[i])) continue;
            Vector3 mid = (pts[i - 1] + pts[i]) * 0.5f, d = pts[i] - pts[i - 1];
            Quaternion rot = Quaternion.LookRotation(d.normalized, Vector3.up);
            Box(boxes, iron, mid - Vector3.up * 0.6f, new Vector3(5.2f, 1.2f, d.magnitude + 1.2f), rot);
            Box(boxes, iron, mid + rot * new Vector3(2.45f, 0.4f, 0f), new Vector3(0.35f, 1.5f, d.magnitude + 1.2f), rot);   // the girders
            Box(boxes, iron, mid + rot * new Vector3(-2.45f, 0.4f, 0f), new Vector3(0.35f, 1.5f, d.magnitude + 1.2f), rot);
            if (i % 3 == 0 && OffAvenue(i * 360f / (pts.Length - 1)) > 2.5f)   // a pier every third bay, never in an avenue
            {
                // A riveted iron pier: the column, a flanged plate up each side (the rivet rows are what reads), a cap
                Box(boxes, iron, new Vector3(pts[i].x, (y - 1.2f) * 0.5f + 0.8f, pts[i].z), new Vector3(1.8f, y - 2.8f, 1.8f), rot);
                Box(boxes, iron, new Vector3(pts[i].x, (y - 1.2f) * 0.5f + 0.8f, pts[i].z), new Vector3(2.6f, y - 3.6f, 0.3f), rot);
                Box(boxes, iron, new Vector3(pts[i].x, y - 1.9f, pts[i].z), new Vector3(3.2f, 0.5f, 3.2f), rot);
                Box(boxes, stoneLime, new Vector3(pts[i].x, 0.8f, pts[i].z), new Vector3(3.4f, 1.6f, 3.4f), rot);   // the stone footing
            }
            if (i % 6 == 3)
            {
                Vector3 post = mid + rot * new Vector3(2.8f, 0f, 0f);
                Box(boxes, bronze, post + Vector3.up * 1.6f, new Vector3(0.22f, 3.2f, 0.22f), rot);
                Box(boxes, glowWhite, post + Vector3.up * 3.4f, Vector3.one * 0.7f, null, PrimitiveType.Sphere);
            }
        }
        if (trains) RingLine(ground, carriages, r, y + 1.8f, 3.2f, 512f, 0.3f, true);
    }

    /// A rail's line: a closed ring (the last point is the first), so a train runs it for ever
    public static Vector3[] RailPath(float r, float y)
    {
        const int Seg = 72;
        var pts = new Vector3[Seg + 1];
        for (int i = 0; i <= Seg; i++) pts[i] = Polar(r, i * 360f / Seg, y);
        return pts;
    }

    // ---------------------------------------------------------------- the plaza, and the House in it

    /// The company's plaza: brass lines in the black stone, lamp standards round its edge and down the approach from
    /// the south; and what the House wears that the stone does not: its windows, punched between the piers and lit
    /// in runs (HouseLight, MenuCityHouse), gold at the top floors, the wash of its floodlights
    static void PlazaAndTower(MeshBaker boxes, SheetBaker sheets, SheetBaker ground)
    {
        float[] paving = { 112f, 146f, 180f };
        foreach (float r in paving) ground.Ring(glowGold, Origin + new Vector3(0f, 0.12f, 0f), r - 0.3f, r + 0.3f, 96, 1, 50f);
        // Lamp standards round the edge, and down both kerbs of the approach from the south: a stone plinth, a
        // bronze column, a cream globe. Monumental; it is that kind of company
        for (int i = 0; i < 36; i++)
        {
            float az = (i + 0.5f) * 10f;
            Standard(boxes, Polar(Plaza - 9f, az), 12f, az);
        }
        for (int i = 0; i < 6; i++)
            for (int side = -1; side <= 1; side += 2)
                Standard(boxes, Origin + new Vector3(side * 13f, 0f, -(Plaza + 22f + i * 30f)), 8f, 0f);
        HouseLight(sheets, MenuBackdrop.TowerScale);
    }

    /// A LAMP STANDARD: a stone plinth, a bronze column, a cream globe
    static void Standard(MeshBaker boxes, Vector3 p, float height, float yaw)
    {
        if (Cut(p, 2f)) return;
        Box(boxes, stoneChar, p + Vector3.up * 1.2f, new Vector3(2.6f, 2.4f, 2.6f), Quaternion.Euler(0f, yaw, 0f));
        Box(boxes, bronze, p + Vector3.up * (2.4f + height * 0.5f), new Vector3(0.8f, height, 0.8f), Quaternion.Euler(0f, yaw + 45f, 0f));
        Box(boxes, glowWhite, p + Vector3.up * (2.4f + height + 1.1f), Vector3.one * 2.4f, null, PrimitiveType.Sphere);
    }
}
