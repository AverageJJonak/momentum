using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IMPORTED SURFACE SETS (2026-10-23). Real PBR texture sets (ambientCG, CC0; Tools/ImportTextures.ps1 puts them in
/// Resources/Surfaces/&lt;Set&gt;/ as Color, Normal, MetalGloss, Occlusion) stand in for the procedural maps of the
/// surface kinds mapped below; every other kind, and every gridded floor, keeps its procedural map. The palette's TINT
/// stays the surface's average colour: the base colour is the tint divided by the set's mean luminance (sets.json),
/// so a bright plaster photo and a dark brick photo both come out at the palette's value, and the readability maths in
/// LevelKitLook hold. A set that is not in Resources simply does not apply: the procedural map draws as before.
/// </summary>
public static partial class LevelKit
{
    public class SurfaceSet
    {
        public string name;
        public Texture2D color, normal, metalGloss, occlusion;
        public float mean = 0.5f;
    }

    [Serializable] class SetIndex { public SetEntry[] sets; }
    [Serializable] class SetEntry { public string name; public float mean; }

    /// Which set each surface kind wears. Terrazzo, Fluted and Gravel keep their drawn lines; Oxblood is a lacquer;
    /// Brass, Plate, Groove, Panel, Strata, Bark and Leaf have no photo that fits the poster look
    static readonly Dictionary<Surf, string> setOf = new Dictionary<Surf, string>
    {
        { Surf.Plaster, "Plaster001" },
        { Surf.Concrete, "Concrete034" },
        { Surf.Marble, "Marble021" },
        { Surf.Brick, "Bricks106A" },
        { Surf.Ashlar, "PavingStones046" },
        { Surf.Tile, "Tiles036" },
        { Surf.Wood, "WoodFloor051" },
        { Surf.WoodPanel, "WoodFloor051" },
        { Surf.Brass, "Metal034" },          // 2026-10-26: brushed gold for the brass
        { Surf.Plate, "DiamondPlate009" },   // plate steel for the plate floors
        { Surf.Panel, "Concrete045" },       // 2026-10-27: nothing drawn by hand any more — the panels are cast concrete,
        { Surf.Strata, "Concrete031" },      // the strata bunker tiles, the grooves board-formed concrete, the gravel real,
        { Surf.Groove, "Concrete028" },      // the terrazzo a photo (its drawn divider strips are gone with the grid)
        { Surf.Gravel, "Gravel043" },
        { Surf.Terrazzo, "Terrazzo013" },
        { Surf.Fluted, "PaintedPlaster017" },   // the House's cobalt walls: painted plaster under the tint (the flutes were drawn)
    };

    /// A marble asked for DARK (the House's black marble: a tint under this luminance) wears the black set, not the white one lifted
    const string DarkMarbleSet = "Marble016";
    const float DarkMarbleBelow = 0.16f;

    /// The set for `surf` under `tint`: the dark marble when the tint is black, else as TryGetSet
    public static bool TryGetSet(Surf surf, Color tint, out SurfaceSet set)
    {
        if (surf == Surf.Marble && PhotoSets && 0.2126f * tint.r + 0.7152f * tint.g + 0.0722f * tint.b < DarkMarbleBelow && TryLoad(DarkMarbleSet, out set)) return true;
        return TryGetSet(surf, out set);
    }

    static readonly Dictionary<string, SurfaceSet> loadedSets = new Dictionary<string, SurfaceSet>();
    static Dictionary<string, float> setMeans;
    /// PlayerPrefs "look_photo": 0 turns the imported sets off (the procedural maps draw), for comparing the two
    static int photoSets = -1;

    public static bool PhotoSets
    {
        get { if (photoSets < 0) photoSets = PlayerPrefs.GetInt("look_photo", 1); return photoSets == 1; }
        set { photoSets = value ? 1 : 0; PlayerPrefs.SetInt("look_photo", photoSets); }
    }

    /// The set for `surf`, loaded once; false when the kind has none or its files are not in Resources
    public static bool TryGetSet(Surf surf, out SurfaceSet set)
    {
        set = null;
        if (!PhotoSets || !setOf.TryGetValue(surf, out string name)) return false;
        return TryLoad(name, out set);
    }

    /// The named set, loaded once; false when its files are not in Resources
    static bool TryLoad(string name, out SurfaceSet set)
    {
        if (loadedSets.TryGetValue(name, out set)) return set != null;
        var color = Resources.Load<Texture2D>("Surfaces/" + name + "/Color");
        if (color == null) { loadedSets[name] = null; return false; }
        set = new SurfaceSet
        {
            name = name,
            color = color,
            normal = Resources.Load<Texture2D>("Surfaces/" + name + "/Normal"),
            metalGloss = Resources.Load<Texture2D>("Surfaces/" + name + "/MetalGloss"),
            occlusion = Resources.Load<Texture2D>("Surfaces/" + name + "/Occlusion"),
            mean = MeanOf(name),
        };
        loadedSets[name] = set;
        return true;
    }

    static float MeanOf(string name)
    {
        if (setMeans == null)
        {
            setMeans = new Dictionary<string, float>();
            var text = Resources.Load<TextAsset>("Surfaces/sets");
            if (text != null)
            {
                try
                {
                    var index = JsonUtility.FromJson<SetIndex>(text.text.TrimStart('﻿'));
                    if (index != null && index.sets != null)
                        foreach (var e in index.sets) if (!string.IsNullOrEmpty(e.name)) setMeans[e.name] = e.mean;
                }
                catch (Exception e) { Debug.LogWarning("LevelKit: Surfaces/sets.json unreadable (" + e.Message + "); tints are not normalised."); }
            }
        }
        return setMeans.TryGetValue(name, out float m) && m > 0.02f ? m : 0.5f;
    }

    /// The set laid on a palette material: the tint normalised by the photo's brightness, the maps bound, the keywords
    /// the LitFull / ToonFull templates expect. `full` = the template carries the metal-gloss and occlusion variants
    static void ApplySet(Material m, SurfaceSet set, Color tint, float smoothness, float bumpScale, bool full)
    {
        float k = 1f / Mathf.Max(set.mean, 0.05f);
        var baseColor = new Color(Mathf.Min(tint.r * k, 1.6f), Mathf.Min(tint.g * k, 1.6f), Mathf.Min(tint.b * k, 1.6f), tint.a);
        m.SetColor("_BaseColor", baseColor);
        m.SetTexture("_BaseMap", set.color);
        if (set.normal != null)
        {
            m.SetTexture("_BumpMap", set.normal);
            m.SetFloat("_BumpScale", Mathf.Clamp(bumpScale, 0.1f, BumpCap));   // a photo's relief kept faint: the poster rule
            m.EnableKeyword("_NORMALMAP");
        }
        if (full)
        {
            if (set.metalGloss != null) { m.SetTexture("_MetallicGlossMap", set.metalGloss); m.EnableKeyword("_METALLICSPECGLOSSMAP"); }
            m.SetFloat("_Smoothness", Mathf.Clamp01(smoothness * 1.55f));   // the map's alpha scales it, as the procedural path does
            m.SetFloat("_Metallic", 0f);
            if (set.occlusion != null) { m.SetTexture("_OcclusionMap", set.occlusion); m.SetFloat("_OcclusionStrength", 1f); m.EnableKeyword("_OCCLUSIONMAP"); }
        }
        else m.SetFloat("_Smoothness", smoothness);
    }
}
