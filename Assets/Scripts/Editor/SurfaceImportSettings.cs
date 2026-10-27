using UnityEditor;
using UnityEngine;

/// <summary>
/// The import flags for the game's surface sets (Assets/Resources/Surfaces, made by Tools/ImportTextures.ps1): the
/// normal map imported as one, the data maps (metal-gloss, occlusion) kept linear, everything tiling at 1024 with
/// mipmaps and a little anisotropy. Runs on every import of a file under that folder, so a set dropped in later gets
/// the same treatment.
/// </summary>
public class SurfaceImportSettings : AssetPostprocessor
{
    const string Folder = "Assets/Resources/Surfaces/";

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder)) return;
        var imp = (TextureImporter)assetImporter;
        string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        imp.maxTextureSize = 1024;
        imp.wrapMode = TextureWrapMode.Repeat;
        imp.mipmapEnabled = true;
        imp.anisoLevel = 4;
        imp.textureCompression = TextureImporterCompression.Compressed;
        imp.alphaIsTransparency = false;
        switch (file)
        {
            case "Normal":
                imp.textureType = TextureImporterType.NormalMap;
                break;
            case "MetalGloss":
                imp.textureType = TextureImporterType.Default;
                imp.sRGBTexture = false;
                imp.alphaSource = TextureImporterAlphaSource.FromInput;
                break;
            case "Occlusion":
                imp.textureType = TextureImporterType.Default;
                imp.sRGBTexture = false;
                break;
            default:
                imp.textureType = TextureImporterType.Default;
                imp.sRGBTexture = true;
                break;
        }
    }
}
