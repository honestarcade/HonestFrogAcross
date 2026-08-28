using System.Collections.Generic;
using System.IO;
using System.Linq;
using FrogAcross.Editor.Generator;
using FrogAcross.Editor.Solver;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using NUnit.Framework;
using UnityEngine;

namespace FrogAcross.Tests.EditMode.Content
{
    /// <summary>
    /// #64: the first five minutes teach the whole game. L1 is a near-straight
    /// line; each of L1–10 introduces at most one new mechanic. (Feel is owner
    /// UAT at verify — these pin the structural guarantees.)
    /// </summary>
    [TestFixture]
    public class TeachingSequenceTests
    {
        private static LevelDto Load(int n) => JsonUtility.FromJson<LevelDto>(
            File.ReadAllText($"{ContentLock.LevelsFolder}/level-{n:D3}.json"));

        [Test]
        public void Level1_IsANearStraightLine()
        {
            var registry = PieceRegistry.Load();
            var level = LevelLoader.Parse(
                File.ReadAllText($"{ContentLock.LevelsFolder}/level-001.json"), registry);
            var solve = LevelSolver.Solve(level, allowDiagonals: false, 250_000, 10_800);
            Assert.That(solve.Solved, Is.True);
            int rows = Load(1).rows.Length; // includes goal + bank
            Assert.That(solve.Script.Count, Is.LessThanOrEqualTo(rows + 2),
                $"L1 must be walkable in a near-straight line ({solve.Script.Count} moves for {rows} rows)");
        }

        [Test]
        public void Level1_HasASingleBay()
        {
            Assert.That(Load(1).bays.Length, Is.EqualTo(1),
                "one bay = one crossing = the gentlest possible first level");
        }

        [Test]
        public void Levels1To10_IntroduceAtMostOneMechanicEach()
        {
            // mechanics = lane kinds with behavior + obstruction presence
            var seen = new HashSet<string>();
            for (int n = 1; n <= 10; n++)
            {
                var dto = Load(n);
                var mechanics = dto.rows
                    .Select(r => r.kind)
                    .Where(k => k != "goal" && k != "bank" && k != "grass" && k != "concrete")
                    .Distinct()
                    .ToList();
                if (dto.rows.Any(r => r.obstructions is { Length: > 0 }))
                    mechanics.Add("obstructions");
                var fresh = mechanics.Where(m => !seen.Contains(m)).ToList();
                Assert.That(fresh.Count, Is.LessThanOrEqualTo(1),
                    $"level {n} introduces {fresh.Count} new mechanics at once: {string.Join(", ", fresh)}");
                foreach (var m in fresh) seen.Add(m);
            }
        }
    }
}
