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
            // The zone must track the DRAWN anatomy, not merely be "small".
            // Body art is 206px: upper body (the back) x=56..156 → 0.27..0.76,
            // head x=140..186 → 0.68..0.90, EYES x=152..166 → 0.738..0.806.
            //
            // #129 ended the zone at 0.68, where the head begins, following the
            // shipped copy's "the head is never safe". On device the owner found
            // that wrong — landing on the eyes sinks you, and it should not
            // (#147). The design mock always said "ride the back and the eyes
            // like a log"; the owner confirmed the mock is the intent, so the
            // zone now runs to just past the eyes and copy.json was rewritten to
            // match. The snout beyond 0.81 is still lethal.
            Assert.That(gator.rideableZoneEnd, Is.EqualTo(0.81f).Within(0.02f),
                "the zone must end just past the drawn eyes — too tight and the "
                + "eyes drown you, too loose and the snout carries you");
            Assert.That(gator.rideableZoneStart, Is.LessThanOrEqualTo(0.1f),
                "the tail end of the back is rideable");
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
