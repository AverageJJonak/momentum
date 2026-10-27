using UnityEngine;

/// <summary>
/// THE VIEWMODEL LAYER (2026-10-08): the arms and whatever they hold are drawn by their own overlay camera at a fixed
/// field of view (ViewmodelCamera), so they never stretch with the world's FOV or its kicks. Everything that must
/// stay in the hands' picture (the rig, the weapons, their flashes and trails) goes on this layer; everything that
/// belongs to the world (cut marks, sparks on walls, bodies) stays off it. Shared by the rig builder and the camera.
/// </summary>
public static class Viewmodel
{
    /// Layer 30 is named "Viewmodel" in ProjectSettings/TagManager.asset (31 is the thumbnails' hidden layer)
    public const int Layer = 30;
    public static int Mask => 1 << Layer;

    /// Put a transform and everything under it on the layer
    public static void Apply(Transform root)
    {
        if (root == null) return;
        root.gameObject.layer = Layer;
        for (int i = 0; i < root.childCount; i++) Apply(root.GetChild(i));
    }
}
