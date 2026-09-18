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
        public void TheGatorWarnsBeforeItOpens_AndTheWarningEndsWhereTheKillBegins()
        {
            var def = PieceRegistry.Load().Get<LaneObjectDef>("gator");
            var train = new ObjectTrain { Def = def, PhaseTicks = 0 };
            int open = def.cycleActiveTicks;
            int lead = SpriteSelector.TelegraphTicks;

            Assert.That(SpriteSelector.GatorTelegraph(def, train, 0), Is.EqualTo(0f),
                "a gator at the start of its closed phase is not warning yet");
            Assert.That(SpriteSelector.GatorTelegraph(def, train, open - lead - 1), Is.EqualTo(0f),
                "no warning before the lead window opens");
            Assert.That(SpriteSelector.GatorTelegraph(def, train, open - lead), Is.GreaterThan(0f)
                .Or.EqualTo(0f), "the window starts here");
            Assert.That(SpriteSelector.GatorTelegraph(def, train, open - 1), Is.GreaterThan(0.9f),
                "by the last closed tick the warning is at full strength");

            // it must RAMP, not blink on: a one-frame flash is not a warning
            float prev = -1f;
            for (int t = open - lead; t < open; t++)
            {
                float v = SpriteSelector.GatorTelegraph(def, train, t);
                Assert.That(v, Is.GreaterThanOrEqualTo(prev), $"the warning must build, tick {t}");
                prev = v;
            }

            // and it stops the instant the mouth is actually open — the open
            // sprite is the signal from there on
            for (int t = open; t < open + def.cycleInactiveTicks; t++)
                Assert.That(SpriteSelector.GatorTelegraph(def, train, t), Is.EqualTo(0f),
                    $"tick {t}: the mouth is open; the warning is over");

            Assert.That(lead / 60f, Is.GreaterThanOrEqualTo(0.25f),
                "the lead must be at least human reaction time, or it warns nobody");
        }

        [Test]
        public void TheWarningNeverChangesWhatIsLethal_OrWhatIsDrawnOpen()
        {
            // The telegraph is view-only. If it ever leaks into the sim or the
            // sprite choice, the "drawn open == lethal" contract breaks and the
            // warning starts lying (#148).
            var def = PieceRegistry.Load().Get<LaneObjectDef>("gator");
            var train = new ObjectTrain { Def = def, PhaseTicks = 0 };
            int period = def.cycleActiveTicks + def.cycleInactiveTicks;
            int warningTicks = 0;
            for (int tick = 0; tick < period; tick++)
            {
                bool warning = SpriteSelector.GatorTelegraph(def, train, tick) > 0f;
                if (!warning) continue;
                warningTicks++;
                Assert.That(def.IsRideableAtTick(tick, 0), Is.True,
                    $"tick {tick}: a warning tick must still be SAFE to ride");
                Assert.That(SpriteSelector.GatorIndex(def, train, tick, +1), Is.LessThan(2),
                    $"tick {tick}: a warning must not draw the open mouth — that would "
                    + "teach players an open mouth is sometimes survivable");
            }

            // Without this the test passes vacuously on a gator that never
            // warns at all — it would `continue` past every tick.
            Assert.That(warningTicks, Is.GreaterThan(0),
                "the gator never warns, so this test proved nothing");
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
