using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE CLOCKWORK MANNEQUIN, POSTER CUT (2026-10-16; the first cut 2026-10-12).
/// The House's staff are shop-window dummies the company gave clockwork, drawn
/// now the way a 1930s poster draws a figure (Docs/Reference: the couple at the
/// terrace rail): ELONGATED, built from a few FLAT PLANES with one lit side —
/// the shells are BOXES, not capsules; a torso is two stacked tapering blocks;
/// the hands are white-glove wedges; the head is a SMALL NEAR-BLACK GLASS OVAL
/// with the clockwork glowing inside it: the cyan mainspring and THE ESCAPEMENT
/// WHEEL, a cyan gear with the brass balance arm clicking across it — the
/// weakspot, exactly where and as big as the head always was. No other cyan on
/// the body. What stays of the first cut: the BRASS BALL JOINTS (single clean
/// spheres), the WIND-UP KEY in the back, the brass name plate, the spill of
/// gears from a cut (SpillGears, from Sever). Everything here is Look: parts are
/// primitives with their colliders stripped, hung under the pivots the
/// animators drive and the ragdoll copier knows (Enemy.BoneFor); materials are
/// shared statics NAMED FOR THE TOON PASS (see Flat/Rounded/Glow); nothing
/// touches a centre, an eye, a muzzle, a hitbox or a tell. The rig builders
/// (Enemy.BuildRig and the rest) call these helpers; what they build directly is
/// reshaped afterwards by EnemyShell (PosterBody, DoormanBody, BouncerBody).
/// </summary>
public partial class Enemy
{
    // ---------------------------------------------------------------- materials (shared per kind, never per instance)
    //
    // THE NAMING CONTRACT for the toon shader (the sibling's material functions route by name prefix):
    //   "Toon/Flat/…"     boxes and planes: hard edges, two-band shading, the HARD LINE bead draws the edges; no outline pass
    //   "Toon/Rounded/…"  spheres, capsules and drums: the inverted-hull OUTLINE pass is asked for by this prefix
    //   "Toon/Glow/…"     unlit emission (the escapement): no outline; _EmissionColor carries the glow for a shader with _Emission
    // A material made here is URP Lit/Unlit until the toon pass swaps the shader; the names are what it keys on.

    static Material Flat(string name, Color c, float metallic, float smoothness)
    {
        var m = Lit(c, metallic, smoothness);
        m.name = "Toon/Flat/" + name;
        return m;
    }

    static Material Rounded(string name, Color c, float metallic, float smoothness)
    {
        var m = Lit(c, metallic, smoothness);
        m.name = "Toon/Rounded/" + name;
        return m;
    }

    static Material Glow(string name, Color c)
    {
        var m = Unlit(c);
        m.name = "Toon/Glow/" + name;
        m.SetColor("_EmissionColor", c);   // a no-op on URP Unlit; read by the toon shader's _Emission once it is on
        return m;
    }

    static Material enamelMat, oxbloodShellMat, oxbloodDrumMat, steelMat, glassMat, glassDarkMat, brassMat, brassBallMat,
        creamMat, charcoalMat, charcoalDrumMat, lacquerMat, lacquerDomeMat, gloveMat, oliveMat, escapementMat;

    static readonly Color BrassColour = new Color(0.78f, 0.58f, 0.25f), CharcoalColour = new Color(0.085f, 0.085f, 0.1f),
        OxbloodColour = new Color(0.34f, 0.09f, 0.09f), LacquerColour = new Color(0.03f, 0.03f, 0.04f);

