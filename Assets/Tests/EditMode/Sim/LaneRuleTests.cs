using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.Sim;
using NUnit.Framework;

namespace FrogAcross.Tests.EditMode.Sim
{
    /// <summary>Per-lane rule batteries for #48–#52, all on deterministic fixtures.</summary>
    public class LaneRuleTests
    {
        private static GameSim Sim(string json) => new(LevelLoader.Parse(json, PieceRegistry.Load()));

        private static void Settle(GameSim sim, int ticks = SimConfig.HopCooldownTicks + 2)
        {
            for (int i = 0; i < ticks; i++) sim.Tick();
        }

        // ---------- #48: turtles ----------
        // Turtle-log: default cycle 240 surfaced / 90 under; phase 0 → submerges at tick 240.
        private const string TurtleFixture = @"{
          ""id"": ""turtle-1"", ""columns"": 7,
          ""medal"": {""gold"": 5, ""silver"": 10, ""bronze"": 20}, ""startColumn"": 2, ""bays"": [2],
          ""rows"": [
            {""kind"": ""goal""},
            {""kind"": ""river"", ""dir"": ""right"", ""speed"": 0.05, ""objects"": [
              {""pieceId"": ""turtle-log"", ""offset"": 1.0, ""spacing"": 0, ""phase"": 0}]},
            {""kind"": ""bank""}
          ]}";

        [Test]
        public void Turtles_DrownTheRider_ExactlyAtSubmerge()
        {
            var sim = Sim(TurtleFixture);
            sim.EnqueueMove(Move.Forward);
            Settle(sim); // riding by ~tick 11
            Assert.IsTrue(sim.State.Riding, "attached to the surfaced turtle-log");
            int deaths = 0; DeathCause cause = DeathCause.None;
            sim.OnDeath += c => { deaths++; cause = c; };
            while (sim.State.Tick < 239) sim.Tick();
            Assert.AreEqual(0, deaths, "still surfaced at tick 239");
            sim.Tick(); // tick 240: submerged
            Assert.AreEqual(1, deaths, "drowns at the submerge tick");
            Assert.AreEqual(DeathCause.Water, cause);
        }

