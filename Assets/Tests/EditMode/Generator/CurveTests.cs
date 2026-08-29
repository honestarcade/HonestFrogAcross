using System.Collections.Generic;
using System.IO;
using System.Linq;
using FrogAcross.Editor.Generator;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FrogAcross.Tests.EditMode.Generator
{
    /// <summary>
    /// #61: the shipped 100-level set follows the curve — schema-valid, the
    /// teaching order holds, and pressure trends upward. File-based (no
    /// solving): CI stays cheap; solver proof lives in #62's fixture.
    /// </summary>
    [TestFixture]
    public class CurveTests
    {
        private static List<(int number, LevelDto dto)> _levels;

        private static List<(int number, LevelDto dto)> Levels()
        {
            if (_levels != null) return _levels;
            _levels = ContentLock.ShippedLevelFiles()
                .Select(f => (int.Parse(Path.GetFileNameWithoutExtension(f).Substring(6)),
                    JsonUtility.FromJson<LevelDto>(File.ReadAllText(f))))
                .OrderBy(t => t.Item1)
                .ToList();
            return _levels;
        }

        [Test]
        public void ShippedSet_Is100Levels_AllSchemaValid()
        {
            var levels = Levels();
            Assert.That(levels.Count, Is.EqualTo(100));
            Assert.That(levels.Select(l => l.number), Is.EqualTo(Enumerable.Range(1, 100)));
            var registry = PieceRegistry.Load();
            foreach (var (n, dto) in levels)
            {
                var errors = LevelValidator.Validate(dto, registry);
                Assert.That(errors, Is.Empty, $"level-{n:D3}: {string.Join("; ", errors)}");
            }
        }

        [Test]
        public void IntroductionOrder_OneNewLaneKindPerDecade()
        {
            var expected = new Dictionary<string, int>
            {
                ["road"] = 1, ["river"] = 11, ["swamp"] = 21,
                ["tracks"] = 31, ["bike"] = 41, ["walkway"] = 51,
            };
            foreach (var (kind, introLevel) in expected.Select(kv => (kv.Key, kv.Value)))
            {
                int first = Levels().First(l => l.dto.rows.Any(r => r.kind == kind)).number;
                Assert.That(first, Is.EqualTo(introLevel),
                    $"'{kind}' should first appear at level {introLevel}");
                // the introduction decade practices its mechanic on every level
                for (int n = introLevel; n < introLevel + 10; n++)
                    Assert.That(Levels()[n - 1].dto.rows.Any(r => r.kind == kind), Is.True,
                        $"level {n} is in the '{kind}' teaching decade but lacks the kind");
            }
        }

        [Test]
        public void CurveAsset_CoversLevels1To100Contiguously()
        {
            var curve = AssetDatabase.LoadAssetAtPath<DifficultyCurve>(CurveGenerator.CurveAssetPath);
            Assert.That(curve, Is.Not.Null, "curve.asset must be committed");
            Assert.That(curve.levelCount, Is.EqualTo(100));
            int next = 1;
            foreach (var band in curve.bands)
            {
                Assert.That(band.startLevel, Is.EqualTo(next), $"band '{band.label}' must start at {next}");
                Assert.That(band.endLevel, Is.GreaterThanOrEqualTo(band.startLevel));
                next = band.endLevel + 1;
            }
            Assert.That(next, Is.EqualTo(101), "bands must cover exactly 1..100");
        }

        private static float Proxy(LevelDto dto)
        {
            var middle = dto.rows.Skip(1).Take(dto.rows.Length - 2).ToList();
            var moving = middle.Where(r => r.speed > 0).ToList();
            float avgSpeed = moving.Count > 0 ? moving.Average(r => r.speed) : 0.5f;
            return middle.Count * avgSpeed * (1 + dto.bays.Length);
        }

        [Test]
        public void Pressure_TrendsUpward_WithBoundedLocalVariance()
        {
            // Difficulty proxy (rows×speed×(1+bays)) at decade granularity:
            // train/gator wait-cycles make raw min-times noisy per decade, so
            // the solver-time trend (via the fixture-derived medal floor) is
            // asserted at phase granularity (teaching / middle / late thirds).
            float[] proxyMeans = new float[10];
            for (int d = 0; d < 10; d++)
                proxyMeans[d] = Levels().Skip(d * 10).Take(10).Average(l => Proxy(l.dto));

            for (int d = 1; d < 10; d++)
                Assert.That(proxyMeans[d], Is.GreaterThanOrEqualTo(proxyMeans[d - 1] * 0.9f),
                    $"difficulty proxy dips too far at decade {d + 1} ({proxyMeans[d - 1]:0.0} → {proxyMeans[d]:0.0})");
            Assert.That(proxyMeans[9], Is.GreaterThan(proxyMeans[0] * 2f),
                "level 90s must be substantially denser than the teaching decade");

            // Difficulty in TIME is the solver floor — gold applies a factor
            // taper on top, which would swamp the comparison.
            var fixture = ContentLock.LoadFixture();
            float FloorMean(int from, int to)
            {
                float sum = 0f; int n = 0;
                foreach (var e in fixture.entries)
                {
                    int number = int.Parse(e.id.Substring(ContentLock.ShippedPrefix.Length));
                    if (number < from || number > to) continue;
                    sum += e.minTicks / 60f; n++;
                }
                return n == 0 ? 0f : sum / n;
            }
            float early = FloorMean(1, 30), middle = FloorMean(31, 60), late = FloorMean(61, 100);
            Assert.That(middle, Is.GreaterThan(early), "middle phase must outlast the teaching phase");
            Assert.That(late, Is.GreaterThan(middle), "late phase must outlast the middle phase");
            Assert.That(late, Is.GreaterThan(early * 1.3f), "level 60+ must take substantially longer than 1–30");
        }
    }
}
