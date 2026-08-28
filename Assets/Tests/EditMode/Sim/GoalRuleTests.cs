using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.Sim;
using FrogAcross.UI;
using NUnit.Framework;

namespace FrogAcross.Tests.EditMode.Sim
{
    /// <summary>#53's bay lifecycle + #54's medal thresholds.</summary>
    public class GoalRuleTests
    {
        private const string TwoBay = @"{
          ""id"": ""goal-1"", ""columns"": 5,
          ""medal"": {""gold"": 5, ""silver"": 10, ""bronze"": 20}, ""startColumn"": 2, ""bays"": [1, 3],
          ""rows"": [
            {""kind"": ""goal""},
            {""kind"": ""grass""},
            {""kind"": ""bank""}
          ]}";

        private static GameSim Sim() => new(LevelLoader.Parse(TwoBay, PieceRegistry.Load()));

        private static void Do(GameSim sim, Move m, int ticks = SimConfig.HopCooldownTicks + 2)
        {
            sim.EnqueueMove(m);
            for (int i = 0; i < ticks; i++) sim.Tick();
        }

        [Test]
        public void BayLifecycle_FillRefuseComplete()
        {
            var sim = Sim();
            int filled = 0; bool completed = false;
            sim.OnBayFilled += _ => filled++;
            sim.OnCompleted += () => completed = true;

            Do(sim, Move.Forward);          // bank → grass (col 2)
            Do(sim, Move.Left);             // col 1
            Do(sim, Move.Forward);          // into bay 1 → fills, respawn at bank
            Assert.AreEqual(1, filled);
            Assert.IsFalse(completed);
            Assert.AreEqual(sim.Level.BankRow, sim.State.PlayerRow, "next attempt starts at bank");

            Do(sim, Move.Forward);          // grass col 2
            Do(sim, Move.Left);             // col 1
            Do(sim, Move.Forward);          // filled bay refuses: no move
            Assert.AreEqual(1, filled, "a filled bay cannot be re-entered");
            Assert.AreEqual(1, sim.State.PlayerRow, "still on the grass row");

            Do(sim, Move.Right);            // col 2
            Do(sim, Move.Right);            // col 3
            Do(sim, Move.Forward);          // bay 3 → completes
            Assert.AreEqual(2, filled);
            Assert.IsTrue(completed, "last bay completes the level");
            Assert.IsTrue(sim.State.Completed);
        }

        [Test]
        public void OffBayGoalTiles_Refuse()
        {
            var sim = Sim();
            Do(sim, Move.Forward);          // grass col 2 (not a bay column)
            Do(sim, Move.Forward);          // goal row col 2: refused
            Assert.AreEqual(1, sim.State.PlayerRow, "off-bay goal tile is obstructed");
        }

        [Test]
        public void MedalThresholds_BoundaryExact()
        {
            var level = LevelLoader.Parse(TwoBay, PieceRegistry.Load());
            Assert.AreEqual("GOLD", LevelCompleteOverlay.MedalFor(5.0f, level).name);
            Assert.AreEqual("SILVER", LevelCompleteOverlay.MedalFor(5.01f, level).name);
            Assert.AreEqual("SILVER", LevelCompleteOverlay.MedalFor(10.0f, level).name);
            Assert.AreEqual("BRONZE", LevelCompleteOverlay.MedalFor(10.01f, level).name);
            Assert.AreEqual("BRONZE", LevelCompleteOverlay.MedalFor(20.0f, level).name);
            Assert.AreEqual("COMPLETE", LevelCompleteOverlay.MedalFor(20.01f, level).name);
        }
    }
}
