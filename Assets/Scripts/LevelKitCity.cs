using UnityEngine;

/// <summary>
/// The city's windows, DARK DECO: a hole's worth of amber glass set into a
/// wall's thickness, in a charcoal iron frame with brass mullions and a stone
/// sill, a transom a third of the way up. A small hole gets a PORTHOLE
/// instead: a round brass rim round an amber disc. The pane keeps its collider
/// — it is a wall you can see through, and nothing goes out of it — so the
/// props that snap to walls still find one.
/// </summary>
public static partial class LevelKit
{
    static Material windowGlass, windowFrame;

    /// A window in `hole`, whose wall lies on `plane` (x for East/West, z for North/South)
    public static void Window(Transform parent, Hole hole, float plane)
    {
        EnsureOffice();
        windowGlass ??= Transparent(new Color(1f, 0.74f, 0.42f, 0.3f));
        windowFrame ??= Lit(Shader.Find("Universal Render Pipeline/Lit"), new Color(0.11f, 0.11f, 0.13f), 0.5f);
        bool ns = hole.face == Face.North || hole.face == Face.South;
        float u = (hole.u0 + hole.u1) * 0.5f, len = hole.u1 - hole.u0, v = (hole.v0 + hole.v1) * 0.5f, h = hole.v1 - hole.v0;
        if (len <= 0.5f || h <= 0.5f) return;
        Vector3 At(float along, float y) => ns ? new Vector3(along, y, plane) : new Vector3(plane, y, along);
        Vector3 Size(float along, float y, float depth) => ns ? new Vector3(along, y, depth) : new Vector3(depth, y, along);

        // The glass, in the middle of the wall's thickness, and a wall to the world still
        var pane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pane.name = "Window";
        pane.transform.SetParent(parent);
        pane.transform.position = At(u, v);
        pane.transform.localScale = Size(len, h, 0.08f);
        var pr = pane.GetComponent<MeshRenderer>();
        pr.sharedMaterial = windowGlass;
        pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        float deep = T + 0.1f;
        if (len < 2.6f && h < 2.6f)
        {
            // A porthole: a brass rim through the wall, an amber disc inside it, both faces
            float dia = Mathf.Min(len, h);
            Quaternion axial = ns ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.Euler(0f, 0f, 90f);
            Decor(parent, PrimitiveType.Cylinder, At(u, v), axial, new Vector3(dia + 0.3f, deep * 0.5f, dia + 0.3f), Brass, "PortholeRim");
            Decor(parent, PrimitiveType.Cylinder, At(u, v), axial, new Vector3(dia, deep * 0.5f + 0.02f, dia), WindowAmber, "PortholeGlass");
            Decor(parent, PrimitiveType.Cube, At(u, v), Quaternion.identity, Size(0.06f, dia, 0.16f), Brass, "PortholeBar");
            return;
        }
        // The frame, straddling the wall on the hole's edges: head, sill, jambs — a lip on both faces, a reveal in the cut
        Decor(parent, PrimitiveType.Cube, At(u, hole.v1), Quaternion.identity, Size(len + 0.24f, 0.24f, deep), windowFrame, "WindowHead");
        Decor(parent, PrimitiveType.Cube, At(u, hole.v1 + 0.22f), Quaternion.identity, Size(len + 0.5f, 0.2f, deep + 0.1f), Limestone, "WindowCornice");
        Decor(parent, PrimitiveType.Cube, At(u, hole.v0), Quaternion.identity, Size(len + 0.24f, 0.24f, deep), windowFrame, "WindowSill");
        Decor(parent, PrimitiveType.Cube, At(u, hole.v0 - 0.2f), Quaternion.identity, Size(len + 0.5f, 0.16f, deep + 0.2f), Limestone, "WindowSillStone");
        Decor(parent, PrimitiveType.Cube, At(hole.u0, v), Quaternion.identity, Size(0.24f, h, deep), windowFrame, "WindowJamb");
        Decor(parent, PrimitiveType.Cube, At(hole.u1, v), Quaternion.identity, Size(0.24f, h, deep), windowFrame, "WindowJamb");
        // Brass mullions every four metres or so, and a transom a third of the way up, through the glass
        int n = Mathf.Max(1, Mathf.RoundToInt(len / 4f));
        for (int i = 1; i < n; i++)
            Decor(parent, PrimitiveType.Cube, At(hole.u0 + len * i / n, v), Quaternion.identity, Size(0.14f, h, 0.22f), Brass, "Mullion");
        Decor(parent, PrimitiveType.Cube, At(u, hole.v0 + h * 0.33f), Quaternion.identity, Size(len, 0.12f, 0.22f), Brass, "Transom");
        // Thin glazing bars in the lights above the transom: the Deco grid
        for (int i = 0; i < n; i++)
        {
            float a = hole.u0 + len * i / n, b = hole.u0 + len * (i + 1) / n;
            Decor(parent, PrimitiveType.Cube, At((a + b) * 0.5f, hole.v0 + h * 0.665f), Quaternion.identity, Size(b - a - 0.14f, 0.05f, 0.12f), Brass, "GlazingBar");
            Decor(parent, PrimitiveType.Cube, At((a + b) * 0.5f, hole.v0 + h * 0.665f), Quaternion.identity, Size(0.05f, h * 0.67f - 0.12f, 0.12f), Brass, "GlazingBar");
        }
    }
}
