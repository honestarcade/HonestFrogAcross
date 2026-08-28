using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FrogAcross.Audio;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.Services;
using FrogAcross.Sim;
using FrogAcross.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FrogAcross.Tests.PlayMode
{
    /// <summary>
    /// #65: the scripted every-hook run. Batchmode has no audio engine, so
    /// hooks are asserted via the director's PlayedCounts probe — the sound
    /// routing is what's under test, actual output is the #66 device UAT.
    /// </summary>
    public class AudioHookTests
    {
        private static readonly WaitForSeconds Wait1_3 = new WaitForSeconds(1.3f);
        private int _master, _music, _effects, _ui;

        [SetUp]
        public void SetUp()
        {
            _master = PlayerPrefs.GetInt("sound.master", 1);
            _music = PlayerPrefs.GetInt("sound.music", 1);
            _effects = PlayerPrefs.GetInt("sound.effects", 1);
            _ui = PlayerPrefs.GetInt("sound.ui", 1);
            AudioDirector.Instance.PlayedCounts.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.SetInt("sound.master", _master);
            PlayerPrefs.SetInt("sound.music", _music);
            PlayerPrefs.SetInt("sound.effects", _effects);
            PlayerPrefs.SetInt("sound.ui", _ui);
            PlayerPrefs.Save();
        }

        [UnityTearDown]
        public IEnumerator UnloadScenes() { yield return SceneCleanup.UnloadAll(); }

        private static int Count(GameSound s) => AudioDirector.Instance.PlayedCounts.GetValueOrDefault(s);

        private static GameSim MakeSim(string levelId, out LevelDefinition level)
        {
            level = LevelLoader.LoadFromResources(levelId, PieceRegistry.Load());
            var sim = new GameSim(level);
            AudioDirector.Instance.Bind(sim, level);
            return sim;
        }

        [UnityTest]
        public IEnumerator GameplayHooks_AllFire()
        {
            var director = AudioDirector.Instance;

            // hop + road death
            var sim = MakeSim("dev-road", out _);
            sim.EnqueueMove(Move.Forward);
            for (int i = 0; i < 3600 && Count(GameSound.DeathSplat) == 0; i++)
            {
                sim.Tick();
                if (Count(GameSound.Hop) > 0 && sim.State.RespawnDelay == 0
                    && sim.State.MoveQueue.Count == 0 && sim.State.PlayerRow == sim.Level.BankRow)
                    sim.EnqueueMove(Move.Forward); // keep walking into traffic
            }
            Assert.That(Count(GameSound.Hop), Is.GreaterThan(0), "hop hook");
            Assert.That(Count(GameSound.DeathSplat), Is.GreaterThan(0), "road-death hook");

            // bay fill + level complete (nudge the player next to each bay)
            sim = MakeSim("dev-road", out var road);
            foreach (int bay in road.BayColumns.OrderBy(b => b).ToList())
            {
                for (int i = 0; i < 300 && (sim.State.RespawnDelay > 0 || sim.State.HopCooldown > 0); i++) sim.Tick();
                sim.State.PlayerRow = 1;
                sim.State.PlayerX = bay;
                sim.State.Riding = false;
                sim.EnqueueMove(Move.Forward);
                for (int i = 0; i < 300 && !sim.State.BaysFilled.Contains(bay); i++) sim.Tick();
                Assert.That(sim.State.BaysFilled, Does.Contain(bay), $"bay {bay} should fill");
            }
            Assert.That(Count(GameSound.BayFill), Is.EqualTo(road.BayColumns.Count), "bay-fill hook per bay");
            Assert.That(Count(GameSound.LevelComplete), Is.EqualTo(1), "level-complete hook");

            // rider crash + stun
            sim = MakeSim("dev-bike", out var bike);
            int bikeRow = -1;
            for (int r = 0; r < bike.Rows.Count; r++)
                if (bike.Rows[r].Kind.semantics == LaneSemantics.CrashTraffic) { bikeRow = r; break; }
            sim.State.PlayerRow = bikeRow;
            sim.State.PlayerX = 4f;
            for (int i = 0; i < 7200 && Count(GameSound.RiderCrash) == 0; i++) sim.Tick();
            Assert.That(Count(GameSound.RiderCrash), Is.GreaterThan(0), "rider-crash hook");
            Assert.That(Count(GameSound.Stun), Is.GreaterThan(0), "stun hook");

            // train warning (LateUpdate poll — tick in frame-sized chunks)
            sim = MakeSim("dev-tracks", out _);
            for (int frame = 0; frame < 600 && Count(GameSound.TrainWarning) == 0; frame++)
            {
                for (int i = 0; i < 8; i++) sim.Tick();
                yield return null;
            }
            Assert.That(Count(GameSound.TrainWarning), Is.GreaterThan(0), "train-warning hook");

            // turtle warning: trigger rule is unit-tested (AudioGuardTests);
            // the routed hook itself:
            director.Play(GameSound.TurtleWarning);
            Assert.That(Count(GameSound.TurtleWarning), Is.GreaterThan(0), "turtle-warning hook");

            // medal from the overlay, music slots
            var host = new GameObject("overlay-host");
            var overlay = host.AddComponent<LevelCompleteOverlay>();
            overlay.Show(road, 60); // 1s: gold on the dev board
            Assert.That(Count(GameSound.Medal), Is.GreaterThan(0), "medal hook");
            Object.Destroy(host);

            director.PlayMusic(MusicSlot.Menu);
            Assert.That(director.CurrentMusic, Is.EqualTo(MusicSlot.Menu));
            director.PlayMusic(MusicSlot.Gameplay);
            Assert.That(director.CurrentMusic, Is.EqualTo(MusicSlot.Gameplay));
        }

        [UnityTest]
        public IEnumerator UiHooks_FireFromShellNavigation()
        {
            SceneManager.LoadScene("Shell");
            yield return null;
            var shell = Object.FindAnyObjectByType<AppShell>();
            yield return Wait1_3;
            AudioDirector.Instance.PlayedCounts.Clear();

            shell.Push("settings");
            yield return null;
            Assert.That(Count(GameSound.UiNavigate), Is.GreaterThan(0), "navigate hook");
            Assert.That(AudioDirector.Instance.CurrentMusic, Is.EqualTo(MusicSlot.Menu), "menu music slot");

            var button = UiKit.Button(shell.transform, "probe", Vector2.zero, new Vector2(10, 10), null);
            button.onClick.Invoke();
            Assert.That(Count(GameSound.UiTap), Is.GreaterThan(0), "tap hook");
        }

        [Test]
        public void SettingsToggles_RebindBusesImmediately()
        {
            var director = AudioDirector.Instance;
            SoundSettings.Master = true;
            SoundSettings.Music = true;
            Assert.That(director.AppliedDb["MusicVol"], Is.EqualTo(0f));

            SoundSettings.Music = false;
            Assert.That(director.AppliedDb["MusicVol"], Is.EqualTo(-80f), "category toggle lands on its bus");
            Assert.That(director.AppliedDb["MasterVol"], Is.EqualTo(0f));

            SoundSettings.Music = true;
            SoundSettings.Master = false;
            Assert.That(director.AppliedDb["MasterVol"], Is.EqualTo(-80f),
                "master bus gates everything downstream in the mixer graph");
            Assert.That(director.AppliedDb["MusicVol"], Is.EqualTo(0f),
                "category buses untouched — the graph does the override");
        }
    }
}
