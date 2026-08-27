using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

namespace FrogAcross.Editor.Build
{
    /// <summary>
    /// Wires the rasterized brand PNGs into the Android adaptive-icon slots.
    /// minSdk is 26, where adaptive icons are universal — Unity 6000.5 has
    /// deprecated the Round/Legacy kinds, so Adaptive is the only kind set.
    /// Sources: ArtSource/brand/*.svg → Assets/Art/Icons/*.png (see
    /// ArtSource/brand/README.md for the reproducible conversion commands).
    /// </summary>
    public static class IconAssigner
    {
        public const string ForegroundPath = "Assets/Art/Icons/adaptive-foreground.png";
        public const string BackgroundPath = "Assets/Art/Icons/adaptive-background.png";

        [MenuItem("FrogAcross/Build/Assign Android Icons")]
        public static void AssignIcons()
        {
            var fg = Load(ForegroundPath);
            var bg = Load(BackgroundPath);

            var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, AndroidPlatformIconKind.Adaptive);
            foreach (var icon in icons) icon.SetTextures(bg, fg);
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, AndroidPlatformIconKind.Adaptive, icons);

            AssetDatabase.SaveAssets();
            Debug.Log($"[IconAssigner] Android adaptive icons assigned ({icons.Length} slots).");
        }

        private static Texture2D Load(string path)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
                throw new BuildFailedException($"[IconAssigner] Missing icon texture: {path}");
            return tex;
        }
    }
}
