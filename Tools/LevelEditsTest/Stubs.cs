// Minimal stand-ins for the Unity types LevelEdits.cs and LevelEditsApply.cs touch, so their logic runs under plain
// .NET. Transforms compose translation, rotation and scale like Unity's (world = parent * local).
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine
{
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 one => new Vector3(1, 1, 1);
        public static Vector3 up => new Vector3(0, 1, 0);
        public float sqrMagnitude => x * x + y * y + z * z;
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator *(Vector3 a, float d) => new Vector3(a.x * d, a.y * d, a.z * d);
        public static Vector3 Scale(Vector3 a, Vector3 b) => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        public override string ToString() => $"({x:0.###}, {y:0.###}, {z:0.###})";
    }

    public struct Vector3Int : IEquatable<Vector3Int>
    {
        public int x, y, z;
        public Vector3Int(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
        public static implicit operator Vector3(Vector3Int v) => new Vector3(v.x, v.y, v.z);
        public bool Equals(Vector3Int o) => x == o.x && y == o.y && z == o.z;
        public override bool Equals(object o) => o is Vector3Int v && Equals(v);
        public override int GetHashCode() => (x * 73856093) ^ (y * 19349663) ^ (z * 83492791);
    }

    public struct Quaternion
    {
        public float x, y, z, w;
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion identity => new Quaternion(0, 0, 0, 1);
        public static Quaternion operator *(Quaternion a, Quaternion b) => new Quaternion(
            a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
            a.w * b.y - a.x * b.z + a.y * b.w + a.z * b.x,
            a.w * b.z + a.x * b.y - a.y * b.x + a.z * b.w,
            a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z);
        public static Vector3 operator *(Quaternion q, Vector3 v)
        {
            var p = q * new Quaternion(v.x, v.y, v.z, 0) * new Quaternion(-q.x, -q.y, -q.z, q.w);
            return new Vector3(p.x, p.y, p.z);
        }
        public static Quaternion AngleY(float deg) { double r = deg * Math.PI / 360.0; return new Quaternion(0, (float)Math.Sin(r), 0, (float)Math.Cos(r)); }
    }

    public static class Mathf { public static int RoundToInt(float f) => (int)Math.Round(f, MidpointRounding.ToEven); }

    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }

    public class Object
    {
        string ownName;
        public virtual string name { get => ownName; set => ownName = value; }
        public static GameObject Instantiate(GameObject original, Transform parent, bool instantiateInWorldSpace)
        {
            var c = original.CloneDeep();
            c.transform.SetParent(parent, false);
            return c;
        }
        public static void Destroy(Object o) { if (o is GameObject g) g.destroyed = true; }
    }

    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public override string name { get => gameObject.name; set => gameObject.name = value; }
        public T GetComponent<T>() where T : class => gameObject.GetComponent<T>();
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : class => gameObject.GetComponentsInChildren<T>(includeInactive);
        public virtual Component CloneTo(GameObject g) { var c = (Component)MemberwiseClone(); c.gameObject = g; return c; }
    }

    public class Behaviour : Component { }
    public class MonoBehaviour : Behaviour { }

    public class Mesh : Object { }
    public class Material : Object { }
    public class MeshFilter : Component { public Mesh sharedMesh; }
    public class Renderer : Component
    {
        public Material sharedMaterial;
        public Material[] sharedMaterials => sharedMaterial != null ? new[] { sharedMaterial } : new Material[0];
    }
    public class MeshRenderer : Renderer { }

    public class GameObject : Object
    {
        public readonly List<Component> components = new List<Component>();
        public Transform transform;
        public bool activeSelf = true;
        public bool destroyed;
        public GameObject(string n) { name = n; transform = new Transform { gameObject = this }; components.Add(transform); }
        public void SetActive(bool v) => activeSelf = v;
        public T GetComponent<T>() where T : class => components.OfType<T>().FirstOrDefault();
        public T AddComponent<T>() where T : Component, new() { var c = new T { gameObject = this }; components.Add(c); return c; }
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : class
        {
            var list = new List<T>();
            void Go(Transform t) { if (!includeInactive && !t.gameObject.activeSelf) return; list.AddRange(t.gameObject.components.OfType<T>()); foreach (var c in t.children) Go(c); }
            Go(transform);
            return list.ToArray();
        }
        public static GameObject CreatePrimitive(PrimitiveType type)
        {
            var g = new GameObject(type.ToString());
            g.AddComponent<MeshFilter>().sharedMesh = new Mesh { name = type.ToString() };
            g.AddComponent<MeshRenderer>();
            return g;
        }
        public GameObject CloneDeep()
        {
            var g = new GameObject(name) { activeSelf = activeSelf };
            g.transform.localPosition = transform.localPosition; g.transform.localRotation = transform.localRotation; g.transform.localScale = transform.localScale;
            foreach (var c in components) if (!(c is Transform)) g.components.Add(c.CloneTo(g));
            foreach (var child in transform.children.ToList()) child.gameObject.CloneDeep().transform.SetParent(g.transform, false);
            return g;
        }
    }

    public class Transform : Component
    {
        public Transform parentT;
        public readonly List<Transform> children = new List<Transform>();
        public Vector3 localPosition;
        public Quaternion localRotation = Quaternion.identity;
        public Vector3 localScale = Vector3.one;
        public Transform parent => parentT;
        public int childCount => children.Count;
        public Transform GetChild(int i) => children[i];
        public Vector3 position => parentT == null ? localPosition : parentT.TransformPoint(localPosition);
        public Quaternion rotation { get => parentT == null ? localRotation : parentT.rotation * localRotation; set => localRotation = parentT == null ? value : Inverse(parentT.rotation) * value; }
        public Vector3 lossyScale => parentT == null ? localScale : Vector3.Scale(parentT.lossyScale, localScale);
        public Vector3 TransformPoint(Vector3 p) => position + rotation * Vector3.Scale(lossyScale, p);
        static Quaternion Inverse(Quaternion q) => new Quaternion(-q.x, -q.y, -q.z, q.w);
        public void SetParent(Transform p, bool worldPositionStays)
        {
            if (parentT != null) parentT.children.Remove(this);
            parentT = p;
            if (p != null) p.children.Add(this);
        }
    }

    public static class Debug { public static void Log(object o) { } public static void LogWarning(object o) { } }
    public static class Application { public static bool isEditor => false; public static bool isPlaying => false; public static string dataPath => "C:/Fake/Assets"; }
    public class TextAsset : Object { public string text = ""; }
    public static class Resources { public static T Load<T>(string path) where T : class => null; }
    public static class JsonUtility { public static T FromJson<T>(string json) => default; }
}

namespace UnityEngine.SceneManagement
{
    public struct Scene { public GameObject[] roots; public GameObject[] GetRootGameObjects() => roots; }
}

// The game types the two files reference
public enum LevelKind { PracticeRange, Tutorial, Level1 }
public enum EnemyKind { Dummy, Chaser, Boss }
public class MovementConfig { }
public class Enemy : UnityEngine.MonoBehaviour
{
    public EnemyKind kind;
    public bool rehomed;
    public void Rehome() => rehomed = true;
    public static Enemy Spawn(EnemyKind kind, UnityEngine.Vector3 feet, MovementConfig config)
    {
        var g = new UnityEngine.GameObject("Enemy" + kind);
        g.transform.localPosition = feet;
        var e = g.AddComponent<Enemy>();
        e.kind = kind;
        Test.Spawned.Add(g);
        return e;
    }
}
public static class LevelKit
{
    public static UnityEngine.Material Floor = new UnityEngine.Material { name = "Floor FFFFFF" };
    public static UnityEngine.Material UntiledOf(UnityEngine.Material m) => m;
    public static UnityEngine.Material Retile(UnityEngine.Material m, UnityEngine.Vector3 size) => m;
}
