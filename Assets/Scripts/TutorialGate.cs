using System.Collections.Generic;
using UnityEngine;

public enum GateMode { RecentMove, Sliding, Dashing, Slamming, GrapplePulling }

/// <summary>
/// A tutorial checkpoint: floating text + arrows over a volume. Pass through it
/// while performing the required move and every visual piece shatters into glass
/// shards; pass through wrong and the text flashes red with a dull thunk.
/// Volume overlap is checked manually against the motor (no Rigidbody needed).
/// </summary>
public class TutorialGate : MonoBehaviour
{
    public MovementConfig config;
    public MoveType requiredMove;
    public GateMode mode;
    public Vector3 size;
    public GameObject[] visualPieces;
    public string requiredDetail; // e.g. "EXECUTE": the move must also have come with this detail

    public static PlayerMotor Motor;
    public static GrappleAbility Grapple;

    static readonly List<TutorialGate> All = new List<TutorialGate>();
    static readonly Dictionary<MoveType, float> lastMoveTime = new Dictionary<MoveType, float>();
    static readonly Dictionary<MoveType, string> lastDetail = new Dictionary<MoveType, string>();

    bool broken;
    bool playerInside;
    bool missFlashed;
    TextMesh text;
    Color textColor;
    float redUntil;

    void OnEnable()
    {
        All.Add(this);
        MovementEvents.MoveExecuted += Track;
        FindText();
    }

    // OnEnable runs inside AddComponent, before the builder hands over the pieces: look again in Start
    void Start() => FindText();

    void FindText()
    {
        if (text != null || visualPieces == null) return;
        foreach (GameObject piece in visualPieces)
        {
            text = piece != null ? piece.GetComponent<TextMesh>() : null;
            if (text != null) { textColor = text.color; break; }
        }
    }

    void OnDisable()
    {
        All.Remove(this);
        MovementEvents.MoveExecuted -= Track;
    }

    static void Track(MoveType type, string detail)
    {
        lastMoveTime[type] = Time.time;
        lastDetail[type] = detail;
    }

    /// Called on tutorial build so stale pre-scene-load moves can't pre-open a gate
    public static void ClearRecency()
    {
        lastMoveTime.Clear();
        lastDetail.Clear();
    }

    bool Recent(MoveType type)
        => lastMoveTime.TryGetValue(type, out float t)
           && Time.time - t <= config.gateRecentMoveWindow;

    bool DetailOk(MoveType type)
        => string.IsNullOrEmpty(requiredDetail)
           || (lastDetail.TryGetValue(type, out string d) && d != null && d.StartsWith(requiredDetail));

    void FixedUpdate()
    {
        if (broken || Motor == null) return;

        Vector3 chest = Motor.CurrPosition + Vector3.up * 0.9f;
        bool inside = new Bounds(transform.position, size).Contains(chest);

        if (inside)
        {
            // Re-check every tick while inside: the correct move can land
            // mid-gate (a parry an instant before the wall, a timed bhop)
            if (CheckPassed())
            {
                Shatter();
            }
            else if (!playerInside && !missFlashed)
            {
                missFlashed = true;
                FlashRed();
            }
        }
        else
        {
            missFlashed = false;
        }
        playerInside = inside;
    }

    void Update()
    {
        if (text != null && redUntil > 0f && Time.time >= redUntil)
        {
            text.color = textColor;
            redUntil = 0f;
        }
    }

    bool CheckPassed() => mode switch
    {
        GateMode.Sliding => Motor.Sliding,
        GateMode.Dashing => Motor.Dashing || Recent(MoveType.Dash),
        GateMode.Slamming => Motor.Slamming,
        GateMode.GrapplePulling => (Grapple != null && Grapple.IsPulling) || Recent(MoveType.GrappleYank),
        _ => (Recent(requiredMove) && DetailOk(requiredMove))
             || (requiredMove == MoveType.Bhop && Recent(MoveType.PerfectBhop))
             || (requiredMove == MoveType.SlideJump && Recent(MoveType.PerfectSlideJump))
             || (requiredMove == MoveType.WallKick && Recent(MoveType.EdgeBoost)),   // a kick on a corner is still a kick
    };

    void FlashRed()
    {
        if (text != null)
        {
            text.color = new Color(0.95f, 0.25f, 0.2f);
            redUntil = Time.time + 0.5f;
        }
        if (GameFeel.Instance != null) GameFeel.Instance.PlayThunk();
    }

    void Shatter()
    {
        broken = true;
        Vector3 playerVel = Motor.Velocity;
        foreach (GameObject piece in visualPieces)
        {
            if (piece == null) continue;
            SpawnShards(piece, playerVel);
            piece.SetActive(false);
        }
        if (GameFeel.Instance != null) GameFeel.Instance.PlayShatter();
    }

    static void SpawnShards(GameObject piece, Vector3 playerVel)
    {
        var renderer = piece.GetComponent<Renderer>();
        Bounds b = renderer != null ? renderer.bounds : new Bounds(piece.transform.position, Vector3.one);
        int count = Mathf.Clamp((int)(b.size.magnitude * 4f), 6, 22);
        for (int i = 0; i < count; i++)
        {
            var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard.name = "Shard";
            shard.layer = 2; // Ignore Raycast: invisible to the motor's casts
            shard.transform.position = new Vector3(
                Random.Range(b.min.x, b.max.x),
                Random.Range(b.min.y, b.max.y),
                Random.Range(b.min.z, b.max.z));
            shard.transform.rotation = Random.rotation;
            shard.transform.localScale = new Vector3(
                Random.Range(0.08f, 0.28f), Random.Range(0.08f, 0.28f), 0.03f);
            shard.GetComponent<MeshRenderer>().sharedMaterial = ShardMaterial();
            var rb = shard.AddComponent<Rigidbody>();
            rb.mass = 0.05f;
            rb.linearVelocity = playerVel * 0.4f
                + Random.insideUnitSphere * 3.5f + Vector3.up * 2f;
            rb.angularVelocity = Random.insideUnitSphere * 12f;
            shard.AddComponent<ShardFade>();
        }
    }

    static Material shardMat;
    static Material ShardMaterial()
    {
        if (shardMat == null)
        {
            shardMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            shardMat.SetColor("_BaseColor", new Color(0.78f, 0.93f, 0.95f));
        }
        return shardMat;
    }

    public static void ResetAll()
    {
        foreach (TutorialGate gate in All)
        {
            gate.broken = false;
            gate.playerInside = false;
            gate.missFlashed = false;
            foreach (GameObject piece in gate.visualPieces)
                if (piece != null) piece.SetActive(true);
        }
    }
}

/// Glass shards shrink away and clean themselves up.
public class ShardFade : MonoBehaviour
{
    float born;
    Vector3 baseScale;
    const float Hold = 0.4f, Fade = 0.8f;

    void Start() { born = Time.time; baseScale = transform.localScale; }

    void Update()
    {
        float age = Time.time - born;
        if (age <= Hold) return;
        float t = (age - Hold) / Fade;
        if (t >= 1f) { Destroy(gameObject); return; }
        transform.localScale = baseScale * (1f - t);
    }
}
