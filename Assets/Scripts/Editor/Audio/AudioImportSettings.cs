using System.IO;
using UnityEditor;
using UnityEngine;

namespace FrogAcross.Editor.Audio
{
    /// <summary>
    /// Import rules for the game's audio (#66). Effects are tiny and fire on
    /// input, so they decompress on load and cost nothing to play. Music is
    /// two 24s stereo loops — 4MB each as raw PCM, more than every other asset
    /// in the game put together — so it ships Vorbis-compressed and streamed.
    /// Nothing streams from the NETWORK (invariant 1): "streaming" here means
    /// off local storage rather than fully resident in memory.
    /// </summary>
    public sealed class AudioImportSettings : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            if (!assetPath.Contains("/Resources/Audio/")) return;
            var importer = (AudioImporter)assetImporter;
            bool music = Path.GetFileName(assetPath).StartsWith("music-");

            var settings = importer.defaultSampleSettings;
            settings.compressionFormat = music
                ? AudioCompressionFormat.Vorbis
                : AudioCompressionFormat.PCM;
            settings.loadType = music
                ? AudioClipLoadType.Streaming
                : AudioClipLoadType.DecompressOnLoad;
            settings.quality = music ? 0.7f : 1f;
            importer.defaultSampleSettings = settings;

            // effects are already mono; the flag keeps a re-roll honest
            importer.forceToMono = !music;
        }
    }
}
