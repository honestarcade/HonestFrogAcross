using System.Collections;
using System.Linq;
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
        public IEnumerator ChangingASetting_LeavesBackWorkingFirstPress()
        {
            // owner report: after picking a character (or a control scheme) the
            // back button did nothing until pressed twice — the screen rebuilt
            // itself with Push, stacking a second copy of itself
            SceneManager.LoadScene("Shell");
            yield return null;
            var shell = Object.FindAnyObjectByType<AppShell>();
            yield return Wait1_3;

            shell.Push("character");
            yield return null;
            shell.Replace("character", CharacterScreen.Build); // what selecting does
            yield return null;
            Assert.That(ScreenActive("character"), Is.True, "still on the rebuilt screen");

            shell.Back();
            yield return null;
            Assert.That(ScreenActive("menu"), Is.True, "one press returns to the menu");

            shell.Push("settings");
            yield return null;
            shell.Replace("settings", SettingsScreen.Build);
            yield return null;
            shell.Back();
            yield return null;
            Assert.That(ScreenActive("menu"), Is.True, "same for settings");
        }

        [UnityTest]
        public IEnumerator ChangingASetting_KeepsYourPlaceInTheScroll()
        {
            // owner: flipping the control scheme halfway down Settings snapped
            // the page back to the top
            SceneManager.LoadScene("Shell");
            yield return null;
            var shell = Object.FindAnyObjectByType<AppShell>();
            yield return Wait1_3;
            shell.Push("settings");
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;

            var settings = GameObject.Find("shell-canvas").transform.Find("safe-area/settings");
            var scroll = settings.GetComponentInChildren<ScrollRect>(true);
            Assert.That(scroll.viewport.GetComponent<Graphic>(), Is.Not.Null,
                "the viewport needs a raycast surface or drags over empty space do nothing");

            scroll.verticalNormalizedPosition = 0.25f;
            yield return null;
            shell.Replace("settings", SettingsScreen.Build);
            yield return null;
            yield return null;

            var rebuilt = GameObject.Find("shell-canvas").transform.Find("safe-area/settings")
                .GetComponentInChildren<ScrollRect>(true);
            Assert.That(rebuilt.verticalNormalizedPosition, Is.EqualTo(0.25f).Within(0.05f),
                "the rebuilt screen keeps your place");
        }

        [UnityTest]
        public IEnumerator StudioScreen_SupportBoxLinksOut_AndDropsTheFooterLinks()
        {
            SceneManager.LoadScene("Shell");
            yield return null;
            var shell = Object.FindAnyObjectByType<AppShell>();
            yield return Wait1_3;
            shell.Push("studio");
            yield return null;

            var studio = GameObject.Find("shell-canvas").transform.Find("safe-area/studio");
            var support = studio.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "support-card");
            Assert.That(support, Is.Not.Null);
            Assert.That(support.GetComponent<Button>(), Is.Not.Null, "the whole box is the link");
            Assert.That(StaticScreens.SupportUrl, Is.EqualTo("https://honestarcade.app/contribute"));

            foreach (var text in studio.GetComponentsInChildren<Text>(true))
                Assert.That(text.text, Does.Not.Contain("SOURCE ON GITHUB"),
                    "the footer link lines were removed");
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
