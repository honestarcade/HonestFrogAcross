using UnityEditor;

namespace FrogAcross.Editor.Art
{
    /// <summary>
    /// Anything dropped in Resources/UI is a single UI sprite. Without this the
    /// project's sprite-sheet defaults imported the logo as a multi-sprite
    /// atlas, so Resources.Load&lt;Sprite&gt; returned one slice of it — the
    /// menu showed a corner of the mark instead of the frog.
    /// </summary>
    public sealed class UiSpriteImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("Assets/Resources/UI/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.spritePixelsPerUnit = 100f;
        }
    }
}
