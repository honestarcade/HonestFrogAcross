using System;
using System.Collections.Generic;
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
        public void NoPlaceholderAssets_Remain()
        {
            // #66's definition of done: every hook resolves to a real, licensed
            // clip. The generated blips were scaffolding for #65 and are gone
            // now that the owner's audio is in (2026-09-16).
            var left = Directory.GetFiles(AudioFolder)
                .Select(Path.GetFileName)
                .Where(n => n.StartsWith("placeholder-"))
                .ToList();
            Assert.That(left, Is.Empty,
                $"placeholder assets still ship: {string.Join(", ", left)}");
        }

        /// <summary>Peak sample of a 16-bit PCM wav, as dBFS.</summary>
        private static float PeakDbfs(string path)
        {
            var bytes = File.ReadAllBytes(path);
            int data = -1;
            for (int i = 12; i < bytes.Length - 8; i += 2)
                if (bytes[i] == 'd' && bytes[i + 1] == 'a' && bytes[i + 2] == 't' && bytes[i + 3] == 'a')
                {
                    data = i + 8;
                    break;
                }
            Assert.That(data, Is.GreaterThan(0), $"{path}: no data chunk");
            int peak = 0;
            for (int i = data; i + 1 < bytes.Length; i += 2)
                peak = Math.Max(peak, Math.Abs((short)(bytes[i] | (bytes[i + 1] << 8))));
            return 20f * Mathf.Log10(Math.Max(peak, 1) / 32767f);
        }

        [Test]
        public void ShippedClips_SitAtTheirMixLevels()
        {
            // Generation normalises every clip to -1 dBFS; installing applies
            // the relative mix (ArtSource/pipeline/sfx.py, MIX_DB). Without it
            // a menu tap is as loud as the win fanfare.
            var peaks = new Dictionary<string, float>();
            foreach (var file in Directory.GetFiles(AudioFolder, "*.wav"))
            {
                float db = PeakDbfs(file);
                peaks[Path.GetFileNameWithoutExtension(file)] = db;
                Assert.That(db, Is.LessThanOrEqualTo(-1f),
                    $"{Path.GetFileName(file)} peaks at {db:0.0} dBFS — headroom is -1");
            }
            if (peaks.Count == 0) return; // nothing installed yet

            // the shape of the mix, not exact numbers: quiet chrome, loud rewards
            foreach (var chrome in new[] { "ui-tap", "ui-navigate", "hop" })
            foreach (var reward in new[] { "medal", "level-complete" })
                if (peaks.ContainsKey(chrome) && peaks.ContainsKey(reward))
                    Assert.That(peaks[chrome], Is.LessThan(peaks[reward] - 3f),
                        $"'{chrome}' ({peaks[chrome]:0.0} dBFS) must sit well under "
                        + $"'{reward}' ({peaks[reward]:0.0} dBFS)");
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
