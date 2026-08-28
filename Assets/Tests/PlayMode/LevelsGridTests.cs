using System.Collections;
using System.IO;
using FrogAcross.Levels;
using FrogAcross.Services;
using FrogAcross.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
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
    }
}
