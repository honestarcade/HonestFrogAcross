using System.Collections;
using System.IO;
using FrogAcross.Levels;
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
            Assert.That(cell1.Find("medal"), Is.Not.Null, "completed level shows its medal dot");
            Assert.That(cell1.GetComponent<Button>(), Is.Not.Null, "completed level stays launchable");
            bool showsTime = false;
            foreach (var text in cell1.GetComponentsInChildren<Text>(true))
                if (text.text == "10.0s") showsTime = true;
            Assert.That(showsTime, Is.True, "completed cell shows the best time");

            var cell2 = Cell(2);
            Assert.That(cell2.Find("medal"), Is.Null, "unplayed level has no medal");
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
    }
}
