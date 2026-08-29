using System;
using System.Collections.Generic;
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
            PlayerSettings.bundleVersion = "0.4.0"; // CI overrides per tag; keep in step so dev builds do not lie
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

            // #35: R8 minification on release builds — produces the mapping
            // file Play wants for readable crash reports (and shrinks the AAB).
            PlayerSettings.Android.minifyRelease = true;
            PlayerSettings.Android.minifyDebug = false;

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

        /// <summary>
        /// CI entry point for game-ci/unity-builder's custom buildMethod. The
        /// action passes its standard values as CLI args regardless of build
        /// method (-customBuildPath, -buildVersion, -androidVersionCode, and
        /// the androidKeystore* quartet); we parse them ourselves instead of
        /// letting the action inject its UnityBuilderAction script — which our
        /// analyzer gate rejects (vendor code with UNT violations) and which
        /// cp -R would overwrite if we vendored a fixed copy at its path.
        /// </summary>
        public static void BuildCi()
        {
            var args = ParseArgs(Environment.GetCommandLineArgs());
            EditorUserBuildSettings.buildAppBundle = true;

            if (args.TryGetValue("buildVersion", out string v) && !string.IsNullOrEmpty(v))
                PlayerSettings.bundleVersion = v;
            if (args.TryGetValue("androidVersionCode", out string vc)
                && int.TryParse(vc, out int code) && code > 0)
                PlayerSettings.Android.bundleVersionCode = code;

            string ks = args.GetValueOrDefault("androidKeystoreName", "");
            string ksPass = args.GetValueOrDefault("androidKeystorePass", "");
            string alias = args.GetValueOrDefault("androidKeyaliasName", "");
            string aliasPass = args.GetValueOrDefault("androidKeyaliasPass", "");
            bool signed = !string.IsNullOrEmpty(ks) && !string.IsNullOrEmpty(ksPass)
                && !string.IsNullOrEmpty(alias) && !string.IsNullOrEmpty(aliasPass);
            if (signed)
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = ks;
                PlayerSettings.Android.keystorePass = ksPass;
                PlayerSettings.Android.keyaliasName = alias;
                PlayerSettings.Android.keyaliasPass = aliasPass;
                Debug.Log($"[BuildScript] CI signing with key alias '{alias}'.");
            }
            else
            {
                PlayerSettings.Android.useCustomKeystore = false;
                Debug.Log("[BuildScript] CI build unsigned (no keystore args).");
            }

            string outPath = args.GetValueOrDefault("customBuildPath", "");
            if (string.IsNullOrEmpty(outPath)) outPath = OutputPath;

            bool ok;
            try
            {
                ok = BuildAabInner(outPath);
            }
            finally
            {
                ClearSigning();
            }
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
            else if (!ok) throw new BuildFailedException("[BuildScript] CI build failed — see log.");
        }

        /// <summary>-key value pairs from a Unity command line (testable core).</summary>
        public static Dictionary<string, string> ParseArgs(string[] argv)
        {
            var result = new Dictionary<string, string>();
            for (int i = 0; i < argv.Length; i++)
            {
                if (!argv[i].StartsWith("-", StringComparison.Ordinal)) continue;
                string key = argv[i].TrimStart('-');
                string value = i + 1 < argv.Length && !argv[i + 1].StartsWith("-", StringComparison.Ordinal)
                    ? argv[i + 1]
                    : string.Empty;
                result[key] = value;
            }
            return result;
        }

        private static bool BuildAabInner() => BuildAabInner(OutputPath);

        private static bool BuildAabInner(string outputPath)
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

            string dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            // #35: emit a symbols package (symbol tables are enough for
            // readable Play crash/ANR stacks, at a fraction of full-debug size)
            UnityEditor.Android.UserBuildSettings.DebugSymbols.level = Unity.Android.Types.DebugSymbolLevel.SymbolTable;
            UnityEditor.Android.UserBuildSettings.DebugSymbols.format = Unity.Android.Types.DebugSymbolFormat.Zip;

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.LogError($"[BuildScript] Build result: {report.summary.result} ({report.summary.totalErrors} errors).");
                return false;
            }

            CollectMappingFile(outputPath);
            Debug.Log($"[BuildScript] AAB built: {outputPath} ({report.summary.totalSize} bytes).");
            return true;
        }

        /// <summary>
        /// #35: Unity leaves the R8 mapping.txt deep in the gradle work tree —
        /// copy the freshest one next to the AAB so CI can upload it to Play.
        /// </summary>
        private static void CollectMappingFile(string outputPath)
        {
            const string gradleRoot = "Library/Bee";
            if (!Directory.Exists(gradleRoot)) return;
            var mapping = Directory.GetFiles(gradleRoot, "mapping.txt", SearchOption.AllDirectories)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
            if (mapping == null)
            {
                Debug.Log("[BuildScript] No R8 mapping.txt found (minification may be off for this variant).");
                return;
            }
            string dest = Path.Combine(Path.GetDirectoryName(outputPath) ?? ".", "mapping.txt");
            File.Copy(mapping, dest, overwrite: true);
            Debug.Log($"[BuildScript] R8 mapping collected: {dest}");
        }

        private static void Fail(string message)
        {
            Debug.LogError($"[BuildScript] {message}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw new BuildFailedException(message);
        }
    }
}
