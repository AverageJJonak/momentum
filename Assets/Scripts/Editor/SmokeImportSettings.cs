using UnityEditor;
using UnityEngine;

/// <summary>
/// The import flags for the game's smoke textures (Assets/Resources/Smoke, made by Tools/MakeSmoke.ps1 from Kenney's
/// CC0 puffs): the alpha is the density and the only thing that matters, the tile repeats, the puff clamps.
/// </summary>
public class SmokeImportSettings : AssetPostprocessor
{
    const string Folder = "Assets/Resources/Smoke/";

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder)) return;
        var imp = (TextureImporter)assetImporter;
        string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        imp.textureType = TextureImporterType.Default;
        imp.sRGBTexture = true;
        imp.alphaSource = TextureImporterAlphaSource.FromInput;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = true;
        imp.anisoLevel = 2;
        imp.textureCompression = TextureImporterCompression.CompressedHQ;
        imp.wrapMode = file == "Puff" ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
        imp.maxTextureSize = file == "Puff" ? 256 : 1024;
    }
}
