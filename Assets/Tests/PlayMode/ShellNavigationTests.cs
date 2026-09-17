using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
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
        public IEnumerator RegionsPreview_ClosesOnATapAnywhere()
        {
            // owner: "opening the regions overlay works, however closing it is
            // spotty… tapping towards the center works, but not the edges" —
            // the zone captions were raycast targets sitting over the scrim,
            // and a 600-wide caption covers a 20%-wide side zone completely
            SceneManager.LoadScene("Shell");
            yield return null;
            var shell = Object.FindAnyObjectByType<AppShell>();
            yield return Wait1_3;

            SettingsScreen.ShowRegionsPreview(shell.transform);
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            Assert.That(GameObject.Find("regions-preview"), Is.Not.Null, "the preview opened");

            var events = EventSystem.current;
            var hits = new List<RaycastResult>();
            for (int ix = 0; ix <= 10; ix++)
            for (int iy = 0; iy <= 10; iy++)
            {
                var point = new Vector2(
                    Mathf.Lerp(2f, Screen.width - 2f, ix / 10f),
                    Mathf.Lerp(2f, Screen.height - 2f, iy / 10f));
                hits.Clear();
                events.RaycastAll(new PointerEventData(events) { position = point }, hits);
                Assert.That(hits.Count, Is.GreaterThan(0), $"nothing under {point}");
                Assert.That(hits[0].gameObject.name, Is.EqualTo("dismiss"),
                    $"a tap at {point} must reach the dismiss sheet, not '{hits[0].gameObject.name}'");
            }

            // and the tap actually closes it — near the left edge, the corner
            // the owner could not dismiss from
            var edge = new Vector2(Screen.width * 0.04f, Screen.height * 0.5f);
            hits.Clear();
            events.RaycastAll(new PointerEventData(events) { position = edge }, hits);
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,
                new PointerEventData(events) { position = edge }, ExecuteEvents.pointerClickHandler);
            yield return null;
            Assert.That(GameObject.Find("regions-preview"), Is.Null, "one tap closes the preview");
        }

        [UnityTest]
        public IEnumerator EveryButtonLabel_IsOnTheTypeScale()
        {
            // The confirm dialogs never passed a fontSize, so they inherited
            // UiKit.Button's silent default of 22 while every other button in
            // the game runs at 46 or 64 — the owner read it as "tiny" (#121).
            // Guarding the class, not the two dialogs: any future button that
            // forgets to opt in fails here.
            SceneManager.LoadScene("Shell");
            yield return null;
            var shell = Object.FindAnyObjectByType<AppShell>();
            yield return Wait1_3;

            // Scoped to UiKit.Button's own objects ("btn-*"), which is where the
            // silent default lives. Buttons that WRAP content — a levels-grid
            // cell whose text is the level number, the support card whose text
            // is a paragraph — are a different pattern and set their own sizes.
            // Floor is Body: a button label should never be smaller than body
            // copy, and it clears the 44 the header chevron deliberately uses.
            var offenders = new List<string>();
            void Audit(GameObject where, string context)
            {
                foreach (var button in where.GetComponentsInChildren<Button>(true))
                {
                    if (!button.name.StartsWith("btn-")) continue;
                    foreach (var label in button.GetComponentsInChildren<Text>(true))
                    {
                        if (string.IsNullOrWhiteSpace(label.text)) continue;
                        if (label.fontSize < UiKit.Body)
                            offenders.Add($"{context}/{button.name}: '{label.text}' at {label.fontSize}");
                    }
                }
            }

            foreach (var screen in new[] { "menu", "levels", "character", "about", "gameplay", "settings", "studio" })
            {
                shell.Push(screen);
                yield return null;
                Audit(GameObject.Find("shell-canvas"), screen);
                shell.Back();
                yield return null;
            }

            // and the surfaces built ON DEMAND, which sit on no screen and so
            // went unaudited — that is exactly how #121 reached a phone (#130)
            var dialog = ConfirmDialog.Show(shell.transform, "Restart level?",
                "Bays and the clock reset — this attempt is abandoned.", "Restart", () => { });
            yield return null;
            Audit(dialog, "confirm-dialog");
            Object.Destroy(dialog);

            var overlayHost = new GameObject("overlay-audit");
            var overlay = overlayHost.AddComponent<LevelCompleteOverlay>();
            overlay.Show(LevelLoader.LoadFromResources("level-001", PieceRegistry.Load()),
                260, newBest: true, prevBest: 6.8f, levelNumber: 1);
            yield return null;
            Audit(overlayHost, "level-complete");
            Object.Destroy(overlayHost);

            Assert.That(offenders, Is.Empty,
                "button labels below UiKit.Body (" + UiKit.Body + "): "
                + string.Join(" | ", offenders));
            Object.Destroy(dialog);
        }

        [UnityTest]
        public IEnumerator ConfirmDialog_CentresItsBodyClearOfTheTitle()
        {
            SceneManager.LoadScene("Shell");
            yield return null;
            var shell = Object.FindAnyObjectByType<AppShell>();
            yield return Wait1_3;

            var dialog = ConfirmDialog.Show(shell.transform, "Quit to menu?",
                "Your current run is abandoned.", "Quit", () => { });
            yield return null;

            var texts = dialog.GetComponentsInChildren<Text>(true);
            var title = texts.First(t => t.text == "Quit to menu?");
            var body = texts.First(t => t.text == "Your current run is abandoned.");

            Assert.That(body.alignment, Is.EqualTo(TextAnchor.UpperCenter),
                "the body reads centred, against centred buttons");

            // Rendered EDGES, not anchor centres. The old form measured
            // centre-to-centre and read 130 on the pre-fix layout versus 120
            // after — it went the wrong way and still passed, so it pinned
            // nothing (#128). Title bottom to body top is what a reader sees.
            float titleBottom = title.rectTransform.anchoredPosition.y
                - title.rectTransform.sizeDelta.y / 2f;
            float bodyTop = body.rectTransform.anchoredPosition.y
                + body.rectTransform.sizeDelta.y / 2f;
            float gap = titleBottom - bodyTop;
            Assert.That(gap, Is.GreaterThanOrEqualTo(25f),
                $"only {gap:0} units between the title's bottom and the body's top — "
                + "the copy is crowding the heading");
            Object.Destroy(dialog);
        }

        [UnityTest]
        public IEnumerator SettingsSectionLabels_AllMatchTheSameStyle()
        {
            // The four sound toggles opened the screen with no heading while
            // CONTROLS and DATA both had one (#125). Asserting the style
            // matches, not just that some text exists.
            SceneManager.LoadScene("Shell");
            yield return null;
            var shell = Object.FindAnyObjectByType<AppShell>();
            yield return Wait1_3;
            shell.Push("settings");
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;

            var settings = GameObject.Find("shell-canvas").transform.Find("safe-area/settings");
            var texts = settings.GetComponentsInChildren<Text>(true);
            var labels = new[] { "SOUND", "CONTROLS", "DATA" }
                .Select(name => texts.FirstOrDefault(t => t.text == name))
                .ToList();
            for (int i = 0; i < labels.Count; i++)
                Assert.That(labels[i], Is.Not.Null,
                    $"settings is missing the '{new[] { "SOUND", "CONTROLS", "DATA" }[i]}' section label");

            foreach (var label in labels)
            {
                Assert.That(label.fontSize, Is.EqualTo(labels[1].fontSize), $"'{label.text}' font size");
                Assert.That(label.color, Is.EqualTo(labels[1].color), $"'{label.text}' colour");
                Assert.That(label.alignment, Is.EqualTo(labels[1].alignment), $"'{label.text}' alignment");
            }

            // and it sits ABOVE the first sound row, not merely somewhere
            var firstRow = settings.GetComponentsInChildren<Transform>(true)
                .First(t => t.name == "row-All sound");
            Assert.That(labels[0].transform.position.y, Is.GreaterThan(firstRow.position.y),
                "the SOUND label must sit above the first toggle it labels");
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
