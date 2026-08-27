using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FrogAcross.Tests.EditMode
{
    /// <summary>
    /// Signing is env-only (#19): keystore paths and passwords must never be
    /// serialized into ProjectSettings.asset, where they'd end up in git.
    /// </summary>
    public class SigningPersistenceTests
    {
        private const string SettingsPath = "ProjectSettings/ProjectSettings.asset";

        [Test]
        public void ProjectSettings_CarryNoKeystoreMaterial()
        {
            string text = File.ReadAllText(SettingsPath);

            foreach (string key in new[] { "AndroidKeystoreName", "AndroidKeyaliasName" })
            {
                var match = Regex.Match(text, $@"(?m)^[ \t]*{key}:[ \t]*(?<val>[^\r\n]*?)[ \t]*$");
                if (match.Success)
                {
                    string val = match.Groups["val"].Value.Trim('\'', '"').Trim();
                    // "{inproject}:" is Unity's serialized empty in-project selector, not a path.
                    if (val == "{inproject}:") val = string.Empty;
                    Assert.IsEmpty(val, $"{key} is persisted in ProjectSettings.asset ('{val}') — signing must stay env-only.");
                }
            }

            StringAssert.DoesNotContain("frogacross-upload", text);
            Assert.IsFalse(Regex.IsMatch(text, @"(?m)^\s*AndroidUseCustomKeystore:\s*1\s*$"),
                "useCustomKeystore must not be persisted as enabled.");
        }
    }
}
