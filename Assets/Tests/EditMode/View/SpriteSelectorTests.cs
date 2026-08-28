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
