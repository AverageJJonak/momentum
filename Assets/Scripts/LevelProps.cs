using UnityEngine;

/// A lit enamel billboard, DARK DECO: a 1939 sign box with a steady warm backlight
/// (cream, amber or sepia, one per box) carrying the company's mark and words —
/// and every so often the box glitches: the backlight goes to red static and the
/// words flicker into what the city really says, then it settles. The tell that
/// this perfect city is running on something rotten underneath. No drift, no
/// jolt: a lamp behind glass. The emblem beside the words is a separate static
/// piece (LevelKit.Billboard) and never glitches.
public class Hologram : MonoBehaviour
{
    static readonly Color[] Tones =
    {
        new Color(1.9f, 1.65f, 1.25f), new Color(1.8f, 1.35f, 0.75f), new Color(1.7f, 1.5f, 1.1f),
        new Color(1.6f, 1.2f, 0.7f), new Color(2.0f, 1.75f, 1.4f), new Color(1.5f, 1.1f, 0.65f),
    };
    static readonly Color Static = new Color(2.4f, 0.15f, 0.1f);

    Material mat;
    Color tone;
    float glitchUntil, nextJitter;
    Vector3 basePos;
    // The words on the panel, and what they say when the glitch lets the truth through
    TextMesh mark, tag;
    string markText, tagText;
    float markSize, tagSize, markMaxW, tagMaxW;
    bool corrupted, locked, glitching;

    /// Seize the panel for good: red, still, and saying what it really says
    public void Lock(string wordmark, string tagline)
    {
        locked = true;
        if (mat != null) mat.SetColor("_BaseColor", Static * 0.8f);
        transform.position = basePos;
        if (mark != null) { mark.text = wordmark; mark.color = new Color(1f, 0.15f, 0.1f); LevelKit.Fit(mark, markSize, markMaxW); }
        if (tag != null) { tag.text = tagline; tag.color = new Color(1f, 0.15f, 0.1f); LevelKit.Fit(tag, tagSize, tagMaxW); }
    }

    public void Init(Material material, TextMesh wordmark = null, TextMesh tagline = null, float wordmarkMaxWidth = 0f, float taglineMaxWidth = 0f)
    {
        mat = material;
        mark = wordmark;
        tag = tagline;
        markMaxW = wordmarkMaxWidth;
        tagMaxW = taglineMaxWidth;
        if (mark != null) { markText = mark.text; markSize = mark.characterSize; }
        if (tag != null) { tagText = tag.text; tagSize = tag.characterSize; }
        tone = Tones[Random.Range(0, Tones.Length)];
        basePos = transform.position;
        mat.SetColor("_BaseColor", tone);
    }

    void Update()
    {
        if (mat == null || locked) return;
        float now = Time.time;
        if (now < glitchUntil)
        {
            glitching = true;
            if (now >= nextJitter)
            {
                nextJitter = now + 0.03f;
                bool dark = Random.value > 0.6f;
                mat.SetColor("_BaseColor", dark ? Static * 0.25f : Static);
                if (!corrupted && Random.value > 0.5f) Corrupt(true);
            }
            return;
        }
        if (glitching)
        {
            // The box settles: its own light back, its own words back — and nothing touched until the next time
            glitching = false;
            mat.SetColor("_BaseColor", tone);
            if (corrupted) Corrupt(false);
        }
        if (Random.value < Time.deltaTime * 0.07f) glitchUntil = now + Random.Range(0.08f, 0.35f);
    }

    /// The glitch shows what the city really says; the box's own light returns and so do the slogans
    void Corrupt(bool on)
    {
        corrupted = on;
        if (mark != null)
        {
            mark.text = on ? LevelKit.Corruptions[Random.Range(0, LevelKit.Corruptions.Length)] : markText;
            mark.color = on ? new Color(1f, 0.15f, 0.1f) : new Color(0.07f, 0.07f, 0.1f);
            LevelKit.Fit(mark, markSize, markMaxW); // a longer truth still fits the panel
        }
        if (tag != null)
        {
            tag.text = on ? LevelKit.Corruptions[Random.Range(0, LevelKit.Corruptions.Length)] : tagText;
            tag.color = on ? new Color(1f, 0.15f, 0.1f) : new Color(0.07f, 0.07f, 0.1f);
            LevelKit.Fit(tag, tagSize, tagMaxW);
        }
    }
}

/// An emergency lamp that never quite holds steady
public class FlickerLight : MonoBehaviour
{
    Light lamp;
    float baseIntensity, seed, outUntil;

    void Start()
    {
        lamp = GetComponent<Light>();
        baseIntensity = lamp != null ? lamp.intensity : 0f;
        seed = Random.value * 100f;
    }

