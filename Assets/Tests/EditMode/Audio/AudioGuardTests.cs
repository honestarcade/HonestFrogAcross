using System;
using System.IO;
using System.Linq;
using FrogAcross.Audio;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using NUnit.Framework;
using UnityEngine;

namespace FrogAcross.Tests.EditMode.Audio
{
    /// <summary>#65: gameplay logic stays audio-free, every hook has a clip,
    /// temporary assets are findable, and the trigger rules are pure.</summary>
    [TestFixture]
    public class AudioGuardTests
    {
        private const string AudioFolder = "Assets/Resources/Audio";

        [Test]
        public void SimSources_ContainNoAudioReferences()
        {
            foreach (var file in Directory.GetFiles("Assets/Scripts/Runtime/Sim", "*.cs"))
            {
                string source = File.ReadAllText(file);
                Assert.That(source, Does.Not.Contain("Audio"),
                    $"{Path.GetFileName(file)}: the sim raises events; audio listens from outside");
            }
        }

        [Test]
        public void EveryHook_ResolvesToAClipFile()
        {
            foreach (GameSound sound in Enum.GetValues(typeof(GameSound)))
            {
                string key = AudioDirector.KeyFor(sound);
                bool found = Directory.GetFiles(AudioFolder)
                    .Select(Path.GetFileNameWithoutExtension)
                    .Any(n => n == key || n == $"placeholder-{key}");
                Assert.That(found, Is.True, $"no clip for hook '{key}'");
            }
        }

        [Test]
        public void NoPlaceholderMusic_Ships()
        {
            // A generated blip stands in for a one-shot. A generated pad stands
            // in for nothing: on loop it is a continuous hum for as long as the
            // app is open, and that is what shipped in 0.8.0 (owner: "a weird
            // sound like a hum right when the app loads and it doesn't shut
            // off"). The music slots stay silent until #66 lands real tracks.
            foreach (var slot in new[] { "music-menu", "music-gameplay" })
            {
                Assert.That(File.Exists(Path.Combine(AudioFolder, $"placeholder-{slot}.wav")), Is.False,
                    $"placeholder-{slot} is a looping tone, not a placeholder");
                foreach (var file in Directory.GetFiles(AudioFolder, $"placeholder-{slot}.*"))
                    Assert.Fail($"{Path.GetFileName(file)}: no placeholder music may ship");
            }
            Assert.That(File.ReadAllText("Assets/Scripts/Editor/Audio/PlaceholderSfx.cs"),
                Does.Not.Contain("placeholder-music"),
                "the generator must not put them back either");
        }

        [Test]
        public void TemporaryClips_AreClearlyTagged()
        {
            // every audio file is either a tagged placeholder or (post-#66)
            // covered by the LICENSES.md manifest
            string manifest = File.Exists("Assets/Audio/LICENSES.md")
                ? File.ReadAllText("Assets/Audio/LICENSES.md") : "";
            foreach (var file in Directory.GetFiles(AudioFolder)
                         .Where(f => f.EndsWith(".wav") || f.EndsWith(".ogg") || f.EndsWith(".mp3")))
            {
                string name = Path.GetFileName(file);
                Assert.That(name.StartsWith("placeholder-") || manifest.Contains(name), Is.True,
                    $"{name}: neither placeholder-tagged nor licensed in LICENSES.md");
            }
        }

        [Test]
        public void TargetDb_MapsToggleToBusVolume()
        {
            Assert.That(AudioDirector.TargetDb(true), Is.EqualTo(0f));
            Assert.That(AudioDirector.TargetDb(false), Is.EqualTo(-80f));
        }

        [Test]
        public void TurtleWarning_TriggersOnlyWhileRidingASubmergingRow()
        {
            var registry = PieceRegistry.Load();
            var level = LevelLoader.Parse(@"{
                ""id"": ""turtle-warn-fixture"", ""name"": ""t"", ""columns"": 9, ""startColumn"": 4,
                ""bays"": [4], ""medal"": { ""gold"": 10, ""silver"": 20, ""bronze"": 30 },
                ""rows"": [
                    { ""kind"": ""goal"" },
                    { ""kind"": ""river"", ""dir"": ""right"", ""speed"": 1.0,
                      ""objects"": [ { ""pieceId"": ""turtle-log"", ""offset"": 0, ""spacing"": 6, ""phase"": 0 } ] },
                    { ""kind"": ""bank"" }
                ]}", registry);
            var turtle = registry.Get<LaneObjectDef>("turtle-log");
            const int row = 1;

            Assert.That(AudioDirector.TurtleWarnTicksLeft(level, row, 0, riding: false),
                Is.EqualTo(0), "no warning while not riding");
            Assert.That(AudioDirector.TurtleWarnTicksLeft(level, row, 0, riding: true),
                Is.EqualTo(turtle.cycleActiveTicks), "full active span at cycle start");
            Assert.That(AudioDirector.TurtleWarnTicksLeft(level, row, turtle.cycleActiveTicks - 30, riding: true),
                Is.EqualTo(30), "30 ticks before submerge — inside the warning window");
            Assert.That(AudioDirector.TurtleWarnTicksLeft(level, row, turtle.cycleActiveTicks + 5, riding: true),
                Is.EqualTo(0), "already submerged: too late to warn");
        }
    }
}
