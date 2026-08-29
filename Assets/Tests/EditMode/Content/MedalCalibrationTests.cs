using System.IO;
using System.Linq;
using FrogAcross.Editor.Generator;
using FrogAcross.Levels;
using NUnit.Framework;
using UnityEngine;

namespace FrogAcross.Tests.EditMode.Content
{
    /// <summary>
    /// #63: gold is earnable on every level — the bot proves the floor, the
    /// factors put thresholds a human distance above it.
    /// </summary>
    [TestFixture]
    public class MedalCalibrationTests
    {
        [Test]
        public void EveryLevel_MinTimeLeqGold_LtSilver_LtBronze()
        {
            var fixture = ContentLock.LoadFixture();
            foreach (var file in ContentLock.ShippedLevelFiles())
            {
                string id = Path.GetFileNameWithoutExtension(file);
                var dto = JsonUtility.FromJson<LevelDto>(File.ReadAllText(file));
                float minSec = fixture.entries.First(e => e.id == id).minTicks / 60f;
                Assert.That(dto.medal.gold, Is.GreaterThanOrEqualTo(minSec),
                    $"{id}: gold below the proven floor is unearnable");
                Assert.That(dto.medal.gold, Is.LessThan(dto.medal.silver), $"{id}: gold < silver");
                Assert.That(dto.medal.silver, Is.LessThan(dto.medal.bronze), $"{id}: silver < bronze");
                // Outer sanity bound derived from the schedule itself (a magic
                // constant here silently went stale the moment a factor moved).
                int number = int.Parse(id.Substring(ContentLock.ShippedPrefix.Length));
                float maxMultiple = MedalCalibrator.GoldFactorFor(number) * MedalCalibrator.BronzeOverGold * 1.05f;
                Assert.That(dto.medal.bronze, Is.LessThanOrEqualTo(minSec * maxMultiple),
                    $"{id}: bronze drifted beyond the calibrated schedule");
            }
        }

        [Test]
        public void MedalsMatchTheFactorSchedule_NotHandTypedValues()
        {
            var fixture = ContentLock.LoadFixture();
            foreach (var file in ContentLock.ShippedLevelFiles())
            {
                string id = Path.GetFileNameWithoutExtension(file);
                int n = int.Parse(id.Substring(ContentLock.ShippedPrefix.Length));
                var dto = JsonUtility.FromJson<LevelDto>(File.ReadAllText(file));
                float minSec = fixture.entries.First(e => e.id == id).minTicks / 60f;
                float g = MedalCalibrator.GoldFactorFor(n);
                Assert.That(dto.medal.gold, Is.EqualTo(minSec * g).Within(0.06f),
                    $"{id}: gold is not floor × {g} — hand-typed medal times are drift");
            }
        }

        [Test]
        public void EarlyLevels_AnchorNearTheDesignChips()
        {
            // The design's example chips anchor early gold at 24.0s; L2–5 must
            // land within the 2× ballpark. L1 is exempt by design: #64 pins it
            // as a single-bay near-straight line, so its floor is tiny — its
            // gold still gets the 4.0× beginner factor.
            for (int n = 2; n <= 5; n++)
            {
                var dto = JsonUtility.FromJson<LevelDto>(
                    File.ReadAllText($"{ContentLock.LevelsFolder}/level-{n:D3}.json"));
                Assert.That(dto.medal.gold, Is.InRange(12f, 48f),
                    $"level {n} gold {dto.medal.gold:0.0}s outside the 24s-anchor ballpark");
            }
        }
    }
}
