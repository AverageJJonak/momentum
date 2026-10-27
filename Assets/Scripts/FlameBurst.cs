using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// THE BURST. Where a burning arrow stops, the room goes up — in three
/// dimensions: a white-hot core that blows out and collapses in a tenth of a
/// second, a fireball swelling through it and burning off, fourteen tongues
/// of flame thrown out on every axis, three shock rings on random planes so
/// the blast reads as a sphere from any angle, a dark smoke shell rising
/// through all of it, and a light that dies in a third of a second. Boxes,
/// spheres and cylinders with no colliders, on the raycast-ignore layer, gone
/// inside a second. Gold fire and white heat — never the danger red.
/// </summary>
public class FlameBurst : MonoBehaviour
{
    static Material fireMat, coreMat, smokeMat;
    static readonly MaterialPropertyBlock mpb = new MaterialPropertyBlock();
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    float born, radius;
    Renderer core, fireball, smoke;
    Renderer[] tongues, rings;
    Vector3[] dirs;
    float[] stretch;
    Light glow;

    public static void Spawn(Vector3 at, float radius)
    {
        if (fireMat == null)
        {
            fireMat = Blended(new Color(2.6f, 1.2f, 0.3f, 1f), true);   // gold fire
            coreMat = Blended(new Color(3f, 2.4f, 1.2f, 1f), true);     // white heat
            smokeMat = Blended(new Color(0.1f, 0.06f, 0.05f, 0.5f), false);
        }
        var go = new GameObject("FlameBurst") { layer = 2 };
        go.transform.position = at;
        var b = go.AddComponent<FlameBurst>();
        b.radius = Mathf.Max(0.5f, radius);
        b.born = Time.time;
        Transform t = go.transform;
        b.core = Piece(t, at, PrimitiveType.Sphere, coreMat, "Core");
        b.fireball = Piece(t, at, PrimitiveType.Sphere, fireMat, "Fireball");
        b.smoke = Piece(t, at, PrimitiveType.Sphere, smokeMat, "Smoke");
        // Tongues of flame on every axis — uniform over the sphere, so the burst has no flat side
        const int n = 14;
        b.tongues = new Renderer[n];
        b.dirs = new Vector3[n];
        b.stretch = new float[n];
        for (int i = 0; i < n; i++)
        {
            b.dirs[i] = Random.onUnitSphere;
            b.stretch[i] = Random.Range(0.8f, 1.25f);
            b.tongues[i] = Piece(t, at, PrimitiveType.Cube, i % 3 == 0 ? coreMat : fireMat, "Tongue");
        }
        // Three shock rings on random planes: a cage of light, not a disc
        b.rings = new Renderer[3];
        for (int i = 0; i < 3; i++)
        {
            b.rings[i] = Piece(t, at, PrimitiveType.Cylinder, fireMat, "Ring");
            b.rings[i].transform.rotation = Random.rotationUniform;
        }
        var lightGo = new GameObject("Glow");
        lightGo.transform.SetParent(t, false);
        b.glow = lightGo.AddComponent<Light>();
        b.glow.type = LightType.Point;
        b.glow.color = new Color(1f, 0.62f, 0.25f);
        b.glow.range = b.radius * 2.5f;
        b.glow.intensity = 9f;
        b.glow.shadows = LightShadows.None;
        b.Update();
    }

    static Renderer Piece(Transform parent, Vector3 at, PrimitiveType type, Material mat, string name)
    {
        var go = LevelKit.Prim(parent, at, Vector3.one * 0.01f, mat, name, type);
        go.layer = 2;
        return go.GetComponent<Renderer>();
    }

    static float Out(float k) => 1f - (1f - k) * (1f - k);

    void Update()
    {
        float age = Time.time - born;
        if (age >= 0.95f) { Destroy(gameObject); return; }
        float r = radius;
        Vector3 at = transform.position;

        // The core: out in a tenth of a second, collapsed by three tenths
        float grow = Out(Mathf.Clamp01(age / 0.12f)), die = Mathf.Clamp01((age - 0.12f) / 0.2f);
        Set(core, at, Vector3.one * (r * 0.9f * grow * (1f - die) + 0.01f), Quaternion.identity, coreMat, 1f - Mathf.Clamp01(age / 0.3f));

        // The fireball: swells through the core and burns off
        float fk = Mathf.Clamp01(age / 0.35f);
        float fade = 1f - Mathf.Clamp01(age / 0.6f);
        Set(fireball, at, Vector3.one * (r * Mathf.Lerp(0.3f, 1.7f, Out(fk))), Quaternion.identity, fireMat, 0.9f * fade * fade);

        // The smoke: a dark shell, wider and slower, rising as it thins
        float sk = Mathf.Clamp01(age / 0.9f);
        Set(smoke, at + Vector3.up * (age * 1.2f), Vector3.one * (r * Mathf.Lerp(0.5f, 2.3f, Out(sk))), Quaternion.identity, smokeMat, 0.5f * (1f - sk));

        // The tongues: thrown out along their axes, thinning as they go
        float tk = Mathf.Clamp01(age / 0.3f), talpha = 1f - Mathf.Clamp01(age / 0.5f);
        for (int i = 0; i < tongues.Length; i++)
        {
            float len = r * Mathf.Lerp(0.2f, 1.9f, Out(tk)) * stretch[i];
            float thick = r * 0.14f * (1f - Mathf.Clamp01(age / 0.6f)) + 0.01f;
            Set(tongues[i], at + dirs[i] * (len * 0.5f), new Vector3(thick, thick, len), Quaternion.LookRotation(dirs[i]), i % 3 == 0 ? coreMat : fireMat, talpha);
        }

        // The rings: three planes of shock, expanding and fading
        float rk = Mathf.Clamp01(age / 0.45f);
        float ring = r * Mathf.Lerp(0.4f, 1.45f, Out(rk));
        for (int i = 0; i < rings.Length; i++)
            Set(rings[i], at, new Vector3(ring * 2f, 0.06f, ring * 2f), rings[i].transform.rotation, fireMat, 0.9f * (1f - rk));

        if (glow != null) glow.intensity = 9f * (1f - Mathf.Clamp01(age / 0.35f));
    }

    /// One piece's frame: where, how big, which way, and how much of its colour is left
    static void Set(Renderer piece, Vector3 pos, Vector3 scale, Quaternion rot, Material mat, float alpha)
    {
        if (piece == null) return;
        piece.transform.SetPositionAndRotation(pos, rot);
        piece.transform.localScale = scale;
        Color c = mat.GetColor(BaseColor);
        c.a *= Mathf.Clamp01(alpha);
        mpb.SetColor(BaseColor, c);
        piece.SetPropertyBlock(mpb);
    }

    /// Unlit and see-through: additive for the fire, alpha for the smoke
    static Material Blended(Color c, bool additive)
    {
        var m = LevelKit.FromTemplate("UnlitTransparent", "Universal Render Pipeline/Unlit");
        m.SetColor(BaseColor, c);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", additive ? 2f : 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
        m.SetInt("_ZWrite", 0);
        m.SetInt("_Cull", (int)CullMode.Off);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        return m;
    }
}
