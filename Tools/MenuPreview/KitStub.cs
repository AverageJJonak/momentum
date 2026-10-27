// Stand-ins for the parts of the game's kit the menu city's generator calls: the box welder (which here just keeps the
// boxes), the keyword-template lookup, and the pieces of CityKit the city uses. The House's stone is MenuCity's own
// (MenuCityHouse.cs, compiled here as it is in the game), so nothing of it is ported; the far skyline is ported box
// for box from CityKit.Skyline.
using System.Collections.Generic;
using UnityEngine;

public class MeshBaker
{
    public struct Box { public Material mat; public Vector3 pos, size; public Quaternion rot; public PrimitiveType type; }
    public readonly List<Box> boxes = new List<Box>();
    public float CellSize;
    public void Add(Material mat, Vector3 pos, Vector3 size, Quaternion? rot = null, PrimitiveType type = PrimitiveType.Cube)
    {
        if (mat == null) return;
        boxes.Add(new Box { mat = mat, pos = pos, size = size, rot = rot ?? Quaternion.identity, type = type });
    }
}

public static partial class LevelKit
{
    public static Material FromTemplate(string name, string fallback)
    {
        var m = new Material(Shader.Find(fallback)) { name = name };
        if (name.Contains("Transparent")) m.transparent = true;
        return m;
    }
}

public static class MenuBackdrop { public const float TowerScale = 3.5f; }

public static class CityKit
{
    public const float TowerH = 190f;
    public static Material mass, massFar, seamDim, beaconRed;
    public static Material Mass => mass;
    public static Material BeaconRed => beaconRed;

    static Material Lit(Color c, float s) { var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", s); return m; }
    static Material Unlit(Color c) { var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")); m.SetColor("_BaseColor", c); return m; }

    public static void Init(bool dusk)
    {
        mass = Lit(new Color(0.12f, 0.115f, 0.12f), 0.3f);
        massFar = Lit(new Color(0.07f, 0.07f, 0.09f), 0.2f);
        seamDim = Unlit(new Color(0.9f, 0.72f, 0.45f));
        beaconRed = Unlit(new Color(2.6f, 0.2f, 0.12f));
    }

    /// The House: its stone is MenuCity.HouseMass's, here as in the game. The mark and the name are scene objects
    /// in the game (LevelKit.Emblem, LevelKit.Text) and are not drawn here
    public static void Tower(MeshBaker b, Vector3 at, float scale) => MenuCity.HouseMass(b, at, scale);

    /// CityKit.Skyline, box for box: with `sheets` the ring is MenuCity.FarRing's buildings (compiled here as in the
    /// game), without it the horizon's plain masses
    public static void Skyline(MeshBaker b, Vector3 center, float baseY, int count, float rMin, float rMax, float fromDeg, float toDeg, float heightScale = 1f, SheetBaker sheets = null)
    {
        if (sheets != null) { MenuCity.FarRing(b, sheets, center, baseY, count, rMin, rMax, fromDeg, toDeg, heightScale, true); return; }
        for (int i = 0; i < count; i++)
        {
            float a = Mathf.Lerp(fromDeg, toDeg, i / (float)Mathf.Max(1, count - 1)) * Mathf.Deg2Rad;
            float r = Random.Range(rMin, rMax);
            bool mega = i % 12 == 5;
            float h = (mega ? Random.Range(170f, 250f) : Random.Range(30f, 140f)) * heightScale;
            float w = (mega ? Random.Range(50f, 70f) : Random.Range(26f, 60f)) * Mathf.Sqrt(heightScale);
            Vector3 p = new Vector3(center.x + Mathf.Cos(a) * r, baseY + h * 0.4f, center.z + Mathf.Sin(a) * r);
            b.Add(massFar, p, new Vector3(w, h * 0.8f, w));
            b.Add(massFar, new Vector3(p.x, baseY + h * 0.9f, p.z), new Vector3(w * 0.7f, h * 0.2f, w * 0.7f));
            if (mega)
            {
                b.Add(massFar, new Vector3(p.x, baseY + h + 14f, p.z), new Vector3(w * 0.4f, 28f, w * 0.4f));
                b.Add(massFar, new Vector3(p.x, baseY + h + 36f, p.z), new Vector3(w * 0.2f, 16f, w * 0.2f));
                b.Add(seamDim, new Vector3(p.x, baseY + h + 0.4f, p.z), new Vector3(w * 0.7f + 1f, 0.8f, w * 0.7f + 1f));
                b.Add(seamDim, new Vector3(p.x, baseY + h + 28.4f, p.z), new Vector3(w * 0.4f + 1f, 0.8f, w * 0.4f + 1f));
            }
        }
    }
}