        // ---------- #49: gators (owner rules) ----------
        // Gator: zone [0.05,0.55] of 3.7 cells → back spans x=[left+0.185, left+2.035].
        // Cycle 300 closed / 120 open.
        private static string GatorFixture(int phase) => @"{
          ""id"": ""gator-1"", ""columns"": 9,
          ""medal"": {""gold"": 5, ""silver"": 10, ""bronze"": 20}, ""startColumn"": 2, ""bays"": [2],
          ""rows"": [
            {""kind"": ""goal""},
            {""kind"": ""swamp"", ""dir"": ""right"", ""speed"": 0.05, ""objects"": [
              {""pieceId"": ""gator"", ""offset"": 1.0, ""spacing"": 0, ""phase"": " + phase + @"}]},
            {""kind"": ""bank""}
          ]}";

        [Test]
        public void ClosedGatorBack_Rides()
        {
            var sim = Sim(GatorFixture(0));
            sim.EnqueueMove(Move.Forward); // land x=2: within back zone [1.185, 3.035]
            Settle(sim);
            Assert.IsTrue(sim.State.Riding, "closed-mouth back is the platform");
            Assert.AreEqual(0, sim.State.Deaths);
        }

        [Test]
        public void OpenGatorBack_Kills()
        {
            var sim = Sim(GatorFixture(300)); // phase 300 → open at tick 0..119
            int deaths = 0; DeathCause cause = DeathCause.None;
            sim.OnDeath += c => { deaths++; cause = c; };
            sim.EnqueueMove(Move.Forward);
            Settle(sim);
            Assert.AreEqual(1, deaths, "open-mouth back kills on landing (owner rule)");
            Assert.AreEqual(DeathCause.Gator, cause);
        }

        [Test]
        public void GatorSnout_NeverRideable_EvenClosed()
        {
            var sim = Sim(GatorFixture(0));
            // head/snout: zone fraction > 0.55 → x > left+2.035; land x=4 (frac ~0.81)
            sim.EnqueueMove(Move.Right); Settle(sim);
            sim.EnqueueMove(Move.Right); Settle(sim); // col 4
            int deaths = 0; DeathCause cause = DeathCause.None;
            sim.OnDeath += c => { deaths++; cause = c; };
            sim.EnqueueMove(Move.Forward); Settle(sim);
            Assert.AreEqual(1, deaths, "the head/snout is water, not platform");
            Assert.AreEqual(DeathCause.Water, cause);
        }

        [Test]
        public void RidingWhenMouthOpens_KillsAtTheOpenTick()
        {
            var sim = Sim(GatorFixture(0));
            sim.EnqueueMove(Move.Forward);
            Settle(sim);
            Assert.IsTrue(sim.State.Riding);
            int deaths = 0; DeathCause cause = DeathCause.None;
            sim.OnDeath += c => { deaths++; cause = c; };
            while (sim.State.Tick < 299) sim.Tick();
            Assert.AreEqual(0, deaths, "closed through tick 299");
            sim.Tick(); // tick 300: mouth opens
            Assert.AreEqual(1, deaths, "mouth opening mid-ride kills (ledgered interpretation)");
            Assert.AreEqual(DeathCause.Gator, cause);
        }

        // ---------- #50: trains ----------
        private const string TrainFixture = @"{
          ""id"": ""train-1"", ""columns"": 9,
          ""medal"": {""gold"": 5, ""silver"": 10, ""bronze"": 20}, ""startColumn"": 4, ""bays"": [4],
          ""rows"": [
            {""kind"": ""goal""},
            {""kind"": ""tracks"", ""dir"": ""right"", ""speed"": 6.0, ""objects"": [
              {""pieceId"": ""freight"", ""offset"": -12.0, ""spacing"": 0, ""phase"": 0}]},
            {""kind"": ""bank""}
          ]}";

        [Test]
        public void Warning_LeadsTheTrain_ThenTrainKills()
        {
            var sim = Sim(TrainFixture);
            // offset -12 = fully off-board left (margin 12); enters ~tick 10 at 6 c/s.
            // Warning fires when the train will be on-board within 90 ticks.
            bool sawWarningBeforeTrain = false;
            int deaths = 0;
            sim.OnDeath += _ => deaths++;
            sim.EnqueueMove(Move.Forward); // stand on the tracks
            for (int i = 0; i < 600 && deaths == 0; i++)
            {
                sim.Tick();
                if (sim.WarningActive(1) && deaths == 0) sawWarningBeforeTrain = true;
            }
            Assert.AreEqual(1, deaths, "the train kills");
            Assert.IsTrue(sawWarningBeforeTrain, "the crossing warned before the hit");
            Assert.AreEqual(DeathCause.Train, sim.State.LastDeath);
        }

        // ---------- #51: bike stun ----------
        private const string BikeFixture = @"{
          ""id"": ""bike-1"", ""columns"": 9,
          ""medal"": {""gold"": 5, ""silver"": 10, ""bronze"": 20}, ""startColumn"": 4, ""bays"": [4],
          ""rows"": [
            {""kind"": ""goal""},
            {""kind"": ""bike"", ""dir"": ""left"", ""speed"": 2.0, ""objects"": [
              {""pieceId"": ""cyclist"", ""offset"": 6.0, ""spacing"": 0, ""phase"": 0}]},
            {""kind"": ""bank""}
          ]}";

        [Test]
        public void BikeCollision_StunsNeverKills_WreckStaysPassable()
        {
            var sim = Sim(BikeFixture);
            int crashes = 0;
            sim.OnRiderCrashed += (_, _, _) => crashes++;
            sim.EnqueueMove(Move.Forward); // stand in the bike lane at col 4
            for (int i = 0; i < 600 && crashes == 0; i++) sim.Tick();
            Assert.AreEqual(1, crashes, "the cyclist crashed");
            Assert.AreEqual(0, sim.State.Deaths, "bike lane never kills");
            Assert.AreEqual(120, sim.State.StunTicksLeft, "2s stun (120 ticks) begins");

            Assert.IsFalse(sim.EnqueueMove(Move.Forward), "swipes dropped during stun");
            for (int i = 0; i < 130; i++) sim.Tick();
            Assert.AreEqual(0, sim.State.StunTicksLeft, "stun over");
            // wreck persists, passable: standing on it re-stuns nothing
            long stunAfter = sim.State.StunTicksLeft;
            for (int i = 0; i < 120; i++) sim.Tick();
            Assert.AreEqual(0, sim.State.Deaths);
            Assert.AreEqual(stunAfter, sim.State.StunTicksLeft, "crashed wreck causes no further stun");
        }

        // ---------- #52: walkway ----------
        private const string WalkwayFixture = @"{
          ""id"": ""walk-1"", ""columns"": 7,
          ""medal"": {""gold"": 5, ""silver"": 10, ""bronze"": 20}, ""startColumn"": 3, ""bays"": [3],
          ""rows"": [
            {""kind"": ""goal""},
            {""kind"": ""walkway"", ""dir"": ""right"", ""speed"": 2.0},
            {""kind"": ""bank""}
          ]}";

        [Test]
        public void Walkway_Carries_AndTheEdgeKills()
        {
            var sim = Sim(WalkwayFixture);
            sim.EnqueueMove(Move.Forward);
            Settle(sim);
            float x0 = sim.State.PlayerX;
            for (int i = 0; i < 30; i++) sim.Tick();
            Assert.AreEqual(x0 + 1.0f, sim.State.PlayerX, 1e-3f, "carried at 2 cells/s for 0.5s");
            int deaths = 0; DeathCause cause = DeathCause.None;
            sim.OnDeath += c => { deaths++; cause = c; };
            for (int i = 0; i < 300 && deaths == 0; i++) sim.Tick();
            Assert.AreEqual(1, deaths, "carried off the edge (owner rule: edge kills)");
            Assert.AreEqual(DeathCause.EdgeDrift, cause);
        }
    }
}
