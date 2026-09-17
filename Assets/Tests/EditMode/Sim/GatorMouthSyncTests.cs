using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.View;
using NUnit.Framework;

namespace FrogAcross.Tests.EditMode.Sim
{
    /// <summary>
    /// #148 reported dying "a quarter to half a second before the mouth opens".
    /// Measured, the gap is zero: the sim and the sprite selector call the same
    /// IsRideableAtTick with the same tick and phase, so they cannot disagree.
    ///
    /// This pins that. If anyone ever gives the view its own tick source — an
    /// interpolated render tick, a cached sprite, a separate animation clock —
    /// the player starts dying to a hazard that is not on screen yet, and this
    /// test is what catches it.
    /// </summary>
    public class GatorMouthSyncTests
    {
        [Test]
        public void SimLethality_AndTheDrawnMouth_AgreeOnEveryTickOfTheCycle()
        {
            var def = PieceRegistry.Load().Get<LaneObjectDef>("gator");
            Assert.That(def.inactiveKills, Is.True, "the gator kills when its mouth is open");
            int period = def.cycleActiveTicks + def.cycleInactiveTicks;
            Assert.That(period, Is.GreaterThan(1), "a gator with no cycle cannot desync");

            foreach (int dirSign in new[] { +1, -1 })
            foreach (int phase in new[] { 0, 37, 300 })
            {
                var train = new ObjectTrain { Def = def, PhaseTicks = phase };
                for (int tick = 0; tick < period * 2; tick++)
                {
                    bool simLethal = !def.IsRideableAtTick(tick, phase);
                    bool drawnOpen = SpriteSelector.GatorIndex(def, train, tick, dirSign) >= 2;
                    Assert.That(drawnOpen, Is.EqualTo(simLethal),
                        $"dir={dirSign} phase={phase} tick={tick}: the sim says lethal={simLethal} "
                        + $"but the drawn mouth is open={drawnOpen} — the player dies to an "
                        + "invisible hazard, or rides a visibly open mouth");
                }
            }
        }

        [Test]
        public void TheOpenMouthSprites_AreTheOnesIndexedAsOpen()
        {
            // The agreement above is vacuous if index >= 2 is not actually the
            // open-mouth art. Pin the asset's sprite order to the index maths.
            var def = PieceRegistry.Load().Get<LaneObjectDef>("gator");
            Assert.That(def.sprites.Length, Is.EqualTo(4), "closed/open x right/left");
            Assert.That(def.sprites[0].name, Does.Not.Contain("open"), "[0] is closed-right");
            Assert.That(def.sprites[1].name, Does.Not.Contain("open"), "[1] is closed-left");
            Assert.That(def.sprites[2].name, Does.Contain("open"), "[2] must be open-right");
            Assert.That(def.sprites[3].name, Does.Contain("open"), "[3] must be open-left");
            Assert.That(def.sprites[1].name, Does.Contain("left"));
            Assert.That(def.sprites[3].name, Does.Contain("left"));
        }
    }
}
