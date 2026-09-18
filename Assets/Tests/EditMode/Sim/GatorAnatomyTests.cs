using System.Globalization;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.Sim;
using NUnit.Framework;

namespace FrogAcross.Tests.EditMode.Sim
{
    /// <summary>
    /// #147. The owner, on device: "If I jump on or even a little behind the
    /// eyes I still sink... This seems to be worse when gator is moving left to
    /// right."
    ///
    /// Measured, the ride band was symmetric to within a hundredth of a body
    /// length (right 0.06–0.68, left 0.32–0.94 in screen space, both 0.62 wide),
    /// so the reported asymmetry was not in this logic. The real defect was that
    /// the eyes sat outside the band in BOTH directions. These tests pin the
    /// anatomy against the drawn art, and pin the symmetry so it stays true.
    ///
    /// Art, from LaneObject.dc.html over a 206px body:
    ///   back  x=56..156  -> 0.27..0.76
    ///   eyes  x=152..166 -> 0.738..0.806
    ///   head  x=140..186 -> 0.68..0.90
    /// Fractions below are SCREEN fractions: 0 is the sprite's left edge as
    /// drawn, so for a left-facing (mirrored) gator they are 1 - the above.
    /// </summary>
    public class GatorAnatomyTests
    {
        private const float EyesMid = 0.772f;   // centre of the drawn eyes
        private const float BackMid = 0.50f;    // solidly the back
        private const float Snout = 0.88f;      // past the eyes, still head

        private static bool Rides(string dir, float screenFrac)
        {
            const float size = 3.7f;
            int col = 5;
            float left = col - screenFrac * size;
            string json = $@"{{
              ""id"": ""anatomy"", ""columns"": 11,
              ""medal"": {{""gold"": 5, ""silver"": 10, ""bronze"": 20}},
              ""startColumn"": {col}, ""bays"": [1],
              ""rows"": [
                {{""kind"": ""goal""}},
                {{""kind"": ""swamp"", ""dir"": ""{dir}"", ""speed"": 0.0001, ""objects"": [
                  {{""pieceId"": ""gator"", ""offset"": {left.ToString(CultureInfo.InvariantCulture)}, ""spacing"": 0, ""phase"": 0}}]}},
                {{""kind"": ""bank""}}
              ]}}";
            var sim = new GameSim(LevelLoader.Parse(json, PieceRegistry.Load()));
            sim.EnqueueMove(Move.Forward);
            for (int i = 0; i < SimConfig.HopCooldownTicks + 2; i++) sim.Tick();
            return sim.State.Riding;
        }

        /// <summary>Mirror a screen fraction for a left-facing gator.</summary>
        private static float Drawn(string dir, float rightFacingFrac) =>
            dir == "left" ? 1f - rightFacingFrac : rightFacingFrac;

        [Test]
        public void TheEyesCarryYou_InBothDirections()
        {
            foreach (var dir in new[] { "right", "left" })
                Assert.That(Rides(dir, Drawn(dir, EyesMid)), Is.True,
                    $"{dir}-moving gator: the drawn eyes must carry the player — "
                    + "the owner reported sinking here, in both directions (#147)");
        }

        [Test]
        public void TheSnoutPastTheEyes_StillKills()
        {
            foreach (var dir in new[] { "right", "left" })
                Assert.That(Rides(dir, Drawn(dir, Snout)), Is.False,
                    $"{dir}-moving gator: past the eyes is snout and must not carry the player — "
                    + "widening the zone must not make the whole head safe");
        }

        [Test]
        public void TheBackStillCarriesYou_InBothDirections()
        {
            foreach (var dir in new[] { "right", "left" })
                Assert.That(Rides(dir, Drawn(dir, BackMid)), Is.True, $"{dir}-moving gator: the back");
        }

        /// <summary>The owner reported one direction being worse than the other.
        /// It measured symmetric, and this is what keeps it that way.</summary>
        [Test]
        public void TheSafeBand_IsTheSameWidthInBothDirections()
        {
            float widthRight = BandWidth("right"), widthLeft = BandWidth("left");
            Assert.That(widthLeft, Is.EqualTo(widthRight).Within(0.02f),
                $"right-moving gators carry you across {widthRight:0.00} of the body and "
                + $"left-moving across {widthLeft:0.00} — a mirrored sprite must mirror its "
                + "ride zone exactly, or one direction is quietly harder (#147)");
        }

        private static float BandWidth(string dir)
        {
            float first = -1f, last = -1f;
            for (int i = 0; i <= 100; i++)
            {
                float f = i / 100f;
                if (!Rides(dir, f)) continue;
                if (first < 0) first = f;
                last = f;
            }
            Assert.That(first, Is.GreaterThanOrEqualTo(0f), $"{dir}: nothing on the body rides at all");
            return last - first;
        }
    }
}
