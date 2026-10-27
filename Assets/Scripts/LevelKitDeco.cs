using UnityEngine;

/// <summary>
/// DARK DECO (2026-10-07): the materials of the new direction, in one place, so every builder names the same ones.
/// Gotham 1939: the world is painted on black paper and light is what is drawn. Black marble floors with brass grid
/// lines, cream and oxblood walls, fluted plaster, brick and riveted iron for the plant, brass and bronze for
/// everything the company touches, frosted glass, amber windows. Every one of them is a textured Surface (the
/// LevelKitTextures fields: PBR maps, since the poster cut of 2026-10-16 each ONE flat field with at most one large
/// feature — a course line, a vein, a board joint — and a near-flat normal) but the glass and the two glows.
/// The colour language is untouched: cyan is the blade's alone, gold is flow, red is danger, amber is a unit's light.
/// </summary>
public static partial class LevelKit
{
    static Material marble, terrazzo, cream, limestone, charcoal, fluted, brick, brass, bronze, iron, oxblood, frostGlass, lampCream, windowAmber, woodPanel;

    // The bible's palette, linear floats, named once
    public static readonly Color MarbleBlack = new Color(0.05f, 0.05f, 0.06f);
    public static readonly Color CharcoalStone = new Color(0.20f, 0.20f, 0.22f);   // 2026-10-16 readability: still the dark stone, but 43 walkable decks and lips sit on it, so it must read (0.11 → 0.20)
    public static readonly Color CreamPlaster = new Color(0.78f, 0.72f, 0.6f);
    public static readonly Color LimestoneGrey = new Color(0.55f, 0.52f, 0.46f);
    public static readonly Color BrassGold = new Color(0.78f, 0.58f, 0.25f);
    public static readonly Color BronzeDark = new Color(0.35f, 0.24f, 0.12f);
    public static readonly Color IronGrey = new Color(0.16f, 0.16f, 0.18f);
    public static readonly Color OxbloodRed = new Color(0.32f, 0.08f, 0.08f);
    public static readonly Color Mustard = new Color(0.7f, 0.55f, 0.2f);
    public static readonly Color Walnut = new Color(0.26f, 0.15f, 0.08f);
    public static readonly Color BrickRed = new Color(0.36f, 0.17f, 0.12f);
    public static readonly Color LampCreamLight = new Color(1f, 0.9f, 0.7f);
    public static readonly Color WindowAmberLight = new Color(1f, 0.72f, 0.38f);

    /// Black marble with cream veins, polished (the office floor; the grid is drawn in it when it is a floor)
    public static Material Marble => marble ??= Surface(MarbleBlack, 0.85f, Surf.Marble, 2f);
    /// Terrazzo: dark chips in a charcoal matrix, the speed grid as brass divider strips (the civic floor)
    public static Material Terrazzo => terrazzo ??= Surface(CharcoalStone, 0.7f, Surf.Terrazzo, 2f, -1f, Grid());
    /// Cream plaster, the civic wall
    public static Material Cream => cream ??= Surface(CreamPlaster, 0.35f, Surf.Plaster, 2f);
    /// Cut limestone in half-metre courses: banks, plinths, the rooftops' parapets
    public static Material Limestone => limestone ??= Surface(LimestoneGrey, 0.3f, Surf.Ashlar, 2f);
    /// Charcoal plaster: ceilings, the dark of a stepped mass
    public static Material Charcoal => charcoal ??= Surface(CharcoalStone, 0.4f, Surf.Plaster, 2f, 0.4f);
    /// Fluted plaster: seven half-round ribs to the repeat, for pilasters and the bays between them
    public static Material Fluted => fluted ??= Surface(new Color(0.72f, 0.67f, 0.56f), 0.35f, Surf.Fluted, 2f);
    /// Running-bond brick with dark mortar: the sublevels and the plant
    public static Material Brick => brick ??= Surface(BrickRed, 0.2f, Surf.Brick, 2f);
    /// Brushed brass, metallic 0.9: rails, frames, the company's trim (gold is flow)
    public static Material Brass => brass ??= Surface(BrassGold, 0.7f, Surf.Brass, 1f);
    /// Dark bronze, the same brushing: statues, the heavier fittings
    public static Material Bronze => bronze ??= Surface(BronzeDark, 0.6f, Surf.Brass, 1f);
    /// Riveted iron plate: the plant's own skin. LevelKit.Iron (the office kit) is the plain lit one
    public static Material RivetIron => iron ??= Surface(IronGrey, 0.5f, Surf.Plate, 1f);
    /// Oxblood lacquered plaster: the company's rooms
    public static Material Oxblood => oxblood ??= Surface(OxbloodRed, 0.45f, Surf.Oxblood, 2f);
    /// Dark walnut panelling, three wide boards to the repeat: crates and pallets stand on it (lips), so it is the
    /// readable walnut (LevelKit.WalnutLit, luminance 0.22) rather than the bible's 0.17 that shaded to black (2026-10-16)
    public static Material WoodPanel => woodPanel ??= Surface(WalnutLit, 0.55f, Surf.WoodPanel, 2f);
    /// Frosted glass: a see-through cream sheet on the transparent template
    public static Material FrostGlass => frostGlass ??= Sheet(new Color(0.92f, 0.88f, 0.78f, 0.55f), null, false);
    /// Light, as a surface: a lamp's cream and a window's amber, unlit so they carry the picture at night
    public static Material LampCream => lampCream ??= Unlit(Shader.Find("Universal Render Pipeline/Unlit"), new Color(2.2f, 1.95f, 1.5f));
    public static Material WindowAmber => windowAmber ??= Unlit(Shader.Find("Universal Render Pipeline/Unlit"), new Color(2.4f, 1.7f, 0.9f));

    /// A level build starts: the previous level's instances are gone with its scene
    static void ResetDeco()
    {
        marble = terrazzo = cream = limestone = charcoal = fluted = brick = brass = bronze = iron = oxblood = frostGlass = lampCream = windowAmber = woodPanel = null;
    }
}
