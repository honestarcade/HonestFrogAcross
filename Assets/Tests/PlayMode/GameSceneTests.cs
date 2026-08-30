using System.Collections;
using System.IO;
using FrogAcross.Pieces;
using FrogAcross.Services;
using FrogAcross.Sim;
using FrogAcross.UI;
using FrogAcross.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FrogAcross.Tests.PlayMode
{
    /// <summary>#57: the selected character drives gameplay. #60: the clock is
    /// game-time — a frozen sim (dialogs, suspension) adds zero.</summary>
    public class GameSceneTests
    {
        private static readonly WaitForSeconds Wait0_3 = new WaitForSeconds(0.3f);
        private static readonly WaitForSeconds Wait0_4 = new WaitForSeconds(0.4f);
        private static readonly WaitForSeconds Wait0_5 = new WaitForSeconds(0.5f);
        private string _prevCharacter;
        private byte[] _saveBackup;


        [UnityTearDown]
        public IEnumerator UnloadScenes() { yield return SceneCleanup.UnloadAll(); }

        [SetUp]
        public void SetUp()
        {
            _prevCharacter = PlayerPrefs.GetString(CharacterSelection.PrefKey, "");
            _saveBackup = File.Exists(Progression.SavePath) ? File.ReadAllBytes(Progression.SavePath) : null;
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.SetString(CharacterSelection.PrefKey, _prevCharacter);
            PlayerPrefs.Save();
            if (File.Exists(Progression.SavePath)) File.Delete(Progression.SavePath);
            if (_saveBackup != null) File.WriteAllBytes(Progression.SavePath, _saveBackup);
            Progression.ReloadFromDisk();
        }

        private static IEnumerator LoadGame(string levelId)
        {
            AppShell.PendingLevelId = levelId;
            SceneManager.LoadScene("Game");
            yield return null; // Start runs
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheCharacterAnimatesBetweenCells()
        {
            // The sim moves in a single tick; the view has to carry the frog
            // across the gap, or a move reads as a teleport (owner: "character
            // should use its designated move when it moves"). Shape per style
            // is covered by SpriteSelectorTests.MoveArc_IsTheCharactersOwnStyle.
            CharacterSelection.SelectedId = "frog";
            yield return LoadGame("dev-road");
            var boot = Object.FindAnyObjectByType<GameBootstrap>();
            var board = Object.FindAnyObjectByType<BoardView>();
            var player = board.transform.Find("player");
            float row = player.position.y;      // the bank row's line
            float startX = player.position.x;

            // Drive the frames ourselves: batchmode runs the game loop in
            // coarse steps that can swallow a 150ms move whole.
            boot.Frozen = true;
            boot.Sim.EnqueueMove(Move.Right);   // sideways along the bank: always safe
            boot.Sim.Tick();                    // the move executes on this tick
            long hopTick = boot.Sim.State.Tick;

            bool betweenCells = false, leftTheGround = false;
            for (int i = 0; i <= SimConfig.HopCooldownTicks * 2; i++)
            {
                board.Render(hopTick + i * 0.5f);
                float dx = player.position.x - startX;
                if (dx > 0.15f && dx < 0.85f) betweenCells = true;
                if (player.position.y > row + 0.05f) leftTheGround = true;
            }

            Assert.That(betweenCells, Is.True, "the character was never drawn between the two cells");
            Assert.That(leftTheGround, Is.True, "the character never left the row line — no move style played");
            board.Render(hopTick + SimConfig.HopCooldownTicks);
            Assert.That(player.position.x - startX, Is.EqualTo(1f).Within(0.01f), "and it lands on the next cell");
            Assert.That(player.position.y, Is.EqualTo(row).Within(0.01f), "back on the ground when it lands");
        }

        [UnityTest]
        public IEnumerator Hud_SurvivesRestartingAndAdvancingLevels()
        {
            // owner: "buttons and timer disappear if restart level and don't
            // show up again. Sometimes also happens when progressing levels" —
            // BoardView cleared every child of its GameObject on rebuild, and
            // the HUD canvas is one of them.
            yield return LoadGame("dev-road");
            var boot = Object.FindAnyObjectByType<GameBootstrap>();

            for (int round = 0; round < 3; round++)
            {
                boot.StartLevel();      // what Replay does
                yield return null;
                yield return null;

                var hud = GameObject.Find("hud");
                Assert.That(hud, Is.Not.Null, $"round {round}: the HUD canvas is gone");
                int buttons = hud.GetComponentsInChildren<UnityEngine.UI.Button>(true).Length;
                Assert.That(buttons, Is.EqualTo(2), $"round {round}: restart and menu buttons");
                Assert.That(hud.GetComponentsInChildren<UnityEngine.UI.Text>(true).Length,
                    Is.GreaterThan(2), $"round {round}: timer and labels");
                Assert.That(Object.FindObjectsByType<GameHud>(FindObjectsInactive.Include).Length,
                    Is.EqualTo(1), "exactly one HUD, never a stack of them");
            }

            // and the timer still ticks against the rebuilt HUD
            boot.Sim.EnqueueMove(Move.Forward);
            yield return Wait0_4;
            var timer = GameObject.Find("hud").GetComponentsInChildren<UnityEngine.UI.Text>(true);
            bool running = false;
            foreach (var t in timer)
                if (float.TryParse(t.text, out float v) && v > 0f) running = true;
            Assert.That(running, Is.True, "the rebuilt timer is live");
        }

        [UnityTest]
        public IEnumerator SelectedCharacter_IsTheOneOnTheBoard()
        {
            CharacterSelection.SelectedId = "cat";
            yield return LoadGame("dev-road");
            var board = Object.FindAnyObjectByType<BoardView>();
            Assert.That(board, Is.Not.Null);
            Assert.That(board.Character.id, Is.EqualTo("cat"));
            Assert.That(board.Character.moveStyle, Is.EqualTo(MoveStyle.Step));
        }

        [UnityTest]
        public IEnumerator FreshInstall_DefaultsToFrog()
        {
            CharacterSelection.Reset();
            yield return LoadGame("dev-road");
            var board = Object.FindAnyObjectByType<BoardView>();
            Assert.That(board.Character.id, Is.EqualTo("frog"));
        }

        [UnityTest]
        public IEnumerator Camera_FramesTheBoundLevel_WithTheDesignTilt()
        {
            // The design draws the board rolled -8° and bleeding off every edge
            // (owner correction 2026-08-29: my first pass removed the tilt).
            yield return LoadGame("dev-road");
            var bootstrap = Object.FindAnyObjectByType<GameBootstrap>();
            var cam = Camera.main;
            var level = bootstrap.Sim.Level;

            Assert.That(cam.transform.eulerAngles.z,
                Is.EqualTo(360f + GameBootstrap.BoardRollDegrees).Within(0.01f), "design roll");
            Assert.That(cam.transform.position.x,
                Is.EqualTo((level.Columns - 1) / 2f).Within(0.001f), "board centered");

            float halfW = cam.orthographicSize * cam.aspect, halfH = cam.orthographicSize;
            foreach (var corner in new[]
                     {
                         new Vector3(-0.5f, 0.5f, 0f),
                         new Vector3(level.Columns - 0.5f, 0.5f, 0f),
                         new Vector3(-0.5f, -(level.BankRow + 0.5f), 0f),
                         new Vector3(level.Columns - 0.5f, -(level.BankRow + 0.5f), 0f),
                     })
            {
                var local = cam.transform.InverseTransformPoint(corner);
                Assert.That(Mathf.Abs(local.y), Is.LessThanOrEqualTo(halfH + 0.001f),
                    $"corner {corner} must stay inside the rolled frame");
            }
        }

        [UnityTest]
        public IEnumerator FrozenSim_AddsZeroToTheClock()
        {
            yield return LoadGame("dev-road");
            var bootstrap = Object.FindAnyObjectByType<GameBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);

            bootstrap.Sim.EnqueueMove(Move.Forward); // clock starts on first move
            yield return Wait0_4;
            long before = bootstrap.Sim.State.ClockTicks;
            Assert.That(before, Is.GreaterThan(0), "clock must be running after the first move");

            bootstrap.Frozen = true; // what dialogs and OS suspension amount to
            yield return Wait0_5;
            Assert.That(bootstrap.Sim.State.ClockTicks, Is.EqualTo(before),
                "a frozen sim adds nothing to the game-time clock");

            bootstrap.Frozen = false;
            yield return Wait0_3;
            Assert.That(bootstrap.Sim.State.ClockTicks, Is.GreaterThan(before),
                "clock resumes after unfreezing");
        }
    }
}
