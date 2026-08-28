using System.IO;
using FrogAcross.Input;
using FrogAcross.Services;
using NUnit.Framework;
using UnityEngine;

namespace FrogAcross.Tests.EditMode.Services
{
    /// <summary>#59: toggles persist, master overrides categories, wipe is complete.</summary>
    [TestFixture]
    public class SettingsPersistenceTests
    {
        private byte[] _saveBackup;
        private int _master, _music, _effects, _ui, _scheme;
        private string _character;

        [SetUp]
        public void SetUp()
        {
            _saveBackup = File.Exists(Progression.SavePath) ? File.ReadAllBytes(Progression.SavePath) : null;
            _master = PlayerPrefs.GetInt("sound.master", 1);
            _music = PlayerPrefs.GetInt("sound.music", 1);
            _effects = PlayerPrefs.GetInt("sound.effects", 1);
            _ui = PlayerPrefs.GetInt("sound.ui", 1);
            _scheme = PlayerPrefs.GetInt(ControlSchemeSetting.PrefKey, 0);
            _character = PlayerPrefs.GetString(CharacterSelection.PrefKey, "");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(Progression.SavePath)) File.Delete(Progression.SavePath);
            if (_saveBackup != null) File.WriteAllBytes(Progression.SavePath, _saveBackup);
            Progression.ReloadFromDisk();
            PlayerPrefs.SetInt("sound.master", _master);
            PlayerPrefs.SetInt("sound.music", _music);
            PlayerPrefs.SetInt("sound.effects", _effects);
            PlayerPrefs.SetInt("sound.ui", _ui);
            PlayerPrefs.SetInt(ControlSchemeSetting.PrefKey, _scheme);
            PlayerPrefs.SetString(CharacterSelection.PrefKey, _character);
            PlayerPrefs.Save();
        }

        [Test]
        public void SoundToggles_PersistThroughPlayerPrefs()
        {
            SoundSettings.Music = false;
            Assert.That(PlayerPrefs.GetInt("sound.music", 1), Is.EqualTo(0));
            SoundSettings.Music = true;
            Assert.That(SoundSettings.Music, Is.True);
        }

        [Test]
        public void MasterOff_OverridesEveryCategory()
        {
            SoundSettings.Music = true;
            SoundSettings.Effects = true;
            SoundSettings.Ui = true;
            SoundSettings.Master = false;
            Assert.That(SoundSettings.EffectiveMusic, Is.False);
            Assert.That(SoundSettings.EffectiveEffects, Is.False);
            Assert.That(SoundSettings.EffectiveUi, Is.False);
            Assert.That(SoundSettings.Music, Is.True, "category flags themselves are untouched");
        }

        [Test]
        public void CategoryOff_SilencesOnlyItself()
        {
            SoundSettings.Master = true;
            SoundSettings.Effects = false;
            SoundSettings.Music = true;
            Assert.That(SoundSettings.EffectiveEffects, Is.False);
            Assert.That(SoundSettings.EffectiveMusic, Is.True);
        }

        [Test]
        public void CharacterSelection_PersistsAndResets()
        {
            CharacterSelection.SelectedId = "cat";
            Assert.That(CharacterSelection.SelectedId, Is.EqualTo("cat"));
            CharacterSelection.Reset();
            Assert.That(CharacterSelection.SelectedId, Is.Empty);
        }

        [Test]
        public void ControlScheme_Persists()
        {
            ControlSchemeSetting.Current = ControlScheme.TapRegions;
            Assert.That(ControlSchemeSetting.Current, Is.EqualTo(ControlScheme.TapRegions));
            ControlSchemeSetting.Current = ControlScheme.Swipe;
            Assert.That(ControlSchemeSetting.Current, Is.EqualTo(ControlScheme.Swipe));
        }

        [Test]
        public void DataWipe_ReturnsEveryStoreToFactoryState()
        {
            // seed every store the game persists
            Progression.ReportCompletion("level-007", 7, 12f, 15f, 20f, 30f);
            SoundSettings.Master = false;
            SoundSettings.Music = false;
            CharacterSelection.SelectedId = "cat";
            ControlSchemeSetting.Current = ControlScheme.TapRegions;

            DataWipe.WipeAll();
            Progression.ReloadFromDisk();

            Assert.That(Progression.HighestUnlocked, Is.EqualTo(1));
            Assert.That(Progression.Data.records, Is.Empty);
            Assert.That(SoundSettings.Master, Is.True);
            Assert.That(SoundSettings.Music, Is.True);
            Assert.That(CharacterSelection.SelectedId, Is.Empty);
            Assert.That(ControlSchemeSetting.Current, Is.EqualTo(ControlScheme.Swipe));
        }
    }
}
