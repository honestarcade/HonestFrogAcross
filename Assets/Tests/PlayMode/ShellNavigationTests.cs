using System.Collections;
using FrogAcross.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FrogAcross.Tests.PlayMode
{
    /// <summary>#55: boot → menu, every screen reachable, back walks the stack,
    /// two-tone lockup is present.</summary>
    public class ShellNavigationTests
    {
        private static readonly WaitForSeconds Wait1_3 = new WaitForSeconds(1.3f);
        private static bool ScreenActive(string name)
        {
            var canvas = GameObject.Find("shell-canvas");
            if (canvas == null) return false;
            // screens live under the safe-area node; walk it (inactive children
            // are invisible to GameObject.Find)
            var t = canvas.transform.Find($"safe-area/{name}");
            if (t == null) t = canvas.transform.Find(name);
            return t != null && t.gameObject.activeSelf;
        }


        [UnityTearDown]
        public IEnumerator UnloadScenes() { yield return SceneCleanup.UnloadAll(); }

        [UnityTest]
        public IEnumerator Boot_LandsOnMenu_AndEveryScreenIsReachable()
        {
            SceneManager.LoadScene("Shell");
            yield return null;
            var shell = Object.FindAnyObjectByType<AppShell>();
            Assert.That(shell, Is.Not.Null, "Shell scene must contain an AppShell");

            yield return Wait1_3; // boot beat is 0.9s
            Assert.That(ScreenActive("menu"), Is.True, "boot should land on the menu");
            Assert.That(ScreenActive("loading"), Is.False);

            foreach (var screen in new[] { "levels", "character", "about", "gameplay", "settings", "studio" })
            {
                shell.Push(screen);
                yield return null;
                Assert.That(ScreenActive(screen), Is.True, $"push should show '{screen}'");
                Assert.That(ScreenActive("menu"), Is.False, $"menu hidden while '{screen}' shows");

                shell.Back();
                yield return null;
                Assert.That(ScreenActive("menu"), Is.True, $"back from '{screen}' returns to menu");
            }
        }

        [UnityTest]
        public IEnumerator BackAtMenuRoot_IsANoOp()
        {
            SceneManager.LoadScene("Shell");
            yield return null;
            var shell = Object.FindAnyObjectByType<AppShell>();
            yield return Wait1_3;

            shell.Back(); // at the root: OS gets it on device; here it must not throw or blank
            yield return null;
            Assert.That(ScreenActive("menu"), Is.True);
        }

        [UnityTest]
        public IEnumerator Lockup_IsTwoToneSpacedFrogAcross()
        {
            SceneManager.LoadScene("Shell");
            yield return null;
            yield return Wait1_3;

            bool sawFrogWhite = false, sawAcrossMint = false;
            var canvas = GameObject.Find("shell-canvas");
            foreach (var text in canvas.GetComponentsInChildren<Text>(true))
            {
                if (text.text == "Frog" && text.color == UiKit.White) sawFrogWhite = true;
                if (text.text == " Across" && text.color == UiKit.Mint) sawAcrossMint = true;
                Assert.That(text.text, Does.Not.Contain("FrogAcross"), "player-visible name is spaced");
            }
            Assert.That(sawFrogWhite, Is.True, "lockup renders \"Frog\" in white");
            Assert.That(sawAcrossMint, Is.True, "lockup renders \" Across\" in mint (#00D6B4)");
        }
    }
}
