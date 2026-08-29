using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;

namespace FrogAcross.Tests.EditMode
{
    /// <summary>
    /// Pins the platform configuration story #14 established. A drive-by change
    /// to any of these values in the editor UI fails the suite.
    /// </summary>
    public class PlayerSettingsTests
    {
        [Test]
        public void PackageId_IsPermanentPlayIdentifier() =>
            Assert.AreEqual("com.honestarcade.frogacross",
                PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));

        [Test]
        public void ProductAndCompany_MatchBrand()
        {
            Assert.AreEqual("Frog Across", PlayerSettings.productName);
            Assert.AreEqual("Honest Arcade", PlayerSettings.companyName);
        }

        [Test]
        public void Orientation_IsLandscapeOnly()
        {
            Assert.AreEqual(UIOrientation.AutoRotation, PlayerSettings.defaultInterfaceOrientation);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToPortrait);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToPortraitUpsideDown);
            Assert.IsTrue(PlayerSettings.allowedAutorotateToLandscapeLeft);
            Assert.IsTrue(PlayerSettings.allowedAutorotateToLandscapeRight);
        }

        [Test]
        public void Scripting_IsIl2cppArm64Only()
        {
            Assert.AreEqual(ScriptingImplementation.IL2CPP,
                PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android));
            Assert.AreEqual(AndroidArchitecture.ARM64, PlayerSettings.Android.targetArchitectures);
        }

        [Test]
        public void MinSdk_Is26_TargetAuto()
        {
            Assert.AreEqual(26, (int)PlayerSettings.Android.minSdkVersion);
            Assert.AreEqual(AndroidSdkVersions.AndroidApiLevelAuto, PlayerSettings.Android.targetSdkVersion);
        }

        [Test]
        public void Version_IsInitial()
        {
            // CI stamps the tag version at build time, so pin the shape (and
            // that it is not the placeholder), never one frozen literal.
            Assert.That(PlayerSettings.bundleVersion, Does.Match(@"^\d+\.\d+\.\d+$"),
                "bundleVersion must be semver");
            Assert.That(PlayerSettings.bundleVersion, Is.Not.EqualTo("1.0"), "left at Unity's default");
            Assert.That(PlayerSettings.Android.bundleVersionCode, Is.GreaterThan(0));
        }
    }
}
