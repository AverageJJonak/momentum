using UnityEditor;
using UnityEngine;

/// <summary>
/// THE IMPORT RULES for the modeller's pieces (2026-11-05; Docs/Art/MODELLER_BRIEF.md and the kit spec): every model under
/// Assets/Resources/Models comes in the way DecoKit and the level welder need it, whatever the export settings were:
/// READ/WRITE on (LevelWeld combines meshes, and CombineMeshes needs readable ones), the axis conversion BAKED into the
/// vertices (the prefab's root is identity, so DecoKit.Place can set its rotation outright), the file's own scale (metres),
/// normals as authored (the hard edges the toon ramp wants), materials imported so their NAMES are the slot names
/// DecoKit swaps by, no cameras or lights. The arm files keep their skeleton (a generic rig) for ArmRigBuild to bind.
/// </summary>
public class ModelImportRules : AssetPostprocessor
{
    const string Folder = "Assets/Resources/Models/";

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Folder)) return;
        var mi = (ModelImporter)assetImporter;
        mi.isReadable = true;
        mi.bakeAxisConversion = true;
        mi.useFileScale = true;
        mi.globalScale = 1f;
        mi.importNormals = ModelImporterNormals.Import;
        mi.importTangents = ModelImporterTangents.CalculateMikk;
        mi.meshCompression = ModelImporterMeshCompression.Off;
        mi.importCameras = false;
        mi.importLights = false;
        mi.importBlendShapes = false;
        mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
        bool arm = System.IO.Path.GetFileNameWithoutExtension(assetPath).StartsWith("arm_");
        mi.animationType = arm ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None;
        mi.importAnimation = false;
    }

    /// The trim sheet and any other texture dropped beside the pieces: no compression artefacts on the ribs, the normal map marked as one
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder)) return;
        var ti = (TextureImporter)assetImporter;
        string n = System.IO.Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
        ti.textureType = n.Contains("normal") ? TextureImporterType.NormalMap : TextureImporterType.Default;
        ti.sRGBTexture = !(n.Contains("normal") || n.Contains("mask"));
        ti.mipmapEnabled = true;
        ti.anisoLevel = 8;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
    }
}
