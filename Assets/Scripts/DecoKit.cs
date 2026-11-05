using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE DECO DRESSING (2026-11-05). The levels are boxes with colliders; the modeller's pieces (Docs/Art/MODELLER_BRIEF.md)
/// are the skin on them: a fluted panel on a run wall, a cornice along its top, the shafts and the transom of a gate, a
/// capital under a hop stone, a lamp standard at a deck's edge. A piece is a prefab in Resources/Models named as the
/// brief names it; the beat kit asks for it by name wherever it places the box the piece dresses, and gets NOTHING when
/// the file is not there yet, so a level builds the same with or without the pieces and the first file to arrive shows
/// up everywhere at once. Every piece: colliders stripped (the game's own boxes are the collision, so the validator, the
/// metrics and the motor never see a piece), its material slots swapped for the game's materials by the slot NAME the
/// brief fixes (Stone, Dark, Brass, Bronze, Glass, Lit, Suede, Sleeve, Cuff), put on the Ignore Raycast layer (the
/// welder and the motor's casts leave it alone; the cameras draw it). STAND-INS: with `StandIns` on, a missing piece
/// is a brass box of its size, to see where the pieces will go before they exist; off by default.
/// </summary>
public static class DecoKit
{
    /// Off: no dressing at all, the boxes as they were
    public static bool Enabled = true;
    /// A brass box where a piece is missing: the placement, seen before the piece exists (a dev switch, off in play)
    public static bool StandIns = false;
    /// The relief of a facade panel: its ribs stand this far off its back, and a run wall's panel is sunk by this much so
    /// the ribs' tops are flush with the collider the player actually runs on (the box's face fills the grooves)
    public const float FacadeRelief = 0.15f;
    /// How far a piece that is not sunk stands off its box's face: never two faces on one plane
    public const float Proud = 0.05f;
    public const int Layer = 0;   // Default: the level welder combines the pieces (one draw per material per 40 m cell); without colliders nothing casts against them anyway

    static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();
    static readonly HashSet<string> missing = new HashSet<string>();
    static readonly Dictionary<string, int> placed = new Dictionary<string, int>();
    static int standIns;

    public static bool Has(string name) => Prefab(name) != null;

    /// The prefab for a piece, from Resources/Models, or null when it is not there (asked once per name per session)
    public static GameObject Prefab(string name)
    {
        if (cache.TryGetValue(name, out var p)) return p;
        if (missing.Contains(name)) return null;
        p = Resources.Load<GameObject>("Models/" + name);
        if (p == null) { missing.Add(name); return null; }
        cache[name] = p;
        return p;
    }

    /// Forgets what was and was not found, so a piece dropped into Resources/Models during a session is seen on the next build
    public static void Reload() { cache.Clear(); missing.Clear(); trim = null; }

    /// Places a piece with its pivot at `pos`, facing `rot` (the piece's +Z out of the wall or up the line, +Y up): the prefab
    /// when it exists, a stand-in box of `standInSize` (its base at the pivot, centred on x, extending along +Z) when asked,
    /// nothing otherwise. `scale` stretches the piece (a capital over a stone a little wider than 3 m); `standInCentred` centres the
    /// stand-in on the pivot (a floor piece: a post shaft, a capital, a lamp) instead of reaching out along +Z (a wall piece)
    public static GameObject Place(Transform root, string name, Vector3 pos, Quaternion rot, Vector3 standInSize, Vector3? scale = null, bool standInCentred = false)
    {
        if (!Enabled || root == null) return null;
        var prefab = Prefab(name);
        GameObject go;
        if (prefab != null)
        {
            go = Object.Instantiate(prefab, pos, rot, Holder(root));
            go.name = "Deco:" + name;
            Strip(go);
            Skin(go);
            placed[name] = placed.TryGetValue(name, out int n) ? n + 1 : 1;
        }
        else if (StandIns)
        {
            go = StandIn(name, standInSize, standInCentred);
            go.transform.SetParent(Holder(root), false);
            go.transform.SetPositionAndRotation(pos, rot);
            standIns++;
        }
        else return null;
        if (scale.HasValue) go.transform.localScale = Vector3.Scale(go.transform.localScale, scale.Value);
        SetLayer(go.transform, Layer);
        return go;
    }

    /// One "Deco" child per level root: everything dressed hangs under it, easy to hide or count
    static Transform Holder(Transform root)
    {
        var h = root.Find("Deco");
        if (h == null) { h = new GameObject("Deco").transform; h.SetParent(root, false); }
        return h;
    }

    static void Strip(GameObject go)
    {
        foreach (var c in go.GetComponentsInChildren<Collider>(true))
        {
            if (Application.isPlaying) Object.Destroy(c); else Object.DestroyImmediate(c);
        }
    }