    void Update()
    {
        if (lamp == null) return;
        float now = Time.time;
        if (now < outUntil) { lamp.intensity = 0f; return; }
        if (Random.value < Time.deltaTime * 0.15f) outUntil = now + Random.Range(0.04f, 0.25f);
        lamp.intensity = baseIntensity * (0.55f + 0.45f * Mathf.PerlinNoise(now * 8f, seed));
    }
}

/// A cloud drifting across an open sky, wrapping around its lane
public class CloudDrift : MonoBehaviour
{
    public float speed = 0.5f, lane = 40f;
    public Vector3 origin;

    void Update()
    {
        var p = transform.position;
        p.x += speed * Time.deltaTime;
        if (p.x > origin.x + lane) p.x -= lane * 2f;
        transform.position = p;
    }
}

/// A drone on patrol: drifts in a slow loop round its post, bobbing, turning into its travel
public class DroneDrift : MonoBehaviour
{
    public Vector3 origin;
    public float radius = 4f, speed = 0.35f, phase;

    void Update()
    {
        float t = Time.time * speed + phase;
        Vector3 p = origin + new Vector3(Mathf.Cos(t) * radius, Mathf.Sin(t * 2.3f) * 0.4f, Mathf.Sin(t) * radius * 0.6f);
        Vector3 d = p - transform.position;
        if (d.sqrMagnitude > 1e-4f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d.normalized, Vector3.up) * Quaternion.Euler(8f, 0f, 0f), 1f - Mathf.Exp(-3f * Time.deltaTime));
        transform.position = p;
    }
}

/// A light that breathes: modulates one renderer's base colour through a property block, never the shared material
public class PulseGlow : MonoBehaviour
{
    public float rate = 1.2f, depth = 0.25f, phase;
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    Renderer target;
    MaterialPropertyBlock block;
    Color baseColor;

    void Start()
    {
        target = GetComponent<Renderer>();
        if (target == null) { enabled = false; return; }
        block = new MaterialPropertyBlock();
        var m = target.sharedMaterial;
        baseColor = m != null && m.HasProperty(BaseColor) ? m.GetColor(BaseColor) : Color.white;
    }

    void Update()
    {
        float k = 1f - depth * 0.5f + depth * 0.5f * Mathf.Sin(Time.time * rate * 6.2832f + phase);
        block.SetColor(BaseColor, new Color(baseColor.r * k, baseColor.g * k, baseColor.b * k, baseColor.a));
        target.SetPropertyBlock(block);
    }
}

/// <summary>
/// A sprite that turns to face the camera every frame: a painted cloud reads
/// as a body from wherever you look at it. The quad's face is its -Z, so the
/// object's forward points away from the camera.
/// </summary>
public class Billboard : MonoBehaviour
{
    static Camera cam;
    static int camFrame = -1;

    void LateUpdate()
    {
        if (camFrame != Time.frameCount) { cam = Camera.main; camFrame = Time.frameCount; }
        if (cam == null) return;
        Vector3 d = transform.position - cam.transform.position;
        if (d.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.LookRotation(d, Vector3.up);
    }
}

/// <summary>
/// Water that moves: the sheet's texture (and with it the ripple normal map,
/// which shares its tiling) drifts a few centimetres a second. The material is
/// the slab's shared tiled instance, so every pool of one size moves as one.
/// </summary>
public class WaterFlow : MonoBehaviour
{
    public Vector2 speed = new Vector2(0.02f, 0.013f);
    static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    Material mat;

    void Start()
    {
        var r = GetComponent<Renderer>();
        mat = r != null ? r.sharedMaterial : null;
    }

    void Update()
    {
        if (mat == null) return;
        float t = Time.time;
        mat.SetTextureOffset(BaseMap, new Vector2(Mathf.Repeat(t * speed.x, 1f), Mathf.Repeat(t * speed.y, 1f)));
    }
}

/// <summary>
/// A searchlight head sweeping the sky: a slow yaw back and forth round its
/// post, never quite the same arc twice. The beam and the lamp hang under it.
/// </summary>
public class SearchlightSweep : MonoBehaviour
{
    public float phase, rate = 0.11f, swing = 70f;
    float baseYaw;

    void Start() => baseYaw = transform.eulerAngles.y;

    void Update()
    {
        float t = Time.time * rate + phase;
        float yaw = baseYaw + Mathf.Sin(t * 6.2832f) * swing + Mathf.Sin(t * 2.7f) * 12f;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }
}

/// <summary>
/// The star dome rides with the camera, so the stars are always at the same
/// distance and the towers always stand in front of them.
/// </summary>
public class StarDome : MonoBehaviour
{
    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam != null) transform.position = cam.transform.position;
    }
}
