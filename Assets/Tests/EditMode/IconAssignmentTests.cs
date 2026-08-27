using System.Linq;
using FrogAcross.Editor.Build;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

namespace FrogAcross.Tests.EditMode
{
    /// <summary>
    /// PNGs existing in Assets is not the icon being set: these assert the
    /// Android adaptive-icon slots actually reference our textures.
    /// (Round/Legacy kinds are deprecated in Unity 6000.5 and minSdk 26 makes
    /// adaptive icons universal, so Adaptive is the only kind checked.)
    /// </summary>
    public class IconAssignmentTests
    {
        [Test]
        public void IconSourceTextures_Exist()
        {
            foreach (string path in new[] { IconAssigner.ForegroundPath, IconAssigner.BackgroundPath })
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(path), $"missing {path}");
            }
        }

        [Test]
        public void AdaptiveIcons_HaveBothLayersAssigned()
        {
            var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, AndroidPlatformIconKind.Adaptive);
            Assert.IsNotEmpty(icons);
            foreach (var icon in icons)
            {
                var textures = icon.GetTextures();
                Assert.GreaterOrEqual(textures.Length, 2, "adaptive icon should carry background+foreground layers");
                Assert.IsTrue(textures.All(t => t != null),
                    $"adaptive icon {icon} has an unassigned layer");
            }
        }
    }
}
