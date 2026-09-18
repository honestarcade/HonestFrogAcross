using System.Collections;
using System.Linq;
using System.IO;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.Services;
using FrogAcross.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FrogAcross.Tests.PlayMode
{
    /// <summary>#56: cell states render from persistence — completed-with-medal,
    /// unlocked-unplayed launchable, locked not launchable.</summary>
    public class LevelsGridTests
    {
        private static readonly WaitForSeconds Wait1_3 = new WaitForSeconds(1.3f);
        private byte[] _saveBackup;


        [UnityTearDown]
        public IEnumerator UnloadScenes() { yield return SceneCleanup.UnloadAll(); }

        [SetUp]
        public void SetUp()
        {
            _saveBackup = File.Exists(Progression.SavePath) ? File.ReadAllBytes(Progression.SavePath) : null;
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(Progression.SavePath)) File.Delete(Progression.SavePath);
            if (_saveBackup != null) File.WriteAllBytes(Progression.SavePath, _saveBackup);
            Progression.ReloadFromDisk();
        }

        [UnityTest]
        public IEnumerator CellStates_ComeFromTheSaveFile()
        {
            // fixture: level 1 completed with gold, so 2 is unlocked-unplayed and 3 locked
            Progression.ResetAll();
            Progression.ReportCompletion(LevelCatalog.IdFor(1), 1, 10f, 15f, 20f, 30f);

            SceneManager.LoadScene("Shell");
            yield return null;
            var shell = Object.FindAnyObjectByType<AppShell>();
            yield return Wait1_3;

            shell.RebuildScreen("levels", LevelsScreen.Build);
            shell.Push("levels");
            yield return null;

            var canvas = GameObject.Find("shell-canvas").transform;
            Transform Cell(int n)
            {
                foreach (var t in canvas.GetComponentsInChildren<Transform>(true))
                    if (t.name == $"cell-{n}") return t;
                return null;
            }

            var cell1 = Cell(1);
            Assert.That(cell1, Is.Not.Null);
            Assert.That(cell1.Find("medal").GetComponent<Image>().color, Is.EqualTo(UiKit.Gold),
                "completed level shows its medal colour behind the number");
            Assert.That(cell1.GetComponent<Button>(), Is.Not.Null, "completed level stays launchable");
            bool showsTime = false;
            foreach (var text in cell1.GetComponentsInChildren<Text>(true))
                if (text.text == "10.0s") showsTime = true;
            Assert.That(showsTime, Is.True, "completed cell shows the best time");

            // every cell carries the disc now (it is the number's backing); an
            // unearned one is neutral rather than absent
            var cell2 = Cell(2);
            var disc2 = cell2.Find("medal").GetComponent<Image>();
            Assert.That(disc2.color, Is.Not.EqualTo(UiKit.Gold).And.Not.EqualTo(UiKit.Silver)
                .And.Not.EqualTo(UiKit.Bronze), "unplayed level shows no medal colour");
            Assert.That(cell2.GetComponent<Button>(), Is.Not.Null, "unlocked level is launchable");

            var cell3 = Cell(3);
            Assert.That(cell3.GetComponent<Button>(), Is.Null, "locked level must not launch");
        }

        [UnityTest]
        public IEnumerator AllLevelsOnOneScrollingSurface_HeaderStaysPut()
        {
            // owner ruling (2026-08-29): pagination removed — every level is
            // on one scrolling surface, header fixed above it
            Progression.ResetAll();
            SceneManager.LoadScene("Shell");
            yield return null;
            var shell = Object.FindAnyObjectByType<AppShell>();
            yield return Wait1_3;
            shell.RebuildScreen("levels", LevelsScreen.Build);
            shell.Push("levels");
            yield return null;

            var levels = GameObject.Find("shell-canvas").transform.Find("safe-area/levels");
            Assert.That(levels, Is.Not.Null);

            int cells = 0;
            foreach (var t in levels.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("cell-")) cells++;
            Assert.That(cells, Is.EqualTo(LevelCatalog.Count), "every level is present at once");

            var scroll = levels.GetComponentInChildren<ScrollRect>(true);
            Assert.That(scroll, Is.Not.Null, "the grid scrolls");
            Assert.That(scroll.vertical, Is.True);
            Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height),
                "content taller than the viewport — there is something to scroll");

            // the header is outside the scrolling content, so it cannot scroll away
            var header = levels.Find("header");
            Assert.That(header, Is.Not.Null, "levels screen uses the shared header");
            Assert.That(header.IsChildOf(scroll.content), Is.False, "header must not scroll");
            Assert.That(header.GetSiblingIndex(), Is.EqualTo(levels.childCount - 1),
                "header renders above the grid so the back button is always tappable");
        }

        [UnityTest]
        public IEnumerator Grid_IsTenAcross_WithMedalDiscsAndNoLeadingGap()
        {
            Progression.ResetAll();
            Progression.ReportCompletion(LevelCatalog.IdFor(1), 1, 1f, 60f, 90f, 120f); // gold
            SceneManager.LoadScene("Shell");
            yield return null;
            var shell = Object.FindAnyObjectByType<AppShell>();
            yield return Wait1_3;
            shell.RebuildScreen("levels", LevelsScreen.Build);
            shell.Push("levels");
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;

            var levels = GameObject.Find("shell-canvas").transform.Find("safe-area/levels");
            var grid = levels.GetComponentInChildren<GridLayoutGroup>(true);
            Assert.That(grid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
            Assert.That(grid.constraintCount, Is.EqualTo(10), "owner asked for exactly ten across");

            // cells one and eleven start a row each: no phantom slot before level 1
            var cell1 = (RectTransform)grid.transform.Find("cell-1");
            var cell10 = (RectTransform)grid.transform.Find("cell-10");
            var cell11 = (RectTransform)grid.transform.Find("cell-11");
            Assert.That(cell10.anchoredPosition.y, Is.EqualTo(cell1.anchoredPosition.y).Within(0.5f),
                "levels 1-10 share the first row");
            Assert.That(cell11.anchoredPosition.y, Is.LessThan(cell1.anchoredPosition.y),
                "level 11 starts the second row");
            Assert.That(cell11.anchoredPosition.x, Is.EqualTo(cell1.anchoredPosition.x).Within(0.5f),
                "and lines up under level 1 — no leading gap");

            // the medal is the disc behind the number, identical on every cell
            var disc1 = (RectTransform)cell1.Find("medal");
            var disc100 = (RectTransform)grid.transform.Find("cell-100").Find("medal");
            Assert.That(disc1, Is.Not.Null, "completed level shows its medal disc");
            Assert.That(disc1.rect.size, Is.EqualTo(disc100.rect.size), "same disc size for 1 and 100");
            Assert.That(disc1.GetComponent<Image>().color, Is.EqualTo(UiKit.Gold), "gold level, gold disc");
            Assert.That(disc1.GetComponentInChildren<Outline>(), Is.Not.Null, "number is outlined");
        }

        [UnityTest]
        public IEnumerator ResetAllData_RefreshesTheBuiltScreens()
        {
            // #89: the wipe rebuilds menu/levels/character, no restart needed
            Progression.ResetAll();
            Progression.ReportCompletion(LevelCatalog.IdFor(1), 1, 10f, 15f, 20f, 30f);
            SceneManager.LoadScene("Shell");
            yield return null;
            var shell = Object.FindAnyObjectByType<AppShell>();
            yield return Wait1_3;

            var canvas = GameObject.Find("shell-canvas").transform;
            bool MenuSays(string text)
            {
                foreach (var t in canvas.GetComponentsInChildren<Text>(true))
                    if (t.text.Contains(text)) return true;
                return false;
            }
            Assert.That(MenuSays("Continue — Level 2"), Is.True, "seeded save shows level 2");

            FrogAcross.Services.DataWipe.WipeAll();
            shell.RefreshDataScreens();
            yield return null;
            Assert.That(MenuSays("Continue — Level 1"), Is.True, "wipe + refresh returns the menu to level 1");
            Assert.That(MenuSays("Continue — Level 2"), Is.False, "no stale screen survives the wipe");
        }

        [UnityTest]
        public IEnumerator LongPressingALevel_ShowsItsMedalTimes_AndLaunchesNothing()
        {
            // owner: "if the user taps and holds a level, a bubble should pop up
            // to show the times needed for the various medals" (#123)
            SceneManager.LoadScene("Shell");
            yield return null;
            var shell = Object.FindAnyObjectByType<AppShell>();
            yield return Wait1_3;
            shell.Push("levels");
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;

            var levels = GameObject.Find("shell-canvas").transform.Find("safe-area/levels");
            var cell = levels.GetComponentsInChildren<Transform>(true).First(t => t.name == "cell-1");
            var press = cell.GetComponent<LongPress>();
            Assert.That(press, Is.Not.Null, "every cell is long-pressable");

            AppShell.PendingLevelId = null;
            press.SimulateHold();
            yield return null;

            var bubble = levels.Find(LevelsScreen.BubbleName);
            Assert.That(bubble, Is.Not.Null, "the hold shows the medal-times bubble");

            var level = LevelLoader.LoadFromResources("level-001", PieceRegistry.Load());
            var shown = bubble.GetComponentsInChildren<Text>(true).Select(t => t.text).ToList();
            foreach (float seconds in new[] { level.GoldSeconds, level.SilverSeconds, level.BronzeSeconds })
                Assert.That(shown, Does.Contain($"{seconds:0.0}s"),
                    $"the bubble lists the {seconds:0.0}s threshold — shown: {string.Join(", ", shown)}");

            // The hold must SUPPRESS the click that follows it. The earlier
            // version asserted PendingLevelId was null without ever invoking
            // onClick — a value nothing could have changed (#127).
            Assert.That(press.Held, Is.True, "the cell knows a hold happened");
            cell.GetComponent<Button>().onClick.Invoke();
            Assert.That(AppShell.PendingLevelId, Is.Null,
                "a preview must not launch: the click after a hold is suppressed");
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Shell"));

            // and the bubble goes away on release
            press.OnRelease?.Invoke();
            yield return null;
            Assert.That(levels.Find(LevelsScreen.BubbleName), Is.Null, "release dismisses the bubble");
        }

        [UnityTest]
        public IEnumerator AShortTapStillLaunchesTheLevel()
        {
            SceneManager.LoadScene("Shell");
            yield return null;
            var shell = Object.FindAnyObjectByType<AppShell>();
            yield return Wait1_3;
            shell.Push("levels");
            yield return null;

            var levels = GameObject.Find("shell-canvas").transform.Find("safe-area/levels");
            var cell = levels.GetComponentsInChildren<Transform>(true).First(t => t.name == "cell-1");
            Assert.That(cell.GetComponent<LongPress>().Held, Is.False,
                "no hold has happened, so the click must not be suppressed");

            // `.Or.Null` made this unfailable — it passed when nothing launched.
            // LaunchLevel sets PendingLevelId then loads the scene, and the load
            // completes on a later frame, so the id is readable right here (#127).
            AppShell.PendingLevelId = null;
            cell.GetComponent<Button>().onClick.Invoke();
            Assert.That(AppShell.PendingLevelId, Is.EqualTo("level-001"),
                "a plain tap still launches the level");
            yield return null;
        }

        /// <summary>Owner, device UAT on v0.11.0: "it stays if you keep your
        /// finger in the same place, but if you move your finger, even to roll
        /// it, the popup disappears" (#146).</summary>
        [UnityTest]
        public IEnumerator MovingTheFinger_DoesNotDismissThePreview()
        {
            yield return OpenLevels();
            var levels = LevelsRoot();
            var cell = levels.GetComponentsInChildren<Transform>(true).First(t => t.name == "cell-1");
            var press = cell.GetComponent<LongPress>();

            press.SimulateHold();
            yield return null;
            Assert.That(levels.Find(LevelsScreen.BubbleName), Is.Not.Null, "the hold opened the preview");

            // the finger moves off the cell while still down
            press.OnPointerExit(new PointerEventData(EventSystem.current));
            yield return null;
            Assert.That(levels.Find(LevelsScreen.BubbleName), Is.Not.Null,
                "moving the finger must not close the preview — the times sit beside "
                + "the finger, so reading them requires moving it");

            // lifting still dismisses
            press.OnPointerUp(new PointerEventData(EventSystem.current));
            yield return null;
            Assert.That(levels.Find(LevelsScreen.BubbleName), Is.Null, "release dismisses it");
        }

        /// <summary>A scroll drag STARTED before the hold completes must still
        /// cancel, or the grid stops scrolling (#123's ScrollRect guarantee).</summary>
        [UnityTest]
        public IEnumerator AScrollBeforeTheHoldCompletes_StillCancels()
        {
            yield return OpenLevels();
            var levels = LevelsRoot();
            var press = levels.GetComponentsInChildren<Transform>(true)
                .First(t => t.name == "cell-1").GetComponent<LongPress>();

            // Drive the REAL timer, not SimulateHold(): that seam forces _down
            // back to true, so it cannot express a press that was cancelled.
            press.OnPointerDown(new PointerEventData(EventSystem.current));
            press.OnPointerExit(new PointerEventData(EventSystem.current));
            yield return new WaitForSeconds(press.holdSeconds * 2f);
            Assert.That(press.Held, Is.False, "the cancelled press never became a hold");
            Assert.That(levels.Find(LevelsScreen.BubbleName), Is.Null,
                "a finger that left before the hold completed was scrolling, not previewing");

            // and the same press, left alone, DOES become a hold — otherwise
            // this test would pass on a LongPress that never fires at all
            press.OnPointerDown(new PointerEventData(EventSystem.current));
            yield return new WaitForSeconds(press.holdSeconds * 2f);
            Assert.That(press.Held, Is.True, "an uninterrupted press still completes");
            Assert.That(levels.Find(LevelsScreen.BubbleName), Is.Not.Null,
                "an uninterrupted press still opens the preview");
            press.OnPointerUp(new PointerEventData(EventSystem.current));
        }

        /// <summary>The bubble must never be drawn under the finger holding it,
        /// and must flip side so it stays on screen (#146).</summary>
        [UnityTest]
        public IEnumerator ThePreview_SitsBesideTheCell_OnTheSideWithRoom()
        {
            yield return OpenLevels();
            var levels = LevelsRoot();
            var cells = levels.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith("cell-")).Select(t => (RectTransform)t).ToList();
            Assert.That(cells.Count, Is.GreaterThan(1), "need several cells to test both sides");

            var parent = (RectTransform)levels;
            var leftCell = cells.OrderBy(c => parent.InverseTransformPoint(c.position).x).First();
            var rightCell = cells.OrderByDescending(c => parent.InverseTransformPoint(c.position).x).First();

            foreach (var (cell, label) in new[] { (leftCell, "left-hand"), (rightCell, "right-hand") })
            {
                cell.GetComponent<LongPress>().SimulateHold();
                yield return null;
                var bubble = (RectTransform)levels.Find(LevelsScreen.BubbleName);
                Assert.That(bubble, Is.Not.Null, $"{label} cell opened a preview");

                float cellX = parent.InverseTransformPoint(cell.position).x;
                float bubbleX = bubble.anchoredPosition.x;
                if (cellX > 0f)
                    Assert.That(bubbleX, Is.LessThan(cellX),
                        $"{label} cell is in the right half — the bubble opens to its LEFT");
                else
                    Assert.That(bubbleX, Is.GreaterThan(cellX),
                        $"{label} cell is in the left half — the bubble opens to its RIGHT");

                // and it does not overlap the cell itself: that is what put the
                // bronze row under the owner's finger
                float gap = Mathf.Abs(bubbleX - cellX)
                            - (bubble.sizeDelta.x * 0.5f + cell.rect.width * 0.5f);
                Assert.That(gap, Is.GreaterThan(0f),
                    $"{label}: the bubble overlaps its own cell by {-gap:0} units — it is under the finger");

                // it must also stay inside the screen
                Assert.That(Mathf.Abs(bubbleX) + bubble.sizeDelta.x * 0.5f,
                    Is.LessThanOrEqualTo(parent.rect.width * 0.5f + 0.5f),
                    $"{label}: the bubble runs off the screen edge");

                cell.GetComponent<LongPress>().OnPointerUp(new PointerEventData(EventSystem.current));
                yield return null;
            }
        }

        /// <summary>The root cause of #146: the bubble is drawn above the cell,
        /// so a raycast target in it steals the pointer, the cell gets
        /// OnPointerExit, and the hold cancels itself.</summary>
        [UnityTest]
        public IEnumerator ThePreview_TakesNoRaycasts()
        {
            yield return OpenLevels();
            var levels = LevelsRoot();
            levels.GetComponentsInChildren<Transform>(true)
                .First(t => t.name == "cell-1").GetComponent<LongPress>().SimulateHold();
            yield return null;

            var bubble = levels.Find(LevelsScreen.BubbleName);
            Assert.That(bubble, Is.Not.Null);
            var greedy = bubble.GetComponentsInChildren<Graphic>(true)
                .Where(g => g.raycastTarget).Select(g => g.name).ToList();
            Assert.That(greedy, Is.Empty,
                "these steal the pointer from the cell underneath and cancel the hold: "
                + string.Join(", ", greedy));
        }

        private static IEnumerator OpenLevels()
        {
            SceneManager.LoadScene("Shell");
            yield return null;
            var shell = Object.FindAnyObjectByType<AppShell>();
            yield return Wait1_3;
            shell.Push("levels");
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
        }

        private static Transform LevelsRoot() =>
            GameObject.Find("shell-canvas").transform.Find("safe-area/levels");
    }
}
