using FrogAcross.Editor.Solver;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using NUnit.Framework;

namespace FrogAcross.Tests.EditMode.Solver
{
    public class SolverTests
    {
        private const string Simple = @"{
          ""id"": ""solve-1"", ""columns"": 7,
          ""medal"": {""gold"": 5, ""silver"": 10, ""bronze"": 20}, ""startColumn"": 3, ""bays"": [3],
          ""rows"": [
            {""kind"": ""goal""},
            {""kind"": ""grass""},
            {""kind"": ""road"", ""dir"": ""right"", ""speed"": 1.5, ""objects"": [
              {""pieceId"": ""car"", ""offset"": 0, ""spacing"": 5.0, ""phase"": 0}]},
            {""kind"": ""bank""}
          ]}";

        private const string Walled = @"{
          ""id"": ""solve-walled"", ""columns"": 5,
          ""medal"": {""gold"": 5, ""silver"": 10, ""bronze"": 20}, ""startColumn"": 2, ""bays"": [2],
          ""rows"": [
            {""kind"": ""goal""},
            {""kind"": ""grass"", ""obstructions"": [
              {""pieceId"": ""tree"", ""column"": 0}, {""pieceId"": ""tree"", ""column"": 1},
              {""pieceId"": ""tree"", ""column"": 2}, {""pieceId"": ""tree"", ""column"": 3},
              {""pieceId"": ""tree"", ""column"": 4}]},
            {""kind"": ""bank""}
          ]}";

        private static LevelDefinition Load(string json) => LevelLoader.Parse(json, PieceRegistry.Load());

        [Test]
        public void SolvesASimpleLevel_AndTheScriptReplays()
        {
            var level = Load(Simple);
            var result = LevelSolver.Solve(level, allowDiagonals: true);
            Assert.IsTrue(result.Solved, result.FailReason);
            Assert.Greater(result.Script.Count, 0);
            Assert.IsTrue(LevelSolver.Replay(level, result.Script, out long clock), "script must replay to completion");
            Assert.AreEqual(result.MinTicks, clock, "replayed clock matches the solver's");
        }

        [Test]
        public void DiagonalFreeMode_SolvesToo_NeverFaster()
        {
            var level = Load(Simple);
            var full = LevelSolver.Solve(level, allowDiagonals: true);
            var noDiag = LevelSolver.Solve(level, allowDiagonals: false);
            Assert.IsTrue(noDiag.Solved, noDiag.FailReason);
            Assert.GreaterOrEqual(noDiag.MinTicks, full.MinTicks, "diagonal-free can never beat full actions");
        }

        [Test]
        public void WalledLevel_ReportsUnsolvable()
        {
            var result = LevelSolver.Solve(Load(Walled), allowDiagonals: true, nodeBudget: 30_000, tickBudget: 1800);
            Assert.IsFalse(result.Solved);
            StringAssert.Contains("unsolvable", result.FailReason);
        }

        [Test]
        public void DevBoard_IsSolvable()
        {
            var level = LevelLoader.LoadFromResources("dev-001", PieceRegistry.Load());
            var result = LevelSolver.Solve(level, allowDiagonals: false);
            Assert.IsTrue(result.Solved, result.FailReason);
            Assert.IsTrue(LevelSolver.Replay(level, result.Script, out _));
        }
    }
}
