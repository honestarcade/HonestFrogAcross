using FrogAcross.Input;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.Sim;
using NUnit.Framework;
using UnityEngine;

namespace FrogAcross.Tests.EditMode.Input
{
    public class SwipeClassifierTests
    {
        private static Vector2 AtAngle(float deg, float cm = 2f) =>
            new(cm * Mathf.Cos(deg * Mathf.Deg2Rad), cm * Mathf.Sin(deg * Mathf.Deg2Rad));

        [TestCase(42.9f, Move.Right)]          // just outside the band → nearest cardinal (0° is 42.9 away vs 90° at 47.1)
        [TestCase(43.0f, Move.DiagForwardRight)]
        [TestCase(45.0f, Move.DiagForwardRight)]
        [TestCase(47.0f, Move.DiagForwardRight)]
        [TestCase(47.1f, Move.Forward)]
        [TestCase(133.5f, Move.DiagForwardLeft)]
        [TestCase(137.2f, Move.Left)]
        [TestCase(225.0f, Move.DiagBackLeft)]
        [TestCase(313.2f, Move.DiagBackRight)] // 313.2 ≡ 45−1.8 in-quadrant → inside the 43–47 band
        [TestCase(312.9f, Move.Back)]          // 42.9 in-quadrant → outside the band → nearest cardinal
        [TestCase(315.0f, Move.DiagBackRight)]
        [TestCase(0f, Move.Right)]
        [TestCase(90f, Move.Forward)]
        [TestCase(180f, Move.Left)]
        [TestCase(270f, Move.Back)]
        [TestCase(30f, Move.Right)]
        [TestCase(60f, Move.Forward)]
        public void AngleTable(float deg, Move expected)
        {
            Assert.AreEqual(expected, SwipeClassifier.Classify(AtAngle(deg), 0.15f));
        }

        [Test]
        public void ClearTap_IsForward()
        {
            Assert.AreEqual(Move.Forward, SwipeClassifier.Classify(new Vector2(0.1f, -0.05f), 0.1f));
        }

        [Test]
        public void SlowPress_IsNothing()
        {
            Assert.IsNull(SwipeClassifier.Classify(new Vector2(0.1f, 0f), 0.6f), "long-press at tap distance");
        }

        [Test]
        public void DeadZone_IsNothing()
        {
            Assert.IsNull(SwipeClassifier.Classify(AtAngle(90f, 0.6f), 0.1f), "0.4cm < 0.6cm < 0.8cm");
        }

        [Test]
        public void QueueCap_RefusesThirdBufferedMove()
        {
            var sim = new GameSim(LevelLoader.LoadFromResources("dev-001", PieceRegistry.Load()));
            Assert.IsTrue(sim.EnqueueMove(Move.Forward));
            Assert.IsTrue(sim.EnqueueMove(Move.Forward));
            Assert.IsFalse(sim.EnqueueMove(Move.Forward), "cap 2: third buffered move dropped");
        }

        private const string DiagFixture = @"{
          ""id"": ""diag-fixture"", ""columns"": 5,
          ""medal"": {""gold"": 5, ""silver"": 10, ""bronze"": 20}, ""startColumn"": 2, ""bays"": [2],
          ""rows"": [
            {""kind"": ""goal""},
            {""kind"": ""grass"", ""obstructions"": [
              {""pieceId"": ""tree"", ""column"": 2}, {""pieceId"": ""bush"", ""column"": 3}]},
            {""kind"": ""bank""}
          ]}";

        [Test]
        public void Diagonal_OnlyLandingSquareMatters()
        {
            // From bank col 2: Forward is blocked (tree at r1c2); DiagForwardRight
            // lands r1c3 — also blocked (bush). DiagForwardLeft lands r1c1 — free,
            // even though the orthogonal intermediates (r1c2 tree / bank c1 free) include a block.
            var sim = new GameSim(LevelLoader.Parse(DiagFixture, PieceRegistry.Load()));
            sim.EnqueueMove(Move.Forward);
            for (int i = 0; i < 20; i++) sim.Tick();
            Assert.AreEqual(sim.Level.BankRow, sim.State.PlayerRow, "tree blocks forward landing");

            sim.EnqueueMove(Move.DiagForwardLeft);
            for (int i = 0; i < 20; i++) sim.Tick();
            Assert.AreEqual(1, sim.State.PlayerRow, "diagonal over the blocked column is allowed");
            Assert.AreEqual(1, GameSim.NearestColumn(sim.State.PlayerX));
        }
    }
}
