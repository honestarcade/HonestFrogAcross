using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.Sim;
using NUnit.Framework;

namespace FrogAcross.Tests.EditMode.Sim
{
    /// <summary>
    /// #124: the gator's ride zone is authored from its NOSE (0.05–0.55 of the
    /// body), and the sprite mirrors with travel direction. The zone has to
    /// mirror with it — otherwise a left-moving gator is safe on its drawn head
    /// and deadly on its drawn back, which is how the owner kept drowning on
    /// what looked like a clean landing.
    /// </summary>
    public class GatorRideZoneTests
    {
        // goal / swamp(one gator, left edge at x=1.0, 3.7 cells long) / bank.
        // Landing column picks the spot on the body: col 2 → 27% along,
        // col 4 → 81% along, both measured from the body's left edge.
        private static string Fixture(string dir, int startColumn, int phase) => $@"{{
          ""id"": ""gator-zone"", ""columns"": 8,
          ""medal"": {{""gold"": 5, ""silver"": 10, ""bronze"": 20}},
          ""startColumn"": {startColumn}, ""bays"": [3],
          ""rows"": [
            {{""kind"": ""goal""}},
            {{""kind"": ""swamp"", ""dir"": ""{dir}"", ""speed"": 0.5, ""objects"": [
              {{""pieceId"": ""gator"", ""offset"": 1.0, ""spacing"": 0, ""phase"": {phase}}}]}},
            {{""kind"": ""bank""}}
          ]}}";

        private const int NoseEnd = 0;      // mouth closed: rideable, phase 0
        private const int MouthOpen = 300;  // past cycleActiveTicks: the mouth is open

        private static GameSim Land(string dir, int startColumn, int phase = NoseEnd,
            bool backTest = false)
        {
            if (backTest) return LandAt(dir, 0.62f, phase);
            var sim = new GameSim(LevelLoader.Parse(Fixture(dir, startColumn, phase), PieceRegistry.Load()));
            sim.EnqueueMove(Move.Forward);
            for (int i = 0; i < SimConfig.HopCooldownTicks + 2; i++) sim.Tick();
            return sim;
        }

        /// <summary>Land at a precise fraction along the body, measured from the
        /// drawn tail (so 0.62 is the rear back in either direction).</summary>
        private static GameSim LandAt(string dir, float fracFromTail, int phase)
        {
            // gator is 3.7 cells; place its left edge so the wanted fraction
            // falls on an integer column, and mirror the target for left-movers
            const float size = 3.7f;
            float want = dir == "left" ? 1f - fracFromTail : fracFromTail;
            int col = 4;
            float left = col - want * size;
            string json = $@"{{
              ""id"": ""gator-back"", ""columns"": 9,
              ""medal"": {{""gold"": 5, ""silver"": 10, ""bronze"": 20}},
              ""startColumn"": {col}, ""bays"": [1],
              ""rows"": [
                {{""kind"": ""goal""}},
                {{""kind"": ""swamp"", ""dir"": ""{dir}"", ""speed"": 0.5, ""objects"": [
                  {{""pieceId"": ""gator"", ""offset"": {left.ToString(System.Globalization.CultureInfo.InvariantCulture)}, ""spacing"": 0, ""phase"": {phase}}}]}},
                {{""kind"": ""bank""}}
              ]}}";
            var sim = new GameSim(LevelLoader.Parse(json, PieceRegistry.Load()));
            sim.EnqueueMove(Move.Forward);
            for (int i = 0; i < SimConfig.HopCooldownTicks + 2; i++) sim.Tick();
            return sim;
        }

        [Test]
        public void RightMoving_BackIsSafe_HeadKills()
        {
            // art faces right, so the head is the right-hand end of the body
            Assert.That(Land("right", 2).State.Riding, Is.True,
                "27% along a right-moving gator is its back — a landing there rides");
            var head = Land("right", 4);
            Assert.That(head.State.Riding, Is.False, "81% along is the snout");
            Assert.That(head.State.Deaths, Is.EqualTo(1));
        }

        [Test]
        public void LeftMoving_BackIsSafe_HeadKills()
        {
            // The sprite mirrors, so the head is now the LEFT-hand end and the
            // back is the right. Before the fix this was exactly inverted: the
            // drawn back drowned you and the drawn head carried you.
            Assert.That(Land("left", 4).State.Riding, Is.True,
                "81% along a LEFT-moving gator is its back — a landing there must ride");

            var head = Land("left", 2);
            Assert.That(head.State.Riding, Is.False,
                "27% along a left-moving gator is its snout — that must not carry the player");
            Assert.That(head.State.Deaths, Is.EqualTo(1));
        }

        [Test]
        public void TheWholeDrawnBackRides_NotJustItsFrontHalf()
        {
            // #129: the zone stopped at 0.55 while the drawn back runs to 0.76
            // and the head only begins at 0.68 (LaneObject.dc.html: upper body
            // x=56..156, head x=140..186, of a 206px body). So the rear half of
            // the visible back drowned the player — the owner's original
            // symptom, surviving the mirroring fix.
            // col 3 sits 54% along; col 4 is 81%. Use a 5-wide gator span so a
            // column lands squarely on the rear back.
            foreach (var dir in new[] { "right", "left" })
            {
                var sim = Land(dir, dir == "right" ? 3 : 3, backTest: true);
                Assert.That(sim.State.Riding, Is.True,
                    $"{dir}-moving gator: 62% along is drawn back and must carry the player");
            }
        }

        [Test]
        public void AnOpenMouthKillsAnywhereOnTheBody_EitherDirection()
        {
            foreach (var dir in new[] { "right", "left" })
            foreach (int col in new[] { 2, 4 })
            {
                var sim = Land(dir, col, MouthOpen);
                Assert.That(sim.State.Riding, Is.False, $"{dir} gator, column {col}");
                Assert.That(sim.State.Deaths, Is.EqualTo(1), $"{dir} gator, column {col}");
                Assert.That(sim.State.LastDeath, Is.EqualTo(DeathCause.Gator),
                    $"{dir} gator, column {col}: an open mouth is a gator death, not a drowning");
            }
        }
    }
}
