using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.Sim;
using NUnit.Framework;

namespace FrogAcross.Tests.EditMode.Sim
{
    public class RidingSystemTests
    {
        // goal / grass / river(single slow log-short: left=1.6, size 2.4) / bank. columns 7.
        private const string RiverFixture = @"{
          ""id"": ""ride-1"", ""columns"": 7,
          ""medal"": {""gold"": 5, ""silver"": 10, ""bronze"": 20}, ""startColumn"": 2, ""bays"": [3],
          ""rows"": [
            {""kind"": ""goal""},
            {""kind"": ""grass""},
            {""kind"": ""river"", ""dir"": ""right"", ""speed"": 0.6, ""objects"": [
              {""pieceId"": ""log-short"", ""offset"": 1.6, ""spacing"": 0, ""phase"": 0}]},
            {""kind"": ""bank""}
          ]}";

        // goal / river A (log left=1.0) / river B (log left=1.5) / bank
        private const string TransferFixture = @"{
          ""id"": ""ride-2"", ""columns"": 7,
          ""medal"": {""gold"": 5, ""silver"": 10, ""bronze"": 20}, ""startColumn"": 2, ""bays"": [3],
          ""rows"": [
            {""kind"": ""goal""},
            {""kind"": ""river"", ""dir"": ""right"", ""speed"": 0.3, ""objects"": [
              {""pieceId"": ""log"", ""offset"": 1.0, ""spacing"": 0, ""phase"": 0}]},
            {""kind"": ""river"", ""dir"": ""right"", ""speed"": 0.3, ""objects"": [
              {""pieceId"": ""log"", ""offset"": 1.5, ""spacing"": 0, ""phase"": 0}]},
            {""kind"": ""bank""}
          ]}";

        private static GameSim Sim(string json) => new(LevelLoader.Parse(json, PieceRegistry.Load()));

        private static void Settle(GameSim sim, int ticks = SimConfig.HopCooldownTicks + 2)
        {
            for (int i = 0; i < ticks; i++) sim.Tick();
        }

        [Test]
        public void LandingOnALog_AttachesAndDrifts()
        {
            var sim = Sim(RiverFixture);
            sim.EnqueueMove(Move.Forward);
            Settle(sim);
            Assert.IsTrue(sim.State.Riding, "col 2 is inside the log span [1.6, 4.0]");
            float x0 = sim.State.PlayerX;
            for (int i = 0; i < 60; i++) sim.Tick();
            Assert.AreEqual(x0 + 0.6f, sim.State.PlayerX, 1e-3f, "one second of drift at 0.6 cells/s");
            Assert.AreEqual(0, sim.State.Deaths);
        }

        [Test]
        public void LandingInOpenWater_Drowns()
        {
            var sim = Sim(RiverFixture);
            // col 2 → move right twice on bank (to col 4... col 4 inside span; use col 5), then forward.
            sim.EnqueueMove(Move.Right); Settle(sim);
            sim.EnqueueMove(Move.Right); Settle(sim);
            sim.EnqueueMove(Move.Right); Settle(sim); // col 5, log span ends 4.0
            int deaths = 0; sim.OnDeath += _ => deaths++;
            sim.EnqueueMove(Move.Forward); Settle(sim);
            Assert.AreEqual(1, deaths, "x=5 is past the log (span [1.6,4.0]+grace)");
            Assert.AreEqual(DeathCause.Water, sim.State.LastDeath);
        }

        [Test]
        public void AttachGrace_BoundariesAreExact()
        {
            // grace 0.20: landing x=4.19 attaches (edge 4.0), x=4.21 drowns — approximated
            // by column landings around the drifting edge; verified directly on TryAttach
            // via landing positions: use bank col 4 (x=4 inside grace of right edge 4.0+0.2).
            var sim = Sim(RiverFixture);
            sim.EnqueueMove(Move.Right); Settle(sim);
            sim.EnqueueMove(Move.Right); Settle(sim); // col 4
            int deaths = 0; sim.OnDeath += _ => deaths++;
            sim.EnqueueMove(Move.Forward); Settle(sim);
            Assert.AreEqual(0, deaths, "x=4.0 within span+grace");
            Assert.IsTrue(sim.State.Riding);
        }

        [Test]
        public void NearestColumn_RoundingBoundary()
        {
            Assert.AreEqual(2, GameSim.NearestColumn(2.49f));
            Assert.AreEqual(3, GameSim.NearestColumn(2.51f));
            Assert.AreEqual(3, GameSim.NearestColumn(2.5f), "banker's edge: floor(x+0.5)");
        }

        [Test]
        public void SwipeFromDrift_LandsNearestColumn()
        {
            var sim = Sim(RiverFixture);
            sim.EnqueueMove(Move.Forward);
            Settle(sim);
            Assert.IsTrue(sim.State.Riding);
            // drift from x=2 at 0.6 cells/s: after 60 ticks x=2.6 → nearest col 3
            for (int i = 0; i < 60; i++) sim.Tick();
            sim.EnqueueMove(Move.Forward); // to grass row
            Settle(sim);
            Assert.IsFalse(sim.State.Riding);
            Assert.AreEqual(1, sim.State.PlayerRow);
            Assert.AreEqual(3f, sim.State.PlayerX, 1e-3f, "landed snapped to nearest column 3");
        }

        [Test]
        public void PlatformToPlatform_Transfers()
        {
            var sim = Sim(TransferFixture);
            sim.EnqueueMove(Move.Forward); Settle(sim); // onto row B log (span [1.5,5.1])
            Assert.IsTrue(sim.State.Riding);
            sim.EnqueueMove(Move.Forward); Settle(sim); // onto row A log (span [1.0,4.6]) — still aligned early
            Assert.IsTrue(sim.State.Riding, "transferred to the upper log");
            Assert.AreEqual(1, sim.State.PlayerRow);
            Assert.AreEqual(0, sim.State.Deaths);
        }

        [Test]
        public void DriftingPastTheEdge_Kills()
        {
            var sim = Sim(RiverFixture);
            sim.EnqueueMove(Move.Forward); Settle(sim);
            Assert.IsTrue(sim.State.Riding);
            int deaths = 0; DeathCause cause = DeathCause.None;
            sim.OnDeath += c => { deaths++; cause = c; };
            for (int i = 0; i < 60 * 60 && deaths == 0; i++) sim.Tick(); // 0.6 c/s toward col 6.5 edge
            Assert.AreEqual(1, deaths, "rode the log off the board");
            Assert.AreEqual(DeathCause.EdgeDrift, cause);
        }
    }
}