    /// Cream canvas: the dummy's own skin (the Concierge's near-white Porcelain is his: EnemyConcierge). Matte: a colour field.
    static Material Enamel() => enamelMat ??= Flat("Enamel", new Color(0.84f, 0.78f, 0.66f), 0.05f, 0.35f);
    /// Oxblood: the bell staff's shell (the page, the bellhop) — one hue, flat
    static Material OxbloodShell() => oxbloodShellMat ??= Flat("Oxblood", OxbloodColour, 0.05f, 0.45f);
    /// The same oxblood on a drum (the pillbox)
    static Material OxbloodDrum() => oxbloodDrumMat ??= Rounded("Oxblood", OxbloodColour, 0.05f, 0.45f);
    /// Blued steel: springs, the escapement's cock
    static Material Steel() => steelMat ??= Rounded("Steel", new Color(0.55f, 0.56f, 0.6f), 0.9f, 0.6f);
    /// Brass on a plane: the key, the name plate, the frogging bars
    static Material Brass() => brassMat ??= Flat("Brass", BrassColour, 0.9f, 0.7f);
    /// Brass on a ball: the joints
    static Material BrassBall() => brassBallMat ??= Rounded("Brass", BrassColour, 0.9f, 0.7f);
    /// Cream: a chest panel, the dummy's target
    static Material Cream() => creamMat ??= Flat("Cream", new Color(0.9f, 0.85f, 0.72f), 0.05f, 0.4f);
    /// Charcoal cloth: the coats, the trousers, the caps
    static Material Coat() => charcoalMat ??= Flat("Charcoal", CharcoalColour, 0.1f, 0.5f);
    /// The same charcoal on a drum (a cap's crown)
    static Material CoatDrum() => charcoalDrumMat ??= Rounded("Charcoal", CharcoalColour, 0.1f, 0.5f);
    /// Black lacquer on a plane: the shoes, a cap's peak
    static Material Lacquer() => lacquerMat ??= Flat("Lacquer", LacquerColour, 0.3f, 0.9f);
    /// Black lacquer on a dome or a drum: the bowler, a lamp body
    static Material LacquerDome() => lacquerDomeMat ??= Rounded("Lacquer", LacquerColour, 0.3f, 0.9f);
    /// The white glove
    static Material GloveWhite() => gloveMat ??= Flat("Glove", new Color(0.98f, 0.98f, 0.96f), 0.05f, 0.4f);
    /// Olive on a drum: the Works' doorman family (the signalman's cap crown)
    static Material OliveDrum() => oliveMat ??= Rounded("Olive", new Color(0.3f, 0.3f, 0.16f), 0.05f, 0.45f);
    /// THE ESCAPEMENT: hot cyan, so it still reads through the dark glass (the mainspring under it wears Enemy.WeakMat)
    static Material Escapement() => escapementMat ??= Glow("Escapement", new Color(0.5f, 3f, 3.6f));

    /// Clear glass with a hard highlight: the Stationmaster's cab and the moth's lens keep it (the breakable glass template; no shadow)
    static Material Glass() => glassMat ??= GlassLike("Glass", new Color(0.8f, 0.88f, 0.95f, 0.24f));
    /// THE HEAD'S GLASS: near-black, so the oval reads as a solid dark shape with the clockwork glowing inside it
    static Material GlassDark() => glassDarkMat ??= GlassLike("GlassDark", new Color(0.03f, 0.035f, 0.05f, 0.62f));

