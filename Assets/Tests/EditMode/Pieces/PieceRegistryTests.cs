using System.IO;
using System.Linq;
using FrogAcross.Pieces;
using NUnit.Framework;
using UnityEngine;

namespace FrogAcross.Tests.EditMode.Pieces
{
    public class PieceRegistryTests
    {
        [Test]
        public void Registry_LoadsWithUniqueNonEmptyIds()
        {
            var reg = PieceRegistry.Load();
            Assert.IsNotEmpty(reg.pieces);
            Assert.IsTrue(reg.pieces.All(p => p != null && !string.IsNullOrEmpty(p.id)), "null/empty-id piece");
            CollectionAssert.AllItemsAreUnique(reg.pieces.Select(p => p.id).ToList());
        }

        [Test]
        public void Registry_CarriesTheV1Catalogue()
        {
            var reg = PieceRegistry.Load();
            Assert.GreaterOrEqual(reg.All<CharacterDef>().Count(), 6);
            Assert.GreaterOrEqual(reg.All<LaneKindDef>().Count(), 10);
            Assert.GreaterOrEqual(reg.All<LaneObjectDef>().Count(), 13);
            Assert.GreaterOrEqual(reg.All<ObstructionDef>().Count(), 9);
            // Owner's gator rule is data:
            var gator = reg.Get<LaneObjectDef>("gator");
            Assert.IsTrue(gator.inactiveKills, "open-mouth gator must kill");
            Assert.Less(gator.rideableZoneEnd, 0.7f, "head/snout must be outside the rideable zone");
        }

        [Test]
        public void AddingAPiece_NeedsNoCode()
        {
            // A def created purely from data resolves through the same lookup path.
            var reg = ScriptableObject.CreateInstance<PieceRegistry>();
            var def = ScriptableObject.CreateInstance<LaneObjectDef>();
            def.id = "test-hovercraft";
            def.role = ObjectRole.Rideable;
            reg.pieces.Add(def);
            Assert.AreEqual(def, reg.Get<LaneObjectDef>("test-hovercraft"));
        }

        [Test]
        public void GameplayCode_NeverSwitchesOnPieceIds()
        {
            // Piece behavior must flow from def fields. Sim/View/Input sources
            // must not contain piece-id string literals.
            string[] ids = { "\"gator\"", "\"truck\"", "\"cyclist\"", "\"turtle-log\"", "\"freight\"", "\"lily-pad\"" };
            var offenders = Directory.GetFiles("Assets/Scripts/Runtime", "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Replace('\\', '/').Contains("/Pieces/"))
                .Where(f => ids.Any(File.ReadAllText(f).Contains))
                .ToList();
            Assert.IsEmpty(offenders, "piece-id literals in gameplay code: " + string.Join(", ", offenders));
        }

        [Test]
        public void RideableCycle_MathIsExact()
        {
            var def = ScriptableObject.CreateInstance<LaneObjectDef>();
            def.role = ObjectRole.Rideable;
            def.cycleActiveTicks = 300;
            def.cycleInactiveTicks = 120;
            Assert.IsTrue(def.IsRideableAtTick(0, 0));
            Assert.IsTrue(def.IsRideableAtTick(299, 0));
            Assert.IsFalse(def.IsRideableAtTick(300, 0), "first inactive tick");
            Assert.IsFalse(def.IsRideableAtTick(419, 0));
            Assert.IsTrue(def.IsRideableAtTick(420, 0), "wraps to active");
            Assert.IsFalse(def.IsRideableAtTick(0, 300), "phase shifts the cycle");
        }
    }
}
