using UnityEditor;
using UnityEngine;

/// <summary>
/// The import flags for the borrowed UI (Assets/Resources/UI: Kenney's CC0 sprites and clicks, drawn by UIAssets).
/// The sprites import as plain textures for IMGUI — sRGB, the alpha as transparency, no mipmaps, uncompressed so a
/// one-pixel rule stays a rule, clamped, bilinear — and the clicks decompressed on load, mono, preloaded. A file
/// dropped into the folder later gets the same treatment without anyone clicking.
/// </summary>
public class UIImportSettings : AssetPostprocessor
{
    const string Folder = "Assets/Resources/UI/";

    public override uint GetVersion() => 1;

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder)) return;
        var imp = (TextureImporter)assetImporter;
        imp.textureType = TextureImporterType.Default;
        imp.sRGBTexture = true;
        imp.alphaSource = TextureImporterAlphaSource.FromInput;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.npotScale = TextureImporterNPOTScale.None;
        imp.filterMode = FilterMode.Bilinear;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.maxTextureSize = 256;
    }

    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith(Folder + "Audio/")) return;
        var imp = (AudioImporter)assetImporter;
        var s = imp.defaultSampleSettings;
        s.loadType = AudioClipLoadType.DecompressOnLoad;
        s.compressionFormat = AudioCompressionFormat.PCM;
        s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        s.preloadAudioData = true;
        imp.defaultSampleSettings = s;
        imp.ClearSampleSettingOverride("Standalone");
        imp.forceToMono = true;
        imp.loadInBackground = false;
        imp.ambisonic = false;
    }
}