    static Material GlassLike(string name, Color c)
    {
        var m = LevelKit.FromTemplate("LitTransparent", "Universal Render Pipeline/Lit");
        m.name = "Toon/Rounded/" + name;
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_SrcBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
        m.SetInt("_DstBlendAlpha", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        m.SetShaderPassEnabled("ShadowCaster", false);
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_Smoothness", 0.94f);
        return m;
    }

    // ---------------------------------------------------------------- the parts

    /// An enamel shell segment, POSTER CUT: a BOX `size` = (width, length, depth) — a limb or body piece in flat planes
    static Renderer Shell(Transform parent, Vector3 at, Vector3 size, Material mat, string name)
        => Part(parent, PrimitiveType.Cube, at, size, mat, name);

    /// A shell lying along z (a body, a fuselage): `size` = (width, length, height); the same extents the capsule had
    static Renderer ShellZ(Transform parent, Vector3 at, Vector3 size, Material mat, string name)
        => Part(parent, PrimitiveType.Cube, at, new Vector3(size.x, size.z, size.y), mat, name);

    /// A brass ball joint, `d` across: one clean sphere between two blocks, the articulation read of the clockwork
    static Renderer Ball(Transform parent, Vector3 at, float d, string name)
        => Part(parent, PrimitiveType.Sphere, at, Vector3.one * d, BrassBall(), name);

    /// The hand: ONE white-glove WEDGE off the wrist — a flat plane tilted to the fingers (the fingers are painted on it)
    static Renderer Hand(Transform arm, Vector3 at, float w)
        => Turned(Part(arm, PrimitiveType.Cube, at + new Vector3(0f, -0.02f, 0.012f), new Vector3(w, w * 1.5f, w * 0.42f), GloveWhite(), "Hand"), 12f);

    /// THE WIND-UP KEY on the back: a brass stem out of the shell (local -z) with a bar bow on the end, on a pivot
    /// MannequinTick turns slowly about the stem. `at` is where the stem leaves the shell. Two renderers.
    static Transform Key(Rig rig, Transform parent, Vector3 at, float size) => Key(rig, parent, at, size, Vector3.back);

    /// ...and one pointing `dir` (a key out of the top of a disc, a fuselage): the mount holds the direction, the key
    /// inside it turns
    static Transform Key(Rig rig, Transform parent, Vector3 at, float size, Vector3 dir)
    {
        Transform mount = Pivot(parent, "KeyMount", at);
        mount.localRotation = Quaternion.FromToRotation(Vector3.back, dir);
        Transform key = Pivot(mount, "Key", Vector3.zero);
        Part(key, PrimitiveType.Cylinder, new Vector3(0f, 0f, -size * 0.5f), new Vector3(size * 0.2f, size * 0.5f, size * 0.2f), BrassBall(), "KeyStem").transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Part(key, PrimitiveType.Cube, new Vector3(0f, 0f, -size * 1.05f), new Vector3(size * 0.9f, size * 0.3f, size * 0.1f), Brass(), "KeyBow");   // a bar, so the turn is seen
        rig.key = key;
        return key;
    }

    /// THE GLASS HEAD, the weakspot, POSTER CUT: a small NEAR-BLACK GLASS OVAL (the jar: 0.84 wide, 1.04 tall of `size`)
    /// with the clockwork glowing inside it — THE MAINSPRING (rig.head: a cyan ball at half size, its collider grown back
    /// out to the old head's world radius, so the hitbox is the sphere it always was) and, with `gear`, THE ESCAPEMENT
    /// WHEEL: a cyan disc facing forward (rig.ring: what flares when the fist's window rests on it — it fills the oval)
    /// with the brass balance arm clicking across it (rig.balance). Three renderers; four with the gear.
    static void GlassCell(Rig rig, Transform pivot, float size, Vector3 at, bool gear)
    {
        float glow = size * 0.5f;
        Renderer head = Part(pivot, PrimitiveType.Sphere, at, Vector3.one * glow, WeakMat(), "Head", keepCollider: true);
        head.GetComponent<SphereCollider>().radius = 0.5f * size / glow;   // the same world radius as a head of `size`
        head.gameObject.AddComponent<EnemyWeakspot>();
        rig.head = head;
        var jar = Part(pivot, PrimitiveType.Sphere, at, new Vector3(size * 0.84f, size * 1.04f, size * 0.84f), GlassDark(), "Jar");
        jar.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        if (gear)   // named as the halo was: the ragdoll copier and the mark skip it by that name, as they skipped the halo
            rig.ring = Turned(Part(pivot, PrimitiveType.Cylinder, at, new Vector3(size * 0.62f, 0.006f, size * 0.62f), Escapement(), "WeakspotRing"), 70f).transform;
        rig.balance = Pivot(pivot, "Balance", at + new Vector3(0f, 0f, size * 0.2f));
        Part(rig.balance, PrimitiveType.Cube, Vector3.zero, new Vector3(size * 0.7f, size * 0.05f, size * 0.025f), Brass(), "BalanceArm");   // the balance, a bar: its tick is seen
    }

    /// The civic head: the glass oval with the escapement in it; rig.ring = the gear. Four renderers. The collar and the
    /// halo of the first cut went with the poster cut (the oval sits straight on the squared shoulders, like the poster
    /// figure's); the parameters stay for the callers.
    static void GlassHead(Rig rig, Transform pivot, float size, float collarY = -0.4f, float collarD = 0.8f, float haloScale = 2.14f)
        => GlassCell(rig, pivot, size, Vector3.zero, true);

    /// The glass cell in a CAGE of the unit's own shape (the response and variety way): rig.ring is an empty pivot the
    /// caller hangs its bars on, and that is what flares. Three renderers plus the bars.
    static void GlassCage(Rig rig, Transform pivot, float size)
    {
        GlassCell(rig, pivot, size, Vector3.zero, false);
        rig.ring = Pivot(pivot, "Cage", Vector3.zero);
    }

    /// The brass name plate on the chest and the company line engraved on it ("HALCYON · BELLHOP"), readable from the
    /// front (+z): `at` is the plate's centre, just proud of the shell. Two renderers.
    static void NamePlate(Transform torso, Vector3 at, float width, EnemyKind kind, float textSize = 0.011f)
    {
        Part(torso, PrimitiveType.Cube, at, new Vector3(width, 0.05f, 0.01f), Brass(), "NamePlate");
        LevelKit.Text(torso, torso.TransformPoint(at + new Vector3(0f, 0f, 0.007f)), torso.rotation * Quaternion.Euler(0f, 180f, 0f),
            Plate(kind), textSize, new Color(0.08f, 0.07f, 0.06f), "ChestLabel", width * 0.92f, true, 18f, 40f); // dark on the brass
    }

    /// A peaked company cap: the crown (a flat drum) and the lacquer peak out the front (+z). `at` is the crown's bottom
    /// centre. Two renderers (the band went: the peak says what it is).
    static void Cap(Transform parent, Vector3 at, float radius, Material crown = null)
    {
        float r2 = radius * 2f;
        Part(parent, PrimitiveType.Cylinder, at + new Vector3(0f, 0.04f, 0f), new Vector3(r2 + 0.02f, 0.04f, r2 + 0.02f), crown ?? CoatDrum(), "CapCrown");
        Turned(Part(parent, PrimitiveType.Cube, at + new Vector3(0f, 0.012f, radius * 0.95f), new Vector3(r2 * 0.8f, 0.01f, radius * 0.8f), Lacquer(), "CapPeak"), 10f);
    }

    /// A mannequin's bow tie: one lacquer piece at the collar (`at` just proud of the shell's front)
    static void Tie(Transform parent, Vector3 at, float width, Material cloth)
        => Part(parent, PrimitiveType.Cube, at, new Vector3(width, width * 0.3f, 0.016f), cloth, "BowTie");

    // ---------------------------------------------------------------- the tick (look only)

    /// Angles snap to `step` degrees: a mannequin moves in clicks (the gait's legs, a scanning head)
    static float Click(float deg, float step) => Mathf.Round(deg / step) * step;

    /// Every frame a unit animates: the key turns, the balance arm ticks across the gear (two beats a second, like a clock in a hall)
    void MannequinTick(float now)
    {
        if (rig.key != null) rig.key.localRotation = Quaternion.Euler(0f, 0f, now * 40f);
        if (rig.balance != null) rig.balance.localRotation = Quaternion.Euler(0f, 0f, Click(Mathf.Sin(now * 12.57f) * 36f, 12f));
    }

    // ---------------------------------------------------------------- the spill

    /// Gears and springs out of a cut: small brass wheels and blued coils thrown off the severed joint, gone in a
    /// second (ShardFade). Never a plate: a mannequin's insides are clockwork, not armour.
    static void SpillGears(Vector3 at, Vector3 n, int count)
    {
        if (GameFeel.Instance != null) GameFeel.Instance.PlayGearSpill(); // one soft scatter per spill, never per piece (rate-limited there)
        for (int i = 0; i < count; i++)
        {
            bool spring = i % 3 == 2;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = spring ? "Spring" : "Gear";
            go.layer = 2;   // the primitive's own collider stays: they bounce on the floor like the boss's shed plates
            go.transform.position = at + Random.insideUnitSphere * 0.08f;
            go.transform.rotation = Random.rotation;
            float s = Random.Range(0.05f, 0.11f);
            go.transform.localScale = spring ? new Vector3(s * 0.35f, s * 0.9f, s * 0.35f) : new Vector3(s, s * 0.12f, s);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = spring ? Steel() : BrassBall();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.05f;
            rb.linearVelocity = n * (Random.Range(-1f, 1f) * 3f) + Random.insideUnitSphere * 2.5f + Vector3.up * Random.Range(2f, 4.5f);
            rb.angularVelocity = Random.insideUnitSphere * 30f;
            go.AddComponent<ShardFade>();
        }
    }
}
