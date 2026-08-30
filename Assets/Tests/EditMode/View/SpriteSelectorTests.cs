using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.Sim;
using FrogAcross.View;
using NUnit.Framework;
using UnityEngine;

namespace FrogAcross.Tests.EditMode.View
{
    public class SpriteSelectorTests
    {
        [Test]
        public void MoveArc_IsTheCharactersOwnStyle()
        {
            // owner: "character should use its designated move when it moves.
            // Hop or run" — CharacterDef.moveStyle was carried in the data and
            // ignored by the view, so every creature slid flat between cells.
            Assert.AreEqual(0f, SpriteSelector.MoveArc(MoveStyle.Hop, 0f).lift, 1e-4f, "starts on the ground");
            Assert.AreEqual(0f, SpriteSelector.MoveArc(MoveStyle.Hop, 1f).lift, 1e-4f, "lands on the ground");

            // a hop: one arc, peaking mid-move, stretched at the apex
            var apex = SpriteSelector.MoveArc(MoveStyle.Hop, 0.5f);
            Assert.AreEqual(SpriteSelector.HopHeight, apex.lift, 1e-3f);
            Assert.Greater(apex.squash, 1f, "a hop stretches upward at the top");
            for (float p = 0.05f; p < 1f; p += 0.05f)
                Assert.LessOrEqual(SpriteSelector.MoveArc(MoveStyle.Hop, p).lift, apex.lift + 1e-4f,
                    "a hop has a single apex");

            // a run: feet stay near the ground, two footfalls per cell
            int peaks = 0;
            float highest = 0f;
            for (float p = 0.02f; p < 1f; p += 0.02f)
            {
                float here = SpriteSelector.MoveArc(MoveStyle.Step, p).lift;
                highest = Mathf.Max(highest, here);
                if (here > SpriteSelector.MoveArc(MoveStyle.Step, p - 0.02f).lift
                    && here >= SpriteSelector.MoveArc(MoveStyle.Step, p + 0.02f).lift) peaks++;
            }
            Assert.AreEqual(2, peaks, "a run bobs twice across the cell");
            Assert.AreEqual(SpriteSelector.StepBob, highest, 1e-3f);
            Assert.Less(highest, SpriteSelector.HopHeight / 2f, "a runner never hops");
            Assert.Less(SpriteSelector.MoveArc(MoveStyle.Step, 0.25f).squash, 1f, "a runner squats, not stretches");
        }

        [Test]
        public void CharacterIndices_MapFacings()
        {
            Assert.AreEqual(0, SpriteSelector.CharacterIndex(Move.Forward));
            Assert.AreEqual(1, SpriteSelector.CharacterIndex(Move.Back));
            Assert.AreEqual(2, SpriteSelector.CharacterIndex(Move.DiagForwardLeft));
            Assert.AreEqual(3, SpriteSelector.CharacterIndex(Move.Right));
        }

        [Test]
        public void RiderFrames_CycleWithTicks_AndSidesSplit()
        {
            // 4 liveries → left side starts at 12
            Assert.AreEqual(0, SpriteSelector.RiderIndex(0, +1, 0, 4));
            Assert.AreEqual(1, SpriteSelector.RiderIndex(9, +1, 0, 4));
            Assert.AreEqual(2, SpriteSelector.RiderIndex(18, +1, 0, 4));
            Assert.AreEqual(0, SpriteSelector.RiderIndex(27, +1, 0, 4), "wraps");
            Assert.AreEqual(12 + 3 * 3 + 1, SpriteSelector.RiderIndex(9, -1, 3, 4));
        }

        [Test]
        public void GatorIndex_TracksMouthCycleAndDirection()
        {
            var def = ScriptableObject.CreateInstance<LaneObjectDef>();
            def.role = ObjectRole.Rideable;
            def.inactiveKills = true;
            def.cycleActiveTicks = 300;
            def.cycleInactiveTicks = 120;
            var train = new ObjectTrain { Def = def, PhaseTicks = 0 };
            Assert.AreEqual(0, SpriteSelector.GatorIndex(def, train, 10, +1), "closed right");
            Assert.AreEqual(1, SpriteSelector.GatorIndex(def, train, 10, -1), "closed left");
            Assert.AreEqual(2, SpriteSelector.GatorIndex(def, train, 310, +1), "open right");
            Assert.AreEqual(3, SpriteSelector.GatorIndex(def, train, 310, -1), "open left");
        }

        [Test]
        public void TurtleAlpha_DipsWhileSubmerged()
        {
            var def = ScriptableObject.CreateInstance<LaneObjectDef>();
            def.role = ObjectRole.Rideable;
            def.cycleActiveTicks = 240;
            def.cycleInactiveTicks = 90;
            var train = new ObjectTrain { Def = def, PhaseTicks = 0 };
            Assert.AreEqual(1f, SpriteSelector.TurtleAlpha(def, train, 100));
            Assert.Less(SpriteSelector.TurtleAlpha(def, train, 250), 1f);
        }

        [Test]
        public void LiveryAssignment_IsDeterministicAndSpread()
        {
            int a = SpriteSelector.LiveryFor(1, 0, 0, 4);
            Assert.AreEqual(a, SpriteSelector.LiveryFor(1, 0, 0, 4), "deterministic");
            Assert.AreNotEqual(SpriteSelector.LiveryFor(1, 0, 0, 4), SpriteSelector.LiveryFor(1, 0, 1, 4),
                "adjacent instances differ");
        }
    }
}
