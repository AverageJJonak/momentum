using UnityEngine;

/// <summary>
/// Karlson-style glass: a pane you can only pass by CRASHING through it.
/// Below glassBreakSpeed (measured straight into the pane) it's a wall — you
/// thud, it flashes, the HUD says how fast you needed to be. At or above it the
/// pane shatters in your direction, you keep most of your speed, and it pays
/// style. The fast routes hide behind glass: the shortcut is the one you earn.
/// PlayerMotor.MoveAndSlide calls TryBreak on every capsule-cast hit.
/// </summary>
public class BreakableGlass : MonoBehaviour
{
    public MovementConfig config;

    static Material paneMat, flashMat, frameMat, shardMat;
    Renderer pane;
    Collider col;
    Vector3 size;
    float lastBumpAt = -10f;
    float flashUntil;
    bool broken;

    public static BreakableGlass Create(Transform parent, Vector3 center, Vector3 size, MovementConfig config)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Glass";
        go.transform.SetParent(parent);
        go.transform.position = center;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = PaneMat();
        var g = go.AddComponent<BreakableGlass>();
        g.config = config;
        g.size = size;
        g.pane = go.GetComponent<Renderer>();
        g.col = go.GetComponent<Collider>();
        Frame(parent, center, size);
        return g;
    }

    /// Black bars around the pane's edges — the frame stays when the glass goes
    static void Frame(Transform parent, Vector3 c, Vector3 s)
    {
        const float w = 0.3f;
        Material m = FrameMat();
        if (s.z <= s.x && s.z <= s.y)          // pane in the XY plane (faces ±z)
        {
            Bar(parent, c + Vector3.up * (s.y * 0.5f + w * 0.5f), new Vector3(s.x + 2f * w, w, s.z + 0.2f), m);
            Bar(parent, c - Vector3.up * (s.y * 0.5f + w * 0.5f), new Vector3(s.x + 2f * w, w, s.z + 0.2f), m);
            Bar(parent, c + Vector3.right * (s.x * 0.5f + w * 0.5f), new Vector3(w, s.y, s.z + 0.2f), m);
            Bar(parent, c - Vector3.right * (s.x * 0.5f + w * 0.5f), new Vector3(w, s.y, s.z + 0.2f), m);
        }
        else if (s.x <= s.y && s.x <= s.z)     // pane in the YZ plane (faces ±x)
        {
            Bar(parent, c + Vector3.up * (s.y * 0.5f + w * 0.5f), new Vector3(s.x + 0.2f, w, s.z + 2f * w), m);
            Bar(parent, c - Vector3.up * (s.y * 0.5f + w * 0.5f), new Vector3(s.x + 0.2f, w, s.z + 2f * w), m);
            Bar(parent, c + Vector3.forward * (s.z * 0.5f + w * 0.5f), new Vector3(s.x + 0.2f, s.y, w), m);
            Bar(parent, c - Vector3.forward * (s.z * 0.5f + w * 0.5f), new Vector3(s.x + 0.2f, s.y, w), m);
        }
        else                                   // glass floor / ceiling (faces ±y)
        {
            Bar(parent, c + Vector3.forward * (s.z * 0.5f + w * 0.5f), new Vector3(s.x + 2f * w, s.y + 0.2f, w), m);
            Bar(parent, c - Vector3.forward * (s.z * 0.5f + w * 0.5f), new Vector3(s.x + 2f * w, s.y + 0.2f, w), m);
            Bar(parent, c + Vector3.right * (s.x * 0.5f + w * 0.5f), new Vector3(w, s.y + 0.2f, s.z), m);
            Bar(parent, c - Vector3.right * (s.x * 0.5f + w * 0.5f), new Vector3(w, s.y + 0.2f, s.z), m);
        }
    }

    static void Bar(Transform parent, Vector3 center, Vector3 size, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "GlassFrame";
        go.transform.SetParent(parent);
        go.transform.position = center;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = m;
    }

    /// Motor hook: `velocity` is the player's, `normal` the pane surface hit.
    /// True = shattered, the motor may continue through.
    public bool TryBreak(Vector3 velocity, Vector3 normal, Vector3 point)
    {
        if (broken || config == null) return false;
        float into = -Vector3.Dot(velocity, normal);
        if (into >= config.glassBreakSpeed)
        {
            Shatter(into, velocity.normalized);
            return true;
        }
        if (into >= 4f && Time.time - lastBumpAt > 0.8f)
        {
            // Too slow: thud, flash, and tell them the number
            lastBumpAt = Time.time;
            flashUntil = Time.time + 0.15f;
            pane.sharedMaterial = FlashMat();
            if (GameFeel.Instance != null) GameFeel.Instance.PlayThunk();
            MovementEvents.Emit(MoveType.GlassBump, $"{into:0} / {config.glassBreakSpeed:0} m/s");
        }
        return false;
    }

    void Update()
    {
        if (!broken && flashUntil > 0f && Time.time >= flashUntil)
        {
            flashUntil = 0f;
            pane.sharedMaterial = PaneMat();
        }
    }

    void Shatter(float speed, Vector3 dir)
    {
        broken = true;
        col.enabled = false;
        pane.enabled = false;

        // Shards carry on with your momentum
        float thin = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
        float area = size.x * size.y * size.z / Mathf.Max(0.01f, thin);
        int count = Mathf.Clamp(Mathf.RoundToInt(area * 1.4f), 16, 48);
        for (int i = 0; i < count; i++)
        {
            var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard.name = "GlassShard";
            shard.layer = 2; // Ignore Raycast: invisible to every gameplay cast
            Vector3 local = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f));
            shard.transform.position = transform.TransformPoint(local);
            shard.transform.rotation = Random.rotation;
            shard.transform.localScale = new Vector3(Random.Range(0.15f, 0.5f), Random.Range(0.15f, 0.5f), 0.05f);
            shard.GetComponent<MeshRenderer>().sharedMaterial = ShardMat();
            var rb = shard.AddComponent<Rigidbody>();
            rb.mass = 0.05f;
            rb.linearVelocity = dir * Random.Range(speed * 0.3f, speed * 0.8f) + Random.insideUnitSphere * 3f;
            rb.angularVelocity = Random.insideUnitSphere * 15f;
            shard.AddComponent<ShardFade>();
        }

        MovementEvents.Emit(MoveType.GlassBreak, $"{speed:0.0} m/s");
        var director = CombatDirector.Instance;
        if (director != null && director.style != null) director.style.AddBonus(config.glassBreakStyleBonus);
        Destroy(gameObject, 0.1f);
    }

    static Material PaneMat() => paneMat ??= Transparent(new Color(0.92f, 0.88f, 0.78f, 0.3f));
    static Material FlashMat() => flashMat ??= Transparent(new Color(1f, 1f, 1f, 0.75f));

    static Material FrameMat()
    {
        if (frameMat == null)
        {
            frameMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            frameMat.SetColor("_BaseColor", new Color(0.08f, 0.08f, 0.1f));
            frameMat.SetFloat("_Smoothness", 0.5f);
        }
        return frameMat;
    }

    static Material ShardMat()
    {
        if (shardMat == null)
        {
            shardMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            shardMat.SetColor("_BaseColor", new Color(1.8f, 1.6f, 1.3f));
        }
        return shardMat;
    }

    /// URP Lit switched to alpha-blended transparency at runtime
    static Material Transparent(Color c)
    {
        var m = LevelKit.FromTemplate("LitTransparent", "Universal Render Pipeline/Lit");
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
        m.SetFloat("_Smoothness", 0.95f);
        return m;
    }
}
