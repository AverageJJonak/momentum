using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE LINT. After a level is built and before anyone runs it, the geometry
/// is checked for the mistakes a builder makes by number: a bridge whose end
/// hangs over nothing, a bridge too steep to run, a jump pad or a beacon
/// buried under something laid after it, a unit spawned inside a solid — and,
/// since the course grammar (2026-10-09), a SEALED SPACE (a section whose exit
/// plane is walled right across: the one allowed is the ward's lesson seal)
/// and a LANE UNDER 6 M (the free width at head height across a section,
/// probed every few metres). Each is a warning in the console with a position
/// to jump to; one summary line counts what the level is made of, so a heavy
/// level shows its weight next to the frame rate it gets.
/// </summary>
public static class LevelLint
{
    const int MaxReports = 40;
    const float LaneMin = 6f;

    public static int Run()
    {
        Physics.SyncTransforms();
        var report = new List<string>();
        Bridges(report);
        Pads(report);
        Beacons(report);
        Units(report);
        Sealed(report);
        Lanes(report);
        CityDistrict.LintAll(report);   // every district: its entrances reachable, three routes at least, two entrances a building
        for (int i = 0; i < report.Count && i < MaxReports; i++) Debug.LogWarning("LevelLint: " + report[i]);
        if (report.Count > MaxReports) Debug.LogWarning($"LevelLint: ... and {report.Count - MaxReports} more");
        Debug.Log("LevelLint: " + Summary() + (report.Count == 0 ? "  ·  no issues" : $"  ·  {report.Count} issue(s)"));
        return report.Count;
    }

    static string F(Vector3 p) => $"({p.x:0.#}, {p.y:0.#}, {p.z:0.#})";

    /// Something to stand on within a few metres under `p`
    static bool Roofed(Vector3 p) => Physics.Raycast(p + Vector3.up * 2f, Vector3.down, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

    /// Every deck: both ends over something (just past the end, where the roof or the landing should be), and a
    /// pitch you can run. Torn bridges hang in the air on purpose and are left alone
    static void Bridges(List<string> report)
    {
        foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            string n = mr.gameObject.name;
            if (n != "Bridge" && n != "Deck" && n != "Court") continue;
            var t = mr.transform;
            float half = t.lossyScale.z * 0.5f, top = t.lossyScale.y * 0.5f;
            Vector3 a = t.position + t.rotation * new Vector3(0f, top, -half), b = t.position + t.rotation * new Vector3(0f, top, half);
            Vector3 dir = (b - a).normalized;
            if (!Roofed(a - dir * 0.6f)) report.Add($"{n} end over nothing at {F(a)}");
            if (!Roofed(b + dir * 0.6f)) report.Add($"{n} end over nothing at {F(b)}");
            float len = Vector3.Distance(a, b);
            if (len > 4f)
            {
                float pitch = Mathf.Asin(Mathf.Clamp01(Mathf.Abs(b.y - a.y) / len)) * Mathf.Rad2Deg;
                if (pitch > 14f) report.Add($"{n} steep ({pitch:0}°) from {F(a)} to {F(b)}");
            }
        }
    }

