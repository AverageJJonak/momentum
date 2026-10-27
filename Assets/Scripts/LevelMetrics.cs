using System.Reflection;
using UnityEngine;

/// <summary>
/// THE LEVEL METRICS ASSET (Phase 1, 2026-10-27): Assets/Config/LevelMetrics.asset. It holds a reference to the
/// movement config, READS its values (by name, through reflection, never writing one back), and keeps the report
/// LevelMetricsMath makes from them: the jump tables, the wall run, the slides, the drops, what each technique
/// does to speed. The report regenerates whenever the asset or the config is touched in the inspector; the
/// validator and the beat library (Phases 2 and 3) take their numbers from `values` and the math class, so every
/// gap in a level is sized from the motor's own rules. The inspector (Editor/LevelMetricsEditor) shows the report
/// and writes it to Docs/Design/level-metrics.md; Tools/LevelMetrics prints the same from the command line.
/// </summary>
[CreateAssetMenu(fileName = "LevelMetrics", menuName = "Momentum/Level Metrics")]
public class LevelMetrics : ScriptableObject
{
    [Tooltip("The movement values the numbers come from. Read only: nothing here writes to it")]
    public MovementConfig config;
    [Tooltip("The entry speeds the tables are made for, m/s")]
    public float[] entrySpeeds = { 8f, 10f, 12f, 14f, 16f, 18f, 20f, 24f };
    [Tooltip("The slopes the slide table is made for, degrees")]
    public float[] slopeAngles = { 10f, 17f, 28f, 35f, 45f };
    [Tooltip("The drops the drop table is made for, metres")]
    public float[] dropHeights = { 2f, 3.9f, 6f, 9f, 12f, 20f };
    [Tooltip("The copy of the movement values the report was made from (what the validator and the beats use)")]
    public LevelMetricsMath.Values values = new LevelMetricsMath.Values();
    [TextArea(10, 60)] public string report = "(assign the movement config)";

    /// A copy of the config's values, field by field by name; the physics tick from the project settings
    public static LevelMetricsMath.Values Read(MovementConfig c)
    {
        var v = new LevelMetricsMath.Values();
        if (c == null) return v;
        foreach (var f in typeof(LevelMetricsMath.Values).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            var src = typeof(MovementConfig).GetField(f.Name, BindingFlags.Public | BindingFlags.Instance);
            if (src == null || src.FieldType != f.FieldType) continue;
            f.SetValue(v, src.GetValue(c));
        }
        v.fixedDeltaTime = Time.fixedDeltaTime;
        return v;
    }

    public void Recalculate()
    {
        values = Read(config);
        report = config == null ? "(assign the movement config)" : LevelMetricsMath.Report(values, entrySpeeds, slopeAngles, dropHeights);
    }

    void OnValidate() => Recalculate();

    [ContextMenu("Print the report")]
    void Print() => Debug.Log(report);

    /// The envelope for other systems at runtime: the asset's copy when it is in Resources, else a fresh read
    public static LevelMetricsMath.Values Live(MovementConfig c)
    {
        var asset = Resources.Load<LevelMetrics>("LevelMetrics");
        if (asset != null && asset.config == c) return asset.values;
        return Read(c);
    }
}
