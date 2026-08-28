using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace FrogAcross.Editor.Build
{
    /// <summary>
    /// One-command Android builds plus the baseline platform configuration.
    /// Settings are applied programmatically (not hand-edited in ProjectSettings.asset)
    /// so the definitive values live here and in PlayerSettingsTests.
    /// </summary>
    public static class BuildScript
    {
        public const string PackageId = "com.honestarcade.frogacross";
        public const string OutputPath = "Builds/frogacross.aab";

        [MenuItem("FrogAcross/Build/Apply Baseline Settings")]
        public static void ApplyBaselineSettings()
        {
            PlayerSettings.productName = "Frog Across"; // spaced everywhere player-visible (owner decision 2026-08-28)
            PlayerSettings.companyName = "Honest Arcade";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageId);
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = 1;

            // Landscape only, both rotations.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            // Invariant 1 (no network): never force the INTERNET permission.
            PlayerSettings.Android.forceInternetPermission = false;

            AssetDatabase.SaveAssets();
            Debug.Log("[BuildScript] Baseline settings applied.");
        }

        /// <summary>
        /// Signing comes from env at build time only (never persisted into
        /// ProjectSettings — a test guards that). FROG_RELEASE=1 makes missing
        /// signing a hard failure so no unsigned release path exists.
        /// </summary>
        private static bool ApplySigningFromEnv()
        {
            string path = Environment.GetEnvironmentVariable("FROG_KEYSTORE_PATH");
            string storePass = Environment.GetEnvironmentVariable("FROG_KEYSTORE_PASS");
            string alias = Environment.GetEnvironmentVariable("FROG_KEY_ALIAS");
            string keyPass = Environment.GetEnvironmentVariable("FROG_KEY_PASS");
            bool release = Environment.GetEnvironmentVariable("FROG_RELEASE") == "1";

            bool haveAll = !string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(storePass)
                && !string.IsNullOrEmpty(alias) && !string.IsNullOrEmpty(keyPass);

            if (!haveAll)
            {
                if (release)
                {
                    Fail("FROG_RELEASE=1 but signing env is incomplete "
                        + "(need FROG_KEYSTORE_PATH/FROG_KEYSTORE_PASS/FROG_KEY_ALIAS/FROG_KEY_PASS). "
                        + "There is no unsigned release path.");
                    return false;
                }
                PlayerSettings.Android.useCustomKeystore = false;
                Debug.Log("[BuildScript] No signing env — building unsigned (dev). See .n8/memory/signing.md.");
                return true;
            }

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = path;
            PlayerSettings.Android.keystorePass = storePass;
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keyaliasPass = keyPass;
            Debug.Log($"[BuildScript] Signing with upload key '{alias}'.");
            return true;
        }

        private static void ClearSigning()
        {
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.keystoreName = string.Empty;
            PlayerSettings.Android.keystorePass = string.Empty;
            PlayerSettings.Android.keyaliasName = string.Empty;
            PlayerSettings.Android.keyaliasPass = string.Empty;
        }

        [MenuItem("FrogAcross/Build/Android AAB")]
        public static void BuildAndroidAab()
        {
            EditorUserBuildSettings.buildAppBundle = true;
            if (!ApplySigningFromEnv()) return;
            bool ok;
            try
            {
                ok = BuildAabInner();
            }
            finally
            {
                ClearSigning(); // never let key material persist in editor state
            }
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
            else if (!ok) throw new BuildFailedException("[BuildScript] Build failed — see log.");
        }

        private static bool BuildAabInner()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[BuildScript] No enabled scenes in EditorBuildSettings.");
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath)!);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.LogError($"[BuildScript] Build result: {report.summary.result} ({report.summary.totalErrors} errors).");
                return false;
            }

            Debug.Log($"[BuildScript] AAB built: {OutputPath} ({report.summary.totalSize} bytes).");
            return true;
        }

        private static void Fail(string message)
        {
            Debug.LogError($"[BuildScript] {message}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw new BuildFailedException(message);
        }
    }
}
