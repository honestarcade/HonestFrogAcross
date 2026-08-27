using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace FrogAcross.Tests.EditMode.InvariantGuards
{
    /// <summary>
    /// Executable guard for project invariant 1: no ads, no tracking, no
    /// analytics, no network — all player data stays on-device.
    /// (CLAUDE.md: guard #17. The third layer — built-manifest scan — lives in
    /// Assets/Scripts/Editor/Build/ManifestGuard.cs and runs inside every
    /// non-development Android build.)
    /// </summary>
    public class NoNetworkGuardTests
    {
        /// <summary>
        /// One obvious editable list. Substring match against every dependency
        /// name in Packages/manifest.json.
        /// </summary>
        private static readonly string[] ForbiddenPackageFragments =
        {
            "com.unity.ads",
            "analytics",   // catches com.unity.analytics AND com.unity.modules.unityanalytics
            "com.unity.purchasing",
            "com.unity.services.",
            "firebase",
            "admob",
            "applovin",
            "facebook",
            "appsflyer",
            "adjust",
            "singular",
        };

        [Test]
        public void PackageManifest_ContainsNoAdsAnalyticsOrNetworkSdks()
        {
            string manifest = File.ReadAllText("Packages/manifest.json").ToLowerInvariant();
            var hits = ForbiddenPackageFragments.Where(manifest.Contains).ToList();
            Assert.IsEmpty(hits,
                "Invariant 1 (no ads/tracking/analytics/network) breached by packages: "
                + string.Join(", ", hits)
                + ". Removing them or amending the invariant is an owner conversation, not a test edit.");
        }

        [Test]
        public void InternetPermission_IsNeverForced()
        {
            Assert.IsFalse(PlayerSettings.Android.forceInternetPermission,
                "Android Internet Access must stay 'Auto' (never 'Require') — invariant 1.");
        }

        [Test]
        public void LauncherManifest_StripsNetworkPermissions()
        {
            const string path = "Assets/Plugins/Android/LauncherManifest.xml";
            Assert.IsTrue(File.Exists(path), $"{path} missing — it carries the tools:node=remove enforcement.");
            string text = File.ReadAllText(path);
            foreach (string perm in new[] { "android.permission.INTERNET", "android.permission.ACCESS_NETWORK_STATE" })
            {
                StringAssert.IsMatch(
                    $@"uses-permission[^>]*{System.Text.RegularExpressions.Regex.Escape(perm)}[^>]*tools:node=""remove""",
                    text.Replace("\n", " "),
                    $"launcher manifest must carry tools:node=\"remove\" for {perm}");
            }
        }
    }
}