    /// Every jump pad: nothing laid over it since
    static void Pads(List<string> report)
    {
        foreach (var col in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (col.gameObject.name != "JumpPad") continue;
            Vector3 c = col.bounds.center;
            float top = col.bounds.max.y;
            if (Physics.Raycast(new Vector3(c.x, top + 1.2f, c.z), Vector3.down, out RaycastHit hit, 1.1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.collider != col)
                report.Add($"jump pad buried under {hit.collider.gameObject.name} at {F(c)}");
        }
    }

    /// Every beacon: standing in the open
    static void Beacons(List<string> report)
    {
        if (District.Instance == null) return;
        foreach (var b in District.Instance.beacons)
        {
            var hits = Physics.OverlapSphere(b.at + Vector3.up * 0.8f, 0.25f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (hits.Length > 0) report.Add($"beacon inside {hits[0].gameObject.name} at {F(b.at)}");
        }
    }

    /// Every unit: its body clear of the world (units are on Ignore Raycast, so only the world answers)
    static void Units(List<string> report)
    {
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (!e.Alive) continue;
            Vector3 c = e.Center;
            if (Physics.CheckCapsule(c + Vector3.down * 0.3f, c + Vector3.up * 0.3f, 0.28f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                report.Add($"unit {e.kind} inside a solid at {F(c)}");
        }
    }

    /// Every section but the last: its exit plane must have a way through it. Probed at head height and chest
    /// height across the lane, a metre and a half before the plane, forward three metres: if every probe hits, the
    /// space is sealed. The ward's lesson seal is the one the grammar allows; a boss gate is its arena's
    static void Sealed(List<string> report)
    {
        var layout = LevelSections.Last;
        if (layout == null) return;
        for (int i = 0; i + 1 < layout.rooms.Count; i++)
        {
            var r = layout.rooms[i];
            if (r.z1 - r.z0 < 4f) continue;
            var blockers = new HashSet<string>();
            bool open = false, allowed = false;
            for (float x = -r.halfW + 0.6f; x <= r.halfW - 0.6f && !open; x += 1f)
                for (int k = 0; k < 3 && !open; k++)
                {
                    var from = new Vector3(x, r.exitY + 0.5f + k, r.z1 - 1.5f);
                    if (!Physics.Raycast(from, Vector3.forward, out RaycastHit hit, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) { open = true; break; }
                    string n = hit.collider.gameObject.name;
                    blockers.Add(n);
                    if (n == "WardSeal" || n.Contains("Boss") || n.Contains("Gate")) allowed = true;   // the ward's lesson, a boss arena's gate
                }
            if (open || allowed) continue;
            report.Add($"section {i + 1} ({r.zone}) is SEALED at its exit plane z {r.z1:0.#} by {string.Join(", ", blockers)} — a course never closes");
        }
    }

    /// Every section: the free width across the lane at head height, every four metres along it, must be six
    /// metres or more somewhere across the plane (a pinch you slide under is still a lane; a wall across is not)
    static void Lanes(List<string> report)
    {
        var layout = LevelSections.Last;
        if (layout == null) return;
        for (int i = 0; i < layout.rooms.Count; i++)
        {
            var r = layout.rooms[i];
            if (r.z1 - r.z0 < 8f) continue;
            for (float z = r.z0 + 3f; z < r.z1 - 3f; z += 4f)
            {
                float floor = Mathf.Lerp(r.entryY, r.exitY, Mathf.InverseLerp(r.z0, r.z1, z)) + 1.2f;
                float best = 0f, bestX = 0f;
                // The widest free span across the lane: every metre, how far to the nearest solid each way
                for (float x = -r.halfW + 0.5f; x <= r.halfW - 0.5f; x += 1f)
                {
                    var p = new Vector3(x, floor, z);
                    if (Physics.CheckSphere(p, 0.3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                    float w = Physics.Raycast(p, Vector3.left, out RaycastHit hw, r.halfW * 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? hw.distance : r.halfW + x;
                    float e = Physics.Raycast(p, Vector3.right, out RaycastHit he, r.halfW * 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? he.distance : r.halfW - x;
                    if (w + e > best) { best = w + e; bestX = x; }
                }
                if (best > 0f && best < LaneMin) report.Add($"section {i + 1} ({r.zone}) lane {best:0.#} m wide at {F(new Vector3(bestX, floor, z))} — never under {LaneMin:0} m");
            }
        }
    }

    /// What the level is made of
    static string Summary()
    {
        int renderers = 0, lights = 0, probes = 0, colliders = 0;
        long tris = 0;
        foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            var m = mf.sharedMesh;
            if (m == null) continue;
            for (int s = 0; s < m.subMeshCount; s++) tris += (long)m.GetIndexCount(s) / 3;
        }
        renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length;
        lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length;
        probes = Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None).Length;
        colliders = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Length;
        return $"{renderers} renderers, {tris / 1000}k triangles, {lights} lights, {probes} probes, {colliders} colliders";
    }
}
