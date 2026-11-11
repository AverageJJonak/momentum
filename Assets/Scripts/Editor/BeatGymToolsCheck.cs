using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// A check of the gym's test tools (BeatGymTools) that needs no one at the keys: builds the gym, then asks whether
/// every station has a pad the menu can send you to (a checkpoint of its own name), pieces of its own, a fall line
/// under it, and a PRINT that stays the same when the whole row stands somewhere else (the row is moved and turned
/// and every station printed again: a station is "changed" only when its own pieces change, never because something
/// further up the row did). Momentum → Level Validator → Check The Gym's Test Tools, or -executeMethod BeatGymToolsCheck.Run;
/// the report goes to Docs/Design/validation/BeatGym-tools.md
/// </summary>
public static class BeatGymToolsCheck
{
    [MenuItem("Momentum/Level Validator/Check The Gym's Test Tools", false, 4)]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.Log("BeatGymToolsCheck: works outside Play Mode."); return; }
        var config = AssetDatabase.LoadAssetAtPath<MovementConfig>("Assets/Config/MovementConfig.asset");
        if (config == null) { Debug.LogError("BeatGymToolsCheck: the movement config was not found."); return; }
        LevelSections.Last = null;
        GameBootstrap.BuildLevel(LevelKind.BeatGym, config);
        var rootGo = GameObject.Find("BeatGym");
        if (rootGo == null) { Debug.LogError("BeatGymToolsCheck: the gym did not build."); return; }
        var root = rootGo.transform;
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine("# The Beat Gym's test tools: a check");
        sb.AppendLine();

        var checkpoints = new HashSet<string>();
        if (LevelSections.Last != null) foreach (var cp in LevelSections.Last.checkpoints) checkpoints.Add(cp.name);
        var seen = new HashSet<string>();
        int noPad = 0, noPieces = 0, twice = 0, owned = 0;
        foreach (var s in GymStations.All)
        {
            if (!checkpoints.Contains(s.title)) noPad++;
            if (s.nums.Count == 0) noPieces++;
            if (!seen.Add(s.title)) twice++;
        }
        foreach (var c in root.GetComponentsInChildren<Collider>()) if (GymStations.Of(c) >= 0) owned++;
        sb.AppendLine("- stations: " + GymStations.All.Count + " (" + noPad + " without a pad of their own name, " + noPieces + " without pieces, " + twice + " named twice)");
        sb.AppendLine("- pieces that belong to a station: " + owned);
        sb.AppendLine("- has the tools' component: " + (rootGo.GetComponent<BeatGymTools>() != null ? "yes" : "NO"));

        // the row somewhere else: moved and turned, every station printed again from where its pad now is
        var where = new List<(Vector3 origin, Quaternion rot)>();
        foreach (var s in GymStations.All) where.Add((s.origin, s.rot));
        root.SetPositionAndRotation(new Vector3(1234.5f, -321.25f, 987.75f), Quaternion.Euler(0f, 33.3f, 0f));
        int differ = 0, worst = 0;
        var nums = new List<int>();
        for (int i = 0; i < GymStations.All.Count; i++)
        {
            var s = GymStations.All[i];
            string names = GymStations.Print(root, s.first, s.beatEnd, root.TransformPoint(where[i].origin), root.rotation * where[i].rot, nums);
            bool same = names == s.names && nums.Count == s.nums.Count;
            for (int j = 0; same && j < nums.Count; j++)
            {
                int d = Mathf.Abs(nums[j] - s.nums[j]);
                worst = Mathf.Max(worst, d);
                if (d > 3) same = false;
            }
            if (!same) differ++;
        }
        sb.AppendLine("- the row moved 1.6 km and turned 33°: " + differ + " station(s) read as changed (must be 0); the largest difference in any number " + worst + " cm (the allowance is 3)");
        sb.AppendLine();
        sb.AppendLine("| station | pieces | fall line (m under its pad) |");
        sb.AppendLine("|---|---|---|");
        foreach (var s in GymStations.All)
            sb.AppendLine("| " + s.title + " | " + (s.nums.Count / 6) + " | " + (s.origin.y - (s.lowY - 25f)).ToString("0", ci) + " |");

        string dir = Path.Combine(Application.dataPath, "..", "Docs", "Design", "validation");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "BeatGym-tools.md"), sb.ToString());
        Debug.Log("BeatGymToolsCheck: " + GymStations.All.Count + " stations, " + differ + " read as changed after the move, worst " + worst + " cm → Docs/Design/validation/BeatGym-tools.md");
        Object.DestroyImmediate(rootGo);
    }
}
