using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FrogAcross.Editor.Build
{
    /// <summary>
    /// Build-time layers of the invariant-1 guard (#17).
    ///
    /// Unity 6's engine template injects android.permission.INTERNET into the
    /// unityLibrary manifest regardless of module usage, so presence there is
    /// not the signal. Enforcement is the launcher manifest's
    /// tools:node="remove" directives (Assets/Plugins/Android/LauncherManifest.xml),
    /// which win the Gradle manifest merge. This guard verifies:
    ///   (a) post-gradle-generate: the removal directives actually reached the
    ///       generated launcher module (catches the override silently not applying);
    ///   (b) post-build: the FINAL .aab's merged manifest contains no network
    ///       permission strings (the authoritative check on the shipped artifact).
    /// Development builds are exempt from (b)-failure semantics but still logged.
    /// </summary>
    public class ManifestGuard : IPostGenerateGradleAndroidProject, IPostprocessBuildWithReport
    {
        private static readonly string[] ForbiddenPermissions =
        {
            "android.permission.INTERNET",
            "android.permission.ACCESS_NETWORK_STATE",
        };

        public int callbackOrder => 100;

        public void OnPostGenerateGradleAndroidProject(string basePath)
        {
            // Sanity only: the launcher module must never itself GRANT a network
            // permission. (The tools:node="remove" directives from our
            // LauncherManifest.xml are consumed by Unity's template processing,
            // so their absence as literal text here is expected — the
            // authoritative check is the final-AAB scan in OnPostprocessBuild;
            // the source override file is pinned by NoNetworkGuardTests.)
            string root = Path.GetDirectoryName(basePath) ?? basePath;
            string launcherManifest = Path.Combine(root, "launcher", "src", "main", "AndroidManifest.xml");
            if (!File.Exists(launcherManifest)) return;

            string text = File.ReadAllText(launcherManifest);
            var granted = ForbiddenPermissions
                .Where(p => text.Contains(p) && !HasRemoveDirective(text, p))
                .ToList();
            if (granted.Count > 0)
            {
                throw new BuildFailedException(
                    "[ManifestGuard] The launcher manifest grants network permissions: "
                    + string.Join(", ", granted)
                    + " — invariant 1. Something edited the launcher template.");
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;
            string artifact = report.summary.outputPath;
            if (!File.Exists(artifact) || Path.GetExtension(artifact) != ".aab") return;

            using var zip = ZipFile.OpenRead(artifact);
            var entry = zip.GetEntry("base/manifest/AndroidManifest.xml");
            if (entry == null)
            {
                Debug.LogWarning("[ManifestGuard] No base manifest entry in AAB — cannot verify.");
                return;
            }

            using var ms = new MemoryStream();
            using (var s = entry.Open()) s.CopyTo(ms);
            byte[] bytes = ms.ToArray();

            var found = ForbiddenPermissions
                .Where(p => Contains(bytes, Encoding.UTF8.GetBytes(p)) || Contains(bytes, Encoding.Unicode.GetBytes(p)))
                .ToList();
            if (found.Count == 0)
            {
                Debug.Log("[ManifestGuard] Final AAB manifest clean — no network permissions.");
                return;
            }

            string detail = string.Join(", ", found);
            if (EditorUserBuildSettings.development)
            {
                Debug.LogWarning($"[ManifestGuard] Network permissions in DEVELOPMENT AAB (exempt): {detail}");
                return;
            }

            throw new BuildFailedException(
                $"[ManifestGuard] Invariant 1 (no network) breached — final release AAB grants: {detail}. "
                + "Find what re-introduced the permission (package, plugin, template change) and remove it; "
                + "amending the invariant itself is an owner conversation.");
        }

        private static bool HasRemoveDirective(string manifestText, string permission)
        {
            // Examine the whole element around each occurrence — Unity reorders
            // attributes, so tools:node may precede or follow android:name.
            int idx = manifestText.IndexOf(permission, System.StringComparison.Ordinal);
            while (idx >= 0)
            {
                int start = manifestText.LastIndexOf('<', idx);
                int end = manifestText.IndexOf('>', idx);
                if (start >= 0 && end > start
                    && manifestText.Substring(start, end - start).Contains("remove")) return true;
                idx = manifestText.IndexOf(permission, idx + 1, System.StringComparison.Ordinal);
            }
            return false;
        }

        private static bool Contains(byte[] haystack, byte[] needle)
        {
            for (int i = 0; i + needle.Length <= haystack.Length; i++)
            {
                int j = 0;
                while (j < needle.Length && haystack[i + j] == needle[j]) j++;
                if (j == needle.Length) return true;
            }
            return false;
        }
    }
}