    /// The brief's slot names → the game's materials: a modeller's placeholder colours never reach the screen
    static void Skin(GameObject go)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = Slot(mats[i] != null ? mats[i].name : "");
                if (m != null) { mats[i] = m; changed = true; }
            }
            if (changed) r.sharedMaterials = mats;
        }
    }

    /// The game's material for a slot name ("Brass", "Brass.001", "Brass (Instance)" all read as Brass); null for a name that is not a slot
    public static Material Slot(string slotName)
    {
        if (string.IsNullOrEmpty(slotName)) return null;
        string s = slotName.Replace(" (Instance)", "");
        int dot = s.IndexOf('.'); if (dot > 0) s = s.Substring(0, dot);
        switch (s.Trim())
        {
            case "Stone": return LevelKit.Wall;
            case "Dark": return LevelKit.Dark;
            case "Brass": return LevelKit.Brass;
            case "Bronze": return LevelKit.Bronze;
            case "Glass": return LevelKit.FrostGlass;
            case "Lit": return LevelKit.Neon;
            case "Suede": return RigDesign.Suede;
            case "Sleeve": return RigDesign.Charcoal;
            case "Cuff": return LevelKit.Cream;
            case "Trim": return TrimMaterial();   // the kit's shared trim sheet, when its textures are beside the pieces
            default: return null;
        }
    }

    static Material trim;
    /// THE TRIM SHEET: one material for the whole kit from the textures dropped beside the pieces (Resources/Models/trim_albedo,
    /// trim_normal, trim_mask with metallic in R and smoothness in A, trim_emission), on the kit's own lit template so it shades
    /// like every other textured surface of the game. Null until trim_albedo exists: the modeller's own material then stays
    public static Material TrimMaterial()
    {
        if (trim != null) return trim;
        var albedo = Resources.Load<Texture2D>("Models/trim_albedo");
        if (albedo == null) return null;
        bool full = LevelKit.Template("LitFull") != null;
        var m = LevelKit.FromTemplate(full ? "LitFull" : "LitNormalMap", "Universal Render Pipeline/Lit");
        m.name = "Trim";
        m.SetColor("_BaseColor", Color.white);
        m.SetTexture("_BaseMap", albedo);
        var normal = Resources.Load<Texture2D>("Models/trim_normal");
        if (normal != null) { m.SetTexture("_BumpMap", normal); m.SetFloat("_BumpScale", 1f); m.EnableKeyword("_NORMALMAP"); }
        var mask = Resources.Load<Texture2D>("Models/trim_mask");
        if (mask != null && full) { m.SetTexture("_MetallicGlossMap", mask); m.SetFloat("_Smoothness", 1f); m.SetFloat("_Metallic", 1f); m.EnableKeyword("_METALLICSPECGLOSSMAP"); }
        var emission = Resources.Load<Texture2D>("Models/trim_emission");
        if (emission != null) { m.SetTexture("_EmissionMap", emission); m.SetColor("_EmissionColor", Color.white * 2f); m.EnableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; }
        trim = m;
        return m;
    }

    static Material standInMat;
    static GameObject StandIn(string name, Vector3 size, bool centred)
    {
        if (standInMat == null) standInMat = RigDesign.Unlit(new Color(0.9f, 0.62f, 0.2f));
        var go = new GameObject("DecoStandIn:" + name);
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var col = box.GetComponent<Collider>();
        if (Application.isPlaying) Object.Destroy(col); else Object.DestroyImmediate(col);
        box.name = "StandIn";
        box.transform.SetParent(go.transform, false);
        box.transform.localPosition = new Vector3(0f, size.y * 0.5f, centred ? 0f : size.z * 0.5f);   // the base at the pivot; a wall piece reaches out along +Z, a floor piece is centred on it
        box.transform.localScale = size;
        box.GetComponent<MeshRenderer>().sharedMaterial = standInMat;
        return go;
    }

    static void SetLayer(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        for (int i = 0; i < t.childCount; i++) SetLayer(t.GetChild(i), layer);
    }

    /// What a build placed: "Deco: 240 facade_flute_2x4, 60 gate_post_shaft …" or "Deco: nothing placed (no pieces in Resources/Models)"
    public static string Report()
    {
        if (placed.Count == 0 && standIns == 0) return "Deco: nothing placed (no pieces in Resources/Models" + (StandIns ? "" : "; stand-ins off") + ")";
        var parts = new List<string>();
        foreach (var kv in placed) parts.Add(kv.Value + " " + kv.Key);
        if (standIns > 0) parts.Add(standIns + " stand-in(s)");
        return "Deco: " + string.Join(", ", parts);
    }

    /// Clears the tallies before a build
    public static void BeginBuild() { placed.Clear(); standIns = 0; }
}
