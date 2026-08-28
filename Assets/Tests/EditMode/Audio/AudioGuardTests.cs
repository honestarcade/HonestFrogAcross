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
            foreach (var slot in new[] { "music-menu", "music-gameplay" })
                Assert.That(Directory.GetFiles(AudioFolder).Select(Path.GetFileNameWithoutExtension)
                    .Any(n => n == slot || n == $"placeholder-{slot}"), Is.True, $"no clip for music slot '{slot}'");
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
