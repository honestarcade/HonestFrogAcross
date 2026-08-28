using System.Linq;
using FrogAcross.Editor.Generator;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using NUnit.Framework;
using UnityEngine;

namespace FrogAcross.Tests.EditMode.Generator
{
    public class GeneratorTests
    {
        private static GeneratorParams Params(int seed, int count)
        {
            var p = ScriptableObject.CreateInstance<GeneratorParams>();
            p.baseSeed = seed;
            p.count = count;
            return p;
        }

        [Test]
        public void SameSeed_ByteIdenticalOutput()
        {
            var reg = PieceRegistry.Load();
            var a = LevelGenerator.GenerateCandidates(Params(4242, 3), reg);
            var b = LevelGenerator.GenerateCandidates(Params(4242, 3), reg);
            Assert.AreEqual(a.Accepted.Count, b.Accepted.Count);
            for (int i = 0; i < a.Accepted.Count; i++)
                Assert.AreEqual(a.Accepted[i].json, b.Accepted[i].json, $"candidate {i} differs across runs");
        }

        [Test]
        public void AcceptedCandidates_AreValidAndSolvable_ByConstruction()
        {
            var reg = PieceRegistry.Load();
            var report = LevelGenerator.GenerateCandidates(Params(777, 5), reg);
            Assert.Greater(report.Accepted.Count, 0, "at least some candidates must survive: "
                + string.Join("; ", report.Rejected.Select(r => r.reason)));
            foreach (var (id, json, minTicks) in report.Accepted)
            {
                var dto = JsonUtility.FromJson<LevelDto>(json);
                Assert.IsEmpty(LevelValidator.Validate(dto, reg), id);
                Assert.Greater(minTicks, 0, id);
                Assert.Less(dto.medal.gold, dto.medal.silver, id);
                Assert.Less(dto.medal.silver, dto.medal.bronze, id);
                Assert.GreaterOrEqual(dto.medal.gold, minTicks / 60f, $"{id}: gold must be ≥ the proven floor");
            }
        }

        [Test]
        public void Rejections_CarryReasons()
        {
            var reg = PieceRegistry.Load();
            var p = Params(9001, 4);
            p.solverNodeBudget = 5;   // strangle the solver → rejections
            var report = LevelGenerator.GenerateCandidates(p, reg);
            Assert.AreEqual(0, report.Accepted.Count);
            Assert.AreEqual(4, report.Rejected.Count);
            Assert.IsTrue(report.Rejected.All(r => r.reason.StartsWith("solver: ") || r.reason.StartsWith("validation: ")));
        }
    }
}
