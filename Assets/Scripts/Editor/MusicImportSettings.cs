using UnityEditor;
using UnityEngine;

/// <summary>
/// Import settings for the score, enforced at import so a dropped-in file needs no clicking.
/// Resources/Music/jazz holds the LEADS of the remix (MusicRemix.cs): Vorbis, mono,
/// DECOMPRESS ON LOAD with preload OFF and background loading ON — the engine decodes a
/// side only while it is the one playing or the next in its rotation, and reads its
/// samples (AudioClip.GetData, which a Compressed In Memory or Streaming clip REFUSES,
/// which is exactly how the score went silent on 2026-10-11) for the tempo/downbeat/key
/// analysis, the mastering and the stutter edits. Anything else under Resources/Music
/// stays the old way: Vorbis, compressed in memory.
/// Two guards: GetVersion (a bump reimports every audio asset this touches) and the
/// InitializeOnLoad sweep, which reimports any lead whose importer disagrees on every
/// script reload — files imported before this script existed, or imported while an older
/// version of it ran, get fixed without anyone noticing.
/// </summary>
public class MusicImportSettings : AssetPostprocessor
{
    const string Music = "/Resources/Music/";
    const string Leads = "/Resources/Music/jazz/";

    public override uint GetVersion() => 3; // 3: the jazz folder's Decompress On Load (2026-10-12)

    void OnPreprocessAudio()
    {
        if (!assetPath.Contains(Music)) return; // asset paths always use forward slashes
        Apply((AudioImporter)assetImporter, assetPath.Contains(Leads));
    }

    static void Apply(AudioImporter importer, bool lead)
    {
        var s = importer.defaultSampleSettings;
        s.loadType = lead ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.CompressedInMemory;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        s.quality = lead ? 0.7f : 0.55f;
        s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        s.preloadAudioData = !lead;
        importer.defaultSampleSettings = s;
        importer.ClearSampleSettingOverride("Standalone"); // no platform override may undo the default
        importer.forceToMono = lead; // the 78s are mono anyway: half the memory, one channel to analyse
        importer.loadInBackground = lead;
        importer.ambisonic = false;
    }

    static bool Wanted(AudioImporter importer, bool lead)
    {
        var s = importer.defaultSampleSettings;
        return s.loadType == (lead ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.CompressedInMemory)
            && s.preloadAudioData == !lead && importer.loadInBackground == lead && importer.forceToMono == lead
            && !importer.ContainsSampleSettingsOverride("Standalone");
    }

    /// Every script reload: any lead imported with other settings is fixed and reimported
    [InitializeOnLoadMethod]
    static void EnforceLeads()
    {
        EditorApplication.delayCall += () =>
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Music/jazz")) return;
            int fixedCount = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Resources/Music/jazz" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null || Wanted(importer, true)) continue;
                Apply(importer, true);
                importer.SaveAndReimport();
                fixedCount++;
                Debug.Log("MusicImportSettings: reimported " + path + " as Decompress On Load, preload off, background on, mono — the remix engine reads its samples");
            }
            if (fixedCount > 0) Debug.Log("MusicImportSettings: " + fixedCount + " lead(s) reimported");
        };
    }
}
