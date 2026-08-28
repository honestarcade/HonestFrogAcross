using FrogAcross.Editor.Build;
using NUnit.Framework;

namespace FrogAcross.Tests.EditMode
{
    /// <summary>
    /// BuildCi consumes game-ci/unity-builder's CLI args — the parser is the
    /// contract point, so it gets pinned. Shape mirrors the action's real
    /// invocation (dist/platforms/ubuntu/steps/build.sh @ v5.0.0).
    /// </summary>
    public class BuildCiArgsTests
    {
        [Test]
        public void ParsesKeyValuePairs_LikeUnityBuilderInvocation()
        {
            var args = BuildScript.ParseArgs(new[]
            {
                "/opt/unity/Editor/Unity", "-quit",
                "-customBuildPath", "/github/workspace/build/Android/Android.aab",
                "-buildVersion", "0.2.0",
                "-androidVersionCode", "103",
                "-androidKeystoreName", "frogacross-upload.keystore",
                "-androidKeystorePass", "s3cret",
                "-androidKeyaliasName", "upload",
                "-androidKeyaliasPass", "s3cret",
            });

            Assert.AreEqual("/github/workspace/build/Android/Android.aab", args["customBuildPath"]);
            Assert.AreEqual("0.2.0", args["buildVersion"]);
            Assert.AreEqual("103", args["androidVersionCode"]);
            Assert.AreEqual("upload", args["androidKeyaliasName"]);
        }

        [Test]
        public void FlagFollowedByFlag_YieldsEmptyValue()
        {
            var args = BuildScript.ParseArgs(new[] { "-quit", "-batchmode", "-buildVersion", "1.0" });
            Assert.AreEqual(string.Empty, args["quit"]);
            Assert.AreEqual(string.Empty, args["batchmode"]);
            Assert.AreEqual("1.0", args["buildVersion"]);
        }

        [Test]
        public void EmptyStringValues_ReadAsMissing()
        {
            // build.sh passes -androidKeystorePass "" when unset — must read as unsigned.
            var args = BuildScript.ParseArgs(new[] { "-androidKeystorePass", "" });
            Assert.AreEqual(string.Empty, args["androidKeystorePass"]);
        }
    }
}
